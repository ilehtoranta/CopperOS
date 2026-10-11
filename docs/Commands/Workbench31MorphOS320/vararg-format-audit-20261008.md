# Raw accessor and formatting audit — 2026-10-08

The local experiment receipts and compiler worktree referenced below were absent
on 2026-10-11. This report preserves the historical findings; current focused
requalification is recorded in [the correctness checkpoint](correctness-checkpoint-20261011.md).

## Findings and fixes

**The `AmigaVarArg.Raw` bug predates the CString correction.** On identical frozen assemblies, compiler `9e61ea7` and the accepted CString compiler payload produce byte-identical failing scalar and literal reproductions. The valid symbol-free execution comparison fails with a stack-restoration mismatch for the scalar case and an out-of-range guest access for the literal case. Earlier symbol-enabled runs failed before execution and are excluded from evidence.

The compiler treated a parameterless `uint` getter named `get_Raw` as an intrinsic accessor. An ordinary instance getter requires the address of its storage; the shortcut instead supplied the contained scalar bits. Both local promotion and legacy argument/frame shortcuts now require an actual `intrinsic:*-raw` import. Getter names do not grant intrinsic semantics. A custom nonidentity Raw getter guards against reinstating that assumption.

**Lock had a separate `VPrintf` ABI bug.** It supplied the drive string itself as the argument vector. The old fixture also read that direct string and masked the error. Lock now supplies a private four-byte stack cell containing the string pointer. The fixture dereferences the vector, verifies the selected lock/unlock format, and includes unlock packet failure. All three CPUs reject the old shipping image with the unmapped pointer `$576F726B` (the bytes of `Work`).

List now explicitly verifies QUICK, KEYS, NODATES and file/directory format selection. MakeDir reads no argument cell for its argument-free `No name given` message. Both profile fixtures pass. The reviewed DosList, DiskFree, Dir, Rename, Copy, ModList and TaskList helpers consume the actual selected conversions; their applicable shipping suites pass. LoadResource's message/catalog handling was reviewed statically and its separate worker suite was not rerun.

## Measurements

| Image | Before bytes | Corrected bytes | Change |
| --- | ---: | ---: | ---: |
| Scalar Raw reproduction | 52 | 64 | +12 |
| Literal Raw reproduction | 84 | 92 | +8 |
| Lock | 1,568 | 1,592 | +24 |
| Complete 61-command set | 244,860 | 244,884 | +24 |

These are correctness costs. **The other 60 commands are byte-identical.** The compiler fix changes none of the 61 shipping command images when comparing identical freshly frozen assemblies; Lock's change comes from the source correction. Execute remains 6,168 bytes, List 10,232, Search 10,664, Reboot 516. The recorded genuine Workbench 3.1 Lock is 536 bytes, giving a corrected ratio of 2.970×. No comparison is made with PPC originals.

## Qualification

- **1,665 focused compiler tests pass**, retaining branch-liveness and size-pass regressions. Raw execution covers scalar, literal, conditional, reference, by-value, embedded-field and custom nonidentity getters, all available compilation targets, disabled/bounded/fixed-point peepholes, 68000/68020/68040 execution, two relocation bases and repeated invocation. Guest checks include stack restoration and reserved registers.
- **843 host profile tests pass.**
- Workbench and MorphOS Lock, List and MakeDir suites pass on all three CPUs. Applicable shipping formatting suites also pass. Receipts contain **1,443 successful native invocations** across the recorded runs; repeated qualification of the same image is included in that count.
- Corrected shipping Lock has 12 invocations per CPU and a peak stack of 160 bytes, within the unchanged 16 KiB fixture stack. Its local frame grows from 44 to 52 bytes, while saved-register storage falls from 32 to 28 bytes: four additional bytes in total. Per-case instruction counts, allocations/releases, output bytes, return levels and IoErr are retained in receipts. The old image cannot complete the corrected ABI fixture, so no matched execution-overhead claim is made.
- Actual emulator: Copper68k `1.5.1+43a3a84f79657cb4c2b32bb58d8e510e3aec9c15`, SHA-256 `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`.

## Inputs and publication

The earlier receipt's managed-input paths were missing. A complete fresh source build was staged, its assemblies/dependencies frozen once, and both compiler payloads run against those exact inputs with unchanged per-command policies. The accepted compiler reproduces all 60 unchanged shipping images; its corrected-source Lock image matches the candidate. Both payloads produce identical complete inventories. MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes and symbols disabled are retained. Exact compiler payload hashes, SDK identities, response flags, map metrics and input hashes are recorded in the JSON report and local experiment receipts.

All 61 final images and hashes were verified before `out/C` was refreshed. Previous output remains in the experiment's shipping snapshot. No stale command files required removal. The compiler's primary dirty checkout was preserved; work remains in `CopperSharp68k-wt-stack-size`.

## Limits and next work

This is focused resident-HUNK qualification. Other runtime profiles and legacy emission were not independently executed. Fixtures do not establish full RawDoFmt compatibility (Dir's string precision remains incomplete). Minimum-stack execution, original OS images, real DOS parsing/I/O and CopperScreen were unavailable. The full compiler repository suite was not rerun.

With these correctness issues addressed, the next size investigation is List/Search register pressure and aggregate transport. Establish sizes and stack costs before selecting further allocator changes.
