/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiVirtgroupLayoutStateRecord
{
	internal const uint Size = 24;
	internal const uint Cookie = 0x56475250u; // 'VGRP'

	internal uint Magic;
	internal int Width;
	internal int Height;
	internal int Left;
	internal int Top;
	internal uint TryFit;
}

internal enum MuiVirtgroupLayoutField : byte
{
	Magic,
	Width,
	Height,
	Left,
	Top,
	TryFit,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiVirtgroupLayoutFieldCursor
{
	internal APTR Address;
	internal MuiVirtgroupLayoutField Field;
}

internal static class MuiVirtgroupLayoutFieldCursorCodec
{
	private static bool TryResolve(MuiVirtgroupLayoutField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiVirtgroupLayoutField.Magic:
			case MuiVirtgroupLayoutField.Width:
			case MuiVirtgroupLayoutField.Height:
			case MuiVirtgroupLayoutField.Left:
			case MuiVirtgroupLayoutField.Top:
			case MuiVirtgroupLayoutField.TryFit:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiVirtgroupLayoutFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address,
				MuiVirtgroupLayoutStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiVirtgroupLayoutField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiVirtgroupLayoutFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiVirtgroupLayoutField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiVirtgroupLayoutFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiVirtgroupLayoutStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiVirtgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiVirtgroupLayoutStateRecord.Size) ||
			!MuiVirtgroupLayoutFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupLayoutField.Magic, out var magic) ||
			magic != MuiVirtgroupLayoutStateRecord.Cookie ||
			!MuiVirtgroupLayoutFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupLayoutField.Width, out var width) ||
			!MuiVirtgroupLayoutFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupLayoutField.Height, out var height) ||
			!MuiVirtgroupLayoutFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupLayoutField.Left, out var left) ||
			!MuiVirtgroupLayoutFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupLayoutField.Top, out var top) ||
			!MuiVirtgroupLayoutFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupLayoutField.TryFit, out value.TryFit)) return false;
		value.Magic = magic;
		value.Width = unchecked((int)width);
		value.Height = unchecked((int)height);
		value.Left = unchecked((int)left);
		value.Top = unchecked((int)top);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiVirtgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiVirtgroupLayoutStateRecord.Size) ||
			value.Magic != MuiVirtgroupLayoutStateRecord.Cookie) return false;
		return MuiVirtgroupLayoutFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiVirtgroupLayoutField.Magic, value.Magic) &&
			MuiVirtgroupLayoutFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupLayoutField.Width, unchecked((uint)value.Width)) &&
			MuiVirtgroupLayoutFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupLayoutField.Height, unchecked((uint)value.Height)) &&
			MuiVirtgroupLayoutFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupLayoutField.Left, unchecked((uint)value.Left)) &&
			MuiVirtgroupLayoutFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupLayoutField.Top, unchecked((uint)value.Top)) &&
			MuiVirtgroupLayoutFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupLayoutField.TryFit, value.TryFit);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiScrollgroupLayoutStateRecord
{
	internal const uint Size = 32;
	internal const uint Cookie = 0x53475250u; // 'SGRP'

	internal uint Magic;
	internal APTR Contents;
	internal uint FreeHorizontal;
	internal uint FreeVertical;
	internal APTR HorizontalBar;
	internal APTR VerticalBar;
	internal uint NoHorizontalBar;
	internal uint NoVerticalBar;
}

