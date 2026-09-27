# MakeDir contract

Command/profile IDs: `CC12.MakeDir.wb31`, `CC12.MakeDir.morphos320`.
Goal steps: CC01, CC12. Recorded: 2026-08-30.

Status: **Workbench 37.2 CLI control flow and arguments verified against the
original binary and NDK. Actual original and generated instructions now pass
38 supplied-vector comparisons per CPU on 68000/020/040, plus one generated-
only allocation-failure case per CPU. Real DOS/parser/filesystem launches and
full replacement qualification remain open. MorphOS 50.4 has verified media
identity only.** A separate Workbench 3.1 resident entry now passes a bounded
supplied-public-DOS fixture on all three CPU targets; this is native ABI
evidence only and does not close the original guest or shipping gates. The two
profiles must not be conflated.
The new classic body is independently written from this behavioral contract,
not a translation or inclusion of original executable bytes. No original
binary, disassembly listing or vendor implementation is copied into the repo.

## Original identities and evidence confidence

| Profile | Pinned member | Evidence |
| --- | --- | --- |
| `wb31` | `Workbench3.1:C/MakeDir`, file header block 336 on selected Workbench disk; same bytes at `Install3.1:C/MakeDir`, header block 789 | 464 bytes, SHA256 `23911db49742055d8bddcfe5f8de82cbb2232f8a7ef850d51bfd27f9b54c819b`; readable tag `makedir 37.2 (5.4.91)` at file offset 363. Both member hashes rechecked from complete hash-verified ZIP/ADF inputs. |
| `morphos320` | `MorphOS/C/MakeDir`, ISO block 176176, block size 2048 | 1982 bytes, SHA256 `788749f0c8d4bdc2896505f4f1a23047371236593b9af47241f9a376bf10f685`; readable tag `MakeDir 50.4 (27.11.04)` at file offset 1945. Packed `7f4d4f53` payload; code and argument template not decoded or executed. |

Exact archive/image identities, original placements and protection evidence
are in the [inventory](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/command-inventory.json)
and [media evidence](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/media-evidence.json).
Private Workbench inputs are the two selected ZIPs under
`D:/TestData/TestImages/`; the MorphOS input is
`D:/TestData/MorphOSReferences/morphos-3.20.iso`.

Both inspected Workbench file headers have protection word zero, including
P clear. Installed metadata remains unobserved; the profile's inventory
classification is `unresolved-not-nonpure`. MorphOS has an observed P addition
in `hdinstall.fixc:42`, so that profile is required pure; final installed flags
and replacement purity are still unqualified. Neither a small binary nor
apparently immutable code is sufficient evidence to set P.

The Workbench member has one 428-byte HUNK_CODE, including literals, beginning
at file offset 32. It has no data/BSS/relocation hunks. Capstone decoded the
68000 instructions read-only in memory. Addresses below are **code-relative**;
add 32 to obtain a file offset. `B` denotes direct binary/control-flow evidence,
`A` the primary API contract, and `R` a runtime capture. This record contains
B and A evidence, with no R command outcomes.

## NDK authority

All entries below are read-only members of the private
[AmigaDeveloperCD.iso](D:/TestData/AmigaDeveloperCD.iso); ISO block size is 2048.
NDK paths start `NDK_3.1/`. These are primary API references, not evidence that
an original command has been executed successfully.

| Member | Block; bytes | SHA256 |
| --- | --- | --- |
| `DOCS/DOC/DOS.DOC` | 17730; 174666 | `2e22d1d1b7c00fac5757eead0ef4d49a01c7666f0db5850735193c9597a7cee2` |
| `Includes&Libs/fd/dos_lib.fd` | 20705; 5765 | `a9b24c1d9fb1053955dfb28eb8dabe92eff5dbde10b952e67d6cee2a80c5c228` |
| `Includes&Libs/fd/exec_lib.fd` | 20709; 5721 | `4da0ae2a91d0758696e0d1f72f8bc748321b5fd14e7e31d8215cda8e002348c2` |
| `Includes&Libs/include_h/dos/dos.h` | 21043; 10860 | `7791a911cab18de7aa5b5e4a818d5e09439c8c16aa7b029ea18a12ada000b8c3` |
| `Includes&Libs/include_h/dos/dosextens.h` | 21052; 16933 | `d65038a5297ffa91e3ab1ba063dca102580de63b14c071a1158d4e9ecdfb2f50` |
| `Includes&Libs/include_h/exec/execbase.h` | 21101; 7113 | `f1eeb80669d9e96075aae4736c61ecb77640f8ac1d4b5a358da4a53219f6c240` |

