# Pending copy-home allocator rollout — 2026-10-18

## Result

Compared **61 shipping commands and 35 distinct alternate profiles**, using identical frozen assemblies for each compiler. Accepted shipping updates: **none**.

The complete 61-command inventory is **220,920 → 220,920 bytes**, saving **0 bytes**. Only strictly smaller shipping images with passing checks for every applicable profile are eligible for publication. Every unselected installed hash remains unchanged.

Of the 96 profile comparisons, **93 retain their executable size** and **82 are byte-identical**. Equal-size images are measured controls and receive no new runtime-adoption claim unless their command also has a smaller profile.

## Controlled inputs

Baseline compiler: clean `c536d31`. Candidate: that compiler plus the pending-copy-home ranking fix committed as `daab9bc`. The candidate binaries and patch are the exact retained artifacts from Type’s qualified isolated build; the dirty primary compiler checkout was not built. The prior 721 passing compiler regressions and their receipt identities are retained in this report.

Command assemblies, all adjacent dependencies, SDK identities and response flags are frozen from the preceding scalar allocator rollout. Every fresh baseline reproduces its preceding compiler image. **59 shipping baselines reproduce the installed executables byte for byte.** List and Type retain their earlier accepted installed images: the previous size-neutral List candidate was not adopted, while Type’s initial repair grew. List remains 4,604 bytes with both compilers. Type changes 4,700 → 4,696 bytes, tying its installed size, and its separate qualification is not counted as a new beneficiary here. Both retained installed identities are checked against the preceding baseline artifacts.

Flags remain MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes and symbols off, with each command’s accepted pass selection. No command sources, options, stack limits or build-policy settings change. Maps record effective passes, rewrites, code/data metrics, relocations, method sizes and allocation records.

## Runtime qualification

Each command with a smaller profile is checked in both distinct profiles where implemented. Supplied-vector fixtures run the exact baseline and candidate images on **68000, 68020 and 68040**, comparing output bytes, return codes, IoErr and command-visible resource/ownership observations. Repeated invocation and interleaving coverage is limited to the cases supplied by each fixture. Compiler-owned invocation context is accounted separately. Peak stack must remain within the existing configured limits.

| Command/profile | Calls per image/CPU | Instructions before → after | Peak stack before → after |
|---|---:|---:|---:|
| Search/shipping | 30 | 3,195,954 → 3,195,954 | 476 → 476 |
| Search/morphos | 57 | 13,191,464 → 13,191,464 | 712 → 712 |

**522 command invocations** across both images and all three CPUs. Per-CPU and individual-case instruction/stack observations are retained in the raw receipts. These are instruction counts, not execution-time measurements.

Executor: Copper68k **1.5.1**, SHA-256 `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`. Actual .NET SDK: `10.0.401`.

## Smaller profile details

### Search/morphos

- `CopperOS.Commands.Native.NativeMorphOSSearchCommand::FormatLines`: 918 → 912 encoded bytes.
- `CopperOS.Commands.Native.NativeMorphOSSearchCommand::FormatLines` allocation: spills 18 → 16, frame 56 → 52 bytes, saved registers 40 → 40 bytes.

## Complete size sweep

