<#
.SYNOPSIS
  Development shadow-run: build to bin/Debug, copy to artifacts/run/{api,mvc}, then start from the copies.

.DESCRIPTION
  Running processes lock files under artifacts/run only, so the next build can freely
  overwrite bin/Debug/net8.0 (avoids MSB3027 / MSB3021).

  Tracks PIDs in artifacts/run/dev-run.pids.json for scripts/dev-stop.ps1.
  Refuses to start a second instance while those PIDs are still alive.

.EXAMPLE
  pwsh -File scripts/dev-run.ps1
#>
[CmdletBinding()]
param(
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$ApiProject = Join-Path $RepoRoot "FinalProject\FinalProject.csproj"
$MvcProject = Join-Path $RepoRoot "FinalProject.MVC\FinalProject.MVC.csproj"
$ApiBin = Join-Path $RepoRoot "FinalProject\bin\Debug\net8.0"
$MvcBin = Join-Path $RepoRoot "FinalProject.MVC\bin\Debug\net8.0"
$RunRoot = Join-Path $RepoRoot "artifacts\run"
$ApiShadow = Join-Path $RunRoot "api"
$MvcShadow = Join-Path $RunRoot "mvc"
$PidFile = Join-Path $RunRoot "dev-run.pids.json"
$ApiUrls = "https://localhost:7268;http://localhost:5051"
$MvcUrls = "https://localhost:7245;http://localhost:5146"

function Test-PidAlive([int]$ProcessId) {
    if ($ProcessId -le 0) { return $false }
    try {
        $p = Get-Process -Id $ProcessId -ErrorAction Stop
        return $null -ne $p
    }
    catch {
        return $false
    }
}

function Read-DevRunState {
    if (-not (Test-Path -LiteralPath $PidFile)) { return $null }
    try {
        return Get-Content -LiteralPath $PidFile -Raw | ConvertFrom-Json
    }
    catch {
        return $null
    }
}

function Assert-NotAlreadyRunning {
    $state = Read-DevRunState
    if ($null -eq $state) { return }

    $apiAlive = Test-PidAlive ([int]$state.apiPid)
    $mvcAlive = Test-PidAlive ([int]$state.mvcPid)
    if (-not $apiAlive -and -not $mvcAlive) {
        Remove-Item -LiteralPath $PidFile -Force -ErrorAction SilentlyContinue
        return
    }

    Write-Host "Shadow-run already active (apiPid=$($state.apiPid) alive=$apiAlive, mvcPid=$($state.mvcPid) alive=$mvcAlive)."
    Write-Host "Stop first:  pwsh -File scripts/dev-stop.ps1"
    exit 1
}

function Invoke-ProjectBuild([string]$ProjectPath) {
    Write-Host "Building $ProjectPath ..."
    & dotnet build $ProjectPath -c Debug --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for $ProjectPath (exit $LASTEXITCODE). If MSB3027 appears, stop leftover bin/Debug runners, then retry."
    }
}

function Sync-Shadow([string]$Source, [string]$Destination) {
    if (-not (Test-Path -LiteralPath $Source)) {
        throw "Build output missing: $Source"
    }
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    # /MIR keeps shadow aligned with bin/Debug without touching bin/Debug itself.
    & robocopy $Source $Destination /MIR /R:2 /W:1 /NFL /NDL /NJH /NJS /NP /XD refs runtimes\.git | Out-Null
    $code = $LASTEXITCODE
    if ($code -ge 8) {
        throw "robocopy failed copying '$Source' -> '$Destination' (exit $code). Is a previous shadow process still locking files? Run scripts/dev-stop.ps1."
    }
}

function Start-ShadowProcess {
    param(
        [Parameter(Mandatory)][string]$DllPath,
        [Parameter(Mandatory)][string]$WorkingDirectory,
        [Parameter(Mandatory)][string]$Urls,
        [Parameter(Mandatory)][string]$HttpsPort
    )

    if (-not (Test-Path -LiteralPath $DllPath)) {
        throw "DLL not found: $DllPath"
    }

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = "dotnet"
    $psi.Arguments = "`"$DllPath`""
    $psi.WorkingDirectory = $WorkingDirectory
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardOutput = $false
    $psi.RedirectStandardError = $false
    # Windows PowerShell 5.1 uses EnvironmentVariables (not Environment).
    $psi.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development"
    $psi.EnvironmentVariables["ASPNETCORE_URLS"] = $Urls
    $psi.EnvironmentVariables["ASPNETCORE_HTTPS_PORT"] = $HttpsPort
    $psi.EnvironmentVariables["FINALPROJECT_SHADOW_RUN"] = "1"
    $psi.EnvironmentVariables["FINALPROJECT_REPO_ROOT"] = $RepoRoot

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi
    if (-not $proc.Start()) {
        throw "Failed to start $DllPath"
    }
    return $proc
}

function Wait-HttpReady([string]$Url, [int]$TimeoutSeconds = 120) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            # Prefer HTTP probe to avoid local-dev certificate prompts.
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                return
            }
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }
    throw "Timed out waiting for $Url"
}

Assert-NotAlreadyRunning

New-Item -ItemType Directory -Force -Path $ApiShadow, $MvcShadow | Out-Null

if (-not $SkipBuild) {
    Invoke-ProjectBuild $ApiProject
    Invoke-ProjectBuild $MvcProject
}

Sync-Shadow -Source $ApiBin -Destination $ApiShadow
Sync-Shadow -Source $MvcBin -Destination $MvcShadow

Write-Host "Starting API from $ApiShadow ..."
$apiProc = Start-ShadowProcess `
    -DllPath (Join-Path $ApiShadow "FinalProject.dll") `
    -WorkingDirectory $ApiShadow `
    -Urls $ApiUrls `
    -HttpsPort "7268"

try {
    Wait-HttpReady "http://localhost:5051/api/cars/lookups" 120
}
catch {
    if (-not $apiProc.HasExited) { Stop-Process -Id $apiProc.Id -Force -ErrorAction SilentlyContinue }
    throw
}

Write-Host "Starting MVC from $MvcShadow ..."
$mvcProc = Start-ShadowProcess `
    -DllPath (Join-Path $MvcShadow "FinalProject.MVC.dll") `
    -WorkingDirectory $MvcShadow `
    -Urls $MvcUrls `
    -HttpsPort "7245"

# Give MVC a moment to bind; DevelopmentApiHost will no-op because API is already up.
Start-Sleep -Seconds 2
if ($mvcProc.HasExited) {
    if (-not $apiProc.HasExited) { Stop-Process -Id $apiProc.Id -Force -ErrorAction SilentlyContinue }
    throw "MVC process exited immediately (PID $($mvcProc.Id)). Check HTTPS cert / ports."
}

$state = [ordered]@{
    startedAt = (Get-Date).ToString("o")
    apiPid    = $apiProc.Id
    mvcPid    = $mvcProc.Id
    apiShadow = $ApiShadow
    mvcShadow = $MvcShadow
    starter   = "scripts/dev-run.ps1"
}
($state | ConvertTo-Json) | Set-Content -LiteralPath $PidFile -Encoding UTF8

Write-Host ""
Write-Host "Shadow-run started."
Write-Host "  API  PID $($apiProc.Id)  https://localhost:7268  (shadow: artifacts/run/api)"
Write-Host "  MVC  PID $($mvcProc.Id)  https://localhost:7245  (shadow: artifacts/run/mvc)"
Write-Host "Stop with:  pwsh -File scripts/dev-stop.ps1"
Write-Host "You can rebuild bin/Debug while these stay running (no MSB3027 from these processes)."
