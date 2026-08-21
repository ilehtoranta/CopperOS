/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupPageState
{
	public const uint Magic = 0x47504147; // "GPAG"
	public const uint Size = 16;
	public uint Cookie;
	public uint Active;
	public uint Changes;
	public uint LastSelector;
}

internal enum MuiGroupPageStateField : byte
{
	Cookie,
	Active,
	Changes,
	LastSelector,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupPageStateFieldCursor
{
	internal APTR Record;
	internal MuiGroupPageStateField Field;
}

internal static class MuiGroupPageStateFieldCursorCodec
{
	private static bool TryResolve(MuiGroupPageStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiGroupPageStateField.Cookie => 0,
			MuiGroupPageStateField.Active => 4,
			MuiGroupPageStateField.Changes => 8,
			MuiGroupPageStateField.LastSelector => 12,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGroupPageStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiGroupPageState.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupPageStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiGroupPageStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupPageStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGroupPageStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiGroupPageStateCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupPageState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupPageState.Size)) return false;
		return MuiGroupPageStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGroupPageStateField.Cookie, value.Cookie) &&
			MuiGroupPageStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupPageStateField.Active, value.Active) &&
			MuiGroupPageStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupPageStateField.Changes, value.Changes) &&
			MuiGroupPageStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupPageStateField.LastSelector, value.LastSelector);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupPageState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupPageState.Size) ||
			!MuiGroupPageStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupPageStateField.Cookie, out var cookie) ||
			cookie != MuiGroupPageState.Magic ||
			!MuiGroupPageStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupPageStateField.Active, out value.Active) ||
			!MuiGroupPageStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupPageStateField.Changes, out value.Changes) ||
			!MuiGroupPageStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupPageStateField.LastSelector, out value.LastSelector)) return false;
		value.Cookie = MuiGroupPageState.Magic;
		return true;
	}
}

// MorphOS ActivePage special inputs are normalized once at the Group boundary
// and retained as a named guest record. Layout therefore consumes a canonical
// page index and never has to reinterpret a selector after the set operation.
public static class MuiGroupPageCore
{
	public const uint ActivePage = 0x80424199;
	public const int ActiveFirst = 0;
	public const int ActiveLast = -1;
	public const int ActivePrev = -2;
	public const int ActiveNext = -3;
	public const int ActiveAdvance = -4;

	private const uint StateAttribute = 0x7FFE0041;

	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == ActivePage;

