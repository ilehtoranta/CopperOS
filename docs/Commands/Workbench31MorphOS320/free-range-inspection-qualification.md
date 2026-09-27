# Read-only FreeMem range inspection

Recorded 2026-08-30. The final private selection passes **95 cases: 59 range,
ownership-fixture and strict-word-read cases, plus 36 existing allocator,
vector, pool and memory-handler controls**. This is a prerequisite for the
[RunCommand retirement handoff](runcommand-retirement-handoff-contract.md).
It enables no cleanup and grants no allocation ownership or release authority.
No native CPU, command, purity, resident or packaging gate is closed here.

## Implemented boundary

The new portable
[ExecMemoryCore.Inspection.cs](D:/Koodit/GIT/CopperStart/src/CopperStart.Exec/ExecMemoryCore.Inspection.cs)
exposes `InspectFreeMem` and fixed-width result data. The only change to the
existing `ExecMemoryCore.cs` is its partial declaration. `FreeMem`, Classic
allocation/free behavior, guest structures and public Exec vectors are unchanged.
There are no provider calls, allocator critical-section calls, writes, heap
allocations or mutable static state in the query.

For a non-null, nonzero direct Classic request it checks the eight-byte rounded
span, overflow, mapping, typed-address alignment, the complete bounded public
MemList topology and the selected header's ordered free-list/accounting. The
span must not overlap an already free chunk. Valid empty free lists and free
neighbours on both sides are supported. Coalescing with neighbours does not
enlarge the reported range of newly released allocated bytes.

The actual FreeMem owner probes for a CSAD descriptor 40 bytes before the user
address. The query requires that probe to be readable and rejects every CSAD
marker, including corrupt or unsupported descriptors. A valid descriptor can
redirect release to raw storage, a pool or DMA ownership, so a direct span would
be unsafe to infer. Non-marker probe bytes are retained for later revalidation.
Plain classic AllocVec instead has a four-byte length prefix; its user pointer
is not accepted as a direct FreeMem address. Real aligned/DMA vector controls
remain correctly releasable through their existing owner.

Null-address or zero-size requests return `NoOperation` without memory reads,
matching FreeMem's early return. Non-Classic policies, corrupt, unmapped,
misaligned or over-budget inputs return `InvalidOrUnsupported` with empty data.
The 4096-header and 4096-free-chunk bounds limit inspection, not the Amiga ABI.

Classic has no allocated-block ledger. An aligned interior address can pass
the geometric checks; a dedicated case demonstrates this limitation. The
future issuer must authenticate the actual DOS allocation and request, bind
the allocator kind from its real owner, serialize with mutation, revalidate
before each action, and exclude all live users of the **whole rounded range**.
Copied result fields, caller-supplied policy values and successful inspection
are not a retirement capability.

## Preserved qualification sequence

All accepted runs are private under
`D:/TestData/CopperOSCommands/Q/freegeomcd492bd1`. Each before/after pair uses
one unchanged test DLL and stable unrelated runtime inputs. The original Exec
source base is the frozen RET02 capture; it does not absorb the later Signal
guard or unrelated live edits. These assemblies are not a composed boot build.

| Selection | Before | After |
| --- | --- | --- |
| Initial 44 cases | 42 fail because the API is absent; two real allocator controls pass | 44 pass |
| Expanded geometry and existing controls, 93 cases | 55 absent-API failures; 38 pass | 93 pass |
| Strict odd-metadata reads, 95 cases | The initial query fails two odd Exec/header cases; the other 93 pass | 95 pass after the even-address guard |

The absent-API failures record a missing capability, not a reproduced premature
free. The later two alignment failures are an actual admission defect: the
initial query reached a forbidden odd word read in a strict memory adapter.
The repair rejects odd typed addresses before reading them. This adapter models
that 68000 constraint; it is not native instruction execution.

