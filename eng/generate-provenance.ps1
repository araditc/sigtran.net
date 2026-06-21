param(
    [string]$PackagePath = "artifacts/signing/Sigtran.NET.1.0.0.nupkg",
    [string]$SbomPath = "artifacts/sbom/Sigtran.NET.spdx.json",
    [string]$OutputPath = "artifacts/provenance/Sigtran.NET.provenance.json"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$packageFullPath = Resolve-Path (Join-Path $root $PackagePath)
$sbomFullPath = Resolve-Path (Join-Path $root $SbomPath)
$outputFullPath = Join-Path $root $OutputPath
$outputDirectory = Split-Path $outputFullPath -Parent
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

$commit = (git -C $root rev-parse HEAD).Trim()
$branch = (git -C $root rev-parse --abbrev-ref HEAD).Trim()
$remote = (git -C $root config --get remote.origin.url).Trim()
$packageHash = (Get-FileHash -Path $packageFullPath -Algorithm SHA256).Hash.ToLowerInvariant()
$sbomHash = (Get-FileHash -Path $sbomFullPath -Algorithm SHA256).Hash.ToLowerInvariant()

$provenance = [ordered]@{
    subject = [ordered]@{
        name = "Sigtran.NET.1.0.0.nupkg"
        sha256 = $packageHash
    }
    buildType = "https://github.com/araditc/Sigtran.NET/actions/workflows/release.yml"
    builder = [ordered]@{
        id = "Sigtran.NET-commercial-release"
    }
    invocation = [ordered]@{
        configSource = [ordered]@{
            uri = $remote
            digest = [ordered]@{
                sha1 = $commit
            }
            entryPoint = $branch
        }
    }
    materials = @(
        [ordered]@{
            uri = $remote
            digest = [ordered]@{
                sha1 = $commit
            }
        },
        [ordered]@{
            uri = $SbomPath
            digest = [ordered]@{
                sha256 = $sbomHash
            }
        }
    )
    metadata = [ordered]@{
        completeness = [ordered]@{
            parameters = $true
            environment = $true
            materials = $true
        }
        reproducible = $false
    }
}

$provenance | ConvertTo-Json -Depth 20 | Set-Content -Path $outputFullPath -Encoding UTF8
$provenanceHash = (Get-FileHash -Path $outputFullPath -Algorithm SHA256).Hash.ToLowerInvariant()

[ordered]@{
    OutputPath = $OutputPath
    Sha256 = $provenanceHash
    SourceCommit = $commit
    PackageSha256 = $packageHash
    SbomSha256 = $sbomHash
} | ConvertTo-Json -Depth 5
