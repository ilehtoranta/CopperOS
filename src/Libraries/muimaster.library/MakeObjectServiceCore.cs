/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Host-side view of the variable MUI_MakeObjectA parameter prefix. The guest
// vector is decoded once at this boundary; construction code consumes named
// fields rather than repeating byte offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMakeObjectParameterRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint FirstOffset = 0;
	internal const uint SecondOffset = 4;
	internal const uint ThirdOffset = 8;
	internal const uint FourthOffset = 12;

	internal uint First;
	internal uint Second;
	internal uint Third;
	internal uint Fourth;
}

internal enum MuiMakeObjectParameterField : byte
{
	First,
	Second,
	Third,
	Fourth,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMakeObjectParameterFieldCursor
{
	internal APTR Base;
	internal MuiMakeObjectParameterField Field;
}

internal static class MuiMakeObjectParameterFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMakeObjectParameterFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiMakeObjectParameterMemoryCodec.TryGetAddress(ref platform,
			cursor.Base, cursor.Field, MuiMakeObjectParameterRecord.Size,
			out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR parameters, MuiMakeObjectParameterField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiMakeObjectParameterMemoryCodec.TryReadUInt32(ref platform,
			parameters, field, MuiMakeObjectParameterRecord.Size, out value);
}

// Struct-first guest-memory adapter for the variable MUI_MakeObjectA
// parameter prefix. The caller supplies the admitted prefix length so a
// short object form does not require bytes beyond its actual vector.
internal static class MuiMakeObjectParameterMemoryCodec
{
	private static bool TryResolve(MuiMakeObjectParameterField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiMakeObjectParameterField.First => MuiMakeObjectParameterRecord.FirstOffset,
			MuiMakeObjectParameterField.Second => MuiMakeObjectParameterRecord.SecondOffset,
			MuiMakeObjectParameterField.Third => MuiMakeObjectParameterRecord.ThirdOffset,
			MuiMakeObjectParameterField.Fourth => MuiMakeObjectParameterRecord.FourthOffset,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR parameters, MuiMakeObjectParameterField field, uint availableBytes,
		out APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || parameters.IsNull ||
			availableBytes < offset || availableBytes - offset <
			MuiMakeObjectParameterRecord.FieldSize ||
			parameters.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(parameters.Raw + offset);
		return platform.IsMapped(address, MuiMakeObjectParameterRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR parameters, MuiMakeObjectParameterField field, uint availableBytes,
		out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, parameters, field, availableBytes,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}
}

internal static class MuiMakeObjectParameterCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR parameters, uint count, out MuiMakeObjectParameterRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (count == 0) return true;
		if (parameters.IsNull || count > 4 ||
			!platform.IsMapped(parameters, count * 4) ||
			!MuiMakeObjectParameterMemoryCodec.TryReadUInt32(ref platform,
				parameters, MuiMakeObjectParameterField.First, count *
				MuiMakeObjectParameterRecord.FieldSize, out record.First))
			return false;
		if (count > 1 &&
			!MuiMakeObjectParameterMemoryCodec.TryReadUInt32(ref platform,
				parameters, MuiMakeObjectParameterField.Second, count *
				MuiMakeObjectParameterRecord.FieldSize, out record.Second))
			return false;
		if (count > 2 &&
			!MuiMakeObjectParameterMemoryCodec.TryReadUInt32(ref platform,
				parameters, MuiMakeObjectParameterField.Third, count *
				MuiMakeObjectParameterRecord.FieldSize, out record.Third))
			return false;
		if (count > 3 &&
			!MuiMakeObjectParameterMemoryCodec.TryReadUInt32(ref platform,
				parameters, MuiMakeObjectParameterField.Fourth, count *
				MuiMakeObjectParameterRecord.FieldSize, out record.Fourth))
			return false;
		return true;
	}
}

// Fixed GadTools NewMenu entry as it crosses the guest-memory boundary. The
// parser consumes this named record; only this codec knows the packed offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNewMenuRecord
{
	internal const uint Size = 20;
	internal const uint ByteFieldSize = 1;
	internal const uint WordFieldSize = 2;
	internal const uint LongFieldSize = 4;
	internal const uint TypeOffset = 0;
	internal const uint PaddingOffset = 1;
	internal const uint LabelOffset = 2;
	internal const uint CommandKeyOffset = 6;
	internal const uint FlagsOffset = 10;
	internal const uint MutualExcludeOffset = 12;
	internal const uint UserDataOffset = 16;
	internal byte Type;
	internal byte Padding;
	internal uint Label;
	internal uint CommandKey;
	internal ushort Flags;
	internal uint MutualExclude;
	internal uint UserData;
}

internal enum MuiNewMenuField : byte
{
	Type,
	Padding,
	Label,
	CommandKey,
	Flags,
	MutualExclude,
	UserData,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNewMenuFieldCursor
{
	internal APTR Record;
	internal MuiNewMenuField Field;
}

internal static class MuiNewMenuFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiNewMenuFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNewMenuRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiNewMenuField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNewMenuRecordMemoryCodec.TryReadUInt32(ref platform, record, field,
			out value);

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiNewMenuField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNewMenuRecordMemoryCodec.TryReadUInt16(ref platform, record, field,
			out value);

	internal static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiNewMenuField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNewMenuRecordMemoryCodec.TryReadUInt8(ref platform, record, field,
			out value);
}