| Command | Shipping before → candidate | Saved | Alternate before → candidate | Decision |
|---|---:|---:|---:|---|
| AddBuffers | 1,084 → 1,084 | 0 | 1,544 → 1,544 | No shipping size reduction. |
| AddDataTypes | 7,616 → 7,616 | 0 | 7,760 → 7,760 | No shipping size reduction. |
| Assign | 2,056 → 2,056 | 0 | 1,992 → 1,992 | No shipping size reduction. |
| Avail | 1,564 → 1,564 | 0 | 2,800 → 2,800 | No shipping size reduction. |
| Beep | 268 → 268 | 0 | — | No shipping size reduction. |
| BindDrivers | 2,096 → 2,096 | 0 | 2,380 → 2,380 | No shipping size reduction. |
| Break | 1,364 → 1,364 | 0 | 2,484 → 2,484 | No shipping size reduction. |
| ChangeTaskPri | 1,360 → 1,360 | 0 | 2,084 → 2,084 | No shipping size reduction. |
| Check2090 | 504 → 504 | 0 | — | No shipping size reduction. |
| Copy | 15,208 → 15,208 | 0 | — | No shipping size reduction. |
| Date | 2,528 → 2,528 | 0 | 3,844 → 3,844 | No shipping size reduction. |
| Delete | 5,604 → 5,604 | 0 | 5,592 → 5,592 | No shipping size reduction. |
| DevList | 1,196 → 1,196 | 0 | — | No shipping size reduction. |
| Dir | 6,136 → 6,136 | 0 | 7,468 → 7,468 | No shipping size reduction. |
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
| Info | 3,420 → 3,420 | 0 | 9,500 → 9,500 | No shipping size reduction. |
| Join | 2,228 → 2,228 | 0 | 2,228 → 2,228 | No shipping size reduction. |
| LibList | 1,204 → 1,204 | 0 | — | No shipping size reduction. |
| List | 4,604 → 4,604 | 0 | 4,724 → 4,724 | No shipping size reduction. |
| LoadMonDrvs | 2,528 → 2,528 | 0 | — | No shipping size reduction. |
| Lock | 1,556 → 1,556 | 0 | 1,560 → 1,560 | No shipping size reduction. |
| MakeDir | 1,244 → 1,244 | 0 | 2,276 → 2,276 | No shipping size reduction. |
| MakeLink | 1,792 → 1,792 | 0 | 1,452 → 1,452 | No shipping size reduction. |
| ModList | 2,424 → 2,424 | 0 | — | No shipping size reduction. |
| Mount | 11,152 → 11,152 | 0 | 11,712 → 11,712 | No shipping size reduction. |
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
| Search | 5,188 → 5,188 | 0 | 9,880 → 9,876 | No shipping size reduction. |
| SetClock | 1,680 → 1,680 | 0 | 1,928 → 1,928 | No shipping size reduction. |
| SetDate | 1,816 → 1,816 | 0 | 2,068 → 2,068 | No shipping size reduction. |
| SetFont | 2,644 → 2,644 | 0 | — | No shipping size reduction. |
| SetKeyboard | 1,616 → 1,616 | 0 | 2,620 → 2,620 | No shipping size reduction. |
| Status | 3,564 → 3,564 | 0 | 3,560 → 3,560 | No shipping size reduction. |
| TaskList | 14,076 → 14,076 | 0 | — | No shipping size reduction. |
| Touch | 2,120 → 2,120 | 0 | — | No shipping size reduction. |
| Type | 4,700 → 4,696 | 4 | 4,700 → 4,696 | Type already qualified; ties installed size. |
| Version | 8,548 → 8,548 | 0 | 19,736 → 19,736 | No shipping size reduction. |
| Wait | 2,356 → 2,356 | 0 | 2,468 → 2,468 | No shipping size reduction. |
| WaitForLib | 1,300 → 1,300 | 0 | — | No shipping size reduction. |
| WaitForNotification | 1,980 → 1,980 | 0 | — | No shipping size reduction. |
| WaitForPort | 1,392 → 1,392 | 0 | — | No shipping size reduction. |
| Which | 3,896 → 3,896 | 0 | 3,896 → 3,896 | No shipping size reduction. |

## Qualification limits and evidence

The finite vector fixtures use emulated OS calls. They do not establish exhaustive equivalence or original MorphOS/PPC execution. No new original-DOS, WinUAE, original-command-image or CopperScreen checks are claimed by this rollout; earlier native checks do not qualify a newly changed image. MorphOS-only commands receive MC68000 measurements without comparing them to PPC binaries. Unsupported interleavings and minimum-stack guarantees are not inferred.

Evidence: `artifacts/type-allocator-rollout-20261018`. Reproduce with `tools/Commands/rollout_type_allocator.py` modes `prepare`, `measure`, `vectors`, `audit`, `publish`, `report`, using a fresh stage. Baselines, complete dependencies, both compilers, patch, responses, maps and runtime receipts are preserved. Publication verifies a complete staged 61-file inventory before replacing only accepted commands. The accompanying JSON contains exact identities and qualification results.
