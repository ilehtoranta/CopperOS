# Workbench 3.1 passive slow-RAM readiness capture — 2026-09-26

The missing memory visibility is resolved. A bounded clean native run now exposes
Exec 40.10, DOS 40.3 and Process/CLI structures, including native startup command
names and a Workbench process. This is stronger startup evidence than the earlier
chip-only capture. It is **not full-boot certification or command parity evidence**.

## Changes and qualification

The owning engine change is three lines in
`CopperScreen/CopperMod.Amiga.Lightweight/LightweightA500Machine.cs`: a documented
public `ReadOnlyMemory<byte> SlowRam` property exposing `$C00000-$C7FFFF` for owner
thread observation. No clock, device, CPU, disk, DMA, display or memory behavior
changed. There were no other edits to the engine's 21 source files in this slice.

`CopperScreen/CopperMod.Amiga.Lightweight.Tests/LightweightPassiveMemoryTests.cs`
adds `SlowRamViewObservesCpuStoresWithoutChangingExecution`. An authored ROM writes
both ends of slow RAM, then stops; five frames of observations match an unobserved
control for CPU/register, memory, framebuffer and audio state. The isolated
diagnostic test run passed **1/1, zero skips**. This test is observer evidence,
not a hardware correctness oracle.

The new [private runner and reproduction notes](../../../../tools/Commands/Workbench31PassiveRunner/README.md)
build isolated source snapshots against the original runner's frozen Copper68k,
CopperDisk and CopperFloat DLLs. No shared binaries were rebuilt or replaced.
Public read-only state/memory/output properties are the only capture inputs;
there are no host guest-bus reads, private-memory access, runtime reflection,
host DOS services, guest writes, input injection or media modifications.

## Retained evidence

The local artifact directory is
`artifacts/workbench31-clean-runner-slowram-20260926-8c39d4a2/`.

| Evidence | Result |
| --- | --- |
| [Capture receipt](../../../../artifacts/workbench31-clean-runner-slowram-20260926-8c39d4a2/capture-receipt.json) | Exact command and before/after input identities; process exit 0; inputs unchanged |
| [Qualification](../../../../artifacts/workbench31-clean-runner-slowram-20260926-8c39d4a2/qualification.json) | Source comparisons, PDB document bindings, regression and evidence hashes |
| [Passive analysis](../../../../artifacts/workbench31-clean-runner-slowram-20260926-8c39d4a2/readiness-analysis.json) | 35 observations; 31 with DOS roots; 153 Process and 35 CLI observations across snapshots |
| [Final framebuffer](../../../../artifacts/workbench31-clean-runner-slowram-20260926-8c39d4a2/snapshots/frame-002000.bmp) | Still garbled/striped; no legible Shell prompt |
| [Regression TRX](../../../../artifacts/workbench31-clean-runner-slowram-20260926-8c39d4a2/test-results/passive-memory.trx) | 1 passed, 0 failed, 0 skipped |

The profile remains PAL OCS A500/68000, 512 KiB chip + 512 KiB slow RAM, the licensed
512 KiB Kickstart 3.1 A500 ROM and original Workbench 3.1 rev 40.42 disk 2 ADF.
Exactly 2,000 frames execute, with no warmup or input. ROM SHA-256 remains
`8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee`;
ADF remains `a8f167bad2897e8c7f2cfe73de7a9f8dad10c7a5b1f78ca17f818ab17278b985`.

Compared with the [prior capture](workbench31-clean-runner-readiness-20260926.md),
CPU fingerprint `AC179436D9BA9EAE`, output fingerprint `5F77038F9388B112` and cycle
`284204014` match exactly. **All 35 chip-RAM and 35 BMP files are byte-identical.**
The old internal-state hardware fingerprint is not available through the selected
public API and was not compared. `UnsupportedActiveFeature` remained null at the
recorded observations; that is not an unsupported-feature completeness guarantee.

The original frozen engine DLL remains SHA-256
`e64ad1fd16effa63ed33ce1cd3811a1580774c67cccf5fe7584d71f520939425`.
The isolated engine used here is
`708dda6f94e2640389dcdcf1ddec46f9ad9fad0996f53bffba09f59b50e1325e`;
private runner `75206d04f5e477ca079fa933c6ea0570ee5d30929c6594c3724cafa1cbc4e5dc`.
Portable PDB document SHA-256 records match all 21 engine source files in the
before-change and capture snapshots to their respective binaries' PDBs. The three
generated build documents were not copied and are explicitly absent, not matched.
The source comparison has one changed file: the new `SlowRam` property.

## What the saved bytes establish

The analyzer decodes only saved chip/slow RAM and the unchanged reference ROM,
using `CopperSharp68k/Sdk.Amiga/Exec/ExecLayout.cs`, `Exec/Enums.cs` and
`DOS/DosLayout.cs`. Early snapshots can contain incomplete bootstrap structures;
their parse issues remain recorded. From frame 120 onward, sampled public lists
decode without issues.

| Observation | Evidence |
| --- | --- |
| Frame 120 | ExecBase `$00C00B00`, Exec 40.10, valid task/library lists |
| Frame 240 onward | DOS 40.3 at `$00C0DA2C`, RootNode `$00C0DA74` |
| Frame 540 | Running `Initial CLI`, CLI command name `C:SetPatch` |
| Frames 840 / 1020 / 1260 | Running `Initial CLI`, command names `Resident` / `Assign` / `SetEnv` |
| Frames 1440 / 1560 / 1860 | Running `Initial CLI`, command names `C:AddDataTypes` / `C:IPrefs` / `Path` |
| Frames 1980 and 2000 | Running `Workbench` Process with CLI command name `Workbench`, directory `SYS:`, nonzero module and stream BPTRs |