// Struct-first guest-memory adapter for one packed GadTools NewMenu record.
// The named record owns the byte/word/long positions; this bounded adapter is
// the only place that projects those positions into guest memory.
internal static class MuiNewMenuRecordMemoryCodec
{
	private static bool TryResolve(MuiNewMenuField field, out uint offset,
		out uint fieldSize)
	{
		offset = field switch
		{
			MuiNewMenuField.Type => MuiNewMenuRecord.TypeOffset,
			MuiNewMenuField.Padding => MuiNewMenuRecord.PaddingOffset,
			MuiNewMenuField.Label => MuiNewMenuRecord.LabelOffset,
			MuiNewMenuField.CommandKey => MuiNewMenuRecord.CommandKeyOffset,
			MuiNewMenuField.Flags => MuiNewMenuRecord.FlagsOffset,
			MuiNewMenuField.MutualExclude => MuiNewMenuRecord.MutualExcludeOffset,
			MuiNewMenuField.UserData => MuiNewMenuRecord.UserDataOffset,
			_ => uint.MaxValue,
		};
		fieldSize = field == MuiNewMenuField.Type ||
			field == MuiNewMenuField.Padding ? MuiNewMenuRecord.ByteFieldSize :
			field == MuiNewMenuField.Flags ? MuiNewMenuRecord.WordFieldSize :
			MuiNewMenuRecord.LongFieldSize;
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiNewMenuField field, out APTR address,
		out uint fieldSize) where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolve(field, out var offset, out fieldSize) || record.IsNull ||
			record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, MuiNewMenuRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, fieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiNewMenuField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address,
			out var fieldSize) || fieldSize != MuiNewMenuRecord.LongFieldSize)
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiNewMenuField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address,
			out var fieldSize) || fieldSize != MuiNewMenuRecord.WordFieldSize)
			return false;
		value = platform.ReadUInt16(address, 0);
		return true;
	}

	internal static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiNewMenuField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address,
			out var fieldSize) || fieldSize != MuiNewMenuRecord.ByteFieldSize)
			return false;
		value = platform.ReadUInt8(address, 0);
		return true;
	}
}

internal static class MuiNewMenuRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiNewMenuRecord record) where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiNewMenuRecord.Size)) return false;
		if (!MuiNewMenuRecordMemoryCodec.TryReadUInt8(ref platform, address,
			MuiNewMenuField.Type, out record.Type) ||
			!MuiNewMenuRecordMemoryCodec.TryReadUInt8(ref platform, address,
				MuiNewMenuField.Padding, out record.Padding) ||
			!MuiNewMenuRecordMemoryCodec.TryReadUInt32(ref platform, address,
				MuiNewMenuField.Label, out record.Label) ||
			!MuiNewMenuRecordMemoryCodec.TryReadUInt32(ref platform, address,
				MuiNewMenuField.CommandKey, out record.CommandKey) ||
			!MuiNewMenuRecordMemoryCodec.TryReadUInt16(ref platform, address,
				MuiNewMenuField.Flags, out record.Flags) ||
			!MuiNewMenuRecordMemoryCodec.TryReadUInt32(ref platform, address,
				MuiNewMenuField.MutualExclude, out record.MutualExclude) ||
			!MuiNewMenuRecordMemoryCodec.TryReadUInt32(ref platform, address,
				MuiNewMenuField.UserData, out record.UserData)) return false;
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNewMenuCursor
{
	internal const uint EntrySize = MuiNewMenuRecord.Size;
	internal const uint MaximumEntries = 256;
	internal APTR Base;
	internal uint Index;
}

// Struct-first guest-memory adapter for caller-owned NewMenu vectors.
// Complete 20-byte records and the MorphOS 256-entry bound are admitted here;
// the typed cursor remains a compatibility wrapper for existing callers.
internal static class MuiNewMenuVectorMemoryCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (vector.IsNull || index >= MuiNewMenuCursor.MaximumEntries ||
			index > (uint.MaxValue - vector.Raw) / MuiNewMenuRecord.Size) return false;
		var offset = index * MuiNewMenuRecord.Size;
		if (vector.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(vector.Raw + offset);
		return platform.IsMapped(address, MuiNewMenuRecord.Size);
	}
}

internal static class MuiNewMenuCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiNewMenuCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNewMenuVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index, out address);
	}

// Native-safe bounded implementation of the MorphOS MUI_MakeObjectA helper.
// The parameter vector is not a TagItem list: each object type has its own
// fixed prefix. Unsupported types are rejected until their MorphOS contracts
// have a dedicated implementation and qualification evidence.
public static class MuiMakeObjectServiceCore
{
	public const uint MUIO_Label = 1;
	public const uint MUIO_Button = 2;
	public const uint MUIO_Checkmark = 3;
	public const uint MUIO_Cycle = 4;
	public const uint MUIO_Radio = 5;
	public const uint MUIO_Slider = 6;
	public const uint MUIO_String = 7;
	public const uint MUIO_PopButton = 8;
	public const uint MUIO_HSpace = 9;
	public const uint MUIO_VSpace = 10;
	public const uint MUIO_HBar = 11;
	public const uint MUIO_VBar = 12;
	public const uint MUIO_MenustripNM = 13;
	public const uint MUIO_Menuitem = 14;
	public const uint MUIO_BarTitle = 15;
	public const uint MUIO_NumericButton = 16;

	private const uint LabelSingleFrame = 0x00000100;
	private const uint LabelDoubleFrame = 0x00000200;
	private const uint LabelLeftAligned = 0x00000400;
	private const uint LabelCentered = 0x00000800;
	private const uint LabelFreeVert = 0x00001000;
	private const uint LabelTiny = 0x00002000;
	private const uint LabelDontCopy = 0x00004000;
	private const uint LabelKnownFlags = 0x00007FFF;

