/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS MUIP_InitResize carries { MethodID, flags }; MUIP_ExitResize is a
// method-only packet. These records keep the guest ABI named at the dispatcher
// boundary while the small codec below owns the unavoidable packed accesses.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaInitResizeMessage
{
	internal const uint Size = 8;
	internal uint MethodId;
	internal uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaExitResizeMessage
{
	internal const uint Size = 4;
	internal uint MethodId;
}

internal enum MuiAreaResizeMessageField : byte
{
	MethodId,
	Flags,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaResizeMessageFieldCursor
{
	internal APTR Message;
	internal MuiAreaResizeMessageField Field;
}

internal static class MuiAreaResizeMessageFieldCursorCodec
{
	private static bool TryResolve(MuiAreaResizeMessageField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaResizeMessageField.MethodId:
				offset = 0;
				return true;
			case MuiAreaResizeMessageField.Flags:
				offset = 4;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaResizeMessageFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Message.IsNull ||
			cursor.Message.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaResizeMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaResizeMessageFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaResizeMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaResizeMessageFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaResizeMessageCodec
{
	internal const uint InitResize = 0x804292BDu;
	internal const uint ExitResize = 0x80428431u;

	internal static bool IsMethod(uint method) => method == InitResize ||
		method == ExitResize;

	internal static bool TryReadInit<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaInitResizeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiAreaInitResizeMessage.Size) ||
			!MuiAreaResizeMessageFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiAreaResizeMessageField.MethodId, out var method) ||
			method != InitResize ||
			!MuiAreaResizeMessageFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiAreaResizeMessageField.Flags, out var flags)) return false;
		packet.MethodId = method;
		packet.Flags = flags;
		return true;
	}

	internal static bool TryReadExit<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaExitResizeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiAreaExitResizeMessage.Size) ||
			!MuiAreaResizeMessageFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiAreaResizeMessageField.MethodId, out var method) ||
			method != ExitResize) return false;
		packet.MethodId = method;
		return true;
	}

	internal static bool WriteInit<TPlatform>(ref TPlatform platform,
		APTR message, uint flags) where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiAreaInitResizeMessage.Size) ||
			!MuiAreaResizeMessageFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiAreaResizeMessageField.MethodId, InitResize) ||
			!MuiAreaResizeMessageFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiAreaResizeMessageField.Flags, flags)) return false;
		return true;
	}

	internal static bool WriteExit<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiAreaExitResizeMessage.Size) ||
			!MuiAreaResizeMessageFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiAreaResizeMessageField.MethodId, ExitResize)) return false;
		return true;
	}
}

// The MorphOS Area initResize/exitResize pair is a lifecycle notification
// around a resize pass. Keep the flags and active transition in one named,
// guest-resident record so custom classes can observe the same state without
// relying on managed fields or private object-layout offsets.
public struct MuiAreaResizeStateInput
{
	public uint Active;
	public uint Flags;
	public uint Generation;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaResizeStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x4152535Au; // "ARSZ"

	internal uint Magic;
	internal uint Active;
	internal uint Flags;
	internal uint Generation;
}

internal enum MuiAreaResizeStateField : byte
{
	Magic,
	Active,
	Flags,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaResizeStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaResizeStateField Field;
}

internal static class MuiAreaResizeStateFieldCursorCodec
{
	private static bool TryResolve(MuiAreaResizeStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaResizeStateField.Magic:
				offset = 0;
				return true;
			case MuiAreaResizeStateField.Active:
				offset = 4;
				return true;
			case MuiAreaResizeStateField.Flags:
				offset = 8;
				return true;
			case MuiAreaResizeStateField.Generation:
				offset = 12;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaResizeStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiAreaResizeStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaResizeStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaResizeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaResizeStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaResizeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaResizeStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaResizeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaResizeStateRecord.Size) ||
			!MuiAreaResizeStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaResizeStateField.Magic, out var magic) ||
			magic != MuiAreaResizeStateRecord.Cookie ||
			!MuiAreaResizeStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaResizeStateField.Active, out var active) ||
			!MuiAreaResizeStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaResizeStateField.Flags, out var flags) ||
			!MuiAreaResizeStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaResizeStateField.Generation, out var generation))
			return false;
		value.Magic = magic;
		value.Active = active == 0 ? 0u : 1u;
		value.Flags = flags;
		value.Generation = generation;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaResizeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaResizeStateRecord.Size) || value.Magic !=
			MuiAreaResizeStateRecord.Cookie) return false;
		return MuiAreaResizeStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaResizeStateField.Magic, value.Magic) &&
			MuiAreaResizeStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaResizeStateField.Active, value.Active == 0 ? 0u : 1u) &&
			MuiAreaResizeStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaResizeStateField.Flags, value.Flags) &&
			MuiAreaResizeStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaResizeStateField.Generation, value.Generation);
	}
}

