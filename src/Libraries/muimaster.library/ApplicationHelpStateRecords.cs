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
// Keep field selection structural: the bounded cursor consumes the complete
// record before exposing a field address to compatibility callers.
internal static class MuiApplicationHelpStateRecordMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiApplicationHelpStateField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		switch (field)
		{
			case MuiApplicationHelpStateField.Magic:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationHelpStateRecord.FieldSize, out address);
			case MuiApplicationHelpStateField.AboutReferenceWindow:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationHelpStateRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationHelpStateRecord.FieldSize, out address);
			case MuiApplicationHelpStateField.AboutRequests:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationHelpStateRecord.FieldSize, out address);
			case MuiApplicationHelpStateField.HelpWindow:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationHelpStateRecord.FieldSize, out address);
			case MuiApplicationHelpStateField.HelpName:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationHelpStateRecord.FieldSize, out address);
			case MuiApplicationHelpStateField.HelpNode:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationHelpStateRecord.FieldSize, out address);
			case MuiApplicationHelpStateField.HelpLine:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationHelpStateRecord.FieldSize, out address);
			case MuiApplicationHelpStateField.HelpRequests:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationHelpStateRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationHelpStateRecord.FieldSize, out address);
			default:
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationHelpStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, record,
			MuiApplicationHelpStateRecord.Size, out var cursor) ||
			!TryTakeField(ref platform, ref cursor, field, out address))
			return false;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationHelpStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiApplicationHelpStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiApplicationHelpStateField.Magic)
			value = state.Magic;
		else if (field == MuiApplicationHelpStateField.AboutReferenceWindow)
			value = state.AboutReferenceWindow.Raw;
		else if (field == MuiApplicationHelpStateField.AboutRequests)
			value = state.AboutRequests;
		else if (field == MuiApplicationHelpStateField.HelpWindow)
			value = state.HelpWindow.Raw;
		else if (field == MuiApplicationHelpStateField.HelpName)
			value = state.HelpName.Raw;
		else if (field == MuiApplicationHelpStateField.HelpNode)
			value = state.HelpNode.Raw;
		else if (field == MuiApplicationHelpStateField.HelpLine)
			value = state.HelpLine;
		else if (field == MuiApplicationHelpStateField.HelpRequests)
			value = state.HelpRequests;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationHelpStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiApplicationHelpStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiApplicationHelpStateField.Magic)
			state.Magic = value;
		else if (field == MuiApplicationHelpStateField.AboutReferenceWindow)
			state.AboutReferenceWindow = APTR.FromPointer(value);
		else if (field == MuiApplicationHelpStateField.AboutRequests)
			state.AboutRequests = value;
		else if (field == MuiApplicationHelpStateField.HelpWindow)
			state.HelpWindow = APTR.FromPointer(value);
		else if (field == MuiApplicationHelpStateField.HelpName)
			state.HelpName = APTR.FromPointer(value);
		else if (field == MuiApplicationHelpStateField.HelpNode)
			state.HelpNode = APTR.FromPointer(value);
		else if (field == MuiApplicationHelpStateField.HelpLine)
			state.HelpLine = value;
		else if (field == MuiApplicationHelpStateField.HelpRequests)
			state.HelpRequests = value;
		else return false;
		return MuiApplicationHelpStateRecordCodec.WriteRecord(ref platform,
			record, state);
	}
}

internal static class MuiApplicationHelpStateRecordCodec
{
	// AboutMUI/ShowHelp state is a fixed eight-ULONG record. Exchange the
	// declaration-ordered fields through one named struct; pointer and string
	// validity remains in the admission layer rather than in the wire codec.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationHelpStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var aboutWindow) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.AboutRequests) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var helpWindow) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var helpName) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var helpNode) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.HelpLine) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.HelpRequests)) return false;
		value.AboutReferenceWindow = APTR.FromPointer(aboutWindow);
		value.HelpWindow = APTR.FromPointer(helpWindow);
		value.HelpName = APTR.FromPointer(helpName);
		value.HelpNode = APTR.FromPointer(helpNode);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationHelpStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.AboutReferenceWindow.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.AboutRequests) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.HelpWindow.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.HelpName.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.HelpNode.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.HelpLine) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.HelpRequests) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

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
		return WriteRecord(ref platform, address, value);
	}
}
