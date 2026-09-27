# Archive provider work package

Recorded 2026-08-30. Owners: CC07 for shared service prerequisites, CC31 for
compression/archive/hash commands, CC32 for XAD tools and Exe2Arc. This refines
the existing goal; it does not change its command inventory or completion
requirements. **All 16 commands below remain required and unqualified.**

The audit found usable SDK declarations and host fixture helpers, but no
qualified native archive provider in the inspected command/runtime owners.
Exe2Arc can progress independently because its published implementation uses
DOS/Exec without XAD. The other families have concrete work below; none is
removed because an extension service is missing.

## Required command-to-provider map

The baseline is `morphos320`, not the latest upstream codec release. Binary
hashes and locations remain authoritative in the [inventory](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/command-inventory.json).
Versions below are unexecuted parsed media tags, not source-equivalence or
feature proofs. Each dependency ID is defined in the ownership table below.

| Stage | Command | Parsed media version | Required owner dependencies |
| --- | --- | --- | --- |
| CC31 | Bz2 | 50.6 (28.7.2016) | `CC07.ARCH.SOURCE`, `CC07.ARCH.BZIP2` |
| CC31 | LhA | 2.7; no parsed date | `CC07.ARCH.SOURCE`, `CC07.ARCH.LHA` |
| CC31 | LZMADec | 51.7 (31.03.2026) | `CC07.ARCH.SOURCE`, `CC07.ARCH.LZMA` |
| CC31 | LZMAInfo | 51.7 (31.03.2026) | `CC07.ARCH.SOURCE`, `CC07.ARCH.LZMA` |
| CC31 | XZ | 51.7 (31.03.2026) | `CC07.ARCH.SOURCE`, `CC07.ARCH.LZMA` |
| CC31 | XZDec | 51.7 (31.03.2026) | `CC07.ARCH.SOURCE`, `CC07.ARCH.LZMA` |
| CC31 | UnRAR | 3.90 (01.11.2009) | `CC07.ARCH.SOURCE`, `CC07.ARCH.RAR` |
| CC31 | OFArc | 1.5 (24.05.26) | `CC07.ARCH.SOURCE`, `CC07.ARCH.OFARC`; `CC07.ARCH.URI` for evidenced remote modes |
| CC31 | OFHash | 1.5 (24.05.26) | `CC07.ARCH.SOURCE`, `CC07.ARCH.HASH`; `CC07.ARCH.URI` for evidenced IRI modes |
| CC32 | XAD2LhA | 1.4 (12.04.2016) | `CC07.ARCH.XADABI`, `CC07.ARCH.XADCORE`, `CC07.ARCH.XADCLIENT`, `CC07.ARCH.LHA` output mode |
| CC32 | XADLibInfo | 1.7 (12.04.2016) | `CC07.ARCH.XADABI`, `CC07.ARCH.XADCORE`, actual client registry from `CC07.ARCH.XADCLIENT` |
| CC32 | XADList | 1.1 (12.04.2016) | `CC07.ARCH.SOURCE`, `CC07.ARCH.XADABI`, `CC07.ARCH.XADCORE`, `CC07.ARCH.XADCLIENT` |
| CC32 | XADUnDisk | 1.17 (12.04.2016) | `CC07.ARCH.XADABI`, `CC07.ARCH.XADCORE`, `CC07.ARCH.XADCLIENT`, `CC07.ARCH.XADDISK` |
| CC32 | XADUnFile | 1.27 (24.03.2020) | `CC07.ARCH.XADABI`, `CC07.ARCH.XADCORE`, `CC07.ARCH.XADCLIENT` |
| CC32 | XADUnTar | 1.10 (02.05.2025) | `CC07.ARCH.XADABI`, `CC07.ARCH.XADCORE`, `CC07.ARCH.XADCLIENT`, `CC07.ARCH.METADATA` |
| CC32 | Exe2Arc | 1.6 (12.04.2016) | `CC07.ARCH.EXA`; no XAD provider required by inspected source |

Common to every row are CC01's exact grammar/options contract and CC02-CC06 /
CC08's ABI, native code, resource, purity and packaging gates. Bz2 has an
observed installer P addition; the other 15 rows have unresolved purity in
the current inventory, which is not a non-pure classification. Preserve
future installed-media findings rather than guessing from language or size.

## Existing owners and actual limits