	internal static bool TryGetAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (!IsPublicGetterAttribute(attribute) ||
			!MuiGroupChangeCore.IsGroupObject(ref platform, state, group))
			return false;
		var count = CountChildren(ref platform, state, group);
		if (TryGetState(ref platform, state, group, out var pageState))
		{
			value = count == 0 ? pageState.Active :
				NormalizeActive(pageState.Active, count);
			return true;
		}
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
			ActivePage, out var raw))
		{
			value = 0;
			return true;
		}
		value = count == 0 ? raw : NormalizeActive(raw, count);
		return true;
	}

	internal static bool TrySet<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, uint attribute, uint requested, bool notify, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		handled = false;
		if (attribute != ActivePage) return false;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return false;
		var obj = objectValue.Boopsi;
		if (obj.IsNull || !MuiGroupChangeCore.IsGroupObject(ref platform, state,
			obj)) return false;
		handled = true;
		var count = CountChildren(ref platform, state, obj);
		if (count == 0)
		{
			if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
				record, ActivePage, requested, false)) return false;
			if (notify) MuiNotifyCore.DispatchAttributeChange(ref platform, state,
				record, ActivePage, requested);
			return true;
		}
		var block = EnsureState(ref platform, state, record);
		if (block.IsNull || !TryReadState(ref platform, block, out var value))
			return false;
		var current = value.Active < count ? value.Active : 0u;
		if (!Resolve(requested, current, count, out var active)) return false;
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, ActivePage, active, false)) return false;
		value.Active = active;
		value.LastSelector = requested;
		value.Changes = value.Changes == uint.MaxValue ? uint.MaxValue :
			value.Changes + 1;
		WriteState(ref platform, block, value);
		MuiHeadlessMemory.Mutated(ref platform, state);
		if (notify) MuiNotifyCore.DispatchAttributeChange(ref platform, state,
			record, ActivePage, active);
		return true;
	}

	internal static uint ReadActivePage<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, uint count) where TPlatform : struct,
		IMuiHeadlessPlatform
	{
		if (TryGetState(ref platform, state, group, out var value))
			return NormalizeActive(value.Active, count);
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
			ActivePage, out var raw)) return 0;
		return NormalizeActive(raw, count);
	}

	internal static void Cleanup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return;
		var item = FindAttributeValue(ref platform, objectValue.Attributes,
			StateAttribute);
		var block = APTR.Null;
		if (item.IsNotNull && MuiHeadlessAttributeCodec.TryRead(ref platform,
			item, out var itemValue)) block = APTR.FromPointer(itemValue.Value);
		if (!TryReadState(ref platform, block, out _)) return;
		platform.Clear(block, MuiGroupPageState.Size);
		platform.Free(block, MuiGroupPageState.Size);
		MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
			StateAttribute, 0, false);
	}

	// Struct-first native qualification seam for the guest page state.
	public static bool WritePageRecord<TPlatform>(ref TPlatform platform,
		APTR storage, uint active, uint changes, uint selector)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (storage.IsNull || !platform.IsMapped(storage,
			MuiGroupPageState.Size)) return false;
		var value = default(MuiGroupPageState);
		value.Cookie = MuiGroupPageState.Magic;
		value.Active = active;
		value.Changes = changes;
		value.LastSelector = selector;
		return MuiGroupPageStateCodec.Write(ref platform, storage, value);
	}

	public static uint DispatchPageRecord<TPlatform>(ref TPlatform platform,
		APTR storage) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGroupPageStateCodec.TryRead(ref platform, storage,
			out var value)) return 0;
		return value.Cookie ^ value.Active ^ value.Changes ^ value.LastSelector;
	}

	private static bool Resolve(uint requested, uint current, uint count,
		out uint active)
	{
		active = 0;
		var value = unchecked((int)requested);
		if (value == ActiveFirst) return true;
		if (value == ActiveLast)
		{
			active = count - 1;
			return true;
		}
		if (value == ActivePrev)
		{
			active = current == 0 ? 0 : current - 1;
			return true;
		}
		if (value == ActiveNext)
		{
			active = current + 1 < count ? current + 1 : count - 1;
			return true;
		}
		if (value == ActiveAdvance)
		{
			active = current + 1 < count ? current + 1 : 0;
			return true;
		}
		if (value < 0 || (uint)value >= count) return false;
		active = (uint)value;
		return true;
	}

	private static APTR EnsureState<TPlatform>(ref TPlatform platform, APTR state,
		APTR record) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return APTR.Null;
		var item = FindAttributeValue(ref platform, objectValue.Attributes,
			StateAttribute);
		var block = APTR.Null;
		if (item.IsNotNull && MuiHeadlessAttributeCodec.TryRead(ref platform,
			item, out var itemValue)) block = APTR.FromPointer(itemValue.Value);
		if (TryReadState(ref platform, block, out _)) return block;
		block = MuiHeadlessMemory.Allocate(ref platform, MuiGroupPageState.Size);
		if (block.IsNull) return APTR.Null;
		var value = default(MuiGroupPageState);
		value.Cookie = MuiGroupPageState.Magic;
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, StateAttribute, block.Raw, false))
		{
			platform.Clear(block, MuiGroupPageState.Size);
			platform.Free(block, MuiGroupPageState.Size);
			return APTR.Null;
		}
		WriteState(ref platform, block, value);
		return block;
	}

	private static uint CountChildren<TPlatform>(ref TPlatform platform,
		APTR state, APTR group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = 0u;
		while (count < MuiHeadlessLayout.MaximumTraversal &&
			MuiFamilyCore.GetChild(ref platform, state, group, (int)count,
				APTR.Null).IsNotNull) count++;
		return count;
	}

	private static uint NormalizeActive(uint requested, uint count)
	{
		if (count == 0) return 0;
		var value = unchecked((int)requested);
		if (value == ActiveLast) return count - 1;
		return value >= 0 && (uint)value < count ? (uint)value : 0;
	}

	private static bool TryGetState<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, out MuiGroupPageState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, group);
		if (record.IsNull || !MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return false;
		var block = FindAttributeValue(ref platform, objectValue.Attributes,
			StateAttribute);
		if (block.IsNull || !MuiHeadlessAttributeCodec.TryRead(ref platform, block,
			out var blockValue)) return false;
		return TryReadState(ref platform, APTR.FromPointer(blockValue.Value),
			out value);
	}

	private static APTR FindAttributeValue<TPlatform>(ref TPlatform platform,
		APTR current, uint attribute) where TPlatform : struct, IMuiGuestMemory
	{
		var currentRaw = current.Raw;
		var visited = 0u;
		while (currentRaw != 0 && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			var item = APTR.FromPointer(currentRaw);
			if (!MuiHeadlessAttributeCodec.TryRead(ref platform, item,
				out var attributeValue)) return APTR.Null;
			if (attributeValue.Id == attribute) return item;
			currentRaw = attributeValue.Next.Raw;
		}
		return APTR.Null;
	}

	private static void WriteState<TPlatform>(ref TPlatform platform, APTR block,
		MuiGroupPageState value) where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupPageStateCodec.Write(ref platform, block, value);

	private static bool TryReadState<TPlatform>(ref TPlatform platform, APTR block,
		out MuiGroupPageState value) where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupPageStateCodec.TryRead(ref platform, block, out value);
}
