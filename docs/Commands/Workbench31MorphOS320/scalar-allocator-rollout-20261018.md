# Scalar register allocator rollout — 2026-10-18

## Accepted images

| Command | Installed before | Accepted | Saved | MorphOS before → after | Workbench 3.1 ratio |
|---|---:|---:|---:|---:|---:|
| AddDataTypes | 7,624 | 7,616 | 8 | 7,768 → 7,760 | 1.295× |
| Copy | 15,236 | 15,208 | 28 | same implemented body | 2.725× |
| Delete | 5,612 | 5,604 | 8 | 5,600 → 5,592 | 2.842× |
| Dir | 6,164 | 6,136 | 28 | 7,512 → 7,468 | 1.784× |
| Mount | 11,156 | 11,152 | 4 | 11,716 → 11,712 | 1.621× |

Refreshed only these 5 executables in `out/C`. All 61 images remain present: **220,996 → 220,920 bytes**, saving **76 bytes**. Every other installed hash is unchanged. Search’s previously accepted 4-byte saving is excluded.

### Other findings

- Info’s MorphOS profile saves **88 bytes** (9,588 → 9,500); its 3,420-byte shipping image is unchanged. Both profiles pass.
- Version’s MorphOS profile saves **32 bytes** (19,768 → 19,736); its 8,548-byte shipping image is unchanged. Both profiles pass. Its MorphOS fixture gains 819 instructions per CPU, with unchanged peak stack.
- Type grows **4 bytes** in both profiles (4,696 → 4,700), so its installed image is retained. Its `Copy` helper still has four spilled values while saved-register storage increases from 36 to 40 bytes. It receives no adoption claim or new runtime qualification.
- List, Execute and Exe2Arc do not shrink with this fix. Their installed images are retained.

## Controlled comparison

Compared all 61 accepted shipping command assemblies and 35 distinct alternate profiles using frozen complete adjacent dependencies and unchanged response flags. Both compilers are retained from the Search allocator investigation: clean `9eaa2aa` and that exact compiler plus the bounded narrow-scalar reassignment committed as `c536d31`. Compiler binaries, source patch, SDK identities and the 717 passing compiler regressions remain bound by the preceding Search qualification report. Unrelated current CopperSharp and CopperOS edits were excluded.

The policy remains opt-in through the existing `SimplifyLoopPhis` setting. There are no command source, option, stack-limit or build-policy changes. Images use MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes and symbols off. Effective passes, code/data sizes, reachable methods, relocations, allocation records and largest methods are recorded in the JSON and retained maps.

Shipping baselines must reproduce the installed bytes before adoption. Search intentionally reproduces its pre-fix 5,192-byte baseline and its already installed 5,188-byte candidate. Complete CIL/dependency directories are frozen to retain the compiler’s adjacent-assembly metadata discovery.

## Qualification and measured costs

For every size beneficiary, both available distinct profiles were exercised with the same exact baseline and candidate binaries on 68000, 68020 and 68040. The supplied-vector fixtures compare output bytes, return codes, IoErr and command-visible resource/ownership observations, repeated invocation and their supported interleavings. Candidate peak stack remains within the fixtures’ existing configured limits. Compiler-owned invocation context is accounted separately by the existing fixtures.

| Command/profile | Calls per image/CPU | Instructions before → after | Peak stack before → after |
|---|---:|---:|---:|
| AddDataTypes/shipping | 15 | 31,346 → 31,294 | 516 → 512 |
| AddDataTypes/morphos | 42 | 88,548 → 88,287 | 520 → 516 |
| Copy/shipping | 121 | 7,767,507 → 7,767,039 | 688 → 688 |
| Delete/shipping | 8 | 252,050 → 252,050 | 528 → 528 |
| Delete/morphos | 8 | 252,222 → 252,222 | 528 → 528 |
| Dir/shipping | 33 | 756,090 → 754,086 | 384 → 388 |
| Dir/morphos | 42 | 792,299 → 792,046 | 844 → 852 |
| Info/shipping | 14 | 12,756 → 12,756 | 336 → 336 |
| Info/morphos | 66 | 284,986 → 283,673 | 736 → 736 |
| Mount/shipping | 9 | 2,241 → 2,229 | 308 → 308 |
| Mount/morphos | 5 | 1,458 → 1,443 | 260 → 264 |
| Version/shipping | 93 | 351,049 → 351,049 | 784 → 784 |
| Version/morphos | 94 | 3,235,528 → 3,236,347 | 1524 → 1524 |

