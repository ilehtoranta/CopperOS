# CC04 native ReadArgs ownership slice

Recorded 2026-08-30. Implementation:
`src/Commands/Native/NativeCommandArguments.cs`.

This slice supplies invocation-owned result storage through the real DOS vector
interface. It does not replace `ReadArgs`, qualify all templates, or establish
original Kickstart/MorphOS parser equivalence by itself. The native qualification
below executes the helper's generated instructions with supplied vector results;
those results are not an original or substitute DOS parser.

## Public helper API

```csharp
public struct NativeCommandArguments
{
    public static bool TryRead(CString template, uint resultCount,
        out NativeCommandArguments arguments);

    public bool IsSuccess { get; }
    public uint ResultCount { get; }
    public int ReturnLevel { get; }
    public int IoError { get; }

    public bool TryGetResult(uint index, out uint value);
    public void Release();
}
```

Open `dos.library` through the native startup owner before using `TryRead`.
Compile the command with the existing resident runtime profile so its normal
Exec/DOS library-base calls remain invocation-local. Release the arguments
before `NativeCommandStartup.Finish` closes DOS.

`resultCount` is the frozen template's number of option entries, including
switches and anonymous positional entries. The command supplies it alongside
the exact template; the helper does not implement a second template parser.
An incorrect count can violate DOS's array contract, so each command must test
its declared count against its authoritative template. An empty template may
use zero entries; the helper allocates one unused LONG to supply a valid array
address but exposes no result index.

`IsSuccess` means a successful parse still owns live results; it becomes false
after release. `ReturnLevel` and `IoError` describe the parse attempt, not a
later file operation or the command's ultimate result. On successful parsing,
they are `RETURN_OK` and zero. The helper does not reset the process's ambient
IoErr simply because parsing succeeded.

## Ownership and failure behavior

| State or action | Owned resources and result |
| --- | --- |
| Null template or count exceeding safe ULONG-size/Exec-rounding bounds | No allocation or parser call; `RETURN_ERROR`, `BadTemplate`, and matching DOS IoErr. |
| Result allocation failure | No parser call; `RETURN_FAIL`, `NoFreeStore`, and matching DOS IoErr. |
| Provider returns an unaligned or wrapping result span | Return the exact acquired allocation through FreeMem before reporting `RETURN_FAIL`/`NoFreeStore`; never hand the invalid span to ReadArgs. |
| ReadArgs returns NULL | Capture DOS IoErr immediately; free the result allocation; restore that error; return `RETURN_ERROR`. No successful RDArgs lease is published. |
| ReadArgs succeeds | The lease owns the Exec result array and DOS's returned RDArgs. It keeps both until explicit Release. |
| Release | Preserve the current DOS IoErr, invalidate the lease, call FreeArgs before FreeMem, then restore the saved error. |
| Release repeated on the same instance, or on a default/failed lease | No resource/vector operation. |
| Out-of-range index, empty result set, or released lease | TryGetResult returns false and zero without reading the result array. |

The array is allocated with `Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear`.
`Exec.AllocMem` supplies the original longword alignment guarantee; the helper
also checks it before DOS can perform a LONG store. Allocation multiplication
is bounded with `ExecConstants.MemoryBlockMask` so Exec's chunk rounding cannot
wrap the request. The exact original pointer and requested byte count are used
for FreeMem. Successful index reads compute a validated unsigned guest address
instead of casting a potentially large byte offset to a signed `int`.

This value-type lease has **one owner**. Do not copy a live lease and release
both copies; C# value copying cannot enforce unique ownership. Pass it by `ref`
when sharing access, release the old lease before replacing it with another
`out` result, and do not retain raw DOS result pointers after release. No managed
`IDisposable`, `using`/exception path, finalizer, or hidden global registry is used.

No borrowed file handle, current directory, input text, template, or library is
closed or freed. The helper does not change command streams. Its Release method
preserves the **current** DOS error; a command choosing a result independently
should retain that chosen error and pass it to its final startup cleanup.

## DOS remains the parsing authority

