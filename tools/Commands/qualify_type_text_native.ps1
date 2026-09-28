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
$executorProject = Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj'
$root = Join-Path $repo "tests\Commands.TypeNativeRoot\bin\$Configuration\net10.0"
$assembly = Join-Path $root 'CopperOS.Commands.TypeNativeRoot.dll'
$sdk = Join-Path $root 'CopperSharp.Sdk.Amiga.dll'
$compiler = Join-Path $root 'CopperSharp.Compiler.dll'
$executor = Join-Path $repo "tests\Commands.NativeExecution\bin\$Configuration\net10.0\CopperOS.Commands.NativeExecution.dll"
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
$run = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $root ('qualification-type-text-native\' + [guid]::NewGuid().ToString('N'))
}
New-Item -ItemType Directory -Force -Path $run | Out-Null
& $DotnetPath build $project --configuration $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw 'Type native root build failed.' }
& $DotnetPath build $executorProject --configuration $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw 'Native execution fixture build failed.' }
$support = Get-ChildItem -LiteralPath (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter 'CopperSharp.Sdk.Amiga.Support.dll' |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (-not $support) { throw 'Amiga support assembly is missing.' }
$artifacts = @()
foreach ($cpu in '68000','68020','68040') {
    $hunk = Join-Path $run "type-text-$cpu.hunk"
    $report = "$hunk.compatibility.json"
    & $DotnetPath $cli $assembly '--entry' 'CopperOS.Commands.TypeNativeRoot.TypeTextProbe::Main' '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' '--managed-assembly' $sdk '--managed-assembly' $support '--managed-assembly' $compiler '--output' $hunk '--compatibility-report' $report
    if ($LASTEXITCODE -ne 0) { throw "Type native compilation failed for $cpu." }
    $static = Get-Content -Raw -LiteralPath $report | ConvertFrom-Json
    $native = $static.NativeCompatibility
    if (-not $static.IsCompatible -or $static.ReachableMethodCount -ne 10 -or
        $native.RuntimeFeatureCount -ne 0 -or $native.RuntimeHelperCount -ne 0 -or
        $native.ExternalNativeTargetCount -ne 0 -or $native.ExceptionRegionCount -ne 0 -or
        $native.FatalMachineFaultSiteCount -ne 0) { throw "Type static compatibility is incomplete for $cpu." }
    $executionReport = "$hunk.execution.json"
    & $DotnetPath $executor $hunk $cpu $executionReport 'type-text-probe-fixture'
    if ($LASTEXITCODE -ne 0) { throw "Type native execution failed for $cpu." }
    $execution = Get-Content -Raw -LiteralPath $executionReport | ConvertFrom-Json
    if ($execution.status -ne 'passed' -or $execution.suite -ne 'type-text-probe-fixture' -or
        $execution.cpu -ne $cpu -or $execution.imageSha256 -ne (Get-FileHash -Algorithm SHA256 -LiteralPath $hunk).Hash.ToLowerInvariant() -or
        $execution.sharedImageWrites -ne 0 -or $execution.passed -ne 10) {
        throw "Type native execution receipt is incomplete for $cpu."
    }
    $file = Get-Item -LiteralPath $hunk
    $artifacts += [ordered]@{ cpu=$cpu; bytes=$file.Length; sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $hunk).Hash.ToLowerInvariant(); reachableMethods=$static.ReachableMethodCount; executionReceipt=(Split-Path -Leaf $executionReport); nativeInvocations=$execution.passed; sharedImageWrites=$execution.sharedImageWrites }
}
[ordered]@{ schemaVersion=1; suite='CC11-Type-text-native'; status='passed'; runtime='resident'; entry='CopperOS.Commands.TypeNativeRoot.TypeTextProbe::Main'; scope='Static three-CPU compilation and Copper68k execution of the bounded Type text formatter; no DOS, outer command entry, or original-command parity claim.'; artifacts=$artifacts } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $run 'qualification.json') -Encoding utf8
Write-Host "PASS Type text native: 68000/020/040 resident HUNKs and direct control-block execution."