Size-beneficiary vector coverage: **3,300 invocations** across both images and all three CPUs. These are executed instruction counts, not wall-clock timing claims. Per-CPU values and any individual-case stack changes remain in the raw receipts. Size takes priority over execution speed.

Vector executor: Copper68k **1.5.1**, SHA-256 `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`. The complete executor and core hashes are frozen.

### Dir under original DOS

**588 original Kickstart 3.1 DOS invocations** pass across both profiles and all three CPUs, covering 27 finite contracts at the existing 4 KiB/8 KiB stacks. Eight auditor mutation controls are rejected. The independent WinUAE runs pass **96 command invocations** (eight cases, both images, both profiles and all three CPUs), with JIT disabled and 4 KiB stacks. Output files and return/IoErr records are rechecked against the declared contracts, including OPT Z, lowercase ignored options and multiple ignored options.

The retained native executor uses Copper68k **1.5.4-detroit-dev.2+ddc753000b492a15048ba6c16a06e03c0c160a63**, SHA-256 `5780f5084fd44e2d4732969bc0f9ce7ae7e3beffc1135ded92bc46d9b21399f2`. WinUAE is **6.0.3.0**, SHA-256 `f3e55700fd811cd543fdfebf6c3221cfaa3d385828f110c6d3eb534c64503123`. ROM SHA-256: `8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee`. Source bindings, complete native runtime identities, guest inputs and raw results are retained. Original DOS runs exercise the MC68000 MorphOS-profile binary on Kickstart DOS; they are not original MorphOS execution.

### Focused Delete soft-link qualification

Delete’s eight command cases do not descend through the changed `CanEnter` helper. Added **54 supplementary invocations**: nine helper cases, both compilers and all three CPUs. These cover lock success, other lock errors, missing device, non-link, dangling link, quiet/warning output, allocation failure and interleaved callers. The helper is **314 → 306 bytes**. Its complete emitted body is identical to the helper in both Delete profiles after normalizing exactly two HUNK relocations, whose targets are verified as the same empty string and warning text. All remaining instructions, branches, register saves, stack offsets and call operands must match byte for byte.

The first supplementary run exposed an existing fixture bug: the generic allocation fallback expected eight bytes for the 512-byte soft-link buffer, failing identically with both compilers. Corrected the fixture to require exactly one 512-byte Public allocation, preserve failure injection, and verify exact ownership/release. The isolated rebuilt runner and sources are frozen separately; the complete command matrix retains its original executor. No compiler or command source change was needed. Supplementary instructions fall from 1,493 to 1,490 per CPU, while peak stack rises from 96 to 100 bytes within the existing limit.

## Where allocation improves

### AddDataTypes/shipping

- `CopperOS.Commands.Native.NativeMorphOSAddDataTypesCommand::ComparePriority`: spills 1 → 0, frame 4 → 0 bytes, saved registers 32 → 36 bytes.
- `CopperOS.Commands.Native.NativeMorphOSAddDataTypesCommand::RegisterFileHeader`: spills 7 → 6, frame 36 → 32 bytes, saved registers 40 → 40 bytes.

### AddDataTypes/morphos

- `CopperOS.Commands.Native.NativeMorphOSAddDataTypesCommand::ComparePriority`: spills 1 → 0, frame 4 → 0 bytes, saved registers 32 → 36 bytes.
- `CopperOS.Commands.Native.NativeMorphOSAddDataTypesCommand::RegisterFileHeader`: spills 7 → 6, frame 36 → 32 bytes, saved registers 40 → 40 bytes.

### Copy/shipping

- `CopperOS.Commands.Native.NativeMorphOSCopyMakeDirectory::OpenDirectory`: spills 3 → 1, frame 16 → 12 bytes, saved registers 32 → 40 bytes.
- `CopperOS.Commands.Native.NativeMorphOSCopySoftLinkCheck::CanEnter`: spills 4 → 3, frame 24 → 24 bytes, saved registers 36 → 40 bytes.

