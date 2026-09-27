# Compiler qualification for external commands

Recorded 2026-08-30 for CC02/CC04. This is a bounded compiler regression record,
not a shipping command, original-OS differential, or pure/resident approval.
The inspected CopperSharp68k working tree was based on commit
`46ac6600c3c1a5ce7aceb2236ae744d63aa1cd15` and contained other existing edits;
the hashes below identify the actual tested files and rebuilt DLLs.

## Fixed: stale reads of address-exposed 32-bit parameters

The native Eval numeric probe exposed a compiler defect: `Divide(ref high,
ref low, radix)` changed the parameter homes on the caller's stack, but later
loop conditions still read the original register values. Input one therefore
kept writing digits backwards after the quotient became zero. This was a
compiler lowering defect, not a failure of the two-lane division algorithm.

In [CilMachineIrBuilder.cs](D:/Koodit/GIT/CopperSharp68k/Compiler/Backend/CilMachineIrBuilder.cs:1579),
an `ldarg` for an address-exposed, 32-bit scalar now becomes `ArgumentLoad`
from its existing home. It no longer copies the immutable incoming SSA value.
Unexposed parameters keep their previous lowering. This change does not alter
argument registers, frame layout, SDK APIs, or exported calling conventions.

[ArgumentHomeMutationTests.cs](D:/Koodit/GIT/CopperSharp68k/Compiler.Tests/ArgumentHomeMutationTests.cs:44)
contains three independent native scenarios: preserve a pre-mutation snapshot
while observing the replacement, terminate a loop after changing both parameter
homes, and observe a mutation of a stack-passed parameter. Each runs on
MC68000, MC68020, and MC68040, with fixed-point peephole optimization and with
peephole optimization disabled.

| Run | Result |
| --- | --- |
| New regression before the lowering change | 18 failed; 0 passed; 0 skipped |
| New regression after the change | 18 passed; 0 failed; 0 skipped |
| Final nearby suite, including the same 18 cases | 99 passed; 0 failed; 0 skipped |
| Final Release CLI build | Passed; 0 warnings; 0 errors |

The 99 tests include external-call register preservation, UInt64 split/lane
formatting, nested address returns, machine optimizer tests, and ROM section
tests. **The 18 cases are included in 99, not additional to it.** The regression
executor runs generated instructions using Copper68k 1.4.0; it does not boot
Kickstart or CopperStart, run the complete Eval command, or qualify its purity.

Reproduction commands, from `D:/Koodit/GIT/CopperSharp68k`:

```powershell
dotnet test Compiler.Tests/CopperSharp.Compiler.Tests.csproj -c Release --no-restore -p:SkipCopperScreenHeadlessProjectReference=true --filter FullyQualifiedName~ArgumentHomeMutationTests --logger "console;verbosity=quiet" -v quiet

dotnet test Compiler.Tests/CopperSharp.Compiler.Tests.csproj -c Release --no-restore -p:SkipCopperScreenHeadlessProjectReference=true --filter "FullyQualifiedName~ArgumentHomeMutationTests|FullyQualifiedName~AmigaExternalCallClobberTests|FullyQualifiedName~UInt64LaneFormatterExecutesOnEveryCpu|FullyQualifiedName~SplitInt64IntrinsicPreservesBothRegisterLanes|FullyQualifiedName~NestedAddressReturnChainTests|FullyQualifiedName~M68kMachineOptimizerTests|FullyQualifiedName~RomAssemblySectionTests" --logger "console;verbosity=quiet" -v quiet

dotnet build Compiler.Cli/CopperSharp.Compiler.Cli.csproj -c Release --no-restore -v quiet
```

The first command produced the before/after results; the final two commands
produced the 99-test result and DLLs below. Reproducing the historical failure
requires an isolated copy without the new `ArgumentLoad` branch; do not revert
the shared working tree or its unrelated changes.

Paths below are relative to `D:/Koodit/GIT/CopperSharp68k`. DLL paths are under
`Compiler.Cli/bin/Release/net10.0/` unless otherwise stated.

| File | SHA-256 |
| --- | --- |
| `Compiler/Backend/CilMachineIrBuilder.cs` | `ed2e931697b933af53d3adab63a6d5f14ef7790e7b1516e97b2cbeaa87014d72` |
| `Compiler.Tests/ArgumentHomeMutationTests.cs` | `fbdd5127fa8a763a6055352acc1d714b01a9901933c3b62139deb7eda4ead78a` |
| `CopperSharp.Compiler.Cli.dll` | `e58a6083f914c6b1c869d3946199aea5ac12967b2e166673a790b9119c28e12a` |
| `CopperSharp.Compiler.dll` | `04760baef99aba282d7703c6da363c913ca733f36af8623e2dc7f7fb47286f95` |
| `CopperSharp.Targets.Amiga.dll` | `cd094de9c91346d733411c994c2d8d9ce72e71882e878e2fbe39e9817b6c7533` |

