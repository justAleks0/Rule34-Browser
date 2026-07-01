# Copies Core help-topics.json into the native Android app assets (bundled in APK).

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $repoRoot "scripts\android-paths.ps1")
$source = Join-Path $repoRoot "Rule34Gallery.Core\Content\help-topics.json"
$androidAssets = Join-Path $R34AndroidRoot "app\src\main\assets"
$dest = Join-Path $androidAssets "help-topics.json"

if (-not (Test-Path $source)) {
    Write-Error "Missing help topics at $source"
}

if (-not (Test-Path $androidAssets)) {
    Write-Error "Android assets folder not found at $androidAssets"
}

New-Item -ItemType Directory -Path $androidAssets -Force | Out-Null
Copy-Item $source $dest -Force
Write-Host "Synced help-topics.json -> $dest"