### Delete/shipping

- `CopperOS.Commands.Native.NativeMorphOSCopySoftLinkCheck::CanEnter`: spills 4 → 3, frame 24 → 24 bytes, saved registers 36 → 40 bytes.

### Delete/morphos

- `CopperOS.Commands.Native.NativeMorphOSCopySoftLinkCheck::CanEnter`: spills 4 → 3, frame 24 → 24 bytes, saved registers 36 → 40 bytes.

### Dir/shipping

- `CopperOS.Commands.Native.NativeMorphOSDirCommand::ListDirectory`: spills 12 → 11, frame 64 → 68 bytes, saved registers 40 → 40 bytes.
- `CopperOS.Commands.Native.NativeMorphOSDirCommand::ListPattern`: spills 8 → 7, frame 40 → 40 bytes, saved registers 40 → 40 bytes.
- `CopperOS.Commands.Native.NativeMorphOSDirCommand::WriteFileRows`: spills 1 → 0, frame 24 → 20 bytes, saved registers 36 → 40 bytes.
- `CopperOS.Commands.Native.NativeMorphOSDirCommand::WriteMatchedDirectory`: spills 2 → 1, frame 16 → 16 bytes, saved registers 36 → 40 bytes.

### Dir/morphos

- `CopperOS.Commands.Native.NativeMorphOSDirCommand::ListChildDirectory`: spills 4 → 4, frame 20 → 24 bytes, saved registers 40 → 40 bytes.
- `CopperOS.Commands.Native.NativeMorphOSDirCommand::ListDirectory`: spills 13 → 12, frame 64 → 64 bytes, saved registers 40 → 40 bytes.
- `CopperOS.Commands.Native.NativeMorphOSDirCommand::ListPattern`: spills 8 → 8, frame 40 → 44 bytes, saved registers 40 → 40 bytes.
- `CopperOS.Commands.Native.NativeMorphOSDirCommand::WriteFileRows`: spills 1 → 0, frame 24 → 20 bytes, saved registers 36 → 40 bytes.
- `CopperOS.Commands.Native.NativeMorphOSDirCommand::WriteMatchedDirectory`: spills 2 → 1, frame 16 → 16 bytes, saved registers 36 → 40 bytes.
- `CopperOS.Commands.Native.NativeMorphOSSearchSoftLinks::ShouldDescend`: spills 5 → 5, frame 24 → 28 bytes, saved registers 40 → 40 bytes.

### Info/morphos

- `CopperOS.Commands.Native.NativeMorphOSInfoCommand::RenderRecords`: spills 26 → 25, frame 240 → 240 bytes, saved registers 40 → 40 bytes.

### Mount/shipping

- `CopperOS.Commands.Native.NativeMorphOSMountCommand::RunProfile`: spills 5 → 5, frame 116 → 120 bytes, saved registers 40 → 40 bytes.
- `CopperOS.Commands.Native.NativeMorphOSMountCommand::RunWorkbenchProfile`: spills 1 → 0, frame 36 → 32 bytes, saved registers 36 → 40 bytes.

### Mount/morphos

- `CopperOS.Commands.Native.NativeMorphOSMountCommand::RunProfile`: spills 5 → 5, frame 116 → 120 bytes, saved registers 40 → 40 bytes.
- `CopperOS.Commands.Native.NativeMorphOSMountCommand::RunWorkbenchProfile`: spills 1 → 0, frame 36 → 32 bytes, saved registers 36 → 40 bytes.

### Search/shipping

- `CopperOS.Commands.Native.NativeMorphOSSearchCommand::FormatLines`: spills 10 → 10, frame 36 → 40 bytes, saved registers 40 → 40 bytes.
- `CopperOS.Commands.Native.NativeMorphOSSearchCommand::Run`: spills 27 → 27, frame 140 → 140 bytes, saved registers 40 → 40 bytes.

### Search/morphos

