/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Initializer-only window policy captured at the native OpenWindow boundary.
// Signed geometry remains signed in the semantic record; all other fields
// retain MorphOS ULONG semantics.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowOpenPolicyStateRecord
{
	internal const uint Size = 88;
	internal const uint Cookie = 0x574F5053u; // 'WOPS'

	internal uint Magic;
	internal int AlternateHeight;
	internal int AlternateWidth;
	internal int AlternateLeftEdge;
	internal int AlternateTopEdge;
	internal int Height;
	internal int Width;
	internal int LeftEdge;
	internal int TopEdge;
	internal uint CloseGadget;
	internal uint DepthGadget;
	internal uint DragBar;
	internal uint SizeGadget;
	internal uint SizeRight;
	internal uint AppWindow;
	internal uint Backdrop;
	internal uint Borderless;
	internal uint PanelWindow;
	internal uint TabletMessages;
	internal uint UseBottomBorderScroller;
	internal uint UseLeftBorderScroller;
	internal uint UseRightBorderScroller;
}

// The structural codec owns the packed guest representation. This admission
// boundary owns the canonical MorphOS BOOL invariants; signed geometry remains
// represented by the named Int32 fields above.
internal static class MuiWindowOpenPolicyStateAdmission
{
	internal static bool Validate(MuiWindowOpenPolicyStateRecord value) =>
		value.Magic == MuiWindowOpenPolicyStateRecord.Cookie &&
		value.CloseGadget <= 1 && value.DepthGadget <= 1 &&
		value.DragBar <= 1 && value.SizeGadget <= 1 && value.SizeRight <= 1 &&
		value.AppWindow <= 1 && value.Backdrop <= 1 && value.Borderless <= 1 &&
		value.PanelWindow <= 1 && value.TabletMessages <= 1 &&
		value.UseBottomBorderScroller <= 1 &&
		value.UseLeftBorderScroller <= 1 &&
		value.UseRightBorderScroller <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, MuiWindowOpenPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !window.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, window).IsNull;
}

