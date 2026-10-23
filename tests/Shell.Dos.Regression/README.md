# ROM-backed Shell rollback regression

Run from the CopperOS root, with published CopperStart and CopperSharp68k
checkouts beside it:

```powershell
$targets = (Resolve-Path tests/Shell.Dos.Regression/Regression.targets).Path
dotnet test ../CopperStart/tests/CopperStart.Exec.Tests/CopperStart.Exec.Tests.csproj -c Release --filter 'FullyQualifiedName~ShellRomRollbackTests|FullyQualifiedName~DosPreparedChildArgumentInputTests|FullyQualifiedName~DosChildCompletionCoreTests' "-p:CustomAfterMicrosoftCommonTargets=$targets"
dotnet test tests/Commands/CopperOS.Commands.Tests.csproj -c Release
```

The targets file enables the ROM branch only in the host test assembly and adds
the regression to CopperStart's existing process fixture. That assembly already
links this checkout's production `DosShellPreparedChildCore.cs`. The regression
fails against the old private-loader rollback: the platform release callback is
never invoked. It also checks the command error, task retirement and absence of
publication. These are host semantic tests, without guest CPU execution.

The production `CopperOS.Shell.Dos` project enables ROM DOS vector loading. The
separate `Shell.Dos.NativeRoot` project continues to qualify the standalone
provider loader. Compile the production Shell and Execute with
`../CopperStart/build/Build-Shell.ps1`; mixed SDK and caller-provided DOS bindings
require the CopperSharp68k fix in PR #7. Compilation does not establish a full
ROM/media replay.

Review of the older commits:

- `621fcdc`: superseded by `ShellScriptEngine`'s missing-command handling and
  `Missing_command_is_not_launched_and_interactive_reader_recovers` tests.
- `d82df9c`: retain the paired installed-vector load/unload path for ROM builds.
- `e9919fa`: retain the platform release capability on ROM publication rollback.
  Standalone qualifications keep their private loader.
