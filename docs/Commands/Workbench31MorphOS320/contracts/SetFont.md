# `SetFont` contract

## Media identity and syntax

The Workbench 3.1 member is the 1,092-byte `C/SetFont` Amiga HUNK from the
selected M10 Workbench disk. Its SHA-256 is
`c3e1e763b12fd4f710a414443facea49479aa723a94828828dbaee214fe4ad8c`; the
embedded version tag is `setfont 39.1 (2.6.92)`. The only syntax candidate
recovered from the original bytes is:

```text
NAME/A,SIZE/N/A,SCALE/S,PROP/S,ITALIC/S,BOLD/S,UNDERLINE/S
```

This is a syntax observation, not yet a frozen parser contract. The bounded
binary audit is recorded in
[`setfont-wb31-binary-audit-20260922.json`](../reference-captures/setfont-wb31-binary-audit-20260922.json).

## Required behavior boundary

The binary opens `dos.library`, `graphics.library`, `diskfont.library`, and
`utility.library` at version 37, parses the seven result slots with DOS
`ReadArgs`, constructs a `TextAttr`, and appends `.font` case-insensitively
when the name does not already have that suffix. It calls
`diskfont.library/OpenDiskFont`, obtains the current process console task, and
uses DOS `DoPkt` action `ACTION_DISK_INFO` (25) with a BPTR to `InfoData`. The
original reads the console `Window` pointer from `id_VolumeNode`; it applies
`graphics.library/SetFont` to `Window.RastPort` and updates `Window.Font`.
If `id_VolumeNode` is NULL, the original succeeds without changing a font or
writing output. These packet and structure observations are recorded in
[`setfont-console-window-audit-20260925.json`](../reference-captures/setfont-console-window-audit-20260925.json).
The captured Workbench HUNK calls `Exec.Forbid` before `SetFont`, conditionally
closes the previous font, stores the replacement at `Window.Font` offset
`0x80`, and then calls `Exec.Permit`; the replacement must preserve this
balanced protection window and ordering. The disassembly observations are
recorded in
[`setfont-console-forbid-permit-audit-20260925.json`](../reference-captures/setfont-console-forbid-permit-audit-20260925.json).
A successful replacement retains the new font for the console. The `ITALIC`,
`BOLD`, and `UNDERLINE` switches produce console escape output only when the
opened font's corresponding `tf_Style` bit is clear; `PROP` also allows the
opened font to be proportional, while a proportional result without `PROP` is
closed and rejected as `ERROR_OBJECT_WRONG_TYPE`. The HUNK's exact literals
and style bits are recorded in
[`setfont-style-output-audit-20260925.json`](../reference-captures/setfont-style-output-audit-20260925.json),
and the proportional-font check is recorded in
[`setfont-proportional-font-audit-20260925.json`](../reference-captures/setfont-proportional-font-audit-20260925.json).
The HUNK initializes `TextAttr.ta_Flags` to `0x43`; `PROP` adds
`FPF_PROPORTIONAL` (`0x20`), and `SCALE` clears `FPF_DESIGNED` (`0x40`).
Their combined values and code offsets are recorded in
[`setfont-textattr-flags-audit-20260926.json`](../reference-captures/setfont-textattr-flags-audit-20260926.json).

The font-ownership audit finds that the captured HUNK retains the newly opened
font on the successful no-window path: it enters shared cleanup without a
`CloseFont` call. The candidate preserves that observed path. The HUNK also
leaves the font open on certain post-open failures; the candidate closes an
untransferred font on failure to avoid a leak. This is a deliberate cleanup
difference pending original-guest comparison, not evidence that guest-visible
behavior is identical. The bounded disassembly is recorded in
[`setfont-font-ownership-audit-20260926.json`](../reference-captures/setfont-font-ownership-audit-20260926.json).

The replacement must use public Kickstart calls where they exist and preserve
the console-window ownership and old-font close order. The HUNK initializes
the return to `RETURN_FAIL`, preserves that result when `ReadArgs` fails, and
truncates `SIZE/N` to the `TextAttr.ta_YSize` word before rejecting values at
or below four with error 115. Thus the high 16 bits do not participate in the
size check; the word-level evidence is in
[`setfont-size-word-audit-20260926.json`](../reference-captures/setfont-size-word-audit-20260926.json).
OpenDiskFont failure and a
missing console task use error 205; a proportional font without `PROP` is
closed and rejected with error 212. A missing console Window succeeds without
changing the font or writing output. After output, the command returns success
without checking `PutStr` or `Flush` results. These control-flow observations
are recorded in
[`setfont-result-policy-audit-20260925.json`](../reference-captures/setfont-result-policy-audit-20260925.json).

## Open gates

The provisional resident native candidate compiles for 68000, 68020 and
68040 with no managed allocation sites, runtime helpers/features, exception
regions, external native targets, or fatal machine-fault sites. The static
receipt is
`artifacts/setfont-wb31-native-static-20260922-v1/qualification.json`.
This is compiler admission evidence only; no runtime fixture or shipping
profile is claimed.

A supplied native execution fixture now runs thirty-three invocations on each
of 68000/020/040. The refreshed receipt
`artifacts/setfont-wb31-native-entry-20260926-font-ownership-v20/qualification.json`
checks failure of the ReadArgs result array and each name/TextAttr/InfoData
allocation, together with the binary-derived `SIZE/N` low-word compare
boundaries, the `ACTION_DISK_INFO`/`InfoData` window lookup, successful
no-window behavior including retention of the opened font, `Forbid`/`Permit`
interval, and suppression of
requested style escapes when the opened font's `tf_Style` already carries the
style bit; it also checks `PROP` handling of proportional fonts, the recovered
result/IoErr paths, and failures from output/flush calls while preserving the
original success result, and independent `SCALE`/`PROP` flag mapping. Each CPU
image has eighteen reachable methods,
no managed allocation sites, runtime helpers or features, external native
targets, exception regions or fatal machine-fault sites, and zero shared-image
writes. The fixture covers parser/result ownership, library floors and failure
cleanup, `TextAttr` construction, escape output, startup guards and interleaved
callers. It is replacement-side evidence and does not establish the selected
Workbench guest handler's behavior or real console concurrency.

The exact DOS 3.1 handler behavior, window-pointer lifetime, guest output
side-effects, missing-library result levels, and actual diskfont reference
counts
need original guest captures. The HUNK and public layouts identify the packet,
fields, `PROP` acceptance rule, static escape literals, and `tf_Style` gating,
and recover command result levels from the control flow; they do not prove the
selected handler's effects or failing output calls' IoErr behavior. Result
observations are recorded in
[`setfont-result-policy-audit-20260925.json`](../reference-captures/setfont-result-policy-audit-20260925.json).
PURE/resident
classification, installed placement, package admission, and differential
comparison remain open. No shipping status is claimed by this contract.
