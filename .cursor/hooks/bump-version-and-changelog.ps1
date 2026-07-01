param(
    [string]$RepoRoot,
    [string]$PayloadJson = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    exit 0
}

$desktopCsproj = Join-Path $RepoRoot "Rule34GalleryApp\Rule34GalleryApp.csproj"
$androidGradle = "C:\Users\aleks\AndroidStudioProjects\R34Browser\app\build.gradle.kts"
$changelogPath = Join-Path $RepoRoot "changelog.md"

if (-not (Test-Path $desktopCsproj)) { exit 0 }
if (-not (Test-Path $androidGradle)) { exit 0 }

function Get-SemVerParts([string]$versionText) {
    if ([string]::IsNullOrWhiteSpace($versionText)) { return @(1,0,0) }
    $parts = $versionText.Trim().Split('.')
    $nums = @()
    foreach ($p in $parts) {
        $n = 0
        [void][int]::TryParse($p, [ref]$n)
        $nums += $n
    }
    while ($nums.Count -lt 3) { $nums += 0 }
    return @($nums[0], $nums[1], $nums[2])
}

function Get-ChangeProfile {
    param([string]$repo)
    $files = 0
    $lines = 0

    try {
        $isRepo = (& git -C $repo rev-parse --is-inside-work-tree 2>$null).Trim()
        if ($isRepo -eq "true") {
            $stats = & git -C $repo diff --numstat -- "Rule34GalleryApp" "Rule34Gallery.Core"
            foreach ($row in $stats) {
                if ([string]::IsNullOrWhiteSpace($row)) { continue }
                $cols = $row -split "\s+"
                if ($cols.Length -lt 3) { continue }
                $add = 0
                $del = 0
                [void][int]::TryParse($cols[0], [ref]$add)
                [void][int]::TryParse($cols[1], [ref]$del)
                $files++
                $lines += ($add + $del)
            }
        }
    } catch {
        # fall back below
    }

    if ($files -eq 0) {
        # No diff info available from git; default safe bump.
        return @{ tier = "small"; reason = "fallback" }
    }

    if ($files -ge 8 -or $lines -ge 240) {
        return @{ tier = "big"; reason = "$files files, $lines lines" }
    }
    if ($files -ge 3 -or $lines -ge 60) {
        return @{ tier = "medium"; reason = "$files files, $lines lines" }
    }
    return @{ tier = "small"; reason = "$files files, $lines lines" }
}

$csprojRaw = Get-Content -Raw -Path $desktopCsproj
$versionMatch = [regex]::Match($csprojRaw, "<Version>([^<]+)</Version>")
if (-not $versionMatch.Success) { exit 0 }
$currentVersion = $versionMatch.Groups[1].Value.Trim()
$v = Get-SemVerParts $currentVersion
$major = $v[0]; $minor = $v[1]; $patch = $v[2]

$profile = Get-ChangeProfile -repo $RepoRoot
$tier = $profile.tier

switch ($tier) {
    "big"    { $major += 1; $minor = 0; $patch = 0 }
    "medium" { $minor += 1; $patch = 0 }
    default  { $patch += 1 }
}

$newVersion = "$major.$minor.$patch"
if ($newVersion -eq $currentVersion) { exit 0 }

$updatedCsproj = [regex]::Replace($csprojRaw, "<Version>[^<]+</Version>", "<Version>$newVersion</Version>", 1)
Set-Content -Path $desktopCsproj -Value $updatedCsproj -Encoding UTF8

$androidRaw = Get-Content -Raw -Path $androidGradle
$androidRaw = [regex]::Replace($androidRaw, 'versionName\s*=\s*"[^"]+"', "versionName = `"$newVersion`"", 1)

$codeMatch = [regex]::Match($androidRaw, 'versionCode\s*=\s*(\d+)')
if ($codeMatch.Success) {
    $code = 1
    [void][int]::TryParse($codeMatch.Groups[1].Value, [ref]$code)
    $nextCode = $code + 1
    $androidRaw = [regex]::Replace($androidRaw, 'versionCode\s*=\s*\d+', "versionCode = $nextCode", 1)
}
Set-Content -Path $androidGradle -Value $androidRaw -Encoding UTF8

if (-not (Test-Path $changelogPath)) {
    Set-Content -Path $changelogPath -Encoding UTF8 -Value "# Changelog`r`n`r`n"
}

$changelogNotes = @()
if (-not [string]::IsNullOrWhiteSpace($PayloadJson)) {
    try {
        $payload = $PayloadJson | ConvertFrom-Json
        if ($payload.changelog_notes) {
            $changelogNotes = @($payload.changelog_notes | ForEach-Object { "$_".Trim() } | Where-Object { $_ })
        }
    } catch {
        # ignore malformed payload
    }
}

$existing = Get-Content -Raw -Path $changelogPath
$date = (Get-Date).ToString("yyyy-MM-dd")
$bulletLines = if ($changelogNotes.Count -gt 0) {
    ($changelogNotes | ForEach-Object { "- $_" }) -join "`r`n"
} else {
    "- Maintenance release ($tier change: $($profile.reason))."
}
$entry = @"
## $newVersion - $date

$bulletLines

"@

if ($existing -match '^\s*#\s*Changelog\s*') {
    $rest = $existing -replace '^\s*#\s*Changelog\s*\r?\n+', ''
    $newChangelog = "# Changelog`r`n`r`n$entry$rest"
} else {
    $newChangelog = "# Changelog`r`n`r`n$entry$existing"
}
Set-Content -Path $changelogPath -Value $newChangelog -Encoding UTF8

$syncScript = Join-Path $RepoRoot "scripts\sync-changelog-android.ps1"
if (Test-Path $syncScript) {
    & powershell -ExecutionPolicy Bypass -File $syncScript
}

Write-Host "[hook] Version bumped $currentVersion -> $newVersion ($tier)."
exit 0

