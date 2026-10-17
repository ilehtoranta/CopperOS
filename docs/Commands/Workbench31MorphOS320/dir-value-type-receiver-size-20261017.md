# Dir value-type receiver size correction — 2026-10-17

**Fixed and qualified.** Dir is now **6,164 bytes** in `out/C`, down from the
installed 6,192 bytes and the preceding fixed-compiler 6,208-byte candidate.
The qualified OPT Z formatting correction is included. Only Dir was replaced;
the complete 61-command inventory is **225,052 bytes**, saving 28 bytes.

## Where the 16-byte gap came from

This was a net toolchain difference, not an added command option or the `%lc`
format correction. Comparing the installed image's accepted map with the
fixed-compiler map shows:

| Method | Installed | Fixed compiler before this correction | Difference |
| --- | ---: | ---: | ---: |
| NativeCommandArguments.Release | 112 | 138 | +26 |
| NativeCommandArguments.TryGetResult | 54 | 70 | +16 |
| Dir's changed methods combined | — | — | -30 |
| Encoded code net change | — | — | **+12** |
| Additional HUNK relocation entry | — | — | **+4** |

The fixed image therefore grew by 16 executable bytes. The two argument
helpers accounted for 42 additional method bytes, partially offset by smaller
CStringLength, CopyCString, EnsureRowCapacity, ListDirectory and Run bodies.

Disassembly exposes a needless root slot for the struct receiver, stores
publishing that receiver before calls, clears after its final use, and frame
setup/cleanup around that slot. Release also retains two initial local clears
that the private-initialization pass cannot remove while it sees GC references.
The expanded Release exit branch needs an absolute jump and relocation.

## Cause and compiler fix

`CilStackAnalyzer.StackKindForParameter` classified every ordinary instance
receiver as an object reference. An unboxed value-type method receives a
managed byref as `this`. For NativeCommandArguments this points to the caller's
lease storage. Incorrect object-reference classification triggered GC root
tracking even in these memory-disabled native commands.

The correction classifies an ordinary value-type receiver as `ManagedPointer`.
Class receivers retain object-reference classification, and existing transparent
scalar receiver handling is unchanged. `CompilationModule.IsValueTypeMethod`
provides the declaring-type check, including methods resolved from another
assembly; constructor classification delegates to the same check.

There is no special case for Dir or its method names, no disabled check, and no
new optimization switch. The existing Version address-coalescing correction
remains intact.

| Controlled result, identical corrected CIL and SDK | Before | After | Saved |
| --- | ---: | ---: | ---: |
| Shipping Dir executable | 6,208 | **6,164** | **44** |
| Private MC68000 MorphOS executable | 7,556 | **7,512** | **44** |
| Release method | 138 | 112 | 26 |
| TryGetResult method | 70 | 56 | 14 |

The compiler change removes **40 code bytes plus one four-byte relocation
entry**. Release's frame falls from 12 to 8 bytes, TryGetResult's from 4 to zero;
both lose their root-store/clear operations. The preceding installed compiler
had other changes, so its 54-byte TryGetResult is not claimed as this fix's
controlled baseline. The private MorphOS image is not a PPC size comparison.

## Reproducible compiler inputs and integration

Work used the isolated `CopperSharp68k-dir-size-20261017` checkout at clean
**`69d01d1`**, which includes both the helper continuation and Version coalescing
fixes. Before modification, rebuilding that revision reproduces the preceding
fixed compiler's Dir images byte for byte: 6,208 and 7,556 bytes. Candidate
builds add only this two-file compiler change. Both sides use identical frozen
command assemblies, SDK dependencies, profiles and pass selections.

A fresh normal Dir source build with the candidate CLI exactly reproduces the
accepted image. MC68000 resident HUNK, YOLO exceptions, memory disabled,
fixed-point peepholes, symbols disabled and the existing size policy remain
selected. Candidate SHA-256:
`375986a78d6dad236d9b831ef6e74bc342b18363e0b162d69f36bc526be72963`.
The [JSON report](dir-value-type-receiver-size-20261017.json) binds compiler
payloads/revision/patch, command and SDK inputs, flags, maps, disassembly,
test receipts, native runtime identities and final hashes.

