/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Window-owned object relationships.  These pointers remain caller-owned
// guest objects; the record is the named ABI snapshot consumed by public
// getters and relationship transitions.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowRelationshipStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint RootObjectOffset = 4;
	internal const uint MenustripOffset = 8;
	internal const uint RefWindowOffset = 12;
	internal const uint Cookie = 0x57524C54u; // 'WRLT'

	internal uint Magic;
	internal APTR RootObject;
	internal APTR Menustrip;
	internal APTR RefWindow;
}

// The structural record carries opaque guest pointers.  The codec-level
// predicate admits only mapped pointer carriers; WindowPublicCore adds the
// owner/family relationship checks that require the object state registry.
internal static class MuiWindowRelationshipStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiWindowRelationshipStateRecord.Cookie &&
		IsMapped(ref platform, value.RootObject) &&
		IsMapped(ref platform, value.Menustrip) &&
		IsMapped(ref platform, value.RefWindow);

	private static bool IsMapped<TPlatform>(ref TPlatform platform, APTR value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.IsNull || platform.IsMapped(value, 1);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, MuiWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(ref platform, value) || owner.IsNull) return false;
		if (!ValidateOwnedChild(ref platform, state, owner, value.RootObject,
			false) || !ValidateOwnedChild(ref platform, state, owner,
			value.Menustrip, true)) return false;
		return value.RefWindow.IsNull || (value.RefWindow != owner &&
			!MuiHeadlessObjectCore.FindObject(ref platform, state,
				value.RefWindow).IsNull);
	}

	private static bool ValidateOwnedChild<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, APTR child, bool menustrip)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (child.IsNull) return true;
		if (child == owner || MuiHeadlessObjectCore.FindObject(ref platform, state,
			child).IsNull || MuiHeadlessObjectCore.ParentObject(ref platform,
			state, child) != owner) return false;
		return !menustrip || MuiMenuSpecialistCore.Classify(ref platform, state,
			child) == MuiMenuSpecialistClass.Menustrip;
	}
}

internal enum MuiWindowRelationshipStateField : byte
{
	Magic,
	RootObject,
	Menustrip,
	RefWindow,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowRelationshipStateFieldCursor
{
	internal APTR Record;
	internal MuiWindowRelationshipStateField Field;
}

internal static class MuiWindowRelationshipStateFieldCursorCodec
{
	private static bool TryResolve(MuiWindowRelationshipStateField field,
		out uint offset)
	{
		if (field == MuiWindowRelationshipStateField.Magic)
			offset = MuiWindowRelationshipStateRecord.MagicOffset;
		else if (field == MuiWindowRelationshipStateField.RootObject)
			offset = MuiWindowRelationshipStateRecord.RootObjectOffset;
		else if (field == MuiWindowRelationshipStateField.Menustrip)
			offset = MuiWindowRelationshipStateRecord.MenustripOffset;
		else if (field == MuiWindowRelationshipStateField.RefWindow)
			offset = MuiWindowRelationshipStateRecord.RefWindowOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowRelationshipStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiWindowRelationshipStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, MuiWindowRelationshipStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowRelationshipStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiWindowRelationshipStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowRelationshipStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiWindowRelationshipStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Relationship capabilities remain named
// APTR fields; only this bounded boundary translates their fixed guest slots.
internal static class MuiWindowRelationshipStateRecordMemoryCodec
{
	private static bool TryResolve(MuiWindowRelationshipStateField field,
		out uint offset)
	{
		if (field == MuiWindowRelationshipStateField.Magic)
			offset = MuiWindowRelationshipStateRecord.MagicOffset;
		else if (field == MuiWindowRelationshipStateField.RootObject)
			offset = MuiWindowRelationshipStateRecord.RootObjectOffset;
		else if (field == MuiWindowRelationshipStateField.Menustrip)
			offset = MuiWindowRelationshipStateRecord.MenustripOffset;
		else if (field == MuiWindowRelationshipStateField.RefWindow)
			offset = MuiWindowRelationshipStateRecord.RefWindowOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowRelationshipStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryResolve(field, out var offset) &&
			TryGetAddress(ref platform, record, offset, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowRelationshipStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiWindowRelationshipStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowRelationshipStateField.Magic)
			value = state.Magic;
		else if (field == MuiWindowRelationshipStateField.RootObject)
			value = state.RootObject.Raw;
		else if (field == MuiWindowRelationshipStateField.Menustrip)
			value = state.Menustrip.Raw;
		else if (field == MuiWindowRelationshipStateField.RefWindow)
			value = state.RefWindow.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowRelationshipStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWindowRelationshipStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowRelationshipStateField.Magic)
			state.Magic = value;
		else if (field == MuiWindowRelationshipStateField.RootObject)
			state.RootObject = APTR.FromPointer(value);
		else if (field == MuiWindowRelationshipStateField.Menustrip)
			state.Menustrip = APTR.FromPointer(value);
		else if (field == MuiWindowRelationshipStateField.RefWindow)
			state.RefWindow = APTR.FromPointer(value);
		else return false;
		return MuiWindowRelationshipStateRecordCodec.WriteStructural(ref platform,
			record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiWindowRelationshipStateRecord.Size -
			MuiWindowRelationshipStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiWindowRelationshipStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiWindowRelationshipStateRecord.FieldSize);
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

internal static class MuiWindowRelationshipStateRecordCodec
{
	// Sequential named-struct path used by Window.mui. Cookie and the three
	// opaque relationship pointers are exchanged in declaration order; numeric
	// positions remain confined to the bounded compatibility adapter.
	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWindowRelationshipStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.RootObject.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Menustrip.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.RefWindow.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWindowRelationshipStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rootObject) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var menustrip) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var refWindow) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Magic = magic;
		value.RootObject = APTR.FromPointer(rootObject);
		value.Menustrip = APTR.FromPointer(menustrip);
		value.RefWindow = APTR.FromPointer(refWindow);
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiWindowRelationshipStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWindowRelationshipStateAdmission.Validate(ref platform, value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
