param([string]$DotnetPath='dotnet',[string]$OutputDirectory)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$root=Join-Path $repo 'tests/Commands.AddBuffersNativeRoot/bin/Release/net10.0'
$cli=Join-Path $repo '../CopperSharp68k/Compiler.Cli/bin/Release/net10.0/CopperSharp.Compiler.Cli.dll'
$run=if($OutputDirectory){[IO.Path]::GetFullPath($OutputDirectory)}else{Join-Path $repo 'artifacts/qualification-copy-explicit-parser'}
New-Item -ItemType Directory -Force $run | Out-Null
foreach($project in 'tests/Commands.AddBuffersNativeRoot/CopperOS.Commands.AddBuffersNativeRoot.csproj','tests/Commands.NativeExecution/CopperOS.Commands.NativeExecution.csproj') {
 & $DotnetPath build (Join-Path $repo $project) -c Release --nologo
 if($LASTEXITCODE){throw 'Build failed'}
}
$support=Get-ChildItem "$env:USERPROFILE/.nuget/packages/coppersharp.sdk.amiga.support" -Recurse -Filter CopperSharp.Sdk.Amiga.Support.dll | Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
$artifacts=@()
foreach($cpu in '68000','68020','68040') {
 $hunk=Join-Path $run "copy-$cpu.hunk"
 & $DotnetPath $cli "$root/CopperOS.Commands.AddBuffersNativeRoot.dll" --entry CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSCopyExplicitParserOperationsEntry::Main --platform amiga --cpu $cpu --clr always --exceptions yolo --format hunk --runtime resident --memory none --peephole disabled --managed-assembly "$root/CopperSharp.Sdk.Amiga.dll" --managed-assembly $support --managed-assembly "$root/CopperSharp.Compiler.dll" --output $hunk --compatibility-report "$hunk.static.json"
 if($LASTEXITCODE){throw 'Native compile failed'}
 $r=Get-Content "$hunk.static.json" -Raw | ConvertFrom-Json
 if(!$r.IsCompatible -or $r.ReachableMethodCount -ne 61 -or $r.NativeCompatibility.RuntimeFeatureCount -ne 0 -or $r.NativeCompatibility.RuntimeHelperCount -ne 0 -or $r.NativeCompatibility.ExternalNativeTargetCount -ne 0 -or $r.NativeCompatibility.ExceptionRegionCount -ne 0 -or $r.NativeCompatibility.FatalMachineFaultSiteCount -ne 0){throw 'Static checks failed'}
 foreach($branch in 'delete','makedir','direct') {
  $suite=if($branch -eq 'direct'){'copy-direct-native-entry-vector-fixture'}else{"copy-parsed-$branch-native-entry-vector-fixture"}
  $report=Join-Path $run "$branch-$cpu.runtime.json"
  & $DotnetPath "$repo/tests/Commands.NativeExecution/bin/Release/net10.0/CopperOS.Commands.NativeExecution.dll" $hunk $cpu $report "$suite+explicit-parser"
  if($LASTEXITCODE){throw 'Runtime failed'}
  $e=Get-Content $report -Raw | ConvertFrom-Json
  $expected=if($branch -eq 'delete'){10}elseif($branch -eq 'direct'){9}else{7}
  if($e.status -ne 'passed' -or $e.passed -ne $expected -or $e.sharedImageWrites -ne 0){throw 'Incomplete runtime receipt'}
 }
 $artifacts+=@{cpu=$cpu;sha256=(Get-FileHash $hunk).Hash;invocations=26}
}
@{status='passed';scope='Explicit RDArgs/help/cleanup operation root: DELETE, MAKEDIR and DIRECT matrices only. Normal COPY/MOVE/LINK target branches and shipping lifecycle remain open.';artifacts=$artifacts} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $run 'qualification.json')
