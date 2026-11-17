[CmdletBinding()]
param(
    [ValidateSet('68000', '68020', '68040')][string]$Cpu = '68000',
    [string]$OutputDirectory = '',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '../../../CopperSharp68k')
)
$ErrorActionPreference = 'Stop'
$mathtransRepo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (!$OutputDirectory) { $OutputDirectory = Join-Path $mathtransRepo "artifacts/mathtrans/$Cpu" }
$mathtransProject = Join-Path $PSScriptRoot 'CopperOS.MathTrans.LibraryBuilder.csproj'
& dotnet build $mathtransProject -c Release -p:CopperOSUseLocalCopperSharp=true "-p:CopperSharp68kRoot=$([IO.Path]::GetFullPath($CopperSharpRoot))" --nologo
if ($LASTEXITCODE) { throw 'MathTrans managed build failed.' }
$mathtransBuilder = Join-Path $PSScriptRoot 'bin/Release/net10.0/CopperOS.MathTrans.LibraryBuilder.dll'
& dotnet $mathtransBuilder $Cpu ([IO.Path]::GetFullPath($OutputDirectory))
if ($LASTEXITCODE) { throw 'MathTrans native compilation or packaging failed.' }
