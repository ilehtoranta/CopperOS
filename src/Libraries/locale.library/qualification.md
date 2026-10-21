# locale.library compatibility qualification

## Current input and catalog-tag follow-up — 2026-10-20

The 1 MiB physical input/paint regression now passes both widths, with three
lifetimes and 18 exact bitmap/display captures per width. Its obsolete close
mark, missing pointer hotspot, mid-feedback pressure capture and permanent
provider-reference expectations are corrected in the test; input and painting
capacities remain unchanged. Details and receipts are in the
[Workbench report](workbench-qualification.md).

A new original-binary comparison exposes a catalog-tag compatibility defect.
OC_BuiltInCodeSet (+2) does not select translation output encoding. The former
alias produced cat_CodeSet=1/2 where V40 retained zero, and an empty byte-string
lookup after selecting UTF-32. Before-fix tests record all three differences
in desktop-memory/locale-before-v689/artifacts/locale-tests/original-comparison.json;
both selected regressions fail in locale-v689-before-results/locale-before.trx.
Only OC_CodeSet (+5) now selects conversion. The tag identities are specified
by the [official MorphOS header](https://morphos-team.net/sdk/includes/libraries/locale.html),
and the ignored built-in hint is also confirmed in the supplied V40 execution.

The expanded original matrix has 4,728 observations. Native extension checks
cover LANG_NAME, %L/%ll extreme integers, OC_CodeSet/OC_ConvertStrings in all six
cross-encoding pairs, CatComp array/block tags, simultaneous per-open built-ins,
and ARexx status/result registers on all three CPU targets. They retain code
immutability and nonvolatile-register checks and reject exception regions and runtime dependencies. Compiler fault-site
counts are reported separately: the seven 68000 sites map to deliberate
divide-by-zero guards in FormatCore.Divide, RexxCore.Decimal and DateCore.Number.
A new zero-count assertion incorrectly rejected these retained guards and was
corrected before native execution. All 53 tests pass without skips in locale-v699-results/locale.trx. The expanded
original comparison has 4,728 observations and zero differences, and all three
native CPU variants pass the register, immutability and extension checks.
The 68000 image is 53,152 bytes, SHA-256
4CBABF1EF8E436D2CA6B9911F2F9E61219751B5E6399DD67A96F2889936F1122.
Its dense lookup remains 14 instructions / 176 flat-bus cycles, and sparse
measurements are unchanged. The full 1 MiB Workbench boot with this exact image
passes at frame 67,920, all 28 original locale/status fields match, startup
commandFailures=[] and the raster has zero stray pixels. Full results are in
[the Workbench report](workbench-qualification.md) and
desktop-memory/qualification-v704.json.


Checked on 2026-10-20 with the production CopperSharp Resident build and
Copper68k's 68000 interpreter. The comparison executes unmodified reference
code; it does not compile a port of the reference implementation.

| Reference | Identification | SHA-256 |
| --- | --- | --- |
| Workbench 3.1 locale.library | locale 40.4 (16.8.93) | `7A5E637728ADEBB7E14A2722D90DFB3739AC5ABF6746A39931E0AA4F14EF327F` |
| Workbench 3.1 iffparse.library | iffparse 40.1 (9.2.93) | `3EA9F30CEA549CA3B6CED046152B7CEF15A3FAFBA935CA232870D5394097CA9B` |

Both references come from the existing local Workbench extraction. They are
not redistributed. The test loader relocates their CODE/DATA HUNKs, builds the
negative vectors and applies their Exec AutoInit structures. LocalePrefsUpdate
installs the original system patches. Original iffparse code opens generated
IFF CTLG files for the reference locale implementation.

The checked differential matrix contains **4,728 cases with zero differences**:

- All 50 classic English locale strings.
- All 256 byte values for lower/upper conversion and eleven character tests.
- All 256 byte values through patched utility lower/upper functions.
- ASCII, accent and case-sensitive collation, plus patched Stricmp/Strnicmp.
- DOS DateToStr formats 0–4, relative substitutions and weekday handling.
- DOS StrToDate calendar validation, numeric/abbreviated months, relative
  dates, future/past weekdays and permissive time parsing.
- Null LocalePrefsUpdate query, positive/negative DOS error IDs and misses.
- Open-vector D0 handling, global Language and utility language side effects,
  and the complete set of eight installed system patch targets. Classic
  DosGetString is patched at -978 with the error ID in D1; -924 is not patched.
  Distinct D0/D1 values exercise the saved getter's actual argument mapping.
- RawDoFmt word/long, grouping, positional arguments, callbacks and D0 results.
- Real catalog opening, unsigned IDs, misses, and 1,024-entry dense/sparse
  catalog lookups from the same generated files.

The fixture verifies balanced stacks and preserved D2–D7/A2–A6. Exec/DOS
dependency gateways implement memory, file, list, semaphore and patch services;
utility math/calendar gateways use independent host arithmetic and Gregorian
dates. Consequently this qualifies locale behavior under those services, not
the implementations of Exec, DOS, utility, or a complete booted Workbench.

## Translation lookup measurements

Cycles below include the negative-vector JMP and the routine's RTS. They
exclude the caller's JSR. Both images run on the same flat memory bus, without
chip RAM contention or hardware wait states.

| Catalog / query (1,024 strings) | Original V40 cycles | CopperOS cycles |
| --- | ---: | ---: |
| Dense, first | 116 | 176 |
| Dense, middle | 24,692 | 176 |
| Dense, last | 49,220 | 176 |
| Dense, absent after last | 49,234 | 122 |
| Sparse, first | 116 | 1,572 |
| Sparse, middle | 24,692 | 372 |
| Sparse, last | 49,220 | 1,632 |
| Sparse, absent after last | 49,234 | 1,708 |

The original's first entry is cheaper; its cost then grows with the number of
entries visited. CopperOS dense hits take 14 instructions independent of the
catalog size. Sparse lookup is logarithmic. Both CopperOS paths allocate no
memory and take no locks.

## Reproduction and limits

```powershell
$env:COPPEROS_ORIGINAL_LOCALE = 'C:\private-reference\locale.library'
$env:COPPEROS_ORIGINAL_IFFPARSE = 'C:\private-reference\iffparse.library'
dotnet test tests/Locale/CopperOS.Locale.Tests.csproj -c Release -p:CopperOSUseLocalCopperSharp=true
```

The generated `artifacts/locale-tests/original-comparison.json` contains the
reference hashes, individual observations, differences and measurements. The
separate native export tests compile and execute the production library for
68000, 68020 and 68040, checking immutable image bytes after each call. They
exercise all eight installed patches, stack callbacks, translation/fallback of
DOS errors, preference changes, retained string pointers, and resident lifetime.

This is a finite comparison, not proof of all possible inputs. The separate
[Workbench qualification](workbench-qualification.md) covers real German and
Swedish language drivers and IPrefs notifications with the supplied disks;
it distinguishes the original Kickstart control from the passing native CopperStart boot.
Other language drivers and physical-machine timing remain outside this matrix.
MorphOS register extensions are
covered by the implementation tests and official ABI/source references, not
by the classic V40 binary.

Native results use a staged compiler/SDK set: compiler SHA-256
`509AAB22A1FA15AC8B3EBEED9DD9E991E7CCFF731D5571E27E8FA049577ABC9D`, SDK
`6F240CB97694BB9393D54B471D1741DF791F003E9781583FA7B01F937527E687`.
The library build receipt records all producer hashes. Management/date code is
compiled with peephole optimization **Disabled**: FixedPoint builds exhibited
layout-sensitive 68000 date formatting failures, including with the pinned set.
The separately emitted GetCatalogStr assembly and its measurements are unchanged.
The later sibling set (compiler `B7CC10A1…`, SDK `6596369B…`) is not qualified.
No sibling compiler or SDK source was changed.