`Compiler/bin/Release/net10.0/CopperSharp.Compiler.dll` had the same backend
hash. These are slice receipts, not a complete immutable build-input manifest;
the separate native qualifier must bind its own complete source/binary inputs.

Scope is explicitly limited to machine value width `Long` (32 bits).
The new execution fixtures exercise `uint` homes. Narrow byte/word and wide
64-bit address-exposed parameter homes remain **unqualified and unfixed by this
slice**. In particular, existing home initialization writes a LONG for a
four-byte home, so extending this branch to narrower machine values requires
a separate layout/normalization audit and native tests. Public `long`/`ulong`
transport through split/combine intrinsics is not proof of mutable wide homes.

## Cross-repository numeric follow-up: exploratory native pass

After rebuilding with the fixed compiler, the parent independently reran the
unchanged production formatter and probe in CopperOS. All **540 actual native
invocations passed: 180 per CPU**. Each CPU used one shared image; output,
control-block and stack guards, separate invocation ownership and instruction
interleaving passed, with zero host gateways and zero shared-image writes.
MC68000 successful boundary writes use the 24-bit physical bus limit; the
MC68020/040 fixture uses synthetic 32-bit boundary regions. Logical 32-bit APTR
wrap rejection remains tested on all three CPUs.

These are exploratory `corrected-compiler-trial` receipts, inspected read-only
after the parent's run. They are **not** a complete Eval command, original-OS
comparison, minimum-stack qualification, shipping/purity approval, or the final
snapshot-bound reproducible qualification run. The guarded workflow must
reproduce and bind them separately.

Receipt files are under
`tests/Commands.EvalNativeRoot/bin/Release/net10.0/corrected-compiler-trial/`
in CopperOS.

| CPU | HUNK bytes | HUNK SHA-256 | Execution JSON SHA-256 |
| --- | --- | --- | --- |
| MC68000 | 2132 | `c31d58911003cb536ef0e2f29edfc4809b733ab005577de6ab0dcd6de5f81315` | `1cc18aea88006e00239dce2f7d307b07b0ce37eece6a7456f69e5e155c4b49c7` |
| MC68020 | 2012 | `87df508b4a0c87bcbe06cf1eb530425273d4a8d8779acd4e362bb5f04f9f3b45` | `8330f3153acde1643e4bc18d032237a4462d1aaae8cbb4f791745b62f4a09873` |
| MC68040 | 1992 | `3dfef21e84014fb3ef17ac71489bb9e5e561abbbe643a9738331ac440fa3e875` | `5a4c63fe3361996b0f7b67db7e7ac421663ec7a77a726768f78c1c595d22349d` |

The JSON filenames are `execution-68000.json`, `execution-68020.json` and
`execution-68040.json`. All three explicitly record `fullEvalCommand: false`
and `shippingOrPureApproval: false`.

## CC02.API04: interim command-entry policy

**Shipping command entries must not require heap-backed resident context.**
Until an explicit command-only bootstrap failure policy is implemented and
qualified, admit only entries whose context is absent or stack-backed, with
the actual context and stack requirements recorded. Do not increase the
threshold merely to bypass this gate.

The compiler's default resident stack-context threshold is 512 bytes
([PublicApi.cs](D:/Koodit/GIT/CopperSharp68k/Compiler/PublicApi.cs:556)). Context size
comes from materialized writable library-base slots, four bytes per slot; it
is not the size of ordinary C# locals
([M68kCodeGenerator.cs](D:/Koodit/GIT/CopperSharp68k/Compiler/Backend/M68kCodeGenerator.cs:74)).
Above the threshold, entry/export adapters call Exec AllocMem before entering
the managed body. Allocation failure reaches the common zero-result path
([ManagedPoolRuntime.cs](D:/Koodit/GIT/CopperSharp68k/Compiler/Backend/M68kCodeGenerator.ManagedPoolRuntime.cs:241)).

**Preserve generic zero/null failure behavior.** Changing the shared fallback
to `RETURN_FAIL` (20) would change generic scalar, boolean, pointer and wide
return contracts; the helper is also used by export adapters. A startup-shaped
method signature or Resident profile does not identify a DOS command.

A future command-only policy must cover CLI error reporting, Workbench startup
message ownership/reply, stack preservation and allocation cleanup before the
managed body can run. Merely returning 20 would leave Workbench startup handling
unperformed. The existing forced-heap tests at
[CompilerExecutionTests.cs](D:/Koodit/GIT/CopperSharp68k/Compiler.Tests/CompilerExecutionTests.cs:3299)
exercise allocation and zero fallback; they do not establish that command policy.

