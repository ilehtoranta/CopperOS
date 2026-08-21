/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Fixed guest-memory layout for the final MG09 "misc" specialist family:
// Keyadjust.mui, Panel.mui, Filepanel.mui, Fontdisplay.mui, the private
// Scrmodelist.mui, Argstring.mui, Aboutmui.mui, Mccprefs.mui,
// FSProtectionBits.mui and Title.mui. Unlike the Pop* family these classes do
// not share a single MUI ancestor; each descends from its own documented base
// (see MuiMiscSpecialistCore comments). They are grouped here only because they
// share the additive standalone service surface (IMuiServicePlatform), an
// initialized guest-resident instance block discriminated by an exact,
// case-sensitive official class id, and identical failure-atomic ownership,
// notification and disposal machinery. Standalone calls never chain into the
// frozen common-control / collection / generic object cores; the optional
// factory path stores this same block behind one private headless sidecar
// attribute and lets the object lifecycle own its teardown. The family never
// allocates on the managed heap or holds managed data.
internal static class MuiMiscSpecialistLayout
{
	public const uint Magic = 0x4D4D5343;   // "MMSC"
	// Private headless-object attribute used when a Misc class is created through
	// MUI_NewObjectA. The value is a guest pointer to the fixed instance block;
	// it is never exposed as a public MUI attribute.
	public const uint SidecarAttribute = 0x7F4D5343u; // "MSC"

	// Instance block. One instance is exactly one class, so class-specific
	// regions never overlap for a given live object.
	public const uint InstanceSize = 196;

	// Keyadjust.mui : Group  (owns the Key description string).
	// The string slots are addressed by MuiMiscOwnedStringField below.

	// Argstring.mui : String  (owns Template and Contents).

	// Aboutmui.mui : Window  and Panel.mui : Group.
	public const int WindowPanelStateOffset = 48;

	// FSProtectionBits.mui : Group.
	public const int ProtectionStateOffset = 56;

	// Mccprefs.mui : Group  (bounded guest gadget registry).
	public const int MccprefsStateOffset = 60;

	// Title.mui : Group  (owned bounded page topology).
	public const int TitleStateOffset = 76; // named 28-byte title state block

	// Filepanel.mui : Panel  (owned strings, ASL integration, adopted rows).
	public const int FilepanelServiceStateOffset = 144;

	// Scrmodelist.mui : List (private) — bounded screenmode id records.
	public const int ScrmodelistStateOffset = 164;

	// Fontdisplay.mui : Area — last laid-out natural size only.
	public const int FontdisplaySizeOffset = 176;

	// Shared flag bits.
	public const uint FlagDisabled = 1u << 0;

	// Keyadjust policy flags.
	public const uint FlagKaMultipleKeys = 1u << 1;
	public const uint FlagKaDoubleClick = 1u << 2;
	public const uint FlagKaTripleClick = 1u << 3;
	public const uint FlagKaMouseEvents = 1u << 4;
	public const uint FlagKaForceKeyCode = 1u << 5;

	// Filepanel init booleans + runtime ASL state.
	public const uint FlagFpDoMultiSelect = 1u << 8;
	public const uint FlagFpDoPatterns = 1u << 9;
	public const uint FlagFpDoSaveMode = 1u << 10;
	public const uint FlagFpDrawersOnly = 1u << 11;
	public const uint FlagFpFilterDrawers = 1u << 12;
	public const uint FlagFpRejectIcons = 1u << 13;
	public const uint FlagFpAslActive = 1u << 14;

	// Title flags.
	public const uint FlagTiClickable = 1u << 16;
	public const uint FlagTiClosable = 1u << 17;
	public const uint FlagTiNewable = 1u << 18;
	public const uint FlagTiSortable = 1u << 19;

	// Aboutmui / Panel lifetime.
	public const uint FlagAboutOpen = 1u << 24;
	public const uint FlagPanelRan = 1u << 25;
	// Generic MUI lifecycle state. Setup is idempotent and Cleanup clears this
	// bit after cancelling any owned Filepanel ASL state.
	public const uint FlagSetupActive = 1u << 26;

	// Owned-block sizes and bounds.
	public const uint AslStateSize = MuiAslServiceStateRecord.Size;
	public const uint HookMsgSize = 16;
	public const int MsgMethod = 0;
	public const int MsgParam1 = 4;
	public const int MsgParam2 = 8;

	public const uint PageRecordSize = 8;   // { handle, flags }
	public const int MaximumPages = 64;

	public const uint RegistryRecordSize = 24; // { gadget,id,params,title,attr,label }
	public const int MaximumRegistry = 64;

	public const uint RowRecordSize = 8;    // { label, contents } (adopted)
	public const int MaximumRows = 64;

	public const uint ModeRecordSize = 4;   // { modeId }
	public const int MaximumModes = 256;

	public const uint MaximumString = 4096;
}

// Named view of one class-specific region inside the fixed Misc specialist
// instance block. Callers select a semantic region rather than repeating its
// byte offset; the codec below owns the one ABI layout mapping and overflow
// check.
internal enum MuiMiscStateRegion : byte
{
	Title,
	FilepanelService,
	Mccprefs,
	Scrmodelist,
	WindowPanel,
	Protection,
	Fontdisplay,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscStateCursor
{
	internal APTR Instance;
	internal MuiMiscStateRegion Region;
}

internal static class MuiMiscStateCursorCodec
{
	internal static bool TryGetAddress(MuiMiscStateCursor cursor,
		out APTR address)
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Region)
		{
			case MuiMiscStateRegion.Title:
				offset = unchecked((uint)MuiMiscSpecialistLayout.TitleStateOffset);
				break;
			case MuiMiscStateRegion.FilepanelService:
				offset = unchecked((uint)MuiMiscSpecialistLayout.FilepanelServiceStateOffset);
				break;
			case MuiMiscStateRegion.Mccprefs:
				offset = unchecked((uint)MuiMiscSpecialistLayout.MccprefsStateOffset);
				break;
			case MuiMiscStateRegion.Scrmodelist:
				offset = unchecked((uint)MuiMiscSpecialistLayout.ScrmodelistStateOffset);
				break;
			case MuiMiscStateRegion.WindowPanel:
				offset = unchecked((uint)MuiMiscSpecialistLayout.WindowPanelStateOffset);
				break;
			case MuiMiscStateRegion.Protection:
				offset = unchecked((uint)MuiMiscSpecialistLayout.ProtectionStateOffset);
				break;
			case MuiMiscStateRegion.Fontdisplay:
				offset = unchecked((uint)MuiMiscSpecialistLayout.FontdisplaySizeOffset);
				break;
			default:
				return false;
		}
		if (cursor.Instance.IsNull || cursor.Instance.Raw >
			uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Instance.Raw + offset);
		return true;
	}
}

// Named common header for every Misc specialist instance. Class, flags, and
// notification state occupy the first 24 guest bytes; class-specific regions
// follow at the documented layout positions below. Keep these shared fields as
// a record so ordinary specialist logic never decodes anonymous ULONG slots.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscSpecialistHeader
{
	internal const uint Size = 24;
	internal const uint Cookie = MuiMiscSpecialistLayout.Magic;

	internal uint Magic;
	internal uint Class;
	internal uint Flags;
	internal uint NotifyAttribute;
	internal uint NotifyValue;
	internal uint NotifyCount;
}

internal enum MuiMiscRecordKind : byte
{
	Header,
	Title,
	FilepanelService,
	OwnedStringSlot,
	Mccprefs,
	Scrmodelist,
	WindowPanel,
	Fontdisplay,
	TitlePage,
	MccprefsRegistry,
	FilepanelRow,
}

internal enum MuiMiscRecordField : byte
{
	Magic,
	Class,
	Flags,
	NotifyAttribute,
	NotifyValue,
	NotifyCount,
	Pages,
	PageCount,
	ActivePage,
	PageSequence,
	Position,
	EventPriority,
	OnLastClose,
	FilterFunc,
	AslState,
	Rows,
	RowCount,
	HookMsg,
	Value,
	AllocationSize,
	Registry,
	RegistryCount,
	RegistryConfig,
	RegistryOriginator,
	Modes,
	ModeCount,
	ActiveMode,
	Application,
	PanelWindow,
	Width,
	Height,
	Handle,
	PageFlags,
	Gadget,
	Id,
	Params,
	Title,
	Attr,
	Label,
	Contents,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscRecordFieldCursor
{
	internal APTR Address;
	internal MuiMiscRecordKind Record;
	internal MuiMiscRecordField Field;
}

internal static class MuiMiscRecordFieldCursorCodec
{
	private static bool TryResolve(MuiMiscRecordKind record,
		MuiMiscRecordField field, out uint offset, out uint size)
	{
		offset = 0;
		size = 0;
		switch (record)
		{
			case MuiMiscRecordKind.Header:
				switch (field)
				{
					case MuiMiscRecordField.Magic:
						offset = 0;
						size = MuiMiscSpecialistHeader.Size;
						return true;
					case MuiMiscRecordField.Class:
						offset = 4;
						size = MuiMiscSpecialistHeader.Size;
						return true;
					case MuiMiscRecordField.Flags:
						offset = 8;
						size = MuiMiscSpecialistHeader.Size;
						return true;
					case MuiMiscRecordField.NotifyAttribute:
						offset = 12;
						size = MuiMiscSpecialistHeader.Size;
						return true;
					case MuiMiscRecordField.NotifyValue:
						offset = 16;
						size = MuiMiscSpecialistHeader.Size;
						return true;
					case MuiMiscRecordField.NotifyCount:
						offset = 20;
						size = MuiMiscSpecialistHeader.Size;
						return true;
				}
				break;
			case MuiMiscRecordKind.Title:
				switch (field)
				{
					case MuiMiscRecordField.Pages:
						offset = 0;
						size = MuiMiscTitleState.Size;
						return true;
					case MuiMiscRecordField.PageCount:
						offset = 4;
						size = MuiMiscTitleState.Size;
						return true;
					case MuiMiscRecordField.ActivePage:
						offset = 8;
						size = MuiMiscTitleState.Size;
						return true;
					case MuiMiscRecordField.PageSequence:
						offset = 12;
						size = MuiMiscTitleState.Size;
						return true;
					case MuiMiscRecordField.Position:
						offset = 16;
						size = MuiMiscTitleState.Size;
						return true;
					case MuiMiscRecordField.EventPriority:
						offset = 20;
						size = MuiMiscTitleState.Size;
						return true;
					case MuiMiscRecordField.OnLastClose:
						offset = 24;
						size = MuiMiscTitleState.Size;
						return true;
				}
				break;
			case MuiMiscRecordKind.FilepanelService:
				switch (field)
				{
					case MuiMiscRecordField.FilterFunc:
						offset = 0;
						size = MuiMiscFilepanelServiceState.Size;
						return true;
					case MuiMiscRecordField.AslState:
						offset = 4;
						size = MuiMiscFilepanelServiceState.Size;
						return true;
					case MuiMiscRecordField.Rows:
						offset = 8;
						size = MuiMiscFilepanelServiceState.Size;
						return true;
					case MuiMiscRecordField.RowCount:
						offset = 12;
						size = MuiMiscFilepanelServiceState.Size;
						return true;
					case MuiMiscRecordField.HookMsg:
						offset = 16;
						size = MuiMiscFilepanelServiceState.Size;
						return true;
				}
				break;
			case MuiMiscRecordKind.OwnedStringSlot:
				switch (field)
				{
					case MuiMiscRecordField.Value:
						offset = 0;
						size = MuiMiscOwnedStringSlot.Size;
						return true;
					case MuiMiscRecordField.AllocationSize:
						offset = 4;
						size = MuiMiscOwnedStringSlot.Size;
						return true;
				}
				break;
			case MuiMiscRecordKind.Mccprefs:
				switch (field)
				{
					case MuiMiscRecordField.Registry:
						offset = 0;
						size = MuiMiscMccprefsState.Size;
						return true;
					case MuiMiscRecordField.RegistryCount:
						offset = 4;
						size = MuiMiscMccprefsState.Size;
						return true;
					case MuiMiscRecordField.RegistryConfig:
						offset = 8;
						size = MuiMiscMccprefsState.Size;
						return true;
					case MuiMiscRecordField.RegistryOriginator:
						offset = 12;
						size = MuiMiscMccprefsState.Size;
						return true;
				}
				break;
			case MuiMiscRecordKind.Scrmodelist:
				switch (field)
				{
					case MuiMiscRecordField.Modes:
						offset = 0;
						size = MuiMiscScrmodelistState.Size;
						return true;
					case MuiMiscRecordField.ModeCount:
						offset = 4;
						size = MuiMiscScrmodelistState.Size;
						return true;
					case MuiMiscRecordField.ActiveMode:
						offset = 8;
						size = MuiMiscScrmodelistState.Size;
						return true;
				}
				break;
			case MuiMiscRecordKind.WindowPanel:
				switch (field)
				{
					case MuiMiscRecordField.Application:
						offset = 0;
						size = MuiMiscWindowPanelState.Size;
						return true;
					case MuiMiscRecordField.PanelWindow:
						offset = 4;
						size = MuiMiscWindowPanelState.Size;
						return true;
				}
				break;
			case MuiMiscRecordKind.Fontdisplay:
				switch (field)
				{
					case MuiMiscRecordField.Width:
						offset = 0;
						size = MuiMiscFontdisplaySize.Size;
						return true;
					case MuiMiscRecordField.Height:
						offset = 4;
						size = MuiMiscFontdisplaySize.Size;
						return true;
				}
				break;
			case MuiMiscRecordKind.TitlePage:
				switch (field)
				{
					case MuiMiscRecordField.Handle:
						offset = 0;
						size = MuiTitlePageRecord.Size;
						return true;
					case MuiMiscRecordField.PageFlags:
						offset = 4;
						size = MuiTitlePageRecord.Size;
						return true;
				}
				break;
			case MuiMiscRecordKind.MccprefsRegistry:
				switch (field)
				{
					case MuiMiscRecordField.Gadget:
						offset = 0;
						size = MuiMccprefsRegistryRecord.Size;
						return true;
					case MuiMiscRecordField.Id:
						offset = 4;
						size = MuiMccprefsRegistryRecord.Size;
						return true;
					case MuiMiscRecordField.Params:
						offset = 8;
						size = MuiMccprefsRegistryRecord.Size;
						return true;
					case MuiMiscRecordField.Title:
						offset = 12;
						size = MuiMccprefsRegistryRecord.Size;
						return true;
					case MuiMiscRecordField.Attr:
						offset = 16;
						size = MuiMccprefsRegistryRecord.Size;
						return true;
					case MuiMiscRecordField.Label:
						offset = 20;
						size = MuiMccprefsRegistryRecord.Size;
						return true;
				}
				break;
			case MuiMiscRecordKind.FilepanelRow:
				switch (field)
				{
					case MuiMiscRecordField.Label:
						offset = 0;
						size = MuiFilepanelRowRecord.Size;
						return true;
					case MuiMiscRecordField.Contents:
						offset = 4;
						size = MuiFilepanelRowRecord.Size;
						return true;
				}
				break;
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMiscRecordFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Record, cursor.Field, out var offset,
			out var size) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address, size)) return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiMiscRecordKind record, MuiMiscRecordField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiMiscRecordFieldCursor);
		cursor.Address = address;
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress)) return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiMiscRecordKind record, MuiMiscRecordField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiMiscRecordFieldCursor);
		cursor.Address = address;
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress)) return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiMiscSpecialistHeaderCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiMiscSpecialistHeader value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiMiscSpecialistHeader.Size) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.Header, MuiMiscRecordField.Magic,
				out var magic) || magic != MuiMiscSpecialistHeader.Cookie)
			return false;
		value.Magic = MuiMiscSpecialistHeader.Cookie;
		if (!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiMiscRecordKind.Header, MuiMiscRecordField.Class, out value.Class) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.Header, MuiMiscRecordField.Flags, out value.Flags) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.Header, MuiMiscRecordField.NotifyAttribute,
				out value.NotifyAttribute) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.Header, MuiMiscRecordField.NotifyValue,
				out value.NotifyValue) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.Header, MuiMiscRecordField.NotifyCount,
				out value.NotifyCount)) return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiMiscSpecialistHeader value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiMiscSpecialistHeader.Size) || value.Magic !=
			MuiMiscSpecialistHeader.Cookie) return false;
		return MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiMiscRecordKind.Header, MuiMiscRecordField.Magic, value.Magic) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.Header, MuiMiscRecordField.Class, value.Class) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.Header, MuiMiscRecordField.Flags, value.Flags) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.Header, MuiMiscRecordField.NotifyAttribute,
				value.NotifyAttribute) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.Header, MuiMiscRecordField.NotifyValue,
				value.NotifyValue) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.Header, MuiMiscRecordField.NotifyCount,
				value.NotifyCount);
	}
}

