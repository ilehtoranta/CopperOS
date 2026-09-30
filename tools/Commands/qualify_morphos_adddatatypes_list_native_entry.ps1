param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$rootProject = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\CopperOS.Commands.AddBuffersNativeRoot.csproj'
$executionProject = Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj'
$root = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\bin\Release\net10.0'
$rootDll = Join-Path $root 'CopperOS.Commands.AddBuffersNativeRoot.dll'
$runner = Join-Path $repo 'tests\Commands.NativeExecution\bin\Release\net10.0\CopperOS.Commands.NativeExecution.dll'
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
$run = if ($OutputDirectory) {
    [IO.Path]::GetFullPath($OutputDirectory)
} else {
    Join-Path $repo 'artifacts\adddatatypes-morphos-profile-stacks-20260925-v20'
}
New-Item -ItemType Directory -Force $run | Out-Null

& $DotnetPath build $rootProject -c Release --no-restore --nologo
if ($LASTEXITCODE) { throw 'MorphOS AddDataTypes native root build failed.' }
& $DotnetPath build $executionProject -c Release --no-restore --nologo
if ($LASTEXITCODE) { throw 'MorphOS AddDataTypes execution fixture build failed.' }

$support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'CopperSharp Amiga support assembly missing.' }

$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $run "adddatatypes-morphos-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    $execution = "$hunk.execution.json"
    & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSAddDataTypesListEntry::Main' `
        '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
        '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') `
        '--managed-assembly' $support `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') `
        '--output' $hunk '--compatibility-report' $static
    if ($LASTEXITCODE) { throw "MorphOS AddDataTypes resident HUNK compilation failed for $cpu." }
    $s = Get-Content -Raw $static | ConvertFrom-Json
    $n = $s.NativeCompatibility
    if (!$s.IsCompatible -or $s.RootMethodCount -ne 5 -or
        $s.ReachableMethodCount -lt 44 -or
        @($s.ManagedAllocationSites).Count -ne 0 -or
        $n.RuntimeFeatureCount -ne 0 -or $n.RuntimeHelperCount -ne 0 -or
        $n.ExternalNativeTargetCount -ne 0 -or $n.ExceptionRegionCount -ne 0 -or
        $n.FatalMachineFaultSiteCount -ne 0) {
        throw "MorphOS AddDataTypes resident compatibility report incomplete for $cpu."
    }
    & $DotnetPath $runner $hunk $cpu $execution 'morphos320-adddatatypes-files-iff-dtcd-native-entry-vector-fixture'
    if ($LASTEXITCODE) { throw "MorphOS AddDataTypes execution fixture failed for $cpu." }
    $r = Get-Content -Raw $execution | ConvertFrom-Json
    if ($r.status -ne 'passed' -or $r.cpu -ne $cpu -or $r.passed -ne 42 -or
        $r.imageLoads -ne 1 -or $r.sharedImageWrites -ne 0) {
        throw "MorphOS AddDataTypes execution receipt is incomplete for $cpu."
    }

    $callbackHunk = Join-Path $run "adddatatypes-callback-probe-$cpu.hunk"
    $callbackStatic = "$callbackHunk.compatibility.json"
    $callbackExecution = "$callbackHunk.execution.json"
    & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSAddDataTypesCallbackProbeEntry::Main' `
        '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
        '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') `
        '--managed-assembly' $support `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') `
        '--output' $callbackHunk '--compatibility-report' $callbackStatic
    if ($LASTEXITCODE) { throw "MorphOS AddDataTypes callback-probe resident HUNK compilation failed for $cpu." }
    $cs = Get-Content -Raw $callbackStatic | ConvertFrom-Json
    $cn = $cs.NativeCompatibility
    if (!$cs.IsCompatible -or $cs.RootMethodCount -lt 1 -or
        $cs.ReachableMethodCount -lt 1 -or
        @($cs.ManagedAllocationSites).Count -ne 0 -or
        $cn.RuntimeFeatureCount -ne 0 -or $cn.RuntimeHelperCount -ne 0 -or
        $cn.ExternalNativeTargetCount -ne 0 -or $cn.ExceptionRegionCount -ne 0 -or
        $cn.FatalMachineFaultSiteCount -ne 0) {
        throw "MorphOS AddDataTypes callback-probe resident compatibility report incomplete for $cpu."
    }
    & $DotnetPath $runner $callbackHunk $cpu $callbackExecution 'morphos320-adddatatypes-dtcd-callback-wrapper-probe'
    if ($LASTEXITCODE) { throw "MorphOS AddDataTypes callback execution probe failed for $cpu." }
    $cr = Get-Content -Raw $callbackExecution | ConvertFrom-Json
    if ($cr.status -ne 'passed' -or $cr.cpu -ne $cpu -or $cr.passed -ne 1 -or
        $cr.imageLoads -ne 1 -or $cr.sharedImageWrites -ne 0) {
        throw "MorphOS AddDataTypes callback-probe receipt is incomplete for $cpu."
    }
    $file = Get-Item $hunk
    $callbackFile = Get-Item $callbackHunk
    $artifacts += [ordered]@{
        cpu = $cpu
        bytes = $file.Length
        sha256 = (Get-FileHash $hunk -Algorithm SHA256).Hash.ToLowerInvariant()
        rootMethods = $s.RootMethodCount
        reachableMethods = $s.ReachableMethodCount
        passedCases = $r.passed
        sharedImageWrites = $r.sharedImageWrites
        callbackProbeBytes = $callbackFile.Length
        callbackProbeSha256 = (Get-FileHash $callbackHunk -Algorithm SHA256).Hash.ToLowerInvariant()
        callbackProbeRootMethods = $cs.RootMethodCount
        callbackProbeReachableMethods = $cs.ReachableMethodCount
        callbackProbePassedCases = $cr.passed
    }
}

