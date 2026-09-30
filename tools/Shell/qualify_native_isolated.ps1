param(
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$Configuration = 'Release',
    [string]$ArtifactDirectory = 'artifacts\shell-native-isolated-20260926-v1',
    [switch]$IncludeFullExecute,
    [switch]$UseShellTaskEntry
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($IncludeFullExecute -and $UseShellTaskEntry) {
    throw 'Choose either the capability probe or the production Shell task entry.'
}

function Get-InputEvidence {
    param([string[]]$Paths)
    foreach ($path in $Paths | Sort-Object -Unique) {
        $item = Get-Item -LiteralPath $path
        [ordered]@{
            path = [IO.Path]::GetRelativePath($repo, $item.FullName)
            bytes = $item.Length
            sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }
}

function Get-SourceEvidence {
    $sourceFiles = @(
        (Join-Path $repo 'CopperOS.Portable.props'),
        (Join-Path $repo '..\CopperStart\Directory.Build.props'))
    foreach ($sourceRoot in $sourceRoots) {
        $sourceFiles += Get-ChildItem -LiteralPath $sourceRoot -Recurse -File |
            Where-Object {
                $_.Extension -in '.cs', '.csproj' -and
                $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
            } | ForEach-Object FullName
    }
    Get-InputEvidence -Paths $sourceFiles
}

function Copy-InputSnapshot {
    param([string]$SourceDirectory, [string]$DestinationDirectory)
    # Keep resolver siblings together. Read every source hash before copying,
    # then verify both the source inventory and the copied bytes before use.
    $files = @(Get-ChildItem -LiteralPath $SourceDirectory -File |
        Where-Object {
            $_.Extension -eq '.dll' -or
            $_.Name -like '*.deps.json' -or $_.Name -like '*.runtimeconfig.json'
        } | Sort-Object Name)
    $before = @(Get-InputEvidence -Paths @($files.FullName))
    New-Item -ItemType Directory -Path $DestinationDirectory | Out-Null
    foreach ($file in $files) {
        Copy-Item -LiteralPath $file.FullName -Destination $DestinationDirectory
    }
    $afterFiles = @(Get-ChildItem -LiteralPath $SourceDirectory -File |
        Where-Object {
            $_.Extension -eq '.dll' -or
            $_.Name -like '*.deps.json' -or $_.Name -like '*.runtimeconfig.json'
        } | Sort-Object Name)
    $after = @(Get-InputEvidence -Paths @($afterFiles.FullName))
    if (($before | ConvertTo-Json -Depth 4 -Compress) -cne
        ($after | ConvertTo-Json -Depth 4 -Compress)) {
        throw "Qualification inputs changed during snapshot: $SourceDirectory. Preserve this run and retry in a fresh artifact directory."
    }
    foreach ($inputFile in $before) {
        $destination = Join-Path $DestinationDirectory ([IO.Path]::GetFileName($inputFile.path))
        $evidence = @(Get-InputEvidence -Paths @($destination))[0]
        if ($evidence.sha256 -cne $inputFile.sha256 -or $evidence.bytes -ne $inputFile.bytes) {
            throw "Qualification snapshot differs from its build output: $destination"
        }
        [ordered]@{
            path = $evidence.path
            sourcePath = $inputFile.path
            bytes = $evidence.bytes
            sha256 = $evidence.sha256
        }
    }
}

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$run = [IO.Path]::GetFullPath((Join-Path $repo $ArtifactDirectory))
if ((Test-Path -LiteralPath $run) -and
    @(Get-ChildItem -LiteralPath $run -Force).Count -ne 0) {
    throw 'Use a fresh artifact directory; preserve previous qualification evidence.'
}
New-Item -ItemType Directory -Force $run | Out-Null

$project = Join-Path $repo 'tests\Shell.Dos.NativeRoot\CopperOS.Shell.Dos.NativeRoot.csproj'
$outDir = Join-Path $repo "tests\Shell.Dos.NativeRoot\bin\$Configuration\net10.0"
$cliSourceDirectory = Join-Path $CopperSharpRoot "Compiler.Cli\bin\$Configuration\net10.0"
$requiredManagedNames = @('CopperOS.Shell.Dos.NativeRoot.dll',
    'CopperSharp.Sdk.Amiga.dll', 'CopperSharp.Sdk.Amiga.Support.dll',
    'CopperSharp.Compiler.dll', 'CopperStart.Exec.dll', 'CopperStart.Dos.dll')
$sourceRoots = @(
    (Join-Path $repo 'tests\Shell.Dos.NativeRoot'),
    (Join-Path $repo 'src\System\Shell'),
    (Join-Path $repo '..\CopperStart\src\CopperStart.Exec'),
    (Join-Path $repo '..\CopperStart\src\CopperStart.Dos'))
$sourceBeforeBuild = @(Get-SourceEvidence)
$buildStartedUtc = [DateTime]::UtcNow.ToString('o')

dotnet restore $project --verbosity minimal "-p:CopperSharp68kRoot=$CopperSharpRoot"
if ($LASTEXITCODE -ne 0) { throw 'Shell native-root restore failed.' }
dotnet build $project --configuration $Configuration --no-restore `
    --verbosity minimal "-p:CopperSharp68kRoot=$CopperSharpRoot"
if ($LASTEXITCODE -ne 0) { throw 'Shell native-root managed input build failed.' }
$buildFinishedUtc = [DateTime]::UtcNow.ToString('o')
$sourceAfterBuild = @(Get-SourceEvidence)

foreach ($name in $requiredManagedNames) {
    $path = Join-Path $outDir $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required Shell qualification input is missing: $path"
    }
}
foreach ($name in @('CopperSharp.Compiler.Cli.dll',
    'CopperSharp.Compiler.Cli.deps.json', 'CopperSharp.Compiler.Cli.runtimeconfig.json')) {
    $path = Join-Path $cliSourceDirectory $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required compiler input is missing: $path"
    }
}

# Every managed dependency comes from the same project build output, then all
# three targets use the frozen copies. No sibling build output is modified.
$managedInputs = Join-Path $run 'managed-inputs'
$buildInputs = @(Copy-InputSnapshot -SourceDirectory $outDir -DestinationDirectory $managedInputs)
$toolchainInputs = Join-Path $run 'compiler-inputs'
$compilerInputs = @(Copy-InputSnapshot -SourceDirectory $cliSourceDirectory -DestinationDirectory $toolchainInputs)
$cli = Join-Path $toolchainInputs 'CopperSharp.Compiler.Cli.dll'
$assembly = Join-Path $managedInputs 'CopperOS.Shell.Dos.NativeRoot.dll'
$sdk = Join-Path $managedInputs 'CopperSharp.Sdk.Amiga.dll'
$sdkSupport = Join-Path $managedInputs 'CopperSharp.Sdk.Amiga.Support.dll'
$compiler = Join-Path $managedInputs 'CopperSharp.Compiler.dll'
$exec = Join-Path $managedInputs 'CopperStart.Exec.dll'
$dos = Join-Path $managedInputs 'CopperStart.Dos.dll'

$entryPoint = 'CopperOS.Shell.Dos.NativeRoot.DosShellNativeRoots::ParkCapabilityRoot'
$exports = @('copperos.shell.execute-park')
$mode = 'park'
if ($IncludeFullExecute) {
    $mode = 'execute'
    $entryPoint = 'CopperOS.Shell.Dos.NativeRoot.DosShellNativeRoots::CapabilityRoot'
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

$artifacts = foreach ($target in $targets) {
    $output = Join-Path $run "shell-$mode-$($target.Cpu).$($target.Extension)"
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
    $compilerOutput = & dotnet @arguments
    $compilerExit = $LASTEXITCODE
    foreach ($line in $compilerOutput) { Write-Host $line }
    if ($compilerExit -ne 0) { throw "Shell native compilation failed for $($target.Cpu)." }

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
        '__c68k_exception_', 'runtime:exception-', 'runtime:method-unwind-',
        'runtime:type-descriptor:System.Exception', '__c68k_gc_', 'runtime:gc-',
        'runtime:root-map-', 'runtime:managed-pool-')
    $hasForbiddenRuntimeSymbol = $false
    foreach ($symbol in $forbiddenRuntimeSymbols) {
        if ($mapText.Contains($symbol)) { $hasForbiddenRuntimeSymbol = $true; break }
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
    $native = $data.NativeCompatibility

    $file = Get-Item -LiteralPath $output
    [ordered]@{
        cpu = $target.Cpu
        format = $target.Format
        artifact = [IO.Path]::GetRelativePath($repo, $output)
        bytes = $file.Length
        sha256 = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash.ToLowerInvariant()
        compatibilityReport = [IO.Path]::GetRelativePath($repo, $report)
        compatibilityReportSha256 = (Get-FileHash -LiteralPath $report -Algorithm SHA256).Hash.ToLowerInvariant()
        map = [IO.Path]::GetRelativePath($repo, $map)
        mapSha256 = (Get-FileHash -LiteralPath $map -Algorithm SHA256).Hash.ToLowerInvariant()
        includedExports = @($data.IncludedExportNames)
        reachableMethods = [int]$data.ReachableMethodCount
        managedAllocationSites = @($data.ManagedAllocationSites).Count
        runtimeFeatureCount = [int]$native.RuntimeFeatureCount
        runtimeHelperCount = [int]$native.RuntimeHelperCount
        externalNativeTargetCount = [int]$native.ExternalNativeTargetCount
        exceptionRegionCount = [int]$native.ExceptionRegionCount
        fatalMachineFaultSiteCount = [int]$native.FatalMachineFaultSiteCount
    }
}

# Detect any modification of the frozen inputs while targets were compiled.
foreach ($inputFile in @($buildInputs) + @($compilerInputs)) {
    $path = Join-Path $repo $inputFile.path
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -cne $inputFile.sha256) {
        throw "Frozen qualification input changed: $path"
    }
}

$receipt = [ordered]@{
    schemaVersion = 2
    suite = "CC02-Shell-native-$mode-isolated"
    status = 'passed'
    head = (git -C $repo rev-parse HEAD).Trim()
    runtimeProfile = 'freestanding'
    entry = $entryPoint
    exports = $exports
    scope = 'Isolated three-target compiler/static compatibility check. 68000 emits HUNK; 68020 and 68040 emit assembler listings. No original OS execution or Shell behavior claim is made.'
    builds = $artifacts
    staticFinding = if (@($artifacts | Where-Object { $_.fatalMachineFaultSiteCount -gt 0 }).Count -ne 0) {
        'Native reports identify fatal machine-fault sites; this is a current static-build baseline, not a release qualification.'
    } else { $null }
    sourceEvidence = [ordered]@{
        scope = 'Selected Shell/Exec/DOS source and project files sampled before and after the managed build. Equal hashes do not prove compiler consumption, exclude intervening changes, or cover generated files and every SDK/compiler source. Native artifacts are bound to the captured DLLs, not asserted to be built from a later source-tree snapshot.'
        buildStartedUtc = $buildStartedUtc
        buildFinishedUtc = $buildFinishedUtc
        selectedFilesUnchangedAcrossBuild = (($sourceBeforeBuild | ConvertTo-Json -Depth 4 -Compress) -ceq
            ($sourceAfterBuild | ConvertTo-Json -Depth 4 -Compress))
        beforeBuild = @($sourceBeforeBuild)
        afterBuild = @($sourceAfterBuild)
    }
    managedInputs = @($buildInputs)
    compilerInputs = @($compilerInputs)
    siblingCopperStartSdkCopiesRemoved = $false
    previousShellNativeArtifactsOverwritten = $false
    guestExecution = $false
    shippingAdmission = $false
}
$receiptPath = Join-Path $run 'qualification.json'
$receipt | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $receiptPath -Encoding utf8NoBOM
Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json | Out-Null
Write-Output $receiptPath
