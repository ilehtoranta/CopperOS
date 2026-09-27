# Workbench 3.1 / MorphOS 3.20 `RequestFile` contract

Profiles: `wb31` and `morphos320`. Goal step: CC24. Recorded:
2026-09-20.

Status: bounded Workbench and MorphOS resident candidates; no shipping profile.

## Reference identity and grammar

The inventory binds the Workbench 3.1 `C:RequestFile` HUNK (1,520 bytes,
version 39.2, SHA-256
`2ba44b2795cfb683d6ad17cd10e3f714e84aa9ba1e039e18f3a806941628e354`) and
the MorphOS 3.20 `MorphOS/C/RequestFile` member (2,323 bytes, version 50.4,
SHA-256 `43f04f703255c1a6a96fea5892be235bd6384b34f8b8daa5a654d6de0bca9be3`).
The Workbench media exposes this syntax candidate:

```text
DRAWER,FILE/K,PATTERN/K,TITLE/K,POSITIVE/K,NEGATIVE/K,ACCEPTPATTERN/K,
REJECTPATTERN/K,SAVEMODE/S,MULTISELECT/S,DRAWERSONLY/S,NOICONS/S,PUBSCREEN/K
```

The MorphOS source-derived candidate adds `INITIALVOLUMES/S` as the fourteenth
slot. This difference remains a profile contract requiring original guest
confirmation; the replacement does not silently apply the MorphOS extension to
the Workbench profile.

## MorphOS source candidate behavior and ownership

The captured source requests `dos.library` 37 and `asl.library` 37. The
replacement opens DOS36 for its parser contract and ASL at the verified V36
requester capability floor, parses the options
with DOS `ReadArgs`, constructs an invocation-owned `FileRequester` tag list,
calls `AslRequest`, and frees the requester before closing either library.
The tags map initial drawer/file/pattern, title and button labels, save mode,
multi-select, drawers-only, icon rejection, public-screen name, pattern mode,
and the optional initial-volume display flag. A 512-byte public buffer is used
with DOS `AddPart` to construct the returned paths.

Single selection prints one quoted path followed by a newline. Multi-selection
prints each quoted path followed by a space and a final newline. A cancelled
request returns `WARN` with `ERROR_BREAK`; requester/parser/allocation failures
retain their DOS result and diagnostic policy. The resident entry rejects
Workbench startup, validates the argument boundary, and closes/replies through
the common startup owner.

## Workbench bounded native candidate

`src/Commands/Native/NativeWorkbench31RequestFileCommand.cs` keeps the
captured Workbench thirteen-slot grammar and omits MorphOS's
`INITIALVOLUMES/S` extension. `Workbench31RequestFileEntry` opens DOS 36 and
ASL 36, rejects Workbench startup, and routes parser, requester, output and
cleanup through the public Kickstart interfaces. The fixture covers the same
option-to-tag mapping, single and multi-select output, cancellation,
allocation/parser/startup failures, repeat ownership and two interleaved
callers without sharing invocation state.

Receipt:
`artifacts/requestfile-wb31-native-20260920-candidate/qualification.json`.

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 5140 | `98871128bea4a9f0788538dcf49ec6b06966c9bad015e6d8df3db7a5322172f7` | 16 | 12 |
| 68020 | 5196 | `e264d4a4d1c960c755c83f5616cc7d0ebe3740eba234f88461a8b3d8e696178b` | 16 | 12 |
| 68040 | 5108 | `65c51ae798424b807552b1b6128680e56e3953ba48ad703be443e0943d93fbe4` | 16 | 12 |

This is a syntax-bound candidate, not original Workbench parity. Exact ASL
requester behavior, diagnostics, PURE/resident classification, minimum stack,
licensing, package placement and guest differential evidence remain open.

## Bounded MorphOS native receipt

`tools/Commands/qualify_morphos_requestfile_native_entry.ps1` builds the
resident entry for 68000, 68020 and 68040 and runs twelve supplied DOS/ASL
invocations per CPU. The fixture covers every option-to-tag mapping, single and
multi-select output, cancel, result-buffer allocation failure, parser failure,
ASL-open failure, Workbench startup rejection, repeat ownership, and two
interleaved callers.

Receipt:
`artifacts/requestfile-morphos-native-20260921-v4/qualification.json`.

The replacement opens `asl.library` at the verified V36 capability floor; the
captured V37 request remains differential evidence. The ASL authority marks
the requester vectors used here as V36 or later.

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 5264 | `f20119667d7a30fc214549c022036c9b0ebe4d03a505bd414b4f9d088d9b67c4` | 16 | 12 |
| 68020 | 5324 | `2ab9522eb05f91644c410bce36c92f21e827223ffe2b397622740e1251f6a1aa` | 16 | 12 |
| 68040 | 5236 | `9543ddcc3fc3b005593fc5f2f3e6361b90f3fa1d5742d4532a1183792a471cf4` | 16 | 12 |

This is adapter and supplied-provider evidence only. Exact requester layout and
button behavior on original MorphOS, Workbench correspondence, real ASL/UI
lifecycle, PURE/resident reuse, minimum stack, licensing, package placement,
and differential evidence remain open.