Relevant DOS.DOC entry offsets are CreateDir 22694, FreeArgs 69792, Lock 89765,
PrintFault 119073, ReadArgs 121650, SetIoErr 146325, UnLock 165294 and VPrintf
170318. The FD maps the exact called vectors: Lock -84, UnLock -90, CreateDir
-120, IoErr -132, SetIoErr -462, PrintFault -474, ReadArgs -798, FreeArgs -858
and VPrintf -954. Exec OpenLibrary is -552 and CloseLibrary is -414.

## Workbench CLI arguments and launch boundary

The verified template at file offset 356 / code offset `$144` is:

```text
NAME/M
```

The binary zeroes one LONG result slot, passes its address and a null RDArgs
to ReadArgs at `$48`, then consumes its returned string-pointer vector. This
is a verified use of the template, not merely a printable-string candidate.

`NAME` accepts multiple strings under normal DOS ReadArgs rules. It is neither
keyword-only nor marked `/A`. A missing list is rejected by command logic
after successful parsing; adding `/A` would change its diagnostic/error path.
Use real DOS handling for positional/named values, quotes, star escapes,
keyword case and interactive `?` continuation. There are no ALL, PARENTS,
QUIET or other switches in this template. Option-like strings can be names
rather than invalid switches; exact lexical cases need parser/reference
fixtures. No MatchFirst/MatchNext, pattern parser or private wildcard expansion
appears in the binary. Handler-specific filename validity is a DOS concern.

An absent list is a zero slot. A present vector whose first pointer is zero
is a different state: it succeeds without doing work. Whether normal original
ReadArgs can produce that second state for any input remains unobserved, but
the body must preserve the distinction and a provider fixture must test it.

Original startup opens `dos.library` with minimum version 36 (`$18-$1E`).
Failure returns 20 and directly writes 122, ERROR_INVALID_RESIDENT_LIBRARY,
to the current Process secondary result (`$11C-$12C`), without DOS output.
The command has no explicit CLI/Workbench test or startup-message handshake
in this HUNK. That absence does not establish safe Workbench launch behavior;
the launcher/process lifecycle must be observed separately.

The new body deliberately implements neither branch of library acquisition
nor Workbench handling. Its preconditions are a prepared CLI input and a live,
correct invocation-owned `DOS.DOSLibraryBase`, with the normal Exec context
available for the argument lease. A later native entry owns version 36 open,
missing-library result handling, library close and any verified launch policy.
Do not silently adopt the generic Workbench startup helper as original MakeDir
behavior without resolving that gap.

## Workbench operations, output and error selection

For each name, in original vector order:

1. Call `Lock(name, ACCESS_READ)` (`$86-$8E`). ACCESS_READ is -2; it obtains a
   shared lock for either an existing file or directory. A successful Lock
   does not distinguish the object type.
2. If Lock succeeds, select return 10, clear the saved secondary error to
   zero, print the existing-name diagnostic and UnLock that lock. Do not call
   CreateDir on this path (`$98-$AE`, `$DC-$E2`).
3. If Lock fails for any reason, call CreateDir with the same name. The binary
   does not inspect Lock's IoErr to decide whether creation is permitted.
   CreateDir returns an exclusive BPTR lock on success; UnLock it once.
4. If CreateDir fails and return 10 has not already been selected, capture
   IoErr immediately and select 10. If return is already 10, retain the saved
   error instead of querying this failure's IoErr. Print the creation-failure
   diagnostic in either case (`$BE-$D8`).
5. Continue to the next name. There is no rollback of created directories,
   transaction, explicit break polling or command-side parent creation.
   Ordered inputs such as parent followed by child may therefore differ from
   the reverse order; actual filesystem outcomes need R fixtures.

There are three exact command-owned message formats, ending in LF:

```text
No name given\n
%s already exists\n
Can't create directory %s\n
```

The notation `\n` above represents one byte 0A, not a literal backslash.
Names are inserted as strings, without added quotes. Successful creations
print nothing. VPrintf uses the current buffered Output stream; no separate
stderr channel or stream close is requested. Its result is ignored, so an
output error does not independently change the selected return/error. DOS
PrintFault supplies parser or final saved-error text with a null header;
exact DOS/localization text and failed-output behavior require capture.

