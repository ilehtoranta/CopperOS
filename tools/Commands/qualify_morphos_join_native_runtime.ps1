param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = '',
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ([string]::IsNullOrWhiteSpace($CopperSharpRoot)) {
    $CopperSharpRoot = Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'
}
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$rootProject = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\CopperOS.Commands.AddBuffersNativeRoot.csproj'
$runnerProject = Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj'
$root = Join-Path $repo "tests\Commands.AddBuffersNativeRoot\bin\$Configuration\net10.0"
$rootDll = Join-Path $root 'CopperOS.Commands.AddBuffersNativeRoot.dll'
$runner = Join-Path $repo "tests\Commands.NativeExecution\bin\$Configuration\net10.0\CopperOS.Commands.NativeExecution.dll"
$cli = Join-Path $CopperSharpRoot "Compiler.Cli\bin\$Configuration\net10.0\CopperSharp.Compiler.Cli.dll"
$run = [IO.Path]::GetFullPath($OutputDirectory)
if ((Test-Path -LiteralPath $run) -and @(Get-ChildItem -LiteralPath $run -Force).Count -ne 0) {
    throw 'Use a fresh output directory; existing qualification evidence must be preserved.'
}
[void](New-Item -ItemType Directory -Force -Path $run)

& $DotnetPath build $rootProject --configuration $Configuration --nologo --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MorphOS Join native root build failed.' }
& $DotnetPath build $runnerProject --configuration $Configuration --nologo --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MorphOS Join native fixture build failed.' }
if (!(Test-Path -LiteralPath $cli)) { throw "CopperSharp compiler CLI is missing: $cli" }
$support = Get-ChildItem -LiteralPath (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter 'CopperSharp.Sdk.Amiga.Support.dll' |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'CopperSharp Amiga support assembly is missing.' }

$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $run "join-morphos-$cpu.hunk"
    $staticPath = "$hunk.compatibility.json"
    $runtimePath = "$hunk.runtime.json"
    & $DotnetPath $cli $rootDll --entry 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSJoinEntry::Main' --platform amiga --cpu $cpu --clr always --exceptions yolo --format hunk --runtime resident --memory none --peephole disabled --managed-assembly (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') --managed-assembly $support --managed-assembly (Join-Path $root 'CopperSharp.Compiler.dll') --output $hunk --compatibility-report $staticPath
    if ($LASTEXITCODE -ne 0) { throw "Native HUNK compilation failed for $cpu." }
    $receipt = Get-Content -Raw -LiteralPath $staticPath | ConvertFrom-Json
    $native = $receipt.NativeCompatibility
    if (!$receipt.IsCompatible -or $receipt.ReachableMethodCount -le 0 -or
        $native.RuntimeFeatureCount -ne 0 -or $native.RuntimeHelperCount -ne 0 -or
        $native.ExternalNativeTargetCount -ne 0 -or $native.ExceptionRegionCount -ne 0 -or
        $native.FatalMachineFaultSiteCount -ne 0) {
        throw "Static resident gate failed for $cpu."
    }
    $file = Get-Item -LiteralPath $hunk
    $bytes = [IO.File]::ReadAllBytes($hunk)
    $header = if ($bytes.Length -ge 4) {
        $word = ([uint32]$bytes[0] -shl 24) -bor ([uint32]$bytes[1] -shl 16) -bor
            ([uint32]$bytes[2] -shl 8) -bor [uint32]$bytes[3]
        ('0x{0:X8}' -f $word)
    } else { 'missing' }
    if ($header -ne '0x000003F3') { throw "Unexpected HUNK header for ${cpu}: $header" }
    & $DotnetPath $runner $hunk $cpu $runtimePath 'morphos320-join-native-entry-vector-fixture'
    if ($LASTEXITCODE -ne 0) { throw "MorphOS Join supplied-vector fixture failed for $cpu." }
    $runtime = Get-Content -Raw -LiteralPath $runtimePath | ConvertFrom-Json
    if ($runtime.status -ne 'passed' -or $runtime.passed -ne 18 -or
        $runtime.sharedImageWrites -ne 0 -or $runtime.nativeWrites -le 0) {
        throw "MorphOS Join runtime receipt incomplete for $cpu."
    }
    $artifacts += [ordered]@{
        cpu = $cpu
        bytes = $file.Length
        sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $hunk).Hash.ToLowerInvariant()
        reachableMethods = $receipt.ReachableMethodCount
        hunkHeader = $header
        suppliedVectorInvocations = $runtime.passed
        nativeCompatibility = [ordered]@{
            runtimeFeatureCount = $native.RuntimeFeatureCount
            runtimeHelperCount = $native.RuntimeHelperCount
            externalNativeTargetCount = $native.ExternalNativeTargetCount
            exceptionRegionCount = $native.ExceptionRegionCount
            fatalMachineFaultSiteCount = $native.FatalMachineFaultSiteCount
        }
    }
}

[ordered]@{
    schemaVersion = 1
    suite = 'CC13-Join-morphos320-native-candidate-runtime'
    status = 'passed'
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSJoinEntry::Main'
    template = 'FILE/M/A,AS=TO/K/A'
    scope = 'Three-CPU resident HUNK compilation plus eighteen supplied public-DOS invocations per CPU for the MorphOS 3.20 Join frontend. The fixture covers source order, append exactness, read/write/open/workspace-result-buffer allocation/parser/Ctrl-C failures, destination cleanup, startup boundaries and interleaved callers. Exact guest handler behavior, wildcard/no-match policy, original PURE metadata, packaging and differential parity remain open.'
    runtimeFixture = $true
    artifacts = $artifacts
} | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
