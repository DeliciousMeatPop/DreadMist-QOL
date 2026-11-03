# Shared helpers. Dot-source this file: . "$PSScriptRoot\common.ps1"
# No machine-specific paths: the game folder is detected (or you are asked once).

$script:AppId = '2285190'   # DreadMist on Steam
$script:RepoRoot = Split-Path -Parent $PSScriptRoot

function Test-GameDir([string]$Path) {
    return ($Path -and (Test-Path -LiteralPath (Join-Path $Path 'DreadMist.exe')))
}

function Get-SteamRoots {
    $roots = @()
    foreach ($key in 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam', 'HKCU:\Software\Valve\Steam') {
        try {
            $p = Get-ItemProperty -Path $key -ErrorAction Stop
            foreach ($name in 'InstallPath', 'SteamPath') {
                if ($p.$name) { $roots += $p.$name }
            }
        } catch { }
    }
    return $roots | Select-Object -Unique
}

function Find-GameDir {
    # 1) environment variable, 2) path saved by tools\inspect_install.py, 3) Steam libraries
    if (Test-GameDir $env:DREADMIST_DIR) { return $env:DREADMIST_DIR }

    $saved = Join-Path $script:RepoRoot 'inspection\game_path.txt'
    if (Test-Path -LiteralPath $saved) {
        $p = (Get-Content -LiteralPath $saved -Raw).Trim()
        if (Test-GameDir $p) { return $p }
    }

    foreach ($root in Get-SteamRoots) {
        $libs = @($root)
        $vdf = Join-Path $root 'steamapps\libraryfolders.vdf'
        if (Test-Path -LiteralPath $vdf) {
            foreach ($m in [regex]::Matches((Get-Content -LiteralPath $vdf -Raw), '"path"\s+"([^"]+)"')) {
                $libs += $m.Groups[1].Value.Replace('\\', '\')
            }
        }
        foreach ($lib in $libs) {
            $manifest = Join-Path $lib "steamapps\appmanifest_$($script:AppId).acf"
            if (Test-Path -LiteralPath $manifest) {
                $d = [regex]::Match((Get-Content -LiteralPath $manifest -Raw), '"installdir"\s+"([^"]+)"').Groups[1].Value
                $candidate = Join-Path $lib "steamapps\common\$d"
                if (Test-GameDir $candidate) { return $candidate }
            }
        }
    }
    return $null
}

function Resolve-GameDir([string]$Given) {
    if ($Given) {
        if (Test-GameDir $Given) { return (Resolve-Path -LiteralPath $Given).Path }
        throw "That folder does not contain DreadMist.exe: $Given"
    }
    $found = Find-GameDir
    if ($found) { return $found }

    Write-Host 'Could not find DreadMist automatically. Please pick the folder that contains DreadMist.exe.'
    Add-Type -AssemblyName System.Windows.Forms
    $dlg = New-Object System.Windows.Forms.FolderBrowserDialog
    $dlg.Description = 'Select the DreadMist game folder (the one containing DreadMist.exe)'
    if ($dlg.ShowDialog() -eq 'OK' -and (Test-GameDir $dlg.SelectedPath)) { return $dlg.SelectedPath }
    throw 'No valid DreadMist folder selected.'
}
