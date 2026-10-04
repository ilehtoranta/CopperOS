<# Refresh out/C only from a complete, qualified and hash-checked staging build. #>
param(
    [Parameter(Mandatory)][string]$Stage,
    [Parameter(Mandatory)][string]$Qualification,
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$stageRoot = (Resolve-Path -LiteralPath $Stage).Path
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repo 'out/C' }
$target = [IO.Path]::GetFullPath($OutputDirectory)
$report = Get-Content -LiteralPath $Qualification -Raw | ConvertFrom-Json
$projects = Get-ChildItem (Join-Path $repo 'src/Commands') -Directory |
    ForEach-Object { Get-ChildItem $_.FullName -Filter '*.csproj' -File } |
    ForEach-Object { if ((Get-Content -LiteralPath $_.FullName -Raw) -match '<CopperOSCommandName>([^<]+)</CopperOSCommandName>') { $Matches[1] } }
$names = @($report.commands | ForEach-Object command)
if (@(Compare-Object ($projects | Sort-Object) ($names | Sort-Object)).Count -ne 0 -or
    @($names | Sort-Object -Unique).Count -ne $names.Count) { throw 'Report is not a complete command set.' }
$images = Join-Path $stageRoot 'C'
foreach ($row in $report.commands) {
    $image = Join-Path $images $row.command
    if ((Get-Item -LiteralPath $image).Length -ne $row.afterBytes -or
        (Get-FileHash -LiteralPath $image -Algorithm SHA256).Hash.ToLowerInvariant() -ne $row.sha256) {
        throw "Staged image differs from qualification: $($row.command)"
    }
    if ($row.afterBytes -ne $row.beforeBytes -and (-not $row.qualified -or $row.afterBytes -ge $row.beforeBytes)) {
        throw "Unqualified or larger replacement: $($row.command)"
    }
}
if ($report.afterBytes -ge $report.beforeBytes) { throw 'Aggregate is not smaller than the baseline.' }
New-Item -ItemType Directory -Path $target -Force | Out-Null
$backup = Join-Path $stageRoot 'previous-C'
if (Test-Path -LiteralPath $backup) { throw 'Output backup already exists; use a fresh publication stage.' }
New-Item -ItemType Directory -Path $backup | Out-Null
Get-ChildItem -LiteralPath $target -Force -File | Copy-Item -Destination $backup
foreach ($name in $names) { Copy-Item -LiteralPath (Join-Path $images $name) -Destination (Join-Path $target $name) -Force }
foreach ($row in $report.commands) {
    if ((Get-FileHash -LiteralPath (Join-Path $target $row.command) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $row.sha256) {
        throw "Published hash differs: $($row.command); original files are in $backup"
    }
}
# No recursive deletion. Prune exact stale files only after every image is verified.
$stale = @(Get-ChildItem -LiteralPath $target -Force -File | Where-Object Name -NotIn $names)
$stale | Remove-Item -Force
Write-Output "Published $($names.Count) commands, $($report.afterBytes) bytes. Backup: $backup"
if ($stale.Count) { Write-Output "Removed stale files: $($stale.Name -join ', ')" }
