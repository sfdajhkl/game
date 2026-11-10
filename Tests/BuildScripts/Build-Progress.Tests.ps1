# Dependency-free UTF-8 and real child-process checks; no Unity or installer runs.
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
. (Join-Path $root 'Unity/Arvito/Tools/Build-Progress.ps1')
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('arvito-progress-check-' + [guid]::NewGuid().ToString('N'))
$outputLines = New-Object 'System.Collections.Generic.List[string]'
$observation = [pscustomobject]@{ Live = $false }
$childProcess = $null
function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Append-Bytes([string]$Path, [byte[]]$Bytes) {
    $writer = [IO.File]::Open($Path, [IO.FileMode]::Append, [IO.FileAccess]::Write, [IO.FileShare]::ReadWrite)
    try { $writer.Write($Bytes, 0, $Bytes.Length) } finally { $writer.Dispose() }
}
function Write-Host {
    param([object]$Object)
    $outputLines.Add([string]$Object)
    if ($childProcess -and -not $childProcess.HasExited -and [string]$Object -match 'Resolving packages') { $observation.Live = $true }
}
try {
    New-Item -ItemType Directory -Path $fixture | Out-Null
    $log = Join-Path $fixture 'unicode.log'
    $word = [string][char]0xD55C + [char]0xAE00
    $bytes = [Text.Encoding]::UTF8.GetBytes($word)
    [IO.File]::WriteAllBytes($log, [byte[]]@($bytes[0]))
    $state = New-ArvitoLogState $log
    Assert-True (@(Read-ArvitoLog $state).Count -eq 0) 'A split UTF-8 character was emitted early.'
    Append-Bytes $log ([byte[]]$bytes[1..($bytes.Length - 1)])
    Assert-True (@(Read-ArvitoLog $state).Count -eq 0) 'An unfinished line was emitted early.'
    Append-Bytes $log ([Text.Encoding]::UTF8.GetBytes("`nlast fragment"))
    $lines = @(Read-ArvitoLog $state)
    Assert-True ($lines.Count -eq 1 -and $lines[0] -ceq $word) 'Split Korean UTF-8 text was corrupted or duplicated.'
    $last = @(Read-ArvitoLog $state -Final)
    Assert-True ($last.Count -eq 1 -and $last[0] -ceq 'last fragment') 'The final unterminated error line was lost.'
    [Console]::WriteLine('PASS split Korean UTF-8, partial lines and final fragments are preserved')

    [IO.File]::WriteAllText($log, "previous build history`n")
    $state = New-ArvitoLogState $log -WaitForChange
    Assert-True (@(Read-ArvitoLog $state).Count -eq 0) 'Old build history was printed before this run wrote its log.'
    [IO.File]::WriteAllText($log, "new run`n")
    $lines = @(Read-ArvitoLog $state)
    Assert-True ($lines.Count -eq 1 -and $lines[0] -ceq 'new run') 'The new build log was not shown.'
    [IO.File]::WriteAllText($log, "x`n")
    $lines = @(Read-ArvitoLog $state)
    Assert-True ($lines.Count -eq 1 -and $lines[0] -ceq 'x') 'A truncated log was not restarted correctly.'
    [Console]::WriteLine('PASS previous-run logs are skipped and log truncation resets reading')

    $shared = Join-Path $fixture 'shared.log'
    $writer = [IO.File]::Open($shared, [IO.FileMode]::Create, [IO.FileAccess]::Write, [IO.FileShare]::ReadWrite)
    try {
        $data = [Text.Encoding]::UTF8.GetBytes("writer still open`n"); $writer.Write($data, 0, $data.Length); $writer.Flush()
        $lines = @(Read-ArvitoLog (New-ArvitoLogState $shared))
        Assert-True ($lines.Count -eq 1 -and $lines[0] -ceq 'writer still open') 'Live reading blocked the log writer.'
    }
    finally { $writer.Dispose() }
    [Console]::WriteLine('PASS log can be read while a writer holds it open')

    $child = Join-Path $fixture 'child process.ps1'
    @'
param([string]$LogFile)
$encoding = [Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText($LogFile, "[Package Manager] Resolving packages.`n", $encoding)
Start-Sleep -Milliseconds 1400
[IO.File]::AppendAllText($LogFile, 'Final simulated error', $encoding)
exit 7
'@ | Set-Content -LiteralPath $child -Encoding UTF8
    $live = Join-Path $fixture 'live.log'
    $state = New-ArvitoLogState $live -WaitForChange
    $shell = (Get-Process -Id $PID).Path
    $childProcess = Start-Process -FilePath $shell -ArgumentList @('-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"{0}"' -f $child), '-LogFile', ('"{0}"' -f $live)) -WorkingDirectory $fixture -PassThru
    $exitCode = Wait-ArvitoUnityBuild $childProcess $state
    Assert-True $observation.Live 'The log was only displayed after process exit.'
    Assert-True ($exitCode -eq 7) 'The child process failure code was lost.'
    Assert-True ($outputLines.Contains('Final simulated error')) 'The final error was not drained after exit.'
    Assert-True (@($outputLines | Where-Object { $_ -match 'Elapsed .*No new log for' }).Count -gt 0) 'The elapsed/quiet-time heartbeat was not displayed.'
    [Console]::WriteLine('PASS live output precedes real child-process exit and final error/exit code are retained')
    [Console]::WriteLine('4 progress checks passed. Child process only; actual Unity on Windows was not executed.')
}
finally {
    if ($childProcess) { $childProcess.WaitForExit(); $childProcess.Dispose() }
    if (Test-Path -LiteralPath $fixture) { Remove-Item -LiteralPath $fixture -Recurse -Force }
}