## Separate source-only observation: export A6 restoration

**Unexecuted, unfixed source-audit finding; no regression or fix is claimed.**
The inspected heap-backed export success path restores caller registers,
including A6, before calling `EmitDestroyResidentInvocationContext`
([M68kCodeGenerator.cs](D:/Koodit/GIT/CopperSharp68k/Compiler/Backend/M68kCodeGenerator.cs:10111)).
The destroy helper then loads ExecBase into A6 to call FreeMem
([ManagedPoolRuntime.cs](D:/Koodit/GIT/CopperSharp68k/Compiler/Backend/M68kCodeGenerator.ManagedPoolRuntime.cs:274)).
Source inspection therefore suggests that the caller's A6 can be lost on this
path. The existing export allocation test checks D2/A2 preservation, not A6.

Keep this as a separate compiler ABI investigation: force a heap context,
seed an A6 sentinel, exercise success and failure on each CPU, and verify the
full export convention before changing restore ordering. Neither this issue nor
the API04 bootstrap policy was changed by the argument-home fix.

## Fixed: value-type construction incorrectly required a managed heap

The first CC04 native I/O build failed with `C68K0010` at
`NativeCommandIo::ReadOnce IL_0019`: assigning a new readonly
`NativeCommandIoError` through an `out` parameter was classified as a managed
allocation. This eight-byte record contains only a capture tag and a raw DOS
error value. The production source was valid for the compiler's existing
reference-free aggregate lowering; enabling a heap was not appropriate.

[M68kStaticAnalyzer.cs](D:/Koodit/GIT/CopperSharp68k/Compiler/M68kStaticAnalyzer.cs:199)
now excludes resolved value-type constructors, as well as transparent scalar
constructors, from the `newobj` heap gate. Constructor bodies are still visited,
so actual class and array allocations inside them still require a heap.
[CilMachineIrBuilder.cs](D:/Koodit/GIT/CopperSharp68k/Compiler/Backend/CilMachineIrBuilder.cs:4287)
already lowers this construction into a frame temporary and checks that the
result has a supported reference-free layout. Framework allocation analysis
already excludes value-type construction. **This validator slice changed no
backend lowering, metadata identity, SDK, or command I/O source.** The readonly
record, capture tag, immediate `IoErr` observation, and no-heap policy remain.

[ValueTypeConstructionNoHeapTests.cs](D:/Koodit/GIT/CopperSharp68k/Compiler.Tests/ValueTypeConstructionNoHeapTests.cs:46)
first confirms that the fixture contains a non-transparent value-type `newobj`,
then executes its HUNK on MC68000, MC68020 and MC68040 with fixed-point/disabled
peephole optimization and freestanding/resident profiles: **12 native cases**.
They distinguish uncaptured default from captured zero, preserve `-101` and
`205`, clear a reused output record, restore SP, and leave the loaded image
unchanged. Four additional test cases reject direct class/array allocation and
allocation inside a value-type constructor; each checks both runtime profiles.
These four are compile-time rejection tests, not native command invocations.

| Run | Result |
| --- | --- |
| Final new fixture before the validator change | 14 failed; 2 passed; 0 skipped |
| Same fixture after the change | 16 passed; 0 failed; 0 skipped |
| Nearby suite, including those 16 cases | 85 passed; 0 failed; 0 skipped |
| Release CLI rebuild | Passed; 0 warnings; 0 errors |

Before the fix, all 12 positive cases failed compilation. The two nested
allocation tests also failed because the diagnostic incorrectly identified the
outer struct construction instead of the real allocation in its constructor.
The direct class/array rejection tests already passed. **The 16 cases are
included in 85, not additional to it.** The 85 also include the earlier
argument-home and external-call ABI regressions, existing aggregate/constructor
tests, allocation inventory, and ROM heap rejection. They are a mixed compiler
test suite, not 85 shipping-command executions.

The [validator receipt](D:/Koodit/GIT/CopperOS/obj/compiler-valuetype-construction/e44a64da019042e2abacbf78f9dc9b41/receipt.json)
has SHA-256 `173e70b9da60aa75e26c2924efe2ece028536d6e25e5bdf65c78387d152bc456`.
Its directory preserves `before-inputs.json`, `before-result.json`, before/after
validator source copies, the unchanged test source, `before.trx`, `after.trx`,
`nearby.trx`, logs, and the exact nearby filter. The receipt binds their test
results and consumed compiler/test/emulator binaries, with unchanged relevant
inputs checked across the change. It is not a replacement for the native
qualifier's complete source and dependency manifest.

