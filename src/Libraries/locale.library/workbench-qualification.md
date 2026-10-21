# Workbench 3.1 integration qualification — 2026-10-20

<!-- one-mib-display-followup -->
## Display and startup follow-up

**PASS: full Workbench 3.1 locale verification on 512 KiB Chip + 512 KiB slow,
with no Fast RAM.** The normal full-frame MC68000 replay full-one-mib-v693
reaches EndCLI at frame **67,920**, with Workbench phase 1, desktop ready,
and CopperBench, IPrefs, ConClip and Intuition refresh alive. English → German →
Swedish → English matches all **28** original locale/status values, including
DOS Fault 205 and 103. Raw commandFailures=[]; no failure is filtered. There is
no halt, low-PC failure, boot error, unsupported active feature, host OS overlay
or scalar execution shortcut. The combined receipt is qualification-v704.json.

The final raster has **zero unexpected pixels**, 100 visible pointer pixels,
seven terminated unused sprite channels, and a zero sprite terminator
(pixels-v694.json). The screenshot was visually reviewed. The strict locale
comparison passes with --require-clean-commands (verification-v694.json).
Public free-list totals agree with all recorded chunks: 43,408 Chip bytes free
(largest 14,104), 7,104 slow bytes free (largest 6,104), and zero Fast RAM.
All saved task stack pointers are within bounds; passive stack contents are
not a worst-case stack guarantee (memory-v694.json, stacks-v694.json).

The screen-owned Copper prefix reloads all eight sprite pointers each field,
then continues the original graphics list through COP2. Sprite zero uses the
owned pointer image; channels 1–7 point to its zero terminator. Rebuild and
teardown retain the original list and blank pointers before freeing storage.
The compact input path retains its motion/repeat wake bookkeeping. This uses
84 Chip bytes and 96 public screen bytes per screen. In the passive full-boot
control, v597 has 590 stray pixels and no visible pointer; corrected v629 has
zero stray pixels, 100 visible pointer pixels, and seven terminated channels.
This checks the actual bitmap, palette and sprite overlay, without claiming
vendor Workbench pixel identity.

CopperBench requests SimpleRefresh for its own backdrop and uses its existing
BeginRefresh/PaintDesktop/EndRefresh handler. The physical 1 MiB drawer test
passes against the final desktop in backdrop-v670-results/backdrop.trx: open SYS: with mouse input, close
it with Escape, wait for the drawing lock, then compare every backdrop bitplane
byte below the live title with the pre-open snapshot. Other applications retain
their requested refresh mode.

Script-context tracing identifies KEYMAPS Assign RC=20 and the later Path RC=10.
ConClip itself returns zero and keeps its daemon alive. The supplied Workbench
archive lacks the Devs/Keymaps, Tools and Tools/Commodities directories. Private
media preparation restores these paths and real d, s and usa2 keymaps from the
supplied Storage disk, with readback hashes. LOCALE: DF2: also conflicts with the
supplied Locale volume name. Original and native C:Assign both return
RC=20 / IoErr=203 for a genuine volume-name conflict, so that check stays intact.
The private Locale copy is relabelled LocaleSupport; preparation verifies that
every byte outside root block 880 is unchanged. All source disks/archives and
the existing fixture Startup-Sequence remain byte-for-byte unchanged. No raw
command failure is suppressed or filtered.

Independent original/native C:Assign controls exposed a DOS RunCommand bug:
it required a NUL after a counted argument span. Preflight now maps exactly the
counted bytes. The unchanged old ROM fails the valid Assign controls with
ERROR_BAD_NUMBER (115); the corrected ROM passes. All 12 original/native 1 MiB
startup controls pass without skips in startup-controls-v636-results/startup.trx
(ten Assign and two ConClip), including genuine missing paths and volume-name
conflicts. The focused RunCommand/input host suite passes 44 cases.

The clean-media v638 and v642 replays have raw commandFailures=[] and all 28
locale/status values matching, but fail desktop initialization. Private stage
and allocation traces v649/v652 isolate a 16,432-byte MEMF_PUBLIC|MEMF_CLEAR
Layers aggregate-lock frame requested before preparing the screen background
layer, even when capturing zero existing layers. Its allocation fails 222 times;
both screen bitplanes, palette, MakeVPort, MrgCop and sprite prefix have already
succeeded. The diagnostic images are excluded from qualification.

Aggregate locks now use a fixed 336-byte frame with eight inline layer pointers
and a fixed directory. Larger sets add fixed 280-byte blocks holding 64 pointers
each, addressed in constant time. The full 4,096-layer limit is unchanged. Header,
owner, generation, directory-index, exact extent and unused-slot checks run
before teardown; Clear/Free sizes are compile-time constants. Partial allocation,
rollback, nested unlock, task death and reset release each block exactly once.
A proposed screen retry did not fix v655 and was removed; the production image
contains neither retries nor diagnostic markers.

The Layers host sweep passes 547 cases in layers-v665-results/layers.trx. One
source-inspection check is unavailable because build/Generate-LayersNativeDirectVectors.ps1
is absent. The earlier sweep also had a transient managed raster allocation
assertion; the isolated old-Layers control and repeated new-Layers sweep both
pass it. The exact frozen provider passes 23 focused cases in
compact-v667-results/compact.trx: 18 host cases and five emitted MC68000 cases,
including 8/9/73-layer boundaries, forged block sizes and a 512-byte free-block
case. With the original fixed-workspace core and test visibility restored, that
last native case returns zero instead of one
(compact-v668-before-results/compact-before.trx). The first old-provider attempt
could not access the internal host setup method and is invalid control evidence.

The qualification cohort is ROM/Shell v617, desktop v659, runner v620. Shell
remains 318,320 code bytes in a byte-identical 329,312-byte HUNK. Desktop has
257,456 code bytes in a 267,776-byte HUNK, retaining all 19 GUI v9 exports; latest
GUI v10 services are outside this full-machine qualification. Each image's own
producer role passes the 27-check audit (cohort-v666.json). CopperStart Release
build passes with zero errors and 20 warnings (copperstart-v666-build.log);
CopperOS Release previously passed with zero errors and 39 warnings, with no
subsequent production OS change. All 39 native desktop regression cases pass without skips in
native-v664-results/native.trx. The full replay v660 passes the strict locale, startup-command and raster checks.

