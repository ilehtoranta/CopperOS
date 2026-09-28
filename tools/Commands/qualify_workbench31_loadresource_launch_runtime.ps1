param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$rootProject = Join-Path $repo 'tests\Commands.LoadResourceNativeRoot\CopperOS.Commands.LoadResourceNativeRoot.csproj'
$root = Join-Path $repo 'tests\Commands.LoadResourceNativeRoot\bin\Release\net10.0'
$rootDll = Join-Path $root 'CopperOS.Commands.LoadResourceNativeRoot.dll'
$runnerProject = Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj'
$runner = Join-Path $repo 'tests\Commands.NativeExecution\bin\Release\net10.0\CopperOS.Commands.NativeExecution.dll'
$cliProject = Join-Path $CopperSharpRoot 'Compiler.Cli\CopperSharp.Compiler.Cli.csproj'
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
$run = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $repo ('artifacts\workbench31-loadresource-launch-runtime-' + [guid]::NewGuid().ToString('N'))
}
if (Test-Path -LiteralPath $run) { throw 'Use a fresh output directory; do not overwrite historical evidence.' }
New-Item -ItemType Directory -Path $run | Out-Null
$status = 'failed'
$failure = $null
$artifacts = @()
$sourcePaths = @(
    'src\Commands\Native\NativeWorkbench31LoadResourceLaunch.cs',
    'src\Commands\Native\NativeWorkbench31LoadResourceProtocol.cs',
    'src\Commands\Native\NativeCommandArguments.cs',
    'tests\Commands.LoadResourceNativeRoot\NativeWorkbench31LoadResourceLaunchProbe.cs',
    'tests\Commands.LoadResourceNativeRoot\CopperOS.Commands.LoadResourceNativeRoot.csproj',
    'tests\Commands.NativeExecution\LoadResourceLaunchRuntimeSuite.cs',
    'tests\Commands.NativeExecution\Program.cs',
    'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj',
    'tests\Commands.NativeExecution\CommandTestBus.cs',
    'tests\Commands.NativeExecution\HunkImage.cs',
    'tools\Commands\qualify_workbench31_loadresource_launch_runtime.ps1'
)
$sources = @($sourcePaths | ForEach-Object {
    [ordered]@{ path = $_; sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $repo $_)).Hash.ToLowerInvariant() }
})
try {
    foreach ($project in $cliProject, $rootProject, $runnerProject) {
        & $DotnetPath build $project -c Release --nologo
        if ($LASTEXITCODE) { throw "Build failed: $project" }
    }
    $support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
        Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
    if (!$support) { throw 'CopperSharp Amiga support assembly missing.' }
    foreach ($cpu in '68000', '68020', '68040') {
        $hunk = Join-Path $run "loadresource-launch-runtime-$cpu.hunk"
        $staticPath = "$hunk.compatibility.json"
        $runtimePath = "$hunk.runtime.json"
        & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.LoadResourceNativeRoot.NativeWorkbench31LoadResourceLaunchProbe::Main' `
            '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
            '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' `
            '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') `
            '--managed-assembly' $support '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') `
            '--output' $hunk '--compatibility-report' $staticPath
        if ($LASTEXITCODE) { throw "HUNK compilation failed for $cpu." }
        $static = Get-Content -Raw $staticPath | ConvertFrom-Json
        $native = $static.NativeCompatibility
        if (!$static.IsCompatible -or $static.ReachableMethodCount -lt 12 -or $static.ManagedAllocationSites.Count -ne 0 -or
            $native.RuntimeFeatureCount -ne 0 -or $native.RuntimeHelperCount -ne 0 -or
            $native.ExternalNativeTargetCount -ne 0 -or $native.ExceptionRegionCount -ne 0 -or
            $native.FatalMachineFaultSiteCount -ne 0) { throw "Resident compatibility failed for $cpu." }
        & $DotnetPath $runner $hunk $cpu $runtimePath 'loadresource-wb31-launch-runtime'
        if ($LASTEXITCODE) { throw "Runtime qualification failed for $cpu." }
        $runtime = Get-Content -Raw $runtimePath | ConvertFrom-Json
        if ($runtime.status -ne 'passed' -or $runtime.observations.Count -ne 15 -or $runtime.cpu -ne $cpu) {
            throw "Incomplete runtime receipt for $cpu."
        }
        $artifacts += [ordered]@{
            cpu = $cpu
            hunkSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $hunk).Hash.ToLowerInvariant()
            staticSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $staticPath).Hash.ToLowerInvariant()
            runtimeSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $runtimePath).Hash.ToLowerInvariant()
            cases = $runtime.observations.Count
            reachableMethods = $static.ReachableMethodCount
        }
    }
    if ($artifacts.Count -ne 3) { throw 'Missing CPU qualification.' }
    foreach ($source in $sources) {
        if ((Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $repo $source.path)).Hash.ToLowerInvariant() -ne $source.sha256) {
            throw ('Source changed during qualification: ' + $source.path)
        }
    }
    $status = 'passed'
}
catch { $failure = $_.ToString() }
[ordered]@{
    schemaVersion = 1
    suite = 'CC20-LoadResource-wb31-launch-runtime'
    status = $status
    failure = $failure
    runtime = 'resident'
    scope = 'Native client-side service lookup, detached worker SegList handoff/rollback and first resource request, with supplied public DOS/Exec vectors. Does not execute the child, actual DOS segment unload, complete command, callback quiescence, original guest behavior, PURE or shipping readiness.'
    sourceSha256 = $sources
    artifacts = $artifacts
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
if ($status -ne 'passed') { throw $failure }
