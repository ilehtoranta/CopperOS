param(
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [string]$DotnetPath = 'dotnet',
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$rootProject = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\CopperOS.Commands.AddBuffersNativeRoot.csproj'
$executionProject = Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj'
$rootOutput = Join-Path $repo "tests\Commands.AddBuffersNativeRoot\bin\$Configuration\net10.0"
$assembly = Join-Path $rootOutput 'CopperOS.Commands.AddBuffersNativeRoot.dll'
$sdk = Join-Path $rootOutput 'CopperSharp.Sdk.Amiga.dll'
$compiler = Join-Path $rootOutput 'CopperSharp.Compiler.dll'
$runner = Join-Path $repo "tests\Commands.NativeExecution\bin\$Configuration\net10.0\CopperOS.Commands.NativeExecution.dll"
$cli = Join-Path $CopperSharpRoot "Compiler.Cli\bin\$Configuration\net10.0\CopperSharp.Compiler.Cli.dll"
$runRoot = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $repo "artifacts\mount-morphos-native-entry-$((Get-Date).ToString('yyyyMMdd'))"
}
$summaryPath = Join-Path $runRoot 'qualification.json'

function Require([bool]$condition, [string]$message) {
    if (-not $condition) { throw $message }
}
function Identity([string]$path) {
    $file = Get-Item -LiteralPath $path
    [ordered]@{ path = $file.FullName; bytes = $file.Length; sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
}
function Save([object]$value) {
    $value | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath "$summaryPath.next" -Encoding utf8
    Move-Item -LiteralPath "$summaryPath.next" -Destination $summaryPath -Force
}

$summary = [ordered]@{
    schemaVersion = 1
    suite = 'CC22-Mount-morphos320-native-entry-boundary'
    status = 'running'
    failure = $null
    configuration = $Configuration
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSMountEntry::Main'
    expectedCasesPerCpu = 5
    executionScope = 'Supplied vectors cover the exact MorphOS outer ReadArgs template, cleared result ownership, empty DEVICE/M no-op semantics, parser failure, result allocation failure, repeat/interleaving, DOS startup and cleanup. Real DOSDrivers, DEVS:MountList, icon, Expansion and handler/provider execution, Workbench source iteration, PURE approval, original-guest differential and package admission remain open.'
    artifacts = @()
}

try {
    New-Item -ItemType Directory -Path $runRoot -Force | Out-Null
    & $DotnetPath build (Join-Path $CopperSharpRoot 'Compiler.Cli\CopperSharp.Compiler.Cli.csproj') --configuration $Configuration --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Compiler build failed.' }
    & $DotnetPath build $rootProject --configuration $Configuration --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Native Mount root build failed.' }
    & $DotnetPath build $executionProject --configuration $Configuration --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Native Mount execution fixture build failed.' }

    $support = Get-ChildItem -LiteralPath (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter 'CopperSharp.Sdk.Amiga.Support.dll' -ErrorAction Stop |
        Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
    foreach ($path in @($assembly, $sdk, $compiler, $runner, $cli, $support)) {
        Require (Test-Path -LiteralPath $path -PathType Leaf) "Required input is missing: $path"
    }

    foreach ($cpu in @('68000', '68020', '68040')) {
        $artifact = Join-Path $runRoot "mount-morphos-native-entry-resident-$cpu.hunk"
        $compatibility = "$artifact.compatibility.json"
        $execution = "$artifact.execution.json"
        & $DotnetPath $cli $assembly --entry $summary.entry --platform amiga --cpu $cpu --clr always --exceptions yolo --format hunk --runtime resident --memory none --peephole disabled --managed-assembly $sdk --managed-assembly $support --managed-assembly $compiler --output $artifact --compatibility-report $compatibility
        if ($LASTEXITCODE -ne 0) { throw "Native Mount compilation failed for $cpu." }
        $static = Get-Content -LiteralPath $compatibility -Raw | ConvertFrom-Json
        $native = $static.NativeCompatibility
        Require ([bool]$static.IsCompatible -and $static.RootMethodCount -eq 1 -and
            $static.ReachableMethodCount -gt 0 -and @($static.ManagedAllocationSites).Count -eq 0 -and
            $native.ExceptionRegionCount -eq 0 -and $native.RuntimeHelperCount -eq 0 -and
            $native.ExternalNativeTargetCount -eq 0 -and $native.FatalMachineFaultSiteCount -eq 0) "Native Mount static report is incomplete for $cpu."
        & $DotnetPath $runner $artifact $cpu $execution 'mount-morphos-native-entry-boundary-fixture'
        if ($LASTEXITCODE -ne 0) { throw "Native Mount execution failed for $cpu." }
        $runtime = Get-Content -LiteralPath $execution -Raw | ConvertFrom-Json
        Require ($runtime.status -eq 'passed' -and $runtime.cpu -eq $cpu -and
            $runtime.passed -eq 5 -and $runtime.imageLoads -eq 1 -and
            $runtime.sharedImageWrites -eq 0) "Native Mount execution receipt is incomplete for $cpu."
        $summary.artifacts += [ordered]@{
            cpu = $cpu
            artifact = Identity $artifact
            compatibility = Identity $compatibility
            execution = Identity $execution
            reachableMethods = $static.ReachableMethodCount
            runtimeFeatureCount = $native.RuntimeFeatureCount
            fatalMachineFaultSiteCount = $native.FatalMachineFaultSiteCount
            passedCases = $runtime.passed
            sharedImageWrites = $runtime.sharedImageWrites
        }
    }
    $summary.status = 'passed'
}
catch {
    $summary.status = 'failed'
    $summary.failure = $_.Exception.Message
    throw
}
finally {
    if (Test-Path -LiteralPath $runRoot) { Save $summary }
}

Write-Host 'PASS MorphOS Mount native boundary: 15 supplied invocations across resident 68000/020/040 HUNKs.'