The physical-input regression now passes both 320- and 640-pixel profiles
without skips (input-v684-results/input.trx, input-summary-v688.json). Its old
reference expected a bar-shaped close mark instead of the documented inset X,
and ignored the Preferences pointer hotspot (-1, 0). The pressure snapshot also
used a fixed four-frame delay that could stop during release feedback. Capture
now waits for the physical button ring and input worker to settle while all
16 notifications remain unreplied; every bitmap and display pixel is still
compared. Teardown verifies the exact reset-lifetime Layers/graphics/utility
pins instead of requiring their permanent graphics extension to unload.

Both widths execute three screen/window lifetimes: 18 captures each, 214/211
balanced event records, repeated full-pool overflow and recovery, close-button
release/cancellation, focus, repeat/key state, console restoration, and identical
497,336-byte free-memory totals before and after input-owner cleanup. The only
retained dependencies are the expected one Layers, one graphics and one utility
reference. Earlier failures in v628/v630, v676 and v680 remain controls. Production
painting, input capacities and library lifetimes were not changed to satisfy the
test. CopperStart Release builds with zero errors and 22 warnings
(copperstart-v683-build.log).

The fresh full-frame 1 MiB replay v681 passes after the input test fix. The
remaining locale audit then identifies an independent catalog-tag defect:
OC_BuiltInCodeSet (+2) was incorrectly selecting translation output encoding.
Original V40 execution retains cat_CodeSet=0 for that hint; the old CopperOS
build instead publishes 1/2 and can return an empty byte string after selecting
UTF-32. The before-fix host and binary controls both fail, with three concrete
differences (locale-v689-before-results/locale-before.trx). OC_CodeSet (+5) alone
now controls translation encoding. Cache ownership and lookup assembly are unchanged.

All **53 locale tests pass without skips** in locale-v699-results/locale.trx,
including **4,728 original-binary observations with zero differences**. Native
68000/68020/68040 calls cover LANG_NAME, signed/unsigned 64-bit formatting,
six cross-encoding pairs with conversion on/off and conflicting built-in hints,
CatComp array/block adapters and simultaneous per-open built-ins, and ARexx
D0/A0 success/error results. Register preservation and every shared code byte
are checked after calls. The compiler's seven 68000 and fourteen 68020 arithmetic
fault guards remain intact; a new zero-count audit assertion was identified as
invalid and corrected using method/byte maps (locale-faults-v696/sites*.json).
All actual native calls return successfully. Dense hits remain 14 instructions /
176 flat-bus cycles, and sparse timings match the prior qualified measurements.

The final v693 full boot uses the corrected 53,152-byte library, SHA-256
4CBABF1EF8E436D2CA6B9911F2F9E61219751B5E6399DD67A96F2889936F1122,
identical to both the native 68000 test image and the original-comparison image.
Its producer and private-filesystem readback hashes match. The startup script,
source media, capacities, memory profile and execution mode remain unchanged.
Verification-v694, pixels-v694, memory-v694 and stacks-v694 pass; the screenshot
was visually reviewed. Current production library/builder/test sources exactly
match the frozen tested sources (source-provenance-v701.json). Current locale
test sources build with zero errors and warnings (locale-source-v695-build.log);
the final frozen test producer also builds cleanly. Full-machine GUI qualification
continues to cover the stated GUI v9 provider cohort.

<!-- /one-mib-display-followup -->

<!-- one-mib-memory-followup -->
## Earlier 1 MiB memory follow-up

**PASS: the full Workbench 3.1 locale replay now completes on 512 KiB Chip +
512 KiB slow RAM, with no Fast RAM.** Full-frame MC68000 run
full-one-mib-v597 reaches EndCLI at frame **64,736**, with Workbench phase 1,
desktop window 13,012,360, and CopperBench, IPrefs, ConClip and the refresh worker
alive. English → German → Swedish → English matches all **28** original
locale/status values, including DOS Fault 205 and 103. There is no CPU halt,
low-PC failure, boot error or unsupported active feature. Source media remain
unchanged; no host OS overlays or scalar execution shortcuts are used.

The fixed-workspace Shell contains **318,320 code bytes** in a **329,312-byte
HUNK**, compared with 494,494 / 526,980 bytes originally. A native script reader
packs its 21,248 bytes of mandatory buffers into the existing runner allocation's
trailing storage. Every buffer retains its separate span and full capacity.
The complete 21,416-byte allocation is reclaimed together, avoiding interleaved
small allocations that prevented a later 20 KiB screen-plane allocation.
Cleanup also supports independent-buffer readers. The nullable numeric workspace,
on-demand .KEY storage, exact-size cleanup and prompt retirement remain in place.
The client also forwards handler creation through additive CSNS operation 6,
removing 28,384 code bytes of duplicate DOS/Exec launch implementation. The full
handler implementation stays in the matching ROM. Public commands and limits
are unchanged.

The current-source Shell tests pass **208/208**, layout/cleanup tests **16/16**,
and additional arena ownership cases **3/3**. The matched pair passes **37/37**
native/original cases without skips, including the handler packet test for both
classic modes and negative priority. Ten host handler/ownership cases also pass.
The separate native 1 MiB case runs two backtick substitutions, a 256-code Fault
command and EndCLI with
return code zero. The Fault argument list is produced by 16 × 16 variable
expansion; an earlier long literal OFS script-line fixture timed out during
floppy reads and is not a passing I/O performance qualification.

Both solution Release builds pass with zero errors (CopperOS: zero warnings;
CopperStart: 20 existing dependency warnings). Each image's own producer role
passes the 27-check cohort audit. The Shell and desktop have no managed runtime
helpers/features, external native targets or fatal compiler sites.

