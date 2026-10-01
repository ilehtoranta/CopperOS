param(
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [string]$DotnetPath = 'dotnet',
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$rootProject = Join-Path $repo 'tests\Commands.NativeRoot\CopperOS.Commands.NativeRoot.csproj'
$executionProject = Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj'
$rootOutput = Join-Path $repo "tests\Commands.NativeRoot\bin\$Configuration\net10.0"
$assembly = Join-Path $rootOutput 'CopperOS.Commands.NativeRoot.dll'
$native = Join-Path $rootOutput 'CopperOS.Commands.Native.dll'
$sdk = Join-Path $rootOutput 'CopperSharp.Sdk.Amiga.dll'
$compiler = Join-Path $rootOutput 'CopperSharp.Compiler.dll'
$runner = Join-Path $repo "tests\Commands.NativeExecution\bin\$Configuration\net10.0\CopperOS.Commands.NativeExecution.dll"
$cli = Join-Path $CopperSharpRoot "Compiler.Cli\bin\$Configuration\net10.0\CopperSharp.Compiler.Cli.dll"
$runRoot = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $rootOutput ('qualification-eval-native-entry\' + [guid]::NewGuid().ToString('N'))
}
$summaryPath = Join-Path $runRoot 'qualification.json'

function Require([bool]$condition, [string]$message) {
    if (-not $condition) { throw $message }
}

function Identity([string]$path) {
    $file = Get-Item -LiteralPath $path
    [ordered]@{
        path = $file.FullName
        bytes = $file.Length
        sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}

function Save([object]$value) {
    $value | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath "$summaryPath.next" -Encoding utf8
    Move-Item -LiteralPath "$summaryPath.next" -Destination $summaryPath -Force
}

$summary = [ordered]@{
    schemaVersion = 1
    suite = 'CC10-Eval-native-entry-supplied-vector'
    status = 'running'
    failure = $null
    configuration = $Configuration
    runtime = 'resident'
    entry = 'CopperOS.Commands.NativeRoot.NativeEvalEntry::Main'
    expectedCasesPerCpu = 20
    executionScope = 'Copper68k supplied post-ReadArgs vectors, including source-derived lowercase two-letter operator prefixes and the MorphOS 08/09 lexer quirk; no real DOS parser, OS, filesystem, or original-command comparison.'
    artifacts = @()
}

try {
    New-Item -ItemType Directory -Path $runRoot -Force | Out-Null
    & $DotnetPath build $rootProject --configuration $Configuration --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Native Eval root build failed.' }
    & $DotnetPath build $executionProject --configuration $Configuration --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Native Eval execution fixture build failed.' }
    foreach ($path in @($assembly, $native, $sdk, $compiler, $runner, $cli)) {
        Require (Test-Path -LiteralPath $path -PathType Leaf) "Required input is missing: $path"
    }
    $support = Get-ChildItem -LiteralPath (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') `
        -Recurse -Filter 'CopperSharp.Sdk.Amiga.Support.dll' -ErrorAction Stop |
        Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
    Require ($null -ne $support) 'CopperSharp.Sdk.Amiga.Support package assembly is missing.'

    foreach ($cpu in @('68000', '68020', '68040')) {
        $artifact = Join-Path $runRoot "eval-native-entry-resident-$cpu.hunk"
        $compatibility = "$artifact.compatibility.json"
        $execution = "$artifact.execution.json"
        $arguments = @(
            $cli, $assembly,
            '--entry', 'CopperOS.Commands.NativeRoot.NativeEvalEntry::Main',
            '--platform', 'amiga', '--cpu', $cpu,
            '--clr', 'always', '--exceptions', 'yolo',
            '--format', 'hunk', '--runtime', 'resident', '--memory', 'none',
            '--peephole', 'disabled',
            '--managed-assembly', $sdk,
            '--managed-assembly', $support,
            '--managed-assembly', $compiler,
            '--managed-assembly', $native,
            '--output', $artifact,
            '--compatibility-report', $compatibility)
        & $DotnetPath @arguments
        if ($LASTEXITCODE -ne 0) { throw "Native Eval compilation failed for $cpu." }
        $static = Get-Content -LiteralPath $compatibility -Raw | ConvertFrom-Json
        $nativeCompatibility = $static.NativeCompatibility
        Require ([bool]$static.IsCompatible -and $static.RuntimeProfile -eq 'resident' -and
            $static.ReachableMethodCount -gt 0 -and @($static.ManagedAllocationSites).Count -eq 0 -and
            $nativeCompatibility.FatalMachineFaultSiteCount -eq 0 -and
            $nativeCompatibility.RuntimeHelperCount -eq 0 -and
            $nativeCompatibility.ExternalNativeTargetCount -eq 0) "Native Eval static report is incomplete for $cpu."
        $features = @($nativeCompatibility.RuntimeFeatures)
        Require ($features.Count -eq 1 -and $features[0] -eq 'nullable-values') "Unexpected native Eval runtime feature set for $cpu."

        & $DotnetPath $runner $artifact $cpu $execution 'eval-native-entry-vector-fixture'
        if ($LASTEXITCODE -ne 0) { throw "Native Eval execution failed for $cpu." }
        $runtime = Get-Content -LiteralPath $execution -Raw | ConvertFrom-Json
        Require ($runtime.status -eq 'passed' -and $runtime.cpu -eq $cpu -and
            $runtime.passed -eq 20 -and $runtime.imageLoads -eq 1 -and
            $runtime.sharedImageWrites -eq 0) "Native Eval execution receipt is incomplete for $cpu."
        $summary.artifacts += [ordered]@{
            cpu = $cpu
            artifact = Identity $artifact
            compatibility = Identity $compatibility
            execution = Identity $execution
            reachableMethods = $static.ReachableMethodCount
            runtimeFeatures = @($nativeCompatibility.RuntimeFeatures)
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

Write-Host "PASS Eval native entry: 60 supplied-vector invocations across resident 68000/020/040 HUNKs."
