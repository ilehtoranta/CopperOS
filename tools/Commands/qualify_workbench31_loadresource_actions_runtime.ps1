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
    Join-Path $repo ('artifacts\workbench31-loadresource-actions-runtime-' + [guid]::NewGuid().ToString('N'))
}
if (Test-Path -LiteralPath $run) { throw 'Use a fresh output directory; do not overwrite historical evidence.' }
New-Item -ItemType Directory -Path $run | Out-Null
$status = 'failed'
$failure = $null
$artifacts = @()
$sourcePaths = @('Actions', 'Messages', 'Registry', 'LoadSeg') | ForEach-Object {
    'src\Commands\LoadResource\Native\NativeWorkbench31LoadResource' + $_ + '.cs'
}
$sourcePaths += @(
    'tests\Commands.LoadResourceNativeRoot\NativeWorkbench31LoadResourceActionsProbe.cs',
    'tests\Commands.LoadResourceNativeRoot\CopperOS.Commands.LoadResourceNativeRoot.csproj',
    'tests\Commands.NativeExecution\LoadResourceActionsRuntimeSuite.cs',
    'tests\Commands.NativeExecution\Program.cs',
    'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj',
    'tests\Commands.NativeExecution\CommandTestBus.cs',
    'tests\Commands.NativeExecution\HunkImage.cs',
    'tools\Commands\qualify_workbench31_loadresource_actions_runtime.ps1'
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
        $hunk = Join-Path $run "loadresource-actions-runtime-$cpu.hunk"
        $staticPath = "$hunk.compatibility.json"
        $runtimePath = "$hunk.runtime.json"
        & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.LoadResourceNativeRoot.NativeWorkbench31LoadResourceActionsProbe::Main' `
            '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
            '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' `
            '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') `
            '--managed-assembly' $support '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') `
            '--output' $hunk '--compatibility-report' $staticPath
        if ($LASTEXITCODE) { throw "HUNK compilation failed for $cpu." }
        $static = Get-Content -Raw $staticPath | ConvertFrom-Json
        $native = $static.NativeCompatibility
        $members = @($static.Members)
        if ($members.Count -ne 1 -or
            $members[0].Status -ne 'intrinsic' -or
            $members[0].Binding -ne 'intrinsic:nullable-get-value-or-default-no-argument:Amiga.BPTR' -or
            $members[0].Member.AssemblyName -ne 'System.Runtime' -or
            $members[0].Member.TypeName -ne 'System.Nullable<Amiga.BPTR>' -or
            $members[0].Member.Name -ne 'GetValueOrDefault' -or
            $members[0].Member.IsStatic -or $members[0].Member.GenericArity -ne 0 -or
            $members[0].Member.ReturnType -ne '!0' -or
            @($members[0].Member.MethodTypeArguments).Count -ne 0 -or
            @($members[0].Member.ParameterTypes).Count -ne 0 -or
            @($members[0].Effects).Count -ne 0 -or
            @($members[0].RequiredFeatures).Count -ne 1 -or
            $members[0].RequiredFeatures[0] -ne 'nullable-values' -or
            @($static.ManagedAllocationSites).Count -ne 0) { throw "Unexpected managed binding for $cpu." }
        if (!$static.IsCompatible -or $static.ReachableMethodCount -lt 25 -or
            $native.RuntimeFeatureCount -ne 1 -or @($native.RuntimeFeatures).Count -ne 1 -or
            $native.RuntimeFeatures[0] -ne 'nullable-values' -or
            $native.RuntimeHelperCount -ne 0 -or
            $native.ExternalNativeTargetCount -ne 0 -or $native.ExceptionRegionCount -ne 0 -or
            $native.FatalMachineFaultSiteCount -ne 0) { throw "Resident compatibility failed for $cpu." }
        & $DotnetPath $runner $hunk $cpu $runtimePath 'loadresource-wb31-actions-runtime'
        if ($LASTEXITCODE) { throw "Runtime qualification failed for $cpu." }
        $runtime = Get-Content -Raw $runtimePath | ConvertFrom-Json
        if ($runtime.status -ne 'passed' -or $runtime.observations.Count -ne 34 -or $runtime.cpu -ne $cpu) {
            throw "Incomplete runtime receipt for $cpu."
        }
        $artifacts += [ordered]@{
            cpu = $cpu
            hunkSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $hunk).Hash.ToLowerInvariant()
            staticSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $staticPath).Hash.ToLowerInvariant()
            runtimeSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $runtimePath).Hash.ToLowerInvariant()
            cases = $runtime.observations.Count
            reachableMethods = $static.ReachableMethodCount
            runtimeFeatures = @($native.RuntimeFeatures)
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
    suite = 'CC20-LoadResource-wb31-actions-runtime'
    status = $status
    failure = $failure
    runtime = 'resident'
    scope = 'Generated HUNK resource actions and source-shaped lazy catalog messages with supplied public Exec/DOS/Utility/Graphics/Locale/Diskfont vectors on 68000/68020/68040. Includes error capture after message CloseCatalog and deliberately uncleared stored pointer. Does not prove guest pointer validity, process integration, original guest behavior, code lifetime, PURE or shipping readiness.'
    sourceSha256 = $sources
    artifacts = $artifacts
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
if ($status -ne 'passed') { throw $failure }
