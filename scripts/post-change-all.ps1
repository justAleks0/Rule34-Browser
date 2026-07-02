# Runs the full post-change pipeline (version, changelog, desktop publish/install, Android APK).
# Use when finishing a task - hooks may not run for Android-only edits or if the agent skipped them.

param(
    [ValidateSet("auto", "small", "medium", "big")]
    [string]$Tier = "auto",
    [string[]]$ChangelogNotes = @(),
    [switch]$SkipVersionBump,
    [switch]$SkipInstall,
    [switch]$SkipAndroid,
    [switch]$SkipDesktop
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $repoRoot "scripts\windows-paths.ps1")
. (Join-Path $repoRoot "scripts\android-paths.ps1")
$androidRoot = $R34AndroidRoot

Write-Host "=== Post-change pipeline ===" -ForegroundColor Cyan

if (-not $SkipVersionBump) {
    $payloadObj = @{
        file_paths = @("Rule34GalleryApp/", "Rule34Gallery.Core/")
    }
    if ($ChangelogNotes.Count -gt 0) {
        $payloadObj.changelog_notes = $ChangelogNotes
    }
    $payload = $payloadObj | ConvertTo-Json -Compress
    & powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot ".cursor\hooks\bump-version-and-changelog.ps1") `
        -RepoRoot $repoRoot -PayloadJson $payload
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot "scripts\sync-changelog-android.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot "scripts\sync-help-android.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $SkipDesktop) {
    Write-Host ""
    Write-Host "--- Desktop publish ---" -ForegroundColor Cyan
    & powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot "scripts\publish-windows.ps1")
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if (-not $SkipInstall) {
        $sourceDir = Join-Path $repoRoot "dist\$R34WindowsDistFolder"
        $installer = Join-Path $sourceDir "Install.ps1"
        if (-not (Test-Path $installer)) {
            Write-Error "Installer not found: $installer"
        }
        Assert-R34WindowsDistFolder -DistDir $sourceDir
        Stop-R34WindowsProcess
        Write-Host ""
        Write-Host "--- Desktop install ---" -ForegroundColor Cyan
        & $installer -SourceDir $sourceDir
    }
}

if (-not $SkipAndroid) {
    if (-not (Test-Path $androidRoot)) {
        Write-Warning "Android project not found at $androidRoot - skipping APK build."
    } else {
        Write-Host ""
        Write-Host "--- Android release APK ---" -ForegroundColor Cyan
        & powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot "scripts\publish-android.ps1")
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        if (-not $SkipInstall) {
            Write-Host ""
            Write-Host "--- Android install (if device connected) ---" -ForegroundColor Cyan
            & powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot "scripts\install-r34-android.ps1")
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        }
    }
}

Write-Host ""
Write-Host "=== Post-change pipeline complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "GitHub release (when shipping): scripts/publish-github-release.ps1 -SkipBuild" -ForegroundColor DarkGray
Write-Host "  Release notes must come from changelog.md — generic 'Release vX' is blocked." -ForegroundColor DarkGray
