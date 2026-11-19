<#
.SYNOPSIS
    Removes ControllerPC installed by install.ps1: the Start Menu shortcut and the installed exe.

.DESCRIPTION
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

if (Get-Process -Name $appName -ErrorAction SilentlyContinue) {
    throw "$appName is running. Quit it from its tray icon, then run this script again."
}

$shortcutPath = Join-Path ([Environment]::GetFolderPath('Programs')) "$appName.lnk"
Remove-Item -Path $shortcutPath -Force -ErrorAction SilentlyContinue
Remove-Item -Path $InstallDir -Recurse -Force -ErrorAction SilentlyContinue

if ($RemoveSettings) {
    Remove-Item -Path (Join-Path $env:APPDATA $appName) -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "$appName uninstalled."
