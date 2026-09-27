# Rename contract

Command/profile IDs: `CC12.Rename.wb31`, `CC12.Rename.morphos320`.
Goal steps: CC01, CC12. Recorded: 2026-08-30.

Status: **Classic 37.2 arguments, public calls and control flow are verified
statically against the pinned executable and 3.1 NDK. RN1-WB now adds original
machine-code observations with supplied DOS vectors: 40 returned cases and
eight separately stopped hazard experiments on each of 68000, 68020 and 68040
after the separately qualified CPU correction. The historical 68020 failure
remains retained. A bounded original-DOS direct/directory run now passes (see the dated section below). An independent Workbench replacement now compiles for
all three CPUs; 309 cumulative native invocations and bounded 68000 original-DOS effects/concurrency fixtures pass. See [remaining-work audit](../rename-remaining-work.md) for coverage limits and next steps.
There is no complete contract
or shipping/native purity qualification. MorphOS 50.8 has
verified media identity/P evidence and now version-matched official source observations; see [MorphOS source contract](../rename-morphos50-source-contract.md). Runtime and implementation remain open.**

The bounded parser-edge harness now executes in the sibling test project:
`KickstartRomRenameCommandLaunchTests.cs` binds the private 37.2 image and the
source-generated 68000 HUNK/map, then sends both through original DOS
`RunCommand` with `FROM/A/M,TO=AS/A,QUIET/S` and NIL streams. Four invocations
(original/generated image × empty-line/unterminated-quote input) and two
comparisons pass. The inputs return 20/116 and 20/120, with matching
`ReadArgs` failure, `PrintFault`/`IoErr`, no `FreeArgs`, and the observed single
`UnLock(NULL)` common-cleanup vector. Receipt:
`artifacts/rename-parser-edges-20260917/qualification.json`. Full
positional/alias/quote, help/continuation and pattern-forwarding coverage
remains open.

This document records behavior and evidence, not vendor implementation code.
Original executable bytes, disassembly listings and NDK source are not copied
into this repository. Source-visible hazards below must be resolved through
bounded observation and an explicit safe implementation decision; they are
not instructions to reproduce memory corruption or leak resources.

## Identity, provenance and confidence

| Profile | Pinned member | Verified facts |
| --- | --- | --- |
| `wb31` | `Workbench3.1:C/Rename`, header block 367 on the selected Workbench disk | 1140 bytes; SHA256 `ebff9d5f1401ca67fe9542bde304c603d21366f81852716c236c5c7ca1e49579`; tag `rename 37.2 (30.5.91)` at file offset 1013 |
| `morphos320` | `MorphOS/C/Rename`, ISO block 178100, block size 2048 | 3143 bytes; SHA256 `3f1dfec8f16649b022be210363636c0fdd4cee243ed93ae477730affdc78bc69`; tag `Rename 50.8 (27.11.04)` at file offset 3107; packed `7f4d4f53` payload |

The classic archive is
`D:/TestData/TestImages/Workbench v3.1 rev 40.42 (1994)(Commodore)(M10)(Disk 2 of 6)(Workbench)[!].zip`.
Its SHA256 is `d93611887acf91f68f5608a5b7812f03eb16f743f40451b67c93067b04c967ed`;
the contained ADF SHA256 is
`a8f167bad2897e8c7f2cfe73de7a9f8dad10c7a5b1f78ca17f818ab17278b985`.
Both whole hashes, the ZIP CRC, filesystem traversal and Rename member hash
were checked read-only for this contract. No Rename counterpart is recorded
on the selected Install disk; this does not close the four missing disk roles.

The MorphOS member and `hdinstall.fixc` were re-read at their recorded extents
from `D:/TestData/MorphOSReferences/morphos-3.20.iso`. Whole-media identity and
coverage remain those in the [inventory](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/command-inventory.json)
and [media evidence](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/media-evidence.json).
The packed MorphOS body was neither decoded nor executed. Its template,
options, error policy and correspondence to classic 37.2 are **unestablished**.

Classic raw disk protection is zero, including P clear. Installed selection
and flags remain open, so its classification is `unresolved-not-nonpure`.
MorphOS `hdinstall.fixc:63` explicitly adds P to `MorphOS/C/Rename`. That
2478-byte script is at ISO block 6348, SHA256
`46751fd064329077fe3f1ebeff6ea47c13159dd84841fac83d2043fb270ca8df`.
MorphOS is therefore `required-pure`, without a claim that final installed
flags or a replacement artifact have been qualified. Do not infer Amiga P
from ISO POSIX mode, binary size, or an apparently read-only code hunk.

The classic file has one 1104-byte HUNK_CODE, beginning at file offset 32,
and no separate data, BSS or relocation hunk. The instruction region ends
with return at code offset `$3AE`; literals follow. All `$` addresses below
are **code-relative**; add 32 for file offsets. Evidence labels are `B` for
the original binary/control flow, `A` for the primary API contract, and `R-V`
for original machine code executed with explicitly supplied vectors. `R-V`
does not establish original DOS, parser, matcher, handler or formatter behavior.
The evidence below retains that distinction.

## NDK authority and public boundary

References are members of the private
[AmigaDeveloperCD.iso](D:/TestData/AmigaDeveloperCD.iso), under `NDK_3.1/`.
The containing CD identity is recorded in goal Appendix C; the following
member hashes were checked directly. ISO blocks are 2048 bytes.

| Member | Block; bytes | SHA256 |
| --- | --- | --- |
| `Docs/doc/dos.doc` | 17730; 174666 | `2e22d1d1b7c00fac5757eead0ef4d49a01c7666f0db5850735193c9597a7cee2` |
| `Docs/doc/exec.doc` | 17816; 162487 | `71df8eeab9f6b9873a38bea96bec34fd6e44b78aa2d67d8f1b28319c207dd2ad` |
| `Includes&Libs/fd/dos_lib.fd` | 20705; 5765 | `a9b24c1d9fb1053955dfb28eb8dabe92eff5dbde10b952e67d6cee2a80c5c228` |
| `Includes&Libs/fd/exec_lib.fd` | 20709; 5721 | `4da0ae2a91d0758696e0d1f72f8bc748321b5fd14e7e31d8215cda8e002348c2` |
| `Includes&Libs/include_h/dos/dos.h` | 21043; 10860 | `7791a911cab18de7aa5b5e4a818d5e09439c8c16aa7b029ea18a12ada000b8c3` |
| `Includes&Libs/include_h/dos/dosasl.h` | 21049; 4896 | `42376c87586929810607b4b5e0449398d30223bbcaa04d4e67f857a8b0609a26` |
| `Includes&Libs/include_h/exec/memory.h` | 21111; 3405 | `1f811ab2b567d47130cfa071435275645cb43e51bf59b800f8b235327f87513f` |
| `Includes&Libs/startups37/startup.asm` | 22291; 26931 | `a8a75cb9335456699ba1db9858bdb3f6f0dc53eeb18ab90bb8aa796c16b5e0c6` |

Relevant DOS.DOC entry offsets: CheckSignal 16592, Examine 48255, FreeArgs
69792, Lock 89765, MatchEnd 98276, MatchFirst 98808, MatchNext 102729,
NameFromLock 107189, ParsePattern 114095, PrintFault 119073, ReadArgs 121650,
Rename 132494, SameLock 136393, SetIoErr 146325, UnLock 165294 and VPrintf
170318. EXEC.DOC AllocVec is at 36779 and FreeVec at 78424.

The binary calls these public vectors; no command-private filesystem or
argument parser is justified by this reference:

| Library | Verified called vectors, decimal offsets |
| --- | --- |
| Exec | OpenLibrary -552, CloseLibrary -414, AllocVec -684, FreeVec -690 |
| DOS | Rename -78, Lock -84, UnLock -90, Examine -102, IoErr -132, NameFromLock -402, SameLock -420, SetIoErr -462, PrintFault -474, CheckSignal -792, ReadArgs -798, MatchFirst -822, MatchNext -828, MatchEnd -834, ParsePattern -840, FreeArgs -858, VPrintf -954 |

FD register arguments remain authoritative: Rename takes old/new C strings
in D1/D2; Lock takes name/mode in D1/D2 and returns a **BPTR**; Examine takes
BPTR/FIB in D1/D2; SameLock takes two BPTRs. ReadArgs uses D1/D2/D3; MatchFirst
uses pattern/AnchorPath in D1/D2; MatchNext and MatchEnd take AnchorPath in D1;
NameFromLock and ParsePattern use D1/D2/D3 with byte capacity. Exec AllocVec
uses D0/D1 and FreeVec uses A1. All are available in the selected 3.1 API.

## Classic grammar and entry

The exact template at file offset 988 / code `$3BC` is passed to ReadArgs
at `$E4`, with three cleared LONG result cells and a null optional RDArgs:

```text
FROM/A/M,TO=AS/A,QUIET/S
```

| Cell | Contract from actual use and ReadArgs |
| --- | --- |
| 0: FROM | Required multiple string values; pointer to a null-terminated string-pointer vector |
| 1: TO / AS | Required destination string, with the two names as aliases; not `/K` |
| 2: QUIET | Switch; any nonzero result suppresses directory-mode progress output only |

ReadArgs handles positional values, keywords, quotes, star escapes, case and
interactive `?`. Its `/M` plus later `/A` rule can take a trailing value from
FROM to fill TO. The command does not implement that selection itself. Empty
input, only one positional value, alias precedence and continuation need
real-parser fixtures; supplying result pointers is not grammar evidence.
A successful `/A/M` parse is assumed to provide a usable nonempty FROM vector
and destination. No defensive null-list result policy is visible in the body.

