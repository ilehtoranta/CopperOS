# Relabel contract

Profiles: wb31 and morphos320. Goal steps: CC01 and CC12. Updated: 2026-09-17.

FFS name-boundary follow-up (2026-09-17): original and replacement both accept
thirty name bytes (0/0), but thirty-one bytes, empty NAME and `Left/Right` reach
DOS unchanged and fail in the handler (20/210, `object name invalid` plus LF).
Intermediate sector observations prove rejection leaves the original label
intact. All four runs then recover to SavedDisk at 0/0 with intact payloads,
metadata and balanced resident ownership. Eight boot TRXs and sixteen
corruption controls pass: `artifacts/relabel-name-boundaries-20260916-v2/`.
Do not replace this with frontend truncation/sanitization or infer other
filesystems' behavior. See [boundary evidence](../relabel-original-dos-boot.md).

Cold-remount follow-up (2026-09-16): fresh ROM guests now mount each exact
persisted output with DF1 write-protected, resolve SavedDisk:, and read the
proof plus all 1792 bytes of the nested guard. Neither loads or invokes Relabel,
and all disk bytes remain unchanged. Evidence:
`artifacts/relabel-cold-remount-20260916/qualified/comparison.json`; twelve
corruption controls pass. This closes the bounded DOS1 cold-readback obligation,
not automatic discovery, OFS, other platforms or full PURE/package admission.

Persistent-volume follow-up (2026-09-16): both Workbench binaries now pass
device-name and volume-name relabels on a writable owned DOS1 floppy. Actual
exported sectors retain the final label; only the root block's label/date/
checksum fields change, with all file bytes, metadata and allocation preserved.
Both return 0/0 with empty diagnostics, unlike RAM's observed success IoErr 210.
The command must preserve handler-dependent error state. Evidence:
`artifacts/relabel-persistent-20260916-v6/qualified/comparison.json`; twelve
corruption controls pass. The fixture uses original Mount and a RAM-backed ENV:;
it does not prove automatic DF1 discovery, cold-remount or hardware DMA. See
[persistent FFS evidence](../relabel-original-dos-boot.md). These two additional
FFS paths do not close full platform, PURE or shipping gates.

Latest integration: the unchanged Workbench 68000 replacement and original each
pass eight original-DOS/Shell boot cases, four volume-label readbacks, four exact
diagnostic readbacks and sequential resident registration/reuse/removal. See
[paired boot evidence](../relabel-original-dos-boot.md) for the complete scope,
provider limits and twelve evidence-rejection controls. The earlier supplied-
vector results below remain distinct; full PURE and shipping admission are open.
Eleven further paired argument/help cases now pass, including duplicate keyword
rejection, quote/star escapes, empty NAME and real ReadArgs `?` continuation/EOF.
The protected-DF0 case now also passes: after a boot-time input-service
forwarding/reinstallation fix, production host-keyboard events reach the guest
handler and original EasyRequestArgs returns zero. Both commands finish at
20/214 with exact fault output, balanced cleanup and subsequent help/recovery.
The final receipt is `artifacts/relabel-input-forward-fixed-20260914/qualified/
comparison.json`. This reaches twenty distinct paired scenarios; the earlier
incomplete captures remain failed evidence. A fresh baseline also passes under
the fixed runtime. Eighteen device tests and twenty extended/cancellation plus
twelve baseline corruption controls pass. Original purity classification,
concurrent lifecycle, further platform/mode coverage and packaging remain open.

Concurrent follow-up (2026-09-14): both binaries now pass a real two-task,
one-segment overlap. The protected invocation holds its parser/storage while
another task renames RAM, then Cancel and parent recovery complete. Each caller
owns distinct live RDArgs; the replacement's two allocations per caller remain
disjoint and all six allocations are freed by their owner. Code remains
unchanged and segment removal/free occurs only after all three returns. The
receipt is `artifacts/relabel-concurrent-20260914/qualified-v2/comparison.json`;
twelve corruption controls pass. Active registry changes, task death, broader
concurrency/platforms and original PURE classification still require evidence.

