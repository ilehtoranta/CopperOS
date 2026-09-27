# Join contract

Profiles: wb31 and morphos320. Goal steps: CC01 and CC13. Recorded:
2026-09-11.

Status: partial MorphOS release-source grammar and I/O lifecycle evidence plus
a bounded native append-stream stage and MorphOS frontend. This is not a
complete Join qualification: diagnostics, no-match policy, real handler
behavior, packed-binary correspondence, runtime capture, packaging and parity
remain open.

The observed Workbench 3.1 v40.42 `C/Join` HUNK is 1,200 bytes with SHA-256
`50d8543c20eaa193b324d0b845772dfaaa2fbab1bf17cd4f698d8b98fb35d266`. A
raw scan finds the syntax candidate `FILE/M/A,AS=TO/K/A`; it does not establish
classic parser or file behavior.

The observed MorphOS 3.20 `MorphOS/C/Join` is a 2,657-byte packed native
member, SHA-256
`866b739c811985a071a215758eb03840c5808235d3064c33697ec61830e23a5e`, with
version tag `Join 50.4 (4.10.10)`. The hash-bound 3.20 release archive contains
`c/join/join.c`, 9,234 bytes, SHA-256
`a7cb316d37888f3909888d29c775dc13d68517cc8279f61274caae95ac5a5557`. It
uses DOS `ReadArgs` with:

```text
FILE/M/A,AS=TO/K/A
```

The source opens the destination as a new file before traversing source
patterns. A failure in the join path closes it and deletes the incomplete
destination. Each FILE element gets a fresh public matcher sequence with
Ctrl-C state; directories are skipped quietly, while a non-pattern value is
retried directly after no match. Each append opens the source, allocates a
262,144-byte buffer, loops public Read/Write with exact write-length checking,
polls Ctrl-C, and frees/closes resources on every branch. The source reports
open/read/write/allocation faults and returns FAIL after an aborted append.

The selected MorphOS installer script adds P for Join at `hdinstall.fixc` line
36. This is required-pure design evidence, not installed protection or resident
lifecycle behavior. A future implementation must keep matcher, streams, buffer,
ReadArgs, and output cleanup invocation-local and use DOS files/handlers rather
than host paths.

Before admitting a body, capture exact reference behavior for input order,
multiple patterns, files/directories, destination collisions and self-input,
short read/write, partial destination cleanup, allocation failure, parser
failure, Ctrl-C, output, result and IoErr. Packed correspondence, source-reuse
rights, real handler behavior, package placement, and Workbench/MorphOS parity
remain open.

## Bounded native append-stream stage

[NativeMorphOSJoinAppendLoop.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSJoinAppendLoop.cs)
independently implements only the `append()` transfer rule over caller-owned
source/destination BPTRs and caller-owned guest buffer storage. It delegates the
common exact-transfer policy: poll Ctrl-C without changing signals before each
read, perform one Write for every non-failing Read including the zero-length EOF
read, fail after a short or failed Write, and preserve the immediate `IoErr` for
read, write, or Ctrl-C failure. It does not open/close source files, allocate
Join's 262,144-byte buffer, report diagnostics, or clean an incomplete
destination.

The private
[NativeMorphOSJoinAppendLoopEntry.cs](D:/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeMorphOSJoinAppendLoopEntry.cs)
is a 16-byte direct-control receipt adapter, never the external `Join` command
entry. It opens `dos.library` v37 through the common startup owner and passes
the caller's source BPTR, destination BPTR, APTR buffer, and signed buffer size
to the stage.

