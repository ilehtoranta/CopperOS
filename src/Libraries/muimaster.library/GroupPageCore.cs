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
	public const uint FieldSize = 4;
	public const uint CookieOffset = 0;
	public const uint ActiveOffset = 4;
	public const uint ChangesOffset = 8;
	public const uint LastSelectorOffset = 12;
	public uint Cookie;
	public uint Active;
	public uint Changes;
	public uint LastSelector;
}

internal static class MuiGroupPageStateValidation
{
	private const int MinimumSelector = -4;

	private static bool IsSelector(uint raw)
	{
		var value = unchecked((int)raw);
		return value >= MinimumSelector;
	}

	internal static bool IsValidRecord(MuiGroupPageState value) =>
		value.Cookie == MuiGroupPageState.Magic &&
		value.Active <= int.MaxValue && IsSelector(value.LastSelector);

	internal static bool IsValidState(MuiGroupPageState value) =>
		IsValidRecord(value);

	internal static bool IsValidActive(MuiGroupPageState value, uint count)
	{
		if (!IsValidState(value)) return false;
		return count == 0 ? value.Active == 0 : value.Active < count;
	}
}

// Page layout consumes one active child at a time.  Keep that decision in a
// named host record so inactive and explicitly hidden pages are represented by
// zero-area geometry without guest pointers, offsets, or managed collections.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupPageLayoutSelection
{
	internal int ActiveIndex;
	internal int Count;
	internal int Left;
	internal int Top;
	internal int Width;
	internal int Height;
	internal uint ActiveShown;
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

// The fixed ActivePage state record owns its packed positions in this bounded
// adapter. Live page consumers use it directly; the typed cursor remains only
// for compatibility callers and adapter-focused tests.
internal static class MuiGroupPageStateMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiGroupPageStateField field,
		out uint index)
	{
		if (field == MuiGroupPageStateField.Cookie)
			index = 0;
		else if (field == MuiGroupPageStateField.Active)
			index = 1;
		else if (field == MuiGroupPageStateField.Changes)
			index = 2;
		else if (field == MuiGroupPageStateField.LastSelector)
			index = 3;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupPageStateField field, out APTR address)
	where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGroupPageStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGroupPageStateFieldCursor cursor, out APTR address, out uint fieldSize)
	where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiGroupPageState.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiGroupPageState.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiGroupPageState.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupPageStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiGroupPageStateCodec.TryReadRecord(ref platform, record,
			out var state)) return false;
		if (field == MuiGroupPageStateField.Cookie)
			value = state.Cookie;
		else if (field == MuiGroupPageStateField.Active)
			value = state.Active;
		else if (field == MuiGroupPageStateField.Changes)
			value = state.Changes;
		else if (field == MuiGroupPageStateField.LastSelector)
			value = state.LastSelector;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupPageStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGroupPageStateCodec.TryReadRecord(ref platform, record,
			out var state)) return false;
		if (field == MuiGroupPageStateField.Cookie)
			state.Cookie = value;
		else if (field == MuiGroupPageStateField.Active)
			state.Active = value;
		else if (field == MuiGroupPageStateField.Changes)
			state.Changes = value;
		else if (field == MuiGroupPageStateField.LastSelector)
			state.LastSelector = value;
		else return false;
		return MuiGroupPageStateCodec.WriteRecord(ref platform, record, state);
	}
}

internal static class MuiGroupPageStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGroupPageStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGroupPageStateFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGroupPageStateMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupPageStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGroupPageStateMemoryCodec.TryReadUInt32(ref platform, record, field,
			out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupPageStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGroupPageStateMemoryCodec.TryWriteUInt32(ref platform, record, field,
			value);
}

