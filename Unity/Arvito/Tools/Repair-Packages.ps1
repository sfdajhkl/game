param([Parameter(Mandatory=$true)][string]$ProjectPath, [Parameter(Mandatory=$true)][string]$UnityPath)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Package-Repair-Plan.ps1')
. (Join-Path $PSScriptRoot 'Build-Progress.ps1')
$ProjectPath = [IO.Path]::GetFullPath($ProjectPath)
$plan = Get-ArvitoPackagePlan $ProjectPath
if (-not $plan.NeedsRepair) { Write-Host '[Arvito Packages] Compatible package requests already present.'; return }
if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) { throw 'The Unity Editor executable was not found.' }
$lock = Join-Path $ProjectPath 'Temp/UnityLockfile'
if (Test-Path -LiteralPath $lock -PathType Leaf) {
    try { $probe = [IO.File]::Open($lock, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None); $probe.Dispose() }
    catch [IO.IOException] { throw 'Close this project in Unity before repairing packages. Unity Hub may stay open.' }
}
$builds = Join-Path $ProjectPath 'Builds'
New-Item -ItemType Directory -Force -Path $builds | Out-Null
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('Arvito-PackageRepair-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $scratch | Out-Null
$oldRecipe = $env:ARVITO_PACKAGE_REPAIR_RECIPE
$finished = $false
try {
    Write-Host '[Arvito Packages] Updating Input System to 1.20.1 and URP to 17.3.0; removing collab-proxy.'
    Write-Host '[Arvito Packages] Creating an empty temporary project for the Unity Package Manager...'
    $createLog = Join-Path $builds 'package-repair-create.log'
    $state = New-ArvitoLogState $createLog -WaitForChange
    $createArgs = @('-batchmode', '-nographics', '-quit', '-createProject', ('"{0}"' -f $scratch.Replace('\','/')), '-logFile', ('"{0}"' -f $createLog.Replace('\','/')))
    $process = Start-Process -FilePath $UnityPath -ArgumentList $createArgs -WorkingDirectory $scratch -PassThru
    try { $null = $process.Handle } catch [InvalidOperationException] { }
    $code = Wait-ArvitoUnityBuild $process $state
    if ($code -ne 0 -or -not (Test-Path -LiteralPath (Join-Path $scratch 'Packages/manifest.json'))) { throw "Could not create the package repair project. See $createLog" }
    $editor = Join-Path $scratch 'Assets/Editor'
    New-Item -ItemType Directory -Force -Path $editor | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'PackageRepair.cs') -Destination (Join-Path $editor 'PackageRepair.cs')
    $success = Join-Path $scratch 'resolved.ok'
    $recipe = Join-Path $scratch 'repair-recipe.json'
    [IO.File]::WriteAllText($recipe, (@{ requests = @($plan.Requests); successFile = $success } | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false))
    $env:ARVITO_PACKAGE_REPAIR_RECIPE = $recipe
    $repairLog = Join-Path $builds 'package-repair.log'
    $state = New-ArvitoLogState $repairLog -WaitForChange
    # No -quit here: Client.AddAndRemove completes on EditorApplication.update.
    $repairArgs = @('-batchmode', '-nographics', '-projectPath', ('"{0}"' -f $scratch.Replace('\','/')), '-executeMethod', 'ArvitoPackageRepair.Installer.Install', '-logFile', ('"{0}"' -f $repairLog.Replace('\','/')))
    $process = Start-Process -FilePath $UnityPath -ArgumentList $repairArgs -WorkingDirectory $scratch -PassThru
    try { $null = $process.Handle } catch [InvalidOperationException] { }
    $code = Wait-ArvitoUnityBuild $process $state
    if ($code -ne 0 -or -not (Test-Path -LiteralPath $success)) { throw "Package resolution failed. The original package files were preserved. See $repairLog" }
    $resolved = Get-Content -LiteralPath (Join-Path $scratch 'Packages/manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $actual = @{}
    foreach ($entry in $resolved.dependencies.PSObject.Properties) { $actual[$entry.Name] = [string]$entry.Value }
    if ($actual.Count -ne $plan.Versions.Count) { throw 'Resolved package root count differs from the requested repair.' }
    foreach ($name in $plan.Versions.Keys) { if ($actual[$name] -ne $plan.Versions[$name]) { throw "Unexpected resolved package version: $name" } }
    $backup = Join-Path $builds ('PackageBackup-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $backup | Out-Null
    $files = @('manifest.json', 'packages-lock.json')
    foreach ($name in $files) {
        $target = Join-Path $ProjectPath ('Packages/' + $name)
        if (Test-Path -LiteralPath $target) { Copy-Item -LiteralPath $target -Destination (Join-Path $backup $name) }
        Copy-Item -LiteralPath (Join-Path $scratch ('Packages/' + $name)) -Destination ($target + '.repair-tmp')
    }
    try {
        foreach ($name in $files) {
            $target = Join-Path $ProjectPath ('Packages/' + $name)
            if (Test-Path -LiteralPath $target) { [IO.File]::Replace($target + '.repair-tmp', $target, [NullString]::Value) }
            else { [IO.File]::Move($target + '.repair-tmp', $target) }
        }
    }
    catch {
        foreach ($name in $files) {
            $target = Join-Path $ProjectPath ('Packages/' + $name)
            if (Test-Path -LiteralPath (Join-Path $backup $name)) { Copy-Item -LiteralPath (Join-Path $backup $name) -Destination $target -Force }
            elseif (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Force }
        }
        throw
    }
    $finished = $true
    Write-Host "[Arvito Packages] Package repair complete. Backup: $backup"
}
finally {
    $env:ARVITO_PACKAGE_REPAIR_RECIPE = $oldRecipe
    foreach ($name in @('manifest.json', 'packages-lock.json')) {
        $staged = Join-Path $ProjectPath ('Packages/' + $name + '.repair-tmp')
        if (Test-Path -LiteralPath $staged) { Remove-Item -LiteralPath $staged -Force -ErrorAction SilentlyContinue }
    }
    if ($finished) {
        try { Remove-Item -LiteralPath $scratch -Recurse -Force } catch { Write-Host "Temporary repair files remain at: $scratch" }
    }
    else { Write-Host "Repair diagnostics remain at: $scratch" }
}
