# ExtractKickstart contract

Profile: `wb31`. Goal step: CC22. Status: source/media contract recovered,
resident native candidate compiled, and bounded native fixture passed; guest
behavior, PURE admission and package gates remain open.

The Workbench 3.1 installation image contains `C/ExtractKickstart`, a
1,216-byte helper with version `$VER: extractkickstart 39.3 (5.8.92)` and
SHA-256
`832fc20fe032a4028b0e1ebc5b4e93641cb0ed02598f6a238fa54426a14ca736`.
The captured argument template is:

```text
DEVICE/A,TO/A,1.3/S
```

The original helper is a raw trackdisk workflow rather than a normal DOS file
copy. It accepts a `DF0:`-style device name (case-insensitive `DF` prefix, one
decimal unit digit and a trailing colon), opens `trackdisk.device` for that
unit, reads the SuperKickstart boot block, validates the `KICKSUP0` signature,
selects the 1.3 or later image layout, and writes the extracted image to the
required `TO` path. The source calls DOS and utility library vectors for
argument parsing, file output and arithmetic, and uses an `IOStdReq` with
trackdisk read commands for media access. It emits distinct diagnostics for
read failures, write failures, and media that does not contain a SuperKickstart
disk; the original result and `IoErr` precedence still require guest capture.

The replacement must keep the parser result array, `RDArgs`, trackdisk request,
read buffer, output file handle, and all temporary arithmetic invocation-local.
It must use the public Kickstart 3.1 `ReadArgs`, `OpenDevice`, `DoIO`, DOS file
open/read/write/close and Exec allocation APIs, preserve the original
resident/PURE classification required by the installer flow, and never embed a
host filesystem or managed static state in the resident image.

## Source-derived operation checkpoints

- DOS and `utility.library` are opened before parsing; the captured binary
  requests the classic v36 interface level.
- `ReadArgs` receives `DEVICE/A,TO/A,1.3/S` and a three-slot result array.
- The device spelling is validated before opening `trackdisk.device`.
- The boot-block read is 512 bytes and is rejected unless its first eight
  bytes are `KICKSUP0`.
- A second 1 KiB read supplies the source lengths at boot-block offsets `+8`
  and `+12`. With `1.3/S`, the source copies that first length from raw offset
  `$C1400`; without it, it copies two `$40000` chunks from `$40400` and
  `$80400`, followed by the second length from `$C0400`.
- Every selected image chunk is written to the required destination; all
  device and DOS leases are released on every failure path.

These checkpoints are disassembly observations, not a completed behavioral
contract. A disposable Workbench guest must confirm offsets, image lengths,
the 1.3 layout distinction, diagnostics, short-read/short-write behavior,
result levels, and `IoErr` preservation before native qualification.

## Native candidate checkpoint (2026-09-21)

`src/Commands/Native/NativeWorkbench31ExtractKickstartCommand.cs` and
`tests/Commands.AddBuffersNativeRoot/Workbench31ExtractKickstartEntry.cs`
implement the source-derived raw trackdisk path. The candidate keeps the
256 KiB `AllocVec` buffer, message port, IOStdReq, DOS inhibition lease,
destination handle and parser ownership inside one invocation. It compiles as
a resident HUNK for 68000/020/040 with fourteen reachable methods and zero
managed allocation sites, runtime features/helpers, external targets,
exception regions or shared-image writes. The bounded native fixture passes
seventeen vectors per CPU for both layouts, parser and library failures,
diagnostics, cleanup and interleaving. Receipt:
`artifacts/extractkickstart-wb31-native-20260921-v5/qualification.json`; it does
not claim guest or shipping qualification.

The candidate retains the captured utility lease at capability floor v0
because the replacement does not call a utility.library vector; the captured
v36 request remains differential evidence.

## Required implementation and evidence steps

1. Add a DOS36 resident entry with explicit Workbench-startup and malformed
   argument-buffer guards.
2. Implement the parser and validation boundary, including exact device
   spelling and `1.3` switch behavior.
3. Implement invocation-owned trackdisk `IOStdReq` setup, boot-block signature
   validation, layout-specific reads, DOS destination writes, and cleanup.
4. [x] Build synthetic trackdisk/DOS fixtures for valid 1.3 and later images,
   invalid signatures, short reads/writes, missing device, parser failures,
   missing DOS, Workbench startup, repeated and interleaved callers.
5. [x] Compile resident HUNKs for 68000/020/040, prove no managed allocation or
   shared-image writes, and run the bounded fixture. Compare with the original
   guest before changing
   the ledger beyond source-contract partial status.

Extracted Kickstart media and binary contents remain private licensed evidence;
they must not be redistributed in the repository or package.
