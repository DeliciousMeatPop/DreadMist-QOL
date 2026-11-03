<#
 Copies the built plugin into BepInEx\plugins\DreadMistQOL inside the game folder.
 Does not touch any original game file.
 Usage:  powershell -ExecutionPolicy Bypass -File scripts\install.ps1 [-GameDir "path\to\DreadMist"]
#>
param([string]$GameDir)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

$GameDir = Resolve-GameDir $GameDir
$dll = Join-Path $script:RepoRoot 'dist\DreadMistQOL.dll'
if (-not (Test-Path -LiteralPath $dll)) { throw 'dist\DreadMistQOL.dll not found. Run scripts\build.ps1 first.' }
if (-not (Test-Path -LiteralPath (Join-Path $GameDir 'BepInEx\core\BepInEx.dll'))) {
    throw 'BepInEx is not installed in the game folder. See "Install BepInEx" in the README.'
}
if (Get-Process -Name 'DreadMist' -ErrorAction SilentlyContinue) { throw 'Close DreadMist first.' }

$dest = Join-Path $GameDir 'BepInEx\plugins\DreadMistQOL'
New-Item -ItemType Directory -Force -Path $dest | Out-Null
Copy-Item -LiteralPath $dll -Destination $dest -Force
Write-Host "Installed to: $dest"
Write-Host 'Start the game once. A settings file appears in BepInEx\config (com.deliciousmeatpop.dreadmistqol.cfg).'
Write-Host 'Logs: BepInEx\LogOutput.log'
