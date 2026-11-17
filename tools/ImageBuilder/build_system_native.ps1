[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '../../../CopperSharp68k'),
    [string]$MathTransLibrary = (Join-Path $PSScriptRoot '../../filesystem/SYS/Libs/mathtrans.library'),
    [string]$MathTransReceipt = (Join-Path $PSScriptRoot '../../artifacts/mathtrans/68000/mathtrans.library.json'),
    [string]$Python = 'python',
    [switch]$WithoutMathTrans,
    [ValidateSet('system', 'proof')][string]$BuildKind = 'system'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$toolchain = (Resolve-Path $CopperSharpRoot).Path
$output = [IO.Path]::GetFullPath($OutputDirectory)
if ((Test-Path -LiteralPath $output) -and @(Get-ChildItem -LiteralPath $output -Force).Count) { throw 'Use a fresh proof output directory.' }
$null = New-Item -ItemType Directory -Path $output -Force
if (-not $WithoutMathTrans) {
$libraryPath = (Resolve-Path -LiteralPath $MathTransLibrary).Path
$libraryReceiptPath = (Resolve-Path -LiteralPath $MathTransReceipt).Path
$libraryHash = (Get-FileHash -LiteralPath $libraryPath -Algorithm SHA256).Hash.ToLowerInvariant()
$libraryReceiptHash = (Get-FileHash -LiteralPath $libraryReceiptPath -Algorithm SHA256).Hash.ToLowerInvariant()
$libraryReceipt = Get-Content -LiteralPath $libraryReceiptPath -Raw | ConvertFrom-Json
if ($libraryReceipt.Cpu -ne '68000' -or $libraryReceipt.Name -ne 'mathtrans.library' -or
    $libraryReceipt.Sha256.ToLowerInvariant() -cne $libraryHash -or $libraryReceipt.ImageBytes -ne (Get-Item -LiteralPath $libraryPath).Length) {
    throw 'Staged mathtrans library does not match the 68000 native build receipt; run tools/build_system.ps1.'
}
Copy-Item -LiteralPath $libraryPath -Destination (Join-Path $output 'mathtrans.library')
Copy-Item -LiteralPath $libraryReceiptPath -Destination (Join-Path $output 'mathtrans.library.json')
}
$build = Join-Path $output 'build'
$rootProject = Join-Path $repo 'tools/SystemImage.NativeRoot/CopperOS.SystemImage.NativeRoot.csproj'
$sourceFiles = @($rootProject, $PSCommandPath, (Join-Path $repo 'CopperOS.Portable.props'),
    (Join-Path $PSScriptRoot 'wrap_shell_stack.py'),
    (Join-Path $PSScriptRoot 'build_system_image.py'),
    (Join-Path $PSScriptRoot 'stage_system_image.py'),
    (Join-Path $PSScriptRoot 'audit_shared_shell.py'),
    (Join-Path $repo 'tools/build_system.ps1'),
    (Join-Path $repo '../CopperStart/Directory.Build.props'),
    (Join-Path $repo 'src/Commands/Directory.Build.props'), (Join-Path $repo 'src/Commands/Directory.Build.targets'),
    (Join-Path $repo 'src/Commands/CodeSizePolicies.props'), (Join-Path $repo 'src/Commands/CommandVersions.props'),
    (Join-Path $repo 'tools/Commands/append_hunk_version.py'))
