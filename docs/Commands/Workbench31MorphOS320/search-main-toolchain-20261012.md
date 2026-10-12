# Search default main compiler checkout, 2026-10-12

The main CopperSharp checkout now contains the qualified resident size-pass
history, including the intrinsic-only Raw getter fix and opt-in
SimplifyLoopPhis. Compiler merge commit
`eb04dac67d2ac2f6e5a41fde5254e56a808bc8d5` joins main `25a23d4` with the
qualified `02a7fa3` branch. Search builds and passes its finite correctness
gate using the normal command with **no toolchain or optimization overrides**:

```powershell
pwsh -NoProfile -File tools/Commands/build-c.ps1 -Command Search
```

Run it from the CopperOS root. It builds the compiler from the default sibling
`../CopperSharp68k`, publishes Search using its default pass policy and updates
`out/C/Search`. The resulting MC68000 HUNK is 5,428 bytes, SHA-256
`38444ca732d98dab5d2fe087b72863e9b02ee096d4998db4fdeddfd2a494f671`.
It is 32 bytes larger than the earlier isolated 5,396-byte candidate and
5,276 bytes smaller than the corrected 10,704-byte baseline. The main checkout
retains additional uncommitted compiler work, so this image was freshly
qualified rather than assumed identical to the isolated result.

| CPU interpreter | Native DOS invocations | Baseline + main-build vectors | Independent WinUAE strict output |
| --- | ---: | ---: | --- |
| 68000 | 64 | 22 + 22 | pass |
| 68020 | 64 | 22 + 22 | pass |
| 68040 | 64 | 22 + 22 | pass |

All 192 native DOS invocations match the previously qualified exact output and
return/IoErr contracts. The two batches per CPU retain the 3000-frame/120-second
limits, 4096/8192-byte RunCommand stacks, alternating loaded images, caller-state
restoration and zero AvailMem delta on the final 20 calls. The 132 supplied
vectors pass output, return/error, resource ownership and image-immutability
checks, including output failure and short-write cases. WinUAE independently
captures the exact numbered line `     2> Needle\n` and Shell return/Result2 0/0
on all three CPUs.

## Existing work and compiler checks

Before touching the compiler checkout, all 281 modified/untracked source files
were backed up along with the original index and tracked patch. Three-way
integration of the 39 incoming files resolved six overlaps in a prepared
directory. Of the backed-up files, 260 were restored byte for byte and 21 were
combined with incoming changes; the report rechecks every expected final hash.
The existing removal of the optional headless test-project reference was kept
as an uncommitted edit. The ordinary stash list was restored, with a recovery
snapshot retained at `refs/codex/backups/search-main-toolchain-20261012` and raw
backups in `artifacts/search-main-toolchain-20261012/compiler-before`.

The staged merge contained the qualified branch tree plus one imported test
fixture adjustment. Unrelated compiler/runtime/test work remains uncommitted.
The executed compiler is the composed working tree, not a claim that the clean
merge commit alone includes or qualifies that unrelated work. Frozen source
snapshots and compiler binary hashes make that boundary explicit.

The final selected compiler run passes **1,156 tests**, covering generated
code-size execution, loop-phi/carried-value and register constraints, resident
HUNK behavior, native pointer provenance and register allocation. The initial
run had one size-assertion failure: current allocation gives the positive
wrapper-based memory helper a saved register, and the inliner correctly
excludes stack-dependent bodies. The imported positive fixture now uses a
single scalar argument and the same fixed guest addresses. Its strict size
reduction, branched caller, repeated execution, all-CPU boundary values and
memory-access comparisons remain enforced. No production allocator or inliner
guard was changed to satisfy the fixture. Failed attempts remain in the raw
evidence alongside the passing final TRX.

The [JSON receipt](search-main-toolchain-20261012.json) binds the default build
command, main compiler source/binary snapshots, merge parents/tree, existing-work
preservation audit, exact native binary receipts, vector comparisons, WinUAE
results, installed Search identity and compiler tests. The verifier's
`--main-toolchain` mode requires the exact default command, validates the main
compiler payload and preserved source hashes, and independently checks native
results against the prior strict contracts.

Evidence is `artifacts/search-main-toolchain-20261012`. SimplifyLoopPhis stays
enabled for Search. This closes the earlier default-toolchain dependency;
original Workbench Search parity, actual MorphOS execution, shipping/PURE
admission, absolute minimum stack and payload speed qualification remain open.
The native DOS/WinUAE providers and frozen engine-source-drift limits are carried
forward from the earlier correctness checkpoint. CopperScreen was not modified.
