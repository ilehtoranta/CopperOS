/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

global using IMuiGuestMemory = Amiga.IAmigaGuestMemory;
using Amiga;

namespace CopperOS.MuiMaster;

public interface IMuiExecCapability
{
	APTR Allocate(uint byteSize, uint flags);
	void Free(APTR address, uint byteSize);
}

public interface IMuiBoopsiCapability
{
	APTR NewObject(APTR classPointer, APTR tagList);
	uint DoMethod(APTR obj, APTR message);
	// Deliver a method directly through the supplied BOOPSI class, matching
	// MorphOS CoerceMethod() semantics. The class pointer is an opaque guest
	// IClass*; the platform owns dispatcher resolution.
	uint CoerceMethod(APTR classPointer, APTR obj, APTR message);
	void DisposeObject(APTR obj);
}

// MorphOS MUI object persistence seam for MUIM_Export/MUIM_Import. The MUI
// method receives a live dataspace object and is invoked only for objects with
// a non-zero MUIA_ObjectID. The core validates both guest object pointers and
// the ObjectID before crossing this boundary; the capability owns class-specific
// serialization without exposing a managed store or host object graph.
public interface IMuiObjectPersistenceCapability
{
	bool ExportMuiObject(APTR obj, APTR dataspace, uint objectId);
	bool ImportMuiObject(APTR obj, APTR dataspace, uint objectId);
}

public interface IMuiIntuitionCapability
{
	APTR OpenWindow(APTR newWindow);
	void CloseWindow(APTR window);
}

public interface IMuiLayersCapability
{
	bool LockLayer(APTR layer);
	void UnlockLayer(APTR layer);
	bool BeginUpdate(APTR layer);
	void EndUpdate(APTR layer, bool completed);
	APTR PushClip(APTR layer, int left, int top, int width, int height);
	void PopClip(APTR layer, APTR previousClip);
}

public interface IMuiGraphicsCapability
{
	int TextWidth(APTR rastPort, APTR font, APTR text, int length);
	int TextHeight(APTR rastPort, APTR font);
	void SetPen(APTR rastPort, uint pen);
	void FillRectangle(APTR rastPort, int left, int top, int right, int bottom);
	void DrawLine(APTR rastPort, int x1, int y1, int x2, int y2);
	void DrawText(APTR rastPort, APTR font, int left, int baseline, APTR text,
		int length);
	void DrawImage(APTR rastPort, APTR image, int left, int top, int width,
		int height);
	bool ScheduleRedraw(APTR obj, uint flags);
}

public interface IMuiDosCapability
{
	APTR Open(APTR name, int mode);
	int Close(APTR handle);
	// Bounded DOS file transfer used by the application settings bridge. The
	// buffer is caller-owned guest memory; a short transfer is allowed and is
	// retried by the codec. No managed stream or host path is exposed here.
	int Read(APTR handle, APTR buffer, uint length);
	int Write(APTR handle, APTR buffer, uint length);
	int IoErr();
}

// Narrow IFFParse capability used by the MorphOS Dataspace ReadIFF/WriteIFF
// methods.  The MUI core owns the guest packet and dataspace record format;
// this seam owns only the stream/chunk operations.  Buffers are caller-owned
// guest memory and transfers may be short, exactly like ReadChunkBytes and
// WriteChunkBytes on Amiga's IFFParse API.  No managed stream is exposed.
public interface IMuiIffCapability
{
	int ReadChunkBytes(APTR handle, APTR buffer, uint length);
	int WriteChunkBytes(APTR handle, APTR buffer, uint length);
	// `size` is normally IFFSIZE_UNKNOWN for Dataspace WriteIFF; it is kept in
	// the seam so the native provider receives the complete PushChunk contract.
	int PushChunk(APTR handle, uint type, uint id, uint size);
	int PopChunk(APTR handle);
}

