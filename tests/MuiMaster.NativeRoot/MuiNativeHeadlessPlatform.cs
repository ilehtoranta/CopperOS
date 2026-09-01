using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Amiga;
using Amiga.MUI;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.NativeRoot;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiNativeDispatchCapture
{
	public const uint Address = 0x0004F020;
	public const uint Size = 12;
	public APTR Class;
	public APTR Object;
	public uint Method;
}

public static class MuiNativeDispatchCaptureCodec
{
	public static bool Write(ref MuiNativeHeadlessPlatform platform,
		MuiNativeDispatchCapture value)
	{
		var address = APTR.FromPointer(MuiNativeDispatchCapture.Address);
		APTR.WriteUInt32(address, 0, value.Class.Raw);
		APTR.WriteUInt32(address, 4, value.Object.Raw);
		APTR.WriteUInt32(address, 8, value.Method);
		return true;
	}

	public static bool TryRead(ref MuiNativeHeadlessPlatform platform,
		out MuiNativeDispatchCapture value)
	{
		value = default;
		var address = APTR.FromPointer(MuiNativeDispatchCapture.Address);
		value.Class = APTR.FromPointer(APTR.ReadUInt32(address, 0));
		value.Object = APTR.FromPointer(APTR.ReadUInt32(address, 4));
		value.Method = APTR.ReadUInt32(address, 8);
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiNativeShortHelpProviderState
{
	public const uint Address = 0x0004F124;
	public const uint Size = 12;
	public uint Active;
	public uint Enabled;
	public APTR Object;
}

public static class MuiNativeShortHelpProviderStateCodec
{
	public static bool TryRead(ref MuiNativeHeadlessPlatform platform,
		out MuiNativeShortHelpProviderState value)
	{
		value = default;
		var address = APTR.FromPointer(MuiNativeShortHelpProviderState.Address);
		value.Active = APTR.ReadUInt32(address, 0);
		value.Enabled = APTR.ReadUInt32(address, 4);
		value.Object = APTR.FromPointer(APTR.ReadUInt32(address, 8));
		return true;
	}

	public static bool TryWrite(ref MuiNativeHeadlessPlatform platform,
		MuiNativeShortHelpProviderState value)
	{
		var address = APTR.FromPointer(MuiNativeShortHelpProviderState.Address);
		APTR.WriteUInt32(address, 0, value.Active);
		APTR.WriteUInt32(address, 4, value.Enabled);
		APTR.WriteUInt32(address, 8, value.Object.Raw);
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MuiNativeShortHelpTextRecord
{
	public const uint Address = 0x0004F120;
	public const uint Size = 2;
	public byte First;
	public byte Terminator;
}

public static class MuiNativeShortHelpTextRecordCodec
{
	public static bool TryRead(ref MuiNativeHeadlessPlatform platform,
		out MuiNativeShortHelpTextRecord value)
	{
		value = default;
		var address = APTR.FromPointer(MuiNativeShortHelpTextRecord.Address);
		value.First = APTR.ReadUInt8(address, 0);
		value.Terminator = APTR.ReadUInt8(address, 1);
		return true;
	}

	public static bool TryWrite(ref MuiNativeHeadlessPlatform platform,
		MuiNativeShortHelpTextRecord value)
	{
		var address = APTR.FromPointer(MuiNativeShortHelpTextRecord.Address);
		APTR.WriteUInt8(address, 0, value.First);
		APTR.WriteUInt8(address, 1, value.Terminator);
		return true;
	}
}

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
	private const uint WindowTabletMessages = 0x0004F030;
	private const uint WindowBorderScrollerBottom = 0x0004F034;
	private const uint WindowBorderScrollerLeft = 0x0004F038;
	private const uint WindowBorderScrollerRight = 0x0004F03C;
	private const uint WindowAlternateHeight = 0x0004F040;
	private const uint WindowAlternateWidth = 0x0004F044;
	private const uint WindowAlternateLeft = 0x0004F048;
	private const uint WindowAlternateTop = 0x0004F04C;
	private const uint WindowGeometryHeight = 0x0004F050;
	private const uint WindowGeometryWidth = 0x0004F054;
	private const uint WindowGeometryLeft = 0x0004F058;
	private const uint WindowGeometryTop = 0x0004F05C;
	private const uint WindowGadgetClose = 0x0004F060;
	private const uint WindowGadgetDepth = 0x0004F064;
	private const uint WindowGadgetDrag = 0x0004F068;
	private const uint WindowGadgetSize = 0x0004F06C;
	private const uint WindowGadgetSizeRight = 0x0004F070;
	private const uint WindowModeAppWindow = 0x0004F074;
	private const uint WindowModeBackdrop = 0x0004F078;
	private const uint WindowModeBorderless = 0x0004F07C;
	private const uint WindowModePanelWindow = 0x0004F080;
	private const uint WindowEventPending = 0x0004F084;
	private const uint WindowPointerPending = 0x0004F088;
	private const uint WindowMuiEventPending = 0x0004F090;
	private const uint WindowMuiEventInputMessage = 0x0004F094;
	private const uint WindowMuiEventKey = 0x0004F098;
	private const uint WindowMuiEventHandlerNode = 0x0004F09C;
	private const uint StringEditActions = 0x0004F0A0;
	private const uint StringEditReuseCount = 0x0004F0A4;
	private const uint CurrentStateAddress = 0x0004F0A8;
	private const uint ReuseWindowAddress = 0x0004F0B4;
	private const uint ReuseGateAddress = 0x0004F0B8;
	private const uint WindowDoubleClickPending = 0x0004F100;
	private const uint WindowDoubleClickObject = 0x0004F104;
	private const uint WindowDoubleClickValue = 0x0004F108;
	private const uint DoubleBufferTargetRastPort = 0x0004F0C0;
	private const uint DoubleBufferTargetRenderInfo = 0x0004F0CC;
	private const uint DoubleBufferBeginMarker = 0x0004F0C4;
	private const uint DoubleBufferEndMarker = 0x0004F0C8;
	private const uint BackfillKindMarker = 0x0004F0D0;
	private const uint BackfillOffsetMarker = 0x0004F0D4;
	private const uint BackfillBrightnessMarker = 0x0004F0D8;
	private const uint BackfillFlagsMarker = 0x0004F0DC;
	// Provider-owned opaque custom-font handles.  The payload carries only the
	// integer metrics required by the headless graphics seam; no managed font
	// object or guest-memory offset table is used.
	private const uint CustomFontHandleTag = 0x7E000000u;
	// Native ShortHelp provider fixture state.  The provider publishes one
	// bounded two-byte guest C string and keeps its ownership marker in a named
	// fixed-width state block.  The marker is disabled by default so existing
	// fallback qualification roots continue to observe caller-owned static help.

	public void Reset()
	{
		_slot = 0;
		APTR.WriteUInt32(APTR.FromPointer(Allocator), 0, ArenaStart);
		APTR.WriteUInt32(APTR.FromPointer(SettingsFileLength), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(SettingsFilePosition), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(IffLength), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(IffPosition), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowTabletMessages), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowBorderScrollerBottom), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowBorderScrollerLeft), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowBorderScrollerRight), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowAlternateHeight), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowAlternateWidth), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowAlternateLeft), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowAlternateTop), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowGeometryHeight), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowGeometryWidth), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowGeometryLeft), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowGeometryTop), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowGadgetClose), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowGadgetDepth), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowGadgetDrag), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowGadgetSize), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowGadgetSizeRight), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowModeAppWindow), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowModeBackdrop), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowModeBorderless), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowModePanelWindow), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowEventPending), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowPointerPending), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowMuiEventPending), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowMuiEventInputMessage), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowMuiEventKey), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowMuiEventHandlerNode), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(StringEditActions), 0,
			MuiStringEditWorkCodec.ActionUse);
		APTR.WriteUInt32(APTR.FromPointer(StringEditReuseCount), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(CurrentStateAddress), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(ReuseWindowAddress), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(ReuseGateAddress), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowDoubleClickPending), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowDoubleClickObject), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowDoubleClickValue), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(DoubleBufferBeginMarker), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(DoubleBufferEndMarker), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(BackfillKindMarker), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(BackfillOffsetMarker), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(BackfillBrightnessMarker), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(BackfillFlagsMarker), 0, 0);
		var shortHelpState = default(MuiNativeShortHelpProviderState);
		MuiNativeShortHelpProviderStateCodec.TryWrite(ref this, shortHelpState);
		MuiNativeShortHelpTextRecordCodec.TryWrite(ref this,
			default(MuiNativeShortHelpTextRecord));
	}

	public void SetState(APTR state) => APTR.WriteUInt32(
		APTR.FromPointer(CurrentStateAddress), 0, state.Raw);

	public void QueueWindowReuse(APTR window)
	{
		APTR.WriteUInt32(APTR.FromPointer(ReuseWindowAddress), 0, window.Raw);
		APTR.WriteUInt32(APTR.FromPointer(ReuseGateAddress), 0, 1);
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

	// The freestanding fixture models an Exec pool as an opaque arena token.
	// Pool headers are not interpreted by the MUI core; native Exec owns that
	// layout in the eventual target profile.
	public APTR CreatePool(uint requirements, uint puddleSize, uint threshold) =>
		Allocate(16, requirements);

	public void DeletePool(APTR pool)
	{
	}

	public APTR AllocPooled(APTR pool, uint byteSize) =>
		pool.IsNull ? APTR.Null : Allocate(byteSize, 0);

	public void FreePooled(APTR pool, APTR address, uint byteSize)
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

	public uint DoMethod(APTR obj, APTR message)
	{
		// A notification destination is an opaque guest object.  Keep the
		// freestanding fixture's callback seam bounded when malformed or stale
		// guest state names an unmapped destination; the production dispatcher
		// treats that callback as a no-op rather than dereferencing host memory.
		if (!IsMapped(obj, 1) || !IsMapped(message, 4)) return 0;
		var method = APTR.ReadUInt32(message, 0);
		var state = APTR.FromPointer(APTR.ReadUInt32(
			APTR.FromPointer(CurrentStateAddress), 0));
		if (state.IsNotNull && method == MuiCommonControlPacketCore.HandleEvent)
		{
			if (!MuiCommonControlPacketCore.TryReadHandleEvent(ref this, message,
				out var handleEvent)) return 0;
			if (APTR.ReadUInt32(APTR.FromPointer(ReuseGateAddress), 0) != 0)
			{
				var window = APTR.FromPointer(APTR.ReadUInt32(
					APTR.FromPointer(ReuseWindowAddress), 0));
				if (window.IsNotNull)
					MuiApplicationWindowCore.QueueWindowEventReuse(ref this, state,
						window, APTR.FromPointer(handleEvent.InputMessage));
				APTR.WriteUInt32(APTR.FromPointer(ReuseGateAddress), 0, 0);
			}
			return 1u;
		}
		MuiNativeDispatchCaptureCodec.Write(ref this,
			new MuiNativeDispatchCapture
			{
				Class = APTR.Null,
				Object = obj,
				Method = method,
			});
		// Notification callbacks are observational in this fixture. Return the
		// ordinary successful DoMethod result; the selector itself is retained in
		// the named capture record above for the native assertion.
		return 1u;
	}
	public uint CoerceMethod(APTR classPointer, APTR obj, APTR message)
	{
		// Native qualification breadcrumbs make the explicit MorphOS class path
		// observable without a managed callback or runtime service.
		var method = APTR.ReadUInt32(message, 0);
		var inputMessage = APTR.ReadUInt32(message, 4);
		MuiNativeDispatchCaptureCodec.Write(ref this,
			new MuiNativeDispatchCapture
			{
				Class = classPointer,
				Object = obj,
				Method = method,
			});
		// MUIM_HandleEvent carries the test method selector as InputMessage;
		// ordinary method packets keep their selector in the first word.
		return method == 0x90000077 ||
			(method == 0x80426D66 && inputMessage == 0x90000077) ?
			0x77u : 1u;
	}
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
		if (ReadUInt32(hook, 8) == 0x00CA0006u &&
			MuiStringEditWorkCodec.TryRead(ref this, objectAddress,
				out var stringEdit))
		{
			stringEdit.Actions = APTR.ReadUInt32(
				APTR.FromPointer(StringEditActions), 0);
			return MuiStringEditWorkCodec.Write(ref this, objectAddress,
				stringEdit) ? 1u : 0u;
		}
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
		if (ReadUInt32(hook, 8) == 0x00CA0003u)
		{
			var compare = 0;
			if (MuiListtreeCore.MuiListtreeNodeFieldCursorCodec.TryReadUInt32(
				ref this, objectAddress, MuiListtreeCore.MuiListtreeNodeField.Name,
				out var leftName) &&
				MuiListtreeCore.MuiListtreeNodeFieldCursorCodec.TryReadUInt32(
				ref this, messageAddress, MuiListtreeCore.MuiListtreeNodeField.Name,
				out var rightName) &&
				leftName != 0 && rightName != 0 &&
				IsMapped(APTR.FromPointer(leftName), 1) &&
				IsMapped(APTR.FromPointer(rightName), 1))
			{
				var leftFirst = ReadUInt8(APTR.FromPointer(leftName), 0);
				var rightFirst = ReadUInt8(APTR.FromPointer(rightName), 0);
				compare = leftFirst < rightFirst ? -1
					: leftFirst > rightFirst ? 1 : 0;
			}
			var compareData = APTR.FromPointer(ReadUInt32(hook, 16));
			if (compareData.IsNotNull && IsMapped(compareData, 12))
			{
				WriteUInt32(compareData, 0, hook.Raw);
				WriteUInt32(compareData, 4, objectAddress.Raw);
				WriteUInt32(compareData, 8, messageAddress.Raw);
			}
			return unchecked((uint)compare);
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
	public bool BeginMuiDoubleBuffer(ref MuiDoubleBufferRenderRequest request)
	{
		APTR.WriteUInt32(APTR.FromPointer(DoubleBufferBeginMarker), 0,
			request.Object.Raw);
		request.TargetRenderInfo = APTR.FromPointer(DoubleBufferTargetRenderInfo);
		request.TargetRastPort = APTR.FromPointer(DoubleBufferTargetRastPort);
		return request.Object.IsNotNull && request.SourceRastPort.IsNotNull;
	}
	public bool EndMuiDoubleBuffer(ref MuiDoubleBufferRenderRequest request,
		bool completed)
	{
		APTR.WriteUInt32(APTR.FromPointer(DoubleBufferEndMarker), 0,
			completed ? 1u : 0u);
		return request.Object.IsNotNull;
	}
	public bool ApplyMuiBackfill(ref MuiBackfillRenderRequest request)
	{
		APTR.WriteUInt32(APTR.FromPointer(BackfillKindMarker), 0, request.Kind);
		APTR.WriteUInt32(APTR.FromPointer(BackfillOffsetMarker), 0,
			unchecked((uint)request.XOffset));
		APTR.WriteUInt32(APTR.FromPointer(BackfillBrightnessMarker), 0,
			unchecked((uint)request.Brightness));
		APTR.WriteUInt32(APTR.FromPointer(BackfillFlagsMarker), 0, request.Flags);
		return request.CustomBackfill != 0;
	}
	public APTR PushClip(APTR layer, int left, int top, int width, int height) =>
		APTR.FromPointer(1);

	public int TranslateTextInput(APTR intuiMessage)
	{
		return MuiIntuiMessageCodec.TryReadRawKey(ref this, intuiMessage,
			out var message) ? message.Code : -1;
	}
	public bool DisplayMuiBeep(ref MuiStringEditBeepRequest request)
	{
		request.Accepted = 1;
		return true;
	}
	public bool ReuseMuiInput(ref MuiStringEditReuseRequest request)
	{
		// This fixture has no native event queue. Returning a declined, typed
		// request exercises the core's bounded guest-resident fallback path.
		var count = APTR.ReadUInt32(APTR.FromPointer(StringEditReuseCount), 0);
		APTR.WriteUInt32(APTR.FromPointer(StringEditReuseCount), 0, count + 1);
		if (count == 0)
			QueueStringEditActions(MuiStringEditWorkCodec.ActionUse);
		request.Accepted = 0;
		return false;
	}
	public bool ReadMuiKeyadjustTextInput(ref MuiKeyadjustTextInputSample input)
	{
		if (!MuiIntuiMessageCodec.TryReadRawKey(ref this, input.IntuiMessage,
			out var message)) return false;
		input.TextCode = message.Code;
		input.Available = 1;
		return true;
	}
	public bool ReadMuiKeyadjustInput(ref MuiKeyadjustInputSample input) => false;
	public bool CreateMuiShortHelp(ref MuiShortHelpCreateSample sample)
	{
		MuiNativeShortHelpProviderStateCodec.TryRead(ref this, out var state);
		if (state.Enabled == 0 || sample.Object.IsNull || state.Active != 0)
			return false;
		var text = APTR.FromPointer(MuiNativeShortHelpTextRecord.Address);
		var record = new MuiNativeShortHelpTextRecord
		{
			First = (byte)'?',
			Terminator = 0,
		};
		MuiNativeShortHelpTextRecordCodec.TryWrite(ref this, record);
		state.Active = 1;
		state.Object = sample.Object;
		MuiNativeShortHelpProviderStateCodec.TryWrite(ref this, state);
		sample.Result = text;
		return true;
	}
	public bool CheckMuiShortHelp(ref MuiShortHelpCheckSample sample) => false;
	public bool DeleteMuiShortHelp(ref MuiShortHelpDeleteSample sample)
	{
		MuiNativeShortHelpProviderStateCodec.TryRead(ref this, out var state);
		var text = APTR.FromPointer(MuiNativeShortHelpTextRecord.Address);
		if (state.Enabled == 0 || sample.Object.IsNull ||
			sample.Object.Raw != state.Object.Raw || sample.Help.Raw != text.Raw ||
			state.Active == 0)
			return false;
		MuiNativeShortHelpTextRecordCodec.TryWrite(ref this,
			default(MuiNativeShortHelpTextRecord));
		state.Active = 0;
		state.Object = APTR.Null;
		MuiNativeShortHelpProviderStateCodec.TryWrite(ref this, state);
		return true;
	}
	public bool CreateMuiBubble(ref MuiBubbleCreateSample sample)
	{
		if (sample.Object.IsNull || sample.Text.IsNull) return false;
		sample.Result = APTR.FromPointer(0x7E000120u);
		return true;
	}
	public bool DeleteMuiBubble(ref MuiBubbleDeleteSample sample) =>
		sample.Object.IsNotNull && sample.Bubble.Raw == 0x7E000120u;
	public bool AddMuiContextMenu(ref MuiContextMenuAddSample sample)
	{
		if (sample.Object.IsNull) return false;
		sample.Result = 1u;
		return true;
	}
	public bool HandleMuiContextMenuChoice(ref MuiContextMenuChoiceSample sample) =>
		false;
	public bool CreateMuiDragImage(ref MuiDragImageCreateSample sample) => false;
	public bool DeleteMuiDragImage(ref MuiDragImageDeleteSample sample) => false;
	public bool CaptureMuiPointer(ref MuiPointerCaptureSample sample) => false;
	public bool ReleaseMuiPointer(ref MuiPointerCaptureSample sample) => false;
	public bool RouteMuiDrag(ref MuiDragRouteSample sample) => false;
	public void PopClip(APTR layer, APTR previousClip) { }
	public int TextWidth(APTR rastPort, APTR font, APTR text, int length)
	{
		if (length < 0) return 0;
		if (!TryGetMuiCustomFontMetrics(font, out var metrics)) return length * 8;
		return length > int.MaxValue / metrics.GlyphWidth ? int.MaxValue :
			length * metrics.GlyphWidth;
	}
	public int TextHeight(APTR rastPort, APTR font)
	{
		return TryGetMuiCustomFontMetrics(font, out var metrics) ? metrics.Height : 8;
	}
	public APTR OpenMuiCustomFont(ref MuiCustomFontOpenRequest request)
	{
		var metrics = MuiCustomFontMetricsCore.FromSpec(request.Spec);
		var payload = (unchecked((uint)metrics.Height) << 8) |
			unchecked((uint)metrics.GlyphWidth);
		request.Result = APTR.FromPointer(CustomFontHandleTag | payload);
		return request.Result;
	}
	public bool CloseMuiCustomFont(APTR font) => font.IsNotNull;
	public bool TryGetMuiCustomFontMetrics(APTR font,
		out MuiCustomFontMetrics metrics)
	{
		metrics = default;
		if (font.IsNull || (font.Raw & 0xFF000000u) != CustomFontHandleTag)
			return false;
		var payload = font.Raw & 0x00FFFFFFu;
		var height = unchecked((int)(payload >> 8));
		var glyphWidth = unchecked((int)(payload & 0xFFu));
		if (height < 1 || glyphWidth < 1) return false;
		metrics.Height = height;
		metrics.GlyphWidth = glyphWidth;
		return true;
	}
	public void SetPen(APTR rastPort, uint pen) { }
	public bool ResolveMuiTextColor(ref MuiTextColorResolutionRequest request)
	{
		request.Color = request.CustomFontAvailable != 0 &&
			(request.CustomFontSpec.ValueFlags & MuiCustomFontSpecFlags.HasTextColor) != 0
			? request.CustomFontSpec.TextColor & 0x00FFFFFFu
			: 0x00FFFFFFu;
		request.Available = 1;
		return request.Object.IsNotNull && request.RenderInfo.IsNotNull;
	}
	public bool ApplyMuiTextColor(ref MuiTextColorRenderRequest request) =>
		request.RastPort.IsNotNull;
	public bool ApplyMuiCustomFont(ref MuiCustomFontRenderRequest request) =>
		request.RastPort.IsNotNull && request.Font.IsNotNull;
	public bool ApplyMuiTextStyle(ref MuiTextStyleRenderRequest request) =>
		request.RastPort.IsNotNull && request.Present != 0;
	public bool ApplyMuiTextInlineColor(ref MuiTextInlineColorRenderRequest request) =>
		request.RastPort.IsNotNull && request.Present != 0;
	public bool ApplyMuiTextInlineImage(ref MuiTextInlineImageRenderRequest request) =>
		request.RastPort.IsNotNull && request.Present != 0;
	public bool ApplyMuiTextMethod(ref MuiTextMethodRenderRequest request) =>
		request.RastPort.IsNotNull && request.Present != 0;
	public bool ApplyMuiTextDimensions(ref MuiTextDimensionRequest request)
	{
		if (request.RastPort.IsNull || request.Present == 0) return false;
		request.Width = request.Length < 0 ? 0 : request.Length * 8;
		request.Height = 8;
		return true;
	}
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
	public bool ReadWindowEvent(ref MuiWindowEventSample sample)
	{
		var eventClass = APTR.ReadUInt32(APTR.FromPointer(WindowEventPending), 0);
		if (eventClass == 0 || sample.NativeWindow.IsNull ||
			sample.InputEvent.IsNull) return false;
		APTR.WriteUInt32(APTR.FromPointer(WindowEventPending), 0, 0);
		var input = default(InputEvent);
		input.Class = InputEventClass.Event;
		input.Code = unchecked((ushort)eventClass);
		input.TimeStamp.Seconds = 1;
		input.TimeStamp.Microseconds = eventClass;
		if (!MuiWindowInputEventCodec.Write(ref this, sample.InputEvent, input))
			return false;
		sample.EventClass = eventClass;
		sample.TimerDelayElapsed = eventClass == 0x00400000 ? 1u : 0u;
		sample.DoubleClick.Available = APTR.ReadUInt32(
			APTR.FromPointer(WindowDoubleClickPending), 0);
		sample.DoubleClick.Object = APTR.FromPointer(APTR.ReadUInt32(
			APTR.FromPointer(WindowDoubleClickObject), 0));
		sample.DoubleClick.Value = unchecked((int)APTR.ReadUInt32(
			APTR.FromPointer(WindowDoubleClickValue), 0));
		APTR.WriteUInt32(APTR.FromPointer(WindowDoubleClickPending), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowDoubleClickObject), 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(WindowDoubleClickValue), 0, 0);
		return true;
	}
	public void QueueWindowEvent(uint eventClass) =>
		APTR.WriteUInt32(APTR.FromPointer(WindowEventPending), 0, eventClass);
	public void QueueWindowDoubleClick(APTR obj, int value)
	{
		APTR.WriteUInt32(APTR.FromPointer(WindowDoubleClickObject), 0,
			obj.Raw);
		APTR.WriteUInt32(APTR.FromPointer(WindowDoubleClickValue), 0,
			unchecked((uint)value));
		APTR.WriteUInt32(APTR.FromPointer(WindowDoubleClickPending), 0, 1);
	}
	public bool ReadMuiWindowPointer(APTR nativeWindow,
		ref MuiWindowPointerInput input)
	{
		var mouseObject = APTR.ReadUInt32(
			APTR.FromPointer(WindowPointerPending), 0);
		if (mouseObject == 0 || nativeWindow.IsNull || input.Window.IsNull ||
			input.InputEvent.IsNull) return false;
		APTR.WriteUInt32(APTR.FromPointer(WindowPointerPending), 0, 0);
		input.MouseObject = APTR.FromPointer(mouseObject);
		return true;
	}
	public void QueueWindowPointer(APTR mouseObject) =>
		APTR.WriteUInt32(APTR.FromPointer(WindowPointerPending), 0,
			mouseObject.Raw);
	public void QueueWindowMuiEvent(APTR inputMessage, int muiKey,
		APTR eventHandlerNode)
	{
		APTR.WriteUInt32(APTR.FromPointer(WindowMuiEventInputMessage), 0,
			inputMessage.Raw);
		APTR.WriteUInt32(APTR.FromPointer(WindowMuiEventKey), 0,
			unchecked((uint)muiKey));
		APTR.WriteUInt32(APTR.FromPointer(WindowMuiEventHandlerNode), 0,
			eventHandlerNode.Raw);
		APTR.WriteUInt32(APTR.FromPointer(WindowMuiEventPending), 0, 1);
	}
	public void QueueStringEditActions(uint actions) =>
		APTR.WriteUInt32(APTR.FromPointer(StringEditActions), 0, actions);
	public bool ReadMuiWindowEvent(ref MuiWindowEventInput input)
	{
		if (APTR.ReadUInt32(APTR.FromPointer(WindowMuiEventPending), 0) == 0 ||
			input.Window.IsNull || input.NativeWindow.IsNull ||
			input.InputEvent.IsNull || input.Message.IsNull ||
			input.EventClass == 0) return false;
		var inputMessage = APTR.ReadUInt32(
			APTR.FromPointer(WindowMuiEventInputMessage), 0);
		var muiKey = unchecked((int)APTR.ReadUInt32(
			APTR.FromPointer(WindowMuiEventKey), 0));
		var eventHandlerNode = APTR.ReadUInt32(
			APTR.FromPointer(WindowMuiEventHandlerNode), 0);
		if (!MuiCommonControlPacketCore.WriteHandleEvent(ref this, input.Message,
			inputMessage, muiKey, eventHandlerNode)) return false;
		APTR.WriteUInt32(APTR.FromPointer(WindowMuiEventPending), 0, 0);
		return true;
	}
	public bool ActivateMuiWindow(APTR nativeWindow) => nativeWindow.IsNotNull;
	public bool SetMuiWindowBusy(APTR nativeWindow, bool busy) =>
		nativeWindow.IsNotNull;
	public bool SetMuiWindowTabletMessages(APTR nativeWindow, bool enabled)
	{
		if (nativeWindow.IsNull) return false;
		APTR.WriteUInt32(APTR.FromPointer(WindowTabletMessages), 0,
			enabled ? 1u : 0u);
		return true;
	}
	public bool SetMuiWindowBorderScrollers(APTR nativeWindow, bool useBottom,
		bool useLeft, bool useRight)
	{
		if (nativeWindow.IsNull) return false;
		APTR.WriteUInt32(APTR.FromPointer(WindowBorderScrollerBottom), 0,
			useBottom ? 1u : 0u);
		APTR.WriteUInt32(APTR.FromPointer(WindowBorderScrollerLeft), 0,
			useLeft ? 1u : 0u);
		APTR.WriteUInt32(APTR.FromPointer(WindowBorderScrollerRight), 0,
			useRight ? 1u : 0u);
		return true;
	}
	public bool ConfigureMuiWindowAlternateGeometry(APTR nativeWindow,
		MuiWindowPublicCore.MuiWindowAlternateGeometry geometry)
	{
		if (nativeWindow.IsNull) return false;
		APTR.WriteUInt32(APTR.FromPointer(WindowAlternateHeight), 0,
			unchecked((uint)geometry.Height));
		APTR.WriteUInt32(APTR.FromPointer(WindowAlternateWidth), 0,
			unchecked((uint)geometry.Width));
		APTR.WriteUInt32(APTR.FromPointer(WindowAlternateLeft), 0,
			unchecked((uint)geometry.LeftEdge));
		APTR.WriteUInt32(APTR.FromPointer(WindowAlternateTop), 0,
			unchecked((uint)geometry.TopEdge));
		return true;
	}
	public bool ConfigureMuiWindowGeometry(APTR nativeWindow,
		MuiWindowPublicCore.MuiWindowGeometry geometry)
	{
		if (nativeWindow.IsNull) return false;
		APTR.WriteUInt32(APTR.FromPointer(WindowGeometryHeight), 0,
			unchecked((uint)geometry.Height));
		APTR.WriteUInt32(APTR.FromPointer(WindowGeometryWidth), 0,
			unchecked((uint)geometry.Width));
		APTR.WriteUInt32(APTR.FromPointer(WindowGeometryLeft), 0,
			unchecked((uint)geometry.LeftEdge));
		APTR.WriteUInt32(APTR.FromPointer(WindowGeometryTop), 0,
			unchecked((uint)geometry.TopEdge));
		return true;
	}
	public bool ConfigureMuiWindowGadgets(APTR nativeWindow,
		MuiWindowPublicCore.MuiWindowGadgetPolicy policy)
	{
		if (nativeWindow.IsNull) return false;
		APTR.WriteUInt32(APTR.FromPointer(WindowGadgetClose), 0,
			policy.CloseGadget);
		APTR.WriteUInt32(APTR.FromPointer(WindowGadgetDepth), 0,
			policy.DepthGadget);
		APTR.WriteUInt32(APTR.FromPointer(WindowGadgetDrag), 0,
			policy.DragBar);
		APTR.WriteUInt32(APTR.FromPointer(WindowGadgetSize), 0,
			policy.SizeGadget);
		APTR.WriteUInt32(APTR.FromPointer(WindowGadgetSizeRight), 0,
			policy.SizeRight);
		return true;
	}
	public bool ConfigureMuiWindowMode(APTR nativeWindow,
		MuiWindowPublicCore.MuiWindowModePolicy policy)
	{
		if (nativeWindow.IsNull) return false;
		APTR.WriteUInt32(APTR.FromPointer(WindowModeAppWindow), 0,
			policy.AppWindow);
		APTR.WriteUInt32(APTR.FromPointer(WindowModeBackdrop), 0,
			policy.Backdrop);
		APTR.WriteUInt32(APTR.FromPointer(WindowModeBorderless), 0,
			policy.Borderless);
		APTR.WriteUInt32(APTR.FromPointer(WindowModePanelWindow), 0,
			policy.PanelWindow);
		return true;
	}
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