internal enum MuiScrollgroupLayoutField : byte
{
	Magic,
	Contents,
	FreeHorizontal,
	FreeVertical,
	HorizontalBar,
	VerticalBar,
	NoHorizontalBar,
	NoVerticalBar,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiScrollgroupLayoutFieldCursor
{
	internal APTR Address;
	internal MuiScrollgroupLayoutField Field;
}

internal static class MuiScrollgroupLayoutFieldCursorCodec
{
	private static bool TryResolve(MuiScrollgroupLayoutField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiScrollgroupLayoutField.Magic:
			case MuiScrollgroupLayoutField.Contents:
			case MuiScrollgroupLayoutField.FreeHorizontal:
			case MuiScrollgroupLayoutField.FreeVertical:
			case MuiScrollgroupLayoutField.HorizontalBar:
			case MuiScrollgroupLayoutField.VerticalBar:
			case MuiScrollgroupLayoutField.NoHorizontalBar:
			case MuiScrollgroupLayoutField.NoVerticalBar:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiScrollgroupLayoutFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address,
				MuiScrollgroupLayoutStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiScrollgroupLayoutField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiScrollgroupLayoutFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiScrollgroupLayoutField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiScrollgroupLayoutFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiScrollgroupLayoutStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiScrollgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiScrollgroupLayoutStateRecord.Size) ||
			!MuiScrollgroupLayoutFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupLayoutField.Magic, out var magic) ||
			magic != MuiScrollgroupLayoutStateRecord.Cookie ||
			!MuiScrollgroupLayoutFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiScrollgroupLayoutField.Contents, out var contents) ||
			!MuiScrollgroupLayoutFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiScrollgroupLayoutField.FreeHorizontal, out value.FreeHorizontal) ||
			!MuiScrollgroupLayoutFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiScrollgroupLayoutField.FreeVertical, out value.FreeVertical) ||
			!MuiScrollgroupLayoutFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiScrollgroupLayoutField.HorizontalBar, out var horizontalBar) ||
			!MuiScrollgroupLayoutFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiScrollgroupLayoutField.VerticalBar, out var verticalBar) ||
			!MuiScrollgroupLayoutFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiScrollgroupLayoutField.NoHorizontalBar, out value.NoHorizontalBar) ||
			!MuiScrollgroupLayoutFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiScrollgroupLayoutField.NoVerticalBar, out value.NoVerticalBar))
			return false;
		value.Magic = magic;
		value.Contents = APTR.FromPointer(contents);
		value.HorizontalBar = APTR.FromPointer(horizontalBar);
		value.VerticalBar = APTR.FromPointer(verticalBar);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiScrollgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiScrollgroupLayoutStateRecord.Size) ||
			value.Magic != MuiScrollgroupLayoutStateRecord.Cookie) return false;
		return MuiScrollgroupLayoutFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiScrollgroupLayoutField.Magic, value.Magic) &&
			MuiScrollgroupLayoutFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupLayoutField.Contents, value.Contents.Raw) &&
			MuiScrollgroupLayoutFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupLayoutField.FreeHorizontal, value.FreeHorizontal) &&
			MuiScrollgroupLayoutFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupLayoutField.FreeVertical, value.FreeVertical) &&
			MuiScrollgroupLayoutFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupLayoutField.HorizontalBar, value.HorizontalBar.Raw) &&
			MuiScrollgroupLayoutFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupLayoutField.VerticalBar, value.VerticalBar.Raw) &&
			MuiScrollgroupLayoutFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupLayoutField.NoHorizontalBar, value.NoHorizontalBar) &&
			MuiScrollgroupLayoutFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupLayoutField.NoVerticalBar, value.NoVerticalBar);
	}
}

public static class MuiBalanceCore
{
	public static bool AskMinMax<TPlatform>(ref TPlatform platform, APTR storage,
		bool horizontal) where TPlatform : struct, IMuiGuestMemory
	{
		MuiMinMaxValues values = default;
		values.MinWidth = horizontal ? (short)4 : (short)0;
		values.MinHeight = horizontal ? (short)0 : (short)4;
		values.MaxWidth = horizontal ? (short)4 : (short)10000;
		values.MaxHeight = horizontal ? (short)10000 : (short)4;
		values.DefWidth = values.MinWidth;
		values.DefHeight = values.MinHeight;
		return MuiAreaLayoutCore.WriteMinMax(ref platform, storage, values);
	}

