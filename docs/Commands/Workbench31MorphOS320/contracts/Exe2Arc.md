# Exe2Arc contract

Profile: `morphos320`. Contract ID: `CC32.Exe2Arc.morphos320`. Goal steps:
CC01, CC07, and CC32. Recorded: 2026-08-30.

Status: **source-backed partial contract and bounded RAR4/CAB/ACE/ARJ/ZIP/LHA/LZH components;
no original-command execution, full-command qualification, or parity claim**. The
release member and source version agree, but source-to-binary correspondence
has not been proved. Every command/reference fixture below is pending. Unsafe source paths are
identified as unresolved compatibility decisions, not behavior to reproduce
by allowing memory corruption, unbounded execution, or resource leaks.

## Evidence and identity

`M` means bytes inspected on the pinned distribution; `S` means behavior
observed in the published source; `R` would mean a captured reference run.
The original-command evidence is M and S only. Generated-component host and
native checkpoints below are separate implementation observations, not R.
Source line numbers refer to the
unmodified member in the archive below, not a local vendor-code checkout.

| Item | Evidence |
| --- | --- |
| Distribution | [Official MorphOS 3.20 ISO](https://www.morphos-team.net/morphos-3.20.iso), locally `D:/TestData/MorphOSReferences/morphos-3.20.iso` |
| Full ISO SHA256 | `3761e191124cfd204a490915f8d285d55969e95b66c5c3cd1cf854bc71dab911` |
| Executable member (M) | `MorphOS/C/Exe2Arc`, logical block 175400, block size 2048, length 7652 bytes |
| Executable SHA256 (M) | `01d5fb8c3eab8bf19aab2221d1d587dfc081f9708c28d42ebe2313b8c733fe4f` |
| Readable version (M) | `Exe2Arc 1.6 (12.04.2016)`, version tag at member offset 7621 |
| Executable inspection | Starts `7f4d4f53`: packed native MorphOS executable. No decoded code or verified binary ReadArgs template is available. |
| Source package | [Official 3.20 command source](https://www.morphos-team.net/files/src/3.20/c.tar.bz2), 214038 bytes |
| Source package SHA256 | `db099324cbf4b07de70bcafc626af0b168903a714911f840652e1a364578b5ba` |
| Main source | `c/xad/exe2arc.c`, 626 lines; SHA256 `fcbf2f58345c5c74ecdadb28539413d78e66fa0c6b4c1f25f1409d35a9ed647d` |
| Source version (S) | Revision 6 and date 12.04.2016 at lines 1-4; `c/xad/Makefile:14` supplies major version 1. This agreement is not a binary-equivalence test. |
| Workbench 3.1 | Not in the inspected external-command inventory; do not add it to the exact `wb31` profile. Remaining Workbench media closure stays with CC00. |
| Pure/resident (M) | No Exe2Arc event in the observed installer P additions or resident scripts. Installed protection is unknown: classification remains `unresolved-not-nonpure`. Absence from those scripts does not establish non-purity. |

See the pinned [inventory](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/command-inventory.json)
and [media evidence](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/media-evidence.json).
ISO POSIX mode is not AmigaDOS protection. Source globals for library bases
and a writable scanner table do not establish the shipped binary's P policy.

### Per-file source licensing

| File in the command source archive | Observed grant and use boundary |
| --- | --- |
| `c/xad/exe2arc.c:9-24` | Copyright 1998 and later Dirk Stöcker; explicit LGPL 2.1-or-later grant. This is the behavioral source used here. Admission of derived implementation still needs the project's source/license record and distribution obligations. |
| `c/xad/Makefile` | Copyright 2016 MorphOS Team, all rights reserved; no explicit reuse grant found in this file. SHA256 `395c3aaa9abd58c292585587c0ab7d4fc1d185946e2323293aa83ddb52d78794`. Used only to identify version/build assumptions. |
| `c/xad/xad.notes` | A dated provenance note describing LGPL/freeware command ports; no per-file grant. SHA256 `0f8304916656697c08dc266569b57d86d520708fdda2184194f93237b1ee56af`. It does not relicense the directory. |

No vendor implementation or build script is copied into CopperOS by this
contract. Do not silently treat all XAD command files as sharing one license.

## Arguments and launch

The exact template at `exe2arc.c:38`, passed to DOS `ReadArgs` at line 93, is:

```text
FROM/A,TO,TYPE/K
```

| Slot | S-observed meaning |
| --- | --- |
| `FROM/A` | Required input filename. Open with `MODE_OLDFILE`; there is no command-private wildcard expansion, multiple-input vector, or stdin mode. |
| `TO` | Optional output name. It is not keyword-only: retain DOS positional and named-argument handling. Its file-first, directory-fallback behavior is below. |
| `TYPE/K` | Optional keyword string selecting a scanner by its extension or display name, compared without case. No prefix matching or additional aliases appear in this source. |

Use real DOS `ReadArgs`, three initialized invocation-owned pointer slots,
and exactly one `FreeArgs` after the returned strings are no longer needed.
Preserve DOS quoting, star escapes, keyword handling, missing-argument errors,
and interactive `?` continuation; do not add switches or a private tokenizer.
Exact help/continuation transcripts, empty quoted arguments, duplicate keywords,
and excess operands remain R gaps, even though the source template is known.

The source rejects a Workbench launch with return level 20. It waits for and
gets the startup message, calls `Forbid` and replies, and returns without
opening DOS (`74-83`). No Workbench argument conversion exists. Keep this
visible refusal while using the qualified startup owner for the actual ABI
and message lifetime. CLI startup opens `dos.library` version 37; failed open
returns 20 without a command diagnostic (`68-95`).

## Detection and recovered payload

This command recovers an embedded archive; it does not extract archive
members or execute the input. **Never call LoadSeg, RunCommand, Execute, or a
host process on a fixture's executable wrapper.** There is no xadmaster.library
open or XAD call in this source. DOS and Exec provide its system services.

Without TYPE, scanners run in the following fixed order (`616-624`). The first
successful scanner stops selection, including when opening/writing its output
later fails. Scanner order takes precedence over which archive starts earliest
in the input. With TYPE, only its matching entry runs. A selected type is not
automatically a detected archive; unknown types print the valid-type list
without running a scanner (`112-196`).

All byte offsets below are zero-based. LE and BE describe archive bytes, not
host-native integer layout. A nonzero start offset is required by every
scanner. A marker at offset zero can stop a scanner without returning success;
do not assume plain archives are accepted or that it will then find a later
marker. Except for the HUNK/SFX check in the LhA row, an executable header is
not validated.

| Order; TYPE spellings | S-observed recognition and size conditions | S-observed output |
| --- | --- | --- |
| 1; `zip`, `Zip` | Backward search for `50 4b 05 06` (EOCD), while remaining input is greater than 22 bytes. Use its LE32 central-directory size and offset; infer prefix correction from the EOCD's physical position. Read the first central entry's local-header offset to select the nonzero start. Source `243-286`. | Parse local, central, and EOCD records; adjust central entries' local-header offsets and EOCD's central-directory offset for the removed prefix. Copy member data and record name/extra/comment bytes. Stop after EOCD and its comment, not necessarily EOF. Source `289-354`. |
| 2; `ace`, `Ace` | Forward search with remaining input greater than 14 bytes; `**ACE**` at candidate offsets 7-13. No header CRC validation. Source `359-390`. | Copy candidate start through EOF unchanged. |
| 3; `rar`, `Rar` | Admit a forward read window only when more than 7 bytes remain at its start; inspect every complete seven-byte candidate in that window for `52 61 72 21 1a 07 00`. A later candidate may end exactly at EOF. The RAR5 marker is not this identifier. Source `405-436`. | Copy marker through EOF unchanged. No password, decompression, or archive integrity check. |
| 4; `cab`, `Cabinet` | Admit a forward read window only when more than 20 bytes remain at its start; inspect every complete 20-byte candidate for `MSCF`. LE32 cabinet length at +8 must fit the remaining file from the candidate, and the LE32 file-table offset at +16 must be less than that length. Source `441-477`. | Copy exactly the declared cabinet length; omit any trailer. Source `479-485`. |
| 5; `arj`, `Arj` | Forward search with remaining input greater than 50 bytes for `60 ea`; LE16 header length at +2; reflected CRC32 over the following header bytes, polynomial `edb88320`, complemented result compared with the following LE32 CRC. See the insufficient tail bound below. Source `490-552`. | Copy candidate start through EOF unchanged. |
| 6; `lha`, `LhA` | Read 100 bytes from file start; require `00 00 03 f3` at 0 and `SFX!` at 44; take a BE32 archive start from 52-55 and seek there. Short-read and out-of-file bounds are not safely established. Source `557-575`. | Copy that start through EOF unchanged; generated extension is `lha`. This is the HUNK/SFX path, not the later generic LhA-header heuristic. |
| 7; `lzh`, `Amiga-LhA` | Forward search with remaining input greater than 21 bytes; candidate +2 is `-`, +3 is `l`, +4 is `h` or `z`, and +6 is `-`. The source tests byte 20 of the scan window, not candidate +20, against level 2. Source `580-611`. | Copy candidate start through EOF unchanged; generated extension is `lzh`. No header checksum test. |

ACE, RAR, ARJ, LhA and LZH share the copy-to-EOF extractor (`393-400`). This
also preserves trailing wrapper bytes; it is not a promise that the result
is a valid archive. ZIP handles only the three record signatures listed
above; data-descriptor records, ZIP64, split archives, empty archives and
false EOCD markers require fixtures, not claims of general ZIP support.
It does not decompress data or verify member CRCs. The scanner does not check
the central-directory signature before using the first entry's offset.

### Buffer and arithmetic limits

The source allocates 103424 bytes: a 102400-byte scan/copy buffer followed by
1024 bytes of path scratch (`46`, `107`). Files may be larger than this buffer.
Forward scanners retain overlapping windows; ZIP scans overlapping windows
backwards. Test markers and records across a 102400-byte boundary as well as
the strict small-file conditions in the table.

RAR advances an unsuccessful window by `windowBytes - 7 + 1`, retaining six
bytes of overlap (`418`, `428-429`). CAB advances by `windowBytes - 20 + 1`,
retaining 19 (`454`, `468-469`). The strict outer-window admission and inclusive
candidate bounds are distinct. For the source's 102400-byte window, the
following EXA-S13 cases therefore have different source-observed outcomes:

| Fixture suffix | Marker offset | File bytes | Scanner outcome from S; R pending |
| --- | ---: | ---: | --- |
| `rar.eof-last-first-window` | 102393 | 102400 | Found; seven-byte candidate ends at EOF inside the first window. |
| `rar.eof-at-next-window` | 102394 | 102401 | Not found; exactly seven bytes remain at the next window's start, so that window is not admitted. |
| `rar.eof-inside-next-window` | 102395 | 102402 | Found; eight bytes remain at the next window's start and the candidate begins at index one. |
| `cab.eof-last-first-window` | 102380 | 102400 | Found when the CAB length/table predicate passes. |
| `cab.eof-at-next-window` | 102381 | 102401 | Not found; exactly 20 bytes remain at the next window's start. |
| `cab.eof-inside-next-window` | 102382 | 102402 | Found when the CAB predicate passes; candidate index one in a 21-byte window. |

After a recognizable candidate, both scanners issue the unusual relative
`Seek(-candidateIndex, CURRENT)` while the current cursor is at the end of the
read window (`423`, `462`). Its success gates accepting the candidate. The
scanner stops at that candidate even when the relative seek fails or its
absolute offset is zero; it does not search for a later marker. A nonzero
accepted offset then causes a final absolute Seek whose result the source
ignores (`433`, `473`). This is EXA-U08, not permission for a replacement to
copy from an unconfirmed position. A bounded component must expose
matched-but-not-positioned separately and perform no payload copy on that path.

File size comes from the FIB's 32-bit size; scanner sizes are ULONG, and seeks
and several offsets use LONG. No 64-bit file-size contract is established.
Addition, subtraction, signed conversion, offset beyond EOF, and the 2 GiB /
4 GiB boundaries need explicit bounded handling and reference evidence. Do
not infer a large-file mode or silently let guest address arithmetic wrap.

ARJ creates a 256-longword automatic CRC table, 1024 bytes before other stack
needs (`492`). This is a source observation, not the measured stack requirement
of the packed binary or a proposed native implementation. Measure the actual
target artifact; the general resident context limit is not its data-buffer
size. Explicitly owned AllocMem buffers are distinct from compiler-generated
resident bootstrap heap allocation.

## Output naming and file effects

The naming path is source `124-170`:

1. Always construct a candidate from the complete FROM path. Replace the last
   dot-suffix in its final component, or append a dot when it has no suffix,
   then append the selected lowercase extension. Directory/volume prefixes
   remain. For example, `RAM:in/demo.exe` selected as ZIP becomes
   `RAM:in/demo.zip`.
2. Without TO, open that generated path as `MODE_NEWFILE`. With TO, first open
   the literal TO value as `MODE_NEWFILE`; it is not preclassified as a directory.
3. Only if that TO open fails, build a second path with DOS `AddPart`: TO plus
   the generated candidate's basename. Open that path as `MODE_NEWFILE` too.
   The fallback buffer is 512 bytes, and the source ignores AddPart's result.
4. An existing target can be truncated; there is no overwrite switch or prompt.
   No intermediate-directory creation or timestamp/protection/comment copying
   appears. Input/output aliases are not detected.
5. After extraction, close the output. A failed extraction then attempts to
   delete the selected output name, even if a previous file was truncated.
   Close/DeleteFile results are ignored. There is no temporary-file commit.

The initial filename copy is unbounded and the fallback buffer begins halfway
through the 1024-byte scratch area. Long paths can overflow or overlap it.
Neither 512 nor 1024 is a verified documented filename limit. Do not reproduce
this memory hazard or treat AddPart failure as successful naming. Boundary
behavior and a safe rejection/ownership policy must be recorded before those
cases can be claimed compatible. All overwrite/alias tests use disposable
guest files; never use reference media or valuable user files as targets.

## Return, diagnostics, break and ownership

| Path | S-observed result; remaining R gap |
| --- | --- |
| Extraction returns a nonzero byte count | Set command return to 0; print a saved-byte/path report and a warning to check correctness. ZIP's reported count is input size minus selected start, which can exceed actual output length if trailing bytes are ignored. |
| No detection, unknown TYPE, input/FIB/buffer/output failure, or extractor returns zero | Retain return level 20. No level 5 or 10 assignment appears. Zero-length recovered output is not treated as success by the caller. |
| ReadArgs failure | PrintFault with current IoErr and return 20. Parser messages and destination stream depend on DOS and need capture. |
| Normal scanning | Progress goes through DOS Printf/Output, is flushed, and failed detection uses CR plus ESC `[K` to erase it. Successful detection adds a line before extraction. Redirected output still needs exact byte capture; no terminal-detection branch exists. |
| Scanner/extractor I/O failure | Scan seeks/reads can become no-detection. DoCopy requires the entire requested chunk from Read and Write; short positive results are failures, without retry. ZIP has extra unknown-record/short-header diagnostics and the unsafe EOF path below. |
| Ctrl-C | Only ZIP's record loop polls pending Ctrl-C. The bounded owner now exposes that same optional signal boundary: it sets `ERROR_BREAK`, returns the source's nonzero `filesize-start` count, and retains partial output; scanners, common copy and other extractors do not poll. Exact guest signal timing and post-cleanup IoErr still require reference capture. |
| Final IoErr | Except the ZIP break assignment, no final SetIoErr policy exists. Diagnostic and cleanup DOS calls may affect it. Do not assert zero on success, preserved initial error, or ERROR_BREAK after cleanup without capturing the binary. |

Most diagnostics are fixed English Printf strings rather than locale catalog
lookups. One TYPE diagnostic contains source byte `a0` before the quoted type.
Binary output encoding, number formatting, line endings, redirection and
error-stream selection are
not closed by a source-text transcription. Store stdout and stderr separately
as bytes in reference receipts, together with return level and final IoErr.

Source cleanup releases the scan buffer with its exact size, closes any opened
output and input, frees RDArgs, and closes the opened DOS library. Borrowed
Output is flushed, not closed. However, line 205 calls
`FreeDosObject(DOS_FIB, 0)` instead of passing the allocated FIB. This is a
source-only potential leak/invalid release; the packed command has not been
executed to establish the effect. A replacement must release its actual owned
FIB exactly once and document the safety correction. No resource success is
proved by merely returning the expected status.

Use invocation-owned writable state, source/destination buffers, RDArgs and
library leases. Do not infer that source globals may be shared across resident
invocations. Installed flags and the final command's purity requirement remain
CC00/CC06 gates. If P is required, qualify concurrent calls into one image,
shared-image write protection and all failure cleanup before setting it.

### Explicit unsafe or ambiguous source paths

| ID | S-observed concern | Required resolution; no unsafe imitation |
| --- | --- | --- |
| EXA-U01 | Unbounded filename copy, overlapping fallback scratch, ignored AddPart failure; empty name decrement can underflow if such an input reaches naming. | Guarded path-length/empty-name probes and a documented safe naming failure policy. |
| EXA-U02 | ZIP Read of a four-byte record header that returns fewer bytes prints an EOF diagnostic without setting its loop error. It can repeat indefinitely until a signal changes the loop condition. | Time- and output-bounded original run; require termination in the replacement and retain the behavioral discrepancy explicitly. |
| EXA-U03 | ZIP cancellation can exit with a zero internal error and report success, keeping incomplete output; later cleanup may change IoErr. | Signal-at-boundary fixtures covering output retention, return and final IoErr; explicit safe cancellation policy if compatibility conflicts. |
| EXA-U04 | LhA rejects negative reads, not short reads; it examines bytes through 55 without establishing their availability. The claimed start is not checked against file size. | Short-file/uninitialized-memory and offset bounds checked before consumption; separate defined-input parity from malformed-input correction. |
| EXA-U05 | ARJ checks header length plus four before reading four further CRC bytes. It does not establish the entire CRC tail is available. | Header/CRC truncation at window and EOF boundaries with guard checks. |
| EXA-U06 | LZH level predicate uses scan-window byte 20 rather than candidate byte 20. | Paired fixtures with differing values at both positions; source-to-binary observation and explicit compatibility decision. |
| EXA-U07 | FIB release uses NULL; output Close/DeleteFile failures are ignored; same-file output can truncate the source. | Allocation and file-identity tracing on disposable files; actual ownership cleanup and documented output failure policy. |
| EXA-U08 | Several scanners ignore the final seek result, and malformed archive arithmetic can wrap or select out-of-file offsets. | Inject final-seek failures and reject unsafe lengths/addresses before I/O; do not claim untested large-file support. |

## Finite fixture matrix

These are required, named cases, **all pending**. An expanded case uses stable
suffixes instead of hiding untested combinations. Every reference receipt
records input SHA256, member hash, argument bytes, filesystem setup, stdout,
stderr, return level, final IoErr, output bytes/name/metadata, elapsed or
instruction bound, injected failure/signal point, and retained resources.
Source-derived expectations are not reference observations.

| IDs | Inputs / setup | Expected evidence or decision |
| --- | --- | --- |
| EXA-S01.zip-relative; EXA-S02.zip-absolute | Small SFX ZIPs whose central/local offsets exclude/include the wrapper; one member, extra data, filename and archive comment. | Output offsets rebased correctly and data unchanged; actual length versus printed count. |
| EXA-S03.ace; EXA-S04.rar; EXA-S05.cab; EXA-S06.arj; EXA-S07.lha; EXA-S08.lzh | One recognized nonzero-start layout per table, valid ARJ CRC, safe offsets and lengths. Use inert wrapper bytes. | Selected scanner, exact output bytes; CAB drops trailer, shared copy paths keep it. |
| EXA-S09.zip-trailer | Valid SFX ZIP with bytes after the EOCD comment. | Output ends after comment; compare printed count with input and output lengths. |
| EXA-S10.two-types | One input containing an earlier ACE candidate and later valid ZIP. Repeat unrestricted, TYPE ace, TYPE zip. | Scanner priority and TYPE restriction, not earliest-marker selection. |
| EXA-S11.type-names | Each row's extension/display name in lower, upper and mixed case, with its matching fixture. | Exact accepted spellings; the `lha` versus `lzh` distinction and generated suffix. |
| EXA-S12.naming | FROM with no suffix, multiple dots, dots in directory, volume prefix and quoted spaces; TO omitted, literal file, then existing directory. | Exact names, fallback order and AddPart path semantics; no overwrite prompt. |
| EXA-S13.window | For each scanning family, place its relevant header across a 102400-byte boundary; include ZIP backwards-window boundary. | Correct overlap and copy across multiple chunks, no lost/duplicated bytes. |
| EXA-S14.redirect | Run successful and no-detection input with normal and redirected output. | Progress/erase bytes, flush behavior, saved-count formatting and stream attribution. |
| EXA-F01.args | No FROM; missing TYPE value; extra positional; unknown option; duplicate keyword; empty quoted values; `?` continuation. | Real ReadArgs template and transcripts, levels, final IoErr and no leaked RDArgs. |
| EXA-F02.launch | CLI DOS-open failure and Workbench startup message. | 20, message ownership/order, no unowned library close or input/output activity. |
| EXA-F03.type | Unsupported TYPE and known TYPE with a different archive. | No scanner versus selected scanner, valid-type order and distinct diagnostics. |
| EXA-F04.no-marker | Inert data plus each family's threshold lengths: minimum minus one, minimum, minimum plus one. | Defined no-detection and termination; no stale buffer reads. |
| EXA-F05.offset-zero | Plain archive at zero, then zero marker plus later valid candidate. | Which scanner stops or rejects; no invented plain-archive support. |
| EXA-F06.acquire | Fail input Open, FIB allocation, ExamineFH, then buffer allocation individually. | 20 and stage-specific diagnostics; owned resources released without losing the recorded root error. |
| EXA-F07.output | Fail literal TO open, fallback open, write, Close, and cleanup DeleteFile separately; existing target only on disposable guest storage. | Actual fallback, truncation/removal and return/IoErr behavior; close/delete gaps exposed. |
| EXA-F08.io | Negative/short Read and Write at scan, header and payload boundaries; initial/intermediate/final Seek failure. | Exact failure versus no-detection; EXA-U02/08 must be bounded and recorded, never accepted as a harness hang. |
| EXA-F09.zip-records | Wrong central signature, false EOCD, empty ZIP, unknown record, data descriptor, ZIP64 marker, truncated header/comment. | Supported subset distinguished from rejection or unsafe source outcomes; no decompressor success inferred. |
| EXA-F10.format-bounds | RAR5 marker; oversized CAB length/offset; bad or truncated ARJ CRC; short/out-of-range HUNK-SFX; paired LZH level bytes. | Every EXA-U04/05/06 boundary and no out-of-bounds read/write. |
| EXA-F11.paths | Generated/joined path lengths around both scratch-region boundaries, empty components, input/output aliases. | EXA-U01/07 decision, guards and bounded cleanup; no valuable input can be truncated. |
| EXA-F12.arithmetic | Sparse/disposable input and injected FIB/Seek boundaries around signed/unsigned 32-bit limits. | Defined safe rejection or supported result; no wrapped guest address or overlarge write. |
| EXA-F13.break | Ctrl-C before scan, during common copy, before ZIP record, during ZIP payload, after EOCD. | Which paths poll, return/IoErr/output-retention behavior; EXA-U03 policy remains explicit. |
| EXA-F14.isolation | Repeated success/failure and interleaved invocations against one native image, with library/argument/buffer accounting. | No mutable shared command state, stale handles or leaked owned allocation; apply required P policy once original metadata closes. |

## Implementation gates and next slice

1. Bind the packed binary to captured help and representative reference runs;
   preserve the verified source hash and version without calling that parity.
   Resolve EXA-U01 through EXA-U08 with bounded disposable fixtures and written
   compatibility/safety decisions. Missing reference execution remains visible.
2. Under dependency `CC07.ARCH.EXA`, implement one source-owned, independently
   reviewed scanner/copy slice at a time using the guest memory/DOS owners.
   Start with RAR's fixed marker and CAB's explicit length, then the other
   layouts and ZIP rewriting. Synthetic scanner success is not full command
   or format completion. Add real ReadArgs, naming, output and teardown only
   with their corresponding contract cases.
3. Preserve the existing [startup and argument ownership](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/argument-ownership.md)
   and [compiler qualification policy](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/compiler-qualification.md).
   No host codec, managed filesystem, Shell builtin, vendor-code copy, or
   substitute parser belongs in the production closure.
4. Run all applicable fixture rows against native 68000/68020/68040 artifacts
   and the pinned original; bind source/tool/binary hashes, verify package
   metadata and the resolved pure/resident policy. Completion stays open until
   the command-level options, semantics, native, cleanup, packaging and
   differential gates pass. See the [archive work package](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/archive-provider-work-package.md).

## Bounded implementation: one RAR4 or CAB candidate

[Exe2ArcHeaderProbe.cs](D:/Koodit/GIT/CopperOS/src/Commands/Exe2ArcHeaderProbe.cs)
is an independently written implementation of the recorded marker/length
predicates. No vendor implementation or build script was copied or translated.
It consumes one caller-owned guest header window and known unsigned file
offset/length. RAR4 returns the bytes through EOF; CAB returns its LE32 declared
length after checking that it fits and that the LE32 file-table offset is
strictly lower. RAR5 does not match. Reads use individual bytes, including
unaligned guest addresses; payload data is neither read nor allocated.

The public one-candidate helper rejects offset zero without specifying whether
an enclosing search should continue after that marker. A candidate needs at
least 7/20 bytes, respectively; the enclosing scanner's window starts require
strictly more. It preserves the limited
source predicate rather than claiming CAB integrity: for example, length 1
with table offset 0 can match when the surrounding file is long enough. It
does not add a 36-byte CAB minimum or reserved-field checks absent from the
recorded predicate. Source-to-packed-binary correspondence remains open.

Invalid, short, wrapping, out-of-file or unmapped header spans are rejected
before access, always with zero result length. There are no writes or mutable
static scratch fields. Known unsigned byte lengths do not establish support
for DOS signed Seek or files above its qualified range. These are safety
properties of the primitive, not observed outcomes of malformed original
command invocations.

[Exe2ArcHeaderProbeTests.cs](D:/Koodit/GIT/CopperOS/tests/Commands/Exe2ArcHeaderProbeTests.cs)
initially passed **54 host cases**, with **347 inclusive Shell/command tests** passing
and zero skipped. Fixtures are inert bytes only: no wrapper is executed and
no file is opened or written. They exercise marker mutations, RAR5 rejection,
unaligned little-endian fields, the then-specified strict EOF thresholds, short windows, high-bit
lengths, table/length boundaries, address overflow, mapped-range rejection,
interleaved callers and zero managed allocation after warmup. Package builds
have zero warnings/errors. An initial no-restore attempt retained an older
local-SDK restore graph and failed on missing references; restoring only the
CopperOS package graph corrected setup without any source/test change or
compiler/SDK build.

The [host receipt](D:/Koodit/GIT/CopperOS/obj/exe2arc-header-probe/1c74c36d26aa42a1849848ecc68e79a0/receipt.json)
has SHA256 `9405e0dd6c963ecff9ac72b299445577f28576fbee2ea167fbe6089a1a50edd1`.
Production source SHA256 at that checkpoint is
`9fc73b8e178ca6d9cbcd88ec27b60d866c725bb7549a448c11bce701a1b12751`;
test source is `9f0a6a5bd8bca1b6cd019305913aa9848f732f4c5c2ec0f7fc3c5fa48f07ce6d`.
This is not reproducible native artifact qualification or pure/resident admission.

A subsequent review found one boundary error: a nonempty valid header window
whose final byte is `$FFFFFFFF` was rejected. The unsigned span check now
subtracts `headerBytes - 1` after establishing a nonempty header. The unchanged
four new boundary cases first report two failures for valid last-byte reads
and two passing wrap controls; after the one-line correction all four pass.
That checkpoint has **58 focused and 351 inclusive passing host cases**, zero skips,
warnings or errors. These sparse host addresses do not imply a physical
32-bit address bus on 68000.

The new [boundary repair receipt](D:/Koodit/GIT/CopperOS/obj/exe2arc-header-probe/20260830T150127Z-last-byte-a5f85643/receipt.json)
has SHA256 `02090c22712afe8247826b7129dd48fb7dbe10cbe71feb8f750478b5943523aa`.
Corrected production source is
`dd79842d71e0802509d00daaaadbb3ac47a332a15381cb2c4d12dd8a84aac646`;
test source is `ad90d6f57b2fa4b0d68087c799d6e288f5668720f5896b09dede088472357394`.
Only Commands and its tests were rebuilt with project-reference building
disabled; Shell, compiler, SDK and CopperStart projects were not rebuilt.
The first receipt and its exact sources/binaries remain unchanged.

### Historical native header checkpoint: the earlier strict predicate

The immutable [native header receipt](D:/TestData/CopperOSCommands/Exe2ArcHeaderNative/20260830T151336Z-79687144/summary.json)
has SHA256 `8a23cfefb17352f25e33f465352d2e983d755bc87f8a2b0c4cbd4e5c19db0621`.
It binds the exact `dd79842d...` source above, source-linked probe, package SDK,
private compiler/source/restore closure and executor to **228 actual generated
primitive invocations**: 76 per CPU, including 36 caller invocations in
instruction-interleaved pairs. There were **zero original or full-command
runs**. One immutable image per CPU was reused with independent scratch and
4 KiB/16 KiB stacks; code/input/stack/control guards passed, with no host
gateways. The observed maximum stack use was 128 bytes, not an approved
minimum stack size. The fixture distinguishes the 68000's physical 24-bit
address bus from 020/040 address space and rejects unsigned wrap before access.

| CPU | HUNK bytes | SHA256 |
| --- | ---: | --- |
| 68000 | 1860 | `3648923fb844eb54186c9b78b7e9eed3566e01b279b8ee14a1f7606157ed9bd7` |
| 68020 | 1852 | `00e5891dc52acf260974f588584e15ff0869f0bfa9f3ea1c7419afa5e4bcdc50` |
| 68040 | 1856 | `ab80650dedceedae76cb8cd1a95b79b4b51938473a039edbd222ad32e020d206` |

All three native compilations reproduced byte-identical HUNKs from the same
frozen managed root. This is not a fresh C# source rebuild or a claim that a
new compiler was rebuilt from that snapshot. Ten separate negative controls
passed: malformed/missing inputs, wrong CPU/suite, existing report and alias
protection, deliberately incorrect native result, and forbidden image/input/
foreign accesses. Four deliberately invalid native body attempts are excluded
from the 228 successful primitive invocations. The negative-control receipt
SHA256 is `6452075fde17976d9a2392f227b57e28b6cfe09f434ab607fc190d96cbeae110`.

This result proves execution of that bounded helper's then-specified strict
per-candidate predicate. The subsequent source audit below found that predicate
was not the source scanner's candidate rule. Retain the receipt; do not relabel
it as scanner parity or current-predicate qualification.

### EOF candidate correction, H2

Re-reading source lines `410`, `418`, `446` and `454` exposed the distinction
between strict window admission and inclusive candidate availability. The
helper's earlier `remaining <= headerSize` rejection improperly discarded a
complete EOF candidate already inside an admitted window. Its check now rejects
only `remaining < headerSize`. Window admission remains the scanner's job; a
global change from `>` to `>=` in that scanner would also be incorrect.

Six new source-derived candidate fixtures first failed against the unchanged
`dd79842d...` source and then passed after the correction. Their bodies were
unchanged; two earlier equality expectations were explicitly aligned with the
corrected contract. The resulting **64 focused and 357 inclusive host cases**
pass with zero skips, warnings or errors. These six cases only test the helper;
the six full scanner triplets above are separate required cases.

The [H2 red/green receipt](D:/Koodit/GIT/CopperOS/obj/exe2arc-header-probe/20260830T153505Z-window-candidate-9b9230fa/receipt.json)
SHA256 is `e9909a365d105926106ebb2e6eeb25946a47fec593aacb1f4ce82d9f0e508b16`.
Production source is `de0cb274b2c3a64fcaa10daa7f6c1890ca70ba9e6567ec28c1cef73723c1dada`;
test source is `de75a4f839e8240400d7269c6a6be956aa8d0f18bd5ef3716a1d15abd56cb799`.
Only Commands and Commands.Tests were rebuilt, with package dependencies and
project-reference builds disabled. There is no native result yet for H2.

Next: add bounded borrowed DOS-window RAR/CAB scanning and a separate copy
primitive with EXA-S04/S05/S13 and EXA-F04/F05/F08/F10. Preserve raw count/seek
results, an explicit captured-IoErr tag, stage and confirmed completed bytes;
do not infer full-command error, retry, cancellation or deletion policy.
File acquisition, type priority, ReadArgs, naming, diagnostics, cleanup and all
other formats remain required.

## Bounded implementation: RAR4/CAB/ACE DOS scanning and copy, 2c

`CC32.Exe2Arc.2c` adds independently written components. No vendor source or
build script is copied or translated. The scanner uses the corrected candidate
predicate after strict window admission and preserves the source's 6/19-byte
overlap, first recognized candidate, offset-zero stop and intermediate relative
seek. Internal raw inspection can recognize offset zero so the scanner stops;
the existing public one-candidate API continues to reject zero without reading
it. This internal reuse does not change the public helper's contract.

| Owner | Bounded responsibility |
| --- | --- |
| [Exe2ArcIo.cs](D:/Koodit/GIT/CopperOS/src/Commands/Exe2ArcIo.cs) | `IExe2ArcIo` exposes raw Read/Write/Seek/IoErr plus guest memory. The optional `IExe2ArcBreakSource` models only the source ZIP loop's Ctrl-C poll and `ERROR_BREAK` assignment. Status and observation types own no resources. |
| [Exe2ArcForwardScanner.cs](D:/Koodit/GIT/CopperOS/src/Commands/Exe2ArcForwardScanner.cs) | `ScanRar4`, `ScanCabinet`, `ScanAce`, `ScanArj`, and `ScanLzh` borrow input and scratch; return status, archive offset, payload length and last I/O observation. ACE recognizes `**ACE**` at candidate offset +7; ARJ validates the source marker, variable header and reflected CRC; LZH preserves the source's scan-window byte-20 level predicate. Only Completed confirms the final input position. MatchedNotPositioned retains the recognized offset/length but prohibits copy. |
| [Exe2ArcLhaScanner.cs](D:/Koodit/GIT/CopperOS/src/Commands/Exe2ArcLhaScanner.cs) | Fixed 100-byte LhA HUNK/SFX probe with big-endian start extraction. Positive short reads and out-of-file starts are rejected before unsafe use; the source's repeated absolute seek is exposed as a separate final-position observation. |
| [Exe2ArcZipScanner.cs](D:/Koodit/GIT/CopperOS/src/Commands/Exe2ArcZipScanner.cs) | Bounded backward EOCD scan that derives the central-directory prefix correction and first local-header offset. It stops on the latest EOCD candidate, rejects wrapped or out-of-file arithmetic, and leaves ZIP record rewriting/extraction to a separate owner. |
| [Exe2ArcZipRecordRewriter.cs](D:/Koodit/GIT/CopperOS/src/Commands/Exe2ArcZipRecordRewriter.cs) | Allocation-free in-place rebasing for local, central and EOCD records. It adjusts only the central local-header offset and EOCD central-directory offset, rejecting short, unknown or overflowing records before writes. |
| [Exe2ArcZipRecordExtractor.cs](D:/Koodit/GIT/CopperOS/src/Commands/Exe2ArcZipRecordExtractor.cs) | Borrowed-handle streaming owner for local, central and EOCD records. It reads exact record tails, rewrites offsets in scratch, copies member/comment bytes through the shared exact-chunk copier, and applies the source ZIP-only Ctrl-C boundary through an optional signal owner. It still does not own close/delete policy. |
| [Exe2ArcPayloadCopy.cs](D:/Koodit/GIT/CopperOS/src/Commands/Exe2ArcPayloadCopy.cs) | `Copy` consumes a caller-selected length from already-positioned input. Exact chunks only; no Write after a short Read, and no retry after any short Write. It does not seek. |
| [NativeExe2ArcIo.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeExe2ArcIo.cs) | One-APTR adapter invokes the actual public DOS Read/Write/Seek/IoErr imports. DOS base, BPTR handles and writable scratch must already be supplied by their owner. Its mapping check validates that owner's stated scratch extent; it does not ask the OS whether arbitrary memory is mapped. |
| [Exe2ArcOutputName.cs](D:/Koodit/GIT/CopperOS/src/Commands/Exe2ArcOutputName.cs) | Invocation-owned, allocation-free generated-name builder. It matches the source's final-component suffix rule, but rejects unterminated input and insufficient destination capacity instead of reproducing the source's unbounded/overlapping scratch writes. |
| [Exe2ArcOutputSelection.cs](D:/Koodit/GIT/CopperOS/src/Commands/Exe2ArcOutputSelection.cs) and [NativeExe2ArcOutputIo.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeExe2ArcOutputIo.cs) | Borrowed output-open adapter and source-order selector: literal `TO` first, then two `AddPart` calls into a caller-mapped fallback buffer only after the literal open fails. The caller retains Close/Delete ownership. |
| [Exe2ArcTypeSelection.cs](D:/Koodit/GIT/CopperOS/src/Commands/Exe2ArcTypeSelection.cs) | Allocation-free, bounded `TYPE/K` matcher. It accepts only the exact case-insensitive extension/display-name pairs recorded by the source (`zip`, `ace`, `rar`, `cab`/`cabinet`, `arj`, `lha`, `lzh`/`amiga-lha`); prefixes, unknown names and unterminated guest strings are rejected. |
| [Exe2ArcScannerSelection.cs](D:/Koodit/GIT/CopperOS/src/Commands/Exe2ArcScannerSelection.cs) | Allocation-free scanner dispatch. Explicit TYPE selects one scanner; unspecified dispatch follows ZIP, ACE, RAR, CAB, ARJ, LhA, LZH. Only `NoMatch` falls through; offset-zero and unsafe positioning are terminal. |
| [Exe2ArcResultPolicy.cs](D:/Koodit/GIT/CopperOS/src/Commands/Exe2ArcResultPolicy.cs) | Command-level return and cleanup decisions. Only nonzero completed extraction succeeds; failed/empty extraction requests close-then-delete, while pre-open failures do not. It emits no diagnostic bytes or DOS calls. |
| [NativeExe2ArcArgumentGate.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeExe2ArcArgumentGate.cs) | Invocation-owned DOS `ReadArgs` boundary for the exact `FROM/A,TO,TYPE/K` template and three result slots. It retains the parser lease for the command owner and performs no file, FIB, buffer or diagnostic work. |
| [NativeMorphOSExe2ArcCommand.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSExe2ArcCommand.cs) | Bounded MorphOS frontend composition of parser, input/FIB/workspace, dispatch, naming/output, ZIP or EOF extraction and cleanup. The input is never loaded or executed. |

The active scan/copy window is exactly 102400 bytes. A nonzero operation needs
at least that capacity and a nonnull, nonwrapping mapped active span. The
components qualify file/payload lengths through `int.MaxValue`; larger values
return UnsupportedRange before I/O. This is an explicit component admission
boundary, not a newly advertised limitation or inferred behavior of Exe2Arc.
Zero-length copy completes with no I/O or unused-buffer access. The original
command still does not treat a zero extracted-byte count as successful output.

An observation contains stage, raw signed result, request bytes, confirmed
output bytes and an independent IoErr-captured flag/value. Exactly -1 causes
immediate IoErr capture; successful, zero and positive-short transfers do not.
ZIP cancellation is tagged separately as `BreakObserved` because it assigns
`ERROR_BREAK` without a failing transfer; its source-visible count is the
remaining input span, not a claim about bytes actually written.
The flag distinguishes a captured zero from no observation. Confirmed output
includes positive short Writes no larger than their request. A -1 may have
unreported handler side effects; the count does not assert that no bytes were
written. Scanner observations have zero output bytes. No command return,
SetIoErr, diagnostic, retry, Ctrl-C, overwrite, alias or deletion policy is
invented. The components neither acquire nor close/free handles, libraries or
buffers. An eventual frontend must not automatically impose the Foundation
Finish helper's selected-error reset on this command's still-unresolved ambient
IoErr behavior.

### Safe generated-name owner

`Exe2ArcOutputName.TryBuild` is the first frontend ownership slice. It reads a
bounded NUL-terminated FROM path from guest memory, finds the last dot before
the final `/` or `:` separator, appends the selected lower-case extension, and
writes one terminating NUL into a caller-mapped destination. It performs no
allocation or DOS call, and rejects a missing terminator, empty extension,
overflowed arithmetic, insufficient capacity, or an unmapped destination
before writing. The source's 1024-byte scratch layout and 512-byte `AddPart`
fallback remain frontend policy; this helper deliberately does not claim those
resource or DOS ownership gates.

`Exe2ArcOutputSelection.TryOpen` now covers the next frontend ordering gate.
With no TO it opens the generated name directly. With TO it tries the literal
name first; only failure permits fallback construction and a second open. A
failed first or second `AddPart` stops before another DOS call, and a fallback
open failure returns no borrowed handle. It never closes or deletes a handle or
path, so extraction-result cleanup remains with the eventual command owner.

`Exe2ArcTypeSelection.TrySelect` covers the explicit `TYPE/K` dispatch gate
without allocation or DOS calls. It bounds the guest string at 64 bytes and
matches the source's exact extension/display-name pairs case-insensitively;
unknown, prefix and unterminated values remain rejected rather than silently
selecting a different scanner.

The source-backed ARJ component now validates `60 ea`, the little-endian
variable header length, the reflected `edb88320` CRC and the strict remaining
file bound before returning a copy-to-EOF payload. It rejects incomplete CRC
tails before reading them. Managed coverage exercises valid, bad-CRC, later-
candidate and truncated-tail cases; this extension has not yet been added to
the resident supplied-DOS receipt.

The LZH component now preserves the source's seven-byte marker predicate and
its unusual scan-window byte-20 level check, with bounded overlap and offset
handling. Managed coverage includes a paired candidate-byte/window-byte case
and the high-level rejection path; it remains host component evidence pending
resident execution.

The LhA component now performs the source's fixed 100-byte HUNK/SFX probe,
extracts the big-endian archive start and preserves the repeated absolute seek
ordering. Positive short reads, out-of-file starts and final-seek failure are
bounded explicitly; the managed cases are host component evidence only.

The ZIP component now performs the source's backward EOCD search, reads the
first central entry's local-header offset, and returns the prefix correction
needed by the eventual record rewriter. It preserves latest-candidate stop
ordering and rejects wrapped central offsets or incomplete central tails before
additional I/O. ZIP rewriting, member copying and cancellation remain open.

`Exe2ArcZipRecordRewriter.TryRewrite` now owns the bounded in-place mutation
primitive for the three source record signatures. Local records remain
unchanged; central and EOCD offsets receive the scanner's correction only after
length, mapping and overflow checks. Streaming reads/writes, record ordering,
member data copying and Ctrl-C behavior remain with the command owner.

`Exe2ArcZipRecordExtractor.Extract` now composes those records with the shared
exact-chunk copier. It preserves the source order of local, central and EOCD
records, rebases central/EOCD offsets before each output write, and stops at
the EOCD comment. Its optional signal owner polls at the source loop boundary,
sets `ERROR_BREAK`, and returns the source's nonzero remaining-span count so
the command retains partial output. Unknown records, short reads/writes and
arithmetic overflow are still surfaced as component failures; diagnostics,
close/delete and final guest IoErr ordering remain frontend/reference work.

`Exe2ArcScannerSelection.Scan` now owns the source scanner order and terminal
failure precedence. `Exe2ArcResultPolicy` maps scanner/extractor outcomes to
return and deletion actions without issuing DOS calls. The MorphOS frontend
composes these owners with real DOS `ReadArgs`, `Open`, `ExamineFH`, `AllocMem`,
`Close` and `DeleteFile` leases. It uses a bounded split path workspace rather
than reproducing the source's overlapping filename scratch hazard. The
frontend now consumes the source-compatible ZIP break boundary; exact
diagnostic and saved-byte transcript bytes, final IoErr ordering and original
guest behavior remain unresolved.

`NativeExe2ArcArgumentGate.Read` now supplies the exact three-slot template to
the existing Kickstart DOS `ReadArgs` owner and returns its parser lease and
error level without replacing the parser or releasing resources early. This is
the parser ownership boundary used by the composed frontend; the command
still needs the reference transcript and final-IoErr evidence for shipping
admission.

The primary NDK member used for these API requirements is
`NDK_3.1/Docs/doc/dos.doc` in the private developer CD, 174666 bytes at logical
block 17730 (2048 bytes/block), SHA256
`2e22d1d1b7c00fac5757eead0ef4d49a01c7666f0db5850735193c9597a7cee2`.
The CD SHA256 is
`5d6bfcb213f1395d4c95584dc94d0e36265be355076dae1710bd36fb4dfdbff3`.
The member's Read/Write/Seek/IoErr entries begin at byte offsets 120448,
173078, 137438 and 86858 respectively. Read and Write return actual signed
counts; -1 indicates error and Read zero indicates EOF. Seek returns the
**old position**, not a Boolean; zero is a successful old position. Its position
argument is a signed LONG. The NDK also documents a V36/V37 filesystem Seek
error-return bug; no speculative workaround or success-IoErr probe is added to
these Kickstart 3.1 component tests. The source copy facts are `exe2arc.c:222-238`,
ACE search `359-390`, RAR/CAB searches `405-477`, EOF copy `393-400` and CAB
length copy `479-485`.

### Host checkpoint

The first component checkpoint passes 136 Exe2Arc cases and 429 inclusive
tests: 64 headers, 48 scanner and 24 copy. Its receipt SHA256 is
`fcffaf7d3984b43f9d6fd706deec42e98af00100898a26a99aede2f2fa985577`.
Peer review then added four explicitly invalid-provider controls: Read/Write
returning more than requested or returning -2. These must stop without an
invented IoErr or impossible completed-byte count; they are not assertions of
ordinary DOS behavior. Production sources were unchanged.

The final [host receipt](D:/Koodit/GIT/CopperOS/obj/exe2arc-io/20260830T161742Z-52052057/receipt.json)
SHA256 is `a6d2a707555eba0b4a3b8250d3ffd932eaba56ddb1befb3f4b1b94d5f91af411`:
**140 focused and 433 inclusive passes**, zero skips, warnings or errors.
Only package-based Commands and Commands.Tests projects were built, with
project-reference building disabled. Managed tests do not execute the native
adapter, which remains excluded from the ordinary Commands project.

### Historical RAR4/CAB generated-component checkpoint

The isolated source-linked [probe](D:/Koodit/GIT/CopperOS/tests/Commands.Exe2ArcIoNativeRoot/Exe2ArcIoNativeProbe.cs)
calls the exact production components through NativeExe2ArcIo. The isolated
[executor](D:/Koodit/GIT/CopperOS/tests/Commands.Exe2ArcIoNativeExecution/README.md)
uses pinned Copper68k 1.4.0 and source-links the existing HunkImage reader.
The final [native receipt](D:/TestData/CopperOSCommands/Exe2ArcIoNative/r6130b2ac/summary.json)
SHA256 is `30022b75d868ae08443b2a52acd32c0064b72e5d2a76edc761c8f3ca65967b00`.

The historical checkpoint records **231 actual native fixture invocations**, 77 per CPU, including 24 caller
invocations in twelve instruction-interleaved pairs. These execute 784787234
guest instructions and 729 supplied public-vector calls. There are **zero
original-command, full-command or packed-original comparisons**. Each CPU uses
one immutable image with separate caller contexts and scratch. Exact vector
scripts check raw BPTRs, buffer pointers, lengths, seek modes and old-position
results. Successful calls poison ambient IoErr; -1 capture must occur exactly
once before another DOS call. Volatile D0/D1/A0/A1 and CCR are clobbered. Output
bytes, source bytes, code/vector/control guards and SP restoration pass. Only
DOS Read supplies scratch writes; the generated scanner performs byte reads.
Configured stacks are 4 KiB/16 KiB; maximum observed component stack usage is
444 bytes, excluding a real DOS handler. This is not a minimum stack approval.
A counted invocation is one probe entry: successful scan/copy pipeline cases
call both primitives. The recorded results cover 159 scanner calls and 84 copy
calls across the three CPUs; these are not additional entry invocations.

| CPU | HUNK bytes | SHA256 |
| --- | ---: | --- |
| 68000 | 5156 | `3b76431602d84d42860b7d1a0d1d23167a2170fdf57c22e581d54ae7e32a535a` |
| 68020 | 5268 | `5ea7b2629aa40451d6670be9ff6912b5302978ace0c0c767f22c499f8f042290` |
| 68040 | 5184 | `654f7f51b772f78996b0ac5e654088e840396c6daf0b226f347908c23bca0bdd` |

All three HUNKs reproduce byte for byte. Compatibility reports show no managed
allocation, fatal machine fault site, runtime helper/feature or external native
target, and no initialized RAM/BSS in the artifact. This does not qualify every
public-header case under the new API: the native slice reaches the internal
candidate checks through the scanner, while the 64 current public helper cases
are host tests. The preceding 228-call native header checkpoint remains tied
to its earlier strict predicate.

The private run copies 15 current source/settings files **before** building the
two isolated projects, and binds 601 inputs plus 196 host inputs through 18
ordinary phase checks. Manifest SHA256 is
`111dcfcafe6df9707b1efcfdd66c2561ccbdbc86753ed70578e8c193352cb674`.
It reuses the complete byte-verified compiler folder and retained owner
source/restore observations from the bridge/header checkpoint; there is no
claim of a new compiler/SDK build or a complete historical NuGet restore proof.
The actual runtime DLL is pinned to
`8046d9a2083c198647a02d9146b735629890d9a2e14bc313968a3b48139a2cd5`.
All native buffers lie below the 68000's physical 24-bit address limit. Logical
uint-wrap rejection is not high-address hardware mapping proof, and no cycle
timing qualification is claimed.

Thirteen [negative controls](D:/TestData/CopperOSCommands/Exe2ArcIoNative/r6130b2ac/negative-controls-v2/receipt.json)
pass, SHA256 `589417ac051b706acd40416383e7a93a6bd8291c7703b9144ba3f465a76d2a88`:
missing/truncated HUNK, wrong CPU/suite, existing report and hardlink protection,
wrong returned behavior, code/scratch/vector writes, foreign reads, forbidden
Open and wrong raw BPTR. Seven deliberately invalid native body attempts are
excluded from the 231 ordinary calls. A first negative run is retained as
failed: its four-byte return-zero body hit prefetch bounds before the intended
missing-call assertion. Padding that inert body and rerunning all controls
corrected the test setup without changing the executor.

An exploratory fixture also exposed an **unrepaired compiler case**:
`DOS.DOSLibraryBase = new APTR(raw)` stored the temporary's address rather than
its value. The failing 5160-byte HUNK, source/DLL snapshots, ASM and execution
receipt remain under `D:/TestData/CopperOSCommands/Exe2ArcIoNative/trial-74735608`.
The fixture now uses the established public APTR.FromPointer boundary; this
does not repair or qualify the constructor/property-setter form. Production
scanner/adapter sources were unchanged. A separate fixture correction admitted
the six-byte vector stub's four following prefetch bytes, fetch-only; executable
PC admission stayed limited to the vector start. The exploratory successful
68000 run is separate from the source-bound 231-call checkpoint above. The
2026-09-23 ACE extension supersedes this as the current component receipt; the
old bytes and hashes remain here as historical RAR4/CAB evidence.

The 2026-09-23 ACE extension is recorded separately in
[artifacts/exe2arc-ace-native-20260923/qualification.json](D:/Koodit/GIT/CopperOS/artifacts/exe2arc-ace-native-20260923/qualification.json).
It adds the source's `**ACE**` marker at candidate offset +7, strict
`size > 14` window admission with thirteen-byte overlap, offset-zero stop,
relative candidate seek, final absolute seek, EOF payload length and the
shared exact-chunk copy path. The three resident HUNKs pass 84 independent
supplied-DOS invocations each (252 total), including seven ACE cases and an
ACE scan/copy pipeline. HUNK bytes/hashes are 68000: 6380 /
`f67d7cbf67768028cdbbdf0ad0c7c3d2e0ef33632601e88071f7132c17276fbe`,
68020: 6428 /
`9e2331d580c9c51f14fd498155efad568f9579f5bbbb0ab76729f8db394154e0`,
and 68040: 6364 /
`25c1d23eecfc92d37ed63d4b3ca798a72e5b959e0de4c283381936cdd4b9b3ad`.
This remains a component checkpoint: no ReadArgs/input/FIB owner, output
naming, full command frontend, original guest run, packed correspondence,
archive decoder, PURE, or package admission is claimed.

### Resident frontend static qualification

The source-linked resident root is
`tests/Commands.Exe2ArcNativeRoot/CopperOS.Commands.Exe2ArcNativeRoot.csproj`,
with entry `CopperOS.Commands.Exe2ArcNativeRoot.Exe2ArcEntry::Main`. The
reproducible gate is
`tools/Commands/qualify_exe2arc_native_static.ps1`; its receipt is
`artifacts/exe2arc-native-static-20260922-v8/qualification.json`.

| CPU | HUNK bytes | SHA256 |
| --- | ---: | --- |
| 68000 | 27164 | `20ba9c2cb42703f497164366b742117fa1a2dc4d92e32b85b0b125779cae62df` |
| 68020 | 27656 | `07ddfe29b7a82d881861eef57732f760dd28f33ecfe24b3db1e56cf95fedad19` |
| 68040 | 27132 | `f29fc59953b16b3f88d131a491244a9396560d415ac4ccf7bbdc8003f5820bf5` |

All reports are statically compatible and contain 100 reachable methods with
zero managed allocations, runtime features/helpers, external native targets,
exception regions and fatal machine-fault sites. The managed Commands suite
passes 704 tests. This does not establish original guest behavior, filesystem
handler semantics, redirected diagnostics, final `IoErr`, PURE/resident
admission, packaging or shipping.

Current production source hashes at this checkpoint are:

| File | SHA256 |
| --- | --- |
| Exe2ArcHeaderProbe.cs, including internal zero-offset inspection | `30059bee8b1544d366d6de1d73bef87b7267242a3d95f4f291eec5e0339e6eb9` |
| Exe2ArcIo.cs | `6926498444758be5473cac5b5bdd74c885a8670a1b4ded00049c618cbe5aa572` |
| Exe2ArcForwardScanner.cs | `f3f0a0f95de0948dd3357475a12278d68358c1790fe1279fc695754584950c25` |
| Exe2ArcPayloadCopy.cs | `477311596a9307f411af1ec959f2385be0265a80f34000c4d0578d2b938fb391` |
| NativeExe2ArcIo.cs | `23fde25c91fc471ccbb464f44ae09a156b81df1d03e020cc16a201019491c4fd` |
| Exe2ArcOutputName.cs | `080b96f86470a5c782472eeca49be128be19c004abb797471b9f299ce0457691` |
| Exe2ArcOutputSelection.cs | `775528b0c98b1e4723c394da00fb986837e463044c46dcc2293b1622ac7e4cf5` |
| NativeExe2ArcOutputIo.cs | `fe7e496615f1f04cdf27cc581e303c5bc69e1710a4e556c339e4fcbecdff5412` |
| Exe2ArcTypeSelection.cs | `c4b2c993ff556012902dab4978b3ab44ddd3ff1d9d9a3dd64014b090e9003693` |
| Exe2ArcScannerSelection.cs | `7d7de64fd9cbff2058914128e5db9b4651e0257d255a6dac77981a10ed45c839` |
| Exe2ArcResultPolicy.cs | `7c44cdcf3e28c1389fb07f3e89be060b9b854ae2f79e04f84e28443601d1090b` |
| NativeMorphOSExe2ArcCommand.cs | `3971a348d6366e5066daf0d1ec336c37035fafe91386e0629ccb47faf326932c` |
| NativeExe2ArcArgumentGate.cs | `cc0292d09ed659da49e55332d08ff3214a03e255d579cd9321c0094c36f921f6` |
| Exe2ArcLhaScanner.cs | `af31f2bdfbe9ca21bf747af3857c07ff56548fb89b12da245d89a9fd6e8794a9` |
| Exe2ArcZipScanner.cs | `01c036be6c4c99b6257f6e3848d152384a1e6dbe8cafa40d3c17931d8cfb1cac` |
| Exe2ArcZipRecordRewriter.cs | `7c55f83fe79d06577de2cc5da3932f2a48edaa325b3c6a976ce852e109734c99` |
| Exe2ArcZipRecordExtractor.cs | `088d1122a3ac2544ecd14d0bc6c85e88aee9df9ebf08af1a553222d78a48efa7` |

The scanner-selection, result-policy and MorphOS frontend owners now compose
source dispatch order, terminal failure precedence, resource ownership,
close-then-delete decisions and the ZIP-only Ctrl-C success/`ERROR_BREAK`
boundary. Next required slices are exact command diagnostics/saved-byte
transcripts, original MorphOS observations and the full guest lifecycle. Keep
EXA-U01 through U08 and installed purity/packaging closure open. Supplied vectors
do not establish a real parser, handler, CLI launch, full command, original
behavior or shipping/P-bit admission.
