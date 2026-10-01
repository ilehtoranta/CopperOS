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
    Join-Path $repo ('artifacts\version-morphos-native-' + [guid]::NewGuid().ToString('N'))
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
    $hunk = Join-Path $run "version-morphos-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSVersionEntry::Main' `
        '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
        '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' `
        '--exports' 'none' `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') `
        '--managed-assembly' $support `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') `
        '--output' $hunk '--compatibility-report' $static
    if ($LASTEXITCODE) { throw "Native HUNK compilation failed for $cpu." }
    $s = Get-Content -Raw $static | ConvertFrom-Json
    $n = $s.NativeCompatibility
    if (!$s.IsCompatible -or $s.RootMethodCount -ne 1 -or
        $s.ReachableMethodCount -le 0 -or @($s.ManagedAllocationSites).Count -ne 0 -or
        $n.RuntimeFeatureCount -ne 0 -or $n.RuntimeHelperCount -ne 0 -or
        $n.ExternalNativeTargetCount -ne 0 -or $n.ExceptionRegionCount -ne 0 -or
        $n.FatalMachineFaultSiteCount -ne 0) {
        throw "Resident compatibility report incomplete for $cpu."
    }
    $runtime = "$hunk.runtime.json"
    & $DotnetPath $runner $hunk $cpu $runtime 'morphos320-version-native-entry-vector-fixture'
    if ($LASTEXITCODE) { throw "Native fixture failed for $cpu." }
    $r = Get-Content -Raw $runtime | ConvertFrom-Json
    if ($r.status -ne 'passed' -or $r.passed -ne 94 -or
        $r.sharedImageWrites -ne 0 -or $r.nativeWrites -le 0) {
        throw "Native fixture report incomplete for $cpu."
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
    suite = 'CC17-Version-morphos320-native-entry'
    status = 'passed'
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSVersionEntry::Main'
    scope = 'Three-CPU resident HUNK compilation plus ninety-four supplied DOS/Exec vectors per CPU for the source-backed MorphOS Version system, RES, direct FILE scan and MD5SUM paths, including MorphOS-resident absence, protected case-insensitive version.library LibList lookup, dotted-token preference, copied and parsed library IdString with FULL date/extra formatting, source-authoritative resident IdString parsing, versionless residents, MorphOS RTF_EXTENDED rt_Revision fallback, both resident parse-allocation fallback paths including classic revision -1, version mismatch fallback, partial-output failure when version.library is unavailable or absent from LibList, comparison ordering, source-shaped Ambient ARexx request/result parsing/local variable and cleanup, missing port, malformed result and copy-allocation failure, ambient_path, MOSSYS, and SYS Ambient file providers with public DOS GetVar/OpenRaw/Read/Close, IsFileSystem and LoadSeg resident resolution across linked segments, non-filesystem, load failure and missing-resident fallback, and balanced file/segment/buffer cleanup, direct FILE LoadSeg resident version and MD5SUM fallback, missing-resident handling and MD5 diagnostic suppression when no version exists, MorphOS default and RES basename lookup through DOS FilePart, Exec Resident and case-insensitive Exec LibList and DeviceList lookup with copied node names, numeric version fallback, FULL date/extra formatting and comparison, source-ordered MOSSYS:LIBS/ and LIBS: plus MOSSYS:DEVS/ and DEVS: automatic filename searches with lowercase ELF suffix preference and MD5SUM success output, direct named-file fallback before resident command segments including hit and miss before FindSegment, volume-only GetDeviceProc handler lookup without a Utility dependency, device handler segment lookup, volume task-to-device-node traversal past a nonmatching entry under DOS-list lock, DevProc and lock cleanup, missing DevProc/device/segment fallback to FindSegment and terminal ObjectNotFound error, handler segment with no Resident diagnostic/result, requested-version comparison and MD5-unavailable output, Utility Stricmp ABI, utility.library open failure and balanced lease cleanup, DOS FindSegment lookup across both system lists including internal/disabled shellcmd fallback, in-segment $VER parsing, split-read and MorphOS v0 end-seek tag detection, full-file multi-block MD5, unavailable-digest system/resident output, parser and cleanup boundaries. Original guest comparison, PURE/resident lifecycle and package admission remain open.'
    artifacts = $artifacts
} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
