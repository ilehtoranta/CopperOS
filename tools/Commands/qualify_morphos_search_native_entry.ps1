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
    Join-Path $repo ('artifacts\search-morphos-native-' + [guid]::NewGuid().ToString('N'))
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
    $hunk = Join-Path $run "search-morphos-native-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    $runtime = "$hunk.runtime.json"
    & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSSearchEntry::Main' `
        '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
        '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') `
        '--managed-assembly' $support `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') `
        '--output' $hunk '--compatibility-report' $static
    if ($LASTEXITCODE) { throw "Native HUNK compilation failed for $cpu." }
    & $DotnetPath $runner $hunk $cpu $runtime 'morphos320-search-native-entry-vector-fixture'
    if ($LASTEXITCODE) { throw "Native fixture failed for $cpu." }
    $s = Get-Content -Raw $static | ConvertFrom-Json
    $r = Get-Content -Raw $runtime | ConvertFrom-Json
    $n = $s.NativeCompatibility
    # The shared test assembly declares one entry per profile; --entry trims
    # the HUNK to the selected profile's reachable implementation.
    # The shared native-root assembly can contain unrelated command/probe
    # entrypoints; --entry above selects this Search root explicitly.
    if (!$s.IsCompatible -or $s.RootMethodCount -lt 1 -or
        @($s.ManagedAllocationSites).Count -ne 0 -or
        $n.RuntimeFeatureCount -ne 0 -or $n.RuntimeHelperCount -ne 0 -or
        $n.ExternalNativeTargetCount -ne 0 -or $n.ExceptionRegionCount -ne 0 -or
        $n.FatalMachineFaultSiteCount -ne 0 -or $r.status -ne 'passed' -or
        $r.passed -ne 46 -or $r.sharedImageWrites -ne 0 -or $r.nativeWrites -le 0) {
        throw "Qualification receipt incomplete for $cpu."
    }
    $file = Get-Item $hunk
    $artifacts += [ordered]@{
        cpu = $cpu; bytes = $file.Length
        sha256 = (Get-FileHash $hunk -Algorithm SHA256).Hash.ToLowerInvariant()
        reachableMethods = $s.ReachableMethodCount
        suppliedVectorInvocations = $r.passed
    }
}

[ordered]@{
    schemaVersion = 1
    suite = 'CC11-Search-morphos320-native-entry'
    status = 'passed'
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSSearchEntry::Main'
    scope = 'Three-CPU resident HUNK compilation plus forty-six supplied DOS/Exec/Locale-vector invocations per CPU for the bounded MorphOS Search parser, locale.library v37/OpenLocale/ConvToUpper/IsCntrl/IsPrint behavior and cleanup, nested and sibling AnchorPath traversal, MorphOS soft-link-to-file and soft-link-to-directory handling, dangling-link ReadLink warnings and FILE/QUIET/QUICK suppression, QUICK full-path/control-code, first-match and no-match output, CTRL-D current-file abandonment during partial reads, 512 KiB MEMF_ANY source buffer sizing and allocation fallback including failed-first-allocation recovery, LF-only maximum-line pre-scan with short reads, overlong-line buffer growth and rewind/reread, DOS 51.28+ Seek64 absolute and signed relative rewinds for files above the 32-bit offset limit including the source-shaped per-file seek-failure policy, per-file Open/Read failures that continue traversal and clear IoErr after successful matching, CTRL-D during pre-scan, locale-classified control-byte line delimiters with tab preserved, locale-printable output bytes, LF-only source line-number increments, correctly marked numbered context lines, current-directory and target-lock cleanup, NameFromLock/AddPart path construction, literal and pattern modes, line output, allocation/read/parser/Ctrl-C/startup guards. Original output, Workbench parity, real files over 2 GiB, PURE/resident lifecycle and package admission remain open.'
    artifacts = $artifacts
} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
