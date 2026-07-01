# Installs Rule34 Gallery from the folder containing this script (or -SourceDir).
# Creates Start Menu shortcut; optional Desktop shortcut.
# Never installs into or removes PH Browser (Programs\PHBrowser).

param(
    [string]$SourceDir = $PSScriptRoot,
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA "Programs\Rule34Gallery"),
    [switch]$DesktopShortcut
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "windows-paths.ps1")

Assert-R34WindowsSource -SourceDir $SourceDir
Assert-R34WindowsInstallTarget -InstallDir $InstallDir

Write-Host "Installing Rule34 Gallery to:" -ForegroundColor Cyan
Write-Host "  $InstallDir"

if (Test-Path $InstallDir) {
    Write-Host "Updating existing Rule34 Gallery installation..." -ForegroundColor Yellow
    Remove-Item $InstallDir -Recurse -Force
}

New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
Copy-Item -Path (Join-Path $SourceDir "*") -Destination $InstallDir -Recurse -Force

$targetExe = Join-Path $InstallDir $R34WindowsExe
$wsh = New-Object -ComObject WScript.Shell
$startMenu = [Environment]::GetFolderPath("Programs")
Protect-PhBrowserShortcuts -StartMenuDir $startMenu

$shortcutPath = Join-Path $startMenu $R34StartMenuShortcut
$shortcut = $wsh.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $targetExe
$shortcut.WorkingDirectory = $InstallDir
$shortcut.Description = "Rule34 Gallery"
$shortcut.Save()

# Do not touch PH Browser shortcuts — it is a separate app (Programs\PHBrowser).

if ($DesktopShortcut) {
    $desktop = [Environment]::GetFolderPath("Desktop")
    $deskShortcut = Join-Path $desktop $R34DesktopShortcut
    $sc = $wsh.CreateShortcut($deskShortcut)
    $sc.TargetPath = $targetExe
    $sc.WorkingDirectory = $InstallDir
    $sc.Description = "Rule34 Gallery"
    $sc.Save()
}

Write-Host ""
Write-Host "Installed successfully." -ForegroundColor Green
Write-Host "  Start Menu: Rule34 Gallery"
Write-Host "  PH Browser install left unchanged: $PhWindowsInstallDir"
if ($DesktopShortcut) { Write-Host "  Desktop shortcut created" }
Write-Host ""
Write-Host "For Firebase login, copy firebase-config.example.json to firebase-config.json in:"
Write-Host "  $InstallDir"
