# `SetKeyboard` contract

## Media identity and syntax

The Workbench 3.1 profile is the 1,412-byte `C/SetKeyboard` HUNK observed on
the selected Workbench 3.1 M10 disk (`setkeyboard 38.4 (9.3.92)`, SHA-256
`ffb46c3b077872ea9b85620b6601e18ef08903c902b5a65110d7f96fa0b7d074`). The
captured template candidate is `KEYMAP/A`; this contract keeps that grammar
and does not invent options.

The MorphOS 3.20 profile is the 2,856-byte packed member
`MorphOS/C/SetKeyboard` (`SetKeyboard 50.4 (9.9.07)`, SHA-256
`f12a418b372b7e2d724a2ba56235e58bcd9135466c26fea8031e12f1af3c8fa4`). Its
verified at ISO extent 178200 with 2048-byte sectors. The payload begins with
the `7f4d4f53` MorphOS packed-native marker. A bounded unexecuted inspection
found no plaintext template or diagnostic candidates; the exact parser
contract therefore remains an open reference gate. The reproducible inspection
record is [`setkeyboard-morphos-packed-inspection-20260922.json`](../reference-captures/setkeyboard-morphos-packed-inspection-20260922.json).

The available MorphOS source gives a related but non-identical keymap path in
`C:IPrefs`: it compares the `FilePart` against `keymap.resource`, tries the
`KEYMAPS:` assign and then `MOSSYS:Devs/Keymaps`, recognizes resident and
extended keymap nodes, serializes the resource update with `Forbid`/`Permit`,
and updates charset preferences after selecting the default. The independent
MorphOS candidate below adopts only the keymap loading/publication portion;
charset preference writes remain deliberately outside the command body until
the packed entry path is observed.

## Required behavior

`KEYMAP/A` names a file in `DEVS:Keymaps`. The command joins the path with DOS
`AddPart`, checks the `keymap.resource` list for an already resident node,
loads a missing keymap through DOS `LoadSeg`, converts the returned BPTR to the
`KeyMapNode` at `BADDR(seglist + 1)`, opens `keymap.library` at V36 or newer,
and calls `SetKeyMapDefault`. A newly selected segment is intentionally kept
loaded: the keymap library and other processes retain pointers to the arrays
for the rest of the machine lifetime. A failed load or library open must
unload the private segment and preserve the DOS `IoErr` for `PrintFault`.

Workbench startup messages are rejected before `ReadArgs`; CLI invocation uses
the normal DOS `ReadArgs` lease and returns the parser's result level. The
replacement must stay resident-compatible and use only public Kickstart APIs;
no host path or managed allocation is permitted on the reachable native path.

## Current implementation checkpoint

The shared body is
[`NativeSetKeyboardCommand.cs`](C:/D-drive/Koodit/GIT/CopperOS/src/Commands/Native/NativeSetKeyboardCommand.cs)
and the Workbench resident entry is
[`Workbench31SetKeyboardEntry.cs`](C:/D-drive/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/Workbench31SetKeyboardEntry.cs).
The body implements the captured grammar, DOS path join, LoadSeg BPTR
conversion, keymap-library call, failure cleanup, and permanent successful
segment ownership. The repeatable three-CPU static receipt is
[`qualification.json`](C:/D-drive/Koodit/GIT/CopperOS/artifacts/setkeyboard-wb31-native-static-20260922-v2/qualification.json);
all reachable paths have zero managed allocation sites, runtime helpers,
external native targets, exception regions, and fatal machine-fault sites.
The executable boundary receipt is
[`qualification.json`](C:/D-drive/Koodit/GIT/CopperOS/artifacts/setkeyboard-wb31-native-entry-20260922-v2/qualification.json):
ten supplied vectors per CPU cover parser failure, keymap-resource reuse,
LoadSeg success/failure, library failures, startup, missing DOS, repeated
ownership and interleaving, with no shared-image writes or leaked resources.
Original-guest comparison is still required before this profile can ship;
MorphOS remains a separate source/packed correspondence task.

The MorphOS resident entry is
[`NativeMorphOSSetKeyboardEntry.cs`](C:/D-drive/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeMorphOSSetKeyboardEntry.cs),
with the source-informed body in
[`NativeMorphOSSetKeyboardCommand.cs`](C:/D-drive/GIT/CopperOS/src/Commands/Native/NativeMorphOSSetKeyboardCommand.cs).
It uses DOS 37, keeps the `KEYMAP/A` syntax as a provisional shared grammar,
and implements the released `IPrefs` `SetKeyMap` path: resident reuse,
absolute-path loading, `KEYMAPS:` then `MOSSYS:Devs/Keymaps` fallback,
resident/extended-node publication under `Forbid`/`Permit`, duplicate-race
recheck, `SetKeyMapDefault`, and private-segment rollback. The three-CPU
static and 12-vector-per-CPU boundary receipt is
[`qualification.json`](C:/D-drive/GIT/CopperOS/artifacts/setkeyboard-morphos-native-entry-20260922-v1/qualification.json).
It is source-correspondence and supplied-vector evidence only; the packed
MorphOS template, exact diagnostics, original guest behavior, PURE metadata,
package admission, and differential comparison remain open.

## 2026-09-25 MorphOS segment-scan safety checkpoint

The MorphOS candidate's Resident search now checks the size word, segment
address arithmetic and declared final byte through Exec `TypeOfMem`. It reads
the full six-byte Resident match/tag pair only when that pair fits inside the
declared HUNK, traverses ordinary multi-HUNK lists and rejects cyclic lists
with a constant-space cycle check. Receipt:
[`qualification.json`](C:/D-drive/Koodit/GIT/CopperOS/artifacts/setkeyboard-morphos-native-entry-20260925-hunk-safety-v2/qualification.json).
Sixteen supplied invocations per CPU cover undersized, overflowing, unmapped
and cyclic segment fixtures alongside the existing resource, load, publication,
cleanup and startup vectors. The resident HUNK remains free of managed runtime
features/helpers, external native targets, exception regions, fatal sites,
shared-image writes and leaked fixture resources.

The independent Workbench 3.1 entry regression still passes ten supplied
vectors per CPU:
[`qualification.json`](C:/D-drive/Koodit/GIT/CopperOS/artifacts/setkeyboard-wb31-native-entry-regression-20260925-morphos-hunk-safety/qualification.json).
Neither receipt proves real guest behavior or the packed MorphOS command's
parser and diagnostics. Keep both profile rows partial until their original
guest, resident metadata, installation, licensing and package gates are closed.
