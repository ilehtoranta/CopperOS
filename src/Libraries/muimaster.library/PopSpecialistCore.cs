/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Fixed guest-memory layout for the MG09 Pop* specialist family
// (Popstring.mui, Popobject.mui, Poplist.mui, Popasl.mui, Popcolor.mui,
// Poppen.mui and the private Popscreen.mui). The whole family shares a single
// initialized guest-resident instance block discriminated by an exact,
// case-sensitive official class id. Each instance owns a small hook-message
// scratch block; the ASL-derived classes additionally own a 12-byte ASL
// service-state block, and Poplist owns a materialized copy of its source
// array. The family never allocates on the managed heap, holds no managed
// data, and never chains into the frozen common-control / collection / generic
// object cores or their dispatchers. It is deliberately additive: it only
// requires the frozen MG09 service surface (IMuiServicePlatform) plus the
// existing callback (struct Hook) and ASL capabilities carried by it.
internal static class MuiPopSpecialistLayout
{
	public const uint Magic = 0x4D504F50;   // "MPOP"

	// Instance block.
	public const uint InstanceSize = 108;

	// Flags.
	public const uint FlagOpen = 1u << 0;          // popup currently open
	public const uint FlagDisabled = 1u << 1;      // MUIA_Disabled
	public const uint FlagCloseDeferred = 1u << 2; // CloseHook pending next tick
	public const uint FlagToggle = 1u << 3;        // MUIA_Popstring_Toggle
	public const uint FlagVolatile = 1u << 4;      // MUIA_Popobject_Volatile
	public const uint FlagFollow = 1u << 5;        // MUIA_Popobject_Follow
	public const uint FlagLight = 1u << 6;         // MUIA_Popobject_Light
	public const uint FlagShowAlpha = 1u << 7;     // MUIA_Popcolor_ShowAlpha
	public const uint FlagAslActive = 1u << 8;     // MUIA_Popasl_Active
	public const uint FlagAslPending = 1u << 9;    // ASL run scheduled next tick
	public const uint FlagSetupActive = 1u << 10;  // MUIM_Setup seen

	// Owned block sizes.
	public const uint HookMsgSize = 16;
	public const uint AslStateSize = MuiAslServiceStateRecord.Size;
	public const uint WindowSize = 16;     // opaque volatile window record

	// Hook message scratch offsets.
	public const int MsgMethod = 0;
	public const int MsgParam1 = 4;
	public const int MsgParam2 = 8;

	// Bound on the NULL-terminated Poplist source array traversal.
	public const int MaximumArray = 1024;
}

// Named wire record for the complete 108-byte Pop* specialist instance block.
// The state is intentionally represented as fields rather than anonymous
// offsets at call sites; only this codec owns the guest ABI serialization.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPopSpecialistState
{
	internal const uint Size = MuiPopSpecialistLayout.InstanceSize;
	internal const uint Cookie = MuiPopSpecialistLayout.Magic;

	internal uint Magic;
	internal uint Class;
	internal uint Flags;
	internal APTR StringChild;
	internal APTR ButtonChild;
	internal APTR OpenHook;
	internal APTR CloseHook;
	internal APTR PopObject;
	internal APTR ObjStrHook;
	internal APTR StrObjHook;
	internal APTR WindowHook;
	internal APTR Array;
	internal APTR MaterializedArray;
	internal uint ArrayCount;
	internal APTR StartHook;
	internal APTR StopHook;
	internal uint AslType;
	internal uint FontStyles;
	internal APTR AslTags;
	internal APTR AslRequester;
	internal APTR AslState;
	internal APTR Window;
	internal APTR HookMsg;
	internal uint Selected;
	internal uint NotifyAttribute;
	internal uint NotifyValue;
	internal uint NotifyCount;
}

