# MakeDir original/native vector fixture

This independent executor runs the private original Workbench 37.2 machine
code and the generated `Workbench31MakeDirProbe` machine code. Both receive
the same finite, independently supplied DOS vector outcomes. It does not
modify or replace the Foundation startup/argument suites.

The CLI takes exactly five arguments, in this order:

```text
<generated.hunk> <68000|68020|68040> <report.json> makedir-classic-reference-vector-fixture <private-original.hunk>
```

The original is required, must remain outside this repository, must be 464
bytes, and must have SHA256
`23911db49742055d8bddcfe5f8de82cbb2232f8a7ef850d51bfd27f9b54c819b`.
The admitted private path is currently
`D:/TestData/CopperOSCommands/Workbench31/MakeDir-37.2.hunk`.
The fixture never copies original bytes into the repo, snapshots or receipts.
Run from the repository or keep the executor under its normal project path
so it can validate that boundary. Missing or mismatched reference is failure,
with no original/generated invocation or comparison claimed as passed.

The host project pins Copper68k 1.4.0 and uses the package SDK selected by
`CopperOS.Portable.props`; it has no native/compiler project reference.
`HunkImage.cs` is source-linked from the existing executor. The probe is a
new entry in `Commands.NativeRoot`; select
`CopperOS.Commands.NativeRoot.Workbench31MakeDirProbe.Main` when emitting
its resident HUNK. Coordinate builds with the active compiler/provider owner.

Expected successful execution per CPU is **38 original invocations, 39
generated invocations and 38 comparisons**. The generated-only case fails
the additional four-byte result-slot allocation. One protected image per
side is reused for normal cases, failure/success repeats and three pairs of
instruction-interleaved callers. Actual counts and failure progress are
reported separately; these numbers are expectations until executed.

The fixture checks exact NAME/M and register arguments, original stack versus
generated owned result slots, live aligned pointer vectors and strings, raw
BPTR Lock/CreateDir/UnLock ownership, error selection, DOS/argument allocation
cleanup, returned SP/result and immutable image/input/process
guards. Public vectors clobber D0/D1/A0/A1 and condition codes. Cleanup and
successful VPrintf poison IoErr to verify capture and restoration. Concurrent callers have distinct DOS bases,
process fields, stacks and allocations; heap reads after free or from another
caller are rejected. Workbench operations are excluded and cause failure.

The original NDK `STARTUP.ASM:264-267` permits a command image to modify every
register except SP; see the [entry ABI audit](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/baseline-and-api-audit.md).
Each case requires `entryStackRestored`, the expected D0 result and the resource
and memory assertions. `nonvolatileRegistersRestored` remains an honest
observation of D2-D7/A2-A6 and may be false without rejecting a command image.
This allowance does not weaken the separate public-library vector argument,
A6-base or volatile-clobber checks.

Qualification limits are intentional:

- ReadArgs returns supplied names/errors. This tests its caller ABI and
  lifetime, not parser grammar, quoting, keyword handling or `?` continuation.
- Lock/CreateDir results form an explicit protocol, not a filesystem or host
  directory adapter. No real handler, metadata, traversal or race is proven.
- VPrintf uses a test-only renderer for three exact command-owned formats.
  The receipt retains exact guest format/name bytes, requested/accepted bytes
  and returned count. These bytes are not a full original-DOS stdout capture.
- PrintFault records code/header/result only. No host-generated DOS fault
  message is presented as original output or compared with invented text.
- The CLI-only probe opens DOS36, calls the production body and finishes. Its
  missing-library path sets the known process error 122. Workbench launch,
  booted DOS/Shell, minimum stack, packaging and P approval remain open.

Native execution/source/binary binding is owned by the main qualification
workflow. Source availability, a host project build or these expected counts
alone do not establish any native or original-command result.
