<#
.SYNOPSIS
  Stops only the API/MVC processes started by scripts/dev-run.ps1.

.DESCRIPTION
  Reads artifacts/run/dev-run.pids.json and stops those PIDs (process tree).
  Does not kill unrelated dotnet / Visual Studio / Cursor processes.

.EXAMPLE
  pwsh -File scripts/dev-stop.ps1
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$PidFile = Join-Path $RepoRoot "artifacts\run\dev-run.pids.json"

if (-not (Test-Path -LiteralPath $PidFile)) {
    Write-Host "No shadow-run PID file found ($PidFile). Nothing to stop."
    exit 0
}

try {
    $state = Get-Content -LiteralPath $PidFile -Raw | ConvertFrom-Json
}
catch {
    Write-Host "PID file is unreadable; removing it."
    Remove-Item -LiteralPath $PidFile -Force -ErrorAction SilentlyContinue
    exit 0
}

if ($null -eq $state -or [string]$state.starter -ne "scripts/dev-run.ps1") {
    Write-Host "PID file was not created by scripts/dev-run.ps1. Refusing to stop unknown processes."
    exit 1
}

function Stop-TrackedPid([int]$ProcessId, [string]$Label) {
    if ($ProcessId -le 0) {
        Write-Host "  ${Label}: no PID recorded"
        return
    }

    try {
        $proc = Get-Process -Id $ProcessId -ErrorAction Stop
    }
    catch {
        Write-Host "  ${Label} PID ${ProcessId}: already stopped"
        return
    }

    Write-Host "  Stopping ${Label} PID ${ProcessId} ($($proc.ProcessName)) ..."
    try {
        Stop-Process -Id $ProcessId -Force -ErrorAction Stop
    }
    catch {
        # Prefer tree kill when available (Windows)
        & taskkill /PID $ProcessId /T /F 2>$null | Out-Null
    }

    Start-Sleep -Milliseconds 400
    if (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue) {
        Write-Host "  WARNING: $Label PID $ProcessId is still running (access denied?). Stop it from Task Manager if needed."
    }
    else {
        Write-Host "  $Label stopped."
    }
}

Write-Host "Stopping shadow-run processes from $PidFile ..."
Stop-TrackedPid -ProcessId ([int]$state.mvcPid) -Label "MVC"
Stop-TrackedPid -ProcessId ([int]$state.apiPid) -Label "API"

Remove-Item -LiteralPath $PidFile -Force -ErrorAction SilentlyContinue
Write-Host "Done. bin/Debug is free for the next build (for these shadow processes)."