	public static bool ResizeAdjacent<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, APTR balance, int delta, bool horizontal)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var index = 0;
		while (index < 65535)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			if (child.IsNull) return false;
			if (child.Raw == balance.Raw) break;
			index++;
		}
		if (index == 0) return false;
		var previous = MuiFamilyCore.GetChild(ref platform, state, group, index - 1,
			APTR.Null);
		var next = MuiFamilyCore.GetChild(ref platform, state, group, index + 1,
			APTR.Null);
		if (next.IsNull) return false;
		if (!MuiAreaLayoutCore.TryReadGeometryState(ref platform, state, previous,
			out var beforeGeometry) || !MuiAreaLayoutCore.TryReadGeometryState(
			ref platform, state, next, out var afterGeometry)) return false;
		var beforeExtent = horizontal ? unchecked((uint)beforeGeometry.Width) :
			unchecked((uint)beforeGeometry.Height);
		var afterExtent = horizontal ? unchecked((uint)afterGeometry.Width) :
			unchecked((uint)afterGeometry.Height);
		var before = unchecked((int)beforeExtent) + delta;
		var after = unchecked((int)afterExtent) - delta;
		var beforeMinMax = MuiAreaLayoutCore.ComputeMinMax(ref platform, state,
			previous);
		var afterMinMax = MuiAreaLayoutCore.ComputeMinMax(ref platform, state, next);
		var beforeMin = horizontal ? beforeMinMax.MinWidth : beforeMinMax.MinHeight;
		var afterMin = horizontal ? afterMinMax.MinWidth : afterMinMax.MinHeight;
		if (before < beforeMin || after < afterMin) return false;
		var beforeLeft = beforeGeometry.Left;
		var beforeTop = beforeGeometry.Top;
		var afterLeft = afterGeometry.Left;
		var afterTop = afterGeometry.Top;
		var beforeWidth = beforeGeometry.Width;
		var beforeHeight = beforeGeometry.Height;
		var afterWidth = afterGeometry.Width;
		var afterHeight = afterGeometry.Height;
		if (horizontal)
		{
			afterLeft += delta;
			beforeWidth = before;
			afterWidth = after;
		}
		else
		{
			afterTop += delta;
			beforeHeight = before;
			afterHeight = after;
		}
		return MuiAreaLayoutCore.Layout(ref platform, state, previous, beforeLeft,
			beforeTop, beforeWidth, beforeHeight) && MuiAreaLayoutCore.Layout(
			ref platform, state, next, afterLeft, afterTop, afterWidth, afterHeight);
	}

}

public static class MuiRegisterCore
{
	private const uint PageMode = 0x80421A5F;
	private const uint ActivePage = 0x80424199;

	public static bool Initialize<TPlatform>(ref TPlatform platform, APTR state,
		APTR register) where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, register, PageMode,
			1, false) && SetActive(ref platform, state, register, 0);

	public static bool SetActive<TPlatform>(ref TPlatform platform, APTR state,
		APTR register, int page) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var target = page;
		var count = Count(ref platform, state, register);
		if (count == 0) return false;
		if (target == -1) target = count - 1;
		else if (target == -2)
			target = Previous(ref platform, state, register, count);
		else if (target == -3 || target == -4)
			target = Next(ref platform, state, register, count);
		if (target < 0 || target >= count) return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, register,
			ActivePage, unchecked((uint)target), true);
	}

	internal static int Count<TPlatform>(ref TPlatform platform, APTR state,
		APTR group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = 0;
		while (count < 65535 && MuiFamilyCore.GetChild(ref platform, state, group,
			count, APTR.Null).IsNotNull) count++;
		return count;
	}

	private static int Previous<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, int count) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var active = Active(ref platform, state, group);
		return active <= 0 ? count - 1 : active - 1;
	}

	private static int Next<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, int count) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var active = Active(ref platform, state, group) + 1;
		return active >= count ? 0 : active;
	}

	private static int Active<TPlatform>(ref TPlatform platform, APTR state,
		APTR group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		uint value;
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, group, ActivePage,
			out value);
		return unchecked((int)value);
	}
}

public static class MuiSelectgroupCore
{
	private const uint Selected = 0x80421788;

	public static bool SetActive<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, int selection) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var target = selection;
		var count = MuiRegisterCore.Count(ref platform, state, group);
		if (count == 0) return false;
		uint currentRaw;
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, group, Selected,
			out currentRaw);
		var current = unchecked((int)currentRaw);
		if (target == -1) target = current + 1 >= count ? 0 : current + 1;
		else if (target == -2)
			target = current <= 0 ? count - 1 : current - 1;
		if (target < 0 || target >= count) return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, group,
			Selected, unchecked((uint)target), true) &&
			MuiRegisterCore.SetActive(ref platform, state, group, target);
	}
}

