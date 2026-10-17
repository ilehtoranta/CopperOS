# Dir fixed compiler qualification — 2026-10-17

Follow-up: the [format ABI correction](dir-opt-format-fix-20261017.md) fixes
the source and fixture and passes both profiles under original DOS. This
report preserves the earlier failing images; installation remains held for size.

**Held. `out/C/Dir` remains unchanged at 6,192 bytes.** The fixed compiler
reproduces the earlier 6,208-byte probe, which is 16 bytes larger than the
currently installed image. Original DOS also exposes an existing output bug
in both images and both profiles. No command sources or build policies changed.

| Profile | Baseline | Fixed compiler | Difference |
| --- | ---: | ---: | ---: |
| Shipping Workbench | 6,192 | 6,208 | **16 larger** |
| Private MC68000 MorphOS | 7,648 | 7,556 | 92 smaller |

The earlier controlled compiler experiment measured **6,216 → 6,208**, but
6,216 was a clean-compiler control, not the installed image. The installed
6,192-byte image came from the earlier toolchain. Its hash matches the
accepted loop-phi rollout and that rollout's normal build. Consequently the
8-byte controlled reduction is not an installation saving. The private image
is a qualification fixture, not a PPC executable size comparison.

## Frozen inputs

The compiler payload is the same one used for
[List/Search qualification](list-search-fixed-compiler-20261017.md): isolated
base `55628a0` plus the conservative address-coalescing correction later
committed as CopperSharp `69d01d1`, including the helper-continuation fix.
Compiler DLL SHA-256:
`abf49732268b0834c86c2237daaf9026782ea3bc05b9d1613d9585d8a0ce9937`.
This payload predates the separate CPU-probe/word-shift merge; current dirty
compiler and emulator sources were not rebuilt.

Normal source build settings with this frozen CLI produce 6,208 bytes,
SHA-256 `2454bd3eb2de144d91a20957d9f810ba7cb1cb8883b05e30b04a74029f104d48`.
The frozen earlier shipping assemblies reproduce exactly the same image.
Recompiling freshly captured normal-build assemblies and SDK dependencies
also reproduces it byte for byte. Flags remain MC68000 resident HUNK, YOLO
exceptions, memory disabled, fixed-point peepholes, symbols disabled and
the existing Dir size policy including SimplifyLoopPhis.

The [JSON report](dir-fixed-compiler-20261017.json) binds compiler provenance,
SDK/CIL identities, policies, maps, executable images, runtime identities,
raw original-DOS receipts, native boot disks, WinUAE outputs and the unchanged
complete installation inventory.

## Vector suites pass

Both baseline and fixed candidate pass **30 Workbench and 39 MorphOS cases
per image per CPU** on 68000, 68020 and 68040: **414 invocations**. Declared
outputs, return codes, IoErr, allocation/release ownership, supported
interleaving and image immutability agree. These finite supplied-vector
checks include parsing, allocation failures, traversal/provider errors,
Ctrl-C and startup guards. The MorphOS suite additionally covers recursive
ALL, joined-line OPT I, soft links and larger row tables.

Per-CPU vector measurements are identical across the three CPU modes:

| Profile | Instructions before → after | Peak stack bytes before → after |
| --- | ---: | ---: |
| Shipping | 778,451 → 751,841 | 384 → 384 |
| Private MorphOS | 817,082 → 788,357 | 844 → 844 |

The candidate executes about 3.42% fewer shipping instructions and 3.52%
fewer private-profile instructions in these fixtures, within unchanged stack
limits. These are fixture totals, not hardware timings.

## Original DOS exposes a shared bug

The new Dir diagnostic follows the Search native OS harness: native LoadSeg,
alternating RunCommand calls, native ReadArgs/Lock/ExAll/pattern/formatting
APIs, RAM fixture files, and a binary receipt written through native OFS.
The guest implements no OS services and the host injects no CPU state.
It captures return code and IoErr immediately, plus output and ten caller
state fields around each invocation. Expected error text comes from an
independent native DOS Fault query.

