<#
.SYNOPSIS
    Builds ControllerPC and installs it for the current user, with a Start Menu shortcut so it shows up in
    Windows Search.

.DESCRIPTION
    Publishes a Release build, copies ControllerPC.exe to the install folder (replacing an older copy), and
    creates "ControllerPC" in the current user's Start Menu Programs folder. No administrator rights needed.
    Settings in %AppData%\ControllerPC are not touched.

.PARAMETER InstallDir
    Where ControllerPC.exe is installed. Defaults to %LocalAppData%\Programs\ControllerPC.
#>
[CmdletBinding()]
param(
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'Programs\ControllerPC')
)

$ErrorActionPreference = 'Stop'

# Resolve against PowerShell's current location so a relative -InstallDir works and the shortcut gets an absolute target.
$InstallDir = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($InstallDir).TrimEnd('\')

$appName = 'ControllerPC'
$exeName = "$appName.exe"
$repoRoot = Split-Path -Parent $PSScriptRoot
$appProject = Join-Path $repoRoot 'src\ControllerPC.App'

if (Get-Process -Name $appName -ErrorAction SilentlyContinue) {
    throw "$appName is running. Quit it from its tray icon, then run this script again."
}

$publishDir = Join-Path ([System.IO.Path]::GetTempPath()) "$appName-publish-$([guid]::NewGuid())"
try {
    Write-Host "Building $appName (Release)..."
    dotnet publish $appProject -c Release -o $publishDir --nologo -v quiet
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }

    New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
    Copy-Item -LiteralPath (Join-Path $publishDir $exeName) -Destination (Join-Path $InstallDir $exeName) -Force
}
finally {
    if (Test-Path -LiteralPath $publishDir) {
        Remove-Item -LiteralPath $publishDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

$exePath = Join-Path $InstallDir $exeName
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Programs')) "$appName.lnk"

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = $InstallDir
$shortcut.Description = 'Use your controller as a mouse and keyboard'
$shortcut.Save()

Write-Host "Installed $exePath"
Write-Host "Start Menu shortcut: $shortcutPath"
Write-Host "Search for '$appName' in the Start menu to launch it."
