param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $repo 'tests\Commands.TypeNativeRoot\CopperOS.Commands.TypeNativeRoot.csproj'
$nativeExecutionProject = Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj'
$compilerProject = Join-Path $CopperSharpRoot 'Compiler.Cli\CopperSharp.Compiler.Cli.csproj'
$root = Join-Path $repo "tests\Commands.TypeNativeRoot\bin\$Configuration\net10.0"
$assembly = Join-Path $root 'CopperOS.Commands.TypeNativeRoot.dll'
$sdk = Join-Path $root 'CopperSharp.Sdk.Amiga.dll'
$compiler = Join-Path $root 'CopperSharp.Compiler.dll'
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
$nativeExecution = Join-Path $repo "tests\Commands.NativeExecution\bin\$Configuration\net10.0\CopperOS.Commands.NativeExecution.dll"
$run = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $root ('qualification-type-native-entry\' + [guid]::NewGuid().ToString('N'))
}
New-Item -ItemType Directory -Force -Path $run | Out-Null
& $DotnetPath build $compilerProject --configuration $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw 'CopperSharp compiler CLI build failed.' }
& $DotnetPath build $project --configuration $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw 'Type native root build failed.' }
& $DotnetPath build $nativeExecutionProject --configuration $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw 'Native Type entry execution fixture build failed.' }
$support = Get-ChildItem -LiteralPath (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter 'CopperSharp.Sdk.Amiga.Support.dll' |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (-not $support) { throw 'Amiga support assembly is missing.' }
$artifacts = @()
foreach ($cpu in '68000','68020','68040') {
    $hunk = Join-Path $run "type-$cpu.hunk"
    $report = "$hunk.compatibility.json"
    & $DotnetPath $cli $assembly '--entry' 'CopperOS.Commands.TypeNativeRoot.NativeMorphOSTypeEntry::Main' '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' '--managed-assembly' $sdk '--managed-assembly' $support '--managed-assembly' $compiler '--output' $hunk '--compatibility-report' $report
    if ($LASTEXITCODE -ne 0) { throw "Type native entry compilation failed for $cpu." }
    $runtimeReport = "$hunk.runtime.json"
    & $DotnetPath $nativeExecution $hunk $cpu $runtimeReport 'type-native-entry-vector-fixture'
    if ($LASTEXITCODE -ne 0) { throw "Type native entry execution failed for $cpu." }
    $static = Get-Content -Raw -LiteralPath $report | ConvertFrom-Json
    $runtime = Get-Content -Raw -LiteralPath $runtimeReport | ConvertFrom-Json
    $native = $static.NativeCompatibility
    if (-not $static.IsCompatible -or $static.ReachableMethodCount -ne 26 -or
        $native.RuntimeFeatureCount -ne 0 -or $native.RuntimeHelperCount -ne 0 -or
        $native.ExternalNativeTargetCount -ne 0 -or $native.ExceptionRegionCount -ne 0 -or
        $native.FatalMachineFaultSiteCount -ne 0) {
        throw "Type native entry purity compatibility is incomplete for $cpu."
    }
    $file = Get-Item -LiteralPath $hunk
    if ($runtime.status -ne 'passed' -or $runtime.passed -ne 16 -or
        $runtime.nativeWrites -le 0 -or $runtime.nativeReads -le 0) {
        throw "Type native entry runtime receipt is incomplete for $cpu."
    }
    $artifacts += [ordered]@{ cpu=$cpu; bytes=$file.Length; sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $hunk).Hash.ToLowerInvariant(); reachableMethods=$static.ReachableMethodCount; suppliedVectorInvocations=$runtime.passed; runtimeReport=(Split-Path -Leaf $runtimeReport) }
}
[ordered]@{ schemaVersion=1; suite='CC11-Type-native-entry'; status='passed'; runtime='resident'; entry='CopperOS.Commands.TypeNativeRoot.NativeMorphOSTypeEntry::Main'; scope='Static three-CPU pure/resident compilation plus supplied DOS parser and wildcard vectors for bounded MorphOS Type text and HEX streams. It checks the DOS 50.67 version gate, literal-soft-link AnchorPath flag, ignored-option and HEX/NUMBER behavior, and source-shaped Read/Write failures including a positive short write followed by an error with selected IoErr. Parser, filesystem, and DOS I/O results are fixture inputs; this is not original-command parity.'; artifacts=$artifacts } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $run 'qualification.json') -Encoding utf8
Write-Host "PASS Type native entry: 68000/020/040 pure resident HUNKs plus supplied vectors."
