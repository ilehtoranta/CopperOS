# MUI / native Exec integration

Run from the CopperOS repository:

```powershell
dotnet run --project tests/MuiMaster.ExecIntegration/CopperOS.MuiMaster.ExecIntegration.csproj -- 68000
dotnet run --project tests/MuiMaster.ExecIntegration/CopperOS.MuiMaster.ExecIntegration.csproj -- 68000 library
dotnet run --project tests/MuiMaster.ExecIntegration/CopperOS.MuiMaster.ExecIntegration.csproj -- 68000 providers
dotnet run --project tests/MuiMaster.ExecIntegration/CopperOS.MuiMaster.ExecIntegration.csproj -p:CopperOSUseLocalCopperSharp=true -- 68000 classes
dotnet run --project tools/MuiMaster.LibraryBuilder -- 68000
dotnet run --project tests/MuiMaster.ExecIntegration/CopperOS.MuiMaster.ExecIntegration.csproj -- 68000 packaged tools/MuiMaster.LibraryBuilder/bin/Debug/net10.0/artifacts/68000/copperos-muimaster.library
dotnet run --project tests/MuiMaster.ExecIntegration/CopperOS.MuiMaster.ExecIntegration.csproj -- 68000 classes --output $env:TEMP\mui-classes-run
```

The optional `--output <directory>` selects an alternate artifact directory.
When specified, the directory must be empty; this prevents a verification run
from replacing earlier HUNK images or execution receipts.

Prerequisites are .NET 10 and sibling `CopperSharp68k`, `CopperStart`, and
`CopperMod` checkouts. The project references their local compiler, SDK, Exec,
Intuition, and Copper68k projects. Paths can be overridden with MSBuild properties
`CopperSharp68kRoot`, `CopperStartRoot`, and `CopperModRoot`.

The default scenario compiles two independent freestanding images. The MUI client calls
the SDK Exec allocation vectors. A separate test kernel installs CopperStart's
production Exec vector surface and initializes a real TLSF heap. CopperStart
implementation code is forbidden in the client's reachable assembly closure.
The emulator bus supplies memory only and rejects host traps; instruction
addresses must remain inside the compiled images or installed native vectors.

The client exercises private-root initialization, bounded memory, busy-root
rejection, destruction, repeated allocation/free, and actual heap exhaustion.
Successful execution returns 42. Framework, managed allocation, runtime,
exception-region, and external-native-target gates apply to both images.
Input hashes, HUNKs, maps, compatibility reports, and an execution receipt are
written under the runner's build output `artifacts/M68000` directory.

Pass `68020` or `68040` to compile and inspect both images for that CPU; those
variants are compile-only. This milestone proves the private-root ownership
boundary. It does not yet produce a disk-loadable `muimaster.library`, perform disk
loading, or exercise Intuition, rendering, application clients, or MorphOS
differential compatibility.

The `library` scenario compiles a third independent image containing only an
inert ordinary entry and the development lifecycle plus currently implemented
public-vector exports. It is named
`copperos-muimaster.library`, version 0.1; it does not claim the MorphOS MUI API.
The runner never executes this image's ordinary startup. A typed SDK Resident
and AutoInit fixture points directly at the exported ABI adapters, so its cold
initializer must initialize its own Exec base. The client, library and Exec
kernel share no compiler-generated writable library-base slots.

This scenario checks publication through InitResident, version rejection,
multiple opens, deferred expunge, reopening that clears deferred expunge,
zero-count close and saturated-count open guards, busy private-root refusal,
segment-token returns through direct library vectors, and both final close
and accepted removal through the real Exec vectors. Cold Init now also attaches
one typed 202-byte owner (private version 5) containing its native context,
empty service/registry/requester/public-object/drawing records, the
task-aware custom-dispatch frame head/poison state, and SDK SignalSemaphore.
Cold Init initializes the semaphore through Exec before publication. Native checks verify the idle
gate, root aliases, embedded addresses and four null
provider handles; this increment does not open provider libraries. Busy owner,
lease/class/object heads, notification state, callbacks and external children
must defer expunge without releasing storage. Cumulative sequence/mutation
counters do not prevent quiescent destruction. The `-66`/`-72`
`Error`/`SetError` adapters forward to DOS `IoErr()` and `SetIoErr()`. If DOS is
absent from this isolated Exec image, the lifecycle checks that `Error` returns
zero and `SetError` safely does nothing; that branch does not claim task-local
`IoErr` persistence. When DOS is available, the fixture checks a `0 -> 7 -> 0`
round trip.