public static class MuiAreaResizeCore
{
	internal const uint StateKey = 0x7F070067u;
	internal const uint InitResize = MuiAreaResizeMessageCodec.InitResize;
	internal const uint ExitResize = MuiAreaResizeMessageCodec.ExitResize;

	public static bool IsResizeMethod(uint method) => method == InitResize ||
		method == ExitResize;

	// Pure transition helpers keep the native qualification boundary small and
	// make the lifecycle policy explicit without constructing managed objects.
	public static bool BuildInitState(uint flags, uint previousGeneration,
		out MuiAreaResizeStateInput value)
	{
		value = default;
		value.Active = 1;
		value.Flags = flags;
		value.Generation = previousGeneration == uint.MaxValue ? 1u :
			previousGeneration + 1u;
		return true;
	}

	public static bool BuildExitState(MuiAreaResizeStateInput current,
		out MuiAreaResizeStateInput value)
	{
		value = current;
		if (current.Active == 0) return false;
		value.Active = 0;
		return true;
	}

	internal static bool Init<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint flags) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var previous = default(MuiAreaResizeStateRecord);
		if (!TryGetState(ref platform, state, obj, out previous))
			previous = default;
		if (!BuildInitState(flags, previous.Generation, out var input))
			return false;
		return WriteState(ref platform, state, obj, input);
	}

	internal static bool Exit<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryGetState(ref platform, state, obj, out var previous))
			return false;
		var current = default(MuiAreaResizeStateInput);
		current.Active = previous.Active;
		current.Flags = previous.Flags;
		current.Generation = previous.Generation;
		if (!BuildExitState(current, out var input)) return false;
		return WriteState(ref platform, state, obj, input);
	}

	internal static bool Cleanup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (obj.IsNull) return true;
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			StateKey);
		if (length == 0) return true;
		return MuiStoreCore.DataspaceRemove(ref platform, state, obj, StateKey);
	}

	public static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiAreaResizeStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (!TryGetState(ref platform, state, obj, out var record)) return false;
		value.Active = record.Active;
		value.Flags = record.Flags;
		value.Generation = record.Generation;
		return true;
	}

	internal static bool TryGetState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaResizeStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var data = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StateKey);
		if (data.IsNull || MuiStoreCore.DataspaceLength(ref platform, state, obj,
			StateKey) != (int)MuiAreaResizeStateRecord.Size)
			return false;
		return MuiAreaResizeStateRecordCodec.TryRead(ref platform, data, out value);
	}

	private static bool WriteState<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, MuiAreaResizeStateInput input)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaResizeStateRecord.Size);
		if (scratch.IsNull) return false;
		var record = default(MuiAreaResizeStateRecord);
		record.Magic = MuiAreaResizeStateRecord.Cookie;
		record.Active = input.Active;
		record.Flags = input.Flags;
		record.Generation = input.Generation;
		var written = MuiAreaResizeStateRecordCodec.Write(ref platform, scratch,
			record);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateKey, scratch, (int)MuiAreaResizeStateRecord.Size);
		platform.Clear(scratch, MuiAreaResizeStateRecord.Size);
		platform.Free(scratch, MuiAreaResizeStateRecord.Size);
		return stored;
	}
}

public static class MuiAreaResizePacketCore
{
	public static bool Init<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint flags) where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaResizeCore.Init(ref platform, state, obj, flags);

	public static bool Exit<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaResizeCore.Exit(ref platform, state, obj);

	public static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiAreaResizeStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaResizeCore.TryGet(ref platform, state, obj, out value);
}