	private const uint Frame = 0x8042AC64;
	private const uint InputMode = 0x8042FB04;
	private const uint Background = 0x8042545B;
	private const uint Font = 0x8042BE50;
	private const uint Selected = 0x8042654B;
	private const uint ShowSelState = 0x8042CAAC;
	private const uint ImageSpec = 0x804233D5;
	private const uint ImageFreeHoriz = 0x8042DA84;
	private const uint ImageFreeVert = 0x8042EA28;
	private const uint TextContents = 0x8042F8DC;
	private const uint TextPreParse = 0x8042566D;
	private const uint TextHiChar = 0x804218FF;
	private const uint TextCopy = 0x80427727;
	private const uint TextSetVMax = 0x80420D8B;
	private const uint ControlChar = 0x8042120B;
	private const uint CycleChain = 0x80421CE7;
	private const uint CycleEntries = 0x80420629;
	private const uint RadioEntries = 0x8042B6A1;
	private const uint NumericMin = 0x8042E404;
	private const uint NumericMax = 0x8042D78A;
	private const uint NumericFormat = 0x804263E9;
	private const uint StringMaxLen = 0x80424984;
	private const uint MenuTitle = 0x8042A0E3;
	private const uint MenuEnabled = 0x8042ED48;
	private const uint MenuitemTitle = 0x804218BE;
	private const uint MenuitemShortcut = 0x80422030;
	private const uint MenuitemCheckit = 0x80425ACE;
	private const uint MenuitemChecked = 0x8042562A;
	private const uint MenuitemToggle = 0x80424D5C;
	private const uint MenuitemEnabled = 0x8042AE0F;
	private const uint MenuitemExclude = 0x80420BC6;
	private const uint MenuitemCommandString = 0x8042B9CC;
	private const uint MenuitemCopyStrings = 0x8042DC1B;
	private const uint UserData = 0x80420313;
	private const uint FixWidth = 0x8042A3F1;
	private const uint FixHeight = 0x8042A92B;
	private const uint RectangleBarTitle = 0x80426689;
	private const uint RectangleHBar = 0x8042C943;
	private const uint RectangleVBar = 0x80422204;

	private const uint TextFrame = 3;
	private const uint ButtonFrame = 1;
	private const uint ImageButtonFrame = 2;
	private const uint InputModeRelVerify = 1;
	private const uint InputModeToggle = 3;
	private const uint ButtonBackground = 2;
	private const uint CheckmarkImage = 15;
	private const uint ButtonFont = unchecked((uint)-7);
	private const uint TinyFont = unchecked((uint)-3);
	private const uint MUIImageBuiltinMax = 0x00000093;
	private const uint MUIO_MenustripNMCommandKeyCheck = 1;
	private const uint MUIO_MenuitemCopyStrings = 0x40000000;
	private const uint NewMenuBarLabel = 0xFFFFFFFF;
	private const uint NewMenuMenuDisabled = 0x0001;
	private const uint NewMenuItemDisabled = 0x0010;
	private const uint NewMenuCheckit = 0x0001;
	private const uint NewMenuChecked = 0x0100;
	private const uint NewMenuToggle = 0x0008;
	private const uint NewMenuCommandString = 0x0020;
	private const uint MaximumMenuEntries = 256;

	private const uint ClassText = 1;
	private const uint ClassRectangle = 2;
	private const uint ClassImage = 3;
	private const uint ClassCycle = 4;
	private const uint ClassRadio = 5;
	private const uint ClassSlider = 6;
	private const uint ClassString = 7;
	private const uint ClassNumericbutton = 8;
	private const uint ClassMenustrip = 9;
	private const uint ClassMenu = 10;
	private const uint ClassMenuitem = 11;
	private const uint ClassNameStorage = MuiMakeObjectClassNameRecord.Size;
	private const uint TagStorage = 88; // ten TagItems plus TAG_DONE
	private const uint MaximumCString = 4096;

	public static APTR MakeObjectA<TPlatform>(ref TPlatform platform, APTR state,
		uint type, APTR parameters) where TPlatform : struct, IMuiServicePlatform
	{
		uint parameterCount;
		if (!ParameterCount(type, out parameterCount)) return APTR.Null;
		if (!MuiMakeObjectParameterCodec.TryRead(ref platform, parameters,
			parameterCount, out var parameterRecord)) return APTR.Null;
		if (type == MUIO_MenustripNM)
			return MakeMenustripNM(ref platform, state, parameterRecord.First,
				parameterRecord.Second);

		uint classKind;
		uint tagCount;
		uint preParseKind = 0;
		if (!BuildShape(type, parameterRecord.First, parameterRecord.Second,
			parameterRecord.Third, parameterRecord.Fourth, out classKind,
			out tagCount, out preParseKind)) return APTR.Null;

		if ((type == MUIO_Button || type == MUIO_Label ||
				type == MUIO_BarTitle || type == MUIO_Cycle ||
				type == MUIO_Radio || type == MUIO_Slider ||
				type == MUIO_String || type == MUIO_NumericButton) &&
			!ValidCString(ref platform, parameterRecord.First))
			return APTR.Null;
		if (type == MUIO_Menuitem &&
			!ValidMenuitemLabel(ref platform, parameterRecord.First)) return APTR.Null;
		if (type == MUIO_Menuitem && parameterRecord.Second != 0 &&
			!ValidCString(ref platform, parameterRecord.Second)) return APTR.Null;
		if (type == MUIO_Menuitem &&
			(parameterRecord.Third & ~(NewMenuCheckit | NewMenuChecked | NewMenuToggle |
				NewMenuItemDisabled | NewMenuCommandString |
				MUIO_MenuitemCopyStrings)) != 0)
			return APTR.Null;
		if (type == MUIO_PopButton &&
			!ValidImageSpec(ref platform, parameterRecord.First))
			return APTR.Null;
		if (type == MUIO_NumericButton && parameterRecord.Fourth != 0 &&
			!ValidCString(ref platform, parameterRecord.Fourth)) return APTR.Null;
		if ((type == MUIO_Cycle || type == MUIO_Radio) &&
			!ValidEntryVector(ref platform, parameterRecord.Second,
				type == MUIO_Radio))
			return APTR.Null;

		var className = MuiHeadlessMemory.Allocate(ref platform, ClassNameStorage);
		var tags = MuiHeadlessMemory.Allocate(ref platform, TagStorage);
		var preParse = APTR.Null;
		if (className.IsNull || tags.IsNull)
		{
			ReleaseTemporary(ref platform, className, tags, preParse);
			return APTR.Null;
		}
		if (preParseKind != 0)
		{
			preParse = MuiHeadlessMemory.Allocate(ref platform,
				MuiMakeObjectPreParseRecord.Size);
			if (preParse.IsNull)
			{
				ReleaseTemporary(ref platform, className, tags, preParse);
				return APTR.Null;
			}
			var preParseRecord = default(MuiMakeObjectPreParseRecord);
			preParseRecord.Escape = 0x1B;
			preParseRecord.Command = preParseKind == 1 ? (byte)'c' : (byte)'l';
			if (!MuiMakeObjectPreParseRecordCodec.Write(ref platform, preParse,
				preParseRecord))
			{
				ReleaseTemporary(ref platform, className, tags, preParse);
				return APTR.Null;
			}
		}

		if (!WriteClassName(ref platform, className, classKind) ||
			!WriteTags(ref platform, tags, type, parameterRecord.First,
				parameterRecord.Second, parameterRecord.Third,
				parameterRecord.Fourth, preParse))
		{
			ReleaseTemporary(ref platform, className, tags, preParse);
			return APTR.Null;
		}
		var classRecord = MuiHeadlessObjectCore.FindClassByName(ref platform,
			state, className);
		var obj = classRecord.IsNull ? APTR.Null :
			MuiCommonControlCore.CreateControl(ref platform, state, classRecord,
				tags);
		if (obj.IsNotNull && classKind == ClassMenuitem &&
			AttachMenuSpecialist(ref platform, state, classKind, obj).IsNull)
		{
			MuiHeadlessObjectCore.DisposeObject(ref platform, state, obj);
			obj = APTR.Null;
		}
		ReleaseTemporary(ref platform, className, tags, preParse);
		return obj;
	}

