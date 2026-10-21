param([Parameter(Mandatory)][string]$FrozenRuntime, [Parameter(Mandatory)][string]$CopperStart,
      [Parameter(Mandatory)][string]$Output, [string[]]$Components = @(), [string]$FrozenTestSources,
      [string]$FrozenDosSources, [string]$FrozenHostSources, [string]$FrozenDesktopSources,
      [string]$FrozenExecSources, [string]$FrozenSystemSources, [string]$FrozenShellSources, [string]$FrozenShellDosSources,
      [switch]$PreserveFrozenTestSources)
$ErrorActionPreference = 'Stop'
$frozen = (Resolve-Path -LiteralPath $FrozenRuntime).Path
$repo = (Resolve-Path -LiteralPath $CopperStart).Path
$out = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $out) { throw 'Use a fresh output directory.' }
New-Item -ItemType Directory -Path $out | Out-Null
$runtime = Join-Path $out 'runtime'
Copy-Item -LiteralPath $frozen -Destination $runtime -Recurse
$records = @()
foreach ($component in @(
    @{ name='CopperStart.Exec'; path='src/CopperStart.Exec' },
    @{ name='CopperStart.Dos'; path='src/CopperStart.Dos' },
    @{ name='CopperOS.Shell.Dos'; path='../CopperOS/src/System/Shell' },
    @{ name='CopperStart-System'; path='rom/CopperStart.SystemRom' },
    @{ name='CopperStart.Shell'; path='native/CopperStart.Shell' },
    @{ name='CopperStart.DesktopProviders'; path='native/CopperStart.DesktopProviders'; linked='native/CopperStart.Shell/NativeUtilityLibrary.cs' },
    @{ name='CopperStart.Graphics'; path='native/CopperStart.Graphics'; recursive=$true; portable=$true },
    @{ name='CopperStart.Floppy.Native.Tests'; path='tests/CopperStart.Floppy.Native.Tests'; linked='tests/CopperStart.Exec.Tests/NativeParityBus.cs'; tests=$true }
    @{ name='CopperStart.Exec.Tests'; path='tests/CopperStart.Exec.Tests'; tests=$true; selected=@('TestMemoryPlatform.cs','ExecIoVectorFrameCoreTests.cs','DosErrorCoreTests.cs','DosManifestTests.cs','DosSegmentLoaderCoreTests.cs','DosNotificationCoreTests.cs','DosDeviceProcCoreTests.cs','DosChildProcessLaunchCoreTests.cs','DosAssignHandlerIdentityTests.cs') }
)) {
    if ($Components.Count -gt 0 -and $component.name -notin $Components) { continue }
    if ($Components.Count -eq 0 -and $component.name -in @('CopperStart.Exec.Tests','CopperStart.Shell','CopperOS.Shell.Dos')) { continue }
    $build = Join-Path $out $component.name
    $source = Join-Path $build 'source'
    New-Item -ItemType Directory -Path $source -Force | Out-Null
    $sourceDirectory = Join-Path $repo $component.path
    if ($component.name -eq 'CopperStart.Exec' -and $FrozenExecSources) { $sourceDirectory = (Resolve-Path -LiteralPath $FrozenExecSources).Path }
    if ($component.name -eq 'CopperStart-System' -and $FrozenSystemSources) { $sourceDirectory = (Resolve-Path -LiteralPath $FrozenSystemSources).Path }
    if ($component.name -eq 'CopperStart.Shell' -and $FrozenShellSources) { $sourceDirectory = (Resolve-Path -LiteralPath $FrozenShellSources).Path }
    if ($component.name -eq 'CopperOS.Shell.Dos' -and $FrozenShellDosSources) { $sourceDirectory = (Resolve-Path -LiteralPath $FrozenShellDosSources).Path }
    if ($component.name -eq 'CopperStart.Dos' -and $FrozenDosSources) { $sourceDirectory = (Resolve-Path -LiteralPath $FrozenDosSources).Path }
    if ($component.name -eq 'CopperStart.DesktopProviders' -and $FrozenDesktopSources) { $sourceDirectory = (Resolve-Path -LiteralPath $FrozenDesktopSources).Path }
    if ($component.name -eq 'CopperStart.Exec.Tests' -and $FrozenHostSources) { $sourceDirectory = (Resolve-Path -LiteralPath $FrozenHostSources).Path }
    if ($component.name -eq 'CopperStart.Floppy.Native.Tests' -and $FrozenTestSources) { $sourceDirectory = (Resolve-Path -LiteralPath $FrozenTestSources).Path }
    $files = @(Get-ChildItem -LiteralPath $sourceDirectory -File -Filter '*.cs' -Recurse:$([bool]$component.recursive) |
        Where-Object FullName -NotMatch '[\\/](obj|bin)[\\/]')
    if ($component.name -eq 'CopperStart.Exec.Tests' -and $FrozenHostSources) {
        foreach ($name in @('DosAssignHandlerIdentityTests.cs','ExecIoVectorFrameCoreTests.cs')) {
            $files = @($files | Where-Object Name -ne $name) +
                (Get-Item -LiteralPath (Join-Path (Join-Path $repo $component.path) $name))
        }
    }
    if ($component.name -eq 'CopperStart.Floppy.Native.Tests' -and $FrozenTestSources -and !$PreserveFrozenTestSources) {
        foreach ($name in @('NativeDesktopIntegrationTests.cs','NativeDosLocaleBootTests.cs','NativeLegacyPreferencesTests.cs','NativeFileNotificationTests.cs','NativeFullShellBootTests.cs','NativeWorkbenchStartupTests.cs','NativeResidentLookupTests.cs','NativeTimerDeviceTests.cs','NativeWorkbenchConfigTests.cs','DosDiskImage.cs')) {
            $files = @($files | Where-Object Name -ne $name) + (Get-Item -LiteralPath (Join-Path (Join-Path $repo $component.path) $name))
        }
    }
    if ($component.selected) { $files = @($files | Where-Object Name -In $component.selected) }
    if ($component.linked -and [IO.Path]::GetFileName($component.linked) -notin $files.Name) { $files += Get-Item -LiteralPath (Join-Path $repo $component.linked) }
    if ($component.portable) { $files += Get-ChildItem -LiteralPath (Join-Path $repo 'host/CopperStart.CopperMod/CopperStart/Graphics/Portable') -File -Filter '*.cs' }
    foreach ($file in $files) {
        Copy-Item -LiteralPath $file.FullName -Destination $source
        $records += @{ component=$component.name; source=$file.FullName; sha256=(Get-FileHash -LiteralPath (Join-Path $source $file.Name)).Hash }
    }
    $references = foreach ($file in Get-ChildItem -LiteralPath $runtime -Filter 'Copper*.dll' |
        Where-Object { $_.BaseName -ne $component.name -and $_.Name -notlike '*Tests.dll' -and
            !($component.name -eq 'CopperStart.DesktopProviders' -and $_.BaseName -eq 'CopperStart.Shell') -and
            !($component.name -eq 'CopperStart.Shell' -and $_.BaseName -eq 'CopperStart.DesktopProviders') }) {
        '<Reference Include="'+$file.BaseName+'"><HintPath>'+[Security.SecurityElement]::Escape($file.FullName)+'</HintPath></Reference>'
    }
    $test = if ($component.tests) { '<IsTestProject>true</IsTestProject>' } elseif ($component.portable) { '<OutputType>Exe</OutputType>' } else { '' }
    $packages = if ($component.tests) { '<Using Include="Xunit"/><PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1"/><PackageReference Include="xunit" Version="2.9.3"/><PackageReference Include="xunit.runner.visualstudio" Version="3.1.4"/>' } elseif ($component.portable) { '<InternalsVisibleTo Include="CopperStart.Floppy.Native.Tests"/>' } else { '' }
    $project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>'+$component.name+'</AssemblyName><AllowUnsafeBlocks>true</AllowUnsafeBlocks><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><NuGetAudit>false</NuGetAudit>'+$test+'</PropertyGroup><ItemGroup>'+($references -join '')+$packages+'</ItemGroup></Project>'
    $projectPath = Join-Path $build 'Producer.csproj'
    [IO.File]::WriteAllText($projectPath, $project)
    & dotnet build $projectPath -c Release --ignore-failed-sources -m:1 -p:UseSharedCompilation=false -p:GenerateDependencyFile=false -nodeReuse:false
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $($component.name)" }
    Copy-Item -LiteralPath (Join-Path $build "bin/Release/net10.0/$($component.name).dll") -Destination $runtime
    Copy-Item -LiteralPath (Join-Path $build "bin/Release/net10.0/$($component.name).pdb") -Destination $runtime
    if ($component.tests) {
        Copy-Item -LiteralPath (Join-Path $build "bin/Release/net10.0/$($component.name).runtimeconfig.json") -Destination $runtime
    }
}
$records | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $out 'sources.json')
Get-ChildItem -LiteralPath $runtime -Filter '*.dll' | ForEach-Object {
    @{file=$_.Name; sha256=(Get-FileHash -LiteralPath $_.FullName).Hash}
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'assemblies.json')
