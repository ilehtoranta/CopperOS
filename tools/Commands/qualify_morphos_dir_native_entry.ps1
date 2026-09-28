param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [ValidateSet('MorphOS320', 'Workbench31')]
    [string]$Profile = 'MorphOS320'
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
$workbench = $Profile -eq 'Workbench31'
$entry = if ($workbench) {
    'CopperOS.Commands.AddBuffersNativeRoot.Workbench31DirEntry::Main'
} else {
    'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSDirEntry::Main'
}
$suite = if ($workbench) {
    'workbench31-dir-native-entry-vector-fixture'
} else {
    'morphos320-dir-native-entry-vector-fixture'
}
$label = if ($workbench) { 'dir-wb31' } else { 'dir-morphos' }
$run = [IO.Path]::GetFullPath($OutputDirectory)
if ((Test-Path -LiteralPath $run) -and @(Get-ChildItem -LiteralPath $run -Force).Count -ne 0) {
    throw 'Use a fresh output directory; existing qualification evidence must be preserved.'
}
New-Item -ItemType Directory -Force -Path $run | Out-Null

& $DotnetPath build $project -c Release --nologo --no-restore -p:BuildProjectReferences=false
if ($LASTEXITCODE) { throw 'Native command root build failed.' }
& $DotnetPath build $runnerProject -c Release --nologo --no-restore -p:BuildProjectReferences=false
if ($LASTEXITCODE) { throw 'Native fixture runner build failed.' }
if (!(Test-Path -LiteralPath $cli)) { throw 'CopperSharp compiler CLI is missing.' }
$support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'CopperSharp Amiga support assembly missing.' }

$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $run "$label-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    $runtime = "$hunk.runtime.json"
    & $DotnetPath $cli $rootDll --entry $entry --platform amiga --cpu $cpu --clr always --exceptions yolo --format hunk --runtime resident --memory none --peephole disabled --managed-assembly (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') --managed-assembly $support --managed-assembly (Join-Path $root 'CopperSharp.Compiler.dll') --output $hunk --compatibility-report $static
    if ($LASTEXITCODE) { throw "Native HUNK compilation failed for $cpu." }
    & $DotnetPath $runner $hunk $cpu $runtime $suite
    if ($LASTEXITCODE) { throw "Native fixture failed for $cpu." }
    $s = Get-Content -Raw $static | ConvertFrom-Json
    $r = Get-Content -Raw $runtime | ConvertFrom-Json
    $n = $s.NativeCompatibility
    if (!$s.IsCompatible -or $s.RootMethodCount -ne 2 -or $s.ReachableMethodCount -le 0 -or
        @($s.ManagedAllocationSites).Count -ne 0 -or $n.RuntimeFeatureCount -ne 0 -or
        $n.RuntimeHelperCount -ne 0 -or $n.ExternalNativeTargetCount -ne 0 -or
        $n.ExceptionRegionCount -ne 0 -or $n.FatalMachineFaultSiteCount -ne 0 -or
        $r.status -ne 'passed' -or $r.passed -ne $(if ($workbench) { 30 } else { 39 }) -or $r.sharedImageWrites -ne 0 -or
        $r.nativeWrites -le 0) { throw "Qualification receipt incomplete for $cpu." }
    $file = Get-Item -LiteralPath $hunk
    $artifacts += [ordered]@{
        cpu = $cpu
        bytes = $file.Length
        sha256 = (Get-FileHash -LiteralPath $hunk -Algorithm SHA256).Hash.ToLowerInvariant()
        reachableMethods = $s.ReachableMethodCount
        suppliedVectorInvocations = $r.passed
    }
}

[ordered]@{
    schemaVersion = 1
    suite = if ($workbench) { 'CC11-Dir-wb31-native-entry' } else { 'CC11-Dir-morphos320-native-entry' }
    status = 'passed'
    runtime = 'resident'
    entry = $entry
    scope = if ($workbench) { 'Three-CPU resident HUNK compilation plus thirty supplied DOS-vector invocations per CPU for the bounded Workbench 3.1 Dir syntax candidate. The candidate exercises ReadArgs ownership, OPT D/F and ignored-option handling, public Lock/ExAll enumeration and provider-error return levels, dynamic row growth and cleanup under injected low-memory failures, paged ExAll and wildcard matching, break-level return and ERROR_BREAK preservation, directory/file filtering and output, allocation/parser, Workbench startup and missing-DOS guards, ordering, and interleaved invocations. ALL/OPT A, interactive INTER, soft-link behavior, original guest comparison, PURE/resident lifecycle and package admission remain open.' } else { 'Three-CPU resident HUNK compilation plus thirty-nine supplied DOS-vector invocations per CPU for the MorphOS 3.20 Dir source grammar. The candidate exercises ReadArgs ownership, OPT A/D/F/I handling, public Lock/ExAll recursion with CurrentDir-relative child locks, ExAll terminal-error handling and RETURN_ERROR propagation through recursive ALL, dynamically growing directory and pattern row tables over 256 results, paged ExAll, saved IoErr across MatchNext termination and MatchEnd cleanup, wrong-type and generic source-shaped diagnostics, allocation failure after retained names and during 128-row table growth, CTRL-C before listing output and between file pairs, MatchNext break propagation, wildcard ParsePattern soft-link classification, ExAll soft-link Examine resolution, dangling-link ReadLink warnings, dangling non-link lock failure, nested directory-order preservation, sorted file pairing, joined-line output, parser/startup guards, resource cleanup, and interleaved invocations. Interactive INTER, original guest comparison, PURE/resident lifecycle and package admission remain open.' }
    runtimeFixture = $true
    artifacts = $artifacts
} | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