After a successful parse, all names finish before FreeArgs. The binary then
calls SetIoErr(saved error), conditionally calls PrintFault for a nonzero saved
error, closes DOS and returns the selected level (`$F2-$136`). NDK PrintFault
sets IoErr to its supplied code, as well as printing to default Output. Parser
failure instead prints the parser's IoErr, skips FreeArgs and returns 20.

| State or sequence | Selected return / secondary error (B + A; R pending) |
| --- | --- |
| DOS cannot open | 20 / 122; outside new body |
| ReadArgs fails with error E | 20 / E; one PrintFault, no successful RDArgs to free |
| Parsed NAME slot is null | 20 / 0; missing-name message, FreeArgs, SetIoErr(0) |
| Parsed vector exists but starts with null | 0 / 0; no name operations or messages |
| Every requested directory is newly created | 0 / 0; release each returned lock |
| Any existing file/directory, no creation failure | 10 / 0 |
| Creation failure E, then successful creation | 10 / E |
| Creation failure E, then creation failure F | 10 / E |
| Creation failure E, then any existing name | **10 / 0** |
| Existing name, then creation failure F | **10 / 0** |
| Failure E, existing name, failure F, success | **10 / 0**; creation failures still print their per-name messages |

The existing-name error reset is intentional compatibility behavior in this
body. Do not replace it with maximum severity, the last DOS error, or a generic
ERROR_OBJECT_EXISTS. An earlier return 10 also suppresses later error capture
when the saved error has been reset to zero.

## Body API, ownership and implementation limits

[Workbench31MakeDirCommand.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/Workbench31MakeDirCommand.cs)
provides `public static int Run(out int ioError)`. It reads DOS input, releases
its own argument lease and locks, sets the selected DOS secondary error and
returns that error separately for the caller to preserve during final cleanup.
It never opens/closes DOS, changes CurrentDir, receives/replies to messages,
closes borrowed Output or changes the original vector/string bytes.

The existing [NativeCommandArguments](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeCommandArguments.cs)
owns a checked Exec allocation for the zeroed result slot and the returned
RDArgs. Unlike the original stack slot, this helper adds an allocation point.
Its failure must be reported as 20 / ERROR_NO_FREE_STORE (103), with no leak;
it is an explicit safe implementation difference needing a native failure
fixture. Parser failures always map to MakeDir's return 20, even though the
shared helper normally reports return 10 for them. An impossible failure to
retrieve slot zero from a successful one-slot lease is defensively rejected
as 20 / ERROR_BAD_TEMPLATE (114), not treated as a missing name.

Each live `/M` vector cell already holds the one LONG pointer needed by `%s`.
VPrintf borrows that cell directly, without an allocated format array, stack
address conversion, mutation or extra filename copy. Keep RDArgs alive through
all output and lock operations, then release it before the caller closes DOS.
The body trusts DOS's valid aligned, terminated `/M` vector and string memory
contract; it is not a parser for arbitrary untrusted guest pointer arrays.

No host filesystem, managed parser, runtime array, mutable static state or
replacement DOS implementation is part of this source. Literal strings are
native CString inputs; their lowering is exercised by the accepted vector
fixture below. The body has no entry attribute and is not a shipped command.
The original image's apparent read-only layout is not a proof of reentrancy,
minimum stack or resident purity for either artifact. Follow the current
[compiler policy](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/compiler-qualification.md)
and [argument ownership](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/argument-ownership.md).

## Finite fixture matrix

All rows were pending at contract creation. The accepted vector fixture below
now covers bounded control-flow/output/ownership variations, using expanded
case IDs. It does not close rows requiring actual DOS parsing, a handler or
normal CLI input. Static expectations and supplied vector values are kept
separate from executed original library behavior.