Active-removal follow-up (2026-09-15): ordinary Resident REMOVE is now exercised
while the background call remains active. Both binary runs preserve the same
registry node and segment after refusal, complete the overlapping RAM invocation
and recovery, then remove/free the segment after all returns. Resident returns
5/202 with `object is in use` plus LF on the first attempt and 0/202 on retry;
successful removal preserves the observed ambient error. The receipt is
`artifacts/relabel-active-remove-20260915-v2/qualified/comparison.json`.
Twenty corruption controls pass. Forced removal, active replacement, task death
and full original classification/platform/package coverage remain open.

Active-replacement follow-up (2026-09-16): the unchanged original Workbench
Resident rejects `Ed C:Ed REPLACE PURE` at 10/202 before loading or mutating the
registry. Both Relabel binaries retain the same node at count 2, complete the
RAM overlap, Cancel and recovery, and finally remove/free at count 1 (0/202).
The receipt is `artifacts/relabel-active-replace-20260915/qualified/comparison.json`;
21 corruption controls pass. This differs from ordinary REMOVE's 5/202 and
qualifies no new behavior of CopperOS's MorphOS Shell-owned Resident command.
Successful replacement, forced removal, task death and full-profile admission
remain open; see the boot report for executor hashes and scope.

Idle-replacement follow-up (2026-09-16): successful REPLACE/PURE now loads a
second copy before freeing the first, retains the same registry node at count 1
and returns 0/0. One invocation uses the old image; two reuse the new image.
All return 0/210 with matching DOS operations, payloads and owned cleanup;
final REMOVE returns 0/202 and frees the second image. Both relocated image
hashes and load/free lifetimes are verified separately. Evidence is
`artifacts/relabel-idle-replace-20260916/qualified-v2/comparison.json` with 24
corruption controls. Failed-load retention, other applicable lifecycle cases,
original classification, launch/platform and packaging remain open.

Failed-replacement follow-up (2026-09-16): both missing-file and invalid-HUNK
source paths retain the same idle registry node and loaded image, allow two
further successful Relabel calls, and finally remove/free once. Missing source
fails at Lock/205; the existing text file reaches LoadSeg/212 and FreeArgs/212,
but original Workbench Resident reports 5/205 with `object not found` plus LF
in both cases. This error mapping is preserved as original-helper evidence,
not applied to the separate MorphOS Shell command. Receipts are under
`artifacts/relabel-failed-replace-20260916/{missing,invalid}/qualified/`;
56 corruption controls pass. Other loader failures, task/process lifecycle,
original classification, launch/platform and package gates remain open.

Caller-retirement follow-up (2026-09-16): after the background command returns,
original DOS/Shell reaches terminal RemTask(NULL), with no active command calls
and resident count 1. Both fresh boots switch away, leave the caller absent from
ready/wait lists, then execute parent recovery on the retained segment and
finally remove the same node. Evidence:
`artifacts/relabel-caller-retirement-20260916/qualified/comparison.json`;
20 retirement and 21 overlap/refusal corruption controls pass. This observes
normal self-removal under host Exec takeover; complete task-storage reaping,
outstanding-resource termination and forced task death remain open.

Caller-memory follow-up (2026-09-16): four captured tc_MemEntry spans (3520
rounded bytes), including Process/stack storage, are now bound to exact FreeMem
calls and complete free-chunk coverage immediately after each return in both
boots. Later allocator reuse is not mistaken for missing cleanup. Original ROM
RemTask uses host FreeMem and still runs on the retiring user stack before its
terminal switch; this does not waive separate host reaper safety requirements.
Evidence: `artifacts/relabel-caller-memory-20260916-v2/qualified/comparison.json`,
with 17 memory, 20 retirement and 21 overlap/refusal controls. The broader
process-resource ledger, forced termination, host RemTask native proof and
remaining original classification/platform/package gates stay open.

## Workbench correction from original execution (2026-09-13)

The Workbench body is now independent. The shared MorphOS body described below
was a historical candidate, not classic behavior evidence. Executing the pinned
Workbench Relabel 37.2 disproved that assumption. The original is 584 bytes,
SHA-256 `163ea95df394d2c800a161befae0bea664cb6b8a07dffc2b2b1af3141588a3ff`,
extracted from the inventory's verified Workbench ADF into the private path
`D:/TestData/CopperOSCommands/Workbench31/Relabel-37.2.hunk`. Original bytes and
disassembly are not copied into the repository.