public static class MuiScrollgroupCore
{
	private const uint Contents = 0x80421261;
	private const uint FreeHorizontal = 0x804292F3;
	private const uint FreeVertical = 0x804224F2;
	private const uint HorizontalBar = 0x8042B63D;
	private const uint VerticalBar = 0x8042CDC0;
	private const uint NoHorizontalBar = 0x8042CAB1;
	private const uint NoVerticalBar = 0x804264C3;
	private const uint LayoutStateKey = 0x0D100012u;
	private const int BarExtent = 12;

	public static bool Layout<TPlatform>(ref TPlatform platform, APTR state,
		APTR scrollgroup, int left, int top, int width, int height)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!PublishLayoutState(ref platform, state, scrollgroup,
			out var layoutState)) return false;
		var contents = layoutState.Contents;
		if (contents.IsNull) return false;
		var horizontalBar = layoutState.HorizontalBar;
		var verticalBar = layoutState.VerticalBar;
		var noHorizontal = layoutState.NoHorizontalBar;
		var noVertical = layoutState.NoVerticalBar;
		var viewportWidth = width - (verticalBar.IsNotNull && noVertical == 0 ?
			BarExtent : 0);
		var viewportHeight = height - (horizontalBar.IsNotNull && noHorizontal == 0 ?
			BarExtent : 0);
		if (viewportWidth < 0) viewportWidth = 0;
		if (viewportHeight < 0) viewportHeight = 0;
		var minMax = MuiGroupLayoutCore.ComputeMinMax(ref platform, state, contents);
		var contentWidth = layoutState.FreeHorizontal != 0 ?
			Larger(viewportWidth, minMax.DefWidth) : viewportWidth;
		var contentHeight = layoutState.FreeVertical != 0 ?
			Larger(viewportHeight, minMax.DefHeight) : viewportHeight;
		if (!MuiGroupLayoutCore.Layout(ref platform, state, contents, left, top,
			contentWidth, contentHeight)) return false;
		if (horizontalBar.IsNotNull && noHorizontal == 0 &&
			!MuiAreaLayoutCore.Layout(ref platform, state, horizontalBar, left,
				top + viewportHeight, viewportWidth, BarExtent)) return false;
		if (verticalBar.IsNotNull && noVertical == 0 &&
			!MuiAreaLayoutCore.Layout(ref platform, state, verticalBar,
				left + viewportWidth, top, BarExtent, viewportHeight)) return false;
		return MuiAreaLayoutCore.Layout(ref platform, state, scrollgroup, left, top,
			width, height);
	}

	internal static bool TryGetLayoutState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiScrollgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			LayoutStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			LayoutStateKey) != unchecked((int)MuiScrollgroupLayoutStateRecord.Size))
			return false;
		return MuiScrollgroupLayoutStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool PublishLayoutState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiScrollgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			LayoutStateKey);
		if (TryGetLayoutState(ref platform, state, obj, out value))
		{
			value.Contents = Pointer(ref platform, state, obj, Contents);
			value.FreeHorizontal = Read(ref platform, state, obj, FreeHorizontal);
			value.FreeVertical = Read(ref platform, state, obj, FreeVertical);
			value.HorizontalBar = Pointer(ref platform, state, obj, HorizontalBar);
			value.VerticalBar = Pointer(ref platform, state, obj, VerticalBar);
			value.NoHorizontalBar = Read(ref platform, state, obj,
				NoHorizontalBar);
			value.NoVerticalBar = Read(ref platform, state, obj, NoVerticalBar);
			return MuiScrollgroupLayoutStateRecordCodec.Write(ref platform, block,
				value);
		}

		value = default;
		value.Magic = MuiScrollgroupLayoutStateRecord.Cookie;
		value.Contents = Pointer(ref platform, state, obj, Contents);
		value.FreeHorizontal = Read(ref platform, state, obj, FreeHorizontal);
		value.FreeVertical = Read(ref platform, state, obj, FreeVertical);
		value.HorizontalBar = Pointer(ref platform, state, obj, HorizontalBar);
		value.VerticalBar = Pointer(ref platform, state, obj, VerticalBar);
		value.NoHorizontalBar = Read(ref platform, state, obj, NoHorizontalBar);
		value.NoVerticalBar = Read(ref platform, state, obj, NoVerticalBar);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiScrollgroupLayoutStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiScrollgroupLayoutStateRecord.Size);
		var written = MuiScrollgroupLayoutStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			LayoutStateKey, scratch,
			unchecked((int)MuiScrollgroupLayoutStateRecord.Size));
		platform.Clear(scratch, MuiScrollgroupLayoutStateRecord.Size);
		platform.Free(scratch, MuiScrollgroupLayoutStateRecord.Size);
		return added;
	}

	private static APTR Pointer<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute) where TPlatform : struct, IMuiHeadlessPlatform =>
		APTR.FromPointer(Read(ref platform, state, obj, attribute));

	private static uint Read<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute) where TPlatform : struct, IMuiHeadlessPlatform
	{
		uint value;
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out value);
		return value;
	}

	private static int Larger(int left, short right) =>
		left > right ? left : right;
}

