param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$OutputDirectory,
    [string]$OriginalAddBuffersHunk = 'D:/TestData/CopperOSCommands/Workbench31/AddBuffers-37.2.hunk'
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
    Join-Path $repo ('artifacts\addbuffers-wb31-native-' + [guid]::NewGuid().ToString('N'))
}
if ((Test-Path -LiteralPath $run) -and @(Get-ChildItem -LiteralPath $run -Force).Count -ne 0) {
    throw 'Use a fresh output directory; existing qualification evidence must be preserved.'
}
New-Item -ItemType Directory -Force -Path $run | Out-Null
if (!(Test-Path -LiteralPath $OriginalAddBuffersHunk) -or
    (Get-FileHash -LiteralPath $OriginalAddBuffersHunk -Algorithm SHA256).Hash.ToLowerInvariant() -ne
        '49be9c1cf1fb87c60fe8d6cc30c73aed6615654763833f3247fc17aecdc6b1ca') {
    throw 'Pinned Workbench AddBuffers 37.2 reference is required.'
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
    $hunk = Join-Path $run "addbuffers-wb31-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    $runtime = "$hunk.runtime.json"
    & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.AddBuffersNativeRoot.Workbench31AddBuffersEntry::Main' '--platform' 'amiga' '--cpu' $cpu '--clr' always '--exceptions' yolo '--format' hunk '--runtime' resident '--memory' none '--peephole' disabled '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') '--managed-assembly' $support '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') '--output' $hunk '--compatibility-report' $static
    if ($LASTEXITCODE) { throw "Native HUNK compilation failed for $cpu." }
    & $DotnetPath $runner $hunk $cpu $runtime 'addbuffers-wb31-reference-vector-fixture' $OriginalAddBuffersHunk
    if ($LASTEXITCODE) { throw "Native fixture failed for $cpu." }
    $s = Get-Content -Raw $static | ConvertFrom-Json
    $r = Get-Content -Raw $runtime | ConvertFrom-Json
    $n = $s.NativeCompatibility
    if (!$s.IsCompatible -or $s.RootMethodCount -ne 5 -or $s.ReachableMethodCount -le 0 -or
        @($s.ManagedAllocationSites).Count -ne 0 -or $n.RuntimeFeatureCount -ne 0 -or
        $n.RuntimeHelperCount -ne 0 -or $n.ExternalNativeTargetCount -ne 0 -or
        $n.ExceptionRegionCount -ne 0 -or $n.FatalMachineFaultSiteCount -ne 0 -or
        $r.status -ne 'passed' -or $r.original.Count -ne 27 -or $r.generated.Count -ne 27 -or
        $r.comparisons.Count -ne 27 -or @($r.comparisons | Where-Object { !$_.passed }).Count -ne 0 -or
        $r.allocationFailure.Result -ne 20 -or $r.allocationFailure.Error -ne 103) {
        throw "Qualification receipt incomplete for $cpu."
    }
    $file = Get-Item $hunk
    $artifacts += [ordered]@{ cpu=$cpu; bytes=$file.Length; sha256=(Get-FileHash $hunk -Algorithm SHA256).Hash.ToLowerInvariant(); reachableMethods=$s.ReachableMethodCount; originalInvocations=$r.original.Count; generatedInvocations=($r.generated.Count + 1); comparisons=$r.comparisons.Count }
}
[ordered]@{ schemaVersion=1; suite='CC12-AddBuffers-wb31-reference-vectors'; status='passed'; runtime='resident'; referenceCpu='68000'; entry='CopperOS.Commands.AddBuffersNativeRoot.Workbench31AddBuffersEntry::Main'; scope='Resident HUNK compilation and original 37.2 on 68000 versus replacement on each target CPU under supplied DOS/Exec vectors. Separate classic body preserves change/query, parser faults, result/error and cleanup behavior. Original 68020 execution has a retained unsupported emulator timing failure. Real DOS parsing/formatting/handlers, Workbench launch, PURE/resident lifecycle and packaging remain open.'; runtimeFixture=$true; artifacts=$artifacts } | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
