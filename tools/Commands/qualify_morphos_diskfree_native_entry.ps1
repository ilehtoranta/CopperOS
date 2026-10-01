param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = '',
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if ([string]::IsNullOrWhiteSpace($CopperSharpRoot)) {
    $CopperSharpRoot = (Resolve-Path (Join-Path $repo '..\CopperSharp68k')).Path
}
$project = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\CopperOS.Commands.AddBuffersNativeRoot.csproj'
$root = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\bin\Release\net10.0'
$rootDll = Join-Path $root 'CopperOS.Commands.AddBuffersNativeRoot.dll'
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
$run = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $repo 'artifacts\diskfree-morphos-native-20260919-static'
}
New-Item -ItemType Directory -Force $run | Out-Null

& $DotnetPath build (Join-Path $CopperSharpRoot 'Compiler.Cli\CopperSharp.Compiler.Cli.csproj') -c Release --nologo
if ($LASTEXITCODE) { throw 'Compiler build failed.' }
& $DotnetPath build $project -c Release --nologo
if ($LASTEXITCODE) { throw 'Root build failed.' }

$support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'CopperSharp Amiga support assembly missing.' }

$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $run "diskfree-morphos-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    & $DotnetPath $cli $rootDll '--entry' 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSDiskFreeEntry::Main' `
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
        $n.RuntimeHelperCount -ne 0 -or $n.ExternalNativeTargetCount -ne 0 -or
        $n.ExceptionRegionCount -ne 0 -or $n.FatalMachineFaultSiteCount -ne 0) {
        throw "Resident compatibility receipt incomplete for $cpu."
    }
    $file = Get-Item $hunk
    $artifacts += [ordered]@{
        cpu = $cpu
        bytes = $file.Length
        sha256 = (Get-FileHash $hunk -Algorithm SHA256).Hash.ToLowerInvariant()
        reachableMethods = $s.ReachableMethodCount
        runtimeFeatures = @($n.RuntimeFeatures)
    }
}

[ordered]@{
    schemaVersion = 1
    suite = 'CC11-DiskFree-morphos320-native-static'
    status = 'passed'
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSDiskFreeEntry::Main'
    scope = 'Three-CPU resident HUNK compilation for the exact MorphOS DiskFree template VOLUME,NOPOSTFIX/S,PERCENT/S, guest-owned ReadArgs/InfoData storage, public DOS Lock/Info ownership, numeric byte/KB/MB/GB and percentage modes. It proves static ABI lowering only; exact localized output, large-volume provider behavior, original guest comparison, PURE/resident lifecycle and package admission remain open.'
    artifacts = $artifacts
} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
