[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Image,
    [Parameter(Mandatory)][string]$KickstartRom,
    [Parameter(Mandatory)][string]$WorkingImage,
    [Parameter(Mandatory)][string]$Configuration
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$source = (Resolve-Path -LiteralPath $Image).Path
$rom = (Resolve-Path -LiteralPath $KickstartRom).Path
$working = [IO.Path]::GetFullPath($WorkingImage)
$config = [IO.Path]::GetFullPath($Configuration)
if ($source -eq $working -or $source -eq $config -or $working -eq $config -or
    (Test-Path -LiteralPath $working) -or (Test-Path -LiteralPath $config)) {
    throw 'The working image and configuration must be distinct new files.'
}
if ($working.Contains(',') -or $working.Contains('"') -or $rom.Contains('"')) {
    throw 'The chosen paths contain characters not supported by this WinUAE configuration generator.'
}
if ([IO.Path]::GetExtension($working) -ne '.hdf' -or [IO.Path]::GetExtension($config) -ne '.uae') {
    throw 'Use .hdf for the working image and .uae for the configuration.'
}
$file = Get-Item -LiteralPath $source
if ($file.Length -ne 8MB) { throw 'Expected an 8 MiB CopperOS HDF.' }
$stream = [IO.File]::OpenRead($source)
try {
    $header = New-Object byte[] 4
    if ($stream.Read($header, 0, 4) -ne 4 -or [BitConverter]::ToString($header) -ne '44-4F-53-01') {
        throw 'Expected a DOS1 FFS partition image.'
    }
} finally { $stream.Dispose() }
$expected = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($working)), ([IO.Path]::GetDirectoryName($config)) -Force | Out-Null
[IO.File]::Copy($source, $working, $false)
if ((Get-FileHash -LiteralPath $working -Algorithm SHA256).Hash -ne $expected) {
    throw 'Working HDF does not match the source image.'
}
$lines = @(
    'config_description=CopperOS Kickstart 3.1 development HDF'
    'use_gui=no', 'headless=false', "kickstart_rom_file=$rom"
    'cpu_type=68000', 'cpu_model=68000', 'cpu_compatible=false', 'cpu_speed=max', 'cachesize=0'
    'chipset=ecs', 'chipmem_size=4', 'fastmem_size=0', 'bogomem_size=0'
    'nr_floppies=0', 'gfx_fullscreen_amiga=false', 'gfx_width_windowed=800', 'gfx_height_windowed=600'
    "hardfile2=rw,DH0:$working,32,2,2,512,0,,uae0"
)
$configStream = [IO.File]::Open($config, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
try {
    $writer = [IO.StreamWriter]::new($configStream, [Text.UTF8Encoding]::new($false))
    try { foreach ($line in $lines) { $writer.WriteLine($line) } } finally { $writer.Dispose() }
} finally { $configStream.Dispose() }
Write-Output "Created WinUAE configuration $config with working HDF $working (source SHA-256 $expected)."
