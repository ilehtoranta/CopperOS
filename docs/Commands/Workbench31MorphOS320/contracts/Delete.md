# Delete contract

Profiles: wb31 and morphos320. Goal steps: CC01 and CC13. Recorded:
2026-09-11.

Status: partial Workbench/MorphOS release-source grammar and lifecycle evidence
plus bounded native Delete frontends and a final-object-removal stage. This is
not a complete Delete qualification: protection retries, diagnostics,
recursion, packed-binary correspondence, real handler behavior, packaging and
parity remain open.

The [2026-10-04 size receipt](../size-reductions-20261004.json) selects a
deletion-only object worker through the existing generic Copy traversal.
With the same clean compiler/SDK, the shipping MC68000 HUNK shrinks from 9,524
to 5,980 bytes, removing reachable file-transfer and destination-creation code.
Eight supplied Workbench invocations per CPU match baseline output, result,
IoErr, allocations, cleanup and DOS call order on 68000/020/040. The MorphOS
profile separately passes eight cases per CPU. These cases cover regular
files, FORCE, partial failure, parser cleanup and interleaving; recursive/link
behavior, parent-protection retry and original guest parity remain open.

The observed Workbench 3.1 v40.42 `C/Delete` HUNK is 1,972 bytes with SHA-256
`78b2714a750a5e2d7635238049990303f49c758808eef857b69cf8df28338d5a` and
its raw string scan finds the syntax candidate:

```text
FILE/M/A,ALL/S,QUIET/S,FORCE/S
```

The candidate is not evidence of parser use or of classic traversal, link,
force, output, result, or IoErr behavior.

The observed MorphOS 3.20 `MorphOS/C/Delete` is a 3,616-byte packed native
member, SHA-256
`d1dbaeb539baec60386e5c5ec3f45aa21c36f92260f94cde60cd7bb87fb527f8`, with
version tag `Delete 50.13 (26.7.2025)`. The hash-bound 3.20 release archive
contains `c/delete/delete.c`, 17,482 bytes, SHA-256
`86e4c8a518d045f1b4f0703c35ba213d881342614d8dac4577ff7bb1ef312a82`. It
uses the extended MorphOS template:

```text
FILE/M/A,ALL/S,QUIET/S,FORCE/S,FOLLOWLINKS/S
```

Source inspection observes an invocation-local double AnchorPath workspace,
`ReadArgs`/`FreeArgs`, public DOS pattern traversal, Ctrl-C polling, and
`MatchEnd` on every completed or interrupted pattern path. The MorphOS link
policy is explicit: FOLLOWLINKS controls directory-link descent; current
DOS-library version support adds literal soft-link behavior and avoids following
soft links by default. A source warning leaves the exact hard-link-follow rule
open for capture.

For each matched object, source order is: determine directory state, descend
only under ALL, copy the current AnchorPath before `MatchNext`, check delete
protection, then call public `DeleteFile`. FORCE may clear the target's delete
protection and, for specified protection errors, temporarily adjust and restore
the parent protection before retrying. The source rejects a device/volume root,
continues after object failures, reports protected and failed objects, suppresses
success names under QUIET, treats no deleted object as WARN, returns ERROR on
Ctrl-C with ERROR_BREAK, and retains the last relevant error in its
Commodore-compatibility result path. These are source observations, not packed
binary parity.

The selected MorphOS installer script adds P for Delete at `hdinstall.fixc`
line 19. A classic install script temporarily makes it resident. Both establish
a pure/reentrant replacement requirement, not installed metadata or resident
lifecycle evidence. A body must use the public DOS matcher, handler, link, and
protection APIs; it must not walk host directories or replace handler policy.

Before a body is admitted, record controlled original captures for empty and
multiple FILE vectors, files, empty/nonempty directories, patterns, malformed
patterns, device roots, protected targets and parent retries, ALL/QUIET/FORCE/
FOLLOWLINKS combinations, link loops, handler errors, Ctrl-C, partial success,
result, output and IoErr. Packed correspondence, source-reuse rights, real
handler behavior, installation profile, and cross-profile parity remain open.

## Bounded native final-object-removal stage

[NativeMorphOSDeleteObject.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSDeleteObject.cs)
independently implements only the source-observed final `DeleteFile` action for
one already-selected, deletable object. A null name fails with `ERROR_BAD_TEMPLATE`;
otherwise it calls public DOS `DeleteFile` exactly once and preserves the
immediate `IoErr` on failure. It owns no matcher, AnchorPath, object protection,
FORCE handling, parent-protection retry, diagnostics, directory recursion, or
final command result aggregation.

