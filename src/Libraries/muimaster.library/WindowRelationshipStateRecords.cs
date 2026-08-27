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
		switch (field)
		{
			case MuiWindowRelationshipStateField.Magic:
			case MuiWindowRelationshipStateField.RootObject:
			case MuiWindowRelationshipStateField.Menustrip:
			case MuiWindowRelationshipStateField.RefWindow:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
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
		return platform.IsMapped(address, 4);
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiWindowRelationshipStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiWindowRelationshipStateRecord.Size)) return false;
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

internal static class MuiWindowRelationshipStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiWindowRelationshipStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out var magic) ||
			!MuiWindowRelationshipStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 4, out var rootObject) ||
			!MuiWindowRelationshipStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 8, out var menustrip) ||
			!MuiWindowRelationshipStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 12, out var refWindow)) return false;
		value.Magic = magic;
		value.RootObject = APTR.FromPointer(rootObject);
		value.Menustrip = APTR.FromPointer(menustrip);
		value.RefWindow = APTR.FromPointer(refWindow);
		return true;
	}

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
		return MuiWindowRelationshipStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 0, value.Magic) &&
			MuiWindowRelationshipStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 4, value.RootObject.Raw) &&
			MuiWindowRelationshipStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 8, value.Menustrip.Raw) &&
			MuiWindowRelationshipStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 12, value.RefWindow.Raw);
	}
}
