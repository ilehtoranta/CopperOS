param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$runnerProject = Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj'
$runner = Join-Path $repo 'tests\Commands.NativeExecution\bin\Release\net10.0\CopperOS.Commands.NativeExecution.dll'
$staticScript = Join-Path $PSScriptRoot 'qualify_workbench31_binddrivers_native_static.ps1'
$run = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $repo ('artifacts\workbench31-binddrivers-native-entry-' + [guid]::NewGuid().ToString('N'))
}
$staticRun = Join-Path $run 'static'
New-Item -ItemType Directory -Force -Path $run | Out-Null

& $staticScript -DotnetPath $DotnetPath -CopperSharpRoot $CopperSharpRoot -OutputDirectory $staticRun
if ($LASTEXITCODE) { throw 'Workbench BindDrivers static resident qualification failed.' }

& $DotnetPath build $runnerProject -c Release --nologo
if ($LASTEXITCODE) { throw 'Native execution fixture build failed.' }

$staticReceipt = Get-Content -Raw (Join-Path $staticRun 'qualification.json') | ConvertFrom-Json
if ($staticReceipt.status -ne 'passed' -or $staticReceipt.artifacts.Count -ne 3) {
    throw 'Workbench BindDrivers three-CPU static receipt is incomplete.'
}

$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $staticRun "binddrivers-wb31-$cpu.hunk"
    $runtimePath = Join-Path $run "binddrivers-wb31-$cpu.runtime.json"
    & $DotnetPath $runner $hunk $cpu $runtimePath 'binddrivers-wb31-native-entry-vector-fixture'
    if ($LASTEXITCODE) { throw "Workbench BindDrivers runtime fixture failed for $cpu." }

    $runtime = Get-Content -Raw $runtimePath | ConvertFrom-Json
    $static = $staticReceipt.artifacts | Where-Object cpu -eq $cpu | Select-Object -First 1
    if (!$static -or $runtime.status -ne 'passed' -or $runtime.passed -ne 12 -or
        $runtime.sharedImageWrites -ne 0 -or $runtime.realKickstartExecution -ne $false -or
        $runtime.referenceCommandBehavior -ne $false -or
        $runtime.shippingOrPureApproval -ne $false -or
        $static.runtimeFeatureCount -ne 0 -or $static.runtimeHelperCount -ne 0 -or
        $static.externalNativeTargetCount -ne 0 -or $static.exceptionRegionCount -ne 0 -or
        $static.fatalMachineFaultSiteCount -ne 0) {
        throw "Workbench BindDrivers qualification receipt incomplete for $cpu."
    }

    $artifacts += [ordered]@{
        cpu = $cpu
        bytes = $static.bytes
        sha256 = $static.sha256
        reachableMethods = $static.reachableMethods
        runtimeFeatureCount = $static.runtimeFeatureCount
        runtimeHelperCount = $static.runtimeHelperCount
        externalNativeTargetCount = $static.externalNativeTargetCount
        exceptionRegionCount = $static.exceptionRegionCount
        fatalMachineFaultSiteCount = $static.fatalMachineFaultSiteCount
        suppliedVectorInvocations = $runtime.passed
        runtimeReport = [IO.Path]::GetFileName($runtimePath)
        sharedImageWrites = $runtime.sharedImageWrites
    }
}

[ordered]@{
    schemaVersion = 1
    suite = 'CC20-BindDrivers-wb31-native-entry'
    status = 'passed'
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.NativeWorkbench31BindDriversEntry::Main'
    scope = 'Three-CPU resident HUNK compatibility plus nine supplied-vector invocations per CPU covering library-open failures and order, directory Lock/Examine validation, no-match scans, CurrentDir restoration, ExNext cleanup, case-insensitive .info stripping, strict PRODUCT parsing, ConfigDev lookup, CurrentBinding field offsets, LoadSeg, Resident discovery, InitResident, and Workbench startup-message reply after cleanup. Guest execution, original-binary parity, PURE, lifecycle, licensing, and package gates remain open.'
    artifacts = $artifacts
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
