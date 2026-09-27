# Copy contract

Profiles: wb31 and morphos320. Goal steps: CC01 and CC13. Recorded:
2026-09-04.

Status: a native MorphOS-profile command body now includes parsing, mode
selection, traversal, metadata and cleanup. Bounded original-Kickstart-DOS
execution covers resident reuse, concurrency, copying, overwrite, recursion
and selected metadata/error profiles. This is not a shipping qualification:
full option/failure coverage, original MorphOS runtime parity, Workbench-profile
behavior, installed purity flags and packaging gates remain open. The dated
sections below retain the history of earlier incomplete entry points; consult
the progress log and hash-bound receipts for the current revision.

The observed Workbench 3.1 v40.42 `C/Copy` HUNK is 5,580 bytes with SHA-256
`930f081b7dbdcae9d2fef7fd516dd8a92102f1f740071bd224f4637051c41529`. Its
raw candidate is `FROM/M,TO/A,ALL/S,QUIET/S,BUF=BUFFER/K/N,CLONE/S,DATES/S,
NOPRO/S,COM/S,NOREQ/S`; it does not establish classic command behavior or the
MorphOS extension set.

The observed MorphOS 3.20 `MorphOS/C/Copy` is a 9,970-byte packed native
member, SHA-256
`36944a01d0996892fef5d24076e2900c5513bacbdae1e81d95fecf990137b517`, with
version tag `Copy 50.21 (17.8.2025)`. The hash-bound 3.20 release archive
contains `c/copy/copy.c`, 72,173 bytes, SHA-256
`13be3cb51223c726aa13abbbac28e9a4545a0a36ec32621262c7bda27a69808f`.

Its source ReadArgs template has the extended FROM/TO, PAT=PATTERN,
BUF=BUFFER, ALL, DIRECT, CLONE, DATES, NOPRO, PROX, COM=COMMENT, QUIET, NOREQ,
ERRWARN, MAKEDIR, MOVE, DELETE, HARD=HARDLINK, SOFT=SOFTLINK,
FOLNK=FORCELINK, FODEL=FORCEDELETE, FOOVR=FORCEOVERWRITE,
DONTOVR=DONTOVERWRITE, and FORCE fields. The exact string is in the source
archive and must remain the implementation parser authority.

Source inspection observes six mutually selected working modes: normal copy,
move, delete, make-directory, hard-link, and soft-link. DIRECT has a distinct
single-input/single-output path. The normal path uses DOS `ReadArgs` with an
allocated RDArgs object, matcher traversal with `MatchEnd`, destination and
loop checks, public DOS locks/examines/opens, and a CopyFile routine that polls
Ctrl-C and requires exact public Read/Write transfer counts. It creates missing
destination directories, distinguishes partial warnings from errors and fails,
and ERRWARN promotes a warning to ERROR.

CLONE/DATES/NOPRO/PROX/COMMENT control public protection/date/comment updates.
MOVE may rename or copy then delete; DELETE and the link modes have independent
handler and protection behavior. The source uses per-invocation buffers and
matcher state, returns a selected Result2 after cleanup, and restores requester
window state. It must not be replaced by host copy, link, or metadata calls.

The selected MorphOS installer script adds P for Copy at `hdinstall.fixc` line
15. This is required-pure design evidence, not installed protection or resident
lifecycle behavior. Before a body is admitted, capture every mode, their
conflicts, DIRECT restrictions, metadata options, destination/loop/cross-volume
cases, short I/O, break, partial results, output, final result and IoErr on
disposable original-profile systems. Packed correspondence, source-reuse rights,
real handler behavior, package placement, and Workbench/MorphOS parity remain
open.

## Bounded native streaming core

[NativeMorphOSCopyModeSelection.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSCopyModeSelection.cs)
independently models the source's five selected modes (copy, move, delete,
make-directory, and link) after positional target normalization. It rejects
multiple selected modes, SOFTLINK with ALL, a missing target where the source
requires one, implicit input for MAKEDIR, and the source's DIRECT restrictions.
It also maps compatibility FORCE to FORCEDELETE or FORCELINK, and makes soft
link selection force directory-link permission. Two focused semantic tests cover
single-mode selection, force mapping, and invalid combinations.

The private
[NativeMorphOSCopyModeSelectionEntry.cs](D:/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeMorphOSCopyModeSelectionEntry.cs)
is a 12-byte direct-control receipt adapter, not the external `Copy` command
entry. [qualify_copy_mode_selection_native.ps1](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_copy_mode_selection_native.ps1)
builds resident HUNKs for 68000, 68020, and 68040 and executes fourteen supplied
direct vectors per CPU: Copy, Move, Delete, MakeDir, hard and soft Link, FORCE
mapping, valid DIRECT Copy, five rejected combinations, and two interleaved
callers. The 2026-09-04 receipt
has two reachable methods and no managed runtime features/helpers, external
native targets, exception regions, fatal machine fault sites, resource leaks,
or shared-image writes. Artifacts are 1,084 bytes for 68000 SHA-256
`24dc4f7936456c9446a166a4b8fc015db58d379463b96f77ea9167e33cf02d1c`, and
1,052 bytes for both 68020 and 68040 SHA-256
`db6fbd163ee8d20b3f7ea3ef6071c59c65bb98d0c42dfddb3e873c452edd761a`.
It performs no ReadArgs call, positional target normalization, I/O, matcher,
metadata, output, or command lifecycle action. The supplied control vectors do
not prove real DOS parsing, handler behavior, packaging, full command behavior,
or original-reference parity.

[NativeMorphOSCopyArgumentGate.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSCopyArgumentGate.cs)
independently uses the exact 24-slot MorphOS `ReadArgs` template, with
invocation-owned result storage and a `FreeArgs`/`FreeMem` release path. It
preserves the source's default-Copy positional `TO` adjustment before applying
the mode gate; it rejects a mode, overwrite, or DIRECT conflict with
`ERROR_TOO_MANY_ARGS`. The private
[NativeMorphOSCopyArgumentGateEntry.cs](D:/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeMorphOSCopyArgumentGateEntry.cs)
is not an external Copy command body.

[qualify_copy_argument_gate_native.ps1](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_copy_argument_gate_native.ps1)
builds it as a resident HUNK for 68000, 68020, and 68040 and executes thirteen
supplied-DOS vectors per CPU: positional normal Copy/DIRECT, DELETE FORCE,
missing MOVE target, mode and overwrite conflicts, rejected DIRECT CLONE,
omitted-input rejection, parser failure, MAKEDIR, SOFTLINK+ALL, and interleaved callers. The 2026-09-04
receipt has 14 reachable methods and no managed runtime features/helpers,
external native targets, exception regions, fatal machine fault sites, resource
leaks, or shared-image writes. Artifacts are 5,528 bytes for 68000 SHA-256
`3b663c2e16b4a72dd2eb08c5ef6c1b68dfd588f9c3d763def9a5d237667aa51d`, and
5,488 bytes for both 68020 and 68040 SHA-256
`499f6f588ecefb20f0f1046655d4030cdd40f3c2086a477464dfd5246956b36b`.
The vectors supply DOS parser results and do not prove parser grammar, matcher
traversal, filesystem effects, metadata, output, packaging, or reference parity.

[NativeMorphOSCopyFilePair.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSCopyFilePair.cs)
adds one regular-file public-DOS operation around the existing stream loop. It
opens destination before source (corrected by the later source audit), closes destination before source, removes a
newly created destination after a failed transfer, and restores the first
open/transfer `IoErr` after cleanup. It intentionally does not create
destinations, match names, or update metadata.

The cleanup ordering matches the release source's `KillFileKeepErr`: a failed
transfer may remove the partial destination, but that cleanup cannot replace
the transfer error selected for Copy's later diagnostic/result policy.
[qualify_copy_file_pair_native.ps1](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_copy_file_pair_native.ps1)
qualifies nine supplied vectors per 68000/020/040 HUNK for success, both open
failures, read/short-write failure, Ctrl-C, invalid buffer, and interleaving.
The seven-method receipt has no managed features/helpers, external targets,
exception regions, fault sites, leaks, or shared-image writes. Hashes: 68000
`8dc0bba65c05dbc6600529a69383b806f054b14515bd241ac6b24d1da88f07df`
(1,692 bytes); 68020
`c8781ff5b4bde72c0c5458befeff6f871ff298ed7a7258efcf45df4fecd6b2d0`
(1,716 bytes); 68040
`7ff151ac7a655fcb57ec5f0128f625f621b0204ae96f51cc36a2294da7cf7ba1`
(1,692 bytes). This is not full Copy behavior.

[NativeMorphOSCopyDestination.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSCopyDestination.cs)
implements the bounded `TestDest` policy with public `Lock`, FIB allocation
and `Examine`, releasing the lock before mutation. It preserves an existing
required directory, reports `ERROR_OBJECT_EXISTS` for DONTOVERWRITE, and uses
`DeleteFile` for ordinary overwrite. FORCEOVERWRITE calls `SetProtection(name,
0)` before that deletion and still attempts the delete when setting protection
fails. Parent creation, requester/output behavior, matcher traversal, and full
path policy remain separate open stages.

The private
[NativeMorphOSCopyDestinationEntry.cs](D:/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeMorphOSCopyDestinationEntry.cs)
and [qualify_copy_destination_native.ps1](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_copy_destination_native.ps1)
produce a three-CPU resident receipt: five reachable methods and no managed
runtime features/helpers, external targets, exception regions, fatal fault
sites, resource leaks, or shared-image writes. Its 2026-09-04 HUNKs are 1,464 bytes for 68000 SHA-256
`abf3cd0b746378f5275a510bc0d5a57b5b5b9cd1f77444d62433dc90b468b5c4`, and
1,456 bytes for 68020/68040 SHA-256
`209f2c237dc3c4953cf20d070963156a27b5087da0547c1cd9988f479301450a`.
The direct entry passes eleven supplied public-DOS vectors per CPU for absent
target, FIB/examine failures, directory preservation, DONTOVERWRITE, ordinary
overwrite, FORCEOVERWRITE including a failed protection-clear call, delete
failure, and interleaved callers. Parent creation, real handler behavior, and
full Copy behavior remain open.

[NativeMorphOSCopyDestinationDirectories.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSCopyDestinationDirectories.cs)
now isolates the source's filesystem `OpenDestDir` prefix loop. For writable
slash-delimited caller storage, it temporarily terminates every prefix, applies
the bounded `TestDest` policy as a required directory, creates an absent or
replaced prefix, releases the temporary creation lock, restores the character,
and finally returns a caller-owned shared lock for the full name. Its private
[NativeMorphOSCopyDestinationDirectoriesEntry.cs](D:/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeMorphOSCopyDestinationDirectoriesEntry.cs)
and [qualify_copy_destination_directories_native.ps1](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_copy_destination_directories_native.ps1)
have a resident three-CPU receipt: six reachable methods and no managed runtime
features/helpers, external native targets, exception regions, fatal fault sites,
resource leaks, or shared-image writes. The 2026-09-04 HUNKs are 1,948 bytes for 68000 SHA-256
`ff1cf97b57bc19f8af4e1390abd51d62342d063db4dad8c0dc07561a68aa9c47`,
1,968 bytes for 68020 SHA-256
`58919d26d6f32f6f15b87245c644601c119f696d273682aab8a37eb2ec7c5d2f`,
and 1,944 bytes for 68040 SHA-256
`9b1be81dc616388226522f304ff6b93fda8db68bf7c44fe1792c051e842f6e62`.
It passes seven supplied public-DOS vectors per CPU for retained, missing, and
replaced prefixes, CreateDir failure, final-lock failure, and interleaved
callers. Non-filesystem fallback, diagnostics, MAKEDIR's existing-final-directory
result, matcher traversal, metadata, and full Copy behavior remain open.

[NativeMorphOSCopyNonFileSystemDestination.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSCopyNonFileSystemDestination.cs)
isolates the Copy/MOVE `OpenDestDir` non-filesystem branch. It temporarily
terminates the name immediately after its colon for public `IsFileSystem`,
restores that byte, copies a non-filesystem destination into caller-owned
storage, and obtains the source-observed shared `Lock("")`. Its private
[NativeMorphOSCopyNonFileSystemDestinationEntry.cs](D:/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeMorphOSCopyNonFileSystemDestinationEntry.cs)
and [qualify_copy_nonfilesystem_native.ps1](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_copy_nonfilesystem_native.ps1)
have a pure/resident three-CPU receipt with five reachable methods and no
managed runtime features/helpers, external native targets, exception regions,
fatal fault sites, resource leaks, or shared-image writes: 68000 1,612 bytes SHA-256
`355e5395cfdc8ebc60d67601e59cdd07f4347ad170fefe1514b2cbd7188814bc`,
68020 1,632 bytes SHA-256
`ff909f60a3cf005c8d28b96c83a63f17e2dcab34a140f01c898a7025422517f0`, and
68040 1,604 bytes SHA-256
`318a36931bd091fa31f3a31f25d168383f4245daa058c12d8cc1b4696b7542e0`.
It passes eight supplied public-DOS vectors per CPU for non-filesystem and
filesystem classification, mode gating, capacity failure, empty-name lock
failure, no-colon input, and interleaved callers. Diagnostics, matcher,
metadata, real handler behavior, and full Copy behavior remain open.