At frame 2000, `IPrefs` is ready; `ConClip`, `ramlib`, `RAM` and `DF0` processes
are waiting, as are input/console/trackdisk tasks. These fields support startup
progress. They do not show command completion traces, original stdout/stderr,
filesystem side effects or return-code parity. In particular, sampled Result2
fields are not individual command outcome receipts.

## Remaining boundary and next action

Absence of Exec/DOS or inaccessible slow RAM is no longer the immediate blocker.
The next bounded integration task is normal CLI launch of an authored guest probe
and collection of its guest-owned completion record. The follow-up audit below
identifies a disposable copied-image startup route that avoids dependence on the
garbled display. No host DOS shim or direct PC/vector injection should be used.
The present task did not issue guest input or alter startup/media to create a harness.

The framebuffer corruption still limits visual interaction and needs a separately
bounded display diagnosis if that path is used. This capture does not identify
its cause or justify changing disk, CPU, or display timing. KS3.1 compatibility
remains diagnostic evidence outside the engine's established KS1.3 scope.
Resident LoadResource lifetime and arbitrary command qualification remain separate
from these native startup observations.

## Read-only follow-up: disposable startup probe image

This audit made **no media edits**. The original ADF is DOS1 FFS, with 174 tree
entries. Its `S/Startup-Sequence` is 1,754 bytes, SHA-256
`64cb5972947dba207e852ad69a1a84f0aeb84e3f8b7a1f45ffde91d61de2546c`.
It has exactly one `EndCLI >NIL:` line, line 80, immediately following `C:LoadWB`
at line 79. `C/CopperProbe` does not exist. A single authored command invocation
before that EndCLI can therefore be checked unambiguously against this exact input.

Available APIs and boundaries:

| Owner | Available capability / limit |
| --- | --- |
| `CopperScreen/CopperDisk/AmigaDiskMedia.cs:160` | `IWritableAmigaSectorDiskMedia.TryWriteBytes` writes sector-image ranges; there is no pathname-level OFS/FFS insertion API |
| `CopperScreen/CopperDisk/AdfDiskMedia.cs:60` | Raw-byte writes invalidate encoded track caches; caller would still need a filesystem allocator/writer |
| Installed `amitools==0.8.1`, `amitools/tools/xdftool.py:435` | `WriteCmd` inserts a host file at a supplied Amiga path, using `ADFSVolume.write_file` |
| Installed `amitools/fs/ADFSVolume.py:354` | `write_file(data, FSString(path))` allocates a file; `delete` at line 378 removes an existing path. The volume detects DOS type at open and file serialization has OFS/FFS branches |
| `tools/Commands/Inventory/prepare_execute_fixture_adf.py:57` | Existing diagnostic helper replaces only already allocated files in a copied FFS image; it cannot insert absent `C/CopperProbe` and must not be mistaken for a general allocator |
| `tools/Commands/Inventory/inventory.py:117` | Independent read-only ADF parser; `walk` line 174 and `read_file` line 191 can verify the original/patched trees and every unchanged payload |
| `tools/DiskBuilder/build_command_image.py:46` | Already binds installed amitools identity and uses an isolated external `xdftool` process. Its production entry point admits only qualified new distribution volumes; it is not a copied-Workbench diagnostic builder |
| `tools/DiskBuilder/verify_image.py:91` | Existing strict validator expects a new DOS1 distribution with canonical dates/exact declared tree; it cannot directly validate an unchanged reference Workbench volume |

The installed package root is
`C:/Users/vsys-admin/AppData/Roaming/Python/Python314/site-packages/`.
No install/download is necessary. Its local `xdftool` has separate `write`,
`delete`, `protect`, `read` and `--read-only` operations. A replacement startup
file requires removing the old pathname before creating the new one; `write`
does not silently authorize replacement. Keep this host dependency external,
as the existing packaging owner does; do not vendor it into guest code.

Recommended bounded implementation:

1. Create a fresh, exclusively named derivative **outside the repository** from
   the hash-bound original. Resolve both paths and reject equality, containment
   outside the designated diagnostic directory, and existing outputs. Retain the
   original read-only and rehash it before/after. Never run `format`, boot install,
   `create`, or production distribution admission overrides on the copied image.
2. Use a separate diagnostic wrapper around pinned external `xdftool` to insert
   authored `C/CopperProbe` and replace only the startup file with exactly one
   invocation before the unique EndCLI. Preserve existing startup text/newlines,
   original file metadata where possible, boot blocks, and every other payload.
   Changes to allocation bitmap/directory hash links and affected timestamps must
   be explicitly recorded. Set ordinary executable protection; make no PURE claim.
3. Independently reread the copied image, check the boot-block bytes and DOS type,
   the complete expected tree (original plus one probe), all unchanged file hashes,
   the exact probe/startup bytes, and allocation/header consistency. Record writer
   package/source identity, patch specification, probe HUNK identity, source and
   derivative hashes, and allowed metadata differences. Treat the image as a
   diagnostic derivative, never an original reference or release artifact.
4. Launch through the unchanged ROM's startup CLI, using the existing public
   machine execution path. Let the authored probe publish a guest-owned completion
   record in allocated RAM, discoverable through an agreed public Exec structure,
   and keep it alive until passive capture. This preserves normal guest ownership
   and avoids direct host memory/PC/vector injection.

Guest disk output is a separate capability gap: Lightweight's
`LightweightFloppyDrive.cs:92` asserts write protection and
`LightweightDiskDma.cs:62` reports disk-write DMA unsupported. CopperDisk's host
sector writer does not change that engine contract. A RAM completion record (or
a guest `RAM:` file with a separate passive readback design) avoids requiring a
disk-write implementation. No broader emulator change is justified for this probe.
