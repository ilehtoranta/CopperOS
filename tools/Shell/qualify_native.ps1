param(
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$Configuration = 'Release',
    [switch]$IncludeFullExecute,
    [switch]$UseShellTaskEntry
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($IncludeFullExecute -and $UseShellTaskEntry) {
    throw 'Choose either the capability probe or the production Shell task entry.'
}

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $repo 'tests\Shell.Dos.NativeRoot\CopperOS.Shell.Dos.NativeRoot.csproj'
$outDir = Join-Path $repo "tests\Shell.Dos.NativeRoot\bin\$Configuration\net10.0"
$cli = Join-Path $CopperSharpRoot "Compiler.Cli\bin\$Configuration\net10.0\CopperSharp.Compiler.Cli.dll"
$sdk = Join-Path $outDir 'CopperSharp.Sdk.Amiga.dll'
$sdkSupport = Join-Path $outDir 'CopperSharp.Sdk.Amiga.Support.dll'
$compiler = Join-Path $outDir 'CopperSharp.Compiler.dll'
$exec = Join-Path $outDir 'CopperStart.Exec.dll'
$dos = Join-Path $outDir 'CopperStart.Dos.dll'
$assembly = Join-Path $outDir 'CopperOS.Shell.Dos.NativeRoot.dll'

dotnet restore $project --verbosity minimal `
    "-p:CopperSharp68kRoot=$CopperSharpRoot"
if ($LASTEXITCODE -ne 0) { throw 'Shell native-root restore failed.' }

dotnet build $project --configuration $Configuration --no-restore `
    --verbosity minimal "-p:CopperSharp68kRoot=$CopperSharpRoot"
if ($LASTEXITCODE -ne 0) { throw 'Shell native-root managed input build failed.' }

if (-not (Test-Path -LiteralPath $cli -PathType Leaf)) { throw "Compiler CLI not found: $cli" }
if (-not (Test-Path -LiteralPath $sdk -PathType Leaf)) { throw "SDK assembly not found: $sdk" }
if (-not (Test-Path -LiteralPath $sdkSupport -PathType Leaf)) { throw "SDK support assembly not found: $sdkSupport" }

# Use the project-reference copies from one directory. Loading sibling build
# outputs creates duplicate SDK identities and interferes with concurrent builds
# if their SDK copies are removed to work around that ambiguity.
foreach ($dependency in @($compiler, $exec, $dos)) {
    if (-not (Test-Path -LiteralPath $dependency -PathType Leaf)) {
        throw "Shell native dependency not found: $dependency"
    }
}

$entryPoint = 'CopperOS.Shell.Dos.NativeRoot.DosShellNativeRoots::ParkCapabilityRoot'
$exports = @('copperos.shell.execute-park')
$mode = 'park'
if ($IncludeFullExecute) {
    $mode = 'execute'
    $entryPoint = 'CopperOS.Shell.Dos.NativeRoot.DosShellNativeRoots::CapabilityRoot'
    # ExportAddress resolves only symbols admitted by this explicit allowlist.
    # The standalone qualification root wraps the production DOS return owner.
    $exports = @('copperos.shell.execute-begin', 'copperos.shell.execute-poll',
        'copperos.shell.entry', 'copperos.shell.child',
        'copperos.shell.execute-park',
        'copperstart.dos.process-return')
} elseif ($UseShellTaskEntry) {
    $mode = 'shell-task'
    $entryPoint = 'CopperOS.Shell.Dos.DosShellNativeEntrypoints::ShellTaskEntry'
    $exports = @('copperos.shell.execute-begin', 'copperos.shell.execute-poll',
        'copperos.shell.entry', 'copperos.shell.child',
        'copperos.shell.execute-park',
        'copperstart.dos.process-return')
}

$targets = @(
    [ordered]@{ Cpu = '68000'; Format = 'hunk'; Extension = 'hunk' },
    [ordered]@{ Cpu = '68020'; Format = 'asm'; Extension = 's' },
    [ordered]@{ Cpu = '68040'; Format = 'asm'; Extension = 's' }
)

$results = foreach ($target in $targets) {
    $name = "shell-$mode-$($target.Cpu)"
    $output = Join-Path $outDir "$name.$($target.Extension)"
    $report = "$output.compatibility.json"
    $arguments = @(
        $cli, $assembly,
        '--entry', $entryPoint,
        '--platform', 'amiga', '--cpu', $target.Cpu,
        '--clr', 'always', '--exceptions', 'yolo',
        '--format', $target.Format, '--runtime', 'freestanding',
        '--memory', 'none', '--peephole', 'disabled',
        '--managed-assembly', $sdk,
        '--managed-assembly', $sdkSupport,
        '--managed-assembly', $compiler,
        '--managed-assembly', $exec,
        '--managed-assembly', $dos,
        '--output', $output,
        '--compatibility-report', $report)
    foreach ($export in $exports) {
        $arguments += @('--include-export', $export)
    }
    dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "Shell native compilation failed for $($target.Cpu)." }

    $data = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    $map = "$output.map"
    if (-not (Test-Path -LiteralPath $map -PathType Leaf)) {
        throw "Shell native map is missing: $map"
    }
    $mapText = Get-Content -LiteralPath $map -Raw
    foreach ($export in $exports) {
        $symbolPattern = '(?m)^[0-9A-Fa-f]{8}\s+\d+\s+' +
            [regex]::Escape($export) + '\r?$'
        if ($export -notin $data.IncludedExportNames -or
            $mapText -notmatch $symbolPattern) {
            throw "Required Shell export missing from native artifact: $export"
        }
    }
    $forbiddenRuntimeSymbols = @(
        '__c68k_exception_',
        'runtime:exception-',
        'runtime:method-unwind-',
        'runtime:type-descriptor:System.Exception',
        '__c68k_gc_',
        'runtime:gc-',
        'runtime:root-map-',
        'runtime:managed-pool-'
    )
    $hasForbiddenRuntimeSymbol = $false
    foreach ($symbol in $forbiddenRuntimeSymbols) {
        if ($mapText.Contains($symbol)) {
            $hasForbiddenRuntimeSymbol = $true
            break
        }
    }
    if (-not [bool]$data.IsCompatible -or
        [string]$data.RuntimeProfile -ne 'freestanding' -or
        @($data.Members | Where-Object { $_.Status -ne 'implemented' }).Count -ne 0 -or
        @($data.ManagedAllocationSites).Count -ne 0 -or
        [int]$data.NativeCompatibility.ExceptionRegionCount -ne 0 -or
        @($data.NativeCompatibility.RuntimeFeatures).Count -ne 0 -or
        @($data.NativeCompatibility.RuntimeHelpers).Count -ne 0 -or
        @($data.NativeCompatibility.ExternalNativeTargets).Count -ne 0 -or
        -not $mapText.Contains('framework-features=0') -or
        -not $mapText.Contains('managed-allocation-sites=0') -or
        $hasForbiddenRuntimeSymbol) {
        throw "Shell compatibility report is incomplete: $report"
    }

    [pscustomobject]@{
        Cpu = $target.Cpu
        Artifact = $output
        Compatibility = $report
        ReachableMethods = [int]$data.ReachableMethodCount
        ManagedAllocations = @($data.ManagedAllocationSites).Count
    }
}

$results | Format-Table -AutoSize
if ($UseShellTaskEntry) {
    Write-Host "Shell task entry native qualification passed in $outDir"
} elseif ($IncludeFullExecute) {
    Write-Host "Shell Execute ABI native qualification passed in $outDir"
} else {
    Write-Host "Shell park ABI native qualification passed in $outDir"
}
