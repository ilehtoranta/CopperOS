param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$OutputDirectory,
    [string]$OriginalRelabelHunk = 'D:/TestData/CopperOSCommands/Workbench31/Relabel-37.2.hunk'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\CopperOS.Commands.AddBuffersNativeRoot.csproj'
$runnerProject = Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj'
$root = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\bin\Release\net10.0'
$rootDll = Join-Path $root 'CopperOS.Commands.AddBuffersNativeRoot.dll'
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
$runner = Join-Path $repo 'tests\Commands.NativeExecution\bin\Release\net10.0\CopperOS.Commands.NativeExecution.dll'
$run = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $repo ('artifacts\relabel-wb31-native-' + [guid]::NewGuid().ToString('N'))
}
if ((Test-Path -LiteralPath $run) -and @(Get-ChildItem -LiteralPath $run -Force).Count -ne 0) {
    throw 'Use a fresh output directory; existing qualification evidence must be preserved.'
}
New-Item -ItemType Directory -Force -Path $run | Out-Null
if (!(Test-Path -LiteralPath $OriginalRelabelHunk) -or
    (Get-FileHash -LiteralPath $OriginalRelabelHunk -Algorithm SHA256).Hash.ToLowerInvariant() -ne
        '163ea95df394d2c800a161befae0bea664cb6b8a07dffc2b2b1af3141588a3ff') {
    throw 'Pinned Workbench Relabel 37.2 reference is required.'
}
& $DotnetPath build (Join-Path $CopperSharpRoot 'Compiler.Cli\CopperSharp.Compiler.Cli.csproj') -c Release --nologo
if ($LASTEXITCODE) { throw 'Compiler build failed.' }
& $DotnetPath build $project -c Release --nologo
if ($LASTEXITCODE) { throw 'Root build failed.' }
& $DotnetPath build $runnerProject -c Release --nologo
if ($LASTEXITCODE) { throw 'Runner build failed.' }
$support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'CopperSharp Amiga support assembly missing.' }
$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $run "relabel-wb31-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    $runtime = "$hunk.runtime.json"
    & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.AddBuffersNativeRoot.Workbench31RelabelEntry::Main' '--platform' 'amiga' '--cpu' $cpu '--clr' always '--exceptions' yolo '--format' hunk '--runtime' resident '--memory' none '--peephole' disabled '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') '--managed-assembly' $support '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') '--output' $hunk '--compatibility-report' $static
    if ($LASTEXITCODE) { throw "Native HUNK compilation failed for $cpu." }
    & $DotnetPath $runner $hunk $cpu $runtime 'relabel-wb31-reference-vector-fixture' $OriginalRelabelHunk
    if ($LASTEXITCODE) { throw "Native fixture failed for $cpu." }
    $s = Get-Content -Raw $static | ConvertFrom-Json
    $r = Get-Content -Raw $runtime | ConvertFrom-Json
    $n = $s.NativeCompatibility
    $hunkHash = (Get-FileHash -LiteralPath $hunk -Algorithm SHA256).Hash.ToLowerInvariant()
    if (!$s.IsCompatible -or $s.RootMethodCount -ne 1 -or $s.ReachableMethodCount -le 0 -or
        @($s.ManagedAllocationSites).Count -ne 0 -or $n.RuntimeFeatureCount -ne 0 -or
        $n.RuntimeHelperCount -ne 0 -or $n.ExternalNativeTargetCount -ne 0 -or
        $n.ExceptionRegionCount -ne 0 -or $n.FatalMachineFaultSiteCount -ne 0 -or
        $r.status -ne 'passed' -or $r.original.Count -ne 38 -or $r.generated.Count -ne 38 -or
        $r.comparisons.Count -ne 38 -or @($r.comparisons | Where-Object { !$_.passed }).Count -ne 0 -or
        $r.allocationFailure.Result -ne 20 -or $r.allocationFailure.Error -ne 103 -or
        $r.scratchAllocationFailure.Result -ne 20 -or $r.scratchAllocationFailure.Error -ne 103 -or $r.longDrive.Result -ne 0 -or
        $r.referenceCpu -ne $cpu -or $r.cpu -ne $cpu -or $r.generatedSha256 -ne $hunkHash -or
        $r.originalSha256 -ne '163ea95df394d2c800a161befae0bea664cb6b8a07dffc2b2b1af3141588a3ff' -or
        $r.executorSha256 -ne (Get-FileHash -LiteralPath $runner -Algorithm SHA256).Hash.ToLowerInvariant()) {
        throw "Qualification receipt incomplete for $cpu."
    }
    $file = Get-Item $hunk
    $artifacts += [ordered]@{ cpu=$cpu; bytes=$file.Length; sha256=$hunkHash; reachableMethods=$s.ReachableMethodCount; originalInvocations=$r.original.Count; generatedInvocations=($r.generated.Count + 3); comparisons=$r.comparisons.Count; runtimeReportSha256=(Get-FileHash -LiteralPath $runtime -Algorithm SHA256).Hash.ToLowerInvariant() }
}
[ordered]@{ schemaVersion=1; suite='CC12-Relabel-wb31-reference-vectors'; status='passed'; runtime='resident'; referenceCpu='same as generated CPU'; entry='CopperOS.Commands.AddBuffersNativeRoot.Workbench31RelabelEntry::Main'; scope='Resident HUNK compilation and original 37.2 versus replacement on each target CPU under supplied DOS/Exec vectors. Separate classic body preserves assigns/device/volume lookup, last-byte drive handling, diagnostics, result/error and cleanup behavior. Parser-buffer mutation is recorded separately; the replacement retains immutable borrowed arguments. Real DOS parsing/formatting/handlers, Workbench launch, PURE/resident lifecycle and packaging remain open.'; runtimeFixture=$true; artifacts=$artifacts } | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
