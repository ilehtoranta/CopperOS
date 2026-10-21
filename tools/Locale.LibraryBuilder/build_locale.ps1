[CmdletBinding()]
param(
    [ValidateSet('68000', '68020', '68040')][string]$Cpu = '68000',
    [string]$OutputDirectory = ''
)
$ErrorActionPreference = 'Stop'
$localeRepo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (!$OutputDirectory) { $OutputDirectory = Join-Path $localeRepo "artifacts/locale/$Cpu" }
$localeProject = Join-Path $PSScriptRoot 'CopperOS.Locale.LibraryBuilder.csproj'
& dotnet build $localeProject -c Release -p:CopperOSUseLocalCopperSharp=true --nologo
if ($LASTEXITCODE) { throw 'Locale managed build failed.' }
$localeBuilder = Join-Path $PSScriptRoot 'bin/Release/net10.0/CopperOS.Locale.LibraryBuilder.dll'
& dotnet $localeBuilder $Cpu ([IO.Path]::GetFullPath($OutputDirectory))
if ($LASTEXITCODE) { throw 'Locale native compilation or packaging failed.' }
