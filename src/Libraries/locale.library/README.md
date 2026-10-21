# CopperOS locale.library

Original C# implementation compiled to a loadable M68k disk library. The
default artifact targets the 68000, has no FPU or managed runtime dependency,
and installs as `LIBS:locale.library`. Version 53.9 identifies the register-ABI
feature level; it does not expose MorphOS System V functions to 68k callers.
The builder disables compiler peephole optimization for management/date code
after layout-sensitive 68000 failures. The handwritten GetCatalogStr routine
retains its constant-time dense path and logarithmic sparse path.

## Public interface

All Kickstart/Workbench 3.1 public locale vectors are implemented at their
original offsets: CloseCatalog, CloseLocale, ConvToLower, ConvToUpper,
FormatDate, FormatString, GetCatalogStr, GetLocaleStr, the eleven IsXXX
functions, OpenCatalogA, OpenLocale, ParseDate, StrConvert and StrnCmp.
Management vectors implement open counts and memory reclamation. The public
Locale and Catalog prefixes retain their 168-byte and 28-byte packed layouts.

OpenLocale(NULL) shares the configured default, initially English/US. Init
reads `ENV:sys/locale.prefs` when present. Named locales read IFF PREF/LCLE
and optional CTRY chunks, retain country strings and all ten preferred
languages, and open the first available `LOCALE:Languages/<name>.language`.
Each driver method is selected by its language-function mask; absent methods
use the built-in ISO-8859-1 English implementation. Old defaults stay alive
for the system lifetime to protect lock-free default-locale operations and
returned strings when preferences change.

Catalog selection supports explicit paths, PROGDIR:/LOCALE: catalog searches,
preferred-language fallback, OC_Language, OC_BuiltInLanguage, exact OC_Version
checks, TAG_MORE/TAG_SKIP/TAG_IGNORE, and cached open references. OC_BuiltInCodeSet is ignored, as in V40; OC_CodeSet alone selects translation
encoding, independently of tag order. On a cache
miss, an available LOCALE: precedes an unavailable PROGDIR:; the availability
probe suppresses requesters and restores the process window pointer and IoErr.
Unused catalogs are reclaimed by an Exec V39 memory handler. The handler never
waits for the library semaphore or runs during an unfinished catalog mutation.

The MorphOS additions carried by this M68k interface are:

- RexxHost at -30, including its D0 status/A0 Argstring result convention.
- LANG_NAME (GetLocaleStr ID 51), the `%F` date format, and 64-bit `%L`/`%ll`
  integer formatting through the existing FormatString registers.
- OC_CodeSet, OC_BuiltInStrings (CatComp array and block), and OC_ConvertStrings.
  Catalog text converts between legacy Latin-1, UTF-8 and big-endian UTF-32.
  Characters unavailable in Latin-1 become `?`; malformed encoded input fails
  catalog loading. Per-open built-in strings cannot modify a shared catalog.

The Unicode, normalization, snprintf and timezone functions in the MorphOS
native System V section are deliberately excluded: they have no M68k register
ABI in the inspected function table. No offsets are invented for them.

## Lookup performance

Catalog opening validates the IFF structure and builds an immutable index.
Dense ID ranges use a direct pointer table (four bytes per ID slot); sparse
ranges use sorted ID/pointer pairs (eight bytes per entry) and binary search.
Density requires fewer than twice as many slots as unique IDs. Sorting uses
heapsort; duplicate IDs keep the first on-disk occurrence. If the optional
dense table allocation fails, the existing sparse index remains usable.

The packaged GetCatalogStr vector is a small, relocation-free 68000 routine
emitted by `LocaleLookupEmitter`. Dense hits use only volatile registers and
do not touch the stack. Sparse searches save only the additional registers
they use. Both paths preserve unsigned 32-bit ID ordering, missing/empty string
semantics and caller fallback pointers. Lookup allocates no memory, acquires
no lock, scans no string payload and performs no multiplication or division.
The C# core remains the host-testable implementation of the same format.

`tests/Locale/NativeExportTests.cs` executes the actual packaged vector in
Copper68k and writes instruction/cycle measurements to
`artifacts/locale-tests/M68000.json` (also M68020/M68040). These are emulator
measurements with a flat memory bus. The separate original-binary comparison
below measures both libraries with the same bus and CPU model.

The checked 68000 build resolves a dense hit in 14 instructions / 176 cycles
including the six-byte library-vector JMP (13 instructions / 164 cycles in
the lookup body). This excludes the caller's JSR. The count is independent
of the number of catalog strings.

## Build and verification

From the repository root, with CopperSharp68k and CopperMod sibling checkouts:

```powershell
& tools/Locale.LibraryBuilder/build_locale.ps1 -Cpu 68000
dotnet test tests/Locale/CopperOS.Locale.Tests.csproj -c Release -p:CopperOSUseLocalCopperSharp=true
```

