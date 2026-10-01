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
    Join-Path $repo 'artifacts\setfont-wb31-native-entry-20260922-v2'
}
if ((Test-Path -LiteralPath $run) -and @(Get-ChildItem -LiteralPath $run -Force).Count -ne 0) {
    throw 'Use a fresh output directory; existing qualification evidence must be preserved.'
}
New-Item -ItemType Directory -Force $run | Out-Null

& $DotnetPath build $project -c Release --nologo --no-restore
if ($LASTEXITCODE) { throw 'Native command root build failed.' }
& $DotnetPath build $runnerProject -c Release --nologo --no-restore
if ($LASTEXITCODE) { throw 'Native fixture runner build failed.' }
if (!(Test-Path -LiteralPath $cli)) { throw 'CopperSharp compiler CLI is missing.' }
$support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'CopperSharp Amiga support assembly missing.' }

$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $run "setfont-wb31-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    $runtime = Join-Path $run "setfont-wb31-$cpu.runtime.json"
    & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.AddBuffersNativeRoot.Workbench31SetFontEntry::Main' `
        '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
        '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') `
        '--managed-assembly' $support `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') `
        '--output' $hunk '--compatibility-report' $static
    if ($LASTEXITCODE) { throw "Native HUNK compilation failed for $cpu." }
    & $DotnetPath $runner $hunk $cpu $runtime 'workbench31-setfont-native-entry-vector-fixture'
    if ($LASTEXITCODE) { throw "Native fixture failed for $cpu." }
    $s = Get-Content -Raw $static | ConvertFrom-Json
    $r = Get-Content -Raw $runtime | ConvertFrom-Json
    $n = $s.NativeCompatibility
    if (!$s.IsCompatible -or $s.RootMethodCount -ne 5 -or
        $s.ReachableMethodCount -ne 18 -or @($s.ManagedAllocationSites).Count -ne 0 -or
        $n.RuntimeFeatureCount -ne 0 -or $n.RuntimeHelperCount -ne 0 -or
        $n.ExternalNativeTargetCount -ne 0 -or $n.ExceptionRegionCount -ne 0 -or
        $n.FatalMachineFaultSiteCount -ne 0 -or $r.status -ne 'passed' -or
        $r.passed -ne 33 -or $r.sharedImageWrites -ne 0 -or $r.nativeWrites -le 0) {
        throw "Qualification receipt incomplete for $cpu."
    }
    $file = Get-Item -LiteralPath $hunk
    $artifacts += [ordered]@{
        cpu = $cpu
        bytes = $file.Length
        sha256 = (Get-FileHash -LiteralPath $hunk -Algorithm SHA256).Hash.ToLowerInvariant()
        reachableMethods = $s.ReachableMethodCount
        suppliedVectorInvocations = $r.passed
        sharedImageWrites = $r.sharedImageWrites
        nativeWrites = $r.nativeWrites
        managedAllocationSites = @($s.ManagedAllocationSites).Count
        staticRuntimeFeatureCount = $n.RuntimeFeatureCount
        staticRuntimeHelperCount = $n.RuntimeHelperCount
        staticExternalNativeTargetCount = $n.ExternalNativeTargetCount
        staticExceptionRegionCount = $n.ExceptionRegionCount
        staticFatalMachineFaultSiteCount = $n.FatalMachineFaultSiteCount
    }
}

[ordered]@{
    schemaVersion = 1
    suite = 'CC20-SetFont-wb31-native-entry-candidate'
    status = 'passed'
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.Workbench31SetFontEntry::Main'
    sourceBinary = [ordered]@{
        path = 'D:/TestData/TestImages/Workbench v3.1 rev 40.42 (1994)(Commodore)(M10)(Disk 2 of 6)(Workbench)[!].zip::C/SetFont'
        version = 'setfont 39.1 (2.6.92)'
        sha256 = 'c3e1e763b12fd4f710a414443facea49479aa723a94828828dbaee214fe4ad8c'
    }
    scope = 'Three-CPU resident HUNK compilation plus thirty-three supplied DOS/Exec/graphics/diskfont/console vectors per CPU. The fixture covers parser/result ownership, each result/name/TextAttr/InfoData allocation failure and cleanup, version-37 library floors and failure cleanup, TextAttr name/size/style/flags including low-word SIZE/N truncation and boundary rejection, exact SCALE and PROP flag interactions, proportional-font rejection unless PROP is selected, ACTION_DISK_INFO InfoData BPTR/window extraction and absent-window behavior, Forbid/Permit protection of Window.RastPort/font replacement, tf_Style-gated escape output, ignored output failures, startup and missing-DOS guards, and interleaved callers. Exact original guest handler/result behavior, PURE/resident lifecycle, installed placement, package and differential gates remain open.'
    runtimeFixture = $true
    artifacts = $artifacts
} | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