- `CopperOS.Commands.Native.NativeMorphOSSearchCommand::FormatLines`: spills 19 → 18, frame 52 → 56 bytes, saved registers 40 → 40 bytes.
- `CopperOS.Commands.Native.NativeMorphOSSearchCommand::LiteralContains`: spills 2 → 1, frame 8 → 4 bytes, saved registers 36 → 40 bytes.
- `CopperOS.Commands.Native.NativeMorphOSSearchSoftLinks::ShouldDescend`: spills 5 → 5, frame 24 → 28 bytes, saved registers 40 → 40 bytes.

### Type/shipping

- `CopperOS.Commands.Native.NativeMorphOSTypeTextIo::Copy`: spills 4 → 4, frame 32 → 32 bytes, saved registers 36 → 40 bytes.

### Type/morphos

- `CopperOS.Commands.Native.NativeMorphOSTypeTextIo::Copy`: spills 4 → 4, frame 32 → 32 bytes, saved registers 36 → 40 bytes.

### Version/morphos

- `CopperOS.Commands.Native.NativeMorphOSVersionCommand::ParseFileVersion`: spills 4 → 2, frame 44 → 40 bytes, saved registers 36 → 40 bytes.
- `CopperOS.Commands.Native.NativeMorphOSVersionCommand::RunMorphOSDirectoryFileCandidates`: spills 11 → 11, frame 108 → 104 bytes, saved registers 40 → 40 bytes.

## Complete size sweep

