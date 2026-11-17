# CopperOS system build and boot images

Recorded artifacts, staging guarantees and current runtime evidence are in
[Normal system build](../../docs/NormalSystemBuild.md).

From the CopperOS repository root:

```powershell
& tools/build_system.ps1 -Python '<python executable>'
```

The normal build compiles the 68000 math library, shared Shell, bootstrap and
Execute/List commands, runs strict native and public-interface audits, stages
the verified files in `filesystem/SYS`, and writes
`artifacts/system/68000/System.adf`. `system-build.json` identifies the retained
native manifest beneath that output directory. `system-image.json` and
`system-staging.json` bind the boot image and each staged file by hash.
Alternate output, SYS and compiler roots are explicit build parameters.
The build first double-reads and snapshots its required CopperOS, CopperStart,
CopperFS and CopperSharp sources. All subsequent compilation uses that retained
tree, so concurrent checkout edits cannot mix source versions within a build.
The source snapshot receipt is named in `system-build.json`; native source
audits refer to that captured tree. Boot tests remain separate from these gates.

The shared native builder is `build_system_native.ps1`. The proof entry below
is a compatibility wrapper around it; both workflows use the same Shell sources,
export roots, stack gate and bootstrap. Normal manifests use
`copperos-system-native`; the resulting image receipt uses
`copperos-system-image`. Both remain development outputs with `shipping: false`.
The image builder does not import loose files from SYS or command-release records.

Boot the exact normal image with `qualify_unmodified_shell31.py --image
<System.adf> --rom <ROM> --probe-dll <ExactBootProbe.dll> --mathtrans
--output-directory <fresh-directory>`. Add `--extension-rom <extension.rom>`
for CopperStart. This mounts the unchanged input as DF0 and a read-only math
probe as DF1. The helper supplies no library or replacement startup. Follow
with the command/process fixtures below when verifying a changed native Shell.

## Original development proof workflow

The proof is a separate, nonshipping development image. It does not change
the command distribution admission rules in `tools/DiskBuilder`, import loose
`out/C` files, or include reference operating-system binaries.

From the CopperScreen workspace, with the CopperOS, CopperStart and
CopperSharp68k producer checkouts alongside it:

```powershell
& ../CopperOS/tools/build_system.ps1
& ../CopperOS/tools/ImageBuilder/build_system_proof.ps1 -OutputDirectory artifacts/copperos-system-proof -Python '<python executable>'
python ../CopperOS/tools/ImageBuilder/build_system_image.py --manifest artifacts/copperos-system-proof/native-manifest.json --output artifacts/copperos-system-proof/System.adf --receipt artifacts/copperos-system-proof/system-image.json
```

The proof build imports `filesystem/SYS/Libs/mathtrans.library` only when its
hash and length match the 68000 native build receipt. It snapshots that file
and receipt into the proof directory and rejects changes during the build.
Use a fresh output directory. Native compilation freezes managed inputs,
records producer/toolchain source hashes and rejects source changes during the
managed build. Explicit export allowlists prevent the bootstrap and demonstration
from unintentionally rooting the whole Shell. The Shell and bootstrap use the
freestanding profile; Execute and List use the resident command profile. All are built
for the 68000 without an FPU and checked for unsupported runtime services.

When the compiler checkout is under independent development, pin its source
before building the proof:

```powershell
python tools/ImageBuilder/snapshot_shell_toolchain.py --source ../CopperSharp68k --output artifacts/shell-toolchain-source
& tools/ImageBuilder/build_system_proof.ps1 -CopperSharpRoot artifacts/shell-toolchain-source -OutputDirectory artifacts/system-proof -Python '<python executable>' -MathTransReceipt '<native mathtrans receipt>'
```

The snapshot includes the compiler, SDK publish targets and runtime sources.
It verifies two reads before writing its immutable copy and records per-file
hashes. The build manifest hashes targets and JSON metadata as well as C#;
`audit_shared_shell.py` checks those pinned inputs and the live Shell producer
sources. Keep the snapshot with the proof's inputs and receipts.

