/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Application AboutMUI/ShowHelp state shared by the presentation methods.
// Guest pointers remain caller-owned; this record only retains validated
// references and saturating request counters.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationHelpStateRecord
{
	internal const uint Size = 32;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint AboutReferenceWindowOffset = 4;
	internal const uint AboutRequestsOffset = 8;
	internal const uint HelpWindowOffset = 12;
	internal const uint HelpNameOffset = 16;
	internal const uint HelpNodeOffset = 20;
	internal const uint HelpLineOffset = 24;
	internal const uint HelpRequestsOffset = 28;
	internal const uint Cookie = 0x41485354u; // 'AHST'

	internal uint Magic;
	internal APTR AboutReferenceWindow;
	internal uint AboutRequests;
	internal APTR HelpWindow;
	internal APTR HelpName;
	internal APTR HelpNode;
	internal uint HelpLine;
	internal uint HelpRequests;
}

// Admission for the Application help/presentation sidecar. Reference windows
// remain opaque guest MUI objects, while HelpName and HelpNode remain
// caller-owned bounded C strings. Request counters and the signed HelpLine
// retain their full MorphOS ULONG/LONG representation.
internal static class MuiApplicationHelpStateAdmission
{
	private const uint MaximumStringLength = 65536;

	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiApplicationHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiApplicationHelpStateRecord.Cookie &&
		(value.AboutReferenceWindow.IsNull || platform.IsMapped(
			value.AboutReferenceWindow, 1)) &&
		(value.HelpWindow.IsNull || platform.IsMapped(value.HelpWindow, 1)) &&
		(value.HelpName.IsNull || CStringCodec.TryReadLength(ref platform,
			value.HelpName, MaximumStringLength, out _)) &&
		(value.HelpNode.IsNull || CStringCodec.TryReadLength(ref platform,
			value.HelpNode, MaximumStringLength, out _));

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, MuiApplicationHelpStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(ref platform, value) ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull)
			return false;
		if (value.AboutReferenceWindow.IsNotNull &&
			MuiHeadlessObjectCore.FindObject(ref platform, state,
				value.AboutReferenceWindow).IsNull) return false;
		if (value.HelpWindow.IsNotNull &&
			MuiHeadlessObjectCore.FindObject(ref platform, state,
				value.HelpWindow).IsNull) return false;
		return true;
	}
}

internal enum MuiApplicationHelpStateField : byte
{
	Magic,
	AboutReferenceWindow,
	AboutRequests,
	HelpWindow,
	HelpName,
	HelpNode,
	HelpLine,
	HelpRequests,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationHelpStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationHelpStateField Field;
}

internal static class MuiApplicationHelpStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationHelpStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationHelpStateRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationHelpStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationHelpStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationHelpStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationHelpStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Fixed application-help records are read and written as named value types.
// Keep the packed guest positions in this ABI adapter; production state
// consumers never select a field through the compatibility cursor.
internal static class MuiApplicationHelpStateRecordMemoryCodec
{
	private static bool TryResolve(MuiApplicationHelpStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationHelpStateField.Magic:
				offset = MuiApplicationHelpStateRecord.MagicOffset;
				return true;
			case MuiApplicationHelpStateField.AboutReferenceWindow:
				offset = MuiApplicationHelpStateRecord.AboutReferenceWindowOffset;
				return true;
			case MuiApplicationHelpStateField.AboutRequests:
				offset = MuiApplicationHelpStateRecord.AboutRequestsOffset;
				return true;
			case MuiApplicationHelpStateField.HelpWindow:
				offset = MuiApplicationHelpStateRecord.HelpWindowOffset;
				return true;
			case MuiApplicationHelpStateField.HelpName:
				offset = MuiApplicationHelpStateRecord.HelpNameOffset;
				return true;
			case MuiApplicationHelpStateField.HelpNode:
				offset = MuiApplicationHelpStateRecord.HelpNodeOffset;
				return true;
			case MuiApplicationHelpStateField.HelpLine:
				offset = MuiApplicationHelpStateRecord.HelpLineOffset;
				return true;
			case MuiApplicationHelpStateField.HelpRequests:
				offset = MuiApplicationHelpStateRecord.HelpRequestsOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationHelpStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiApplicationHelpStateRecord.Size) &&
			platform.IsMapped(address, MuiApplicationHelpStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationHelpStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationHelpStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationHelpStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationHelpStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationHelpStateField.Magic,
			out var magic) ||
			!MuiApplicationHelpStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationHelpStateField.AboutReferenceWindow,
				out var aboutWindow) ||
			!MuiApplicationHelpStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationHelpStateField.AboutRequests,
				out value.AboutRequests) ||
			!MuiApplicationHelpStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationHelpStateField.HelpWindow,
				out var helpWindow) ||
			!MuiApplicationHelpStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationHelpStateField.HelpName,
				out var helpName) ||
			!MuiApplicationHelpStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationHelpStateField.HelpNode,
				out var helpNode) ||
			!MuiApplicationHelpStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationHelpStateField.HelpLine,
				out value.HelpLine) ||
			!MuiApplicationHelpStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationHelpStateField.HelpRequests,
				out value.HelpRequests)) return false;
		value.Magic = magic;
		value.AboutReferenceWindow = APTR.FromPointer(aboutWindow);
		value.HelpWindow = APTR.FromPointer(helpWindow);
		value.HelpName = APTR.FromPointer(helpName);
		value.HelpNode = APTR.FromPointer(helpNode);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationHelpStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationHelpStateRecord.Size) ||
			!MuiApplicationHelpStateAdmission.Validate(ref platform, value))
			return false;
		return MuiApplicationHelpStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationHelpStateField.Magic,
			value.Magic) &&
			MuiApplicationHelpStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationHelpStateField.AboutReferenceWindow,
				value.AboutReferenceWindow.Raw) &&
			MuiApplicationHelpStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiApplicationHelpStateField.AboutRequests,
				value.AboutRequests) &&
			MuiApplicationHelpStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiApplicationHelpStateField.HelpWindow,
				value.HelpWindow.Raw) &&
			MuiApplicationHelpStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiApplicationHelpStateField.HelpName,
				value.HelpName.Raw) &&
			MuiApplicationHelpStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiApplicationHelpStateField.HelpNode,
				value.HelpNode.Raw) &&
			MuiApplicationHelpStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiApplicationHelpStateField.HelpLine,
				value.HelpLine) &&
			MuiApplicationHelpStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiApplicationHelpStateField.HelpRequests,
				value.HelpRequests);
	}
}
