$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
$app = Join-Path $env:LOCALAPPDATA 'Programs\Arvito'
$marker = Join-Path $app 'arvito-install.json'
$staging = $app + '.staging-' + [guid]::NewGuid().ToString('N')
$backup = $app + '.backup-' + [guid]::NewGuid().ToString('N')
try {
    if (Get-Process -Name Arvito -ErrorAction SilentlyContinue) { throw 'Close Arvito before installing this version.' }
    if ((Test-Path -LiteralPath $app) -and -not (Test-Path -LiteralPath $marker)) { throw 'The destination contains unrecognized files. No files have been replaced.' }
    New-Item -ItemType Directory -Path $staging -Force | Out-Null
    Expand-Archive -LiteralPath (Join-Path $PSScriptRoot 'Arvito-Windows.zip') -DestinationPath $staging
    foreach ($required in @('Arvito.exe', 'UnityPlayer.dll', 'Arvito_Data')) {
        if (-not (Test-Path -LiteralPath (Join-Path $staging $required))) { throw 'The installation archive is incomplete.' }
    }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Uninstall.ps1') -Destination $staging
    '{"product":"Arvito","version":"0.2.0"}' | Set-Content -LiteralPath (Join-Path $staging 'arvito-install.json') -Encoding UTF8
    if (Test-Path -LiteralPath $app) { Move-Item -LiteralPath $app -Destination $backup }
    try { Move-Item -LiteralPath $staging -Destination $app }
    catch { if (Test-Path -LiteralPath $backup) { Move-Item -LiteralPath $backup -Destination $app }; throw }
    $shell = New-Object -ComObject WScript.Shell
    $menu = Join-Path ([Environment]::GetFolderPath('Programs')) 'Arvito'
    New-Item -ItemType Directory -Force -Path $menu | Out-Null
    foreach ($path in @((Join-Path $menu 'Arvito.lnk'), (Join-Path ([Environment]::GetFolderPath('Desktop')) 'Arvito.lnk'))) {
        $shortcut = $shell.CreateShortcut($path); $shortcut.TargetPath = Join-Path $app 'Arvito.exe'; $shortcut.WorkingDirectory = $app; $shortcut.Save()
    }
    $key = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ArvitoPlaytest'
    New-Item -Path $key -Force | Out-Null
    $uninstall = 'powershell.exe -NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $app 'Uninstall.ps1') + '"'
    foreach ($entry in @{ DisplayName='Arvito Playtest'; DisplayVersion='0.2.0'; Publisher='Arvito'; InstallLocation=$app; DisplayIcon=(Join-Path $app 'Arvito.exe'); UninstallString=$uninstall }.GetEnumerator()) {
        New-ItemProperty -Path $key -Name $entry.Key -Value $entry.Value -PropertyType String -Force | Out-Null
    }
    if (Test-Path -LiteralPath $backup) { Remove-Item -LiteralPath $backup -Recurse -Force }
    $answer = [Windows.Forms.MessageBox]::Show('Arvito is installed. Start playing now?', 'ARVITO', 'YesNo', 'Information')
    if ($answer -eq 'Yes') { Start-Process -FilePath (Join-Path $app 'Arvito.exe') -WorkingDirectory $app }
}
catch { [Windows.Forms.MessageBox]::Show($_.Exception.Message, 'ARVITO installation failed', 'OK', 'Error') | Out-Null; exit 1 }
finally { if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force } }