Both original and replacement execute native instructions against the same
supplied public DOS/Exec responses. Observations establish:

- `ReadArgs("DRIVE/A,NAME/A", cleared two-LONG array, NULL)` and DOS version 36.
- A colon anywhere in NAME prints `':' not legal character in volume name`
  plus LF, returns FAIL 20 and leaves the ambient error subject to output/cleanup.
- Lookup removes the final DRIVE byte even when it is not a colon. For example,
  `DH0` looks up `DH` and, if found, passes `DH:` to `Relabel`.
- List locking uses Read + Devices + Volumes + Assigns (29), and `FindDosEntry`
  uses Devices + Volumes + Assigns (28). Unlock precedes mutation. The original
  also calls Find and Unlock after a supplied null LockDosList result; these two
  injection cases prove control flow, not that real blocking LockDosList fails.
- A missing entry prints `Invalid device or volume name` plus LF and returns
  FAIL 20 without clearing ambient IoErr. A found entry appends `:` to the lookup
  string and calls DOS `Relabel` with the supplied NAME. Nonzero handler returns
  are success, including negative values; zero prints `PrintFault(IoErr,NULL)`
  and returns FAIL 20. Handler, diagnostic and FreeArgs error timing is preserved.
- Parser failures print `PrintFault(IoErr,NULL)` and return FAIL 20. Failed DOS
  open returns 20 with process Result2 122. Successful parser storage is freed
  once, before DOS close; its cleanup may replace the final error.

The implementation calls public Kickstart APIs, uses invocation-owned result and
scratch buffers, and makes no image writes. It deliberately keeps borrowed parser
storage unchanged. The original removes a byte in that storage; `DriveAtFree`
records this difference separately. Equivalence compares return/error values,
diagnostic bytes, and ordered list/mutation/diagnostic operations. Private
allocation, error reads/restoration, and parser-buffer contents at release are
excluded from that ordered comparison. This normalization does not hide a changed
lookup or handler argument: both are compared byte for byte.

For an empty DRIVE, the replacement produces the observed empty lookup and `:`
handler argument without reproducing the original's write before the string.
The original copies into a fixed 128-byte stack buffer; oversized input is not
executed against it. A generated-only 255-byte DRIVE case checks dynamically
allocated storage instead. Both new allocation-failure paths return FAIL 20 /
NoFreeStore 103 with one null-header fault, balanced ownership, and no list or
mutation calls. These are explicit safety/ownership differences, not original
overflow or allocation-path parity claims.

The current receipt is
`artifacts/relabel-wb31-reference-20260913-qualified/qualification.json`:

| CPU (both binaries) | Replacement bytes | SHA-256 | Reachable methods | Comparisons | Generated-only cases |
| --- | ---: | --- | ---: | ---: | ---: |
| 68000 | 1748 | `60726c52a74d4b046f020ac51cc7fedcc1a21b3b870e5aeacfd47b08c9760df1` | 5 | 38 | 3 |
| 68020 | 1776 | `ea1bdece28245e1006a2ff3339c8db757be57ef9662e946845f209c59a6d9993` | 5 | 38 | 3 |
| 68040 | 1748 | `60726c52a74d4b046f020ac51cc7fedcc1a21b3b870e5aeacfd47b08c9760df1` | 5 | 38 | 3 |

That is 114 original calls, 123 generated calls, and 114 comparisons. Cases cover
drive/name boundaries, spaces and Latin-1 bytes, lookup flags and absence,
handler/parser/library failure, negative BOOL success, diagnostic output failure,
and error changes during output, list and cleanup calls. Each side reuses one
protected loaded image for sequential calls. Guards verify stack restoration,
library/argument/allocation ownership and unchanged code. Reports bind original,
generated, executor and CPU assembly hashes; the qualification manifest also
binds each runtime report hash. The driver rejects populated output directories.

