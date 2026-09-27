# CopperStart filesystem integration checkpoint

Recorded: 2026-09-08. Command/profile: Workbench 3.1 MakeLink. The versioned
68000 command now passes a bounded native CopperStart DOS run using the
production configured host filesystem. A separate test verifies the production
`DosServices` guest packet queues. These are distinct executions, not a combined
native Shell launch or full CopperStart boot. Complete runtime, minimum-stack,
PURE/resident, installation and shipping gates remain open.

## Original FileHandle ABI correction

The original NDK 3.1 `dos/dosextens.h` aliases `fh_Arg1` to `fh_Args` at byte 36;
`fh_Arg2` is at byte 40, and the structure is 44 bytes. The previous SDK named
the byte-40 field `Argument1`. A native sender and fixture handler using that
same definition could agree while disagreeing with an original handler.

The SDK owner now publishes `FileHandle.Argument1` at 36, `Argument2` at 40,
and the older `Arguments` spelling as a get/set alias of `Argument1`.
`DosLayout.FileHandle` and `DosFileHandleCodec` match. Ordinary source reads,
writes and initializers using `Arguments` remain supported; it is now a
property, so field reflection, by-reference access and precompiled binary
compatibility are not promised. SDK and Support consumers require rebuilding.
All existing native sender `Handle.Argument1` consumers now select byte 36
when rebuilt; this correction required no compiler or CPU change.

The native regression writes distinct cookies directly to literal offsets 36
and 40, independently of the SDK codec, then checks the native Read, Write,
Seek64 and End packets and resource cleanup. The accepted runs contain 89 SDK
layout checks and 39 CopperStart packed-record/core/native-provider checks.
The original header is bound by private-media entry and SHA-256 in the report;
no vendor source was copied into the repository.

## Fresh current-source command builds

The current two-build receipt is
[workbench-makelink-filehandle-abi-20260908/qualification.json](../../../artifacts/workbench-makelink-filehandle-abi-20260908/qualification.json).
It replaces the old SDK-bound metadata build as current-source evidence.
Its 453 observed source/project/configuration inputs match current files;
this is not a hermetic cross-host build claim. Both fresh passes produce
identical raw and versioned HUNKs, and the versioned bytes also equal the
preceding tagged images:

| CPU | Bytes | Versioned HUNK SHA-256 |
| --- | ---: | --- |
| 68000 | 3232 | `12d1f36504f5a0f7099bc419e4fffc5f43f138d7b4e8745a2ca907bcb73b5f45` |
| 68020 | 3272 | `f2ee57c9d4f79b971d98718ec1585b8cd9780b085699abd04f7aa808789c1fd8` |
| 68040 | 3228 | `34fdd4711ebd79aae834cc064c2cf142e7ec4581a96f9402c06a6df8c2eb566a` |

The existing supplied-vector suite passes 20 invocations per CPU, 60 total,
including interleaving with no shared-image writes. Its 16384-byte stacks do
not qualify the declared 4096-byte minimum. The tag remains
`$VER: MakeLink 0.1 (8.9.2026) CopperOS wb31`; dependency floors and development
restrictions remain as recorded in the [MakeLink contract](contracts/MakeLink.md).

The old `workbench-makelink-versioned-20260908/qualification.json` remains
historical build evidence; its SDK source snapshot is not current. Independent
original-DOS and original C:Version receipts still identify their exact,
unchanged HUNK and observer bytes. Their continuing applicability follows the
verified same executable identity, not an assumption that unchanged source
fragments transfer runtime evidence to a different executable.

## Native command with the production host filesystem

The exact versioned 68000 HUNK above executes five invocations through native
CopperStart RunCommand, ReadArgs, formatting, mutation and DOS packet-provider
code. The rebuilt native DOS HUNK has SHA-256
`675560c38cac37a679db980e0595b393ce9417e7ae7fd0a09d4d226999cbbeb7`.
The backing filesystem is the production `ConfiguredDosPacketDispatcher`,
`ConfiguredDosHostHandler` and `ConfiguredDosVolume`, linked from their actual
sources. Exec allocation, packet delivery and StackSwap remain fixtures.
The native test does **not** execute the `DosServices` owner queue or Shell.

| Invocation | Primary result | Final IoErr |
| --- | ---: | ---: |
| Default hard-link success | 0 | 0 |
| Duplicate destination | 20 | 203 |
| Absent target | 20 | 205 |
| Missing required TO | 20 | 116 |
| Recovery success | 0 | 0 |

The run performs 6,019,914 CPU instructions, including 2,483 command
instructions, and 496 production packet dispatches. Captured diagnostic bytes
are exactly `object already exists\n`,
`Can't find DH0:absent object not found\n`, and
`required argument missing\n`. The source name is deleted; native public DOS
reads and independent host readback confirm both aliases retain
`native-provider-payload\n`. The existing destination remains unchanged,
caller streams survive, invocation allocations balance, and no host files,
host locks or guest lock allocations remain live. Command/DOS image and vector
integrity checks pass. Success IoErr 0 is this provider's result; it does not
replace the separate original-DOS fixture's observed success IoErr 205.

