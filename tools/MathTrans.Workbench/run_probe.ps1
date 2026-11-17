[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Library,
    [Parameter(Mandatory)][string]$KickstartRom,
    [Parameter(Mandatory)][string]$Python,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$WinUAE = 'C:\Program Files\WinUAE\winuae64.exe',
    [uint32]$Version = 0,
    [switch]$Edge
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw "Output directory already exists: $output" }
$sys = Join-Path $output 'sys'
New-Item -ItemType Directory -Force -Path (Join-Path $sys 'C'), (Join-Path $sys 'Libs'), (Join-Path $sys 'S'), (Join-Path $sys 'T') | Out-Null
Get-ChildItem -LiteralPath (Join-Path $repo 'out/C') -File | Copy-Item -Destination (Join-Path $sys 'C')
$inputLibrary = [IO.Path]::GetFullPath($Library)
$mountedLibrary = Join-Path $sys 'Libs/mathtrans.library'
$libraryHash = (Get-FileHash -LiteralPath $inputLibrary -Algorithm SHA256).Hash
Copy-Item -LiteralPath $inputLibrary -Destination $mountedLibrary
if ((Get-FileHash -LiteralPath $mountedLibrary -Algorithm SHA256).Hash -ne $libraryHash) { throw 'Probe library copy does not match the supplied library.' }
Copy-Item -LiteralPath (Join-Path (Split-Path $WinUAE) 'Amiga Programs/UAEquit') -Destination (Join-Path $sys 'C/UAEquit')
$generator = if ($Edge) { 'make_edge_probe.py' } else { 'make_probe.py' }
& $Python (Join-Path $PSScriptRoot $generator) (Join-Path $sys 'C/MathTransProbe') --version $Version
if ($LASTEXITCODE) { throw 'Probe generation failed.' }
[IO.File]::WriteAllText((Join-Path $sys 'S/Startup-Sequence'), "FailAt 21`nC:MathTransProbe`nEcho >SYS:result.txt `"RESULT `$RC`"`nC:UAEquit`n", [Text.Encoding]::GetEncoding(28591))
$config = @('use_gui=no', 'headless=true', "kickstart_rom_file=$([IO.Path]::GetFullPath($KickstartRom))",
    'cpu_type=68000', 'cpu_model=68000', 'cpu_compatible=false', 'cpu_speed=max', 'chipset=ecs',
    'chipmem_size=4', 'fastmem_size=0', 'bogomem_size=0', 'nr_floppies=1', 'floppy0type=0',
    'floppy0=', 'sound_output=none', "filesystem2=rw,DH0:Workbench3.1:$sys,1")
$configPath = Join-Path $output 'probe.uae'
[IO.File]::WriteAllLines($configPath, $config, [Text.UTF8Encoding]::new($false))
$process = Start-Process -FilePath $WinUAE -ArgumentList @('-nobootlog', '-f', ('"' + $configPath + '"')) -PassThru -WindowStyle Hidden
if (-not $process.WaitForExit(45000)) { $process.Kill(); throw 'WinUAE probe timed out.' }
$result = Join-Path $sys 'result.txt'
if (-not (Test-Path -LiteralPath $result)) { throw 'WinUAE did not produce a result.' }
$text = [IO.File]::ReadAllText($result).Trim()
if ($text -ne 'RESULT 0') { throw "Mathtrans probe failed: $text" }
if ((Get-FileHash -LiteralPath $inputLibrary -Algorithm SHA256).Hash -ne $libraryHash -or
    (Get-FileHash -LiteralPath $mountedLibrary -Algorithm SHA256).Hash -ne $libraryHash) { throw 'Probe library changed during execution.' }
@{ library = $inputLibrary; sha256 = $libraryHash; romSha256 = (Get-FileHash -LiteralPath $KickstartRom -Algorithm SHA256).Hash;
   version = $Version; edge = [bool]$Edge; result = $text } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'receipt.json') -Encoding utf8
Write-Output "$text ($output)"
