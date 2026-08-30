/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessMethodMessage
{
	public const uint Size = 4;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	public uint MethodId;
}

internal enum MuiHeadlessMethodMessageField : byte
{
	MethodId,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessMethodMessageFieldCursor
{
	internal APTR Record;
	internal MuiHeadlessMethodMessageField Field;
}

// Struct-first guest-memory adapter for the fixed headless method header.
// Wire positions are confined here; dispatcher code receives the named
// MuiHeadlessMethodMessage value instead of addressing the selector directly.
internal static class MuiHeadlessMethodMessageRecordMemoryCodec
{
	private static bool TryResolve(MuiHeadlessMethodMessageField field,
		out uint offset)
	{
		offset = field == MuiHeadlessMethodMessageField.MethodId ?
			MuiHeadlessMethodMessage.MethodIdOffset : uint.MaxValue;
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessMethodMessageField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			record, MuiHeadlessMethodMessage.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiHeadlessMethodMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessMethodMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessMethodMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiHeadlessMethodMessageFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiHeadlessMethodMessageFieldCursor cursor, out APTR address)
	where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessMethodMessageRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessMethodMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessMethodMessageRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessMethodMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessMethodMessageRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
}

internal static class MuiHeadlessMethodMessageRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiHeadlessMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiHeadlessMessageStructCodec.TryReadMethodIdValue(ref platform,
			address, out value.MethodId);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiHeadlessMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessMessageStructCodec.TryWriteMethod(ref platform, address,
			value.MethodId);
}

// BOOPSI OM_SET carries a standard TagItem list and an optional GadgetInfo
// pointer. Keep that fixed packet named at the MUI boundary; only this codec
// knows the packed guest positions of the three ULONG fields.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessOmSetMessage
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint AttributesOffset = 4;
	internal const uint GadgetInfoOffset = 8;
	internal uint MethodId;
	internal APTR Attributes;
	internal APTR GadgetInfo;
}

