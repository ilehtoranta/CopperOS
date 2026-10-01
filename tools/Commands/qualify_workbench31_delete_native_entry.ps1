param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$rootProject = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\CopperOS.Commands.AddBuffersNativeRoot.csproj'
$runnerProject = Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj'
$root = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\bin\Release\net10.0'
$runner = Join-Path $repo 'tests\Commands.NativeExecution\bin\Release\net10.0\CopperOS.Commands.NativeExecution.dll'
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
$run = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $root ('qualification-workbench31-delete-native-entry\' + [guid]::NewGuid().ToString('N'))
}
New-Item -ItemType Directory -Force -Path $run | Out-Null

& $DotnetPath build (Join-Path $CopperSharpRoot 'Compiler.Cli\CopperSharp.Compiler.Cli.csproj') -c Release --nologo
if ($LASTEXITCODE) { throw 'Compiler build failed.' }
& $DotnetPath build $rootProject -c Release --nologo
if ($LASTEXITCODE) { throw 'Root build failed.' }
& $DotnetPath build $runnerProject -c Release --nologo
if ($LASTEXITCODE) { throw 'Runner build failed.' }

$support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'Support assembly missing.' }

$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $run "delete-wb31-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    $runtime = "$hunk.runtime.json"
    & $DotnetPath $cli (Join-Path $root 'CopperOS.Commands.AddBuffersNativeRoot.dll') '--entry' 'CopperOS.Commands.AddBuffersNativeRoot.Workbench31DeleteEntry::Main' '--platform' amiga '--cpu' $cpu '--clr' always '--exceptions' yolo '--format' hunk '--runtime' resident '--memory' none '--peephole' disabled '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') '--managed-assembly' $support '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') '--output' $hunk '--compatibility-report' $static
    if ($LASTEXITCODE) { throw "Compile failed for $cpu." }
    & $DotnetPath $runner $hunk $cpu $runtime wb31-delete-command-native-entry-vector-fixture
    if ($LASTEXITCODE) { throw "Runtime fixture failed for $cpu." }
    $receipt = Get-Content -Raw $static | ConvertFrom-Json
    $execution = Get-Content -Raw $runtime | ConvertFrom-Json
    $native = $receipt.NativeCompatibility
    if (!$receipt.IsCompatible -or $receipt.ReachableMethodCount -ne 43 -or
        $native.RuntimeFeatureCount -ne 0 -or $native.RuntimeHelperCount -ne 0 -or
        $native.ExternalNativeTargetCount -ne 0 -or $native.ExceptionRegionCount -ne 0 -or
        $native.FatalMachineFaultSiteCount -ne 0 -or $execution.status -ne 'passed' -or
        $execution.passed -ne 8 -or $execution.sharedImageWrites -ne 0) {
        throw "Receipt incomplete for $cpu (methods=$($receipt.ReachableMethodCount))."
    }
    $file = Get-Item -LiteralPath $hunk
    $artifacts += [ordered]@{
        cpu = $cpu
        bytes = $file.Length
        sha256 = (Get-FileHash $hunk -Algorithm SHA256).Hash.ToLowerInvariant()
        reachableMethods = $receipt.ReachableMethodCount
        runtimeFeatureCount = $native.RuntimeFeatureCount
        runtimeHelperCount = $native.RuntimeHelperCount
        externalNativeTargetCount = $native.ExternalNativeTargetCount
        exceptionRegionCount = $native.ExceptionRegionCount
        fatalMachineFaultSiteCount = $native.FatalMachineFaultSiteCount
        suppliedVectorInvocations = $execution.passed
        sharedImageWrites = $execution.sharedImageWrites
    }
}

[ordered]@{
    schemaVersion = 1
    suite = 'CC13-Delete-wb31-native-entry-candidate'
    status = 'passed'
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.Workbench31DeleteEntry::Main'
    scope = 'Static three-CPU resident compilation plus eight supplied-DOS invocations per CPU for the Workbench 3.1 four-slot Delete syntax candidate. The entry uses DOS 36 and shares the public-DOS matcher, lock, protection and DeleteFile worker with the MorphOS frontend. Exact Workbench binary behavior, diagnostics, recursion/link policy, PURE/resident lifecycle, packaging and differential parity remain open.'
    runtimeFixture = $true
    artifacts = $artifacts
} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8

Write-Host "PASS Workbench Delete candidate: $($artifacts.Count) CPU artifacts and supplied runtime vectors per CPU."