The preceding arena-only run, full-one-mib-v566, failed to create a desktop.
The instrumented v586 replay identified OpenScreen stage 14,
CreateUpfrontHookLayer, after both bitplanes and the display lists succeeded.
Forwarding handler creation removes the remaining duplicated launch code and
allows the regular, uninstrumented desktop to complete in v597.

The passing replay uses matched ROM/Shell v591 and runner-v597. The earlier
v592 replay was rejected before boot by the frozen runtime hash guard because
its runner still referenced DOS v563; it is invalid. Final public free lists are
consistent: 49,496 Chip bytes and 7,288 slow bytes remain. Stack snapshots are
passive observations, not worst-case stack bounds.

Visual review confirms the desktop backdrop, title and SYS: disk icon. Pointer
striping and a small white display mark remain visible in the captured frame;
pixel identity with vendor Workbench is not qualified. The then-reported startup returns
Assign RC=20 and ConClip RC=10 were retained as non-fatal issues; the later script
trace above corrects the attribution of RC=10 to Path. The ConClip daemon is alive. These are retained in the boot receipt rather than hidden by
the successful locale comparison.

Receipts are under artifacts/desktop-memory/: shell-v591/, rom-v591/,
inputs-v591/runtime/, native-v598-results/native.trx,
host-shell-v577-results/shell.trx, ownership-v588-results/ownership.trx,
host-arena-v585-results/arena.trx, handler-v595-results/handler.trx,
handler-host-v596-results/handler-host.trx, fault-v592-results/fault.trx,
cohort-v592.json, verification-v600.json, memory-v600-final.json and
stacks-v600-final.json. The full boot receipt and reviewed screen are in
full-one-mib-v597/.
The full replay deliberately pins the independently
qualified desktop-v411 / inputs-v411/runtime GUI v9 cohort with 19 exports.
The latest parallel GUI v10 services are outside this full-machine cohort;
current-source builds and the four passing Clock/window cases are separate scope.
The locale payload remains the unchanged 53,112-byte image identified below.
<!-- /one-mib-memory-followup -->


## Earlier full 2 MiB Fast RAM qualification

**Full CopperStart Workbench locale qualification passes on the 2 MiB Fast RAM
profile.** The original startup
and real IPrefs complete English → German → Swedish → English, with all six
public observations matching the original locale.library. Startup reaches EndCLI
at frame 95,272 in the current-source Shell replay, with IPrefs, ConClip,
CopperBench and the timer worker alive,
without a CPU halt, low-PC fault or unsupported active feature. Source media
remain unchanged. The independent standard startup and shorter IPrefs diagnostic
also pass their separate checks.

## Additional memory work and qualification scope

The earlier size-pass Shell cohort contains **342,588 code bytes** in a **354,288-byte HUNK**,
a 30.7% / 32.8% reduction from the original 494,494 / 526,980-byte build. The
Freestanding HUNK builder explicitly opts into the compiler's generated code
size passes; its default policy and persistent static state remain unchanged.
The complete command set and all 19 desktop exports remain present.

Two compiler regressions were corrected during this work. Helper inlining now
reruns cleanup in the rebuilt method scopes instead of rescanning the whole
image for every rewrite. A platform-base entry adapter explicitly jumps to its
managed entry when hot-method clustering places another method first. Both have
native regression cases and failing controls using the prior compiler. All
1,302 selected compiler cases pass without skips, including freestanding static
state, entry relocation, and the scoped-cleanup work bound.

The public CLI default-stack field remains in longwords. The original ROM
records 1,024 longwords initially, and `Stack 8195` records 2,048 (8,192 bytes,
rounding down). CopperShell's byte API converts at the boundary; inherited child
and RunCommand stacks convert back to bytes. Explicit NP_StackSize tags remain
byte-valued. All 32 focused bridge cases pass. Separate private service budgets
avoid imposing a 32 KiB command stack on the low-memory system.

The production Shell loads the installed locale library before the larger
desktop module can fragment Chip RAM, preserving IoErr around the optional
open. Its generated guest thunk preserves Amiga callee-saved registers around
the desktop process entry. The ROM releases its 8 KiB bootstrap path/script
workspace before handing off to the full Shell; the recovery CLI retains it.
Screen-owned bitplanes allocate separately in Chip RAM, with private ownership
records and rollback on partial failure. Caller-owned bitmaps retain their
original lifetime.

All builds use immutable source/assembly snapshots. ROM, Shell client and
desktop producer roles are audited independently; the DOS client is never
substituted for the ROM's full provider. The production floppy profile is
Classic, and the 30,195-byte graphics resident at FF89F0 is compiled with its
matching ROM bootstrap into a single 512 KiB ROM. The locale library itself is
the unchanged 53,112-byte payload identified below.

The intermediate `desktop-memory/full-one-mib-v206` and `v210` attempts
match all 28 original locale/status fields with IPrefs and ConClip alive, but
LoadWB returns 5 and CopperBench does not remain present. They do **not** qualify
the complete Workbench startup. The latter includes independent screen planes
but still retains the bootstrap buffer through startup. Their strict
comparisons correctly fail the Workbench-task requirement.

## Earlier Shell footprint follow-up — 2026-10-19

**CopperShell now loads and executes scripts on 1 MiB. Full Workbench locale
verification on that profile still fails at the desktop allocation.**

The current-source native build is `shell-production-v147/`: code shrinks from
494,494 to **378,966 bytes** (23.4%), and its HUNK file from 526,980 to **394,764
bytes**. The file SHA-256 is `A7B662A1065C5F6A777B1556EDAF49B808349FA09EEF0365027BF854B8889D06`.
Execute also builds (6,376 bytes). Both have zero fatal sites, managed allocation
sites, runtime helpers/features and unresolved native targets. Release builds
of the complete CopperStart solution pass with zero warnings/errors.

The Shell uses a separate DOS client compiler input. Ordinary operations call
the installed public M68k DOS vectors. A versioned CSNS V2 register/packed-record
interface shares the ROM filesystem entry and process ownership operations.
The full provider remains in the ROM/replay runtime. No compiler-private call
address, guest memory patch, host OS overlay or scalar diagnostic is used.

