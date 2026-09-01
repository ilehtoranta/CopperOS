# Native RAR4/CAB DOS component fixture

This standalone fixture executes the generated production
`Exe2ArcForwardScanner`, `Exe2ArcPayloadCopy` and `NativeExe2ArcIo` under pinned
Copper68k 1.4.0. It supplies public DOS vectors, inert input bytes, borrowed raw
BPTRs, a borrowed DOS base and guarded writable scratch. It does not execute an
original command, parser, filesystem handler, executable wrapper, archive
decoder or full Exe2Arc entry. Compiler/source/HUNK binding belongs to a separate
private compilation receipt; the executor records raw source and loaded DLL
identities before and after a run.

The two projects have no Commands, Shell, compiler or SDK project references.
The source-linked root imports the pinned public SDK packages. The executor
uses Copper68k 1.4.0 and links the existing `Commands.NativeExecution/HunkImage.cs`
without modifying it. Build only these projects with `BuildProjectReferences=false`;
do not implicitly rebuild shared native compiler/provider inputs.

```powershell
dotnet build tests/Commands.Exe2ArcIoNativeRoot/CopperOS.Commands.Exe2ArcIoNativeRoot.csproj -c Release -p:CopperOSUseLocalCopperSharp=false -p:BuildProjectReferences=false
dotnet build tests/Commands.Exe2ArcIoNativeExecution/CopperOS.Commands.Exe2ArcIoNativeExecution.csproj -c Release -p:BuildProjectReferences=false
```

Compile `CopperOS.Commands.Exe2ArcIoNativeRoot.Exe2ArcIoNativeProbe::Main` from the
root DLL with the bound compiler, `--platform amiga --cpu 68000` (also 68020 and
68040), `--fpu disabled --format hunk --runtime resident --memory none
--exceptions yolo --exports none`. Supply the exact package SDK and SDK.Support
DLLs with `--managed-assembly`; preserve compatibility JSON, map, input hashes,
source/restore/compiler closures and all failed runs. Do not copy reference
media or vendor implementations into these projects or snapshots.

```text
dotnet exec <executor.dll> <generated.hunk> <68000|68020|68040> <new-report.json> exe2arc-rar4-cab-dos-components
```

Use a fresh JSON path. Existing paths, including hardlink/symlink aliases, are
never truncated, even on admission failure. The runner rejects an absent HUNK,
unknown CPU/suite, unsupported HUNK layout or unpinned loaded Copper68k DLL.
Source identity is not silently inferred from a successful build or filename.

The finite corpus is **77 component invocations per CPU**: 69 sequential and
eight caller invocations in four instruction-interleaved pairs. Each CPU run
loads one image. Tests include all six source-window EOF triplets, exact 6/19
byte overlap, crossing/third-window candidates, candidate offset-zero stopping,
CAB predicate failures before a later marker, RAR5 rejection, first-marker
selection, scan seeks/short reads, payload short/negative transfers, selected
payload/trailer bytes, invalid buffers/handles and the bounded signed-seek
domain. Only a successful final seek permits the probe's optional copy stage.

Independent expected DOS scripts require exact D1 raw BPTRs, D2 pointer/seek
argument, D3 length/mode, correct A6, old-position Seek results, immediate IoErr
only after -1, no retries and no missing calls. Successful calls poison ambient
IoErr. D0/D1/A0/A1 and CCR are clobbered at the public-call boundary. Input,
output and library ownership remain with the fixture: there are no allowed
Open/Close/Free/SetIoErr/signal gateways. Output bytes are compared exactly;
receipts also record their hashes, counts, raw observations and the call trace.
Confirmed output counts do not imply that a real -1 Write had no hidden side
effects.

Guest code can read only the current invocation's initialized scratch, input
controls, stack, image and bounded vector prefetch bytes. Only results and
stack accept guest writes. DOS Read alone supplies scratch bytes. Code, vector,
caller-control and guard bytes are checked; other callers' buffers/stacks are
not admitted. Whole-image return requires SP restoration and the expected
result, not preservation of every library nonvolatile register. Final registers
are still recorded. Configured 4 KiB/16 KiB stacks and observed usage do not
establish a minimum stack or P-bit qualification.

All actual memory is below the 68000 physical 24-bit limit. The unsigned-wrap
admission control causes no I/O and is not proof of physical mapping at that
logical address. The fixture uses each public default CPU/interpreter profile
without fallback; no cycle-timing claim follows from these semantic runs.

One exploratory compiler failure is intentionally retained separately:
`DOS.DOSLibraryBase = new APTR(raw)` in fixture setup stored the address of its
temporary instead of the APTR value. The probe uses the established public
`APTR.FromPointer(raw)` boundary. That source change does not repair or qualify
the constructor/property-setter compiler form. Production command startup uses
the value returned from `Exec.OpenLibraryRaw`; these components require an
already-open base and never bootstrap it themselves.
