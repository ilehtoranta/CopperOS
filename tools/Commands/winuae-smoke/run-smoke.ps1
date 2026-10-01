<#
.SYNOPSIS
Boots WinUAE from a host directory whose C: holds only the out/C binaries,
runs the cases in smoke-cases.txt from S:Startup-Sequence, and reports what
each command printed and returned.

.DESCRIPTION
No disk image and no Workbench files are needed. The boot volume is a fresh
host folder (a WinUAE directory filesystem, bootable) containing
  C/<every out/C command>, C/UAEquit (from the WinUAE install),
  S/Startup-Sequence (generated), smoke-input.txt.
The Kickstart ROM is the only external input. -Aros uses WinUAE's built-in
AROS ROM replacement instead: that checks the harness and gives a second
opinion, but it is NOT Kickstart 3.1.

The Amiga side appends to SYS:smoke.log. Each case is bracketed by
  @@BEGIN <name>   ...output...   @@END <name> rc <RC> r2 <Result2>
If the log stops growing for -StallSeconds, the running case is recorded as
crashed-or-hung, WinUAE is stopped, and a new boot resumes with the next case.
A boot ends with C:UAEquit.

Reference replays (reference-replays.txt, skipped with -NoReplay) re-run the
probes behind captures of the ORIGINAL commands, verbatim, against out/C:
  probe-script captures (probe.source + probe.capture_text): the whole probe
    script up to its Echo "END" line, Echo lines included, with only the
    capture target rewritten to SYS:replay/<case>.txt; the source file must
    still hash to probe.source_sha256.
  observation captures (observations[]): one case per command line, compared
    on output and return code.
Status is match / mismatch / no-capture instead of the RC.

Outputs (under -OutputDirectory, default artifacts/winuae-smoke/<timestamp>):
  boot-N/sys/     boot volume of boot N, including its smoke.log
  boot-N/smoke.uae
  results.json    one record per case
  summary.txt     the table printed at the end
