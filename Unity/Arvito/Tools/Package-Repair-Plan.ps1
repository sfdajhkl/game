# Read the target's package requests. Manifest changes are resolved by Unity's Client API.
function Get-ArvitoPackagePlan([string]$ProjectPath) {
    $manifest = Join-Path $ProjectPath 'Packages/manifest.json'
    $data = Get-Content -LiteralPath $manifest -Raw -Encoding UTF8 | ConvertFrom-Json
    if (-not $data.dependencies) { throw 'Package manifest has no dependencies object.' }
    if (@($data.PSObject.Properties | Where-Object { $_.Name -ne 'dependencies' }).Count) {
        throw 'This repair supports the original registry-only Arvito manifest. Use Unity Package Manager for a customized manifest.'
    }
    $packages = @{}
    foreach ($entry in $data.dependencies.PSObject.Properties) {
        if ($entry.Name -notmatch '^com\.unity\.[a-z0-9.-]+$' -or [string]$entry.Value -notmatch '^\d+\.\d+\.\d+([-.][a-zA-Z0-9.-]+)?$') {
            throw "Custom package source requires manual Package Manager review: $($entry.Name)"
        }
        $packages[$entry.Name] = [string]$entry.Value
    }
    $needsRepair = $packages.ContainsKey('com.unity.collab-proxy')
    $null = $packages.Remove('com.unity.collab-proxy')
    if (-not $packages.ContainsKey('com.unity.inputsystem') -or $packages['com.unity.inputsystem'] -ne '1.20.1') {
        $packages['com.unity.inputsystem'] = '1.20.1'; $needsRepair = $true
    }
    if (-not $packages.ContainsKey('com.unity.render-pipelines.universal') -or $packages['com.unity.render-pipelines.universal'] -ne '17.3.0') {
        $packages['com.unity.render-pipelines.universal'] = '17.3.0'; $needsRepair = $true
    }
    # Animation 10.x contains USS selectors rejected by Unity 6.3. Upgrade its
    # installed importer companions too: they share the 2D Common dependency.
    # Do not introduce optional 2D tools into projects that do not use them.
    $twoDVersions = @{
        'com.unity.2d.animation' = '13.0.6'
        'com.unity.2d.psdimporter' = '12.0.2'
        'com.unity.2d.aseprite' = '3.0.2'
    }
    foreach ($name in $twoDVersions.Keys) {
        if ($packages.ContainsKey($name) -and $packages[$name] -ne $twoDVersions[$name]) {
            $packages[$name] = $twoDVersions[$name]; $needsRepair = $true
        }
    }
    $requests = @($packages.Keys | Sort-Object | ForEach-Object { $_ + '@' + $packages[$_] })
    return [pscustomobject]@{ NeedsRepair = [bool]$needsRepair; Requests = $requests; Versions = $packages }
}
