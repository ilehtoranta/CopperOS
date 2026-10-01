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
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
$run = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $repo ('artifacts\tasklist-morphos-native-' + [guid]::NewGuid().ToString('N'))
}
New-Item -ItemType Directory -Force $run | Out-Null

& $DotnetPath build (Join-Path $CopperSharpRoot 'Compiler.Cli\CopperSharp.Compiler.Cli.csproj') -c Release --nologo
if ($LASTEXITCODE) { throw 'Compiler build failed.' }
& $DotnetPath build $project -c Release --nologo
if ($LASTEXITCODE) { throw 'Root build failed.' }
& $DotnetPath build $runnerProject -c Release --nologo
if ($LASTEXITCODE) { throw 'Runner build failed.' }

$support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'CopperSharp Amiga support assembly missing.' }

$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $run "tasklist-morphos-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    $runtime = "$hunk.runtime.json"
    & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSTaskListEntry::Main' `
        '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
        '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') `
        '--managed-assembly' $support `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') `
        '--output' $hunk '--compatibility-report' $static
    if ($LASTEXITCODE) { throw "Native HUNK compilation failed for $cpu." }
    & $DotnetPath $runner $hunk $cpu $runtime 'morphos320-tasklist-native-entry-vector-fixture'
    if ($LASTEXITCODE) { throw "Native fixture failed for $cpu." }
    $s = Get-Content -Raw $static | ConvertFrom-Json
    $r = Get-Content -Raw $runtime | ConvertFrom-Json
    $n = $s.NativeCompatibility
    # Additional compiler/export/lifecycle roots can enter the graph as the
    # command evolves; require a nonempty analyzed graph and let compatibility,
    # allocation, helper, and runtime checks enforce its actual closure.
    if (!$s.IsCompatible -or $s.RootMethodCount -lt 1 -or
        $s.ReachableMethodCount -le 0 -or @($s.ManagedAllocationSites).Count -ne 0 -or
        $n.RuntimeFeatureCount -ne 0 -or $n.RuntimeHelperCount -ne 0 -or
        $n.ExternalNativeTargetCount -ne 0 -or $n.ExceptionRegionCount -ne 0 -or
        $n.FatalMachineFaultSiteCount -ne 0 -or $r.status -ne 'passed' -or
        $r.passed -ne 54 -or $r.sharedImageWrites -ne 0 -or $r.nativeWrites -le 0) {
        throw "Qualification receipt incomplete for $cpu."
    }
    $file = Get-Item $hunk
    $artifacts += [ordered]@{
        cpu = $cpu
        bytes = $file.Length
        sha256 = (Get-FileHash $hunk -Algorithm SHA256).Hash.ToLowerInvariant()
        rootMethods = $s.RootMethodCount
        reachableMethods = $s.ReachableMethodCount
        suppliedVectorInvocations = $r.passed
    }
}

[ordered]@{
    schemaVersion = 1
    suite = 'CC17-TaskList-morphos320-native-entry'
    status = 'passed'
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSTaskListEntry::Main'
    scope = 'Three-CPU resident HUNK compilation plus fifty-four supplied DOS/Exec vectors per CPU for the source-backed MorphOS TaskList task snapshot with source-shaped growing AllocVec storage under Forbid/Permit, public MorphOS task attributes, six public NewGetSystemAttrsA selectors and source TAG_DONE ordering, stopped-task VERBOSE output and live current-task A7 capture through a compiler intrinsic, PPC register capture and REGDUMP output, bounded STACKTRACE backchain walking with default STACKLEVEL, SegTracker symbol resolution and INTERNAL emulation/module frame classification, REGCHECK classification for Task/Process/ETask/stacks, raw and BADDR process streams and CLI pointers, exact library/device/resource/port/semaphore nodes, library function/base ranges and INTERNAL module/emulation pointers, CLI process classification, command-name display and filtering, maximum-length command names, multi-node ready/wait traversal, a 100-ready-task population that exhausts the 128 KiB initial buffer and verifies a 256 KiB retry, list exclusions, parser, allocation, Ctrl-C, sysdebug.library v0 lease ordering and failed-open startup path. The fixture verifies discarded partial snapshots are freed, foreign-task attributes and SegTracker lookup are protected by the expected synchronization, the running-task stack sample lies within the command stack, and each retry balances Forbid/Permit. Original MorphOS guest comparison, PURE/resident lifecycle and package admission remain open.'
    artifacts = $artifacts
} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
