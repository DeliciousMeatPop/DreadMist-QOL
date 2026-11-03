<#
 Removes this mod. Add -RemoveBepInEx to also remove BepInEx itself (restores the game folder to stock).
 Usage:  powershell -ExecutionPolicy Bypass -File scripts\uninstall.ps1 [-GameDir "..."] [-RemoveBepInEx] [-RemoveConfig]
#>
param([string]$GameDir, [switch]$RemoveBepInEx, [switch]$RemoveConfig)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

$GameDir = Resolve-GameDir $GameDir
if (Get-Process -Name 'DreadMist' -ErrorAction SilentlyContinue) { throw 'Close DreadMist first.' }

$plugin = Join-Path $GameDir 'BepInEx\plugins\DreadMistQOL'
if (Test-Path -LiteralPath $plugin) { Remove-Item -LiteralPath $plugin -Recurse -Force; Write-Host "Removed $plugin" }

if ($RemoveConfig) {
    $cfg = Join-Path $GameDir 'BepInEx\config\com.deliciousmeatpop.dreadmistqol.cfg'
    if (Test-Path -LiteralPath $cfg) { Remove-Item -LiteralPath $cfg -Force; Write-Host "Removed $cfg" }
}

if ($RemoveBepInEx) {
    foreach ($name in 'BepInEx', 'winhttp.dll', 'doorstop_config.ini', '.doorstop_version', 'changelog.txt') {
        $p = Join-Path $GameDir $name
        if (Test-Path -LiteralPath $p) { Remove-Item -LiteralPath $p -Recurse -Force; Write-Host "Removed $p" }
    }
}
Write-Host 'Done.'
