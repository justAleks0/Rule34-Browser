# Reads the package name from an APK (aapt badging). Used to prevent shipping the wrong app as R34Browser-release.apk.

param(
    [Parameter(Mandatory = $true)]
    [string]$ApkPath
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ApkPath)) {
    Write-Error "APK not found: $ApkPath"
}

$sdkRoot = $env:ANDROID_HOME
if ([string]::IsNullOrWhiteSpace($sdkRoot)) {
    $sdkRoot = Join-Path $env:LOCALAPPDATA "Android\Sdk"
}

$buildTools = Get-ChildItem (Join-Path $sdkRoot "build-tools") -Directory -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending |
    Select-Object -First 1
if (-not $buildTools) {
    Write-Error "Android build-tools not found under $sdkRoot"
}

$aapt = Join-Path $buildTools.FullName "aapt.exe"
if (-not (Test-Path $aapt)) {
    Write-Error "aapt.exe not found: $aapt"
}

$line = & $aapt dump badging $ApkPath 2>&1 | Select-String "^package: name=" | Select-Object -First 1
if (-not $line -or $line.Line -notmatch "name='([^']+)'") {
    Write-Error "Could not read package name from APK: $ApkPath"
}

$Matches[1]
