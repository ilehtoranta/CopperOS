# Exe2Arc header component execution

This executor runs only the generated RAR4/CAB one-candidate predicates. The
root project source-links the unmodified production Exe2ArcHeaderProbe.cs;
its fixture entry supplies a borrowed guest-memory adapter, not a replacement
header algorithm. No original executable, vendor source, archive payload,
scanner, DOS vector, file handler or decompressor is included.

The finite suite has 76 actual invocations per CPU, including twelve callers
scheduled in instruction-interleaved pairs. One loaded image is reused for
all cases. Each invocation has separately guarded input, output-control and
stack storage. Header reads must be byte accesses to the supplied fields;
the guest adapter and bus independently record them. Input writes, foreign
memory, code writes, wide header reads and execution outside the image fail.
Final D0 and SP are asserted; no library-export register convention is imposed
on the whole-image entry. 4 KiB/16 KiB stacks are configured test resources,
not a minimum-stack qualification.

RAR/CAB markers, strict remaining-length thresholds, unsigned little-endian
fields, short/missing/unmapped windows, last-address and one-byte overflow
boundaries are explicit. Success at the physical limit ends at $00FFFFFF
on 68000 and $FFFFFFFF on 68020/040. Logical uint overflow is rejected
before memory access on every CPU. CAB length 1/table 0 intentionally matches
the limited source predicate. Rejecting offset zero does not specify whether
a future scanner continues. Source-to-packed-MorphOS correspondence is open.

Build these projects with the pinned packages and BuildProjectReferences=false;
do not rebuild sibling compiler/SDK projects in another owner's build window.
Native compilation additionally needs a separately frozen compiler/SDK receipt.
The managed root alone is not native execution.

```powershell
dotnet build tests/Commands.Exe2ArcNativeRoot/CopperOS.Commands.Exe2ArcNativeRoot.csproj -c Release -p:CopperOSUseLocalCopperSharp=false -p:BuildProjectReferences=false
dotnet build tests/Commands.Exe2ArcNativeExecution/CopperOS.Commands.Exe2ArcNativeExecution.csproj -c Release -p:BuildProjectReferences=false
dotnet tests/Commands.Exe2ArcNativeExecution/bin/Release/net10.0/CopperOS.Commands.Exe2ArcNativeExecution.dll <generated.hunk> 68000 <new-report.json> exe2arc-header-components
```

Repeat explicitly for each matching 68020 and 68040 artifact. No timing/core
fallback is selected. The receipt records actual returned counts, per-case
results and byte-read offsets, HUNK and loaded-runtime hashes, and raw source
hashes before/after. Those observations do not themselves bind HUNK derivation
or establish reproducible native qualification. Missing/wrong artifacts fail;
failed runs retain partial counts. Reports must use a new .json path: existing
files and aliases are never overwritten, including on failed admission.

The command contract, options, ReadArgs, startup, output, archive scanning and
copying, cleanup, original comparison, packaging and pure/resident policy all
remain separate required gates. Passing this fixture is not command completion.