There are no ALL, FORCE, OVERWRITE, recursive, regular-expression-substitution,
or other switches in this classic template. Wildcards are DOS patterns in
FROM, processed by MatchFirst/MatchNext. The binary does not parse a pattern
in TO or implement capture/replacement syntax. The meaning of an unquoted
`*` also depends on DOS pattern configuration; do not substitute host globbing.

Startup opens DOS V36 at `$34-$3C`. Failure writes 122 to the current Process
secondary result and returns 20 (`$394-$3AE`), without DOS calls. After opening,
CheckSignal($1000), the Ctrl-C bit, runs before allocation or parsing. A set
bit produces PrintFault(304, NULL) and return 20. NDK CheckSignal clears all
signals included in its mask. No explicit CLI/Workbench distinction, message
receive/reply protocol or borrowed-stream close appears in this hunk.
Workbench support and a later entry's launch policy require separate evidence.

## Classic destination dispatch and operation order

After successful parsing the saved primary return is cleared to **0**. The
binary prepares an AnchorPath, setting break mask `$1000`, flags `APF_DOWILD`
(1), and `ap_Strlen=255`. It first calls MatchFirst on the first FROM value
before inspecting the destination (`$FC-$124`). This is a preflight search,
including for a literal single-source rename.

If that MatchFirst fails, the command captures **IoErr**, not the nonzero
MatchFirst return code. For captured error 205 it prints the failure prefix
with the original first FROM and TO; other errors have no such prefix at this
point. The common saved-error path then decides whether to print a fault and
select return 20. The difference between MatchFirst's returned error and
IoErr matters: the NDK specifies the former and does not promise their equality.

After successful preflight (`$150-$1B6`):

1. Lock TO with ACCESS_READ (-2). Keep a successful destination lock until
   cleanup. Examine it into a longword-aligned stack FileInfoBlock. Only a
   successful Examine with `fib_DirEntryType > 0` selects directory mode.
   Lock or Examine failure does not directly select their error for reporting.
2. If TO was not recognized as a directory, but the preflight AnchorPath has
   APF_ITSWILD (bit 1) or FROM contains a second string, print the destination
   diagnostic and branch directly to cleanup. **The saved return remains 0**;
   there is no PrintFault or assigned ERROR_OBJECT_WRONG_TYPE on this branch.
   Wildcard syntax still requires a directory even when it matches one object.
3. For a directory destination and only one FROM string, also Lock the raw
   source string. If that succeeds, compare it with the destination lock using
   SameLock. LOCK_SAME (0) switches to direct mode; any nonzero comparison keeps
   directory mode. Release this temporary source lock once. Failure to acquire
   it leaves directory mode selected (`$1BA-$1E6`).
4. End the preflight search with MatchEnd on the normal dispatch path (`$1EA`).

**Direct mode** (`$1F2-$238`) calls
ParsePattern(first FROM, source buffer, 256), then Rename(source buffer, TO).
It uses the parsed literal buffer, not the first match's reported path, and
does not check ParsePattern's return. No progress message is printed, even
without QUIET. Rename failure captures IoErr before printing the failure
prefix, calls SetIoErr with that captured value, and enters common handling.
Success enters common handling without selecting another error.

**Directory mode** (`$23C-$32E`) calls NameFromLock(destination lock, destination
buffer, 256), without checking its return. It finds the resulting terminator
and uses that position as an append point, inserting `/` unless the preceding
byte is `:`. Each FROM pattern then receives a new MatchFirst, so the first
FROM is matched again after its preflight. For each matched item:

1. Append its `ap_Info.fib_FileName` to the destination prefix and copy its full
   `ap_Buf` into the separate source buffer.
2. Call **MatchNext before output or Rename** and retain that return code. The
   copied current names remain independent of the overwritten match record.
3. Unless QUIET is set, print the progress message using those copied paths.
   Then call Rename with the two buffers.
4. On Rename failure, capture IoErr, print the failure prefix regardless of
   QUIET, restore that captured value with SetIoErr and stop the entire command.
5. After success, MatchNext return 0 continues with its next match. **Any
   nonzero return ends this pattern**, followed by MatchEnd and the next FROM
   string. The binary does not distinguish ERROR_NO_MORE_ENTRIES (232) from
   break, allocation or other MatchNext errors on this path.

A later pattern's failing MatchFirst captures IoErr and stops, without the
special initial-205 prefix. Successful earlier renames are not rolled back.
No CurrentDir, file copy, DeleteFile, replacement of an existing target, or
cross-volume fallback is requested. NDK Rename can move within a volume,
fails when its target already exists, and cannot move between volumes. Exact
case-only/self-rename, links, duplicate names, assigns and handler errors need
original-runtime observation. The binary does not explicitly set APF_DODIR
to request directory descent; use the actual DOS matcher rather than inventing
recursive behavior from the presence of a directory match.

## Output, saved errors and final secondary result

These are the three exact command-owned formats. `\n` denotes byte 0A and
`\x20` denotes one space; the first format has a trailing space and no LF:

```text
Can't rename %s as %s because\x20
Destination "%s" is not a directory.\n
Renaming %s as %s\n
```

Their code offsets are `$3F4`, `$414` and `$43A`. VPrintf uses a LONG pointer
argument vector and the current buffered Output. Neither VPrintf's return
nor PrintFault's return is tested. QUIET suppresses only the progress format,
not destination diagnostics, failure prefixes, faults or early break messages.
Exact DOS fault text/localization and complete stdout must be captured from
the actual formatter, not supplied by a host-language imitation.

Common handling at `$332-$350` examines saved error E. If nonzero, it calls
MatchEnd, PrintFault(E, NULL), and selects return 20. If zero, it skips those
calls and retains the previously selected return. No return 5 or 10 is selected.

| Branch / supplied result | Statically selected primary result and explicit error action |
| --- | --- |
| DOS open fails | 20; direct Process result 122 |
| Initial Ctrl-C check reports a bit | 20; PrintFault(304, NULL) |
| Any of four startup AllocVec calls fails | 20; PrintFault(103, NULL) |
| ReadArgs fails with IoErr E | 20; common PrintFault(E, NULL) only if E is nonzero |
| First/later MatchFirst fails, captured IoErr E is nonzero | 20; PrintFault(E, NULL); initial E=205 also prints the prefix |
| MatchFirst fails but supplied IoErr is zero | 0 after successful parsing; no common fault; synthetic provider case, not a normal-DOS assertion |
| Destination is not a directory but multiple/wild FROM requires one | **0**; destination diagnostic only |
| All attempted Rename calls succeed | 0; no selected final error or forced clear |
| Rename fails with captured IoErr E != 0 | 20; prefix, SetIoErr(E), MatchEnd, PrintFault(E, NULL); later sources not attempted |
| Rename fails with supplied IoErr zero | 0; prefix and SetIoErr(0), no common fault; synthetic provider case |
| MatchNext returns a nonzero error after finding a current item, whose Rename succeeds | That item is still renamed; continue with next FROM after MatchEnd; the MatchNext error does not itself select failure |

**Do not turn this table into unconditional final-IoErr assertions.**
PrintFault sets IoErr to its supplied code by API contract, but Rename then
performs FreeArgs, destination UnLock, FreeVec calls and CloseLibrary. It does
not capture and restore IoErr around this cleanup or explicitly clear it on
success. Thus the final Process secondary result, including after a printed
fault, requires an original DOS/runtime capture. Controlled vector fixtures
must distinguish selected diagnostic code, each API's side effects and final
IoErr. A future generic entry must not silently impose MakeDir's error-restoring
policy on this command.

## Resources, sizes and unresolved source-visible hazards

The original makes four checked AllocVec acquisitions in this order before
ReadArgs: 80 bytes with flags 0; 538 bytes with MEMF_CLEAR (`$10000`) for the
AnchorPath and trailing buffer; 256 bytes with flags 0 for the source path;
and 256 bytes with MEMF_CLEAR for the destination path (`$68-$C2`). The 80-byte
block is stored and later freed but has no other use in the decoded body;
its purpose is unknown. Record its original allocation/failure point without
inventing a structure or requiring a replacement to duplicate dead storage.

The anchor fields used are break bits at +8, flags at +16, path capacity at
+18, FileInfoBlock at +20, its filename at +28, and full-path buffer at +280.
NDK requires longword alignment of AnchorPath and FileInfoBlock. Borrowed
ReadArgs strings stay valid through all matching, DOS calls and diagnostics.
The original rewrites its own stack result cell for some formatting calls;
that is not permission to alter the parser's string/vector allocation.

Cleanup order (`$352-$38E`) is FreeArgs for a nonnull successful RDArgs,
UnLock(destination BPTR, possibly zero), FreeVec(anchor), FreeVec(80-byte
block), FreeVec(destination buffer), FreeVec(source buffer), CloseLibrary.
Partial acquisition pointers start null. NDK permits null FreeVec and harmless
UnLock(0); nonnull acquired pointers/locks remain owned and must not be freed
twice. MatchFirst/MatchNext separately own search storage released by MatchEnd.
No borrowed stream is closed. The 348-byte local frame is a static layout
fact, not a measured or qualified minimum stack.

The following are **source-audit findings, not executed original defects**:

- The destination-not-directory branch bypasses MatchEnd after a successful
  preflight and then frees the anchor. Depending on the matcher's allocations,
  search-owned storage can remain unreleased. A safe replacement must resolve
  this ownership issue explicitly instead of disguising a cleanup difference.
- Common error handling can call MatchEnd after the normal dispatch path
  already ended that anchor, including a failing direct Rename. The NDK text
  alone does not establish repeated-MatchEnd semantics. Record actual call
  counts and whether the first call clears the original provider's ownership.