// Named Title-specific state block. It starts at the TitleStateOffset within
// the complete Misc instance and keeps page topology plus the public Title
// scalar attributes together as one guest record.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscTitleState
{
	internal const uint Size = 28;
	internal APTR Pages;
	internal uint PageCount;
	internal uint ActivePage;
	internal uint PageSequence;
	internal uint Position;
	internal uint EventPriority;
	internal uint OnLastClose;
}

internal static class MuiMiscTitleStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiMiscTitleState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiMiscTitleState.Size)) return false;
		if (!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiMiscRecordKind.Title, MuiMiscRecordField.Pages, out var pages) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.Title, MuiMiscRecordField.PageCount,
				out value.PageCount) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.Title, MuiMiscRecordField.ActivePage,
				out value.ActivePage) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.Title, MuiMiscRecordField.PageSequence,
				out value.PageSequence) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.Title, MuiMiscRecordField.Position,
				out value.Position) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.Title, MuiMiscRecordField.EventPriority,
				out value.EventPriority) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.Title, MuiMiscRecordField.OnLastClose,
				out value.OnLastClose)) return false;
		value.Pages = APTR.FromPointer(pages);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiMiscTitleState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiMiscTitleState.Size)) return false;
		return MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiMiscRecordKind.Title, MuiMiscRecordField.Pages, value.Pages.Raw) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.Title, MuiMiscRecordField.PageCount,
				value.PageCount) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.Title, MuiMiscRecordField.ActivePage,
				value.ActivePage) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.Title, MuiMiscRecordField.PageSequence,
				value.PageSequence) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.Title, MuiMiscRecordField.Position,
				value.Position) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.Title, MuiMiscRecordField.EventPriority,
				value.EventPriority) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.Title, MuiMiscRecordField.OnLastClose,
				value.OnLastClose);
	}
}

// Named Filepanel service-state tail. The five fields cover the FilterFunc
// hook, ASL service state, adopted-row table/count, and hook-message scratch.
// Owned strings remain separate slots and are intentionally outside this
// bounded service record.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscFilepanelServiceState
{
	internal const uint Size = 20;
	internal APTR FilterFunc;
	internal APTR AslState;
	internal APTR Rows;
	internal uint RowCount;
	internal APTR HookMsg;
}

internal static class MuiMiscFilepanelServiceStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiMiscFilepanelServiceState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiMiscFilepanelServiceState.Size)) return false;
		if (!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiMiscRecordKind.FilepanelService, MuiMiscRecordField.FilterFunc,
			out var filterFunc) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.FilepanelService, MuiMiscRecordField.AslState,
				out var aslState) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.FilepanelService, MuiMiscRecordField.Rows,
				out var rows) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.FilepanelService, MuiMiscRecordField.RowCount,
				out value.RowCount) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.FilepanelService, MuiMiscRecordField.HookMsg,
				out var hookMsg)) return false;
		value.FilterFunc = APTR.FromPointer(filterFunc);
		value.AslState = APTR.FromPointer(aslState);
		value.Rows = APTR.FromPointer(rows);
		value.HookMsg = APTR.FromPointer(hookMsg);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiMiscFilepanelServiceState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiMiscFilepanelServiceState.Size)) return false;
		return MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiMiscRecordKind.FilepanelService, MuiMiscRecordField.FilterFunc,
			value.FilterFunc.Raw) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.FilepanelService, MuiMiscRecordField.AslState,
				value.AslState.Raw) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.FilepanelService, MuiMiscRecordField.Rows,
				value.Rows.Raw) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.FilepanelService, MuiMiscRecordField.RowCount,
				value.RowCount) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.FilepanelService, MuiMiscRecordField.HookMsg,
				value.HookMsg.Raw);
	}
}

// Every class-owned C string is stored as one named guest slot containing the
// copied STRPTR and its byte allocation size. The slot codec is shared by
// Keyadjust, Argstring, and Filepanel; the field selector keeps the sparse
// instance layout out of normal ownership and attribute logic.
internal enum MuiMiscOwnedStringField : uint
{
	Key = 0,
	ArgTemplate = 1,
	ArgContents = 2,
	FilepanelDrawer = 3,
	FilepanelFile = 4,
	FilepanelPattern = 5,
	FilepanelAcceptPattern = 6,
	FilepanelRejectPattern = 7,
}

// Named cursor for the sparse owned-string slots in the Misc instance block.
// The semantic field selector keeps raw slot offsets in one ABI adapter while
// ownership and attribute logic works with the field identity itself.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscOwnedStringCursor
{
	internal APTR Instance;
	internal MuiMiscOwnedStringField Field;
}

internal static class MuiMiscOwnedStringCursorCodec
{
	internal static bool TryGetAddress(MuiMiscOwnedStringCursor cursor,
		out APTR address)
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Field)
		{
			case MuiMiscOwnedStringField.Key: offset = 24; break;
			case MuiMiscOwnedStringField.ArgTemplate: offset = 32; break;
			case MuiMiscOwnedStringField.ArgContents: offset = 40; break;
			case MuiMiscOwnedStringField.FilepanelDrawer: offset = 104; break;
			case MuiMiscOwnedStringField.FilepanelFile: offset = 112; break;
			case MuiMiscOwnedStringField.FilepanelPattern: offset = 120; break;
			case MuiMiscOwnedStringField.FilepanelAcceptPattern: offset = 128; break;
			case MuiMiscOwnedStringField.FilepanelRejectPattern: offset = 136; break;
			default: return false;
		}
		if (cursor.Instance.IsNull || cursor.Instance.Raw >
			uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Instance.Raw + offset);
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscOwnedStringSlot
{
	internal const uint Size = 8;
	internal APTR Value;
	internal uint AllocationSize;
}

internal static class MuiMiscOwnedStringSlotCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiMiscOwnedStringSlot value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiMiscOwnedStringSlot.Size)) return false;
		if (!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiMiscRecordKind.OwnedStringSlot, MuiMiscRecordField.Value,
			out var stringValue) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.OwnedStringSlot,
				MuiMiscRecordField.AllocationSize, out value.AllocationSize))
			return false;
		value.Value = APTR.FromPointer(stringValue);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiMiscOwnedStringSlot value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiMiscOwnedStringSlot.Size)) return false;
		return MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiMiscRecordKind.OwnedStringSlot, MuiMiscRecordField.Value,
			value.Value.Raw) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.OwnedStringSlot,
				MuiMiscRecordField.AllocationSize, value.AllocationSize);
	}
}

// Named Mccprefs-specific state block: the owned bounded registry plus the
// caller-provided config and originator references observed by the two config
// transfer methods.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscMccprefsState
{
	internal const uint Size = 16;
	internal APTR Registry;
	internal uint RegistryCount;
	internal APTR RegistryConfig;
	internal APTR RegistryOriginator;
}

internal static class MuiMiscMccprefsStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiMiscMccprefsState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiMiscMccprefsState.Size)) return false;
		if (!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiMiscRecordKind.Mccprefs, MuiMiscRecordField.Registry,
			out var registry) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.Mccprefs, MuiMiscRecordField.RegistryCount,
				out value.RegistryCount) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.Mccprefs, MuiMiscRecordField.RegistryConfig,
				out var registryConfig) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.Mccprefs,
				MuiMiscRecordField.RegistryOriginator,
				out var registryOriginator)) return false;
		value.Registry = APTR.FromPointer(registry);
		value.RegistryConfig = APTR.FromPointer(registryConfig);
		value.RegistryOriginator = APTR.FromPointer(registryOriginator);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiMiscMccprefsState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiMiscMccprefsState.Size)) return false;
		return MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiMiscRecordKind.Mccprefs, MuiMiscRecordField.Registry,
			value.Registry.Raw) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.Mccprefs, MuiMiscRecordField.RegistryCount,
				value.RegistryCount) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.Mccprefs, MuiMiscRecordField.RegistryConfig,
				value.RegistryConfig.Raw) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.Mccprefs,
				MuiMiscRecordField.RegistryOriginator,
				value.RegistryOriginator.Raw);
	}
}

// Named private Scrmodelist state block: bounded mode storage, count, and the
// active-mode index (the latter remains zero until a future documented action
// selects a mode).
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscScrmodelistState
{
	internal const uint Size = 12;
	internal APTR Modes;
	internal uint ModeCount;
	internal uint ActiveMode;
}

internal static class MuiMiscScrmodelistStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiMiscScrmodelistState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiMiscScrmodelistState.Size)) return false;
		if (!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiMiscRecordKind.Scrmodelist, MuiMiscRecordField.Modes,
			out var modes) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.Scrmodelist, MuiMiscRecordField.ModeCount,
				out value.ModeCount) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.Scrmodelist, MuiMiscRecordField.ActiveMode,
				out value.ActiveMode)) return false;
		value.Modes = APTR.FromPointer(modes);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiMiscScrmodelistState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiMiscScrmodelistState.Size)) return false;
		return MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiMiscRecordKind.Scrmodelist, MuiMiscRecordField.Modes,
			value.Modes.Raw) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.Scrmodelist, MuiMiscRecordField.ModeCount,
				value.ModeCount) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.Scrmodelist, MuiMiscRecordField.ActiveMode,
				value.ActiveMode);
	}
}

// Named shared pointer block for Aboutmui's referenced Application and
// Panel's last-run Application/Window pair. Neither pointer is owned here.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscWindowPanelState
{
	internal const uint Size = 8;
	internal APTR Application;
	internal APTR PanelWindow;
}

internal static class MuiMiscWindowPanelStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiMiscWindowPanelState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiMiscWindowPanelState.Size)) return false;
		if (!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiMiscRecordKind.WindowPanel, MuiMiscRecordField.Application,
			out var application) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.WindowPanel, MuiMiscRecordField.PanelWindow,
				out var panelWindow)) return false;
		value.Application = APTR.FromPointer(application);
		value.PanelWindow = APTR.FromPointer(panelWindow);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiMiscWindowPanelState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiMiscWindowPanelState.Size)) return false;
		return MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiMiscRecordKind.WindowPanel, MuiMiscRecordField.Application,
			value.Application.Raw) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.WindowPanel, MuiMiscRecordField.PanelWindow,
				value.PanelWindow.Raw);
	}
}

// Named FSProtectionBits scalar state. Keeping the single flags ULONG behind
// a record/codec makes its wire boundary explicit without inventing extra
// protection semantics.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscProtectionState
{
	internal const uint Size = 4;
	internal uint Flags;
}

internal static class MuiMiscProtectionStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiMiscProtectionState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiMiscProtectionState.Size)) return false;
		value.Flags = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiMiscProtectionState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiMiscProtectionState.Size)) return false;
		platform.WriteUInt32(address, 0, value.Flags);
		return true;
	}
}

// Named Fontdisplay natural-size state recorded by MUIM_Draw.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscFontdisplaySize
{
	internal const uint Size = 8;
	internal uint Width;
	internal uint Height;
}

internal static class MuiMiscFontdisplaySizeCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiMiscFontdisplaySize value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiMiscFontdisplaySize.Size)) return false;
		if (!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiMiscRecordKind.Fontdisplay, MuiMiscRecordField.Width,
			out value.Width) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.Fontdisplay, MuiMiscRecordField.Height,
				out value.Height)) return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiMiscFontdisplaySize value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiMiscFontdisplaySize.Size)) return false;
		return MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiMiscRecordKind.Fontdisplay, MuiMiscRecordField.Width, value.Width) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.Fontdisplay, MuiMiscRecordField.Height,
				value.Height);
	}
}

// The misc-family discriminator. Values are ordinal; the exact official class
// ids and inheritance are resolved by MuiMiscSpecialistCore.
public enum MuiMiscSpecialistClass : uint
{
	None = 0,
	Keyadjust = 1,        // : Group
	Panel = 2,            // : Group
	Filepanel = 3,        // : Panel
	Fontdisplay = 4,      // : Area
	Scrmodelist = 5,      // : List  (private)
	Argstring = 6,        // : String
	Aboutmui = 7,         // : Window
	Mccprefs = 8,         // : Group
	FSProtectionBits = 9, // : Group
	Title = 10,           // : Group
}

// Title owns a bounded page topology. Each page slot carries a synthetic
// handle and its flags as two ULONG fields; keep compaction and lookup on this
// named record boundary.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTitlePageRecord
{
	internal const uint Size = 8;
	internal uint Handle;
	internal uint Flags;
}

internal static class MuiTitlePageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiTitlePageRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiTitlePageRecord.Size)) return false;
		if (!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiMiscRecordKind.TitlePage, MuiMiscRecordField.Handle,
			out record.Handle) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.TitlePage, MuiMiscRecordField.PageFlags,
				out record.Flags)) return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiTitlePageRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiTitlePageRecord.Size)) return false;
		return MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiMiscRecordKind.TitlePage, MuiMiscRecordField.Handle,
			record.Handle) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.TitlePage, MuiMiscRecordField.PageFlags,
				record.Flags);
	}
}

// Title owns a bounded contiguous page table. Keep page indexing behind one
// named cursor so creation, compaction, close, and lookup share one
// overflow-checked guest boundary instead of rebuilding `base + index * 8`.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTitlePageCursor
{
	internal const uint EntrySize = MuiTitlePageRecord.Size;
	internal const uint MaximumEntries = MuiMiscSpecialistLayout.MaximumPages;
	internal APTR Base;
	internal uint Index;
}

