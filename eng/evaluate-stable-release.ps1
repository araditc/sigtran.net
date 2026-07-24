param(
    [string]$ManifestPath = "eng/release/stable-release.json",
    [string]$JsonOutputPath = "artifacts/release-evidence/stable-release-decision.json",
    [string]$MarkdownOutputPath = "artifacts/release-evidence/stable-release-decision.md",
    [string]$ExpectedVersion = "",
    [switch]$RequireGo
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$manifestFullPath = Resolve-Path (Join-Path $root $ManifestPath)
$jsonOutputFullPath = Join-Path $root $JsonOutputPath
$markdownOutputFullPath = Join-Path $root $MarkdownOutputPath
New-Item -ItemType Directory -Force -Path (
    Split-Path $jsonOutputFullPath -Parent
) | Out-Null
New-Item -ItemType Directory -Force -Path (
    Split-Path $markdownOutputFullPath -Parent
) | Out-Null

$manifest = Get-Content -LiteralPath $manifestFullPath -Raw |
    ConvertFrom-Json
if ($manifest.schemaVersion -ne 1) {
    throw "Unsupported stable release manifest schema version."
}
if ($manifest.version -notmatch "^[0-9]+\.[0-9]+\.[0-9]+$") {
    throw "Stable release version '$($manifest.version)' is not stable SemVer."
}
if (-not [string]::IsNullOrWhiteSpace($ExpectedVersion) -and
    $manifest.version -ne $ExpectedVersion) {
    throw "Stable release manifest version '$($manifest.version)' does not match requested version '$ExpectedVersion'."
}

$baselineFullPath = Join-Path $root $manifest.publicApiBaseline
$baselinePresent = Test-Path -LiteralPath $baselineFullPath -PathType Leaf
$gateResults = @()
$blockers = @()

foreach ($gate in $manifest.gates) {
    $evidenceResults = @()
    foreach ($evidencePath in @($gate.evidence)) {
        $evidenceFullPath = Join-Path $root $evidencePath
        $present = Test-Path -LiteralPath $evidenceFullPath -PathType Leaf
        $sha256 = if ($present) {
            (
                Get-FileHash -LiteralPath $evidenceFullPath -Algorithm SHA256
            ).Hash.ToLowerInvariant()
        }
        else {
            $null
        }
        $evidenceResults += [ordered]@{
            path = $evidencePath
            present = $present
            sha256 = $sha256
        }
    }

    $evidenceComplete = @(
        $evidenceResults | Where-Object { -not $_.present }
    ).Count -eq 0
    $passed = [bool]$gate.passed -and $evidenceComplete
    $gateResults += [ordered]@{
        id = $gate.id
        title = $gate.title
        required = [bool]$gate.required
        declaredPassed = [bool]$gate.passed
        evidenceComplete = $evidenceComplete
        passed = $passed
        note = $gate.note
        evidence = $evidenceResults
    }

    if ([bool]$gate.required -and -not $passed) {
        $blockers += "$($gate.id): $($gate.note)"
    }
}

if (-not $baselinePresent) {
    $blockers += "public-api-baseline: $($manifest.publicApiBaseline) is missing."
}

$decision = if ($blockers.Count -eq 0) { "GO" } else { "NO-GO" }
$commit = (git -C $root rev-parse HEAD).Trim()
$report = [ordered]@{
    schemaVersion = 1
    packageId = $manifest.packageId
    version = $manifest.version
    sourceCommit = $commit
    evaluatedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    publicApiBaseline = [ordered]@{
        path = $manifest.publicApiBaseline
        present = $baselinePresent
        sha256 = if ($baselinePresent) {
            (
                Get-FileHash -LiteralPath $baselineFullPath -Algorithm SHA256
            ).Hash.ToLowerInvariant()
        }
        else {
            $null
        }
    }
    decision = $decision
    blockers = $blockers
    gates = $gateResults
}
$report | ConvertTo-Json -Depth 20 |
    Set-Content -LiteralPath $jsonOutputFullPath -Encoding utf8

$markdown = @(
    "# Stable Release Decision"
    ""
    "- Package: ``$($manifest.packageId)``"
    "- Version: ``$($manifest.version)``"
    "- Source commit: ``$commit``"
    "- Decision: **$decision**"
    "- Required blockers: $($blockers.Count)"
    ""
    "## Gates"
    ""
    "| Gate | Required | Passed | Evidence complete |"
    "| --- | --- | --- | --- |"
)
foreach ($gate in $gateResults) {
    $markdown += "| $($gate.id) | $($gate.required) | $($gate.passed) | $($gate.evidenceComplete) |"
}
$markdown += @(
    ""
    "## Blockers"
    ""
)
if ($blockers.Count -eq 0) {
    $markdown += "None."
}
else {
    $markdown += $blockers | ForEach-Object { "- $_" }
}
$markdown | Set-Content -LiteralPath $markdownOutputFullPath -Encoding utf8

[ordered]@{
    Decision = $decision
    BlockerCount = $blockers.Count
    JsonOutputPath = $JsonOutputPath
    MarkdownOutputPath = $MarkdownOutputPath
} | ConvertTo-Json -Depth 5

if ($RequireGo -and $decision -ne "GO") {
    throw "Stable release is blocked by $($blockers.Count) required gate(s)."
}
