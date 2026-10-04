# Search contract

Profiles: wb31 and morphos320. Goal steps: CC01 and CC11. Recorded: 2026-09-02.

Status: partial MorphOS release-source grammar, a captured Workbench Search
binary identity, and bounded native entries for both profile candidates; no
packed-binary correspondence or shipping claim. The inspected official 3.20
`c/search/search.c` source member has SHA-256
`526bef28d0e1e4bf2bac2f2dc6a0ed889360046a78d4c55fc87e77fa44118179` and
defines this DOS ReadArgs template:

```text
FROM/M,SEARCH/A,ALL/S,NONUM/S,QUIET/S,QUICK/S,FILE/S,PATTERN/S,CASE/S,LINES/N
```

The current-source CC11 resident entries were refreshed on 2026-09-27.
MorphOS Search passes 46 supplied vectors per CPU in
`artifacts/cc11-search-morphos-native-20260927-source-file-errors-v1/qualification.json`;
the separate Workbench syntax candidate passes 19 per CPU in
`artifacts/cc11-search-wb31-native-20260927-source-error-regression-v1/qualification.json`.
The MorphOS fixture covers locale-library ownership, default-locale failure,
non-ASCII literal folding, source-observed locale classification, nested and
sibling `ALL` traversal, per-directory
`NameFromLock`/`AddPart` paths, soft links to files/directories and dangling-link
warnings, FILE/QUIET/QUICK suppression, QUICK presentation and CTRL-D, plus
locale-classified control delimiters with TAB preserved, locale-printable high
bytes, LF-only line-number changes, and numbered context output in both literal
and DOS-pattern modes. `WriteLine`
now selects the `>` or `:` marker before its DOS formatting call so the native
vector cannot overwrite the marker state. The inspected MorphOS source declares
`LOCALE_VERSION 38`, but the macro is unused: its active entry opens
locale.library v37. The candidate preserves that call and uses
`IsCntrl(locale, ch)` for line separators and `IsPrint(locale, ch)` for output
sanitization, preserving TAB as data.
Fixture-specific C1-control and high-byte printable classes exercise both
paths; exact classes and output still need original-guest confirmation.
Larger real DOS trees, exact original output, packed correspondence, guest
parity, PURE/resident admission, packaging, and differential gates remain open.
Workbench Search is now bound to the selected disk 2 binary: a 2,476-byte
HUNK with SHA-256
`b3e70309e954ea4a27d2e83a900bd749e59f3759e4bb1f8cb1e6943646f7bf43`.
Its primary embedded template candidate is
`FROM/M,SEARCH/A,ALL/S,NONUM/S,QUIET/S,QUICK/S,FILE/S,PATTERN/S`, an eight-slot
profile without MorphOS's `CASE` and `LINES`. A second embedded
`SEARCH/A,PATTERN/S,NONUM/S` string remains unclassified. Version and diagnostic
string offsets are recorded in
[`search-wb31-binary-audit-20260923.json`](../reference-captures/search-wb31-binary-audit-20260923.json).
These are static candidates; they do not establish runtime parser use or
behavioral parity.

| Profile | CPU | Resident HUNK | SHA-256 | Reachable methods | Supplied vectors |
| --- | --- | ---: | --- | ---: | ---: |
| MorphOS 3.20 | 68000 | 48,580 bytes | `0ac0fad6eedda759f056ed178047415daecbaefaf50025247a1f1b4d5bc0d168` | 43 | 46 |
| MorphOS 3.20 | 68020 | 48,716 bytes | `39893e3f733ed284cd5fb64472602fd30260d14d4ed62ca9c2fdf1a6af731079` | 43 | 46 |
| MorphOS 3.20 | 68040 | 48,468 bytes | `765da086988dd06c443d430fc9508239f964523f67fd19ffacecdd1f9b493d22` | 43 | 46 |
| Workbench candidate | 68000 | 48,580 bytes | `651c962874c650f6196c11943c7780a44c8cc76d2b7640e587ec379c5a8cd254` | 43 | 19 |
| Workbench candidate | 68020 | 48,716 bytes | `d2da563c9dd118927548da3d880d38bd2e8ca654764aa16af13f23b76fb577a0` | 43 | 19 |
| Workbench candidate | 68040 | 48,468 bytes | `b0da6784562a98909a5a75a53d67ed600da86d58e8f320dc09df6eee2087f697` | 43 | 19 |

