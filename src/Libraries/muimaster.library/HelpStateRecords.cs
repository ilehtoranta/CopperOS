/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_HelpNode and MUIA_HelpLine are common [ISG] object attributes.  Keep
// their state together as one guest-resident record: the node is an opaque
// caller-owned C-string pointer and the line is a signed LONG carried on the
// 32-bit guest bus.  No managed string or object-private offset is needed.
public struct MuiHelpStateInput
{
	public APTR Node;
	public int Line;
	public uint Generation;
}

// Result of the application-level online-help walk.  The source objects are
// guest pointers retained for diagnostics and future presentation policy; no
// managed object graph is created while walking the Parent chain.
public struct MuiHelpResolutionInput
{
	public APTR Node;
	public int Line;
	public APTR NodeObject;
	public APTR LineObject;
}

// Typed input for the automatic online-help trigger.  The application/window
// and current object are guest object pointers; Name is optional and follows
// MUIM_Application_ShowHelp's caller-owned help-file override semantics.
// MuiKey is the preprocessed MorphOS MUIKEY value, not a raw Intuition code.
public struct MuiHelpTriggerInput
{
	public const int HelpKey = 20;

	public APTR Application;
	public APTR Window;
	public APTR CurrentObject;
	public APTR Name;
	public int MuiKey;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHelpStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x48454C50u; // 'HELP'

	internal uint Magic;
	internal APTR Node;
	internal uint Line;
	internal uint Generation;
}

internal static class MuiHelpStateAdmission
{
	internal static bool Validate(MuiHelpStateRecord value) =>
		value.Magic == MuiHelpStateRecord.Cookie && value.Generation != 0;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiHelpStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiHelpStateField : byte
{
	Magic,
	Node,
	Line,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHelpStateFieldCursor
{
	internal APTR Record;
	internal MuiHelpStateField Field;
}

internal static class MuiHelpStateFieldCursorCodec
{
	private static bool TryResolve(MuiHelpStateField field, out uint offset)
	{
		switch (field)
		{
			case MuiHelpStateField.Magic:
				offset = 0;
				return true;
			case MuiHelpStateField.Node:
				offset = 4;
				return true;
			case MuiHelpStateField.Line:
				offset = 8;
				return true;
			case MuiHelpStateField.Generation:
				offset = 12;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiHelpStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiHelpStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiHelpStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiHelpStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiHelpStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiHelpStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. The help-node pointer, signed line, and
// generation remain named semantic fields; bounded fixed-layout translation
// is isolated here.
internal static class MuiHelpStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiHelpStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiHelpStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiHelpStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiHelpStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var node) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Line) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Generation)) return false;
		value.Node = APTR.FromPointer(node);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiHelpStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Node.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Line) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Generation) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiHelpStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiHelpStateAdmission.Validate(value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}