// Narrow, native-safe directory/volume enumeration seam used by the MG08
// collection classes (Dirlist.mui / Volumelist.mui). The existing
// IMuiDosCapability is the narrow file-handle seam for application settings;
// it is composed into IMuiHeadlessPlatform but remains separate from the
// bounded directory enumeration below. The directory capability is introduced
// and aggregated into IMuiHeadlessPlatform because it is
// deliberately value-type oriented: every method reads and writes only bounded
// guest memory through fixed-layout scratch records, never managed data. The
// host test platform implements it deterministically; the freestanding native
// platform implements it as a bounded no-op (empty scans, success mutators).
//
// A scanned entry is published into a caller-owned scratch block with this
// fixed layout (see MuiDirlistCore.ScanEntry* constants):
//   0  LONG  type       (>=0 drawer, <0 file; Amiga dirEntryType convention)
//   4  ULONG size low
//   8  ULONG size high
//   12 ULONG protection mask
//   16 LONG  date days
//   20 LONG  date minutes
//   24 LONG  date ticks
//   28 char  name    (bounded, NUL terminated)
//   136 char comment (bounded, NUL terminated)
public interface IMuiDirectoryCapability
{
	// Probe a directory named by a guest C-string. Returns the number of
	// entries (>= 0) that a scan will produce, or a negative value on failure
	// (missing/unreadable). On failure DirectoryError() holds the IoErr code.
	int DirectoryScan(APTR path);

	// Publish entry `index` of `path` into `storage` using the fixed scan
	// layout above. Returns false on a mid-scan failure, with DirectoryError()
	// holding the IoErr code.
	bool DirectoryEntry(APTR path, int index, APTR storage);

	// Enumerate mounted volumes for Volumelist. Returns the count (>= 0) or a
	// negative value on failure.
	int VolumeScan();

	// Publish volume `index` into `storage` using the same fixed layout (a
	// volume is reported as a drawer). Returns false on failure.
	bool VolumeEntry(int index, APTR storage);

	// Filesystem mutators. Each returns 0 on success or an IoErr value on
	// failure (matching the MUIM_Dirlist_* result contract).
	int DirectoryRename(APTR path, APTR fromName, APTR toName);
	int DirectorySetComment(APTR path, APTR name, APTR comment);
	int DirectorySetProtection(APTR path, APTR name, uint mask);

	// Most recent IoErr()-style code produced by this seam.
	int DirectoryError();
}

public interface IMuiAslCapability
{
	APTR AllocateRequest(uint requestType, APTR tags);
	int Request(APTR requester, APTR tags);
	void FreeRequest(APTR requester);
}

// MG09 requester capability for the synchronous MUI_RequestA and
// MUI_RequestObjectA entry points. Title and gadget strings remain caller-owned
// guest pointers. The service validates the caller's format/vector and, when a
// supported conversion is present, passes the platform a temporary guest C
// string with the vector already materialized; conversion-free formats retain
// their caller-owned pointer. No host formatter is implied by this seam.
// RequestObject is separate from the ASL Request overload because it models
// MUI's object-backed requester path rather than an ASL requester handle.
public interface IMuiRequesterCapability
{
	int Request(APTR application, APTR window, uint flags, APTR title,
		APTR gadgets, APTR format, APTR parameters);
	int RequestObject(APTR application, APTR window, uint flags, APTR title,
		APTR gadgets, APTR obj, APTR format, APTR parameters);
}

public interface IMuiTimerCapability
{
	uint ReadTicks();
}

// Amiga callback (struct Hook) seam. Models exec's CallHookPkt register ABI:
// the hook is entered with A0 = hook base, A2 = object, A1 = message, and its
// result is delivered in D0. The hook BASE pointer is passed (not a
// pre-extracted h_Entry) so the adapter owns reading h_Entry (hook+8) and the
// callback can reach its own h_Data (hook+16) through A0 exactly as documented.
public interface IMuiCallbackCapability
{
	uint InvokeHook(APTR hook, APTR objectAddress, APTR messageAddress);
}

public interface IMuiLibraryLoaderCapability
{
	APTR OpenLibrary(APTR name, ushort minimumVersion);
	void CloseLibrary(APTR library);
}

