param([Parameter(Mandatory)][string]$Stage, [string]$Rom='C:/Data/ROM/kickstart-3.1-a500.rom', [string]$WinUAE='C:/Program Files/WinUAE/winuae64.exe', [switch]$Resume, [string[]]$Batches=@('typed','automatic','names','errors1','errors2','signals','repeat'))
$ErrorActionPreference='Stop'
$Stage=(Resolve-Path -LiteralPath $Stage).Path
$records=@()
foreach ($cpu in '68000','68020','68040') {
 foreach ($batch in $Batches) {
  $root=Join-Path $Stage "winuae/$batch-$cpu"
  if (Test-Path -LiteralPath $root) {
   if ($Resume -and (Test-Path -LiteralPath "$root/result.json")) {
    $prior=Get-Content -LiteralPath "$root/result.json" -Raw | ConvertFrom-Json
    if (-not $prior.completed) {throw 'Existing run failed; preserve evidence.'}
    $records+=$prior; continue
   }
   $root=Join-Path $Stage "winuae/retry-$batch-$cpu"
   if (Test-Path -LiteralPath $root) {throw 'Preserve retry evidence.'}
  }
  $sys=Join-Path $root 'sys'
  New-Item -ItemType Directory -Path "$sys/C","$sys/S" -Force | Out-Null
  Copy-Item -LiteralPath "$Stage/guest/Exe2ArcQualification" -Destination "$sys/Exe2ArcQualification"
  Copy-Item -LiteralPath "$Stage/inputs/Exe2Arc.baseline" -Destination "$sys/EXE2ARCB"
  Copy-Item -LiteralPath "$Stage/inputs/Exe2Arc.candidate" -Destination "$sys/EXE2ARCC"
  Copy-Item -LiteralPath "$Stage/runs/$batch-$cpu/CASES.FIX" -Destination "$sys/CASES.FIX"
  Get-ChildItem -LiteralPath "$Stage/inputs" -Filter '*.IN' | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $sys $_.Name)}
  Copy-Item -LiteralPath (Join-Path (Split-Path $WinUAE) 'Amiga Programs/UAEquit') -Destination "$sys/C/UAEquit"
  $script=@('Failat 21','Stack 8192','CD SYS:','SYS:Exe2ArcQualification','Echo "$RC $Result2" >SYS:guest-return.txt','Echo DONE >SYS:done.txt','C:UAEquit')
  [IO.File]::WriteAllText("$sys/S/Startup-Sequence",($script -join "`n")+"`n",[Text.Encoding]::ASCII)
  $config=@('use_gui=no','headless=true',"kickstart_rom_file=$Rom", "cpu_type=$cpu", "cpu_model=$cpu",'fpu_model=0','cpu_compatible=false','cpu_speed=max','cachesize=0','chipset=ecs','chipmem_size=4','fastmem_size=0','bogomem_size=0','nr_floppies=1','floppy0type=0','floppy0=','sound_output=none',"filesystem2=rw,DH0:Exe2ArcQualification:$sys,1")
  [IO.File]::WriteAllLines("$root/run.uae",$config,[Text.UTF8Encoding]::new($false))
  $process=Start-Process -FilePath $WinUAE -ArgumentList @('-nobootlog','-f',"`"$root/run.uae`"") -WindowStyle Hidden -PassThru
  $watch=[Diagnostics.Stopwatch]::StartNew()
  while (-not $process.WaitForExit(1000) -and $watch.Elapsed.TotalSeconds -lt 45) {}
  $exited=$process.HasExited
  if (-not $exited) {Stop-Process -Id $process.Id -Force; $process.WaitForExit(5000) | Out-Null}
  $completed=$exited -and (Test-Path -LiteralPath "$sys/done.txt") -and (Test-Path -LiteralPath "$sys/PAYLOAD.OK")
  $return=if (Test-Path -LiteralPath "$sys/guest-return.txt") {[IO.File]::ReadAllText("$sys/guest-return.txt")} else {$null}
  $record=[ordered]@{batch=$batch;cpu=$cpu;completed=$completed;guestReturn=$return;jit=$false;seconds=$watch.Elapsed.TotalSeconds;romSha256=(Get-FileHash -LiteralPath $Rom).Hash.ToLowerInvariant();winuaeSha256=(Get-FileHash -LiteralPath $WinUAE).Hash.ToLowerInvariant();winuaeVersion=(Get-Item -LiteralPath $WinUAE).VersionInfo.ProductVersion}
  $record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath "$root/result.json" -Encoding utf8
  $records+=$record
  Write-Output "WinUAE $batch $cpu completed=$completed return=$return"
  if (-not $completed) {throw 'Guest failed; retain evidence.'}
 }
}
$records | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath "$Stage/winuae/results.json" -Encoding utf8
