# Builds a release APK for the native Android app (Kotlin / Compose).
# Output: dist/R34Browser-release.apk

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $repoRoot "scripts\android-paths.ps1")
$androidRoot = $R34AndroidRoot
$distRoot = Join-Path $repoRoot "dist"

if (-not (Test-Path $androidRoot)) {
    Write-Error "Android project not found at $androidRoot"
}

Write-Host "Building Android release APK..." -ForegroundColor Cyan
Push-Location $androidRoot
try {
    & .\gradlew.bat assembleRelease
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} finally {
    Pop-Location
}

$apk = Join-Path $androidRoot "app\build\outputs\apk\release\app-release-unsigned.apk"
if (-not (Test-Path $apk)) {
    $apk = Get-ChildItem -Path (Join-Path $androidRoot "app\build\outputs\apk\release") -Filter "*.apk" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}
if (-not $apk -or -not (Test-Path $apk)) {
    Write-Error "Release APK not found under app/build/outputs/apk/release"
}

New-Item -ItemType Directory -Path $distRoot -Force | Out-Null
$outApk = Join-Path $distRoot "R34Browser-release.apk"
Copy-Item $apk $outApk -Force

$package = & (Join-Path $repoRoot "scripts\get-android-apk-package.ps1") -ApkPath $outApk
if ($package -ne $R34AndroidPackage) {
    Remove-Item $outApk -Force
    Write-Error @"
Refusing to publish: APK package is '$package' but Rule34 Gallery must be '$R34AndroidPackage'.
This build may have come from PH Browser ($PhAndroidPackage). R34 APK was not copied to dist.
"@
}

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "  APK: $outApk ($R34AndroidPackage)"
Write-Host ""
Write-Host "Install on a device: scripts\install-r34-android.ps1"
Write-Host "  or: adb install -r `"$outApk`""
Write-Host "Or copy the APK to the phone and open it (allow unknown sources)."
