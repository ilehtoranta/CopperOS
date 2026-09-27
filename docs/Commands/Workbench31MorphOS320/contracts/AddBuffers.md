# AddBuffers contract

Profiles: wb31 and morphos320. Goal steps: CC01 and CC12. Updated: 2026-09-13.

## Workbench 37.2 reference comparison and corrected body

Executing the pinned 444-byte original (`49be9c1cf1fb87c60fe8d6cc30c73aed6615654763833f3247fc17aecdc6b1ca`)
exposed differences from the earlier shared MorphOS body. The 2026-09-12
Workbench receipt below is historical candidate evidence and is superseded.
`NativeWorkbench31AddBuffersCommand` now implements these classic behaviors:

- The executed template is `DRIVE/A,BUFFERS/N`, with a null ReadArgs source.
- Omitted BUFFERS performs one `AddBuffers(drive, 0)` query. Any supplied
  number, including zero, first performs a change call; a nonzero result then
  causes a separate zero-count query. Numeric pointers preserve signed LONGs.
- Query results greater than zero produce `%s has %ld buffers\n` using that
  result as the count. Negative results are silent, including `-1`; the
  Workbench command does not print an IoErr count for that result.
- A zero result from either handler call invokes `PrintFault(IoErr(), NULL)`.
  After successful parsing, the command returns OK even on this fault path.
- Parser failures print a null-header fault and return FAIL (20).
  Failure to open DOS 36 writes `ERROR_INVALID_RESIDENT_LIBRARY` (122)
  directly to the current Process's Result2 and returns FAIL.
- Output errors do not change the command return level. The command leaves
  the ambient IoErr following output and FreeArgs intact. The replacement
  preserves that value across its extra Exec storage cleanup.

The body uses an invocation-owned eight-byte results/format allocation,
`ReadArgs`, `AddBuffers`, `VPrintf`, `PrintFault`, and `FreeArgs`. It requires
no post-mutation allocation. Its generated-only allocation-failure case
returns FAIL/103, prints the fault, performs no handler operation, and leaks
no resources.

`tests/Commands.NativeExecution/AddBuffersReferenceSuite.cs` compares original
and generated machine-code observations for 27 cases, including omitted/zero/
negative/extreme numeric counts, signed query results, change/query failures,
parser and library failures, output failure/short output, fault-output failure,
and cleanup that changes IoErr. Result, error, output bytes and semantic vector
sequence must agree. DOS parsing, formatting and handlers are supplied by the
fixture. Stack restoration/guards, RDArgs and library ownership, borrowed
argument immutability, and unchanged shared image are checked on each call.

Receipt: `artifacts/addbuffers-wb31-reference-20260913-qualified/qualification.json`.
The original executes on **68000** for all comparisons; replacements execute
on 68000/68020/68040. Each target passes 27 comparisons plus one generated-only
allocation failure: 81 comparisons, 81 original and 84 generated invocations.
All three 1,344-byte HUNKs have five reachable methods and SHA-256
`7e67c35a6aff20de7b7710ee253424e296d8421f281234e835083340733f74ea`.
Static reports contain no managed allocation sites, runtime helpers/features,
external targets, exception regions or fatal fault sites.

The failed earlier comparison is retained at
`artifacts/addbuffers-reference-20260913/baseline-68000-v3.json` (21 original
and 21 candidate calls). An attempted original 68020 execution stopped on
unsupported exact timing for opcode `B232`; its failed receipt remains at
`artifacts/addbuffers-wb31-reference-20260913/addbuffers-wb31-68020.hunk.runtime.json`.
The passing cross-CPU comparison does not close that emulator gap.

Original DOS parsing/formatting/handler execution, Workbench launch, complete
resident/concurrent lifecycle, installed PURE classification and package
admission remain open. MorphOS retains its separate source-based behavior.

## Historical MorphOS and initial Workbench candidate evidence

Status: partial MorphOS release-source grammar and bounded native entry plus a
Workbench syntax candidate; no packed-binary or runtime-parity claim. Official
3.20 `c/addbuffers/addbuffers.c`
is SHA-256 `c3f5fb4f9e70fa0c29484356f8841bcf96d9bb8647443e77de36357626f45c9d`
and defines:

```text
DRIVE/A,BUFFERS/N
```

This establishes a required drive and numeric buffer count for the MorphOS
source. Source inspection observes an omitted BUFFERS value passed as zero to
the public `AddBuffers` vector; vector result `-1` prints the IoErr count, a
positive result prints that result, and zero prints a fault named `AddBuffers`
and returns FAIL. Parser storage is released before library close.

`NativeMorphOSAddBuffersCommand` independently implements that public-vector
path with invocation-owned ReadArgs and VPrintf storage, and its private entry
has a [qualification script](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_addbuffers_native.ps1)
which passes six supplied parser/handler vectors per CPU: changed count, omitted
count/query, handler failure, parser failure, and repeat/interleaved callers.
The ten-method resident HUNKs have no managed runtime features/helpers,
external targets, exception regions, fatal fault sites, or shared-image writes:
68000 is 2,396 bytes, SHA-256
`6a65951cbe52b9879149fedfec324fc0cd196b20ff2aba453761c24bf60411ef`;
68020 is 2,396 bytes, SHA-256
`a1deca85fd41615efb27313b25947c8e326d6e2020b65590ee586220a61384e7`;
and 68040 is 2,396 bytes with the same SHA-256. Packed correspondence,
Workbench grammar, real handler behavior, packaging, and reference comparisons
remain open.

The Workbench 3.1 candidate now has a separate DOS 36 resident entry,
`src/Commands/Native/NativeWorkbench31AddBuffersCommand.cs` with startup
adapter `tests/Commands.AddBuffersNativeRoot/Workbench31AddBuffersEntry.cs`.
It preserves the observed classic `DRIVE/A,BUFFERS/N` boundary and shares the
bounded public-DOS AddBuffers body. The receipt is
`artifacts/addbuffers-wb31-native-20260912-qualified/qualification.json`:

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 2464 | `67418b8cd91ac34e65fd9546c242e768283c94f1ee0908da78619e4a55985355` | 11 | 6 |
| 68020 | 2464 | `70078bef3eb2cce91f6894de083c752e536e7a571a8932bbfa97e339548578e9` | 11 | 6 |
| 68040 | 2464 | `70078bef3eb2cce91f6894de083c752e536e7a571a8932bbfa97e339548578e9` | 11 | 6 |

This remains a bounded syntax/body candidate only. Exact Workbench handler
behavior and diagnostics, original guest parity, PURE/resident reuse,
packaging, and differential comparison remain open.