	private static bool ParameterCount(uint type, out uint count)
	{
		switch (type)
		{
			case MUIO_Label:
				count = 2; return true;
			case MUIO_Button:
			case MUIO_Checkmark:
			case MUIO_HSpace:
			case MUIO_VSpace:
			case MUIO_BarTitle:
				count = 1; return true;
			case MUIO_Cycle:
			case MUIO_Radio:
			case MUIO_String:
				count = 2; return true;
			case MUIO_PopButton:
				count = 1; return true;
			case MUIO_Slider:
				count = 3; return true;
			case MUIO_MenustripNM:
				count = 2; return true;
			case MUIO_Menuitem:
				count = 4; return true;
			case MUIO_NumericButton:
				count = 4; return true;
			case MUIO_HBar:
			case MUIO_VBar:
				count = 1; return true;
			default:
				count = 0; return false;
		}
	}

	private static bool BuildShape(uint type, uint p0, uint p1, uint p2,
		uint p3,
		out uint classKind, out uint tagCount, out uint preParseKind)
	{
		classKind = 0;
		tagCount = 0;
		preParseKind = 0;
		switch (type)
		{
			case MUIO_HSpace:
			case MUIO_VSpace:
			case MUIO_HBar:
			case MUIO_VBar:
			case MUIO_BarTitle:
				classKind = ClassRectangle;
				tagCount = type == MUIO_HBar || type == MUIO_VBar ? 2u : 1u;
				return true;
			case MUIO_Button:
				classKind = ClassText;
				tagCount = 6;
				preParseKind = 1;
				return true;
			case MUIO_Checkmark:
				classKind = ClassImage;
				tagCount = 8;
				return true;
			case MUIO_PopButton:
				classKind = ClassImage;
				tagCount = 6;
				return true;
			case MUIO_Cycle:
				classKind = ClassCycle;
				tagCount = 5;
				return true;
			case MUIO_Radio:
				classKind = ClassRadio;
				tagCount = 2;
				return true;
			case MUIO_Slider:
				classKind = ClassSlider;
				tagCount = 3;
				return true;
			case MUIO_String:
				classKind = ClassString;
				tagCount = 3;
				return true;
			case MUIO_Menuitem:
				classKind = ClassMenuitem;
				tagCount = 8u + ((p2 & NewMenuCommandString) != 0 ? 1u : 0u);
				return true;
			case MUIO_NumericButton:
				classKind = ClassNumericbutton;
				tagCount = 4;
				return true;
			case MUIO_Label:
				var flags = p1;
				if ((flags & ~LabelKnownFlags) != 0 ||
					(flags & LabelSingleFrame) != 0 &&
					(flags & LabelDoubleFrame) != 0 ||
					(flags & LabelLeftAligned) != 0 &&
					(flags & LabelCentered) != 0) return false;
				classKind = ClassText;
				tagCount = 2;
				if ((flags & LabelSingleFrame) != 0 ||
					(flags & LabelDoubleFrame) != 0) tagCount++;
				if ((flags & LabelLeftAligned) != 0 ||
					(flags & LabelCentered) != 0) { tagCount++; preParseKind =
					(flags & LabelCentered) != 0 ? 1u : 2u; }
				if ((flags & LabelFreeVert) != 0) tagCount++;
				if ((flags & LabelTiny) != 0) tagCount++;
				if ((flags & 0xFF) != 0) tagCount += 2;
				return tagCount <= 8;
			default:
				return false;
		}
	}

