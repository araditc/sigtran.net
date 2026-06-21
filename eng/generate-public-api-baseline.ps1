param(
    [string]$DocumentationPath = "src/Sigtran.NET/bin/Release/net10.0/Sigtran.NET.xml",
    [string]$OutputPath = "artifacts/api/Sigtran.NET-public-api.txt"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$documentationFullPath = Resolve-Path (Join-Path $root $DocumentationPath)
$outputFullPath = Join-Path $root $OutputPath
$outputDirectory = Split-Path $outputFullPath -Parent
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

[xml]$documentation = Get-Content $documentationFullPath
$members = $documentation.doc.members.member |
    ForEach-Object { $_.name } |
    Where-Object { $_ -match "^[TMPFE]:" } |
    Sort-Object -Unique

if ($members.Count -eq 0) {
    throw "No public API members were found in $documentationFullPath."
}

$members | Set-Content -Path $outputFullPath -Encoding UTF8
$baselineHash = (Get-FileHash -Path $outputFullPath -Algorithm SHA256).Hash.ToLowerInvariant()

[ordered]@{
    OutputPath = $OutputPath
    Sha256 = $baselineHash
    MemberCount = $members.Count
} | ConvertTo-Json -Depth 5
