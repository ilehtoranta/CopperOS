# Public DOS command session driver

This is a compiled guest integration driver for
`../CopperStartBootIntegration`. It is not a Shell implementation or a shipped
`C:` command. It connects a production application session to native DOS process
creation and command execution without constructing private Process records or
replacing Exec, DOS, or filesystem packet operations.

The boot owner first installs the native DOS HUNK through its exported installer
and production Exec, then publishes the existing configured-volume ports through
public DOS list calls. The driver's parent entry opens that installed library
and calls `CreateNewProc` with `NP_Cli`, `NP_Entry`, `NP_UserData` and owned stack
settings. The resulting child obtains its real Task, CLI and streams, loads the
command with public `LoadSeg`, runs it with `RunCommand`, and unloads it. Optional
diagnostic output, target deletion and alias readback use public DOS calls too.

## Build and entry points

Build `CopperStartBootSessionLauncher.csproj` in Release with a fresh
`--artifacts-path` directory. Then invoke `build.py` with:

```text
--dotnet <dotnet executable>
--compiler <identified CopperSharp.Compiler.Cli.dll>
--managed-build <the managed build's artifacts directory>
--output <new, nonexistent native output directory>
```

The script resolves Support from the actual restore assets, records the compiler
and managed input identities, and retains the compiler command, log, HUNK, map,
static compatibility report and `build.json`. It never overwrites an earlier
output directory. `built-not-executed` describes compilation only.

The production HUNK loader must retain symbols. Invoke the **export alias**
`copperos.boot-session.start`, with A0 pointing to the configuration/result
record. The child entry is the export alias `copperos.boot-session.child`.
The exit callback is `copperos.boot-session.exit`, published through public
`NP_ExitCode` with the result record supplied as `NP_ExitData`. Its D0/D1 entry
records the invocation count at byte 152 and received return code at byte 156,
then returns that code unchanged. These fields are no longer reserved.
Fully qualified method-body symbols bypass the resident ABI trampoline and must
not be used as entry points. `ImageEntry` is only the compiler's image root.

The record is 160 bytes, with magic `0x4E445331` and capacity in its first two
longwords. `SessionLauncher.cs` documents every input and output offset. The
host must own the record, strings, read buffer and launcher image throughout
the child's lifetime. The driver requires caller-selected child and command
stack sizes; those values do not establish a minimum stack qualification.

## Completion and scope

The parent allocates an Exec signal, creates the child and calls public `Wait`
until the child finishes its body. The child signals the real parent Task after
cleanup; the parent then frees its signal. The record stores the parent Task at
byte 144 and signal mask at byte 148. A busy parent return loop cannot replace
this handoff when the equally prioritized child needs the parent to yield.

The callback executes after the child body returns and before native DOS releases
the Process state. The host requires exactly one invocation and the matching
child return code after actual retirement; the earlier `done` signal alone does
not prove callback execution. Callback counts do not establish nonzero-code,
replacement-result, cancellation or resident-concurrency behavior by themselves.

The `done` field indicates that the child body finished cleanup. It is written
before the child returns, so it does **not** authorize freeing its image or
buffers. The integration must also observe return to the original task and
confirm the child is no longer present through public Exec. A timed-out or
faulted guest call retains borrowed inputs until the whole session is reset;
do not resume cleanup by invoking another DOS call on an unresolved stack.

The companion integration requires explicit paths in these environment variables:

```text
COPPER_BOOT_INTEGRATION_OUTPUT  # fresh, owned result directory
COPPER_BOOT_INTEGRATION_STAGE   # install or command
COPPER_BOOT_NATIVE_DOS         # DOS HUNK; matching .map beside it
COPPER_BOOT_MAKELINK           # identified command HUNK
COPPER_BOOT_LAUNCHER           # required for command stage
COPPER_BOOT_LAUNCHER_MAP        # matching launcher map
```

The test currently pins the admitted DOS, command and launcher input hashes;
changing an input requires a deliberate source update and a fresh run. Its
`result.json` records failures as well as successful checkpoints. It references
the complete production emulator, although the test assembly itself is isolated
from unrelated tests that currently fail to compile.

Even a passing command-stage run covers a bounded application-session bootstrap
and one command path. It does not establish disk startup, the existing Shell
loop, all command options, minimum stack, PURE/resident safety, release admission
or a bootable distribution image. Those remain separate steps in the goal.