[NativeMorphOSCopyLoopGuard.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSCopyLoopGuard.cs)
isolates the source's `TestLoop` ancestor check. It first uses `SameDevice`,
then compares the source lock to the destination and successive `ParentDir`
locks with `SameLock`; it releases only acquired parent locks, leaving the
caller-owned destination lock untouched. Its private
[NativeMorphOSCopyLoopGuardEntry.cs](D:/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeMorphOSCopyLoopGuardEntry.cs)
has a pure/resident three-CPU receipt with six reachable methods and no
managed runtime features/helpers, external native targets, exception regions,
or fatal fault sites. Its [qualify_copy_loop_guard_native.ps1](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_copy_loop_guard_native.ps1)
receipt executes six supplied public-DOS vectors per CPU on one shared private
entry image: different devices, recursive target, ancestor parent walk, root
exhaustion, exact parent-lock ownership, and interleaved callers. HUNKs are
1,152 bytes for 68000 SHA-256
`085567e5f969f6cc9590f44ac92ec14aa19a9df3b0658cccdd0521feb99f7ae2`,
1,180 bytes for 68020 SHA-256
`0f4cb1c2e9d4ba864826ce5f75e8d1df9532b855782547556e325933290aa150`,
and 1,152 bytes for 68040 SHA-256
`90454f2e47637be4f3d48e8123fb19b75d20d096a29eb9e89368d69e9f54c290`.
The fixture has no resource leaks or shared-image writes. Full Copy lifecycle
integration remains open.

[NativeMorphOSCopyMetadata.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSCopyMetadata.cs)
implements the bounded MorphOS `SetData` metadata action: NOPRO precedence,
normal protection with ARCHIVE cleared, PROX execute/pure/script masking,
comment propagation, and classic or POSIX date selection from the FIB overlay.
Its private [NativeMorphOSCopyMetadataEntry.cs](D:/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeMorphOSCopyMetadataEntry.cs)
has a static pure/resident receipt on 68000/020/040: six reachable methods,
zero runtime features/helpers, external targets, exception regions, or fatal
fault sites. Its [qualify_copy_metadata_native.ps1](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_copy_metadata_native.ps1)
receipt exercises one shared private direct-control entry image on each CPU
with eight supplied public-DOS vectors: NOPRO precedence, normal ARCHIVE
removal, PROX masking, comment forwarding, classic/POSIX date selection,
no-op flags, and interleaved callers. Each HUNK is 1,236 bytes, SHA-256
`496ffb78e982826bc86010e1d69d50aee20750f376178a057359beabb5dfd55d`, with
six reachable methods and no managed runtime features/helpers, external native
targets, exception regions, fatal fault sites, fixture resource leaks, or
shared-image writes. The fixture confirmed an explicit `fib_Comment` pointer
at the documented FIB comment offset; it does not rely on the helper lowering
for that address. Full Copy lifecycle integration remains open.

[NativeMorphOSCopyLoop.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSCopyLoop.cs)
independently implements only the normal-path `CopyFile` stream loop over
caller-owned DOS handles and caller-owned guest buffer storage. Before every
read it observes Ctrl-C through `Exec.SetSignal(0, 0)`; a pending Ctrl-C sets
`ERROR_BREAK` and returns `RETURN_FAIL`. A read failure returns its immediate
`IoErr`; every non-failing read, including the zero-length EOF read, is followed
by exactly one DOS `Write`; and a short or failed write returns its immediate
`IoErr` as `RETURN_FAIL`. It owns no lock, stream, RDArgs object, buffer, or
metadata state.

The private
[NativeMorphOSCopyLoopEntry.cs](D:/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeMorphOSCopyLoopEntry.cs)
is a 16-byte direct-control receipt adapter, never the external `Copy` command
entry. It opens `dos.library` v37 through the common startup owner and passes
the caller's two BPTRs, APTR buffer, and signed buffer size to the core.

[qualify_copyloop_native.ps1](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_copyloop_native.ps1)
builds resident HUNKs for 68000, 68020, and 68040 and executes eight supplied
direct vectors per CPU: empty input's required EOF write, multi-chunk success,
read failure, short write, failed write, Ctrl-C before reading, and interleaved
callers. The 2026-09-04 receipt has six reachable methods and no managed runtime
features/helpers, external native targets, exception regions, fatal machine
fault sites, resource leaks, or shared-image writes. Artifacts are 1,296 bytes
for 68000 SHA-256
`fadb8cb8487666955fa62636ddc5ab046ae0671c90178475d1c93c3a5b1c77e2`,
1,316 bytes for 68020 SHA-256
`69f6105d98b958eb6070758e9f19cf19386103d1d6edacc9412d603e49742a46`, and
1,292 bytes for 68040 SHA-256
`e931bcfc54322e1f15d8ca985e03f6bc2177a95d08b95286768007afcf057727`.
The vectors supply DOS results; they do not prove real handler behavior, a DOS
parser, packaging, full command behavior, or original-reference parity.

## Bounded final result policy

[NativeMorphOSCopyResultPolicy.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSCopyResultPolicy.cs)
implements the source's post-cleanup selection order: a Ctrl-C with no result
sets `ERROR_BREAK` and WARN; the non-QUIET, non-primary failure path calls
`PrintFault(IoErr(), NULL)`; a primary result overrides `RetVal2`; and ERRWARN
promotes only WARN to ERROR. Its private
[NativeMorphOSCopyResultPolicyEntry.cs](D:/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeMorphOSCopyResultPolicyEntry.cs)
and [qualify_copy_result_policy_native.ps1](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_copy_result_policy_native.ps1)
pass nine supplied public-DOS `IoErr`/`SetIoErr`/`PrintFault` vectors per CPU,
including Ctrl-C, QUIET, primary precedence, ERRWARN, and interleaved callers.
The resident HUNK is 1,048 bytes with SHA-256
`1065c8ffab9b31ba835cc61fd166a97c7aaa1f1112db97e29bd7e8a42dc11e93` on
68000/020/040, has five reachable methods, and uses no managed runtime
features/helpers, external native targets, exception regions, fatal fault
sites, fixture resource leaks, or shared-image writes. It does not attach this
policy to a complete Copy command lifecycle.

## Joined destination-open integration (static checkpoint)

[NativeMorphOSCopyOpenDestination.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSCopyOpenDestination.cs)
joins the bounded non-filesystem `OpenDestDir` fallback with the normal
filesystem prefix-creation path. It selects the non-filesystem lock only when
`IsFileSystem` rejects a Copy/MOVE destination; otherwise it preserves the
normal writable-prefix behavior and its `IoErr`. Its private
[NativeMorphOSCopyOpenDestinationEntry.cs](D:/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeMorphOSCopyOpenDestinationEntry.cs)
has a pure/resident receipt with eight reachable methods, zero managed runtime
features/helpers, external native targets, exception regions, and fatal fault
sites. Its [qualify_copy_open_destination_native.ps1](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_copy_open_destination_native.ps1)
receipt passes five supplied public-DOS vectors per CPU for non-filesystem
Copy/MOVE selection, filesystem prefix creation, CreateDir failure, and
interleaved callers. HUNKs are 2,748 bytes for 68000 SHA-256
`af3225f4f11931e640a30276384533e5996d3eeff613c6a058cf5eb3e3d4f395`,
2,772 bytes for 68020 SHA-256
`23c593c4c9a09d757e809480cab755a1fd8b453bbba9acea1d781afc11d5418f`, and
2,736 bytes for 68040 SHA-256
`5a7ae96533b80f41971109a46daf651b05bfd36f4f4c7a95d4febed4f13418ca`.
There are no fixture resource leaks or shared-image writes. Full destination
policy and command lifecycle behavior remain open.

## Bounded pattern classifier

[NativeMorphOSCopyPatternClassifier.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSCopyPatternClassifier.cs)
implements the release source's `IsMatchPattern` helper with caller-owned
AnchorPath storage. It initializes only the source-observed zero break mask,
`APF_DOWILD`, and zero string length; then returns one for `APF_ITSWILD`, zero
for a successful literal match, or minus one when `MatchFirst` fails. It calls
`MatchEnd` only after a successful `MatchFirst`.

The private
[NativeMorphOSCopyPatternClassifierEntry.cs](D:/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeMorphOSCopyPatternClassifierEntry.cs)
and
[qualify_copy_pattern_classifier_native.ps1](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_copy_pattern_classifier_native.ps1)
qualify five supplied `MatchFirst`/`MatchEnd` vectors per 68000/020/040 HUNK:
wildcard, literal, MatchFirst failure without MatchEnd, and interleaved callers.
Each HUNK has five reachable methods, no managed runtime features/helpers,
external native targets, exception regions, fatal fault sites, fixture leaks, or
shared-image writes. Artifacts are 1,040 bytes: 68000 SHA-256
`af610295c2a509f0c5a4ebec9522a6cf13b390dd2a79515048e652d9bab4550d`, and
68020/68040 SHA-256
`f355676f6d94264383a0b2090b07e9e321af776986bf3b6b6ca92d004f11f4ca`.
It does not allocate matcher state, perform PatCopy iteration, call DoWork, or
implement full Copy behavior.

## Bounded flat PatCopy traversal

[NativeMorphOSCopyFlatTraversal.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSCopyFlatTraversal.cs)
implements a directory-free regular-file lane of the source `PatCopy` matcher loop.
It reproduces the cleared AnchorPath header, `SIGBREAKF_CTRL_C`, and 2,048-byte
source path size; uses public `MatchFirst`, `MatchNext`, and `MatchEnd`; and
captures a source-sized path plus FIB snapshot per match in caller-owned work
records. A captured record becomes delivered only at the next iteration, or
after `MatchEnd` for the final record, matching the source's deferred DoWork
ordering. It returns the raw terminal matcher result for a later command-level
error policy.

The private
[NativeMorphOSCopyFlatTraversalEntry.cs](D:/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeMorphOSCopyFlatTraversalEntry.cs)
and
[qualify_copy_flat_traversal_native.ps1](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_copy_flat_traversal_native.ps1)
qualify six supplied MatchFirst/MatchNext/MatchEnd vectors per CPU: one and two
regular files, MatchFirst failure, MatchNext failure, and interleaved callers.
The receipt has six reachable methods and no managed runtime features/helpers,
external native targets, exception regions, fatal fault sites, fixture leaks, or
shared-image writes. This remains deliberately smaller than `PatCopy`: it does
not allocate the matcher, select/filter directories or PATTERN entries, move the
destination parent, perform DoWork, emit diagnostics, or select Copy's result.
HUNKs are 1,560 bytes for 68000 SHA-256
`dd906f293ba5a95e2188595767c3d95ea6a0ed42d3c5d965a995d8c482fca707`,
1,544 bytes for 68020 SHA-256
`2472ca95c7735fe0718162b94d6f8f3a62625bf98638b60efccba08927d2cbd6`, and
1,520 bytes for 68040 SHA-256
`d427a79bf6920188bfd4c95f23dc217edcae2608c2b90a01fb329f356b9713cd`.

## Bounded directory-exit transition

[NativeMorphOSCopyDirectoryExit.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSCopyDirectoryExit.cs)
implements the source `APF_DIDDIR` branch: it clears the flag, decrements
depth, schedules DELETE/MOVE deferred work, moves CurDest through `ParentDir`,
and unlocks only a non-destination CurDest. A null parent is reported to the
future loop owner, which is responsible for the source's next-iteration
diagnostic. The private
[NativeMorphOSCopyDirectoryExitEntry.cs](D:/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeMorphOSCopyDirectoryExitEntry.cs)
and [qualify_copy_directory_exit_native.ps1](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_copy_directory_exit_native.ps1)
pass six supplied ParentDir/UnLock vectors per CPU for no exit, destination-root
exit, transient MOVE exit, parent failure, and interleaved callers. The receipt
has six reachable methods with no runtime features/helpers, external native
targets, exception regions, fatal fault sites, leaks, or shared-image writes.
HUNKs are 1,304 bytes for 68000 SHA-256
`c2ce47d74d9c01ef57bd90fe5672ec4350982c9574a85dc2bd16498ce67e5b7d`, and
1,296 bytes for 68020/68040 SHA-256
`43e488a1ed6ed360203c72436d6312622d05f8534500ae7f3f435863dd2f719b`.
Directory entry, recursion, DoWork, diagnostics, and full command integration
remain open.

## Directory-entry static checkpoint

[NativeMorphOSCopyDirectoryEntry.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSCopyDirectoryEntry.cs)
now isolates the two directory-entry branches: a first directory sets
`APF_DODIR` without scheduling work; a later directory schedules deferred work
and sets `APF_DODIR` plus a next-iteration Deep increment only for ALL. Its
private root compiles as a five-method resident HUNK with no runtime
features/helpers, external native targets, exception regions, or fatal fault
sites: 68000 1,124 bytes SHA-256
`416814946a8e7693867c4c9f70723e5dfd054f7a84099dfe8f825b53154f6b83`, and
68020/68040 1,116 bytes SHA-256
`e8ade6d9b48dcb38a9559f25c4430852fdfd12789c8d67835c2c8c83eea46d4e`.
Runtime vectors, matcher integration, recursion, and object work remain open.

The static checkpoint above is superseded by the 2026-09-05 entry-decision
receipt at `artifacts/qualification-copy-directory-entry-native/qualification.json`.
The helper now requires `entryAllowed`, supplied by the caller's soft-link
check. A denied ordinary ALL entry still schedules object work but does not
set DODIR or schedule a depth increment. First-directory entry remains
independent of this check, matching the separate original branch. The caller
must set VERBOSE there and must route DIDDIR to the exit path first.
Seven native invocations pass on each of 68000/68020/68040, including flag
preservation and interleaved allowed/denied entries, with no image writes.
The actual soft-link check, streaming matcher integration and DoWork remain open.