foreach ($name in @('Directory.Build.props', 'Directory.Build.targets',
    'Directory.Packages.props', 'global.json', 'NuGet.Config')) {
    $metadata = Join-Path $toolchain $name
    if (Test-Path -LiteralPath $metadata) { $sourceFiles += $metadata }
}
foreach ($directory in @((Join-Path $repo 'tools/SystemImage.NativeRoot'), (Join-Path $repo 'src/System/Shell'),
    (Join-Path $repo 'src/Commands/Common'), (Join-Path $repo 'src/Commands/Execute'), (Join-Path $repo 'src/Commands/List'),
    (Join-Path $repo '../CopperStart/src/CopperStart.Exec'), (Join-Path $repo '../CopperStart/src/CopperStart.Dos'),
    (Join-Path $toolchain 'Compiler'), (Join-Path $toolchain 'Compiler.Cli'), (Join-Path $toolchain 'Targets.Amiga'),
    (Join-Path $toolchain 'Sdk'), (Join-Path $toolchain 'Sdk.Amiga'), (Join-Path $toolchain 'Sdk.Amiga.Support'), (Join-Path $toolchain 'Runtime.Managed'),
    (Join-Path $toolchain 'Runtime.AmigaPal'))) {
    $sourceFiles += @(Get-ChildItem $directory -Recurse -File | Where-Object {
        $_.Extension -in '.cs','.csproj','.props','.targets','.json' -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
    } | ForEach-Object FullName)
}
function SourceEvidence {
    @($sourceFiles | Sort-Object -Unique | ForEach-Object {
        @{path=[IO.Path]::GetRelativePath($repo,$_); sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}
    })
}
$sourcesBefore = SourceEvidence
& dotnet build (Join-Path $toolchain 'Compiler.Cli/CopperSharp.Compiler.Cli.csproj') -c Release -m:1 --artifacts-path $build -p:SkipCopperScreenHeadlessProjectReference=true
if ($LASTEXITCODE) { throw 'Compiler input build failed.' }
& dotnet build $rootProject -c Release -m:1 --artifacts-path $build "-p:CopperSharp68kRoot=$toolchain"
if ($LASTEXITCODE) { throw 'System proof input build failed.' }
$sourcesAfter = SourceEvidence
if (($sourcesBefore | ConvertTo-Json -Depth 5 -Compress) -cne ($sourcesAfter | ConvertTo-Json -Depth 5 -Compress)) {
    throw 'Producer/toolchain sources changed during input build; preserve this run and rebuild in a new directory.'
}
$frozen = Join-Path $output 'inputs'
$null = New-Item -ItemType Directory -Path $frozen
Copy-Item -Path (Join-Path $build 'bin/CopperSharp.Compiler.Cli/release/*') -Destination $frozen -Recurse
foreach ($file in Get-ChildItem (Join-Path $build 'bin/CopperOS.SystemImage.NativeRoot/release') -Filter '*.dll') {
    $destination = Join-Path $frozen $file.Name
    if ((Test-Path -LiteralPath $destination) -and (Get-FileHash $destination).Hash -ne (Get-FileHash $file.FullName).Hash) {
        throw "Incoherent compiler/SDK input: $($file.Name)"
    }
    Copy-Item -LiteralPath $file.FullName -Destination $frozen -Force
}
$artifacts = @()
$executeStage = Join-Path $output 'execute-stage'
$null = New-Item -ItemType Directory -Path $executeStage
$executeRaw = Join-Path $output 'Execute.raw'
& dotnet publish (Join-Path $repo 'src/Commands/Execute/CopperOS.Commands.Execute.csproj') -c Release -m:1 `
    --artifacts-path (Join-Path $output 'execute-build') "-p:CopperSharp68kRoot=$toolchain" `
    "-p:CopperOSCompilerAssembly=$(Join-Path $frozen 'CopperSharp.Compiler.Cli.dll')" `
    "-p:CopperOSCDirectory=$($executeStage.TrimEnd('\'))\" "-p:CopperOSPythonExecutable=$Python" `
    "-p:CopperSharpOutputPath=$executeRaw"
if ($LASTEXITCODE) { throw 'Execute proof command build failed.' }
$executeReportPath = $executeRaw + '.framework.json'
$executeReport = Get-Content -LiteralPath $executeReportPath -Raw | ConvertFrom-Json
$executeNative = $executeReport.NativeCompatibility
if (-not $executeReport.IsCompatible -or $executeReport.RuntimeProfile -ne 'resident' -or $executeReport.Cpu -ne 'm68000' -or
    @($executeReport.ManagedAllocationSites).Count -ne 0 -or $executeNative.ExceptionRegionCount -ne 0 -or
    $executeNative.FatalMachineFaultSiteCount -ne 0 -or @($executeNative.RuntimeFeatures).Count -ne 0 -or
    @($executeNative.RuntimeHelpers).Count -ne 0 -or @($executeNative.ExternalNativeTargets).Count -ne 0) {
    throw 'Execute requires unsupported native runtime services.'
}
Copy-Item -LiteralPath (Join-Path $executeStage 'Execute') -Destination (Join-Path $output 'Execute.hunk')
Copy-Item -LiteralPath $executeReportPath -Destination (Join-Path $output 'Execute.hunk.compatibility.json')
if (($sourcesBefore | ConvertTo-Json -Depth 5 -Compress) -cne ((SourceEvidence) | ConvertTo-Json -Depth 5 -Compress)) {
    throw 'Producer/toolchain sources changed during Execute build; rebuild in a new directory.'
}
$artifacts += @{ name = 'Execute'; path = 'Execute.hunk'; bytes = (Get-Item (Join-Path $output 'Execute.hunk')).Length;
    sha256 = (Get-FileHash (Join-Path $output 'Execute.hunk')).Hash.ToLowerInvariant(); entry = 'CopperOS.Commands.Entries.ExecuteEntry::Main';
    report = 'Execute.hunk.compatibility.json'; reportSha256 = (Get-FileHash (Join-Path $output 'Execute.hunk.compatibility.json')).Hash.ToLowerInvariant() }