The implementation calls `DOS.ReadArgs(template, resultArray, APTR.Null)`.
This deliberately uses normal DOS input and lets DOS allocate/manage RDArgs;
it does not wrap the D0/A0 command text in an alternative lexer. The loader and
Shell must supply DOS's normal command-input buffering. Interactive `?`, quotes,
star escapes, numeric validation, keywords, `/M` handling and parser failures
remain DOS responsibilities.

`TryGetResult` returns the raw ULONG slot. The consuming command must interpret
it according to the template:

- `/N` contains a pointer to the LONG number, not the number itself.
- Ordinary strings contain pointers; `/M` contains a pointer to a NULL-terminated
  vector of string pointers.
- Switch and toggle values follow DOS's documented result representation.
- An optional pointer remaining zero is distinct from a supplied numeric zero.

Strings, numeric values and pointer vectors belong to the RDArgs allocation
and remain usable only while the lease is live. A command that retains a value
for asynchronous work must copy the required data into its own or the target
process's storage before release.

This first helper initializes all slots to zero. Commands whose original
contracts require caller-initialized nonzero defaults, custom RDArgs sources,
extended help, or caller-provided parse buffers need an explicit further slice.
Do not silently change `/T` or another command's defaults to fit this interface.
No undocumented user-facing option is introduced to select a parsing mode.

## Original reference evidence

Read-only inspection of `D:/TestData/AmigaDeveloperCD.iso` used these originals:

| File | SHA-256 |
| --- | --- |
| `NDK_3.1/DOCS/DOC/DOS.DOC` | `2e22d1d1b7c00fac5757eead0ef4d49a01c7666f0db5850735193c9597a7cee2` |
| `NDK_3.1/DOCS/DOC/EXEC.DOC` | `71df8eeab9f6b9873a38bea96bec34fd6e44b78aa2d67d8f1b28319c207dd2ad` |

`DOS.DOC:4118-4237` documents ReadArgs: buffered Input by default, one initialized
LONG per template option, numeric-pointer and multiple-pointer results, and
RDArgs as the allocation anchor. `DOS.DOC:2334-2356` establishes FreeArgs ownership:
DOS-allocated RDArgs is freed by FreeArgs; a caller-provided RDArgs requires
separate caller destruction after its associated storage is released. Reused
caller RDArgs must have `RDA_Buffer` reset for every call. This helper passes
NULL and therefore does not own or reuse a caller-allocated RDArgs structure.

`DOS.DOC:4487-4529` requires newline-terminated RunCommand arguments for normal
ReadArgs operation and describes the V37 input-buffer/GetArgStr setup and
restoration. The helper relies on that native launch boundary rather than
guessing command text from a host process.

`EXEC.DOC:905-1060` describes allocation rounding, cleared/public memory flags,
NULL on allocation failure, and longword-aligned results.
`EXEC.DOC:2500-2531` describes exact FreeMem pointer/size ownership and the danger
of freeing the same block twice. These constraints explain the lease and its
explicit invalidation, not a new resource-lifetime policy.

## Native boundary qualification

`tests/Commands.NativeRoot/NativeArgumentBoundaryProbe.cs` is a separate private
entry, compiled into its own HUNK for each CPU. It uses a 16-byte launch block
containing four big-endian ULONGs: `0x43433034` (CC04), scenario selector, expected
numeric value, and expected ambient IoErr after a successful fixture parse.
This namespaced test-control protocol is not DOS command input or a user option.
The helper still calls `ReadArgs` with NULL RDArgs and normal DOS input ownership.

The executor selects `command-argument-boundary-vector-fixture` explicitly.
It shares the existing HUNK loader, instruction loop, vector gateways, and
allocation ownership checks with the unchanged 31-case startup inventory.
Each suite loads one code image once, repeats calls, and interleaves two CPU
contexts with separate Process state, DOS bases, stacks, and allocations.

The boundary inventory is 15 independent cases, four failure/success repeats,
and six interleaved invocations: **25 per CPU**.

