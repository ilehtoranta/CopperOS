[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Image,
    [Parameter(Mandatory)][string]$KickstartRom,
    [Parameter(Mandatory)][string]$WinUAE,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [ValidateRange(1, 60)][int]$DurationSeconds = 20
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$imagePath = (Resolve-Path -LiteralPath $Image).Path
$romPath = (Resolve-Path -LiteralPath $KickstartRom).Path
$winuaePath = (Resolve-Path -LiteralPath $WinUAE).Path
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'Choose a fresh output directory.' }
New-Item -ItemType Directory -Path $output | Out-Null
$copy = Join-Path $output 'candidate.hdf'
Copy-Item -LiteralPath $imagePath -Destination $copy
$configuration = Join-Path $output 'candidate.uae'
$config = @(
    'config_description=CopperOS unqualified Kickstart candidate'
    'use_gui=no', 'headless=true', "kickstart_rom_file=$romPath"
    'cpu_type=68000', 'cpu_model=68000', 'cpu_compatible=false', 'cpu_speed=max'
    'chipset=ecs', 'chipmem_size=4', 'fastmem_size=0', 'bogomem_size=0'
    'nr_floppies=0', 'sound_output=none'
    "hardfile2=rw,DH0:$copy,32,2,2,512,0,,uae0"
)
[IO.File]::WriteAllLines($configuration, $config, [Text.UTF8Encoding]::new($false))
$process = Start-Process -FilePath $winuaePath -ArgumentList @('-nobootlog', '-f', ('"' + $configuration + '"')) -PassThru -WindowStyle Hidden
$exited = $process.WaitForExit($DurationSeconds * 1000)
if (-not $exited) { $process.Kill(); $process.WaitForExit() }
$result = @{
    kind = 'kickstart31-candidate-launch'; runtimeQualified = $false
    qualification = 'No interactive prompt, command execution or persistence observation was made.'
    sourceImageSha256 = (Get-FileHash -LiteralPath $imagePath -Algorithm SHA256).Hash.ToLowerInvariant()
    imageCopySha256 = (Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash.ToLowerInvariant()
    romSha256 = (Get-FileHash -LiteralPath $romPath -Algorithm SHA256).Hash.ToLowerInvariant()
    winuaeSha256 = (Get-FileHash -LiteralPath $winuaePath -Algorithm SHA256).Hash.ToLowerInvariant()
    winuaeVersion = (Get-Item -LiteralPath $winuaePath).VersionInfo.FileVersion
    configuration = $config; durationSeconds = $DurationSeconds; exitedBeforeTimeout = $exited
    exitCode = $process.ExitCode
}
$result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'launch.json') -Encoding utf8
Write-Output 'Candidate launch completed; no boot or runtime qualification is claimed.'
