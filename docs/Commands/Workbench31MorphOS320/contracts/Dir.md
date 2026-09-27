# Dir contract

Profiles: `wb31` and `morphos320`. Goal steps: CC01 and CC11. Recorded:
2026-09-23.

Status: partial MorphOS source-bound resident implementation and a separate
Workbench syntax candidate. Neither profile has packed-binary or original-guest
parity evidence, so neither is ready to ship.

The MorphOS 3.20 command archive contains `c/dir/dir.c`, SHA-256
`de52d372837cd4b68e17bd4c6b4ab1a7ec231b860b729ba9fddf1edc5ed18690`.
The source file's license still needs admission before any source reuse.

## MorphOS 3.20

The source grammar is:

```text
DIR,OPT/K,ALL/S,DIRS/S,FILES/S,INTER/S
```

The interactive subcommand grammar is:

```text
E=ENTER/S,B=BACK/S,DEL=DELETE/S,Q=QUIT/S,C=COM/S,COMMAND
```

The source checks explicit `INTER/S` and returns `RETURN_ERROR` with
`ERROR_NOT_IMPLEMENTED`. `OPT A`, `OPT D`, `OPT F`, and `OPT I` select recursive
listing, directories, files, and the legacy joined-line output mode. The
interactive command reader is disabled in the inspected source; `OPT I` still
suppresses line breaks. Directory labels retain ExAll order, file names are
case-insensitively sorted and paired, and `ALL` descends into each directory
while preserving the five-space indentation levels.

The current resident candidate uses DOS `ReadArgs`/`FreeArgs`, `ParsePattern`,
`MatchFirst`/`MatchNext`/`MatchEnd`, `Lock`, `CurrentDir`, `NameFromLock`,
`ExAll`, `Examine`/`Examine64`, `GetDeviceProc`, `ReadLink`, and
invocation-owned buffers. It implements flat directory/file filtering,
ignored option output, MorphOS `OPT A` recursion and `OPT I` formatting.
Wildcard paths use DOS pattern matching, preserve directory labels, classify
soft links by following their targets, warn about dangling links, and recurse
into matched directories under `ALL`. ExAll entries likewise follow links to
classify file and directory targets; recursive and explicit dangling paths use
the source-shaped `ReadLink` warning. The candidate accepts
`ERROR_NO_MORE_ENTRIES` as normal `ExAll` completion, returns `RETURN_ERROR`
for other provider failures, propagates recursive failure levels, and exercises
the generic and wrong-type source-shaped diagnostics. Its three-CPU fixture
passes thirty-nine supplied DOS-vector invocations per CPU. Wildcard traversal
saves DOS `IoErr` after processing each match, before `MatchNext`, and restores
that value after `MatchEnd` and buffer cleanup, as the inspected source does.
The fixture verifies a nonzero saved value survives terminal
`ERROR_NO_MORE_ENTRIES` and a cleanup-time `IoErr` change; it also records the
source-visible `ERROR_NO_MORE_ENTRIES` and dangling-link
`ERROR_OBJECT_NOT_FOUND` values after successful wildcard traversals. A Ctrl-C
during wildcard file output keeps `RETURN_WARN` while the source's saved
pre-output `IoErr` drives the diagnostic. Row tables grow in 128-entry
increments and retain copied names under invocation ownership; the
fixture pages `ExAll` and lists 300 direct and wildcard-matched files to cover
results beyond the former 256-row cap. It checks multi-directory ordering,
sorted file pairing, multi-level recursion, wildcard and wildcard-recursive
listings, file/directory/dangling soft links, direct and nested provider
errors, lock/control/buffer cleanup, empty-directory success, explicit
`INTER/S`, parser/startup/allocation guards, non-link lock failure, and
interleaved calls. Signal and matcher cases preserve `RETURN_WARN` for
interrupts before output, between directory and file output, during
wildcard-pair output, and when `MatchNext` reports a break. Direct-listing
interrupts and a `MatchNext` break retain `ERROR_BREAK`; wildcard output
restores the source-saved `IoErr` after the output interrupt. Low-memory cases
fail name allocation after ten retained rows and row-table growth at the
128-entry boundary, then verify prior allocations are released. Receipt:
`artifacts/cc11-dir-morphos-native-20260923-saved-ioerr-v3/qualification.json`.

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 15252 | `9ccb0f8e7cc69290d34be0a95fd966a2934ccfad1448a3a891200c8bb1cb0d33` | 34 | 39 |
| 68020 | 15224 | `90c4338f43855d3702e87f1d96d905fc283ccc601c3f1c5a79e9874d17579d0e` | 34 | 39 |
| 68040 | 15044 | `c283e11d1795092ab94d88b2649b96b3bc6f0e77a9babdbb4abcd3c741526f46` | 34 | 39 |

This is resident-HUNK and fixture evidence only. The complete `IoErr` and
diagnostic matrix, guest verification of pattern-matcher and Ctrl-C result
levels, original guest comparison, PURE/resident lifecycle, package admission,
source licensing, and differential evidence remain open.

## Workbench 3.1

The inspected Workbench member does not yet have a bound complete grammar or
runtime contract. A separate DOS 36 wrapper currently exercises a six-slot
syntax candidate through the public-DOS worker. It passes thirty supplied
vectors per CPU for flat listing, `OPT D/F`, wildcard matching, `ExAll`
provider-error return levels and diagnostics, lock failure, output and cleanup,
directory ordering, dynamic growth beyond 256 rows, paged `ExAll`, startup
boundaries, missing DOS, break-level return and `ERROR_BREAK` preservation,
injected allocation failure after retained names and at row-table growth, and
interleaved calls. The candidate rejects `ALL`
(including wildcard `ALL`) and explicit
`INTER`; these results are fixture checks, not established original behavior.
`OPT A`, soft links, and the actual grammar remain unclaimed pending original
binary/guest evidence. Receipt:
`artifacts/cc11-dir-wb31-native-20260923-saved-ioerr-v1/qualification.json`.

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 15252 | `23978133dd6ac8cb2f323b4821fcd9fe56cc20386e3d23d2ba0ed1bd7a4b4f27` | 34 | 30 |
| 68020 | 15224 | `24044e13154b821f032f6cf8e49122fc3e1ab924f11dd13b5ea9ae71e5f54c6a` | 34 | 30 |
| 68040 | 15044 | `fe31e2400b4f2f3adf0a1d7c2000ac117823af5b311fc7a2b06ff2c2492714e1` | 34 | 30 |

## Required gates

- [ ] Bind the original Workbench and MorphOS members; establish packed/source
      correspondence and clear source licensing before reuse.
- [ ] Compare parser, output, ordering, diagnostics, result levels and `IoErr`
      against the original binaries on disposable guests.
- [x] Qualify MorphOS wildcard, soft-link, and bounded ExAll error paths on
      68000/020/040 in
      `artifacts/cc11-dir-morphos-native-20260923-saved-ioerr-v3/`, including
      saved `IoErr` across matcher termination and cleanup.
- [x] Exercise paged direct/wildcard listings beyond 256 rows and verify the
      grown tables and retained names are released.
- [x] Check representative break and low-memory result levels, diagnostics,
      partial output, and cleanup in the native fixtures.
- [ ] Close the complete IoErr/diagnostic/result-level matrix, Ctrl-C, and
      nested failure behavior against original guests.
- [ ] Establish Workbench grammar and behavior independently; do not infer it
      from the MorphOS source.
- [ ] Verify each profile's actual PURE/resident flags, resident reuse,
      packaging, startup ownership, and differential behavior.
