param(
    [Parameter(Mandatory)][string]$Stage,
    [string]$Rom = $env:COPPERSHARP_KICKSTART31_ROM,
    [string]$WinUAE = 'C:\Program Files\WinUAE\winuae64.exe',
    [switch]$MorphOS,
    [switch]$Extended
)
$ErrorActionPreference = 'Stop'
$Stage = (Resolve-Path -LiteralPath $Stage).Path
$Rom = (Resolve-Path -LiteralPath $Rom).Path
$dirFaultBytes = [IO.File]::ReadAllBytes("$Stage/runs/part1-68000/FAULTS.FIX")
$dirUnsupported = $null
for ($dirFaultOffset = 0; $dirFaultOffset -lt $dirFaultBytes.Length; $dirFaultOffset += 128) {
    if ($dirFaultBytes[$dirFaultOffset] -eq 0 -and $dirFaultBytes[$dirFaultOffset+1] -eq 0 -and
        $dirFaultBytes[$dirFaultOffset+2] -eq 0 -and $dirFaultBytes[$dirFaultOffset+3] -eq 236) {
        $dirUnsupported = [Text.Encoding]::Latin1.GetString($dirFaultBytes, $dirFaultOffset+4, 124).Split([char]0)[0]+"`n"
    }
}
if ($null -eq $dirUnsupported) { throw 'Require independent original DOS Fault(236) diagnostic.' }
$dirFiles = '  ' + 'empty.txt'.PadRight(32) + " long.txt`n  " + 'one.txt'.PadRight(32) + " spaced name.txt`n"
$dirFull = "     sub (dir)`n" + $dirFiles
$dirRecursive = "     sub (dir)`n       " + 'two.txt'.PadRight(32) + " `n" + $dirFiles
$dirCases = @(
    @{Name='full'; Args='SYS:DirQ'; Output=$dirFull; RC=0; Error=0},
    @{Name='files'; Args='SYS:DirQ FILES'; Output=$dirFiles; RC=0; Error=0},
    @{Name='dirs'; Args='SYS:DirQ DIRS'; Output="     sub (dir)`n"; RC=0; Error=0},
    @{Name='ignored'; Args='SYS:DirQ OPT Z'; Output="Z option ignored`n"+$dirFull; RC=0; Error=0},
    @{Name='all'; Args='SYS:DirQ ALL'; Output=$(if ($MorphOS) {$dirRecursive} else {$dirUnsupported}); RC=$(if ($MorphOS) {0} else {20}); Error=$(if ($MorphOS) {0} else {236})},
    @{Name='opt-i'; Args='SYS:DirQ OPT I'; Output=$(if ($MorphOS) {$dirFull.Replace("`n",'')} else {$dirUnsupported}); RC=$(if ($MorphOS) {0} else {20}); Error=$(if ($MorphOS) {0} else {236})}
)
if ($Extended) {
    $dirCases += @{Name='ignored-lower'; Args='SYS:DirQ OPT z'; Output="z option ignored`n"+$dirFull; RC=0; Error=0},
        @{Name='ignored-multiple'; Args='SYS:DirQ OPT Z?'; Output="Z option ignored`n? option ignored`n"+$dirFull; RC=0; Error=0}
}
$dirRecords = @()
foreach ($dirCpu in '68000', '68020', '68040') {
    $dirRoot = Join-Path $Stage "winuae/$dirCpu"
    if (Test-Path -LiteralPath $dirRoot) { throw 'Preserve prior evidence; use a fresh stage.' }
    $dirSys = Join-Path $dirRoot 'sys'
    New-Item -ItemType Directory -Path "$dirSys/C", "$dirSys/S", "$dirSys/DirQ/sub" -Force | Out-Null
    foreach ($dirName in 'empty.txt','long.txt','one.txt','spaced name.txt') {
        [IO.File]::WriteAllText("$dirSys/DirQ/$dirName", '', [Text.Encoding]::ASCII)
    }
    [IO.File]::WriteAllText("$dirSys/DirQ/sub/two.txt", '', [Text.Encoding]::ASCII)
    Copy-Item -LiteralPath "$Stage/inputs/Dir.baseline" -Destination "$dirSys/C/DirB"
    Copy-Item -LiteralPath "$Stage/inputs/Dir.candidate" -Destination "$dirSys/C/DirC"
    Copy-Item -LiteralPath (Join-Path (Split-Path $WinUAE) 'Amiga Programs/UAEquit') -Destination "$dirSys/C/UAEquit"
    $dirScript = @('Failat 21', 'Stack 4096')
    $dirExpectedReturns = ''
    foreach ($dirImage in 'B','C') {
        foreach ($dirCase in $dirCases) {
            $dirKey = "$dirImage-$($dirCase.Name)"
            $dirScript += "C:Dir$dirImage $($dirCase.Args) <NIL: >SYS:$dirKey.txt"
            $dirRedirect = if ($dirExpectedReturns -eq '') {'>'} else {'>>'}
            $dirScript += 'Echo "' + $dirKey + ' $RC $Result2" ' + $dirRedirect + 'SYS:returns.txt'
            $dirExpectedReturns += "$dirKey $($dirCase.RC) $($dirCase.Error)`n"
        }
    }
    $dirScript += 'Echo DONE >SYS:done.txt', 'C:UAEquit'
    [IO.File]::WriteAllText("$dirSys/S/Startup-Sequence", ($dirScript -join "`n")+"`n", [Text.Encoding]::ASCII)
    $dirConfig = @(
        'config_description=Frozen Dir original DOS confirmation', 'use_gui=no', 'headless=true', "kickstart_rom_file=$Rom"
        "cpu_type=$dirCpu", "cpu_model=$dirCpu", 'fpu_model=0', 'cpu_compatible=false', 'cpu_speed=max', 'cachesize=0'
        'chipset=ecs', 'chipmem_size=4', 'fastmem_size=0', 'bogomem_size=0', 'nr_floppies=1', 'floppy0type=0'
        'floppy0=', 'sound_output=none', "filesystem2=rw,DH0:DirQualification:$dirSys,1"
    )
    $dirConfigPath = Join-Path $dirRoot 'dir.uae'
    [IO.File]::WriteAllLines($dirConfigPath, $dirConfig, [Text.UTF8Encoding]::new($false))
    $dirProcess = Start-Process -FilePath $WinUAE -ArgumentList @('-nobootlog','-f',"`"$dirConfigPath`"") -PassThru -WindowStyle Hidden
    $dirWatch = [Diagnostics.Stopwatch]::StartNew()
    while (-not $dirProcess.WaitForExit(1000) -and $dirWatch.Elapsed.TotalSeconds -lt 45) {}
    $dirExited = $dirProcess.HasExited
    if (-not $dirExited) { Stop-Process -Id $dirProcess.Id -Force; $dirProcess.WaitForExit(5000) | Out-Null }
    $dirCompleted = $dirExited -and (Test-Path -LiteralPath "$dirSys/done.txt")
    $dirChecks = @()
    foreach ($dirImage in 'B','C') {
        foreach ($dirCase in $dirCases) {
            $dirKey = "$dirImage-$($dirCase.Name)"
            $dirText = if (Test-Path -LiteralPath "$dirSys/$dirKey.txt") {[IO.File]::ReadAllText("$dirSys/$dirKey.txt",[Text.Encoding]::Latin1)} else {$null}
            $dirChecks += @{name=$dirKey; expected=$dirCase.Output; actual=$dirText; passed=$dirCompleted -and $dirText -ceq $dirCase.Output}
        }
    }
    $dirReturns = if (Test-Path -LiteralPath "$dirSys/returns.txt") {[IO.File]::ReadAllText("$dirSys/returns.txt",[Text.Encoding]::Latin1)} else {$null}
    $dirRecord = [ordered]@{
        cpu=$dirCpu; profile=$(if ($MorphOS) {'morphos'} else {'workbench'}); completed=$dirCompleted; stackBytes=4096; jit=$false
        romSha256=(Get-FileHash -LiteralPath $Rom).Hash.ToLowerInvariant()
        winuaeSha256=(Get-FileHash -LiteralPath $WinUAE).Hash.ToLowerInvariant(); winuaeVersion=(Get-Item -LiteralPath $WinUAE).VersionInfo.ProductVersion
        checks=$dirChecks; returns=$dirReturns; expectedReturns=$dirExpectedReturns
        passed=$dirCompleted -and $dirReturns -ceq $dirExpectedReturns -and @($dirChecks | Where-Object {-not $_.passed}).Count -eq 0
    }
    $dirRecord | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath "$dirRoot/result.json" -Encoding utf8
    $dirRecords += $dirRecord
}
$dirRecords | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath "$Stage/winuae/results.json" -Encoding utf8
$dirRecords | ForEach-Object { Write-Output "WinUAE $($_.profile) $($_.cpu): completed=$($_.completed), passed=$($_.passed)" }
if (@($dirRecords | Where-Object {-not $_.passed}).Count) { throw 'Strict Dir WinUAE contracts failed; retain installed image.' }
