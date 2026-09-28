# C: command implementation and qualification

The stable goal is `Goals/WORKBENCH31_MORPHOS_C_COMMANDS_GOAL.md`. Execution
evidence and the per-command ledger live in
`docs/Commands/Workbench31MorphOS320/`. The existing Shell owns its 31 internal
commands; this directory does not generate duplicate internal executables.

## Inventory

Use the read-only media workflow in [Inventory/README.md](Inventory/README.md).
It can verify the checked-in facts without private images; `verify-media`
also verifies and re-extracts the original locally held references.

The bounded Rename error-policy checkpoint can be rechecked without rebuilding
or executing a command:

```powershell
python tools/Commands/verify_rename_error_policy.py `
    --output artifacts/rename-error-policy-20260917/qualification.json
```

It binds the existing Workbench and MorphOS three-CPU receipts for E02, E04 and
R03. A pass is development evidence only; it does not admit a command for
shipping or establish original-MorphOS parity.

The bounded Workbench Rename parser-edge comparison is a licensed-ROM test in
the sibling `CopperMod.Amiga.Tests` project. Configure the private 37.2
reference, source-generated 68000 HUNK and its map before running the test:

```powershell
$env:COPPEROS_RENAME_ORIGINAL_REFERENCE = 'D:/private/Workbench31/Rename-37.2.hunk'
$env:COPPEROS_RENAME_GENERATED_HUNK = 'D:/build/CopperOS/rename-68000.hunk'
$env:COPPEROS_RENAME_GENERATED_MAP = 'D:/build/CopperOS/rename-68000.hunk.map'
dotnet test D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/CopperMod.Amiga.Tests.csproj `
    --filter 'Category=LicensedDosRenameCommandLaunch' --verbosity minimal
```

The fixture rechecks the reference and candidate hashes, keeps the original
binary private, and runs both images through original DOS `RunCommand` with
NIL: streams. It is a bounded A01/A03 parser receipt; it does not qualify the
full Rename grammar, MorphOS correspondence, resident/PURE metadata or
shipping.

## Native component qualification

From the CopperOS root:

```powershell
pwsh -File tools/Commands/qualify_native.ps1
pwsh -File tools/Commands/qualify_native.ps1 -Component EvalNumeric
pwsh -File tools/Commands/qualify_native.ps1 -Component Workbench31MakeDir `
    -OriginalMakeDirHunk D:/TestData/CopperOSCommands/Workbench31/MakeDir-37.2.hunk
