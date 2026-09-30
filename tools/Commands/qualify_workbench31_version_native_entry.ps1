param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\CopperOS.Commands.AddBuffersNativeRoot.csproj'
$runnerProject = Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj'
$root = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\bin\Release\net10.0'
$rootDll = Join-Path $root 'CopperOS.Commands.AddBuffersNativeRoot.dll'
$runner = Join-Path $repo 'tests\Commands.NativeExecution\bin\Release\net10.0\CopperOS.Commands.NativeExecution.dll'
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
$run = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $repo ('artifacts\version-wb31-native-' + [guid]::NewGuid().ToString('N'))
}
if (Test-Path -LiteralPath $run) { throw 'Use a fresh output directory; do not overwrite historical evidence.' }
New-Item -ItemType Directory -Path $run | Out-Null
$sourcePaths = @(
    'src\Commands\Version\Native\NativeWorkbench31VersionCommand.cs',
    'src\Commands\Version\Native\NativeWorkbench31VersionFull.cs',
    'src\Commands\Common\NativeCommandArguments.cs',
    'src\Commands\Common\NativeCommandStartup.cs',
    'tests\Commands.AddBuffersNativeRoot\Workbench31VersionEntry.cs',
    'tests\Commands.AddBuffersNativeRoot\CopperOS.Commands.AddBuffersNativeRoot.csproj',
    'tests\Commands.NativeExecution\VersionEntrySuite.cs',
    'tests\Commands.NativeExecution\Program.cs',
    'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj',
    'tests\Commands.NativeExecution\CommandTestBus.cs',
    'tests\Commands.NativeExecution\HunkImage.cs',
    'CopperOS.Portable.props',
    'tools\Commands\qualify_workbench31_version_native_entry.ps1'
)
$sources = @($sourcePaths | ForEach-Object {
    [ordered]@{ path = $_; sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $repo $_)).Hash.ToLowerInvariant() }
})
$status = 'failed'
$failure = $null
$sourcesUnchanged = $false
$compiledInputs = @()
$compiledInputsUnchanged = $false
$artifacts = @()
try {
& $DotnetPath build (Join-Path $CopperSharpRoot 'Compiler.Cli\CopperSharp.Compiler.Cli.csproj') -c Release --nologo
if ($LASTEXITCODE) { throw 'Compiler build failed.' }
& $DotnetPath build $project -c Release --nologo
if ($LASTEXITCODE) { throw 'Root build failed.' }
& $DotnetPath build $runnerProject -c Release --nologo
if ($LASTEXITCODE) { throw 'Runner build failed.' }

$support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'CopperSharp Amiga support assembly missing.' }
$compiledInputPaths = @($support)
foreach ($directory in (Split-Path $cli), $root, (Split-Path $runner)) {
    $compiledInputPaths += @(Get-ChildItem -LiteralPath $directory -File |
        Where-Object { $_.Extension -in '.dll', '.json', '.pdb' } |
        Select-Object -ExpandProperty FullName)
}
$compiledInputs = @($compiledInputPaths | Sort-Object -Unique | ForEach-Object {
    $file = Get-Item -LiteralPath $_
    [ordered]@{ path = $file.FullName; bytes = $file.Length; sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $_).Hash.ToLowerInvariant() }
})
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $run "version-wb31-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.AddBuffersNativeRoot.Workbench31VersionEntry::Main' `
        '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
        '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' `
        '--exports' 'none' `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') `
        '--managed-assembly' $support `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') `
        '--output' $hunk '--compatibility-report' $static
    if ($LASTEXITCODE) { throw "Native HUNK compilation failed for $cpu." }
    $s = Get-Content -Raw $static | ConvertFrom-Json
    $n = $s.NativeCompatibility
    if (!$s.IsCompatible -or $s.RootMethodCount -ne 1 -or
        $s.ReachableMethodCount -le 0 -or @($s.ManagedAllocationSites).Count -ne 0 -or
        @($s.Members).Count -ne 0 -or
        $n.RuntimeFeatureCount -ne 0 -or $n.RuntimeHelperCount -ne 0 -or
        $n.ExternalNativeTargetCount -ne 0 -or $n.ExceptionRegionCount -ne 0 -or
        $n.FatalMachineFaultSiteCount -ne 0) {
        throw "Resident compatibility report incomplete for $cpu."
    }
    $runtime = "$hunk.runtime.json"
    & $DotnetPath $runner $hunk $cpu $runtime 'workbench31-version-native-entry-vector-fixture'
    if ($LASTEXITCODE) { throw "Native fixture failed for $cpu." }
    $r = Get-Content -Raw $runtime | ConvertFrom-Json
    if ($r.status -ne 'passed' -or $r.passed -ne $r.cases.Count -or
        $r.passed -le 58 -or
        $r.sharedImageWrites -ne 0 -or $r.nativeWrites -le 0) {
        throw "Native fixture report incomplete for $cpu."
    }
    $file = Get-Item $hunk
    $artifacts += [ordered]@{
        cpu = $cpu
        bytes = $file.Length
        sha256 = (Get-FileHash $hunk -Algorithm SHA256).Hash.ToLowerInvariant()
        reachableMethods = $s.ReachableMethodCount
        suppliedVectorInvocations = $r.passed
        sharedImageWrites = $r.sharedImageWrites
        compatibilitySha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $static).Hash.ToLowerInvariant()
        runtimeSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $runtime).Hash.ToLowerInvariant()
    }
}
if ($artifacts.Count -ne 3) { throw 'Missing CPU qualification.' }
foreach ($source in $sources) {
    if ((Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $repo $source.path)).Hash.ToLowerInvariant() -ne $source.sha256) {
        throw ('Source changed during qualification: ' + $source.path)
    }
}
$sourcesUnchanged = $true
foreach ($inputFile in $compiledInputs) {
    if ((Get-FileHash -Algorithm SHA256 -LiteralPath $inputFile.path).Hash.ToLowerInvariant() -ne $inputFile.sha256) {
        throw ('Compiled input changed during qualification: ' + $inputFile.path)
    }
}
$compiledInputsUnchanged = $true
$status = 'passed'
}
catch { $failure = $_.ToString() }