// MG09-only custom/external class seam. The frozen IMuiBoopsiClassCapability
// models the generic BOOPSI MakeClass/AddClass surface used by the headless
// object registry; it does not model the two MUI-specific facts that
// MUI_CreateCustomClass and MUI_GetClass require and that the authority
// documents explicitly:
//
//   * "For public classes, MUI makes sure that a6 contains a pointer to your
//      library base when your dispatcher is called."  MakeCustomClass therefore
//      takes the library base as an explicit argument (Null for private classes)
//      so the platform can bind the A6 register-delivery contract to the
//      created class.  MUI creates the dispatcher hook itself; no h_Data is exposed.
//   * A public class published by an opened "mui/<id>" library must be resolved
//      back to its struct IClass* after OpenLibrary succeeds.  ResolvePublicClass
//      performs exactly that lookup and never allocates.
//
// This capability is deliberately kept apart from the frozen aggregate so the
// frozen headless/layout/application interfaces are preserved unchanged.
public interface IMuiCustomClassCapability
{
	// Create a MUI custom class over `superClass` with `instanceSize` bytes of
	// instance data and the caller's raw dispatcher (no hook).  When
	// `libraryBase` is non-null the class is public and its dispatcher must be
	// entered with A6 = libraryBase; a null `libraryBase` selects a private
	// class whose A6 the caller manages.  Returns the created struct IClass* or Null.
	APTR MakeCustomClass(APTR superClass, ushort instanceSize, APTR dispatcher,
		APTR libraryBase);

	// Release a class created by MakeCustomClass.  Returns success.
	bool FreeCustomClass(APTR classPointer);

	// Resolve the public struct IClass* published under `classId` by a
	// previously opened "mui/<classId>" library, or Null when no public class of
	// that exact (case-sensitive) id is present.
	APTR ResolvePublicClass(APTR classId);
}

// MG09-only clip-region capability. The frozen IMuiLayersCapability models
// rectangle clipping through PushClip/PopClip, but MUI_AddClipRegion installs a
// whole struct Region on the layer (InstallClipRegion) and MUI_RemoveClipRegion
// restores the previously installed one. That region contract is not part of
// the frozen layers surface, so it is introduced separately here and only
// aggregated into the MG09 service platform. The install call returns an opaque
// "previous region" token that RemoveClipRegion hands back verbatim; the
// drawing service never interprets it.
public interface IMuiRegionCapability
{
	// Install `region` as the layer's clip region, returning the previously
	// installed region (an opaque token, possibly Null).
	APTR InstallClipRegion(APTR layer, APTR region);

	// Restore `previousRegion` (as returned by an earlier InstallClipRegion) as
	// the layer's clip region.
	void RestoreClipRegion(APTR layer, APTR previousRegion);
}

// MG09-only pen/color capability. struct MUI_PenSpec is an explicit black box,
// so the drawing service never reads its bytes; it passes the 32-byte spec
// straight through this seam. ObtainPen returns the FULL MUI pen token (the low
// MUIPEN_MASK bits are the physical pen; higher bits are MUI's own tagging) and
// ReleasePen must be given that same full token. GetRGBColor resolves a spec to
// the generated struct MUI_RGBColor without the service interpreting either.
public interface IMuiPenCapability
{
	// Obtain a pen described by the black-box `penSpec`. Returns the full pen
	// token (>= 0) or a negative value on failure.
	int ObtainPen(APTR renderInfo, APTR penSpec, uint flags);

	// Release a pen previously obtained through ObtainPen, identified by its
	// full token.
	void ReleasePen(APTR renderInfo, int pen);

	// Resolve the black-box `penSpec` into the generated MUI_RGBColor block at
	// `rgbColor`. Returns success.
	bool GetRGBColor(APTR renderInfo, APTR penSpec, APTR rgbColor);
}