The deterministic 880 KiB DOS1 FFS image contains original `C/BootShell`,
`C/Demo`, `C/Execute`, `C/List`, `L/Shell-Seg`, `Libs/mathtrans.library`,
`S/Startup-Sequence`, `S/CopperOS-Release.json`, and a `T` work directory.
The proof builder compiles and hash-binds Execute and List with the local toolchain;
`-Python` supplies the interpreter used to append its version identifier.
The proof does not yet create the standard `ENV:` or `ENVARC:` assigns;
global-variable commands require an environment configured by the boot system
or by a test fixture. `List` remains a bounded implementation rather than a
complete replacement for the Workbench command.
Its standard boot block enters native DOS. The bootstrap streams the single
CODE/self-RELOC32 Shell HUNK, registers it with public DOS `AddSegment` as a
permanent SYSTEM segment, and starts a real DOS Process/CLI with a 64 KiB stack.
The parent waits and retains borrowed console streams; it does not run another
console reader. SYSTEM registration is not PURE command admission.
The Shell HUNK entry wrapper initializes the CLI-owned `C:` lock list before
entering `DosShellNativeEntrypoints.ShellTaskEntry`.

The packaged Shell also has a small 68000 entry gate for processes started
with less than 32 KiB of stack headroom, as can happen with `System()` and
`NewShell`. It obtains a temporary 128 KiB public-memory stack and switches
through Exec V37 `StackSwap`, restores the caller's stack after the Shell
returns, and releases the allocation. An allocation failure returns 20. The
manifest records hashes of both the compiler's unwrapped HUNK and the stack
packager, and the image builder checks those hashes and regenerates the gate
before admitting the Shell. The compiler compatibility report and map describe that unwrapped
code, while the packaged HUNK includes the separately checked gate.

On Kickstart 3.1, the directly created proof Shell enters the shared Shell
runner using the public CLI and process structures, with the parent's standard
console input. The Shell owns its runner state and updates the CLI-owned path
lock list. Packet startup, foreground/background children, NewShell/NewCLI,
continuations and stream leases use public Exec/DOS services. The legacy
context-register arguments are ignored. A linked-method audit rejects private
CopperStart DOS services in the packaged Shell. Complete runtime acceptance
is tracked in `docs/SharedShellAcceptance.md`; passing the build audit alone
does not qualify the complete Shell.

The following diagnostic copies a proof image into a disposable HDF and adds a
guest keyboard injector ahead of the same `BootShell`. It checks interactive
commands and `EndCLI` on Kickstart 3.1. The modified startup is not a test of
the exact input image's startup or a qualification of the complete Shell.

```powershell
python tools/ImageBuilder/qualify_shared_shell31.py --image <System.adf> --rom <Kickstart-3.1.rom> --winuae <winuae64.exe> --output-directory <fresh-directory>
```

Pass `--system` in a second run with a fresh output directory to exercise
packet-started synchronous and asynchronous `System()` calls, including a
missing command's return code and secondary error. Pass `--lifetime` in another
fresh run to compare public free memory after repeated asynchronous children.
Pass `--failat` to check that the CLI failure threshold survives a nested
`Execute` script, including the value printed before and after the script.
Pass `--path` to exercise path add, remove and reset, including the implicit
`C:` search after the explicit CLI path is cleared.
Pass `--path-isolation` to check that a background Shell inherits the parent's
CLI path, can replace its own lock list, and does not alter the parent's list.
The child signals the parent through public Exec after closing its completion
marker; the parent blocks on that signal before taking its final listing.
For a diagnostic Chip RAM capture, `inspect_cli_path_snapshot.py` records each
Process's public CLI pointer, path-node addresses, and lock BPTRs together with
the capture hash; mapped lock records also include their key, handler, and
volume fields. Supply task addresses from the corresponding guest trace;
the inspector does not prove that locks refer to the same directory.
The CopperStart replay offers the same check with `--case path-isolation`.
Pass `--substitution` to check the original Shell's single backtick result,
verify that `T:` is unassigned, and check that the capture file is removed.
`--substitution-assigned` runs the same check with `T:` assigned to `SYS:T`.
Pass `--resident` to check ordinary and system listings, missing single-name
files, built-in disable/restore, and the resulting CLI diagnostics.
`--resident-extensions` checks that the V40 shared Shell returns a DOS error for
the newer deferred and resident-alias extensions without calling absent vectors.
On a V50-capable CopperStart DOS, `qualify_copperstart_run.py --case resident-defer`
checks that a deferred entry is initially unloaded, loads on first execution,
and is removed afterwards, including its forced-resident warning.

