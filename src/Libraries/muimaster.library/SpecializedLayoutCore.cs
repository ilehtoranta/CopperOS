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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint WidthOffset = 4;
	internal const uint HeightOffset = 8;
	internal const uint LeftOffset = 12;
	internal const uint TopOffset = 16;
	internal const uint TryFitOffset = 20;
	internal const uint Cookie = 0x56475250u; // 'VGRP'

	internal uint Magic;
	internal int Width;
	internal int Height;
	internal int Left;
	internal int Top;
	internal uint TryFit;
}

internal static class MuiVirtgroupLayoutStateValidation
{
	// Layout state is the normalized projection consumed by LayoutCore.  Unlike
	// the public policy record, it contains only the non-negative geometry that
	// can be admitted to child layout; VirtualLeft/Top remain signed at the
	// public policy boundary and are rejected here when they cannot be laid out.
	internal static bool IsValid(MuiVirtgroupLayoutStateRecord value) =>
		value.Magic == MuiVirtgroupLayoutStateRecord.Cookie &&
		value.Width >= 0 && value.Height >= 0 && value.Left >= 0 &&
		value.Top >= 0 && value.TryFit <= 1;
}

internal static class MuiVirtgroupLayoutStateAdmission
{
	internal static bool Validate(MuiVirtgroupLayoutStateRecord value) =>
		MuiVirtgroupLayoutStateValidation.IsValid(value);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiVirtgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiVirtgroupLayoutFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiVirtgroupLayoutStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Address, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiVirtgroupLayoutField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiVirtgroupLayoutStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiVirtgroupLayoutField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiVirtgroupLayoutStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, field, value);
	}
}

internal static class MuiVirtgroupLayoutStateRecordMemoryCodec
{
	private static bool TryResolve(MuiVirtgroupLayoutField field, out uint offset)
	{
		if (field == MuiVirtgroupLayoutField.Magic)
			offset = MuiVirtgroupLayoutStateRecord.MagicOffset;
		else if (field == MuiVirtgroupLayoutField.Width)
			offset = MuiVirtgroupLayoutStateRecord.WidthOffset;
		else if (field == MuiVirtgroupLayoutField.Height)
			offset = MuiVirtgroupLayoutStateRecord.HeightOffset;
		else if (field == MuiVirtgroupLayoutField.Left)
			offset = MuiVirtgroupLayoutStateRecord.LeftOffset;
		else if (field == MuiVirtgroupLayoutField.Top)
			offset = MuiVirtgroupLayoutStateRecord.TopOffset;
		else if (field == MuiVirtgroupLayoutField.TryFit)
			offset = MuiVirtgroupLayoutStateRecord.TryFitOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiVirtgroupLayoutField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiVirtgroupLayoutStateRecord.Size) &&
			platform.IsMapped(address, MuiVirtgroupLayoutStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiVirtgroupLayoutField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiVirtgroupLayoutField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiVirtgroupLayoutStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiVirtgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiVirtgroupLayoutStateRecord.Size) ||
			!MuiVirtgroupLayoutStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupLayoutField.Magic, out var magic) ||
			!MuiVirtgroupLayoutStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupLayoutField.Width, out var width) ||
			!MuiVirtgroupLayoutStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupLayoutField.Height, out var height) ||
			!MuiVirtgroupLayoutStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupLayoutField.Left, out var left) ||
			!MuiVirtgroupLayoutStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupLayoutField.Top, out var top) ||
			!MuiVirtgroupLayoutStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupLayoutField.TryFit, out value.TryFit)) return false;
		value.Magic = magic;
		value.Width = unchecked((int)width);
		value.Height = unchecked((int)height);
		value.Left = unchecked((int)left);
		value.Top = unchecked((int)top);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiVirtgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiVirtgroupLayoutStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiVirtgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiVirtgroupLayoutStateRecord.Size) ||
			!MuiVirtgroupLayoutStateAdmission.Validate(value)) return false;
		return MuiVirtgroupLayoutStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiVirtgroupLayoutField.Magic, value.Magic) &&
			MuiVirtgroupLayoutStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupLayoutField.Width, unchecked((uint)value.Width)) &&
			MuiVirtgroupLayoutStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupLayoutField.Height, unchecked((uint)value.Height)) &&
			MuiVirtgroupLayoutStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupLayoutField.Left, unchecked((uint)value.Left)) &&
			MuiVirtgroupLayoutStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupLayoutField.Top, unchecked((uint)value.Top)) &&
			MuiVirtgroupLayoutStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupLayoutField.TryFit, value.TryFit);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiScrollgroupLayoutStateRecord
{
	internal const uint Size = 32;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ContentsOffset = 4;
	internal const uint FreeHorizontalOffset = 8;
	internal const uint FreeVerticalOffset = 12;
	internal const uint HorizontalBarOffset = 16;
	internal const uint VerticalBarOffset = 20;
	internal const uint NoHorizontalBarOffset = 24;
	internal const uint NoVerticalBarOffset = 28;
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

internal static class MuiScrollgroupLayoutStateValidation
{
	internal static bool IsValid<TPlatform>(ref TPlatform platform,
		MuiScrollgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiScrollgroupLayoutStateRecord.Cookie &&
		(value.Contents.IsNull || platform.IsMapped(value.Contents, 4)) &&
		(value.HorizontalBar.IsNull || platform.IsMapped(value.HorizontalBar, 4)) &&
		(value.VerticalBar.IsNull || platform.IsMapped(value.VerticalBar, 4)) &&
		value.FreeHorizontal <= 1 && value.FreeVertical <= 1 &&
		value.NoHorizontalBar <= 1 && value.NoVerticalBar <= 1;
}

internal static class MuiScrollgroupLayoutStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiScrollgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiScrollgroupLayoutStateValidation.IsValid(ref platform, value);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiScrollgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiScrollgroupLayoutFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiScrollgroupLayoutStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Address, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiScrollgroupLayoutField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiScrollgroupLayoutStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiScrollgroupLayoutField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiScrollgroupLayoutStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, field, value);
	}
}

