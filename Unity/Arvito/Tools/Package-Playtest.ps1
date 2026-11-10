param([Parameter(Mandatory=$true)][string]$PlayerFolder, [Parameter(Mandatory=$true)][string]$OutputFolder)
$ErrorActionPreference = 'Stop'
$PlayerFolder = (Resolve-Path -LiteralPath $PlayerFolder).Path
New-Item -ItemType Directory -Force -Path $OutputFolder | Out-Null
$OutputFolder = (Resolve-Path -LiteralPath $OutputFolder).Path
foreach ($required in @('Arvito.exe', 'UnityPlayer.dll', 'Arvito_Data')) {
    if (-not (Test-Path -LiteralPath (Join-Path $PlayerFolder $required))) { throw "Missing built player file: $required" }
}
$zip = Join-Path $OutputFolder 'Arvito-Windows.zip'
Compress-Archive -Path (Join-Path $PlayerFolder '*') -DestinationPath $zip -Force -CompressionLevel Optimal
$iexpress = Join-Path $env:SystemRoot 'System32\iexpress.exe'
if (-not (Test-Path -LiteralPath $iexpress)) { throw "Windows IExpress is unavailable. The portable game is available at $zip" }
$stage = Join-Path $env:TEMP ('Arvito-Package-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage | Out-Null
try {
    Copy-Item -LiteralPath $zip -Destination (Join-Path $stage 'Arvito-Windows.zip')
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Installer\Install.ps1') -Destination $stage
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Installer\Install.cmd') -Destination $stage
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Installer\Uninstall.ps1') -Destination $stage
    $setup = Join-Path $stage 'Arvito-Setup.exe'
    $sed = @"
[Version]
Class=IEXPRESS
SEDVersion=3
[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=0
HideExtractAnimation=0
UseLongFileName=1
InsideCompressed=0
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=Install ARVITO playtest for the current Windows user?
DisplayLicense=
FinishMessage=
TargetName=$setup
FriendlyName=ARVITO Playtest
AppLaunched=cmd.exe /c Install.cmd
PostInstallCmd=<None>
AdminQuietInstCmd=
UserQuietInstCmd=
SourceFiles=SourceFiles
[Strings]
FILE0="Arvito-Windows.zip"
FILE1="Install.ps1"
FILE2="Install.cmd"
FILE3="Uninstall.ps1"
[SourceFiles]
SourceFiles0=$stage\
[SourceFiles0]
%FILE0%=
%FILE1%=
%FILE2%=
%FILE3%=
"@
    $sedPath = Join-Path $stage 'Arvito.sed'
    [IO.File]::WriteAllText($sedPath, $sed, [Text.Encoding]::Default)
    $package = Start-Process -FilePath $iexpress -ArgumentList @('/N', '/Q', ('"{0}"' -f $sedPath)) -Wait -PassThru
    if ($package.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $setup)) { throw "IExpress packaging failed. Portable game remains at $zip" }
    Copy-Item -LiteralPath $setup -Destination (Join-Path $OutputFolder 'Arvito-Setup.exe') -Force
    Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $OutputFolder 'Arvito-Setup.exe'), $zip |
        Format-Table -AutoSize | Out-String | Set-Content -LiteralPath (Join-Path $OutputFolder 'SHA256.txt') -Encoding UTF8
}
finally { Remove-Item -LiteralPath $stage -Recurse -Force }