Ordinary LoadSeg now keeps a **4,096-byte** header/relocation window and reads
CODE/DATA directly into final guest hunks. It no longer stages a whole file.
Reusing the cached payload prefix and reading the remaining payload in large
spans avoids restarting OFS block-chain lookup every 4 KiB. InternalLoadSeg's
arbitrary callback state machine is unchanged. All 24 focused loader/callback
tests pass, including short reads, window crossings, relocation and rollback.

The genuine native regression
`FullShellRunsScriptAndDiskCommandWithOneMiBAndNoFastRam` passes: the complete
Shell boots with 512 KiB Chip, 512 KiB slow and no Fast RAM, runs Set/If and a
native disk command, checks its public receipt, and reaches EndCLI. All 15
selected native/original service and compiler checks pass without skips.

`full-one-mib-v149/` uses the **current-source** Shell and original startup.
It reaches WBInit at frame 13,320, then returns to the prompt at frame 14,168
with RC 20 / DOS 103. Boot itself has no error, halt or low-PC fault. The desktop
needs a 267,492-byte code hunk (267,490 unpadded code bytes), while the largest
free chunks are only **111,776 slow bytes** and **48,176 Chip bytes**; public
free totals match the MemChunk sums (112,200 / 80,944 bytes). IPrefs and all four
language probes remain unexecuted on 1 MiB. The strict same-profile comparison
correctly fails. A before control with the old Shell and the same updated ROM
fails at frame 1,016 with DOS 103, before Shell execution.

| Private evidence under artifacts/locale-workbench | Result |
| --- | --- |
| `shell-production-v147/`, `shell-production-v147.log` | Current-source Shell/Execute and exact compiler inputs |
| `shell-footprint-rom-v145/` | Streaming-loader/CSNS ROM, SHA-256 52222410DA420227F07982269A2DD7FFBF2D9870106D5A28D125B3EC19A0B93E; 488,758 code bytes plus unchanged 31,998-byte graphics image |
| `shell-cohort-v149.json` | All 11 ROM / 9 current-source Shell / 7 retained desktop producers match their independent role inputs |
| `loader-tests-v148/loader.trx` | 24 focused host checks pass |
| `shell-native-tests-v149-results/native.trx` | 15 native/original checks pass |
| `shell-one-mib-native-v151-results/shell.trx` | Full Shell script/disk-command/EndCLI regression passes on 1 MiB |
| `full-one-mib-v149/`, `shell-memory-v149.json`, `native-one-mib-comparison-v149.json` | Real full-startup attempt and remaining desktop allocation failure |
| `shell-before-one-mib-v152/` | Old Shell fails to load on the same new ROM/profile |
| `managed-build-v149.log` | Complete Release solution: zero warnings/errors |

The final native packet explicitly contains 17 TagItem pairs. Its actual stack
home is 136 bytes in both pinned and current-source maps. Earlier process-sharing
prototypes reserved only eight bytes despite StructLayout.Size; the v137/v139 Shell images and composed v140 cache
are rejected, and interrupted v140 replays establish no compatibility result.
Their failure must not be reported as a passing cohort. The build/cache guards
reject the known short packet home. locale.library remains the unchanged
53,112-byte payload with SHA-256
`196FB304DF7E49CED9B53A41CF06D26B541A73DD2CED73E72F869F1D365489DA`.

The paired **2 MiB Fast RAM** run `full-fast-2m-v149/` passes the full
original startup at frame **95,272** with the current-source Shell. Real IPrefs
performs English → German → Swedish → English, and all 28 status/value fields
match the original control (`native-switch-v149.json`, no errors). Both DOS
Fault 205 and 103 translations match in all four phases. IPrefs, ConClip,
CopperBench and the timer remain alive; no halt, low-PC fault or unsupported
active feature occurs, and all source media remain unchanged. The native
framebuffer was visually reviewed; pixel identity with vendor Workbench is not
claimed. This successful larger-memory run does not qualify the 1 MiB boot.

The good native cache passes `shell-cache-v154-results/cache.trx`. The known
8-byte-packet image fails `shell-cache-rejected-v154-results/rejected.trx`
with the explicit packet-size diagnostic, before guest execution.

## Earlier 1 MiB keyboard/palette follow-up

The exact PAL OCS 68000 / 512 KiB Chip + 512 KiB A501 slow / zero Fast RAM
profile has now been replayed separately. The original Kickstart/Workbench
control completes at frame 4,736 with IPrefs and Workbench alive; all four
English/German/Swedish/English probes match the values below.

The earlier v118 full CopperStart startup **does not pass on 1 MiB**. It stops at frame 856
with DOS error 103 (`NoFreeStore`), before IPrefs or the desktop is loaded.
CopperShell has a single 494,496-byte code hunk (526,980-byte HUNK file).
At failure, the public memory lists show 459,944 free Chip bytes and 344,752
free slow bytes, but their largest free chunks are only 427,176 and 334,816
bytes respectively. The loader cannot allocate that hunk across two banks.
The old loader first requests a whole-file 526,980-byte staging allocation,
which already exceeds both largest chunks; the later code allocation is also
too large. Both problems are addressed by the Shell follow-up above. This v118
receipt does not run the locale probes.

Private evidence is under `artifacts/locale-workbench/`:

