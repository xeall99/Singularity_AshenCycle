param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe',
    [string]$OutputPath = '',
    [string]$TestProfileName = ''
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (-not (Test-Path -LiteralPath $UnityEditor)) { throw "Unity Editor missing: $UnityEditor" }

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $projectRoot 'Builds\Windows\SYNGRAVA.exe'
}

$outputDir = Split-Path $OutputPath -Parent
if (-not (Test-Path -LiteralPath $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

$runRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('syngrava-build-' + [guid]::NewGuid().ToString('N'))
$resolvedTempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd('\', '/')
$resolvedRunRoot = [System.IO.Path]::GetFullPath($runRoot).TrimEnd('\', '/')
if (-not $resolvedRunRoot.StartsWith($resolvedTempRoot + [System.IO.Path]::DirectorySeparatorChar,
        [System.StringComparison]::OrdinalIgnoreCase) -or
    (Split-Path $resolvedRunRoot -Leaf) -notmatch '^syngrava-build-[0-9a-f]{32}$') {
    throw "Refusing to use an unexpected temporary build path: $resolvedRunRoot"
}
New-Item -ItemType Directory -Path $runRoot | Out-Null

try {
    Write-Output "Preparing isolated build project at: $runRoot"
    
    # Unity cannot open the same project twice if the Editor is open.
    # Copy Assets, Packages, ProjectSettings to isolated root.
    foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) {
        Copy-Item -LiteralPath (Join-Path $projectRoot $folder) -Destination $runRoot -Recurse
    }

    if (-not [string]::IsNullOrWhiteSpace($TestProfileName)) {
        if ($TestProfileName -notmatch '^[A-Za-z][A-Za-z0-9_]{4,63}$') {
            throw 'TestProfileName must be 5-64 letters, digits, or underscores, starting with a letter.'
        }
        $settingsPath = Join-Path $runRoot 'ProjectSettings/ProjectSettings.asset'
        $settings = [System.IO.File]::ReadAllText($settingsPath)
        if ($settings -notmatch '(?m)^  companyName: .+$' -or
            $settings -notmatch '(?m)^  productName: .+$') {
            throw 'Company/product identity was not found in copied ProjectSettings.asset.'
        }
        $settings = $settings -replace '(?m)^  companyName: .+$', '  companyName: CodexGateTests'
        $settings = $settings -replace '(?m)^  productName: .+$', ('  productName: ' + $TestProfileName)
        [System.IO.File]::WriteAllText($settingsPath, $settings)
        Write-Output "Isolated player identity: CodexGateTests/$TestProfileName"
    }

    # Use already-resolved package sources to avoid UPM network requests.
    $manifestPath = Join-Path $runRoot 'Packages/manifest.json'
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $cachePath = Join-Path $projectRoot 'Library/PackageCache'
    if (Test-Path -LiteralPath $cachePath) {
        foreach ($package in Get-ChildItem -LiteralPath $cachePath -Directory) {
            $packageJson = Join-Path $package.FullName 'package.json'
            if (-not (Test-Path -LiteralPath $packageJson)) { continue }
            $info = Get-Content -LiteralPath $packageJson -Raw | ConvertFrom-Json
            if ($info.name -like 'com.unity.modules.*') { continue }
            $manifest.dependencies | Add-Member -NotePropertyName $info.name -NotePropertyValue ('file:' + $package.FullName.Replace('\', '/')) -Force
        }
        [System.IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 20))
    }

    $log = Join-Path $runRoot 'unity-build.log'
    Write-Output "Building Windows x86_64 to: $OutputPath"
    Write-Output "Build log: $log"

    $prevEnv = [System.Environment]::GetEnvironmentVariable('SYNGRAVA_BUILD_OUTPUT', 'Process')
    [System.Environment]::SetEnvironmentVariable('SYNGRAVA_BUILD_OUTPUT', $OutputPath, 'Process')

    $unityArgs = @(
        '-batchmode',
        '-nographics',
        '-projectPath', ('"' + $runRoot + '"'),
        '-executeMethod', 'BuildScript.BuildWindowsDev',
        '-logFile', ('"' + $log + '"')
    )

    $process = Start-Process -FilePath $UnityEditor -ArgumentList $unityArgs -WindowStyle Hidden -PassThru
    $process.WaitForExit()

    [System.Environment]::SetEnvironmentVariable('SYNGRAVA_BUILD_OUTPUT', $prevEnv, 'Process')

    if (Test-Path -LiteralPath $log) {
        Copy-Item -LiteralPath $log -Destination (Join-Path $outputDir 'unity-build.log') -Force
        Write-Output "--- Build Log Tail ---"
        Get-Content -LiteralPath $log -Tail 40
    }

    if ($process.ExitCode -ne 0) {
        throw "Unity build exited with error code $($process.ExitCode). Check log: $log"
    }

    if (-not (Test-Path -LiteralPath $OutputPath)) {
        throw "Build executable was not found at: $OutputPath"
    }

    $exeItem = Get-Item -LiteralPath $OutputPath
    Write-Output "`n=========================================="
    Write-Output "BUILD SUCCESSFUL!"
    Write-Output "Executable: $($exeItem.FullName)"
    Write-Output "Size: $('{0:N0}' -f $exeItem.Length) bytes"
    Write-Output "=========================================="
}
finally {
    if (Test-Path -LiteralPath $resolvedRunRoot) {
        Write-Output "Cleaning up temporary build folder..."
        Remove-Item -LiteralPath $resolvedRunRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
