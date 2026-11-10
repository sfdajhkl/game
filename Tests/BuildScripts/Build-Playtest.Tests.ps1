# Dependency-free launcher regression checks. Unity is mocked; no Editor or installer runs.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('arvito-launch-check-' + [guid]::NewGuid().ToString('N'))
$oldBuildPath = $env:ARVITO_BUILD_PATH
$mockState = [pscustomobject]@{ Launches = 0; Message = 'Simulated compilation error.' }
function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Start-Process {
    param([string]$FilePath, [string[]]$ArgumentList, [switch]$Wait, [switch]$PassThru)
    $mockState.Launches++
    [IO.File]::WriteAllText((Join-Path $fixture 'Builds/unity-build.log'), $mockState.Message)
    return [pscustomobject]@{ ExitCode = 17 }
}
function Read-BuildError([string]$EditorPath) {
    try { & (Join-Path $fixture 'Tools/Build-Playtest.ps1') -UnityPath $EditorPath | Out-Null }
    catch { return $_.Exception.Message }
    throw 'Expected the simulated Unity build to fail.'
}
try {
    New-Item -ItemType Directory -Path (Join-Path $fixture 'Tools'), (Join-Path $fixture 'Temp') | Out-Null
    Copy-Item -LiteralPath (Join-Path $root 'Unity/Arvito/Tools/Build-Playtest.ps1') -Destination (Join-Path $fixture 'Tools/Build-Playtest.ps1')
    Set-Content -LiteralPath (Join-Path $fixture 'Tools/Package-Playtest.ps1') -Value "throw 'Packaging must not run after Unity fails.'"
    # An existing path satisfies discovery; the mock above intercepts every launch.
    $fakeEditor = Join-Path $fixture 'Tools/Build-Playtest.ps1'
    $lock = Join-Path $fixture 'Temp/UnityLockfile'
    [IO.File]::WriteAllText($lock, 'preserve this lock file')
    $message = Read-BuildError $fakeEditor
    Assert-True ($mockState.Launches -eq 1) 'A leftover lock file incorrectly prevented Unity launch.'
    Assert-True ($message -match '^Unity build failed') 'The actual simulated Unity failure was hidden.'
    Assert-True ([IO.File]::ReadAllText($lock) -eq 'preserve this lock file') 'The lock file was modified.'
    Write-Host 'PASS leftover lock file allows Unity launch and remains untouched'

    $mockState.Message = 'It looks like another Unity instance is running with this project open. Multiple Unity instances cannot open the same project.'
    $heldLock = [IO.File]::Open($lock, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    try { $message = Read-BuildError $fakeEditor }
    finally { $heldLock.Dispose() }
    Assert-True ($mockState.Launches -eq 2) 'Unity did not get to perform its own lock check.'
    Assert-True ($message -match 'close this project''s Unity Editor window' -and $message -match 'Unity Hub may stay open') 'The active-project guidance was missing.'
    Assert-True ([IO.File]::ReadAllText($lock) -eq 'preserve this lock file') 'The held lock file was modified.'
    Assert-True ((Get-Content -LiteralPath (Join-Path $fixture 'Builds/unity-build.log') -Raw).Trim() -eq $mockState.Message) 'The Unity log was not preserved.'
    Write-Host 'PASS Unity reports an open project, guidance is actionable, and packaging is not attempted'

    $message = Read-BuildError (Join-Path $fixture 'missing/Unity.exe')
    Assert-True ($mockState.Launches -eq 2) 'A missing Editor path triggered a launch.'
    Assert-True ($message -match 'Unity 6.3 LTS was not found') 'The prelaunch error was hidden.'
    Assert-True ($message -notmatch 'unity-build.log') 'A prelaunch error required a nonexistent log.'
    Write-Host 'PASS missing Editor reports a prelaunch error without requiring a build log'
    Write-Host '3 launcher regression checks passed. Actual Unity locking on Windows was not executed.'
}
finally {
    $env:ARVITO_BUILD_PATH = $oldBuildPath
    if (Test-Path -LiteralPath $fixture) { Remove-Item -LiteralPath $fixture -Recurse -Force }
}
