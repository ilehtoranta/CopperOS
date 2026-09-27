# TaskList contract

Profile: `morphos320`. Goal steps: CC01 and CC17. Recorded: 2026-09-27.

The source-bound command is
`artifacts/morphos320-c-source-extracted/c/tasklist/tasklist.c`, 21,603 bytes,
SHA-256
`66fd02c45ac1e91e770bb9b4cefb6966427f94a0b951a4fc21b695b5294ed94b`.
The extracted source identifies an AROS-derived TaskList implementation
(`$Id: tasklist.c,v 1.6 2019/08/12 20:45:10 piru $`), so source rights and exact
MorphOS package correspondence remain an explicit admission question.

The source template is:

```text
NAME,ADDRESS/N,VERBOSE/S,STACKTRACE/S,STACKLEVEL/N,NORUN/S,NOWAIT/S,NOREADY/S,INTERNAL/S,REGDUMP/S,REGCHECK/S
```

`NAME` filters the task node name and, for CLI processes, also matches the CLI
command name. `ADDRESS` selects one task address, and `NORUN`, `NOWAIT` and
`NOREADY` control the current, waiting and ready lists.
The source opens `dos.library` version 37 and `sysdebug.library` version 0,
reads MorphOS system attributes for emulation/module/task-exit boundaries,
allocates a growing `AllocVec` task buffer, snapshots the Exec task lists under
`Forbid`, and formats rows after `Permit`. It captures PPC SRR0/LR/CTR/CR/XER,
GPR and FPR blocks, VSAVE and VSCR for every non-running task through public
`NewGetTaskAttrsA` calls, independently of `REGDUMP`. `REGDUMP` prints the five
scalar values and four GPR rows; the captured source compiles its Altivec block
out. `VERBOSE` prints already-captured task fields. For the running task,
`SPReg` is the live A7 register; stopped tasks use `tc_SPReg`. `STACKTRACE` and
`REGCHECK` use stackdump symbol/provider paths; `STACKLEVEL` limits stack
frames. Ctrl-C is polled after each row and returns `RETURN_FAIL` with
`ERROR_BREAK`.

Task rows request MorphOS `TASKINFOTYPE_PID_CLI` (`0x24`). The documented
behavior is the CLI number for a Process with a CLI and the unique PID
otherwise; `TASKINFOTYPE_PID` (`0x33`) always returns the unique task PID.
MorphOS warns that a CLI number can identify a replacement Process after the
old CLI terminates, so this field is display metadata rather than a stable
target key. The MorphOS 3.20 SDK's `exec/tasks.h` defines `Task.tc_ETask` at
byte 34 and `ETask.UniqueID` at byte 24 of the extension. The same header
defines `TF_ETASK` as bit 3, which must be set before `tc_ETask` is valid. The
shared provider now gates ETask reads on this flag and falls back to the task
address when it is clear; these layout and flag values are bound to the local
official SDK archive in
`reference-captures/tasklist-pid-cli-sdk-20260927.json`. The shared CopperStart
provider reads that unique ID when a MorphOS ETask extension is present, reads
`Process.pr_TaskNum` for CLI Processes, and retains the guest task-address
fallback only for task allocations without an extension. MorphOS
`NewCreateTaskA` now allocates an 86-byte ETask in the existing owned task
block and assigns a nonzero ID from ExecBase `ex_TaskID` under `Forbid`/
`Permit`, and marks the extension valid with `TF_ETASK`; the owned-block
reaper frees it with the task. DOS-owned Process allocation now
checks the Exec library version, adds the ETask before the child stack on
version 50 and later, and assigns the same native ID. The version-40 Workbench
path keeps its existing Process and stack layout. Other standard Exec task
creation paths still lack ETask coverage. The provider's `FindTaskByPID` path accepts either its unique-ID
value or a live CLI number and searches current, ready, and waiting tasks;
the focused tests show a reused CLI number resolving the replacement Process
after the first task is removed. They do not exercise concurrent scheduler
changes or native guest PID assignment.

