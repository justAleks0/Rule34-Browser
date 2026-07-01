# Applies a staged Rule34 Gallery build after the running app exits.
param(
    [Parameter(Mandatory = $true)]
    [int]$ParentPid,
    [Parameter(Mandatory = $true)]
    [string]$SourceDir,
    [Parameter(Mandatory = $true)]
    [string]$InstallDir,
    [Parameter(Mandatory = $true)]
    [string]$ExePath
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "windows-paths.ps1")

Write-Host "Waiting for Rule34 Gallery (PID $ParentPid) to exit..." -ForegroundColor Cyan
while (Get-Process -Id $ParentPid -ErrorAction SilentlyContinue) {
    Start-Sleep -Milliseconds 400
}

Stop-R34WindowsProcess
Start-Sleep -Seconds 1

if (-not (Test-Path $SourceDir)) {
    throw "Staged update folder not found: $SourceDir"
}

Assert-R34WindowsSource -SourceDir $SourceDir

$configPath = Join-Path $InstallDir "firebase-config.json"
$configBackup = Join-Path $env:TEMP "Rule34Gallery-firebase-config.update.json"
$hadConfig = $false

if (Test-Path $configPath) {
    Copy-Item $configPath $configBackup -Force
    $hadConfig = $true
    Write-Host "Backed up firebase-config.json" -ForegroundColor DarkGray
}

if (-not (Test-Path $InstallDir)) {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
}

Write-Host "Installing update to $InstallDir ..." -ForegroundColor Cyan
Get-ChildItem -Path $InstallDir -Force -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notin @("firebase-config.json") } |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Copy-Item -Path (Join-Path $SourceDir "*") -Destination $InstallDir -Recurse -Force

if ($hadConfig -and (Test-Path $configBackup)) {
    Copy-Item $configBackup $configPath -Force
    Remove-Item $configBackup -Force
    Write-Host "Restored firebase-config.json" -ForegroundColor Green
}

$launchExe = if (Test-Path $ExePath) { $ExePath } else { Join-Path $InstallDir $R34WindowsExe }
if (-not (Test-Path $launchExe)) {
    throw "Could not find executable after update: $launchExe"
}

Write-Host "Starting Rule34 Gallery..." -ForegroundColor Green
Start-Process -FilePath $launchExe -WorkingDirectory $InstallDir
