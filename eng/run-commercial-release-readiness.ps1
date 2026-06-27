<#
.SYNOPSIS
Runs the SIGTRAN.NET commercial release-day readiness checks.

.DESCRIPTION
This runner executes the local release evidence pipeline and records a
reviewable readiness report under the configured artifact root. It never stores
secret values. Remote VM checks use an existing SSH key only, and signing checks
only verify whether the produced package is already trusted and signed.
#>
param(
    [string]$SolutionPath = "src/Sigtran.NET.sln",
    [string]$ProjectPath = "src/Sigtran.NET/Sigtran.NET.csproj",
    [string]$TestProjectPath = "src/Sigtran.NET.Tests/Sigtran.NET.Tests.csproj",
    [string]$Version = "1.0.0",
    [string]$ArtifactRoot = "artifacts/commercial-release",
    [string]$VmHost = "192.168.100.28",
    [string]$VmUser = "ammar",
    [string]$SshKeyPath = "$HOME/.ssh/sigtran_vm_release_ed25519",
    [switch]$SkipVmProbe
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$runId = [DateTimeOffset]::UtcNow.ToString("yyyyMMddTHHmmssZ")
$artifactRootFullPath = Join-Path $root $ArtifactRoot
$runRoot = Join-Path $artifactRootFullPath $runId
$logRoot = Join-Path $runRoot "logs"
$reportPath = Join-Path $runRoot "commercial-release-readiness.md"
$jsonPath = Join-Path $runRoot "commercial-release-readiness.json"

New-Item -ItemType Directory -Force -Path $logRoot | Out-Null

function Get-RelativePath {
    param([string]$Path)

    $rootUri = [System.Uri]::new((Resolve-Path $root).Path + [System.IO.Path]::DirectorySeparatorChar)
    $targetUri = [System.Uri]::new((Resolve-Path $Path).Path)
    return [System.Uri]::UnescapeDataString($rootUri.MakeRelativeUri($targetUri).ToString()).Replace("/", [System.IO.Path]::DirectorySeparatorChar)
}

function Invoke-ReadinessStep {
    param(
        [string]$Name,
        [scriptblock]$Action,
        [string]$CommercialBlocker
    )

    $logPath = Join-Path $logRoot "$Name.log"
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $passed = $false
    $errorMessage = $null

    try {
        & $Action *> $logPath
        $passed = $true
    }
    catch {
        $errorMessage = $_.Exception.Message
        $_ | Out-String | Add-Content -Path $logPath -Encoding UTF8
    }
    finally {
        $stopwatch.Stop()
    }

    [ordered]@{
        Name = $Name
        Passed = $passed
        DurationMilliseconds = $stopwatch.ElapsedMilliseconds
        LogPath = Get-RelativePath -Path $logPath
        CommercialBlocker = if ($passed) { $null } else { $CommercialBlocker }
        Error = $errorMessage
    }
}

function Assert-LastExitCode {
    param([string]$Operation)

    if ($LASTEXITCODE -ne 0) {
        throw "$Operation failed with exit code $LASTEXITCODE."
    }
}

function Get-FileEvidence {
    param([string]$Path)

    if (!(Test-Path $Path)) {
        return $null
    }

    $item = Get-Item $Path
    [ordered]@{
        Path = Get-RelativePath -Path $item.FullName
        Size = $item.Length
        Sha256 = (Get-FileHash -Path $item.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}

$packagePath = Join-Path $root "src/Sigtran.NET/bin/Release/Sigtran.NET.$Version.nupkg"
$symbolsPath = Join-Path $root "src/Sigtran.NET/bin/Release/Sigtran.NET.$Version.snupkg"
$sbomPath = Join-Path $root "artifacts/sbom/Sigtran.NET.spdx.json"
$apiPath = Join-Path $root "artifacts/api/Sigtran.NET-public-api.txt"
$benchmarkPath = Join-Path $root "artifacts/benchmarks/Sigtran.NET-smoke-benchmark.json"
$provenancePath = Join-Path $root "artifacts/provenance/Sigtran.NET.provenance.json"
$signingPackagePath = Join-Path $root "artifacts/signing/Sigtran.NET.$Version.nupkg"

$steps = @()
$steps += Invoke-ReadinessStep -Name "build" -CommercialBlocker "build-failed" -Action {
    dotnet build (Join-Path $root $SolutionPath) -c Release
    Assert-LastExitCode "dotnet build"
}

$steps += Invoke-ReadinessStep -Name "test" -CommercialBlocker "tests-failed" -Action {
    dotnet run --project (Join-Path $root $TestProjectPath) -c Release
    Assert-LastExitCode "dotnet test workload"
}

$steps += Invoke-ReadinessStep -Name "pack" -CommercialBlocker "pack-failed" -Action {
    dotnet pack (Join-Path $root $ProjectPath) -c Release /p:Version=$Version
    Assert-LastExitCode "dotnet pack"
}

$steps += Invoke-ReadinessStep -Name "sbom" -CommercialBlocker "sbom-generation-failed" -Action {
    powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root "eng/generate-sbom.ps1") -CreatedUtc ([DateTimeOffset]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"))
    Assert-LastExitCode "SBOM generation"
}

$steps += Invoke-ReadinessStep -Name "public-api-baseline" -CommercialBlocker "public-api-baseline-failed" -Action {
    powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root "eng/generate-public-api-baseline.ps1")
    Assert-LastExitCode "public API baseline generation"
}

$steps += Invoke-ReadinessStep -Name "smoke-benchmark" -CommercialBlocker "benchmark-generation-failed" -Action {
    powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root "eng/run-benchmark.ps1")
    Assert-LastExitCode "smoke benchmark"
}

$steps += Invoke-ReadinessStep -Name "provenance" -CommercialBlocker "provenance-generation-failed" -Action {
    New-Item -ItemType Directory -Force -Path (Split-Path $signingPackagePath -Parent) | Out-Null
    Copy-Item -Force $packagePath $signingPackagePath
    powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root "eng/generate-provenance.ps1") -PackagePath "artifacts/signing/Sigtran.NET.$Version.nupkg"
    Assert-LastExitCode "provenance generation"
}

