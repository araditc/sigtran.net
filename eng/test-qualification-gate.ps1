$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$temp = Join-Path $root 'artifacts/qualification/negative-gate'
New-Item -ItemType Directory -Force $temp | Out-Null
$manifest = Get-Content (Join-Path $root 'eng/release/stable-release.json') -Raw | ConvertFrom-Json
# Simulate a mistaken/manual promotion of every legacy flag while candidate evidence is absent.
foreach ($gate in $manifest.gates) { $gate.passed = $true; $gate.evidence = @('README.md') }
$manifest | ConvertTo-Json -Depth 20 | Set-Content (Join-Path $temp 'forged-legacy.json') -Encoding utf8
$runArgs = @('-NoProfile','-File', (Join-Path $PSScriptRoot 'evaluate-stable-release.ps1'),
    '-ManifestPath','artifacts/qualification/negative-gate/forged-legacy.json',
    '-JsonOutputPath','artifacts/qualification/negative-gate/decision.json',
    '-MarkdownOutputPath','artifacts/qualification/negative-gate/decision.md','-RequireGo')
& pwsh @runArgs *> (Join-Path $temp 'execution.log')
if ($LASTEXITCODE -eq 0) { throw 'Legacy passed flags bypassed candidate qualification.' }
$decision = Get-Content (Join-Path $temp 'decision.json') -Raw | ConvertFrom-Json
if ($decision.decision -ne 'NO-GO' -or @($decision.blockers).Count -ne 1 `
    -or $decision.candidateQualification.decision -ne 'NO-GO') {
    throw 'Forged legacy promotion did not produce the expected isolated candidate blocker.'
}
Write-Output 'PASS legacy flag promotion is rejected by the stable gate'
