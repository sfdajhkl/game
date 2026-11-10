param([string]$UnityPath, [switch]$OpenEditor)
$ErrorActionPreference = 'Stop'
$launcherRoot = Split-Path -Parent $PSScriptRoot
$project = $null
foreach ($candidate in @($launcherRoot, (Join-Path $launcherRoot 'Unity/Arvito'))) {
    if ((Test-Path -LiteralPath (Join-Path $candidate 'Assets') -PathType Container) -and
        (Test-Path -LiteralPath (Join-Path $candidate 'ProjectSettings/ProjectVersion.txt') -PathType Leaf) -and
        (Test-Path -LiteralPath (Join-Path $candidate 'Packages/manifest.json') -PathType Leaf)) {
        $project = [IO.Path]::GetFullPath((Get-Item -LiteralPath $candidate).FullName)
        break
    }
}
if (-not $project) {
    throw 'Arvito Unity project not found. Extract the complete project ZIP, then run Build-Playtest.cmd from game-main or Unity/Arvito. Assets, Packages/manifest.json and ProjectSettings/ProjectVersion.txt must exist.'
}
$packageScript = Join-Path $project 'Tools/Package-Playtest.ps1'
if (-not $OpenEditor -and -not (Test-Path -LiteralPath $packageScript -PathType Leaf)) {
    throw "The project is incomplete: missing $packageScript. Extract the complete project ZIP again."
}
$progressScript = Join-Path $project 'Tools/Build-Progress.ps1'
if (-not (Test-Path -LiteralPath $progressScript -PathType Leaf)) {
    throw "The project is incomplete: missing $progressScript. Extract the complete project ZIP again."
}
. $progressScript
$version = '6000.3.28f1'
$builds = Join-Path $project 'Builds'
$player = Join-Path $builds 'Windows'
New-Item -ItemType Directory -Force -Path $builds | Out-Null
if (-not $UnityPath) {
    $editorRoot = Join-Path ${env:ProgramFiles} 'Unity\Hub\Editor'
    $UnityPath = Join-Path $editorRoot "$version\Editor\Unity.exe"
    if (-not (Test-Path -LiteralPath $UnityPath)) {
        $candidates = @(Get-ChildItem -LiteralPath $editorRoot -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match '^6000\.3\.(\d+)f\d+$' } |
            Sort-Object { [int]([regex]::Match($_.Name, '^6000\.3\.(\d+)').Groups[1].Value) } -Descending)
        if ($candidates.Count) { $UnityPath = Join-Path $candidates[0].FullName 'Editor\Unity.exe' }
    }
}
if (-not $UnityPath -or -not (Test-Path -LiteralPath $UnityPath)) {
    throw "Unity 6.3 LTS was not found. Install $version with Unity Hub, or run: .\Build-Playtest.cmd -UnityPath <path-to-Unity.exe>"
}
$repairScript = Join-Path $project 'Tools/Repair-Packages.ps1'
if (-not (Test-Path -LiteralPath $repairScript -PathType Leaf)) { throw 'Package repair tools are missing. Apply the complete build-tools update or download the full project again.' }
& $repairScript -ProjectPath $project -UnityPath $UnityPath
if ($OpenEditor) {
    Write-Host "Unity: $UnityPath"
    Write-Host "Project: $project"
    Write-Host '[Arvito] Opening the game scene in Unity. Wait for import, then press Play.'
    $editorArgs = @('-projectPath', ('"{0}"' -f $project.Replace('\', '/')),
        '-executeMethod', 'Arvito.Editor.ProjectSetup.OpenScene')
    $null = Start-Process -FilePath $UnityPath -ArgumentList $editorArgs -WorkingDirectory $project -PassThru
    return
}
# Unity owns project locking. A lock file can remain after an interrupted session;
# its presence alone must not prevent Unity from opening the project again.
Write-Host "Unity: $UnityPath"
Write-Host "Project: $project"
Write-Host 'Building Arvito for Windows. Initial package import can take several minutes.'
$env:ARVITO_BUILD_PATH = Join-Path $player 'Arvito.exe'
$log = Join-Path $builds 'unity-build.log'
$unityArgs = @('-batchmode', '-nographics', '-quit', '-buildTarget', 'Win64', '-projectPath', ('"{0}"' -f $project.Replace('\', '/')),
    '-executeMethod', 'Arvito.Editor.ProjectSetup.BuildWindows', '-logFile', ('"{0}"' -f $log.Replace('\', '/')))
$logState = New-ArvitoLogState $log -WaitForChange
$process = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -WorkingDirectory $project -PassThru
# Cache the handle while Unity is alive so Windows PowerShell retains its exit code.
try { $null = $process.Handle } catch [InvalidOperationException] { }
$exitCode = Wait-ArvitoUnityBuild $process $logState
if ($exitCode -ne 0 -or -not (Test-Path -LiteralPath $env:ARVITO_BUILD_PATH)) {
    $logText = ''
    if (Test-Path -LiteralPath $log) {
        $logText = Get-Content -LiteralPath $log -Raw
        Get-Content -LiteralPath $log -Tail 35
    }
    if ($logText -match 'another Unity instance|Multiple Unity instances cannot open') {
        throw 'This project is already open in Unity. Save your work, close this project''s Unity Editor window, and run Build-Playtest.cmd again. Unity Hub may stay open. To play in the Editor, use Arvito > Open Game Scene, then Play.'
    }
    throw "Unity build failed. Read the error above and $log"
}
Write-Host '[Arvito] Packaging the portable ZIP and Windows installer...'
& $packageScript -PlayerFolder $player -OutputFolder $builds
Write-Host ''
Write-Host 'Created Builds\Arvito-Setup.exe and Builds\Arvito-Windows.zip'
Write-Host 'For an immediate playtest, open Builds\Windows\Arvito.exe.'
Start-Process explorer.exe -ArgumentList ('"{0}"' -f $builds)
