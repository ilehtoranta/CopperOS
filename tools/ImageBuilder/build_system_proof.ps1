[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '../../../CopperSharp68k'),
    [string]$MathTransLibrary = (Join-Path $PSScriptRoot '../../filesystem/SYS/Libs/mathtrans.library'),
    [string]$MathTransReceipt = (Join-Path $PSScriptRoot '../../artifacts/mathtrans/68000/mathtrans.library.json'),
    [switch]$WithoutMathTrans
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
    (Join-Path $repo '../CopperStart/Directory.Build.props'))
foreach ($directory in @((Join-Path $repo 'tools/SystemImage.NativeRoot'), (Join-Path $repo 'src/System/Shell'),
    (Join-Path $repo '../CopperStart/src/CopperStart.Exec'), (Join-Path $repo '../CopperStart/src/CopperStart.Dos'),
    (Join-Path $toolchain 'Compiler'), (Join-Path $toolchain 'Compiler.Cli'), (Join-Path $toolchain 'Targets.Amiga'),
    (Join-Path $toolchain 'Sdk.Amiga'), (Join-Path $toolchain 'Sdk.Amiga.Support'), (Join-Path $toolchain 'Runtime.Managed'),
    (Join-Path $toolchain 'Runtime.AmigaPal'))) {
    $sourceFiles += @(Get-ChildItem $directory -Recurse -File | Where-Object {
        $_.Extension -in '.cs','.csproj','.props' -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
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
            'copperos.shell.execute-park','copperos.shell.queue-handler-task','copperstart.dos.process-return')) {
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
    $artifacts += @{ name = $item.Name; path = [IO.Path]::GetFileName($file); bytes = (Get-Item $file).Length;
        sha256 = (Get-FileHash $file).Hash.ToLowerInvariant(); entry = $item.Entry;
        report = [IO.Path]::GetFileName($file + '.compatibility.json'); reportSha256 = (Get-FileHash ($file + '.compatibility.json')).Hash.ToLowerInvariant() }
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
@{ schemaVersion = 1; kind = 'original-system-boot-proof'; version = '0.1.0-dev.1'; shipping = $false; runtimeQualified = $false;
    artifacts = $artifacts;
    inputs = @(Get-ChildItem $frozen -File -Filter '*.dll' | ForEach-Object { @{file=$_.Name; sha256=(Get-FileHash $_.FullName).Hash.ToLowerInvariant()} });
    sources = $sourcesBefore
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $output 'native-manifest.json') -Encoding utf8
