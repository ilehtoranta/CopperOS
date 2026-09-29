/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
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
// bounded adapter. The typed cursor is the canonical field-address path;
// scalar overloads remain as compatibility adapters for existing callers.
internal static class MuiAreaResizeMessageMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiAreaResizeMessageField field,
		out uint index, out uint recordSize)
	{
		if (field == MuiAreaResizeMessageField.MethodId)
		{
			index = 0;
			recordSize = MuiAreaExitResizeMessage.Size;
			return true;
		}
		if (field == MuiAreaResizeMessageField.Flags)
		{
			index = 1;
			recordSize = MuiAreaInitResizeMessage.Size;
			return true;
		}
		index = uint.MaxValue;
		recordSize = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaResizeMessageField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaResizeMessageFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaResizeMessageFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index,
			out var recordSize) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Message, recordSize,
				out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiAreaInitResizeMessage.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiAreaInitResizeMessage.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaResizeMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (field == MuiAreaResizeMessageField.MethodId)
			return MuiAreaExitResizeMethodHeaderCodec.TryReadValue(ref platform,
				message, out value);
		if (field != MuiAreaResizeMessageField.Flags ||
			!MuiAreaInitResizeMessageCodec.TryReadStructural(ref platform,
				message, out var init)) return false;
		value = init.Flags;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaResizeMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (field == MuiAreaResizeMessageField.MethodId)
			return MuiAreaExitResizeMethodHeaderCodec.WriteValue(ref platform,
				message, value);
		if (field != MuiAreaResizeMessageField.Flags ||
			!MuiAreaInitResizeMessageCodec.TryReadStructural(ref platform,
				message, out var init)) return false;
		init.Flags = value;
		return MuiAreaInitResizeMessageCodec.WriteStructural(ref platform,
			message, init);
	}
}

internal static class MuiAreaResizeMessageFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaResizeMessageFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaResizeMessageFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaResizeMessageMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out fieldSize);

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

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaInitResizeMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryRead(ref platform, address, out value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaInitResizeMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		Write(ref platform, address, value);
}

internal static class MuiAreaExitResizeMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaExitResizeMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return TryReadStructural(ref platform, address, out value);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaExitResizeMessage value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteStructural(ref platform, address, value);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaExitResizeMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiAreaExitResizeMethodHeaderCodec.TryReadValue(ref platform,
			address, out var methodId)) return false;
		value.MethodId = methodId;
		return true;
	}

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaExitResizeMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaExitResizeMethodHeaderCodec.WriteValue(ref platform, address,
			value.MethodId);
}

// Struct-first codec for the method-only exitResize header. Scalar-safe entry
// points keep the packed one-ULONG record address explicit for freestanding
// native lowering while reusing the shared guest storage representation.
internal static class MuiAreaExitResizeMethodHeaderCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaExitResizeMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.TryReadValue(ref platform, valueAddress,
				out methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WriteValue<TPlatform>(ref TPlatform platform,
		APTR address, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaExitResizeMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
				methodId)) return false;
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
		=> MuiAreaExitResizeMethodHeaderCodec.WriteValue(ref platform, message,
			ExitResize);

	internal static bool TryReadExitMethod<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaExitResizeMethodHeaderCodec.TryReadValue(ref platform,
			message, out methodId) || methodId != ExitResize)
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
// adapter. The typed cursor is the canonical field-address path; scalar
// overloads remain compatibility adapters for existing callers.
internal static class MuiAreaResizeStateMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiAreaResizeStateField field,
		out uint index)
	{
		if (field == MuiAreaResizeStateField.Magic) index = 0;
		else if (field == MuiAreaResizeStateField.Active) index = 1;
		else if (field == MuiAreaResizeStateField.Flags) index = 2;
		else if (field == MuiAreaResizeStateField.Generation) index = 3;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaResizeStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaResizeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaResizeStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiAreaResizeStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiAreaResizeStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiAreaResizeStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaResizeStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaResizeStateRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiAreaResizeStateField.Magic)
			value = state.Magic;
		else if (field == MuiAreaResizeStateField.Active)
			value = state.Active;
		else if (field == MuiAreaResizeStateField.Flags)
			value = state.Flags;
		else if (field == MuiAreaResizeStateField.Generation)
			value = state.Generation;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaResizeStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaResizeStateRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiAreaResizeStateField.Magic)
			state.Magic = value;
		else if (field == MuiAreaResizeStateField.Active)
			state.Active = value;
		else if (field == MuiAreaResizeStateField.Flags)
			state.Flags = value;
		else if (field == MuiAreaResizeStateField.Generation)
			state.Generation = value;
		else return false;
		return MuiAreaResizeStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiAreaResizeStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaResizeStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaResizeStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaResizeStateMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out fieldSize);

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
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaResizeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaResizeStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Active) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Flags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Generation) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaResizeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaResizeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadRecord(ref platform, address, out value) ||
			value.Magic != MuiAreaResizeStateRecord.Cookie) return false;
		value.Active = value.Active == 0 ? 0u : 1u;
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaResizeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaResizeStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Active) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Flags) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Generation) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaResizeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaResizeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (value.Magic != MuiAreaResizeStateRecord.Cookie) return false;
		value.Active = value.Active == 0 ? 0u : 1u;
		return WriteRecord(ref platform, address, value);
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