$steps += Invoke-ReadinessStep -Name "signature-verification" -CommercialBlocker "trusted-timestamped-package-signing-required" -Action {
    dotnet nuget verify $packagePath --all
    Assert-LastExitCode "package signature verification"
}

if (!$SkipVmProbe) {
    $steps += Invoke-ReadinessStep -Name "linux-vm-ssh-probe" -CommercialBlocker "linux-vm-key-based-ssh-required" -Action {
        if (!(Test-Path $SshKeyPath)) {
            throw "SSH key was not found at $SshKeyPath."
        }

        ssh -i $SshKeyPath -o BatchMode=yes -o StrictHostKeyChecking=accept-new -o ConnectTimeout=10 "$VmUser@$VmHost" "uname -a; command -v ss; command -v tcpdump; command -v dotnet || true"
        Assert-LastExitCode "Linux VM SSH probe"
    }
}

$steps += Invoke-ReadinessStep -Name "github-cli-probe" -CommercialBlocker "github-cli-or-release-dispatch-access-required" -Action {
    $gh = Get-Command gh -ErrorAction Stop
    & $gh.Source auth status
    Assert-LastExitCode "GitHub CLI auth status"
}

$evidence = @(
    Get-FileEvidence -Path $packagePath
    Get-FileEvidence -Path $symbolsPath
    Get-FileEvidence -Path $sbomPath
    Get-FileEvidence -Path $apiPath
    Get-FileEvidence -Path $benchmarkPath
    Get-FileEvidence -Path $provenancePath
) | Where-Object { $null -ne $_ }

$commercialBlockers = @(
    $steps | Where-Object { !$_.Passed -and ![string]::IsNullOrWhiteSpace($_.CommercialBlocker) } | ForEach-Object { $_.CommercialBlocker }
)

if ($commercialBlockers -notcontains "maintained-external-peer-evidence-required") {
    $commercialBlockers += "maintained-external-peer-evidence-required"
}

if ($commercialBlockers -notcontains "production-peer-benchmark-evidence-required") {
    $commercialBlockers += "production-peer-benchmark-evidence-required"
}

$commercialBlockers = $commercialBlockers | Sort-Object -Unique
$localEvidenceReady = ($steps | Where-Object { $_.Name -in @("build", "test", "pack", "sbom", "public-api-baseline", "smoke-benchmark", "provenance") -and !$_.Passed }).Count -eq 0
$commercialReady = $localEvidenceReady -and $commercialBlockers.Count -eq 0
$sourceCommit = (git -C $root rev-parse HEAD).Trim()

$result = [ordered]@{
    RunId = $runId
    SourceCommit = $sourceCommit
    ArtifactRoot = Get-RelativePath -Path $runRoot
    LocalEvidenceReady = $localEvidenceReady
    CommercialReady = $commercialReady
    Steps = $steps
    Evidence = $evidence
    CommercialBlockers = $commercialBlockers
}

$result | ConvertTo-Json -Depth 20 | Set-Content -Path $jsonPath -Encoding UTF8

$builder = [System.Text.StringBuilder]::new()
[void]$builder.AppendLine("# SIGTRAN.NET Commercial Release Readiness Run")
[void]$builder.AppendLine()
[void]$builder.AppendLine(("- Run id: ``{0}``" -f $runId))
[void]$builder.AppendLine(("- Source commit: ``{0}``" -f $sourceCommit))
[void]$builder.AppendLine(("- Local evidence ready: ``{0}``" -f $localEvidenceReady))
[void]$builder.AppendLine(("- Commercial ready: ``{0}``" -f $commercialReady))
[void]$builder.AppendLine()
[void]$builder.AppendLine("## Steps")
[void]$builder.AppendLine()
[void]$builder.AppendLine("| Step | Passed | Duration ms | Log |")
[void]$builder.AppendLine("| --- | --- | ---: | --- |")
foreach ($step in $steps) {
    [void]$builder.AppendLine(("| {0} | {1} | {2} | ``{3}`` |" -f $step.Name, $step.Passed, $step.DurationMilliseconds, $step.LogPath))
}

[void]$builder.AppendLine()
[void]$builder.AppendLine("## Evidence")
[void]$builder.AppendLine()
[void]$builder.AppendLine("| Path | Size | SHA-256 |")
[void]$builder.AppendLine("| --- | ---: | --- |")
foreach ($item in $evidence) {
    [void]$builder.AppendLine(("| ``{0}`` | {1} | ``{2}`` |" -f $item.Path, $item.Size, $item.Sha256))
}

[void]$builder.AppendLine()
[void]$builder.AppendLine("## Commercial Blockers")
[void]$builder.AppendLine()
foreach ($blocker in $commercialBlockers) {
    [void]$builder.AppendLine(("- ``{0}``" -f $blocker))
}

$builder.ToString() | Set-Content -Path $reportPath -Encoding UTF8

[ordered]@{
    RunId = $runId
    ReportPath = Get-RelativePath -Path $reportPath
    JsonPath = Get-RelativePath -Path $jsonPath
    LocalEvidenceReady = $localEvidenceReady
    CommercialReady = $commercialReady
    CommercialBlockers = $commercialBlockers
} | ConvertTo-Json -Depth 10