| Boundary | Verified native behavior |
| --- | --- |
| Empty template, zero result entries | One cleared, four-byte-aligned 4-byte dummy allocation; no exposed result index; success, allocation failure, and supplied parser failure paths. |
| NULL template with zero or one entry | `RETURN_ERROR`/`BadTemplate` (10/114), with no allocation or ReadArgs call. |
| Count `0x40000000` | Reject multiplication overflow before allocation. |
| Count `0x3fffffff` | Reject Exec chunk-rounding overflow before allocation. |
| Maximum safe count `0x3ffffffe` | Submit exactly `0xfffffff8` bytes to AllocMem; the fixture returns NULL immediately, without allocating huge host memory. Result is `RETURN_FAIL`/`NoFreeStore` (20/103), with no parser or free call. |
| Single-result allocation failure | Same 20/103 failure class, before ReadArgs. |
| Supplied parser errors 115 and 118 | Preserve the original parser error across FreeMem poisoning; free the result array once and never FreeArgs a failed parse. |
| Numeric 42, zero, and `int.MinValue` | Keep valid numeric pointers until release; verify values and reject out-of-range indices with zero output. |
| Successful parse with ambient IoErr 34567 | Lease outcome remains 0/0 without erasing the process's ambient error. |
| First successful Release | Preserve a subsequently selected IoErr 205 despite FreeArgs/FreeMem poisoning it to 901/902; free RDArgs before the exact result array. |
| Default, failed, and repeated Release | No resource or DOS vector operations; preserve the subsequently selected IoErr 202 and the stored parse outcome. |
| Released or invalid index access | Return false and zero; the bus rejects native reads from freed, guarded, or another invocation's allocations. |

The deliberate final IoErr 202 on successful boundary probes is a private test
checkpoint, not the success/error contract of any shipping command. Exact vector
sequences make hidden calls in default, failed, or repeated Release observable.
No borrowed stream can be closed. Every DOS gateway requires an active successful
OpenLibrary lease, and CloseLibrary requires owned cleanup to have completed.
The startup suite additionally tracks exactly one WaitPort/GetMsg and one owned
WBStartup reply under Forbid, with explicit valid ordering.

Interleaved calls use both 4096-byte and 16384-byte configured stacks. The largest
observed native stack-write extent was 156 bytes in this boundary probe and 120
bytes in the startup probe. These are observed fixture measurements, **not a
qualified minimum stack size**, and do not include real DOS implementation usage.

## Validation and remaining gates

Executed:

```powershell
dotnet build src/Commands/Native/CopperOS.Commands.Native.csproj --configuration Release --verbosity minimal
pwsh -NoLogo -NoProfile -File tests/Commands.NativeExecution/QualificationScriptRegressionTests.ps1
pwsh -NoLogo -NoProfile -File tools/Commands/qualify_native.ps1
```

The original helper build passed with zero warnings/errors. Final Foundation
run **`5062fe92a7514e2c95eb7c36f0df1658`** passed all three forced bootstrap
builds and **168 actual CPU invocations**:
`(31 startup + 25 boundary) x (68000, 68020, 68040)`. It includes the DOS-lease/WB
queue guards, both compiler corrections described below, source/binary drift
checks, and bootstrap failure reporting. Earlier runs
`71db809adfd14ad5b07a7a40b444e3e3` and `f46d48496f14444aacc76471eae8d902`
are retained as historical receipts, not current-source qualification.

Final receipt:
`tests/Commands.NativeRoot/bin/Release/net10.0/qualification/5062fe92a7514e2c95eb7c36f0df1658/qualification.json`
has SHA-256 `bf5ffb271eead79f17c3d3c48001bb61b3c14c66718ed04da4ac292b0ad7684d`.
Every HUNK reproduced byte-for-byte from its bound inputs. The schema-2 receipt
records six CPU/suite artifacts, **43 passed stages and 45 passed input checks**.

