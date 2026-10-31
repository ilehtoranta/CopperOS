[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$OutputDirectory,
    [Parameter(Mandatory)][string]$Python,
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '../../../CopperSharp68k'),
    [string]$BinaryVersion,
    [string]$BinaryDate,
    [string]$SystemBinaryVersion = '1.0',
    [string]$SystemBinaryDate = '31.10.2026'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$compilerRoot = (Resolve-Path -LiteralPath $CopperSharpRoot).Path
$copperStartRoot = (Resolve-Path (Join-Path $repo '../CopperStart')).Path
$pythonPath = (Resolve-Path -LiteralPath $Python).Path
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'Choose a fresh output directory.' }

$sources = @(
    $PSCommandPath,
    (Join-Path $repo 'CopperOS.Portable.props'),
    (Join-Path $repo 'tools/Commands/build-c.ps1'),
    (Join-Path $repo 'tools/Commands/append_hunk_version.py'),
    (Join-Path $repo 'tools/ImageBuilder/build_system_proof.ps1'),
    (Join-Path $repo 'tools/ImageBuilder/build_kickstart31_candidate.py'),
    (Join-Path $repo 'tools/ImageBuilder/build_system_image.py'),
    (Join-Path $repo 'tools/DiskBuilder/verify_image.py'),
    (Join-Path $repo 'tools/Commands/Inventory/inventory.py')
)
foreach ($directory in @('src/Commands', 'src/System/Shell', 'tools/SystemImage.NativeRoot')) {
    $sources += @(Get-ChildItem -LiteralPath (Join-Path $repo $directory) -Recurse -File | Where-Object {
        $_.Extension -in '.cs', '.csproj', '.props', '.targets' -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
    } | ForEach-Object FullName)
}
foreach ($directory in @('Compiler', 'Compiler.Cli', 'Sdk.Amiga', 'Sdk.Amiga.Support', 'Targets.Amiga', 'Runtime.Managed', 'Runtime.AmigaPal', 'Sdk/Sdk')) {
    $path = Join-Path $compilerRoot $directory
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing compiler input directory: $path" }
    $sources += @(Get-ChildItem -LiteralPath $path -Recurse -File | Where-Object {
        $_.Extension -in '.cs', '.csproj', '.props', '.targets' -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
    } | ForEach-Object FullName)
}
foreach ($directory in @('src/CopperStart.Exec', 'src/CopperStart.Dos', 'external/CopperSFS/src/CopperSFS.Core')) {
    $path = Join-Path $copperStartRoot $directory
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing Shell dependency directory: $path" }
    $sources += @(Get-ChildItem -LiteralPath $path -Recurse -File | Where-Object {
        $_.Extension -in '.cs', '.csproj', '.props', '.targets' -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
    } | ForEach-Object FullName)
}
foreach ($root in @($repo, $compilerRoot, $copperStartRoot)) {
    $sources += @(Get-ChildItem -LiteralPath $root -File | Where-Object {
        $_.Name -in 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'global.json', 'NuGet.Config'
    } | ForEach-Object FullName)
}
function Get-SourceSnapshot {
    @($sources | Sort-Object -Unique | ForEach-Object {
        @{ path = $_; sha256 = (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant() }
    })
}
$before = Get-SourceSnapshot
$repoRevision = (& git -C $repo rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot identify CopperOS revision.' }
$compilerRevision = (& git -C $compilerRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot identify CopperSharp revision.' }
$copperStartRevision = (& git -C $copperStartRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot identify CopperStart revision.' }
New-Item -ItemType Directory -Path $output | Out-Null
$commands = Join-Path $output 'C'
$shell = Join-Path $output 'shell-build'
$commandOptions = @{ OutputDirectory = $commands; CopperSharpRoot = $compilerRoot; PythonExecutable = $pythonPath }
if ($BinaryVersion) { $commandOptions.BinaryVersion = $BinaryVersion }
if ($BinaryDate) { $commandOptions.BinaryDate = $BinaryDate }
& (Join-Path $repo 'tools/Commands/build-c.ps1') @commandOptions
& (Join-Path $repo 'tools/ImageBuilder/build_system_proof.ps1') -OutputDirectory $shell -CopperSharpRoot $compilerRoot -WithoutMathTrans
$image = Join-Path $output 'CopperOS.hdf'
$receipt = Join-Path $output 'image.json'
& $pythonPath (Join-Path $repo 'tools/ImageBuilder/build_kickstart31_candidate.py') `
    --shell (Join-Path $shell 'Shell-Seg.hunk') --bootstrap (Join-Path $shell 'BootShell.hunk') `
    --commands $commands --binary-version $SystemBinaryVersion --binary-date $SystemBinaryDate --output $image --receipt $receipt
if ($LASTEXITCODE -ne 0) { throw 'HDF image composition or verification failed.' }
$after = Get-SourceSnapshot
if (($before | ConvertTo-Json -Depth 4 -Compress) -cne ($after | ConvertTo-Json -Depth 4 -Compress)) {
    throw 'A build input changed during the build; preserve this unqualified output and build again.'
}
if ((& git -C $repo rev-parse HEAD).Trim() -cne $repoRevision -or
    (& git -C $compilerRoot rev-parse HEAD).Trim() -cne $compilerRevision -or
    (& git -C $copperStartRoot rev-parse HEAD).Trim() -cne $copperStartRevision) {
    throw 'A producer revision changed during the build; preserve this unqualified output and build again.'
}
$result = @{
    kind = 'kickstart31-command-hdf-development-build'; shipping = $false; runtimeQualified = $false
    copperOSRevision = $repoRevision; copperSharpRevision = $compilerRevision; copperStartRevision = $copperStartRevision
    commandVersionOverride = $BinaryVersion; commandDateOverride = $BinaryDate
    systemBinaryVersion = $SystemBinaryVersion; systemBinaryDate = $SystemBinaryDate; cpu = '68000'
    dotnetVersion = (& dotnet --version).Trim()
    pythonVersion = (& $pythonPath --version).Trim()
    pythonPath = $pythonPath; pythonSha256 = (Get-FileHash -LiteralPath $pythonPath -Algorithm SHA256).Hash.ToLowerInvariant()
    compilerSha256 = (Get-FileHash -LiteralPath (Join-Path $compilerRoot 'Compiler.Cli/bin/Release/net10.0/CopperSharp.Compiler.Cli.dll') -Algorithm SHA256).Hash.ToLowerInvariant()
    sdkSha256 = (Get-FileHash -LiteralPath (Join-Path $compilerRoot 'Sdk.Amiga/bin/Release/net10.0/CopperSharp.Sdk.Amiga.dll') -Algorithm SHA256).Hash.ToLowerInvariant()
    sdkSupportSha256 = (Get-FileHash -LiteralPath (Join-Path $compilerRoot 'Sdk.Amiga.Support/bin/Release/net10.0/CopperSharp.Sdk.Amiga.Support.dll') -Algorithm SHA256).Hash.ToLowerInvariant()
    shellManifestSha256 = (Get-FileHash -LiteralPath (Join-Path $shell 'native-manifest.json') -Algorithm SHA256).Hash.ToLowerInvariant()
    imageSha256 = (Get-FileHash -LiteralPath $image -Algorithm SHA256).Hash.ToLowerInvariant()
    imageReceiptSha256 = (Get-FileHash -LiteralPath $receipt -Algorithm SHA256).Hash.ToLowerInvariant()
    sourceInputs = $before
}
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $output 'build.json') -Encoding utf8
Write-Output "Development HDF built at $image; no runtime qualification is claimed."