// MG09 class-service platform.  Aggregates the frozen headless surface with the
// previously unused loader capability and the added custom-class capability so a
// single guest-resident service gateway (MuiClassServiceCore) can open
// "mui/<id>" libraries, resolve/register external classes, and build private or
// public custom classes.  The MG09 drawing service (MuiDrawingServiceCore) is
// additionally served through the frozen layers surface plus the region and
// pen capabilities.  The frozen IMuiHeadlessPlatform / IMuiLayoutPlatform /
// MG09-only Process/Slave scheduler seam. This is the single additive
// capability the Process.mui / Slave.mui specialist family needs beyond the
// frozen headless surface. It is deliberately narrow: it exposes only the
// scheduler-visible launch/kill/poll/signal operations that the documented
// Process state machine requires and never surfaces a host scheduler pointer,
// a host thread, or a managed callback. Launch returns an opaque, non-zero task
// token that the specialist stores verbatim as MUIA_Process_Task; every other
// call is expressed purely in terms of that token and exec-style signal masks.
//
// A deterministic host implementation can inject launch failure and drive the
// documented poll transitions; the freestanding native implementation is a
// bounded, allocation-free model. No behavior beyond the documented state
// machine is invented: ProcessPoll only reports the scheduler's own
// running/exited/error status, and the specialist maps that onto the legal
// pending/running/completed/killed/failed states.
public interface IMuiProcessCapability
{
	// Launch a scheduler-visible process running the code described by the
	// caller-owned source object/class. `name` is a guest C-string (may be
	// Null), `priority` is a signed task priority, `stackSize` a byte count.
	// Returns a non-zero opaque task token on success, or 0 on failure. No host
	// host process or callback object is created or exposed.
	uint ProcessLaunch(APTR name, int priority, uint stackSize, APTR sourceClass,
		APTR sourceObject);

	// Request termination of a launched process identified by its token.
	// Returns success (the token was known and a kill was posted).
	bool ProcessKill(uint taskToken);

	// Poll the scheduler status of a launched process. Returns one of the
	// MuiProcessSchedulerStatus codes (Unknown/Running/Completed/Failed).
	uint ProcessPoll(uint taskToken);

	// Deliver an exec-style signal mask to a launched process.
	void ProcessSignal(uint taskToken, uint signalMask);

	// Read (and consume) the signal mask received by the current task,
	// restricted to `signalMask`. Mirrors the exec SetSignal(0,0) & mask poll a
	// Slave uses to learn which coordinated signals arrived.
	uint ProcessSignalsReceived(uint signalMask);
}

// MG09-only external BOOPSI class loader seam for Boopsi.mui. The frozen
// IMuiLibraryLoaderCapability models a generic exec OpenLibrary/CloseLibrary
// pair and the frozen IMuiBoopsiCapability models NewObject/DoMethod/
// DisposeObject, but neither resolves the struct IClass* published by an
// external "system style" BOOPSI gadget class (e.g. colorwheel.gadget) that
// MUIA_Boopsi_ClassID names. This capability is the single narrow seam the
// Boopsi wrapper uses to open such a class by its guest C-string id and to
// close it again exactly once. It is deliberately pointer-only: it never
// touches a host file, a managed image, or host reflection. When
// MUIA_Boopsi_Class supplies a caller-owned private class this seam is not
// used at all; the wrapper only opens (and therefore only closes) a class it
// resolved itself through OpenExternalClass.
public interface IMuiExternalBoopsiCapability
{
	// Resolve and open the external BOOPSI class named by the guest C-string
	// `classId` (for example "colorwheel.gadget"). Returns the struct IClass*
	// to build objects from, or Null when the class cannot be opened. The
	// returned class stays valid until CloseExternalClass is called with it.
	APTR OpenExternalClass(APTR classId);

	// Close a class previously returned by OpenExternalClass. The wrapper calls
	// this exactly once per successful open.
	void CloseExternalClass(APTR classPointer);
}

// MG09-only datatypes.library picture seam for Dtpic.mui. datatypes.library
// picture objects are acquired, laid out and drawn entirely through guest
// pointers and fixed-width records: `name` is a guest C-string, `screen` and
// `rastPort` are opaque guest pointers, and the laid-out natural dimensions are
// published into a caller-owned 8-byte record (two LONGs: width then height).
// The seam never surfaces a host file, a managed image, or host reflection; a
// picture object is an opaque guest token that the wrapper owns for its
// lifetime and releases exactly once.
public interface IMuiDatatypeCapability
{
	// Acquire a datatypes.library picture object for the file named by the
	// guest C-string `name`, remapped for the friend `screen` (may be Null).
	// Returns the opaque picture Object* or Null on failure (bad name, missing
	// file, out of memory). No partial object is ever returned.
	APTR AcquirePicture(APTR name, APTR screen);

	// Release a picture object acquired through AcquirePicture. The wrapper
	// calls this exactly once per successful acquire.
	void ReleasePicture(APTR pictureObject);

