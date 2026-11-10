param([string]$LogPath)
$ErrorActionPreference = 'Stop'
if (-not $LogPath) {
    $root = Split-Path -Parent $PSScriptRoot
    foreach ($relative in @('Builds/unity-build.log', 'Unity/Arvito/Builds/unity-build.log', 'Arvito/Builds/unity-build.log')) {
        $candidate = Join-Path $root $relative
        if (Test-Path -LiteralPath $candidate -PathType Leaf) { $LogPath = $candidate; break }
    }
}
if (-not $LogPath -and $env:OS -eq 'Windows_NT') {
    Add-Type -AssemblyName System.Windows.Forms
    $picker = New-Object Windows.Forms.OpenFileDialog
    try {
        $picker.Title = 'Select the running build log: Arvito/Builds/unity-build.log'
        $picker.Filter = 'Unity build log (unity-build.log)|unity-build.log|Log files (*.log)|*.log'
        $picker.InitialDirectory = [Environment]::GetFolderPath('Desktop')
        if ($picker.ShowDialog() -ne [Windows.Forms.DialogResult]::OK) { exit }
        $LogPath = $picker.FileName
    }
    finally { $picker.Dispose() }
}
if (-not $LogPath -or -not (Test-Path -LiteralPath $LogPath -PathType Leaf)) { throw 'Select the existing Arvito/Builds/unity-build.log, or pass -LogPath <file>.' }
Write-Host "Watching: $LogPath"
Write-Host 'New lines appear as Unity writes them. Ctrl+C closes this viewer; the build keeps running.'
Get-Content -LiteralPath $LogPath -Encoding UTF8 -Tail 30 -Wait