This establishes source-observed 3.20 options, including CASE and LINES. Source
inspection further observes these candidate behaviors, still awaiting packed or
runtime confirmation:

- Search begins at every `FROM/M` element, defaulting to the current directory
  when FROM is omitted, and uses `MatchFirst`/`MatchNext`/`MatchEnd` with
  Ctrl-C as its anchor break mask.
- `ALL` recurses; directory headings are suppressed by FILE, QUIET, and QUICK.
  `FILE` applies an AmigaDOS pattern to file names. `PATTERN` wraps the supplied
  SEARCH value in `#?` on both sides before applying it to each line.
- MorphOS `FindString` caps its initial buffer at 512 KiB plus the NUL byte and
  retries failed initial allocations by halving the request. If that buffer is
  no larger than the file, it pre-scans LF-delimited lines, grows the buffer to
  the longest line, seeks back, then processes complete lines and rewinds an
  incomplete trailing line before the next read. The resident candidate now
  models this path with `MEMF_ANY`, including CTRL-D polling during the pre-scan.
  For DOS 51.28+, the resident candidate reads `Size64` and uses `Seek64` for
  files above the classic 32-bit offset limit, including signed relative
  rewinds and error handling. Supplied vectors cover the D0:D1 return pair and
  a synthetic large-file size; behavior with real >2 GiB files and handlers is
  still unverified.
- Literal matching uses a prefix table and locale-aware uppercase conversion
  when CASE is clear. The active source opens locale.library version 37 and
  the default locale, then uses `ConvToUpper`; its `LOCALE_VERSION 38` macro is
  unused.
  MorphOS line splitting uses `IsCntrl`, except TAB remains data, and output
  sanitization uses `IsPrint` before replacing nonprintable bytes with dots.
  Displayed line numbers advance only for LF.
  Matched lines have a six-character line-number prefix unless NONUM is set.
  QUIET stops after the first match in a file; `LINES/N` prints following
  lines; QUICK uses a compact full-path form.
- A complete successful traversal clears IoErr. A remaining Ctrl-C becomes
  `ERROR_BREAK` after cleanup; nonzero IoErr prints a fault and returns FAIL.
  A found object returns OK; the default result is WARN.
- In the inspected MorphOS source, per-file Open, Read, and Seek failures are
  misses: traversal continues, and successful completion clears IoErr. During
  `ALL`, a dangling soft link is warned about and skipped without making the
  command fail. The MorphOS resident vectors now exercise these source-shaped
  outcomes, including warning status and cleared IoErr. Workbench retains its
  separate syntax-candidate behavior and is not inferred from MorphOS source.

`SearchLiteralMatcher` is an independent, pure caller-buffer primitive for a
nonempty literal span. It accepts explicit guest ranges, does not allocate or
write, and supports byte-exact CASE plus an ASCII-only no-CASE fallback. It is
not an implementation of the source's locale behavior, pattern grammar,
line splitting, traversal, output, signal handling, or command lifecycle.

The private `Commands.SearchNativeRoot` compiles that primitive through an
explicit control block into resident HUNKs with no managed runtime features,
helpers, external targets, exception regions, fatal fault sites, or image
writes. The [native qualification script](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_search_literal_native.ps1)
rebuilds the compiler and runs nine direct supplied vectors per CPU, including
CASE/no-CASE, nonmatch, rejected empty pattern, and repeat/interleaved calls.
Its current receipt is 1,060 bytes on 68000, SHA-256
`ebcc092a41db61d8bff6af53f4363e44ed72287d2a6236617a7a954c4a4bced8`;
1,084 bytes on 68020, SHA-256
`2646773899a00e82a409422f620c1bbdd81e06a50a8a78b15086a15ba07c8c6a`;
and 1,052 bytes on 68040, SHA-256
`4135553b44c5d407394ebb73b7d05ece95ef272f1f9400d76b2a125b7011899f`.
This is a direct primitive probe, not a Search command entry or a DOS/reference
equivalence test.

