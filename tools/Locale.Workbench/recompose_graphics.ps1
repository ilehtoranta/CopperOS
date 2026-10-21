param([Parameter(Mandatory)][string]$RomRoot, [Parameter(Mandatory)][string]$Runtime,
      [Parameter(Mandatory)][string]$Output)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $RomRoot).Path
$runtimePath = (Resolve-Path -LiteralPath $Runtime).Path
$out = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $out) { throw 'Use a fresh directory; preserve original manifests.' }
$manifestPath = Join-Path $root 'disk-boot-test-manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
foreach ($item in $manifest.inputs) {
    if ($item.file -ne 'CopperStart.Graphics.dll' -and (Get-FileHash -LiteralPath (Join-Path $runtimePath $item.file)).Hash -cne $item.sha256) {
        throw "The compiled root producer changed: $($item.file)"
    }
}
$compiled = Join-Path $root 'CopperStart-DiskBoot-compiled.rom'
if ((Get-FileHash -LiteralPath $compiled).Hash -cne $manifest.compiledRomSha256) { throw 'Compiled root hash mismatch.' }
New-Item -ItemType Directory -Path $out | Out-Null
Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $out 'parent-manifest.json')
foreach ($name in @('CopperStart-DiskBoot-compiled.rom', 'CopperStart-DiskBoot.map', 'CopperStart-SecondStage.adf')) {
    if (Test-Path -LiteralPath (Join-Path $root $name)) { Copy-Item -LiteralPath (Join-Path $root $name) -Destination $out }
}
$rom = Join-Path $out 'CopperStart-DiskBoot.rom'
Copy-Item -LiteralPath $compiled -Destination $rom
& dotnet exec --runtimeconfig (Join-Path $runtimePath 'CopperStart.Floppy.Native.Tests.runtimeconfig.json') `
    --depsfile (Join-Path $runtimePath 'CopperStart.Floppy.Native.Tests.deps.json') `
    (Join-Path $runtimePath 'CopperStart.Graphics.dll') $rom $manifest.codeBytes
if ($LASTEXITCODE -ne 0) { throw 'Native graphics composition failed.' }
$graphics = Get-Content -LiteralPath ([IO.Path]::ChangeExtension($rom, '.graphics.json')) -Raw | ConvertFrom-Json
$parentSha = $manifest.sha256
$manifest.sha256 = $graphics.sha256
$manifest.graphicsBytes = $graphics.graphicsBytes
$manifest.graphicsEntries = $graphics.Entries
$manifest.graphicsSupportedVectors = $graphics.SupportedVectors
foreach ($item in $manifest.inputs) {
    if ($item.file -eq 'CopperStart.Graphics.dll') { $item.sha256 = $graphics.emitterSha256 }
}
$manifest | Add-Member -NotePropertyName recomposition -NotePropertyValue @{
    kind='new-graphics-over-unchanged-compiled-root'; parentRomSha256=$parentSha
    parentManifestSha256=(Get-FileHash -LiteralPath $manifestPath).Hash
}
$manifest | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath (Join-Path $out 'disk-boot-test-manifest.json')
