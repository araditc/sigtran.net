param(
    [string]$ManifestPath = "eng/release/stable-release.json",
    [string]$JsonOutputPath = "artifacts/release-evidence/stable-release-decision.json",
    [string]$MarkdownOutputPath = "artifacts/release-evidence/stable-release-decision.md",
    [string]$ExpectedVersion = "",
    [switch]$RequireGo
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$rootPath = [IO.Path]::GetFullPath($root.Path)
$rootPrefix = $rootPath + [IO.Path]::DirectorySeparatorChar

function Resolve-RepositoryRelativePath {
    param([string]$RelativePath)

    if ([string]::IsNullOrWhiteSpace($RelativePath) -or
        [IO.Path]::IsPathRooted($RelativePath)) {
        return $null
    }

    try {
        $candidate = [IO.Path]::GetFullPath(
            (Join-Path $rootPath $RelativePath)
        )
    }
    catch {
        return $null
    }

    if (-not $candidate.StartsWith(
        $rootPrefix,
        [StringComparison]::OrdinalIgnoreCase
    )) {
        return $null
    }

    return $candidate
}

function Test-RepositoryPathHasNoLinks {
    param([string]$FullPath)

    if ([string]::IsNullOrWhiteSpace($FullPath)) {
        return $false
    }

    $candidate = [IO.Path]::GetFullPath($FullPath)
    if ($candidate -ne $rootPath -and -not $candidate.StartsWith(
        $rootPrefix,
        [StringComparison]::OrdinalIgnoreCase
    )) {
        return $false
    }

    $relative = [IO.Path]::GetRelativePath($rootPath, $candidate)
    if ($relative -eq ".") {
        return $true
    }

    $current = $rootPath
    foreach ($segment in $relative.Split(
        [IO.Path]::DirectorySeparatorChar,
        [StringSplitOptions]::RemoveEmptyEntries
    )) {
        $current = Join-Path $current $segment
        if (-not (Test-Path -LiteralPath $current)) {
            break
        }

        $item = Get-Item -LiteralPath $current -Force
        $linkTypeProperty = $item.PSObject.Properties["LinkType"]
        if ($null -ne $linkTypeProperty -and
            -not [string]::IsNullOrWhiteSpace([string]$item.LinkType)) {
            return $false
        }
    }

    return $true
}

function Test-RegularRepositoryFile {
    param([string]$FullPath)

    if ([string]::IsNullOrWhiteSpace($FullPath) -or
        -not (Test-RepositoryPathHasNoLinks -FullPath $FullPath) -or
        -not (Test-Path -LiteralPath $FullPath -PathType Leaf)) {
        return $false
    }

    return $true
}

function Test-GateEvidencePathPolicy {
    param(
        [string]$GateId,
        [string]$EvidencePath
    )

    if ([string]::IsNullOrWhiteSpace($EvidencePath)) {
        return $false
    }

    $normalized = $EvidencePath.Replace("\\", "/")
    if ($GateId -eq "public-api-baseline") {
        return $normalized.StartsWith(
            "eng/api/",
            [StringComparison]::Ordinal
        )
    }

    return $normalized.StartsWith(
        "docs/evidence/",
        [StringComparison]::Ordinal
    )
}

$manifestFullPath = Resolve-RepositoryRelativePath -RelativePath $ManifestPath
if ($null -eq $manifestFullPath -or
    -not (Test-RegularRepositoryFile -FullPath $manifestFullPath)) {
    throw "Stable release manifest must be a regular file inside the repository."
}

$jsonOutputFullPath = Resolve-RepositoryRelativePath -RelativePath $JsonOutputPath
$markdownOutputFullPath = Resolve-RepositoryRelativePath -RelativePath $MarkdownOutputPath
if ($null -eq $jsonOutputFullPath -or $null -eq $markdownOutputFullPath) {
    throw "Stable release output paths must stay inside the repository."
}
if (-not (Test-RepositoryPathHasNoLinks -FullPath $jsonOutputFullPath) -or
    -not (Test-RepositoryPathHasNoLinks -FullPath $markdownOutputFullPath)) {
    throw "Stable release output paths must not traverse symbolic links or junctions."
}

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

$requiredStableGateIds = @(
    "native-linux-sctp",
    "external-m3ua",
    "full-stack-traffic",
    "independent-m2pa",
    "operator-profile",
    "capacity-target",
    "multi-host-soak",
    "operations-runtime",
    "kubernetes-sctp",
    "trusted-signing",
    "public-api-baseline",
    "protected-publication"
)

$baselinePath = [string]$manifest.publicApiBaseline
$baselineFullPath = Resolve-RepositoryRelativePath -RelativePath $baselinePath
$baselinePathValid = $null -ne $baselineFullPath
$baselinePresent = $baselinePathValid -and
    (Test-RegularRepositoryFile -FullPath $baselineFullPath)

$acceptedAdditionsPath = if (
    $manifest.PSObject.Properties.Name -contains "publicApiAcceptedAdditions"
) {
    [string]$manifest.publicApiAcceptedAdditions
}
else {
    ""
}
$acceptedAdditionsFullPath = if (
    -not [string]::IsNullOrWhiteSpace($acceptedAdditionsPath)
) {
    Resolve-RepositoryRelativePath -RelativePath $acceptedAdditionsPath
}
else {
    $null
}
$acceptedAdditionsPathValid = (
    -not [string]::IsNullOrWhiteSpace($acceptedAdditionsPath)
) -and ($null -ne $acceptedAdditionsFullPath)
$acceptedAdditionsPresent = $acceptedAdditionsPathValid -and
    (Test-RegularRepositoryFile -FullPath $acceptedAdditionsFullPath)

$gateResults = @()
$blockers = @()
$seenGateIds = [System.Collections.Generic.HashSet[string]]::new(
    [StringComparer]::Ordinal
)

foreach ($gate in $manifest.gates) {
    $gateId = [string]$gate.id
    if ([string]::IsNullOrWhiteSpace($gateId)) {
        throw "Stable release gate id is required."
    }
    if (-not $seenGateIds.Add($gateId)) {
        throw "Stable release gate id '$gateId' is duplicated."
    }

    $evidencePaths = @($gate.evidence)
    $evidenceDeclared = $evidencePaths.Count -gt 0
    $evidenceResults = @()

    foreach ($evidenceValue in $evidencePaths) {
        $evidencePath = [string]$evidenceValue
        $evidenceFullPath = Resolve-RepositoryRelativePath -RelativePath $evidencePath
        $repositoryPathValid = $null -ne $evidenceFullPath
        $canonicalEvidencePath = if ($repositoryPathValid) {
            [IO.Path]::GetRelativePath(
                $rootPath,
                $evidenceFullPath
            ).Replace(
                [IO.Path]::DirectorySeparatorChar,
                [char]'/'
            )
        }
        else {
            ""
        }
        $gatePathValid = $repositoryPathValid -and (
            Test-GateEvidencePathPolicy -GateId $gateId -EvidencePath $canonicalEvidencePath
        )
        $pathPolicyValid = $repositoryPathValid -and $gatePathValid
        $present = $pathPolicyValid -and
            (Test-RegularRepositoryFile -FullPath $evidenceFullPath)
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
            canonicalPath = $canonicalEvidencePath
            pathPolicyValid = $pathPolicyValid
            present = $present
            sha256 = $sha256
        }
    }

    $evidenceComplete = $evidenceDeclared -and @(
        $evidenceResults |
            Where-Object { -not $_.pathPolicyValid -or -not $_.present }
    ).Count -eq 0
    $declaredPassed = [bool]$gate.passed
    $passed = $declaredPassed -and $evidenceComplete

    $evidencePolicyIssue = if (-not $evidenceDeclared) {
        "no retained evidence is declared"
    }
    elseif (@(
        $evidenceResults | Where-Object { -not $_.pathPolicyValid }
    ).Count -gt 0) {
        "one or more evidence paths escape the repository or are invalid"
    }
    elseif (@(
        $evidenceResults | Where-Object { -not $_.present }
    ).Count -gt 0) {
        "one or more evidence files are missing, linked, or not regular files"
    }
    else {
        $null
    }

    $gateResults += [ordered]@{
        id = $gateId
        title = $gate.title
        required = [bool]$gate.required
        declaredPassed = $declaredPassed
        evidenceDeclared = $evidenceDeclared
        evidenceComplete = $evidenceComplete
        passed = $passed
        evidencePolicyIssue = $evidencePolicyIssue
        note = $gate.note
        evidence = $evidenceResults
    }

    if ([bool]$gate.required -and -not $passed) {
        $blocker = "${gateId}: $($gate.note)"
        if ($declaredPassed -and $null -ne $evidencePolicyIssue) {
            $blocker += " Evidence policy: $evidencePolicyIssue."
        }
        $blockers += $blocker
    }
}

