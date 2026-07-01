# Copies repo changelog.md into the native Android app assets (bundled in APK).

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $repoRoot "scripts\android-paths.ps1")
$source = Join-Path $repoRoot "changelog.md"
$androidAssets = Join-Path $R34AndroidRoot "app\src\main\assets"
$dest = Join-Path $androidAssets "changelog.md"

if (-not (Test-Path $source)) {
    Write-Error "Missing changelog at $source"
}

if (-not (Test-Path $androidAssets)) {
    Write-Error "Android assets folder not found at $androidAssets"
}

New-Item -ItemType Directory -Path $androidAssets -Force | Out-Null
Copy-Item $source $dest -Force
Write-Host "Synced changelog -> $dest"
