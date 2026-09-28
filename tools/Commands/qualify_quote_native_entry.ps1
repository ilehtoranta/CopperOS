param(
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [string]$DotnetPath = 'dotnet',
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$rootProject = Join-Path $repo 'tests\Commands.QuoteNativeRoot\CopperOS.Commands.QuoteNativeRoot.csproj'
$executionProject = Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj'
$rootOutput = Join-Path $repo "tests\Commands.QuoteNativeRoot\bin\$Configuration\net10.0"
$assembly = Join-Path $rootOutput 'CopperOS.Commands.QuoteNativeRoot.dll'
$sdk = Join-Path $rootOutput 'CopperSharp.Sdk.Amiga.dll'
$compiler = Join-Path $rootOutput 'CopperSharp.Compiler.dll'
$runner = Join-Path $repo "tests\Commands.NativeExecution\bin\$Configuration\net10.0\CopperOS.Commands.NativeExecution.dll"
$cli = Join-Path $CopperSharpRoot "Compiler.Cli\bin\$Configuration\net10.0\CopperSharp.Compiler.Cli.dll"
$runRoot = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $rootOutput ('qualification-quote-native-entry\' + [guid]::NewGuid().ToString('N'))
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
    suite = 'CC10-Quote-native-entry-supplied-vector'
    status = 'running'
    failure = $null
    configuration = $Configuration
    runtime = 'resident'
    entry = 'CopperOS.Commands.QuoteNativeRoot.NativeMorphOSQuoteEntry::Main'
    expectedCasesPerCpu = 12
    executionScope = 'Copper68k supplied post-ReadArgs vectors and DOS/Exec adapters for the bounded MorphOS Quote STR forward entry; no real DOS parser, MorphOS shell, or original-command comparison.'
    artifacts = @()
}

try {
    New-Item -ItemType Directory -Path $runRoot -Force | Out-Null
    & $DotnetPath build $rootProject --configuration $Configuration --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Native Quote root build failed.' }
    & $DotnetPath build $executionProject --configuration $Configuration --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Native Quote execution fixture build failed.' }
    foreach ($path in @($assembly, $sdk, $compiler, $runner, $cli)) {
        Require (Test-Path -LiteralPath $path -PathType Leaf) "Required input is missing: $path"
    }
    $support = Get-ChildItem -LiteralPath (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') `
        -Recurse -Filter 'CopperSharp.Sdk.Amiga.Support.dll' -ErrorAction Stop |
        Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
    Require ($null -ne $support) 'CopperSharp.Sdk.Amiga.Support package assembly is missing.'

    foreach ($cpu in @('68000', '68020', '68040')) {
        $artifact = Join-Path $runRoot "quote-native-entry-resident-$cpu.hunk"
        $compatibility = "$artifact.compatibility.json"
        $execution = "$artifact.execution.json"
        $arguments = @(
            $cli, $assembly,
            '--entry', 'CopperOS.Commands.QuoteNativeRoot.NativeMorphOSQuoteEntry::Main',
            '--platform', 'amiga', '--cpu', $cpu,
            '--clr', 'always', '--exceptions', 'yolo',
            '--format', 'hunk', '--runtime', 'resident', '--memory', 'none',
            '--peephole', 'disabled',
            '--managed-assembly', $sdk,
            '--managed-assembly', $support,
            '--managed-assembly', $compiler,
            '--output', $artifact,
            '--compatibility-report', $compatibility)
        & $DotnetPath @arguments
        if ($LASTEXITCODE -ne 0) { throw "Native Quote compilation failed for $cpu." }
        $static = Get-Content -LiteralPath $compatibility -Raw | ConvertFrom-Json
        $native = $static.NativeCompatibility
        Require ([bool]$static.IsCompatible -and $static.RuntimeProfile -eq 'resident' -and
            $static.ReachableMethodCount -gt 0 -and @($static.ManagedAllocationSites).Count -eq 0 -and
            $native.ExceptionRegionCount -eq 0 -and $native.FatalMachineFaultSiteCount -eq 0 -and
            $native.RuntimeHelperCount -eq 0 -and $native.RuntimeFeatureCount -eq 0 -and
            $native.ExternalNativeTargetCount -eq 0) "Native Quote static report is incomplete for $cpu."

        & $DotnetPath $runner $artifact $cpu $execution 'quote-native-entry-vector-fixture'
        if ($LASTEXITCODE -ne 0) { throw "Native Quote execution failed for $cpu." }
        $runtime = Get-Content -LiteralPath $execution -Raw | ConvertFrom-Json
        Require ($runtime.status -eq 'passed' -and $runtime.cpu -eq $cpu -and
            $runtime.passed -eq 12 -and $runtime.imageLoads -eq 1 -and
            $runtime.sharedImageWrites -eq 0) "Native Quote execution receipt is incomplete for $cpu."
        $summary.artifacts += [ordered]@{
            cpu = $cpu
            artifact = Identity $artifact
            compatibility = Identity $compatibility
            execution = Identity $execution
            reachableMethods = $static.ReachableMethodCount
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

Write-Host 'PASS Quote native entry: 36 supplied-vector invocations across resident 68000/020/040 HUNKs.'
