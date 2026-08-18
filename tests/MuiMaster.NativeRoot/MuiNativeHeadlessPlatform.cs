using System.Runtime.CompilerServices;
using Amiga;
using Amiga.MUI;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.NativeRoot;

public struct MuiNativeHeadlessPlatform : IMuiApplicationPlatform,
	IMuiServicePlatform, IMuiIffCapability
{
	private uint _slot;
	private const uint Allocator = 0x00036F00;
	private const uint ArenaStart = 0x00037000;
	private const uint ArenaEnd = 0x0003F000;
	private const uint SettingsFileHandle = 0x0004F000;
	private const uint SettingsFileLength = 0x0004F004;
	private const uint SettingsFilePosition = 0x0004F008;
	private const uint SettingsFileData = 0x00050000;
	private const uint SettingsFileCapacity = 0x00001000;
	private const uint IffHandle = 0x0004E000;
	private const uint IffLength = 0x0004E004;
	private const uint IffPosition = 0x0004E008;
	private const uint IffData = 0x0004E100;
	private const uint IffCapacity = 0x00000F00;

	public void Reset()
	{
		_slot = 0;
		APTR.WriteUInt32(APTR.FromPointer(Allocator), 0, ArenaStart);
		APTR.WriteUInt32(APTR.FromPointer(SettingsFileLength), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(SettingsFilePosition), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(IffLength), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(IffPosition), 0, 0);
	}

	public APTR Allocate(uint byteSize, uint flags)
	{
		if (byteSize == 0) return APTR.Null;
		var next = APTR.ReadUInt32(APTR.FromPointer(Allocator), 0);
		var aligned = (byteSize + 3u) & ~3u;
		if (next < ArenaStart || next > ArenaEnd || aligned > ArenaEnd - next)
			return APTR.Null;
		APTR.WriteUInt32(APTR.FromPointer(Allocator), 0, next + aligned);
		var result = APTR.FromPointer(next);
		Clear(result, aligned);
		return result;
	}

	public void Free(APTR address, uint byteSize)
	{
	}

	public APTR MakeClass(APTR classId, APTR superClass, ushort instanceSize,
		APTR dispatcher)
	{
		var result = Allocate(24, 0);
		if (result.IsNull) return result;
		WriteUInt32(result, 0, classId.Raw);
		WriteUInt32(result, 4, superClass.Raw);
		WriteUInt16(result, 8, instanceSize);
		WriteUInt32(result, 12, dispatcher.Raw);
		return result;
	}

	public bool AddClass(APTR classPointer) => classPointer.IsNotNull;
	public bool RemoveClass(APTR classPointer) => classPointer.IsNotNull;
	public bool FreeClass(APTR classPointer) => classPointer.IsNotNull;

	public APTR NewObject(APTR classPointer, APTR tagList)
	{
		if (classPointer.IsNull) return APTR.Null;
		var result = Allocate(16, 0);
		if (result.IsNotNull)
		{
			WriteUInt32(result, 0, classPointer.Raw);
			WriteUInt32(result, 4, 1);
		}
		return result;
	}

	public uint DoMethod(APTR obj, APTR message) => 1;
	public void DisposeObject(APTR obj)
	{
	}
	public uint DoSuperMethod(APTR classPointer, APTR obj, APTR message) => 1;
	public APTR InstanceData(APTR classPointer, APTR obj) => obj;
	public bool RetainObject(APTR obj)
	{
		if (!IsMapped(obj, 8)) return false;
		var count = ReadUInt32(obj, 4);
		WriteUInt32(obj, 4, count + 1);
		return true;
	}
	public bool ReleaseObject(APTR obj)
	{
		if (!IsMapped(obj, 8)) return false;
		var count = ReadUInt32(obj, 4);
		if (count == 0) return false;
		WriteUInt32(obj, 4, count - 1);
		return count == 1;
	}
	// Freestanding CallHookPkt marshalling: A0 = hook base, A2 = object,
	// A1 = message, result in D0. struct Hook holds h_Entry at +8 and h_Data at
	// +16. Without a real 68k callee to branch to, the adapter records the three
	// delivered registers into the hook's own h_Data block (reachable only via
	// A0) so a native root can assert the ABI, then returns h_Data as the result.
	public uint InvokeHook(APTR hook, APTR objectAddress, APTR messageAddress)
	{
		if (hook.IsNull || !IsMapped(hook, 20) || ReadUInt32(hook, 8) == 0)
			return 0;
		if (ReadUInt32(hook, 8) == 0x00CA0005u &&
			MUI_LayoutMsgCodec.TryRead(ref this, messageAddress,
				out var layoutMessage))
		{
			if (layoutMessage.lm_Type == 1)
			{
				layoutMessage.lm_MinMax = new MUI_MinMax
				{
					MinWidth = 13, MinHeight = 17, MaxWidth = 101,
					MaxHeight = 107, DefWidth = 31, DefHeight = 37,
				};
				MUI_LayoutMsgCodec.Write(ref this, messageAddress,
					layoutMessage);
				return 0;
			}
			if (layoutMessage.lm_Type == 2)
			{
				MUI_LayoutMsgCodec.Write(ref this, messageAddress,
					layoutMessage);
				return 1;
			}
		}
		var data = APTR.FromPointer(ReadUInt32(hook, 16));
		if (data.IsNotNull && IsMapped(data, 12))
		{
			WriteUInt32(data, 0, hook.Raw);
			WriteUInt32(data, 4, objectAddress.Raw);
			WriteUInt32(data, 8, messageAddress.Raw);
		}
		return data.Raw;
	}
	public uint CurrentTaskToken() => _slot + 1;

	public bool LockLayer(APTR layer) => layer.IsNotNull;
	public void UnlockLayer(APTR layer) { }
	public bool BeginUpdate(APTR layer) => layer.IsNotNull;
	public void EndUpdate(APTR layer, bool completed) { }
	public APTR PushClip(APTR layer, int left, int top, int width, int height) =>
		APTR.FromPointer(1);

	public int TranslateTextInput(APTR intuiMessage)
	{
		if (intuiMessage.IsNull || !IsMapped(intuiMessage, 28) ||
			ReadUInt32(intuiMessage, 20) != 0x00000400u) return -1;
		return ReadUInt16(intuiMessage, 24);
	}
	public void PopClip(APTR layer, APTR previousClip) { }
	public int TextWidth(APTR rastPort, APTR font, APTR text, int length) =>
		length < 0 ? 0 : length * 8;
	public int TextHeight(APTR rastPort, APTR font) => 8;
	public void SetPen(APTR rastPort, uint pen) { }
	public void FillRectangle(APTR rastPort, int left, int top, int right,
		int bottom) { }
	public void DrawLine(APTR rastPort, int x1, int y1, int x2, int y2) { }
	public void DrawText(APTR rastPort, APTR font, int left, int baseline,
		APTR text, int length) { }
	public void DrawImage(APTR rastPort, APTR image, int left, int top, int width,
		int height) { }
	public bool ScheduleRedraw(APTR obj, uint flags) => obj.IsNotNull;
	public APTR OpenMuiWindow(APTR windowObject) => windowObject;
	public bool ShowMuiAbout(APTR application, APTR refWindow) =>
		application.IsNotNull;
	public bool ShowMuiHelp(APTR application, APTR window, APTR name,
		APTR node, int line) => application.IsNotNull;
	public bool GetApplicationDefaultConfigItem(APTR application, uint configId,
		out uint value)
	{
		value = configId ^ 0xA5A55A5Au;
		return application.IsNotNull;
	}
	public bool GetMuiConfigItem(APTR objectAddress, uint configId,
		out uint value)
	{
		value = 0x0003E000;
		return objectAddress.IsNotNull && configId == 0x24;
	}
	public APTR BuildMuiSettingsPanel(APTR application, uint number)
	{
		if (application.IsNull) return APTR.Null;
		return application;
	}
	public bool OpenMuiConfigWindow(APTR application, uint flags, APTR classId) =>
		application.IsNotNull;
	public bool SaveMuiApplicationSettings(APTR state, APTR application, APTR name) =>
		application.IsNotNull;
	public bool LoadMuiApplicationSettings(APTR state, APTR application, APTR name) =>
		application.IsNotNull;
	public bool ExportMuiObject(APTR obj, APTR dataspace, uint objectId)
	{
		if (obj.IsNull) return false;
		if (dataspace.IsNull) return false;
		if (objectId == 0) return false;
		return true;
	}
	public bool ImportMuiObject(APTR obj, APTR dataspace, uint objectId)
	{
		if (obj.IsNull) return false;
		if (dataspace.IsNull) return false;
		if (objectId == 0) return false;
		return true;
	}
	public bool RefreshMuiWindow(APTR windowObject) => windowObject.IsNotNull;
	public void CloseMuiWindow(APTR nativeWindow) { }
	public bool ConfigureWindowEvents(APTR nativeWindow, uint eventMask) =>
		nativeWindow.IsNotNull;
	public uint ReadWindowEvent(APTR nativeWindow, APTR eventStorage) => 0;
	public bool ActivateMuiWindow(APTR nativeWindow) => nativeWindow.IsNotNull;
	public bool MoveMuiWindow(APTR nativeWindow, bool toFront) =>
		nativeWindow.IsNotNull;
	public bool MoveMuiScreen(APTR nativeWindow, bool toFront) =>
		nativeWindow.IsNotNull;
	public bool SnapshotMuiWindow(APTR nativeWindow, uint flags) =>
		nativeWindow.IsNotNull && flags <= 1;
	public bool SetMuiMenuState(APTR nativeWindow, uint menuId, bool enabled,
		bool check, bool checkedState) => nativeWindow.IsNotNull;
	public bool GetMuiMenuState(APTR nativeWindow, uint menuId, bool check,
		out bool state)
	{
		state = nativeWindow.IsNotNull;
		return state;
	}
	public bool SetApplicationIconified(APTR application, bool iconified) =>
		application.IsNotNull;
	public bool CoordinateRequester(APTR application, APTR window, APTR requester,
		bool open) => application.IsNotNull && requester.IsNotNull;
	public uint ReadSignals(uint signalMask) => 0;
	public uint WaitMuiSignals(uint signalMask) => 0;
	public void SignalTask(uint taskToken, uint signalMask) { }
	public uint ReadTicks() => 100;

	public byte ReadUInt8(APTR address, int offset) =>
		APTR.ReadUInt8(address, offset);
	public ushort ReadUInt16(APTR address, int offset) =>
		APTR.ReadUInt16(address, offset);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public uint ReadUInt32(APTR address, int offset) =>
		APTR.ReadUInt32(address, offset);
	public void WriteUInt8(APTR address, int offset, byte value) =>
		APTR.WriteUInt8(address, offset, value);
	public void WriteUInt16(APTR address, int offset, ushort value) =>
		APTR.WriteUInt16(address, offset, value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteUInt32(APTR address, int offset, uint value) =>
		APTR.WriteUInt32(address, offset, value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Clear(APTR address, uint byteSize)
	{
		for (var index = 0u; index < byteSize; index++)
			APTR.WriteUInt8(address, (int)index, 0);
	}
	public void Copy(APTR source, APTR destination, uint byteSize)
	{
		if (destination.Raw <= source.Raw)
		{
			for (var index = 0u; index < byteSize; index++)
				APTR.WriteUInt8(destination, (int)index,
					APTR.ReadUInt8(source, (int)index));
			return;
		}
		for (var index = byteSize; index != 0; index--)
			APTR.WriteUInt8(destination, (int)(index - 1),
				APTR.ReadUInt8(source, (int)(index - 1)));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsMapped(APTR address, uint byteSize) =>
		address.Raw >= 0x00035F00 && address.Raw <= 0x00051000 &&
		byteSize <= 0x00051000 - address.Raw;

	// The native qualification platform deliberately exposes an empty
	// filesystem.  Directory/volume collection code must therefore remain
	// failure-safe without reaching a host filesystem service.
	public int DirectoryScan(APTR path) => 0;
	public bool DirectoryEntry(APTR path, int index, APTR storage) => false;
	public int VolumeScan() => 0;
	public bool VolumeEntry(int index, APTR storage) => false;
	public int DirectoryRename(APTR path, APTR fromName, APTR toName) => 0;
	public int DirectorySetComment(APTR path, APTR name, APTR comment) => 0;
	public int DirectorySetProtection(APTR path, APTR name, uint mask) => 0;
	public int DirectoryError() => 0;
	public APTR Open(APTR name, int mode)
	{
		var length = APTR.ReadUInt32(APTR.FromPointer(SettingsFileLength), 0);
		if (mode == MuiApplicationSettingsFileCore.OldFileMode && length == 0)
			return APTR.Null;
		if (mode == MuiApplicationSettingsFileCore.NewFileMode)
			APTR.WriteUInt32(APTR.FromPointer(SettingsFileLength), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(SettingsFilePosition), 0, 0);
		return APTR.FromPointer(SettingsFileHandle);
	}
	public int Close(APTR handle) => handle.Raw == SettingsFileHandle ? 0 : -1;
	public int Read(APTR handle, APTR buffer, uint length)
	{
		if (handle.Raw != SettingsFileHandle || buffer.IsNull ||
			!IsMapped(buffer, length)) return -1;
		var position = APTR.ReadUInt32(APTR.FromPointer(SettingsFilePosition), 0);
		var fileLength = APTR.ReadUInt32(APTR.FromPointer(SettingsFileLength), 0);
		if (position >= fileLength) return 0;
		var count = length < fileLength - position ? length : fileLength - position;
		Copy(APTR.FromPointer(SettingsFileData + position), buffer, count);
		APTR.WriteUInt32(APTR.FromPointer(SettingsFilePosition), 0,
			position + count);
		return unchecked((int)count);
	}
	public int Write(APTR handle, APTR buffer, uint length)
	{
		if (handle.Raw != SettingsFileHandle || buffer.IsNull ||
			!IsMapped(buffer, length)) return -1;
		var position = APTR.ReadUInt32(APTR.FromPointer(SettingsFilePosition), 0);
		if (position > SettingsFileCapacity || length > SettingsFileCapacity - position)
			return -1;
		Copy(buffer, APTR.FromPointer(SettingsFileData + position), length);
		position += length;
		APTR.WriteUInt32(APTR.FromPointer(SettingsFilePosition), 0, position);
		var fileLength = APTR.ReadUInt32(APTR.FromPointer(SettingsFileLength), 0);
		if (position > fileLength)
			APTR.WriteUInt32(APTR.FromPointer(SettingsFileLength), 0, position);
		return unchecked((int)length);
	}
	public int IoErr() => 0;

	// Native-safe IFF capability. The qualification root uses a fixed guest
	// chunk buffer; no managed stream or exception path is reachable.
	public int ReadChunkBytes(APTR handle, APTR buffer, uint length)
	{
		if (handle.Raw != IffHandle || buffer.IsNull || !IsMapped(buffer, length))
			return -5;
		var position = APTR.ReadUInt32(APTR.FromPointer(IffPosition), 0);
		var fileLength = APTR.ReadUInt32(APTR.FromPointer(IffLength), 0);
		if (position >= fileLength) return 0;
		var count = length < fileLength - position ? length : fileLength - position;
		Copy(APTR.FromPointer(IffData + position), buffer, count);
		APTR.WriteUInt32(APTR.FromPointer(IffPosition), 0, position + count);
		return unchecked((int)count);
	}

	public int WriteChunkBytes(APTR handle, APTR buffer, uint length)
	{
		if (handle.Raw != IffHandle || buffer.IsNull || !IsMapped(buffer, length))
			return -6;
		var position = APTR.ReadUInt32(APTR.FromPointer(IffPosition), 0);
		if (position > IffCapacity || length > IffCapacity - position) return -6;
		Copy(buffer, APTR.FromPointer(IffData + position), length);
		position += length;
		APTR.WriteUInt32(APTR.FromPointer(IffPosition), 0, position);
		var fileLength = APTR.ReadUInt32(APTR.FromPointer(IffLength), 0);
		if (position > fileLength)
			APTR.WriteUInt32(APTR.FromPointer(IffLength), 0, position);
		return unchecked((int)length);
	}

	public int PushChunk(APTR handle, uint type, uint id, uint size)
	{
		if (handle.Raw != IffHandle) return -5;
		APTR.WriteUInt32(APTR.FromPointer(IffLength), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(IffPosition), 0, 0);
		return 0;
	}

	public int PopChunk(APTR handle) => handle.Raw == IffHandle ? 0 : -5;

	// ---- MG09 ASL/requester capability --------------------------------------
	public APTR AllocateRequest(uint requestType, APTR tags) => Allocate(16, 0);
	public int Request(APTR requester, APTR tags) => requester.IsNotNull ? 1 : 0;
	public void FreeRequest(APTR requester) { }
	public int Request(APTR application, APTR window, uint flags, APTR title,
		APTR gadgets, APTR format, APTR parameters) => 1;
	public int RequestObject(APTR application, APTR window, uint flags,
		APTR title, APTR gadgets, APTR obj, APTR format, APTR parameters) =>
		obj.IsNotNull ? 1 : 0;

	// ---- MG09 class-service capability --------------------------------------
	// Deterministic freestanding loader fixture for the external-class closure.
	// The only published class is Foo.mcc; no host filesystem or loader is
	// reached. The fixed pointers are opaque guest tokens owned by this fixture.
	public APTR OpenLibrary(APTR name, ushort minimumVersion)
	{
		if (name.IsNull || !IsMapped(name, 10)) return APTR.Null;
		if (ReadUInt8(name, 0) != (byte)'m' || ReadUInt8(name, 1) != (byte)'u' ||
			ReadUInt8(name, 2) != (byte)'i' || ReadUInt8(name, 3) != (byte)'/' ||
			ReadUInt8(name, 4) != (byte)'F' || ReadUInt8(name, 5) != (byte)'o' ||
			ReadUInt8(name, 6) != (byte)'o' || ReadUInt8(name, 7) != (byte)'.' ||
			ReadUInt8(name, 8) != (byte)'m' || ReadUInt8(name, 9) != (byte)'c')
			return APTR.Null;
		return APTR.FromPointer(0x00036500);
	}
	public void CloseLibrary(APTR library) { }
	public APTR MakeCustomClass(APTR superClass, ushort instanceSize,
		APTR dispatcher, APTR libraryBase)
	{
		if (dispatcher.IsNull) return APTR.Null;
		var result = Allocate(24, 0);
		if (result.IsNull) return result;
		WriteUInt32(result, 0, superClass.Raw);
		WriteUInt16(result, 4, instanceSize);
		WriteUInt32(result, 8, dispatcher.Raw);
		WriteUInt32(result, 12, libraryBase.Raw);
		return result;
	}
	public bool FreeCustomClass(APTR classPointer) => classPointer.IsNotNull;
	public APTR ResolvePublicClass(APTR classId)
	{
		if (classId.IsNull || !IsMapped(classId, 7)) return APTR.Null;
		if (ReadUInt8(classId, 0) != (byte)'F' ||
			ReadUInt8(classId, 1) != (byte)'o' ||
			ReadUInt8(classId, 2) != (byte)'o' ||
			ReadUInt8(classId, 3) != (byte)'.' ||
			ReadUInt8(classId, 4) != (byte)'m' ||
			ReadUInt8(classId, 5) != (byte)'c' ||
			ReadUInt8(classId, 6) != (byte)'c') return APTR.Null;
		return APTR.FromPointer(0x00036600);
	}

	// ---- MG09 drawing-service region capability -----------------------------
	// The freestanding qualification platform has no graphics.library, so the
	// region install/restore is a deterministic, bounded stub: install returns a
	// fixed synthetic "previous region" token and restore is a no-op.
	public APTR InstallClipRegion(APTR layer, APTR region) =>
		region.IsNull ? APTR.Null : APTR.FromPointer(0x00036F80);
	public void RestoreClipRegion(APTR layer, APTR previousRegion) { }

	// ---- MG09 drawing-service pen capability --------------------------------
	// ObtainPen returns a fixed full token whose low MUIPEN_MASK bits are the
	// physical pen (7) and whose high bits (0x0001) prove the service releases
	// the full token verbatim; a null spec fails. GetRGBColor writes deterministic
	// components.
	public int ObtainPen(APTR renderInfo, APTR penSpec, uint flags) =>
		penSpec.IsNull ? -1 : 0x00010007;
	public void ReleasePen(APTR renderInfo, int pen) { }
	public bool GetRGBColor(APTR renderInfo, APTR penSpec, APTR rgbColor)
	{
		if (penSpec.IsNull || rgbColor.IsNull) return false;
		WriteUInt32(rgbColor, 0, 0x11111111u);
		WriteUInt32(rgbColor, 4, 0x22222222u);
		WriteUInt32(rgbColor, 8, 0x33333333u);
		return true;
	}

	// ---- MG09 Process/Slave scheduler capability ----------------------------
	// The freestanding qualification platform has no exec scheduler, so this is
	// a bounded, allocation-free deterministic model: a launch with a plausible
	// (non-zero) stack succeeds and returns a fixed opaque token; a zero stack is
	// treated as a scheduler rejection so the failure-atomic Launch path is
	// reachable natively. Poll always reports the process still Running; kill and
	// signal are no-ops that report success. No host Task/thread is created.
	public uint ProcessLaunch(APTR name, int priority, uint stackSize,
		APTR sourceClass, APTR sourceObject) =>
		stackSize == 0 ? 0u : 0x00C0DE01u;
	public bool ProcessKill(uint taskToken) => taskToken != 0;
	public uint ProcessPoll(uint taskToken) =>
		taskToken == 0 ? MuiProcessSchedulerStatus.Unknown
			: MuiProcessSchedulerStatus.Running;
	public void ProcessSignal(uint taskToken, uint signalMask) { }
	public uint ProcessSignalsReceived(uint signalMask) => 0;

	// ---- MG09 external BOOPSI loader capability -----------------------------
	// Deterministic freestanding fixture: the only external class published is
	// "colorwheel.gadget" (exercising the -1 workaround path natively). No host
	// file or loader is reached; the returned pointer is an opaque guest token.
	public APTR OpenExternalClass(APTR classId)
	{
		if (classId.IsNull || !IsMapped(classId, 18)) return APTR.Null;
		if (ReadUInt8(classId, 0) != (byte)'c' ||
			ReadUInt8(classId, 1) != (byte)'o' ||
			ReadUInt8(classId, 2) != (byte)'l' ||
			ReadUInt8(classId, 3) != (byte)'o' ||
			ReadUInt8(classId, 4) != (byte)'r' ||
			ReadUInt8(classId, 5) != (byte)'w' ||
			ReadUInt8(classId, 6) != (byte)'h' ||
			ReadUInt8(classId, 7) != (byte)'e' ||
			ReadUInt8(classId, 8) != (byte)'e' ||
			ReadUInt8(classId, 9) != (byte)'l' ||
			ReadUInt8(classId, 10) != (byte)'.' ||
			ReadUInt8(classId, 11) != (byte)'g' ||
			ReadUInt8(classId, 12) != (byte)'a' ||
			ReadUInt8(classId, 13) != (byte)'d' ||
			ReadUInt8(classId, 14) != (byte)'g' ||
			ReadUInt8(classId, 15) != (byte)'e' ||
			ReadUInt8(classId, 16) != (byte)'t' ||
			ReadUInt8(classId, 17) != 0) return APTR.Null;
		return APTR.FromPointer(0x00036900);
	}
	public void CloseExternalClass(APTR classPointer) { }

	// ---- MG09 datatypes picture capability ----------------------------------
	// The freestanding platform has no datatypes.library, so this is a bounded,
	// allocation-free model: a non-null name acquires a fixed opaque picture
	// token, layout publishes a fixed natural size, draw is a no-op success.
	public APTR AcquirePicture(APTR name, APTR screen) =>
		name.IsNull ? APTR.Null : APTR.FromPointer(0x00036A00);
	public void ReleasePicture(APTR pictureObject) { }
	public bool LayoutPicture(APTR pictureObject, APTR rastPort,
		APTR dimensionStorage)
	{
		if (pictureObject.IsNull || dimensionStorage.IsNull ||
			!IsMapped(dimensionStorage, 8)) return false;
		WriteUInt32(dimensionStorage, 0, 32);
		WriteUInt32(dimensionStorage, 4, 24);
		return true;
	}
	public bool DrawPicture(APTR pictureObject, APTR rastPort, int left, int top,
		int width, int height) => pictureObject.IsNotNull;
}
