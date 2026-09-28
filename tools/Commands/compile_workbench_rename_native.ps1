param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$rootProject = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\CopperOS.Commands.AddBuffersNativeRoot.csproj'
$root = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\bin\Release\net10.0'
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
$run = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force $run | Out-Null
& $DotnetPath build (Join-Path $CopperSharpRoot 'Compiler.Cli\CopperSharp.Compiler.Cli.csproj') -c Release --nologo
if ($LASTEXITCODE) { throw 'Compiler build failed.' }
& $DotnetPath build $rootProject -c Release --nologo
if ($LASTEXITCODE) { throw 'Root build failed.' }
$support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'Support assembly missing.' }
$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $run "rename-$cpu.hunk"
    $static = Join-Path $run "rename-$cpu.compatibility.json"
    & $DotnetPath $cli (Join-Path $root 'CopperOS.Commands.AddBuffersNativeRoot.dll') --entry 'CopperOS.Commands.AddBuffersNativeRoot.Workbench31RenameEntry::Main' --platform amiga --cpu $cpu --clr always --exceptions yolo --format hunk --runtime resident --memory none --peephole disabled --managed-assembly (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') --managed-assembly $support --managed-assembly (Join-Path $root 'CopperSharp.Compiler.dll') --output $hunk --compatibility-report $static
    if ($LASTEXITCODE) { throw "Compilation failed for $cpu." }
    $receipt = Get-Content -Raw $static | ConvertFrom-Json
    $native = $receipt.NativeCompatibility
    if (!$receipt.IsCompatible -or $native.RuntimeFeatureCount -ne 0 -or $native.RuntimeHelperCount -ne 0 -or $native.ExternalNativeTargetCount -ne 0 -or $native.ExceptionRegionCount -ne 0 -or $native.FatalMachineFaultSiteCount -ne 0) { throw "Static native gate failed for $cpu." }
    $artifacts += [ordered]@{ cpu = $cpu; bytes = (Get-Item $hunk).Length; sha256 = (Get-FileHash $hunk -Algorithm SHA256).Hash.ToLowerInvariant(); reachableMethods = $receipt.ReachableMethodCount }
}
[ordered]@{ status = 'compiled'; scope = 'Workbench Rename three-CPU resident compilation only; runtime, installed flags and full parity remain separate gates.'; artifacts = $artifacts } |
    ConvertTo-Json -Depth 5 | Set-Content (Join-Path $run 'compilation.json') -Encoding utf8
