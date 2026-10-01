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
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
$run = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $root ('qualification-wait-wb31-native\' + [guid]::NewGuid().ToString('N'))
}
New-Item -ItemType Directory -Force $run | Out-Null

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
    $hunk = Join-Path $run "wait-wb31-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    $runtime = "$hunk.runtime.json"
    & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.AddBuffersNativeRoot.NativeWorkbench31WaitEntry::Main' `
        '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
        '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') `
        '--managed-assembly' $support `
        '--managed-assembly' (Join-Path $root 'CopperSharp.Compiler.dll') `
        '--output' $hunk '--compatibility-report' $static
    if ($LASTEXITCODE) { throw "Native HUNK compilation failed for $cpu." }
    & $DotnetPath (Join-Path $repo 'tests\Commands.NativeExecution\bin\Release\net10.0\CopperOS.Commands.NativeExecution.dll') `
        $hunk $cpu $runtime 'workbench31-wait-native-entry-vector-fixture'
    if ($LASTEXITCODE) { throw "Native fixture failed for $cpu." }

    $s = Get-Content -Raw $static | ConvertFrom-Json
    $r = Get-Content -Raw $runtime | ConvertFrom-Json
    $n = $s.NativeCompatibility
    if (!$s.IsCompatible -or $s.RootMethodCount -ne 1 -or
        $s.ReachableMethodCount -le 0 -or @($s.ManagedAllocationSites).Count -ne 0 -or
        $n.RuntimeFeatureCount -ne 0 -or $n.RuntimeHelperCount -ne 0 -or
        $n.ExternalNativeTargetCount -ne 0 -or $n.ExceptionRegionCount -ne 0 -or
        $n.FatalMachineFaultSiteCount -ne 0 -or $r.status -ne 'passed' -or
        $r.passed -ne 17 -or $r.sharedImageWrites -ne 0 -or $r.nativeWrites -le 0) {
        throw "Qualification receipt incomplete for $cpu."
    }
    $file = Get-Item $hunk
    $artifacts += [ordered]@{
        cpu = $cpu
        bytes = $file.Length
        sha256 = (Get-FileHash $hunk -Algorithm SHA256).Hash.ToLowerInvariant()
        reachableMethods = $s.ReachableMethodCount
        suppliedVectorInvocations = $r.passed
    }
}

[ordered]@{
    schemaVersion = 1
    suite = 'CC16-Wait-wb31-native-entry'
    status = 'passed'
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.NativeWorkbench31WaitEntry::Main'
    scope = 'Three-CPU resident HUNK compilation plus supplied-DOS/Exec execution of the bounded Workbench 3.1 Wait ReadArgs, default/seconds/minutes conversion, HH:MM UNTIL parsing, VBlank timer scheduling, Ctrl-C cancellation, startup and missing-DOS boundaries, ownership and diagnostics. Exact original guest comparison, lifecycle, purity and packaging remain open.'
    artifacts = $artifacts
} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
