param([Parameter(Mandatory = $true)][string]$ReleaseJson, [string]$InstallDirectory)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$release = (Get-Content -LiteralPath $ReleaseJson -Raw | ConvertFrom-Json).results[0]
if ($release.version -notmatch '^\d+\.\d+\.\d+[afbp]\d+$') { throw 'Unexpected editor version.' }
$download = $release.downloads | Where-Object { $_.platform -eq 'WINDOWS' -and $_.architecture -eq 'X86_64' } | Select-Object -First 1
if (!$download -or ([uri]$download.url).Host -ne 'download.unity3d.com') { throw 'Expected an official Unity Windows installer.' }
$root = Join-Path $projectRoot 'Build/CompatibilityEditors'
New-Item -ItemType Directory -Force -Path $root | Out-Null
$destination = if ($InstallDirectory) { [IO.Path]::GetFullPath((Join-Path $projectRoot $InstallDirectory)) } else { Join-Path $root $release.version }
$allowedRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Build')) + [IO.Path]::DirectorySeparatorChar
if (!$destination.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Verification editor must remain under the project Build directory.' }
$editor = Join-Path $destination 'Editor/Unity.exe'
if ((Test-Path -LiteralPath $editor) -and (Test-Path -LiteralPath (Join-Path $destination 'installation-completed.json'))) { Write-Output "Editor already available: $editor"; exit 0 }
$installer = Join-Path $root ("UnitySetup64-" + $release.version + '.exe')
$integrityParts = $download.integrity -split '-', 2
if ($integrityParts[0] -ne 'md5') { throw 'Unexpected published integrity format.' }
$integrityBytes = [Convert]::FromBase64String($integrityParts[1])
$expected = if ($integrityBytes.Length -eq 32) { [Text.Encoding]::ASCII.GetString($integrityBytes) } else { [Convert]::ToHexString($integrityBytes) }
$verifiedDownload = (Test-Path -LiteralPath $installer) -and (Get-FileHash -LiteralPath $installer -Algorithm MD5).Hash -eq $expected
if (!$verifiedDownload) {
    Write-Output "Downloading official editor $($release.version)"
    & curl.exe --fail --location --silent --show-error --retry 2 --continue-at - --output $installer $download.url
    if ($LASTEXITCODE -ne 0) { throw "Editor download failed: $LASTEXITCODE" }
    if ((Get-FileHash -LiteralPath $installer -Algorithm MD5).Hash -ne $expected) { throw 'Published installer hash mismatch.' }
}
# API size values can be rounded; the published content hash and trusted signature are authoritative.
$signature = Get-AuthenticodeSignature -LiteralPath $installer
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Unity') { throw "Installer signature is not verified: $($signature.Status)" }
@{ version = $release.version; url = $download.url; sha256 = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash; signer = $signature.SignerCertificate.Subject } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root ($release.version + '-installer.json'))
Write-Output "Verified installer. Installing $($release.version)"
$process = Start-Process -FilePath $installer -ArgumentList ("/S /D=" + $destination) -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath $editor)) { throw "Editor installation failed: $($process.ExitCode)" }
@{ version = $release.version; editor = $editor; exitCode = $process.ExitCode } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $destination 'installation-completed.json')
Write-Output "Editor ready: $editor"
