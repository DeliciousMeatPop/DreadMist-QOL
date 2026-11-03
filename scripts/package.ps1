<#
 Builds the mod and creates the release zip users download:
   dist\DreadMistQOL-v<version>.zip   (extracts straight into the game folder)
   dist\DreadMistQOL-v<version>.zip.sha256
 Usage:  powershell -ExecutionPolicy Bypass -File scripts\package.ps1 [-GameDir "path\to\DreadMist"]
#>
param([string]$GameDir)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

& "$PSScriptRoot\build.ps1" -GameDir $GameDir
if ($LASTEXITCODE -ne 0 -and $null -ne $LASTEXITCODE) { throw 'Build failed.' }

$root = $script:RepoRoot
$plugin = Get-Content -LiteralPath (Join-Path $root 'src\DreadMistQOL\Plugin.cs') -Raw
$version = [regex]::Match($plugin, 'Version\s*=\s*"([^"]+)"').Groups[1].Value
if (-not $version) { throw 'Could not read the version from Plugin.cs' }

$dll = Join-Path $root 'dist\DreadMistQOL.dll'
if (-not (Test-Path -LiteralPath $dll)) { throw 'dist\DreadMistQOL.dll missing' }

$stage = Join-Path $root 'dist\package'
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
$pluginDir = Join-Path $stage 'BepInEx\plugins\DreadMistQOL'
New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null

Copy-Item -LiteralPath $dll -Destination $pluginDir
foreach ($f in 'README.md', 'LICENSE', 'CHANGELOG.md') {
    Copy-Item -LiteralPath (Join-Path $root $f) -Destination $pluginDir
}

$zip = Join-Path $root "dist\DreadMistQOL-v$version.zip"
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip

$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLower()
Set-Content -LiteralPath "$zip.sha256" -Value "$hash  $(Split-Path $zip -Leaf)"

Write-Host ''
Write-Host "Release zip: $zip"
Write-Host "SHA-256:     $hash"
Write-Host 'Upload the .zip (and the .sha256) to a new GitHub Release.'