To compare `Resident ... SYSTEM` with the original ROM Shell, use two fresh
output directories. The first fixture omits `L:Shell-Seg`; the second uses the
proof Shell and compares return codes, the public segment count, and listings.
The comparison excludes the proof's extra `Cls` internal command and its
registered `Shell` module from the full-listing comparison.

```powershell
python tools/ImageBuilder/observe_resident_system31.py --image <System.adf> --rom <Kickstart-3.1.rom> --winuae <winuae64.exe> --output-directory <reference-directory> --original
python tools/ImageBuilder/observe_resident_system31.py --image <System.adf> --rom <Kickstart-3.1.rom> --winuae <winuae64.exe> --output-directory <candidate-directory> --reference <reference-directory>/observations.json --require-order
```

For a separate boot check of the unmodified ADF, build and run the independent
CopperMod lightweight emulator probe. It mounts the proof directly as DF0,
types one command through emulated keyboard hardware, and checks its output in
a separately exported ADF. It uses a 68000 with 2 MiB Chip RAM and does not
qualify the complete Shell or prove behavior on physical hardware.

```powershell
dotnet build tools/ImageBuilder/ExactBootProbe/ExactBootProbe.csproj -c Release --artifacts-path <fresh-build-directory>
python tools/ImageBuilder/qualify_unmodified_shell31.py --image <System.adf> --rom <Kickstart-3.1.rom> --probe-dll <fresh-build-directory>/bin/ExactBootProbe/release/ExactBootProbe.dll --output-directory <fresh-results-directory>
```

Add `--mathtrans` to mount a generated, read-only probe disk in DF1 after
selecting the same proof disk for DF0. The guest runs the probe from DF1; it
opens `SYS:Libs/mathtrans.library` at version 40 and writes its result on the
proof disk. The helper contains no library and does not replace `SYS:`.

The separate keyboard-injection test exercises the Shell on a disposable HDF
made from the proof. It requires locally supplied Kickstart 3.1 and WinUAE:

```powershell
python tools/ImageBuilder/qualify_shared_shell31.py --image <System.adf> --rom <Kickstart-3.1.rom> --winuae <winuae64.exe> --output-directory <fresh-results-directory> --run
python tools/ImageBuilder/qualify_shared_shell31.py --image <System.adf> --rom <Kickstart-3.1.rom> --winuae <winuae64.exe> --output-directory <another-fresh-results-directory> --run-lifetime
```

