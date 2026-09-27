# FileNote contract

Profiles: `wb31` (`Filenote`) and `morphos320` (`FileNote`). Goal steps: CC01
and CC14. Recorded: 2026-09-12.

Status: open reference contract with a bounded MorphOS native body. The
Workbench template is only a syntax candidate and no command-level parity is
claimed.

## Reference identity

| Profile | Evidence |
| --- | --- |
| Workbench 3.1 | `C/Filenote`, 896-byte Amiga HUNK, SHA-256 `c5e4576eddb734c57260f6c3516f082ae955e3a89b3a495c3a27cfacb5b209f`, version `filenote 37.1 (10.1.91)`. The observed protection word is clear, so no PURE classification is inferred. |
| MorphOS 3.20 | `MorphOS/C/FileNote`, 3,583-byte packed native member, SHA-256 `6c554befecdd54ac474bfe3d795c02d2717cc7dea33d1b8c093d4d10f010286d`, version `FileNote 50.6 (4.6.05)`. The installer marks FileNote P, but installed metadata and resident lifecycle are not observed. |

The complete MorphOS ISO hash and media authorities are recorded in the
[command inventory](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/command-inventory.json).
The official 3.20 source archive is inspection evidence only; no source or
binary is copied into the repository. Its extracted `c/filenote/filenote.c`
is 9,504 bytes, SHA-256
`79e42dad2839680d1cba63315bfd7735ac2c6ac10a4eb186a9c0f46eb37fdedc`, within
the archive hash already bound by the authorities.

## Grammar and observed behavior

The Workbench binary contains this syntax candidate:

```text
FILE/A,COMMENT,ALL/S,QUIET/S
```

It is not a parser or help capture. The 3.20 release source uses the same
template. `FILE` is a pattern or path, `COMMENT` is optional and maps a null
value to an empty comment, `ALL` enables recursive directory traversal, and
`QUIET` suppresses per-object output. The source truncates comments longer than
79 characters, reports that truncation, follows hard links through its
AnchorPath policy, calls `SetComment`, continues after per-object failures with
WARN, and reports `MatchFirst` failure as FAIL. Its output indents recursive
entries, labels directories with ` (dir)`, and appends `..done` or a fault
diagnostic. The source notes that 50.6 fixed the null-comment zeropage read and
that 50.2 removed shared writable state for purity.

These observations do not establish the packed executable, exact diagnostics,
soft-link provider behavior, volume/device rejection, parser edge cases,
Workbench 37.1 behavior, or reference output bytes. Those remain required
captures.

## Bounded MorphOS implementation

`NativeMorphOSFileNoteCommand` owns the `ReadArgs`/`FreeArgs` lease, a cleared
AnchorPath plus 512-byte path storage, an 80-byte comment copy, and formatting
arguments for one invocation. It initializes the public `DoWild` and
`FollowHardLinks` AnchorPath flags, truncates the copied comment at 79 bytes,
uses `MatchFirst`/`MatchNext`/`MatchEnd`, performs recursive directory entry
and exit handling through the public flags, calls `SetComment` on each selected
object, and preserves the last SetComment error after diagnostic output. It
does not use host paths or shared mutable state. The Workbench DOS 36 candidate
now has a separate resident entry and qualification suite while sharing this
public-DOS body. Its syntax and output remain candidates until the packed v37.1
binary and original guest behavior are captured.

`tools/Commands/qualify_filenote_native.ps1` emits
`artifacts/filenote-morphos-native-20260912-qualified/qualification.json`. The resident HUNKs
compile on all three CPUs with 14 reachable methods and zero managed runtime
features/helpers, external native targets, exception regions, or fatal machine
fault sites. The same receipt now runs the supplied-DOS fixture on all three
CPUs: ten invocations per CPU, including parser failure, no-match, truncation,
quiet output, directory descent/exit, SetComment failure, cleanup and
interleaving. It records no shared-image writes or leaked invocation storage.

| CPU | Bytes | SHA-256 |
| --- | ---: | --- |
| 68000 | 3,680 | `d7e7b210cdfe526c2e9a773055d5f1e06caa08990a449c10a47308bbdd310ceb` |
| 68020 | 3,728 | `aed0f54151e3ec909a92ae207d2eba7f358873dcbc7aa462de1d311c20f127c7` |
| 68040 | 3,672 | `d38bb13d407824c866369eff3827323567cd1db1114fc8c53685fd288d1bdced` |

`tools/Commands/qualify_workbench31_filenote_native.ps1` emits
`artifacts/filenote-wb31-native-20260917-qualified-v1/qualification.json`. The
syntax-candidate HUNKs use the separate DOS 36 entry, have fifteen reachable
methods, and pass ten supplied DOS vectors per CPU (30 total):

| CPU | Bytes | SHA-256 |
| --- | ---: | --- |
| 68000 | 3,752 | `c5b3f95fc637d7749a97f668a3db87ca0f860522d23e7700ef73f11fe3b268f2` |
| 68020 | 3,800 | `4cb246479873f30943364db4dc819de363aebceb0d8d001d49223ceb376016a7` |
| 68040 | 3,744 | `7bd403039813b256741d4d7ee3b94c2526f96ec79c6fabe9273bc5b2909e8167` |

This remains a syntax-candidate native receipt. It does not establish packed
correspondence, exact Workbench output/diagnostics, PURE/resident admission,
production lifecycle, packaging or differential parity.

The receipt is authoritative for this bounded static and supplied-DOS
checkpoint. It is not a packed-binary, original-command, Workbench-parity,
production-PURE or shipping qualification.

## Required captures and gates

- [ ] Freeze both profile grammars, aliases, parser/help behavior, comment
  quoting, truncation, status and exact diagnostics.
- [ ] Capture files, patterns, directories, recursive order, links,
  volume/device roots, empty comments, long comments, failures and Ctrl-C.
- [x] Add a supplied-DOS runtime fixture for parser, matcher, SetComment,
  recursive flags, output, IoErr, cleanup and interleaved callers.
- [ ] Compare against original Workbench and MorphOS binaries, qualify P
  lifecycle and same-SegList execution, and package the correct profile.
