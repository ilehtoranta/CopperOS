param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\..\..\CopperSharp68k'),
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$project = Join-Path $PSScriptRoot 'CopperOS.Workbench31GuestProbe.csproj'
$binaryRoot = Join-Path $PSScriptRoot 'bin\Release\net10.0'
$assembly = Join-Path $binaryRoot 'CopperOS.Workbench31GuestProbe.dll'
$cliProject = Join-Path $CopperSharpRoot 'Compiler.Cli\CopperSharp.Compiler.Cli.csproj'
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
$run = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else {
    Join-Path $repo ('artifacts\workbench31-guest-probe-native-' + [guid]::NewGuid().ToString('N'))
}
if (Test-Path -LiteralPath $run) { throw 'Use a fresh output directory; preserve historical build evidence.' }
New-Item -ItemType Directory -Path $run | Out-Null
$status = 'failed'
$failure = $null
$artifact = $null
$toolchain = @()
$sourcePaths = @(
    'tools\Commands\Workbench31GuestProbe\native\NativeGuestCommandProbe.cs',
    'tools\Commands\Workbench31GuestProbe\native\CopperOS.Workbench31GuestProbe.csproj',
    'tools\Commands\Workbench31GuestProbe\native\build_probe.ps1',
    'src\Commands\Common\NativeCommandArguments.cs',
    'CopperOS.Portable.props'
)
$sources = @($sourcePaths | ForEach-Object {
    [ordered]@{ path = $_; sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $repo $_)).Hash.ToLowerInvariant() }
})
try {
    foreach ($buildProject in $cliProject, $project) {
        & $DotnetPath build $buildProject -c Release --nologo
        if ($LASTEXITCODE) { throw "Build failed: $buildProject" }
    }
    $support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
        Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
    if (!$support) { throw 'CopperSharp Amiga support assembly missing.' }
    $toolchain = @($cli, (Join-Path $binaryRoot 'CopperSharp.Compiler.dll'),
        (Join-Path $binaryRoot 'CopperSharp.Sdk.Amiga.dll'), $support) | ForEach-Object {
        [ordered]@{ path = [IO.Path]::GetFullPath($_); sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $_).Hash.ToLowerInvariant() }
    }
    $hunk = Join-Path $run 'CopperProbe'
    $staticPath = Join-Path $run 'CopperProbe.compatibility.json'
    & $DotnetPath $cli $assembly '--entry' 'CopperOS.Workbench31GuestProbe.NativeGuestCommandProbe::Main' `
        '--platform' 'amiga' '--cpu' '68000' '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' `
        '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' `
        '--managed-assembly' (Join-Path $binaryRoot 'CopperSharp.Sdk.Amiga.dll') `
        '--managed-assembly' $support '--managed-assembly' (Join-Path $binaryRoot 'CopperSharp.Compiler.dll') `
        '--output' $hunk '--compatibility-report' $staticPath
    if ($LASTEXITCODE) { throw '68000 probe HUNK compilation failed.' }
    $static = Get-Content -Raw -LiteralPath $staticPath | ConvertFrom-Json
    $native = $static.NativeCompatibility
    if (!$static.IsCompatible -or $static.ReachableMethodCount -lt 8 -or
        @($static.Members).Count -ne 0 -or @($static.ManagedAllocationSites).Count -ne 0 -or
        $native.RuntimeFeatureCount -ne 0 -or $native.RuntimeHelperCount -ne 0 -or
        $native.ExternalNativeTargetCount -ne 0 -or $native.ExceptionRegionCount -ne 0 -or
        $native.FatalMachineFaultSiteCount -ne 0) { throw 'Probe native compatibility requirements failed.' }
    foreach ($source in $sources) {
        if ((Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $repo $source.path)).Hash.ToLowerInvariant() -ne $source.sha256) {
            throw ('Source changed during build: ' + $source.path)
        }
    }
    foreach ($tool in $toolchain) {
        if ((Get-FileHash -Algorithm SHA256 -LiteralPath $tool.path).Hash.ToLowerInvariant() -ne $tool.sha256) {
            throw ('Toolchain changed during HUNK compilation: ' + $tool.path)
        }
    }
    $artifact = [ordered]@{
        path = $hunk
        cpu = '68000'
        bytes = (Get-Item -LiteralPath $hunk).Length
        sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $hunk).Hash.ToLowerInvariant()
        compatibilityPath = $staticPath
        compatibilitySha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $staticPath).Hash.ToLowerInvariant()
        assemblySha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $assembly).Hash.ToLowerInvariant()
        reachableMethods = $static.ReachableMethodCount
        runtimeFeatures = @($native.RuntimeFeatures)
    }
    $status = 'passed'
}
catch { $failure = $_.ToString() }
[ordered]@{
    schemaVersion = 1
    suite = 'Workbench31-authored-guest-command-probe-native-build'
    status = $status
    failure = $failure
    originalGuestExecution = $false
    suppliedVectorExecution = $false
    commandParityProven = $false
    scope = 'Authored CLI guest probe compilation only. Publishes a public Exec port/allocated record, uses public DOS ReadArgs and synchronous SystemTagList with NIL input and RAM output, captures up to4096 bytes, and retains code/storage until diagnostic reboot. Post-System IoErr is not proven to be child Result2. No emulator or media mutation performed by this build script.'
    sources = $sources
    toolchain = @($toolchain)
    artifact = $artifact
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $run 'build-receipt.json') -Encoding utf8
Write-Output (Join-Path $run 'build-receipt.json')
if ($status -ne 'passed') { throw $failure }