- ParsePattern(-1) and NameFromLock(FALSE) are not checked. ParsePattern's
  output/error semantics cannot be replaced with an assumed successful copy.
  NameFromLock failure can leave an empty cleared destination, after which the
  original reads the preceding byte while testing for a colon.
- Destination-prefix scanning and filename appending have no explicit total
  capacity check. A 256-byte NameFromLock result followed by `/` and a matched
  basename can exceed the allocated destination buffer. Bound future reference
  experiments with read/write guards; do not execute uncontrolled host or guest
  memory accesses to discover consequences.
- MatchNext errors are not selected as failures, and MatchFirst errors are
  interpreted via IoErr instead of their specified returned code. Runtime
  evidence must determine which combinations real V40 DOS supplies; supplied
  vectors may establish only the command's conditional behavior.

## Finite native/reference matrix

The table is the complete pending/partial contract matrix; the RN1-WB section
below identifies the executed supplied-vector suffixes. No broad row is closed
by executing a subset. Expand listed variations with stable suffixes. Fix the
injection set to 0, 103, 116, 119, 120, 203, 205, 214, 221, 232, 303 and 304;
"other E" below means 214. Use one, two or three FROM values and one or two
matches per pattern, apart from explicit buffer-boundary rows. These are finite
discriminating cases, not exhaustive OS error coverage. Record original and
replacement counts separately; a missing reference, guard stop, or unexpected
result cannot count as a pass. The isolated original-only executor contains
no replacement implementation and leaves all real-provider rows open.

| Fixture ID | Bounded input / injection | Required observation or static expectation |
| --- | --- | --- |
| RN31-A01 | Empty line, one positional value, missing FROM or TO | Original ReadArgs required-argument error, cursor/help and cleanup; no guessed private parser |
| RN31-A02 | Two/three positional values; FROM and TO/AS keywords; duplicate aliases | Exact `/M` to later `/A` assignment, vector order and keyword policy |
| RN31-A03 | Spaces, empty quoted value, star escapes, Latin-1, option-like filenames | Exact parsed bytes and subsequent DOS paths; separate grammar from supplied vectors |
| RN31-A04 | QUIET absent/present/nonzero supplied switch | Suppresses only directory progress, not any diagnostic |
| RN31-A05 | `?`, continuation, EOF and cancellation | Real input/template behavior and ownership; no pass from static template alone |
| RN31-L01 | DOS V36 open succeeds/fails | One owned close on success; missing library 20/direct122; no DOS call after failure |
| RN31-L02 | Ctrl-C present/absent at CheckSignal | Mask $1000 and clearing semantics; break 20/PrintFault304 before allocations/parser |
| RN31-L03 | Original Workbench launch | Establish actual launcher/message behavior; generic startup tests do not close this row |
| RN31-M01 | Fail each of the four AllocVec acquisitions | Sizes/flags/order, 20/PrintFault103, all prior allocations released, null frees allowed |
| RN31-M02 | ReadArgs fails with E, including E=0 as an explicitly synthetic case | Return20; fault only for E!=0; no successful RDArgs lease; exact MatchEnd/cleanup trace |
| RN31-D01 | First MatchFirst fails with captured205, other E, and D0/IoErr mismatch | Correct initial prefix condition, chosen error source and stop-before-destination precedence |
| RN31-D02 | Single literal, missing target; existing regular target; Examine failure | Direct-mode dispatch and exact provider Rename result; destination BPTR ownership |
| RN31-D03 | Wildcard matching one/many objects, or multiple literal FROM values, with nondirectory target | Destination diagnostic and statically selected return0; capture omitted MatchEnd as an ownership observation |
| RN31-D04 | Directory target, one source; source Lock fails or SameLock returns0/1/-1 | Same-object direct mode only for0; exact temporary source UnLock |
| RN31-D05 | Case-only/self-rename and identical lock represented by distinct BPTRs | Actual DOS/handler outcome; comparison is by SameLock, not pointer equality |
| RN31-P01 | Direct nonwild source with pattern quoting/escapes | Actual ParsePattern256 bytes and old-name argument used by Rename |
| RN31-P02 | ParsePattern error with guarded destination, no valid output string | Record original unsafe/unchecked path as a guard stop; decide safe replacement rejection before implementation |
| RN31-N01 | Directory prefix ends in colon versus ordinary component | Exact appended destination bytes and no host path normalization |
| RN31-N02 | NameFromLock failure/empty output and prefix at capacity boundary | Bounded guard observations; no invented successful rename or generic error code |
| RN31-N03 | Combined prefix/slash/basename below, exactly at and beyond capacity | Detect unchecked original access; separately specify safe replacement bounds/outcome |
| RN31-W01 | One pattern/one match, one pattern/several matches, several FROM patterns | Preflight then repeated first MatchFirst; correct order and immutable copied current paths |
| RN31-W02 | MatchNext advances and changes AnchorPath before Rename | Rename uses saved current full path/basename, not the next match's fields |
| RN31-W03 | MatchNext returns232,304,103,other E, current Rename succeeds | Current item still processed; any nonzero result ends pattern; later pattern attempts and final IoErr recorded |
| RN31-W04 | Later MatchFirst fails | Stop; selected IoErr; no special initial205 prefix; retain earlier successful filesystem effects |
| RN31-W05 | Directory matches, trailing slash/empty basename, pattern configuration for `*` | Capture actual matcher/handler behavior and flags; no assumed recursion |
| RN31-E01 | Direct Rename failure E; directory-mode failure at first/later item | Error captured before diagnostic; prefix, SetIoErr, common fault20 when E!=0; stop/no rollback |
| RN31-E02 | Rename(FALSE) with supplied IoErr0 | Static return0/prefix/no common fault; label synthetic provider contract case |
| RN31-E03 | Target exists, cross-volume target, access/device error, race after preflight | Exact actual handler error and surviving side effects; no overwrite/copy fallback |
| RN31-E04 | Successful/failed VPrintf and PrintFault, with independent ambient IoErr changes | Primary branch result unchanged by output result; distinguish chosen error, requests, output bytes and final secondary result |
| RN31-R01 | Successful/direct/error/reused AnchorPath paths | Exact MatchEnd calls, live provider ownership, RDArgs/AllocVec/lock release; do not assume repeated MatchEnd safety |
| RN31-R02 | Nondirectory rejection after allocated preflight search | Record potential search-resource leak separately from returned status; no false all-resources-released pass |
| RN31-R03 | Cleanup APIs change IoErr after success and printed failure | Original final Process result observed; no fabricated post-cleanup restoration |
| RN31-R04 | Same native image repeated and instruction-interleaved callers | Private anchors/buffers/RDArgs/locks/errors, volatile-register clobbers, code/input/stack guards on 68000/020/040 |
| RN31-R05 | Booted original DOS/parser/matcher/filesystem differential | Full arguments, stdout/fault rendering, return/IoErr, effects and resource trace; vector fixtures alone are insufficient |
| RN50-C01 | Exact MorphOS50.8 primary template/code or controlled help/runtime | Establish independent options/semantics; do not inherit classic quirks or newer documented options without release binding |
| RN50-C02 | Installed selection/flags and shared-image execution | Preserve observed required P design, qualify the implementation and installed counterpart |

Receipts must identify the command hash, CPU, actual DOS/provider/handler,
input and parsed bytes, each returned API status and IoErr, ordered calls,
exact raw BPTRs, allocation and search ownership, output requests/bytes, final
primary/secondary results and filesystem effects. A vector-generated diagnostic
is not original DOS output; supplied matches are not a real wildcard search.

## RN1-WB original-only supplied-vector observations

The isolated [executor project](D:/Koodit/GIT/CopperOS/tests/Commands.RenameNativeExecution/CopperOS.Commands.RenameNativeExecution.csproj)
executes the exact private 1140-byte reference at
`D:/TestData/CopperOSCommands/Workbench31/Rename-37.2.hunk`. Its hash is the
pinned member hash above. No original bytes, NDK implementation, replacement
Rename body or generated command were added to the repository or receipts.
The project imports the existing pinned package props and links the existing
HUNK reader read-only; it does not build a compiler, SDK or sibling provider.

Build and invocation are explicit:

```powershell
dotnet build tests/Commands.RenameNativeExecution/CopperOS.Commands.RenameNativeExecution.csproj -c Release -p:CopperOSUseLocalCopperSharp=false -p:BuildProjectReferences=false --nologo
dotnet tests/Commands.RenameNativeExecution/bin/Release/net10.0/CopperOS.Commands.RenameNativeExecution.dll D:/TestData/CopperOSCommands/Workbench31/Rename-37.2.hunk 68000 <private-report.json> rename-classic-original-vector-fixture
```

The historical coordinated build passed with zero warnings/errors. Substitute `68020`
or `68040` to select that CPU; no automatic substitute, timing bypass or JIT
fallback is used. The factory's public default interpreter and pinned
`Copper68k 1.4.0` are recorded in each receipt. The table describes direct
execution observations, **not a source-bound reproducible qualification run**:

| CPU | Final receipt status | Invocations started | Returned cases accepted | Expected guard stops | Meaning |
| --- | --- | ---: | ---: | ---: | --- |
| 68000 | passed for the declared bounded suite | 48 | 40 | 8 | Complete supplied-vector subset; no full-DOS claim |
| 68020 | failed / incomplete | 17 | 16 before failure | 0 | CPU package limitation at the seventeenth case; no matrix pass |
| 68040 | passed for the declared bounded suite | 48 | 40 | 8 | Complete supplied-vector subset; no cycle-timing claim |

