$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$failed = $false
Get-ChildItem -LiteralPath (Join-Path $root 'Unity/Arvito/Tools') -Filter '*.ps1' -Recurse | ForEach-Object {
    $tokens = $null; $errors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($_.FullName, [ref]$tokens, [ref]$errors) | Out-Null
    if ($errors.Count) { $errors | ForEach-Object { Write-Host $_.Message }; $failed = $true }
    else { Write-Host ('PASS PowerShell syntax: ' + $_.Name) }
}
if ($failed) { exit 1 }
Write-Host 'Syntax only. Windows installation and IExpress packaging have not been executed.'
