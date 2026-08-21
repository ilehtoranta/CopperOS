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

internal static class MuiWindowOpenPolicyStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowOpenPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiWindowOpenPolicyStateRecord.Size) ||
			!TryRead(ref platform, address,
				MuiWindowOpenPolicyStateField.Magic, out var magic) ||
			magic != MuiWindowOpenPolicyStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!TryReadSigned(ref platform, address,
			MuiWindowOpenPolicyStateField.AlternateHeight, out value.AlternateHeight) ||
			!TryReadSigned(ref platform, address,
				MuiWindowOpenPolicyStateField.AlternateWidth, out value.AlternateWidth) ||
			!TryReadSigned(ref platform, address,
				MuiWindowOpenPolicyStateField.AlternateLeftEdge, out value.AlternateLeftEdge) ||
			!TryReadSigned(ref platform, address,
				MuiWindowOpenPolicyStateField.AlternateTopEdge, out value.AlternateTopEdge) ||
			!TryReadSigned(ref platform, address,
				MuiWindowOpenPolicyStateField.Height, out value.Height) ||
			!TryReadSigned(ref platform, address,
				MuiWindowOpenPolicyStateField.Width, out value.Width) ||
			!TryReadSigned(ref platform, address,
				MuiWindowOpenPolicyStateField.LeftEdge, out value.LeftEdge) ||
			!TryReadSigned(ref platform, address,
				MuiWindowOpenPolicyStateField.TopEdge, out value.TopEdge)) return false;
		return TryRead(ref platform, address,
			MuiWindowOpenPolicyStateField.CloseGadget, out value.CloseGadget) &&
			TryRead(ref platform, address, MuiWindowOpenPolicyStateField.DepthGadget,
				out value.DepthGadget) &&
			TryRead(ref platform, address, MuiWindowOpenPolicyStateField.DragBar,
				out value.DragBar) &&
			TryRead(ref platform, address, MuiWindowOpenPolicyStateField.SizeGadget,
				out value.SizeGadget) &&
			TryRead(ref platform, address, MuiWindowOpenPolicyStateField.SizeRight,
				out value.SizeRight) &&
			TryRead(ref platform, address, MuiWindowOpenPolicyStateField.AppWindow,
				out value.AppWindow) &&
			TryRead(ref platform, address, MuiWindowOpenPolicyStateField.Backdrop,
				out value.Backdrop) &&
			TryRead(ref platform, address, MuiWindowOpenPolicyStateField.Borderless,
				out value.Borderless) &&
			TryRead(ref platform, address, MuiWindowOpenPolicyStateField.PanelWindow,
				out value.PanelWindow) &&
			TryRead(ref platform, address, MuiWindowOpenPolicyStateField.TabletMessages,
				out value.TabletMessages) &&
			TryRead(ref platform, address,
				MuiWindowOpenPolicyStateField.UseBottomBorderScroller,
				out value.UseBottomBorderScroller) &&
			TryRead(ref platform, address,
				MuiWindowOpenPolicyStateField.UseLeftBorderScroller,
				out value.UseLeftBorderScroller) &&
			TryRead(ref platform, address,
				MuiWindowOpenPolicyStateField.UseRightBorderScroller,
				out value.UseRightBorderScroller);
	}

	private static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowOpenPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiWindowOpenPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
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
			MuiWindowOpenPolicyStateRecord.Size) || value.Magic !=
			MuiWindowOpenPolicyStateRecord.Cookie) return false;
		return Write(ref platform, address, MuiWindowOpenPolicyStateField.Magic,
			value.Magic) &&
			WriteSigned(ref platform, address,
				MuiWindowOpenPolicyStateField.AlternateHeight, value.AlternateHeight) &&
			WriteSigned(ref platform, address,
				MuiWindowOpenPolicyStateField.AlternateWidth, value.AlternateWidth) &&
			WriteSigned(ref platform, address,
				MuiWindowOpenPolicyStateField.AlternateLeftEdge, value.AlternateLeftEdge) &&
			WriteSigned(ref platform, address,
				MuiWindowOpenPolicyStateField.AlternateTopEdge, value.AlternateTopEdge) &&
			WriteSigned(ref platform, address, MuiWindowOpenPolicyStateField.Height,
				value.Height) &&
			WriteSigned(ref platform, address, MuiWindowOpenPolicyStateField.Width,
				value.Width) &&
			WriteSigned(ref platform, address, MuiWindowOpenPolicyStateField.LeftEdge,
				value.LeftEdge) &&
			WriteSigned(ref platform, address, MuiWindowOpenPolicyStateField.TopEdge,
				value.TopEdge) &&
			Write(ref platform, address, MuiWindowOpenPolicyStateField.CloseGadget,
				value.CloseGadget) &&
			Write(ref platform, address, MuiWindowOpenPolicyStateField.DepthGadget,
				value.DepthGadget) &&
			Write(ref platform, address, MuiWindowOpenPolicyStateField.DragBar,
				value.DragBar) &&
			Write(ref platform, address, MuiWindowOpenPolicyStateField.SizeGadget,
				value.SizeGadget) &&
			Write(ref platform, address, MuiWindowOpenPolicyStateField.SizeRight,
				value.SizeRight) &&
			Write(ref platform, address, MuiWindowOpenPolicyStateField.AppWindow,
				value.AppWindow) &&
			Write(ref platform, address, MuiWindowOpenPolicyStateField.Backdrop,
				value.Backdrop) &&
			Write(ref platform, address, MuiWindowOpenPolicyStateField.Borderless,
				value.Borderless) &&
			Write(ref platform, address, MuiWindowOpenPolicyStateField.PanelWindow,
				value.PanelWindow) &&
			Write(ref platform, address, MuiWindowOpenPolicyStateField.TabletMessages,
				value.TabletMessages) &&
			Write(ref platform, address,
				MuiWindowOpenPolicyStateField.UseBottomBorderScroller,
				value.UseBottomBorderScroller) &&
			Write(ref platform, address,
				MuiWindowOpenPolicyStateField.UseLeftBorderScroller,
				value.UseLeftBorderScroller) &&
			Write(ref platform, address,
				MuiWindowOpenPolicyStateField.UseRightBorderScroller,
				value.UseRightBorderScroller);
	}

	private static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowOpenPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiWindowOpenPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, field, value);

	private static bool WriteSigned<TPlatform>(ref TPlatform platform,
		APTR address, MuiWindowOpenPolicyStateField field, int value)
		where TPlatform : struct, IMuiGuestMemory =>
		Write(ref platform, address, field, unchecked((uint)value));
}
