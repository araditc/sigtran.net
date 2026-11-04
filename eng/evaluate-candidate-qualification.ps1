param(
    [string]$PolicyPath = 'eng/qualification/release-policy.json',
    [string]$EvidenceRoot = '',
    [string]$SourceCommit = '',
    [string]$PackagePath = '',
    [string]$JsonOutputPath = 'artifacts/qualification/candidate-decision.json',
    [switch]$RequireGo
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (-not $EvidenceRoot) { $EvidenceRoot = $root }
if (-not $SourceCommit) { $SourceCommit = (& git -C $root rev-parse HEAD).Trim() }
$outputPath = Join-Path $root $JsonOutputPath
New-Item -ItemType Directory -Force (Split-Path $outputPath -Parent) | Out-Null
# Build the verifier from this checkout, rather than accidentally executing stale tool binaries.
& dotnet build "$PSScriptRoot/Sigtran.NET.Qualification/Sigtran.NET.Qualification.csproj" -c Release -m:1 --nologo -v:q *> "$outputPath.build.log"
if ($LASTEXITCODE -ne 0) { throw 'Qualification verifier build failed.' }
$arguments = @('evaluate', (Join-Path $root $PolicyPath), $EvidenceRoot)
if ($PackagePath) {
    $arguments += @($SourceCommit, (Resolve-Path $PackagePath).Path, (Join-Path $root 'eng/qualification/scope.json'))
}
$json = & dotnet "$PSScriptRoot/Sigtran.NET.Qualification/bin/Release/net10.0/Sigtran.NET.Qualification.dll" @arguments
if ($LASTEXITCODE -notin @(0,1)) { throw 'Qualification verifier failed to produce a decision.' }
$decision = $json | ConvertFrom-Json
if ($decision.decision -notin @('GO','NO-GO')) { throw 'Invalid qualification decision.' }
$decision | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $outputPath -Encoding utf8
if ($RequireGo -and $decision.decision -ne 'GO') { throw 'Candidate qualification is NO-GO.' }
# A reported NO-GO is a successful assessment; only -RequireGo fails the caller.
$global:LASTEXITCODE = 0
$decision
