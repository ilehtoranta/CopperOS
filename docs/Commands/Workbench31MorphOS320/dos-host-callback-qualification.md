# API14: host DOS callback ownership checkpoint

Recorded 2026-08-30. This closes the bounded host callback regressions below,
not a command, original-OS, native CPU, removal/Exit, or residency gate. The
[stable goal](../../../Goals/WORKBENCH31_MORPHOS_C_COMMANDS_GOAL.md) and all
shipping/P admissions remain unchanged. Portable input ownership and native
callback execution have separate receipts in the
[input-context notes](runcommand-input-context-implementation-notes.md).

## Implemented host behavior

The MedPlayer owner is
[DosServices.cs](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Emulator/CopperStart/Dos/DosServices.cs).
It now keeps a LIFO callback stack for each task. A nested RunCommand preserves
the outer callback and its input position; another task has independent state.
Before callback entry, the owner checks the callback kind, instruction-fetch
address and actual continuation-push slot. RunCommand additionally checks its
mapped writable Process, StackSwap descriptor and owned stack span.

RunCommand uses the existing portable Exec StackSwap operation, exchanging
public task bounds, descriptor fields and the active stack pointer together.
The pending record binds the DOS state, expected return SP and exchanged
descriptors. On an accepted return, the owner moves back to the caller stack
before the portable RunCommand owner restores input and frees command storage.
A wrong return context leaves the callback pending. A declined portable resume
reverses the tentative stack exchange and retains the host record for a valid
return; it does not consume the callback or its allocation.

When the host proves that a callback cannot start, it reports Resume(false) to
the portable owner. This includes missing starter support and malformed or
unwritable entry/stack context. Successor cleanup callbacks follow the same
existing 16,384-boundary limit as the native dispatcher. Exceptions after the
starter was invoked are not treated as proof that the callback never entered.

Reset retains host callbacks and provider resources until ordinary callbacks
return. ReleaseTask similarly defers while a callback remains. These changes
protect storage; they do not implement cancellation of a removed task. The
separate [retirement owner plan](runcommand-retirement-owner-plan.md) records
why actual RemTask/reaping needs scheduler and external-pointer ownership
proof before abandoned command storage can be released.

## Reproduced failures and unchanged regressions

The new
[callback tests](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/DosServicesRunCommandCallbackTests.cs)
model the starter's return-word push and supply guest returns. They execute
zero guest instructions and no original ROM or distribution command.

| Checkpoint | Result | Scope |
| --- | --- | --- |
| Original host owner, initial 14 rows | 10 failed, 4 passed, 0 skipped | Nested/interleaved callbacks, missing-start rollback, malformed stacks, bounds, return validation, deferred reset and direct task-release deferral. |
| Host lifecycle correction, identical 14 rows | 14 passed, 0 skipped | Same test-source bytes; callback input/stack ownership restored. |
| Existing DOS class plus new 14, original old fixtures | 29 passed, 2 failed, 0 skipped | Old InternalLoadSeg fixture had SP zero; old RunCommand fixture had neither NT_PROCESS nor established Input. These are fixture-context gaps, retained separately. |
| Two fixture contexts corrected | 31 passed, 0 skipped | Added a usable stack and a real Process with opened/selected NIL input. Original assertions remain. |
| Added rejected-successor regression, prior corrected owner | 14 passed, 1 failed, 0 skipped | InternalLoadSeg read/EOF requests allocation, but a newly read-only continuation-push slot prevents that successor from starting. |
| Final owner, same 15 focused rows plus existing class | 32 passed, 0 skipped | Final normal run and independently bound repeat both pass. |

The successor regression distinguishes the original DOS call's captured PC
`$6006` from the callback gateway's post-token PC `$F08C2E`. The old completion
path restored `$6006` after the failed successor had already rolled back.
Passing the final disposition back from CompleteDispatch lets ContinueCallback
preserve `$F08C2E`, which the FF00 gateway needs for its ordinary implicit RTS.
The regression also checks no successor entry, exact scratch cleanup, caller SP,
register restoration, BadHunk, and harmless duplicate completion. It changes
only a mapped stack-slot protection in the fixture, not private DOS records.

## Source, binary and run binding

