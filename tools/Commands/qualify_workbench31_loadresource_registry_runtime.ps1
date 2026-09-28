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
    Join-Path $repo ('artifacts\workbench31-loadresource-registry-runtime-' + [guid]::NewGuid().ToString('N'))
}
if (Test-Path -LiteralPath $run) { throw 'Use a fresh output directory; do not overwrite historical evidence.' }
New-Item -ItemType Directory -Path $run | Out-Null
$status = 'failed'
$failure = $null
$artifacts = @()
try {
    foreach ($project in $cliProject, $rootProject, $runnerProject) {
        & $DotnetPath build $project -c Release --nologo
        if ($LASTEXITCODE) { throw "Build failed: $project" }
    }
    $support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
        Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
    if (!$support) { throw 'CopperSharp Amiga support assembly missing.' }
    foreach ($cpu in '68000', '68020', '68040') {
        $hunk = Join-Path $run "loadresource-registry-runtime-$cpu.hunk"
        $staticPath = "$hunk.compatibility.json"
        $runtimePath = "$hunk.runtime.json"
        & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.LoadResourceNativeRoot.NativeWorkbench31LoadResourceRegistryProbe::Main' `
            '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
            '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' `
            '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') `
            '--managed-assembly' $support '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') `
            '--output' $hunk '--compatibility-report' $staticPath
        if ($LASTEXITCODE) { throw "HUNK compilation failed for $cpu." }
        $static = Get-Content -Raw $staticPath | ConvertFrom-Json
        $native = $static.NativeCompatibility
        if (!$static.IsCompatible -or $static.ReachableMethodCount -lt 7 -or
            $native.RuntimeFeatureCount -ne 0 -or $native.RuntimeHelperCount -ne 0 -or
            $native.ExternalNativeTargetCount -ne 0 -or $native.ExceptionRegionCount -ne 0 -or
            $native.FatalMachineFaultSiteCount -ne 0) { throw "Resident compatibility failed for $cpu." }
        & $DotnetPath $runner $hunk $cpu $runtimePath 'loadresource-wb31-registry-runtime'
        if ($LASTEXITCODE) { throw "Runtime qualification failed for $cpu." }
        $runtime = Get-Content -Raw $runtimePath | ConvertFrom-Json
        if ($runtime.status -ne 'passed' -or $runtime.observations.Count -ne 10 -or $runtime.cpu -ne $cpu) {
            throw "Incomplete runtime receipt for $cpu."
        }
        $artifacts += [ordered]@{
            cpu = $cpu
            hunkSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $hunk).Hash.ToLowerInvariant()
            staticSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $staticPath).Hash.ToLowerInvariant()
            runtimeSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $runtimePath).Hash.ToLowerInvariant()
            cases = $runtime.observations.Count
        }
    }
    if ($artifacts.Count -ne 3) { throw 'Missing CPU qualification.' }
    $status = 'passed'
}
catch { $failure = $_.ToString() }
[ordered]@{
    schemaVersion = 1
    suite = 'CC20-LoadResource-wb31-registry-runtime'
    status = $status
    failure = $failure
    runtime = 'resident'
    scope = 'Generated HUNK opened-resource registry with supplied public Exec/Utility/Graphics/Locale vectors on 68000/68020/68040. Does not prove worker/resource integration, original guest behavior, cross-invocation code lifetime, PURE or shipping readiness.'
    sourceSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $repo 'src\Commands\Native\NativeWorkbench31LoadResourceRegistry.cs')).Hash.ToLowerInvariant()
    artifacts = $artifacts
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
if ($status -ne 'passed') { throw $failure }
