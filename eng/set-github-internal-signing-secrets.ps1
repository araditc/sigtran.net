<#
.SYNOPSIS
Creates GitHub Actions signing secrets for an internal SIGTRAN.NET dry-run.

.DESCRIPTION
This helper creates a temporary self-signed code-signing certificate, exports it
as a PFX, stores the PFX and password in GitHub repository secrets, and deletes
local private-key material after upload. It is intended only for protected
release workflow dry-runs with `publish=false`.

Stable or public package releases must use the organization's approved signing
certificate and secret-management process.
#>
param(
    [string]$Repository = "araditc/sigtran.net",
    [string]$GhPath = "C:\Program Files\GitHub CLI\gh.exe",
    [string]$ArtifactRoot = "artifacts/github-release-signing",
    [string]$TimestampAuthority = "http://timestamp.sectigo.com",
    [string]$CertificateSubject = "CN=Sigtran.NET GitHub Actions Internal RC Signing, O=SIGTRAN.NET contributors"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$runId = [DateTimeOffset]::UtcNow.ToString("yyyyMMddTHHmmssZ")
$runRoot = Join-Path (Join-Path $root $ArtifactRoot) $runId
$pfxPath = Join-Path $runRoot "sigtran-github-actions-internal-rc-signing.pfx"
$publicCertificatePath = Join-Path $runRoot "sigtran-github-actions-internal-rc-signing.cer"
$publicInfoPath = Join-Path $runRoot "certificate-public-info.json"

New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

if (!(Test-Path $GhPath)) {
    throw "GitHub CLI was not found at $GhPath."
}

function Invoke-GhSecretSet {
    param(
        [string]$Name,
        [string]$Value
    )

    $Value | & $GhPath secret set $Name --repo $Repository
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to set GitHub secret $Name."
    }
}

function Invoke-GhVariableSet {
    param(
        [string]$Name,
        [string]$Value
    )

    $Value | & $GhPath variable set $Name --repo $Repository
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to set GitHub variable $Name."
    }
}

function Get-Sha256 {
    param([string]$Path)

    return (Get-FileHash -Path $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

$certificate = $null
$passwordPlainText = [Guid]::NewGuid().ToString("N") + [Guid]::NewGuid().ToString("N")
$password = ConvertTo-SecureString -String $passwordPlainText -Force -AsPlainText

try {
    $certificate = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $CertificateSubject `
        -CertStoreLocation Cert:\CurrentUser\My `
        -KeyAlgorithm RSA `
        -KeyLength 3072 `
        -HashAlgorithm SHA256 `
        -KeyExportPolicy Exportable `
        -KeyUsage DigitalSignature `
        -NotAfter (Get-Date).AddYears(1)

    Export-Certificate -Cert $certificate -FilePath $publicCertificatePath | Out-Null
    Export-PfxCertificate -Cert $certificate -FilePath $pfxPath -Password $password | Out-Null

    $certificateBase64 = [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($pfxPath))
    Invoke-GhSecretSet -Name "SIGNING_CERTIFICATE" -Value $certificateBase64
    Invoke-GhSecretSet -Name "SIGNING_CERTIFICATE_PASSWORD" -Value $passwordPlainText
    Invoke-GhVariableSet -Name "TIMESTAMP_AUTHORITY" -Value $TimestampAuthority

    [ordered]@{
        RunId = "github-internal-signing-$runId"
        Repository = $Repository
        PublicCertificatePath = $publicCertificatePath.Replace((Resolve-Path $root).Path + [System.IO.Path]::DirectorySeparatorChar, "").Replace("\", "/")
        CertificateSubject = $certificate.Subject
        CertificateThumbprintSha1 = $certificate.Thumbprint.ToLowerInvariant()
        CertificateFingerprintSha256 = Get-Sha256 -Path $publicCertificatePath
        TimestampAuthority = $TimestampAuthority
        GitHubSecrets = @("SIGNING_CERTIFICATE", "SIGNING_CERTIFICATE_PASSWORD")
        GitHubVariables = @("TIMESTAMP_AUTHORITY")
        Scope = "Internal GitHub Actions dry-run signing only; no NuGet publication."
        PrivateKeyDeleted = $false
    } | ConvertTo-Json -Depth 5 | Set-Content -Path $publicInfoPath -Encoding UTF8
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

$publicInfo = Get-Content -Raw -Path $publicInfoPath | ConvertFrom-Json
$publicInfo.PrivateKeyDeleted = !(Test-Path $pfxPath)
$publicInfo | ConvertTo-Json -Depth 5 | Set-Content -Path $publicInfoPath -Encoding UTF8

[ordered]@{
    RunId = $publicInfo.RunId
    Repository = $Repository
    PublicCertificatePath = $publicInfo.PublicCertificatePath
    CertificateFingerprintSha256 = $publicInfo.CertificateFingerprintSha256
    TimestampAuthority = $TimestampAuthority
    PrivateKeyDeleted = $publicInfo.PrivateKeyDeleted
} | ConvertTo-Json -Depth 5