Paths in the following table are relative to `D:/Koodit/GIT/CopperSharp68k`.
These are historical validator-slice identities; the subsequent folding fix
below produces a newer backend DLL.

| File/state | SHA-256 |
| --- | --- |
| `Compiler/M68kStaticAnalyzer.cs`, before | `0925e7441c6cf132a3b20ebcf94cadf367da11036bcd408896b1d3b55e352caa` |
| `Compiler/M68kStaticAnalyzer.cs`, after | `5ca8b93a7d1f33984199a06015384dbe9e96f690eb763700197bc1206c42bf4c` |
| `Compiler.Tests/ValueTypeConstructionNoHeapTests.cs` | `cd68cdebceda1303433a2de8056afc3ddf402fa3e97805da9566373059ca29d6` |
| `Compiler.Tests/bin/Release/net10.0/CopperSharp.Compiler.dll`, before | `715044c95185ae8689004067dd15122bed3840a08dde9c03a7a0dbc3e1694289` |
| `Compiler.Cli/bin/Release/net10.0/CopperSharp.Compiler.dll`, after | `dc3c9b3c42313ef96019ddf6d647768ce52f0ce3b88a63287046568750b5b683` |
| `Compiler.Tests/bin/Release/net10.0/CopperSharp.Compiler.Tests.dll`, before and after | `20d7a42edf3e654d64b389a3609a8f1de8404bf20d43f664a2f6a319e8cf6d1c` |

Recorded commands, from `D:/Koodit/GIT/CopperSharp68k`, used the receipt directory
as `$valueTypeAuditDirectory`. For another run, select a fresh result directory
and preserve these receipts. Reproducing the old failure requires an isolated
checkout with the recorded pre-fix validator, not reverting the shared tree.

```powershell
dotnet test Compiler.Tests/CopperSharp.Compiler.Tests.csproj -c Release --no-restore -p:SkipCopperScreenHeadlessProjectReference=true --filter 'FullyQualifiedName~ValueTypeConstructionNoHeapTests' --logger 'trx;LogFileName=before.trx' --results-directory $valueTypeAuditDirectory
# After the validator correction, the same command used LogFileName=after.trx.

$valueTypeAuditFilter = 'FullyQualifiedName~ValueTypeConstructionNoHeapTests|FullyQualifiedName~AmigaExternalCallClobberTests|FullyQualifiedName~ArgumentHomeMutationTests|FullyQualifiedName~OutRefStructExecutionRegressionTests|FullyQualifiedName~TrivialValueTypeConstructorInliningTests|FullyQualifiedName~ValueTypeConstructorsCanBeStoredThroughOutParameters|FullyQualifiedName~RomRuntimeProfileRejectsManagedAllocationByDefault|FullyQualifiedName~AnalysisInventoriesReachableManagedAllocationInstructions|FullyQualifiedName~CompilesObjectConstructionWithArguments|FullyQualifiedName~NestedExternalValueTypeTokenPreservesAggregateOutStore'
dotnet test Compiler.Tests/CopperSharp.Compiler.Tests.csproj -c Release --no-build --no-restore -p:SkipCopperScreenHeadlessProjectReference=true --filter $valueTypeAuditFilter --logger 'trx;LogFileName=nearby.trx' --results-directory $valueTypeAuditDirectory
dotnet build Compiler.Cli/CopperSharp.Compiler.Cli.csproj -c Release --no-restore
```

## Fixed: identical-body folding merged different external calls

After the validator repair, Foundation run
`e0dde03caff74ef78496d9f4c9ddd210` compiled the I/O probe but failed its first
write case on all three CPUs: the generated `WriteOnce` called DOS `Read`.
The MC68000 map placed both helpers at `000002BC`, size 70. This run remains
**failed**; successful compilation and the other foundation suites did not
qualify the I/O helper.

`HaveSameInstructionOperand` compared resolved calls using their managed method
identities. Cross-module platform declarations and unregistered external imports
can have synthetic definitions with empty module/handle identities
([CompilationModule.cs](D:/Koodit/GIT/CopperSharp68k/Compiler/Metadata/CompilationModule.cs:4773)).
Different DOS vectors therefore compared equal. The bounded correction in
[M68kCodeGenerator.cs](D:/Koodit/GIT/CopperSharp68k/Compiler/Backend/M68kCodeGenerator.cs:1158)
handles imported/platform targets before managed alias comparison: their
declaration tokens must match. The surrounding proof already requires matching
caller module and generic construction. This conservatively preserves exact
declaration identity without disabling folding globally or changing metadata
identities. Calls through genuinely identical declarations and ordinary managed
helper folding remain supported.

