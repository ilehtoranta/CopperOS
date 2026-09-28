param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$rootProject = Join-Path $repo 'tests\Commands.Exe2ArcNativeRoot\CopperOS.Commands.Exe2ArcNativeRoot.csproj'
$root = Join-Path $repo 'tests\Commands.Exe2ArcNativeRoot\bin\Release\net10.0'
$cliProject = Join-Path $CopperSharpRoot 'Compiler.Cli\CopperSharp.Compiler.Cli.csproj'
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
$run = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $repo ('artifacts\exe2arc-native-static-' + [guid]::NewGuid().ToString('N'))
}
if (Test-Path -LiteralPath $run) { throw "Output directory already exists: $run" }
New-Item -ItemType Directory -Force $run | Out-Null

& $DotnetPath build $cliProject -c Release --nologo
if ($LASTEXITCODE) { throw 'CopperSharp compiler build failed.' }
& $DotnetPath restore $rootProject --nologo -p:CopperOSUseLocalCopperSharp=true
if ($LASTEXITCODE) { throw 'Exe2Arc root restore failed.' }
& $DotnetPath build $rootProject -c Release --no-restore --nologo -p:CopperOSUseLocalCopperSharp=true
if ($LASTEXITCODE) { throw 'Exe2Arc root managed build failed.' }

$support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'CopperSharp SDK support assembly missing.' }
if (!(Test-Path -LiteralPath $cli -PathType Leaf)) { throw "Compiler CLI missing: $cli" }

$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $run "exe2arc-$cpu.hunk"
    $report = "$hunk.compatibility.json"
    & $DotnetPath $cli (Join-Path $root 'CopperOS.Commands.Exe2ArcNativeRoot.dll') `
        --entry 'CopperOS.Commands.Exe2ArcNativeRoot.Exe2ArcEntry::Main' `
        --platform amiga --cpu $cpu --clr always --exceptions yolo `
        --format hunk --runtime resident --memory none --peephole disabled `
        --managed-assembly (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') `
        --managed-assembly $support `
        --managed-assembly (Join-Path $root 'CopperSharp.Compiler.dll') `
        --output $hunk --compatibility-report $report
    if ($LASTEXITCODE) { throw "Exe2Arc native compilation failed for $cpu." }

    $receipt = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    $native = $receipt.NativeCompatibility
    if (!$receipt.IsCompatible -or
        $native.RuntimeFeatureCount -ne 0 -or
        $native.RuntimeHelperCount -ne 0 -or
        $native.ExternalNativeTargetCount -ne 0 -or
        $native.ExceptionRegionCount -ne 0 -or
        $native.FatalMachineFaultSiteCount -ne 0 -or
        @($receipt.ManagedAllocationSites).Count -ne 0) {
        throw "Static native compatibility gate failed for $cpu."
    }

    $map = "$hunk.map"
    if (!(Test-Path -LiteralPath $map -PathType Leaf)) { throw "Native map missing: $map" }
    $file = Get-Item -LiteralPath $hunk
    $artifacts += [ordered]@{
        cpu = $cpu
        hunk = $hunk
        bytes = $file.Length
        sha256 = (Get-FileHash -LiteralPath $hunk -Algorithm SHA256).Hash.ToLowerInvariant()
        compatibilityReport = $report
        compatibilitySha256 = (Get-FileHash -LiteralPath $report -Algorithm SHA256).Hash.ToLowerInvariant()
        mapSha256 = (Get-FileHash -LiteralPath $map -Algorithm SHA256).Hash.ToLowerInvariant()
        reachableMethods = [int]$receipt.ReachableMethodCount
        runtimeFeatureCount = [int]$native.RuntimeFeatureCount
        runtimeHelperCount = [int]$native.RuntimeHelperCount
        externalNativeTargetCount = [int]$native.ExternalNativeTargetCount
        exceptionRegionCount = [int]$native.ExceptionRegionCount
        fatalMachineFaultSiteCount = [int]$native.FatalMachineFaultSiteCount
        managedAllocationSites = @($receipt.ManagedAllocationSites).Count
    }
}

$sourcePaths = @(
    'src/Commands/Exe2ArcHeaderProbe.cs',
    'src/Commands/Exe2ArcIo.cs',
    'src/Commands/Exe2ArcForwardScanner.cs',
    'src/Commands/Exe2ArcPayloadCopy.cs',
    'src/Commands/Native/NativeExe2ArcIo.cs',
    'src/Commands/Exe2ArcOutputName.cs',
    'src/Commands/Exe2ArcOutputSelection.cs',
    'src/Commands/Native/NativeExe2ArcOutputIo.cs',
    'src/Commands/Exe2ArcTypeSelection.cs',
    'src/Commands/Exe2ArcScannerSelection.cs',
    'src/Commands/Exe2ArcResultPolicy.cs',
    'src/Commands/Native/NativeMorphOSExe2ArcCommand.cs',
    'src/Commands/Native/NativeExe2ArcArgumentGate.cs',
    'src/Commands/Exe2ArcLhaScanner.cs',
    'src/Commands/Exe2ArcZipScanner.cs',
    'src/Commands/Exe2ArcZipRecordRewriter.cs',
    'src/Commands/Exe2ArcZipRecordExtractor.cs',
    'tests/Commands.Exe2ArcNativeRoot/CopperOS.Commands.Exe2ArcNativeRoot.csproj',
    'tests/Commands.Exe2ArcNativeRoot/Exe2ArcEntry.cs'
)
$sourceEvidence = @()
foreach ($relative in $sourcePaths) {
    $path = Join-Path $repo $relative
    $sourceEvidence += [ordered]@{
        path = $relative
        sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}

[ordered]@{
    schemaVersion = 1
    suite = 'CC32-Exe2Arc-native-static'
    status = 'passed'
    runtime = 'resident'
    entry = 'CopperOS.Commands.Exe2ArcNativeRoot.Exe2ArcEntry::Main'
    cpus = @('68000', '68020', '68040')
    scope = 'Three-CPU resident HUNK compilation of the source-linked MorphOS Exe2Arc frontend. Static compiler compatibility only; no original guest, filesystem handler, runtime fixture, PURE admission or shipping claim.'
    sourceEvidence = $sourceEvidence
    artifacts = $artifacts
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $run 'qualification.json') -Encoding utf8

Write-Host "Exe2Arc native static qualification passed: $run"