foreach ($requiredGateId in $requiredStableGateIds) {
    $requiredGateMatches = @(
        $gateResults | Where-Object { $_.id -eq $requiredGateId }
    )
    if ($requiredGateMatches.Count -ne 1) {
        throw "Required stable release gate '$requiredGateId' is missing."
    }
    if (-not [bool]$requiredGateMatches[0].required) {
        throw "Required stable release gate '$requiredGateId' cannot be optional."
    }
}

if (-not $baselinePathValid) {
    $blockers += "public-api-baseline: $baselinePath is outside the repository or invalid."
}
elseif (-not $baselinePresent) {
    $blockers += "public-api-baseline: $baselinePath is missing, linked, or not a regular file."
}

if ([string]::IsNullOrWhiteSpace($acceptedAdditionsPath)) {
    $blockers += "public-api-baseline: accepted-additions path is required."
}
elseif (-not $acceptedAdditionsPathValid) {
    $blockers += "public-api-baseline: $acceptedAdditionsPath is outside the repository or invalid."
}
elseif (-not $acceptedAdditionsPresent) {
    $blockers += "public-api-baseline: $acceptedAdditionsPath is missing, linked, or not a regular file."
}

$decision = if ($blockers.Count -eq 0) { "GO" } else { "NO-GO" }
$commit = (git -C $rootPath rev-parse HEAD).Trim()
$report = [ordered]@{
    schemaVersion = 1
    packageId = $manifest.packageId
    version = $manifest.version
    sourceCommit = $commit
    evaluatedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    publicApiBaseline = [ordered]@{
        path = $baselinePath
        pathPolicyValid = $baselinePathValid
        present = $baselinePresent
        sha256 = if ($baselinePresent) {
            (
                Get-FileHash -LiteralPath $baselineFullPath -Algorithm SHA256
            ).Hash.ToLowerInvariant()
        }
        else {
            $null
        }
        acceptedAdditionsPath = $acceptedAdditionsPath
        acceptedAdditionsPathPolicyValid = $acceptedAdditionsPathValid
        acceptedAdditionsPresent = $acceptedAdditionsPresent
        acceptedAdditionsSha256 = if ($acceptedAdditionsPresent -and $null -ne $acceptedAdditionsFullPath) {
            (
                Get-FileHash -LiteralPath $acceptedAdditionsFullPath -Algorithm SHA256
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
