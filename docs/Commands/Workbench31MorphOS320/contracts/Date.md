# Workbench 3.1 `Date` contract

Status: **partial implementation; no shipping profile**.

## Reference identity

- Profile: `wb31` (`CC16.Date.wb31`).
- Reference: Workbench 3.1 M10 disk `C:Date`.
- Captured file: `artifacts/date-wb31-wb31-workbench-m10-40.42.bin`.
- Captured size: 1092 bytes (264-long HUNK_CODE payload plus HUNK metadata).
- Captured SHA-256: `9568FA98CD28304D185F07E4DD396340DCD36CB18451CA095323712AC0E29AC7`.
- Version string: `date 37.1 (10.1.91)`.
- The binary imports `dos.library` v36, `utility.library` v0, and
  `timer.device` unit 0.

## Frozen grammar and observable paths

The captured binary passes this exact template to DOS `ReadArgs`:

```text
DAY,DATE,TIME,TO=VER/K
```

With no `DAY`, `DATE`, or `TIME` value it obtains the current DOS stamp,
formats it with `DateToStr`, and writes `%s %s %s\n` to the current output.
`TO` opens a new file with `MODE_NEWFILE` and uses `VFPrintf`; an output-open
failure prints the DOS fault against the requested name and returns `FAIL` or
`WARN` according to whether a setter argument was supplied. The captured
setter path tries each supplied token as a date and then as a time, preserving
omitted fields from the current stamp, converts days/minutes/ticks through
`utility.library` (`UMult32`/`UDivMod32`), and submits
`TimerCommand.SetSystemTime` through `timer.device`.

The replacement opens `utility.library` at capability floor v36 because its
`UMult32` and `UDivMod32` calls are V36 vectors. The captured v0 import is
differential evidence and is not sufficient for this API use.

The static string evidence also includes the original usage text:

```text
- use DD-MMM-YY or <dayname> or yesterday etc. to set date
      HH:MM:SS OR HH:MM to set time
```

## Current implementation

`src/Commands/Native/NativeWorkbench31DateCommand.cs` preserves the template,
uses DOS `DateStamp`/`StrToDate`/`DateToStr`, uses public Exec allocation and
timer-device calls for the setter, and releases every invocation-owned record
before the startup code closes `dos.library`. The private native root is
`tests/Commands.DateNativeRoot/Workbench31DateEntry.cs`.

Release resident HUNKs compile for MC68000, MC68020, and MC68040 under the
`memory=none`, `exceptions=yolo` profile. The durable supplied-vector receipt
is `artifacts/date-native-entry-20260921-v2/qualification.json`: nine cases per
CPU (27 total) pass with balanced guest allocations/libraries, including
Workbench startup and missing-DOS boundaries, one image load per CPU, and zero
shared-image writes. The HUNK identities are:

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 3880 | `fa4b9d5ad99a0339479dea8869e318a609c99bd616b34d68299d57944ead206e` | 11 | 9 |
| 68020 | 3916 | `388d63d7b11074af5f3eeae4f3ec61cb5e2fdb60e09688d9e96a2662e198888f` | 11 | 9 |
| 68040 | 3880 | `a8ca9186f01e456a377eae62bb979af80b155d392a0e449b43430cc1ac5c6d50` | 11 | 9 |

## Gates still open

- The Workbench original guest needs captures for exact date/time precedence,
  `IoErr`, timer-device failure, and partial-set failure results.
- MorphOS `Date` is a separate profile and is not covered by this body.
- PURE/resident same-segment reuse, concurrent invocation, final package
  placement, and differential reference runs remain open.

Do not promote either `Date` profile to shipping until those gates are recorded
in the completion ledger.

## MorphOS 3.20 `Date` profile

Status: **partial implementation; no shipping profile**.

The MorphOS 3.20 source contract is `Date` version 50.7 with the template:

```text
DAY,DATE,TIME,TO=VER/K,LFORMAT/K
```

The setter classifies hyphenated input as a date, colon input as a time, and a
single short `HH:MM` value is expanded with `:00`. A remaining unclassified
token is treated as the weekday/date expression. It creates a message port and
timer request, opens `timer.device` VBlank unit 1, parses the supplied values
with `DateStamp`/`StrToDate`, checks the 32-bit seconds range, and submits
`TimerCommand.SetSystemTime` with `IOF_QUICK`. Timer resources are closed and
deleted before optional output formatting.

With `TO` the command opens `MODE_NEWFILE` and writes the normal
`DateToStr` result. With `LFORMAT` it opens `locale.library` version 38,
calls `OpenLocale(NULL)` and `FormatDate`, sends callback characters through a
native `Hook`, and appends a newline. The released source calls the locale
formatting path even when `OpenLocale` returns `NULL`; this behavior is retained
in the fixture contract. Parser failures use `PrintFault`; timer and write
failures preserve the source diagnostic strings and return `FAIL`.

`src/Commands/Native/NativeMorphOSDateCommand.cs` and the private native root
`tests/Commands.MorphOSDateNativeRoot/MorphOSDateEntry.cs` implement this
bounded body with invocation-owned public/cleared storage. The durable supplied
vector receipt is
`artifacts/morphos-date-native-entry-20260912-qualified/qualification.json`:
13 cases per CPU (39 total) pass on resident 68000/020/040 HUNKs, including
normal and `TO`/`LFORMAT` output, date and short-time setters, parser failure,
timer open and I/O failures, locale-open behavior, write failure, cleanup and
interleaving. The receipt records one image load per CPU, zero shared-image
writes, balanced DOS/Exec/timer/locale ownership, and no managed allocation or
runtime-helper sites in the static reports.

The MorphOS packed identity is now hash-bound in
`reference-captures/date-morphos-binary-audit-20260923.json` (ISO extent
`175288`, 3,748 bytes, version 50.7). Original guest execution, exact
`IoErr`/diagnostic parity,
PURE/resident same-segment reuse, concurrent invocation, final package
placement and differential reference runs remain open.
