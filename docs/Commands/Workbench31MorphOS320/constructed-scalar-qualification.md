# Constructed scalar compiler qualification

Recorded 2026-08-30 for CC02. This is a compiler prerequisite for the
[native command work](completion-ledger.md), not a command implementation or
pure/resident shipping approval. The stable goal and command inventory are
unchanged.

## Defect and correction

The retained Exe2Arc component probe passed a directly constructed `APTR` to
`DOS.DOSLibraryBase`. The generated setter stored the address of the temporary
struct instead of its four-byte pointer value. The first scanner invocation
then fetched an instruction at `$0004FF9E`, with no DOS-vector call completed.
Using `APTR.FromPointer` avoided that expression in the old probe; it did not
repair the compiler.

The compiler's value-type construction path allocated a temporary and marked
every result as an aggregate address. The correction in
[CilMachineIrBuilder.cs](D:/Koodit/GIT/CopperSharp68k/Compiler/Backend/CilMachineIrBuilder.cs)
retains the real constructor call, then loads the completed payload for a
transparent scalar result. Other aggregate results keep their address
representation. This also preserves constructors that transform their input
or accept multiple arguments; it does not replace arbitrary constructors with
an identity conversion. No SDK, DOS adapter, command implementation, CPU,
optimizer or property-setter special case was changed by this repair.

## Source-matched comparison

The initial frozen compiler `814b2ade...` reproduced 84 failures and 25 controls
in the new 109-test suite. A wider selection initially gave 95 failures and
194 passes before, then 289 passes afterward. Those wider totals alone could
not isolate this patch: the PDB audit found eight current compiler files that
did not match the previously built DLL. The failed audit is retained; eleven
of those initial failures are not attributed to this repair.

A new compiler was therefore built from the captured, unmodified pre-fix
sources. Every captured source/settings file matches the after-build input
except the intended `CilMachineIrBuilder.cs` change. The same test DLL now
gives **84 failures and 205 controls before, then 289 passes after**, with zero
skips. The unchanged 180 existing tests all pass on both sides. A separate
after-build repeat also passes 289 tests.

The [new regression file](D:/Koodit/GIT/CopperSharp68k/Compiler.Tests/ConstructedScalarLibraryBaseTests.cs)
contains 108 native matrix rows and one CIL/managed fixture check. It covers
68000/020/040, fixed-point and disabled peephole optimization, and Resident and
Freestanding profiles. Eight scalar consumers use zero, one, ordinary bits,
high-bit values and all ones: direct/local/factory/returned library bases,
pointer conversion, a memory read, and transformed and two-argument scalar
constructors. A separate row calls the declared DOS IoErr vector and checks
the actual A6 base. The vector is supplied by the fixture.

Each successful focused matrix executes 492 native entries and 12 supplied
DOS-vector calls, totaling 9750 instructions. All expected values, input
memory and stack returns match. Resident images remain unchanged. No managed
allocation, runtime helper/feature or external native target is admitted.
These are compiler fixtures, not distribution commands or original DOS calls.

The existing selection covers value-type construction and heap rejection,
constructor inlining, aggregate returns, pointer conversions/ABI, out/ref
structs, nested address returns, RunCommand's stack bridge, argument homes,
narrow fields, guest-memory wrappers and external-call folding. It is a
selected regression set, not the entire compiler repository.

## Retained identities and audit

All private evidence is under
[scalarefeb4b48](D:/TestData/CopperOSCommands/Q/scalarefeb4b48).

