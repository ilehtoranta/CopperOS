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
    Join-Path $root ('qualification-type-text-io-native\' + [guid]::NewGuid().ToString('N'))
}
New-Item -ItemType Directory -Force -Path $run | Out-Null
& $DotnetPath build $project --configuration $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw 'Type text I/O native root build failed.' }
& $DotnetPath build $executorProject --configuration $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw 'Native execution fixture build failed.' }
$support = Get-ChildItem -LiteralPath (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter 'CopperSharp.Sdk.Amiga.Support.dll' |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (-not $support) { throw 'Amiga support assembly is missing.' }
$artifacts = @()
foreach ($cpu in '68000','68020','68040') {
    $hunk = Join-Path $run "type-text-io-$cpu.hunk"
    $compatibility = "$hunk.compatibility.json"
    $executionReceipt = "$hunk.execution.json"
    & $DotnetPath $cli $assembly '--entry' 'CopperOS.Commands.TypeNativeRoot.TypeTextIoProbe::Main' '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' '--managed-assembly' $sdk '--managed-assembly' $support '--managed-assembly' $compiler '--output' $hunk '--compatibility-report' $compatibility
    if ($LASTEXITCODE -ne 0) { throw "Type text I/O native compilation failed for $cpu." }
    $static = Get-Content -Raw -LiteralPath $compatibility | ConvertFrom-Json
    $native = $static.NativeCompatibility
    if (-not $static.IsCompatible -or $native.RuntimeFeatureCount -ne 0 -or
        $native.RuntimeHelperCount -ne 0 -or $native.ExceptionRegionCount -ne 0 -or
        $native.FatalMachineFaultSiteCount -ne 0) {
        throw "Type text I/O static compatibility is incomplete for $cpu."
    }
    & $DotnetPath $executor $hunk $cpu $executionReceipt 'type-text-io-probe-fixture'
    if ($LASTEXITCODE -ne 0) { throw "Type text I/O execution failed for $cpu." }
    $execution = Get-Content -Raw -LiteralPath $executionReceipt | ConvertFrom-Json
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $hunk).Hash.ToLowerInvariant()
    if ($execution.status -ne 'passed' -or $execution.suite -ne 'type-text-io-probe-fixture' -or
        $execution.cpu -ne $cpu -or $execution.imageSha256 -ne $hash -or
        $execution.sharedImageWrites -ne 0 -or $execution.passed -ne 6) {
        throw "Type text I/O execution receipt is incomplete for $cpu."
    }
    $file = Get-Item -LiteralPath $hunk
    $artifacts += [ordered]@{ cpu=$cpu; bytes=$file.Length; sha256=$hash; reachableMethods=$static.ReachableMethodCount; executionReceipt=(Split-Path -Leaf $executionReceipt); nativeInvocations=$execution.passed; sharedImageWrites=$execution.sharedImageWrites }
}
[ordered]@{ schemaVersion=1; suite='CC11-Type-text-io-native'; status='passed'; runtime='resident'; entry='CopperOS.Commands.TypeNativeRoot.TypeTextIoProbe::Main'; scope='Static three-CPU compilation and Copper68k supplied-DOS-vector execution of the bounded Type text stream stage; no ReadArgs, traversal, full command, or original-command parity claim.'; artifacts=$artifacts } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $run 'qualification.json') -Encoding utf8
Write-Host "PASS Type text I/O native: 68000/020/040 resident HUNKs and supplied DOS-vector execution."
