[CmdletBinding()]
param(
    [string]$SystemRoot = (Join-Path $PSScriptRoot '../filesystem/SYS'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../artifacts/system/68000'),
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '../../CopperSharp68k'),
    [string]$Python = 'python',
    [switch]$Verify,
    [string]$VerificationDirectory = (Join-Path $PSScriptRoot '../artifacts/normal-full-verification-v1')
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$system = [IO.Path]::GetFullPath($SystemRoot)
$output = [IO.Path]::GetFullPath($OutputDirectory)

$toolchain = (Resolve-Path -LiteralPath $CopperSharpRoot).Path
$null = New-Item -ItemType Directory -Path $output -Force
# Preserve every native run. Publish the normal image after native gates and
# staging pass; neither proofs nor normal builds import loose staged commands.
$run = Join-Path $output ('build-' + [Guid]::NewGuid().ToString('N'))
$snapshot = Join-Path $run 'source'
& $Python (Join-Path $repo 'tools/ImageBuilder/snapshot_system_sources.py') --repo $repo --toolchain $toolchain --output $snapshot
if ($LASTEXITCODE) { throw 'System source snapshot failed.' }
$producer = Join-Path $snapshot 'CopperOS'
$toolchain = Join-Path $snapshot 'CopperSharp68k'
$math = Join-Path $run 'mathtrans'
& (Join-Path $producer 'tools/MathTrans.LibraryBuilder/build_mathtrans.ps1') -Cpu 68000 -OutputDirectory $math -CopperSharpRoot $toolchain
if ($LASTEXITCODE) { throw 'mathtrans native build failed.' }
$native = Join-Path $run 'native'
& (Join-Path $producer 'tools/ImageBuilder/build_system_native.ps1') -OutputDirectory $native -CopperSharpRoot $toolchain `
    -MathTransLibrary (Join-Path $math 'mathtrans.library') -MathTransReceipt (Join-Path $math 'mathtrans.library.json') -Python $Python
$manifest = Join-Path $native 'native-manifest.json'
$image = Join-Path $native 'System.adf'
$receipt = Join-Path $native 'system-image.json'
& $Python (Join-Path $producer 'tools/ImageBuilder/build_system_image.py') --manifest $manifest --output $image --receipt $receipt
if ($LASTEXITCODE) { throw 'System image composition failed.' }
& $Python (Join-Path $producer 'tools/ImageBuilder/audit_shared_shell.py') --manifest $manifest --image $image `
    --output (Join-Path $native 'public-interface-audit.json')
if ($LASTEXITCODE) { throw 'Shared Shell public-interface/source/native audit failed.' }
$staging = Join-Path $native 'system-staging.json'
& $Python (Join-Path $producer 'tools/ImageBuilder/stage_system_image.py') --image $image --receipt $receipt --system-root $system --output $staging
if ($LASTEXITCODE) { throw 'System staging verification failed.' }
foreach ($name in @('System.adf', 'system-image.json', 'system-staging.json')) {
    $source = Join-Path $native $name
    $temporary = Join-Path $output ('.' + [Guid]::NewGuid().ToString('N') + '.tmp')
    try {
        [IO.File]::Copy($source, $temporary, $false)
        if ((Get-FileHash -LiteralPath $temporary).Hash -ne (Get-FileHash -LiteralPath $source).Hash) { throw 'System output copy changed.' }
        [IO.File]::Move($temporary, (Join-Path $output $name), $true)
    } finally {
        if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force }
    }
}
@{kind='copperos-system-build';cpu='68000';shipping=$false;runtimeQualified=$false;
  sourceSnapshot=[IO.Path]::GetRelativePath($output,(Join-Path $snapshot 'snapshot.json'));
  sourceSnapshotSha256=(Get-FileHash -LiteralPath (Join-Path $snapshot 'snapshot.json')).Hash.ToLowerInvariant();
  nativeManifest=[IO.Path]::GetRelativePath($output,$manifest);
  nativeManifestSha256=(Get-FileHash -LiteralPath $manifest).Hash.ToLowerInvariant();
  image='System.adf';imageSha256=(Get-FileHash -LiteralPath $image).Hash.ToLowerInvariant();
  stagingReceiptSha256=(Get-FileHash -LiteralPath $staging).Hash.ToLowerInvariant()
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'system-build.json') -Encoding utf8
Write-Host "Built $output/System.adf and staged shared 68000 Shell/system files in $system."
if ($Verify) {
    & (Join-Path $PSScriptRoot 'verify_system.ps1') -EvidenceDirectory $VerificationDirectory -OutputDirectory $output -Python $Python
}