internal enum MuiPopSpecialistRecordField : byte
{
	Magic,
	Class,
	Flags,
	StringChild,
	ButtonChild,
	OpenHook,
	CloseHook,
	PopObject,
	ObjStrHook,
	StrObjHook,
	WindowHook,
	Array,
	MaterializedArray,
	ArrayCount,
	StartHook,
	StopHook,
	AslType,
	FontStyles,
	AslTags,
	AslRequester,
	AslState,
	Window,
	HookMsg,
	Selected,
	NotifyAttribute,
	NotifyValue,
	NotifyCount,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPopSpecialistRecordFieldCursor
{
	internal APTR Address;
	internal MuiPopSpecialistRecordField Field;
}

internal static class MuiPopSpecialistRecordFieldCursorCodec
{
	private static bool TryResolve(MuiPopSpecialistRecordField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiPopSpecialistRecordField.Magic => 0,
			MuiPopSpecialistRecordField.Class => 4,
			MuiPopSpecialistRecordField.Flags => 8,
			MuiPopSpecialistRecordField.StringChild => 12,
			MuiPopSpecialistRecordField.ButtonChild => 16,
			MuiPopSpecialistRecordField.OpenHook => 20,
			MuiPopSpecialistRecordField.CloseHook => 24,
			MuiPopSpecialistRecordField.PopObject => 28,
			MuiPopSpecialistRecordField.ObjStrHook => 32,
			MuiPopSpecialistRecordField.StrObjHook => 36,
			MuiPopSpecialistRecordField.WindowHook => 40,
			MuiPopSpecialistRecordField.Array => 44,
			MuiPopSpecialistRecordField.MaterializedArray => 48,
			MuiPopSpecialistRecordField.ArrayCount => 52,
			MuiPopSpecialistRecordField.StartHook => 56,
			MuiPopSpecialistRecordField.StopHook => 60,
			MuiPopSpecialistRecordField.AslType => 64,
			MuiPopSpecialistRecordField.FontStyles => 68,
			MuiPopSpecialistRecordField.AslTags => 72,
			MuiPopSpecialistRecordField.AslRequester => 76,
			MuiPopSpecialistRecordField.AslState => 80,
			MuiPopSpecialistRecordField.Window => 84,
			MuiPopSpecialistRecordField.HookMsg => 88,
			MuiPopSpecialistRecordField.Selected => 92,
			MuiPopSpecialistRecordField.NotifyAttribute => 96,
			MuiPopSpecialistRecordField.NotifyValue => 100,
			MuiPopSpecialistRecordField.NotifyCount => 104,
			_ => 0,
		};
		return field <= MuiPopSpecialistRecordField.NotifyCount;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiPopSpecialistRecordFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address, MuiPopSpecialistState.Size))
			return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiPopSpecialistRecordField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiPopSpecialistRecordFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiPopSpecialistRecordField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiPopSpecialistRecordFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiPopSpecialistStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiPopSpecialistState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiPopSpecialistState.Size) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.Magic, out var magic) ||
			magic != MuiPopSpecialistState.Cookie)
			return false;
		value.Magic = MuiPopSpecialistState.Cookie;
		if (!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiPopSpecialistRecordField.Class, out value.Class) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.Flags, out value.Flags) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.StringChild, out var stringChild) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.ButtonChild, out var buttonChild) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.OpenHook, out var openHook) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.CloseHook, out var closeHook) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.PopObject, out var popObject) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.ObjStrHook, out var objStrHook) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.StrObjHook, out var strObjHook) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.WindowHook, out var windowHook) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.Array, out var array) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.MaterializedArray,
				out var materializedArray) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.ArrayCount, out value.ArrayCount) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.StartHook, out var startHook) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.StopHook, out var stopHook) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.AslType, out value.AslType) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.FontStyles, out value.FontStyles) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.AslTags, out var aslTags) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.AslRequester, out var aslRequester) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.AslState, out var aslState) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.Window, out var window) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.HookMsg, out var hookMsg) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.Selected, out value.Selected) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.NotifyAttribute,
				out value.NotifyAttribute) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.NotifyValue,
				out value.NotifyValue) ||
			!MuiPopSpecialistRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPopSpecialistRecordField.NotifyCount,
				out value.NotifyCount)) return false;
		value.StringChild = APTR.FromPointer(stringChild);
		value.ButtonChild = APTR.FromPointer(buttonChild);
		value.OpenHook = APTR.FromPointer(openHook);
		value.CloseHook = APTR.FromPointer(closeHook);
		value.PopObject = APTR.FromPointer(popObject);
		value.ObjStrHook = APTR.FromPointer(objStrHook);
		value.StrObjHook = APTR.FromPointer(strObjHook);
		value.WindowHook = APTR.FromPointer(windowHook);
		value.Array = APTR.FromPointer(array);
		value.MaterializedArray = APTR.FromPointer(materializedArray);
		value.StartHook = APTR.FromPointer(startHook);
		value.StopHook = APTR.FromPointer(stopHook);
		value.AslTags = APTR.FromPointer(aslTags);
		value.AslRequester = APTR.FromPointer(aslRequester);
		value.AslState = APTR.FromPointer(aslState);
		value.Window = APTR.FromPointer(window);
		value.HookMsg = APTR.FromPointer(hookMsg);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiPopSpecialistState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiPopSpecialistState.Size) || value.Magic !=
			MuiPopSpecialistState.Cookie) return false;
		return MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiPopSpecialistRecordField.Magic, value.Magic) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.Class, value.Class) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.Flags, value.Flags) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.StringChild,
				value.StringChild.Raw) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.ButtonChild,
				value.ButtonChild.Raw) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.OpenHook, value.OpenHook.Raw) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.CloseHook,
				value.CloseHook.Raw) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.PopObject,
				value.PopObject.Raw) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.ObjStrHook,
				value.ObjStrHook.Raw) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.StrObjHook,
				value.StrObjHook.Raw) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.WindowHook,
				value.WindowHook.Raw) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.Array, value.Array.Raw) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.MaterializedArray,
				value.MaterializedArray.Raw) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.ArrayCount, value.ArrayCount) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.StartHook,
				value.StartHook.Raw) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.StopHook, value.StopHook.Raw) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.AslType, value.AslType) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.FontStyles, value.FontStyles) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.AslTags, value.AslTags.Raw) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.AslRequester,
				value.AslRequester.Raw) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.AslState,
				value.AslState.Raw) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.Window, value.Window.Raw) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.HookMsg, value.HookMsg.Raw) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.Selected, value.Selected) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.NotifyAttribute,
				value.NotifyAttribute) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.NotifyValue,
				value.NotifyValue) &&
			MuiPopSpecialistRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiPopSpecialistRecordField.NotifyCount,
				value.NotifyCount);
	}
}

// Poplist's caller-owned Array and materialized copy are NULL-terminated
// vectors of STRPTR values. Keep each pointer slot as a named guest record so
// array ownership and selection logic never decode an anonymous ULONG.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPoplistArrayEntry
{
	internal const uint Size = 4;
	internal APTR Value;
}

internal enum MuiPoplistArrayEntryField : byte
{
	Value,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPoplistArrayEntryFieldCursor
{
	internal APTR Record;
	internal MuiPoplistArrayEntryField Field;
}

internal static class MuiPoplistArrayEntryFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiPoplistArrayEntryFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Field != MuiPoplistArrayEntryField.Value ||
			cursor.Record.IsNull || !platform.IsMapped(cursor.Record,
				MuiPoplistArrayEntry.Size)) return false;
		address = cursor.Record;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiPoplistArrayEntryField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiPoplistArrayEntryFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiPoplistArrayEntryField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiPoplistArrayEntryFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiPoplistArrayEntryCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiPoplistArrayEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiPoplistArrayEntryFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiPoplistArrayEntryField.Value, out var pointer)) return false;
		value.Value = APTR.FromPointer(pointer);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiPoplistArrayEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiPoplistArrayEntryFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiPoplistArrayEntryField.Value, value.Value.Raw);
	}
}

// Poplist source and materialized arrays are bounded NULL-terminated vectors
// of the named pointer-slot records above. Keep the entry index in a cursor so
// every array access shares one overflow-checked guest boundary instead of
// rebuilding `base + index * 4` at each call site.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPoplistArrayCursor
{
	internal const uint EntrySize = MuiPoplistArrayEntry.Size;
	internal const uint MaximumEntries =
		MuiPopSpecialistLayout.MaximumArray + 1;
	internal APTR Base;
	internal uint Index;
}

internal static class MuiPoplistArrayCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiPoplistArrayCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >=
			MuiPoplistArrayCursor.MaximumEntries || cursor.Index >
			(uint.MaxValue - cursor.Base.Raw) /
			MuiPoplistArrayCursor.EntrySize) return false;
		var offset = cursor.Index * MuiPoplistArrayCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address, MuiPoplistArrayCursor.EntrySize);
	}
}

// The Pop* class discriminator. The values are ordinal; the exact official
// class ids and inheritance are resolved by MuiPopSpecialistCore.
public enum MuiPopSpecialistClass : uint
{
	None = 0,
	Popstring = 1,   // : Group
	Popobject = 2,   // : Popstring
	Poplist = 3,     // : Popobject
	Popasl = 4,      // : Popstring
	Popscreen = 5,   // : Popasl (private)
	Popcolor = 6,    // : Popobject
	Poppen = 7,      // : Popobject
}

public static class MuiPopSpecialistCore
{
	// ---- Classification ------------------------------------------------------

