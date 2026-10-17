# Dir ignored-option format correction — 2026-10-17

Follow-up: the [receiver classification fix](dir-value-type-receiver-size-20261017.md)
reduces shipping Dir to 6,164 bytes and installs it after qualification. The
measurements and held installation below describe the earlier checkpoint.

**Fixed the format ABI mismatch and corrected the vector fixture.** Both
Workbench and private MorphOS profiles pass on 68000, 68020 and 68040,
including the expanded original-DOS corpus and independent WinUAE checks.

## Cause and fix

Dir stores an ignored option character in a 32-bit `NameRow.Name` cell. Plain
`%c` consumes a word on Amiga DOS. On big-endian MC68000, a cell containing
`0x0000005A` supplies a zero character from its first word, rather than `Z`.
The remaining diagnostic text still prints.

Changed the format to **`%lc option ignored\n`**, matching the long cell and
the convention already used by Type. Argument ownership, startup, parsing,
return codes and resource lifetime are unchanged. Shipping remains **6,208
bytes** and private MorphOS **7,556 bytes**, with no increase over the preceding
fixed-compiler images. The extra format character fits existing data alignment.

`DirEntrySuite` previously treated `%c` as a long load, hiding the bug. It now
reads a word for `%c` and a long for `%lc`, and models the observed handling of
a zero character. Added `OPT Z`, lowercase `OPT z` and multiple ignored-option
(`OPT Z?`) regressions, retaining `OPT X`. The native corpus adds lowercase,
multiple and X coverage without changing its original Z expectation.

## Qualification

All three CPUs run the same MC68000 images, with no emulator changes:

- **450 supplied-vector invocations pass:** 33 Workbench and 42 MorphOS cases
  per image per CPU. Both images pass output bytes, return codes, IoErr,
  declared allocation/release ownership, stack limits, startup guards,
  supported interleaving and image immutability.
- **Six ABI negative controls reject the old buggy images:** both profiles
  on every CPU. The rebuilt fixture fails them at `unknown-option` with the
  missing-character output observed on original DOS. This proves the fixture
  no longer accepts the old ABI arrangement.
- **588 original-DOS invocations pass:** 27 contracts per profile, across
  both images and all CPUs, with warm-up and repeated/interleaved calls at
  requested 4 KiB and 8 KiB RunCommand stacks. All 18 full batches pass;
  maximum completion is 1,980 frames under unchanged 3,000-frame/120-second
  limits, in interpreter mode. Guest, host and OS runner binaries are reused
  byte for byte from the preceding experiment.
- Native checks include exact uppercase/lowercase/multiple diagnostics,
  listing order, paired/odd rows, empty/relative/quoted paths, filters,
  wildcards, parser/wrong-type failures, Ctrl-C, and distinct ALL/OPT A/OPT I
  contracts. Return/IoErr and all ten caller-state fields agree. Stable repeat
  calls have zero AvailMem delta. Initial DOS growth remains recorded, without
  claiming exact command allocation ownership.
- **96 independent WinUAE calls pass:** both profiles and images on every CPU,
  using 4 KiB stacks, original Shell/DOS and no JIT. Full listing, filters,
  uppercase/lowercase/multiple ignored options, ALL and OPT I have exact
  output and Shell return/Result2 checks. Actual files and copied image hashes
  are re-audited.
- Eight auditor negative controls reject false return codes, IoErr, output
  contracts and forged JSON, restoring original evidence byte for byte.

The passing comparison controls contain corrected source and disable only
SimplifyLoopPhis. Candidates use the existing shipping pass selection, with
identical CIL/dependencies within each comparison. Old buggy images are
retained separately as rejection controls. No command option or check was
removed to make qualification pass.

| Profile | Corrected control, phi off | Corrected candidate, phi on | Instructions off → on | Peak stack off → on |
| --- | ---: | ---: | ---: | ---: |
| Workbench | 7,740 bytes | 6,208 bytes | 806,263 → 757,238 | 420 → 384 |
| Private MorphOS | 9,180 bytes | 7,556 bytes | 849,245 → 793,857 | 888 → 844 |

Measurements are per CPU and identical across tested modes. These compare
policies on corrected source; added regression cases prevent direct comparison
of totals with the earlier suite. They are finite vector measurements, not
hardware timings or DOS boot-cycle benchmarks. All stack limits pass.

## Reproducibility and installation

The frozen compiler remains isolated base `55628a0` plus the conservative
address-coalescing fix later committed as CopperSharp `69d01d1`, including the
helper continuation fix. Compiler DLL SHA-256:
`abf49732268b0834c86c2237daaf9026782ea3bc05b9d1613d9585d8a0ce9937`.
No dirty compiler or emulator checkout was rebuilt.

The vector runner and private CIL root were rebuilt in isolated stage projects
from copied sources against frozen SDK references. Source snapshots, generated
projects, package receipts and build logs are retained. Unrelated nullability
and unassigned-field warnings are recorded; both builds have zero errors.

A normal source build with existing properties and the frozen CLI produces
the shipping candidate. An independent replay using the same frozen SDK
dependencies as the private profile reproduces it byte for byte. Both normal
build and frozen SDK identities are recorded. Flags remain resident MC68000
HUNK, YOLO exceptions, memory disabled, fixed-point peepholes and symbols
disabled. Shipping SHA-256:
`d14f35dc799bd50a57369147f4be93d3c8cbad33c296d17296383b54a1d934aa`.

**`out/C` remains unchanged under the existing size acceptance rule.** The
qualified 6,208-byte image is still 16 bytes larger than installed Dir at
6,192 bytes. This gap predates the format fix. All 61 installed images retain
their initial bytes, totaling **225,080 bytes**. The source/fixture fix is
qualified; installing the larger executable remains held for size.

## Runtime identities and limits

Vectors use Copper68k **1.5.1**, SHA-256
`99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`.
The frozen native harness uses Copper68k
**1.5.4-detroit-dev.2+ddc753000b492a15048ba6c16a06e03c0c160a63**,
SHA-256 `5780f5084fd44e2d4732969bc0f9ce7ae7e3beffc1135ded92bc46d9b21399f2`,
with CopperMod.Amiga.Lightweight
`1.0.0+a4e80b68a7b58e6c12b940b9d786b2ac73757965`. WinUAE is **6.0.3.0**,
SHA-256 `f3e55700fd811cd543fdfebf6c3221cfaa3d385828f110c6d3eb534c64503123`.
ROM SHA-256:
`8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee`.
ROM bytes are not redistributed. Runtime hashes and PDB source bindings are
retained; current dirty lightweight engine sources are not qualified by
executing the older frozen runtime.

The private MorphOS source profile runs on classic DOS, not a real MorphOS
boot. Native tests cover CLI launches; Workbench startup and injected resource
failures remain supplied-vector coverage. Native tests do not inject arbitrary
low-memory/I/O failures, isolate stderr, measure stack canaries/high-water
marks, establish PURE safety or compare against original Workbench Dir. A
current CopperScreen GUI run was not performed.

Evidence is at `artifacts/dir-format-fix-20261017`, bound by the
[JSON report](dir-opt-format-fix-20261017.json). Replay instructions are in
`tools/Commands/DirOsQualification/README.md`. The
[preceding failed qualification](dir-fixed-compiler-20261017.md) and its raw
receipts remain preserved.
