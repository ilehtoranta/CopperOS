/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Application-owned pointer relationships. DiskObject is a caller-owned
// Workbench structure; DropObject and Menustrip are guest MUI object pointers.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationObjectStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint DiskObjectOffset = 4;
	internal const uint DropObjectOffset = 8;
	internal const uint MenustripOffset = 12;
	internal const uint Cookie = 0x414F5354u; // 'AOST'

	internal uint Magic;
	internal APTR DiskObject;
	internal APTR DropObject;
	internal APTR Menustrip;
}

// Admission for Application-owned object relationships.  DiskObject remains
// caller-owned Workbench memory; DropObject and Menustrip remain guest MUI
// capabilities and are validated against the live headless object graph.
internal static class MuiApplicationObjectStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiApplicationObjectStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiApplicationObjectStateRecord.Cookie &&
		(value.DiskObject.IsNull || platform.IsMapped(value.DiskObject,
			Amiga.DiskObject.Size)) &&
		(value.DropObject.IsNull || platform.IsMapped(value.DropObject, 1)) &&
		(value.Menustrip.IsNull || platform.IsMapped(value.Menustrip, 1));

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, MuiApplicationObjectStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(ref platform, value)) return false;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull)
			return false;
		if (value.DropObject.IsNotNull &&
			MuiHeadlessObjectCore.FindObject(ref platform, state,
				value.DropObject).IsNull) return false;
		if (value.Menustrip.IsNull) return true;
		if (value.Menustrip == application ||
			MuiHeadlessObjectCore.FindObject(ref platform, state,
				value.Menustrip).IsNull ||
			MuiMenuSpecialistCore.Classify(ref platform, state, value.Menustrip) !=
				MuiMenuSpecialistClass.Menustrip ||
			MuiHeadlessObjectCore.ParentObject(ref platform, state,
				value.Menustrip).Raw != application.Raw) return false;
		return true;
	}
}

internal enum MuiApplicationObjectStateField : byte
{
	Magic,
	DiskObject,
	DropObject,
	Menustrip,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationObjectStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationObjectStateField Field;
}

internal static class MuiApplicationObjectStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationObjectStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationObjectStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationObjectStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationObjectStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationObjectStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationObjectStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed application object state is read and written as a named value. Keep
// packed guest positions in this ABI adapter; production consumers do not
// select fields through the compatibility cursor.
internal static class MuiApplicationObjectStateRecordMemoryCodec
{
	private static bool TryResolve(MuiApplicationObjectStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationObjectStateField.Magic:
				offset = MuiApplicationObjectStateRecord.MagicOffset;
				return true;
			case MuiApplicationObjectStateField.DiskObject:
				offset = MuiApplicationObjectStateRecord.DiskObjectOffset;
				return true;
			case MuiApplicationObjectStateField.DropObject:
				offset = MuiApplicationObjectStateRecord.DropObjectOffset;
				return true;
			case MuiApplicationObjectStateField.Menustrip:
				offset = MuiApplicationObjectStateRecord.MenustripOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationObjectStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record,
			MuiApplicationObjectStateRecord.Size) &&
			platform.IsMapped(address,
				MuiApplicationObjectStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationObjectStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationObjectStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationObjectStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationObjectStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationObjectStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiApplicationObjectStateField.Magic, out var magic) ||
			!MuiApplicationObjectStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiApplicationObjectStateField.DiskObject,
				out var diskObject) ||
			!MuiApplicationObjectStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiApplicationObjectStateField.DropObject,
				out var dropObject) ||
			!MuiApplicationObjectStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiApplicationObjectStateField.Menustrip,
				out var menustrip)) return false;
		value.Magic = magic;
		value.DiskObject = APTR.FromPointer(diskObject);
		value.DropObject = APTR.FromPointer(dropObject);
		value.Menustrip = APTR.FromPointer(menustrip);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationObjectStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationObjectStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationObjectStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationObjectStateRecord.Size) ||
			!MuiApplicationObjectStateAdmission.Validate(ref platform, value))
			return false;
		return MuiApplicationObjectStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationObjectStateField.Magic,
			value.Magic) &&
			MuiApplicationObjectStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiApplicationObjectStateField.DiskObject,
				value.DiskObject.Raw) &&
			MuiApplicationObjectStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiApplicationObjectStateField.DropObject,
				value.DropObject.Raw) &&
			MuiApplicationObjectStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiApplicationObjectStateField.Menustrip,
				value.Menustrip.Raw);
	}
}
