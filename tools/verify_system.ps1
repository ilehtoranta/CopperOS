[CmdletBinding()]
param(
    [string]$EvidenceDirectory = (Join-Path $PSScriptRoot '../artifacts/normal-full-verification-v1'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../artifacts/system/68000'),
    [string]$Python = 'python'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = [IO.Path]::GetFullPath($OutputDirectory)
$profile = Join-Path $output 'verified-profile.json'
if (Test-Path -LiteralPath $profile) { Remove-Item -LiteralPath $profile -Force }
$evidence = (Resolve-Path -LiteralPath $EvidenceDirectory).Path
$plan = Get-Content -LiteralPath (Join-Path $evidence 'plan.json') -Raw | ConvertFrom-Json
$image = Join-Path $output 'System.adf'
if ((Get-FileHash -LiteralPath $image).Hash.ToLowerInvariant() -ne $plan.imageSha256) {
    throw 'This build differs from the runtime-qualified image. Fresh runtime qualification is required.'
}
$report = Join-Path $output 'system-runtime-acceptance.json'
& $Python (Join-Path $repo 'tools/ImageBuilder/audit_normal_system_acceptance.py') --evidence-directory $evidence --output $report
if ($LASTEXITCODE) { throw 'Complete Shell/mathtrans evidence audit failed.' }
$accepted = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
if (-not $accepted.passed -or $accepted.platformSelection -ne 'both' -or $accepted.receipts.Count -ne 53) {
    throw 'Incomplete two-platform acceptance.'
}
$roms = Join-Path $output 'roms'
$null = New-Item -ItemType Directory -Path $roms -Force
foreach ($item in @(
    @{source=$plan.copperstartRom; hash=$plan.copperstartRomSha256; name='CopperStart-DiskBoot.rom'},
    @{source=$plan.extensionRom; hash=$plan.extensionRomSha256; name='CopperStart-DesktopExtension.rom'}
)) {
    if ((Get-FileHash -LiteralPath $item.source).Hash.ToLowerInvariant() -ne $item.hash) { throw 'ROM identity mismatch.' }
    $target = Join-Path $roms $item.name
    [IO.File]::Copy($item.source,$target,$true)
    if ((Get-FileHash -LiteralPath $target).Hash.ToLowerInvariant() -ne $item.hash) { throw 'ROM publication mismatch.' }
}
@{kind='copperos-verified-system-profile'; runtimeQualified=$true; evidenceReuse=$true;
  imageSha256=$plan.imageSha256; copperstartRomSha256=$plan.copperstartRomSha256;
  extensionRomSha256=$plan.extensionRomSha256; kickstartRomSha256=$plan.kickstartRomSha256;
  acceptanceSha256=(Get-FileHash -LiteralPath $report).Hash.ToLowerInvariant()
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'verified-profile.json') -Encoding utf8
Write-Host 'Verified existing complete runtime evidence and published the accepted CopperStart ROM pair. No guest tests were rerun.'
