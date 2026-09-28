param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$rootProject = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\CopperOS.Commands.AddBuffersNativeRoot.csproj'
$root = Join-Path $repo "tests\Commands.AddBuffersNativeRoot\bin\$Configuration\net10.0"
$assembly = Join-Path $root 'CopperOS.Commands.AddBuffersNativeRoot.dll'
$runnerProject = Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj'
$runner = Join-Path $repo "tests\Commands.NativeExecution\bin\$Configuration\net10.0\CopperOS.Commands.NativeExecution.dll"
$sdk = Join-Path $root 'CopperSharp.Sdk.Amiga.dll'
$compiler = Join-Path $root 'CopperSharp.Compiler.dll'
$compilerProject = Join-Path $CopperSharpRoot 'Compiler.Cli\CopperSharp.Compiler.Cli.csproj'
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
$run = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $root ('qualification-workbench31-filenote-native\' + [guid]::NewGuid().ToString('N'))
}
New-Item -ItemType Directory -Force -Path $run | Out-Null

& $DotnetPath build $compilerProject --configuration $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw 'CopperSharp compiler CLI build failed.' }
& $DotnetPath build $rootProject --configuration $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw 'Workbench Filenote native root build failed.' }
& $DotnetPath build $runnerProject --configuration $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw 'Native execution fixture build failed.' }
$support = Get-ChildItem -LiteralPath (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter 'CopperSharp.Sdk.Amiga.Support.dll' |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (-not $support) { throw 'Amiga support assembly is missing.' }

$artifacts = @()
foreach ($cpu in '68000','68020','68040') {
    $hunk = Join-Path $run "filenote-wb31-$cpu.hunk"
    $report = "$hunk.compatibility.json"
    & $DotnetPath $cli $assembly '--entry' 'CopperOS.Commands.AddBuffersNativeRoot.Workbench31FileNoteEntry::Main' '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' '--managed-assembly' $sdk '--managed-assembly' $support '--managed-assembly' $compiler '--output' $hunk '--compatibility-report' $report
    if ($LASTEXITCODE -ne 0) { throw "Workbench Filenote native entry compilation failed for $cpu." }
    $static = Get-Content -Raw -LiteralPath $report | ConvertFrom-Json
    $native = $static.NativeCompatibility
    if (-not $static.IsCompatible -or $static.ReachableMethodCount -ne 15 -or
        $native.RuntimeFeatureCount -ne 0 -or $native.RuntimeHelperCount -ne 0 -or
        $native.ExternalNativeTargetCount -ne 0 -or $native.ExceptionRegionCount -ne 0 -or
        $native.FatalMachineFaultSiteCount -ne 0) {
        throw "Workbench Filenote static compatibility is incomplete for $cpu."
    }
    $runtimeReport = "$hunk.runtime.json"
    & $DotnetPath $runner $hunk $cpu $runtimeReport 'wb31-filenote-native-entry-vector-fixture'
    if ($LASTEXITCODE -ne 0) { throw "Workbench Filenote supplied-DOS runtime fixture failed for $cpu." }
    $runtime = Get-Content -Raw -LiteralPath $runtimeReport | ConvertFrom-Json
    if ($runtime.status -ne 'passed' -or $runtime.passed -ne 10 -or
        $runtime.sharedImageWrites -ne 0 -or $runtime.nativeWrites -le 0) {
        throw "Workbench Filenote supplied-DOS runtime qualification is incomplete for $cpu."
    }
    $file = Get-Item -LiteralPath $hunk
    $artifacts += [ordered]@{
        cpu = $cpu
        bytes = $file.Length
        sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $hunk).Hash.ToLowerInvariant()
        reachableMethods = $static.ReachableMethodCount
        suppliedVectorInvocations = $runtime.passed
        runtimeReport = [IO.Path]::GetFileName($runtimeReport)
    }
}
[ordered]@{
    schemaVersion = 1
    suite = 'CC14-FileNote-wb31-native-entry-candidate'
    status = 'passed'
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.Workbench31FileNoteEntry::Main'
    scope = 'Three-CPU resident compilation plus ten supplied-DOS execution vectors per CPU for the bounded Workbench 3.1 Filenote syntax candidate. The entry uses DOS 36 and a separate profile startup boundary while sharing the public-DOS parser, matcher, recursive AnchorPath and SetComment body. Exact Workbench binary behavior, output/diagnostic parity, PURE/resident lifecycle and package admission remain open.'
    artifacts = $artifacts
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $run 'qualification.json') -Encoding utf8
Write-Host 'PASS Workbench Filenote native candidate: three resident HUNKs with zero managed runtime features, helpers, external targets, exception regions or fatal fault sites.'