	// Lay out an acquired picture (datatypes DTM_PROCLAYOUT) against `rastPort`,
	// publishing its natural width and height as two LONGs into the caller-owned
	// 8-byte `dimensionStorage`. Returns success.
	bool LayoutPicture(APTR pictureObject, APTR rastPort, APTR dimensionStorage);

	// Draw an acquired, laid-out picture (datatypes DTM_DRAW) into `rastPort`
	// at the given rectangle. Returns success.
	bool DrawPicture(APTR pictureObject, APTR rastPort, int left, int top,
		int width, int height);
}

// IMuiApplicationPlatform aggregates are left untouched. The MG09 service
// aggregate additionally carries the external BOOPSI loader and datatypes
// picture seams used by the additive Boopsi.mui / Dtpic.mui external wrapper.
public interface IMuiServicePlatform : IMuiHeadlessPlatform,
	IMuiLibraryLoaderCapability, IMuiCustomClassCapability, IMuiAslCapability,
	IMuiRequesterCapability, IMuiLayersCapability, IMuiRegionCapability,
	IMuiPenCapability, IMuiProcessCapability, IMuiExternalBoopsiCapability,
	IMuiDatatypeCapability
{
}



public interface IMuiBoopsiClassCapability : IMuiBoopsiCapability
{
	APTR MakeClass(APTR classId, APTR superClass, ushort instanceSize,
		APTR dispatcher);
	bool AddClass(APTR classPointer);
	bool RemoveClass(APTR classPointer);
	bool FreeClass(APTR classPointer);
	uint DoSuperMethod(APTR classPointer, APTR obj, APTR message);
	APTR InstanceData(APTR classPointer, APTR obj);
	bool RetainObject(APTR obj);
	bool ReleaseObject(APTR obj);
}

public interface IMuiTaskCapability
{
	uint CurrentTaskToken();
}

public interface IMuiHeadlessPlatform : IMuiGuestMemory, IMuiExecCapability,
	IMuiBoopsiClassCapability, IMuiCallbackCapability, IMuiTaskCapability,
	IMuiDirectoryCapability, IMuiObjectPersistenceCapability, IMuiDosCapability
{
	// Resolve one item from the object's local MUI configuration.  MorphOS
	// currently publishes only MUICFG_PublicScreen (0x24) through
	// MUIM_GetConfigItem; the capability returns the native opaque value and
	// never exposes a managed configuration object or host service.
	bool GetMuiConfigItem(APTR objectAddress, uint configId, out uint value);
}

public interface IMuiInputCapability
{
	int TranslateTextInput(APTR intuiMessage);
}

public interface IMuiLayoutPlatform : IMuiHeadlessPlatform,
	IMuiGraphicsCapability, IMuiLayersCapability, IMuiInputCapability
{
}