[ordered]@{
    schemaVersion = 1
    suite = 'CC20-AddDataTypes-morphos320-production-cli-workbench-startup-files-iff-readargs-dtcd-list-callbacks-segment-lifetime-and-profile-stacks'
    status = 'passed'
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSAddDataTypesListEntry::Main'
    evidence = 'Public pack-2 Exec/Datatypes layouts plus the released MorphOS command field ordering; private list layout remains subject to original-guest confirmation.'
    scope = 'Three-CPU resident HUNK qualification of the production CLI Run and MorphOS RunWorkbenchStartup methods through fixture DOS vectors. The test root supplies outer DOS startup and Workbench message receive/reply, then delegates to production orchestration. Workbench cases verify startup-message receive/reply order, zero-argument startup, a descriptor WBArg under its lock, duplicated first-lock ownership, per-file CurrentDir switching and restoration, MorphOS-profile DTCD loading during Workbench startup, malformed missing-ArgList rejection, partial utility-library-open cleanup, and descriptor/file cleanup before replying. Production library leases and reverse partial-open cleanup, named-object user-space lookup/release, shared-list semaphore acquisition/release, four built-in descriptor allocations and list insertion/sorting, DOS ReadArgs result-slot mapping and RDArgs/result-array ownership, parser/allocation failure cleanup, MorphOS sorted LIST output and Ctrl-C interruption, initialized private record fields, FILES scanner no-match and matched-file cleanup, first-directory APF_DODIR/APF_DIDDIR handling, and refusal to recurse into a later nested directory while continuing with another matched file are also covered. Synthetic DTYP/DTHD descriptors are copied into invocation-owned resident memory. Malformed DTHD cases verify short-header, out-of-range name offset, unterminated pattern, and out-of-bounds mask rejection. Duplicate DTHD cases verify open-descriptor preservation, case-insensitive same-descriptor ID refresh, and unlink/free/replacement of a changed closed descriptor. New segment-lifetime cases verify an open descriptor retains its existing segment, discarding a newly loaded same/open duplicate unloads only that candidate segment after freeing its copied DTCD source, and replacing a closed descriptor unloads the old segment before freeing the descriptor. DTCD cases verify copied code bytes, per-load state, resident callback addresses, InternalLoadSeg register arguments, profile-specific stack sizes (32768 MorphOS and 4096 Workbench 3.1), segment/function publication, and failed-loader cleanup. REFRESH cases verify lock alias de-duplication, unchanged-date skips, empty-directory scans, date-stamp handling and process window-pointer restoration. A separate resident HUNK probe enters the exported read/allocate/free callbacks through SDK indirect-call wrappers and verifies partial reads, EOF, copying, allocation and release. This is fixture evidence, not original MorphOS guest parity, a real DOS loader callback test, historical AROS_STACKSIZE-to-binary correspondence, authentic segment unload safety, Workbench 3.1 implementation, private ABI closure, PURE/resident admission, licensing or package approval.'
    commandSourceSha256 = (Get-FileHash (Join-Path $repo 'src\Commands\AddDataTypes\Native\NativeMorphOSAddDataTypesCommand.cs') -Algorithm SHA256).Hash.ToLowerInvariant()
    rootAssemblySha256 = (Get-FileHash $rootDll -Algorithm SHA256).Hash.ToLowerInvariant()
    testRunnerSha256 = (Get-FileHash $runner -Algorithm SHA256).Hash.ToLowerInvariant()
    artifacts = $artifacts
} | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
