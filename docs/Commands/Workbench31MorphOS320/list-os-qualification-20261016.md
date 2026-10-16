# List original-DOS qualification — 2026-10-16

**The installed 4,684-byte List passes the finite original-DOS gate.**
All 300 full-corpus native invocations pass on 68000, 68020 and 68040.
Baseline and candidate agree byte for byte and independently meet the declared
row, diagnostic, return and IoErr contracts. WinUAE independently passes six
calls per CPU. No command, compiler or emulator implementation change was needed.
The existing List optimization and installed image remain unchanged.

## Native boundary

This adapts the Search native OS harness. A frozen NativeOsRunner boots a
RAM-backed OFS floppy with the user-provided Kickstart 3.1 A500 40.63 ROM,
SHA-256 `8C8A0CF04F91B88EAF0C4F1126041987067E2286A8EE590BDBAE447A8000C5EE`.
Original DOS LoadSeg and RunCommand load and invoke the unchanged images.
The authored diagnostic uses native RAM files and writes binary receipts
through OFS. There are no supplied DOS vectors, host HUNK loading, guest
result injection or copied OS implementation in this execution route.

| CPU interpreter | Full-corpus calls | Matched pairs | Requested stack bytes | Caller restoration | Final repeat memory |
| --- | ---: | ---: | --- | --- | --- |
| 68000 | 100 | 50 | 4096 / 8192 | pass | pass |
| 68020 | 100 | 50 | 4096 / 8192 | pass | pass |
| 68040 | 100 | 50 | 4096 / 8192 | pass | pass |

Four batches per CPU complete under the unchanged 3000-frame/120-second caps.
The largest completes at 2050 frames.
The preliminary six-call 68000 smoke run is recorded separately and is not
counted in the 300-call matrix. Boot-inclusive frames and cycles are diagnostic,
not a payload speed result or exact accelerator timing qualification.

## Contracts and cases

The 32 declared cases cover QUICK, QUICK with DATES, full metadata, KEYS,
DATES/NODATES and all four headings, BLOCK rounding, empty files, quoted names,
wildcards, FILES/DIRS, PAT, literal SUB filtering including a metacharacter,
recursive ALL, multiple DIR inputs, SINCE/UPTO inclusion and exclusion, TO
redirection, missing paths, missing arguments, unterminated quotes, unsupported
LFORMAT, invalid dates and pending Ctrl-C.

Native SetFileDate/SetComment/SetProtection create known fixture metadata.
Separate Lock/Examine queries record sizes, keys, protection and timestamps;
known lengths, the fixed date stamp and protection bits are additionally
checked. Separate original-DOS Fault calls supply diagnostic text. Expected
rows and return/error contracts are declared before execution, rather than
learned from either command image. Multi-entry checks require exact row
multisets because directory order belongs to the native handler; every
baseline/candidate pair additionally has identical complete output bytes,
including ordering. This does not claim sorting or original Workbench parity.

The auditor independently checks raw requests and binary receipts against
host JSON. Four negative controls reject wrong return, IoErr and row contracts,
and JSON output that differs from the raw receipt; original evidence is
restored byte for byte afterward. The host's completion flag alone cannot pass
this gate.

## Cleanup and repeated invocation

Both LoadSeg instances stay loaded while calls alternate between them and
between successful, filtered, error and redirected-output cases. The final
20 calls per CPU alternate both images at 4096- and 8192-byte RunCommand stacks.
Every call restores selected input/output, current directory, CLI, parent stack
size/base/bounds, allocated signals and DOS library open count. Every final
repeat has zero AvailMem delta. Earlier per-call reductions of 48, 144 and
208 bytes are retained as observations of initial handler/buffer growth, not
credited as proof of either a command leak or complete OS resource reclamation.

TO cases require the selected-output capture to remain empty and read the
closed redirected file through original DOS. Pending Ctrl-C uses public
SetSignal, with the previous break signal restored after each invocation;
keyboard and mid-read interruption are not exercised.

## Independent WinUAE check

WinUAE 6.0.3.0, JIT disabled, repeats QUICK, NODATES metadata
and TO redirection for both images on all three CPUs. The original Kickstart
Shell runs with a 4096-byte stack and a disposable directory filesystem.
Exact outputs and all six Shell return/Result2 records per CPU pass at 0/0.
This independently changes the CPU/engine and filesystem provider boundary;
it is narrower than the complete OFS/RAM corpus.

## Images and evidence

| Image | Bytes | SHA-256 |
| --- | ---: | --- |
| Preserved baseline | 10,232 | `faf639a602c4bbd97efab476d1046b83b14cde18e43d15f55d48f897c5fce4be` |
| Installed candidate | 4,684 | `4a906e81b92ab2d0187cb590d6af9ebc86dced90c98c8ce8e4a92b7f8b812450` |

The candidate is byte-identical to `out/C/List` and the previously qualified
normal-build image. No image was rebuilt or refreshed for this OS qualification.
[The size checkpoint](list-loop-phis-20261016.md) records the default policy,
independent compiler comparisons and supplied-vector qualification.

[The JSON report](list-os-qualification-20261016.json) binds the frozen runner,
SDK/compiler inputs, authored guest/host, request corpus, binary receipts,
final disk hashes, native observations, independent WinUAE results and auditor
controls. Raw evidence is `artifacts/list-os-qualification-20261016`.
Reproduction instructions are in
[ListOsQualification](../../../tools/Commands/ListOsQualification/README.md).

The runner and launcher PDB IDs match their frozen modules, and all ten of
their source documents match. Of 50 engine documents, 46 match and four differ:
LightweightA500Machine, LightweightCopper, LightweightDiskDma and generated
AssemblyInfo. Therefore this qualifies the exact frozen engine binary, not
the current dirty CopperScreen source checkout. Runtime versions and binary
hashes are retained in JSON. ROM bytes remain an external user-owned input.
No primary compiler or CopperScreen checkout was modified.

## Remaining limits

This closes the recorded finite List original-DOS gate. It does not establish
original Workbench command/option parity, actual MorphOS runtime behavior,
shipping/PURE admission, loaded-segment immutability, absolute minimum stack,
overflow canaries, a general leak proof or a speed improvement. Real
allocation/read/write failure injection and separate stderr remain outside
this corpus. No production behavior changes, assertion weakening or golden
output replacements were needed. This report records qualification before the changes were committed.