[qualify_join_append_native.ps1](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_join_append_native.ps1)
builds resident HUNKs for 68000, 68020, and 68040 and executes eight supplied
direct append-stream vectors per CPU: empty input's required EOF write,
multi-chunk success, read failure, short write, failed write, Ctrl-C before
reading, and interleaved callers. The 2026-09-04 receipt has seven reachable
methods and no managed runtime features/helpers, external native targets,
exception regions, fatal machine fault sites, resource leaks, or shared-image
writes. Artifacts are 1,400 bytes for 68000 SHA-256
`e1409c6c8db0a7c424561aae55f2030349c6f5c8b869b6e05babff37bec50a2b`,
1,428 bytes for 68020 SHA-256
`3567e73b5c895e2706744063504cb446816b180c1d68b45cf608d4232c3a1235`, and
1,400 bytes for 68040 SHA-256
`e7c4d851ef24c7bc18459c80dcb5dbc211030903d2e75ef0c5945d3378e1b575`.
The vectors supply DOS results; they do not prove real handler behavior, the
Join parser or matcher, source/destination lifecycle, packaging, full command
behavior, or original-reference parity.

## Bounded MorphOS frontend

[NativeMorphOSJoinCommand.cs](D:/D-drive/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSJoinCommand.cs)
adds the MorphOS external entry around the captured append loop. It uses the
exact `FILE/M/A,AS=TO/K/A` template, keeps the parser lease and a fixed
invocation-owned matcher/path/FIB workspace, opens the destination before
source processing, uses public DOS pattern matching, skips directory matches,
opens each regular source through DOS, allocates and frees Join's 262,144-byte
buffer per append, and removes an incomplete destination after a failure. The
Workbench 3.1 syntax candidate now has a separate DOS 36 resident entry that
shares this public-DOS body; its exact classic behavior remains unverified.

[qualify_join_command_native.ps1](D:/D-drive/Koodit/GIT/CopperOS/tools/Commands/qualify_join_command_native.ps1)
records a static resident HUNK checkpoint in
`artifacts/join-command-native-20260911/qualification.json`: 18 reachable
methods, zero runtime features/helpers, external targets, exception regions or
fatal fault sites on 68000 (4,208 bytes), 68020 (4,284 bytes) and 68040 (4,200
bytes). The checkpoint intentionally has no full runtime fixture yet; exact
diagnostics, wildcard/no-match behavior, handler execution, packaging, packed
correspondence and differential evidence remain open.

`tools/Commands/qualify_workbench31_join_native_entry.ps1` emits the separate
Workbench receipt in
`artifacts/join-wb31-native-20260912-qualified/qualification.json`. The DOS 36
resident syntax-candidate HUNKs compile for 68000 (4,276 bytes), 68020 (4,356
bytes), and 68040 (4,268 bytes), each with 19 reachable methods and zero
managed runtime features/helpers, external native targets, exception regions
or fatal machine-fault sites. This is static entry evidence only; no runtime
fixture is claimed. Exact Workbench parser/diagnostic behavior, handler
effects, lifecycle, packaging, packed correspondence and differential parity
remain open.

## Bounded frontend runtime receipts

The compiled frontends now run through a supplied public-DOS fixture on one
shared resident HUNK for each of 68000, 68020 and 68040. The fixture invokes
eighteen cases per CPU, including ordered single and multi-source appends,
exact short-write and read-failure handling, destination/source open failure,
parser and workspace/result/buffer allocation failure, empty input, Ctrl-C before a
read, missing-DOS and Workbench startup boundaries, malformed argument
buffers, and instruction-interleaved callers. It checks the `ReadArgs`,
`MatchFirst`/`MatchEnd`, `Open`/`Close`, `Read`/`Write`, `DeleteFile`, signal,
allocation and `IoErr` contracts, including incomplete-destination cleanup.

The Workbench receipt is
`artifacts/join-wb31-native-20260918-runtime-v3/qualification.json`; its
HUNKs are 4,332/4,408/4,324 bytes for 68000/020/040. The MorphOS receipt is
`artifacts/join-morphos320-native-20260918-runtime-v2/qualification.json`;
its HUNKs are 4,264/4,336/4,256 bytes. All six images retain zero managed
runtime features/helpers, external targets, exception regions and fatal
machine-fault sites, with no shared-image writes or fixture leaks.

This is supplied-vector ABI and ownership evidence, not an OS or reference
qualification. Exact Workbench parser/diagnostic and MorphOS handler behavior,
wildcard/no-match policy, packed correspondence, original PURE metadata and
same-segment lifecycle, package placement, and differential parity remain
open.