| Existing file/owner | What is available | What it does not establish |
| --- | --- | --- |
| [CopperSharp68k XadMaster.cs](D:/Koodit/GIT/CopperSharp68k/Sdk.Amiga/XadMaster/XadMaster.cs:11) | MIT, manually opened library base; register/LVO declarations for object allocation, recognition, info, file/disk extraction, hooks, clients, dates/protection, CRC and name conversion. | No implemented xadmaster.library or codec. Archive/file/disk/client structures, tag constants and hook payload types are not present in this directory; raw uint pointer declarations do not qualify their layouts or lifetimes. |
| [CopperSharp68k XpkMaster.cs](D:/Koodit/GIT/CopperSharp68k/Sdk.Amiga/XpkMaster/XpkMaster.cs:11) | MIT, manual-base optional XPK binding. | No installed XPK implementation or sublibraries. XPK is a conditional client dependency, not a universal replacement for CC31 codecs or XAD. |
| [OptionalAmigaBindingTests.cs](D:/Koodit/GIT/CopperSharp68k/Compiler.Tests/OptionalAmigaBindingTests.cs:17) | Reflection and compiler-output checks, including selected bases/registers/LVOs. | No native archive recognition, decompression, callback, allocation-failure or teardown execution. |
| [CopperDisk AmigaDiskLoader.cs](D:/Koodit/GIT/MedPlayer/CopperDisk/AmigaDiskLoader.cs:79) | Local MIT project; host ZIP and gzip/ADZ image loading with System.IO.Compression. | Host ZipFile/GZipStream, streams and arrays are not production guest codecs or command semantics. |
| [CopperScreenDiskImageArchive.cs](D:/Koodit/GIT/MedPlayer/CopperScreen/CopperScreenDiskImageArchive.cs:69) | MIT host ZIP/disk-set discovery for the emulator UI. | No native archive command, metadata/overwrite policy or library ABI. |
| [CopperDisk DmsDecoder.cs](D:/Koodit/GIT/MedPlayer/CopperDisk/DmsDecoder.cs:12) | Host DMS decoder and CRC checks; useful fixture/oracle candidate. Local header attributes xDMS behavior to a public-domain source. | This audit did not independently establish the upstream grant. Uses managed arrays/state and rejects encrypted, high-density, MS-DOS, FMS and other unsupported disk forms. Not a complete native DMS/XAD client. |

The SDK [README](D:/Koodit/GIT/CopperSharp68k/Sdk.Amiga/README.md:313)
explicitly leaves XAD/XPK opening, closing and installed codecs to the caller
and system. Nothing in those declarations makes them Kickstart 3.1 services.
The missing-provider search covered CopperOS `src`/`tests`, CopperStart
`src`/`tests`, and CopperSharp68k `Runtime.Managed`, `Runtime.AmigaPal`, and
`Sdk.Amiga.Support`. It is a bounded local audit, not a claim about every checkout
or external package. At this audit, CopperOS `src/Libraries` contains only
`muimaster.library`; archive owner directories below are proposed additions.

Audited SDK SHA256 values:

| File | SHA256 |
| --- | --- |
| `Sdk.Amiga/XadMaster/XadMaster.cs` | `e4ed76e09e0395739cad8124061a3e1578e2bcb64917ad46802ccecafce95bd7` |
| `Sdk.Amiga/XpkMaster/XpkMaster.cs` | `3ddc6e1ae9b6ec1f41fdf87ff8cf322f9624e242a7fb52888d7fc763d9629999` |
| `Compiler.Tests/OptionalAmigaBindingTests.cs` | `da566b24660175263d4bacdd808233ad3fd58cefd327f00bb7e7ebf121bef926` |

## Pinned source candidates and license gates