Both profiles run the same **24 declared contracts** with profile-dependent
ALL/OPT A/OPT I expectations. Contracts include default/explicit/relative
directories, directory/file filters, option parsing/case, paired and odd
file rows, an empty directory, wildcards, missing paths, wrong-type and quoted
file paths, parser failures, Ctrl-C, unsupported options, and repeated,
interleaved calls at requested 4 KiB and 8 KiB stacks.

All **18 full batches complete**, with **552 invocations** (92 per profile
per CPU), plus six passing shipping smoke calls. The maximum is 1,670 frames
under unchanged 3,000-frame/120-second caps with JIT disabled.

**12 invocations fail the strict `OPT Z` output contract:** both images,
both profiles, all three CPUs. They agree with one another but fail the
declared contract:

```text
Expected: Z option ignored
Actual:    option ignored
```

The actual bytes start with one space, then `option ignored\n`. All other
540 full-batch calls meet their declared contracts. Return codes, IoErr and
caller state agree throughout; final repeat calls have zero AvailMem delta.
Initial native DOS memory growth is retained, not interpreted as exact
command allocation ownership.

Independent **WinUAE 6.0.3.0** original Shell/DOS checks reproduce the same
bug in both profiles on 68000/68020/68040, with no JIT and 4 KiB stacks.
There are **72 calls**, covering full listing, files, directories, ignored
option, ALL and OPT I. The 12 ignored-option calls fail; all other output
contracts and all Shell return/Result2 lines pass. Actual files and copied
image hashes are rechecked. Eight auditor negative controls reject wrong
return codes, IoErr, output contracts and forged decoded output.

The initial WinUAE run used an incorrect guessed diagnostic for error 236.
That complete run is preserved separately. The final script derives that
diagnostic independently from original DOS Fault (`function not implemented`)
and was rerun on all three CPUs. The `Z` expectation remains strict.

## Why vectors missed it; next correction

`NativeMorphOSDirCommand.Run` stores the ignored character in a **32-bit**
`NameRow.Name` field and passes its address to `DOS.VPrintf("%c option ignored\n", ...)`.
The fixture's `%c` handler reads `Bus.Long(args)` and casts that long to a
character, matching the implementation's storage choice. Thus the mock
formatter accepts an ABI arrangement that original DOS does not render as
intended. A packed character argument width mismatch is the likely cause;
the native and independent emulator failure establishes the symptom without
claiming a new code-generator regression.

The next correction should fix the character argument/format ABI and make
the vector formatter enforce the native convention, then rerun this corpus.
The 16-byte installation size gap remains a separate acceptance requirement.
Neither the expected character nor any command option was removed here to
make qualification pass.

## Runtime identities and limits

Vector runner: Copper68k **1.5.1**, SHA-256
`99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`.
Native harness: frozen Copper68k
**1.5.4-detroit-dev.2+ddc753000b492a15048ba6c16a06e03c0c160a63**,
SHA-256 `5780f5084fd44e2d4732969bc0f9ce7ae7e3beffc1135ded92bc46d9b21399f2`,
with CopperMod.Amiga.Lightweight
`1.0.0+a4e80b68a7b58e6c12b940b9d786b2ac73757965`.
Native runner/launcher bindings match all ten documents at binding time;
the frozen lightweight engine matches 45 of 50 current source documents.
This qualifies execution with those frozen binaries, not current dirty
CopperScreen sources. Independent WinUAE identity/configurations are recorded.
Original Kickstart ROM SHA-256 is
`8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee`;
no ROM bytes are redistributed.

The private MorphOS image is executed on original classic DOS here, not a
real MorphOS boot. Native tests cover CLI launches; startup and injected
failure cases remain supplied-vector coverage. Native tests do not inject
arbitrary low-memory/I/O failures, isolate stderr, measure stack canaries or
high-water marks, or establish PURE safety. No original Workbench Dir binary
comparison or current CopperScreen GUI run was performed. No compiler code
changed, so prior fixed-compiler regressions were reused rather than rerun.

Evidence is under `artifacts/dir-fixed-v2-20261017`. The incomplete initial
freeze attempt is preserved under `artifacts/dir-fixed-20261017`. The
reusable driver and new native diagnostic are described in
`tools/Commands/DirOsQualification/README.md`. All 61 installed images remain
byte-identical to the start of this qualification, totaling **225,080 bytes**.
