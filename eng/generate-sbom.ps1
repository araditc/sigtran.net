param(
    [string]$ProjectPath = "src/Sigtran.NET/Sigtran.NET.csproj",
    [string]$PackageDirectory = "src/Sigtran.NET/bin/Release",
    [string]$OutputPath = "artifacts/sbom/Sigtran.NET.spdx.json",
    [string]$CreatedUtc = "2026-06-20T00:00:00Z"
)

$ErrorActionPreference = "Stop"

function Get-RelativePathForSbom {
    param(
        [string]$BasePath,
        [string]$TargetPath
    )

    $baseUri = [System.Uri]::new((Resolve-Path $BasePath).Path + [System.IO.Path]::DirectorySeparatorChar)
    $targetUri = [System.Uri]::new((Resolve-Path $TargetPath).Path)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace("/", [System.IO.Path]::DirectorySeparatorChar)
}

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectFullPath = Resolve-Path (Join-Path $root $ProjectPath)
$packageFullDirectory = Resolve-Path (Join-Path $root $PackageDirectory)
$outputFullPath = Join-Path $root $OutputPath
$outputDirectory = Split-Path $outputFullPath -Parent
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

[xml]$project = Get-Content $projectFullPath
$properties = $project.Project.PropertyGroup | Select-Object -First 1
$packageId = [string]$properties.PackageId
if ([string]::IsNullOrWhiteSpace($packageId)) {
    $packageId = [System.IO.Path]::GetFileNameWithoutExtension($projectFullPath)
}

$targetFramework = [string]$properties.TargetFramework
$license = [string]$properties.PackageLicenseExpression
$repositoryUrl = [string]$properties.RepositoryUrl
$description = [string]$properties.Description
$version = [string]$properties.Version
if ([string]::IsNullOrWhiteSpace($version)) {
    $version = "1.0.0"
}

$packageFiles = @(
    Get-ChildItem -Path $packageFullDirectory -File -Filter "*.nupkg"
    Get-ChildItem -Path $packageFullDirectory -File -Filter "*.snupkg"
) | Sort-Object Name

if ($packageFiles.Count -eq 0) {
    throw "No NuGet package files found in $packageFullDirectory. Run dotnet pack first."
}

$documentNamespace = "https://github.com/araditc/Sigtran.NET/sbom/$packageId/$version"

$fileEntries = @()
foreach ($packageFile in $packageFiles) {
    $sha256 = (Get-FileHash -Path $packageFile.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $fileEntries += [ordered]@{
        SPDXID = "SPDXRef-File-$($packageFile.BaseName.Replace('.', '-'))"
        fileName = Get-RelativePathForSbom -BasePath $root -TargetPath $packageFile.FullName
        checksums = @(
            [ordered]@{
                algorithm = "SHA256"
                checksumValue = $sha256
            }
        )
        licenseConcluded = $license
        copyrightText = "NOASSERTION"
    }
}

$sbom = [ordered]@{
    spdxVersion = "SPDX-2.3"
    dataLicense = "CC0-1.0"
    SPDXID = "SPDXRef-DOCUMENT"
    name = "$packageId SBOM"
    documentNamespace = $documentNamespace
    creationInfo = [ordered]@{
        created = $CreatedUtc
        creators = @(
            "Tool: Sigtran.NET eng/generate-sbom.ps1",
            "Organization: SIGTRAN.NET contributors"
        )
    }
    packages = @(
        [ordered]@{
            SPDXID = "SPDXRef-Package-$($packageId.Replace('.', '-'))"
            name = $packageId
            versionInfo = $version
            downloadLocation = $repositoryUrl
            filesAnalyzed = $true
            licenseConcluded = $license
            licenseDeclared = $license
            description = $description
            externalRefs = @(
                [ordered]@{
                    referenceCategory = "PACKAGE-MANAGER"
                    referenceType = "purl"
                    referenceLocator = "pkg:nuget/$packageId@$version"
                }
            )
            supplier = "Organization: SIGTRAN.NET contributors"
            primaryPackagePurpose = "LIBRARY"
            summary = "Target framework: $targetFramework"
        }
    )
    files = $fileEntries
    relationships = @(
        [ordered]@{
            spdxElementId = "SPDXRef-DOCUMENT"
            relationshipType = "DESCRIBES"
            relatedSpdxElement = "SPDXRef-Package-$($packageId.Replace('.', '-'))"
        }
    )
}

$sbom | ConvertTo-Json -Depth 20 | Set-Content -Path $outputFullPath -Encoding UTF8
$sbomHash = (Get-FileHash -Path $outputFullPath -Algorithm SHA256).Hash.ToLowerInvariant()

[ordered]@{
    OutputPath = Get-RelativePathForSbom -BasePath $root -TargetPath $outputFullPath
    Sha256 = $sbomHash
    PackageFileCount = $packageFiles.Count
} | ConvertTo-Json -Depth 5
