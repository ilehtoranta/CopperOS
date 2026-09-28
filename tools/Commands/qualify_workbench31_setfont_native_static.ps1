param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\CopperOS.Commands.AddBuffersNativeRoot.csproj'
$root = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\bin\Release\net10.0'
$rootDll = Join-Path $root 'CopperOS.Commands.AddBuffersNativeRoot.dll'
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
$run = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $repo 'artifacts\setfont-wb31-native-static-20260922-v1'
}
New-Item -ItemType Directory -Force $run | Out-Null

& $DotnetPath build $project -c Release --nologo
if ($LASTEXITCODE) { throw 'Root build failed.' }
$support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'CopperSharp Amiga support assembly missing.' }

$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $run "setfont-wb31-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.AddBuffersNativeRoot.Workbench31SetFontEntry::Main' `
        '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
        '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') `
        '--managed-assembly' $support `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') `
        '--output' $hunk '--compatibility-report' $static
    if ($LASTEXITCODE) { throw "Native HUNK compilation failed for $cpu." }
    $s = Get-Content -Raw $static | ConvertFrom-Json
    $n = $s.NativeCompatibility
    if (!$s.IsCompatible -or $s.RootMethodCount -ne 1 -or
        $s.ReachableMethodCount -le 0 -or @($s.ManagedAllocationSites).Count -ne 0 -or
        $n.RuntimeFeatureCount -ne 0 -or $n.RuntimeHelperCount -ne 0 -or
        $n.ExternalNativeTargetCount -ne 0 -or $n.ExceptionRegionCount -ne 0 -or
        $n.FatalMachineFaultSiteCount -ne 0) {
        throw "Resident compatibility report incomplete for $cpu."
    }
    $file = Get-Item $hunk
    $artifacts += [ordered]@{
        cpu = $cpu
        bytes = $file.Length
        sha256 = (Get-FileHash $hunk -Algorithm SHA256).Hash.ToLowerInvariant()
        reachableMethods = $s.ReachableMethodCount
        managedAllocationSites = @($s.ManagedAllocationSites).Count
        staticRuntimeFeatureCount = $n.RuntimeFeatureCount
        staticRuntimeHelperCount = $n.RuntimeHelperCount
        staticExternalNativeTargetCount = $n.ExternalNativeTargetCount
        staticExceptionRegionCount = $n.ExceptionRegionCount
        staticFatalMachineFaultSiteCount = $n.FatalMachineFaultSiteCount
    }
}

[ordered]@{
    schemaVersion = 1
    suite = 'CC20-SetFont-wb31-native-static'
    status = 'passed'
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.Workbench31SetFontEntry::Main'
    sourceBinary = [ordered]@{
        path = 'D:/TestData/TestImages/Workbench v3.1 rev 40.42 (1994)(Commodore)(M10)(Disk 2 of 6)(Workbench)[!].zip::C/SetFont'
        version = 'setfont 39.1 (2.6.92)'
        sha256 = 'c3e1e763b12fd4f710a414443facea49479aa723a94828828dbaee214fe4ad8c'
    }
    scope = 'Three-CPU resident HUNK compilation for the captured Workbench 3.1 SetFont syntax and library-call candidate. Static resident compatibility is green; exact console DoPkt packet semantics, diagnostics/result precedence, guest runtime parity, PURE/resident metadata, installed placement, package and differential gates remain open.'
    runtimeFixture = $false
    artifacts = $artifacts
} | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