The failed 68020 case is `RN31-N01.component-prefix`: the package reports
unsupported exact MC68020 timing for original opcode `$12FC` at PC `$0010026A`
(code `$26A`, adding `/`), profile `Ocs68020_14MHz`. The failed receipt remains
failed and is retained with the earlier trial receipt. Public 1.4.0 options
expose interpreter/JIT selection only; JIT is 68040-only. No public semantic
timing mode was found, and no CPU, package or original-byte workaround was
applied. This is a separately owned CPU capability gate, not evidence of a
Rename semantic discrepancy. Those unexecuted rows remained open at this
historical checkpoint; the separate CPU-only replay below completes this
declared supplied-vector subset without altering the failed records.

The forty returned cases are 32 ordinary cases, four sequential reuse cases,
and four invocations in two actually instruction-interleaved pairs. Every CPU
loads one shared protected code image. All returned cases assert the selected
D0, final supplied Process IoErr, entry SP, exact ordered public calls, raw
BPTRs, allocation/lock/search ownership, argument/image/input/stack guards and
per-caller isolation. `STARTUP.ASM:264-267` allows image-entry registers other
than SP to change; nonvolatile-register restoration is recorded without
imposing a library-subroutine convention on the whole command. Public-library
argument checks and D1/A0/A1/CCR clobbers remain active. 4 KiB/16 KiB fixture
stacks and 396 bytes touched by the original in returned cases are observations,
not a minimum-stack requirement or purity approval.

The precise finite suffixes and independently supplied expected values are
in [RenameCases.cs](D:/Koodit/GIT/CopperOS/tests/Commands.RenameNativeExecution/RenameCases.cs)
and the receipts. Covered branches include all four allocation failures,
missing DOS, initial Ctrl-C, initial205 diagnostics versus other chosen errors,
direct and directory dispatch, distinct-BPTR SameLock0/1/-1, colon/component
prefixes, QUIET, multiple patterns/matches, MatchNext advancing/erroring before
Rename, first/later rename errors, failed output calls, exact destination
capacity and cleanup changing ambient IoErr. Explicitly synthetic inconsistent
provider combinations, including a failed parser or Rename with IoErr0, are
labelled as such. They do not assert that original DOS returns those combinations.

The fixture supplies three cleared ReadArgs result cells and verifies the
exact template and null D3 before returning read-only FROM/TO/QUIET data.
It never parses the unconsumed `not-parsed` entry text. Supplied MatchNext
overwrites AnchorPath names even when returning end/error, verifying that
Rename uses the copied current path. Direct-mode failure diagnostics use raw
FROM while Rename receives the distinct supplied ParsePattern result. The
three command-owned format strings and VPrintf argument/requested/accepted
bytes are observed; PrintFault's code/header/result are separate, without
host-generated DOS fault text. Most cleanup vectors deliberately poison
IoErr. The selected fault and the final ambient value are therefore separate
assertions; no post-cleanup IoErr restoration is invented.

The eight hazardous experiments stop at a guarded boundary. **None returns
or counts as a passed command, safe cleanup or established original DOS
behavior.** Receipts retain PC/SP, raw D0, the command's selected stack return
slot where readable, IoErr, calls/output and live resources; a selected stack
value is not a returned D0.

| Fixture suffix | Observed guarded boundary on 68000 and 68040 | Important limit |
| --- | --- | --- |
| `RN31-M02.parser119-unstarted-matchend` | MatchEnd requested with zero prior MatchFirst; selected stack return20 | Original MatchEnd on a never-started cleared anchor is not executed |
| `RN31-D03.multiple-nondirectory-search-not-ended` | Diagnostic and selected return0, then anchor free attempted with an owned search live | No fake MatchEnd is inserted and no resource-safe return is claimed |
| `RN31-D03.wild-nondirectory-search-not-ended` | Same omitted-search cleanup path for a supplied wildcard match | Matcher resource model is supplied, not original DOS internals |
| `RN31-E01.direct-failure-repeated-matchend` | Second MatchEnd requested after earlier end, with chosen error214 restored and raw-FROM diagnostic emitted | Stops before common failure handler sets the final return20; original idempotence remains unknown |
| `RN31-P02.unchecked-parsepattern-output` | Rename receives a buffer without a terminator; the supplied Rename vector's bounded string read stops | `supplied-vector-read`, not a native CPU buffer read or original DOS result |
| `RN31-N02.failed-namefromlock-prefix-underread` | Native byte read before the cleared empty destination allocation | `native-read`; no uncontrolled read is allowed |
| `RN31-N03.destination-append-overflow` | Native append crosses the 256-byte destination allocation | `native-write`; guard prevents the actual out-of-range write |
| `RN31-D01.synthetic-matchfirst-error-zero-ioerr` | Failed MatchFirst/IoErr0 skips common end; live-search anchor free attempted | Explicitly synthetic error combination; no original-DOS outcome claim |

The opaque supplied search allocation deliberately makes omitted cleanup
visible. The executor does not assume unstarted/repeated MatchEnd is safe,
normalize these paths, or finish their cleanup behind the command's back.
Real MatchEnd behavior and a safe independently written replacement policy
remain prerequisites to implementing those branches.

All private receipts are under
`D:/TestData/CopperOSCommands/Workbench31/rename-reference-vectors-b1b45318001c4223bafa55afc95a73b5/`.
The [run audit](D:/TestData/CopperOSCommands/Workbench31/rename-reference-vectors-b1b45318001c4223bafa55afc95a73b5/run-audit.json)
has SHA256 `4a54b9dff3a28c8ef5d2f27ad395f571438c98178ab7caddd625a059ea5ff226`.
It records eleven pre-build input hashes, the build command, unchanged
post-run inputs, actual selected runtime assembly paths/hashes and each
receipt. These observations do not establish clean rebuild reproducibility
or bind every transitive compiler/restore input. The failed 68020 envelope
does not claim a successful end-of-run snapshot check.

| Evidence file or binary | SHA256 |
| --- | --- |
| `prebuild-final-inputs.json` | `205b7844dd9ee02e22421ae1f095bab211dd01dad1408ee54b7e35a96f78e3ef` |
| `68000-final.json` | `9c96b1f2200462998e6e22165b23f3c9bfc9be67173e0d69a91cbbb25c44f878` |
| `68020-final.json` (failed) | `be687ce5f11e668abf39705aa00b20750eaca7cfa543cd9b4ec1935a50e76079` |
| `68040-final.json` | `1b846ada797a7e2fb7007bd9beddc24518f6d920be7c60c61919526c2663fe6b` |
| Final `CopperOS.Commands.RenameNativeExecution.dll` | `9f2b41f845b9cd4dd157a5c97ab57ec886f4b47e86721d4200bdc3e2e54b6ad9` |
| Selected `Copper68k.dll` (1.4.0) | `8046d9a2083c198647a02d9146b735629890d9a2e14bc313968a3b48139a2cd5` |
| Selected `CopperSharp.Sdk.Amiga.dll` (preview.1-g20) | `16fd521454a7c26c5730a35f2864b4fe61ff7d8c617ed091c9abfdef368feeb6` |
| Read-only linked `HunkImage.cs` | `a357bcd594c12a072b23c788e002958636958b23e8361ef204dab4ee9f4c88ee` |

The host-only [receipt safety script](D:/Koodit/GIT/CopperOS/tests/Commands.RenameNativeExecution/verify_failure_receipts.py)
passed twelve checks with zero native executions: missing/wrong-size/wrong-hash
references, unknown suite/CPU, direct/hardlink/physical-alias report targets,
lexical/physical repository input paths, replacement of a safe existing failure
receipt and refusal of a non-JSON report. Windows physical-file resolution
is used; other platforms fail closed until equivalent protection exists.
Existing input content remains protected even when setup failed before its
hash was assigned, and receipt writes replace a new directory entry instead
of truncating an existing hardlink. Disposable invalid files and every receipt
remain outside the repository; the original is read-only and unchanged.
The [twelve-check receipt](D:/TestData/CopperOSCommands/Workbench31/rename-reference-vectors-b1b45318001c4223bafa55afc95a73b5/receipt-negative-5757b22e4dc6448a8726c1ff58f64baf/safety-checks.json)
SHA256 is `e3ba40d2a518fdc55c3f74047623520441e54ba8b327551c68f470c97c3dcaf1`.

## RN1-WB.CPU020 correction and replay

The [CPU correction](../cpu-byte-postincrement-qualification.md) adds the missing
byte-immediate postincrement form, preserving the existing 68040 fallback.
One unchanged 144-case CPU test DLL moves from 60 failures/84 controls to 144
passes; all 857 related checks pass. Source-bound private builds retain the
earlier opcode48F9 CPU parent and exclude later unrelated core changes.

The unchanged original-only Rename executor is then run against the before
and after CPU on each of 68000/020/040. The before 68020 run reproduces the
17-start/16-return `$12FC` failure. The after run records all 40 returned cases
and eight separate guard stops on every CPU. The 68000/040 returned and guarded
records are unchanged. The [new 68020 receipt](D:/TestData/CopperOSCommands/Q/move12fc861db2a9/rename-after-68020.json)
has SHA256 `686ddb82e40fb3fedd084eb0680e5a8ecfc22f09f23f2151f8ecff726b2d7b33`;
the after CPU DLL is `d29fcdbedf4732ee4f0209d0f2a0ff2bb250a6bda826d9a08ffbdfdaebefdd2f`.
No original bytes, vector expectations or timing policy were bypassed.

This closes `RN1-WB.CPU020` only. Supplied parser/matcher results, formatter
output and hazard stops retain all limits above; no full DOS or replacement
Rename result is inferred.

## Next executable slices and completion limits

1. `RN1-WB.CPU020` is complete for the bounded supplied-vector subset above.
   Retain both failed historical matrices and the separate source-bound CPU
   correction/replay; this is not original DOS or generated-command approval.
