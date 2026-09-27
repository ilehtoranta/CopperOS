# Assign contract

Profiles: wb31 and morphos320. Goal steps: CC01 and CC12. Recorded:
2026-09-04.

Status: media identity, resident-design evidence, and bounded public-vector
mutation candidates. The candidates are runtime-fixture evidence only; they do
not establish packed-binary correspondence or shipping behavior.

The selected Workbench 3.1 v40.42 `C/Assign` HUNK is 3,220 bytes with SHA-256
`2f58ca68d02a750a44b46e4b255e53cb244db7a9ce56ce40210594e46bd47a4e`.
It is the same observed member on both the selected Workbench and Install disks,
and identifies itself as `assign 37.4 (25.4.91)`. A raw-string scan finds this
candidate template at byte offset 1,940:

```text
NAME,TARGET/M,LIST/S,EXISTS/S,DISMOUNT/S,DEFER/S,PATH/S,ADD/S,REMOVE/S,VOLS/S,DIRS/S,DEVICES/S
```

It establishes only possible token spellings. It does **not** prove that the
binary calls `ReadArgs`, nor positional/keyword assignment, help interaction,
value ranges, conflicting switch precedence, diagnostics, return values, or
`IoErr` behavior. In particular, `/M` must be treated as a DOS-owned terminated
pointer vector if and only if a later captured command path establishes that
this template is actually consumed by `ReadArgs`.

The observed MorphOS 3.20 `MorphOS/C/Assign` is a 5,399-byte packed native
member, SHA-256
`abfa3f72739f19c48838aa1ec3eb91443ccf225dae242bbec63731bb6b77b1f2`, with
version tag `Assign 51.1 (5.4.05)` at byte offset 5,355. Its packed form yields
no template candidate in the safe raw-string inventory. Neither member has
been executed or decompressed.

The [MorphOS Library Assign reference](https://library.morph.zone/Shell_Commands/Assign)
independently records the MorphOS grammar as
`NAME,TARGET/M,LIST/S,DISMOUNT/S,DEFER/S,PATH/S,ADD/S,REMOVE/S,VOLS/S,DIRS/S,DEVICES/S`.
That documentation confirms the eleven option positions used by the native
candidate below; it does not establish the packed 51.1 implementation's
precedence, diagnostics, handler protocol, or `IoErr` behavior.

## 2026-09-17 - MorphOS mutation candidate

`src/Commands/Native/NativeMorphOSAssignCommand.cs` now implements the bounded
mutation path for the documented MorphOS grammar through the public DOS
`AssignLock`, `AssignLate`, `AssignPath`, `AssignAdd`, and `RemAssignList`
vectors. Its private resident entry compiles as clean MC68000, MC68020, and
MC68040 HUNK images; the receipt is
`artifacts/assign-morphos320-native-20260917-qualified-v1/qualification.json`.
The candidate deliberately fails closed for LIST, DISMOUNT and VOLS/DIRS/
DEVICES until the packed command is decompressed or run in a licensed guest.
This is native-boundary evidence only, not a shipping or packed-binary parity
claim.

The current independent runtime receipt is
`artifacts/assign-morphos320-native-20260917-runtime-v3/qualification.json`.
It passes nineteen supplied public-DOS invocations per CPU with balanced
parser/result ownership and no shared-image writes. The fixture covers the
same mutation and failure matrix as the Workbench candidate; it does not close
the packed MorphOS member's listing/filter or handler behavior.

## 2026-09-17 - Workbench mutation candidate

`src/Commands/Native/NativeWorkbench31AssignCommand.cs` now implements the
bounded Workbench mutation path using the public DOS `AssignLock`,
`AssignLate`, `AssignPath`, `AssignAdd`, and `RemAssignList` vectors. The
observed classic template is passed to `ReadArgs`; `/M` targets remain DOS
owned until the parser lease is released. Successful lock-consuming calls do
not unlock their transferred lock, while failed calls and comparison locks
are released by the invocation. The private resident entry compiles as clean
MC68000, MC68020, and MC68040 HUNK images; the receipt is
`artifacts/assign-wb31-native-20260917-runtime-v4/qualification.json`.

The supplied-vector fixture runs nineteen invocations per CPU, including
replacement, ADD, REMOVE, DEFER and PATH modes, multi-target lock ownership,
operation/lock failures, parser and result-allocation failures, invalid target
vectors, startup boundaries and interleaved callers. The resident HUNKs have
zero managed runtime features/helpers, external targets, exception regions or
fatal machine-fault sites, and the fixture reports no shared-image writes.

The original-ROM parser-edge probe reaches both images safely for two
unterminated-quote inputs and agrees on ReadArgs error 120, PrintFault 120,
FreeArgs(NULL), and zero filesystem calls. It also exposes an unresolved
post-run IoErr delta: the original image leaves a command-image-derived value,
while the native candidate leaves 120. Durable receipt:
`artifacts/assign-parser-edges-20260917/qualification.json`. This is evidence
for the parser call sequence only; it is not a shipping or full parity gate.

This is a source-informed implementation candidate only. It intentionally
fails closed for LIST, EXISTS, DISMOUNT, VOLS, DIRS, and DEVICES until their
original output, precedence, and handler behavior are captured. It is not a
shipping or MorphOS implementation and does not close the required runtime,
PURE/resident, lifecycle, packaging, or parity gates.

Assign is required-pure by observed design evidence. Classic setup scripts
contain literal resident add/remove operations for it at Install
`Update/Startup-HardDrive` lines 13/56 and Workbench `S/Startup-Sequence` lines
12/77. MorphOS `MorphOS/S/startup-sequence` likewise adds/removes it at lines
16/106; MorphOS `hdinstall.fixc` line 3 adds `P` as installer intent. These
are not evidence of the resulting installed protection bits, resident table,
or use-count lifecycle. Any implementation must remain reentrant and keep
parser records, name storage, target vectors, locks, list locks, output, and
error state invocation-local.

The current SDK exposes the public Kickstart DOS boundaries needed for a later
implementation: `AssignLock`, `AssignLate`, `AssignPath`, `AssignAdd`, and
`RemAssignList` (LVOs -612 through -636), plus DOS-list read locking and
assignment structures. A body must use these vectors and normal guest locks;
it must not edit DOS-list memory, model assigns as host paths, or implement a
separate resident registry. The present evidence does not identify which vector
applies to any candidate switch.

Before an Assign body is admitted, collect controlled original per-profile
captures for: listing, EXISTS, VOLS/DIRS/DEVICES filtering, one/multiple TARGET
values, replacement versus ADD/REMOVE, DISMOUNT, DEFER/PATH, empty/invalid
names and targets, incompatible switch combinations, handler/lock failures,
output/short-write failures, Ctrl-C, final result and `IoErr`. Capture each
case from disposable guest state and bind it to the exact source member.
Then implement the established public-vector path with `ReadArgs` where proven,
and qualify 68000/020/040 resident execution with interleaved callers and
resource-cleanup assertions. Packed correspondence, source-reuse rights,
installed metadata, real handler behavior, package placement, and parity all
remain open.
