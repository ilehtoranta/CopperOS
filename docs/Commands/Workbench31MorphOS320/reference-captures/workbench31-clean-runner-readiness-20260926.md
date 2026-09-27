# Workbench 3.1 clean-runner readiness capture — 2026-09-26

The existing CopperScreen Lightweight runner executes the available Kickstart
3.1 ROM and Workbench disk for **2,000 frames without reporting an unsupported
feature**. This exploratory capture does **not** establish DOS, Shell, full-boot
or command readiness. No commands receive coverage credit.

The runner exited **0**, completed all requested frames at cycle `284204014`,
and produced 35 passive snapshots. The unchanged final fingerprints agree with
the preceding run without the boot probe: CPU `AC179436D9BA9EAE`, hardware
`D078736DAFDE11A7`, output `5F77038F9388B112`. This agreement is bounded evidence
about these end states, not a hardware-timing qualification. FPS and allocation
figures in stdout are diagnostic only.

Evidence:

- [Capture receipt](../../../../artifacts/workbench31-clean-runner-readiness-20260926-83ec7e24/capture-receipt.json) records the executable, exact argument array, process exit, wall time, binary/media hashes before and after, and hashes of every original output file.
- [Passive analysis](../../../../artifacts/workbench31-clean-runner-readiness-20260926-83ec7e24/readiness-analysis.json) records all 35 bounded ChipRam inspections and their limitations.
- [Analysis script](../../../../artifacts/workbench31-clean-runner-readiness-20260926-83ec7e24/analyze-passive-snapshots.py) reads only captured files and the reference ROM; it never calls a guest bus or modifies guest state.
- [Final framebuffer](../../../../artifacts/workbench31-clean-runner-readiness-20260926-83ec7e24/snapshots/frame-002000.bmp) and [final CPU/device snapshot](../../../../artifacts/workbench31-clean-runner-readiness-20260926-83ec7e24/snapshots/frame-002000.json) preserve the observed state.

## Configuration and identities

The existing Release binary is under
`C:/D-drive/Koodit/GIT/CopperScreen/CopperMod.Amiga.Lightweight.Runner/bin/Release/net10.0/`.
Arguments selected the original ROM and ADF, `--frames 2000 --warmup 0`, and
`--boot-probe` pointing to the fresh capture's `snapshots` directory. The native
runner uses A500 PAL OCS, a 68000, 512 KiB Chip RAM and 512 KiB slow RAM. The
supported product profile remains Kickstart 1.3; accepting a 512 KiB ROM and
finishing this interval does not expand that support claim.

| Input | SHA-256 |
| --- | --- |
| Kickstart 3.1 A500, 40.63 | `8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee` |
| Workbench 40.42 M10 disk 2 | `a8f167bad2897e8c7f2cfe73de7a9f8dad10c7a5b1f78ca17f818ab17278b985` |
| Runner DLL | `37723270caf0620dbd86eaf71c5716aecb9f296c81269127f4f8d7757b3a4ae6` |
| Lightweight engine DLL | `e64ad1fd16effa63ed33ce1cd3811a1580774c67cccf5fe7584d71f520939425` |
| Copper68k DLL | `a384427754b2d699527ded17804fb8cee0d37ce13224c8ef77b4f2e2ddff4f6a` |

All bound inputs were unchanged afterward. There was no build, emulator edit,
media edit, input injection, DOS gateway, application-session startup or guest
state patch. Current source inspection identifies the independent engine path;
PDB-to-current-source binding was not established for these existing binaries.

## What prevents a readiness conclusion

At the final frame, ChipRam address 4 contains ExecBase **`0x00C00B00`**, in
slow RAM. The existing `NativeBootProbe` writes ChipRam only. The Exec library,
its library/task lists and their DOS/Process/CLI targets therefore cannot be
inspected from this snapshot. Across the bounded capture, zero DOS roots and
zero complete Process structures were inspected. An inaccessible structure is
not evidence that it is absent or invalid.

The final framebuffer was visually inspected. It shows a gray GUI-like surface
with pronounced horizontal striping and garbled text; no legible Shell prompt
was established. No display, DMA or CPU cause is attributed from that image.

The next discriminating action is a **passive slow-RAM snapshot or mapped-memory
peek in a private host runner**, retaining the same engine and media. Inspect
original Exec/DOS/Process/CLI and filesystem readiness before delivering a
command. This is more informative than extending a blind frame count. After
readiness is demonstrated, a disposable probe-bearing disk and guest-owned
output/return/IoErr completion protocol still need implementation.

Current source anchors in the sibling CopperScreen tree are
`LightweightA500Machine.cs:394` (256/512 KiB ROM admission),
`CopperMod.Amiga.Lightweight.Runner/Program.cs:61` (`--boot-probe`),
`NativeBootProbe.cs:36` (passive capture), and
`LightweightFloppyDrive.cs:31`/`:92` (owned ADF copy and write-protect pin).
The skill used for this bounded diagnosis was `amiga-cycle-exact`; it introduced
no approval requirement or emulator modification.