2. `RN2-WB.MATCHEND`: execute original DOS MatchEnd with the never-started,
   already-ended and live-search contexts under bounded native instrumentation.
   Establish actual ownership/final-IoErr behavior and choose an explicit safe
   replacement policy for the guarded buffer and cleanup paths. Preserve ordinary
   syntax and behavior; do not reproduce memory corruption or unowned cleanup.
3. `RN3-WB.CLI`: use the real launch/input boundary in
   [CC02.API14](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/native-command-launch-contract.md)
   for required arguments, `/M` to `/A` assignment, TO/AS, quoting, QUIET and
   original parser/handler outcomes. Supplied vector strings cannot close those
   cases. MorphOS template and behavior remain an independent required profile.
4. Only after those decisions write independent native code using the public
   3.1 APIs, qualify
   original/replacement results and failure cleanup, and proceed to real DOS,
   handler, Shell, Workbench, installed selection and purity gates.

No replacement Rename, complete real-DOS runtime pass, packaged command,
complete contract or resident qualification is established here. MorphOS
remains required and has not been silently deferred or removed.

## Original-DOS direct and directory reference (2026-09-06)

The hash-verified original1140-byte Workbench Rename now executes two calls
through original Shell/ReadArgs/DOS on disposable media. Direct RAM:old to
RAM:new succeeds through ParsePattern. The subsequent two-source QUIET call
moves RAM:new and RAM:other into RAM:target. NameFromLock yields the observed
Ram Disk:target prefix. Each MatchNext returns232 before the corresponding
Rename succeeds, confirming copied-current-name ordering. Normal direct mode
has one MatchEnd; directory mode has three (preflight plus each source).
Both invocations return0, balance four AllocVec acquisitions/releases and
preserve the shared image. After removal, original Type independently reads
13/14 exact payload bytes at the destination paths.

Evidence: artifacts/copy-boot-fixture/rename-reference-vector.trx,
rename-reference-vector-observations.json, verified-rename-reference-vector.json
and rename-reference-controls.json. Verifier: tools/Commands/verify_rename_reference_boot.py.
The first observer run stopped after successful direct rename because it did
not track AllocVec; preserved as rename-reference.trx/observations. The
observer now records each vector allocation's original header and requires
matching subsequent FreeMem address/size, keeping ownership assertions.

This covers ordinary started-search cleanup only. Never-started/repeated
MatchEnd, omitted search cleanup, unsafe buffer paths and their safe replacement
policy remain unresolved. No Rename replacement or shipping gate is closed.

## RN2-WB never-started/repeated MatchEnd on original DOS (2026-09-06)

Three original resident calls now return20/20/0 for missing TO, duplicate
regular destination and successful recovery. Real ReadArgs failure116 is
followed by one MatchEnd on the cleared anchor with no preceding MatchFirst.
The duplicate case ends its successful preflight, fails Rename with203, then
calls MatchEnd again on the same anchor before PrintFault. Both MatchEnd calls
return. A final rename succeeds. The four command-owned vector allocations
balance percall and the image is unchanged. Original Type reads the recovered
source and untouched existing target exactly.

Evidence: artifacts/copy-boot-fixture/rename-matchend.trx,
rename-matchend-observations.json, verified-rename-matchend.json and
rename-matchend-controls.json; tools/Commands/verify_rename_matchend_reference.py.
Three negative controls reject a missing call, different anchor and different
duplicate error. This establishes returned behavior for these concrete original
DOS contexts; it does not trace every private matcher allocation or generalize
to every provider/anchor state.

Safe replacement decision: explicitly track whether MatchFirst has been
attempted and whether that search has been ended. End each attempt exactly
once, including failed attempts which may own matcher resources. Do not call
MatchEnd before any attempt or repeat it after ending the search. Preserve
selected fault codes and visible results; record omitted redundant no-op calls
as an explicit normalization in differential fixtures. The original
multiple/wild-source nondirectory branch that omits cleanup still needs its
own observation and policy validation before body implementation. Buffer
boundary/failed-name conversion safety decisions also remain open.

## RN2-WB nondirectory branch and safe implementation policy (2026-09-06)

Original multi-source and wildcard-source calls to a regular destination both
return0, print the destination diagnostic and call neither Rename nor
MatchEnd. QUIET on the wildcard call does not suppress the diagnostic. A
following direct rename succeeds; all three original payloads read back
correctly at their expected paths. Command-owned vectors balance, but opaque
matcher leak freedom is explicitly unproven. Evidence:
artifacts/copy-boot-fixture/verified-rename-nondirectory.json and
rename-nondirectory-controls.json; verifier:
tools/Commands/verify_rename_nondirectory_reference.py.

The following decisions now permit an independent bounded body implementation;
they are required implementation rules, not completed replacement tests:

- Preserve return0, diagnostic and no-Rename behavior for nondirectory rejection.
  End its attempted search exactly once before freeing the anchor. Preserve
  ambient IoErr around this added cleanup, then perform normal command cleanup.
  Record this cleanup addition explicitly in differential normalization.
- Track attempted search ownership independently of the primary result. End
  every MatchFirst attempt once, including failed attempts; avoid unstarted or
  repeated MatchEnd. Do not infer ownership from MatchFirst success alone.
- Check ParsePattern and NameFromLock failure. Stop before Rename, capture the
  supplied IoErr, and return20 even for an explicitly synthetic failed-API/error0
  combination. Print a fault only when a nonzero error is available; do not
  invent a successful path buffer or fabricate an OS failure code.
- A successful NameFromLock that yields an empty name is an invalid component:
  reject with210 before indexing its preceding byte. Never read before the
  destination allocation.
- Retain256-byte source/destination capacities with space for the terminator.
  Validate complete source copies and prefix/slash/basename composition before
  mutation. Reject unterminated or over-capacity generated paths with120 before
  Rename; never truncate silently or issue a partial-path rename. Preserve all
  previously completed renames when a later item fails.
- Keep original MatchNext-before-Rename ordering and its nonzero-as-pattern-end
  behavior. QUIET affects progress only. Do not add recursion, overwrite or
  cross-volume copy behavior.
- Keep final ambient IoErr distinct from selected diagnostic error. Do not use
  the shared argument lease's error-restoring Release as an implicit final policy.

These deliberate safety differences replace undefined memory access and missing
ownership cleanup, not ordinary command semantics. Existing guarded original
experiments remain retained as hazards; no unsafe original path must be run to
claim parity. The generated body, finite native matrix and bounded normalized
reference comparisons remain the next work, not completed gates.

### 2026-09-06 - Candidate Rename direct/directory original-DOS pass

The independent 68000 Workbench candidate (SHA-256 ffc200c9350e86e50c1173c73872c1b6bed8739398579ab71b4f97444eef0d28) passes the original-DOS direct/directory fixture. Two resident invocations return 0/0, share the unchanged segment and each balance four vector allocations. The additional candidate directory FIB is allocated and released by identity. Direct ParsePattern dispatch and directory MatchNext-before-Rename order match the reference checks; all three exact path operations succeed. After resident removal, original Type reads the exact 13/14-byte destination contents.

Evidence: artifacts/copy-boot-fixture/rename-native-media.json, rename-native.trx, rename-native-observations.json and verified-rename-native.json. Candidate verifier mode keeps this separate from original-reference evidence. Three negative controls reject wrong destination, incorrect FIB release and changed resident image (rename-native-controls.json). This covers ordinary 68000 behavior only; error/normalization and boundary cases, 68020/68040 runtime, concurrency, full profile, purity and packaging remain open. Shipping 0/200; goal active.

### 2026-09-06 - Candidate Rename parser/duplicate failure and recovery pass

The current 68000 candidate passes the original-DOS failure fixture (rename-native-errors.trx). Three resident invocations return 20/20/0: ReadArgs error 116, duplicate destination error 203, then successful recovery. Exact recovered and preserved-target bytes match the reference. Each invocation balances four command-owned vectors; the existing-destination FIB is released by identity. The shared image stays unchanged and resident removal succeeds.

verify_rename_matchend_reference.py now has explicit candidate mode: MatchEnd counts 0/1/1 versus original 1/2/1. This verifies the documented removal of unstarted/repeated cleanup without accepting it as exact call-trace parity. The original trace still passes its separate mode. Candidate evidence: artifacts/copy-boot-fixture/rename-native-errors-media.json, rename-native-errors-observations.json, verified-rename-native-errors.json and rename-native-errors-controls.json. Three controls reject wrong fault, repeated cleanup and wrong FIB release. Opaque matcher allocation freedom, final error timing, full output formatting, boundary failures, other-CPU runtime, concurrency, purity and packaging remain unqualified. Shipping 0/200; goal active.

### 2026-09-06 - Candidate Rename nondirectory cleanup and recovery verified

Current 68000 candidate passes the multi-source/wildcard-to-regular-file fixture. All three calls return 0, matching the original unusual rejection status; both rejection calls emit the destination diagnostic, including QUIET, with no Rename/MatchNext. Each candidate rejection ends its attempted search exactly once after the diagnostic, unlike the original omission. Recovery Rename succeeds. Original Type confirms exact recovered, untouched second-source and existing-target payloads. Four vector allocations per invocation, parser ownership and candidate FIB releases balance; resident removal succeeds and image stays unchanged.

Evidence: artifacts/copy-boot-fixture/rename-native-nondirectory-media.json, rename-native-nondirectory.trx, rename-native-nondirectory-observations.json, verified-rename-native-nondirectory.json and rename-native-nondirectory-controls.json. Candidate verifier mode explicitly records the cleanup normalization; original mode still passes. Three negative controls reject missing cleanup, suppressed QUIET diagnostic and wrong return. This is not proof of opaque matcher leak freedom or final IoErr timing.

