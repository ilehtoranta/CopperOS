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
    Join-Path $repo 'artifacts\adddatatypes-workbench31-native-20260925-v6'
}
New-Item -ItemType Directory -Force $run | Out-Null

& $DotnetPath build $rootProject -c Release --no-restore --nologo
if ($LASTEXITCODE) { throw 'Workbench AddDataTypes native root build failed.' }
& $DotnetPath build $executionProject -c Release --no-restore --nologo
if ($LASTEXITCODE) { throw 'Workbench AddDataTypes execution fixture build failed.' }

$support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'CopperSharp Amiga support assembly missing.' }

$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $run "adddatatypes-workbench31-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    $execution = "$hunk.execution.json"
    & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.AddBuffersNativeRoot.NativeWorkbench31AddDataTypesEntry::Main' `
        '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
        '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') `
        '--managed-assembly' $support `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') `
        '--output' $hunk '--compatibility-report' $static
    if ($LASTEXITCODE) { throw "Workbench AddDataTypes resident HUNK compilation failed for $cpu." }

    $s = Get-Content -Raw $static | ConvertFrom-Json
    $n = $s.NativeCompatibility
    if (!$s.IsCompatible -or $s.RootMethodCount -ne 5 -or
        $s.ReachableMethodCount -lt 44 -or
        @($s.ManagedAllocationSites).Count -ne 0 -or
        $n.RuntimeFeatureCount -ne 0 -or $n.RuntimeHelperCount -ne 0 -or
        $n.ExternalNativeTargetCount -ne 0 -or $n.ExceptionRegionCount -ne 0 -or
        $n.FatalMachineFaultSiteCount -ne 0) {
        throw "Workbench AddDataTypes resident compatibility report incomplete for $cpu."
    }

    & $DotnetPath $runner $hunk $cpu $execution 'workbench31-adddatatypes-native-entry-vector-fixture'
    if ($LASTEXITCODE) { throw "Workbench AddDataTypes execution fixture failed for $cpu." }
    $r = Get-Content -Raw $execution | ConvertFrom-Json
    if ($r.status -ne 'passed' -or $r.cpu -ne $cpu -or $r.passed -ne 15 -or
        $r.imageLoads -ne 1 -or $r.sharedImageWrites -ne 0) {
        throw "Workbench AddDataTypes execution receipt is incomplete for $cpu."
    }

    $file = Get-Item $hunk
    $artifacts += [ordered]@{
        cpu = $cpu
        bytes = $file.Length
        sha256 = (Get-FileHash $hunk -Algorithm SHA256).Hash.ToLowerInvariant()
        rootMethods = $s.RootMethodCount
        reachableMethods = $s.ReachableMethodCount
        passedCases = $r.passed
        sharedImageWrites = $r.sharedImageWrites
    }
}

[ordered]@{
    schemaVersion = 1
    suite = 'CC20-AddDataTypes-workbench31-resident-profile-library-leases-named-list-bootstrap-readargs-and-cli-cleanup'
    status = 'passed'
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.NativeWorkbench31AddDataTypesEntry::Main'
    evidence = 'Static fields and call order captured from hash-bound Workbench 3.1 M10 AddDataTypes HUNK. Named-object user space and tag meanings checked against the public utility.library contract.'
    scope = 'Three-CPU resident HUNK qualification plus fourteen supplied-vector invocations per CPU. Covers classic DOS39/Utility39/Intuition39/IFFParse37/Locale38 open floors and reverse cleanup, optional sys/c.catalog failure, existing-list reuse, ANO_USERSPACE(140) list creation and publication, semaphore initialization, cleanup of an allocated named object with missing user space, the three-result FILES/M,QUIET/S,REFRESH/S ReadArgs template, single and multiple Workbench FILES patterns, DTHD registration, option-slot mapping and REFRESH precedence, REFRESH date comparison and stamp update, the DEVS:DataTypes/#? scan gate, missing/unchanged/empty directory cases, parser failure and owned-result cleanup. This remains a fixture, not original Workbench guest parity, live utility/DOS semantics, PURE/reuse lifecycle, complete classic datatype ABI proof, licensing, or shipping/package approval.'
    commandSourceSha256 = (Get-FileHash (Join-Path $repo 'src\Commands\Native\NativeWorkbench31AddDataTypesCommand.cs') -Algorithm SHA256).Hash.ToLowerInvariant()
    coreSourceSha256 = (Get-FileHash (Join-Path $repo 'src\Commands\Native\NativeMorphOSAddDataTypesCommand.cs') -Algorithm SHA256).Hash.ToLowerInvariant()
    rootAssemblySha256 = (Get-FileHash $rootDll -Algorithm SHA256).Hash.ToLowerInvariant()
    testRunnerSha256 = (Get-FileHash $runner -Algorithm SHA256).Hash.ToLowerInvariant()
    artifacts = $artifacts
} | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
