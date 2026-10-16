param(
    [Parameter(Mandatory)][string]$Stage,
    [string]$Rom = $env:COPPERSHARP_KICKSTART31_ROM,
    [string]$WinUAE = 'C:\Program Files\WinUAE\winuae64.exe'
)
$ErrorActionPreference = 'Stop'
$Stage = (Resolve-Path -LiteralPath $Stage).Path
$Rom = (Resolve-Path -LiteralPath $Rom).Path
$records = @()
foreach ($cpu in '68000', '68020', '68040') {
    $root = Join-Path $Stage "winuae/$cpu"
    if (Test-Path -LiteralPath $root) { throw 'Preserve the previous WinUAE receipt; use a fresh stage.' }
    $sys = Join-Path $root 'sys'
    New-Item -ItemType Directory -Path "$sys/C", "$sys/S" -Force | Out-Null
    Copy-Item -LiteralPath "$Stage/inputs/List.baseline" -Destination "$sys/C/ListB"
    Copy-Item -LiteralPath "$Stage/inputs/List.candidate" -Destination "$sys/C/ListC"
    Copy-Item -LiteralPath "$Stage/inputs/TEXT.IN" -Destination "$sys/TEXT.IN"
    Copy-Item -LiteralPath (Join-Path (Split-Path $WinUAE) 'Amiga Programs/UAEquit') -Destination "$sys/C/UAEquit"
    $script = @'
Failat 21
Stack 4096
C:ListB SYS:TEXT.IN QUICK <NIL: >SYS:baseline-quick.txt
Echo "baseline-quick $RC $Result2" >SYS:returns.txt
C:ListB SYS:TEXT.IN NODATES NOHEAD <NIL: >SYS:baseline-metadata.txt
Echo "baseline-metadata $RC $Result2" >>SYS:returns.txt
C:ListB SYS:TEXT.IN NODATES NOHEAD TO SYS:baseline-to.txt <NIL: >SYS:baseline-stdout.txt
Echo "baseline-to $RC $Result2" >>SYS:returns.txt
C:ListC SYS:TEXT.IN QUICK <NIL: >SYS:candidate-quick.txt
Echo "candidate-quick $RC $Result2" >>SYS:returns.txt
C:ListC SYS:TEXT.IN NODATES NOHEAD <NIL: >SYS:candidate-metadata.txt
Echo "candidate-metadata $RC $Result2" >>SYS:returns.txt
C:ListC SYS:TEXT.IN NODATES NOHEAD TO SYS:candidate-to.txt <NIL: >SYS:candidate-stdout.txt
Echo "candidate-to $RC $Result2" >>SYS:returns.txt
Echo DONE >SYS:done.txt
C:UAEquit
'@ -replace "`r`n", "`n"
    [IO.File]::WriteAllText("$sys/S/Startup-Sequence", $script + "`n", [Text.Encoding]::ASCII)
    $config = @(
        'config_description=Frozen List redirected-output confirmation'
        'use_gui=no', 'headless=true', "kickstart_rom_file=$Rom"
        "cpu_type=$cpu", "cpu_model=$cpu", 'fpu_model=0', 'cpu_compatible=false'
        'cpu_speed=max', 'cachesize=0', 'chipset=ecs', 'chipmem_size=4'
        'fastmem_size=0', 'bogomem_size=0', 'nr_floppies=1', 'floppy0type=0'
        'floppy0=', 'sound_output=none', "filesystem2=rw,DH0:ListQualification:$sys,1"
    )
    $configPath = Join-Path $root 'list.uae'
    [IO.File]::WriteAllLines($configPath, $config, [Text.UTF8Encoding]::new($false))
    $process = Start-Process -FilePath $WinUAE -ArgumentList @('-nobootlog', '-f', "`"$configPath`"") -PassThru -WindowStyle Hidden
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while (-not $process.WaitForExit(1000) -and $watch.Elapsed.TotalSeconds -lt 45) {}
    $exited = $process.HasExited
    if (-not $exited) { Stop-Process -Id $process.Id -Force; $process.WaitForExit(5000) | Out-Null }
    $completed = $exited -and (Test-Path -LiteralPath "$sys/done.txt")
    $outputs = [ordered]@{}
    foreach ($name in 'baseline-quick','candidate-quick','baseline-metadata','candidate-metadata','baseline-to','candidate-to','baseline-stdout','candidate-stdout','returns') {
        $path = Join-Path $sys "$name.txt"
        $outputs[$name] = if (Test-Path -LiteralPath $path) { [IO.File]::ReadAllText($path, [Text.Encoding]::Latin1) } else { $null }
    }
    $expectedMetadata = 'TEXT.IN'.PadRight(24) + ' ' + '19'.PadLeft(7) + " ----rwed`n"
    $expectedReturns = "baseline-quick 0 0`nbaseline-metadata 0 0`nbaseline-to 0 0`ncandidate-quick 0 0`ncandidate-metadata 0 0`ncandidate-to 0 0`n"
    $correct = $completed -and $outputs['returns'] -ceq $expectedReturns
    foreach ($image in 'baseline','candidate') {
        $correct = $correct -and $outputs["$image-quick"] -ceq "TEXT.IN`n" -and $outputs["$image-metadata"] -ceq $expectedMetadata -and $outputs["$image-to"] -ceq $expectedMetadata -and $outputs["$image-stdout"] -ceq ''
    }
    $record = [ordered]@{cpu=$cpu;completed=$completed;passed=$correct;stackBytes=4096;jit=$false
        filesystem='WinUAE directory filesystem; original Kickstart Shell and DOS'
        romSha256=(Get-FileHash -LiteralPath $Rom).Hash.ToLowerInvariant()
        winuaeSha256=(Get-FileHash -LiteralPath $WinUAE).Hash.ToLowerInvariant()
        winuaeVersion=(Get-Item -LiteralPath $WinUAE).VersionInfo.ProductVersion
        outputs=$outputs;expectedMetadata=$expectedMetadata;expectedReturns=$expectedReturns}
    $record | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath "$root/result.json" -Encoding utf8
    $records += $record
    Write-Output "WinUAE $cpu completed=$completed passed=$correct"
}
$records | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath "$Stage/winuae/results.json" -Encoding utf8
if (@($records | Where-Object { -not $_.passed }).Count) { throw 'Independent List confirmation failed; retained exact output.' }