public interface IMuiApplicationPlatform : IMuiLayoutPlatform,
	IMuiTimerCapability
{
	// Show the MorphOS MUI about window for an Application object. `refWindow`
	// is a MUI Window object (not an Intuition struct Window) and may be Null.
	// The capability owns the platform-specific presentation; the core validates
	// the object boundary and records the request before/after this call.
	bool ShowMuiAbout(APTR application, APTR refWindow);
	// Show an AmigaGuide/MorphOS help page. `window` is a resolved MUI Window
	// object or Null for the default public screen; `name` and `node` may be
	// Null according to the Application method contract. The core validates
	// guest pointers and the special first-open-window selector before calling
	// this seam.
	bool ShowMuiHelp(APTR application, APTR window, APTR name, APTR node,
		int line);
	// Resolve an application-specific MUI configuration default. The core
	// validates the application object and records the accepted override; the
	// platform supplies the value without exposing managed configuration data.
	bool GetApplicationDefaultConfigItem(APTR application, uint configId,
		out uint value);
	// Build one application-provided settings panel. A Null result means the
	// application has no panel for that number; non-null results are validated
	// by the core as live MUI objects before being returned to the guest.
	APTR BuildMuiSettingsPanel(APTR application, uint number);
	// Open the application's non-blocking MUI configuration window. MorphOS
	// currently defines no flags; the raw value is preserved for ABI forward
	// compatibility. A Null class id selects the platform default; otherwise
	// the core validates the caller-owned guest C string before this seam.
	bool OpenMuiConfigWindow(APTR application, uint flags, APTR classId);
	// Persist or restore the application object graph through the platform's
	// native MUI settings service. The core handles the ENV/ENVARC sentinels and
	// bounded guest names; these seams do not expose managed persistence state.
	bool SaveMuiApplicationSettings(APTR state, APTR application, APTR name);
	bool LoadMuiApplicationSettings(APTR state, APTR application, APTR name);
	// Refresh one currently open MUI Window object after an external requester
	// may have damaged its display. Returns false when the platform cannot
	// service the window; no host UI or managed state is exposed here.
	bool RefreshMuiWindow(APTR windowObject);
	APTR OpenMuiWindow(APTR windowObject);
	void CloseMuiWindow(APTR nativeWindow);
	bool ConfigureWindowEvents(APTR nativeWindow, uint eventMask);
	uint ReadWindowEvent(APTR nativeWindow, APTR eventStorage);
	bool ActivateMuiWindow(APTR nativeWindow);
	// Apply or remove the MorphOS busy pointer for an open MUI Window. The
	// window core owns the nesting counter and calls this seam only on the
	// outermost sleep/wake transition (or when a sleeping window opens).
	bool SetMuiWindowBusy(APTR nativeWindow, bool busy);
	// Apply the initializer-only MUIA_Window_TabletMessages policy to the
	// underlying Intuition window. The guest core owns the named BOOL; the
	// platform owns WA_TabletMessages forwarding and may reject unsupported
	// hardware without exposing a managed or host UI object.
	bool SetMuiWindowTabletMessages(APTR nativeWindow, bool enabled);
	// Apply the MorphOS border-scroller policies to the underlying native
	// window. The guest values remain named mutable BOOL attributes; the
	// platform decides how those policies map to Intuition/MUI rendering.
	bool SetMuiWindowBorderScrollers(APTR nativeWindow, bool useBottom,
		bool useLeft, bool useRight);
	// Apply the initializer-only MUIA_Window_Alt* LONG values as one named
	// geometry record. The guest object retains the requested values; the
	// platform owns their native Intuition/MUI interpretation.
	bool ConfigureMuiWindowAlternateGeometry(APTR nativeWindow,
		MuiWindowPublicCore.MuiWindowAlternateGeometry geometry);
	// Apply the initializer-only MUIA_Window_* primary geometry LONG values as
	// one named record. The platform owns magic-value resolution and native
	// Intuition sizing; the guest core retains the caller's raw values.
	bool ConfigureMuiWindowGeometry(APTR nativeWindow,
		MuiWindowPublicCore.MuiWindowGeometry geometry);
	// Apply the initializer-only window gadget policy as one named ULONG
	// record. Native Intuition chrome construction remains platform-owned.
	bool ConfigureMuiWindowGadgets(APTR nativeWindow,
		MuiWindowPublicCore.MuiWindowGadgetPolicy policy);
	// Apply the initializer-only window mode policy as one named ULONG record.
	// Native AppWindow/backdrop/borderless/panel interpretation is platform-owned.
	bool ConfigureMuiWindowMode(APTR nativeWindow,
		MuiWindowPublicCore.MuiWindowModePolicy policy);
	bool MoveMuiWindow(APTR nativeWindow, bool toFront);
	// Move the screen containing an open MUI Window to front or back.
	bool MoveMuiScreen(APTR nativeWindow, bool toFront);
	// Remember or clear the position of a MUI Window. The core validates
	// the MorphOS flags and the required MUIA_Window_ID before crossing this
	// capability boundary.
	bool SnapshotMuiWindow(APTR windowObject, uint flags);
	bool SetMuiMenuState(APTR nativeWindow, uint menuId, bool enabled,
		bool check, bool checkedState);
	bool GetMuiMenuState(APTR nativeWindow, uint menuId, bool check,
		out bool state);
	bool SetApplicationIconified(APTR application, bool iconified);
	bool CoordinateRequester(APTR application, APTR window, APTR requester,
		bool open);
	uint ReadSignals(uint signalMask);
	// Wait for one scheduler-visible signal set without consuming it. The next
	// Input/NewInput call performs the exec-style read-and-clear operation.
	uint WaitMuiSignals(uint signalMask);
	void SignalTask(uint taskToken, uint signalMask);
}
