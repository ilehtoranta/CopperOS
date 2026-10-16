# Version loop-phi spill investigation — 2026-10-17

**Fixed the allocator's overly aggressive address-copy coalescing under the
opt-in SimplifyLoopPhis policy.** Both Version profiles pass. Shipping Version
falls from 8,676 to **8,580 bytes**, so the pass is now enabled for Version.

## Why removing phis caused more spills

Removing trivial loop phis correctly reduces the number of SSA values:
ReadCommandSegmentVersion goes from 289 to 236, and TryParseDirectFileVersion
from 255 to 196. The problem was a later allocation decision.

The allocator unconditionally merged an address-only temporary with its source
when the two did not interfere. For a short-lived memory operand this saves a
register move. When phi removal exposes one long-lived invariant, however, the
merge restricts its entire live range to address registers. For pointers live
across calls, reserved resident state in A5 and call clobbers of A0/A1/A6 leave
only **A2–A4**. Data registers D2–D7 could hold the pointer, but the merged group's
register class no longer allows them.

Allocation traces show the extra spills occur in these restricted pointer
groups. ReadCommandSegmentVersion spills three groups instead of two on its
first allocation iteration. TryParseDirectFileVersion spills two groups first,
then a third on its next iteration, instead of one group. Map spill counts count
SSA members of those groups, not that many independent source variables.

The temporary trace instrumentation is not part of the fix. Its off/on images
are byte-identical to the preceding controlled comparison, binding the observed
allocation behavior to the actual failing size experiment.

## Fix

When SimplifyLoopPhis is selected, address-copy merging now uses the existing
conservative pressure test. It can retain a short address copy rather than
restricting a long-lived source and spilling it. Low-pressure merges still
coalesce. Policies without SimplifyLoopPhis retain their previous behavior;
there is no compiler-wide default change or function-name special case.

The change is in `M68kRegisterAllocation.cs`, with new
`LoopPhiAddressCoalescingTests.cs` regressions. Development and qualification use
the isolated `CopperSharp68k-version-phis-20261017` checkout at `55628a0`, already
containing the helper-continuation fix. The matching guard was ported into the
primary CopperSharp checkout while preserving its other ongoing allocator
changes byte for byte. Those unrelated changes are outside this qualification.
The [portable patch](version-loop-phi-spills-20261017.patch) contains only this
fix and its new tests, against the committed compiler baseline.

## Targeted results

All columns use identical frozen command assemblies and SDK dependencies.

| Helper | Phi off: bytes / spilled SSA members | Old phi on | Fixed phi on |
| --- | ---: | ---: | ---: |
| TryParseDirectFileVersion | 360 / 5 | 408 / 15 | **358 / 0** |
| ReadCommandSegmentVersion | 408 / 8 | 440 / 17 | **380 / 0** |

Both fixed helpers have **zero reloads** and allocate in one iteration. The
spill-byte totals drop from 12 to zero in both. TryParseDirectFileVersion's
frame falls from 32 to 20 bytes; ReadCommandSegmentVersion's from 12 to zero.
Register-save costs remain included in complete image and execution results.

| Image | Original compiler, phi off | Original compiler, phi on | Fixed compiler, phi on |
| --- | ---: | ---: | ---: |
| Shipping Workbench | 8,676 | 8,684 | **8,580** |
| Private MorphOS | 27,640 | 19,920 | **19,804** |

The compiler change alone saves 104 shipping bytes and 116 private-profile
bytes. Enabling the complete fixed policy saves **96 installed shipping bytes**.
The private MorphOS profile is an MC68000 fixture, not a PPC size comparison.

## Qualification and execution costs

- **1,553 focused compiler regressions pass**, including allocator, machine
  optimizer, generated-size, resident execution, helper continuation,
  instruction-dataflow and branch-relaxation coverage.
- The seven new cases cover four to six flexible pointers live across calls,
  safe low-pressure coalescing, genuine address-only pressure, clobbers and
  reserved A5. The **three high-pressure cases fail without the fix**; all seven
  pass with it.
- Version runs all **93 shipping and 94 MorphOS cases per CPU**, on 68000,
  68020 and 68040. Three images per profile produce **1,683 invocations**.
  Both controls independently pass, and the candidate agrees with each in
  output bytes, return codes, IoErr, declared allocation/release ownership,
  supported repeated/interleaved cases and image immutability.
- Representative shared-pass checks also pass for Dir, List and Search on all
  three CPUs: 504 invocations across baseline/candidate images. Their controlled
  image sizes are 6,216 → 6,208; 4,700 → 4,652; and 5,396 → 5,300 respectively.
  Their installed images are unchanged; these measurements use the isolated
  compiler rather than the earlier dirty-toolchain inputs.

Per-CPU Version measurements (identical across the tested CPU modes):

| Profile | Phi off instructions | Old phi on | Fixed phi on | Peak stack off / old / fixed |
| --- | ---: | ---: | ---: | ---: |
| Shipping | 370,984 | 353,642 | 353,715 | 780 / 780 / 784 |
| Private MorphOS | 3,572,993 | 3,134,276 | 3,239,444 | 1,568 / 1,520 / 1,524 |

Relative to the previous phi-enabled candidate, the fix adds 73 shipping
instructions and 105,168 MorphOS instructions (3.36%), plus four peak-stack
bytes in each profile. It still executes fewer instructions than phi-off in
both profiles. Size takes priority; all measured stacks remain within unchanged
configured limits. These are bounded fixture totals, not hardware timing.

## Reproduction, publication and limits

Evidence is under `artifacts/version-spills-20261017`. The
[JSON report](version-loop-phi-spills-20261017.json) binds compiler sources and
payloads, input hashes, response files, maps, allocation traces, tests, CPU
receipts, source publication and the complete accepted inventory.

```powershell
python tools/Commands/qualify_version_spill_fix.py audit `
  --stage artifacts/version-spill-fix-fresh `
  --compiler artifacts/version-spills-20261017/qualification/compiler/CopperSharp.Compiler.Cli.dll
```

The tool validates earlier frozen inputs, compares both original controls with
the new candidate, and requires passing profile checks and strict size savings
before `enable` or `publish`. Existing defaults and other pass selections stay
unchanged. Flags: MC68000 resident HUNK, YOLO exceptions, memory disabled,
symbols off and fixed-point peepholes. Copper68k 1.5.1 SHA-256:
`99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`.

A fresh default source build reproduced the qualified 8,580-byte shipping image
byte for byte, using the frozen compiler CLI without pass overrides. All 61
accepted images were staged and hash-checked; only Version was replaced,
preserving the other 60 images. No stale files were removed.
The installed total after publication is **225,240 bytes**.

This is supplied-vector qualification. New original-DOS, WinUAE/CopperScreen,
actual MorphOS runtime, PURE admission and minimum-stack checks were not run.
The full compiler suite and all other command/profile combinations were not
rerun. Prior List/Search native-OS receipts remain separate and do not qualify
their newly generated probe images under original DOS. Unrelated workspace
changes are preserved. The compiler fix is committed as 69d01d1 and pushed to origin/main.
