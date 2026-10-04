# List contract

Profiles: wb31 and morphos320. Goal steps: CC01 and CC11. Recorded:
2026-09-12; progress updated 2026-09-23.

Status: partial provider contract plus bounded resident native evidence; no
packed-binary or shipping claim. The packed MorphOS 3.20 `List` member is
recorded by the inventory as 12,464 bytes, SHA-256
`ee61ff9a8ddc338bedd8feb274aed3e1da7a5065af439868d8160fc0a08aeadd`, version
50.23 (16.1.2022). The checked-in binary scan did not recover a reliable
MorphOS template string. The [MorphOS Library List reference](https://library.morph.zone/Shell_Commands/List)
records this 19-slot grammar:

```text
DIR/M,P=PAT/K,KEYS/S,DATES/S,NODATES/S,TO/K,SUB/K,SINCE/K,UPTO/K,QUICK/S,BLOCK/S,NOHEAD/S,FILES/S,DIRS/S,LFORMAT/K,SORT/K,USERS/S,GROUPS/S,ALL/S
```

The bounded native stage preserves that grammar and implements the public DOS
matcher path: `DIR/M`, `P=PAT/K`, `KEYS/S`, `DATES/S`, `NODATES/S`, `SUB/K`,
`SINCE/K`, `UPTO/K`, `QUICK`, `BLOCK`, `NOHEAD`, `FILES`, `DIRS`, `ALL`, and
`TO/K`.
`SUB` is a case-insensitive literal file-name substring filter, composed as an
escaped DOS pattern and applied alongside `P=PAT`; directories are excluded
from the substring test. `SINCE` and `UPTO` are parsed through public DOS
`StrToDate` with DOS date format, then compared inclusively against each
`FileInfoBlock` day stamp; filtering is date-only, not time-of-day. Invalid
date input currently returns `ERROR_BAD_TEMPLATE` and follows the normal
cleanup path in the candidate. This is fixture-qualified behavior, not yet
confirmed against the original command. `KEYS` adds the FIB disk key to each
row. Default rows now use the documented file size or `Dir` marker, eight
protection letters, DOS-formatted date/time, and optional colon-prefixed
comment. `NODATES` omits date and time columns and skips `DateToStr`; `DATES`
is accepted with the documented full-row default. `QUICK` prints names only,
including when `DATES` is also supplied, and skips date conversion. The
Workbench manual specifies that dates are the default unless `QUICK` is used;
the MorphOS command reference likewise defines `QUICK` as name-only output.
`DateToStr` receives the current FIB timestamp and a conversion failure follows
the normal error/cleanup path. `ALL` uses the public DOS `AnchorPath` protocol:
the candidate sets `APF_DODIR` on discovered directories and clears
`APF_DIDDIR` when the matcher returns from a child, suppressing the traversal
completion marker from duplicate output. The fixture verifies a nested
directory descent, return, and continuation. Original path rendering and
provider-specific link behavior still need guest comparison. The candidate
allocates an invocation-owned `AnchorPath`, separate pattern input/compiled
buffers, date strings, protection/comment storage, and a `FileInfoBlock`; it
uses `ParsePatternNoCase`, `MatchPatternNoCase`, `MatchFirst`, `MatchNext`,
`MatchEnd`, `StrToDate`, and `DateToStr`, polls Ctrl-C, and restores/closes
redirected output on every exit. MorphOS `LFORMAT`, `SORT`, `USERS`, and
`GROUPS` still fail closed with `ERROR_NOT_IMPLEMENTED`; Workbench `LFORMAT`
also remains fail-closed. Exact header/summary bytes and guest output parity
remain unverified.

The [AmigaDOS Command Reference](https://wiki.amigaos.net/wiki/AmigaOS_Manual:_AmigaDOS_Command_Reference#LIST)
documents the base row fields, `KEYS` behavior, protection-letter form,
date/time/comment output, `ALL` recursion, and `DateToStr`-based DOS date format.
The [Amiga ROM Kernel Reference](https://developer.amigaos3.net/sites/default/files/downloads/2024-10/Amiga_ROM_Kernel_Reference_Manual_DOS.pdf)
documents the public `AnchorPath` `APF_DODIR`/`APF_DIDDIR` traversal protocol.
The MorphOS
reference above remains the source for its 19-slot template and profile-only
options. The candidate uses the public DOS `StrToDate` API documented in the
[DOS autodocs](https://amigadev.grimore.org/Includes_and_Autodocs_2._guide/node0302.html)
to parse the date bounds; original-command boundary and error behavior remain
to be captured.

The Workbench 3.1 `C/List` member is 5,108 bytes, SHA-256
`69fb8b8075bc505977884e50233b7cc84237ebfc83405c27b1d818c9356dede1`,
version 37.5 (8.11.91). Its media scan provides this syntax-only candidate:

```text
DIR/M,P=PAT/K,KEYS/S,DATES/S,NODATES/S,TO/K,SUB/K,SINCE/K,UPTO/K,QUICK/S,BLOCK/S,NOHEAD/S,FILES/S,DIRS/S,LFORMAT/K,ALL/S
```

`NativeWorkbench31ListCommand` reuses the same bounded public-DOS path with
the 16-slot Workbench candidate template and a DOS 36 resident entry. The
candidate is not a runtime capture; exact Workbench ordering, formatting,
diagnostics, protection, and option behavior remain open.

The MorphOS resident entry opens DOS 37 and the Workbench resident entry opens
DOS 36; both reject Workbench or malformed startup input as applicable. The
[native qualification receipt](../../../artifacts/cc11-list-morphos-native-20260923-all-v1/qualification.json)
passes thirty-one supplied vector invocations on each 68000/020/040 target,
including parser/result/workspace ownership, flat and recursive matcher traversal,
pattern and literal-substring filters, escaped pattern punctuation, matching-
directory exclusion, nested descent/return with `APF_DODIR`/`APF_DIDDIR`, inclusive
since-only, up-to-only and ranged date filters,
invalid-date cleanup, `KEYS` behavior, default row fields, comment output,
`DATES` default, name-only `QUICK` precedence when combined with `DATES`, and
`NODATES` date/time suppression without conversion,
`DateToStr` failure cleanup, quick/block/no-header rendering, `TO` output
lifetime, allocation/Ctrl-C/empty-match paths, startup boundaries, and
interleaved callers. The fixture supplies synthetic DOS vectors and dates; it
does not verify a real filesystem or original command comparison.

The Workbench candidate uses the same bounded fixture through
`qualify_morphos_list_native_entry.ps1 -Profile Workbench31`; its refreshed
receipt is
`artifacts/cc11-list-wb31-native-20260923-all-v1/qualification.json`
and passes thirty-two supplied vectors per CPU (96 total), adding nested `ALL`
traversal and the same bounded inclusive date filters and invalid-date cleanup
alongside `KEYS`, `DATES`, `DATES QUICK`, `NODATES`, default-row, comment and
`DateToStr` failure cases to its missing-DOS, startup, parser, matcher, output,
allocation, Ctrl-C and interleaving coverage. Current HUNK identities are:

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 29604 | `64905165d9610275fa35951e740cb6566c591b7362bd3e6e2b2b8c32240e6e1c` | 21 | 32 |
| 68020 | 29704 | `6f029d8b6b1619edadf99b74ebe035c6a5a64d9e67768d15b732e325d956c5d6` | 21 | 32 |
| 68040 | 29600 | `444d5b28dfc41f34bb816ef83240e6c3888ffb4afa8321bb9244ddcdebbc87c7` | 21 | 32 |

The current MorphOS HUNK identities are:

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 29624 | `0f161cb1363f4ae1fda5480e4e09a6e4b6d9365aa76f0e3b00c892167797d3cf` | 21 | 31 |
| 68020 | 29724 | `e3071bd02c8dc300511b62c90b724a9f55d68605bece14ec79cb2484503030c9` | 21 | 31 |
| 68040 | 29620 | `34d2dda38bf484375c2576fa64acc1c64e928b317ad94db8c33a36e9da0181b0` | 21 | 31 |

Packed correspondence, exact Workbench grammar, recursive guest path/link
parity, sort/owner/`LFORMAT` behavior, exact bytes/status/IoErr parity, PURE/resident
lifecycle, source reuse rights, licensing, package admission, and differential
behavior remain open. The implementation must continue to use public DOS APIs
and release every matcher, parser, output, and workspace resource on all
exits.

The [2026-10-04 size receipt](../size-reductions-20261004.json) measures the
shipping Workbench profile with compile-time profile selection and argument
lease helpers passed by reference. Using the same clean compiler/SDK, its
MC68000 HUNK shrinks from 11,640 to 10,664 bytes: 556 bytes from removing lease
copies, then 420 bytes from profile specialization. Thirty-two supplied
Workbench invocations per CPU match baseline output, result, IoErr,
allocations, cleanup and DOS call order on 68000/020/040. The shared MorphOS
build passes 31 cases per CPU. Existing original guest and option gaps remain
open.
