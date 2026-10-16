# Version MorphOS null-buffer read: compiler fix

Investigation started 2026-10-16; qualification completed 2026-10-17.

**Fixed in CopperSharp's `InlineMemoryHelpers` pass.** Version's buffer
allocation and failure checks were correct. No Version source change, fixture
relaxation or command-policy change was necessary.

## Cause

Inlining replaces a call with the helper body. Removing the call first collapses
labels at its start and at its continuation onto the same byte offset. The
subsequent insertion deliberately leaves boundary labels at the start, but
previously failed to distinguish continuation labels from entry labels.

In MorphOS Version, the `md5 == 0` branch should skip `InitializeMd5`. After
inlining, its target instead entered the initializer without staging the MD5
context argument. The stale address pointed into the file lease. The initializer
cleared its scratch pointer, and `ScanForVersionTag` subsequently called DOS Read
with a null destination and a 16,385-byte request. The existing fixture correctly
rejected this read.

Disabling all size passes or just `InlineMemoryHelpers` passes the complete
MorphOS fixture. Disabling `ReuseIncomingArgumentHomes` or
`ForwardReadOnlyAggregateLocals` does not cure the failure. A clean compiler at
`eb04dac67d2ac2f6e5a41fde5254e56a808bc8d5` reproduces the same failure, so it is
independent of the unrelated changes in the primary compiler checkout.

## Fix and regression

Before removing each call, record every label exactly at its continuation.
After inserting the cloned body, restore those labels to the new continuation.
Entry labels still denote the start of the body. Existing analysis-anchor and
internal-branch handling remains in place.

The fix is in `../CopperSharp68k/Compiler/Backend/M68kPeepholeOptimizer.HelperSize.cs`.
The new `GeneratedHelperContinuationTests.cs` is in that repository's
`Compiler.Tests` directory. Both were developed and qualified independently in
`../CopperSharp68k-version-inline-20261016`, based on clean `eb04dac`.
The [portable compiler patch](version-helper-continuation-fix-20261016.patch)
contains both changes for review and delivery alongside CopperOS.

- All **14 new regression layouts fail without the fix and pass with it**.
- They cover relative/absolute calls, two call sites, helper placement before
  and after callers, alias labels, relocated pointer references to call entries
  and continuations, short/word branch distances up to 32,748 bytes of padding,
  and taken/skipped execution on 68000/020/040 at two load addresses.
- Execution checks cover ordinary memory writes, registers including A5/A6,
  condition codes including X, and exact stack restoration.
- **1,455 focused compiler regressions pass**: the new tests plus existing
  generated-size, resident execution, instruction-dataflow and branch-relaxation
  tests. The complete compiler suite was not run.

Initial raw-test drafts lacked the compiler's method-end label convention and
did not exercise helper inlining; those failed attempts are retained. One
broader attempt after restoring the fixed source reused the unfixed DLL because
the copy retained an older timestamp. Its 14 expected failures are retained;
the final rebuilt run verifies that its compiler DLL hash equals the frozen
fixed CLI payload before accepting the 1,455 passes.

## Version qualification

Unchanged managed inputs and SDK assemblies come from the frozen October 16
rollout. With the fixed compiler, compare the existing Version pass selection
against the same selection with only `InlineMemoryHelpers` removed.

| Profile | Cases per CPU, per image | Without inlining | Fixed inlining | Instructions per CPU, off → on | Peak stack |
| --- | ---: | ---: | ---: | ---: | ---: |
| Shipping Workbench | 93 | 8,756 bytes | 8,676 bytes | 374,544 → 370,984 | 780 bytes |
| Private MorphOS | 94 | 27,644 bytes | 27,640 bytes | 3,573,039 → 3,572,993 | 1,568 bytes |

Both images pass on **68000, 68020 and 68040**: 1,122 total command invocations.
Command-visible observations agree, including output bytes, return code, IoErr,
declared allocation/release ownership, repeated/interleaved fixture invocations
and image immutability. All measured stacks fit the unchanged configured limits.
Instruction totals are finite fixture measurements, not hardware timing.

The clean unfixed inlined MorphOS image is 27,636 bytes and fails the original
null-buffer assertion. The fix adds four bytes to that invalid image; the size
comparison above uses the passing no-inlining control. Do not attribute the
different sizes in the earlier rollout's dirty compiler to this five-line fix.

The shipping 8,676-byte fixed image is byte-identical to `out/C/Version`; no
installed binaries need replacement. `SimplifyLoopPhis` remains disabled for
Version, pending its separate rollout decision. No compiler default changes.

## Evidence and reproduction

The [JSON receipt](version-helper-continuation-fix-20261016.json) binds the source
patch, compiler payloads, frozen assemblies, response files, maps, execution
receipts and test results. Working evidence is in
`artifacts/version-null-20261016`; earlier frozen inputs remain in
`artifacts/loop-phi-rollout-20261016`. Failed evidence is preserved.

```powershell
python tools/Commands/audit_version_helper_continuations.py `
  --stage artifacts/version-helper-fresh `
  --inputs artifacts/loop-phi-rollout-20261016 `
  --compiler artifacts/version-null-20261016/compiler-fixed/CopperSharp.Compiler.Cli.dll
```

Use a fresh stage. The tool compiles both profiles with and without inlining,
then compares their three-CPU receipts. Flags are MC68000 resident HUNK, YOLO
exceptions, memory disabled, symbols off and fixed-point peepholes. Executor:
Copper68k 1.5.1, SHA-256
`99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`.

These are supplied-vector fixtures. Original DOS, WinUAE/CopperScreen and actual
MorphOS execution were not run for this fix. The primary compiler checkout's
other ongoing changes were preserved; qualification uses the isolated payload,
not a rebuild of those unrelated changes. The compiler fix is commit `55628a0`,
pushed to `origin/main` through merge commit `5b7525a`, which also incorporates
the remote CPU-probe/word-shift fix.
Qualification above remains bound to the isolated compiler payload before that
remote integration.
