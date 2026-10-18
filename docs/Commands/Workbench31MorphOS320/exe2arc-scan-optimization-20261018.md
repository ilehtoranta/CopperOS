# Exe2Arc cross-window scan and generated-code optimization — 2026-10-18

## Result

**Accepted and installed: 14,652 → 14,096 bytes (556 bytes smaller, 3.79%).** On unchanged command assemblies, the cross-window case falls from **13,109,977 → 12,700,331 executed instructions (3.12% fewer)**. The complete 63-invocation vector suite falls from **13,843,861 → 13,388,640 instructions (3.29% fewer)** per CPU. Peak stack stays **940 bytes**; the cross-window case itself stays at 912 bytes. The same instruction counts pass on MC68000, MC68020 and MC68040.

This enables an explicit selection of existing compiler passes for Exe2Arc. No command C# implementation, validation predicate, algorithm, compiler backend or compiler-wide default changed. Exe2Arc has one MorphOS-derived command profile; there is no Workbench 3.1 counterpart size comparison. Its public DOS calls and startup paths remain qualified.

The installed candidate SHA-256 is `5adba4c7f2675a06d3fee446de9cb7138274e9b4e93b1972e91789fe62d4e7cf`. All 61 installed files were staged and hash-checked; only Exe2Arc changed. The installed total is **221,892 → 221,336 bytes**. This 556-byte saving is additional to the previous receiver-classification and source-repair savings.

## Why the scan costs so much

1. The fixture places the seven-byte RAR signature at offset **102,398**. The scanner examines **102,399 candidate positions**, including the match, across two overlapping windows. Ordinary small RAR fixtures find their signature at offset 32. The input is almost entirely `0x55`, so almost every signature test fails at its first byte. The cross-window case accounts for approximately **94.7%** of the original complete vector suite's instructions.
2. The format switch remains inside the candidate loop. For every candidate the generated code selects RAR/CAB/ACE/ARJ/LZH, forms the pointer/offset, stages arguments and calls the selected header probe. For RAR, that calls `TryValidateCandidate`, which calls `NativeExe2ArcIo.IsMapped` before any signature byte is read. This repeats file/window bounds, overflow and scratch-span checks and incurs three call/return and register-preservation layers on the common mismatch path.
3. The baseline loop contains redundant register transport around each probe call: three `MOVE.L` instructions to rotate two live values, followed by an `EXG` after the call. Loop-phi simplification removes those four instructions from the common path. The measured saving is approximately four instructions per unsuccessful candidate, plus setup/teardown changes.
4. The scalar inliner rejects these branching header/validation helpers. Explicit `AggressiveInlining` hints on the RAR helper, validation helper, and both together produced byte-identical executables; those experimental annotations were discarded. A compiler hint does not bypass the inliner's supported-body restrictions.

The source probe checks and call structure still account for most of the remaining cost. This is CPU scan work, not evidence of a window-boundary hang. The previous [WinUAE investigation](exe2arc-winuae-cross-window-qualification-20261018.md) independently completed the unchanged combined workload; the larger multi-chunk copy is much faster because its signature is near the start. Wall times are sensitive to host load and are not used to claim a percentage speedup here.

## Controlled compiler measurements

Every row uses identical frozen command CIL, SDK/dependencies and compiler payload. The baseline was reproduced byte-for-byte before trials. Individual rows below are MC68000 runtime measurements; only the accepted cumulative row received the full three-CPU and original-OS qualification.

| Selection | HUNK bytes | Cross-window instructions | Complete vector instructions |
|---|---:|---:|---:|
| baseline | 14,652 | 13,109,977 | 13,843,861 |
| phis | 14,404 | 12,700,368 | 13,401,273 |
| transport | 14,624 | 13,109,973 | 13,843,647 |
| memory | 14,632 | 13,109,948 | 13,841,763 |
| inline | 14,632 | 13,109,977 | 13,835,305 |
| all | 14,096 | 12,700,331 | 13,388,640 |

- `phis`: `SimplifyLoopPhis` alone.
- `transport`: `RemoveRedundantTransport,CompactPrivateStackCopies`.
- `memory`: `CompactGuestMemory,NarrowOperations`.
- `inline`: `InlineMemoryHelpers,InlineSingleUseMethods`.
- `all`: the measured cumulative selection below, also including unused-argument elimination, call clustering and incoming-home reuse. It does not mean every available optimization is enabled.

Accepted selection:

```text
SimplifyLoopPhis,RemoveRedundantTransport,CompactPrivateStackCopies,CompactGuestMemory,NarrowOperations,InlineMemoryHelpers,InlineSingleUseMethods,ElideUnusedRegisterArguments,ClusterInternalCalls,ReuseIncomingArgumentHomes
```