The current replacement is a bounded resident native stage in
`src/Commands/Native/NativeMorphOSTaskListCommand.cs`, entered through
`tests/Commands.AddBuffersNativeRoot/NativeMorphOSTaskListEntry.cs`. It keeps
the complete `ReadArgs` grammar, snapshots public `TaskReady`/`TaskWait` and
current-task state under `Forbid`/`Permit`, obtains PID, type, priority, state,
M68k/PPC stack sizes and bounds, and signal masks through public MorphOS
`NewGetTaskAttrsA` selectors (with classic task fields as documented
fallbacks), supports name/address filters and list exclusions, and formats the
source standard row. CLI Processes display their CLI command name and may be
selected by that name. It captures public PPC register attributes into
invocation-owned fixed records and implements source `REGDUMP` scalar/GPR
formatting. After parsing, it queries public `NewGetSystemAttrsA` for the six
source boundary values: emulation start/size, module start/size, and native and
M68k task-exit stub addresses. `VERBOSE` prints all nine source detail lines
for stopped tasks and reads live A7 for the current running task through the
compiler's `m68k-read-stack-pointer` intrinsic, lowered to `MOVE.L A7,D0` in
allocated and fallback code generation. Compiler execution tests cover
68000/020/040/060, and the resident fixture checks that the reported value lies
in the command's active stack rather than the saved task stack bounds. The
replacement starts with a 128 KiB invocation-owned
snapshot buffer and doubles it after an incomplete traversal, matching the
source's grow-and-retry behavior. A 100-ready-task fixture forces a 256 KiB
retry and verifies that the partial first snapshot is discarded and freed.
The entry opens
`sysdebug.library` version 0 before parsing and closes it on parser failure and
all post-parse exits; a missing library prints the current `IoErr` and fails
before parsing. The MorphOS SDK audit records `SysDebugFindSeg` as a PPC SysV
call without a packaged 68k inline entry; the resident 68k HUNK therefore does
not guess a classic vector. The extracted MorphOS 3.20 source does not use the
newer public Exec `TASKINFOTYPE_PPC_STACKHISTORY` selector. Its
`ShowPPCStackHistory` starts at saved GPR1, follows the PPC backchain, validates
frame and return-address pointers with `TypeOfMem` (or captured emulation/module
ranges in `INTERNAL` mode), and resolves return addresses with
`SysDebugFindSeg`; `ShowReg` separately inspects register/provider data for
`REGCHECK`. Do not replace this source behavior with the stack-history selector
without parity evidence. The SDK requires `Forbid()` while querying another
task, and the bounded snapshot now uses `Forbid()`/`Permit()` for task traversal
and attribute reads; supplied vectors verify protected reads and balanced
release. The resident implementation follows saved PPC backchains for stopped
tasks, bounds the walk by `STACKLEVEL` (source default 30), validates frame and
return addresses with `TypeOfMem` or captured emulation/module ranges in
`INTERNAL` mode, and uses the documented legacy `SegTracker` semaphore
function pointer when that semaphore is available. It copies resolved symbol
metadata into the task snapshot while protected, releases the semaphore and
`Forbid` before formatting, and falls back to raw addresses when no symbol is
available. `INTERNAL` labels the captured ABOX ranges and the two task-exit
stubs. The fixture covers bounded raw and symbolized traces, default
`STACKLEVEL`, and separate emulation and module range offsets in internal
frame formatting. `REGCHECK` now follows the inspected `stackdump.c`
classification order for valid/internal/symbol pointers, Exec lists and node
ranges, task stacks, Process handles, ports, semaphores, and SegTracker
symbols. Its 54 supplied vectors per CPU cover ETask interior offsets and
raw/BADDR Process fields. These fixtures do not establish actual MorphOS
guest provider or byte-for-byte output parity. The SDK audit still rules out
guessing a classic vector for PPC `SysDebugFindSeg`; evidence is in
`reference-captures/tasklist-sysdebug-sdk-20260924.json`.

`tools/Commands/qualify_morphos_tasklist_native_entry.ps1` compiles resident
68000/68020/68040 HUNKs with no managed allocation sites, runtime helpers or
exception/fatal-fault sites and runs fifty-four supplied DOS/Exec vectors per CPU.
Coverage includes the sysdebug lease and failed-open path, six public
system-attribute queries, public task-attribute snapshots protected by
`Forbid`/`Permit`, CLI process classification, command-name output and
filtering, 255-character CLI names, three-node ready and waiting lists, the
100-task buffer-growth retry, PPC register attribute calls, `REGDUMP`, stopped
task `VERBOSE`, live current-task A7 capture, filtered-current-task behavior,
bounded raw/symbolized stack traces, default
`STACKLEVEL`, and separate emulation/module `INTERNAL` offsets. The latest
receipt is `artifacts/tasklist-morphos-native-pid-cli-20260927-v4/qualification.json`;
it passes 54 supplied vectors per CPU with 69 reachable methods on 68000,
68020, and 68040. The focused provider/creation filter passes 20 tests, including
the MorphOS `ETask.UniqueID` path, the compiled production `FindTaskByPID`
vector entry, and direct execution of the installed `NewGetTaskAttrsA` jump
vector with selector `0x24`.
The refreshed DOS child/publication/lifecycle filter passes 42 cases, including
MorphOS version-50 Process ETask placement, unique-ID PID lookup, and the
unchanged Workbench version-40 Process layout. These unit fixtures do not
establish guest ID-sequence parity or concurrent task-removal behavior. Generic
`AddTask` still has no paired extension-allocation and post-switch reaper path,
so tasks created through that standard vector retain address-fallback identity.

This contract does not claim original guest output parity, complete task
population across all standard Exec task-creation paths, original-guest ID
sequence parity, source-equivalent
symbol-provider behavior on a MorphOS guest, Workbench behavior, PURE/resident
lifecycle, minimum-stack, licensing or package admission.
