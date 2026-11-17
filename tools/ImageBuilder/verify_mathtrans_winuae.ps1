[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Image,
    [Parameter(Mandatory)][string]$ImageReceipt,
    [Parameter(Mandatory)][string]$StagedLibrary,
    [Parameter(Mandatory)][string]$KickstartRom,
    [Parameter(Mandatory)][string]$Python,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$WinUAE = 'C:\Program Files\WinUAE\winuae64.exe'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$imagePath = (Resolve-Path -LiteralPath $Image).Path
$receiptPath = (Resolve-Path -LiteralPath $ImageReceipt).Path
$stagedPath = (Resolve-Path -LiteralPath $StagedLibrary).Path
$romPath = (Resolve-Path -LiteralPath $KickstartRom).Path
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'Use a fresh WinUAE proof output directory.' }
$checkText = & $Python (Join-Path $PSScriptRoot 'verify_mathtrans_image.py') --image $imagePath --receipt $receiptPath --staged-library $stagedPath
if ($LASTEXITCODE) { throw 'Boot-proof ADF/staged-library verification failed.' }
$check = $checkText | ConvertFrom-Json
$sys = Join-Path $output 'sys'
New-Item -ItemType Directory -Force -Path (Join-Path $sys 'C'), (Join-Path $sys 'S'), (Join-Path $sys 'T') | Out-Null
Get-ChildItem -LiteralPath (Join-Path $repo 'out/C') -File | Copy-Item -Destination (Join-Path $sys 'C')
Copy-Item -LiteralPath (Join-Path (Split-Path $WinUAE) 'Amiga Programs/UAEquit') -Destination (Join-Path $sys 'C/UAEquit')
$privateImage = Join-Path $output 'CopperOS.adf'
Copy-Item -LiteralPath $imagePath -Destination $privateImage
& $Python (Join-Path $repo 'tools/MathTrans.Workbench/make_probe.py') (Join-Path $sys 'C/MathTransProbe') --version 40 --library-name 'DF1:Libs/mathtrans.library'
if ($LASTEXITCODE) { throw 'Native image probe generation failed.' }
[IO.File]::WriteAllText((Join-Path $sys 'S/Startup-Sequence'), "FailAt 21`nC:MathTransProbe`nEcho >SYS:result.txt `"RESULT `$RC`"`nC:UAEquit`n", [Text.Encoding]::GetEncoding(28591))
$config = @('use_gui=no','headless=true',"kickstart_rom_file=$romPath",'cpu_type=68000','cpu_model=68000',
    'cpu_compatible=false','cpu_speed=max','chipset=ecs','chipmem_size=4','fastmem_size=0','bogomem_size=0',
    'nr_floppies=2','floppy0type=0','floppy0=','floppy1type=0',"floppy1=$privateImage",'floppy1writeprotect=true',
    'sound_output=none',"filesystem2=rw,DH0:Workbench3.1:$sys,10")
$configPath = Join-Path $output 'probe.uae'
[IO.File]::WriteAllLines($configPath, $config, [Text.UTF8Encoding]::new($false))
$process = Start-Process -FilePath $WinUAE -ArgumentList @('-nobootlog','-f',('"' + $configPath + '"')) -PassThru -WindowStyle Hidden
if (-not $process.WaitForExit(45000)) { $process.Kill(); throw 'WinUAE image probe timed out.' }
$resultPath = Join-Path $sys 'result.txt'
if (-not (Test-Path -LiteralPath $resultPath)) { throw 'WinUAE image probe did not produce a result.' }
$result = [IO.File]::ReadAllText($resultPath).Trim()
if ($result -ne 'RESULT 0') { throw "WinUAE image probe failed: $result" }
foreach ($path in @($imagePath, $privateImage)) {
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -cne $check.imageSha256) { throw 'Proof ADF changed during WinUAE verification.' }
}
if ((Get-FileHash -LiteralPath $stagedPath -Algorithm SHA256).Hash.ToLowerInvariant() -cne $check.librarySha256) { throw 'Staged library changed during verification.' }
@{ imageSha256=$check.imageSha256; librarySha256=$check.librarySha256; romSha256=(Get-FileHash -LiteralPath $romPath -Algorithm SHA256).Hash.ToLowerInvariant();
   imageVolume=$check.volume; libraryPath='DF1:Libs/mathtrans.library'; minimumVersion=40; result=$result;
   scope='Kickstart 3.1 loads the library directly from the boot-proof ADF mounted in DF1; host directory boots DH0.'
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'receipt.json') -Encoding utf8
Write-Output "${result}: library loaded from boot-proof DF1 ($output)"