The forward scanner body shrinks **986 → 892 bytes**. Its reported spills fall **13 → 4**, reloads **18 → 10**, and pre-peephole stack instructions **107 → 91**. Its local frame remains **172 bytes**, and saved-register space remains 40 bytes. Other helpers account for the rest of the executable reduction. Allocation summaries are static compiler metrics, not dynamic execution counts. The JSON receipt includes full map summaries, rewrite counts and the saved disassembly identities.

Five small error invocations gain between one and five instructions: parser template failure (+1), parser result-allocation failure (+1), workspace-allocation failure (+5), missing FROM slot (+2), and the interleaved parser failure (+1). These costs are retained in the receipt. Every measured command observation is unchanged. No stack limit increases.

## Qualification

- **378 shipping-entry vector invocations:** 63 calls per image on each CPU. Exact output/payload bytes, return/IoErr, allocation ownership, handles, close-before-delete, poisoned cleanup errors, CLI/Workbench message lifecycle, preserved registers, immutable resident image, repeated calls and instruction-interleaved invocations pass. Requested stacks remain 4 KiB and 8 KiB; peak stack remains 940 bytes. The frozen vector runner uses Copper68k **1.5.1**, SHA-256 `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`.
- **342 accepted original-DOS native calls:** both images on all three CPUs, covering all 32 authored contracts for each image. Ten caller-state fields and repeat-call free-memory equality are checked, as are exact files, output, return/IoErr and output cleanup. Ten damaged-receipt/forged-JSON auditor controls are rejected.
- **366 independent WinUAE calls:** all 32 contracts for both images on all three CPUs, including the unsplit combined cross-window and multi-chunk batches. Raw requests are regenerated independently; configuration, ROM, guest, images and selected inputs are hash-checked before auditing raw receipts.

The original-DOS guest/host are unchanged copies of Search's frozen harness. The native interpreter is Copper68k **1.5.4-detroit-dev.2+ddc753000b492a15048ba6c16a06e03c0c160a63**; independent WinUAE is **6.0.3.0**, JIT disabled. The original Kickstart 3.1 A500 ROM SHA-256 remains `8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee`. Runtime/guest/toolchain hashes and source-binding limits are recorded in the frozen manifests and previous original-DOS reports.

The combined 68000 native multi-chunk batch reached its 3,000-frame limit before obtaining a final disk snapshot. That failed attempt is retained and excluded from acceptance. Both images pass separately on all CPUs within the same 3,000-frame/120-second limits; cross-window native cases are likewise split. Combined multi-chunk and cross-window runs both pass in independent WinUAE with the existing bounded 120-second timeout. The acceptance corpus and expected bytes were not weakened. The native call total is lower than before because large-file warm-ups were omitted from the separate boots; all 32 distinct contracts remain covered for each CPU and image.

## Remaining opportunities and boundaries

The present improvement removes unnecessary transport and improves encoding; it does not eliminate repeated validation or move the format switch out of the hot loop. Larger gains would require a qualified compiler transformation for branching helper inlining, loop unswitching, or hoisting checks whose invariance and alias safety are proven. Each must include preservation/caller costs and final HUNK size in its decision. Reusing the full mapped-scratch proof could also support a dedicated scanner kernel, but unchecked per-candidate probes would need an explicit proof and separate component qualification. None of those changes is enabled here.

Original-DOS instruction/peak-stack profiling was not added; instruction and stack measurements come from the protected vector harness. There is no actual MorphOS PPC/Workbench GUI launch, native fault injection or CopperScreen run in this follow-up. Workbench startup and allocation/I/O-failure behavior remain covered by the unchanged vector fixture. No compiler regression suite was needed for a new backend transformation because no backend code changed.

## Reproduction and delivery

- Frozen compiler payload is the same receiver-fixed toolchain associated with **CopperSharp `9eaa2aa`**, with matching frozen SDK/dependencies from the preceding runtime qualification. The unrelated current compiler workspace was not used or modified.
- Unchanged CIL and compiler trials: `artifacts/exe2arc-scan-20261018`.
- Original DOS: `artifacts/exe2arc-scan-os-20261018`; separate multi-chunk runs: `artifacts/exe2arc-scan-large-split-20261018`.
- Independent WinUAE: `artifacts/exe2arc-scan-winuae-20261018`.
- `tools/Commands/optimize_exe2arc_scan.py`: `freeze`, `trials`, `native`, `audit`, `publish`. Use fresh absolute stage paths. `native` now prepares separate cross-window/large native requests and combined WinUAE requests. For this historical main stage, pass `--large-stage` to `audit`/`publish`.
- Run `Exe2ArcOsQualification/confirm-winuae.ps1` with the native stage, fresh evidence root and its full default matrix before `audit`. The auditor uses the original frozen stage only as the startup/configuration reference; it checks the new images against the new native stage.
- Exe2Arc's project opts into the policy, with its exact selection in `CodeSizePolicies.props`. Effective MSBuild properties were checked. Explicit property overrides and compiler-wide defaults are preserved.

[Machine-readable measurements and qualification receipt](exe2arc-scan-optimization-20261018.json).