internal static class MuiTitlePageCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiTitlePageCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >=
			MuiTitlePageCursor.MaximumEntries || cursor.Index >
			(uint.MaxValue - cursor.Base.Raw) /
			MuiTitlePageCursor.EntrySize) return false;
		var offset = cursor.Index * MuiTitlePageCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address, MuiTitlePageCursor.EntrySize);
	}
}

// Mccprefs keeps caller-owned gadget registrations in a fixed six-field table
// entry. Keep registration replacement and removal on this named record
// boundary rather than duplicating each member offset.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMccprefsRegistryRecord
{
	internal const uint Size = 24;
	internal APTR Gadget;
	internal uint Id;
	internal uint Params;
	internal APTR Title;
	internal uint Attr;
	internal APTR Label;
}

internal static class MuiMccprefsRegistryCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiMccprefsRegistryRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiMccprefsRegistryRecord.Size)) return false;
		if (!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiMiscRecordKind.MccprefsRegistry, MuiMiscRecordField.Gadget,
			out var gadget) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.MccprefsRegistry, MuiMiscRecordField.Id,
				out record.Id) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.MccprefsRegistry, MuiMiscRecordField.Params,
				out record.Params) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.MccprefsRegistry, MuiMiscRecordField.Title,
				out var title) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.MccprefsRegistry, MuiMiscRecordField.Attr,
				out record.Attr) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.MccprefsRegistry, MuiMiscRecordField.Label,
				out var label)) return false;
		record.Gadget = APTR.FromPointer(gadget);
		record.Title = APTR.FromPointer(title);
		record.Label = APTR.FromPointer(label);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiMccprefsRegistryRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiMccprefsRegistryRecord.Size)) return false;
		return MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiMiscRecordKind.MccprefsRegistry, MuiMiscRecordField.Gadget,
			record.Gadget.Raw) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.MccprefsRegistry, MuiMiscRecordField.Id,
				record.Id) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.MccprefsRegistry, MuiMiscRecordField.Params,
				record.Params) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.MccprefsRegistry, MuiMiscRecordField.Title,
				record.Title.Raw) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.MccprefsRegistry, MuiMiscRecordField.Attr,
				record.Attr) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.MccprefsRegistry, MuiMiscRecordField.Label,
				record.Label.Raw);
	}
}

// Mccprefs owns a bounded contiguous table of the named registry records.
// Keep the slot index in a cursor so registration, replacement, removal, and
// disposal share one overflow-checked guest boundary rather than rebuilding
// `base + index * 24` at each call site.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMccprefsRegistryCursor
{
	internal const uint EntrySize = MuiMccprefsRegistryRecord.Size;
	internal const uint MaximumEntries =
		MuiMiscSpecialistLayout.MaximumRegistry;
	internal APTR Base;
	internal uint Index;
}

internal static class MuiMccprefsRegistryCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiMccprefsRegistryCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >=
			MuiMccprefsRegistryCursor.MaximumEntries || cursor.Index >
			(uint.MaxValue - cursor.Base.Raw) /
			MuiMccprefsRegistryCursor.EntrySize) return false;
		var offset = cursor.Index * MuiMccprefsRegistryCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address,
			MuiMccprefsRegistryCursor.EntrySize);
	}
}

// Scrmodelist is private but still owns a bounded guest table of mode IDs.
// Keep append and indexed lookup on a named scalar record boundary.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiScrmodelistModeRecord
{
	internal const uint Size = 4;
	internal uint ModeId;
}

internal static class MuiScrmodelistModeCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiScrmodelistModeRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiScrmodelistModeRecord.Size)) return false;
		record.ModeId = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiScrmodelistModeRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiScrmodelistModeRecord.Size)) return false;
		platform.WriteUInt32(address, 0, record.ModeId);
		return true;
	}
}

// Scrmodelist keeps a bounded contiguous table of private mode-id records.
// Keep the slot index in a named cursor so append and indexed lookup share one
// overflow-checked guest boundary rather than rebuilding `base + index * 4`.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiScrmodelistModeCursor
{
	internal const uint EntrySize = MuiScrmodelistModeRecord.Size;
	internal const uint MaximumEntries = MuiMiscSpecialistLayout.MaximumModes;
	internal APTR Base;
	internal uint Index;
}

internal static class MuiScrmodelistModeCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiScrmodelistModeCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >=
			MuiScrmodelistModeCursor.MaximumEntries || cursor.Index >
			(uint.MaxValue - cursor.Base.Raw) /
			MuiScrmodelistModeCursor.EntrySize) return false;
		var offset = cursor.Index * MuiScrmodelistModeCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address,
			MuiScrmodelistModeCursor.EntrySize);
	}
}

// Filepanel adopts each row's two object pointers in a fixed guest-resident
// table. Keep the row element as a named record so add/dispose paths do not
// duplicate the packed label/contents offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFilepanelRowRecord
{
	internal const uint Size = 8;
	internal APTR Label;
	internal APTR Contents;
}

internal static class MuiFilepanelRowCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiFilepanelRowRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiFilepanelRowRecord.Size)) return false;
		if (!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiMiscRecordKind.FilepanelRow, MuiMiscRecordField.Label,
			out var label) ||
			!MuiMiscRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMiscRecordKind.FilepanelRow, MuiMiscRecordField.Contents,
				out var contents)) return false;
		record.Label = APTR.FromPointer(label);
		record.Contents = APTR.FromPointer(contents);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiFilepanelRowRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiFilepanelRowRecord.Size)) return false;
		return MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiMiscRecordKind.FilepanelRow, MuiMiscRecordField.Label,
			record.Label.Raw) &&
			MuiMiscRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMiscRecordKind.FilepanelRow, MuiMiscRecordField.Contents,
				record.Contents.Raw);
	}
}

// Filepanel owns a bounded contiguous table of adopted row records. Keep the
// row index in a cursor so insertion and disposal share one overflow-checked
// guest boundary instead of rebuilding `base + index * 8` independently.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFilepanelRowCursor
{
	internal const uint EntrySize = MuiFilepanelRowRecord.Size;
	internal const uint MaximumEntries = MuiMiscSpecialistLayout.MaximumRows;
	internal APTR Base;
	internal uint Index;
}

internal static class MuiFilepanelRowCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiFilepanelRowCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >=
			MuiFilepanelRowCursor.MaximumEntries || cursor.Index >
			(uint.MaxValue - cursor.Base.Raw) /
			MuiFilepanelRowCursor.EntrySize) return false;
		var offset = cursor.Index * MuiFilepanelRowCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address, MuiFilepanelRowCursor.EntrySize);
	}
}

public static class MuiMiscSpecialistCore
{
	// ---- Classification ------------------------------------------------------

	// Classify a guest C-string class id against the exact official names. The
	// loader contract is case-sensitive, so the match is byte-exact against the
	// documented "<Name>.mui" ids. Freestanding: bytes are compared as ASCII
	// literals with no managed strings, arrays or spans.
	public static MuiMiscSpecialistClass ClassifyName<TPlatform>(
		ref TPlatform platform, APTR classId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (classId.IsNull) return MuiMiscSpecialistClass.None;
		switch (B(ref platform, classId, 0))
		{
			case 'K':
				if (M(ref platform, classId, 1, 'e') && M(ref platform, classId, 2, 'y') &&
					M(ref platform, classId, 3, 'a') && M(ref platform, classId, 4, 'd') &&
					M(ref platform, classId, 5, 'j') && M(ref platform, classId, 6, 'u') &&
					M(ref platform, classId, 7, 's') && M(ref platform, classId, 8, 't') &&
					Suffix(ref platform, classId, 9))
					return MuiMiscSpecialistClass.Keyadjust;
				break;
			case 'P':
				if (M(ref platform, classId, 1, 'a') && M(ref platform, classId, 2, 'n') &&
					M(ref platform, classId, 3, 'e') && M(ref platform, classId, 4, 'l') &&
					Suffix(ref platform, classId, 5))
					return MuiMiscSpecialistClass.Panel;
				break;
			case 'F':
				// Filepanel / Fontdisplay / FSProtectionBits
				if (M(ref platform, classId, 1, 'i') && M(ref platform, classId, 2, 'l') &&
					M(ref platform, classId, 3, 'e') && M(ref platform, classId, 4, 'p') &&
					M(ref platform, classId, 5, 'a') && M(ref platform, classId, 6, 'n') &&
					M(ref platform, classId, 7, 'e') && M(ref platform, classId, 8, 'l') &&
					Suffix(ref platform, classId, 9))
					return MuiMiscSpecialistClass.Filepanel;
				if (M(ref platform, classId, 1, 'o') && M(ref platform, classId, 2, 'n') &&
					M(ref platform, classId, 3, 't') && M(ref platform, classId, 4, 'd') &&
					M(ref platform, classId, 5, 'i') && M(ref platform, classId, 6, 's') &&
					M(ref platform, classId, 7, 'p') && M(ref platform, classId, 8, 'l') &&
					M(ref platform, classId, 9, 'a') && M(ref platform, classId, 10, 'y') &&
					Suffix(ref platform, classId, 11))
					return MuiMiscSpecialistClass.Fontdisplay;
				if (M(ref platform, classId, 1, 'S') && M(ref platform, classId, 2, 'P') &&
					M(ref platform, classId, 3, 'r') && M(ref platform, classId, 4, 'o') &&
					M(ref platform, classId, 5, 't') && M(ref platform, classId, 6, 'e') &&
					M(ref platform, classId, 7, 'c') && M(ref platform, classId, 8, 't') &&
					M(ref platform, classId, 9, 'i') && M(ref platform, classId, 10, 'o') &&
					M(ref platform, classId, 11, 'n') && M(ref platform, classId, 12, 'B') &&
					M(ref platform, classId, 13, 'i') && M(ref platform, classId, 14, 't') &&
					M(ref platform, classId, 15, 's') && Suffix(ref platform, classId, 16))
					return MuiMiscSpecialistClass.FSProtectionBits;
				break;
			case 'S':
				if (M(ref platform, classId, 1, 'c') && M(ref platform, classId, 2, 'r') &&
					M(ref platform, classId, 3, 'm') && M(ref platform, classId, 4, 'o') &&
					M(ref platform, classId, 5, 'd') && M(ref platform, classId, 6, 'e') &&
					M(ref platform, classId, 7, 'l') && M(ref platform, classId, 8, 'i') &&
					M(ref platform, classId, 9, 's') && M(ref platform, classId, 10, 't') &&
					Suffix(ref platform, classId, 11))
					return MuiMiscSpecialistClass.Scrmodelist;
				break;
			case 'A':
				if (M(ref platform, classId, 1, 'r') && M(ref platform, classId, 2, 'g') &&
					M(ref platform, classId, 3, 's') && M(ref platform, classId, 4, 't') &&
					M(ref platform, classId, 5, 'r') && M(ref platform, classId, 6, 'i') &&
					M(ref platform, classId, 7, 'n') && M(ref platform, classId, 8, 'g') &&
					Suffix(ref platform, classId, 9))
					return MuiMiscSpecialistClass.Argstring;
				if (M(ref platform, classId, 1, 'b') && M(ref platform, classId, 2, 'o') &&
					M(ref platform, classId, 3, 'u') && M(ref platform, classId, 4, 't') &&
					M(ref platform, classId, 5, 'm') && M(ref platform, classId, 6, 'u') &&
					M(ref platform, classId, 7, 'i') && Suffix(ref platform, classId, 8))
					return MuiMiscSpecialistClass.Aboutmui;
				break;
			case 'M':
				if (M(ref platform, classId, 1, 'c') && M(ref platform, classId, 2, 'c') &&
					M(ref platform, classId, 3, 'p') && M(ref platform, classId, 4, 'r') &&
					M(ref platform, classId, 5, 'e') && M(ref platform, classId, 6, 'f') &&
					M(ref platform, classId, 7, 's') && Suffix(ref platform, classId, 8))
					return MuiMiscSpecialistClass.Mccprefs;
				break;
			case 'T':
				if (M(ref platform, classId, 1, 'i') && M(ref platform, classId, 2, 't') &&
					M(ref platform, classId, 3, 'l') && M(ref platform, classId, 4, 'e') &&
					Suffix(ref platform, classId, 5))
					return MuiMiscSpecialistClass.Title;
				break;
		}
		return MuiMiscSpecialistClass.None;
	}

	private static int B<TPlatform>(ref TPlatform platform, APTR text, int index)
		where TPlatform : struct, IMuiGuestMemory =>
		platform.IsMapped(text, (uint)index + 1) ? platform.ReadUInt8(text, index)
			: -1;

	private static bool M<TPlatform>(ref TPlatform platform, APTR text, int index,
		int ch) where TPlatform : struct, IMuiGuestMemory =>
		B(ref platform, text, index) == ch;

	private static bool Suffix<TPlatform>(ref TPlatform platform, APTR text,
		int offset) where TPlatform : struct, IMuiGuestMemory =>
		B(ref platform, text, offset) == '.' &&
		B(ref platform, text, offset + 1) == 'm' &&
		B(ref platform, text, offset + 2) == 'u' &&
		B(ref platform, text, offset + 3) == 'i' &&
		B(ref platform, text, offset + 4) == 0;

	// ---- Inheritance ---------------------------------------------------------

	// The documented immediate superclass among these ten classes. Only
	// Filepanel descends from another family member (Panel); every other class
	// roots at an external MUI base (Group/Area/String/List/Window) that this
	// standalone family does not model.
	public static MuiMiscSpecialistClass Superclass(MuiMiscSpecialistClass cls) =>
		cls == MuiMiscSpecialistClass.Filepanel ? MuiMiscSpecialistClass.Panel
			: MuiMiscSpecialistClass.None;

	public static bool InheritsFrom(MuiMiscSpecialistClass cls,
		MuiMiscSpecialistClass ancestor)
	{
		var current = cls;
		for (var step = 0; step < 4; step++)
		{
			if (current == MuiMiscSpecialistClass.None) return false;
			if (current == ancestor) return true;
			current = Superclass(current);
		}
		return false;
	}

	// Scrmodelist is a private class; the others are public.
	public static bool IsPrivate(MuiMiscSpecialistClass cls) =>
		cls == MuiMiscSpecialistClass.Scrmodelist;

	// Aboutmui descends from Window (Notify), not Area, so it carries no
	// MUIA_Disabled state; every other class is Area-derived.
	private static bool IsAreaDerived(MuiMiscSpecialistClass cls) =>
		cls != MuiMiscSpecialistClass.Aboutmui &&
		cls != MuiMiscSpecialistClass.None;