#>
param(
    [string]$KickstartRom = $env:COPPEROS_KICKSTART31_ROM,
    [switch]$Aros,
    [string]$WinUAE = 'C:\Program Files\WinUAE\winuae64.exe',
    [string]$CommandDirectory,
    [string]$CasesFile = (Join-Path $PSScriptRoot 'smoke-cases.txt'),
    [string]$ReplaysFile = (Join-Path $PSScriptRoot 'reference-replays.txt'),
    [switch]$NoReplay,
    [string[]]$Only,
    [string]$OutputDirectory,
    [int]$BootSeconds = 120,
    [int]$StallSeconds = 30,
    [int]$ReplayStallSeconds = 10,
    [int]$StackBytes = 0,
    [switch]$Window
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
if (-not $CommandDirectory) { $CommandDirectory = Join-Path $repo 'out\C' }
$latin1 = [Text.Encoding]::GetEncoding(28591)

# ---- Inputs --------------------------------------------------------------
if (-not (Test-Path -LiteralPath $WinUAE)) { throw "WinUAE not found at '$WinUAE'." }
if ($Aros) {
    $romSetting = ':AROS'
    $romLabel = 'WinUAE built-in AROS ROM (not Kickstart 3.1)'
    $romSha = $null
} else {
    if (-not $KickstartRom) {
        foreach ($default in @((Join-Path $repo '..\..\TestData\ROM\kickstart-3.1-a500.rom'), 'D:\TestData\ROM\kickstart-3.1-a500.rom')) {
            if (Test-Path -LiteralPath $default) { $KickstartRom = $default; break }
        }
    }
    if (-not $KickstartRom -or -not (Test-Path -LiteralPath $KickstartRom)) {
        throw 'No Kickstart ROM. Pass -KickstartRom <path> or set COPPEROS_KICKSTART31_ROM (or use -Aros to check the harness only).'
    }
    $KickstartRom = (Resolve-Path -LiteralPath $KickstartRom).Path
    $romSetting = $KickstartRom
    $romSha = (Get-FileHash -LiteralPath $KickstartRom -Algorithm SHA256).Hash.ToLowerInvariant()
    $romLabel = "$KickstartRom (sha256 $romSha)"
}
if (-not (Test-Path -LiteralPath $CommandDirectory)) {
    throw "No command directory at '$CommandDirectory'. Run tools\Commands\build-c.ps1 first."
}
$uaeQuit = Join-Path (Split-Path $WinUAE) 'Amiga Programs\UAEquit'
if (-not (Test-Path -LiteralPath $uaeQuit)) { throw "UAEquit not found at '$uaeQuit'." }
$shipped = @(Get-ChildItem -LiteralPath $CommandDirectory -File | Where-Object { $_.Extension -eq '' })
# Volume label of the boot disk. The Workbench 3.1 reference captures ran from
# the Workbench disk, labelled Workbench3.1, and Which prints paths with it.
$volumeName = 'Workbench3.1'
# Which probes look up C:Execute (and make it Resident). Until Execute ships,
# C/Execute is a stand-in copy of C:Wait: Which only resolves it, never runs it.
$hasExecute = [bool]($shipped | Where-Object Name -eq 'Execute')
$executeStandIn = if ($hasExecute) { $null } else { $shipped | Where-Object Name -eq 'Wait' | Select-Object -First 1 }

# ---- Cases ---------------------------------------------------------------
$cases = @(foreach ($line in Get-Content -LiteralPath $CasesFile) {
    $t = $line.Trim()
    if ($t -eq '' -or $t.StartsWith('#')) { continue }
    $parts = $t -split '\|', 3
    if ($parts.Count -lt 2) { throw "Malformed case line: $line" }
    $argsText = if ($parts.Count -eq 3) { $parts[2].Trim() } else { '' }
    [pscustomobject]@{
        Name = $parts[0].Trim(); Command = $parts[1].Trim(); Arguments = $argsText
        Skip = $argsText.StartsWith('SKIP:')
        Built = [bool]($shipped | Where-Object Name -eq $parts[1].Trim())
        Kind = 'smoke'; Script = $null; Expected = $null; ExpectedRc = $null; Source = $null; Note = $null
    }
})

# ---- Reference replays ---------------------------------------------------
function New-ReplayCase([string]$name, [string]$command, [string[]]$script, [string]$expected, $expectedRc, [string]$source, [string]$note, [bool]$prefix = $false, [bool]$cutAtNul = $false, [bool]$isolated = $false) {
    [pscustomobject]@{
        Name = $name; Command = $command; Arguments = ''; Skip = $false
        Built = [bool]($shipped | Where-Object Name -eq $command)
        Kind = 'replay'; Script = $script; Expected = $expected; ExpectedRc = $expectedRc; Source = $source; Note = $note; Prefix = $prefix; CutAtNul = $cutAtNul; Isolated = $isolated
    }
}
if (-not $NoReplay -and (Test-Path -LiteralPath $ReplaysFile)) {
    $cases += @(foreach ($line in Get-Content -LiteralPath $ReplaysFile) {
        $t = $line.Trim()
        if ($t -eq '' -or $t.StartsWith('#')) { continue }
        $jsonPath = [IO.Path]::Combine($repo, $t)
        if (-not (Test-Path -LiteralPath $jsonPath)) { throw "Reference capture not found: $t" }
        $cap = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
        $command = Split-Path -Leaf $cap.reference.command_path
        $base = 'ref-' + [IO.Path]::GetFileNameWithoutExtension($jsonPath)
        $romNote = if ($romSha -and $cap.runtime.rom_sha256 -eq $romSha) { $null } else { 'ROM differs from the capture' }
        $captureText = $null; $cutAtNul = $false
        if ($cap.PSObject.Properties['probe'] -and $cap.probe) {
            if ($cap.probe.PSObject.Properties['capture_text']) { $captureText = $cap.probe.capture_text }
            elseif ($cap.probe.PSObject.Properties['capture_text_before_nul']) {
                # The captured file carried NUL padding after the text; only the text
                # before the first NUL was recorded, so compare ours the same way.
                $captureText = $cap.probe.capture_text_before_nul; $cutAtNul = $true
            }
        }
        if ($null -ne $captureText) {
            # Probe script: replay verbatim through its last line that writes the
            # capture; only the capture target changes. Copy/EndCLI after it only
            # persisted the capture.
            $src = Join-Path $repo $cap.probe.source
            if (-not (Test-Path -LiteralPath $src)) { throw "Probe source not found: $($cap.probe.source)" }
            # Hash the LF form: the capture hashed the probe as run. .gitattributes
            # keeps fixtures LF, but an older checkout may still carry CRs.
            $srcText = $latin1.GetString([IO.File]::ReadAllBytes($src)).Replace("`r`n", "`n")
            $sha = ([BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash($latin1.GetBytes($srcText))) -replace '-', '').ToLowerInvariant()
            if ($sha -ne $cap.probe.source_sha256) {
                throw "Probe $($cap.probe.source) is not the captured script (sha256 $sha, capture records $($cap.probe.source_sha256))."
            }
            $lines = @($srcText -split "`n")
            # The capture target is the file the probe's first Echo creates (">", not
            # ">>"); usually Echo "BEGIN", but some probes open with a status line.
            $b = -1; $target = $null
            for ($k = 0; $k -lt $lines.Count; $k++) {
                if ($lines[$k] -match '^\s*Echo\s+"[^"]*"\s+>([^>\s]\S*)\s*$') { $b = $k; $target = $matches[1]; break }
            }
            if ($b -lt 0) { throw "Probe $($cap.probe.source) has no 'Echo `"...`" >target' line." }
            $e = -1
            for ($k = $lines.Count - 1; $k -gt $b; $k--) {
                if ($lines[$k] -match ('>>?' + [regex]::Escape($target) + '\s*$')) { $e = $k; break }
            }
            if ($e -lt 0) { $e = $b }
            $file = "SYS:replay/$base.txt"
            $script = @($lines[0..$e] | ForEach-Object { $_.Replace(">$target", ">$file") })
            $note = $romNote
            if (-not $hasExecute -and $command -ne 'Execute' -and $srcText -match 'C:Execute') {
                $note = (@($note, 'C:Execute is a stand-in') | Where-Object { $_ }) -join '; '
            }
            # Some original runs stopped before the probe's last marker (usually
            # END; their captures say the cause is not established). Those captures
            # are a prefix of the original's output, so only that prefix is compared.
            $prefix = $false
            if ($lines[$e] -match '^\s*Echo\s+"([^"]*)"') { $prefix = -not $captureText.EndsWith("$($matches[1])`n") }
            New-ReplayCase $base $command $script $captureText $null $t $note $prefix $cutAtNul $true
        } elseif ($cap.PSObject.Properties['observations']) {
            $i = 0
            foreach ($o in $cap.observations) {
                $i++
                $name = "$base-$i"
                New-ReplayCase $name $command @("$($o.command) >SYS:replay/$name.txt") $o.output $o.return $t $romNote
            }
        } else {
            throw "Reference capture $t has neither probe.capture_text nor observations."
        }
    })
}
if ($Only) {
    # powershell -File passes "a,b" as one string; accept both forms.
    $Only = @($Only | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    $cases = @($cases | Where-Object { $Only -contains $_.Name -or $Only -contains $_.Command })
}
$dupes = @($cases | Group-Object Name | Where-Object Count -gt 1)
if ($dupes) { throw "Duplicate case names: $(($dupes | ForEach-Object Name) -join ', ')" }
$runnable = @($cases | Where-Object { -not $_.Skip -and $_.Built })
$replayNames = [Collections.Generic.HashSet[string]]::new([string[]]@($cases | Where-Object Kind -eq 'replay' | ForEach-Object Name))

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repo ('artifacts\winuae-smoke\' + (Get-Date -Format 'yyyyMMdd-HHmmss') + $(if ($Aros) { '-aros' } else { '' }))
}
$run = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $run) { throw "Output directory '$run' already exists." }
New-Item -ItemType Directory -Force -Path $run | Out-Null

function New-BootVolume([string]$dir, [object[]]$batch) {
    $sys = Join-Path $dir 'sys'
    New-Item -ItemType Directory -Force -Path (Join-Path $sys 'C'), (Join-Path $sys 'S'), (Join-Path $sys 'T'), (Join-Path $sys 'replay') | Out-Null
    foreach ($f in $shipped) { Copy-Item -LiteralPath $f.FullName -Destination (Join-Path $sys "C\$($f.Name)") }
    Copy-Item -LiteralPath $uaeQuit -Destination (Join-Path $sys 'C\UAEquit')
    if ($executeStandIn) { Copy-Item -LiteralPath $executeStandIn.FullName -Destination (Join-Path $sys 'C\Execute') }
    [IO.File]::WriteAllText((Join-Path $sys 'smoke-input.txt'),
        "CopperOS smoke input`nline two mentions copper again`nthird line`n", $latin1)
    # AmigaDOS script: LF endings, Latin-1. Markers avoid '=' (ReadArgs keyword syntax).
    $s = [Text.StringBuilder]::new()
    [void]$s.Append("Failat 21`n")
    if ($StackBytes -gt 0) { [void]$s.Append("Stack $StackBytes`n") }
    [void]$s.Append("Echo >SYS:smoke.log `"@@SMOKE-START`"`n")
    [void]$s.Append("C:MakeDir >NIL: RAM:smoke`n")
    foreach ($c in $batch) {
        [void]$s.Append("Echo >>SYS:smoke.log `"@@BEGIN $($c.Name)`"`n")
        if ($c.Kind -eq 'replay') {
            foreach ($l in $c.Script) { [void]$s.Append("$l`n") }
        } else {
            [void]$s.Append(("C:$($c.Command) <NIL: >>SYS:smoke.log $($c.Arguments)").TrimEnd() + "`n")
        }
        [void]$s.Append("Echo >>SYS:smoke.log `"@@END $($c.Name) rc `$RC r2 `$Result2`"`n")
        # A probe's own FailAt must not decide whether later cases run.
        if ($c.Kind -eq 'replay') { [void]$s.Append("Failat 21`n") }
    }
    [void]$s.Append("Echo >>SYS:smoke.log `"@@SMOKE-DONE`"`n")
    [void]$s.Append("C:UAEquit`n")
    [IO.File]::WriteAllText((Join-Path $sys 'S\Startup-Sequence'), $s.ToString(), $latin1)
    $config = @(
        'config_description=CopperOS C: smoke test'
        'use_gui=no'
        "headless=$(if ($Window) { 'false' } else { 'true' })"
        "kickstart_rom_file=$romSetting"
        'cpu_type=68000', 'cpu_model=68000', 'cpu_compatible=false', 'cpu_speed=max'
        'chipset=ecs', 'chipmem_size=4', 'fastmem_size=0', 'bogomem_size=0'
        # One empty DD drive: Kickstart always mounts DF0:, and with no drive
        # behind it the trackdisk handler never answers a Lock("DF0:").
        'nr_floppies=1', 'floppy0type=0', 'floppy0=', 'sound_output=none'
        "filesystem2=rw,DH0:$($volumeName):$sys,1"
    )
    $configPath = Join-Path $dir 'smoke.uae'
    [IO.File]::WriteAllLines($configPath, $config, [Text.UTF8Encoding]::new($false))
    return @{ Sys = $sys; Config = $configPath; Log = (Join-Path $sys 'smoke.log') }
}

function Read-Log([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { return @() }
    # WinUAE may hold the file open for writing: share, do not lock it out.
    $fs = [IO.File]::Open($path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
    try { $ms = [IO.MemoryStream]::new(); $fs.CopyTo($ms); $text = $latin1.GetString($ms.ToArray()) } finally { $fs.Dispose() }
    # A command whose last output has no newline (a ReadArgs '?' prompt, a
    # partial line) leaves the next marker mid-line; move markers to their own line.
    $text = [regex]::Replace($text, '(?<=[^\n])(@@(BEGIN|END|SMOKE-DONE))', "`n`$1")
    return @($text -split "`r?`n")
}

# ---- Boots ---------------------------------------------------------------
Write-Host "ROM: $romLabel"
Write-Host "Commands: $($shipped.Count) in $CommandDirectory; runnable cases: $($runnable.Count)"
$results = @{}
$remaining = $runnable
$boot = 0
$started = Get-Date
while ($remaining.Count -gt 0) {
    $boot++
    # Each original probe ran on a freshly booted disk, and probes leave state
    # behind (RAM:T, assigns, residents, FailAt). Give every probe replay a boot
    # of its own; smoke cases and single-line replays share boots.
    $take = 1
    if (-not ($remaining[0].PSObject.Properties['Isolated'] -and $remaining[0].Isolated)) {
        while ($take -lt $remaining.Count -and
               -not ($remaining[$take].PSObject.Properties['Isolated'] -and $remaining[$take].Isolated)) { $take++ }
    }
    $batch = @($remaining[0..($take - 1)])
    $rest = @(if ($take -lt $remaining.Count) { $remaining[$take..($remaining.Count - 1)] })
    $vol = New-BootVolume (Join-Path $run "boot-$boot") $batch
    Write-Host "Boot $boot : $($batch.Count) case(s) from '$($batch[0].Name)'"
    $proc = Start-Process -FilePath $WinUAE -ArgumentList @('-nobootlog', '-f', "`"$($vol.Config)`"") -PassThru
    $bootStart = Get-Date; $lastSize = -1; $lastChange = Get-Date; $reason = 'exited'; $stallLimit = $StallSeconds
    while (-not $proc.WaitForExit(1000)) {
        $size = if (Test-Path -LiteralPath $vol.Log) { (Get-Item -LiteralPath $vol.Log).Length } else { -1 }
        if ($size -ne $lastSize) {
            $lastSize = $size; $lastChange = Get-Date
            # Probes run in well under a second and write to SYS:replay, not the log.
            # One that stops early (its own FailAt, as in the original) leaves the
            # boot shell idle, so give up on it sooner than on a smoke case.
            $current = $null
            try { $current = @(Read-Log $vol.Log | Where-Object { $_.StartsWith('@@') }) | Select-Object -Last 1 } catch { }
            $stallLimit = if ($current -and $current.StartsWith('@@BEGIN ') -and $replayNames.Contains($current.Substring(8))) { $ReplayStallSeconds } else { $StallSeconds }
        }
        $idle = ((Get-Date) - $lastChange).TotalSeconds
        if ($size -lt 0 -and ((Get-Date) - $bootStart).TotalSeconds -gt $BootSeconds) { $reason = 'no-boot'; break }
        if ($size -ge 0 -and $idle -gt $stallLimit) { $reason = 'stalled'; break }
    }
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force; $proc.WaitForExit(10000) | Out-Null }

    $log = Read-Log $vol.Log
    if ($log -notcontains '@@SMOKE-START') {
        foreach ($c in $remaining) { $results[$c.Name] = @{ status = 'no-boot'; boot = $boot } }
        Write-Warning "Boot $boot never started the script ($reason). Stopping."
        break
    }
    $next = @()
    for ($i = 0; $i -lt $batch.Count; $i++) {
        $c = $batch[$i]
        $b = [Array]::IndexOf($log, "@@BEGIN $($c.Name)")
        if ($b -lt 0) { $next = @($batch[$i..($batch.Count - 1)]); break }
        $e = -1
        for ($k = $b + 1; $k -lt $log.Count; $k++) {
            if ($log[$k].StartsWith("@@END $($c.Name) ")) { $e = $k; break }
            if ($log[$k].StartsWith('@@')) { break }
        }
        $stop = if ($e -ge 0) { $e } else { $log.Count }
        $out = @(if ($stop - 1 -ge $b + 1) { $log[($b + 1)..($stop - 1)] })
        if ($e -lt 0 -and $c.Kind -ne 'replay') {
            while ($out.Count -gt 0 -and $out[-1] -eq '') { $out = @($out | Select-Object -SkipLast 1) }
            $results[$c.Name] = @{ status = 'crashed-or-hung'; boot = $boot; output = $out }
            $next = @(if ($i + 1 -lt $batch.Count) { $batch[($i + 1)..($batch.Count - 1)] })
            break
        }
        $rc = $null; $r2 = $null
        if ($e -ge 0 -and $log[$e] -match ' rc (\S*) r2 (\S*)') { $rc = $matches[1]; $r2 = $matches[2] }
        if ($c.Kind -eq 'replay') {
            # A probe that stopped before its END line (its FailAt aborted the
            # script) is still judged on its capture: several originals stopped too.
            $capPath = Join-Path $vol.Sys "replay\$($c.Name).txt"
            $actual = if (Test-Path -LiteralPath $capPath) { $latin1.GetString([IO.File]::ReadAllBytes($capPath)) } else { $null }
            if ($null -ne $actual -and $c.CutAtNul -and $actual.IndexOf([char]0) -ge 0) { $actual = $actual.Substring(0, $actual.IndexOf([char]0)) }
            $note = if ($e -lt 0) { 'script stopped before END' } else { $null }
            $addNote = { param($n) (@($note, $n) | Where-Object { $_ }) -join '; ' }
            if ($null -eq $actual) { $status = 'no-capture' }
            elseif ($null -ne $c.ExpectedRc -and "$($c.ExpectedRc)" -ne $rc) { $status = 'mismatch'; $note = & $addNote "rc $rc, original $($c.ExpectedRc)" }
            elseif ($c.Prefix -and $actual.StartsWith($c.Expected, [StringComparison]::Ordinal)) { $status = 'prefix-match' }
            elseif ($actual -cne $c.Expected) {
                $status = 'mismatch'
                $al = $actual -split "`n"; $el = @($c.Expected -split "`n")
                # A prefix capture ends in LF; its empty last piece is not a line.
                if ($c.Prefix -and $el.Count -gt 1 -and $el[-1] -eq '') { $el = @($el[0..($el.Count - 2)]) }
                $maxK = if ($c.Prefix) { $el.Count } else { [Math]::Max($al.Count, $el.Count) }
                for ($k = 0; $k -lt $maxK; $k++) {
                    $av = if ($k -lt $al.Count) { $al[$k] } else { '<none>' }
                    $ev = if ($k -lt $el.Count) { $el[$k] } else { '<none>' }
                    if ($av -cne $ev) { $note = & $addNote "line $($k + 1): got '$av', original '$ev'"; break }
                }
            } else { $status = 'match' }
            $capLines = @(if ($null -ne $actual) { $actual -split "`n" })
            $results[$c.Name] = @{ status = $status; rc = $rc; result2 = $r2; boot = $boot; output = $capLines; console = $out; note = $note }
            if ($e -lt 0) {
                $next = @(if ($i + 1 -lt $batch.Count) { $batch[($i + 1)..($batch.Count - 1)] })
                break
            }
            continue
        }
        $status = switch ($rc) { '0' { 'ok' } '5' { 'warn' } '10' { 'error' } '20' { 'fail' } default { "rc-$rc" } }
        $results[$c.Name] = @{ status = $status; rc = $rc; result2 = $r2; boot = $boot; output = $out }
    }
    if ($next.Count -eq $batch.Count) {
        Write-Warning "Boot $boot made no progress ($reason). Stopping."
        foreach ($c in $remaining) { $results[$c.Name] = @{ status = 'not-reached'; boot = $boot } }
        break
    }
    $remaining = @($next) + @($rest)
}
$elapsed = [int]((Get-Date) - $started).TotalSeconds

# ---- Report --------------------------------------------------------------
$records = foreach ($c in $cases) {
    $r = if ($c.Skip) { @{ status = 'skipped'; note = $c.Arguments.Substring(5).Trim() } }
         elseif (-not $c.Built) { @{ status = 'not-built' } }
         elseif ($results.ContainsKey($c.Name)) { $results[$c.Name] }
         else { @{ status = 'not-reached' } }
    $rec = [ordered]@{
        name = $c.Name; command = $c.Command; arguments = $(if ($c.Skip) { '' } else { $c.Arguments })
        status = $r.status; rc = $r['rc']; result2 = $r['result2']; boot = $r['boot']
        output = @($(if ($r['output']) { $r['output'] } else { @() })); note = $r['note']
    }
    if ($c.Kind -eq 'replay') {
        $rec.note = (@($r['note'], $c.Note) | Where-Object { $_ }) -join '; '
        $rec.source = $c.Source
        $rec.script = $c.Script
        $rec.expected = $c.Expected
        $rec.expected_rc = $c.ExpectedRc
        $rec.console = @($(if ($r['console']) { $r['console'] } else { @() }))
    }
    [pscustomobject]$rec
}
$records | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $run 'results.json') -Encoding utf8
$lines = @(
    "ROM: $romLabel"
    "WinUAE $((Get-Item -LiteralPath $WinUAE).VersionInfo.ProductVersion), 68000 ECS 2 MB chip; $boot boot(s), $elapsed s"
    ''
    ('{0,-38} {1,-17} {2,-18} {3}' -f 'case', 'command', 'status (rc/r2)', 'first output line')
)
foreach ($r in $records) {
    $first = if ($r.note) { $r.note } elseif ($r.output.Count) { $r.output[0] } else { '' }
    if ($first.Length -gt 60) { $first = $first.Substring(0, 60) }
    $st = if ($null -ne $r.rc) { "$($r.status) ($($r.rc)/$($r.result2))" } else { $r.status }
    $lines += ('{0,-38} {1,-17} {2,-18} {3}' -f $r.name, $r.command, $st, $first)
}
$lines += ''
$lines += 'Totals: ' + (($records | Group-Object status | Sort-Object Name | ForEach-Object { "$($_.Name)=$($_.Count)" }) -join ', ')
$lines | Set-Content -LiteralPath (Join-Path $run 'summary.txt') -Encoding utf8
$lines | ForEach-Object { Write-Host $_ }
Write-Host "Results: $run"