	private static bool WriteClassName<TPlatform>(ref TPlatform platform,
		APTR address, uint classKind) where TPlatform : struct, IMuiGuestMemory
	{
		var value = default(MuiMakeObjectClassNameRecord);
		switch (classKind)
		{
			case ClassText:
				value.Word0 = 0x54657874; // Text
				value.Word1 = 0x2E6D7569; // .mui
				break;
			case ClassRectangle:
				value.Word0 = 0x52656374; // Rect
				value.Word1 = 0x616E676C; // angl
				value.Word2 = 0x652E6D75; // e.mu
				value.Word3 = 0x69000000; // i\0
				break;
			case ClassImage:
				value.Word0 = 0x496D6167; // Imag
				value.Word1 = 0x652E6D75; // e.mu
				value.Word2 = 0x69000000; // i\0
				break;
			case ClassCycle:
				value.Word0 = 0x4379636C; // Cycl
				value.Word1 = 0x652E6D75; // e.mu
				value.Word2 = 0x69000000; // i\0
				break;
			case ClassRadio:
				value.Word0 = 0x52616469; // Radi
				value.Word1 = 0x6F2E6D75; // o.mu
				value.Word2 = 0x69000000; // i\0
				break;
			case ClassSlider:
				value.Word0 = 0x536C6964; // Slid
				value.Word1 = 0x65722E6D; // er.m
				value.Word2 = 0x75690000; // ui\0
				break;
			case ClassString:
				value.Word0 = 0x53747269; // Stri
				value.Word1 = 0x6E672E6D; // ng.m
				value.Word2 = 0x75690000; // ui\0
				break;
			case ClassNumericbutton:
				value.Word0 = 0x4E756D65; // Nume
				value.Word1 = 0x72696362; // ricb
				value.Word2 = 0x7574746F; // utto
				value.Word3 = 0x6E2E6D75; // n.mu
				value.Word4 = 0x69000000; // i\0
				break;
			case ClassMenustrip:
				value.Word0 = 0x4D656E75; // Menu
				value.Word1 = 0x73747269; // stri
				value.Word2 = 0x702E6D75; // p.mu
				value.Word3 = 0x69000000; // i\0
				break;
			case ClassMenu:
				value.Word0 = 0x4D656E75; // Menu
				value.Word1 = 0x2E6D7569; // .mui
				break;
			case ClassMenuitem:
				value.Word0 = 0x4D656E75; // Menu
				value.Word1 = 0x6974656D; // item
				value.Word2 = 0x2E6D7569; // .mui
				break;
			default:
				return false;
		}
		return MuiMakeObjectClassNameRecordCodec.WriteRecord(ref platform, address,
			value);
	}

	private static bool WriteTags<TPlatform>(ref TPlatform platform, APTR tags,
		uint type, uint p0, uint p1, uint p2, uint p3, APTR preParse)
		where TPlatform : struct, IMuiGuestMemory
	{
		uint index = 0;
		if (type == MUIO_HSpace) AddTag(ref platform, tags, ref index,
			FixWidth, p0);
		else if (type == MUIO_VSpace) AddTag(ref platform, tags, ref index,
			FixHeight, p0);
		else if (type == MUIO_HBar)
		{
			AddTag(ref platform, tags, ref index, RectangleHBar, 1);
			AddTag(ref platform, tags, ref index, FixHeight, p0);
		}
		else if (type == MUIO_VBar)
		{
			AddTag(ref platform, tags, ref index, RectangleVBar, 1);
			AddTag(ref platform, tags, ref index, FixWidth, p0);
		}
		else if (type == MUIO_BarTitle) AddTag(ref platform, tags, ref index,
			RectangleBarTitle, p0);
		else if (type == MUIO_Button)
			return WriteButtonTagRecords(ref platform, tags, p0, preParse);
		else if (type == MUIO_Checkmark)
		{
			AddTag(ref platform, tags, ref index, Frame, ImageButtonFrame);
			AddTag(ref platform, tags, ref index, InputMode, InputModeToggle);
			AddTag(ref platform, tags, ref index, ImageSpec, CheckmarkImage);
			AddTag(ref platform, tags, ref index, ImageFreeVert, 1);
			AddTag(ref platform, tags, ref index, Selected, p0);
			AddTag(ref platform, tags, ref index, Background, ButtonBackground);
			AddTag(ref platform, tags, ref index, ShowSelState, 0);
		}
		else if (type == MUIO_PopButton)
		{
			AddTag(ref platform, tags, ref index, Frame, ImageButtonFrame);
			AddTag(ref platform, tags, ref index, Background, ButtonBackground);
			AddTag(ref platform, tags, ref index, ImageSpec, p0);
			AddTag(ref platform, tags, ref index, InputMode, InputModeRelVerify);
			AddTag(ref platform, tags, ref index, ImageFreeVert, 1);
			AddTag(ref platform, tags, ref index, ImageFreeHoriz, 0);
		}
		else if (type == MUIO_Cycle)
		{
			AddTag(ref platform, tags, ref index, Frame, ButtonFrame);
			AddTag(ref platform, tags, ref index, Font, ButtonFont);
			AddTag(ref platform, tags, ref index, CycleEntries, p1);
			var key = ControlCharFromCString(ref platform, p0);
			if (key != 0) AddTag(ref platform, tags, ref index, ControlChar, key);
			AddTag(ref platform, tags, ref index, CycleChain, 1);
		}
		else if (type == MUIO_Radio)
		{
			AddTag(ref platform, tags, ref index, RadioEntries, p1);
			var key = ControlCharFromCString(ref platform, p0);
			if (key != 0) AddTag(ref platform, tags, ref index, ControlChar, key);
		}
		else if (type == MUIO_Slider)
		{
			AddTag(ref platform, tags, ref index, NumericMin, p1);
			AddTag(ref platform, tags, ref index, NumericMax, p2);
			var key = ControlCharFromCString(ref platform, p0);
			if (key != 0) AddTag(ref platform, tags, ref index, ControlChar, key);
		}
		else if (type == MUIO_String)
		{
			AddTag(ref platform, tags, ref index, Frame, 4);
			AddTag(ref platform, tags, ref index, StringMaxLen, p1);
			var key = ControlCharFromCString(ref platform, p0);
			if (key != 0) AddTag(ref platform, tags, ref index, ControlChar, key);
		}
		else if (type == MUIO_NumericButton)
		{
			AddTag(ref platform, tags, ref index, NumericMin, p1);
			AddTag(ref platform, tags, ref index, NumericMax, p2);
			if (p3 != 0) AddTag(ref platform, tags, ref index, NumericFormat, p3);
			var key = ControlCharFromCString(ref platform, p0);
			if (key != 0) AddTag(ref platform, tags, ref index, ControlChar, key);
		}
		else if (type == MUIO_Menuitem)
		{
			// CopyStrings is an init-only latch and must precede the title and
			// shortcut tags so their setters take ownership during OM_NEW.
			AddTag(ref platform, tags, ref index, MenuitemCopyStrings,
				(p2 & MUIO_MenuitemCopyStrings) != 0 ? 1u : 0u);
			AddTag(ref platform, tags, ref index, MenuitemTitle, p0);
			AddTag(ref platform, tags, ref index, MenuitemShortcut, p1);
			AddTag(ref platform, tags, ref index, MenuitemCheckit,
				(p2 & NewMenuCheckit) != 0 ? 1u : 0u);
			AddTag(ref platform, tags, ref index, MenuitemChecked,
				(p2 & NewMenuChecked) != 0 ? 1u : 0u);
			AddTag(ref platform, tags, ref index, MenuitemToggle,
				(p2 & NewMenuToggle) != 0 ? 1u : 0u);
			AddTag(ref platform, tags, ref index, MenuitemEnabled,
				(p2 & NewMenuItemDisabled) == 0 ? 1u : 0u);
			if ((p2 & NewMenuCommandString) != 0)
				AddTag(ref platform, tags, ref index, MenuitemCommandString, 1);
			AddTag(ref platform, tags, ref index, UserData, p3);
		}
		else if (type == MUIO_Label)
		{
			var flags = p1;
			AddTag(ref platform, tags, ref index, TextContents, p0);
			if ((flags & LabelSingleFrame) != 0)
				AddTag(ref platform, tags, ref index, Frame, TextFrame);
			else if ((flags & LabelDoubleFrame) != 0)
				AddTag(ref platform, tags, ref index, Frame, ButtonFrame);
			if ((flags & (LabelLeftAligned | LabelCentered)) != 0)
				AddTag(ref platform, tags, ref index, TextPreParse, preParse.Raw);
			if ((flags & LabelFreeVert) != 0)
				AddTag(ref platform, tags, ref index, TextSetVMax, 0);
			if ((flags & LabelTiny) != 0)
				AddTag(ref platform, tags, ref index, Font, TinyFont);
			AddTag(ref platform, tags, ref index, TextCopy,
				(flags & LabelDontCopy) != 0 ? 0u : 1u);
			if ((flags & 0xFF) != 0)
			{
				var key = flags & 0xFF;
				AddTag(ref platform, tags, ref index, TextHiChar, key);
				AddTag(ref platform, tags, ref index, 0x8042120B, key);
			}
		}
		WriteTagDone(ref platform, tags, index);
		return true;
	}

