# ROM-backed Shell rollback regression

Run from the CopperOS root, with published CopperStart and CopperSharp68k
checkouts beside it:

```powershell
$targets = (Resolve-Path tests/Shell.Dos.Regression/Regression.targets).Path
dotnet test ../CopperStart/tests/CopperStart.Exec.Tests/CopperStart.Exec.Tests.csproj -c Release --filter 'FullyQualifiedName~ShellRomRollbackTests|FullyQualifiedName~DosPreparedChildArgumentInputTests|FullyQualifiedName~DosChildCompletionCoreTests' "-p:CustomAfterMicrosoftCommonTargets=$targets"
dotnet test tests/Commands/CopperOS.Commands.Tests.csproj -c Release
```

The shared Shell path and runner ownership regressions use the same process
fixture with the production Shell project:

```powershell
$targets = (Resolve-Path tests/Shell.Dos.Regression/PathIntegration.targets).Path
dotnet test ../CopperStart/tests/CopperStart.Exec.Tests/CopperStart.Exec.Tests.csproj -c Release --filter 'FullyQualifiedName~PathOwnershipTests' "-p:CustomAfterMicrosoftCommonTargets=$targets"
```

These tests cover public CLI path ownership, runner publication through the
current Process's Shell-private field, nested Shell-owned script frames,
control and label cleanup, child completion under the correct frame, normal
script completion, and rollback when runner publication cannot allocate. The focused 68000 probe is documented
in `tests/Shell.Runner.NativeRoot/README.md`; neither suite by itself qualifies
the complete Shell on a real Kickstart image.

The targets file enables the ROM branch only in the host test assembly and adds
the regression to CopperStart's existing process fixture. That assembly already
links this checkout's production `DosShellPreparedChildCore.cs`. The regression
fails against the old private-loader rollback: the platform release callback is
never invoked. It also checks the command error, task retirement and absence of
publication. These are host semantic tests, without guest CPU execution.

On a writable CopperOS proof image, `Run C:Demo >SYS:T/runresult` exercises the
native public Shell worker and its child-opened redirection. After allowing the
child to finish, the file should contain the two lines printed by `C:Demo`.
The Kickstart probe in `tools/ImageBuilder/qualify_shared_shell31.py --run`
also checks this path. The generic host platform still has a separate legacy
direct-executable launcher, and these checks do not qualify every launch path.

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
