param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$OutputDirectory,
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
    'CopperOS.Commands.AddBuffersNativeRoot.Workbench31ListEntry::Main'
} else {
    'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSListEntry::Main'
}
$suite = if ($workbench) {
    'workbench31-list-native-entry-vector-fixture'
} else {
    'morphos320-list-native-entry-vector-fixture'
}
$label = if ($workbench) { 'list-wb31-native' } else { 'list-morphos-native' }
$run = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $repo ('artifacts\' + $label + '-' + [guid]::NewGuid().ToString('N'))
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
    $hunk = Join-Path $run "$label-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    $runtime = "$hunk.runtime.json"
    & $DotnetPath $cli $rootDll '--entry' $entry `
        '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
        '--exports' 'none' `
        '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') `
        '--managed-assembly' $support `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') `
        '--output' $hunk '--compatibility-report' $static
    if ($LASTEXITCODE) { throw "Native HUNK compilation failed for $cpu." }
    & $DotnetPath $runner $hunk $cpu $runtime $suite
    if ($LASTEXITCODE) { throw "Native fixture failed for $cpu." }
    $s = Get-Content -Raw $static | ConvertFrom-Json
    $r = Get-Content -Raw $runtime | ConvertFrom-Json
    $n = $s.NativeCompatibility
    if (!$s.IsCompatible -or $s.RootMethodCount -ne 1 -or
        @($s.ManagedAllocationSites).Count -ne 0 -or
        $n.RuntimeFeatureCount -ne 0 -or $n.RuntimeHelperCount -ne 0 -or
        $n.ExternalNativeTargetCount -ne 0 -or $n.ExceptionRegionCount -ne 0 -or
        $n.FatalMachineFaultSiteCount -ne 0 -or $r.status -ne 'passed' -or
        $r.passed -ne $(if ($workbench) { 32 } else { 31 }) -or
        $r.sharedImageWrites -ne 0 -or $r.nativeWrites -le 0) {
        throw "Qualification receipt incomplete for $cpu."
    }
    $file = Get-Item $hunk
    $artifacts += [ordered]@{
        cpu = $cpu
        bytes = $file.Length
        sha256 = (Get-FileHash $hunk -Algorithm SHA256).Hash.ToLowerInvariant()
        reachableMethods = $s.ReachableMethodCount
        suppliedVectorInvocations = $r.passed
    }
}

[ordered]@{
    schemaVersion = 1
    suite = if ($workbench) { 'CC11-List-wb31-native-entry' } else { 'CC11-List-morphos320-native-entry' }
    status = 'passed'
    runtime = 'resident'
    entry = $entry
    scope = if ($workbench) { 'Three-CPU resident HUNK compilation plus thirty-two supplied DOS-vector invocations per CPU for the bounded Workbench 3.1 List parser, flat matcher traversal, pattern and case-insensitive literal-substring filters, inclusive SINCE/UPTO date-only filtering through public DOS StrToDate, public AnchorPath recursive ALL traversal, KEYS-only block-number output, documented size/protection/date/time/comment row fields, DATES default and QUICK name-only precedence, NODATES column suppression without DateToStr calls, comment formatting, invalid-date cleanup, output redirection, quick/block/no-header rendering, allocation/Ctrl-C/startup and missing-DOS guards. Sorting, owner fields, LFORMAT, original guest comparison, PURE/resident lifecycle and package admission remain open.' } else { 'Three-CPU resident HUNK compilation plus thirty-one supplied DOS-vector invocations per CPU for the bounded MorphOS List parser, flat matcher traversal, pattern and case-insensitive literal-substring filters, inclusive SINCE/UPTO date-only filtering through public DOS StrToDate, public AnchorPath recursive ALL traversal, KEYS-only block-number output, documented size/protection/date/time/comment row fields, DATES default and QUICK name-only precedence, NODATES column suppression without DateToStr calls, comment formatting, invalid-date cleanup, output redirection, quick/block/no-header rendering, allocation/Ctrl-C/startup guards. Sorting, owner fields, LFORMAT, original guest comparison, PURE/resident lifecycle and package admission remain open.' }
    artifacts = $artifacts
} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
