[CmdletBinding()]
param([Parameter(Mandatory)][string]$BaseProofDirectory,
      [Parameter(Mandatory)][string]$CompilerInputs,
      [Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$base = (Resolve-Path $BaseProofDirectory).Path
$compiler = (Resolve-Path $CompilerInputs).Path
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'Use a fresh output directory.' }
New-Item -ItemType Directory -Path $output | Out-Null
$manifestPath = Join-Path $base 'native-manifest.json'
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$baseNames = @($manifest.artifacts | ForEach-Object name)
if ($manifest.kind -ne 'original-system-boot-proof' -or $manifest.shipping -ne $false -or
    @($baseNames | Sort-Object -Unique).Count -ne $baseNames.Count -or
    @('Shell-Seg', 'BootShell', 'Demo', 'mathtrans.library' | Where-Object { $_ -notin $baseNames }).Count -ne 0 -or
    @($baseNames | Where-Object { $_ -notin @('Shell-Seg', 'BootShell', 'Demo', 'mathtrans.library', 'Execute') }).Count -ne 0) {
    throw 'An original, nonshipping base boot proof with mathtrans.library is required.'
}
Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $output 'base-native-manifest.json')
foreach ($artifact in $manifest.artifacts) {
    foreach ($record in @(@{path=$artifact.path;sha=$artifact.sha256},@{path=$artifact.report;sha=$artifact.reportSha256})) {
        if ([IO.Path]::GetFileName($record.path) -cne $record.path -or (Get-FileHash (Join-Path $base $record.path)).Hash.ToLowerInvariant() -cne $record.sha) { throw 'Base native artifact/report changed.' }
        Copy-Item -LiteralPath (Join-Path $base $record.path) -Destination $output
    }
}
$sourceFiles = @(Get-ChildItem (Join-Path $repo 'tools/SystemImage.NativeRoot') -File | Where-Object Extension -in '.cs','.csproj' | ForEach-Object FullName)
$sourceFiles += @($PSCommandPath,(Join-Path $repo 'tools/ImageBuilder/build_system_image.py'))
$snapshotBefore = @($sourceFiles | Sort-Object | ForEach-Object { @{path=[IO.Path]::GetRelativePath($repo,$_);sha256=(Get-FileHash $_).Hash.ToLowerInvariant()} })
$build = Join-Path $output 'build' 
& dotnet build (Join-Path $repo 'tools/SystemImage.NativeRoot/CopperOS.SystemImage.NativeRoot.csproj') -c Release -m:1 --artifacts-path $build
if ($LASTEXITCODE) { throw 'Storage native input build failed.' }
$snapshotAfter = @($sourceFiles | Sort-Object | ForEach-Object { @{path=[IO.Path]::GetRelativePath($repo,$_);sha256=(Get-FileHash $_).Hash.ToLowerInvariant()} })
if (($snapshotBefore | ConvertTo-Json -Compress) -cne ($snapshotAfter | ConvertTo-Json -Compress)) { throw 'Storage producer sources changed during input build; preserve this run and rebuild.' }
$frozen = Join-Path $output 'inputs'
Copy-Item -LiteralPath $compiler -Destination $frozen -Recurse
# The command uses public Exec/DOS vectors. Preserve the qualified SDK/toolchain
# and bring in only the command root; unrelated producer roots are not linked.
Copy-Item -LiteralPath (Join-Path $build 'bin/CopperOS.SystemImage.NativeRoot/release/CopperOS.SystemImage.NativeRoot.dll') -Destination $frozen -Force
$file = Join-Path $output 'Storage.hunk'
$arguments = @((Join-Path $frozen 'CopperSharp.Compiler.Cli.dll'),(Join-Path $frozen 'CopperOS.SystemImage.NativeRoot.dll'),
 '--entry','CopperOS.SystemImage.StorageProbe::Entry','--platform','amiga','--cpu','68000','--clr','always','--exceptions','yolo','--fpu','disabled',
    '--format','hunk','--symbols','off','--runtime','freestanding','--memory','none','--peephole','fixed-point','--code-size-optimizations','off',
 '--include-export','copperos.system.storage-proof','--output',$file,'--compatibility-report',($file+'.compatibility.json'))
foreach ($name in @('CopperSharp.Sdk.Amiga','CopperSharp.Sdk.Amiga.Support','CopperSharp.Compiler','CopperStart.Exec','CopperStart.Dos')) { $arguments += @('--managed-assembly',(Join-Path $frozen ($name+'.dll'))) }
& dotnet @arguments 2>&1 | Tee-Object -FilePath ($file+'.log') | Out-Host
if ($LASTEXITCODE) { throw 'Storage command compilation failed.' }
if (@(Select-String -LiteralPath ($file + '.map') -Pattern '^ENTRY 00000000$').Count -ne 1) {
    throw 'Storage HUNK must enter at CODE offset zero.'
}
$report = Get-Content ($file+'.compatibility.json') -Raw | ConvertFrom-Json
$n = $report.NativeCompatibility
if (-not $report.IsCompatible -or @($report.ManagedAllocationSites).Count -or $n.ExceptionRegionCount -or $n.FatalMachineFaultSiteCount -or @($n.RuntimeFeatures).Count -or @($n.RuntimeHelpers).Count -or @($n.ExternalNativeTargets).Count) { throw 'Storage command native gates failed.' }
@{schemaVersion=1;kind='original-system-boot-proof';version='0.2.0-dev.1';shipping=$false;runtimeQualified=$false;
 baseManifestSha256=(Get-FileHash $manifestPath).Hash.ToLowerInvariant();
 artifacts=@($manifest.artifacts)+@(@{name='Storage';path='Storage.hunk';bytes=(Get-Item $file).Length;sha256=(Get-FileHash $file).Hash.ToLowerInvariant();entry='CopperOS.SystemImage.StorageProbe::Entry';report='Storage.hunk.compatibility.json';reportSha256=(Get-FileHash ($file+'.compatibility.json')).Hash.ToLowerInvariant()});
 storageInputs=@(Get-ChildItem $frozen -File -Filter '*.dll' | ForEach-Object { @{file=$_.Name;sha256=(Get-FileHash $_.FullName).Hash.ToLowerInvariant()} });
 sources=$snapshotBefore
} | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $output 'native-manifest.json') -Encoding utf8