	public static MuiMiscSpecialistClass Classify<TPlatform>(
		ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiGuestMemory =>
		Valid(ref platform, instance) &&
		MuiMiscSpecialistHeaderCodec.TryRead(ref platform, instance,
			out var header)
			? (MuiMiscSpecialistClass)header.Class
			: MuiMiscSpecialistClass.None;

	public static bool Valid<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiGuestMemory =>
		instance.IsNotNull &&
		platform.IsMapped(instance, MuiMiscSpecialistLayout.InstanceSize) &&
		MuiMiscSpecialistHeaderCodec.TryRead(ref platform, instance,
			out _);

	// ---- Headless-object sidecar adoption -----------------------------------

	// Attach a fixed Misc instance block to an already-created headless object.
	// OM_NEW/tag application has completed before this point, so the specialist
	// state is initialized atomically and the object remains untouched on any
	// allocation or attribute-link failure.
	public static APTR Attach<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, MuiMiscSpecialistClass cls)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (obj.IsNull || cls == MuiMiscSpecialistClass.None ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull ||
			ObjectInstance(ref platform, state, obj).IsNotNull) return APTR.Null;
		var instance = MuiHeadlessMemory.Allocate(ref platform,
			MuiMiscSpecialistLayout.InstanceSize);
		if (instance.IsNull || !Create(ref platform, instance, cls))
		{
			if (instance.IsNotNull)
			{
				platform.Clear(instance, MuiMiscSpecialistLayout.InstanceSize);
				platform.Free(instance, MuiMiscSpecialistLayout.InstanceSize);
			}
			return APTR.Null;
		}
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiMiscSpecialistLayout.SidecarAttribute, instance.Raw, false))
		{
			DisposeOwned(ref platform, instance);
			platform.Clear(instance, MuiMiscSpecialistLayout.InstanceSize);
			platform.Free(instance, MuiMiscSpecialistLayout.InstanceSize);
			return APTR.Null;
		}
		return instance;
	}

	// Resolve the registered class name on a headless object and attach the
	// corresponding Misc specialist. This is the factory/MakeObject interop
	// path; callers do not need to duplicate class-name classification.
	public static MuiMiscSpecialistClass ClassifyObject<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiServicePlatform
	{
		var classRecord = MuiHeadlessObjectCore.ObjectClassRecord(ref platform,
			state, obj);
		if (!MuiHeadlessClassCodec.TryRead(ref platform, classRecord,
			out var classValue)) return MuiMiscSpecialistClass.None;
		return ClassifyName(ref platform, classValue.Name);
	}

	public static APTR AttachByObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiServicePlatform =>
		Attach(ref platform, state, obj,
			ClassifyObject(ref platform, state, obj));

	// Return the attached fixed instance block, if valid. The object itself is
	// deliberately not treated as a Misc instance: headless records and Misc
	// records have different layouts and ownership rules.
	public static APTR ObjectInstance<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		if (obj.IsNull || !MuiHeadlessObjectCore.GetAttribute(ref platform, state,
			obj, MuiMiscSpecialistLayout.SidecarAttribute, out var raw) || raw == 0)
			return APTR.Null;
		var instance = APTR.FromPointer(raw);
		return Valid(ref platform, instance) ? instance : APTR.Null;
	}

	public static bool ValidObject<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiServicePlatform =>
		ObjectInstance(ref platform, state, obj).IsNotNull;

	public static MuiMiscSpecialistClass ClassifyObjectInstance<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiServicePlatform
	{
		var instance = ObjectInstance(ref platform, state, obj);
		return instance.IsNull ? MuiMiscSpecialistClass.None : Classify(ref platform,
			instance);
	}

	// ---- Creation (failure-atomic) -------------------------------------------

	public static MuiMiscSpecialistClass CreateByName<TPlatform>(
		ref TPlatform platform, APTR instance, APTR classId)
		where TPlatform : struct, IMuiServicePlatform
	{
		var cls = ClassifyName(ref platform, classId);
		if (cls == MuiMiscSpecialistClass.None) return MuiMiscSpecialistClass.None;
		return Create(ref platform, instance, cls) ? cls
			: MuiMiscSpecialistClass.None;
	}

	// Create a misc instance of an explicit class. Filepanel additionally owns a
	// 12-byte ASL service-state block and a 16-byte FilterFunc hook scratch,
	// both allocated failure-atomically: a failed allocation frees everything it
	// touched and returns false with the instance cleared. Documented init
	// defaults are established here (Title Newable/Sortable TRUE and
	// Position_Top; Aboutmui not yet open).
	public static bool Create<TPlatform>(ref TPlatform platform, APTR instance,
		MuiMiscSpecialistClass cls) where TPlatform : struct, IMuiServicePlatform
	{
		if (instance.IsNull ||
			!platform.IsMapped(instance, MuiMiscSpecialistLayout.InstanceSize) ||
			cls == MuiMiscSpecialistClass.None) return false;

		APTR aslState = APTR.Null;
		APTR hookMsg = APTR.Null;
		if (cls == MuiMiscSpecialistClass.Filepanel)
		{
			aslState = MuiHeadlessMemory.Allocate(ref platform,
				MuiMiscSpecialistLayout.AslStateSize);
			if (aslState.IsNull ||
				!MuiAslServiceCore.Initialize(ref platform, aslState))
			{
				if (aslState.IsNotNull) Free(ref platform, aslState,
					MuiMiscSpecialistLayout.AslStateSize);
				return false;
			}
			hookMsg = MuiHeadlessMemory.Allocate(ref platform,
				MuiMiscSpecialistLayout.HookMsgSize);
			if (hookMsg.IsNull)
			{
				Free(ref platform, aslState, MuiMiscSpecialistLayout.AslStateSize);
				return false;
			}
		}

		platform.Clear(instance, MuiMiscSpecialistLayout.InstanceSize);
		var header = default(MuiMiscSpecialistHeader);
		header.Magic = MuiMiscSpecialistHeader.Cookie;
		header.Class = (uint)cls;
		var filepanelState = default(MuiMiscFilepanelServiceState);
		filepanelState.AslState = aslState;
		filepanelState.HookMsg = hookMsg;
		if (!WriteFilepanelServiceState(ref platform, instance, filepanelState))
			return false;

		if (cls == MuiMiscSpecialistClass.Title)
		{
			// Documented Title defaults: Newable and Sortable TRUE, Position_Top,
			// EventHandlerPriority_Default, OnLastClose_Remove.
			header.Flags = MuiMiscSpecialistLayout.FlagTiNewable |
				MuiMiscSpecialistLayout.FlagTiSortable;
			var titleState = default(MuiMiscTitleState);
			titleState.Position = MuiMiscAttributes.Title_Position_Top;
			if (!WriteTitleState(ref platform, instance, titleState)) return false;
		}
		return MuiMiscSpecialistHeaderCodec.Write(ref platform, instance, header);
	}

	// ---- Setup / Cleanup -----------------------------------------------------

	public static bool Setup<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Valid(ref platform, instance)) return false;
		SetFlag(ref platform, instance,
			MuiMiscSpecialistLayout.FlagSetupActive, true);
		return true;
	}

	public static bool Cleanup<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Valid(ref platform, instance)) return false;
		CancelAsl(ref platform, instance);
		SetFlag(ref platform, instance,
			MuiMiscSpecialistLayout.FlagSetupActive, false);
		return true;
	}

	public static bool IsSetupActive<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiServicePlatform
	{
		return MuiMiscSpecialistHeaderCodec.TryRead(ref platform, instance,
			out var header) &&
			(header.Flags &
			 MuiMiscSpecialistLayout.FlagSetupActive) != 0;
	}

	// ---- Attribute set -------------------------------------------------------

	public static bool SetAttribute<TPlatform>(ref TPlatform platform,
		APTR instance, uint attribute, uint value, bool isInit, bool notify,
		out bool changed) where TPlatform : struct, IMuiServicePlatform
	{
		changed = false;
		if (!MuiMiscSpecialistHeaderCodec.TryRead(ref platform, instance,
			out var header)) return false;
		var cls = (MuiMiscSpecialistClass)header.Class;

		switch (attribute)
		{
			// -- shared Area (all except Aboutmui) [ISG] --
			case MuiMiscAttributes.Disabled:
				if (!IsAreaDerived(cls)) return false;
				changed = SetFlag(ref platform, instance,
					MuiMiscSpecialistLayout.FlagDisabled, value != 0);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;

			// -- Keyadjust [ISG] --
			case MuiMiscAttributes.Keyadjust_Key:
				if (cls != MuiMiscSpecialistClass.Keyadjust) return false;
				if (!SetOwnedString(ref platform, instance,
					MuiMiscOwnedStringField.Key, value, out changed))
					return false;
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiMiscAttributes.Keyadjust_AllowMultipleKeys:
				return SetKeyadjustFlag(ref platform, instance, cls,
					MuiMiscSpecialistLayout.FlagKaMultipleKeys, attribute, value,
					isInit, notify, out changed);
			case MuiMiscAttributes.Keyadjust_AllowDoubleClick:
				return SetKeyadjustFlag(ref platform, instance, cls,
					MuiMiscSpecialistLayout.FlagKaDoubleClick, attribute, value,
					isInit, notify, out changed);
			case MuiMiscAttributes.Keyadjust_AllowTripleClick:
				return SetKeyadjustFlag(ref platform, instance, cls,
					MuiMiscSpecialistLayout.FlagKaTripleClick, attribute, value,
					isInit, notify, out changed);
			case MuiMiscAttributes.Keyadjust_AllowMouseEvents:
				return SetKeyadjustFlag(ref platform, instance, cls,
					MuiMiscSpecialistLayout.FlagKaMouseEvents, attribute, value,
					isInit, notify, out changed);
			case MuiMiscAttributes.Keyadjust_ForceKeyCode:
				return SetKeyadjustFlag(ref platform, instance, cls,
					MuiMiscSpecialistLayout.FlagKaForceKeyCode, attribute, value,
					isInit, notify, out changed);

			// -- Argstring [ISG] --
			case MuiMiscAttributes.Argstring_Template:
				if (cls != MuiMiscSpecialistClass.Argstring) return false;
				if (!SetOwnedString(ref platform, instance,
					MuiMiscOwnedStringField.ArgTemplate, value, out changed))
					return false;
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiMiscAttributes.Argstring_Contents:
				if (cls != MuiMiscSpecialistClass.Argstring) return false;
				if (!SetOwnedString(ref platform, instance,
					MuiMiscOwnedStringField.ArgContents, value, out changed))
					return false;
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;

			// -- Aboutmui [I.G] Application --
			case MuiMiscAttributes.Aboutmui_Application:
				if (cls != MuiMiscSpecialistClass.Aboutmui) return false;
				if (isInit)
				{
					if (!TryReadWindowPanelState(ref platform, instance,
						out var aboutState)) return false;
					aboutState.Application = APTR.FromPointer(value);
					if (!WriteWindowPanelState(ref platform, instance,
						aboutState)) return false;
					changed = true;
				}
				return true;

			// -- FSProtectionBits [ISG] Flags --
			case MuiMiscAttributes.FSProtectionBits_Flags:
				if (cls != MuiMiscSpecialistClass.FSProtectionBits) return false;
				if (!TryReadProtectionState(ref platform, instance,
					out var protectionState)) return false;
				changed = protectionState.Flags != value;
				protectionState.Flags = value;
				if (changed && !WriteProtectionState(ref platform, instance,
					protectionState)) return false;
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;

			// -- Title [ISG] --
			case MuiMiscAttributes.Title_Position:
				if (cls != MuiMiscSpecialistClass.Title || value > 3) return false;
				if (!TryReadTitleState(ref platform, instance, out var positionState))
					return false;
				changed = positionState.Position != value;
				positionState.Position = value;
				if (changed && !WriteTitleState(ref platform, instance,
					positionState)) return false;
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiMiscAttributes.Title_OnLastClose:
				if (cls != MuiMiscSpecialistClass.Title || value > 1) return false;
				if (!TryReadTitleState(ref platform, instance, out var closeState))
					return false;
				changed = closeState.OnLastClose != value;
				closeState.OnLastClose = value;
				if (changed && !WriteTitleState(ref platform, instance,
					closeState)) return false;
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiMiscAttributes.Title_EventHandlerPriority:
				if (cls != MuiMiscSpecialistClass.Title) return false;
				if (!TryReadTitleState(ref platform, instance, out var priorityState))
					return false;
				changed = priorityState.EventPriority != value;
				priorityState.EventPriority = value;
				if (changed && !WriteTitleState(ref platform, instance,
					priorityState)) return false;
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiMiscAttributes.Title_Clickable:
				return SetTitleFlag(ref platform, instance, cls,
					MuiMiscSpecialistLayout.FlagTiClickable, attribute, value,
					isInit, notify, out changed);
			case MuiMiscAttributes.Title_Closable:
				return SetTitleFlag(ref platform, instance, cls,
					MuiMiscSpecialistLayout.FlagTiClosable, attribute, value,
					isInit, notify, out changed);
			case MuiMiscAttributes.Title_Newable:
				return SetTitleFlag(ref platform, instance, cls,
					MuiMiscSpecialistLayout.FlagTiNewable, attribute, value,
					isInit, notify, out changed);
			case MuiMiscAttributes.Title_Sortable:
				return SetTitleFlag(ref platform, instance, cls,
					MuiMiscSpecialistLayout.FlagTiSortable, attribute, value,
					isInit, notify, out changed);

			// -- Filepanel owned strings [ISG] --
			case MuiMiscAttributes.Filepanel_Drawer:
				return SetFilepanelString(ref platform, instance, cls,
					MuiMiscOwnedStringField.FilepanelDrawer, attribute, value, isInit,
					notify, out changed);
			case MuiMiscAttributes.Filepanel_File:
				return SetFilepanelString(ref platform, instance, cls,
					MuiMiscOwnedStringField.FilepanelFile,
					attribute, value, isInit, notify, out changed);
			case MuiMiscAttributes.Filepanel_Pattern:
				return SetFilepanelString(ref platform, instance, cls,
					MuiMiscOwnedStringField.FilepanelPattern, attribute, value, isInit,
					notify, out changed);
			case MuiMiscAttributes.Filepanel_AcceptPattern:
				return SetFilepanelString(ref platform, instance, cls,
					MuiMiscOwnedStringField.FilepanelAcceptPattern, attribute, value,
					isInit, notify, out changed);
			case MuiMiscAttributes.Filepanel_RejectPattern:
				return SetFilepanelString(ref platform, instance, cls,
					MuiMiscOwnedStringField.FilepanelRejectPattern, attribute, value,
					isInit, notify, out changed);

			// -- Filepanel init booleans [I..] --
			case MuiMiscAttributes.Filepanel_DoMultiSelect:
				return SetFilepanelInitFlag(ref platform, instance, cls,
					MuiMiscSpecialistLayout.FlagFpDoMultiSelect, value, isInit,
					out changed);
			case MuiMiscAttributes.Filepanel_DoPatterns:
				return SetFilepanelInitFlag(ref platform, instance, cls,
					MuiMiscSpecialistLayout.FlagFpDoPatterns, value, isInit,
					out changed);
			case MuiMiscAttributes.Filepanel_DoSaveMode:
				return SetFilepanelInitFlag(ref platform, instance, cls,
					MuiMiscSpecialistLayout.FlagFpDoSaveMode, value, isInit,
					out changed);
			case MuiMiscAttributes.Filepanel_DrawersOnly:
				return SetFilepanelInitFlag(ref platform, instance, cls,
					MuiMiscSpecialistLayout.FlagFpDrawersOnly, value, isInit,
					out changed);
			case MuiMiscAttributes.Filepanel_FilterDrawers:
				return SetFilepanelInitFlag(ref platform, instance, cls,
					MuiMiscSpecialistLayout.FlagFpFilterDrawers, value, isInit,
					out changed);
			case MuiMiscAttributes.Filepanel_RejectIcons:
				return SetFilepanelInitFlag(ref platform, instance, cls,
					MuiMiscSpecialistLayout.FlagFpRejectIcons, value, isInit,
					out changed);
			case MuiMiscAttributes.Filepanel_FilterFunc:
				if (cls != MuiMiscSpecialistClass.Filepanel || !isInit) return false;
				if (!TryReadFilepanelServiceState(ref platform, instance,
					out var filterState)) return false;
				filterState.FilterFunc = APTR.FromPointer(value);
				if (!WriteFilepanelServiceState(ref platform, instance,
					filterState)) return false;
				changed = true;
				return true;
		}
		return false;
	}

	// ---- Attribute get -------------------------------------------------------

	public static bool GetAttribute<TPlatform>(ref TPlatform platform,
		APTR instance, uint attribute, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiMiscSpecialistHeaderCodec.TryRead(ref platform, instance,
			out var header)) return false;
		var cls = (MuiMiscSpecialistClass)header.Class;
		var flags = header.Flags;

		switch (attribute)
		{
			case MuiMiscAttributes.Disabled:
				if (!IsAreaDerived(cls)) return false;
				value = (flags & MuiMiscSpecialistLayout.FlagDisabled) != 0 ? 1u : 0u;
				return true;

			// -- Keyadjust --
			case MuiMiscAttributes.Keyadjust_Key:
				if (cls != MuiMiscSpecialistClass.Keyadjust) return false;
				if (!TryReadOwnedStringSlot(ref platform, instance,
					MuiMiscOwnedStringField.Key, out var keySlot)) return false;
				value = keySlot.Value.Raw;
				return true;
			case MuiMiscAttributes.Keyadjust_AllowMultipleKeys:
				return GetFlag(ref platform, cls, MuiMiscSpecialistClass.Keyadjust,
					flags, MuiMiscSpecialistLayout.FlagKaMultipleKeys, out value);
			case MuiMiscAttributes.Keyadjust_AllowDoubleClick:
				return GetFlag(ref platform, cls, MuiMiscSpecialistClass.Keyadjust,
					flags, MuiMiscSpecialistLayout.FlagKaDoubleClick, out value);
			case MuiMiscAttributes.Keyadjust_AllowTripleClick:
				return GetFlag(ref platform, cls, MuiMiscSpecialistClass.Keyadjust,
					flags, MuiMiscSpecialistLayout.FlagKaTripleClick, out value);
			case MuiMiscAttributes.Keyadjust_AllowMouseEvents:
				return GetFlag(ref platform, cls, MuiMiscSpecialistClass.Keyadjust,
					flags, MuiMiscSpecialistLayout.FlagKaMouseEvents, out value);
			case MuiMiscAttributes.Keyadjust_ForceKeyCode:
				return GetFlag(ref platform, cls, MuiMiscSpecialistClass.Keyadjust,
					flags, MuiMiscSpecialistLayout.FlagKaForceKeyCode, out value);

			// -- Argstring --
			case MuiMiscAttributes.Argstring_Template:
				if (cls != MuiMiscSpecialistClass.Argstring) return false;
				if (!TryReadOwnedStringSlot(ref platform, instance,
					MuiMiscOwnedStringField.ArgTemplate, out var templateSlot))
					return false;
				value = templateSlot.Value.Raw;
				return true;
			case MuiMiscAttributes.Argstring_Contents:
				if (cls != MuiMiscSpecialistClass.Argstring) return false;
				if (!TryReadOwnedStringSlot(ref platform, instance,
					MuiMiscOwnedStringField.ArgContents, out var contentsSlot))
					return false;
				value = contentsSlot.Value.Raw;
				return true;

			// -- Aboutmui --
			case MuiMiscAttributes.Aboutmui_Application:
				if (cls != MuiMiscSpecialistClass.Aboutmui) return false;
				if (!TryReadWindowPanelState(ref platform, instance,
					out var aboutState)) return false;
				value = aboutState.Application.Raw;
				return true;

			// -- FSProtectionBits --
			case MuiMiscAttributes.FSProtectionBits_Flags:
				if (cls != MuiMiscSpecialistClass.FSProtectionBits) return false;
				if (!TryReadProtectionState(ref platform, instance,
					out var protectionState)) return false;
				value = protectionState.Flags;
				return true;

			// -- Title --
			case MuiMiscAttributes.Title_Position:
				if (cls != MuiMiscSpecialistClass.Title) return false;
				if (!TryReadTitleState(ref platform, instance, out var positionState))
					return false;
				value = positionState.Position;
				return true;
			case MuiMiscAttributes.Title_OnLastClose:
				if (cls != MuiMiscSpecialistClass.Title) return false;
				if (!TryReadTitleState(ref platform, instance, out var closeState))
					return false;
				value = closeState.OnLastClose;
				return true;
			case MuiMiscAttributes.Title_EventHandlerPriority:
				if (cls != MuiMiscSpecialistClass.Title) return false;
				if (!TryReadTitleState(ref platform, instance, out var priorityState))
					return false;
				value = priorityState.EventPriority;
				return true;
			case MuiMiscAttributes.Title_Clickable:
				return GetFlag(ref platform, cls, MuiMiscSpecialistClass.Title,
					flags, MuiMiscSpecialistLayout.FlagTiClickable, out value);
			case MuiMiscAttributes.Title_Closable:
				return GetFlag(ref platform, cls, MuiMiscSpecialistClass.Title,
					flags, MuiMiscSpecialistLayout.FlagTiClosable, out value);
			case MuiMiscAttributes.Title_Newable:
				return GetFlag(ref platform, cls, MuiMiscSpecialistClass.Title,
					flags, MuiMiscSpecialistLayout.FlagTiNewable, out value);
			case MuiMiscAttributes.Title_Sortable:
				return GetFlag(ref platform, cls, MuiMiscSpecialistClass.Title,
					flags, MuiMiscSpecialistLayout.FlagTiSortable, out value);

			// -- Filepanel --
			case MuiMiscAttributes.Filepanel_Drawer:
				return GetFilepanelField(ref platform, instance, cls,
					MuiMiscOwnedStringField.FilepanelDrawer, out value);
			case MuiMiscAttributes.Filepanel_File:
				return GetFilepanelField(ref platform, instance, cls,
					MuiMiscOwnedStringField.FilepanelFile, out value);
			case MuiMiscAttributes.Filepanel_Pattern:
				return GetFilepanelField(ref platform, instance, cls,
					MuiMiscOwnedStringField.FilepanelPattern, out value);
			case MuiMiscAttributes.Filepanel_AcceptPattern:
				return GetFilepanelField(ref platform, instance, cls,
					MuiMiscOwnedStringField.FilepanelAcceptPattern, out value);
			case MuiMiscAttributes.Filepanel_RejectPattern:
				return GetFilepanelField(ref platform, instance, cls,
					MuiMiscOwnedStringField.FilepanelRejectPattern, out value);
			case MuiMiscAttributes.Filepanel_FilterFunc:
				if (cls != MuiMiscSpecialistClass.Filepanel ||
					!TryReadFilepanelServiceState(ref platform, instance,
						out var filterState)) return false;
				value = filterState.FilterFunc.Raw;
				return true;
			case MuiMiscAttributes.Filepanel_DoMultiSelect:
				return GetFlag(ref platform, cls, MuiMiscSpecialistClass.Filepanel,
					flags, MuiMiscSpecialistLayout.FlagFpDoMultiSelect, out value);
			case MuiMiscAttributes.Filepanel_DoPatterns:
				return GetFlag(ref platform, cls, MuiMiscSpecialistClass.Filepanel,
					flags, MuiMiscSpecialistLayout.FlagFpDoPatterns, out value);
			case MuiMiscAttributes.Filepanel_DoSaveMode:
				return GetFlag(ref platform, cls, MuiMiscSpecialistClass.Filepanel,
					flags, MuiMiscSpecialistLayout.FlagFpDoSaveMode, out value);
			case MuiMiscAttributes.Filepanel_DrawersOnly:
				return GetFlag(ref platform, cls, MuiMiscSpecialistClass.Filepanel,
					flags, MuiMiscSpecialistLayout.FlagFpDrawersOnly, out value);
			case MuiMiscAttributes.Filepanel_FilterDrawers:
				return GetFlag(ref platform, cls, MuiMiscSpecialistClass.Filepanel,
					flags, MuiMiscSpecialistLayout.FlagFpFilterDrawers, out value);
			case MuiMiscAttributes.Filepanel_RejectIcons:
				return GetFlag(ref platform, cls, MuiMiscSpecialistClass.Filepanel,
					flags, MuiMiscSpecialistLayout.FlagFpRejectIcons, out value);
		}
		return false;
	}

	// ---- Keyadjust input policy ----------------------------------------------

	// Record an input event, honoring the documented allow/force policies. A
	// mouse event is rejected unless AllowMouseEvents; a double/triple click is
	// rejected unless the matching AllowDoubleClick/AllowTripleClick; a
	// multi-key chord is rejected unless AllowMultipleKeys. On acceptance the
	// Key description is stored as a class-owned copy and MUIA_Keyadjust_Key is
	// notified. ForceKeyCode is an observable policy the recorder honors by
	// accepting a raw key code even when a symbolic name is unavailable.
	// Returns whether the event was accepted.
	public static bool RecordInput<TPlatform>(ref TPlatform platform,
		APTR instance, APTR keyText, bool isMouse, uint clickCount, bool multiKey)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (Classify(ref platform, instance) != MuiMiscSpecialistClass.Keyadjust)
			return false;
		if (!MuiMiscSpecialistHeaderCodec.TryRead(ref platform, instance,
			out var header)) return false;
		var flags = header.Flags;
		if ((flags & MuiMiscSpecialistLayout.FlagDisabled) != 0) return false;
		if (isMouse && (flags & MuiMiscSpecialistLayout.FlagKaMouseEvents) == 0)
			return false;
		if (clickCount >= 3 &&
			(flags & MuiMiscSpecialistLayout.FlagKaTripleClick) == 0) return false;
		if (clickCount == 2 &&
			(flags & MuiMiscSpecialistLayout.FlagKaDoubleClick) == 0) return false;
		if (multiKey && (flags & MuiMiscSpecialistLayout.FlagKaMultipleKeys) == 0)
			return false;
		if (!SetOwnedString(ref platform, instance,
			MuiMiscOwnedStringField.Key, keyText.Raw, out var changed))
			return false;
		Notify(ref platform, instance, MuiMiscAttributes.Keyadjust_Key,
			keyText.Raw, false, true, changed);
		return true;
	}

	// ---- Argstring formatting/mutation ---------------------------------------

	// Derive Contents from the owned Template by copying the template text
	// verbatim into a fresh owned Contents block (the ABI-visible identity
	// mapping when no arguments are substituted). Failure-atomic: a failed copy
	// leaves the previous Contents untouched. Returns whether Contents changed.
	public static bool FormatContents<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiServicePlatform
	{
		if (Classify(ref platform, instance) != MuiMiscSpecialistClass.Argstring)
			return false;
		if (!TryReadOwnedStringSlot(ref platform, instance,
			MuiMiscOwnedStringField.ArgTemplate, out var templateSlot)) return false;
		var template = templateSlot.Value.Raw;
		if (!SetOwnedString(ref platform, instance,
			MuiMiscOwnedStringField.ArgContents, template, out var changed))
			return false;
		if (!TryReadOwnedStringSlot(ref platform, instance,
			MuiMiscOwnedStringField.ArgContents, out var contentsSlot))
			return false;
		var contents = contentsSlot.Value.Raw;
		Notify(ref platform, instance, MuiMiscAttributes.Argstring_Contents,
			contents, false, true, changed);
		return true;
	}

	// ---- Aboutmui lifetime ---------------------------------------------------

	// The About window opens against its referenced Application. It requires a
	// bound Application (documented init reference) and refuses to open twice.
	public static bool AboutmuiOpen<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory
	{
		if (Classify(ref platform, instance) != MuiMiscSpecialistClass.Aboutmui)
			return false;
		if (!TryReadWindowPanelState(ref platform, instance,
			out var aboutState)) return false;
		if (aboutState.Application.IsNull)
			return false;
		if (!MuiMiscSpecialistHeaderCodec.TryRead(ref platform, instance,
			out var header)) return false;
		var flags = header.Flags;
		if ((flags & MuiMiscSpecialistLayout.FlagAboutOpen) != 0) return false;
		header.Flags = flags | MuiMiscSpecialistLayout.FlagAboutOpen;
		return MuiMiscSpecialistHeaderCodec.Write(ref platform, instance, header);
	}

	// The About window self-closes: closing an open window clears the open
	// state (application lifetime is unaffected; the referenced Application is
	// never disposed by the window). Returns whether a close occurred.
	public static bool AboutmuiClose<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory
	{
		if (Classify(ref platform, instance) != MuiMiscSpecialistClass.Aboutmui)
			return false;
		if (!MuiMiscSpecialistHeaderCodec.TryRead(ref platform, instance,
			out var header)) return false;
		var flags = header.Flags;
		if ((flags & MuiMiscSpecialistLayout.FlagAboutOpen) == 0) return false;
		header.Flags = flags & ~MuiMiscSpecialistLayout.FlagAboutOpen;
		return MuiMiscSpecialistHeaderCodec.Write(ref platform, instance, header);
	}

	public static bool AboutmuiIsOpen<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscSpecialistHeaderCodec.TryRead(ref platform, instance,
			out var aboutHeader) &&
		(MuiMiscSpecialistClass)aboutHeader.Class ==
			MuiMiscSpecialistClass.Aboutmui &&
		(aboutHeader.Flags &
		 MuiMiscSpecialistLayout.FlagAboutOpen) != 0;

	// ---- Panel_Run (honest undocumented boundary) ----------------------------

	// MUIM_Panel_Run(app, win). The documented method drives a preferences panel
	// against an application/window, but the modal loop and prefs I/O are not
	// part of the public boundary. The honest ABI-visible contract is enforced
	// here: both an application and a window are required (a Null argument is
	// rejected), the pair is recorded, and the panel is marked as having run.
	// This is not an unconditional-success stub — invalid arguments fail and the
	// run state is observable. Returns whether the run boundary was accepted.
	public static bool PanelRun<TPlatform>(ref TPlatform platform, APTR instance,
		APTR app, APTR win) where TPlatform : struct, IMuiGuestMemory
	{
		if (Classify(ref platform, instance) != MuiMiscSpecialistClass.Panel)
			return false;
		if (app.IsNull || win.IsNull) return false;
		if (!TryReadWindowPanelState(ref platform, instance,
			out var panelState)) return false;
		panelState.Application = app;
		panelState.PanelWindow = win;
		if (!WriteWindowPanelState(ref platform, instance, panelState)) return false;
		return SetFlag(ref platform, instance,
			MuiMiscSpecialistLayout.FlagPanelRan, true);
	}

	public static bool PanelHasRun<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscSpecialistHeaderCodec.TryRead(ref platform, instance,
			out var panelHeader) &&
		(MuiMiscSpecialistClass)panelHeader.Class ==
			MuiMiscSpecialistClass.Panel &&
		(panelHeader.Flags &
			MuiMiscSpecialistLayout.FlagPanelRan) != 0;

	// ---- Filepanel AddRow + ASL integration ----------------------------------

	// MUIM_Filepanel_AddRow(label, contents). Both children are adopted by the
	// panel and disposed at teardown. Failure-atomic: a Null child, a class
	// mismatch, or an inability to grow the bounded row block leaves nothing
	// adopted and returns false. On the first row the row block is allocated;
	// if that allocation fails the children are NOT adopted (the caller keeps
	// ownership). Returns whether the row was added.
	public static bool FilepanelAddRow<TPlatform>(ref TPlatform platform,
		APTR instance, APTR label, APTR contents)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (Classify(ref platform, instance) != MuiMiscSpecialistClass.Filepanel)
			return false;
		if (label.IsNull || contents.IsNull) return false;
		if (!TryReadFilepanelServiceState(ref platform, instance,
			out var filepanelState)) return false;
		var count = filepanelState.RowCount;
		if (count >= MuiMiscSpecialistLayout.MaximumRows) return false;
		var block = filepanelState.Rows;
		if (block.IsNull)
		{
			block = MuiHeadlessMemory.Allocate(ref platform,
				(uint)MuiMiscSpecialistLayout.MaximumRows *
				MuiMiscSpecialistLayout.RowRecordSize);
			if (block.IsNull) return false;   // atomic: nothing adopted
			filepanelState.Rows = block;
		}
		var cursor = default(MuiFilepanelRowCursor);
		cursor.Base = block;
		cursor.Index = count;
		if (!MuiFilepanelRowCursorCodec.TryGetEntry(ref platform, cursor,
			out var rowAddress)) return false;
		var row = default(MuiFilepanelRowRecord);
		row.Label = label;
		row.Contents = contents;
		if (!MuiFilepanelRowCodec.Write(ref platform, rowAddress, row)) return false;
		filepanelState.RowCount = count + 1;
		return WriteFilepanelServiceState(ref platform, instance, filepanelState);
	}

	public static uint FilepanelRowCount<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		Classify(ref platform, instance) == MuiMiscSpecialistClass.Filepanel &&
		TryReadFilepanelServiceState(ref platform, instance, out var filepanelState)
			? filepanelState.RowCount : 0;

	// Invoke the FilterFunc hook for a candidate entry using the exact
	// CallHookPkt register ABI (A0 = hook, A2 = object, A1 = message). The
	// message scratch carries the candidate entry pointer. Returns the hook
	// result (non-zero = keep the entry); with no hook installed every entry is
	// kept (returns 1).
	public static uint FilepanelFilter<TPlatform>(ref TPlatform platform,
		APTR instance, APTR entry) where TPlatform : struct, IMuiServicePlatform
	{
		if (Classify(ref platform, instance) != MuiMiscSpecialistClass.Filepanel)
			return 0;
		if (!TryReadFilepanelServiceState(ref platform, instance,
			out var filepanelState)) return 0;
		var hook = filepanelState.FilterFunc;
		if (hook.IsNull) return 1;
		var msg = filepanelState.HookMsg;
		if (msg.IsNotNull &&
			platform.IsMapped(msg, MuiMiscSpecialistLayout.HookMsgSize))
		{
			platform.WriteUInt32(msg, MuiMiscSpecialistLayout.MsgMethod, 0);
			platform.WriteUInt32(msg, MuiMiscSpecialistLayout.MsgParam1, entry.Raw);
			platform.WriteUInt32(msg, MuiMiscSpecialistLayout.MsgParam2, 0);
		}
		return platform.InvokeHook(hook, instance, msg);
	}

	// Drive an ASL requester for the browse action through MuiAslServiceCore.
	// Failure-atomic: if the requester cannot be allocated nothing is left
	// active and the call returns false. On success the requester is run and
	// released within the same call and the active flag is cleared, so a failed
	// service integration never leaks a requester. Returns whether a browse ran.
	public static bool FilepanelBrowse<TPlatform>(ref TPlatform platform,
		APTR instance, uint requestType, APTR tags)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (Classify(ref platform, instance) != MuiMiscSpecialistClass.Filepanel)
			return false;
		if (!TryReadFilepanelServiceState(ref platform, instance,
			out var filepanelState)) return false;
		var requester = MuiAslServiceCore.AllocAslRequest(ref platform,
			filepanelState.AslState,
			requestType, tags);
		if (requester.IsNull) return false;   // service failure: stays inactive
		SetFlag(ref platform, instance, MuiMiscSpecialistLayout.FlagFpAslActive,
			true);
		MuiAslServiceCore.AslRequest(ref platform, filepanelState.AslState,
			requester, tags);
		MuiAslServiceCore.FreeAslRequest(ref platform, filepanelState.AslState,
			requester);
		SetFlag(ref platform, instance, MuiMiscSpecialistLayout.FlagFpAslActive,
			false);
		return true;
	}

	// ---- Mccprefs bounded gadget registry ------------------------------------

	// MUIM_Mccprefs_RegisterGadget. With a non-zero id the gadget is registered
	// (or its record updated in place when the same gadget pointer is already
	// present); with id == 0 the matching gadget is unregistered. Failure-atomic:
	// a full registry, a Null gadget on register, or an unknown gadget on
	// unregister returns false without mutating the registry. The registry
	// stores caller-owned references only and never disposes them.
	public static bool MccprefsRegisterGadget<TPlatform>(ref TPlatform platform,
		APTR instance, APTR gadget, uint id, uint paramsValue, APTR title,
		uint attr, APTR label) where TPlatform : struct, IMuiServicePlatform
	{
		if (Classify(ref platform, instance) != MuiMiscSpecialistClass.Mccprefs)
			return false;
		if (!TryReadMccprefsState(ref platform, instance,
			out var mccprefsState)) return false;
		var count = mccprefsState.RegistryCount;
		if (count > MuiMiscSpecialistLayout.MaximumRegistry) return false;
		var block = mccprefsState.Registry;

		if (id == 0)
		{
			// Unregister the record whose gadget matches.
			if (block.IsNull || gadget.IsNull) return false;
			for (var i = 0u; i < count; i++)
			{
				var cursor = default(MuiMccprefsRegistryCursor);
				cursor.Base = block;
				cursor.Index = i;
				if (!MuiMccprefsRegistryCursorCodec.TryGetEntry(ref platform,
					cursor, out var address)) return false;
				if (!MuiMccprefsRegistryCodec.TryRead(ref platform, address,
					out var record) || record.Gadget != gadget) continue;
				cursor.Index = count - 1;
				if (!MuiMccprefsRegistryCursorCodec.TryGetEntry(ref platform,
					cursor, out var lastAddress)) return false;
				if (address != lastAddress &&
					(!MuiMccprefsRegistryCodec.TryRead(ref platform, lastAddress,
						out var last) || !MuiMccprefsRegistryCodec.Write(ref platform,
						address, last))) return false;
				if (!MuiMccprefsRegistryCodec.Write(ref platform, lastAddress,
					default)) return false;
				mccprefsState.RegistryCount = count - 1;
				return WriteMccprefsState(ref platform, instance, mccprefsState);
			}
			return false;
		}

		if (gadget.IsNull) return false;
		if (block.IsNull)
		{
			block = MuiHeadlessMemory.Allocate(ref platform,
				(uint)MuiMiscSpecialistLayout.MaximumRegistry *
				MuiMiscSpecialistLayout.RegistryRecordSize);
			if (block.IsNull) return false;
			mccprefsState.Registry = block;
		}
		// Update an existing record for the same gadget in place.
		for (var i = 0u; i < count; i++)
		{
			var cursor = default(MuiMccprefsRegistryCursor);
			cursor.Base = block;
			cursor.Index = i;
			if (!MuiMccprefsRegistryCursorCodec.TryGetEntry(ref platform,
				cursor, out var address)) return false;
			if (!MuiMccprefsRegistryCodec.TryRead(ref platform, address,
				out var existing) || existing.Gadget != gadget) continue;
			existing.Id = id;
			existing.Params = paramsValue;
			existing.Title = title;
			existing.Attr = attr;
			existing.Label = label;
			return MuiMccprefsRegistryCodec.Write(ref platform, address, existing);
		}
		if (count >= MuiMiscSpecialistLayout.MaximumRegistry) return false;
		var newCursor = default(MuiMccprefsRegistryCursor);
		newCursor.Base = block;
		newCursor.Index = count;
		if (!MuiMccprefsRegistryCursorCodec.TryGetEntry(ref platform, newCursor,
			out var newAddress)) return false;
		var newRecord = default(MuiMccprefsRegistryRecord);
		newRecord.Gadget = gadget;
		newRecord.Id = id;
		newRecord.Params = paramsValue;
		newRecord.Title = title;
		newRecord.Attr = attr;
		newRecord.Label = label;
		if (!MuiMccprefsRegistryCodec.Write(ref platform, newAddress, newRecord))
			return false;
		mccprefsState.RegistryCount = count + 1;
		return WriteMccprefsState(ref platform, instance, mccprefsState);
	}

	// MUIM_Mccprefs_ConfigToGadgets(configdata). Distributes the config block to
	// every registered gadget. With no gadgets registered there is nothing to
	// distribute and the method reports failure (an honest empty-registry
	// boundary rather than an unconditional success). Records the config source.
	// Returns whether at least one gadget was updated.
	public static bool MccprefsConfigToGadgets<TPlatform>(ref TPlatform platform,
		APTR instance, APTR configData) where TPlatform : struct, IMuiGuestMemory
	{
		if (Classify(ref platform, instance) != MuiMiscSpecialistClass.Mccprefs)
			return false;
		if (!TryReadMccprefsState(ref platform, instance,
			out var mccprefsState)) return false;
		var count = mccprefsState.RegistryCount;
		mccprefsState.RegistryConfig = configData;
		if (!WriteMccprefsState(ref platform, instance, mccprefsState)) return false;
		return count != 0;
	}

	// MUIM_Mccprefs_GadgetsToConfig(configdata, originator). Collects gadget
	// state into the config block. Same honest empty-registry boundary as
	// ConfigToGadgets. Records config source and originator. Returns whether at
	// least one gadget contributed.
	public static bool MccprefsGadgetsToConfig<TPlatform>(ref TPlatform platform,
		APTR instance, APTR configData, APTR originator)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (Classify(ref platform, instance) != MuiMiscSpecialistClass.Mccprefs)
			return false;
		if (!TryReadMccprefsState(ref platform, instance,
			out var mccprefsState)) return false;
		var count = mccprefsState.RegistryCount;
		mccprefsState.RegistryConfig = configData;
		mccprefsState.RegistryOriginator = originator;
		if (!WriteMccprefsState(ref platform, instance, mccprefsState)) return false;
		return count != 0;
	}

	public static uint MccprefsRegistryCount<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		Classify(ref platform, instance) == MuiMiscSpecialistClass.Mccprefs &&
		TryReadMccprefsState(ref platform, instance, out var mccprefsState)
			? mccprefsState.RegistryCount : 0;

	// ---- Title page topology -------------------------------------------------

	// MUIM_Title_New. Appends a fresh page slot and returns a fresh, non-zero
	// synthetic page handle. Requires MUIA_Title_Newable. Failure-atomic: a full
	// page table or an allocation failure returns 0 (no page created). The
	// newly created page becomes active.
	public static uint TitleNew<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (Classify(ref platform, instance) != MuiMiscSpecialistClass.Title)
			return 0;
		if (!MuiMiscSpecialistHeaderCodec.TryRead(ref platform, instance,
			out var header)) return 0;
		if (!TryReadTitleState(ref platform, instance, out var titleState))
			return 0;
		var flags = header.Flags;
		if ((flags & MuiMiscSpecialistLayout.FlagTiNewable) == 0) return 0;
		var count = titleState.PageCount;
		if (count >= MuiMiscSpecialistLayout.MaximumPages) return 0;
		var block = titleState.Pages;
		if (block.IsNull)
		{
			block = MuiHeadlessMemory.Allocate(ref platform,
				(uint)MuiMiscSpecialistLayout.MaximumPages *
				MuiMiscSpecialistLayout.PageRecordSize);
			if (block.IsNull) return 0;
			titleState.Pages = block;
		}
		var handle = titleState.PageSequence + 1;
		var cursor = default(MuiTitlePageCursor);
		cursor.Base = block;
		cursor.Index = count;
		if (!MuiTitlePageCursorCodec.TryGetEntry(ref platform, cursor,
			out var pageAddress)) return 0;
		var page = default(MuiTitlePageRecord);
		page.Handle = handle;
		if (!MuiTitlePageCodec.Write(ref platform, pageAddress, page)) return 0;
		titleState.PageSequence = handle;
		titleState.PageCount = count + 1;
		titleState.ActivePage = count;
		if (!WriteTitleState(ref platform, instance, titleState)) return 0;
		return handle;
	}

	// MUIM_Title_Close(tito). Removes the page whose handle matches. Requires
	// MUIA_Title_Closable. Returns whether a page was closed. Records honor
	// OnLastClose only at the observable state level (the last page can still be
	// closed here; window action is a window-level concern left to the caller).
	public static bool TitleClose<TPlatform>(ref TPlatform platform,
		APTR instance, uint handle) where TPlatform : struct, IMuiGuestMemory
	{
		if (Classify(ref platform, instance) != MuiMiscSpecialistClass.Title)
			return false;
		if (!MuiMiscSpecialistHeaderCodec.TryRead(ref platform, instance,
			out var header)) return false;
		if (!TryReadTitleState(ref platform, instance, out var titleState))
			return false;
		var flags = header.Flags;
		if ((flags & MuiMiscSpecialistLayout.FlagTiClosable) == 0) return false;
		var count = titleState.PageCount;
		var block = titleState.Pages;
		if (block.IsNull || count == 0) return false;
		for (var i = 0u; i < count; i++)
		{
			var cursor = default(MuiTitlePageCursor);
			cursor.Base = block;
			cursor.Index = i;
			if (!MuiTitlePageCursorCodec.TryGetEntry(ref platform, cursor,
				out var pageAddress)) return false;
			if (!MuiTitlePageCodec.TryRead(ref platform, pageAddress,
				out var page) || page.Handle != handle) continue;
			// Compact by shifting subsequent records down one slot.
			for (var j = i; j < count - 1; j++)
			{
				cursor.Index = j;
				if (!MuiTitlePageCursorCodec.TryGetEntry(ref platform, cursor,
					out var destination)) return false;
				cursor.Index = j + 1;
				if (!MuiTitlePageCursorCodec.TryGetEntry(ref platform, cursor,
					out var source)) return false;
				if (!MuiTitlePageCodec.TryRead(ref platform, source,
					out var next) || !MuiTitlePageCodec.Write(ref platform,
					destination, next)) return false;
			}
			cursor.Index = count - 1;
			if (!MuiTitlePageCursorCodec.TryGetEntry(ref platform, cursor,
				out var last)) return false;
			if (!MuiTitlePageCodec.Write(ref platform, last, default)) return false;
			titleState.PageCount = count - 1;
			var active = titleState.ActivePage;
			if (active >= count - 1 && count >= 2)
				titleState.ActivePage = count - 2;
			else if (count == 1)
				titleState.ActivePage = 0;
			return WriteTitleState(ref platform, instance, titleState);
		}
		return false;
	}

	// MUIM_Title_FindPage(titlebutton). Returns the zero-based index of the page
	// whose handle matches, or 0xFFFFFFFF when not found.
	public static uint TitleFindPage<TPlatform>(ref TPlatform platform,
		APTR instance, uint handle) where TPlatform : struct, IMuiGuestMemory
	{
		if (Classify(ref platform, instance) != MuiMiscSpecialistClass.Title)
			return 0xFFFFFFFFu;
		if (!TryReadTitleState(ref platform, instance, out var titleState))
			return 0xFFFFFFFFu;
		var count = titleState.PageCount;
		var block = titleState.Pages;
		if (block.IsNull) return 0xFFFFFFFFu;
		for (var i = 0u; i < count; i++)
		{
			var cursor = default(MuiTitlePageCursor);
			cursor.Base = block;
			cursor.Index = i;
			if (!MuiTitlePageCursorCodec.TryGetEntry(ref platform, cursor,
				out var pageAddress)) return 0xFFFFFFFFu;
			if (MuiTitlePageCodec.TryRead(ref platform, pageAddress,
				out var page) && page.Handle == handle) return i;
		}
		return 0xFFFFFFFFu;
	}

	public static uint TitlePageCount<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		Classify(ref platform, instance) == MuiMiscSpecialistClass.Title &&
		TryReadTitleState(ref platform, instance, out var titleState)
			? titleState.PageCount : 0;

	// ---- Scrmodelist bounded screenmode records ------------------------------

	// Append a display mode id to the private, bounded screenmode record store.
	// Failure-atomic: a full store or a failed allocation returns false. This is
	// the private class's only observable state; no public attributes exist.
	public static bool ScrmodelistAddMode<TPlatform>(ref TPlatform platform,
		APTR instance, uint modeId) where TPlatform : struct, IMuiServicePlatform
	{
		if (Classify(ref platform, instance) != MuiMiscSpecialistClass.Scrmodelist)
			return false;
		if (!TryReadScrmodelistState(ref platform, instance,
			out var scrmodelistState)) return false;
		var count = scrmodelistState.ModeCount;
		if (count >= MuiMiscSpecialistLayout.MaximumModes) return false;
		var block = scrmodelistState.Modes;
		if (block.IsNull)
		{
			block = MuiHeadlessMemory.Allocate(ref platform,
				(uint)MuiMiscSpecialistLayout.MaximumModes *
				MuiMiscSpecialistLayout.ModeRecordSize);
			if (block.IsNull) return false;
			scrmodelistState.Modes = block;
		}
		var cursor = default(MuiScrmodelistModeCursor);
		cursor.Base = block;
		cursor.Index = count;
		if (!MuiScrmodelistModeCursorCodec.TryGetEntry(ref platform, cursor,
			out var address)) return false;
		var record = default(MuiScrmodelistModeRecord);
		record.ModeId = modeId;
		if (!MuiScrmodelistModeCodec.Write(ref platform, address, record))
			return false;
		scrmodelistState.ModeCount = count + 1;
		return WriteScrmodelistState(ref platform, instance, scrmodelistState);
	}

	public static uint ScrmodelistModeCount<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		Classify(ref platform, instance) == MuiMiscSpecialistClass.Scrmodelist &&
		TryReadScrmodelistState(ref platform, instance,
			out var scrmodelistState)
			? scrmodelistState.ModeCount : 0;

	public static uint ScrmodelistModeAt<TPlatform>(ref TPlatform platform,
		APTR instance, uint index) where TPlatform : struct, IMuiGuestMemory
	{
		if (Classify(ref platform, instance) != MuiMiscSpecialistClass.Scrmodelist)
			return 0;
		if (!TryReadScrmodelistState(ref platform, instance,
			out var scrmodelistState)) return 0;
		var count = scrmodelistState.ModeCount;
		if (index >= count) return 0;
		var block = scrmodelistState.Modes;
		if (block.IsNull) return 0;
		var cursor = default(MuiScrmodelistModeCursor);
		cursor.Base = block;
		cursor.Index = index;
		if (!MuiScrmodelistModeCursorCodec.TryGetEntry(ref platform, cursor,
			out var address)) return 0;
		return MuiScrmodelistModeCodec.TryRead(ref platform,
			address, out var record) ? record.ModeId : 0;
	}

	// ---- Fontdisplay minmax / draw (documented state only) -------------------

	// Bounded MUI_AskMinMax for Fontdisplay: a fixed sample area that can grow.
	// Writes the 12-byte MUI_MinMax (six UWORDs). No undocumented attributes are
	// invented.
	public static bool FontdisplayAskMinMax<TPlatform>(ref TPlatform platform,
		APTR instance, APTR storage) where TPlatform : struct, IMuiGuestMemory
	{
		if (Classify(ref platform, instance) != MuiMiscSpecialistClass.Fontdisplay
			|| storage.IsNull || !platform.IsMapped(storage, 12)) return false;
		var values = default(MuiMinMaxValues);
		values.MinWidth = 40;
		values.MinHeight = 16;
		values.MaxWidth = 10000;
		values.MaxHeight = 10000;
		values.DefWidth = 160;
		values.DefHeight = 24;
		return MuiAreaLayoutCore.WriteMinMax(ref platform, storage, values);
	}

	// MUIM_Draw for Fontdisplay: records the laid-out natural size and reports
	// drawability. Disabled objects still report drawability (drawing dims them
	// elsewhere); an invalid object is not drawable.
	public static bool FontdisplayDraw<TPlatform>(ref TPlatform platform,
		APTR instance, uint width, uint height)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (Classify(ref platform, instance) != MuiMiscSpecialistClass.Fontdisplay)
			return false;
		var size = default(MuiMiscFontdisplaySize);
		size.Width = width;
		size.Height = height;
		return WriteFontdisplaySize(ref platform, instance, size);
	}

	// ---- Notification accessors ----------------------------------------------

	public static uint NotificationCount<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscSpecialistHeaderCodec.TryRead(ref platform, instance,
			out var header) ? header.NotifyCount : 0;

	public static uint LastNotifiedAttribute<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscSpecialistHeaderCodec.TryRead(ref platform, instance,
			out var header) ? header.NotifyAttribute : 0;

	public static uint LastNotifiedValue<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscSpecialistHeaderCodec.TryRead(ref platform, instance,
			out var header) ? header.NotifyValue : 0;

	// ---- Recursive class-owned disposal --------------------------------------

	// Free every class-owned block and dispose every adopted child. Filepanel
	// disposes its adopted row label/contents objects through the BOOPSI seam
	// and releases any live ASL requester first; owned strings, the ASL state,
	// the FilterFunc scratch and the page/registry/mode blocks are freed. The
	// referenced Application (Aboutmui), the Panel window, caller-owned Mccprefs
	// gadget/label references and the FilterFunc hook are never freed.
	internal static void DisposeOwned<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiServicePlatform
	{
		CancelAsl(ref platform, instance);
		if (!TryReadFilepanelServiceState(ref platform, instance,
			out var filepanelState)) return;

		// Dispose adopted Filepanel row children.
		var rows = filepanelState.Rows;
		if (rows.IsNotNull)
		{
			var rowCount = filepanelState.RowCount;
			for (var i = 0u; i < rowCount; i++)
			{
				var cursor = default(MuiFilepanelRowCursor);
				cursor.Base = rows;
				cursor.Index = i;
				if (!MuiFilepanelRowCursorCodec.TryGetEntry(ref platform, cursor,
					out var rowAddress)) break;
				if (!MuiFilepanelRowCodec.TryRead(ref platform, rowAddress,
					out var row)) break;
				if (row.Contents.IsNotNull) platform.DisposeObject(row.Contents);
				if (row.Label.IsNotNull) platform.DisposeObject(row.Label);
			}
			Free(ref platform, rows, (uint)MuiMiscSpecialistLayout.MaximumRows *
				MuiMiscSpecialistLayout.RowRecordSize);
			filepanelState.Rows = APTR.Null;
			filepanelState.RowCount = 0;
		}

		FreeOwnedString(ref platform, instance, MuiMiscOwnedStringField.Key);
		FreeOwnedString(ref platform, instance, MuiMiscOwnedStringField.ArgTemplate);
		FreeOwnedString(ref platform, instance, MuiMiscOwnedStringField.ArgContents);
		FreeOwnedString(ref platform, instance,
			MuiMiscOwnedStringField.FilepanelDrawer);
		FreeOwnedString(ref platform, instance,
			MuiMiscOwnedStringField.FilepanelFile);
		FreeOwnedString(ref platform, instance,
			MuiMiscOwnedStringField.FilepanelPattern);
		FreeOwnedString(ref platform, instance,
			MuiMiscOwnedStringField.FilepanelAcceptPattern);
		FreeOwnedString(ref platform, instance,
			MuiMiscOwnedStringField.FilepanelRejectPattern);

		if (TryReadTitleState(ref platform, instance, out var titleState) &&
			titleState.Pages.IsNotNull)
		{
			Free(ref platform, titleState.Pages,
				(uint)MuiMiscSpecialistLayout.MaximumPages *
				MuiMiscSpecialistLayout.PageRecordSize);
			titleState.Pages = APTR.Null;
			titleState.PageCount = 0;
			titleState.ActivePage = 0;
			WriteTitleState(ref platform, instance, titleState);
		}
		if (TryReadMccprefsState(ref platform, instance,
			out var mccprefsState) && mccprefsState.Registry.IsNotNull)
		{
			Free(ref platform, mccprefsState.Registry,
				(uint)MuiMiscSpecialistLayout.MaximumRegistry *
				MuiMiscSpecialistLayout.RegistryRecordSize);
			mccprefsState.Registry = APTR.Null;
			mccprefsState.RegistryCount = 0;
			mccprefsState.RegistryConfig = APTR.Null;
			mccprefsState.RegistryOriginator = APTR.Null;
			WriteMccprefsState(ref platform, instance, mccprefsState);
		}
		if (TryReadScrmodelistState(ref platform, instance,
			out var scrmodelistState) && scrmodelistState.Modes.IsNotNull)
		{
			Free(ref platform, scrmodelistState.Modes,
				(uint)MuiMiscSpecialistLayout.MaximumModes *
				MuiMiscSpecialistLayout.ModeRecordSize);
			scrmodelistState.Modes = APTR.Null;
			scrmodelistState.ModeCount = 0;
			scrmodelistState.ActiveMode = 0;
			WriteScrmodelistState(ref platform, instance, scrmodelistState);
		}

		var aslState = filepanelState.AslState;
		if (aslState.IsNotNull)
		{
			Free(ref platform, aslState, MuiMiscSpecialistLayout.AslStateSize);
			filepanelState.AslState = APTR.Null;
		}
		var hookMsg = filepanelState.HookMsg;
		if (hookMsg.IsNotNull)
		{
			Free(ref platform, hookMsg, MuiMiscSpecialistLayout.HookMsgSize);
			filepanelState.HookMsg = APTR.Null;
		}
		WriteFilepanelServiceState(ref platform, instance, filepanelState);
	}

	// ---- Internals -----------------------------------------------------------

	// Release any live Filepanel ASL requester exactly once and clear the active
	// flag. Idempotent. Only Filepanel owns an ASL state block.
	private static void CancelAsl<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiServicePlatform
	{
		SetFlag(ref platform, instance, MuiMiscSpecialistLayout.FlagFpAslActive,
			false);
	}

	private static bool SetKeyadjustFlag<TPlatform>(ref TPlatform platform,
		APTR instance, MuiMiscSpecialistClass cls, uint bit, uint attribute,
		uint value, bool isInit, bool notify, out bool changed)
		where TPlatform : struct, IMuiGuestMemory
	{
		changed = false;
		if (cls != MuiMiscSpecialistClass.Keyadjust) return false;
		changed = SetFlag(ref platform, instance, bit, value != 0);
		Notify(ref platform, instance, attribute, value, isInit, notify, changed);
		return true;
	}

	private static bool SetTitleFlag<TPlatform>(ref TPlatform platform,
		APTR instance, MuiMiscSpecialistClass cls, uint bit, uint attribute,
		uint value, bool isInit, bool notify, out bool changed)
		where TPlatform : struct, IMuiGuestMemory
	{
		changed = false;
		if (cls != MuiMiscSpecialistClass.Title) return false;
		changed = SetFlag(ref platform, instance, bit, value != 0);
		Notify(ref platform, instance, attribute, value, isInit, notify, changed);
		return true;
	}

	private static bool SetFilepanelInitFlag<TPlatform>(ref TPlatform platform,
		APTR instance, MuiMiscSpecialistClass cls, uint bit, uint value,
		bool isInit, out bool changed) where TPlatform : struct, IMuiGuestMemory
	{
		changed = false;
		if (cls != MuiMiscSpecialistClass.Filepanel || !isInit) return false;
		changed = SetFlag(ref platform, instance, bit, value != 0);
		return true;
	}

	private static bool SetFilepanelString<TPlatform>(ref TPlatform platform,
		APTR instance, MuiMiscSpecialistClass cls,
		MuiMiscOwnedStringField field,
		uint attribute, uint value, bool isInit, bool notify, out bool changed)
		where TPlatform : struct, IMuiServicePlatform
	{
		changed = false;
		if (cls != MuiMiscSpecialistClass.Filepanel) return false;
		if (!SetOwnedString(ref platform, instance, field, value,
			out changed)) return false;
		Notify(ref platform, instance, attribute, value, isInit, notify, changed);
		return true;
	}

	private static bool GetFilepanelField<TPlatform>(ref TPlatform platform,
		APTR instance, MuiMiscSpecialistClass cls,
		MuiMiscOwnedStringField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (cls != MuiMiscSpecialistClass.Filepanel) return false;
		if (!TryReadOwnedStringSlot(ref platform, instance, field,
			out var slot)) return false;
		value = slot.Value.Raw;
		return true;
	}

	private static bool GetFlag<TPlatform>(ref TPlatform platform,
		MuiMiscSpecialistClass cls, MuiMiscSpecialistClass required, uint flags,
		uint bit, out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (cls != required) return false;
		value = (flags & bit) != 0 ? 1u : 0u;
		return true;
	}

	private static APTR TitleStateAddress(APTR instance)
	{
		var cursor = default(MuiMiscStateCursor);
		cursor.Instance = instance;
		cursor.Region = MuiMiscStateRegion.Title;
		return MuiMiscStateCursorCodec.TryGetAddress(cursor, out var address)
			? address : APTR.Null;
	}

	private static bool TryReadTitleState<TPlatform>(ref TPlatform platform,
		APTR instance, out MuiMiscTitleState state)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscTitleStateCodec.TryRead(ref platform,
			TitleStateAddress(instance), out state);

	private static bool WriteTitleState<TPlatform>(ref TPlatform platform,
		APTR instance, MuiMiscTitleState state)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscTitleStateCodec.Write(ref platform,
			TitleStateAddress(instance), state);

	private static APTR FilepanelServiceStateAddress(APTR instance)
	{
		var cursor = default(MuiMiscStateCursor);
		cursor.Instance = instance;
		cursor.Region = MuiMiscStateRegion.FilepanelService;
		return MuiMiscStateCursorCodec.TryGetAddress(cursor, out var address)
			? address : APTR.Null;
	}

	private static bool TryReadFilepanelServiceState<TPlatform>(
		ref TPlatform platform, APTR instance,
		out MuiMiscFilepanelServiceState state)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscFilepanelServiceStateCodec.TryRead(ref platform,
			FilepanelServiceStateAddress(instance), out state);

	private static bool WriteFilepanelServiceState<TPlatform>(
		ref TPlatform platform, APTR instance,
		MuiMiscFilepanelServiceState state)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscFilepanelServiceStateCodec.Write(ref platform,
			FilepanelServiceStateAddress(instance), state);

	private static APTR MccprefsStateAddress(APTR instance)
	{
		var cursor = default(MuiMiscStateCursor);
		cursor.Instance = instance;
		cursor.Region = MuiMiscStateRegion.Mccprefs;
		return MuiMiscStateCursorCodec.TryGetAddress(cursor, out var address)
			? address : APTR.Null;
	}

	private static bool TryReadMccprefsState<TPlatform>(ref TPlatform platform,
		APTR instance, out MuiMiscMccprefsState state)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscMccprefsStateCodec.TryRead(ref platform,
			MccprefsStateAddress(instance), out state);

	private static bool WriteMccprefsState<TPlatform>(ref TPlatform platform,
		APTR instance, MuiMiscMccprefsState state)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscMccprefsStateCodec.Write(ref platform,
			MccprefsStateAddress(instance), state);

	private static APTR ScrmodelistStateAddress(APTR instance)
	{
		var cursor = default(MuiMiscStateCursor);
		cursor.Instance = instance;
		cursor.Region = MuiMiscStateRegion.Scrmodelist;
		return MuiMiscStateCursorCodec.TryGetAddress(cursor, out var address)
			? address : APTR.Null;
	}

	private static bool TryReadScrmodelistState<TPlatform>(
		ref TPlatform platform, APTR instance,
		out MuiMiscScrmodelistState state)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscScrmodelistStateCodec.TryRead(ref platform,
			ScrmodelistStateAddress(instance), out state);

	private static bool WriteScrmodelistState<TPlatform>(
		ref TPlatform platform, APTR instance, MuiMiscScrmodelistState state)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscScrmodelistStateCodec.Write(ref platform,
			ScrmodelistStateAddress(instance), state);

	private static APTR WindowPanelStateAddress(APTR instance)
	{
		var cursor = default(MuiMiscStateCursor);
		cursor.Instance = instance;
		cursor.Region = MuiMiscStateRegion.WindowPanel;
		return MuiMiscStateCursorCodec.TryGetAddress(cursor, out var address)
			? address : APTR.Null;
	}

	private static bool TryReadWindowPanelState<TPlatform>(
		ref TPlatform platform, APTR instance,
		out MuiMiscWindowPanelState state)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscWindowPanelStateCodec.TryRead(ref platform,
			WindowPanelStateAddress(instance), out state);

	private static bool WriteWindowPanelState<TPlatform>(
		ref TPlatform platform, APTR instance, MuiMiscWindowPanelState state)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscWindowPanelStateCodec.Write(ref platform,
			WindowPanelStateAddress(instance), state);

	private static APTR ProtectionStateAddress(APTR instance)
	{
		var cursor = default(MuiMiscStateCursor);
		cursor.Instance = instance;
		cursor.Region = MuiMiscStateRegion.Protection;
		return MuiMiscStateCursorCodec.TryGetAddress(cursor, out var address)
			? address : APTR.Null;
	}

	private static bool TryReadProtectionState<TPlatform>(ref TPlatform platform,
		APTR instance, out MuiMiscProtectionState state)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscProtectionStateCodec.TryRead(ref platform,
			ProtectionStateAddress(instance), out state);

	private static bool WriteProtectionState<TPlatform>(ref TPlatform platform,
		APTR instance, MuiMiscProtectionState state)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscProtectionStateCodec.Write(ref platform,
			ProtectionStateAddress(instance), state);

	private static APTR FontdisplaySizeAddress(APTR instance)
	{
		var cursor = default(MuiMiscStateCursor);
		cursor.Instance = instance;
		cursor.Region = MuiMiscStateRegion.Fontdisplay;
		return MuiMiscStateCursorCodec.TryGetAddress(cursor, out var address)
			? address : APTR.Null;
	}

	private static bool TryReadFontdisplaySize<TPlatform>(
		ref TPlatform platform, APTR instance,
		out MuiMiscFontdisplaySize state)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscFontdisplaySizeCodec.TryRead(ref platform,
			FontdisplaySizeAddress(instance), out state);

	private static bool WriteFontdisplaySize<TPlatform>(
		ref TPlatform platform, APTR instance, MuiMiscFontdisplaySize state)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscFontdisplaySizeCodec.Write(ref platform,
			FontdisplaySizeAddress(instance), state);

	private static APTR OwnedStringSlotAddress(APTR instance,
		MuiMiscOwnedStringField field)
	{
		var cursor = default(MuiMiscOwnedStringCursor);
		cursor.Instance = instance;
		cursor.Field = field;
		return MuiMiscOwnedStringCursorCodec.TryGetAddress(cursor,
			out var address) ? address : APTR.Null;
	}

	private static bool TryReadOwnedStringSlot<TPlatform>(
		ref TPlatform platform, APTR instance, MuiMiscOwnedStringField field,
		out MuiMiscOwnedStringSlot slot)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscOwnedStringSlotCodec.TryRead(ref platform,
			OwnedStringSlotAddress(instance, field), out slot);

	private static bool WriteOwnedStringSlot<TPlatform>(
		ref TPlatform platform, APTR instance, MuiMiscOwnedStringField field,
		MuiMiscOwnedStringSlot slot)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscOwnedStringSlotCodec.Write(ref platform,
			OwnedStringSlotAddress(instance, field), slot);

	// Replace an owned string field failure-atomically. A Null/zero value clears
	// the field. The previous copy is freed only after the replacement succeeds.
	private static bool SetOwnedString<TPlatform>(ref TPlatform platform,
		APTR instance, MuiMiscOwnedStringField field, uint value,
		out bool changed)
		where TPlatform : struct, IMuiServicePlatform
	{
		changed = false;
		if (!TryReadOwnedStringSlot(ref platform, instance, field,
			out var previousSlot)) return false;
		APTR copy = APTR.Null;
		uint copySize = 0;
		if (value != 0)
		{
			if (!CopyString(ref platform, APTR.FromPointer(value), out copy,
				out copySize)) return false;   // atomic: nothing touched
		}
		var previous = previousSlot.Value;
		var previousSize = previousSlot.AllocationSize;
		changed = previous.Raw != copy.Raw;
		previousSlot.Value = copy;
		previousSlot.AllocationSize = copySize;
		if (!WriteOwnedStringSlot(ref platform, instance, field, previousSlot))
		{
			if (copy.IsNotNull) Free(ref platform, copy, copySize);
			return false;
		}
		if (previous.IsNotNull) Free(ref platform, previous, previousSize);
		return true;
	}

	private static void FreeOwnedString<TPlatform>(ref TPlatform platform,
		APTR instance, MuiMiscOwnedStringField field)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!TryReadOwnedStringSlot(ref platform, instance, field,
			out var slot)) return;
		var block = slot.Value;
		if (block.IsNull) return;
		Free(ref platform, block, slot.AllocationSize);
		slot.Value = APTR.Null;
		slot.AllocationSize = 0;
		WriteOwnedStringSlot(ref platform, instance, field, slot);
	}

	private static bool CopyString<TPlatform>(ref TPlatform platform, APTR source,
		out APTR block, out uint size) where TPlatform : struct, IMuiServicePlatform
	{
		block = APTR.Null;
		size = 0;
		if (source.IsNull) return false;
		if (!CStringCodec.TryReadLength(ref platform, source,
			MuiMiscSpecialistLayout.MaximumString + 1, out var length))
			return false;
		var total = length + 1;
		var b = MuiHeadlessMemory.Allocate(ref platform, total);
		if (b.IsNull) return false;
		for (var i = 0u; i < total; i++)
			platform.WriteUInt8(b, (int)i, platform.ReadUInt8(source, (int)i));
		block = b;
		size = total;
		return true;
	}

	private static void Notify<TPlatform>(ref TPlatform platform, APTR instance,
		uint attribute, uint value, bool isInit, bool notify, bool changed)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (isInit || !notify || !changed) return;
		if (!MuiMiscSpecialistHeaderCodec.TryRead(ref platform, instance,
			out var header)) return;
		header.NotifyAttribute = attribute;
		header.NotifyValue = value;
		header.NotifyCount++;
		MuiMiscSpecialistHeaderCodec.Write(ref platform, instance, header);
	}

	private static bool SetFlag<TPlatform>(ref TPlatform platform, APTR instance,
		uint bit, bool set) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiMiscSpecialistHeaderCodec.TryRead(ref platform, instance,
			out var header)) return false;
		var updated = set ? header.Flags | bit : header.Flags & ~bit;
		if (updated == header.Flags) return false;
		header.Flags = updated;
		MuiMiscSpecialistHeaderCodec.Write(ref platform, instance, header);
		return true;
	}

	private static void Free<TPlatform>(ref TPlatform platform, APTR block,
		uint size) where TPlatform : struct, IMuiExecCapability, IMuiGuestMemory
	{
		if (block.IsNull || size == 0) return;
		platform.Clear(block, size);
		platform.Free(block, size);
	}
}