`SearchLineFormatter` adds the bounded literal line stage: it recognizes the
source-observed ASCII line terminators, renders selected matches with the
six-column number plus `>`/`:` marker, honors NONUM, QUIET, and following-line
selection, and replaces nonprintable output bytes. Its no-CASE behavior remains
ASCII-only. Five host vectors pass, and its private 68000 root currently passes
the static resident gate (19 reachable methods; no runtime feature, helper,
external target, exception region, or fatal fault site). The [line qualification script](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_search_line_native.ps1)
now runs ten direct supplied vectors per CPU, including matching/following,
NONUM, QUIET ASCII no-CASE, control bytes, nonmatch, short output, and
repeat/interleaved callers. Its resident receipt is 4,284 bytes on 68000,
SHA-256 `60dc3cc0a53f36d8bc315cf42e5d09b5cf93cfb532670c968de12356147eeab0`;
4,368 bytes on 68020, SHA-256
`17dd86fd4bd26d9694735ea13cd073d4c7e20b9713567757e5456b672f1682ab`;
and 4,264 bytes on 68040, SHA-256
`8609c7d259827547d1c032816d163866ca5b86a1fa06fa477389f1e4ae84d606`.
This remains a direct line-stage probe, not a Search command entry, locale
implementation, DOS traversal, or reference-equivalence test.

## Bounded MorphOS native entry

`NativeMorphOSSearchCommand` and the resident
`NativeMorphOSSearchEntry` now bind the source template to the public DOS
boundary. The entry calls DOS 37 `ReadArgs`/`FreeArgs`, owns its parser result
and workspace with `AllocMem`/`FreeMem`, traverses `FROM/M` paths with
`MatchFirst`/`MatchNext`/`MatchEnd`, opens files through `Open`/`Read`/`Close`,
and renders through `Output`, `VPrintf`, `FPuts`, and `Write`. MorphOS also
opens locale.library v37 and the default locale, uses `ConvToUpper` for
case-insensitive literal matching, `IsCntrl` for source-compatible line
delimiters, and `IsPrint` for output sanitization, then closes both owned
handles. MorphOS also
classifies `ALL` soft-link entries through `CurrentDir`, `Lock`,
`Examine64`/`Examine`, `GetDeviceProc`, and `ReadLink`: it descends into
directory links, reads file links as files, and warns on dangling links unless
`FILE`, `QUIET`, or `QUICK` suppresses the warning. All temporary link and
warning storage belongs to the invocation workspace. The supplied stage covers
literal and DOS-pattern line matching, `FILE`, `PATTERN`, MorphOS `CASE`,
`NONUM`, `QUIET`, MorphOS `LINES/N`, locale and soft-link cleanup, empty
matches, parser/allocation/read failures, CTRL-C, CTRL-D current-file
abandonment, and CLI/Workbench startup boundaries.
MorphOS `QUICK` prints each file's full path followed by the source's
clear-line/carriage-return sequence before opening it. The first matching line
starts after a newline; normal match and context lines retain their numbering
unless NONUM is set, while QUIET suppresses those lines. A final clear-line
sequence is emitted after traversal. CTRL-D abandons the current file, prints
`** File abandoned`, and continues traversal; CTRL-C remains a command break.
The supplied vectors cover matching, no-match, QUIET, recursive, dangling-link,
and file-abandonment cases; original-console behavior is still awaiting guest
comparison. QUICK presentation on the Workbench candidate remains unqualified.

The resident qualification receipt
[`search-morphos-native-20260912-qualified/qualification.json`](D:/Koodit/GIT/CopperOS/artifacts/search-morphos-native-20260912-qualified/qualification.json)
passes seventeen supplied invocations per CPU (51 total). The 68000/68020/68040
HUNKs are 15,156/15,308/15,140 bytes with SHA-256
`a19affbd8a6a94c99653fff1434b93fd2fd6e474db8c010081ae5e99d1bfe869`,
`290587c2fca16b5490967bbb4b2a8ef1b910ce97e0bcc89ac3e57c9937408775`, and
`f15b38a30a7fdaa4090cb5f60b54dd454601778cd29e72f9a29cf080ca9bdd50`.
The static gate reports 24 reachable methods per CPU and no managed allocation
sites, runtime features/helpers, external native targets, exception regions,
fatal fault sites, or shared-image writes.