public static class MuiVirtgroupCore
{
	private const uint VirtualWidth = 0x80427C49;
	private const uint VirtualHeight = 0x80423038;
	private const uint VirtualLeft = 0x80429371;
	private const uint VirtualTop = 0x80425200;
	private const uint TryFit = 0x80429427;
	private const uint LayoutStateKey = 0x0D100011u;

	public static bool Layout<TPlatform>(ref TPlatform platform, APTR state,
		APTR virtgroup, int left, int top, int viewportWidth, int viewportHeight)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!PublishLayoutState(ref platform, state, virtgroup,
			out var layoutState)) return false;
		var width = layoutState.Width;
		var height = layoutState.Height;
		var scrollLeft = layoutState.Left;
		var scrollTop = layoutState.Top;
		if (layoutState.TryFit != 0)
		{
			if (width < viewportWidth) width = viewportWidth;
			if (height < viewportHeight) height = viewportHeight;
		}
		if (width < 0 || height < 0 || scrollLeft < 0 || scrollTop < 0) return false;
		if (scrollLeft > width - Smaller(width, viewportWidth))
			scrollLeft = width - Smaller(width, viewportWidth);
		if (scrollTop > height - Smaller(height, viewportHeight))
			scrollTop = height - Smaller(height, viewportHeight);
		return MuiGroupLayoutCore.Layout(ref platform, state, virtgroup,
			left - scrollLeft, top - scrollTop, width, height);
	}

	internal static bool TryGetLayoutState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiVirtgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			LayoutStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			LayoutStateKey) != unchecked((int)MuiVirtgroupLayoutStateRecord.Size))
			return false;
		return MuiVirtgroupLayoutStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool PublishLayoutState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiVirtgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			LayoutStateKey);
		if (TryGetLayoutState(ref platform, state, obj, out value))
		{
			value.Width = unchecked((int)Read(ref platform, state, obj,
				VirtualWidth));
			value.Height = unchecked((int)Read(ref platform, state, obj,
				VirtualHeight));
			value.Left = unchecked((int)Read(ref platform, state, obj,
				VirtualLeft));
			value.Top = unchecked((int)Read(ref platform, state, obj,
				VirtualTop));
			value.TryFit = Read(ref platform, state, obj, TryFit);
			return MuiVirtgroupLayoutStateRecordCodec.Write(ref platform, block,
				value);
		}

		value = default;
		value.Magic = MuiVirtgroupLayoutStateRecord.Cookie;
		value.Width = unchecked((int)Read(ref platform, state, obj, VirtualWidth));
		value.Height = unchecked((int)Read(ref platform, state, obj,
			VirtualHeight));
		value.Left = unchecked((int)Read(ref platform, state, obj, VirtualLeft));
		value.Top = unchecked((int)Read(ref platform, state, obj, VirtualTop));
		value.TryFit = Read(ref platform, state, obj, TryFit);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiVirtgroupLayoutStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiVirtgroupLayoutStateRecord.Size);
		var written = MuiVirtgroupLayoutStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			LayoutStateKey, scratch,
			unchecked((int)MuiVirtgroupLayoutStateRecord.Size));
		platform.Clear(scratch, MuiVirtgroupLayoutStateRecord.Size);
		platform.Free(scratch, MuiVirtgroupLayoutStateRecord.Size);
		return added;
	}

	private static uint Read<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute) where TPlatform : struct, IMuiHeadlessPlatform
	{
		uint value;
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out value);
		return value;
	}

	private static int Smaller(int left, int right) =>
		left < right ? left : right;
}