| Input or result | SHA-256 |
| --- | --- |
| Pre-fix builder source | `a2ef6b782fd3d6bc74ad3a57b05c9e25e1810c20eef9fb0fda12e304318244de` |
| Fixed builder source | `3415a491efd6a0394c33f6f6b1d1b2ce9226fcefad3860b56a077450c7ee0d5d` |
| New test source, unchanged before/after | `a94206713bf145c4ef35c24714cd8ddc6409db53da539c3939daedf1fcdeef92` |
| Source-matched pre-fix compiler | `64d91dba4ebfb374b4bb8d6d953fef4ac14e736b344417142f656ab50f57d920` |
| Fixed private compiler | `dc9bff8402faa1791a3bbaa042d3fc3a8b81c771fe0b4e90527ad62c5e611938` |
| Unchanged inclusive test DLL | `a1a295c3bf5768d8462ead4add916a8c2735baab11ed972e7463f2300064c8e3` |
| Frozen CPU for compiler tests | `8b7510f64bb9184d0224c3d19f5f0d5f1cc0b18707eb9cbca2473db5b0886dcd` |
| Frozen SDK for compiler tests | `805ceb33d35882aa151a46cf720d715776456da5679ed33529a90713cce6128e` |
| [Source-matched receipt](D:/TestData/CopperOSCommands/Q/scalarefeb4b48/source-matched-receipt.json) | `38ffdb15a602b2315e2581158fd28905e48ca9ee5ad22cba0045e24b2c2b3605` |
| [Source/PDB audit](D:/TestData/CopperOSCommands/Q/scalarefeb4b48/pdb-source-matched-audit.json) | `f7c89ca2b9fe20ee458a9ccae81dd5cd04cbf5fbca31915599704ace75397a67` |
| Retained initial PDB mismatch report | `8d2cd2cf05a8ab2374d2e0b30fdc64bcfbeff5e9fe9054b6d93c5e841ed7975b` |

The source-matched receipt checks 435 files around both executions and 243
resolved compiler Compile/reference inputs. The related-build records bind
its actual source/reference paths separately. The supplemental PDB audit
matches five PE/PDB pairs and 192 document hashes to retained source copies;
PDB documents alone are not a complete compiler input inventory.

A separate [closing audit](D:/TestData/CopperOSCommands/Q/scalarefeb4b48/closing-audit.json)
compares all 108 newly emitted HUNKs with the after-build repeat: every pair is
byte-identical. It also verifies 136 local link targets in the five root-owned
qualification/progress documents and the unchanged goal hash. Its SHA256 is
`d34dd90d1deb93a6e57f1d578db81684cf1d5785345eb57d7947f0a2110976cc`.
These file checks do not extend the runtime or original-command coverage.

Initial and source-matched failing HUNKs, maps, compatibility reports,
execution observations, test DLLs, logs and TRXs are retained. A first private
related-test build lacked the existing MultiModule test assembly and failed
before execution. Its source/project/log remain; the subsequent private
overlay adds that explicitly captured dependency without editing or dropping
the test. Accepted private builds report zero warnings/errors.

No shared compiler output was rebuilt or replaced. Existing command-manifest
artifacts keep their own historical source/compiler identities; this repair
does not silently requalify them. Original reference execution, complete
command behavior and resident registry/packaging gates remain separate.

## Retained Exe2Arc reproduction

The original failed managed probe DLL is reused byte-for-byte, including its
direct constructed-APTR setter: SHA256
`4faddcea70c4a4ac730c83e92ecd81cdab191d493becf96aaaf54bf0e9d70646`.
Its historical SDK and Support dependencies are also retained. Neither the
managed probe nor the execution runner is rebuilt. The runner comes from the
separately qualified Exe2Arc checkpoint, including its already documented
instruction-prefetch padding; it is identical on both sides of this comparison.

With the source-matched pre-fix compiler, the first case again stops at
`$0004FF9E` after 267 instructions: one entry started, zero passed, no DOS
call completed. With the fixed compiler, all 77 planned component entries pass
on 68000, including eight interleaved callers. They execute 264671516
instructions and 243 supplied DOS-vector calls on one unchanged image, with
432 bytes of maximum observed stack writes. This is an observation, not a
minimum-stack guarantee.

The new [reproduction receipt](D:/TestData/CopperOSCommands/Q/scalarefeb4b48/exe2arc-reproduction/receipt.json)
is `91a8779b538b17099533c770c915986d9e55f92e2addf4508c823c28cb2fb74e`.
Both emitted HUNKs are 5156 bytes: the failing pre-fix artifact is
`c8804608b284ddc3d99139614b65e1017c0fc1359d414a6405aed28c79704d35`,
and the passing artifact is
`24a15eb5c5c2e2a96a11a38cb45f282e62ba881f75191760e7825074f9ac042f`.
Both report zero fatal sites, helpers, features, external native targets and
managed allocations. This reproduction uses the executor's declared older
CPU `8046d9a2...`, separately from the `8b7510f6...` compiler-test matrix.

These 77 entries are a new bounded 68000 compiler regression. They do not
replace or add to the historical three-CPU Exe2Arc qualification, execute an
original distribution command, or establish real filesystem/packed-binary
equivalence. All original failure and passing probe checkpoints remain intact.