| Evidence | Scope |
| --- | --- |
| `original-one-mib-v117/` | Original full startup and all four locale phases on 1 MiB |
| `original-one-mib-comparison-v120.json` | All 28 reference status/value observations match the earlier control |
| `full-one-mib-v118/`, `native-one-mib-comparison-v118.json` | Failed full native startup, exact RAM sizes, DOS 103, unchanged media |
| `one-mib-memory-v118.json` | Passive public MemHeader/MemChunk measurements and Shell hunk size |
| `one-mib-cohort-v124.json` | All 27 ROM/Shell/desktop producer hashes match runtime and runner |
| `one-mib-tests-v119-results/one-mib.trx` | Keyboard/mouse/close journey and 14 affected service checks pass |
| `one-mib-final-v124-results/final.trx` | All ten checks pass: nine console cases including public DOS Read during a busy command, plus physical SYS drawer opening and exact palette stability across four fields on 1 MiB |
| `one-mib-keyboard-before-v123/results/negative.trx` | The public DOS Read regression fails against ROM106: a single typed `a` yields `aa\n` (three bytes) |
| `managed-build-v118.log` | Full Release solution builds with zero warnings/errors |
| `verifier-compatible-v124.json`, `verifier-wrong-profile-v124.json` | Earlier 2 MiB locale proof still passes; labeling it as 1 MiB is rejected solely by the new RAM-profile gate |

All 25 focused native checks pass, without skips. They include original-ROM
service controls and different RAM profiles; the physical keyboard/mouse/close,
busy-command, and focused drawer/palette journeys explicitly use 1 MiB.

The new ROM is `one-mib-native-v117/`, SHA-256
`900BD3E3844052A3B9D4EC29EB0E6001B4AE719CC0B3C3D5635436AE0381DA83`;
the desktop is `one-mib-desktop-v118/`, SHA-256
`B95A0FD41B417DFCE328C5081FC35E69189ADF3B1B021D0C7F53E2AC77965FCF`.
The Shell and locale.library bytes are unchanged. The runtime is
`one-mib-inputs-v124/runtime`, with `one-mib-runner-v118`; all 11/9/7 producer
checks pass. The frozen System source is the previous qualified source plus
the console receiver/export, and the frozen desktop is v114 plus the existing
palette and close-on-release corrections. It excludes concurrent GUI work.

The keyboard failure was a delayed physical handshake: a cooperative redraw
could prevent the CON: task from polling within the keyboard's retry interval,
yielding F9 and a duplicate key. The permanent console now receives and
acknowledges bytes in VBlank, then passes them through a 31-byte usable queue
to its task. Only task context edits cooked text or publishes GUI input.
Synchronization/power-up markers are consumed by the transport. A native busy
command regression exceeds the retry interval without scheduling: public DOS
Read returns exactly `a\n` with the fix, while ROM106 duplicates the first
character and returns `aa\n`. It does not assert the private queue layout.

The palette correction retains pointer pens 17–19 through SetRGB4 in the
ColorMap and Copper lists. The earlier direct hardware writes were overwritten
on the next field. The exact four-color active-display assertion passes with
this correction; its expected colors and rectangle remain unchanged.

The frozen test fixture also needs the already implemented close-on-release
contract, and its live title must be checked against the retained memory totals
and clock rather than a newer memory-only title. These fixture differences are
separate from the keyboard and palette fixes. Current shared GUI sources are
preserved; qualification uses the explicit frozen source snapshots.
The broader GUI test in `one-mib-palette-v120-results/` passes the palette,
retained title, and basic paging checks, then fails at newer nested-drawer
navigation that is absent from the frozen v114 desktop. That failure is retained;
the v124 palette check ends after the root drawer and four field captures and
does not claim nested navigation or application launching.

## Earlier qualified 2 MiB Fast RAM cohort

| Evidence | Result |
| --- | --- |
| `iprefs-minimal-boot-v114/`, `iprefs-minimal-comparison-v114.json` | Four phases match the original; EndCLI at frame 48,808; IPrefs and CopperBench remain alive |
| `full-boot-v114/`, `native-switch-v114.json` | Full startup passes at frame 87,592; four probes succeed and match all six original fields |
| `final-native-v115/` | All 14 relevant original/native service checks pass |
| `display-comparison-final-v115.json` | All 44 display IDs, aliases, DISP/DIMS/NAME records and name mutations match |
| `exec-host-v107/` | 115 focused host checks pass |
| `managed-build-v116.log` | Full Release solution builds with zero warnings/errors |

All paths above are under ignored `artifacts/locale-workbench/`. The exact
runtime is `display-inputs-v115/runtime` (tests only differ from v114); the runner
is `display-runner-v114`. The ROM, Shell and desktop manifests' actual 11/9/7
producer hashes have been checked against that runtime and runner.

| Payload | SHA-256 |
| --- | --- |
| ROM (`exec-rom-v106/`) | `4E03BDDF91C9E48F7D92A96A85B9B260520319DE09C1A9247268CBCD347A648C` |
| Shell (`exec-shell-v106/shell/`) | `47BAFD3B128D0C92A38FA477D89A8902D891488F71FFDF44D13A9E7722213365` |
| Desktop (`display-native-v114/`) | `989D422746FA67C4247B4053F0F7C2741EB2DD0D062BA1F2789F779F92A5FD46` |

The machine is PAL OCS 68000 with 512 KiB Chip RAM, 2 MiB Fast RAM and three DD
drives. Guest writes affect private machine memory; the source ROM/disks remain
unchanged. There are no host OS overlays, injected CPU calls, host writes to
guest RAM or forced returns. The counter regression observes a real device
through guest SetFunction and restores the vector afterwards.

The standard original-disk startup result is separate:
`CopperStart/artifacts/timer-exec-20261018/workbench-v2` passes at frame 71,324
with ROM106/Shell106 and the earlier timer102 desktop. It checks strict command
results, live services/timer and physical mouse/icon/drawer interaction. See
the sibling repository's `Docs/NativeWorkbenchStartup.md` for its exact scope.

The native desktop renders its own CopperBench interface. Vendor pixel parity,
interactive use of the Locale preferences editor, translated desktop menus and
general application compatibility are not established here. The earlier 1 MiB
keyboard-sequence and exact-palette failures reproduce on both desktop102 and
the WBConfig desktop111; the follow-up above identifies their corrections. The native
timer has VBlank resolution for units 0/1. MonitorInfo/MonitorSpec and non-PAL
display profiles remain outside the added display service. The ROM retains
seven preexisting fatal sites, so these results do not establish complete ROM
API coverage. The native desktop and Shell compiler gates have zero fatal sites,
runtime helpers and unresolved targets.

