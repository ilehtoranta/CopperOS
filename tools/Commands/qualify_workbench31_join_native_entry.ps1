param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$rootProject = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\CopperOS.Commands.AddBuffersNativeRoot.csproj'
$root = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\bin\Release\net10.0'
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
$run = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $root ('qualification-workbench31-join-native-entry\' + [guid]::NewGuid().ToString('N'))
}
New-Item -ItemType Directory -Force -Path $run | Out-Null

& $DotnetPath build (Join-Path $CopperSharpRoot 'Compiler.Cli\CopperSharp.Compiler.Cli.csproj') -c Release --nologo
if ($LASTEXITCODE) { throw 'Compiler build failed.' }
& $DotnetPath build $rootProject -c Release --nologo
if ($LASTEXITCODE) { throw 'Root build failed.' }

$support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'Support assembly missing.' }

$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $run "join-wb31-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    & $DotnetPath $cli (Join-Path $root 'CopperOS.Commands.AddBuffersNativeRoot.dll') '--entry' 'CopperOS.Commands.AddBuffersNativeRoot.Workbench31JoinEntry::Main' '--platform' amiga '--cpu' $cpu '--clr' always '--exceptions' yolo '--format' hunk '--runtime' resident '--memory' none '--peephole' disabled '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') '--managed-assembly' $support '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') '--output' $hunk '--compatibility-report' $static
    if ($LASTEXITCODE) { throw "Compile failed for $cpu." }
    $receipt = Get-Content -Raw $static | ConvertFrom-Json
    $native = $receipt.NativeCompatibility
    if (!$receipt.IsCompatible -or $receipt.ReachableMethodCount -ne 19 -or
        $native.RuntimeFeatureCount -ne 0 -or $native.RuntimeHelperCount -ne 0 -or
        $native.ExternalNativeTargetCount -ne 0 -or $native.ExceptionRegionCount -ne 0 -or
        $native.FatalMachineFaultSiteCount -ne 0) {
        throw "Static receipt incomplete for $cpu (methods=$($receipt.ReachableMethodCount))."
    }
    $file = Get-Item $hunk
    $artifacts += [ordered]@{
        cpu = $cpu
        bytes = $file.Length
        sha256 = (Get-FileHash $hunk -Algorithm SHA256).Hash.ToLowerInvariant()
        reachableMethods = $receipt.ReachableMethodCount
        runtimeFeatureCount = $native.RuntimeFeatureCount
        runtimeHelperCount = $native.RuntimeHelperCount
        externalNativeTargetCount = $native.ExternalNativeTargetCount
        exceptionRegionCount = $native.ExceptionRegionCount
        fatalMachineFaultSiteCount = $native.FatalMachineFaultSiteCount
    }
}

[ordered]@{
    schemaVersion = 1
    suite = 'CC13-Join-wb31-native-entry-candidate'
    status = 'compiled'
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.Workbench31JoinEntry::Main'
    scope = 'Static three-CPU resident compilation for the Workbench 3.1 Join syntax candidate. The entry uses DOS 36 and the observed FILE/M/A,AS=TO/K/A ReadArgs boundary while sharing the bounded public-DOS Join body. Full DOS runtime vectors, exact diagnostics/no-match policy, real handlers, PURE/resident lifecycle, packaging, packed correspondence and differential parity remain open.'
    runtimeFixture = $false
    artifacts = $artifacts
} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8

Write-Host "PASS static Workbench Join candidate: $($artifacts.Count) CPU artifacts; runtime fixture not yet claimed."
