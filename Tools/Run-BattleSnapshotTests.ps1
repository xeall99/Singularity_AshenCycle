param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (-not (Test-Path -LiteralPath $UnityEditor)) { throw "Unity Editor missing: $UnityEditor" }
$runRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('syngrava-battle-tests-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runRoot | Out-Null

# Unity cannot open the same project twice. Copy inputs, never the user's live Library.
# A distinct product name isolates Application.persistentDataPath from the user's save.
foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $folder) -Destination $runRoot -Recurse
}
$settingsPath = Join-Path $runRoot 'ProjectSettings/ProjectSettings.asset'
$settings = [System.IO.File]::ReadAllText($settingsPath)
$settings = $settings -replace '(?m)^  productName:.*$', '  productName: SYNGRAVA_BattleTests'
$settings = $settings -replace '(?m)^  companyName:.*$', ('  companyName: ' + (Split-Path $runRoot -Leaf))
[System.IO.File]::WriteAllText($settingsPath, $settings)

# Use exactly the already-resolved package sources, avoiding network/version changes.
# All overrides are in the disposable project; the real manifest/lock stay unchanged.
$manifestPath = Join-Path $runRoot 'Packages/manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$cachePath = Join-Path $projectRoot 'Library/PackageCache'
foreach ($package in Get-ChildItem -LiteralPath $cachePath -Directory) {
    $packageJson = Join-Path $package.FullName 'package.json'
    if (-not (Test-Path -LiteralPath $packageJson)) { continue }
    $info = Get-Content -LiteralPath $packageJson -Raw | ConvertFrom-Json
    if ($info.name -like 'com.unity.modules.*') { continue }
    $manifest.dependencies | Add-Member -NotePropertyName $info.name -NotePropertyValue ('file:' + $package.FullName.Replace('\', '/')) -Force
}
[System.IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 20))

$results = Join-Path $runRoot 'playmode-results.xml'
$log = Join-Path $runRoot 'unity-playmode.log'
Write-Output "Isolated project: $runRoot"
Write-Output "Results: $results"
Write-Output "Unity log: $log"
$unityArgs = @('-batchmode', '-projectPath', ('"' + $runRoot + '"'),
    '-runTests', '-testPlatform', 'PlayMode', '-testFilter', 'BattleSnapshotTests',
    '-testResults', ('"' + $results + '"'), '-logFile', ('"' + $log + '"'))
$process = Start-Process -FilePath $UnityEditor -ArgumentList $unityArgs -WindowStyle Hidden -PassThru
$process.WaitForExit()
if (Test-Path -LiteralPath $results) {
    [xml]$report = Get-Content -LiteralPath $results -Raw
    $report.'test-run' | Select-Object result, total, passed, failed, skipped, duration | Format-List
}
if ($process.ExitCode -ne 0) { throw "Unity exited with code $($process.ExitCode). See $log" }
if (-not (Test-Path -LiteralPath $results)) { throw 'Unity did not produce a test report.' }
if ($report.'test-run'.result -ne 'Passed') { throw "Tests did not pass. See $results" }