	// Classify a guest C-string class id against the exact official names. The
	// loader contract is case-sensitive, so the match is byte-exact against the
	// documented "<Name>.mui" ids. Freestanding: the expected names are compared
	// as ASCII byte literals with no managed strings, arrays or spans.
	public static MuiPopSpecialistClass ClassifyName<TPlatform>(
		ref TPlatform platform, APTR classId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (classId.IsNull) return MuiPopSpecialistClass.None;
		var c0 = B(ref platform, classId, 0);
		var c1 = B(ref platform, classId, 1);
		var c2 = B(ref platform, classId, 2);
		if (c0 != 'P' || c1 != 'o' || c2 != 'p') return MuiPopSpecialistClass.None;
		var c3 = B(ref platform, classId, 3);

		// Popstring.mui
		if (c3 == 's' && B(ref platform, classId, 4) == 't' &&
			B(ref platform, classId, 5) == 'r' &&
			B(ref platform, classId, 6) == 'i' &&
			B(ref platform, classId, 7) == 'n' &&
			B(ref platform, classId, 8) == 'g' && Suffix(ref platform, classId, 9))
			return MuiPopSpecialistClass.Popstring;
		// Popobject.mui
		if (c3 == 'o' && B(ref platform, classId, 4) == 'b' &&
			B(ref platform, classId, 5) == 'j' &&
			B(ref platform, classId, 6) == 'e' &&
			B(ref platform, classId, 7) == 'c' &&
			B(ref platform, classId, 8) == 't' && Suffix(ref platform, classId, 9))
			return MuiPopSpecialistClass.Popobject;
		// Poplist.mui
		if (c3 == 'l' && B(ref platform, classId, 4) == 'i' &&
			B(ref platform, classId, 5) == 's' &&
			B(ref platform, classId, 6) == 't' && Suffix(ref platform, classId, 7))
			return MuiPopSpecialistClass.Poplist;
		// Popasl.mui
		if (c3 == 'a' && B(ref platform, classId, 4) == 's' &&
			B(ref platform, classId, 5) == 'l' && Suffix(ref platform, classId, 6))
			return MuiPopSpecialistClass.Popasl;
		// Popscreen.mui
		if (c3 == 's' && B(ref platform, classId, 4) == 'c' &&
			B(ref platform, classId, 5) == 'r' &&
			B(ref platform, classId, 6) == 'e' &&
			B(ref platform, classId, 7) == 'e' &&
			B(ref platform, classId, 8) == 'n' && Suffix(ref platform, classId, 9))
			return MuiPopSpecialistClass.Popscreen;
		// Popcolor.mui
		if (c3 == 'c' && B(ref platform, classId, 4) == 'o' &&
			B(ref platform, classId, 5) == 'l' &&
			B(ref platform, classId, 6) == 'o' &&
			B(ref platform, classId, 7) == 'r' && Suffix(ref platform, classId, 8))
			return MuiPopSpecialistClass.Popcolor;
		// Poppen.mui
		if (c3 == 'p' && B(ref platform, classId, 4) == 'e' &&
			B(ref platform, classId, 5) == 'n' && Suffix(ref platform, classId, 6))
			return MuiPopSpecialistClass.Poppen;
		return MuiPopSpecialistClass.None;
	}

	private static int B<TPlatform>(ref TPlatform platform, APTR text, int index)
		where TPlatform : struct, IMuiGuestMemory =>
		platform.IsMapped(text, (uint)index + 1) ? platform.ReadUInt8(text, index)
			: -1;

	private static bool Suffix<TPlatform>(ref TPlatform platform, APTR text,
		int offset) where TPlatform : struct, IMuiGuestMemory =>
		B(ref platform, text, offset) == '.' &&
		B(ref platform, text, offset + 1) == 'm' &&
		B(ref platform, text, offset + 2) == 'u' &&
		B(ref platform, text, offset + 3) == 'i' &&
		B(ref platform, text, offset + 4) == 0;

	// ---- Inheritance ---------------------------------------------------------

	// The exact documented immediate superclass. Popstring roots the family at
	// Group; every other Pop class descends from Popstring.
	public static MuiPopSpecialistClass Superclass(MuiPopSpecialistClass cls)
	{
		switch (cls)
		{
			case MuiPopSpecialistClass.Popobject: return MuiPopSpecialistClass.Popstring;
			case MuiPopSpecialistClass.Poplist: return MuiPopSpecialistClass.Popobject;
			case MuiPopSpecialistClass.Popcolor: return MuiPopSpecialistClass.Popobject;
			case MuiPopSpecialistClass.Poppen: return MuiPopSpecialistClass.Popobject;
			case MuiPopSpecialistClass.Popasl: return MuiPopSpecialistClass.Popstring;
			case MuiPopSpecialistClass.Popscreen: return MuiPopSpecialistClass.Popasl;
			default: return MuiPopSpecialistClass.None;   // Popstring : Group
		}
	}

	// Whether `cls` is `ancestor` or transitively descends from it.
	public static bool InheritsFrom(MuiPopSpecialistClass cls,
		MuiPopSpecialistClass ancestor)
	{
		var current = cls;
		for (var step = 0; step < 8; step++)
		{
			if (current == MuiPopSpecialistClass.None) return false;
			if (current == ancestor) return true;
			current = Superclass(current);
		}
		return false;
	}

	// Popscreen is a private class; the others are public.
	public static bool IsPrivate(MuiPopSpecialistClass cls) =>
		cls == MuiPopSpecialistClass.Popscreen;

	// Popobject-derived classes own a popup object and implement the window /
	// ObjStr / StrObj / WindowHook conversion contract. Expressed as a direct
	// classification so the hot Get/Set/Open paths never walk the hierarchy.
	public static bool IsObjectDerived(MuiPopSpecialistClass cls) =>
		cls == MuiPopSpecialistClass.Popobject ||
		cls == MuiPopSpecialistClass.Poplist ||
		cls == MuiPopSpecialistClass.Popcolor ||
		cls == MuiPopSpecialistClass.Poppen;

	// Popasl-derived classes drive an ASL requester through MuiAslServiceCore.
	public static bool IsAslDerived(MuiPopSpecialistClass cls) =>
		cls == MuiPopSpecialistClass.Popasl ||
		cls == MuiPopSpecialistClass.Popscreen;

	public static MuiPopSpecialistClass Classify<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		MuiPopSpecialistStateCodec.TryRead(ref platform, instance, out var state)
			? (MuiPopSpecialistClass)state.Class
			: MuiPopSpecialistClass.None;

	public static bool Valid<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiGuestMemory =>
		instance.IsNotNull &&
		MuiPopSpecialistStateCodec.TryRead(ref platform, instance, out _);

	// ---- Creation (failure-atomic child adoption) ----------------------------

	// Create a Pop* instance of a class named by a guest C-string.
	public static MuiPopSpecialistClass CreateByName<TPlatform>(
		ref TPlatform platform, APTR instance, APTR classId, APTR stringChild,
		APTR buttonChild) where TPlatform : struct, IMuiServicePlatform
	{
		var cls = ClassifyName(ref platform, classId);
		if (cls == MuiPopSpecialistClass.None) return MuiPopSpecialistClass.None;
		return Create(ref platform, instance, cls, stringChild, buttonChild) ? cls
			: MuiPopSpecialistClass.None;
	}

