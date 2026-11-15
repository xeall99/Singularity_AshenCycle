param([Parameter(Mandatory=$true)][string]$ProfilePath)
$ErrorActionPreference = 'Stop'
$expected = Join-Path $env:USERPROFILE 'AppData\LocalLow\CodexGateTests\SYNGRAVA_AnomalyGlyphGate_20260930'
if ([IO.Path]::GetFullPath($ProfilePath).TrimEnd('\') -ne [IO.Path]::GetFullPath($expected).TrimEnd('\')) {
    throw 'This helper may only lock the isolated AnomalyGlyph test profile.'
}
$temporaryPath = Join-Path $ProfilePath 'Saves\syngrava-active-run-v0.4.json.tmp'
$releasePath = Join-Path $PSScriptRoot 'release-checkpoint-lock.signal'
if (Test-Path -LiteralPath $releasePath) { throw 'Release signal already exists.' }
$lockStream = [IO.FileStream]::new($temporaryPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
try {
    Write-Output "LOCK_READY: $temporaryPath"
    $deadline = [DateTime]::UtcNow.AddMinutes(15)
    while (-not (Test-Path -LiteralPath $releasePath) -and [DateTime]::UtcNow -lt $deadline) {
        Start-Sleep -Milliseconds 250
    }
} finally {
    $lockStream.Dispose()
    Write-Output 'LOCK_RELEASED'
}