internal enum MuiHeadlessOmSetField : byte
{
	MethodId,
	Attributes,
	GadgetInfo,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessOmSetFieldCursor
{
	internal APTR Record;
	internal MuiHeadlessOmSetField Field;
}

// Struct-first guest-memory adapter for the fixed BOOPSI OM_SET packet. The
// named packet owns the wire positions; this bounded adapter is the only
// place that projects them into guest memory.
internal static class MuiHeadlessOmSetMessageMemoryCodec
{
	private static bool TryResolve(MuiHeadlessOmSetField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiHeadlessOmSetField.MethodId:
				offset = MuiHeadlessOmSetMessage.MethodIdOffset;
				return true;
			case MuiHeadlessOmSetField.Attributes:
				offset = MuiHeadlessOmSetMessage.AttributesOffset;
				return true;
			case MuiHeadlessOmSetField.GadgetInfo:
				offset = MuiHeadlessOmSetMessage.GadgetInfoOffset;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessOmSetField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, MuiHeadlessOmSetMessage.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiHeadlessOmSetMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessOmSetField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessOmSetField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiHeadlessOmSetMessageCodec
{
	internal const uint Method = 0x00000103u;

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiHeadlessOmSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiHeadlessMessageStructCodec.TryReadOmSet(ref platform, address,
			out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR address, MuiHeadlessOmSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiHeadlessMessageStructCodec.TryWriteOmSet(ref platform, address,
			value);
}

// BOOPSI OM_UPDATE carries the same caller-owned TagItem list as OM_SET, plus
// the documented update flags. Keep the four guest ULONG fields in a named
// record so the dispatcher never treats opUpdate as an ad-hoc offset tuple.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessOmUpdateMessage
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint AttributesOffset = 4;
	internal const uint GadgetInfoOffset = 8;
	internal const uint FlagsOffset = 12;
	internal uint MethodId;
	internal APTR Attributes;
	internal APTR GadgetInfo;
	internal uint Flags;
}

internal enum MuiHeadlessOmUpdateField : byte
{
	MethodId,
	Attributes,
	GadgetInfo,
	Flags,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessOmUpdateFieldCursor
{
	internal APTR Record;
	internal MuiHeadlessOmUpdateField Field;
}

// Struct-first guest-memory adapter for the fixed BOOPSI OM_UPDATE packet.
// The named packet owns the wire positions; this bounded adapter is the only
// place that projects them into guest memory.
internal static class MuiHeadlessOmUpdateMessageMemoryCodec
{
	private static bool TryResolve(MuiHeadlessOmUpdateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiHeadlessOmUpdateField.MethodId:
				offset = MuiHeadlessOmUpdateMessage.MethodIdOffset;
				return true;
			case MuiHeadlessOmUpdateField.Attributes:
				offset = MuiHeadlessOmUpdateMessage.AttributesOffset;
				return true;
			case MuiHeadlessOmUpdateField.GadgetInfo:
				offset = MuiHeadlessOmUpdateMessage.GadgetInfoOffset;
				return true;
			case MuiHeadlessOmUpdateField.Flags:
				offset = MuiHeadlessOmUpdateMessage.FlagsOffset;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessOmUpdateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, MuiHeadlessOmUpdateMessage.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiHeadlessOmUpdateMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessOmUpdateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessOmUpdateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiHeadlessOmUpdateMessageCodec
{
	internal const uint Method = 0x00000108u;

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiHeadlessOmUpdateMessage value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiHeadlessMessageStructCodec.TryReadOmUpdate(ref platform, address,
			out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR address, MuiHeadlessOmUpdateMessage value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiHeadlessMessageStructCodec.TryWriteOmUpdate(ref platform, address,
			value);
}

// Sequential codecs for the fixed headless/BOOPSI message records.  The
// field adapters above remain compatibility diagnostics; live dispatch paths
// consume these declaration-ordered named structs and validate the complete
// record before exposing any caller-owned pointer.
internal static class MuiHeadlessMessageStructCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiHeadlessMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out methodId) || !MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool TryWriteMethod<TPlatform>(ref TPlatform platform,
		APTR address, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiHeadlessMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadOmSet<TPlatform>(ref TPlatform platform,
		APTR address, out MuiHeadlessOmSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiHeadlessOmSetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var attributes) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var gadgetInfo) || !MuiGuestStructCursor.IsComplete(cursor) ||
			methodId != MuiHeadlessOmSetMessageCodec.Method) return false;
		value.MethodId = methodId;
		value.Attributes = APTR.FromPointer(attributes);
		value.GadgetInfo = APTR.FromPointer(gadgetInfo);
		return true;
	}

	internal static bool TryWriteOmSet<TPlatform>(ref TPlatform platform,
		APTR address, MuiHeadlessOmSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiHeadlessOmSetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				MuiHeadlessOmSetMessageCodec.Method) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attributes.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.GadgetInfo.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadOmUpdate<TPlatform>(ref TPlatform platform,
		APTR address, out MuiHeadlessOmUpdateMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiHeadlessOmUpdateMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var attributes) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var gadgetInfo) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var flags) || !MuiGuestStructCursor.IsComplete(cursor) ||
			methodId != MuiHeadlessOmUpdateMessageCodec.Method) return false;
		value.MethodId = methodId;
		value.Attributes = APTR.FromPointer(attributes);
		value.GadgetInfo = APTR.FromPointer(gadgetInfo);
		value.Flags = flags;
		return true;
	}

	internal static bool TryWriteOmUpdate<TPlatform>(ref TPlatform platform,
		APTR address, MuiHeadlessOmUpdateMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiHeadlessOmUpdateMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				MuiHeadlessOmUpdateMessageCodec.Method) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attributes.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.GadgetInfo.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Flags)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Shared codec for the fixed method header used by the headless dispatcher
// entry points. Specialized packet codecs remain responsible for their full
// records; this seam only owns the common method-word boundary.
internal static class MuiHeadlessMessageCodec
{
	// Keep scalar selector admission at the guest ABI boundary; dispatcher
	// consumers continue to receive the named fixed-width header record.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiHeadlessMessageStructCodec.TryReadMethodIdValue(ref platform,
			message, out methodId);

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiHeadlessMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryReadMethodIdValue(ref platform, message, out packet.MethodId);
	}
}