The same cold lifecycle calls `MUI_RequestIDCMP` and `MUI_RejectIDCMP` through
LVOs `-90` and `-96`; passive instruction-boundary checks require each call to
reach its matching production entrypoint exactly once. The provider set is
absent in this scenario, so this qualifies negative-vector routing and register
ABI only, not mutation of a live object's IDCMP mask or an Intuition Window.

Heap exhaustion separately forces failure before Init, while allocating the
private root, and while allocating its class owner;
availability and largest-free-block values must be restored exactly. Passive
observations require six Init entries and five owner-sized Exec allocation
attempts across four successful installs and the three failure stages. Only actual JMP vectors
into the library image may execute from the allocated heap. No host callbacks
implement any library behavior.

Library artifacts and the execution receipt are written under
`artifacts/M68000/library` (or the selected compile-only CPU). This verifies
the native resident lifecycle independently of disk loading and MUI class
dispatch; those larger integration requirements remain open.

The `providers` scenario adds a test-owned callable adapter to its separate MUI
image, not to the production library's public vectors or the packaged artifact.
Its client receives only that adapter's resolved native address. The provider
owner and lease core must exist in the library image and be absent from the
client. Real InitResident creates the owner; a real library open protects the
task-level operation. Two attempts must reach real Exec OpenLibrary with
`utility.library`, fail because the provider is absent, and restore the same
empty owner, library links, open count, heap and scheduling nesting. No provider
is represented by a success stub or host callback.

This mode alone establishes a running task with normal SDK nesting fields and
user-mode execution. Unopened/null bases, Forbid, Disable and masked IPL must
reject acquisition without additional provider lookups. The owner's embedded
semaphore is acquired twice through
actual ObtainSemaphore and released twice, checking same-task ownership,
nesting and idle restoration without host callbacks. This is uncontended
recursion, not a blocked-waiter test. Empty explicit
retirement succeeds, then actual CloseLibrary/RemLibrary restore the heap. The
receipt records every library name/version and SR/nesting observed at the real
Exec OpenLibrary vector. The private adapter's callee-saved ABI is checked.
Artifacts are written under `artifacts/M68000/providers`; 68020/68040 remain
compile-only. This proves missing-provider admission/rollback, not successful
four-provider acquisition, concurrent task scheduling, or public MUI services.

The unexpected-provider-success branch also references the production
service-admission entry/exit helpers so their complete native closure is
compiled. The runner requires that implementation in the separate library and
rejects it in the client. This branch is not executed by the missing-provider
scenario; successful provider-backed service admission remains unqualified.

The `packaged <hunk-path>` scenario consumes the actual development library
emitted by `tools/MuiMaster.LibraryBuilder`. Its independent loader reads the
file's single CODE HUNK and RELOC32 records, checks their bounds and duplicate
relocations, and relocates those bytes. It never compiles a substitute library
image, uses compiler relocation metadata, or calls `LibraryFixture.Prepare`.

The native client receives only the relocated image's base and byte extent. It
scans those bytes for the embedded SDK Resident, validates its AUTOINIT table,
image-local management and public vector table, and terminated identity strings, then runs the
same full lifecycle and allocation-failure suite. Host descriptor reads are
passive and only select the initializer/open instruction addresses to observe.
An undersized native scan extent must fail before Init, and the packaged file's
executable guard must return -1 without entering the ordinary compiled startup.
InitResident and cold Init retain the existing callee-saved register checks.
The package run also calls the `-90` and `-96` vectors through the relocated,
installed library table and requires one visit to each corresponding image-local
entrypoint; provider-unavailable bounds remain the same as in the cold-library
scenario above.

Packaged receipts are written under `artifacts/M68000/packaged`, including the
exact input HUNK hash, discovered Resident address, relocation count, guard
result, rejected scan, and native execution statistics. This proves artifact
packaging plus cold native resident lifecycle, not DOS `LoadSeg`/`OpenLibrary`
disk autoloading, a filesystem installation, or public MUI class dispatch.

