# muimaster.library

This directory is reserved for CopperOS's clean-room implementation of the
public MorphOS 3.20 MUI contract for the `MorphOs320M68k` profile. It targets
freestanding MC68000 code and uses the public SDK ABI owned by
`CopperSharp.Sdk.Amiga`; it is not a generic/classic Amiga MUI implementation
and does not copy MorphOS implementation code.

`MUI_MakeObjectA` now decodes its variable parameter prefix once into the
named `MuiMakeObjectParameterRecord` through
`MuiMakeObjectParameterCodec`. Construction and tag materialization use named
`First`/`Second`/`Third`/`Fourth` fields; malformed count, null, and truncated
vectors are rejected at the guest-memory boundary. Host/source/CIL remains
**540/540**. `MakeObjectParameterCodecRoot` produces **1,176 / 1,200 / 1,208
bytes** for MC68000/020/040 with zero relocations and no framework members or
managed allocation sites; MC68000 returns **42** after **388 / 4,406**
instructions/cycles.

Application/message getters now share a typed public-admission predicate for
`MUIA_ApplicationObject`, `MUIA_AppMessage`, and `MUIA_Window_AppWindow`.
Generic common-control `OM_GET` projects these values through the named
`MuiApplicationMessageRoutingStateRecord` even for unknown or custom classes,
matching direct `Get` without exposing raw handler offsets. The slice remains
freestanding, exception-free, managed-runtime-free, and struct-first; host
coverage is **1197/1197**.

Application `Commands` and `WindowList` getters now use the same typed
common-control `OM_GET` seam. The caller-owned command table and read-only
Exec List projection remain represented by named guest structs, including
validation and topology-derived window membership; no raw handler offsets are
introduced. Host coverage is **1198/1198**.

The broader Application/Window lifecycle, identity, policy, relationship, and
focus getter family now shares that typed common-control `OM_GET` seam. The
existing named guest records remain authoritative and no raw handler offsets
are introduced; host coverage is **1199/1199**.

Window.mui control, policy, relationship, lifecycle, presentation, visual, and
event getters now share the typed common-control `OM_GET` seam as well. The
existing named guest records remain authoritative, and the native window
pointer remains capability-backed; host coverage is **1200/1200**.

Event-handler reconciliation now honors `MUI_EHF_ISACTIVEGRP` through a
bounded named-parent walk from the active or default object. Handler state
continues to cross the named codec without managed object graphs; host
coverage is **1201/1201**.

The handled-events slice adds a named `MuiAreaHandledEventsStateRecord` in
object Dataspace. It owns the event mask, Window association, and generated
`MuiEventHandlerNodeRecord`; family attach/detach and object disposal reconcile
that registration through the existing guest list, with no managed shadow
state, exceptions, or raw object offsets. Focused host coverage is **1/1**.
The complete host suite is **1202/1202**.
The numeric C `MUIA_HandledEvents` ingress remains deferred pending MorphOS ABI
verification; the [MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html)
defines the property semantics.

The same named handled-events record now carries MorphOS MUIArea's
`eventHandlerAlwaysKeys`, `eventHandlerGuiMode`, and signed
`eventHandlerPriority` policy. Policy changes detach and unregister the live
generated node before reconciling a new node, and `SetHandledEvents` preserves
the policy across event-mask updates. GUIMODE is enabled by default as in the
MorphOS API. Numeric C attribute ingress remains deferred until the ABI values
are verified.

The Area registration is also exposed through the public named
`MuiAreaEventHandlerStateInput` and `MuiAreaEventHandlerPacketCore` seam. This
keeps the future Objective-C bridge on typed values while Dataspace keys,
private object layout, and generated-node ownership remain internal.

Area activation now uses one named guest-resident
`MuiAreaActivationStateRecord` Dataspace record for Active, Flags, and its
generation. `GoActive` and `GoInactive` replace that record as one state
update, while `MuiAreaActivationPacketCore.TryGet` provides a typed public
projection. The implementation remains freestanding, exception-free,
managed-runtime-free, and struct-first; native GoActive/GoInactive ABI parity
is still progressive. Focused activation coverage is **7/7**; the MG450
qualification was **1204/1204** and the current complete host suite is
**1221/1221**.

The public `MuiAreaActivationPacketCore` now also exposes typed `GoActive` and
`GoInactive` transitions for the future Objective-C bridge. Packet decoding,
Dataspace keys, and guest ownership remain internal.

MorphOS `MUIA_DoubleBuffer` is now represented by the named guest
`MuiAreaDoubleBufferStateRecord`. Generic Get/Set and OM_GET/OM_SET normalize
the BOOL through that record, and `MuiAreaDoubleBufferPacketCore` provides a
typed value seam for future bridge code. Native off-screen allocation,
render-info replacement, and blitting remain progressive; no managed bitmap,
exception path, or managed runtime state was introduced. Focused coverage is
**4/4** and the current complete host suite is **1221/1221**. The behavioral
reference is the [MorphOS MUIArea documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

MorphOS `MUIA_ShortHelp` now uses the named guest
`MuiAreaShortHelpStateRecord` and typed `MuiAreaShortHelpPacketCore` seam.
Generic Get/Set and OM_GET/OM_SET retain the caller-owned `OBString` pointer
without a managed string shadow. Bubble creation, hit-testing, deletion
callbacks, and native help UI remain progressive. Focused short-help coverage
is **4/4** and the current complete host suite is **1221/1221**.

The fixed `MUIP_CreateShortHelp` and `MUIP_DeleteShortHelp` packets now cross
named codecs. Create returns the caller-owned ShortHelp pointer and Delete is
an accepted non-owning no-op; dynamic CheckShortHelp and native bubble
allocation remain progressive. Focused short-help packet coverage is **6/6**
and the current complete host suite is **1221/1221**.

Common-control drawing now consumes the named `MuiAreaRenderPolicyStateRecord`
for `MUIA_FillArea`. When FillArea is false the default background clear is
suppressed while frame and content rendering continue; focused coverage is
**1/1** and the complete host suite is **1221/1221**. See the [MorphOS MUIArea
documentation](https://morphos-team.net/sdk/objectivec/MUIArea.html).

`MUIA_CustomBackfill` now uses the named
`MuiAreaPresentationStateRecord` BOOL field across construction, generic
Get/Set, and OM_GET/OM_SET. Setter values normalize to zero or one; focused
coverage is **2/2** and the complete host suite is **1221/1221**. Native
custom-backfill callback behavior remains progressive.

`MUIA_Draggable` and `MUIA_Dropable` now use the named
`MuiAreaDragPolicyStateRecord` for construction, generic Get/Set, OM_GET, and
drag-policy checks. Both setters normalize BOOL values and Dropable defaults
to TRUE as documented by MorphOS; focused coverage is **2/2** and the complete
host suite is **1221/1221**.

`MUIA_FrameVisible` now lives in the named
`MuiAreaRenderPolicyStateRecord`. Generic Get/Set, OM_GET, base Area Draw, and
common-control DrawControl suppress only frame lines when it is false; fill
and content remain active. Focused coverage is **2/2** and the complete host
suite is **1221/1221**.

`MUIA_ShowSelState` now uses named fields in
`MuiGadgetInteractionStateRecord` and `MuiImageRenderStateRecord`. The
initialize-only flag defaults TRUE, is projected by Get/OM_GET, is rejected by
runtime Set/OM_SET, and controls selected Gadget borders and builtin Image
selected pens. Focused coverage is **2/2** and the complete host suite is
**1223/1223**.

The packed GadTools `NewMenu` entries consumed by `MUIO_MenustripNM` now use
`MuiNewMenuRecordCodec` and the named `MuiNewMenuRecord` fields `Type`,
`Label`, `CommandKey`, `Flags`, `MutualExclude`, and `UserData`. Validation and
tree construction no longer repeat guest offsets. Host/source/CIL remains
**540/540**. `NewMenuRecordCodecRoot` produces **1,308 / 1,348 / 1,348 bytes**
for MC68000/020/040 with zero relocations and no framework members or managed
allocation sites; MC68000 returns **42** after **330 / 3,532**
instructions/cycles. Image-menu entries remain unsupported until their
MorphOS image semantics are qualified.

The existing 40-byte MorphOS `MUIP_BoopsiQuery` alias now uses
`MuiBoopsiQueryMessageCodec` for both guest reads and writes. Consumers use
the named `MuiBoopsiQueryMessage` fields; the packed offsets are confined to
the codec. Host/source/CIL remains **540/540**. `BoopsiQueryMessageCodecRoot`
produces **1,868 / 1,972 / 1,972 bytes** for MC68000/020/040 with zero
relocations and no framework members or managed allocation sites; MC68000
returns **42** after **709 / 8,052** instructions/cycles. External BOOPSI
callback semantics remain capability-backed and separate.

The Dataspace superclass packet family now routes Add, Find, Get, Merge,
Remove, and Clear through `MuiDataspaceMessageCodec`. Consumers keep named
fixed-width records and the packed guest offsets are confined to the codec.
Host/source/CIL remains **540/540**. `DataspaceMessageCodecRoot` produces
**3,876 / 3,948 / 3,952 bytes** for MC68000/020/040 with zero relocations and
no framework members or managed allocation sites; MC68000 returns **42** after
**1,415 / 15,474** instructions/cycles.

The paired ReadIFF and WriteIFF packet boundary now uses
`MuiDataspaceIffMessageCodec`. `DataspaceIffMessageCodecRoot` produces
**2,316 / 2,356 / 2,356 bytes** for MC68000/020/040 with zero relocations and
no framework members or managed allocation sites; MC68000 returns **42** after
**703 / 7,804** instructions/cycles. Stream behavior remains capability-backed.

The fixed `MUIM_CallHook` envelope now uses `MuiCallHookMessageCodec`; the
variadic tail remains caller-owned guest storage. Host/source/CIL remains
**540/540**. `CallHookMessageCodecRoot` produces **1,332 / 1,348 / 1,348
bytes** for MC68000/020/040 with zero relocations and no framework members or
managed allocation sites; MC68000 returns **42** after **351 / 3,768**
instructions/cycles.

The fixed `MUIM_GetConfigItem` packet now uses
`MuiGetConfigItemMessageCodec`. `GetConfigItemMessageCodecRoot` produces
**1,340 / 1,356 / 1,356 bytes** for MC68000/020/040 with zero relocations and
no framework members or managed allocation sites; MC68000 returns **42** after
**351 / 3,756** instructions/cycles.

Notify `MUIM_WriteLong` and `MUIM_WriteString` now use
`MuiNotifyWriteMessageCodec`. `NotifyWriteMessageCodecRoot` produces
**2,324 / 2,356 / 2,356 bytes** for MC68000/020/040 with zero relocations and
no framework members or managed allocation sites; MC68000 returns **42** after
**703 / 7,790** instructions/cycles.

The fixed Layout packet family now uses `MuiLayoutPacketCodec` for AskMinMax,
Relayout, DrawBackground, Backfill, and Text. Consumers retain named packet
structs and the packed guest offsets are confined to the codec. Host/source/
CIL remains **540/540**. `LayoutPacketCodecRoot` produces **2,252 / 2,340 /
2,348 bytes** for MC68000/020/040 with zero relocations and no framework
members or managed allocation sites; MC68000 returns **42** after **879 /
9,468** instructions/cycles.

The bounded ASL TagItem walker now uses the named
`MuiAslTagItemRecord`/`MuiAslTagItemCodec` boundary. TAG_DONE, TAG_MORE,
TAG_SKIP, and TAG_IGNORE traversal remains unchanged; raw 8-byte record
offsets are confined to the codec. Host/source/CIL remains **540/540**.
`AslTagItemCodecRoot` produces **1,296 / 1,284 / 1,288 bytes** for
MC68000/020/040 with zero relocations and no framework members or managed
allocation sites; MC68000 returns **42** after **309 / 3,306**
instructions/cycles.

The fixed Notify packet family (`MUIM_Notify`, `MUIM_KillNotify`,
`MUIM_KillNotifyObj`, `MUIM_Set`, `MUIM_MultiSet`, and `MUIM_FindObject`) now
uses `MuiNotifyPacketCodec.PacketAddress` and named message structs. Parameter
walking and MultiSet-vector decoding are centralized, and Application
`MUIM_Set` reuses the same codec. Host/source/CIL remains **540/540**.
`NotifyPacketCodecRoot` produces **2,996 / 3,060 / 3,060 bytes** for
MC68000/020/040, with zero relocations and no framework members or managed
allocation sites; MC68000 returns **42** after **933 / 10,140**
instructions/cycles. The older broad Notify root is still host-covered but is
tracked for closure-size cleanup because its current map retains internal
relocations. This boundary uses no exceptions or managed runtime services.

Application and Window menu query/set/event-handler packets now cross named
request structs and central codecs. The `MUIM_Set` path for
`MUIA_Window_ActiveObject` uses the same typed packet boundary, so consumers
operate on named menu IDs, state, handler, and active-object fields rather
than repeating ABI offsets. Host/source/CIL remains **540/540**. MC68000
focused artifacts are **44,656**, **44,008**, **48,788**, **49,992**, and
**44,612** bytes for ApplicationMenu, WindowMenuState, WindowActiveObject,
WindowActiveObjectSpatial, and WindowEventHandler; all return **42** in native
execution. Framework analysis has no members or managed allocation sites. The
two larger ActiveObject MC68020 closure artifacts retain **17** internal method
relocations; MC68000/040 and all other affected maps are relocation-free. The
inline SetCycleChain vector remains an explicit array ABI boundary.

Window SetCycleChain now decodes through a named request struct and central
codec. The inline object vector remains an explicit array ABI boundary with
overflow and mapping checks; the typed packet result reaches the existing
failure-atomic chain core. Host/source/CIL remains **540/540**.
`WindowCycleChainRoot` produces **43,148 / 49,248 / 46,284 bytes** for
MC68000/020/040, with zero-runtime maps, and MC68000 returns **42** after
**235,109 / 2,472,400** instructions/cycles.

SetConfigItem, OpenConfigWindow, BuildSettingsPanel, and Save/Load settings
packets now use a named settings-packet request struct and central codecs.
Typed item/data, flags/class ID, panel number, and settings-name fields reach
the existing cores; SetConfigItem retains its named guest-resident state.
Host/source/CIL remains **540/540**. Their MC68000/020/040 focused artifacts
are **43,268 / 49,216 / 46,360**, **42,536 / 48,560 / 45,608**,
**42,116 / 48,100 / 45,192**, and **43,364 / 49,428 / 46,444** bytes,
with zero-runtime maps; MC68000 returns **42** after
**113,793 / 1,190,580**, **149,117 / 1,563,576**, **131,221 / 1,372,866**,
and **206,791 / 2,176,314** instructions/cycles.

Application ShowHelp and AboutMUI now use a named presentation-packet request
struct and central codec. Dispatcher consumers use typed reference-window,
help-file, node, line, and AboutMUI fields with guest-memory access confined to
the codec. Host/source/CIL remains **540/540**. Their MC68000/020/040 focused
artifacts are **42,112 / 48,100 / 45,188** and **44,540 / 50,752 / 47,664**
bytes, with zero-runtime maps; MC68000 returns **42** after
**179,229 / 1,880,818** and **354,512 / 3,722,488** instructions/cycles.

Application PushMethod and UnpushMethod now use a named queue-packet request
struct and central codec. Dispatcher consumers use typed destination, count,
target, selector, and method fields; the inline parameter block address is
derived by a named codec boundary. Host/source/CIL remains **540/540**.
`ApplicationQueueRoot` produces **44,624 / 50,628 / 47,756 bytes** for
MC68000/020/040, returns **42** after **287,955 / 3,023,546**
instructions/cycles, and has zero-runtime maps.

The Application method packet family now uses a named request struct at the
native codec boundary. DefaultConfigItem, CheckRefresh, Execute/Run, Window
setup/cleanup/depth methods, and Window Snapshot decode through typed packet
records, keeping guest-memory access in the codec and preserving the
freestanding 68k call boundary. Host/source/CIL remains **540/540**.
`ApplicationDefaultConfigRoot`, `ApplicationCheckRefreshRoot`,
`ApplicationLoopRoot`, and `WindowSnapshotRoot` produce MC68000/020/040
artifacts of **42,128/48,108/45,204**, **43,392/49,540/46,524**,
**46,472/52,720/49,668**, and **41,768/47,616/44,852** bytes respectively;
all have zero-runtime maps. MC68000 returns **42** after
**130,429/1,364,486**, **286,991/3,013,792**, **176,768/1,863,918**, and
**94,864/996,550** instructions/cycles.

ReturnID, Input/NewInput, InputBuffered, and input-handler packet decoders now
use one named codec. Dispatcher paths consume typed return IDs, signal-storage
pointers, and handler pointers. Their MC68000/020/040 focused artifacts are
**43,348/49,320/46,480**, **44,040/49,988/47,164**, **43,000/48,932/46,120**,
and **44,336/50,440/47,512** bytes; all are zero-runtime clean. MC68000
returns **42** after **149,331/1,570,710**, **133,700/1,404,724**,
**178,040/1,867,702**, and **175,220/1,836,748** instructions/cycles.
Remaining MorphOS Application behavior and ABI coverage remain progressive
work.

The Stringscroll UTF-8 metric seam now follows MorphOS 3.20's codepoint
orientation: valid UTF-8 sequences count as one visual column, CR/LF handling
preserves logical lines, malformed bytes remain visible as one-column fallback
characters, and horizontal drawing starts and ends on codepoint boundaries.
The focused native metric root is zero-runtime clean at 2,224/2,296/2,216
bytes for MC68000/020/040 and returns 42 on MC68000. Full Stringscroll input,
rendering, and MorphOS differential parity remain progressive work.

The MorphOS `String.mui` scroll attributes now reuse the same UTF-8 metric
scanner as `Stringscroll.mui`. The focused record-backed native metric root is
zero-runtime clean at 31,384/35,396/33,268 bytes for MC68000/020/040 and
returns 42 on MC68000. Full String.mui rendering/input parity remains
progressive work.

The fixed Misc specialist/object-aware packet family now uses
`MuiMiscSpecialistMessageCodec` for `OM_GET`, `OM_SET`/`MUIM_NoNotifySet`,
`OM_DISPOSE`, `Panel_Run`, `Title_*`, and `Mccprefs_RegisterGadget`. Both
dispatchers consume named lifecycle, attribute, pointer, pair, and gadget
records; packed guest offsets are confined to the codec. Host coverage is
**555/555**. The focused native boundary is zero-runtime clean at
**3,968/4,036/4,036 bytes** for MC68000/020/040 and returns **42** on
MC68000 after **1,476 instructions / 15,648 cycles**. The existing native
Misc Setup/Cleanup root separately covers the lifecycle dispatch path; full
Misc behavior and MorphOS differential parity remain progressive work.

Application/Window menu and event-handler packet decoders now use a shared
named codec boundary. Dispatcher paths consume typed menu IDs, states, and
handler pointers instead of repeating packet offsets. `ApplicationMenuStateRoot`
is **44,580 / 50,680 / 47,696 bytes** and `WindowEventHandlerRoot` is
**44,484 / 50,608 / 47,672 bytes** for MC68000/020/040; both are zero-runtime
clean. MC68000 returns **42** after **205,142 / 2,154,586** and
**130,973 / 1,373,930** instructions/cycles respectively. Remaining MorphOS
Application/Window behavior and ABI coverage remain progressive work.

Group InitChange, ExitChange, and ExitChange2 packet dispatch now uses one
central guest-memory codec. Fixed packed ABI access is confined to that codec,
while the qualification root verifies valid methods and truncated rejection.
`GroupChangePacketsRoot` is **1,964 / 1,964 / 1,960 bytes** for
MC68000/020/040, returns **42** after **625 instructions / 6,488 cycles**, and
has zero-runtime maps. Remaining MorphOS Group behavior and ABI coverage remain
progressive work.

Fixed Group change and ordering packets now use central named codecs.
MoveMember, Reorder, and Sort qualification seams accept struct-shaped inputs,
and packet consumers no longer repeat field offsets. `GroupOrderingRecordRoot`
is **3,736 / 3,700 / 3,704 bytes** for MC68000/020/040, returns **42** after
**907 instructions / 9,664 cycles**, and has zero-runtime maps. Remaining
MorphOS Group behavior and ABI coverage remain progressive work.

The Group change-bracket sidecar is a named 16-byte state record with one
central codec for depth, exit flags, and exit-request telemetry. The
qualification seam accepts a struct-shaped input and rejects unmapped state
without exposing offsets. `GroupChangeStateRecordRoot` is **1,812 / 1,796 /
1,796 bytes** for MC68000/020/040, returns **42** after **441 instructions /
4,740 cycles**, and has zero-runtime maps. Remaining MorphOS Group behavior
and ABI coverage remain progressive work.

Group forwarding and ChildList sidecar state now use central codecs with
struct-shaped public qualification inputs. The 16-byte forward record
normalizes boolean flags at the boundary, and the 32-byte ChildList record
rejects invalid capacity before publication. `GroupStateRecordRoot` is
**3,584 / 3,628 / 3,628 bytes** for MC68000/020/040, returns **42** after
**1,576 instructions / 17,540 cycles**, and has zero-runtime maps. Remaining
MorphOS Group behavior and ABI coverage remain progressive work.

The Process/Slave specialist sidecar now uses a named 52-byte fixed-width
record and central codec for class, Process state, task token, owned Name,
error/signals, flags, Slave setup/dispatch depth, and notification telemetry.
Production consumers use those named fields rather than repeating offsets;
the qualification seam accepts a struct-shaped input to keep the 68k call
boundary register-safe. `ProcessSpecialistRecordRoot` is **2,720 / 2,872 /
2,872 bytes** for MC68000/020/040, returns **42** after **934 instructions /
11,012 cycles**, and has zero-runtime maps. The broader
`ProcessSpecialistRoot` also returns **42** after **604,551 instructions /
6,347,470 cycles**. Remaining MorphOS Process/Slave behavior and ABI coverage
remain progressive work.

Application/Window queue nodes, input-handler nodes, event-handler nodes, and
SetConfigItem state now use named fixed-width records and central codecs.
Push/return/cycle/event-handler list traversal and cleanup use typed fields;
inline packet placement is retained only as the explicit packet-member ABI
boundary. `ApplicationWindowRecordRoot` is **4,052 / 4,160 / 4,160 bytes** for
MC68000/020/040, returns **42** after **1,097 instructions / 12,522 cycles**,
and has zero-runtime maps. Existing queue and event-handler closures also
return **42** after **287,570 instructions / 3,019,090 cycles** and
**130,890 instructions / 1,372,974 cycles**. Remaining MorphOS Application
behavior and ABI coverage remain progressive work.

The Area/Group layout boundary now writes the six signed 16-bit `MUI_MinMax`
result fields through a named codec, while Area drawing/text render-port lookup
reuses the named 28-byte `MUI_RenderInfo` codec. `AreaLayoutRecordRoot` is
**3,968 / 4,036 / 4,036 bytes** for MC68000/020/040, returns **42** after
**1,072 instructions / 12,190 cycles**, and has zero-runtime maps. Remaining
MorphOS layout behavior and ABI coverage remain progressive work.

Group ChildList projection entries now use a central named 16-byte codec, and
the embedded Exec List header is written through the SDK's typed list boundary.
`NextObject` and projection construction no longer repeat child-entry offsets.
`GroupChildListRoot` is **3,100 / 3,088 / 3,088 bytes** for MC68000/020/040,
returns **42** after **1,117 instructions / 12,018 cycles**, and has
zero-runtime maps. Remaining MorphOS Group behavior and ABI coverage remain
progressive work.

Group grid specifications and ActivePage state now use central named-record
codecs. `GroupGridRecordRoot` is **2,028 / 2,060 / 2,056 bytes** and
`GroupPageRecordRoot` is **1,748 / 1,736 / 1,736 bytes** for MC68000/020/040;
both return **42** with zero-runtime maps. Remaining MorphOS Group behavior
and ABI coverage remain progressive work.

The drawing service uses named fixed-width records and central codecs for its
20-byte state, clip and refresh nodes, pen leases, `MUI_RenderInfo`, and the
RastPort layer view. Clipping, refresh, pen, and layer traversal use typed
fields instead of repeated offsets. `DrawingServiceRecordRoot` is **4,952 /
4,908 / 4,908 bytes** for MC68000/020/040, returns **42** on MC68000 after
**1,301 instructions / 13,920 cycles**, and has zero-runtime maps. The broader
`DrawingServiceRoot` closure also returns **42** after **12,120 instructions /
123,028 cycles**. Remaining MorphOS drawing behavior and ABI coverage remain
progressive work.

The synchronous requester service uses a named 8-byte state struct and central
codec for magic and generation. `MUI_RequestA` and `MUI_RequestObjectA` use
those fields for readiness validation. `RequesterServiceRecordRoot` is
**1,512 / 1,500 / 1,500 bytes** for MC68000/020/040, returns **42** on MC68000
after **258 instructions / 2,628 cycles**, and has zero-runtime maps. Remaining
MorphOS requester behavior and ABI coverage remain progressive work.

The error service uses a named 16-byte state struct and central codec for
magic, version, error value, and sequence. `MUI_Error` and `MUI_SetError` use
those fields instead of repeated record offsets. `ErrorServiceRecordRoot` is
**1,680 / 1,668 / 1,668 bytes** for MC68000/020/040, returns **42** on MC68000
after **342 instructions / 3,664 cycles**, and has zero-runtime maps. Remaining
MorphOS error-service behavior and ABI coverage remain progressive work.

The ASL service uses named fixed-width structs and central codecs for its
12-byte service state and 16-byte requester lease. Allocation, request,
lease-list traversal, linking, and release use typed fields rather than
repeated record offsets. `AslServiceRecordRoot` is **2,684 / 2,664 / 2,664
bytes** for MC68000/020/040, returns **42** on MC68000 after **622
instructions / 6,596 cycles**, and has zero-runtime maps. Remaining MorphOS
ASL behavior and ABI coverage remain progressive work.

The class-service state, lease, and `MUI_CustomClass` blocks are represented by
named fixed-width structs and central codecs. Class-service initialization,
lookup, reference/object lease accounting, custom-class lifecycle, and
unlinking use those fields instead of repeated record offsets.
`ClassServiceRecordRoot` is **4,592 / 4,624 / 4,628 bytes** for
MC68000/020/040, returns **42** on MC68000 after **1,440 instructions / 15,900
cycles**, and has zero-runtime maps. Remaining MorphOS behavioral parity and
ABI coverage remain progressive work.

All headless-state consumers now use the named 32-byte state record for class
and object registry heads, lifecycle teardown, notification depth/sequence,
Group mutation snapshots, specialist class lookup, and class-service
validation. `HeadlessStatePacketRoot` is **2,000 / 2,028 / 2,028 bytes** for
MC68000/020/040, returns **42** on MC68000 after **765 instructions / 8,328
cycles**, and has zero-runtime maps. Remaining MorphOS behavioral parity and
ABI coverage remain progressive work.

The fixed 32-byte notification header is represented by named
`Next`/`Sequence`/`TriggerAttribute`/`TriggerValue`/`Destination`/
`FollowCount`/`Flags`/`Reserved` fields and a central codec. Notify allocation,
traversal, dispatch, cleanup, and trailing payload placement use those fields.
`NotificationRecordRoot` is **2,000 / 2,028 / 2,028 bytes** for MC68000/020/
040, returns **42** on MC68000 after **557 instructions / 6,170 cycles**, and
has zero-runtime maps. Remaining MorphOS behavioral parity and ABI coverage
remain progressive work.

The fixed 24-byte Store/Dataspace record is represented by named
`Next`/`Key`/`Data`/`Length`/`Flags`/`Generation` fields and a central codec.
Store lifecycle operations and Dataspace IFF entry decoding use those fields.
`StoreRecordRoot` is **1,848 / 1,832 / 1,836 bytes** for MC68000/020/040,
returns **42** on MC68000 after **432 instructions / 4,726 cycles**, and has
zero-runtime maps. Remaining MorphOS behavioral parity and ABI coverage remain
progressive work.

The fixed 16-byte Family child-list node is represented by a named
`Next`/`Previous`/`Object`/`Owner` record and central codec. Live Family
topology, `MUIM_Family_DoChildMethods`, selector/mutation projections, and
collection teardown use those fields. `ChildRecordRoot` is **1,684 / 1,672 /
1,676 bytes** for MC68000/020/040, returns **42** on MC68000 after **349
instructions / 3,754 cycles**, and has zero-runtime maps. Remaining MorphOS
behavioral parity and ABI coverage remain progressive work.

The fixed 16-byte attribute node is represented by a named record and codec.
HeadlessObjectCore, Group child/page state, and Stringscroll attribute paths
use those fields. `AttributeRecordRoot` is **1,692 / 1,680 / 1,680 bytes** for
MC68000/020/040, returns **42** on MC68000 after **346 instructions / 3,682
cycles**, and has zero-runtime maps. Remaining non-object ABI records and full
MorphOS differential parity remain progressive work.

The fixed 64-byte headless object record is represented by a named struct and
central codec. Creation, disposal, lookup, object/attribute access, link
updates, list unlinking, and attribute cleanup use those fields. The focused
`HeadlessObjectPacketRoot` is **3,208 / 3,396 / 3,396 bytes** for
MC68000/020/040, returns **42** on MC68000 after **2,358 instructions /
26,470 cycles**, and has zero-runtime maps. Remaining MorphOS behavioral
parity and ABI coverage are still progressive work.

Family topology now consumes the named object codec for parent, child-head,
child-tail, and BOOPSI links across add/remove/get/reorder/transfer and
dispose-time cleanup. `MUIM_Family_DoChildMethods` uses the same typed walk.
`FamilyDoChildMethodsRoot` is **2,044 / 2,112 / 2,036 bytes** for
MC68000/020/040, returns **42** on MC68000 after **966 instructions / 10,024
cycles**, and has zero-runtime maps. Remaining MorphOS behavioral parity and
ABI coverage remain progressive work.

The fixed `MUIM_Family_DoChildMethods` envelope now crosses the named
`MuiFamilyDoChildMethodsMessageCodec`; the live forwarding path consumes the
same central mapping check. `FamilyDoChildMethodsMessageCodecRoot` produces
**1,084 / 1,080 / 1,080 bytes** for MC68000/020/040 with 7 reachable methods,
zero relocations, zero framework members, and zero managed allocation sites;
MC68000 returns **42** after **217 instructions / 2,154 cycles**. The broader
Family forwarding root remains separately qualified at **1,236 instructions /
13,524 cycles**.

The fixed Family AddHead/AddTail/Remove, Insert, and Transfer packets now cross
`MuiFamilyMutationMessageCodec` into named child, predecessor, and family
fields. `FamilyMutationMessageCodecRoot` produces **2,512 / 2,528 / 2,528
bytes** for MC68000/020/040 with 11 reachable methods, zero relocations, zero
framework members, and zero managed allocation sites; MC68000 returns **42**
after **730 instructions / 7,594 cycles**. Reorder and Sort remain covered by
the broader Family packet root, with trailing object vectors treated as the
explicit array ABI boundary.

Group child forwarding/list projection, ActivePage state, and collection
teardown now use the named object codec for BOOPSI, attribute-head, and
child-head links. The focused Group page/forward seams are **1,152 / 1,152
bytes** on MC68000, return **42** after **282 / 3,192** and **282 / 3,176
instructions / cycles**, and have zero-runtime maps. Remaining MorphOS
behavioral parity and ABI coverage remain progressive work.

Notify parent/BOOPSI resolution and notification-list heads, plus semaphore
owner/depth/shared state, now use named fields in the object codec.
`NotifyWritePacketsRoot` is **2,260 bytes** on MC68000; the focused
`SemaphoreObjectRecordRoot` is **2,268 / 2,360 / 2,360 bytes** for
MC68000/020/040, returns **42** on MC68000 after **838 instructions / 9,402
cycles**, and has zero-runtime maps. Remaining MorphOS behavioral parity and
ABI coverage remain progressive work.

Store/Dataspace ownership uses the named object codec for the `Stores` head
across add/resize/merge/find/iterate/remove/clear, objectmap cleanup, and
teardown. `StoreObjectRecordRoot` is **2,272 / 2,316 / 2,316 bytes** for
MC68000/020/040, returns **42** on MC68000 after **800 instructions / 9,070
cycles**, and has zero-runtime maps. Remaining MorphOS behavioral parity and
ABI coverage remain progressive work.

The audited specialist consumers—CommonControl, List, Listtree, menu,
Stringscroll, and MUI lifecycle classification—now use named object fields.
No production source outside the codec boundary retains a
`MuiHeadlessLayout.Object…` read/write. `CommonControlClassRecordRoot` remains
**3,520 / 3,640 / 3,540 bytes** for MC68000/020/040, returns **42** on
MC68000 after **1,638 instructions / 15,244 cycles**, and has zero-runtime
maps. Full MorphOS differential parity remains open.

The headless class registry uses a named 28-byte record for its typed pointers,
instance size, flags, and object count. Registration, lookup, deletion, and
object accounting use the central codec; `HeadlessClassPacketRoot` is
**2,156 / 2,184 / 2,184 bytes** for MC68000/020/040 and returns **42** on
MC68000 with zero-runtime maps.

Common-control class classification and Group superclass traversal also consume
that codec. `CommonControlClassRecordRoot` is **3,520 / 3,640 / 3,540 bytes**
for MC68000/020/040 and returns **42** on MC68000 with zero-runtime maps.

The audited List, Listview, Listtree, menu, misc, process, and object-persistence
class probes use the same codec; those sources no longer duplicate the class
registry field offsets.

The shared headless guest-state header is represented by a named 32-byte
record. Initialization, Ensure, sequence allocation, and mutation tracking use
the central codec; `HeadlessStatePacketRoot` is **2,000 / 2,028 / 2,028 bytes**
for MC68000/020/040 and returns **42** on MC68000 with zero-runtime maps. The
dataspace iteration cursor remains an opaque scalar because it is not a public
record.

The latest MG09 collection slice keeps Listview and Stringscroll surface
dispatch struct-first: named records cover layout, draw, min/max,
set/no-notify-set, and Floattext append, with Listview list methods forwarding
through the same decoded records. Host/source/CIL coverage is **540/540**.
`CollectionCompositePacketsRoot` is **2,488 / 2,516 / 2,524 bytes** for
MC68000/020/040, returns **42** after **593 instructions / 6,302 cycles** on
MC68000, and has zero relocations and zero-runtime map fields.

Application settings Save/Load now uses named fixed-width header and key/length
records, and the persistence boundary consumes a named dataspace store-record
view. `ApplicationSettingsPacketRoot` is **2,444 / 2,432 / 2,428 bytes** for
MC68000/020/040, returns **42** after **622 instructions / 6,362 cycles** on
MC68000, and remains zero-runtime clean. The iteration cursor is intentionally
kept as an opaque scalar because it is not a public record.

The `MUI_Layout` dispatcher now uses named records for AskMinMax, Relayout,
DrawBackground, Backfill, and Text; existing Layout, Draw, Setup, and
text-dimension packets remain typed as well. `LayoutSurfacePacketsRoot` and
`LayoutTextPacketRoot` are **1,792/1,828/1,848** and **1,096/1,104/1,116**
bytes for MC68000/020/040 and return **42** on MC68000 with zero-runtime maps.

The common-control dispatcher now decodes payload-bearing numeric, prop, event,
attribute, geometry, draw, and setup packets into named structs. Numeric
default and Cleanup are zero-payload method-ID operations. Focused packet roots
cover these groups with MC68000/020/040 artifacts of **1,584/1,584/1,592**,
**1,556/1,560/1,560**, **940/932/936**, **1,032/1,020/1,024**, and
**1,148/1,140/1,140** bytes, all zero-runtime clean and returning **42** on
MC68000.

The production project is `CopperOS.MuiMaster.csproj`. MG02 foundations include
value-type platform capability contracts, fixed-width guest-resident state,
CopperOS development resident metadata, and the exact public vector router.
MG03 supplies the shared CopperStart Intuition BOOPSI runtime consumed through
`IMuiBoopsiCapability`; CopperOS does not maintain a second object system. MG04
qualifies the headless master lifecycle, class/object ownership, exact-message
dispatcher, Family, Notify, Dataspace, Datamap, Objectmap, and Semaphore core.
MG05 qualifies Area/Group geometry, Balance, Register, Selectgroup, Scrollgroup,
Virtgroup, redraw scheduling, and neutral rendering through explicit Graphics/
Layers capabilities. MG06 qualifies scheduler-driven Application/Window
ownership, signals, pushed methods, handlers, event polling, focus, menus,
iconification, and requesters. MG07 common controls are now active. Host/static/
CIL gates live in
`tests/MuiMaster`; the self-contained CopperSharp input closure is
`tests/MuiMaster.NativeRoot`, and `tests/MuiMaster.NativeExecution` executes its
MC68000 HUNK under Copper68k. Run the MC68000/020/040 compile/map gates and the
MC68000 return-value smoke test with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\MuiMaster\qualify_native.ps1
```

The latest MG09 Datamap/Objectmap packet slice replaces raw dispatcher field
reads with named fixed-width records for Set, Find, Get, Iterate,
IterationKey, Remove, and Clear. Host/source/CIL coverage is **540/540**.
`StorePacketsRoot` is **5,620 / 5,636 / 5,628 bytes** for MC68000/020/040,
returns **42** after **4,152 instructions / 43,608 cycles** on MC68000, and
has zero relocations and zero-runtime map fields. Full MorphOS differential
parity remains separate progressive work.

The latest MG09 Group change packet slice replaces the remaining raw header
reads for `MUIM_Group_InitChange`, `MUIM_Group_ExitChange`, and
`MUIM_Group_ExitChange2` with named 4-byte/8-byte records. The live path keeps
the existing nested bracket state in guest memory. Host/source/CIL coverage is
**540/540**; `GroupChangePacketsRoot` is **2,140 / 2,132 / 2,128 bytes** for
MC68000/020/040 and returns **42** after **792 instructions / 7,962 cycles** on
MC68000, with zero relocations and zero-runtime map fields.

The latest MG09 GetConfigItem slice uses a named 12-byte
`{MethodID, ConfigId, Storage}` packet for both broad and focused dispatch.
Host/source/CIL coverage is **540/540**. `GetConfigItemPacketsRoot` is
**1,384 / 1,376 / 1,376 bytes** for MC68000/020/040, returns **42** after
**410 instructions / 4,206 cycles** on MC68000, and has zero relocations and
zero-runtime map fields.

The latest MG09 persistence packet slice uses a named 8-byte
`{MethodID, Dataspace}` record for both `MUIM_Export` and `MUIM_Import`.
Host/source/CIL coverage is **540/540**. `ObjectPersistencePacketsRoot` is
**1,792 / 1,780 / 1,780 bytes** for MC68000/020/040, returns **42** after
**705 instructions / 7,286 cycles** on MC68000, and has zero relocations and
zero-runtime map fields.

The fixed Export/Import envelope is also qualified independently through the
named `MuiObjectPersistenceMessage` and central
`MuiObjectPersistenceMessageCodec`. `ObjectPersistenceMessageCodecRoot`
produces **2,328 / 2,316 / 2,316 bytes** for MC68000/020/040, with 13
reachable methods, zero relocations, zero framework members, and zero managed
allocation sites; MC68000 execution returns **42** after **757 instructions /
7,776 cycles**. The root exercises both methods and a truncated-record
rejection. Class-specific persistence payload semantics remain progressive.

The latest MG09 packet slice adds `MUIM_UpdateConfig` as a complete
struct-first 332-byte record: named header, 64 redraw-object pointers, and 64
redraw flags. The codec validates the bounded count and packet mapping; live
preference propagation and redraw scheduling remain separate capability work.

The latest MG09 Family packet slice adds named 8-byte records for
`MUIM_Family_AddHead` (`0x8042E200`), `MUIM_Family_AddTail` (`0x8042D752`),
and `MUIM_Family_Remove` (`0x8042F8A9`), plus a named 12-byte record for
`MUIM_Family_Insert` (`0x80424D34`). These methods route through the live
Family ownership path. `FamilyChildPacketsRoot` qualifies the corresponding
guest head/tail/remove/insert/transfer/reorder/sort projection at **9,364 /
9,988 / 9,232 bytes** for MC68000/020/040; MC68000 returns **42** after
**13,832 instructions / 132,142 cycles**, with zero-runtime map fields.

The Family packet increment adds `MUIM_Family_DoChildMethods` as a named
4-byte record. Its live path forwards the same message to every direct child
through `IMuiBoopsiCapability`, snapshots the next link before each call, and
uses a bounded guest-resident traversal with no managed collection.

The Notify callback increment adds `MUIM_CallHook` as a named 12-byte packet.
Its fixed fields are decoded without exceptions or managed allocation, and the
existing callback capability receives A0=hook, A2=object, and A1=&param1 so
additional guest parameters remain caller-owned.

The latest MG09 Notify increment adds typed `MUIM_SetAsString` support. Its
16-byte guest header and trailing ULONG vector are decoded centrally; the live
path accepts at most eight arguments, uses the freestanding bounded formatter,
caps output at 1,024 characters, and stores an owned guest copy before setting
the requested attribute. Host/source/CIL coverage is **532/532**. The focused
`SetAsStringPacketsRoot` artifacts are **1,420 / 1,412 / 1,412 bytes** for
MC68000/020/040, with zero relocations and zero-runtime map fields; MC68000
returns **42** after **361 instructions / 3,766 cycles**. Unsupported
conversions remain separate progressive work.

The latest MG09 BOOPSI increment adds the complete typed `MUIM_BoopsiQuery`
(`0x80427157`) packet for the SDK `MUIP_BoopsiQuery` alias. Its named 40-byte
record carries screen, flags, min/max dimensions, default dimensions, and
render-info fields. Host/source/CIL coverage is **533/533**. The focused
`BoopsiQueryPacketsRoot` artifacts are **1,756 / 1,804 / 1,804 bytes** for
MC68000/020/040, with zero relocations and zero-runtime map fields; MC68000
returns **42** after **851 instructions / 9,040 cycles**. The fixed ABI record
is qualified; external BOOPSI callback semantics remain separate progressive
work.

The latest MG09 Notify increment adds named packed records for
`MUIM_WriteLong` and `MUIM_WriteString`. The live dispatcher validates mapped
guest destinations and performs bounded ULONG/string writes without
exceptions, managed allocation, or managed runtime services. Host/source/CIL
coverage is **531/531**. The focused `NotifyWritePacketsRoot` artifacts are
**2,260 / 2,248 / 2,248 bytes** for MC68000/020/040, with zero relocations and
zero-runtime map fields; MC68000 returns **42** after **821 instructions /
8,590 cycles**. The fixed `WriteLong`/`WriteString` packet boundary is
qualified; unsupported Notify formatting remains separate progressive work.

The latest MG09 Dataspace increment replaces raw dispatcher offsets with
named packed guest records for `Add`, `Find`, `Get`, `Merge`, `Remove`, and
`Clear`. `MuiDataspaceMessageCore` owns the codecs and packet writers; the
focused `DataspacePacketsRoot` covers the six forms and truncated-packet
rejection. Host/source/CIL coverage is **529/529**. MC68000/020/040 artifacts
are **4,132 / 4,148 / 4,152 bytes**, with zero relocations, framework features,
managed allocation sites, and runtime type descriptors; MC68000 returns **42**
after **2,075 instructions / 21,966 cycles**.

The latest MG09 Dataspace IFF increment adds typed `ReadIFF`/`WriteIFF`
packets and the separate `IMuiIffCapability` chunk-I/O seam. The live bridge
serializes big-endian `{id,length,data}` records, retries short transfers, and
explicitly frees temporary guest buffers; it never substitutes a DOS handle or
managed stream. Host/source/CIL is **530/530**. The focused
`DataspaceIffPacketsRoot` artifacts are **2,268 / 2,256 / 2,256 bytes** for
MC68000/020/040, with zero relocations and zero-runtime fields; MC68000
returns **42** after **842 instructions / 8,680 cycles**. See the
[MorphOS MUI Dataspace documentation](https://morphos-team.net/sdk/MUI/MUI_Dataspace.html).

The latest MG09 Family increment adds the typed `MUIM_Family_GetChild`
(`0x8042c556`) packet with named `MethodID`, `nr`, and `ref` fields. The live
dispatcher supports First, Last, Next, Previous, and Iterate selectors over
the Family topology; the focused native seam uses named child projection
records. Host/source/CIL coverage is **528/528**. MC68000/020/040 artifacts
are **2,940 / 3,124 / 2,920 bytes**, with zero relocations, framework
features, managed allocation sites, and runtime type descriptors; MC68000
returns **42** after **3,052 instructions / 31,874 cycles**. The MorphOS
Family page marks this method undocumented, so complete differential parity
remains open. See the
[MorphOS MUI Family documentation](https://morphos-team.net/sdk/MUI/MUI_Family.html).

The fixed `MUIM_Family_GetChild` record now crosses the central
`MuiFamilyGetChildMessageCodec`; consumers use named `MethodId`, `Number`, and
`Reference` fields while packed offsets remain isolated to the codec.
`FamilyGetChildMessageCodecRoot` produces **1,356 / 1,372 / 1,372 bytes** for
MC68000/020/040, with 7 reachable methods, zero relocations, zero framework
members, and zero managed allocation sites; MC68000 returns **42** after
**357 instructions / 3,812 cycles**. The focused root also rejects a
truncated packet. Selector/topology behavior remains covered by the existing
Family root.

MG08 is qualified. The current List slices include bounded guest-resident storage,
native-safe StringArray construct/display/compare/destruct behavior,
MorphOS Active/First/Quiet normalization, Select-All/Ask counting, a bounded
Format/MaxColumns column model whose non-`px` width limits use MorphOS
percentage semantics, guest-resident integer `{offset,width}` column geometry,
bounded `MUIM_List_TestPos` row/column/flag/offset hit-testing, and
bounded `MUIM_List_CreateImage`/`MUIM_List_DeleteImage` opaque guest-handle
ownership, plus non-recursive List/Listview/Floattext row geometry publication
and bounded per-cell visible-row rendering through the existing
render-info/graphics seam; this is not full visual parity. Routing is
non-recursive through the layout dispatcher. Dirlist and Volumelist now add bounded
capability-backed scans, owned records, filters, counters, sorting, failure
handling, and reread/mutator dispatch. Stringscroll now has a bounded
guest-owned string, content-driven bar visibility, min/max metrics, clamped
pixel scrolling, input policy, CRLF-safe clipped drawing, and packet routing.
Listview now publishes the owned List viewport, clamps Prop-like first-row
movement, binds the owned child to the shared render-info seam, draws visible
child rows, and draws a neutral overflow track/thumb through the existing
graphics seams.
Listtree.mcc is implemented as a standalone external component with fixed
guest-resident topology and native closure qualification; it is deliberately
not classified as a built-in `.mui` collection.
The fixed MorphOS 3.20 List edit family (`CreateEditObject`, `Edit`, `EditDone`,
and `EndEdit`) now crosses `MuiCollectionEditMessageCodec`. Production
consumers use named signed row/column and guest-pointer fields, while the
codec owns the packed guest boundary. `CollectionListEditMessageCodecRoot`
produces **3,164 / 3,196 / 3,212 bytes** for MC68000/020/040 with 13 reachable
methods, zero relocations, zero framework members, zero managed allocation
sites, and zero runtime type descriptors; MC68000 returns **42** after
**1,059 instructions / 11,088 cycles**. The existing guest-resident editor
state machine remains separately covered, and truncated `EditDone` packets are
rejected.
The fixed List Construct/Destruct, Display, Compare, and TestPos records now
use the same struct-first approach through `MuiCollectionRecordMessageCodec`.
`CollectionListRecordMessageCodecRoot` produces **3,380 / 3,428 / 3,436 bytes**
for MC68000/020/040 with 13 reachable methods and zero-runtime maps; MC68000
returns **42** after **1,340 instructions / 13,978 cycles**. Truncated TestPos
records are rejected, while variable hook payloads remain an explicit ABI
boundary.
The latest MG09 increment adds a typed CopperStart Intuition
`BoopsiNextObjectProjectionEntry` record/codec shared with the Group
ChildList projection. The `amiga.intuition.NextObject` export and vector
router now consume `GENT` entries using the caller-owned cursor; ordinary
BOOPSI objects use the typed `_Object` header codec. CopperStart Intuition
tests are **21/21**, and its freestanding native export closure passes on
MC68000/020/040 (**3/3**). Manifest admission and runtime execution
qualification remain open.
The latest MG09 Group slice adds the typed `MUIA_Group_LayoutHook` bridge.
MinMax and Layout callbacks receive the SDK `MUI_LayoutMsg` struct in guest
memory, including the read-only ChildList pointer and Group context; successful
Layout hooks can adjust virtual-group dimensions and bypass built-in child
layout. Host/source/CIL coverage is **527/527**. `GroupLayoutHookRoot` is
zero-runtime clean on MC68000/020/040 (**6,276 / 6,700 / 6,424 bytes**) and
returns **42** after **5,886 instructions / 59,976 cycles** on MC68000. This
qualifies the typed/native bridge seam; complete MorphOS callback/differential
parity remains open. See the
[MorphOS MUI Group documentation](https://morphos-team.net/sdk/MUI/MUI_Group.html).
The latest MG09 Group slice adds a typed, read-only `MUIA_Group_ChildList`
projection: an `Amiga.List` header and bounded child-entry records are rebuilt
from the Family topology after mutation and released with the Group. The local
`MuiGroupChildrenCore.NextObject` seam advances a guest cursor and returns
child BOOPSI pointers without managed enumeration. Host/source/CIL coverage is
**526/526**; `GroupChildListRoot` is zero-runtime clean on MC68000/020/040
(1,980 / 1,972 / 1,972 bytes), returning **42** after **736 instructions /
7,750 cycles** on MC68000. The CopperStart bridge is compile-closed; manifest
admission and runtime execution remain follow-up ABI tasks, so external
NextObject compatibility is not yet claimed complete. See the
[MorphOS MUI Group documentation](https://morphos-team.net/sdk/MUI/MUI_Group.html)
and [intuition `NextObject` documentation](https://morphos-team.net/sdk/intuition.html).
The preceding MG09 Group slice adds typed `MuiGroupForwardState` handling for
MorphOS `MUIA_Group_Child`, `MUIA_Group_ChildCount`, and
`MUIA_Group_Forward`. Child construction tags adopt non-null objects through
the Family seam and NULL tags fail atomically. Forward routes subsequent
attribute sets to direct children; `ForwardDepth` enables bounded descendant
propagation. Host/source/CIL coverage is **524/524**. The focused
`GroupForwardRecordRoot` is zero-runtime clean on MC68000/020/040
(1,136 / 1,124 / 1,128 bytes), returning **42** after **278 instructions /
3,128 cycles** on MC68000. The typed `LayoutHook` bridge is now present;
complete callback differential qualification remains open. See the
[MorphOS MUI Group documentation](https://morphos-team.net/sdk/MUI/MUI_Group.html).

The preceding MG09 Group slice adds typed `MuiGroupPageState` handling for
MorphOS `MUIA_Group_ActivePage`. First/Last/Prev/Next/Advance selectors are
normalized to a canonical child index, page-mode layout consumes that index,
invalid direct values are rejected, and empty Groups retain a raw selector
until children exist. Host/source/CIL coverage is **520/520**. The focused
`GroupPageRecordRoot` is zero-runtime clean on MC68000/020/040
(1,136 / 1,120 / 1,132 bytes), returning **42** after **278 instructions /
3,144 cycles** on MC68000. See the
[MorphOS MUI Group documentation](https://morphos-team.net/sdk/MUI/MUI_Group.html).

The preceding MG09 Group slice adds typed `MuiGroupGridSpec` handling for
MorphOS `Columns`, `Rows`, horizontal/vertical spacing, same-size, and
center-alignment attributes. Row-only and column-only configurations derive
the missing axis; bounded cells reuse Area min/max records and the existing
layout path without managed collections. Host/source/CIL coverage is
**518/518**, and `GroupGridRecordRoot` is zero-runtime clean on MC68000/020/040
(1,164 / 1,168 / 1,176 bytes), returning **42** after **416 instructions /
4,640 cycles** on MC68000. See the
[MorphOS MUI Group documentation](https://morphos-team.net/sdk/MUI/MUI_Group.html).

The current MG09 window packet slices add `MUIA_Window_ActiveObject` None/
Next/Prev and geometry-aware Left/Right/Up/Down selection, obsolete
`MUIM_Window_SetCycleChain`,
`MUIM_Window_Snapshot`, and `MUIM_Window_ScreenToBack`/
`MUIM_Window_ScreenToFront`. Screen-depth operations require an opened window
and use the explicit `MoveMuiScreen` capability. Spatial selection is bounded
to the copied cycle chain and Area edge attributes; its deterministic ranking
is an explicit public-ABI policy because private MorphOS tie-breaking is not
documented. Exact MorphOS settings-file encoding remains intentionally pending.
The Notify-class `MUIM_GetConfigItem` packet now supports the documented
`MUICFG_PublicScreen` item (`0x24`). It validates live-object and caller-owned
storage boundaries before publishing the opaque screen value through the
native-safe `GetMuiConfigItem` capability; unsupported items fail without a
write or capability call.
The remaining public Notify UserData methods are also routed:
`MUIM_FindUData` returns the first matching object in preorder,
`MUIM_GetUData` reads the requested attribute from that object,
`MUIM_SetUData` updates every matching object, and `MUIM_SetUDataOnce` stops
after the first. Their fixed packet records and traversal frames remain
guest-safe and allocation-free from the managed-runtime perspective.
The first MG09 service slice now provides the MorphOS-shaped `MUI_Layout`
scalar and guest-packet entry points. It routes existing native-safe collection
and Radio layout cores, publishes bounded Area geometry, and now reaches the
native-safe three-child Scrollbar composite layout. The custom/external class
gateway also has a native-safe builtin/custom lifecycle seam covering
`GetClass`/`FreeClass`, public/private custom classes, A6 binding, and deletion
guards. The loader-backed external path is now separately native-qualified for
the deterministic `mui/Foo.mcc` fixture; the broader third-party class
inventory remains open. These are progressive slices. The requester/ASL service
now adds a guest-resident lease boundary for
`AllocAslRequest`/`AslRequest`/`FreeAslRequest`, and the synchronous requester
service implements `RequestA`/`RequestObjectA` with balanced object retention
around the modal call. The ASL boundary now validates guest TagItem control
semantics (`TAG_DONE`, `TAG_IGNORE`, `TAG_MORE`, and `TAG_SKIP`) without
copying the caller's list. Synchronous requester calls now validate bounded,
caller-owned title, gadget, and format strings, measure `|`-separated gadget
alternatives, count printf-style conversions, and verify the mapped ULONG
parameter vector. The bounded requester formatter now executes integer,
string, character, binary/hexadecimal, width/precision, and literal-percent
conversions into a temporary guest C string before the synchronous platform
call; unsupported conversions remain an explicit failure. Host-native requester
UI remains deferred. The drawing service also provides strict-LIFO clipping and
clip-region stacks, balanced refresh flags, and full-token pen acquisition/
release through explicit region/pen capabilities. Public `MUI_Redraw` now validates the
guest object registry and draw intent bits before entering the native redraw
seam. Public `MUI_NewObjectA` now performs case-sensitive builtin-class lookup,
bounded TagItem validation, and guest object construction. The class-service
factory path additionally acquires external classes through the bounded
`mui/<classid>` loader seam and holds that lease in guest state until
`DisposeObjectWithClassService` removes the object. The public disposal
boundary now rejects unknown or already-disposed objects and routes the
class-service form through the same lease-aware release. The public error service now
provides guest-resident
`MUI_Error`/`MUI_SetError` state with previous-value returns and no managed
runtime state. Public IDCMP routing now retains requested event masks across
window open/close and updates the native event configuration while open. The
overall MorphOS compatibility claim is still withheld.

The bounded public `MUI_MakeObjectA` route now constructs the documented object
families (`MUIO_Label`, `MUIO_Button`, `MUIO_Checkmark`, `MUIO_Cycle`,
`MUIO_Radio`, `MUIO_Slider`, `MUIO_String`, `MUIO_PopButton`,
`MUIO_NumericButton`, spacing, bars, and bar titles) plus the MorphOS-shaped
`MUIO_MenustripNM` and `MUIO_Menuitem` families from type-specific ULONG
vectors. Menu-family specialist state is attached during construction, so menu
methods are available immediately and generic disposal releases sidecars
recursively. Cycle and Radio vectors are validated bounded guest pointer tables,
PopButton image specs accept builtin IDs or bounded C strings, and NumericButton
validates and copies its optional format string through the numeric ownership
path. The menu route parses bounded guest `NewMenu` records into owned
`Menustrip.mui`/`Menu.mui`/`Menuitem.mui` trees with title, item, sub-item,
separator, shortcut, userdata, mutual-exclusion, CheckIt/Checked/MenuToggle,
command-string, enabled, and menu-disabled behavior. All temporary class/tag
records are guest-resident and disposed before returning; menu trees are
recursively disposed. `MUIO_Menuitem_CopyStrings` now enables failure-atomic
copies of the direct item's title and shortcut; image menu entries and other
not-yet-qualified MUIO constructors remain explicitly unsupported.

The public `MUI_NewObjectA` direct and class-service factories now invoke the
class-aware common-control construction normalization after raw TagItems and
before specialist adoption. Numeric, String, Image, Prop, Gauge, and related
families therefore receive their bounded defaults, clamping, and guest-owned
payload copies through the same factory boundary; unknown/custom classes remain
on the generic path.

MorphOS 3.20 `String.mui` also exposes a bounded scroll-metric slice: pixel
`ScrollWidth`/`ScrollHeight`, laid-out visible dimensions, and clamped
`ScrollLeft`/`ScrollTop` offsets are available through common-control getters
and `OM_GET`/`OM_SET` packets. The first implementation uses a deterministic
8x10 character-cell metric and does not claim full font, UTF-8, multiline, or
Prop-binding parity.

The MG09 menu classes `Menustrip.mui`, `Menu.mui` and `Menuitem.mui` (all
`Family.mui` subclasses) now have an additive specialist family
(`MenuSpecialistCore` / `MenuSpecialistDispatcher` / `MenuSpecialistLifecycle`).
It is layered over real headless objects, and the public headless dispatcher
gives it one non-recursive first-refusal seam, so its owned parent/child hierarchy is
delegated to the frozen `MuiFamilyCore` and its scalar attributes and runtime
notifications flow through the frozen object path; a small per-object
guest-resident sidecar (linked through one private attribute id) carries the
menu-specific state. It implements every official attribute and method with its
exact id and I/S/G policy: Menustrip `CaseSensitive`/`Enabled` plus
`InitChange`/`ExitChange` change-nesting with underflow protection and
`WillOpen`/`Popup` gating; Menu `Title`/`Enabled`/`CopyStrings` with title
ownership governed by `CopyStrings`; Menuitem `Title`/`Shortcut`/`Checkit`/
`Checked`/`Toggle`/`Exclude`/`Enabled`/`CommandString`/`CopyStrings`/`Menuitem`/
`Trigger` with failure-atomic copied strings, mutual exclusion/toggle across
siblings, disabled gating, trigger publication, the one-level Menuitem nesting
rule and recursive class-owned disposal. It does not alter the frozen generic
cores or platform aggregates and needs no native-menu capability; unclaimed
packets retain the existing generic route. The
family classifies and adopts objects emitted by `MUI_MakeObjectA`, direct
`MUI_NewObjectA`, and the class-service object factory; those objects carry
their sidecars from construction onward. The slice is qualified by
`tests/MuiMaster/MuiMenuSpecialistTests.cs` and the
`MenuSpecialistRoot` and integrated `MenuDispatcherRoot` MC68000/020/040
freestanding closures (executed under Copper68k).

The Process/Slave family now has the same additive factory/lifecycle treatment.
`Process.mui` and `Slave.mui` objects created by the direct or class-service
factory receive guest-resident sidecars, and the service-capable public
dispatcher routes their specialist attributes, Process launch/process/kill/
signal methods, Slave setup/dispatch/error/signal methods, inherited Semaphore
verbs, and disposal. Scheduler behavior is an explicit `IMuiProcessCapability`
seam; the implementation creates no managed task, thread, exception, or host
runtime state. The focused `ProcessDispatcherRoot` closure is qualified on
MC68000/020/040, while the broader Process/Slave contract remains part of the
active MG09 progression.

The ten-class Misc family now also supports headless-object factory adoption.
`Keyadjust.mui`, `Panel.mui`, `Filepanel.mui`, `Fontdisplay.mui`, private
`Scrmodelist.mui`, `Argstring.mui`, `Aboutmui.mui`, `Mccprefs.mui`,
`FSProtectionBits.mui`, and `Title.mui` receive a separate guest-resident
instance sidecar after OM_NEW/tag application. Object-aware disposal releases
the sidecar's copied strings, ASL state, hook scratch, adopted rows, and other
owned blocks before the frozen object record. Standalone Misc instances retain
the family-neutral service route. The additive object dispatcher now claims
`OM_GET`, `MUIM_Set`/`MUIM_NoNotifySet`, `MUIM_Cleanup`, the Title page
methods (`MUIM_Title_New`, `MUIM_Title_FindPage`, `MUIM_Title_Close`), and
the validated `MUIM_Panel_Run` boundary, `MUIM_Filepanel_AddRow`,
`MUIM_Mccprefs_RegisterGadget`, `MUIM_Mccprefs_ConfigToGadgets`,
`MUIM_Mccprefs_GadgetsToConfig`, and `OM_DISPOSE` for adopted objects; other
packets remain unclaimed for later outer dispatch layers.

The Application dispatcher also has a bounded MorphOS
`MUIM_Application_AboutMUI` packet boundary. Its `refwindow` argument is
validated as a live MUI object (or Null), passed through the explicit
`ShowMuiAbout` platform capability, and recorded in guest-resident request
telemetry. This does not claim the rest of the Application method inventory
or a pixel-level About window implementation.

`MUIM_Application_CheckRefresh` is implemented as a bounded child-window
walk. Only application children with a live native window handle reach the
explicit `RefreshMuiWindow` capability; the check and refresh counts remain
guest-resident, and dead applications are rejected.

The Application menu compatibility family is implemented as well. The
obsolete-but-ABI-visible `GetMenuCheck`/`GetMenuState` packets use first-match
semantics across open child windows, while `SetMenuCheck`/`SetMenuState` visit
every open child. Closed windows and invalid application objects are skipped
or rejected without managed state.

The Application queue family is implemented with MorphOS semantics. `PushMethod`
copies a bounded packet (up to seven arguments) and returns its queue identifier;
`UnpushMethod` matches target object, queue identifier, and destination method
independently, with zero values acting as wildcards. Queue records stay in guest
memory and use explicit allocation/free seams; no exception or managed runtime
path is involved.

The Application `ShowHelp` packet is also bounded and native-safe. Null uses
the default public screen, `(Object *)-1` resolves to the first open child
window, and ordinary window references must be live MUI objects. Optional help
file/node strings are validated as bounded guest C strings before the explicit
platform presentation seam is called.

`MUIM_Application_DefaultConfigItem` is implemented as an application override
hook. It validates the live application, forwards the configuration identifier
through an explicit value-type capability, and records the accepted result in
guest state without introducing a managed configuration store.

The ABI-visible MorphOS V11 `MUIM_Application_SetConfigItem` packet is also
decoded as the named `{MethodID,item,data}` frame. Its private PSI payload is
opaque by design: the item/data pair and request count live in a typed
guest-resident record, Null data is accepted, non-null data is mapped before
acceptance, and teardown releases the record. No preferences format or
managed configuration store is invented at this boundary.

The documented Group change bracket is routed as exact typed packets for
`MUIM_Group_InitChange`, `MUIM_Group_ExitChange`, and
`MUIM_Group_ExitChange2`. Group subclasses are recognized through the
registered class chain. A guest-resident `MuiGroupChangeState` record carries
bounded nesting depth, ExitChange2 flags, and exit telemetry; malformed
underflow is rejected and object disposal frees the record. This is a
progressive compatibility slice, not a claim of the complete Group method
inventory or visual layout behavior.

The Group ordering packet family is also routed for
`MUIM_Group_MoveMember`, `MUIM_Group_Reorder`, and `MUIM_Group_Sort`. These
packets use named fixed-layout records, validate Group inheritance, and reuse
the bounded guest child topology for position-based moves and NULL-terminated
reorder/sort vectors. The implementation covers the documented ordering
contract; it does not claim private layout-hook or pixel-level behavior.

`MUIM_Application_OpenConfigWindow` is implemented as a bounded, non-blocking
configuration-window request. The `{flags,classid}` packet preserves the raw
flags word (MorphOS currently defines none), accepts a Null class id or a
bounded guest C string, and delegates presentation through an explicit
`OpenMuiConfigWindow` capability. The request and last arguments remain in
guest-resident telemetry; no managed preferences store or UI runtime is
introduced.

`MUIM_Application_BuildSettingsPanel` is implemented as an application
override hook. The `{number}` packet asks an explicit capability for an
optional settings-panel MUI object; non-null results must be live guest
objects, while Null is a valid “no panel” result. The selected number, result,
and request count are retained in guest state without managed UI ownership.

`MUIM_Application_Save` and `MUIM_Application_Load` now validate the paired
`{name}` packets, including the MorphOS ENV (`NULL`) and ENVARC (`(STRPTR)-1`)
selectors and bounded guest C-string paths. They delegate the actual object
graph import/export through explicit save/load capabilities and record the
last operation in guest state. The generic Notify `MUIM_Export` and
`MUIM_Import` packets validate their live `{obj,dataspace}` pair and non-zero
`MUIA_ObjectID`. The nine documented built-in forms are implemented: String
and Text use bounded NUL-terminated guest blobs copied into their existing
owned buffers, while Numeric, Radio, Cycle, List, Area selection, Menuitem,
and Group use ULONG payloads keyed by that ID. Unsupported custom payloads use
the explicit native capability. `MuiApplicationPersistenceCore` now walks the
live application Family tree in preorder using a bounded guest-resident frame
stack and reuses this Dataspace transport, while suppressing zero-ID objects.
`MuiApplicationSettingsFileCore` now connects that walk to a bounded
CopperOS-internal file format through the native-safe `IMuiDosCapability`,
including short-transfer handling and explicit scratch/Dataspace cleanup.
`ImportTransactional` clears and snapshots the live tree into a second
Dataspace, rejects Dataspace aliasing, and compensates a failing import while
preserving the failure result. Exact
MorphOS on-disk compatibility, selector path resolution, and custom-class
persistence remain open.

`MUIM_Window_Snapshot` is implemented with the MorphOS flags (`0` unsnapshot,
`1` snapshot), a required non-zero `MUIA_Window_ID`, and an explicit
`SnapshotMuiWindow` capability. The capability owns the actual settings store;
the packet slice does not claim private MorphOS on-disk encoding parity.

The obsolete-but-ABI-visible `MUIM_Window_SetCycleChain` packet validates its
inline Null-terminated MUI object vector and copies it into guest-resident
nodes. Invalid replacements preserve the previous chain, and object cleanup
releases the copied nodes without managed state.

`MUIA_Window_ActiveObject` `MUIM_Set` selectors now cover MorphOS None, Next,
Prev, Left, Right, Up, and Down values. Next, Prev, and direct object
activation require membership in the copied cycle chain; spatial navigation
uses its published Area geometry and retains focus when no candidate exists.

`MUIM_Application_Execute` and `MUIM_Application_Run` now drive the shared
guest-resident application loop through Input, pushed methods, input handlers,
and window events until `MUIV_Application_ReturnID_Quit`. Signal waiting is an
explicit non-consuming platform seam, and the native closure has a bounded
guard against malformed non-terminating guests.

Standalone MG09 records now use additive service seams. `DispatchStandaloneService`
routes the Pop*, pen/color, and Misc specialist dispatchers by their
guest-resident magic/layout, while `DispatchExternalService` routes the
Boopsi/Dtpic wrapper. Unknown instances remain unclaimed. Keeping these routers
separate from the headless Process/Slave seam preserves small zero-relocation
freestanding closures; the dedicated service roots qualify MC68000/020/040
output without managed allocations, exceptions, or runtime services.

The eventual built runtime library is staged as:

`filesystem/SYS/Libs/muimaster.library`

The fixed MorphOS 3.20 List advanced method envelopes (Insert/InsertSingle,
Remove, NextSelected, SortEntries, Move, Exchange, Jump, Redraw, CreateImage,
DeleteImage, and FloattextAppend) use named records at the dispatcher boundary.
`MuiCollectionAdvancedMessageCodec` is the only owner of their packed guest
offsets; variable hook/display payloads remain explicit ABI boundaries. Its
focused native root is zero-runtime clean at 4,008/4,036/4,040 bytes for
MC68000/020/040 and returns 42 on MC68000. This is a packet-family slice, not
completion of List or MorphOS differential compatibility.

The remaining fixed List basic envelopes (`GetEntry`, `Select`, `Clear`, and
`Sort`) follow the same struct-first boundary through
`MuiCollectionBasicMessageCodec`. The focused native root is zero-runtime
clean at 2,600/2,612/2,616 bytes for MC68000/020/040 and returns 42 on
MC68000. The broader List lifecycle and full MorphOS differential behavior
remain progressive work.

Collection surface packets (`Layout`, `AskMinMax`, `Draw`, and
`Set`/`NoNotifySet`) now use `MuiCollectionSurfaceMessageCodec`, keeping named
geometry, storage, flags, and attribute records at the dispatcher boundary.
The focused native root is zero-runtime clean at 3,348/3,388/3,396 bytes for
MC68000/020/040 and returns 42 on MC68000.

The standalone Boopsi/Dtpic wrapper packet family now uses
`MuiExternalWrapperMessageCodec` for `OM_GET`, `MUIM_Set`, `OM_UPDATE`, setup,
min/max, layout, and fixed method records. The focused native boundary is
zero-runtime clean at 5,184/5,232/5,244 bytes for MC68000/020/040 and returns
42 on MC68000.

The fixed Dirlist/Volumelist packet family now uses
`MuiDirlistMessageCodec` for `Set`/`NoNotifySet`, `ReRead`, `Rename`,
`SetComment`, `SetProtection`, `ListGetEntry`, and `ListClear`. The focused
native boundary is zero-runtime clean at 3,944/3,964/3,968 bytes for
MC68000/020/040 and returns 42 on MC68000. The dispatcher consumes named
records; full directory/volume semantics remain a separate progressive slice.

The fixed external Listtree.mcc packet family now uses
`MuiListtreeMessageCodec` for `Set`/`NoNotifySet`, `Get`, `Insert`, `Remove`,
`GetEntry`, `Open`, `Close`, `Sort`, `GetNr`, `Move`, `Exchange`, `Rename`,
`FindName`, `SetDropMark`, and `TestPos`. The focused native boundary is
zero-runtime clean at 8,540/8,700/8,720 bytes for MC68000/020/040 and returns
42 on MC68000. The external dispatcher consumes named records; full Listtree
semantics remain a separate progressive slice.

The fixed pen/color specialist packet family now uses
`MuiColorSpecialistMessageCodec` for `OM_GET`, `OM_SET`/`MUIM_NoNotifySet`,
`Pendisplay_SetColormap`, `Pendisplay_SetMUIPen`, `Pendisplay_SetRGB`, and
`OM_DISPOSE`. The focused native boundary is zero-runtime clean at
3,972/3,992/4,000 bytes for MC68000/020/040 and returns 42 on MC68000. The
specialist dispatcher consumes named records; full specialist semantics remain
a separate progressive slice.

The authoritative inventory and progressive qualification records are under
`docs/Libraries/MorphOs320Mui/`. No MorphOS-compatible version may be advertised
until the corresponding complete versioned surface passes its gate.

The fixed Process.mui, Slave.mui, and shared Semaphore.mui packet family now
uses `MuiProcessSpecialistMessageCodec` for `OM_GET`, `OM_SET`/
`MUIM_NoNotifySet`, process launch/kill/poll/signal, slave setup/cleanup/
dispatch/error/signals-received, and semaphore operations. The focused native
boundary is zero-runtime clean at 4,520/4,544/4,548 bytes for MC68000/020/040
and returns 42 on MC68000. The dispatcher consumes named records; full
Process/Slave semantics remain a separate progressive slice.

The fixed Popstring, Popobject, Poplist, Popasl, Poppen, Popcolor, and
Popscreen packet family now uses `MuiPopSpecialistMessageCodec` for `OM_GET`,
`OM_SET`/`MUIM_NoNotifySet`, `Popstring_Open`, `Popstring_Close`, `Setup`,
`Cleanup`, and `HandleInput`. The focused native boundary is zero-runtime
clean at 3,284/3,300/3,304 bytes for MC68000/020/040 and returns 42 on
MC68000. The dispatcher consumes named records; full Pop* semantics remain a
separate progressive slice.
The fixed MorphOS Menustrip/Menu/Menuitem packet family now uses
`MuiMenuSpecialistMessageCodec` for `OM_GET`, `OM_SET`/
`MUIM_NoNotifySet`, Family add/remove/insert/reorder/sort/transfer,
Menustrip change/open methods, and popup. The dispatcher consumes named
method, attribute, storage, pointer, pair, and popup records; packed guest
offsets are confined to the codec. The focused native boundary is
zero-runtime clean at **4,712/4,748/4,752 bytes** for MC68000/020/040 and
returns **42** on MC68000 after **1,628 instructions / 17,014 cycles**. Full
menu behavior and MorphOS differential parity remain separate progressive
work.

The String.mui Unicode seam now uses `MUIA_Unicode` with logical BufferPos and
DisplayPos columns. Cursor movement, backspace, and delete map through shared
UTF-8 byte-boundary helpers, while drawing clips only at complete sequences.
`StringUtf8CursorRoot` is zero-runtime clean at **2,360 / 2,440 / 2,348
bytes** for MC68000/020/040 and returns **42** on MC68000. Printable Unicode
input now uses the named `MuiUtf8Character` record and a freestanding encoder;
Unicode Accept/Reject filters compare decoded UTF-8 codepoints, while legacy
strings retain byte-set semantics. Unicode `String_MaxLen` truncation also stops
at complete logical-character boundaries. Broader String.mui parity remains
progressive. Public BufferPos/DisplayPos writes are clamped to the logical
UTF-8 contents length before storage and redraw; cursor movement, editing, and
drawing keep the cursor within the visible logical character window.

Stringscroll `MUIM_HandleInput` uses a named `MuiCollectionHandleInputMessage`
record for the MorphOS `imsg`/`muikey` packet. The current bounded input slice
handles line/page navigation, horizontal movement, top, and bottom, while
honoring `MUIA_Stringscroll_NoInput`; unsupported keys remain unclaimed by the
control behavior until additional MorphOS evidence is implemented.

Listview `MUIM_HandleInput` uses the same named packet and forwards MUIKEY
up/down/page-up/page-down/top/bottom to the owned List's active-row selectors.
The List core performs row clamping and visible-window tracking, and
`MUIA_Listview_Input` gates the behavior. Pointer, drag, and full MorphOS
Listview input semantics remain progressive.

Listview `MUIKEY_PRESS` selects the active row exclusively. `MUIKEY_TOGGLE`
inverts the active row in normal multi-select modes and remains exclusive when
`MUIV_Listview_MultiSelect_None` is selected; all changes flow through the
existing ListCore selection and notification state.

Listview pointer activation now uses a named `MuiIntuiPointerMessage` record
when `MUIKEY_NONE` carries an `IDCMP_MOUSEBUTTONS` `SELECTUP` event. The hit is
resolved through the named `MuiListTestPosResult` seam and then follows the
same click, shift-multiselect, and notification policy as direct clicks.
Guest-memory offsets remain confined to `MuiIntuiMessageCodec` and
`MuiListTestPosResultCodec`; SELECTDOWN and drag/drop remain progressive.

When both the Listview and its child List opt into immediate sortable
dragging, the pointer path additionally keeps a guest-resident
`MuiListviewDragState`: SELECTDOWN arms the source, MOUSEMOVE updates the
child's drop mark, and SELECTUP uses `MuiListCore.DragMove`. A stationary
gesture remains a normal click. External MUIM_Drag* methods, pointer capture,
and cancellation are intentionally still separate progressive work.

The local sortable drag seam now consumes MorphOS `MUIKEY_RELEASE` (**-2**) as
its cancellation edge. It clears the child drop mark and releases the named
`MuiListviewDragState` before claiming the event, even when the child list has
become empty. `MUIKEY_TOP` and `MUIKEY_BOTTOM` use the MorphOS values **6/7**;
`MUIKEY_NONE` remains **-1**. This does not claim external `MUIM_Drag*`
messages or pointer-capture/focus-loss behavior, which remain progressive.

The MorphOS Area drag method family now has a bounded struct-first baseline.
`MUIM_DragBegin`, `MUIM_DragDrop`, `MUIM_DragEvent`, `MUIM_DragFinish`,
`MUIM_DragQuery`, and `MUIM_DragReport` use named packet records in
`AreaDragMessagesCore.cs`; `MuiAreaDragState` keeps guest-resident lifecycle
state without managed allocation or exceptions. Begin requires
`MUIA_Draggable`, query requires a draggable source and `MUIA_Dropable` target,
drop/report/event update the record, and finish releases it. The focused native
codec/state roots are zero-runtime and zero-relocation clean; host coverage is
**569/569**. Pointer capture, drag images, application-level drop dispatch,
and full MorphOS differential behavior remain progressive.

The Area activation baseline now consumes the MorphOS `MUIP_GoActive` and
`MUIP_GoInactive` named `{ MethodID, flags }` packet. `GoActive` records the
flags and marks the object active; `GoInactive` records the flags and clears
active state. The state remains in the object store, so this seam uses no
managed allocation, exceptions, or unowned guest block. Per-class visual
activation effects and focus-window coordination remain progressive.

The shared common-control `MUIP_HandleEvent` packet now uses the exact named
MorphOS fields `InputMessage`, signed `MuiKey`, and `EventHandlerNode`. Common
control event behavior consumes `MuiKey` explicitly while preserving the node
pointer for a future callback-capability implementation. Callback dispatch and
full MorphOS event parity remain progressive.

The application-window event path now has a typed single-node callback seam for
`MuiEventHandlerNode`. It validates the GUI-mode flag and event mask, then
invokes the node object through `DoMethod`; the window walk reuses the same
helper. Host coverage is **574/574**, and the focused native root is
zero-runtime and zero-relocation clean. Class coercion, richer return-code
propagation, handler ordering, and complete MorphOS event parity remain
progressive.

Family reorder/sort packet vector lookup now uses the named
`MuiFamilyInlineVectorCursor` and `MuiFamilyInlineVectorCursorCodec.TryGetEntry`
helpers, layered over the typed `MuiFamilyMutationVectorEntry` record. The
fixed packet header, 4-byte pointer-vector elements, bounded mapping, and
overflow checks remain intact. Host coverage is **838/838**; native Family
inline-vector ABI and complete MorphOS differential parity remain progressive.

Listview generic Get and OM_GET for `Input`, `MultiSelect`, `ScrollerPos`, and
`DragType` use
the named `MuiListviewInteractionPolicyState` guest record, including the
collection dispatcher’s fixed `OM_GET` result-slot path. The generic scalar
projection remains a compatibility seam, while the typed record is
authoritative for public reads and behavior. Host coverage is **1184/1184**;
native Listview policy getter ABI and complete MorphOS differential parity
remain progressive.

Floattext generic Get and collection OM_GET for `Text`, `SkipChars`, `TabSize`,
`Justify`, and shared `Width` use the named `MuiFloattextPolicyState` record.
Raw scalar reads remain confined to initialization and compatibility
synchronization so construction tags are not hidden by the public projection.

Stringscroll generic Get and collection OM_GET for `String`, `HorizBar`,
`NoInput`, `SetMin`, `SetVMin`, `UseWinBorder`, `VertBar`, and
`VertScrollerOnly` use the named `MuiStringscrollPolicyRecord`; raw scalar
storage remains only the initialization and compatibility synchronization
seam.

Dirlist and Volumelist generic Get now use the named filter and sort records;
Dirlist OM_GET publishes those values through the named ULONG result-slot
boundary. Volumelist `ExampleMode` uses its named mode record, while
construction and compatibility synchronization remain on raw attributes. Host
coverage is **1187/1187**; native Dirlist/Volumelist getter ABI and complete
MorphOS differential parity remain progressive.

Listtree policy and hook getters now prefer the named
`MuiListtreePolicyStateRecord`, including the external Listtree `Get` message's
ULONG result storage. Construction and compatibility synchronization stay on
raw attributes to preserve the external-class boundary. Host coverage is
**1188/1188**; native Listtree getter ABI and complete MorphOS differential
parity remain progressive.

Group grid policy getters now prefer the named `MuiGroupGridStateRecord` for
columns, rows, spacing, same-size, and centering. Raw attributes remain only
the bootstrap and compatibility synchronization seam, and common-control
`OM_GET` publishes the same typed projection for Group objects. Native Group
grid getter ABI and complete MorphOS differential parity remain progressive.
Host coverage is **1189/1189**.

Group `MUIA_Group_ActivePage` getters now use the named `MuiGroupPageState`
record and selector normalization for populated Groups. Zero-child Groups
retain their raw compatibility value for persistence round-trips, while both
generic `Get` and common-control `OM_GET` share the same projection. Host
coverage is **1190/1190**; native Group ActivePage ABI and complete MorphOS
differential parity remain progressive.

Group layout-policy getters now use the named `MuiGroupLayoutPolicyStateRecord`
for orientation, effective spacing, same-size policy, spacing, and page mode;
Group grid retains precedence for overlapping grid attributes. Bootstrap reads
remain on the explicit raw-attribute seam, while generic `Get` and common
control `OM_GET` share the named projection. Host coverage is **1191/1191**;
native Group layout-policy ABI and complete MorphOS differential parity remain
progressive.

Group `MUIA_Group_LayoutHook` now uses the named guest-resident
`MuiGroupLayoutHookStateRecord`; typed layout dispatch, generic `Get`, and
common-control `OM_GET` share that pointer state. Raw storage remains only the
compatibility/bootstrap seam, the state is released with the object, and the
initialize-only MorphOS contract is enforced. Focused Group layout-hook
coverage is green at **18/18**, with full host coverage at **1191/1191**;
native LayoutHook ABI and complete MorphOS differential parity remain
progressive.

Getter-only Group `MUIA_Group_ChildCount` and `MUIA_Group_ChildList` now route
through common-control `OM_GET`, including the external Group class boundary.
ChildCount remains family-derived and ChildList remains a named read-only guest
projection; raw public slots cannot replace the live values. Focused Group
children coverage is **10/10**, with full host coverage at **1192/1192**;
native child-getter ABI and complete MorphOS differential parity remain
progressive.

Generic `MUIA_Parent`, `MUIA_ObjectID`, and `MUIA_UserData` getters now use the
named `MuiHeadlessObjectRecord` for direct `Get` and common-control `OM_GET`,
including unknown and external classes. Parent relationship state remains
authoritative over raw compatibility slots. Focused metadata coverage is
**1/1**, with full host coverage at **1193/1193**; native metadata ABI and
complete MorphOS differential parity remain progressive.

Getter-only `MUIA_Family_ChildCount` and `MUIA_Family_List` now reuse the
named guest-resident child-list projection for Family-compatible class chains,
including Family, Group, application/window, and menu ownership. The count is
topology-derived, the returned `MinList` is read-only, and raw public slots
remain compatibility/bootstrap storage. Focused Family getter coverage is
**1/1**; native Family getter ABI and complete MorphOS differential parity
remain progressive.

Initialize-only `MUIA_Family_Child` tags now adopt non-null children through
the guest-resident Family topology in tag order. Runtime sets after object
initialization are rejected, and null child tags fail without leaving a
partially-created object. Focused Family coverage is **2/2**; native Family
child-tag ABI and complete MorphOS differential parity remain progressive.

`MUIA_Version` and `MUIA_Revision` now use named class metadata when a builtin
or external class is registered with explicit values. Direct `Get` and common
`OM_GET` share that projection, while unannotated classes retain raw fallback
compatibility. Focused version/revision coverage is **1/1**; the complete
per-class MorphOS metadata inventory remains progressive.

`MUIA_Group_Child` creation tags now follow the documented
`MUIA_Family_Child` alias for non-Group Family classes. Alias adoption is
initialize-only, while the existing Group-specific route remains unchanged.
Focused Family coverage is **3/3**; native alias ABI and complete MorphOS
differential parity remain progressive.

The shared Area `MUIA_Weight` input now uses the named
`MuiAreaWeightState`/`MuiAreaWeightStateRecord` structs for generic `Get` and
`OM_GET`. The default of 100, raw-only compatibility synchronization, runtime
setter behavior, and persistence/import updates remain intact while resolved
horizontal/vertical weights stay in the separate layout-policy record. Host
coverage is **1183/1183**; native Area Weight ABI and complete MorphOS
differential parity remain progressive.

Prop and Scrollbar policy scalars now use the named
`MuiPropPolicyState`/`MuiPropPolicyStateRecord` structs for generic `Get` and
`OM_GET`. Raw storage remains only the compatibility synchronization seam;
runtime DeltaFactor/Slider updates, Slider normalization, initializer-only
Horiz/UseWinBorder rules, and Scrollbar child forwarding remain intact. Host
coverage is **1182/1182**; native Prop policy ABI and complete MorphOS
differential parity remain progressive.

Image spec getter projection now routes `MUIA_Image_Spec` and
`MUIA_Image_BuiltinSpec` through the guest-resident
`MuiImageSpecStateRecord`. Separate presence fields preserve MorphOS union
semantics, runtime Image_Spec writes update the named struct, and direct,
generic, and `OM_GET` paths use raw-only bootstrap/synchronization reads.
Host coverage is **1167/1167**; native Image spec ABI and complete MorphOS
differential parity remain progressive.

Image render and legacy-pointer getters now route
`MUIA_Image_State`, `MUIA_Selected`, `MUIA_Image_FreeHoriz`, and
`MUIA_Image_FreeVert` through `MuiImageRenderStateRecord`, and
`MUIA_Image_OldImage` through `MuiImageOldImageStateRecord`. The implementation
preserves selection/free-axis synchronization, init-only OldImage behavior,
caller-owned pointers, and raw-only internal bootstrap reads. Host coverage is
**1168/1168**; native Image render/legacy getter ABI and complete MorphOS
differential parity remain progressive.

Bitmap and Bodychunk getter projection now uses named records for shared
width/height geometry, class-specific source pointers, and Bodychunk
compression/depth/masking. Class gating, caller-owned sources, default depth,
and live remap/redecode behavior remain intact, with raw-only internal
synchronization. Host coverage is **1169/1169**; native Bitmap/Bodychunk
getter ABI and complete MorphOS differential parity remain progressive.

Rectangle bar flags now route through `MuiRectanglePresentationStateRecord`,
and the optional caller-owned `MUIA_Rectangle_BarTitle` pointer through
`MuiRectangleBarTitleStateRecord`. Class gating, init-only behavior,
absent-title presence, and raw-only synchronization remain intact. Host
coverage is **1169/1169**; native Rectangle getter ABI and complete MorphOS
differential parity remain progressive.

The optional common-control `MUIA_Font` pointer now routes through
`MuiControlFontStateRecord`, and `MUIA_Image_FontMatchString` through
`MuiImageFontMatchStringStateRecord`. Presence semantics, Image class gating,
caller-owned pointer validation, runtime setter behavior, and raw-only
synchronization remain intact. Host coverage is **1169/1169**; native
Font/Image font-match getter ABI and complete MorphOS differential parity
remain progressive.

Shared Area presentation attributes (`MUIA_Disabled`, `MUIA_ShowMe`,
`MUIA_Background`, and `MUIA_Frame`) now route through
`MuiAreaPresentationStateRecord` for generic `Get` and `OM_GET`. Class gating,
runtime setter/redraw behavior, ULONG semantics, and raw-only synchronization
remain intact. Host coverage is **1169/1169**; native Area presentation getter
ABI and complete MorphOS differential parity remain progressive.

Area geometry now routes `MUIA_LeftEdge`, `MUIA_TopEdge`, `MUIA_Width`,
`MUIA_Height`, `MUIA_RightEdge`, and `MUIA_BottomEdge` through
`MuiAreaGeometryStateRecord` for generic `Get` and `OM_GET`. Signed coordinates
remain ULONG-compatible on the guest bus, with layout updates and raw-only
synchronization preserved. Host coverage is **1170/1170**; native Area geometry
getter ABI and complete MorphOS differential parity remain progressive.

Area layout policy now routes `MUIA_Weight`, `MUIA_HorizWeight`,
`MUIA_VertWeight`, `MUIA_FixWidth`, `MUIA_FixHeight`, `MUIA_MaxWidth`,
`MUIA_MaxHeight`, `MUIA_InnerLeft`, `MUIA_InnerRight`, `MUIA_InnerTop`, and
`MUIA_InnerBottom` through `MuiAreaLayoutPolicyStateRecord` for generic `Get`
and `OM_GET`. Shared-weight defaults, min/max behavior, and raw-only policy
synchronization remain intact. Host coverage is **1171/1171**; native Area
layout-policy getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_FillArea` now routes through `MuiAreaRenderPolicyStateRecord` for generic
`Get` and `OM_GET`. Runtime updates keep the same named record used by Area
drawing synchronized, with default fill behavior and raw-only reads preserved.
Host coverage is **1172/1172**; native Area render-policy getter ABI and
complete MorphOS differential parity remain progressive.

Slider `MUIA_Slider_Horiz`/`MUIA_Slider_Quiet`, Scale `MUIA_Scale_Horiz`, and
Levelmeter `MUIA_Gauge_Horiz` now route through their named presentation
records for generic `Get` and `OM_GET`. Shared-key ownership, defaults,
raw-only synchronization, and runtime Slider/Scale updates remain intact. Host
coverage is **1173/1173**; native presentation getter ABI and complete MorphOS
differential parity remain progressive.

Gadget `MUIA_InputMode`, `MUIA_Selected`, and `MUIA_Pressed` now route through
`MuiGadgetInteractionStateRecord` for generic `Get` and `OM_GET`. Class
gating, keyboard/runtime selection transitions, and raw-only interaction
synchronization remain intact. Host coverage is **1174/1174**; native Gadget
interaction getter ABI and complete MorphOS differential parity remain
progressive.

Prop/Scrollbar `MUIA_Prop_Entries`, `MUIA_Prop_Visible`, and `MUIA_Prop_First`
now route through `MuiPropRangeStateRecord`; Scrollbar
`MUIA_Group_Horiz`/`MUIA_Scrollbar_Type` use `MuiScrollbarLayoutStateRecord`.
Class gating, range movement, child forwarding, and raw-only synchronization
remain intact. Host coverage is **1175/1175**; native range/layout getter ABI
and complete MorphOS differential parity remain progressive.

Cycle/Radio `MUIA_Cycle_Entries`/`MUIA_Radio_Entries` now route through
`MuiChoiceEntriesStateRecord`, while `MUIA_Cycle_Active`/`MUIA_Radio_Active`
use `MuiChoiceActiveStateRecord` for generic `Get` and `OM_GET`. Bounded
NULL-terminated entry validation, Cycle wrap selectors, Radio selection rules,
class gating, and raw-only synchronization remain intact. Host coverage is
**1176/1176**; native Choice getter ABI and complete MorphOS differential
parity remain progressive.

String scroll width/height, visible viewport dimensions, and pixel offsets now
route through `MuiStringScrollMetricsStateRecord` for direct `Get`, generic
`Get`, and `OM_GET`. UTF-8 visual-column counting, CR/LF semantics,
layout-derived viewport values, bounded offset clamping, and raw-only
synchronization remain intact. Host coverage is **1177/1177**; native String
scroll getter ABI and complete MorphOS differential parity remain progressive.

Image `MUIA_Image_FontMatch`, `MUIA_Image_FontMatchHeight`, and
`MUIA_Image_FontMatchWidth` now route through `MuiImageFontMatchStateRecord`
for generic `Get` and `OM_GET`. Their initializer-only setter policy,
independent caller-owned `FontMatchString` record, and raw-only persistence
synchronization remain intact. Host coverage is **1178/1178**; native Image
FontMatch getter ABI and complete MorphOS differential parity remain
progressive.

Bitmap-only `MUIA_Bitmap_Alpha`, `MUIA_Bitmap_MappingTable`,
`MUIA_Bitmap_Precision`, `MUIA_Bitmap_SourceColors`,
`MUIA_Bitmap_Transparent`, and `MUIA_Bitmap_UseFriend` now route through
`MuiBitmapPolicyStateRecord` for generic `Get` and `OM_GET`. Bitmap/Bodychunk
separation, runtime `[ISG]` mutation and remap invalidation, initializer-only
`UseFriend`, and raw-only synchronization remain intact. Host coverage is
**1179/1179**; native Bitmap policy getter ABI and complete MorphOS
differential parity remain progressive.

Renderer-produced `MUIA_Bitmap_Remapped` now routes through
`MuiBitmapRemappedStateRecord` for Bitmap and Bodychunk generic `Get` and
`OM_GET`. Source rebuild, cleanup, null-on-failure behavior, get-only setter
policy, caller-owned pointer semantics, and raw-only synchronization remain
intact. Host coverage is **1180/1180**; native remapped-state getter ABI and
complete MorphOS differential parity remain progressive.

Getter-only `MUIA_Gadget_Gadget` now routes through
`MuiGadgetGadgetStateRecord` for generic `Get` and `OM_GET`. Caller-owned
relationship semantics, Gadget class gating, persistence/bootstrap
synchronization, and get-only setter behavior remain intact. Host coverage is
**1181/1181**; native Gadget relationship getter ABI and complete MorphOS
differential parity remain progressive.

Store/Objectmap/Datamap packets now resolve their fixed fields through the
named `MuiStoreFieldCursor` and `MuiStoreFieldCursorCodec` helpers. Pointer,
signed-length, key, result-storage, object, and iteration-counter payloads,
packet validation, mapping checks, and overflow rejection remain intact. Host
coverage is **905/905**; native Store/Objectmap/Datamap packet ABI and complete
MorphOS differential parity remain progressive.

Family mutation packets now resolve their fixed fields through the named
`MuiFamilyPacketFieldCursor` and `MuiFamilyPacketFieldCursorCodec` helpers,
while Family mutation list head/tail fields use
`MuiFamilyMutationListFieldCursor` and
`MuiFamilyMutationListFieldCursorCodec`. Pointer payloads, packet validation,
mapping checks, and vector-header overflow rejection remain intact. Host
coverage is **906/906**; native Family mutation packet/list ABI and complete
MorphOS differential parity remain progressive.

The fixed MorphOS `MUIM_Family_GetChild` packet now resolves its method,
signed selector, and reference fields through the named
`MuiFamilyGetChildPacketFieldCursor` and
`MuiFamilyGetChildPacketFieldCursorCodec` helpers. Packet validation, mapping
checks, and overflow rejection remain intact. Host coverage is **907/907**;
native Family_GetChild packet ABI and complete MorphOS differential parity
remain progressive.

The fixed MorphOS `MUIM_Family_DoChildMethods` method field now resolves
through the named `MuiFamilyDoChildMethodsPacketFieldCursor` and
`MuiFamilyDoChildMethodsPacketFieldCursorCodec` helpers. Method validation,
packet-size checks, mapping checks, and bounded child-forward dispatch remain
intact. Host coverage is **908/908**; native Family_DoChildMethods packet ABI
and complete MorphOS differential parity remain progressive.

The fixed MorphOS Boopsi query packet now resolves its screen, flags, signed
dimension, and RenderInfo fields through the named
`MuiBoopsiQueryPacketFieldCursor` and
`MuiBoopsiQueryPacketFieldCursorCodec` helpers. Packet validation, mapping
checks, and overflow rejection remain intact. Host coverage is **909/909**;
native Boopsi query packet ABI and complete MorphOS differential parity remain
progressive.

The fixed MorphOS `MUIM_CallHook` envelope now resolves method, hook, and
first-parameter fields through the named `MuiCallHookPacketFieldCursor` and
`MuiCallHookPacketFieldCursorCodec` helpers. Variadic-tail addressing, packet
validation, mapping checks, and overflow rejection remain intact. Host
coverage is **910/910**; native CallHook packet ABI and complete MorphOS
differential parity remain progressive.

Notify `WriteLong` and `WriteString` packets now resolve their value,
source-string, and destination-memory fields through the named
`MuiNotifyWritePacketFieldCursor` and
`MuiNotifyWritePacketFieldCursorCodec` helpers. Bounded copy behavior, packet
validation, mapping checks, and overflow rejection remain intact. Host
coverage is **911/911**; native Notify write packet ABI and complete MorphOS
differential parity remain progressive.

The fixed MorphOS `MUIM_GetConfigItem` envelope now resolves method, config-id,
and result-storage fields through the named
`MuiGetConfigItemPacketFieldCursor` and
`MuiGetConfigItemPacketFieldCursorCodec` helpers. Packet validation, mapping
checks, and overflow rejection remain intact. Host coverage is **912/912**;
native GetConfigItem packet ABI and complete MorphOS differential parity remain
progressive.

Notify UserData Find/Get/Set packets now resolve their fields through the
named `MuiNotifyUserDataPacketFieldCursor` and
`MuiNotifyUserDataPacketFieldCursorCodec` helpers, while traversal-frame
Object/NextChild fields use `MuiUDataTraversalFieldCursor` and
`MuiUDataTraversalFieldCursorCodec`. Bounded traversal, packet validation,
mapping checks, and overflow rejection remain intact. Host coverage is
**913/913**; native UserData packet/traversal ABI and complete MorphOS
differential parity remain progressive.

The fixed MorphOS `MUIM_SetAsString` envelope now resolves method, attribute,
format, and value fields through the named
`MuiSetAsStringPacketFieldCursor` and
`MuiSetAsStringPacketFieldCursorCodec` helpers. Variadic-parameter addressing,
bounded formatting, packet validation, mapping checks, and overflow rejection
remain intact. Host coverage is **914/914**; native SetAsString packet ABI and
complete MorphOS differential parity remain progressive.

The fixed MorphOS Notify, KillNotify, KillNotifyObject, Set/NoNotifySet,
MultiSet, and FindObject packets now resolve their fields through the named
`MuiNotifyPacketFieldCursor` and `MuiNotifyPacketFieldCursorCodec` helpers.
Inline-vector addressing, packet validation, mapping checks, and overflow
rejection remain intact. Host coverage is **915/915**; native Notify core
packet ABI and complete MorphOS differential parity remain progressive.

The fixed MorphOS Export/Import persistence packets now resolve method and
Dataspace fields through the named `MuiObjectPersistencePacketFieldCursor` and
`MuiObjectPersistencePacketFieldCursorCodec` helpers. Packet validation,
mapping checks, and overflow rejection remain intact. Host coverage is
**916/916**; native object persistence packet ABI and complete MorphOS
differential parity remain progressive.

The fixed MorphOS `MUIM_UpdateConfig` method, config-id, and signed
redraw-count header fields now resolve through the named
`MuiUpdateConfigPacketFieldCursor` and
`MuiUpdateConfigPacketFieldCursorCodec` helpers. Existing redraw-table
cursors, count validation, packet checks, mapping checks, and overflow
rejection remain intact. Host coverage is **917/917**; native UpdateConfig
packet ABI and complete MorphOS differential parity remain progressive.

Application ReturnID, Input/NewInput, and Add/RemoveInputHandler packet fields
now resolve through the named `MuiApplicationInputPacketFieldCursor` and
`MuiApplicationInputPacketFieldCursorCodec` helpers. The fixed 8-byte packet
records, payload selection, mapping and overflow checks remain intact. Host
coverage is **918/918**; native Application input packet ABI and complete
MorphOS differential parity remain progressive.

Application PushMethod and Unpush packet fields now resolve through the named
`MuiApplicationQueuePacketFieldCursor` and
`MuiApplicationQueuePacketFieldCursorCodec` helpers, keyed by packet kind.
The 12-byte and 16-byte packet records, parameter-tail addressing, mapping,
and overflow checks remain intact. Host coverage is **919/919**; native
Application queue packet ABI and complete MorphOS differential parity remain
progressive.

Application ShowHelp and AboutMUI packet fields now resolve through the named
`MuiApplicationPresentationPacketFieldCursor` and
`MuiApplicationPresentationPacketFieldCursorCodec` helpers, keyed by packet
kind. The 20-byte and 8-byte packet records, mapping, and overflow checks
remain intact. Host coverage is **920/920**; native Application presentation
packet ABI and complete MorphOS differential parity remain progressive.

Application SetConfigItem, OpenConfigWindow, BuildSettingsPanel, and Settings
I/O packet fields now resolve through the named
`MuiApplicationSettingsPacketFieldCursor` and
`MuiApplicationSettingsPacketFieldCursorCodec` helpers, keyed by packet kind.
The 12-byte and 8-byte packet records, mapping, and overflow checks remain
intact. Host coverage is **921/921**; native Application settings packet ABI
and complete MorphOS differential parity remain progressive.

Application ConfigID, CheckRefresh, Loop, WindowMethod, and Snapshot records
now resolve through the named `MuiApplicationMethodPacketFieldCursor` and
`MuiApplicationMethodPacketFieldCursorCodec` helpers, keyed by packet kind.
The method-only 4-byte records and snapshot/config-id payloads retain their
mapping and overflow checks. Host coverage is **922/922**; native Application
method-record ABI and complete MorphOS differential parity remain progressive.

Application and Window menu query/set packet fields now resolve through the
named `MuiApplicationMenuPacketFieldCursor` and
`MuiApplicationMenuPacketFieldCursorCodec` helpers, keyed by packet kind.
The 8-byte and 12-byte packet records, mapping, and overflow checks remain
intact. Host coverage is **923/923**; native menu packet ABI and complete
MorphOS differential parity remain progressive.

Window Add/RemoveEventHandler packet fields now resolve through the named
`MuiWindowEventHandlerPacketFieldCursor` and
`MuiWindowEventHandlerPacketFieldCursorCodec` helpers, keyed by packet kind.
The fixed 8-byte record, handler pointer, mapping, and overflow checks remain
intact. Host coverage is **924/924**; native Window event-handler packet ABI
and complete MorphOS differential parity remain progressive.

Window SetCycleChain method and first-object header fields now resolve through
the named `MuiWindowCycleChainPacketFieldCursor` and
`MuiWindowCycleChainPacketFieldCursorCodec` helpers. The fixed header remains
separate from the existing inline object-vector tail cursor. Host coverage is
**925/925**; native SetCycleChain header ABI and complete MorphOS differential
parity remain progressive.

The current `ApplicationDispatcher.cs` fixed-packet surface is now fully
struct-first: its nonzero guest-memory `Read/WriteUInt*` offset scan is empty.
Named packet structs and cursor codecs retain mapping, overflow, method, and
vector validation. Host coverage remains **925/925**; native
ApplicationDispatcher ABI and complete MorphOS differential parity remain
progressive.

The duplicate Window event-handler codec in `ApplicationWindowCore.cs` now
reuses the named event-handler packet cursor shared with the dispatcher.
Existing packet write/read, method validation, handler mapping, and
freestanding behavior remain intact. Host coverage remains **925/925**;
native ApplicationWindow event-handler ABI and complete MorphOS differential
parity remain progressive.

The persistent 20-byte ApplicationWindow node record now resolves Next, Value,
Sequence, Auxiliary, and Packet through the named
`MuiApplicationWindowNodeFieldCursor` and
`MuiApplicationWindowNodeFieldCursorCodec` helpers. Host coverage is
**926/926**; native node-record ABI and complete MorphOS differential parity
remain progressive.

The mixed-width 24-byte ApplicationWindow event-handler node now resolves
successor/predecessor links, reserved and priority bytes, flags, object/class
pointers, and events through the named `MuiEventHandlerNodeFieldCursor` and
`MuiEventHandlerNodeFieldCursorCodec` helpers. Host coverage is **927/927**;
native event-handler node ABI and complete MorphOS differential parity remain
progressive.

The remaining ApplicationWindowCore fixed records now use named cursors as
well: the 24-byte input-handler node and private SetConfigItem state no longer
use nonzero guest-memory offsets. The source-level offset audit is empty and
host coverage is **929/929**; native ApplicationWindowCore ABI and complete
MorphOS differential parity remain progressive.

The Listtree.mcc fixed records now use named field cursors: the 48-byte
header, mixed-width 64-byte tree node, and 12-byte TestPos result no longer
perform direct nonzero guest-memory field access. Host coverage is **932/932**;
native Listtree ABI and complete MorphOS differential parity remain
progressive.

The ListCore header, contiguous slot record, and image-chain record now use
named field cursors for all fixed fields, including cookie and pointer/value
validation. Host coverage is **934/934**; remaining ListCore record families,
native ABI, and complete MorphOS differential parity remain progressive.

ListCore edit state and column geometry now use named field cursors as well,
including signed row/column and pointer fields. Host coverage is **936/936**;
remaining ListCore record families, native ABI, and complete MorphOS
differential parity remain progressive.

ListCore column-metrics state now uses a named field cursor for Magic, Width,
Columns, and Values. Host coverage is **937/937**; remaining ListCore record
families, native ABI, and complete MorphOS differential parity remain
progressive.

ListCore title-array, redraw, column-visibility, column-order, and viewport
state now use the shared typed `MuiListStateFieldCursor`. Host coverage is
**938/938**; remaining ListCore records, native ABI, and complete MorphOS
differential parity remain progressive.

The Misc specialist header and Title state now use the typed
`MuiMiscRecordFieldCursor` for class, flags, notifications, page topology, and
Title scalars. Host coverage is **939/939**; remaining Misc records, native
ABI, and complete MorphOS differential parity remain progressive.

Misc Filepanel service state and owned-string slots now use the shared typed
record cursor for hook/ASL/row pointers, row counts, string pointers, and
allocation sizes. Host coverage remains **939/939**; remaining Misc records,
native ABI, and complete MorphOS differential parity remain progressive.

Misc Mccprefs, Scrmodelist, WindowPanel, and Fontdisplay state now use the
shared typed record cursor for registry/mode data, application/window pointers,
and natural-size values. Host coverage remains **939/939**; remaining Misc
records, native ABI, and complete MorphOS differential parity remain
progressive.

Misc Title-page, Mccprefs registry, and Filepanel row records now use the same
typed cursor for handles/flags, registry pointers and scalar parameters, and
row pointers. Host coverage remains **939/939**; native Misc ABI and complete
MorphOS differential parity remain progressive.

The complete 108-byte Pop* specialist instance record now uses the named
`MuiPopSpecialistRecordFieldCursor` for class state, child and hook pointers,
popup/ASL ownership pointers, selection, and notification scalars. Mapping and
overflow checks remain intact; host coverage is **940/940**. Native
PopSpecialist ABI and complete MorphOS differential parity remain progressive.

The 32-byte MUI_PenSpec copy, 64-byte ColorSpecialist state, and 12-byte
MUI_RGBColor records now use `MuiColorRecordFieldCursor`, keyed by semantic
record kind and field. Mapping and overflow checks remain intact; host coverage
is **941/941**. Native ColorSpecialist ABI and complete MorphOS differential
parity remain progressive.

The 16-byte class-service state, 44-byte class lease, and 28-byte
`MUI_CustomClass` records now use `MuiClassRecordFieldCursor`, keyed by semantic
record kind and field. Mapping and overflow checks remain intact; host coverage
is **942/942**. Native ClassService ABI and complete MorphOS differential parity
remain progressive.

Drawing-service state, clip, refresh, pen, render-info, and RasterPort records
now use `MuiDrawingRecordFieldCursor`, keyed by semantic record kind and field.
Mapping and overflow checks remain intact; host coverage is **943/943**. Native
DrawingService ABI and complete MorphOS differential parity remain progressive.

Group forward state, child-list state, child-entry, and Exec list records now
use `MuiGroupRecordFieldCursor`, including explicit byte-width handling for Exec
list fields. Mapping and overflow checks remain intact; host coverage is
**944/944**. Native GroupChildren ABI and complete MorphOS differential parity
remain progressive.

Dirlist’s byte-total QUAD, FileInfo-like entry header, and ExAll-like scan
header now use `MuiDirlistRecordFieldCursor`; variable-length string tails remain
explicit guest pointers. Mapping and overflow checks remain intact; host
coverage is **945/945**. Native Dirlist ABI and complete MorphOS differential
parity remain progressive.

The Process dispatch header and Process/Slave sidecar now use
`MuiProcessRecordFieldCursor`, covering dispatch counts/method ids, process
state, task/name ownership, signal/error/notification fields, and mapping and
overflow checks. Host coverage is **946/946**. Native ProcessSpecialist ABI and
complete MorphOS differential parity remain progressive.

Group Init/Exit, ExitChange2, and bracket-state records now use
`MuiGroupChangeRecordFieldCursor` for method/flag decoding, nested depth, and
exit counters. The remaining fixed byte reads are intentional Group.mui
C-string identity checks. Mapping and overflow checks remain intact; host
coverage is **947/947**. Native GroupChange ABI and complete MorphOS differential
parity remain progressive.

The packed 44-byte String.mui SGWork record now uses
`MuiStringEditRecordFieldCursor`, preserving pointer/value fields and the
16-bit Code, BufferPos, NumChars, and EditOp members at their exact ABI
positions. Mapping and overflow checks remain intact; host coverage is
**948/948**. Native StringEditHook ABI and complete MorphOS differential parity
remain progressive.

The 52-byte Menustrip/Menu/Menuitem specialist sidecar now uses
`MuiMenuRecordFieldCursor` for class/depth state, owned title/shortcut storage,
flags, trigger publication, and notification counters. Mapping and overflow
checks remain intact; host coverage is **949/949**. Native MenuSpecialist ABI and
complete MorphOS differential parity remain progressive.

List TestPos results, scalar storage, Listview drag state, and the Listview
IntuiMessage pointer envelope now use the named
`MuiListInputRecordFieldCursor` and codec. The 12-byte mixed-width TestPos
record, four-byte scalar/drag fields, Intuition fields at offsets `0x14`
through `0x22`, signed 16-bit conversion, mapping, and overflow checks remain
intact; host coverage is **951/951**. Native ListInputRecords ABI and complete
MorphOS differential parity remain progressive.

Application WindowList projection state and Exec projection entries now use
named state/entry field cursors. Cookie/state validation, pointer links,
projection marker, count/capacity, generation, mapping, and overflow checks
remain intact; host coverage is **952/952**. Native Application WindowList ABI
and complete MorphOS differential parity remain progressive.

The fixed 32-byte GroupGrid specification now uses the named
`MuiGroupGridSpecFieldCursor` for columns, rows, spacing, same-size, and
centering fields. Mapping and overflow checks remain intact; host coverage is
**953/953**. Native GroupGrid specification ABI and complete MorphOS
differential parity remain progressive.

The 32-byte Area drag lifecycle state now uses the named
`MuiAreaDragStateFieldCursor` for cookie, source/target, coordinates,
qualifier, event flags, and lifecycle flags. Mapping and overflow checks remain
intact; host coverage is **954/954**. Native Area drag-state ABI and complete
MorphOS differential parity remain progressive.

`MUI_MakeObjectA` parameter prefixes and packed 20-byte `NewMenu` records now
use named mixed-width field cursors. Parameter bounds, byte/word/long field
positions, mapping, and overflow checks remain intact; host coverage is
**955/955**. Native MakeObjectA/NewMenu ABI and complete MorphOS differential
parity remain progressive.

The caller-owned 8-byte graphics `Image` geometry prefix now uses the named
`MuiImageGeometryFieldCursor`, preserving signed edge words, unsigned
dimensions, mapping, and overflow checks; host coverage is **956/956**. Native
Image geometry ABI and complete MorphOS differential parity remain progressive.

Group MoveMember, Reorder, Sort, and method-header packets now use the named
`MuiGroupOrderingPacketFieldCursor`, preserving packet-kind boundaries, method
selectors, object vectors, signed positions, mapping, and overflow checks;
host coverage is **957/957**. Native Group ordering packet ABI and complete
MorphOS differential parity remain progressive.

The 12-byte ASL service state and 16-byte requester lease records now use the
named `MuiAslRecordFieldCursor`, preserving state head/generation,
requester/type/tag links, mapping, and overflow checks; host coverage is
**958/958**. Native ASL service record ABI and complete MorphOS differential
parity remain progressive.

The 20-byte Listview click-state record now uses the named
`MuiListviewClickStateFieldCursor`, preserving cookie validation, click-column,
edge-triggered flags, click-count normalization, mapping, and overflow checks;
host coverage is **959/959**. Native Listview click-state ABI and complete
MorphOS differential parity remain progressive.

Error service state, Group page state, String QUAD high/low storage, and
Requester service state now use named field cursors. Cookie/version fields,
counters/selectors, signed-value representation, mapping, and overflow checks
remain intact; host coverage is **963/963**. Native service/value-record ABI
and complete MorphOS differential parity remain progressive.

Store iteration counters now use a named Ordinal field cursor. Dataspace and
Objectmap length publication plus scalar export scratch storage use the named
`MuiGuestUlongStorageCodec`, preserving caller-owned ULONG result semantics;
host coverage is **964/964**. Native Store ABI and complete MorphOS differential
parity remain progressive.

CommonControl choice-entry text pointers and List metric values, pointer slots,
and owned-record length headers now use named field cursors. Host coverage is
**966/966**; native single-field record ABI and complete MorphOS differential
parity remain progressive.

Application UsedClasses, Poplist, Requester parameter, Process dispatch, and
UpdateConfig object/flag slots now use named field cursors while preserving
their pointer and byte-width semantics; host coverage is **967/967**. Native
slot ABI and complete MorphOS differential parity remain progressive.

Vertical `Scale.mui` drawing now emits an integer-only centre axis and
graduated horizontal ticks for the 0%..100% range, with detail adapting to
available height. Host coverage is **968/968**; native visual parity and
complete MorphOS differential parity remain progressive.

Dtpic picture layout results now resolve Width and Height through the named
`MuiExternalDtpicLayoutFieldCursor` and
`MuiExternalDtpicLayoutFieldCursorCodec` helpers. The typed 8-byte result,
mapping/overflow checks, and existing picture-acquisition behavior remain
intact. Host coverage is **862/862**; native Dtpic layout-result field ABI and
complete MorphOS differential parity remain progressive.

ExternalWrapper setup now resolves the four MUI_RenderInfo pointers through
the named `MuiExternalRenderInfoFieldCursor` and
`MuiExternalRenderInfoFieldCursorCodec` helpers. The typed 16-byte record,
mapping/overflow checks, and existing setup behavior remain intact. Host
coverage is **863/863**; native RenderInfo field ABI and complete MorphOS
differential parity remain progressive.

The stored ExternalWrapper display environment now resolves Window, Screen,
and DrawInfo through the named `MuiExternalDisplayEnvironmentFieldCursor` and
`MuiExternalDisplayEnvironmentFieldCursorCodec` helpers. The typed 12-byte
record, mapping/overflow checks, and existing display-state behavior remain
intact. Host coverage is **864/864**; native display-environment field ABI and
complete MorphOS differential parity remain progressive.

The stored ExternalWrapper RastPort pointer now resolves through the named
`MuiExternalRastPortSlotFieldCursor` and
`MuiExternalRastPortSlotFieldCursorCodec` helpers. The typed 4-byte slot,
mapping checks, and existing display-state behavior remain intact. Host
coverage is **865/865**; native RastPort-slot field ABI and complete MorphOS
differential parity remain progressive.

Application Save/Load traversal frames now resolve Object, NextChild, and
VisitMarker through the named `MuiApplicationPersistenceFrameFieldCursor` and
`MuiApplicationPersistenceFrameFieldCursorCodec` helpers. The typed 12-byte
frame, mapping/overflow checks, and existing traversal behavior remain intact.
Host coverage is **866/866**; native persistence-frame field ABI and complete
MorphOS differential parity remain progressive.

Application settings headers now resolve MagicValue, VersionValue, RecordCount,
and PayloadBytes through the named `MuiApplicationSettingsHeaderFieldCursor`
and `MuiApplicationSettingsHeaderFieldCursorCodec` helpers. The typed 16-byte
header, mapping/overflow checks, and existing settings-file behavior remain
intact. Host coverage is **867/867**; native settings-header field ABI and
complete MorphOS differential parity remain progressive.

Application settings records now resolve Key and Length through the named
`MuiApplicationSettingsRecordFieldCursor` and
`MuiApplicationSettingsRecordFieldCursorCodec` helpers. The typed 8-byte
record, mapping/overflow checks, and existing settings-file behavior remain
intact. Host coverage is **868/868**; native settings-record field ABI and
complete MorphOS differential parity remain progressive.

Dataspace IFF entry headers now resolve Id and Length through the named
`MuiDataspaceIffEntryHeaderFieldCursor` and
`MuiDataspaceIffEntryHeaderFieldCursorCodec` helpers. The typed 8-byte header,
mapping/overflow checks, and existing IFF streaming behavior remain intact.
Host coverage is **869/869**; native IFF entry-header field ABI and complete
MorphOS differential parity remain progressive.

Dataspace WriteIFF messages now resolve MethodId, Handle, Type, and Id through
the named `MuiDataspaceWriteIffFieldCursor` and
`MuiDataspaceWriteIffFieldCursorCodec` helpers. The typed 16-byte packet,
method validation, mapping/overflow checks, and existing IFF behavior remain
intact. Host coverage is **870/870**; native WriteIFF message-field ABI and
complete MorphOS differential parity remain progressive.

Dataspace ReadIFF messages now resolve MethodId and Handle through the named
`MuiDataspaceReadIffFieldCursor` and
`MuiDataspaceReadIffFieldCursorCodec` helpers. The typed 8-byte packet, method
validation, mapping/overflow checks, and existing IFF behavior remain intact.
Host coverage is **871/871**; native ReadIFF message-field ABI and complete
MorphOS differential parity remain progressive.

The shared Dataspace IFF method word now resolves through the named
`MuiDataspaceIffMethodFieldCursor` and
`MuiDataspaceIffMethodFieldCursorCodec` helpers. Method-header decoding,
mapping checks, and existing IFF behavior remain intact. Host coverage is
**872/872**; native IFF method-field ABI and complete MorphOS differential
parity remain progressive.

The shared Store/Dataspace record now resolves Next, Key, Data, Length, Flags,
and Generation through the named `MuiStoreRecordFieldCursor` and
`MuiStoreRecordFieldCursorCodec` helpers. The typed 24-byte record,
mapping/overflow checks, and existing Datamap/Objectmap behavior remain
intact. Host coverage is **873/873**; native Store/Dataspace record-field ABI
and complete MorphOS differential parity remain progressive.

The fixed 32-byte headless-state record now resolves `Magic`, `Version`,
`Classes`, `Objects`, `NextSequence`, `NotifyDepth`, `Mutation`, and `Reserved`
through the named `MuiHeadlessStateFieldCursor` and
`MuiHeadlessStateFieldCursorCodec` helpers. Class/object registry and state
semantics, mapping checks, and overflow rejection remain intact. Host coverage
is **874/874**; native headless-state record-field ABI and complete MorphOS
differential parity remain progressive.

The fixed 28-byte headless-class registry record now resolves `Next`, `Name`,
`Boopsi`, `Super`, `InstanceSize`, `Reserved`, `Flags`, and `ObjectCount`
through the named `MuiHeadlessClassFieldCursor` and
`MuiHeadlessClassFieldCursorCodec` helpers, preserving the mixed UWORD/ULONG
ABI. Class registry semantics, mapping checks, and overflow rejection remain
intact. Host coverage is **875/875**; native headless-class record-field ABI
and complete MorphOS differential parity remain progressive.

The fixed 64-byte headless-object record now resolves all sixteen pointer and
scalar fields through the named `MuiHeadlessObjectFieldCursor` and
`MuiHeadlessObjectFieldCursorCodec` helpers. Object topology, semaphore,
notification, and Store/Dataspace semantics, mapping checks, and overflow
rejection remain intact. Host coverage is **876/876**; native headless-object
record-field ABI and complete MorphOS differential parity remain progressive.

The fixed 16-byte headless-attribute record now resolves `Next`, `Id`, `Value`,
and `Generation` through the named `MuiHeadlessAttributeFieldCursor` and
`MuiHeadlessAttributeFieldCursorCodec` helpers. Attribute mutation and
notification semantics, mapping checks, and overflow rejection remain intact.
Host coverage is **877/877**; native headless-attribute record-field ABI and
complete MorphOS differential parity remain progressive.

The fixed 16-byte Family child-list record now resolves `Next`, `Previous`,
`Object`, and `Owner` through the named `MuiHeadlessChildFieldCursor` and
`MuiHeadlessChildFieldCursorCodec` helpers. Family topology and mutation
semantics, mapping checks, and overflow rejection remain intact. Host coverage
is **878/878**; native headless-child record-field ABI and complete MorphOS
differential parity remain progressive.

The fixed 32-byte headless notification header now resolves all eight fields
through the named `MuiHeadlessNotificationFieldCursor` and
`MuiHeadlessNotificationFieldCursorCodec` helpers while retaining the bounded
payload cursor. Notification sequencing, trigger, destination, follow, and
payload semantics, mapping checks, and overflow rejection remain intact. Host
coverage is **879/879**; native notification-header record-field ABI and
complete MorphOS differential parity remain progressive.

The shared four-byte `MuiGuestUlongStorage.Value` result slot now resolves
through `MuiGuestUlongStorageFieldCursor` and
`MuiGuestUlongStorageFieldCursorCodec` across specialist, external-wrapper,
common-control, and notification publication paths. Mapping checks and
overflow rejection remain intact. Host coverage is **880/880**; native
guest-ULONG storage ABI and complete MorphOS differential parity remain
progressive.

The shared 8-byte ASL `MuiAslTagItemRecord` now resolves `Tag` and `Data`
through `MuiAslTagItemFieldCursor` and `MuiAslTagItemFieldCursorCodec` while
retaining vector traversal and control-tag semantics. Even-address and mapping
checks remain intact. Host coverage is **881/881**; native ASL TagItem field
ABI and complete MorphOS differential parity remain progressive.

The 12-byte `MuiMinMaxValues` layout record now resolves all six signed UWORD
geometry fields through `MuiMinMaxFieldCursor` and
`MuiMinMaxFieldCursorCodec`, preserving AskMinMax semantics. Mapping checks and
overflow rejection remain intact. Host coverage is **882/882**; native MinMax
layout-field ABI and complete MorphOS differential parity remain progressive.

The fixed 36-byte application-command descriptor now resolves all nine fields
through `MuiApplicationCommandFieldCursor` and
`MuiApplicationCommandFieldCursorCodec` while retaining NULL-terminated table
validation. Mapping checks, signed parameter/reserved values, and overflow
rejection remain intact. Host coverage is **883/883**; native
application-command descriptor ABI and complete MorphOS differential parity
remain progressive.

The 20-byte AppMessage Exec node header now resolves all seven mixed-width
fields through `MuiAppMessageNodeFieldCursor` and
`MuiAppMessageNodeFieldCursorCodec`, preserving links, signed priority, and
message type/length semantics. Mapping checks and overflow rejection remain
intact. Host coverage is **884/884**; native AppMessage node-header ABI and
complete MorphOS differential parity remain progressive.

The fixed 8-byte `MuiWorkbenchArgumentRecord` now resolves `Lock` and `Name`
through `MuiWorkbenchArgumentFieldCursor` and
`MuiWorkbenchArgumentFieldCursorCodec` while retaining argument-vector
validation. BPTR/STRPTR semantics, mapping checks, and overflow rejection
remain intact. Host coverage is **885/885**; native Workbench-argument record
ABI and complete MorphOS differential parity remain progressive.

The fixed 86-byte AppMessage record body now resolves all nineteen mixed-width
fields through `MuiAppMessageFieldCursor` and
`MuiAppMessageFieldCursorCodec`, while node-header delegation remains intact.
Scalar, pointer, signed-coordinate, reserved-field, mapping, and overflow
behavior remain intact. Host coverage is **886/886**; native AppMessage
body-field ABI and complete MorphOS differential parity remain progressive.

The fixed MorphOS Area drag method packets now resolve their mixed-width
fields through `MuiAreaDragFieldCursor` and
`MuiAreaDragFieldCursorCodec`, keyed by packet kind. Method selectors,
pointer/value payloads, signed coordinates, packet-size checks, mapping checks,
and overflow behavior remain intact. Host coverage is **887/887**; native
Area drag packet ABI and complete MorphOS differential parity remain
progressive.

The fixed MorphOS List basic packets now resolve their fields through
`MuiCollectionBasicFieldCursor` and
`MuiCollectionBasicFieldCursorCodec`, keyed by packet kind. Method selectors,
positions, selection/storage values, packet-size checks, mapping checks, and
overflow behavior remain intact. Host coverage is **888/888**; native
Collection basic packet ABI and complete MorphOS differential parity remain
progressive.

The fixed MorphOS List advanced packets now resolve their fields through
`MuiCollectionAdvancedFieldCursor` and
`MuiCollectionAdvancedFieldCursorCodec`, keyed by packet kind. Method-group
validation, entry/position/column/pointer/pair/image payloads, packet-size
checks, mapping checks, and overflow behavior remain intact. Host coverage is
**889/889**; native Collection advanced packet ABI and complete MorphOS
differential parity remain progressive.

The fixed MorphOS collection surface packets now resolve their fields through
`MuiCollectionSurfaceFieldCursor` and
`MuiCollectionSurfaceFieldCursorCodec`, keyed by packet kind. Layout geometry,
storage/flags, IntuiMessage and signed MUI key payloads, attribute values,
packet-size checks, mapping checks, and overflow behavior remain intact. Host
coverage is **890/890**; native Collection surface packet ABI and complete
MorphOS differential parity remain progressive.

The fixed MorphOS List record packets now resolve their fields through
`MuiCollectionRecordFieldCursor` and
`MuiCollectionRecordFieldCursorCodec`, keyed by packet kind. Entry/pool,
display, compare, and hit-test payloads, method validation, packet-size
checks, mapping checks, and overflow behavior remain intact. Host coverage is
**891/891**; native Collection record packet ABI and complete MorphOS
differential parity remain progressive.

The fixed MorphOS List edit packets now resolve their fields through
`MuiCollectionEditFieldCursor` and
`MuiCollectionEditFieldCursorCodec`, keyed by packet kind. Signed row/column
conversion, entry/edit-object and mode payloads, method validation, packet-size
checks, mapping checks, and overflow behavior remain intact. Host coverage is
**892/892**; native Collection edit packet ABI and complete MorphOS
differential parity remain progressive.

The fixed MorphOS Listtree.mcc packets now resolve their fields through
`MuiListtreeFieldCursor` and `MuiListtreeFieldCursorCodec`, keyed by packet
kind. Pointer/value payloads, method-group validation, packet-size checks,
mapping checks, and overflow behavior remain intact. Host coverage is
**893/893**; native Listtree packet ABI and complete MorphOS differential
parity remain progressive.

The fixed MorphOS Dirlist/Volumelist packets now resolve their fields through
`MuiDirlistFieldCursor` and `MuiDirlistFieldCursorCodec`, keyed by packet kind.
Pointer/value payloads, method-group validation, packet-size checks, mapping
checks, and overflow behavior remain intact. Host coverage is **894/894**;
native Dirlist packet ABI and complete MorphOS differential parity remain
progressive.

The fixed MorphOS Process.mui/Slave.mui packets now resolve their fields
through `MuiProcessSpecialistFieldCursor` and
`MuiProcessSpecialistFieldCursorCodec`, keyed by packet kind.
Attribute/storage/value, signal, error, and dispatch payloads, method
validation, packet-size checks, mapping checks, and overflow behavior remain
intact. Host coverage is **895/895**; native Process/Slave packet ABI and
complete MorphOS differential parity remain progressive.

The fixed MorphOS pen/color specialist packets now resolve their fields through
`MuiColorSpecialistFieldCursor` and
`MuiColorSpecialistFieldCursorCodec`, keyed by packet kind.
Attribute/storage/value, pointer, and RGB payloads, method validation,
packet-size checks, mapping checks, and overflow behavior remain intact. Host
coverage is **896/896**; native color-specialist packet ABI and complete
MorphOS differential parity remain progressive.

The fixed MorphOS Popstring/Popobject/Popasl packets now resolve their fields
through `MuiPopSpecialistFieldCursor` and
`MuiPopSpecialistFieldCursorCodec`, keyed by packet kind.
Attribute/storage/value and close-result payloads, the tolerant method-only
close boundary, method validation, packet-size checks, mapping checks, and
overflow behavior remain intact. Host coverage is **897/897**; native Pop
specialist packet ABI and complete MorphOS differential parity remain
progressive.

The fixed MorphOS Menustrip/Menu/Menuitem packets now resolve their fields
through `MuiMenuSpecialistFieldCursor` and
`MuiMenuSpecialistFieldCursorCodec`, keyed by packet kind.
Attribute/storage/value, object-pointer, pair, and popup-coordinate payloads,
method validation, packet-size checks, mapping checks, and overflow behavior
remain intact. Host coverage is **898/898**; native menu-specialist packet ABI
and complete MorphOS differential parity remain progressive.

The fixed MorphOS `MUIP_GoActive`/`MUIP_GoInactive` packet now resolves its
method and flags through `MuiAreaActivationFieldCursor` and
`MuiAreaActivationFieldCursorCodec`, keyed by packet kind. Active/inactive
validation, packet-size checks, mapping checks, and overflow behavior remain
intact. Host coverage is **899/899**; native Area activation packet ABI and
complete MorphOS differential parity remain progressive.

The fixed MorphOS layout packets now resolve their fields through
`MuiLayoutFieldCursor` and `MuiLayoutFieldCursorCodec`, keyed by packet kind.
Storage, flags, geometry, reserved words, text, length, and RenderInfo
payloads, method validation, packet-size checks, mapping checks, and overflow
behavior remain intact. Host coverage is **900/900**; native layout packet ABI
and complete MorphOS differential parity remain progressive.

The fixed MorphOS Misc specialist packets now resolve their fields through
`MuiMiscSpecialistFieldCursor` and
`MuiMiscSpecialistFieldCursorCodec`, keyed by packet kind.
Attribute/storage/value, pointer, pair, and register-gadget payloads,
lifecycle and method validation, packet-size checks, mapping checks, and
overflow behavior remain intact. Host coverage is **901/901**; native Misc
specialist packet ABI and complete MorphOS differential parity remain
progressive.

The fixed MorphOS common-control packets now resolve their fields through
`MuiCommonFieldCursor` and `MuiCommonFieldCursorCodec`, keyed by packet kind.
Signed values, geometry, input/event payloads, storage, attribute/value
payloads, method validation, packet-size checks, mapping checks, and overflow
behavior remain intact. Host coverage is **902/902**; native common-control
packet ABI and complete MorphOS differential parity remain progressive.

The fixed MorphOS Boopsi.mui/Dtpic.mui wrapper packets now resolve their fields
through `MuiExternalWrapperFieldCursor` and
`MuiExternalWrapperFieldCursorCodec`, keyed by packet kind.
Attribute-list, gadget-info, flags, attribute/storage/value, RenderInfo, and
geometry payloads, method validation, packet-size checks, mapping checks, and
overflow behavior remain intact. Host coverage is **903/903**; native
external-wrapper packet ABI and complete MorphOS differential parity remain
progressive.

The fixed MorphOS Dataspace packets now resolve their fields through
`MuiDataspaceFieldCursor` and `MuiDataspaceFieldCursorCodec`, keyed by packet
kind. Data, signed length, ID, size-storage, and Dataspace pointer payloads,
method validation, packet-size checks, mapping checks, and overflow behavior
remain intact. Host coverage is **904/904**; native Dataspace packet ABI and
complete MorphOS differential parity remain progressive.

Boopsi OpSet, OpGet, Render, TagItem, and result workspace fields now resolve
through the semantic packet-field cursor. Method selectors, payloads, record
sizes, mapping validation, and overflow behavior remain intact. Host coverage
is **861/861**; native workspace packet ABI and complete MorphOS differential
parity remain progressive.

ExternalWrapper notification Attribute, Value, and Count now resolve through
the named notification-field cursor. Recording/query semantics, mapping
validation, and overflow behavior remain intact. Host coverage is **860/860**;
native notification-field ABI and complete MorphOS differential parity remain
progressive.

ExternalWrapper shared scratch ownership now resolves through the named
scratch-field cursor. RememberBuffer, RememberCount, WorkBuffer, mapping
validation, and overflow behavior remain intact. Host coverage is **859/859**;
native scratch-field ABI and complete MorphOS differential parity remain
progressive.

Dtpic sidecar ownership, attribute, and dimension fields now resolve through
the named Dtpic state-field cursor while retaining the typed state record. The
nine fields, mapping validation, and overflow behavior remain intact. Host
coverage is **858/858**; native Dtpic state-field ABI and complete MorphOS
differential parity remain progressive.

Boopsi resource ownership/input pointers now resolve through the named
resource-field cursor while retaining the typed resource record. All five
pointers, full-record mapping validation, and overflow behavior remain intact.
Host coverage is **857/857**; native resource-field ABI and complete MorphOS
differential parity remain progressive.

Boopsi geometry/configuration fields now resolve through the named geometry
field cursor while retaining the typed state record. All Min/Max and tag
values, full-record mapping validation, and overflow behavior remain intact.
Host coverage is **856/856**; native geometry-field ABI and complete MorphOS
differential parity remain progressive.

ExternalWrapper Magic, Class, and Flags header words now resolve through the
named header-field cursor. Cookie/class validation, lifecycle flag updates,
the 12-byte mapping contract, and overflow behavior remain intact. Host
coverage is **855/855**; native header-field ABI and complete MorphOS
differential parity remain progressive.

ExternalWrapper stored display state now composes named display-environment and
RastPort records. The 12-byte environment span, 4-byte RastPort slot, mapping
checks, and setup/cleanup behavior remain intact. Host coverage is **854/854**;
native display-record ABI and complete MorphOS differential parity remain
progressive.

Dtpic picture-layout dimensions now decode through the named
`MuiExternalDtpicLayoutResult` and codec. The 8-byte width/height record,
mapped-result validation, and failure-atomic picture publication remain intact.
Host coverage is **853/853**; native Dtpic layout-result ABI and complete
MorphOS differential parity remain progressive.

ExternalWrapper MUIM_Setup now decodes the optional four-pointer `MUI_RenderInfo`
input through the named `MuiExternalRenderInfoRecord` and codec. The 16-byte
layout and permissive null/unmapped behavior remain intact. Host coverage is
**852/852**; native RenderInfo ABI and complete MorphOS differential parity
remain progressive.

Boopsi work-buffer inline TagItem/result resolution now uses the named
`MuiExternalWorkRegionCursor` and `MuiExternalWorkRegionCursorCodec` helpers.
The fixed 16-byte inline region, 40-byte mapping contract, shared packet/result
address, and overflow behavior remain intact. Host coverage is **851/851**;
native ExternalWrapper work-region ABI and complete MorphOS differential parity
remain progressive.

ExternalWrapper fixed Boopsi/Dtpic sidecar regions now resolve through the
named `MuiExternalStateCursor` and `MuiExternalStateCursorCodec` helpers. The
documented seven-region instance layout, mapping checks, and overflow behavior
remain intact. Host coverage is **850/850**; native ExternalWrapper region ABI
and complete MorphOS differential parity remain progressive.

Misc specialist fixed-region access now uses the named `MuiMiscStateCursor` and
`MuiMiscStateCursorCodec.TryGetAddress` helpers. Title, Filepanel, Mccprefs,
Scrmodelist, Window/Panel, Protection, and Fontdisplay state access retain the
documented instance layout through semantic regions with centralized overflow
checks. Host coverage is **839/839**; native Misc state-region ABI and complete
MorphOS differential parity remain progressive.

Misc specialist owned-string slot access now uses the named
`MuiMiscOwnedStringCursor` and `MuiMiscOwnedStringCursorCodec.TryGetAddress`
helpers. Keyadjust, Argstring, and Filepanel retain their sparse instance
layout through semantic fields with centralized overflow checks, while the
8-byte slot record and failure-atomic ownership remain intact. Host coverage is
**840/840**; native Misc owned-string ABI and complete MorphOS differential
parity remain progressive.

Application PushMethod parameter-span validation now uses the named
`MuiApplicationPushMethodParameterCursor` and
`MuiApplicationPushMethodParameterCursorCodec.TryGetEntry` helpers. The fixed
12-byte packet header, 4-byte parameter records, seven-entry bound, mapping,
and overflow checks remain intact. Host coverage is **841/841**; native
PushMethod tail ABI and complete MorphOS differential parity remain progressive.

CallHook param1 and variadic parameter access now use the named
`MuiCallHookParameterCursor` and `MuiCallHookParameterCursorCodec.TryGetEntry`
helpers. The 12-byte packet envelope, 4-byte parameter records, caller-owned
tail, mapping, and overflow checks remain intact. Host coverage is **842/842**;
native CallHook parameter ABI and complete MorphOS differential parity remain
progressive.

Notify follow-parameter and MultiSet target-vector bases now use the named
`MuiNotifyInlineVectorCursor` and `MuiNotifyInlineVectorCursorCodec.TryGetAddress`
helpers. Semantic vector kinds preserve the fixed message headers, 4-byte ULONG
entries, kind-specific boundaries, and overflow checks. Host coverage is
**843/843**; native Notify inline-vector ABI and complete MorphOS differential
parity remain progressive.

Window cycle-chain packet vector lookup now uses the named
`MuiWindowCycleChainInlineVectorCursor` and
`MuiWindowCycleChainInlineVectorCodec.TryGetEntry` helpers, layered over the
typed cycle-chain slot cursor. The fixed 8-byte header, 4-byte object slots,
NULL termination, mapping, and overflow checks remain intact. Host coverage is
**844/844**; native cycle-chain vector ABI and complete MorphOS differential
parity remain progressive.

Application window-node payload access now uses the named
`MuiApplicationWindowNodePayloadCursor` and
`MuiApplicationWindowNodePayloadCursorCodec.TryGetAddress` helpers. The
20-byte node record, Packet-field boundary, requested byte-count mapping, and
overflow checks remain intact. Host coverage is **845/845**; native window-node
payload ABI and complete MorphOS differential parity remain progressive.

Headless notification payload access now uses the named
`MuiHeadlessNotificationPayloadCursor` and
`MuiHeadlessNotificationPayloadCursorCodec.TryGetAddress` helpers. The fixed
32-byte record, variable payload count, total-range mapping, zero-length payload
support, and overflow checks remain intact. Host coverage is **846/846**;
native notification payload ABI and complete MorphOS differential parity remain
progressive.

SetAsString Value address lookup now uses the named
`MuiSetAsStringValueCursor` and `MuiSetAsStringValueCursorCodec.TryGetAddress`
helpers. The 16-byte packet record, 4-byte value field, formatter-facing guest
address, mapping, and overflow checks remain intact. Host coverage is
**847/847**; native SetAsString value ABI and complete MorphOS differential
parity remain progressive.

Process/Slave dispatch argument access now uses the named
`MuiProcessArgumentCursor` and `MuiProcessArgumentCursorCodec.TryGetEntry`
helpers, with semantic kinds for caller packets and generated method messages.
The 8-byte/4-byte headers, bounded 4-byte argument slots, mapping, and overflow
checks remain intact. Host coverage is **848/848**; native Process dispatch ABI
and complete MorphOS differential parity remain progressive.

Requester format parameter reads now use the named
`MuiRequesterParameterCursor` and `MuiRequesterParameterCursorCodec.TryGetEntry`
helpers. The 4-byte slot layout, 2048-entry bound, caller-owned storage,
mapping, and overflow checks remain intact. Host coverage is **849/849**;
native requester parameter ABI and complete MorphOS differential parity remain
progressive.

List selection and `NextSelected` caller-owned four-byte storage now use the
named `MuiListScalarStorageRecord` and `MuiListScalarStorageCodec`. Host
coverage is **782/782**; native list-storage ABI and differential parity remain
progressive.

List measured-width slots now use the named `MuiListColumnMetricValue` and
`MuiListColumnMetricCodec`; title construction and display-array paths reuse
the named pointer-slot codec and size boundary. Host coverage is **783/783**;
native List metric ABI and differential parity remain progressive.

`MuiListColumnMetricsState.Values` is now a named `APTR` field decoded by
`MuiListColumnMetricsStateCodec`; metric lookup, cleanup, and publication no
longer convert the state pointer ad hoc. Host coverage is **784/784**; native
List metrics-state ABI and differential parity remain progressive.

List edit-session `Entry` and `EditObject` fields now use named `APTR` members
through `MuiListEditStateCodec`. Host coverage is **785/785**; native List
edit-state ABI and differential parity remain progressive.

Dynamic `MUIM_UpdateConfig` redraw-table flag writes now use the named
`MuiUpdateConfigFlagSlot` and `MuiUpdateConfigFlagSlotCodec` alongside the
object slot. Host coverage is **759/759**; native UpdateConfig redraw-table
ABI and complete MorphOS differential parity remain progressive.

Group reorder/sort vector reads now resolve entries through the named
`MuiFamilyMutationVectorCursor` and
`MuiFamilyMutationVectorCodec.TryGetEntry` helpers. The 4-byte object-pointer
layout, NULL termination, traversal bound, and malformed-range rejection
remain intact. Host coverage is **822/822**; native Group ordering vector ABI
and complete MorphOS differential parity remain progressive.

Headless object creation now walks TAG_IGNORE, TAG_MORE, and TAG_SKIP through
the named `MuiAslTagItemCursor` and `MuiAslTagItemVectorCodec` helpers. The
8-byte TagItem layout, bounded traversal, skip-count overflow rejection, and
malformed-range behavior remain intact. Host coverage is **822/822**; native
headless tag-walk ABI and complete MorphOS differential parity remain
progressive.

Poplist caller-array traversal, materialized-copy/terminator writes, and
selection lookup now use the named `MuiPoplistArrayCursor` and
`MuiPoplistArrayCursorCodec.TryGetEntry` helpers. The 4-byte STRPTR-slot layout,
1024-entry source bound plus terminator, ownership behavior, and malformed-range
checks remain intact. Host coverage is **832/832**; native Poplist array ABI and
complete MorphOS differential parity remain progressive.

List TitleArray/StringArray pointer-table reads and writes now resolve slots
through the named `MuiListPointerSlotCursor` and
`MuiListPointerSlotCursorCodec.TryGetEntry` helpers. The 4-byte pointer-slot
layout, bounded column behavior, terminators, ownership/copy behavior, and
malformed-range rejection remain intact. Host coverage is **823/823**; native
List pointer-table ABI and complete MorphOS differential parity remain
progressive.

List entry-index reads, writes, and destruction now resolve addresses through
the named `MuiListSlotCursor` and `MuiListSlotCursorCodec.TryGetEntry` helpers.
The 8-byte entry/flags layout, one-million-entry bound, O(1) access, ownership
flags, and malformed-range rejection remain intact. Host coverage is **824/824**;
native List index-slot ABI and complete MorphOS differential parity remain
progressive.

StringArray materialization, comparison, ownership cleanup, and display-array
reads/writes now resolve bounded slots through the named
`MuiListPointerSlotCursor` and `MuiListPointerSlotCursorCodec.TryGetEntry`
helpers. The 4-byte pointer-slot layout, 256-entry bound plus terminator,
display-column behavior, and malformed-range rejection remain intact. Host
coverage is **825/825**; native StringArray display-table ABI and complete
MorphOS differential parity remain progressive.

Caller-supplied entry vectors for List Insert, SourceArray materialization,
and SortEntries now resolve slots through the named
`MuiListPointerVectorCursor` and `MuiListPointerVectorCursorCodec.TryGetEntry`
helpers. The 4-byte pointer-slot layout, one-million-entry bound, NULL
termination, failure-atomic behavior, and malformed-range rejection remain
intact. Host coverage is **826/826**; native caller-vector ABI and complete
MorphOS differential parity remain progressive.

Measured List column-width reads and refresh writes now resolve entries through
the named `MuiListColumnMetricCursor` and
`MuiListColumnMetricCursorCodec.TryGetEntry` helpers. The 4-byte ULONG metric
layout, 64-column geometry bound, width-refresh behavior, and malformed-range
rejection remain intact. Host coverage is **827/827**; native column-metric ABI
and complete MorphOS differential parity remain progressive.

FORMAT descriptor build, validation, lookup, and cleanup now resolve records
through the named `MuiListFormatDescriptorCursor` and
`MuiListFormatDescriptorCursorCodec.TryGetEntry` helpers. The 40-byte
descriptor layout, 256-column bound, ReadArgs parsing, ownership cleanup, and
malformed-range rejection remain intact. Host coverage is **828/828**; native
FORMAT descriptor ABI and complete MorphOS differential parity remain
progressive.

Public geometry projection and cached layout reads now resolve records through
the named `MuiListColumnGeometryCursor` and
`MuiListColumnGeometryCursorCodec.TryGetEntry` helpers. The 8-byte offset/width
layout, 64-column bound, layout caching, editor-placement behavior, and
malformed-range rejection remain intact. Host coverage is **829/829**; native
column-geometry ABI and complete MorphOS differential parity remain
progressive.

ColumnOrder source parsing, guest-owned comparison, cleanup, and display lookup
now resolve bytes through the named `MuiListColumnOrderByteCursor` and
`MuiListColumnOrderByteCursorCodec.TryGetEntry` helpers. The caller-facing
BYTE* permutation, 64-column bound, packed big-endian storage, identity
completion, duplicate/range validation, and malformed-range rejection remain
intact. Host coverage is **830/830**; native ColumnOrder ABI and complete
MorphOS differential parity remain progressive.

`MUIM_UpdateConfig` redraw-object and redraw-flag table reads and writes now
resolve slots through the named `MuiUpdateConfigObjectCursor`/
`MuiUpdateConfigObjectCursorCodec` and `MuiUpdateConfigFlagCursor`/
`MuiUpdateConfigFlagCursorCodec` helpers. The 332-byte packet, 64-entry tables,
named packet fields, and malformed-range rejection remain intact. Host coverage
is **831/831**; native UpdateConfig table ABI and complete MorphOS differential
parity remain progressive.

`MUIM_Slave_Dispatch` now crosses the named
`MuiProcessDispatchPacketHeader`/`MuiProcessDispatchPacketCodec` boundary.
The codec validates the bounded argument vector and exposes each argument as
a value, keeping raw guest offsets out of the live Process/Slave dispatch
logic. Host coverage is **737/737**; native packet ABI and complete MorphOS
differential parity remain progressive.

## NotifyWrite typed method headers

NotifyWrite Long and String packet decoding now uses the named
`MuiNotifyWriteMethodMessage` codec before consuming bounded write records.
Host coverage is **730/730**; native NotifyWrite packet ABI and complete
MorphOS differential parity remain progressive.

## Dataspace and Dataspace-IFF typed method headers

Dataspace and Dataspace-IFF packet decoding now use their named method header
codecs before consuming typed Add/Find/Get/Merge/Remove/Clear and Read/Write
records. Host coverage is **732/732**; native Dataspace packet ABI and complete
MorphOS differential parity remain progressive.

## Layout typed method headers

Layout packet decoding now uses `MuiLayoutMethodMessage` before consuming
typed AskMinMax, Relayout, rectangle, text, render-info, flags, and layout
records. Host coverage is **733/733**; native Layout packet ABI and complete
MorphOS differential parity remain progressive.

## Listtree typed method headers

Listtree packet decoding now uses `MuiListtreeMethodMessage` before consuming
typed set/get, insert/remove, open/close, sorting, movement, rename, find,
drop-mark, and test-position records. Host coverage is **734/734**; native
Listtree packet ABI and complete MorphOS differential parity remain progressive.

When `MuiEventHandlerNode.Class` is non-null, the callback seam now follows the
MorphOS contract and invokes `CoerceMethod(Class, Object, message)` directly;
when it is null, it uses the normal `DoMethod(Object, message)` path. The
platform contract keeps this as an opaque named class pointer, with no managed
dispatcher or exception path. Host coverage is **575/575**; priority ordering,
active/default-object precedence, richer return-code propagation, and full
MorphOS event parity remain progressive.

Event-handler registration now decodes the signed MorphOS priority byte and
keeps the guest wrapper list in descending priority order with FIFO ties.
Window delivery checks the active object first, then the default object, and
then the remaining queue. This ordering uses bounded named-record passes with
no managed arrays or exception path. Host coverage is **576/576**; GUI-state
visibility checks, richer return-code propagation, and full MorphOS event
parity remain progressive.

## Collection surface method headers

Shared Layout, AskMinMax, Draw, HandleInput, and attribute packet checks now
consume the named collection method header before decoding their typed records.
Host coverage is **701/701**; native surface packet ABI and complete MorphOS
differential parity remain progressive.

## Collection-basic method header

Collection Clear and Sort packet decoding now uses the named
`MuiCollectionMethodMessage` codec before accepting the selector. Host
coverage is **726/726**; native collection-basic packet ABI and complete
MorphOS differential parity remain progressive.

## Dirlist/Volumelist typed method headers

Dirlist/Volumelist packet decoding now uses the named `MuiDirlistMethodMessage`
codec before consuming method-only, set, rename, protection, and get-entry
records. Host coverage is **727/727**; native Dirlist/Volumelist packet ABI
and complete MorphOS differential parity remain progressive.

Family reorder/sort now obtains each object-pointer vector entry through the
named `MuiFamilyMutationVectorCursor` and
`MuiFamilyMutationVectorCodec.TryGetEntry` helpers, preserving NULL
termination, ordering, bounded mapping, overflow checks, and malformed-range
rejection. Host coverage is **807/807**; native Family vector ABI and complete
MorphOS differential parity remain progressive.

## External-wrapper typed method headers

External-wrapper packet decoding now uses the named `MuiExternalMethodMessage`
codec before consuming method-only, set, render-info, and sized records. Host
coverage is **728/728**; native external-wrapper packet ABI and complete
MorphOS differential parity remain progressive.

## Menu-specialist typed method headers

MenuSpecialist packet decoding now uses the named
`MuiMenuSpecialistMethodMessage` codec before consuming method-only, set,
pointer, pair, popup, and sized records. Host coverage is **729/729**; native
MenuSpecialist packet ABI and complete MorphOS differential parity remain
progressive.

## Common-control typed method headers

Common-control signed, numeric, event, attribute, layout, draw, setup, and
OM_GET readers now route selector checks through the named common method header
before decoding typed records. Host coverage is **702/702**; native
common-control packet ABI and complete MorphOS differential parity remain
progressive.

GUI-mode event-handler eligibility now uses the corrected MorphOS
`MUI_EHF_GUIMODE` flag value **0x0002**. Handlers targeting disabled objects,
objects with `MUIA_ShowMe == 0`, or objects whose internal `IsShown` state is
zero are skipped. ACTIVEWINDOW, INACTIVEWINDOW, and CHANGEWINDOW event classes
remain eligible as MorphOS exceptions; non-GUI handlers retain the normal
mask/object path. The gate uses named attributes and typed records without
exceptions, managed runtime, or raw handler-node offsets. Host coverage is
**577/577**; virtual-group ancestry and richer return-code propagation remain
progressive.

The typed event-handler callback now preserves the complete
`DoMethod`/`CoerceMethod` result. Window delivery stops only for the MorphOS
`MUI_EventHandlerRC_Eat` value **1**; other non-zero values remain observable
and do not prevent the remaining priority queue from running. Host coverage is
**578/578**; virtual-group ancestry and complete MorphOS event parity remain
progressive.

GUI-mode eligibility now follows the named `Parent` chain in each live
`MuiHeadlessObjectRecord`. Disabled, hidden, or not-shown ancestors suppress
delivery just like the target object, while window-state exception classes
remain eligible. The traversal is bounded and uses no managed collection or
exception path. Host coverage is **579/579**; full MorphOS group visibility
semantics remain progressive.

The typed window event walk now honors MorphOS `MUI_EHF_ALWAYSKEYS = 0x0001`.
For a named `MUIP_HandleEvent` packet, `MuiKey != MUIKEY_NONE` marks keyboard
delivery; inactive handlers are admitted only when their
`MuiEventHandlerNode.Flags` includes `ALWAYSKEYS`. Active/default-object passes
and non-key IDCMP events retain the ordinary route. The focused fixtures use
`MuiWindowEventHandlerPacketInput`, `MuiEventHandlerNodeInput`, and the named
HandleEvent codec rather than inventing raw handler-node records. Host coverage
is **580/580**; `WindowEventHandlerRoot` emits **52,972 / 60,004 / 56,364
bytes** for MC68000/020/040, reaches 164 methods, and MC68000 returns **42**
after **1,455,700 instructions / 15,256,342 cycles**. Full MorphOS key
routing, virtual-group, and differential event behavior remain progressive.

The typed window event walk now applies MorphOS page-mode visibility. While
walking the named `MuiHeadlessObjectRecord.Parent` chain, a parent with
`MUIA_Group_PageMode != 0` admits only its `MUIA_Group_ActivePage` child; an
inactive page and all descendants are skipped for GUI-mode callbacks. Page
membership uses bounded named family records and does not mutate caller
attributes or allocate managed state. Host coverage is **581/581**.
`WindowEventHandlerRoot` emits **54,796 / 62,148 / 58,312 bytes** for
MC68000/020/040, reaches 167 methods, and MC68000 returns **42** after
**1,974,452 instructions / 20,699,164 cycles** with zero relocations,
framework members/features, managed allocations, and runtime descriptors.
Full virtual-group clipping, page transition side effects, and complete
MorphOS differential event behavior remain progressive.

GUI-mode event delivery now also checks virtual-group visibility. The typed
gate walks named parent records, recognizes virtual groups from their named
`MUIA_Virtgroup_Width` and `MUIA_Virtgroup_Height` attributes, and compares the
target area rectangle with each viewport. Targets outside a viewport are
skipped; missing geometry remains permissive until layout supplies it. The
host suite is **582/582**. `WindowEventHandlerRoot` emits **56,368 / 64,040 /
60,028 bytes** for MC68000/020/040, reaches 172 methods, and MC68000 returns
**42** after **1,985,695 instructions / 20,817,494 cycles**. The dedicated
`WindowEventHandlerVirtualGroupRoot` emits **52,332 / 59,564 / 55,928 bytes**,
reaches 171 methods, and returns **42** after **411,164 instructions /
4,321,962 cycles**. Full clipping-region propagation, scrolling, and complete
MorphOS differential event behavior remain progressive.

The typed window event walk also recognizes MorphOS `MUI_EHF_PRIORITY = 0x0800`.
Priority handlers are represented by the named application-window wrapper
record, kept in a leading partition, ordered by signed priority with FIFO ties,
and dispatched before active/default routing. A non-eating priority callback is
not visited again by later passes. Host coverage is **583/583**;
`WindowEventHandlerRoot` emits **57,444 / 65,156 / 61,172 bytes** and returns
**42** after **2,033,249 instructions / 21,317,924 cycles**, while the focused
`WindowEventHandlerPriorityRoot` emits **52,664 / 59,892 / 56,256 bytes** and
returns **42** after **236,992 instructions / 2,491,248 cycles**. The internal
`ISACTIVEGRP`, `ISACTIVE`, `ISCALLING`, and `ISENABLED` flag transitions still
require later compatibility goals.

The current MG09 event-routing slice adds struct-first active-parent keyboard
delivery. A typed `MUIP_HandleEvent` key walks the active object's named
`MuiHeadlessObjectRecord.Parent` chain before the default object, stops if the
active object changes during delivery, and excludes visited ancestors from the
remaining queue. The default pass treats only `MUI_EventHandlerRC_Eat == 1` as
terminal; non-eat method results continue. Host coverage is **584/584**. The
standard, priority, and active-parent native closures return **42** under the
freestanding M68000 profile with zero framework members, managed allocations,
relocations, and descriptors. Full MorphOS key-routing parity remains open.

The event-handler callback boundary now maintains the MorphOS
`MUI_EHF_ISCALLING = 0x4000` bit directly in the named guest
`MuiEventHandlerNode`. It is set before `DoMethod`/`CoerceMethod`, then cleared
after a struct re-read while preserving callback changes to the other fields.
The host probe observes the transient state and the native
`WindowEventHandlerCallingRoot` returns **42** under the freestanding profile.
Internal `ISACTIVE*`/`ISENABLED` transitions and automatic
`MUIA_HandledEvents` registration remain progressive.

After this callback-state update, the focused MC68000 event-route closures
remain zero-runtime-gate clean: standard **58,588 bytes**, priority **53,808**,
active-parent **54,044**, and calling-state **46,168**; each returns **42** in
the native harness.

The named event-handler record also tracks MorphOS
`MUI_EHF_ISENABLED = 0x8000`: accepted registration sets it, explicit removal
clears it, and window cleanup clears it when releasing the wrapper list.
Rejected insertion restores the caller's original flags. The focused enabled
state closure returns **42** on MC68000/020/040 qualification.

After this state update, the focused MC68000 routes requalify at **59,392**
(standard), **54,612** (priority), **54,848** (active-parent), **46,724**
(calling), and **47,792** (enabled) bytes; each returns **42** in the native
harness.

The named event-handler record now also maintains MorphOS
`MUI_EHF_ISACTIVE = 0x2000`. At registration and bounded dispatch boundaries,
the typed path derives the read-only bit from the window's active and default
object attributes; active-object transitions therefore become visible before
delivery. Explicit removal and cleanup clear both `ISACTIVE` and `ISENABLED`.
Host coverage is **587/587**. The focused active-state native root emits
**56,164 / 63,900 / 60,084 bytes** for MC68000/020/040 and returns **42** after
**396,395 instructions / 4,156,876 cycles** on MC68000. The implementation
remains freestanding, exception-free, managed-runtime-free, and struct-first;
`ISACTIVEGRP`, automatic `MUIA_HandledEvents` registration, and full MorphOS
differential event-handler behavior remain progressive.

Final focused MC68000 requalification after the typed dispatch cleanup emits
**60,236 / 55,452 / 55,692 / 46,776 / 48,568 / 56,148 bytes** for the
standard, priority, active-parent, calling, enabled, and active-state roots;
all six return **42** in the native harness and remain zero-runtime-gate clean.

The public `MUIA_Window_DisableKeys` mask is now honored at the typed
`MUIP_HandleEvent` boundary. A set bit suppresses that non-negative MUI key
before event-handler routing; `MUIKEY_NONE`, non-key packets, and out-of-range
synthetic values retain the existing route. Host coverage is **588/588**.
`WindowEventHandlerDisableKeysRoot` emits **55,704 / 63,268 / 59,508 bytes**
for MC68000/020/040 and returns **42** after **208,024 instructions /
2,188,318 cycles** on MC68000. Private `ISACTIVEGRP`, automatic
`MUIA_HandledEvents`, and complete MorphOS differential behavior remain
progressive.

The typed public window setter now covers `MUIA_Window_DefaultObject`. It
accepts a live object or `NULL`, rejects unknown guest targets without
mutation, and immediately refreshes `MUI_EHF_ISACTIVE` in the named event
handler records. Host coverage is **589/589**. The focused
`WindowEventHandlerDefaultObjectRoot` emits **50,424 / 57,272 / 53,856 bytes**
for MC68000/020/040, reaches 162 methods, and returns **42** after **298,690
instructions / 3,137,130 cycles** on MC68000. The MC68000 report is
zero-runtime and zero-relocation clean; the 68020/040 closure maps retain
**13 / 0 relocations**. Private `ISACTIVEGRP`, automatic
`MUIA_HandledEvents`, and full MorphOS differential behavior remain
progressive.

The typed public window setter now covers `MUIA_Window_Activate` through both
`Set` and `NoNotifySet`. TRUE requires an open native window and successful
platform activation; FALSE is a no-op. Host coverage is **590/590**. The
focused `WindowActivateRoot` emits **44,592 / 50,604 / 47,672 bytes** for
MC68000/020/040, reaches 149 methods, and returns **42** after **113,845
instructions / 1,197,222 cycles** on MC68000. All focused maps are
zero-runtime and zero-relocation clean. Additional MorphOS window attributes
remain progressive.

The typed public window setter now covers `MUIA_Window_Sleep`. Non-zero writes
nest the sleep counter, zero writes wake one level, the prior `MUIA_Disabled`
state is restored after the final wake, and window events are suppressed while
the depth is nonzero. Host coverage is **591/591**. `WindowSleepRoot` emits
**52,196 / 59,264 / 55,672 bytes** for MC68000/020/040, reaches 165 methods,
and returns **42** after **148,196 instructions / 1,567,420 cycles** on
MC68000. Focused maps have zero framework features and managed allocations;
busy-pointer forwarding remains a platform-contract follow-up.

The typed `MUIA_Window_Sleep` path now exposes the MorphOS busy-pointer effect
through `IMuiApplicationPlatform.SetMuiWindowBusy`. It balances the capability
at the outermost sleep/final wake boundary, releases it during close, and
replays it when a sleeping object opens. Host coverage is **591/591**. The
focused native root emits **53,548 / 60,676 / 57,004 bytes** for
MC68000/020/040, with MC68000 execution returning **42** after **204,774
instructions / 2,165,920 cycles**. The implementation remains freestanding,
exception-free, managed-runtime-free, and struct-first.

The typed `MUIA_Application_Sleep` path now implements the MorphOS nested
application sleep contract. It adjusts each owned window's named sleep depth,
suppresses application input handlers while nonzero, and applies the full
depth to windows added during sleep. Host coverage is **592/592**. The focused
native root emits **56,124 / 63,676 / 59,660 bytes** for MC68000/020/040, with
MC68000 execution returning **42** after **761,510 instructions / 8,038,202
cycles**. The implementation remains freestanding, exception-free,
managed-runtime-free, and struct-first.

The typed `MUIA_Application_Iconified` path now follows the MorphOS
iconification contract. It closes currently native owned windows while
retaining a named guest reopen marker, defers `OpenWindow` requests made while
iconified, and restores all remembered windows after uniconification. Host
coverage is **593/593**. `ApplicationIconifiedRoot` emits **48,560 / 55,104 /
51,672 bytes** for MC68000/020/040 and returns **42** after **830,581
instructions / 8,754,704 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first.

The typed `MUIA_Application_Active` path now canonicalizes commodities-facing
BOOL writes: any non-zero guest value is stored as MorphOS TRUE and zero as
FALSE. The value remains named guest state; MUI itself performs no external
action for this attribute. Host coverage is **594/594**. `ApplicationActiveRoot`
emits **43,452 / 49,488 / 46,532 bytes** for MC68000/020/040, reaches 142
methods, and returns **42** after **113,063 instructions / 1,184,588 cycles**
on MC68000. The implementation remains freestanding, exception-free,
managed-runtime-free, and struct-first. See the [MorphOS MUI Application
documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Window_VisibleOnMaximize` path now uses a named mutable
MorphOS BOOL record. `Set` and `NoNotifySet` share the focused
`DispatchWindowVisibleOnMaximize` packet seam, and every non-zero value is
canonicalized to TRUE. Maximize presentation remains a platform capability.
Host coverage is **623/623**. `WindowVisibleOnMaximizeRoot` emits **53,248 /
60,720 / 56,964 bytes** for MC68000/020/040, reaches 169 methods, and returns
**42** after **67,488 instructions / 706,114 cycles** on MC68000. The
implementation remains freestanding, exception-free, managed-runtime-free,
and struct-first; packet fields cross named records rather than raw handler
offsets. See the [MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_IsSubWindow` path now uses an initializer-only named
MorphOS BOOL record. Creation tags canonicalize non-zero values to TRUE;
later `Set`/`NoNotifySet` writes are rejected. During guest-family disposal,
flagged windows are detached and retained while ordinary children are disposed
normally. Host coverage is **624/624**. `WindowIsSubWindowRoot` emits **53,976 /
61,632 / 57,688 bytes** for MC68000/020/040, reaches 170 methods, and returns
**42** after **170,468 instructions / 1,790,440 cycles** on MC68000. The
implementation remains freestanding, exception-free, managed-runtime-free,
and struct-first; named packet records and guest object flags carry the policy
without raw handler offsets. See the [MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_RefWindow` path now retains a live MUI window target in
named state for relative placement. Self-references and non-live pointers are
rejected without mutation; coordinate calculation remains a platform seam.
Host coverage is **622/622**. `WindowRefWindowRoot` emits **53,448 / 60,968 /
57,180 bytes** for MC68000/020/040, reaches 169 methods, and returns **42**
after **124,581 instructions / 1,308,384 cycles** on MC68000. The
implementation remains freestanding, exception-free, managed-runtime-free,
and struct-first; packet fields cross named records rather than raw handler
offsets. See the [MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The public `MUIA_Window_Window` getter now exposes the opaque native window
pointer from the named lifecycle record. It returns NULL before opening and
after closing, rejects writes, and never mirrors or owns the platform object.
Host coverage is **611/611**. `WindowWindowRoot` emits **51,484 / 58,572 /
54,836 bytes** for MC68000/020/040, reaches 169 methods, and returns **42**
after **121,853 instructions / 1,282,288 cycles** on MC68000. The implementation
remains freestanding, exception-free, managed-runtime-free, and struct-first.
See the [MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_ID` path now stores a mutable ULONG identity in the
ordinary named attribute record. Both `Set` and `NoNotifySet` reach the same
named packet route, so Snapshot and other consumers observe the guest value
without a managed mirror or positional state offsets. Host coverage is
**612/612**. `WindowIdRoot` emits **51,244 / 58,320 / 54,596 bytes** for
MC68000/020/040, reaches 166 methods, and returns **42** after **64,697
instructions / 677,500 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first. See the
[MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_Screen` path now retains an explicit guest `Screen *`
selection in named window state. The getter follows the MorphOS lifecycle
contract: it returns NULL while closed and exposes the selected pointer only
after `OpenWindow`; unmapped pointers are rejected without mutation. Host
coverage is **621/621**. `WindowScreenRoot` emits **54,464 / 61,968 / 58,096
bytes** for MC68000/020/040, reaches 175 methods, and returns **42** after
**140,494 instructions / 1,478,186 cycles** on MC68000. The implementation
remains freestanding, exception-free, managed-runtime-free, and struct-first;
packet fields cross named records rather than raw handler offsets. See the
[MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_PublicScreen` path now retains the caller-owned guest
screen-name C-string in named window state. Bounded strings are validated in
place, NULL clears the value, and malformed pointers are rejected without
managed copies. Public-screen lookup remains a platform capability. Host
coverage is **620/620**. `WindowPublicScreenRoot` emits **52,832 / 60,196 /
56,432 bytes** for MC68000/020/040, reaches 168 methods, and returns **42**
after **72,916 instructions / 763,328 cycles** on MC68000. The implementation
remains freestanding, exception-free, managed-runtime-free, and struct-first;
packet fields cross named records rather than raw handler offsets. See the
[MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_ScreenTitle` path now retains the caller-owned guest
C-string pointer in named window state. Bounded strings are validated in place,
NULL clears the value, and malformed pointers are rejected without managed
copies. Host coverage is **619/619**. `WindowScreenTitleRoot` emits
**52,788 / 60,132 / 56,376 bytes** for MC68000/020/040, reaches 168 methods,
and returns **42** after **72,790 instructions / 762,312 cycles** on MC68000.
The implementation remains freestanding, exception-free, managed-runtime-
free, and struct-first; packet fields cross named records rather than raw
handler offsets. See the [MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_CloseRequest` path now uses a named mutable BOOL shared
by event polling, `Set`, `NoNotifySet`, and `Get`. Every non-zero write is
canonicalized to TRUE, so close-gadget publication and caller acknowledgement
observe one guest-resident value without managed shadow state or positional
offsets. Host coverage is **613/613**. `WindowCloseRequestRoot` emits
**51,296 / 58,376 / 54,672 bytes** for MC68000/020/040, reaches 166 methods,
and returns **42** after **66,055 instructions / 691,390 cycles** on MC68000.
The implementation remains freestanding, exception-free,
managed-runtime-free, and struct-first. See the [MorphOS MUI Window
documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_TabletMessages` path now uses an initializer-only
named MorphOS BOOL record. Creation tags canonicalize non-zero values to TRUE;
later `Set`/`NoNotifySet` writes are rejected. `OpenWindow` forwards the named
state through the explicit `SetMuiWindowTabletMessages` platform capability.
Host coverage is **625/625**. `WindowTabletMessagesRoot` emits **55,576 /
63,352 / 59,388 bytes** for MC68000/020/040, reaches 178 methods, and returns
**42** after **127,958 instructions / 1,344,152 cycles** on MC68000. The
implementation remains freestanding, exception-free, managed-runtime-free,
and struct-first; the ABI crosses named records rather than raw offsets. See
the [MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_UseBottomBorderScroller`,
`MUIA_Window_UseLeftBorderScroller`, and `MUIA_Window_UseRightBorderScroller`
paths now use mutable named MorphOS BOOL records. Creation tags and later
`Set`/`NoNotifySet` writes canonicalize non-zero values to TRUE; updates to an
open window forward the complete policy through the single typed
`SetMuiWindowBorderScrollers` platform capability. Host coverage is
**626/626**. `WindowBorderScrollersRoot` emits **57,232 / 65,180 / 61,048
bytes** for MC68000/020/040, reaches 181 methods, and returns **42** after
**192,366 instructions / 2,023,506 cycles** on MC68000. The implementation
remains freestanding, exception-free, managed-runtime-free, and struct-first;
the ABI crosses named records rather than raw offsets. See the [MorphOS MUI
Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

## String Accept/Reject filter state

The MorphOS `MUIA_String_Accept` and `MUIA_String_Reject` attributes are
exposed through the named `MuiStringFilterState` record. Each caller-owned
`[ISG]` STRPTR is checked as a bounded guest C string at construction and on
runtime `Set`/`NoNotifySet`; the original pointer is retained, with no managed
copy or positional state offset. `StringAllowsCodePoint` consumes this same
record for both byte-mode and Unicode UTF-8 filtering. Host coverage is
**639/639**; native focused qualification and complete MorphOS String parity
remain progressive.

The typed `MUIA_Window_AltHeight`, `MUIA_Window_AltWidth`,
`MUIA_Window_AltLeftEdge`, and `MUIA_Window_AltTopEdge` paths now use one
named signed-LONG geometry record. Creation tags preserve the caller's values;
later writes are rejected as initializer-only attributes. `OpenWindow` forwards
the record through `ConfigureMuiWindowAlternateGeometry`. Host coverage is
**627/627**. `WindowAlternateGeometryRoot` emits **57,124 / 65,044 / 60,936
bytes** for MC68000/020/040, reaches 180 methods, and returns **42** after
**175,860 instructions / 1,845,064 cycles** on MC68000. The implementation
remains freestanding, exception-free, managed-runtime-free, and struct-first;
the ABI crosses a named geometry record rather than raw offsets. See the
[MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_Height`, `MUIA_Window_Width`,
`MUIA_Window_LeftEdge`, and `MUIA_Window_TopEdge` paths now use one named
signed-LONG geometry record. Creation tags preserve the caller's values;
later writes are rejected as initializer-only attributes. `OpenWindow` forwards
the record through `ConfigureMuiWindowGeometry`. Host coverage is **628/628**.
`WindowGeometryRoot` emits **56,560 / 64,492 / 60,424 bytes** for
MC68000/020/040, reaches 177 methods, and returns **42** after **187,242
instructions / 1,965,090 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first; the ABI
crosses a named geometry record rather than raw offsets. See the [MorphOS MUI
Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed initializer-only gadget policy now covers
`MUIA_Window_CloseGadget`, `MUIA_Window_DepthGadget`, `MUIA_Window_DragBar`,
`MUIA_Window_SizeGadget`, and `MUIA_Window_SizeRight` in one named ULONG
record. Creation tags canonicalize non-zero values; later writes are rejected.
`OpenWindow` forwards the policy through `ConfigureMuiWindowGadgets`. Host
coverage is **629/629**. `WindowGadgetPolicyRoot` emits **57,432 / 65,452 /
61,340 bytes** for MC68000/020/040, reaches 178 methods, and returns **42**
after **213,804 instructions / 2,242,524 cycles** on MC68000. The
implementation remains freestanding, exception-free, managed-runtime-free,
and struct-first; the ABI crosses one named policy record rather than raw
offsets. See the [MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_RootObject` path now uses the guest-resident Family
child relationship as its named state. Replacing a root releases the previous
child relationship, clearing removes it, and invalid or already-parented
objects are rejected without a managed object graph or positional offsets.
Host coverage is **614/614**. `WindowRootObjectRoot` emits **52,624 / 60,008 /
56,132 bytes** for MC68000/020/040, reaches 168 methods, and returns **42**
after **196,311 instructions / 2,070,466 cycles** on MC68000. The
implementation remains freestanding, exception-free, managed-runtime-free,
and struct-first. See the [MorphOS MUI Window
documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Application_WindowList` path now exposes a read-only,
guest-resident Exec `List` projection of application-owned windows. Named
`MuiApplicationWindowListState` and `MuiApplicationWindowListEntry` structs
keep list ownership and traversal in guest memory, filter out unrelated Family
children, and rebuild after topology mutation. Host coverage is **608/608**.
`ApplicationWindowListRoot` emits **48,432 / 55,224 / 51,720 bytes** for
MC68000/020/040, reaches 155 methods, and returns **42** after **414,850
instructions / 4,354,368 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first. See the
[MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_Commands` path now validates and retains a
caller-owned, NUL-terminated MorphOS `MUI_Command` table through the named
`MuiApplicationCommandRecord` codec. Each command name and optional template is
validated as a bounded guest C string (or accepted as the documented
`MC_TEMPLATE_ID` sentinel), and malformed tables leave the previous pointer
unchanged. Host coverage is **609/609**. `ApplicationCommandsRoot`
emits **49,732 / 56,524 / 52,988 bytes** for MC68000/020/040, reaches 160
methods, and returns **42** after **72,174 instructions / 751,584 cycles** on
MC68000. The implementation remains freestanding, exception-free,
managed-runtime-free, and struct-first; ARexx transport and hook execution are
separate capability work.

The typed `MUIA_Application_SingleTask` and `MUIA_Application_DoubleStart`
paths now implement the MorphOS single-task lifecycle contract. A TRUE
single-task initializer claims a guest-resident application slot; a conflicting
initializer is rejected and sets `DoubleStart` on the live application. The
initializer cannot be changed after application initialization. Host coverage
is **595/595**. `ApplicationSingleTaskRoot` emits **45,208 / 51,484 / 48,352
bytes** for MC68000/020/040, reaches 145 methods, and returns **42** after
**435,682 instructions / 4,576,170 cycles** on MC68000. The implementation
remains freestanding, exception-free, managed-runtime-free, and struct-first.
See the [MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_DropObject` path now retains the mutable,
caller-owned pointer to a live MUI object that receives iconified-app
AppMessages. `Set` and `NoNotifySet` validate object identity, accept NULL to
clear the value, and reject invalid objects without mutation. Message delivery
remains a separate platform capability. Host coverage is **604/604**.
`ApplicationDropObjectRoot` emits **43,272 / 49,296 / 46,364 bytes** for
MC68000/020/040, reaches 142 methods, and returns **42** after **111,568
instructions / 1,172,426 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first. See the
[MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Window_AppWindow`, `MUIA_ApplicationObject`, and
`MUIA_AppMessage` paths now provide the first AppWindow transport slice.
`MUIA_Window_AppWindow` is a named BOOL on `Window.mui` and becomes immutable
after opening. `MUIA_ApplicationObject` resolves the initialized application
through the guest parent chain. `MUIA_AppMessage` is getter-only and transient:
`DispatchAppMessage` validates a named `MuiAppMessageRecord` plus its
`MuiWorkbenchArgumentRecord` array, publishes it while notifications run, and
restores the prior value immediately afterward. Host coverage is **610/610**.
`AppMessageRoot` emits **59,388 / 67,460 / 63,376 bytes** for
MC68000/020/040, reaches 186 methods, and returns **42** after **453,276
instructions / 4,755,618 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first; raw
offsets are confined to the ABI codecs. See the [MorphOS MUI Application
documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_IconifyTitle` path now retains a mutable,
caller-owned guest C-string pointer. Each `Set` or `NoNotifySet` validates the
bounded string before storing the pointer in named application state; NULL
clears the title and malformed guest pointers are rejected without mutation.
Host coverage is **601/601**. `ApplicationIconifyTitleRoot` emits **43,588 /
49,648 / 46,680 bytes** for MC68000/020/040, reaches 143 methods, and returns
**42** after **74,282 instructions / 778,486 cycles** on MC68000. The
implementation remains freestanding, exception-free, managed-runtime-free,
and struct-first. See the [MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_UseScreenNotify` path now stores the
initializer-only MorphOS BOOL in named application state. Non-zero values are
canonicalized to TRUE, zero to FALSE, and post-initialization writes are
rejected. The screen-notify transport remains a separate platform boundary;
the conservative default is disabled. Host coverage is **602/602**.
`ApplicationUseScreenNotifyRoot` emits **44,492 / 50,684 / 47,652 bytes** for
MC68000/020/040, reaches 144 methods, and returns **42** after **332,506
instructions / 3,490,766 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first. See the
[MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_DiskObject` path now retains a mutable,
caller-owned guest pointer to a complete Workbench `DiskObject` record. The
fixed ABI range is validated before storage, NULL clears the pointer, and
malformed or unmapped records are rejected without mutation. AppIcon
presentation remains a separate platform capability. Host coverage is
**603/603**. `ApplicationDiskObjectRoot` emits **43,256 / 49,276 / 46,352
bytes** for MC68000/020/040, reaches 142 methods, and returns **42** after
**67,880 instructions / 711,444 cycles** on MC68000. The implementation
remains freestanding, exception-free, managed-runtime-free, and struct-first.
See the [MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_ForceQuit` path now maintains the MorphOS
force-quit query flag. It defaults to FALSE when an application is initialized,
canonicalizes all non-zero writes to TRUE, and never invokes a host exit path;
the application remains responsible for exiting quietly after a quit ReturnID.
Host coverage is **596/596**. `ApplicationForceQuitRoot` emits **44,136 /
50,328 / 47,296 bytes** for MC68000/020/040, reaches 143 methods, and returns
**42** after **151,526 instructions / 1,590,184 cycles** on MC68000. The
implementation remains freestanding, exception-free, managed-runtime-free,
and struct-first. See the [MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_Window` initializer path now routes each guest
object through `AddWindow`, preserving named ownership and application-sleep
inheritance. Multiple initializer tags are accepted in order; duplicate,
invalid, and post-initialization writes are rejected. Host coverage is
**599/599**. `ApplicationWindowInitializerRoot` emits **46,820 / 53,156 /
50,020 bytes** for MC68000/020/040, reaches 151 methods, and returns **42**
after **402,848 instructions / 4,227,902 cycles** on MC68000. The
implementation remains freestanding, exception-free, managed-runtime-free,
and struct-first. See the [MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_HelpFile` path now retains a mutable, validated
guest C-string pointer. `MUIM_Application_ShowHelp` uses that application
value when its HelpFile field is NULL, while an explicit packet pointer still
takes precedence. Host coverage remains **598/598**. The qualified
`ApplicationShowHelpRoot` emits **49,536 / 56,204 / 52,732 bytes** for
MC68000/020/040, reaches 159 methods, and returns **42** after **494,746
instructions / 5,185,694 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first. See the
[MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_UseRexx` path now implements the documented
initializer-only ARexx policy. The default is TRUE; an initializer may request
FALSE before `InitializeApplication`, and any post-initialization write is
rejected without mutation. The setting remains named guest state while ARexx
transport itself remains a separate platform service. Host coverage is
**597/597**. `ApplicationUseRexxRoot` emits **44,192 / 50,380 / 47,352 bytes**
for MC68000/020/040, reaches 143 methods, and returns **42** after **320,676
instructions / 3,364,308 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first. See the
[MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed application identity-string path now covers the MorphOS `[I.G]`
attributes `MUIA_Application_Title`, `Author`, `Base`, `Copyright`,
`Description`, and `Version`. Each retains a bounded caller-owned guest
C-string pointer in the named attribute record, accepts NULL before
initialization, and rejects writes after initialization. Host coverage is
**598/598**. `ApplicationIdentityStringsRoot` emits **46,472 / 52,772 /
49,616 bytes** for MC68000/020/040, reaches 151 methods, and returns **42**
after **254,445 instructions / 2,662,062 cycles** on MC68000. The
implementation remains freestanding, exception-free, managed-runtime-free,
and struct-first. See the [MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_UsedClasses` path now validates the mutable
`[ISG]` guest `STRPTR` vector as a bounded, NULL-terminated list of C-string
class names. `MuiApplicationUsedClassesVectorCursor` and
`MuiApplicationUsedClassesVectorEntry` are named guest-memory records; the
vector pointer is retained in the application attribute without a
managed copy, and a NULL vector represents an empty list. Host coverage is
**600/600**. `ApplicationUsedClassesRoot` emits **43,944 / 50,004 / 47,024
bytes** for MC68000/020/040, reaches 144 methods, and returns **42** after
**70,416 instructions / 732,784 cycles** on MC68000. The implementation
remains freestanding, exception-free, managed-runtime-free, and struct-first.
See the [MorphOS MUI Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Application_MenuAction`/`MUIA_Application_MenuHelp` path now
keeps menu event state in named guest attributes. MenuAction accepts
`Set`/`NoNotifySet`; MenuHelp is getter-only and is updated through the typed
menu transport seam. Both initialize to zero, with no managed event shadow.
Host coverage is **605/605**. `ApplicationMenuEventStateRoot` emits
**44,688 / 50,876 / 47,844 bytes** for MC68000/020/040, reaches 145 methods,
and returns **42** after **192,715 instructions / 2,021,186 cycles** on
MC68000. The implementation remains freestanding, exception-free,
managed-runtime-free, and struct-first. See the [MorphOS MUI Application
documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

## Window mode policy

The MorphOS initializer-only Window mode attributes are represented by the
named `MuiWindowModePolicy` struct. `AppWindow`, `Backdrop`, `Borderless`, and
`PanelWindow` are canonicalized as ULONG/BOOL values during construction;
post-initialization writes are rejected. `OpenWindow` forwards the complete
record through the typed `ConfigureMuiWindowMode` platform capability. No
exceptions, managed allocation, or runtime-owned shadow object is introduced;
raw offsets remain confined to the guest TagItem boundary.

## Window Menustrip ownership

`MUIA_Window_Menustrip` accepts only a live, unparented `Menustrip.mui`
object. The named pointer record and the window's guest family relationship
are changed together, with rollback on failure; replacement and NULL clearing
detach the previous child without creating a managed menu graph. The root
object pointer is stored independently, so a window can own both a root object
and a Menustrip without positional-child ambiguity.

## Window FancyDrawing compatibility

The obsolete MorphOS `MUIA_Window_FancyDrawing` BOOL is retained as named
guest state and supports `Set`, `NoNotifySet`, and `Get`. It does not bypass
the normal draw lifecycle or create a rendering shortcut; the implementation
keeps this compatibility surface separate from `MUIM_Draw`.

## Window MenuAction state

`MUIA_Window_MenuAction` is retained as a named mutable ULONG event value.
Ordinary Set/Get packets and `SetWindowMenuActionValue` use the same guest
attribute record, so future Menustrip selection delivery can publish UserData
without a second shadow state or raw offset path.

The typed `MUIA_Application_Menustrip` path now adopts a live, unparented
`Menustrip.mui` object before application initialization through the
guest-resident family relationship. `Menuitem.mui` trigger selection publishes
its named `MUIA_UserData` to the owning application's MenuAction, while help
selection publishes to MenuHelp. Host coverage is **606/606**.
`ApplicationMenuTransportRoot` emits **52,220 / 59,156 / 55,444 bytes** for
MC68000/020/040, reaches 171 methods, and returns **42** after **586,012
instructions / 6,134,996 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first, with
named packet records and no raw handler offsets. See the [MorphOS MUI
Application documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

The typed `MUIA_Window_NoMenus` path now uses a named mutable BOOL record.
`Set` and `NoNotifySet` share the focused `DispatchWindowNoMenus` packet seam,
and every non-zero value is canonicalized to TRUE. The guest state is
qualified independently of menu rendering, which remains a platform
capability. Host coverage is **615/615**. `WindowNoMenusRoot` emits
**52,348 / 59,684 / 55,888 bytes** for MC68000/020/040, reaches 168 methods,
and returns **42** after **67,048 instructions / 702,220 cycles** on MC68000.
The implementation remains freestanding, exception-free,
managed-runtime-free, and struct-first; packet fields cross named records
rather than raw handler offsets. See the [MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_HasAlpha` path now uses a named mutable BOOL record.
`Set` and `NoNotifySet` share the focused `DispatchWindowHasAlpha` packet
seam, and every non-zero value is canonicalized to TRUE. Alpha-buffer and
Intuition forwarding remain a platform capability. Host coverage is
**616/616**. `WindowHasAlphaRoot` emits **52,392 / 59,740 / 55,932 bytes** for
MC68000/020/040, reaches 168 methods, and returns **42** after **67,099
instructions / 702,614 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first; packet
fields cross named records rather than raw handler offsets. See the [MorphOS
MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_Title` path now retains the caller-owned guest C-string
pointer in named window state. Bounded strings are validated in place, NULL
clears the value, and malformed pointers are rejected without managed copies.
Host coverage is **618/618**. `WindowTitleRoot` emits **52,732 / 60,076 /
56,316 bytes** for MC68000/020/040, reaches 168 methods, and returns **42**
after **72,601 instructions / 760,758 cycles** on MC68000. The implementation
remains freestanding, exception-free, managed-runtime-free, and struct-first;
packet fields cross named records rather than raw handler offsets. See the
[MorphOS MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Window_Opacity` path now retains a bounded LONG in named
window state. Valid values are **0..255**; malformed writes are rejected
atomically without changing the previous value. Intuition opacity forwarding
remains a platform capability. Host coverage is **617/617**.
`WindowOpacityRoot` emits **52,584 / 59,948 / 56,140 bytes** for
MC68000/020/040, reaches 168 methods, and returns **42** after **72,060
instructions / 755,876 cycles** on MC68000. The implementation remains
freestanding, exception-free, managed-runtime-free, and struct-first; packet
fields cross named records rather than raw handler offsets. See the [MorphOS
MUI Window documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

The typed `MUIA_Application_UseCommodities` path now implements the
initializer-only BOOL policy: the MorphOS default is TRUE, a pre-
initialization FALSE value is retained in named guest state, and live writes
are rejected. Commodities transport remains a separate platform capability.
Host coverage is **607/607**. `ApplicationUseCommoditiesRoot` emits
**44,616 / 50,804 / 47,764 bytes** for MC68000/020/040, reaches 144 methods,
and returns **42** after **409,996 instructions / 4,297,218 cycles** on
MC68000. The implementation remains freestanding, exception-free,
managed-runtime-free, and struct-first. See the [MorphOS MUI Application
documentation](https://morphos-team.net/sdk/MUI/MUI_Application.html).

## Window MouseObject state

`MUIA_Window_NeedsMouseObject` is an initializer-only named MorphOS BOOL:
creation tags canonicalize non-zero values to TRUE and live writes are
rejected. `MUIA_Window_MouseObject` is a getter-only named APTR. The
`PublishWindowMouseObjectValue` seam validates a live target object, rejects
self/unknown pointers, and permits NULL clearing without introducing a
managed object graph or raw handler offsets. Hit testing and pointer delivery
remain a platform capability. Host coverage is **634/634**; native focused
qualification remains progressive.

## List slot and image state

The shared List backbone uses `MuiListSlotState`/`MuiListSlotCodec` for each
fixed 8-byte index element and `MuiListImageState`/`MuiListImageCodec` for the
16-byte opaque image-handle chain. The contiguous slot array remains an
explicit ABI boundary, while slot members, image metadata, and next links are
named struct fields in all consumers. Host coverage is **666/666**; native
List slot/image ABI and complete MorphOS differential parity remain
progressive.

## List column geometry state

The fixed 8-byte `{Offset, Width}` geometry elements used by List layout,
drawing, hit-testing, and edit placement now use the named
`MuiListColumnGeometry` record and `MuiListColumnGeometryCodec`. The geometry
array remains an explicit ABI boundary; all consumers use the named fields.
Host coverage is **667/667**; native geometry ABI and complete MorphOS
differential parity remain progressive.

## List render-info state

List drawing and render-port lookup consume the named
`MuiDrawingRenderInfoRecord` through `MuiDrawingRenderInfoCodec`, including the
typed `RastPort` pointer. The List core no longer repeats the fixed
`MUI_RenderInfo` member offset. Host coverage remains **667/667**; native
render-port ABI and complete MorphOS differential parity remain progressive.

## Dirlist fixed wire records

The fixed headers of owned FileInfoBlock-like entries and transient ExAll-like
scan payloads use `MuiDirlistEntryWireState`/`MuiDirlistEntryWireCodec` and
`MuiDirlistScanEntryWireState`/`MuiDirlistScanEntryWireCodec`. Variable inline
name/comment payloads remain explicit ABI tails; fixed field accesses are
codec-only. Host coverage is **668/668**; native Dirlist entry ABI and
complete MorphOS differential parity remain progressive.

## AreaDrag typed method headers

AreaDrag Begin, Drop, Event, Finish, Query, and Report readers now route shared
selector checks through `MuiAreaDragMessageCodec.TryReadMethodId` before
decoding typed records. Host coverage is **710/710**; native AreaDrag packet
ABI and complete MorphOS differential parity remain progressive.

## Process specialist typed method headers

Process specialist Get/Set, signal, error, dispatch, and lifecycle readers now
route selector checks through the named specialist method header before decoding
typed records. Host coverage is **709/709**; native Process packet ABI and
complete MorphOS differential parity remain progressive.

## Pop specialist typed method headers

Pop specialist Get/Set, Close, and lifecycle readers now route selector checks
through the named specialist method header before decoding typed records. Host
coverage is **708/708**; native Pop packet ABI and complete MorphOS differential
parity remain progressive.

## Color specialist typed method headers

Color specialist Get/Set, pointer, RGB, and lifecycle readers now route
selector checks through the named specialist method header before decoding
typed records. Host coverage is **707/707**; native Color packet ABI and
complete MorphOS differential parity remain progressive.

## Family mutation typed method headers

Family AddHead/AddTail/Remove, Insert, Transfer, Reorder, and Sort readers now
route selector checks through `MuiFamilyMutationMessageCodec.TryReadMethodId`
before decoding their typed records and bounded array tails. Host coverage is
**706/706**; native Family mutation packet ABI and complete MorphOS differential
parity remain progressive.

## Family_DoChildMethods method header

Family_DoChildMethods dispatch now validates its selector through the named
`MuiFamilyDoChildMethodsMessage` and
`MuiFamilyDoChildMethodsMessageCodec.TryReadMethodId`. Host coverage is
**705/705**; native Family_DoChildMethods packet ABI and complete MorphOS
differential parity remain progressive.

## Family_GetChild method header

Family_GetChild packet decoding now obtains its selector through the named
`MuiFamilyGetChildMethodMessage` and
`MuiFamilyGetChildMessageCodec.TryReadMethodId` before validating the complete
number/reference record. Host coverage is **704/704**; native Family_GetChild
packet ABI and complete MorphOS differential parity remain progressive.

## Dirlist byte-total state

The `MUIA_Dirlist_NumBytes64` guest QUAD now crosses
`MuiDirlistByteTotalCodec` using the named `MuiDirlistByteTotalState` (`High`,
`Low`) fields. Public byte-total publication and inspection no longer repeat
word offsets. Host coverage is **669/669**; native QUAD ABI and complete
MorphOS differential parity remain progressive.

## Group-change typed method headers

InitChange, ExitChange, and ExitChange2 packet decoding now uses the named
`MuiGroupChangeMessage` codec before consuming the optional flags record. Host
coverage is **725/725**; native group-change packet ABI and complete MorphOS
differential parity remain progressive.

## Listview click state

`MuiListviewClickState`/`MuiListviewClickStateCodec` now own the fixed 20-byte
click-column, BOOL, and count record. Listview click lifecycle and result
publication consume named fields, while `DrawScroller` uses the named
`MuiDrawingRenderInfoRecord` seam for its typed `RastPort`. Host coverage is
**670/670**; native Listview click/render ABI and complete MorphOS differential
parity remain progressive.

## List header state

The shared List backbone now uses the named `MuiListHeaderState` record with
typed `Index` and `Images` pointers plus `Capacity` and `Count`. The bounded
`MuiListHeaderCodec` is the only place that knows the fixed 20-byte guest
layout; construction, slot access, image-chain management, and capacity
growth consume named fields. Host coverage is **662/662**. Native List ABI
qualification and complete MorphOS differential parity remain progressive.

## Listtree header state

The external `Listtree.mcc` core now uses the named 48-byte
`MuiListtreeHeaderState` record for root links, counters, redraw coalescing,
and drop-mark state. `MuiListtreeHeaderCodec` contains the fixed guest layout;
all lifecycle, traversal, and mutation consumers use named fields while the
public tree-node prefix and external-component packaging remain unchanged.
Host coverage is **663/663**. Native Listtree ABI and complete MorphOS
differential qualification remain progressive.

## Listtree node prefix state

The public 18-byte `MUIS_Listtree_TreeNode` prefix now crosses the named
`MuiListtreeNodePublicState` and `MuiListtreeNodePublicCodec` boundary. Cookie,
owner, name, flags, and user fields are typed; the 64-byte allocation and
private topology remain separate guest state. Host coverage is **664/664**;
native Listtree node ABI and complete MorphOS differential qualification remain
progressive.

## Listtree complete node state

The full 64-byte Listtree node now crosses the named
`MuiListtreeNodeState`/`MuiListtreeNodeCodec` boundary. Parent, child, sibling,
count, and ownership fields are typed, and the public prefix codec projects
from this complete record. Host coverage is **665/665**; native Listtree
topology ABI and complete MorphOS differential qualification remain
progressive.

## AppMessage node state

The 20-byte Exec Workbench node embedded in `MUIA_AppMessage` now uses the
named `MuiAppMessageNodeState` record (`Successor`, `Predecessor`, `Type`,
`Priority`, `Name`, `ReplyPort`, and `Length`). `MuiAppMessageNodeCodec`
contains the packed node offsets, while the surrounding 86-byte AppMessage
codec consumes typed state. Host coverage is **661/661**; native Workbench
message ABI qualification remains progressive.

## Obsolete Window Menu initializer alias

`MUIA_Window_Menu` (`0x8042DB94`) is accepted only during Window creation and
aliases the named Menustrip family relationship. The MorphOS
`MUIV_Window_Menu_NoMenu` sentinel (`-1`) clears the relationship; live writes
are rejected and no second menu graph is created. Host coverage is
**637/637**; native menu qualification remains progressive.

## Window Open lifecycle state

`MUIA_Window_Open` is represented by the named `MuiWindowLifecycleState.Open`
BOOL view and the existing opaque native-window pointer. `DispatchWindowOpen`
and the broad Set/NoNotifySet route call `SetWindowOpenValue`, so TRUE is
published only after `OpenMuiWindow` and FALSE is published after the native
window is closed. The implementation adds no managed lifecycle object and no
raw handler offsets. Host coverage is **635/635**; native focused qualification
remains progressive.

## Window InputEvent state

`MUIA_Window_InputEvent` is a getter-only named pointer to the caller-owned
Amiga `InputEvent` struct. `PollWindowEvents` validates the full fixed-size
record and publishes its address before routing the event, so notifications
can observe the current record without a managed copy or positional offset.
Host coverage is **636/636**; native focused input qualification remains
progressive.

## Window DisableKeys keyboard mask

`MUIA_Window_DisableKeys` (`0x80424C36`) is exposed as the named
`MuiWindowPublicCore.MuiWindowKeyboardState.DisableKeys` ULONG. The focused
`DispatchWindowDisableKeys` seam and the broad `Set`/`NoNotifySet` route retain
the caller's MUIKEYF bit mask; `DispatchWindowEvent` reads that same named
state before offering a preprocessed key to handlers. No managed keyboard
object, exception, or raw handler offset is introduced. Host coverage is
**638/638**; native focused qualification and complete MorphOS keyboard
parity remain progressive. The contract follows the [MorphOS MUI Window
documentation](https://morphos-team.net/sdk/objectivec/MUIWindow.html).

## String interaction state

`MUIA_String_Editable`, `MUIA_String_AdvanceOnCR`, and initializer-only
`MUIA_String_Multiline` are represented by the named
`MuiStringInteractionState` record. Construction and mutable setters
canonicalize MorphOS BOOL values; `Multiline` is rejected after construction.
With `AdvanceOnCR`, Return is deliberately left unclaimed so a containing
cycle-chain/input platform can perform focus advancement without a managed
focus graph in the String core. Host coverage is **640/640**; native focused
qualification and full MorphOS String focus behavior remain progressive.

## String EditHook / LonelyEditHook

`MUIA_String_EditHook` (`0x80424C33`) and `MUIA_String_LonelyEditHook`
(`0x80421569`) use the named `MuiStringEditHookState` record. The hook is
validated as a mapped guest Hook record, then invoked through the existing
`InvokeHook` capability with a named, fixed 44-byte `MuiStringEditWorkRecord`
in A2 and the `SGH_KEY` command in A1. A nonzero result can publish a bounded
WorkBuffer, cursor, and action bits; a zero result falls back to private
editing unless LonelyEditHook is enabled. No managed callback wrapper,
exception, or raw handler offset is used. Host coverage is **641/641**;
native hook/action and focus qualification remain progressive. See the
[MorphOS MUI String documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

## Stringscroll named runtime state

`Stringscroll.mui` uses the named `MuiStringscrollState` record for its
object-owned String pointer, derived content dimensions, and pixel scroll
coordinates. Recompute, scrolling, min/max, and drawing consume that record;
private store-key access remains confined to the read/write seam. Host
coverage is **647/647**; native Stringscroll rendering/input qualification
remains progressive.

## Stringscroll policy state

`Stringscroll.mui` stores its bar, minimum-size, border, and input policy in
the named `MuiStringscrollPolicyState` record. Construction and mutable writes
canonicalize `HorizBar`, `NoInput`, `SetMin`, `SetVMin`, `UseWinBorder`,
`VertBar`, and `VertScrollerOnly`; bar visibility, min/max, and input paths
consume that same state while preserving the public attribute and notification
seams. No managed policy object, exception, or private widget offset is used.
Host coverage is **648/648**; native Stringscroll policy qualification
remains progressive. The contract follows the
[MorphOS Stringscroll evidence](https://morphos-team.net/sdk/MUI/MUI_String.html).

`Stringscroll.mui` now renders proportional horizontal and vertical scrollbar
thumbs through the named `MuiStringscrollBarGeometry` record. Thumb sizes and
travel use bounded integer arithmetic over the named viewport state; host
coverage is **969/969**, while native widget composition and complete MorphOS
differential parity remain progressive.

`Stringscroll.mui` `MUIM_HandleInput` now decodes the existing named
`MuiIntuiPointerMessage` for MorphOS SELECTUP track clicks. Horizontal and
vertical clicks map through the proportional thumb geometry into bounded pixel
scroll state, with the vertical track owning the shared bottom-right corner;
the focused host suite is **970/970**.

Thumb dragging uses the guest-resident named `MuiStringscrollPointerState`:
SELECTDOWN captures the grab offset, MOUSEMOVE updates bounded scroll, and
SELECTUP retires the state. Horizontal and vertical paths remain integer-only;
the drag record is guest-resident and uses no managed allocation, exceptions,
or managed runtime; focused host coverage is **971/971**.

`Listview.mui` scroller draw and input now share the named
`MuiListviewScrollerGeometry` record. Thumb gestures use the guest-resident
`MuiListviewScrollerDragState`, track clicks map to the child List's bounded
`MUIA_List_First`, and MUIKEY_RELEASE cancellation releases the state;
focused host coverage is **972/972**.

Listview pointer multi-selection now accepts Shift, Control, and Alt through
one named qualifier mask while retaining the existing `MUIA_Listview_MultiSelect`
policy; focused host coverage is **973/973**.

Stringscroll keyboard navigation now uses named MorphOS key constants,
including `MUIKEY_TOP`/`MUIKEY_BOTTOM` values 6/7. `MUIKEY_RELEASE` retires an
active guest-resident `MuiStringscrollPointerState` before the `NoInput` gate,
so a policy change cannot strand a thumb gesture; focused host coverage is
**974/974**.

Listview sortable dragging now moves the complete selected set as one stable
group when the source row is selected. The reorder scratch storage is a
guest-resident array of named `MuiListSlotState` records, with deterministic
cleanup and no managed arrays, exceptions, floating point, or raw consumer
offsets; focused host coverage is **975/975**.

The same typed drag state now invalidates its target when pointer hit-testing
leaves the child viewport, clears the public drop mark, and prevents SELECTUP
from reordering against stale geometry; focused host coverage is **976/976**.

Empty List active requests now normalize through the existing guest-backed
attribute state to `MUIV_List_Active_Off` (`-1`). Once a row exists, the normal
MorphOS selector and index clamping resumes; focused host coverage is
**977/977**.

Listview sortable drags now use the named `MuiListTestPosResult` boundary flags
to append when the pointer is over an empty row slot inside the child viewport.
Pointer movement outside the geometry remains cancellation, while single-row
and selected-group append moves use guest-resident named slot records; focused
host coverage is **978/978**.

`MUIM_List_NextSelected` now returns the active row as the implicit selection
when the initial named scalar cursor finds no selected rows, then publishes the
end sentinel on the next call. Explicit selected-row iteration remains
struct-backed; focused host coverage is **979/979**.

Listview title rows now resolve through named `MuiListTestPosResult` and
`MuiListColumnGeometry` state for both the title string and title-array paths.
SELECTUP publishes `MUIA_List_TitleClick`; only FORMAT columns marked
`SORTABLE` invoke the existing typed List sort seam. Focused host coverage is
**980/980**; title pixel styling and complete MorphOS differential parity remain
progressive.

`MUIKEY_PRESS` now selects the active row and publishes the configured
`MUIA_Listview_DefClickColumn` through the named `MuiListviewClickState` record.
Toggle and navigation paths remain unchanged; focused host coverage is
**981/981**.

Listview layout now treats the title row as non-data space. The child List's
visible data-row count and vertical scroller range use the shared named title
state before clamping `First`; focused host coverage is **982/982**.

MorphOS `MUIA_List_HScrollerVisibility` construction policy now uses the named
guest-resident `MuiListHScrollerState` record and field cursor. Malformed values
normalize to Auto; integer-only Always/Never/Auto resolution records content
width, viewport width, and the derived visibility bit for future Listview
horizontal-scroller composition. Focused host coverage is **984/984**; native
horizontal scroller composition remains progressive.

Listview now resolves the child List policy before layout. A visible horizontal
scroller reserves a named 16-pixel bottom band, vertical geometry ends above
that band, and both track/thumb pairs draw through the freestanding integer
graphics seam. Focused host coverage is **985/985**; horizontal thumb input and
content-offset scrolling remain progressive.

Horizontal thumb gestures now update the named `ScrollX`/`MaxScrollX` state
through a guest-resident drag record. List column layout, drawing, and TestPos
share that clamped content offset. Focused host coverage is **986/986**; native
wheel/key horizontal scrolling remains progressive.

Listview now handles MorphOS left/right key actions and NewMouse horizontal
wheel events through the same bounded named `ScrollX` state. Focused host
coverage is **987/987**; vertical wheel policy remains progressive.

Listview now also handles NewMouse `NM_WHEEL_UP`/`NM_WHEEL_DOWN` through the
child List's bounded named `First` state, preserving the Listview input gate and
viewport limits. Active pointer grabs leave wheel packets available for
drop-target forwarding. Focused host coverage is **988/988**; complete MorphOS
differential parity remains progressive.

Bounded Listview scroller movement now routes through the List class-aware
`First` setter and refreshes the named `TopPixel`/`VisiblePixel`/`TotalPixel`
viewport record without requiring a full layout pass. Focused host coverage is
**989/989**; native metric ABI and complete MorphOS differential parity remain
progressive.

Keyboard Listview navigation now refreshes that same named viewport record
after ListCore adjusts `First` to keep the active row visible. Page-down and
bottom navigation publish synchronized `TopPixel` values; focused host
coverage remains **989/989**. Native keyboard metric ABI and complete MorphOS
differential parity remain progressive.

Listview layout now refreshes the named viewport record after resize-time
visible-row publication and bounded `First` clamping. A larger viewport no
longer leaves stale `TopPixel` values; focused host coverage remains
**989/989**. Native resize metric ABI and complete MorphOS differential parity
remain progressive.

`MUIA_Listview_DragType` now projects into the owned List's named drag state,
so sortable pointer reordering can be enabled through the composite attribute
alone. Focused host coverage remains **989/989**; native drag/drop ABI and
complete MorphOS differential parity remain progressive.

Pointer activation now uses the List class-aware `Active` setter and refreshes
the named viewport record when a clicked row causes auto-visible scrolling.
Focused host coverage is **990/990**; native pointer metric ABI and complete
MorphOS differential parity remain progressive.

List insertion, removal, and clear operations now refresh the named viewport
record at the Entries publication boundary, keeping `TotalPixel` current after
layout without waiting for another input event. Focused host coverage is
**991/991**; native mutation metric ABI and complete MorphOS differential
parity remain progressive.

Row removal also clamps positive `First` values to the new legal viewport range
while preserving the non-empty `-1` sentinel and its zero `TopPixel` projection.
Focused host coverage remains **991/991**; native mutation-range ABI and
complete MorphOS differential parity remain progressive.

Selected-row removal, selected-row batches, and clearing a non-empty list now
publish one change-only `MUIA_List_SelectChange` notification; unselected
removal remains quiet. Focused host coverage is **992/992**; native selection
notification ABI and complete MorphOS differential parity remain progressive.

Listview-owned Lists now retain a named parent link and mirror child selection
changes to the composite's `MUIA_Listview_SelectChange` signal, including
selection and removal paths. The link is cleared during child cleanup; focused
coverage is **993/993**. Native Listview notification ABI and complete MorphOS
differential parity remain progressive.

Passive Listview mouse movement now remains outside row hit-testing; only an
active drag or scroller grab consumes movement. Hovering therefore cannot
change the active row or selection. Focused coverage is **994/994**; native
passive-input ABI and complete MorphOS differential parity remain progressive.

Exclusive Listview selection replacement now edits named slot records before
publishing one `MUIA_List_SelectChange` transition per click or keyboard
activation. Direct List selection and multiselect toggles remain unchanged;
focused coverage is **995/995**. Native selection-notification ABI and
complete MorphOS differential parity remain progressive.

Disabling `MUIA_Listview_Input` now immediately releases active named drag and
scroller records and clears the owned List drop marker, so a later pointer
packet cannot commit a gesture that has been disabled. Focused coverage remains
**995/995**; native pointer-capture ABI and complete MorphOS differential parity
remain progressive.

Changing `MUIA_Listview_ScrollerPos` or disabling `MUIA_Listview_DragType` now
uses the same named-grab cancellation boundary, preventing stale pointer
geometry from changing `MUIA_List_First` or committing a reorder. Focused
coverage is **996/996**; native pointer-capture ABI and complete MorphOS
differential parity remain progressive.

Listview BOOL and enum policies are now normalized at the composite boundary,
with invalid values falling back to documented defaults and `DragType` kept
coherent with the owned List. Focused coverage remains **996/996**; native
policy ABI and complete MorphOS differential parity remain progressive.

Floattext append now stages concatenated text through a named pending dataspace
record and commits the public Text pointer only after parsing succeeds, with
failure paths restoring the prior source. Focused coverage remains **996/996**;
native text transaction ABI and complete MorphOS differential parity remain
progressive.

List FORMAT geometry, column order, metrics, drawing, and explicit visibility
now use the named 256-column bound; a 65-column geometry regression and
balanced teardown are covered. Focused coverage is **997/997**; native
wide-column ABI and complete MorphOS differential parity remain progressive.

`MUIM_List_Jump` now refreshes the named viewport record and public pixel
projections immediately, so `TopPixel`, `VisiblePixel`, and `TotalPixel` stay
coherent without a later Layout pass. Focused coverage is **998/998**; native
scroller integration and complete MorphOS differential parity remain
progressive.

Direct class-aware `MUIA_List_Active` and `MUIA_List_First` writes now refresh
that same named viewport record immediately while preserving Active-driven
First clamping. Focused coverage is **999/999**; native direct-List metric ABI
and complete MorphOS differential parity remain progressive.

Boolean `MUIA_List_Title=TRUE` now invokes the display hook with a NULL entry
for the title row, including an empty list. Focused coverage is **1000/1000**;
native display-hook ABI and complete MorphOS differential parity remain
progressive.

Real MorphOS List display hooks now receive the current row through the named
`MuiListDisplayRowRecord` immediately before the logical column array, including
the `-1` title-row sentinel. Internal AdjustWidth, column-metric, and Draw
buffers reserve that prefix through a typed display-array storage record while
leaving the consumer-facing pointer table terminator-bounded. Focused coverage
is **1001/1001**; native display-hook ABI and complete MorphOS differential
parity remain progressive.

Direct List selection now consults `MUIA_List_MultiTestHook` before admitting an
unselected row through `MUIM_List_Select`, including Select-All and toggle
paths. Deselecting an already-selected row remains allowed. Focused coverage
is **1002/1002**; native MultiTestHook ABI and complete MorphOS differential
parity remain progressive.

`MUIM_List_Redraw` now honors the named visible viewport: concrete rows outside
`MUIA_List_First`/`MUIA_List_Visible` and the active-row sentinel with no active
entry are no-ops, while `MUIV_List_Redraw_All` still schedules a full refresh.
Focused coverage is **1003/1003**; native redraw ABI and complete MorphOS
differential parity remain progressive.

MorphOS 3.20's empty-list active projection is explicit: `MUIA_List_Active`
reads as zero for an empty List, while the named guest-resident
`MuiListActiveState` record preserves the internal no-active-row selector state.
Listview and Dirlist consume the typed active-row seam, so a public zero does
not become a real row until the cursor is actually established. Focused
coverage remains **1003/1003**; native active-cursor ABI and complete MorphOS
differential parity remain progressive.

List geometry publishes `MUIA_List_Visible` as the row capacity even when a
short List has fewer entries. Drawing and hit-testing stay bounded by the named
entry records; typed viewport pixels follow the geometry capacity, and
`MUIA_List_First` remains zero when no scrolling is possible. Focused coverage
is **1004/1004**; native geometry ABI and complete MorphOS differential parity
remain progressive.

When a List has no visible rectangle, its public `MUIA_List_Visible` and
`MUIA_List_First` values now use MorphOS's `-1` sentinel. The named viewport
state keeps pixel metrics bounded, Listview scroller code rejects the sentinel
as a row capacity, and a later visible layout restores normal state. Focused
coverage is **1006/1006**; native hidden-window ABI and complete MorphOS
differential parity remain progressive.

List pool construction policy now uses the named guest-resident
`MuiListPoolPolicyState` record. `MUIA_List_Pool`,
`MUIA_List_PoolPuddleSize`, and `MUIA_List_PoolThreshSize` retain MorphOS's
caller-owned pool identity and 2008/1024 defaults; the two size tags remain
construction-only and the record is released during teardown. No managed or
hidden host allocator is introduced. Focused coverage is **1008/1008**;
native pool ABI and complete MorphOS differential parity remain progressive.

Listview interaction policy now uses the named guest-resident
`MuiListviewInteractionPolicyState` record. `MUIA_Listview_Input`,
`MUIA_Listview_MultiSelect`, `MUIA_Listview_ScrollerPos`, and
`MUIA_Listview_DragType` retain their public ABI, while normalized keyboard,
pointer, scroller, drag, and cleanup paths consume the same struct-backed
state. Host coverage is **1010/1010**; native policy-record ABI and complete
MorphOS differential parity remain progressive.

Listview selection-change publication now uses the named guest-resident
`MuiListviewSelectionSignalState` record. Child List selection edges toggle
that composite signal, Listview getters read the record, and application
setters reject the getter-only attribute. Host coverage is **1011/1011**;
native signal-record ABI and complete MorphOS differential parity remain
progressive.

Listview also rejects runtime writes to its getter-only `List`, `ClickColumn`,
`AgainClick`, `DoubleClick`, and `SelectChange` projections. Internal
ownership, click-state, and selection-signal updates still use named
guest-resident records. Host coverage is **1012/1012**; native mutability ABI
and complete MorphOS differential parity remain progressive.

List runtime writes now reject the MorphOS getter-only and construction-only
projections `Entries`, `Visible`, `SelectChange`, `InsertPosition`, `DropMark`,
`LineHeight`, `TotalPixel`, `VisiblePixel`, `MaxColumns`, and `SourceArray` at
the dispatcher-facing `MuiListCore.SetRuntimeAttribute` boundary. Internal
navigation, persistence, and derived-state publication retain the lower-level
named List setter without ambient mutability flags or offset-based state. Host
coverage is **1013/1013**; native mutability ABI and complete MorphOS
differential parity remain progressive. The contracts follow the
[MorphOS List documentation](https://morphos-team.net/sdk/MUI/MUI_List.html).

Direct List runtime writes also reject the MorphOS `[I..]` `Input`,
`MultiSelect`, and `ScrollerPos` projections. Valid construction tags remain
in the named guest attribute store, while Listview interaction uses its separate
named policy record. Host coverage is **1014/1014**; native mutability ABI and
complete MorphOS differential parity remain progressive.

Direct List construction policy now also uses the named guest-resident
`MuiListInteractionPolicyState` record. BOOL and selector values normalize to
MorphOS defaults; explicit tags remain publicly readable, omitted defaults stay
in policy state without creating unrelated public getter attributes, and the
record is released during teardown. Host coverage is **1015/1015**; native
policy-record ABI and complete MorphOS differential parity remain progressive.
Lower-level direct List writes to these construction-only values are rejected
so the named record remains authoritative.

Direct List click projections now use the named guest-resident
`MuiListClickState` record. `AgainClick`, `ClickColumn`, `DefClickColumn`,
`DoubleClick`, and click counts remain synchronized with public attributes;
Listview click publication forwards the typed result to its owned child List,
and teardown releases the record. Host coverage is **1016/1016**; native
click-record ABI and complete MorphOS differential parity remain progressive.

Direct List hook configuration now uses the named guest-resident
`MuiListHookPolicyState` record for `ConstructHook`, `DestructHook`,
`DisplayHook`, `CompareHook`, and `MultiTestHook`. Entry ownership,
display/comparison, sorting, editing, and Listview multiselection consume the
same typed policy, and teardown releases it. Host coverage is **1017/1017**;
native hook-policy ABI and complete MorphOS differential parity remain
progressive.

Direct List sort/title interaction now uses the named guest-resident
`MuiListSortState` record for `SortColumn` and `TitleClick`. Format changes,
runtime setters, and title-click publication keep the named state and public
attributes synchronized. Host coverage is **1018/1018**; native sort/title
record ABI and complete MorphOS differential parity remain progressive.

Direct List presentation and interaction policy now uses the named
`MuiListPresentationPolicyState` record for `Editable`, `Quiet`,
`AdjustHeight`, `AdjustWidth`, `Stripes`, `ShowDropMarks`,
`DragSortable`, `DragType`, `AutoVisible`, `AutoLineHeight`, and
`MinLineHeight`. Construction/runtime normalization keeps public projections
coherent, and editing, redraw suppression, drag validation, striping, drop
marks, auto-visible navigation, and line-height calculation consume that
state. Teardown releases the record. Host coverage is **1019/1019**; native
presentation-policy ABI and complete MorphOS differential parity remain
progressive.

The named `MuiListViewportState` record now also carries the live `First`
cursor. Viewport refresh, direct navigation, and Listview scroller projection
share that field through the semantic codec while preserving existing pixel
field positions and MorphOS hidden sentinels. Host coverage is **1020/1020**;
native viewport-record ABI and complete MorphOS differential parity remain
progressive.

The same named `MuiListViewportState` record now carries the effective
`LineHeight` projection. Automatic line-height recomputation updates the
record and public attribute together, while the established pixel and `First`
field positions remain unchanged. Host coverage is **1021/1021**; native
line-height-record ABI and complete MorphOS differential parity remain
progressive.

The named `MuiListViewportState` record now also carries the visible row
capacity, including the MorphOS hidden `-1` sentinel. Navigation, redraw
visibility, and Listview scroller projection consume the typed capacity after
publication, while Layout keeps its transition value coherent before the
record is republished. Host coverage is **1022/1022**; native visible-capacity
ABI and complete MorphOS differential parity remain progressive.

The named viewport record now also carries the bounded `DropMark` insertion
cue. Drag producers and rendering update/read that typed marker while
preserving the public `-1` sentinel. Host coverage is **1023/1023**; native
drop-mark-record ABI and complete MorphOS differential parity remain
progressive.

Remaining steady-state List consumers now read `First` through the named
viewport cursor, including activation paging, hit-testing, drawing, Jump,
redraw visibility, and edit placement. Construction, Layout transitions, and
viewport refresh retain raw fallback reads where the record is being published.
Host coverage remains **1023/1023**; native first-cursor ABI and complete
MorphOS differential parity remain progressive.

The named active-cursor record now carries the selected row alongside its
presence bit. ActiveIndex, insertion/removal shifts, and empty-list handling
consume that record while retaining compatibility with raw construction
writers. Host coverage is **1024/1024**; native active-cursor ABI and complete
MorphOS differential parity remain progressive.

The scalar `MUIA_List_Title` projection now uses a named title-value record.
Title-row counting, measurement, and drawing consume that typed value while
preserving caller-owned pointers, the `TRUE` display-hook form, and
TitleArray precedence. Host coverage is **1025/1025**; native title-value ABI
and complete MorphOS differential parity remain progressive.

The getter-only `MUIA_List_SelectChange` edge now uses a named signal record.
Selection mutations update that record and the public projection together, and
Listview forwarding consumes the same typed transition. Host coverage is
**1026/1026**; native selection-signal ABI and complete MorphOS differential
parity remain progressive.

The List `FORMAT` pointer, normalized `MAXCOLUMNS` limit, and derived column
count now share the named `MuiListFormatPolicyState` record. Format
normalization, descriptor replacement, cleanup, and column consumers use the
typed policy while preserving caller-owned format strings. Host coverage is
**1027/1027**; native format-policy ABI and complete MorphOS differential
parity remain progressive.

The inherited List font pointer now uses the named `MuiListFontState` record.
Width measurement, drawing, rendering, and class-aware font updates share the
typed pointer while preserving caller ownership of the external `TextFont`.
Host coverage is **1028/1028**; native font-state ABI and complete MorphOS
differential parity remain progressive.

List baseline line-height calculation now consumes `MinLineHeight` from the
named `MuiListPresentationPolicyState` instead of rereading the raw attribute.
This keeps AskMinMax, automatic line-height refresh, and geometry policy on one
typed source. Host coverage is **1029/1029**; native baseline-policy ABI and
complete MorphOS differential parity remain progressive.

Listview's adopted `MUIA_Listview_List` relationship now uses the named
`MuiListviewChildState` record. Child lookup, getter publication, and cleanup
share the typed pointer while preserving failure-atomic adoption and the
getter-only runtime contract. Host coverage is **1030/1030**; native child
relationship ABI and complete MorphOS differential parity remain progressive.

Listview's named click state now also carries `DefClickColumn`. Keyboard and
pointer activation, getters, and runtime updates use the same typed click
record instead of a separate raw default-column projection. Host coverage is
**1031/1031**; native click-state ABI and complete MorphOS differential parity
remain progressive.

Floattext now keeps its private Text/SkipChars pointers and TabSize, Justify,
and Width policy in the named guest-resident `MuiFloattextPolicyState` record.
Parser reads, runtime policy updates, text replacement, and append commits use
that typed Dataspace record. Host coverage is **1032/1032**; native Floattext
policy ABI and complete MorphOS differential parity remain progressive.

Stringscroll now stores its seven BOOL policy attributes in the named
guest-resident `MuiStringscrollPolicyRecord`. Input, layout, scrollbar policy,
and runtime updates share that typed record while the public attribute words
remain synchronized for ABI compatibility. Host coverage is **1033/1033**;
native Stringscroll policy ABI and complete MorphOS differential parity remain
progressive.

Stringscroll now keeps `String`, `ContentWidth`, `ContentHeight`, `ScrollX`,
and `ScrollY` in the named guest-resident `MuiStringscrollStateRecord`.
Recompute, pixel scrolling, string replacement, and state readback use that
typed Dataspace record while the public/raw projections remain synchronized.
Host coverage is **1034/1034**; native Stringscroll state ABI and complete
MorphOS differential parity remain progressive.

Stringscroll Area geometry now also uses the named guest-resident
`MuiStringscrollLayoutStateRecord` for signed `Left`, `Top`, `Width`, and
`Height`. Layout, geometry reads, scrolling, clipping, and drawing share the
typed record while public Area attributes remain synchronized. Host coverage is
**1035/1035**; native layout-state ABI and complete MorphOS differential parity
remain progressive.

Stringscroll drawing context now uses the named guest-resident
`MuiStringscrollRenderStateRecord` for RenderInfo, the decoded RastPort, and
Font. Drawing and render inspection share the validated typed record while
public render attributes remain synchronized. Host coverage is **1036/1036**;
native render-state ABI and complete MorphOS differential parity remain
progressive.

Stringscroll now publishes bar visibility, effective viewport dimensions, and
maximum scroll bounds through the named guest-resident
`MuiStringscrollViewportStateRecord`. Scrolling and drawing consume that typed
derived state after recomputation. Host coverage is **1037/1037**; native
viewport-state ABI and complete MorphOS differential parity remain progressive.

Listview now publishes its signed composite and adopted-child rectangles in the
named guest-resident `MuiListviewLayoutState` record. Layout and scrollbar
geometry consume that typed state, and disposal retires it with the composite.
Host coverage is **1038/1038**; native Listview layout-state ABI and complete
MorphOS differential parity remain progressive.

Listview now keeps RenderInfo and its decoded RastPort in the named
guest-resident `MuiListviewRenderState` record. Scrollbar drawing and child
render binding use the typed context, with raw fallback only before publication.
Host coverage is **1039/1039**; native Listview render-state ABI and complete
MorphOS differential parity remain progressive.

Listview now publishes its vertical scroller projection (`Entries`, `Visible`,
`First`, and `MaxFirst`) through the named guest-resident
`MuiListviewScrollerState` record. Scroller geometry and input consume the
typed projection after child synchronization. Host coverage is **1040/1040**;
native Listview scroller-state ABI and complete MorphOS differential parity
remain progressive.

Listview now publishes horizontal track/thumb geometry and child scroll metrics
through the named guest-resident `MuiListviewHorizontalScrollerState` record.
Drawing, keyboard/wheel movement, and thumb input refresh and consume the typed
projection. Host coverage is **1041/1041**; native horizontal scroller-state
ABI and complete MorphOS differential parity remain progressive.

String.mui now publishes `BufferPos` and `DisplayPos` through the named
guest-resident `MuiStringCursorStateRecord`. Normalization, editing, cursor
visibility, and rendering share that typed projection while public attributes
remain synchronized. Host coverage is **1042/1042**; native String cursor-state
ABI and complete MorphOS differential parity remain progressive.

String.mui now publishes `MaxLen`, `Secret`, `Format`, and `Unicode` through
the named guest-resident `MuiStringPresentationStateRecord`. Normalization,
input encoding, length limits, and rendering consume the typed policy while
public attributes remain synchronized. Host coverage is **1043/1043**; native
String presentation-state ABI and complete MorphOS differential parity remain
progressive.

String.mui now publishes `Editable`, `AdvanceOnCR`, and `Multiline` through
the named guest-resident `MuiStringInteractionStateRecord`. Input gating and
CR handling consume the typed policy while public attributes remain
synchronized. Host coverage is **1044/1044**; native String interaction-state
ABI and complete MorphOS differential parity remain progressive.

String.mui spell-checking policy now uses the named guest-resident
`MuiStringSpellCheckingStateRecord`. Construction, canonical reads, and
runtime setters share the typed BOOL state while dictionary integration stays
an explicit platform capability. Host coverage is **1045/1045**; native
spellchecker ABI and complete MorphOS differential parity remain progressive.

String.mui `Acknowledge` publication now uses the named guest-resident
`MuiStringAcknowledgeStateRecord`. Return writes the current owned contents
pointer into the typed record, while Get absorbs only bounded, validated
guest-string pointers. Host coverage is **1046/1046**; native notification
timing and complete MorphOS differential parity remain progressive.

String.mui `AttachedList` now uses the named guest-resident
`MuiStringAttachedListStateRecord`. Construction, Get, runtime setters, and
Listview cursor forwarding share the validated typed relationship. Host
coverage is **1047/1047**; native attachment and complete MorphOS differential
parity remain progressive.

String.mui edit-hook policy now uses the named guest-resident
`MuiStringEditHookStateRecord` for the Hook pointer and `LonelyEditHook` BOOL.
Hook invocation, fallback policy, construction, Get, and runtime setters share
that typed state. Host coverage is **1048/1048**; native hook/action and
complete MorphOS differential parity remain progressive.

String.mui `Accept` and `Reject` now use the named guest-resident
`MuiStringFilterStateRecord`. Construction, runtime setters, raw bootstrap
synchronization, and legacy-byte/UTF-8 admission share the validated
caller-owned pointers. Host coverage is **1049/1049**; native focused String
qualification and complete MorphOS differential parity remain progressive.

String.mui ordinary `Integer` state now uses the named guest-resident
`MuiStringIntegerStateRecord`. Signed decimal seeds, runtime integer sets,
contents edits, and imports synchronize the typed value with the public ULONG
attribute. Host coverage is **1050/1050**; native numeric qualification and
complete MorphOS differential parity remain progressive.

String.mui `Placeholder` now uses the named guest-resident
`MuiStringPlaceholderStateRecord`. Construction and replacements retain an
object-owned bounded C string, while Get and drawing consume the typed pointer.
Host coverage is **1051/1051**; native placeholder qualification and complete
MorphOS differential parity remain progressive.

String.mui `Contents` now uses the named guest-resident
`MuiStringContentsStateRecord`. CopyContents publishes the object-owned
pointer through typed state, and editing, numeric synchronization, cursor
normalization, hook work, and drawing consume it. Host coverage is
**1052/1052**; native contents ABI and complete MorphOS differential parity
remain progressive.

Cycle and Radio entry vectors now use the named guest-resident
`MuiChoiceEntriesStateRecord`. Construction, active-choice navigation,
normalization, min/max sizing, and drawing consume the bounded typed vector;
direct persistence writes are folded back into the record. Host coverage is
**1053/1053**; native choice-vector ABI and complete MorphOS differential
parity remain progressive.

Text.mui `Contents` now uses the named guest-resident
`MuiTextContentsStateRecord`. Copy ownership, persistence, min/max sizing, and
drawing consume the typed contents pointer, while caller-owned references stay
unmanaged when `MUIA_Text_Copy` is false. Host coverage is **1054/1054**;
native text-contents ABI and complete MorphOS differential parity remain
progressive.

Text.mui `PreParse` now uses the named guest-resident
`MuiTextPreParseStateRecord`. Its bounded copied buffer is published through
typed state and consumed by min/max measurement and drawing; runtime
replacement preserves caller ownership. Host coverage is **1055/1055**;
native preparse ABI and complete MorphOS differential parity remain
progressive.

Numeric-family `Format` now uses the named guest-resident
`MuiNumericFormatStateRecord`. Construction and bounded replacement publish
the object-owned format through typed state, and numeric stringification reads
that state. Host coverage is **1056/1056**; native numeric-format ABI and
complete MorphOS differential parity remain progressive.

Gauge.mui `InfoText` now uses the named guest-resident
`MuiGaugeInfoTextStateRecord`. Construction and bounded replacements publish
the object-owned format through typed state, and gauge rendering consumes that
state. Host coverage is **1057/1057**; native gauge-format ABI and complete
MorphOS differential parity remain progressive.

Levelmeter.mui `Label` now uses the named guest-resident
`MuiLevelmeterLabelStateRecord`. Construction and bounded replacements
publish the object-owned label through typed state, and Levelmeter drawing
consumes that state. Host coverage is **1058/1058**; native levelmeter-label
ABI and complete MorphOS differential parity remain progressive.

Image.mui `OldImage` now uses the named guest-resident
`MuiImageOldImageStateRecord`. Construction, image sizing, and the primary
draw fallback consume the typed caller-owned pointer, while scalar projections
are folded back into that record. Host coverage is **1059/1059**; native
OldImage ABI and complete MorphOS differential parity remain progressive.

Image.mui `Image_Spec` now uses the named guest-resident
`MuiImageSpecStateRecord`. The tagged union preserves absent attributes,
builtin values, and guest specification pointers while drawing consumes the
typed state. Host coverage is **1060/1060**; native Image_Spec ABI and
complete MorphOS differential parity remain progressive.

Bitmap.mui `Bitmap` and Bodychunk.mui `Body` now share the named
guest-resident `MuiBitmapSourceStateRecord`. Construction, remap/decoding
setup, runtime source replacement, and drawing consume the class-aware typed
source pointer. Host coverage is **1061/1061**; native bitmap-source ABI and
complete MorphOS differential parity remain progressive.

Rectangle.mui `BarTitle` now uses the named guest-resident
`MuiRectangleBarTitleStateRecord`. Construction and rectangle drawing consume
the typed optional caller-owned title pointer while preserving absent titles.
Host coverage is **1062/1062**; native bar-title ABI and complete MorphOS
differential parity remain progressive.

The shared common-control `Font` pointer now uses the named guest-resident
`MuiControlFontStateRecord`. Construction, runtime projection, and drawing
preserve optional-font presence while consuming typed state. Host coverage is
**1063/1063**; native Font ABI and complete MorphOS differential parity remain
progressive.

Image.mui `FontMatchString` now uses the named guest-resident
`MuiImageFontMatchStringStateRecord`. Construction and runtime replacement
validate bounded guest strings and publish the optional caller-owned pointer
through typed state. Host coverage is **1064/1064**; native font-match ABI and
complete MorphOS differential parity remain progressive.

Bodychunk.mui decoding now uses the named guest-resident
`MuiBodychunkFormatStateRecord` for Compression, Depth, and Masking.
Construction, runtime mutation, and BODY decode consume the typed format state
without anonymous widget offsets. Host coverage is **1065/1065**; native
Bodychunk format ABI and complete MorphOS differential parity remain
progressive.

Bitmap.mui and Bodychunk.mui geometry now uses the shared named guest-resident
`MuiBitmapGeometryStateRecord` for Width and Height. Construction, runtime
mutation, min/max layout, and Bodychunk preparation consume the typed geometry
state. Host coverage is **1066/1066**; native bitmap-geometry ABI and complete
MorphOS differential parity remain progressive.

Image.mui selection and free-axis policy now use the named guest-resident
`MuiImageRenderStateRecord`. Construction, setters, input toggling, persistence,
min/max layout, and builtin-image drawing consume the shared typed state. Host
coverage is **1067/1067**; native image-render-state ABI and complete MorphOS
differential parity remain progressive.

Numeric-family controls now share the named guest-resident
`MuiNumericStateRecord` for Minimum, Maximum, Value, Default, and Reverse.
Construction, clamping, scaling, keyboard input, and numeric/levelmeter
drawing consume the typed state. Host coverage is **1069/1069**; native
Numeric-state ABI and complete MorphOS differential parity remain progressive.

Prop.mui and Scrollbar.mui now share the named guest-resident
`MuiPropRangeStateRecord` for Entries, Visible, and First. Construction,
movement, clamping, and Prop/Scrollbar drawing consume the typed range state.
Host coverage is **1070/1070**; native Prop-range ABI and complete MorphOS
differential parity remain progressive.

Gauge.mui now uses the named guest-resident `MuiGaugeStateRecord` for Maximum,
Current, Divide, and Horizontal. Construction, divide scaling, clamping,
runtime mutation, and drawing consume the typed Gauge state. Host coverage is
**1071/1071**; native Gauge-state ABI and complete MorphOS differential parity
remain progressive.

Scrollbar.mui now uses the named guest-resident `MuiScrollbarLayoutStateRecord`
for Group orientation and Scrollbar type. Child construction, Prop forwarding,
layout, and drawing consume the typed scrollbar geometry state. Host coverage
is **1072/1072**; native Scrollbar-layout ABI and complete MorphOS differential
parity remain progressive.

Slider.mui now uses the named guest-resident `MuiSliderPresentationStateRecord`
for orientation and quiet-display policy. Construction, runtime orientation
changes, min/max layout, and drawing consume the typed presentation state. Host
coverage is **1073/1073**; native Slider-presentation ABI and complete MorphOS
differential parity remain progressive.

Scale.mui now uses the named guest-resident `MuiScalePresentationStateRecord`
for orientation. Construction, runtime orientation changes, and graduated
scale drawing consume the typed presentation state. Host coverage is
**1074/1074**; native Scale-presentation ABI and complete MorphOS differential
parity remain progressive.

Gadget.mui now uses the named guest-resident `MuiGadgetInteractionStateRecord`
for InputMode, Selected, and Pressed. Construction, keyboard activation,
runtime selection changes, persistence, and drawing consume the typed
interaction state. Host coverage is **1075/1075**; native Gadget-interaction
ABI and complete MorphOS differential parity remain progressive.

Levelmeter.mui now uses the named guest-resident
`MuiLevelmeterPresentationStateRecord` for `Gauge_Horiz` orientation. Numeric
range/value behavior is unchanged, while construction and Levelmeter drawing
consume the typed presentation state. Host coverage is **1076/1076**; native
Levelmeter-presentation ABI and complete MorphOS differential parity remain
progressive.

Text.mui now uses the named guest-resident `MuiTextPresentationStateRecord`
for sizing flags, control character, marking, shortening, and high-character
policy. Construction, keyboard activation, min/max sizing, runtime mutable
attributes, and drawing consume the typed Text presentation state. Host
coverage is **1077/1077**; native Text-presentation ABI and complete MorphOS
differential parity remain progressive.

Rectangle.mui now uses the named guest-resident
`MuiRectanglePresentationStateRecord` for its horizontal and vertical bar
flags. Construction and drawing consume the typed presentation state while
the init-only MorphOS attributes remain projected through the public object
surface. Host coverage is **1078/1078**; native Rectangle-presentation ABI and
complete MorphOS differential parity remain progressive.

Common controls now share the named guest-resident
`MuiAreaPresentationStateRecord` for `Disabled`, `ShowMe`, `Background`, and
`Frame`. Construction, disabled-aware input, visibility sizing, and neutral
drawing consume the typed Area presentation state. Host coverage is
**1079/1079**; native Area-presentation ABI and complete MorphOS differential
parity remain progressive.

The shared Area layout path now uses the named guest-resident
`MuiAreaGeometryStateRecord` for signed position, size, and derived edge
values. Layout publication, Area drawing, and common-control rendering use the
typed geometry state. Host coverage is **1080/1080**; native Area-geometry ABI
and complete MorphOS differential parity remain progressive.

Floattext wrapping now resolves its effective width through the shared named
`MuiAreaGeometryStateRecord` after layout, while retaining the explicit
Floattext policy as the fallback public projection. State inspection and row
rebuilding consume the typed laid-out width instead of rereading a separate raw
scalar. Host coverage is **1081/1081**; native Floattext geometry ABI and
complete MorphOS differential parity remain progressive.

Balance adjacent-member resizing now reads neighboring rectangles as
`MuiAreaGeometryState` structs and republishes the resized records through the
shared Area layout boundary. Horizontal and vertical adjustments share typed
geometry with the public projection. Host coverage is **1082/1082**; native
Balance geometry ABI and complete MorphOS differential parity remain
progressive.

List TestPos, row drawing, and edit-object placement now resolve their
viewport rectangles through the shared named `MuiAreaGeometryStateRecord`.
Public geometry writes are reconciled at that boundary before hit-testing and
rendering. Host coverage is **1083/1083**; native List geometry ABI and
complete MorphOS differential parity remain progressive.

Listview scroller visibility and vertical/horizontal track fallbacks now read
child and composite rectangles through `MuiAreaGeometryState` structs. Layout
publication also derives its composite record from the same typed geometry
boundary. Host coverage is **1084/1084**; native Listview geometry ABI and
complete MorphOS differential parity remain progressive.

String.mui pixel scroll metrics now derive visible width and height from the
shared named `MuiAreaGeometryStateRecord`, reconciling public Area writes before
clamping scroll offsets. Host coverage is **1085/1085**; native String scroll
geometry ABI and complete MorphOS differential parity remain progressive.

Stringscroll.mui layout-state fallback and width/height setters now cross the
shared `MuiAreaGeometryState` boundary before updating the component’s own
typed layout record. Host coverage is **1086/1086**; native Stringscroll
geometry ABI and complete MorphOS differential parity remain progressive.

Dirlist and Volumelist sort policy now lives in the guest-resident named
`MuiDirlistSortStateRecord`. Canonical sort reads and runtime setters share the
record instead of rereading raw selector words. Host coverage is **1087/1087**;
native Dirlist sort-state ABI and complete MorphOS differential parity remain
progressive.

Dirlist and Volumelist filter policy now lives in the guest-resident named
`MuiDirlistFilterStateRecord`, including owned pattern pointers, normalized
BOOLs, `ExAllType`, and `FilterHook`. Scans and runtime filter setters consume
the typed record. Host coverage is **1088/1088**; native Dirlist filter-state
ABI and complete MorphOS differential parity remain progressive.

Dirlist and Volumelist scan publication now uses the guest-resident named
`MuiDirlistScanStateRecord` for status, counters, byte totals, and `IoErr`.
Scan reads and publication share that typed result boundary. Host coverage is
**1089/1089**; native Dirlist scan-state ABI and complete MorphOS differential
parity remain progressive.

Listtree object policy now uses the guest-resident named
`MuiListtreePolicyStateRecord` for active node, quiet/redraw, duplicate-name,
drag/drop, double-click, and node-hook selectors. Public object attributes stay
synchronized while internal policy reads and active-node transitions consume
the typed record. Host coverage is **1090/1090**; native Listtree policy-state
ABI and complete MorphOS differential parity remain progressive.

Volumelist `MUIA_Volumelist_ExampleMode` now uses the guest-resident named
`MuiVolumelistModeStateRecord`. Construction, population, getters, and setters
keep the public BOOL projection synchronized with the typed mode record. Host
coverage is **1091/1091**; native Volumelist mode-state ABI and complete
MorphOS differential parity remain progressive.

Virtgroup layout now republishes virtual width/height, scroll position, and
`TryFit` through the guest-resident named `MuiVirtgroupLayoutStateRecord` before
signed viewport clamping and group layout consumption. Host coverage is
**1092/1092**; native Virtgroup layout-state ABI and complete MorphOS
differential parity remain progressive.

Scrollgroup layout now republishes contents/bar pointers and free-space/no-bar
policies through the guest-resident named `MuiScrollgroupLayoutStateRecord`
before viewport and bar geometry calculation. Host coverage is **1093/1093**;
native Scrollgroup layout-state ABI and complete MorphOS differential parity
remain progressive.

Group min/max and layout now consume orientation, effective spacing, equal-size
flags, and page mode through the guest-resident named
`MuiGroupLayoutPolicyStateRecord`, including custom layout-hook boundaries. Host
coverage is **1095/1095**; native Group policy-state ABI and complete MorphOS
differential parity remain progressive.

Shared Area min/max and weighted layout now consume visibility, fixed/max
dimensions, inner margins, and effective weights through the guest-resident
named `MuiAreaLayoutPolicyStateRecord`. Host coverage is **1098/1098**; native
Area policy-state ABI and complete MorphOS differential parity remain
progressive.

Group-grid min/max and layout now consume sanitized columns/rows, spacing,
equal-size, and alignment through the guest-resident named
`MuiGroupGridStateRecord`. Host coverage is **1100/1100**; native Group-grid
policy-state ABI and complete MorphOS differential parity remain progressive.

Generic Area drawing, background fills, text dimensions, and text drawing now
consume `FillArea`, `Background`, `Frame`, and `Font` through the guest-resident
named `MuiAreaRenderPolicyStateRecord`. Host coverage is **1102/1102**; native
Area render-policy ABI and complete MorphOS differential parity remain
progressive.

Application initialization, single-task discovery, iconification transitions,
active-state writes, double-start notification, and force-quit state now use
the guest-resident named `MuiApplicationLifecycleStateRecord`. Host coverage is
**1104/1104**; native Application lifecycle ABI and complete MorphOS
differential parity remain progressive.

Native-window capability, open state, IDCMP event mask, and deferred
iconified-open state now use the guest-resident named
`MuiWindowLifecycleStateRecord`. Open/close, IDCMP, event/menu, refresh, and
application iconification paths consume the typed snapshot. Host coverage is
**1106/1106**; native Window lifecycle ABI and complete MorphOS differential
parity remain progressive.

Initializer-only alternate/primary geometry, gadget chrome, window mode,
tablet messages, and border-scroller policy now use the guest-resident named
`MuiWindowOpenPolicyStateRecord` at the native OpenWindow boundary. Host
coverage is **1108/1108**; native Window open-policy ABI and complete MorphOS
differential parity remain progressive.

Mutable Window title, requested screen, screen title, and public screen
pointers now use the guest-resident named `MuiWindowPresentationStateRecord`.
Setters/getters preserve caller-owned guest-string validation and closed-window
screen hiding. Host coverage is **1110/1110**; native Window presentation ABI
and complete MorphOS differential parity remain progressive.

Mutable Window `NoMenus`, `HasAlpha`, bounded `Opacity`, `FancyDrawing`, and
`MenuAction` values now use the guest-resident named
`MuiWindowVisualStateRecord`. Host coverage is **1112/1112**; native Window
visual-policy ABI and complete MorphOS differential parity remain progressive.

Nested Window and Application sleep depth, saved Window disabled state, and
the public sleep request use the shared guest-resident named
`MuiSleepStateRecord`. Open/close, add/remove, input suppression, application
sleep inheritance, rollback, and wake paths consume the typed snapshot with
no managed state, exceptions, or private widget offsets. Host coverage is
**1114/1114**; native sleep-state ABI and complete MorphOS differential parity
remain progressive.

Application ReturnID queue heads/tails, input-handler registration, pushed
method queue, and signal mask use the guest-resident named
`MuiApplicationSchedulerStateRecord`. Initialization, application loop,
ReturnID/Input, PushMethod/UnpushMethod, handler registration, cleanup, and
signal waits consume the typed scheduler snapshot without managed state,
exceptions, or private queue offsets. Host coverage is **1116/1116**; native
scheduler-state ABI and complete MorphOS differential parity remain
progressive.

Window snapshot flags/requests and the copied cycle-chain head, count, and
request counter use the guest-resident named
`MuiWindowInteractionStateRecord`. Snapshot, SetCycleChain, active-object
cycling/spatial selection, and disposal consume the typed interaction
snapshot without managed state, exceptions, or private chain offsets. Host
coverage is **1118/1118**; native interaction-state ABI and complete MorphOS
differential parity remain progressive.

Window close-request state and getter-only InputEvent/MouseObject pointers use
the guest-resident named `MuiWindowEventStateRecord`. Native event polling,
close-request dispatch, and pointer-publication helpers consume the typed
event snapshot without managed state, exceptions, or private pointer offsets.
Host coverage is **1120/1120**; native event-state ABI and complete MorphOS
differential parity remain progressive.

AboutMUI and ShowHelp reference windows, help name/node pointers, signed help
line, and request counters use the guest-resident named
`MuiApplicationHelpStateRecord`. Presentation methods consume the typed state
without managed state, exceptions, or private pointer offsets while retaining
MorphOS reference validation, caller-owned strings, first-open-window
resolution, and signed line semantics. Host coverage is **1122/1122**; native
help-state ABI and complete MorphOS differential parity remain progressive.

DefaultConfigItem’s requested config ID, accepted value, and saturating request
counter use the guest-resident named
`MuiApplicationDefaultConfigStateRecord`. The platform override result
publishes typed state without managed state, exceptions, or private offsets
while retaining MorphOS ULONG semantics and capability failure behavior. Host
coverage is **1124/1124**; native default-config ABI and complete MorphOS
differential parity remain progressive.

OpenConfigWindow flags, caller-owned class ID, and saturating request counter
use the guest-resident named `MuiApplicationConfigWindowStateRecord`. The
non-blocking configuration-window request publishes typed state without
managed state, exceptions, or private offsets while retaining MorphOS ULONG
flags, bounded class-ID validation, and capability failure behavior. Host
coverage is **1126/1126**; native config-window ABI and complete MorphOS
differential parity remain progressive.

BuildSettingsPanel’s requested panel number, returned panel object, and
saturating request counter use the guest-resident named
`MuiApplicationSettingsPanelStateRecord`. The application override result
publishes typed state without managed state, exceptions, or private offsets
while retaining MorphOS ULONG semantics, live-object validation, and
null-panel behavior. Host coverage is **1128/1128**; native settings-panel ABI
and complete MorphOS differential parity remain progressive.

Save/Load operation, caller-owned name selector, and saturating request, save,
and load counters use the guest-resident named
`MuiApplicationSettingsPersistenceStateRecord`. The settings persistence
boundary publishes typed state without managed state, exceptions, or private
offsets while retaining MorphOS ENV/ENVARC sentinel selectors, bounded
C-string validation, and capability failure behavior. Host coverage is
**1130/1130**; native persistence ABI and complete MorphOS differential parity
remain progressive.

CheckRefresh’s saturating check count and last refreshed-window count use the
guest-resident named `MuiApplicationRefreshStateRecord`. The refresh traversal
publishes typed state without managed state, exceptions, or private offsets
while retaining live-window filtering and MorphOS capability behavior. Host
coverage is **1132/1132**; native refresh ABI and complete MorphOS differential
parity remain progressive.

Application MenuAction and MenuHelp event values use the guest-resident named
`MuiApplicationMenuStateRecord`. Menu setters and selection publication
publish typed state without managed state, exceptions, or private offsets
while retaining MorphOS ULONG event semantics and getter-only MenuHelp
behavior. Host coverage is **1134/1134**; native menu-event ABI and complete
MorphOS differential parity remain progressive.

Application DiskObject, DropObject, and Menustrip relationships use the
guest-resident named `MuiApplicationObjectStateRecord`. Relationship setters
publish typed pointer state without managed state, exceptions, or private
offsets while retaining caller-owned DiskObject validation, live-object
validation, and menustrip family ownership. Host coverage is **1136/1136**;
native application-object ABI and complete MorphOS differential parity remain
progressive.

Application HelpFile and IconifyTitle caller-owned C-string pointers use the
guest-resident named `MuiApplicationTextStateRecord`. Text setters and
ShowHelp fallback resolution consume typed state without managed state,
exceptions, or private offsets while retaining bounded C-string validation and
MorphOS pointer semantics. Host coverage is **1138/1138**; native
application-text ABI and complete MorphOS differential parity remain
progressive.

Application Author, Base, Copyright, Description, Title, and Version
initializer pointers use the guest-resident named
`MuiApplicationIdentityStateRecord`. The initializer-only string boundary
publishes typed state without managed state, exceptions, or private offsets
while retaining bounded C-string validation, post-initialization rejection,
caller ownership, and MorphOS pointer semantics. Host coverage is
**1139/1139**; native identity ABI and complete MorphOS differential parity
remain progressive.

Application UseRexx, UseCommodities, and UseScreenNotify initializer policies
use the guest-resident named `MuiApplicationPolicyStateRecord`. Policy setters
and initialization publish typed state without managed state, exceptions, or
private offsets while retaining canonical MorphOS BOOL semantics,
initializer-only rejection, and default policy behavior. Host coverage is
**1141/1141**; native policy ABI and complete MorphOS differential parity
remain progressive.

The caller-owned `MUIA_Application_UsedClasses` STRPTR vector pointer uses the
guest-resident named `MuiApplicationUsedClassesStateRecord`. The validated
vector setter publishes typed owner state without managed state, exceptions,
or private offsets while retaining NULL-terminated vector validation and
bounded string checks. Host coverage is **1142/1142**; native UsedClasses owner
ABI and complete MorphOS differential parity remain progressive.

Repeated `MUIA_Application_Window` initializer requests use the guest-resident
named `MuiApplicationWindowRelationshipStateRecord`, retaining the last
accepted window and a saturating accepted-request count. Publication preserves
family ownership, pre-initialization-only validation, and failure-atomic
rollback without managed state, exceptions, or private offsets. Host coverage
is **1144/1144**; native relationship ABI and complete MorphOS differential
parity remain progressive.

Window `RootObject`, `Menustrip`, and `RefWindow` relationships use the
guest-resident named `MuiWindowRelationshipStateRecord`. Public getters and
validated relationship transitions publish the typed pointer snapshot while
preserving caller-owned objects, family ownership, and invalid-reference
rejection without managed state, exceptions, or private offsets. Host coverage
is **1146/1146**; native Window relationship ABI and complete MorphOS
differential parity remain progressive.

Window `Id`, `DisableKeys`, `VisibleOnMaximize`, `IsSubWindow`, and
`NeedsMouseObject` use the guest-resident named `MuiWindowControlStateRecord`.
Scalar getters and validated setters publish canonical ULONG/BOOL state while
preserving initializer-only restrictions, without managed state, exceptions, or
private offsets. Host coverage is **1148/1148**; native Window control-state ABI
and complete MorphOS differential parity remain progressive.

The opaque `MUIM_Application_SetConfigItem` item/data pair uses the named
`MuiApplicationSetConfigItemStateRecord`, with caller-owned data represented as
an explicit APTR and retained without dereferencing or copying. Mapping
validation, request counting, and cleanup remain intact. Host coverage is
**1149/1149**; native SetConfigItem ABI and complete MorphOS differential parity
remain progressive.

Transient `MUIA_AppMessage` publication and `MUIA_Window_AppWindow`
participation use the named `MuiApplicationMessageRoutingStateRecord`.
Synchronous delivery publishes and restores the caller-owned AppMessage APTR
while retaining canonical BOOL semantics, validation, and rollback. Host
coverage is **1150/1150**; native AppMessage routing ABI and complete MorphOS
differential parity remain progressive.

The caller-owned `MUIA_Application_Commands` table pointer uses the named
`MuiApplicationCommandsStateRecord`. Validated publication and getters consume
typed owner state while preserving NULL-terminated validation, explicit APTR
ownership, and failure-atomic rollback. Host coverage is **1151/1151**; native
Commands owner ABI and complete MorphOS differential parity remain progressive.

`MUIA_Window_ActiveObject` and `MUIA_Window_DefaultObject` now use the named
`MuiWindowFocusStateRecord`. Focus transitions and getter projections publish
validated object APTRs while preserving cycle-chain validation and rollback.
Host coverage is **1153/1153**; native Window focus-state ABI and complete
MorphOS differential parity remain progressive.

Public Application lifecycle attributes now project through the named
`MuiApplicationLifecycleStateRecord`. Canonical BOOL values are preserved,
publication is recursion-safe, and unrelated objects do not receive synthetic
lifecycle records. Host coverage is **1154/1154**; native Application lifecycle
getter ABI and complete MorphOS differential parity remain progressive.

`MUIA_Window_Window` and `MUIA_Window_Open` now project through the named
`MuiWindowLifecycleStateRecord`. Native window capabilities remain opaque and
Open retains canonical BOOL semantics. Host coverage is **1155/1155**; native
Window lifecycle getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_Window_CloseRequest`, `MUIA_Window_InputEvent`, and
`MUIA_Window_MouseObject` now project through the named
`MuiWindowEventStateRecord`. Close requests remain canonical BOOLs and event
object pointers remain caller-owned APTRs. Host coverage is **1156/1156**;
native Window event getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_Application_DiskObject`, `MUIA_Application_DropObject`, and
`MUIA_Application_Menustrip` now project through the named
`MuiApplicationObjectStateRecord`. Caller-owned APTR capabilities and
relationship validation remain intact. Host coverage is **1157/1157**; native
Application object getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_Application_Author`, `MUIA_Application_Base`,
`MUIA_Application_Copyright`, `MUIA_Application_Description`,
`MUIA_Application_Title`, and `MUIA_Application_Version` now project through
the named `MuiApplicationIdentityStateRecord`. Caller-owned guest C-string
pointers and initializer-only validation remain intact. Host coverage is
**1158/1158**; native Application identity getter ABI and complete MorphOS
differential parity remain progressive.

`MUIA_Application_HelpFile` and `MUIA_Application_IconifyTitle` now project
through the named `MuiApplicationTextStateRecord`. Caller-owned guest
C-string pointers and bounded string validation remain intact. Host coverage
is **1159/1159**; native Application text getter ABI and complete MorphOS
differential parity remain progressive.

`MUIA_Application_UseRexx`, `MUIA_Application_UseCommodities`, and
`MUIA_Application_UseScreenNotify` now project through the named
`MuiApplicationPolicyStateRecord`. Canonical BOOL values, initializer-only
rules, and MorphOS defaults for unconfigured applications remain intact. Host
coverage is **1160/1160**; native Application policy getter ABI and complete
MorphOS differential parity remain progressive.

`MUIA_Application_UsedClasses` now projects through the named
`MuiApplicationUsedClassesStateRecord`. The caller-owned NULL-terminated
STRPTR vector and bounded validation remain intact, and unconfigured objects
do not receive synthetic state. Host coverage is **1161/1161**; native
UsedClasses getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_Application_Window` now projects through the named
`MuiApplicationWindowRelationshipStateRecord`. Last-window and accepted-count
semantics, initializer-only relationship validation, and unconfigured-object
behavior remain intact. Host coverage is **1162/1162**; native Application
window relationship getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_Application_Sleep` now projects through the shared named
`MuiSleepStateRecord`. Nested depth/request semantics, recursion-safe raw
storage, and unconfigured-object behavior remain intact. Host coverage is
**1163/1163**; native Application sleep getter ABI and complete MorphOS
differential parity remain progressive.

`MUIA_Window_Sleep` now projects through the shared named
`MuiSleepStateRecord`. Nested depth/request semantics, saved-disabled
restoration, and recursion-safe raw storage remain intact. Host coverage is
**1164/1164**; native Window sleep getter ABI and complete MorphOS differential
parity remain progressive.

Window geometry, gadget, mode, tablet, and border-scroller getters now project
through the named `MuiWindowOpenPolicyStateRecord`. Signed LONG values,
canonical MorphOS BOOL/ULONG values, initializer-only validation, and
recursion-safe raw storage remain intact. Host coverage is **1165/1165**;
native Window open-policy getter ABI and complete MorphOS differential parity
remain progressive.

`MUIA_Application_MenuAction` and `MUIA_Application_MenuHelp` now project
through the named `MuiApplicationMenuStateRecord`. Menu UserData values and
recursion-safe raw storage remain intact. Host coverage is **1166/1166**;
native Application menu getter ABI and complete MorphOS differential parity
remain progressive.

`MUIA_Window_Sleep` is included in the public handled getter set and projects
through the named `MuiSleepStateRecord`. The obsolete `MUIA_Window_Menu` alias
projects the named `MuiWindowRelationshipStateRecord` Menustrip field. Nested
sleep depth, saved-disabled restoration, relationship identity, and
recursion-safe raw storage remain intact. Host coverage is **1166/1166**;
native alias/getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_Numeric_Value` now projects through the named `MuiNumericStateRecord`
for generic `Get` and common-control `OM_GET`. Numeric, Slider, Knob,
Numericbutton, and Levelmeter share raw-only synchronization, preserving range
clamping without getter recursion. Host coverage is **1166/1166**; native
Numeric value getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_Gauge_Current` now projects through the named `MuiGaugeStateRecord` for
generic `Get` and common-control `OM_GET`. Gauge-only classification preserves
divide/clamp behavior while Levelmeter continues using Numeric-family value
state. Host coverage is **1166/1166**; native Gauge current getter ABI and
complete MorphOS differential parity remain progressive.

The remaining public Gauge fields—`MUIA_Gauge_Max`, `MUIA_Gauge_Divide`, and
`MUIA_Gauge_Horiz`—now use the same named `MuiGaugeStateRecord` for generic
`Get` and `OM_GET`. All four Gauge getters preserve Gauge-only classification,
divide/clamp behavior, and raw-safe synchronization. Host coverage is
**1166/1166**; native Gauge state getter ABI and complete MorphOS differential
parity remain progressive.

The remaining scalar Numeric fields—`MUIA_Numeric_Min`, `MUIA_Numeric_Max`,
`MUIA_Numeric_Default`, and `MUIA_Numeric_Reverse`—now use the named
`MuiNumericStateRecord` for generic `Get` and `OM_GET`, alongside
`MUIA_Numeric_Value`. Numeric-family classification and range/scale behavior
remain intact. Host coverage is **1166/1166**; native Numeric scalar getter ABI
and complete MorphOS differential parity remain progressive.

`MUIA_Numeric_Format` now uses the named `MuiNumericFormatStateRecord` for
generic `Get` and `OM_GET`. The projection preserves the owned copy and
bounded guest C-string validation, with raw storage consulted only for
bootstrap/synchronization. Host coverage remains **1166/1166**; native Numeric
format getter ABI and complete MorphOS differential parity remain progressive.

`MUIA_Gauge_InfoText` now uses the named `MuiGaugeInfoTextStateRecord` for
generic `Get` and `OM_GET`. Gauge-only classification, owned-copy lifetime,
bounded guest C-string validation, and raw-only synchronization remain intact.
Host coverage remains **1166/1166**; native Gauge InfoText getter ABI and
complete MorphOS differential parity remain progressive.

`MUIA_Levelmeter_Label` now uses the named `MuiLevelmeterLabelStateRecord` for
generic `Get` and `OM_GET`. Levelmeter-only classification, owned-copy
lifetime, bounded guest C-string validation, and raw-only synchronization
remain intact. Host coverage remains **1166/1166**; native Levelmeter label
getter ABI and complete MorphOS differential parity remain progressive.

`MUIA_Text_Contents` now uses the named `MuiTextContentsStateRecord` for
generic `Get` and `OM_GET`. Text-only classification, the `MUIA_Text_Copy`
ownership policy, bounded guest C-string validation, and raw-only
synchronization remain intact. Host coverage remains **1166/1166**; native
Text contents getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_Text_PreParse` now uses the named `MuiTextPreParseStateRecord` for
generic `Get` and `OM_GET`. The object-owned preparse copy, Text-only
classification, bounded guest C-string validation, and raw-only
synchronization remain intact. Host coverage remains **1166/1166**; native
Text PreParse getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_String_Contents` now uses the named `MuiStringContentsStateRecord` for
generic `Get` and `OM_GET`. String-owned copies, `MUIA_String_MaxLen` bounds,
String-only classification, bounded guest C-string validation, and raw-only
synchronization remain intact. Host coverage remains **1166/1166**; native
String contents getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_String_Placeholder` now uses the named `MuiStringPlaceholderStateRecord`
for generic `Get` and `OM_GET`. The object-owned 128-byte copy, String-only
classification, bounded guest C-string validation, and raw-only
synchronization remain intact. Host coverage remains **1166/1166**; native
String placeholder getter ABI and complete MorphOS differential parity remain
progressive.

Getter-only `MUIA_String_Acknowledge` now uses the named
`MuiStringAcknowledgeStateRecord` for generic `Get` and `OM_GET`. Notification-
time publication, String-only classification, bounded guest C-string
validation, and raw-only synchronization remain intact. Host coverage remains
**1166/1166**; native String acknowledgement getter ABI and complete MorphOS
differential parity remain progressive.

`MUIA_String_AttachedList` now uses the named
`MuiStringAttachedListStateRecord` for generic `Get` and `OM_GET`. The
caller-owned live `Listview.mui` relationship, pointer-only mutation,
String input-key forwarding, and class validation remain intact. Raw-only
bootstrap keeps malformed construction pointers visible to normalization so
creation fails instead of defaulting them to NULL. Host coverage remains
**1166/1166**; native String attached-list getter ABI and complete MorphOS
differential parity remain progressive.

`MUIA_String_Integer` now uses the named `MuiStringIntegerStateRecord` for
generic `Get` and `OM_GET`. Signed ULONG semantics, contents synchronization,
and construction-time integer seeds remain intact; raw-only bootstrap prevents
an absent attribute from becoming an unintended zero seed. Host coverage
remains **1166/1166**; native String integer getter ABI and complete MorphOS
differential parity remain progressive.

`MUIA_String_SpellChecking` now uses the named
`MuiStringSpellCheckingStateRecord` for generic `Get` and `OM_GET`. MorphOS
BOOL canonicalization, construction defaults, runtime setter behavior, and
raw-only synchronization remain intact. Host coverage remains **1166/1166**;
native String spell-checking getter ABI and complete MorphOS differential parity
remain progressive.

`MUIA_String_EditHook` and `MUIA_String_LonelyEditHook` now use the named
`MuiStringEditHookStateRecord` for generic `Get` and `OM_GET`. Caller-owned Hook
mapping validation, canonical BOOL behavior, native input dispatch, and
raw-only synchronization remain intact. Host coverage remains **1166/1166**;
native String edit-hook getter ABI and complete MorphOS differential parity
remain progressive.

`MUIA_String_Accept` and `MUIA_String_Reject` now use the named
`MuiStringFilterStateRecord` for generic `Get` and `OM_GET`. Caller-owned
filter strings, pointer validation, byte/UTF-8 admission, and raw-only
synchronization remain intact. Host coverage remains **1166/1166**; native
String filter getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_String_BufferPos` and `MUIA_String_DisplayPos` now use the named
`MuiStringCursorStateRecord` for generic `Get` and `OM_GET`. Cursor clamping,
display-origin visibility updates, UTF-8 logical-column behavior, and raw-only
synchronization remain intact; internal bootstrap reads stay recursion-safe.
Host coverage remains **1166/1166**; native String cursor getter ABI and
complete MorphOS differential parity remain progressive.

`MUIA_String_Editable`, `MUIA_String_AdvanceOnCR`, and
`MUIA_String_Multiline` now use the named `MuiStringInteractionStateRecord`
for generic `Get` and `OM_GET`. MorphOS BOOL canonicalization,
initializer-only Multiline behavior, editable input gating, and raw-only
synchronization remain intact. Host coverage is **1167/1167**; native String
interaction getter ABI and complete MorphOS differential parity remain
progressive.

`MUIA_String_MaxLen`, `MUIA_String_Secret`, `MUIA_String_Format`, and
`MUIA_String_Unicode` now use the named `MuiStringPresentationStateRecord`
for generic `Get` and `OM_GET`. Initializer-only setter enforcement, format
normalization, MorphOS BOOL canonicalization, UTF-8 rendering/cursor semantics,
and raw-only synchronization remain intact. Host coverage is **1167/1167**;
native String presentation getter ABI and complete MorphOS differential parity
remain progressive.

`MUIA_String_Integer64` now projects the validated object-owned guest QUAD
pointer through the existing semantic state and two-ULONG value structs for
generic `Get` and `OM_GET`. Caller-pointer isolation, signed 64-bit
parsing/stringification, contents synchronization, malformed pointer rejection,
and raw-only internal bootstrap reads remain intact. Host coverage is
**1167/1167**; native String Integer64 getter ABI and complete MorphOS
differential parity remain progressive.

`MUIA_Text_SetMin`, `MUIA_Text_SetMax`, `MUIA_Text_SetVMax`,
`MUIA_Text_ControlChar`, `MUIA_Text_Marking`, `MUIA_Text_Shorten`, and
`MUIA_Text_HiChar` now use the named `MuiTextPresentationStateRecord` for
generic `Get` and `OM_GET`. Initializer-only versus runtime-settable
enforcement, control-character normalization, shorten-mode validation,
marking/HiChar drawing behavior, and raw-only synchronization remain intact.
Host coverage is **1167/1167**; native Text presentation getter ABI and
complete MorphOS differential parity remain progressive.

Renderer-produced `MUIA_Text_Shortened` status now uses the dedicated
`MuiTextShortenedStateRecord` for generic `Get` and `OM_GET`. Draw-time
canonical BOOL publication, get-only setter enforcement, and raw compatibility
storage remain intact without a managed status flag. Host coverage is
**1167/1167**; native Text Shortened getter ABI and complete MorphOS
differential parity remain progressive.

## Stringscroll layout state

Stringscroll Area geometry uses the named `MuiStringscrollLayoutState` record
with signed `Left`, `Top`, `Width`, and `Height` fields. Recompute, scrolling,
bar reservation, page navigation, clipping, and drawing consume that shared
layout seam instead of rereading individual Area attributes. No managed
geometry object, exception, or private widget offset is used. Host coverage is
**649/649**; native Stringscroll geometry qualification remains progressive.
The contract follows the
[MorphOS Stringscroll evidence](https://morphos-team.net/sdk/MUI/MUI_String.html).

## Stringscroll render state

Stringscroll drawing uses the named `MuiStringscrollRenderState` record for
`RenderInfo`, the decoded rastport, and `Font`. The shared guest
`MUI_RenderInfo` record is validated through `MuiDrawingRenderInfoCodec`, and
the drawing path no longer depends on a private rastport field offset. Host
coverage is **650/650**; native Stringscroll rendering qualification remains
progressive. The contract follows the
[MorphOS Stringscroll evidence](https://morphos-team.net/sdk/MUI/MUI_String.html).

## Stringscroll viewport state

Stringscroll derives effective viewport dimensions, bar visibility, and bounded
scroll limits through the named `MuiStringscrollViewportState` record.
Recompute, `SetScroll`, `GetScrollState`, page-key input, and drawing consume
one shared viewport computation. Host coverage is **651/651**; native
Stringscroll viewport qualification remains progressive. The contract follows
the [MorphOS Stringscroll evidence](https://morphos-team.net/sdk/MUI/MUI_String.html).

## Floattext named state

Floattext uses the named `MuiFloattextState` record for owned text and
skip-character pointers plus `TabSize`, `Justify`, and `Width`. Construction,
rebuild, append, direct writes, and the shared List dispatcher consume that
state; no managed text object, exception path, or private widget offset is
introduced. Host coverage is **652/652**; native Floattext layout/rendering
qualification remains progressive. The contract follows the
[MorphOS Floattext evidence](https://morphos-team.net/sdk/MUI/MUI_Floattext.html).

## Dirlist filter state

Dirlist and Volumelist filtering use the named `MuiDirlistFilterState` record
for owned accept/reject/pattern pointers, filter BOOLs, `ExAllType`, and
`FilterHook`. Construction, scans, pattern matching, and mutable writes share
that state without introducing managed filesystem objects, exceptions, or
private widget offsets. Host coverage is **653/653**; native Dirlist filtering
qualification remains progressive. The contract follows the
[MorphOS Dirlist evidence](https://morphos-team.net/sdk/MUI/MUI_Dirlist.html).

## Dirlist sort state

Dirlist and Volumelist sorting use the named `MuiDirlistSortState` record for
sort type, directory ordering, and high-low direction. Unknown selectors and
BOOL values are canonicalized before the allocation-free reorder pass. Host
coverage is **654/654**; native Dirlist sorting qualification remains
progressive. The contract follows the
[MorphOS Dirlist evidence](https://morphos-team.net/sdk/MUI/MUI_Dirlist.html).

## Dirlist scan state

Dirlist and Volumelist scan results use the named `MuiDirlistScanState` record
for status, file/drawer counters, byte totals, and captured `IoErr`. Valid and
invalid scans publish the complete record through one seam, and public getters
read that same state without managed filesystem objects, exceptions, or private
widget offsets. Host coverage is **655/655**; native scan/error timing and
complete MorphOS differential qualification remain progressive. The contract
follows the [MorphOS Dirlist evidence](https://morphos-team.net/sdk/MUI/MUI_Dirlist.html).

## Dirlist entry state

Owned FileInfoBlock-like records are exposed through the named
`MuiDirlistEntryState` record. Its bounded codec validates record size and
inline name/comment strings; sorting, path construction, and mutator methods
consume the decoded fields instead of repeating private offsets. Host coverage
is **656/656**; native entry ABI and complete MorphOS differential
qualification remain progressive. The contract follows the
[MorphOS Dirlist evidence](https://morphos-team.net/sdk/MUI/MUI_Dirlist.html).

## Dirlist scan-entry state

The transient directory-capability payload is decoded into the named
`MuiDirlistScanEntryState` record. Scan filtering, counters, and owned-record
construction consume its validated type, size, protection/date, and
name/comment fields; the Volumelist producer also uses the named writer for
type/name publication, while scratch field reads remain confined to the
decoder. Host
coverage is **657/657**; native directory-capability ABI qualification remains
progressive. The contract follows the
[MorphOS Dirlist evidence](https://morphos-team.net/sdk/MUI/MUI_Dirlist.html).

## Dirlist owned-entry writer

Variable-length FileInfoBlock-like record construction, protection updates, and
failure cleanup now use the named `MuiDirlistEntryState` writer/lifecycle seam.
The public record layout remains unchanged while fixed offsets stay inside the
codec. Host coverage remains **657/657**; native entry ABI qualification is
progressive. The contract follows the
[MorphOS Dirlist evidence](https://morphos-team.net/sdk/MUI/MUI_Dirlist.html).

## Dirlist byte-total state

The `MUIA_Dirlist_NumBytes64` guest QUAD now uses the named
`MuiDirlistByteTotalState` record and bounded read/write codec. Publication and
inspection retain the 8-byte 68k layout without exposing word offsets to
callers. Host coverage is **658/658**; native QUAD ABI qualification remains
progressive. The contract follows the
[MorphOS Dirlist evidence](https://morphos-team.net/sdk/MUI/MUI_Dirlist.html).

## String presentation state

Initializer-only `MUIA_String_MaxLen`, `MUIA_String_Secret`,
`MUIA_String_Format`, and `MUIA_String_Unicode` use the named
`MuiStringPresentationState` record. Construction canonicalizes BOOLs and
unknown alignment selectors before bounded contents ownership; bounded copy,
UTF-8 metrics, secret masking, and alignment consume the same record. No
managed text state, exception path, or private widget offset is introduced.
Host coverage remains **646/646**; native presentation qualification is
progressive. See the
[MorphOS MUI String documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

## String cursor state

`MUIA_String_BufferPos` (`0x80428B6C`) and `MUIA_String_DisplayPos`
(`0x8042CCBF`) use the named `MuiStringCursorState` record. Both positions are
normalized against the bounded guest contents length during construction,
contents replacement, and runtime Set/NoNotifySet. The guest attribute IDs
remain the ABI surface; the implementation adds no managed cursor object or
private widget offset. Host coverage is **646/646**; native cursor/scroll
qualification remains progressive. See the
[MorphOS MUI String documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

## String AttachedList

`MUIA_String_AttachedList` (`0x80420FD2`) is represented by the named
`MuiStringAttachedListState` Listview pointer. Construction and runtime writes
require a live `Listview.mui` object or NULL. Supported cursor/navigation keys
are forwarded through the existing typed `MuiListviewCore.HandleInput` seam,
so the String core does not duplicate list state or ownership. No managed
callback, exception, or raw handler offset is used. Host coverage is
**642/642**; native attachment and differential focus qualification remain
progressive. See the [MorphOS MUI String documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

## String Integer64

`MUIA_String_Integer64` (`0x80424820`) is represented by the named
`MuiStringInteger64Value` QUAD record and `MuiStringInteger64State`. The fixed
8-byte guest pointer is validated and copied into object-owned dataspace;
bounded four-16-bit-limb arithmetic renders and parses signed decimal text,
including values outside the 32-bit range. No managed 64-bit conversion,
exception, runtime object, or raw offset is used. Host coverage is
**643/643**; native focused qualification remains progressive. The contract
follows the [MorphOS MUI String documentation](https://morphos-team.net/sdk/MUI/MUI_String.html)
and MorphOS [QUAD definition](https://morphos-team.net/sdk/includes/exec/types.html).

## String SpellChecking policy

`MUIA_String_SpellChecking` (`0x804266C6`) uses the named
`MuiStringSpellCheckingState.Enabled` BOOL. Construction and mutable
Set/NoNotifySet writes canonicalize non-zero values without introducing a
managed spellchecker object or private widget offset. Dictionary, marking, and
replacement behavior remain an explicit platform service capability. Host
coverage is **644/644**; native spellchecker qualification remains progressive.
The contract follows the [MorphOS MUI String documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

## String Acknowledge state

The getter-only `MUIA_String_Acknowledge` (`0x8042026C`) uses the named
`MuiStringAcknowledgeState.Contents` pointer. On Return, the current owned
guest C string is validated before publication and notification; the path adds
no managed copy, exception, or raw widget offset. Host coverage is **645/645**;
native notification timing remains progressive. The contract follows the
[MorphOS MUI String documentation](https://morphos-team.net/sdk/MUI/MUI_String.html).

## Application persistence frame state

The guest-resident Save/Load traversal stack uses the named
`MuiApplicationPersistenceFrameState` record (`Object`, `NextChild`, and
`VisitMarker`). `MuiApplicationPersistenceFrameCodec` confines the fixed
12-byte guest layout; traversal logic consumes the named state and remains
freestanding, exception-free, managed-runtime-free, and struct-first. Host
coverage is **659/659**; native persistence qualification remains progressive.

## User Data traversal frame state

The `MUIM_FindUData`, `MUIM_GetUData`, and `MUIM_SetUData` traversal stack now
uses the named `MuiUDataTraversalFrame` record with a typed `APTR Object` and
`NextChild`. Its bounded codec validates every frame read/write, and failure
is propagated through stack setup and descent without changing the public
method behavior. Host coverage is **660/660**; native User Data traversal
qualification remains progressive.

## Common-control image geometry and render-port state

CommonControl image sizing now reads the named `MuiImageGeometryState` through
`MuiImageGeometryCodec`, and common-control drawing obtains `RastPort` through
the named `MuiDrawingRenderInfoRecord`/`MuiDrawingRenderInfoCodec` seam. Fixed
guest member offsets are confined to codecs. Host coverage is **671/671**;
native Image/RenderInfo ABI and complete MorphOS differential parity remain
progressive.

## Common-control method header

`MuiCommonControlDispatcher` now selects methods through the named
`MuiCommonMethodMessage` record via `TryReadMethodId`; the dispatcher no longer
reads the method word directly. Host coverage is **672/672**; native common
packet ABI and complete MorphOS differential parity remain progressive.

## Misc specialist typed method headers

Misc specialist lifecycle, Get/Set, pointer, pair, and gadget readers now
route selector checks through the named specialist method header before
decoding typed records. Host coverage is **703/703**; native Misc packet ABI
and complete MorphOS differential parity remain progressive.

## List advanced method headers

The List advanced packet family now routes InsertSingle, Insert, positional,
pointer, pair, and CreateImage selector checks through the named collection
method header before decoding each typed record. Host coverage is **699/699**;
native List advanced packet ABI and complete MorphOS differential parity remain
progressive.

## List basic typed method headers

GetEntry and Select packet readers now route selector checks through the named
collection method header before decoding their complete records. Host coverage
is **700/700**; native List basic packet ABI and complete MorphOS differential
parity remain progressive.

## List edit method headers

The List edit packet family now routes CreateEditObject, Edit, EditDone, and
EndEdit selector checks through the named collection method header before
decoding each typed record. Host coverage is **698/698**; native List edit
packet ABI and complete MorphOS differential parity remain progressive.

## List record method headers

The List record packet family now routes Construct, Destruct, Display, Compare,
and TestPos selector checks through the named collection method header before
decoding each complete record. Host coverage is **697/697**; native List record
packet ABI and complete MorphOS differential parity remain progressive.

## SetAsString method header

SetAsString packet decoding now uses the named
`MuiSetAsStringMethodMessage` through
`MuiSetAsStringMessageCodec.TryReadMethodId`; complete attribute, format, and
value records remain codec-backed. Host coverage is **693/693**; native
SetAsString packet ABI and complete MorphOS differential parity remain
progressive.

## UserData method header

FindUData, GetUData, and SetUData packet selection now share the named
`MuiNotifyUserDataMethodMessage` through
`MuiNotifyUserDataMessageCodec.TryReadMethodId`; the existing operation records
remain named structs. Host coverage is **694/694**; native UserData packet ABI
and complete MorphOS differential parity remain progressive.

## Area activation method header

GoActive and GoInactive packet selection now uses the named
`MuiAreaActivationMethodMessage` through
`MuiAreaActivationMessageCodec.TryReadMethodId`; the shared flags record
remains struct-backed. Host coverage is **696/696**; native Area activation
packet ABI and complete MorphOS differential parity remain progressive.

## BoopsiQuery method header

BoopsiQuery packet decoding now obtains its selector through the named
`MuiBoopsiQueryMethodMessage` and
`MuiBoopsiQueryMessageCodec.TryReadMethodId` before validating the complete
geometry record. Host coverage is **695/695**; native BoopsiQuery packet ABI
and complete MorphOS differential parity remain progressive.

## Collection dispatcher method header

Collection dispatch entry points now select methods through the named
`MuiCollectionMethodMessage` via `MuiCollectionBasicMessageCodec.TryReadMethodId`.
Specialized packet codecs still validate complete records, while collection
dispatchers no longer read the method word directly. Host coverage is
**673/673**; native collection packet ABI and complete MorphOS differential
parity remain progressive.

## Application method header

The top-level dispatcher plus Application settings I/O, menu, queue, loop,
input, input-handler, application/window menu-state, window event-handler,
screen-depth, and selected window-set entry points now select methods through
the named `MuiApplicationMethodHeaderMessage` and
`MuiApplicationMethodHeaderCodec`. Their specialized packet codecs continue
to validate complete records. Host coverage is **674/674**; native
Application/Window packet ABI and complete MorphOS differential parity remain
progressive.

## Dirlist method header

Dirlist packet and full dispatch entry points now select methods through the
named `MuiDirlistMethodMessage` via `MuiDirlistMessageCodec.TryReadMethodId`.
Specialized Dirlist codecs continue to validate complete records. Host
coverage is **675/675**; native Dirlist/Volumelist packet ABI and complete
MorphOS differential parity remain progressive.

## External wrapper method header

`MuiExternalWrapperDispatcher` now selects wrapper methods through the named
`MuiExternalMethodMessage` via `MuiExternalWrapperMessageCodec.TryReadMethodId`.
Specialized Boopsi/Dtpic packet codecs continue to validate complete records.
Host coverage is **676/676**; native external-wrapper packet ABI and complete
MorphOS differential parity remain progressive.

## Menu specialist method header

`MuiMenuSpecialistDispatcher` now selects Menustrip/Menu/Menuitem methods
through the named `MuiMenuSpecialistMethodMessage` via
`MuiMenuSpecialistMessageCodec.TryReadMethodId`. Specialized menu packet
codecs continue to validate complete records. Host coverage is **677/677**;
native menu-specialist packet ABI and complete MorphOS differential parity
remain progressive.

## Color specialist method header

`MuiColorSpecialistDispatcher` now selects pen/color specialist methods
through the named `MuiColorSpecialistMethodMessage` via
`MuiColorSpecialistMessageCodec.TryReadMethodId`. Specialized color packet
codecs continue to validate complete records. Host coverage is **678/678**;
native color-specialist packet ABI and complete MorphOS differential parity
remain progressive.

## Pop specialist method header

`MuiPopSpecialistDispatcher` now selects Popstring/Popobject/Popasl methods
through the named `MuiPopSpecialistMethodMessage` via
`MuiPopSpecialistMessageCodec.TryReadMethodId`. Specialized Pop packet codecs
continue to validate complete records. Host coverage is **679/679**; native
Pop-specialist packet ABI and complete MorphOS differential parity remain
progressive.

## Process specialist method header

`MuiProcessSpecialistDispatcher` now selects Process/Slave/Semaphore methods
through the named `MuiProcessSpecialistMethodMessage` via
`MuiProcessSpecialistMessageCodec.TryReadMethodId`. Specialized Process and
Slave packet codecs continue to validate complete records. Host coverage is
**680/680**; native Process/Slave packet ABI and complete MorphOS differential
parity remain progressive.

## Listtree method header

`MuiListtreeDispatcher` now selects external Listtree.mcc methods through the
named `MuiListtreeMethodMessage` via
`MuiListtreeMessageCodec.TryReadMethodId`. Specialized Listtree packet codecs
continue to validate complete records. Host coverage is **681/681**; native
Listtree.mcc packet ABI and complete MorphOS differential parity remain
progressive.

## Layout method header

`MuiLayoutDispatcher` now selects layout, activation, drag, and fallback
methods through the named `MuiLayoutMethodMessage` via
`MuiLayoutPacketCodec.TryReadMethodId`; all fixed layout packet readers are
also kept in that codec boundary. Host coverage is **682/682**; native layout
packet ABI and complete MorphOS differential parity remain progressive.

## Headless dispatcher method header

All headless dispatcher entry points now select methods through the named
`MuiHeadlessMethodMessage` via `MuiHeadlessMessageCodec.TryReadMethodId`.
Specialized Notify, Dataspace, persistence, store, family, group, and
semaphore codecs continue to validate complete records. Host coverage is
**683/683**; native headless packet ABI and complete MorphOS differential
parity remain progressive.

## Area drag method header

`MuiAreaDragCore.Dispatch` now selects drag methods through the named
`MuiAreaDragMethodMessage` via `MuiAreaDragMessageCodec.TryReadMethodId`.
Specialized begin, drop, event, finish, query, and report codecs continue to
validate complete records. Host coverage is **684/684**; native Area drag
packet ABI and complete MorphOS differential parity remain progressive.

## Family mutation method header

Family mutation record and projection entry points now select methods through
the named `MuiFamilyMethodMessage` via
`MuiFamilyMutationMessageCodec.TryReadMethodId`. Specialized child, insert,
transfer, reorder, and sort packet codecs continue to validate complete
records. Host coverage is **685/685**; native Family packet ABI and complete
MorphOS differential parity remain progressive.

## Store method header

Store/datamap/objectmap live and packet-only dispatch entry points now select
methods through the named `MuiStoreMethodMessage` via
`MuiStoreMessageCodec.TryReadMethodId`. Specialized datamap/objectmap packet
codecs continue to validate complete records. Host coverage is **686/686**;
native Store packet ABI and complete MorphOS differential parity remain
progressive.

## Dataspace method header

Dataspace packet-only method extraction now uses the named
`MuiDataspaceMethodMessage` through
`MuiDataspaceMessageCodec.TryReadMethodId`; the existing scalar helper remains
a struct-backed adapter. Specialized Add, Find, Get, Merge, Remove, and Clear
codecs continue to validate complete records. Host coverage is **687/687**;
native Dataspace packet ABI and complete MorphOS differential parity remain
progressive.

## Dataspace-IFF method header

Dataspace-IFF packet-only method extraction now uses the named
`MuiDataspaceIffMethodMessage` through
`MuiDataspaceIffMessageCodec.TryReadMethodId`; the existing scalar helper
remains a struct-backed adapter. Specialized ReadIFF and WriteIFF codecs
continue to validate complete records. Host coverage is **688/688**; native
Dataspace-IFF packet ABI and complete MorphOS differential parity remain
progressive.

## NotifyWrite method header

NotifyWrite packet-only method extraction now uses the named
`MuiNotifyWriteMethodMessage` through
`MuiNotifyWriteMessageCodec.TryReadMethodId`; the existing scalar helper
remains a struct-backed adapter. Specialized WriteLong and WriteString codecs
continue to validate complete records. Host coverage is **689/689**; native
NotifyWrite packet ABI and complete MorphOS differential parity remain
progressive.

## CallHook method header

CallHook packet decoding now obtains its selector through the named
`MuiCallHookMethodMessage` and `MuiCallHookMessageCodec.TryReadMethodId` before
validating the complete hook envelope. The existing named hook and parameter
fields remain intact. Host coverage is **691/691**; native CallHook packet ABI
and complete MorphOS differential parity remain progressive.

## Object persistence method header

Object persistence packet-only method extraction now uses the named
`MuiObjectPersistenceMethodMessage` through
`MuiObjectPersistenceMessageCodec.TryReadMethodId`; the existing scalar helper
remains a struct-backed adapter. The Export and Import codecs continue to
validate complete records. Host coverage is **690/690**; native object
persistence packet ABI and complete MorphOS differential parity remain
progressive.

## GetConfigItem method header

GetConfigItem packet decoding now obtains its selector through the named
`MuiGetConfigItemMethodMessage` and
`MuiGetConfigItemMessageCodec.TryReadMethodId` before validating the complete
config-item envelope. Host coverage is **692/692**; native GetConfigItem
packet ABI and complete MorphOS differential parity remain progressive.

## Layout service packet header

`MuiLayoutServiceCore.Dispatch` now decodes guest packets through the named
`MuiLayoutMessage` using `MuiLayoutPacketCodec.TryReadLayout`. Fixed layout
packet fields remain in the central struct-backed codec, keeping the service
freestanding and offset-light. Host coverage is **711/711**; native
layout-service packet ABI and complete MorphOS differential parity remain
progressive.

## Group change method header

Packet-only Group change dispatch now reads its selector through the named
`MuiGroupChangeMessage` and `MuiGroupChangeMessageCodec.TryReadMethodId`.
ExitChange2 flags use the named `MuiGroupExitChange2Message` record. Host
coverage is **712/712**; native Group change packet ABI and complete MorphOS
differential parity remain progressive.

## Notify method header

Notify, KillNotify, KillNotifyObject, Set, MultiSet, and FindObject readers now
consume the named `MuiNotifyMethodMessage` through
`MuiNotifyPacketCodec.TryReadMethodId` before reading their payload records.
Host coverage is **713/713**; native Notify packet ABI and complete MorphOS
differential parity remain progressive.

## UpdateConfig method header

UpdateConfig full-packet validation and redraw-entry writes now consume the
named `MuiUpdateConfigMethodMessage` through
`MuiUpdateConfigCore.TryReadMethodId`; redraw tables remain explicit value-type
records. Host coverage is **714/714**; native UpdateConfig packet ABI and
complete MorphOS differential parity remain progressive.

## Group ordering method header

Group MoveMember, Reorder, and Sort readers now consume the named
`MuiGroupOrderingMethodMessage` through
`MuiGroupOrderingMessageCodec.TryReadMethodId` before reading their payload
records. Host coverage is **715/715**; native Group ordering packet ABI and
complete MorphOS differential parity remain progressive.

## Application input method headers

Application ReturnId, Input, InputBuffered, and InputHandler readers now use
the named `MuiApplicationMethodHeaderMessage` codec before consuming typed
payload records. Host coverage is **716/716**; native Application input packet
ABI and complete MorphOS differential parity remain progressive.

## Application queue method headers

Application PushMethod and UnpushMethod readers now use the named
`MuiApplicationMethodHeaderMessage` codec before consuming queue payload
records. Host coverage is **717/717**; native Application queue packet ABI and
complete MorphOS differential parity remain progressive.

## Application presentation method headers

Application ShowHelp and AboutMUI readers now use the named
`MuiApplicationMethodHeaderMessage` codec before consuming presentation
payload records. Host coverage is **718/718**; native Application presentation
packet ABI and complete MorphOS differential parity remain progressive.

## Application settings method headers

Application SetConfigItem, OpenConfigWindow, BuildSettingsPanel, and Load/Save
settings readers now use the named `MuiApplicationMethodHeaderMessage` codec
before consuming settings payload records. Host coverage is **719/719**;
native Application settings packet ABI and complete MorphOS differential
parity remain progressive.

## Application method-packet headers

Application ConfigId, CheckRefresh, Execute/Run, window-method, and Snapshot
readers now use the named `MuiApplicationMethodHeaderMessage` codec before
consuming typed payload records. Host coverage is **720/720**; native
Application method-packet ABI and complete MorphOS differential parity remain
progressive.

## Window cycle-chain method header

Window SetCycleChain packet decoding now uses the named
`MuiApplicationMethodHeaderMessage` codec before consuming its bounded vector
payload. Host coverage is **721/721**; native Window cycle-chain packet ABI
and complete MorphOS differential parity remain progressive.

## Application/window menu method headers

Application and window menu query/set readers now use the named
`MuiApplicationMethodHeaderMessage` codec before consuming menu payload
records. Host coverage is **722/722**; native menu packet ABI and complete
MorphOS differential parity remain progressive.

## Window event-handler method header

Window AddEventHandler and RemoveEventHandler packet decoding now uses the
named `MuiApplicationMethodHeaderMessage` codec before consuming the handler
pointer. Host coverage is **723/723**; native Window event-handler packet ABI
and complete MorphOS differential parity remain progressive.

The public `MuiWindowEventHandlerPacketCore` helper now uses the named
`MuiWindowEventHandlerPacket` record and central codec for both reads and
writes. Its packed guest offsets are isolated at the codec boundary, and
unknown methods are rejected before a packet is exposed to callers. Host
coverage is **735/735**; native packet ABI and complete MorphOS differential
parity remain progressive.

`MuiCollectionDispatcher.TryDispatch` now delegates exclusively to the
named-codec `TryDispatchPacket` route. The former duplicate offset-based
fallback for List, Listview, Stringscroll, and Floattext is no longer a live
decode path; malformed recognized packets are claimed by the typed codec and
unclaimed objects continue outward. Host coverage is **736/736**; native
collection packet ABI and complete MorphOS differential parity remain
progressive.

## Datamap/Objectmap typed method headers

Datamap and Objectmap packet decoding now uses the named `MuiStoreMethodMessage`
codec before consuming typed set, get, key, counter, and clear records. Host
coverage is **724/724**; native Datamap/Objectmap packet ABI and complete
MorphOS differential parity remain progressive.

The live external BOOPSI wrapper now constructs its `OM_SET`, `OM_GET`,
`GM_RENDER`, inline `TagItem`, and result-word scratch frames through named
`MuiExternalBoopsi*` records and `MuiExternalBoopsiPacketCodec`. Packed guest
offsets remain confined to that codec for geometry, drawing, and attribute
pass-through. Host coverage is **738/738**; native external BOOPSI packet ABI
and complete MorphOS differential parity remain progressive.

Headless object creation now consumes the shared named
`MuiAslTagItemRecord` through `MuiAslTagItemCodec`. `TAG_DONE`, `TAG_IGNORE`,
`TAG_MORE`, and `TAG_SKIP` traversal therefore uses named `Tag`/`Data` fields;
packed guest reads remain confined to the codec boundary. Host coverage is
**739/739**; native headless TagItem ABI and complete MorphOS differential
parity remain progressive.

Family projection add/remove/insert/transfer/reorder/sort paths now consume
named `MuiFamilyMutationListRecord` and `MuiFamilyMutationVectorEntry` records
through central codecs. Packed list and vector field access remains confined
to those adapters. Host coverage is **740/740**; native Family projection ABI
and complete MorphOS differential parity remain progressive.

`MUI_MakeObjectA` now emits generated attributes and `TAG_DONE` through the
shared named `MuiAslTagItemRecord` and `MuiAslTagItemCodec`. Button, control,
label, and menu-family construction paths therefore keep packed TagItem access
at the codec boundary. Host coverage is **741/741**; native MakeObjectA
TagItem ABI and complete MorphOS differential parity remain progressive.

The private `MUIA_List_TitleArray` pointer table now uses the named
`MuiListPointerSlotRecord` and `MuiListPointerSlotCodec`; its state table
pointer is typed as an `APTR`. Construction, validation, copying, and
terminator handling keep packed slot access at the codec boundary. Host
coverage is **742/742**; native List TitleArray ABI and complete MorphOS
differential parity remain progressive.

List `StringArray` validation, duplication, display-copy, lookup, and teardown
now reuse `MuiListPointerSlotRecord` and `MuiListPointerSlotCodec`, sharing the
typed pointer-table boundary with TitleArray. Host coverage remains **742/742**;
native List StringArray ABI and complete MorphOS differential parity remain
progressive.

List external entry vectors, StringArray edit/copy paths, display arrays,
insertion/sort inputs, and `GetEntry` storage now reuse
`MuiListPointerSlotRecord` and `MuiListPointerSlotCodec`. Host coverage remains
**742/742**; native List pointer-vector ABI and complete MorphOS differential
parity remain progressive.

The private `MUIA_List_ColumnOrder` state now stores its BYTE* payload in a
named `APTR` field of `MuiListColumnOrderState`. State construction,
validation, copying, freeing, and display-column lookup consume that typed
field. Host coverage is **743/743**; native List ColumnOrder ABI and complete
MorphOS differential parity remain progressive.

ExternalWrapper creation-tag patching, remembered BOOPSI tag storage and
reapplication, and OM_UPDATE notification walks now use the shared named
`MuiAslTagItemRecord` and `MuiAslTagItemCodec`. Host coverage is **744/744**;
native ExternalWrapper TagItem ABI and complete MorphOS differential parity
remain progressive.

Family reorder and Group ordering vector consumers now use the named
`MuiFamilyMutationVectorEntry` and `MuiFamilyMutationVectorCodec`, with Family
reorder address arithmetic guarded before codec entry. Host coverage is
**745/745**; native Family/Group vector ABI and complete MorphOS differential
parity remain progressive.

Poplist caller-array traversal, materialized-array copying and terminator
writes, and selection lookup now use the named `MuiPoplistArrayEntry` and
`MuiPoplistArrayEntryCodec`. Host coverage is **746/746**; native Poplist array
ABI and complete MorphOS differential parity remain progressive.

The `MUIM_MultiSet` target vector now uses the named `MuiMultiSetTargetEntry`
and `MuiMultiSetTargetEntryCodec` during target counting and mutation. Host
coverage is **747/747**; native Notify MultiSet ABI and complete MorphOS
differential parity remain progressive.

Cycle/Radio choice-array counting, Radio child construction, and active
selection lookup now use the named `MuiChoiceEntry` and `MuiChoiceEntryCodec`.
Host coverage is **748/748**; native common-control choice-array ABI and
complete MorphOS differential parity remain progressive.

`MUI_MakeObjectA` Cycle/Radio entry-vector validation now reuses the named
`MuiChoiceEntry` and `MuiChoiceEntryCodec`, rejecting malformed caller arrays
before object creation. Host coverage is **749/749**; native MakeObject
choice-vector ABI and complete MorphOS differential parity remain progressive.

The inline `MUIM_Slave_Dispatch` ULONG vector now uses the named
`MuiProcessDispatchArgumentSlot` and `MuiProcessDispatchArgumentSlotCodec` for
packet reads and reconstructed BOOPSI message writes. Host coverage is
**750/750**; native Process/Slave dispatch ABI and complete MorphOS
differential parity remain progressive.

`MUIM_Notify` follow-parameter trigger-value substitution now uses the named
`MuiNotifyFollowParameterSlot` and `MuiNotifyFollowParameterSlotCodec`.
Host coverage is **751/751**; native Notify follow-vector ABI and complete
MorphOS differential parity remain progressive.

Requester formatting for `MUI_RequestA` and `MUI_RequestObjectA` now reads
ULONG parameters through the named `MuiRequesterParameterSlot` and
`MuiRequesterParameterSlotCodec`. Host coverage is **752/752**; native
requester parameter-vector ABI and complete MorphOS differential parity remain
progressive.

Dynamic `MUIM_UpdateConfig` redraw-table object writes now use the named
`MuiUpdateConfigObjectSlot` and `MuiUpdateConfigObjectSlotCodec`. Host coverage
is **753/753**; native UpdateConfig redraw-table ABI and complete MorphOS
differential parity remain progressive.

The public 12-byte `MUI_RGBColor` block now uses the named
`MuiColorRgbRecord` and `MuiColorRgbCodec` throughout color-specialist
component, copy, and packed-color paths. Host coverage is **754/754**; native
color RGB ABI and complete MorphOS differential parity remain progressive.

Adopted Filepanel rows now use the named `MuiFilepanelRowRecord` and
`MuiFilepanelRowCodec` during row addition and recursive disposal. Host
coverage is **755/755**; native Filepanel row-table ABI and complete MorphOS
differential parity remain progressive.

Title page creation, lookup, close/compaction, and clearing now use the named
`MuiTitlePageRecord` and `MuiTitlePageCodec`. Host coverage is **756/756**;
native Title page-table ABI and complete MorphOS differential parity remain
progressive.

Mccprefs gadget registration replacement, append, unregister compaction, and
clearing now use the named `MuiMccprefsRegistryRecord` and
`MuiMccprefsRegistryCodec`. Host coverage is **757/757**; native Mccprefs
registry ABI and complete MorphOS differential parity remain progressive.

Private Scrmodelist mode append and indexed lookup now use the named
`MuiScrmodelistModeRecord` and `MuiScrmodelistModeCodec`. Host coverage is
**758/758**; native Scrmodelist mode-table ABI and complete MorphOS differential
parity remain progressive.

Group child-list and Application window-list projections now use the named
`MuiGroupExecListRecord` and `MuiGroupExecListCodec` for complete 14-byte Exec
`List` writes, including padding. Host coverage is **760/760**; native Exec
List projection ABI and complete MorphOS differential parity remain
progressive.

Menu/Menustrip/Menuitem sidecars now use the named `MuiMenuSpecialistState`
and `MuiMenuSpecialistStateCodec` for class, ownership, flags, trigger, and
notification state. Host coverage is **761/761**; native Menu sidecar ABI and
complete MorphOS differential parity remain progressive.

Pendisplay/Colorfield/Coloradjust/Palette/Penadjust instance blocks now use
the named `MuiColorSpecialistState` and `MuiColorSpecialistStateCodec` for
lifecycle, pointers, flags, and notifications. Host coverage is **762/762**;
native Color specialist ABI and complete MorphOS differential parity remain
progressive.

Popstring/Popobject/Poplist/Popasl/Popcolor/Poppen instances now use the named
`MuiPopSpecialistState` and `MuiPopSpecialistStateCodec` for class, ownership,
hooks, arrays, ASL state, selection, and notifications. Host coverage is
**763/763**; native Pop specialist ABI and complete MorphOS differential
parity remain progressive.

Misc specialist instances now use the named `MuiMiscSpecialistHeader` and
`MuiMiscSpecialistHeaderCodec` for shared class, flags, and notification state
while retaining complete 196-byte validation and class-specific regions. Host
coverage is **764/764**; native Misc specialist ABI and complete MorphOS
differential parity remain progressive.

Title specialists now use the named `MuiMiscTitleState` and
`MuiMiscTitleStateCodec` for page storage, counts, active-page state, sequence,
position, priority, and close policy. Host coverage is **765/765**; native
Title specialist ABI and complete MorphOS differential parity remain
progressive.

Filepanel service state now uses the named `MuiMiscFilepanelServiceState` and
`MuiMiscFilepanelServiceStateCodec` for FilterFunc, ASL state, adopted rows,
row count, and hook scratch. Host coverage is **766/766**; native Filepanel
service-state ABI and complete MorphOS differential parity remain progressive.

Misc owned strings now use the named `MuiMiscOwnedStringSlot` and
`MuiMiscOwnedStringSlotCodec` across Keyadjust, Argstring, and Filepanel.
Host coverage is **767/767**; native Misc string-slot ABI and complete
MorphOS differential parity remain progressive.

Mccprefs registry state now uses the named `MuiMiscMccprefsState` and
`MuiMiscMccprefsStateCodec` for registry storage/count and config-transfer
references. Host coverage is **768/768**; native Mccprefs state ABI and
complete MorphOS differential parity remain progressive.

Private Scrmodelist state now uses the named `MuiMiscScrmodelistState` and
`MuiMiscScrmodelistStateCodec` for mode-table storage/count and active-mode
state. Host coverage is **769/769**; native Scrmodelist state ABI and complete
MorphOS differential parity remain progressive.

Aboutmui and Panel references now use the named `MuiMiscWindowPanelState` and
`MuiMiscWindowPanelStateCodec` for caller-owned application/window bindings.
Host coverage is **770/770**; native Aboutmui/Panel reference ABI and complete
MorphOS differential parity remain progressive.

FSProtectionBits flags now use the named `MuiMiscProtectionState` and
`MuiMiscProtectionStateCodec`. Host coverage is **771/771**; native
FSProtectionBits ABI and complete MorphOS differential parity remain
progressive.

Fontdisplay natural-size state now uses the named `MuiMiscFontdisplaySize` and
`MuiMiscFontdisplaySizeCodec` for MUIM_Draw width/height publication. Host
coverage is **772/772**; native Fontdisplay size ABI and complete MorphOS
differential parity remain progressive.

External-wrapper magic, class, and lifecycle flags now use the named
`MuiExternalWrapperHeader` and `MuiExternalWrapperHeaderCodec`. Host coverage
is **773/773**; native external-wrapper header ABI and complete MorphOS
differential parity remain progressive.

Boopsi min/max dimensions and creation-tag IDs now use the named
`MuiExternalBoopsiGeometryState` and `MuiExternalBoopsiGeometryCodec`. Host
coverage is **774/774**; native Boopsi geometry ABI and complete MorphOS
differential parity remain progressive.

Setup-time Window, Screen, DrawInfo, and RastPort references now use the named
`MuiExternalDisplayState` and `MuiExternalDisplayStateCodec`. Host coverage is
**775/775**; native display-state ABI and complete MorphOS differential parity
remain progressive.

Boopsi private/public class references, opened-class ownership, object handles,
and creation tags now use the named `MuiExternalBoopsiResourceState` and
`MuiExternalBoopsiResourceCodec`. Host coverage is **776/776**; native Boopsi
resource ABI and complete MorphOS differential parity remain progressive.

Remember-buffer storage/count and the shared work scratch pointer now use the
named `MuiExternalScratchState` and `MuiExternalScratchStateCodec`. Host
coverage is **777/777**; native scratch-state ABI and complete MorphOS
differential parity remain progressive.

Dtpic caller/owned names, picture handles, alpha, minimums, and natural
dimensions now use the named `MuiExternalDtpicState` and
`MuiExternalDtpicStateCodec`. Host coverage is **778/778**; native Dtpic state
ABI and complete MorphOS differential parity remain progressive.

External-wrapper notification attribute/value and count now use the named
`MuiExternalNotificationState` and `MuiExternalNotificationStateCodec`. Host
coverage is **779/779**; native notification-state ABI and complete MorphOS
differential parity remain progressive.

The external-wrapper raw instance audit removed unused offset helpers. Fixed
wrapper state is now represented through named records/codecs, with no direct
instance-field reads or writes in the MUI library. Host coverage remains
**779/779**; native ABI and differential parity remain progressive.

The color specialist's internally authored 32-byte `MUI_PenSpec` copy now uses
the named `MuiColorPenSpecRecord` and `MuiColorPenSpecCodec`, including
reserved-word preservation. Host coverage is **780/780**; native pen-spec ABI
and differential parity remain progressive.

Specialized Color, Popstring, Fontdisplay, and external-wrapper AskMinMax paths
now reuse the named `MuiMinMaxValues`/`MuiMinMaxRecordCodec` boundary. Host
coverage is **781/781**; native min/max ABI and differential parity remain
progressive.

List FORMAT descriptors now represent PREPARSE and owned PREPARSE storage as
named `APTR` fields in `MuiListFormatDescriptor`; only the named descriptor
codec converts those fields to the 40-byte guest wire layout. Replacement,
cleanup, and teardown preserve ownership behavior. Host coverage is
**786/786**; native FORMAT descriptor ABI and complete MorphOS differential
parity remain progressive.

Caller-owned List records now expose their self-describing four-byte size
header through the named `MuiListOwnedRecordHeader` and
`MuiListOwnedRecordHeaderCodec`; `FreeOwnedRecord` no longer reads the size
from an ad hoc offset. Host coverage is **787/787**; native owned-record ABI
and complete MorphOS differential parity remain progressive.

Dataspace IFF stream entries now use the named
`MuiDataspaceIffEntryHeader`/`MuiDataspaceIffEntryHeaderCodec` boundary for
their `Id` and `Length` fields. `ReadIFF` and `WriteIFF` preserve short
transfers and entry-size validation without ad hoc header access. Host
coverage is **788/788**; native Dataspace IFF header ABI and complete MorphOS
differential parity remain progressive.

`MUIM_Window_SetCycleChain` vector elements now use the named
`MuiApplicationWindowCycleChainSlot`/`MuiApplicationWindowCycleChainSlotCodec`
boundary. `SetCycleChain` preserves the NULL-terminated APTR contract,
failure-atomic replacement, and chain ownership. Host coverage is **789/789**;
native cycle-chain vector ABI and complete MorphOS differential parity remain
progressive.

Application input signal publication now uses the named
`MuiApplicationWindowSignalStorage`/`MuiApplicationWindowSignalStorageCodec`
boundary. ReturnID delivery still clears storage, while signal polling writes
the pending mask and keeps null/unmapped callers harmless. Host coverage is
**790/790**; native signal-storage ABI and complete MorphOS differential parity
remain progressive.

`MUIA_Application_UsedClasses` STRPTR vector elements now use the named
`MuiApplicationUsedClassesVectorEntry`/`MuiApplicationUsedClassesVectorEntryCodec`
boundary. Validation preserves NULL termination and bounded guest-string
checks without the previous direct helper construction. Host coverage is
**791/791**; native UsedClasses vector ABI and complete MorphOS differential
parity remain progressive.

Application settings headers and records now use the named
`MuiApplicationSettingsHeaderCodec`/`MuiApplicationSettingsRecordCodec`
boundary. The packet transport preserves magic/version, record counts,
payload lengths, and record key/length fields without embedding their offsets
in the transport adapter. Host coverage is **792/792**; native settings
transport ABI and complete MorphOS differential parity remain progressive.

Datamap/Objectmap iteration now represents its caller-owned four-byte ordinal
as `MuiStoreIterationCounter` and routes reads/advancement through
`MuiStoreIterationCounterCodec`. Traversal and exhaustion behavior are
unchanged. Host coverage is **793/793**; native Store iteration ABI and
complete MorphOS differential parity remain progressive.

`MUIM_GetConfigItem` result publication now uses the named
`MuiNotifyConfigStorage`/`MuiNotifyConfigStorageCodec` boundary. Public-screen
selection, validation ordering, capability failures, and null/unmapped
rejection are unchanged. Host coverage is **794/794**; native config-storage
ABI and complete MorphOS differential parity remain progressive.

`MUIM_Listtree_TestPos` now publishes its mixed-width 12-byte result through
the named `MuiListtreeTestPosResult`/`MuiListtreeTestPosResultCodec` boundary.
Node, drop flags, list entry, and list flags retain their MorphOS layout and
semantics. Host coverage is **795/795**; native Listtree TestPos ABI and
complete MorphOS differential parity remain progressive.

The repeated caller-owned four-byte ULONG result slot used by `opGet` paths is
now represented by `MuiGuestUlongStorage` and
`MuiGuestUlongStorageCodec`. Specialist, external-wrapper, common-control,
Listtree, and Notify UserData publication keeps its existing result and
mapping behavior. Host coverage is **796/796**; native opGet storage ABI and
complete MorphOS differential parity remain progressive.

Application/Window queue nodes now expose their inline method payload through
`MuiApplicationWindowNodeCodec.TryGetPayload`. Pushed-method copying and
dispatch plus timed input-handler dispatch retain their bounded mapping and
cleanup behavior without recomputing the payload offset in production logic.
Host coverage is **797/797**; native inline-payload ABI and complete MorphOS
differential parity remain progressive.

Family reorder/sort object-pointer vectors now use named base and indexed-entry
helpers on `MuiFamilyMutationMessageCodec`. Dispatch, vector construction, and
projection traversal preserve NULL termination, mapping/overflow checks, and
ordering behavior. Host coverage is **798/798**; native Family vector ABI and
complete MorphOS differential parity remain progressive.

CallHook invocation now obtains the A1 first-parameter address through
`MuiCallHookMessageCodec.TryGetFirstParameter`, preserving fixed packet
validation, mapping checks, and callback delivery. Host coverage is
**799/799**; native CallHook tail ABI and complete MorphOS differential parity
remain progressive.

Application command-table validation now obtains indexed 36-byte records through
`MuiApplicationCommandTableCodec.TryGetEntry`, preserving NULL termination,
bounded mapping, overflow, and string validation. Host coverage is **800/800**;
native Application command-table ABI and complete MorphOS differential parity
remain progressive.

UsedClasses validation now obtains indexed STRPTR slots through
`MuiApplicationUsedClassesVectorCodec.TryGetEntry`, preserving NULL
termination, bounded mapping/overflow checks, and string validation. Host
coverage is **801/801**; native UsedClasses vector ABI and complete MorphOS
differential parity remain progressive.

Application Save/Load traversal now obtains depth-indexed guest stack frames
through `MuiApplicationPersistenceFrameCodec.TryGetFrame`. Preorder traversal,
child-frame writes, cleanup, and malformed-stack checks remain unchanged. Host
coverage is **802/802**; native persistence frame ABI and complete MorphOS
differential parity remain progressive.

Application WindowList projection construction now obtains indexed current and
successor entries through `MuiApplicationWindowListEntryVectorCodec.TryGetEntry`.
Exec links, projection ordering, cleanup, and malformed-range checks remain
unchanged. Host coverage is **803/803**; native WindowList entry ABI and
complete MorphOS differential parity remain progressive.

AppMessage validation now obtains indexed Workbench argument records through
`MuiWorkbenchArgumentVectorCodec.TryGetEntry`, preserving the 8-byte record
shape, full-span mapping, overflow checks, and guest string validation. Host
coverage is **804/804**; native AppMessage argument ABI and complete MorphOS
differential parity remain progressive.

ASL TagItem traversal now uses the named `MuiAslTagItemCursor` and
`MuiAslTagItemVectorCodec` helpers for TAG_MORE, TAG_SKIP, TAG_IGNORE, and
normal successor movement. Control-tag semantics, bounded mapping, overflow
checks, and malformed-list rejection remain unchanged. Host coverage is
**805/805**; native ASL TagItem ABI and complete MorphOS differential parity
remain progressive.

Group child-list projection construction and the two-entry qualification seam
now obtain indexed entries through
`MuiGroupChildListEntryVectorCodec.TryGetEntry`. Exec links, ordering,
cleanup, bounded mapping, overflow checks, and malformed-range rejection
remain unchanged. Host coverage is **806/806**; native Group child-list ABI
and complete MorphOS differential parity remain progressive.

Notify UserData traversal now obtains current and child stack frames through
the named `MuiUDataTraversalCursor` and
`MuiUDataTraversalFrameCodec.TryGetEntry` helpers. Preorder traversal, depth
limits, cleanup, bounded mapping, overflow checks, and malformed-range
rejection remain unchanged. Host coverage is **808/808**; native Notify
UserData frame ABI and complete MorphOS differential parity remain progressive.

MUIM_MultiSet target traversal now obtains each 4-byte target slot through the
named `MuiMultiSetTargetVectorCursor` and
`MuiMultiSetTargetVectorCodec.TryGetEntry` helpers. NULL termination, the
256-entry limit, bounded mapping, overflow checks, and malformed-range
rejection remain unchanged. Host coverage is **809/809**; native MultiSet
vector ABI and complete MorphOS differential parity remain progressive.

SetCycleChain replacement now walks caller-owned object slots through the
named `MuiApplicationWindowCycleChainCursor` and
`MuiApplicationWindowCycleChainVectorCodec.TryGetEntry` helpers. Four-byte
slots, traversal bounds, NULL termination, failure-atomic cleanup, and
malformed-range rejection remain intact. Host coverage is **815/815**; native
SetCycleChain cursor ABI and complete MorphOS differential parity remain
progressive.

Notification dispatch now obtains copied MUIM_Notify follow-parameter slots
through the named `MuiNotifyFollowParameterVectorCursor` and
`MuiNotifyFollowParameterVectorCodec.TryGetEntry` helpers. Sentinel
substitution, the 256-entry bound, bounded mapping, overflow checks, and
malformed-range rejection remain unchanged. Host coverage is **810/810**;
native follow-parameter ABI and complete MorphOS differential parity remain
progressive.

Notification creation and dispatch now obtain the trailing payload address
through `MuiHeadlessNotificationCodec.TryGetPayload`, preserving the 32-byte
header, follow-parameter copies, bounded total-size mapping, overflow checks,
and malformed-range rejection. Host coverage is **811/811**; native
notification payload ABI and complete MorphOS differential parity remain
progressive.

SetAsString Apply now obtains the caller-owned parameter tail through
`MuiSetAsStringMessageCodec.TryGetParameters`, preserving the 16-byte fixed
packet, value-tail address, bounded mapping, overflow checks, and
malformed-range rejection. Host coverage is **812/812**; native SetAsString
tail ABI and complete MorphOS differential parity remain progressive.

Application PushMethod dispatch now obtains its caller-owned argument tail
through the named `MuiApplicationPushMethodParameter` record and
`MuiApplicationQueuePacketCodec.TryGetParameters`. The 12-byte packet,
four-byte argument slots, seven-argument limit, mapping/overflow checks, and
malformed-range rejection remain intact. Host coverage is **813/813**; native
PushMethod tail ABI and complete MorphOS differential parity remain
progressive.

Window SetCycleChain dispatch now obtains the inline object vector through
`MuiWindowCycleChainPacketCodec.TryGetVector`, preserving the 8-byte packet,
four-byte object-pointer slots, NULL termination, mapping/overflow checks, and
malformed-range rejection. Host coverage is **814/814**; native SetCycleChain
vector ABI and complete MorphOS differential parity remain progressive.

Application settings Save/Load chunk transfers now resolve source and
destination addresses through the named `MuiApplicationSettingsTransferCursor`
and `MuiApplicationSettingsTransferCursorCodec.TryGetAddress` helpers. Short
transfer retries, mapping/overflow checks, and caller-owned buffers remain
intact. Host coverage is **816/816**; native settings-file transfer ABI and
complete MorphOS differential parity remain progressive.

Dataspace IFF Read/Write chunk transfers now resolve header and payload
addresses through the named `MuiDataspaceIffTransferCursor` and
`MuiDataspaceIffTransferCursorCodec.TryGetAddress` helpers. Short-transfer
retries, mapping/overflow checks, and bounded payload handling remain intact.
Host coverage is **817/817**; native Dataspace IFF transfer ABI and complete
MorphOS differential parity remain progressive.

ExternalWrapper creation-tag and OM_UPDATE walks now obtain TagItem addresses
through the named `MuiExternalTagListCursor` and
`MuiExternalTagListCursorCodec.TryGetEntry` helpers. The 8-byte TagItem
layout, 64-entry bound, TAG_DONE termination, and malformed-range checks remain
intact. Host coverage is **818/818**; native ExternalWrapper tag-list ABI and
complete MorphOS differential parity remain progressive.

ExternalWrapper remembered TagItem add/save/reapply paths now resolve their
five-entry slots through the named `MuiExternalRememberCursor` and
`MuiExternalRememberCursorCodec.TryGetEntry` helpers. The 8-byte layout,
five-tag limit, mapping/overflow checks, and malformed-range rejection remain
intact. Host coverage is **819/819**; native remembered-tag ABI and complete
MorphOS differential parity remain progressive.

Boopsi geometry and pass-through OM_SET marshalling now resolve all five
work-buffer TagItem slots through the named `MuiExternalBoopsiTagCursor` and
`MuiExternalBoopsiTagCursorCodec.TryGetEntry` helpers. The 8-byte layout,
bounded work-buffer contract, and mapping/overflow checks remain intact. Host
coverage is **820/820**; native Boopsi work-buffer ABI and complete MorphOS
differential parity remain progressive.

Cycle/Radio choice-vector counting, active lookup, and Radio child construction
now resolve entries through the named `MuiChoiceEntryCursor` and
`MuiChoiceEntryCursorCodec.TryGetEntry` helpers. The 4-byte STRPTR layout,
NULL termination, 4096-entry bound, and mapping/overflow checks remain intact.
Host coverage is **821/821**; native choice-vector ABI and complete MorphOS
differential parity remain progressive.

MUI_MakeObjectA generated TagItem slots now use the named
`MuiAslTagItemCursor` boundary, Cycle/Radio validation reuses
`MuiChoiceEntryCursor`, and NewMenu parsing uses the named `MuiNewMenuCursor`.
The 8-byte TagItem, 4-byte STRPTR, and 20-byte NewMenu layouts plus their
bounds remain intact. Host coverage is **822/822**; native MakeObjectA vector
ABI and complete MorphOS differential parity remain progressive.

Group reorder/sort vector reads now resolve entries through the named
`MuiFamilyMutationVectorCursor` and
`MuiFamilyMutationVectorCodec.TryGetEntry` helpers. The 4-byte object-pointer
layout, NULL termination, traversal bound, and malformed-range rejection
remain intact. Host coverage is **822/822**; native Group ordering vector ABI
and complete MorphOS differential parity remain progressive.

Headless object creation now walks TAG_IGNORE, TAG_MORE, and TAG_SKIP through
the named `MuiAslTagItemCursor` and `MuiAslTagItemVectorCodec` helpers. The
8-byte TagItem layout, bounded traversal, skip-count overflow rejection, and
malformed-range behavior remain intact. Host coverage is **822/822**; native
headless tag-walk ABI and complete MorphOS differential parity remain
progressive.

Application Save/Load traversal frames now use the named
`MuiApplicationPersistenceFrameCursor` and
`MuiApplicationPersistenceFrameCursorCodec.TryGetEntry` helpers, with the
existing frame codec retained as a typed wrapper. The 12-byte frame layout,
256-entry depth bound, traversal cleanup, and malformed-range checks remain
intact. Host coverage is **833/833**; native persistence frame ABI and complete
MorphOS differential parity remain progressive.

Mccprefs registration, replacement, removal, and table access now use the
named `MuiMccprefsRegistryCursor` and
`MuiMccprefsRegistryCursorCodec.TryGetEntry` helpers. The 24-byte registry
record layout, 64-entry bound, caller-owned references, failure-atomic updates,
and malformed-range checks remain intact. Host coverage is **834/834**; native
Mccprefs registry ABI and complete MorphOS differential parity remain
progressive.

Filepanel adopted-row insertion and recursive disposal now use the named
`MuiFilepanelRowCursor` and `MuiFilepanelRowCursorCodec.TryGetEntry` helpers.
The 8-byte `{label, contents}` row layout, 64-entry bound, failure-atomic
adoption, and malformed-range checks remain intact. Host coverage is
**835/835**; native Filepanel row ABI and complete MorphOS differential parity
remain progressive.

Title page creation, compaction, close, and lookup now use the named
`MuiTitlePageCursor` and `MuiTitlePageCursorCodec.TryGetEntry` helpers. The
8-byte `{handle, flags}` page layout, 64-entry bound, active-page adjustment,
and malformed-range checks remain intact. Host coverage is **836/836**; native
Title page ABI and complete MorphOS differential parity remain progressive.

Scrmodelist mode append and indexed lookup now use the named
`MuiScrmodelistModeCursor` and `MuiScrmodelistModeCursorCodec.TryGetEntry`
helpers. The 4-byte mode record layout, 256-entry bound, private-class
behavior, and malformed-range checks remain intact. Host coverage is
**837/837**; native Scrmodelist mode ABI and complete MorphOS differential
parity remain progressive.

Family reorder/sort packet vector lookup now uses the named
`MuiFamilyInlineVectorCursor` and `MuiFamilyInlineVectorCursorCodec.TryGetEntry`
helpers, layered over the typed `MuiFamilyMutationVectorEntry` record. The
fixed packet header, 4-byte pointer-vector elements, bounded mapping, and
overflow checks remain intact. Host coverage is **838/838**; native Family
inline-vector ABI and complete MorphOS differential parity remain progressive.
