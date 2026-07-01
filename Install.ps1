# Installs Rule34 Gallery from dist/Rule34Gallery-win-x64 (run publish-windows.ps1 first if missing).

$ErrorActionPreference = "Stop"
$repoRoot = $PSScriptRoot
. (Join-Path $repoRoot "scripts\windows-paths.ps1")
$appDir = Join-Path $repoRoot "dist\$R34WindowsDistFolder"
$installer = Join-Path $appDir "Install.ps1"

try {
    Assert-R34WindowsDistFolder -DistDir $appDir
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "Build Rule34 Gallery first:" -ForegroundColor Yellow
    Write-Host "  powershell -ExecutionPolicy Bypass -File scripts\publish-windows.ps1"
    exit 1
}

if (-not (Test-Path $installer)) {
    Write-Host "Installer not found: $installer" -ForegroundColor Red
    Write-Host "Build Rule34 Gallery first:" -ForegroundColor Yellow
    Write-Host "  powershell -ExecutionPolicy Bypass -File scripts\publish-windows.ps1"
    exit 1
}

& $installer -SourceDir $appDir @args
