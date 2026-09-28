param(
    [Parameter(Mandatory)][string]$Runner,
    [Parameter(Mandatory)][string]$Rom,
    [Parameter(Mandatory)][string]$Adf,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [Parameter(Mandatory)][string]$FrozenRunnerDirectory,
    [Parameter(Mandatory)][string]$SourceDirectory,
    [Parameter(Mandatory)][string]$BaselineDirectory,
    [int]$Frames = 2000
)
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$runnerPath = (Resolve-Path -LiteralPath $Runner).Path
$outputPath = (Resolve-Path -LiteralPath $OutputDirectory).Path
$inputPaths = @($Rom, $Adf) + @(
    Get-ChildItem -LiteralPath $FrozenRunnerDirectory -File | Where-Object Extension -In '.dll', '.json', '.pdb' | ForEach-Object FullName
) + @(
    Get-ChildItem -LiteralPath (Split-Path $runnerPath) -File | Where-Object Extension -In '.dll', '.json', '.pdb' | ForEach-Object FullName
) + @(Get-ChildItem -LiteralPath $SourceDirectory -Filter '*.cs' | ForEach-Object FullName)
function Get-Identities($paths) {
    @($paths | Sort-Object -Unique | ForEach-Object {
        $item = Get-Item -LiteralPath $_
        [ordered]@{ path = $item.FullName; bytes = $item.Length; sha256 = (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant() }
    })
}
$before = Get-Identities $inputPaths
$arguments = @($runnerPath, (Resolve-Path -LiteralPath $Rom).Path, (Resolve-Path -LiteralPath $Adf).Path, "$Frames", "$outputPath\snapshots")
$start = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
foreach ($argument in $arguments) { $start.ArgumentList.Add($argument) }
$startedUtc = [DateTime]::UtcNow.ToString('o')
$process = [Diagnostics.Process]::Start($start)
$stdoutTask = $process.StandardOutput.ReadToEndAsync()
$stderrTask = $process.StandardError.ReadToEndAsync()
$process.WaitForExit()
$stdoutTask.Result | Set-Content -LiteralPath "$outputPath\stdout.txt"
$stderrTask.Result | Set-Content -LiteralPath "$outputPath\stderr.txt"
$after = Get-Identities $inputPaths
$baselineFiles = @(Get-ChildItem -LiteralPath "$BaselineDirectory\snapshots" -File | Where-Object Extension -In '.chipram', '.bmp')
$comparisons = @($baselineFiles | ForEach-Object {
    $newPath = Join-Path "$outputPath\snapshots" $_.Name
    $oldHash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $newHash = if (Test-Path -LiteralPath $newPath) { (Get-FileHash -LiteralPath $newPath -Algorithm SHA256).Hash.ToLowerInvariant() } else { $null }
    [ordered]@{ name = $_.Name; baselineSha256 = $oldHash; captureSha256 = $newHash; equal = $oldHash -eq $newHash }
})
$summaryPath = "$outputPath\snapshots\summary.json"
$summary = if (Test-Path -LiteralPath $summaryPath) { Get-Content -LiteralPath $summaryPath -Raw | ConvertFrom-Json } else { $null }
$receipt = [ordered]@{
    schemaVersion = 1
    suite = 'WB31-clean-lightweight-passive-slowram-readiness'
    status = 'bounded-capture-no-boot-or-command-parity-claim'
    startedUtc = $startedUtc
    finishedUtc = [DateTime]::UtcNow.ToString('o')
    process = [ordered]@{ executable = $start.FileName; arguments = $arguments; exitCode = $process.ExitCode }
    profile = 'A500 PAL OCS; 68000; 512 KiB chip + 512 KiB slow; native KS3.1; unchanged write-protected Workbench ADF; no input or warmup'
    observation = 'Public read-only memory/output/state properties on hardware owner thread; no reflection, guest bus reads or host DOS services'
    inputsBefore = $before
    inputsAfter = $after
    inputsUnchanged = ($before | ConvertTo-Json -Depth 5 -Compress) -ceq ($after | ConvertTo-Json -Depth 5 -Compress)
    summary = $summary
    baseline = [ordered]@{
        directory = $BaselineDirectory
        receiptSha256 = (Get-FileHash -LiteralPath "$BaselineDirectory\capture-receipt.json").Hash.ToLowerInvariant()
        expectedCpuFingerprint = 'AC179436D9BA9EAE'
        expectedOutputFingerprint = '5F77038F9388B112'
        expectedCycle = 284204014
        fingerprintsEqual = $summary.cpuFingerprint -eq 'AC179436D9BA9EAE' -and $summary.outputFingerprint -eq '5F77038F9388B112' -and $summary.Cycle -eq 284204014
        snapshotComparisons = $comparisons
        allComparedSnapshotsEqual = @($comparisons | Where-Object { -not $_.equal }).Count -eq 0 -and $comparisons.Count -eq 70
        hardwareFingerprintCompared = $false
    }
}
$receipt | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath "$outputPath\capture-receipt.json"
[pscustomobject]$receipt | Select-Object status, inputsUnchanged, summary | ConvertTo-Json -Depth 5
if ($process.ExitCode -ne 0) { throw "Capture failed with exit $($process.ExitCode)" }
