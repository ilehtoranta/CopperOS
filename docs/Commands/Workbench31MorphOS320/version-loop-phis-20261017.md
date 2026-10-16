# Version SimplifyLoopPhis qualification — 2026-10-17

**Keep SimplifyLoopPhis disabled for Version: the shipping executable grows by eight bytes.**

## Controlled comparison

Both profiles use the exact fixed compiler payload qualified for the Version continuation-label fix: clean `eb04dac` plus the five-line fix subsequently committed as `55628a0`. This payload predates the separate remote CPU-probe/word-shift merge. Its hashes are validated against the earlier fix receipt. Within each profile the managed assemblies, SDK dependencies and existing passes are identical; only SimplifyLoopPhis is added. Both freshly compiled baselines reproduce the earlier fixed images byte for byte. The shipping baseline also matches the installed executable.

| Profile | Baseline | Candidate | Change | Qualification |
| --- | ---: | ---: | ---: | --- |
| shipping | 8,676 | 8,684 | +8 | Pass |
| morphos | 27,640 | 19,920 | -7,720 | Pass |

## Execution and stack

Both baseline and candidate run all 93 shipping and 94 private MorphOS cases on 68000, 68020 and 68040 (1,122 invocations total). The supplied-vector suites enforce their declared contracts and compare output bytes, return codes, IoErr, allocation/release ownership, image immutability and supported repeated/interleaved cases. Instruction totals and stack peaks below are per CPU; all three modes give the same totals.

| Profile | Instructions baseline → candidate | Peak stack baseline → candidate |
| --- | ---: | ---: |
| shipping | 370,984 → 353,642 | 780 → 780 |
| morphos | 3,572,993 → 3,134,276 | 1568 → 1520 |

Measured stacks fit unchanged configured limits. Instruction totals describe these fixtures, not hardware timing or a general speed guarantee. Original DOS, WinUAE/CopperScreen and actual MorphOS execution were not run. No compiler implementation changed and compiler regressions were not rerun for this pass-selection experiment; prior fixed-payload qualification remains separate.

## Encoded method changes

Largest shipping changes:

- `CopperOS.Commands.Native.NativeWorkbench31VersionCommand::TryParseDirectFileVersion`: 360 → 408 bytes (+48).
- `CopperOS.Commands.Native.NativeWorkbench31VersionCommand::ScanDirectFileForVersion`: 464 → 428 bytes (-36).
- `CopperOS.Commands.Native.NativeWorkbench31VersionCommand::ReadCommandSegmentVersion`: 408 → 440 bytes (+32).
- `CopperOS.Commands.Native.NativeWorkbench31VersionFull::Print`: 372 → 346 bytes (-26).
- `CopperOS.Commands.Native.NativeWorkbench31VersionCommand::BuildPrefixedFilePath`: 204 → 196 bytes (-8).

Largest morphos changes:

- `CopperOS.Commands.Native.NativeMorphOSVersionCommand::Run`: 11628 → 4660 bytes (-6968).
- `CopperOS.Commands.Native.NativeMorphOSVersionCommand::ParseFileVersion`: 1138 → 874 bytes (-264).
- `CopperOS.Commands.Native.NativeMorphOSVersionCommand::RunSystemVersion`: 1550 → 1392 bytes (-158).
- `CopperOS.Commands.Native.NativeMorphOSVersionCommand::TransformMd5`: 482 → 324 bytes (-158).
- `CopperOS.Commands.Native.NativeMorphOSVersionCommand::TryParseAndFormatDate`: 574 → 524 bytes (-50).

These are final method ranges; executable changes also include layout, relocation and padding effects. In the shipping profile, TryParseDirectFileVersion grows by 48 bytes and ReadCommandSegmentVersion by 32, outweighing other method savings. Their allocator spill counts increase from 5 to 15 and 8 to 17 respectively; reloads increase from 6 to 18 and 10 to 22. Removing SSA values changes register allocation, so fewer IR values can still produce a larger image. The earlier result from the unfixed dirty compiler is not a valid acceptance baseline.

## Evidence and disposition

Evidence: `artifacts/version-loop-phis-20261017`. The JSON receipt binds both profile inputs, compiler and Copper68k payloads, response files, maps, optimizer statistics, instruction/stack measurements and raw execution receipts. Flags: MC68000 resident HUNK, YOLO exceptions, memory disabled, symbols off, fixed-point peepholes. Copper68k 1.5.1 SHA-256: `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`.

Reproduce in a fresh stage with `python tools/Commands/qualify_version_loop_phis.py audit --stage artifacts/version-loop-phis-fresh`. The compiler payload and inputs are frozen under the preceding Version-fix and rollout stages. `report` refreshes the report from existing receipts without running commands again. `enable` and `publish` require passing profile and strict-size gates.

No Version policy or installed executable was changed in this investigation. The complete 61-command installed set remains 225,336 bytes. Unrelated workspace changes are preserved. This records the rejected selection before the allocator fix; the subsequent [spill investigation](version-loop-phi-spills-20261017.md) qualifies and enables the fixed policy.
