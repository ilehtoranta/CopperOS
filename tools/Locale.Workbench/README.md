# Private Workbench locale verification

These tools run disk-loaded 68000 code on the full PAL OCS machine. They do not
inject calls into the CPU, patch guest memory, or substitute host OS services.
They require locally owned Workbench 3.1 and Locale disks and a Kickstart ROM.
Keep all derived media, language drivers, catalogs, RAM snapshots and screenshots
under ignored `artifacts/`. These tools do not produce a shipping system image.

`prepare_media.py` accepts `--workbench`, `--locale` (ADF or single-ADF ZIP),
`--library` (built HUNK), `--shell` (CopperStart's built CopperShell),
`--output` (a fresh directory), and `--host-tools` (a directory containing the
separately installed amitools 0.8.1 package). It copies inputs, replaces only
Libs/locale.library on a derived Workbench disk, verifies filesystem readback,
and records hashes. Original media remain unchanged.

`prepare_switch.py --media MEDIA --output FRESH --host-tools TOOLS` makes a
private startup fixture. It assigns LOCALE to DF2, adds a User-Startup with
**Amiga LF line endings**, and places probes and preferences on DF1. Countries,
drivers and catalogs come from the supplied Locale disk. The preferences select
English/US, German/Germany, Swedish/Sweden and English/US again. Each change
uses the original Copy command to replace ENV:sys/locale.prefs. The real IPrefs
task receives the notification; each following probe waits 600 DOS ticks.
`--original` retains Commodore's locale.library for the control run.

`make_probe.py` emits an original, relocatable HUNK command using public Exec,
DOS and locale APIs. Each command publishes a PA_IGNORE message port containing
owned copies of its observations: default language, January, global Language,
utility's language selector, and DOS Fault 103/205. The host reads these ports
passively. A zero probe status is a failure, even if Workbench starts.

Build the host runner against a frozen CopperStart replay runtime:

```powershell
dotnet build tools/Locale.Workbench/BootReplay/BootReplay.csproj -c Release `
  -p:FrozenRuntimeDirectory=C:/private/frozen-replay-bin `
  --artifacts-path artifacts/locale-workbench/replay

dotnet artifacts/locale-workbench/replay/bin/BootReplay/release/BootReplay.dll `
  --rom C:/private/kickstart-3.1-a500.rom `
  --df0 PRIVATE/Workbench.adf --df1 PRIVATE/Support.adf `
  --df2 MEDIA/Locale-original.adf --frames 12000 --output RECEIPT
```

The runner uses 512 KiB Chip RAM and 2 MiB Fast RAM. Drives accept guest writes
in the machine's private memory; it never saves them to source media. Passing
`--write-protected true` protects them, but can leave the startup script waiting
at a write-protection requester. The receipt checks source hashes afterwards.
It also captures the framebuffer and RAM for local diagnosis.

For the original-library control, pass `--locale-version 40`. To run CopperStart,
use its ROM and `--manifest PATH/disk-boot-test-manifest.json`; ROM and producer
runtime hashes must match. The runner's exit status checks the desktop, IPrefs,
and library version. **Language switching additionally requires this comparison:**

```powershell
python tools/Locale.Workbench/verify_switch.py `
  --candidate CANDIDATE/boot.json --original ORIGINAL/boot.json `
  --output artifacts/locale-workbench/switch-comparison.json
```

This rejects missing/failed probes, wrong languages, empty Fault results,
translation differences and incomplete boots. It also decodes the runner's
RGB PNGs and compares final English desktop pixels, independently of PNG
compression or image-preview rendering. Different pixels require visual review.
A Kickstart control pass does
not qualify a CopperStart desktop.

`build_blocker_inputs.ps1 -FrozenRuntime BIN -CopperStart REPO -Output FRESH`
copies a coherent replay runtime and snapshots the requested producer sources
before building them. `-Components` selects producers; `-FrozenTestSources`
keeps unrelated test/engine API changes outside a diagnostic cohort while
overlaying the locale integration probes. `-FrozenDosSources` selects an explicit
DOS source snapshot when the shared checkout contains unrelated work; include
the intended fixes in that snapshot before building. The output records source and assembly
hashes. Never copy a new DLL into an existing qualified cohort or relabel a ROM
with different producer hashes.
`-FrozenHostSources`, `-FrozenDesktopSources`, and `-FrozenExecSources` similarly
select explicit host test, desktop, and Exec producer snapshots when coordinating
work in a shared checkout.
`-FrozenSystemSources` selects the permanent ROM source snapshot, including its
console receiver and production export list.
`-PreserveFrozenTestSources` keeps the complete supplied native test snapshot,
including its desktop export list, instead of overlaying current probes. Use it
when a qualified desktop profile intentionally excludes concurrent provider work;
include any new regression in the snapshot before building.

`prepare_desktop.py --fixture SWITCH_FIXTURE --desktop CopperDesktop.hunk
--manifest desktop.json --host-tools TOOLS --output FRESH` adds a real disk-loaded
WBInit command to a private startup copy. The retained desktop HUNK lives on
DF1, beside the Shell support payload. WBInit uses public DOS LoadSeg and a
guest stack switch; it does not inject a host service. This is a verification
fixture, not a shipping installer. Recompile disk-loaded Shells as well as the
ROM when changing code that the native compiler includes in both images.
Pass both `--shell CopperShell --shell-manifest shell-manifest.json` to replace
that payload with the matching rebuilt Shell. The preparer checks both HUNK
hashes and filesystem readback before recording the media receipt.

`verify_native_switch.py` compares the same four public locale observations
for CopperStart's own Workbench desktop against the original boot. It requires
Workbench, IPrefs, unmodified inputs, and successful execution without overlays.
The native desktop's actual `CopperBench` task name is recorded in the receipt;
the vendor control still requires its `Workbench` task.
It records both framebuffer hashes for separate visual review. It does not
establish vendor desktop pixel parity; `verify_switch.py` retains that gate.
The comparison also requires IPrefs to remain in the final public task list;
having observed it earlier in startup is insufficient.

`BootReplay --ram-profile one-mib` selects exactly 512 KiB Chip RAM, 512 KiB
A501 slow RAM and zero Fast RAM. The default `fast-2m` retains the earlier
512 KiB Chip / 2 MiB Fast profile. Both record actual memory sizes; the runner
passively inspects and saves slow RAM as `slow.bin` alongside Chip/Fast dumps.
Use `verify_native_switch.py --ram-profile one-mib` with a reference boot on the
same profile: it rejects missing or mismatched memory sizes in either receipt.
The private pinned engine's slow RAM backing array is resolved once through
reflection because it has no public passive view. This never performs bus reads
or writes and does not change guest execution.

`verify_display_info.py --candidate display-native.json --original
display-reference.json --output comparison.json` checks the PAL OCS display
service used by IPrefs to translate mode names. It compares all 44 IDs,
default/physical handle aliases, DISP/DIMS/NAME records, transfer lengths and
private name additions/replacements. MonitorInfo/MonitorSpec remains outside
this native slice and is explicitly excluded from that qualification.

The runner waits for 512 emulated frames after startup ends so background
servers can settle. A low PC after boot fails the run before subsequent
unhandled instructions can corrupt the public lists being inspected.
Returning to an idle prompt before EndCLI also stops the full-startup fixture
with a failed receipt, framebuffer and RAM captures.
For the native desktop, a task appearing during startup is insufficient:
`nativeDesktopReady` requires initialization phase 1 and a published desktop
window. Pending initialization keeps the replay running; a desktop that exits
or fails to initialize cannot pass the locale comparison.

`--direct --locale-on-support` on prepare_switch makes a separate diagnostic:
it places the selected language drivers/catalogs on DF1 and applies preferences
through OpenLocale/LocalePrefsUpdate in each guest probe. It does **not** cover
IPrefs notifications or establish a successful Workbench boot. This distinction
must remain explicit in reports.

Use exact compiler/SDK input hashes from the library's build receipt when
reproducing native results. Sibling compiler outputs may change during other
work; stage a private copy before running qualification. The library builder
checks input immutability and records every producer assembly hash.


The native Shell build uses the separate CopperStart DOS client compiler input
(see CopperStart/native/CopperStart.DosClient/README.md). Ordinary DOS calls use
public M68k vectors; CSNS version 2 shares the ROM filesystem and process services.
Keep the full provider DLL in the ROM/replay runtime. The Shell manifest records
the client DLL under client/CopperStart.Dos.dll and declares serviceVersion 2.
Each image's compiler inputs remain independent of the ROM producer cohort.

ShellClientBuilder builds a pinned MC68000 Shell from --runtime BIN and --client
CLIENT_DLL into --output FRESH. Supply FrozenRuntimeDirectory when building the
tool itself. It checks compiler-input hashes before and after compilation, rejects
managed allocations/runtime helpers/fatal sites/unresolved native targets, and
checks the native process packet stack home against its 136-byte wire contract.
CopperStart's Build-Shell.ps1 supports -OutputRoot FRESH and isolates compiler,
managed provider, managed client, and native outputs to avoid shared DLL locks.

inspect_memory.py --boot REPLAY_DIRECTORY --output REPORT.json reads the saved
Chip/slow/Fast snapshots, walks public Exec MemHeader/MemChunk lists, checks their
bounds and byte totals, and records largest free blocks and the boot CLI Result2.
It never changes guest memory or uses the allocator's private TLSF structures.

DesktopBuilder compiles the complete current desktop export set from a pinned
runtime. Supply `--exports` with a JSON array of export names and `--size true`
to opt into the compiler's Freestanding HUNK size policy. ShellClientBuilder
accepts the same size option. Both tools retain the entire module until reset,
check producer hashes before and after compilation, and reject unsupported
runtime dependencies. Compiler checks alone do not qualify a Workbench boot.

`prepare_desktop.py --autoload` installs the desktop at `L/CopperDesktop` for
the production Shell loader. It preserves Startup-Sequence byte for byte and
records its source/prepared hashes. The default diagnostic WBInit mode retains
its separate stack and must be identified separately in memory comparisons.
Use the production floppy **Classic** ROM profile explicitly
(`CopperStartNativeSfsWrites=false`). The project default Full profile requires
the separate SFS extension ROM, including its DOS client service entry.

`inspect_stacks.py --boot REPLAY_DIRECTORY --output REPORT.json` records public
task stack bounds, saved stack pointers, and the first nonzero byte in the
passive RAM captures. These observations are not worst-case bounds: zero writes
and uncleared allocations affect the apparent span. Private service budgets
are defined in CopperStart's NativeStackBudget; application NP_StackSize
requests are preserved. Qualify any budget change with the complete startup
and language-switch workload, rather than treating a module load as completion.

`verify_cohort.py` checks the ROM, Shell and desktop image hashes and each
image's own producer list. Pass `--rom-manifest`/`--rom-runtime`, the equivalent
Shell and desktop arguments, and `--output REPORT.json`. It resolves the Shell
DOS client sidecar separately from the ROM's full DOS provider. A passing
producer audit establishes reproducibility; native execution must still pass.

BootReplay --memory-checkpoints true optionally saves passive RAM snapshots at
the first LoadWB observation and five frame boundaries around the first
Workbench task. These use ordinary full-frame execution, introduce no guest
writes or injected calls, and can be read by inspect_memory.py/inspect_stacks.py.
The final full-startup receipt remains independent of these checkpoints.

Receipts also include the saved user and supervisor stack pointers as
`registers.usp`/`registers.ssp`; `registers.a[7]` is the active stack pointer.
These distinguish a caller-stack failure from an unsupported native library
vector without changing guest execution.

For complete private floppy fixtures, pass `--storage-keymaps STORAGE.zip` (or
an ADF) to restore absent startup drawers and install the supplied d/s/usa2
keymaps. Pass `--locale-disk Locale.adf` to make a private Locale copy labelled
LocaleSupport. This avoids the legitimate volume/assign name conflict when the
fixture executes Assign LOCALE: DF2:. The preparer verifies that relabelling
changes only the root block, records source/prepared hashes, and preserves
the existing fixture Startup-Sequence in autoload mode. Mount the resulting
output/Locale.adf in DF2. Source disks are never edited.

Use `verify_native_switch.py --require-clean-commands` for the completed startup:
it rejects any raw commandFailures entry. BootReplay also writes the passive
script-progress.json sidecar to identify the script line behind a CLI return
code; the raw failure collection remains unchanged.

`verify_desktop_pixels.py --boot REPLAY_DIRECTORY --output REPORT.json` compares
the settled framebuffer with its saved bitplanes and actual Copper palette,
requires a visible owned pointer, and checks all seven unused sprite channels
against the zero terminator. It follows the sprite Copper prefix through COP2
and accounts for each PNG scanline's filter byte. This is a native raster
consistency check, independent of vendor desktop pixel identity.

To qualify a fresh locale build, pass `--locale-library locale.library` and
`--locale-manifest locale.library.json` together. The private Workbench copy
receives that library after its producer hash, byte count and name are checked.
A filesystem readback must match the same hash; the library source and startup
script remain unchanged. The media receipt includes the complete locale producer
manifest alongside the ROM, Shell and desktop inputs.