### Directory-transition authority

Both directory adapters are constrained to the hash-bound MorphOS 3.20 source
member recorded above: `c/copy/copy.c` lines 1266-1285 select first-directory,
APF_DIDDIR, and DELETE/MOVE deferred-work behavior; lines 1317-1380 cover
ordinary-directory entry including the soft-link entry check; lines
1287-1301 define ParentDir and non-destination unlock ownership. They do not
authorize the omitted soft-link check, `SetData`, diagnostic, or DoWork ranges.

## Corrected native validation (2026-09-05)

### Soft-link probe implementation checkpoint

`NativeMorphOSCopySoftLinkCheck.CanEnter` implements the ordinary ALL-entry
probe from source lines 1324-1370: CurrentDir, shared Lock, ObjectNotFound-only
GetDeviceProc, 512-byte public allocation, ReadLink with size 511, optional
warning, FreeMem/FreeDeviceProc, saved IoErr restoration and CurrentDir restore.
Only a positive ReadLink denies entry. The caller supplies eight bytes for
warning arguments; no mutable shared storage is introduced.
The private root builds and compiles to resident HUNK for 68000 (1396 bytes)
and 68020/68040 (1392 bytes). Reports in `artifacts/copy-softlink-static/`
show five reachable methods and zero runtime features/helpers, external native
targets, exception regions and fatal fault sites. These are static evidence
only: runtime ABI, warning output, allocation/device failures, cleanup order
and interleaved invocation validation remain required before integration.

Runtime follow-up: `artifacts/qualification-copy-softlink-native/qualification.json`
now records 9 passing invocations per CPU (68000/68020/68040), with no leaked
allocations or shared-image writes. Cases cover a successful target lock,
non-ObjectNotFound error, missing device, negative ReadLink, dangling link,
QUIET, allocation failure and interleaved warning/quiet callers. The fixture
checks public vector arguments, warning format and substitution pointers,
release counts, current-directory restoration and saved IoErr after cleanup
deliberately overwrites it. This closes the bounded probe's supplied-vector
runtime checkpoint; integration and original-system execution remain open.

`NativeMorphOSCopyDirectoryEntry.ProcessMatched` now connects that probe to
the directory-entry transition. It reads the live AnchorPath current AChain
lock and FIB filename using SDK offsets, performs the probe for ordinary ALL
entries, and forwards its result to DODIR/deferred-depth selection. First
entries set COPYFLAG_VERBOSE and bypass probing; non-ALL entries bypass it.
The integrated native root exercises ordinary ALL entries with the same nine
failure/success/interleaving cases and additionally checks object work remains
scheduled when descent is denied. All 27 invocations pass without leaks or
image writes; receipts are in
`artifacts/qualification-copy-matched-directory-native/qualification.json`.
This receipt covers the ordinary ALL integration; first/non-ALL integration,
matcher iteration, directory-exit integration and actual DoWork remain open.

Directory-exit integration follow-up: `ProcessMatched` connects the exit
transition to ENTERSECOND, destination path-cache invalidation, and SetData
through `NativeMorphOSCopyMetadata`. COPY/MOVE attribute calls run with the
new parent destination as CurrentDir; the previous directory is restored
even when SetProtection returns failure. The upgraded exit root's six cases
pass on each CPU, including interleaved COPY/MOVE and failed ParentDir.
The fixture checks ARCHIVE removal, attribute-call directory context,
restoration, ownership, flags and path length. Receipt:
`artifacts/qualification-copy-directory-exit-integrated/qualification.json`.
This supersedes earlier exit-root hashes and method count (now nine).
The full matcher loop and actual DoWork remain open.

### Combined matcher-body checkpoint

`NativeMorphOSCopyMatchStep.Process` now implements source-ordered selection
after the previous pending DoWork: pending-depth increment, ENTERSECOND clear,
one reusable 2048-byte path/FIB snapshot, first-directory priority, directory
exit, ordinary-directory probing, and public DOS MatchPatternNoCase for files.
It preserves the parent-failure early continue and first-state update point.
The caller still owns matcher iteration, DoWork invocation and final cleanup.
The private 80-byte-control root builds without warnings and compiles resident
on 68000 (4012 bytes), 68020 (4004 bytes) and 68040 (3984 bytes), with reports
in `artifacts/copy-match-step-static/`. Combined runtime transitions remain
unverified; the individual adapter receipts do not prove this new root.

Runtime follow-up: the combined root now passes 11 invocations per CPU in
`artifacts/qualification-copy-match-step-native/qualification.json`. Cases
check full 2048-byte path and FIB snapshots, pending-depth advancement,
ENTERSECOND clearing/setting, first-directory precedence over DIDDIR,
destination-free DELETE exit, nonrecursive and recursive directories, public
pattern acceptance/rejection, and interleaved callers. Fixture workspaces use
65536-byte spacing to isolate both callers' 8192-byte regions. All three CPU
runs report no leaks or shared-image writes. Matcher iteration, previous/final
DoWork execution and error/cleanup lifecycle are still required.

### Filesystem matcher-loop implementation checkpoint

`NativeMorphOSCopyTraversal.RunFileSystem` surrounds the match step with
source-ordered MatchFirst/MatchNext/MatchEnd, one allocated AnchorPath,
deferred worker dispatch, parent-error handling, ERRWARN threshold, final
worker dispatch, diagnostics and resource release. Invocation state is passed
by reference; the generic worker owns actual DoWork semantics. The original
allocation-failure diagnostic expression is preserved (it never prints).
The command still must compute `first`, route non-filesystem sources and
provide the complete production object worker.

The private root uses a reporting worker solely to expose dispatch ordering.
It builds without warnings and compiles resident for 68000 (5124 bytes),
68020 (5136 bytes) and 68040 (5108 bytes), with static reports under
`artifacts/copy-traversal-static/`. Native runtime ordering, state mutation
through the worker interface and error/cleanup paths remain unverified.

Runtime follow-up: `artifacts/qualification-copy-traversal-native/qualification.json`
records eight passing invocations per CPU. A reporting worker checks the
previous path/FIB identity at deferred dispatch and final dispatch after
MatchEnd. Cases cover two/one/zero files, initial matcher error, MatchNext
error, allocation failure, and interleaved success/error callers. Diagnostics
are checked after MatchEnd and preserve the original error across a poisoned
VPrintf result. The worker's SecondaryResult guard suppresses pending action
after matcher failure. All runs release allocated anchors without image writes.
Worker-driven result/threshold changes, recursive multi-step sequences,
initial classification, non-filesystem routing and production DoWork remain open.

Worker-result follow-up: the private reporting worker now accepts synthetic
primary/secondary outcomes from its vector, exercising actual state writes
through the generic worker interface. Added warning-continues, error-stops,
ERRWARN warning-stops and secondary-error-stops cases. The source's current
iteration still snapshots/selects after deferred work changes the result;
the loop then performs MatchNext before rechecking its condition. Pending
final DoWork is invoked for a primary or secondary stop, but the original
DoWork guard suppresses the object action in both cases. The initial reporting
fixture omitted the primary guard; the corrected receipt below supersedes it.
All 12 invocations pass per CPU, without leaks or image writes, in
`artifacts/qualification-copy-traversal-thresholds/qualification.json`.
These updated reporting-root hashes supersede the earlier lifecycle root.

Guard correction: source DoWork lines 1676-1684 guard both the primary
threshold and secondary error. The reporting worker now mirrors both, and
the stopping cases require only one actual action, although the loop still
invokes final DoWork. All 12 cases pass per CPU in
`artifacts/qualification-copy-traversal-work-guard/qualification.json`,
superseding the threshold receipt's primary-stop action claim.

`NativeMorphOSCopyWorkPreparation.Prepare` begins the production worker's
prelude: the same two guards, DELETE/non-filesystem destination bypass,
NameFromLock cache population, truncation at cached parent length and AddPart.
It preserves the original ignored AddPart result and NameFromLock-failure
secondary result plus null unlock. DestinationName is invocation-owned.
This new helper builds but is not yet connected to production object actions
or qualified by native runtime vectors.

Path-prelude runtime follow-up: 12 invocations per CPU now pass in
`artifacts/qualification-copy-work-preparation-native/qualification.json`.
Coverage includes cache population, previous-name truncation, NameFromLock
failure and null unlock, ignored AddPart failure, DELETE/device bypass,
normal-warning allowance, both error guards, ERRWARN, and interleaved callers.
The fixture verifies the 2048-byte API capacities and exact pointer/lock
arguments. All runs report no leaks or shared-image writes. This qualifies
the prelude only; complete object operations remain required.

### File-pair open-order correction

DoWork source lines 2017-2068 open the destination before the source and
remove that destination if the source open fails. The earlier file-pair
adapter and fixture used the reverse order. Both are corrected: a failed
destination open never attempts the source, and failed source open closes
and removes the already-created destination. Nine invocations pass per CPU
in `artifacts/qualification-copy-file-pair-open-order/qualification.json`,
with no leaked handles or shared-image writes. This supersedes earlier
file-pair open-order descriptions and binary hashes. Full CopyFile behavior,
source-lock release in DoWork, metadata and mode integration remain open.

### CopyFile examination and buffer implementation

`NativeMorphOSCopyFileTransfer.Run` adds the source CopyFile behavior absent
from the bounded streaming loop: invocation-owned cached buffer reuse,
allocation retries by halving down to 512 bytes, ExamineFH stream fallback,
examined-size termination and premature-EOF failure. The MorphOS 51.66+
ExamineFH64 branch requests POSIX dates and tracks the 64-bit size with two
32-bit words; the caller must verify the library version before enabling it.
The default uses Kickstart 3.1 ExamineFH. Buffer ownership remains with the
command, and Ctrl-C is the only explicitly synthesized handler error.
The private root builds and compiles resident for all three CPUs, with
reports in `artifacts/copy-file-transfer-static/`. Allocation, examination,
64-bit carry and EOF runtime coverage remain required before integration.

Transfer runtime follow-up: 14 native invocations pass per CPU in
`artifacts/qualification-copy-file-transfer-native/qualification.json`.
Coverage includes 2048/1024/512-byte allocation retry and exhaustion,
cached-buffer reuse, ExamineFH/ExamineFH64 arguments and extension tags,
size-bound termination without an extra EOF read, zero-length file behavior,
premature EOF, stream fallback, Ctrl-C, read and short-write failure, and
interleaved callers. An extended size above 4 GB verifies the high word is
used; it does not exercise carry through the 4 GB boundary. All runs release
the root-owned cache and report no shared-image writes. Carry-boundary,
real-handler and full worker integration remain open.

### Integrated file-operation implementation

`NativeMorphOSCopyFileOperation.Run` connects the full transfer routine to
DoWork's destination-first opens, filesystem source-lock release before
opening input, output-before-input close order, MOVE source deletion and
FORCEDELETE protection clearing. Failed transfer/source-open removes the
destination; successful transfer followed by failed MOVE deletion retains it.
KillFileKeepErr captures IoErr after close calls, matching the original;
the older bounded FilePair's earlier error capture is not used by this path.
Direct-device callers skip source-lock release. Destination policy,
diagnostics and metadata remain owned by the surrounding DoWork coordinator.
The private root builds and compiles resident for all three CPUs, with
reports at `artifacts/copy-file-operation-static/`. Integrated runtime
handle/lock ownership, deletion and error-order checks remain required.

File-operation runtime follow-up: nine invocations pass per CPU in
`artifacts/qualification-copy-file-operation-native/qualification.json`.
Cases cover COPY, destination/source open failure, failed transfer, MOVE,
forced MOVE deletion failure, direct-device ownership and interleaved callers.
Assertions check source unlock between opens, close ownership/order,
source-versus-destination deletion, preservation of the completed destination
after failed MOVE deletion, forced protection clearing and post-close IoErr
preservation during destination cleanup. Root-owned cache is freed and no
shared-image writes occur. Full DoWork selection/diagnostics remain open.

### Copy LinkFile operation

`NativeMorphOSCopyLinkOperation.Run` implements hard-link creation from the
caller's BPTR and soft-link creation from a NameFromLock device-qualified
path. Soft links allocate/free a 2048-byte MEMF_ANY buffer; neither branch
takes ownership of the source lock. All eight native invocations pass per
CPU in `artifacts/qualification-copy-link-operation-native/qualification.json`,
covering both modes, allocation/name/link failures and interleaved callers.
Tests check target representation, mode flags and temporary-buffer cleanup.
DoWork still must enforce directory FORCELINK and regular-file soft-link
restrictions before selecting this operation.

### TestDest cleanup-order correction

Source TestDest releases the target lock before mutation but keeps its FIB
until after classification/deletion. The earlier adapter freed the FIB too
soon and restored pre-cleanup errors. It now follows source ordering and
returns the observed post-cleanup error (except CANTDELETE explicitly sets
ObjectExists). Twelve native invocations pass per CPU in
`artifacts/qualification-copy-destination-cleanup-order/qualification.json`,
including a cleanup routine that changes IoErr after failed deletion. This
supersedes earlier destination-root hashes and error-preservation claims.
Extended examination and full directory-coordinator integration remain open.

### Directory-operation coordinator implementation

