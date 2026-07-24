param(
    [string]$AssemblyPath = "src/Sigtran.NET/bin/Release/net10.0/Sigtran.NET.dll",
    [string]$OutputPath = "artifacts/api/Sigtran.NET-public-api.txt"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$assemblyCandidate = if ([System.IO.Path]::IsPathRooted($AssemblyPath)) {
    $AssemblyPath
}
else {
    Join-Path $root $AssemblyPath
}
$assemblyFullPath = Resolve-Path $assemblyCandidate
$outputFullPath = Join-Path $root $OutputPath
$outputDirectory = Split-Path $outputFullPath -Parent
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

dotnet run `
    --project (Join-Path $PSScriptRoot "Sigtran.NET.ApiSurface/Sigtran.NET.ApiSurface.csproj") `
    --configuration Release `
    -- `
    $assemblyFullPath `
    $outputFullPath
if ($LASTEXITCODE -ne 0) {
    throw "Public API surface generation failed with exit code $LASTEXITCODE."
}