`--run` checks background commands, quoting, nested scripts, inherited paths,
aliases and local variables, redirections and the CLI notice. `--run-lifetime`
checks the launcher notice and compares free public memory after repeated workers
exit. `--run-owned-lifetime` repeats the measurement with an output opened
by the launcher. These tests alter the
startup on a copied image for keyboard injection; they do not qualify the
unmodified boot or the complete Shell.
The same harness offers `--cd` for current-directory changes, parent lookup,
and the result and retained directory after a failed change, and `--path` for
explicit CLI path operations.
`--new-shell` and `--new-cli` start a separate Shell with a supplied startup
script and check inherited aliases, variables, path, current directory, stack,
and clean exit. `--new-shell-default` and `--new-shell-window` exercise the
default startup file and an explicit console specification respectively;
`--new-shell-prompt` checks the inherited prompt and `--new-shell-missing`
checks the interactive child when the requested startup file does not exist.
`--new-shell-lifetime` starts and retires repeated child Shells, comparing free
public memory after warmup and two further batches.
`--run-leading` checks the distinct V40 behavior of redirection immediately
after `Run`; the current proof passes that guest replay on Kickstart 3.1.
`observe_run_stream31.py` and `compare_run_stream31.py` separately
compare delayed child output and caller handle ownership with the original
Shell. Focused comparisons cover a leading file, `NIL:`, and a `System()`
caller-supplied output; they do not qualify all devices or redirection forms.
`--run-leading-split`
separately checks an empty child and an explicitly redirected child without
depending on that stream-lifetime difference. It also checks transferred
input and error streams with native guest commands. The shared-error case
checks the current Shell's handle lifetime; its original-ROM behavior remains
unresolved because the delayed reference child did not reach the probe marker.
The observer's `--shared-command` controls can separately run the same probe
directly or without the shared-error redirection to distinguish a command-form
failure from a broken probe. These controls do not establish why the original
shared-error form fails to reach its entry marker.
`--split-case` can isolate the empty, child, error, input, or shared-stream
portion of that probe while retaining the same follow-up and exit checks;
`none` runs the script and exit checks without a background command. The
default `all` runs the combined case. `--suppress-exit-state` omits the
injector's extra exit-state writes for a diagnostic control.
`--isolate-split-input` explicitly opens `NIL:` for split-case workers that
otherwise inherit input, allowing comparison of script-input sharing with
the default sequence.
`--run-input-race` types a follow-up while an external background command is
inside `RunCommand()` with the inherited input handle. `--run-input-control`
runs the same check with the child's input independently opened from `NIL:`.
The injector waits for the child's start marker, and the child records whether
the follow-up completed before it returned and whether the input handles differ,
so both cases require actual overlap and independent handles.
`--input-poll-followup` holds the child in a bounded guest-side wait for that
follow-up instead of relying on a fixed delay.
`--run-console-lifetime` compares public free memory after repeated workers
launched with console input.

