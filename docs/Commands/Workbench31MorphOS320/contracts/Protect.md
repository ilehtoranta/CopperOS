# Protect contract

Profiles: `wb31` and `morphos320`. Goal step: CC14. Recorded: 2026-09-12.

The MorphOS 3.20 C member is `c/protect/protect.c`, 5709 bytes, SHA-256
`03d20c48412bcfd71a481b4c2a74a35d601ce9d37c2a9f01ed338b4df4a12abe`. Its
version include records `Protect 50.6 (20.1.2021)` and identifies the AROS
derived implementation. The source template is:

```text
FILE/A,FLAGS,ADD/S,SUB/S,ALL/S,QUIET/S
```

`FLAGS` accepts `HSPARWED`; `R/W/D/E` are active-low protection bits and
`H/S/P/A` (plus the source's hold bit) are active-high. A leading `+` or `-`
selects add or subtract semantics, while `ADD` and `SUB` together are an
error. Without a leading mode, the command clears the low protection byte and
sets the selected flags while preserving higher bits. `ALL` requests recursive
AnchorPath descent and `QUIET` suppresses recursive progress output. The
source rejects volume/device targets without `ALL`, uses `SIGBREAKF_CTRL_C`,
and reports failed mutations as `Can't set protection for %s - ` followed by
the DOS fault.

The bounded native body is
`src/Commands/Native/NativeMorphOSProtectCommand.cs`, with the private
resident startup adapter in
`tests/Commands.AddBuffersNativeRoot/NativeMorphOSProtectEntry.cs`.
`tools/Commands/qualify_morphos_protect_native_entry.ps1` compiles the body as
resident HUNK for 68000/020/040 and runs the supplied DOS/Exec fixture. The
receipt is `artifacts/protect-morphos-native-20260912-qualified/qualification.json`.

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 5748 | `5b3cae4b355746cfec011aa61350f9582e0aef1879021c35e1780d09fe35df9d` | 13 | 14 |
| 68020 | 5788 | `4b12117bf74ffa5433673f0596cc5a5a63c91f59a3fa2043b645d91cf56d4bb6` | 13 | 14 |
| 68040 | 5732 | `413611058d43424bd83a0ec2d590daca55009893c4a9994441993583b4aef4d4` | 13 | 14 |

The supplied fixture covers parser/result ownership, active-low/high bit
mapping, replacement/add/subtract forms, recursive directory flags, quiet
output, mutation failure, no-match, break, invalid flags, and interleaved
calls. It is adapter evidence only. Exact Workbench behavior, volume/device
classification, soft-link descent, complete wildcard traversal, original
guest correspondence, PURE/resident lifecycle, packaging, and differential
comparison remain open.

The Workbench 3.1 syntax candidate now has a separate DOS 36 resident entry,
`src/Commands/Native/NativeWorkbench31ProtectCommand.cs` with startup adapter
`tests/Commands.AddBuffersNativeRoot/Workbench31ProtectEntry.cs`. Its observed
classic boundary is the same six-slot `FILE/A,FLAGS,ADD/S,SUB/S,ALL/S,QUIET/S`
template, and the bounded body is shared with the MorphOS profile. The receipt
is `artifacts/protect-wb31-native-20260917-qualified-v1/qualification.json`:

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 5816 | `d8fa7abf2aa4fc15e72193de3c793e55794ceea3a548ce1c0e0ff169157e2994` | 14 | 14 |
| 68020 | 5860 | `25a66c17553111234b08d3a29e32502d52e635f37ad772b75b61c371223ce70b` | 14 | 14 |
| 68040 | 5800 | `58b96a9488b1194c5f916a9f50e0997320e415c224e344da68b1ce6e7808ab13` | 14 | 14 |

This is a bounded syntax/body candidate only. Original Workbench guest
parity, complete filesystem and diagnostic behavior, PURE/resident reuse,
packaging, and differential comparison remain open.
