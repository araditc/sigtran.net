$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "../..")
$evaluator = Join-Path $root "eng/evaluate-stable-release.ps1"
$fixtureRootRelative = "artifacts/stable-evaluator-tests"
$fixtureRoot = Join-Path $root $fixtureRootRelative

$requiredGateIds = @(
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

Remove-Item -LiteralPath $fixtureRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $fixtureRoot | Out-Null

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )
    if (-not $Condition) {
        throw $Message
    }
}

function Get-FixtureEvidencePath {
    param([string]$GateId)

    if ($GateId -eq "public-api-baseline") {
        return "eng/api/Sigtran.NET.1.0.public-api.txt"
    }

    return "docs/evidence/PHASE55_GITHUB_PROTECTION_20260724T102354Z.json"
}

function New-FixtureGates {
    param(
        [string]$TargetId = "",
        [object[]]$TargetEvidence = $null,
        [bool]$TargetPassed = $true,
        [bool]$TargetRequired = $true
    )

    $targetEvidenceWasSupplied = $PSBoundParameters.ContainsKey("TargetEvidence")
    $gates = @()
    foreach ($id in $requiredGateIds) {
        $gate = [ordered]@{
            id = $id
            title = "Fixture $id"
            required = $true
            passed = $true
            evidence = @(Get-FixtureEvidencePath -GateId $id)
            note = "Fixture gate requires retained evidence."
        }
        if ($id -eq $TargetId) {
            $gate.required = $TargetRequired
            $gate.passed = $TargetPassed
            if ($targetEvidenceWasSupplied) {
                $gate.evidence = $TargetEvidence
            }
        }
        $gates += $gate
    }
    return $gates
}

function Write-FixtureManifest {
    param(
        [string]$Name,
        [object[]]$Gates,
        [string]$Baseline = "eng/api/Sigtran.NET.1.0.public-api.txt",
        [string]$AcceptedAdditions = "eng/api/Sigtran.NET.1.0.accepted-additions.txt"
    )

    $relative = "$fixtureRootRelative/$Name.manifest.json"
    $full = Join-Path $root $relative
    [ordered]@{
        schemaVersion = 1
        packageId = "Sigtran.NET"
        version = "1.0.0"
        publicApiBaseline = $Baseline
        publicApiAcceptedAdditions = $AcceptedAdditions
        gates = $Gates
    } | ConvertTo-Json -Depth 10 |
        Set-Content -LiteralPath $full -Encoding utf8
    return $relative
}

function Invoke-Fixture {
    param(
        [string]$Name,
        [string]$ManifestPath
    )

    $jsonRelative = "$fixtureRootRelative/$Name.decision.json"
    $markdownRelative = "$fixtureRootRelative/$Name.decision.md"
    $arguments = @{
        ManifestPath = $ManifestPath
        ExpectedVersion = "1.0.0"
        JsonOutputPath = $jsonRelative
        MarkdownOutputPath = $markdownRelative
    }
    & $evaluator @arguments | Out-Null
    return Get-Content -LiteralPath (Join-Path $root $jsonRelative) -Raw |
        ConvertFrom-Json
}

function Assert-FixtureRejected {
    param(
        [string]$Name,
        [string]$ManifestPath,
        [string]$MessagePattern
    )

    $rejected = $false
    try {
        Invoke-Fixture -Name $Name -ManifestPath $ManifestPath | Out-Null
    }
    catch {
        if ($_.Exception.Message -match $MessagePattern) {
            $rejected = $true
        }
        else {
            throw
        }
    }
    Assert-True $rejected "Fixture '$Name' must be rejected with '$MessagePattern'."
}

