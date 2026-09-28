param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\CopperOS.Commands.AddBuffersNativeRoot.csproj'
$executionProject = Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj'
$root = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\bin\Release\net10.0'
$rootDll = Join-Path $root 'CopperOS.Commands.AddBuffersNativeRoot.dll'
$runner = Join-Path $repo 'tests\Commands.NativeExecution\bin\Release\net10.0\CopperOS.Commands.NativeExecution.dll'
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
$run = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $repo 'artifacts\setkeyboard-morphos-native-entry-20260922-v1'
}
New-Item -ItemType Directory -Force $run | Out-Null

& $DotnetPath build $project -c Release --nologo
if ($LASTEXITCODE) { throw 'Native MorphOS SetKeyboard root build failed.' }
& $DotnetPath build $executionProject -c Release --nologo
if ($LASTEXITCODE) { throw 'Native MorphOS SetKeyboard execution fixture build failed.' }
$support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'CopperSharp Amiga support assembly missing.' }

$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $run "setkeyboard-morphos-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    $execution = "$hunk.execution.json"
    & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSSetKeyboardEntry::Main' `
        '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
        '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') `
        '--managed-assembly' $support `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') `
        '--output' $hunk '--compatibility-report' $static
    if ($LASTEXITCODE) { throw "Native MorphOS SetKeyboard HUNK compilation failed for $cpu." }
    $s = Get-Content -Raw $static | ConvertFrom-Json
    $n = $s.NativeCompatibility
    if (!$s.IsCompatible -or $s.RootMethodCount -ne 2 -or
        $s.ReachableMethodCount -le 0 -or @($s.ManagedAllocationSites).Count -ne 0 -or
        $n.RuntimeFeatureCount -ne 0 -or $n.RuntimeHelperCount -ne 0 -or
        $n.ExternalNativeTargetCount -ne 0 -or $n.ExceptionRegionCount -ne 0 -or
        $n.FatalMachineFaultSiteCount -ne 0) {
        throw "MorphOS SetKeyboard resident compatibility report incomplete for $cpu."
    }
    & $DotnetPath $runner $hunk $cpu $execution 'morphos320-setkeyboard-native-entry-vector-fixture'
    if ($LASTEXITCODE) { throw "Native MorphOS SetKeyboard execution failed for $cpu." }
    $r = Get-Content -Raw $execution | ConvertFrom-Json
    if ($r.status -ne 'passed' -or $r.cpu -ne $cpu -or $r.passed -ne 16 -or
        $r.imageLoads -ne 1 -or $r.sharedImageWrites -ne 0) {
        throw "MorphOS SetKeyboard execution receipt is incomplete for $cpu."
    }
    $file = Get-Item $hunk
    $artifacts += [ordered]@{
        cpu = $cpu
        bytes = $file.Length
        sha256 = (Get-FileHash $hunk -Algorithm SHA256).Hash.ToLowerInvariant()
        reachableMethods = $s.ReachableMethodCount
        passedCases = $r.passed
        sharedImageWrites = $r.sharedImageWrites
    }
}

[ordered]@{
    schemaVersion = 1
    suite = 'CC20-SetKeyboard-morphos320-native-entry'
    status = 'passed'
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSSetKeyboardEntry::Main'
    source = 'MorphOS 3.20 IPrefs SetKeyMap source correspondence; packed C:SetKeyboard template remains unobserved'
    scope = 'Three-CPU resident HUNK compilation and 16 supplied vectors covering resource reuse, KEYMAPS:/MOSSYS:Devs/Keymaps fallback, absolute paths, standard and extended-node publication, duplicate handling, parser/startup/library/load failures, and bounded resident discovery over undersized, overflowing, unmapped and cyclic segment fixtures. Original MorphOS guest behavior, exact packed diagnostics/template, PURE metadata, package and differential gates remain open.'
    artifacts = $artifacts
} | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
