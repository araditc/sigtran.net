$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "../..")
$evaluator = Join-Path $root "eng/evaluate-stable-release.ps1"
$fixtureRootRelative = "artifacts/stable-evaluator-tests"
$fixtureRoot = Join-Path $root $fixtureRootRelative

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
    & $evaluator         -ManifestPath $ManifestPath         -ExpectedVersion "1.0.0"         -JsonOutputPath $jsonRelative         -MarkdownOutputPath $markdownRelative |
        Out-Null
    return Get-Content -LiteralPath (Join-Path $root $jsonRelative) -Raw |
        ConvertFrom-Json
}

try {
    $validGate = [ordered]@{
        id = "fixture-gate"
        title = "Fixture gate"
        required = $true
        passed = $true
        evidence = @("README.md")
        note = "Fixture gate requires retained evidence."
    }

    $validManifest = Write-FixtureManifest -Name "valid" -Gates @($validGate)
    $validReport = Invoke-Fixture -Name "valid" -ManifestPath $validManifest
    Assert-True ($validReport.decision -eq "GO") "Valid retained evidence must produce GO."
    Assert-True ($validReport.gates[0].evidenceDeclared -eq $true) "Valid gate must declare evidence."
    Assert-True ($validReport.gates[0].evidenceComplete -eq $true) "Valid gate evidence must be complete."
    Assert-True ($validReport.gates[0].evidence[0].pathPolicyValid -eq $true) "Valid evidence path must stay inside the repository."

    $emptyEvidenceGate = [ordered]@{
        id = "empty-evidence"
        title = "Empty evidence"
        required = $true
        passed = $true
        evidence = @()
        note = "Administrative PASS without evidence must fail closed."
    }
    $emptyManifest = Write-FixtureManifest -Name "empty" -Gates @($emptyEvidenceGate)
    $emptyReport = Invoke-Fixture -Name "empty" -ManifestPath $emptyManifest
    Assert-True ($emptyReport.decision -eq "NO-GO") "Declared PASS with no evidence must be NO-GO."
    Assert-True ($emptyReport.gates[0].declaredPassed -eq $true) "Fixture must retain declared PASS."
    Assert-True ($emptyReport.gates[0].evidenceDeclared -eq $false) "Empty evidence must be explicit in report."
    Assert-True ($emptyReport.gates[0].passed -eq $false) "Empty evidence must prevent gate PASS."
    Assert-True ($emptyReport.blockers.Count -eq 1) "Empty evidence fixture must have one blocker."

    $escapingGate = [ordered]@{
        id = "escaping-evidence"
        title = "Escaping evidence"
        required = $true
        passed = $true
        evidence = @("../README.md")
        note = "Evidence outside the repository must fail closed."
    }
    $escapingManifest = Write-FixtureManifest -Name "escape" -Gates @($escapingGate)
    $escapingReport = Invoke-Fixture -Name "escape" -ManifestPath $escapingManifest
    Assert-True ($escapingReport.decision -eq "NO-GO") "Repository-escaping evidence must be NO-GO."
    Assert-True ($escapingReport.gates[0].evidence[0].pathPolicyValid -eq $false) "Escaping evidence path must be rejected."
    Assert-True ($escapingReport.gates[0].evidence[0].present -eq $false) "Rejected evidence must never be treated as present."

    $baselineEscapeManifest = Write-FixtureManifest         -Name "baseline-escape"         -Gates @($validGate)         -Baseline "../README.md"
    $baselineEscapeReport = Invoke-Fixture         -Name "baseline-escape"         -ManifestPath $baselineEscapeManifest
    Assert-True ($baselineEscapeReport.decision -eq "NO-GO") "Repository-escaping API baseline must be NO-GO."
    Assert-True ($baselineEscapeReport.publicApiBaseline.pathPolicyValid -eq $false) "API baseline path policy must be reported."

    $duplicateGateA = [ordered]@{
        id = "duplicate"
        title = "Duplicate A"
        required = $false
        passed = $false
        evidence = @()
        note = "First duplicate."
    }
    $duplicateGateB = [ordered]@{
        id = "duplicate"
        title = "Duplicate B"
        required = $false
        passed = $false
        evidence = @()
        note = "Second duplicate."
    }
    $duplicateManifest = Write-FixtureManifest         -Name "duplicate"         -Gates @($duplicateGateA, $duplicateGateB)
    $duplicateRejected = $false
    try {
        Invoke-Fixture -Name "duplicate" -ManifestPath $duplicateManifest | Out-Null
    }
    catch {
        if ($_.Exception.Message -match "duplicated") {
            $duplicateRejected = $true
        }
        else {
            throw
        }
    }
    Assert-True $duplicateRejected "Duplicate gate IDs must be rejected."

    Write-Host "PASS stable release evaluator fail-closed evidence policy"
}
finally {
    Remove-Item -LiteralPath $fixtureRoot -Recurse -Force -ErrorAction SilentlyContinue
}