Added rename-development-candidates.json, validating binary hashes for all three CPUs and receipt/image/observation/report hash chains for the three 68000 fixtures. Eight candidate original-DOS invocations are covered across ordinary, error and nondirectory fixtures. Other-CPU runtime, boundary cases, concurrency, full profile, purity and packaging remain open. No accepted packaging manifest changed; shipping 0/200 and full goal active.

### 2026-09-06 - Candidate Rename native startup/early-break suite passes on three CPUs

Added a separate workbench-rename-startup-vector-fixture in the shared native runner; the original-only Rename executor remains unchanged and hash-restricted. Ten candidate invocations per CPU (30 total) pass on 68000/020/040: missing DOS, Workbench message, Workbench with missing DOS, negative entry length, nonzero length/null pointer, pending Ctrl-C, and interleaved missing-DOS/break plus Workbench caller pairs. Existing runner guards verify current-process Result2=122 for missing DOS, exact startup results/errors, stack restoration, library/message ownership, ABI volatility and unchanged shared image. Break mask 0x1000 and fault304/null header are checked; no parser/workspace allocation occurs on these paths. These candidate startup policies do not establish full original startup parity.

Evidence: artifacts/workbench-rename-native/rename-{68000,68020,68040}.startup.json, hash-bound in rename-development-candidates.json. Build output: artifacts/copy-boot-fixture/rename-startup-runner-build.log; runner: artifacts/rename-startup-runner/bin/CopperOS.Commands.NativeExecution/release/CopperOS.Commands.NativeExecution.dll. Invoke the runner with candidate HUNK, CPU, output JSON and workbench-rename-startup-vector-fixture. Workbench MakeLink's existing 20-case 68000 suite passes as a runner regression (artifacts/rename-startup-runner/makelink-regression.json).

This adds actual candidate native execution on all three CPUs, restricted to startup/early-break scope. Full Rename body failure and boundary vectors, body concurrency, minimum stack, full profile, original startup comparison, purity and packaging remain open. Shipping 0/200; goal active.

### 2026-09-06 - Rename four allocation failure points qualified on three CPUs

Extended the separate candidate startup suite with failure at each of the four AllocVec requests and an interleaved first/fourth allocation failure pair. Sixteen invocations per CPU pass (48 total, including prior startup cases). Vector gateways assert sizes/flags/order, exact pointer ownership and cleanup order including null FreeVec calls. Generic allocation guards enforce no leaks or foreign writes. Each failure reports 103 with null fault header and return20; no ReadArgs or matching occurs. Shared image remains unchanged. This is supplied-vector candidate evidence, not real low-memory DOS execution or original error-timing parity.

Evidence: artifacts/workbench-rename-native/rename-{68000,68020,68040}.allocation.json, hash-bound in rename-development-candidates.json. Runner build artifacts/rename-allocation-runner; build log artifacts/copy-boot-fixture/rename-allocation-runner-build.log. Earlier startup-only reports remain preserved. Body parser/matcher/path boundary vectors, successful body concurrency, full profile and shipping/purity gates remain open. Shipping 0/200; goal active.

### 2026-09-06 - Rename parser failure native coverage on three CPUs

Candidate suite now executes ReadArgs failure116 and synthetic failure with IoErr0, separately and interleaved. Twenty cumulative invocations per CPU pass (60 total including startup/allocation cases). Assertions check exact FROM/A/M,TO=AS/A,QUIET/S template, private result-buffer identity, three initially cleared argument cells, null supplied RDArgs, primary20 in both cases, fault116/null header only for nonzero error, four owned buffer releases and no unstarted MatchEnd or FreeArgs. Shared image remains unchanged. Synthetic error0 is not claimed to arise from original DOS.

Evidence: artifacts/workbench-rename-native/rename-{68000,68020,68040}.parser.json, bound by image/report hashes in rename-development-candidates.json; runner artifacts/rename-parser-runner and build log artifacts/copy-boot-fixture/rename-parser-runner-build.log. Historical smaller suite reports remain preserved. Matcher/path-boundary vectors, successful body concurrency, full profile and shipping/purity gates remain open. Shipping 0/200; full goal active.

### 2026-09-06 - Reproducible Rename candidate qualification driver

Added tools/Commands/qualify_workbench_rename_native.ps1. It requires a fresh output directory, compiles all three resident HUNKs through the existing static gate, builds an isolated native runner, executes the current 20-case suite on each CPU and verifies suite/CPU/count/image-hash/image-write/non-shipping report fields. It records per-CPU report hashes and emits qualification.json only after all checks succeed. It does not claim full command qualification.

Executed successfully with -DotnetPath C:/D-drive/Koodit/GIT/CopperOS/obj/dotnet-sdk-10.0.301/dotnet.exe -OutputDirectory C:/D-drive/Koodit/GIT/CopperOS/artifacts/workbench-rename-entry-qualified. All 60 native invocations pass; rebuilt binary hashes match the existing development candidates, retaining their bounded original-DOS evidence. Driver receipt is bound in rename-development-candidates.json. Build/run log: artifacts/copy-boot-fixture/rename-qualification-driver.log. Next extend candidate vectors into matcher/path failures and successful body concurrency; full profile, purity and packaging remain open. Shipping 0/200; goal active.

### 2026-09-06 - Native initial matcher failure and parser ownership

Extended candidate vectors through successful ReadArgs into MatchFirst failure. Supplied MatchFirst D0=103 with independent IoErr214 returns20/fault214; synthetic IoErr0 returns0 without a fault, preserving the original selected-error policy. Both cases run sequentially and interleaved. Assertions check private parser storage, cleared argument cells, matcher anchor/break configuration, one MatchEnd per attempted search, parser release after MatchEnd and exact four-vector cleanup. No Lock/Rename gateway is available to accidentally accept later mutation. Twenty-four cumulative invocations per CPU pass (72 total).

Updated qualification driver expected coverage and ran it successfully into artifacts/workbench-rename-matcher-qualified. qualification.json and per-CPU runtime reports are bound in rename-development-candidates.json; binary hashes remain unchanged. Log: artifacts/copy-boot-fixture/rename-matcher-driver.log. Build retains three existing nullable warnings outside the Rename suite, zero errors. Supplied vectors do not prove real-DOS synthetic error combinations or opaque matcher leak freedom. Remaining: error205 prefix, successful matcher/direct/path-boundary vectors, successful body concurrency, full profile, purity and packaging. Shipping 0/200; full goal active.

### 2026-09-06 - Rename initial error205 diagnostic survives output failure

Added initial MatchFirst IoErr205 case and interleaving with synthetic zero-IoErr failure. VPrintf asserts the exact prefix template, private argument storage and old/new strings, captures exact output bytes, then deliberately returns -1 and changes IoErr to999. Native code restores205 before MatchEnd/PrintFault; assertions verify that order and fault205/null header. Twenty-seven cumulative invocations per CPU pass (81 total), with unchanged binaries and shared image, balanced parser/vector storage. This tests selected-error preservation under supplied output failure, not exact real-DOS output-failure timing.

Evidence: artifacts/workbench-rename-prefix-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-prefix-driver.log. Qualification driver coverage count updated. Successful matcher/direct/path-boundary vectors, successful body concurrency, full profile and shipping/purity remain open. Shipping0/200; goal active.

### 2026-09-06 - Native direct Rename success and pattern rejection

Extended candidate vectors through successful MatchFirst, missing destination Lock, completed preflight and ParsePattern. Direct success asserts Rename uses the parsed private source buffer and original TO string. ParsePattern failure120 and synthetic failure/IoErr0 both return20 without Rename; the zero-error case emits no invented fault. A successful ParsePattern output containing256 non-NUL bytes is rejected120 before Rename. The success/unterminated pair also executes interleaved with private buffers/parser storage. Thirty-three cumulative cases per CPU pass (99 total), unchanged binary/image and full tracked cleanup.

Evidence: artifacts/workbench-rename-direct-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-direct-driver.log. Driver count/scope updated. This is supplied-vector boundary evidence, not proof that original DOS emits unterminated output. Exact-capacity valid path, directory composition/NameFromLock boundaries, broader direct errors and successful directory concurrency remain required along with full profile, purity and packaging. Shipping0/200; goal active.

### 2026-09-06 - Rename exact-capacity valid parsed path

Added a successful ParsePattern result with255 source bytes and NUL at byte255. Rename receives all255 bytes unchanged, using its private source buffer and original TO. Sequential and interleaved execution against the256-byte unterminated rejection both pass on68000/020/040. This checks both sides of the buffer boundary without truncating valid input. Thirty-six cumulative invocations per CPU pass (108 total), with unchanged binary/image and balanced tracked ownership. Supplied Rename success does not establish that every filesystem accepts a255-byte component.

Evidence: artifacts/workbench-rename-capacity-qualified/qualification.json and per-CPU runtime reports, hash-bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-capacity-driver.log. Directory composition/NameFromLock boundaries, broader direct errors, directory concurrency, full profile, purity and packaging remain required. Shipping0/200; full goal active.

### 2026-09-06 - Native directory name failure and invalid-output boundaries

Added directory-mode cases with successful preflight, private destination lock, allocated/aligned FIB and Examine directory result. NameFromLock failure120 and synthetic failure/IoErr0 both return20; successful empty output rejects210, and256 non-NUL bytes reject120. No case reaches ParsePattern or Rename. Assertions verify256-byte output capacity, preflight cleanup before NameFromLock, FIB release by identity, one destination unlock and parser/vector cleanup. Failure/unterminated cases also run interleaved with distinct owned locks and buffers. Forty-two cumulative invocations per CPU pass (126 total), unchanged binary and shared image.

Evidence: artifacts/workbench-rename-name-qualified/qualification.json and per-CPU runtime reports, hash-bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-name-driver.log. These are supplied-vector invalid-output policies, not original unsafe-path reproduction. Valid directory prefix/basename composition boundaries, successful directory matching/concurrency, wider option/errors, full profile and shipping/purity remain open. Shipping0/200; goal active.

