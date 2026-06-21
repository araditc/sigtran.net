param(
    [string]$OutputPath = "artifacts/benchmarks/Sigtran.NET-smoke-benchmark.json"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$outputFullPath = Join-Path $root $OutputPath
$outputDirectory = Split-Path $outputFullPath -Parent
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$testOutput = dotnet run --project (Join-Path $root "src/Sigtran.NET.Tests/Sigtran.NET.Tests.csproj") -c Release
$exitCode = $LASTEXITCODE
$stopwatch.Stop()

if ($exitCode -ne 0) {
    $testOutput | Set-Content -Path (Join-Path $outputDirectory "Sigtran.NET-smoke-benchmark.failed.log") -Encoding UTF8
    throw "Benchmark workload failed with exit code $exitCode."
}

$passedChecks = ($testOutput | Where-Object { $_ -like "PASS *" }).Count
$report = [ordered]@{
    name = "Sigtran.NET smoke benchmark"
    workload = "Release test workload"
    durationMilliseconds = $stopwatch.ElapsedMilliseconds
    passedChecks = $passedChecks
    commercialPerformanceEvidence = $false
    note = "Smoke benchmark only; peer/load benchmark evidence is still required for commercial performance claims."
}

$report | ConvertTo-Json -Depth 10 | Set-Content -Path $outputFullPath -Encoding UTF8
$reportHash = (Get-FileHash -Path $outputFullPath -Algorithm SHA256).Hash.ToLowerInvariant()

[ordered]@{
    OutputPath = $OutputPath
    Sha256 = $reportHash
    DurationMilliseconds = $stopwatch.ElapsedMilliseconds
    PassedChecks = $passedChecks
} | ConvertTo-Json -Depth 5
