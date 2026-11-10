# Dependency-free launcher regression checks. Unity is mocked; no Editor or installer runs.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('arvito-launch-check-' + [guid]::NewGuid().ToString('N'))
$oldBuildPath = $env:ARVITO_BUILD_PATH
$mockState = [pscustomobject]@{ Launches = 0; Message = 'Simulated compilation error.'; ExitCode = 17; Project = ''; WorkingDirectory = ''; BuildPath = ''; LogPath = ''; Waited = 0; Blocking = $false }
function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function New-TestProject([string]$Path) {
    foreach ($folder in @('Assets', 'Packages', 'ProjectSettings', 'Tools')) { New-Item -ItemType Directory -Force -Path (Join-Path $Path $folder) | Out-Null }
    Set-Content -LiteralPath (Join-Path $Path 'ProjectSettings/ProjectVersion.txt') -Value 'm_EditorVersion: 6000.3.23f1'
    Set-Content -LiteralPath (Join-Path $Path 'Packages/manifest.json') -Value '{"dependencies":{"com.unity.inputsystem":"1.20.1","com.unity.render-pipelines.universal":"17.3.0"}}'
    Copy-Item -LiteralPath (Join-Path $root 'Unity/Arvito/Tools/Build-Progress.ps1') -Destination (Join-Path $Path 'Tools/Build-Progress.ps1')
    foreach ($name in @('Package-Repair-Plan.ps1', 'Repair-Packages.ps1', 'PackageRepair.cs')) {
        Copy-Item -LiteralPath (Join-Path $root ('Unity/Arvito/Tools/' + $name)) -Destination (Join-Path $Path ('Tools/' + $name))
    }
}
function Start-Process {
    param([string]$FilePath, [string[]]$ArgumentList, [string]$WorkingDirectory, [switch]$Wait, [switch]$PassThru)
    if ($FilePath -eq 'explorer.exe') { return }
    $mockState.Launches++
    $mockState.Blocking = [bool]$Wait
    $mockState.Project = $ArgumentList[[Array]::IndexOf($ArgumentList, '-projectPath') + 1].Trim('"')
    $mockState.LogPath = $ArgumentList[[Array]::IndexOf($ArgumentList, '-logFile') + 1].Trim('"')
    $mockState.WorkingDirectory = $WorkingDirectory
    $mockState.BuildPath = $env:ARVITO_BUILD_PATH
    $mockState | Add-Member -MemberType NoteProperty -Name Arguments -Value $ArgumentList -Force
    [IO.File]::WriteAllText($mockState.LogPath, $mockState.Message)
    if ($mockState.ExitCode -eq 0 -and $ArgumentList -contains '-batchmode') {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $mockState.BuildPath) | Out-Null
        [IO.File]::WriteAllText($mockState.BuildPath, 'Mock player marker, never executed or distributed.')
    }
    $process = [pscustomobject]@{ ExitCode = $mockState.ExitCode; HasExited = $true }
    $process | Add-Member -MemberType ScriptMethod -Name Refresh -Value { }
    $process | Add-Member -MemberType ScriptMethod -Name WaitForExit -Value { $mockState.Waited++ }
    return $process
}
function Read-BuildError([string]$EditorPath, [string]$Launcher = '') {
    if (-not $Launcher) { $Launcher = Join-Path $fixture 'Tools/Build-Playtest.ps1' }
    try { & $Launcher -UnityPath $EditorPath | Out-Null }
    catch { return $_.Exception.Message }
    throw 'Expected the simulated Unity build to fail.'
}
try {
    New-Item -ItemType Directory -Path (Join-Path $fixture 'Tools'), (Join-Path $fixture 'Temp') | Out-Null
    New-TestProject $fixture
    Copy-Item -LiteralPath (Join-Path $root 'Unity/Arvito/Tools/Build-Playtest.ps1') -Destination (Join-Path $fixture 'Tools/Build-Playtest.ps1')
    Set-Content -LiteralPath (Join-Path $fixture 'Tools/Package-Playtest.ps1') -Value "throw 'Packaging must not run after Unity fails.'"
    # An existing path satisfies discovery; the mock above intercepts every launch.
    $fakeEditor = Join-Path $fixture 'Tools/Build-Playtest.ps1'
    $lock = Join-Path $fixture 'Temp/UnityLockfile'
    [IO.File]::WriteAllText($lock, 'preserve this lock file')
    $message = Read-BuildError $fakeEditor
    Assert-True ($mockState.Launches -eq 1) 'A leftover lock file incorrectly prevented Unity launch.'
    Assert-True (-not $mockState.Blocking -and $mockState.Waited -eq 1) 'Progress launch blocked or skipped waiting for the exit code.'
    Assert-True ($message -match '^Unity build failed') 'The actual simulated Unity failure was hidden.'
    Assert-True ($mockState.Project -eq $fixture.Replace('\', '/') -and $mockState.WorkingDirectory -eq $fixture) 'Direct project launch used an incorrect project path or working directory.'
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

    # Reproduce the user extracting the update under game-main/Tools. Include spaces
    # and non-ASCII characters without requiring a BOM for Windows PowerShell 5.1.
    $repository = Join-Path $fixture ('repo with spaces ' + [char]0xD55C + [char]0xAE00)
    $nestedProject = Join-Path $repository 'Unity/Arvito'
    New-TestProject $nestedProject
    New-Item -ItemType Directory -Path (Join-Path $repository 'Tools') | Out-Null
    $misplacedLauncher = Join-Path $repository 'Tools/Build-Playtest.ps1'
    Copy-Item -LiteralPath $fakeEditor -Destination $misplacedLauncher
    Set-Content -LiteralPath (Join-Path $repository 'Tools/Package-Playtest.ps1') -Value "throw 'The repository-root packaging helper must not run.'"
    Set-Content -LiteralPath (Join-Path $nestedProject 'Tools/Package-Playtest.ps1') -Value 'Set-Content -LiteralPath (Join-Path $PSScriptRoot "packaged.txt") -Value "called"'
    $mockState.ExitCode = 0
    & $misplacedLauncher -UnityPath $fakeEditor | Out-Null
    Assert-True ($mockState.Launches -eq 3) 'The repository-root launcher did not start Unity.'
    Assert-True ($mockState.Project -eq $nestedProject.Replace('\', '/')) 'Unity received the repository root instead of the nested project.'
    Assert-True ($mockState.WorkingDirectory -eq $nestedProject) 'Unity working directory was not the resolved project.'
    Assert-True ($mockState.BuildPath -eq (Join-Path $nestedProject 'Builds/Windows/Arvito.exe')) 'The player output was written outside the resolved project.'
    Assert-True ($mockState.LogPath -eq (Join-Path $nestedProject 'Builds/unity-build.log').Replace('\', '/')) 'The Unity log path was outside the resolved project.'
    Assert-True (Test-Path -LiteralPath (Join-Path $nestedProject 'Tools/packaged.txt')) 'The resolved project packaging helper was not invoked.'
    Write-Host 'PASS misplaced launcher resolves nested Unity project, paths and packaging with spaces and non-ASCII names'

    $invalidRoot = Join-Path $fixture 'not-a-project'
    New-Item -ItemType Directory -Path (Join-Path $invalidRoot 'Tools'), (Join-Path $invalidRoot 'Assets') | Out-Null
    $invalidLauncher = Join-Path $invalidRoot 'Tools/Build-Playtest.ps1'
    Copy-Item -LiteralPath $fakeEditor -Destination $invalidLauncher
    $message = Read-BuildError $fakeEditor $invalidLauncher
    Assert-True ($mockState.Launches -eq 3) 'An invalid Unity project launched the Editor.'
    Assert-True ($message -match '^Arvito Unity project not found') 'The incomplete project was not explained.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $invalidRoot 'Builds'))) 'An invalid project created build output.'
    Write-Host 'PASS missing Unity project is rejected before launching or creating build output'
    # Windows PowerShell can occasionally expose a null process exit code. An old
    # player must never make that look like a successful new build.
    $mockState.ExitCode = $null
    $oldPlayer = Join-Path $fixture 'Builds/Windows/Arvito.exe'
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $oldPlayer) | Out-Null
    [IO.File]::WriteAllText($oldPlayer, 'Old mock player marker; never executed or distributed.')
    $message = Read-BuildError $fakeEditor
    Assert-True ($message -match 'without a readable exit code') 'An unknown exit code was treated as success with an older player present.'
    Write-Host 'PASS unknown exit code blocks packaging even when an older player exists'

    $mockState.ExitCode = 0
    $launches = $mockState.Launches
    $waited = $mockState.Waited
    & $fakeEditor -UnityPath $fakeEditor -PlayInEditor | Out-Null
    Assert-True ($mockState.Launches -eq $launches + 1 -and $mockState.Waited -eq $waited) 'Editor play did not launch interactively or blocked waiting for exit.'
    Assert-True ($mockState.Arguments -notcontains '-batchmode' -and $mockState.Arguments -notcontains '-quit' -and $mockState.Arguments -notcontains '-nographics') 'Editor play was launched headlessly or told to quit.'
    Assert-True ($mockState.Arguments -contains 'Arvito.Editor.ProjectSetup.PlayGame') 'Editor play did not invoke game preparation and Play Mode.'
    Assert-True ($mockState.LogPath -eq (Join-Path $fixture 'Builds/unity-play.log').Replace('\', '/')) 'Editor play has no dedicated log.'
    Write-Host 'PASS editor play prepares the game without building, packaging, quitting or blocking'

    $launches = $mockState.Launches
    $heldLock = [IO.File]::Open($lock, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    try {
        $message = ''
        try { & $fakeEditor -UnityPath $fakeEditor -PlayInEditor | Out-Null }
        catch { $message = $_.Exception.Message }
    }
    finally { $heldLock.Dispose() }
    Assert-True ($mockState.Launches -eq $launches -and $message -match 'Arvito > Play Game') 'Editor play ignored an active project lock or omitted the existing-Editor instructions.'
    Assert-True ([IO.File]::ReadAllText($lock) -eq 'preserve this lock file') 'Editor play modified the active project lock.'
    Write-Host 'PASS editor play preserves an open project and points to its Play Game menu'
    Write-Host '8 launcher regression checks passed. Actual Unity on Windows was not executed.'
}
finally {
    $env:ARVITO_BUILD_PATH = $oldBuildPath
    if (Test-Path -LiteralPath $fixture) { Remove-Item -LiteralPath $fixture -Recurse -Force }
}