### 2026-09-06 - Directory composition limits and successful native interleaving

Added normal OUT: prefix,251-byte colon-terminated prefix plus4-byte basename (255-byte valid path), and252-byte prefix plus4-byte basename (256-byte rejection). Successful cases traverse two supplied source entries under QUIET. MatchNext overwrites anchor name/path buffers before Rename; the command still supplies intact private SRC:file and composed destination strings. Overflow rejects120 before MatchNext/Rename and ends the attempted search. Normal and capacity-success callers also execute interleaved with distinct locks/FIB/parser/vector storage and shared unchanged image. Forty-seven cumulative invocations per CPU pass (141 total).

Evidence: artifacts/workbench-rename-compose-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-compose-driver.log. Rebuilt HUNK hashes unchanged. Supplied duplicate source entries are control-flow/ownership fixtures, not real filesystem duplicate-move semantics. Slash-appended prefix boundaries, nonquiet output, directory mutation failures, original-DOS concurrency and full profile/purity/packaging remain required. Shipping0/200; goal active.

### 2026-09-06 - Slash-appended directory path boundaries

Added250-byte non-colon prefix plus slash and4-byte basename (valid255-byte destination),251-byte prefix plus slash/basename (256-byte rejection), and255-byte prefix lacking room for slash/NUL. Success preserves the entire composed name; both overflow cases return20/error120 without Rename. The no-room case stops before beginning another match; attempted searches remain balanced by the existing equality guard. Valid and overflow callers also execute interleaved. Fifty-two cumulative cases per CPU pass (156 total), unchanged binary/image and full tracked cleanup.

Evidence: artifacts/workbench-rename-slash-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-slash-driver.log. Nonquiet progress output, directory mutation failures, broader matcher/options, original-DOS concurrency, full profile and purity/packaging remain required. Shipping0/200; full goal active.

### 2026-09-06 - Directory progress output failure does not suppress Rename

Added nonquiet directory traversal with exact two progress lines, private format-argument identity and MatchNext -> VPrintf -> Rename ordering. VPrintf captures each requested line then returns -1 and sets IoErr999; subsequent supplied Rename succeeds and sets IoErr0. Command returns0 and completes both operations. Nonquiet failure-output and QUIET success callers also execute interleaved, checking isolated output streams and owned buffers. Fifty-five cumulative cases per CPU pass (165 total), unchanged binaries/image and balanced tracked resources. Captured requested bytes do not claim that a failing real console would display them fully.

Evidence: artifacts/workbench-rename-progress-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-progress-driver.log. Directory mutation failures, broader matcher/options, original-DOS concurrency, full profile and purity/packaging remain required. Shipping0/200; full goal active.

### 2026-09-06 - Directory second-operation failure after success

Added supplied first Rename success followed by second Rename failure203. The command performs exactly two calls, prints exact SRC:file/OUT:file failure prefix despite QUIET, restores203 after failing VPrintf poisons IoErr999, ends the active search and reports fault203/null header with return20. No retry or compensating Rename occurs. Interleaving with a fully successful directory caller passes with private ownership and output. Fifty-eight cumulative invocations per CPU pass (174 total), unchanged binary/image and balanced tracked resources. This verifies operation sequence; the supplied vector fixture does not itself prove persisted filesystem state after partial failure.

Evidence: artifacts/workbench-rename-partial-qualified/qualification.json and per-CPU runtime reports, hash-bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-partial-driver.log. Real-provider partial-failure persistence, broader matcher/options, original-DOS concurrency, full profile and purity/packaging remain required. Shipping0/200; full goal active.

### 2026-09-06 - Original-DOS partial failure persistence verified

The 68000 candidate completes a real directory rename of RAM:first, then fails203 on RAM:second because target/second exists. A following resident invocation successfully renames the remaining second source to RAM:recovered. Returns20/0, exact operation paths/counts, failure prefix/fault ordering, search/parser cleanup, four balanced command vectors per call, unchanged image and resident removal pass. Original Type independently reads exact first-payload, existing-payload and second-payload bytes from target/first, target/second and recovered respectively. This establishes persisted partial success, preserved destination and recoverable source for this RAM-handler case.

Evidence: artifacts/copy-boot-fixture/rename-native-partial-media.json, rename-native-partial.trx, rename-native-partial-observations.json, verified-rename-native-partial.json and rename-native-partial-controls.json. New verify_rename_partial_boot.py rejects wrong first destination, wrong second error and altered payload. Receipt/report chain is recorded against current68000 candidate. Broader filesystem/error/option coverage, original-DOS concurrency, full profile and purity/packaging remain open. Shipping0/200; goal active.

### 2026-09-06 - Candidate directory FIB allocation failure cleanup

Added AllocDosObject failure103 and synthetic failure/IoErr0 after successful destination Lock and preflight. Both return20, only nonzero error produces a fault, and neither reaches Examine, FreeDosObject, NameFromLock or Rename. Search, parser, directory lock and four command vectors are released; failed FIB is neither used nor freed. Interleaving allocation failure with directory success passes. Sixty-two cumulative cases per CPU pass (186 total), unchanged binaries/shared image. This covers the candidate's additional private FIB allocation, not original stack-FIB parity or real-memory exhaustion.

Evidence: artifacts/workbench-rename-fib-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-fib-driver.log. Broader matcher/options, original-DOS concurrency, full profile and purity/packaging remain required. Shipping0/200; full goal active.

### 2026-09-06 - MatchNext break/error retains current-item processing

Added supplied MatchNext304 and103, each setting matching IoErr and overwriting the anchor buffers. Candidate still renames the saved current item, ends that search and starts the next supplied source pattern, where the behavior repeats. Supplied successful Rename sets IoErr0; final return0/no fault is asserted. The two callers also execute interleaved. Existing exact paths/counts/ownership guards remain active. Sixty-six cumulative cases per CPU pass (198 total), unchanged binaries/shared image.

Evidence: artifacts/workbench-rename-next-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-next-driver.log. This preserves the documented original nonzero-MatchNext control flow; it does not claim full real-signal timing parity. Single-source SameLock branches, later MatchFirst errors, real-DOS concurrency, full profile and purity/packaging remain required. Shipping0/200; goal active.

### 2026-09-06 - Later source-pattern failure205 without initial prefix

Added later MatchFirst205 after a completed first source operation. Candidate performs exactly one Rename, cleans up the failed later search, prints fault205/null header with no VPrintf prefix, and returns20. Interleaving with a fully successful directory caller passes. Sixty-nine cumulative invocations per CPU pass (207 total), unchanged binary/shared image and balanced tracked resources. This is supplied-vector call-flow evidence; persisted partial success is separately covered by the original-DOS duplicate-destination fixture.

Evidence: artifacts/workbench-rename-later-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-later-driver.log. Single-source SameLock dispatch, broader options, original-DOS concurrency, full profile and purity/packaging remain required. Shipping0/200; goal active.

### 2026-09-06 - Single-source directory dispatch and temporary lock ownership

Added one-source vectors with SameLock1 and-1, plus failure to lock the source after successful destination examination. All follow directory traversal and perform exactly one Rename. SameLock receives distinct source/destination BPTRs; successful temporary source locks must be released before final destination cleanup, while missing source lock has no fabricated unlock. SameLock1/-1 callers also execute interleaved with distinct process-owned locks. Seventy-four cumulative invocations per CPU pass (222 total), unchanged binary/shared image and balanced tracked resources.

Evidence: artifacts/workbench-rename-single-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-single-driver.log. SameLock0 direct fallback, broader options, original-DOS concurrency, full profile and purity/packaging remain required. Shipping0/200; goal active.

### 2026-09-06 - SameLock0 direct fallback

Added distinct source/destination BPTRs whose supplied SameLock result is0. Candidate releases temporary source lock, ends preflight, calls ParsePattern and performs exactly one direct Rename using parsed-old and original TO. It does not call NameFromLock or directory traversal. Destination FIB/lock and parser/vector ownership remain balanced. Interleaving this direct fallback with SameLock1 directory traversal passes on all CPUs. Seventy-seven cumulative invocations per CPU pass (231 total), unchanged binary/shared image.

Evidence: artifacts/workbench-rename-same-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-same-driver.log. This verifies dispatch based on SameLock semantics rather than BPTR equality. Broader option/reference coverage, original-DOS concurrency, full profile and purity/packaging remain required. Shipping0/200; goal active.

### 2026-09-06 - Original-DOS concurrent resident Rename succeeds

Two original Shell processes run the same resident68000 Rename image with separate two-source directory moves. The trace proves maximum active invocations2, distinct tasks and overlapping DOS leases. Both return0 with four command vectors balanced per task; parser/FIB/destination-lock identities match cleanup, and MatchNext precedes each of four exact Rename operations. Image stays unchanged. Resident removal succeeds after both calls complete; original Type reads exact background-one/two and foreground-one/two payloads from all four destinations. The intervening source Type is not assumed to synchronize; observed completion/removal order is required by the verifier.

Evidence: artifacts/copy-boot-fixture/rename-native-concurrent-media.json, rename-native-concurrent.trx, rename-native-concurrent-observations.json, verified-rename-native-concurrent.json and rename-native-concurrent-controls.json. New verify_rename_concurrent_boot.py rejects absent overlap, same-task substitution and wrong destination. Receipt/report hashes are bound to current68000 candidate. This is bounded same-image original-DOS concurrency, not full PURE admission or opaque matcher leak freedom. Full option/reference coverage, minimum stack, purity metadata and packaging remain required. Shipping0/200; goal active.

