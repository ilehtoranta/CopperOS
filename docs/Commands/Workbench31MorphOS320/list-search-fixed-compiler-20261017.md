# List and Search: fixed compiler qualification — 2026-10-17

**Both smaller binaries pass, including original DOS checks, and are installed
in `out/C`.** Only List and Search were replaced; all other 59 images, including
Version, remain byte-identical to the initial inventory.

| Shipping command | Previously installed | Accepted | Saved |
| --- | ---: | ---: | ---: |
| List | 4,684 | **4,652** | 32 |
| Search | 5,428 | **5,300** | 128 |
| Complete 61-command set | 225,240 | **225,080** | **160** |

The private MC68000 MorphOS profiles also pass: List 5,148 → 5,096 bytes and
Search 10,496 → 10,416 bytes. These are fixture images, not comparisons with
MorphOS PPC executables. The installed baseline and fixed candidate have
different compiler provenance; their difference is an installation saving,
not a controlled attribution of all savings to one compiler change.

## Inputs and compiler

The frozen compiler is the isolated payload already qualified in the
[Version spill investigation](version-loop-phi-spills-20261017.md): base
`55628a0` plus the conservative address-coalescing correction subsequently
committed as CopperSharp `69d01d1`. It includes the helper continuation fix.
It predates the separate CPU-probe/word-shift merge in the primary checkout.
No current dirty compiler sources or emulator sources were rebuilt here.

Compiler DLL SHA-256:
`abf49732268b0834c86c2237daaf9026782ea3bc05b9d1613d9585d8a0ce9937`.
Compiler payloads, SDK assemblies, CIL inputs, maps, policies, execution
receipts and the complete installation inventory are bound in the
[JSON report](list-search-fixed-compiler-20261017.json).

Fresh normal List/Search source builds with the frozen CLI and existing build
settings exactly reproduce the candidate images. Recompiling the captured
installation assemblies independently also reproduces them byte for byte.
Flags remain MC68000, resident HUNK, YOLO exceptions, memory disabled,
fixed-point peepholes and symbols disabled. Existing per-command size-pass
selections, including SimplifyLoopPhis, remain unchanged. There are no command
source changes or policy changes in this qualification.

## Qualification

All checks run on **68000, 68020 and 68040**:

- Shipping and private profile vectors: List 32/31 cases and Search 22/49
  cases per CPU and per image, **804 invocations** in total. Baseline and
  candidate independently pass their contracts and agree in observed output,
  return codes, IoErr, declared resource ownership and supported repeat cases.
- Original DOS through the frozen native Search/List harness: **300 List
  calls and 192 Search calls**, plus six List smoke calls. Both baseline and
  candidate are loaded with native LoadSeg and alternated through RunCommand.
  Each CPU runs the same 32 List and 18 Search contracts at requested 4 KiB
  and 8 KiB stacks. All 18 full batches pass; maximum completion is 2,410
  frames under unchanged 3,000-frame/120-second caps, with JIT disabled.
- List checks include native metadata, protection, comments, dates, keys,
  block sizes, headings, filters, TO redirection, parser failures and Ctrl-C.
  Its independent auditor reconstructs rows from guest metadata and obtains
  diagnostics from original DOS Fault. Search checks include exact buffered
  prefix/heading ordering, numbering, multiple FROM arguments, wildcard and
  recursive traversal, quoting, long lines, parser failures and Ctrl-C.
- Raw guest requests and binary receipts are checked against decoded JSON,
  declared outputs, return codes, IoErr and baseline/candidate pairing.
  Current directory, CLI, parent stack state/bounds, allocated signals and
  DOS library open count are restored. Final repeated calls have zero
  AvailMem delta; initial native DOS growth is retained in receipts and is
  not treated as an exact command-allocation ownership measurement.
- Independent **WinUAE 6.0.3.0** checks pass for both images on all three CPUs:
  18 List calls and six Search calls, using original Kickstart Shell/DOS,
  a directory filesystem, 4 KiB stacks and no JIT. Actual output files,
  return/Result2 text and copied executable hashes are checked. Search's
  numbered result must be exactly `     2> Needle` followed by LF.
- Eight auditor negative controls reject altered return codes, IoErr,
  output contracts and forged decoded output. The corrected vector runner
  also rejects the historical faulty Search for mixing buffered output with
  raw Write without flushing. After its old artifact path became unavailable,
  an identical SHA-256 copy was recovered from a retained build, frozen
  locally and rerun; both observations and provenance are preserved.

## Execution costs

Vector instruction totals and peak stack bytes below are identical across
the three tested CPU modes. These are finite fixture measurements, not
hardware timing or native DOS boot-cycle performance claims.

| Profile | Instructions before → after | Peak stack before → after |
| --- | ---: | ---: |
| List shipping | 40,384 → 42,982 | 268 → 264 |
| List private MorphOS | 43,616 → 46,753 | 268 → 264 |
| Search shipping | 922,451 → 864,816 | 468 → 476 |
| Search private MorphOS | 10,518,382 → 9,309,851 | 704 → 712 |

List executes about 6.43% more shipping instructions and 7.19% more private
instructions. Search executes about 6.25% and 11.49% fewer respectively,
with eight additional peak-stack bytes. All fixture stack limits pass.

## Runtime identity and limits

The vector runner uses **Copper68k 1.5.1**, SHA-256
`99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`.
The unchanged native OS harness uses its previously frozen **Copper68k
1.5.4-detroit-dev.2+ddc753000b492a15048ba6c16a06e03c0c160a63** payload,
SHA-256 `5780f5084fd44e2d4732969bc0f9ce7ae7e3beffc1135ded92bc46d9b21399f2`.
These are separate recorded runtimes. The native engine is
CopperMod.Amiga.Lightweight `1.0.0+a4e80b68a7b58e6c12b940b9d786b2ac73757965`.
Original Kickstart ROM SHA-256 is
`8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee`;
ROM bytes are not redistributed.

Runner and launcher source bindings match all ten recorded documents. The
frozen lightweight engine matches 46 of 50 current source documents. This
qualifies the frozen executable runtime, not today's dirty CopperScreen
sources. WinUAE supplies the independent emulator check; a current CopperScreen
GUI run and original Workbench List/Search executable comparison were not run.

Native tests cover CLI launches; Workbench launch and MorphOS behavior are
covered only by applicable vectors, not a real MorphOS OS boot. Native tests
do not inject arbitrary allocation/Read/Write failures, isolate stderr, measure
stack high-water/canaries, or establish executable immutability/PURE safety.
No broader OS compatibility or leak-freedom claim follows from this finite
corpus. No compiler code changed, so the preceding fixed-compiler regression
qualification is reused rather than reported as a new compiler test run.

## Evidence and replay

The preserved stage is `artifacts/list-search-fixed-20261017`. It contains the
initial complete installation, both shipping images/maps, private inputs and
images, vector receipts, native boot disks/receipts, WinUAE configurations and
outputs, negative controls, frozen installation inputs and replay, acceptance
receipt, and final 61-image inventory. The reusable driver is
`tools/Commands/qualify_list_search_fixed.py`; its adjacent README documents
the sequence. `publish` re-audits the evidence, checks that the installed
inventory has not changed, and replaces only the two accepted files.