`NativeMorphOSCopyDirectoryOperation.Run` now connects TestLoop and TestDest
to DoWork's directory branch. ALL enters an existing directory or creates,
unlocks and re-locks a new directory; failed entry restores the previous
destination, while success releases only a non-root previous destination.
It resets the destination path cache and sets original WARN/ERROR outcomes.
Non-ALL MOVE renames; LINK enforces FORCELINK and selects hard/soft creation;
plain COPY returns display-only. Typed outcomes retain the distinct diagnostic
branches for the surrounding worker. The source lock stays caller-owned.
The private root builds and compiles resident on all three CPUs, with static
reports at `artifacts/copy-directory-operation-static/`. Combined runtime
branches, diagnostics, and complete DoWork integration remain open.

Directory coordinator runtime follow-up: 14 invocations pass per CPU in
`artifacts/qualification-copy-directory-operation-native/qualification.json`.
Cases cover creation/entry, creation and re-lock failures, prior-lock
restoration, release of transient previous destinations, MOVE success/failure,
FORCELINK rejection, hard-link success/failure, display-only COPY and immediate
loop rejection, plus interleaved creation/failure. All runs release fixture
FIB allocations and preserve the shared image. Root-destination transitions,
soft-link policy combinations, recursive multi-step behavior and complete
DoWork diagnostics still require integrated verification.

### Worker output implementation

`NativeMorphOSCopyOutput` implements source PrintName/PrintNotDone for
USE_ALWAYSVERBOSE=1: three leading spaces, eight per additional depth level,
five directory spaces, `(Dir)` suffix, file progress dots, and Flush(Output()).
PrintNotDone emits only the operation prefix and obtains IoErr afterward,
matching the original build's disabled name-printing branch. VPrintf arguments
are invocation-owned. The root builds and compiles resident on all three CPUs
with reports at `artifacts/copy-output-static/`. Exact output and flush/error
ordering runtime checks remain required before worker integration.

Output runtime follow-up: seven invocations pass per CPU in
`artifacts/qualification-copy-output-native/qualification.json`. Exact text
checks cover zero/one/three-level file indentation, directory suffix/spacing,
progress dots and the error prefix. The fixture checks Flush(Output()) after
complete progress text and forces VPrintf to change IoErr, proving PrintFault
uses the post-prefix value. Interleaved directory/error callers preserve
their argument storage without image writes. Full worker integration remains open.

### Combined DoWork implementation checkpoint

`NativeMorphOSCopyWork` now implements INativeMorphOSCopyWork and connects
destination preparation, direct-device transfer, source/parent lock checks,
DELETE/second-visit removal, directory outcomes, file TestDest, MOVE rename
fallback, regular-file LINK restrictions, transfer, reporting, DONE and
metadata. Invocation state now owns the cached transfer buffer and configured
buffer size; the command must release that cache at exit. The worker retains
source QUIET/VERBOSE branch behavior, including its unusual success-tail
selection when an error is quiet. It does not claim reference runtime parity.
The combined root builds and compiles resident on 68000/68020/68040, with
reports at `artifacts/copy-work-static/`. Full-worker native vectors,
traversal-to-worker integration, command startup/arguments and packaging are
still required. Earlier individual receipts cannot establish this root's
runtime correctness.

Depth fidelity follow-up: source CopyData.Deep is UBYTE. MatchStep increments
and directory-exit decrements now wrap modulo 256, rather than retaining an
unbounded signed value. Added 255-to-0 entry and 0-to-255 exit cases to the
combined match-step suite. All 13 invocations pass per CPU in
`artifacts/qualification-copy-match-step-byte-depth/qualification.json`,
superseding earlier MatchStep/exit-dependent hashes. Combined DoWork runtime
and traversal integration remain open.

Combined-worker runtime follow-up: 11 invocations pass per CPU in
`artifacts/qualification-copy-work-native/qualification.json`. The actual
NativeMorphOSCopyWork root covers quiet COPY success/read failure, source-lock
and parent failure, DELETE success/failure/FORCEDELETE, directory first and
second passes, and interleaved COPY/DELETE. Checks include source/parent
unlock ordering, destination construction, transfer ownership and result/DONE
flags (including the original quiet-error tail). No buffer leaks or image
writes occur. MOVE/LINK/direct-device branches, visible diagnostics, metadata
and full traversal integration are not proven by this receipt.

Combined mode follow-up: the worker suite now also covers MOVE rename,
failed-rename fallback to transfer plus source deletion, regular-file hard
links, regular-file soft-link rejection, and direct non-filesystem output.
All 16 cases pass per CPU in
`artifacts/qualification-copy-work-modes/qualification.json`; assertions
check mode-specific Rename/MakeLink selection and direct-output lock bypass.
Visible diagnostics, metadata, recursive sequences and traversal-to-worker
integration remain open; this is not shipping-command qualification.

### Matcher-to-worker connection

The non-generic `NativeMorphOSCopyTraversal.RunFileSystem` now instantiates
NativeMorphOSCopyWork and runs it through the streaming matcher. The shared
invocation state carries destination cache, results, flags, metadata and
the transfer cache across matches. A combined native root supplies caller-owned
snapshots and frees the transfer cache after traversal; traversal owns its
AnchorPath and transient destination cleanup. All three resident variants
compile, with static reports in `artifacts/copy-traversal-work-static/`.
This connection requires runtime sequence tests; the earlier independent
matcher and worker suites do not prove their interaction. Command argument
and initial-source classification, non-filesystem routing, reference execution
and packaging remain required.

Matcher/worker runtime follow-up: five sequences pass per CPU in
`artifacts/qualification-copy-traversal-work-native/qualification.json`:
empty, one-file, two-file and interleaved one/two-file traversals. These run
actual DoWork and check byte payloads, deferred/final opening order,
destination-cache truncation/reuse, a single transfer-cache allocation across
files, source/parent ownership and final cache/anchor release. All runs
preserve the shared image. Recursive traversal, error sequences, visible
output, metadata and complete command startup/arguments remain open.

The non-filesystem destination fallback now passes a real empty string to
`Lock`, using the copied destination's terminating byte. Earlier code passed
NULL, which the fixture incorrectly treated as equivalent. Both standalone
and integrated OpenDest fixtures now reject NULL. Corrected receipts are in
`artifacts/qualification-copy-nonfilesystem-corrected/qualification.json` and
`artifacts/qualification-copy-open-destination-corrected/qualification.json`:
8 and 5 invocations respectively pass on each of 68000, 68020, and 68040.
These receipts supersede earlier binary hashes for these two roots.

The flat traversal fixture previously overlapped its output records with the
AnchorPath path buffer. Records now begin at offset 4096 in a 12288-byte
invocation workspace, and a new two-entry case uses 1804/1904-byte paths.
All 7 invocations pass on each CPU, with no shared-image writes, in
`artifacts/qualification-copy-flat-traversal-corrected/qualification.json`.
This validates snapshot capture only. It does not prove deferred DoWork
ordering or implement the production streaming traversal lifecycle.

### PatCopy entry routing checkpoint (2026-09-05)

`NativeMorphOSCopyTraversal.Run` now composes the existing classifier and filesystem traversal with the actual worker's non-filesystem path. The caller supplies writable terminated source names, sufficient Path storage and separate invocation-owned classifier AnchorPath scratch. The frontend must enforce the path-size precondition. Failed wildcard classification does not select old directory syntax. Device probing temporarily terminates after the colon and restores the suffix before dispatch. Non-COPY modes bypass source-device probing; COPY stream sources set SRCNOFILESYS only around direct DoWork(FilePart(name)).

Dedicated root: `tests/Commands.AddBuffersNativeRoot/NativeMorphOSCopySourceEntry.cs`; 68-byte private control record, classifier scratch pointer at offset 64, remaining fields inherited from the traversal-work fixture (offset 28 unused). Native compilation passes for all three CPUs with 32 reachable methods. Reports: `artifacts/copy-source-routing-static/`. Runtime routing qualification is still required, including classifier failure, literal/wildcard directories, no-colon names, device suffix restoration, direct stream errors, flag cleanup and interleaved scratch ownership. No original runtime, full command, packaging or purity gate is established by this checkpoint.

### Source routing runtime checkpoint (2026-09-05)

The new source-entry fixture executes eight cases on each of 68000/020/040 through the compiled actual worker. It validates literal/wildcard/classification-failure and relative-name routing, restored device prefixes, source-copy-before-FilePart ordering and successful direct NET: input without matcher-anchor allocation. Filesystem interleaving retains independent scratch and caches. All cases have no leaked resources or shared-image writes. The older traversal fixture passes five cases per CPU as a regression control. Receipt: `artifacts/qualification-copy-source-routing-native/qualification.json`. These are supplied-vector cases, not original runtime comparisons. Literal directory-first behavior, direct-stream failures/interleaving, mode-specific classifier bypass, full ReadArgs/lifecycle composition and shipping qualification remain open.

### Argument admission correction (2026-09-05)

The original positional TO normalization covers COPY, MOVE and LINK; only DELETE and MAKEDIR bypass it. The old gate's default-COPY-only condition was incorrect and has been fixed. The original DIRECT validation also allows multiple DELETE sources, while DIRECT COPY requires exactly one source after positional TO normalization. Five new admission cases cover those differences. All 18 cases per CPU pass in `artifacts/qualification-copy-argument-gate-positional/qualification.json`, retaining exact template parsing and interleaved cleanup checks. This supersedes earlier conflicting descriptions of these two rules. Admission still does not mutate/retain the actual source vector for execution; that belongs to the pending frontend composition.

### Parser ownership integration API (2026-09-05)

`NativeMorphOSCopyArgumentGate.Read` now returns invocation-owned `NativeCommandArguments` without releasing a successful parse. `Validate` and its result reads borrow that lease by reference. The legacy `Run` wrapper retains its prior immediate-cleanup behavior. A parsed but rejected invocation still owns a lease and must release it; parser failure returns an empty lease safe to release. This enables the future command frontend to keep FROM/TO/PATTERN data alive across operations without reparsing.

All 18 existing gate cases pass per CPU using the new wrapper composition; receipt `artifacts/qualification-copy-argument-gate-lease/qualification.json`. This proves regression behavior and immediate cleanup only. Retained lease use across filesystem work, original explicit RDArgs/extended help, requester restoration and complete command teardown remain unqualified.

### Retained option setup implementation (2026-09-05)

`NativeMorphOSCopyOptionSetup.Apply` maps an admitted live parse to operation state and borrowed options. It provides the empty current-directory source using twelve caller-owned bytes, removes the final positional TO from the retained FROM vector, maps original flags and metadata flags, and preserves unsigned buffer-size arithmetic after the signed positive-value check. `SelectVerbosity` performs the original post-normalization classifier scan unless QUIET. Requester suppression is reported as an option, not applied here. Initial secondary result stays FAIL until the operation branch explicitly admits work, matching source initialization.

`NativeMorphOSCopyOptionSetupEntry` compiles the parser/setup combination for all three CPUs. Reports: `artifacts/copy-option-setup-static/`. No runtime setup receipt exists yet. Full command sequencing must still preserve the original pre-rejection verbosity/requester side effects; the admission-first private root is not an end-to-end parity claim. The metadata helper's comment-before-date order remains a known difference from original SetData and must be repaired.

### SetData order repair (2026-09-05)

The previously recorded comment-before-date difference is fixed. NativeMorphOSCopyMetadata now invokes protection, then the selected date API, then SetComment, retaining source behavior of ignoring operation return values. Native gateway assertions enforce order as well as arguments and counts. Eight cases per CPU pass in `artifacts/qualification-copy-metadata-order/qualification.json`. The earlier `artifacts/qualification-copy-metadata/copy-metadata-68000.hunk` fails the new fixture at premature SetComment, confirming the ordering assertions reject the old implementation. Broader worker and command receipts compiled before this change do not qualify the new metadata body.

### TestLoop null-lock fidelity correction (2026-09-05)

The loop guard now follows the source do/while semantics and final lock-identity test exactly: initial NULL is passed to SameLock when SameDevice succeeds, and exhausted ancestry can issue UnLock(NULL). Previous null-skipping behavior is superseded. Native tests cover both initial-null outcomes, root exhaustion and source/destination lock arguments alongside prior device/ancestor/interleaving cases. All eight cases pass per CPU in `artifacts/qualification-copy-loop-guard-null/qualification.json`. Broader recursive integration remains open and prior combined artifacts have not been requalified for this change.

### Option setup runtime checkpoint (2026-09-05)

Eight native setup cases per CPU now verify positional source counts, the omitted-source default, mode/operation and metadata flags, default buffer size, DIRECT/NOREQ intent and interleaved retained parser ownership during literal classification. QUIET alone leaves requester suppression false. Receipt: `artifacts/qualification-copy-option-setup-native/qualification.json`. The prior 18-case argument-gate matrix passes as a regression control. This remains a private parser/setup root; it does not execute files, verify all retained pointer values, exercise numeric BUFFER overrides or establish the original full lifecycle/side-effect order.

### DIRECT operation composition (2026-09-05)

NativeMorphOSCopyDirect implements the original dedicated branch rather than routing through normal DoWork. DIRECT COPY uses input-first opens and retains partial output after transfer failure; failed opens leave the initialized secondary result untouched. DIRECT DELETE visits all sources without stopping on individual deletion failure and finishes with secondary OK. The caller retains responsibility for cache release and command finalization. The helper requires admitted COPY/DELETE options.

