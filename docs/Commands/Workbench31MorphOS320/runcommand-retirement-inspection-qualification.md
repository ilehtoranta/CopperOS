# RunCommand retirement inspection: RET03.1

Recorded 2026-08-30. The portable inspection passes **31 new cases and all 337
tests in its inclusive selection**. The first host selection passes **26 new
cases and all 63 tests**. A separate read-only memory guard then changes **11 new
failures plus the same 63 passes to 74 passes**, using one unchanged test DLL.
Selections overlap; these are host tests, not native command invocations.

This completes the bounded read-only inspection slice of the
[handoff contract](runcommand-retirement-handoff-contract.md). No lease, stack
restoration, callback release, changed DOS barrier or cleanup consumer is
implemented. The four historical RET03 liveness failures remain open. This is
not a full boot, native Switch, CLI, resident/pure or shipping qualification.

## Boundaries and limits

`DosRunCommandCore.InspectRetirementContext` takes guest memory, DOS state, a
named task and an exact token; token zero selects that task's top context. It
uses only `IAmigaGuestMemory`, not `IDosPlatform`. It cannot call providers,
consult `CurrentDosTask`, lazily create a Process/console, or allocate storage.
No existing Begin, Resume, argument restoration or teardown behavior changes.

Before exposing context facts it validates the complete DOS object chain,
strict token order, each task's parent chain, and the complete Process/live
captured-input membership lists needed by all RunCommand records. A released
input wrapper is not read. A changed selected Input does not erase the captured
handle's ownership. Objects belonging to other tasks cannot hide corrupt
RunCommand nesting. Generic objects are traversed as object headers; their
own payload semantics are not qualified here.

The result distinguishes `Absent`, `Context`, `TokenNotFound`, invalid request,
invalid state, invalid ownership, `TraversalLimit` and `WorkLimit`. Failed
portable inspection returns empty chain/context facts. `Complete` means that
the stated structural inspection finished; it does not certify CPU quiescence
or authorize release. Absence of RunCommand records does not establish absence
of all other DOS-owned resources.

Each object, parent, Process and handle walk has a 4096-node bound. An
invocation-local, four-byte budget limits the aggregate query/node visits to
65536. The host shares that budget across its complete multi-context query.
Default, consumed or invalid budgets fail closed; partial traversal is never
treated as absence. Bounds are conservative inspection policy, not Amiga ABI
limits. Caller serialization against mutation remains a precondition.

The 40-byte chain and 124-byte context records contain no managed fields.
`AllocationRequestBytes` is the DOS-owned header's raw request, not a rounded
allocator release span. Command stack bounds are derived from that request and
the fixed continuation/argument layout. Raw StackSwap descriptor contents are
reported separately because entry exchanges command and caller triples. Raw
stack size may be odd; no invented multiple-of-four requirement is applied.
Input wrapper membership is checked, but buffer validity and the exact later
restoration operation still belong to the input owner.

`DosServices.InspectRunCommandRetirement` matches every named-task host
RunCommand callback against that portable chain, innermost first. It checks
token/state/descriptor identity, command and captured caller triples, nesting,
the top public task bounds and expected callback-return stack pointer. It
reports mixed generic callbacks, one-sided ownership, unmatched counts,
invalid owners, pending reset and incomplete work separately. It does not
inspect all surviving tasks' callback/wait roots or grant a retirement capability.

The final host path uses a dedicated `RunCommandInspectionMemory` adapter. Its
five write, clear and copy operations throw immediately. It exposes no DOS
platform or CPU state, checks address arithmetic and rejects odd typed reads.
Thus a hypothetical write followed by restoration cannot evade the guard.
The earlier host checkpoint had complete before/after RAM snapshots and source
inspection, rather than this explicit write trap; that historical distinction
is preserved.

## Frozen sources and private proof

Only the partial declaration changes in existing `DosRunCommandCore.cs` and
`DosServices.cs`. New production files are listed below. Faraday's checked
write-provider work and the Close branch were not changed or silently included
in these private builds.