	// Create a Pop* instance of an explicit class. Every Pop class inherits
	// Popstring, so a valid String and Button child are mandatory and adopted
	// atomically: if either is Null, or any owned block cannot be allocated, the
	// call frees everything it touched and returns false, leaving the instance
	// clear. The children become class-owned and are recursively disposed at
	// OM_DISPOSE. Popobject-derived instances default MUIA_Popobject_Volatile to
	// TRUE, matching the documented default.
	public static bool Create<TPlatform>(ref TPlatform platform, APTR instance,
		MuiPopSpecialistClass cls, APTR stringChild, APTR buttonChild)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (instance.IsNull ||
			!platform.IsMapped(instance, MuiPopSpecialistLayout.InstanceSize) ||
			cls == MuiPopSpecialistClass.None || stringChild.IsNull ||
			buttonChild.IsNull) return false;

		var hookMsg = Alloc(ref platform, MuiPopSpecialistLayout.HookMsgSize);
		if (hookMsg.IsNull) return false;

		APTR aslState = APTR.Null;
		if (IsAslDerived(cls))
		{
			aslState = Alloc(ref platform, MuiPopSpecialistLayout.AslStateSize);
			if (aslState.IsNull ||
				!MuiAslServiceCore.Initialize(ref platform, aslState))
			{
				if (aslState.IsNotNull)
					Free(ref platform, aslState, MuiPopSpecialistLayout.AslStateSize);
				Free(ref platform, hookMsg, MuiPopSpecialistLayout.HookMsgSize);
				return false;
			}
		}