[ExternalCallBodyFoldingTests.cs](D:/Koodit/GIT/CopperSharp68k/Compiler.Tests/ExternalCallBodyFoldingTests.cs:46)
executes actual generated instructions for two ABI families: DOS `Read`/`Write`
with their distinct LVOs and a pair of named cross-module imports. Each family
runs on MC68000/020/040, fixed-point/disabled peephole optimization, and
freestanding/resident profiles: **24 native cases**. They check the exact
callee sequence and register arguments, result and SP, distinct addresses for
different callees, and shared addresses for wrappers using the same declaration.
The import declarations live in
[ExternalFoldImports.cs](D:/Koodit/GIT/CopperSharp68k/Compiler.Tests.MultiModule/ExternalFoldImports.cs).
No SDK, command body, I/O helper, or native fixture expectations were changed.

| Run | Result |
| --- | --- |
| Final new fixture before folding correction | 24 failed; 0 passed; 0 skipped |
| Same fixture after correction | 24 passed; 0 failed; 0 skipped |
| Nearby suite, including those 24 and the preceding 85 cases | 134 passed; 0 failed; 0 skipped |
| Release CLI rebuild | Passed; 0 warnings; 0 errors |

The 24 failures were native wrong-callee observations: 12 expected `Write` but
entered `Read`, and 12 expected the second named import but entered the first.
An earlier test setup registered the import dependency as managed code and
passed its 12 import cases while the DOS cases failed. That exploratory receipt
is preserved as `initial-managed-import-*`; the final fixture deliberately
uses adjacent external metadata without managed registration to exercise the
synthetic-import path. **The 24 and 85 are included in 134, not additional to
it.** Existing direct, helper-cascade, generic, address-taken, constructor,
exception-identity and import ABI checks remain in the combined suite.

The [folding receipt](D:/Koodit/GIT/CopperOS/obj/compiler-external-call-folding/c784d81c596141eb89709eea9c43811d/receipt.json)
has SHA-256 `1a1e21e6af2d324193ce727367c3fea6ec9175efdcdb2e58781fb3a495ec71dd`.
Its directory preserves source snapshots, input hashes, before/after and nearby
TRX/logs, the exact filter, and the CLI build log. The unchanged metadata and
validator inputs were rechecked; source and consumed binary hashes are in the
receipt. No commit was made and unrelated sibling edits were preserved.

| File, relative to `D:/Koodit/GIT/CopperSharp68k` | SHA-256 after folding correction |
| --- | --- |
| `Compiler/Backend/M68kCodeGenerator.cs` | `c4e3be6471da0376b2b783b5d49cc90d16777ff3983027d3a51fc4494cf95557` |
| `Compiler.Tests/ExternalCallBodyFoldingTests.cs` | `3346f178ff7ed332280c979e3e338b3a519a893e69a3d466b691ed29a34a1677` |
| `Compiler.Tests.MultiModule/ExternalFoldImports.cs` | `e85c1fb402ed13bfaddccc34dd9310bd1d33b8e5e9421ec2440cbc7af30e4a94` |
| `Compiler.Cli/bin/Release/net10.0/CopperSharp.Compiler.dll` | `2083e5497e70c9c7162beb84b85e56aa19229a3e3306b5b708eecbc505fe434a` |
| `Compiler.Cli/bin/Release/net10.0/CopperSharp.Compiler.Cli.dll` | `e58a6083f914c6b1c869d3946199aea5ac12967b2e166673a790b9119c28e12a` |
| `Compiler.Cli/bin/Release/net10.0/CopperSharp.Targets.Amiga.dll` | `cd094de9c91346d733411c994c2d8d9ce72e71882e878e2fbe39e9817b6c7533` |
| `Compiler.Tests/bin/Release/net10.0/Copper68k.dll` | `8046d9a2083c198647a02d9146b735629890d9a2e14bc313968a3b48139a2cd5` |

The backend DLL hash also matches `Compiler/bin` and `Compiler.Tests/bin`.
Recorded commands used the folding receipt directory as `$foldAuditDirectory`:

```powershell
dotnet test Compiler.Tests/CopperSharp.Compiler.Tests.csproj -c Release --no-restore -p:SkipCopperScreenHeadlessProjectReference=true --filter 'FullyQualifiedName~ExternalCallBodyFoldingTests' --logger 'trx;LogFileName=before.trx' --results-directory $foldAuditDirectory
# After the folding correction, the same command used LogFileName=after.trx.

$foldAuditFilter = $valueTypeAuditFilter + '|FullyQualifiedName~ExternalCallBodyFoldingTests|FullyQualifiedName~ExactDirectBodiesFoldButAddressTakenMethodsRemainDistinct|FullyQualifiedName~ExactConstructedGenericBodiesWithSameContextFold|FullyQualifiedName~ExactBodiesCallingFoldedHelpersAlsoFold|FullyQualifiedName~FullExceptionIdenticalBodiesKeepDistinctUnwindIdentity|FullyQualifiedName~PolymorphismSampleExecutesClassAndInterfaceDispatch|FullyQualifiedName~ResolvesAbsoluteImports|FullyQualifiedName~MapsRegisterAbiImports'
dotnet test Compiler.Tests/CopperSharp.Compiler.Tests.csproj -c Release --no-build --no-restore -p:SkipCopperScreenHeadlessProjectReference=true --filter $foldAuditFilter --logger 'trx;LogFileName=nearby.trx' --results-directory $foldAuditDirectory
dotnet build Compiler.Cli/CopperSharp.Compiler.Cli.csproj -c Release --no-restore
```