[ordered]@{
    schemaVersion = 1
    suite = 'CC17-Version-wb31-native-entry'
    status = $status
    failure = $failure
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.Workbench31VersionEntry::Main'
    scope = 'Three-CPU resident HUNK compilation plus every supplied DOS/Exec/Utility invocation per CPU. Named first-provider lookup traverses case-insensitive full-name Resident tables and high-bit continuations, retains canonical name ownership and Resident byte version, parses signed revision through StrToLong, and reconstructs FULL date/extra with public date calls. Covers the no-name Kickstart/version.library report and comparisons; DOS FindSegment system 0/1 command lookup, $VER: extraction, FULL date/extra, and internal/disabled shell Resident fallback; trailing-colon DOS device lookup with LockDosList/FindDosEntry/UnLockDosList, DeviceNode startup/segment checks, Resident lookup, FULL formatting, missing-node/startup/segment/Resident fallthrough, and colon restoration; direct FILE tag scans across DOS.Read boundaries, FILE FULL output and post-output minimum comparisons, FILE provider restriction, HUNK LoadSeg Resident fallback and linked segments; cleanup on open/read/allocation failures; ignored UNIT/INTERNAL/RES slots; ReadArgs failure severity 20; library cleanup; and the original DOS v37 startup requirement. Original guest observations show C:Version DF0: and C:Version DF0: RES both emit filesystem 40.1, but candidate guest parity for this handler path and segment lookup remains open. Complete classic system behavior, original secondary-error semantics, broader FILE parity, original PURE admission, and package admission remain open; original command flags are zero, and resident compiler storage checks do not establish original PURE admission.'
    sourceSha256 = $sources
    sourcesUnchanged = $sourcesUnchanged
    compiledInputs = $compiledInputs
    compiledInputsUnchanged = $compiledInputsUnchanged
    artifacts = $artifacts
} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
if ($status -ne 'passed') { throw $failure }
