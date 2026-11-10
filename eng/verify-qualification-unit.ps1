param(
    [Parameter(Mandatory)][ValidateRange(1,10)][int]$Unit,
    [ValidateRange(56,65)][int]$Phase = 56
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    # Keep the original baseline logs in place and separate later phase results.
    $unitRoot = if ($Phase -eq 56) { 'units' } else { "phase-$Phase/units" }
    $output = "artifacts/qualification/$unitRoot/$('{0:d2}' -f $Unit)"
    New-Item -ItemType Directory -Force $output | Out-Null
    # Every unit retains its own command output; a failed native process stops promotion.
    $commands = @(
        @('build', @('build','src/Sigtran.NET.sln','-c','Release','--nologo','-v:q','-m:1','/nodeReuse:false')),
        @('sdk-tests', @('run','--project','src/Sigtran.NET.Tests','-c','Release','--no-build')),
        @('qualification-tests', @('run','--project','eng/Sigtran.NET.Qualification.Tests','-c','Release','--no-build')),
        @('pack', @('pack','src/Sigtran.NET/Sigtran.NET.csproj','-c','Release','--no-build','-o',"$output/package"))
    )
    foreach ($command in $commands) {
        & dotnet @($command[1]) *> "$output/$($command[0]).log"
        if ($LASTEXITCODE -ne 0) { Get-Content "$output/$($command[0]).log" -Tail 25; throw "$($command[0]) failed" }
    }
    Write-Output "Unit $Unit build/test/pack passed; logs: $output"
} finally { Pop-Location }
