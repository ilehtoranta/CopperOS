param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\CopperOS.Commands.AddBuffersNativeRoot.csproj'
$runnerProject = Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj'
$root = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\bin\Release\net10.0'
$rootDll = Join-Path $root 'CopperOS.Commands.AddBuffersNativeRoot.dll'
$runner = Join-Path $repo 'tests\Commands.NativeExecution\bin\Release\net10.0\CopperOS.Commands.NativeExecution.dll'
$cliProject = Join-Path $CopperSharpRoot 'Compiler.Cli\CopperSharp.Compiler.Cli.csproj'
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
$run = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $root ('qualification-loadmondrvs-morphos-native-entry\' + [guid]::NewGuid().ToString('N'))
}
New-Item -ItemType Directory -Force -Path $run | Out-Null

& $DotnetPath build $cliProject -c Release --nologo
if ($LASTEXITCODE) { throw 'Compiler build failed.' }
& $DotnetPath build $project -c Release --nologo
if ($LASTEXITCODE) { throw 'LoadMonDrvs native root build failed.' }
& $DotnetPath build $runnerProject -c Release --nologo
if ($LASTEXITCODE) { throw 'Native execution fixture build failed.' }

$support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'CopperSharp Amiga support assembly missing.' }

$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $run "loadmondrvs-morphos-$cpu.hunk"
    $staticPath = "$hunk.compatibility.json"
    $runtimePath = "$hunk.runtime.json"
    & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSLoadMonDrvsEntry::Main' `
        '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
        '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') `
        '--managed-assembly' $support `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') `
        '--output' $hunk '--compatibility-report' $staticPath
    if ($LASTEXITCODE) { throw "LoadMonDrvs native HUNK compilation failed for $cpu." }
    & $DotnetPath $runner $hunk $cpu $runtimePath 'morphos320-loadmondrvs-native-entry-vector-fixture'
    if ($LASTEXITCODE) { throw "LoadMonDrvs native execution fixture failed for $cpu." }
    $static = Get-Content -Raw $staticPath | ConvertFrom-Json
    $runtime = Get-Content -Raw $runtimePath | ConvertFrom-Json
    $native = $static.NativeCompatibility
    if (!$static.IsCompatible -or $static.ReachableMethodCount -ne 22 -or
        $native.RuntimeFeatureCount -ne 0 -or $native.RuntimeHelperCount -ne 0 -or
        $native.ExternalNativeTargetCount -ne 0 -or $native.ExceptionRegionCount -ne 0 -or
        $native.FatalMachineFaultSiteCount -ne 0 -or $runtime.status -ne 'passed' -or
        $runtime.passed -ne 17 -or $runtime.sharedImageWrites -ne 0) {
        throw "LoadMonDrvs qualification receipt incomplete for $cpu."
    }
    $file = Get-Item -LiteralPath $hunk
    $artifacts += [ordered]@{
        cpu = $cpu
        bytes = $file.Length
        sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $hunk).Hash.ToLowerInvariant()
        reachableMethods = $static.ReachableMethodCount
        staticRuntimeFeatureCount = $native.RuntimeFeatureCount
        staticRuntimeHelperCount = $native.RuntimeHelperCount
        staticExternalNativeTargetCount = $native.ExternalNativeTargetCount
        staticExceptionRegionCount = $native.ExceptionRegionCount
        staticFatalMachineFaultSiteCount = $native.FatalMachineFaultSiteCount
        suppliedVectorInvocations = $runtime.passed
        runtimeReport = [IO.Path]::GetFileName($runtimePath)
        sharedImageWrites = $runtime.sharedImageWrites
        nativeWrites = $runtime.nativeWrites
    }
}

[ordered]@{
    schemaVersion = 1
    suite = 'CC20-LoadMonDrvs-morphos-native-entry'
    status = 'passed'
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSLoadMonDrvsEntry::Main'
    scope = 'Three-CPU resident HUNK compilation plus seventeen supplied DOS/Exec boundary invocations per CPU. Covers default and FROM/EXCEPT ReadArgs ownership, case-insensitive exclusion and nonmatching EXCEPT behavior, an excluded first match followed by a loaded second driver, directory-entry skipping, parser failure, missing DOS, matcher cleanup, alternate directory paths, driver initialization and rollback, later-HUNK resident discovery, undersized/overflowing/unmapped HUNK extents, cyclic segment-list rejection, repeat/interleaved execution, TypeOfMem validation, and zero shared image writes. Provider-backed monitor behavior, packed correspondence, guest parity, PURE/package admission, and differential gates remain open.'
    artifacts = $artifacts
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
