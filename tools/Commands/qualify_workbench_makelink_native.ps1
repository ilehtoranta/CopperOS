param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$run = [IO.Path]::GetFullPath($OutputDirectory)
& (Join-Path $PSScriptRoot 'compile_workbench_makelink_native.ps1') -DotnetPath $DotnetPath -CopperSharpRoot $CopperSharpRoot -OutputDirectory $run
& $DotnetPath build (Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj') -c Release --nologo
if ($LASTEXITCODE) { throw 'Runner build failed.' }
$runner = Join-Path $repo 'tests\Commands.NativeExecution\bin\Release\net10.0\CopperOS.Commands.NativeExecution.dll'
foreach ($cpu in '68000','68020','68040') {
    $hunk = Join-Path $run "makelink-$cpu.hunk"
    $runtime = Join-Path $run "makelink-$cpu.runtime.json"
    & $DotnetPath $runner $hunk $cpu $runtime workbench-makelink-native-entry-vector-fixture
    if ($LASTEXITCODE) { throw "Runtime failed for $cpu." }
    $receipt = Get-Content -Raw $runtime | ConvertFrom-Json
    if ($receipt.status -ne 'passed' -or $receipt.passed -ne 20 -or $receipt.sharedImageWrites -ne 0) { throw "Incomplete runtime receipt for $cpu." }
}
[ordered]@{ status = 'passed'; suite = 'CC12-Workbench-MakeLink-native'; invocations = 60;
    scope = 'Three-CPU native execution with supplied DOS vectors, including interleaving and distinct invocation locks. Original DOS, startup parity, installed flags and full profile parity are separate gates.' } |
    ConvertTo-Json | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