internal static class MuiScrollgroupLayoutStateRecordMemoryCodec
{
	private static bool TryResolve(MuiScrollgroupLayoutField field, out uint offset)
	{
		if (field == MuiScrollgroupLayoutField.Magic)
			offset = MuiScrollgroupLayoutStateRecord.MagicOffset;
		else if (field == MuiScrollgroupLayoutField.Contents)
			offset = MuiScrollgroupLayoutStateRecord.ContentsOffset;
		else if (field == MuiScrollgroupLayoutField.FreeHorizontal)
			offset = MuiScrollgroupLayoutStateRecord.FreeHorizontalOffset;
		else if (field == MuiScrollgroupLayoutField.FreeVertical)
			offset = MuiScrollgroupLayoutStateRecord.FreeVerticalOffset;
		else if (field == MuiScrollgroupLayoutField.HorizontalBar)
			offset = MuiScrollgroupLayoutStateRecord.HorizontalBarOffset;
		else if (field == MuiScrollgroupLayoutField.VerticalBar)
			offset = MuiScrollgroupLayoutStateRecord.VerticalBarOffset;
		else if (field == MuiScrollgroupLayoutField.NoHorizontalBar)
			offset = MuiScrollgroupLayoutStateRecord.NoHorizontalBarOffset;
		else if (field == MuiScrollgroupLayoutField.NoVerticalBar)
			offset = MuiScrollgroupLayoutStateRecord.NoVerticalBarOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupLayoutField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiScrollgroupLayoutStateRecord.Size) &&
			platform.IsMapped(address, MuiScrollgroupLayoutStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupLayoutField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupLayoutField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiScrollgroupLayoutStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiScrollgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiScrollgroupLayoutStateRecord.Size) ||
			!MuiScrollgroupLayoutStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupLayoutField.Magic, out var magic) ||
			!MuiScrollgroupLayoutStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
				MuiScrollgroupLayoutField.Contents, out var contents) ||
			!MuiScrollgroupLayoutStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
				MuiScrollgroupLayoutField.FreeHorizontal, out value.FreeHorizontal) ||
			!MuiScrollgroupLayoutStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
				MuiScrollgroupLayoutField.FreeVertical, out value.FreeVertical) ||
			!MuiScrollgroupLayoutStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
				MuiScrollgroupLayoutField.HorizontalBar, out var horizontalBar) ||
			!MuiScrollgroupLayoutStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
				MuiScrollgroupLayoutField.VerticalBar, out var verticalBar) ||
			!MuiScrollgroupLayoutStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
				MuiScrollgroupLayoutField.NoHorizontalBar, out value.NoHorizontalBar) ||
			!MuiScrollgroupLayoutStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
				MuiScrollgroupLayoutField.NoVerticalBar, out value.NoVerticalBar))
			return false;
		value.Magic = magic;
		value.Contents = APTR.FromPointer(contents);
		value.HorizontalBar = APTR.FromPointer(horizontalBar);
		value.VerticalBar = APTR.FromPointer(verticalBar);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiScrollgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiScrollgroupLayoutStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiScrollgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiScrollgroupLayoutStateRecord.Size) ||
			!MuiScrollgroupLayoutStateAdmission.Validate(ref platform, value))
			return false;
		return MuiScrollgroupLayoutStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiScrollgroupLayoutField.Magic, value.Magic) &&
			MuiScrollgroupLayoutStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupLayoutField.Contents, value.Contents.Raw) &&
			MuiScrollgroupLayoutStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupLayoutField.FreeHorizontal, value.FreeHorizontal) &&
			MuiScrollgroupLayoutStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupLayoutField.FreeVertical, value.FreeVertical) &&
			MuiScrollgroupLayoutStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupLayoutField.HorizontalBar, value.HorizontalBar.Raw) &&
			MuiScrollgroupLayoutStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupLayoutField.VerticalBar, value.VerticalBar.Raw) &&
			MuiScrollgroupLayoutStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupLayoutField.NoHorizontalBar, value.NoHorizontalBar) &&
			MuiScrollgroupLayoutStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
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
	public const uint Frame = 0x8042349B;
	public const uint Titles = 0x804297EC;
	private const uint PolicyStateKey = 0x0D100015u;

	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == Frame || attribute == Titles;

	public static bool Initialize<TPlatform>(ref TPlatform platform, APTR state,
		APTR register) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!PublishPolicyState(ref platform, state, register,
			out _)) return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, register,
			PageMode, 1, false) && SetActive(ref platform, state, register, 0);
	}

	internal static bool TryGetAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (!IsPublicGetterAttribute(attribute) ||
			!IsRegisterObject(ref platform, state, obj)) return false;
		if (!PublishPolicyState(ref platform, state, obj, out var policy))
			return false;
		value = attribute == Frame ? policy.Frame : policy.Titles.Raw;
		return true;
	}

	internal static bool TryGetPolicyState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiRegisterPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var policy = default(MuiRegisterPolicyState);
		if (!PublishPolicyState(ref platform, state, obj, out policy)) return false;
		value.Magic = MuiRegisterPolicyStateRecord.Cookie;
		value.Frame = policy.Frame;
		value.Titles = policy.Titles;
		return true;
	}

	internal static bool TrySet<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, uint attribute, uint value, bool notify, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		handled = false;
		if (!IsPublicGetterAttribute(attribute) ||
			!MuiHeadlessObjectCodec.TryRead(ref platform, record,
				out var objectValue) ||
			!IsRegisterObject(ref platform, state, objectValue.Boopsi))
			return false;
		handled = true;
		// Register.Frame and Register.Titles are initializer-only.  The tag
		// application path runs before ObjectInitialized; later writes fail.
		if (MuiHeadlessObjectCore.IsObjectInitialized(ref platform, record))
			return false;
		if (!TryReadPolicyAdmission(ref platform, state, objectValue.Boopsi,
			out _, out _)) return false;
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, attribute, attribute == Frame && value != 0 ? 1u : value,
			false)) return false;
		return true;
	}

	public static bool SetActive<TPlatform>(ref TPlatform platform, APTR state,
		APTR register, int page, bool notify = true)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadPolicyAdmission(ref platform, state, register,
			out _, out _)) return false;
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
			ActivePage, unchecked((uint)target), notify);
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

	private static bool PublishPolicyState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiRegisterPolicyState policy)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		policy = default;
		if (!TryReadPolicyAdmission(ref platform, state, obj,
			out var existing, out var present)) return false;
		var frame = ReadRaw(ref platform, state, obj, Frame, 1) == 0 ? 0u : 1u;
		var titles = APTR.FromPointer(ReadRaw(ref platform, state, obj, Titles, 0));
		if (present)
		{
			existing.Frame = frame;
			existing.Titles = titles;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				PolicyStateKey);
			if (!MuiRegisterPolicyStateRecordCodec.Write(ref platform, block,
				existing)) return false;
		}
		else
		{
			var scratch = MuiHeadlessMemory.Allocate(ref platform,
				MuiRegisterPolicyStateRecord.Size);
			if (scratch.IsNull) return false;
			platform.Clear(scratch, MuiRegisterPolicyStateRecord.Size);
			var value = default(MuiRegisterPolicyStateRecord);
			value.Magic = MuiRegisterPolicyStateRecord.Cookie;
			value.Frame = frame;
			value.Titles = titles;
			var written = MuiRegisterPolicyStateRecordCodec.Write(ref platform,
				scratch, value);
			var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
				obj, PolicyStateKey, scratch,
				unchecked((int)MuiRegisterPolicyStateRecord.Size));
			platform.Clear(scratch, MuiRegisterPolicyStateRecord.Size);
			platform.Free(scratch, MuiRegisterPolicyStateRecord.Size);
			if (!added) return false;
		}
		policy.Frame = frame;
		policy.Titles = titles;
		return true;
	}

	// A present Register policy block is authoritative typed state. Reject a
	// wrong-sized or malformed record before initialization or public getters
	// can rebuild it from raw compatibility attributes.
	private static bool TryReadPolicyAdmission<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiRegisterPolicyStateRecord value,
		out bool present) where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
		{
			present = false;
			return false;
		}
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PolicyStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			PolicyStateKey);
		present = block.IsNotNull || length != 0;
		if (!present) return true;
		if (block.IsNull || length != unchecked((int)
			MuiRegisterPolicyStateRecord.Size) ||
			!MuiRegisterPolicyStateRecordCodec.TryRead(ref platform, block,
				out value)) return false;
		return MuiRegisterPolicyStateAdmission.ValidateLive(ref platform, state,
			obj, value);
	}

	private static uint ReadRaw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint defaultValue)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			attribute, out var value) ? value : defaultValue;

	private static bool IsRegisterObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var classRecord = MuiHeadlessObjectCore.ObjectClassRecord(ref platform,
			state, obj);
		uint depth = 0;
		while (classRecord.IsNotNull && depth++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessClassCodec.TryRead(ref platform, classRecord,
				out var classValue)) return false;
			if (IsRegisterName(ref platform, classValue.Name)) return true;
			if (classValue.Super.IsNull) return false;
			classRecord = FindClassByBoopsi(ref platform, state, classValue.Super);
		}
		return false;
	}

	private static APTR FindClassByBoopsi<TPlatform>(ref TPlatform platform,
		APTR state, APTR boopsi) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return APTR.Null;
		var current = stateValue.Classes;
		uint depth = 0;
		while (current.IsNotNull && depth++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessClassCodec.TryRead(ref platform, current,
				out var classValue)) return APTR.Null;
			if (classValue.Boopsi.Raw == boopsi.Raw) return current;
			current = classValue.Next;
		}
		return APTR.Null;
	}

	private static bool IsRegisterName<TPlatform>(ref TPlatform platform, APTR name)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (name.IsNull || !platform.IsMapped(name, 13)) return false;
		return platform.ReadUInt8(name, 0) == (byte)'R' &&
			platform.ReadUInt8(name, 1) == (byte)'e' &&
			platform.ReadUInt8(name, 2) == (byte)'g' &&
			platform.ReadUInt8(name, 3) == (byte)'i' &&
			platform.ReadUInt8(name, 4) == (byte)'s' &&
			platform.ReadUInt8(name, 5) == (byte)'t' &&
			platform.ReadUInt8(name, 6) == (byte)'e' &&
			platform.ReadUInt8(name, 7) == (byte)'r' &&
			platform.ReadUInt8(name, 8) == (byte)'.' &&
			platform.ReadUInt8(name, 9) == (byte)'m' &&
			platform.ReadUInt8(name, 10) == (byte)'u' &&
			platform.ReadUInt8(name, 11) == (byte)'i' &&
			platform.ReadUInt8(name, 12) == 0;
	}
}

public static class MuiSelectgroupCore
{
	public const uint Active = 0x80421788;
	private const uint PolicyStateKey = 0x0D100016u;

	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == Active;