pwsh -File tests/Commands.NativeExecution/QualificationScriptRegressionTests.ps1
dotnet test tests/Commands/CopperOS.Commands.Tests.csproj --verbosity minimal
```

The native script uses the sibling CopperSharp68k compiler and SDK for both the
native root and its execution harness (override `-CopperSharpRoot` when needed),
plus .NET 10 and the pinned Copper68k 1.4.0 instruction core. Run the modes
sequentially: they rebuild shared compiler/SDK outputs.
Avoid concurrent source edits or builds in their declared input projects;
the runner rejects input changes instead of accepting a mixed build.

| Mode | Native suites per CPU | Total invocations | Private project/output root |
| --- | --- | ---: | --- |
| `Foundation` (default) | Startup: 31; argument boundary: 25; I/O: 32 | 264 | `tests/Commands.NativeRoot` |
| `EvalNumeric` | Numeric formatter: 180 | 540 | `tests/Commands.EvalNativeRoot` |
| `EvalEntry` | Native Eval entry: 20 | 60 | `tools/Commands/qualify_eval_native_entry.ps1` |
| `EvalWorkbenchEntry` | Symbol-free Workbench Eval captured arithmetic/number/operator/LFORMAT subset: 35 | 105 | `tools/Commands/qualify_eval_wb31_native_entry.ps1` |
| `PathPartEntry` | Native PathPart entry: 15 | 45 | `tools/Commands/qualify_pathpart_native_entry.ps1` |
| `WhichEntry` | Native Workbench Which entry: 22 | 66 | `tools/Commands/qualify_which_native_entry.ps1` |
| `MorphOS320WhichEntry` | Native MorphOS Which extended syntax candidate: 17 | 51 | `tools/Commands/qualify_morphos_which_native_entry.ps1` |
| `QuoteForward` | Native Quote forward probe: 8 | 24 | `tools/Commands/qualify_quote_forward_native_root.ps1` |
| `QuoteEntry` | Native bounded MorphOS Quote STR entry: 12 | 36 | `tools/Commands/qualify_quote_native_entry.ps1` |
| `Workbench31SetDateEntry` | Native Workbench SetDate entry: 12 | 36 | `tools/Commands/qualify_setdate_native_entry.ps1` |
| `MorphOS320SetDateEntry` | Native MorphOS SetDate entry: 12 | 36 | `tools/Commands/qualify_morphos_setdate_native_entry.ps1` |
| `Workbench31DateEntry` | Native Workbench Date entry: 9 | 27 | `tools/Commands/qualify_date_native_entry.ps1` |
| `MorphOS320DateEntry` | Native MorphOS Date entry: 13 | 39 | `tools/Commands/qualify_morphos_date_native_entry.ps1` |
| `Workbench31WaitEntry` | Native Workbench Wait entry: 17 | 51 | `tools/Commands/qualify_wait_wb31_native.ps1` |
| `MorphOS320WaitEntry` | Native MorphOS Wait entry: 15 | 45 | `tools/Commands/qualify_morphos_wait_native_entry.ps1` |
| `MorphOS320WaitForPortEntry` | Native MorphOS WaitForPort entry: 10 | 30 | `tools/Commands/qualify_morphos_waitforport_native_entry.ps1` |
| `MorphOS320WaitForLibEntry` | Native MorphOS WaitForLib entry: 10 | 30 | `tools/Commands/qualify_morphos_waitforlib_native_entry.ps1` |
| `MorphOS320WaitForNotificationEntry` | Native MorphOS WaitForNotification entry: 12 | 36 | `tools/Commands/qualify_morphos_waitfornotification_native_entry.ps1` |
| `MorphOS320BeepEntry` | Native MorphOS Beep entry: 5 | 15 | `tools/Commands/qualify_morphos_beep_native_entry.ps1` |
| `MorphOS320RequestChoiceEntry` | Native MorphOS RequestChoice Intuition/timer entry: 16 | 48 | `tools/Commands/qualify_morphos_requestchoice_native_runtime.ps1` |
| `Workbench31RequestChoiceEntry` | Native Workbench 3.1 RequestChoice DOS/Intuition syntax candidate: 11 | 33 | `tools/Commands/qualify_workbench31_requestchoice_native.ps1` |
| `Workbench31RequestFileEntry` | Native Workbench 3.1 RequestFile DOS/ASL syntax candidate: 12 | 36 | `tools/Commands/qualify_workbench31_requestfile_native.ps1` |
| `MorphOS320RequestFileEntry` | Native MorphOS RequestFile DOS/ASL entry: 12 | 36 | `tools/Commands/qualify_morphos_requestfile_native_entry.ps1` |
| `MorphOS320LockEntry` | Native MorphOS Lock entry: 11 | 33 | `tools/Commands/qualify_morphos_lock_native_entry.ps1` |
| `Workbench31LockEntry` | Native Workbench 3.1 Lock syntax candidate: 11 | 33 | `tools/Commands/qualify_workbench31_lock_native_entry.ps1` |
| `MorphOS320DiskChangeEntry` | Native MorphOS DiskChange entry: 10 | 30 | `tools/Commands/qualify_morphos_diskchange_native_entry.ps1` |
| `Workbench31DiskChangeEntry` | Native Workbench 3.1 DiskChange syntax candidate: 11 | 33 | `tools/Commands/qualify_workbench31_diskchange_native_entry.ps1` |
| `MorphOS320RebootEntry` | Native MorphOS Reboot entry: 10 | 30 | `tools/Commands/qualify_morphos_reboot_native_entry.ps1` |
| `Workbench31RebootEntry` | Native Workbench 3.1 Reboot syntax candidate: 10 | 30 | `tools/Commands/qualify_workbench31_reboot_native_entry.ps1` |
| `MorphOS320BreakEntry` | Native MorphOS Break entry: 34 | 102 | `tools/Commands/qualify_break_native.ps1` |
| `Workbench31BreakEntry` | Native Workbench 3.1 Break syntax candidate: 24 | 72 | `tools/Commands/qualify_break_wb31_native.ps1` |
| `Workbench31ChangeTaskPriEntry` | Native Workbench 3.1 ChangeTaskPri syntax candidate: 13 | 39 | `tools/Commands/qualify_changetaskpri_wb31_native.ps1` |
| `MorphOS320ChangeTaskPriEntry` | Native MorphOS ChangeTaskPri entry with `pr_CES` error-stream routing and Exec 50.45 PID fallback floor: 14 | 42 | `tools/Commands/qualify_changetaskpri_native.ps1` |
| `MorphOS320ResListEntry` | Native MorphOS ResList entry: 11 | 33 | `tools/Commands/qualify_morphos_reslist_native_entry.ps1` |
| `MorphOS320LibListEntry` | Native MorphOS LibList entry: 11 | 33 | `tools/Commands/qualify_morphos_liblist_native_entry.ps1` |
| `MorphOS320DevListEntry` | Native MorphOS DevList entry: 11 | 33 | `tools/Commands/qualify_morphos_devlist_native_entry.ps1` |
| `MorphOS320DosListEntry` | Native MorphOS DOSList entry: 34 | 102 | `tools/Commands/qualify_morphos_doslist_native_entry.ps1` |
| `MorphOS320ListEntry` | Native bounded MorphOS List entry: 31 | 93 | `tools/Commands/qualify_morphos_list_native_entry.ps1` |
| `MorphOS320DirEntry` | Native bounded MorphOS Dir source-grammar entry: 15 | 45 | `tools/Commands/qualify_morphos_dir_native_entry.ps1` |
| `Workbench31DirEntry` | Native bounded Workbench 3.1 Dir syntax candidate: 16 | 48 | `tools/Commands/qualify_morphos_dir_native_entry.ps1 -Profile Workbench31` |
| `Workbench31ListEntry` | Native bounded Workbench 3.1 List entry: 32 | 96 | `tools/Commands/qualify_morphos_list_native_entry.ps1 -Profile Workbench31` |
| `Workbench31SearchEntry` | Native bounded Workbench 3.1 Search syntax candidate: 19 | 57 | `tools/Commands/qualify_workbench31_search_native_entry.ps1` |
| `MorphOS320SearchEntry` | Native bounded MorphOS Search entry: 46 | 138 | `tools/Commands/qualify_morphos_search_native_entry.ps1` |
| `MorphOS320TypeEntry` | Native bounded MorphOS Type entry: 16 | 48 | `tools/Commands/qualify_type_native_entry.ps1` |
| `Workbench31TypeEntry` | Native Workbench 3.1 Type syntax candidate: 17 | 51 | `tools/Commands/qualify_workbench31_type_native_entry.ps1` |
| `MorphOS320PortListEntry` | Native MorphOS PortList entry: 11 | 33 | `tools/Commands/qualify_morphos_portlist_native_entry.ps1` |
| `MorphOS320ModListEntry` | Native MorphOS ModList entry: 13 | 39 | `tools/Commands/qualify_morphos_modlist_native_entry.ps1` |
| `MorphOS320InfoEntry` | Native MorphOS Info entry: 66 | 198 | `tools/Commands/qualify_morphos_info_native_entry.ps1` |
| `Workbench31InfoEntry` | Native Workbench 3.1 Info DEVICE entry: 14 | 42 | `tools/Commands/qualify_workbench31_info_native_entry.ps1` |
| `MorphOS320DiskFreeEntry` | Native MorphOS DiskFree entry: fifteen supplied vectors per CPU plus static closure | 45 | `tools/Commands/qualify_morphos_diskfree_native_entry_runtime.ps1` and `tools/Commands/qualify_morphos_diskfree_native_entry.ps1` |
| `MorphOS320TaskListEntry` | Native MorphOS TaskList entry: 54 | 162 | `tools/Commands/qualify_morphos_tasklist_native_entry.ps1` |
| `MorphOS320VersionEntry` | Native MorphOS Version entry: 20 | 60 | `tools/Commands/qualify_morphos_version_native_entry.ps1` |
| `Workbench31VersionEntry` | Native Workbench 3.1 Version syntax candidate: 15 | 45 | `tools/Commands/qualify_workbench31_version_native_entry.ps1` |
| `MorphOS320SetClockEntry` | Native MorphOS SetClock entry: 17 | 51 | `tools/Commands/qualify_morphos_setclock_native_entry.ps1` |
| `Workbench31SetClockEntry` | Native Workbench 3.1 SetClock classic-vector syntax candidate: 16 | 48 | `tools/Commands/qualify_workbench31_setclock_native_entry.ps1` |
| `MorphOS320StatusEntry` | Native MorphOS Status entry: 15 | 45 | `tools/Commands/qualify_morphos_status_native_entry.ps1` |
| `Workbench31StatusEntry` | Native Workbench 3.1 Status legacy-list candidate: 15 | 45 | `tools/Commands/qualify_workbench31_status_native_entry.ps1` |
| `Workbench31DeleteEntry` | Native Workbench 3.1 Delete four-slot syntax candidate: 8 | 24 | `tools/Commands/qualify_workbench31_delete_native_entry.ps1` |
| `Workbench31JoinEntry` | Native Workbench 3.1 Join syntax candidate: resident HUNK and supplied-DOS runtime | 18 | `tools/Commands/qualify_workbench31_join_native_runtime.ps1` |
| `NativeMorphOSJoinEntry` | Native MorphOS 3.20 Join resident HUNK and supplied-DOS runtime | 18 | `tools/Commands/qualify_morphos_join_native_runtime.ps1` |
| `MorphOS320AvailEntry` | Native MorphOS Avail source-bound entry: 11 | 33 | `tools/Commands/qualify_morphos_avail_native_entry.ps1` |
| `Workbench31AvailEntry` | Native Workbench 3.1 Avail entry: 11 | 33 | `tools/Commands/qualify_avail_wb31_native.ps1` |
| `MorphOS320TouchEntry` | Native MorphOS Touch entry: 10 | 30 | `tools/Commands/qualify_morphos_touch_native_entry.ps1` |
| `MorphOS320ProtectEntry` | Native MorphOS Protect entry: 14 | 42 | `tools/Commands/qualify_morphos_protect_native.ps1` |
| `Workbench31ProtectEntry` | Native Workbench 3.1 Protect syntax candidate: 14 | 42 | `tools/Commands/qualify_workbench31_protect_native_entry.ps1` |
| `MorphOS320FileNoteEntry` | Native MorphOS FileNote entry: 10 | 30 | `tools/Commands/qualify_filenote_native.ps1` |
| `Workbench31FileNoteEntry` | Native Workbench 3.1 Filenote syntax candidate: 10 | 30 | `tools/Commands/qualify_workbench31_filenote_native.ps1` |
| `MorphOS320RelabelEntry` | Native MorphOS Relabel entry: 8 | 24 | `tools/Commands/qualify_relabel_native.ps1` |
| `Workbench31RelabelEntry` | Workbench Relabel 37.2 original/replacement comparison: 38 + 3 generated storage cases; separate 68000 original-DOS boot comparisons | 114 original, 123 generated native; 20 distinct paired single-caller scenarios and paired two-task workloads for overlap, active-removal refusal and active-replacement refusal; paired idle replacement with two image generations and missing/invalid-source retention; normal background caller retirement, recorded Process/stack memory release and later resident reuse; two writable FFS volume renames with independent persisted-sector readback and fresh read-only guest remount; four FFS name-boundary/recovery pairs | `tools/Commands/qualify_workbench31_relabel_native_entry.ps1`; `prepare_relabel_boot.py`, `verify_relabel_boot.py`, `verify_relabel_extended_boot.py`, `verify_relabel_concurrent_boot.py`, `verify_relabel_replacement_boot.py`, `verify_relabel_failed_replacement.py`, `verify_relabel_caller_retirement.py`, `verify_relabel_caller_memory.py`, `prepare_relabel_data_disk.py`, `verify_relabel_persistent_boot.py`, `prepare_relabel_cold_remount.py`, `verify_relabel_cold_remount.py`, `verify_relabel_name_boundaries.py` (see [boot procedure](../../docs/Commands/Workbench31MorphOS320/relabel-original-dos-boot.md)) |
| `MorphOS320AddBuffersEntry` | Native MorphOS AddBuffers entry: 6 | 18 | `tools/Commands/qualify_addbuffers_native.ps1` |
| `Workbench31AssignEntry` | Workbench Assign public-vector mutation candidate (three-CPU resident HUNK) | 19 supplied vectors per CPU | `tools/Commands/qualify_workbench31_assign_native_runtime.ps1` |
| `NativeMorphOSAssignEntry` | MorphOS 3.20 Assign public-vector mutation candidate (three-CPU resident HUNK) | 19 supplied vectors per CPU | `tools/Commands/qualify_morphos_assign_native_runtime.ps1` |
| `Workbench31AddBuffersEntry` | Original 68000 versus generated target: 27 comparisons + 1 generated allocation failure | 81 original + 84 generated; 81 comparisons | `tools/Commands/qualify_workbench31_addbuffers_native_entry.ps1 -OriginalAddBuffersHunk <private-37.2.hunk>` |
| `Workbench31MakeDir` | Original: 38; generated: 39; comparisons: 38 | 114 original + 117 generated; 114 comparisons | `tests/Commands.NativeRoot`, separate `qualification-makedir/` |
| `MorphOS320MakeDirEntry` | Source-informed NAME/M,ALL/S resident candidate: public-DOS vectors | 15 per CPU | `tools/Commands/qualify_morphos_makedir_native.ps1` |
| `Workbench31MakeDirEntry` | Workbench 37.2 NAME/M resident candidate: public-DOS vectors | 14 per CPU | `tools/Commands/qualify_workbench31_makedir_native.ps1` |

These are required counts, not a substitute for a passing receipt. Foundation
and EvalNumeric each have their own `bin/Release/net10.0/qualification/`
directory and `latest.json`. MakeDir uses `qualification-makedir/` under the
NativeRoot output so it cannot replace Foundation's pointer. A run creates
its durable receipt before resolving or building
the compiler; failed bootstrap, compile, execution and input checks retain
their failure stage and logs. A successful latest pointer is replaced only
after every required artifact and final input check passes.

The runner snapshots sources/settings, forces fresh builds, copies declared
managed dependency closures, and compiles/executes those copies. It binds
restore metadata, build-tool identities and the selected .NET runtime. It
rechecks both live inputs and snapshots before/after tool use and publication,
including added source or binary files. Reports must identify the actually
loaded executor and instruction-core DLLs and the pinned runtime version.
The exact reachable assembly set and each assembly hash must match the bound
inputs. This is not a claim of a hermetic host operating system.

For each MC68000, MC68020 and MC68040 target it:

1. Emits an actual HUNK with resident context, no managed memory/exception
   runtime, no FPU requirement, and no unrelated exports.
2. Checks the dependency closure and absence of shared writable data/BSS.
3. Executes the Foundation probes in user mode through public Exec/DOS vectors.
   Library handlers overwrite volatile registers. CLI and Workbench startup,
   argument byte/length preservation, result/IoErr, aligned result storage,
   numeric/switch pointers, allocation and parser failures, output failures,
   resource ownership, repeated release, and Workbench reply ordering are
   checked by the instruction executor. The boundary suite also covers empty
   templates, invalid/count-overflow inputs, allocation span/alignment errors,
   preserved ambient IoErr and result-pointer lifetime.
   The I/O suite checks raw full/short/zero/-1 results, immediate error capture
   including zero and unknown signed errors, borrowed Input/Output BPTRs,
   non-consuming Ctrl-C polls and retained task state across invocations.
4. Reuses one relocated image for sequential success/failure invocations and
   pairs of instruction-interleaved processes, each with independent stacks,
   arguments, DOS bases, streams, errors and allocation ownership. Every
   native write is checked against the active invocation's storage. Shared
   code and constants are protected and compared after execution.
5. Recompiles identical copied inputs and compares the HUNK SHA-256. Reports
   bind each result to native bytes, source, compiler, target, SDK and fixture.

The EvalNumeric mode calls the production `EvalNumericFormatter` public
`long`/`ulong` interface through a private control block. It executes generated
integer-formatting instructions without host gateways or external native
targets. Cases cover signed decimal, unsigned hexadecimal/octal, prefix/LF,
64-bit boundaries, short/invalid buffers, address wrap and interleaved callers.
The MC68000 physical boundary is 24-bit; MC68020/040 use synthetic 32-bit bus
regions. It does not implement Eval parsing, full LFORMAT, TO or its command
entry. All modes use 4096-byte and 16384-byte stacks and report observed writes;
none establishes a minimum shipping stack.

MakeDir compiles the independent classic CLI body and executes both it and the
private original 37.2 machine code with supplied DOS vectors. The original must
be the pinned 464-byte member with SHA-256
`23911db49742055d8bddcfe5f8de82cbb2232f8a7ef850d51bfd27f9b54c819b`,
held outside the repository. The parameter can instead be supplied through
`COPPEROS_WB31_MAKEDIR_REFERENCE`. Its identity is rechecked throughout the run,
but its bytes are never copied to a source snapshot, artifact or receipt.
Original and generated invocations are counted separately; the additional
generated case covers result-slot allocation failure. The comparison includes
result/error selection, command-owned VPrintf bytes and PrintFault requests.
It does not claim an original DOS parser/formatter, real filesystem, complete
stdout, Workbench launch or booted command execution. See the
[MakeDir fixture contract](../../tests/Commands.MakeDirNativeExecution/README.md).
The public SDK returns `BPTR?` for locks. Only MakeDir's exact `HasValue` and
`Value` intrinsic bindings are admitted; these emit native tests/loads. The
`nullable-values` metadata feature does not authorize a managed runtime,
allocation, helper, external native target or fatal-machine-fault site.

The I/O suite has one separately audited compiler-generated native return tail.
The runner retains its reported helper count and admits only its exact name,
ten bytes, code/data boundary, real ReadOnce/WriteOnce symbols, matching
prologues and incoming branches, with no relocation overlap. It does not admit
an arbitrary helper prefix or an external runtime. The other suites still
require zero helpers; each artifact records its helper-admission evidence.

The regression script has 37 cases: thirteen original rejection cases, two
positive helper controls and 22 attempts to bypass the exact native-tail proof.
The original rejection cases cover failed compiler
bootstrap, modified source, added source, modified snapshot DLL, modified live
DLL, added snapshot DLL, modified external reference bytes, and six attempts
to expand the precise nullable-intrinsic admission. Tail cases change helper
counts/names, code/data layout, bytes, symbols, prologues, branches, relocations,
scope and HUNK records. The script uses
disposable inputs and the real qualification
guard functions; it never edits the real compiler to inject failure. Failure
receipts must persist without changing the preceding successful latest pointer.

Foundation is a **native vector fixture**, not a complete Amiga operating system.
Its ReadArgs handler supplies explicit fixture results while checking ABI and
ownership; it does not parse input or prove DOS grammar compatibility. It
does not implement Shell resident registration/removal, boot, installed
protection fields, or original command behavior. Those remain independent
CC03/CC06/CC08/CC09 gates. The probe is never installed into `SYS:C` and a pass
does not authorize a P bit or mark any of the 200 commands complete.

The fixtures currently admit the compiler's single code/constant HUNK layout
and rejects unexpected allocation/runtime behavior. In particular, it does
not qualify the compiler's large heap-allocated resident context, whose
failure mapping is tracked as `CC02.API04` in the API audit. Shipping command
entries using that path are not admitted until a command-specific bootstrap
policy is implemented and tested. Adding a new runtime path requires its own
allocation-failure and resource-lifetime checks.
