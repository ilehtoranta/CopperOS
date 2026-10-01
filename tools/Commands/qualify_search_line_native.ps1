param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [string]$DotnetPath = 'dotnet',
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'; Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $repo 'tests\Commands.SearchNativeRoot\CopperOS.Commands.SearchNativeRoot.csproj'
$runnerProject = Join-Path $repo 'tests\Commands.NativeExecution\CopperOS.Commands.NativeExecution.csproj'
$compilerProject = Join-Path $CopperSharpRoot 'Compiler.Cli\CopperSharp.Compiler.Cli.csproj'
$root = Join-Path $repo "tests\Commands.SearchNativeRoot\bin\$Configuration\net10.0"
$assembly = Join-Path $root 'CopperOS.Commands.SearchNativeRoot.dll'; $sdk = Join-Path $root 'CopperSharp.Sdk.Amiga.dll'; $compiler = Join-Path $root 'CopperSharp.Compiler.dll'
$cli = Join-Path $CopperSharpRoot 'Compiler.Cli\bin\Release\net10.0\CopperSharp.Compiler.Cli.dll'; $runner = Join-Path $repo "tests\Commands.NativeExecution\bin\$Configuration\net10.0\CopperOS.Commands.NativeExecution.dll"
$run = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else { Join-Path $root ('qualification-search-line-native\' + [guid]::NewGuid().ToString('N')) }
New-Item -ItemType Directory -Force -Path $run | Out-Null
& $DotnetPath build $compilerProject --configuration $Configuration --nologo; if ($LASTEXITCODE -ne 0) { throw 'CopperSharp compiler CLI build failed.' }
& $DotnetPath build $project --configuration $Configuration --nologo; if ($LASTEXITCODE -ne 0) { throw 'Search native root build failed.' }
& $DotnetPath build $runnerProject --configuration $Configuration --nologo; if ($LASTEXITCODE -ne 0) { throw 'Native execution fixture build failed.' }
$support = Get-ChildItem -LiteralPath (Join-Path $env:USERPROFILE '.nuget\packages\coppersharp.sdk.amiga.support') -Recurse -Filter 'CopperSharp.Sdk.Amiga.Support.dll' | Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (-not $support) { throw 'Amiga support assembly is missing.' }
$artifacts = @()
foreach ($cpu in '68000','68020','68040') {
  $hunk = Join-Path $run "search-line-$cpu.hunk"; $report = "$hunk.compatibility.json"; $runtimeReport = "$hunk.runtime.json"
  & $DotnetPath $cli $assembly '--entry' 'CopperOS.Commands.SearchNativeRoot.SearchLineProbe::Main' '--platform' 'amiga' '--cpu' $cpu '--clr' 'always' '--exceptions' 'yolo' '--format' 'hunk' '--runtime' 'resident' '--memory' 'none' '--peephole' 'disabled' '--managed-assembly' $sdk '--managed-assembly' $support '--managed-assembly' $compiler '--output' $hunk '--compatibility-report' $report
  if ($LASTEXITCODE -ne 0) { throw "Search line compilation failed for $cpu." }
  & $DotnetPath $runner $hunk $cpu $runtimeReport 'search-line-probe-fixture'; if ($LASTEXITCODE -ne 0) { throw "Search line execution failed for $cpu." }
  $static = Get-Content -Raw $report | ConvertFrom-Json; $runtime = Get-Content -Raw $runtimeReport | ConvertFrom-Json; $native = $static.NativeCompatibility
  if (-not $static.IsCompatible -or $static.ReachableMethodCount -ne 19 -or $native.RuntimeFeatureCount -ne 0 -or $native.RuntimeHelperCount -ne 0 -or $native.ExternalNativeTargetCount -ne 0 -or $native.ExceptionRegionCount -ne 0 -or $native.FatalMachineFaultSiteCount -ne 0) { throw "Search line native purity compatibility is incomplete for $cpu." }
  if ($runtime.status -ne 'passed' -or $runtime.suite -ne 'search-line-probe-fixture' -or $runtime.cpu -ne $cpu -or $runtime.passed -ne 10 -or $runtime.sharedImageWrites -ne 0 -or $runtime.nativeWrites -le 0 -or $runtime.nativeReads -le 0) { throw "Search line native execution receipt is incomplete for $cpu." }
  $file = Get-Item $hunk; $artifacts += [ordered]@{ cpu=$cpu; bytes=$file.Length; sha256=(Get-FileHash -Algorithm SHA256 $hunk).Hash.ToLowerInvariant(); reachableMethods=$static.ReachableMethodCount; suppliedVectorInvocations=$runtime.passed; runtimeReport=(Split-Path -Leaf $runtimeReport) }
}
[ordered]@{ schemaVersion=1; suite='CC11-Search-line-native'; status='passed'; runtime='resident'; entry='CopperOS.Commands.SearchNativeRoot.SearchLineProbe::Main'; scope='Static three-CPU pure/resident compilation and direct control-block execution of bounded Search literal line formatting. No DOS parser, locale, traversal, file I/O, packaged command, or original-command parity claim.'; artifacts=$artifacts } | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $run 'qualification.json') -Encoding utf8
Write-Host 'PASS Search line native: 68000/020/040 resident HUNKs and direct control-block execution.'