The private NativeMorphOSCopyDirectEntry now holds the parser lease through this operation and frees the transfer cache before releasing it. Reports under `artifacts/copy-direct-static/` prove three-CPU resident compilation only. The root rejects non-DIRECT requests as a fixture restriction, not as the intended command behavior. Full command dispatch must implement every other branch, original pre-admission side effects, extended help and final result policy. New DIRECT runtime cases remain required.

### DIRECT runtime checkpoint (2026-09-05)

Seven native cases per CPU now exercise the retained parser through actual DIRECT operations. Success verifies payload; failed input/output opens verify ownership; a failed write verifies that no DeleteFile targets partial output; failed deletes verify continuation across the source list. Close gateways return failure and change IoErr while the transfer secondary result remains as observed by the private root. Interleaved COPY/open-failure callers retain independent parser/cache storage. Receipt: `artifacts/qualification-copy-direct-native/qualification.json`. Eight setup cases also pass as regression controls. The root still reports private fixture results, not the full original command finalization/error output, and no real filesystem handler or shipping gate is qualified.

### Normal PATTERN and DELETE composition (2026-09-05)

NativeMorphOSCopyPatternSetup now owns normal-operation pattern preparation and explicit release. Omitted PATTERN proceeds without allocation; empty PATTERN sets BadTemplate; nonempty PATTERN allocates 2*length+3 bytes with MEMF_ANY and invokes ParsePatternNoCase. Failed parse frees its allocation and does not restore an earlier IoErr over cleanup. Successful storage survives every source traversal. The DELETE source loop resets secondary to OK once and tests only primary against ERRWARN's threshold, leaving per-worker secondary guards intact.

The private NativeMorphOSCopyPatternDeleteEntry compiles for all CPUs with reports in `artifacts/copy-pattern-delete-static/`. Native runtime/filter/error coverage and composition with retained ReadArgs remain pending. The root's supplied-vector entry is not the shipping command. Full COPY/MOVE/LINK destination routing, MAKEDIR dispatch, requesters and command finalization remain required.

### Parsed normal DELETE chain (2026-09-05)

The new private NativeMorphOSCopyParsedDeleteEntry retains ReadArgs through option normalization, verbosity classification, pattern preparation and all normal DELETE source traversals. Scratch layout is 12-byte default source, 282-byte classifier anchor, 2048-byte snapshot, 260-byte FIB and eight-byte formatting arguments, totaling 2610 bytes. Pattern/scratch cleanup precedes parser release. No static mutable command state was introduced.

Three-CPU compilation reports are in `artifacts/copy-parsed-delete-static/`. This root is a runtime qualification target, not a published command. Execution tests must still verify matched-file deletion, filter rejection, multiple sources, failure thresholds, recursion, cleanup and parser lifetime. Its control record and final result reporting remain private fixture conventions; shipping launch, original finalization and every other mode remain required.

### Parsed normal DELETE runtime checkpoint (2026-09-05)

The parsed root now passes six supplied-DOS cases on each CPU, including actual matched-file deletion across one/two source patterns, failed deletion continuation, ERRWARN primary-threshold stopping and interleaving. Native callbacks enforce live ReadArgs ownership, final deletion after MatchEnd, and release of parent/source locks before DeleteFile. The private control record offset 24 reports primary result rather than omitted-input state, making failure propagation observable. Receipt: `artifacts/qualification-copy-parsed-delete-native/qualification.json`. Seven DIRECT cases also pass as regression controls. This does not qualify PATTERN filtering, recursive paths, visible output, original handlers or shipping finalization.

### Parsed DELETE pattern runtime checkpoint (2026-09-05)

Ten cases now pass per CPU, adding normal PATTERN acceptance/rejection and invalid/empty input to the parsed DELETE chain. The fixture checks exact allocation size/flags, parser and matcher vector arguments, compiled-pattern reuse across sources, no deletion for rejected files, no traversal on preparation failure, and balanced cleanup. Receipt: `artifacts/qualification-copy-parsed-delete-pattern/qualification.json`. Pattern grammar itself is supplied by the fixture; real DOS parsing, allocation-failure IoErr, mixed accept/reject sequences, recursive deletion and shipping lifecycle still require qualification.

### State completion stage (2026-09-05)

NativeMorphOSCopyResultPolicy.Complete now accepts operation state, performs the original conditional Ctrl-C observation, selects/reports the final result and releases the transfer cache afterward. It clears freed cache ownership fields for the invocation. Callers must invoke it only after pattern/parser/requester cleanup and before freeing command state or closing DOS.

Nine native cases per CPU pass in `artifacts/qualification-copy-completion-native/qualification.json`, including real SetSignal queries guarded by secondary OK. No case allocates a transfer cache yet; this receipt does not prove cache-release ordering or full command finalization. Original explicit RDArgs allocation/free and extended-help remain missing from the generic retained parser path, which also preserves IoErr during its own cleanup. Those differences must be resolved before shipping parity.

### Allocated-cache completion runtime (2026-09-05)

Sixteen cases per CPU now cover final selection both with and without a 512-byte transfer cache. The private completion root uses control flag bit 8 to allocate the cache and verifies cleared ownership afterward. Diagnostics must occur before FreeMem; the fixture deliberately changes IoErr during cleanup and observes that post-cleanup value. Allocation/free counts exclude leaks or repeated frees. Receipt: `artifacts/qualification-copy-completion-cache/qualification.json`. This supersedes the prior no-cache-only limitation for the isolated stage. Full command/parser/requester finalization and original handler comparisons remain open.

### Parsed MAKEDIR implementation (2026-09-05)

NativeMorphOSCopyMakeDirectory now implements the source-list branch, including ParsePattern allocation/free, wildcard/error state and the original continued attempt for the current source after classification failure. OpenDirectory includes filesystem prefix creation, reporting, state transitions, existing-directory notification and returned-lock ownership. It intentionally has no non-filesystem COPY/MOVE fallback; that remains part of full destination routing. Existing TestDest is reused and still requires extended-examine integration.

Private parsed root: `NativeMorphOSCopyParsedMakeDirectoryEntry`. All three CPUs compile resident; reports in `artifacts/copy-parsed-makedir-static/`. Native execution, prefix/output failure cases, multiple-source termination and actual command finalization remain open. This is Copy's MAKEDIR mode and does not qualify the separate external MakeDir command.

### Parsed MAKEDIR runtime checkpoint (2026-09-05)

Seven native invocations per CPU now verify retained ReadArgs through one/two directory operations, failed CreateDir, failed/wildcard ParsePattern and interleaved calls. Classification errors still reach the current OpenDirectory operation and then stop further sources. All parser/created/final lock ownership checks pass without leaks or image writes. Receipt: `artifacts/qualification-copy-parsed-makedir-native/qualification.json`. Ten parsed DELETE cases pass as regression controls. This remains a quiet supplied-vector subset; existing targets, nested prefixes, visible diagnostics, original handlers and full command finalization remain unqualified.

### Stateful directory destination dispatch (2026-09-05)

NativeMorphOSCopyOpenDestination now has a stateful overload composing filesystem creation/reporting and original COPY/MOVE-only stream routing. DESNOFILESYS is set and the destination is copied before Lock of an actual empty string, including lock failure. RunDirectorySources enforces original primary/secondary/DEST_FILE admission, opens one root destination, checks sources and Ctrl-C in source order, runs the actual worker traversal and releases the root afterward. Cleared lock fields prevent accidental reuse after release.

Private source-vector root: NativeMorphOSCopyDirectorySourcesEntry. Three-CPU compilation reports: `artifacts/copy-directory-sources-static/`. This is only the directory-target branch after destination classification; normal single-file classification/dispatch, retained ReadArgs composition and native runtime tests remain required. It does not qualify all COPY/MOVE modes or replace the separate existing bounded helper receipts.

### Directory-target runtime checkpoint (2026-09-05)

Five cases per CPU now execute destination creation, source routing and actual COPY work with zero/one/two matches and interleaving. Root/created directory lock counts and release order are checked alongside payload and per-file resources. Receipt: `artifacts/qualification-copy-directory-sources-native/qualification.json`. Eight source-routing cases pass as regression controls. This covers a single source pattern per invocation and a newly created filesystem destination only. Multiple source patterns, existing targets, stream destinations, cancellation, MOVE, full parsing/finalization and original runtime comparisons remain required.

### Normal target-dispatch implementation (2026-09-05)

NativeMorphOSCopyTargetDispatch now joins destination classification to single-file or directory processing. It implements the original PathPart decision, source wildcard checks, existing destination/source examination, parsed tick-quoted source replacement after a successful lock, parent opening, DoWork dispatch and non-filesystem source fallback. It preserves the original distinction between a lock variable's last value and lock ownership in fallback selection. Examine64 tags temporarily use Path workspace before source-path population; callers must authorize the extension only on a supporting DOS version.

Private root NativeMorphOSCopyTargetDispatchEntry compiles resident for 68000/020/040; reports: `artifacts/copy-target-dispatch-static/`. This is compilation evidence only. Every target selection/failure branch, actual examined extended layout, quoted-name path, stream fallback and parser/full-command composition remains subject to runtime and original-reference qualification. No shipping gate is closed.

### Unified operation root (2026-09-05)

NativeMorphOSCopyOperations now dispatches every admitted operation mode and owns normal PATTERN cleanup plus the nothing-processed notice. The notice uses the verbosity established from arguments before traversal, preserving its distinction from subsequently set state flags. NativeMorphOSCopyParsedOperationsEntry holds ReadArgs across that shared dispatcher and supplies separate 2048-byte source and destination buffers within 4658-byte invocation storage.

All CPUs compile resident; reports: `artifacts/copy-parsed-operations-static/`. This does not establish runtime parity for the unified root or qualify previously untested target branches. The private root still returns fixture-style status, frees its cache before generic parser release and does not implement original explicit RDArgs/help, requester restoration or source finalization. It must not be packaged as the finished command.

### Unified operation runtime checkpoint (2026-09-05)

All three CPUs now execute DELETE (10), MAKEDIR (7) and DIRECT (7) matrices against the same compiled unified entry: 24 invocations per CPU. Each matrix retains its operation/ownership assertions while selecting the unified 4658-byte workspace. Unified DELETE also checks the source-observed metadata tail to the empty destination string; the smaller prior branch root had a null destination pointer and did not exercise that call. Receipt: `artifacts/qualification-copy-unified-operations/qualification.json`. These matrices do not cover normal COPY/MOVE/LINK destination selection, original parser/help, requesters or shipping finalization.

### Target classification runtime checkpoint (2026-09-05)

Seven native cases per CPU now exercise target ParsePattern/PathPart selection before the verified directory-target COPY path, plus wildcard/invalid rejection without filesystem work. Rejected patterns retain secondary FAIL and the wildcard case sets primary ERROR. Receipt: `artifacts/qualification-copy-target-dispatch-native/qualification.json`. Five directory-source regression cases also pass. Supplied parser outcomes are not original grammar comparisons. The single-file target branch and its quoted-source, examined-layout, fallback and failure behavior remain unverified.

### Single-file target runtime checkpoint (2026-09-05)

The target-dispatch HUNK passes a successful normal COPY pair and two interleaved instances on each CPU. The fixture validates literal classifications, source examination, unchanged parsed-source normalization, parent directory creation, source/output names, payload, normal destination-first open order and six source/parent/root releases. Receipt: `artifacts/qualification-copy-single-target-native/qualification.json`. Seven earlier target cases pass as controls. This does not qualify transformed tick quoting, existing targets, any failure branch, stream fallback, MOVE/LINK or the unified retained-parser entry.

### Single-target transfer failure checkpoint (2026-09-05)

Six cases per CPU now include output-open failure, input-open failure and failed write through target dispatch and the actual COPY worker. The fixture verifies primary WARN, unchanged secondary OK, handle/lock ownership and the original quiet-path flags. Partial-output DeleteFile occurs after the applicable Close calls and observes their changed IoErr; output-open failure skips deletion. Receipt: `artifacts/qualification-copy-single-target-failures/qualification.json`. This expands transfer failures only, not all earlier selection/examination/parent creation failures, visible diagnostics, quoting transformations, stream fallback or MOVE/LINK behavior.

### 2026-09-05 — Single-file MOVE and LINK target execution

Expanded the normal single-file target suite to ten invocations per CPU: COPY success/open/write failures, MOVE rename and copy/delete fallback, hard-link creation, soft-file rejection and interleaved callers. Checks enforce rename/link arguments and order, source-lock ownership, no transfer for rename/link, fallback source deletion after both closes, soft-file primary WARN and restored target suffix.

Receipt: artifacts/qualification-copy-single-target-modes/qualification.json. All 30 invocations pass across 68000/68020/68040 with balanced resources and no shared-image writes. These are supplied DOS vectors through a private entry, not shipping lifecycle or original-system differential evidence. Directory MOVE/LINK, quoting, earlier selection failures, streams and retained-parser target composition remain open. No shipping gate closes.

### 2026-09-05 — Explicit Copy RDArgs lifecycle integration

Added NativeMorphOSCopyParser with DOS AllocDosObject(RdArgs), original extended help, ReadArgs using caller-cleared 24-LONG storage, successful-parse FreeArgs and unconditional owned-object FreeDosObject. The original stale BUFFER default and QUIET help wording are preserved. New NativeCommandArguments borrowed-storage methods retain parser-owned result pointers without allocating/freeing the caller array or preserving IoErr across parser cleanup. Existing owned parsing remains unchanged; callers must pair each lease with its matching release method.

