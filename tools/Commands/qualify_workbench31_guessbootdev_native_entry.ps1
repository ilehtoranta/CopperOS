param(
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\CopperOS.Commands.AddBuffersNativeRoot.csproj'
$runnerProject = Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj'
$root = Join-Path $repo 'tests\Commands.AddBuffersNativeRoot\bin\Release\net10.0'
$rootDll = Join-Path $root 'CopperOS.Commands.AddBuffersNativeRoot.dll'
$runner = Join-Path $repo 'tests\Commands.NativeExecution\bin\Release\net10.0\CopperOS.Commands.NativeExecution.dll'
$cli = if (Test-Path (Join-Path $CopperSharpRoot 'CopperSharp.Compiler.Cli.dll')) {
    Join-Path $CopperSharpRoot 'CopperSharp.Compiler.Cli.dll'
} else {
    Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'
}
$run = [IO.Path]::GetFullPath($OutputDirectory)
if ((Test-Path -LiteralPath $run) -and @(Get-ChildItem -LiteralPath $run -Force).Count -ne 0) {
    throw 'Use a fresh output directory; existing qualification evidence must be preserved.'
}
New-Item -ItemType Directory -Force -Path $run | Out-Null

& $DotnetPath build $project -c Release --nologo --no-restore -p:BuildProjectReferences=false
if ($LASTEXITCODE) { throw 'Native command root build failed.' }
& $DotnetPath build $runnerProject -c Release --nologo --no-restore -p:BuildProjectReferences=false
if ($LASTEXITCODE) { throw 'Native fixture runner build failed.' }
if (!(Test-Path -LiteralPath $cli)) { throw 'CopperSharp compiler CLI is missing.' }
$support = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$support) { throw 'CopperSharp Amiga support assembly missing.' }

$artifacts = @()
foreach ($cpu in '68000', '68020', '68040') {
    $hunk = Join-Path $run "guessbootdev-wb31-$cpu.hunk"
    $static = "$hunk.compatibility.json"
    $runtime = "$hunk.runtime.json"
    & $DotnetPath $cli $rootDll --entry 'CopperOS.Commands.AddBuffersNativeRoot.Workbench31GuessBootDevEntry::Main' --platform amiga --cpu $cpu --clr always --exceptions yolo --format hunk --runtime resident --memory none --peephole disabled --managed-assembly (Join-Path $root 'CopperSharp.Sdk.Amiga.dll') --managed-assembly $support --managed-assembly (Join-Path $root 'CopperSharp.Compiler.dll') --output $hunk --compatibility-report $static
    if ($LASTEXITCODE) { throw "Native HUNK compilation failed for $cpu." }
    & $DotnetPath $runner $hunk $cpu $runtime 'workbench31-guessbootdev-native-entry-vector-fixture'
    if ($LASTEXITCODE) { throw "Native fixture failed for $cpu." }
    $s = Get-Content -Raw $static | ConvertFrom-Json
    $r = Get-Content -Raw $runtime | ConvertFrom-Json
    $n = $s.NativeCompatibility
    if (!$s.IsCompatible -or $s.RootMethodCount -ne 1 -or $s.ReachableMethodCount -le 0 -or
        @($s.ManagedAllocationSites).Count -ne 0 -or $n.RuntimeFeatureCount -ne 0 -or
        $n.RuntimeHelperCount -ne 0 -or $n.ExternalNativeTargetCount -ne 0 -or
        $n.ExceptionRegionCount -ne 0 -or $n.FatalMachineFaultSiteCount -ne 0 -or
        $r.status -ne 'passed' -or $r.passed -ne 16 -or $r.sharedImageWrites -ne 0 -or
        $r.nativeWrites -le 0) { throw "Qualification receipt incomplete for $cpu." }
    $file = Get-Item -LiteralPath $hunk
    $artifacts += [ordered]@{
        cpu = $cpu
        bytes = $file.Length
        sha256 = (Get-FileHash -LiteralPath $hunk -Algorithm SHA256).Hash.ToLowerInvariant()
        reachableMethods = $s.ReachableMethodCount
        suppliedVectorInvocations = $r.passed
    }
}

[ordered]@{
    schemaVersion = 1
    suite = 'CC22.GuessBootDev.wb31-native-entry-candidate'
    status = 'passed'
    runtime = 'resident'
    entry = 'CopperOS.Commands.AddBuffersNativeRoot.Workbench31GuessBootDevEntry::Main'
    sourceBinary = [ordered]@{
        path = 'D:/TestData/TestImages/Workbench v3.1 rev 40.42 (1994)(Commodore)(M10)(Disk 1 of 6)(Install)[!].zip::C/GuessBootDev'
    version = '$VER: guessbootdev 39.3 (6.8.92)'
        sha256 = 'b0f61f0852e877b4e7b219ca2ea5a1d55bd9c5b9c5e13b643d5ca3b10d9dcf88'
    }
    scope = 'Three-CPU resident HUNK compilation plus sixteen supplied Exec/DOS/utility.library/expansion.library vectors per CPU for the Workbench 3.1 GuessBootDev installer command. Vectors cover boot-node priority selection, signed priorities, disabled and unusable filesystems, lock fallback, parser and library-open failures, startup guards, missing DOS and interleaved callers. Exact original diagnostics, installed PURE admission, guest differential and package closure remain open.'
    runtimeFixture = $true
    artifacts = $artifacts
} | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
Write-Output (Join-Path $run 'qualification.json')
