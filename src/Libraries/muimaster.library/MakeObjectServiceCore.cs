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
internal struct MuiMakeObjectParameterRecord
{
	internal const uint Size = 16;

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
	private static bool TryResolve(MuiMakeObjectParameterField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiMakeObjectParameterField.First => 0,
			MuiMakeObjectParameterField.Second => 4,
			MuiMakeObjectParameterField.Third => 8,
			MuiMakeObjectParameterField.Fourth => 12,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMakeObjectParameterFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Base.IsNull ||
			cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR parameters, MuiMakeObjectParameterField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiMakeObjectParameterFieldCursor);
		cursor.Base = parameters;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
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
			!MuiMakeObjectParameterFieldCursorCodec.TryReadUInt32(ref platform,
				parameters, MuiMakeObjectParameterField.First, out record.First))
			return false;
		if (count > 1 &&
			!MuiMakeObjectParameterFieldCursorCodec.TryReadUInt32(ref platform,
				parameters, MuiMakeObjectParameterField.Second, out record.Second))
			return false;
		if (count > 2 &&
			!MuiMakeObjectParameterFieldCursorCodec.TryReadUInt32(ref platform,
				parameters, MuiMakeObjectParameterField.Third, out record.Third))
			return false;
		if (count > 3 &&
			!MuiMakeObjectParameterFieldCursorCodec.TryReadUInt32(ref platform,
				parameters, MuiMakeObjectParameterField.Fourth, out record.Fourth))
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
	private static bool TryResolve(MuiNewMenuField field, out uint offset,
		out uint size)
	{
		offset = field switch
		{
			MuiNewMenuField.Type => 0,
			MuiNewMenuField.Padding => 1,
			MuiNewMenuField.Label => 2,
			MuiNewMenuField.CommandKey => 6,
			MuiNewMenuField.Flags => 10,
			MuiNewMenuField.MutualExclude => 12,
			MuiNewMenuField.UserData => 16,
			_ => uint.MaxValue,
		};
		size = field == MuiNewMenuField.Type ||
			field == MuiNewMenuField.Padding ? 1u :
			field == MuiNewMenuField.Flags ? 2u : 4u;
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiNewMenuFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolve(cursor.Field, out var offset, out fieldSize) ||
			cursor.Record.IsNull || cursor.Record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, fieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiNewMenuField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiNewMenuFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address,
			out var fieldSize) || fieldSize != 4) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiNewMenuField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiNewMenuFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address,
			out var fieldSize) || fieldSize != 2) return false;
		value = platform.ReadUInt16(address, 0);
		return true;
	}

	internal static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiNewMenuField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiNewMenuFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address,
			out var fieldSize) || fieldSize != 1) return false;
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
		if (!MuiNewMenuFieldCursorCodec.TryReadUInt8(ref platform, address,
			MuiNewMenuField.Type, out record.Type) ||
			!MuiNewMenuFieldCursorCodec.TryReadUInt8(ref platform, address,
				MuiNewMenuField.Padding, out record.Padding) ||
			!MuiNewMenuFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiNewMenuField.Label, out record.Label) ||
			!MuiNewMenuFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiNewMenuField.CommandKey, out record.CommandKey) ||
			!MuiNewMenuFieldCursorCodec.TryReadUInt16(ref platform, address,
				MuiNewMenuField.Flags, out record.Flags) ||
			!MuiNewMenuFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiNewMenuField.MutualExclude, out record.MutualExclude) ||
			!MuiNewMenuFieldCursorCodec.TryReadUInt32(ref platform, address,
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

internal static class MuiNewMenuCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiNewMenuCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >=
			MuiNewMenuCursor.MaximumEntries || cursor.Index >
			(uint.MaxValue - cursor.Base.Raw) /
			MuiNewMenuCursor.EntrySize) return false;
		var offset = cursor.Index * MuiNewMenuCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address, MuiNewMenuCursor.EntrySize);
	}
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
	private const uint NewMenuEnd = 0;
	private const uint NewMenuTitle = 1;
	private const uint NewMenuItem = 2;
	private const uint NewMenuSub = 3;
	private const uint NewMenuIgnore = 64;
	private const uint NewMenuImage = 128;
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
	private const uint ClassNameStorage = 24;
	private const uint TagStorage = 88; // ten TagItems plus TAG_DONE
	private const uint PreParseStorage = 4;
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
				PreParseStorage);
			if (preParse.IsNull)
			{
				ReleaseTemporary(ref platform, className, tags, preParse);
				return APTR.Null;
			}
			WritePreParse(ref platform, preParse, preParseKind);
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
		if (!platform.IsMapped(address, ClassNameStorage)) return false;
		if (classKind == ClassText)
			return WriteTextName(ref platform, address);
		if (classKind == ClassRectangle)
			return WriteRectangleName(ref platform, address);
		if (classKind == ClassImage)
			return WriteImageName(ref platform, address);
		if (classKind == ClassCycle)
			return WriteCycleName(ref platform, address);
		if (classKind == ClassRadio)
			return WriteRadioName(ref platform, address);
		if (classKind == ClassSlider)
			return WriteSliderName(ref platform, address);
		if (classKind == ClassString)
			return WriteStringName(ref platform, address);
		if (classKind == ClassNumericbutton)
			return WriteNumericbuttonName(ref platform, address);
		if (classKind == ClassMenustrip)
			return WriteMenustripName(ref platform, address);
		if (classKind == ClassMenu)
			return WriteMenuName(ref platform, address);
		if (classKind == ClassMenuitem)
			return WriteMenuitemName(ref platform, address);
		return false;
	}

	private static bool WriteTextName<TPlatform>(ref TPlatform platform, APTR a)
		where TPlatform : struct, IMuiGuestMemory
	{
		Write(ref platform, a, 0, (byte)'T'); Write(ref platform, a, 1, (byte)'e');
		Write(ref platform, a, 2, (byte)'x'); Write(ref platform, a, 3, (byte)'t');
		Write(ref platform, a, 4, (byte)'.'); Write(ref platform, a, 5, (byte)'m');
		Write(ref platform, a, 6, (byte)'u'); Write(ref platform, a, 7, (byte)'i');
		Write(ref platform, a, 8, 0); return true;
	}

	private static bool WriteRectangleName<TPlatform>(ref TPlatform platform, APTR a)
		where TPlatform : struct, IMuiGuestMemory
	{
		Write(ref platform, a, 0, (byte)'R'); Write(ref platform, a, 1, (byte)'e');
		Write(ref platform, a, 2, (byte)'c'); Write(ref platform, a, 3, (byte)'t');
		Write(ref platform, a, 4, (byte)'a'); Write(ref platform, a, 5, (byte)'n');
		Write(ref platform, a, 6, (byte)'g'); Write(ref platform, a, 7, (byte)'l');
		Write(ref platform, a, 8, (byte)'e'); Write(ref platform, a, 9, (byte)'.');
		Write(ref platform, a, 10, (byte)'m'); Write(ref platform, a, 11, (byte)'u');
		Write(ref platform, a, 12, (byte)'i'); Write(ref platform, a, 13, 0);
		return true;
	}

	private static bool WriteImageName<TPlatform>(ref TPlatform platform, APTR a)
		where TPlatform : struct, IMuiGuestMemory
	{
		Write(ref platform, a, 0, (byte)'I'); Write(ref platform, a, 1, (byte)'m');
		Write(ref platform, a, 2, (byte)'a'); Write(ref platform, a, 3, (byte)'g');
		Write(ref platform, a, 4, (byte)'e'); Write(ref platform, a, 5, (byte)'.');
		Write(ref platform, a, 6, (byte)'m'); Write(ref platform, a, 7, (byte)'u');
		Write(ref platform, a, 8, (byte)'i'); Write(ref platform, a, 9, 0);
		return true;
	}

	private static bool WriteCycleName<TPlatform>(ref TPlatform platform, APTR a)
		where TPlatform : struct, IMuiGuestMemory
	{
		Write(ref platform, a, 0, (byte)'C'); Write(ref platform, a, 1, (byte)'y');
		Write(ref platform, a, 2, (byte)'c'); Write(ref platform, a, 3, (byte)'l');
		Write(ref platform, a, 4, (byte)'e'); Write(ref platform, a, 5, (byte)'.');
		Write(ref platform, a, 6, (byte)'m'); Write(ref platform, a, 7, (byte)'u');
		Write(ref platform, a, 8, (byte)'i'); Write(ref platform, a, 9, 0);
		return true;
	}

	private static bool WriteRadioName<TPlatform>(ref TPlatform platform, APTR a)
		where TPlatform : struct, IMuiGuestMemory
	{
		Write(ref platform, a, 0, (byte)'R'); Write(ref platform, a, 1, (byte)'a');
		Write(ref platform, a, 2, (byte)'d'); Write(ref platform, a, 3, (byte)'i');
		Write(ref platform, a, 4, (byte)'o'); Write(ref platform, a, 5, (byte)'.');
		Write(ref platform, a, 6, (byte)'m'); Write(ref platform, a, 7, (byte)'u');
		Write(ref platform, a, 8, (byte)'i'); Write(ref platform, a, 9, 0);
		return true;
	}

	private static bool WriteSliderName<TPlatform>(ref TPlatform platform, APTR a)
		where TPlatform : struct, IMuiGuestMemory
	{
		Write(ref platform, a, 0, (byte)'S'); Write(ref platform, a, 1, (byte)'l');
		Write(ref platform, a, 2, (byte)'i'); Write(ref platform, a, 3, (byte)'d');
		Write(ref platform, a, 4, (byte)'e'); Write(ref platform, a, 5, (byte)'r');
		Write(ref platform, a, 6, (byte)'.'); Write(ref platform, a, 7, (byte)'m');
		Write(ref platform, a, 8, (byte)'u'); Write(ref platform, a, 9, (byte)'i');
		Write(ref platform, a, 10, 0); return true;
	}

	private static bool WriteStringName<TPlatform>(ref TPlatform platform, APTR a)
		where TPlatform : struct, IMuiGuestMemory
	{
		Write(ref platform, a, 0, (byte)'S'); Write(ref platform, a, 1, (byte)'t');
		Write(ref platform, a, 2, (byte)'r'); Write(ref platform, a, 3, (byte)'i');
		Write(ref platform, a, 4, (byte)'n'); Write(ref platform, a, 5, (byte)'g');
		Write(ref platform, a, 6, (byte)'.'); Write(ref platform, a, 7, (byte)'m');
		Write(ref platform, a, 8, (byte)'u'); Write(ref platform, a, 9, (byte)'i');
		Write(ref platform, a, 10, 0); return true;
	}

	private static bool WriteNumericbuttonName<TPlatform>(ref TPlatform platform,
		APTR a) where TPlatform : struct, IMuiGuestMemory
	{
		Write(ref platform, a, 0, (byte)'N'); Write(ref platform, a, 1, (byte)'u');
		Write(ref platform, a, 2, (byte)'m'); Write(ref platform, a, 3, (byte)'e');
		Write(ref platform, a, 4, (byte)'r'); Write(ref platform, a, 5, (byte)'i');
		Write(ref platform, a, 6, (byte)'c'); Write(ref platform, a, 7, (byte)'b');
		Write(ref platform, a, 8, (byte)'u'); Write(ref platform, a, 9, (byte)'t');
		Write(ref platform, a, 10, (byte)'t'); Write(ref platform, a, 11, (byte)'o');
		Write(ref platform, a, 12, (byte)'n'); Write(ref platform, a, 13, (byte)'.');
		Write(ref platform, a, 14, (byte)'m'); Write(ref platform, a, 15, (byte)'u');
		Write(ref platform, a, 16, (byte)'i'); Write(ref platform, a, 17, 0);
		return true;
	}

	private static bool WriteMenustripName<TPlatform>(ref TPlatform platform,
		APTR a) where TPlatform : struct, IMuiGuestMemory
	{
		Write(ref platform, a, 0, (byte)'M'); Write(ref platform, a, 1, (byte)'e');
		Write(ref platform, a, 2, (byte)'n'); Write(ref platform, a, 3, (byte)'u');
		Write(ref platform, a, 4, (byte)'s'); Write(ref platform, a, 5, (byte)'t');
		Write(ref platform, a, 6, (byte)'r'); Write(ref platform, a, 7, (byte)'i');
		Write(ref platform, a, 8, (byte)'p'); Write(ref platform, a, 9, (byte)'.');
		Write(ref platform, a, 10, (byte)'m'); Write(ref platform, a, 11, (byte)'u');
		Write(ref platform, a, 12, (byte)'i'); Write(ref platform, a, 13, 0);
		return true;
	}

	private static bool WriteMenuName<TPlatform>(ref TPlatform platform, APTR a)
		where TPlatform : struct, IMuiGuestMemory
	{
		Write(ref platform, a, 0, (byte)'M'); Write(ref platform, a, 1, (byte)'e');
		Write(ref platform, a, 2, (byte)'n'); Write(ref platform, a, 3, (byte)'u');
		Write(ref platform, a, 4, (byte)'.'); Write(ref platform, a, 5, (byte)'m');
		Write(ref platform, a, 6, (byte)'u'); Write(ref platform, a, 7, (byte)'i');
		Write(ref platform, a, 8, 0); return true;
	}

	private static bool WriteMenuitemName<TPlatform>(ref TPlatform platform,
		APTR a) where TPlatform : struct, IMuiGuestMemory
	{
		Write(ref platform, a, 0, (byte)'M'); Write(ref platform, a, 1, (byte)'e');
		Write(ref platform, a, 2, (byte)'n'); Write(ref platform, a, 3, (byte)'u');
		Write(ref platform, a, 4, (byte)'i'); Write(ref platform, a, 5, (byte)'t');
		Write(ref platform, a, 6, (byte)'e'); Write(ref platform, a, 7, (byte)'m');
		Write(ref platform, a, 8, (byte)'.'); Write(ref platform, a, 9, (byte)'m');
		Write(ref platform, a, 10, (byte)'u'); Write(ref platform, a, 11, (byte)'i');
		Write(ref platform, a, 12, 0); return true;
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
		{
			AddTag(ref platform, tags, ref index, Frame, ButtonFrame);
			AddTag(ref platform, tags, ref index, Font, ButtonFont);
			AddTag(ref platform, tags, ref index, TextContents, p0);
			AddTag(ref platform, tags, ref index, TextPreParse, preParse.Raw);
			AddTag(ref platform, tags, ref index, InputMode, InputModeRelVerify);
			AddTag(ref platform, tags, ref index, Background, ButtonBackground);
		}
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

	private static void AddTag<TPlatform>(ref TPlatform platform, APTR tags,
		ref uint index, uint tag, uint value) where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAslTagItemCursor);
		cursor.Base = tags;
		cursor.Index = index;
		if (!MuiAslTagItemVectorCodec.TryGetEntry(ref platform, cursor,
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
		var menuCursor = default(MuiNewMenuCursor);
		menuCursor.Base = APTR.FromPointer(newMenuRaw);
		for (var index = 0u; index < MaximumMenuEntries; index++)
		{
			menuCursor.Index = index;
			if (!MuiNewMenuCursorCodec.TryGetEntry(ref platform, menuCursor,
				out var entry))
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
			var entryType = menuRecord.Type;
			var label = menuRecord.Label;
			var shortcut = menuRecord.CommandKey;
			var menuFlags = menuRecord.Flags;
			var mutualExclude = menuRecord.MutualExclude;
			var userData = menuRecord.UserData;
			if (entryType == NewMenuEnd) return strip;
			if ((entryType & NewMenuIgnore) != 0) continue;
			if (entryType == NewMenuTitle)
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
			var parent = entryType == NewMenuSub ? menuItem : menu;
		if (item.IsNull || parent.IsNull ||
			!MuiFamilyCore.AddTail(ref platform, state, parent, item))
		{
			DisposeMenuTree(ref platform, state, strip);
			return APTR.Null;
		}
		if (entryType == NewMenuItem) menuItem = item;
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
		var cursor = default(MuiNewMenuCursor);
		cursor.Base = newMenu;
		for (var index = 0u; index < MaximumMenuEntries; index++)
		{
			cursor.Index = index;
			if (!MuiNewMenuCursorCodec.TryGetEntry(ref platform, cursor,
				out var entry)) return 2;
			if (!MuiNewMenuRecordCodec.TryRead(ref platform, entry,
				out var menuRecord)) return 2;
			var entryType = menuRecord.Type;
			var label = menuRecord.Label;
			var shortcut = menuRecord.CommandKey;
			if (entryType == NewMenuEnd) return 0;
			if ((entryType & NewMenuIgnore) != 0) continue;
			if ((entryType & NewMenuImage) != 0) return 3;
			if (entryType == NewMenuTitle)
			{
				if (!ValidCString(ref platform, label)) return 4;
				haveMenu = true;
				haveItem = false;
				continue;
			}
			if (entryType != NewMenuItem && entryType != NewMenuSub) return 5;
			if (!haveMenu || entryType == NewMenuSub && !haveItem) return 6;
			if (!ResolveMenuItemStrings(ref platform, label, shortcut, flags,
				out _, out _)) return 7;
			if (entryType == NewMenuItem) haveItem = true;
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
			platform.IsMapped(labelAddress, 2) &&
			platform.ReadUInt8(labelAddress, 1) == 0)
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
		if (!MuiAslTagItemVectorCodec.TryGetEntry(ref platform, cursor,
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
		var cursor = default(MuiChoiceEntryCursor);
		cursor.Base = APTR.FromPointer(raw);
		for (var index = 0u; index < 4096; index++)
		{
			cursor.Index = index;
			if (!MuiChoiceEntryCursorCodec.TryGetEntry(ref platform, cursor,
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

	private static void WritePreParse<TPlatform>(ref TPlatform platform, APTR a,
		uint kind) where TPlatform : struct, IMuiGuestMemory
	{
		platform.WriteUInt8(a, 0, 0x1B);
		platform.WriteUInt8(a, 1, kind == 1 ? (byte)'c' : (byte)'l');
		platform.WriteUInt8(a, 2, 0);
	}

	private static void Write<TPlatform>(ref TPlatform platform, APTR a, int offset,
		byte value) where TPlatform : struct, IMuiGuestMemory =>
		platform.WriteUInt8(a, offset, value);

	private static void ReleaseTemporary<TPlatform>(ref TPlatform platform,
		APTR className, APTR tags, APTR preParse)
		where TPlatform : struct, IMuiExecCapability
	{
		if (preParse.IsNotNull) platform.Free(preParse, PreParseStorage);
		if (tags.IsNotNull) platform.Free(tags, TagStorage);
		if (className.IsNotNull) platform.Free(className, ClassNameStorage);
	}
}