The final native framebuffer has been visually inspected: backdrop, pointer,
SYS icon and live status bar are present. The captured frame catches the status
text during redraw; its complete text is retained in the owned title buffer.
`native-switch-v114.json` records both framebuffer hashes and reports different
native/vendor pixels. The original-ROM candidate/control pixel check below
remains separate and unchanged.

## Full-machine Kickstart control

The test executes the original Workbench 3.1 startup and IPrefs on the full
PAL OCS 68000 emulator, with 512 KiB Chip RAM and 2 MiB Fast RAM. It uses the
locally supplied Workbench and Locale disks. No host Exec/DOS gateways, CPU
call injection, forced returns or guest-memory patching are used.

Private media assign LOCALE to physical DF2 and add a User-Startup. Original
Copy replaces ENV:sys/locale.prefs; the real IPrefs task handles notifications.
Disk-loaded 68000 probes wait 600 DOS ticks after each change, then copy their
public API observations into owned PA_IGNORE message ports. These switches
happen during User-Startup, before its final LoadWB/EndCLI. This does not cover
interactive use of the Locale preferences editor or translated desktop menus.

The same fixture is run with Commodore locale.library 40.4 and the CopperOS
candidate. Both finish with Workbench and IPrefs present. All four probes
succeed and match these reference values exactly:

| Phase | Global Language | January | Fault 205 | Fault 103 | utility language |
| --- | --- | --- | --- | --- | ---: |
| Initial | english | January | object not found | not enough memory available | 1 |
| German | deutsch | Januar | Objekt nicht gefunden | Speicherplatzmangel | 3 |
| Swedish | svenska | Januari | kan ej finna objektet | ej tillräckligt med minne | 12 |
| Return | english | January | object not found | not enough memory available | 1 |

The public loc_LanguageName also matches `<language>.language` in every phase.
The original deutsch.language 38.4 and svenska.language 38.5 remain loaded.
The final English desktop's decoded PNG pixels match the reference exactly;
their SHA-256, including the PNG row filter bytes, is
`DF125B83F690801A49E039345D5959D664D05EDE078A074008453032859CAA7D`.
The final control completes at frame 5336. Extending the earlier candidate run
to frame 7504 produced the same pixels.
Source ROM/media hashes remain unchanged. Guest writes affect private machine
memory only. Pixel comparison operates on decoded PNG bytes independently of
the image-preview renderer.

## Compatibility fixes found by booting

- The library Open vector now matches V40's D0 handling. Exec performs the
  version check; rejecting arbitrary incoming D0 values in the vector caused
  later OpenLibrary calls to fail on real Kickstart.
- LocalePrefsUpdate publishes global ENV:Language and utility.ub_Language.
  Its basename workspace lives on the guest stack and IoErr is preserved.
- The classic DOS patch belongs at private **-978**, with the error number in
  **D1**, for both translation and the saved getter. Public -924 is not the
  classic patch target. D0 may contain unrelated scratch data. This is checked
  against the actual original patch and ROM getter, not inferred from another
  OS's FD. Private negative IDs remain unsigned catalog keys, as in V40.
- Regression coverage compares all eight installed patch targets and distinct
  D0/D1 arguments. The separate binary matrix has 4,724 observations; the native
  suite also checks register/stack preservation and immutable image bytes on
  68000, 68020 and 68040. See [qualification.md](qualification.md).

## Compatibility work and earlier integration runs

The later native desktop replay at `artifacts/locale-workbench/full-boot-v43/`
reaches a real Workbench task and completes startup at frame 68032, without a
low-PC failure, halted CPU or unsupported active machine feature. It loads
locale 53.9 and retains the original IPrefs and ConClip background tasks. Source
media remain unchanged. None of the four language probe ports is present, so
this is still a **failed locale integration qualification**. The captured
native desktop renders its title, background, pointer and SYS: disk icon.

The independent generated-media checks now pass:

- The original DOS getter's 110 default strings, 14 translated Fault/truncation
  rows, five unknown-error forms, and seven OCS SetChipRev requests.
- Flagged CODE/DATA/BSS hunks, relocations, and real Chip/Fast memory placement,
  on both CopperStart and the original ROM. This fixes IPrefs' legal Chip data
  hunk being rejected with ERROR_BAD_HUNK.
- Original IPrefs returns zero and leaves its background process present after
  settling, with the native desktop provider and its private screen/control
  preference entry. This isolated fixture does not verify ENV notifications.
- Original LoadWB starts an independent native desktop process/display; legacy
  preference partial copies, NewPrefs messages/replies, timing, and display
  rebuild calls pass their separate native probes.

The Shell has now been rebuilt with the flagged-hunk loader and native
filesystem notification backend, without fatal machine fault sites, helpers
or unresolved targets. The original-ROM notification control passes
absent-path creation, WAIT_REPLY coalescing, signals and cleanup. Native testing
found that the boot process had no default filesystem port. Original-ROM
testing confirms that clearing pr_FileSystemTask also breaks relative operations
there. The boot process now initializes this field from the mounted filesystem,
and child processes inherit it. GetDeviceProc retains the current-directory lock
for relative paths and selects the volume root for a leading colon. Notification
registration resolves directory assigns to a canonical filesystem path instead
of passing ENV: unchanged to the RAM handler. The corrected native notification
probe and original-ROM control both pass, including the exact canonical name
`NativeBoot:Data/NotifyFile` and DevProc/default-filesystem/current-directory
relationships. These results
must not be combined into a claim that CopperStart language switching works.

The focused host cohort passes 108 tests, including relative-handler and child
filesystem-port inheritance regressions; another 30 filesystem/geometry tests pass with the factored native
division operations. The current CopperStart solution builds with zero warnings
and errors. Three native Shell OFS/FFS/initial-assign checks pass. These results
do not replace the full Workbench language checks.

