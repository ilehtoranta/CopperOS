param(
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [string]$DotnetPath = 'dotnet',
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$rootProject = Join-Path $repo 'tests\Commands.WhichNativeRoot\CopperOS.Commands.WhichNativeRoot.csproj'
$executionProject = Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj'
$rootOutput = Join-Path $repo "tests\Commands.WhichNativeRoot\bin\$Configuration\net10.0"
$assembly = Join-Path $rootOutput 'CopperOS.Commands.WhichNativeRoot.dll'
$sdk = Join-Path $rootOutput 'CopperSharp.Sdk.Amiga.dll'
$compiler = Join-Path $rootOutput 'CopperSharp.Compiler.dll'
$runner = Join-Path $repo "tests\Commands.NativeExecution\bin\$Configuration\net10.0\CopperOS.Commands.NativeExecution.dll"
$cli = Join-Path $CopperSharpRoot "Compiler.Cli\bin\$Configuration\net10.0\CopperSharp.Compiler.Cli.dll"
$runRoot = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $repo ('artifacts\which-morphos-native-' + [guid]::NewGuid().ToString('N'))
}
New-Item -ItemType Directory -Force $runRoot | Out-Null

& $DotnetPath build (Join-Path $CopperSharpRoot 'Compiler.Cli\CopperSharp.Compiler.Cli.csproj') -c $Configuration --nologo
if ($LASTEXITCODE) { throw 'Compiler build failed.' }
& $DotnetPath build $rootProject -c $Configuration --nologo
if ($LASTEXITCODE) { throw 'Native Which root build failed.' }
& $DotnetPath build $executionProject -c $Configuration --nologo
if ($LASTEXITCODE) { throw 'Native Which execution fixture build failed.' }

$support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'CopperSharp Amiga support assembly missing.' }

$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $runRoot "which-morphos-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    $runtime = "$hunk.runtime.json"
    & $DotnetPath $cli $assembly '--entry' 'CopperOS.Commands.WhichNativeRoot.MorphOS320WhichEntry::Main' `
        '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
        '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' `
        '--managed-assembly' $sdk '--managed-assembly' $support '--managed-assembly' $compiler `
        '--output' $hunk '--compatibility-report' $static
    if ($LASTEXITCODE) { throw "Native Which HUNK compilation failed for $cpu." }
    & $DotnetPath $runner $hunk $cpu $runtime 'which-morphos-native-entry-vector-fixture'
    if ($LASTEXITCODE) { throw "Native Which fixture failed for $cpu." }
    $s = Get-Content -Raw $static | ConvertFrom-Json
    $r = Get-Content -Raw $runtime | ConvertFrom-Json
    $n = $s.NativeCompatibility
    if (!$s.IsCompatible -or $s.RootMethodCount -ne 1 -or
        $s.ReachableMethodCount -le 0 -or @($s.ManagedAllocationSites).Count -ne 0 -or
        $n.ExceptionRegionCount -ne 0 -or $n.FatalMachineFaultSiteCount -ne 0 -or
        $n.RuntimeHelperCount -ne 0 -or $n.ExternalNativeTargetCount -ne 0 -or
        $r.status -ne 'passed' -or $r.passed -ne 85 -or
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
    suite = 'CC10-Which-morphos320-native-entry'
    status = 'passed'
    runtime = 'resident'
    entry = 'CopperOS.Commands.WhichNativeRoot.MorphOS320WhichEntry::Main'
    scope = 'Three-CPU resident HUNK compilation plus eighty-five supplied DOS/Exec vectors per CPU for the MorphOS Which extended parser, public FindVar alias lookup, ordinary resident/path lookup, all 32 documented switch combinations with candidate found/missing fixtures, alias-only no-fallthrough, duplicate CLI path ordering, cleanup and interleaving paths. The option matrix exercises candidate assumptions only. Alias output/order parity, exact shipped grammar, original guest comparison, PURE/resident lifecycle, packaging and differential comparison remain open.'
    artifacts = $artifacts
} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $runRoot 'qualification.json') -Encoding utf8
Write-Output (Join-Path $runRoot 'qualification.json')