Added private NativeMorphOSCopyExplicitParserOperationsEntry joining this parser to setup and unified operations. Build succeeds and resident native compilation passes on 68000/68020/68040; reports are artifacts/copy-explicit-parser-static. Runtime parser allocation/failure/help/cleanup verification is still required. This entry remains private and does not yet reproduce original pre-admission effects, requester handling, cache/finalization ordering or full startup. No shipping gate closes.

### 2026-09-05 — Explicit RDArgs native operation execution

The explicit-parser operation entry passes 25 invocations per CPU: DELETE ten, MAKEDIR seven, DIRECT seven and a parser-failure case. The fixture verifies DOS_RDARGS type/tag arguments, non-null caller-supplied ReadArgs object, all 24 initially cleared result slots, SHA-256 of the complete source-observed extended help, FreeArgs before FreeDosObject on successful parsing, and FreeDosObject without FreeArgs on parser failure. Operations retain parser-owned data through execution. FreeDosObject deliberately overwrites IoErr; the private entry still reports its captured fixture result, so this does not qualify original command finalization timing.

Receipt: artifacts/qualification-copy-explicit-parser/qualification.json (75 invocations total). Existing owned-parser operation regression also passes 72 invocations: artifacts/qualification-copy-owned-parser-regression/qualification.json. No resources leak or shared-image writes occur in these supplied-vector runs. DOS_RDARGS allocation failure, interactive DOS help rendering/grammar, requester/version handling, pre-admission effects and shipping finalization remain open. No shipping gate closes.

### 2026-09-05 — Copy requester ownership in explicit operation entry

Added NativeMorphOSCopyRequesters using invocation-owned saved process/window state. It captures pr_WindowPtr, writes -1 only for NOREQ, and restores the original value before parser teardown. Integrated into the explicit-parser operation root around setup/verbosity/operations. The fixture seeds distinct process window pointers, checks suppression during DIRECT success and output-open failure, and verifies restoration at FreeArgs. Native-write permission is restricted to the current parsed invocation's four-byte WindowPointer field, not the entire process.

All 25 cases per CPU pass (75 total) across 68000/68020/68040: artifacts/qualification-copy-requesters/qualification.json. The root has 61 reachable methods and no reported runtime features/helpers, external native targets, exception regions or fatal machine-fault sites. This remains a private entry: requester capture/suppression on rejected admission, final cache/report ordering, version handling, original runtime behavior and shipping qualification remain open. No shipping gate closes.

### 2026-09-05 — Pre-admission Copy setup ordering

Separated no-I/O admission evaluation from error publication. Existing Validate remains an Evaluate + SetIoErr wrapper. The explicit-parser operation entry now performs option/default-source setup, positional target normalization, requester suppression and verbosity classification even when admission will fail, then publishes the rejection instead of running operations. Successful admission no longer resets IoErr before this setup through Validate.

Added rejected DIRECT+CLONE+NOREQ coverage verifying one classifier/cleanup pair before rejection, no early TooManyArguments error, suppressed requester state during classification, restored state before FreeArgs, retained setup values and no file operations. All 26 invocations per CPU pass (78 total); the strengthened DIRECT assertions were rerun on every CPU. Receipt: artifacts/qualification-copy-admission-order/qualification.json. This is supplied-vector private-entry evidence; original state-allocation/startup ordering, final result/cache teardown, version handling and shipping comparisons remain open. Existing wrapper behavior is retained but older static method-count receipts need regeneration after the new Evaluate boundary. No shipping gate closes.

### 2026-09-05 — Normal-ABI Copy lifecycle candidate

Added NativeMorphOSCopyCommand.Run combining explicit parsing, pre-admission setup/verbosity/requesters, all operation dispatch, requester restoration, parser teardown, final diagnostics/result selection, transfer cache release and workspace release in one invocation. A cleared 4754-byte workspace contains the result array and prior operation buffers; parser storage survives operations, and transfer cache survives parser cleanup and final reporting. Added NativeMorphOSCopyCommandEntry with normal command ABI and no private control block. It rejects/replies Workbench startup before opening DOS, uses DOS 37 minimum and closes DOS without restoring an earlier IoErr after final cleanup.

Build succeeds; resident compilation succeeds for 68000/68020/68040 with no reported runtime features/helpers, external native targets, exception regions or fatal machine-fault sites. Reports: artifacts/copy-command-lifecycle-static. This is a command candidate, not a qualified shipping artifact. Whole-entry runtime, allocation and parser failure paths, command output/results, version-gated extended examination, original-runtime differential behavior, packaging and original purity requirements remain unqualified. The original allocates CopyData even after DOS-open failure; this candidate returns immediately on that failure, so startup failure parity remains open. No shipping gate closes.

### 2026-09-05 — Normal Copy command ABI runtime

Added a +command fixture selector restricted to DIRECT lifecycle cases. It invokes the normal command entry without the private 32-byte control block, validates real return levels and final IoErr, and reuses existing payload, open/close, parser/help and requester assertions. The command's single 4754-byte cleared workspace replaces separate result/scratch allocations. Final PrintFault must observe the deliberately poisoned FreeDosObject error 903 before any cache/workspace free; final cleanup leaves IoErr 902 rather than restoring the earlier error. Tests include successful DIRECT copy/delete, input/output/write failure, parser failure, rejected DIRECT+CLONE+NOREQ and interleaved invocations.

Nine cases pass per CPU (27 total), with balanced resources and no shared-image writes. Reports: artifacts/copy-command-lifecycle-static/direct-68000.runtime.json and corresponding 68020/68040 reports. Reproduction driver: tools/Commands/qualify_copy_command_lifecycle_native.ps1. These execute the normal ABI candidate through supplied DOS vectors; normal non-DIRECT modes, startup failures, Ctrl-C/ERRWARN integration, actual DOS parser behavior, original comparisons and shipping qualification remain open. No shipping gate closes.

### 2026-09-05 — Normal command DELETE/MAKEDIR and final cancellation

Extended the normal-ABI command fixture to DELETE and MAKEDIR, preserving their existing parser, pattern, payload, lock and stopping assertions. Final expectations now select primary errors over secondary results and apply ERRWARN promotion. Added a DIRECT copy case whose Ctrl-C becomes pending only after FreeDosObject; it checks the final signal query, PrintFault(ERROR_BREAK) before any cache/workspace free, RETURN_ERROR with ERRWARN, and cleanup's final IoErr rather than an artificially restored break error.

All 27 cases per CPU pass (81 total): DELETE ten, MAKEDIR seven, DIRECT ten. Receipt: artifacts/qualification-copy-command-final-break/qualification.json. The earlier 26-case matrix also completed successfully in artifacts/qualification-copy-command-operations. All invocations use the normal command entry with supplied DOS vectors and no private output control. Normal COPY/MOVE/LINK target composition, startup/allocation failure paths, version gating, original runtime comparisons and shipping qualification remain open. No shipping gate closes.

### 2026-09-05 — DOS version selection and missing-DOS allocation path

The normal Copy command now attempts workspace allocation even after DOS-open failure, then frees any acquired workspace and returns FAIL without DOS calls, matching the original control-flow requirement. This failure branch is implemented but not runtime-qualified yet. The command selects ExtendedExamine from the open library's version/revision: 51.66 or newer; older libraries retain classic calls.

Added normal-command DIRECT cases for DOS 51.65, 51.66 and 52.0. Callbacks reject the wrong legacy/extended examination vector, verify the extended POSIX-date request tags and supply a 64-bit size. All 30 invocations per CPU pass (90 total): artifacts/qualification-copy-command-version-boundaries/qualification.json. The earlier 27-case regression also completed in artifacts/qualification-copy-command-version-gate. Destination TestDest still uses classic Examine and requires the same extended-path integration; the new version predicate does not close that gap. Normal COPY/MOVE/LINK target integration, startup failure runtime, original-runtime comparison and shipping qualification remain open. No shipping gate closes.

### 2026-09-05 — Version-aware destination examination

Added the extended TestDest examination path: clear actual extension flags before examining; use Examine64 with POSIX-date tags when selected, otherwise classic Examine. Stateful directory opening, file work and directory work now pass the command's version choice and dedicated tag workspace. The normal command workspace grows from 4754 to 4770 bytes to retain sixteen invocation-owned tag bytes without overwriting a live source/destination string. The classic bounded helper overload remains available.

All 30 normal-command regression cases pass per CPU (90 total), including existing DIRECT version boundary tests: artifacts/qualification-copy-destination-examine/qualification.json. These regressions do not yet exercise an existing destination through the new Examine64 path; its lock/examine/error/cleanup runtime matrix remains required. Callers enabling ExtendedExamine must now supply ExamineTags (sixteen writable bytes); older private roots that set the flag without that storage require updating before using extended destination cases. Full normal target composition, original-system comparison and shipping qualification remain open. No shipping gate closes.

### 2026-09-05 — Existing destination Examine64 runtime

Added normal-command MAKEDIR cases for an existing directory and failed Examine64 on DOS 51.66. The fixture provides a destination FIB through AllocDosObject, poisons its actual extension flags, checks that the command clears them and supplies the original POSIX-date tags, and verifies no directory creation occurs. Success unlocks the destination before freeing the FIB and subsequently obtains/releases the final lock. Examination failure frees the FIB before releasing the failed target lock, returns primary ERROR, and skips the final lock.

All 32 cases per CPU pass (96 total): artifacts/qualification-copy-existing-destination/qualification.json. The stronger poisoned-flag and FIB/unlock ordering assertions were rerun for all three CPUs. Existing-file deletion/overwrite variants, legacy destination examination, FIB allocation failure, normal COPY/MOVE/LINK composition and original-system qualification remain open. This is supplied-DOS normal-entry evidence, not shipping approval. No shipping gate closes.

### 2026-09-05 — Normal command startup allocation failures

Added runtime cases for missing DOS with successful workspace allocation, workspace allocation failure with DOS available, and both failures together. The first enforces exactly one allocation/free and no parser, fault, SetIoErr or filesystem calls. Allocation failures enforce no free of nonexistent storage and no parser allocation; only the DOS-available case reports the current IoErr. All return FAIL with balanced library ownership and the source-observed cleanup/error timing.

All 35 cases per CPU pass (105 total): artifacts/qualification-copy-startup-allocation/qualification.json. The earlier missing-DOS-only matrix also passed in artifacts/qualification-copy-missing-dos. Workbench startup, DOS_RDARGS allocation failure, normal COPY/MOVE/LINK composition and original-system/shipping qualification remain open. These are supplied-vector runs through the normal command ABI. No shipping gate closes.

### 2026-09-05 — Original Copy Workbench rejection order

Corrected the normal command entry's Workbench path to FindTask/CLI check, WaitPort, Forbid, GetMsg, ReplyMsg, then RETURN_FAIL. The generic startup helper retrieved the message before Forbid, which did not match this command's original source. COPY now performs its own source-specific sequence without opening DOS or allocating state. Other commands retain their existing startup helper.

Added normal-entry Workbench runtime coverage and narrowed the shared fixture's alternate ordering rule to the Copy command selector. Checks enforce zero library opens/closes, allocations or parser calls, one wait/get/reply and final Forbid/GetMsg/ReplyMsg ordering. All 36 cases per CPU pass (108 total): artifacts/qualification-copy-workbench-startup/qualification.json. DOS_RDARGS allocation failure, full normal COPY/MOVE/LINK runtime composition and original-system/packaging/purity qualification remain open. No shipping gate closes.

### 2026-09-05 — DOS_RDARGS allocation failure through normal entry

Added a failed DOS_RDARGS allocation case with NoFreeStore supplied by DOS. The normal command returns FAIL, reports that error before releasing its sole workspace, never calls ReadArgs/FreeArgs/FreeDosObject, and performs no file operation. The test provides no synthetic RDArgs allocation in this case, so an accidental use/free cannot be hidden by fixture storage. Cleanup leaves its own final IoErr as in the existing whole-entry contract.

All 37 cases per CPU pass (111 total): artifacts/qualification-copy-parser-allocation/qualification.json. This completes the currently enumerated normal-entry startup allocation cases, not all command failure behavior or shipping qualification. Normal COPY/MOVE/LINK target composition, wider option/recursive/error coverage, original-system comparisons, packaging and pure/resident acceptance remain open. No shipping gate closes.

### 2026-09-05 — Normal single-file COPY/MOVE/LINK lifecycle

Connected the single-target behavior matrix to NativeMorphOSCopyCommandEntry using a +single-command selector. It supplies ReadArgs input into the command-owned cleared workspace, retains a real fixture-owned RDArgs object until FreeArgs/FreeDosObject, and checks actual command return values/final IoErr rather than private state output. Existing target classification, payload, lock, MOVE rename/fallback, hard-link/soft-file rejection and failure cleanup assertions remain active. Added protection-metadata ABI checks and parser release after the final target unlock, followed by final workspace/cache cleanup.

Ten cases pass per CPU (30 total): artifacts/single-command-68000.json and corresponding 68020/68040 reports, using the hash-bound command HUNKs in artifacts/qualification-copy-parser-allocation. The standard command lifecycle driver now includes this matrix (47 total per CPU on its next complete run). These supplied-DOS cases exercise single literal files, quiet mode and classic source examination; recursive/multiple-source targets, quoting transformations, streams, broader metadata/options and original-runtime/purity/packaging acceptance remain open. No shipping gate closes.

### 2026-09-05 — Normal directory-target COPY composition