| Boundary HUNK CPU | Bytes | SHA-256 |
| --- | ---: | --- |
| 68000 | 3096 | `f44b9fdb258ca47a0768619c49eebf244da49dddff1d70caeff76d84a4d89424` |
| 68020 | 3116 | `d8773b0c14ba50dfc43823e2f043527d3356fb0a155290a6cab959d1ba329a4e` |
| 68040 | 3132 | `bdcb5ce77c5544e6cc69152b61c2ea100f577bf1652a4b08ca1d677fc179e8da` |

The adjacent `input-manifest.json` has SHA-256
`113c97b65e2f6092f2596818aa36ecfe3cb1828f7ff6043729937723f2c8db84`.
It binds **436 source/build-setting files**, including the qualifier script,
**20 copied managed input/runtime files**, **35 resolved build-input files**,
and **196 host runtime/build-tool files**. Raw sources and declared managed
dependency closures are copied into the attempt directory; builds use the
checked live sources, and native compilation/execution use the copied binaries.
The source inventory, original files, snapshots, restored project references,
and selected host runtime identities are checked before/after tool calls and
before publishing success. Unbound project references or changed input bytes/file
inventories fail the run. SDK 10.0.301 and runtime 10.0.11 are recorded; native
tools use the latter with runtime roll-forward disabled.

Each execution report also supplies the **actually loaded** Copper68k and
executor assembly paths/hashes and host runtime version. The script requires
these to match its captured identities. The Copper68k DLL hash is
`8046d9a2083c198647a02d9146b735629890d9a2e14bc313968a3b48139a2cd5`;
CopperFloat and all other declared dependencies are included in the manifest.

The script checks all six expected rows, suite identity, image hash, exact case
counts, runtime closure, zero shared initialized/BSS storage, and reproduction.
Any failed suite makes the run fail and prevents writing a new successful latest
receipt. An attempt receipt exists before compiler-root resolution or any
bootstrap build; failures retain their stage, message, native exit code when
available, and build log. Receipt/latest publication uses staged files. These
guarantees apply to valid script invocations whose output directory is writable;
they do not promise reporting after process termination or storage failure.

The six script regressions passed: a deliberately failing compiler build,
successful builds that mutate an existing source or add a source, changed binary
snapshot bytes, changed live binary bytes, and an added snapshot DLL. Bootstrap
fixtures use disposable projects and do not modify the real compiler. All three
bootstrap attempts failed as expected without artifacts or a changed successful
latest receipt; the three binary cases exercise the actual script guard functions.
The report is
`tests/Commands.NativeExecution/obj/qualification-script-regressions/a01005382f014c89be254d01ff4234e7/script-regressions.json`,
SHA-256 `9e52178d3bda3cf08bc7dbd1eeb74bc16412e8b1b7a63b0bc0ded2b65c964f6d`.
The earlier `f46d...` run's wrong-HUNK/suite rejection remains historical evidence.

This uses the **CC02.API09** compiler correction recorded in
[baseline-and-api-audit.md](baseline-and-api-audit.md): the Amiga target explicitly
supplies D0/D1/A0/A1 clobbers, consumed by both allocation IR and emitted effects.
The helper API above did not change. A separately authorized compiler correction
then made address-exposed 32-bit scalar argument reads reload their current
argument homes after ref mutation; the final Foundation run includes that fix.
The boundary implementation itself did not change compiler or SDK source.

Execution reports retain `realDosParser=false`. This evidence grants no real
Kickstart/CopperStart/MorphOS execution claim, shipping, P-bit, resident-registry,
or packaging approval.
Real ReadArgs/help continuation and licensed differential behavior remain later
gates. Native execution of a deliberately invalid allocation provider's alignment
or wrapping span is not covered here. Nonzero caller defaults/custom RDArgs and
compiler heap-context failure mapping remain separate work. The input manifest
binds this Foundation run at qualifier revision
`4eba03c9d4527745e98464f4d38783c343ee0e3137bb3153da15e979cc882ac3`;
later source or pipeline edits require a fresh run before claiming qualification
of those edits. Unrelated docs and other component projects are outside its
source scope. This is not a hermetic operating-system/environment claim.