	public static bool SetActive<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, int selection) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = MuiRegisterCore.Count(ref platform, state, group);
		if (count == 0) return false;
		if (!TryReadCurrent(ref platform, state, group, count,
			out var current)) return false;
		var target = ResolveSelection(selection, current, count);
		if (target < 0 || target >= count) return false;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, group);
		return record.IsNotNull && ApplySelection(ref platform, state, record,
			target, true);
	}

	internal static bool TryGetAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (attribute != Active || !IsSelectgroupObject(ref platform, state, obj))
			return false;
		if (!PublishActiveState(ref platform, state, obj, out var policy))
			return false;
		value = policy.Active;
		return true;
	}

	internal static bool TryGetPolicyState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiSelectgroupActiveStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (!PublishActiveState(ref platform, state, obj, out var policy))
			return false;
		value.Magic = MuiSelectgroupActiveStateRecord.Cookie;
		value.Active = policy.Active;
		return true;
	}

	internal static bool TrySet<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, uint attribute, uint value, bool notify, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		handled = false;
		if (attribute != Active ||
			!MuiHeadlessObjectCodec.TryRead(ref platform, record,
				out var objectValue) ||
			!IsSelectgroupObject(ref platform, state, objectValue.Boopsi))
			return false;
		handled = true;
		// Selectgroup.Active accepts the -1/-2 cycling selectors at runtime;
		// construction tags only seed the raw value before children exist.
		if (!MuiHeadlessObjectCore.IsObjectInitialized(ref platform, record))
			return MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
				record, attribute, value, false);
		var count = MuiRegisterCore.Count(ref platform, state, objectValue.Boopsi);
		if (count == 0) return false;
		if (!TryReadCurrent(ref platform, state, objectValue.Boopsi, count,
			out var current)) return false;
		var target = ResolveSelection(unchecked((int)value), current, count);
		if (target < 0 || target >= count) return false;
		return ApplySelection(ref platform, state, record, target, notify);
	}

	private static bool ApplySelection<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, int target, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return false;
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
			Active, unchecked((uint)target), notify)) return false;
		return MuiRegisterCore.SetActive(ref platform, state, objectValue.Boopsi,
			target, notify);
	}

	private static bool TryReadCurrent<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int count, out int current)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		current = 0;
		if (!IsSelectgroupObject(ref platform, state, obj))
		{
			current = NormalizeCurrent(ReadRaw(ref platform, state, obj,
				Active, 0), count);
			return true;
		}
		// Once a typed policy block is present it is authoritative.  Do not
		// recover a malformed record by reading the raw Active scalar.
		if (!TryReadActiveStateAdmission(ref platform, state, obj,
			out _, out _) || !PublishActiveState(ref platform, state, obj,
			out var policy)) return false;
		current = NormalizeCurrent(policy.Active, count);
		return true;
	}

	private static int NormalizeCurrent(uint raw, int count)
	{
		var current = unchecked((int)raw);
		return current < 0 || current >= count ? 0 : current;
	}

	private static int ResolveSelection(int selection, int current, int count)
	{
		if (selection == -1) return current + 1 >= count ? 0 : current + 1;
		if (selection == -2) return current <= 0 ? count - 1 : current - 1;
		return selection;
	}

	private static bool PublishActiveState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiSelectgroupActiveState policy)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		policy = default;
		if (!TryReadActiveStateAdmission(ref platform, state, obj,
			out var existing, out var present)) return false;
		var count = MuiRegisterCore.Count(ref platform, state, obj);
		var raw = ReadRaw(ref platform, state, obj, Active, 0);
		policy.Active = count == 0 ? raw : unchecked((uint)NormalizeCurrent(raw, count));
		if (present)
		{
			existing.Active = policy.Active;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				PolicyStateKey);
			return MuiSelectgroupActiveStateAdmission.ValidateLive(ref platform, state,
				obj, existing) && MuiSelectgroupActiveStateRecordCodec.Write(ref platform,
				block, existing);
		}
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiSelectgroupActiveStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiSelectgroupActiveStateRecord.Size);
		var value = default(MuiSelectgroupActiveStateRecord);
		value.Magic = MuiSelectgroupActiveStateRecord.Cookie;
		value.Active = policy.Active;
		if (!MuiSelectgroupActiveStateAdmission.ValidateLive(ref platform, state, obj,
			value))
		{
			platform.Clear(scratch, MuiSelectgroupActiveStateRecord.Size);
			platform.Free(scratch, MuiSelectgroupActiveStateRecord.Size);
			return false;
		}
		var written = MuiSelectgroupActiveStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			PolicyStateKey, scratch,
			unchecked((int)MuiSelectgroupActiveStateRecord.Size));
		platform.Clear(scratch, MuiSelectgroupActiveStateRecord.Size);
		platform.Free(scratch, MuiSelectgroupActiveStateRecord.Size);
		return added;
	}

	// A non-NULL Selectgroup policy block is authoritative typed state.  A
	// wrong-sized or malformed present block is rejected rather than treated as
	// permission to rebuild the record from the raw Active attribute.
	private static bool TryReadActiveStateAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiSelectgroupActiveStateRecord value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
		{
			present = false;
			return false;
		}
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PolicyStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			PolicyStateKey);
		present = block.IsNotNull || length != 0;
		if (!present) return true;
		return !block.IsNull && length == unchecked((int)
			MuiSelectgroupActiveStateRecord.Size) &&
			MuiSelectgroupActiveStateRecordCodec.TryReadStructural(ref platform, block,
				out value) && MuiSelectgroupActiveStateAdmission.ValidateLive(ref platform,
					state, obj, value);
	}

	private static uint ReadRaw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint defaultValue)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			attribute, out var value) ? value : defaultValue;

	private static bool IsSelectgroupObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var classRecord = MuiHeadlessObjectCore.ObjectClassRecord(ref platform,
			state, obj);
		uint depth = 0;
		while (classRecord.IsNotNull && depth++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessClassCodec.TryRead(ref platform, classRecord,
				out var classValue)) return false;
			if (IsSelectgroupName(ref platform, classValue.Name)) return true;
			if (classValue.Super.IsNull) return false;
			classRecord = FindClassByBoopsi(ref platform, state, classValue.Super);
		}
		return false;
	}

	private static APTR FindClassByBoopsi<TPlatform>(ref TPlatform platform,
		APTR state, APTR boopsi) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return APTR.Null;
		var current = stateValue.Classes;
		uint depth = 0;
		while (current.IsNotNull && depth++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessClassCodec.TryRead(ref platform, current,
				out var classValue)) return APTR.Null;
			if (classValue.Boopsi.Raw == boopsi.Raw) return current;
			current = classValue.Next;
		}
		return APTR.Null;
	}

	private static bool IsSelectgroupName<TPlatform>(ref TPlatform platform,
		APTR name) where TPlatform : struct, IMuiGuestMemory
	{
		if (name.IsNull || !platform.IsMapped(name, 16)) return false;
		return platform.ReadUInt8(name, 0) == (byte)'S' &&
			platform.ReadUInt8(name, 1) == (byte)'e' &&
			platform.ReadUInt8(name, 2) == (byte)'l' &&
			platform.ReadUInt8(name, 3) == (byte)'e' &&
			platform.ReadUInt8(name, 4) == (byte)'c' &&
			platform.ReadUInt8(name, 5) == (byte)'t' &&
			platform.ReadUInt8(name, 6) == (byte)'g' &&
			platform.ReadUInt8(name, 7) == (byte)'r' &&
			platform.ReadUInt8(name, 8) == (byte)'o' &&
			platform.ReadUInt8(name, 9) == (byte)'u' &&
			platform.ReadUInt8(name, 10) == (byte)'p' &&
			platform.ReadUInt8(name, 11) == (byte)'.' &&
			platform.ReadUInt8(name, 12) == (byte)'m' &&
			platform.ReadUInt8(name, 13) == (byte)'u' &&
			platform.ReadUInt8(name, 14) == (byte)'i' &&
			platform.ReadUInt8(name, 15) == 0;
	}
}

public static class MuiScrollgroupCore
{
	public const uint AutoBars = 0x8042F50E;
	public const uint Contents = 0x80421261;
	public const uint FreeHorizontal = 0x804292F3;
	public const uint FreeVertical = 0x804224F2;
	public const uint HorizontalBar = 0x8042B63D;
	public const uint NoHorizontalBar = 0x8042CAB1;
	public const uint NoVerticalBar = 0x804264C3;
	public const uint UseWindowBorder = 0x804284C1;
	public const uint VerticalBar = 0x8042CDC0;
	private const uint LayoutStateKey = 0x0D100012u;
	private const uint PolicyStateKey = 0x0D100017u;
	private const uint ViewportStateKey = 0x0D100019u;
	private const uint BorderScrollerStateKey = 0x0D10001Cu;
	private const int BarExtent = 12;
	private const int KeyUp = 2;
	private const int KeyDown = 3;
	private const int KeyPageUp = 4;
	private const int KeyPageDown = 5;
	private const int KeyLeft = 8;
	private const int KeyRight = 9;
	private const int KeyHome = 12;
	private const int KeyEnd = 13;

	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == AutoBars || attribute == Contents ||
		attribute == HorizontalBar || attribute == VerticalBar ||
		attribute == NoHorizontalBar || attribute == NoVerticalBar;