Connected the existing directory-target/wildcard traversal fixture to the normal command entry using a +directory-command selector. The command parses into its own workspace, classifies/creates the destination, routes a source pattern, performs pending file operations, releases the target, and only then frees parser state and final workspace/cache. Pointer checks now account for command-owned classifier and path locations while retaining payload, match-step ordering, root lock and cleanup assertions. Cases cover zero/one/two matched files and interleaving; target-pattern rejection cases remain in their prior private fixture.

The combined standard driver passes 52 invocations per CPU (156 total): DELETE ten, MAKEDIR nine, DIRECT/startup eighteen, single-file COPY/MOVE/LINK ten and directory-target COPY five. Receipt: artifacts/qualification-copy-normal-targets/qualification.json. These are normal-ABI supplied-DOS runs. Multiple source patterns, recursive directories, stream targets/sources, transformed quoting, visible output and original-system/purity/packaging acceptance remain open. No shipping gate closes.

### 2026-09-05 — Normal command destination rejection

Included wildcard and invalid destination patterns in the normal directory-target command suite. Both now verify actual command return levels (ERROR for wildcard destination, FAIL for parse failure), target-sized ParsePattern allocation, zero source classification/destination locks, and balanced parser/workspace cleanup. These were previously covered only through the private target-dispatch entry. Quiet behavior is retained; visible diagnostics remain a separate requirement.

The complete normal-entry driver passes 54 cases per CPU (162 total): artifacts/qualification-copy-target-rejection/qualification.json. Multiple source patterns, recursive directories, streams, quoting transformations, visible output and original-system/purity/packaging qualification remain open. No shipping gate closes.

### 2026-09-05 — Quoted source normalization through normal command

