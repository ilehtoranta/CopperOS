param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$qualifier = Join-Path $repo 'tools\Commands\qualify_native.ps1'
$testDirectory = Join-Path (Join-Path $PSScriptRoot 'obj\qualification-script-regressions') ([guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $testDirectory -Force)
$latest = Join-Path $repo "tests\Commands.NativeRoot\bin\$Configuration\net10.0\qualification\latest.json"
$latestBefore = if (Test-Path -LiteralPath $latest) { (Get-FileHash -LiteralPath $latest -Algorithm SHA256).Hash } else { $null }
$results = @()

function Convert-HexToBytes([string]$hex) {
    $fromHex = [Convert].GetMethod('FromHexString', [Type[]]@([string]))
    if ($null -ne $fromHex) { return [Convert]::FromHexString($hex) }
    if (($hex.Length % 2) -ne 0) { throw 'Hex fixture must contain an even number of characters.' }
    $bytes = [byte[]]::new($hex.Length / 2)
    for ($index = 0; $index -lt $bytes.Length; $index++) {
        $bytes[$index] = [Convert]::ToByte($hex.Substring($index * 2, 2), 16)
    }
    return $bytes
}

foreach ($scenario in @('bootstrap-failure', 'source-drift', 'source-inventory-drift')) {
    $fixture = Join-Path $testDirectory $scenario
    $compilerFixture = Join-Path $fixture 'CopperSharp68k'
    foreach ($name in @('Compiler', 'Compiler.Cli', 'Targets.Amiga', 'Sdk.Amiga', 'Runtime.Managed', 'Runtime.AmigaPal')) {
        $directory = Join-Path $compilerFixture $name
        [void](New-Item -ItemType Directory -Path $directory -Force)
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>' |
            Set-Content -LiteralPath (Join-Path $directory "CopperSharp.$name.csproj") -Encoding utf8
    }
    '// Initial source bytes must remain stable through bootstrap.' |
        Set-Content -LiteralPath (Join-Path $compilerFixture 'Compiler\QualificationDrift.cs') -Encoding utf8
    $action = if ($scenario -eq 'bootstrap-failure') {
        '<Error Text="CC04 expected bootstrap failure" />'
    } elseif ($scenario -eq 'source-drift') {
        '<WriteLinesToFile File="../Compiler/QualificationDrift.cs" Lines="// Deliberately changed during bootstrap." Overwrite="true" />'
    } else {
        '<WriteLinesToFile File="../Compiler/AdditionalSource.cs" Lines="// Deliberately added during bootstrap." Overwrite="true" />'
    }
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <Target Name="QualificationRegression" BeforeTargets="BeforeBuild">$action</Target>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $compilerFixture 'Compiler.Cli\CopperSharp.Compiler.Cli.csproj') -Encoding utf8

    # The child qualifier must fail for these scenarios. Temporarily allow
    # its stderr stream through so PowerShell returns the durable receipt and
    # exit code to the assertions below instead of terminating the harness.
    $savedErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = @(& pwsh -NoLogo -NoProfile -File $qualifier -Configuration $Configuration -CopperSharpRoot $compilerFixture 2>&1)
    }
    finally {
        $ErrorActionPreference = $savedErrorActionPreference
    }
    $exitCode = $LASTEXITCODE
    $output | Set-Content -LiteralPath (Join-Path $fixture 'qualifier.log') -Encoding utf8
    $attemptLine = @($output | ForEach-Object ToString | Where-Object { $_.StartsWith('Qualification attempt: ') })
    if ($attemptLine.Count -ne 1) { throw "$scenario did not create an initial attempt receipt. See $fixture" }
    $attemptPath = $attemptLine[0].Substring('Qualification attempt: '.Length)
    $attempt = Get-Content -LiteralPath $attemptPath -Raw | ConvertFrom-Json
    if ($exitCode -eq 0 -or $attempt.status -ne 'failed' -or $attempt.allPassed -or
        $attempt.failure.stage -ne 'bootstrap/compiler' -or $attempt.artifacts.Count -ne 0 -or
        $attempt.shippingCommandsQualified -ne 0 -or $attempt.inputManifest.sourceFiles -eq 0) {
        throw "$scenario did not fail closed during compiler bootstrap. See $attemptPath"
    }
    $buildStage = @($attempt.stages | Where-Object { $_.name -eq 'bootstrap/compiler' })
    if ($buildStage.Count -ne 1 -or $buildStage[0].status -ne 'failed' -or -not (Test-Path -LiteralPath $buildStage[0].log)) {
        throw "$scenario omitted its failed stage or durable build log."
    }
    if ($scenario -eq 'bootstrap-failure') {
        if ($buildStage[0].exitCode -ne 1 -or $attempt.failure.message -notmatch 'exit code 1') {
            throw 'The deliberate bootstrap failure lost its native exit code.'
        }
    } elseif ($buildStage[0].exitCode -ne 0 -or $attempt.failure.message -notmatch 'Input binding drift' -or
        @($attempt.inputChecks | Where-Object { $_.status -eq 'failed' }).Count -ne 1) {
        throw 'A successful build that mutated a bound source was not rejected as drift.'
    }
    $latestAfter = if (Test-Path -LiteralPath $latest) { (Get-FileHash -LiteralPath $latest -Algorithm SHA256).Hash } else { $null }
    if ($latestBefore -ne $latestAfter) { throw 'A failed attempt replaced the successful latest receipt.' }
    $results += [ordered]@{
        scenario = $scenario; status = 'passed'; qualifierExitCode = $exitCode
        attempt = $attemptPath; sha256 = (Get-FileHash -LiteralPath $attemptPath -Algorithm SHA256).Hash.ToLowerInvariant()
        buildExitCode = $buildStage[0].exitCode; successfulLatestUnchanged = $true
    }
    Write-Output "PASS ${scenario}: durable failure receipt, exact stage/exit, no artifact or latest admission."
}