In a separate V40 run with the parent output captured and held open until
after a delay, the captured parent output contained only the later `AFTER`
line. Another V40 run produced an empty child output for a leading input file
and only the notice for a leading shared output/error stream. These are
observed orderings, not guarantees for concurrent output.
In that reference setup, a direct `ErrProbe >directout *>directerr` control
created `directout` but no `directerr`. The
[AmigaOS DOS reference](https://developer.amigaos3.net/sites/default/files/downloads/2024-10/Amiga_ROM_Kernel_Reference_Manual_DOS.pdf)
dates `*>`, `*>>`, and `*<>` to version 45; the original V40 Shell does not
provide those documented operators. The exact V40 parsing and diagnostics for
these command lines have not been established.
`--run-nested-delayed` exercises a nested worker that writes to its standard
output after a delay. `--run-nested-control` checks the test command
while its launcher remains alive; both leave the interactive Shell
running to avoid concurrent keyboard input during the delayed command.
`--run-multi-owned` checks two delayed descendants and their redirected output, and
`--run-nested-lifetime` compares free memory after repeated nested launches.
For exit diagnostics, `--independent-exit-observer` starts a second guest
process before the Shell. It records its start, an intermediate timer wakeup,
and whether the Shell task remains after a later delay. Pair it with
`--observer-control` to leave the baseline Shell running and check the observer
itself. The ordinary injector markers and all command results are still checked;
a missing observer marker is a failed test, not evidence of Shell exit. The
probe also rejects `second-boot.txt`, which the injector writes when the
startup runs again after an earlier injection. A repeated startup invalidates
the run even if other markers from different starts are present.
Use `--duration-seconds` to extend the observation window when investigating
late or missing markers; the default remains 60 seconds. `--line-delay-ticks`
adds a delay after each injected Return to compare immediate input with a
slower typist; by default the injector pauses only one tick between keys.
`--diagnostic-log` runs a portable copy of WinUAE inside the output directory,
enables its log and illegal-memory diagnostics, and captures Exec's preserved
alert fields if the guest starts again. The log and copied executable remain
in that disposable output directory.

The corresponding CopperStart overlap test adds only a test command to a copy
of the proof ADF. The guest publishes an in-command memory marker; the emulator
then types the parent follow-up and exports the disk for independent checking.
The runner checks distinct handles, overlap, restoration of the parent console
task, and Shell cleanup. Run the control in a second fresh directory with
`--control`; it uses independently opened `NIL:` input.
For the console case the probe also checks that the worker's distinct input
handle is attached to the same public handler as the Shell's input, and that
the run did not create a stray filesystem file named `*`.

```powershell
dotnet build tools/ImageBuilder/CopperStartInputProbe/CopperStartInputProbe.csproj -c Release --artifacts-path <fresh-build-directory>
python tools/ImageBuilder/qualify_copperstart_input.py --image <System.adf> --rom <CopperStart-DiskBoot.rom> --extension-rom <CopperStart-DesktopExtension.rom> --probe-dll <fresh-build-directory>/bin/CopperStartInputProbe/release/CopperStartInputProbe.dll --output-directory <fresh-results-directory>
```

The same probe can run a disposable asynchronous `Run` regression. The
`broad` case launches successive workers with inherited state and redirected
streams; `--case owned` starts with the leading-output case and checks
input, output, and error handles. `--case nested-leading` tests a
background script immediately before the leading-output command and captures
that command's return code and DOS error; `nested-leading-control` gives the
second worker its own `NIL:` input. Each case requires a fresh result
directory and checks the guest files independently. It checks that the Shell
task exits, but does not treat bytes at an old freed address as leak evidence.
`--case broad-prefix --prelude-count <0..5>` varies how many of the broad
case's workers precede the leading-output command for failure isolation.
The `new-shell` and `new-cli` cases exercise the corresponding child startup
and inherited CLI state under CopperStart.
`--case resident-system` exercises system resident admission, removal,
replacement, return codes, interleaved listing order and Shell exit;
`resident-system-short` checks the additions and ordering with fewer guest
commands. Both use a disposable image because system entries are permanent.

```powershell
python tools/ImageBuilder/qualify_copperstart_run.py --image <System.adf> --rom <CopperStart-DiskBoot.rom> --extension-rom <CopperStart-DesktopExtension.rom> --probe-dll <fresh-build-directory>/bin/CopperStartInputProbe/release/CopperStartInputProbe.dll --output-directory <fresh-results-directory> --case broad
```

Set `COPPEROS_PROBE_SCREEN=1` when running the script to print periodic guest
screen snapshots and save intermediate ADF copies for diagnosis.

For a Kickstart proof, boot the ADF as DF0 and mount a separate helper volume
containing the probe made by `tools/MathTrans.Workbench/make_probe.py` with
`--version 40 --library-name SYS:Libs/mathtrans.library` and a report path on
the helper volume. At the Shell prompt, run the probe from that volume.
The report must say `RESULT 0`; independently check the ADF and staged library
with `verify_mathtrans_image.py`. The helper volume must not replace SYS: as
the boot volume or supply the library under test.

The bounded original loader opens `L:Shell-Seg` through native DOS and streams
it through a 4 KiB buffer, avoiding a whole-file allocation alongside the loaded
code. It supports the generated single-CODE/self-relocation format. DOS resolves
the boot volume. For the original DD DF0 layout, the loader retains its existing
native trackdisk reader with track/table caches; DH0 uses DOS reads. The choice
compares the opened file's public handler port with the DF0 DOS device entry,
so an inserted floppy cannot redirect an HDF bootstrap. All buffers and code
are guest owned. There are no host filesystem services.

The current compiler's size optimization can relocate a small program's entry
away from the first CODE byte. The proof scripts disable it for BootShell, Demo
and Storage, retain it for the large Shell, and require `ENTRY 00000000` in every
native output map. HUNK commands have no separate executable entry-offset field.

## A500 HDF boot

Use `--format hdf` to produce an 8 MiB DOS1 FFS partition image. It has no RDB;
mount it with the explicit `DH0:2:32:0:255` geometry recorded in its receipt.
The supported machine remains CopperStart standalone A500, PAL OCS 68000,
512 KiB Chip plus 512 KiB slow RAM. No floppy is needed. This does not add
Kickstart hard-disk boot support or qualify A1200.

```powershell
& ../CopperOS/tools/ImageBuilder/build_storage_proof.ps1 -BaseProofDirectory artifacts/copperos-system-proof -CompilerInputs artifacts/copperos-system-proof/inputs -OutputDirectory artifacts/copperos-hdf-proof
python ../CopperOS/tools/ImageBuilder/build_system_image.py --manifest artifacts/copperos-hdf-proof/native-manifest.json --format hdf --output artifacts/copperos-hdf-proof/CopperOS.hdf --receipt artifacts/copperos-hdf-proof/hdf-image.json
dotnet CopperScreen/bin/Release/net10.0/CopperScreen.dll --copperstart --rom '<qualified CopperStart-DiskBoot.rom>' --hdf-ffs-writable artifacts/copperos-hdf-proof/CopperOS.hdf DH0:2:32:0:255
```

Use `--hdf-ffs` for a read-only mount. Use a copy of the generated HDF for daily
writes; its build receipt identifies the pristine image. The optional original
`Storage` command creates settings/data, exercises rename/delete and directory
operations, then verifies their contents. After a complete shutdown and fresh
boot, running it again verifies the persisted files. It reports DOS write
protection when run against a pristine read-only image.

Set `COPPERSCREEN_COPPEROS_HDF_PROOF` to the directory containing `CopperOS.hdf`
and `hdf-image.json` to enable the HDF cases in `CopperOsBootProofTests`. Those
cases copy the image for writes, use normal app sessions with no inserted
floppy, type commands through the keyboard, read native console pixels, and
dispose the entire session before cold-reopen verification. The start menu is
unchanged.

The image builder verifies boot checksum and independently reads the entire
filesystem tree/bitmap using the existing command-image verifier. That verifier
requires nonbootable disks, so it receives a private copy with boot code/checksum
zeroed after the original boot checksum has separately passed. The original
image is unchanged. Serialization/readback is not native execution qualification.

```powershell
python -m unittest discover -s ../CopperOS/tools/ImageBuilder/tests -v
$env:COPPERSCREEN_COPPERSTART_ROM = '<exact qualified standalone ROM>'
$env:COPPERSCREEN_COPPEROS_PROOF = '<absolute proof output directory>'
dotnet test CopperScreen.Lightweight.Tests/CopperScreen.Lightweight.Tests.csproj -c Release --filter FullyQualifiedName~CopperOsBootProofTests
```

The optional CopperScreen test uses its standard Lightweight app session, real
keyboard events and native console pixels. Missing assets mean unavailable
coverage. A successful run must observe Shell-only Alias dispatch, the original
external demonstration returning, and recovery after an unknown command.
Generated images, ROMs, native outputs, reports and captures belong under ignored
local artifacts, never source control. `shipping` and `runtimeQualified` remain
false in build receipts; a separate dated runtime result records any observed
proof. The HDF cases cover bounded native persistence; this work does not admit
a Store bundle or establish general filesystem, rights, installed-update or
general Shell lifecycle qualification.

## Kickstart 3.1 library load

The ADF can be checked with a locally owned Kickstart 3.1 ROM and WinUAE:

```powershell
& ../CopperOS/tools/ImageBuilder/verify_mathtrans_winuae.ps1 `
  -Image artifacts/copperos-system-proof/System.adf `
  -ImageReceipt artifacts/copperos-system-proof/system-image.json `
  -StagedLibrary ../CopperOS/filesystem/SYS/Libs/mathtrans.library `
  -KickstartRom '<Kickstart-ROM>' -Python '<python.exe>' `
  -OutputDirectory artifacts/mathtrans-image-winuae
```

The verifier independently reads the ADF, compares the library with the staged
file and image receipt, then opens `DF1:Libs/mathtrans.library` from the image
on Kickstart 3.1 and calls representative vectors. WinUAE boots a private host
directory as DH0; the proof ADF is mounted read-only as DF1. This verifies the
library load from the image, not booting the CopperOS image on Kickstart.
