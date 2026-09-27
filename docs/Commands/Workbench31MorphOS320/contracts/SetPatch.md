# `SetPatch` contract

Profile: `wb31` (`CC20.SetPatch.wb31`). Goal stage: CC20. Recorded
2026-09-22. This is a reference boundary, not a shipping implementation.

## Reference identity

The 13,484-byte `C:SetPatch` HUNK is byte-identical on the selected Install3.1
and Workbench3.1 media members. Its SHA-256 is
`745b2f90fabcdeccba099b3302593eff89f393d65bd84a9c6bad1b46063ab5cc` and its
version tag is `setpatch 40.16 (14.2.94)`. Both media copies have protection
word zero, so the original is not marked PURE. The complete bounded audit is
[`setpatch-wb31-binary-audit-20260922.json`](../reference-captures/setpatch-wb31-binary-audit-20260922.json).

## Invocation boundary

The binary contains the candidate DOS `ReadArgs` template:

```text
QUIET/S,NOCACHE/S,REVERSE/S,NOAGA/S
```

This remains a syntax candidate until a disposable Workbench guest captures
help, parser errors, switch precedence, result levels and `IoErr`. The
replacement must use Kickstart DOS `ReadArgs`/`FreeArgs`, preserve the empty
tail behavior, and reject Workbench startup unless an original guest proves a
different startup contract.

## Machine and patch boundary

SetPatch is a machine-specific patch installer. The captured body references
Exec, DOS, graphics, Intuition, layers, console, SCSI, trackdisk, timer,
nonvolatile and CD devices plus `68040.library`. Its strings identify patches
for A3000 RAMSEY/DMAC, 68040 support, graphics/Intuition, CIA and console,
SCSI/trackdisk, DOS `ExAll`/`Flush`/`Open`, NMI and Line-A vectors, monitor and
cache behavior, nonvolatile storage, third-party Nu* hardware and memory pools.

An implementation must first select an explicitly supported ROM/machine
profile and prove each patch's address, version and idempotence predicate from
the original guest. It must never scan or modify unknown ROM addresses, use a
host callback as a patch target, or report success for an unsupported machine.
`NOCACHE`, `REVERSE` and `NOAGA` must affect the original patch ordering and
selection only after guest evidence freezes their meaning. `QUIET` may suppress
diagnostics but cannot suppress required failure results.

The repeated-install path must detect an already-installed SetPatch body and
preserve the original `already been installed` result/diagnostic. Patch lists
and machine state are process/global resources, so the implementation must
serialize installation, retain live patch ownership, and provide a tested
restart path rather than unloading code still referenced by a vector.

## Required qualification gates

Before admission, capture and qualify:

1. Workbench guest parser/help, all switch combinations, exact result/IoErr and
   diagnostic text;
2. cold and warm startup on every supported ROM/machine profile, including
   unsupported-Kickstart and missing-device paths;
3. each patch predicate, address range, ordering, reverse mode, cache mode and
   already-installed detection with byte-level ROM/RAM diffs;
4. failure rollback after every partial patch and safe repeated invocation;
5. resident/lifecycle and concurrent invocation behavior without dangling
   vector targets or shared-image writes; and
6. package placement, source licensing and original-guest differential parity.

Until those gates pass, keep `CC20.SetPatch.wb31` open and do not create a
native no-op or success stub.
