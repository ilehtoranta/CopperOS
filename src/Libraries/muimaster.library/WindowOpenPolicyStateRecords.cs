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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint AlternateHeightOffset = 4;
	internal const uint AlternateWidthOffset = 8;
	internal const uint AlternateLeftEdgeOffset = 12;
	internal const uint AlternateTopEdgeOffset = 16;
	internal const uint HeightOffset = 20;
	internal const uint WidthOffset = 24;
	internal const uint LeftEdgeOffset = 28;
	internal const uint TopEdgeOffset = 32;
	internal const uint CloseGadgetOffset = 36;
	internal const uint DepthGadgetOffset = 40;
	internal const uint DragBarOffset = 44;
	internal const uint SizeGadgetOffset = 48;
	internal const uint SizeRightOffset = 52;
	internal const uint AppWindowOffset = 56;
	internal const uint BackdropOffset = 60;
	internal const uint BorderlessOffset = 64;
	internal const uint PanelWindowOffset = 68;
	internal const uint TabletMessagesOffset = 72;
	internal const uint UseBottomBorderScrollerOffset = 76;
	internal const uint UseLeftBorderScrollerOffset = 80;
	internal const uint UseRightBorderScrollerOffset = 84;
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
		if (field == MuiWindowOpenPolicyStateField.Magic)
			offset = MuiWindowOpenPolicyStateRecord.MagicOffset;
		else if (field == MuiWindowOpenPolicyStateField.AlternateHeight)
			offset = MuiWindowOpenPolicyStateRecord.AlternateHeightOffset;
		else if (field == MuiWindowOpenPolicyStateField.AlternateWidth)
			offset = MuiWindowOpenPolicyStateRecord.AlternateWidthOffset;
		else if (field == MuiWindowOpenPolicyStateField.AlternateLeftEdge)
			offset = MuiWindowOpenPolicyStateRecord.AlternateLeftEdgeOffset;
		else if (field == MuiWindowOpenPolicyStateField.AlternateTopEdge)
			offset = MuiWindowOpenPolicyStateRecord.AlternateTopEdgeOffset;
		else if (field == MuiWindowOpenPolicyStateField.Height)
			offset = MuiWindowOpenPolicyStateRecord.HeightOffset;
		else if (field == MuiWindowOpenPolicyStateField.Width)
			offset = MuiWindowOpenPolicyStateRecord.WidthOffset;
		else if (field == MuiWindowOpenPolicyStateField.LeftEdge)
			offset = MuiWindowOpenPolicyStateRecord.LeftEdgeOffset;
		else if (field == MuiWindowOpenPolicyStateField.TopEdge)
			offset = MuiWindowOpenPolicyStateRecord.TopEdgeOffset;
		else if (field == MuiWindowOpenPolicyStateField.CloseGadget)
			offset = MuiWindowOpenPolicyStateRecord.CloseGadgetOffset;
		else if (field == MuiWindowOpenPolicyStateField.DepthGadget)
			offset = MuiWindowOpenPolicyStateRecord.DepthGadgetOffset;
		else if (field == MuiWindowOpenPolicyStateField.DragBar)
			offset = MuiWindowOpenPolicyStateRecord.DragBarOffset;
		else if (field == MuiWindowOpenPolicyStateField.SizeGadget)
			offset = MuiWindowOpenPolicyStateRecord.SizeGadgetOffset;
		else if (field == MuiWindowOpenPolicyStateField.SizeRight)
			offset = MuiWindowOpenPolicyStateRecord.SizeRightOffset;
		else if (field == MuiWindowOpenPolicyStateField.AppWindow)
			offset = MuiWindowOpenPolicyStateRecord.AppWindowOffset;
		else if (field == MuiWindowOpenPolicyStateField.Backdrop)
			offset = MuiWindowOpenPolicyStateRecord.BackdropOffset;
		else if (field == MuiWindowOpenPolicyStateField.Borderless)
			offset = MuiWindowOpenPolicyStateRecord.BorderlessOffset;
		else if (field == MuiWindowOpenPolicyStateField.PanelWindow)
			offset = MuiWindowOpenPolicyStateRecord.PanelWindowOffset;
		else if (field == MuiWindowOpenPolicyStateField.TabletMessages)
			offset = MuiWindowOpenPolicyStateRecord.TabletMessagesOffset;
		else if (field == MuiWindowOpenPolicyStateField.UseBottomBorderScroller)
			offset = MuiWindowOpenPolicyStateRecord.UseBottomBorderScrollerOffset;
		else if (field == MuiWindowOpenPolicyStateField.UseLeftBorderScroller)
			offset = MuiWindowOpenPolicyStateRecord.UseLeftBorderScrollerOffset;
		else if (field == MuiWindowOpenPolicyStateField.UseRightBorderScroller)
			offset = MuiWindowOpenPolicyStateRecord.UseRightBorderScrollerOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
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
		return platform.IsMapped(address, MuiWindowOpenPolicyStateRecord.FieldSize);
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
	private static bool TryResolve(MuiWindowOpenPolicyStateField field,
		out uint offset)
	{
		if (field == MuiWindowOpenPolicyStateField.Magic)
			offset = MuiWindowOpenPolicyStateRecord.MagicOffset;
		else if (field == MuiWindowOpenPolicyStateField.AlternateHeight)
			offset = MuiWindowOpenPolicyStateRecord.AlternateHeightOffset;
		else if (field == MuiWindowOpenPolicyStateField.AlternateWidth)
			offset = MuiWindowOpenPolicyStateRecord.AlternateWidthOffset;
		else if (field == MuiWindowOpenPolicyStateField.AlternateLeftEdge)
			offset = MuiWindowOpenPolicyStateRecord.AlternateLeftEdgeOffset;
		else if (field == MuiWindowOpenPolicyStateField.AlternateTopEdge)
			offset = MuiWindowOpenPolicyStateRecord.AlternateTopEdgeOffset;
		else if (field == MuiWindowOpenPolicyStateField.Height)
			offset = MuiWindowOpenPolicyStateRecord.HeightOffset;
		else if (field == MuiWindowOpenPolicyStateField.Width)
			offset = MuiWindowOpenPolicyStateRecord.WidthOffset;
		else if (field == MuiWindowOpenPolicyStateField.LeftEdge)
			offset = MuiWindowOpenPolicyStateRecord.LeftEdgeOffset;
		else if (field == MuiWindowOpenPolicyStateField.TopEdge)
			offset = MuiWindowOpenPolicyStateRecord.TopEdgeOffset;
		else if (field == MuiWindowOpenPolicyStateField.CloseGadget)
			offset = MuiWindowOpenPolicyStateRecord.CloseGadgetOffset;
		else if (field == MuiWindowOpenPolicyStateField.DepthGadget)
			offset = MuiWindowOpenPolicyStateRecord.DepthGadgetOffset;
		else if (field == MuiWindowOpenPolicyStateField.DragBar)
			offset = MuiWindowOpenPolicyStateRecord.DragBarOffset;
		else if (field == MuiWindowOpenPolicyStateField.SizeGadget)
			offset = MuiWindowOpenPolicyStateRecord.SizeGadgetOffset;
		else if (field == MuiWindowOpenPolicyStateField.SizeRight)
			offset = MuiWindowOpenPolicyStateRecord.SizeRightOffset;
		else if (field == MuiWindowOpenPolicyStateField.AppWindow)
			offset = MuiWindowOpenPolicyStateRecord.AppWindowOffset;
		else if (field == MuiWindowOpenPolicyStateField.Backdrop)
			offset = MuiWindowOpenPolicyStateRecord.BackdropOffset;
		else if (field == MuiWindowOpenPolicyStateField.Borderless)
			offset = MuiWindowOpenPolicyStateRecord.BorderlessOffset;
		else if (field == MuiWindowOpenPolicyStateField.PanelWindow)
			offset = MuiWindowOpenPolicyStateRecord.PanelWindowOffset;
		else if (field == MuiWindowOpenPolicyStateField.TabletMessages)
			offset = MuiWindowOpenPolicyStateRecord.TabletMessagesOffset;
		else if (field == MuiWindowOpenPolicyStateField.UseBottomBorderScroller)
			offset = MuiWindowOpenPolicyStateRecord.UseBottomBorderScrollerOffset;
		else if (field == MuiWindowOpenPolicyStateField.UseLeftBorderScroller)
			offset = MuiWindowOpenPolicyStateRecord.UseLeftBorderScrollerOffset;
		else if (field == MuiWindowOpenPolicyStateField.UseRightBorderScroller)
			offset = MuiWindowOpenPolicyStateRecord.UseRightBorderScrollerOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowOpenPolicyStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryResolve(field, out var offset) &&
			TryGetAddress(ref platform, record, offset, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowOpenPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiWindowOpenPolicyStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowOpenPolicyStateField.Magic)
			value = state.Magic;
		else if (field == MuiWindowOpenPolicyStateField.AlternateHeight)
			value = unchecked((uint)state.AlternateHeight);
		else if (field == MuiWindowOpenPolicyStateField.AlternateWidth)
			value = unchecked((uint)state.AlternateWidth);
		else if (field == MuiWindowOpenPolicyStateField.AlternateLeftEdge)
			value = unchecked((uint)state.AlternateLeftEdge);
		else if (field == MuiWindowOpenPolicyStateField.AlternateTopEdge)
			value = unchecked((uint)state.AlternateTopEdge);
		else if (field == MuiWindowOpenPolicyStateField.Height)
			value = unchecked((uint)state.Height);
		else if (field == MuiWindowOpenPolicyStateField.Width)
			value = unchecked((uint)state.Width);
		else if (field == MuiWindowOpenPolicyStateField.LeftEdge)
			value = unchecked((uint)state.LeftEdge);
		else if (field == MuiWindowOpenPolicyStateField.TopEdge)
			value = unchecked((uint)state.TopEdge);
		else if (field == MuiWindowOpenPolicyStateField.CloseGadget)
			value = state.CloseGadget;
		else if (field == MuiWindowOpenPolicyStateField.DepthGadget)
			value = state.DepthGadget;
		else if (field == MuiWindowOpenPolicyStateField.DragBar)
			value = state.DragBar;
		else if (field == MuiWindowOpenPolicyStateField.SizeGadget)
			value = state.SizeGadget;
		else if (field == MuiWindowOpenPolicyStateField.SizeRight)
			value = state.SizeRight;
		else if (field == MuiWindowOpenPolicyStateField.AppWindow)
			value = state.AppWindow;
		else if (field == MuiWindowOpenPolicyStateField.Backdrop)
			value = state.Backdrop;
		else if (field == MuiWindowOpenPolicyStateField.Borderless)
			value = state.Borderless;
		else if (field == MuiWindowOpenPolicyStateField.PanelWindow)
			value = state.PanelWindow;
		else if (field == MuiWindowOpenPolicyStateField.TabletMessages)
			value = state.TabletMessages;
		else if (field == MuiWindowOpenPolicyStateField.UseBottomBorderScroller)
			value = state.UseBottomBorderScroller;
		else if (field == MuiWindowOpenPolicyStateField.UseLeftBorderScroller)
			value = state.UseLeftBorderScroller;
		else if (field == MuiWindowOpenPolicyStateField.UseRightBorderScroller)
			value = state.UseRightBorderScroller;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowOpenPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWindowOpenPolicyStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowOpenPolicyStateField.Magic)
			state.Magic = value;
		else if (field == MuiWindowOpenPolicyStateField.AlternateHeight)
			state.AlternateHeight = unchecked((int)value);
		else if (field == MuiWindowOpenPolicyStateField.AlternateWidth)
			state.AlternateWidth = unchecked((int)value);
		else if (field == MuiWindowOpenPolicyStateField.AlternateLeftEdge)
			state.AlternateLeftEdge = unchecked((int)value);
		else if (field == MuiWindowOpenPolicyStateField.AlternateTopEdge)
			state.AlternateTopEdge = unchecked((int)value);
		else if (field == MuiWindowOpenPolicyStateField.Height)
			state.Height = unchecked((int)value);
		else if (field == MuiWindowOpenPolicyStateField.Width)
			state.Width = unchecked((int)value);
		else if (field == MuiWindowOpenPolicyStateField.LeftEdge)
			state.LeftEdge = unchecked((int)value);
		else if (field == MuiWindowOpenPolicyStateField.TopEdge)
			state.TopEdge = unchecked((int)value);
		else if (field == MuiWindowOpenPolicyStateField.CloseGadget)
			state.CloseGadget = value;
		else if (field == MuiWindowOpenPolicyStateField.DepthGadget)
			state.DepthGadget = value;
		else if (field == MuiWindowOpenPolicyStateField.DragBar)
			state.DragBar = value;
		else if (field == MuiWindowOpenPolicyStateField.SizeGadget)
			state.SizeGadget = value;
		else if (field == MuiWindowOpenPolicyStateField.SizeRight)
			state.SizeRight = value;
		else if (field == MuiWindowOpenPolicyStateField.AppWindow)
			state.AppWindow = value;
		else if (field == MuiWindowOpenPolicyStateField.Backdrop)
			state.Backdrop = value;
		else if (field == MuiWindowOpenPolicyStateField.Borderless)
			state.Borderless = value;
		else if (field == MuiWindowOpenPolicyStateField.PanelWindow)
			state.PanelWindow = value;
		else if (field == MuiWindowOpenPolicyStateField.TabletMessages)
			state.TabletMessages = value;
		else if (field == MuiWindowOpenPolicyStateField.UseBottomBorderScroller)
			state.UseBottomBorderScroller = value;
		else if (field == MuiWindowOpenPolicyStateField.UseLeftBorderScroller)
			state.UseLeftBorderScroller = value;
		else if (field == MuiWindowOpenPolicyStateField.UseRightBorderScroller)
			state.UseRightBorderScroller = value;
		else return false;
		return MuiWindowOpenPolicyStateRecordCodec.WriteStructural(ref platform,
			record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiWindowOpenPolicyStateRecord.Size -
			MuiWindowOpenPolicyStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiWindowOpenPolicyStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiWindowOpenPolicyStateRecord.FieldSize);
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
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
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
			address, field, out value);

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
			address, field, value);

	private static bool WriteSigned<TPlatform>(ref TPlatform platform,
		APTR address, MuiWindowOpenPolicyStateField field, int value)
		where TPlatform : struct, IMuiGuestMemory =>
		Write(ref platform, address, field, unchecked((uint)value));
}
