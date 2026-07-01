$ErrorActionPreference = "Stop"

# Hook payload is sent via stdin.
$rawInput = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($rawInput)) {
    $pipelineInput = @($input) -join [Environment]::NewLine
    if (-not [string]::IsNullOrWhiteSpace($pipelineInput)) {
        $rawInput = $pipelineInput
    }
}
if ([string]::IsNullOrWhiteSpace($rawInput)) {
    exit 0
}

$payload = $null
try {
    $payload = $rawInput | ConvertFrom-Json -Depth 100
} catch {
    # If payload is malformed, fail open.
    exit 0
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
. (Join-Path $repoRoot "scripts\windows-paths.ps1")
$payloadText = $rawInput.ToLowerInvariant()

# Only trigger for real app/shared code edits.
# Support both slash styles and JSON-escaped backslashes.
$shouldRun = [regex]::IsMatch(
    $payloadText,
    'rule34gallery(\.core|app)(\\\\|\\|/)')

if (-not $shouldRun) {
    exit 0
}

Write-Host "[hook] Publishing updated Windows package..."
Push-Location $repoRoot
try {
    powershell -ExecutionPolicy Bypass -File ".cursor\hooks\bump-version-and-changelog.ps1" -RepoRoot "$repoRoot" -PayloadJson "$rawInput"

    powershell -ExecutionPolicy Bypass -File "scripts\publish-windows.ps1"

    $sourceDir = Join-Path $repoRoot "dist\$R34WindowsDistFolder"
    $installer = Join-Path $sourceDir "Install.ps1"
    if (-not (Test-Path $installer)) {
        Write-Warning "[hook] Installer not found after publish: $installer"
        exit 0
    }

    try {
        Assert-R34WindowsDistFolder -DistDir $sourceDir
    } catch {
        Write-Warning ("[hook] Refusing auto-install: " + $_.Exception.Message)
        exit 0
    }

    # Stop only Rule34 Gallery — never touch PH Browser.
    Stop-R34WindowsProcess

    Write-Host "[hook] Running installer update..."
    & $installer -SourceDir $sourceDir
    Write-Host "[hook] Publish + install update complete."
} catch {
    # Fail open so editing is never blocked.
    Write-Warning ("[hook] Auto publish/install failed: " + $_.Exception.Message)
    exit 0
} finally {
    Pop-Location
}

exit 0