The current native DOS cohort is `dos-native-v20/` with four passing tests,
including original-ROM DOS/Fault/hunk and notification comparisons. Its ROM has
485,782 code bytes and SHA-256
`3644BD809A2530C957683AC25BEF89B3807FC8C87E456309DF2C027847B1F326`.
The matching `shell-next-v55/shell/CopperShell` has 625,980 bytes and SHA-256
`D2EBFEC05778E85FD36D6471AAD43D35740D8B4E97747B2642EC7DF5C9D6BEB4`;
its compiler gate reports no fatal fault sites, helpers or unresolved targets.
`full-boot-v55/` ends at frame 69,248 with Workbench, IPrefs and ConClip alive,
without a CPU fault, but produces no language probe ports. The full comparison
therefore fails. A separate generated OFS probe retains its BEFORE marker;
bounded native tracing shows resident lookup returning zero for the registered
Execute command before its code is entered.

The cause is now identified: native resident lookup treated every allocation
header's ReturnOffset as a payload offset. Active Shell control/runner records
use that field for their owning frame pointer. The lookup now applies the same
record-type rule as the other allocation-chain validators. A disk-loaded 68000
reproducer fails before the fix with ERROR_OBJECT_WRONG_TYPE and returns the
same resident entry as the generic lookup after the fix. The coherent `v68`
cohort passes all five native tests, including this regression and both ROMs'
DOS/Fault/hunk and notification comparisons. Its 108 focused host tests pass.
The ROM is bit-identical to the coordinated native-services ROM,
`CCEADD6C97FA77927F0F446B62D279E332F3CDA3E1C4E37572FB006EC0A102D0`,
with its actual DOS producer recorded in the new manifest. The matching
FixedPoint Shell is 522,736 bytes, SHA-256
`B690AD7E7964192F902361A198DD927507E0D7FC95BFAC25E76413CDB7AD43C7`,
with zero fatal fault sites, helpers or unresolved targets. The original
Execute-in-IF fixture (`combined-execute-v72/`) now passes: the child script's
`hello disk` marker is present and EndCLI returns zero. Its frame bound includes
the original Execute command's work-file I/O.

`full-boot-v69/` completes at frame 89,584 with CopperBench, IPrefs and ConClip
alive, unmodified sources, and no CPU halt or low-PC fault. All four User-Startup
probes now execute and publish successful observations. **The language comparison
still fails:** German and Swedish remain English. The console reports three
invalid-lock errors while the original Copy command replaces locale preferences.
The native `Delay` boundary was also empty, so the probes did not wait for IPrefs.
The branch-corrected `v73` replay reproduces the untranslated values.

A separate original-ROM/native packet test identifies the lock defect: a public
Lock works with DOS Examine, but sending it directly to its handler fails on
CopperStart with ERROR_INVALID_LOCK. Native DOS had published a copy of the
handler's FileLock at a different address. The replacement now preserves the
handler's public BPTR and keeps its DOS ownership record private. The `v85`
native cohort passes seven checks, including both ROMs' direct handler packets
and public DoPkt, notifications, Fault/hunks, and resident lookup. The fixed host
cohort (`combined-host-v86/`) passes all 108 checks.

Native Delay now waits on a timer request. The disk-loaded native provider
installs interval timers before User-Startup. Its IRQ only advances the clocks;
a task handles pending requests, cancellation and message replies. This profile
services MicroHz/VBlank intervals at vertical-blank granularity. In the cached
original/native comparison (`timer-native-v90/`), Delay(25) spans 26/27 PAL frames
and GetSysTime advances 515,863/540,000 microseconds. Both runs pass aborted and
normal requests without duplicate replies. The native provider HUNK is 272,512
bytes, SHA-256
`361FDD3888E6EDE8DE46E6AC180289DC16FC52AAB86BADAA9C2E8FE1AB7583DC`.
These focused checks are not yet evidence of successful language switching.

The combined `v92` replay exposed a further assign defect: directory and late
assigns cached the process's default handler even when their target lock belonged
to RAM:. Opening through the lock worked, but cleanup and metadata packets went
to the boot filesystem. The corrected assign/device-proc paths pass 112 focused
host tests, including four failing-before cross-handler cases. The coordinated
original Avail substitution and real Execute/User-Startup checks now pass.

An expanded timer check found a stale clock snapshot across ReplyMsg dispatch:
a newer request could be processed with a timestamp preceding its enqueue time.
The worker now reads time per record and restarts after replies. `timer-native-v102/`
passes both original/native comparisons, including a two-second pending interval
spanning exactly 100 frames while wall time changes. Its corrected provider is
272,472 bytes, SHA-256
`A4CD4381EDA55544F0DCDC4B5FF445BD03CB2C6EDDB64B6DD7A7321CC14AE7C4`.

`v103` combines these fixes. All 11 ROM, nine Shell and seven desktop producer
hashes match its fresh runtime. It loads the original German language driver but
fails at frame 78,374 before publishing the German probe. This is not a locale
verification pass. Full startup must reach EndCLI; an unexpected return to the
prompt also records a failed receipt instead of waiting until the frame limit.

A separate native counter reproduces Exec submitting asynchronous DoIO twice:
the original Kickstart invokes BeginIO once, while the earlier CopperStart ROM
invokes it twice. Exec now retains the submitted continuation across blocking
WaitIO calls, including unrelated wakes, and clears it only after completion.
The focused host cohort passes 115 checks. The corrected guest counter uses
public SetFunction to observe the actual timer device vector and restores it
afterwards; the host does not patch guest memory.

The matching ROM and Shell compile successfully (`exec-rom-v106/` and
`exec-shell-v106/`). The Shell has zero fatal sites, helpers or unresolved targets.
The ROM retains its baseline seven fatal sites; this is not a claim of complete
ROM compiler compatibility. `exec-native-v108/` passes all nine original/native
integration checks; both timer devices receive exactly one BeginIO per DoIO,
and the two-second intervals both span 100 PAL frames during wall-clock changes.

