<#
 Builds dist\DreadMistQOL.dll.
 Needs: the .NET SDK (https://dotnet.microsoft.com/download) and BepInEx already installed in the game folder.
 Usage:  powershell -ExecutionPolicy Bypass -File scripts\build.ps1 [-GameDir "path\to\DreadMist"]
#>
param([string]$GameDir)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

$GameDir = Resolve-GameDir $GameDir
Write-Host "Game folder: $GameDir"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET SDK is not installed. Get it from https://dotnet.microsoft.com/download, then reopen PowerShell.'
}

$proj = Join-Path $script:RepoRoot 'src\DreadMistQOL\DreadMistQOL.csproj'
dotnet build $proj -c Release "-p:GameDir=$GameDir"
if ($LASTEXITCODE -ne 0) { throw 'Build failed. Copy the red error lines and send them over.' }

Write-Host ''
Write-Host "Built: $(Join-Path $script:RepoRoot 'dist\DreadMistQOL.dll')"
Write-Host 'Next: scripts\install.ps1'