### 2026-09-06 - Explicit4096-byte candidate stack budget

All77 cases per CPU now explicitly configure4096-byte stacks instead of inheriting the runner's16384-byte default. Existing stack write bounds, upper/lower canaries and restored-SP checks pass in all231 invocations, including interleaved callers. Qualification driver rejects reports with a different configured budget or excessive written depth. Maximum observed candidate-written depths by CPU: {'68000': 204, '68020': 204, '68040': 204}. These measurements exclude real DOS/library stack consumption because vector calls are supplied; they establish neither an absolute minimum nor full real-system4096-byte safety.

Evidence: artifacts/workbench-rename-stack-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-stack-driver.log. HUNK hashes unchanged. Full reference/options, real-system stack qualification, purity metadata and packaging remain open. Shipping0/200; goal active.

### 2026-09-06 - Matcher filename/full-path terminator boundaries

Added supplied successful MatchFirst outputs with108 non-NUL filename bytes and256 non-NUL full-path bytes. The valid destination prefix leaves room for a normal name, so these specifically exercise source termination checks rather than destination overflow. Both reject120/return20 before MatchNext or Rename and release attempted search, FIB, directory lock, parser and command vectors. Interleaved malformed-name/malformed-path callers pass. Eighty-one cumulative cases per CPU pass (243 total) on4096-byte configured candidate stacks, unchanged binaries/shared image.

Evidence: artifacts/workbench-rename-anchor-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-anchor-driver.log. These are explicit safe malformed-provider output policies, not claims that original DOS emits them. Full reference/options, real-system stack qualification, purity metadata and packaging remain open. Shipping0/200; goal active.

### 2026-09-06 - Single source yields two distinct matches

Added one-source traversal where first MatchNext returns0 and replaces anchor file/SRC:file with next/SRC:next before the first Rename. Candidate still renames SRC:file to OUT:file, then on MatchNext232 renames SRC:next to OUT:next. Exactly two mutation calls and balanced preflight/traversal ends are required. Interleaving with a single-match source caller passes. Eighty-four cumulative invocations per CPU pass (252 total), unchanged binaries/image and4096-byte guarded candidate stacks.

Evidence: artifacts/workbench-rename-multimatch-qualified/qualification.json and per-CPU reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-multimatch-driver.log. Updated W01 audit status. Real wildcard grammar/provider behavior and later errors within one pattern remain open, along with other listed profile/purity/packaging gaps. Shipping0/200; goal active.

### 2026-09-06 - Synthetic direct Rename(FALSE)/IoErr0

Added direct Rename returningFALSE with IoErr0. Candidate emits the original raw FROM/TO prefix, restores zero after VPrintf fails and poisons IoErr999, performs no PrintFault and returns0. It still releases parser, vectors and preflight correctly. Interleaving with direct success passes. Eighty-seven cumulative cases per CPU pass (261 total), unchanged binaries/image and4096-byte guarded candidate stacks. This deliberately preserves original control flow and is explicitly synthetic, not evidence that a real handler uses this status combination.

Evidence: artifacts/workbench-rename-falsezero-qualified/qualification.json and per-CPU reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-falsezero-driver.log. E02 audit updated; directory variant, PrintFault/cleanup IoErr gaps and other full-profile/purity/packaging requirements remain open. Shipping0/200; goal active.

### 2026-09-06 - PrintFault failure and cleanup-time secondary error

Added ReadArgs failure116 with PrintFault returning0 and setting IoErr999; primary return remains20 and final observed secondary result999. Another failure116 reports normally, then each FreeVec sets774..777; final observed error777 remains visible instead of restoring selected116. Both run independently and interleaved, preserving private errors and cleanup. Ninety-one cumulative cases per CPU pass (273 total), unchanged binary/image and4096-byte guarded candidate stacks.

Evidence: artifacts/workbench-rename-ioerr-qualified/qualification.json and per-CPU reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-ioerr-driver.log. E04/R03 audit updated with this limited parser-failure coverage. These injected error mutations are synthetic; original final Process comparison, other APIs/success branches and full-profile/purity/packaging remain open. Shipping0/200; goal active.

### 2026-09-06 - Original-DOS positional wildcard and AS alias

Candidate68000 runs Ed RAM:wild-#? TO RAM:target QUIET followed by Ed RAM:target/wild-one AS RAM:renamed through the original Shell/ReadArgs. One wildcard produces two matches (MatchNext0 then232) and two correct directory mutations; AS selects the direct destination successfully. Both resident calls return0 with four balanced command vectors, parser cleanup and unchanged image. Resident removal succeeds; original Type reads exact wildcard-one/two payloads from renamed and target/wild-two.

Evidence: artifacts/copy-boot-fixture/rename-native-wildcard-media.json, rename-native-wildcard.trx, rename-native-wildcard-observations.json, verified-rename-native-wildcard.json and rename-native-wildcard-controls.json. New verifier rejects wrong wildcard, missing continuation and wrong AS destination. Hash chain bound to current68000 candidate. This covers these concrete grammar/effect cases, not all aliases, duplicate keywords, escapes or full original-command differential. Remaining profile/purity/packaging gates stay open. Shipping0/200; goal active.

### 2026-09-06 - Original/candidate wildcard and AS comparison passes

Ran pinned original Rename37.2 with exactly the candidate wildcard/AS startup script on the same original DOS/ROM and observer. Both pass the scenario verifier: wildcard has two successful effects, AS produces the expected direct move, returns0/0, no command diagnostics, exact final payload bytes and resident removal. compare_rename_wildcard_boot.py binds original hash, candidate binary hash, both media/observation receipts and identical startup/reference-disk/ROM/test-assembly identities. Comparison excludes full call-trace, final IoErr timing and full grammar coverage.

Evidence: artifacts/copy-boot-fixture/rename-reference-wildcard-media.json, rename-reference-wildcard.trx, rename-reference-wildcard-observations.json, verified-rename-reference-wildcard.json, verified-rename-native-wildcard-regression.json and compared-rename-wildcard.json. Comparison report bound in development candidates; W01 audit updated. Original executable remains private. Other options, interactive parser behavior, handler errors, real-system stack and purity/packaging remain open. Shipping0/200; goal active.

### 2026-09-06 - Directory Rename(FALSE)/IoErr0 counterpart

Added second directory operation returningFALSE/IoErr0 after supplied first success. Candidate prints exact SRC:file/OUT:file prefix despite QUIET, restores zero after output poisoning, ends the active search, performs no common fault and returns0. Exactly two Rename calls and balanced cleanup remain required; interleaving with full success passes. Ninety-four cumulative cases per CPU pass (282 total), unchanged binaries/shared image and4096-byte guarded candidate stacks.

Evidence: artifacts/workbench-rename-directoryzero-qualified/qualification.json and per-CPU reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-directoryzero-driver.log. E02 audit updated for both direct and directory synthetic combinations. Full real-provider/options, stack/purity and packaging gaps remain open. Shipping0/200; goal active.

### 2026-09-06 - Successful direct Rename retains cleanup-time IoErr

Added direct success with Rename returningtrue/IoErr0 followed by FreeVec setting774..777. Candidate returns primary0 with final secondary777 and emits no fault. It runs interleaved with parser failure whose cleanup also sets777; primary results remain separately0/20. Ninety-seven cumulative cases per CPU pass (291 total), unchanged binaries/image and guarded4096-byte candidate stacks.

Evidence: artifacts/workbench-rename-successio-qualified/qualification.json and per-CPU reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-successio-driver.log. R03 audit updated. Synthetic cleanup mutations establish candidate policy for these branches, not original final Process equivalence across every API. Other profile, real-stack, purity and packaging requirements remain open. Shipping0/200; goal active.

### 2026-09-06 - Second distinct match fails within one source pattern

Added single-pattern file -> next traversal with first Rename success and second failure203. Diagnostic uses saved SRC:next/OUT:next even after MatchNext overwrites anchor buffers; exactly two mutation calls, one traversal end after failure, fault203/return20 and complete tracked cleanup are required. Interleaving with the fully successful two-match caller passes. One hundred cumulative invocations per CPU pass (300 total), unchanged binaries/image and4096-byte guarded candidate stacks.

Evidence: artifacts/workbench-rename-multifailure-qualified/qualification.json and per-CPU reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-multifailure-driver.log. Updated W01 remaining-work audit. This is supplied-vector partial sequence evidence, not additional real-handler persistence evidence. MorphOS source/provenance work remains separate and no full-profile/purity/packaging gate is closed. Shipping0/200; goal active.

### 2026-09-06 - Workbench Examine failure retains direct fallback

Added successful destination Lock/FIB allocation with Examine(FALSE)/IoErr222 despite a supplied positive type field. Classic candidate releases FIB, ends preflight and performs ParsePattern/direct Rename, rather than taking directory traversal or the MorphOS fatal-Examine policy. Supplied Rename succeeds with IoErr0; return0/no fault and destination-lock cleanup are required. Interleaving with normal directory success passes. One hundred three cumulative invocations per CPU pass (309 total), unchanged binaries/image and4096-byte candidate stacks.

Evidence: artifacts/workbench-rename-examine-qualified/qualification.json and per-CPU reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-examine-driver.log. D02 audit updated; original handler injection/runtime parity for this case remains separate. Full profile, purity and packaging stay open. Shipping0/200; goal active.

### 2026-09-07 - MorphOS candidate body checkpoint

The independent MorphOS50.8 implementation, native112-case-per-CPU qualification
and bounded original-Kickstart-DOS ordinary/wildcard/AS readback are recorded in
[the source contract checkpoint](../rename-morphos50-source-contract.md). This
supersedes earlier absent-MorphOS-body statements; original MorphOS runtime
correspondence and full profile, purity and packaging gates remain open.
