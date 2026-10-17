# Exe2Arc runtime qualification — 17 October 2026

Shipping executable: **14,708 → 14,652 bytes**, saving **56 bytes**. Both repaired-source compiler images pass the supplied-vector suite on 68000, 68020 and 68040; the receiver-fixed compiler saves a further 28 bytes on identical repaired CIL.

## Defects exposed and corrected

- ZIP scanner used seek mode 2 for end-of-file. DOS `OffsetEnd` is 1; use the SDK constant. The host test adapter had also accepted the incorrect mode and now rejects it.
- Unknown-type and typed-no-match diagnostics passed the type string as the `VPrintf` LONG argument vector. They now use `PutStr` directly. The strict fixture rejects the old binary for reading its first string bytes as a guest pointer.

- C-style `\033` escapes in C# diagnostic literals introduced embedded NULs, truncating the messages. They now use `\u001b`. A literal-only mutant independently reintroduces this defect and is rejected on all three CPUs.

The previously proposed 14,680-byte image is rejected. The replacement is built from repaired sources. Six negative controls reject the old image on the seek/argument-vector defects; three additional controls reject embedded-NUL diagnostics. Full original-image output equivalence cannot be claimed: these paths were broken before this repair.

## Runtime matrix

| CPU | Invocations per image | Instructions control → fixed | Peak stack bytes control → fixed |
|---|---:|---:|---:|
| 68000 | 63 | 13,844,949 → 13,843,861 | 940 → 940 |
| 68020 | 63 | 13,844,949 → 13,843,861 | 940 → 940 |
| 68040 | 63 | 13,844,949 → 13,843,861 | 940 → 940 |

The suite executes the actual shipping entry through one protected, relocated resident HUNK image. It checks inert ZIP/ACE/RAR/CAB/ARJ/LhA/LZH payloads and auto-detection, output bytes and naming, exact parser/workspace/FIB allocation ownership and release, close-before-delete order, output/return/IoErr, deliberately poisoned cleanup errors, register/CCR clobbers at public calls, 4 KiB stack bounds and guards, Workbench message ownership, repeated calls and instruction-level interleaving. An 8 KiB peer stack is also exercised. ZIP Ctrl-C cases preserve the existing partial-output/count contract; other extractors retain their existing cancellation behavior.

All 221 portable component tests pass. The 61-command installed total changes from 221,948 to 221,892 bytes. Instruction totals fall by 3,264 across the measured matrix, with no peak-stack increase.

Exe2Arc has a MorphOS-derived implementation only; no PPC executable-size comparison or separate Workbench command profile is claimed. Workbench launch handling is included. Supplied DOS parsing and in-memory file vectors are used: this does not qualify original DOS/MorphOS image parity, filesystem handlers, original diagnostic text from PrintFault, or CopperScreen execution.

Runtime: Copper68k 1.5.1, SHA-256 `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`. Compiler payloads are the retained clean `69d01d1` control and the receiver-classification fix committed as `9eaa2aa`. Flags and frozen SDK/CIL/source hashes are in the JSON evidence. Exe2Arc size-policy settings are unchanged.

## Reproduction

Run `tools/Commands/qualify_exe2arc_runtime.py` modes `prepare`, `compile`, `vectors`, `publish`, `report`, with `--stage <fresh-absolute-directory>`. The complete installed inventory is preserved and hash-checked; only Exe2Arc is updated. Raw evidence for this run is under `artifacts/exe2arc-runtime-v8-20261017`. Earlier fixture/build iterations are preserved under the preceding Exe2Arc runtime stages. The initial 68040 fixture failure was a longword fetch starting at a half-line boundary before a library stub; the fixture now maps that bounded instruction-fetch span, retaining data-read, write and executable-PC restrictions.