The `classes` scenario exercises the production MUI native class provider against
the real CopperStart BOOPSI algorithms. A separate production Intuition image
contains the actual exported adapters; neither the client nor kernel duplicates
the BOOPSI implementation. A typed passive export table binds those image-local
addresses, and its ordinary startup is never executed. The native kernel allocates an
owned subsystem base and rootclass through Exec and installs the production
MakeClass/FreeClass/AddClass/RemoveClass/NewObjectA/DisposeObject exports at the SDK
LVOs. Typed private registry state follows the full SDK IntuitionBase prefix;
the old fixed `0x36500` registry is guarded and must remain untouched.

The client receives that base and the actual native DoSuperMethodA thunk address.
No Intuition implementation is linked into the client, and no dummy Utility
callback vector is installed: production BOOPSI dispatch enters the guest through
the native Hook bridge. Passive ABI observations check D2-D7 and A2-A6 across the
owned BOOPSI exports and MUI/custom dispatchers. After client cleanup, the kernel
destroys its rootclass/base and the real Exec heap must equal its original size.
Only actual absolute JMP vectors into a compiled native image may run from the heap.
Native custom-dispatch callbacks now link a named 24-byte task/class/object/method
frame into the private owner while user code runs and unlink it before returning.
The MC68000 `classes` scenario exercises that frame lifetime: it returns **42**
after **6,528,814 instructions / 66,816,774 cycles**, restores heap usage from
**586,572** to **586,572** bytes, and records zero host traps. This does not call
the published `MUI_Redraw` vector.

The client also supplies explicitly fixture-owned, typed headless/service records
to the real generic class-service backend. It checks repeated GetClass/FreeClass
leases, stale-free refusal, and CreateCustomClass/DeleteCustomClass against actual
native IClass storage, including superclass counts, MUI_CustomClass contents,
binding cleanup, and exact heap restoration. This proves that the scalar native
provider can instantiate those production generics. It also exercises the
named public-object ownership core against a real Intuition object: TagItems
are validated, `NewObjectA` creates the object, a packed binding is linked into
the separate typed public-object registry, and `DisposeObject` sends OM_DISPOSE once before releasing
the class lease and unlinking the binding. Direct and public custom-class
object accounting is also qualified; the public binding retains the
`MUI_CustomClass` identity and releases only its object reservation. This
fixture-owned call is not a resident-vector invocation and does not acquire
state through public library vectors or exercise external MCC loading.

The class fixture aliases the builtin class names to its live base class and
exercises the resident MakeObjectA rectangle/text/image/control/menu seam for
the five rectangle forms, Button, Label, Checkmark, PopButton, Cycle, Radio,
Slider, String, NumericButton and Menuitem. Each form is created and disposed
through real Intuition; the temporary parameter, class-name, preparse and
TagItem storage crosses the boundary as named records and the public-object
binding is reused. This proves only the bounded fifteen-form family; remaining
menu forms and the MUI sidecar remain unqualified.

Class receipts live in `artifacts/M68000/classes`; MC68020 and MC68040 are
compile-only for this scenario. Their client/kernel/BOOPSI images are
306,792/111,928/76,456 and 302,200/108,764/75,128 bytes, respectively. This is
serialized, caller-owned BOOPSI subsystem integration, not
a published Intuition library lifecycle or packaged MUI public-vector admission.
It qualifies the native custom-class backend and the builtin/external public
object binding core, not the full MUI class-service or public-vector set.
Custom-class object accounting is qualified as a separate ownership seam;
MUI sidecar construction/destruction remains open, as do the larger
system-integration and MorphOS-compatibility gates.
In particular, the current BOOPSI backend's `FreeClass` refuses listed
classes. This fixture uses unnamed backend classes and does not qualify the
legacy MUI `MUI_GetClass`/`MUI_FreeClass` vectors. The MorphOS SDK marks both
MUI functions obsolete since MUI V8 and directs callers to
`MUI_CreateCustomClass`/`MUI_DeleteCustomClass`; their legacy runtime behavior
must not be inferred from Intuition's separate `FreeClass` API. See the
[MorphOS MUImaster autodocs](https://morphos-team.net/sdk/MUI/MUImaster.html).