| Source | SHA-256 |
| --- | --- |
| CopperStart `DosRunCommandRetirementTypes.cs` | `8b6e49fcd15440625ffddd33e023d156d54b710eb0eb1ac46d6ae6523a35d862` |
| CopperStart `DosRunCommandCore.RetirementInspection.cs` | `461f05f30099c910e2695e40064fd3ffaf101626f9164b4d36fa9066cc8eb8e7` |
| CopperStart `DosCore.RunCommandRetirementInspection.cs` | `f14ce31522743b9cb085e88584798509a4ddd0caf3c094fd9910c2f7686aac00` |
| MedPlayer `DosServices.RunCommandRetirementInspection.cs`, first checkpoint | `7acdf83a5f79cc25627cf827ee1399ecf90e7bfad140700b771c34e64ea27b8d` |
| Same host source, final read-only memory guard | `4a947bb685210b2a83884c2d01259f217a077acb806d040c9cf130a0a5ef0bf5` |
| Portable `DosRunCommandRetirementInspectionTests.cs` | `8e1bbaa94c409b4c9e43777eb58c0d236fe8e06e83732ba00ebb6c3372b82c7d` |
| Host `DosRunCommandRetirementInspectionTests.cs`, unchanged by guard | `a623991a17d3b1db1d3577510d2f5887db5548bedf0f9360429d4a8fb0ca4c81` |
| New host `DosRunCommandInspectionMemoryTests.cs` | `1945771dec0ae5ace3b66db05be89ac025c42514afc2d23e9af744023b5b8a1d` |

The [first receipt](D:/TestData/CopperOSCommands/Q/ret03inspectab01/receipt.json),
SHA-256 `8d9012c117a30775e177847b62892b665e6450020308b4f502538cc65b3e5070`,
binds 31/337 portable and 26/63 host passes. Its source parents are the frozen
API17 DOS `1c4db0fa5b8fe3fae596af2c5c5c466faac75ed0db5f7f7f3ecf98cee29d53ef`
and host `9ab44f92657852a6d7d8e8cc625b91e6529b913c018b672aca53307937e32871`.
The capture includes 94 explicit DOS sources (91 existing plus three), 125 host
sources (124 plus one), actual Compile/ReferencePath records, pinned copies,
restore inputs and stable outputs. No whole live provider tree, compiler or SDK
was built. These host sources predate the RET02 scheduler/Exec-owner changes;
this is not a composed retirement integration build.

The resulting DOS DLL is
`7728de2c9afdcfd465916da9e54f817903ad2c058e18db85ab7fb5f308eb072c`;
the first host DLL is
`cb1067bfece414d4cdf70a30a110f7e64769198bb71aceee56f7dfc23d5b995f`.
Portable tests use a memory-only adapter that throws on every write. Host tests
compare all chip RAM, callback records, visible CPU/control fields, provider
call counts, allocations/frees and owner fields. Cases include interleaved
tasks, foreign tokens, released-input wrappers, corrupt tails, one-sided and
mixed state, exact traversal boundaries, and compound-budget exhaustion.

The separate [guard baseline](D:/TestData/CopperOSCommands/Q/ret03inspectguard02/before-receipt.json),
SHA-256 `f505cc202555401681facbe2043db308e30de0cacc5410a902b5135eb05ebc9b`,
has 11 missing-adapter failures and 63 unchanged passes. Its
[after receipt](D:/TestData/CopperOSCommands/Q/ret03inspectguard02/after-receipt.json),
SHA-256 `94f14c0fbb6f2f738dc63419ad4cf998acf1e292d3565f08584687c6f2cf3fbe`,
passes all 74 with the same test DLL
`543b0a0073cb49152894c60ae7d4eab7df12f639ae88870a735dc5ab12b5869f`.
Only the host owner DLL changes to
`be49cd9e7bc3c5cd54f93d52f03e0380bf584aec03f8622edc57c42cfddc146e`;
the DOS DLL stays `7728de...`. The one-file
[source delta](D:/TestData/CopperOSCommands/Q/ret03inspectguard02/host-source-delta.patch)
replaces the inspector's memory adapter without changing its matching logic.

An [earlier guard attempt](D:/TestData/CopperOSCommands/Q/ret03inspectguard01/before-receipt.json)
is retained as failed evidence: the 74 tests produced the expected 11/63 result,
but the receipt helper read `tests` instead of the result parser's `rows` key.
The helper was corrected and a fresh baseline rerun; that failed envelope is
not acceptance, and none of its files was overwritten.

Reproducible capture helpers are
`D:/TestData/CopperOSCommands/RetirementBoundary/ret03_inspection_capture.py`
and `ret03_inspection_memory_guard.py`. They reject reused outputs and changed
parents. For the guard's exact before/after design, select a fresh private path:

```powershell
C:\Python314\python.exe D:/TestData/CopperOSCommands/RetirementBoundary/ret03_inspection_memory_guard.py --phase before --output D:/TestData/CopperOSCommands/Q/ret03inspectguard-new
C:\Python314\python.exe D:/TestData/CopperOSCommands/RetirementBoundary/ret03_inspection_memory_guard.py --phase after --output D:/TestData/CopperOSCommands/Q/ret03inspectguard-new
```

The original captured source hashes above, rather than whatever later lives in
the working tree, define each historical checkpoint.

## Next bounded RET03.2 work, before mutation

The [read-only FreeMem range query](free-range-inspection-qualification.md) is
now a separately qualified prerequisite. Its geometry is freely copyable data,
not proof that an aligned interior address is an allocation start. The DOS
header/task/token/generation/request and actual bound owner must be validated
independently. Do not combine unrelated private DLLs as a qualified boot image.

| Slice | Test-first action and owner | Required outcome |
| --- | --- | --- |
| `RET03.2A` | Add read-only DOS/Exec owner-range binding using actual `ExecMemoryContext`, bus, allocator kind and allocation/free delegate identity. Query every nested raw request and retain the complete rounded intervals plus owner-header/CSAD-probe facts. Use real Classic allocations. | Foreign/replaced allocator, different bus/free delegate, stale generation/token, unsupported descriptor, overlap or uninspectable rounded padding declines. Odd stack/request sizes keep their original meaning. No free or lease is issued. |
| `RET03.2B` | Enumerate other tasks' pending callback and wait records through their owning `DosServices`, under one shared census/inspection budget. Return original frame data and validated typed caller/command triples; report generic callbacks or missing stack metadata explicitly. | Each surviving return PC, captured stack and relevant argument/data pointer into any retained interval prevents later retirement. An arbitrary outside stack or a missing row is not normalized to absence. No dictionary/CPU/guest mutation. |
| `RET03.2C` | Compose those facts with the actual RET02 issuer-owned synthetic commit and saved-frame checks in a new private capture. Require stable owner/CPU/state/task-incarnation/epoch and a revalidated complete census before any future typed transition. | Stale/reset/foreign/replayed or changed facts decline. Failed and incomplete/native Switch stays deferred. This is still inspection until the separately reviewed lease and exact named-task transition are implemented. |

Concrete owner wiring was read in `AmigaBoot.cs`
(`4ef1aae5d9776a6c28874da9c84728eb916d07c784aa16c2d3b610ae373621fb`):
lines 741-744 bind Exec memory operations to `AllocatePortableMemory` and
`FreePortableMemory`; 863-898 pass the same allocation/free path into DOS and
Exec memory contexts; 907-908 bind the retirement barrier and memory context;
2553-2554 return that context; 13134-13156 choose the actual allocator policy.
`ExecMemoryServices.cs`
(`6ec1c5bc39e7645a46b0ab2b13a344cd4e03628a838c7bfa10dbe046de4208cb`)
owns the sealed context, allocator kind, bus, allocation/free delegates and
`CreatePlatform` at lines 9-42. A getter returning some context is insufficient
if the DOS allocation/free delegates or the reaper's owner differ. Supplied
allocation-ledger fixtures cannot substitute for that real binding proof.

Surviving callback records currently contain `Frame`, state, token, expected
return SP and caller/command triples. `PendingWait` contains a frame/token but
no captured stack triple; it must be paired with the actual saved execution
owner before admitting a continuation interval. `RestoreStack == false` does
not reveal every generic callback kind, so no invented kind or bounds should
be inferred from its LVO. Preserve these unknowns for their concrete owner
gate, rather than skipping those roots.

The first useful finite tests are: another task's callback return PC into each
inner/outer rounded interval; a saved RunCommand argument pointer into the
removed caller's storage; a callback caller stack outside its current command
bounds but inside a validated captured triple; a wait frame without matching
saved execution metadata; changed callback counts/tokens, reset and budget
exhaustion; and clean nested/interleaved real-Classic observations. Include
rounded tail bytes, not only the nominal stack payload. Keep all target
intervals in the reachability union throughout later inner-to-outer work.

The existing RET02 `FrameIsClear` accepts every nonzero stack bank only within
that task's public bounds. Legitimate RunCommand restoration therefore needs
a narrow typed handoff, not a blanket relaxation or recapture. Host callback
and wait publication/consumption must also be revalidated across future owner
calls; the current metadata inspection is not a revision capability. Packet
and notification draining, DOS process-owned storage, unsupported allocator
families and native Switch remain separate required gates. No `CurrentDosTask`
swap, provider-ready Boolean or `Resume(false)` can replace them.
