# Test the repair plan and file transaction with a mocked Editor/UPM.
# Test fixtures are temporary; no licensed Unity Editor is run by these checks.
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
. (Join-Path $root 'Unity/Arvito/Tools/Package-Repair-Plan.ps1')
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('arvito-repair-check-' + [guid]::NewGuid().ToString('N'))
$oldRecipe = $env:ARVITO_PACKAGE_REPAIR_RECIPE
$mock = [pscustomobject]@{ Mode = 'success'; Scratch = (New-Object 'System.Collections.Generic.List[string]'); AddHadQuit = $false; Calls = 0 }
function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Make-Target([string]$Name) {
    $path = Join-Path $fixture $Name
    New-Item -ItemType Directory -Path (Join-Path $path 'Packages') | Out-Null
    # Keep a legacy fixture independent of the shipped manifest: main must already
    # open with compatible requests, while this suite still exercises old checkouts.
    [IO.File]::WriteAllText((Join-Path $path 'Packages/manifest.json'), '{"dependencies":{"com.unity.collab-proxy":"2.6.0","com.unity.inputsystem":"1.12.0","com.unity.render-pipelines.universal":"17.0.3","com.unity.ugui":"2.0.0"}}')
    [IO.File]::WriteAllText((Join-Path $path 'Packages/packages-lock.json'), '{"old":"lock bytes"}')
    return $path
}
function Start-Process {
    param([string]$FilePath, [string[]]$ArgumentList, [string]$WorkingDirectory, [switch]$PassThru)
    $mock.Calls++
    $log = $ArgumentList[[Array]::IndexOf($ArgumentList, '-logFile') + 1].Trim('"')
    [IO.File]::WriteAllText($log, "Mocked Unity Package Manager operation`n")
    $code = 0
    if ($ArgumentList -contains '-createProject') {
        $scratch = $ArgumentList[[Array]::IndexOf($ArgumentList, '-createProject') + 1].Trim('"')
        $mock.Scratch.Add($scratch)
        New-Item -ItemType Directory -Force -Path (Join-Path $scratch 'Packages'), (Join-Path $scratch 'Assets') | Out-Null
        # This file is a test stand-in for Unity's generated default manifest.
        [IO.File]::WriteAllText((Join-Path $scratch 'Packages/manifest.json'), '{"dependencies":{}}')
    }
    else {
        $mock.AddHadQuit = $ArgumentList -contains '-quit'
        $recipe = Get-Content -LiteralPath $env:ARVITO_PACKAGE_REPAIR_RECIPE -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($mock.Mode -eq 'failure') { $code = 1 }
        else {
            $deps = [ordered]@{}
            foreach ($request in $recipe.requests) { $parts = $request -split '@', 2; $deps[$parts[0]] = $parts[1] }
            if ($mock.Mode -eq 'bad-version') { $deps['com.unity.inputsystem'] = '1.12.0' }
            [IO.File]::WriteAllText((Join-Path $WorkingDirectory 'Packages/manifest.json'), (@{ dependencies = $deps } | ConvertTo-Json -Depth 6))
            [IO.File]::WriteAllText((Join-Path $WorkingDirectory 'Packages/packages-lock.json'), '{"mock":"client-resolved"}')
            [IO.File]::WriteAllText($recipe.successFile, 'mock UPM success')
        }
    }
    $process = [pscustomobject]@{ ExitCode = $code; HasExited = $true; Handle = 0 }
    $process | Add-Member -MemberType ScriptMethod -Name Refresh -Value { }
    $process | Add-Member -MemberType ScriptMethod -Name WaitForExit -Value { }
    return $process
}
try {
    $shipped = Get-ArvitoPackagePlan (Join-Path $root 'Unity/Arvito')
    Assert-True (-not $shipped.NeedsRepair) 'The shipped project still needs external repair before Unity can compile it.'
    Write-Host 'PASS shipped Unity project already requests compatible packages without an external repair step'
    New-Item -ItemType Directory -Path $fixture | Out-Null
    $target = Make-Target 'success'
    $manifest = Join-Path $target 'Packages/manifest.json'
    $before = [IO.File]::ReadAllText($manifest)
    $plan = Get-ArvitoPackagePlan $target
    Assert-True $plan.NeedsRepair 'Old template was not identified.'
    Assert-True ($plan.Versions['com.unity.inputsystem'] -eq '1.20.1' -and $plan.Versions['com.unity.render-pipelines.universal'] -eq '17.3.0') 'Wrong compatible versions requested.'
    Assert-True (-not $plan.Versions.ContainsKey('com.unity.collab-proxy')) 'Collab was retained.'
    $old = $before | ConvertFrom-Json
    foreach ($p in $old.dependencies.PSObject.Properties) {
        if ($p.Name -notin @('com.unity.inputsystem','com.unity.render-pipelines.universal','com.unity.collab-proxy')) { Assert-True ($plan.Versions[$p.Name] -eq $p.Value) 'An unrelated dependency was changed.' }
    }
    Assert-True ([IO.File]::ReadAllText($manifest) -ceq $before) 'Planning modified the target manifest.'
    Write-Host 'PASS repair plan targets the known failures and preserves unrelated requests without writing manifest'

    $repair = Join-Path $root 'Unity/Arvito/Tools/Repair-Packages.ps1'
    & $repair -ProjectPath $target -UnityPath $PSCommandPath
    Assert-True (-not $mock.AddHadQuit) 'The asynchronous Client API phase was launched with -quit.'
    Assert-True (-not (Get-ArvitoPackagePlan $target).NeedsRepair) 'Resolved package requests were not applied.'
    $backups = @(Get-ChildItem -LiteralPath (Join-Path $target 'Builds') -Directory -Filter 'PackageBackup-*')
    Assert-True ($backups.Count -eq 1 -and [IO.File]::ReadAllText((Join-Path $backups[0].FullName 'manifest.json')) -ceq $before) 'Original manifest backup was missing or changed.'
    Assert-True ([IO.File]::ReadAllText((Join-Path $backups[0].FullName 'packages-lock.json')) -ceq '{"old":"lock bytes"}') 'Original lock backup was missing.'
    Write-Host 'PASS mocked Client-generated files apply after success with original manifest/lock backups'

    $calls = $mock.Calls
    & $repair -ProjectPath $target -UnityPath $PSCommandPath
    Assert-True ($mock.Calls -eq $calls) 'Already repaired requests launched another Editor.'
    Write-Host 'PASS compatible request set skips repeat repair'

    foreach ($mode in @('failure','bad-version')) {
        $target = Make-Target $mode
        $original = [IO.File]::ReadAllText((Join-Path $target 'Packages/manifest.json'))
        $mock.Mode = $mode
        $errorMessage = ''
        try { & $repair -ProjectPath $target -UnityPath $PSCommandPath } catch { $errorMessage = $_.Exception.Message }
        Assert-True ($errorMessage.Length -gt 0) 'The simulated UPM failure was accepted.'
        Assert-True ([IO.File]::ReadAllText((Join-Path $target 'Packages/manifest.json')) -ceq $original) 'Failed repair modified the original manifest.'
        Assert-True ([IO.File]::ReadAllText((Join-Path $target 'Packages/packages-lock.json')) -ceq '{"old":"lock bytes"}') 'Failed repair modified the original lock.'
        Write-Host ('PASS ' + $mode + ' leaves original package files untouched')
    }
    Write-Host '6 package repair checks passed. Unity/UPM were mocked; actual Windows repair is not executed here.'
}
finally {
    $env:ARVITO_PACKAGE_REPAIR_RECIPE = $oldRecipe
    foreach ($scratch in $mock.Scratch) { if (Test-Path -LiteralPath $scratch) { Remove-Item -LiteralPath $scratch -Recurse -Force } }
    if (Test-Path -LiteralPath $fixture) { Remove-Item -LiteralPath $fixture -Recurse -Force }
}