These archives are linked by the [MorphOS source release index](https://www.morphos-team.net/sources).
They were inspected as data in memory. Their availability does not establish
binary equivalence, cover every shipped format, or approve unreviewed source
reuse. No vendor implementation is added by this work package.

| Source package | Bytes; SHA256 | Useful members and observed licensing |
| --- | --- | --- |
| [3.20 c.tar.bz2](https://www.morphos-team.net/files/src/3.20/c.tar.bz2) | 214038; `db099324cbf4b07de70bcafc626af0b168903a714911f840652e1a364578b5ba` | `c/xad` command sources. Grants differ by file; see next table. |
| [3.20 xadmaster.library.tar.bz2](https://www.morphos-team.net/files/src/3.20/xadmaster.library.tar.bz2) | 271306; `cfa31fc45b80e1ac97749338d33fc21b0e33b5c384bde3e60361f663d0a772ed` | XAD core, public headers, Amiga glue and clients. Package LICENSE is LGPL 2.1; `include/xadmaster.h` explicitly grants LGPL 2.1-or-later. Audit every admitted core/client file separately. |
| [ObjFW 1.5.4.tar.gz](https://www.morphos-team.net/files/src/3.20/objfw-1.5.4.tar.gz) | 1090224; `6897cc416f95e63cf35efba51f349ac638aa4b33b81aec60580118b788eb6efd` | `utils/ofarc/OFArc.m`, `utils/ofhash/OFHash.m`, framework archive/hash classes. Inspected command headers specify LGPL 3.0 only, not the XAD grant. Objective-C/framework implementation is not directly consumable by the C# native compiler. |

The two-component OFArc/OFHash inventory tag is not enough to establish or
reject correspondence with ObjFW 1.5.4; bind the complete tag, source revision,
build features and command behavior before claiming a match. No corresponding
Bz2/LhA/LZMA-XZ/UnRAR source identity or per-file grant was established in this
bounded release-source audit. Pin them under `CC07.ARCH.SOURCE`; do not adopt
a current codec's syntax, feature set or presumed license as the 3.20 contract.

| Command source member | Observed grant | SHA256 |
| --- | --- | --- |
| `c/xad/exe2arc.c` | LGPL 2.1-or-later, Dirk Stöcker | `fcbf2f58345c5c74ecdadb28539413d78e66fa0c6b4c1f25f1409d35a9ed647d` |
| `c/xad/xad2lha.c` | LGPL 2.1-or-later | `32668f33a11e3397fc20efe60bf58f48cf3676a1d32a1cf1662a316140c38477` |
| `c/xad/xadlibinfo.c` | LGPL 2.1-or-later | `de7660f4516ff2c8e75d0110cf47714a265ebca4ce7142424bc581c773a4fa2b` |
| `c/xad/xadlist.c` | Header says Freeware, author SDI; no explicit LGPL reuse grant found | `f49a8f346b0cf79ce508c3bde96b2b143be5c64d1e73f8a4a48a88ba20e5016e` |
| `c/xad/xadundisk.c` | LGPL 2.1-or-later | `25b4f0b1c8ffbed2b8d23b702a838a14e510d94999f95ca2e575f65a057c903a` |
| `c/xad/xadunfile.c` | LGPL 2.1-or-later | `1f0ec4d3e6b78b64f8b331a2ea0513860a49e950ed827a3b74b2ccdb388910cd` |
| `c/xad/xaduntar.c` | LGPL 2.1-or-later | `d16523e157f9c4cc8423211dc36965ac9d090136101ea9fe6b96938a8518459b` |

The command Makefile's all-rights-reserved header is not replaced by the
neighboring LGPL notices; see [Exe2Arc's per-file evidence](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Exe2Arc.md).
Before deriving code, record the exact admitted files, grants, modifications,
linking/distribution choice and required notices/source offer. Alternatively,
record an independent implementation based on the behavioral contract. An
ambiguous grant stays a source-admission gap, not a reason to remove a command.

XAD provider members identify real implementation responsibilities:

- `include/xadmaster.h`, `include/functions.h`, `functions.def`: ABI, structures,
  tags, flags and function ordering; not just SDK method names.
- `objects.c`, `info.c`, `fileunarc.c`, `diskunarc.c`, `clients.c`, `clientfunc.c`,
  `crc.c`, `dates.c`, `protection.c`, `filename.c`: object/client ownership,
  decoding dispatch and metadata services.
- `amiga/libinit.c`, `amiga/xadmaster_68k.c`, `amiga/hook_fh.c`,
  `amiga/hook_disk.c` and the `hook*` sources: library lifecycle, native gateways,
  seek/stream/memory/split input and disk I/O. These need actual guest owners.
- Client candidates include Zip, LhA, Tar, CAB, Ace, DMS, LZX, bzip2, Zstd,
  Cpio, StuffIt and several filesystem/disk formats. The observed archive does
  not contain an identified RAR or LZMA/XZ client. The complete installed 3.20
  client/feature list still requires media and XADLibInfo/reference capture.
  Do not advertise only a convenient subset and mark the stage complete.
- `test/open.c`, `test/tags.c`, `test/extract.c` are upstream test leads,
  not existing CopperOS native qualification receipts.

Useful ObjFW member identities are `utils/ofarc/OFArc.m` SHA256
`36f86f136dee40722553ee2f411cb894c431466f48146a49dfcc2008115da87c`
and `utils/ofhash/OFHash.m` SHA256
`e6015185c58a02be5deab182093f69175ec8ea2ff112c9f5fee76f9c649d780e`.
The hash front end references MD5, RIPEMD-160, SHA-1, SHA-224/256 and SHA-384/512;
capture all corresponding switches, combinations and input modes. This audit
does not close OFArc's full archive/mode list. Its framework classes are source
candidates, not permission to make a host ObjFW process the provider.

## Dependency ownership and bounded deliverables

Paths marked **new** are proposed implementation owners, not files created or
services discovered by this audit. Relative proposed paths are under
`D:/Koodit/GIT/CopperOS` unless explicitly assigned to CopperSharp68k. Each
later slice must declare its own allowed files before editing. Source/license
admission applies to every derived provider, including shared codecs.

| Dependency ID | Owning stage and concrete owner | Bounded deliverable and acceptance evidence |
| --- | --- | --- |
| `CC07.ARCH.SOURCE` | CC01/CC07; **new** `docs/Commands/Workbench31MorphOS320/archives/source-admission.md` and per-command `contracts/*.md` | Pin media/source/build options and per-file grants; enumerate every grammar option, format and mode. Each has an owner, fixture ID and unresolved evidence field. No copied implementation until admission is explicit. |
| `CC07.ARCH.EXA` | CC32; existing [Exe2Arc contract](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Exe2Arc.md); **new** `src/Commands/Archives/Exe2Arc*.cs`, independent native root under `tests/Commands.Exe2ArcNativeRoot` | DOS/Exec scanner and payload recovery, first one fixed marker/length family, then all seven. Implement real ReadArgs, safe naming, status and teardown. Pass that contract's normal/failure matrices and disclose each safety correction. No XAD dependency or executed SFX fixture. |
| `CC07.ARCH.XADABI` | CC07; existing `CopperSharp68k/Sdk.Amiga/XadMaster/XadMaster.cs`; **new** adjacent `XadTypes.cs`, `XadTags.cs` and native ABI fixtures | Versioned public structures, sizes, alignment, flags/tags, typed hook messages, borrowed/owned pointer rules and LVO gates. Qualify actual big-endian memory and callbacks; assembly-string tests alone are insufficient. Coordinate SDK/compiler rebuilds with qualification owners. |
| `CC07.ARCH.XADCORE` | CC07; **new** `src/Libraries/xadmaster.library/` and independent library native fixtures | Real library init/open/close/expunge, allocations, client registry, recognition/info dispatch, error text and hook ownership. Start one qualified client path, then implement the full required API. No host codec inside a gateway or empty successful placeholder. |
| `CC07.ARCH.XADCLIENT` | CC07 with CC31 reuse; **new** `src/Libraries/xadmaster.library/Clients/` | Pin installed client matrix and each grant; one decode/list fixture and negative corpus per required format/method. Add each client until the baseline matrix closes; cover password, multi-volume and nested/split input wherever the reference advertises them. Missing clients stay explicit rows. |
| `CC07.ARCH.XADDISK` | CC07/CC32; **new** `src/Libraries/xadmaster.library/Hooks/` and `tests/Commands.ArchiveNativeExecution` disk fixtures | DOS/device-backed disk extraction, geometry/range checks and progress/cancel callbacks. Require expected sectors and untouched guards on a disposable guest block device; deny any target resolved to a host disk. |
| `CC07.ARCH.METADATA` | CC07/CC32; **new** `src/Libraries/xadmaster.library/Metadata/`; reuse actual SDK DOS/locale owners where sufficient | Dates, protection, comments, names/encoding and link policy with precise versions and fallbacks. Audit XADUnTar's 64-bit/time-zone branches separately; a source request for locale.library 53/54 is not a Kickstart 3.1 call. Test post-2038/negative/range cases and unavailable extensions. |
| `CC07.ARCH.BZIP2` | CC07/CC31; **new** `src/Libraries/ArchiveCodecs/BZip2/`, command front end under `src/Commands/Archives/` | Pinned Bz2 modes/options and a guest streaming codec; begin one known decode vector, then all create/test/stream modes evidenced for 50.6. CRC, truncation, short I/O, bounded allocation and P/shared-image tests. |
| `CC07.ARCH.LHA` | CC07/CC31/CC32; **new** `src/Libraries/ArchiveCodecs/Lha/` | Versioned methods and header writer/reader shared only where contracts match. Cover LhA 2.7's actual modes and XAD2LhA's header 0/2 output. Do not require an external LhA process: the XAD2LhA source has its own writer. |
| `CC07.ARCH.LZMA` | CC07/CC31; **new** `src/Libraries/ArchiveCodecs/LzmaXz/` | Four distinct front ends over qualified stream/container primitives. Begin LZMA header/info plus a known decode vector, then XZ checks/filters/concatenation and all evidenced encode/decode modes. Dictionary limits, malformed sizes, allocation failure and streaming need separate cases. |
| `CC07.ARCH.RAR` | CC07/CC31; **new** `src/Libraries/ArchiveCodecs/Rar/` | Pin UnRAR 3.90 source/allowed implementation route and actual methods/modes. Qualify list/test/extract, passwords/volumes/solid data when supported, corruption and output policy. Do not claim RAR5 from the command name or a current host utility. |
| `CC07.ARCH.OFARC` | CC07/CC31; **new** `src/Commands/Archives/OFArc*.cs` with native format owners under `src/Libraries/ArchiveCodecs/` | Preserve original argument grammar, action precedence, every archive type and create/list/extract/test/stream mode actually present. Reuse native codec services only where format and metadata semantics agree; choose an admitted port or independent front end, not a CLR/ObjC host bridge. |
| `CC07.ARCH.HASH` | CC07/CC31; **new** `src/Libraries/ArchiveCodecs/Hashes/` and `src/Commands/Archives/OFHash*.cs` | Incremental native hashes with independent known-answer vectors, then exact OFHash selection, formatting and input/error modes. Qualify 64-bit lane operations for SHA-384/512; the existing Eval numeric result does not prove general wide arithmetic or hash correctness. |
| `CC07.ARCH.URI` | CC07/CC31 with CC28-CC30 service owners; **new** archive IRI adapter under `src/Commands/Archives/`; remote handlers/network/TLS at their actual subsystem owners | First pin which OFArc/OFHash modes accept which schemes. Resolve each to a real DOS handler or native service, with redirects/auth/TLS/error/cancel evidence where applicable. Implement local-file modes independently; a host mirror or silently rejected required scheme does not close remote modes. |

`ArchiveCodecs` is a proposed common native source owner, not a claim that all
codecs must be a single resident library. XAD's externally visible library
ABI belongs in `xadmaster.library`; command-only code may be linked into its
native command image. Keep per-invocation allocation and lifecycle ownership
explicit in either arrangement.

The command sources establish different XAD minimum opens: XADLibInfo and
XADList request version 1, XADUnTar version 9, XADUnDisk version 11, and
XADUnFile/XAD2LhA version 13. XADUnFile also opens utility.library version 37.
These minima do not prove every later structure/feature is available from
every version. XADList accesses obsolete base fields; qualify those layouts
or a behavior-equivalent versioned route before replacing them. XADUnTar's
locale 53/54 branches are conditional fallbacks in date conversion, not an
unconditional reason to block its core extraction. Audit the alternative
paths and observable timestamp results under `CC07.ARCH.METADATA`.

## Steps that can be executed in goal mode

The following slice IDs can be recorded in the ordinary progress ledger when
implemented; this document creates no completed entries. Each slice produces
a reviewable result and leaves the rest of its provider matrix open. Missing
reference runtime access does not prevent source-backed primitives or fixture
preparation, but it prevents original-command differential closure.

1. **ARCH-01: sources and contracts.** Use the pinned inventory and package
   hashes above; record source/license decisions, exact grammar and full
   format/mode rows in the new archive docs/command contracts. First finish
   Exe2Arc's help/reference evidence and XADLibInfo/XADList structures, then
   each remaining command. Output: no unnamed options, codecs or provider
   versions; unresolved rows carry owners and reference-access requirements.
2. **ARCH-02: finite fixture corpus.** Own a new
   `tests/Commands.ArchiveFixtures` fixture description/generator area. Create
   inert SFX layouts from Exe2Arc's matrix, small reference-generated archives,
   independent hash vectors and disposable disk geometry. Store licensed
   fixture provenance and expected byte/metadata hashes. Mutations cover
   truncation, checksum, path and allocation limits. Production code must not
   generate its own expected results; never execute an SFX wrapper.
3. **ARCH-03: first Exe2Arc native slice.** Implement a bounded RAR marker scan
   and CAB-length recovery primitive using guest memory, then an independent
   native root that calls that production code. Check every byte and guard,
   exact/short capacity, split windows, malformed offsets and reentrancy.
   Deliver only those named capabilities, then repeat for ACE, ARJ, HUNK-SFX,
   LZH and ZIP rewriting. Add actual ReadArgs and DOS file/naming ownership
   with the corresponding reference/safety cases before full command claims.
4. **ARCH-04: real XAD ABI and core.** Extend the existing SDK owner and create
   the guest library owner after source admission. Start with object/tag
   lifetime and a single real client; execute open, enumerate, get-info,
   callback, free and close across 68000/020/040. A stub or host decoder can
   test gateway marshaling but must be labelled as such, never codec success.
5. **ARCH-05: XAD front ends and clients.** Implement XADLibInfo on the real
   registry, then listing and one actual file-extraction path; expand every
   required client/method and each XADUnFile/XAD2LhA/XADUnTar option. Add
   password/cancel, name/metadata and failed-output cases at the owner that
   implements them. Only separately tested output-header behavior is shared
   with LhA. Every unimplemented installed client remains required.
6. **ARCH-06: disk extraction.** Add XADUnDisk's genuine disk-hook path against
   an explicitly created guest image. Verify sectors, geometry, no writes
   outside the image, metadata/error/cancel semantics and device teardown.
   Resolve the final target before any write; host block devices are forbidden
   fixtures, even if a path happens to look temporary.
7. **ARCH-07: CC31 codec families.** Take BZIP2, LHA, LZMA and RAR dependency
   rows one at a time: one licensed native known-answer primitive, its negative
   corpus, then the exact command front ends and all remaining mode rows.
   Share codecs with XAD after both contracts agree. Preserve non-ReadArgs
   grammars where original tools use them; do not collapse four LZMA/XZ tools
   into one aliased command with missing options.
8. **ARCH-08: OFHash and OFArc.** Implement one native hash and independent
   vectors, then all required algorithms and the original hash front end.
   Add OFArc one format/action at a time, including metadata and output-error
   behavior. Pin IRI mode requirements and attach native networking/handler
   success and cancellation fixtures only to those modes that need them.
9. **ARCH-09: full qualification and package closure.** Execute the complete
   per-command matrices against the original baseline and emitted native
   artifacts; bind the compiler/SDK/source/fixture and binary hashes. Check
   original placement and installed selection, exact grammar, output/status,
   resource teardown, bounded stack/heap, concurrency and required P flags.
   Count zero unowned options/clients/unsupported modes before closing CC31
   or CC32. A successful decompressor primitive never closes an entire command.

## Required fixture and failure evidence

For each implemented format/mode, keep a finite matrix with a real valid
sample, byte/metadata expectations, empty/minimum/larger-than-buffer inputs,
truncation at every header/payload boundary, incorrect checksum, unsupported
method/version, short Read/Write, allocation failure, interrupted operation
and repeat-after-failure cleanup. Add format-specific password, volumes,
filters, dictionaries and disk geometry where the reference supports them.
Recorded coverage is per case and command profile, not a broad archive pass.

Archive entry names need explicit tests for Amiga volume prefixes, relative
parents, separators, links, duplicate names, pre-existing targets, permission
failures and metadata failures. Keep all output inside disposable guest
storage. Any safety rule that differs from the original's observable behavior
must be stated and reconciled; neither silent traversal acceptance nor a
silent option removal is compatible completion.

Reference receipts contain exact binary/source identity, client versions and
loaded libraries, input/argument bytes, output bytes/metadata, stdout/stderr,
return/IoErr, cancellation/failure point and resource accounting. Native
receipts also identify the actual code run and prove there was no host codec
process or CLR implementation in the production dependency closure. A host
oracle is allowed only as an independent fixture tool, labelled and kept out
of that closure.

The [compiler report](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/compiler-qualification.md)
and [argument ownership](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/argument-ownership.md)
remain prerequisites. The current stack-only shipping command-context policy
does not forbid explicitly checked, invocation-owned codec buffers; it avoids
an unhandled compiler-bootstrap allocation failure. Do not infer a minimum
stack, pure library export ABI, or generic 64-bit arithmetic qualification
from previous unrelated native command probes.

No source, compiler/SDK, provider, binary or stable goal file was changed by
this audit. These work items remain executable follow-up work, with source
admission, original-runtime capture and all command/native completions open.
