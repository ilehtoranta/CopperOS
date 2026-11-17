# Compatibility entry point for development proof builds. Production staging
# and proofs compile the same shared Shell through the same native builder.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '../../../CopperSharp68k'),
    [string]$MathTransLibrary = (Join-Path $PSScriptRoot '../../filesystem/SYS/Libs/mathtrans.library'),
    [string]$MathTransReceipt = (Join-Path $PSScriptRoot '../../artifacts/mathtrans/68000/mathtrans.library.json'),
    [string]$Python = 'python',
    [switch]$WithoutMathTrans
)
$ErrorActionPreference = 'Stop'
$arguments = @{} + $PSBoundParameters
if (-not $arguments.ContainsKey('MathTransReceipt')) {
    $systemBuild = Join-Path $PSScriptRoot '../../artifacts/system/68000/system-build.json'
    if (Test-Path -LiteralPath $systemBuild) {
        $record = Get-Content -LiteralPath $systemBuild -Raw | ConvertFrom-Json
        $manifest = Join-Path (Split-Path $systemBuild) $record.nativeManifest
        $arguments.MathTransReceipt = Join-Path (Split-Path $manifest) 'mathtrans.library.json'
    }
}
& (Join-Path $PSScriptRoot 'build_system_native.ps1') @arguments -BuildKind proof
