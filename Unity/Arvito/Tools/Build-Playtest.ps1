param([string]$UnityPath)
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
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
if (Test-Path -LiteralPath (Join-Path $project 'Temp\UnityLockfile')) {
    throw 'Close this project in Unity before running the build script.'
}
Write-Host "Unity: $UnityPath"
Write-Host 'Building Arvito for Windows. Initial package import can take several minutes.'
$env:ARVITO_BUILD_PATH = Join-Path $player 'Arvito.exe'
$log = Join-Path $builds 'unity-build.log'
$unityArgs = @('-batchmode', '-nographics', '-quit', '-buildTarget', 'Win64', '-projectPath', ('"{0}"' -f $project),
    '-executeMethod', 'Arvito.Editor.ProjectSetup.BuildWindows', '-logFile', ('"{0}"' -f $log))
$process = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -Wait -PassThru
if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $env:ARVITO_BUILD_PATH)) {
    if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log -Tail 35 }
    throw "Unity build failed. Check your Unity Hub license and $log"
}
& (Join-Path $PSScriptRoot 'Package-Playtest.ps1') -PlayerFolder $player -OutputFolder $builds
Write-Host ''
Write-Host 'Created Builds\Arvito-Setup.exe and Builds\Arvito-Windows.zip'
Write-Host 'For an immediate playtest, open Builds\Windows\Arvito.exe.'
Start-Process explorer.exe -ArgumentList ('"{0}"' -f $builds)
