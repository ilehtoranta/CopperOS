param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$run = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $run) { throw 'Use a new output directory to preserve previous qualification evidence.' }
& (Join-Path $PSScriptRoot 'compile_workbench_rename_native.ps1') -DotnetPath $DotnetPath -CopperSharpRoot $CopperSharpRoot -OutputDirectory $run
$runnerArtifacts = Join-Path $run 'runner'
& $DotnetPath build (Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj') -c Release --nologo --artifacts-path $runnerArtifacts
if ($LASTEXITCODE) { throw 'Runner build failed.' }
$runner = Join-Path $runnerArtifacts 'bin\CopperOS.Commands.NativeExecution\release\CopperOS.Commands.NativeExecution.dll'
$suite = 'workbench-rename-startup-vector-fixture'
$evidence = @()
foreach ($cpu in '68000','68020','68040') {
    $hunk = Join-Path $run "rename-$cpu.hunk"
    $runtime = Join-Path $run "rename-$cpu.runtime.json"
    $before = (Get-FileHash -LiteralPath $hunk -Algorithm SHA256).Hash.ToLowerInvariant()
    & $DotnetPath $runner $hunk $cpu $runtime $suite
    if ($LASTEXITCODE) { throw "Runtime failed for $cpu." }
    $receipt = Get-Content -LiteralPath $runtime -Raw | ConvertFrom-Json
    $after = (Get-FileHash -LiteralPath $hunk -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($receipt.status -ne 'passed' -or $receipt.suite -ne $suite -or $receipt.cpu -ne $cpu -or
        $receipt.passed -ne 103 -or $receipt.cases.Count -ne 103 -or $receipt.sharedImageWrites -ne 0 -or
        $receipt.imageSha256 -ne $before -or $before -ne $after -or $receipt.shippingOrPureApproval) {
        throw "Incomplete or mismatched runtime receipt for $cpu."
    }
    if (@($receipt.cases | Where-Object { $_.configuredStackBytes -ne 4096 -or $_.stackBytesWritten -gt 4096 }).Count -ne 0) {
        throw "Candidate stack budget mismatch for $cpu."
    }
    $evidence += [ordered]@{ cpu = $cpu; binarySha256 = $before; runtime = $runtime;
        reportSha256 = (Get-FileHash -LiteralPath $runtime -Algorithm SHA256).Hash.ToLowerInvariant(); invocations = 103 }
}
[ordered]@{ status = 'passed'; suite = $suite; invocations = 309;
    scope = 'Candidate startup, early break, allocation, parser and initial matcher failures, direct success and pattern rejection with supplied vectors, including interleaving. Full body, original DOS/parity, minimum stack, purity and packaging remain separate gates.';
    shippingOrPureApproval = $false; evidence = $evidence } |
    ConvertTo-Json -Depth 5 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