| Command | Shipping before → candidate | Saved | Alternate before → candidate | Decision |
|---|---:|---:|---:|---|
| AddBuffers | 1,084 → 1,084 | 0 | 1,544 → 1,544 | No shipping size reduction. |
| AddDataTypes | 7,624 → 7,616 | 8 | 7,768 → 7,760 | accepted |
| Assign | 2,056 → 2,056 | 0 | 1,992 → 1,992 | No shipping size reduction. |
| Avail | 1,564 → 1,564 | 0 | 2,800 → 2,800 | No shipping size reduction. |
| Beep | 268 → 268 | 0 | — | No shipping size reduction. |
| BindDrivers | 2,096 → 2,096 | 0 | 2,380 → 2,380 | No shipping size reduction. |
| Break | 1,364 → 1,364 | 0 | 2,484 → 2,484 | No shipping size reduction. |
| ChangeTaskPri | 1,360 → 1,360 | 0 | 2,084 → 2,084 | No shipping size reduction. |
| Check2090 | 504 → 504 | 0 | — | No shipping size reduction. |
| Copy | 15,236 → 15,208 | 28 | — | accepted |
| Date | 2,528 → 2,528 | 0 | 3,844 → 3,844 | No shipping size reduction. |
| Delete | 5,612 → 5,604 | 8 | 5,600 → 5,592 | accepted |
| DevList | 1,196 → 1,196 | 0 | — | No shipping size reduction. |
| Dir | 6,164 → 6,136 | 28 | 7,512 → 7,468 | accepted |
| DiskChange | 1,188 → 1,188 | 0 | 1,188 → 1,188 | No shipping size reduction. |
| DiskFree | 1,976 → 1,976 | 0 | — | No shipping size reduction. |
| DosList | 9,736 → 9,736 | 0 | — | No shipping size reduction. |
| Eval | 9,568 → 9,568 | 0 | 13,680 → 13,680 | No shipping size reduction. |
| Exe2Arc | 13,836 → 13,836 | 0 | — | No shipping size reduction. |
| Execute | 5,812 → 5,812 | 0 | — | No shipping size reduction. |
| ExtractKickstart | 2,640 → 2,640 | 0 | — | No shipping size reduction. |
| FileNote | 2,048 → 2,048 | 0 | 2,052 → 2,052 | No shipping size reduction. |
| FindResident | 1,060 → 1,060 | 0 | — | No shipping size reduction. |
| Format | 7,696 → 7,696 | 0 | — | No shipping size reduction. |
| GuessBootDev | 2,080 → 2,080 | 0 | — | No shipping size reduction. |
| IconPos | 2,424 → 2,424 | 0 | — | No shipping size reduction. |
| Info | 3,420 → 3,420 | 0 | 9,588 → 9,500 | No shipping size reduction. |
| Join | 2,228 → 2,228 | 0 | 2,228 → 2,228 | No shipping size reduction. |
| LibList | 1,204 → 1,204 | 0 | — | No shipping size reduction. |
| List | 4,604 → 4,604 | 0 | 4,724 → 4,724 | No shipping size reduction. |
| LoadMonDrvs | 2,528 → 2,528 | 0 | — | No shipping size reduction. |
| Lock | 1,556 → 1,556 | 0 | 1,560 → 1,560 | No shipping size reduction. |
| MakeDir | 1,244 → 1,244 | 0 | 2,276 → 2,276 | No shipping size reduction. |
| MakeLink | 1,792 → 1,792 | 0 | 1,452 → 1,452 | No shipping size reduction. |
| ModList | 2,424 → 2,424 | 0 | — | No shipping size reduction. |
| Mount | 11,156 → 11,152 | 4 | 11,716 → 11,712 | accepted |
| PathPart | 2,084 → 2,084 | 0 | — | No shipping size reduction. |
| PortList | 1,460 → 1,460 | 0 | — | No shipping size reduction. |
| Protect | 3,264 → 3,264 | 0 | 3,268 → 3,268 | No shipping size reduction. |
| Quote | 6,040 → 6,040 | 0 | — | No shipping size reduction. |
| Reboot | 516 → 516 | 0 | 516 → 516 | No shipping size reduction. |
| Relabel | 1,060 → 1,060 | 0 | 1,536 → 1,536 | No shipping size reduction. |
| Rename | 2,592 → 2,592 | 0 | 2,536 → 2,536 | No shipping size reduction. |
| RequestChoice | 2,204 → 2,204 | 0 | 2,844 → 2,844 | No shipping size reduction. |
| RequestFile | 2,952 → 2,952 | 0 | 3,068 → 3,068 | No shipping size reduction. |
| ResList | 1,076 → 1,076 | 0 | — | No shipping size reduction. |
| Search | 5,192 → 5,188 | 4 | 9,896 → 9,880 | Already qualified and installed in the Search allocator investigation. |
| SetClock | 1,680 → 1,680 | 0 | 1,928 → 1,928 | No shipping size reduction. |
| SetDate | 1,816 → 1,816 | 0 | 2,068 → 2,068 | No shipping size reduction. |
| SetFont | 2,644 → 2,644 | 0 | — | No shipping size reduction. |
| SetKeyboard | 1,616 → 1,616 | 0 | 2,620 → 2,620 | No shipping size reduction. |
| Status | 3,564 → 3,564 | 0 | 3,560 → 3,560 | No shipping size reduction. |
| TaskList | 14,076 → 14,076 | 0 | — | No shipping size reduction. |
| Touch | 2,120 → 2,120 | 0 | — | No shipping size reduction. |
| Type | 4,696 → 4,700 | -4 | 4,696 → 4,700 | No shipping size reduction. |
| Version | 8,548 → 8,548 | 0 | 19,768 → 19,736 | No shipping size reduction. |
| Wait | 2,356 → 2,356 | 0 | 2,468 → 2,468 | No shipping size reduction. |
| WaitForLib | 1,300 → 1,300 | 0 | — | No shipping size reduction. |
| WaitForNotification | 1,980 → 1,980 | 0 | — | No shipping size reduction. |
| WaitForPort | 1,392 → 1,392 | 0 | — | No shipping size reduction. |
| Which | 3,896 → 3,896 | 0 | 3,896 → 3,896 | No shipping size reduction. |

## Qualification limits

Copy and Beep ship their sole implemented MorphOS-derived body; they do not have a second independent Workbench body. MorphOS-only commands likewise receive MC68000 measurements without PPC size comparisons. Workbench ratios use the genuine media identities in the receiver-classification report.

Supplied-vector fixtures are finite checks with emulated OS calls. They do not establish exhaustive equivalence to original commands or original MorphOS/PPC execution. Original-image and CopperScreen checks are unavailable for commands without a retained native OS harness. No new minimum-stack or unsupported interleaving guarantee is inferred.

Evidence: `artifacts/scalar-allocator-rollout-20261018`. Reproduce with `tools/Commands/rollout_scalar_allocator.py prepare`, `measure`, `vectors`, relevant native modes, `audit` and `publish` using a fresh staging directory. Baseline executables, responses, complete dependencies, compilers, maps and execution receipts are preserved. Publication stages the complete 61-file inventory before replacing only accepted commands.