Both records qualify bounded compiler behavior. They do not establish a real
DOS parser or handler run, original-command output semantics, complete I/O
foundation qualification, minimum stack, or shipping/pure approval. The native
qualifier must rerun and bind the final compiler and unchanged production inputs
separately; previous failed runs must remain failed.

## Fixed: narrow instance-field storage disagreed with field-address access

CC02.API15 was observed in the frozen generated DOS image
`9a9a69046c0595e59e90f087a2f8be948f4ee4a49d8a1b84555a3c7bde41b403`.
ReadArgsEntry has four uint fields, byte-backed flags at offset 16, and an
index at offset 20. The native template reader writes the flags with MOVE.B,
but AllocateReadArgsValue loads MOVE.L at offset 16 before AND#4. The
big-endian longword therefore loses the low modifier bit. The disassembly
audit is private at
[readargs-template-native-audit-9a9a6904.txt](D:/TestData/CopperOSCommands/ReadArgsDifferential/readargs-template-native-audit-9a9a6904.txt),
SHA256 `10916743449fa981db3329d177f2614b7a39ff7b8fbb3db3de4593ae52a5c3da`.

The repair changes only instance-field emission in
[M68kCodeGenerator.cs](D:/Koodit/GIT/CopperSharp68k/Compiler/Backend/M68kCodeGenerator.cs)
and [M68kCodeGenerator.Allocated.cs](D:/Koodit/GIT/CopperSharp68k/Compiler/Backend/M68kCodeGenerator.Allocated.cs).
Loads/stores use the resolved byte/word width, with correct signed or unsigned
promotion. Direct/fallback stack operations follow the existing narrow value
transport. Narrow zero stores cannot select the CLR.L shortcut. Explicit
MemorySize=4 aggregate lanes and long pairs retain their existing widths.
Field offsets, aggregate sizes, IR/metadata, the DOS enum and parser source
are unchanged. Static-field behavior is outside this correction.

[NarrowInstanceFieldStorageTests.cs](D:/Koodit/GIT/CopperSharp68k/Compiler.Tests/NarrowInstanceFieldStorageTests.cs)
executes ref writes followed by by-value reads, direct writes followed by ref
reads, and simple getters. All signed/unsigned 8/16-bit enum and primitive
fields are checked, along with surrounding guards and returned SP/image
bytes. The same 36-case source failed before and passed after the repair.
[NarrowInstanceFieldBoundaryTests.cs](D:/Koodit/GIT/CopperSharp68k/Compiler.Tests/NarrowInstanceFieldBoundaryTests.cs)
adds 12 native cases for canonical bool, high-bit char/enums, and zero stores
with ten deliberately nonzero padding bytes in an explicit 24-byte structure.
It rejects even transient writes to its single loaded code image. Both matrices
use 68000/020/040, fixed-point/disabled peephole modes and freestanding/resident
profiles, with no heap or external runtime helpers. Forced fallback emission
is source-reviewed, not separately execution-qualified by these matrices.

| Check | Result |
| --- | --- |
| First native matrix before correction | 36 failed; 0 passed; 0 skipped |
| Same source after correction | 36 passed; 0 failed; 0 skipped |
| Related suite, including those 36 and the 12 additional native cases | 389 passed; 0 failed; 0 skipped |
| Release compiler CLI build | Passed; 0 warnings; 0 errors |

The [repair receipt](D:/Koodit/GIT/CopperOS/obj/compiler-narrow-instance-fields/e3103abdb499412abd4ff7bf4bfa18a8/receipt.json)
has SHA256 `4e8e7749cc1b86ca28dc10eef4b6105b2284b14dca06e77642c2e1f3253b2658`.
It preserves before/after source and binary snapshots, the exact related-test
filter, TRX/logs and CLI build. A setup compile failure and a failed related
run caused by a new bridge fixture's global export remain recorded. The bridge
fixture was isolated without changing existing tests or compiler options, and
the unchanged related filter then passed. The existing test-project CS8601
warning at AhiBindingTests.cs:207 remains; it did not occur in the CLI build.