| Fixture ID | Input / injection | Required outcome or remaining observation |
| --- | --- | --- |
| MK31-A01 | Original ReadArgs on an empty command line, NAME slot null | Exact missing-name bytes; 20/0; one successful RDArgs release and no Lock/CreateDir |
| MK31-A02 | Provider returns a nonnull vector containing only a terminator | 0/0, no output/name calls; do not label this normal parser behavior without reference evidence |
| MK31-A03 | ReadArgs failure E, including malformed quotes and allocation failure inside DOS | 20/E; PrintFault(NULL header); no FreeArgs for failed parse and no result-slot leak |
| MK31-A04 | Multiple positional names and named NAME values | Original parsing/keyword precedence; preserve vector order and null termination |
| MK31-A05 | Quoted spaces, empty string, star escapes and keyword-like name | Capture exact argument bytes and DOS path results; no host normalization |
| MK31-A06 | `?` followed by continuation, EOF and cancellation | Original template/help/input cursor, return/error and argument ownership |
| MK31-A07 | Names resembling ALL, QUIET, PARENTS, `-p`, or patterns | No invented switches or command-side expansion; capture filesystem acceptance separately |
| MK31-B01 | One then several new directories | 0/0; no success output; one CreateDir and one UnLock per successful creation |
| MK31-B02 | Existing directory and existing regular file | 10/0, exact diagnostic, Lock/UnLock only; no CreateDir or object-type probe |
| MK31-B03 | Same new name twice | First creation persists; second existing-name behavior, 10/0 |
| MK31-B04 | Parent then child; child then missing parent | Correct ordered DOS calls and retained successes; no implicit parent traversal or rollback |
| MK31-E01 | CreateDir failure E then success | 10/E; first failure captured before diagnostic; all remaining names attempted |
| MK31-E02 | CreateDir failures E then F | 10/E; no replacement by F; per-name messages and one final PrintFault(E) |
| MK31-E03 | Creation failure E then existing name | 10/0; saved error cleared, no final PrintFault |
| MK31-E04 | Existing name then creation failure F | 10/0; F not captured; creation-failure diagnostic still emitted |
| MK31-E05 | Failure E, existing, failure F, success | 10/0 and complete ordered resource/message trace |
| MK31-E06 | Lock fails for access/device/other error, CreateDir succeeds or fails | Always attempt CreateDir; creation result, not Lock failure, controls error selection |
| MK31-E07 | Race between failed Lock and CreateDir; race after successful Lock | Follow public call results and release the exact owned locks; no implicit transaction |
| MK31-E08 | VPrintf/PrintFault output error or short output | Preserve selected result/error policy; record actual DOS diagnostics and final IoErr |
| MK31-E09 | Shared result-slot allocation failure or rejected allocation span | Safe 20/103, no DOS ReadArgs and no leaked allocation; implementation-specific acquisition case |
| MK31-E10 | Post-parse ownership invariant failure at result access | Safe 20/114 and full lease cleanup; fault injection only, not original user syntax |
| MK31-R01 | Every success/failure path plus repeated invocation | Each acquired Lock/CreateDir BPTR unlocked once, RDArgs/result allocation freed once, borrowed streams/library untouched |
| MK31-R02 | Shared native image with interleaved invocations and differing argument storage | No image writes or cross-invocation pointers/errors; retain exact argument/format data until the corresponding FreeArgs |
| MK31-R03 | Long names/list, final vector terminator, native 68000/020/040 | Bounded memory ownership and measured stack; correct APTR/BPTR and format-vector ABI, no host implementation inside production closure |
| MK31-L01 | Original library open failure and version gate | Observe 20/122 process result, no DOS use; qualify later entry separately from body |
| MK31-L02 | Original Workbench launch and startup-message ownership | Establish actual supported/refused behavior; no assumed message policy or pass from generic helper tests |
| MK31-L03 | CLI entry with original and generated real DOS/ReadArgs/filesystem | Full command stdout/return/IoErr, directory effects and resource lifetime differential; vector stubs alone do not close this row |
| MK50-C01 | Exact MorphOS 50.4 help/template and primary code/source evidence | Establish independent option/behavior matrix; do not reuse NAME/M or classic error selection by assumption |
| MK50-C02 | Installed flags and shared-image execution for MorphOS | Preserve observed required P design and prove replacement behavior; ISO POSIX mode is insufficient |

Every receipt records command/profile and member hash, argument bytes, DOS/
filesystem identity, injected call outcome, ordered calls, stdout/stderr bytes,
return and final IoErr, directory state and live resource counts. Original
command media must stay outside the repository; filesystem tests use only
disposable guest directories/devices. Actual library code versus intercepted
vectors must be identified explicitly in the receipt.

## Accepted native/reference vector fixture

The private [native executor](D:/Koodit/GIT/CopperOS/tests/Commands.MakeDirNativeExecution/README.md)
executes the pinned original and the unchanged independent body through
`Workbench31MakeDirProbe.Main`. The
[qualification receipt](D:/Koodit/GIT/CopperOS/tests/Commands.NativeRoot/bin/Release/net10.0/qualification-makedir/012c05cf6707457a9f288e8b862dbcf4/qualification.json)
has SHA-256 `e59c8ed1d4f9e2f7a1e7186f301a3feb3552e503e66ef8a1c6152d8d66cbc266`;
its input manifest is
`65892f5a53c626207c71934e882cf46453ca5340642e90f2314732c70689c559`.
All 25 stages and 27 input checks passed, binding 442 source/settings files,
20 copied binaries, 35 restore files, 196 host/runtime files and the single
private original by hash. The original bytes are never copied into the snapshot.

