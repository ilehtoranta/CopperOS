param(
    [string]$CopperSharpRoot = (Join-Path $PSScriptRoot '..\..\..\CopperSharp68k'),
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $repo 'tests\MuiMaster.NativeRoot\CopperOS.MuiMaster.NativeRoot.csproj'
$cli = Join-Path $CopperSharpRoot "Compiler.Cli\bin\$Configuration\net10.0\CopperSharp.Compiler.Cli.dll"
if (-not (Test-Path $cli)) { throw "CopperSharp compiler CLI not found: $cli" }

dotnet build $project --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Native-root managed input build failed.' }

$outDir = Join-Path $repo "tests\MuiMaster.NativeRoot\bin\$Configuration\net10.0"
$assembly = Join-Path $outDir 'CopperOS.MuiMaster.NativeRoot.dll'
$sdk = Join-Path $outDir 'CopperSharp.Sdk.Amiga.dll'
$sdkSupport = Join-Path $outDir 'CopperSharp.Sdk.Amiga.Support.dll'
if (-not (Test-Path $sdkSupport)) { throw "CopperSharp SDK Support assembly not found: $sdkSupport" }
if (-not (Test-Path $sdk)) { throw "CopperSharp SDK assembly not found: $sdk" }
function Qualify-NativeRoot {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Entry,
        [switch]$AllowRelocations
    )

    $hunk = Join-Path $outDir "$Name.hunk"
    $report = Join-Path $outDir "$Name.framework.json"

    & dotnet $cli $assembly --entry $Entry --output $hunk --platform amiga `
        --cpu 68000 --fpu disabled --runtime freestanding --memory none `
        --exceptions yolo --exports none --format hunk --managed-assembly $sdk --managed-assembly $sdkSupport
    if ($LASTEXITCODE -ne 0) { throw "$Name MC68000 HUNK compilation failed." }

    & dotnet $cli $assembly --entry $Entry --platform amiga --cpu 68000 `
        --fpu disabled --runtime freestanding --memory none --exceptions yolo `
        --exports none --format hunk `
        --framework-report $report --managed-assembly $sdk --managed-assembly $sdkSupport
    if ($LASTEXITCODE -ne 0) { throw "$Name framework analysis failed." }

    $data = Get-Content $report -Raw | ConvertFrom-Json
    if (-not $data.IsCompatible) { throw "$Name framework report is not compatible." }
    if ($data.RuntimeProfile -ne 'freestanding' -or $data.Cpu -ne 'm68000') {
        throw "$Name has the wrong runtime or CPU profile."
    }
    if ($data.Members.Count -ne 0 -or $data.ManagedAllocationSites.Count -ne 0) {
        throw "$Name reaches framework members or managed allocation sites."
    }

    $mapPath = "$hunk.map"
    $map = Get-Content $mapPath -Raw
    $strictRelocationsOk = $AllowRelocations
    if (!$strictRelocationsOk) {
        $metric = [regex]::Match($map, 'relocations=(\d+)')
        if (!$metric.Success -or [int]$metric.Groups[1].Value -eq 0) {
            $strictRelocationsOk = $true
        } else {
            # The compiler may materialize a switch jump table as internal
            # generated relocations. They do not introduce external state or
            # managed runtime dependencies, so keep the zero-runtime gate
            # strict while accepting that one mechanically identifiable form.
            $section = [regex]::Match($map,
                '(?ms)^RELOCATIONS\r?\n(?<block>.*?)(?:\r?\n(?:LOOPS|FRAMEWORK FEATURES|RUNTIME HELPERS|EXTERNAL NATIVE TARGETS|REACHABLE ASSEMBLIES)|\z)')
            if ($section.Success) {
                $relocationLines = @($section.Groups['block'].Value -split '\r?\n' |
                    Where-Object { $_.Trim().Length -gt 0 })
                $strictRelocationsOk = $relocationLines.Count -gt 0
                foreach ($line in $relocationLines) {
                    if ($line -notmatch 'generated:allocated-switch-edge:') {
                        $strictRelocationsOk = $false
                        break
                    }
                }
            }
        }
    }
    if ((!$strictRelocationsOk) -or
        $map -notmatch 'framework-features=0' -or
        $map -notmatch 'managed-allocation-sites=0') {
        throw "$Name map failed the zero-runtime gate."
    }
    if ($map -match 'runtime:type-descriptor:') {
        throw "$Name retains a runtime type-descriptor relocation."
    }
    if ((Get-Item $hunk).Length -le 0) { throw "$Name native HUNK is empty." }

    $relocations = 0
    if ($map -match 'relocations=(\d+)') {
        $relocations = [int]$Matches[1]
    }

    [pscustomobject]@{
        Root = $Name
        Entry = $Entry
        Artifact = $hunk
        Map = $mapPath
        Bytes = (Get-Item $hunk).Length
        ReachableMethods = $data.ReachableMethodCount
        FrameworkMembers = $data.Members.Count
        ManagedAllocations = $data.ManagedAllocationSites.Count
        Relocations = $relocations
    }
}

function Qualify-ClosureVariant {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Entry,
        [Parameter(Mandatory = $true)][ValidateSet('68020', '68040')][string]$Cpu
    )

    $hunk = Join-Path $outDir "$Name-m$Cpu.hunk"
    & dotnet $cli $assembly --entry $Entry --output $hunk --platform amiga `
        --cpu $Cpu --fpu disabled --runtime freestanding --memory none `
        --exceptions yolo --exports none --format hunk --managed-assembly $sdk --managed-assembly $sdkSupport
    if ($LASTEXITCODE -ne 0) { throw "$Name MC$Cpu HUNK compilation failed." }

    $mapPath = "$hunk.map"
    $map = Get-Content $mapPath -Raw
    # CPU-specific closures may contain legitimate internal call relocations;
    # the zero-runtime gate is framework/managed-state cleanliness, not a
    # requirement that every ABI variant have identical relocation counts.
    if ($map -notmatch 'framework-features=0' -or
        $map -notmatch 'managed-allocation-sites=0' -or
        $map -match 'runtime:type-descriptor:') {
        throw "$Name MC$Cpu map failed the zero-runtime gate."
    }

    $relocations = 0
    if ($map -match 'relocations=(\d+)') {
        $relocations = [int]$Matches[1]
    }

    [pscustomobject]@{
        Root = $Name
        Cpu = "M$Cpu"
        Artifact = $hunk
        Map = $mapPath
        Bytes = (Get-Item $hunk).Length
        Relocations = $relocations
    }
}


$results = @(
    Qualify-NativeRoot -Name 'mui-vector' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ResolveKnownVector'
    Qualify-NativeRoot -Name 'mui-init' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::InitializeRoot'
    Qualify-NativeRoot -Name 'mui-master-private-root-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MasterPrivateRootStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-master-private-root-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MasterPrivateRootStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-master-private-root-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MasterPrivateRootStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-edit-command-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditCommandStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-edit-command-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditCommandStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-edit-command-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditCommandStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-headless-method-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessMethodStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-headless-method-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessMethodStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-headless-method-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessMethodStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-specialist-hook-message-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SpecialistHookMessageStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-specialist-hook-message-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SpecialistHookMessageStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-specialist-hook-message-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SpecialistHookMessageStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-store-iteration-counter-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreIterationCounterStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-store-iteration-counter-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreIterationCounterStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-store-iteration-counter-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreIterationCounterStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-store-iteration-counter-field-struct-mg2148' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreIterationCounterStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-store-iteration-counter-field-struct-mg2148' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreIterationCounterStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-store-iteration-counter-field-struct-mg2148' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreIterationCounterStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-store-iteration-counter-field-struct-mg2801' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreIterationCounterStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-store-iteration-counter-field-struct-mg2801' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreIterationCounterStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-store-iteration-counter-field-struct-mg2801' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreIterationCounterStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-guest-ulong-memory-struct-mg2149' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GuestUlongStorageStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-guest-ulong-memory-struct-mg2149' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GuestUlongStorageStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-guest-ulong-memory-struct-mg2149' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GuestUlongStorageStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-commands-state-field-struct-mg2150' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCommandsStateStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-commands-state-field-struct-mg2150' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCommandsStateStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-commands-state-field-struct-mg2150' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCommandsStateStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-refresh-state-field-struct-mg2151' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationRefreshStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-refresh-state-field-struct-mg2151' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationRefreshStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-refresh-state-field-struct-mg2151' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationRefreshStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-menu-state-field-struct-mg2152' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuStateRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-menu-state-field-struct-mg2152' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuStateRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-menu-state-field-struct-mg2152' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuStateRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-message-routing-state-field-struct-mg2153' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMessageRoutingStateStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-message-routing-state-field-struct-mg2153' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMessageRoutingStateStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-message-routing-state-field-struct-mg2153' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMessageRoutingStateStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-object-state-field-struct-mg2154' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationObjectStateRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-object-state-field-struct-mg2154' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationObjectStateRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-object-state-field-struct-mg2154' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationObjectStateRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-text-state-field-struct-mg2155' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationTextStateRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-text-state-field-struct-mg2155' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationTextStateRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-text-state-field-struct-mg2155' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationTextStateRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-help-state-field-struct-mg2156' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationHelpStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-help-state-field-struct-mg2156' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationHelpStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-help-state-field-struct-mg2156' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationHelpStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-config-window-state-field-struct-mg2157' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationConfigWindowStateStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-config-window-state-field-struct-mg2157' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationConfigWindowStateStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-config-window-state-field-struct-mg2157' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationConfigWindowStateStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-default-config-state-field-struct-mg2158' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationDefaultConfigStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-default-config-state-field-struct-mg2158' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationDefaultConfigStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-default-config-state-field-struct-mg2158' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationDefaultConfigStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-lifecycle-state-field-struct-mg2159' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationLifecycleStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-lifecycle-state-field-struct-mg2159' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationLifecycleStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-lifecycle-state-field-struct-mg2159' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationLifecycleStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-scheduler-state-field-struct-mg2160' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSchedulerStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-scheduler-state-field-struct-mg2160' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSchedulerStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-scheduler-state-field-struct-mg2160' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSchedulerStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-identity-state-field-struct-mg2161' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIdentityStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-identity-state-field-struct-mg2161' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIdentityStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-identity-state-field-struct-mg2161' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIdentityStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-set-config-item-state-field-struct-mg2162' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSetConfigItemStateStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-set-config-item-state-field-struct-mg2162' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSetConfigItemStateStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-set-config-item-state-field-struct-mg2162' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSetConfigItemStateStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-policy-state-field-struct-mg2163' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPolicyStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-policy-state-field-struct-mg2163' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPolicyStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-policy-state-field-struct-mg2163' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPolicyStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-settings-panel-state-field-struct-mg2164' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPanelStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-settings-panel-state-field-struct-mg2164' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPanelStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-settings-panel-state-field-struct-mg2164' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPanelStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-settings-persistence-state-field-struct-mg2165' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPersistenceStateStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-settings-persistence-state-field-struct-mg2165' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPersistenceStateStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-settings-persistence-state-field-struct-mg2165' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPersistenceStateStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-used-classes-state-field-struct-mg2166' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUsedClassesStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-used-classes-state-field-struct-mg2166' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUsedClassesStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-used-classes-state-field-struct-mg2166' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUsedClassesStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-window-relationship-state-field-struct-mg2167' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowRelationshipStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-window-relationship-state-field-struct-mg2167' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowRelationshipStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-window-relationship-state-field-struct-mg2167' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowRelationshipStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-activation-state-field-struct-mg2168' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-activation-state-field-struct-mg2168' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-activation-state-field-struct-mg2168' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-builtin-font-state-field-struct-mg2169' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBuiltinFontStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-builtin-font-state-field-struct-mg2169' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBuiltinFontStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-builtin-font-state-field-struct-mg2169' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBuiltinFontStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-context-menu-state-field-struct-mg2170' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-context-menu-state-field-struct-mg2170' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-context-menu-state-field-struct-mg2170' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-control-char-state-field-struct-mg2171' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaControlCharStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-control-char-state-field-struct-mg2171' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaControlCharStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-control-char-state-field-struct-mg2171' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaControlCharStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-custom-font-state-field-struct-mg2172' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-state-field-struct-mg2172' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-state-field-struct-mg2172' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-cycle-chain-state-field-struct-mg2173' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCycleChainStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-cycle-chain-state-field-struct-mg2173' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCycleChainStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-cycle-chain-state-field-struct-mg2173' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCycleChainStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-custom-font-runtime-state-field-struct-mg2174' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontRuntimeStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-runtime-state-field-struct-mg2174' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontRuntimeStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-runtime-state-field-struct-mg2174' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontRuntimeStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-disappear-policy-state-field-struct-mg2175' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDisappearPolicyStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-disappear-policy-state-field-struct-mg2175' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDisappearPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-disappear-policy-state-field-struct-mg2175' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDisappearPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-double-buffer-state-field-struct-mg2176' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleBufferStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-double-buffer-state-field-struct-mg2176' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleBufferStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-double-buffer-state-field-struct-mg2176' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleBufferStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-double-click-state-field-struct-mg2177' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleClickStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-double-click-state-field-struct-mg2177' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleClickStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-double-click-state-field-struct-mg2177' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleClickStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-drag-policy-state-field-struct-mg2178' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragPolicyStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-drag-policy-state-field-struct-mg2178' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-drag-policy-state-field-struct-mg2178' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-fixed-text-state-field-struct-mg2179' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFixedTextStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-fixed-text-state-field-struct-mg2179' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFixedTextStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-fixed-text-state-field-struct-mg2179' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFixedTextStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-floating-state-field-struct-mg2180' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFloatingStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-floating-state-field-struct-mg2180' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFloatingStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-floating-state-field-struct-mg2180' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFloatingStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-font-selection-state-field-struct-mg2181' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontSelectionStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-font-selection-state-field-struct-mg2181' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontSelectionStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-font-selection-state-field-struct-mg2181' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontSelectionStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-geometry-state-field-struct-mg2182' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaGeometryStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-geometry-state-field-struct-mg2182' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaGeometryStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-geometry-state-field-struct-mg2182' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaGeometryStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-layout-policy-state-field-struct-mg2183' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaLayoutPolicyStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-layout-policy-state-field-struct-mg2183' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaLayoutPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-layout-policy-state-field-struct-mg2183' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaLayoutPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-presentation-state-field-struct-mg2184' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaPresentationStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-presentation-state-field-struct-mg2184' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaPresentationStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-presentation-state-field-struct-mg2184' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaPresentationStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-render-policy-state-field-struct-mg2185' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaRenderPolicyStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-render-policy-state-field-struct-mg2185' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaRenderPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-render-policy-state-field-struct-mg2185' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaRenderPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-short-help-state-field-struct-mg2186' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-short-help-state-field-struct-mg2186' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-short-help-state-field-struct-mg2186' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-text-color-state-field-struct-mg2187' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTextColorStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-text-color-state-field-struct-mg2187' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTextColorStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-text-color-state-field-struct-mg2187' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTextColorStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-timer-state-field-struct-mg2188' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-timer-state-field-struct-mg2188' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-timer-state-field-struct-mg2188' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-timer-event-state-field-struct-mg2189' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerEventStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-timer-event-state-field-struct-mg2189' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerEventStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-timer-event-state-field-struct-mg2189' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerEventStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-weight-state-field-struct-mg2190' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaWeightStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-weight-state-field-struct-mg2190' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaWeightStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-weight-state-field-struct-mg2190' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaWeightStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-balance-policy-state-field-struct-mg2191' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BalancePolicyStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-balance-policy-state-field-struct-mg2191' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BalancePolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-balance-policy-state-field-struct-mg2191' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BalancePolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bitmap-geometry-state-field-struct-mg2192' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapGeometryStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-bitmap-geometry-state-field-struct-mg2192' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapGeometryStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bitmap-geometry-state-field-struct-mg2192' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapGeometryStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bitmap-policy-state-field-struct-mg2193' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapPolicyStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-bitmap-policy-state-field-struct-mg2193' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bitmap-policy-state-field-struct-mg2193' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bitmap-source-state-field-struct-mg2194' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapSourceStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-bitmap-source-state-field-struct-mg2194' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapSourceStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bitmap-source-state-field-struct-mg2194' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapSourceStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bitmap-remapped-state-field-struct-mg2195' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapRemappedStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-bitmap-remapped-state-field-struct-mg2195' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapRemappedStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bitmap-remapped-state-field-struct-mg2195' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapRemappedStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bodychunk-format-state-field-struct-mg2196' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BodychunkFormatStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-bodychunk-format-state-field-struct-mg2196' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BodychunkFormatStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bodychunk-format-state-field-struct-mg2196' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BodychunkFormatStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-choice-active-state-field-struct-mg2197' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceActiveStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-choice-active-state-field-struct-mg2197' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceActiveStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-choice-active-state-field-struct-mg2197' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceActiveStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-choice-entries-state-field-struct-mg2198' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceEntriesStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-choice-entries-state-field-struct-mg2198' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceEntriesStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-choice-entries-state-field-struct-mg2198' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceEntriesStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-control-font-state-field-struct-mg2199' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-control-font-state-field-struct-mg2199' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-control-font-state-field-struct-mg2199' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-control-font-resolution-state-field-struct-mg2200' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontResolutionStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-control-font-resolution-state-field-struct-mg2200' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontResolutionStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-control-font-resolution-state-field-struct-mg2200' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontResolutionStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-gadget-gadget-state-field-struct-mg2201' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetGadgetStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-gadget-gadget-state-field-struct-mg2201' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetGadgetStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-gadget-gadget-state-field-struct-mg2201' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetGadgetStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-gadget-interaction-state-field-struct-mg2202' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetInteractionStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-gadget-interaction-state-field-struct-mg2202' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetInteractionStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-gadget-interaction-state-field-struct-mg2202' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetInteractionStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-gauge-inforate-state-field-struct-mg2203' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoRateStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-gauge-inforate-state-field-struct-mg2203' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoRateStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-gauge-inforate-state-field-struct-mg2203' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoRateStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-gauge-infotext-state-field-struct-mg2204' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoTextStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-gauge-infotext-state-field-struct-mg2204' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoTextStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-gauge-infotext-state-field-struct-mg2204' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoTextStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-gauge-state-field-struct-mg2205' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-gauge-state-field-struct-mg2205' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-gauge-state-field-struct-mg2205' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-layout-hook-state-field-struct-mg2206' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutHookStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-group-layout-hook-state-field-struct-mg2206' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutHookStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-layout-hook-state-field-struct-mg2206' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutHookStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-layout-policy-state-field-struct-mg2207' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutPolicyStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-group-layout-policy-state-field-struct-mg2207' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-layout-policy-state-field-struct-mg2207' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-grid-state-field-struct-mg2208' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-group-grid-state-field-struct-mg2208' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-grid-state-field-struct-mg2208' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-page-state-field-struct-mg2209' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupPageStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-group-page-state-field-struct-mg2209' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupPageStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-page-state-field-struct-mg2209' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupPageStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-image-spec-state-field-struct-mg2210' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageSpecStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-image-spec-state-field-struct-mg2210' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageSpecStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-image-spec-state-field-struct-mg2210' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageSpecStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-image-render-state-field-struct-mg2211' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageRenderStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-image-render-state-field-struct-mg2211' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageRenderStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-image-render-state-field-struct-mg2211' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageRenderStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-image-oldimage-state-field-struct-mg2212' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageOldImageStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-image-oldimage-state-field-struct-mg2212' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageOldImageStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-image-oldimage-state-field-struct-mg2212' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageOldImageStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-image-fontmatchstring-state-field-struct-mg2213' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStringStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-image-fontmatchstring-state-field-struct-mg2213' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStringStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-image-fontmatchstring-state-field-struct-mg2213' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStringStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-image-fontmatch-state-field-struct-mg2214' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-image-fontmatch-state-field-struct-mg2214' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-image-fontmatch-state-field-struct-mg2214' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-levelmeter-label-state-field-struct-mg2215' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterLabelStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-levelmeter-label-state-field-struct-mg2215' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterLabelStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-levelmeter-label-state-field-struct-mg2215' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterLabelStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-levelmeter-presentation-state-field-struct-mg2216' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterPresentationStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-levelmeter-presentation-state-field-struct-mg2216' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterPresentationStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-levelmeter-presentation-state-field-struct-mg2216' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterPresentationStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-numeric-format-state-field-struct-mg2217' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NumericFormatStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-numeric-format-state-field-struct-mg2217' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NumericFormatStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-numeric-format-state-field-struct-mg2217' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NumericFormatStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-prop-range-state-field-struct-mg2218' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropRangeStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-prop-range-state-field-struct-mg2218' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropRangeStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-prop-range-state-field-struct-mg2218' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropRangeStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-prop-policy-state-field-struct-mg2219' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropPolicyStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-prop-policy-state-field-struct-mg2219' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-prop-policy-state-field-struct-mg2219' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-scrollbar-layout-state-field-struct-mg2220' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollbarLayoutStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-scrollbar-layout-state-field-struct-mg2220' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollbarLayoutStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-scrollbar-layout-state-field-struct-mg2220' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollbarLayoutStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-slider-presentation-state-field-struct-mg2221' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SliderPresentationStateFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-slider-presentation-state-field-struct-mg2221' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SliderPresentationStateFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-slider-presentation-state-field-struct-mg2221' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SliderPresentationStateFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-rectangle-bartitle-state-field-struct-mg2222' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectangleBarTitleStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-rectangle-bartitle-state-field-struct-mg2222' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectangleBarTitleStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-rectangle-bartitle-state-field-struct-mg2222' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectangleBarTitleStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-rectangle-presentation-state-field-struct-mg2223' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectanglePresentationStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-rectangle-presentation-state-field-struct-mg2223' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectanglePresentationStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-rectangle-presentation-state-field-struct-mg2223' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectanglePresentationStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-scale-presentation-state-field-struct-mg2224' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScalePresentationStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-scale-presentation-state-field-struct-mg2224' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScalePresentationStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-scale-presentation-state-field-struct-mg2224' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScalePresentationStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-register-policy-state-field-struct-mg2225' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RegisterPolicyStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-register-policy-state-field-struct-mg2225' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RegisterPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-register-policy-state-field-struct-mg2225' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RegisterPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-keyadjust-text-field-struct-mg2226' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::KeyadjustTextFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-keyadjust-text-field-struct-mg2226' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::KeyadjustTextFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-keyadjust-text-field-struct-mg2226' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::KeyadjustTextFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-makeobject-preparse-field-struct-mg2227' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectPreParseFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-makeobject-preparse-field-struct-mg2227' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectPreParseFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-makeobject-preparse-field-struct-mg2227' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectPreParseFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-pointer-state-field-struct-mg2228' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollPointerStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-stringscroll-pointer-state-field-struct-mg2228' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollPointerStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-pointer-state-field-struct-mg2228' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollPointerStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-help-state-field-struct-mg2229' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HelpStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-help-state-field-struct-mg2229' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HelpStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-help-state-field-struct-mg2229' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HelpStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-acknowledge-state-field-struct-mg2230' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAcknowledgeStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-acknowledge-state-field-struct-mg2230' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAcknowledgeStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-acknowledge-state-field-struct-mg2230' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAcknowledgeStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-contents-state-field-struct-mg2231' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringContentsStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-contents-state-field-struct-mg2231' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringContentsStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-contents-state-field-struct-mg2231' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringContentsStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-cursor-state-field-struct-mg2232' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringCursorStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-cursor-state-field-struct-mg2232' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringCursorStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-cursor-state-field-struct-mg2232' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringCursorStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-interaction-state-field-struct-mg2233' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteractionStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-interaction-state-field-struct-mg2233' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteractionStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-interaction-state-field-struct-mg2233' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteractionStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-filter-state-field-struct-mg2234' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringFilterStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-filter-state-field-struct-mg2234' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringFilterStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-filter-state-field-struct-mg2234' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringFilterStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-placeholder-state-field-struct-mg2235' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPlaceholderStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-placeholder-state-field-struct-mg2235' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPlaceholderStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-placeholder-state-field-struct-mg2235' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPlaceholderStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-edit-hook-state-field-struct-mg2236' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditHookStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-edit-hook-state-field-struct-mg2236' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditHookStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-edit-hook-state-field-struct-mg2236' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditHookStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-presentation-state-field-struct-mg2237' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPresentationStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-presentation-state-field-struct-mg2237' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPresentationStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-presentation-state-field-struct-mg2237' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPresentationStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-attached-list-state-field-struct-mg2238' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAttachedListStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-attached-list-state-field-struct-mg2238' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAttachedListStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-attached-list-state-field-struct-mg2238' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAttachedListStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-spell-checking-state-field-struct-mg2239' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringSpellCheckingStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-spell-checking-state-field-struct-mg2239' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringSpellCheckingStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-spell-checking-state-field-struct-mg2239' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringSpellCheckingStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-scroll-metrics-state-field-struct-mg2240' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringScrollMetricsStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-scroll-metrics-state-field-struct-mg2240' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringScrollMetricsStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-scroll-metrics-state-field-struct-mg2240' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringScrollMetricsStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-contents-state-field-struct-mg2241' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextContentsStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-text-contents-state-field-struct-mg2241' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextContentsStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-contents-state-field-struct-mg2241' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextContentsStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-copy-state-field-struct-mg2242' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextCopyStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-text-copy-state-field-struct-mg2242' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextCopyStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-copy-state-field-struct-mg2242' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextCopyStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-preparse-state-field-struct-mg2243' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPreParseStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-text-preparse-state-field-struct-mg2243' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPreParseStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-preparse-state-field-struct-mg2243' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPreParseStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-presentation-state-field-struct-mg2244' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPresentationStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-text-presentation-state-field-struct-mg2244' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPresentationStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-presentation-state-field-struct-mg2244' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPresentationStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-shortened-state-field-struct-mg2245' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextShortenedStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-text-shortened-state-field-struct-mg2245' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextShortenedStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-shortened-state-field-struct-mg2245' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextShortenedStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-unicode-state-field-struct-mg2246' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextUnicodeStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-text-unicode-state-field-struct-mg2246' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextUnicodeStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-unicode-state-field-struct-mg2246' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextUnicodeStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-control-state-field-struct-mg2247' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowControlStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-control-state-field-struct-mg2247' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowControlStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-control-state-field-struct-mg2247' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowControlStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-state-field-struct-mg2248' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-event-state-field-struct-mg2248' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-state-field-struct-mg2248' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-focus-state-field-struct-mg2249' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowFocusStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-focus-state-field-struct-mg2249' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowFocusStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-focus-state-field-struct-mg2249' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowFocusStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-lifecycle-state-field-struct-mg2250' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowLifecycleStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-lifecycle-state-field-struct-mg2250' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowLifecycleStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-lifecycle-state-field-struct-mg2250' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowLifecycleStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-interaction-state-field-struct-mg2251' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowInteractionStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-interaction-state-field-struct-mg2251' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowInteractionStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-interaction-state-field-struct-mg2251' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowInteractionStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-presentation-state-field-struct-mg2252' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowPresentationStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-presentation-state-field-struct-mg2252' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowPresentationStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-presentation-state-field-struct-mg2252' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowPresentationStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-relationship-state-field-struct-mg2253' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowRelationshipStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-relationship-state-field-struct-mg2253' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowRelationshipStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-relationship-state-field-struct-mg2253' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowRelationshipStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-visual-state-field-struct-mg2254' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowVisualStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-visual-state-field-struct-mg2254' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowVisualStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-visual-state-field-struct-mg2254' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowVisualStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-open-policy-state-field-struct-mg2255' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowOpenPolicyStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-open-policy-state-field-struct-mg2255' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowOpenPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-open-policy-state-field-struct-mg2255' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowOpenPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-reuse-state-field-struct-mg2256' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventReuseStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-event-reuse-state-field-struct-mg2256' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventReuseStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-reuse-state-field-struct-mg2256' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventReuseStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-input-event-field-struct-mg2257' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowInputEventFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-input-event-field-struct-mg2257' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowInputEventFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-input-event-field-struct-mg2257' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowInputEventFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-input-event-field-cursor-struct-mg2567' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowInputEventFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-input-event-field-cursor-struct-mg2567' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowInputEventFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-input-event-field-cursor-struct-mg2567' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowInputEventFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-numeric-state-field-cursor-struct-mg2568' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NumericStateRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-numeric-state-field-cursor-struct-mg2568' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NumericStateRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-numeric-state-field-cursor-struct-mg2568' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NumericStateRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-image-fontmatch-state-field-cursor-struct-mg2569' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-image-fontmatch-state-field-cursor-struct-mg2569' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-image-fontmatch-state-field-cursor-struct-mg2569' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-image-spec-state-field-cursor-struct-mg2570' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageSpecStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-image-spec-state-field-cursor-struct-mg2570' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageSpecStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-image-spec-state-field-cursor-struct-mg2570' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageSpecStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-image-render-state-field-cursor-struct-mg2571' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageRenderStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-image-render-state-field-cursor-struct-mg2571' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageRenderStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-image-render-state-field-cursor-struct-mg2571' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageRenderStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-grid-spec-field-cursor-struct-mg2572' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridSpecFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-group-grid-spec-field-cursor-struct-mg2572' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridSpecFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-grid-spec-field-cursor-struct-mg2572' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridSpecFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-change-field-cursor-struct-mg2573' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupChangeFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-group-change-field-cursor-struct-mg2573' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupChangeFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-change-field-cursor-struct-mg2573' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupChangeFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'class-service-field-cursor-struct-mg2574' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ClassServiceFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'class-service-field-cursor-struct-mg2574' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ClassServiceFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'class-service-field-cursor-struct-mg2574' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ClassServiceFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'floattext-policy-field-cursor-struct-mg2575' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FloattextPolicyFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'floattext-policy-field-cursor-struct-mg2575' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FloattextPolicyFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'floattext-policy-field-cursor-struct-mg2575' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FloattextPolicyFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-family-get-child-field-cursor-struct-mg2576' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyGetChildFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-family-get-child-field-cursor-struct-mg2576' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyGetChildFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-family-get-child-field-cursor-struct-mg2576' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyGetChildFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-color-specialist-field-cursor-struct-mg2577' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorSpecialistFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-color-specialist-field-cursor-struct-mg2577' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorSpecialistFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-color-specialist-field-cursor-struct-mg2577' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorSpecialistFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-wrapper-field-cursor-struct-mg2578' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-external-wrapper-field-cursor-struct-mg2578' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-wrapper-field-cursor-struct-mg2578' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-ordering-field-cursor-struct-mg2579' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupOrderingFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-group-ordering-field-cursor-struct-mg2579' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupOrderingFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-ordering-field-cursor-struct-mg2579' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupOrderingFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-edit-hook-field-cursor-struct-mg2580' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditHookFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-edit-hook-field-cursor-struct-mg2580' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditHookFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-edit-hook-field-cursor-struct-mg2580' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditHookFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-interaction-field-cursor-struct-mg2581' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteractionFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-interaction-field-cursor-struct-mg2581' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteractionFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-interaction-field-cursor-struct-mg2581' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteractionFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-presentation-field-cursor-struct-mg2582' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPresentationFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-presentation-field-cursor-struct-mg2582' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPresentationFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-presentation-field-cursor-struct-mg2582' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPresentationFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-scroll-metrics-field-cursor-struct-mg2583' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringScrollMetricsFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-scroll-metrics-field-cursor-struct-mg2583' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringScrollMetricsFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-scroll-metrics-field-cursor-struct-mg2583' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringScrollMetricsFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-pointer-field-cursor-struct-mg2584' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollPointerStateFieldCursorStructRecordCodecRootMg2718'
    Qualify-ClosureVariant -Name 'mui-stringscroll-pointer-field-cursor-struct-mg2584' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollPointerStateFieldCursorStructRecordCodecRootMg2718' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-pointer-field-cursor-struct-mg2584' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollPointerStateFieldCursorStructRecordCodecRootMg2718' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-state-field-cursor-struct-mg2585' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollStateFieldCursorStructRecordCodecRootMg2719'
    Qualify-ClosureVariant -Name 'mui-stringscroll-state-field-cursor-struct-mg2585' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollStateFieldCursorStructRecordCodecRootMg2719' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-state-field-cursor-struct-mg2585' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollStateFieldCursorStructRecordCodecRootMg2719' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-policy-field-cursor-struct-mg2586' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollPolicyFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-stringscroll-policy-field-cursor-struct-mg2586' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollPolicyFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-policy-field-cursor-struct-mg2586' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollPolicyFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-scrollbar-field-cursor-struct-mg2587' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollScrollbarFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-stringscroll-scrollbar-field-cursor-struct-mg2587' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollScrollbarFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-scrollbar-field-cursor-struct-mg2587' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollScrollbarFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-composition-field-cursor-struct-mg2588' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollCompositionFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-stringscroll-composition-field-cursor-struct-mg2588' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollCompositionFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-composition-field-cursor-struct-mg2588' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollCompositionFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-layout-field-cursor-struct-mg2589' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollLayoutFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-stringscroll-layout-field-cursor-struct-mg2589' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollLayoutFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-layout-field-cursor-struct-mg2589' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollLayoutFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-render-field-cursor-struct-mg2590' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollRenderFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-stringscroll-render-field-cursor-struct-mg2590' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollRenderFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-render-field-cursor-struct-mg2590' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollRenderFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-viewport-field-cursor-struct-mg2591' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollViewportFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-stringscroll-viewport-field-cursor-struct-mg2591' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollViewportFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-viewport-field-cursor-struct-mg2591' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollViewportFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-color-record-field-cursor-struct-mg2592' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorRecordFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-color-record-field-cursor-struct-mg2592' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorRecordFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-color-record-field-cursor-struct-mg2592' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorRecordFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-drawing-record-field-cursor-struct-mg2593' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DrawingRecordFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-drawing-record-field-cursor-struct-mg2593' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DrawingRecordFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-drawing-record-field-cursor-struct-mg2593' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DrawingRecordFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-scrollgroup-viewport-field-cursor-struct-mg2594' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupViewportFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-scrollgroup-viewport-field-cursor-struct-mg2594' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupViewportFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-scrollgroup-viewport-field-cursor-struct-mg2594' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupViewportFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-scrollgroup-border-scroller-field-cursor-struct-mg2595' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupBorderScrollerFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-scrollgroup-border-scroller-field-cursor-struct-mg2595' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupBorderScrollerFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-scrollgroup-border-scroller-field-cursor-struct-mg2595' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupBorderScrollerFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-specialized-layout-field-cursor-struct-mg2596' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SpecializedLayoutFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-specialized-layout-field-cursor-struct-mg2596' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SpecializedLayoutFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-specialized-layout-field-cursor-struct-mg2596' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SpecializedLayoutFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-header-field-cursor-struct-mg2597' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeHeaderFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-header-field-cursor-struct-mg2597' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeHeaderFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-header-field-cursor-struct-mg2597' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeHeaderFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-column-geometry-field-cursor-struct-mg2598' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeColumnGeometryFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-column-geometry-field-cursor-struct-mg2598' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeColumnGeometryFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-column-geometry-field-cursor-struct-mg2598' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeColumnGeometryFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-display-snapshot-field-cursor-struct-mg2599' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeDisplaySnapshotFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-display-snapshot-field-cursor-struct-mg2599' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeDisplaySnapshotFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-display-snapshot-field-cursor-struct-mg2599' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeDisplaySnapshotFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-policy-field-cursor-struct-mg2600' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreePolicyFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-policy-field-cursor-struct-mg2600' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreePolicyFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-policy-field-cursor-struct-mg2600' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreePolicyFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-hook-pool-field-cursor-struct-mg2601' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeHookPoolFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-hook-pool-field-cursor-struct-mg2601' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeHookPoolFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-hook-pool-field-cursor-struct-mg2601' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeHookPoolFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-click-state-field-cursor-struct-mg2602' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeClickStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-click-state-field-cursor-struct-mg2602' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeClickStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-click-state-field-cursor-struct-mg2602' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeClickStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-click-column-field-cursor-struct-mg2603' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeClickColumnFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-click-column-field-cursor-struct-mg2603' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeClickColumnFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-click-column-field-cursor-struct-mg2603' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeClickColumnFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-surface-field-cursor-struct-mg2604' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeSurfaceFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-surface-field-cursor-struct-mg2604' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeSurfaceFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-surface-field-cursor-struct-mg2604' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeSurfaceFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-lifecycle-field-cursor-struct-mg2605' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeLifecycleFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-lifecycle-field-cursor-struct-mg2605' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeLifecycleFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-lifecycle-field-cursor-struct-mg2605' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeLifecycleFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-node-field-cursor-struct-mg2606' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeNodeFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-node-field-cursor-struct-mg2606' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeNodeFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-node-field-cursor-struct-mg2606' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeNodeFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-presentation-field-cursor-struct-mg2607' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreePresentationFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-presentation-field-cursor-struct-mg2607' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreePresentationFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-presentation-field-cursor-struct-mg2607' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreePresentationFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-message-field-cursor-struct-mg2608' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeMessageFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-message-field-cursor-struct-mg2608' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeMessageFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-message-field-cursor-struct-mg2608' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeMessageFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-display-column-field-cursor-struct-mg2609' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeDisplayColumnFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-display-column-field-cursor-struct-mg2609' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeDisplayColumnFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-display-column-field-cursor-struct-mg2609' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeDisplayColumnFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-display-column-vector-cursor-struct-mg2610' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeDisplayColumnVectorCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-display-column-vector-cursor-struct-mg2610' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeDisplayColumnVectorCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-display-column-vector-cursor-struct-mg2610' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeDisplayColumnVectorCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-column-geometry-vector-cursor-struct-mg2611' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeColumnGeometryVectorCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-column-geometry-vector-cursor-struct-mg2611' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeColumnGeometryVectorCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-column-geometry-vector-cursor-struct-mg2611' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeColumnGeometryVectorCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-child-field-cursor-struct-mg2612' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewChildStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-child-field-cursor-struct-mg2612' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewChildStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-child-field-cursor-struct-mg2612' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewChildStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-click-state-field-cursor-struct-mg2613' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewClickStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-click-state-field-cursor-struct-mg2613' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewClickStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-click-state-field-cursor-struct-mg2613' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewClickStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-selection-signal-field-cursor-struct-mg2614' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewSelectionSignalFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-selection-signal-field-cursor-struct-mg2614' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewSelectionSignalFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-selection-signal-field-cursor-struct-mg2614' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewSelectionSignalFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-layout-field-cursor-struct-mg2615' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewLayoutFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-layout-field-cursor-struct-mg2615' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewLayoutFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-layout-field-cursor-struct-mg2615' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewLayoutFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-render-field-cursor-struct-mg2616' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewRenderFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-render-field-cursor-struct-mg2616' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewRenderFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-render-field-cursor-struct-mg2616' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewRenderFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-external-connection-field-cursor-struct-mg2617' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewExternalScrollerConnectionFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-external-connection-field-cursor-struct-mg2617' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewExternalScrollerConnectionFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-external-connection-field-cursor-struct-mg2617' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewExternalScrollerConnectionFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-scroller-field-cursor-struct-mg2618' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewScrollerFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-scroller-field-cursor-struct-mg2618' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewScrollerFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-scroller-field-cursor-struct-mg2618' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewScrollerFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-interaction-policy-field-cursor-struct-mg2619' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewInteractionPolicyFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-interaction-policy-field-cursor-struct-mg2619' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewInteractionPolicyFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-interaction-policy-field-cursor-struct-mg2619' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewInteractionPolicyFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-horizontal-scroller-field-cursor-struct-mg2620' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHorizontalScrollerFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-horizontal-scroller-field-cursor-struct-mg2620' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHorizontalScrollerFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-horizontal-scroller-field-cursor-struct-mg2620' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHorizontalScrollerFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-horizontal-drag-field-cursor-struct-mg2621' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHorizontalScrollerDragStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-horizontal-drag-field-cursor-struct-mg2621' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHorizontalScrollerDragStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-horizontal-drag-field-cursor-struct-mg2621' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHorizontalScrollerDragStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-vertical-drag-field-cursor-struct-mg2622' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewScrollerDragStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-vertical-drag-field-cursor-struct-mg2622' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewScrollerDragStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-vertical-drag-field-cursor-struct-mg2622' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewScrollerDragStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-owner-field-cursor-struct-mg2623' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewOwnerStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-owner-field-cursor-struct-mg2623' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewOwnerStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-owner-field-cursor-struct-mg2623' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewOwnerStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-hscroller-field-cursor-struct-mg2624' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHScrollerStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-hscroller-field-cursor-struct-mg2624' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHScrollerStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-hscroller-field-cursor-struct-mg2624' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHScrollerStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-slot-field-cursor-struct-mg2625' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListSlotFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-slot-field-cursor-struct-mg2625' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListSlotFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-slot-field-cursor-struct-mg2625' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListSlotFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-image-field-cursor-struct-mg2626' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListImageFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-image-field-cursor-struct-mg2626' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListImageFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-image-field-cursor-struct-mg2626' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListImageFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-title-array-field-cursor-struct-mg2627' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTitleArrayStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-title-array-field-cursor-struct-mg2627' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTitleArrayStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-title-array-field-cursor-struct-mg2627' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTitleArrayStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-title-field-cursor-struct-mg2628' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTitleStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-title-field-cursor-struct-mg2628' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTitleStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-title-field-cursor-struct-mg2628' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTitleStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-selection-signal-field-cursor-struct-mg2629' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListSelectionSignalStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-selection-signal-field-cursor-struct-mg2629' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListSelectionSignalStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-selection-signal-field-cursor-struct-mg2629' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListSelectionSignalStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-format-policy-field-cursor-struct-mg2630' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatPolicyStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-format-policy-field-cursor-struct-mg2630' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatPolicyStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-format-policy-field-cursor-struct-mg2630' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatPolicyStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-font-field-cursor-struct-mg2631' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFontStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-font-field-cursor-struct-mg2631' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFontStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-font-field-cursor-struct-mg2631' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFontStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-pointer-slot-field-cursor-struct-mg2632' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPointerSlotFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-pointer-slot-field-cursor-struct-mg2632' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPointerSlotFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-pointer-slot-field-cursor-struct-mg2632' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPointerSlotFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-edit-field-cursor-struct-mg2633' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListEditStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-edit-field-cursor-struct-mg2633' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListEditStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-edit-field-cursor-struct-mg2633' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListEditStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-column-geometry-field-cursor-struct-mg2634' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnGeometryFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-column-geometry-field-cursor-struct-mg2634' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnGeometryFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-column-geometry-field-cursor-struct-mg2634' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnGeometryFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-column-layout-field-cursor-struct-mg2635' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnLayoutFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-column-layout-field-cursor-struct-mg2635' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnLayoutFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-column-layout-field-cursor-struct-mg2635' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnLayoutFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-owned-record-header-field-cursor-struct-mg2636' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListOwnedRecordHeaderFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-owned-record-header-field-cursor-struct-mg2636' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListOwnedRecordHeaderFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-owned-record-header-field-cursor-struct-mg2636' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListOwnedRecordHeaderFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-redraw-field-cursor-struct-mg2637' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListRedrawStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-redraw-field-cursor-struct-mg2637' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListRedrawStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-redraw-field-cursor-struct-mg2637' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListRedrawStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-active-field-cursor-struct-mg2638' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListActiveStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-active-field-cursor-struct-mg2638' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListActiveStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-active-field-cursor-struct-mg2638' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListActiveStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-insert-position-field-cursor-struct-mg2639' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInsertPositionStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-insert-position-field-cursor-struct-mg2639' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInsertPositionStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-insert-position-field-cursor-struct-mg2639' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInsertPositionStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-viewport-field-cursor-struct-mg2640' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListViewportStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-viewport-field-cursor-struct-mg2640' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListViewportStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-viewport-field-cursor-struct-mg2640' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListViewportStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-interaction-policy-field-cursor-struct-mg2641' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInteractionPolicyStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-interaction-policy-field-cursor-struct-mg2641' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInteractionPolicyStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-interaction-policy-field-cursor-struct-mg2641' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInteractionPolicyStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-click-field-cursor-struct-mg2642' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListClickStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-click-field-cursor-struct-mg2642' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListClickStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-click-field-cursor-struct-mg2642' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListClickStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-hook-policy-field-cursor-struct-mg2643' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHookPolicyStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-hook-policy-field-cursor-struct-mg2643' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHookPolicyStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-hook-policy-field-cursor-struct-mg2643' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHookPolicyStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-sort-field-cursor-struct-mg2644' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListSortStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-sort-field-cursor-struct-mg2644' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListSortStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-sort-field-cursor-struct-mg2644' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListSortStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-pool-policy-field-cursor-struct-mg2645' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPoolPolicyFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-pool-policy-field-cursor-struct-mg2645' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPoolPolicyFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-pool-policy-field-cursor-struct-mg2645' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPoolPolicyFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-presentation-policy-field-cursor-struct-mg2646' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPresentationPolicyStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-presentation-policy-field-cursor-struct-mg2646' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPresentationPolicyStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-presentation-policy-field-cursor-struct-mg2646' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPresentationPolicyStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-header-field-cursor-struct-mg2647' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHeaderFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-header-field-cursor-struct-mg2647' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHeaderFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-header-field-cursor-struct-mg2647' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHeaderFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-column-visibility-field-cursor-struct-mg2648' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnVisibilityStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-column-visibility-field-cursor-struct-mg2648' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnVisibilityStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-column-visibility-field-cursor-struct-mg2648' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnVisibilityStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-column-order-field-cursor-struct-mg2649' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnOrderStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-column-order-field-cursor-struct-mg2649' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnOrderStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-column-order-field-cursor-struct-mg2649' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnOrderStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-format-descriptor-field-cursor-struct-mg2650' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatDescriptorFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-format-descriptor-field-cursor-struct-mg2650' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatDescriptorFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-format-descriptor-field-cursor-struct-mg2650' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatDescriptorFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-format-descriptor-state-field-cursor-struct-mg2651' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatDescriptorStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-format-descriptor-state-field-cursor-struct-mg2651' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatDescriptorStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-format-descriptor-state-field-cursor-struct-mg2651' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatDescriptorStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-column-metrics-field-cursor-struct-mg2652' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnMetricsFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-column-metrics-field-cursor-struct-mg2652' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnMetricsFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-column-metrics-field-cursor-struct-mg2652' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnMetricsFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-state-field-cursor-struct-mg2653' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-state-field-cursor-struct-mg2653' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-state-field-cursor-struct-mg2653' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-store-pool-iteration-field-cursor-struct-mg2654' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StorePoolAndIterationFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-store-pool-iteration-field-cursor-struct-mg2654' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StorePoolAndIterationFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-store-pool-iteration-field-cursor-struct-mg2654' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StorePoolAndIterationFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-integer64-field-cursor-struct-mg2655' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteger64StructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-integer64-field-cursor-struct-mg2655' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteger64StructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-integer64-field-cursor-struct-mg2655' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteger64StructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-state-field-cursor-struct-mg2656' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-event-state-field-cursor-struct-mg2656' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-state-field-cursor-struct-mg2656' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-reuse-state-field-cursor-struct-mg2657' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventReuseStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-event-reuse-state-field-cursor-struct-mg2657' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventReuseStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-reuse-state-field-cursor-struct-mg2657' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventReuseStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-lifecycle-state-field-cursor-struct-mg2658' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowLifecycleStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-lifecycle-state-field-cursor-struct-mg2658' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowLifecycleStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-lifecycle-state-field-cursor-struct-mg2658' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowLifecycleStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-help-state-field-cursor-struct-mg2659' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HelpStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-help-state-field-cursor-struct-mg2659' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HelpStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-help-state-field-cursor-struct-mg2659' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HelpStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-presentation-state-field-cursor-struct-mg2660' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPresentationStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-text-presentation-state-field-cursor-struct-mg2660' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPresentationStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-presentation-state-field-cursor-struct-mg2660' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPresentationStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-specialist-hook-message-field-cursor-struct-mg2661' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SpecialistHookMessageFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-specialist-hook-message-field-cursor-struct-mg2661' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SpecialistHookMessageFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-specialist-hook-message-field-cursor-struct-mg2661' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SpecialistHookMessageFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-virtgroup-policy-field-cursor-struct-mg2662' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::VirtgroupPolicyFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-virtgroup-policy-field-cursor-struct-mg2662' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::VirtgroupPolicyFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-virtgroup-policy-field-cursor-struct-mg2662' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::VirtgroupPolicyFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-shortened-state-field-cursor-struct-mg2663' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextShortenedStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-text-shortened-state-field-cursor-struct-mg2663' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextShortenedStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-shortened-state-field-cursor-struct-mg2663' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextShortenedStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-balance-policy-field-cursor-struct-mg2664' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BalancePolicyStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-balance-policy-field-cursor-struct-mg2664' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BalancePolicyStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-balance-policy-field-cursor-struct-mg2664' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BalancePolicyStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bitmap-geometry-field-cursor-struct-mg2665' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapGeometryStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-bitmap-geometry-field-cursor-struct-mg2665' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapGeometryStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bitmap-geometry-field-cursor-struct-mg2665' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapGeometryStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bitmap-remapped-field-cursor-struct-mg2666' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapRemappedStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-bitmap-remapped-field-cursor-struct-mg2666' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapRemappedStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bitmap-remapped-field-cursor-struct-mg2666' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapRemappedStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bitmap-source-field-cursor-struct-mg2667' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapSourceStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-bitmap-source-field-cursor-struct-mg2667' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapSourceStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bitmap-source-field-cursor-struct-mg2667' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapSourceStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bitmap-policy-field-cursor-struct-mg2668' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapPolicyStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-bitmap-policy-field-cursor-struct-mg2668' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapPolicyStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bitmap-policy-field-cursor-struct-mg2668' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapPolicyStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bodychunk-format-field-cursor-struct-mg2669' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BodychunkFormatStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-bodychunk-format-field-cursor-struct-mg2669' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BodychunkFormatStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bodychunk-format-field-cursor-struct-mg2669' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BodychunkFormatStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-choice-active-field-cursor-struct-mg2670' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceActiveStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-choice-active-field-cursor-struct-mg2670' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceActiveStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-choice-active-field-cursor-struct-mg2670' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceActiveStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-choice-entries-field-cursor-struct-mg2671' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceEntriesStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-choice-entries-field-cursor-struct-mg2671' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceEntriesStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-choice-entries-field-cursor-struct-mg2671' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceEntriesStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-control-font-field-cursor-struct-mg2672' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-control-font-field-cursor-struct-mg2672' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-control-font-field-cursor-struct-mg2672' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-control-font-resolution-field-cursor-struct-mg2673' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontResolutionFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-control-font-resolution-field-cursor-struct-mg2673' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontResolutionFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-control-font-resolution-field-cursor-struct-mg2673' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontResolutionFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-gadget-gadget-field-cursor-struct-mg2674' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetGadgetStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-gadget-gadget-field-cursor-struct-mg2674' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetGadgetStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-gadget-gadget-field-cursor-struct-mg2674' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetGadgetStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-gadget-interaction-field-cursor-struct-mg2675' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetInteractionStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-gadget-interaction-field-cursor-struct-mg2675' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetInteractionStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-gadget-interaction-field-cursor-struct-mg2675' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetInteractionStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-gauge-inforate-field-cursor-struct-mg2676' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoRateStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-gauge-inforate-field-cursor-struct-mg2676' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoRateStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-gauge-inforate-field-cursor-struct-mg2676' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoRateStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-gauge-infotext-field-cursor-struct-mg2677' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoTextStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-gauge-infotext-field-cursor-struct-mg2677' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoTextStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-gauge-infotext-field-cursor-struct-mg2677' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoTextStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-gauge-field-cursor-struct-mg2678' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-gauge-field-cursor-struct-mg2678' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-gauge-field-cursor-struct-mg2678' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-layout-hook-field-cursor-struct-mg2679' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutHookStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-group-layout-hook-field-cursor-struct-mg2679' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutHookStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-layout-hook-field-cursor-struct-mg2679' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutHookStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-layout-policy-field-cursor-struct-mg2680' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutPolicyStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-group-layout-policy-field-cursor-struct-mg2680' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutPolicyStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-layout-policy-field-cursor-struct-mg2680' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutPolicyStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-grid-field-cursor-struct-mg2681' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-group-grid-field-cursor-struct-mg2681' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-grid-field-cursor-struct-mg2681' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-page-field-cursor-struct-mg2682' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupPageStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-group-page-field-cursor-struct-mg2682' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupPageStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-page-field-cursor-struct-mg2682' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupPageStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-image-render-field-cursor-struct-mg2683' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageRenderStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-image-render-field-cursor-struct-mg2683' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageRenderStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-image-render-field-cursor-struct-mg2683' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageRenderStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-image-oldimage-field-cursor-struct-mg2684' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageOldImageStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-image-oldimage-field-cursor-struct-mg2684' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageOldImageStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-image-oldimage-field-cursor-struct-mg2684' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageOldImageStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-image-fontmatchstring-field-cursor-struct-mg2685' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStringStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-image-fontmatchstring-field-cursor-struct-mg2685' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStringStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-image-fontmatchstring-field-cursor-struct-mg2685' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStringStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-image-fontmatch-field-cursor-struct-mg2686' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-image-fontmatch-field-cursor-struct-mg2686' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-image-fontmatch-field-cursor-struct-mg2686' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-image-spec-field-cursor-struct-mg2687' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageSpecStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-image-spec-field-cursor-struct-mg2687' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageSpecStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-image-spec-field-cursor-struct-mg2687' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageSpecStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-levelmeter-label-field-cursor-struct-mg2688' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterLabelStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-levelmeter-label-field-cursor-struct-mg2688' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterLabelStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-levelmeter-label-field-cursor-struct-mg2688' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterLabelStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-levelmeter-presentation-field-cursor-struct-mg2689' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterPresentationStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-levelmeter-presentation-field-cursor-struct-mg2689' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterPresentationStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-levelmeter-presentation-field-cursor-struct-mg2689' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterPresentationStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-numeric-format-field-cursor-struct-mg2690' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NumericFormatStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-numeric-format-field-cursor-struct-mg2690' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NumericFormatStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-numeric-format-field-cursor-struct-mg2690' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NumericFormatStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-numeric-state-field-cursor-struct-mg2691' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NumericStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-numeric-state-field-cursor-struct-mg2691' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NumericStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-numeric-state-field-cursor-struct-mg2691' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NumericStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-prop-range-field-cursor-struct-mg2692' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropRangeStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-prop-range-field-cursor-struct-mg2692' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropRangeStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-prop-range-field-cursor-struct-mg2692' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropRangeStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-prop-policy-field-cursor-struct-mg2693' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropPolicyStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-prop-policy-field-cursor-struct-mg2693' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropPolicyStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-prop-policy-field-cursor-struct-mg2693' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropPolicyStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-scrollbar-layout-field-cursor-struct-mg2694' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollbarLayoutStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-scrollbar-layout-field-cursor-struct-mg2694' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollbarLayoutStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-scrollbar-layout-field-cursor-struct-mg2694' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollbarLayoutStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-slider-presentation-field-cursor-struct-mg2695' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SliderPresentationStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-slider-presentation-field-cursor-struct-mg2695' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SliderPresentationStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-slider-presentation-field-cursor-struct-mg2695' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SliderPresentationStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-rectangle-bartitle-field-cursor-struct-mg2696' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectangleBarTitleStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-rectangle-bartitle-field-cursor-struct-mg2696' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectangleBarTitleStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-rectangle-bartitle-field-cursor-struct-mg2696' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectangleBarTitleStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-rectangle-presentation-field-cursor-struct-mg2697' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectanglePresentationStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-rectangle-presentation-field-cursor-struct-mg2697' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectanglePresentationStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-rectangle-presentation-field-cursor-struct-mg2697' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectanglePresentationStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-register-policy-field-cursor-struct-mg2698' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RegisterPolicyStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-register-policy-field-cursor-struct-mg2698' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RegisterPolicyStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-register-policy-field-cursor-struct-mg2698' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RegisterPolicyStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-requester-service-state-field-cursor-struct-mg2699' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterServiceStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-requester-service-state-field-cursor-struct-mg2699' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterServiceStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-requester-service-state-field-cursor-struct-mg2699' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterServiceStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-selectgroup-active-field-cursor-struct-mg2700' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SelectgroupActiveStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-selectgroup-active-field-cursor-struct-mg2700' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SelectgroupActiveStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-selectgroup-active-field-cursor-struct-mg2700' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SelectgroupActiveStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-scrollgroup-viewport-state-field-cursor-struct-mg2701' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupViewportStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-scrollgroup-viewport-state-field-cursor-struct-mg2701' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupViewportStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-scrollgroup-viewport-state-field-cursor-struct-mg2701' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupViewportStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-scrollgroup-border-scroller-state-field-cursor-struct-mg2702' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupBorderScrollerStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-scrollgroup-border-scroller-state-field-cursor-struct-mg2702' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupBorderScrollerStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-scrollgroup-border-scroller-state-field-cursor-struct-mg2702' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupBorderScrollerStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-scrollgroup-policy-state-field-cursor-struct-mg2703' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupPolicyStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-scrollgroup-policy-state-field-cursor-struct-mg2703' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupPolicyStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-scrollgroup-policy-state-field-cursor-struct-mg2703' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupPolicyStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-scale-presentation-field-cursor-struct-mg2704' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScalePresentationStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-scale-presentation-field-cursor-struct-mg2704' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScalePresentationStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-scale-presentation-field-cursor-struct-mg2704' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScalePresentationStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-sleep-state-field-cursor-struct-mg2705' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SleepStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-sleep-state-field-cursor-struct-mg2705' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SleepStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-sleep-state-field-cursor-struct-mg2705' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SleepStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-acknowledge-state-field-cursor-struct-mg2706' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAcknowledgeStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-acknowledge-state-field-cursor-struct-mg2706' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAcknowledgeStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-acknowledge-state-field-cursor-struct-mg2706' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAcknowledgeStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-attached-list-state-field-cursor-struct-mg2707' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAttachedListStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-attached-list-state-field-cursor-struct-mg2707' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAttachedListStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-attached-list-state-field-cursor-struct-mg2707' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAttachedListStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-contents-state-field-cursor-struct-mg2708' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringContentsStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-contents-state-field-cursor-struct-mg2708' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringContentsStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-contents-state-field-cursor-struct-mg2708' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringContentsStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-cursor-state-field-cursor-struct-mg2709' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringCursorStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-cursor-state-field-cursor-struct-mg2709' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringCursorStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-cursor-state-field-cursor-struct-mg2709' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringCursorStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-filter-state-field-cursor-struct-mg2710' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringFilterStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-filter-state-field-cursor-struct-mg2710' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringFilterStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-filter-state-field-cursor-struct-mg2710' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringFilterStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-integer-state-field-cursor-struct-mg2711' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringIntegerStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-integer-state-field-cursor-struct-mg2711' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringIntegerStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-integer-state-field-cursor-struct-mg2711' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringIntegerStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-placeholder-state-field-cursor-struct-mg2712' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPlaceholderStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-placeholder-state-field-cursor-struct-mg2712' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPlaceholderStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-placeholder-state-field-cursor-struct-mg2712' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPlaceholderStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-spell-checking-state-field-cursor-struct-mg2713' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringSpellCheckingStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-spell-checking-state-field-cursor-struct-mg2713' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringSpellCheckingStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-spell-checking-state-field-cursor-struct-mg2713' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringSpellCheckingStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-presentation-state-field-cursor-struct-mg2714' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPresentationStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-presentation-state-field-cursor-struct-mg2714' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPresentationStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-presentation-state-field-cursor-struct-mg2714' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPresentationStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-scroll-metrics-state-field-cursor-struct-mg2715' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringScrollMetricsStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-scroll-metrics-state-field-cursor-struct-mg2715' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringScrollMetricsStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-scroll-metrics-state-field-cursor-struct-mg2715' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringScrollMetricsStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-edit-hook-state-field-cursor-struct-mg2716' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditHookStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-edit-hook-state-field-cursor-struct-mg2716' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditHookStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-edit-hook-state-field-cursor-struct-mg2716' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditHookStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-interaction-state-field-cursor-struct-mg2717' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteractionStateFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-interaction-state-field-cursor-struct-mg2717' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteractionStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-interaction-state-field-cursor-struct-mg2717' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteractionStateFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-pointer-state-field-cursor-struct-mg2718' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollPointerStateFieldCursorStructRecordCodecRootMg2718'
    Qualify-ClosureVariant -Name 'mui-stringscroll-pointer-state-field-cursor-struct-mg2718' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollPointerStateFieldCursorStructRecordCodecRootMg2718' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-pointer-state-field-cursor-struct-mg2718' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollPointerStateFieldCursorStructRecordCodecRootMg2718' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-state-field-cursor-struct-mg2719' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollStateFieldCursorStructRecordCodecRootMg2719'
    Qualify-ClosureVariant -Name 'mui-stringscroll-state-field-cursor-struct-mg2719' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollStateFieldCursorStructRecordCodecRootMg2719' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-state-field-cursor-struct-mg2719' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollStateFieldCursorStructRecordCodecRootMg2719' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-policy-field-cursor-struct-mg2720' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollPolicyFieldCursorStructRecordCodecRootMg2720'
    Qualify-ClosureVariant -Name 'mui-stringscroll-policy-field-cursor-struct-mg2720' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollPolicyFieldCursorStructRecordCodecRootMg2720' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-policy-field-cursor-struct-mg2720' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollPolicyFieldCursorStructRecordCodecRootMg2720' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-scrollbar-field-cursor-struct-mg2721' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollScrollbarFieldCursorStructRecordCodecRootMg2721'
    Qualify-ClosureVariant -Name 'mui-stringscroll-scrollbar-field-cursor-struct-mg2721' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollScrollbarFieldCursorStructRecordCodecRootMg2721' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-scrollbar-field-cursor-struct-mg2721' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollScrollbarFieldCursorStructRecordCodecRootMg2721' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-composition-field-cursor-struct-mg2722' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollCompositionFieldCursorStructRecordCodecRootMg2722'
    Qualify-ClosureVariant -Name 'mui-stringscroll-composition-field-cursor-struct-mg2722' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollCompositionFieldCursorStructRecordCodecRootMg2722' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-composition-field-cursor-struct-mg2722' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollCompositionFieldCursorStructRecordCodecRootMg2722' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-layout-field-cursor-struct-mg2723' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollLayoutStateFieldCursorStructRecordCodecRootMg2723'
    Qualify-ClosureVariant -Name 'mui-stringscroll-layout-field-cursor-struct-mg2723' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollLayoutStateFieldCursorStructRecordCodecRootMg2723' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-layout-field-cursor-struct-mg2723' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollLayoutStateFieldCursorStructRecordCodecRootMg2723' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-render-field-cursor-struct-mg2724' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollRenderStateFieldCursorStructRecordCodecRootMg2724'
    Qualify-ClosureVariant -Name 'mui-stringscroll-render-field-cursor-struct-mg2724' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollRenderStateFieldCursorStructRecordCodecRootMg2724' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-render-field-cursor-struct-mg2724' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollRenderStateFieldCursorStructRecordCodecRootMg2724' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-viewport-field-cursor-struct-mg2725' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollViewportStateFieldCursorStructRecordCodecRootMg2725'
    Qualify-ClosureVariant -Name 'mui-stringscroll-viewport-field-cursor-struct-mg2725' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollViewportStateFieldCursorStructRecordCodecRootMg2725' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-viewport-field-cursor-struct-mg2725' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollViewportStateFieldCursorStructRecordCodecRootMg2725' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-contents-state-field-cursor-struct-mg2726' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextContentsStateFieldCursorStructRecordCodecRootMg2726'
    Qualify-ClosureVariant -Name 'mui-text-contents-state-field-cursor-struct-mg2726' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextContentsStateFieldCursorStructRecordCodecRootMg2726' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-contents-state-field-cursor-struct-mg2726' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextContentsStateFieldCursorStructRecordCodecRootMg2726' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-preparse-state-field-cursor-struct-mg2727' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPreParseStateFieldCursorStructRecordCodecRootMg2727'
    Qualify-ClosureVariant -Name 'mui-text-preparse-state-field-cursor-struct-mg2727' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPreParseStateFieldCursorStructRecordCodecRootMg2727' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-preparse-state-field-cursor-struct-mg2727' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPreParseStateFieldCursorStructRecordCodecRootMg2727' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-unicode-state-field-cursor-struct-mg2728' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextUnicodeStateFieldCursorStructRecordCodecRootMg2728'
    Qualify-ClosureVariant -Name 'mui-text-unicode-state-field-cursor-struct-mg2728' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextUnicodeStateFieldCursorStructRecordCodecRootMg2728' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-unicode-state-field-cursor-struct-mg2728' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextUnicodeStateFieldCursorStructRecordCodecRootMg2728' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-copy-state-field-cursor-struct-mg2729' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextCopyStateFieldCursorStructRecordCodecRootMg2729'
    Qualify-ClosureVariant -Name 'mui-text-copy-state-field-cursor-struct-mg2729' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextCopyStateFieldCursorStructRecordCodecRootMg2729' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-copy-state-field-cursor-struct-mg2729' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextCopyStateFieldCursorStructRecordCodecRootMg2729' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-activation-state-field-cursor-struct-mg2730' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationStateFieldCursorStructRecordCodecRootMg2730'
    Qualify-ClosureVariant -Name 'mui-area-activation-state-field-cursor-struct-mg2730' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationStateFieldCursorStructRecordCodecRootMg2730' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-activation-state-field-cursor-struct-mg2730' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationStateFieldCursorStructRecordCodecRootMg2730' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-control-char-state-field-cursor-struct-mg2731' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaControlCharStateFieldCursorStructRecordCodecRootMg2731'
    Qualify-ClosureVariant -Name 'mui-area-control-char-state-field-cursor-struct-mg2731' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaControlCharStateFieldCursorStructRecordCodecRootMg2731' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-control-char-state-field-cursor-struct-mg2731' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaControlCharStateFieldCursorStructRecordCodecRootMg2731' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-double-buffer-state-field-cursor-struct-mg2732' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleBufferStateFieldCursorStructRecordCodecRootMg2732'
    Qualify-ClosureVariant -Name 'mui-area-double-buffer-state-field-cursor-struct-mg2732' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleBufferStateFieldCursorStructRecordCodecRootMg2732' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-double-buffer-state-field-cursor-struct-mg2732' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleBufferStateFieldCursorStructRecordCodecRootMg2732' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-disappear-policy-state-field-cursor-struct-mg2733' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDisappearPolicyStateFieldCursorStructRecordCodecRootMg2733'
    Qualify-ClosureVariant -Name 'mui-area-disappear-policy-state-field-cursor-struct-mg2733' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDisappearPolicyStateFieldCursorStructRecordCodecRootMg2733' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-disappear-policy-state-field-cursor-struct-mg2733' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDisappearPolicyStateFieldCursorStructRecordCodecRootMg2733' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-cycle-chain-state-field-cursor-struct-mg2734' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCycleChainStateFieldCursorStructRecordCodecRootMg2734'
    Qualify-ClosureVariant -Name 'mui-area-cycle-chain-state-field-cursor-struct-mg2734' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCycleChainStateFieldCursorStructRecordCodecRootMg2734' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-cycle-chain-state-field-cursor-struct-mg2734' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCycleChainStateFieldCursorStructRecordCodecRootMg2734' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-double-click-state-field-cursor-struct-mg2735' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleClickStateFieldCursorStructRecordCodecRootMg2735'
    Qualify-ClosureVariant -Name 'mui-area-double-click-state-field-cursor-struct-mg2735' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleClickStateFieldCursorStructRecordCodecRootMg2735' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-double-click-state-field-cursor-struct-mg2735' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleClickStateFieldCursorStructRecordCodecRootMg2735' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-builtin-font-state-field-cursor-struct-mg2736' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBuiltinFontStateFieldCursorStructRecordCodecRootMg2736'
    Qualify-ClosureVariant -Name 'mui-area-builtin-font-state-field-cursor-struct-mg2736' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBuiltinFontStateFieldCursorStructRecordCodecRootMg2736' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-builtin-font-state-field-cursor-struct-mg2736' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBuiltinFontStateFieldCursorStructRecordCodecRootMg2736' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-context-menu-state-field-cursor-struct-mg2737' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuStateFieldCursorStructRecordCodecRootMg2737'
    Qualify-ClosureVariant -Name 'mui-area-context-menu-state-field-cursor-struct-mg2737' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuStateFieldCursorStructRecordCodecRootMg2737' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-context-menu-state-field-cursor-struct-mg2737' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuStateFieldCursorStructRecordCodecRootMg2737' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-custom-font-state-field-cursor-struct-mg2738' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontStateFieldCursorStructRecordCodecRootMg2738'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-state-field-cursor-struct-mg2738' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontStateFieldCursorStructRecordCodecRootMg2738' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-state-field-cursor-struct-mg2738' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontStateFieldCursorStructRecordCodecRootMg2738' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-custom-font-runtime-state-field-cursor-struct-mg2739' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontRuntimeStateFieldCursorStructRecordCodecRootMg2739'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-runtime-state-field-cursor-struct-mg2739' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontRuntimeStateFieldCursorStructRecordCodecRootMg2739' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-runtime-state-field-cursor-struct-mg2739' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontRuntimeStateFieldCursorStructRecordCodecRootMg2739' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-drag-policy-state-field-cursor-struct-mg2740' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragPolicyStateFieldCursorStructRecordCodecRootMg2740'
    Qualify-ClosureVariant -Name 'mui-area-drag-policy-state-field-cursor-struct-mg2740' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragPolicyStateFieldCursorStructRecordCodecRootMg2740' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-drag-policy-state-field-cursor-struct-mg2740' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragPolicyStateFieldCursorStructRecordCodecRootMg2740' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-drag-state-field-cursor-struct-mg2741' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragStateFieldCursorStructRecordCodecRootMg2741'
    Qualify-ClosureVariant -Name 'mui-area-drag-state-field-cursor-struct-mg2741' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragStateFieldCursorStructRecordCodecRootMg2741' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-drag-state-field-cursor-struct-mg2741' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragStateFieldCursorStructRecordCodecRootMg2741' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-geometry-state-field-cursor-struct-mg2742' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaGeometryStateFieldCursorStructRecordCodecRootMg2742'
    Qualify-ClosureVariant -Name 'mui-area-geometry-state-field-cursor-struct-mg2742' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaGeometryStateFieldCursorStructRecordCodecRootMg2742' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-geometry-state-field-cursor-struct-mg2742' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaGeometryStateFieldCursorStructRecordCodecRootMg2742' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-layout-policy-state-field-cursor-struct-mg2743' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaLayoutPolicyStateFieldCursorStructRecordCodecRootMg2743'
    Qualify-ClosureVariant -Name 'mui-area-layout-policy-state-field-cursor-struct-mg2743' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaLayoutPolicyStateFieldCursorStructRecordCodecRootMg2743' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-layout-policy-state-field-cursor-struct-mg2743' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaLayoutPolicyStateFieldCursorStructRecordCodecRootMg2743' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-presentation-state-field-cursor-struct-mg2744' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaPresentationStateFieldCursorStructRecordCodecRootMg2744'
    Qualify-ClosureVariant -Name 'mui-area-presentation-state-field-cursor-struct-mg2744' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaPresentationStateFieldCursorStructRecordCodecRootMg2744' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-presentation-state-field-cursor-struct-mg2744' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaPresentationStateFieldCursorStructRecordCodecRootMg2744' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-render-policy-state-field-cursor-struct-mg2745' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaRenderPolicyStateFieldCursorStructRecordCodecRootMg2745'
    Qualify-ClosureVariant -Name 'mui-area-render-policy-state-field-cursor-struct-mg2745' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaRenderPolicyStateFieldCursorStructRecordCodecRootMg2745' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-render-policy-state-field-cursor-struct-mg2745' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaRenderPolicyStateFieldCursorStructRecordCodecRootMg2745' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-fixed-text-state-field-cursor-struct-mg2746' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFixedTextStateFieldCursorStructRecordCodecRootMg2746'
    Qualify-ClosureVariant -Name 'mui-area-fixed-text-state-field-cursor-struct-mg2746' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFixedTextStateFieldCursorStructRecordCodecRootMg2746' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-fixed-text-state-field-cursor-struct-mg2746' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFixedTextStateFieldCursorStructRecordCodecRootMg2746' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-floating-state-field-cursor-struct-mg2747' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFloatingStateFieldCursorStructRecordCodecRootMg2747'
    Qualify-ClosureVariant -Name 'mui-area-floating-state-field-cursor-struct-mg2747' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFloatingStateFieldCursorStructRecordCodecRootMg2747' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-floating-state-field-cursor-struct-mg2747' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFloatingStateFieldCursorStructRecordCodecRootMg2747' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-font-selection-state-field-cursor-struct-mg2748' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontSelectionStateFieldCursorStructRecordCodecRootMg2748'
    Qualify-ClosureVariant -Name 'mui-area-font-selection-state-field-cursor-struct-mg2748' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontSelectionStateFieldCursorStructRecordCodecRootMg2748' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-font-selection-state-field-cursor-struct-mg2748' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontSelectionStateFieldCursorStructRecordCodecRootMg2748' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-text-color-state-field-cursor-struct-mg2749' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTextColorStateFieldCursorStructRecordCodecRootMg2749'
    Qualify-ClosureVariant -Name 'mui-area-text-color-state-field-cursor-struct-mg2749' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTextColorStateFieldCursorStructRecordCodecRootMg2749' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-text-color-state-field-cursor-struct-mg2749' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTextColorStateFieldCursorStructRecordCodecRootMg2749' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-timer-state-field-cursor-struct-mg2750' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerStateFieldCursorStructRecordCodecRootMg2750'
    Qualify-ClosureVariant -Name 'mui-area-timer-state-field-cursor-struct-mg2750' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerStateFieldCursorStructRecordCodecRootMg2750' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-timer-state-field-cursor-struct-mg2750' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerStateFieldCursorStructRecordCodecRootMg2750' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-timer-event-state-field-cursor-struct-mg2751' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerEventStateFieldCursorStructRecordCodecRootMg2751'
    Qualify-ClosureVariant -Name 'mui-area-timer-event-state-field-cursor-struct-mg2751' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerEventStateFieldCursorStructRecordCodecRootMg2751' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-timer-event-state-field-cursor-struct-mg2751' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerEventStateFieldCursorStructRecordCodecRootMg2751' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-weight-state-field-cursor-struct-mg2752' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaWeightStateFieldCursorStructRecordCodecRootMg2752'
    Qualify-ClosureVariant -Name 'mui-area-weight-state-field-cursor-struct-mg2752' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaWeightStateFieldCursorStructRecordCodecRootMg2752' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-weight-state-field-cursor-struct-mg2752' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaWeightStateFieldCursorStructRecordCodecRootMg2752' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-short-help-state-field-cursor-struct-mg2753' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpStateFieldCursorStructRecordCodecRootMg2753'
    Qualify-ClosureVariant -Name 'mui-area-short-help-state-field-cursor-struct-mg2753' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpStateFieldCursorStructRecordCodecRootMg2753' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-short-help-state-field-cursor-struct-mg2753' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpStateFieldCursorStructRecordCodecRootMg2753' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-activation-message-field-cursor-struct-mg2754' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationMessageFieldCursorStructRecordCodecRootMg2754'
    Qualify-ClosureVariant -Name 'mui-area-activation-message-field-cursor-struct-mg2754' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationMessageFieldCursorStructRecordCodecRootMg2754' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-activation-message-field-cursor-struct-mg2754' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationMessageFieldCursorStructRecordCodecRootMg2754' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-bubble-message-field-cursor-struct-mg2755' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBubbleMessageFieldCursorStructRecordCodecRootMg2755'
    Qualify-ClosureVariant -Name 'mui-area-bubble-message-field-cursor-struct-mg2755' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBubbleMessageFieldCursorStructRecordCodecRootMg2755' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-bubble-message-field-cursor-struct-mg2755' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBubbleMessageFieldCursorStructRecordCodecRootMg2755' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-custom-font-message-field-cursor-struct-mg2756' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontMessageFieldCursorStructRecordCodecRootMg2756'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-message-field-cursor-struct-mg2756' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontMessageFieldCursorStructRecordCodecRootMg2756' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-message-field-cursor-struct-mg2756' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontMessageFieldCursorStructRecordCodecRootMg2756' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-short-help-message-field-cursor-struct-mg2757' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpMessageFieldCursorStructRecordCodecRootMg2757'
    Qualify-ClosureVariant -Name 'mui-area-short-help-message-field-cursor-struct-mg2757' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpMessageFieldCursorStructRecordCodecRootMg2757' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-short-help-message-field-cursor-struct-mg2757' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpMessageFieldCursorStructRecordCodecRootMg2757' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-context-menu-message-field-cursor-struct-mg2758' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuMessageFieldCursorStructRecordCodecRootMg2758'
    Qualify-ClosureVariant -Name 'mui-area-context-menu-message-field-cursor-struct-mg2758' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuMessageFieldCursorStructRecordCodecRootMg2758' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-context-menu-message-field-cursor-struct-mg2758' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuMessageFieldCursorStructRecordCodecRootMg2758' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-resize-message-field-cursor-struct-mg2759' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaResizeMessageFieldCursorStructRecordCodecRootMg2759'
    Qualify-ClosureVariant -Name 'mui-area-resize-message-field-cursor-struct-mg2759' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaResizeMessageFieldCursorStructRecordCodecRootMg2759' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-resize-message-field-cursor-struct-mg2759' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaResizeMessageFieldCursorStructRecordCodecRootMg2759' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-minmax-field-cursor-struct-mg2760' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MinMaxFieldCursorStructRecordCodecRootMg2760'
    Qualify-ClosureVariant -Name 'mui-minmax-field-cursor-struct-mg2760' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MinMaxFieldCursorStructRecordCodecRootMg2760' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-minmax-field-cursor-struct-mg2760' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MinMaxFieldCursorStructRecordCodecRootMg2760' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-resize-state-field-cursor-struct-mg2761' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaResizeStateFieldCursorStructRecordCodecRootMg2761'
    Qualify-ClosureVariant -Name 'mui-area-resize-state-field-cursor-struct-mg2761' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaResizeStateFieldCursorStructRecordCodecRootMg2761' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-resize-state-field-cursor-struct-mg2761' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaResizeStateFieldCursorStructRecordCodecRootMg2761' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-handled-events-state-field-cursor-struct-mg2762' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaHandledEventsStateFieldCursorStructRecordCodecRootMg2762'
    Qualify-ClosureVariant -Name 'mui-area-handled-events-state-field-cursor-struct-mg2762' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaHandledEventsStateFieldCursorStructRecordCodecRootMg2762' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-handled-events-state-field-cursor-struct-mg2762' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaHandledEventsStateFieldCursorStructRecordCodecRootMg2762' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-drag-message-field-cursor-struct-mg2763' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragMessageFieldCursorStructRecordCodecRootMg2763'
    Qualify-ClosureVariant -Name 'mui-area-drag-message-field-cursor-struct-mg2763' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragMessageFieldCursorStructRecordCodecRootMg2763' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-drag-message-field-cursor-struct-mg2763' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragMessageFieldCursorStructRecordCodecRootMg2763' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-menuitem-field-cursor-struct-mg2764' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuItemFieldCursorStructRecordCodecRootMg2764'
    Qualify-ClosureVariant -Name 'mui-menuitem-field-cursor-struct-mg2764' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuItemFieldCursorStructRecordCodecRootMg2764' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-menuitem-field-cursor-struct-mg2764' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuItemFieldCursorStructRecordCodecRootMg2764' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-intuitext-field-cursor-struct-mg2765' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::IntuiTextFieldCursorStructRecordCodecRootMg2765'
    Qualify-ClosureVariant -Name 'mui-intuitext-field-cursor-struct-mg2765' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::IntuiTextFieldCursorStructRecordCodecRootMg2765' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-intuitext-field-cursor-struct-mg2765' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::IntuiTextFieldCursorStructRecordCodecRootMg2765' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-layout-message-field-cursor-struct-mg2766' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutMessageFieldCursorStructRecordCodecRootMg2766'
    Qualify-ClosureVariant -Name 'mui-layout-message-field-cursor-struct-mg2766' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutMessageFieldCursorStructRecordCodecRootMg2766' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-layout-message-field-cursor-struct-mg2766' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutMessageFieldCursorStructRecordCodecRootMg2766' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-family-mutation-list-field-cursor-struct-mg2767' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyMutationListFieldCursorStructRecordCodecRootMg2767'
    Qualify-ClosureVariant -Name 'mui-family-mutation-list-field-cursor-struct-mg2767' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyMutationListFieldCursorStructRecordCodecRootMg2767' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-family-mutation-list-field-cursor-struct-mg2767' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyMutationListFieldCursorStructRecordCodecRootMg2767' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-image-geometry-field-cursor-struct-mg2768' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageGeometryFieldCursorStructRecordCodecRootMg2768'
    Qualify-ClosureVariant -Name 'mui-image-geometry-field-cursor-struct-mg2768' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageGeometryFieldCursorStructRecordCodecRootMg2768' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-image-geometry-field-cursor-struct-mg2768' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageGeometryFieldCursorStructRecordCodecRootMg2768' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-master-private-root-field-cursor-struct-mg2769' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MasterPrivateRootFieldCursorStructRecordCodecRootMg2769'
    Qualify-ClosureVariant -Name 'mui-master-private-root-field-cursor-struct-mg2769' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MasterPrivateRootFieldCursorStructRecordCodecRootMg2769' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-master-private-root-field-cursor-struct-mg2769' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MasterPrivateRootFieldCursorStructRecordCodecRootMg2769' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-update-config-header-field-cursor-struct-mg2770' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UpdateConfigHeaderFieldCursorStructRecordCodecRootMg2770'
    Qualify-ClosureVariant -Name 'mui-update-config-header-field-cursor-struct-mg2770' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UpdateConfigHeaderFieldCursorStructRecordCodecRootMg2770' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-update-config-header-field-cursor-struct-mg2770' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UpdateConfigHeaderFieldCursorStructRecordCodecRootMg2770' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-edit-work-field-cursor-struct-mg2771' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditWorkFieldCursorStructRecordCodecRootMg2771'
    Qualify-ClosureVariant -Name 'mui-string-edit-work-field-cursor-struct-mg2771' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditWorkFieldCursorStructRecordCodecRootMg2771' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-edit-work-field-cursor-struct-mg2771' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditWorkFieldCursorStructRecordCodecRootMg2771' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-edit-command-struct-cursor-mg2772' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditCommandStructCursorRootMg2772'
    Qualify-ClosureVariant -Name 'mui-string-edit-command-struct-cursor-mg2772' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditCommandStructCursorRootMg2772' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-edit-command-struct-cursor-mg2772' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditCommandStructCursorRootMg2772' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-menu-specialist-field-cursor-struct-mg2773' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuSpecialistFieldCursorStructRecordCodecRootMg2773'
    Qualify-ClosureVariant -Name 'mui-menu-specialist-field-cursor-struct-mg2773' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuSpecialistFieldCursorStructRecordCodecRootMg2773' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-menu-specialist-field-cursor-struct-mg2773' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuSpecialistFieldCursorStructRecordCodecRootMg2773' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-pop-specialist-field-cursor-struct-mg2774' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopSpecialistFieldCursorStructRecordCodecRootMg2774'
    Qualify-ClosureVariant -Name 'mui-pop-specialist-field-cursor-struct-mg2774' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopSpecialistFieldCursorStructRecordCodecRootMg2774' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-pop-specialist-field-cursor-struct-mg2774' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopSpecialistFieldCursorStructRecordCodecRootMg2774' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-pop-packet-field-cursor-struct-mg2789' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopPacketFieldCursorStructRecordCodecRootMg2789'
    Qualify-ClosureVariant -Name 'mui-pop-packet-field-cursor-struct-mg2789' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopPacketFieldCursorStructRecordCodecRootMg2789' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-pop-packet-field-cursor-struct-mg2789' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopPacketFieldCursorStructRecordCodecRootMg2789' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-process-packet-field-cursor-struct-mg2790' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistPacketFieldCursorStructRecordCodecRootMg2790'
    Qualify-ClosureVariant -Name 'mui-process-packet-field-cursor-struct-mg2790' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistPacketFieldCursorStructRecordCodecRootMg2790' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-process-packet-field-cursor-struct-mg2790' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistPacketFieldCursorStructRecordCodecRootMg2790' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-process-record-field-cursor-struct-mg2797' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistRecordFieldCursorStructRecordCodecRootMg2797'
    Qualify-ClosureVariant -Name 'mui-process-record-field-cursor-struct-mg2797' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistRecordFieldCursorStructRecordCodecRootMg2797' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-process-record-field-cursor-struct-mg2797' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistRecordFieldCursorStructRecordCodecRootMg2797' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-message-field-cursor-struct-mg2798' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeMessageFieldCursorStructRecordCodecRootMg2798'
    Qualify-ClosureVariant -Name 'mui-listtree-message-field-cursor-struct-mg2798' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeMessageFieldCursorStructRecordCodecRootMg2798' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-message-field-cursor-struct-mg2798' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeMessageFieldCursorStructRecordCodecRootMg2798' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-family-inline-vector-kind-struct-mg2799' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyInlineVectorKindStructRecordCodecRootMg2799'
    Qualify-ClosureVariant -Name 'mui-family-inline-vector-kind-struct-mg2799' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyInlineVectorKindStructRecordCodecRootMg2799' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-family-inline-vector-kind-struct-mg2799' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyInlineVectorKindStructRecordCodecRootMg2799' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-state-field-cursor-struct-mg2800' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListStateFieldCursorStructRecordCodecRootMg2800'
    Qualify-ClosureVariant -Name 'mui-list-state-field-cursor-struct-mg2800' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListStateFieldCursorStructRecordCodecRootMg2800' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-state-field-cursor-struct-mg2800' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListStateFieldCursorStructRecordCodecRootMg2800' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-notify-packet-field-cursor-struct-mg2791' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyPacketFieldCursorStructRecordCodecRootMg2791'
    Qualify-ClosureVariant -Name 'mui-notify-packet-field-cursor-struct-mg2791' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyPacketFieldCursorStructRecordCodecRootMg2791' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-notify-packet-field-cursor-struct-mg2791' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyPacketFieldCursorStructRecordCodecRootMg2791' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-misc-record-field-cursor-struct-mg2792' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscRecordFieldCursorStructRecordCodecRootMg2792'
    Qualify-ClosureVariant -Name 'mui-misc-record-field-cursor-struct-mg2792' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscRecordFieldCursorStructRecordCodecRootMg2792' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-misc-record-field-cursor-struct-mg2792' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscRecordFieldCursorStructRecordCodecRootMg2792' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dirlist-record-field-cursor-struct-mg2793' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistRecordFieldCursorStructRecordCodecRootMg2793'
    Qualify-ClosureVariant -Name 'mui-dirlist-record-field-cursor-struct-mg2793' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistRecordFieldCursorStructRecordCodecRootMg2793' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dirlist-record-field-cursor-struct-mg2793' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistRecordFieldCursorStructRecordCodecRootMg2793' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-store-packet-field-cursor-struct-mg2794' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StorePacketFieldCursorStructRecordCodecRootMg2794'
    Qualify-ClosureVariant -Name 'mui-store-packet-field-cursor-struct-mg2794' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StorePacketFieldCursorStructRecordCodecRootMg2794' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-store-packet-field-cursor-struct-mg2794' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StorePacketFieldCursorStructRecordCodecRootMg2794' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-family-packet-field-cursor-struct-mg2795' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyPacketFieldCursorStructRecordCodecRootMg2795'
    Qualify-ClosureVariant -Name 'mui-family-packet-field-cursor-struct-mg2795' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyPacketFieldCursorStructRecordCodecRootMg2795' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-family-packet-field-cursor-struct-mg2795' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyPacketFieldCursorStructRecordCodecRootMg2795' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-virtgroup-input-field-cursor-struct-mg2796' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::VirtgroupInputFieldCursorStructRecordCodecRootMg2796'
    Qualify-ClosureVariant -Name 'mui-virtgroup-input-field-cursor-struct-mg2796' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::VirtgroupInputFieldCursorStructRecordCodecRootMg2796' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-virtgroup-input-field-cursor-struct-mg2796' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::VirtgroupInputFieldCursorStructRecordCodecRootMg2796' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-set-as-string-packet-field-cursor-struct-mg2775' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SetAsStringPacketFieldCursorStructRecordCodecRootMg2775'
    Qualify-ClosureVariant -Name 'mui-set-as-string-packet-field-cursor-struct-mg2775' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SetAsStringPacketFieldCursorStructRecordCodecRootMg2775' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-set-as-string-packet-field-cursor-struct-mg2775' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SetAsStringPacketFieldCursorStructRecordCodecRootMg2775' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-get-config-item-packet-field-cursor-struct-mg2776' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GetConfigItemPacketFieldCursorStructRecordCodecRootMg2776'
    Qualify-ClosureVariant -Name 'mui-get-config-item-packet-field-cursor-struct-mg2776' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GetConfigItemPacketFieldCursorStructRecordCodecRootMg2776' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-get-config-item-packet-field-cursor-struct-mg2776' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GetConfigItemPacketFieldCursorStructRecordCodecRootMg2776' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dataspace-packet-field-cursor-struct-mg2777' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspacePacketFieldCursorStructRecordCodecRootMg2777'
    Qualify-ClosureVariant -Name 'mui-dataspace-packet-field-cursor-struct-mg2777' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspacePacketFieldCursorStructRecordCodecRootMg2777' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dataspace-packet-field-cursor-struct-mg2777' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspacePacketFieldCursorStructRecordCodecRootMg2777' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-notify-write-packet-field-cursor-struct-mg2778' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyWritePacketFieldCursorStructRecordCodecRootMg2778'
    Qualify-ClosureVariant -Name 'mui-notify-write-packet-field-cursor-struct-mg2778' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyWritePacketFieldCursorStructRecordCodecRootMg2778' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-notify-write-packet-field-cursor-struct-mg2778' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyWritePacketFieldCursorStructRecordCodecRootMg2778' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-notify-userdata-packet-field-cursor-struct-mg2779' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataPacketFieldCursorStructRecordCodecRootMg2779'
    Qualify-ClosureVariant -Name 'mui-notify-userdata-packet-field-cursor-struct-mg2779' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataPacketFieldCursorStructRecordCodecRootMg2779' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-notify-userdata-packet-field-cursor-struct-mg2779' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataPacketFieldCursorStructRecordCodecRootMg2779' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-menu-specialist-packet-field-cursor-struct-mg2780' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuSpecialistPacketFieldCursorStructRecordCodecRootMg2780'
    Qualify-ClosureVariant -Name 'mui-menu-specialist-packet-field-cursor-struct-mg2780' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuSpecialistPacketFieldCursorStructRecordCodecRootMg2780' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-menu-specialist-packet-field-cursor-struct-mg2780' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuSpecialistPacketFieldCursorStructRecordCodecRootMg2780' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-misc-specialist-packet-field-cursor-struct-mg2781' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscSpecialistPacketFieldCursorStructRecordCodecRootMg2781'
    Qualify-ClosureVariant -Name 'mui-misc-specialist-packet-field-cursor-struct-mg2781' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscSpecialistPacketFieldCursorStructRecordCodecRootMg2781' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-misc-specialist-packet-field-cursor-struct-mg2781' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscSpecialistPacketFieldCursorStructRecordCodecRootMg2781' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-notify-userdata-traversal-frame-field-cursor-struct-mg2782' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataTraversalFrameFieldCursorStructRecordCodecRootMg2782'
    Qualify-ClosureVariant -Name 'mui-notify-userdata-traversal-frame-field-cursor-struct-mg2782' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataTraversalFrameFieldCursorStructRecordCodecRootMg2782' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-notify-userdata-traversal-frame-field-cursor-struct-mg2782' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataTraversalFrameFieldCursorStructRecordCodecRootMg2782' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dirlist-packet-field-cursor-struct-mg2783' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistPacketFieldCursorStructRecordCodecRootMg2783'
    Qualify-ClosureVariant -Name 'mui-dirlist-packet-field-cursor-struct-mg2783' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistPacketFieldCursorStructRecordCodecRootMg2783' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dirlist-packet-field-cursor-struct-mg2783' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistPacketFieldCursorStructRecordCodecRootMg2783' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-integer-state-field-struct-mg2258' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringIntegerStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-integer-state-field-struct-mg2258' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringIntegerStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-integer-state-field-struct-mg2258' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringIntegerStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-menuitem-trigger-field-struct-mg2259' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuItemTriggerFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-menuitem-trigger-field-struct-mg2259' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuItemTriggerFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-menuitem-trigger-field-struct-mg2259' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuItemTriggerFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-intuitext-trigger-field-struct-mg2260' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::IntuiTextTriggerFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-intuitext-trigger-field-struct-mg2260' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::IntuiTextTriggerFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-intuitext-trigger-field-struct-mg2260' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::IntuiTextTriggerFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-specialist-hook-message-field-struct-mg2261' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SpecialistHookMessageFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-specialist-hook-message-field-struct-mg2261' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SpecialistHookMessageFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-specialist-hook-message-field-struct-mg2261' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SpecialistHookMessageFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-store-policy-state-field-struct-mg2262' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StorePolicyStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-store-policy-state-field-struct-mg2262' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StorePolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-store-policy-state-field-struct-mg2262' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StorePolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-appmessage-field-struct-mg2263' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AppMessageFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-appmessage-field-struct-mg2263' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AppMessageFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-appmessage-field-struct-mg2263' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AppMessageFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-workbench-argument-field-struct-mg2264' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WorkbenchArgumentFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-workbench-argument-field-struct-mg2264' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WorkbenchArgumentFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-workbench-argument-field-struct-mg2264' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WorkbenchArgumentFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-persistence-frame-field-struct-mg2265' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPersistenceFrameFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-persistence-frame-field-struct-mg2265' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPersistenceFrameFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-persistence-frame-field-struct-mg2265' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPersistenceFrameFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-settings-field-struct-mg2266' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-settings-field-struct-mg2266' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-settings-field-struct-mg2266' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-queue-packet-field-struct-mg2267' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationQueuePacketFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-queue-packet-field-struct-mg2267' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationQueuePacketFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-queue-packet-field-struct-mg2267' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationQueuePacketFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-input-packet-field-struct-mg2268' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationInputPacketCursorStructRecordRoot'
    Qualify-ClosureVariant -Name 'mui-application-input-packet-field-struct-mg2268' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationInputPacketCursorStructRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-input-packet-field-struct-mg2268' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationInputPacketCursorStructRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-command-field-struct-mg2269' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCommandFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-command-field-struct-mg2269' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCommandFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-command-field-struct-mg2269' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCommandFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-window-list-field-struct-mg2270' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowListFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-window-list-field-struct-mg2270' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowListFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-window-list-field-struct-mg2270' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowListFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-window-node-field-struct-mg2271' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowNodeFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-window-node-field-struct-mg2271' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowNodeFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-window-node-field-struct-mg2271' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowNodeFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-event-handler-node-field-struct-mg2272' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::EventHandlerNodeFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-event-handler-node-field-struct-mg2272' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::EventHandlerNodeFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-event-handler-node-field-struct-mg2272' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::EventHandlerNodeFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-input-handler-field-struct-mg2273' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::InputHandlerFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-input-handler-field-struct-mg2273' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::InputHandlerFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-input-handler-field-struct-mg2273' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::InputHandlerFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-presentation-field-struct-mg2274' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPresentationFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-presentation-field-struct-mg2274' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPresentationFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-presentation-field-struct-mg2274' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPresentationFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-settings-packet-field-struct-mg2275' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPacketFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-settings-packet-field-struct-mg2275' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPacketFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-settings-packet-field-struct-mg2275' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPacketFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-activation-packet-field-struct-mg2276' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationPacketFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-activation-packet-field-struct-mg2276' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationPacketFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-activation-packet-field-struct-mg2276' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationPacketFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-bubble-packet-field-struct-mg2277' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBubblePacketFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-bubble-packet-field-struct-mg2277' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBubblePacketFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-bubble-packet-field-struct-mg2277' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBubblePacketFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-context-menu-packet-field-struct-mg2278' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuPacketFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-context-menu-packet-field-struct-mg2278' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuPacketFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-context-menu-packet-field-struct-mg2278' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuPacketFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-custom-font-packet-field-struct-mg2279' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontPacketFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-packet-field-struct-mg2279' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontPacketFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-packet-field-struct-mg2279' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontPacketFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-resize-packet-field-struct-mg2280' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaResizePacketFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-resize-packet-field-struct-mg2280' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaResizePacketFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-resize-packet-field-struct-mg2280' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaResizePacketFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-drag-packet-field-struct-mg2281' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragPacketFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-drag-packet-field-struct-mg2281' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragPacketFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-drag-packet-field-struct-mg2281' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragPacketFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-resize-state-field-struct-mg2282' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaResizeStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-resize-state-field-struct-mg2282' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaResizeStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-resize-state-field-struct-mg2282' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaResizeStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-drag-state-field-struct-mg2283' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-drag-state-field-struct-mg2283' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-drag-state-field-struct-mg2283' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-short-help-field-struct-mg2284' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-short-help-field-struct-mg2284' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-short-help-field-struct-mg2284' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dirlist-field-struct-mg2285' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-dirlist-field-struct-mg2285' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dirlist-field-struct-mg2285' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-asl-field-struct-mg2286' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslRecordFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-asl-field-struct-mg2286' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslRecordFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-asl-field-struct-mg2286' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslRecordFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-asl-tag-item-field-struct-mg2287' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslTagItemFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-asl-tag-item-field-struct-mg2287' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslTagItemFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-asl-tag-item-field-struct-mg2287' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslTagItemFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-callhook-field-struct-mg2288' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CallHookFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-callhook-field-struct-mg2288' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CallHookFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-callhook-field-struct-mg2288' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CallHookFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-keyadjust-text-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::KeyadjustTextStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-keyadjust-text-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::KeyadjustTextStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-keyadjust-text-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::KeyadjustTextStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-headless-state-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessStatePacketRoot'
    Qualify-ClosureVariant -Name 'mui-headless-state-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessStatePacketRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-headless-state-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessStatePacketRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-class-service-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ClassServiceRecordRoot'
    Qualify-ClosureVariant -Name 'mui-class-service-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ClassServiceRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-class-service-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ClassServiceRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-class-service-field-struct-mg2295' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ClassServiceFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-class-service-field-struct-mg2295' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ClassServiceFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-class-service-field-struct-mg2295' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ClassServiceFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-headless-class-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessClassPacketRoot'
    Qualify-ClosureVariant -Name 'mui-headless-class-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessClassPacketRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-headless-class-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessClassPacketRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-headless-object-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessObjectPacketRoot'
    Qualify-ClosureVariant -Name 'mui-headless-object-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessObjectPacketRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-headless-object-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessObjectPacketRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-attribute-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AttributeRecordRoot'
    Qualify-ClosureVariant -Name 'mui-attribute-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AttributeRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-attribute-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AttributeRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-child-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChildRecordRoot'
    Qualify-ClosureVariant -Name 'mui-child-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChildRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-child-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChildRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-store-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreRecordRoot'
    Qualify-ClosureVariant -Name 'mui-store-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-store-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-notification-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotificationRecordRoot'
    Qualify-ClosureVariant -Name 'mui-notification-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotificationRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-notification-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotificationRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-semaphore-object-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SemaphoreObjectRecordRoot'
    Qualify-ClosureVariant -Name 'mui-semaphore-object-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SemaphoreObjectRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-semaphore-object-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SemaphoreObjectRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-store-object-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreObjectRecordRoot'
    Qualify-ClosureVariant -Name 'mui-store-object-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreObjectRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-store-object-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreObjectRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-headless' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessCoreRoot'
    Qualify-ClosureVariant -Name 'mui-headless' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessCoreRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-headless' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessCoreRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-headless-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessMethodHeaderCodecRoot'
    Qualify-ClosureVariant -Name 'mui-headless-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessMethodHeaderCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-headless-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessMethodHeaderCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-headless-om-record-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessOmRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-headless-om-record-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessOmRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-headless-om-record-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessOmRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-headless-creation-tag-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessCreationTagCodecRoot'
    Qualify-ClosureVariant -Name 'mui-headless-creation-tag-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessCreationTagCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-headless-creation-tag-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessCreationTagCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-object-persistence' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ObjectPersistenceRoot'
    Qualify-ClosureVariant -Name 'mui-object-persistence' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ObjectPersistenceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-object-persistence' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ObjectPersistenceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-object-persistence-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ObjectPersistencePacketsRoot'
    Qualify-ClosureVariant -Name 'mui-object-persistence-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ObjectPersistencePacketsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-object-persistence-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ObjectPersistencePacketsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-object-persistence-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ObjectPersistenceMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-object-persistence-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ObjectPersistenceMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-object-persistence-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ObjectPersistenceMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-object-persistence-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ObjectPersistenceMethodHeaderStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-object-persistence-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ObjectPersistenceMethodHeaderStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-object-persistence-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ObjectPersistenceMethodHeaderStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-persistence-tree' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPersistenceTreeRoot'
    Qualify-NativeRoot -Name 'mui-window-snapshot' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowSnapshotRoot'
    Qualify-ClosureVariant -Name 'mui-window-snapshot' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowSnapshotRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-snapshot' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowSnapshotRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-cycle-chain' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowCycleChainRoot'
    Qualify-ClosureVariant -Name 'mui-window-cycle-chain' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowCycleChainRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-cycle-chain' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowCycleChainRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-active-object' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowActiveObjectRoot'
    Qualify-ClosureVariant -Name 'mui-window-active-object' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowActiveObjectRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-active-object' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowActiveObjectRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-active-object-spatial' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowActiveObjectSpatialRoot'
    Qualify-ClosureVariant -Name 'mui-window-active-object-spatial' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowActiveObjectSpatialRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-active-object-spatial' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowActiveObjectSpatialRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-screen-depth' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowScreenDepthRoot'
    Qualify-ClosureVariant -Name 'mui-window-screen-depth' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowScreenDepthRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-screen-depth' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowScreenDepthRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-get-config-item' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GetConfigItemRoot'
    Qualify-ClosureVariant -Name 'mui-get-config-item' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GetConfigItemRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-get-config-item' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GetConfigItemRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-get-config-item-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GetConfigItemPacketsRoot'
    Qualify-ClosureVariant -Name 'mui-get-config-item-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GetConfigItemPacketsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-get-config-item-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GetConfigItemPacketsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-get-config-item-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GetConfigItemMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-get-config-item-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GetConfigItemMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-get-config-item-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GetConfigItemMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-get-config-item-field-struct-mg2290' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GetConfigItemFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-get-config-item-field-struct-mg2290' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GetConfigItemFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-get-config-item-field-struct-mg2290' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GetConfigItemFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-user-data' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataRoot'
    Qualify-ClosureVariant -Name 'mui-user-data' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-user-data' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-user-data-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-user-data-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-user-data-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-user-data-traversal-frame-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataTraversalFrameVectorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-user-data-traversal-frame-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataTraversalFrameVectorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-user-data-traversal-frame-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataTraversalFrameVectorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-notify-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyPacketRoot'
    Qualify-ClosureVariant -Name 'mui-notify-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyPacketRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-notify-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyPacketRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-notify-packet-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyPacketCodecRoot'
    Qualify-ClosureVariant -Name 'mui-notify-packet-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyPacketCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-notify-packet-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyPacketCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-multi-set' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MultiSetRoot' `
        -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-multi-set' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MultiSetRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-multi-set' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MultiSetRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-lifecycle' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowLifecycleRoot' `
        -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-window-lifecycle' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowLifecycleRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-lifecycle' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowLifecycleRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-input-event' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowInputEventRoot' `
        -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-window-input-event' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowInputEventRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-input-event' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowInputEventRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-polling' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventPollingRoot' `
        -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-window-event-polling' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventPollingRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-polling' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventPollingRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-pointer-polling' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowPointerPollingRoot' `
        -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-window-pointer-polling' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowPointerPollingRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-pointer-polling' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowPointerPollingRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-preprocessed-event-polling' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowPreprocessedEventPollingRoot' `
        -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-window-preprocessed-event-polling' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowPreprocessedEventPollingRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-preprocessed-event-polling' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowPreprocessedEventPollingRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-handler-polling' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerPollingRoot' `
        -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-window-event-handler-polling' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerPollingRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-polling' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerPollingRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-handled-events-registration' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaHandledEventsRegistrationRoot' `
        -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-area-handled-events-registration' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaHandledEventsRegistrationRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-handled-events-registration' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaHandledEventsRegistrationRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-handler-active-group' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerActiveGroupRoot'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-active-group' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerActiveGroupRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-active-group' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerActiveGroupRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-handler-links' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerLinksRoot' `
        -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-window-event-handler-links' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerLinksRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-links' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerLinksRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-multi-set-target-vector-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MultiSetTargetVectorCodecRoot'
    Qualify-ClosureVariant -Name 'mui-multi-set-target-vector-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MultiSetTargetVectorCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-multi-set-target-vector-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MultiSetTargetVectorCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-find-object' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FindObjectRoot'
    Qualify-ClosureVariant -Name 'mui-find-object' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FindObjectRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-find-object' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FindObjectRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-return-id' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationReturnIdRoot'
    Qualify-ClosureVariant -Name 'mui-application-return-id' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationReturnIdRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-return-id' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationReturnIdRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-input' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationInputRoot'
    Qualify-ClosureVariant -Name 'mui-application-input' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationInputRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-input' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationInputRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-input-buffered' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationInputBufferedRoot'
    Qualify-ClosureVariant -Name 'mui-application-input-buffered' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationInputBufferedRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-input-buffered' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationInputBufferedRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-input-handler' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationInputHandlerRoot'
    Qualify-ClosureVariant -Name 'mui-application-input-handler' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationInputHandlerRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-input-handler' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationInputHandlerRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-menu-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuStateRoot'
    Qualify-ClosureVariant -Name 'mui-application-menu-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuStateRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-menu-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuStateRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-menu-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowMenuStateRoot'
    Qualify-ClosureVariant -Name 'mui-window-menu-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowMenuStateRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-menu-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowMenuStateRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-handler' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerRoot'
    Qualify-ClosureVariant -Name 'mui-window-event-handler' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-handler' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-handler-priority' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerPriorityRoot'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-priority' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerPriorityRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-priority' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerPriorityRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-handler-active-parent' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerActiveParentRoot'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-active-parent' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerActiveParentRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-active-parent' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerActiveParentRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-handler-calling' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerCallingRoot'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-calling' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerCallingRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-calling' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerCallingRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-handler-enabled' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerEnabledRoot'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-enabled' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerEnabledRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-enabled' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerEnabledRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-handler-active-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerActiveStateRoot'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-active-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerActiveStateRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-active-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerActiveStateRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-handler-disable-keys' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerDisableKeysRoot'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-disable-keys' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerDisableKeysRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-disable-keys' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerDisableKeysRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-handler-default-object' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerDefaultObjectRoot'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-default-object' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerDefaultObjectRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-default-object' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerDefaultObjectRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-activate' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowActivateRoot'
    Qualify-ClosureVariant -Name 'mui-window-activate' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowActivateRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-activate' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowActivateRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-sleep' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowSleepRoot'
    Qualify-ClosureVariant -Name 'mui-window-sleep' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowSleepRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-sleep' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowSleepRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-layout' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutCoreRoot'
    Qualify-ClosureVariant -Name 'mui-layout' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutCoreRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-layout' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutCoreRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-layout-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutServiceRoot'
    Qualify-ClosureVariant -Name 'mui-layout-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutServiceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-layout-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutServiceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-layout-surface-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutSurfacePacketsRoot'
    Qualify-ClosureVariant -Name 'mui-layout-surface-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutSurfacePacketsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-layout-surface-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutSurfacePacketsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-layout-text-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutTextPacketRoot'
    Qualify-ClosureVariant -Name 'mui-layout-text-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutTextPacketRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-layout-text-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutTextPacketRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-layout-packet-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutPacketCodecRoot'
    Qualify-ClosureVariant -Name 'mui-layout-packet-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutPacketCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-layout-packet-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutPacketCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-layout-field-struct-adapter-mg2350' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-layout-field-struct-adapter-mg2350' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-layout-field-struct-adapter-mg2350' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-layout-packet-field-cursor-struct-mg2786' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutPacketFieldCursorStructRecordCodecRootMg2786'
    Qualify-ClosureVariant -Name 'mui-layout-packet-field-cursor-struct-mg2786' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutPacketFieldCursorStructRecordCodecRootMg2786' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-layout-packet-field-cursor-struct-mg2786' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutPacketFieldCursorStructRecordCodecRootMg2786' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-common-control-field-struct-adapter-mg2351' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-common-control-field-struct-adapter-mg2351' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-common-control-field-struct-adapter-mg2351' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-minmax-field-struct-adapter-mg2352' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MinMaxFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-minmax-field-struct-adapter-mg2352' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MinMaxFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-minmax-field-struct-adapter-mg2352' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MinMaxFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-testpos-field-struct-adapter-mg2353' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTestPosFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-testpos-field-struct-adapter-mg2353' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTestPosFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-testpos-field-struct-adapter-mg2353' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTestPosFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-input-field-struct-adapter-mg2354' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInputFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-input-field-struct-adapter-mg2354' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInputFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-input-field-struct-adapter-mg2354' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInputFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-drag-field-struct-adapter-mg2355' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewDragStateFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-listview-drag-field-struct-adapter-mg2355' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewDragStateFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-drag-field-struct-adapter-mg2355' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewDragStateFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-header-field-struct-adapter-mg2356' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHeaderFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-header-field-struct-adapter-mg2356' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHeaderFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-header-field-struct-adapter-mg2356' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHeaderFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-owner-field-struct-adapter-mg2357' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewOwnerFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-listview-owner-field-struct-adapter-mg2357' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewOwnerFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-owner-field-struct-adapter-mg2357' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewOwnerFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-hscroller-field-struct-adapter-mg2358' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHScrollerFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-listview-hscroller-field-struct-adapter-mg2358' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHScrollerFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-hscroller-field-struct-adapter-mg2358' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHScrollerFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-slot-field-struct-adapter-mg2359' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListSlotFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-slot-field-struct-adapter-mg2359' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListSlotFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-slot-field-struct-adapter-mg2359' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListSlotFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-image-field-struct-adapter-mg2360' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListImageFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-image-field-struct-adapter-mg2360' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListImageFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-image-field-struct-adapter-mg2360' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListImageFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-title-array-field-struct-adapter-mg2361' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTitleArrayFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-title-array-field-struct-adapter-mg2361' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTitleArrayFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-title-array-field-struct-adapter-mg2361' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTitleArrayFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-title-field-struct-adapter-mg2362' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTitleFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-title-field-struct-adapter-mg2362' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTitleFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-title-field-struct-adapter-mg2362' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTitleFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-selection-signal-field-struct-adapter-mg2363' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListSelectionSignalFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-selection-signal-field-struct-adapter-mg2363' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListSelectionSignalFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-selection-signal-field-struct-adapter-mg2363' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListSelectionSignalFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-format-policy-field-struct-adapter-mg2364' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatPolicyFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-format-policy-field-struct-adapter-mg2364' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatPolicyFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-format-policy-field-struct-adapter-mg2364' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatPolicyFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-font-field-struct-adapter-mg2365' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFontFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-font-field-struct-adapter-mg2365' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFontFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-font-field-struct-adapter-mg2365' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFontFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-redraw-field-struct-adapter-mg2366' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListRedrawFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-redraw-field-struct-adapter-mg2366' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListRedrawFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-redraw-field-struct-adapter-mg2366' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListRedrawFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-active-field-struct-adapter-mg2367' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListActiveFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-active-field-struct-adapter-mg2367' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListActiveFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-active-field-struct-adapter-mg2367' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListActiveFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-insert-position-field-struct-adapter-mg2368' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInsertPositionFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-insert-position-field-struct-adapter-mg2368' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInsertPositionFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-insert-position-field-struct-adapter-mg2368' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInsertPositionFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-viewport-field-struct-adapter-mg2369' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListViewportFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-viewport-field-struct-adapter-mg2369' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListViewportFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-viewport-field-struct-adapter-mg2369' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListViewportFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-interaction-policy-field-struct-adapter-mg2370' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInteractionPolicyFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-interaction-policy-field-struct-adapter-mg2370' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInteractionPolicyFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-interaction-policy-field-struct-adapter-mg2370' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInteractionPolicyFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-click-field-struct-adapter-mg2371' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListClickFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-click-field-struct-adapter-mg2371' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListClickFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-click-field-struct-adapter-mg2371' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListClickFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-hook-policy-field-struct-adapter-mg2372' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHookPolicyFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-hook-policy-field-struct-adapter-mg2372' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHookPolicyFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-hook-policy-field-struct-adapter-mg2372' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHookPolicyFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-sort-field-struct-adapter-mg2373' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListSortFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-sort-field-struct-adapter-mg2373' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListSortFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-sort-field-struct-adapter-mg2373' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListSortFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-pool-policy-field-struct-adapter-mg2374' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPoolPolicyFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-pool-policy-field-struct-adapter-mg2374' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPoolPolicyFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-pool-policy-field-struct-adapter-mg2374' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPoolPolicyFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-format-descriptor-field-struct-adapter-mg2375' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatDescriptorFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-format-descriptor-field-struct-adapter-mg2375' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatDescriptorFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-format-descriptor-field-struct-adapter-mg2375' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatDescriptorFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-format-descriptor-state-field-struct-adapter-mg2376' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatDescriptorStateFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-format-descriptor-state-field-struct-adapter-mg2376' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatDescriptorStateFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-format-descriptor-state-field-struct-adapter-mg2376' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatDescriptorStateFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-column-metrics-field-struct-adapter-mg2377' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnMetricsFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-column-metrics-field-struct-adapter-mg2377' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnMetricsFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-column-metrics-field-struct-adapter-mg2377' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnMetricsFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-column-metric-value-field-struct-adapter-mg2378' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnMetricValueFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-column-metric-value-field-struct-adapter-mg2378' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnMetricValueFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-column-metric-value-field-struct-adapter-mg2378' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnMetricValueFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-edit-field-struct-adapter-mg2379' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListEditFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-edit-field-struct-adapter-mg2379' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListEditFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-edit-field-struct-adapter-mg2379' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListEditFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-column-geometry-field-struct-adapter-mg2380' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnGeometryFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-column-geometry-field-struct-adapter-mg2380' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnGeometryFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-column-geometry-field-struct-adapter-mg2380' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnGeometryFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-column-layout-field-struct-adapter-mg2381' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnLayoutFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-column-layout-field-struct-adapter-mg2381' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnLayoutFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-column-layout-field-struct-adapter-mg2381' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnLayoutFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-column-visibility-field-struct-adapter-mg2382' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnVisibilityFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-column-visibility-field-struct-adapter-mg2382' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnVisibilityFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-column-visibility-field-struct-adapter-mg2382' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnVisibilityFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-column-order-field-struct-adapter-mg2383' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnOrderFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-column-order-field-struct-adapter-mg2383' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnOrderFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-column-order-field-struct-adapter-mg2383' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnOrderFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-presentation-policy-field-struct-adapter-mg2384' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPresentationPolicyFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-presentation-policy-field-struct-adapter-mg2384' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPresentationPolicyFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-presentation-policy-field-struct-adapter-mg2384' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPresentationPolicyFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-state-multiplexed-field-struct-adapter-mg2385' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListStateMultiplexedFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-state-multiplexed-field-struct-adapter-mg2385' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListStateMultiplexedFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-state-multiplexed-field-struct-adapter-mg2385' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListStateMultiplexedFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-pointer-slot-field-struct-adapter-mg2386' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPointerSlotFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-pointer-slot-field-struct-adapter-mg2386' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPointerSlotFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-pointer-slot-field-struct-adapter-mg2386' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPointerSlotFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-owned-record-header-field-struct-adapter-mg2387' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListOwnedRecordHeaderFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-owned-record-header-field-struct-adapter-mg2387' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListOwnedRecordHeaderFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-owned-record-header-field-struct-adapter-mg2387' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListOwnedRecordHeaderFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-scalar-display-row-field-struct-adapter-mg2388' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListScalarDisplayRowFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-scalar-display-row-field-struct-adapter-mg2388' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListScalarDisplayRowFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-scalar-display-row-field-struct-adapter-mg2388' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListScalarDisplayRowFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-column-order-byte-field-struct-adapter-mg2389' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnOrderByteFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-column-order-byte-field-struct-adapter-mg2389' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnOrderByteFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-column-order-byte-field-struct-adapter-mg2389' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnOrderByteFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-custom-font-runtime-offset-bridge-struct-mg2390' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontRuntimeOffsetBridgeStructRoot'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-runtime-offset-bridge-struct-mg2390' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontRuntimeOffsetBridgeStructRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-runtime-offset-bridge-struct-mg2390' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontRuntimeOffsetBridgeStructRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-geometry-offset-bridge-struct-mg2391' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaGeometryOffsetBridgeStructRoot'
    Qualify-ClosureVariant -Name 'mui-area-geometry-offset-bridge-struct-mg2391' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaGeometryOffsetBridgeStructRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-geometry-offset-bridge-struct-mg2391' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaGeometryOffsetBridgeStructRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-balance-policy-offset-bridge-struct-mg2392' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BalancePolicyOffsetBridgeStructRoot'
    Qualify-ClosureVariant -Name 'mui-balance-policy-offset-bridge-struct-mg2392' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BalancePolicyOffsetBridgeStructRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-balance-policy-offset-bridge-struct-mg2392' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BalancePolicyOffsetBridgeStructRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bitmap-geometry-offset-bridge-struct-mg2393' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapGeometryOffsetBridgeStructRoot'
    Qualify-ClosureVariant -Name 'mui-bitmap-geometry-offset-bridge-struct-mg2393' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapGeometryOffsetBridgeStructRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bitmap-geometry-offset-bridge-struct-mg2393' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapGeometryOffsetBridgeStructRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bitmap-source-offset-bridge-struct-mg2394' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapSourceOffsetBridgeStructRoot'
    Qualify-ClosureVariant -Name 'mui-bitmap-source-offset-bridge-struct-mg2394' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapSourceOffsetBridgeStructRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bitmap-source-offset-bridge-struct-mg2394' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapSourceOffsetBridgeStructRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bodychunk-format-offset-bridge-struct-mg2395' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BodychunkFormatOffsetBridgeStructRoot'
    Qualify-ClosureVariant -Name 'mui-bodychunk-format-offset-bridge-struct-mg2395' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BodychunkFormatOffsetBridgeStructRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bodychunk-format-offset-bridge-struct-mg2395' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BodychunkFormatOffsetBridgeStructRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-control-font-offset-bridge-struct-mg2396' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontOffsetBridgeStructRoot'
    Qualify-ClosureVariant -Name 'mui-control-font-offset-bridge-struct-mg2396' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontOffsetBridgeStructRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-control-font-offset-bridge-struct-mg2396' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontOffsetBridgeStructRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dirlist-packet-offset-bridge-struct-mg2397' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistPacketOffsetBridgeStructRoot'
    Qualify-ClosureVariant -Name 'mui-dirlist-packet-offset-bridge-struct-mg2397' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistPacketOffsetBridgeStructRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dirlist-packet-offset-bridge-struct-mg2397' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistPacketOffsetBridgeStructRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-boopsi-geometry-field-transfer-struct-mg2398' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiGeometryFieldTransferStructRoot'
    Qualify-ClosureVariant -Name 'mui-external-boopsi-geometry-field-transfer-struct-mg2398' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiGeometryFieldTransferStructRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-boopsi-geometry-field-transfer-struct-mg2398' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiGeometryFieldTransferStructRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-boopsi-resource-field-transfer-struct-mg2399' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiResourceFieldTransferStructRoot'
    Qualify-ClosureVariant -Name 'mui-external-boopsi-resource-field-transfer-struct-mg2399' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiResourceFieldTransferStructRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-boopsi-resource-field-transfer-struct-mg2399' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiResourceFieldTransferStructRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-scratch-field-transfer-struct-mg2400' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalScratchFieldTransferStructRoot'
    Qualify-ClosureVariant -Name 'mui-external-scratch-field-transfer-struct-mg2400' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalScratchFieldTransferStructRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-scratch-field-transfer-struct-mg2400' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalScratchFieldTransferStructRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-dtpic-field-transfer-struct-mg2401' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalDtpicFieldTransferStructRoot'
    Qualify-ClosureVariant -Name 'mui-external-dtpic-field-transfer-struct-mg2401' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalDtpicFieldTransferStructRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-dtpic-field-transfer-struct-mg2401' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalDtpicFieldTransferStructRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-dtpic-layout-field-transfer-struct-mg2402' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalDtpicLayoutFieldTransferStructRoot'
    Qualify-ClosureVariant -Name 'mui-external-dtpic-layout-field-transfer-struct-mg2402' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalDtpicLayoutFieldTransferStructRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-dtpic-layout-field-transfer-struct-mg2402' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalDtpicLayoutFieldTransferStructRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-notification-field-transfer-struct-mg2403' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalNotificationFieldTransferStructRoot'
    Qualify-ClosureVariant -Name 'mui-external-notification-field-transfer-struct-mg2403' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalNotificationFieldTransferStructRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-notification-field-transfer-struct-mg2403' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalNotificationFieldTransferStructRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-display-environment-field-transfer-struct-mg2404' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalDisplayEnvironmentFieldTransferStructRoot'
    Qualify-ClosureVariant -Name 'mui-external-display-environment-field-transfer-struct-mg2404' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalDisplayEnvironmentFieldTransferStructRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-display-environment-field-transfer-struct-mg2404' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalDisplayEnvironmentFieldTransferStructRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-rastport-slot-field-transfer-struct-mg2405' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalRastPortSlotFieldTransferStructRoot'
    Qualify-ClosureVariant -Name 'mui-external-rastport-slot-field-transfer-struct-mg2405' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalRastPortSlotFieldTransferStructRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-rastport-slot-field-transfer-struct-mg2405' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalRastPortSlotFieldTransferStructRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-render-info-field-transfer-struct-mg2406' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalRenderInfoFieldTransferStructRoot'
    Qualify-ClosureVariant -Name 'mui-external-render-info-field-transfer-struct-mg2406' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalRenderInfoFieldTransferStructRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-render-info-field-transfer-struct-mg2406' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalRenderInfoFieldTransferStructRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-wrapper-header-field-transfer-struct-mg2407' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperHeaderFieldTransferStructRoot'
    Qualify-ClosureVariant -Name 'mui-external-wrapper-header-field-transfer-struct-mg2407' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperHeaderFieldTransferStructRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-wrapper-header-field-transfer-struct-mg2407' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperHeaderFieldTransferStructRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-boopsi-packet-field-transfer-struct-mg2408' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiPacketFieldTransferStructRoot'
    Qualify-ClosureVariant -Name 'mui-external-boopsi-packet-field-transfer-struct-mg2408' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiPacketFieldTransferStructRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-boopsi-packet-field-transfer-struct-mg2408' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiPacketFieldTransferStructRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-boopsi-work-region-struct-mg2409' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiWorkRegionStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-external-boopsi-work-region-struct-mg2409' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiWorkRegionStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-boopsi-work-region-struct-mg2409' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiWorkRegionStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-scratch-field-struct-mg2410' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalScratchFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-external-scratch-field-struct-mg2410' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalScratchFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-scratch-field-struct-mg2410' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalScratchFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-boopsi-geometry-field-struct-mg2411' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiGeometryFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-external-boopsi-geometry-field-struct-mg2411' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiGeometryFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-boopsi-geometry-field-struct-mg2411' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiGeometryFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-boopsi-resource-field-struct-mg2412' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiResourceFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-external-boopsi-resource-field-struct-mg2412' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiResourceFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-boopsi-resource-field-struct-mg2412' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiResourceFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-bubble-packet-field-struct-mg2413' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBubblePacketFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-bubble-packet-field-struct-mg2413' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBubblePacketFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-bubble-packet-field-struct-mg2413' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBubblePacketFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-activation-packet-field-struct-mg2414' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationPacketFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-activation-packet-field-struct-mg2414' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationPacketFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-activation-packet-field-struct-mg2414' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationPacketFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-activation-state-field-struct-mg2415' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationStateFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-activation-state-field-struct-mg2415' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationStateFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-activation-state-field-struct-mg2415' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationStateFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-help-field-struct-mg2416' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationHelpFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-help-field-struct-mg2416' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationHelpFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-help-field-struct-mg2416' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationHelpFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-config-window-field-struct-mg2417' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationConfigWindowFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-config-window-field-struct-mg2417' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationConfigWindowFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-config-window-field-struct-mg2417' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationConfigWindowFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-default-config-field-struct-mg2418' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationDefaultConfigFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-default-config-field-struct-mg2418' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationDefaultConfigFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-default-config-field-struct-mg2418' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationDefaultConfigFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-lifecycle-field-struct-mg2419' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationLifecycleFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-lifecycle-field-struct-mg2419' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationLifecycleFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-lifecycle-field-struct-mg2419' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationLifecycleFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-scheduler-field-struct-mg2420' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSchedulerFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-scheduler-field-struct-mg2420' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSchedulerFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-scheduler-field-struct-mg2420' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSchedulerFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-identity-field-struct-mg2421' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIdentityFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-identity-field-struct-mg2421' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIdentityFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-identity-field-struct-mg2421' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIdentityFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-set-config-item-field-struct-mg2422' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSetConfigItemFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-set-config-item-field-struct-mg2422' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSetConfigItemFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-set-config-item-field-struct-mg2422' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSetConfigItemFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-settings-panel-field-struct-mg2423' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPanelFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-settings-panel-field-struct-mg2423' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPanelFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-settings-panel-field-struct-mg2423' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPanelFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-settings-persistence-field-struct-mg2424' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPersistenceFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-settings-persistence-field-struct-mg2424' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPersistenceFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-settings-persistence-field-struct-mg2424' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPersistenceFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-used-classes-field-struct-mg2425' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUsedClassesFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-used-classes-field-struct-mg2425' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUsedClassesFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-used-classes-field-struct-mg2425' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUsedClassesFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-object-field-struct-mg2426' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationObjectFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-object-field-struct-mg2426' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationObjectFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-object-field-struct-mg2426' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationObjectFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-policy-field-struct-mg2427' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPolicyFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-policy-field-struct-mg2427' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPolicyFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-policy-field-struct-mg2427' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPolicyFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-commands-field-struct-mg2428' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCommandsFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-commands-field-struct-mg2428' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCommandsFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-commands-field-struct-mg2428' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCommandsFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-menu-field-struct-mg2429' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-menu-field-struct-mg2429' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-menu-field-struct-mg2429' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-message-routing-field-struct-mg2430' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMessageRoutingFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-message-routing-field-struct-mg2430' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMessageRoutingFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-message-routing-field-struct-mg2430' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMessageRoutingFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-refresh-field-struct-mg2431' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationRefreshFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-refresh-field-struct-mg2431' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationRefreshFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-refresh-field-struct-mg2431' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationRefreshFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-text-field-struct-mg2432' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationTextFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-text-field-struct-mg2432' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationTextFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-text-field-struct-mg2432' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationTextFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-window-relationship-field-struct-mg2433' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowRelationshipFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-window-relationship-field-struct-mg2433' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowRelationshipFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-window-relationship-field-struct-mg2433' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowRelationshipFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-window-list-field-struct-mg2434' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowListFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-window-list-field-struct-mg2434' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowListFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-window-list-field-struct-mg2434' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowListFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-command-field-struct-mg2435' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCommandFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-command-field-struct-mg2435' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCommandFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-command-field-struct-mg2435' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCommandFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-used-classes-vector-field-struct-mg2436' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUsedClassesVectorFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-used-classes-vector-field-struct-mg2436' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUsedClassesVectorFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-used-classes-vector-field-struct-mg2436' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUsedClassesVectorFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-app-message-node-field-struct-mg2437' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AppMessageNodeFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-app-message-node-field-struct-mg2437' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AppMessageNodeFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-app-message-node-field-struct-mg2437' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AppMessageNodeFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-workbench-argument-field-struct-mg2438' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WorkbenchArgumentFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-workbench-argument-field-struct-mg2438' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WorkbenchArgumentFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-workbench-argument-field-struct-mg2438' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WorkbenchArgumentFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-app-message-field-struct-mg2439' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AppMessageFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-app-message-field-struct-mg2439' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AppMessageFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-app-message-field-struct-mg2439' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AppMessageFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-builtin-font-field-struct-mg2440' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBuiltinFontFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-builtin-font-field-struct-mg2440' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBuiltinFontFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-builtin-font-field-struct-mg2440' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBuiltinFontFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-control-char-field-struct-mg2441' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaControlCharFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-control-char-field-struct-mg2441' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaControlCharFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-control-char-field-struct-mg2441' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaControlCharFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-context-menu-field-struct-mg2442' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-context-menu-field-struct-mg2442' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-context-menu-field-struct-mg2442' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-double-click-field-struct-mg2443' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleClickFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-double-click-field-struct-mg2443' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleClickFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-double-click-field-struct-mg2443' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleClickFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-disappear-policy-field-struct-mg2444' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDisappearPolicyFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-disappear-policy-field-struct-mg2444' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDisappearPolicyFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-disappear-policy-field-struct-mg2444' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDisappearPolicyFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-double-buffer-field-struct-mg2445' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleBufferFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-double-buffer-field-struct-mg2445' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleBufferFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-double-buffer-field-struct-mg2445' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleBufferFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-drag-policy-field-struct-mg2446' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragPolicyFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-drag-policy-field-struct-mg2446' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragPolicyFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-drag-policy-field-struct-mg2446' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragPolicyFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-fixed-text-field-struct-mg2447' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFixedTextFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-fixed-text-field-struct-mg2447' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFixedTextFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-fixed-text-field-struct-mg2447' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFixedTextFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-cycle-chain-field-struct-mg2448' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCycleChainFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-cycle-chain-field-struct-mg2448' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCycleChainFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-cycle-chain-field-struct-mg2448' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCycleChainFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-floating-field-struct-mg2449' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFloatingFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-floating-field-struct-mg2449' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFloatingFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-floating-field-struct-mg2449' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFloatingFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-font-selection-field-struct-mg2450' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontSelectionFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-font-selection-field-struct-mg2450' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontSelectionFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-font-selection-field-struct-mg2450' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontSelectionFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-short-help-field-struct-mg2451' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-short-help-field-struct-mg2451' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-short-help-field-struct-mg2451' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-choice-active-field-struct-mg2452' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceActiveFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-choice-active-field-struct-mg2452' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceActiveFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-choice-active-field-struct-mg2452' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceActiveFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bitmap-remapped-field-struct-mg2453' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapRemappedFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-bitmap-remapped-field-struct-mg2453' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapRemappedFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bitmap-remapped-field-struct-mg2453' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapRemappedFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-choice-entries-field-struct-mg2454' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceEntriesFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-choice-entries-field-struct-mg2454' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceEntriesFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-choice-entries-field-struct-mg2454' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceEntriesFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bitmap-source-field-struct-mg2455' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapSourceFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-bitmap-source-field-struct-mg2455' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapSourceFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bitmap-source-field-struct-mg2455' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapSourceFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-gauge-info-rate-field-struct-mg2456' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoRateFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-gauge-info-rate-field-struct-mg2456' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoRateFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-gauge-info-rate-field-struct-mg2456' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoRateFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-gauge-info-text-field-struct-mg2457' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoTextFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-gauge-info-text-field-struct-mg2457' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoTextFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-gauge-info-text-field-struct-mg2457' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoTextFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-control-font-field-struct-mg2458' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontFieldStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-control-font-field-struct-mg2458' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontFieldStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-control-font-field-struct-mg2458' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontFieldStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-gauge-state-all-fields-struct-mg2459' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeStateAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-gauge-state-all-fields-struct-mg2459' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeStateAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-gauge-state-all-fields-struct-mg2459' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeStateAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-text-color-all-fields-struct-mg2460' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTextColorAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-text-color-all-fields-struct-mg2460' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTextColorAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-text-color-all-fields-struct-mg2460' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTextColorAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bodychunk-format-all-fields-struct-mg2461' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BodychunkFormatAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-bodychunk-format-all-fields-struct-mg2461' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BodychunkFormatAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bodychunk-format-all-fields-struct-mg2461' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BodychunkFormatAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bitmap-policy-all-fields-struct-mg2462' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapPolicyAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-bitmap-policy-all-fields-struct-mg2462' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapPolicyAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bitmap-policy-all-fields-struct-mg2462' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapPolicyAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bitmap-geometry-all-fields-struct-mg2463' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapGeometryAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-bitmap-geometry-all-fields-struct-mg2463' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapGeometryAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bitmap-geometry-all-fields-struct-mg2463' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapGeometryAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-weight-all-fields-struct-mg2464' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaWeightAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-weight-all-fields-struct-mg2464' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaWeightAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-weight-all-fields-struct-mg2464' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaWeightAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-custom-font-all-fields-struct-mg2465' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-all-fields-struct-mg2465' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-all-fields-struct-mg2465' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-gadget-gadget-all-fields-struct-mg2466' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetGadgetAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-gadget-gadget-all-fields-struct-mg2466' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetGadgetAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-gadget-gadget-all-fields-struct-mg2466' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetGadgetAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-custom-font-runtime-all-fields-struct-mg2467' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontRuntimeAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-runtime-all-fields-struct-mg2467' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontRuntimeAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-runtime-all-fields-struct-mg2467' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontRuntimeAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-levelmeter-label-all-fields-struct-mg2468' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterLabelAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-levelmeter-label-all-fields-struct-mg2468' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterLabelAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-levelmeter-label-all-fields-struct-mg2468' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterLabelAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-levelmeter-presentation-all-fields-struct-mg2469' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterPresentationAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-levelmeter-presentation-all-fields-struct-mg2469' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterPresentationAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-levelmeter-presentation-all-fields-struct-mg2469' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterPresentationAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-balance-policy-all-fields-struct-mg2470' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BalancePolicyAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-balance-policy-all-fields-struct-mg2470' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BalancePolicyAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-balance-policy-all-fields-struct-mg2470' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BalancePolicyAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-scale-presentation-all-fields-struct-mg2471' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScalePresentationAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-scale-presentation-all-fields-struct-mg2471' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScalePresentationAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-scale-presentation-all-fields-struct-mg2471' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScalePresentationAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-layout-hook-all-fields-struct-mg2472' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutHookAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-group-layout-hook-all-fields-struct-mg2472' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutHookAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-layout-hook-all-fields-struct-mg2472' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutHookAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-layout-policy-all-fields-struct-mg2473' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutPolicyAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-group-layout-policy-all-fields-struct-mg2473' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutPolicyAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-layout-policy-all-fields-struct-mg2473' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutPolicyAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-presentation-all-fields-struct-mg2474' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaPresentationAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-presentation-all-fields-struct-mg2474' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaPresentationAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-presentation-all-fields-struct-mg2474' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaPresentationAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-timer-all-fields-struct-mg2475' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-timer-all-fields-struct-mg2475' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-timer-all-fields-struct-mg2475' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-timer-event-all-fields-struct-mg2476' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerEventAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-timer-event-all-fields-struct-mg2476' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerEventAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-timer-event-all-fields-struct-mg2476' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerEventAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-render-policy-all-fields-struct-mg2477' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaRenderPolicyAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-render-policy-all-fields-struct-mg2477' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaRenderPolicyAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-render-policy-all-fields-struct-mg2477' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaRenderPolicyAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-drag-all-fields-struct-mg2478' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-drag-all-fields-struct-mg2478' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-drag-all-fields-struct-mg2478' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-gadget-interaction-all-fields-struct-mg2479' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetInteractionAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-gadget-interaction-all-fields-struct-mg2479' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetInteractionAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-gadget-interaction-all-fields-struct-mg2479' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetInteractionAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-resize-all-fields-struct-mg2480' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaResizeAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-resize-all-fields-struct-mg2480' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaResizeAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-resize-all-fields-struct-mg2480' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaResizeAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-geometry-all-fields-struct-mg2481' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaGeometryAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-geometry-all-fields-struct-mg2481' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaGeometryAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-geometry-all-fields-struct-mg2481' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaGeometryAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-error-service-all-fields-struct-mg2482' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ErrorServiceAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-error-service-all-fields-struct-mg2482' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ErrorServiceAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-error-service-all-fields-struct-mg2482' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ErrorServiceAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-page-all-fields-struct-mg2483' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupPageAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-group-page-all-fields-struct-mg2483' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupPageAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-page-all-fields-struct-mg2483' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupPageAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-sleep-state-all-fields-struct-mg2484' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SleepStateAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-sleep-state-all-fields-struct-mg2484' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SleepStateAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-sleep-state-all-fields-struct-mg2484' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SleepStateAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-contents-all-fields-struct-mg2485' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextContentsAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-text-contents-all-fields-struct-mg2485' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextContentsAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-contents-all-fields-struct-mg2485' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextContentsAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-slider-presentation-all-fields-struct-mg2486' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SliderPresentationAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-slider-presentation-all-fields-struct-mg2486' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SliderPresentationAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-slider-presentation-all-fields-struct-mg2486' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SliderPresentationAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-rectangle-bar-title-all-fields-struct-mg2487' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectangleBarTitleAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-rectangle-bar-title-all-fields-struct-mg2487' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectangleBarTitleAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-rectangle-bar-title-all-fields-struct-mg2487' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectangleBarTitleAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-prop-range-all-fields-struct-mg2488' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropRangeAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-prop-range-all-fields-struct-mg2488' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropRangeAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-prop-range-all-fields-struct-mg2488' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropRangeAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-cursor-all-fields-struct-mg2489' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringCursorAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-cursor-all-fields-struct-mg2489' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringCursorAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-cursor-all-fields-struct-mg2489' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringCursorAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-contents-all-fields-struct-mg2490' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringContentsAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-contents-all-fields-struct-mg2490' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringContentsAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-contents-all-fields-struct-mg2490' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringContentsAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-placeholder-all-fields-struct-mg2491' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPlaceholderAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-placeholder-all-fields-struct-mg2491' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPlaceholderAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-placeholder-all-fields-struct-mg2491' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPlaceholderAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-acknowledge-all-fields-struct-mg2492' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAcknowledgeAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-acknowledge-all-fields-struct-mg2492' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAcknowledgeAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-acknowledge-all-fields-struct-mg2492' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAcknowledgeAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-integer-all-fields-struct-mg2493' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringIntegerAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-integer-all-fields-struct-mg2493' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringIntegerAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-integer-all-fields-struct-mg2493' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringIntegerAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-spell-checking-all-fields-struct-mg2494' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringSpellCheckingAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-spell-checking-all-fields-struct-mg2494' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringSpellCheckingAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-spell-checking-all-fields-struct-mg2494' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringSpellCheckingAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-attached-list-all-fields-struct-mg2495' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAttachedListAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-attached-list-all-fields-struct-mg2495' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAttachedListAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-attached-list-all-fields-struct-mg2495' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAttachedListAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-filter-all-fields-struct-mg2496' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringFilterAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-filter-all-fields-struct-mg2496' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringFilterAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-filter-all-fields-struct-mg2496' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringFilterAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-copy-all-fields-struct-mg2497' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextCopyAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-text-copy-all-fields-struct-mg2497' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextCopyAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-copy-all-fields-struct-mg2497' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextCopyAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-pre-parse-all-fields-struct-mg2498' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPreParseAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-text-pre-parse-all-fields-struct-mg2498' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPreParseAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-pre-parse-all-fields-struct-mg2498' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPreParseAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-shortened-all-fields-struct-mg2499' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextShortenedAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-text-shortened-all-fields-struct-mg2499' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextShortenedAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-shortened-all-fields-struct-mg2499' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextShortenedAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-unicode-all-fields-struct-mg2500' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextUnicodeAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-text-unicode-all-fields-struct-mg2500' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextUnicodeAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-unicode-all-fields-struct-mg2500' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextUnicodeAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-volumelist-mode-all-fields-struct-mg2501' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::VolumelistModeAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-volumelist-mode-all-fields-struct-mg2501' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::VolumelistModeAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-volumelist-mode-all-fields-struct-mg2501' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::VolumelistModeAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-asl-all-fields-struct-mg2502' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-asl-all-fields-struct-mg2502' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-asl-all-fields-struct-mg2502' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-image-font-match-string-all-fields-struct-mg2503' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStringAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-image-font-match-string-all-fields-struct-mg2503' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStringAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-image-font-match-string-all-fields-struct-mg2503' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStringAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-rectangle-presentation-all-fields-struct-mg2504' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectanglePresentationAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-rectangle-presentation-all-fields-struct-mg2504' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectanglePresentationAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-rectangle-presentation-all-fields-struct-mg2504' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectanglePresentationAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-register-policy-all-fields-struct-mg2505' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RegisterPolicyAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-register-policy-all-fields-struct-mg2505' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RegisterPolicyAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-register-policy-all-fields-struct-mg2505' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RegisterPolicyAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-scrollbar-layout-all-fields-struct-mg2506' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollbarLayoutAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-scrollbar-layout-all-fields-struct-mg2506' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollbarLayoutAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-scrollbar-layout-all-fields-struct-mg2506' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollbarLayoutAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-requester-service-all-fields-struct-mg2507' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterServiceAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-requester-service-all-fields-struct-mg2507' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterServiceAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-requester-service-all-fields-struct-mg2507' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterServiceAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-numeric-format-all-fields-struct-mg2508' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NumericFormatAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-numeric-format-all-fields-struct-mg2508' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NumericFormatAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-numeric-format-all-fields-struct-mg2508' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NumericFormatAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-image-old-image-all-fields-struct-mg2509' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageOldImageAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-image-old-image-all-fields-struct-mg2509' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageOldImageAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-image-old-image-all-fields-struct-mg2509' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageOldImageAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-selectgroup-all-fields-struct-mg2510' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SelectgroupAllFieldsStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-selectgroup-all-fields-struct-mg2510' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SelectgroupAllFieldsStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-selectgroup-all-fields-struct-mg2510' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SelectgroupAllFieldsStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-slider-presentation-all-fields-struct-mg2511' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SliderPresentationAllFieldsStructCursorRoot'
    Qualify-ClosureVariant -Name 'mui-slider-presentation-all-fields-struct-mg2511' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SliderPresentationAllFieldsStructCursorRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-slider-presentation-all-fields-struct-mg2511' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SliderPresentationAllFieldsStructCursorRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-copy-cursor-struct-mg2512' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextCopyCursorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-text-copy-cursor-struct-mg2512' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextCopyCursorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-copy-cursor-struct-mg2512' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextCopyCursorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-keyadjust-text-cursor-struct-mg2513' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::KeyadjustTextCursorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-keyadjust-text-cursor-struct-mg2513' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::KeyadjustTextCursorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-keyadjust-text-cursor-struct-mg2513' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::KeyadjustTextCursorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-asl-tag-item-cursor-struct-mg2514' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslTagItemCursorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-asl-tag-item-cursor-struct-mg2514' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslTagItemCursorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-asl-tag-item-cursor-struct-mg2514' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslTagItemCursorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-short-help-cursor-struct-mg2515' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpCursorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-short-help-cursor-struct-mg2515' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpCursorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-short-help-cursor-struct-mg2515' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpCursorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-context-menu-message-struct-mg2516' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuMessageStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-context-menu-message-struct-mg2516' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuMessageStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-context-menu-message-struct-mg2516' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuMessageStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-boopsi-query-message-struct-mg2517' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BoopsiQueryFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-boopsi-query-message-struct-mg2517' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BoopsiQueryFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-boopsi-query-message-struct-mg2517' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BoopsiQueryFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-custom-font-message-struct-mg2518' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontMessageStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-message-struct-mg2518' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontMessageStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-message-struct-mg2518' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontMessageStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-call-hook-message-struct-mg2519' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CallHookFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-call-hook-message-struct-mg2519' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CallHookFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-call-hook-message-struct-mg2519' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CallHookFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-call-hook-parameter-tail-cursor-mg2807' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CallHookParameterStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-call-hook-parameter-tail-cursor-mg2807' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CallHookParameterStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-call-hook-parameter-tail-cursor-mg2807' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CallHookParameterStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-requester-parameter-slot-field-struct-mg2808' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterParameterSlotStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-requester-parameter-slot-field-struct-mg2808' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterParameterSlotStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-requester-parameter-slot-field-struct-mg2808' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterParameterSlotStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-push-method-parameter-header-cursor-struct-mg2809' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPushMethodParameterStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-push-method-parameter-header-cursor-struct-mg2809' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPushMethodParameterStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-push-method-parameter-header-cursor-struct-mg2809' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPushMethodParameterStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-process-argument-vector-header-cursor-struct-mg2810' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessArgumentVectorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-process-argument-vector-header-cursor-struct-mg2810' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessArgumentVectorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-process-argument-vector-header-cursor-struct-mg2810' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessArgumentVectorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-record-message-struct-mg2520' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionRecordFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-collection-record-message-struct-mg2520' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionRecordFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-record-message-struct-mg2520' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionRecordFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-basic-message-struct-mg2521' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionBasicFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-collection-basic-message-struct-mg2521' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionBasicFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-basic-message-struct-mg2521' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionBasicFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-surface-message-struct-mg2522' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionSurfaceFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-collection-surface-message-struct-mg2522' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionSurfaceFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-surface-message-struct-mg2522' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionSurfaceFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-edit-message-struct-mg2523' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionEditFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-collection-edit-message-struct-mg2523' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionEditFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-edit-message-struct-mg2523' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionEditFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-advanced-message-struct-mg2524' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionAdvancedFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-collection-advanced-message-struct-mg2524' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionAdvancedFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-advanced-message-struct-mg2524' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionAdvancedFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-queue-packet-cursor-struct-mg2525' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationQueuePacketStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-queue-packet-cursor-struct-mg2525' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationQueuePacketStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-queue-packet-cursor-struct-mg2525' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationQueuePacketStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-input-packet-cursor-struct-mg2526' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationInputPacketCursorStructRecordRoot'
    Qualify-ClosureVariant -Name 'mui-application-input-packet-cursor-struct-mg2526' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationInputPacketCursorStructRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-input-packet-cursor-struct-mg2526' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationInputPacketCursorStructRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-presentation-packet-cursor-struct-mg2527' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPresentationPacketStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-presentation-packet-cursor-struct-mg2527' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPresentationPacketStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-presentation-packet-cursor-struct-mg2527' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPresentationPacketStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-settings-packet-cursor-struct-mg2528' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPacketStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-settings-packet-cursor-struct-mg2528' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPacketStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-settings-packet-cursor-struct-mg2528' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPacketStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-method-packet-cursor-struct-mg2529' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMethodPacketStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-method-packet-cursor-struct-mg2529' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMethodPacketStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-method-packet-cursor-struct-mg2529' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMethodPacketStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-cycle-chain-packet-cursor-struct-mg2530' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowCycleChainPacketStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-cycle-chain-packet-cursor-struct-mg2530' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowCycleChainPacketStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-cycle-chain-packet-cursor-struct-mg2530' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowCycleChainPacketStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-cycle-chain-inline-vector-cursor-mg2806' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowCycleChainInlineVectorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-cycle-chain-inline-vector-cursor-mg2806' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowCycleChainInlineVectorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-cycle-chain-inline-vector-cursor-mg2806' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowCycleChainInlineVectorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-menu-packet-cursor-struct-mg2531' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuPacketStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-menu-packet-cursor-struct-mg2531' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuPacketStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-menu-packet-cursor-struct-mg2531' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuPacketStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-handler-packet-cursor-struct-mg2532' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerPacketStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-packet-cursor-struct-mg2532' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerPacketStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-packet-cursor-struct-mg2532' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerPacketStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-window-node-field-cursor-struct-mg2533' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowNodeFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-window-node-field-cursor-struct-mg2533' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowNodeFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-window-node-field-cursor-struct-mg2533' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowNodeFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-window-node-payload-cursor-struct-mg2805' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowNodePayloadCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-window-node-payload-cursor-struct-mg2805' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowNodePayloadCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-window-node-payload-cursor-struct-mg2805' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowNodePayloadCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-event-handler-node-field-cursor-struct-mg2534' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::EventHandlerNodeFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-event-handler-node-field-cursor-struct-mg2534' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::EventHandlerNodeFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-event-handler-node-field-cursor-struct-mg2534' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::EventHandlerNodeFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-input-handler-node-field-cursor-struct-mg2535' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::InputHandlerFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-input-handler-node-field-cursor-struct-mg2535' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::InputHandlerFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-input-handler-node-field-cursor-struct-mg2535' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::InputHandlerFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-persistence-frame-field-cursor-struct-mg2536' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPersistenceFrameStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-persistence-frame-field-cursor-struct-mg2536' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPersistenceFrameStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-persistence-frame-field-cursor-struct-mg2536' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPersistenceFrameStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-settings-file-field-cursor-struct-mg2537' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsFileStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-settings-file-field-cursor-struct-mg2537' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsFileStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-settings-file-field-cursor-struct-mg2537' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsFileStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dataspace-iff-field-cursor-struct-mg2538' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceIffFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-dataspace-iff-field-cursor-struct-mg2538' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceIffFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dataspace-iff-field-cursor-struct-mg2538' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceIffFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-store-record-field-cursor-struct-mg2539' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreRecordStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-store-record-field-cursor-struct-mg2539' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreRecordStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-store-record-field-cursor-struct-mg2539' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreRecordStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-headless-state-field-cursor-struct-mg2540' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessStateStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-headless-state-field-cursor-struct-mg2540' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessStateStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-headless-state-field-cursor-struct-mg2540' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessStateStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-headless-class-field-cursor-struct-mg2541' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessClassFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-headless-class-field-cursor-struct-mg2541' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessClassFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-headless-class-field-cursor-struct-mg2541' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessClassFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-headless-attribute-field-cursor-struct-mg2542' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessAttributeFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-headless-attribute-field-cursor-struct-mg2542' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessAttributeFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-headless-attribute-field-cursor-struct-mg2542' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessAttributeFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-headless-child-field-cursor-struct-mg2543' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessChildFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-headless-child-field-cursor-struct-mg2543' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessChildFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-headless-child-field-cursor-struct-mg2543' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessChildFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-headless-notification-field-cursor-struct-mg2544' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessNotificationFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-headless-notification-field-cursor-struct-mg2544' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessNotificationFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-headless-notification-field-cursor-struct-mg2544' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessNotificationFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-headless-object-field-cursor-struct-mg2545' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessObjectFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-headless-object-field-cursor-struct-mg2545' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessObjectFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-headless-object-field-cursor-struct-mg2545' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessObjectFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-guest-ulong-field-cursor-struct-mg2546' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GuestUlongStorageFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-guest-ulong-field-cursor-struct-mg2546' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GuestUlongStorageFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-guest-ulong-field-cursor-struct-mg2546' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GuestUlongStorageFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-headless-method-field-cursor-struct-mg2547' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessMethodFieldCursorStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-headless-method-field-cursor-struct-mg2547' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessMethodFieldCursorStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-headless-method-field-cursor-struct-mg2547' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessMethodFieldCursorStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-headless-om-field-cursor-struct-mg2548' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessOmRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-headless-om-field-cursor-struct-mg2548' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessOmRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-headless-om-field-cursor-struct-mg2548' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessOmRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-grid-field-cursor-struct-mg2549' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-group-grid-field-cursor-struct-mg2549' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-grid-field-cursor-struct-mg2549' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-control-font-resolution-field-cursor-struct-mg2550' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontResolutionStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-control-font-resolution-field-cursor-struct-mg2550' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontResolutionStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-control-font-resolution-field-cursor-struct-mg2550' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontResolutionStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-makeobject-preparse-field-cursor-struct-mg2551' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectPreParseFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-makeobject-preparse-field-cursor-struct-mg2551' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectPreParseFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-makeobject-preparse-field-cursor-struct-mg2551' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectPreParseFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-prop-policy-state-field-cursor-struct-mg2552' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropPolicyStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-prop-policy-state-field-cursor-struct-mg2552' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-prop-policy-state-field-cursor-struct-mg2552' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-focus-state-field-cursor-struct-mg2553' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowFocusStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-focus-state-field-cursor-struct-mg2553' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowFocusStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-focus-state-field-cursor-struct-mg2553' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowFocusStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-control-state-field-cursor-struct-mg2554' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowControlStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-control-state-field-cursor-struct-mg2554' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowControlStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-control-state-field-cursor-struct-mg2554' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowControlStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-presentation-state-field-cursor-struct-mg2555' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowPresentationStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-presentation-state-field-cursor-struct-mg2555' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowPresentationStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-presentation-state-field-cursor-struct-mg2555' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowPresentationStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-relationship-state-field-cursor-struct-mg2556' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowRelationshipStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-relationship-state-field-cursor-struct-mg2556' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowRelationshipStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-relationship-state-field-cursor-struct-mg2556' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowRelationshipStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-visual-state-field-cursor-struct-mg2557' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowVisualStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-visual-state-field-cursor-struct-mg2557' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowVisualStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-visual-state-field-cursor-struct-mg2557' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowVisualStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-interaction-state-field-cursor-struct-mg2558' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowInteractionStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-interaction-state-field-cursor-struct-mg2558' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowInteractionStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-interaction-state-field-cursor-struct-mg2558' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowInteractionStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-scrollgroup-policy-state-field-cursor-struct-mg2560' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupPolicyStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-scrollgroup-policy-state-field-cursor-struct-mg2560' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupPolicyStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-scrollgroup-policy-state-field-cursor-struct-mg2560' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupPolicyStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-open-policy-state-field-cursor-struct-mg2561' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowOpenPolicyStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-open-policy-state-field-cursor-struct-mg2561' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowOpenPolicyStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-open-policy-state-field-cursor-struct-mg2561' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowOpenPolicyStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-minmax-field-cursor-struct-mg2562' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MinMaxFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-minmax-field-cursor-struct-mg2562' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MinMaxFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-minmax-field-cursor-struct-mg2562' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MinMaxFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-testpos-field-cursor-struct-mg2563' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTestPosFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-testpos-field-cursor-struct-mg2563' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTestPosFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-testpos-field-cursor-struct-mg2563' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTestPosFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-input-field-cursor-struct-mg2564' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInputFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-list-input-field-cursor-struct-mg2564' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInputFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-input-field-cursor-struct-mg2564' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInputFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-intui-message-field-cursor-struct-mg2565' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::IntuiMessageStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-intui-message-field-cursor-struct-mg2565' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::IntuiMessageStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-intui-message-field-cursor-struct-mg2565' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::IntuiMessageStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-drag-state-field-cursor-struct-mg2566' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewDragStateFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-listview-drag-state-field-cursor-struct-mg2566' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewDragStateFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-drag-state-field-cursor-struct-mg2566' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewDragStateFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-layout-policy-state-field-cursor-struct-mg2559' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaLayoutPolicyStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-layout-policy-state-field-cursor-struct-mg2559' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaLayoutPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-layout-policy-state-field-cursor-struct-mg2559' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaLayoutPolicyStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-layout-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutMethodHeaderStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-layout-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutMethodHeaderStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-layout-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LayoutMethodHeaderStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-scrollbar-layout-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollbarLayoutServiceRoot'
    Qualify-ClosureVariant -Name 'mui-scrollbar-layout-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollbarLayoutServiceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-scrollbar-layout-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollbarLayoutServiceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-class-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ClassServiceRoot'
    Qualify-ClosureVariant -Name 'mui-class-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ClassServiceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-class-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ClassServiceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-class-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalClassServiceRoot'
    Qualify-ClosureVariant -Name 'mui-external-class-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalClassServiceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-class-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalClassServiceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-asl-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslServiceRoot'
    Qualify-ClosureVariant -Name 'mui-asl-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslServiceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-asl-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslServiceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-asl-service-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslServiceRecordRoot'
    Qualify-ClosureVariant -Name 'mui-asl-service-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslServiceRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-asl-service-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslServiceRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-asl-tag-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslTagServiceRoot'
    Qualify-ClosureVariant -Name 'mui-asl-tag-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslTagServiceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-asl-tag-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslTagServiceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-asl-tag-item-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslTagItemCodecRoot'
    Qualify-ClosureVariant -Name 'mui-asl-tag-item-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslTagItemCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-asl-tag-item-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AslTagItemCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-requester-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterServiceRoot'
    Qualify-ClosureVariant -Name 'mui-requester-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterServiceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-requester-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterServiceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-requester-service-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterServiceRecordRoot'
    Qualify-ClosureVariant -Name 'mui-requester-service-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterServiceRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-requester-service-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterServiceRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-requester-service-field-struct-mg2292' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterServiceFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-requester-service-field-struct-mg2292' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterServiceFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-requester-service-field-struct-mg2292' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterServiceFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-requester-payload-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterPayloadServiceRoot'
    Qualify-ClosureVariant -Name 'mui-requester-payload-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterPayloadServiceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-requester-payload-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterPayloadServiceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-requester-format-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterFormatServiceRoot'
    Qualify-ClosureVariant -Name 'mui-requester-format-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterFormatServiceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-requester-format-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterFormatServiceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-requester-application-signal-policy' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRequesterSignalRoot::CtrlCInterruptsApplicationRequesterPump'
    Qualify-ClosureVariant -Name 'mui-requester-application-signal-policy' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRequesterSignalRoot::CtrlCInterruptsApplicationRequesterPump' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-requester-application-signal-policy' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRequesterSignalRoot::CtrlCInterruptsApplicationRequesterPump' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-requester-gadget-set-struct' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterGadgetSetStructRoot'
    Qualify-ClosureVariant -Name 'mui-requester-gadget-set-struct' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterGadgetSetStructRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-requester-gadget-set-struct' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RequesterGadgetSetStructRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-redraw-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RedrawServiceRoot'
    Qualify-ClosureVariant -Name 'mui-redraw-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RedrawServiceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-redraw-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RedrawServiceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-new-object-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NewObjectServiceRoot'
    Qualify-ClosureVariant -Name 'mui-new-object-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NewObjectServiceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-new-object-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NewObjectServiceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-common-factory-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonFactoryServiceRoot'
    Qualify-ClosureVariant -Name 'mui-common-factory-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonFactoryServiceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-common-factory-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonFactoryServiceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-class-common-factory-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ClassServiceCommonFactoryRoot'
    Qualify-ClosureVariant -Name 'mui-class-common-factory-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ClassServiceCommonFactoryRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-class-common-factory-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ClassServiceCommonFactoryRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-new-object-menu-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NewObjectMenuServiceRoot'
    Qualify-ClosureVariant -Name 'mui-new-object-menu-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NewObjectMenuServiceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-new-object-menu-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NewObjectMenuServiceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-misc-object-factory' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscObjectFactoryRoot'
    Qualify-ClosureVariant -Name 'mui-misc-object-factory' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscObjectFactoryRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-misc-object-factory' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscObjectFactoryRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-misc-object-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscObjectDispatcherRoot'
    Qualify-ClosureVariant -Name 'mui-misc-object-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscObjectDispatcherRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-misc-object-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscObjectDispatcherRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-misc-object-lifecycle' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscObjectLifecycleRoot'
    Qualify-ClosureVariant -Name 'mui-misc-object-lifecycle' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscObjectLifecycleRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-misc-object-lifecycle' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscObjectLifecycleRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-misc-specialist-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscSpecialistMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-misc-specialist-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscSpecialistMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-misc-specialist-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscSpecialistMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-misc-specialist-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscSpecialistMethodHeaderStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-misc-specialist-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscSpecialistMethodHeaderStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-misc-specialist-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscSpecialistMethodHeaderStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-keyadjust-raw-key-boundary' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::KeyadjustRawKeyBoundaryRoot'
    Qualify-ClosureVariant -Name 'mui-keyadjust-raw-key-boundary' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::KeyadjustRawKeyBoundaryRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-keyadjust-raw-key-boundary' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::KeyadjustRawKeyBoundaryRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-short-help-check-boundary' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ShortHelpCheckBoundaryRoot'
    Qualify-ClosureVariant -Name 'mui-short-help-check-boundary' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ShortHelpCheckBoundaryRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-short-help-check-boundary' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ShortHelpCheckBoundaryRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-short-help-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpMethodHeaderCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-short-help-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpMethodHeaderCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-short-help-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpMethodHeaderCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-handled-events-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaHandledEventsCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-handled-events-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaHandledEventsCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-handled-events-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaHandledEventsCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-fixed-text-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFixedTextCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-fixed-text-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFixedTextCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-fixed-text-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFixedTextCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-floating-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFloatingCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-floating-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFloatingCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-floating-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFloatingCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-font-resolution-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontResolutionCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-font-resolution-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontResolutionCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-font-resolution-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontResolutionCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-text-color-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTextColorCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-text-color-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTextColorCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-text-color-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTextColorCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-text-color-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTextColorRenderRoot'
    Qualify-ClosureVariant -Name 'mui-area-text-color-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTextColorRenderRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-text-color-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTextColorRenderRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-custom-font-style-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontStyleRenderRoot'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-style-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontStyleRenderRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-style-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontStyleRenderRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-preparse-style-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPreParseStyleRenderRoot'
    Qualify-ClosureVariant -Name 'mui-text-preparse-style-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPreParseStyleRenderRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-preparse-style-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPreParseStyleRenderRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-unicode-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextUnicodeStateRoot'
    Qualify-ClosureVariant -Name 'mui-text-unicode-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextUnicodeStateRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-unicode-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextUnicodeStateRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-inline-color' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextInlineColorRenderRoot'
    Qualify-ClosureVariant -Name 'mui-text-inline-color' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextInlineColorRenderRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-inline-color' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextInlineColorRenderRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-inline-image' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextInlineImageRenderRoot'
    Qualify-ClosureVariant -Name 'mui-text-inline-image' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextInlineImageRenderRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-inline-image' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextInlineImageRenderRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-method-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextMethodRenderRoot'
    Qualify-ClosureVariant -Name 'mui-text-method-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextMethodRenderRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-method-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextMethodRenderRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-method-custom-font-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextMethodCustomFontRenderRoot'
    Qualify-ClosureVariant -Name 'mui-text-method-custom-font-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextMethodCustomFontRenderRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-method-custom-font-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextMethodCustomFontRenderRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-bubble' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBubbleRoot'
    Qualify-ClosureVariant -Name 'mui-area-bubble' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBubbleRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-bubble' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBubbleRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-context-menu' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuRoot'
    Qualify-ClosureVariant -Name 'mui-area-context-menu' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-context-menu' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-context-menu-admission-mg988' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-context-menu-admission-mg988' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-context-menu-admission-mg988' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaContextMenuAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-control-char' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaControlCharRoot'
    Qualify-ClosureVariant -Name 'mui-area-control-char' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaControlCharRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-control-char' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaControlCharRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-control-char-admission-mg989' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaControlCharAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-control-char-admission-mg989' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaControlCharAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-control-char-admission-mg989' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaControlCharAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-cycle-chain' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCycleChainRoot'
    Qualify-ClosureVariant -Name 'mui-area-cycle-chain' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCycleChainRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-cycle-chain' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCycleChainRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-cycle-chain-admission-mg990' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCycleChainAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-cycle-chain-admission-mg990' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCycleChainAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-cycle-chain-admission-mg990' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCycleChainAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-window-relationship' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaWindowRelationshipRoot'
    Qualify-ClosureVariant -Name 'mui-area-window-relationship' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaWindowRelationshipRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-window-relationship' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaWindowRelationshipRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-double-click' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleClickRoot'
    Qualify-ClosureVariant -Name 'mui-area-double-click' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleClickRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-double-click' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleClickRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-double-click-admission-mg991' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleClickAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-double-click-admission-mg991' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleClickAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-double-click-admission-mg991' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleClickAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-double-buffer-admission-mg992' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleBufferAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-double-buffer-admission-mg992' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleBufferAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-double-buffer-admission-mg992' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleBufferAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-floating-admission-mg994' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFloatingAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-floating-admission-mg994' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFloatingAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-floating-admission-mg994' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFloatingAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-short-help-admission-mg995' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-short-help-admission-mg995' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-short-help-admission-mg995' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaShortHelpAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-text-color-admission-mg996' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTextColorAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-text-color-admission-mg996' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTextColorAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-text-color-admission-mg996' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTextColorAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-drag-policy-admission-mg997' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragPolicyAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-drag-policy-admission-mg997' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragPolicyAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-drag-policy-admission-mg997' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragPolicyAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-disappear-policy-admission-mg998' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDisappearPolicyAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-disappear-policy-admission-mg998' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDisappearPolicyAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-disappear-policy-admission-mg998' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDisappearPolicyAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-custom-font-admission-mg999' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-admission-mg999' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-admission-mg999' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-custom-font-runtime-admission-mg1000' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontRuntimeAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-runtime-admission-mg1000' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontRuntimeAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-runtime-admission-mg1000' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontRuntimeAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-geometry-admission-mg1001' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaGeometryAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-geometry-admission-mg1001' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaGeometryAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-geometry-admission-mg1001' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaGeometryAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-fixed-text-admission-mg1002' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFixedTextAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-fixed-text-admission-mg1002' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFixedTextAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-fixed-text-admission-mg1002' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFixedTextAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-render-policy-admission-mg1003' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaRenderPolicyAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-render-policy-admission-mg1003' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaRenderPolicyAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-render-policy-admission-mg1003' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaRenderPolicyAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-layout-policy-admission-mg1004' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaLayoutPolicyAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-layout-policy-admission-mg1004' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaLayoutPolicyAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-layout-policy-admission-mg1004' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaLayoutPolicyAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-control-font-admission-mg1005' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-control-font-admission-mg1005' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-control-font-admission-mg1005' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ControlFontAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-rectangle-bar-title-admission-mg1006' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectangleBarTitleAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-rectangle-bar-title-admission-mg1006' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectangleBarTitleAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-rectangle-bar-title-admission-mg1006' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectangleBarTitleAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-image-font-match-string-admission-mg1007' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStringAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-image-font-match-string-admission-mg1007' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStringAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-image-font-match-string-admission-mg1007' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageFontMatchStringAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-contents-admission-mg1008' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringContentsAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-string-contents-admission-mg1008' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringContentsAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-contents-admission-mg1008' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringContentsAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-copy-admission-mg1009' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextCopyAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-text-copy-admission-mg1009' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextCopyAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-copy-admission-mg1009' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextCopyAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-contents-admission-mg1010' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextContentsAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-text-contents-admission-mg1010' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextContentsAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-contents-admission-mg1010' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextContentsAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-preparse-admission-mg1011' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPreParseAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-text-preparse-admission-mg1011' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPreParseAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-preparse-admission-mg1011' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPreParseAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-placeholder-admission-mg1012' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPlaceholderAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-string-placeholder-admission-mg1012' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPlaceholderAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-placeholder-admission-mg1012' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPlaceholderAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-numeric-format-admission-mg1013' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NumericFormatAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-numeric-format-admission-mg1013' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NumericFormatAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-numeric-format-admission-mg1013' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NumericFormatAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-gauge-info-text-admission-mg1014' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoTextAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-gauge-info-text-admission-mg1014' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoTextAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-gauge-info-text-admission-mg1014' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeInfoTextAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-levelmeter-label-admission-mg1015' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterLabelAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-levelmeter-label-admission-mg1015' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterLabelAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-levelmeter-label-admission-mg1015' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::LevelmeterLabelAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-acknowledge-admission-mg1016' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAcknowledgeAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-string-acknowledge-admission-mg1016' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAcknowledgeAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-acknowledge-admission-mg1016' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAcknowledgeAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-attached-list-admission-mg1017' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAttachedListAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-string-attached-list-admission-mg1017' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAttachedListAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-attached-list-admission-mg1017' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringAttachedListAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-interaction-admission-mg1018' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteractionAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-string-interaction-admission-mg1018' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteractionAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-interaction-admission-mg1018' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteractionAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-presentation-admission-mg1019' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPresentationAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-string-presentation-admission-mg1019' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPresentationAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-presentation-admission-mg1019' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringPresentationAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-spell-checking-admission-mg1020' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringSpellCheckingAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-string-spell-checking-admission-mg1020' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringSpellCheckingAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-spell-checking-admission-mg1020' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringSpellCheckingAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-edit-hook-admission-mg1021' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditHookAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-string-edit-hook-admission-mg1021' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditHookAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-edit-hook-admission-mg1021' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringEditHookAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-filter-admission-mg1022' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringFilterAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-string-filter-admission-mg1022' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringFilterAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-filter-admission-mg1022' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringFilterAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-integer-admission-mg1023' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringIntegerAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-string-integer-admission-mg1023' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringIntegerAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-integer-admission-mg1023' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringIntegerAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-integer64-admission-mg1024' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteger64AdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-string-integer64-admission-mg1024' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteger64AdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-integer64-admission-mg1024' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteger64AdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-cursor-admission-mg1025' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringCursorAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-string-cursor-admission-mg1025' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringCursorAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-cursor-admission-mg1025' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringCursorAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-scroll-metrics-admission-mg1026' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringScrollMetricsAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-string-scroll-metrics-admission-mg1026' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringScrollMetricsAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-scroll-metrics-admission-mg1026' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringScrollMetricsAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-presentation-admission-mg1027' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPresentationAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-text-presentation-admission-mg1027' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPresentationAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-presentation-admission-mg1027' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextPresentationAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-shortened-admission-mg1028' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextShortenedAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-text-shortened-admission-mg1028' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextShortenedAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-shortened-admission-mg1028' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextShortenedAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-unicode-admission-mg1029' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextUnicodeAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-text-unicode-admission-mg1029' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextUnicodeAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-unicode-admission-mg1029' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextUnicodeAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bitmap-geometry-admission-mg1030' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapGeometryAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-bitmap-geometry-admission-mg1030' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapGeometryAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bitmap-geometry-admission-mg1030' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapGeometryAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bitmap-source-admission-mg1031' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapSourceAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-bitmap-source-admission-mg1031' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapSourceAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bitmap-source-admission-mg1031' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BitmapSourceAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-bodychunk-format-admission-mg1032' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BodychunkFormatAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-bodychunk-format-admission-mg1032' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BodychunkFormatAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-bodychunk-format-admission-mg1032' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BodychunkFormatAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-image-old-image-admission-mg1033' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageOldImageAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-image-old-image-admission-mg1033' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageOldImageAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-image-old-image-admission-mg1033' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageOldImageAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-image-render-admission-mg1034' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageRenderAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-image-render-admission-mg1034' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageRenderAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-image-render-admission-mg1034' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageRenderAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-image-spec-admission-mg1035' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageSpecAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-image-spec-admission-mg1035' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageSpecAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-image-spec-admission-mg1035' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ImageSpecAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-register-policy-admission-mg1036' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RegisterPolicyAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-register-policy-admission-mg1036' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RegisterPolicyAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-register-policy-admission-mg1036' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RegisterPolicyAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-headless-state-admission-mg1037' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessStateAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-headless-state-admission-mg1037' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessStateAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-headless-state-admission-mg1037' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessStateAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-control-admission-mg1038' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowControlAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-window-control-admission-mg1038' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowControlAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-control-admission-mg1038' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowControlAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-lifecycle-admission-mg1039' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowLifecycleAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-window-lifecycle-admission-mg1039' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowLifecycleAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-lifecycle-admission-mg1039' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowLifecycleAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-visual-admission-mg1040' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowVisualAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-window-visual-admission-mg1040' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowVisualAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-visual-admission-mg1040' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowVisualAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-presentation-admission-mg1041' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowPresentationAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-window-presentation-admission-mg1041' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowPresentationAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-presentation-admission-mg1041' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowPresentationAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-open-policy-admission-mg1042' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowOpenPolicyAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-window-open-policy-admission-mg1042' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowOpenPolicyAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-open-policy-admission-mg1042' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowOpenPolicyAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-relationship-admission-mg1043' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowRelationshipAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-window-relationship-admission-mg1043' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowRelationshipAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-relationship-admission-mg1043' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowRelationshipAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-focus-admission-mg1044' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowFocusAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-window-focus-admission-mg1044' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowFocusAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-focus-admission-mg1044' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowFocusAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-admission-mg1045' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-window-event-admission-mg1045' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-admission-mg1045' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-interaction-admission-mg1046' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowInteractionAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-window-interaction-admission-mg1046' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowInteractionAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-interaction-admission-mg1046' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowInteractionAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-reuse-admission-mg1047' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventReuseAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-window-event-reuse-admission-mg1047' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventReuseAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-reuse-admission-mg1047' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventReuseAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-lifecycle-admission-mg1048' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationLifecycleAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-application-lifecycle-admission-mg1048' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationLifecycleAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-lifecycle-admission-mg1048' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationLifecycleAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-scheduler-admission-mg1049' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSchedulerAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-application-scheduler-admission-mg1049' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSchedulerAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-scheduler-admission-mg1049' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSchedulerAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-message-routing-admission-mg1050' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMessageRoutingAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-application-message-routing-admission-mg1050' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMessageRoutingAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-message-routing-admission-mg1050' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMessageRoutingAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-identity-admission-mg1051' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIdentityTextAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-application-identity-admission-mg1051' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIdentityTextAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-identity-admission-mg1051' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIdentityTextAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-text-admission-mg1052' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIdentityTextAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-application-text-admission-mg1052' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIdentityTextAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-text-admission-mg1052' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIdentityTextAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-policy-admission-mg1053' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPolicyAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-application-policy-admission-mg1053' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPolicyAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-policy-admission-mg1053' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationPolicyAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-menu-admission-mg1054' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-application-menu-admission-mg1054' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-menu-admission-mg1054' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-commands-admission-mg1055' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCommandsAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-application-commands-admission-mg1055' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCommandsAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-commands-admission-mg1055' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCommandsAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-config-window-admission-mg1056' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationConfigWindowAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-application-config-window-admission-mg1056' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationConfigWindowAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-config-window-admission-mg1056' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationConfigWindowAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-default-config-admission-mg1057' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationDefaultConfigAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-application-default-config-admission-mg1057' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationDefaultConfigAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-default-config-admission-mg1057' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationDefaultConfigAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-help-admission-mg1058' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationHelpAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-application-help-admission-mg1058' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationHelpAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-help-admission-mg1058' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationHelpAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-refresh-admission-mg1059' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationRefreshAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-application-refresh-admission-mg1059' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationRefreshAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-refresh-admission-mg1059' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationRefreshAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-object-admission-mg1060' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationObjectAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-application-object-admission-mg1060' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationObjectAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-object-admission-mg1060' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationObjectAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-set-config-item-admission-mg1061' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSetConfigItemAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-application-set-config-item-admission-mg1061' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSetConfigItemAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-set-config-item-admission-mg1061' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSetConfigItemAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-settings-panel-admission-mg1062' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPanelAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-application-settings-panel-admission-mg1062' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPanelAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-settings-panel-admission-mg1062' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPanelAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-settings-persistence-admission-mg1063' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPersistenceAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-application-settings-persistence-admission-mg1063' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPersistenceAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-settings-persistence-admission-mg1063' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPersistenceAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-used-classes-admission-mg1064' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUsedClassesAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-application-used-classes-admission-mg1064' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUsedClassesAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-used-classes-admission-mg1064' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUsedClassesAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-window-relationship-admission-mg1065' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowRelationshipAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-application-window-relationship-admission-mg1065' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowRelationshipAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-window-relationship-admission-mg1065' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowRelationshipAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-activation-admission-mg1066' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-activation-admission-mg1066' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-activation-admission-mg1066' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-policy-admission-mg1067' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaPolicyAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-policy-admission-mg1067' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaPolicyAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-policy-admission-mg1067' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaPolicyAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-presentation-policy-admission-mg1068' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaPresentationPolicyAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-presentation-policy-admission-mg1068' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaPresentationPolicyAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-presentation-policy-admission-mg1068' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaPresentationPolicyAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-font-admission-mg1069' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-font-admission-mg1069' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-font-admission-mg1069' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-buffer-weight-admission-mg1070' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBufferWeightAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-buffer-weight-admission-mg1070' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBufferWeightAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-buffer-weight-admission-mg1070' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBufferWeightAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-presentation-timer-admission-mg1071' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaPresentationTimerAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-presentation-timer-admission-mg1071' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaPresentationTimerAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-presentation-timer-admission-mg1071' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaPresentationTimerAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-balance-bitmap-admission-mg1072' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BalanceBitmapAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-balance-bitmap-admission-mg1072' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BalanceBitmapAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-balance-bitmap-admission-mg1072' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BalanceBitmapAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-choice-admission-mg1073' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-choice-admission-mg1073' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-choice-admission-mg1073' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-gadget-admission-mg1074' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-gadget-admission-mg1074' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-gadget-admission-mg1074' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GadgetAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-gauge-levelmeter-admission-mg1075' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeLevelmeterAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-gauge-levelmeter-admission-mg1075' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeLevelmeterAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-gauge-levelmeter-admission-mg1075' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GaugeLevelmeterAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-groupgrid-numeric-admission-mg1076' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridNumericAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-groupgrid-numeric-admission-mg1076' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridNumericAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-groupgrid-numeric-admission-mg1076' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridNumericAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-prop-admission-mg1077' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-prop-admission-mg1077' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-prop-admission-mg1077' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PropAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-rectangle-scrollbar-admission-mg1078' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectangleScrollbarAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-rectangle-scrollbar-admission-mg1078' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectangleScrollbarAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-rectangle-scrollbar-admission-mg1078' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RectangleScrollbarAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-selectgroup-scrollgroup-admission-mg1079' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SelectgroupScrollgroupAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-selectgroup-scrollgroup-admission-mg1079' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SelectgroupScrollgroupAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-selectgroup-scrollgroup-admission-mg1079' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SelectgroupScrollgroupAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-slider-scale-admission-mg1080' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SliderScaleAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-slider-scale-admission-mg1080' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SliderScaleAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-slider-scale-admission-mg1080' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SliderScaleAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-sleep-volumelist-admission-mg1081' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SleepVolumelistAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-sleep-volumelist-admission-mg1081' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SleepVolumelistAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-sleep-volumelist-admission-mg1081' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SleepVolumelistAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dirlist-admission-mg1082' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-dirlist-admission-mg1082' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dirlist-admission-mg1082' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-color-admission-mg1083' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-color-admission-mg1083' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-color-admission-mg1083' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-virtgroup-admission-mg1084' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::VirtgroupAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-virtgroup-admission-mg1084' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::VirtgroupAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-virtgroup-admission-mg1084' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::VirtgroupAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-menu-admission-mg1085' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-menu-admission-mg1085' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-menu-admission-mg1085' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-pop-admission-mg1086' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-pop-admission-mg1086' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-pop-admission-mg1086' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-misc-admission-mg1087' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-misc-admission-mg1087' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-misc-admission-mg1087' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-admission-mg1088' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-stringscroll-admission-mg1088' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-admission-mg1088' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-layout-admission-mg1089' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollLayoutAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-stringscroll-layout-admission-mg1089' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollLayoutAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-layout-admission-mg1089' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollLayoutAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-policy-hookpool-click-admission-mg1090' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreePolicyHookPoolClickAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-listtree-policy-hookpool-click-admission-mg1090' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreePolicyHookPoolClickAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-policy-hookpool-click-admission-mg1090' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreePolicyHookPoolClickAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-clickcolumn-surface-lifecycle-admission-mg1091' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeClickColumnSurfaceLifecycleAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-listtree-clickcolumn-surface-lifecycle-admission-mg1091' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeClickColumnSurfaceLifecycleAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-clickcolumn-surface-lifecycle-admission-mg1091' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeClickColumnSurfaceLifecycleAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-header-presentation-admission-mg1092' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeHeaderPresentationAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-listtree-header-presentation-admission-mg1092' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeHeaderPresentationAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-header-presentation-admission-mg1092' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeHeaderPresentationAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-node-snapshot-admission-mg1093' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeNodeSnapshotAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-listtree-node-snapshot-admission-mg1093' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeNodeSnapshotAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-node-snapshot-admission-mg1093' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeNodeSnapshotAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-core-admission-mg1094' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewCoreAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-listview-core-admission-mg1094' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewCoreAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-core-admission-mg1094' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewCoreAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-render-scroller-admission-mg1095' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewRenderScrollerAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-listview-render-scroller-admission-mg1095' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewRenderScrollerAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-render-scroller-admission-mg1095' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewRenderScrollerAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-horizontal-scroller-admission-mg1096' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHorizontalScrollerAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-listview-horizontal-scroller-admission-mg1096' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHorizontalScrollerAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-horizontal-scroller-admission-mg1096' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHorizontalScrollerAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-drag-admission-mg1097' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewDragStateAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-listview-drag-admission-mg1097' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewDragStateAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-drag-admission-mg1097' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewDragStateAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-floattext-policy-admission-mg1098' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FloattextPolicyAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-floattext-policy-admission-mg1098' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FloattextPolicyAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-floattext-policy-admission-mg1098' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FloattextPolicyAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-core-admission-mg1099' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListCoreAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-list-core-admission-mg1099' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListCoreAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-core-admission-mg1099' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListCoreAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-header-edit-admission-mg1100' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHeaderEditAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-list-header-edit-admission-mg1100' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHeaderEditAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-header-edit-admission-mg1100' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHeaderEditAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-column-layout-admission-mg1101' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnLayoutAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-list-column-layout-admission-mg1101' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnLayoutAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-column-layout-admission-mg1101' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnLayoutAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-format-metrics-admission-mg1102' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatMetricsAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-list-format-metrics-admission-mg1102' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatMetricsAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-format-metrics-admission-mg1102' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatMetricsAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-title-policy-admission-mg1103' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTitlePolicyAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-list-title-policy-admission-mg1103' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTitlePolicyAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-title-policy-admission-mg1103' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTitlePolicyAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-active-admission-mg1104' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListActiveAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-list-active-admission-mg1104' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListActiveAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-active-admission-mg1104' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListActiveAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-insert-position-admission-mg1105' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInsertPositionAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-list-insert-position-admission-mg1105' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInsertPositionAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-insert-position-admission-mg1105' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInsertPositionAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-viewport-admission-mg1106' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListViewportAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-list-viewport-admission-mg1106' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListViewportAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-viewport-admission-mg1106' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListViewportAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-redraw-admission-mg1107' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListRedrawAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-list-redraw-admission-mg1107' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListRedrawAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-redraw-admission-mg1107' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListRedrawAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-interaction-click-admission-mg1108' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInteractionClickAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-list-interaction-click-admission-mg1108' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInteractionClickAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-interaction-click-admission-mg1108' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListInteractionClickAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-hook-sort-admission-mg1109' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHookSortAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-list-hook-sort-admission-mg1109' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHookSortAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-hook-sort-admission-mg1109' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListHookSortAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-presentation-admission-mg1110' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPresentationAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-list-presentation-admission-mg1110' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPresentationAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-presentation-admission-mg1110' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPresentationAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-column-visibility-admission-mg1111' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnVisibilityAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-list-column-visibility-admission-mg1111' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnVisibilityAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-column-visibility-admission-mg1111' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnVisibilityAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-column-order-admission-mg1112' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnOrderAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-list-column-order-admission-mg1112' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnOrderAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-column-order-admission-mg1112' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnOrderAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-title-array-admission-mg1113' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTitleArrayAdmissionRoot' `
          -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-list-title-array-admission-mg1113' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTitleArrayAdmissionRoot' `
          -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-title-array-admission-mg1113' `
          -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListTitleArrayAdmissionRoot' `
          -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-double-buffer-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleBufferRenderRoot'
    Qualify-ClosureVariant -Name 'mui-area-double-buffer-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleBufferRenderRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-double-buffer-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDoubleBufferRenderRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-backfill-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBackfillRenderRoot'
    Qualify-ClosureVariant -Name 'mui-area-backfill-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBackfillRenderRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-backfill-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBackfillRenderRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-timer-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerStateRoot'
    Qualify-ClosureVariant -Name 'mui-area-timer-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerStateRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-timer-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerStateRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-timer-event-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerEventStateRoot'
    Qualify-ClosureVariant -Name 'mui-area-timer-event-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerEventStateRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-timer-event-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerEventStateRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-timer-admission-mg993' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-timer-admission-mg993' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-timer-admission-mg993' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaTimerAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-text-dimension-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextDimensionRenderRoot'
    Qualify-ClosureVariant -Name 'mui-text-dimension-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextDimensionRenderRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-text-dimension-render' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TextDimensionRenderRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-builtin-font-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBuiltinFontCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-builtin-font-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBuiltinFontCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-builtin-font-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBuiltinFontCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-builtin-font-admission-mg986' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBuiltinFontAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-builtin-font-admission-mg986' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBuiltinFontAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-builtin-font-admission-mg986' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaBuiltinFontAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-custom-font-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-custom-font-spec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontSpecRoot'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-spec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontSpecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-spec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontSpecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-font-selection-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontSelectionCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-font-selection-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontSelectionCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-font-selection-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontSelectionCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-font-selection-admission-mg987' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontSelectionAdmissionRoot'
    Qualify-ClosureVariant -Name 'mui-area-font-selection-admission-mg987' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontSelectionAdmissionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-font-selection-admission-mg987' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontSelectionAdmissionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-custom-font-runtime-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontRuntimeCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-runtime-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontRuntimeCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-runtime-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontRuntimeCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-custom-font-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-custom-font-metrics' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontMetricsRoot'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-metrics' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontMetricsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-custom-font-metrics' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaCustomFontMetricsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-font-inheritance' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontInheritanceRoot'
    Qualify-ClosureVariant -Name 'mui-area-font-inheritance' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontInheritanceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-font-inheritance' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaFontInheritanceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-menustrip-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowMenustripCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-menustrip-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowMenustripCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-menustrip-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowMenustripCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-visual-state-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowVisualStateCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-visual-state-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowVisualStateCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-visual-state-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowVisualStateCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-handler-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-event-handler-field-struct-mg2297' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerPacketFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-field-struct-mg2297' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerPacketFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-event-handler-field-struct-mg2297' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowEventHandlerPacketFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-cycle-chain-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowCycleChainCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-cycle-chain-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowCycleChainCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-cycle-chain-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowCycleChainCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-cycle-chain-field-struct-mg2296' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowCycleChainPacketFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-cycle-chain-field-struct-mg2296' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowCycleChainPacketFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-cycle-chain-field-struct-mg2296' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowCycleChainPacketFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-menu-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowMenuPacketCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-menu-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowMenuPacketCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-menu-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowMenuPacketCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-menu-field-struct-mg2298' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuPacketFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-menu-field-struct-mg2298' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuPacketFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-menu-field-struct-mg2298' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuPacketFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-method-field-struct-mg2299' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMethodPacketFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-method-field-struct-mg2299' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMethodPacketFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-method-field-struct-mg2299' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMethodPacketFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-basic-field-struct-mg2300' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionBasicFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-collection-basic-field-struct-mg2300' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionBasicFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-basic-field-struct-mg2300' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionBasicFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-record-field-struct-mg2301' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionRecordFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-collection-record-field-struct-mg2301' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionRecordFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-record-field-struct-mg2301' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionRecordFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-edit-field-struct-mg2302' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionEditFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-collection-edit-field-struct-mg2302' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionEditFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-edit-field-struct-mg2302' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionEditFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-surface-field-struct-mg2303' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionSurfaceFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-collection-surface-field-struct-mg2303' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionSurfaceFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-surface-field-struct-mg2303' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionSurfaceFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-advanced-field-struct-mg2304' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionAdvancedFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-collection-advanced-field-struct-mg2304' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionAdvancedFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-advanced-field-struct-mg2304' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionAdvancedFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-field-struct-mg2305' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-field-struct-mg2305' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-field-struct-mg2305' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-presentation-field-struct-mg2306' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreePresentationFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-presentation-field-struct-mg2306' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreePresentationFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-presentation-field-struct-mg2306' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreePresentationFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-horizontal-scroller-field-struct-mg2307' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHorizontalScrollerFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-horizontal-scroller-field-struct-mg2307' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHorizontalScrollerFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-horizontal-scroller-field-struct-mg2307' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHorizontalScrollerFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-horizontal-scroller-drag-field-struct-mg2308' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHorizontalScrollerDragStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-horizontal-scroller-drag-field-struct-mg2308' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHorizontalScrollerDragStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-horizontal-scroller-drag-field-struct-mg2308' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHorizontalScrollerDragStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-scroller-drag-field-struct-mg2309' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewScrollerDragStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-scroller-drag-field-struct-mg2309' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewScrollerDragStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-scroller-drag-field-struct-mg2309' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewScrollerDragStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-child-field-struct-mg2310' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewChildStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-child-field-struct-mg2310' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewChildStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-child-field-struct-mg2310' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewChildStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-click-field-struct-mg2311' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewClickStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-click-field-struct-mg2311' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewClickStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-click-field-struct-mg2311' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewClickStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-interaction-policy-field-struct-mg2312' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewInteractionPolicyFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-interaction-policy-field-struct-mg2312' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewInteractionPolicyFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-interaction-policy-field-struct-mg2312' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewInteractionPolicyFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-selection-signal-field-struct-mg2313' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewSelectionSignalFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-selection-signal-field-struct-mg2313' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewSelectionSignalFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-selection-signal-field-struct-mg2313' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewSelectionSignalFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-layout-field-struct-mg2314' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewLayoutFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-layout-field-struct-mg2314' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewLayoutFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-layout-field-struct-mg2314' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewLayoutFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-render-field-struct-mg2315' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewRenderFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-render-field-struct-mg2315' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewRenderFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-render-field-struct-mg2315' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewRenderFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-external-scroller-connection-field-struct-mg2316' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewExternalScrollerConnectionFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-external-scroller-connection-field-struct-mg2316' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewExternalScrollerConnectionFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-external-scroller-connection-field-struct-mg2316' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewExternalScrollerConnectionFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-scroller-field-struct-mg2317' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewScrollerFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listview-scroller-field-struct-mg2317' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewScrollerFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-scroller-field-struct-mg2317' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewScrollerFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-header-field-struct-mg2318' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeHeaderFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-header-field-struct-mg2318' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeHeaderFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-header-field-struct-mg2318' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeHeaderFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-display-column-field-struct-mg2319' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeDisplayColumnFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-display-column-field-struct-mg2319' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeDisplayColumnFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-display-column-field-struct-mg2319' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeDisplayColumnFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-column-geometry-field-struct-mg2320' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeColumnGeometryFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-column-geometry-field-struct-mg2320' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeColumnGeometryFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-column-geometry-field-struct-mg2320' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeColumnGeometryFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-display-snapshot-field-struct-mg2321' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeDisplaySnapshotFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-display-snapshot-field-struct-mg2321' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeDisplaySnapshotFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-display-snapshot-field-struct-mg2321' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeDisplaySnapshotFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-policy-field-struct-mg2322' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreePolicyFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-policy-field-struct-mg2322' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreePolicyFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-policy-field-struct-mg2322' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreePolicyFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-hook-pool-field-struct-mg2323' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeHookPoolFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-hook-pool-field-struct-mg2323' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeHookPoolFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-hook-pool-field-struct-mg2323' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeHookPoolFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-click-state-field-struct-mg2324' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeClickStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-click-state-field-struct-mg2324' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeClickStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-click-state-field-struct-mg2324' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeClickStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-click-column-field-struct-mg2325' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeClickColumnFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-click-column-field-struct-mg2325' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeClickColumnFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-click-column-field-struct-mg2325' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeClickColumnFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-surface-field-struct-mg2326' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeSurfaceFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-surface-field-struct-mg2326' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeSurfaceFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-surface-field-struct-mg2326' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeSurfaceFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-lifecycle-field-struct-mg2327' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeLifecycleFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-lifecycle-field-struct-mg2327' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeLifecycleFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-lifecycle-field-struct-mg2327' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeLifecycleFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-node-field-struct-mg2328' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeNodeFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-node-field-struct-mg2328' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeNodeFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-node-field-struct-mg2328' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeNodeFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-test-pos-field-struct-mg2329' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeTestPosFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-test-pos-field-struct-mg2329' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeTestPosFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-test-pos-field-struct-mg2329' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeTestPosFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-family-get-child-field-struct-mg2330' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyGetChildFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-family-get-child-field-struct-mg2330' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyGetChildFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-family-get-child-field-struct-mg2330' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyGetChildFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-family-mutation-field-struct-mg2331' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyMutationFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-family-mutation-field-struct-mg2331' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyMutationFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-family-mutation-field-struct-mg2331' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyMutationFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-family-mutation-list-field-struct-mg2332' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyMutationListFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-family-mutation-list-field-struct-mg2332' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyMutationListFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-family-mutation-list-field-struct-mg2332' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyMutationListFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-family-do-child-field-struct-mg2333' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyDoChildMethodsFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-family-do-child-field-struct-mg2333' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyDoChildMethodsFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-family-do-child-field-struct-mg2333' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyDoChildMethodsFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-family-do-child-field-struct-mg2804' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyDoChildMethodsFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-family-do-child-field-struct-mg2804' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyDoChildMethodsFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-family-do-child-field-struct-mg2804' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyDoChildMethodsFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dataspace-field-struct-mg2334' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceMessageFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-dataspace-field-struct-mg2334' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceMessageFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dataspace-field-struct-mg2334' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceMessageFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dataspace-iff-field-struct-mg2335' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceIffFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-dataspace-iff-field-struct-mg2335' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceIffFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dataspace-iff-field-struct-mg2335' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceIffFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-set-as-string-field-struct-mg2336' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SetAsStringFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-set-as-string-field-struct-mg2336' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SetAsStringFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-set-as-string-field-struct-mg2336' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SetAsStringFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-notify-write-field-struct-mg2337' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyWriteFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-notify-write-field-struct-mg2337' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyWriteFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-notify-write-field-struct-mg2337' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyWriteFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-notify-userdata-field-struct-mg2338' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataPacketFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-notify-userdata-field-struct-mg2338' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataPacketFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-notify-userdata-field-struct-mg2338' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataPacketFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-notify-userdata-frame-field-struct-mg2339' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataTraversalFrameFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-notify-userdata-frame-field-struct-mg2339' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataTraversalFrameFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-notify-userdata-frame-field-struct-mg2339' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UserDataTraversalFrameFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-notify-field-struct-mg2340' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyPacketFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-notify-field-struct-mg2340' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyPacketFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-notify-field-struct-mg2340' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyPacketFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-object-persistence-field-struct-mg2341' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ObjectPersistenceFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-object-persistence-field-struct-mg2341' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ObjectPersistenceFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-object-persistence-field-struct-mg2341' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ObjectPersistenceFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-store-field-struct-mg2342' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-store-field-struct-mg2342' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-store-field-struct-mg2342' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StoreFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-misc-panel-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscPanelDispatcherRoot'
    Qualify-ClosureVariant -Name 'mui-misc-panel-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscPanelDispatcherRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-misc-panel-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscPanelDispatcherRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-misc-filepanel-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscFilepanelDispatcherRoot'
    Qualify-ClosureVariant -Name 'mui-misc-filepanel-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscFilepanelDispatcherRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-misc-filepanel-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscFilepanelDispatcherRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-misc-mccprefs-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscMccprefsDispatcherRoot'
    Qualify-ClosureVariant -Name 'mui-misc-mccprefs-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscMccprefsDispatcherRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-misc-mccprefs-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscMccprefsDispatcherRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-misc-mccprefs-config-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscMccprefsConfigDispatcherRoot'
    Qualify-ClosureVariant -Name 'mui-misc-mccprefs-config-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscMccprefsConfigDispatcherRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-misc-mccprefs-config-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscMccprefsConfigDispatcherRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-misc-mccprefs-gadgets-config-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscMccprefsGadgetsConfigDispatcherRoot'
    Qualify-ClosureVariant -Name 'mui-misc-mccprefs-gadgets-config-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscMccprefsGadgetsConfigDispatcherRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-misc-mccprefs-gadgets-config-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscMccprefsGadgetsConfigDispatcherRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-menu-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuDispatcherRoot'
    Qualify-ClosureVariant -Name 'mui-menu-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuDispatcherRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-menu-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuDispatcherRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-process-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessDispatcherRoot'
    Qualify-ClosureVariant -Name 'mui-process-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessDispatcherRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-process-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessDispatcherRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-process-dispatch-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessDispatchMethodHeaderCodecRoot'
    Qualify-ClosureVariant -Name 'mui-process-dispatch-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessDispatchMethodHeaderCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-process-dispatch-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessDispatchMethodHeaderCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-process-method-message-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessMethodMessageHeaderStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-process-method-message-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessMethodMessageHeaderStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-process-method-message-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessMethodMessageHeaderStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-service-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ServiceDispatcherRoot'
    Qualify-ClosureVariant -Name 'mui-service-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ServiceDispatcherRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-service-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ServiceDispatcherRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalDispatcherRoot'
    Qualify-ClosureVariant -Name 'mui-external-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalDispatcherRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalDispatcherRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-object-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalObjectServiceRoot'
    Qualify-ClosureVariant -Name 'mui-external-object-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalObjectServiceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-object-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalObjectServiceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dispose-object-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DisposeObjectServiceRoot'
    Qualify-ClosureVariant -Name 'mui-dispose-object-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DisposeObjectServiceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dispose-object-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DisposeObjectServiceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-make-object-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectServiceRoot'
    Qualify-ClosureVariant -Name 'mui-make-object-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectServiceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-make-object-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectServiceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-make-object-parameter-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectParameterCodecRoot'
    Qualify-ClosureVariant -Name 'mui-make-object-parameter-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectParameterCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-make-object-parameter-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectParameterCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-make-object-parameter-field-cursor-struct-mg2787' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectParameterFieldCursorStructRecordCodecRootMg2787'
    Qualify-ClosureVariant -Name 'mui-make-object-parameter-field-cursor-struct-mg2787' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectParameterFieldCursorStructRecordCodecRootMg2787' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-make-object-parameter-field-cursor-struct-mg2787' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectParameterFieldCursorStructRecordCodecRootMg2787' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-make-object-preparse-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectPreParseStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-make-object-preparse-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectPreParseStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-make-object-preparse-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectPreParseStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-make-object-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectClassNameStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-make-object-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectClassNameStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-make-object-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectClassNameStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollClassNameStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-stringscroll-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollClassNameStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollClassNameStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-settings-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsClassNameStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-settings-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsClassNameStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-settings-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsClassNameStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowClassNameStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-window-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowClassNameStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowClassNameStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupClassNameStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-group-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupClassNameStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupClassNameStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeClassNameStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeClassNameStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeClassNameStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-register-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RegisterClassNameStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-register-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RegisterClassNameStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-register-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::RegisterClassNameStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-selectgroup-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SelectgroupClassNameStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-selectgroup-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SelectgroupClassNameStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-selectgroup-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SelectgroupClassNameStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-scrollgroup-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupClassNameStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-scrollgroup-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupClassNameStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-scrollgroup-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrollgroupClassNameStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-virtgroup-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::VirtgroupClassNameStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-virtgroup-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::VirtgroupClassNameStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-virtgroup-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::VirtgroupClassNameStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dirlist-example-volume-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistExampleVolumeNameStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-dirlist-example-volume-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistExampleVolumeNameStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dirlist-example-volume-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistExampleVolumeNameStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-color-specialist-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorSpecialistClassNameStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-color-specialist-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorSpecialistClassNameStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-color-specialist-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorSpecialistClassNameStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-process-specialist-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistClassNameStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-process-specialist-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistClassNameStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-process-specialist-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistClassNameStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-wrapper-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperClassNameStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-external-wrapper-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperClassNameStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-wrapper-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperClassNameStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-menu-specialist-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuSpecialistClassNameStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-menu-specialist-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuSpecialistClassNameStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-menu-specialist-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuSpecialistClassNameStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-pop-specialist-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopSpecialistClassNameStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-pop-specialist-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopSpecialistClassNameStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-pop-specialist-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopSpecialistClassNameStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-misc-specialist-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscSpecialistClassNameStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-misc-specialist-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscSpecialistClassNameStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-misc-specialist-class-name-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscSpecialistClassNameStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-make-object-generated-tag-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectGeneratedTagCodecRoot'
    Qualify-ClosureVariant -Name 'mui-make-object-generated-tag-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectGeneratedTagCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-make-object-generated-tag-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectGeneratedTagCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-new-menu-record-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NewMenuRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-new-menu-record-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NewMenuRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-new-menu-record-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NewMenuRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-new-menu-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NewMenuVectorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-new-menu-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NewMenuVectorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-new-menu-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NewMenuVectorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-make-object-new-menu-field-cursor-struct-mg2788' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectNewMenuFieldCursorStructRecordCodecRootMg2788'
    Qualify-ClosureVariant -Name 'mui-make-object-new-menu-field-cursor-struct-mg2788' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectNewMenuFieldCursorStructRecordCodecRootMg2788' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-make-object-new-menu-field-cursor-struct-mg2788' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MakeObjectNewMenuFieldCursorStructRecordCodecRootMg2788' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-error-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ErrorServiceRoot'
    Qualify-ClosureVariant -Name 'mui-error-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ErrorServiceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-error-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ErrorServiceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-error-service-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ErrorServiceRecordRoot'
    Qualify-ClosureVariant -Name 'mui-error-service-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ErrorServiceRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-error-service-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ErrorServiceRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-error-service-field-struct-mg2291' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ErrorServiceFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-error-service-field-struct-mg2291' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ErrorServiceFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-error-service-field-struct-mg2291' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ErrorServiceFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-idcmp-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::IdcmpServiceRoot'
    Qualify-ClosureVariant -Name 'mui-idcmp-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::IdcmpServiceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-idcmp-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::IdcmpServiceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-drawing-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DrawingServiceRoot'
    Qualify-ClosureVariant -Name 'mui-drawing-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DrawingServiceRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-drawing-service' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DrawingServiceRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-drawing-service-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DrawingServiceRecordRoot'
    Qualify-ClosureVariant -Name 'mui-drawing-service-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DrawingServiceRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-drawing-service-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DrawingServiceRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-color-specialist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorSpecialistRoot'
    Qualify-ClosureVariant -Name 'mui-color-specialist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorSpecialistRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-color-specialist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorSpecialistRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-color-specialist-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorSpecialistMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-color-specialist-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorSpecialistMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-color-specialist-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorSpecialistMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-color-specialist-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorSpecialistMethodHeaderStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-color-specialist-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorSpecialistMethodHeaderStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-color-specialist-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorSpecialistMethodHeaderStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-pop-specialist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopSpecialistRoot'
    Qualify-ClosureVariant -Name 'mui-pop-specialist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopSpecialistRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-pop-specialist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopSpecialistRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-pop-specialist-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopSpecialistMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-pop-specialist-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopSpecialistMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-pop-specialist-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopSpecialistMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-pop-specialist-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopSpecialistMethodHeaderStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-pop-specialist-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopSpecialistMethodHeaderStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-pop-specialist-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PopSpecialistMethodHeaderStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-poplist-array-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PoplistArrayCodecRoot'
    Qualify-ClosureVariant -Name 'mui-poplist-array-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PoplistArrayCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-poplist-array-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PoplistArrayCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-poplist-array-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PoplistArrayVectorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-poplist-array-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PoplistArrayVectorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-poplist-array-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PoplistArrayVectorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-poplist-array-field-struct-mg2803' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PoplistArrayVectorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-poplist-array-field-struct-mg2803' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PoplistArrayVectorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-poplist-array-field-struct-mg2803' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::PoplistArrayVectorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-menu-specialist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuSpecialistRoot'
    Qualify-ClosureVariant -Name 'mui-menu-specialist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuSpecialistRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-menu-specialist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuSpecialistRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-menu-specialist-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuSpecialistMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-menu-specialist-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuSpecialistMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-menu-specialist-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuSpecialistMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-menu-specialist-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuSpecialistMethodHeaderStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-menu-specialist-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuSpecialistMethodHeaderStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-menu-specialist-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MenuSpecialistMethodHeaderStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-process-specialist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistRoot'
    Qualify-ClosureVariant -Name 'mui-process-specialist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-process-specialist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-process-specialist-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistRecordRoot'
    Qualify-ClosureVariant -Name 'mui-process-specialist-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-process-specialist-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-process-specialist-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-process-specialist-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-process-specialist-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-process-specialist-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistMethodHeaderStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-process-specialist-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistMethodHeaderStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-process-specialist-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ProcessSpecialistMethodHeaderStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-wrapper-specialist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperSpecialistRoot'
    Qualify-ClosureVariant -Name 'mui-external-wrapper-specialist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperSpecialistRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-wrapper-specialist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperSpecialistRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-wrapper-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-external-wrapper-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-wrapper-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-wrapper-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperMethodHeaderStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-external-wrapper-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperMethodHeaderStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-wrapper-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperMethodHeaderStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-wrapper-tagitem-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperTagItemCodecRoot'
    Qualify-ClosureVariant -Name 'mui-external-wrapper-tagitem-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperTagItemCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-wrapper-tagitem-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalWrapperTagItemCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-boopsi-packet-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiPacketCodecRoot'
    Qualify-ClosureVariant -Name 'mui-external-boopsi-packet-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiPacketCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-boopsi-packet-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiPacketCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dirlist-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-dirlist-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dirlist-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dirlist-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistMethodHeaderCodecRoot'
    Qualify-ClosureVariant -Name 'mui-dirlist-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistMethodHeaderCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dirlist-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistMethodHeaderCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-misc-specialist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscSpecialistRoot'
    Qualify-ClosureVariant -Name 'mui-misc-specialist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscSpecialistRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-misc-specialist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscSpecialistRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-title-page-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TitlePageVectorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-title-page-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TitlePageVectorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-title-page-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::TitlePageVectorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-misc-mccprefs-registry-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MccprefsRegistryVectorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-misc-mccprefs-registry-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MccprefsRegistryVectorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-misc-mccprefs-registry-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MccprefsRegistryVectorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-misc-scrmodelist-mode-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrmodelistModeVectorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-misc-scrmodelist-mode-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrmodelistModeVectorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-misc-scrmodelist-mode-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ScrmodelistModeVectorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-misc-filepanel-row-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FilepanelRowVectorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-misc-filepanel-row-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FilepanelRowVectorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-misc-filepanel-row-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FilepanelRowVectorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-display-column-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeDisplayColumnVectorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-display-column-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeDisplayColumnVectorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-display-column-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeDisplayColumnVectorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-column-geometry-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeColumnGeometryVectorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-column-geometry-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeColumnGeometryVectorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-column-geometry-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeColumnGeometryVectorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-format-descriptor-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatDescriptorVectorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-format-descriptor-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatDescriptorVectorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-format-descriptor-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListFormatDescriptorVectorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-taglist-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalTagListVectorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-external-taglist-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalTagListVectorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-taglist-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalTagListVectorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-remember-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalRememberVectorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-external-remember-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalRememberVectorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-remember-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalRememberVectorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-external-boopsi-tag-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiTagVectorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-external-boopsi-tag-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiTagVectorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-external-boopsi-tag-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ExternalBoopsiTagVectorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-misc-service-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscServiceDispatcherRoot'
    Qualify-ClosureVariant -Name 'mui-misc-service-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscServiceDispatcherRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-misc-service-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscServiceDispatcherRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-misc-setup-cleanup' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscSetupCleanupRoot'
    Qualify-ClosureVariant -Name 'mui-misc-setup-cleanup' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscSetupCleanupRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-misc-setup-cleanup' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::MiscSetupCleanupRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCoreRoot'
    Qualify-ClosureVariant -Name 'mui-application' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCoreRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCoreRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-sleep' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSleepRoot'
    Qualify-ClosureVariant -Name 'mui-application-sleep' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSleepRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-sleep' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSleepRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-iconified' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIconifiedRoot'
    Qualify-ClosureVariant -Name 'mui-application-iconified' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIconifiedRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-iconified' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIconifiedRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-active' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationActiveRoot'
    Qualify-ClosureVariant -Name 'mui-application-active' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationActiveRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-active' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationActiveRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-single-task' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSingleTaskRoot'
    Qualify-ClosureVariant -Name 'mui-application-single-task' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSingleTaskRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-single-task' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSingleTaskRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-force-quit' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationForceQuitRoot'
    Qualify-ClosureVariant -Name 'mui-application-force-quit' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationForceQuitRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-force-quit' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationForceQuitRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-use-rexx' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUseRexxRoot'
    Qualify-ClosureVariant -Name 'mui-application-use-rexx' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUseRexxRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-use-rexx' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUseRexxRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-use-commodities' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUseCommoditiesRoot'
    Qualify-ClosureVariant -Name 'mui-application-use-commodities' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUseCommoditiesRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-use-commodities' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUseCommoditiesRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-window-list' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowListRoot'
    Qualify-ClosureVariant -Name 'mui-application-window-list' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowListRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-window-list' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowListRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-commands' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCommandsRoot'
    Qualify-ClosureVariant -Name 'mui-application-commands' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCommandsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-commands' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCommandsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-app-message' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AppMessageRoot'
    Qualify-ClosureVariant -Name 'mui-application-app-message' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AppMessageRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-app-message' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AppMessageRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-window' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowWindowRoot'
    Qualify-ClosureVariant -Name 'mui-window-window' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowWindowRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-window' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowWindowRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-id' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowIdRoot'
    Qualify-ClosureVariant -Name 'mui-window-id' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowIdRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-id' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowIdRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-close-request' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowCloseRequestRoot'
    Qualify-ClosureVariant -Name 'mui-window-close-request' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowCloseRequestRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-close-request' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowCloseRequestRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-root-object' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowRootObjectRoot'
    Qualify-ClosureVariant -Name 'mui-window-root-object' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowRootObjectRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-root-object' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowRootObjectRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-no-menus' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowNoMenusRoot'
    Qualify-ClosureVariant -Name 'mui-window-no-menus' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowNoMenusRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-no-menus' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowNoMenusRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-has-alpha' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowHasAlphaRoot'
    Qualify-ClosureVariant -Name 'mui-window-has-alpha' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowHasAlphaRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-has-alpha' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowHasAlphaRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-title' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowTitleRoot'
    Qualify-ClosureVariant -Name 'mui-window-title' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowTitleRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-title' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowTitleRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-screen-title' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowScreenTitleRoot'
    Qualify-ClosureVariant -Name 'mui-window-screen-title' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowScreenTitleRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-screen-title' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowScreenTitleRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-public-screen' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowPublicScreenRoot'
    Qualify-ClosureVariant -Name 'mui-window-public-screen' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowPublicScreenRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-public-screen' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowPublicScreenRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-screen' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowScreenRoot'
    Qualify-ClosureVariant -Name 'mui-window-screen' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowScreenRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-screen' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowScreenRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-ref-window' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowRefWindowRoot'
    Qualify-ClosureVariant -Name 'mui-window-ref-window' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowRefWindowRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-ref-window' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowRefWindowRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-visible-on-maximize' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowVisibleOnMaximizeRoot'
    Qualify-ClosureVariant -Name 'mui-window-visible-on-maximize' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowVisibleOnMaximizeRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-visible-on-maximize' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowVisibleOnMaximizeRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-is-sub-window' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowIsSubWindowRoot'
    Qualify-ClosureVariant -Name 'mui-window-is-sub-window' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowIsSubWindowRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-is-sub-window' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowIsSubWindowRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-tablet-messages' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowTabletMessagesRoot'
    Qualify-ClosureVariant -Name 'mui-window-tablet-messages' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowTabletMessagesRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-tablet-messages' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowTabletMessagesRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-border-scrollers' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowBorderScrollersRoot'
    Qualify-ClosureVariant -Name 'mui-window-border-scrollers' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowBorderScrollersRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-border-scrollers' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowBorderScrollersRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-alternate-geometry' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowAlternateGeometryRoot'
    Qualify-ClosureVariant -Name 'mui-window-alternate-geometry' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowAlternateGeometryRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-alternate-geometry' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowAlternateGeometryRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-geometry' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowGeometryRoot'
    Qualify-ClosureVariant -Name 'mui-window-geometry' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowGeometryRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-geometry' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowGeometryRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-gadget-policy' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowGadgetPolicyRoot'
    Qualify-ClosureVariant -Name 'mui-window-gadget-policy' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowGadgetPolicyRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-gadget-policy' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowGadgetPolicyRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-window-mode-policy' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowModePolicyRoot' `
        -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-window-mode-policy' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowModePolicyRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-window-mode-policy' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::WindowModePolicyRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-identity-strings' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIdentityStringsRoot'
    Qualify-ClosureVariant -Name 'mui-application-identity-strings' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIdentityStringsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-identity-strings' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIdentityStringsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-window-initializer' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowInitializerRoot'
    Qualify-ClosureVariant -Name 'mui-application-window-initializer' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowInitializerRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-window-initializer' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowInitializerRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-used-classes' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUsedClassesRoot'
    Qualify-ClosureVariant -Name 'mui-application-used-classes' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUsedClassesRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-used-classes' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUsedClassesRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-used-classes-vector-entry-struct-mg2146' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUsedClassesVectorEntryStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-application-used-classes-vector-entry-struct-mg2146' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUsedClassesVectorEntryStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-used-classes-vector-entry-struct-mg2146' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUsedClassesVectorEntryStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-integer64-field-struct-mg2147' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteger64StructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-string-integer64-field-struct-mg2147' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteger64StructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-integer64-field-struct-mg2147' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringInteger64StructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-iconify-title' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIconifyTitleRoot'
    Qualify-ClosureVariant -Name 'mui-application-iconify-title' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIconifyTitleRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-iconify-title' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationIconifyTitleRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-use-screen-notify' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUseScreenNotifyRoot'
    Qualify-ClosureVariant -Name 'mui-application-use-screen-notify' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUseScreenNotifyRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-use-screen-notify' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationUseScreenNotifyRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-disk-object' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationDiskObjectRoot'
    Qualify-ClosureVariant -Name 'mui-application-disk-object' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationDiskObjectRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-disk-object' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationDiskObjectRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-drop-object' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationDropObjectRoot'
    Qualify-ClosureVariant -Name 'mui-application-drop-object' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationDropObjectRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-drop-object' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationDropObjectRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-menu-event-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuEventStateRoot'
    Qualify-ClosureVariant -Name 'mui-application-menu-event-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuEventStateRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-menu-event-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuEventStateRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-menu-transport' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuTransportRoot'
    Qualify-ClosureVariant -Name 'mui-application-menu-transport' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuTransportRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-menu-transport' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuTransportRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-aboutmui' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationAboutMuiRoot'
    Qualify-ClosureVariant -Name 'mui-application-aboutmui' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationAboutMuiRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-aboutmui' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationAboutMuiRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-showhelp' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationShowHelpRoot'
    Qualify-ClosureVariant -Name 'mui-application-showhelp' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationShowHelpRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-showhelp' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationShowHelpRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-loop' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationLoopRoot'
    Qualify-ClosureVariant -Name 'mui-application-loop' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationLoopRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-loop' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationLoopRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-defaultconfig' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationDefaultConfigRoot'
    Qualify-ClosureVariant -Name 'mui-application-defaultconfig' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationDefaultConfigRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-defaultconfig' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationDefaultConfigRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-setconfigitem' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSetConfigItemRoot'
    Qualify-ClosureVariant -Name 'mui-application-setconfigitem' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSetConfigItemRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-setconfigitem' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSetConfigItemRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-setconfigitem-dispatch' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSetConfigItemDispatchRoot'
    Qualify-ClosureVariant -Name 'mui-application-setconfigitem-dispatch' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSetConfigItemDispatchRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-setconfigitem-dispatch' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSetConfigItemDispatchRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-change' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupChangeRecordRoot'
    Qualify-ClosureVariant -Name 'mui-group-change' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupChangeRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-change' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupChangeRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-change-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupChangeStateRecordRoot'
    Qualify-ClosureVariant -Name 'mui-group-change-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupChangeStateRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-change-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupChangeStateRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-change-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupChangePacketsRoot'
    Qualify-ClosureVariant -Name 'mui-group-change-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupChangePacketsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-change-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupChangePacketsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-ordering' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupOrderingRecordRoot'
    Qualify-ClosureVariant -Name 'mui-group-ordering' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupOrderingRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-ordering' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupOrderingRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-grid' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridRecordRoot'
    Qualify-ClosureVariant -Name 'mui-group-grid' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-grid' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-grid-spec-field-struct-mg2293' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridSpecFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-group-grid-spec-field-struct-mg2293' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridSpecFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-grid-spec-field-struct-mg2293' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupGridSpecFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-sleep-state-field-struct-mg2294' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SleepStateFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-sleep-state-field-struct-mg2294' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SleepStateFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-sleep-state-field-struct-mg2294' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SleepStateFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-page' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupPageRecordRoot'
    Qualify-ClosureVariant -Name 'mui-group-page' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupPageRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-page' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupPageRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-forward' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupForwardRecordRoot'
    Qualify-ClosureVariant -Name 'mui-group-forward' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupForwardRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-forward' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupForwardRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupStateRecordRoot'
    Qualify-ClosureVariant -Name 'mui-group-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupStateRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupStateRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-child-list' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupChildListRoot'
    Qualify-ClosureVariant -Name 'mui-group-child-list' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupChildListRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-child-list' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupChildListRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-record-field-cursor-struct-mg2785' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupRecordFieldCursorStructRecordCodecRootMg2785'
    Qualify-ClosureVariant -Name 'mui-group-record-field-cursor-struct-mg2785' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupRecordFieldCursorStructRecordCodecRootMg2785' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-record-field-cursor-struct-mg2785' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupRecordFieldCursorStructRecordCodecRootMg2785' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-layout-hook' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutHookRoot'
    Qualify-ClosureVariant -Name 'mui-group-layout-hook' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutHookRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-layout-hook' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupLayoutHookRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-family-get-child' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyGetChildRoot'
    Qualify-ClosureVariant -Name 'mui-family-get-child' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyGetChildRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-family-get-child' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyGetChildRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-family-get-child-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyGetChildMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-family-get-child-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyGetChildMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-family-get-child-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyGetChildMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-family-get-child-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyGetChildMethodHeaderStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-family-get-child-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyGetChildMethodHeaderStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-family-get-child-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyGetChildMethodHeaderStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-family-child-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyChildPacketsRoot'
    Qualify-ClosureVariant -Name 'mui-family-child-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyChildPacketsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-family-child-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyChildPacketsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-family-mutation-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyMutationMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-family-mutation-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyMutationMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-family-mutation-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyMutationMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-family-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyMethodHeaderStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-family-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyMethodHeaderStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-family-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyMethodHeaderStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-family-projection-list-vector-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyProjectionListVectorCodecRoot'
    Qualify-ClosureVariant -Name 'mui-family-projection-list-vector-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyProjectionListVectorCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-family-projection-list-vector-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyProjectionListVectorCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-family-mutation-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyMutationVectorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-family-mutation-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyMutationVectorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-family-mutation-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyMutationVectorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-store-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StorePacketsRoot'
    Qualify-ClosureVariant -Name 'mui-store-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StorePacketsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-store-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StorePacketsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-family-do-child-methods' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyDoChildMethodsRoot'
    Qualify-ClosureVariant -Name 'mui-family-do-child-methods' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyDoChildMethodsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-family-do-child-methods' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyDoChildMethodsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-family-do-child-methods-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyDoChildMethodsMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-family-do-child-methods-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyDoChildMethodsMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-family-do-child-methods-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyDoChildMethodsMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-family-do-child-methods-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyDoChildMethodsMethodHeaderStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-family-do-child-methods-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyDoChildMethodsMethodHeaderStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-family-do-child-methods-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::FamilyDoChildMethodsMethodHeaderStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-call-hook-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CallHookPacketsRoot'
    Qualify-ClosureVariant -Name 'mui-call-hook-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CallHookPacketsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-call-hook-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CallHookPacketsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-call-hook-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CallHookMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-call-hook-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CallHookMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-call-hook-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CallHookMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dataspace-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspacePacketsRoot'
    Qualify-ClosureVariant -Name 'mui-dataspace-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspacePacketsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dataspace-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspacePacketsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dataspace-iff-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceIffPacketsRoot'
    Qualify-ClosureVariant -Name 'mui-dataspace-iff-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceIffPacketsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dataspace-iff-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceIffPacketsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dataspace-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-dataspace-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dataspace-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dataspace-iff-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceIffMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-dataspace-iff-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceIffMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dataspace-iff-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceIffMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dataspace-iff-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceIffMethodHeaderCodecRoot'
    Qualify-ClosureVariant -Name 'mui-dataspace-iff-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceIffMethodHeaderCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dataspace-iff-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DataspaceIffMethodHeaderCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-notify-write-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyWritePacketsRoot'
    Qualify-ClosureVariant -Name 'mui-notify-write-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyWritePacketsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-notify-write-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyWritePacketsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-notify-write-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyWriteMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-notify-write-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyWriteMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-notify-write-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyWriteMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-notify-write-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyWriteMethodHeaderCodecRoot'
    Qualify-ClosureVariant -Name 'mui-notify-write-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyWriteMethodHeaderCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-notify-write-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyWriteMethodHeaderCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-notify-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyMethodHeaderStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-notify-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyMethodHeaderStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-notify-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::NotifyMethodHeaderStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-notify-setasstring-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SetAsStringPacketsRoot'
    Qualify-ClosureVariant -Name 'mui-notify-setasstring-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SetAsStringPacketsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-notify-setasstring-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::SetAsStringPacketsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-boopsi-query-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BoopsiQueryPacketsRoot'
    Qualify-ClosureVariant -Name 'mui-boopsi-query-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BoopsiQueryPacketsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-boopsi-query-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BoopsiQueryPacketsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-boopsi-query-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BoopsiQueryMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-boopsi-query-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BoopsiQueryMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-boopsi-query-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BoopsiQueryMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-boopsi-query-field-struct-mg2289' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BoopsiQueryFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-boopsi-query-field-struct-mg2289' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BoopsiQueryFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-boopsi-query-field-struct-mg2289' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::BoopsiQueryFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-update-config-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UpdateConfigPacketsRoot'
    Qualify-ClosureVariant -Name 'mui-update-config-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UpdateConfigPacketsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-update-config-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UpdateConfigPacketsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-update-config-table-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UpdateConfigTableVectorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-update-config-table-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UpdateConfigTableVectorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-update-config-table-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UpdateConfigTableVectorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-update-config-method-header-ulong-struct' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UpdateConfigMethodHeaderCodecRoot'
    Qualify-ClosureVariant -Name 'mui-update-config-method-header-ulong-struct' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UpdateConfigMethodHeaderCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-update-config-method-header-ulong-struct' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::UpdateConfigMethodHeaderCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-openconfig' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationOpenConfigRoot'
    Qualify-ClosureVariant -Name 'mui-application-openconfig' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationOpenConfigRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-openconfig' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationOpenConfigRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-settings-panel' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPanelRoot'
    Qualify-ClosureVariant -Name 'mui-application-settings-panel' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPanelRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-settings-panel' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPanelRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-settings-io' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsIORoot'
    Qualify-ClosureVariant -Name 'mui-application-settings-io' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsIORoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-settings-io' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsIORoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-settings-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPacketRoot'
    Qualify-ClosureVariant -Name 'mui-application-settings-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPacketRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-settings-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationSettingsPacketRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-checkrefresh' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCheckRefreshRoot'
    Qualify-ClosureVariant -Name 'mui-application-checkrefresh' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCheckRefreshRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-checkrefresh' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationCheckRefreshRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-menu' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuRoot'
    Qualify-ClosureVariant -Name 'mui-application-menu' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-menu' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationMenuRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-queue' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationQueueRoot'
    Qualify-ClosureVariant -Name 'mui-application-queue' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationQueueRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-queue' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationQueueRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-application-window-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowRecordRoot'
    Qualify-ClosureVariant -Name 'mui-application-window-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-application-window-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ApplicationWindowRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-layout-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaLayoutRecordRoot'
    Qualify-ClosureVariant -Name 'mui-area-layout-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaLayoutRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-layout-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaLayoutRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-commoncontrol' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlRoot'
    Qualify-ClosureVariant -Name 'mui-commoncontrol' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-commoncontrol' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-commoncontrol-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlPacketsRoot'
    Qualify-ClosureVariant -Name 'mui-commoncontrol-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlPacketsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-commoncontrol-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlPacketsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-commoncontrol-numeric-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlNumericPacketsRoot'
    Qualify-ClosureVariant -Name 'mui-commoncontrol-numeric-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlNumericPacketsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-commoncontrol-numeric-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlNumericPacketsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-commoncontrol-numeric-toggle-default' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlNumericToggleDefaultRoot'
    Qualify-ClosureVariant -Name 'mui-commoncontrol-numeric-toggle-default' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlNumericToggleDefaultRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-commoncontrol-numeric-toggle-default' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlNumericToggleDefaultRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-commoncontrol-class-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlClassRecordRoot'
    Qualify-ClosureVariant -Name 'mui-commoncontrol-class-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlClassRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-commoncontrol-class-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlClassRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-commoncontrol-attribute-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlAttributePacketsRoot'
    Qualify-ClosureVariant -Name 'mui-commoncontrol-attribute-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlAttributePacketsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-commoncontrol-attribute-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlAttributePacketsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-commoncontrol-event-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlEventPacketRoot'
    Qualify-ClosureVariant -Name 'mui-commoncontrol-event-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlEventPacketRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-commoncontrol-event-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlEventPacketRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-commoncontrol-geometry-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlGeometryPacketRoot'
    Qualify-ClosureVariant -Name 'mui-commoncontrol-geometry-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlGeometryPacketRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-commoncontrol-geometry-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlGeometryPacketRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-commoncontrol-render-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlRenderPacketsRoot'
    Qualify-ClosureVariant -Name 'mui-commoncontrol-render-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlRenderPacketsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-commoncontrol-render-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlRenderPacketsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-scroll-attributes' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringScrollAttributeRoot'
    Qualify-ClosureVariant -Name 'mui-string-scroll-attributes' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringScrollAttributeRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-scroll-attributes' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringScrollAttributeRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-scroll-attributes-utf8-metrics' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringScrollAttributeUtf8MetricsRoot'
    Qualify-ClosureVariant -Name 'mui-string-scroll-attributes-utf8-metrics' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringScrollAttributeUtf8MetricsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-scroll-attributes-utf8-metrics' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringScrollAttributeUtf8MetricsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-multiline-contents-visibility' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringMultilineContentsVisibilityRoot'
    Qualify-ClosureVariant -Name 'mui-string-multiline-contents-visibility' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringMultilineContentsVisibilityRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-multiline-contents-visibility' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringMultilineContentsVisibilityRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionCoreRoot'
    Qualify-ClosureVariant -Name 'mui-collection' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionCoreRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionCoreRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListPacketRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListPacketRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListPacketRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-advanced-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListAdvancedPacketRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-advanced-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListAdvancedPacketRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-advanced-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListAdvancedPacketRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-advanced-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListAdvancedMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-advanced-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListAdvancedMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-advanced-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListAdvancedMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-advanced-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionAdvancedMethodHeaderStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-advanced-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionAdvancedMethodHeaderStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-advanced-method-header-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionAdvancedMethodHeaderStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-basic-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListBasicMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-basic-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListBasicMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-basic-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListBasicMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-surface-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionSurfaceMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-collection-surface-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionSurfaceMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-surface-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionSurfaceMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-handle-input' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHandleInputRoot'
    Qualify-ClosureVariant -Name 'mui-listview-handle-input' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHandleInputRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-handle-input' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewHandleInputRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-selection-input' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewSelectionInputRoot'
    Qualify-ClosureVariant -Name 'mui-listview-selection-input' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewSelectionInputRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-selection-input' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewSelectionInputRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-drag-cancel' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewDragCancelRoot'
    Qualify-ClosureVariant -Name 'mui-listview-drag-cancel' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewDragCancelRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-drag-cancel' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewDragCancelRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-drag-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-drag-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-drag-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-drag-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragStateRoot'
    Qualify-ClosureVariant -Name 'mui-area-drag-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragStateRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-drag-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaDragStateRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-area-activation-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-area-activation-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-area-activation-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::AreaActivationMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-pointer-input' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewPointerInputRoot'
    Qualify-ClosureVariant -Name 'mui-listview-pointer-input' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewPointerInputRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-pointer-input' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewPointerInputRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listview-drag-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewDragStateRoot'
    Qualify-ClosureVariant -Name 'mui-listview-drag-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewDragStateRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listview-drag-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListviewDragStateRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-record-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListRecordPacketRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-record-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListRecordPacketRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-record-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListRecordPacketRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-record-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListRecordMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-record-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListRecordMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-record-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListRecordMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-surface-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListSurfacePacketRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-surface-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListSurfacePacketRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-surface-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListSurfacePacketRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-composite-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionCompositePacketsRoot'
    Qualify-ClosureVariant -Name 'mui-collection-composite-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionCompositePacketsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-composite-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionCompositePacketsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-edit-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListEditPacketRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-edit-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListEditPacketRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-edit-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListEditPacketRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-edit-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListEditMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-edit-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListEditMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-edit-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListEditMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-edit-commit' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListEditCommitRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-edit-commit' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListEditCommitRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-edit-commit' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListEditCommitRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-edit-stringarray' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListEditStringArrayRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-edit-stringarray' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListEditStringArrayRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-edit-stringarray' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListEditStringArrayRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-edit-placement' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListEditPlacementRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-edit-placement' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListEditPlacementRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-edit-placement' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListEditPlacementRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-title-array' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListTitleArrayRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-title-array' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListTitleArrayRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-title-array' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListTitleArrayRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-pointer-slot-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPointerSlotCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-pointer-slot-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPointerSlotCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-pointer-slot-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPointerSlotCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-pointer-vector-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPointerVectorCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-pointer-vector-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPointerVectorCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-pointer-vector-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListPointerVectorCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-adjust-height' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListAdjustHeightRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-adjust-height' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListAdjustHeightRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-adjust-height' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListAdjustHeightRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-adjust-width' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListAdjustWidthRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-adjust-width' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListAdjustWidthRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-adjust-width' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListAdjustWidthRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-stripes' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListStripesRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-stripes' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListStripesRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-stripes' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListStripesRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-drop-mark' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListDropMarkRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-drop-mark' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListDropMarkRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-drop-mark' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListDropMarkRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-drag' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListDragRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-drag' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListDragRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-drag' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListDragRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-auto-visible' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListAutoVisibleRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-auto-visible' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListAutoVisibleRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-auto-visible' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListAutoVisibleRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-sort-column' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListSortColumnRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-sort-column' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListSortColumnRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-sort-column' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListSortColumnRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-listview-forwarding' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListviewForwardingRoot'
    Qualify-ClosureVariant -Name 'mui-collection-listview-forwarding' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListviewForwardingRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-listview-forwarding' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListviewForwardingRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-quiet' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListQuietRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-quiet' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListQuietRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-quiet' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListQuietRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-viewport-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListViewportRecordRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-viewport-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListViewportRecordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-viewport-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListViewportRecordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-format' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-format' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-format' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-format-col' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatColRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-format-col' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatColRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-format-col' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatColRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-format-bar' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatBarRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-format-bar' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatBarRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-format-bar' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatBarRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-format-preparse' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatPreparseRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-format-preparse' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatPreparseRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-format-preparse' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatPreparseRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-format-minus-one' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatMinusOneRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-format-minus-one' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatMinusOneRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-format-minus-one' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatMinusOneRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-format-quoted' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatQuotedRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-format-quoted' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatQuotedRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-format-quoted' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatQuotedRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-format-escapes' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatEscapesRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-format-escapes' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatEscapesRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-format-escapes' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatEscapesRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-format-keyword' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatKeywordRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-format-keyword' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatKeywordRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-format-keyword' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatKeywordRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-format-weight' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatWeightRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-format-weight' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatWeightRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-format-weight' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatWeightRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-format-errors' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatErrorsRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-format-errors' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatErrorsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-format-errors' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatErrorsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-format-minimum' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatMinimumRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-format-minimum' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatMinimumRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-format-minimum' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListFormatMinimumRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-column-visibility' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListColumnVisibilityRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-column-visibility' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListColumnVisibilityRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-list-column-order' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListColumnOrderRoot'
    Qualify-ClosureVariant -Name 'mui-collection-list-column-order' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListColumnOrderRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-list-column-order' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListColumnOrderRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-column-order-byte-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnOrderByteCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-column-order-byte-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnOrderByteCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-column-order-byte-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnOrderByteCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-list-column-order-byte-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnOrderByteVectorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-list-column-order-byte-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnOrderByteVectorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-list-column-order-byte-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListColumnOrderByteVectorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-choice-entry-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceEntryVectorStructCodecRoot'
    Qualify-ClosureVariant -Name 'mui-choice-entry-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceEntryVectorStructCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-choice-entry-vector-struct-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ChoiceEntryVectorStructCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-common-control-field-struct-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-common-control-field-struct-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-common-control-field-struct-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-choice-entry-field-struct-mg2802' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-choice-entry-field-struct-mg2802' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-choice-entry-field-struct-mg2802' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-common-control-packet-field-cursor-struct-mg2784' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlPacketFieldCursorStructRecordCodecRootMg2784'
    Qualify-ClosureVariant -Name 'mui-common-control-packet-field-cursor-struct-mg2784' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlPacketFieldCursorStructRecordCodecRootMg2784' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-common-control-packet-field-cursor-struct-mg2784' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CommonControlPacketFieldCursorStructRecordCodecRootMg2784' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-group-change-field-struct-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupChangeFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-group-change-field-struct-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupChangeFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-group-change-field-struct-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::GroupChangeFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-color-field-struct-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-color-field-struct-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-color-field-struct-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ColorFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dirlist-byte-total-field-struct-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistByteTotalFieldStructRecordCodecRoot'
    Qualify-ClosureVariant -Name 'mui-dirlist-byte-total-field-struct-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistByteTotalFieldStructRecordCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dirlist-byte-total-field-struct-record' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::DirlistByteTotalFieldStructRecordCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-headless-class-field-struct-adapter-mg2347' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessClassFieldStructAdapterRoot'
    Qualify-ClosureVariant -Name 'mui-headless-class-field-struct-adapter-mg2347' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessClassFieldStructAdapterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-headless-class-field-struct-adapter-mg2347' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessClassFieldStructAdapterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-headless-state-field-struct-adapter-mg2348' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessStatePacketRoot'
    Qualify-ClosureVariant -Name 'mui-headless-state-field-struct-adapter-mg2348' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessStatePacketRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-headless-state-field-struct-adapter-mg2348' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::HeadlessStatePacketRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-listview-click-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListviewClickStateRoot'
    Qualify-ClosureVariant -Name 'mui-collection-listview-click-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListviewClickStateRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-listview-click-state' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListviewClickStateRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-collection-image' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionImageRoot'
    Qualify-ClosureVariant -Name 'mui-collection-image' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionImageRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-collection-image' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionImageRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-hookabi' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionHookAbiRoot'
    Qualify-ClosureVariant -Name 'mui-hookabi' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionHookAbiRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-hookabi' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionHookAbiRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dirlist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionDirlistRoot'
    Qualify-ClosureVariant -Name 'mui-dirlist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionDirlistRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dirlist' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionDirlistRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-dirlist-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionDirlistPacketRoot'
    Qualify-ClosureVariant -Name 'mui-dirlist-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionDirlistPacketRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-dirlist-packets' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionDirlistPacketRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionStringscrollRoot'
    Qualify-ClosureVariant -Name 'mui-stringscroll' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionStringscrollRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionStringscrollRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-stringscroll-utf8-metrics' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollUtf8MetricsRoot'
    Qualify-ClosureVariant -Name 'mui-stringscroll-utf8-metrics' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollUtf8MetricsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-stringscroll-utf8-metrics' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringscrollUtf8MetricsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-utf8-cursor' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringUtf8CursorRoot'
    Qualify-ClosureVariant -Name 'mui-string-utf8-cursor' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringUtf8CursorRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-utf8-cursor' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringUtf8CursorRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-utf8-input' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringUtf8InputRoot'
    Qualify-ClosureVariant -Name 'mui-string-utf8-input' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringUtf8InputRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-utf8-input' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringUtf8InputRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-translated-tab' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringTranslatedTabInputRoot'
    Qualify-ClosureVariant -Name 'mui-string-translated-tab' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringTranslatedTabInputRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-translated-tab' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringTranslatedTabInputRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-word-navigation' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringWordNavigationRoot'
    Qualify-ClosureVariant -Name 'mui-string-word-navigation' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringWordNavigationRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-word-navigation' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringWordNavigationRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-multiline-page-navigation' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringMultilinePageNavigationRoot'
    Qualify-ClosureVariant -Name 'mui-string-multiline-page-navigation' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringMultilinePageNavigationRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-multiline-page-navigation' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringMultilinePageNavigationRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-multiline-boundary-navigation' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringMultilineBoundaryNavigationRoot'
    Qualify-ClosureVariant -Name 'mui-string-multiline-boundary-navigation' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringMultilineBoundaryNavigationRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-multiline-boundary-navigation' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringMultilineBoundaryNavigationRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-utf8-filter' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringUtf8FilterRoot'
    Qualify-ClosureVariant -Name 'mui-string-utf8-filter' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringUtf8FilterRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-utf8-filter' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringUtf8FilterRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-string-utf8-position' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringUtf8PositionRoot'
    Qualify-ClosureVariant -Name 'mui-string-utf8-position' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringUtf8PositionRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-string-utf8-position' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::StringUtf8PositionRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListtreeRoot'
    Qualify-ClosureVariant -Name 'mui-listtree' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListtreeRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListtreeRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeMessageCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeMessageCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-message-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeMessageCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeMethodHeaderCodecRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeMethodHeaderCodecRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-method-header-codec' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::ListtreeMethodHeaderCodecRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-pointer-drag-commit' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListtreePointerDragCommitRoot' `
        -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-listtree-pointer-drag-commit' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListtreePointerDragCommitRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-pointer-drag-commit' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListtreePointerDragCommitRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-keyboard-selection' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListtreeKeyboardSelectionRoot'
    Qualify-NativeRoot -Name 'mui-listtree-double-click' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListtreeDoubleClickRoot'
    Qualify-NativeRoot -Name 'mui-listtree-exchange-relative' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListtreeExchangeRelativeRoot'
    Qualify-NativeRoot -Name 'mui-listtree-double-click-columns' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListtreeDoubleClickColumnsRoot'
    Qualify-ClosureVariant -Name 'mui-listtree-double-click-columns' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListtreeDoubleClickColumnsRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-double-click-columns' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListtreeDoubleClickColumnsRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-listtree-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListtreeDispatcherRoot' `
        -AllowRelocations
    Qualify-ClosureVariant -Name 'mui-listtree-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListtreeDispatcherRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-listtree-dispatcher' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativeRoots::CollectionListtreeDispatcherRoot' `
        -Cpu '68040'
    Qualify-NativeRoot -Name 'mui-native-public-object-retained-descendants-mg2825' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativePublicObjectOwnershipRoots::RetainedDescendantReleaseRoot'
    Qualify-ClosureVariant -Name 'mui-native-public-object-retained-descendants-mg2825' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativePublicObjectOwnershipRoots::RetainedDescendantReleaseRoot' `
        -Cpu '68020'
    Qualify-ClosureVariant -Name 'mui-native-public-object-retained-descendants-mg2825' `
        -Entry 'CopperOS.MuiMaster.NativeRoot.MuiNativePublicObjectOwnershipRoots::RetainedDescendantReleaseRoot' `
        -Cpu '68040'
)


$results
$executionProject = Join-Path $repo 'tests\MuiMaster.NativeExecution\CopperOS.MuiMaster.NativeExecution.csproj'
$headlessHunk = Join-Path $outDir 'mui-headless.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $headlessHunk
if ($LASTEXITCODE -ne 0) { throw 'MG04 MC68000 execution qualification failed.' }
$headlessCreationTagCodecHunk = Join-Path $outDir 'mui-headless-creation-tag-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $headlessCreationTagCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG603 headless creation TagItem codec MC68000 execution qualification failed.' }
$multiSetTargetVectorCodecHunk = Join-Path $outDir 'mui-multi-set-target-vector-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $multiSetTargetVectorCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG611 MultiSet target-vector codec MC68000 execution qualification failed.' }
$multiSetLiveHunk = Join-Path $outDir 'mui-multi-set.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $multiSetLiveHunk
if ($LASTEXITCODE -ne 0) { throw 'MG612 live MultiSet mutation MC68000 execution qualification failed.' }
$windowLifecycleHunk = Join-Path $outDir 'mui-window-lifecycle.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowLifecycleHunk
if ($LASTEXITCODE -ne 0) { throw 'MG613 native Window lifecycle MC68000 execution qualification failed.' }
$windowInputEventHunk = Join-Path $outDir 'mui-window-input-event.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowInputEventHunk
if ($LASTEXITCODE -ne 0) { throw 'MG614 native Window InputEvent MC68000 execution qualification failed.' }
$windowEventPollingHunk = Join-Path $outDir 'mui-window-event-polling.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowEventPollingHunk
if ($LASTEXITCODE -ne 0) { throw 'MG615 native Window event polling MC68000 execution qualification failed.' }
$windowPointerPollingHunk = Join-Path $outDir 'mui-window-pointer-polling.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowPointerPollingHunk
if ($LASTEXITCODE -ne 0) { throw 'MG616 native Window pointer polling MC68000 execution qualification failed.' }
$windowPreprocessedEventPollingHunk = Join-Path $outDir 'mui-window-preprocessed-event-polling.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowPreprocessedEventPollingHunk
if ($LASTEXITCODE -ne 0) { throw 'MG617 native preprocessed Window event polling MC68000 execution qualification failed.' }
$windowEventHandlerPollingHunk = Join-Path $outDir 'mui-window-event-handler-polling.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowEventHandlerPollingHunk
if ($LASTEXITCODE -ne 0) { throw 'MG618 native Window event-handler polling MC68000 execution qualification failed.' }
$areaHandledEventsRegistrationHunk = Join-Path $outDir 'mui-area-handled-events-registration.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaHandledEventsRegistrationHunk
if ($LASTEXITCODE -ne 0) { throw 'MG619 native Area handled-events registration MC68000 execution qualification failed.' }
$windowEventHandlerActiveGroupHunk = Join-Path $outDir 'mui-window-event-handler-active-group.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowEventHandlerActiveGroupHunk
if ($LASTEXITCODE -ne 0) { throw 'MG620 native Window event-handler active-group MC68000 execution qualification failed.' }
$windowEventHandlerLinksHunk = Join-Path $outDir 'mui-window-event-handler-links.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowEventHandlerLinksHunk
if ($LASTEXITCODE -ne 0) { throw 'MG621 native Window event-handler links MC68000 execution qualification failed.' }
$makeObjectGeneratedTagCodecHunk = Join-Path $outDir 'mui-make-object-generated-tag-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $makeObjectGeneratedTagCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG605 MUI_MakeObjectA generated TagItem codec MC68000 execution qualification failed.' }
$makeObjectClassNameStructCodecHunk = Join-Path $outDir 'mui-make-object-class-name-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $makeObjectClassNameStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1393 MUI_MakeObjectA class-name struct codec MC68000 execution qualification failed.' }
$stringscrollClassNameStructCodecHunk = Join-Path $outDir 'mui-stringscroll-class-name-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringscrollClassNameStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1394 Stringscroll class-name struct codec MC68000 execution qualification failed.' }
$applicationSettingsClassNameStructCodecHunk = Join-Path $outDir 'mui-application-settings-class-name-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationSettingsClassNameStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1395 Application settings Dataspace class-name struct codec MC68000 execution qualification failed.' }
$windowClassNameStructCodecHunk = Join-Path $outDir 'mui-window-class-name-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowClassNameStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1396 Window.mui class-name struct codec MC68000 execution qualification failed.' }
$groupClassNameStructCodecHunk = Join-Path $outDir 'mui-group-class-name-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $groupClassNameStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1397 Group.mui class-name struct codec MC68000 execution qualification failed.' }
$listtreeClassNameStructCodecHunk = Join-Path $outDir 'mui-listtree-class-name-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeClassNameStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1398 Listtree.mcc class-name struct codec MC68000 execution qualification failed.' }
$registerClassNameStructCodecHunk = Join-Path $outDir 'mui-register-class-name-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $registerClassNameStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1399 Register.mui class-name struct codec MC68000 execution qualification failed.' }
$selectgroupClassNameStructCodecHunk = Join-Path $outDir 'mui-selectgroup-class-name-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $selectgroupClassNameStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1400 Selectgroup.mui class-name struct codec MC68000 execution qualification failed.' }
$scrollgroupClassNameStructCodecHunk = Join-Path $outDir 'mui-scrollgroup-class-name-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $scrollgroupClassNameStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1401 Scrollgroup.mui class-name struct codec MC68000 execution qualification failed.' }
$virtgroupClassNameStructCodecHunk = Join-Path $outDir 'mui-virtgroup-class-name-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $virtgroupClassNameStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1402 Virtgroup.mui class-name struct codec MC68000 execution qualification failed.' }
$dirlistExampleVolumeNameStructCodecHunk = Join-Path $outDir 'mui-dirlist-example-volume-name-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $dirlistExampleVolumeNameStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1403 Dirlist example-volume name struct codec MC68000 execution qualification failed.' }
$colorSpecialistClassNameStructCodecHunk = Join-Path $outDir 'mui-color-specialist-class-name-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $colorSpecialistClassNameStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1404 Color specialist class-name struct codec MC68000 execution qualification failed.' }
$processSpecialistClassNameStructCodecHunk = Join-Path $outDir 'mui-process-specialist-class-name-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $processSpecialistClassNameStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1405 Process specialist class-name struct codec MC68000 execution qualification failed.' }
$externalWrapperClassNameStructCodecHunk = Join-Path $outDir 'mui-external-wrapper-class-name-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $externalWrapperClassNameStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1406 External-wrapper class-name struct codec MC68000 execution qualification failed.' }
$menuSpecialistClassNameStructCodecHunk = Join-Path $outDir 'mui-menu-specialist-class-name-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $menuSpecialistClassNameStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1407 Menu specialist class-name struct codec MC68000 execution qualification failed.' }
$popSpecialistClassNameStructCodecHunk = Join-Path $outDir 'mui-pop-specialist-class-name-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $popSpecialistClassNameStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1408 Pop specialist class-name struct codec MC68000 execution qualification failed.' }
$miscSpecialistClassNameStructCodecHunk = Join-Path $outDir 'mui-misc-specialist-class-name-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $miscSpecialistClassNameStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1409 Misc specialist class-name struct codec MC68000 execution qualification failed.' }
$layoutHunk = Join-Path $outDir 'mui-layout.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $layoutHunk
if ($LASTEXITCODE -ne 0) { throw 'MG05 MC68000 execution qualification failed.' }
$commonControlPacketsHunk = Join-Path $outDir 'mui-commoncontrol-packets.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $commonControlPacketsHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 common-control packet codec MC68000 execution qualification failed.' }
$commonControlNumericToggleDefaultHunk = Join-Path $outDir 'mui-commoncontrol-numeric-toggle-default.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $commonControlNumericToggleDefaultHunk
if ($LASTEXITCODE -ne 0) { throw 'MG720 Numeric toggle-default MC68000 execution qualification failed.' }
$areaFixedTextHunk = Join-Path $outDir 'mui-area-fixed-text-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaFixedTextHunk
if ($LASTEXITCODE -ne 0) { throw 'MG721 Area fixed-text codec MC68000 execution qualification failed.' }
$areaFloatingHunk = Join-Path $outDir 'mui-area-floating-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaFloatingHunk
if ($LASTEXITCODE -ne 0) { throw 'MG722 Area Floating codec MC68000 execution qualification failed.' }
$callHookMessageCodecHunk = Join-Path $outDir 'mui-callhook-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $callHookMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 CallHook message codec MC68000 execution qualification failed.' }

$windowEventHandlerHunk = Join-Path $outDir 'mui-window-event-handler.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowEventHandlerHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 window event-handler MC68000 execution qualification failed.' }

$windowEventHandlerPriorityHunk = Join-Path $outDir 'mui-window-event-handler-priority.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowEventHandlerPriorityHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 window event-handler priority MC68000 execution qualification failed.' }

$windowEventHandlerActiveParentHunk = Join-Path $outDir 'mui-window-event-handler-active-parent.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowEventHandlerActiveParentHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 active-parent window event-handler MC68000 execution qualification failed.' }
$windowEventHandlerCallingHunk = Join-Path $outDir 'mui-window-event-handler-calling.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowEventHandlerCallingHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 calling-flag window event-handler MC68000 execution qualification failed.' }
$windowEventHandlerEnabledHunk = Join-Path $outDir 'mui-window-event-handler-enabled.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowEventHandlerEnabledHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 enabled-flag window event-handler MC68000 execution qualification failed.' }
$windowEventHandlerActiveStateHunk = Join-Path $outDir 'mui-window-event-handler-active-state.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowEventHandlerActiveStateHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 active-state window event-handler MC68000 execution qualification failed.' }
$windowEventHandlerDisableKeysHunk = Join-Path $outDir 'mui-window-event-handler-disable-keys.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowEventHandlerDisableKeysHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 disable-keys window event-handler MC68000 execution qualification failed.' }

$layoutServiceHunk = Join-Path $outDir 'mui-layout-service.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $layoutServiceHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 MUI_Layout service MC68000 execution qualification failed.' }

$scrollbarLayoutServiceHunk = Join-Path $outDir 'mui-scrollbar-layout-service.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $scrollbarLayoutServiceHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 scrollbar layout service MC68000 execution qualification failed.' }

$classServiceHunk = Join-Path $outDir 'mui-class-service.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $classServiceHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 class-service MC68000 execution qualification failed.' }

$classServiceFieldStructMg2295Hunk = Join-Path $outDir 'mui-class-service-field-struct-mg2295.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $classServiceFieldStructMg2295Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2295 class-service field-struct MC68000 execution qualification failed.' }

$aslServiceHunk = Join-Path $outDir 'mui-asl-service.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $aslServiceHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 ASL service MC68000 execution qualification failed.' }

$aslTagServiceHunk = Join-Path $outDir 'mui-asl-tag-service.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $aslTagServiceHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 ASL tag service MC68000 execution qualification failed.' }

$requesterServiceHunk = Join-Path $outDir 'mui-requester-service.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $requesterServiceHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 requester service MC68000 execution qualification failed.' }
$requesterServiceFieldStructMg2292Hunk = Join-Path $outDir 'mui-requester-service-field-struct-mg2292.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $requesterServiceFieldStructMg2292Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2292 requester service field-struct MC68000 execution qualification failed.' }

$requesterPayloadServiceHunk = Join-Path $outDir 'mui-requester-payload-service.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $requesterPayloadServiceHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 requester payload service MC68000 execution qualification failed.' }

$requesterFormatServiceHunk = Join-Path $outDir 'mui-requester-format-service.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $requesterFormatServiceHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 requester format service MC68000 execution qualification failed.' }

$requesterSignalPolicyHunk = Join-Path $outDir 'mui-requester-application-signal-policy.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $requesterSignalPolicyHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 requester application signal-policy MC68000 execution qualification failed.' }

$redrawServiceHunk = Join-Path $outDir 'mui-redraw-service.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $redrawServiceHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 redraw service MC68000 execution qualification failed.' }

$newObjectServiceHunk = Join-Path $outDir 'mui-new-object-service.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $newObjectServiceHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 new-object service MC68000 execution qualification failed.' }

$commonFactoryServiceHunk = Join-Path $outDir 'mui-common-factory-service.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $commonFactoryServiceHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 common-control factory MC68000 execution qualification failed.' }

$classCommonFactoryServiceHunk = Join-Path $outDir 'mui-class-common-factory-service.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $classCommonFactoryServiceHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 class-service common-control factory MC68000 execution qualification failed.' }

$newObjectMenuServiceHunk = Join-Path $outDir 'mui-new-object-menu-service.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $newObjectMenuServiceHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 menu object-factory service MC68000 execution qualification failed.' }

$menuDispatcherHunk = Join-Path $outDir 'mui-menu-dispatcher.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $menuDispatcherHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 menu dispatcher MC68000 execution qualification failed.' }

$processDispatcherHunk = Join-Path $outDir 'mui-process-dispatcher.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $processDispatcherHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 process dispatcher MC68000 execution qualification failed.' }

$processDispatchMethodHeaderHunk = Join-Path $outDir 'mui-process-dispatch-method-header-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $processDispatchMethodHeaderHunk
if ($LASTEXITCODE -ne 0) { throw 'MG601 Process/Slave dispatch method-header MC68000 execution qualification failed.' }

$serviceDispatcherHunk = Join-Path $outDir 'mui-service-dispatcher.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $serviceDispatcherHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 standalone specialist dispatcher MC68000 execution qualification failed.' }

$externalDispatcherHunk = Join-Path $outDir 'mui-external-dispatcher.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $externalDispatcherHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 external specialist dispatcher MC68000 execution qualification failed.' }

$externalObjectServiceHunk = Join-Path $outDir 'mui-external-object-service.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $externalObjectServiceHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 external-object service MC68000 execution qualification failed.' }

$disposeObjectServiceHunk = Join-Path $outDir 'mui-dispose-object-service.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $disposeObjectServiceHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 dispose-object service MC68000 execution qualification failed.' }

$makeObjectServiceHunk = Join-Path $outDir 'mui-make-object-service.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $makeObjectServiceHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 make-object service MC68000 execution qualification failed.' }

$errorServiceHunk = Join-Path $outDir 'mui-error-service.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $errorServiceHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 error service MC68000 execution qualification failed.' }
$errorServiceFieldStructMg2291Hunk = Join-Path $outDir 'mui-error-service-field-struct-mg2291.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $errorServiceFieldStructMg2291Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2291 Error service field-struct MC68000 execution qualification failed.' }

$idcmpServiceHunk = Join-Path $outDir 'mui-idcmp-service.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $idcmpServiceHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 IDCMP service MC68000 execution qualification failed.' }

$drawingServiceHunk = Join-Path $outDir 'mui-drawing-service.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $drawingServiceHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 drawing service MC68000 execution qualification failed.' }

$colorSpecialistHunk = Join-Path $outDir 'mui-color-specialist.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $colorSpecialistHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 pen/color specialist MC68000 execution qualification failed.' }
$colorSpecialistMessageCodecHunk = Join-Path $outDir 'mui-color-specialist-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $colorSpecialistMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 pen/color specialist message codec MC68000 execution qualification failed.' }

$popSpecialistHunk = Join-Path $outDir 'mui-pop-specialist.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $popSpecialistHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Pop* specialist MC68000 execution qualification failed.' }

$popSpecialistMessageCodecHunk = Join-Path $outDir 'mui-pop-specialist-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $popSpecialistMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Pop* specialist message codec MC68000 execution qualification failed.' }

$menuSpecialistHunk = Join-Path $outDir 'mui-menu-specialist.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $menuSpecialistHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 menu specialist MC68000 execution qualification failed.' }

$menuSpecialistMessageCodecHunk = Join-Path $outDir 'mui-menu-specialist-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $menuSpecialistMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 menu specialist message codec MC68000 execution qualification failed.' }

$processSpecialistHunk = Join-Path $outDir 'mui-process-specialist.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $processSpecialistHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 process/slave specialist MC68000 execution qualification failed.' }

$processSpecialistMessageCodecHunk = Join-Path $outDir 'mui-process-specialist-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $processSpecialistMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Process/Slave specialist message codec MC68000 execution qualification failed.' }

$externalWrapperSpecialistHunk = Join-Path $outDir 'mui-external-wrapper-specialist.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $externalWrapperSpecialistHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Boopsi/Dtpic external-wrapper specialist MC68000 execution qualification failed.' }
$externalWrapperMessageCodecHunk = Join-Path $outDir 'mui-external-wrapper-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $externalWrapperMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 external-wrapper message codec MC68000 execution qualification failed.' }
$externalWrapperTagItemCodecHunk = Join-Path $outDir 'mui-external-wrapper-tagitem-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $externalWrapperTagItemCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG609 external-wrapper TagItem codec MC68000 execution qualification failed.' }
$poplistArrayCodecHunk = Join-Path $outDir 'mui-poplist-array-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $poplistArrayCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG610 Poplist array codec MC68000 execution qualification failed.' }
$poplistArrayVectorStructCodecHunk = Join-Path $outDir 'mui-poplist-array-vector-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $poplistArrayVectorStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1367 Poplist array vector struct codec MC68000 execution qualification failed.' }

$poplistArrayFieldStructMg2803Hunk = Join-Path $outDir 'mui-poplist-array-field-struct-mg2803.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $poplistArrayFieldStructMg2803Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2803 Poplist array field-struct MC68000 execution qualification failed.' }

$externalBoopsiPacketCodecHunk = Join-Path $outDir 'mui-external-boopsi-packet-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $externalBoopsiPacketCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG602 external BOOPSI packet codec MC68000 execution qualification failed.' }
$updateConfigTableVectorStructCodecHunk = Join-Path $outDir 'mui-update-config-table-vector-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $updateConfigTableVectorStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1375 UpdateConfig table vector struct codec MC68000 execution qualification failed.' }
$updateConfigMethodHeaderUlongStructHunk = Join-Path $outDir 'mui-update-config-method-header-ulong-struct.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $updateConfigMethodHeaderUlongStructHunk
if ($LASTEXITCODE -ne 0) { throw 'MG2145 UpdateConfig method-header ULONG struct codec MC68000 execution qualification failed.' }

$dirlistMessageCodecHunk = Join-Path $outDir 'mui-dirlist-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $dirlistMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Dirlist message codec MC68000 execution qualification failed.' }

$miscSpecialistHunk = Join-Path $outDir 'mui-misc-specialist.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $miscSpecialistHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 misc specialist (Keyadjust/Panel/Filepanel/Fontdisplay/Scrmodelist/Argstring/Aboutmui/Mccprefs/FSProtectionBits/Title) MC68000 execution qualification failed.' }
$miscMccprefsRegistryVectorStructCodecHunk = Join-Path $outDir 'mui-misc-mccprefs-registry-vector-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $miscMccprefsRegistryVectorStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1368 Mccprefs registry vector struct codec MC68000 execution qualification failed.' }
$miscScrmodelistModeVectorStructCodecHunk = Join-Path $outDir 'mui-misc-scrmodelist-mode-vector-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $miscScrmodelistModeVectorStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1369 Scrmodelist mode vector struct codec MC68000 execution qualification failed.' }
$miscFilepanelRowVectorStructCodecHunk = Join-Path $outDir 'mui-misc-filepanel-row-vector-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $miscFilepanelRowVectorStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1370 Filepanel row vector struct codec MC68000 execution qualification failed.' }
$listtreeDisplayColumnVectorStructCodecHunk = Join-Path $outDir 'mui-listtree-display-column-vector-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeDisplayColumnVectorStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1371 Listtree DisplayHook column vector struct codec MC68000 execution qualification failed.' }
$listtreeColumnGeometryVectorStructCodecHunk = Join-Path $outDir 'mui-listtree-column-geometry-vector-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeColumnGeometryVectorStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1372 Listtree column geometry vector struct codec MC68000 execution qualification failed.' }
$externalTagListVectorStructCodecHunk = Join-Path $outDir 'mui-external-taglist-vector-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $externalTagListVectorStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1373 external TagItem vector struct codec MC68000 execution qualification failed.' }
$externalRememberVectorStructCodecHunk = Join-Path $outDir 'mui-external-remember-vector-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $externalRememberVectorStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1373 external remember vector struct codec MC68000 execution qualification failed.' }
$externalBoopsiTagVectorStructCodecHunk = Join-Path $outDir 'mui-external-boopsi-tag-vector-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $externalBoopsiTagVectorStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1373 external BOOPSI TagItem vector struct codec MC68000 execution qualification failed.' }

$miscServiceDispatcherHunk = Join-Path $outDir 'mui-misc-service-dispatcher.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $miscServiceDispatcherHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Misc specialist service-dispatcher MC68000 execution qualification failed.' }

$miscSetupCleanupHunk = Join-Path $outDir 'mui-misc-setup-cleanup.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $miscSetupCleanupHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Misc Setup/Cleanup lifecycle MC68000 execution qualification failed.' }
$areaResizeAllFieldsStructMg2480Hunk = Join-Path $outDir 'mui-area-resize-all-fields-struct-mg2480.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaResizeAllFieldsStructMg2480Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2480 Area resize-state all-fields struct MC68000 execution qualification failed.' }
$areaGeometryAllFieldsStructMg2481Hunk = Join-Path $outDir 'mui-area-geometry-all-fields-struct-mg2481.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaGeometryAllFieldsStructMg2481Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2481 Area geometry all-fields struct MC68000 execution qualification failed.' }
$errorServiceAllFieldsStructMg2482Hunk = Join-Path $outDir 'mui-error-service-all-fields-struct-mg2482.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $errorServiceAllFieldsStructMg2482Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2482 Error-service all-fields struct MC68000 execution qualification failed.' }
$groupPageAllFieldsStructMg2483Hunk = Join-Path $outDir 'mui-group-page-all-fields-struct-mg2483.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $groupPageAllFieldsStructMg2483Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2483 Group-page all-fields struct MC68000 execution qualification failed.' }
$sleepStateAllFieldsStructMg2484Hunk = Join-Path $outDir 'mui-sleep-state-all-fields-struct-mg2484.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $sleepStateAllFieldsStructMg2484Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2484 Sleep-state all-fields struct MC68000 execution qualification failed.' }
$textContentsAllFieldsStructMg2485Hunk = Join-Path $outDir 'mui-text-contents-all-fields-struct-mg2485.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $textContentsAllFieldsStructMg2485Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2485 Text-contents all-fields struct MC68000 execution qualification failed.' }
$sliderPresentationAllFieldsStructMg2486Hunk = Join-Path $outDir 'mui-slider-presentation-all-fields-struct-mg2486.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $sliderPresentationAllFieldsStructMg2486Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2486 Slider-presentation all-fields struct MC68000 execution qualification failed.' }
$rectangleBarTitleAllFieldsStructMg2487Hunk = Join-Path $outDir 'mui-rectangle-bar-title-all-fields-struct-mg2487.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $rectangleBarTitleAllFieldsStructMg2487Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2487 Rectangle bar-title all-fields struct MC68000 execution qualification failed.' }
$propRangeAllFieldsStructMg2488Hunk = Join-Path $outDir 'mui-prop-range-all-fields-struct-mg2488.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $propRangeAllFieldsStructMg2488Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2488 Prop range all-fields struct MC68000 execution qualification failed.' }
$stringCursorAllFieldsStructMg2489Hunk = Join-Path $outDir 'mui-string-cursor-all-fields-struct-mg2489.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringCursorAllFieldsStructMg2489Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2489 String cursor all-fields struct MC68000 execution qualification failed.' }
$stringContentsAllFieldsStructMg2490Hunk = Join-Path $outDir 'mui-string-contents-all-fields-struct-mg2490.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringContentsAllFieldsStructMg2490Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2490 String contents all-fields struct MC68000 execution qualification failed.' }
$stringPlaceholderAllFieldsStructMg2491Hunk = Join-Path $outDir 'mui-string-placeholder-all-fields-struct-mg2491.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringPlaceholderAllFieldsStructMg2491Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2491 String placeholder all-fields struct MC68000 execution qualification failed.' }
$stringAcknowledgeAllFieldsStructMg2492Hunk = Join-Path $outDir 'mui-string-acknowledge-all-fields-struct-mg2492.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringAcknowledgeAllFieldsStructMg2492Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2492 String acknowledgement all-fields struct MC68000 execution qualification failed.' }
$stringIntegerAllFieldsStructMg2493Hunk = Join-Path $outDir 'mui-string-integer-all-fields-struct-mg2493.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringIntegerAllFieldsStructMg2493Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2493 String integer all-fields struct MC68000 execution qualification failed.' }
$stringSpellCheckingAllFieldsStructMg2494Hunk = Join-Path $outDir 'mui-string-spell-checking-all-fields-struct-mg2494.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringSpellCheckingAllFieldsStructMg2494Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2494 String spell-checking all-fields struct MC68000 execution qualification failed.' }
$stringAttachedListAllFieldsStructMg2495Hunk = Join-Path $outDir 'mui-string-attached-list-all-fields-struct-mg2495.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringAttachedListAllFieldsStructMg2495Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2495 String attached-list all-fields struct MC68000 execution qualification failed.' }
$stringFilterAllFieldsStructMg2496Hunk = Join-Path $outDir 'mui-string-filter-all-fields-struct-mg2496.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringFilterAllFieldsStructMg2496Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2496 String filter all-fields struct MC68000 execution qualification failed.' }
$textCopyAllFieldsStructMg2497Hunk = Join-Path $outDir 'mui-text-copy-all-fields-struct-mg2497.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $textCopyAllFieldsStructMg2497Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2497 Text copy all-fields struct MC68000 execution qualification failed.' }
$textPreParseAllFieldsStructMg2498Hunk = Join-Path $outDir 'mui-text-pre-parse-all-fields-struct-mg2498.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $textPreParseAllFieldsStructMg2498Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2498 Text pre-parse all-fields struct MC68000 execution qualification failed.' }
$textShortenedAllFieldsStructMg2499Hunk = Join-Path $outDir 'mui-text-shortened-all-fields-struct-mg2499.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $textShortenedAllFieldsStructMg2499Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2499 Text shortened all-fields struct MC68000 execution qualification failed.' }
$textUnicodeAllFieldsStructMg2500Hunk = Join-Path $outDir 'mui-text-unicode-all-fields-struct-mg2500.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $textUnicodeAllFieldsStructMg2500Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2500 Text Unicode all-fields struct MC68000 execution qualification failed.' }
$volumelistModeAllFieldsStructMg2501Hunk = Join-Path $outDir 'mui-volumelist-mode-all-fields-struct-mg2501.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $volumelistModeAllFieldsStructMg2501Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2501 Volumelist mode all-fields struct MC68000 execution qualification failed.' }
$aslAllFieldsStructMg2502Hunk = Join-Path $outDir 'mui-asl-all-fields-struct-mg2502.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $aslAllFieldsStructMg2502Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2502 ASL all-fields struct MC68000 execution qualification failed.' }
$imageFontMatchStringAllFieldsStructMg2503Hunk = Join-Path $outDir 'mui-image-font-match-string-all-fields-struct-mg2503.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $imageFontMatchStringAllFieldsStructMg2503Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2503 Image FontMatchString all-fields struct MC68000 execution qualification failed.' }
$rectanglePresentationAllFieldsStructMg2504Hunk = Join-Path $outDir 'mui-rectangle-presentation-all-fields-struct-mg2504.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $rectanglePresentationAllFieldsStructMg2504Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2504 Rectangle presentation all-fields struct MC68000 execution qualification failed.' }
$registerPolicyAllFieldsStructMg2505Hunk = Join-Path $outDir 'mui-register-policy-all-fields-struct-mg2505.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $registerPolicyAllFieldsStructMg2505Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2505 Register policy all-fields struct MC68000 execution qualification failed.' }
$scrollbarLayoutAllFieldsStructMg2506Hunk = Join-Path $outDir 'mui-scrollbar-layout-all-fields-struct-mg2506.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $scrollbarLayoutAllFieldsStructMg2506Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2506 Scrollbar layout all-fields struct MC68000 execution qualification failed.' }
$requesterServiceAllFieldsStructMg2507Hunk = Join-Path $outDir 'mui-requester-service-all-fields-struct-mg2507.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $requesterServiceAllFieldsStructMg2507Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2507 Requester service all-fields struct MC68000 execution qualification failed.' }
$numericFormatAllFieldsStructMg2508Hunk = Join-Path $outDir 'mui-numeric-format-all-fields-struct-mg2508.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $numericFormatAllFieldsStructMg2508Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2508 Numeric format all-fields struct MC68000 execution qualification failed.' }
$imageOldImageAllFieldsStructMg2509Hunk = Join-Path $outDir 'mui-image-old-image-all-fields-struct-mg2509.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $imageOldImageAllFieldsStructMg2509Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2509 Image OldImage all-fields struct MC68000 execution qualification failed.' }
$selectgroupAllFieldsStructMg2510Hunk = Join-Path $outDir 'mui-selectgroup-all-fields-struct-mg2510.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $selectgroupAllFieldsStructMg2510Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2510 Selectgroup all-fields struct MC68000 execution qualification failed.' }
$sliderPresentationAllFieldsStructMg2511Hunk = Join-Path $outDir 'mui-slider-presentation-all-fields-struct-mg2511.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $sliderPresentationAllFieldsStructMg2511Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2511 Slider presentation all-fields struct MC68000 execution qualification failed.' }
$textCopyCursorStructMg2512Hunk = Join-Path $outDir 'mui-text-copy-cursor-struct-mg2512.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $textCopyCursorStructMg2512Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2512 Text copy cursor struct MC68000 execution qualification failed.' }
$keyadjustTextCursorStructMg2513Hunk = Join-Path $outDir 'mui-keyadjust-text-cursor-struct-mg2513.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $keyadjustTextCursorStructMg2513Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2513 Keyadjust text cursor struct MC68000 execution qualification failed.' }
$aslTagItemCursorStructMg2514Hunk = Join-Path $outDir 'mui-asl-tag-item-cursor-struct-mg2514.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $aslTagItemCursorStructMg2514Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2514 ASL TagItem cursor struct MC68000 execution qualification failed.' }
$areaShortHelpCursorStructMg2515Hunk = Join-Path $outDir 'mui-area-short-help-cursor-struct-mg2515.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaShortHelpCursorStructMg2515Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2515 ShortHelp packet cursor struct MC68000 execution qualification failed.' }
$areaContextMenuMessageStructMg2516Hunk = Join-Path $outDir 'mui-area-context-menu-message-struct-mg2516.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaContextMenuMessageStructMg2516Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2516 Area ContextMenu message struct MC68000 execution qualification failed.' }
$boopsiQueryMessageStructMg2517Hunk = Join-Path $outDir 'mui-boopsi-query-message-struct-mg2517.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $boopsiQueryMessageStructMg2517Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2517 BoopsiQuery message struct MC68000 execution qualification failed.' }
$areaCustomFontMessageStructMg2518Hunk = Join-Path $outDir 'mui-area-custom-font-message-struct-mg2518.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaCustomFontMessageStructMg2518Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2518 Area custom-font message struct MC68000 execution qualification failed.' }
$callHookMessageStructMg2519Hunk = Join-Path $outDir 'mui-call-hook-message-struct-mg2519.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $callHookMessageStructMg2519Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2519 CallHook message struct MC68000 execution qualification failed.' }
$callHookParameterTailCursorMg2807Hunk = Join-Path $outDir 'mui-call-hook-parameter-tail-cursor-mg2807.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $callHookParameterTailCursorMg2807Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2807 CallHook parameter-tail cursor MC68000 execution qualification failed.' }
$requesterParameterSlotFieldStructMg2808Hunk = Join-Path $outDir 'mui-requester-parameter-slot-field-struct-mg2808.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $requesterParameterSlotFieldStructMg2808Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2808 Requester parameter-slot field struct MC68000 execution qualification failed.' }
$applicationPushMethodParameterHeaderCursorStructMg2809Hunk = Join-Path $outDir 'mui-application-push-method-parameter-header-cursor-struct-mg2809.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationPushMethodParameterHeaderCursorStructMg2809Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2809 Application PushMethod parameter-header cursor struct MC68000 execution qualification failed.' }
$processArgumentVectorHeaderCursorStructMg2810Hunk = Join-Path $outDir 'mui-process-argument-vector-header-cursor-struct-mg2810.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $processArgumentVectorHeaderCursorStructMg2810Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2810 Process argument vector header cursor MC68000 execution qualification failed.' }
$collectionRecordMessageStructMg2520Hunk = Join-Path $outDir 'mui-collection-record-message-struct-mg2520.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionRecordMessageStructMg2520Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2520 Collection record message struct MC68000 execution qualification failed.' }
$collectionBasicMessageStructMg2521Hunk = Join-Path $outDir 'mui-collection-basic-message-struct-mg2521.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionBasicMessageStructMg2521Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2521 Collection basic message struct MC68000 execution qualification failed.' }
$collectionSurfaceMessageStructMg2522Hunk = Join-Path $outDir 'mui-collection-surface-message-struct-mg2522.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionSurfaceMessageStructMg2522Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2522 Collection surface message struct MC68000 execution qualification failed.' }
$collectionEditMessageStructMg2523Hunk = Join-Path $outDir 'mui-collection-edit-message-struct-mg2523.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionEditMessageStructMg2523Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2523 Collection edit message struct MC68000 execution qualification failed.' }
$collectionAdvancedMessageStructMg2524Hunk = Join-Path $outDir 'mui-collection-advanced-message-struct-mg2524.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionAdvancedMessageStructMg2524Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2524 Collection advanced message struct MC68000 execution qualification failed.' }
$applicationQueuePacketCursorStructMg2525Hunk = Join-Path $outDir 'mui-application-queue-packet-cursor-struct-mg2525.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationQueuePacketCursorStructMg2525Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2525 Application queue packet cursor struct MC68000 execution qualification failed.' }
$applicationInputPacketCursorStructMg2526Hunk = Join-Path $outDir 'mui-application-input-packet-cursor-struct-mg2526.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationInputPacketCursorStructMg2526Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2526 Application input packet cursor struct MC68000 execution qualification failed.' }
$applicationPresentationPacketCursorStructMg2527Hunk = Join-Path $outDir 'mui-application-presentation-packet-cursor-struct-mg2527.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationPresentationPacketCursorStructMg2527Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2527 Application presentation packet cursor struct MC68000 execution qualification failed.' }
$applicationSettingsPacketCursorStructMg2528Hunk = Join-Path $outDir 'mui-application-settings-packet-cursor-struct-mg2528.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationSettingsPacketCursorStructMg2528Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2528 Application settings packet cursor struct MC68000 execution qualification failed.' }
$applicationMethodPacketCursorStructMg2529Hunk = Join-Path $outDir 'mui-application-method-packet-cursor-struct-mg2529.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationMethodPacketCursorStructMg2529Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2529 Application method packet cursor struct MC68000 execution qualification failed.' }
$windowCycleChainPacketCursorStructMg2530Hunk = Join-Path $outDir 'mui-window-cycle-chain-packet-cursor-struct-mg2530.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowCycleChainPacketCursorStructMg2530Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2530 Window cycle-chain packet cursor struct MC68000 execution qualification failed.' }
$windowCycleChainInlineVectorCursorMg2806Hunk = Join-Path $outDir 'mui-window-cycle-chain-inline-vector-cursor-mg2806.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowCycleChainInlineVectorCursorMg2806Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2806 Window cycle-chain inline-vector cursor MC68000 execution qualification failed.' }
$applicationMenuPacketCursorStructMg2531Hunk = Join-Path $outDir 'mui-application-menu-packet-cursor-struct-mg2531.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationMenuPacketCursorStructMg2531Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2531 Application menu packet cursor struct MC68000 execution qualification failed.' }
$windowEventHandlerPacketCursorStructMg2532Hunk = Join-Path $outDir 'mui-window-event-handler-packet-cursor-struct-mg2532.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowEventHandlerPacketCursorStructMg2532Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2532 Window event-handler packet cursor struct MC68000 execution qualification failed.' }
$applicationWindowNodeFieldCursorStructMg2533Hunk = Join-Path $outDir 'mui-application-window-node-field-cursor-struct-mg2533.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationWindowNodeFieldCursorStructMg2533Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2533 Application window-node field cursor struct MC68000 execution qualification failed.' }
$applicationWindowNodePayloadCursorStructMg2805Hunk = Join-Path $outDir 'mui-application-window-node-payload-cursor-struct-mg2805.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationWindowNodePayloadCursorStructMg2805Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2805 Application window-node payload cursor struct MC68000 execution qualification failed.' }
$eventHandlerNodeFieldCursorStructMg2534Hunk = Join-Path $outDir 'mui-event-handler-node-field-cursor-struct-mg2534.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $eventHandlerNodeFieldCursorStructMg2534Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2534 Event-handler node field cursor struct MC68000 execution qualification failed.' }
$inputHandlerNodeFieldCursorStructMg2535Hunk = Join-Path $outDir 'mui-input-handler-node-field-cursor-struct-mg2535.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $inputHandlerNodeFieldCursorStructMg2535Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2535 Input-handler node field cursor struct MC68000 execution qualification failed.' }
$applicationPersistenceFrameFieldCursorStructMg2536Hunk = Join-Path $outDir 'mui-application-persistence-frame-field-cursor-struct-mg2536.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationPersistenceFrameFieldCursorStructMg2536Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2536 Application persistence frame field cursor struct MC68000 execution qualification failed.' }
$applicationSettingsFileFieldCursorStructMg2537Hunk = Join-Path $outDir 'mui-application-settings-file-field-cursor-struct-mg2537.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationSettingsFileFieldCursorStructMg2537Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2537 Application settings-file field cursor struct MC68000 execution qualification failed.' }
$dataspaceIffFieldCursorStructMg2538Hunk = Join-Path $outDir 'mui-dataspace-iff-field-cursor-struct-mg2538.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $dataspaceIffFieldCursorStructMg2538Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2538 Dataspace-IFF field cursor struct MC68000 execution qualification failed.' }
$storeRecordFieldCursorStructMg2539Hunk = Join-Path $outDir 'mui-store-record-field-cursor-struct-mg2539.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $storeRecordFieldCursorStructMg2539Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2539 Store-record field cursor struct MC68000 execution qualification failed.' }
$headlessStateFieldCursorStructMg2540Hunk = Join-Path $outDir 'mui-headless-state-field-cursor-struct-mg2540.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $headlessStateFieldCursorStructMg2540Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2540 Headless-state field cursor struct MC68000 execution qualification failed.' }
$headlessClassFieldCursorStructMg2541Hunk = Join-Path $outDir 'mui-headless-class-field-cursor-struct-mg2541.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $headlessClassFieldCursorStructMg2541Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2541 Headless-class field cursor struct MC68000 execution qualification failed.' }
$headlessAttributeFieldCursorStructMg2542Hunk = Join-Path $outDir 'mui-headless-attribute-field-cursor-struct-mg2542.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $headlessAttributeFieldCursorStructMg2542Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2542 Headless-attribute field cursor struct MC68000 execution qualification failed.' }
$headlessChildFieldCursorStructMg2543Hunk = Join-Path $outDir 'mui-headless-child-field-cursor-struct-mg2543.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $headlessChildFieldCursorStructMg2543Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2543 Headless-child field cursor struct MC68000 execution qualification failed.' }
$headlessNotificationFieldCursorStructMg2544Hunk = Join-Path $outDir 'mui-headless-notification-field-cursor-struct-mg2544.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $headlessNotificationFieldCursorStructMg2544Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2544 Headless-notification field cursor struct MC68000 execution qualification failed.' }
$headlessObjectFieldCursorStructMg2545Hunk = Join-Path $outDir 'mui-headless-object-field-cursor-struct-mg2545.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $headlessObjectFieldCursorStructMg2545Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2545 Headless-object field cursor struct MC68000 execution qualification failed.' }
$guestUlongFieldCursorStructMg2546Hunk = Join-Path $outDir 'mui-guest-ulong-field-cursor-struct-mg2546.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $guestUlongFieldCursorStructMg2546Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2546 Guest ULONG field cursor struct MC68000 execution qualification failed.' }
$headlessMethodFieldCursorStructMg2547Hunk = Join-Path $outDir 'mui-headless-method-field-cursor-struct-mg2547.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $headlessMethodFieldCursorStructMg2547Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2547 Headless-method field cursor struct MC68000 execution qualification failed.' }
$headlessOmFieldCursorStructMg2548Hunk = Join-Path $outDir 'mui-headless-om-field-cursor-struct-mg2548.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $headlessOmFieldCursorStructMg2548Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2548 Headless OM field cursor struct MC68000 execution qualification failed.' }
$groupGridFieldCursorStructMg2549Hunk = Join-Path $outDir 'mui-group-grid-field-cursor-struct-mg2549.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $groupGridFieldCursorStructMg2549Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2549 Group-grid field cursor struct MC68000 execution qualification failed.' }
$controlFontResolutionFieldCursorStructMg2550Hunk = Join-Path $outDir 'mui-control-font-resolution-field-cursor-struct-mg2550.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $controlFontResolutionFieldCursorStructMg2550Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2550 Control-font-resolution field cursor struct MC68000 execution qualification failed.' }
$makeObjectPreParseFieldCursorStructMg2551Hunk = Join-Path $outDir 'mui-makeobject-preparse-field-cursor-struct-mg2551.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $makeObjectPreParseFieldCursorStructMg2551Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2551 MakeObject preparse field cursor struct MC68000 execution qualification failed.' }
$propPolicyStateFieldCursorStructMg2552Hunk = Join-Path $outDir 'mui-prop-policy-state-field-cursor-struct-mg2552.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $propPolicyStateFieldCursorStructMg2552Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2552 Prop-policy-state field cursor struct MC68000 execution qualification failed.' }
$windowFocusStateFieldCursorStructMg2553Hunk = Join-Path $outDir 'mui-window-focus-state-field-cursor-struct-mg2553.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowFocusStateFieldCursorStructMg2553Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2553 Window-focus-state field cursor struct MC68000 execution qualification failed.' }
$windowControlStateFieldCursorStructMg2554Hunk = Join-Path $outDir 'mui-window-control-state-field-cursor-struct-mg2554.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowControlStateFieldCursorStructMg2554Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2554 Window-control-state field cursor struct MC68000 execution qualification failed.' }
$windowPresentationStateFieldCursorStructMg2555Hunk = Join-Path $outDir 'mui-window-presentation-state-field-cursor-struct-mg2555.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowPresentationStateFieldCursorStructMg2555Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2555 Window-presentation-state field cursor struct MC68000 execution qualification failed.' }
$windowRelationshipStateFieldCursorStructMg2556Hunk = Join-Path $outDir 'mui-window-relationship-state-field-cursor-struct-mg2556.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowRelationshipStateFieldCursorStructMg2556Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2556 Window-relationship-state field cursor struct MC68000 execution qualification failed.' }
$windowVisualStateFieldCursorStructMg2557Hunk = Join-Path $outDir 'mui-window-visual-state-field-cursor-struct-mg2557.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowVisualStateFieldCursorStructMg2557Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2557 Window-visual-state field cursor struct MC68000 execution qualification failed.' }
$windowInteractionStateFieldCursorStructMg2558Hunk = Join-Path $outDir 'mui-window-interaction-state-field-cursor-struct-mg2558.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowInteractionStateFieldCursorStructMg2558Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2558 Window-interaction-state field cursor struct MC68000 execution qualification failed.' }
$areaLayoutPolicyStateFieldCursorStructMg2559Hunk = Join-Path $outDir 'mui-area-layout-policy-state-field-cursor-struct-mg2559.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaLayoutPolicyStateFieldCursorStructMg2559Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2559 Area-layout-policy-state field cursor struct MC68000 execution qualification failed.' }
$scrollgroupPolicyStateFieldCursorStructMg2560Hunk = Join-Path $outDir 'mui-scrollgroup-policy-state-field-cursor-struct-mg2560.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $scrollgroupPolicyStateFieldCursorStructMg2560Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2560 Scrollgroup-policy-state field cursor struct MC68000 execution qualification failed.' }
$windowOpenPolicyStateFieldCursorStructMg2561Hunk = Join-Path $outDir 'mui-window-open-policy-state-field-cursor-struct-mg2561.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowOpenPolicyStateFieldCursorStructMg2561Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2561 Window-open-policy-state field cursor struct MC68000 execution qualification failed.' }
$minMaxFieldCursorStructMg2562Hunk = Join-Path $outDir 'mui-minmax-field-cursor-struct-mg2562.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $minMaxFieldCursorStructMg2562Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2562 MinMax field cursor struct MC68000 execution qualification failed.' }
$listTestPosFieldCursorStructMg2563Hunk = Join-Path $outDir 'mui-list-testpos-field-cursor-struct-mg2563.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listTestPosFieldCursorStructMg2563Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2563 List TestPos field cursor struct MC68000 execution qualification failed.' }
$listInputFieldCursorStructMg2564Hunk = Join-Path $outDir 'mui-list-input-field-cursor-struct-mg2564.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listInputFieldCursorStructMg2564Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2564 List input field cursor struct MC68000 execution qualification failed.' }
$intuiMessageFieldCursorStructMg2565Hunk = Join-Path $outDir 'mui-intui-message-field-cursor-struct-mg2565.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $intuiMessageFieldCursorStructMg2565Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2565 IntuiMessage field cursor struct MC68000 execution qualification failed.' }
$listviewDragStateFieldCursorStructMg2566Hunk = Join-Path $outDir 'mui-listview-drag-state-field-cursor-struct-mg2566.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewDragStateFieldCursorStructMg2566Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2566 Listview drag-state field cursor struct MC68000 execution qualification failed.' }
$windowInputEventFieldCursorStructMg2567Hunk = Join-Path $outDir 'mui-window-input-event-field-cursor-struct-mg2567.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowInputEventFieldCursorStructMg2567Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2567 Window-input-event field cursor struct MC68000 execution qualification failed.' }
$numericStateFieldCursorStructMg2568Hunk = Join-Path $outDir 'mui-numeric-state-field-cursor-struct-mg2568.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $numericStateFieldCursorStructMg2568Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2568 Numeric-state field cursor struct MC68000 execution qualification failed.' }
$imageFontMatchStateFieldCursorStructMg2569Hunk = Join-Path $outDir 'mui-image-fontmatch-state-field-cursor-struct-mg2569.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $imageFontMatchStateFieldCursorStructMg2569Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2569 Image FontMatch-state field cursor struct MC68000 execution qualification failed.' }
$imageSpecStateFieldCursorStructMg2570Hunk = Join-Path $outDir 'mui-image-spec-state-field-cursor-struct-mg2570.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $imageSpecStateFieldCursorStructMg2570Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2570 Image spec-state field cursor struct MC68000 execution qualification failed.' }
$imageRenderStateFieldCursorStructMg2571Hunk = Join-Path $outDir 'mui-image-render-state-field-cursor-struct-mg2571.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $imageRenderStateFieldCursorStructMg2571Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2571 Image render-state field cursor struct MC68000 execution qualification failed.' }
$groupGridSpecFieldCursorStructMg2572Hunk = Join-Path $outDir 'mui-group-grid-spec-field-cursor-struct-mg2572.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $groupGridSpecFieldCursorStructMg2572Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2572 Group-grid-spec field cursor struct MC68000 execution qualification failed.' }
$groupChangeFieldCursorStructMg2573Hunk = Join-Path $outDir 'mui-group-change-field-cursor-struct-mg2573.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $groupChangeFieldCursorStructMg2573Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2573 GroupChange field cursor struct MC68000 execution qualification failed.' }
$classServiceFieldCursorStructMg2574Hunk = Join-Path $outDir 'class-service-field-cursor-struct-mg2574.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $classServiceFieldCursorStructMg2574Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2574 ClassService field cursor struct MC68000 execution qualification failed.' }
$floattextPolicyFieldCursorStructMg2575Hunk = Join-Path $outDir 'floattext-policy-field-cursor-struct-mg2575.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $floattextPolicyFieldCursorStructMg2575Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2575 Floattext policy field cursor struct MC68000 execution qualification failed.' }

$familyGetChildFieldCursorStructMg2576Hunk = Join-Path $outDir 'mui-family-get-child-field-cursor-struct-mg2576.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $familyGetChildFieldCursorStructMg2576Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2576 Family_GetChild field cursor struct MC68000 execution qualification failed.' }

$colorSpecialistFieldCursorStructMg2577Hunk = Join-Path $outDir 'mui-color-specialist-field-cursor-struct-mg2577.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $colorSpecialistFieldCursorStructMg2577Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2577 Color specialist field cursor struct MC68000 execution qualification failed.' }

$externalWrapperFieldCursorStructMg2578Hunk = Join-Path $outDir 'mui-external-wrapper-field-cursor-struct-mg2578.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $externalWrapperFieldCursorStructMg2578Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2578 External wrapper field cursor struct MC68000 execution qualification failed.' }

$groupOrderingFieldCursorStructMg2579Hunk = Join-Path $outDir 'mui-group-ordering-field-cursor-struct-mg2579.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $groupOrderingFieldCursorStructMg2579Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2579 Group ordering field cursor struct MC68000 execution qualification failed.' }

$stringEditHookFieldCursorStructMg2580Hunk = Join-Path $outDir 'mui-string-edit-hook-field-cursor-struct-mg2580.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringEditHookFieldCursorStructMg2580Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2580 String edit-hook field cursor struct MC68000 execution qualification failed.' }

$stringInteractionFieldCursorStructMg2581Hunk = Join-Path $outDir 'mui-string-interaction-field-cursor-struct-mg2581.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringInteractionFieldCursorStructMg2581Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2581 String interaction field cursor struct MC68000 execution qualification failed.' }

$stringPresentationFieldCursorStructMg2582Hunk = Join-Path $outDir 'mui-string-presentation-field-cursor-struct-mg2582.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringPresentationFieldCursorStructMg2582Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2582 String presentation field cursor struct MC68000 execution qualification failed.' }

$stringScrollMetricsFieldCursorStructMg2583Hunk = Join-Path $outDir 'mui-string-scroll-metrics-field-cursor-struct-mg2583.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringScrollMetricsFieldCursorStructMg2583Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2583 String scroll-metrics field cursor struct MC68000 execution qualification failed.' }

$stringscrollPointerFieldCursorStructMg2584Hunk = Join-Path $outDir 'mui-stringscroll-pointer-field-cursor-struct-mg2584.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringscrollPointerFieldCursorStructMg2584Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2584 Stringscroll pointer-state field cursor struct MC68000 execution qualification failed.' }

$stringscrollStateFieldCursorStructMg2585Hunk = Join-Path $outDir 'mui-stringscroll-state-field-cursor-struct-mg2585.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringscrollStateFieldCursorStructMg2585Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2585 Stringscroll state field cursor struct MC68000 execution qualification failed.' }

$stringscrollPolicyFieldCursorStructMg2586Hunk = Join-Path $outDir 'mui-stringscroll-policy-field-cursor-struct-mg2586.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringscrollPolicyFieldCursorStructMg2586Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2586 Stringscroll policy field cursor struct MC68000 execution qualification failed.' }

$stringscrollScrollbarFieldCursorStructMg2587Hunk = Join-Path $outDir 'mui-stringscroll-scrollbar-field-cursor-struct-mg2587.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringscrollScrollbarFieldCursorStructMg2587Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2587 Stringscroll scrollbar field cursor struct MC68000 execution qualification failed.' }

$stringscrollCompositionFieldCursorStructMg2588Hunk = Join-Path $outDir 'mui-stringscroll-composition-field-cursor-struct-mg2588.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringscrollCompositionFieldCursorStructMg2588Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2588 Stringscroll composition field cursor struct MC68000 execution qualification failed.' }

$stringscrollLayoutFieldCursorStructMg2589Hunk = Join-Path $outDir 'mui-stringscroll-layout-field-cursor-struct-mg2589.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringscrollLayoutFieldCursorStructMg2589Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2589 Stringscroll layout field cursor struct MC68000 execution qualification failed.' }

$stringscrollRenderFieldCursorStructMg2590Hunk = Join-Path $outDir 'mui-stringscroll-render-field-cursor-struct-mg2590.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringscrollRenderFieldCursorStructMg2590Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2590 Stringscroll render field cursor struct MC68000 execution qualification failed.' }

$stringscrollViewportFieldCursorStructMg2591Hunk = Join-Path $outDir 'mui-stringscroll-viewport-field-cursor-struct-mg2591.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringscrollViewportFieldCursorStructMg2591Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2591 Stringscroll viewport field cursor struct MC68000 execution qualification failed.' }

$colorRecordFieldCursorStructMg2592Hunk = Join-Path $outDir 'mui-color-record-field-cursor-struct-mg2592.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $colorRecordFieldCursorStructMg2592Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2592 Color record field cursor struct MC68000 execution qualification failed.' }

$drawingRecordFieldCursorStructMg2593Hunk = Join-Path $outDir 'mui-drawing-record-field-cursor-struct-mg2593.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $drawingRecordFieldCursorStructMg2593Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2593 Drawing record field cursor struct MC68000 execution qualification failed.' }

$scrollgroupViewportFieldCursorStructMg2594Hunk = Join-Path $outDir 'mui-scrollgroup-viewport-field-cursor-struct-mg2594.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $scrollgroupViewportFieldCursorStructMg2594Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2594 Scrollgroup viewport field cursor struct MC68000 execution qualification failed.' }

$scrollgroupBorderScrollerFieldCursorStructMg2595Hunk = Join-Path $outDir 'mui-scrollgroup-border-scroller-field-cursor-struct-mg2595.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $scrollgroupBorderScrollerFieldCursorStructMg2595Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2595 Scrollgroup border-scroller field cursor struct MC68000 execution qualification failed.' }

$specializedLayoutFieldCursorStructMg2596Hunk = Join-Path $outDir 'mui-specialized-layout-field-cursor-struct-mg2596.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $specializedLayoutFieldCursorStructMg2596Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2596 Specialized layout field cursor struct MC68000 execution qualification failed.' }

$listtreeHeaderFieldCursorStructMg2597Hunk = Join-Path $outDir 'mui-listtree-header-field-cursor-struct-mg2597.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeHeaderFieldCursorStructMg2597Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2597 Listtree header field cursor struct MC68000 execution qualification failed.' }

$listtreeColumnGeometryFieldCursorStructMg2598Hunk = Join-Path $outDir 'mui-listtree-column-geometry-field-cursor-struct-mg2598.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeColumnGeometryFieldCursorStructMg2598Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2598 Listtree column geometry field cursor struct MC68000 execution qualification failed.' }

$listtreeDisplaySnapshotFieldCursorStructMg2599Hunk = Join-Path $outDir 'mui-listtree-display-snapshot-field-cursor-struct-mg2599.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeDisplaySnapshotFieldCursorStructMg2599Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2599 Listtree display snapshot field cursor struct MC68000 execution qualification failed.' }

$listtreePolicyFieldCursorStructMg2600Hunk = Join-Path $outDir 'mui-listtree-policy-field-cursor-struct-mg2600.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreePolicyFieldCursorStructMg2600Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2600 Listtree policy field cursor struct MC68000 execution qualification failed.' }

$listtreeHookPoolFieldCursorStructMg2601Hunk = Join-Path $outDir 'mui-listtree-hook-pool-field-cursor-struct-mg2601.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeHookPoolFieldCursorStructMg2601Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2601 Listtree hook-pool field cursor struct MC68000 execution qualification failed.' }

$listtreeClickStateFieldCursorStructMg2602Hunk = Join-Path $outDir 'mui-listtree-click-state-field-cursor-struct-mg2602.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeClickStateFieldCursorStructMg2602Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2602 Listtree click-state field cursor struct MC68000 execution qualification failed.' }

$listtreeClickColumnFieldCursorStructMg2603Hunk = Join-Path $outDir 'mui-listtree-click-column-field-cursor-struct-mg2603.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeClickColumnFieldCursorStructMg2603Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2603 Listtree click-column field cursor struct MC68000 execution qualification failed.' }

$listtreeSurfaceFieldCursorStructMg2604Hunk = Join-Path $outDir 'mui-listtree-surface-field-cursor-struct-mg2604.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeSurfaceFieldCursorStructMg2604Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2604 Listtree surface field cursor struct MC68000 execution qualification failed.' }

$listtreeLifecycleFieldCursorStructMg2605Hunk = Join-Path $outDir 'mui-listtree-lifecycle-field-cursor-struct-mg2605.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeLifecycleFieldCursorStructMg2605Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2605 Listtree lifecycle field cursor struct MC68000 execution qualification failed.' }

$listtreeNodeFieldCursorStructMg2606Hunk = Join-Path $outDir 'mui-listtree-node-field-cursor-struct-mg2606.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeNodeFieldCursorStructMg2606Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2606 Listtree node field cursor struct MC68000 execution qualification failed.' }

$listtreePresentationFieldCursorStructMg2607Hunk = Join-Path $outDir 'mui-listtree-presentation-field-cursor-struct-mg2607.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreePresentationFieldCursorStructMg2607Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2607 Listtree presentation field cursor struct MC68000 execution qualification failed.' }

$listtreeMessageFieldCursorStructMg2608Hunk = Join-Path $outDir 'mui-listtree-message-field-cursor-struct-mg2608.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeMessageFieldCursorStructMg2608Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2608 Listtree message field cursor struct MC68000 execution qualification failed.' }

$listtreeDisplayColumnFieldCursorStructMg2609Hunk = Join-Path $outDir 'mui-listtree-display-column-field-cursor-struct-mg2609.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeDisplayColumnFieldCursorStructMg2609Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2609 Listtree display-column field cursor struct MC68000 execution qualification failed.' }

$listtreeDisplayColumnVectorCursorStructMg2610Hunk = Join-Path $outDir 'mui-listtree-display-column-vector-cursor-struct-mg2610.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeDisplayColumnVectorCursorStructMg2610Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2610 Listtree display-column vector cursor struct MC68000 execution qualification failed.' }

$listtreeColumnGeometryVectorCursorStructMg2611Hunk = Join-Path $outDir 'mui-listtree-column-geometry-vector-cursor-struct-mg2611.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeColumnGeometryVectorCursorStructMg2611Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2611 Listtree column-geometry vector cursor struct MC68000 execution qualification failed.' }

$listviewChildFieldCursorStructMg2612Hunk = Join-Path $outDir 'mui-listview-child-field-cursor-struct-mg2612.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewChildFieldCursorStructMg2612Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2612 Listview child field cursor struct MC68000 execution qualification failed.' }

$listviewClickStateFieldCursorStructMg2613Hunk = Join-Path $outDir 'mui-listview-click-state-field-cursor-struct-mg2613.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewClickStateFieldCursorStructMg2613Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2613 Listview click-state field cursor struct MC68000 execution qualification failed.' }

$listviewSelectionSignalFieldCursorStructMg2614Hunk = Join-Path $outDir 'mui-listview-selection-signal-field-cursor-struct-mg2614.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewSelectionSignalFieldCursorStructMg2614Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2614 Listview selection-signal field cursor struct MC68000 execution qualification failed.' }

$listviewLayoutFieldCursorStructMg2615Hunk = Join-Path $outDir 'mui-listview-layout-field-cursor-struct-mg2615.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewLayoutFieldCursorStructMg2615Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2615 Listview layout field cursor struct MC68000 execution qualification failed.' }

$listviewRenderFieldCursorStructMg2616Hunk = Join-Path $outDir 'mui-listview-render-field-cursor-struct-mg2616.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewRenderFieldCursorStructMg2616Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2616 Listview render field cursor struct MC68000 execution qualification failed.' }

$listviewExternalConnectionFieldCursorStructMg2617Hunk = Join-Path $outDir 'mui-listview-external-connection-field-cursor-struct-mg2617.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewExternalConnectionFieldCursorStructMg2617Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2617 Listview external connection field cursor struct MC68000 execution qualification failed.' }

$listviewScrollerFieldCursorStructMg2618Hunk = Join-Path $outDir 'mui-listview-scroller-field-cursor-struct-mg2618.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewScrollerFieldCursorStructMg2618Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2618 Listview scroller field cursor struct MC68000 execution qualification failed.' }

$listviewInteractionPolicyFieldCursorStructMg2619Hunk = Join-Path $outDir 'mui-listview-interaction-policy-field-cursor-struct-mg2619.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewInteractionPolicyFieldCursorStructMg2619Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2619 Listview interaction-policy field cursor struct MC68000 execution qualification failed.' }

$listviewHorizontalScrollerFieldCursorStructMg2620Hunk = Join-Path $outDir 'mui-listview-horizontal-scroller-field-cursor-struct-mg2620.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewHorizontalScrollerFieldCursorStructMg2620Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2620 Listview horizontal-scroller field cursor struct MC68000 execution qualification failed.' }

$listviewHorizontalDragFieldCursorStructMg2621Hunk = Join-Path $outDir 'mui-listview-horizontal-drag-field-cursor-struct-mg2621.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewHorizontalDragFieldCursorStructMg2621Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2621 Listview horizontal drag field cursor struct MC68000 execution qualification failed.' }

$listviewVerticalDragFieldCursorStructMg2622Hunk = Join-Path $outDir 'mui-listview-vertical-drag-field-cursor-struct-mg2622.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewVerticalDragFieldCursorStructMg2622Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2622 Listview vertical drag field cursor struct MC68000 execution qualification failed.' }

$listviewOwnerFieldCursorStructMg2623Hunk = Join-Path $outDir 'mui-listview-owner-field-cursor-struct-mg2623.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewOwnerFieldCursorStructMg2623Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2623 Listview owner field cursor struct MC68000 execution qualification failed.' }

$listHScrollerFieldCursorStructMg2624Hunk = Join-Path $outDir 'mui-list-hscroller-field-cursor-struct-mg2624.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listHScrollerFieldCursorStructMg2624Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2624 List horizontal-scroller field cursor struct MC68000 execution qualification failed.' }

$listSlotFieldCursorStructMg2625Hunk = Join-Path $outDir 'mui-list-slot-field-cursor-struct-mg2625.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listSlotFieldCursorStructMg2625Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2625 List slot field cursor struct MC68000 execution qualification failed.' }

$listImageFieldCursorStructMg2626Hunk = Join-Path $outDir 'mui-list-image-field-cursor-struct-mg2626.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listImageFieldCursorStructMg2626Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2626 List image field cursor struct MC68000 execution qualification failed.' }

$listTitleArrayFieldCursorStructMg2627Hunk = Join-Path $outDir 'mui-list-title-array-field-cursor-struct-mg2627.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listTitleArrayFieldCursorStructMg2627Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2627 List title-array field cursor struct MC68000 execution qualification failed.' }

$listTitleFieldCursorStructMg2628Hunk = Join-Path $outDir 'mui-list-title-field-cursor-struct-mg2628.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listTitleFieldCursorStructMg2628Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2628 List title field cursor struct MC68000 execution qualification failed.' }

$listSelectionSignalFieldCursorStructMg2629Hunk = Join-Path $outDir 'mui-list-selection-signal-field-cursor-struct-mg2629.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listSelectionSignalFieldCursorStructMg2629Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2629 List selection-signal field cursor struct MC68000 execution qualification failed.' }

$listFormatPolicyFieldCursorStructMg2630Hunk = Join-Path $outDir 'mui-list-format-policy-field-cursor-struct-mg2630.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listFormatPolicyFieldCursorStructMg2630Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2630 List format-policy field cursor struct MC68000 execution qualification failed.' }

$listFontFieldCursorStructMg2631Hunk = Join-Path $outDir 'mui-list-font-field-cursor-struct-mg2631.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listFontFieldCursorStructMg2631Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2631 List font field cursor struct MC68000 execution qualification failed.' }

$listPointerSlotFieldCursorStructMg2632Hunk = Join-Path $outDir 'mui-list-pointer-slot-field-cursor-struct-mg2632.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listPointerSlotFieldCursorStructMg2632Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2632 List pointer-slot field cursor struct MC68000 execution qualification failed.' }

$listEditFieldCursorStructMg2633Hunk = Join-Path $outDir 'mui-list-edit-field-cursor-struct-mg2633.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listEditFieldCursorStructMg2633Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2633 List edit field cursor struct MC68000 execution qualification failed.' }

$listColumnGeometryFieldCursorStructMg2634Hunk = Join-Path $outDir 'mui-list-column-geometry-field-cursor-struct-mg2634.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listColumnGeometryFieldCursorStructMg2634Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2634 List column-geometry field cursor struct MC68000 execution qualification failed.' }

$listColumnLayoutFieldCursorStructMg2635Hunk = Join-Path $outDir 'mui-list-column-layout-field-cursor-struct-mg2635.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listColumnLayoutFieldCursorStructMg2635Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2635 List column-layout field cursor struct MC68000 execution qualification failed.' }

$listOwnedRecordHeaderFieldCursorStructMg2636Hunk = Join-Path $outDir 'mui-list-owned-record-header-field-cursor-struct-mg2636.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listOwnedRecordHeaderFieldCursorStructMg2636Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2636 List owned-record-header field cursor struct MC68000 execution qualification failed.' }

$listRedrawFieldCursorStructMg2637Hunk = Join-Path $outDir 'mui-list-redraw-field-cursor-struct-mg2637.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listRedrawFieldCursorStructMg2637Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2637 List redraw field cursor struct MC68000 execution qualification failed.' }

$listActiveFieldCursorStructMg2638Hunk = Join-Path $outDir 'mui-list-active-field-cursor-struct-mg2638.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listActiveFieldCursorStructMg2638Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2638 List active field cursor struct MC68000 execution qualification failed.' }

$listInsertPositionFieldCursorStructMg2639Hunk = Join-Path $outDir 'mui-list-insert-position-field-cursor-struct-mg2639.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listInsertPositionFieldCursorStructMg2639Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2639 List insert-position field cursor struct MC68000 execution qualification failed.' }

$listViewportFieldCursorStructMg2640Hunk = Join-Path $outDir 'mui-list-viewport-field-cursor-struct-mg2640.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listViewportFieldCursorStructMg2640Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2640 List viewport field cursor struct MC68000 execution qualification failed.' }

$listInteractionPolicyFieldCursorStructMg2641Hunk = Join-Path $outDir 'mui-list-interaction-policy-field-cursor-struct-mg2641.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listInteractionPolicyFieldCursorStructMg2641Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2641 List interaction-policy field cursor struct MC68000 execution qualification failed.' }

$listClickFieldCursorStructMg2642Hunk = Join-Path $outDir 'mui-list-click-field-cursor-struct-mg2642.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listClickFieldCursorStructMg2642Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2642 List click field cursor struct MC68000 execution qualification failed.' }

$listHookPolicyFieldCursorStructMg2643Hunk = Join-Path $outDir 'mui-list-hook-policy-field-cursor-struct-mg2643.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listHookPolicyFieldCursorStructMg2643Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2643 List hook-policy field cursor struct MC68000 execution qualification failed.' }

$listSortFieldCursorStructMg2644Hunk = Join-Path $outDir 'mui-list-sort-field-cursor-struct-mg2644.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listSortFieldCursorStructMg2644Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2644 List sort field cursor struct MC68000 execution qualification failed.' }

$listPoolPolicyFieldCursorStructMg2645Hunk = Join-Path $outDir 'mui-list-pool-policy-field-cursor-struct-mg2645.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listPoolPolicyFieldCursorStructMg2645Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2645 List pool-policy field cursor struct MC68000 execution qualification failed.' }

$listPresentationPolicyFieldCursorStructMg2646Hunk = Join-Path $outDir 'mui-list-presentation-policy-field-cursor-struct-mg2646.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listPresentationPolicyFieldCursorStructMg2646Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2646 List presentation-policy field cursor struct MC68000 execution qualification failed.' }

$listHeaderFieldCursorStructMg2647Hunk = Join-Path $outDir 'mui-list-header-field-cursor-struct-mg2647.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listHeaderFieldCursorStructMg2647Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2647 List header field cursor struct MC68000 execution qualification failed.' }

$listColumnVisibilityFieldCursorStructMg2648Hunk = Join-Path $outDir 'mui-list-column-visibility-field-cursor-struct-mg2648.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listColumnVisibilityFieldCursorStructMg2648Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2648 List column-visibility field cursor struct MC68000 execution qualification failed.' }

$listColumnOrderFieldCursorStructMg2649Hunk = Join-Path $outDir 'mui-list-column-order-field-cursor-struct-mg2649.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listColumnOrderFieldCursorStructMg2649Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2649 List column-order field cursor struct MC68000 execution qualification failed.' }

$listFormatDescriptorFieldCursorStructMg2650Hunk = Join-Path $outDir 'mui-list-format-descriptor-field-cursor-struct-mg2650.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listFormatDescriptorFieldCursorStructMg2650Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2650 List FORMAT descriptor field cursor struct MC68000 execution qualification failed.' }

$listFormatDescriptorStateFieldCursorStructMg2651Hunk = Join-Path $outDir 'mui-list-format-descriptor-state-field-cursor-struct-mg2651.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listFormatDescriptorStateFieldCursorStructMg2651Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2651 List FORMAT descriptor state field cursor struct MC68000 execution qualification failed.' }

$listColumnMetricsFieldCursorStructMg2652Hunk = Join-Path $outDir 'mui-list-column-metrics-field-cursor-struct-mg2652.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listColumnMetricsFieldCursorStructMg2652Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2652 List column-metrics field cursor struct MC68000 execution qualification failed.' }

$listStateFieldCursorStructMg2653Hunk = Join-Path $outDir 'mui-list-state-field-cursor-struct-mg2653.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listStateFieldCursorStructMg2653Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2653 aggregate List-state field cursor struct MC68000 execution qualification failed.' }

$storePoolIterationFieldCursorStructMg2654Hunk = Join-Path $outDir 'mui-store-pool-iteration-field-cursor-struct-mg2654.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $storePoolIterationFieldCursorStructMg2654Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2654 Store pool/iteration field cursor struct MC68000 execution qualification failed.' }

$stringInteger64FieldCursorStructMg2655Hunk = Join-Path $outDir 'mui-string-integer64-field-cursor-struct-mg2655.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringInteger64FieldCursorStructMg2655Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2655 String Integer64 field cursor struct MC68000 execution qualification failed.' }

$windowEventStateFieldCursorStructMg2656Hunk = Join-Path $outDir 'mui-window-event-state-field-cursor-struct-mg2656.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowEventStateFieldCursorStructMg2656Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2656 Window event-state field cursor struct MC68000 execution qualification failed.' }

$windowEventReuseStateFieldCursorStructMg2657Hunk = Join-Path $outDir 'mui-window-event-reuse-state-field-cursor-struct-mg2657.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowEventReuseStateFieldCursorStructMg2657Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2657 Window EventReuse field cursor struct MC68000 execution qualification failed.' }

$windowLifecycleStateFieldCursorStructMg2658Hunk = Join-Path $outDir 'mui-window-lifecycle-state-field-cursor-struct-mg2658.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowLifecycleStateFieldCursorStructMg2658Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2658 Window lifecycle-state field cursor struct MC68000 execution qualification failed.' }

$helpStateFieldCursorStructMg2659Hunk = Join-Path $outDir 'mui-help-state-field-cursor-struct-mg2659.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $helpStateFieldCursorStructMg2659Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2659 Help-state field cursor struct MC68000 execution qualification failed.' }

$textPresentationStateFieldCursorStructMg2660Hunk = Join-Path $outDir 'mui-text-presentation-state-field-cursor-struct-mg2660.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $textPresentationStateFieldCursorStructMg2660Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2660 Text presentation-state field cursor struct MC68000 execution qualification failed.' }

$specialistHookMessageFieldCursorStructMg2661Hunk = Join-Path $outDir 'mui-specialist-hook-message-field-cursor-struct-mg2661.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $specialistHookMessageFieldCursorStructMg2661Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2661 Specialist hook-message field cursor struct MC68000 execution qualification failed.' }

$virtgroupPolicyFieldCursorStructMg2662Hunk = Join-Path $outDir 'mui-virtgroup-policy-field-cursor-struct-mg2662.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $virtgroupPolicyFieldCursorStructMg2662Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2662 Virtgroup policy field cursor struct MC68000 execution qualification failed.' }

$textShortenedStateFieldCursorStructMg2663Hunk = Join-Path $outDir 'mui-text-shortened-state-field-cursor-struct-mg2663.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $textShortenedStateFieldCursorStructMg2663Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2663 Text shortened-state field cursor struct MC68000 execution qualification failed.' }

$balancePolicyFieldCursorStructMg2664Hunk = Join-Path $outDir 'mui-balance-policy-field-cursor-struct-mg2664.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $balancePolicyFieldCursorStructMg2664Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2664 Balance policy field cursor struct MC68000 execution qualification failed.' }

$bitmapGeometryFieldCursorStructMg2665Hunk = Join-Path $outDir 'mui-bitmap-geometry-field-cursor-struct-mg2665.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $bitmapGeometryFieldCursorStructMg2665Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2665 Bitmap geometry field cursor struct MC68000 execution qualification failed.' }

$bitmapRemappedFieldCursorStructMg2666Hunk = Join-Path $outDir 'mui-bitmap-remapped-field-cursor-struct-mg2666.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $bitmapRemappedFieldCursorStructMg2666Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2666 Bitmap remapped field cursor struct MC68000 execution qualification failed.' }

$bitmapSourceFieldCursorStructMg2667Hunk = Join-Path $outDir 'mui-bitmap-source-field-cursor-struct-mg2667.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $bitmapSourceFieldCursorStructMg2667Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2667 Bitmap source field cursor struct MC68000 execution qualification failed.' }

$bitmapPolicyFieldCursorStructMg2668Hunk = Join-Path $outDir 'mui-bitmap-policy-field-cursor-struct-mg2668.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $bitmapPolicyFieldCursorStructMg2668Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2668 Bitmap policy field cursor struct MC68000 execution qualification failed.' }

$bodychunkFormatFieldCursorStructMg2669Hunk = Join-Path $outDir 'mui-bodychunk-format-field-cursor-struct-mg2669.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $bodychunkFormatFieldCursorStructMg2669Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2669 Bodychunk format field cursor struct MC68000 execution qualification failed.' }

$choiceActiveFieldCursorStructMg2670Hunk = Join-Path $outDir 'mui-choice-active-field-cursor-struct-mg2670.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $choiceActiveFieldCursorStructMg2670Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2670 Choice active field cursor struct MC68000 execution qualification failed.' }

$choiceEntriesFieldCursorStructMg2671Hunk = Join-Path $outDir 'mui-choice-entries-field-cursor-struct-mg2671.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $choiceEntriesFieldCursorStructMg2671Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2671 Choice entries field cursor struct MC68000 execution qualification failed.' }

$controlFontFieldCursorStructMg2672Hunk = Join-Path $outDir 'mui-control-font-field-cursor-struct-mg2672.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $controlFontFieldCursorStructMg2672Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2672 ControlFont field cursor struct MC68000 execution qualification failed.' }

$controlFontResolutionFieldCursorStructMg2673Hunk = Join-Path $outDir 'mui-control-font-resolution-field-cursor-struct-mg2673.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $controlFontResolutionFieldCursorStructMg2673Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2673 ControlFont resolution field cursor struct MC68000 execution qualification failed.' }

$gadgetGadgetFieldCursorStructMg2674Hunk = Join-Path $outDir 'mui-gadget-gadget-field-cursor-struct-mg2674.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $gadgetGadgetFieldCursorStructMg2674Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2674 Gadget_Gadget field cursor struct MC68000 execution qualification failed.' }

$gadgetInteractionFieldCursorStructMg2675Hunk = Join-Path $outDir 'mui-gadget-interaction-field-cursor-struct-mg2675.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $gadgetInteractionFieldCursorStructMg2675Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2675 Gadget interaction field cursor struct MC68000 execution qualification failed.' }

$gaugeInfoRateFieldCursorStructMg2676Hunk = Join-Path $outDir 'mui-gauge-inforate-field-cursor-struct-mg2676.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $gaugeInfoRateFieldCursorStructMg2676Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2676 Gauge.InfoRate field cursor struct MC68000 execution qualification failed.' }

$gaugeInfoTextFieldCursorStructMg2677Hunk = Join-Path $outDir 'mui-gauge-infotext-field-cursor-struct-mg2677.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $gaugeInfoTextFieldCursorStructMg2677Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2677 Gauge.InfoText field cursor struct MC68000 execution qualification failed.' }

$gaugeFieldCursorStructMg2678Hunk = Join-Path $outDir 'mui-gauge-field-cursor-struct-mg2678.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $gaugeFieldCursorStructMg2678Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2678 Gauge field cursor struct MC68000 execution qualification failed.' }

$groupLayoutHookFieldCursorStructMg2679Hunk = Join-Path $outDir 'mui-group-layout-hook-field-cursor-struct-mg2679.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $groupLayoutHookFieldCursorStructMg2679Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2679 Group.LayoutHook field cursor struct MC68000 execution qualification failed.' }

$groupLayoutPolicyFieldCursorStructMg2680Hunk = Join-Path $outDir 'mui-group-layout-policy-field-cursor-struct-mg2680.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $groupLayoutPolicyFieldCursorStructMg2680Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2680 Group layout-policy field cursor struct MC68000 execution qualification failed.' }

$groupGridFieldCursorStructMg2681Hunk = Join-Path $outDir 'mui-group-grid-field-cursor-struct-mg2681.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $groupGridFieldCursorStructMg2681Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2681 Group grid field cursor struct MC68000 execution qualification failed.' }

$groupPageFieldCursorStructMg2682Hunk = Join-Path $outDir 'mui-group-page-field-cursor-struct-mg2682.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $groupPageFieldCursorStructMg2682Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2682 Group ActivePage field cursor struct MC68000 execution qualification failed.' }

$imageRenderFieldCursorStructMg2683Hunk = Join-Path $outDir 'mui-image-render-field-cursor-struct-mg2683.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $imageRenderFieldCursorStructMg2683Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2683 Image render field cursor struct MC68000 execution qualification failed.' }

$imageOldImageFieldCursorStructMg2684Hunk = Join-Path $outDir 'mui-image-oldimage-field-cursor-struct-mg2684.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $imageOldImageFieldCursorStructMg2684Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2684 Image OldImage field cursor struct MC68000 execution qualification failed.' }

$imageFontMatchStringFieldCursorStructMg2685Hunk = Join-Path $outDir 'mui-image-fontmatchstring-field-cursor-struct-mg2685.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $imageFontMatchStringFieldCursorStructMg2685Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2685 Image FontMatchString field cursor struct MC68000 execution qualification failed.' }

$imageFontMatchFieldCursorStructMg2686Hunk = Join-Path $outDir 'mui-image-fontmatch-field-cursor-struct-mg2686.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $imageFontMatchFieldCursorStructMg2686Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2686 Image FontMatch field cursor struct MC68000 execution qualification failed.' }

$imageSpecFieldCursorStructMg2687Hunk = Join-Path $outDir 'mui-image-spec-field-cursor-struct-mg2687.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $imageSpecFieldCursorStructMg2687Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2687 Image spec field cursor struct MC68000 execution qualification failed.' }

$levelmeterLabelFieldCursorStructMg2688Hunk = Join-Path $outDir 'mui-levelmeter-label-field-cursor-struct-mg2688.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $levelmeterLabelFieldCursorStructMg2688Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2688 Levelmeter label field cursor struct MC68000 execution qualification failed.' }

$levelmeterPresentationFieldCursorStructMg2689Hunk = Join-Path $outDir 'mui-levelmeter-presentation-field-cursor-struct-mg2689.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $levelmeterPresentationFieldCursorStructMg2689Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2689 Levelmeter presentation field cursor struct MC68000 execution qualification failed.' }

$numericFormatFieldCursorStructMg2690Hunk = Join-Path $outDir 'mui-numeric-format-field-cursor-struct-mg2690.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $numericFormatFieldCursorStructMg2690Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2690 Numeric format field cursor struct MC68000 execution qualification failed.' }

$numericStateFieldCursorStructMg2691Hunk = Join-Path $outDir 'mui-numeric-state-field-cursor-struct-mg2691.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $numericStateFieldCursorStructMg2691Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2691 Numeric state field cursor struct MC68000 execution qualification failed.' }

$propRangeFieldCursorStructMg2692Hunk = Join-Path $outDir 'mui-prop-range-field-cursor-struct-mg2692.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $propRangeFieldCursorStructMg2692Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2692 Prop range field cursor struct MC68000 execution qualification failed.' }

$propPolicyFieldCursorStructMg2693Hunk = Join-Path $outDir 'mui-prop-policy-field-cursor-struct-mg2693.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $propPolicyFieldCursorStructMg2693Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2693 Prop policy field cursor struct MC68000 execution qualification failed.' }

$scrollbarLayoutFieldCursorStructMg2694Hunk = Join-Path $outDir 'mui-scrollbar-layout-field-cursor-struct-mg2694.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $scrollbarLayoutFieldCursorStructMg2694Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2694 Scrollbar layout field cursor struct MC68000 execution qualification failed.' }

$sliderPresentationFieldCursorStructMg2695Hunk = Join-Path $outDir 'mui-slider-presentation-field-cursor-struct-mg2695.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $sliderPresentationFieldCursorStructMg2695Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2695 Slider presentation field cursor struct MC68000 execution qualification failed.' }

$rectangleBarTitleFieldCursorStructMg2696Hunk = Join-Path $outDir 'mui-rectangle-bartitle-field-cursor-struct-mg2696.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $rectangleBarTitleFieldCursorStructMg2696Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2696 Rectangle bar-title field cursor struct MC68000 execution qualification failed.' }

$rectanglePresentationFieldCursorStructMg2697Hunk = Join-Path $outDir 'mui-rectangle-presentation-field-cursor-struct-mg2697.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $rectanglePresentationFieldCursorStructMg2697Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2697 Rectangle presentation field cursor struct MC68000 execution qualification failed.' }

$registerPolicyFieldCursorStructMg2698Hunk = Join-Path $outDir 'mui-register-policy-field-cursor-struct-mg2698.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $registerPolicyFieldCursorStructMg2698Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2698 Register policy field cursor struct MC68000 execution qualification failed.' }

$requesterServiceStateFieldCursorStructMg2699Hunk = Join-Path $outDir 'mui-requester-service-state-field-cursor-struct-mg2699.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $requesterServiceStateFieldCursorStructMg2699Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2699 Requester service state field cursor struct MC68000 execution qualification failed.' }

$selectgroupActiveFieldCursorStructMg2700Hunk = Join-Path $outDir 'mui-selectgroup-active-field-cursor-struct-mg2700.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $selectgroupActiveFieldCursorStructMg2700Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2700 Selectgroup active field cursor struct MC68000 execution qualification failed.' }

$scrollgroupViewportStateFieldCursorStructMg2701Hunk = Join-Path $outDir 'mui-scrollgroup-viewport-state-field-cursor-struct-mg2701.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $scrollgroupViewportStateFieldCursorStructMg2701Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2701 Scrollgroup viewport state field cursor struct MC68000 execution qualification failed.' }

$scrollgroupBorderScrollerStateFieldCursorStructMg2702Hunk = Join-Path $outDir 'mui-scrollgroup-border-scroller-state-field-cursor-struct-mg2702.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $scrollgroupBorderScrollerStateFieldCursorStructMg2702Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2702 Scrollgroup border-scroller state field cursor struct MC68000 execution qualification failed.' }

$scrollgroupPolicyStateFieldCursorStructMg2703Hunk = Join-Path $outDir 'mui-scrollgroup-policy-state-field-cursor-struct-mg2703.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $scrollgroupPolicyStateFieldCursorStructMg2703Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2703 Scrollgroup policy state field cursor struct MC68000 execution qualification failed.' }

$scalePresentationFieldCursorStructMg2704Hunk = Join-Path $outDir 'mui-scale-presentation-field-cursor-struct-mg2704.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $scalePresentationFieldCursorStructMg2704Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2704 Scale presentation field cursor struct MC68000 execution qualification failed.' }

$sleepStateFieldCursorStructMg2705Hunk = Join-Path $outDir 'mui-sleep-state-field-cursor-struct-mg2705.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $sleepStateFieldCursorStructMg2705Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2705 Sleep state field cursor struct MC68000 execution qualification failed.' }

$stringAcknowledgeStateFieldCursorStructMg2706Hunk = Join-Path $outDir 'mui-string-acknowledge-state-field-cursor-struct-mg2706.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringAcknowledgeStateFieldCursorStructMg2706Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2706 String acknowledgement state field cursor struct MC68000 execution qualification failed.' }

$stringAttachedListStateFieldCursorStructMg2707Hunk = Join-Path $outDir 'mui-string-attached-list-state-field-cursor-struct-mg2707.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringAttachedListStateFieldCursorStructMg2707Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2707 String attached-list state field cursor struct MC68000 execution qualification failed.' }

$stringContentsStateFieldCursorStructMg2708Hunk = Join-Path $outDir 'mui-string-contents-state-field-cursor-struct-mg2708.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringContentsStateFieldCursorStructMg2708Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2708 String contents state field cursor struct MC68000 execution qualification failed.' }

$stringCursorStateFieldCursorStructMg2709Hunk = Join-Path $outDir 'mui-string-cursor-state-field-cursor-struct-mg2709.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringCursorStateFieldCursorStructMg2709Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2709 String cursor state field cursor struct MC68000 execution qualification failed.' }

$stringFilterStateFieldCursorStructMg2710Hunk = Join-Path $outDir 'mui-string-filter-state-field-cursor-struct-mg2710.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringFilterStateFieldCursorStructMg2710Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2710 String filter state field cursor struct MC68000 execution qualification failed.' }

$stringIntegerStateFieldCursorStructMg2711Hunk = Join-Path $outDir 'mui-string-integer-state-field-cursor-struct-mg2711.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringIntegerStateFieldCursorStructMg2711Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2711 String integer state field cursor struct MC68000 execution qualification failed.' }

$stringPlaceholderStateFieldCursorStructMg2712Hunk = Join-Path $outDir 'mui-string-placeholder-state-field-cursor-struct-mg2712.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringPlaceholderStateFieldCursorStructMg2712Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2712 String placeholder state field cursor struct MC68000 execution qualification failed.' }

$stringSpellCheckingStateFieldCursorStructMg2713Hunk = Join-Path $outDir 'mui-string-spell-checking-state-field-cursor-struct-mg2713.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringSpellCheckingStateFieldCursorStructMg2713Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2713 String spell-checking state field cursor struct MC68000 execution qualification failed.' }

$stringPresentationStateFieldCursorStructMg2714Hunk = Join-Path $outDir 'mui-string-presentation-state-field-cursor-struct-mg2714.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringPresentationStateFieldCursorStructMg2714Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2714 String presentation state field cursor struct MC68000 execution qualification failed.' }

$stringScrollMetricsStateFieldCursorStructMg2715Hunk = Join-Path $outDir 'mui-string-scroll-metrics-state-field-cursor-struct-mg2715.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringScrollMetricsStateFieldCursorStructMg2715Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2715 String scroll metrics state field cursor struct MC68000 execution qualification failed.' }

$stringEditHookStateFieldCursorStructMg2716Hunk = Join-Path $outDir 'mui-string-edit-hook-state-field-cursor-struct-mg2716.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringEditHookStateFieldCursorStructMg2716Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2716 String edit-hook state field cursor struct MC68000 execution qualification failed.' }

$stringInteractionStateFieldCursorStructMg2717Hunk = Join-Path $outDir 'mui-string-interaction-state-field-cursor-struct-mg2717.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringInteractionStateFieldCursorStructMg2717Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2717 String interaction state field cursor struct MC68000 execution qualification failed.' }

$stringscrollPointerStateFieldCursorStructMg2718Hunk = Join-Path $outDir 'mui-stringscroll-pointer-state-field-cursor-struct-mg2718.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringscrollPointerStateFieldCursorStructMg2718Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2718 Stringscroll pointer state field cursor struct MC68000 execution qualification failed.' }

$stringscrollStateFieldCursorStructMg2719Hunk = Join-Path $outDir 'mui-stringscroll-state-field-cursor-struct-mg2719.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringscrollStateFieldCursorStructMg2719Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2719 Stringscroll state field cursor struct MC68000 execution qualification failed.' }

$stringscrollPolicyFieldCursorStructMg2720Hunk = Join-Path $outDir 'mui-stringscroll-policy-field-cursor-struct-mg2720.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringscrollPolicyFieldCursorStructMg2720Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2720 Stringscroll policy field cursor struct MC68000 execution qualification failed.' }

$stringscrollScrollbarFieldCursorStructMg2721Hunk = Join-Path $outDir 'mui-stringscroll-scrollbar-field-cursor-struct-mg2721.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringscrollScrollbarFieldCursorStructMg2721Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2721 Stringscroll scrollbar field cursor struct MC68000 execution qualification failed.' }

$stringscrollCompositionFieldCursorStructMg2722Hunk = Join-Path $outDir 'mui-stringscroll-composition-field-cursor-struct-mg2722.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringscrollCompositionFieldCursorStructMg2722Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2722 Stringscroll composition field cursor struct MC68000 execution qualification failed.' }

$stringscrollLayoutFieldCursorStructMg2723Hunk = Join-Path $outDir 'mui-stringscroll-layout-field-cursor-struct-mg2723.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringscrollLayoutFieldCursorStructMg2723Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2723 Stringscroll layout field cursor struct MC68000 execution qualification failed.' }

$stringscrollRenderFieldCursorStructMg2724Hunk = Join-Path $outDir 'mui-stringscroll-render-field-cursor-struct-mg2724.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringscrollRenderFieldCursorStructMg2724Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2724 Stringscroll render field cursor struct MC68000 execution qualification failed.' }

$stringscrollViewportFieldCursorStructMg2725Hunk = Join-Path $outDir 'mui-stringscroll-viewport-field-cursor-struct-mg2725.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringscrollViewportFieldCursorStructMg2725Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2725 Stringscroll viewport field cursor struct MC68000 execution qualification failed.' }

$textContentsStateFieldCursorStructMg2726Hunk = Join-Path $outDir 'mui-text-contents-state-field-cursor-struct-mg2726.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $textContentsStateFieldCursorStructMg2726Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2726 Text contents state field cursor struct MC68000 execution qualification failed.' }

$textPreParseStateFieldCursorStructMg2727Hunk = Join-Path $outDir 'mui-text-preparse-state-field-cursor-struct-mg2727.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $textPreParseStateFieldCursorStructMg2727Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2727 Text PreParse state field cursor struct MC68000 execution qualification failed.' }

$textUnicodeStateFieldCursorStructMg2728Hunk = Join-Path $outDir 'mui-text-unicode-state-field-cursor-struct-mg2728.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $textUnicodeStateFieldCursorStructMg2728Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2728 Text Unicode state field cursor struct MC68000 execution qualification failed.' }

$textCopyStateFieldCursorStructMg2729Hunk = Join-Path $outDir 'mui-text-copy-state-field-cursor-struct-mg2729.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $textCopyStateFieldCursorStructMg2729Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2729 Text Copy state field cursor struct MC68000 execution qualification failed.' }

$areaActivationStateFieldCursorStructMg2730Hunk = Join-Path $outDir 'mui-area-activation-state-field-cursor-struct-mg2730.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaActivationStateFieldCursorStructMg2730Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2730 Area activation state field cursor struct MC68000 execution qualification failed.' }

$areaControlCharStateFieldCursorStructMg2731Hunk = Join-Path $outDir 'mui-area-control-char-state-field-cursor-struct-mg2731.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaControlCharStateFieldCursorStructMg2731Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2731 Area ControlChar state field cursor struct MC68000 execution qualification failed.' }

$areaDoubleBufferStateFieldCursorStructMg2732Hunk = Join-Path $outDir 'mui-area-double-buffer-state-field-cursor-struct-mg2732.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaDoubleBufferStateFieldCursorStructMg2732Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2732 Area double-buffer state field cursor struct MC68000 execution qualification failed.' }

$areaDisappearPolicyStateFieldCursorStructMg2733Hunk = Join-Path $outDir 'mui-area-disappear-policy-state-field-cursor-struct-mg2733.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaDisappearPolicyStateFieldCursorStructMg2733Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2733 Area disappearance-policy state field cursor struct MC68000 execution qualification failed.' }

$areaCycleChainStateFieldCursorStructMg2734Hunk = Join-Path $outDir 'mui-area-cycle-chain-state-field-cursor-struct-mg2734.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaCycleChainStateFieldCursorStructMg2734Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2734 Area CycleChain state field cursor struct MC68000 execution qualification failed.' }

$areaDoubleClickStateFieldCursorStructMg2735Hunk = Join-Path $outDir 'mui-area-double-click-state-field-cursor-struct-mg2735.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaDoubleClickStateFieldCursorStructMg2735Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2735 Area DoubleClick state field cursor struct MC68000 execution qualification failed.' }

$areaBuiltinFontStateFieldCursorStructMg2736Hunk = Join-Path $outDir 'mui-area-builtin-font-state-field-cursor-struct-mg2736.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaBuiltinFontStateFieldCursorStructMg2736Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2736 Area BuiltinFont state field cursor struct MC68000 execution qualification failed.' }

$areaContextMenuStateFieldCursorStructMg2737Hunk = Join-Path $outDir 'mui-area-context-menu-state-field-cursor-struct-mg2737.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaContextMenuStateFieldCursorStructMg2737Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2737 Area ContextMenu state field cursor struct MC68000 execution qualification failed.' }

$areaCustomFontStateFieldCursorStructMg2738Hunk = Join-Path $outDir 'mui-area-custom-font-state-field-cursor-struct-mg2738.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaCustomFontStateFieldCursorStructMg2738Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2738 Area CustomFont state field cursor struct MC68000 execution qualification failed.' }

$areaCustomFontRuntimeStateFieldCursorStructMg2739Hunk = Join-Path $outDir 'mui-area-custom-font-runtime-state-field-cursor-struct-mg2739.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaCustomFontRuntimeStateFieldCursorStructMg2739Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2739 Area CustomFont runtime state field cursor struct MC68000 execution qualification failed.' }

$areaDragPolicyStateFieldCursorStructMg2740Hunk = Join-Path $outDir 'mui-area-drag-policy-state-field-cursor-struct-mg2740.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaDragPolicyStateFieldCursorStructMg2740Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2740 Area drag-policy state field cursor struct MC68000 execution qualification failed.' }

$areaDragStateFieldCursorStructMg2741Hunk = Join-Path $outDir 'mui-area-drag-state-field-cursor-struct-mg2741.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaDragStateFieldCursorStructMg2741Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2741 Area drag state field cursor struct MC68000 execution qualification failed.' }

$areaGeometryStateFieldCursorStructMg2742Hunk = Join-Path $outDir 'mui-area-geometry-state-field-cursor-struct-mg2742.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaGeometryStateFieldCursorStructMg2742Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2742 Area geometry state field cursor struct MC68000 execution qualification failed.' }

$areaLayoutPolicyStateFieldCursorStructMg2743Hunk = Join-Path $outDir 'mui-area-layout-policy-state-field-cursor-struct-mg2743.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaLayoutPolicyStateFieldCursorStructMg2743Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2743 Area layout-policy state field cursor struct MC68000 execution qualification failed.' }

$areaPresentationStateFieldCursorStructMg2744Hunk = Join-Path $outDir 'mui-area-presentation-state-field-cursor-struct-mg2744.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaPresentationStateFieldCursorStructMg2744Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2744 Area presentation state field cursor struct MC68000 execution qualification failed.' }

$areaRenderPolicyStateFieldCursorStructMg2745Hunk = Join-Path $outDir 'mui-area-render-policy-state-field-cursor-struct-mg2745.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaRenderPolicyStateFieldCursorStructMg2745Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2745 Area render-policy state field cursor struct MC68000 execution qualification failed.' }

$areaFixedTextStateFieldCursorStructMg2746Hunk = Join-Path $outDir 'mui-area-fixed-text-state-field-cursor-struct-mg2746.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaFixedTextStateFieldCursorStructMg2746Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2746 Area fixed-text state field cursor struct MC68000 execution qualification failed.' }

$areaFloatingStateFieldCursorStructMg2747Hunk = Join-Path $outDir 'mui-area-floating-state-field-cursor-struct-mg2747.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaFloatingStateFieldCursorStructMg2747Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2747 Area floating state field cursor struct MC68000 execution qualification failed.' }

$areaFontSelectionStateFieldCursorStructMg2748Hunk = Join-Path $outDir 'mui-area-font-selection-state-field-cursor-struct-mg2748.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaFontSelectionStateFieldCursorStructMg2748Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2748 Area font-selection state field cursor struct MC68000 execution qualification failed.' }

$areaTextColorStateFieldCursorStructMg2749Hunk = Join-Path $outDir 'mui-area-text-color-state-field-cursor-struct-mg2749.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaTextColorStateFieldCursorStructMg2749Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2749 Area TextColor state field cursor struct MC68000 execution qualification failed.' }

$areaTimerStateFieldCursorStructMg2750Hunk = Join-Path $outDir 'mui-area-timer-state-field-cursor-struct-mg2750.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaTimerStateFieldCursorStructMg2750Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2750 Area timer state field cursor struct MC68000 execution qualification failed.' }

$areaTimerEventStateFieldCursorStructMg2751Hunk = Join-Path $outDir 'mui-area-timer-event-state-field-cursor-struct-mg2751.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaTimerEventStateFieldCursorStructMg2751Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2751 Area timer-event state field cursor struct MC68000 execution qualification failed.' }

$areaWeightStateFieldCursorStructMg2752Hunk = Join-Path $outDir 'mui-area-weight-state-field-cursor-struct-mg2752.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaWeightStateFieldCursorStructMg2752Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2752 Area weight state field cursor struct MC68000 execution qualification failed.' }

$areaShortHelpStateFieldCursorStructMg2753Hunk = Join-Path $outDir 'mui-area-short-help-state-field-cursor-struct-mg2753.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaShortHelpStateFieldCursorStructMg2753Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2753 Area ShortHelp state field cursor struct MC68000 execution qualification failed.' }

$areaActivationMessageFieldCursorStructMg2754Hunk = Join-Path $outDir 'mui-area-activation-message-field-cursor-struct-mg2754.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaActivationMessageFieldCursorStructMg2754Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2754 Area activation message field cursor struct MC68000 execution qualification failed.' }

$areaBubbleMessageFieldCursorStructMg2755Hunk = Join-Path $outDir 'mui-area-bubble-message-field-cursor-struct-mg2755.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaBubbleMessageFieldCursorStructMg2755Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2755 Area bubble message field cursor struct MC68000 execution qualification failed.' }

$areaCustomFontMessageFieldCursorStructMg2756Hunk = Join-Path $outDir 'mui-area-custom-font-message-field-cursor-struct-mg2756.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaCustomFontMessageFieldCursorStructMg2756Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2756 Area custom-font message field cursor struct MC68000 execution qualification failed.' }

$areaShortHelpMessageFieldCursorStructMg2757Hunk = Join-Path $outDir 'mui-area-short-help-message-field-cursor-struct-mg2757.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaShortHelpMessageFieldCursorStructMg2757Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2757 Area ShortHelp message field cursor struct MC68000 execution qualification failed.' }

$areaContextMenuMessageFieldCursorStructMg2758Hunk = Join-Path $outDir 'mui-area-context-menu-message-field-cursor-struct-mg2758.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaContextMenuMessageFieldCursorStructMg2758Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2758 Area context-menu message field cursor struct MC68000 execution qualification failed.' }

$areaResizeMessageFieldCursorStructMg2759Hunk = Join-Path $outDir 'mui-area-resize-message-field-cursor-struct-mg2759.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaResizeMessageFieldCursorStructMg2759Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2759 Area resize message field cursor struct MC68000 execution qualification failed.' }

$minMaxFieldCursorStructMg2760Hunk = Join-Path $outDir 'mui-minmax-field-cursor-struct-mg2760.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $minMaxFieldCursorStructMg2760Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2760 MinMax field cursor struct MC68000 execution qualification failed.' }

$areaResizeStateFieldCursorStructMg2761Hunk = Join-Path $outDir 'mui-area-resize-state-field-cursor-struct-mg2761.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaResizeStateFieldCursorStructMg2761Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2761 Area resize state field cursor struct MC68000 execution qualification failed.' }

$areaHandledEventsStateFieldCursorStructMg2762Hunk = Join-Path $outDir 'mui-area-handled-events-state-field-cursor-struct-mg2762.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaHandledEventsStateFieldCursorStructMg2762Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2762 Area handled-events state field cursor struct MC68000 execution qualification failed.' }

$areaDragMessageFieldCursorStructMg2763Hunk = Join-Path $outDir 'mui-area-drag-message-field-cursor-struct-mg2763.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaDragMessageFieldCursorStructMg2763Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2763 Area drag message field cursor struct MC68000 execution qualification failed.' }

$menuItemFieldCursorStructMg2764Hunk = Join-Path $outDir 'mui-menuitem-field-cursor-struct-mg2764.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $menuItemFieldCursorStructMg2764Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2764 MenuItem field cursor struct MC68000 execution qualification failed.' }

$intuiTextFieldCursorStructMg2765Hunk = Join-Path $outDir 'mui-intuitext-field-cursor-struct-mg2765.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $intuiTextFieldCursorStructMg2765Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2765 IntuiText field cursor struct MC68000 execution qualification failed.' }

$miscObjectFactoryHunk = Join-Path $outDir 'mui-misc-object-factory.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $miscObjectFactoryHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Misc specialist object-factory MC68000 execution qualification failed.' }

$miscObjectDispatcherHunk = Join-Path $outDir 'mui-misc-object-dispatcher.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $miscObjectDispatcherHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Misc specialist object-dispatcher MC68000 execution qualification failed.' }

$miscObjectLifecycleHunk = Join-Path $outDir 'mui-misc-object-lifecycle.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $miscObjectLifecycleHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Misc object-aware Setup/Cleanup lifecycle MC68000 execution qualification failed.' }

$miscSpecialistMessageCodecHunk = Join-Path $outDir 'mui-misc-specialist-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $miscSpecialistMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Misc specialist message codec MC68000 execution qualification failed.' }

$keyadjustRawKeyBoundaryHunk = Join-Path $outDir 'mui-keyadjust-raw-key-boundary.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $keyadjustRawKeyBoundaryHunk
if ($LASTEXITCODE -ne 0) { throw 'MG546 native Keyadjust raw-key boundary MC68000 execution qualification failed.' }

$shortHelpCheckBoundaryHunk = Join-Path $outDir 'mui-short-help-check-boundary.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $shortHelpCheckBoundaryHunk
if ($LASTEXITCODE -ne 0) { throw 'MG548 native ShortHelp check boundary MC68000 execution qualification failed.' }

$areaHandledEventsCodecHunk = Join-Path $outDir 'mui-area-handled-events-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaHandledEventsCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG550 native Area handled-events codec MC68000 execution qualification failed.' }

$windowMenustripCodecHunk = Join-Path $outDir 'mui-window-menustrip-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowMenustripCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG551 native Window Menustrip codec MC68000 execution qualification failed.' }

$windowVisualStateCodecHunk = Join-Path $outDir 'mui-window-visual-state-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowVisualStateCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG552 native Window visual-state codec MC68000 execution qualification failed.' }

$windowEventHandlerCodecHunk = Join-Path $outDir 'mui-window-event-handler-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowEventHandlerCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG553 native Window event-handler codec MC68000 execution qualification failed.' }

$windowEventHandlerFieldStructMg2297Hunk = Join-Path $outDir 'mui-window-event-handler-field-struct-mg2297.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowEventHandlerFieldStructMg2297Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2297 Window event-handler field-struct MC68000 execution qualification failed.' }

$windowCycleChainCodecHunk = Join-Path $outDir 'mui-window-cycle-chain-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowCycleChainCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG554 native Window cycle-chain codec MC68000 execution qualification failed.' }

$windowCycleChainFieldStructMg2296Hunk = Join-Path $outDir 'mui-window-cycle-chain-field-struct-mg2296.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowCycleChainFieldStructMg2296Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2296 Window cycle-chain field-struct MC68000 execution qualification failed.' }

$windowMenuCodecHunk = Join-Path $outDir 'mui-window-menu-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowMenuCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG555 native Application/Window menu codec MC68000 execution qualification failed.' }

$applicationMenuFieldStructMg2298Hunk = Join-Path $outDir 'mui-application-menu-field-struct-mg2298.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationMenuFieldStructMg2298Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2298 Application/Window menu field-struct MC68000 execution qualification failed.' }

$applicationMethodFieldStructMg2299Hunk = Join-Path $outDir 'mui-application-method-field-struct-mg2299.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationMethodFieldStructMg2299Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2299 Application/Window method field-struct MC68000 execution qualification failed.' }

$collectionBasicFieldStructMg2300Hunk = Join-Path $outDir 'mui-collection-basic-field-struct-mg2300.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionBasicFieldStructMg2300Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2300 Collection basic field-struct MC68000 execution qualification failed.' }

$collectionRecordFieldStructMg2301Hunk = Join-Path $outDir 'mui-collection-record-field-struct-mg2301.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionRecordFieldStructMg2301Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2301 Collection record field-struct MC68000 execution qualification failed.' }

$collectionEditFieldStructMg2302Hunk = Join-Path $outDir 'mui-collection-edit-field-struct-mg2302.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionEditFieldStructMg2302Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2302 Collection edit field-struct MC68000 execution qualification failed.' }

$collectionSurfaceFieldStructMg2303Hunk = Join-Path $outDir 'mui-collection-surface-field-struct-mg2303.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionSurfaceFieldStructMg2303Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2303 Collection surface field-struct MC68000 execution qualification failed.' }

$collectionAdvancedFieldStructMg2304Hunk = Join-Path $outDir 'mui-collection-advanced-field-struct-mg2304.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionAdvancedFieldStructMg2304Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2304 Collection advanced field-struct MC68000 execution qualification failed.' }

$listtreeFieldStructMg2305Hunk = Join-Path $outDir 'mui-listtree-field-struct-mg2305.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeFieldStructMg2305Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2305 Listtree field-struct MC68000 execution qualification failed.' }

$listtreePresentationFieldStructMg2306Hunk = Join-Path $outDir 'mui-listtree-presentation-field-struct-mg2306.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreePresentationFieldStructMg2306Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2306 Listtree presentation field-struct MC68000 execution qualification failed.' }

$listviewHorizontalScrollerFieldStructMg2307Hunk = Join-Path $outDir 'mui-listview-horizontal-scroller-field-struct-mg2307.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewHorizontalScrollerFieldStructMg2307Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2307 Listview horizontal scroller field-struct MC68000 execution qualification failed.' }

$listviewHorizontalScrollerDragFieldStructMg2308Hunk = Join-Path $outDir 'mui-listview-horizontal-scroller-drag-field-struct-mg2308.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewHorizontalScrollerDragFieldStructMg2308Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2308 Listview horizontal scroller drag field-struct MC68000 execution qualification failed.' }

$listviewScrollerDragFieldStructMg2309Hunk = Join-Path $outDir 'mui-listview-scroller-drag-field-struct-mg2309.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewScrollerDragFieldStructMg2309Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2309 Listview vertical scroller drag field-struct MC68000 execution qualification failed.' }

$listviewChildFieldStructMg2310Hunk = Join-Path $outDir 'mui-listview-child-field-struct-mg2310.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewChildFieldStructMg2310Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2310 Listview child field-struct MC68000 execution qualification failed.' }

$listviewClickFieldStructMg2311Hunk = Join-Path $outDir 'mui-listview-click-field-struct-mg2311.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewClickFieldStructMg2311Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2311 Listview click field-struct MC68000 execution qualification failed.' }

$listviewInteractionPolicyFieldStructMg2312Hunk = Join-Path $outDir 'mui-listview-interaction-policy-field-struct-mg2312.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewInteractionPolicyFieldStructMg2312Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2312 Listview interaction policy field-struct MC68000 execution qualification failed.' }

$listviewSelectionSignalFieldStructMg2313Hunk = Join-Path $outDir 'mui-listview-selection-signal-field-struct-mg2313.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewSelectionSignalFieldStructMg2313Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2313 Listview selection signal field-struct MC68000 execution qualification failed.' }

$listviewLayoutFieldStructMg2314Hunk = Join-Path $outDir 'mui-listview-layout-field-struct-mg2314.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewLayoutFieldStructMg2314Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2314 Listview layout field-struct MC68000 execution qualification failed.' }

$listviewRenderFieldStructMg2315Hunk = Join-Path $outDir 'mui-listview-render-field-struct-mg2315.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewRenderFieldStructMg2315Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2315 Listview render field-struct MC68000 execution qualification failed.' }

$listviewExternalScrollerConnectionFieldStructMg2316Hunk = Join-Path $outDir 'mui-listview-external-scroller-connection-field-struct-mg2316.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewExternalScrollerConnectionFieldStructMg2316Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2316 Listview external scroller connection field-struct MC68000 execution qualification failed.' }

$listviewScrollerFieldStructMg2317Hunk = Join-Path $outDir 'mui-listview-scroller-field-struct-mg2317.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewScrollerFieldStructMg2317Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2317 Listview scroller field-struct MC68000 execution qualification failed.' }

$listtreeHeaderFieldStructMg2318Hunk = Join-Path $outDir 'mui-listtree-header-field-struct-mg2318.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeHeaderFieldStructMg2318Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2318 Listtree header field-struct MC68000 execution qualification failed.' }

$listtreeDisplayColumnFieldStructMg2319Hunk = Join-Path $outDir 'mui-listtree-display-column-field-struct-mg2319.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeDisplayColumnFieldStructMg2319Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2319 Listtree display-column field-struct MC68000 execution qualification failed.' }

$listtreeColumnGeometryFieldStructMg2320Hunk = Join-Path $outDir 'mui-listtree-column-geometry-field-struct-mg2320.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeColumnGeometryFieldStructMg2320Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2320 Listtree column-geometry field-struct MC68000 execution qualification failed.' }

$listtreeDisplaySnapshotFieldStructMg2321Hunk = Join-Path $outDir 'mui-listtree-display-snapshot-field-struct-mg2321.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeDisplaySnapshotFieldStructMg2321Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2321 Listtree display-snapshot field-struct MC68000 execution qualification failed.' }

$listtreePolicyFieldStructMg2322Hunk = Join-Path $outDir 'mui-listtree-policy-field-struct-mg2322.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreePolicyFieldStructMg2322Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2322 Listtree policy field-struct MC68000 execution qualification failed.' }

$listtreeHookPoolFieldStructMg2323Hunk = Join-Path $outDir 'mui-listtree-hook-pool-field-struct-mg2323.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeHookPoolFieldStructMg2323Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2323 Listtree hook-pool field-struct MC68000 execution qualification failed.' }

$listtreeClickStateFieldStructMg2324Hunk = Join-Path $outDir 'mui-listtree-click-state-field-struct-mg2324.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeClickStateFieldStructMg2324Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2324 Listtree click-state field-struct MC68000 execution qualification failed.' }

$listtreeClickColumnFieldStructMg2325Hunk = Join-Path $outDir 'mui-listtree-click-column-field-struct-mg2325.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeClickColumnFieldStructMg2325Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2325 Listtree click-column field-struct MC68000 execution qualification failed.' }

$listtreeSurfaceFieldStructMg2326Hunk = Join-Path $outDir 'mui-listtree-surface-field-struct-mg2326.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeSurfaceFieldStructMg2326Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2326 Listtree surface field-struct MC68000 execution qualification failed.' }

$listtreeLifecycleFieldStructMg2327Hunk = Join-Path $outDir 'mui-listtree-lifecycle-field-struct-mg2327.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeLifecycleFieldStructMg2327Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2327 Listtree lifecycle field-struct MC68000 execution qualification failed.' }

$listtreeNodeFieldStructMg2328Hunk = Join-Path $outDir 'mui-listtree-node-field-struct-mg2328.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeNodeFieldStructMg2328Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2328 Listtree node field-struct MC68000 execution qualification failed.' }

$listtreeTestPosFieldStructMg2329Hunk = Join-Path $outDir 'mui-listtree-test-pos-field-struct-mg2329.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeTestPosFieldStructMg2329Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2329 Listtree TestPos field-struct MC68000 execution qualification failed.' }

$familyGetChildFieldStructMg2330Hunk = Join-Path $outDir 'mui-family-get-child-field-struct-mg2330.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $familyGetChildFieldStructMg2330Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2330 Family_GetChild field-struct MC68000 execution qualification failed.' }

$familyMutationFieldStructMg2331Hunk = Join-Path $outDir 'mui-family-mutation-field-struct-mg2331.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $familyMutationFieldStructMg2331Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2331 Family mutation field-struct MC68000 execution qualification failed.' }

$familyMutationListFieldStructMg2332Hunk = Join-Path $outDir 'mui-family-mutation-list-field-struct-mg2332.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $familyMutationListFieldStructMg2332Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2332 Family mutation list field-struct MC68000 execution qualification failed.' }

$familyDoChildFieldStructMg2333Hunk = Join-Path $outDir 'mui-family-do-child-field-struct-mg2333.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $familyDoChildFieldStructMg2333Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2333 Family_DoChildMethods field-struct MC68000 execution qualification failed.' }

$familyDoChildFieldStructMg2804Hunk = Join-Path $outDir 'mui-family-do-child-field-struct-mg2804.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $familyDoChildFieldStructMg2804Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2804 Family_DoChildMethods field-struct MC68000 execution qualification failed.' }

$dataspaceFieldStructMg2334Hunk = Join-Path $outDir 'mui-dataspace-field-struct-mg2334.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $dataspaceFieldStructMg2334Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2334 Dataspace field-struct MC68000 execution qualification failed.' }

$dataspaceIffFieldStructMg2335Hunk = Join-Path $outDir 'mui-dataspace-iff-field-struct-mg2335.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $dataspaceIffFieldStructMg2335Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2335 Dataspace-IFF field-struct MC68000 execution qualification failed.' }

$setAsStringFieldStructMg2336Hunk = Join-Path $outDir 'mui-set-as-string-field-struct-mg2336.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $setAsStringFieldStructMg2336Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2336 SetAsString field-struct MC68000 execution qualification failed.' }

$notifyWriteFieldStructMg2337Hunk = Join-Path $outDir 'mui-notify-write-field-struct-mg2337.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $notifyWriteFieldStructMg2337Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2337 Notify Write field-struct MC68000 execution qualification failed.' }

$notifyUserDataFieldStructMg2338Hunk = Join-Path $outDir 'mui-notify-userdata-field-struct-mg2338.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $notifyUserDataFieldStructMg2338Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2338 Notify UserData field-struct MC68000 execution qualification failed.' }

$notifyUserDataFrameFieldStructMg2339Hunk = Join-Path $outDir 'mui-notify-userdata-frame-field-struct-mg2339.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $notifyUserDataFrameFieldStructMg2339Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2339 Notify UserData traversal-frame field-struct MC68000 execution qualification failed.' }

$notifyFieldStructMg2340Hunk = Join-Path $outDir 'mui-notify-field-struct-mg2340.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $notifyFieldStructMg2340Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2340 Notify field-struct MC68000 execution qualification failed.' }

$objectPersistenceFieldStructMg2341Hunk = Join-Path $outDir 'mui-object-persistence-field-struct-mg2341.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $objectPersistenceFieldStructMg2341Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2341 ObjectPersistence field-struct MC68000 execution qualification failed.' }

$storeFieldStructMg2342Hunk = Join-Path $outDir 'mui-store-field-struct-mg2342.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $storeFieldStructMg2342Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2342 Store field-struct MC68000 execution qualification failed.' }

$commonControlFieldStructMg2343Hunk = Join-Path $outDir 'mui-common-control-field-struct-record.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $commonControlFieldStructMg2343Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2343 common-control field-struct MC68000 execution qualification failed.' }

$choiceEntryFieldStructMg2802Hunk = Join-Path $outDir 'mui-choice-entry-field-struct-mg2802.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $choiceEntryFieldStructMg2802Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2802 ChoiceEntry field-struct MC68000 execution qualification failed.' }

$groupChangeFieldStructMg2344Hunk = Join-Path $outDir 'mui-group-change-field-struct-record.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $groupChangeFieldStructMg2344Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2344 GroupChange field-struct MC68000 execution qualification failed.' }

$colorFieldStructMg2345Hunk = Join-Path $outDir 'mui-color-field-struct-record.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $colorFieldStructMg2345Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2345 Color field-struct MC68000 execution qualification failed.' }

$dirlistByteTotalFieldStructMg2346Hunk = Join-Path $outDir 'mui-dirlist-byte-total-field-struct-record.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $dirlistByteTotalFieldStructMg2346Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2346 Dirlist ByteTotal field-struct MC68000 execution qualification failed.' }

$headlessClassFieldStructAdapterMg2347Hunk = Join-Path $outDir 'mui-headless-class-field-struct-adapter-mg2347.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $headlessClassFieldStructAdapterMg2347Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2347 headless class field-struct adapter MC68000 execution qualification failed.' }

$headlessStateFieldStructAdapterMg2348Hunk = Join-Path $outDir 'mui-headless-state-field-struct-adapter-mg2348.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $headlessStateFieldStructAdapterMg2348Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2348 headless state field-struct adapter MC68000 execution qualification failed.' }

$layoutFieldStructAdapterMg2350Hunk = Join-Path $outDir 'mui-layout-field-struct-adapter-mg2350.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $layoutFieldStructAdapterMg2350Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2350 Layout field-struct adapter MC68000 execution qualification failed.' }

$commonControlFieldStructAdapterMg2351Hunk = Join-Path $outDir 'mui-common-control-field-struct-adapter-mg2351.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $commonControlFieldStructAdapterMg2351Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2351 CommonControl field-struct adapter MC68000 execution qualification failed.' }

$minMaxFieldStructAdapterMg2352Hunk = Join-Path $outDir 'mui-minmax-field-struct-adapter-mg2352.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $minMaxFieldStructAdapterMg2352Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2352 MinMax field-struct adapter MC68000 execution qualification failed.' }

$listTestPosFieldStructAdapterMg2353Hunk = Join-Path $outDir 'mui-list-testpos-field-struct-adapter-mg2353.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listTestPosFieldStructAdapterMg2353Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2353 List.TestPos field-struct adapter MC68000 execution qualification failed.' }

$listInputFieldStructAdapterMg2354Hunk = Join-Path $outDir 'mui-list-input-field-struct-adapter-mg2354.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listInputFieldStructAdapterMg2354Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2354 List input field-struct adapter MC68000 execution qualification failed.' }

$listviewDragFieldStructAdapterMg2355Hunk = Join-Path $outDir 'mui-listview-drag-field-struct-adapter-mg2355.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewDragFieldStructAdapterMg2355Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2355 Listview drag field-struct adapter MC68000 execution qualification failed.' }

$listHeaderFieldStructAdapterMg2356Hunk = Join-Path $outDir 'mui-list-header-field-struct-adapter-mg2356.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listHeaderFieldStructAdapterMg2356Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2356 List header field-struct adapter MC68000 execution qualification failed.' }

$listviewOwnerFieldStructAdapterMg2357Hunk = Join-Path $outDir 'mui-listview-owner-field-struct-adapter-mg2357.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewOwnerFieldStructAdapterMg2357Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2357 Listview owner field-struct adapter MC68000 execution qualification failed.' }

$listviewHScrollerFieldStructAdapterMg2358Hunk = Join-Path $outDir 'mui-listview-hscroller-field-struct-adapter-mg2358.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewHScrollerFieldStructAdapterMg2358Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2358 Listview horizontal-scroller field-struct adapter MC68000 execution qualification failed.' }

$listSlotFieldStructAdapterMg2359Hunk = Join-Path $outDir 'mui-list-slot-field-struct-adapter-mg2359.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listSlotFieldStructAdapterMg2359Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2359 List slot field-struct adapter MC68000 execution qualification failed.' }

$listImageFieldStructAdapterMg2360Hunk = Join-Path $outDir 'mui-list-image-field-struct-adapter-mg2360.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listImageFieldStructAdapterMg2360Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2360 List image field-struct adapter MC68000 execution qualification failed.' }

$listTitleArrayFieldStructAdapterMg2361Hunk = Join-Path $outDir 'mui-list-title-array-field-struct-adapter-mg2361.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listTitleArrayFieldStructAdapterMg2361Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2361 List title-array field-struct adapter MC68000 execution qualification failed.' }

$listTitleFieldStructAdapterMg2362Hunk = Join-Path $outDir 'mui-list-title-field-struct-adapter-mg2362.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listTitleFieldStructAdapterMg2362Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2362 List title field-struct adapter MC68000 execution qualification failed.' }

$listSelectionSignalFieldStructAdapterMg2363Hunk = Join-Path $outDir 'mui-list-selection-signal-field-struct-adapter-mg2363.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listSelectionSignalFieldStructAdapterMg2363Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2363 List selection-signal field-struct adapter MC68000 execution qualification failed.' }

$listFormatPolicyFieldStructAdapterMg2364Hunk = Join-Path $outDir 'mui-list-format-policy-field-struct-adapter-mg2364.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listFormatPolicyFieldStructAdapterMg2364Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2364 List format-policy field-struct adapter MC68000 execution qualification failed.' }

$listFontFieldStructAdapterMg2365Hunk = Join-Path $outDir 'mui-list-font-field-struct-adapter-mg2365.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listFontFieldStructAdapterMg2365Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2365 List font field-struct adapter MC68000 execution qualification failed.' }

$listRedrawFieldStructAdapterMg2366Hunk = Join-Path $outDir 'mui-list-redraw-field-struct-adapter-mg2366.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listRedrawFieldStructAdapterMg2366Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2366 List redraw field-struct adapter MC68000 execution qualification failed.' }

$listActiveFieldStructAdapterMg2367Hunk = Join-Path $outDir 'mui-list-active-field-struct-adapter-mg2367.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listActiveFieldStructAdapterMg2367Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2367 List active field-struct adapter MC68000 execution qualification failed.' }

$listInsertPositionFieldStructAdapterMg2368Hunk = Join-Path $outDir 'mui-list-insert-position-field-struct-adapter-mg2368.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listInsertPositionFieldStructAdapterMg2368Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2368 List insertion-position field-struct adapter MC68000 execution qualification failed.' }

$listViewportFieldStructAdapterMg2369Hunk = Join-Path $outDir 'mui-list-viewport-field-struct-adapter-mg2369.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listViewportFieldStructAdapterMg2369Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2369 List viewport field-struct adapter MC68000 execution qualification failed.' }

$listInteractionPolicyFieldStructAdapterMg2370Hunk = Join-Path $outDir 'mui-list-interaction-policy-field-struct-adapter-mg2370.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listInteractionPolicyFieldStructAdapterMg2370Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2370 List interaction-policy field-struct adapter MC68000 execution qualification failed.' }

$listClickFieldStructAdapterMg2371Hunk = Join-Path $outDir 'mui-list-click-field-struct-adapter-mg2371.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listClickFieldStructAdapterMg2371Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2371 List click field-struct adapter MC68000 execution qualification failed.' }

$listHookPolicyFieldStructAdapterMg2372Hunk = Join-Path $outDir 'mui-list-hook-policy-field-struct-adapter-mg2372.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listHookPolicyFieldStructAdapterMg2372Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2372 List hook-policy field-struct adapter MC68000 execution qualification failed.' }

$listSortFieldStructAdapterMg2373Hunk = Join-Path $outDir 'mui-list-sort-field-struct-adapter-mg2373.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listSortFieldStructAdapterMg2373Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2373 List sort field-struct adapter MC68000 execution qualification failed.' }

$listPoolPolicyFieldStructAdapterMg2374Hunk = Join-Path $outDir 'mui-list-pool-policy-field-struct-adapter-mg2374.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listPoolPolicyFieldStructAdapterMg2374Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2374 List pool-policy field-struct adapter MC68000 execution qualification failed.' }

$listFormatDescriptorFieldStructAdapterMg2375Hunk = Join-Path $outDir 'mui-list-format-descriptor-field-struct-adapter-mg2375.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listFormatDescriptorFieldStructAdapterMg2375Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2375 List FORMAT descriptor field-struct adapter MC68000 execution qualification failed.' }

$listFormatDescriptorStateFieldStructAdapterMg2376Hunk = Join-Path $outDir 'mui-list-format-descriptor-state-field-struct-adapter-mg2376.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listFormatDescriptorStateFieldStructAdapterMg2376Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2376 List FORMAT descriptor-state field-struct adapter MC68000 execution qualification failed.' }

$listColumnMetricsFieldStructAdapterMg2377Hunk = Join-Path $outDir 'mui-list-column-metrics-field-struct-adapter-mg2377.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listColumnMetricsFieldStructAdapterMg2377Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2377 List column-metrics field-struct adapter MC68000 execution qualification failed.' }

$listColumnMetricValueFieldStructAdapterMg2378Hunk = Join-Path $outDir 'mui-list-column-metric-value-field-struct-adapter-mg2378.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listColumnMetricValueFieldStructAdapterMg2378Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2378 List column-metric value field-struct adapter MC68000 execution qualification failed.' }

$listEditFieldStructAdapterMg2379Hunk = Join-Path $outDir 'mui-list-edit-field-struct-adapter-mg2379.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listEditFieldStructAdapterMg2379Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2379 List edit field-struct adapter MC68000 execution qualification failed.' }

$listColumnGeometryFieldStructAdapterMg2380Hunk = Join-Path $outDir 'mui-list-column-geometry-field-struct-adapter-mg2380.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listColumnGeometryFieldStructAdapterMg2380Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2380 List column-geometry field-struct adapter MC68000 execution qualification failed.' }

$listColumnLayoutFieldStructAdapterMg2381Hunk = Join-Path $outDir 'mui-list-column-layout-field-struct-adapter-mg2381.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listColumnLayoutFieldStructAdapterMg2381Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2381 List column-layout field-struct adapter MC68000 execution qualification failed.' }

$listColumnVisibilityFieldStructAdapterMg2382Hunk = Join-Path $outDir 'mui-list-column-visibility-field-struct-adapter-mg2382.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listColumnVisibilityFieldStructAdapterMg2382Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2382 List column-visibility field-struct adapter MC68000 execution qualification failed.' }

$listColumnOrderFieldStructAdapterMg2383Hunk = Join-Path $outDir 'mui-list-column-order-field-struct-adapter-mg2383.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listColumnOrderFieldStructAdapterMg2383Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2383 List column-order field-struct adapter MC68000 execution qualification failed.' }

$listPresentationPolicyFieldStructAdapterMg2384Hunk = Join-Path $outDir 'mui-list-presentation-policy-field-struct-adapter-mg2384.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listPresentationPolicyFieldStructAdapterMg2384Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2384 List presentation-policy field-struct adapter MC68000 execution qualification failed.' }

$listStateMultiplexedFieldStructAdapterMg2385Hunk = Join-Path $outDir 'mui-list-state-multiplexed-field-struct-adapter-mg2385.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listStateMultiplexedFieldStructAdapterMg2385Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2385 multiplexed List-state field-struct adapter MC68000 execution qualification failed.' }

$listPointerSlotFieldStructAdapterMg2386Hunk = Join-Path $outDir 'mui-list-pointer-slot-field-struct-adapter-mg2386.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listPointerSlotFieldStructAdapterMg2386Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2386 List pointer-slot field-struct adapter MC68000 execution qualification failed.' }

$listOwnedRecordHeaderFieldStructAdapterMg2387Hunk = Join-Path $outDir 'mui-list-owned-record-header-field-struct-adapter-mg2387.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listOwnedRecordHeaderFieldStructAdapterMg2387Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2387 List owned-record-header field-struct adapter MC68000 execution qualification failed.' }

$listScalarDisplayRowFieldStructAdapterMg2388Hunk = Join-Path $outDir 'mui-list-scalar-display-row-field-struct-adapter-mg2388.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listScalarDisplayRowFieldStructAdapterMg2388Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2388 List scalar-storage/display-row field-struct adapter MC68000 execution qualification failed.' }

$listColumnOrderByteFieldStructAdapterMg2389Hunk = Join-Path $outDir 'mui-list-column-order-byte-field-struct-adapter-mg2389.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listColumnOrderByteFieldStructAdapterMg2389Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2389 List ColumnOrder byte-field struct adapter MC68000 execution qualification failed.' }

$areaCustomFontRuntimeOffsetBridgeStructMg2390Hunk = Join-Path $outDir 'mui-area-custom-font-runtime-offset-bridge-struct-mg2390.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaCustomFontRuntimeOffsetBridgeStructMg2390Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2390 Area CustomFont runtime offset-bridge struct MC68000 execution qualification failed.' }

$areaGeometryOffsetBridgeStructMg2391Hunk = Join-Path $outDir 'mui-area-geometry-offset-bridge-struct-mg2391.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaGeometryOffsetBridgeStructMg2391Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2391 Area geometry offset-bridge struct MC68000 execution qualification failed.' }

$miscPanelDispatcherHunk = Join-Path $outDir 'mui-misc-panel-dispatcher.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $miscPanelDispatcherHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Misc Panel_Run object-dispatcher MC68000 execution qualification failed.' }

$miscFilepanelDispatcherHunk = Join-Path $outDir 'mui-misc-filepanel-dispatcher.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $miscFilepanelDispatcherHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Misc Filepanel_AddRow object-dispatcher MC68000 execution qualification failed.' }

$miscMccprefsDispatcherHunk = Join-Path $outDir 'mui-misc-mccprefs-dispatcher.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $miscMccprefsDispatcherHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Misc Mccprefs_RegisterGadget object-dispatcher MC68000 execution qualification failed.' }

$miscMccprefsConfigDispatcherHunk = Join-Path $outDir 'mui-misc-mccprefs-config-dispatcher.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $miscMccprefsConfigDispatcherHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Misc Mccprefs_ConfigToGadgets object-dispatcher MC68000 execution qualification failed.' }

$miscMccprefsGadgetsConfigDispatcherHunk = Join-Path $outDir 'mui-misc-mccprefs-gadgets-config-dispatcher.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $miscMccprefsGadgetsConfigDispatcherHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Misc Mccprefs_GadgetsToConfig object-dispatcher MC68000 execution qualification failed.' }

$applicationHunk = Join-Path $outDir 'mui-application.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationHunk
if ($LASTEXITCODE -ne 0) { throw 'MG06 MC68000 execution qualification failed.' }

$applicationSleepHunk = Join-Path $outDir 'mui-application-sleep.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationSleepHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application Sleep MC68000 execution qualification failed.' }

$applicationIconifiedHunk = Join-Path $outDir 'mui-application-iconified.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationIconifiedHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application Iconified MC68000 execution qualification failed.' }

$applicationActiveHunk = Join-Path $outDir 'mui-application-active.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationActiveHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application Active MC68000 execution qualification failed.' }

$applicationSingleTaskHunk = Join-Path $outDir 'mui-application-single-task.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationSingleTaskHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application SingleTask MC68000 execution qualification failed.' }

$applicationForceQuitHunk = Join-Path $outDir 'mui-application-force-quit.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationForceQuitHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application ForceQuit MC68000 execution qualification failed.' }

$applicationUseRexxHunk = Join-Path $outDir 'mui-application-use-rexx.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationUseRexxHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application UseRexx MC68000 execution qualification failed.' }

$applicationUseCommoditiesHunk = Join-Path $outDir 'mui-application-use-commodities.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationUseCommoditiesHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application UseCommodities MC68000 execution qualification failed.' }

$applicationWindowListHunk = Join-Path $outDir 'mui-application-window-list.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationWindowListHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application WindowList MC68000 execution qualification failed.' }

$applicationCommandsHunk = Join-Path $outDir 'mui-application-commands.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationCommandsHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application Commands MC68000 execution qualification failed.' }

$applicationAppMessageHunk = Join-Path $outDir 'mui-application-app-message.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationAppMessageHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application AppMessage MC68000 execution qualification failed.' }

$windowWindowHunk = Join-Path $outDir 'mui-window-window.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowWindowHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Window Window getter MC68000 execution qualification failed.' }

$windowIdHunk = Join-Path $outDir 'mui-window-id.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowIdHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Window ID MC68000 execution qualification failed.' }

$windowCloseRequestHunk = Join-Path $outDir 'mui-window-close-request.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowCloseRequestHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Window CloseRequest MC68000 execution qualification failed.' }

$windowRootObjectHunk = Join-Path $outDir 'mui-window-root-object.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowRootObjectHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Window RootObject MC68000 execution qualification failed.' }

$windowNoMenusHunk = Join-Path $outDir 'mui-window-no-menus.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowNoMenusHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Window NoMenus MC68000 execution qualification failed.' }

$windowHasAlphaHunk = Join-Path $outDir 'mui-window-has-alpha.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowHasAlphaHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Window HasAlpha MC68000 execution qualification failed.' }

$windowTitleHunk = Join-Path $outDir 'mui-window-title.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowTitleHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Window Title MC68000 execution qualification failed.' }

$windowScreenTitleHunk = Join-Path $outDir 'mui-window-screen-title.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowScreenTitleHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Window ScreenTitle MC68000 execution qualification failed.' }

$windowPublicScreenHunk = Join-Path $outDir 'mui-window-public-screen.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowPublicScreenHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Window PublicScreen MC68000 execution qualification failed.' }

$windowScreenHunk = Join-Path $outDir 'mui-window-screen.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowScreenHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Window Screen MC68000 execution qualification failed.' }

$windowRefWindowHunk = Join-Path $outDir 'mui-window-ref-window.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowRefWindowHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Window RefWindow MC68000 execution qualification failed.' }

$windowVisibleOnMaximizeHunk = Join-Path $outDir 'mui-window-visible-on-maximize.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowVisibleOnMaximizeHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Window VisibleOnMaximize MC68000 execution qualification failed.' }

$windowIsSubWindowHunk = Join-Path $outDir 'mui-window-is-sub-window.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowIsSubWindowHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Window IsSubWindow MC68000 execution qualification failed.' }

$windowTabletMessagesHunk = Join-Path $outDir 'mui-window-tablet-messages.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowTabletMessagesHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Window TabletMessages MC68000 execution qualification failed.' }

$windowBorderScrollersHunk = Join-Path $outDir 'mui-window-border-scrollers.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowBorderScrollersHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Window border-scrollers MC68000 execution qualification failed.' }

$windowAlternateGeometryHunk = Join-Path $outDir 'mui-window-alternate-geometry.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowAlternateGeometryHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Window alternate-geometry MC68000 execution qualification failed.' }

$windowGeometryHunk = Join-Path $outDir 'mui-window-geometry.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowGeometryHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Window geometry MC68000 execution qualification failed.' }

$windowGadgetPolicyHunk = Join-Path $outDir 'mui-window-gadget-policy.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowGadgetPolicyHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Window gadget-policy MC68000 execution qualification failed.' }

$windowModePolicyHunk = Join-Path $outDir 'mui-window-mode-policy.hunk'
$windowModePolicyMap = Get-Content ($windowModePolicyHunk + '.map') -Raw
if ($windowModePolicyMap -match 'relocations=0') {
    & dotnet run --project $executionProject --configuration $Configuration -- $windowModePolicyHunk
    if ($LASTEXITCODE -ne 0) { throw 'MG09 Window mode-policy MC68000 execution qualification failed.' }
} else {
    Write-Host 'MG09 Window mode-policy native execution deferred: closure contains internal relocations.'
}

$applicationIdentityStringsHunk = Join-Path $outDir 'mui-application-identity-strings.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationIdentityStringsHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application identity strings MC68000 execution qualification failed.' }

$applicationWindowInitializerHunk = Join-Path $outDir 'mui-application-window-initializer.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationWindowInitializerHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application Window initializer MC68000 execution qualification failed.' }

$applicationUsedClassesHunk = Join-Path $outDir 'mui-application-used-classes.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationUsedClassesHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application UsedClasses MC68000 execution qualification failed.' }

$applicationUsedClassesVectorEntryStructMg2146Hunk = Join-Path $outDir 'mui-application-used-classes-vector-entry-struct-mg2146.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationUsedClassesVectorEntryStructMg2146Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2146 Application UsedClasses vector-entry struct MC68000 execution qualification failed.' }

$stringInteger64FieldStructMg2147Hunk = Join-Path $outDir 'mui-string-integer64-field-struct-mg2147.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringInteger64FieldStructMg2147Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2147 String Integer64 field-struct MC68000 execution qualification failed.' }

$storeIterationCounterFieldStructMg2148Hunk = Join-Path $outDir 'mui-store-iteration-counter-field-struct-mg2148.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $storeIterationCounterFieldStructMg2148Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2148 Store iteration-counter field-struct MC68000 execution qualification failed.' }

$storeIterationCounterFieldStructMg2801Hunk = Join-Path $outDir 'mui-store-iteration-counter-field-struct-mg2801.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $storeIterationCounterFieldStructMg2801Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2801 Store iteration-counter field-struct MC68000 execution qualification failed.' }

$guestUlongMemoryStructMg2149Hunk = Join-Path $outDir 'mui-guest-ulong-memory-struct-mg2149.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $guestUlongMemoryStructMg2149Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2149 Guest ULONG memory-struct MC68000 execution qualification failed.' }

$applicationCommandsStateFieldStructMg2150Hunk = Join-Path $outDir 'mui-application-commands-state-field-struct-mg2150.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationCommandsStateFieldStructMg2150Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2150 Application commands-state field-struct MC68000 execution qualification failed.' }

$applicationRefreshStateFieldStructMg2151Hunk = Join-Path $outDir 'mui-application-refresh-state-field-struct-mg2151.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationRefreshStateFieldStructMg2151Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2151 Application refresh-state field-struct MC68000 execution qualification failed.' }

$applicationMenuStateFieldStructMg2152Hunk = Join-Path $outDir 'mui-application-menu-state-field-struct-mg2152.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationMenuStateFieldStructMg2152Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2152 Application menu-state field-struct MC68000 execution qualification failed.' }

$applicationMessageRoutingStateFieldStructMg2153Hunk = Join-Path $outDir 'mui-application-message-routing-state-field-struct-mg2153.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationMessageRoutingStateFieldStructMg2153Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2153 Application message-routing-state field-struct MC68000 execution qualification failed.' }

$applicationObjectStateFieldStructMg2154Hunk = Join-Path $outDir 'mui-application-object-state-field-struct-mg2154.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationObjectStateFieldStructMg2154Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2154 Application object-state field-struct MC68000 execution qualification failed.' }

$applicationTextStateFieldStructMg2155Hunk = Join-Path $outDir 'mui-application-text-state-field-struct-mg2155.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationTextStateFieldStructMg2155Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2155 Application text-state field-struct MC68000 execution qualification failed.' }

$applicationHelpStateFieldStructMg2156Hunk = Join-Path $outDir 'mui-application-help-state-field-struct-mg2156.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationHelpStateFieldStructMg2156Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2156 Application help-state field-struct MC68000 execution qualification failed.' }

$applicationConfigWindowStateFieldStructMg2157Hunk = Join-Path $outDir 'mui-application-config-window-state-field-struct-mg2157.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationConfigWindowStateFieldStructMg2157Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2157 Application config-window-state field-struct MC68000 execution qualification failed.' }

$applicationDefaultConfigStateFieldStructMg2158Hunk = Join-Path $outDir 'mui-application-default-config-state-field-struct-mg2158.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationDefaultConfigStateFieldStructMg2158Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2158 Application default-config-state field-struct MC68000 execution qualification failed.' }

$applicationLifecycleStateFieldStructMg2159Hunk = Join-Path $outDir 'mui-application-lifecycle-state-field-struct-mg2159.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationLifecycleStateFieldStructMg2159Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2159 Application lifecycle-state field-struct MC68000 execution qualification failed.' }

$applicationSchedulerStateFieldStructMg2160Hunk = Join-Path $outDir 'mui-application-scheduler-state-field-struct-mg2160.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationSchedulerStateFieldStructMg2160Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2160 Application scheduler-state field-struct MC68000 execution qualification failed.' }

$applicationIdentityStateFieldStructMg2161Hunk = Join-Path $outDir 'mui-application-identity-state-field-struct-mg2161.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationIdentityStateFieldStructMg2161Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2161 Application identity-state field-struct MC68000 execution qualification failed.' }

$applicationSetConfigItemStateFieldStructMg2162Hunk = Join-Path $outDir 'mui-application-set-config-item-state-field-struct-mg2162.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationSetConfigItemStateFieldStructMg2162Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2162 Application SetConfigItem-state field-struct MC68000 execution qualification failed.' }

$applicationPolicyStateFieldStructMg2163Hunk = Join-Path $outDir 'mui-application-policy-state-field-struct-mg2163.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationPolicyStateFieldStructMg2163Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2163 Application policy-state field-struct MC68000 execution qualification failed.' }

$applicationSettingsPanelStateFieldStructMg2164Hunk = Join-Path $outDir 'mui-application-settings-panel-state-field-struct-mg2164.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationSettingsPanelStateFieldStructMg2164Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2164 Application settings-panel-state field-struct MC68000 execution qualification failed.' }

$applicationSettingsPersistenceStateFieldStructMg2165Hunk = Join-Path $outDir 'mui-application-settings-persistence-state-field-struct-mg2165.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationSettingsPersistenceStateFieldStructMg2165Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2165 Application settings-persistence-state field-struct MC68000 execution qualification failed.' }

$applicationUsedClassesStateFieldStructMg2166Hunk = Join-Path $outDir 'mui-application-used-classes-state-field-struct-mg2166.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationUsedClassesStateFieldStructMg2166Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2166 Application UsedClasses-state field-struct MC68000 execution qualification failed.' }

$applicationWindowRelationshipStateFieldStructMg2167Hunk = Join-Path $outDir 'mui-application-window-relationship-state-field-struct-mg2167.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationWindowRelationshipStateFieldStructMg2167Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2167 Application window-relationship-state field-struct MC68000 execution qualification failed.' }

$areaActivationStateFieldStructMg2168Hunk = Join-Path $outDir 'mui-area-activation-state-field-struct-mg2168.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaActivationStateFieldStructMg2168Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2168 Area activation-state field-struct MC68000 execution qualification failed.' }

$areaBuiltinFontStateFieldStructMg2169Hunk = Join-Path $outDir 'mui-area-builtin-font-state-field-struct-mg2169.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaBuiltinFontStateFieldStructMg2169Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2169 Area BuiltinFont-state field-struct MC68000 execution qualification failed.' }

$areaContextMenuStateFieldStructMg2170Hunk = Join-Path $outDir 'mui-area-context-menu-state-field-struct-mg2170.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaContextMenuStateFieldStructMg2170Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2170 Area ContextMenu-state field-struct MC68000 execution qualification failed.' }

$areaControlCharStateFieldStructMg2171Hunk = Join-Path $outDir 'mui-area-control-char-state-field-struct-mg2171.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaControlCharStateFieldStructMg2171Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2171 Area ControlChar-state field-struct MC68000 execution qualification failed.' }

$areaCustomFontStateFieldStructMg2172Hunk = Join-Path $outDir 'mui-area-custom-font-state-field-struct-mg2172.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaCustomFontStateFieldStructMg2172Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2172 Area CustomFont-state field-struct MC68000 execution qualification failed.' }

$areaCycleChainStateFieldStructMg2173Hunk = Join-Path $outDir 'mui-area-cycle-chain-state-field-struct-mg2173.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaCycleChainStateFieldStructMg2173Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2173 Area CycleChain-state field-struct MC68000 execution qualification failed.' }

$areaCustomFontRuntimeStateFieldStructMg2174Hunk = Join-Path $outDir 'mui-area-custom-font-runtime-state-field-struct-mg2174.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaCustomFontRuntimeStateFieldStructMg2174Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2174 Area CustomFont-runtime-state field-struct MC68000 execution qualification failed.' }

$areaDisappearPolicyStateFieldStructMg2175Hunk = Join-Path $outDir 'mui-area-disappear-policy-state-field-struct-mg2175.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaDisappearPolicyStateFieldStructMg2175Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2175 Area DisappearPolicy-state field-struct MC68000 execution qualification failed.' }

$areaDoubleBufferStateFieldStructMg2176Hunk = Join-Path $outDir 'mui-area-double-buffer-state-field-struct-mg2176.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaDoubleBufferStateFieldStructMg2176Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2176 Area DoubleBuffer-state field-struct MC68000 execution qualification failed.' }

$areaDoubleClickStateFieldStructMg2177Hunk = Join-Path $outDir 'mui-area-double-click-state-field-struct-mg2177.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaDoubleClickStateFieldStructMg2177Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2177 Area DoubleClick-state field-struct MC68000 execution qualification failed.' }

$areaDragPolicyStateFieldStructMg2178Hunk = Join-Path $outDir 'mui-area-drag-policy-state-field-struct-mg2178.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaDragPolicyStateFieldStructMg2178Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2178 Area DragPolicy-state field-struct MC68000 execution qualification failed.' }

$areaFixedTextStateFieldStructMg2179Hunk = Join-Path $outDir 'mui-area-fixed-text-state-field-struct-mg2179.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaFixedTextStateFieldStructMg2179Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2179 Area FixedText-state field-struct MC68000 execution qualification failed.' }

$areaFloatingStateFieldStructMg2180Hunk = Join-Path $outDir 'mui-area-floating-state-field-struct-mg2180.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaFloatingStateFieldStructMg2180Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2180 Area Floating-state field-struct MC68000 execution qualification failed.' }

$areaFontSelectionStateFieldStructMg2181Hunk = Join-Path $outDir 'mui-area-font-selection-state-field-struct-mg2181.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaFontSelectionStateFieldStructMg2181Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2181 Area FontSelection-state field-struct MC68000 execution qualification failed.' }

$areaGeometryStateFieldStructMg2182Hunk = Join-Path $outDir 'mui-area-geometry-state-field-struct-mg2182.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaGeometryStateFieldStructMg2182Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2182 Area Geometry-state field-struct MC68000 execution qualification failed.' }

$areaLayoutPolicyStateFieldStructMg2183Hunk = Join-Path $outDir 'mui-area-layout-policy-state-field-struct-mg2183.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaLayoutPolicyStateFieldStructMg2183Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2183 Area LayoutPolicy-state field-struct MC68000 execution qualification failed.' }

$areaPresentationStateFieldStructMg2184Hunk = Join-Path $outDir 'mui-area-presentation-state-field-struct-mg2184.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaPresentationStateFieldStructMg2184Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2184 Area Presentation-state field-struct MC68000 execution qualification failed.' }

$areaRenderPolicyStateFieldStructMg2185Hunk = Join-Path $outDir 'mui-area-render-policy-state-field-struct-mg2185.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaRenderPolicyStateFieldStructMg2185Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2185 Area RenderPolicy-state field-struct MC68000 execution qualification failed.' }

$areaShortHelpStateFieldStructMg2186Hunk = Join-Path $outDir 'mui-area-short-help-state-field-struct-mg2186.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaShortHelpStateFieldStructMg2186Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2186 Area ShortHelp-state field-struct MC68000 execution qualification failed.' }

$areaTextColorStateFieldStructMg2187Hunk = Join-Path $outDir 'mui-area-text-color-state-field-struct-mg2187.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaTextColorStateFieldStructMg2187Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2187 Area TextColor-state field-struct MC68000 execution qualification failed.' }

$areaTimerStateFieldStructMg2188Hunk = Join-Path $outDir 'mui-area-timer-state-field-struct-mg2188.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaTimerStateFieldStructMg2188Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2188 Area Timer-state field-struct MC68000 execution qualification failed.' }

$areaTimerEventStateFieldStructMg2189Hunk = Join-Path $outDir 'mui-area-timer-event-state-field-struct-mg2189.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaTimerEventStateFieldStructMg2189Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2189 Area Timer-event-state field-struct MC68000 execution qualification failed.' }

$areaWeightStateFieldStructMg2190Hunk = Join-Path $outDir 'mui-area-weight-state-field-struct-mg2190.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaWeightStateFieldStructMg2190Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2190 Area Weight-state field-struct MC68000 execution qualification failed.' }

$gaugeInfoRateStateFieldStructMg2203Hunk = Join-Path $outDir 'mui-gauge-inforate-state-field-struct-mg2203.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $gaugeInfoRateStateFieldStructMg2203Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2203 Gauge InfoRate-state field-struct MC68000 execution qualification failed.' }

$gaugeInfoTextStateFieldStructMg2204Hunk = Join-Path $outDir 'mui-gauge-infotext-state-field-struct-mg2204.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $gaugeInfoTextStateFieldStructMg2204Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2204 Gauge InfoText-state field-struct MC68000 execution qualification failed.' }

$gaugeStateFieldStructMg2205Hunk = Join-Path $outDir 'mui-gauge-state-field-struct-mg2205.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $gaugeStateFieldStructMg2205Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2205 Gauge-state field-struct MC68000 execution qualification failed.' }

$groupLayoutHookStateFieldStructMg2206Hunk = Join-Path $outDir 'mui-group-layout-hook-state-field-struct-mg2206.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $groupLayoutHookStateFieldStructMg2206Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2206 Group layout-hook-state field-struct MC68000 execution qualification failed.' }

$groupLayoutPolicyStateFieldStructMg2207Hunk = Join-Path $outDir 'mui-group-layout-policy-state-field-struct-mg2207.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $groupLayoutPolicyStateFieldStructMg2207Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2207 Group layout-policy-state field-struct MC68000 execution qualification failed.' }

$groupGridStateFieldStructMg2208Hunk = Join-Path $outDir 'mui-group-grid-state-field-struct-mg2208.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $groupGridStateFieldStructMg2208Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2208 Group-grid-state field-struct MC68000 execution qualification failed.' }
$groupGridSpecFieldStructMg2293Hunk = Join-Path $outDir 'mui-group-grid-spec-field-struct-mg2293.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $groupGridSpecFieldStructMg2293Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2293 Group-grid-spec field-struct MC68000 execution qualification failed.' }

$sleepStateFieldStructMg2294Hunk = Join-Path $outDir 'mui-sleep-state-field-struct-mg2294.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $sleepStateFieldStructMg2294Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2294 Sleep-state field-struct MC68000 execution qualification failed.' }

$groupPageStateFieldStructMg2209Hunk = Join-Path $outDir 'mui-group-page-state-field-struct-mg2209.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $groupPageStateFieldStructMg2209Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2209 Group-page-state field-struct MC68000 execution qualification failed.' }

$imageSpecStateFieldStructMg2210Hunk = Join-Path $outDir 'mui-image-spec-state-field-struct-mg2210.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $imageSpecStateFieldStructMg2210Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2210 Image-spec-state field-struct MC68000 execution qualification failed.' }

$imageRenderStateFieldStructMg2211Hunk = Join-Path $outDir 'mui-image-render-state-field-struct-mg2211.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $imageRenderStateFieldStructMg2211Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2211 Image-render-state field-struct MC68000 execution qualification failed.' }

$imageOldImageStateFieldStructMg2212Hunk = Join-Path $outDir 'mui-image-oldimage-state-field-struct-mg2212.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $imageOldImageStateFieldStructMg2212Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2212 Image OldImage-state field-struct MC68000 execution qualification failed.' }

$imageFontMatchStringStateFieldStructMg2213Hunk = Join-Path $outDir 'mui-image-fontmatchstring-state-field-struct-mg2213.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $imageFontMatchStringStateFieldStructMg2213Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2213 Image FontMatchString-state field-struct MC68000 execution qualification failed.' }

$imageFontMatchStateFieldStructMg2214Hunk = Join-Path $outDir 'mui-image-fontmatch-state-field-struct-mg2214.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $imageFontMatchStateFieldStructMg2214Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2214 Image FontMatch-state field-struct MC68000 execution qualification failed.' }

$levelmeterLabelStateFieldStructMg2215Hunk = Join-Path $outDir 'mui-levelmeter-label-state-field-struct-mg2215.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $levelmeterLabelStateFieldStructMg2215Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2215 Levelmeter-label-state field-struct MC68000 execution qualification failed.' }

$levelmeterPresentationStateFieldStructMg2216Hunk = Join-Path $outDir 'mui-levelmeter-presentation-state-field-struct-mg2216.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $levelmeterPresentationStateFieldStructMg2216Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2216 Levelmeter-presentation-state field-struct MC68000 execution qualification failed.' }

$numericFormatStateFieldStructMg2217Hunk = Join-Path $outDir 'mui-numeric-format-state-field-struct-mg2217.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $numericFormatStateFieldStructMg2217Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2217 Numeric-format-state field-struct MC68000 execution qualification failed.' }

$propRangeStateFieldStructMg2218Hunk = Join-Path $outDir 'mui-prop-range-state-field-struct-mg2218.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $propRangeStateFieldStructMg2218Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2218 Prop-range-state field-struct MC68000 execution qualification failed.' }

$propPolicyStateFieldStructMg2219Hunk = Join-Path $outDir 'mui-prop-policy-state-field-struct-mg2219.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $propPolicyStateFieldStructMg2219Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2219 Prop-policy-state field-struct MC68000 execution qualification failed.' }

$scrollbarLayoutStateFieldStructMg2220Hunk = Join-Path $outDir 'mui-scrollbar-layout-state-field-struct-mg2220.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $scrollbarLayoutStateFieldStructMg2220Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2220 Scrollbar-layout-state field-struct MC68000 execution qualification failed.' }

$sliderPresentationStateFieldStructMg2221Hunk = Join-Path $outDir 'mui-slider-presentation-state-field-struct-mg2221.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $sliderPresentationStateFieldStructMg2221Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2221 Slider-presentation-state field-struct MC68000 execution qualification failed.' }

$rectangleBarTitleStateFieldStructMg2222Hunk = Join-Path $outDir 'mui-rectangle-bartitle-state-field-struct-mg2222.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $rectangleBarTitleStateFieldStructMg2222Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2222 Rectangle bar-title-state field-struct MC68000 execution qualification failed.' }

$rectanglePresentationStateFieldStructMg2223Hunk = Join-Path $outDir 'mui-rectangle-presentation-state-field-struct-mg2223.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $rectanglePresentationStateFieldStructMg2223Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2223 Rectangle-presentation-state field-struct MC68000 execution qualification failed.' }

$scalePresentationStateFieldStructMg2224Hunk = Join-Path $outDir 'mui-scale-presentation-state-field-struct-mg2224.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $scalePresentationStateFieldStructMg2224Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2224 Scale-presentation-state field-struct MC68000 execution qualification failed.' }

$registerPolicyStateFieldStructMg2225Hunk = Join-Path $outDir 'mui-register-policy-state-field-struct-mg2225.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $registerPolicyStateFieldStructMg2225Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2225 Register-policy-state field-struct MC68000 execution qualification failed.' }

$keyadjustTextFieldStructMg2226Hunk = Join-Path $outDir 'mui-keyadjust-text-field-struct-mg2226.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $keyadjustTextFieldStructMg2226Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2226 Keyadjust-text field-struct MC68000 execution qualification failed.' }

$makeObjectPreParseFieldStructMg2227Hunk = Join-Path $outDir 'mui-makeobject-preparse-field-struct-mg2227.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $makeObjectPreParseFieldStructMg2227Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2227 MUI_MakeObject preparse field-struct MC68000 execution qualification failed.' }

$stringscrollPointerStateFieldStructMg2228Hunk = Join-Path $outDir 'mui-stringscroll-pointer-state-field-struct-mg2228.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringscrollPointerStateFieldStructMg2228Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2228 Stringscroll-pointer-state field-struct MC68000 execution qualification failed.' }

$helpStateFieldStructMg2229Hunk = Join-Path $outDir 'mui-help-state-field-struct-mg2229.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $helpStateFieldStructMg2229Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2229 Help-state field-struct MC68000 execution qualification failed.' }

$stringAcknowledgeStateFieldStructMg2230Hunk = Join-Path $outDir 'mui-string-acknowledge-state-field-struct-mg2230.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringAcknowledgeStateFieldStructMg2230Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2230 String-acknowledge-state field-struct MC68000 execution qualification failed.' }

$stringContentsStateFieldStructMg2231Hunk = Join-Path $outDir 'mui-string-contents-state-field-struct-mg2231.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringContentsStateFieldStructMg2231Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2231 String-contents-state field-struct MC68000 execution qualification failed.' }

$stringCursorStateFieldStructMg2232Hunk = Join-Path $outDir 'mui-string-cursor-state-field-struct-mg2232.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringCursorStateFieldStructMg2232Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2232 String-cursor-state field-struct MC68000 execution qualification failed.' }

$stringInteractionStateFieldStructMg2233Hunk = Join-Path $outDir 'mui-string-interaction-state-field-struct-mg2233.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringInteractionStateFieldStructMg2233Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2233 String-interaction-state field-struct MC68000 execution qualification failed.' }

$stringFilterStateFieldStructMg2234Hunk = Join-Path $outDir 'mui-string-filter-state-field-struct-mg2234.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringFilterStateFieldStructMg2234Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2234 String-filter-state field-struct MC68000 execution qualification failed.' }

$stringPlaceholderStateFieldStructMg2235Hunk = Join-Path $outDir 'mui-string-placeholder-state-field-struct-mg2235.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringPlaceholderStateFieldStructMg2235Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2235 String-placeholder-state field-struct MC68000 execution qualification failed.' }

$stringEditHookStateFieldStructMg2236Hunk = Join-Path $outDir 'mui-string-edit-hook-state-field-struct-mg2236.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringEditHookStateFieldStructMg2236Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2236 String-edit-hook-state field-struct MC68000 execution qualification failed.' }

$stringPresentationStateFieldStructMg2237Hunk = Join-Path $outDir 'mui-string-presentation-state-field-struct-mg2237.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringPresentationStateFieldStructMg2237Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2237 String-presentation-state field-struct MC68000 execution qualification failed.' }

$stringAttachedListStateFieldStructMg2238Hunk = Join-Path $outDir 'mui-string-attached-list-state-field-struct-mg2238.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringAttachedListStateFieldStructMg2238Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2238 String-attached-list-state field-struct MC68000 execution qualification failed.' }

$stringSpellCheckingStateFieldStructMg2239Hunk = Join-Path $outDir 'mui-string-spell-checking-state-field-struct-mg2239.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringSpellCheckingStateFieldStructMg2239Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2239 String-spell-checking-state field-struct MC68000 execution qualification failed.' }

$stringScrollMetricsStateFieldStructMg2240Hunk = Join-Path $outDir 'mui-string-scroll-metrics-state-field-struct-mg2240.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringScrollMetricsStateFieldStructMg2240Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2240 String-scroll-metrics-state field-struct MC68000 execution qualification failed.' }

$textContentsStateFieldStructMg2241Hunk = Join-Path $outDir 'mui-text-contents-state-field-struct-mg2241.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $textContentsStateFieldStructMg2241Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2241 Text-contents-state field-struct MC68000 execution qualification failed.' }

$textCopyStateFieldStructMg2242Hunk = Join-Path $outDir 'mui-text-copy-state-field-struct-mg2242.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $textCopyStateFieldStructMg2242Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2242 Text-copy-state field-struct MC68000 execution qualification failed.' }

$textPreParseStateFieldStructMg2243Hunk = Join-Path $outDir 'mui-text-preparse-state-field-struct-mg2243.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $textPreParseStateFieldStructMg2243Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2243 Text-preparse-state field-struct MC68000 execution qualification failed.' }

$textPresentationStateFieldStructMg2244Hunk = Join-Path $outDir 'mui-text-presentation-state-field-struct-mg2244.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $textPresentationStateFieldStructMg2244Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2244 Text-presentation-state field-struct MC68000 execution qualification failed.' }

$textShortenedStateFieldStructMg2245Hunk = Join-Path $outDir 'mui-text-shortened-state-field-struct-mg2245.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $textShortenedStateFieldStructMg2245Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2245 Text-shortened-state field-struct MC68000 execution qualification failed.' }

$textUnicodeStateFieldStructMg2246Hunk = Join-Path $outDir 'mui-text-unicode-state-field-struct-mg2246.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $textUnicodeStateFieldStructMg2246Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2246 Text-Unicode-state field-struct MC68000 execution qualification failed.' }

$windowControlStateFieldStructMg2247Hunk = Join-Path $outDir 'mui-window-control-state-field-struct-mg2247.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowControlStateFieldStructMg2247Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2247 Window-control-state field-struct MC68000 execution qualification failed.' }

$windowEventStateFieldStructMg2248Hunk = Join-Path $outDir 'mui-window-event-state-field-struct-mg2248.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowEventStateFieldStructMg2248Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2248 Window-event-state field-struct MC68000 execution qualification failed.' }

$windowFocusStateFieldStructMg2249Hunk = Join-Path $outDir 'mui-window-focus-state-field-struct-mg2249.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowFocusStateFieldStructMg2249Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2249 Window-focus-state field-struct MC68000 execution qualification failed.' }

$windowLifecycleStateFieldStructMg2250Hunk = Join-Path $outDir 'mui-window-lifecycle-state-field-struct-mg2250.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowLifecycleStateFieldStructMg2250Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2250 Window-lifecycle-state field-struct MC68000 execution qualification failed.' }

$windowInteractionStateFieldStructMg2251Hunk = Join-Path $outDir 'mui-window-interaction-state-field-struct-mg2251.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowInteractionStateFieldStructMg2251Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2251 Window-interaction-state field-struct MC68000 execution qualification failed.' }

$windowPresentationStateFieldStructMg2252Hunk = Join-Path $outDir 'mui-window-presentation-state-field-struct-mg2252.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowPresentationStateFieldStructMg2252Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2252 Window-presentation-state field-struct MC68000 execution qualification failed.' }

$windowRelationshipStateFieldStructMg2253Hunk = Join-Path $outDir 'mui-window-relationship-state-field-struct-mg2253.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowRelationshipStateFieldStructMg2253Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2253 Window-relationship-state field-struct MC68000 execution qualification failed.' }

$windowVisualStateFieldStructMg2254Hunk = Join-Path $outDir 'mui-window-visual-state-field-struct-mg2254.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowVisualStateFieldStructMg2254Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2254 Window-visual-state field-struct MC68000 execution qualification failed.' }

$windowOpenPolicyStateFieldStructMg2255Hunk = Join-Path $outDir 'mui-window-open-policy-state-field-struct-mg2255.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowOpenPolicyStateFieldStructMg2255Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2255 Window-open-policy-state field-struct MC68000 execution qualification failed.' }

$windowEventReuseStateFieldStructMg2256Hunk = Join-Path $outDir 'mui-window-event-reuse-state-field-struct-mg2256.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowEventReuseStateFieldStructMg2256Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2256 Window-event-reuse-state field-struct MC68000 execution qualification failed.' }

$windowInputEventFieldStructMg2257Hunk = Join-Path $outDir 'mui-window-input-event-field-struct-mg2257.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $windowInputEventFieldStructMg2257Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2257 Window-input-event field-struct MC68000 execution qualification failed.' }

$stringIntegerStateFieldStructMg2258Hunk = Join-Path $outDir 'mui-string-integer-state-field-struct-mg2258.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringIntegerStateFieldStructMg2258Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2258 String-integer-state field-struct MC68000 execution qualification failed.' }

$menuItemTriggerFieldStructMg2259Hunk = Join-Path $outDir 'mui-menuitem-trigger-field-struct-mg2259.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $menuItemTriggerFieldStructMg2259Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2259 MenuItem-trigger field-struct MC68000 execution qualification failed.' }

$intuiTextTriggerFieldStructMg2260Hunk = Join-Path $outDir 'mui-intuitext-trigger-field-struct-mg2260.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $intuiTextTriggerFieldStructMg2260Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2260 IntuiText-trigger field-struct MC68000 execution qualification failed.' }

$specialistHookMessageFieldStructMg2261Hunk = Join-Path $outDir 'mui-specialist-hook-message-field-struct-mg2261.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $specialistHookMessageFieldStructMg2261Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2261 Specialist-hook-message field-struct MC68000 execution qualification failed.' }

$storePolicyStateFieldStructMg2262Hunk = Join-Path $outDir 'mui-store-policy-state-field-struct-mg2262.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $storePolicyStateFieldStructMg2262Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2262 Store-policy-state field-struct MC68000 execution qualification failed.' }

$appMessageFieldStructMg2263Hunk = Join-Path $outDir 'mui-appmessage-field-struct-mg2263.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $appMessageFieldStructMg2263Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2263 AppMessage field-struct MC68000 execution qualification failed.' }

$workbenchArgumentFieldStructMg2264Hunk = Join-Path $outDir 'mui-workbench-argument-field-struct-mg2264.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $workbenchArgumentFieldStructMg2264Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2264 Workbench-argument field-struct MC68000 execution qualification failed.' }

$applicationPersistenceFrameFieldStructMg2265Hunk = Join-Path $outDir 'mui-application-persistence-frame-field-struct-mg2265.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationPersistenceFrameFieldStructMg2265Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2265 Application-persistence-frame field-struct MC68000 execution qualification failed.' }

$applicationSettingsFieldStructMg2266Hunk = Join-Path $outDir 'mui-application-settings-field-struct-mg2266.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationSettingsFieldStructMg2266Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2266 Application-settings field-struct MC68000 execution qualification failed.' }

$applicationQueuePacketFieldStructMg2267Hunk = Join-Path $outDir 'mui-application-queue-packet-field-struct-mg2267.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationQueuePacketFieldStructMg2267Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2267 Application-queue-packet field-struct MC68000 execution qualification failed.' }

$applicationInputPacketFieldStructMg2268Hunk = Join-Path $outDir 'mui-application-input-packet-field-struct-mg2268.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationInputPacketFieldStructMg2268Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2268 Application-input-packet field-struct MC68000 execution qualification failed.' }

$applicationCommandFieldStructMg2269Hunk = Join-Path $outDir 'mui-application-command-field-struct-mg2269.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationCommandFieldStructMg2269Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2269 Application-command field-struct MC68000 execution qualification failed.' }

$applicationWindowListFieldStructMg2270Hunk = Join-Path $outDir 'mui-application-window-list-field-struct-mg2270.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationWindowListFieldStructMg2270Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2270 Application-window-list field-struct MC68000 execution qualification failed.' }

$applicationWindowNodeFieldStructMg2271Hunk = Join-Path $outDir 'mui-application-window-node-field-struct-mg2271.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationWindowNodeFieldStructMg2271Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2271 Application-window-node field-struct MC68000 execution qualification failed.' }

$eventHandlerNodeFieldStructMg2272Hunk = Join-Path $outDir 'mui-event-handler-node-field-struct-mg2272.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $eventHandlerNodeFieldStructMg2272Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2272 Event-handler-node field-struct MC68000 execution qualification failed.' }

$inputHandlerFieldStructMg2273Hunk = Join-Path $outDir 'mui-input-handler-field-struct-mg2273.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $inputHandlerFieldStructMg2273Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2273 Input-handler field-struct MC68000 execution qualification failed.' }

$applicationPresentationFieldStructMg2274Hunk = Join-Path $outDir 'mui-application-presentation-field-struct-mg2274.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationPresentationFieldStructMg2274Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2274 Application-presentation field-struct MC68000 execution qualification failed.' }

$applicationSettingsPacketFieldStructMg2275Hunk = Join-Path $outDir 'mui-application-settings-packet-field-struct-mg2275.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationSettingsPacketFieldStructMg2275Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2275 Application-settings packet field-struct MC68000 execution qualification failed.' }

$areaActivationPacketFieldStructMg2276Hunk = Join-Path $outDir 'mui-area-activation-packet-field-struct-mg2276.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaActivationPacketFieldStructMg2276Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2276 Area-activation packet field-struct MC68000 execution qualification failed.' }

$areaBubblePacketFieldStructMg2277Hunk = Join-Path $outDir 'mui-area-bubble-packet-field-struct-mg2277.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaBubblePacketFieldStructMg2277Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2277 Area-bubble packet field-struct MC68000 execution qualification failed.' }

$areaContextMenuPacketFieldStructMg2278Hunk = Join-Path $outDir 'mui-area-context-menu-packet-field-struct-mg2278.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaContextMenuPacketFieldStructMg2278Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2278 Area-context-menu packet field-struct MC68000 execution qualification failed.' }

$areaCustomFontPacketFieldStructMg2279Hunk = Join-Path $outDir 'mui-area-custom-font-packet-field-struct-mg2279.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaCustomFontPacketFieldStructMg2279Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2279 Area-custom-font packet field-struct MC68000 execution qualification failed.' }

$areaResizePacketFieldStructMg2280Hunk = Join-Path $outDir 'mui-area-resize-packet-field-struct-mg2280.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaResizePacketFieldStructMg2280Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2280 Area-resize packet field-struct MC68000 execution qualification failed.' }

$areaDragPacketFieldStructMg2281Hunk = Join-Path $outDir 'mui-area-drag-packet-field-struct-mg2281.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaDragPacketFieldStructMg2281Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2281 Area-drag packet field-struct MC68000 execution qualification failed.' }

$areaResizeStateFieldStructMg2282Hunk = Join-Path $outDir 'mui-area-resize-state-field-struct-mg2282.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaResizeStateFieldStructMg2282Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2282 Area-resize state field-struct MC68000 execution qualification failed.' }

$areaDragStateFieldStructMg2283Hunk = Join-Path $outDir 'mui-area-drag-state-field-struct-mg2283.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaDragStateFieldStructMg2283Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2283 Area-drag state field-struct MC68000 execution qualification failed.' }

$areaShortHelpFieldStructMg2284Hunk = Join-Path $outDir 'mui-area-short-help-field-struct-mg2284.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaShortHelpFieldStructMg2284Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2284 Area-ShortHelp field-struct MC68000 execution qualification failed.' }

$dirlistFieldStructMg2285Hunk = Join-Path $outDir 'mui-dirlist-field-struct-mg2285.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $dirlistFieldStructMg2285Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2285 Dirlist field-struct MC68000 execution qualification failed.' }

$aslFieldStructMg2286Hunk = Join-Path $outDir 'mui-asl-field-struct-mg2286.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $aslFieldStructMg2286Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2286 ASL field-struct MC68000 execution qualification failed.' }

$aslTagItemFieldStructMg2287Hunk = Join-Path $outDir 'mui-asl-tag-item-field-struct-mg2287.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $aslTagItemFieldStructMg2287Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2287 ASL TagItem field-struct MC68000 execution qualification failed.' }

$callHookFieldStructMg2288Hunk = Join-Path $outDir 'mui-callhook-field-struct-mg2288.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $callHookFieldStructMg2288Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2288 CallHook field-struct MC68000 execution qualification failed.' }

$applicationIconifyTitleHunk = Join-Path $outDir 'mui-application-iconify-title.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationIconifyTitleHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application IconifyTitle MC68000 execution qualification failed.' }

$applicationUseScreenNotifyHunk = Join-Path $outDir 'mui-application-use-screen-notify.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationUseScreenNotifyHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application UseScreenNotify MC68000 execution qualification failed.' }

$applicationDiskObjectHunk = Join-Path $outDir 'mui-application-disk-object.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationDiskObjectHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application DiskObject MC68000 execution qualification failed.' }

$applicationDropObjectHunk = Join-Path $outDir 'mui-application-drop-object.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationDropObjectHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application DropObject MC68000 execution qualification failed.' }

$applicationMenuEventStateHunk = Join-Path $outDir 'mui-application-menu-event-state.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationMenuEventStateHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application MenuAction/MenuHelp MC68000 execution qualification failed.' }

$applicationMenuTransportHunk = Join-Path $outDir 'mui-application-menu-transport.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationMenuTransportHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application Menu transport MC68000 execution qualification failed.' }

$applicationAboutMuiHunk = Join-Path $outDir 'mui-application-aboutmui.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationAboutMuiHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application AboutMUI MC68000 execution qualification failed.' }

$applicationShowHelpHunk = Join-Path $outDir 'mui-application-showhelp.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationShowHelpHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application ShowHelp MC68000 execution qualification failed.' }

$applicationLoopHunk = Join-Path $outDir 'mui-application-loop.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationLoopHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application Execute/Run MC68000 execution qualification failed.' }

$applicationDefaultConfigHunk = Join-Path $outDir 'mui-application-defaultconfig.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationDefaultConfigHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application DefaultConfigItem MC68000 execution qualification failed.' }

$applicationOpenConfigHunk = Join-Path $outDir 'mui-application-openconfig.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationOpenConfigHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application OpenConfigWindow MC68000 execution qualification failed.' }

$applicationSettingsPanelHunk = Join-Path $outDir 'mui-application-settings-panel.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationSettingsPanelHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application BuildSettingsPanel MC68000 execution qualification failed.' }

$applicationSettingsIoHunk = Join-Path $outDir 'mui-application-settings-io.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationSettingsIoHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application Save/Load MC68000 execution qualification failed.' }

$objectPersistenceHunk = Join-Path $outDir 'mui-object-persistence.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $objectPersistenceHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 MUIM_Export/Import MC68000 execution qualification failed.' }
$objectPersistenceMessageCodecHunk = Join-Path $outDir 'mui-object-persistence-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $objectPersistenceMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 MUIM_Export/Import packet codec MC68000 execution qualification failed.' }
$getConfigItemMessageCodecHunk = Join-Path $outDir 'mui-get-config-item-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $getConfigItemMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 MUIM_GetConfigItem packet codec MC68000 execution qualification failed.' }
$getConfigItemFieldStructMg2290Hunk = Join-Path $outDir 'mui-get-config-item-field-struct-mg2290.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $getConfigItemFieldStructMg2290Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2290 GetConfigItem field-struct MC68000 execution qualification failed.' }
$notifySetAsStringPacketsHunk = Join-Path $outDir 'mui-notify-setasstring-packets.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $notifySetAsStringPacketsHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 MUIM_SetAsString packet MC68000 execution qualification failed.' }
$boopsiQueryMessageCodecHunk = Join-Path $outDir 'mui-boopsi-query-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $boopsiQueryMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 BoopsiQuery packet codec MC68000 execution qualification failed.' }
$boopsiQueryFieldStructMg2289Hunk = Join-Path $outDir 'mui-boopsi-query-field-struct-mg2289.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $boopsiQueryFieldStructMg2289Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2289 BoopsiQuery field-struct MC68000 execution qualification failed.' }
$userDataMessageCodecHunk = Join-Path $outDir 'mui-user-data-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $userDataMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 UserData packet codec MC68000 execution qualification failed.' }
$familyGetChildMessageCodecHunk = Join-Path $outDir 'mui-family-get-child-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $familyGetChildMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 MUIM_Family_GetChild packet codec MC68000 execution qualification failed.' }
$familyDoChildMethodsMessageCodecHunk = Join-Path $outDir 'mui-family-do-child-methods-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $familyDoChildMethodsMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 MUIM_Family_DoChildMethods packet codec MC68000 execution qualification failed.' }
$familyMutationMessageCodecHunk = Join-Path $outDir 'mui-family-mutation-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $familyMutationMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Family mutation packet codec MC68000 execution qualification failed.' }
$familyProjectionListVectorCodecHunk = Join-Path $outDir 'mui-family-projection-list-vector-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $familyProjectionListVectorCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG604 Family projection list/vector codec MC68000 execution qualification failed.' }
$familyMutationVectorStructCodecHunk = Join-Path $outDir 'mui-family-mutation-vector-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $familyMutationVectorStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG1374 Family mutation vector struct codec MC68000 execution qualification failed.' }
$applicationPersistenceTreeHunk = Join-Path $outDir 'mui-application-persistence-tree.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationPersistenceTreeHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 application-tree persistence MC68000 execution qualification failed.' }

$applicationCheckRefreshHunk = Join-Path $outDir 'mui-application-checkrefresh.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationCheckRefreshHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application CheckRefresh MC68000 execution qualification failed.' }

$applicationMenuHunk = Join-Path $outDir 'mui-application-menu.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationMenuHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application menu MC68000 execution qualification failed.' }

$applicationQueueHunk = Join-Path $outDir 'mui-application-queue.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $applicationQueueHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Application PushMethod/UnpushMethod MC68000 execution qualification failed.' }

$commonControlHunk = Join-Path $outDir 'mui-commoncontrol.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $commonControlHunk
if ($LASTEXITCODE -ne 0) { throw 'MG07 MC68000 execution qualification failed.' }
$stringScrollAttributeHunk = Join-Path $outDir 'mui-string-scroll-attributes.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringScrollAttributeHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 String.mui scroll-attribute MC68000 execution qualification failed.' }
$stringScrollAttributeUtf8MetricsHunk = Join-Path $outDir 'mui-string-scroll-attributes-utf8-metrics.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringScrollAttributeUtf8MetricsHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 String.mui UTF-8 scroll-attribute metrics MC68000 execution qualification failed.' }

$collectionHunk = Join-Path $outDir 'mui-collection.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionHunk
if ($LASTEXITCODE -ne 0) { throw 'MG08 MC68000 execution qualification failed.' }
$collectionListPacketsHunk = Join-Path $outDir 'mui-collection-list-packets.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListPacketsHunk
if ($LASTEXITCODE -ne 0) { throw 'MG08 List packet seam MC68000 execution qualification failed.' }
$collectionListAdvancedPacketsHunk = Join-Path $outDir 'mui-collection-list-advanced-packets.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListAdvancedPacketsHunk
if ($LASTEXITCODE -ne 0) { throw 'MG08 List advanced packet seam MC68000 execution qualification failed.' }
$collectionListAdvancedMessageCodecHunk = Join-Path $outDir 'mui-collection-list-advanced-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListAdvancedMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List advanced message codec MC68000 execution qualification failed.' }
$collectionListAdvancedMethodHeaderStructCodecHunk = Join-Path $outDir 'mui-collection-list-advanced-method-header-struct-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListAdvancedMethodHeaderStructCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG2143 List advanced method-header struct codec MC68000 execution qualification failed.' }
$collectionListBasicMessageCodecHunk = Join-Path $outDir 'mui-collection-list-basic-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListBasicMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List basic message codec MC68000 execution qualification failed.' }
$collectionSurfaceMessageCodecHunk = Join-Path $outDir 'mui-collection-surface-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionSurfaceMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 collection surface message codec MC68000 execution qualification failed.' }
$collectionListRecordPacketsHunk = Join-Path $outDir 'mui-collection-list-record-packets.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListRecordPacketsHunk
if ($LASTEXITCODE -ne 0) { throw 'MG08 List record packet seam MC68000 execution qualification failed.' }
$collectionListRecordMessageCodecHunk = Join-Path $outDir 'mui-collection-list-record-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListRecordMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List record message codec MC68000 execution qualification failed.' }
$collectionListSurfacePacketsHunk = Join-Path $outDir 'mui-collection-list-surface-packets.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListSurfacePacketsHunk
if ($LASTEXITCODE -ne 0) { throw 'MG08 List surface packet seam MC68000 execution qualification failed.' }
$collectionListEditPacketsHunk = Join-Path $outDir 'mui-collection-list-edit-packets.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListEditPacketsHunk
if ($LASTEXITCODE -ne 0) { throw 'MG08 List edit packet seam MC68000 execution qualification failed.' }
$collectionListEditMessageCodecHunk = Join-Path $outDir 'mui-collection-list-edit-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListEditMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List edit message codec MC68000 execution qualification failed.' }
$collectionListEditCommitHunk = Join-Path $outDir 'mui-collection-list-edit-commit.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListEditCommitHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List edit commit MC68000 execution qualification failed.' }
$collectionListEditStringArrayHunk = Join-Path $outDir 'mui-collection-list-edit-stringarray.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListEditStringArrayHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List StringArray edit commit MC68000 execution qualification failed.' }
$collectionListEditPlacementHunk = Join-Path $outDir 'mui-collection-list-edit-placement.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListEditPlacementHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List editor placement MC68000 execution qualification failed.' }
$collectionListTitleArrayHunk = Join-Path $outDir 'mui-collection-list-title-array.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListTitleArrayHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List TitleArray MC68000 execution qualification failed.' }
$listPointerSlotCodecHunk = Join-Path $outDir 'mui-list-pointer-slot-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listPointerSlotCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG606 List pointer-slot codec MC68000 execution qualification failed.' }
$listPointerVectorCodecHunk = Join-Path $outDir 'mui-list-pointer-vector-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listPointerVectorCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG607 List pointer-vector codec MC68000 execution qualification failed.' }
$listColumnOrderByteCodecHunk = Join-Path $outDir 'mui-list-column-order-byte-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listColumnOrderByteCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG608 List ColumnOrder byte codec MC68000 execution qualification failed.' }
$collectionListAdjustHeightHunk = Join-Path $outDir 'mui-collection-list-adjust-height.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListAdjustHeightHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List AdjustHeight MC68000 execution qualification failed.' }
$collectionListAdjustWidthHunk = Join-Path $outDir 'mui-collection-list-adjust-width.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListAdjustWidthHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List AdjustWidth MC68000 execution qualification failed.' }
$collectionListStripesHunk = Join-Path $outDir 'mui-collection-list-stripes.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListStripesHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List Stripes MC68000 execution qualification failed.' }
$collectionListDropMarkHunk = Join-Path $outDir 'mui-collection-list-drop-mark.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListDropMarkHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List DropMark MC68000 execution qualification failed.' }
$collectionListDragHunk = Join-Path $outDir 'mui-collection-list-drag.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListDragHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List drag-sort MC68000 execution qualification failed.' }
$collectionListAutoVisibleHunk = Join-Path $outDir 'mui-collection-list-auto-visible.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListAutoVisibleHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List AutoVisible MC68000 execution qualification failed.' }
$collectionListSortColumnHunk = Join-Path $outDir 'mui-collection-list-sort-column.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListSortColumnHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List SortColumn MC68000 execution qualification failed.' }
$collectionListviewForwardingHunk = Join-Path $outDir 'mui-collection-listview-forwarding.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListviewForwardingHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Listview forwarding MC68000 execution qualification failed.' }
$collectionListQuietHunk = Join-Path $outDir 'mui-collection-list-quiet.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListQuietHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List Quiet MC68000 execution qualification failed.' }
$collectionListFormatHunk = Join-Path $outDir 'mui-collection-list-format.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListFormatHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List FORMAT MC68000 execution qualification failed.' }
$collectionListFormatColHunk = Join-Path $outDir 'mui-collection-list-format-col.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListFormatColHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List FORMAT COL MC68000 execution qualification failed.' }
$collectionListFormatBarHunk = Join-Path $outDir 'mui-collection-list-format-bar.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListFormatBarHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List FORMAT BAR MC68000 execution qualification failed.' }
$collectionListFormatPreparseHunk = Join-Path $outDir 'mui-collection-list-format-preparse.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListFormatPreparseHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List FORMAT PREPARSE MC68000 execution qualification failed.' }
$collectionListFormatMinusOneHunk = Join-Path $outDir 'mui-collection-list-format-minus-one.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListFormatMinusOneHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List FORMAT -1 MC68000 execution qualification failed.' }
$collectionListFormatQuotedHunk = Join-Path $outDir 'mui-collection-list-format-quoted.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListFormatQuotedHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List FORMAT quoted ReadArgs MC68000 execution qualification failed.' }
$collectionListFormatEscapesHunk = Join-Path $outDir 'mui-collection-list-format-escapes.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListFormatEscapesHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List FORMAT escape ReadArgs MC68000 execution qualification failed.' }
$collectionListFormatErrorsHunk = Join-Path $outDir 'mui-collection-list-format-errors.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $collectionListFormatErrorsHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 List FORMAT ReadArgs errors MC68000 execution qualification failed.' }
$imageHunk = Join-Path $outDir 'mui-collection-image.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $imageHunk
if ($LASTEXITCODE -ne 0) { throw 'MG08 List image-handle MC68000 execution qualification failed.' }

$hookAbiHunk = Join-Path $outDir 'mui-hookabi.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $hookAbiHunk
if ($LASTEXITCODE -ne 0) { throw 'MG08 hook-ABI MC68000 execution qualification failed.' }

$dirlistHunk = Join-Path $outDir 'mui-dirlist.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $dirlistHunk
if ($LASTEXITCODE -ne 0) { throw 'MG08 Dirlist MC68000 execution qualification failed.' }
$dirlistPacketsHunk = Join-Path $outDir 'mui-dirlist-packets.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $dirlistPacketsHunk
if ($LASTEXITCODE -ne 0) { throw 'MG08 Dirlist packet seam MC68000 execution qualification failed.' }
$stringscrollHunk = Join-Path $outDir 'mui-stringscroll.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringscrollHunk
if ($LASTEXITCODE -ne 0) { throw 'MG08 Stringscroll MC68000 execution qualification failed.' }
$stringscrollUtf8MetricsHunk = Join-Path $outDir 'mui-stringscroll-utf8-metrics.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringscrollUtf8MetricsHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Stringscroll UTF-8 metrics MC68000 execution qualification failed.' }
$stringUtf8CursorHunk = Join-Path $outDir 'mui-string-utf8-cursor.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringUtf8CursorHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 String.mui UTF-8 cursor MC68000 execution qualification failed.' }
$stringUtf8InputHunk = Join-Path $outDir 'mui-string-utf8-input.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringUtf8InputHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 String.mui UTF-8 input MC68000 execution qualification failed.' }
$stringTranslatedTabHunk = Join-Path $outDir 'mui-string-translated-tab.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringTranslatedTabHunk
if ($LASTEXITCODE -ne 0) { throw 'MG716 String.mui translated TAB MC68000 execution qualification failed.' }
$stringWordNavigationHunk = Join-Path $outDir 'mui-string-word-navigation.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringWordNavigationHunk
if ($LASTEXITCODE -ne 0) { throw 'MG717 String.mui word navigation MC68000 execution qualification failed.' }
$stringMultilinePageNavigationHunk = Join-Path $outDir 'mui-string-multiline-page-navigation.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringMultilinePageNavigationHunk
if ($LASTEXITCODE -ne 0) { throw 'MG718 String.mui multiline page navigation MC68000 execution qualification failed.' }
$stringMultilineBoundaryNavigationHunk = Join-Path $outDir 'mui-string-multiline-boundary-navigation.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringMultilineBoundaryNavigationHunk
if ($LASTEXITCODE -ne 0) { throw 'MG719 String.mui multiline boundary navigation MC68000 execution qualification failed.' }
$stringUtf8FilterHunk = Join-Path $outDir 'mui-string-utf8-filter.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringUtf8FilterHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 String.mui UTF-8 filter MC68000 execution qualification failed.' }
$stringUtf8PositionHunk = Join-Path $outDir 'mui-string-utf8-position.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $stringUtf8PositionHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 String.mui UTF-8 position MC68000 execution qualification failed.' }
$listviewPointerInputHunk = Join-Path $outDir 'mui-listview-pointer-input.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewPointerInputHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Listview pointer input MC68000 execution qualification failed.' }
$listviewDragCancelHunk = Join-Path $outDir 'mui-listview-drag-cancel.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewDragCancelHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Listview drag-cancel MC68000 execution qualification failed.' }
$areaDragMessageCodecHunk = Join-Path $outDir 'mui-area-drag-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaDragMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Area drag message codec MC68000 execution qualification failed.' }
$areaDragStateHunk = Join-Path $outDir 'mui-area-drag-state.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaDragStateHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Area drag state MC68000 execution qualification failed.' }
$areaActivationMessageCodecHunk = Join-Path $outDir 'mui-area-activation-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $areaActivationMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Area activation message codec MC68000 execution qualification failed.' }
$listviewDragStateHunk = Join-Path $outDir 'mui-listview-drag-state.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listviewDragStateHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Listview drag-state MC68000 execution qualification failed.' }
$listtreeHunk = Join-Path $outDir 'mui-listtree.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeHunk
if ($LASTEXITCODE -ne 0) { throw 'MG08 Listtree MC68000 execution qualification failed.' }
$listtreeMessageCodecHunk = Join-Path $outDir 'mui-listtree-message-codec.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeMessageCodecHunk
if ($LASTEXITCODE -ne 0) { throw 'MG09 Listtree message codec MC68000 execution qualification failed.' }
$listtreeDispatcherHunk = Join-Path $outDir 'mui-listtree-dispatcher.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeDispatcherHunk
if ($LASTEXITCODE -ne 0) { throw 'MG622 Listtree dispatcher MC68000 execution qualification failed.' }
$listtreePointerDragCommitHunk = Join-Path $outDir 'mui-listtree-pointer-drag-commit.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreePointerDragCommitHunk
if ($LASTEXITCODE -ne 0) { throw 'MG640 Listtree pointer-drag commit MC68000 execution qualification failed.' }
$listtreeKeyboardSelectionHunk = Join-Path $outDir 'mui-listtree-keyboard-selection.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeKeyboardSelectionHunk
if ($LASTEXITCODE -ne 0) { throw 'MG642 Listtree keyboard-selection MC68000 execution qualification failed.' }
$listtreeDoubleClickHunk = Join-Path $outDir 'mui-listtree-double-click.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeDoubleClickHunk
if ($LASTEXITCODE -ne 0) { throw 'MG643 Listtree double-click MC68000 execution qualification failed.' }
$listtreeExchangeRelativeHunk = Join-Path $outDir 'mui-listtree-exchange-relative.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeExchangeRelativeHunk
if ($LASTEXITCODE -ne 0) { throw 'MG644 Listtree exchange-relative selectors MC68000 execution qualification failed.' }
$listtreeDoubleClickColumnsHunk = Join-Path $outDir 'mui-listtree-double-click-columns.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $listtreeDoubleClickColumnsHunk
if ($LASTEXITCODE -ne 0) { throw 'MG646 Listtree double-click column-history MC68000 execution qualification failed.' }

$nativePublicObjectRetainedDescendantsMg2825Hunk = Join-Path $outDir 'mui-native-public-object-retained-descendants-mg2825.hunk'
& dotnet run --project $executionProject --configuration $Configuration -- $nativePublicObjectRetainedDescendantsMg2825Hunk
if ($LASTEXITCODE -ne 0) { throw 'MG2825 Native public-object retained-descendant reconciliation MC68000 execution qualification failed.' }
