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
& (Join-Path $PSScriptRoot 'compile_morphos_rename_native.ps1') -DotnetPath $DotnetPath -CopperSharpRoot $CopperSharpRoot -OutputDirectory $run
$runnerArtifacts = Join-Path $run 'runner'
& $DotnetPath build (Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj') -c Release --nologo --artifacts-path $runnerArtifacts
if ($LASTEXITCODE) { throw 'Runner build failed.' }
$runner = Join-Path $runnerArtifacts 'bin\CopperOS.Commands.NativeExecution\release\CopperOS.Commands.NativeExecution.dll'
$suite = 'morphos-rename-native-entry-vector-fixture'
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
        $receipt.passed -ne 112 -or $receipt.cases.Count -ne 112 -or $receipt.sharedImageWrites -ne 0 -or
        $receipt.imageSha256 -ne $before -or $before -ne $after -or $receipt.shippingOrPureApproval) {
        throw "Incomplete or mismatched runtime receipt for $cpu."
    }
    if (@($receipt.cases | Where-Object { $_.configuredStackBytes -ne 4096 -or $_.stackBytesWritten -gt 4096 }).Count -ne 0) {
        throw "Candidate stack budget mismatch for $cpu."
    }
    $evidence += [ordered]@{ cpu = $cpu; binarySha256 = $before; runtime = $runtime;
        reportSha256 = (Get-FileHash -LiteralPath $runtime -Algorithm SHA256).Hash.ToLowerInvariant(); invocations = 112 }
}
$sources = @()
foreach ($relative in 'src/Commands/Native/NativeMorphOSRenameCommand.cs', 'src/Commands/Native/NativeCommandStartup.cs', 'tests/Commands.AddBuffersNativeRoot/NativeMorphOSRenameEntry.cs', 'tests/Commands.NativeExecution/MorphOSRenameSuite.cs', 'tests/Commands.NativeExecution/Program.cs') {
    $sources += [ordered]@{ path = $relative; sha256 = (Get-FileHash -LiteralPath (Join-Path $repo $relative) -Algorithm SHA256).Hash.ToLowerInvariant() }
}
[ordered]@{ status = 'passed'; suite = $suite; invocations = 336; sourceEvidence = $sources;
    scope = 'MorphOS50.8 source-observed body behavior: parser and allocation failures, destination dispatch, direct/directory mutation, ordering, quiet/break/IoErr policies and 2048-byte boundaries, with every scenario also interleaved. Supplied vectors only; original MorphOS parity, real OS stack, pure admission and packaging remain open.';
    shippingOrPureApproval = $false; evidence = $evidence } |
    ConvertTo-Json -Depth 5 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
