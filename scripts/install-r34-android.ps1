# Installs dist/R34Browser-release.apk via adb. Verifies package before install so PH Browser is never overwritten.

param(
    [string]$ApkPath = (Join-Path (Split-Path -Parent $PSScriptRoot) "dist\R34Browser-release.apk")
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "android-paths.ps1")

if (-not (Test-Path $ApkPath)) {
    Write-Error "APK not found: $ApkPath. Run scripts/publish-android.ps1 first."
}

$package = & (Join-Path $PSScriptRoot "get-android-apk-package.ps1") -ApkPath $ApkPath
if ($package -ne $R34AndroidPackage) {
    Write-Error @"
Refusing to install: APK package is '$package' but Rule34 Gallery must be '$R34AndroidPackage'.
This APK may have been built from PH Browser by mistake.
PH Browser ($PhAndroidPackage) is a separate app — build it from $PhAndroidRoot.
"@
}

$adb = Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\adb.exe"
if (-not (Test-Path $adb)) {
    Write-Warning "adb not found: $adb — skipping phone install."
    exit 0
}

$devices = & $adb devices 2>$null |
    Select-Object -Skip 1 |
    Where-Object { $_.Trim() -match '\tdevice$' }
if (-not $devices -or @($devices).Count -eq 0) {
    Write-Host "No Android device detected (adb devices) — skipping phone install." -ForegroundColor Yellow
    exit 0
}

Write-Host "Installing Rule34 Gallery ($R34AndroidPackage) on connected device..." -ForegroundColor Cyan
& $adb install -r $ApkPath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Installed on phone. PH Browser ($PhAndroidPackage) is unchanged." -ForegroundColor Green
