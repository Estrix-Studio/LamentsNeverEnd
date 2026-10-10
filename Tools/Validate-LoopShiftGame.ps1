param([Parameter(Mandatory = $true)][string]$UnityPath)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$editor = (Resolve-Path -LiteralPath $UnityPath).Path
$log = Join-Path $root 'Logs/loopshift-game-checks.log'
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $log) | Out-Null
# Graphics stays enabled because the installed Asset Store Tools updater may open an EditorWindow.
$arguments = '-batchmode -projectPath "{0}" -executeMethod LamentsNeverEnd.Editor.LoopShiftGameChecks.Run -logFile "{1}"' -f $root, $log
$process = Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
$content = Get-Content -LiteralPath $log -Raw
if ($process.ExitCode -ne 0 -or $content -notmatch 'LOOPSHIFT GAME CHECKS PASSED:') {
    throw "Game integration failed (exit $($process.ExitCode)). See $log"
}
Write-Output ($content -split '\r?\n' | Where-Object { $_ -match 'LOOPSHIFT GAME CHECKS PASSED:' })
Write-Output "Log: $log"