$listStage = Join-Path $output 'list-stage'
$null = New-Item -ItemType Directory -Path $listStage
$listRaw = Join-Path $output 'List.raw'
& dotnet publish (Join-Path $repo 'src/Commands/List/CopperOS.Commands.List.csproj') -c Release -m:1 `
    --artifacts-path (Join-Path $output 'list-build') "-p:CopperSharp68kRoot=$toolchain" `
    "-p:CopperOSCompilerAssembly=$(Join-Path $frozen 'CopperSharp.Compiler.Cli.dll')" `
    "-p:CopperOSCDirectory=$($listStage.TrimEnd('\'))\" "-p:CopperOSPythonExecutable=$Python" `
    "-p:CopperSharpOutputPath=$listRaw"
if ($LASTEXITCODE) { throw 'List proof command build failed.' }
$listReportPath = $listRaw + '.framework.json'
$listReport = Get-Content -LiteralPath $listReportPath -Raw | ConvertFrom-Json
$listNative = $listReport.NativeCompatibility
if (-not $listReport.IsCompatible -or $listReport.RuntimeProfile -ne 'resident' -or $listReport.Cpu -ne 'm68000' -or `
    @($listReport.ManagedAllocationSites).Count -ne 0 -or $listNative.ExceptionRegionCount -ne 0 -or `
    $listNative.FatalMachineFaultSiteCount -ne 0 -or @($listNative.RuntimeFeatures).Count -ne 0 -or `
    @($listNative.RuntimeHelpers).Count -ne 0 -or @($listNative.ExternalNativeTargets).Count -ne 0) {
    throw 'List requires unsupported native runtime services.'
}
Copy-Item -LiteralPath (Join-Path $listStage 'List') -Destination (Join-Path $output 'List.hunk')
Copy-Item -LiteralPath $listReportPath -Destination (Join-Path $output 'List.hunk.compatibility.json')
if (($sourcesBefore | ConvertTo-Json -Depth 5 -Compress) -cne ((SourceEvidence) | ConvertTo-Json -Depth 5 -Compress)) {
    throw 'Producer/toolchain sources changed during List build; rebuild in a new directory.'
}
$artifacts += @{ name = 'List'; path = 'List.hunk'; bytes = (Get-Item (Join-Path $output 'List.hunk')).Length;
    sha256 = (Get-FileHash (Join-Path $output 'List.hunk')).Hash.ToLowerInvariant(); entry = 'CopperOS.Commands.Entries.ListEntry::Main';
    report = 'List.hunk.compatibility.json'; reportSha256 = (Get-FileHash (Join-Path $output 'List.hunk.compatibility.json')).Hash.ToLowerInvariant() }
foreach ($item in @(
    @{ Name = 'Shell-Seg'; Entry = 'CopperOS.SystemImage.SystemBootstrap::ShellEntry'; Shell = $true },
    @{ Name = 'BootShell'; Entry = 'CopperOS.SystemImage.SystemBootstrap::Boot'; Shell = $false; Export = 'copperos.system.bootstrap' },
    @{ Name = 'Demo'; Entry = 'CopperOS.SystemImage.SystemBootstrap::Demonstration'; Shell = $false; Export = 'copperos.system.demo' }
)) {
    $file = Join-Path $output ($item.Name + '.hunk')
    # The current size optimizer can move small program entry labels away from
    # offset zero. Amiga HUNK commands enter the first CODE byte. Keep the large
    # Shell's size optimizations, but reject any nonzero entry for every output.
    $sizeOptimizations = if ($item.Shell) { 'on' } else { 'off' }
    $arguments = @((Join-Path $frozen 'CopperSharp.Compiler.Cli.dll'), (Join-Path $frozen 'CopperOS.SystemImage.NativeRoot.dll'),
        '--entry', $item.Entry, '--platform', 'amiga', '--cpu', '68000', '--clr', 'always', '--exceptions', 'yolo', '--fpu', 'disabled',
        '--format', 'hunk', '--symbols', 'off', '--runtime', 'freestanding', '--memory', 'none', '--peephole', 'fixed-point', '--code-size-optimizations', $sizeOptimizations,
        '--output', $file, '--compatibility-report', ($file + '.compatibility.json'))
    foreach ($name in @('CopperSharp.Sdk.Amiga', 'CopperSharp.Sdk.Amiga.Support', 'CopperSharp.Compiler', 'CopperStart.Exec', 'CopperStart.Dos')) {
        $arguments += @('--managed-assembly', (Join-Path $frozen ($name + '.dll')))
    }
    if ($item.Shell) {
        foreach ($symbol in @('copperos.system.shell-entry','copperos.shell.entry','copperos.shell.child','copperos.shell.execute-begin','copperos.shell.execute-poll',
            'copperos.shell.execute-park','copperos.shell.background','copperos.shell.new-process')) {
            $arguments += @('--include-export', $symbol)
        }
    } else {
        # An empty allowlist roots every native export in the input assembly.
        # Bootstrap/demo must not accidentally include the entire Shell.
        $arguments += @('--include-export', $item.Export)
    }
    & dotnet @arguments 2>&1 | Tee-Object -FilePath ($file + '.log') | Out-Host
    if ($LASTEXITCODE) { throw "Native build failed: $($item.Name)" }
    if (@(Select-String -LiteralPath ($file + '.map') -Pattern '^ENTRY 00000000$').Count -ne 1) {
        throw "HUNK must enter at CODE offset zero: $($item.Name)"
    }
    $report = Get-Content -LiteralPath ($file + '.compatibility.json') -Raw | ConvertFrom-Json
    $native = $report.NativeCompatibility
    if (-not $report.IsCompatible -or $report.RuntimeProfile -ne 'freestanding' -or $report.Cpu -ne 'm68000' -or
        @($report.ManagedAllocationSites).Count -ne 0 -or $native.ExceptionRegionCount -ne 0 -or $native.FatalMachineFaultSiteCount -ne 0 -or
        @($native.RuntimeFeatures).Count -ne 0 -or @($native.RuntimeHelpers).Count -ne 0 -or @($native.ExternalNativeTargets).Count -ne 0) {
        throw "Native runtime/static gate failed: $($item.Name)"
    }
    $stackGate = $null
    if ($item.Shell) {
        # A UserShell may start with only a few KiB of stack. Prefix the
        # compiler output with a public Exec StackSwap gate before packaging.
        $raw = Join-Path $output 'Shell-Seg.unwrapped.hunk'
        Copy-Item -LiteralPath $file -Destination $raw
        & $Python (Join-Path $PSScriptRoot 'wrap_shell_stack.py') $raw $file
        if ($LASTEXITCODE) { throw 'Shell stack gate packaging failed.' }
        $stackGate = @{ input = [IO.Path]::GetFileName($raw);
            inputSha256 = (Get-FileHash -LiteralPath $raw).Hash.ToLowerInvariant();
            packager = 'wrap_shell_stack.py';
            packagerSha256 = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'wrap_shell_stack.py')).Hash.ToLowerInvariant() }
    }
    $artifact = @{ name = $item.Name; path = [IO.Path]::GetFileName($file); bytes = (Get-Item $file).Length;
        sha256 = (Get-FileHash $file).Hash.ToLowerInvariant(); entry = $item.Entry;
        report = [IO.Path]::GetFileName($file + '.compatibility.json'); reportSha256 = (Get-FileHash ($file + '.compatibility.json')).Hash.ToLowerInvariant() }
    if ($null -ne $stackGate) { $artifact.stackGate = $stackGate }
    $artifacts += $artifact
}
if (-not $WithoutMathTrans) {
if ((Get-FileHash -LiteralPath $libraryPath -Algorithm SHA256).Hash.ToLowerInvariant() -cne $libraryHash -or
    (Get-FileHash -LiteralPath $libraryReceiptPath -Algorithm SHA256).Hash.ToLowerInvariant() -cne $libraryReceiptHash -or
    (Get-FileHash -LiteralPath (Join-Path $output 'mathtrans.library') -Algorithm SHA256).Hash.ToLowerInvariant() -cne $libraryHash -or
    (Get-FileHash -LiteralPath (Join-Path $output 'mathtrans.library.json') -Algorithm SHA256).Hash.ToLowerInvariant() -cne $libraryReceiptHash) {
    throw 'Staged mathtrans library or receipt changed during proof build.'
}
$artifacts += @{ name = 'mathtrans.library'; path = 'mathtrans.library'; bytes = (Get-Item -LiteralPath $libraryPath).Length;
    sha256 = $libraryHash; entry = 'Exec Resident/AUTOINIT'; report = 'mathtrans.library.json'; reportSha256 = $libraryReceiptHash }
}
$manifestKind = if ($BuildKind -eq 'proof') { 'original-system-boot-proof' } else { 'copperos-system-native' }
@{ schemaVersion = 1; kind = $manifestKind; version = '0.1.0-dev.1'; shipping = $false; runtimeQualified = $false;
    artifacts = $artifacts;
    inputs = @(Get-ChildItem $frozen -File -Filter '*.dll' | ForEach-Object { @{file=$_.Name; sha256=(Get-FileHash $_.FullName).Hash.ToLowerInvariant()} });
    sources = $sourcesBefore
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $output 'native-manifest.json') -Encoding utf8