This is bounded native evidence only. The current MorphOS three-CPU receipt is
[`cc11-search-morphos-native-20260927-source-file-errors-v1/qualification.json`](D:/Koodit/GIT/CopperOS/artifacts/cc11-search-morphos-native-20260927-source-file-errors-v1/qualification.json)
and passes 46 supplied invocations per CPU. It includes the earlier locale,
soft-link and traversal cases, plus a 512 KiB `MEMF_ANY` buffer, allocation
halving and exhaustion, short reads during the LF-only maximum-line pre-scan,
buffer growth and rewind/reread for a 524,300-byte file, and CTRL-D during that
pre-scan, failed-first-allocation recovery, incomplete-line rewind after a
small halved buffer, and `Seek64` absolute/signed-relative and failure cases.
The fixture's high-byte classes exercise the IsCntrl/IsPrint paths
but do not establish the real default locale table. The refreshed Workbench
candidate passes 19 vectors per CPU in
[`cc11-search-wb31-native-20260927-source-error-regression-v1/qualification.json`](D:/Koodit/GIT/CopperOS/artifacts/cc11-search-wb31-native-20260927-source-error-regression-v1/qualification.json),
including a matching line over 8,192 bytes written through bounded DOS calls.
These receipts do not prove real >2 GiB filesystem behavior, exact original
guest output, or Workbench runtime grammar.
The Workbench entry uses the captured eight-slot syntax candidate; its binary
strings bind identity and syntax candidates, not runtime behavior.
Packed correspondence, complete filesystem/output/error parity, original
guest comparison, PURE/resident lifecycle, licensing, package admission, and
differential gates remain open.

## Bounded Workbench 3.1 syntax candidate

`NativeWorkbench31SearchCommand` passes the captured eight-slot candidate to a
separate `Workbench31SearchEntry` that opens DOS 36. Its bounded body uses the
common options, omits MorphOS-only CASE and LINES results, and exercises the
same supplied AnchorPath directory descent, pattern, file-I/O, line-output,
failure, Ctrl-C, and startup-boundary fixture. The binary identity and syntax
are now bound, but runtime option behavior, exact Workbench output, locale
handling, and original guest parity remain open.

The [2026-10-04 size receipt](../size-reductions-20261004.json) measures the
shipping Workbench profile with compile-time MorphOS feature selection.
With the same clean compiler/SDK and fixed-point peephole mode, its MC68000
HUNK shrinks from 20,680 to 10,840 bytes. Nineteen supplied invocations per CPU
match baseline output, result, IoErr, allocations, cleanup and DOS call order
on 68000/020/040; the shared MorphOS build separately passes 46 cases per CPU.
This receipt does not close the original guest comparison.

The refreshed three-CPU resident receipt is
[`cc11-search-wb31-native-20260927-source-error-regression-v1/qualification.json`](D:/Koodit/GIT/CopperOS/artifacts/cc11-search-wb31-native-20260927-source-error-regression-v1/qualification.json)
and passes nineteen supplied invocations per CPU (57 total). HUNKs are 48,580,
48,716, and 48,468 bytes on 68000/020/040, with 43 reachable methods per CPU
and no managed allocation sites, runtime features/helpers, external native
targets, exception regions, fatal fault sites, or shared-image writes. This is
supplied-vector evidence only; exact runtime grammar/output, locale, deep
traversal, guest parity, package identity, PURE/resident lifecycle, licensing,
and differential gates remain open.

The bounded entry does not prove packed-binary correspondence, Workbench 3.1
grammar, complete matching/recursion/output bytes, source error and
cancellation parity, or source reuse rights. Any follow-up body must retain
DOS ReadArgs/FreeArgs and public DOS traversal/I/O ownership. MorphOS must
retain locale.library folding; Workbench behavior must follow original guest
and binary evidence rather than assumed MorphOS extensions.