The private
[NativeMorphOSDeleteObjectEntry.cs](D:/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeMorphOSDeleteObjectEntry.cs)
is a four-byte direct-control receipt adapter, never the external `Delete`
entry. It opens `dos.library` v37 through the common startup owner and passes
one caller-owned CString pointer to the stage.

[qualify_delete_object_native.ps1](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_delete_object_native.ps1)
builds resident HUNKs for 68000, 68020, and 68040 and executes six supplied
direct vectors per CPU: file and empty-directory success, handler failure,
null-name rejection, and interleaved success/failure callers. The 2026-09-04
receipt has five reachable methods and no managed runtime features/helpers,
external native targets, exception regions, fatal machine fault sites, resource
leaks, or shared-image writes. Each artifact is 1,024 bytes, SHA-256
`bdd6f55f1c58488e5d15c96ff8c740e62149ed22511757b9042df8bfd43f5eab`.
The vectors supply DOS results; they do not prove handler behavior, parsing,
matching, protection/retry semantics, packaging, full command behavior, or
original-reference parity.

The public SDK now also exposes raw BPTR forms of `CurrentDir` and `ParentDir`
alongside their nullable convenience forms. Those imports are preparation for a
future resident parent-protection retry stage; their presence is not evidence
that the source's retry and restoration behavior is implemented.

## Bounded MorphOS frontend

[NativeMorphOSDeleteCommand.cs](D:/D-drive/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSDeleteCommand.cs)
adds the public MorphOS Delete entry around the shared DOS traversal. It uses
the exact five-result template `FILE/M/A,ALL/S,QUIET/S,FORCE/S,FOLLOWLINKS/S`,
keeps the `ReadArgs`/`FreeArgs` lease invocation-local, and allocates a fixed
2,600-byte cleared workspace for the classifier AnchorPath, path snapshot,
FileInfoBlock snapshot and warning arguments. Source vectors are dispatched
through `MatchFirst`/`MatchNext`/`MatchEnd`, locks and protection calls, and
`DeleteFile`; the shared result policy owns break, QUIET and final result
selection. FOLLOWLINKS is carried in a frontend-private state bit so existing
Copy behavior is unchanged. The Workbench startup adapter is deliberately not
claimed by this body because its four-option template and original behavior
still need separate evidence.

[qualify_delete_command_native.ps1](D:/D-drive/Koodit/GIT/CopperOS/tools/Commands/qualify_delete_command_native.ps1)
produces the 2026-09-12 receipt in
`artifacts/delete-command-native-20260912-qualified/qualification.json`. Resident HUNKs
compile for 68000 (15,252 bytes), 68020 (15,376 bytes), and 68040 (15,180
bytes); each has 41 reachable methods, zero managed runtime features/helpers,
external native targets, exception regions and fatal machine-fault sites. The
native runner executes eight supplied-DOS invocations per CPU covering the
five-slot parser lease, parser/empty-vector failure cleanup, one/two regular
file matches, lock ordering, FORCE protection, partial failure and
interleaving, with no leaks or shared-image writes. This remains a bounded
frontend checkpoint: parent-protection retry, exact diagnostics, recursive/link
behavior, packed-binary correspondence, real handlers, Workbench profile,
packaging and differential reference evidence remain open.

## Bounded Workbench 3.1 frontend

[NativeWorkbench31DeleteCommand.cs](D:/D-drive/Koodit/GIT/CopperOS/src/Commands/Native/NativeWorkbench31DeleteCommand.cs)
and its resident
[Workbench31DeleteEntry.cs](D:/D-drive/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/Workbench31DeleteEntry.cs)
provide the separate classic startup boundary. The entry opens `dos.library`
v36 and parses the four-slot Workbench template
`FILE/M/A,ALL/S,QUIET/S,FORCE/S`; it shares only the already bounded DOS
matcher, lock, protection and `DeleteFile` worker with the MorphOS frontend.
The MorphOS-only `FOLLOWLINKS` result slot is absent from this profile.

[qualify_workbench31_delete_native_entry.ps1](D:/D-drive/Koodit/GIT/CopperOS/tools/Commands/qualify_workbench31_delete_native_entry.ps1)
produces the 2026-09-12 receipt in
`artifacts/delete-wb31-native-20260912-qualified/qualification.json`. Resident
HUNKs compile for 68000 (15,480 bytes), 68020 (15,608 bytes), and 68040
(15,408 bytes), each with 43 reachable methods and zero managed runtime
features/helpers, external native targets, exception regions or fatal
machine-fault sites. The native runner executes eight supplied-DOS invocations
per CPU with no leaks or shared-image writes. This is a syntax/startup
candidate checkpoint only: exact Workbench behavior, diagnostics, recursion,
link policy, PURE/resident lifecycle, packaging and original differential
parity remain open.
