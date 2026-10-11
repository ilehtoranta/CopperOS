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
    Copy-Item -LiteralPath "$Stage/inputs/Search.baseline" -Destination "$sys/C/SearchB"
    Copy-Item -LiteralPath "$Stage/inputs/Search.candidate" -Destination "$sys/C/SearchC"
    Copy-Item -LiteralPath "$Stage/inputs/TEXT.IN" -Destination "$sys/TEXT.IN"
    Copy-Item -LiteralPath (Join-Path (Split-Path $WinUAE) 'Amiga Programs/UAEquit') -Destination "$sys/C/UAEquit"
    $script = @'
Failat 21
Stack 4096
C:SearchB SYS:TEXT.IN needle <NIL: >SYS:baseline.txt
Echo "baseline $RC $Result2" >SYS:returns.txt
C:SearchC SYS:TEXT.IN needle <NIL: >SYS:candidate.txt
Echo "candidate $RC $Result2" >>SYS:returns.txt
Echo DONE >SYS:done.txt
C:UAEquit
'@ -replace "`r`n", "`n"
    [IO.File]::WriteAllText("$sys/S/Startup-Sequence", $script + "`n", [Text.Encoding]::ASCII)
    $config = @(
        'config_description=Frozen Search redirected-output confirmation'
        'use_gui=no', 'headless=true', "kickstart_rom_file=$Rom"
        "cpu_type=$cpu", "cpu_model=$cpu", 'fpu_model=0', 'cpu_compatible=false'
        'cpu_speed=max', 'cachesize=0', 'chipset=ecs', 'chipmem_size=4'
        'fastmem_size=0', 'bogomem_size=0', 'nr_floppies=1', 'floppy0type=0'
        'floppy0=', 'sound_output=none', "filesystem2=rw,DH0:SearchQualification:$sys,1"
    )
    $configPath = Join-Path $root 'search.uae'
    [IO.File]::WriteAllLines($configPath, $config, [Text.UTF8Encoding]::new($false))
    $process = Start-Process -FilePath $WinUAE -ArgumentList @('-nobootlog', '-f', "`"$configPath`"") -PassThru -WindowStyle Hidden
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while (-not $process.WaitForExit(1000) -and $watch.Elapsed.TotalSeconds -lt 45) {}
    $exited = $process.HasExited
    if (-not $exited) { Stop-Process -Id $process.Id -Force; $process.WaitForExit(5000) | Out-Null }
    $completed = $exited -and (Test-Path -LiteralPath "$sys/done.txt")
    $baseline = if (Test-Path -LiteralPath "$sys/baseline.txt") { [IO.File]::ReadAllText("$sys/baseline.txt", [Text.Encoding]::Latin1) } else { $null }
    $candidate = if (Test-Path -LiteralPath "$sys/candidate.txt") { [IO.File]::ReadAllText("$sys/candidate.txt", [Text.Encoding]::Latin1) } else { $null }
    $record = [ordered]@{
        cpu = $cpu; completed = $completed; stackBytes = 4096; jit = $false
        filesystem = 'WinUAE directory filesystem; native Kickstart shell and DOS'
        romSha256 = (Get-FileHash -LiteralPath $Rom).Hash.ToLowerInvariant()
        winuaeSha256 = (Get-FileHash -LiteralPath $WinUAE).Hash.ToLowerInvariant()
        winuaeVersion = (Get-Item -LiteralPath $WinUAE).VersionInfo.ProductVersion
        baseline = $baseline; candidate = $candidate
        outputsAgree = $completed -and $baseline -ceq $candidate
        numberedOutputCorrect = $completed -and $candidate -ceq "     2> Needle`n"
        returns = $(if (Test-Path -LiteralPath "$sys/returns.txt") { [IO.File]::ReadAllText("$sys/returns.txt") } else { $null })
    }
    $record | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath "$root/result.json" -Encoding utf8
    $records += $record
}
$records | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath "$Stage/winuae/results.json" -Encoding utf8
$records | ForEach-Object { Write-Output "WinUAE $($_.cpu): completed=$($_.completed), match=$($_.outputsAgree), numbered-output-correct=$($_.numberedOutputCorrect)" }
if (@($records | Where-Object { -not $_.completed -or -not $_.outputsAgree }).Count) { throw 'WinUAE confirmation did not complete.' }