Added a source containing a DOS tick escape (SYS:'file0) to the normal single-file COPY matrix. The supplied ParsePattern callback produces SYS:file0; assertions require source classification to receive the original spelling, the normalization allocation to use the original source length, the parser-owned source string to be rewritten in place, and subsequent locks/file operations to use the normalized name. Existing payload, six-lock cleanup and parser-lifetime assertions remain active.

All 55 cases per CPU pass (165 total): artifacts/qualification-copy-quoted-source/qualification.json. This verifies command composition around supplied DOS parsing; it does not independently establish real DOS escape grammar. Failed normalization/lookup, more complex names, multiple/recursive sources, streams, visible output and original-system/purity/packaging acceptance remain open. No shipping gate closes.

### 2026-09-05 — Non-filesystem source through normal COPY

Added NET:file0 as a supplied non-filesystem source to the normal directory-target command matrix. The case verifies classification before IsFileSystem, restoration of the temporarily split device prefix, copying into the command-owned source path, FilePart routing, no filesystem traversal allocation/MatchEnd/source locks, direct DOS payload transfer, and destination/parser/cache cleanup. The fixture now distinguishes the stream's buffer allocation from a filesystem AnchorPath allocation.

All 56 cases per CPU pass (168 total): artifacts/qualification-copy-stream-source/qualification.json. The handler is simulated; actual stream handler integration, non-filesystem destinations, stream failure/cancellation, multiple/recursive sources and original-system/purity/packaging acceptance remain open. No shipping gate closes.

### 2026-09-05 — Multiple source patterns through normal COPY

Added two distinct FROM patterns (SYS:#? and SYS:other#?) to the normal directory-target command matrix. Each supplies one file. Assertions enforce completion of the previous pending copy before the next source classification, a separate matcher lifetime for each pattern, destination path-cache rebuilding per source, preservation of the shared root target lock and reuse of the same transfer-buffer address. The allocation matrix permits the second matcher, not a second transfer buffer. Parser data remains live until all work finishes.

All 57 cases per CPU pass (171 total): artifacts/qualification-copy-multiple-sources/qualification.json. Strengthened buffer-address reuse assertions were rerun on each CPU. Multiple-source failure/cancellation, recursive directories, real handlers, visible output and original-system/purity/packaging acceptance remain open. No shipping gate closes.

### 2026-09-05 — Multiple-source write failure and ERRWARN

Added a write failure on the first of two source patterns through the normal command entry. The default case removes the partial first output after closing its handles, then classifies/transfers the second source with the retained cache and returns WARN. The ERRWARN case skips the second source entirely and returns ERROR. Assertions distinguish the number of processed sources from the input list, check one partial-output deletion, and retain per-source matcher, root lock, cache reuse and parser-lifetime checks.

All 59 cases per CPU pass (177 total): artifacts/qualification-copy-source-failures/qualification.json. Source cancellation and other failure stages, recursive directories, real handlers, visible output and original-system/purity/packaging acceptance remain open. These supplied-DOS normal-entry cases do not close a shipping gate.

### 2026-09-05 — Cancellation between normal COPY sources

Added Ctrl-C becoming pending after the first of two source patterns completes, with and without ERRWARN. Both cases prevent second-source classification, retain the successfully copied first output, release the destination/parser/cache, and perform final cancellation selection after parser teardown. Checks require both the source-loop cancellation observation and the final cancellation query. The command returns WARN normally and ERROR with ERRWARN; no partial-output deletion occurs.

All 61 cases per CPU pass (183 total): artifacts/qualification-copy-between-source-cancel/qualification.json. Mid-transfer and recursive cancellation, real handlers, visible output and original-system/purity/packaging acceptance remain open. No shipping gate closes.

### 2026-09-05 — Visible normal COPY failure reporting

Added a non-QUIET output-open failure through the normal single-file command. The case verifies the additional pre-admission source classification, exact command-owned prefix " not opened for output: ", the PrintFault call using IoErr after VPrintf (deliberately changed by the fixture), and parser liveness during reporting. It rejects duplicate final fault output and the quiet-only metadata tail. The supplied DOS fault renderer emits a marker, so the operating system's localized fault wording is not qualified.

All 62 cases per CPU pass (186 total): artifacts/qualification-copy-visible-failure/qualification.json. Successful verbose output, other diagnostics, recursive behavior and original-system/purity/packaging acceptance remain open. No shipping gate closes.

### 2026-09-05 — Visible input, write and soft-link failures

Extended the normal single-file visible-output matrix to input-open failure, write failure and rejected soft-file linking. Checks require the operation-specific copied./linked. diagnostic prefix, post-prefix IoErr passed to PrintFault, one diagnostic only, no quiet metadata tail, parser liveness and the existing per-path lock/partial-output cleanup. DOS-rendered fault text remains a fixture marker rather than original localized output.

All 65 cases per CPU pass (195 total): artifacts/qualification-copy-visible-failures/qualification.json. Successful verbose output, recursive paths and original-system/purity/packaging qualification remain open. No shipping gate closes.

### 2026-09-05 — Successful verbose normal COPY output

Added a non-QUIET wildcard COPY success case through the normal command entry. Checks cover the extra pre-admission wildcard classification selecting verbosity, exact directory/file indentation and text, creation and copied messages, caller-owned formatting storage, parser liveness and both name flushes before file opens. The expected bytes are "        RAM: (Dir)   [created]\n   file0..copied.\n". No synthetic fault renderer is involved in this successful-output case.

All 66 cases per CPU pass (198 total): artifacts/qualification-copy-verbose-success/qualification.json. Recursive indentation/output, broader operation combinations and original-system/purity/packaging acceptance remain open. No shipping gate closes.

### 2026-09-05 — Verbose empty-match notice

Added the normal-command verbose wildcard case with no matched files. It checks exact destination-creation output followed by "No file was processed.\n", one name flush, no transfer allocation, and notice ordering after matcher/root-lock cleanup but before parser release. This exercises the captured argument-verbosity policy in the complete command lifecycle.

All 67 cases per CPU pass (201 total): artifacts/qualification-copy-empty-notice/qualification.json. Recursive traversal/output and original-system/purity/packaging qualification remain open. No shipping gate closes.

### 2026-09-05 — Classic target-dispatch examination size fields

Corrected classic Examine handling in target dispatch to zero-extend fib_Size and fib_NumBlocks into the MorphOS 64-bit FIB extension fields, as the original _genExamine does, including after a failed Examine call. The native single-target fixture poisons extended high words and supplies classic values with bit 31 set; checks require zero high words and unchanged unsigned low words before releasing the examined source lock. This prevents stale extended sizes and accidental sign extension.

All 67 cases per CPU pass (201 total): artifacts/qualification-copy-classic-size-extension/qualification.json. The new runtime assertion covers successful source Examine in target dispatch; failed Examine and other examination call sites need their own coverage. Recursive end-to-end behavior and original-system/purity/packaging qualification remain open. No shipping gate closes.

### 2026-09-05 — Classic destination examination parity

Applied original _genExamine size/block zero-extension to TestDest's classic Examine path, including examination failure. Added DOS v40 existing-directory success and examination-failure cases through the normal command. The fixture poisons extended high words, supplies unsigned classic values with bit 31 set, and verifies conversion before FIB release, together with existing flag clearing and lock cleanup assertions.

All 69 cases per CPU pass (207 total): artifacts/qualification-copy-classic-destination/qualification.json. Recursive behavior and original-system/purity/packaging qualification remain open. No shipping gate closes.

### 2026-09-05 — Classic file-handle examination extension fields

Completed the classic ExamineFH conversion in the transfer helper: populate size/block 64-bit extension fields by zero-extending the classic ULONGs before the buffer is reused for payload. The DIRECT fixture poisons extended high words and supplies a block count with bit 31 set, then verifies the conversion at the first Read. Extended DOS-version cases retain their existing 64-bit call path.

All 69 cases per CPU pass (207 total): artifacts/qualification-copy-classic-handle/qualification.json. This complements the target-dispatch and destination classic-examination fixes. Failed handle examination and wider transfer edge cases still require their own receipts; recursive and original-system/purity/packaging qualification remain open. No shipping gate closes.


### 2026-09-05 — Unknown source size in DIRECT transfer

Added a normal-command DIRECT case where classic ExamineFH fails after leaving size fields populated. The transfer must ignore that apparent size, read the payload and then EOF, perform the original zero-length EOF Write, and close both handles without deleting output. The fixture also verifies unsigned extension-field conversion following failed examination. This passed without a production change.

All 70 cases per CPU pass (210 total): artifacts/qualification-copy-unknown-size/qualification.json. These are supplied-DOS native execution receipts, not original-system or pure/resident shipping qualification. Recursive end-to-end coverage and full command acceptance remain open.


### 2026-09-05 — DIRECT read failure and premature EOF

Compared the original CopyFile transfer loop and added normal-command cases for Read returning -1 and an examined eight-byte source ending after four bytes. The read-error case forbids Write; premature EOF requires the four-byte payload Write followed by the original zero-length Write and a failure result. Both require output/input close ordering and no DIRECT partial-output deletion. Existing production behavior passed unchanged.

The complete supplied-DOS matrix passes 72 invocations per CPU, 216 total, on 68000/020/040: artifacts/qualification-copy-transfer-errors/qualification.json. These checks do not close reference-runtime, recursive, purity or packaging gates.


### 2026-09-05 — Extended examination transfer failure matrix

Ran unknown-size fallback, Read failure, premature EOF and Write failure through the normal DIRECT command with dos.library 51.66. The supplied vectors require ExamineFH64 and the original PosixDate tag list, reject classic examination at this version, and retain handle, payload, result, parser-lifetime and resource checks. Failed extended examination now has explicit coverage independent of classic FIB conversion. Production code passed unchanged.

All 76 invocations per CPU pass, 228 total on 68000/020/040: artifacts/qualification-copy-extended-transfer-errors/qualification.json. This closes these four version-path coverage gaps only; actual MorphOS/Workbench runtime, recursive behavior, purity and packaging remain unqualified.


### 2026-09-05 — Reference audit of quiet destination errors and metadata

Re-read MorphOS copy.c SHA256 13be3cb51223c726aa13abbbac28e9a4545a0a36ec32621262c7bda27a69808f: DoWork initializes printok to a non-null empty string (1667 onward); a failed file TestDest sets printerr without changing RetVal (1990 onward); QUIET bypasses PrintNotDone and enters the printok tail, setting DONE and calling SetData (2075 onward). TestDest (2304 onward) confirms the distinct FIB/lock cleanup orders and ObjectExists handling. This explains the existing private-worker result assertions; they are source-backed behavior, not a desired-policy change.

Strengthened all three quiet existing-destination cases (DONTOVERWRITE, examination failure, FIB allocation failure) to enable protection metadata and require one SetProtection on RAM:entry despite no copy. All 63 worker executions pass across 68000/020/040: artifacts/qualification-copy-work-quiet-metadata/qualification.json. These tests cover the protection part of the metadata tail only; date/comment and original runtime comparison remain open.

### 2026-09-05 - Literal-directory compiler argument-home fix

Confirmed the literal first-directory failure in generated code: a true Boolean parameter was initialized with MOVE.L into its four-byte home, while its escaped managed address read the high byte. Fixed CopperSharp68k allocated argument-home initialization to store the managed byte/word width; incoming stack values use the low byte/word of the big-endian ABI slot. Narrow argument reads now use the authoritative home after a by-reference mutation rather than the original incoming SSA value. No COPY source semantics were changed.

Added Boolean register/stack argument regressions, checking true and false plus readback after mutation, across three CPUs and two optimizer modes. Before the fix, all 12 new cases failed and the 18 existing cases passed (artifacts/compiler-argument-home/argument-home-before.trx). After the fix, 29/30 pass; optimized 68020 BooleanStackHome stops on Copper68k.UnsupportedM68kTimingException for opcode 0x1EAF at 0x00010046 (artifacts/compiler-argument-home/argument-home-after.trx). This is an unresolved execution-coverage limitation, not a passing test. All six new optimizer-disabled cases pass. Further compiler validation must cover byte/word signedness and this optimizer timing path; do not declare the compiler regression suite fully green.

Rebuilt the compiler CLI and normal command HUNKs. The literal SYS: trace now completes all eight records without changing its expected semantics. Full command aggregate passes 94 invocations per CPU, 282 total, including 54 recursive invocations on 68000/020/040; no shared-image writes or resource leaks. Receipt: artifacts/qualification-copy-literal-home-fix/qualification.json. Driver now expects 18 recursive cases. This supersedes the failing-literal checkpoint only; original-system equivalence, traversal failure coverage, pure/resident acceptance, packaging and all full-scope completion gates remain open.

### 2026-09-05 - Normal recursive matcher terminal errors

Re-read MorphOS copy.c's PatCopy error tail: MatchEnd precedes an unconditional source prefix and saved-error PrintFault, followed by SetIoErr and RETURN_FAIL in the secondary result. Added normal-command cases for terminal MatchNext ObjectNotFound and Break after sibling traversal. Both require diagnostics despite QUIET, no duplicate final fault, original error passed to PrintFault despite prefix output clobbering IoErr, immediate restoration despite PrintFault clobbering IoErr, and parser lifetime through diagnostics. Existing payload, metadata, lock, handle and shared-image assertions remain active. COPY production code passed unchanged.

Full rebuilt aggregate passes 96 invocations per CPU, 288 total, including 60 recursive executions on 68000/020/040. Receipt: artifacts/qualification-copy-terminal-errors/qualification.json. Driver expects 20 recursive cases. This covers matcher errors after the final directory exit only; early traversal errors, pending work at failure, parent-lock failure, original-system equivalence, pure/resident qualification and shipping remain open. No full command or goal gate is closed by these fixture results.

### 2026-09-05 - Pending-file traversal completion and failures

Added normal-command early MatchNext ObjectNotFound and Break with the first child snapshot pending and an owned destination subdirectory still active. Re-read original PatCopy's MatchEnd/error/deferred-work tail and DoWork's RetVal2 guard at copy.c 1667 onward. The failure cases require no file Open, payload, metadata or source mutation; diagnostics still preserve IoErr and occur before parser teardown, and the transient destination lock is released. Added matching ERROR_NO_MORE_ENTRIES at the same boundary: it must copy exactly the first saved child after MatchEnd, preserve its protection metadata and leave the second subtree untouched. No COPY implementation changes were needed.

All 23 recursive cases pass on each CPU. Full rebuilt aggregate passes 99 invocations per CPU, 297 total on 68000/020/040, with no leaked resources or shared-image writes: artifacts/qualification-copy-pending-completion/qualification.json. Driver expectations updated only after the new cases passed. Updated the recursive acceptance table to reflect previously verified nested/literal coverage. Parent failure, dangling links, further interruption boundaries, actual OS/ReadArgs equivalence, pure/resident and shipping gates remain open; this does not close CC13 or the full goal.

### 2026-09-05 - Parent-directory failure during COPY ascent

Added normal-command ParentDir failure at the first directory exit after its child payload has completed. Three source-backed cases distinguish nonzero IoErr (ObjectNotFound), zero IoErr (InvalidLock fallback), and immediate MatchNext NoMoreEntries (no promoted failure). Re-read original PatCopy parentdirerr loop guard and DIDDIR continue: it still calls MatchNext, suppresses directory metadata and only reports the parent failure if another iteration is admitted. Tests require exactly one copied child, untouched second subtree, no directory metadata, restored CurrentDir, no live locks, and normal parser teardown. The two diagnosed cases retain the saved-error output checks; the end-of-enumeration case returns OK without diagnostics. COPY implementation passed unchanged.

All 26 recursive cases pass on each CPU. Full rebuilt aggregate passes 102 invocations per CPU, 306 total on 68000/020/040, with no resource leaks or shared-image writes. Receipt: artifacts/qualification-copy-parent-failure/qualification.json. Updated the driver and recursive trace evidence table. Parent failure in MOVE/DELETE, dangling links, remaining option/OS compatibility, pure/resident and shipping qualification remain open. No complete command or full-goal gate was closed.

### 2026-09-05 - Normal recursive soft-link probe fallback

Re-read and hash-verified the original PatCopy USE_SOFTLINKCHECK branch (copy.c SHA256 13be3cb51223c726aa13abbbac28e9a4545a0a36ec32621262c7bda27a69808f). Added normal-command transient relative Lock failures at the first directory: InvalidLock must avoid device lookup, while ObjectNotFound with GetDeviceProc failure must use the empty relative device name and null previous DevProc. Both remain enterable and subsequently copy both directory payloads. The callbacks require matcher-parent CurrentDir context and restoration of the original IoErr before restoring CurrentDir, even when GetDeviceProc changes it. No ReadLink or FreeDeviceProc is permitted without a device result. Existing full output/metadata/resource assertions remain active. Production COPY passed unchanged.

All 28 recursive cases pass on each CPU. Full rebuilt aggregate passes 104 invocations per CPU, 312 total on 68000/020/040, with no resource leaks or shared-image writes: artifacts/qualification-copy-probe-failure/qualification.json. Driver and recursive evidence table updated. Positive/negative ReadLink, link-buffer allocation failure and dangling-link traversal through the normal entry remain open, as do actual OS, pure/resident and shipping qualification. No full command gate is closed.

### 2026-09-05 - Normal recursive ReadLink fallback

Added negative and zero ReadLink results through normal-command recursive COPY. The probe receives a device object with a port and matcher-parent lock, allocates a 512-byte public buffer, and calls ReadLink with the relative first-directory name and 511-byte limit. Both results permit descent and preserve both copied payloads and metadata. The fixture requires buffer cleanup before FreeDeviceProc, one release, and restoration of the original ObjectNotFound before CurrentDir despite both DOS calls and FreeMem changing IoErr. Added a bounded allocation path for the extra link buffer and later transfer cache; sizes, flags, request tracking, count and cleanup remain checked. Production code passed unchanged.

All 30 recursive cases pass on each CPU. Full rebuilt aggregate passes 106 invocations per CPU, 318 total on 68000/020/040, with no leaked resources or shared-image writes: artifacts/qualification-copy-readlink-fallback/qualification.json. Driver and recursive evidence table updated. Positive ReadLink/dangling-link traversal and link-buffer allocation failure remain open in the normal entry, along with remaining command/OS, pure/resident and shipping gates. No full-scope gate was closed.

### 2026-09-05 - Normal recursive link-buffer allocation failure

Added failure of the 512-byte link-probe allocation after a successful GetDeviceProc. ReadLink must not run, the device must be released without a buffer free, and ObjectNotFound must be restored before CurrentDir despite allocation/device cleanup changing IoErr. Normal directory descent then allocates the separate transfer cache and copies both sibling payloads. The fixture counts five allocation attempts and four successful allocations/frees, while retaining independent bus ownership checks. Renamed the fixture predicate to RecursiveHasLinkDevice so it does not imply ReadLink runs after an allocation failure. COPY production code passed unchanged.

All 31 recursive cases pass on each CPU. Full rebuilt aggregate passes 107 invocations per CPU, 321 total on 68000/020/040, with no leaks or shared-image writes: artifacts/qualification-copy-link-buffer-failure/qualification.json. Driver and recursive evidence table updated. Positive ReadLink/dangling-link traversal through the normal entry remains open; original OS/ReadArgs equivalence, remaining options, pure/resident and shipping gates are also still open. No full command gate closed.

### 2026-09-05 - Normal recursive dangling-link skip under QUIET

Added a positive ReadLink result for the first directory, followed by a valid second directory. The matcher requires DODIR to remain clear for the dangling entry and supplies no child/exit records for it. The filesystem has no lockable first target; it requires exactly one deferred absolute source Lock after the probe is released, which produces WARN. Traversal must then copy the second directory's distinct payload and file/directory metadata, without creating RAM:first. Buffer/device cleanup, saved IoErr, parser lifetime, restored CurrentDir, lock ownership and no QUIET diagnostic remain checked. COPY production code passed unchanged.

All 32 recursive cases pass on each CPU. Full rebuilt aggregate passes 108 invocations per CPU, 324 total on 68000/020/040, with no leaks or shared-image writes: artifacts/qualification-copy-dangling/qualification.json. Updated driver and recursive evidence table. Visible dangling-link warning, ERRWARN behavior and original filesystem link comparison remain open, as do remaining option, OS/ReadArgs, pure/resident and shipping gates. No full command gate closed.

### 2026-09-05 - Dangling-link ERRWARN stop

Added ERRWARN to the normal-command QUIET dangling-link scenario using ReadArgs slot 13. After the deferred source Lock produces WARN, MatchNext still supplies the next record, but no further loop or deferred worker may create a destination or copy a payload. The test requires RETURN_ERROR (10), exactly two MatchNext calls, no output directory/files/metadata, unchanged later source, and complete matcher/device/buffer/parser cleanup. It counts four successful allocations because no transfer cache is needed. The paired case without ERRWARN still copies the later directory and returns WARN. Production implementation passed unchanged.

All 33 recursive cases pass on each CPU. Full rebuilt aggregate passes 109 invocations per CPU, 327 total on 68000/020/040, with no leaks or shared-image writes: artifacts/qualification-copy-dangling-errwarn/qualification.json. Driver and recursive evidence table updated. Visible warning and original filesystem comparison remain open, as do remaining options, actual OS/ReadArgs, pure/resident and shipping qualification. No full command or goal gate closed.

### 2026-09-05 - Visible dangling-link warning with ERRWARN

Added a normal-command visible dangling-link ERRWARN case. It exercises the startup verbosity classifier in addition to traversal classification. The ReadLink fixture poisons buffer byte 511; the warning requires that byte to be terminated while the 512-byte allocation and device are still live, with exact first/missing arguments and invocation-owned VPrintf storage. The deferred absolute source failure prints the original ' not read.: ' prefix followed by one fault. Prefix output deliberately changes IoErr to 901, and PrintFault must receive that post-prefix value, matching PrintNotDone. No duplicate final fault or destination work is allowed. QUIET cases now explicitly verify absence of output and their single classifier invocation. Production COPY passed unchanged.

All 34 recursive cases pass on each CPU. Full rebuilt aggregate passes 110 invocations per CPU, 330 total on 68000/020/040, with no leaks or shared-image writes: artifacts/qualification-copy-dangling-visible/qualification.json. Updated driver and recursive evidence table. Visible continuation after the warning, real filesystem link comparison, remaining options and actual OS/ReadArgs, pure/resident and shipping gates remain open. No full command gate closed.

### 2026-09-05 - Visible continuation after dangling-link warning

Added the visible dangling-link case without ERRWARN. After the warning and deferred source-read fault, normal COPY must create the second directory, copy its child, print exact indentation/name/creation/copy text, and retain WARN. Output callbacks require the two progress-name flushes before file handles are opened, with no payload completed at either flush, and parser ownership throughout output. The paired visible ERRWARN case still prints only the warning/fault and stops. Existing payload, metadata, buffer/device, matcher and shared-image ownership checks remain active. Production code passed unchanged.

All 35 recursive cases pass on each CPU. Full rebuilt aggregate passes 111 invocations per CPU, 333 total on 68000/020/040, with no resource leaks or shared-image writes: artifacts/qualification-copy-visible-continue/qualification.json. Updated driver and recursive evidence table. Real filesystem link behavior, remaining mode/options, actual OS/ReadArgs equivalence, pure/resident and shipping qualification remain open; no full command gate closed.

### 2026-09-05 - Original DOS observations for the full Copy template

Reviewed CC03/CC04/CC13 integration gaps and added a separate LicensedCopyReadArgs test at the existing MedPlayer native reference owner. The old 44-case corpus is unchanged. The new corpus uses the exact current Copy template (checked equal: 275 characters, 24 slots), with empty/positional/multiple/quoted inputs, short and long aliases, mode/metadata switches, negative/invalid/overflow BUFFER and a repeat after failure. Shared fixture capacity checks now admit 96 bytes of slots and 511 template characters within existing nonoverlapping storage; guard bytes, native-vector checks, value copying before FreeArgs, cursor/IoErr observations and instruction limits are unchanged.

The shared build could not copy dependencies because unrelated testhost PID 30644 held its output. Stopped only our build attempt and rebuilt all dependencies under artifacts/copy-original-readargs/isolated-build. The unrelated test process was not interrupted. The isolated licensed original-DOS run passes one test, zero failures/skips, with 12 original observations and zero generated-parser comparisons or command invocations. TRX: artifacts/copy-original-readargs/copy-original-readargs-isolated.trx. Typed observations and source/test/runtime hashes: artifacts/copy-original-readargs/original-copy-observations.json. This uses the existing licensed Exec bootstrap and its documented overlays; it is not normal boot, stream, RunCommand or MorphOS execution.

Observed facts: bare positional source/destination words both remain in FROM/M and TO is null; empty input succeeds with empty slots; short/long option aliases publish corresponding slots; BUFFER many fails with IoErr 115 and no FreeArgs; BUFFER 2147483648 succeeds with raw number 0x80000000; successful cases retain the initialized IoErr sentinel 0x13572468 through cleanup. These are DOS parsing results, not Copy's later admission/normalization results. Compare the generated DOS/native command against these observations next. No reference answer was substituted for native execution, and no CC04/CC13 or shipping gate is closed. Existing 333 command fixture checks remain a separate historical receipt.