The [initial before](D:/TestData/CopperOSCommands/Q/freegeomcd492bd1/before-receipt.json)
and [after](D:/TestData/CopperOSCommands/Q/freegeomcd492bd1/after-receipt.json)
receipts have SHA256
`51785cbea90680a936705b2d0b71ccdb4335451bec76e76d4946feec66e34628`
and `8dcbe5385017c74caacb26978aeb4ed56fbebf3d47df713944618784f2ab0f22`.
The [expanded before](D:/TestData/CopperOSCommands/Q/freegeomcd492bd1/related-v2-before-receipt.json)
and [after](D:/TestData/CopperOSCommands/Q/freegeomcd492bd1/related-v2-after-receipt.json)
receipts are
`72852bb749cad367153f3c096b9ee7f276093e0ecbf3233fed642007d7292043`
and `9fe2693d2cc809cad365c9cd161b8da7edca5486b60861b3ce34f6e7c89ae062`.

The final [alignment baseline](D:/TestData/CopperOSCommands/Q/freegeomcd492bd1/alignment-before-receipt.json)
has SHA256
`342c3a0095c2337fd97fba83608fab71982fe66ef4bf4b12aead2c52f570fbf4`;
the [95-pass receipt](D:/TestData/CopperOSCommands/Q/freegeomcd492bd1/alignment-after-v2-receipt.json)
is `da6255aaf0e2583b169b30b392d844fc42660cc8feafd9656d9073fa8bbbf607`.
Its unchanged test DLL is
`d134d0102e38d42e745660c250224348f5b64a535082cfd32d38987cfeeb516a`.
Only Exec DLL/PDB change in this pair: owner DLL
`03448e6668bfd204986a82bce17f56fcf4ae8338febb47ce8b95760bdefa3d51`
becomes `434b93b242bc1cfc31e634fcf89c4c5a2afac61e95d67806bb0c95542a672f04`.
The 36 existing controls pass on both owners; totals above are overlapping
selections and must not be summed as distinct tests.

All query fixtures forbid guest writes, copying, clearing, allocator callbacks
and other platform effects during inspection, and compare complete memory
afterward. The expanded cases include actual Classic rounding (1132 to 1136
bytes), real vector owners, free-list fragmentation and both traversal-budget
boundaries. The existing tests exercise allocation/free/coalescing, vectors,
pool critical sections and memory-handler behavior independently of the query.

## Source and evidence binding

The [first PDB audit](D:/TestData/CopperOSCommands/Q/freegeomcd492bd1/pdb-audit.json),
SHA256 `fe69b375bb10b5a1027b8fb3f682315f1379e0d58fa08f5b026cebfcf4426108`,
matches four PE/PDB pairs and 177 retained source documents. The separate
[alignment PDB audit](D:/TestData/CopperOSCommands/Q/freegeomcd492bd1/pdb-alignment-audit.json),
SHA256 `e607024f6d00039f98beb28f906892c8dbe0889064f9a911dfbdde48b5f95421`,
matches three pairs and 173 documents. These checks overlap. Actual Compile,
ReferencePath and build properties for all six private projects are retained in
the [binding summary](D:/TestData/CopperOSCommands/Q/freegeomcd492bd1/bindings-summary.json).

Unaccepted setup attempts remain available: an inaccessible private descriptor
type failed to compile; a plain-AllocVec fixture incorrectly expected CSAD;
the first related runner duplicated adapter discovery and accidentally copied
the old test DLL into its after directory; an alignment preflight rejected an
unnecessary added deps file before running tests. None contributes acceptance.
Corrected captures use fresh directories, unique test-name checks and identical
test-binary checks; original logs/receipts are not overwritten.

The four historical RET03 callback liveness failures still require the real
handoff. Surviving callback/wait roots, exact owner/revision/epoch validation,
ordered input restoration, packet draining, native Switch and unsupported
allocator forms remain required in the
[retirement qualification record](task-retirement-qualification.md).
