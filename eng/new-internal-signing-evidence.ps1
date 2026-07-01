<#
.SYNOPSIS
Creates internal RC package-signing evidence for SIGTRAN.NET.

.DESCRIPTION
This runner creates a short-lived self-signed code-signing certificate, signs a
release-candidate NuGet package with a timestamp authority, verifies the signed
package, and writes a digest-covered evidence JSON file. The generated private
key material is deleted after signing. The public certificate can optionally be
trusted in the current user's root store so internal RC verification can run
without weakening the machine-wide trust policy.

This script is intended for internal commercial RC dry-runs. Public or stable
commercial releases must use the organization's approved trusted certificate
and protected release environment.
#>
param(
    [string]$Version = "1.0.0-rc.1",
    [string]$ProjectPath = "src/Sigtran.NET/Sigtran.NET.csproj",
    [string]$ArtifactRoot = "artifacts/internal-signing",
    [string]$TimestampAuthority = "http://timestamp.sectigo.com",
    [string]$CertificateSubject = "CN=Sigtran.NET Internal Production RC Signing, O=SIGTRAN.NET contributors",
    [switch]$TrustCurrentUserRoot
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$runId = [DateTimeOffset]::UtcNow.ToString("yyyyMMddTHHmmssZ")
$runRoot = Join-Path (Join-Path $root $ArtifactRoot) $runId
$packageSource = Join-Path $root "src/Sigtran.NET/bin/Release/Sigtran.NET.$Version.nupkg"
$signedPackage = Join-Path $runRoot "Sigtran.NET.$Version.nupkg"
$publicCertificatePath = Join-Path $runRoot "sigtran-internal-rc-signing.cer"
$pfxPath = Join-Path $runRoot "sigtran-internal-rc-signing.pfx"
$signLogPath = Join-Path $runRoot "sign-package.log"
$verifyLogPath = Join-Path $runRoot "verify-package.log"
$certificateInfoPath = Join-Path $runRoot "certificate-public-info.json"
$evidencePath = Join-Path $runRoot "internal-signing-evidence.json"

New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

function Invoke-NativeCommand {
    param(
        [string]$FilePath,
        [string[]]$Arguments,
        [string]$LogPath
    )

    & $FilePath @Arguments *> $LogPath
    return $LASTEXITCODE
}

function Get-RelativePath {
    param([string]$Path)

    $rootUri = [System.Uri]::new((Resolve-Path $root).Path + [System.IO.Path]::DirectorySeparatorChar)
    $targetUri = [System.Uri]::new((Resolve-Path $Path).Path)
    return [System.Uri]::UnescapeDataString($rootUri.MakeRelativeUri($targetUri).ToString()).Replace("\", "/")
}

function Get-Sha256 {
    param([string]$Path)

    return (Get-FileHash -Path $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

$certificate = $null
$passwordPlainText = [Guid]::NewGuid().ToString("N") + [Guid]::NewGuid().ToString("N")
$password = ConvertTo-SecureString -String $passwordPlainText -Force -AsPlainText
$signExitCode = -1
$verifyExitCode = -1

try {
    dotnet pack (Join-Path $root $ProjectPath) -c Release /p:Version=$Version
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet pack failed with exit code $LASTEXITCODE."
    }

    Copy-Item -Force -Path $packageSource -Destination $signedPackage

    $certificate = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $CertificateSubject `
        -CertStoreLocation Cert:\CurrentUser\My `
        -KeyAlgorithm RSA `
        -KeyLength 3072 `
        -HashAlgorithm SHA256 `
        -KeyExportPolicy Exportable `
        -KeyUsage DigitalSignature `
        -NotAfter (Get-Date).AddYears(2)

    Export-Certificate -Cert $certificate -FilePath $publicCertificatePath | Out-Null
    Export-PfxCertificate -Cert $certificate -FilePath $pfxPath -Password $password | Out-Null

    if ($TrustCurrentUserRoot) {
        Import-Certificate -FilePath $publicCertificatePath -CertStoreLocation Cert:\CurrentUser\Root | Out-Null
    }

    $signExitCode = Invoke-NativeCommand `
        -FilePath "dotnet" `
        -Arguments @(
            "nuget",
            "sign",
            $signedPackage,
            "--certificate-path",
            $pfxPath,
            "--certificate-password",
            $passwordPlainText,
            "--timestamper",
            $TimestampAuthority,
            "--overwrite"
        ) `
        -LogPath $signLogPath

    $certificateFingerprintSha256 = Get-Sha256 -Path $publicCertificatePath
    $verifyExitCode = Invoke-NativeCommand `
        -FilePath "dotnet" `
        -Arguments @(
            "nuget",
            "verify",
            $signedPackage,
            "--all",
            "--certificate-fingerprint",
            $certificateFingerprintSha256,
            "--verbosity",
            "detailed"
        ) `
        -LogPath $verifyLogPath

    [ordered]@{
        Subject = $certificate.Subject
        ThumbprintSha1 = $certificate.Thumbprint.ToLowerInvariant()
        FingerprintSha256 = $certificateFingerprintSha256
        NotBeforeUtc = $certificate.NotBefore.ToUniversalTime().ToString("O")
        NotAfterUtc = $certificate.NotAfter.ToUniversalTime().ToString("O")
        TrustCurrentUserRoot = [bool]$TrustCurrentUserRoot
    } | ConvertTo-Json -Depth 5 | Set-Content -Path $certificateInfoPath -Encoding UTF8
}
finally {
    if (Test-Path $pfxPath) {
        Remove-Item -LiteralPath $pfxPath -Force
    }

    if ($null -ne $certificate) {
        $privateStorePath = "Cert:\CurrentUser\My\$($certificate.Thumbprint)"
        if (Test-Path $privateStorePath) {
            Remove-Item -LiteralPath $privateStorePath -Force
        }
    }

    $passwordPlainText = $null
}

$evidence = [ordered]@{
    RunId = "internal-signing-$runId"
    Version = $Version
    PackagePath = Get-RelativePath -Path $signedPackage
    PublicCertificatePath = Get-RelativePath -Path $publicCertificatePath
    CertificatePublicInfoPath = Get-RelativePath -Path $certificateInfoPath
    CertificateSubject = $CertificateSubject
    CertificateThumbprintSha1 = $certificate.Thumbprint.ToLowerInvariant()
    CertificateFingerprintSha256 = Get-Sha256 -Path $publicCertificatePath
    TimestampAuthority = $TimestampAuthority
    TrustStore = if ($TrustCurrentUserRoot) { "Cert:\CurrentUser\Root" } else { "not-imported" }
    TrustModel = if ($TrustCurrentUserRoot) { "internal-self-signed-current-user-root" } else { "self-signed-untrusted" }
    Scope = "Internal RC signing evidence only; public stable releases require approved trusted signing material."
    SignExitCode = $signExitCode
    VerifyExitCode = $verifyExitCode
    PackageSha256 = Get-Sha256 -Path $signedPackage
    SignLogPath = Get-RelativePath -Path $signLogPath
    VerificationLogPath = Get-RelativePath -Path $verifyLogPath
    SignLogSha256 = Get-Sha256 -Path $signLogPath
    VerificationLogSha256 = Get-Sha256 -Path $verifyLogPath
    PrivateKeyDeleted = !(Test-Path $pfxPath)
}

$evidence | ConvertTo-Json -Depth 10 | Set-Content -Path $evidencePath -Encoding UTF8

if ($signExitCode -ne 0 -or $verifyExitCode -ne 0) {
    throw "Internal signing evidence failed. SignExitCode=$signExitCode VerifyExitCode=$verifyExitCode. See $runRoot."
}

[ordered]@{
    RunId = $evidence.RunId
    EvidencePath = Get-RelativePath -Path $evidencePath
    PackagePath = $evidence.PackagePath
    CertificateFingerprintSha256 = $evidence.CertificateFingerprintSha256
    TimestampAuthority = $TimestampAuthority
    VerifyExitCode = $verifyExitCode
} | ConvertTo-Json -Depth 5
