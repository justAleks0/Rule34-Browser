# Canonical Windows install paths for Rule34 Gallery and PH Browser (separate apps).

$script:R34WindowsExe = "Rule34Gallery.exe"
$script:R34WindowsDistFolder = "Rule34Gallery-win-x64"
$script:R34WindowsInstallDir = Join-Path $env:LOCALAPPDATA "Programs\Rule34Gallery"
$script:R34StartMenuShortcut = "Rule34 Gallery.lnk"
$script:R34DesktopShortcut = "Rule34 Gallery.lnk"
$script:R34ProcessName = "Rule34Gallery"

$script:PhWindowsExe = "PHBrowser.exe"
$script:PhWindowsDistFolder = "PHBrowser-win-x64"
$script:PhWindowsInstallDir = Join-Path $env:LOCALAPPDATA "Programs\PHBrowser"
$script:PhStartMenuShortcut = "PH Browser.lnk"
$script:PhDesktopShortcut = "PH Browser.lnk"
$script:PhProcessName = "PHBrowser"

function Assert-R34WindowsInstallTarget {
    param([string]$InstallDir)

    $normalized = [System.IO.Path]::GetFullPath($InstallDir.TrimEnd('\', '/'))
    $expected = [System.IO.Path]::GetFullPath($R34WindowsInstallDir)
    $phDir = [System.IO.Path]::GetFullPath($PhWindowsInstallDir)

    if ($normalized -eq $phDir) {
        throw @"
Refusing to install Rule34 Gallery into PH Browser's install folder:
  $phDir
Use Programs\Rule34Gallery instead. These are separate apps.
"@
    }

    if ($normalized -ne $expected) {
        throw @"
Refusing to install: InstallDir must be the Rule34 Gallery folder:
  $expected
Got: $InstallDir
Pass -InstallDir only when you are sure you are not overwriting PH Browser.
"@
    }
}

function Assert-R34WindowsSource {
    param([string]$SourceDir)

    $sourceExe = Join-Path $SourceDir $R34WindowsExe
    $wrongExe = Join-Path $SourceDir $PhWindowsExe

    if (Test-Path $wrongExe) {
        throw @"
Refusing to install: source folder contains $PhWindowsExe.
This looks like a PH Browser build (dist\$PhWindowsDistFolder), not Rule34 Gallery.
Build from the Rule34 Gallery repo: scripts\publish-windows.ps1
"@
    }

    if (-not (Test-Path $sourceExe)) {
        throw "Could not find $R34WindowsExe in $SourceDir. Run scripts\publish-windows.ps1 first."
    }
}

function Assert-R34WindowsDistFolder {
    param([string]$DistDir)

    Assert-R34WindowsSource -SourceDir $DistDir

    $folderName = Split-Path $DistDir -Leaf
    if ($folderName -ne $R34WindowsDistFolder) {
        throw @"
Refusing to install: dist folder must be '$R34WindowsDistFolder', not '$folderName'.
"@
    }
}

function Stop-R34WindowsProcess {
    Get-Process $R34ProcessName -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue
}

# PH Browser is a separate install — never remove or rewrite its shortcuts from R34 scripts.
function Protect-PhBrowserShortcuts {
    param([string]$StartMenuDir = [Environment]::GetFolderPath("Programs"))

    $phShortcut = Join-Path $StartMenuDir $PhStartMenuShortcut
    if (Test-Path $phShortcut) {
        Write-Host "Leaving PH Browser shortcut unchanged: $phShortcut" -ForegroundColor DarkGray
    }
}
