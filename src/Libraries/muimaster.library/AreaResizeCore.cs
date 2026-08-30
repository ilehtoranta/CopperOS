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
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint FlagsOffset = 4;
	internal uint MethodId;
	internal uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaExitResizeMessage
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
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

// The fixed initResize/exitResize records own their packed positions in this
// bounded adapter. Live packet consumers use it directly; the typed cursor
// below remains only for compatibility callers.
internal static class MuiAreaResizeMessageMemoryCodec
{
	private static bool TryResolve(MuiAreaResizeMessageField field,
		out uint offset, out uint size)
	{
		switch (field)
		{
			case MuiAreaResizeMessageField.MethodId:
				offset = MuiAreaExitResizeMessage.MethodIdOffset;
				size = MuiAreaExitResizeMessage.Size;
				return true;
			case MuiAreaResizeMessageField.Flags:
				offset = MuiAreaInitResizeMessage.FlagsOffset;
				size = MuiAreaInitResizeMessage.Size;
				return true;
			default:
				offset = 0;
				size = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaResizeMessageField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset, out var size) || message.IsNull ||
			message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, size))
			return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiAreaInitResizeMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaResizeMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaResizeMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaResizeMessageFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaResizeMessageFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaResizeMessageMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaResizeMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaResizeMessageMemoryCodec.TryReadUInt32(ref platform, message,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaResizeMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaResizeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			field, value);
}

internal static class MuiAreaInitResizeMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaInitResizeMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaInitResizeMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Flags) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaInitResizeMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaInitResizeMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Flags)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiAreaExitResizeMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaExitResizeMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaExitResizeMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.MethodId = methodId;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaExitResizeMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaExitResizeMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
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
		if (!MuiAreaInitResizeMessageCodec.TryRead(ref platform, message,
			out packet) || packet.MethodId != InitResize)
		{
			packet = default;
			return false;
		}
		return true;
	}

	internal static bool TryReadExit<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaExitResizeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaExitResizeMessageCodec.TryRead(ref platform, message,
			out packet) || packet.MethodId != ExitResize)
		{
			packet = default;
			return false;
		}
		return true;
	}

	internal static bool WriteInit<TPlatform>(ref TPlatform platform,
		APTR message, uint flags) where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaInitResizeMessage);
		packet.MethodId = InitResize;
		packet.Flags = flags;
		return MuiAreaInitResizeMessageCodec.Write(ref platform, message, packet);
	}

	internal static bool WriteExit<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory
	{
		// Keep this one-field packet on the sequential cursor path. The
		// compiler's by-value ABI for a single-LONG managed struct is not a
		// guest pointer, so passing the record through a helper would lose the
		// record address in freestanding native code.
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiAreaExitResizeMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				ExitResize)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadExitMethod<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiAreaExitResizeMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out methodId) || !MuiGuestStructCursor.IsComplete(cursor) ||
			methodId != ExitResize)
		{
			methodId = 0;
			return false;
		}
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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ActiveOffset = 4;
	internal const uint FlagsOffset = 8;
	internal const uint GenerationOffset = 12;
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

// The fixed resize lifecycle record owns its packed positions in this bounded
// adapter. Live lifecycle code uses it directly; the typed cursor remains only
// for compatibility callers and adapter-focused tests.
internal static class MuiAreaResizeStateMemoryCodec
{
	private static bool TryResolve(MuiAreaResizeStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaResizeStateField.Magic:
				offset = MuiAreaResizeStateRecord.MagicOffset;
				return true;
			case MuiAreaResizeStateField.Active:
				offset = MuiAreaResizeStateRecord.ActiveOffset;
				return true;
			case MuiAreaResizeStateField.Flags:
				offset = MuiAreaResizeStateRecord.FlagsOffset;
				return true;
			case MuiAreaResizeStateField.Generation:
				offset = MuiAreaResizeStateRecord.GenerationOffset;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaResizeStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, MuiAreaResizeStateRecord.Size))
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiAreaResizeStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaResizeStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaResizeStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaResizeStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaResizeStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaResizeStateMemoryCodec.TryGetAddress(ref platform, cursor.Record,
			cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaResizeStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaResizeStateMemoryCodec.TryReadUInt32(ref platform, record, field,
			out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaResizeStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaResizeStateMemoryCodec.TryWriteUInt32(ref platform, record, field,
			value);
}

internal static class MuiAreaResizeStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaResizeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaResizeStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var magic) || magic != MuiAreaResizeStateRecord.Cookie ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var active) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Flags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Generation) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Magic = magic;
		value.Active = active == 0 ? 0u : 1u;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaResizeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (value.Magic != MuiAreaResizeStateRecord.Cookie ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiAreaResizeStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Active == 0 ? 0u : 1u) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Flags) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Generation)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
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
