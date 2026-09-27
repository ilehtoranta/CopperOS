# Workbench 3.1 / MorphOS 3.20 `SetDate` contract

Status: **partial implementation; no shipping profile**.

## Reference identity

- Profile: `wb31` (`CC14.SetDate.wb31`).
- Reference: Workbench 3.1 M10 disk `C:SetDate`.
- Captured file: `artifacts/setdate-wb31-workbench-m10-40.42.bin`.
- Captured size: 688 bytes.
- Captured SHA-256: `AFFCE0FCE8EEFB7700E7848D04B666781BD14A1C6F9597E73828BE203415DBCE`.

## Frozen grammar and observed paths

The captured binary passes this template to DOS `ReadArgs`:

```text
FILE/A,WEEKDAY,DATE,TIME,ALL/S
```

It allocates a public, cleared `AnchorPath`, initializes a DOS `DateTime`
from `DateStamp`, and tries each supplied weekday/date/time token as a date
and then as a time through `StrToDate`. It searches with `MatchFirst`,
`MatchNext`, and `MatchEnd`; when `ALL` is present it applies the original
`DoDirectory`/`DidDirectory` flag transition; and it uses the current match
lock with `DupLock`, `CurrentDir`, and `SetFileDate`. Normal
`ERROR_NO_MORE_ENTRIES` termination succeeds, `ERROR_BREAK` returns `WARN`,
and other search or update failures return `FAIL` after a DOS fault
diagnostic.

## Bounded native implementation

`src/Commands/Native/NativeWorkbench31SetDateCommand.cs` preserves the
captured grammar and the public DOS/Exec call sequence. The private root is
`tests/Commands.SetDateNativeRoot/Workbench31SetDateEntry.cs`.

Release resident HUNKs compile for MC68000, MC68020, and MC68040 under the
`memory=none`, `exceptions=yolo` profile. The durable supplied-vector receipt
is `artifacts/setdate-native-entry-20260920/qualification.json`: twelve cases
per CPU (36 total) pass on one resident image, covering ReadArgs ownership,
date fallback, directory flags, no-match, update failure, break, Workbench
startup, missing-DOS startup, cleanup, and interleaving. The HUNK identities
are:

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 3076 | `995128b2a2148fccc05e82584cc2b145a532e85488f802dd7d1923d7f2de1c55` | 10 | 12 |
| 68020 | 3108 | `2f188b12b64e5360fb9614e9e87842ca3ee95ca4e13f70d7adc6e0707d5c7c46` | 10 | 12 |
| 68040 | 3076 | `7b5013a1797e9ac352f1e46ecb95b212f818c793820bdf7fa5ef2b5336fd288c` | 10 | 12 |

This remains Copper68k adapter evidence; no original-guest execution has been
admitted.

## MorphOS 3.20 profile

- Profile: `morphos320` (`CC14.SetDate.morphos320`).
- Reference: MorphOS 3.20 `MOSSYS:C/SetDate` packed native command.
- Captured binary: `artifacts/setdate-morphos320.iso.bin` (2050 bytes,
  SHA-256 `1A4B0B91A9BA910C9E9D7E25A95078913302C44397405A0E28BDB8F69E486BD9`).
- Released source identifies version `50.3` and the same
  `FILE/A,WEEKDAY,DATE,TIME,ALL/S` template. The implementation opens
  `dos.library` v37, initializes the current date through `DateStamp`, tries
  each supplied token as a date and then a time, and reports a failed token
  with the literal `SetDate failed: Invalid WEEKDAY, DATE or TIME string!`
  line. The source archive was used as behavior evidence only; it is not
  copied into CopperOS.
- For `dos.library` 50.67 and later the command sets the MorphOS extended
  AnchorPath bit and `DontFollowSLinks` extension. `ALL` descends into real
  directories in reverse traversal order; soft-link update failure is treated
  as success by the source compatibility fix. Other matcher or update errors
  are printed with the `SetDate failed` header, with `ERROR_BREAK` returning
  `WARN` and `ERROR_NO_MORE_ENTRIES` returning `OK`.

`NativeMorphOSSetDateCommand` and its private root compile as resident HUNKs
for 68000, 68020, and 68040. The supplied-vector receipt is
`artifacts/morphos-setdate-native-entry-20260912-qualified/qualification.json`:
12 cases per CPU (36 total) cover parser ownership, date fallback and invalid
tokens, extended AnchorPath setup, directory traversal, soft-link failure,
matcher/update/break errors, cleanup, and interleaved callers. All cases pass
with one protected image per CPU, balanced invocation storage, and zero
shared-image writes. This is bounded ABI/resource evidence only.

## Gates still open

- Capture exact parser, date precedence, directory, lock, update, break,
  `IoErr`, and diagnostic behavior from the Workbench guest.
- Compare the supplied-DOS fixture against original Workbench output and
  `IoErr` traces, including the exact diagnostic text.
- Compare the MorphOS profile against an original guest, including exact
  version-gated AnchorPath extension behavior and diagnostics.
- Verify PURE/resident same-SegList reuse, concurrent invocation, package
  placement, and differential reference runs.

Do not promote `CC14.SetDate.wb31` to shipping until those gates are recorded
in the completion ledger.
