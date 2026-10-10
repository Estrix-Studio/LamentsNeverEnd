param(
    [Parameter(Mandatory = $true)][string]$UnityPath
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$editor = (Resolve-Path -LiteralPath $UnityPath).Path
$source = Join-Path $projectRoot 'Assets/LoopShift'
$manifest = Get-Content -LiteralPath (Join-Path $source 'package.json') -Raw | ConvertFrom-Json
$output = Join-Path $projectRoot 'Build/LoopShift'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$log = Join-Path $output 'export.log'
$arguments = '-batchmode -nographics -quit -projectPath "{0}" -executeMethod LamentsNeverEnd.Editor.LoopShiftSampleBuilder.BuildDistribution -logFile "{1}"' -f $projectRoot, $log
$process = Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity package generation failed. See $log" }
$stage = Join-Path $output ('staging-' + [Guid]::NewGuid().ToString('N'))
$package = Join-Path $stage 'package'
New-Item -ItemType Directory -Force -Path $package | Out-Null
foreach ($item in @('Runtime', 'Editor', 'Tests', 'README.md', 'CHANGELOG.md', 'LICENSE', 'Third-Party Notices.txt')) {
    Copy-Item -LiteralPath (Join-Path $source $item) -Destination $package -Recurse
    $meta = Join-Path $source ($item + '.meta')
    if (Test-Path -LiteralPath $meta) { Copy-Item -LiteralPath $meta -Destination $package }
}
Copy-Item -LiteralPath (Join-Path $source 'Documentation') -Destination (Join-Path $package 'Documentation~') -Recurse
$samples = Join-Path $package 'Samples~'
New-Item -ItemType Directory -Path $samples | Out-Null
Copy-Item -LiteralPath (Join-Path $source 'Demo/MinimalTopDown') -Destination $samples -Recurse
$manifest.samples[0].path = 'Samples~/MinimalTopDown'
$manifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $package 'package.json') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $source 'package.json.meta') -Destination $package
$archive = Join-Path $output ($manifest.name + '-' + $manifest.version + '.tgz')
& tar -czf $archive -C $stage package
if ($LASTEXITCODE -ne 0) { throw 'UPM archive creation failed.' }
$files = @($archive, (Join-Path $output ('LoopShift-' + $manifest.version + '.unitypackage')))
$files | ForEach-Object { Get-FileHash -LiteralPath $_ -Algorithm SHA256 } | Select-Object Path, Hash | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'checksums.json')
Write-Output "UPM: $archive"
Write-Output "Asset archive: $($files[1])"
Write-Output "Staging: $package"
