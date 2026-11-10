$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
$app = Join-Path $env:LOCALAPPDATA 'Programs\Arvito'
if (([IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\')) -ne ([IO.Path]::GetFullPath($app).TrimEnd('\')) -or -not (Test-Path -LiteralPath (Join-Path $app 'arvito-install.json'))) {
    throw 'This uninstaller is not inside a recognized Arvito installation.'
}
if (Get-Process -Name Arvito -ErrorAction SilentlyContinue) { [Windows.Forms.MessageBox]::Show('Close Arvito before uninstalling.') | Out-Null; exit 1 }
if ([Windows.Forms.MessageBox]::Show('Uninstall Arvito? Your saved lives will be kept.', 'ARVITO', 'YesNo', 'Question') -ne 'Yes') { exit }
$menu = Join-Path ([Environment]::GetFolderPath('Programs')) 'Arvito'
foreach ($shortcut in @((Join-Path $menu 'Arvito.lnk'), (Join-Path ([Environment]::GetFolderPath('Desktop')) 'Arvito.lnk'))) {
    if (Test-Path -LiteralPath $shortcut) { Remove-Item -LiteralPath $shortcut -Force }
}
if ((Test-Path -LiteralPath $menu) -and -not (Get-ChildItem -LiteralPath $menu)) { Remove-Item -LiteralPath $menu }
Remove-Item -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ArvitoPlaytest' -Recurse -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $app -Recurse -Force
