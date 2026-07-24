param(
    [string]$BaselinePath = "eng/api/Sigtran.NET.1.0.public-api.txt",
    [string]$CurrentPath = "artifacts/api/Sigtran.NET-current.public-api.txt",
    [string]$OutputPath = "artifacts/api/Sigtran.NET.api-diff.md",
    [switch]$FailOnBreaking,
    [switch]$FailOnAnyChange
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$baselineFullPath = Resolve-Path (Join-Path $root $BaselinePath)
$currentFullPath = Resolve-Path (Join-Path $root $CurrentPath)
$outputFullPath = Join-Path $root $OutputPath
$outputDirectory = Split-Path $outputFullPath -Parent
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

$baseline = @(
    Get-Content -LiteralPath $baselineFullPath |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Sort-Object -Unique
)
$current = @(
    Get-Content -LiteralPath $currentFullPath |
        Where-Object { $_ -match "^[TCMPFE]:" } |
        Sort-Object -Unique
)

$removed = @(
    Compare-Object -ReferenceObject $baseline -DifferenceObject $current |
        Where-Object SideIndicator -eq "<=" |
        ForEach-Object InputObject
)
$added = @(
    Compare-Object -ReferenceObject $baseline -DifferenceObject $current |
        Where-Object SideIndicator -eq "=>" |
        ForEach-Object InputObject
)
$breaking = $removed.Count -gt 0

$lines = @(
    "# Sigtran.NET Public API Diff"
    ""
    "- Baseline: ``$BaselinePath``"
    "- Current surface: ``$CurrentPath``"
    "- Baseline members: $($baseline.Count)"
    "- Current members: $($current.Count)"
    "- Added members: $($added.Count)"
    "- Removed members: $($removed.Count)"
    "- Breaking change detected: $($breaking.ToString().ToLowerInvariant())"
    ""
    "## Removed"
    ""
)
if ($removed.Count -eq 0) {
    $lines += "None."
}
else {
    $lines += $removed | ForEach-Object { "- ``$_``" }
}

$lines += @(
    ""
    "## Added"
    ""
)
if ($added.Count -eq 0) {
    $lines += "None."
}
else {
    $lines += $added | ForEach-Object { "- ``$_``" }
}

$lines | Set-Content -LiteralPath $outputFullPath -Encoding utf8
$outputHash = (
    Get-FileHash -LiteralPath $outputFullPath -Algorithm SHA256
).Hash.ToLowerInvariant()

[ordered]@{
    BaselinePath = $BaselinePath
    CurrentPath = $CurrentPath
    OutputPath = $OutputPath
    BaselineMemberCount = $baseline.Count
    CurrentMemberCount = $current.Count
    AddedMemberCount = $added.Count
    RemovedMemberCount = $removed.Count
    BreakingChange = $breaking
    Sha256 = $outputHash
} | ConvertTo-Json -Depth 5

if ($breaking -and $FailOnBreaking) {
    throw "Public API compatibility check failed with $($removed.Count) removed member(s)."
}
if (($removed.Count -gt 0 -or $added.Count -gt 0) -and $FailOnAnyChange) {
    throw "Frozen public API check failed with $($removed.Count) removed and $($added.Count) added member(s)."
}
