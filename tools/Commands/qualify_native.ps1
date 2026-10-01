param(
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [ValidateSet('Foundation', 'EvalNumeric', 'Workbench31MakeDir')][string]$Component = 'Foundation',
    [string]$OriginalMakeDirHunk = $env:COPPEROS_WB31_MAKEDIR_REFERENCE
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$rootFolder = if ($Component -eq 'EvalNumeric') { 'Commands.EvalNativeRoot' } else { 'Commands.NativeRoot' }
$executorFolder = switch ($Component) {
    'Foundation' { 'Commands.NativeExecution' }
    'EvalNumeric' { 'Commands.EvalNativeExecution' }
    'Workbench31MakeDir' { 'Commands.MakeDirNativeExecution' }
}
$rootAssemblyName = "CopperOS.$rootFolder"
$executorAssemblyName = "CopperOS.$executorFolder"
$rootOutput = Join-Path $repo "tests\$rootFolder\bin\$Configuration\net10.0"
$qualificationFolder = if ($Component -eq 'Workbench31MakeDir') { 'qualification-makedir' } else { 'qualification' }
$qualificationRoot = Join-Path $rootOutput $qualificationFolder
$runDirectory = Join-Path $qualificationRoot ([guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $runDirectory -Force)
$summaryPath = Join-Path $runDirectory 'qualification.json'
$manifestPath = Join-Path $runDirectory 'input-manifest.json'
$snapshotDirectory = Join-Path $runDirectory 'inputs'
$suites = @(
    [pscustomobject]@{
        id = 'command-startup-vector-fixture'; entry = 'CopperOS.Commands.NativeRoot.NativeCommandProbe::Main'
        command = 'private-command-probe'; file = 'command-probe.hunk'; invocations = 31
    },
    [pscustomobject]@{
        id = 'command-argument-boundary-vector-fixture'; entry = 'CopperOS.Commands.NativeRoot.NativeArgumentBoundaryProbe::Main'
        command = 'private-argument-boundary-probe'; file = 'argument-boundary-probe.hunk'; invocations = 25
    },
    [pscustomobject]@{
        id = 'command-io-vector-fixture'; entry = 'CopperOS.Commands.NativeRoot.NativeIoProbe::Main'
        command = 'private-io-probe'; file = 'io-probe.hunk'; invocations = 32
    }
)
$suiteGroup = 'CC03-CC04-CC06-native-command-foundation'
$artifactPassStatus = 'native-vector-fixture-passed'
$managedRoles = @('native-adapters', 'sdk')
$allowedAssemblies = @('CopperOS.Commands.Native', 'CopperOS.Commands.NativeRoot', 'CopperSharp.Sdk.Amiga')
if ($Component -eq 'EvalNumeric') {
    $suites = @([pscustomobject]@{
        id = 'eval-numeric-component'; entry = 'CopperOS.Commands.EvalNativeRoot.EvalNumericProbe::Main'
        command = 'private-eval-numeric-probe'; file = 'eval-numeric-probe.hunk'; invocations = 180
    })
    $suiteGroup = 'CC10-Eval-numeric-component'
    $artifactPassStatus = 'native-component-fixture-passed'
    $managedRoles = @('command-core', 'sdk', 'sdk-support')
    $allowedAssemblies = @('CopperOS.Commands', 'CopperOS.Commands.EvalNativeRoot', 'CopperSharp.Sdk.Amiga', 'CopperSharp.Sdk.Amiga.Support')
}
elseif ($Component -eq 'Workbench31MakeDir') {
    $suites = @([pscustomobject]@{
        id = 'makedir-classic-reference-vector-fixture'; entry = 'CopperOS.Commands.NativeRoot.Workbench31MakeDirProbe::Main'
        command = 'private-workbench31-makedir-probe'; file = 'makedir-probe.hunk'; invocations = 39
        originalInvocations = 38; comparisons = 38
    })
    $suiteGroup = 'CC12-MakeDir-Workbench31-reference-vector-fixture'
    $artifactPassStatus = 'native-command-reference-vector-fixture-passed'
}
$summary = [ordered]@{
    schemaVersion = 2; suite = $suiteGroup; component = $Component
    createdUtc = [DateTime]::UtcNow.ToString('o'); completedUtc = $null
    status = 'running'; stage = 'initialize'; failure = $null; allPassed = $false
    configuration = $Configuration; suites = @($suites.id); expectedArtifacts = 3 * $suites.Count
    shippingCommandsQualified = 0; originalKickstartRun = $false; copperStartRun = $false
    originalMorphOSRun = $false; minimumStackQualified = $false
    inputs = @(); inputManifest = $null; toolEnvironment = $null
    stages = [Collections.Generic.List[object]]::new()
    inputChecks = [Collections.Generic.List[object]]::new()
    artifacts = [Collections.Generic.List[object]]::new()
}
$sourceScopes = @()
$settingCandidates = @()
$sourceInputs = @()
$binaryInputs = @()
$restoreInputs = @()
$hostInputs = @()
$referenceInputs = @()
$currentStage = $null
$dotnetExecutable = $null
$runtimeVersion = $null
$runtimeDirectory = $null
$hostPaths = @()

function Convert-BytesToHex([byte[]]$bytes, [int]$offset = 0, [int]$count = -1) {
    if ($count -lt 0) { $count = $bytes.Length - $offset }
    return ([BitConverter]::ToString($bytes, $offset, $count) -replace '-', '')
}

function Get-BytesSha256Hex([byte[]]$bytes) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return (Convert-BytesToHex $sha.ComputeHash($bytes)).ToLowerInvariant() }
    finally { $sha.Dispose() }
}

function Save-Receipt {
    $summary | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath "$summaryPath.next" -Encoding utf8
    Move-Item -LiteralPath "$summaryPath.next" -Destination $summaryPath -Force
}

function Start-Stage([string]$name, [string[]]$arguments = @()) {
    $summary.stage = $name
    $script:currentStage = [ordered]@{
        name = $name; startedUtc = [DateTime]::UtcNow.ToString('o'); completedUtc = $null
        status = 'running'; arguments = $arguments; exitCode = $null; failure = $null; log = $null
    }
    $summary.stages.Add($currentStage)
    Save-Receipt
}

function Complete-Stage {
    $currentStage.status = 'passed'
    $currentStage.completedUtc = [DateTime]::UtcNow.ToString('o')
    Save-Receipt
}