| Checkpoint identity | SHA256 |
| --- | --- |
| Main emitter source | `573923061278c21d7982d191f48fedc89f1b8078eff53014fa47810b9c59ff37` |
| Allocated emitter source | `f9a8374182738b1fe669f47852d9d99be4ae14031d13bfad2d9eeacc08829167` |
| Unchanged 36-case source | `25bc31f543c947a4dccf26c0b8c5f2f6e80fad6bad9db604cf0c693a35ce179a` |
| New 12-case boundary source | `1e93117abad8698074d37ae86f809ab45108656c890100ef61551f3abd9fba10` |
| Compiler DLL in compiler, test and CLI outputs | `142298b971344f9e4516f31a484d25381774c1dc76bdf822234b83520f89c725` |
| Related 389-case TRX | `6efe14bfab0637271b10f1feba10a6fa568401100e83539ec4a951b85dca8024` |

These are checkpoint identities; subsequent bridge changes require their own
receipts. A fresh private DOS build derives from the preceding 761-file
snapshot by replacing precisely the two tested emitter files; the remaining
759 source/settings files stay identical. The unchanged DOS build pipeline,
manifest revalidation and actual parser rerun remain independent gates. No
parser compatibility, command launch or shipping/pure approval follows from
the compiler tests alone. The ensuing private DOS run completed both
44-case captures and native cleanup but failed exact semantic parity in 43
pairs. A later one-file parser correction now passes all 44 exact pairs in a
new receipt; see [parser qualification](readargs-parser-qualification.md).
The compiler repair and parser correction retain separate source bindings.

## Fixed: normal-return command stack bridge

CC02.API14.4 cannot use an ordinary C# call sequence around Exec StackSwap.
The old frame is still addressed relative to SP after the stack changes,
and a whole command image may overwrite every register except SP. Twelve
native baseline configurations reproduce an invalid read at `$00091008`
after the first stack swap, before entering the command. Those failed
baseline results remain recorded separately from the later negative controls.

[DosRunCommandCallbacks.ExecuteOnStack](D:/Koodit/GIT/CopperSharp68k/Sdk.Amiga/DOS/DosRunCommandCallbacks.cs)
adds a bounded import with four native-word operands: descriptor A0, entry A3,
length D0 and argument pointer A1. The original Execute API is unchanged.
The [native emission](D:/Koodit/GIT/CopperSharp68k/Compiler/Backend/M68kCodeGenerator.AmigaRunCommand.cs)
saves caller registers on the old stack, places one recovery word on the
new stack, and calls public Exec StackSwap at -732 using SysBase from address 4.
It supplies the original D0/A0 command entry, recovers the descriptor through
SP after RTS, saves the result across the second StackSwap, and restores the
old stack and registers. No old-frame access occurs while the new stack is
active. No mutable image state, heap, external helper, IR, global command ABI
or target-resolver change is introduced. Main/allocated hooks retain the
preceding narrow-field repair; forced fallback remains source-reviewed only.

| Check | Result and scope |
| --- | --- |
| Before native ordinary-call baseline | 12 failed, 0 passed, 0 skipped; all fail after the first swap and before command execution. |
| Successful bridge configurations | 12 passed: 68000/020/040, fixed-point/disabled peephole, freestanding/resident. Each interleaves two invocations and repeats the first: 36 successful native calls. |
| Retained old-path negative controls | 12 passed by detecting the same invalid old-frame read; these are not successful command calls. |
| Malformed import guards | 5 passed: arity, argument register, result register, 64-bit length and absent ABI metadata. |
| Focused suite / inclusive related suite | 29 / 418 passed, 0 failed, 0 skipped; the latter includes the former and the narrow-field regressions. |
| Release compiler CLI | Passed, 0 warnings, 0 errors. |

The test's native command writes D1-D7/A0-A6 and returns an arbitrary D0.
The Exec StackSwap fixture clobbers D0/D1/A0/A1. The tests check returned
SP/D0, caller D2-D7/A2-A6, descriptor/task bounds, argument delivery, guarded
4K/16K stack allocations and one protected shared image. Reuse does not
reseed the descriptor or task bounds. This qualifies neither a minimum stack
nor original Exec instructions, Exit/nonlocal unwind, command-input buffering,
CopperStart callback integration or pure/resident packaging.

The [bridge receipt](D:/Koodit/GIT/CopperOS/obj/compiler-run-command-bridge/65fa8592a4744984b2e973f5d322b0e9/receipt.json)
has SHA256 `dc51a073634ab8b391c7eed3b0c369f30807be62e65e9ec3407a084dac241b94`.
It records exact source/binary snapshots, commands, filters, failed setup
and native baseline, and passing results. Only five files belong to this
repair: the new emitter partial, main/allocated hooks, SDK declaration and
[native tests](D:/Koodit/GIT/CopperSharp68k/Compiler.Tests/RunCommandStackBridgeTests.cs).

