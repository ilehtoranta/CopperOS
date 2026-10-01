param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $repo 'tests\Commands.LoadResourceNativeRoot\CopperOS.Commands.LoadResourceNativeRoot.csproj'
$runnerProject = Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj'
$root = Join-Path $repo 'tests\Commands.LoadResourceNativeRoot\bin\Release\net10.0'
$rootDll = Join-Path $root 'CopperOS.Commands.LoadResourceNativeRoot.dll'
$runner = Join-Path $repo 'tests\Commands.NativeExecution\bin\Release\net10.0\CopperOS.Commands.NativeExecution.dll'
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
$source = Join-Path $repo 'src\Commands\LoadResource\Native\NativeWorkbench31LoadResourceProtocol.cs'
$sourceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $source).Hash.ToLowerInvariant()
$run = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $repo ('artifacts\workbench31-loadresource-protocol-runtime-' + [guid]::NewGuid().ToString('N'))
}
if ((Test-Path -LiteralPath $run) -and @(Get-ChildItem -LiteralPath $run -Force).Count -ne 0) {
    throw 'Use a fresh output directory; existing qualification evidence must be preserved.'
}
New-Item -ItemType Directory -Force -Path $run | Out-Null
& $DotnetPath build $project -c Release --nologo
if ($LASTEXITCODE) { throw 'LoadResource root build failed.' }
& $DotnetPath build $runnerProject -c Release --nologo
if ($LASTEXITCODE) { throw 'Native fixture runner build failed.' }
if (!(Test-Path -LiteralPath $cli)) { throw 'CopperSharp compiler CLI is missing.' }
$support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'CopperSharp Amiga support assembly missing.' }
$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $run "loadresource-protocol-wb31-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    $runtime = Join-Path $run "loadresource-protocol-wb31-$cpu.runtime.json"
    & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.LoadResourceNativeRoot.NativeWorkbench31LoadResourceProtocolProbe::Main' `
        '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
        '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') `
        '--managed-assembly' $support `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') `
        '--output' $hunk '--compatibility-report' $static
    if ($LASTEXITCODE) { throw "Protocol HUNK compilation failed for $cpu." }
    & $DotnetPath $runner $hunk $cpu $runtime 'loadresource-wb31-protocol-runtime'
    if ($LASTEXITCODE) { throw "Protocol native execution failed for $cpu." }
    $s = Get-Content -Raw -LiteralPath $static | ConvertFrom-Json
    $r = Get-Content -Raw -LiteralPath $runtime | ConvertFrom-Json
    $n = $s.NativeCompatibility
    if (!$s.IsCompatible -or @($s.ManagedAllocationSites).Count -ne 0 -or
        $n.RuntimeFeatureCount -ne 0 -or $n.RuntimeHelperCount -ne 0 -or
        $n.ExternalNativeTargetCount -ne 0 -or $n.ExceptionRegionCount -ne 0 -or
        $n.FatalMachineFaultSiteCount -ne 0 -or $r.status -ne 'passed' -or
        $r.cpu -ne $cpu -or $r.suite -ne 'loadresource-wb31-protocol-runtime' -or
        $r.passed -ne 19 -or $r.sharedImageWrites -ne 0 -or $r.nativeWrites -le 0 -or
        $r.imageSha256 -ne (Get-FileHash -Algorithm SHA256 -LiteralPath $hunk).Hash.ToLowerInvariant()) {
        throw "Incomplete protocol qualification for $cpu."
    }
    $artifacts += [ordered]@{
        cpu = $cpu; bytes = (Get-Item -LiteralPath $hunk).Length
        sha256 = $r.imageSha256; reachableMethods = $s.ReachableMethodCount
        suppliedVectorInvocations = $r.passed; sharedImageWrites = $r.sharedImageWrites
        nativeWrites = $r.nativeWrites; runtimeReceipt = [IO.Path]::GetFileName($runtime)
        staticReceipt = [IO.Path]::GetFileName($static)
    }
}
if ((Get-FileHash -Algorithm SHA256 -LiteralPath $source).Hash.ToLowerInvariant() -ne $sourceHash) {
    throw 'Protocol source changed during qualification.'
}
[ordered]@{
    schemaVersion = 1; suite = 'CC20-LoadResource-wb31-client-protocol-native'; status = 'passed'
    runtime = 'resident'; entry = 'CopperOS.Commands.LoadResourceNativeRoot.NativeWorkbench31LoadResourceProtocolProbe::Main'
    sourceAudit = 'docs/Commands/Workbench31MorphOS320/reference-captures/loadresource-wb31-request-protocol-audit-20260926.json'
    sourceSha256 = $sourceHash
    scope = 'Nineteen supplied-vector native client invocations per CPU, including interleaved callers, 54-byte stack message ABI, borrowed stream/current-directory/result-slot forwarding, live ReadArgs ownership until reply, result/error propagation, cleanup clobber preservation and rejected boundaries. The probe supplies DOS/parser/worker responses; full command startup, actual worker dispatch, original OS execution, code lifetime, PURE and packaging are unproven.'
    realKickstartExecution = $false; realWorkerExecution = $false; shippingOrPureApproval = $false
    artifacts = $artifacts
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
