param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$sdkProject = Join-Path $CopperSharpRoot 'Sdk.Amiga\CopperSharp.Sdk.Amiga.csproj'
$compilerProject = Join-Path $CopperSharpRoot 'Compiler\CopperSharp.Compiler.csproj'
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
$sdk = Join-Path $CopperSharpRoot 'Sdk.Amiga\bin\Debug\net10.0\CopperSharp.Sdk.Amiga.dll'
$compiler = Join-Path $CopperSharpRoot 'Compiler\bin\Debug\net10.0\CopperSharp.Compiler.dll'
$project = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\CopperOS.Commands.AddBuffersNativeRoot.csproj'
$root = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\bin\Release\net10.0'
$rootDll = Join-Path $root 'CopperOS.Commands.AddBuffersNativeRoot.dll'
$runnerProject = Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj'
$runner = Join-Path $repo 'tests\Commands.NativeExecution\bin\Release\net10.0\CopperOS.Commands.NativeExecution.dll'
$run = [IO.Path]::GetFullPath($OutputDirectory)
if ((Test-Path -LiteralPath $run) -and @(Get-ChildItem -LiteralPath $run -Force).Count -ne 0) {
    throw 'Use a fresh output directory; existing qualification evidence must be preserved.'
}
New-Item -ItemType Directory -Force -Path $run | Out-Null

& $DotnetPath build $sdkProject -c Debug --nologo --no-restore
if ($LASTEXITCODE) { throw 'CopperSharp SDK build failed.' }
& $DotnetPath build $compilerProject -c Debug --nologo --no-restore
if ($LASTEXITCODE) { throw 'CopperSharp compiler build failed.' }
& $DotnetPath build $project -c Release --nologo --no-restore -p:BuildProjectReferences=false
if ($LASTEXITCODE) { throw 'Native command root build failed.' }
& $DotnetPath build $runnerProject -c Release --nologo --no-restore
if ($LASTEXITCODE) { throw 'Native execution runner build failed.' }
if (!(Test-Path -LiteralPath $cli)) { throw 'CopperSharp compiler CLI is missing.' }
if (!(Test-Path -LiteralPath $sdk)) { throw 'Source-built CopperSharp SDK is missing.' }
$support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'CopperSharp Amiga support assembly missing.' }

$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $run "extractkickstart-wb31-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    $runtime = "$hunk.runtime.json"
    & $DotnetPath $cli $rootDll --entry 'CopperOS.Commands.AddBuffersNativeRoot.Workbench31ExtractKickstartEntry::Main' --platform amiga --cpu $cpu --clr always --exceptions yolo --format hunk --runtime resident --memory none --peephole disabled --managed-assembly $sdk --managed-assembly $support --managed-assembly $compiler --output $hunk --compatibility-report $static
    if ($LASTEXITCODE) { throw "Native HUNK compilation failed for $cpu." }
    $s = Get-Content -Raw $static | ConvertFrom-Json
    $n = $s.NativeCompatibility
    if (!$s.IsCompatible -or $s.RootMethodCount -ne 1 -or $s.ReachableMethodCount -le 0 -or
        @($s.ManagedAllocationSites).Count -ne 0 -or $n.RuntimeFeatureCount -ne 0 -or
        $n.RuntimeHelperCount -ne 0 -or $n.ExternalNativeTargetCount -ne 0 -or
        $n.ExceptionRegionCount -ne 0 -or $n.FatalMachineFaultSiteCount -ne 0) {
        throw "Native compatibility receipt incomplete for $cpu."
    }
    & $DotnetPath $runner $hunk $cpu $runtime 'workbench31-extractkickstart-native-entry-vector-fixture'
    if ($LASTEXITCODE) { throw "Native runtime fixture failed for $cpu." }
    $r = Get-Content -Raw $runtime | ConvertFrom-Json
    if ($r.status -ne 'passed' -or $r.passed -ne 17 -or $r.sharedImageWrites -ne 0) {
        throw "Native runtime receipt incomplete for $cpu."
    }
    $file = Get-Item -LiteralPath $hunk
    $artifacts += [ordered]@{
        cpu = $cpu
        bytes = $file.Length
        sha256 = (Get-FileHash -LiteralPath $hunk -Algorithm SHA256).Hash.ToLowerInvariant()
        reachableMethods = $s.ReachableMethodCount
        runtimeFeatureCount = $n.RuntimeFeatureCount
        suppliedVectorInvocations = $r.passed
    }
}

[ordered]@{
    schemaVersion = 1
    suite = 'CC22.ExtractKickstart.wb31-native-compile'
    status = 'passed'
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.Workbench31ExtractKickstartEntry::Main'
    sourceBinary = [ordered]@{
        path = 'D:/TestData/TestImages/Workbench v3.1 rev 40.42 (1994)(Commodore)(M10)(Disk 1 of 6)(Install)[!].zip::C/ExtractKickstart'
        version = '$VER: extractkickstart 39.3 (5.8.92)'
        sha256 = '832fc20fe032a4028b0e1ebc5b4e93641cb0ed02598f6a238fa54426a14ca736'
    }
    scope = 'Three-CPU resident HUNK compilation plus the bounded native trackdisk/DOS fixture for the source-derived Workbench 3.1 ExtractKickstart candidate. The fixture covers parser ownership, 1.3 and legacy layout reads, SuperKickstart validation, diagnostics, device/output failures, cleanup, and interleaved invocations. Guest differential, PURE approval and package closure remain open.'
    runtimeFixture = $true
    artifacts = $artifacts
} | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