# Exercise the exact file/snapshot guards from the qualifier against disposable
# binary inputs. This invokes function definitions only, never the qualifier's
# top-level bootstrap and never the real compiler/native artifacts.
$guardResults = & {
    $tokens = $null
    $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile($qualifier, [ref]$tokens, [ref]$errors)
    if ($errors.Count -ne 0) { throw 'The qualification script has parser errors.' }
    $requiredFunctions = @('Convert-BytesToHex', 'Get-BytesSha256Hex', 'Get-FileIdentity', 'Copy-BoundInput', 'Assert-BoundFiles', 'Assert-InputBinding', 'Assert-FrameworkClosure',
        'Read-CommandHunkWord', 'Read-CommandHunkForTailAudit', 'Get-CommandMapMetric', 'Assert-NativeHelperClosure')
    foreach ($name in $requiredFunctions) {
        $definition = @($ast.FindAll({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] }, $true) |
            Where-Object Name -EQ $name)
        if ($definition.Count -ne 1) { throw "Cannot identify qualification guard $name" }
        Invoke-Expression $definition[0].Extent.Text
    }
    $sourceInputs = @(); $restoreInputs = @(); $hostInputs = @(); $referenceInputs = @()
    $summary = @{ inputChecks = [Collections.Generic.List[object]]::new() }
    $snapshotDirectory = Join-Path $testDirectory 'binary-guard-fixture'
    [void](New-Item -ItemType Directory -Path $snapshotDirectory -Force)
    $livePath = Join-Path $snapshotDirectory 'live-input.dll'
    $copyPath = Join-Path $snapshotDirectory 'binary\executor\fixture.dll'
    [IO.File]::WriteAllBytes($livePath, [byte[]]@(1, 2, 3, 4))
    $binaryInputs = @(Copy-BoundInput $livePath $copyPath 'fixture-only')
    Assert-InputBinding 'binary-guard/baseline'
    foreach ($scenario in @('snapshot-byte-drift', 'live-binary-byte-drift', 'snapshot-inventory-drift')) {
        if ($scenario -eq 'snapshot-byte-drift') { [IO.File]::WriteAllBytes($copyPath, [byte[]]@(4, 3, 2, 1)) }
        elseif ($scenario -eq 'live-binary-byte-drift') { [IO.File]::WriteAllBytes($livePath, [byte[]]@(4, 3, 2, 1)) }
        else { [IO.File]::WriteAllBytes((Join-Path (Split-Path -Parent $copyPath) 'unbound.dll'), [byte[]]@(0)) }
        $rejected = $false
        try { Assert-InputBinding $scenario }
        catch [IO.InvalidDataException] { $rejected = $true }
        if (-not $rejected -or $summary.inputChecks[-1].status -ne 'failed') { throw "$scenario was not rejected by the actual input guard." }
        [IO.File]::WriteAllBytes($livePath, [byte[]]@(1, 2, 3, 4))
        [IO.File]::WriteAllBytes($copyPath, [byte[]]@(1, 2, 3, 4))
        [ordered]@{ scenario = $scenario; status = 'passed'; fixtureOnly = $true }
        Write-Host "PASS ${scenario}: actual qualification guard rejects changed consumed inputs."
    }
    # These bytes are disposable test data, not a licensed reference image.
    # References are intentionally not copied; exercise that separate guard.
    $binaryInputs = @()
    $referencePath = Join-Path $snapshotDirectory 'disposable-reference.bin'
    [IO.File]::WriteAllBytes($referencePath, [byte[]]@(5, 6, 7, 8))
    $referenceInputs = @(Get-FileIdentity $referencePath 'fixture-reference-only')
    Assert-InputBinding 'private-reference-guard/baseline'
    [IO.File]::WriteAllBytes($referencePath, [byte[]]@(8, 7, 6, 5))
    $rejected = $false
    try { Assert-InputBinding 'private-reference-byte-drift' }
    catch [IO.InvalidDataException] { $rejected = $true }
    if (-not $rejected -or $summary.inputChecks[-1].status -ne 'failed') {
        throw 'Changed private reference bytes were not rejected by the actual input guard.'
    }
    [ordered]@{ scenario = 'private-reference-byte-drift'; status = 'passed'; fixtureOnly = $true; referenceCopied = $false }
    Write-Host 'PASS private-reference-byte-drift: changed external input rejected without copying reference bytes.'

    # MakeDir's public SDK lock type requires two intrinsic nullable members.
    # Extending that precise boundary must not admit runtime helpers, different
    # pointer types, duplicate members, allocation effects or other features.
    $nullableFixture = @{
        Members = @(
            @{
                Member = @{ AssemblyName = 'System.Runtime'; TypeName = 'System.Nullable<Amiga.BPTR>';
                    Name = 'get_HasValue'; IsStatic = $false; GenericArity = 0; ReturnType = 'bool';
                    ParameterTypes = @(); MethodTypeArguments = @() }
                Status = 'intrinsic'; Binding = 'intrinsic:nullable-has-value:Amiga.BPTR'
                Effects = @(); RequiredFeatures = @('nullable-values')
            },
            @{
                Member = @{ AssemblyName = 'System.Runtime'; TypeName = 'System.Nullable<Amiga.BPTR>';
                    Name = 'get_Value'; IsStatic = $false; GenericArity = 0; ReturnType = '!0';
                    ParameterTypes = @(); MethodTypeArguments = @() }
                Status = 'intrinsic'; Binding = 'intrinsic:nullable-get-value:Amiga.BPTR'
                Effects = @('MayThrow'); RequiredFeatures = @('nullable-values')
            }
        )
        NativeCompatibility = @{ RuntimeFeatureCount = 1; RuntimeFeatures = @('nullable-values') }
    } | ConvertTo-Json -Depth 8
    Assert-FrameworkClosure ($nullableFixture | ConvertFrom-Json) 'Workbench31MakeDir'
    foreach ($scenario in @('nullable-runtime-binding', 'nullable-pointer-type-drift', 'nullable-duplicate-member',
        'nullable-allocation-effect', 'nullable-feature-drift', 'nullable-outside-makedir')) {
        $probe = $nullableFixture | ConvertFrom-Json
        $component = 'Workbench31MakeDir'
        switch ($scenario) {
            'nullable-runtime-binding' { $probe.Members[1].Status = 'runtime-helper' }
            'nullable-pointer-type-drift' {
                $probe.Members[1].Member.TypeName = 'System.Nullable<Amiga.APTR>'
                $probe.Members[1].Binding = 'intrinsic:nullable-get-value:Amiga.APTR'
            }
            'nullable-duplicate-member' { $probe.Members[1] = $probe.Members[0] }
            'nullable-allocation-effect' { $probe.Members[1].Effects += 'Allocates' }
            'nullable-feature-drift' {
                $probe.NativeCompatibility.RuntimeFeatures += 'managed-objects'
                $probe.NativeCompatibility.RuntimeFeatureCount = 2
            }
            'nullable-outside-makedir' { $component = 'Foundation' }
        }
        $rejected = $false
        try { Assert-FrameworkClosure $probe $component }
        catch [InvalidOperationException] { $rejected = $true }
        if (-not $rejected) { throw "$scenario escaped the actual framework closure guard." }
        [ordered]@{ scenario = $scenario; status = 'passed'; fixtureOnly = $true }
        Write-Host "PASS ${scenario}: exact BPTR intrinsic admission rejects expanded runtime scope."
    }

    # Construct a small disposable HUNK with genuine HUNK_SYMBOL records. It
    # tests the production admission guard, not an alternate test-only parser.
    # These are structural fixtures, not native execution or command evidence.
    function Add-TailFixtureWord([Collections.Generic.List[byte]]$bytes, [uint32]$value) {
        $bytes.Add([byte](($value -shr 24) -band 255))
        $bytes.Add([byte](($value -shr 16) -band 255))
        $bytes.Add([byte](($value -shr 8) -band 255))
        $bytes.Add([byte]($value -band 255))
    }
    function New-TailFixture {
        [ordered]@{
            Code = Convert-HexToBytes '48E73832518F600A48E73832518F60024E752002508F4CDF4C1C4E75646F732E6C69627261727900'
            Symbols = @(
                @{ Name = 'CopperOS.Commands.Native.NativeCommandIo::ReadOnce'; Offset = 0; Size = 8 },
                @{ Name = 'CopperOS.Commands.Native.NativeCommandIo::WriteOnce'; Offset = 8; Size = 8 },
                @{ Name = 'CopperOS.Commands.Native.NativeCommandIo::IsCtrlCPending'; Offset = 16; Size = 2 }
            )
            Relocations = @(); CodeBoundary = 28; BssBytes = 0
            MapHelper = '__c68k_shared_epilogue_97FEB6A1E88C94DE'
            Report = @{ NativeCompatibility = @{ RuntimeHelperCount = 1; RuntimeHelpers = @('__c68k_shared_epilogue_97FEB6A1E88C94DE') } }
            Component = 'Foundation'; Suite = 'command-io-vector-fixture'
        }
    }
    function Write-TailFixture([object]$fixture, [string]$scenario) {
        $directory = Join-Path $testDirectory $scenario
        [void](New-Item -ItemType Directory -Path $directory -Force)
        $path = Join-Path $directory 'structural-fixture.hunk'
        $bytes = [Collections.Generic.List[byte]]::new()
        $words = [uint32][Math]::Ceiling($fixture.Code.Length / 4.0)
        foreach ($word in @(0x3f3, 0, 1, 0, 0, $words, 0x3e9, $words)) { Add-TailFixtureWord $bytes $word }
        $bytes.AddRange([byte[]]$fixture.Code)
        while (($bytes.Count % 4) -ne 0) { $bytes.Add(0) }
        if ($fixture.Relocations.Count -gt 0) {
            foreach ($word in @(0x3ec, $fixture.Relocations.Count, 0) + $fixture.Relocations + @(0)) {
                Add-TailFixtureWord $bytes $word
            }
        }
        Add-TailFixtureWord $bytes 0x3f0
        foreach ($symbol in $fixture.Symbols) {
            $name = [Text.Encoding]::ASCII.GetBytes($symbol.Name)
            Add-TailFixtureWord $bytes ([uint32][Math]::Ceiling($name.Length / 4.0))
            $bytes.AddRange($name)
            while (($bytes.Count % 4) -ne 0) { $bytes.Add(0) }
            Add-TailFixtureWord $bytes $symbol.Offset
        }
        Add-TailFixtureWord $bytes 0
        Add-TailFixtureWord $bytes 0x3f2
        [IO.File]::WriteAllBytes($path, $bytes.ToArray())
        $map = @(
            'ENTRY 00000000'
            "METRICS artifact-bytes=$($bytes.Count) code-bytes=$($fixture.Code.Length) rom-bytes=$($fixture.Code.Length) rom-code-bytes=$($fixture.CodeBoundary) rom-rodata-bytes=$($fixture.Code.Length - $fixture.CodeBoundary) initialized-ram-bytes=0 bss-bytes=$($fixture.BssBytes) symbols=$($fixture.Symbols.Count) relocations=$($fixture.Relocations.Count)"
            'SYMBOLS'
            foreach ($symbol in $fixture.Symbols) { '{0:X8} {1} {2}' -f $symbol.Offset, $symbol.Size, $symbol.Name }
            'RUNTIME HELPERS'
            $fixture.MapHelper
            'EXTERNAL NATIVE TARGETS'
        ) -join "`n"
        $map | Set-Content -LiteralPath "$path.map" -Encoding utf8
        $fixture.Report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath "$path.compatibility.json" -Encoding utf8
        return [ordered]@{ Path = $path; Map = $map }
    }

    $fixture = New-TailFixture
    $written = Write-TailFixture $fixture 'tail-exact-admission'
    $accepted = Assert-NativeHelperClosure $fixture.Report $fixture.Component $fixture.Suite $written.Path $written.Map
    if ($accepted.policy -ne 'exact-foundation-io-native-return-tail' -or $accepted.reportedCount -ne 1 -or
        $accepted.reportedHelpers.Count -ne 1 -or $fixture.Report.NativeCompatibility.RuntimeHelperCount -ne 1 -or
        $accepted.codeOffset -ne 18 -or $accepted.byteCount -ne 10 -or
        $accepted.sha256 -ne '97feb6a1e88c94de5f5ad339a80015a1e2a39ed4412338df37c5007e7f8e4867' -or
        $accepted.callers.Count -ne 2 -or $accepted.callers[0].terminalBranchOffset -ne 6 -or
        $accepted.callers[1].terminalBranchOffset -ne 14 -or
        @($accepted.callers | Where-Object { $_.branchTarget -ne 18 }).Count -ne 0 -or $accepted.externalRuntimeDependencyApproved) {
        throw 'The exact native return tail did not preserve its count, byte proof and two caller branches.'
    }
    [ordered]@{ scenario = 'tail-exact-admission'; status = 'passed'; fixtureOnly = $true; admission = $accepted }
    Write-Host 'PASS tail-exact-admission: exact bytes, symbol prologues and both branches; reported helper count remains one.'

    foreach ($component in @('Foundation', 'EvalNumeric', 'Workbench31MakeDir')) {
        $withoutHelper = @{ NativeCompatibility = @{ RuntimeHelperCount = 0; RuntimeHelpers = @() } }
        $accepted = Assert-NativeHelperClosure $withoutHelper $component 'unchanged-no-helper-suite' '' ''
        if ($accepted.policy -ne 'no-runtime-helpers' -or $accepted.reportedCount -ne 0 -or $accepted.reportedHelpers.Count -ne 0) {
            throw "The unchanged no-helper closure regressed for $component."
        }
    }
    [ordered]@{ scenario = 'tail-no-helper-defaults'; status = 'passed'; fixtureOnly = $true; components = 3 }
    Write-Host 'PASS tail-no-helper-defaults: all three component modes retain the empty-helper boundary.'

    foreach ($scenario in @('tail-unknown-helper', 'tail-count-inventory-mismatch', 'tail-duplicate-helper',
        'tail-byte-corruption', 'tail-duplicate-bytes', 'tail-in-readonly-data',
        'tail-redirect-read-branch', 'tail-redirect-write-branch',
        'tail-prologue-register-drift', 'tail-prologue-stack-drift',
        'tail-relocation-overlap', 'tail-branch-relocation-overlap', 'tail-prologue-relocation-overlap',
        'tail-outside-component', 'tail-outside-suite', 'tail-missing-hunk-symbol', 'tail-duplicate-hunk-symbol',
        'tail-truncated-hunk', 'tail-extra-hunk-record', 'tail-map-symbol-drift',
        'tail-map-helper-drift', 'tail-bss-storage')) {
        $fixture = New-TailFixture
        $expectedMessage = 'Shared-tail audit:'
        switch ($scenario) {
            'tail-unknown-helper' {
                $fixture.Report.NativeCompatibility.RuntimeHelpers = @('__c68k_shared_epilogue_UNKNOWN')
                $expectedMessage = 'unqualified runtime helper'
            }
            'tail-count-inventory-mismatch' {
                $fixture.Report.NativeCompatibility.RuntimeHelperCount = 0
                $expectedMessage = 'count and inventory disagree'
            }
            'tail-duplicate-helper' {
                $fixture.Report.NativeCompatibility.RuntimeHelpers += $fixture.MapHelper
                $fixture.Report.NativeCompatibility.RuntimeHelperCount = 2
                $expectedMessage = 'unqualified runtime helper'
            }
            'tail-byte-corruption' { $fixture.Code[21] = 0x87; $expectedMessage = 'tail is absent' }
            'tail-duplicate-bytes' {
                $fixture.Code = (Convert-HexToBytes '2002508F4CDF4C1C4E75') + $fixture.Code
                $fixture.CodeBoundary += 10
                foreach ($symbol in $fixture.Symbols) { $symbol.Offset += 10 }
                $expectedMessage = 'tail is absent, duplicated'
            }
            'tail-in-readonly-data' {
                $fixture.Code = $fixture.Code[0..29]
                $fixture.CodeBoundary = 18
                $expectedMessage = 'read-only data after code boundary'
            }
            'tail-redirect-read-branch' { $fixture.Code[7] = 8; $expectedMessage = 'terminal branch' }
            'tail-redirect-write-branch' { $fixture.Code[15] = 4; $expectedMessage = 'terminal branch' }
            'tail-prologue-register-drift' { $fixture.Code[3] = 0x30; $expectedMessage = 'saved-register prologue' }
            'tail-prologue-stack-drift' { $fixture.Code[12] = 0x55; $expectedMessage = 'saved-register prologue' }
            'tail-relocation-overlap' { $fixture.Relocations = @(16); $expectedMessage = 'relocation overlaps verified' }
            'tail-branch-relocation-overlap' { $fixture.Relocations = @(6); $expectedMessage = 'relocation overlaps verified' }
            'tail-prologue-relocation-overlap' { $fixture.Relocations = @(8); $expectedMessage = 'relocation overlaps verified' }
            'tail-outside-component' { $fixture.Component = 'EvalNumeric'; $expectedMessage = 'unqualified runtime helper' }
            'tail-outside-suite' { $fixture.Suite = 'command-startup-vector-fixture'; $expectedMessage = 'unqualified runtime helper' }
            'tail-missing-hunk-symbol' { $fixture.Symbols[0].Name += '_not_ReadOnce'; $expectedMessage = 'missing HUNK symbol' }
            'tail-duplicate-hunk-symbol' { $fixture.Symbols += $fixture.Symbols[0]; $expectedMessage = 'duplicate HUNK symbol' }
            'tail-map-symbol-drift' { $fixture.Symbols[0].Size = 10; $expectedMessage = 'caller symbols, sizes' }
            'tail-map-helper-drift' { $fixture.MapHelper += "`n__c68k_unqualified"; $expectedMessage = 'map metrics or helper inventory' }
            'tail-bss-storage' { $fixture.BssBytes = 4; $expectedMessage = 'code/data sizes disagree' }
        }
        $written = Write-TailFixture $fixture $scenario
        if ($scenario -eq 'tail-truncated-hunk') {
            $image = [IO.File]::ReadAllBytes($written.Path)
            [IO.File]::WriteAllBytes($written.Path, $image[0..($image.Length - 2)])
            $expectedMessage = 'truncated HUNK word'
        }
        elseif ($scenario -eq 'tail-extra-hunk-record') {
            $image = [IO.File]::ReadAllBytes($written.Path)
            [IO.File]::WriteAllBytes($written.Path, $image + (Convert-HexToBytes '000003E9000000014E750000000003F2'))
            $expectedMessage = 'unexpected, repeated or trailing HUNK record'
        }
        $failureMessage = $null
        try { $null = Assert-NativeHelperClosure $fixture.Report $fixture.Component $fixture.Suite $written.Path $written.Map }
        catch [IO.InvalidDataException] { $failureMessage = $_.Exception.Message }
        catch [InvalidOperationException] { $failureMessage = $_.Exception.Message }
        if ($null -eq $failureMessage -or $failureMessage -notmatch $expectedMessage) {
            throw "$scenario did not reach its expected rejection in the actual helper guard: $failureMessage"
        }
        [ordered]@{ scenario = $scenario; status = 'passed'; fixtureOnly = $true; rejection = $failureMessage }
        Write-Host "PASS ${scenario}: $failureMessage"
    }
}
$results += @($guardResults)

$reportPath = Join-Path $testDirectory 'script-regressions.json'
[ordered]@{ schemaVersion = 1; status = 'passed'; cases = $results; shippingOrPureApproval = $false } |
    ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding utf8
Write-Output "Qualification script regression report: $reportPath"