The first command writes `artifacts/locale/68000/locale.library` and provenance,
framework and native-compatibility reports. Use `-Cpu 68020` or `-Cpu 68040`
for those targets. The builder rejects managed allocation sites, exception
regions, runtime features/helpers and external unresolved native targets.
Compilation uses the Resident profile and explicit caller-provided dependency
bases. Library code and constants remain unchanged across calls. Writable patch
thunks belong to the library's positive base, not the shared code hunk.

Tests cover catalogs, encodings, allocation rollback, selection/cache ownership,
locale preferences and language masks, collation-key equivalence, Gregorian
calendar conversion across 2100, date hooks, positional/word/long/64-bit
formatting, grouping, ARexx, actual register adapters, memory reclamation and
resident patch lifetime on three CPUs. DOS reads in the native fixture are deliberately
short to exercise streaming read completion. Native assertions check register
preservation and every byte of the shared image after each exported call.

## Compatibility qualification

Initialization opens utility.library and installs all eight system patches
through Exec SetFunction, setting `lb_SysPatches`:

| Library | System LVO | Replacement | Private locale LVO |
| --- | ---: | --- | ---: |
| Exec | -522 | RawDoFmt | -186 |
| utility | -168 | Strnicmp | -192 |
| utility | -162 | Stricmp | -198 |
| utility | -180 | ToLower | -204 |
| utility | -174 | ToUpper | -210 |
| DOS | -744 | DateToStr | -216 |
| DOS | -750 | StrToDate | -222 |
| DOS | -978 | DosGetString (classic private ABI, D1 error ID) | -228 |

Per-instance thunks bind the locale base while preserving the incoming foreign
A6. Code caches are cleared before publication. RawDoFmt bridges D0/A3 callbacks
through a Hook on the stack, retaining callback updates to A3 between characters.
MorphOS's string, serial and count callback constants are also supported.
The DOS date adapters also use stack workspace. Ordinary formatting and date
calls allocate nothing. DOS error lookup uses the current `sys/dos.catalog` and
chains the saved DOS getter for untranslated IDs. Catalogs are retained with
their corresponding default locales, preserving borrowed error-string pointers
across preference changes. Installing the patches pins the library, as in the
reference implementation; Expunge can reclaim closed catalogs but cannot unload
code which the system or another patch may still call.

Classic DOS Fault and public DosGetLocalizedString call the private -978
getter. The public -924 entry itself is not the classic locale patch target.
The complete installed target set is compared with the original V40 binary.
LocalePrefsUpdate also publishes global ENV:Language and utility's language
selector, using stack workspace for the language basename.

`OriginalLocaleCompatibilityTests` loads the locally supplied Workbench 3.1
`locale.library` 40.4 and `iffparse.library` 40.1 as unmodified, relocated HUNKs.
Their actual 68000 code initializes the reference, installs its patches through
LocalePrefsUpdate, opens the same generated CTLG catalogs, and executes the same
test cases as a freshly compiled CopperOS image. Only dependencies in Exec,
DOS, utility and Intuition are test gateways. This is a focused binary
comparison, not a full booted Workbench or hardware qualification.

Set `COPPEROS_ORIGINAL_LOCALE` to the reference file and optionally
`COPPEROS_ORIGINAL_IFFPARSE` if iffparse.library is not alongside it. The existing
sibling CopperStart Workbench extraction is used when present. No proprietary
binary is checked into the repository. Results, reference hashes and comparative
lookup timings are written to `artifacts/locale-tests/original-comparison.json`.
See [qualification.md](qualification.md) for the checked matrix and measurements.

The comparisons cover all 256 byte values for English character operations,
locale strings, collation, patched utility calls, relative and numeric dates,
permissive DOS time parsing, DOS error strings, RawDoFmt callbacks and return
values, and catalog hits/misses with dense, sparse and unsigned IDs. Observed
classic details include uppercase AM/PM, printable whitespace, the historical
multiplication/division case pair, case-sensitive secondary collation, numeric
or abbreviated DOS months, ignored trailing time text, and V40 RawDoFmt's
`stream+2` return when a long grouped D/U conversion occurs. That last quirk is
confined to the system adapter; public FormatString retains its documented
next-argument pointer behavior.

API authorities: [classic locale autodocs](https://www.theflatnet.de/pub/cbm/amiga/AmigaDevDocs/locale.html),
[MorphOS locale autodocs](https://morphos-team.net/sdk/locale.html),
[MorphOS locale header](https://morphos-team.net/sdk/includes/libraries/locale.html),
[preference structures](https://morphos-team.net/sdk/includes/prefs/locale.html),
and the register/native split in the official
[MorphOS 3.20 locale source release](https://morphos-team.net/files/src/3.20/locale.library.tar.bz2).
Reference downloads and binary executions stay in ignored artifacts; no
reference implementation is compiled into or included in the shipped library.
