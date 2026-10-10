param(
    [Parameter(Mandatory = $true)]
    [string]$UnityPath
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$editor = (Resolve-Path -LiteralPath $UnityPath).Path
$logPath = Join-Path $projectRoot 'Logs/project-checks.log'
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $logPath) | Out-Null
$arguments = '-batchmode -nographics -quit -projectPath "{0}" -executeMethod LamentsNeverEnd.Editor.ProjectChecks.Run -logFile "{1}"' -f $projectRoot, $logPath
$process = Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
$exitCode = $process.ExitCode
$log = if (Test-Path -LiteralPath $logPath) { Get-Content -LiteralPath $logPath -Raw } else { '' }
if ($exitCode -ne 0 -or $log -notmatch 'PROJECT CHECKS PASSED:') {
    Write-Output $log
    throw "Project validation failed (Unity exit code $exitCode). See $logPath"
}
Write-Output ($log -split '\r?\n' | Where-Object { $_ -match 'PROJECT CHECKS PASSED:' })
Write-Output "Log: $logPath"