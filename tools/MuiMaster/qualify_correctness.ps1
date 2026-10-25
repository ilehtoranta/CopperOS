param(
    [string]$Configuration = 'Release',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\..\artifacts\mui-correctness-2026-10-23')
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $CopperSharpRoot "Compiler.Cli\bin\$Configuration\net10.0\CopperSharp.Compiler.Cli.dll"
if (!(Test-Path -LiteralPath $cli)) { throw "Compiler CLI missing: $cli" }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path

# Restore the pinned SDK explicitly: the full library builder uses the local
# SDK and shares the production project's assets file with host tests.
dotnet test (Join-Path $repo 'tests\MuiMaster\CopperOS.MuiMaster.Tests.csproj') -c $Configuration -p:CopperOSUseLocalCopperSharp=false --logger 'console;verbosity=minimal'
if ($LASTEXITCODE -ne 0) { throw 'MUI host regressions failed.' }
dotnet build (Join-Path $repo 'tests\MuiMaster.NativeRoot\CopperOS.MuiMaster.NativeRoot.csproj') -c $Configuration "-p:CopperSharp68kRoot=$CopperSharpRoot"
if ($LASTEXITCODE -ne 0) { throw 'Native root build failed.' }
dotnet build (Join-Path $repo 'tests\MuiMaster.NativeExecution\CopperOS.MuiMaster.NativeExecution.csproj') -c $Configuration
if ($LASTEXITCODE -ne 0) { throw 'Native runner build failed.' }

$rootDirectory = Join-Path $repo "tests\MuiMaster.NativeRoot\bin\$Configuration\net10.0"
$assembly = Join-Path $rootDirectory 'CopperOS.MuiMaster.NativeRoot.dll'
$sdk = Join-Path $rootDirectory 'CopperSharp.Sdk.Amiga.dll'
$support = Join-Path $rootDirectory 'CopperSharp.Sdk.Amiga.Support.dll'
$runner = Join-Path $repo "tests\MuiMaster.NativeExecution\bin\$Configuration\net10.0\CopperOS.MuiMaster.NativeExecution.dll"
$roots = @(
    @{ Name = 'loop-zero-mask'; Entry = 'MuiNativeApplicationLoopRoot::ZeroMaskContinuesInternalWork' },
    @{ Name = 'loop-ctrlc'; Entry = 'MuiNativeApplicationLoopRoot::CtrlCStopsRunWithNoApplicationSignals' },
    @{ Name = 'numeric-scaling'; Entry = 'MuiCorrectnessRoots::FullRangeNumericScaling' },
    @{ Name = 'rectangle-clip'; Entry = 'MuiCorrectnessRoots::RectangleClipUsesRelativeBounds' },
    @{ Name = 'queued-work'; Entry = 'MuiCorrectnessRoots::QueuedWorkPreventsWaiting' }
)
$results = foreach ($root in $roots) {
    $hunk = Join-Path $OutputDirectory ($root.Name + '.hunk')
    $report = Join-Path $OutputDirectory ($root.Name + '.framework.json')
    $arguments = @($assembly, '--entry', ('CopperOS.MuiMaster.NativeRoot.' + $root.Entry),
        '--platform', 'amiga', '--cpu', '68000', '--fpu', 'disabled',
        '--runtime', 'freestanding', '--memory', 'none', '--exceptions', 'yolo',
        '--exports', 'none', '--format', 'hunk', '--managed-assembly', $sdk,
        '--managed-assembly', $support)
    & dotnet $cli @arguments --output $hunk | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "$($root.Name) native compilation failed." }
    & dotnet $cli @arguments --framework-report $report | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "$($root.Name) framework analysis failed." }
    $analysis = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    $map = Get-Content -LiteralPath ($hunk + '.map') -Raw
    if (!$analysis.IsCompatible -or $analysis.RuntimeProfile -ne 'freestanding' -or
        $analysis.Cpu -ne 'm68000' -or $analysis.Members.Count -ne 0 -or
        $analysis.ManagedAllocationSites.Count -ne 0 -or
        $map -notmatch 'runtime-helpers=0' -or $map -notmatch 'runtime-features=0' -or
        $map -notmatch 'exception-regions=0' -or $map -notmatch 'external-native-targets=0' -or
        $map -match 'runtime:type-descriptor:') { throw "$($root.Name) violates the native contract." }
    $execution = & dotnet $runner $hunk
    if ($LASTEXITCODE -ne 0) { throw "$($root.Name) execution failed: $execution" }
    Write-Host $execution
    [pscustomobject]@{
        Name = $root.Name
        Entry = $root.Entry
        Sha256 = (Get-FileHash -LiteralPath $hunk -Algorithm SHA256).Hash
        Bytes = (Get-Item -LiteralPath $hunk).Length
        Execution = "$execution"
        FrameworkMembers = $analysis.Members.Count
        ManagedAllocations = $analysis.ManagedAllocationSites.Count
    }
}
$results | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'qualification.json')