`full-boot-v108/` confirms a separate failure at frame 78,371. A passive scalar
diagnostic of the earlier ROM (`boot-trace-v106/`) identifies the original IPrefs
calling private Workbench **WBConfig(-84), D0=5, D1=0**, followed by the native
provider's ILLEGAL reject stub. Its stack remains within bounds. The new provider
handles the original preference-pointer exchanges for selectors 1..4, returns
D1 for other selectors, and queues redraw for preference/locale invalidation.
The original Workbench 40.5 passes the matching generated-media probe, while the
old native provider fails it. The fixed desktop comparison (`wbconfig-native-v111/`)
passes all three checks, including the original/native WBConfig return/pointer
matrix. Its HUNK is 272,724 bytes, SHA-256
`49D218BAB2E3BC1826980C8923A1D7015742543F234AE307DDE7DBEE817CB26A`.
Its compiler gate has zero fatal sites, runtime helpers, external targets and
managed allocations. The final desktop's 12 locale/timer/notification/lock/
preference checks pass (`final-native-v111/`). Two broader 1 MiB GUI checks fail
keyboard-sequence and exact-palette assertions. Both reproduce identically with
desktop102 and the same fixed Exec/test inputs (`gui-before-v111.log` and
`gui-before-loadwb-v111.log`); they are preexisting qualification limits.
These results do not establish general GUI compatibility.
This earlier attempt does not qualify language switching; the passing v114
result is recorded above.

The next full replay (`full-boot-v111/`) passes WBConfig but stops at frame
78,982 when IPrefs starts enumerating display modes. The first saved exception
frame identifies graphics NextDisplayInfo (-732), still an ILLEGAL stub.
The native PAL OCS display provider now supplies the 44 enumerated IDs and their
22 shared default/physical handles, DISP/DIMS records, and mutable NAME chunks.
An original-ROM probe establishes byte counts and partial-copy behavior:
Add NAME returns 56; Get/Set NAME clamp to 48; an eight-byte Set returns eight;
DIM records copy complete eight-byte rectangles after their 26-byte prefix.
The `display-native-v114/` comparison passes with no differences across all
44 records and the name-mutation matrix. MonitorInfo/MonitorSpec and non-PAL
profiles are outside this display slice. Its compiler gate remains free of
fatal sites, helpers, external targets and managed allocations.

`full-boot-v114/` completes the full startup and four language observations at
frame 87,592. The separate minimal IPrefs cycle completes at frame 48,808 with
the same observations; it helped isolate services and does not substitute for
the original-startup result.

Earlier private evidence remains under `dos-native-v12/`, `hunk-native-v28/`,
`desktop-next-v33/`, `desktop-checks-v33/`, `iprefs-native-v33/` and
`notify-reference-v37/` within `artifacts/locale-workbench/`. Every replay uses
the actual producer hashes recorded for its individual ROM/HUNK cohort.

### Earlier baseline

The strict `OriginalWorkbenchStartupReachesEndCli` test uses a frozen native
CopperStart ROM/runtime and a private Workbench copy containing locale 53.9.
The recorded run reaches EndCLI at frame 49000, but **fails** its startup gate:
IPrefs returns 10, LoadWB returns 20 and no Workbench task appears. Mount,
AddDataTypes, LoadResource and ConClip also report failures. locale.library,
diskfont.library and iffparse.library load successfully. Intuition and
Workbench libraries are absent. The console reports failed ASL/commodities
loads and DOS error 122. Scalar diagnostics and host OS overlays are disabled.

At that earlier baseline, native Intuition/Workbench providers were absent and
DOS Fault did not call private -978. The current source implements the getter,
locale-aware Fault integration and the native desktop services described above.
The baseline direct-API
diagnostic attempt published no observation ports and is not translation or
IPrefs coverage. Switching languages under CopperStart was unverified in that
baseline; the passing native result is recorded at the top of this document.

The final build disables compiler peephole optimization for management/date
code after layout-sensitive FixedPoint failures in 68000 date formatting.
All 52 tests pass, including the 4,724-case differential matrix. The handwritten
catalog lookup still measures 14 instructions / 176 flat-bus cycles for dense
hits. The root cause in the compiler/emulator combination is not isolated;
optimized management builds are not qualified.

## Earlier control receipts and exact inputs

All paths below are relative to the repository root and ignored. Licensed ROMs,
disks, drivers, catalogs and RAM dumps are not included in source control.

- Final build: `artifacts/locale-workbench/native-conservative/locale.library.json`.
  53,112 bytes; SHA-256
  `196FB304DF7E49CED9B53A41CF06D26B541A73DD2CED73E72F869F1D365489DA`.
  Compiler/SDK hashes are pinned as recorded in [qualification.md](qualification.md).
- Final candidate boot: `artifacts/locale-workbench/kickstart-iprefs-conservative/`.
- Original boot: `artifacts/locale-workbench/kickstart-original-iprefs-lf/`.
- Full comparison: `artifacts/locale-workbench/switch-comparison-conservative.json`.
- Strict CopperStart attempt: `artifacts/locale-workbench/copperstart-full-conservative/`.
- Unit/native suite: `artifacts/locale-workbench/locale-tests-conservative/`.
- Workbench ZIP SHA-256:
  `D93611887ACF91F68F5608A5B7812F03EB16F743F40451B67C93067B04C967ED`.
- Locale ZIP SHA-256:
  `5AA804D69FF5C9B59E70D30CBAA1F876F6EAC555D8D6B5D40504C82273291A18`.
- Vendor Kickstart ROM SHA-256:
  `8C8A0CF04F91B88EAF0C4F1126041987067E2286A8EE590BDBAE447A8000C5EE`.
- Earlier CopperStart ROM SHA-256:
  `0FD83BAF3F46710C4F927F3C9D0F5EC9C40F9D1DBE9DEFD9299D2141FECAE518`.
  Every ROM manifest input matches the frozen replay runtime. All nine Shell
  producer hashes match the separately preserved `driver-context-bin` inputs;
  the HUNK's hash matches its producer receipt and the private support disk.

The [verification tools](../../../tools/Locale.Workbench/README.md) document
media preparation, public probes, frozen-runtime replay and the comparison gate.
This is verification evidence, not a release or hardware-timing qualification.
