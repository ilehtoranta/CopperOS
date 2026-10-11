# Correctness checkpoint — 2026-10-11

Lock now passes a stack vector containing the drive pointer to `DOS.VPrintf`.
The native fixture dereferences that vector, checks lock/unlock format selection,
and covers unlock packet failure. List validates QUICK, KEYS, NODATES, and entry
type against the actual selected format. MakeDir consumes no argument cell for
its argument-free diagnostic.

The October 8 local receipts and compiler worktree were missing. Their report
remains historical. An isolated compiler worktree was restored from `1234d0b`;
the intrinsic-only Raw classification was reconstructed in both lowering paths
and committed separately. Seven execution regressions cover scalar, literal,
conditional, by-reference, by-value, embedded-field, and nonidentity getters.
They execute resident HUNKs on all three CPU models, at two relocation bases,
with repeated calls, boundary inputs, stack restoration, and all peephole modes.
Four regressions fail with the unfixed compiler; all seven pass with the fix.

Fresh verification:

- **1,313 focused compiler tests pass**, including Raw/CString execution,
  generated size passes, register allocation, instruction dataflow, branch
  relaxation, machine optimization, and guest-memory wrapper folding.
- **843 host tests pass** using the restored local SDK. The initially attempted
  package-based restore fails because preview package `0.1.0-preview.1-g20`
  is unavailable; no package-based pass is claimed.
- **405 native invocations pass**: Workbench Lock/List/MakeDir/Search and
  MorphOS Lock/List/MakeDir, each on 68000, 68020, and 68040. MorphOS profiles
  use private 68000 HUNKs from one frozen root assembly.
- All **61 installed command hashes** match the October 8 report. Fresh
  Lock (1,592 bytes), List (10,232), MakeDir (1,288), and Search (10,664)
  reproduce their installed images exactly. No installed image was changed.

The [JSON receipt](correctness-checkpoint-20261011.json) binds frozen inputs,
compiler and runner assemblies, installed images, maps, test receipts, and
native receipts. Local evidence is under
`artifacts/correctness-checkpoint-20261011`. The isolated compiler is
`../CopperSharp68k-search-pressure-20261011`; the unrelated primary compiler
checkout remains untouched. Builds were launched from CopperOS using SDK
10.0.401, bypassing the restored worktree's unavailable SDK 10.0.301 pin.

Reproduce in a new stage by running the focused compiler and host commands,
building the runner and private root against that compiler, staging
Lock/List/MakeDir/Search with `build-c.ps1`, then running
`verify_correctness_checkpoint.py --stage <stage> --compiler-root <root>`.
The verifier intentionally refuses to overwrite its frozen inputs or payloads.

Fixtures model DOS services. Original OS execution, minimum-stack execution,
full RawDoFmt semantics, legacy runtime execution, and the full compiler suite
remain outside this checkpoint. The historical claim of 1,665 tests was not
re-established; the fresh count above identifies the actual selected coverage.