The private checkpoint is
`D:/TestData/CopperOSCommands/Q/a2538df7`. Its
[final receipt](D:/TestData/CopperOSCommands/Q/a2538df7/host-callback-receipt.json)
has SHA256
`842729030ce382cb4c27b7b3dc8be1be7bee6c5e942a75bf003bceebb3457d76`.
It binds 1,118 files before and after the final repeat, including 39 pinned
dependency DLLs, 204 resolved reference paths, 129 source items from the two
ResolveReferences queries, 29 generated/build inputs and 366 toolchain/runtime
inputs. All inputs remain unchanged. The final
[32-test TRX](D:/TestData/CopperOSCommands/Q/a2538df7/bound-final-results/host-callback.trx)
is `50bc70b393377f9f844717ff7761b35c773237169164a2507732a6cd8622de08`.

| Input/output | SHA256 |
| --- | --- |
| Original DosServices source | `c2902d7881d684b39e03b5784c214f9f271b67836559946c329d37c1368c3d7d` |
| First 14/31-pass DosServices source | `5d48ae29c50ee2745c703bd31c655b68ac10ded2fd1838cc7f6e50e2a482eedd` |
| Final DosServices source | `86d2a37327c9a2f5e2ace04551a1308300abb0941b1dad38e1a29f962d3af1db` |
| Unchanged initial 14-test source | `d0ec7ff03677514794b3c8a631e1f2dd32dcbbce9b36fdc5ed26aaaf4dbd9f9a` |
| Final 15-test source | `3597ee11b2493e7c814b25dc0b511eb888246bf26e2def1e4255ebda1d25dedb` |
| Corrected existing DosServicesTests fixture source | `29789636fc780737131954352097128635282091614c43f5a8c93e5bee9b983d` |
| Final private emulator DLL | `c0d4f6fcb7b40fe8ad842187d6a788a39ea62d9cff30d87a6fd08b9a5dccd85f` |
| Final private test DLL | `c22cb0f0e258bb79a8f2c4afd93ba85a6b5d997679889fd757aa1ccd712b8ebe` |
| Consumed portable input-lease DOS DLL | `0b09af6ff0e225ac662071c2e7607f44be47a97a65559820028da560aee08df5` |
| Frozen Amiga core DLL | `5a1b03d93cdb65e9706d3d8c7ffe12219d293e1766a593f4937c2a7efaada9a7` |
| Frozen Copper68k DLL | `bd42669eb9a241c96419fba48ae64183e8e3a0c39a55259ebe32dead681e53b2` |

The later native-dispatch rejection DLL is not substituted into this host
checkpoint. Its separate native receipt is not evidence of extra execution
here. No shared production output was rebuilt.

Five complete test output closures are retained: before, first 14-pass,
31-pass, rejected-successor baseline and final 32-pass. A subsequent
[PDB audit](D:/TestData/CopperOSCommands/Q/a2538df7/host-callback-pdb-audit.json),
SHA256 `d5466f282266f8d5e803f0a8af0b1e390f858bb2dd94700ea14ac62ef62009f6`,
matches all ten emulator/test DLL-PDB pairs and all 665 document checksums to
their retained source bytes. PDB documents supplement the build-input records;
they are not by themselves a complete compiler-input inventory.

## Explicit build and qualification limits

The initial 292-file capture respected ignore rules and was not a complete
buildable current emulator closure. All setup failures remain recorded:
simple assembly-name resolution, newer Intuition sources needing unavailable
dependencies, two missing Icon files, a private-layout test accessibility error,
and one misspelled adapter read method. The private overlay uses the earlier
observed emulator source membership plus the two PDB-matched Icon sources.
Four newer Intuition files are excluded; two current graphics source deltas
remain. Accepted private builds have zero warnings/errors. This is a bounded
DOS-owner build, not qualification of the entire current MedPlayer tree.

Actual CPU callback entry/return, native normal-input composition, real DOS
handlers, full boot, nonlocal Exit and task-removal cancellation remain separate
work. In particular, the direct ReleaseTask test supplies an ordinary return
after deferral; it does not model a task which RemTask has already removed.
No installed command, protection flag, minimum stack, resident registry or
same-SegList operating-system claim is admitted by this checkpoint.