| CPU | Generated HUNK bytes | Original / generated invocations | Comparisons | Generated HUNK SHA-256 |
| --- | ---: | ---: | ---: | --- |
| 68000 | 1876 | 38 / 39 | 38 | `6494fe6888cc46350d47a3b2c32161c55d7d5be3084063f0f3ca618b61ca407f` |
| 68020 | 1908 | 38 / 39 | 38 | `2c7cbc56a62e7b1d162d94da9d4d4fb1016938d465538226a75e0d73a269f10d` |
| 68040 | 1884 | 38 / 39 | 38 | `7b9484832872620796e40fc91a97655284efdeb96171cba08b666a336bc8cf3a` |

Totals are **114 original invocations, 117 generated invocations and 114
comparisons**. Each side/CPU loads one protected image and exercises sequential
and instruction-interleaved callers. All three generated images reproduce
identical bytes. The extra generated-only case is
`MK31-E09.generated-only-slot-allocation-failure`; it does not inflate the
comparison count. The selected return/IoErr, command-owned VPrintf bytes,
PrintFault requests, semantic call order and exact resource ownership match.
VPrintf/PrintFault are supplied vectors, not original DOS formatting. Returned
ReadArgs lists and filesystem call outcomes are also explicit fixture inputs.

The original entry ABI permits all registers except SP to change. Each case
requires restored entry SP, intact guards and image, correct public-vector A6
and argument/clobber behavior, and complete resource cleanup. Other register
preservation remains a recorded observation. The configured 4 KiB/16 KiB stacks
show peak writes of 80 bytes for the original and 140 for the generated entry;
these are observed extents, not qualified minimum shipping stacks. No exported
library/callback register contract is relaxed by the image-entry rule.

The production closure has no managed allocations, runtime helpers, external
native targets, exception regions, fatal-machine-fault sites or shared writable
RAM/BSS. Only the public SDK's exact BPTR nullable HasValue/Value intrinsics are
admitted; they lower to native pointer tests/loads. The actual host executor is
`a0243315724df60d498303199811ddbda17dcfb550e12e4ca75c6cac11b6c51f`.
Its package SDK identity is recorded separately from the native build's local
SDK; the qualifier checks the DLL actually loaded by that executor.

This advances bounded native/differential evidence for the classic body. It
does not pass its complete command/profile gates, observe real directory
effects, qualify original parser/help behavior, or permit installation/P bits.

## Workbench resident entry candidate

`Workbench31MakeDirEntry` owns the observed DOS 36 open boundary, direct
`pr_Result2` publication when DOS is unavailable, Workbench-startup rejection,
argument-boundary checks, body invocation and final cleanup. Its resident HUNK
uses raw BPTR DOS calls so the production closure has no SDK nullable-value
runtime feature.

The independent receipt is
`artifacts/makedir-wb31-native-20260917-runtime-v2/qualification.json`.
The 68000/020/040 HUNKs are 2,356/2,388/2,356 bytes with nine reachable
methods and zero managed runtime features, helpers, external targets,
exception regions, fatal fault sites or shared-image writes. Fourteen supplied
public-DOS invocations pass per CPU, covering the NAME/M result slot, empty and
multiple vectors, existing/create/failure ordering, command-owned messages,
parser/allocation failures, startup boundaries and interleaved callers. Packed
correspondence, original guest behavior, full filesystem effects, installed
PURE/resident lifecycle, packaging and differential parity remain open.

## 2026-09-17 - MorphOS source-informed body

The available 3.20 source release contains an independently readable MakeDir
50.4 implementation (`c/makedir/makedir.c`, 6,373 bytes, SHA-256
`156d1aabc2dca11c546ef46714e7749f7573a6577a3364a11e58e1d03d722202`). Its
verified grammar is `NAME/M,ALL/S`; the source creates each name in order,
returns the last operation level, and implements `ALL` by walking DOS path
components with `Lock`, `CreateDir`, `Examine`, `ChangeMode` and `CurrentDir`.
The replacement in
[`NativeMorphOSMakeDirCommand.cs`](D:/D-drive/GIT/CopperOS/src/Commands/Native/NativeMorphOSMakeDirCommand.cs)
is an independent public-DOS implementation of that contract. It keeps the
ReadArgs lease, mutable path bytes, FileInfoBlock, current-directory restore,
and all errors invocation-local; the private DOS 37 entry uses resident
runtime startup and no host filesystem.

