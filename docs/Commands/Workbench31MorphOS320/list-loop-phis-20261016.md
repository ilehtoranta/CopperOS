# List loop-phi cleanup — 2026-10-16

**Installed List: 10,232 → 4,684 bytes; 5,548 bytes saved (54.2%).**
The complete preserved 61-command inventory falls from 239,648 to
234,100 bytes. Only List changes.

## Relevance and change

Search's loop-phi work was already qualified and enabled on October 12. List
still used its older pass selection. List now also selects the existing,
default-off `SimplifyLoopPhis` policy. No compiler implementation, command
source behavior, or global default changes. Explicit policy overrides keep
their existing behavior.

The cleanup replaces a phi whose only distinct non-self input is one value.
It removes redundant loop-carried SSA transport before allocation, retaining
the existing type, width, register, GC and byref substitution checks. The
allocator is unchanged. Eligibility remains MC68000 resident HUNK, YOLO
exceptions and memory disabled.

| Main-method allocation measure, isolated compiler | Before | After |
| --- | ---: | ---: |
| SSA values | 2993 | 810 |
| Accumulated spill selections | 1073 | 38 |
| Reload operations | 495 | 84 |
| Spill bytes | 68 | 48 |
| Local frame bytes | 172 | 148 |
| Saved-register bytes | 40 | 40 |
| Allocation rounds | 5 | 2 |

Spill selections accumulate over rounds; they are not distinct permanent slots.

## Independent measurements

All compiler comparisons use identical frozen managed assemblies, SDK inputs,
flags and runner payload. The isolated baseline reproduces the installed
10,232-byte image exactly.

| Payload/profile | Executable bytes |
| --- | ---: |
| baseline | 10,232 |
| candidate | 4,700 |
| morphos-baseline | 11,000 |
| morphos-candidate | 5,144 |
| main-baseline | 10,200 |
| main-candidate | 4,684 |
| main-morphos-candidate | 5,148 |

The isolated compiler is clean `02a7fa3` in
`CopperSharp68k-search-pressure-20261011`. Its independent pass saves 5,532 bytes
in the Workbench profile and 5,856 bytes in the private MorphOS profile. The
current main compiler starts at `eb04dac` plus its preserved working-tree edits.
On those same inputs, adding the pass saves 5,516 bytes (10,200 → 4,684).
The additional 32-byte baseline difference belongs to the current toolchain,
not to this pass. The final 4,684-byte source publish reproduces the main
candidate exactly. Current SDK/compiler changes are qualified only within
this recorded List boundary; no blanket correctness claim is made for them.

The recorded genuine Workbench 3.1 List is 5,108 bytes.
The installed image is 424 bytes smaller, with a size ratio
of 0.917×. This does not establish functional parity with
that original. No PPC MorphOS size comparison is made.

## Qualification and execution costs

- **1171 focused compiler tests pass**, covering loop-carried values,
  register allocation, resident execution, peepholes and existing generated-size
  execution regressions. No compiler source or assertion was edited.
- Workbench: 32 baseline/candidate invocations per CPU; private MorphOS: 31
  per CPU. Both profiles pass on 68000, 68020 and 68040 with exact output,
  return/IoErr, resource ownership, repeated/interleaved invocation and image
  immutability comparisons. These checks are also repeated against the current
  main payload and the freshly published Workbench image.
- Final Workbench fixture instructions per CPU: 64,063 →
  40,384; peak stack: 292 →
  268 bytes, within unchanged fixture stack limits.
  These are bounded fixture instruction totals, not hardware timing or a
  general speed claim. No added execution cost was observed in these vectors.
- Executor: Copper68k 1.5.1, SHA-256 `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`.
  The frozen executor comes from the October 11 correctness checkpoint.

## Build, publication and reproduction

`CodeSizePolicies.props` enables the pass only for List in this change. The
normal List source publish, with no optimization overrides and a frozen copy
of the current main compiler payload, produces the qualified image. Build:

```powershell
pwsh -NoProfile -File tools/Commands/build-c.ps1 -Command List
```

The default main compiler checkout contains unrelated ongoing changes; future
builds from a changed checkout require fresh measurement and qualification.
For the exact experiment use the frozen `main-candidate/compiler` payload.
`audit_search_pressure.py freeze --command List` now supports this same freeze,
compile, compile-morphos and qualify workflow while retaining Search as the
backward-compatible default. Both profile inputs, pass lists, compiler patches,
SDK identities, map details, tests and all CPU receipts are bound by the JSON
report. Evidence is `artifacts/list-pressure-20261016`.

Before refreshing `out/C/List`, all 61 accepted images were staged, preserved
and hash-checked. Only List was replaced; the other 60 images were preserved
byte for byte. No stale files were deleted. Unrelated Shell/ImageBuilder and
compiler changes were preserved. At the measurement checkpoint, these changes
were not yet committed.

## Limits and next work

These are supplied-vector fixtures. New List runs under original DOS/WinUAE,
original Workbench functional parity, actual MorphOS runtime execution,
shipping/PURE admission and absolute minimum stack remain unqualified. The
frozen runner's retained source-drift limits carry forward from the correctness
checkpoint. The full compiler suite and the other 60 command builds were not
rerun; this is a filtered per-command rollout of an existing compiler pass.

Next qualify List under original DOS using the Search-style native OS harness.
After that, measure other large commands with loop-carried SSA transport before
attempting a new allocator heuristic.