| Checkpoint identity | SHA256 |
| --- | --- |
| Bridge emitter source | `ffdab7f0f589b11aee5018c83fa753075cb197cce29bf8cac725c1daa38cb754` |
| Main emitter source | `90266f3b1691dbb3c9887f8d0e4958ff040911e4b0fdecc85e89cec1a2c37099` |
| Allocated emitter source | `7073533a5db90e4c8125d89590ea45c07679cf3300b877fd0f797a517b8e4d98` |
| SDK declaration source | `df201d07e698d41eeb363800b4a6ec3c65f56cff016c815c9bfec657ac9f8dcf` |
| Compiler DLL in compiler, test and CLI outputs | `814b2adeb0b2bad5d0fac673c97af1e2a9aed9557f12c6dfac9fbfdce5a4d9e7` |
| SDK DLL | `805ceb33d35882aa151a46cf720d715776456da5679ed33529a90713cce6128e` |
| CLI DLL | `2f13f1385e26730f933c7275f8fa3bf582cf3a2bb495daf3bf640f3b1f1686f9` |
| Inclusive 418-case TRX | `07e06b8195aec1d5810109bd1202f47d830b81fbfa86977692db1f61454e8f06` |

The earlier fifteen command/probe artifacts were qualified against older
compiler inputs. Their receipts remain historical evidence, not qualification
of artifacts built with this bridge checkpoint. The subsequent
[command fixture refresh](command-fixture-requalification.md) qualifies new
artifacts built from captured sources containing these repairs. Production
callback integration is recorded separately below; full DOS launch remains open.

## Production DOS callback integration

The actual RunCommand branch in
[DosNativeEntrypoints.cs](D:/Koodit/GIT/CopperStart/src/CopperStart.Dos/DosNativeEntrypoints.cs)
now calls ExecuteOnStack instead of the ordinary StackSwap/Execute/StackSwap
sequence. No other callback branch, dispatcher or input/lifecycle owner was
changed by this patch. The
[production callback tests](D:/Koodit/GIT/CopperStart/tests/CopperStart.Exec.Tests/DosNativeRunCommandCallbackTests.cs)
compile that exact method and observe its own entry/return ABI boundary.

| Check | Result |
| --- | --- |
| Unchanged final fixture before production edit | Six normal-return cases fail after the first StackSwap, before command entry; six non-start controls pass. |
| Same fixture after production edit | 12 pass: six CPU/peephole configurations make 18 successful command invocations, plus 12 non-started callback invocations across six control cases. |
| Related suite from normal CopperStart output path | 39 pass, zero failures/skips; includes the 12 above. |
| Same related suite from retained receipt folder | 35 pass, four source-inspection tests fail because their repository-root lookup starts outside CopperStart. Retained as a separate path/setup failure. |

The command may overwrite D1-D7/A0-A6 and return any tested D0. Tests verify
D0/A0 argument delivery, outer D2-D7/A2-A6/SP preservation at the actual callback
boundary, publication of the result, restored descriptor/task bounds, 4K/16K
stacks and instruction-interleaved/repeated use of one protected image. Thirty
callbacks complete; 18 enter commands and perform 36 total StackSwap calls.
Null entry/descriptor controls perform neither command instructions nor swaps.
Native compatibility reports have no managed allocations, fatal sites,
exception regions, runtime helpers/features or external native targets.

The [integration receipt](D:/Koodit/GIT/CopperOS/obj/dos-native-runcommand-callback/actual-owner-1788101912807/receipt.json)
has SHA256 `1cc7928b905d5f6228b24abfd97bee43cef93fc163c59f60273fd32a06b81ead`.
Production source is
`e6614de85bd9fa5a24d4c67f36f0a1f4f712e4ab602554138b17ee6a4f22c73f`;
the unchanged final test source is
`d742e8b05ebd5bc417c5c10d7b7abdbe12bb0a74c0152a6a23ffe6a851b0b6e7`.
Compiler/SDK identities remain `814b2ade...` / `805ceb33...` from the bridge
checkpoint above. Initial freestanding-image workspace and whole-startup
register-assertion failures are retained as harness setup history, not evidence
of the old callback defect. The final fixture uses a Resident wrapper with
invocation-owned library workspace; production DOS's runtime profile is unchanged.

This closes the normal-return callback defect under the supplied Exec vector.
It does not execute original Exec StackSwap, implement normal input/GetArgStr,
repair rejected callback rollback, or qualify reset/retirement/Exit, minimum
stack, pure/resident command behavior or package admission.
