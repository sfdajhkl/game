# Windows PowerShell 5.1 compatible. Read append-only UTF-8 logs without locking Unity out.
function New-ArvitoLogState([string]$Path, [switch]$WaitForChange) {
    $baseline = ''
    if (Test-Path -LiteralPath $Path -PathType Leaf) {
        $info = Get-Item -LiteralPath $Path
        $baseline = "$($info.Length)/$($info.LastWriteTimeUtc.Ticks)"
    }
    return [pscustomobject]@{
        Path = $Path; Position = [long]0; Creation = [long]0; Pending = ''
        Decoder = [Text.Encoding]::UTF8.GetDecoder()
        Bytes = [byte[]](New-Object byte[] 8192); Chars = [char[]](New-Object char[] 8193)
        Baseline = $baseline; WaitForChange = [bool]$WaitForChange
    }
}
function Read-ArvitoLog($State, [switch]$Final) {
    $lines = New-Object 'System.Collections.Generic.List[string]'
    $stream = $null
    try {
        if (-not (Test-Path -LiteralPath $State.Path -PathType Leaf)) { return }
        $info = Get-Item -LiteralPath $State.Path
        if ($State.WaitForChange -and "$($info.Length)/$($info.LastWriteTimeUtc.Ticks)" -eq $State.Baseline) { return }
        $State.WaitForChange = $false
        $stream = [IO.File]::Open($State.Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
        if ($stream.Length -lt $State.Position -or ($State.Creation -ne 0 -and $State.Creation -ne $info.CreationTimeUtc.Ticks)) {
            $State.Position = [long]0; $State.Pending = ''; $State.Decoder.Reset()
        }
        $State.Creation = $info.CreationTimeUtc.Ticks
        $null = $stream.Seek($State.Position, [IO.SeekOrigin]::Begin)
        while (($read = $stream.Read($State.Bytes, 0, $State.Bytes.Length)) -gt 0) {
            $State.Position += $read
            $count = $State.Decoder.GetChars($State.Bytes, 0, $read, $State.Chars, 0, $false)
            $State.Pending += [string]::new($State.Chars, 0, $count)
            while (($newline = $State.Pending.IndexOf("`n")) -ge 0) {
                $lines.Add($State.Pending.Substring(0, $newline).TrimEnd("`r").TrimStart([char]0xFEFF))
                $State.Pending = $State.Pending.Substring($newline + 1)
            }
        }
        if ($Final) {
            $count = $State.Decoder.GetChars($State.Bytes, 0, 0, $State.Chars, 0, $true)
            $State.Pending += [string]::new($State.Chars, 0, $count)
            if ($State.Pending.Length) { $lines.Add($State.Pending.TrimEnd("`r").TrimStart([char]0xFEFF)); $State.Pending = '' }
        }
    }
    catch [IO.IOException] { } # A temporary write/share conflict must not stop a build.
    catch [UnauthorizedAccessException] { }
    finally { if ($stream) { $stream.Dispose() } }
    return $lines.ToArray()
}
function Get-ArvitoBuildStage([string]$Line, [string]$Current) {
    if ($Line -match 'Windows build created:') { return 'Unity build complete' }
    if ($Line -match 'BuildPipeline|BuildPlayer|Building Player|Building player') { return 'Building Windows player' }
    if ($Line -match 'Arvito prepared|Arvito Unity JSON') { return 'Preparing Arvito' }
    if ($Line -match 'ShaderCompiler|Compiling shader|shader compilation') { return 'Compiling shaders' }
    if ($Line -match 'ScriptCompilation|Compiling scripts|Csc |CSharpCompiler') { return 'Compiling C#' }
    if ($Line -match 'AssetImport|Importing asset|Importing Assets|Start importing') { return 'Importing assets' }
    if ($Line -match '\[Package Manager\]|Resolving packages|Registering packages') { return 'Package manager' }
    return $Current
}
function Wait-ArvitoUnityBuild($Process, $LogState) {
    $timer = [Diagnostics.Stopwatch]::StartNew()
    $lastOutput = 0.0; $lastStatus = -5.0; $stage = 'Starting Unity'
    Write-Host "Live log: $($LogState.Path)"
    try {
        while ($true) {
            $previousPosition = $LogState.Position
            foreach ($line in @(Read-ArvitoLog $LogState)) {
                Write-Host $line
                $stage = Get-ArvitoBuildStage $line $stage
                $lastOutput = $timer.Elapsed.TotalSeconds
            }
            if ($LogState.Position -ne $previousPosition) { $lastOutput = $timer.Elapsed.TotalSeconds }
            $elapsed = $timer.Elapsed.TotalSeconds
            if ($elapsed - $lastStatus -ge 5) {
                $quiet = [int]($elapsed - $lastOutput)
                $status = "Elapsed $($timer.Elapsed.ToString('hh\:mm\:ss')) | Latest stage: $stage | No new log for ${quiet}s"
                Write-Host "[Arvito] Unity process running | $status"
                Write-Progress -Id 41 -Activity 'Arvito Windows build' -Status $status -PercentComplete -1
                $lastStatus = $elapsed
            }
            $Process.Refresh()
            if ($Process.HasExited) { break }
            Start-Sleep -Milliseconds 500
        }
        $Process.WaitForExit()
        foreach ($line in @(Read-ArvitoLog $LogState -Final)) { Write-Host $line }
        Write-Host "[Arvito] Unity exited with code $($Process.ExitCode). Elapsed $($timer.Elapsed.ToString('hh\:mm\:ss'))."
        return [int]$Process.ExitCode
    }
    finally {
        $timer.Stop()
        Write-Progress -Id 41 -Activity 'Arvito Windows build' -Completed
    }
}
