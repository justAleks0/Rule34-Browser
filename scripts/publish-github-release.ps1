# Publishes desktop + Android builds to a GitHub Release with stable asset names.
param(
    [string]$Tag,
    [string]$Title,
    [string]$Notes,
    [switch]$SkipBuild,
    [switch]$Draft,
    [switch]$EditOnly
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$distRoot = Join-Path $repoRoot "dist"

function Get-ReleaseNotesFromChangelog {
    param(
        [string]$ChangelogPath,
        [string]$Version
    )

    if (-not (Test-Path $ChangelogPath)) {
        return $null
    }

    $text = Get-Content $ChangelogPath -Raw
    $escaped = [regex]::Escape($Version)
    $pattern = "(?ms)^##\s+$escaped(?:\s+-\s+[^\r\n]+)?\s*\r?\n(.*?)(?=^##\s|\z)"
    if ($text -match $pattern) {
        return $Matches[1].Trim()
    }

    return $null
}

function Test-ValidReleaseNotes {
    param([string]$Notes)

    if ([string]::IsNullOrWhiteSpace($Notes)) {
        return $false
    }

    if ($Notes -match '(?m)^\s*-\s+\S') {
        return $true
    }

    return $false
}

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI (gh) is required. Install from https://cli.github.com/"
}

$csproj = Join-Path $repoRoot "Rule34GalleryApp\Rule34GalleryApp.csproj"
[xml]$proj = Get-Content $csproj
$version = $proj.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Could not read <Version> from Rule34GalleryApp.csproj"
}

if ([string]::IsNullOrWhiteSpace($Tag)) {
    $Tag = "v$version"
}

$notesVersion = if ($Tag -match '^v(.+)$') { $Matches[1] } else { $version }

if ([string]::IsNullOrWhiteSpace($Title)) {
    $Title = "Rule34 Gallery $Tag"
}

if ([string]::IsNullOrWhiteSpace($Notes)) {
    $changelog = Join-Path $repoRoot "changelog.md"
    $Notes = Get-ReleaseNotesFromChangelog -ChangelogPath $changelog -Version $notesVersion
}

if (-not (Test-ValidReleaseNotes -Notes $Notes)) {
    throw @"
GitHub release notes for $notesVersion are missing or have no bullet points.

Add a real ## $notesVersion section at the top of changelog.md (see public-repo-hygiene.mdc), then re-run:
  scripts/publish-github-release.ps1

Refusing to publish generic text like 'Release $Tag'.
"@
}

$notesFile = Join-Path $env:TEMP "Rule34Gallery-release-notes-$Tag.md"
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($notesFile, $Notes, $utf8NoBom)

if ($EditOnly) {
    Write-Host "Updating GitHub release notes for $Tag ..." -ForegroundColor Cyan
    & gh release edit $Tag --notes-file $notesFile
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "Updated release notes for $Tag" -ForegroundColor Green
    exit 0
}

if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot "publish-windows.ps1")
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    & (Join-Path $PSScriptRoot "publish-android.ps1")
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$winZip = Join-Path $distRoot "Rule34Gallery-win-x64.zip"
$apkSource = Join-Path $distRoot "R34Browser-release.apk"
$apkStable = Join-Path $distRoot "R34Browser.apk"

if (-not (Test-Path $winZip)) {
    throw "Missing desktop zip: $winZip. Run scripts/publish-windows.ps1 first."
}

if (-not (Test-Path $apkSource)) {
    throw "Missing Android APK: $apkSource. Run scripts/publish-android.ps1 first."
}

Copy-Item $apkSource $apkStable -Force

$stageDir = Join-Path $env:TEMP "Rule34Gallery-release-$Tag"
if (Test-Path $stageDir) {
    Remove-Item $stageDir -Recurse -Force
}
New-Item -ItemType Directory -Path $stageDir -Force | Out-Null
Copy-Item $winZip (Join-Path $stageDir "Rule34Gallery-win-x64.zip") -Force
Copy-Item $apkStable (Join-Path $stageDir "R34Browser.apk") -Force

Write-Host "Creating GitHub release $Tag ..." -ForegroundColor Cyan
$releaseArgs = @(
    "release", "create", $Tag,
    "--title", $Title,
    "--notes-file", $notesFile,
    (Join-Path $stageDir "Rule34Gallery-win-x64.zip"),
    (Join-Path $stageDir "R34Browser.apk")
)

if ($Draft) {
    $releaseArgs += "--draft"
}

& gh @releaseArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Published $Tag" -ForegroundColor Green
Write-Host "  Rule34Gallery-win-x64.zip"
Write-Host "  R34Browser.apk"
Write-Host "  Release notes from changelog.md ($version)"
