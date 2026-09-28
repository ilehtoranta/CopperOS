# PathPart contract

Profile: `morphos320`. Goal steps: CC01 and CC10. Recorded: 2026-08-30.

Status: **partial contract; no reference execution or parity claim**. The
release binary and the original DOS path APIs are verified. The command
template is a documentation lead, not yet verified from the packed 3.20
executable or its interactive help. Unresolved rows below remain completion
gates; they are not permission to invent behavior.

## Reference identity

| Item | Evidence |
| --- | --- |
| MorphOS distribution | [Official 3.20 ISO](https://www.morphos-team.net/morphos-3.20.iso), locally `D:/TestData/MorphOSReferences/morphos-3.20.iso` |
| Member | `MorphOS/C/PathPart`, ISO logical block 177964, 2,098 bytes; block size 2,048 |
| Member SHA256 | `ce7c5d13effeb2eadedb70177ca8bbc1bcd0ae8b611e294a015483233db2d0f7` |
| Embedded version | `PathPart 52.0 (10.1.2013)`, Ilkka Lehtoranta |
| Executable inspection | Begins `7f4d4f53`; packed native MorphOS payload. Version is readable; an absent plaintext template proves nothing. |
| Workbench 3.1 | Absent from the inspected original Workbench and Install `C:` inventories. Do not add it to the exact `wb31` distribution. CC00 owns remaining media reconciliation. |
| Pure/resident classification | **Required pure by original installer design**: ISO `hdinstall.fixc`, line 50, adds P to PathPart. Actual installed metadata and resident execution evidence remain open. |

The verified complete ISO SHA256 is
`3761e191124cfd204a490915f8d285d55969e95b66c5c3cd1cf854bc71dab911`.
`hdinstall.fixc` is 2,478 bytes at ISO block 6348, SHA256
`46751fd064329077fe3f1ebeff6ea47c13159dd84841fac83d2043fb270ca8df`.
Its P addition preserves other flags; it does not itself make PathPart resident.
ISO POSIX mode bits do not establish AmigaDOS protection. See the CC00
[inventory](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/command-inventory.json)
and [media evidence](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/media-evidence.json).

The partial [3.20 command source release](https://www.morphos-team.net/files/src/3.20/c.tar.bz2)
does not contain PathPart. No source reuse is authorized by its availability.

## Argument contract

The [MorphOS Library entry](https://library.morph.zone/Shell_Commands/PathPart)
provides this candidate template and describes extraction and path assembly.
It is community documentation, not release-pinned primary proof:

```text
DIR/K,FILE/K,ADD/K/M
```

Use DOS `ReadArgs` for this template once confirmed. There are three result
slots. `DIR` and `FILE` take keyword strings; `ADD` takes a keyword list of
strings, represented by a null-terminated pointer vector. These are not
switches or numeric arguments. No slot is marked required. Preserve empty
quoted values, DOS quoting and star escapes, keyword spelling/case behavior,
and `?` continuation at the DOS/Shell boundary. Do not add positional operands,
extra switches, or a command-private tokenizer to make examples work.

| Option | Candidate operation | Required confirmation |
| --- | --- | --- |
| `DIR` | Extract the directory portion of its string | Separator retention, root-only and empty results, newline and return level |
| `FILE` | Extract the final component of its string | Empty/trailing-separator behavior, newline and return level |
| `ADD` | Assemble the supplied components in order | Initial destination, empty list/single item, root/absolute replacement, output |
| Combined modes | Not established | Every combination of DIR/FILE/ADD, independent of argument order; whether modes compose, take precedence, or fail |
| No mode | ReadArgs has no `/A` requirement | Whether the command prints usage, emits nothing, succeeds, or returns an error |

Do not claim a maximum path length from a current CopperOS scratch buffer.
Probe the reference boundary and retain overflow-safe allocation in CopperOS.

## Verified API basis and ownership

The original `NDK_3.1/DOCS/DOC/DOS.DOC` on
[AmigaDeveloperCD.iso](D:/TestData/AmigaDeveloperCD.iso) is the primary API
authority. Its SHA256 is
`2e22d1d1b7c00fac5757eead0ef4d49a01c7666f0db5850735193c9597a7cee2`
(ISO block 17730, 174,666 bytes). `FilePart`, `PathPart`, and `AddPart` are
available before Kickstart 3.1:

- `FilePart` returns a pointer into the supplied string after its last path
  separator. For `VOL:one/two`, this identifies `two`.
- `PathPart` differs at a final slash: the pointer identifies that slash.
  For `VOL:one`, it identifies the first character after the colon. An
  implementation extracting a directory must respect this distinction.
- `AddPart` mutates the destination buffer, deals with DOS separators and
  relative parents, and replaces the destination for a fully qualified added
  path. It returns false on insufficient capacity without changing the
  destination. The V37 fixes cover the earlier leading-slash limitation.

These facts establish public API semantics, not the unobserved command's
mode precedence or output bytes. Do not use host `System.IO.Path` rules, resolve
assigns, canonicalize to host paths, or require a path to exist for lexical
operations without command evidence.

Initialize invocation-local argument slots, retain the returned RDArgs while
using its strings/vector, and call `FreeArgs` once. A truncation point or
assembly destination must be in writable invocation-owned memory. Do not
modify the original argument text or shared resident data. Borrow `Output()`;
do not close the caller's stream. Obtain library bases using the normal
standalone startup contract, not an assumed incoming A6.

## Required fixtures

These are fixture specifications, **not completed tests or captured oracles**.
For every run record raw input bytes, profile, command hash, stdout and error
stream bytes, immediate return level and IoErr, signals, and resource deltas.

| IDs | Cases and oracle obligation |
| --- | --- |
| `PP-PARSE-01..` | Every keyword with space and equals syntax; case variants; quoted empty/spaced/star-escaped values; unknown keyword; missing keyword value; unterminated quote; repeated keyword; `?` followed by input and by EOF |
| `PP-DIR-01..` | `VOL:one/two`, `VOL:one`, `VOL:`, `one/two`, `one`, empty, `/`, `//one`, trailing slash, repeated slash; capture exact output and return |
| `PP-FILE-01..` | Same path corpus; ensure nonexistent paths are included so lexical behavior is distinguished from filesystem lookup |
| `PP-ADD-01..` | Zero/one/multiple components, empty components, `VOL:` plus `one` plus `two`, leading slash parents, later fully qualified component, colon-containing component, size boundaries |
| `PP-MODES-01..` | All eight mode-presence combinations, including no mode; repeat mixed cases with different argument order and conflicting values |
| `PP-FAIL-01..` | ReadArgs allocation failure, destination allocation failure, failed output, short write, input EOF and pending Ctrl-C; capture diagnostic owner and IoErr after cleanup |
| `PP-LIFE-01..` | Repeated success/failure and overlapping invocations sharing a loaded segment, without shared scratch storage or leaked RDArgs |

`DIR VOL:one/two` yielding `VOL:one` and `FILE VOL:one/two` yielding `two`
are API-derived candidate checks. Their command-level newline, return values,
and combinations must still be captured on PathPart 52.0.

## Bounded native implementation checkpoint

`MorphOSPathPartCommand` now provides a standalone, invocation-local native
body for the documented candidate `DIR/K,FILE/K,ADD/K/M` outer grammar. It
uses `ReadArgs`/`FreeArgs` for parsing and delegates lexical operations to DOS
`PathPart`, `FilePart`, and `AddPart`; it sizes one invocation-owned scratch
buffer from the parsed strings and `ADD` vector, with checked arithmetic and
the DOS `Write` LONG range as its output bound. It retains no path, CLI, or
resident state. The private
[`Commands.PathPartNativeRoot`](D:/Koodit/GIT/CopperOS/tests/Commands.PathPartNativeRoot)
compiles this body with a normal DOS startup entry. Its explicit cleanup path
releases scratch storage and the ReadArgs lease before restoring IoErr and
closing DOS; it contains no managed exception region. Resident HUNK checkpoint:

| CPU | Bytes | SHA-256 | Header |
| --- | ---: | --- | --- |
| 68000 | 3,088 | `85cae882ace2199b0ac3d4567d650daf765307819fdc90c24bf489bb865de785` | `0x000003f3` |
| 68020 | 3,148 | `a61c28e880ba091dbd87567e2b781267db1f4dae1a9634253e0fded6875be4a4` | `0x000003f3` |
| 68040 | 3,088 | `31e9f26c47237796b5c2af7844b60aaaf18c115013eaaa1162d3ef3ee00ac70d` | `0x000003f3` |

`qualify_pathpart_native_entry.ps1` currently compiles three resident artifacts
and executes 15 supplied post-ReadArgs vectors per CPU through Copper68k. In
addition to DIR, FILE, ADD, combined/no-mode, failure, repeat and
instruction-interleaving cases. It also exercises scratch-allocation failure
after ReadArgs succeeds, 1,100-byte directory and file results, and 70 ADD
components producing a result longer than 1,024 bytes. Every run reports zero
shared-image writes and balanced Exec/DOS/RDArgs resources. The DOS adapters provide
PathPart/FilePart/AddPart answers and do not parse command text, access a
filesystem, or represent MorphOS output behavior.

The candidate combined-mode presentation, no-mode behavior, exact line output,
status, diagnostics, capacity, cancellation, installed P policy, same-SegList
purity against real DOS, and MorphOS runtime comparison remain open.

The current-source 2026-09-22 rerun is recorded at
`artifacts/cc10-pathpart-native-20260922-v1/qualification.json`. It retains 11
supplied invocations per CPU; current HUNK hashes are 68000
`ad92c91ebce428817c9671fb9701a82843b8160cc3e653fc86ff2fc55611d4dd`, 68020
`e8ba70d5999037496675320f4780c08bf491031c948695542140aa2c02ca9ad8`, and
68040 `f4e0d3836d9edc4ef8d22e761d21c2072aaeabceb6652c057d5b1f809960f52e`.
This rerun does not close the open reference, packaging, or resident-lifecycle
gates.

## 2026-09-28 dynamic output sizing checkpoint

`MorphOSPathPartCommand` no longer imposes the old 1,024-byte scratch-buffer
limit or the 64-item `ADD` loop limit. It measures each retained `ReadArgs`
string, scans the DOS-owned `/M` pointer vector to its terminator, and computes
a conservative `AddPart` capacity with overflow guards. Output storage is
invocation-owned and is capped only where the DOS `Write` interface's signed
LONG byte count requires it. The same capacity is released after every path.

The refreshed resident fixture passes 15 supplied vectors per CPU (45 total)
on 68000/020/040. It includes directory and file output longer than 1,024
bytes, 70 `ADD` components producing a path longer than 1,024 bytes, and a
failed scratch allocation after successful parsing, plus the existing
ReadArgs/result-allocation failures, repeated calls, and interleaved callers.
All executions report one image load and zero shared-image writes; the static
reports contain 18 reachable methods, no runtime features, managed allocations,
helpers, external targets, fatal sites or exception regions. Receipt:
`artifacts/cc10-pathpart-native-20260928-dynamic-output-v5/qualification.json`.
The three HUNK SHA-256 values are:

| CPU | Bytes | SHA-256 |
| --- | ---: | --- |
| 68000 | 4,008 | `d64424678473354a762276fe5b0d033d5f80eb8b94d16470b248e4d8b5aca88b` |
| 68020 | 4,088 | `26c8ce3f4c42f9b1b4ac65c209f1bd33481a1f3c6bf7fc3bb5e40e5957b8d344` |
| 68040 | 4,008 | `abb39cd0daf8fc6b2befabd257dcf660e3e745d94047ef19df1285747f93b4ac` |

These are candidate path-helper fixtures, not a real MorphOS parser or
filesystem comparison. Exact template and option interaction, original output,
diagnostics, capacity boundary, installed P policy, lifecycle, PURE admission,
licensing and package gates remain open.

## Completion gates

- [ ] Confirm the exact template and safe `?`/EOF behavior on the 3.20 member.
- [ ] Capture all modes, their interaction, default behavior, diagnostics,
  success/failure return values, IoErr, output errors, and cancellation.
- [ ] Bind installed P policy and resident tests to CC00/CC05 evidence.
- [ ] Run the fixtures against the reference and all required CopperOS targets;
  compare bytes and effects, not just API call counts.

No implementation source or reference executable is copied by this contract.