The three-CPU resident receipt is
`artifacts/makedir-morphos-native-20260917-runtime-v10/qualification.json`:
68000/020/040 HUNKs are 4,196/4,244/4,196 bytes with 13 reachable methods,
zero managed runtime features, helpers, external targets, exception regions,
fatal fault sites and shared writable storage. Fifteen supplied public-DOS
invocations pass on each CPU, covering result ownership, ordinary and `ALL`
operation order, current-directory restoration, parser/allocation failures,
diagnostics, startup boundaries and interleaved callers. This remains
source-informed native evidence; the packed 50.4 binary's exact
parser/output/diagnostics, handler behavior, recursive edge cases, guest
parity, installed P/resident lifecycle and package admission remain open.

## First original-DOS command input comparisons

A separate 68000 component run launches the unchanged original 37.2 and the
unchanged generated 1,876-byte HUNK through original DOS RunCommand. It makes
two original and two generated invocations, with two exact input-edge
comparisons. ReadArgs receives D3 NULL and parses normal Input rather than
supplied result slots. LF-only returns 20/IoErr 0 with an absent /M list,
one FreeArgs and a usage VPrintf request. Opening quote plus `alpha` and LF
(seven bytes) returns 20/IoErr 120, no FreeArgs and one PrintFault request.

The [launch receipt](D:/TestData/CopperOSCommands/BuildSnapshots/dos-makedir-command-host-20260830T150723Z-0095e8b1/results/private-original-dos-makedir-commands-receipt.json)
has SHA256 `ae22f088057f5d44b9b167192f08060bb680a334b6a99316e3b2fe7f79dd3394`.
The [compact observations](D:/TestData/CopperOSCommands/BuildSnapshots/dos-makedir-command-host-20260830T150723Z-0095e8b1/results/original-dos-makedir-command-compact.json)
have SHA256 `68089c60f4435d1d168d944479ec749fea80cffbd2bd668e3bce76c2f24230b6`.
Code, original vectors and argument bytes remain unchanged. The observed
task/SP, CLI, argument pointer and NIL: buffered state restore. Generated
result-slot allocation/free is one/one; the original needs no corresponding
command-owned allocation. There are no new exceptions or cleanup failures.

This closes the exact A01 pair in the [launch contract](../native-command-launch-contract.md).
The seven-byte quote variant is useful additional evidence, but the contract's
exact A03 input is opening quote plus `unterminated` and LF, fourteen bytes.
That exact original/generated pair now passes in a separate capture, without
repeating A01 or relabeling the existing receipt. It adds one original and one
generated invocation and one comparison: both return 20/IoErr 120, with failed
native ReadArgs, one PrintFault request and no FreeArgs. The
[exact A03 receipt](D:/TestData/CopperOSCommands/BuildSnapshots/dos-makedir-exact-quote-host-20260830T152136Z-86a5c8b1/results/private-original-dos-makedir-exact-a03-receipt.json)
is `42383f47cf3ef2a1f1c2738592245464ef47cea4f738da46bb796dfad573f591`;
its [compact observations](D:/TestData/CopperOSCommands/BuildSnapshots/dos-makedir-exact-quote-host-20260830T152136Z-86a5c8b1/results/original-dos-makedir-exact-a03-compact.json)
are `efea2a1f28f057faf0ac9cb04585fea362a2517ea3616d6b68ae3bec60d611ef`.
All ownership/state/code/vector/argument checks pass. Across these two receipts
there are three original and three generated calls, three comparisons, and
two distinct exact contract inputs plus the seven-byte quote variant.
Diagnostic requests are observed, not delivered stdout bytes.
NIL: and inherited SR `$2000` limit this to a component run. There is no
filesystem handler result, generated DOS provider execution, current-compiler
requalification or whole-command/purity admission.

## Remaining completion gates

The tested body and private CLI entry execute on all selected native CPUs;
normal input now has the bounded original-DOS observations above. The
[native command launch contract](../native-command-launch-contract.md) specifies
public CLI/Input/Output readiness and subsequent RunCommand/input restoration
cases. Broader original CLI/parser execution,
generated DOS input integration, real filesystem differential, Workbench policy,
installed selection/protection and package
entry qualification remain open. MorphOS 50.4 needs its own primary behavior
evidence and implementation/profile decisions. None of these gates is closed
by this Markdown contract, the static disassembly or the existence of the
new classic source body.
