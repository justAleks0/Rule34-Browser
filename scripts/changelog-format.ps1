# Shared changelog helpers for changelog.md, GitHub Releases, and version bump hook.

function Get-ChangelogPreamble {
    @"
# Changelog

All notable changes to this project will be documented here.

---
"@
}

function Get-ChangelogVersionHeader {
    param(
        [string]$Version,
        [datetime]$At = (Get-Date)
    )

    $time = $At.ToString("HH-mm")
    $date = $At.ToString("MM-dd-yyyy")
    return "## $Version - $time $date"
}

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

  # New: ## 2.0.83 - 14-30 07-02-2026
  # Legacy: ## 2.0.83 - 2026-07-02 or ## 2.0.83 - 2026-07-02
    $pattern = "(?ms)^##\s+$escaped(?:\s+-\s+[^\r\n]+)?\s*\r?\n(.*?)(?=^##\s+|\z)"
    if ($text -match $pattern) {
        $body = $Matches[1].Trim()
        $body = $body -replace '(?m)^---\s*$', ''
        return $body.Trim()
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

function New-ChangelogEntry {
    param(
        [string]$Version,
        [hashtable]$Sections,
        [datetime]$At = (Get-Date)
    )

    $header = Get-ChangelogVersionHeader -Version $Version -At $At
    $order = @('Added', 'Changed', 'Fixed', 'Removed', 'Notes')
  $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add($header)
    $lines.Add('')

    foreach ($name in $order) {
        $items = @($Sections[$name] | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        if ($items.Count -eq 0) {
            continue
        }

        $lines.Add("### $name")
        foreach ($item in $items) {
            $text = $item.Trim()
            if (-not $text.StartsWith('-')) {
                $text = "- $text"
            }

            $lines.Add($text)
        }

        $lines.Add('')
    }

    $lines.Add('---')
    $lines.Add('')
    return ($lines -join "`r`n")
}

function Insert-ChangelogEntry {
    param(
        [string]$ChangelogPath,
        [string]$Entry
    )

    $preamble = Get-ChangelogPreamble
    $rest = ""

    if (Test-Path $ChangelogPath) {
        $existing = Get-Content -Raw -Path $ChangelogPath
        if ($existing -match '(?ms)^#\s*Changelog\s*\r?\n(?:All notable changes[^\r\n]*\r?\n)?\r?\n?---\s*\r?\n+') {
            $rest = $existing -replace '(?ms)^#\s*Changelog\s*\r?\n(?:All notable changes[^\r\n]*\r?\n)?\r?\n?---\s*\r?\n+', ''
        }
        elseif ($existing -match '(?ms)^#\s*Changelog\s*\r?\n+') {
            $rest = $existing -replace '(?ms)^#\s*Changelog\s*\r?\n+', ''
        }
        else {
            $rest = $existing
        }
    }

    $content = $preamble + "`r`n`r`n" + $Entry + $rest.TrimStart()
    Set-Content -Path $ChangelogPath -Value $content -Encoding UTF8
}
