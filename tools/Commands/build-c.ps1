<#
.SYNOPSIS
    Build every CopperOS C: command into out\C (one MC68000 HUNK per command).

.DESCRIPTION
    Builds the CopperSharp compiler CLI from the local CopperSharp68k tree, then
    runs `dotnet publish` on each src\Commands\<Command> project that declares a
    CopperOSCommandName. Each publish writes the HUNK, .map and .framework.json
    to that project's publish directory and copies the executable to
    <OutputDirectory>\<Command>.

.EXAMPLE
    pwsh tools\Commands\build-c.ps1
    pwsh tools\Commands\build-c.ps1 -Command MakeDir,Eval -SkipCompiler
#>
param(
    [string[]]$Command,
    [string]$OutputDirectory,
    [string]$CopperSharpRoot,
    [string]$CompilerAssembly,
    [ValidateSet('on', 'off')][string]$CodeSizeOptimizations,
    [switch]$SkipCompiler
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if (-not $CopperSharpRoot) { $CopperSharpRoot = Join-Path $repo '..\CopperSharp68k' }
$CopperSharpRoot = (Resolve-Path $CopperSharpRoot).Path
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repo 'out\C' }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)

if (-not $SkipCompiler) {
    $cli = Join-Path $CopperSharpRoot 'Compiler.Cli\CopperSharp.Compiler.Cli.csproj'
    & dotnet build $cli -c Release -nologo -v q -p:SkipCopperScreenHeadlessProjectReference=true
    if ($LASTEXITCODE -ne 0) { throw "CopperSharp compiler build failed ($LASTEXITCODE)." }
}

$projects = Get-ChildItem (Join-Path $repo 'src\Commands') -Directory |
    ForEach-Object { Get-ChildItem $_.FullName -Filter *.csproj -File } |
    Where-Object { (Get-Content -LiteralPath $_.FullName -Raw) -match '<CopperOSCommandName>([^<]+)</CopperOSCommandName>' } |
    ForEach-Object { [pscustomobject]@{ Name = $Matches[1]; Path = $_.FullName } }
$fullBuild = -not $Command
if ($Command) {
    # `powershell -File` passes "A,B" as one string.
    $Command = @($Command | ForEach-Object { $_ -split ',' } | ForEach-Object Trim | Where-Object { $_ })
    $missing = @($Command | Where-Object { $_ -notin $projects.Name })
    if ($missing.Count -gt 0) { throw "No command project for: $($missing -join ', ')" }
    $projects = @($projects | Where-Object Name -In $Command)
}

$directory = $OutputDirectory.TrimEnd('\') + '\'
$failed = [Collections.Generic.List[string]]::new()
$built = foreach ($project in $projects) {
    $properties = @("-p:CopperSharp68kRoot=$CopperSharpRoot", "-p:CopperOSCDirectory=$directory")
    if ($CompilerAssembly) { $properties += "-p:CopperOSCompilerAssembly=$([IO.Path]::GetFullPath($CompilerAssembly))" }
    if ($CodeSizeOptimizations) { $properties += "-p:CopperOSCodeSizeOptimizations=$CodeSizeOptimizations" }
    & dotnet publish $project.Path -c Release -nologo -v q @properties
    if ($LASTEXITCODE -ne 0) { $failed.Add($project.Name); continue }
    $file = Get-Item -LiteralPath (Join-Path $OutputDirectory $project.Name)
    [pscustomobject]@{ Command = $project.Name; Bytes = $file.Length
        Sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
}
$built | Format-Table -AutoSize
Write-Output "C: directory: $OutputDirectory"
if ($failed.Count -gt 0) { throw "Failed: $($failed -join ', ')" }

# A successful full build owns the whole C: output and prunes removed/renamed
# commands. Filtered builds intentionally preserve all unselected binaries.
if ($fullBuild) {
    $commandNames = @($projects | ForEach-Object Name)
    $stale = @(Get-ChildItem -LiteralPath $OutputDirectory -Force -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notin $commandNames })
    if ($stale.Count -gt 0) {
        $stale | Remove-Item -Force
        Write-Output "Removed stale C: files: $($stale.Name -join ', ')"
    }
}