	internal static bool TryGetAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (!IsPublicGetterAttribute(attribute) ||
			!IsScrollgroupObject(ref platform, state, obj) ||
			!PublishPolicyState(ref platform, state, obj, out var policy))
			return false;
		value = attribute == AutoBars ? policy.AutoBars :
			attribute == Contents ? policy.Contents.Raw :
			attribute == HorizontalBar ? policy.HorizontalBar.Raw :
			attribute == VerticalBar ? policy.VerticalBar.Raw :
			attribute == NoHorizontalBar ? policy.NoHorizontalBar :
			policy.NoVerticalBar;
		return true;
	}

	internal static bool IsObject<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		IsScrollgroupObject(ref platform, state, obj);

	internal static bool TryGetPolicyState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiScrollgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (!IsScrollgroupObject(ref platform, state, obj) ||
			!PublishPolicyState(ref platform, state, obj, out var policy))
			return false;
		value.Magic = MuiScrollgroupPolicyStateRecord.Cookie;
		value.Contents = policy.Contents;
		value.FreeHorizontal = policy.FreeHorizontal;
		value.FreeVertical = policy.FreeVertical;
		value.HorizontalBar = policy.HorizontalBar;
		value.VerticalBar = policy.VerticalBar;
		value.NoHorizontalBar = policy.NoHorizontalBar;
		value.NoVerticalBar = policy.NoVerticalBar;
		value.AutoBars = policy.AutoBars;
		value.UseWindowBorder = policy.UseWindowBorder;
		return true;
	}

	internal static bool TryGetBorderScrollerState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiScrollgroupBorderScrollerStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			BorderScrollerStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			BorderScrollerStateKey);
		return length == unchecked((int)
			MuiScrollgroupBorderScrollerStateRecord.Size) &&
			MuiScrollgroupBorderScrollerStateRecordCodec.TryReadStructural(ref platform,
				block, out value) &&
			MuiScrollgroupBorderScrollerStateAdmission.ValidateLive(ref platform,
				state, obj, value);
	}

	internal static bool TrySet<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, uint attribute, uint value, bool notify, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		handled = false;
		if (!IsScrollgroupAttribute(attribute) ||
			!MuiHeadlessObjectCodec.TryRead(ref platform, record,
				out var objectValue) ||
			!IsScrollgroupObject(ref platform, state, objectValue.Boopsi))
			return false;
		handled = true;
		var initialized = MuiHeadlessObjectCore.IsObjectInitialized(ref platform,
			record);
		if (!initialized && !IsConstructionAttribute(attribute)) return false;
		if (initialized && !IsRuntimeSetAttribute(attribute)) return false;
		if (!TryReadPolicyAdmission(ref platform, state, objectValue.Boopsi,
			out _, out _)) return false;
		var normalized = NormalizeBoolean(attribute, value);
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, attribute, normalized, initialized && notify)) return false;
		if (!initialized) return true;
		return PublishPolicyState(ref platform, state, objectValue.Boopsi, out _);
	}

	private static bool IsScrollgroupAttribute(uint attribute) =>
		attribute == AutoBars || attribute == Contents ||
		attribute == FreeHorizontal || attribute == FreeVertical ||
		attribute == HorizontalBar || attribute == NoHorizontalBar ||
		attribute == NoVerticalBar || attribute == UseWindowBorder ||
		attribute == VerticalBar;

	private static bool IsConstructionAttribute(uint attribute) =>
		attribute == AutoBars || attribute == Contents ||
		attribute == FreeHorizontal || attribute == FreeVertical ||
		attribute == NoHorizontalBar || attribute == NoVerticalBar ||
		attribute == UseWindowBorder;

	private static bool IsRuntimeSetAttribute(uint attribute) =>
		attribute == AutoBars || attribute == NoHorizontalBar ||
		attribute == NoVerticalBar;

	private static uint NormalizeBoolean(uint attribute, uint value) =>
		attribute == AutoBars || attribute == FreeHorizontal ||
		attribute == FreeVertical || attribute == NoHorizontalBar ||
		attribute == NoVerticalBar || attribute == UseWindowBorder ?
		(value == 0 ? 0u : 1u) : value;

	private static bool PublishPolicyState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiScrollgroupPolicyState policy)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		policy = default;
		if (!TryReadPolicyAdmission(ref platform, state, obj,
			out var existing, out var present)) return false;
		policy.Contents = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			Contents, 0));
		policy.FreeHorizontal = ReadRaw(ref platform, state, obj,
			FreeHorizontal, 0) == 0 ? 0u : 1u;
		policy.FreeVertical = ReadRaw(ref platform, state, obj,
			FreeVertical, 0) == 0 ? 0u : 1u;
		policy.HorizontalBar = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			HorizontalBar, 0));
		policy.VerticalBar = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			VerticalBar, 0));
		policy.NoHorizontalBar = ReadRaw(ref platform, state, obj,
			NoHorizontalBar, 0) == 0 ? 0u : 1u;
		policy.NoVerticalBar = ReadRaw(ref platform, state, obj,
			NoVerticalBar, 0) == 0 ? 0u : 1u;
		policy.AutoBars = ReadRaw(ref platform, state, obj, AutoBars, 0) == 0 ?
			0u : 1u;
		policy.UseWindowBorder = ReadRaw(ref platform, state, obj,
			UseWindowBorder, 0) == 0 ? 0u : 1u;

		if (present)
		{
			existing.Contents = policy.Contents;
			existing.FreeHorizontal = policy.FreeHorizontal;
			existing.FreeVertical = policy.FreeVertical;
			existing.HorizontalBar = policy.HorizontalBar;
			existing.VerticalBar = policy.VerticalBar;
			existing.NoHorizontalBar = policy.NoHorizontalBar;
			existing.NoVerticalBar = policy.NoVerticalBar;
			existing.AutoBars = policy.AutoBars;
			existing.UseWindowBorder = policy.UseWindowBorder;
			if (!MuiScrollgroupPolicyStateAdmission.ValidateLive(ref platform,
				state, obj, existing)) return false;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				PolicyStateKey);
			return MuiScrollgroupPolicyStateRecordCodec.Write(ref platform, block,
				existing);
		}

		var value = default(MuiScrollgroupPolicyStateRecord);
		value.Magic = MuiScrollgroupPolicyStateRecord.Cookie;
		value.Contents = policy.Contents;
		value.FreeHorizontal = policy.FreeHorizontal;
		value.FreeVertical = policy.FreeVertical;
		value.HorizontalBar = policy.HorizontalBar;
		value.VerticalBar = policy.VerticalBar;
		value.NoHorizontalBar = policy.NoHorizontalBar;
		value.NoVerticalBar = policy.NoVerticalBar;
		value.AutoBars = policy.AutoBars;
		value.UseWindowBorder = policy.UseWindowBorder;
		if (!MuiScrollgroupPolicyStateAdmission.ValidateLive(ref platform, state,
			obj, value)) return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiScrollgroupPolicyStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiScrollgroupPolicyStateRecord.Size);
		var written = MuiScrollgroupPolicyStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			PolicyStateKey, scratch,
			unchecked((int)MuiScrollgroupPolicyStateRecord.Size));
		platform.Clear(scratch, MuiScrollgroupPolicyStateRecord.Size);
		platform.Free(scratch, MuiScrollgroupPolicyStateRecord.Size);
		return added;
	}

	// A present Scrollgroup policy block is authoritative typed state. Reject a
	// wrong-sized or malformed record before layout, event, getter, or setter
	// consumers can recover through raw compatibility attributes.
	private static bool TryReadPolicyAdmission<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiScrollgroupPolicyStateRecord value,
		out bool present) where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PolicyStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			PolicyStateKey);
		present = block.IsNotNull || length != 0;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		if (!present) return true;
		return !block.IsNull && length == unchecked((int)
			MuiScrollgroupPolicyStateRecord.Size) &&
			MuiScrollgroupPolicyStateRecordCodec.TryReadStructural(ref platform,
				block, out value) &&
			MuiScrollgroupPolicyStateAdmission.ValidateLive(ref platform, state,
				obj, value);
	}

	private static uint ReadRaw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint defaultValue)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			attribute, out var value) ? value : defaultValue;

	private static bool IsScrollgroupObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var classRecord = MuiHeadlessObjectCore.ObjectClassRecord(ref platform,
			state, obj);
		uint depth = 0;
		while (classRecord.IsNotNull && depth++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessClassCodec.TryRead(ref platform, classRecord,
				out var classValue)) return false;
			if (IsScrollgroupName(ref platform, classValue.Name)) return true;
			if (classValue.Super.IsNull) return false;
			classRecord = FindClassByBoopsi(ref platform, state, classValue.Super);
		}
		return false;
	}

	private static APTR FindClassByBoopsi<TPlatform>(ref TPlatform platform,
		APTR state, APTR boopsi) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return APTR.Null;
		var current = stateValue.Classes;
		uint depth = 0;
		while (current.IsNotNull && depth++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessClassCodec.TryRead(ref platform, current,
				out var classValue)) return APTR.Null;
			if (classValue.Boopsi.Raw == boopsi.Raw) return current;
			current = classValue.Next;
		}
		return APTR.Null;
	}

	private static bool IsScrollgroupName<TPlatform>(ref TPlatform platform,
		APTR name) where TPlatform : struct, IMuiGuestMemory
	{
		if (name.IsNull || !platform.IsMapped(name, 16)) return false;
		return platform.ReadUInt8(name, 0) == (byte)'S' &&
			platform.ReadUInt8(name, 1) == (byte)'c' &&
			platform.ReadUInt8(name, 2) == (byte)'r' &&
			platform.ReadUInt8(name, 3) == (byte)'o' &&
			platform.ReadUInt8(name, 4) == (byte)'l' &&
			platform.ReadUInt8(name, 5) == (byte)'l' &&
			platform.ReadUInt8(name, 6) == (byte)'g' &&
			platform.ReadUInt8(name, 7) == (byte)'r' &&
			platform.ReadUInt8(name, 8) == (byte)'o' &&
			platform.ReadUInt8(name, 9) == (byte)'u' &&
			platform.ReadUInt8(name, 10) == (byte)'p' &&
			platform.ReadUInt8(name, 11) == (byte)'.' &&
			platform.ReadUInt8(name, 12) == (byte)'m' &&
			platform.ReadUInt8(name, 13) == (byte)'u' &&
			platform.ReadUInt8(name, 14) == (byte)'i' &&
			platform.ReadUInt8(name, 15) == 0;
	}

	public static bool Layout<TPlatform>(ref TPlatform platform, APTR state,
		APTR scrollgroup, int left, int top, int width, int height)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (width < 0 || height < 0) return false;
		if (IsScrollgroupObject(ref platform, state, scrollgroup) &&
			!TryReadPolicyAdmission(ref platform, state, scrollgroup,
				out _, out _)) return false;
		if (!TryReadViewportAdmission(ref platform, state, scrollgroup,
			out _, out _)) return false;
		if (!PublishLayoutState(ref platform, state, scrollgroup,
			out var layoutState)) return false;
		var contents = layoutState.Contents;
		if (contents.IsNull) return false;
		var horizontalBar = layoutState.HorizontalBar;
		var verticalBar = layoutState.VerticalBar;
		var noHorizontal = layoutState.NoHorizontalBar;
		var noVertical = layoutState.NoVerticalBar;
		var useWindowBorder = false;
		var autoBars = false;
		// Real Scrollgroup objects use the complete named policy record. The
		// compatibility fallback preserves the older generic-group test seam,
		// which can exercise Layout with only the named layout record populated.
		if (IsScrollgroupObject(ref platform, state, scrollgroup) &&
			TryGetPolicyState(ref platform, state, scrollgroup, out var policyState))
		{
			horizontalBar = policyState.HorizontalBar;
			verticalBar = policyState.VerticalBar;
			noHorizontal = policyState.NoHorizontalBar;
			noVertical = policyState.NoVerticalBar;
			useWindowBorder = policyState.UseWindowBorder != 0;
			autoBars = policyState.AutoBars != 0;
		}
		var horizontalBarVisible = horizontalBar.IsNotNull && noHorizontal == 0 &&
			!autoBars;
		var verticalBarVisible = verticalBar.IsNotNull && noVertical == 0 &&
			!autoBars;
		var minMax = MuiGroupLayoutCore.ComputeMinMax(ref platform, state, contents);
		var virtgroupPolicy = default(MuiVirtgroupPolicyStateRecord);
		var hasVirtgroup = MuiVirtgroupCore.IsObject(ref platform, state, contents);
		if (hasVirtgroup && !MuiVirtgroupCore.TryGetPolicyState(ref platform, state,
			contents, out virtgroupPolicy)) hasVirtgroup = false;
		var viewportWidth = 0;
		var viewportHeight = 0;
		var contentWidth = 0;
		var contentHeight = 0;
		for (var pass = 0; pass < 3; pass++)
		{
			viewportWidth = width - (verticalBarVisible && !useWindowBorder ?
				BarExtent : 0);
			viewportHeight = height - (horizontalBarVisible && !useWindowBorder ?
				BarExtent : 0);
			if (viewportWidth < 0) viewportWidth = 0;
			if (viewportHeight < 0) viewportHeight = 0;
			contentWidth = layoutState.FreeHorizontal != 0 ?
				Larger(viewportWidth, minMax.DefWidth) : viewportWidth;
			contentHeight = layoutState.FreeVertical != 0 ?
				Larger(viewportHeight, minMax.DefHeight) : viewportHeight;
			if (hasVirtgroup)
			{
				contentWidth = virtgroupPolicy.Width > 0 ? virtgroupPolicy.Width :
					contentWidth > 0 ? contentWidth : viewportWidth;
				contentHeight = virtgroupPolicy.Height > 0 ? virtgroupPolicy.Height :
					contentHeight > 0 ? contentHeight : viewportHeight;
			}
			if (!autoBars) break;
			var changed = false;
			if (!horizontalBarVisible && horizontalBar.IsNotNull &&
				noHorizontal == 0 && layoutState.FreeHorizontal != 0 &&
				contentWidth > viewportWidth)
			{
				horizontalBarVisible = true;
				changed = true;
			}
			if (!verticalBarVisible && verticalBar.IsNotNull && noVertical == 0 &&
				layoutState.FreeVertical != 0 && contentHeight > viewportHeight)
			{
				verticalBarVisible = true;
				changed = true;
			}
			if (!changed) break;
		}
		var scrollLeft = 0;
		var scrollTop = 0;
		var maximumScrollX = 0;
		var maximumScrollY = 0;
		if (hasVirtgroup)
		{
			if (contentWidth < 0 || contentHeight < 0) return false;
			if (!MuiVirtgroupCore.LayoutFromScrollgroup(ref platform, state,
				contents, left, top, viewportWidth, viewportHeight, contentWidth,
				contentHeight)) return false;
			scrollLeft = ClampScroll(virtgroupPolicy.Left, contentWidth,
				viewportWidth, out maximumScrollX);
			scrollTop = ClampScroll(virtgroupPolicy.Top, contentHeight,
				viewportHeight, out maximumScrollY);
		}
		else
		{
			if (!MuiGroupLayoutCore.Layout(ref platform, state, contents, left, top,
				contentWidth, contentHeight)) return false;
		}
		if (!PublishViewportState(ref platform, state, scrollgroup,
			viewportWidth, viewportHeight, contentWidth, contentHeight,
			maximumScrollX, maximumScrollY, scrollLeft, scrollTop,
			horizontalBarVisible, verticalBarVisible)) return false;
		if (!PublishBorderScrollerState(ref platform, state, scrollgroup,
			useWindowBorder, layoutState.FreeHorizontal != 0,
			layoutState.FreeVertical != 0)) return false;
		if (!ProjectScrollbarRange(ref platform, state, horizontalBar,
			contentWidth, viewportWidth, scrollLeft) ||
			!ProjectScrollbarRange(ref platform, state, verticalBar,
			contentHeight, viewportHeight, scrollTop)) return false;
		if (horizontalBar.IsNotNull)
		{
			if (!horizontalBarVisible)
			{
				if (!MuiAreaLayoutCore.Layout(ref platform, state, horizontalBar,
					left, top, 0, 0)) return false;
			}
			else if (useWindowBorder)
			{
				if (!MuiAreaLayoutCore.Layout(ref platform, state, horizontalBar,
					left, top, 0, 0)) return false;
			}
			else if (!MuiAreaLayoutCore.Layout(ref platform, state, horizontalBar,
				left, top + viewportHeight, viewportWidth, BarExtent)) return false;
		}
		if (verticalBar.IsNotNull)
		{
			if (!verticalBarVisible)
			{
				if (!MuiAreaLayoutCore.Layout(ref platform, state, verticalBar,
					left, top, 0, 0)) return false;
			}
			else if (useWindowBorder)
			{
				if (!MuiAreaLayoutCore.Layout(ref platform, state, verticalBar,
					left, top, 0, 0)) return false;
			}
			else if (!MuiAreaLayoutCore.Layout(ref platform, state, verticalBar,
				left + viewportWidth, top, BarExtent, viewportHeight)) return false;
		}
		return MuiAreaLayoutCore.Layout(ref platform, state, scrollgroup, left, top,
			width, height);
	}

	internal static uint HandleEvent<TPlatform>(ref TPlatform platform, APTR state,
		APTR scrollgroup, APTR intuiMessage, int muiKey)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!IsScrollgroupObject(ref platform, state, scrollgroup) ||
			!TryReadPolicyAdmission(ref platform, state, scrollgroup,
				out _, out _) ||
			!TryGetLayoutState(ref platform, state, scrollgroup,
				out var layoutState) || !TryGetViewportState(ref platform, state,
				scrollgroup, out var viewport)) return 0;
		var horizontal = muiKey == KeyLeft || muiKey == KeyRight ||
			muiKey == KeyHome || muiKey == KeyEnd;
		var vertical = muiKey == KeyUp || muiKey == KeyDown ||
			muiKey == KeyPageUp || muiKey == KeyPageDown;
		if ((!horizontal && !vertical) || (horizontal &&
			(layoutState.FreeHorizontal == 0 ||
				viewport.HorizontalBarVisible == 0)) || (vertical &&
			(layoutState.FreeVertical == 0 ||
				viewport.VerticalBarVisible == 0))) return 0;
		var bar = horizontal ? layoutState.HorizontalBar : layoutState.VerticalBar;
		if (bar.IsNull || !MuiCommonControlCore.IsPropClass(
			MuiCommonControlCore.Classify(ref platform, state, bar))) return 0;
		var changed = false;
		if (muiKey == KeyHome || muiKey == KeyEnd)
		{
			if (!MuiCommonControlCore.TryReadPropRangeState(ref platform, state, bar,
				out var range)) return 0;
			var last = range.Entries > range.Visible ?
				range.Entries - range.Visible : 0u;
			var target = muiKey == KeyHome ? 0u : last;
			if (target != range.First && !MuiHeadlessObjectCore.SetAttribute(
				ref platform, state, bar, MuiCommonControlCore.PropFirst, target,
				true)) return 0;
			changed = target != range.First;
		}
		else changed = MuiCommonControlCore.HandleEvent(ref platform, state, bar,
			intuiMessage, muiKey) != 0;
		if (!changed) return 0;
		if (layoutState.Contents.IsNotNull && MuiVirtgroupCore.IsObject(ref platform,
			state, layoutState.Contents) &&
			MuiCommonControlCore.TryReadPropRangeState(ref platform, state, bar,
				out var updatedRange))
		{
			var offsetAttribute = horizontal ? MuiVirtgroupCore.VirtualLeft :
				MuiVirtgroupCore.VirtualTop;
			if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state,
				layoutState.Contents, offsetAttribute, updatedRange.First, true))
				return 0;
		}
		if (!MuiAreaLayoutCore.TryReadGeometryState(ref platform, state, scrollgroup,
			out var geometry)) return 1u;
		return Layout(ref platform, state, scrollgroup, geometry.Left, geometry.Top,
			geometry.Width, geometry.Height) ? 1u : 0u;
	}

	private static int ClampScroll(int requested, int content, int viewport,
		out int maximum)
	{
		maximum = content > viewport ? content - viewport : 0;
		if (requested < 0) return 0;
		return requested > maximum ? maximum : requested;
	}

	private static bool PublishBorderScrollerState<TPlatform>(
		ref TPlatform platform, APTR state, APTR scrollgroup,
		bool useWindowBorder, bool horizontalRequested,
		bool verticalRequested)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadBorderScrollerAdmission(ref platform, state, scrollgroup,
			out var previous, out var present)) return false;
		var window = FindParentWindow(ref platform, state, scrollgroup);
		var desiredHorizontal = useWindowBorder && horizontalRequested ? 1u : 0u;
		var desiredVertical = useWindowBorder && verticalRequested ? 1u : 0u;
		if (present && previous.Applied != 0 &&
			(previous.Window.Raw != window.Raw || !useWindowBorder))
		{
			// Release only values this Scrollgroup previously installed. This keeps
			// an application's explicit Window policy intact when a layout policy
			// is withdrawn or the object is reparented.
			ClearOwnedWindowBorderScroller(ref platform, state, previous);
		}
		if (useWindowBorder && window.IsNotNull)
		{
			// Set only the axes owned by this Scrollgroup. Leaving an unrelated
			// Window border policy untouched is important when a Window contains
			// more than one border-scrolling child.
			if (desiredHorizontal != 0 &&
				!MuiHeadlessObjectCore.SetAttribute(ref platform, state, window,
					MuiWindowPublicCore.UseBottomBorderScroller, 1, false)) return false;
			if (desiredVertical != 0 &&
				!MuiHeadlessObjectCore.SetAttribute(ref platform, state, window,
					MuiWindowPublicCore.UseRightBorderScroller, 1, false)) return false;
		}

		var value = default(MuiScrollgroupBorderScrollerStateRecord);
		value.Magic = MuiScrollgroupBorderScrollerStateRecord.Cookie;
		value.Window = window;
		value.UseWindowBorder = useWindowBorder ? 1u : 0u;
		value.HorizontalRequested = desiredHorizontal;
		value.VerticalRequested = desiredVertical;
		value.Applied = useWindowBorder && window.IsNotNull ? 1u : 0u;
		if (!MuiScrollgroupBorderScrollerStateAdmission.ValidateLive(ref platform,
			state, scrollgroup, value)) return false;
		if (present)
		{
			var existingBlock = MuiStoreCore.DataspaceFind(ref platform, state,
				scrollgroup, BorderScrollerStateKey);
			return MuiScrollgroupBorderScrollerStateRecordCodec.Write(ref platform,
				existingBlock, value);
		}
		var block = MuiStoreCore.DataspaceFind(ref platform, state, scrollgroup,
			BorderScrollerStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, scrollgroup,
			BorderScrollerStateKey) == unchecked((int)
			MuiScrollgroupBorderScrollerStateRecord.Size) &&
			MuiScrollgroupBorderScrollerStateRecordCodec.TryRead(ref platform, block,
				out _))
			return MuiScrollgroupBorderScrollerStateRecordCodec.Write(ref platform,
				block, value);

		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiScrollgroupBorderScrollerStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiScrollgroupBorderScrollerStateRecord.Size);
		var written = MuiScrollgroupBorderScrollerStateRecordCodec.Write(
			ref platform, scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			scrollgroup, BorderScrollerStateKey, scratch, unchecked((int)
			MuiScrollgroupBorderScrollerStateRecord.Size));
		platform.Clear(scratch, MuiScrollgroupBorderScrollerStateRecord.Size);
		platform.Free(scratch, MuiScrollgroupBorderScrollerStateRecord.Size);
		return added;
	}

	// A present border-scroller projection is authoritative typed state. Reject
	// malformed state before Window policy ownership or projection publication
	// can repair it through a raw private attribute.
	private static bool TryReadBorderScrollerAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiScrollgroupBorderScrollerStateRecord value, out bool present)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		value = default;
		present = false;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			BorderScrollerStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			BorderScrollerStateKey);
		present = block.IsNotNull || length != 0;
		if (!present) return true;
		return !block.IsNull && length == unchecked((int)
			MuiScrollgroupBorderScrollerStateRecord.Size) &&
			MuiScrollgroupBorderScrollerStateRecordCodec.TryReadStructural(ref platform,
				block, out value) &&
			MuiScrollgroupBorderScrollerStateAdmission.ValidateLive(ref platform,
				state, obj, value);
	}

	private static APTR FindParentWindow<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var current = MuiHeadlessObjectCore.ParentObject(ref platform, state, obj);
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (MuiApplicationMessageCore.IsWindowObject(ref platform, state,
				current)) return current;
			current = MuiHeadlessObjectCore.ParentObject(ref platform, state,
				current);
		}
		return APTR.Null;
	}

	private static void ClearOwnedWindowBorderScroller<TPlatform>(
		ref TPlatform platform, APTR state,
		MuiScrollgroupBorderScrollerStateRecord previous)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (previous.Window.IsNull) return;
		if (previous.HorizontalRequested != 0 &&
			MuiHeadlessObjectCore.GetAttribute(ref platform, state, previous.Window,
				MuiWindowPublicCore.UseBottomBorderScroller, out var bottom) &&
			bottom != 0)
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, previous.Window,
				MuiWindowPublicCore.UseBottomBorderScroller, 0, false);
		if (previous.VerticalRequested != 0 &&
			MuiHeadlessObjectCore.GetAttribute(ref platform, state, previous.Window,
				MuiWindowPublicCore.UseRightBorderScroller, out var right) &&
			right != 0)
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, previous.Window,
				MuiWindowPublicCore.UseRightBorderScroller, 0, false);
	}

	private static bool ProjectScrollbarRange<TPlatform>(ref TPlatform platform,
		APTR state, APTR scrollbar, int content, int viewport, int first)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (scrollbar.IsNull || !MuiCommonControlCore.IsPropClass(
			MuiCommonControlCore.Classify(ref platform, state, scrollbar))) return true;
		var entries = content < 0 ? 0u : unchecked((uint)content);
		var visible = viewport < 0 ? 0u : unchecked((uint)viewport);
		var position = first < 0 ? 0u : unchecked((uint)first);
		if (!MuiCommonControlCore.TrySetHeadlessPropAttribute(ref platform, state,
			scrollbar, MuiCommonControlCore.PropEntries, entries, true,
			out var handled) || !handled) return false;
		if (!MuiCommonControlCore.TrySetHeadlessPropAttribute(ref platform, state,
			scrollbar, MuiCommonControlCore.PropVisible, visible, true,
			out handled) || !handled) return false;
		return MuiCommonControlCore.TrySetHeadlessPropAttribute(ref platform, state,
			scrollbar, MuiCommonControlCore.PropFirst, position, true,
			out handled) && handled;
	}

	internal static bool TryGetLayoutState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiScrollgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			LayoutStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			LayoutStateKey) != unchecked((int)MuiScrollgroupLayoutStateRecord.Size))
			return false;
		return MuiScrollgroupLayoutStateRecordCodec.TryReadStructural(ref platform,
			block, out value) && MuiScrollgroupLayoutStateAdmission.ValidateLive(
			ref platform, state, obj, value);
	}

	internal static bool TryGetViewportState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiScrollgroupViewportStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ViewportStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ViewportStateKey) != unchecked((int)MuiScrollgroupViewportStateRecord.Size))
			return false;
		return MuiScrollgroupViewportStateRecordCodec.TryReadStructural(ref platform,
			block, out value) && MuiScrollgroupViewportStateAdmission.ValidateLive(
			ref platform, state, obj, value);
	}

	private static bool PublishViewportState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int viewportWidth, int viewportHeight,
		int contentWidth, int contentHeight, int maximumScrollX,
		int maximumScrollY, int scrollLeft, int scrollTop,
		bool horizontalBarVisible, bool verticalBarVisible)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadViewportAdmission(ref platform, state, obj,
			out _, out _)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ViewportStateKey);
		var value = default(MuiScrollgroupViewportStateRecord);
		value.Magic = MuiScrollgroupViewportStateRecord.Cookie;
		value.ViewportWidth = viewportWidth;
		value.ViewportHeight = viewportHeight;
		value.ContentWidth = contentWidth;
		value.ContentHeight = contentHeight;
		value.MaximumScrollX = maximumScrollX;
		value.MaximumScrollY = maximumScrollY;
		value.ScrollLeft = scrollLeft;
		value.ScrollTop = scrollTop;
		value.HorizontalBarVisible = horizontalBarVisible ? 1u : 0u;
		value.VerticalBarVisible = verticalBarVisible ? 1u : 0u;
		if (!MuiScrollgroupViewportStateAdmission.ValidateLive(ref platform, state,
			obj, value)) return false;
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ViewportStateKey) == unchecked((int)MuiScrollgroupViewportStateRecord.Size) &&
			MuiScrollgroupViewportStateRecordCodec.TryRead(ref platform, block,
				out _)) return MuiScrollgroupViewportStateRecordCodec.Write(
				ref platform, block, value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiScrollgroupViewportStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiScrollgroupViewportStateRecord.Size);
		var written = MuiScrollgroupViewportStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			ViewportStateKey, scratch,
			unchecked((int)MuiScrollgroupViewportStateRecord.Size));
		platform.Clear(scratch, MuiScrollgroupViewportStateRecord.Size);
		platform.Free(scratch, MuiScrollgroupViewportStateRecord.Size);
		return added;
	}

	// A present viewport projection is authoritative typed state. Reject a
	// wrong-sized or geometrically malformed record before layout can rebuild it
	// from newly computed scalar values.
	private static bool TryReadViewportAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiScrollgroupViewportStateRecord value, out bool present)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		value = default;
		present = false;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ViewportStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ViewportStateKey);
		present = block.IsNotNull || length != 0;
		if (!present) return true;
		return !block.IsNull && length == unchecked((int)
			MuiScrollgroupViewportStateRecord.Size) &&
			MuiScrollgroupViewportStateRecordCodec.TryReadStructural(ref platform,
				block, out value) &&
			MuiScrollgroupViewportStateAdmission.ValidateLive(ref platform, state,
				obj, value);
	}

	private static bool PublishLayoutState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiScrollgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		value = default;
		if (!TryReadLayoutAdmission(ref platform, state, obj,
			out var existing, out var present)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			LayoutStateKey);
		if (present)
		{
			value = existing;
			value.Contents = Pointer(ref platform, state, obj, Contents);
			value.FreeHorizontal = Read(ref platform, state, obj, FreeHorizontal);
			value.FreeVertical = Read(ref platform, state, obj, FreeVertical);
			value.HorizontalBar = Pointer(ref platform, state, obj, HorizontalBar);
			value.VerticalBar = Pointer(ref platform, state, obj, VerticalBar);
			value.NoHorizontalBar = Read(ref platform, state, obj,
				NoHorizontalBar);
			value.NoVerticalBar = Read(ref platform, state, obj, NoVerticalBar);
			if (!MuiScrollgroupLayoutStateAdmission.ValidateLive(ref platform, state,
				obj, value)) return false;
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
		if (!MuiScrollgroupLayoutStateAdmission.ValidateLive(ref platform, state,
			obj, value)) return false;
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

	// A present Scrollgroup layout block is authoritative typed state. Reject a
	// wrong-sized or semantically malformed record before raw compatibility
	// attributes can rebuild it or layout can publish dependent projections.
	private static bool TryReadLayoutAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiScrollgroupLayoutStateRecord value, out bool present)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		value = default;
		present = false;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			LayoutStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			LayoutStateKey);
		present = block.IsNotNull || length != 0;
		if (!present) return true;
		return !block.IsNull && length == unchecked((int)
			MuiScrollgroupLayoutStateRecord.Size) &&
			MuiScrollgroupLayoutStateRecordCodec.TryReadStructural(ref platform,
				block, out value) &&
			MuiScrollgroupLayoutStateAdmission.ValidateLive(ref platform, state,
				obj, value);
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
	public const uint VirtualWidth = 0x80427C49;
	public const uint VirtualHeight = 0x80423038;
	public const uint Input = 0x80427F7E;
	public const uint VirtualLeft = 0x80429371;
	public const uint VirtualTop = 0x80425200;
	public const uint TryFit = 0x80429427;
	private const uint LayoutStateKey = 0x0D100011u;
	private const uint PolicyStateKey = 0x0D100018u;
	private const uint DisplayStateKey = 0x0D10001Au;
	private const uint PointerStateKey = 0x0D10001Bu;

	private const int KeyNone = -1;
	private const uint DefaultInput = 1;
	private const uint IdcmpMouseButtons = 1u << 3;
	private const uint IdcmpMouseMove = 1u << 4;
	private const ushort SelectDown = 0x0068;
	private const ushort SelectUp = 0x0069;

	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == VirtualWidth || attribute == VirtualHeight ||
		attribute == VirtualLeft || attribute == VirtualTop ||
		attribute == TryFit;

	internal static bool TryGetAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (!IsPublicGetterAttribute(attribute) ||
			!IsVirtgroupObject(ref platform, state, obj) ||
			!PublishPolicyState(ref platform, state, obj, out var policy))
			return false;
		value = attribute == VirtualWidth ? unchecked((uint)policy.Width) :
			attribute == VirtualHeight ? unchecked((uint)policy.Height) :
			attribute == VirtualLeft ? unchecked((uint)policy.Left) :
			attribute == VirtualTop ? unchecked((uint)policy.Top) : policy.TryFit;
		return true;
	}

	internal static bool TryGetPolicyState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiVirtgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (!IsVirtgroupObject(ref platform, state, obj) ||
			!PublishPolicyState(ref platform, state, obj, out var policy))
			return false;
		value.Magic = MuiVirtgroupPolicyStateRecord.Cookie;
		value.Input = policy.Input;
		value.Width = policy.Width;
		value.Height = policy.Height;
		value.Left = policy.Left;
		value.Top = policy.Top;
		value.TryFit = policy.TryFit;
		return true;
	}

	internal static bool IsObject<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		IsVirtgroupObject(ref platform, state, obj);

	// MorphOS Virtgroup uses MUIM_HandleInput for mouse-driven scrolling. The
	// IntuiMessage is decoded once into the named pointer record; all transient
	// drag state remains guest-resident in MuiVirtgroupPointerStateRecord.
	internal static bool HandleInput<TPlatform>(ref TPlatform platform, APTR state,
		APTR virtgroup, APTR intuiMessage, int muiKey)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (muiKey != KeyNone || !IsVirtgroupObject(ref platform, state,
			virtgroup) || !MuiIntuiMessageCodec.TryReadPointer(ref platform,
			intuiMessage, out var pointer) || !TryReadPolicyAdmission(ref platform,
			state, virtgroup, out _, out _) ||
			!TryReadPointerAdmission(ref platform, state, virtgroup,
				out _, out _)) return false;

		if (pointer.Class == IdcmpMouseButtons && pointer.Code == SelectDown)
		{
			if (ReadRaw(ref platform, state, virtgroup, Input, DefaultInput) == 0 ||
			!TryGetDisplayState(ref platform, state, virtgroup,
				out _)) return false;
			var drag = default(MuiVirtgroupPointerStateRecord);
			drag.Magic = MuiVirtgroupPointerStateRecord.Cookie;
			drag.Flags = MuiVirtgroupPointerStateRecord.ActiveFlag |
				MuiVirtgroupPointerStateRecord.CapturedFlag;
			drag.StartX = pointer.MouseX;
			drag.StartY = pointer.MouseY;
			drag.LastX = pointer.MouseX;
			drag.LastY = pointer.MouseY;
			drag.StartLeft = unchecked((int)ReadRaw(ref platform, state,
				virtgroup, VirtualLeft, 0));
			drag.StartTop = unchecked((int)ReadRaw(ref platform, state,
				virtgroup, VirtualTop, 0));
			return WritePointerState(ref platform, state, virtgroup, drag);
		}

		if (!TryReadPointerState(ref platform, state, virtgroup, out var active) ||
			(active.Flags & MuiVirtgroupPointerStateRecord.ActiveFlag) == 0)
			return false;
		if (pointer.Class == IdcmpMouseButtons && pointer.Code == SelectUp)
		{
			active.Flags = 0;
			return MuiStoreCore.DataspaceRemove(ref platform, state, virtgroup,
				PointerStateKey);
		}
		if (pointer.Class != IdcmpMouseMove) return false;
		if (!TryGetDisplayState(ref platform, state, virtgroup,
			out var display)) return false;
		var width = unchecked((int)ReadRaw(ref platform, state, virtgroup,
			VirtualWidth, 0));
		var height = unchecked((int)ReadRaw(ref platform, state, virtgroup,
			VirtualHeight, 0));
		var maximumLeft = width > display.Width ? width - display.Width : 0;
		var maximumTop = height > display.Height ? height - display.Height : 0;
		var targetLeft = Clamp(active.StartLeft - (pointer.MouseX - active.StartX),
			0, maximumLeft);
		var targetTop = Clamp(active.StartTop - (pointer.MouseY - active.StartY),
			0, maximumTop);
		var currentLeft = unchecked((int)ReadRaw(ref platform, state, virtgroup,
			VirtualLeft, 0));
		var currentTop = unchecked((int)ReadRaw(ref platform, state, virtgroup,
			VirtualTop, 0));
		if (targetLeft != currentLeft && !MuiHeadlessObjectCore.SetAttribute(
			ref platform, state, virtgroup, VirtualLeft,
			unchecked((uint)targetLeft), true)) return false;
		if (targetTop != currentTop && !MuiHeadlessObjectCore.SetAttribute(
			ref platform, state, virtgroup, VirtualTop,
			unchecked((uint)targetTop), true))
		{
			if (targetLeft != currentLeft) MuiHeadlessObjectCore.SetAttribute(
				ref platform, state, virtgroup, VirtualLeft,
				unchecked((uint)currentLeft), false);
			return false;
		}
		active.LastX = pointer.MouseX;
		active.LastY = pointer.MouseY;
		if (!WritePointerState(ref platform, state, virtgroup, active)) return false;
		if ((targetLeft != currentLeft || targetTop != currentTop) &&
			!Layout(ref platform, state, virtgroup, display.Left, display.Top,
				display.Width, display.Height)) return false;
		return true;
	}

	internal static bool TrySet<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, uint attribute, uint value, bool notify, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		handled = false;
		if (!IsVirtgroupAttribute(attribute) ||
			!MuiHeadlessObjectCodec.TryRead(ref platform, record,
				out var objectValue) ||
			!IsVirtgroupObject(ref platform, state, objectValue.Boopsi))
			return false;
		handled = true;
		var initialized = MuiHeadlessObjectCore.IsObjectInitialized(ref platform,
			record);
		if (!initialized && !IsConstructionAttribute(attribute)) return false;
		if (initialized && !IsRuntimeSetAttribute(attribute)) return false;
		if (!TryReadPolicyAdmission(ref platform, state, objectValue.Boopsi,
			out _, out _)) return false;
		var normalized = attribute == Input || attribute == TryFit ?
			(value == 0 ? 0u : 1u) : value;
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, attribute, normalized, initialized && notify)) return false;
		if (!initialized) return true;
		return PublishPolicyState(ref platform, state, objectValue.Boopsi, out _);
	}

	private static bool IsVirtgroupAttribute(uint attribute) =>
		attribute == Input || attribute == VirtualWidth ||
		attribute == VirtualHeight || attribute == VirtualLeft ||
		attribute == VirtualTop || attribute == TryFit;

	private static bool IsConstructionAttribute(uint attribute) =>
		attribute == Input || attribute == VirtualLeft ||
		attribute == VirtualTop || attribute == TryFit;

	private static bool IsRuntimeSetAttribute(uint attribute) =>
		attribute == VirtualLeft || attribute == VirtualTop ||
		attribute == TryFit;

	private static bool PublishPolicyState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiVirtgroupPolicyState policy)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		policy = default;
		if (!TryReadPolicyAdmission(ref platform, state, obj,
			out var existing, out var present)) return false;
		policy.Input = ReadRaw(ref platform, state, obj, Input, DefaultInput) == 0 ?
			0u : 1u;
		policy.Width = unchecked((int)ReadRaw(ref platform, state, obj,
			VirtualWidth, 0));
		policy.Height = unchecked((int)ReadRaw(ref platform, state, obj,
			VirtualHeight, 0));
		policy.Left = unchecked((int)ReadRaw(ref platform, state, obj,
			VirtualLeft, 0));
		policy.Top = unchecked((int)ReadRaw(ref platform, state, obj,
			VirtualTop, 0));
		policy.TryFit = ReadRaw(ref platform, state, obj, TryFit, 0) == 0 ?
			0u : 1u;
		if (present)
		{
			existing.Input = policy.Input;
			existing.Width = policy.Width;
			existing.Height = policy.Height;
			existing.Left = policy.Left;
			existing.Top = policy.Top;
			existing.TryFit = policy.TryFit;
			if (!MuiVirtgroupPolicyStateAdmission.ValidateLive(ref platform, state,
				obj, existing)) return false;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				PolicyStateKey);
			return MuiVirtgroupPolicyStateRecordCodec.Write(ref platform, block,
				existing);
		}
		var value = default(MuiVirtgroupPolicyStateRecord);
		value.Magic = MuiVirtgroupPolicyStateRecord.Cookie;
		value.Input = policy.Input;
		value.Width = policy.Width;
		value.Height = policy.Height;
		value.Left = policy.Left;
		value.Top = policy.Top;
		value.TryFit = policy.TryFit;
		if (!MuiVirtgroupPolicyStateAdmission.ValidateLive(ref platform, state, obj,
			value)) return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiVirtgroupPolicyStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiVirtgroupPolicyStateRecord.Size);
		var written = MuiVirtgroupPolicyStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			PolicyStateKey, scratch,
			unchecked((int)MuiVirtgroupPolicyStateRecord.Size));
		platform.Clear(scratch, MuiVirtgroupPolicyStateRecord.Size);
		platform.Free(scratch, MuiVirtgroupPolicyStateRecord.Size);
		return added;
	}

	// A present Virtgroup policy block is authoritative typed state. Reject a
	// wrong-sized or malformed record before any getter, setter, input, or
	// layout consumer can fall back to raw compatibility attributes.
	private static bool TryReadPolicyAdmission<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiVirtgroupPolicyStateRecord value,
		out bool present) where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PolicyStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			PolicyStateKey);
		present = block.IsNotNull || length != 0;
		if (!present) return true;
		return !block.IsNull && length == unchecked((int)
			MuiVirtgroupPolicyStateRecord.Size) &&
			MuiVirtgroupPolicyStateRecordCodec.TryReadStructural(ref platform, block,
				out value) && MuiVirtgroupPolicyStateAdmission.ValidateLive(ref platform,
				state, obj, value);
	}

	internal static bool TryGetDisplayState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiVirtgroupDisplayStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			DisplayStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			DisplayStateKey) != unchecked((int)MuiVirtgroupDisplayStateRecord.Size))
			return false;
		return MuiVirtgroupDisplayStateRecordCodec.TryReadStructural(ref platform,
			block, out value) && MuiVirtgroupDisplayStateAdmission.ValidateLive(
			ref platform, state, obj, value);
	}

	internal static bool TryGetPointerState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiVirtgroupPointerStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadPointerState(ref platform, state, obj, out value);

	private static bool PublishDisplayState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int left, int top, int width, int height)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var value = default(MuiVirtgroupDisplayStateRecord);
		value.Magic = MuiVirtgroupDisplayStateRecord.Cookie;
		value.Left = left;
		value.Top = top;
		value.Width = width;
		value.Height = height;
		if (!TryReadDisplayAdmission(ref platform, state, obj,
			out _, out var present)) return false;
		if (!MuiVirtgroupDisplayStateAdmission.ValidateLive(ref platform, state,
			obj, value)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			DisplayStateKey);
		if (present) return MuiVirtgroupDisplayStateRecordCodec.Write(ref platform,
			block, value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiVirtgroupDisplayStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiVirtgroupDisplayStateRecord.Size);
		var written = MuiVirtgroupDisplayStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			DisplayStateKey, scratch,
			unchecked((int)MuiVirtgroupDisplayStateRecord.Size));
		platform.Clear(scratch, MuiVirtgroupDisplayStateRecord.Size);
		platform.Free(scratch, MuiVirtgroupDisplayStateRecord.Size);
		return added;
	}

	// A present Virtgroup display block is authoritative typed state. Reject a
	// wrong-sized or geometrically malformed record before LayoutCore can
	// replace it and publish an updated display projection.
	private static bool TryReadDisplayAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiVirtgroupDisplayStateRecord value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			DisplayStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			DisplayStateKey);
		present = block.IsNotNull || length != 0;
		if (!present) return true;
		return !block.IsNull && length == unchecked((int)
			MuiVirtgroupDisplayStateRecord.Size) &&
			MuiVirtgroupDisplayStateRecordCodec.TryReadStructural(ref platform, block,
				out value) && MuiVirtgroupDisplayStateAdmission.ValidateLive(ref platform,
				state, obj, value);
	}

	private static bool TryReadPointerState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiVirtgroupPointerStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PointerStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			PointerStateKey) != unchecked((int)MuiVirtgroupPointerStateRecord.Size))
			return false;
		return MuiVirtgroupPointerStateRecordCodec.TryReadStructural(ref platform,
			block, out value) && MuiVirtgroupPointerStateAdmission.ValidateLive(
			ref platform, state, obj, value);
	}

	private static bool WritePointerState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiVirtgroupPointerStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadPointerAdmission(ref platform, state, obj,
			out _, out var present)) return false;
		if (!MuiVirtgroupPointerStateAdmission.ValidateLive(ref platform, state, obj,
			value)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PointerStateKey);
		if (present) return MuiVirtgroupPointerStateRecordCodec.Write(ref platform,
			block, value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiVirtgroupPointerStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiVirtgroupPointerStateRecord.Size);
		var written = MuiVirtgroupPointerStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			PointerStateKey, scratch,
			unchecked((int)MuiVirtgroupPointerStateRecord.Size));
		platform.Clear(scratch, MuiVirtgroupPointerStateRecord.Size);
		platform.Free(scratch, MuiVirtgroupPointerStateRecord.Size);
		return added;
	}

	// A present Virtgroup pointer block is authoritative transient state. Reject
	// malformed flags or a wrong-sized record before drag handling can replace it.
	private static bool TryReadPointerAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiVirtgroupPointerStateRecord value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PointerStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			PointerStateKey);
		present = block.IsNotNull || length != 0;
		if (!present) return true;
		return !block.IsNull && length == unchecked((int)
			MuiVirtgroupPointerStateRecord.Size) &&
			MuiVirtgroupPointerStateRecordCodec.TryReadStructural(ref platform, block,
				out value) && MuiVirtgroupPointerStateAdmission.ValidateLive(ref platform,
				state, obj, value);
	}

	private static uint ReadRaw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint defaultValue)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			attribute, out var value) ? value : defaultValue;

	private static bool IsVirtgroupObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var classRecord = MuiHeadlessObjectCore.ObjectClassRecord(ref platform,
			state, obj);
		uint depth = 0;
		while (classRecord.IsNotNull && depth++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessClassCodec.TryRead(ref platform, classRecord,
				out var classValue)) return false;
			if (IsVirtgroupName(ref platform, classValue.Name)) return true;
			if (classValue.Super.IsNull) return false;
			classRecord = FindClassByBoopsi(ref platform, state, classValue.Super);
		}
		return false;
	}

	private static APTR FindClassByBoopsi<TPlatform>(ref TPlatform platform,
		APTR state, APTR boopsi) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return APTR.Null;
		var current = stateValue.Classes;
		uint depth = 0;
		while (current.IsNotNull && depth++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessClassCodec.TryRead(ref platform, current,
				out var classValue)) return APTR.Null;
			if (classValue.Boopsi.Raw == boopsi.Raw) return current;
			current = classValue.Next;
		}
		return APTR.Null;
	}

	private static bool IsVirtgroupName<TPlatform>(ref TPlatform platform,
		APTR name) where TPlatform : struct, IMuiGuestMemory
	{
		if (name.IsNull || !platform.IsMapped(name, 14)) return false;
		return platform.ReadUInt8(name, 0) == (byte)'V' &&
			platform.ReadUInt8(name, 1) == (byte)'i' &&
			platform.ReadUInt8(name, 2) == (byte)'r' &&
			platform.ReadUInt8(name, 3) == (byte)'t' &&
			platform.ReadUInt8(name, 4) == (byte)'g' &&
			platform.ReadUInt8(name, 5) == (byte)'r' &&
			platform.ReadUInt8(name, 6) == (byte)'o' &&
			platform.ReadUInt8(name, 7) == (byte)'u' &&
			platform.ReadUInt8(name, 8) == (byte)'p' &&
			platform.ReadUInt8(name, 9) == (byte)'.' &&
			platform.ReadUInt8(name, 10) == (byte)'m' &&
			platform.ReadUInt8(name, 11) == (byte)'u' &&
			platform.ReadUInt8(name, 12) == (byte)'i' &&
			platform.ReadUInt8(name, 13) == 0;
	}

	public static bool Layout<TPlatform>(ref TPlatform platform, APTR state,
		APTR virtgroup, int left, int top, int viewportWidth, int viewportHeight)
		where TPlatform : struct, IMuiLayoutPlatform => LayoutCore(ref platform,
		state, virtgroup, left, top, viewportWidth, viewportHeight, 0, 0);

	internal static bool LayoutFromScrollgroup<TPlatform>(ref TPlatform platform,
		APTR state, APTR virtgroup, int left, int top, int viewportWidth,
		int viewportHeight, int fallbackWidth, int fallbackHeight)
		where TPlatform : struct, IMuiLayoutPlatform => LayoutCore(ref platform,
		state, virtgroup, left, top, viewportWidth, viewportHeight, fallbackWidth,
		fallbackHeight);

	private static bool LayoutCore<TPlatform>(ref TPlatform platform, APTR state,
		APTR virtgroup, int left, int top, int viewportWidth, int viewportHeight,
		int fallbackWidth, int fallbackHeight)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadPolicyAdmission(ref platform, state, virtgroup,
			out _, out _) || !TryReadLayoutAdmission(ref platform, state,
			virtgroup, out _, out _) || !TryReadDisplayAdmission(ref platform,
			state, virtgroup, out _, out _)) return false;
		if (!PublishDisplayState(ref platform, state, virtgroup, left, top,
			viewportWidth, viewportHeight)) return false;
		if (!PublishLayoutState(ref platform, state, virtgroup,
			out var layoutState)) return false;
		var width = layoutState.Width;
		var height = layoutState.Height;
		if (width <= 0 && fallbackWidth > 0) width = fallbackWidth;
		if (height <= 0 && fallbackHeight > 0) height = fallbackHeight;
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
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			LayoutStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			LayoutStateKey) != unchecked((int)MuiVirtgroupLayoutStateRecord.Size))
			return false;
		return MuiVirtgroupLayoutStateRecordCodec.TryReadStructural(ref platform,
			block, out value) && MuiVirtgroupLayoutStateAdmission.ValidateLive(
			ref platform, state, obj, value);
	}

	private static bool PublishLayoutState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiVirtgroupLayoutStateRecord value)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		value = default;
		if (!TryReadLayoutAdmission(ref platform, state, obj,
			out var existing, out var present)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			LayoutStateKey);
		if (present)
		{
			value = existing;
			value.Width = unchecked((int)Read(ref platform, state, obj,
				VirtualWidth));
			value.Height = unchecked((int)Read(ref platform, state, obj,
				VirtualHeight));
			value.Left = unchecked((int)Read(ref platform, state, obj,
				VirtualLeft));
			value.Top = unchecked((int)Read(ref platform, state, obj,
				VirtualTop));
			value.TryFit = Read(ref platform, state, obj, TryFit);
			if (!MuiVirtgroupLayoutStateAdmission.ValidateLive(ref platform, state,
				obj, value)) return false;
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
		if (!MuiVirtgroupLayoutStateAdmission.ValidateLive(ref platform, state, obj,
			value)) return false;
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

	// A present Virtgroup layout block is authoritative typed state. Reject a
	// wrong-sized or semantically malformed record before display/layout
	// projections can repair it from raw compatibility attributes.
	private static bool TryReadLayoutAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiVirtgroupLayoutStateRecord value, out bool present)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		value = default;
		present = false;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			LayoutStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			LayoutStateKey);
		present = block.IsNotNull || length != 0;
		if (!present) return true;
		return !block.IsNull && length == unchecked((int)
			MuiVirtgroupLayoutStateRecord.Size) &&
			MuiVirtgroupLayoutStateRecordCodec.TryReadStructural(ref platform, block,
				out value) && MuiVirtgroupLayoutStateAdmission.ValidateLive(ref platform,
				state, obj, value);
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

	private static int Clamp(int value, int minimum, int maximum)
	{
		var upper = maximum < minimum ? minimum : maximum;
		if (value < minimum) return minimum;
		return value > upper ? upper : value;
	}
}