// Official MG09 misc-family attribute and method identifiers, resolved from the
// authority (libraries/mui.h in the frozen MorphOS 3.20 SDK, mirrored in the
// abi-inventory) and kept beside the core so classification and dispatch stay
// byte-exact.
//
// Inheritance (from the MUI class autodocs):
//   Keyadjust.mui        : Group    (key/allow/force policies + recorded input)
//   Panel.mui            : Group    (MUIM_Panel_Run; honest undocumented boundary)
//   Filepanel.mui        : Panel    (owned strings, init booleans, FilterFunc
//                                     hook, AddRow, ASL integration)
//   Fontdisplay.mui      : Area     (documented state/minmax/draw only)
//   Scrmodelist.mui      : List     (PRIVATE; bounded screenmode records)
//   Argstring.mui        : String   (owned Template/Contents + formatting)
//   Aboutmui.mui         : Window   (Application ref; self-close lifetime)
//   Mccprefs.mui         : Group    (bounded gadget registry; Config<->Gadgets)
//   FSProtectionBits.mui : Group    (Flags state + notification)
//   Title.mui            : Group    (page topology; New/Close/FindPage)
//
// Disposition notes required by the goal:
//  * Fontdisplay, Panel and the private Scrmodelist publish no own attributes in
//    the authority. Only their ABI-visible / state-observable behavior is
//    implemented (Fontdisplay minmax/draw + recorded size; Panel_Run's honest
//    validated boundary; Scrmodelist's private bounded records). No undocumented
//    attributes are invented and no method returns unconditional success.
public static class MuiMiscAttributes
{
	// Shared Area attribute.
	public const uint Disabled = 0x80423661u;