internal static class MuiGroupPageStateCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupPageState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupPageState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Cookie) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Active) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Changes) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.LastSelector) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupPageState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupPageState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Cookie) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Active) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Changes) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.LastSelector)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupPageState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return !address.IsNull &&
			MuiGroupPageStateValidation.IsValidRecord(value) &&
			WriteRecord(ref platform, address, value);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupPageState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return TryReadRecord(ref platform, address, out value) &&
			MuiGroupPageStateValidation.IsValidRecord(value);
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
			if (!MuiGroupPageStateValidation.IsValidActive(pageState, count))
				return false;
			value = count == 0 ? pageState.Active :
				NormalizeActive(pageState.Active, count);
			return true;
		}
		if (HasStateAttribute(ref platform, state, group)) return false;
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
		if (HasStateAttribute(ref platform, state, obj) &&
			!TryGetState(ref platform, state, obj, out _)) return false;
		if (count == 0)
		{
			if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
				record, ActivePage, requested, false)) return false;
			if (notify) MuiNotifyCore.DispatchAttributeChange(ref platform, state,
				record, ActivePage, requested);
			return true;
		}
		var block = EnsureState(ref platform, state, record);
		if (block.IsNull || !TryReadState(ref platform, block, out var value) ||
			!MuiGroupPageStateValidation.IsValidActive(value, count))
			return false;
		var current = value.Active < count ? value.Active : 0u;
		if (!Resolve(requested, current, count, out var active)) return false;
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, ActivePage, active, false)) return false;
		value.Active = active;
		value.LastSelector = requested;
		value.Changes = value.Changes == uint.MaxValue ? uint.MaxValue :
			value.Changes + 1;
		if (!WriteState(ref platform, block, value)) return false;
		MuiHeadlessMemory.Mutated(ref platform, state);
		if (notify) MuiNotifyCore.DispatchAttributeChange(ref platform, state,
			record, ActivePage, active);
		return true;
	}

	internal static uint ReadActivePage<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, uint count) where TPlatform : struct,
		IMuiHeadlessPlatform
	{
		return TryReadActivePage(ref platform, state, group, count,
			out var value) ? value : 0;
	}

	internal static bool TryReadActivePage<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, uint count, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (TryGetState(ref platform, state, group, out var pageState))
		{
			if (!MuiGroupPageStateValidation.IsValidActive(pageState, count))
				return false;
			value = NormalizeActive(pageState.Active, count);
			return true;
		}
		if (HasStateAttribute(ref platform, state, group)) return false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
			ActivePage, out var raw)) return true;
		value = NormalizeActive(raw, count);
		return true;
	}

	internal static bool TryResolveLayout<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, int left, int top, int width,
		int height, out MuiGroupPageLayoutSelection selection)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		selection = default;
		if (count < 0 || !TryReadActivePage(ref platform, state, group,
			unchecked((uint)count), out var active)) return false;
		selection.ActiveIndex = unchecked((int)active);
		selection.Count = count;
		selection.Left = left;
		selection.Top = top;
		selection.Width = width;
		selection.Height = height;
		selection.ActiveShown = 1;
		var child = MuiFamilyCore.GetChild(ref platform, state, group,
			selection.ActiveIndex, APTR.Null);
		if (!child.IsNull && MuiAreaLayoutCore.TryReadLayoutPolicyState(
			ref platform, state, child, out var policy) && policy.ShowMe == 0)
			selection.ActiveShown = 0;
		return true;
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
		if (item.IsNotNull)
		{
			if (!MuiHeadlessAttributeCodec.TryRead(ref platform, item,
				out var itemValue) || itemValue.Value == 0) return APTR.Null;
			var existing = APTR.FromPointer(itemValue.Value);
			return TryReadState(ref platform, existing, out _) ? existing :
				APTR.Null;
		}
		var block = APTR.Null;
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
		if (!WriteState(ref platform, block, value))
		{
			MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
				StateAttribute, 0, false);
			platform.Clear(block, MuiGroupPageState.Size);
			platform.Free(block, MuiGroupPageState.Size);
			return APTR.Null;
		}
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

	private static bool HasStateAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, group);
		if (record.IsNull || !MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return true;
		return FindAttributeValue(ref platform, objectValue.Attributes,
			StateAttribute).IsNotNull;
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

	private static bool WriteState<TPlatform>(ref TPlatform platform, APTR block,
		MuiGroupPageState value) where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupPageStateValidation.IsValidState(value) &&
		MuiGroupPageStateCodec.Write(ref platform, block, value);

	private static bool TryReadState<TPlatform>(ref TPlatform platform, APTR block,
		out MuiGroupPageState value) where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupPageStateCodec.TryRead(ref platform, block, out value) &&
		MuiGroupPageStateValidation.IsValidState(value);
}