try {
    $validManifest = Write-FixtureManifest -Name "valid" -Gates (New-FixtureGates)
    $validReport = Invoke-Fixture -Name "valid" -ManifestPath $validManifest
    Assert-True ($validReport.decision -eq "GO") "Valid retained evidence must produce GO."
    Assert-True ($validReport.blockers.Count -eq 0) "Valid fixture must have zero blockers."
    Assert-True ($validReport.gates.Count -eq $requiredGateIds.Count) "Every required gate must be represented."
    Assert-True ($validReport.gates[0].evidenceDeclared -eq $true) "Valid gate must declare evidence."
    Assert-True ($validReport.gates[0].evidenceComplete -eq $true) "Valid gate evidence must be complete."
    Assert-True ($validReport.gates[0].evidence[0].pathPolicyValid -eq $true) "Valid evidence path must stay inside the repository."

    $emptyManifest = Write-FixtureManifest -Name "empty" -Gates (
        New-FixtureGates -TargetId "operator-profile" -TargetEvidence @()
    )
    $emptyReport = Invoke-Fixture -Name "empty" -ManifestPath $emptyManifest
    $emptyGate = $emptyReport.gates | Where-Object { $_.id -eq "operator-profile" }
    Assert-True ($emptyReport.decision -eq "NO-GO") "Declared PASS with no evidence must be NO-GO."
    Assert-True ($emptyGate.declaredPassed -eq $true) "Fixture must retain declared PASS."
    Assert-True ($emptyGate.evidenceDeclared -eq $false) "Empty evidence must be explicit in report."
    Assert-True ($emptyGate.passed -eq $false) "Empty evidence must prevent gate PASS."
    Assert-True ($emptyReport.blockers.Count -eq 1) "Empty evidence fixture must have one blocker."

    $escapingManifest = Write-FixtureManifest -Name "escape" -Gates (
        New-FixtureGates -TargetId "kubernetes-sctp" -TargetEvidence @("../README.md")
    )
    $escapingReport = Invoke-Fixture -Name "escape" -ManifestPath $escapingManifest
    $escapingGate = $escapingReport.gates | Where-Object { $_.id -eq "kubernetes-sctp" }
    Assert-True ($escapingReport.decision -eq "NO-GO") "Repository-escaping evidence must be NO-GO."
    Assert-True ($escapingGate.evidence[0].pathPolicyValid -eq $false) "Escaping evidence path must be rejected."
    Assert-True ($escapingGate.evidence[0].present -eq $false) "Rejected evidence must never be treated as present."
    Assert-True ($escapingReport.blockers.Count -eq 1) "Escaping evidence fixture must have one blocker."

    $wrongScopeManifest = Write-FixtureManifest -Name "wrong-scope" -Gates (
        New-FixtureGates -TargetId "operator-profile" -TargetEvidence @("README.md")
    )
    $wrongScopeReport = Invoke-Fixture -Name "wrong-scope" -ManifestPath $wrongScopeManifest
    $wrongScopeGate = $wrongScopeReport.gates | Where-Object { $_.id -eq "operator-profile" }
    Assert-True ($wrongScopeReport.decision -eq "NO-GO") "Evidence outside governed roots must be NO-GO."
    Assert-True ($wrongScopeGate.evidence[0].pathPolicyValid -eq $false) "Wrong-scope evidence path must be rejected."
    Assert-True ($wrongScopeReport.blockers.Count -eq 1) "Wrong-scope evidence fixture must have one blocker."

    $canonicalEscapeManifest = Write-FixtureManifest -Name "canonical-escape" -Gates (
        New-FixtureGates -TargetId "multi-host-soak" -TargetEvidence @(
            "docs/evidence/../COMMERCIAL_READINESS_REPORT.md"
        )
    )
    $canonicalEscapeReport = Invoke-Fixture -Name "canonical-escape" -ManifestPath $canonicalEscapeManifest
    $canonicalEscapeGate = $canonicalEscapeReport.gates | Where-Object { $_.id -eq "multi-host-soak" }
    Assert-True ($canonicalEscapeReport.decision -eq "NO-GO") "Canonical path escape from governed evidence root must be NO-GO."
    Assert-True ($canonicalEscapeGate.evidence[0].canonicalPath -eq "docs/COMMERCIAL_READINESS_REPORT.md") "Canonical evidence path must be reported."
    Assert-True ($canonicalEscapeGate.evidence[0].pathPolicyValid -eq $false) "Canonical evidence root escape must be rejected."
    Assert-True ($canonicalEscapeReport.blockers.Count -eq 1) "Canonical root escape fixture must have one blocker."

    $baselineEscapeManifest = Write-FixtureManifest -Name "baseline-escape" -Gates (New-FixtureGates) -Baseline "../README.md"
    $baselineEscapeReport = Invoke-Fixture -Name "baseline-escape" -ManifestPath $baselineEscapeManifest
    Assert-True ($baselineEscapeReport.decision -eq "NO-GO") "Repository-escaping API baseline must be NO-GO."
    Assert-True ($baselineEscapeReport.publicApiBaseline.pathPolicyValid -eq $false) "API baseline path policy must be reported."
    Assert-True ($baselineEscapeReport.blockers.Count -eq 1) "Escaping baseline fixture must have one blocker."

    $missingAdditionsManifest = Write-FixtureManifest -Name "missing-additions" -Gates (New-FixtureGates) -AcceptedAdditions ""
    $missingAdditionsReport = Invoke-Fixture -Name "missing-additions" -ManifestPath $missingAdditionsManifest
    Assert-True ($missingAdditionsReport.decision -eq "NO-GO") "Stable accepted-additions path must be required."
    Assert-True ($missingAdditionsReport.publicApiBaseline.acceptedAdditionsPathPolicyValid -eq $false) "Missing accepted-additions path must be reported invalid."
    Assert-True ($missingAdditionsReport.blockers.Count -eq 1) "Missing accepted-additions fixture must have one blocker."

    $missingGateSet = @(
        New-FixtureGates | Where-Object { $_.id -ne "trusted-signing" }
    )
    $missingGateManifest = Write-FixtureManifest -Name "missing-required-gate" -Gates $missingGateSet
    Assert-FixtureRejected -Name "missing-required-gate" -ManifestPath $missingGateManifest -MessagePattern "Required stable release gate 'trusted-signing' is missing"

    $optionalGateManifest = Write-FixtureManifest -Name "optional-required-gate" -Gates (
        New-FixtureGates -TargetId "multi-host-soak" -TargetRequired $false
    )
    Assert-FixtureRejected -Name "optional-required-gate" -ManifestPath $optionalGateManifest -MessagePattern "cannot be optional"

    $duplicateGates = @(New-FixtureGates)
    $duplicateGates += [ordered]@{
        id = "trusted-signing"
        title = "Duplicate trusted signing"
        required = $true
        passed = $false
        evidence = @()
        note = "Duplicate gate must be rejected."
    }
    $duplicateManifest = Write-FixtureManifest -Name "duplicate" -Gates $duplicateGates
    Assert-FixtureRejected -Name "duplicate" -ManifestPath $duplicateManifest -MessagePattern "duplicated"

    Write-Host "PASS stable release evaluator fail-closed evidence policy"
}
finally {
    Remove-Item -LiteralPath $fixtureRoot -Recurse -Force -ErrorAction SilentlyContinue
}
