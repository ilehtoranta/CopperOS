param(
    [Parameter(Mandatory)][string]$DotnetPath,
    [Parameter(Mandatory)][string]$CompilerCliPath,
    [Parameter(Mandatory)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$run = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $run) { throw 'Use a new output directory to preserve earlier results.' }
New-Item -ItemType Directory -Path $run | Out-Null
$dotnet = [IO.Path]::GetFullPath($DotnetPath)
$cli = [IO.Path]::GetFullPath($CompilerCliPath)
foreach ($name in @('Contracts', 'Adapter', 'Root', 'input', 'dependency')) {
    New-Item -ItemType Directory -Path (Join-Path $run $name) | Out-Null
}
$properties = '<PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>'
[IO.File]::WriteAllText((Join-Path $run 'Contracts/Contracts.csproj'), "<Project Sdk=`"Microsoft.NET.Sdk`">$properties</Project>")
[IO.File]::WriteAllText((Join-Path $run 'Adapter/Adapter.csproj'), "<Project Sdk=`"Microsoft.NET.Sdk`">$properties<ItemGroup><ProjectReference Include=`"../Contracts/Contracts.csproj`" /></ItemGroup></Project>")
[IO.File]::WriteAllText((Join-Path $run 'Root/Root.csproj'), "<Project Sdk=`"Microsoft.NET.Sdk`">$properties<ItemGroup><ProjectReference Include=`"../Adapter/Adapter.csproj`" /></ItemGroup></Project>")
[IO.File]::WriteAllText((Join-Path $run 'Contracts/Code.cs'), 'namespace Probe; public interface IValue { int Get(); }')
[IO.File]::WriteAllText((Join-Path $run 'Adapter/Code.cs'), 'namespace Probe; public struct Value : IValue { private int value; public Value(int n) { value = n; } public int Get() => value; }')
[IO.File]::WriteAllText((Join-Path $run 'Root/Code.cs'), 'namespace Probe; public static class Entry { public static int Main() { var value = new Value(42); return value.Get(); } }')
& $dotnet build (Join-Path $run 'Root/Root.csproj') -c Release *> (Join-Path $run 'managed-build.log')
if ($LASTEXITCODE -ne 0) { throw 'Managed fixture build failed.' }
foreach ($name in @('Root', 'Adapter')) {
    Copy-Item -LiteralPath (Join-Path $run "Root/bin/Release/net10.0/$name.dll") -Destination (Join-Path $run "input/$name.dll")
}
Copy-Item -LiteralPath (Join-Path $run 'Root/bin/Release/net10.0/Contracts.dll') -Destination (Join-Path $run 'dependency/Contracts.dll')
$common = @($cli, (Join-Path $run 'input/Root.dll'), '--entry', 'Probe.Entry::Main', '--platform', 'amiga', '--cpu', '68000', '--exceptions', 'yolo', '--runtime', 'freestanding', '--memory', 'none', '--peephole', 'disabled', '--format', 'hunk', '--managed-assembly', (Join-Path $run 'input/Adapter.dll'))
& $dotnet @common --output (Join-Path $run 'missing.hunk') *> (Join-Path $run 'missing.log')
$missingExit = $LASTEXITCODE
if ($missingExit -eq 0 -or -not ((Get-Content (Join-Path $run 'missing.log') -Raw) -match 'C68K0009.*Probe\.Value::\.ctor')) {
    throw 'The undeclared dependency must remain an explicit compilation failure.'
}
& $dotnet @common --managed-assembly (Join-Path $run 'dependency/Contracts.dll') --output (Join-Path $run 'declared.hunk') *> (Join-Path $run 'declared.log')
if ($LASTEXITCODE -ne 0) { throw 'Explicit transitive dependency was not resolved; see declared.log.' }
if (-not (Test-Path -LiteralPath (Join-Path $run 'declared.hunk'))) { throw 'Native artifact missing.' }
[ordered]@{
    schemaVersion = 1
    status = 'passed'
    scope = 'Fresh CLI processes: referenced struct constructor with interface in a separately supplied assembly; compilation only'
    undeclaredDependencyRejected = $true
    declaredDependencyCompiled = $true
    driverSha256 = (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash
    compilerCliSha256 = (Get-FileHash -LiteralPath $cli -Algorithm SHA256).Hash
    compilerSha256 = (Get-FileHash -LiteralPath (Join-Path (Split-Path $cli) 'CopperSharp.Compiler.dll') -Algorithm SHA256).Hash
    hunkSha256 = (Get-FileHash -LiteralPath (Join-Path $run 'declared.hunk') -Algorithm SHA256).Hash
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'result.json') -Encoding utf8
Get-Content -LiteralPath (Join-Path $run 'result.json')
