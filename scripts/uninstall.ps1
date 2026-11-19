<#
.SYNOPSIS
    Removes ControllerPC installed by install.ps1: the installed exe and its Start Menu shortcut.

.DESCRIPTION
    Deletes only ControllerPC.exe from the install folder, then the folder itself if it is left empty.
    The Start Menu shortcut is removed only when it points into that install folder.
    Settings in %AppData%\ControllerPC are kept unless -RemoveSettings is given.

.PARAMETER InstallDir
    The folder install.ps1 used. Defaults to %LocalAppData%\Programs\ControllerPC.

.PARAMETER RemoveSettings
    Also delete %AppData%\ControllerPC (settings and any .bad backups).
#>
[CmdletBinding()]
param(
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'Programs\ControllerPC'),
    [switch]$RemoveSettings
)

$ErrorActionPreference = 'Stop'

$appName = 'ControllerPC'
$exeName = "$appName.exe"

if (Get-Process -Name $appName -ErrorAction SilentlyContinue) {
    throw "$appName is running. Quit it from its tray icon, then run this script again."
}

$installPath = [System.IO.Path]::GetFullPath($InstallDir).TrimEnd('\')
$exePath = Join-Path $installPath $exeName
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Programs')) "$appName.lnk"

if (Test-Path -LiteralPath $shortcutPath) {
    $target = (New-Object -ComObject WScript.Shell).CreateShortcut($shortcutPath).TargetPath
    $targetDir = if ($target) { [System.IO.Path]::GetDirectoryName($target).TrimEnd('\') } else { '' }
    if ($targetDir -ieq $installPath) {
        Remove-Item -LiteralPath $shortcutPath -Force
        Write-Host "Removed Start Menu shortcut."
    }
    else {
        Write-Host "Kept Start Menu shortcut: it points to '$target', not this install."
    }
}

if (Test-Path -LiteralPath $exePath) {
    Remove-Item -LiteralPath $exePath -Force
    Write-Host "Removed $exePath"
}
else {
    Write-Host "No $exeName found in $installPath"
}

if ((Test-Path -LiteralPath $installPath) -and -not (Get-ChildItem -LiteralPath $installPath -Force)) {
    Remove-Item -LiteralPath $installPath -Force
}

if ($RemoveSettings) {
    $settingsDir = Join-Path $env:APPDATA $appName
    if (Test-Path -LiteralPath $settingsDir) {
        Remove-Item -LiteralPath $settingsDir -Recurse -Force
        Write-Host "Removed settings in $settingsDir"
    }
}

Write-Host "$appName uninstalled."