	// Standard BOOPSI/MUI method identifiers handled by the family.
	public const uint Setup = 0x80428354u;
	public const uint Cleanup = 0x8042d985u;
	public const uint HandleInput = 0x80422a1au;

	// Keyadjust.mui
	public const uint Keyadjust_AllowDoubleClick = 0x8042be82u;
	public const uint Keyadjust_AllowMouseEvents = 0x8042b61cu;
	public const uint Keyadjust_AllowMultipleKeys = 0x8042890bu;
	public const uint Keyadjust_AllowTripleClick = 0x8042fd79u;
	public const uint Keyadjust_ForceKeyCode = 0x8042fbadu;
	public const uint Keyadjust_Key = 0x8042e161u;

	// Panel.mui
	public const uint Panel_Run = 0x8042d789u;   // MUIM

	// Filepanel.mui
	public const uint Filepanel_AddRow = 0x80421d3bu;   // MUIM
	public const uint Filepanel_AcceptPattern = 0x80426f3bu;
	public const uint Filepanel_DoMultiSelect = 0x8042fd78u;
	public const uint Filepanel_DoPatterns = 0x80420b3bu;
	public const uint Filepanel_DoSaveMode = 0x80429022u;
	public const uint Filepanel_Drawer = 0x8042e802u;
	public const uint Filepanel_DrawersOnly = 0x80427726u;
	public const uint Filepanel_File = 0x80427acfu;
	public const uint Filepanel_FilterDrawers = 0x804298a1u;
	public const uint Filepanel_FilterFunc = 0x80429c9du;
	public const uint Filepanel_Pattern = 0x8042c330u;
	public const uint Filepanel_RejectIcons = 0x80423450u;
	public const uint Filepanel_RejectPattern = 0x804281abu;