function Get-FileIdentity([string]$path, [string]$role) {
    $file = Get-Item -LiteralPath $path
    [ordered]@{
        role = $role; path = $file.FullName; bytes = $file.Length
        sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}

function Copy-BoundInput([string]$path, [string]$destination, [string]$role) {
    $identity = Get-FileIdentity $path $role
    [void](New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force)
    Copy-Item -LiteralPath $identity.path -Destination $destination
    $copy = Get-FileIdentity $destination $role
    $after = Get-FileIdentity $path $role
    if ($copy.sha256 -ne $identity.sha256 -or $after.sha256 -ne $identity.sha256) {
        throw [IO.InvalidDataException]::new("Input binding drift while copying $path")
    }
    $copy.sourcePath = $identity.path
    return $copy
}

function Get-SourcePaths {
    $paths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($scope in $sourceScopes) {
        foreach ($file in Get-ChildItem -LiteralPath $scope.path -File -Recurse) {
            $relative = [IO.Path]::GetRelativePath($scope.path, $file.FullName).Replace('\', '/')
            if ($relative -match '(^|/)(bin|obj|\.git)/') { continue }
            # Commands.csproj excludes native bodies, entries, Common and the
            # Native aggregate. None is a source/build dependency of the numeric component.
            if ($Component -eq 'EvalNumeric' -and $scope.name -eq 'CopperOS-src-Commands' -and
                ($relative -like 'Native/*' -or $relative -like 'Common/*' -or
                 $relative -like '*/Native/*' -or $relative -like '*/Entry/*')) { continue }
            if ($file.Extension -notin @('.cs', '.csproj', '.props', '.targets', '.json', '.ruleset') -and
                $file.Name -ne '.editorconfig') { continue }
            # The SDK includes only these two example programs in its ABI assembly.
            if ($scope.name -eq 'CopperSharp-Sdk.Amiga' -and $relative -like 'Examples/*' -and
                $relative -notin @('Examples/DOS/Program.cs', 'Examples/FileStats/Program.cs')) { continue }
            [void]$paths.Add($file.FullName)
        }
    }
    foreach ($path in $settingCandidates) {
        if (Test-Path -LiteralPath $path -PathType Leaf) { [void]$paths.Add([IO.Path]::GetFullPath($path)) }
    }
    @($paths | Sort-Object)
}

function Assert-BoundFiles([object[]]$expected, [string]$pathProperty = 'path') {
    foreach ($identity in $expected) {
        $path = $identity[$pathProperty]
        if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or
            (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $identity.sha256) {
            throw [IO.InvalidDataException]::new("Input binding drift: $path")
        }
    }
}

function Assert-FrameworkClosure([object]$report, [string]$component) {
    if ($component -ne 'Workbench31MakeDir') {
        if ($report.Members.Count -ne 0 -or $report.NativeCompatibility.RuntimeFeatureCount -ne 0) {
            throw [InvalidOperationException]::new('This private suite does not admit framework members or features.')
        }
        return
    }

    # The public SDK returns BPTR? from Lock/CreateDir. These two operations
    # lower to native pointer tests/loads; they do not bring a managed runtime.
    # Admit this exact pair only. Value is used after HasValue in the command;
    # its declared MayThrow effect does not authorize an exception runtime.
    $bindings = @{
        'get_HasValue' = 'intrinsic:nullable-has-value:Amiga.BPTR'
        'get_Value' = 'intrinsic:nullable-get-value:Amiga.BPTR'
    }
    if ($report.Members.Count -ne 2 -or $report.NativeCompatibility.RuntimeFeatureCount -ne 1 -or
        $report.NativeCompatibility.RuntimeFeatures.Count -ne 1 -or
        $report.NativeCompatibility.RuntimeFeatures[0] -ne 'nullable-values' -or
        @($report.Members.Member.Name | Sort-Object -Unique).Count -ne 2) {
        throw [InvalidOperationException]::new('MakeDir requires only the two declared BPTR nullable intrinsics.')
    }
    foreach ($binding in $report.Members) {
        $member = $binding.Member
        $expectedReturn = if ($member.Name -eq 'get_HasValue') { 'bool' } else { '!0' }
        $effects = @($binding.Effects)
        if (-not $bindings.ContainsKey($member.Name) -or $binding.Status -ne 'intrinsic' -or
            $binding.Binding -ne $bindings[$member.Name] -or $member.AssemblyName -ne 'System.Runtime' -or
            $member.TypeName -ne 'System.Nullable<Amiga.BPTR>' -or $member.IsStatic -or $member.GenericArity -ne 0 -or
            $member.ReturnType -ne $expectedReturn -or $member.ParameterTypes.Count -ne 0 -or $member.MethodTypeArguments.Count -ne 0 -or
            $binding.RequiredFeatures.Count -ne 1 -or $binding.RequiredFeatures[0] -ne 'nullable-values' -or
            ($member.Name -eq 'get_HasValue' -and $effects.Count -ne 0) -or
            ($member.Name -eq 'get_Value' -and ($effects.Count -ne 1 -or $effects[0] -ne 'MayThrow'))) {
            throw [InvalidOperationException]::new('MakeDir framework binding is not its declared allocation-free BPTR intrinsic.')
        }
    }
}

function Read-CommandHunkWord([byte[]]$bytes, [ref]$cursor) {
    if ($cursor.Value -lt 0 -or $cursor.Value -gt $bytes.Length - 4) {
        throw [IO.InvalidDataException]::new('Shared-tail audit: truncated HUNK word.')
    }
    $position = [int]$cursor.Value
    $cursor.Value += 4
    return [uint32]([uint64]$bytes[$position] * 16777216 + [uint64]$bytes[$position + 1] * 65536 +
        [uint64]$bytes[$position + 2] * 256 + $bytes[$position + 3])
}

function Read-CommandHunkForTailAudit([byte[]]$bytes) {
    $cursor = 0
    $header = @(foreach ($index in 0..5) { Read-CommandHunkWord $bytes ([ref]$cursor) })
    if ($header[0] -ne 0x3f3 -or $header[1] -ne 0 -or $header[2] -ne 1 -or
        $header[3] -ne 0 -or $header[4] -ne 0 -or $header[5] -eq 0 -or
        (Read-CommandHunkWord $bytes ([ref]$cursor)) -ne 0x3e9) {
        throw [IO.InvalidDataException]::new('Shared-tail audit requires one plain HUNK_CODE allocation.')
    }
    $codeWords = Read-CommandHunkWord $bytes ([ref]$cursor)
    $codeLength = [uint64]$codeWords * 4
    if ($codeWords -ne $header[5] -or $codeLength -gt $bytes.Length - $cursor) {
        throw [IO.InvalidDataException]::new('Shared-tail audit: invalid HUNK_CODE length or allocation flags.')
    }
    $code = [byte[]]::new([int]$codeLength)
    [Array]::Copy($bytes, $cursor, $code, 0, $code.Length)
    $cursor += $code.Length
    $symbols = [Collections.Generic.Dictionary[string, int]]::new([StringComparer]::Ordinal)
    $relocations = [Collections.Generic.HashSet[int]]::new()
    $seenRelocations = $false
    $seenSymbols = $false
    while ($true) {
        $record = Read-CommandHunkWord $bytes ([ref]$cursor)
        if ($record -eq 0x3ec -and -not $seenRelocations -and -not $seenSymbols) {
            $seenRelocations = $true
            while (($count = Read-CommandHunkWord $bytes ([ref]$cursor)) -ne 0) {
                if ((Read-CommandHunkWord $bytes ([ref]$cursor)) -ne 0 -or
                    [uint64]$count * 4 -gt $bytes.Length - $cursor) {
                    throw [IO.InvalidDataException]::new('Shared-tail audit: invalid relocation target or count.')
                }
                for ($index = 0; $index -lt $count; $index++) {
                    $offset = Read-CommandHunkWord $bytes ([ref]$cursor)
                    if ($offset -gt $code.Length - 4 -or ($offset % 2) -ne 0 -or
                        -not $relocations.Add([int]$offset)) {
                        throw [IO.InvalidDataException]::new('Shared-tail audit: invalid or duplicate relocation.')
                    }
                }
            }
        }
        elseif ($record -eq 0x3f0 -and -not $seenSymbols) {
            $seenSymbols = $true
            while (($nameWords = Read-CommandHunkWord $bytes ([ref]$cursor)) -ne 0) {
                $nameBytes = [uint64]$nameWords * 4
                if ($nameBytes -gt $bytes.Length - $cursor - 4) {
                    throw [IO.InvalidDataException]::new('Shared-tail audit: truncated symbol name or address.')
                }
                $nameLength = [int]$nameBytes
                for ($index = 0; $index -lt $nameBytes; $index++) {
                    $character = $bytes[$cursor + $index]
                    if ($character -eq 0) { $nameLength = [Math]::Min($nameLength, $index) }
                    elseif ($index -ge $nameLength -or $character -lt 32 -or $character -gt 126) {
                        throw [IO.InvalidDataException]::new('Shared-tail audit: invalid symbol text or padding.')
                    }
                }
                $name = [Text.Encoding]::ASCII.GetString($bytes, $cursor, $nameLength)
                $cursor += [int]$nameBytes
                $address = Read-CommandHunkWord $bytes ([ref]$cursor)
                if ($nameLength -eq 0 -or $address -ge $code.Length -or ($address % 2) -ne 0 -or
                    $symbols.ContainsKey($name)) {
                    throw [IO.InvalidDataException]::new('Shared-tail audit: invalid or duplicate HUNK symbol.')
                }
                $symbols[$name] = [int]$address
            }
        }
        elseif ($record -eq 0x3f2 -and $seenSymbols -and $cursor -eq $bytes.Length) { break }
        else { throw [IO.InvalidDataException]::new('Shared-tail audit: unexpected, repeated or trailing HUNK record.') }
    }
    return [pscustomobject]@{ Code = $code; Symbols = $symbols; Relocations = @($relocations) }
}

function Get-CommandMapMetric([string]$metrics, [string]$name) {
    $matches = [regex]::Matches($metrics, "(?:^|\s)$([regex]::Escape($name))=([0-9]+)(?=\s|$)")
    $value = 0
    if ($matches.Count -ne 1 -or -not [int]::TryParse($matches[0].Groups[1].Value, [ref]$value)) {
        throw [IO.InvalidDataException]::new("Shared-tail audit: missing, repeated or invalid map metric $name.")
    }
    return $value
}

function Assert-NativeHelperClosure([object]$report, [string]$component, [string]$suite,
    [string]$hunkPath, [string]$map) {
    $helpers = @($report.NativeCompatibility.RuntimeHelpers)
    $count = $report.NativeCompatibility.RuntimeHelperCount
    if ($count -ne $helpers.Count) {
        throw [InvalidOperationException]::new('Runtime helper count and inventory disagree.')
    }
    if ($count -eq 0) {
        return [ordered]@{ policy = 'no-runtime-helpers'; reportedCount = 0; reportedHelpers = @() }
    }
    $knownLabel = '__c68k_shared_epilogue_97FEB6A1E88C94DE'
    if ($component -cne 'Foundation' -or $suite -cne 'command-io-vector-fixture' -or
        $count -ne 1 -or $helpers[0] -cne $knownLabel) {
        throw [InvalidOperationException]::new('This suite contains an unqualified runtime helper.')
    }

    # This exact generated native return tail has no runtime call or data access.
    # Do not admit the __c68k_ namespace generally or erase its reported count.
    # The compiler omits cold-tail labels from HUNK_SYMBOL; locate its unique
    # bytes, then bind both incoming branches to genuine managed method symbols.
    $imageBytes = [IO.File]::ReadAllBytes($hunkPath)
    $hunk = Read-CommandHunkForTailAudit $imageBytes
    $code = $hunk.Code
    $metricLines = [regex]::Matches($map, '(?m)^METRICS ([^\r\n]+)\r?$')
    $mapHelpers = [regex]::Matches($map,
        '(?m)^RUNTIME HELPERS\r?\n([^\r\n]+)\r?\nEXTERNAL NATIVE TARGETS\r?$')
    if ($metricLines.Count -ne 1 -or $mapHelpers.Count -ne 1 -or
        $mapHelpers[0].Groups[1].Value -cne $knownLabel) {
        throw [IO.InvalidDataException]::new('Shared-tail audit: map metrics or helper inventory disagree.')
    }
    $metrics = $metricLines[0].Groups[1].Value
    $logicalBytes = Get-CommandMapMetric $metrics 'code-bytes'
    $codeBoundary = Get-CommandMapMetric $metrics 'rom-code-bytes'
    $rodataBytes = Get-CommandMapMetric $metrics 'rom-rodata-bytes'
    if ((Get-CommandMapMetric $metrics 'artifact-bytes') -ne $imageBytes.Length -or
        (Get-CommandMapMetric $metrics 'rom-bytes') -ne $logicalBytes -or
        (Get-CommandMapMetric $metrics 'initialized-ram-bytes') -ne 0 -or
        (Get-CommandMapMetric $metrics 'bss-bytes') -ne 0 -or
        (Get-CommandMapMetric $metrics 'symbols') -ne $hunk.Symbols.Count -or
        (Get-CommandMapMetric $metrics 'relocations') -ne $hunk.Relocations.Count -or
        $rodataBytes -ne 12 -or [long]$codeBoundary + $rodataBytes -ne $logicalBytes -or
        $logicalBytes -gt $code.Length -or $code.Length - $logicalBytes -gt 3) {
        throw [IO.InvalidDataException]::new('Shared-tail audit: HUNK and map code/data sizes disagree.')
    }
    for ($index = $logicalBytes; $index -lt $code.Length; $index++) {
        if ($code[$index] -ne 0) { throw [IO.InvalidDataException]::new('Shared-tail audit: nonzero HUNK padding.') }
    }
    if ((Convert-BytesToHex $code $codeBoundary $rodataBytes) -cne '646F732E6C69627261727900') {
        throw [IO.InvalidDataException]::new('Shared-tail audit: unexpected read-only data after code boundary.')
    }
    # MOVE.L D2,D0; ADDQ.L #8,A7; MOVEM.L (A7)+,D2-D4/A2-A3/A6; RTS.
    $tail = [byte[]](0x20, 0x02, 0x50, 0x8F, 0x4C, 0xDF, 0x4C, 0x1C, 0x4E, 0x75)
    $occurrences = @(for ($offset = 0; $offset -le $code.Length - $tail.Length; $offset++) {
        if ((Convert-BytesToHex $code $offset $tail.Length) -ceq '2002508F4CDF4C1C4E75') { $offset }
    })
    if ($occurrences.Count -ne 1 -or ($occurrences[0] % 2) -ne 0 -or
        $occurrences[0] + $tail.Length -ne $codeBoundary) {
        throw [IO.InvalidDataException]::new('Shared-tail audit: tail is absent, duplicated or outside the code boundary.')
    }
    $tailOffset = $occurrences[0]
    if (@($hunk.Symbols.Values | Where-Object { $_ -ge $tailOffset }).Count -ne 0) {
        throw [IO.InvalidDataException]::new('Shared-tail audit: a managed symbol claims tail or read-only data bytes.')
    }
    $names = @('CopperOS.Commands.Native.NativeCommandIo::ReadOnce',
        'CopperOS.Commands.Native.NativeCommandIo::WriteOnce',
        'CopperOS.Commands.Native.NativeCommandIo::IsCtrlCPending')
    $offsets = @(foreach ($name in $names) {
        if (-not $hunk.Symbols.ContainsKey($name)) {
            throw [IO.InvalidDataException]::new("Shared-tail audit: missing HUNK symbol $name.")
        }
        $hunk.Symbols[$name]
    })
    $callers = @(for ($index = 0; $index -lt 2; $index++) {
        $start = $offsets[$index]
        $end = $offsets[$index + 1]
        $symbolLines = [regex]::Matches($map, "(?m)^([0-9A-Fa-f]{8})[ \t]+([0-9]+)[ \t]+$([regex]::Escape($names[$index]))\r?$")
        if ($start + 8 -gt $end -or $end -gt $tailOffset -or $symbolLines.Count -ne 1 -or
            [Convert]::ToInt32($symbolLines[0].Groups[1].Value, 16) -ne $start -or
            [long]$symbolLines[0].Groups[2].Value -ne $end - $start -or
            @($hunk.Symbols.Values | Where-Object { $_ -gt $start -and $_ -lt $end }).Count -ne 0 -or
            (Convert-BytesToHex $code $start 6) -cne '48E73832518F') {
            throw [IO.InvalidDataException]::new('Shared-tail audit: caller symbols, sizes or saved-register prologue changed.')
        }
        $branch = $end - 2
        $displacement = [int]$code[$branch + 1]
        if ($displacement -ge 128) { $displacement -= 256 }
        if ($code[$branch] -ne 0x60 -or $displacement -in @(0, -1) -or
            $branch + 2 + $displacement -ne $tailOffset) {
            throw [IO.InvalidDataException]::new('Shared-tail audit: caller terminal branch no longer targets the exact tail.')
        }
        [ordered]@{ symbol = $names[$index]; offset = $start; size = $end - $start
            prologueHex = '48e73832518f'; terminalBranchOffset = $branch; branchTarget = $tailOffset }
    })
    foreach ($relocation in $hunk.Relocations) {
        # Relocation rewrites must not alter the verified tail, prologue or BRA.
        $overlap = $relocation -lt $codeBoundary -and $relocation + 4 -gt $tailOffset
        foreach ($caller in $callers) {
            $overlap = $overlap -or ($relocation -lt $caller.offset + 6 -and $relocation + 4 -gt $caller.offset) -or
                ($relocation -lt $caller.terminalBranchOffset + 2 -and $relocation + 4 -gt $caller.terminalBranchOffset)
        }
        if ($overlap) { throw [IO.InvalidDataException]::new('Shared-tail audit: relocation overlaps verified control-flow bytes.') }
    }
    return [ordered]@{
        policy = 'exact-foundation-io-native-return-tail'; reportedCount = $count; reportedHelpers = $helpers
        helper = $knownLabel; codeOffset = $tailOffset; byteCount = $tail.Length
        bytesHex = '2002508f4cdf4c1c4e75'
        sha256 = Get-BytesSha256Hex $tail
        hunkCodeBoundary = $codeBoundary; callers = $callers; externalRuntimeDependencyApproved = $false
    }
}

function Assert-InputBinding([string]$checkpoint) {
    $check = [ordered]@{ checkpoint = $checkpoint; utc = [DateTime]::UtcNow.ToString('o'); status = 'failed'; failure = $null }
    try {
        if ($sourceInputs.Count -gt 0) {
            $actual = @(Get-SourcePaths)
            $expected = @($sourceInputs.sourcePath | Sort-Object)
            if (@(Compare-Object $expected $actual).Count -ne 0) {
                throw [IO.InvalidDataException]::new('Input binding drift: source/build-setting file inventory changed.')
            }
            Assert-BoundFiles $sourceInputs 'sourcePath'
            Assert-BoundFiles $sourceInputs
        }
        if ($binaryInputs.Count -gt 0) {
            Assert-BoundFiles $binaryInputs 'sourcePath'
            Assert-BoundFiles $binaryInputs
            $actualCopies = @(Get-ChildItem -LiteralPath (Join-Path $snapshotDirectory 'binary') -File -Recurse | ForEach-Object FullName | Sort-Object)
            if (@(Compare-Object @($binaryInputs.path | Sort-Object) $actualCopies).Count -ne 0) {
                throw [IO.InvalidDataException]::new('Input binding drift: binary snapshot inventory changed.')
            }
        }
        if ($restoreInputs.Count -gt 0) { Assert-BoundFiles $restoreInputs 'sourcePath'; Assert-BoundFiles $restoreInputs }
        # Licensed reference images stay at their private path. They are bound
        # and rechecked, but never copied into the repository or run snapshot.
        if ($referenceInputs.Count -gt 0) { Assert-BoundFiles $referenceInputs }
        if ($hostInputs.Count -gt 0) {
            Assert-BoundFiles $hostInputs
            $actualRuntime = @(Get-ChildItem -LiteralPath $runtimeDirectory -File | ForEach-Object FullName | Sort-Object)
            if (@(Compare-Object @($hostPaths | Sort-Object) $actualRuntime).Count -ne 0) {
                throw [IO.InvalidDataException]::new('Input binding drift: selected .NET runtime inventory changed.')
            }
        }
        $check.status = 'passed'
    }
    catch { $check.failure = $_.Exception.Message; throw }
    finally { $summary.inputChecks.Add($check) }
}

function Save-InputManifest {
    $manifest = [ordered]@{
        schemaVersion = 2; capturedUtc = [DateTime]::UtcNow.ToString('o')
        sourceScopes = $sourceScopes; settingCandidates = $settingCandidates
        sourceInputs = $sourceInputs; binaryInputs = $binaryInputs; restoreInputs = $restoreInputs
        hostInputs = $hostInputs; referenceInputs = $referenceInputs; toolEnvironment = $summary.toolEnvironment
        policy = 'Raw source snapshots; forced bootstrap rebuilds; declared managed dependency snapshots; source, copy, restore, private reference and pinned host-runtime identities checked before and after tool use. Licensed reference bytes are never copied.'
        hermeticOperatingSystemClaim = $false
    }
    $manifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $manifestPath -Encoding utf8
    $summary.inputs = $binaryInputs
    $summary.inputManifest = [ordered]@{
        path = $manifestPath; sha256 = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash.ToLowerInvariant()
        sourceFiles = $sourceInputs.Count; binaryFiles = $binaryInputs.Count
        restoreFiles = $restoreInputs.Count; hostFiles = $hostInputs.Count; privateReferenceFiles = $referenceInputs.Count
        sourcesAndCopiesVerified = $false
    }
    Save-Receipt
}

function Invoke-BoundDotnet([string]$stage, [string[]]$arguments) {
    Start-Stage $stage $arguments
    $currentStage.log = Join-Path $runDirectory (($stage -replace '[^a-zA-Z0-9.-]', '_') + '.log')
    Assert-InputBinding "$stage/before"
    & $dotnetExecutable @arguments 2>&1 | Tee-Object -FilePath $currentStage.log | ForEach-Object { Write-Host $_ }
    $currentStage.exitCode = $LASTEXITCODE
    # A failing build must not skip the drift check or erase its exit code.
    Assert-InputBinding "$stage/after"
    if ($currentStage.exitCode -ne 0) { throw "$stage failed with exit code $($currentStage.exitCode). See $($currentStage.log)" }
    Complete-Stage
}

function Copy-ManagedClosure([string]$directory, [string]$assemblyName, [string]$group) {
    $depsPath = Join-Path $directory "$assemblyName.deps.json"
    $deps = Get-Content -LiteralPath $depsPath -Raw | ConvertFrom-Json -AsHashtable
    $files = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    [void]$files.Add("$assemblyName.deps.json")
    $runtimeConfig = "$assemblyName.runtimeconfig.json"
    if (Test-Path -LiteralPath (Join-Path $directory $runtimeConfig)) { [void]$files.Add($runtimeConfig) }
    elseif ($group -ne 'managed-input') { throw "Missing runtime configuration for executable $assemblyName" }
    foreach ($library in $deps.targets[$deps.runtimeTarget.name].Values) {
        if ($library.ContainsKey('runtimeTargets')) { throw 'RID-specific runtime assets require explicit qualification; this closure is flat.' }
        foreach ($kind in @('runtime', 'native')) {
            if (-not $library.ContainsKey($kind)) { continue }
            foreach ($asset in $library[$kind].Keys) { [void]$files.Add([IO.Path]::GetFileName($asset)) }
        }
    }
    if (-not $files.Contains("$assemblyName.dll")) { throw "Missing declared root assembly in $depsPath" }
    foreach ($name in @($files | Sort-Object)) {
        $role = "$group-dependency"
        if ($group -eq 'managed-input' -and $name -eq "$rootAssemblyName.dll") { $role = 'managed-root' }
        elseif ($group -eq 'managed-input' -and $name -eq 'CopperOS.Commands.Native.dll') { $role = 'native-adapters' }
        elseif ($group -eq 'managed-input' -and $name -eq 'CopperOS.Commands.dll') { $role = 'command-core' }
        elseif ($group -eq 'managed-input' -and $name -eq 'CopperSharp.Sdk.Amiga.dll') { $role = 'sdk' }
        elseif ($group -eq 'managed-input' -and $name -eq 'CopperSharp.Sdk.Amiga.Support.dll') { $role = 'sdk-support' }
        elseif ($group -eq 'compiler' -and $name -eq 'CopperSharp.Compiler.Cli.dll') { $role = 'compiler-cli' }
        elseif ($group -eq 'compiler' -and $name -eq 'CopperSharp.Compiler.dll') { $role = 'compiler-backend' }
        elseif ($group -eq 'compiler' -and $name -eq 'CopperSharp.Targets.Amiga.dll') { $role = 'amiga-target' }
        elseif ($group -eq 'executor' -and $name -eq "$executorAssemblyName.dll") { $role = 'instruction-fixture' }
        elseif ($group -eq 'executor' -and $name -eq 'Copper68k.dll') { $role = 'instruction-core' }
        elseif ($group -eq 'executor' -and $name -eq 'CopperFloat.dll') { $role = 'floating-point-runtime' }
        $destination = Join-Path (Join-Path (Join-Path $snapshotDirectory 'binary') $group) $name
        Copy-BoundInput (Join-Path $directory $name) $destination $role
    }
}

# A valid invocation gets an attempt receipt even if input resolution, restore,
# compilation, or later qualification fails. Historical runs are never replaced.
Save-Receipt
Write-Output "Qualification attempt: $summaryPath"
Push-Location -LiteralPath $repo
try {
    Start-Stage 'resolve-inputs'
    $compilerRoot = (Resolve-Path -LiteralPath $CopperSharpRoot).Path
    $compilerOutput = Join-Path $compilerRoot "Compiler.Cli\bin\$Configuration\net10.0"
    $executorOutput = Join-Path $repo "tests\$executorFolder\bin\$Configuration\net10.0"
    $compilerProject = Join-Path $compilerRoot 'Compiler.Cli\CopperSharp.Compiler.Cli.csproj'
    $rootProject = Join-Path $repo "tests\$rootFolder\$rootAssemblyName.csproj"
    $executorProject = Join-Path $repo "tests\$executorFolder\$executorAssemblyName.csproj"
    if ($Component -eq 'Workbench31MakeDir') {
        if ([string]::IsNullOrWhiteSpace($OriginalMakeDirHunk)) {
            throw 'Set -OriginalMakeDirHunk or COPPEROS_WB31_MAKEDIR_REFERENCE to the private MakeDir 37.2 member; it is not bundled.'
        }
        $reference = Get-FileIdentity (Resolve-Path -LiteralPath $OriginalMakeDirHunk).Path 'private-original-workbench31-makedir'
        $relative = [IO.Path]::GetRelativePath($repo, $reference.path)
        if ($relative -ne '..' -and -not $relative.StartsWith('..' + [IO.Path]::DirectorySeparatorChar) -and
            -not [IO.Path]::IsPathRooted($relative)) {
            throw 'The licensed original MakeDir must remain outside the repository and all run snapshots.'
        }
        if ($reference.bytes -ne 464 -or $reference.sha256 -ne '23911db49742055d8bddcfe5f8de82cbb2232f8a7ef850d51bfd27f9b54c819b') {
            throw 'The private reference does not match the pinned 464-byte Workbench MakeDir 37.2 member.'
        }
        $reference.snapshotCopied = $false
        $referenceInputs = @($reference)
    }
    $dotnetExecutable = (Get-Command dotnet -CommandType Application).Source
    $sdkVersion = (& $dotnetExecutable --version).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Could not identify the selected .NET SDK.' }
    $runtimeLines = @(& $dotnetExecutable --list-runtimes)
    if ($LASTEXITCODE -ne 0) { throw 'Could not identify installed .NET runtimes.' }
    $runtimeCandidates = @($runtimeLines | ForEach-Object {
        if ($_ -match '^Microsoft.NETCore.App (10\.\d+\.\d+) \[(.+)\]$') {
            [pscustomobject]@{ version = [version]$Matches[1]; path = Join-Path $Matches[2] $Matches[1] }
        }
    } | Sort-Object version)
    if ($runtimeCandidates.Count -eq 0) { throw 'A stable .NET 10 runtime is required.' }
    $runtimeVersion = $runtimeCandidates[-1].version.ToString()
    $runtimeDirectory = $runtimeCandidates[-1].path
    $sdkDirectory = Join-Path (Join-Path (Split-Path -Parent $dotnetExecutable) 'sdk') $sdkVersion
    $summary.toolEnvironment = [ordered]@{
        dotnet = $dotnetExecutable; sdkVersion = $sdkVersion; sdkDirectory = $sdkDirectory
        runtimeVersion = $runtimeVersion; runtimeDirectory = $runtimeDirectory
        runtimeRollForward = 'Disable'; powershellVersion = $PSVersionTable.PSVersion.ToString()
        operatingSystem = [Environment]::OSVersion.VersionString
        workingDirectory = $repo
    }
    $commandSourceFolders = if ($Component -eq 'EvalNumeric') {
        @('src\Commands', 'src\System\Shell', 'tests\Commands.EvalNativeRoot', 'tests\Commands.EvalNativeExecution')
    } else {
        # The Native aggregate compiles Common/ and every <Command>/Native/.
        @('src\Commands', 'tests\Commands.NativeRoot', "tests\$executorFolder")
    }
    foreach ($name in $commandSourceFolders) {
        $sourceScopes += [ordered]@{ name = "CopperOS-$($name.Replace('\', '-'))"; path = Join-Path $repo $name }
    }
    $compilerSourceFolders = @('Compiler', 'Compiler.Cli', 'Targets.Amiga', 'Sdk.Amiga', 'Runtime.Managed', 'Runtime.AmigaPal')
    if ($Component -eq 'EvalNumeric') { $compilerSourceFolders += 'Sdk.Amiga.Support' }
    foreach ($name in $compilerSourceFolders) {
        $sourceScopes += [ordered]@{ name = "CopperSharp-$name"; path = Join-Path $compilerRoot $name }
    }
    $settingCandidates = @((Join-Path $repo 'CopperOS.Portable.props'), $PSCommandPath)
    if ($Component -ne 'Foundation') {
        # The execution project source-links the common HUNK reader.
        $settingCandidates += Join-Path $repo 'tests\Commands.NativeExecution\HunkImage.cs'
    }
    foreach ($base in @($repo, $compilerRoot)) {
        for ($directory = [IO.DirectoryInfo]::new($base); $null -ne $directory; $directory = $directory.Parent) {
            foreach ($name in @('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props',
                'global.json', 'NuGet.Config', 'nuget.config', '.editorconfig')) {
                $settingCandidates += Join-Path $directory.FullName $name
            }
        }
    }
    Complete-Stage

    Start-Stage 'bind-source-and-host-inputs'
    $sourceIndex = 0
    $sourceInputs = @(foreach ($path in Get-SourcePaths) {
        # A numbered directory avoids collisions for sources outside either repo;
        # the manifest retains exact original paths for review/reconstruction.
        $sourceIndex++
        $destination = Join-Path (Join-Path (Join-Path $snapshotDirectory 'source') $sourceIndex) ([IO.Path]::GetFileName($path))
        Copy-BoundInput $path $destination 'source-or-build-setting'
    })
    $hostPaths = @(Get-ChildItem -LiteralPath $runtimeDirectory -File | ForEach-Object FullName)
    $hostInputs = @(foreach ($path in $hostPaths) { Get-FileIdentity $path 'pinned-dotnet-runtime' })
    $hostInputs += Get-FileIdentity $dotnetExecutable 'dotnet-host'
    foreach ($name in @('MSBuild.dll', 'Microsoft.Build.dll', 'NuGet.Build.Tasks.dll', 'Roslyn\bincore\csc.dll',
        'Roslyn\bincore\Microsoft.CodeAnalysis.dll', 'Roslyn\bincore\Microsoft.CodeAnalysis.CSharp.dll')) {
        $hostInputs += Get-FileIdentity (Join-Path $sdkDirectory $name) 'dotnet-build-tool'
    }
    Save-InputManifest
    Assert-InputBinding 'initial-source-and-host-binding'
    Complete-Stage

    # Rebuild from the bound source bytes. Do not delete SDK copies from other
    # projects or use stale incremental output as current-source evidence.
    Invoke-BoundDotnet 'bootstrap/compiler' @('build', $compilerProject, '--configuration', $Configuration, '--no-incremental', '--verbosity', 'minimal')
    $rootBuildArguments = @('build', $rootProject, '--configuration', $Configuration, '--no-incremental', '--verbosity', 'minimal',
        '-p:CopperOSUseLocalCopperSharp=true', "-p:CopperSharp68kRoot=$compilerRoot")
    if ($Component -eq 'EvalNumeric') {
        # Host overflow-check tests use the same output directory; select the
        # production numeric lowering explicitly when rebuilding this component.
        $rootBuildArguments += '-p:CheckForOverflowUnderflow=false'
    }
    Invoke-BoundDotnet 'bootstrap/native-root' $rootBuildArguments
    Invoke-BoundDotnet 'bootstrap/executor' @('build', $executorProject, '--configuration', $Configuration, '--no-incremental', '--verbosity', 'minimal',
        '-p:CopperOSUseLocalCopperSharp=true', "-p:CopperSharp68kRoot=$compilerRoot")

    Start-Stage 'snapshot-built-inputs'
    $binaryInputs = @(
        Copy-ManagedClosure $compilerOutput 'CopperSharp.Compiler.Cli' 'compiler'
        Copy-ManagedClosure $rootOutput $rootAssemblyName 'managed-input'
        Copy-ManagedClosure $executorOutput $executorAssemblyName 'executor'
    )
    foreach ($required in ($managedRoles + @('managed-root', 'compiler-cli', 'compiler-backend', 'amiga-target',
        'instruction-fixture', 'instruction-core', 'floating-point-runtime'))) {
        if (@($binaryInputs | Where-Object { $_.role -eq $required }).Count -ne 1) { throw "Unbound required binary role: $required" }
    }
    $restoreIndex = 0
    $boundProjectPaths = @($sourceInputs.sourcePath | Where-Object { [IO.Path]::GetExtension($_) -eq '.csproj' })
    $restoreInputs = @(foreach ($scope in $sourceScopes) {
        $objectDirectory = Join-Path $scope.path 'obj'
        foreach ($file in Get-ChildItem -LiteralPath $objectDirectory -File) {
            if ($file.Name -ne 'project.assets.json' -and $file.Name -notlike '*.nuget.g.props' -and $file.Name -notlike '*.nuget.g.targets') { continue }
            if ($file.Name -eq 'project.assets.json') {
                $assets = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json -AsHashtable
                foreach ($framework in $assets.project.restore.frameworks.Values) {
                    if (-not $framework.ContainsKey('projectReferences')) { continue }
                    foreach ($reference in $framework.projectReferences.Keys) {
                        if ([IO.Path]::GetFullPath($reference) -notin $boundProjectPaths) { throw "Unbound build project reference: $reference" }
                    }
                }
            }
            $restoreIndex++
            $destination = Join-Path (Join-Path (Join-Path $snapshotDirectory 'restore') $restoreIndex) $file.Name
            Copy-BoundInput $file.FullName $destination 'resolved-build-input'
        }
    })
    Save-InputManifest
    Assert-InputBinding 'built-input-snapshots'
    Complete-Stage

    $cli = @($binaryInputs | Where-Object { $_.role -eq 'compiler-cli' })[0].path
    $inputAssembly = @($binaryInputs | Where-Object { $_.role -eq 'managed-root' })[0].path
    $managedArguments = @(foreach ($role in $managedRoles) {
        '--managed-assembly'
        @($binaryInputs | Where-Object { $_.role -eq $role })[0].path
    })
    $executor = @($binaryInputs | Where-Object { $_.role -eq 'instruction-fixture' })[0].path
    $executorIdentity = @($binaryInputs | Where-Object { $_.role -eq 'instruction-fixture' })[0]
    $coreIdentity = @($binaryInputs | Where-Object { $_.role -eq 'instruction-core' })[0]
    $runtimeArguments = @('exec', '--fx-version', $runtimeVersion, '--roll-forward', 'Disable')
    foreach ($cpu in @('68000', '68020', '68040')) {
        foreach ($suite in $suites) {
            $targetDirectory = Join-Path (Join-Path $runDirectory $cpu) $suite.id
            [void](New-Item -ItemType Directory -Path $targetDirectory)
            $output = Join-Path $targetDirectory $suite.file
            $compatibilityPath = Join-Path $targetDirectory 'compatibility.json'
            $executionPath = Join-Path $targetDirectory 'execution.json'
            $row = [ordered]@{
                command = $suite.command; suite = $suite.id; shipping = $false; cpu = $cpu; entry = $suite.entry
                runtime = 'resident'; fpu = 'disabled'; format = 'hunk'; output = $output
                compatibilityReport = $compatibilityPath; executionReport = $executionPath
                status = 'failed'; failure = $null
            }
            $summary.artifacts.Add($row)
            try {
                $arguments = $runtimeArguments + @(
                    $cli, $inputAssembly, '--entry', $suite.entry,
                    '--platform', 'amiga', '--cpu', $cpu, '--fpu', 'disabled',
                    '--format', 'hunk', '--runtime', 'resident', '--memory', 'none',
                    '--exceptions', 'yolo', '--exports', 'none',
                    '--output', $output, '--compatibility-report', $compatibilityPath
                ) + $managedArguments
                Invoke-BoundDotnet "$cpu/$($suite.id)/compile" $arguments
                Start-Stage "$cpu/$($suite.id)/check-closure"
                $report = Get-Content -LiteralPath $compatibilityPath -Raw | ConvertFrom-Json
                if (-not $report.IsCompatible -or $report.Cpu -ne "m$cpu" -or
                    $report.RuntimeProfile -ne 'resident' -or $report.OutputFormat -ne 'hunk' -or
                    $report.ManagedAllocationSites.Count -ne 0 -or
                    $report.IncludedExportNames.Count -ne 0 -or
                    $report.NativeCompatibility.MemoryManagement -ne 'none' -or
                    $report.NativeCompatibility.ExceptionRegionCount -ne 0 -or
                    $report.NativeCompatibility.FatalMachineFaultSiteCount -ne 0 -or
                    $report.NativeCompatibility.ExternalNativeTargetCount -ne 0) { throw 'Probe closure contains an unqualified runtime dependency.' }
                Assert-FrameworkClosure $report $Component
                $reachableAssemblies = @($report.NativeCompatibility.ReachableAssemblies)
                $reachableNames = @($reachableAssemblies.Name)
                if ($reachableNames.Count -ne $allowedAssemblies.Count -or
                    @($reachableNames | Sort-Object -Unique).Count -ne $allowedAssemblies.Count -or
                    @(Compare-Object @($allowedAssemblies | Sort-Object) @($reachableNames | Sort-Object)).Count -ne 0) {
                    throw 'The exact required native assembly closure is missing, duplicated, or changed.'
                }
                foreach ($assembly in $reachableAssemblies) {
                    $managedSnapshot = Join-Path (Join-Path (Join-Path $snapshotDirectory 'binary') 'managed-input') "$($assembly.Name).dll"
                    $identity = @($binaryInputs | Where-Object { $_.path -eq $managedSnapshot })
                    if ($identity.Count -ne 1 -or $assembly.Sha256 -ne $identity[0].sha256) {
                        throw "Reachable assembly identity is not its bound input: $($assembly.Name)"
                    }
                }
                if ($Component -eq 'EvalNumeric' -and $report.NativeCompatibility.ExternalNativeTargetCount -ne 0) {
                    throw 'The numeric component has an unexpected external native dependency.'
                }
                $map = Get-Content -LiteralPath "$output.map" -Raw
                if ($map -notmatch '(?m)^ENTRY 00000000\r?$' -or $map -notmatch 'initialized-ram-bytes=0 bss-bytes=0') {
                    throw 'Probe has unqualified entry or shared writable storage.'
                }
                $row.nativeHelperAdmission = Assert-NativeHelperClosure $report $Component $suite.id $output $map
                $hash = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash.ToLowerInvariant()
                Complete-Stage
                $executionArguments = $runtimeArguments + @($executor, $output, $cpu, $executionPath, $suite.id)
                if ($Component -eq 'Workbench31MakeDir') { $executionArguments += $referenceInputs[0].path }
                Invoke-BoundDotnet "$cpu/$($suite.id)/execute" $executionArguments
                Start-Stage "$cpu/$($suite.id)/check-execution"
                $execution = Get-Content -LiteralPath $executionPath -Raw | ConvertFrom-Json
                if ($execution.status -ne 'passed' -or $execution.suite -ne $suite.id -or $execution.cpu -ne $cpu -or
                    $execution.instructionCorePath -ne $coreIdentity.path -or $execution.instructionCoreSha256 -ne $coreIdentity.sha256 -or
                    $execution.managedExecutorPath -ne $executorIdentity.path -or $execution.managedExecutorSha256 -ne $executorIdentity.sha256 -or
                    $execution.hostRuntimeVersion -ne $runtimeVersion -or
                    (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash.ToLowerInvariant() -ne $hash) {
                    throw 'Native execution evidence is incomplete or bound to another image.'
                }
                if ($Component -eq 'Workbench31MakeDir') {
                    $original = $execution.original
                    $generated = $execution.generated
                    $scope = $execution.evidence
                    $executorSdkPath = Join-Path (Join-Path (Join-Path $snapshotDirectory 'binary') 'executor') 'CopperSharp.Sdk.Amiga.dll'
                    $executorSdk = @($binaryInputs | Where-Object { $_.path -eq $executorSdkPath })
                    $comparableIds = @($original.cases.caseId | Sort-Object)
                    $extraId = 'MK31-E09.generated-only-slot-allocation-failure'
                    if ($executorSdk.Count -ne 1 -or $execution.sdkAssemblyPath -ne $executorSdkPath -or
                        $execution.sdkAssemblySha256 -ne $executorSdk[0].sha256 -or
                        $original.inputPath -ne $referenceInputs[0].path -or $original.imageSha256 -ne $referenceInputs[0].sha256 -or
                        $generated.inputPath -ne $output -or $generated.imageSha256 -ne $hash -or
                        $original.imageLoads -ne 1 -or $generated.imageLoads -ne 1 -or $original.loadedBytes -ne 428 -or
                        $original.nativeInvocationsStarted -ne $suite.originalInvocations -or $original.nativeInvocationsPassed -ne $suite.originalInvocations -or
                        $generated.nativeInvocationsStarted -ne $suite.invocations -or $generated.nativeInvocationsPassed -ne $suite.invocations -or
                        $original.cases.Count -ne $suite.originalInvocations -or $generated.cases.Count -ne $suite.invocations -or
                        $execution.expectedComparableCases -ne $suite.comparisons -or $execution.expectedGeneratedOnlyCases -ne 1 -or
                        $execution.comparisonsPassed -ne $suite.comparisons -or $execution.comparisons.Count -ne $suite.comparisons -or
                        $execution.generatedOnlyCaseIds.Count -ne 1 -or $execution.generatedOnlyCaseIds[0] -ne $extraId -or
                        @($comparableIds | Sort-Object -Unique).Count -ne $suite.comparisons -or
                        @($generated.cases.caseId | Sort-Object -Unique).Count -ne $suite.invocations -or
                        @($execution.comparisons.caseId | Sort-Object -Unique).Count -ne $suite.comparisons -or
                        @(Compare-Object $comparableIds @($execution.comparisons.caseId | Sort-Object)).Count -ne 0 -or
                        @(Compare-Object @($comparableIds + $extraId | Sort-Object) @($generated.cases.caseId | Sort-Object)).Count -ne 0 -or
                        @($execution.comparisons | Where-Object { -not $_.passed -or -not $_.resultAndErrorEqual -or
                            -not $_.vPrintfBytesEqual -or -not $_.printFaultRequestsEqual -or -not $_.semanticCallOrderEqual -or
                            $_.realDosParser -or $_.realFilesystem }).Count -ne 0 -or
                        @(@($original.cases) + @($generated.cases) | Where-Object { -not $_.guardsAndImageUnchanged -or
                            -not $_.entryStackRestored -or $_.instructions -le 0 }).Count -ne 0 -or
                        -not $scope.originalCommandMachineCodeExecuted -or -not $scope.generatedCommandMachineCodeExecuted -or
                        $scope.realKickstartExecution -or $scope.realCopperStartExecution -or $scope.realDosParser -or
                        $scope.realFilesystemHandler -or $scope.realDosFormatter -or $scope.completeStdoutCaptured -or
                        $scope.workbenchLaunchQualified -or $scope.shippingOrPureApproval -or $scope.minimumStackQualified) {
                        throw 'MakeDir evidence lacks the distinct original/generated invocations or overstates its vector-fixture scope.'
                    }
                    $executionCases = @($generated.cases)
                    $nativeInvocations = $generated.nativeInvocationsPassed
                    $originalStacks = @($original.cases.configuredStackBytes | Sort-Object -Unique)
                    if (4096 -notin $originalStacks -or 16384 -notin $originalStacks) { throw 'Original MakeDir stack-size diversity is missing.' }
                    $row.originalNativeInvocations = $original.nativeInvocationsPassed
                    $row.originalCommandVectorComparisons = $execution.comparisonsPassed
                    $row.privateReference = $referenceInputs[0]
                    $row.originalConfiguredStackBytes = $originalStacks
                    $row.originalPeakObservedStackBytes = ($original.cases | Measure-Object -Property stackBytesWritten -Maximum).Maximum
                }
                else {
                    if ($execution.imageSha256 -ne $hash -or
                        $execution.passed -ne $suite.invocations -or $execution.cases.Count -ne $suite.invocations -or
                        $execution.imageLoads -ne 1 -or $execution.sharedImageWrites -ne 0 -or
                        $execution.realKickstartExecution -or $execution.realCopperStartExecution -or
                        $execution.realDosParser -or $execution.shippingOrPureApproval -or $execution.minimumStackQualified) {
                        throw 'Native execution evidence is incomplete or overstates its fixture scope.'
                    }
                    $executionCases = @($execution.cases)
                    $nativeInvocations = $execution.passed
                    if ($Component -eq 'Foundation' -and ($execution.realDosIo -or $execution.referenceCommandBehavior)) {
                        throw 'Foundation evidence overstates real DOS I/O or reference command execution.'
                    }
                }
                if ($Component -eq 'EvalNumeric') {
                    $addressBits = if ($cpu -eq '68000') { 24 } else { 32 }
                    if ($execution.originalEvalExecution -or $execution.fullEvalCommand -or
                        $execution.hostGateways -ne 0 -or $execution.boundaryAddressBits -ne $addressBits) {
                        throw 'Numeric component evidence overstates its execution or address-space scope.'
                    }
                }
                $stackSizes = @($executionCases.configuredStackBytes | Sort-Object -Unique)
                if (4096 -notin $stackSizes -or 16384 -notin $stackSizes) { throw 'Configured stack-size diversity is missing; no minimum stack claim is permitted.' }
                Complete-Stage
                $secondOutput = Join-Path $targetDirectory 'reproducibility.hunk'
                $secondArguments = @($arguments)
                $secondArguments[[Array]::IndexOf($secondArguments, '--output') + 1] = $secondOutput
                $secondArguments[[Array]::IndexOf($secondArguments, '--compatibility-report') + 1] = Join-Path $targetDirectory 'reproducibility.compatibility.json'
                Invoke-BoundDotnet "$cpu/$($suite.id)/reproduce" $secondArguments
                Start-Stage "$cpu/$($suite.id)/check-reproduction"
                if ((Get-FileHash -LiteralPath $secondOutput -Algorithm SHA256).Hash.ToLowerInvariant() -ne $hash) {
                    throw 'Identical inputs did not reproduce identical HUNK bytes.'
                }
                $row.status = $artifactPassStatus; $row.sha256 = $hash; $row.bytes = (Get-Item -LiteralPath $output).Length
                $row.nativeInvocations = $nativeInvocations; $row.configuredStackBytes = $stackSizes
                $row.peakObservedStackBytes = ($executionCases | Measure-Object -Property stackBytesWritten -Maximum).Maximum
                $row.reproducibleBytes = $true; $row.originalOsDifferential = 'not-run'; $row.pureAdmission = 'not-approved'
                $row.evidence = @(Get-ChildItem -LiteralPath $targetDirectory -File | ForEach-Object { Get-FileIdentity $_.FullName 'artifact-evidence' })
                Complete-Stage
            }
            catch {
                $row.status = 'failed'
                $row.failure = $_.Exception.Message
                $currentStage.status = 'failed'; $currentStage.failure = $row.failure; $currentStage.completedUtc = [DateTime]::UtcNow.ToString('o')
                Save-Receipt
                if ($_.Exception -is [IO.InvalidDataException]) { throw }
                Write-Warning "$cpu $($suite.id): $($row.failure)"
            }
        }
    }

    Start-Stage 'final-validation'
    Assert-InputBinding 'final-validation'
    foreach ($row in $summary.artifacts) {
        if ($row.status -eq $artifactPassStatus) { Assert-BoundFiles $row.evidence }
    }
    if ($summary.artifacts.Count -ne $summary.expectedArtifacts -or
        @($summary.artifacts | Where-Object { $_.status -ne $artifactPassStatus }).Count -ne 0) {
        throw 'Command native qualification failed; no successful/latest or packaging admission was written.'
    }
    if ((Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $summary.inputManifest.sha256) {
        throw [IO.InvalidDataException]::new('Input binding drift: input manifest changed.')
    }
    $summary.inputManifest.sourcesAndCopiesVerified = $true
    Complete-Stage
    $summary.status = 'passed'; $summary.allPassed = $true; $summary.completedUtc = [DateTime]::UtcNow.ToString('o')
    Save-Receipt
    $latestStagingPath = Join-Path $runDirectory 'successful-latest.json.next'
    [ordered]@{ report = $summaryPath; sha256 = (Get-FileHash -LiteralPath $summaryPath -Algorithm SHA256).Hash.ToLowerInvariant() } |
        ConvertTo-Json | Set-Content -LiteralPath $latestStagingPath -Encoding utf8
    Move-Item -LiteralPath $latestStagingPath -Destination (Join-Path $qualificationRoot 'latest.json') -Force
    Write-Output "Qualification report: $summaryPath"
    Write-Output "$Component private suites passed on all three HUNK targets. Full commands, actual OS/parser, resident registry, boot, minimum stack and packaging gates remain open."
}
catch {
    $summary.status = 'failed'; $summary.allPassed = $false; $summary.completedUtc = [DateTime]::UtcNow.ToString('o')
    $summary.failure = [ordered]@{ stage = $summary.stage; message = $_.Exception.Message; exceptionType = $_.Exception.GetType().FullName }
    if ($null -ne $currentStage) {
        $currentStage.status = 'failed'; $currentStage.failure = $_.Exception.Message; $currentStage.completedUtc = $summary.completedUtc
    }
    Save-Receipt
    Write-Output "Failed qualification report: $summaryPath"
    throw
}
finally { Pop-Location }