The terminal suite has 12 passing tests: the five-invocation native test, six
dispatcher tests and five aligned formatter cases. Dispatcher tests cover
guest BPTR/host-token separation, allocation-failure rollback, reset aborting
an unclosed writer, foreign/unsupported routing, separate volume roots and
Seek64 failure returning BOOL false rather than classic Seek's -1. Wrong-port
file/lock requests do not consume the rightful owner's tokens. The complete
production emulator build also passes, independently of runtime scope.

## Separate production owner queue test

`DosServicesConfiguredPacketOwnerTests` discovers DH0 and DH1 ports through
public DOS list operations, then places genuine StandardPacket messages on
their public Exec queues. Both requests are queued before `ProcessPending`.
They have the same volume-stripped filename and a zero base lock; the selected
destination port routes them to distinct host files of three and seven bytes.

Two LocateObject, two ExamineObject and two FreeLock requests produce exactly
six replies. The test checks real guest lock records, both FIB entry types,
guard bytes, and exactly one free of each lock. Further `ProcessPending` calls
produce no extra replies, allocations or frees. Legacy filesystem callbacks
throw if reached. This verifies the production queue-to-dispatcher connection
in a managed owner test; it is not part of the native MakeLink execution above.

The single test passes in an isolated friend test assembly referencing the
complete production emulator. The broad MedPlayer test project was blocked
by unrelated duplicate aliases in `BitplaneStorageStopTests.cs`; that failure
log remains preserved and the unrelated file was not changed. The isolated
pass is not reported as a complete MedPlayer test-suite pass.

## Bound receipts and remaining gates

The following report hashes and their recorded file bindings were checked
against the local files when writing this checkpoint. The fresh command
source snapshot was also checked independently. Exact source, SDK/Support,
compiler, test assembly, native image and terminal identities remain in the
reports rather than being substituted between isolated builds.

| Evidence | Qualification receipt SHA-256 |
| --- | --- |
| [SDK ABI correction](../../../artifacts/filehandle-arg1-layout-20260908/qualification.json) | `985fe731f23fc49093a4c276cfe34027e91a5bd84d6bd4d620c28efac71b924c` |
| [Fresh command two-build/vector run](../../../artifacts/workbench-makelink-filehandle-abi-20260908/qualification.json) | `fcebfca783daef8febb3c93fac71f47fff48510693d9d700b44b356941b00305` |
| [Native production filesystem](../../../artifacts/copperstart-makelink-host-packet/qualification.json) | `e98f576b190fb1ff166dc095cfc932727786fb4a85525fce48bc8c6c5fc7057f` |
| [Separate production owner queues](../../../artifacts/configured-dos-packet-owner-20260908/qualification.json) | `a2ae8a87ac690af3ddac094707159f8e2d058f21bcf5f9e693c0f7d3a656949a` |

Earlier unversioned native/fixture-handler and old SDK-bound build receipts
remain historical, with no overwritten reports or transferred binary claims.
Combining the two successful integration tests does not establish a single
end-to-end native Shell/system boot. Broader handler and command branches,
the complete runtime/dependency gate, minimum stack, required pure/resident
lifecycles, release admission, actual image staging and protection readback
remain required. MorphOS packed-original correspondence and its additional
profile behavior remain separate work. Shipping remains 0/200 commands and
0/246 profiles; no P or complete runtime gate is closed here.

## Concrete production boot dependency

Read-only follow-up found that the existing production boot path is not yet a
drop-in runner for the native DOS checkpoint. `AmigaBoot.InstallBootHostTraps`
installs DOS vector gateways to `DosServices.InvokeGeneric`, while
`DosServices.PublishConfiguredVolumes` publishes configured volumes into its
managed DOS state. The generated native DOS library must be admitted into the
production launch path and receive those volumes through the matching public
DOS-list/handler interfaces before one run can exercise the native provider
and the production queues together.

`AmigaDosFileSystemTests.BootControllerLaunchProgramSetsStartupRegisters` and
`BootControllerAutoRunStartupSequenceContinuesNativeLoadWbAfterReadArgs` are
nearby production boot regressions. Merely replacing their loaded HUNK does not
close the gap: `AmigaBoot.TryLaunchProgram` directly establishes a subroutine
and D0/A0 startup registers, rather than the native DOS RunCommand input lease.
The next integration must preserve the existing Shell/process owner and prove
native DOS admission, configured-volume publication and command launch together;
it must not add a parallel Shell, private filesystem or fixture packet adapter.

The subsequent [native boot checkpoint](native-boot-integration.md) implements
admission and volume publication and records their combined passing readiness
run. It also records the production Exec callback fixes and the next concrete
failure in the compiled driver; command/Shell execution remains unqualified.