Historical failures remain in `artifacts/relabel-reference-20260913/`:
`baseline-68000.json` has 18 original/candidate observations; `baseline-68000-v2.json`
stopped when a fixture incorrectly treated a null list result as a held lock;
`baseline-68000-v3.json` completes 30 pairs and records the shared candidate's
differences. Earlier candidate and expanded receipts are retained separately.

This does not qualify real DOS ReadArgs/help/quoting, fault rendering, actual
assign/volume mutation, Workbench launch, concurrent resident registry/lifecycle,
original PURE classification, packaging or full differential acceptance. MorphOS
packed-binary correspondence is still open. No shipping or PURE gate closes.

## Historical MorphOS source and initial Workbench candidate (2026-09-12)

Status: partial MorphOS release-source grammar and ownership evidence plus a
bounded Workbench syntax candidate; no packed-binary or runtime-parity claim.
Official 3.20
`c/relabel/relabel.c` is 2,254 bytes with SHA-256
`3b22423c47a4b9de57ce57945d0134c1a0fc182b1b7d64e54a32e822587bef44` and
invokes DOS ReadArgs with:

```text
DRIVE/A,NAME/A
```

Source inspection observes these candidate MorphOS behaviors:

- NAME containing `:` prints `':' not legal character in volume name` and does
  not call Relabel.
- DRIVE must be nonempty and end with `:`. The source removes that final colon,
  takes a combined read lock over devices and volumes, calls `FindDosEntry`,
  and unlocks before mutation. A missing entry or invalid syntax prints
  `Invalid device or volume name`.
- A found entry calls the public DOS `Relabel` vector. Success returns OK;
  vector failure prints its IoErr fault. ReadArgs storage is released before
  closing DOS.

The source does not prove packed-binary correspondence, Workbench grammar,
case/name normalization, exact error stream/result behavior, mutation effects,
resident lifecycle, source reuse rights, or parity. The MorphOS body must use
public DOS `LockDosList`/`FindDosEntry`/`UnLockDosList` and `Relabel`, release
the lock before mutation, and never map volume state to host paths.

`NativeMorphOSRelabelCommand` now independently implements that public-vector
sequence with invocation-owned parser and scratch storage. The private entry
has eleven reachable methods and passes eight supplied parser/list/mutation
vectors on each target CPU, with no managed runtime features/helpers, external
targets, exception regions, fatal fault sites, leaked resources, or shared-image
writes. The current hash-bound resident HUNK receipts are recorded in
`artifacts/relabel-morphos-native-20260912-qualified/qualification.json`:
68000, 2,804 bytes,
`b5e0042756c36a110dd7a6c3520ee22d42d2b1e0fa79ba2d0226c65ff9cb95bc`; 68020,
2,848 bytes, `a1692cd2b8d54fc6142d96cc2505486102c4963b7ced334f2ae422aa315c139d`;
and 68040, 2,804 bytes,
`706dc3e09bb92671f8e91766ebb682f1a36a3cedbd24bfda74b60fad78f4ff7b`.

These are supplied-vector resident receipts only. Packed correspondence,
Workbench behavior, real parser and mutation/handler behavior, packaging, and
reference comparisons remain open.

The Workbench 3.1 candidate now has a separate DOS 36 resident entry,
`src/Commands/Native/NativeWorkbench31RelabelCommand.cs` with startup adapter
`tests/Commands.AddBuffersNativeRoot/Workbench31RelabelEntry.cs`. It preserves
the observed classic `DRIVE/A,NAME/A` boundary and shares the bounded
public-DOS list/Relabel body. The receipt is
`artifacts/relabel-wb31-native-20260912-qualified/qualification.json`:

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 2872 | `8d1910a3b3c5538574f927ad66cc1bd46c1f7a8ccdcae07b0374816a4b09e0ca` | 12 | 8 |
| 68020 | 2916 | `feca1ed4e35572437254797e332eb915f0815776f33970a4d275b3568279e908` | 12 | 8 |
| 68040 | 2872 | `8d748794bd68425bb17ab4027c437ffc93b945da74347875d4a7f90bd6a1751e` | 12 | 8 |

This remains a syntax/body candidate only. Exact Workbench diagnostics and
mutation behavior, original guest parity, PURE/resident reuse, packaging, and
differential comparison remain open.
