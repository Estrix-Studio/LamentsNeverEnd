param(
    [Parameter(Mandatory = $true)][string]$UnityPath,
    [ValidateSet('UPM', 'UnityPackage')][string]$Format = 'UPM',
    [string]$ArchivePath,
    [string]$EditorVersion,
    [string]$UrpVersion,
    [string]$TestFrameworkVersion = '1.9.0',
    [string]$WorkDirectory
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$editor = (Resolve-Path -LiteralPath $UnityPath).Path
$manifest = Get-Content -LiteralPath (Join-Path $projectRoot 'Assets/LoopShift/package.json') -Raw | ConvertFrom-Json
$defaultArchive = if ($Format -eq 'UPM') { "$($manifest.name)-$($manifest.version).tgz" } else { "LoopShift-$($manifest.version).unitypackage" }
if (!$ArchivePath) { $ArchivePath = Join-Path $projectRoot "Build/LoopShift/$defaultArchive" }
$archive = (Resolve-Path -LiteralPath $ArchivePath).Path.Replace('\', '/')
if (!$WorkDirectory) { $WorkDirectory = Join-Path $projectRoot ("Build/LoopShift/Consumer-$Format-" + [Guid]::NewGuid().ToString('N')) }
if (Test-Path -LiteralPath $WorkDirectory) { throw 'Use a new consumer directory to avoid overwriting a project.' }
$consumer = [IO.Path]::GetFullPath($WorkDirectory)
New-Item -ItemType Directory -Force -Path "$consumer/Assets/Editor", "$consumer/Packages", "$consumer/ProjectSettings" | Out-Null
if ($EditorVersion) {
    if ($EditorVersion -notmatch '^\d+\.\d+\.\d+[afbp]\d+$') { throw 'Invalid editor version.' }
    "m_EditorVersion: $EditorVersion" | Set-Content -LiteralPath "$consumer/ProjectSettings/ProjectVersion.txt"
} else {
    Copy-Item -LiteralPath (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt') -Destination "$consumer/ProjectSettings"
}
$dependencies = @{ 'com.unity.test-framework' = $TestFrameworkVersion; 'com.unity.modules.physics2d' = '1.0.0'; 'com.unity.modules.imgui' = '1.0.0' }
if ($UrpVersion) { $dependencies['com.unity.render-pipelines.universal'] = $UrpVersion }
$consumerManifest = @{ dependencies = $dependencies }
if ($Format -eq 'UPM') {
    $dependencies[$manifest.name] = "file:$archive"
    $consumerManifest.testables = @($manifest.name)
}
$consumerManifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath "$consumer/Packages/manifest.json"

function Invoke-ConsumerUnity([string]$Phase, [string]$TaskArguments) {
    # Editor-window tests require a graphics device.
    $arguments = '-batchmode -projectPath "{0}" {1} -logFile "{0}/{2}.log"' -f $consumer, $TaskArguments, $Phase
    $process = Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "$Phase failed with exit $($process.ExitCode). See $consumer/$Phase.log" }
    Write-Output "$Phase passed."
}
if ($Format -eq 'UnityPackage') { Invoke-ConsumerUnity 'import' ('-quit -importPackage "{0}"' -f $archive) }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LoopShiftValidation/ConsumerVerification.cs') -Destination "$consumer/Assets/Editor"
Invoke-ConsumerUnity 'prepare' '-quit -executeMethod LoopShiftConsumerVerification.Prepare'
if ($UrpVersion) {
    Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets/Editor/Validation/LoopShiftRenderingChecks.cs') -Destination "$consumer/Assets/Editor"
    Invoke-ConsumerUnity 'configure-urp' '-quit -executeMethod LamentsNeverEnd.Editor.LoopShiftRenderingChecks.ConfigureConsumer'
    Invoke-ConsumerUnity 'rendering' '-quit -executeMethod LamentsNeverEnd.Editor.LoopShiftRenderingChecks.Run'
}
New-Item -ItemType Directory -Path "$consumer/Assets/Tests", "$consumer/Assets/Validation" | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LoopShiftValidation/SampleSmokeTests.cs') -Destination "$consumer/Assets/Tests"
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LoopShiftValidation/PlayerSmokeCheck.cs') -Destination "$consumer/Assets/Validation"
@{
    name = 'LoopShift.Consumer.Tests'
    references = @('LoopShift.Runtime', 'LoopShift.Samples')
    optionalUnityReferences = @('TestAssemblies')
    autoReferenced = $false
} | ConvertTo-Json | Set-Content -LiteralPath "$consumer/Assets/Tests/LoopShift.Consumer.Tests.asmdef"
New-Item -ItemType Directory -Path "$consumer/Assets/ReloadTests" | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LoopShiftValidation/ReloadModeChecks.cs') -Destination "$consumer/Assets/ReloadTests"
@{
    name = 'LoopShift.Consumer.Editor.Tests'
    references = @('LoopShift.Runtime', 'LoopShift.Samples')
    includePlatforms = @('Editor')
    optionalUnityReferences = @('TestAssemblies')
    autoReferenced = $false
} | ConvertTo-Json | Set-Content -LiteralPath "$consumer/Assets/ReloadTests/LoopShift.Consumer.Editor.Tests.asmdef"
Invoke-ConsumerUnity 'reloadmodes' ('-runTests -testPlatform EditMode -assemblyNames LoopShift.Consumer.Editor.Tests -testResults "{0}/reloadmodes.xml"' -f $consumer)
$suites = @('playmode', 'reloadmodes')
if ($Format -eq 'UPM') {
    Invoke-ConsumerUnity 'editmode' ('-runTests -testPlatform EditMode -assemblyNames LoopShift.Tests.Editor -testResults "{0}/editmode.xml"' -f $consumer)
    $suites += 'editmode'
}
Invoke-ConsumerUnity 'playmode' ('-runTests -testPlatform PlayMode -testResults "{0}/playmode.xml"' -f $consumer)
Invoke-ConsumerUnity 'build' '-quit -executeMethod LoopShiftConsumerVerification.Build'
$playerLog = Join-Path $consumer 'player-smoke.log'
$player = Start-Process -FilePath "$consumer/Build/LoopShiftDemo.exe" -ArgumentList ('-batchmode -loopshift-smoke -logFile "{0}"' -f $playerLog) -WindowStyle Hidden -PassThru
if (!$player.WaitForExit(90000)) { throw "Player smoke check is still running (PID $($player.Id)); inspect it before starting another run. Log: $playerLog" }
if ($player.ExitCode -ne 0 -or (Get-Content -LiteralPath $playerLog -Raw) -notmatch 'LOOPSHIFT PLAYER SMOKE PASSED') {
    throw "Player smoke failed (exit $($player.ExitCode)). See $playerLog"
}
$results = @{}
foreach ($suite in $suites) {
    [xml]$result = Get-Content -LiteralPath "$consumer/$suite.xml"
    if ([int]$result.'test-run'.failed -gt 0 -or [int]$result.'test-run'.passed -eq 0) { throw "Invalid $suite test result." }
    $results[$suite] = [int]$result.'test-run'.passed
    Write-Output "$suite : $($result.'test-run'.passed) passed."
}
@{
    urpVersion = $UrpVersion
    format = $Format
    archive = $archive
    sha256 = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
    editor = (Get-Content "$consumer/ProjectSettings/ProjectVersion.txt" -Raw).Trim()
    suites = $results
    playerSmokePassed = $true
    visualInspectionPassed = $false
    consumer = $consumer
} | ConvertTo-Json -Depth 5 | Set-Content "$consumer/verification.json"
Write-Output 'Player smoke passed.'
Write-Output "Verified consumer project: $consumer"