The primary CopperSharp checkout already contains equivalent receiver
classification work among its unrelated pending changes. Those compiler source
files were left byte-identical during integration. The two new regression
files were copied there. A separately reviewable
[portable patch](dir-value-type-receiver-size-20261017.patch), against
`69d01d1`, contains only the qualified compiler correction and those tests.
It passes an index-only application check against the primary checkout's HEAD.
No unrelated primary compiler work was rebuilt or included in qualification.
The qualified patch is committed in CopperSharp as `9eaa2aa`.

## Qualification

- **900 focused compiler tests pass**, covering register allocation,
  instruction dataflow, optimizer/branch relaxation, resident execution,
  value-type construction, transparent wrappers and managed-byref/GC cases.
  The existing loop-phi coalescing regressions also pass.
- Four new cases verify ordinary struct, constructed generic struct,
  cross-assembly struct and class receivers live across a call. All pass
  with the correction. With the unchanged compiler DLL, all **three struct
  regressions fail** with `Reference` instead of `ManagedPointer`; the class
  positive control continues to pass and retain its object root.
- Both Dir profiles pass **450 vector invocations**: 33 shipping and 42 private
  cases, baseline/candidate, on 68000/68020/68040. Output bytes, return codes,
  IoErr, declared allocation ownership, startup guards, supported interleaving
  and image immutability agree.
- Both profiles pass **588 original-DOS invocations**, with 27 contracts,
  alternating LoadSeg/RunCommand calls, native ReadArgs/ExAll/pattern/formatting
  APIs, errors, Ctrl-C, uppercase/lowercase/multiple ignored options, and 4 KiB
  and 8 KiB requested stacks. All 18 full batches pass. Maximum completion is
  1,670 frames under unchanged 3,000-frame/120-second caps, JIT disabled.
  All ten caller-state fields restore; stable repeats have zero AvailMem delta.
- **96 independent WinUAE calls pass**, both profiles/images and all CPUs,
  using original Shell/DOS, 4 KiB stacks and no JIT. Exact output and return/
  Result2 files and executable hashes are checked. Eight auditor negative
  controls and six old formatting-image negative controls remain effective.

An initial broad compiler selection reported eight failures. One was a new
test setup missing its cross-assembly dependency; that setup was corrected.
The other seven reproduce with the unchanged compiler: two unavailable
CopperScreen headless integrations, four existing 68040 RunCommand fixture
fetch-bound failures with Copper68k 1.5.1, and one pre-existing dictionary
allocation-statistics budget failure. Their raw receipts and the 19-case
baseline-control run are retained. These checks are excluded from the final
900-test selection; no assertion or execution range was relaxed.

Per-CPU fixture costs (identical across tested CPU modes):

| Profile | Instructions before → after | Peak stack bytes before → after |
| --- | ---: | ---: |
| Shipping | 757,238 → 756,090 | 384 → 384 |
| Private MorphOS | 793,857 → 792,299 | 844 → 844 |

The candidate executes 1,148 fewer shipping instructions and 1,558 fewer private
instructions in these fixtures, with no increase in peak stack usage. These
are finite vector totals, not hardware timing or native DOS boot benchmarks.

## Runtime limits and delivery

The vector/compiler execution tests use **Copper68k 1.5.1**. The frozen native
OS runner uses **1.5.4-detroit-dev.2+ddc753000b492a15048ba6c16a06e03c0c160a63**
and CopperMod.Amiga.Lightweight
`1.0.0+a4e80b68a7b58e6c12b940b9d786b2ac73757965`; WinUAE is **6.0.3.0**.
Actual hashes, ROM identity and PDB source bindings are recorded. These are
separate runtimes, reused from the preceding Dir qualification, with no emulator
changes. No current CopperScreen GUI or original Workbench executable comparison
was performed. ROM bytes are not redistributed.

The private profile runs on original classic DOS, not a real MorphOS boot.
Native tests cover CLI launch; Workbench startup and injected resource failures
remain vector coverage. Native tests do not establish arbitrary failure
injection, isolated stderr, stack canaries/high-water marks, PURE safety or exact
command-level allocation ownership. Initial native DOS memory growth remains
recorded rather than treated as a command leak.

Evidence is retained at `artifacts/dir-size-regression-20261017`. The complete
accepted inventory was staged and hashed before updating only `out/C/Dir`.
All other 60 executable hashes remain unchanged. The installed image now
includes both the format correction and compiler size correction.