	// Shared generated-TagItem seam for the button form of MUI_MakeObjectA.
	// Keeping this small typed route separate lets native qualification prove
	// the generated records without pulling the complete object factory into a
	// freestanding closure.
	internal static bool WriteButtonTagRecords<TPlatform>(ref TPlatform platform,
		APTR tags, uint text, APTR preParse)
		where TPlatform : struct, IMuiGuestMemory
	{
		var index = 0u;
		AddTag(ref platform, tags, ref index, Frame, ButtonFrame);
		AddTag(ref platform, tags, ref index, Font, ButtonFont);
		AddTag(ref platform, tags, ref index, TextContents, text);
		AddTag(ref platform, tags, ref index, TextPreParse, preParse.Raw);
		AddTag(ref platform, tags, ref index, InputMode, InputModeRelVerify);
		AddTag(ref platform, tags, ref index, Background, ButtonBackground);
		WriteTagDone(ref platform, tags, index);
		return true;
	}

	private static void AddTag<TPlatform>(ref TPlatform platform, APTR tags,
		ref uint index, uint tag, uint value) where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAslTagItemCursor);
		cursor.Base = tags;
		cursor.Index = index;
		if (!MuiAslTagItemVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index,
			out var address))
		{
			index++;
			return;
		}
		var item = default(MuiAslTagItemRecord);
		item.Tag = tag;
		item.Data = value;
		MuiAslTagItemCodec.Write(ref platform, address, item);
		index++;
	}

	private static APTR MakeMenustripNM<TPlatform>(ref TPlatform platform,
		APTR state, uint newMenuRaw, uint flags)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (ValidateNewMenuCode(ref platform,
			APTR.FromPointer(newMenuRaw), flags) != 0)
			return APTR.Null;
		var emptyTags = MuiHeadlessMemory.Allocate(ref platform, TagStorage);
		if (emptyTags.IsNull) return APTR.Null;
		WriteTagDone(ref platform, emptyTags, 0);
		var strip = CreateRegisteredObject(ref platform, state, ClassMenustrip,
			emptyTags);
		platform.Free(emptyTags, TagStorage);
		if (strip.IsNull) return APTR.Null;

		APTR menu = APTR.Null;
		APTR menuItem = APTR.Null;
		for (var index = 0u; index < MaximumMenuEntries; index++)
		{
			if (!MuiNewMenuVectorMemoryCodec.TryGetEntry(ref platform,
				APTR.FromPointer(newMenuRaw), index, out var entry))
			{
				DisposeMenuTree(ref platform, state, strip);
				return APTR.Null;
			}
			if (!MuiNewMenuRecordCodec.TryRead(ref platform, entry,
				out var menuRecord))
			{
				DisposeMenuTree(ref platform, state, strip);
				return APTR.Null;
			}
			if (!MuiNewMenuTypeRecordCodec.TryClassify(menuRecord.Type,
				out var entryKind))
			{
				DisposeMenuTree(ref platform, state, strip);
				return APTR.Null;
			}
			var label = menuRecord.Label;
			var shortcut = menuRecord.CommandKey;
			var menuFlags = menuRecord.Flags;
			var mutualExclude = menuRecord.MutualExclude;
			var userData = menuRecord.UserData;
			if (entryKind == MuiNewMenuEntryKind.End) return strip;
			if (entryKind == MuiNewMenuEntryKind.Ignored) continue;
			// MorphOS Menuitem.mui deliberately excludes GadTools image menus.
			// Keep the rejection explicit and before any attempt to interpret the
			// label as a text pointer.
			if (entryKind == MuiNewMenuEntryKind.ImageItem ||
				entryKind == MuiNewMenuEntryKind.ImageSub ||
				entryKind == MuiNewMenuEntryKind.ImageUnsupported)
			{
				DisposeMenuTree(ref platform, state, strip);
				return APTR.Null;
			}
			if (entryKind == MuiNewMenuEntryKind.Title)
			{
				var tags = MuiHeadlessMemory.Allocate(ref platform, TagStorage);
				if (tags.IsNull)
				{
					DisposeMenuTree(ref platform, state, strip);
					return APTR.Null;
				}
				var tagIndex = 0u;
				AddTag(ref platform, tags, ref tagIndex, MenuTitle, label);
				AddTag(ref platform, tags, ref tagIndex, UserData, userData);
				AddTag(ref platform, tags, ref tagIndex, MenuEnabled,
					(menuFlags & NewMenuMenuDisabled) == 0 ? 1u : 0u);
				WriteTagDone(ref platform, tags, tagIndex);
				menu = CreateRegisteredObject(ref platform, state, ClassMenu, tags);
				platform.Free(tags, TagStorage);
				if (menu.IsNull || !MuiFamilyCore.AddTail(ref platform, state,
					strip, menu))
				{
					DisposeMenuTree(ref platform, state, strip);
					return APTR.Null;
				}
				menuItem = APTR.Null;
				continue;
			}

			if (!ResolveMenuItemStrings(ref platform, label, shortcut, flags,
				out var effectiveLabel, out var effectiveShortcut))
			{
				DisposeMenuTree(ref platform, state, strip);
				return APTR.Null;
			}
			var itemTags = MuiHeadlessMemory.Allocate(ref platform, TagStorage);
			if (itemTags.IsNull)
			{
				DisposeMenuTree(ref platform, state, strip);
				return APTR.Null;
			}
			var itemTagIndex = 0u;
			AddTag(ref platform, itemTags, ref itemTagIndex, MenuitemTitle,
				effectiveLabel);
			AddTag(ref platform, itemTags, ref itemTagIndex, MenuitemShortcut,
				effectiveShortcut);
			AddTag(ref platform, itemTags, ref itemTagIndex, UserData, userData);
			AddTag(ref platform, itemTags, ref itemTagIndex, MenuitemExclude,
				mutualExclude);
			AddTag(ref platform, itemTags, ref itemTagIndex, MenuitemCheckit,
				(menuFlags & NewMenuCheckit) != 0 ? 1u : 0u);
			AddTag(ref platform, itemTags, ref itemTagIndex, MenuitemChecked,
				(menuFlags & NewMenuChecked) != 0 ? 1u : 0u);
			AddTag(ref platform, itemTags, ref itemTagIndex, MenuitemToggle,
				(menuFlags & NewMenuToggle) != 0 ? 1u : 0u);
			AddTag(ref platform, itemTags, ref itemTagIndex,
				MenuitemCommandString,
				(menuFlags & NewMenuCommandString) != 0 ? 1u : 0u);
			AddTag(ref platform, itemTags, ref itemTagIndex, MenuitemEnabled,
				(menuFlags & NewMenuItemDisabled) == 0 ? 1u : 0u);
			WriteTagDone(ref platform, itemTags, itemTagIndex);
			var item = CreateRegisteredObject(ref platform, state, ClassMenuitem,
				itemTags);
			platform.Free(itemTags, TagStorage);
			var parent = entryKind == MuiNewMenuEntryKind.Sub ? menuItem : menu;
			if (item.IsNull || parent.IsNull ||
				!MuiFamilyCore.AddTail(ref platform, state, parent, item))
			{
				DisposeMenuTree(ref platform, state, strip);
				return APTR.Null;
			}
			if (entryKind == MuiNewMenuEntryKind.Item) menuItem = item;
		}

		DisposeMenuTree(ref platform, state, strip);
		return APTR.Null;
	}

	// MUI_MakeObjectA creates real menu-family objects, not merely generic
	// records with menu attributes. Attach the additive specialist sidecar at
	// construction time so callers can dispatch Menustrip/Menu/Menuitem methods
	// immediately. The helper also keeps all partial-tree rollback paths
	// ownership-correct when a later NewMenu entry fails.
	private static bool DisposeMenuTree<TPlatform>(ref TPlatform platform,
		APTR state, APTR strip)
		where TPlatform : struct, IMuiServicePlatform
	{
		return MuiMenuSpecialistCore.Valid(ref platform, state, strip)
			? MuiMenuSpecialistLifecycle.Dispose(ref platform, state, strip)
			: MuiHeadlessObjectCore.DisposeObject(ref platform, state, strip);
	}

	private static APTR CreateRegisteredObject<TPlatform>(ref TPlatform platform,
		APTR state, uint classKind, APTR tags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var className = MuiHeadlessMemory.Allocate(ref platform, ClassNameStorage);
		if (className.IsNull) return APTR.Null;
		if (!WriteClassName(ref platform, className, classKind))
		{
			platform.Free(className, ClassNameStorage);
			return APTR.Null;
		}
		var classRecord = MuiHeadlessObjectCore.FindClassByName(ref platform,
			state, className);
		var obj = classRecord.IsNull ? APTR.Null :
			MuiHeadlessObjectCore.CreateObjectA(ref platform, state, classRecord,
				tags);
		if (obj.IsNotNull && classKind >= ClassMenustrip &&
			classKind <= ClassMenuitem &&
			AttachMenuSpecialist(ref platform, state, classKind, obj).IsNull)
		{
			MuiHeadlessObjectCore.DisposeObject(ref platform, state, obj);
			obj = APTR.Null;
		}
		platform.Free(className, ClassNameStorage);
		return obj;
	}

	private static APTR AttachMenuSpecialist<TPlatform>(ref TPlatform platform,
		APTR state, uint classKind, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var specialistClass = classKind == ClassMenustrip
			? MuiMenuSpecialistClass.Menustrip
			: classKind == ClassMenu
				? MuiMenuSpecialistClass.Menu
				: MuiMenuSpecialistClass.Menuitem;
		return MuiMenuSpecialistCore.Attach(ref platform, state, obj,
			specialistClass);
	}

	private static uint ValidateNewMenuCode<TPlatform>(ref TPlatform platform,
		APTR newMenu, uint flags) where TPlatform : struct, IMuiGuestMemory
	{
		if (newMenu.IsNull ||
			(flags & ~MUIO_MenustripNMCommandKeyCheck) != 0) return 1;
		var haveMenu = false;
		var haveItem = false;
		for (var index = 0u; index < MaximumMenuEntries; index++)
		{
			if (!MuiNewMenuVectorMemoryCodec.TryGetEntry(ref platform,
				newMenu, index, out var entry)) return 2;
			if (!MuiNewMenuRecordCodec.TryRead(ref platform, entry,
				out var menuRecord)) return 2;
			var entryType = menuRecord.Type;
			if (!MuiNewMenuTypeRecordCodec.TryClassify(entryType,
				out var entryKind)) return 5;
			var label = menuRecord.Label;
			var shortcut = menuRecord.CommandKey;
			if (entryKind == MuiNewMenuEntryKind.End) return 0;
			if (entryKind == MuiNewMenuEntryKind.Ignored) continue;
			if (entryKind == MuiNewMenuEntryKind.ImageItem ||
				entryKind == MuiNewMenuEntryKind.ImageSub ||
				entryKind == MuiNewMenuEntryKind.ImageUnsupported) return 3;
			if (entryKind == MuiNewMenuEntryKind.Title)
			{
				if (!ValidCString(ref platform, label)) return 4;
				haveMenu = true;
				haveItem = false;
				continue;
			}
			if (entryKind != MuiNewMenuEntryKind.Item &&
				entryKind != MuiNewMenuEntryKind.Sub) return 5;
			if (!haveMenu || entryKind == MuiNewMenuEntryKind.Sub && !haveItem)
				return 6;
			if (!ResolveMenuItemStrings(ref platform, label, shortcut, flags,
				out _, out _)) return 7;
			if (entryKind == MuiNewMenuEntryKind.Item) haveItem = true;
		}
		return 8;
	}

	private static bool ResolveMenuItemStrings<TPlatform>(ref TPlatform platform,
		uint rawLabel, uint rawShortcut, uint flags, out uint label,
		out uint shortcut) where TPlatform : struct, IMuiGuestMemory
	{
		label = rawLabel;
		shortcut = rawShortcut;
		if (rawLabel == NewMenuBarLabel)
			return rawShortcut == 0 || ValidCString(ref platform, rawShortcut);
		if (!ValidCString(ref platform, rawLabel)) return false;
		var labelAddress = APTR.FromPointer(rawLabel);
		if ((flags & MUIO_MenustripNMCommandKeyCheck) != 0 &&
			MuiMakeObjectMenuBarLabelRecordCodec.TryReadRecord(ref platform,
				labelAddress, out var labelPrefix) && labelPrefix.Terminator == 0)
		{
			if (rawLabel > uint.MaxValue - 2) return false;
			label = rawLabel + 2;
			shortcut = label;
			return ValidCString(ref platform, label);
		}
		return rawShortcut == 0 || ValidCString(ref platform, rawShortcut);
	}

	private static bool ValidMenuitemLabel<TPlatform>(ref TPlatform platform,
		uint raw) where TPlatform : struct, IMuiGuestMemory =>
		raw == 0 || raw == NewMenuBarLabel || ValidCString(ref platform, raw);

	private static void WriteTagDone<TPlatform>(ref TPlatform platform, APTR tags,
		uint index) where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAslTagItemCursor);
		cursor.Base = tags;
		cursor.Index = index;
		if (!MuiAslTagItemVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index,
			out var address)) return;
		var item = default(MuiAslTagItemRecord);
		item.Tag = MuiAslTagListCore.TagDone;
		MuiAslTagItemCodec.Write(ref platform, address, item);
	}

	private static bool ValidCString<TPlatform>(ref TPlatform platform, uint raw)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (raw == 0) return true;
		uint length;
		return CStringCodec.TryReadLength(ref platform, APTR.FromPointer(raw),
			MaximumCString + 1, out length);
	}

	private static bool ValidImageSpec<TPlatform>(ref TPlatform platform, uint raw)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (raw == 0 || raw <= MUIImageBuiltinMax) return true;
		return ValidCString(ref platform, raw);
	}

	private static bool ValidEntryVector<TPlatform>(ref TPlatform platform,
		uint raw, bool requireEntry) where TPlatform : struct, IMuiGuestMemory
	{
		if (raw == 0) return !requireEntry;
		var entries = APTR.FromPointer(raw);
		for (var index = 0u; index < 4096; index++)
		{
			if (!MuiChoiceEntryVectorMemoryCodec.TryGetEntry(ref platform,
				entries, index,
				out var slot)) return false;
			if (!MuiChoiceEntryCodec.TryRead(ref platform, slot,
				out var entry)) return false;
			if (entry.Text.IsNull) return index != 0 || !requireEntry;
			if (!CStringCodec.TryReadLength(ref platform, entry.Text,
				MaximumCString + 1, out _)) return false;
		}
		return false;
	}

	private static uint ControlCharFromCString<TPlatform>(ref TPlatform platform,
		uint raw) where TPlatform : struct, IMuiGuestMemory
	{
		if (raw == 0) return 0;
		var text = APTR.FromPointer(raw);
		for (var index = 0u; index < MaximumCString; index++)
		{
			if (!platform.IsMapped(text, index + 1)) return 0;
			var ch = platform.ReadUInt8(text, unchecked((int)index));
			if (ch == 0) return 0;
			if (ch == (byte)'_')
			{
				if (!platform.IsMapped(text, index + 2)) return 0;
				var key = platform.ReadUInt8(text, unchecked((int)(index + 1)));
				if (key == 0) return 0;
				return key >= (byte)'A' && key <= (byte)'Z' ?
					unchecked((uint)(key + 32)) : key;
			}
		}
		return 0;
	}

	private static void ReleaseTemporary<TPlatform>(ref TPlatform platform,
		APTR className, APTR tags, APTR preParse)
		where TPlatform : struct, IMuiExecCapability
	{
		if (preParse.IsNotNull) platform.Free(preParse,
			MuiMakeObjectPreParseRecord.Size);
		if (tags.IsNotNull) platform.Free(tags, TagStorage);
		if (className.IsNotNull) platform.Free(className, ClassNameStorage);
	}
}