	// Argstring.mui
	public const uint Argstring_Contents = 0x80429456u;
	public const uint Argstring_Template = 0x80422904u;

	// Aboutmui.mui
	public const uint Aboutmui_Application = 0x80422523u;

	// Mccprefs.mui
	public const uint Mccprefs_ConfigToGadgets = 0x80427043u;   // MUIM
	public const uint Mccprefs_GadgetsToConfig = 0x80425242u;   // MUIM
	public const uint Mccprefs_RegisterGadget = 0x80424828u;    // MUIM

	// FSProtectionBits.mui
	public const uint FSProtectionBits_Flags = 0x8042330cu;

	// Title.mui
	public const uint Title_Close = 0x8042303au;      // MUIM
	public const uint Title_FindPage = 0x80423d0du;   // MUIM
	public const uint Title_New = 0x804247a6u;        // MUIM
	public const uint Title_Clickable = 0x80425959u;
	public const uint Title_Closable = 0x80420402u;
	public const uint Title_EventHandlerPriority = 0x804286bcu;
	public const uint Title_Newable = 0x80424145u;
	public const uint Title_OnLastClose = 0x804253cfu;
	public const uint Title_Position = 0x804273a3u;
	public const uint Title_Sortable = 0x804211f1u;

	// MUIV_Title_Position_* values.
	public const uint Title_Position_Top = 0u;
	public const uint Title_Position_Bottom = 1u;
	public const uint Title_Position_Left = 2u;
	public const uint Title_Position_Right = 3u;
}
