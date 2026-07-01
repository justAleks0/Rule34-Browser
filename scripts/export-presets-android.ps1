# Regenerate Android preset asset from Rule34Gallery.Core catalogs.
$base = Join-Path $PSScriptRoot "..\Rule34Gallery.Core\Services"
$out = "C:\Users\aleks\AndroidStudioProjects\R34Browser\app\src\main\assets\presets.json"
New-Item -ItemType Directory -Force -Path (Split-Path $out) | Out-Null

function Export-Presets($file) {
    $text = Get-Content $file -Raw
    $matches = [regex]::Matches($text, 'Id\s*=\s*"([^"]+)"[\s\S]*?Name\s*=\s*"([^"]*)"[\s\S]*?Description\s*=\s*"([^"]*)"[\s\S]*?Tags\s*=\s*\[([^\]]*)\]')
    $items = @()
    foreach ($m in $matches) {
        $tags = ($m.Groups[4].Value -split ',' | ForEach-Object { ($_ -replace '"', '').Trim() } | Where-Object { $_ })
        $items += [pscustomobject]@{ id = $m.Groups[1].Value; name = $m.Groups[2].Value; description = $m.Groups[3].Value; tags = $tags }
    }
    return $items
}

$search = Export-Presets (Join-Path $base "SearchPresetCatalog.cs")
$search += Export-Presets (Join-Path $base "SearchPresetCatalog.Extended.cs")
$black = Export-Presets (Join-Path $base "BlacklistPresetCatalog.cs")
$black += Export-Presets (Join-Path $base "BlacklistPresetCatalog.Extended.cs")
$json = @{ searchPresets = $search; blacklistPresets = $black } | ConvertTo-Json -Depth 5
# PowerShell emits single-element tag lists as strings; Kotlin expects JSON arrays.
$json = [regex]::Replace($json, '"tags":\s*"([^"]+)"', '"tags": ["$1"]')
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($out, $json, $utf8NoBom)
Write-Host "Wrote $out ($($search.Count) search, $($black.Count) blacklist)"