internal enum MuiWindowOpenPolicyStateField : byte
{
	Magic,
	AlternateHeight,
	AlternateWidth,
	AlternateLeftEdge,
	AlternateTopEdge,
	Height,
	Width,
	LeftEdge,
	TopEdge,
	CloseGadget,
	DepthGadget,
	DragBar,
	SizeGadget,
	SizeRight,
	AppWindow,
	Backdrop,
	Borderless,
	PanelWindow,
	TabletMessages,
	UseBottomBorderScroller,
	UseLeftBorderScroller,
	UseRightBorderScroller,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowOpenPolicyStateFieldCursor
{
	internal APTR Record;
	internal MuiWindowOpenPolicyStateField Field;
}

internal static class MuiWindowOpenPolicyStateFieldCursorCodec
{
	private static bool TryResolve(MuiWindowOpenPolicyStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiWindowOpenPolicyStateField.Magic:
			case MuiWindowOpenPolicyStateField.AlternateHeight:
			case MuiWindowOpenPolicyStateField.AlternateWidth:
			case MuiWindowOpenPolicyStateField.AlternateLeftEdge:
			case MuiWindowOpenPolicyStateField.AlternateTopEdge:
			case MuiWindowOpenPolicyStateField.Height:
			case MuiWindowOpenPolicyStateField.Width:
			case MuiWindowOpenPolicyStateField.LeftEdge:
			case MuiWindowOpenPolicyStateField.TopEdge:
			case MuiWindowOpenPolicyStateField.CloseGadget:
			case MuiWindowOpenPolicyStateField.DepthGadget:
			case MuiWindowOpenPolicyStateField.DragBar:
			case MuiWindowOpenPolicyStateField.SizeGadget:
			case MuiWindowOpenPolicyStateField.SizeRight:
			case MuiWindowOpenPolicyStateField.AppWindow:
			case MuiWindowOpenPolicyStateField.Backdrop:
			case MuiWindowOpenPolicyStateField.Borderless:
			case MuiWindowOpenPolicyStateField.PanelWindow:
			case MuiWindowOpenPolicyStateField.TabletMessages:
			case MuiWindowOpenPolicyStateField.UseBottomBorderScroller:
			case MuiWindowOpenPolicyStateField.UseLeftBorderScroller:
			case MuiWindowOpenPolicyStateField.UseRightBorderScroller:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowOpenPolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiWindowOpenPolicyStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowOpenPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiWindowOpenPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowOpenPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiWindowOpenPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter for the initializer-only OpenWindow
// policy record. Signed geometry and MorphOS BOOL projections stay named in
// the semantic struct; this bounded boundary owns fixed guest translation.
internal static class MuiWindowOpenPolicyStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiWindowOpenPolicyStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiWindowOpenPolicyStateRecord.Size)) return false;
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

internal static class MuiWindowOpenPolicyStateRecordCodec
{
	// Sequential named-struct path used by the OpenWindow policy boundary.
	// Signed geometry and canonical BOOL fields are exchanged in declaration
	// order; numeric positions remain confined to the compatibility adapter.
	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiWindowOpenPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWindowOpenPolicyStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.AlternateHeight)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.AlternateWidth)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.AlternateLeftEdge)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.AlternateTopEdge)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Height)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Width)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.LeftEdge)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.TopEdge)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.CloseGadget) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.DepthGadget) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.DragBar) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.SizeGadget) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.SizeRight) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.AppWindow) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Backdrop) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Borderless) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.PanelWindow) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.TabletMessages) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.UseBottomBorderScroller) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.UseLeftBorderScroller) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.UseRightBorderScroller) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiWindowOpenPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWindowOpenPolicyStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var alternateHeight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var alternateWidth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var alternateLeftEdge) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var alternateTopEdge) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var height) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var width) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var leftEdge) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var topEdge) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var closeGadget) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var depthGadget) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var dragBar) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var sizeGadget) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var sizeRight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var appWindow) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var backdrop) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var borderless) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var panelWindow) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var tabletMessages) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var useBottomBorderScroller) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var useLeftBorderScroller) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var useRightBorderScroller) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Magic = magic;
		value.AlternateHeight = unchecked((int)alternateHeight);
		value.AlternateWidth = unchecked((int)alternateWidth);
		value.AlternateLeftEdge = unchecked((int)alternateLeftEdge);
		value.AlternateTopEdge = unchecked((int)alternateTopEdge);
		value.Height = unchecked((int)height);
		value.Width = unchecked((int)width);
		value.LeftEdge = unchecked((int)leftEdge);
		value.TopEdge = unchecked((int)topEdge);
		value.CloseGadget = closeGadget;
		value.DepthGadget = depthGadget;
		value.DragBar = dragBar;
		value.SizeGadget = sizeGadget;
		value.SizeRight = sizeRight;
		value.AppWindow = appWindow;
		value.Backdrop = backdrop;
		value.Borderless = borderless;
		value.PanelWindow = panelWindow;
		value.TabletMessages = tabletMessages;
		value.UseBottomBorderScroller = useBottomBorderScroller;
		value.UseLeftBorderScroller = useLeftBorderScroller;
		value.UseRightBorderScroller = useRightBorderScroller;
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiWindowOpenPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowOpenPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return TryReadStructural(ref platform, address, out value) &&
			MuiWindowOpenPolicyStateAdmission.Validate(value);
	}

	private static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowOpenPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiWindowOpenPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, (uint)field * 4, out value);

	private static bool TryReadSigned<TPlatform>(ref TPlatform platform,
		APTR address, MuiWindowOpenPolicyStateField field, out int value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryRead(ref platform, address, field, out var raw)) return false;
		value = unchecked((int)raw);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowOpenPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiWindowOpenPolicyStateRecord.Size) ||
			!MuiWindowOpenPolicyStateAdmission.Validate(value)) return false;
		return WriteRecord(ref platform, address, value);
	}

	private static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowOpenPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiWindowOpenPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, (uint)field * 4, value);

	private static bool WriteSigned<TPlatform>(ref TPlatform platform,
		APTR address, MuiWindowOpenPolicyStateField field, int value)
		where TPlatform : struct, IMuiGuestMemory =>
		Write(ref platform, address, field, unchecked((uint)value));
}