		platform.Clear(instance, MuiPopSpecialistLayout.InstanceSize);
		var state = default(MuiPopSpecialistState);
		state.Magic = MuiPopSpecialistState.Cookie;
		state.Class = (uint)cls;
		state.StringChild = stringChild;
		state.ButtonChild = buttonChild;
		state.HookMsg = hookMsg;
		state.AslState = aslState;
		state.Flags = IsObjectDerived(cls)
			? MuiPopSpecialistLayout.FlagVolatile : 0;
		return MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
	}

	// ---- Setup / Cleanup -----------------------------------------------------

	// MUIM_Setup marks the object active in a window tree.
	public static bool Setup<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!Valid(ref platform, instance)) return false;
		SetFlag(ref platform, instance, MuiPopSpecialistLayout.FlagSetupActive,
			true);
		return true;
	}

	// MUIM_Cleanup tears the object down. Any open or pending popup is cancelled
	// here without invoking the deferred CloseHook: an active ASL requester is
	// released through MuiAslServiceCore, a volatile popup window is freed, and
	// the open/pending/deferred state is cleared. Poppen documents this
	// cancel-on-cleanup behavior explicitly; performing it for the whole family
	// keeps teardown safe and idempotent.
	public static bool Cleanup<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Valid(ref platform, instance)) return false;
		CancelPopup(ref platform, instance);
		SetFlag(ref platform, instance, MuiPopSpecialistLayout.FlagSetupActive,
			false);
		return true;
	}

	// Cancel any live popup: release an active ASL requester, free a volatile
	// popup window, and clear open/pending/deferred flags. Idempotent.
	private static void CancelPopup<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return;
		var requester = state.AslRequester;
		if (requester.IsNotNull)
		{
			MuiAslServiceCore.FreeAslRequest(ref platform, state.AslState,
				requester);
			state.AslRequester = APTR.Null;
		}
		FreeWindow(ref platform, instance);
		state.Window = APTR.Null;
		state.Flags &= ~(MuiPopSpecialistLayout.FlagOpen |
			MuiPopSpecialistLayout.FlagCloseDeferred |
			MuiPopSpecialistLayout.FlagAslActive |
			MuiPopSpecialistLayout.FlagAslPending);
		MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
	}

	// ---- Open / Close / Toggle -----------------------------------------------

	// MUIM_Popstring_Open. Opens the popup unless the object is disabled or the
	// popup is already open. The OpenHook is invoked immediately with the exact
	// CallHookPkt register ABI (A0 = hook, A2 = object, A1 = message). For
	// Popobject-derived classes the StrObjHook runs first (string -> object),
	// then a volatile popup window is created (rolled back on failure) and the
	// WindowHook runs. For Popasl-derived classes the StartHook runs and an ASL
	// requester is allocated through MuiAslServiceCore, arming a scheduler tick;
	// no host task or thread is used. Returns whether the popup was opened.
	public static bool Open<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return false;
		var flags = state.Flags;
		if ((flags & MuiPopSpecialistLayout.FlagDisabled) != 0) return false;
		if ((flags & MuiPopSpecialistLayout.FlagOpen) != 0) return false;

		var cls = (MuiPopSpecialistClass)state.Class;

		if (IsAslDerived(cls))
		{
			if (!OpenAsl(ref platform, instance)) return false;
		}
		else if (IsObjectDerived(cls))
		{
			// string -> object, then the volatile window (with rollback).
			InvokeHook(ref platform, instance, state.StrObjHook,
				MuiPopAttributes.Popobject_StrObjHook,
				state.PopObject, state.StringChild.Raw);
			if (!OpenWindow(ref platform, instance)) return false;   // rollback
			MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
				out state);
			InvokeHook(ref platform, instance, state.WindowHook,
				MuiPopAttributes.Popobject_WindowHook,
				state.Window, 0);
		}

		// Base Popstring OpenHook, invoked immediately for every Pop class.
		MuiPopSpecialistStateCodec.TryRead(ref platform, instance, out state);
		InvokeHook(ref platform, instance, state.OpenHook,
			MuiPopAttributes.Popstring_Open, instance, 0);
		state.Flags |= MuiPopSpecialistLayout.FlagOpen;
		MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
		return true;
	}

	// MUIM_Popstring_Close. When the popup is open this schedules the CloseHook:
	// the hook is NOT invoked here but on the next explicit MUIM_HandleInput
	// tick. For Popobject-derived classes the ObjStrHook (object -> string) runs
	// immediately so the string reflects the popup result before the deferred
	// close. `result` is delivered to hooks/notification. Returns whether a
	// close was scheduled.
	public static bool Close<TPlatform>(ref TPlatform platform, APTR instance,
		uint result) where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return false;
		var flags = state.Flags;
		if ((flags & MuiPopSpecialistLayout.FlagOpen) == 0) return false;

		var cls = (MuiPopSpecialistClass)state.Class;
		if (IsObjectDerived(cls) && result != 0)
			InvokeHook(ref platform, instance, state.ObjStrHook,
				MuiPopAttributes.Popobject_ObjStrHook,
				state.StringChild, state.PopObject.Raw);

		// Defer the CloseHook to the next HandleInput tick, recording the close
		// result so the deferred hook receives it.
		state.Flags |= MuiPopSpecialistLayout.FlagCloseDeferred;
		state.Selected = result;
		MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
		return true;
	}

	// MUIA_Popstring_Toggle set to a non-zero value toggles the popup: an open
	// popup is closed (deferred), a closed one is opened. Mirrors the documented
	// toggle button behavior.
	public static bool Toggle<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return false;
		var flags = state.Flags;
		return (flags & MuiPopSpecialistLayout.FlagOpen) != 0
			? Close(ref platform, instance, 1)
			: Open(ref platform, instance);
	}

	// MUIM_HandleInput. The single explicit scheduler tick. A deferred CloseHook
	// is invoked here (not at Close time); a scheduler-armed ASL requester is
	// run here through MuiAslServiceCore, its StopHook invoked, and the requester
	// released. When disabled, input is ignored entirely. Returns whether any
	// pending work was performed.
	public static bool HandleInput<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return false;
		var flags = state.Flags;
		if ((flags & MuiPopSpecialistLayout.FlagDisabled) != 0) return false;

		var handled = false;

		// Run a scheduler-armed ASL requester and finish it on this tick.
		if ((flags & MuiPopSpecialistLayout.FlagAslPending) != 0)
		{
			RunAsl(ref platform, instance);
			MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
				out state);
			flags = state.Flags;
			handled = true;
		}

		// Invoke a deferred CloseHook exactly once, then finish the close.
		if ((flags & MuiPopSpecialistLayout.FlagCloseDeferred) != 0)
		{
			InvokeHook(ref platform, instance, state.CloseHook,
				MuiPopAttributes.Popstring_Close, instance,
				state.Selected);
			var cls = (MuiPopSpecialistClass)state.Class;
			if (IsObjectDerived(cls) &&
				(flags & MuiPopSpecialistLayout.FlagVolatile) != 0)
				FreeWindow(ref platform, instance);
			flags &= ~(MuiPopSpecialistLayout.FlagCloseDeferred |
				MuiPopSpecialistLayout.FlagOpen);
			state.Flags = flags;
			if (IsObjectDerived(cls) &&
				(flags & MuiPopSpecialistLayout.FlagVolatile) != 0)
				state.Window = APTR.Null;
			MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
			handled = true;
		}
		return handled;
	}

	public static bool IsOpen<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiPopSpecialistStateCodec.TryRead(ref platform, instance, out var state) &&
		(state.Flags &
			MuiPopSpecialistLayout.FlagOpen) != 0;

	public static bool IsCloseDeferred<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		MuiPopSpecialistStateCodec.TryRead(ref platform, instance, out var state) &&
		(state.Flags &
			MuiPopSpecialistLayout.FlagCloseDeferred) != 0;

	// ---- Popobject window (volatile) -----------------------------------------

	// Create the volatile popup window record. Atomic: a failed allocation
	// leaves no window and lets Open roll back cleanly.
	private static bool OpenWindow<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return false;
		var existing = state.Window;
		if (existing.IsNotNull) return true;   // non-volatile window kept alive
		var window = Alloc(ref platform, MuiPopSpecialistLayout.WindowSize);
		if (window.IsNull) return false;
		state.Window = window;
		if (!MuiPopSpecialistStateCodec.Write(ref platform, instance, state))
		{
			Free(ref platform, window, MuiPopSpecialistLayout.WindowSize);
			return false;
		}
		return true;
	}

	private static void FreeWindow<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return;
		var window = state.Window;
		if (window.IsNull) return;
		Free(ref platform, window, MuiPopSpecialistLayout.WindowSize);
		state.Window = APTR.Null;
		MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
	}

	// ---- Popasl requester (scheduler driven) ---------------------------------

	// Arm an ASL requester: invoke StartHook, allocate the requester through
	// MuiAslServiceCore, set Active and schedule the run for the next
	// HandleInput tick. On allocation failure nothing is left active. No host
	// task or thread is created.
	private static bool OpenAsl<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return false;
		InvokeHook(ref platform, instance, state.StartHook,
			MuiPopAttributes.Popasl_StartHook, instance, 0);
		var requester = MuiAslServiceCore.AllocAslRequest(ref platform,
			state.AslState, state.AslType, state.AslTags);
		if (requester.IsNull) return false;   // failure cleanup: stays inactive
		state.AslRequester = requester;
		state.Flags |=
			MuiPopSpecialistLayout.FlagAslActive |
			MuiPopSpecialistLayout.FlagAslPending;
		return MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
	}

	// Run the armed ASL requester on a scheduler tick, invoke StopHook, free the
	// requester and clear the active/pending state. The requester result is
	// recorded as the close result for the deferred CloseHook.
	private static void RunAsl<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return;
		var result = MuiAslServiceCore.AslRequest(ref platform, state.AslState,
			state.AslRequester, state.AslTags);
		InvokeHook(ref platform, instance, state.StopHook,
			MuiPopAttributes.Popasl_StopHook, instance, unchecked((uint)result));
		MuiAslServiceCore.FreeAslRequest(ref platform, state.AslState,
			state.AslRequester);
		state.AslRequester = APTR.Null;
		state.Selected = unchecked((uint)result);
		state.Flags &= ~(MuiPopSpecialistLayout.FlagAslActive |
			MuiPopSpecialistLayout.FlagAslPending);
		// Finishing the ASL run schedules the base deferred close.
		state.Flags |= MuiPopSpecialistLayout.FlagCloseDeferred;
		MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
	}

	// ---- Poplist array materialization ---------------------------------------

	// Materialize MUIA_Poplist_Array. The caller-owned NULL-terminated array of
	// string pointers is copied into a class-owned block (bounded traversal) so
	// the popup list can use it without retaining the caller's memory. The
	// source array is never freed; a previously materialized block is released
	// first. Returns whether materialization succeeded.
	public static bool SetArray<TPlatform>(ref TPlatform platform, APTR instance,
		APTR array) where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
			out var state) || (MuiPopSpecialistClass)state.Class !=
			MuiPopSpecialistClass.Poplist)
			return false;
		FreeMaterializedArray(ref platform, instance);
		state.Array = array;
		state.MaterializedArray = APTR.Null;
		state.ArrayCount = 0;
		if (array.IsNull)
		{
			return MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
		}
		var count = 0;
		while (count < MuiPopSpecialistLayout.MaximumArray)
		{
			var cursor = default(MuiPoplistArrayCursor);
			cursor.Base = array;
			cursor.Index = unchecked((uint)count);
			if (!MuiPoplistArrayCursorCodec.TryGetEntry(ref platform, cursor,
				out var address)) break;
			if (!MuiPoplistArrayEntryCodec.TryRead(ref platform, address,
				out var entry)) break;
			if (entry.Value.IsNull) break;
			count++;
		}
		var block = Alloc(ref platform, (uint)(count + 1) *
			MuiPoplistArrayEntry.Size);
		if (block.IsNull) return false;
		for (var index = 0; index < count; index++)
		{
			var sourceCursor = default(MuiPoplistArrayCursor);
			sourceCursor.Base = array;
			sourceCursor.Index = unchecked((uint)index);
			var destinationCursor = default(MuiPoplistArrayCursor);
			destinationCursor.Base = block;
			destinationCursor.Index = unchecked((uint)index);
			if (!MuiPoplistArrayCursorCodec.TryGetEntry(ref platform,
				sourceCursor, out var source) ||
				!MuiPoplistArrayCursorCodec.TryGetEntry(ref platform,
					destinationCursor, out var destination))
			{
				platform.Clear(block, (uint)(count + 1) *
					MuiPoplistArrayEntry.Size);
				platform.Free(block, (uint)(count + 1) *
					MuiPoplistArrayEntry.Size);
				return false;
			}
			if (!MuiPoplistArrayEntryCodec.TryRead(ref platform, source,
				out var entry) || !MuiPoplistArrayEntryCodec.Write(ref platform,
					destination, entry))
			{
				platform.Clear(block, (uint)(count + 1) *
					MuiPoplistArrayEntry.Size);
				platform.Free(block, (uint)(count + 1) *
					MuiPoplistArrayEntry.Size);
				return false;
			}
		}
		var terminatorCursor = default(MuiPoplistArrayCursor);
		terminatorCursor.Base = block;
		terminatorCursor.Index = unchecked((uint)count);
		var end = default(MuiPoplistArrayEntry);
		if (!MuiPoplistArrayCursorCodec.TryGetEntry(ref platform,
			terminatorCursor, out var terminator) ||
			!MuiPoplistArrayEntryCodec.Write(ref platform, terminator, end))
		{
			platform.Clear(block, (uint)(count + 1) * MuiPoplistArrayEntry.Size);
			platform.Free(block, (uint)(count + 1) * MuiPoplistArrayEntry.Size);
			return false;
		}
		state.MaterializedArray = block;
		state.ArrayCount = (uint)count;
		return MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
	}

	// Poplist selection-to-string: select entry `index`, deliver it to the
	// string gadget through the ObjStrHook (object -> string conversion), record
	// the selection, and notify MUIA_String_Contents. Returns whether the index
	// addressed a materialized entry.
	public static bool SelectEntry<TPlatform>(ref TPlatform platform,
		APTR instance, uint index) where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
			out var state) || (MuiPopSpecialistClass)state.Class !=
			MuiPopSpecialistClass.Poplist)
			return false;
		var count = state.ArrayCount;
		if (index >= count) return false;
		var block = state.MaterializedArray;
		if (block.IsNull) return false;
		var cursor = default(MuiPoplistArrayCursor);
		cursor.Base = block;
		cursor.Index = index;
		if (!MuiPoplistArrayCursorCodec.TryGetEntry(ref platform, cursor,
			out var address)) return false;
		if (!MuiPoplistArrayEntryCodec.TryRead(ref platform, address,
			out var entry)) return false;
		state.Selected = entry.Value.Raw;
		MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
		InvokeHook(ref platform, instance, state.ObjStrHook,
			MuiPopAttributes.Popobject_ObjStrHook,
			state.StringChild, entry.Value.Raw);
		Notify(ref platform, instance, MuiPopAttributes.String_Contents,
			entry.Value.Raw,
			false, true, true);
		return true;
	}

	public static uint ArrayCount<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiPopSpecialistStateCodec.TryRead(ref platform, instance, out var state)
			? state.ArrayCount : 0;

	public static uint SelectedEntry<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		MuiPopSpecialistStateCodec.TryRead(ref platform, instance, out var state)
			? state.Selected : 0;

	private static void FreeMaterializedArray<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return;
		var block = state.MaterializedArray;
		if (block.IsNull) return;
		var count = state.ArrayCount;
		Free(ref platform, block, (count + 1) *
			MuiPoplistArrayEntry.Size);
		state.MaterializedArray = APTR.Null;
		state.ArrayCount = 0;
		MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
	}

	// ---- Attribute get -------------------------------------------------------

	public static bool GetAttribute<TPlatform>(ref TPlatform platform,
		APTR instance, uint attribute, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return false;
		var cls = (MuiPopSpecialistClass)state.Class;
		var flags = state.Flags;

		switch (attribute)
		{
			// -- shared / Area --
			case MuiPopAttributes.Disabled:
				value = (flags & MuiPopSpecialistLayout.FlagDisabled) != 0 ? 1u : 0u;
				return true;

			// -- Popstring --
			case MuiPopAttributes.Popstring_String:
				value = state.StringChild.Raw;
				return true;
			case MuiPopAttributes.Popstring_Button:
				value = state.ButtonChild.Raw;
				return true;
			case MuiPopAttributes.Popstring_OpenHook:
				value = state.OpenHook.Raw;
				return true;
			case MuiPopAttributes.Popstring_CloseHook:
				value = state.CloseHook.Raw;
				return true;
			case MuiPopAttributes.Popstring_Toggle:
				value = (flags & MuiPopSpecialistLayout.FlagToggle) != 0 ? 1u : 0u;
				return true;

			// -- Popobject-derived --
			case MuiPopAttributes.Popobject_Object:
				if (!IsObjectDerived(cls)) return false;
				value = state.PopObject.Raw;
				return true;
			case MuiPopAttributes.Popobject_Follow:
				if (!IsObjectDerived(cls)) return false;
				value = (flags & MuiPopSpecialistLayout.FlagFollow) != 0 ? 1u : 0u;
				return true;
			case MuiPopAttributes.Popobject_Light:
				if (!IsObjectDerived(cls)) return false;
				value = (flags & MuiPopSpecialistLayout.FlagLight) != 0 ? 1u : 0u;
				return true;
			case MuiPopAttributes.Popobject_Volatile:
				if (!IsObjectDerived(cls)) return false;
				value = (flags & MuiPopSpecialistLayout.FlagVolatile) != 0 ? 1u : 0u;
				return true;
			case MuiPopAttributes.Popobject_ObjStrHook:
				if (!IsObjectDerived(cls)) return false;
				value = state.ObjStrHook.Raw;
				return true;
			case MuiPopAttributes.Popobject_StrObjHook:
				if (!IsObjectDerived(cls)) return false;
				value = state.StrObjHook.Raw;
				return true;
			case MuiPopAttributes.Popobject_WindowHook:
				if (!IsObjectDerived(cls)) return false;
				value = state.WindowHook.Raw;
				return true;

			// -- Popasl-derived --
			case MuiPopAttributes.Popasl_Active:
				if (!IsAslDerived(cls)) return false;
				value = (flags & MuiPopSpecialistLayout.FlagAslActive) != 0 ? 1u : 0u;
				return true;
			case MuiPopAttributes.Popasl_StartHook:
				if (!IsAslDerived(cls)) return false;
				value = state.StartHook.Raw;
				return true;
			case MuiPopAttributes.Popasl_StopHook:
				if (!IsAslDerived(cls)) return false;
				value = state.StopHook.Raw;
				return true;

			// -- Popcolor --
			case MuiPopAttributes.Popcolor_ShowAlpha:
				if (cls != MuiPopSpecialistClass.Popcolor) return false;
				value = (flags & MuiPopSpecialistLayout.FlagShowAlpha) != 0 ? 1u : 0u;
				return true;
		}
		return false;
	}

	// ---- Attribute set -------------------------------------------------------

	public static bool SetAttribute<TPlatform>(ref TPlatform platform,
		APTR instance, uint attribute, uint value, bool isInit, bool notify,
		out bool changed) where TPlatform : struct, IMuiServicePlatform
	{
		changed = false;
		if (!MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return false;
		var cls = (MuiPopSpecialistClass)state.Class;

		switch (attribute)
		{
			// -- shared / Area -- [ISG]
			case MuiPopAttributes.Disabled:
				changed = SetFlag(ref platform, instance,
					MuiPopSpecialistLayout.FlagDisabled, value != 0);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;

			// -- Popstring -- [ISG] hooks; [.SG] Toggle
			case MuiPopAttributes.Popstring_OpenHook:
				changed = WritePointerField(ref platform, instance, ref state,
					ref state.OpenHook, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiPopAttributes.Popstring_CloseHook:
				changed = WritePointerField(ref platform, instance, ref state,
					ref state.CloseHook, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiPopAttributes.Popstring_Toggle:
				// [.SG]: a runtime set toggles the popup; the flag mirrors it.
				changed = SetFlag(ref platform, instance,
					MuiPopSpecialistLayout.FlagToggle, value != 0);
				if (!isInit && value != 0) Toggle(ref platform, instance);
				return true;
			case MuiPopAttributes.Popstring_String:
			case MuiPopAttributes.Popstring_Button:
				// [I..]: children are adopted at creation; ignored at runtime.
				return true;

			// -- Popobject-derived -- [ISG] Follow/Light/Volatile/hooks; [I..] Object
			case MuiPopAttributes.Popobject_Object:
				if (!IsObjectDerived(cls)) return false;
				if (isInit)
				{
					state.PopObject = APTR.FromPointer(value);
					MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
					changed = true;
				}
				return true;
			case MuiPopAttributes.Popobject_Follow:
				if (!IsObjectDerived(cls)) return false;
				changed = SetFlag(ref platform, instance,
					MuiPopSpecialistLayout.FlagFollow, value != 0);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiPopAttributes.Popobject_Light:
				if (!IsObjectDerived(cls)) return false;
				changed = SetFlag(ref platform, instance,
					MuiPopSpecialistLayout.FlagLight, value != 0);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiPopAttributes.Popobject_Volatile:
				if (!IsObjectDerived(cls)) return false;
				changed = SetFlag(ref platform, instance,
					MuiPopSpecialistLayout.FlagVolatile, value != 0);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiPopAttributes.Popobject_ObjStrHook:
				if (!IsObjectDerived(cls)) return false;
				changed = WritePointerField(ref platform, instance, ref state,
					ref state.ObjStrHook, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiPopAttributes.Popobject_StrObjHook:
				if (!IsObjectDerived(cls)) return false;
				changed = WritePointerField(ref platform, instance, ref state,
					ref state.StrObjHook, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiPopAttributes.Popobject_WindowHook:
				if (!IsObjectDerived(cls)) return false;
				changed = WritePointerField(ref platform, instance, ref state,
					ref state.WindowHook, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;

			// -- Poplist -- [I..] Array
			case MuiPopAttributes.Poplist_Array:
				if (cls != MuiPopSpecialistClass.Poplist) return false;
				if (isInit)
					changed = SetArray(ref platform, instance, APTR.FromPointer(value));
				return true;

			// -- Popasl-derived -- [ISG] hooks; [I..] Type / MUIFontStyles
			case MuiPopAttributes.Popasl_StartHook:
				if (!IsAslDerived(cls)) return false;
				changed = WritePointerField(ref platform, instance, ref state,
					ref state.StartHook, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiPopAttributes.Popasl_StopHook:
				if (!IsAslDerived(cls)) return false;
				changed = WritePointerField(ref platform, instance, ref state,
					ref state.StopHook, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiPopAttributes.Popasl_Type:
				if (!IsAslDerived(cls)) return false;
				if (isInit)
				{
					state.AslType = value;
					MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
					changed = true;
				}
				return true;
			case MuiPopAttributes.Popasl_MUIFontStyles:
				if (!IsAslDerived(cls)) return false;
				if (isInit)
				{
					state.FontStyles = value;
					MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
					changed = true;
				}
				return true;

			// -- Popcolor -- [I..] ShowAlpha (shares Coloradjust_ShowAlpha id)
			case MuiPopAttributes.Popcolor_ShowAlpha:
				if (cls != MuiPopSpecialistClass.Popcolor) return false;
				if (isInit)
					changed = SetFlag(ref platform, instance,
						MuiPopSpecialistLayout.FlagShowAlpha, value != 0);
				return true;
		}
		return false;
	}

	// Provide the caller ASL tag list used by Popasl-derived requesters. These
	// are the documented pass-through ASL tags; the list is caller-owned and
	// never copied or freed by this family.
	public static bool SetAslTags<TPlatform>(ref TPlatform platform,
		APTR instance, APTR tags) where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsAslDerived(Classify(ref platform, instance))) return false;
		if (!MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return false;
		state.AslTags = tags;
		return MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
	}

	// ---- Layout / minmax / draw ----------------------------------------------

	// Bounded MUI_MinMax for the Popstring group: a horizontal string+button
	// composite that can grow. Writes the 12-byte MUI_MinMax (six UWORDs).
	public static bool AskMinMax<TPlatform>(ref TPlatform platform, APTR instance,
		APTR storage) where TPlatform : struct, IMuiGuestMemory
	{
		if (!Valid(ref platform, instance) || storage.IsNull ||
			!platform.IsMapped(storage, 12)) return false;
		var values = default(MuiMinMaxValues);
		values.MinWidth = 40;
		values.MinHeight = 10;
		values.MaxWidth = 10000;
		values.MaxHeight = 10;
		values.DefWidth = 120;
		values.DefHeight = 12;
		return MuiAreaLayoutCore.WriteMinMax(ref platform, storage, values);
	}

	// Group draw is a no-op here: the adopted String and Button children render
	// themselves through their own classes. Returns whether the object is a
	// drawable Pop instance.
	public static bool Draw<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiGuestMemory => Valid(ref platform, instance);

	// ---- Notification accessors ----------------------------------------------

	public static uint NotificationCount<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		MuiPopSpecialistStateCodec.TryRead(ref platform, instance, out var state)
			? state.NotifyCount : 0;

	public static uint LastNotifiedAttribute<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		MuiPopSpecialistStateCodec.TryRead(ref platform, instance, out var state)
			? state.NotifyAttribute : 0;

	public static uint LastNotifiedValue<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		MuiPopSpecialistStateCodec.TryRead(ref platform, instance, out var state)
			? state.NotifyValue : 0;

	// ---- Internals -----------------------------------------------------------

	// CallHookPkt marshalling seam. Delivers A0 = hook base, A2 = object,
	// A1 = message exactly as documented; the message scratch is class-owned and
	// carries a method-id + parameter so the callback can reach it through A1.
	private static void InvokeHook<TPlatform>(ref TPlatform platform,
		APTR instance, APTR hook, uint methodId, APTR a2Object, uint param1)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (hook.IsNull) return;
		if (!MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return;
		var msg = state.HookMsg;
		if (msg.IsNotNull && platform.IsMapped(msg, MuiPopSpecialistLayout.HookMsgSize))
		{
			platform.WriteUInt32(msg, MuiPopSpecialistLayout.MsgMethod, methodId);
			platform.WriteUInt32(msg, MuiPopSpecialistLayout.MsgParam1, param1);
			platform.WriteUInt32(msg, MuiPopSpecialistLayout.MsgParam2, 0);
		}
		platform.InvokeHook(hook, a2Object, msg);
	}

	private static void Notify<TPlatform>(ref TPlatform platform, APTR instance,
		uint attribute, uint value, bool isInit, bool notify, bool changed)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (isInit || !notify || !changed) return;
		if (!MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return;
		state.NotifyAttribute = attribute;
		state.NotifyValue = value;
		state.NotifyCount++;
		MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
	}

	private static bool WritePointerField<TPlatform>(ref TPlatform platform,
		APTR instance, ref MuiPopSpecialistState state, ref APTR field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (field.Raw == value) return false;
		field = APTR.FromPointer(value);
		MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
		return true;
	}

	private static bool SetFlag<TPlatform>(ref TPlatform platform, APTR instance,
		uint bit, bool set) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return false;
		var updated = set ? state.Flags | bit : state.Flags & ~bit;
		if (updated == state.Flags) return false;
		state.Flags = updated;
		MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
		return true;
	}

	// ---- Recursive class-owned disposal --------------------------------------

	// Recursively dispose everything the class owns: the adopted String and
	// Button children and (for Popobject-derived classes) the retained popup
	// Object are disposed through the BOOPSI seam; the materialized array, the
	// ASL service state (with any active requester released first), the volatile
	// popup window and the hook-message scratch are freed. Called by the family
	// lifecycle. The caller-owned source array and ASL tag list are never freed.
	internal static void DisposeOwned<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiServicePlatform
	{
		CancelPopup(ref platform, instance);

		if (!MuiPopSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return;
		var popObject = state.PopObject;
		if (popObject.IsNotNull)
		{
			platform.DisposeObject(popObject);
			state.PopObject = APTR.Null;
			MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
		}
		var button = state.ButtonChild;
		if (button.IsNotNull)
		{
			platform.DisposeObject(button);
			state.ButtonChild = APTR.Null;
			MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
		}
		var stringChild = state.StringChild;
		if (stringChild.IsNotNull)
		{
			platform.DisposeObject(stringChild);
			state.StringChild = APTR.Null;
			MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
		}

		FreeMaterializedArray(ref platform, instance);

		MuiPopSpecialistStateCodec.TryRead(ref platform, instance, out state);
		var aslState = state.AslState;
		if (aslState.IsNotNull)
		{
			Free(ref platform, aslState, MuiPopSpecialistLayout.AslStateSize);
			state.AslState = APTR.Null;
			MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
		}
		MuiPopSpecialistStateCodec.TryRead(ref platform, instance, out state);
		var hookMsg = state.HookMsg;
		if (hookMsg.IsNotNull)
		{
			Free(ref platform, hookMsg, MuiPopSpecialistLayout.HookMsgSize);
			state.HookMsg = APTR.Null;
			MuiPopSpecialistStateCodec.Write(ref platform, instance, state);
		}
	}

	// ---- Ownership (allocation with bounds check) ----------------------------

	internal static APTR Alloc<TPlatform>(ref TPlatform platform, uint size)
		where TPlatform : struct, IMuiExecCapability, IMuiGuestMemory
	{
		var result = platform.Allocate(size, 0x00010001);
		if (result.IsNull || !platform.IsMapped(result, size))
		{
			if (result.IsNotNull) platform.Free(result, size);
			return APTR.Null;
		}
		platform.Clear(result, size);
		return result;
	}

	internal static void Free<TPlatform>(ref TPlatform platform, APTR block,
		uint size) where TPlatform : struct, IMuiExecCapability, IMuiGuestMemory
	{
		if (block.IsNull) return;
		platform.Clear(block, size);
		platform.Free(block, size);
	}
}

// Official MG09 Pop* attribute and method identifiers, resolved from the
// authority (libraries/mui.h in the frozen MorphOS 3.20 SDK, mirrored in the
// abi-inventory). Kept beside the core so classification and dispatch stay
// byte-exact.
//
// Disposition notes required by the goal:
//  * MUIA_Popcolor_ShowAlpha (0x8042e102) is numerically identical to
//    MUIA_Coloradjust_ShowAlpha: Popcolor forwards ShowAlpha to its embedded
//    Coloradjust, so the same id is reused. Only this ABI-visible ShowAlpha
//    state plus the inherited Popobject behavior is implemented for Popcolor;
//    no further Popcolor-private semantics are invented.
//  * Poppen and the private Popscreen publish no own attributes in the
//    authority beyond their class ids. Their behavior is implemented only where
//    ABI-visible or state-observable (Poppen's cancel-on-Cleanup; Popscreen's
//    private Popasl specialization); undocumented internals are not invented.
public static class MuiPopAttributes
{
	// Shared Area attribute.
	public const uint Disabled = 0x80423661u;

	// Standard BOOPSI/MUI method identifiers handled by the family.
	public const uint Setup = 0x80428354u;
	public const uint Cleanup = 0x8042d985u;
	public const uint HandleInput = 0x80422a1au;

	// Popstring.mui
	public const uint Popstring_Close = 0x8042dc52u;   // MUIM
	public const uint Popstring_Open = 0x804258bau;    // MUIM
	public const uint Popstring_Button = 0x8042d0b9u;
	public const uint Popstring_CloseHook = 0x804256bfu;
	public const uint Popstring_OpenHook = 0x80429d00u;
	public const uint Popstring_String = 0x804239eau;
	public const uint Popstring_Toggle = 0x80422b7au;

	// Popobject.mui
	public const uint Popobject_Follow = 0x80424cb5u;
	public const uint Popobject_Light = 0x8042a5a3u;
	public const uint Popobject_Object = 0x804293e3u;
	public const uint Popobject_ObjStrHook = 0x8042db44u;
	public const uint Popobject_StrObjHook = 0x8042fbe1u;
	public const uint Popobject_Volatile = 0x804252ecu;
	public const uint Popobject_WindowHook = 0x8042f194u;

	// Poplist.mui
	public const uint Poplist_Array = 0x8042084cu;

	// Popasl.mui
	public const uint Popasl_Active = 0x80421b37u;
	public const uint Popasl_MUIFontStyles = 0x8042897fu;
	public const uint Popasl_StartHook = 0x8042b703u;
	public const uint Popasl_StopHook = 0x8042d8d2u;
	public const uint Popasl_Type = 0x8042df3du;

	// Popcolor.mui (ShowAlpha shares the Coloradjust ShowAlpha id).
	public const uint Popcolor_ShowAlpha = 0x8042e102u;

	// String contents (used by the Poplist selection-to-string notification).
	public const uint String_Contents = 0x80428ffdu;
}
