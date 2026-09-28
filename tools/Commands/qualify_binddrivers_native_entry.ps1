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
    Join-Path $root ('qualification-binddrivers-native-entry\' + [guid]::NewGuid().ToString('N'))
}
New-Item -ItemType Directory -Force -Path $run | Out-Null

& $DotnetPath build $cliProject -c Release --nologo
if ($LASTEXITCODE) { throw 'Compiler build failed.' }
& $DotnetPath build $project -c Release --nologo
if ($LASTEXITCODE) { throw 'BindDrivers native root build failed.' }
& $DotnetPath build $runnerProject -c Release --nologo
if ($LASTEXITCODE) { throw 'Native execution fixture build failed.' }

$support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'CopperSharp Amiga support assembly missing.' }

$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $run "binddrivers-morphos-$cpu.hunk"
    $staticPath = "$hunk.compatibility.json"
    $runtimePath = "$hunk.runtime.json"
    & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSBindDriversEntry::Main' `
        '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
        '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') `
        '--managed-assembly' $support `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') `
        '--output' $hunk '--compatibility-report' $staticPath
    if ($LASTEXITCODE) { throw "BindDrivers native HUNK compilation failed for $cpu." }
    & $DotnetPath $runner $hunk $cpu $runtimePath 'binddrivers-morphos-native-entry-vector-fixture'
    if ($LASTEXITCODE) { throw "BindDrivers native execution fixture failed for $cpu." }
    $static = Get-Content -Raw $staticPath | ConvertFrom-Json
    $runtime = Get-Content -Raw $runtimePath | ConvertFrom-Json
    $native = $static.NativeCompatibility
    if (!$static.IsCompatible -or $static.ReachableMethodCount -lt 15 -or
        $native.RuntimeFeatureCount -ne 0 -or $native.RuntimeHelperCount -ne 0 -or
        $native.ExternalNativeTargetCount -ne 0 -or $native.ExceptionRegionCount -ne 0 -or
        $native.FatalMachineFaultSiteCount -ne 0 -or $runtime.status -ne 'passed' -or
        $runtime.passed -ne 26 -or $runtime.sharedImageWrites -ne 0) {
        throw "BindDrivers qualification receipt incomplete for $cpu."
    }

    $parserHunk = Join-Path $run "binddrivers-product-parser-morphos-$cpu.hunk"
    $parserStaticPath = "$parserHunk.compatibility.json"
    $parserRuntimePath = "$parserHunk.runtime.json"
    & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSBindDriversProductParserEntry::Main' `
        '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
        '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' '--exports' 'none' `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') `
        '--managed-assembly' $support `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') `
        '--output' $parserHunk '--compatibility-report' $parserStaticPath
    if ($LASTEXITCODE) { throw "BindDrivers PRODUCT parser HUNK compilation failed for $cpu." }
    & $DotnetPath $runner $parserHunk $cpu $parserRuntimePath 'binddrivers-morphos-product-parser-native-entry-vector-fixture'
    if ($LASTEXITCODE) { throw "BindDrivers PRODUCT parser fixture failed for $cpu." }
    $parserStatic = Get-Content -Raw $parserStaticPath | ConvertFrom-Json
    $parserRuntime = Get-Content -Raw $parserRuntimePath | ConvertFrom-Json
    $parserNative = $parserStatic.NativeCompatibility
    if (!$parserStatic.IsCompatible -or $parserStatic.RootMethodCount -ne 1 -or
        $parserStatic.ReachableMethodCount -le 0 -or
        @($parserStatic.ManagedAllocationSites).Count -ne 0 -or
        $parserNative.RuntimeFeatureCount -ne 0 -or
        $parserNative.RuntimeHelperCount -ne 0 -or
        $parserNative.ExternalNativeTargetCount -ne 0 -or
        $parserNative.ExceptionRegionCount -ne 0 -or
        $parserNative.FatalMachineFaultSiteCount -ne 0 -or
        $parserRuntime.status -ne 'passed' -or $parserRuntime.passed -ne 1 -or
        $parserRuntime.sharedImageWrites -ne 0 -or $parserRuntime.nativeWrites -le 0) {
        throw "BindDrivers PRODUCT parser qualification receipt incomplete for $cpu."
    }
    $file = Get-Item -LiteralPath $hunk
    $parserFile = Get-Item -LiteralPath $parserHunk
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
        productParserBytes = $parserFile.Length
        productParserSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $parserHunk).Hash.ToLowerInvariant()
        productParserReachableMethods = $parserStatic.ReachableMethodCount
        productParserInvocations = $parserRuntime.passed
        productParserRuntimeReport = [IO.Path]::GetFileName($parserRuntimePath)
        productParserSharedImageWrites = $parserRuntime.sharedImageWrites
        productParserNativeWrites = $parserRuntime.nativeWrites
    }
}

[ordered]@{
    schemaVersion = 1
    suite = 'CC20-BindDrivers-morphos-native-entry'
    status = 'passed'
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSBindDriversEntry::Main'
    scope = 'Three-CPU resident HUNK compilation plus twenty-six public-DOS/Exec/Icon/Expansion scanner invocations and a separate native PRODUCT-parser invocation per CPU. Scanner vectors cover no-match and empty scans, library-open failures, complete one-pair and multiple-pair driver matches, directory entries with APF_DIDDIR clearing and unrelated AnchorPath flags preserved, missing Icon object/PRODUCT/ConfigDev/resident cases, LoadSeg and InitResident failures, NameFromLock/AddPart warning and PrintFault/IoErr behavior, bounded resident-record discovery across a second HUNK, short/truncated/overflowing HUNK extents, invalid BPTR links, cyclic links, repeat ownership, and matcher/ConfigBinding/object/segment/workspace cleanup. The multiple-pair scan verifies both PRODUCT pairs, five ordered FindConfigDev queries, three returned ConfigDev nodes, and their prepended chain. Two successful scanner invocations are also instruction-interleaved with slot-local path, Icon, PRODUCT, ConfigDev, and HUNK fixture memory to check concurrent ownership and shared-image safety. Resident discovery checks guest addresses through Exec.TypeOfMem and rejects unsafe ranges or cyclic links before continuing. The parser fixture checks seven manufacturer/product pairs from five raw PRODUCT strings and nine FindConfigDev calls, covering non-leading equals, signed and whitespace-prefixed atoi values, numeric-prefix parsing, missing slash, multiple pipe-separated pairs, empty input and repeated ConfigDev results prepended to the returned chain. Packed correspondence, guest parity, PURE, package and differential gates remain open.'
    artifacts = $artifacts
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
