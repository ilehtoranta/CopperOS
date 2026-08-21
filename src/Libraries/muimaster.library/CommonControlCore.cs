/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Class identity for the active MG07 common controls. Determined from the
// registered class name rather than from any private MorphOS vector, so no
// MorphOS compatibility is advertised.
public enum MuiControlClass
{
	Unknown = 0,
	Text,
	Rectangle,
	Image,
	Bitmap,
	Bodychunk,
	Gauge,
	Levelmeter,
	Numeric,
	Slider,
	Knob,
	Numericbutton,
	String,
	Cycle,
	Radio,
	Prop,
	Scrollbar,
	Scale,
	Gadget,
}

// Parsed form of a MUIA_Image_Spec string ("kind:value"). Mirrors the autodoc
// grammar so a spec is interpreted rather than blindly drawn as a bitmap.
public enum MuiImageSpecKind
{
	Invalid = -1,
	BackgroundPattern = 0, // "0:x" builtin background pattern
	BuiltinImage = 1,      // "1:x" builtin image
	Color = 2,             // "2:rrggbb" or "2:8-per-channel" solid colour
	BoopsiImage = 3,       // "3:n" boopsi image class
	Brush = 4,             // "4:n" MUI brush
	Picture = 5,           // "5:n" datatypes picture file
	Preconfigured = 6,     // "6:x" preconfigured image / background
}

public struct MuiImageSpec
{
	public MuiImageSpecKind Kind;
	public uint Value; // decimal id for kinds 0,1,3,4,5,6; packed 0xRRGGBB for Color
	public uint Red;
	public uint Green;
	public uint Blue;
}

// Cycle and Radio entries are caller-owned NULL-terminated vectors of
// STRPTR values. Keep the pointer slot named so common-control behavior does
// not repeatedly decode an anonymous ULONG while counting or selecting.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiChoiceEntry
{
	internal const uint Size = 4;
	internal APTR Text;
}

internal enum MuiChoiceEntryField : byte
{
	Text,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiChoiceEntryFieldCursor
{
	internal APTR Record;
	internal MuiChoiceEntryField Field;
}

internal static class MuiChoiceEntryFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiChoiceEntryFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Field != MuiChoiceEntryField.Text || cursor.Record.IsNull ||
			!platform.IsMapped(cursor.Record, MuiChoiceEntry.Size)) return false;
		address = cursor.Record;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiChoiceEntryField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiChoiceEntryFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiChoiceEntryField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiChoiceEntryFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiChoiceEntryCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiChoiceEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiChoiceEntryFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiChoiceEntryField.Text, out var text)) return false;
		value.Text = APTR.FromPointer(text);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiChoiceEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiChoiceEntryFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiChoiceEntryField.Text, value.Text.Raw);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiChoiceEntryCursor
{
	internal const uint EntrySize = MuiChoiceEntry.Size;
	internal const uint MaximumEntries = 4096;
	internal APTR Base;
	internal uint Index;
}

internal static class MuiChoiceEntryCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiChoiceEntryCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >=
			MuiChoiceEntryCursor.MaximumEntries || cursor.Index >
			(uint.MaxValue - cursor.Base.Raw) /
			MuiChoiceEntryCursor.EntrySize) return false;
		var offset = cursor.Index * MuiChoiceEntryCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address, MuiChoiceEntryCursor.EntrySize);
	}
}

// The leading geometry of the caller-owned graphics.library Image record.
// ImageData and the remaining tail are intentionally outside this bounded
// seam; common controls only need the four leading geometry fields for
// AskMinMax. Keeping the WORD members named avoids scattering private Image
// offsets through the control implementation.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageGeometryState
{
	internal const uint Size = 8;
	internal short LeftEdge;
	internal short TopEdge;
	internal ushort Width;
	internal ushort Height;
}

internal enum MuiImageGeometryField : byte
{
	LeftEdge,
	TopEdge,
	Width,
	Height,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageGeometryFieldCursor
{
	internal APTR Record;
	internal MuiImageGeometryField Field;
}

internal static class MuiImageGeometryFieldCursorCodec
{
	private static bool TryResolve(MuiImageGeometryField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiImageGeometryField.LeftEdge => 0,
			MuiImageGeometryField.TopEdge => 2,
			MuiImageGeometryField.Width => 4,
			MuiImageGeometryField.Height => 6,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiImageGeometryFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 2);
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageGeometryField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiImageGeometryFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt16(address, 0);
		return true;
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageGeometryField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiImageGeometryFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt16(address, 0, value);
		return true;
	}
}

internal static class MuiImageGeometryCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiImageGeometryState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiImageGeometryState.Size)) return false;
		if (!MuiImageGeometryFieldCursorCodec.TryReadUInt16(ref platform, address,
			MuiImageGeometryField.LeftEdge, out var leftEdge) ||
			!MuiImageGeometryFieldCursorCodec.TryReadUInt16(ref platform, address,
				MuiImageGeometryField.TopEdge, out var topEdge) ||
			!MuiImageGeometryFieldCursorCodec.TryReadUInt16(ref platform, address,
				MuiImageGeometryField.Width, out value.Width) ||
			!MuiImageGeometryFieldCursorCodec.TryReadUInt16(ref platform, address,
				MuiImageGeometryField.Height, out value.Height)) return false;
		value.LeftEdge = unchecked((short)leftEdge);
		value.TopEdge = unchecked((short)topEdge);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiImageGeometryState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiImageGeometryState.Size)) return false;
		return MuiImageGeometryFieldCursorCodec.TryWriteUInt16(ref platform,
			address, MuiImageGeometryField.LeftEdge,
			unchecked((ushort)value.LeftEdge)) &&
			MuiImageGeometryFieldCursorCodec.TryWriteUInt16(ref platform, address,
				MuiImageGeometryField.TopEdge, unchecked((ushort)value.TopEdge)) &&
			MuiImageGeometryFieldCursorCodec.TryWriteUInt16(ref platform, address,
				MuiImageGeometryField.Width, value.Width) &&
			MuiImageGeometryFieldCursorCodec.TryWriteUInt16(ref platform, address,
				MuiImageGeometryField.Height, value.Height);
	}
}

// Caller-owned character sets used by MUIA_String_Accept and
// MUIA_String_Reject.  The pointers remain guest-resident [ISG] STRPTRs;
// this record names the two related values without relying on positional
// offsets in a private widget state block.
public struct MuiStringFilterState
{
	public APTR Accept;
	public APTR Reject;
}

// Scalar interaction flags for String.mui.  MorphOS stores these as BOOL
// attributes; keeping them together as a named value record avoids exposing
// positional fields in a private widget block.
public struct MuiStringInteractionState
{
	public uint Editable;
	public uint AdvanceOnCR;
	public uint Multiline;
}

// Initial String content policy. These four values are initializer-only in
// MorphOS; keeping them together makes lifetime and normalization explicit
// without introducing a private widget offset or managed text state.
public struct MuiStringPresentationState
{
	public uint MaxLen;
	public uint Secret;
	public uint Format;
	public uint Unicode;
}

// MorphOS String spell-checking policy.  The BOOL is kept as a named record so
// enabling the service never requires a private widget offset or a managed
// spellchecker object; actual dictionary/markup integration remains a platform
// capability boundary.
public struct MuiStringSpellCheckingState
{
	public uint Enabled;
}

// Getter-only MUIA_String_Acknowledge publication.  The pointer always names
// the current guest-owned contents buffer; keeping it in a named record makes
// the notification seam explicit without exposing a private String offset.
public struct MuiStringAcknowledgeState
{
	public APTR Contents;
}

// The two public String cursor positions form one logical editing state.  The
// attributes remain guest-visible MUI keys, but all validation and mutation use
// this named record rather than relying on a positional private widget layout.
public struct MuiStringCursorState
{
	public int BufferPos;
	public int DisplayPos;
}

// Caller-owned String.mui Listview relationship.  The pointer remains a guest
// MUI object; naming it as a state record keeps validation and key forwarding
// independent of any private object-field offsets.
public struct MuiStringAttachedListState
{
	public APTR Listview;
}

public static class MuiCommonControlCore
{
	// Shared object attributes.
	public const uint Disabled = 0x80423661;
	public const uint Selected = 0x8042654B;
	// Initialize-only selected-visual policy is projected through the named
	// gadget/image render records. MorphOS defaults this BOOL to TRUE.
	public const uint ShowSelState = 0x8042CAAC;
	// Gadget interaction values are projected through
	// MuiGadgetInteractionStateRecord.
	public const uint InputMode = 0x8042FB04;
	public const uint Pressed = 0x80423535;
	public const uint ShowMe = 0x80429BA8;
	public const uint Background = 0x8042545B;
	public const uint Frame = 0x8042AC64;
	public const uint CustomBackfill = 0x80420A63;
	public const uint FrameVisible = 0x80426498;
	public const uint FramePhantomHoriz = 0x8042ED76;
	// Area drag policy is projected through MuiAreaDragPolicyStateRecord.
	public const uint Draggable = 0x80420B6E;
	public const uint Dropable = 0x8042FBCE;
	// Common-control Font is projected through MuiControlFontStateRecord.
	public const uint Font = 0x8042BE50;
	// Area geometry is projected through MuiAreaGeometryStateRecord by the
	// common getter seam; signed coordinates remain ULONG-compatible on the bus.
	public const uint LeftEdge = 0x8042BEC6;
	public const uint TopEdge = 0x8042509B;
	public const uint Width = 0x8042B59C;
	public const uint Height = 0x80423237;
	public const uint RightEdge = 0x8042BA82;
	public const uint BottomEdge = 0x8042E552;
	// Area layout policy is projected through MuiAreaLayoutPolicyStateRecord.
	public const uint Weight = 0x80421D1F;
	public const uint HorizWeight = 0x80426DB9;
	public const uint VertWeight = 0x804298D0;
	public const uint FixWidth = 0x8042A3F1;
	public const uint FixHeight = 0x8042A92B;
	public const uint MaxWidth = 0x8042F112;
	public const uint MaxHeight = 0x804293E4;
	public const uint InnerLeft = 0x804228F8;
	public const uint InnerRight = 0x804297FF;
	public const uint InnerTop = 0x80421EB6;
	public const uint InnerBottom = 0x8042F2C0;
	// Area drawing policy is projected through MuiAreaRenderPolicyStateRecord.
	public const uint FillArea = 0x804294A3;
	// Area double-buffer policy is projected through
	// MuiAreaDoubleBufferStateRecord.
	public const uint DoubleBuffer = 0x8042A9C7;
	// Area short-help pointer is projected through
	// MuiAreaShortHelpStateRecord.
	public const uint ShortHelp = 0x80428FE3;
	private const uint RenderInfoAttr = 0x7FFF0001;

	// Numeric family (also Slider level shares MUIA_Numeric_Value).
	public const uint NumericDefault = 0x804263E8;
	// The owned format string is projected through MuiNumericFormatStateRecord.
	public const uint NumericFormat = 0x804263E9;
	public const uint NumericMax = 0x8042D78A;
	public const uint NumericMin = 0x8042E404;
	// Shared Numeric-family value getter. Keep the public identifier alongside
	// the named state record so callers do not need to repeat the ABI literal.
	public const uint NumericValue = 0x8042AE3A;
	public const uint NumericReverse = 0x8042F2A0;
	private const uint NumericRevUpDown = 0x804252DD;
	private const uint NumericRevLeftRight = 0x804294A7;
	private const uint NumericCheckAllSizes = 0x80421594;
	// Slider/Scale presentation values are projected through their named
	// presentation state records.
	public const uint SliderHoriz = 0x8042FAD1;
	public const uint SliderQuiet = 0x80420B26;
	public const uint ScaleHoriz = 0x8042919A;

	// Prop / Scrollbar.
	// Prop range values are projected through MuiPropRangeStateRecord.
	public const uint PropEntries = 0x8042FBDB;
	public const uint PropFirst = 0x8042D4B2;
	public const uint PropVisible = 0x8042FEA6;
	public const uint PropHoriz = 0x8042F4F3;
	public const uint PropDeltaFactor = 0x80427C5E;
	public const uint PropSlider = 0x80429C3A;
	public const uint PropUseWinBorder = 0x8042DEEE;
	// Scrollbar orientation/type are projected through
	// MuiScrollbarLayoutStateRecord.
	public const uint GroupHoriz = 0x8042536B;
	public const uint ScrollbarType = 0x8042FB6B;
	private const uint UserData = 0x80420313;
	private const uint ScrollbarTypeDefault = 0;
	private const uint ScrollbarTypeBottom = 1;
	private const uint ScrollbarTypeTop = 2;
	private const uint ScrollbarTypeSym = 3;
	private const uint ScrollbarTypeNone = 4;
	private const uint ScrollbarPartProp = 1;
	private const uint ScrollbarPartArrow = 2;

	// Gauge / Levelmeter.
	// Shared Gauge progress value exposed by the named gauge state record.
	public const uint GaugeCurrent = 0x8042F0DD;
	public const uint GaugeMax = 0x8042BCDB;
	public const uint GaugeHoriz = 0x804232DD;
	public const uint GaugeDivide = 0x8042D8DF;
	// Gauge progress text is projected through MuiGaugeInfoTextStateRecord.
	public const uint GaugeInfoText = 0x8042BF15;
	// Levelmeter label is projected through MuiLevelmeterLabelStateRecord.
	public const uint LevelmeterLabel = 0x80420DD5;

	// Choices.
	// Choice entries and active indices are projected through named guest
	// records.  The public constants keep the MorphOS attribute identity
	// available to host-side qualification without exposing storage offsets.
	public const uint CycleActive = 0x80421788;
	public const uint CycleEntries = 0x80420629;
	public const uint RadioActive = 0x80429B41;
	public const uint RadioEntries = 0x8042B6A1;

	// String.
	// String contents are projected through MuiStringContentsStateRecord.
	public const uint StringContents = 0x80428FFD;
	public const uint StringAttachedList = 0x80420FD2;
	public const uint StringScrollHeight = 0x8042BE8B;
	public const uint StringScrollLeft = 0x8042BD0D;
	public const uint StringScrollTop = 0x8042F4E5;
	public const uint StringScrollVisibleHeight = 0x8042791E;
	public const uint StringScrollVisibleWidth = 0x8042D280;
	public const uint StringScrollWidth = 0x80420FB5;
	// String presentation policy is projected through
	// MuiStringPresentationStateRecord.
	public const uint StringMaxLen = 0x80424984;
	public const uint StringSecret = 0x80428769;
	public const uint StringAcknowledge = 0x8042026C;
	public const uint StringAccept = 0x8042E3E1;
	// String cursor positions are projected through MuiStringCursorStateRecord.
	public const uint StringBufferPos = 0x80428B6C;
	public const uint StringDisplayPos = 0x8042CCBF;
	// String interaction flags are projected through
	// MuiStringInteractionStateRecord.
	public const uint StringEditable = 0x8042C94B;
	public const uint StringReject = 0x8042179C;
	// String integer is projected through MuiStringIntegerStateRecord.
	public const uint StringInteger = 0x80426E8A;
	public const uint StringInteger64 = 0x80424820;
	public const uint StringFormat = 0x80427484;
	// String placeholder is projected through MuiStringPlaceholderStateRecord.
	public const uint StringPlaceholder = 0x8042AE65;
	public const uint StringAdvanceOnCR = 0x804226DE;
	public const uint StringMultiline = 0x8042D18B;
	// String spell-checking policy is projected through
	// MuiStringSpellCheckingStateRecord.
	public const uint StringSpellChecking = 0x804266C6;
	public const uint StringEditHook = 0x80424C33;
	public const uint StringLonelyEditHook = 0x80421569;
	public const uint Unicode = 0x8042E7D0;
	private const uint StringFormatLeft = 0;
	private const uint StringFormatCenter = 1;
	private const uint StringFormatRight = 2;

	// Text.
	// Text contents are projected through MuiTextContentsStateRecord.
	public const uint TextContents = 0x8042F8DC;
	// Text scalar presentation is projected through MuiTextPresentationStateRecord.
	public const uint TextControlChar = 0x8042E6D0;
	private const uint TextCopy = 0x80427727;
	public const uint TextHiChar = 0x804218FF;
	public const uint TextMarking = 0x8042F780;
	// Text PreParse is projected through MuiTextPreParseStateRecord.
	public const uint TextPreParse = 0x8042566D;
	public const uint TextSetMin = 0x80424E10;
	public const uint TextSetMax = 0x80424D0A;
	public const uint TextSetVMax = 0x80420D8B;
	public const uint TextShorten = 0x80428BBD;
	// Renderer-produced status is projected through MuiTextShortenedStateRecord.
	public const uint TextShortened = 0x80425A86;
	// MUIV_Text_Shorten_* selectors (mui.h): how contents wider than the
	// allocated width are reconciled at draw time.
	private const uint TextShortenNothing = 0;
	private const uint TextShortenCutoff = 1;
	private const uint TextShortenHide = 2;
	// The MUI text engine escape introducer (0x1B == "\33" in the autodocs) and
	// the DrawInfo pen used when marking is active.
	private const byte TextEscape = 0x1B;
	private const uint TextMarkingPen = 6;

	// Persistence uses these private store keys as the stable owned-content
	// buffers already maintained by String/Text construction and mutation.
	// They remain guest-resident and are never exposed as public MUI keys.

	// Image / Bitmap / Bodychunk.
	// Image spec values are projected through MuiImageSpecStateRecord. The
	// record keeps Image_Spec and Image_BuiltinSpec presence distinct so a
	// supplied builtin zero is not confused with an absent string pointer.
	public const uint ImageSpec = 0x804233D5;
	public const uint ImageBuiltinSpec = 0x8042B907;
	// Image font-match pointers use a named record; scalar match flags remain
	// initializer-only and are handled by the existing raw attribute seam.
	public const uint ImageFontMatch = 0x8042815D;
	public const uint ImageFontMatchHeight = 0x80429F26;
	public const uint ImageFontMatchString = 0x804263C1;
	public const uint ImageFontMatchWidth = 0x804239BF;
	// Image font-match scalar policy is projected through
	// MuiImageFontMatchStateRecord; the optional string keeps its own record.
	// Image render/legacy pointers are projected through named guest records.
	public const uint ImageOldImage = 0x80424F3D;
	public const uint ImageState = 0x8042A3AD;
	public const uint ImageFreeHoriz = 0x8042DA84;
	public const uint ImageFreeVert = 0x8042EA28;
	private const uint MUIImageBuiltinMax = 0x00000093;
	// Rectangle bar flags and optional title are projected through named
	// guest-resident records for generic Get and OM_GET.
	public const uint RectangleBarTitle = 0x80426689;
	public const uint RectangleHBar = 0x8042C943;
	public const uint RectangleVBar = 0x80422204;
	public const uint BitmapBitmap = 0x804279BD;
	public const uint BitmapAlpha = 0x80423E71;
	public const uint BitmapWidth = 0x8042EB3A;
	public const uint BitmapHeight = 0x80421560;
	public const uint BitmapMappingTable = 0x8042E23D;
	public const uint BitmapPrecision = 0x80420C74;
	public const uint BitmapRemapped = 0x80423A47;
	public const uint BitmapSourceColors = 0x80425360;
	public const uint BitmapTransparent = 0x80422805;
	public const uint BitmapUseFriend = 0x804239D8;
	public const uint BodychunkBody = 0x8042CA67;
	public const uint BodychunkCompression = 0x8042DE5F;
	public const uint BodychunkDepth = 0x8042C392;
	public const uint BodychunkMasking = 0x80423B0E;
	public const uint GadgetGadget = 0x8042EC1A;

	// Guest-owned buffer keys, retired through the object store on dispose.
	private const uint StringCopyKey = 0x7F070001;
	private const uint StringInteger64Key = 0x7F07000F;
	private const uint TextCopyKey = 0x7F070002;
	private const uint StringifyKey = 0x7F070003;
	private const uint BodychunkDecodedKey = 0x7F070004;
	private const uint NumericFormatKey = 0x7F070007;
	private const uint GaugeInfoTextKey = 0x7F070008;
	private const uint GaugeInfoRenderKey = 0x7F070009;
	private const uint StringPlaceholderKey = 0x7F07000A;
	private const uint StringMaskKey = 0x7F07000B;
	private const uint TextPreParseKey = 0x7F07000C;
	private const uint TextRenderKey = 0x7F07000D;
		private const uint LevelmeterLabelKey = 0x7F07000E;
	private const uint StringCursorStateKey = 0x7F070010;
	private const uint StringPresentationStateKey = 0x7F070011;
	private const uint StringInteractionStateKey = 0x7F070012;
	private const uint StringSpellCheckingStateKey = 0x7F070013;
	private const uint StringAcknowledgeStateKey = 0x7F070014;
	private const uint StringAttachedListStateKey = 0x7F070015;
	private const uint StringEditHookStateKey = 0x7F070016;
	private const uint StringFilterStateKey = 0x7F070017;
	private const uint StringIntegerStateKey = 0x7F070018;
	private const uint StringPlaceholderStateKey = 0x7F070019;
	private const uint StringContentsStateKey = 0x7F07001A;
	private const uint ChoiceEntriesStateKey = 0x7F07001B;
	private const uint TextContentsStateKey = 0x7F07001C;
	private const uint TextPreParseStateKey = 0x7F07001D;
	private const uint NumericFormatStateKey = 0x7F07001E;
	private const uint GaugeInfoTextStateKey = 0x7F07001F;
	private const uint LevelmeterLabelStateKey = 0x7F070020;
	private const uint ImageOldImageStateKey = 0x7F070021;
	private const uint ImageSpecStateKey = 0x7F070022;
	private const uint BitmapSourceStateKey = 0x7F070023;
	private const uint RectangleBarTitleStateKey = 0x7F070024;
	private const uint ControlFontStateKey = 0x7F070025;
	private const uint ImageFontMatchStringStateKey = 0x7F070026;
	private const uint BodychunkFormatStateKey = 0x7F070027;
	private const uint BitmapGeometryStateKey = 0x7F070028;
	private const uint ImageRenderStateKey = 0x7F070029;
	private const uint NumericStateKey = 0x7F07002A;
	private const uint PropRangeStateKey = 0x7F07002B;
	private const uint GaugeStateKey = 0x7F07002C;
	private const uint ScrollbarLayoutStateKey = 0x7F07002D;
	private const uint SliderPresentationStateKey = 0x7F07002E;
	private const uint ScalePresentationStateKey = 0x7F07002F;
	private const uint GadgetInteractionStateKey = 0x7F070030;
	private const uint LevelmeterPresentationStateKey = 0x7F070031;
	private const uint TextPresentationStateKey = 0x7F070032;
	private const uint RectanglePresentationStateKey = 0x7F070033;
	private const uint AreaPresentationStateKey = 0x7F070034;
	private const uint TextShortenedStateKey = 0x7F070035;
	private const uint ChoiceActiveStateKey = 0x7F070036;
	private const uint ImageFontMatchStateKey = 0x7F070038;
	private const uint BitmapPolicyStateKey = 0x7F070039;
	private const uint BitmapRemappedStateKey = 0x7F07003A;
	private const uint GadgetGadgetStateKey = 0x7F07003B;
	private const uint PropPolicyStateKey = 0x7F07003C;
	private const uint AreaWeightStateKey = 0x7F07003D;

		// A small value record keeps Unicode input encoding explicit at the
		// guest-memory boundary. It is deliberately a struct so no managed byte
		// array or text object is needed on the native path.
		internal struct MuiUtf8Character
		{
			public uint CodePoint;
			public byte Length;
			public byte First;
			public byte Second;
			public byte Third;
			public byte Fourth;
		}

	// Resolved MUIA_Image_Spec instance data. Plain values (not buffers): a
	// spec string is parsed once into a kind and a value so the platform can
	// render it faithfully (obtain a pen for a colour, blit a builtin pattern)
	// instead of treating the STRPTR as a raw drawable.
	private const uint ImageResolvedKindKey = 0x7F070005;
	private const uint ImageResolvedValueKey = 0x7F070006;

	// Cycle gadget neutral geometry: the cycle image (arrow button) allowance and
	// the inner spacing between that image and the active entry text.
	private const int CycleImageWidth = 16;
	private const int CycleSpacing = 4;

	private const int FormatPlus = 1;
	private const int FormatZero = 2;
	private const int FormatUnsigned = 4;
	private const int FormatHexadecimal = 8;
	private const int FormatUppercase = 16;

	// Preprocessed keyboard codes used by MUIM_HandleEvent.
	private const int KeyPress = 0;
	private const int KeyToggle = 1;
	private const int KeyUp = 2;
	private const int KeyDown = 3;
	private const int KeyPageUp = 4;
	private const int KeyPageDown = 5;
	private const int KeyLeft = 8;
	private const int KeyRight = 9;
	private const int KeyHome = 12;
	private const int KeyEnd = 13;
	private const int KeyDelete = 28;
	private const int KeyBackspace = 29;
	private const uint InputModeNone = 0;
	private const uint InputModeRelVerify = 1;
	private const uint InputModeImmediate = 2;
	private const uint InputModeToggle = 3;

	// ---- Class determination -------------------------------------------------

	public static MuiControlClass Classify<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return MuiControlClass.Unknown;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return MuiControlClass.Unknown;
		var classRecord = objectValue.Class;
		return ClassifyRecord(ref platform, classRecord);
	}

	public static MuiControlClass ClassifyRecord<TPlatform>(ref TPlatform platform,
		APTR classRecord) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiHeadlessClassCodec.TryRead(ref platform, classRecord,
			out var classValue))
			return MuiControlClass.Unknown;
		return ClassifyName(ref platform, classValue.Name);
	}

	private static MuiControlClass ClassifyName<TPlatform>(ref TPlatform platform,
		APTR name) where TPlatform : struct, IMuiGuestMemory
	{
		if (name.IsNull) return MuiControlClass.Unknown;
		uint hash = 2166136261u;
		var length = 0;
		for (; length < 64; length++)
		{
			if (!platform.IsMapped(name, (uint)length + 1))
				return MuiControlClass.Unknown;
			var ch = platform.ReadUInt8(name, length);
			if (ch == 0) break;
			hash = (hash ^ Lower(ch)) * 16777619u;
		}
		if (length < 5 || length == 64 ||
			platform.ReadUInt8(name, length - 4) != (byte)'.' ||
			Lower(platform.ReadUInt8(name, length - 3)) != (byte)'m' ||
			Lower(platform.ReadUInt8(name, length - 2)) != (byte)'u' ||
			Lower(platform.ReadUInt8(name, length - 1)) != (byte)'i')
			return MuiControlClass.Unknown;
		switch (hash)
		{
			case 0x0CFBDA2Du: return MuiControlClass.Text;
			case 0xDB25DBDDu: return MuiControlClass.Rectangle;
			case 0x8A1D4D31u: return MuiControlClass.Image;
			case 0x476E77C5u: return MuiControlClass.Bitmap;
			case 0x9623D959u: return MuiControlClass.Bodychunk;
			case 0xB6B77F8Bu: return MuiControlClass.Gauge;
			case 0x45D88DB1u: return MuiControlClass.Levelmeter;
			case 0xCF8E2E93u: return MuiControlClass.Numeric;
			case 0xBB9AD9CFu: return MuiControlClass.Slider;
			case 0x76A8E1E2u: return MuiControlClass.Knob;
			case 0x487ECA6Fu: return MuiControlClass.Numericbutton;
			case 0x3489AAC3u: return MuiControlClass.String;
			case 0xF9CE5B52u: return MuiControlClass.Cycle;
			case 0x6D045437u: return MuiControlClass.Radio;
			case 0x7B38BC03u: return MuiControlClass.Prop;
			case 0x54826C2Au: return MuiControlClass.Scrollbar;
			case 0x86A77BD2u: return MuiControlClass.Scale;
			case 0x6BEFC3B6u: return MuiControlClass.Gadget;
		}
		return MuiControlClass.Unknown;
	}

	private static byte Lower(byte ch) =>
		ch >= (byte)'A' && ch <= (byte)'Z' ? unchecked((byte)(ch + 32)) : ch;

	private static bool IsNumericFamily(MuiControlClass cls) =>
		cls == MuiControlClass.Numeric || cls == MuiControlClass.Slider ||
		cls == MuiControlClass.Knob || cls == MuiControlClass.Numericbutton ||
		cls == MuiControlClass.Levelmeter;

	private static bool IsBitmapFamily(MuiControlClass cls) =>
		cls == MuiControlClass.Bitmap || cls == MuiControlClass.Bodychunk;

	internal static bool IsNumericClass(MuiControlClass cls) => IsNumericFamily(cls);

	internal static bool IsPropClass(MuiControlClass cls) =>
		cls == MuiControlClass.Prop || cls == MuiControlClass.Scrollbar;

	// ---- Construction normalization / defaults -------------------------------

	// Create a common control and automatically normalize its construction. This
	// is the common-control creation entry: it applies class-aware defaults and
	// content ownership without perturbing the shared generic object path used by
	// unrelated closures.
	public static APTR CreateControl<TPlatform>(ref TPlatform platform, APTR state,
		APTR classRecord, APTR tags) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, state,
			classRecord, tags);
		if (obj.IsNull) return APTR.Null;
		if (!Construct(ref platform, state, classRecord, obj))
		{
			MuiHeadlessObjectCore.DisposeObject(ref platform, state, obj);
			return APTR.Null;
		}
		return obj;
	}

	// Default MorphOS List inline editor.  MUIM_List_CreateEditObject is an
	// overload point for subclasses; the base List implementation creates a
	// String object whose contents starts as the selected entry.  Keep this
	// helper guest-resident and allocation-free on the host side: the object
	// store owns the copied contents and normal object disposal retires it.
	internal static APTR CreateInlineStringObject<TPlatform>(
		ref TPlatform platform, APTR state, APTR contents)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var classRecord = FindClassByControlClass(ref platform, state,
			MuiControlClass.String);
		if (classRecord.IsNull) return APTR.Null;
		var obj = CreateControl(ref platform, state, classRecord, APTR.Null);
		if (obj.IsNull) return APTR.Null;
		if (contents.IsNotNull && !SetPersistenceContents(ref platform, state,
			obj, StringContents, contents))
		{
			MuiHeadlessObjectCore.DisposeObject(ref platform, state, obj);
			return APTR.Null;
		}
		return obj;
	}

	internal static uint CreateInlineStringObjectRaw<TPlatform>(
		ref TPlatform platform, APTR state, APTR contents)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		CreateInlineStringObject(ref platform, state, contents).Raw;

	public static bool Construct<TPlatform>(ref TPlatform platform, APTR state,
		APTR classRecord, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var cls = ClassifyRecord(ref platform, classRecord);
		if (cls == MuiControlClass.Unknown) return true;
		var font = default(MuiControlFontState);
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, Font,
			out var rawFont))
		{
			font.Present = true;
			font.Font = APTR.FromPointer(rawFont);
		}
		if (!PublishControlFontState(ref platform, state, obj, font))
			return false;
		var areaPresentation = default(MuiAreaPresentationState);
		areaPresentation.Disabled = ReadRaw(ref platform, state, obj, Disabled, 0);
		areaPresentation.ShowMe = ReadRaw(ref platform, state, obj, ShowMe, 1);
		areaPresentation.Background = ReadRaw(ref platform, state, obj, Background, 0);
		areaPresentation.Frame = ReadRaw(ref platform, state, obj, Frame, 0);
		areaPresentation.CustomBackfill = ReadRaw(ref platform, state, obj,
			CustomBackfill, 0) == 0 ? 0u : 1u;
		if (!PublishAreaPresentationState(ref platform, state, obj,
			areaPresentation)) return false;
		if (!MuiAreaDragCore.TryReadPolicyState(ref platform, state, obj,
			out _)) return false;
		var doubleBuffer = default(MuiAreaDoubleBufferStateInput);
		doubleBuffer.Enabled = ReadRaw(ref platform, state, obj, DoubleBuffer, 0);
		if (!MuiAreaDoubleBufferCore.WriteState(ref platform, state, obj,
			doubleBuffer.Enabled, 1)) return false;
		var shortHelp = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			ShortHelp, 0));
		if (!MuiAreaShortHelpCore.WriteState(ref platform, state, obj, shortHelp,
			1)) return false;
		var areaWeight = default(MuiAreaWeightState);
		areaWeight.Weight = ReadRaw(ref platform, state, obj, Weight, 100);
		if (!PublishAreaWeightState(ref platform, state, obj, areaWeight))
			return false;
		if (IsNumericFamily(cls))
		{
			EnsureDefault(ref platform, state, obj, NumericDefault, 0);
			EnsureDefault(ref platform, state, obj, NumericFormat, 0);
			EnsureDefault(ref platform, state, obj, NumericReverse, 0);
			EnsureDefault(ref platform, state, obj, NumericRevUpDown, 0);
			EnsureDefault(ref platform, state, obj, NumericRevLeftRight, 0);
			EnsureDefault(ref platform, state, obj, NumericCheckAllSizes, 0);
			EnsureDefault(ref platform, state, obj, NumericMin, 0);
			EnsureDefault(ref platform, state, obj, NumericMax, 100);
			var numericState = default(MuiNumericState);
			numericState.Minimum = Read(ref platform, state, obj, NumericMin, 0);
			numericState.Maximum = Read(ref platform, state, obj, NumericMax, 100);
			numericState.Default = Read(ref platform, state, obj, NumericDefault, 0);
			numericState.Reverse = Read(ref platform, state, obj, NumericReverse, 0);
			numericState.Value = numericState.Minimum;
			if (MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
				NumericValue, out var rawNumericValue))
				numericState.Value = rawNumericValue;
			if (!PublishNumericState(ref platform, state, obj, numericState))
				return false;
			if (cls == MuiControlClass.Levelmeter)
			{
				EnsureDefault(ref platform, state, obj, GaugeHoriz, 1);
				var levelmeterPresentation = default(MuiLevelmeterPresentationState);
				levelmeterPresentation.Horizontal = Read(ref platform, state, obj,
					GaugeHoriz, 1);
				if (!PublishLevelmeterPresentationState(ref platform, state, obj,
					levelmeterPresentation)) return false;
			}
			if (cls == MuiControlClass.Slider)
			{
				if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
					SliderHoriz, out _))
				{
					if (MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
						GroupHoriz, out var groupHoriz))
						MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
							SliderHoriz, groupHoriz == 0 ? 0u : 1u, false);
					else
						EnsureDefault(ref platform, state, obj, SliderHoriz, 1);
				}
				EnsureDefault(ref platform, state, obj, SliderQuiet, 0);
				var sliderPresentation = default(MuiSliderPresentationState);
				sliderPresentation.Horizontal = Read(ref platform, state, obj,
					SliderHoriz, 1);
				sliderPresentation.Quiet = Read(ref platform, state, obj,
					SliderQuiet, 0);
				if (!PublishSliderPresentationState(ref platform, state, obj,
					sliderPresentation)) return false;
			}
			if (!CopyContents(ref platform, state, obj, NumericFormat,
				NumericFormatKey, 32, false)) return false;
			var numericFormat = default(MuiNumericFormatState);
			numericFormat.Format = APTR.FromPointer(Read(ref platform, state, obj,
				NumericFormat, 0));
			if (!PublishNumericFormatState(ref platform, state, obj,
				numericFormat)) return false;
			if (cls == MuiControlClass.Levelmeter && !CopyContents(ref platform,
				state, obj, LevelmeterLabel, LevelmeterLabelKey, 6, false))
				return false;
			if (cls == MuiControlClass.Levelmeter)
			{
				var label = default(MuiLevelmeterLabelState);
				label.Label = APTR.FromPointer(Read(ref platform, state, obj,
					LevelmeterLabel, 0));
				if (!PublishLevelmeterLabelState(ref platform, state, obj,
					label)) return false;
			}
			var minimum = Read(ref platform, state, obj, NumericMin, 0);
			EnsureDefault(ref platform, state, obj, NumericValue, minimum);
			ClampNumeric(ref platform, state, obj);
			return true;
		}
		if (cls == MuiControlClass.Gauge)
		{
			EnsureDefault(ref platform, state, obj, GaugeMax, 100);
			EnsureDefault(ref platform, state, obj, GaugeDivide, 0);
			EnsureDefault(ref platform, state, obj, GaugeCurrent, 0);
			var gaugeState = default(MuiGaugeState);
			gaugeState.Maximum = Read(ref platform, state, obj, GaugeMax, 100);
			gaugeState.Current = Read(ref platform, state, obj, GaugeCurrent, 0);
			gaugeState.Divide = Read(ref platform, state, obj, GaugeDivide, 0);
			gaugeState.Horizontal = Read(ref platform, state, obj, GaugeHoriz, 1);
			if (!PublishGaugeState(ref platform, state, obj, gaugeState))
				return false;
			// Gauge owns an optional InfoText format string ("%ld %%"-style) so a
			// caller buffer can be released without dangling the render source.
			if (cls == MuiControlClass.Gauge && !CopyContents(ref platform, state,
				obj, GaugeInfoText, GaugeInfoTextKey, 128, false)) return false;
			var infoText = default(MuiGaugeInfoTextState);
			infoText.InfoText = APTR.FromPointer(Read(ref platform, state, obj,
				GaugeInfoText, 0));
			if (!PublishGaugeInfoTextState(ref platform, state, obj, infoText))
				return false;
			// A Current supplied at construction is divided before further
			// processing, exactly as a later MUIA_Gauge_Current set would be.
			if (gaugeState.Divide != 0)
			{
				gaugeState.Current /= gaugeState.Divide;
				if (!PublishGaugeState(ref platform, state, obj, gaugeState))
					return false;
			}
			ClampGauge(ref platform, state, obj);
			return true;
		}
		if (cls == MuiControlClass.Prop)
		{
			EnsureDefault(ref platform, state, obj, PropEntries, 0);
			EnsureDefault(ref platform, state, obj, PropVisible, 0);
			EnsureDefault(ref platform, state, obj, PropFirst, 0);
			EnsureDefault(ref platform, state, obj, PropHoriz, 1);
			EnsureDefault(ref platform, state, obj, PropDeltaFactor, 1);
			EnsureDefault(ref platform, state, obj, PropSlider, 0);
			EnsureDefault(ref platform, state, obj, PropUseWinBorder, 0);
			var propPolicy = default(MuiPropPolicyState);
			propPolicy.Horizontal = ReadRaw(ref platform, state, obj, PropHoriz, 1);
			propPolicy.DeltaFactor = ReadRaw(ref platform, state, obj,
				PropDeltaFactor, 1);
			propPolicy.Slider = ReadRaw(ref platform, state, obj, PropSlider, 0);
			propPolicy.UseWinBorder = ReadRaw(ref platform, state, obj,
				PropUseWinBorder, 0);
			if (!PublishPropPolicyState(ref platform, state, obj, propPolicy))
				return false;
			var propRange = default(MuiPropRangeState);
			propRange.Entries = Read(ref platform, state, obj, PropEntries, 0);
			propRange.Visible = Read(ref platform, state, obj, PropVisible, 0);
			propRange.First = Read(ref platform, state, obj, PropFirst, 0);
			if (!PublishPropRangeState(ref platform, state, obj, propRange))
				return false;
			if (Read(ref platform, state, obj, PropUseWinBorder, 0) > 3)
				return false;
			ClampProp(ref platform, state, obj);
			return true;
		}
		if (cls == MuiControlClass.Scrollbar)
		{
			EnsureDefault(ref platform, state, obj, GroupHoriz, 0);
			EnsureDefault(ref platform, state, obj, ScrollbarType,
				ScrollbarTypeDefault);
			EnsureDefault(ref platform, state, obj, PropEntries, 0);
			EnsureDefault(ref platform, state, obj, PropVisible, 0);
			EnsureDefault(ref platform, state, obj, PropFirst, 0);
			EnsureDefault(ref platform, state, obj, PropDeltaFactor, 1);
			EnsureDefault(ref platform, state, obj, PropSlider, 0);
			EnsureDefault(ref platform, state, obj, PropUseWinBorder, 0);
			var scrollbarLayout = default(MuiScrollbarLayoutState);
			scrollbarLayout.Horizontal = Read(ref platform, state, obj, GroupHoriz, 0);
			scrollbarLayout.Type = Read(ref platform, state, obj, ScrollbarType,
				ScrollbarTypeDefault);
			if (!PublishScrollbarLayoutState(ref platform, state, obj,
				scrollbarLayout)) return false;
			var scrollbarPolicy = default(MuiPropPolicyState);
			scrollbarPolicy.Horizontal = ReadRaw(ref platform, state, obj,
				PropHoriz, scrollbarLayout.Horizontal);
			scrollbarPolicy.DeltaFactor = ReadRaw(ref platform, state, obj,
				PropDeltaFactor, 1);
			scrollbarPolicy.Slider = ReadRaw(ref platform, state, obj,
				PropSlider, 0);
			scrollbarPolicy.UseWinBorder = ReadRaw(ref platform, state, obj,
				PropUseWinBorder, 0);
			if (!PublishPropPolicyState(ref platform, state, obj, scrollbarPolicy))
				return false;
			var scrollbarRange = default(MuiPropRangeState);
			scrollbarRange.Entries = Read(ref platform, state, obj, PropEntries, 0);
			scrollbarRange.Visible = Read(ref platform, state, obj, PropVisible, 0);
			scrollbarRange.First = Read(ref platform, state, obj, PropFirst, 0);
			if (!PublishPropRangeState(ref platform, state, obj, scrollbarRange))
				return false;
			if (Read(ref platform, state, obj, PropUseWinBorder, 0) > 3)
				return false;
			EnsureDefault(ref platform, state, obj, PropHoriz,
				scrollbarLayout.Horizontal);
			if (scrollbarLayout.Type > ScrollbarTypeNone) return false;
			ClampProp(ref platform, state, obj);
			return BuildScrollbarChildren(ref platform, state, classRecord, obj);
		}
		if (cls == MuiControlClass.Cycle)
		{
			EnsureDefault(ref platform, state, obj, CycleActive, 0);
			var entries = default(MuiChoiceEntriesState);
			entries.Entries = APTR.FromPointer(Read(ref platform, state, obj,
				CycleEntries, 0));
			if (!PublishChoiceEntriesState(ref platform, state, obj,
				CycleEntries, entries)) return false;
			NormalizeChoiceActive(ref platform, state, obj, CycleActive,
				CycleEntries);
			return true;
		}
		if (cls == MuiControlClass.Radio)
		{
			EnsureDefault(ref platform, state, obj, RadioActive, 0);
			var entries = default(MuiChoiceEntriesState);
			entries.Entries = APTR.FromPointer(Read(ref platform, state, obj,
				RadioEntries, 0));
			if (!PublishChoiceEntriesState(ref platform, state, obj,
				RadioEntries, entries)) return false;
			if (!entries.Entries.IsNull && !BuildRadioChildren(ref platform, state,
				classRecord, obj, entries.Entries)) return false;
			NormalizeChoiceActive(ref platform, state, obj, RadioActive,
				RadioEntries);
			return true;
		}
		if (cls == MuiControlClass.Rectangle)
		{
			var rectanglePresentation = default(MuiRectanglePresentationState);
			rectanglePresentation.HorizontalBar = ReadRaw(ref platform, state, obj,
				RectangleHBar, 0);
			rectanglePresentation.VerticalBar = ReadRaw(ref platform, state, obj,
				RectangleVBar, 0);
			if (!PublishRectanglePresentationState(ref platform, state, obj,
				rectanglePresentation)) return false;
			var title = default(MuiRectangleBarTitleState);
			if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				RectangleBarTitle, out var rawTitle))
			{
				title.Present = true;
				title.Title = APTR.FromPointer(rawTitle);
			}
			if (!PublishRectangleBarTitleState(ref platform, state, obj, title))
				return false;
			return true;
		}
		if (cls == MuiControlClass.Image)
		{
			EnsureDefault(ref platform, state, obj, ShowSelState, 1);
			EnsureDefault(ref platform, state, obj, ImageState, 0);
			EnsureDefault(ref platform, state, obj, ImageFontMatch, 0);
			EnsureDefault(ref platform, state, obj, ImageFontMatchHeight, 0);
			EnsureDefault(ref platform, state, obj, ImageFontMatchWidth, 0);
			EnsureDefault(ref platform, state, obj, ImageFreeHoriz, 0);
			EnsureDefault(ref platform, state, obj, ImageFreeVert, 0);
			var imageFontMatch = default(MuiImageFontMatchState);
			imageFontMatch.Match = ReadRaw(ref platform, state, obj,
				ImageFontMatch, 0);
			imageFontMatch.Height = ReadRaw(ref platform, state, obj,
				ImageFontMatchHeight, 0);
			imageFontMatch.Width = ReadRaw(ref platform, state, obj,
				ImageFontMatchWidth, 0);
			if (!PublishImageFontMatchState(ref platform, state, obj,
				imageFontMatch)) return false;
			var imageRender = default(MuiImageRenderState);
			imageRender.ShowSelState = ReadRaw(ref platform, state, obj,
				ShowSelState, 1);
			imageRender.ImageState = ReadRaw(ref platform, state, obj, ImageState, 0);
			imageRender.Selected = ReadRaw(ref platform, state, obj, Selected, 0);
			imageRender.FreeHoriz = ReadRaw(ref platform, state, obj,
				ImageFreeHoriz, 0);
			imageRender.FreeVert = ReadRaw(ref platform, state, obj, ImageFreeVert, 0);
			if (!PublishImageRenderState(ref platform, state, obj, imageRender))
				return false;
			var oldImage = default(MuiImageOldImageState);
			oldImage.Image = APTR.FromPointer(Read(ref platform, state, obj,
				ImageOldImage, 0));
			if (!PublishImageOldImageState(ref platform, state, obj, oldImage))
				return false;
			var spec = default(MuiImageSpecState);
			if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				ImageSpec, out var rawSpec))
			{
				spec.Present = true;
				spec.Raw = rawSpec;
			}
			if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				ImageBuiltinSpec, out var builtinSpec))
			{
				spec.BuiltinPresent = true;
				spec.Builtin = builtinSpec;
			}
			if (!PublishImageSpecState(ref platform, state, obj, spec))
				return false;
			var fontMatchString = default(MuiImageFontMatchStringState);
			if (MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
				ImageFontMatchString, out var rawMatchString))
			{
				fontMatchString.Present = true;
				fontMatchString.MatchString = APTR.FromPointer(rawMatchString);
			}
			if (!PublishImageFontMatchStringState(ref platform, state, obj,
				fontMatchString, false)) return false;
			return true;
		}
		if (IsBitmapFamily(cls))
		{
			EnsureDefault(ref platform, state, obj, BitmapWidth, 0);
			EnsureDefault(ref platform, state, obj, BitmapHeight, 0);
			EnsureDefault(ref platform, state, obj, BitmapRemapped, 0);
			var remapped = default(MuiBitmapRemappedState);
			remapped.Remapped = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				BitmapRemapped, 0));
			if (!PublishBitmapRemappedState(ref platform, state, obj, remapped))
				return false;
			var bitmapGeometry = default(MuiBitmapGeometryState);
			bitmapGeometry.Width = ReadRaw(ref platform, state, obj, BitmapWidth, 0);
			bitmapGeometry.Height = ReadRaw(ref platform, state, obj, BitmapHeight, 0);
			if (!PublishBitmapGeometryState(ref platform, state, obj,
				bitmapGeometry)) return false;
			if (cls == MuiControlClass.Bitmap)
			{
				EnsureDefault(ref platform, state, obj, BitmapAlpha, 0);
				EnsureDefault(ref platform, state, obj, BitmapMappingTable, 0);
				EnsureDefault(ref platform, state, obj, BitmapPrecision, 0);
				EnsureDefault(ref platform, state, obj, BitmapSourceColors, 0);
				EnsureDefault(ref platform, state, obj, BitmapTransparent, 0);
				EnsureDefault(ref platform, state, obj, BitmapUseFriend, 0);
				var bitmapPolicy = default(MuiBitmapPolicyState);
				bitmapPolicy.Alpha = ReadRaw(ref platform, state, obj,
					BitmapAlpha, 0);
				bitmapPolicy.MappingTable = ReadRaw(ref platform, state, obj,
					BitmapMappingTable, 0);
				bitmapPolicy.Precision = ReadRaw(ref platform, state, obj,
					BitmapPrecision, 0);
				bitmapPolicy.SourceColors = ReadRaw(ref platform, state, obj,
					BitmapSourceColors, 0);
				bitmapPolicy.Transparent = ReadRaw(ref platform, state, obj,
					BitmapTransparent, 0);
				bitmapPolicy.UseFriend = ReadRaw(ref platform, state, obj,
					BitmapUseFriend, 0);
				if (!PublishBitmapPolicyState(ref platform, state, obj,
					bitmapPolicy)) return false;
			}
			else
			{
				EnsureDefault(ref platform, state, obj, BodychunkCompression, 0);
				EnsureDefault(ref platform, state, obj, BodychunkDepth, 1);
				EnsureDefault(ref platform, state, obj, BodychunkMasking, 0);
				var bodychunkFormat = default(MuiBodychunkFormatState);
				bodychunkFormat.Compression = ReadRaw(ref platform, state, obj,
					BodychunkCompression, 0);
				bodychunkFormat.Depth = ReadRaw(ref platform, state, obj,
					BodychunkDepth, 1);
				bodychunkFormat.Masking = ReadRaw(ref platform, state, obj,
					BodychunkMasking, 0);
				if (!PublishBodychunkFormatState(ref platform, state, obj,
					bodychunkFormat)) return false;
			}
			var bitmapSource = default(MuiBitmapSourceState);
			var sourceAttribute = cls == MuiControlClass.Bodychunk ?
				BodychunkBody : BitmapBitmap;
			bitmapSource.Source = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				sourceAttribute, 0));
			if (!PublishBitmapSourceState(ref platform, state, obj,
				sourceAttribute, bitmapSource)) return false;
			return true;
		}
		if (cls == MuiControlClass.Scale)
		{
			EnsureDefault(ref platform, state, obj, ScaleHoriz, 1);
			var scalePresentation = default(MuiScalePresentationState);
			scalePresentation.Horizontal = Read(ref platform, state, obj,
				ScaleHoriz, 1);
			return PublishScalePresentationState(ref platform, state, obj,
				scalePresentation);
		}
		if (cls == MuiControlClass.Gadget)
		{
			EnsureDefault(ref platform, state, obj, ShowSelState, 1);
			EnsureDefault(ref platform, state, obj, InputMode, InputModeNone);
			EnsureDefault(ref platform, state, obj, Selected, 0);
			EnsureDefault(ref platform, state, obj, Pressed, 0);
			var gadgetRelationship = default(MuiGadgetGadgetState);
			gadgetRelationship.Gadget = APTR.FromPointer(ReadRaw(ref platform,
				state, obj, GadgetGadget, 0));
			if (!PublishGadgetGadgetState(ref platform, state, obj,
				gadgetRelationship)) return false;
			var gadgetState = default(MuiGadgetInteractionState);
			gadgetState.ShowSelState = ReadRaw(ref platform, state, obj,
				ShowSelState, 1);
			gadgetState.InputMode = Read(ref platform, state, obj, InputMode,
				InputModeNone);
			gadgetState.Selected = Read(ref platform, state, obj, Selected, 0);
			gadgetState.Pressed = Read(ref platform, state, obj, Pressed, 0);
			return PublishGadgetInteractionState(ref platform, state, obj,
				gadgetState);
		}
		if (cls == MuiControlClass.String)
		{
			EnsureDefault(ref platform, state, obj, StringMaxLen, 80);
			EnsureDefault(ref platform, state, obj, StringAttachedList, 0);
			if (!EnsureStringAttachedListStateRecord(ref platform, state, obj))
				return false;
			EnsureDefault(ref platform, state, obj, StringEditable, 1);
			EnsureDefault(ref platform, state, obj, StringAdvanceOnCR, 0);
			EnsureDefault(ref platform, state, obj, StringMultiline, 0);
			if (!EnsureStringInteractionStateRecord(ref platform, state, obj))
				return false;
			EnsureDefault(ref platform, state, obj, StringSecret, 0);
			EnsureDefault(ref platform, state, obj, StringSpellChecking, 0);
			if (!EnsureStringSpellCheckingStateRecord(ref platform, state, obj))
				return false;
			EnsureDefault(ref platform, state, obj, StringAcknowledge, 0);
			if (!EnsureStringAcknowledgeStateRecord(ref platform, state, obj))
				return false;
			EnsureDefault(ref platform, state, obj, StringLonelyEditHook, 0);
			if (!EnsureStringEditHookStateRecord(ref platform, state, obj))
				return false;
			EnsureDefault(ref platform, state, obj, StringFormat, StringFormatLeft);
			EnsureDefault(ref platform, state, obj, Unicode, 0);
			if (!EnsureStringPresentationStateRecord(ref platform, state, obj))
				return false;
			if (!NormalizeStringPresentationState(ref platform, state, obj))
				return false;
			if (!NormalizeStringInteractionState(ref platform, state, obj))
				return false;
			if (!NormalizeStringSpellCheckingState(ref platform, state, obj))
				return false;
			if (!NormalizeStringAttachedListState(ref platform, state, obj))
				return false;
			if (!NormalizeStringInteger64State(ref platform, state, obj))
				return false;
			if (!NormalizeStringEditHookState(ref platform, state, obj))
				return false;
			if (!EnsureStringFilterStateRecord(ref platform, state, obj))
				return false;
			// Accept/Reject are caller-owned [ISG] character-set pointers.  Keep
			// the pointers as supplied, but reject malformed guest strings before
			// the object becomes visible to the application.
			if (!TryReadStringFilterState(ref platform, state, obj, out _))
				return false;
			if (!CopyContents(ref platform, state, obj, StringContents,
				StringCopyKey, StringMaxChars(ref platform, state, obj), false))
				return false;
			if (!EnsureStringIntegerStateRecord(ref platform, state, obj))
				return false;
			// A caller-owned placeholder STRPTR is copied so it can be shown when
			// the field is empty without depending on caller lifetime.
			if (!CopyContents(ref platform, state, obj, StringPlaceholder,
				StringPlaceholderKey, 128, false)) return false;
			if (!EnsureStringPlaceholderStateRecord(ref platform, state, obj))
				return false;
			// If an integer seed was supplied, materialise it into the contents
			// with a signed decimal conversion (the MUIA_String_Integer contract).
			if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				StringInteger, out var seed))
				SetStringInteger(ref platform, state, obj, seed, false);
			if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				StringInteger64, out var seed64) && seed64 != 0 &&
				!SetStringInteger64(ref platform, state, obj,
					APTR.FromPointer(seed64), false)) return false;
			if (!TryReadStringContentsState(ref platform, state, obj,
				out var contentsState)) return false;
			var contents = contentsState.Contents;
			EnsureDefault(ref platform, state, obj, StringBufferPos,
				unchecked((uint)StringCursorLength(ref platform, state, obj,
					contents)));
			EnsureDefault(ref platform, state, obj, StringDisplayPos, 0);
			if (!EnsureStringCursorStateRecord(ref platform, state, obj))
				return false;
			if (!NormalizeStringCursorState(ref platform, state, obj))
				return false;
			SyncStringInteger(ref platform, state, obj);
			SyncStringInteger64(ref platform, state, obj);
			return true;
		}
		if (cls == MuiControlClass.Text)
		{
			EnsureDefault(ref platform, state, obj, TextSetMin, 1);
			EnsureDefault(ref platform, state, obj, TextSetMax, 0);
			EnsureDefault(ref platform, state, obj, TextSetVMax, 1);
			EnsureDefault(ref platform, state, obj, TextControlChar, 0);
			EnsureDefault(ref platform, state, obj, TextMarking, 0);
			EnsureDefault(ref platform, state, obj, TextShorten, TextShortenNothing);
			if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, TextCopy,
				out _))
			{
				var defaultCopy = MuiHeadlessObjectCore.GetAttribute(ref platform, state,
					obj, TextHiChar, out _) ? 0u : 1u;
				MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, TextCopy,
					defaultCopy, false);
			}
			var textPresentation = default(MuiTextPresentationState);
			textPresentation.SetMin = ReadRaw(ref platform, state, obj, TextSetMin, 1);
			textPresentation.SetMax = ReadRaw(ref platform, state, obj, TextSetMax, 0);
			textPresentation.SetVMax = ReadRaw(ref platform, state, obj, TextSetVMax, 1);
			textPresentation.ControlChar = ReadRaw(ref platform, state, obj,
				TextControlChar, 0);
			textPresentation.Marking = ReadRaw(ref platform, state, obj, TextMarking, 0);
			textPresentation.Shorten = ReadRaw(ref platform, state, obj, TextShorten,
				TextShortenNothing);
			textPresentation.HiCharPresent = MuiHeadlessObjectCore.GetRawAttribute(
				ref platform, state, obj, TextHiChar, out var rawHiChar) ? 1u : 0u;
			textPresentation.HiChar = textPresentation.HiCharPresent == 0 ? 0u :
				rawHiChar;
			if (!PublishTextPresentationState(ref platform, state, obj,
				textPresentation)) return false;
			EnsureDefault(ref platform, state, obj, TextShortened, 0);
			if (!EnsureTextShortenedStateRecord(ref platform, state, obj) ||
				!TryReadTextShortenedState(ref platform, state, obj, out _))
				return false;
			// PreParse is a format prefix string; it is always copied into a private
			// buffer so the caller may release its format definition after use, in
			// line with the MUIA_Text_PreParse [ISG] STRPTR contract.
			if (!CopyContents(ref platform, state, obj, TextPreParse, TextPreParseKey,
				-1, false)) return false;
			var preParse = default(MuiTextPreParseState);
			preParse.PreParse = APTR.FromPointer(Read(ref platform, state, obj,
				TextPreParse, 0));
			if (!PublishTextPreParseState(ref platform, state, obj, preParse))
				return false;
			if (Read(ref platform, state, obj, TextCopy, 1) != 0)
			{
				if (!CopyContents(ref platform, state, obj, TextContents,
					TextCopyKey, -1, false)) return false;
			}
			var textContents = default(MuiTextContentsState);
			textContents.Contents = APTR.FromPointer(Read(ref platform, state,
				obj, TextContents, 0));
			return PublishTextContentsState(ref platform, state, obj,
				textContents);
		}
		return true;
	}

	private static bool BuildRadioChildren<TPlatform>(ref TPlatform platform,
		APTR state, APTR classRecord, APTR radio, APTR entries)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = CountEntries(ref platform, entries);
		if (count <= 0) return false;
		var textClass = FindClassByControlClass(ref platform, state,
			MuiControlClass.Text);
		// Older headless callers register only Radio.mui. Preserve that bounded
		// compatibility path; MakeObjectA roots register Text.mui and therefore
		// produce semantically correct Text children.
		if (textClass.IsNull) textClass = classRecord;
		var cursor = default(MuiChoiceEntryCursor);
		cursor.Base = entries;
		for (var index = 0; index < count; index++)
		{
			cursor.Index = unchecked((uint)index);
			if (!MuiChoiceEntryCursorCodec.TryGetEntry(ref platform, cursor,
				out var address)) return false;
			if (!MuiChoiceEntryCodec.TryRead(ref platform, address,
				out var entry)) return false;
			var label = entry.Text;
			var child = label.IsNull ? APTR.Null :
				MuiHeadlessObjectCore.CreateObjectA(ref platform, state, textClass,
					APTR.Null);
			if (child.IsNull || !MuiHeadlessObjectCore.SetAttribute(ref platform,
				state, child, TextContents, label.Raw, false) ||
				!Construct(ref platform, state, textClass, child) ||
				!MuiFamilyCore.AddTail(ref platform, state, radio, child))
			{
				if (child.IsNotNull && MuiHeadlessObjectCore.FindObject(ref platform,
					state, child).IsNotNull)
					MuiHeadlessObjectCore.DisposeObject(ref platform, state, child);
				var record = MuiHeadlessObjectCore.FindObject(ref platform, state, radio);
				if (record.IsNotNull)
					MuiFamilyCore.RemoveAllChildren(ref platform, state, record, true);
				return false;
			}
		}
		return true;
	}

	// Scrollbar.mui is a Group subclass.  The real class exposes one Prop and
	// two button children; keep that topology in the object family so group
	// layout, child ownership, and attribute forwarding all follow the same
	// paths as other MUI groups.  When the built-in Prop/Gadget class records
	// are available they are used.  A native bootstrap may register only the
	// common-control class under test, so a role-marked fallback child is also
	// supported without introducing managed class metadata.
	private static bool BuildScrollbarChildren<TPlatform>(ref TPlatform platform,
		APTR state, APTR classRecord, APTR scrollbar)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiFamilyCore.GetChild(ref platform, state, scrollbar, 0,
			APTR.Null).IsNotNull) return true;
		if (!TryReadScrollbarLayoutState(ref platform, state, scrollbar,
			out var layout)) return false;
		var horizontal = layout.Horizontal != 0;
		var type = layout.Type;
		var propClass = FindClassByControlClass(ref platform, state,
			MuiControlClass.Prop);
		var gadgetClass = FindClassByControlClass(ref platform, state,
			MuiControlClass.Gadget);
		var prop = CreateScrollbarPart(ref platform, state, classRecord,
			propClass, ScrollbarPartProp);
		var firstArrow = CreateScrollbarPart(ref platform, state, classRecord,
			gadgetClass, ScrollbarPartArrow);
		var secondArrow = CreateScrollbarPart(ref platform, state, classRecord,
			gadgetClass, ScrollbarPartArrow);
		if (prop.IsNull || firstArrow.IsNull || secondArrow.IsNull)
		{
			DisposeScrollbarPart(ref platform, state, prop);
			DisposeScrollbarPart(ref platform, state, firstArrow);
			DisposeScrollbarPart(ref platform, state, secondArrow);
			return false;
		}

		SetScrollbarPartGeometry(ref platform, state, prop, horizontal,
			false, type != ScrollbarTypeNone);
		SetScrollbarPartGeometry(ref platform, state, firstArrow, horizontal,
			true, type != ScrollbarTypeNone);
		SetScrollbarPartGeometry(ref platform, state, secondArrow, horizontal,
			true, type != ScrollbarTypeNone);
		if (!SyncScrollbarProp(ref platform, state, scrollbar, prop))
		{
			DisposeScrollbarPart(ref platform, state, prop);
			DisposeScrollbarPart(ref platform, state, firstArrow);
			DisposeScrollbarPart(ref platform, state, secondArrow);
			return false;
		}

		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, scrollbar);
		if (record.IsNull) return false;
		var added = false;
		if (type == ScrollbarTypeTop)
			added = MuiFamilyCore.AddTail(ref platform, state, scrollbar, firstArrow) &&
				MuiFamilyCore.AddTail(ref platform, state, scrollbar, secondArrow) &&
				MuiFamilyCore.AddTail(ref platform, state, scrollbar, prop);
		else if (type == ScrollbarTypeBottom)
			added = MuiFamilyCore.AddTail(ref platform, state, scrollbar, prop) &&
				MuiFamilyCore.AddTail(ref platform, state, scrollbar, firstArrow) &&
				MuiFamilyCore.AddTail(ref platform, state, scrollbar, secondArrow);
		else
			added = MuiFamilyCore.AddTail(ref platform, state, scrollbar, firstArrow) &&
				MuiFamilyCore.AddTail(ref platform, state, scrollbar, prop) &&
				MuiFamilyCore.AddTail(ref platform, state, scrollbar, secondArrow);
		if (added) return true;
		MuiFamilyCore.RemoveAllChildren(ref platform, state, record, true);
		DisposeScrollbarPart(ref platform, state, prop);
		DisposeScrollbarPart(ref platform, state, firstArrow);
		DisposeScrollbarPart(ref platform, state, secondArrow);
		return false;
	}

	private static APTR CreateScrollbarPart<TPlatform>(ref TPlatform platform,
		APTR state, APTR fallbackClass, APTR preferredClass, uint role)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var childClass = preferredClass.IsNotNull ? preferredClass : fallbackClass;
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, state,
			childClass, APTR.Null);
		if (child.IsNull) return APTR.Null;
		if (preferredClass.IsNotNull && !Construct(ref platform, state,
			preferredClass, child))
		{
			MuiHeadlessObjectCore.DisposeObject(ref platform, state, child);
			return APTR.Null;
		}
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, child,
			UserData, role, false))
		{
			MuiHeadlessObjectCore.DisposeObject(ref platform, state, child);
			return APTR.Null;
		}
		return child;
	}

	private static void DisposeScrollbarPart<TPlatform>(ref TPlatform platform,
		APTR state, APTR child) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (child.IsNotNull && MuiHeadlessObjectCore.FindObject(ref platform,
			state, child).IsNotNull)
			MuiHeadlessObjectCore.DisposeObject(ref platform, state, child);
	}

	private static void SetScrollbarPartGeometry<TPlatform>(ref TPlatform platform,
		APTR state, APTR child, bool horizontal, bool arrow, bool shown)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, child, ShowMe,
			shown ? 1u : 0u, false);
		if (arrow || !horizontal)
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, child, FixWidth,
				16, false);
		if (arrow || horizontal)
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, child, FixHeight,
				16, false);
		if (!arrow)
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, child, PropHoriz,
				horizontal ? 1u : 0u, false);
	}

	private static APTR FindClassByControlClass<TPlatform>(ref TPlatform platform,
		APTR state, MuiControlClass target)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return APTR.Null;
		var current = stateValue.Classes;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessClassCodec.TryRead(ref platform, current,
				out var classValue)) return APTR.Null;
			if (ClassifyRecord(ref platform, current) == target) return current;
			current = classValue.Next;
		}
		return APTR.Null;
	}

	private static APTR FindScrollbarPart<TPlatform>(ref TPlatform platform,
		APTR state, APTR scrollbar, uint role)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		for (var index = 0; index < 4; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, scrollbar,
				index, APTR.Null);
			if (child.IsNull) break;
			var childRecord = MuiHeadlessObjectCore.FindObject(ref platform, state,
				child);
			if (childRecord.IsNotNull && MuiHeadlessObjectCodec.TryRead(
				ref platform, childRecord, out var childValue) &&
				childValue.UserData == role) return child;
		}
		return APTR.Null;
	}

	private static APTR FindScrollbarArrow<TPlatform>(ref TPlatform platform,
		APTR state, APTR scrollbar, int ordinal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var found = 0;
		for (var index = 0; index < 4; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, scrollbar,
				index, APTR.Null);
			if (child.IsNull) break;
			var childRecord = MuiHeadlessObjectCore.FindObject(ref platform, state,
				child);
			var role = 0u;
			if (childRecord.IsNotNull && MuiHeadlessObjectCodec.TryRead(
				ref platform, childRecord, out var childValue))
				role = childValue.UserData;
			if (role == ScrollbarPartArrow && found++ == ordinal) return child;
		}
		return APTR.Null;
	}

	private static bool SyncScrollbarProp<TPlatform>(ref TPlatform platform,
		APTR state, APTR scrollbar, APTR prop = default)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var target = prop;
		if (target.IsNull) target = FindScrollbarPart(ref platform, state, scrollbar,
			ScrollbarPartProp);
		if (target.IsNull) return false;
		if (!TryReadScrollbarLayoutState(ref platform, state, scrollbar,
			out var layout)) return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, target,
			PropEntries, Read(ref platform, state, scrollbar, PropEntries, 0), false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, target,
			PropVisible, Read(ref platform, state, scrollbar, PropVisible, 0), false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, target,
			PropFirst, Read(ref platform, state, scrollbar, PropFirst, 0), false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, target,
			PropDeltaFactor, Read(ref platform, state, scrollbar,
				PropDeltaFactor, 1), false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, target,
			PropSlider, Read(ref platform, state, scrollbar, PropSlider, 0), false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, target,
			PropUseWinBorder, Read(ref platform, state, scrollbar,
				PropUseWinBorder, 0), false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, target,
			PropHoriz, layout.Horizontal, false);
	}

	private static void EnsureDefault<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out _))
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
				value, false);
	}

	internal static bool TryReadAreaPresentationState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiAreaPresentationState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		var disabled = ReadRaw(ref platform, state, obj, Disabled, 0);
		var showMe = ReadRaw(ref platform, state, obj, ShowMe, 1);
		var background = ReadRaw(ref platform, state, obj, Background, 0);
		var frame = ReadRaw(ref platform, state, obj, Frame, 0);
		var customBackfill = ReadRaw(ref platform, state, obj, CustomBackfill, 0) ==
			0 ? 0u : 1u;
		if (TryReadAreaPresentationStateRecord(ref platform, state, obj,
			out var record))
		{
			if (record.Disabled != disabled || record.ShowMe != showMe ||
				record.Background != background || record.Frame != frame ||
				record.CustomBackfill != customBackfill)
			{
				record.Disabled = disabled;
				record.ShowMe = showMe;
				record.Background = background;
				record.Frame = frame;
				record.CustomBackfill = customBackfill;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					AreaPresentationStateKey);
				if (!MuiAreaPresentationStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
		}
		result.Disabled = disabled;
		result.ShowMe = showMe;
		result.Background = background;
		result.Frame = frame;
		result.CustomBackfill = customBackfill;
		return true;
	}

	private static bool TryReadAreaPresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiAreaPresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			AreaPresentationStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			AreaPresentationStateKey) != unchecked((int)
			MuiAreaPresentationStateRecord.Size)) return false;
		return MuiAreaPresentationStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureAreaPresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadAreaPresentationStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaPresentationStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaPresentationStateRecord.Size);
		var value = default(MuiAreaPresentationStateRecord);
		value.Magic = MuiAreaPresentationStateRecord.Cookie;
		value.Disabled = ReadRaw(ref platform, state, obj, Disabled, 0);
		value.ShowMe = ReadRaw(ref platform, state, obj, ShowMe, 1);
		value.Background = ReadRaw(ref platform, state, obj, Background, 0);
		value.Frame = ReadRaw(ref platform, state, obj, Frame, 0);
		value.CustomBackfill = ReadRaw(ref platform, state, obj,
			CustomBackfill, 0) == 0 ? 0u : 1u;
		var written = MuiAreaPresentationStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			AreaPresentationStateKey, scratch,
			unchecked((int)MuiAreaPresentationStateRecord.Size));
		platform.Clear(scratch, MuiAreaPresentationStateRecord.Size);
		platform.Free(scratch, MuiAreaPresentationStateRecord.Size);
		return added;
	}

	private static bool PublishAreaPresentationState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiAreaPresentationState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureAreaPresentationStateRecord(ref platform, state, obj))
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			AreaPresentationStateKey);
		var stored = default(MuiAreaPresentationStateRecord);
		stored.Magic = MuiAreaPresentationStateRecord.Cookie;
		stored.Disabled = value.Disabled;
		stored.ShowMe = value.ShowMe;
		stored.Background = value.Background;
		stored.Frame = value.Frame;
		stored.CustomBackfill = value.CustomBackfill == 0 ? 0u : 1u;
		if (!MuiAreaPresentationStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			Disabled, stored.Disabled, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ShowMe, stored.ShowMe, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			Background, stored.Background, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			Frame, stored.Frame, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			CustomBackfill, stored.CustomBackfill, false);
	}

	internal static bool TryGetAreaPresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiAreaPresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadAreaPresentationStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadNumericState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiNumericState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		var minimum = ReadRaw(ref platform, state, obj, NumericMin, 0);
		var maximum = ReadRaw(ref platform, state, obj, NumericMax, 100);
		var value = ReadRaw(ref platform, state, obj, NumericValue, minimum);
		var numericDefault = ReadRaw(ref platform, state, obj, NumericDefault, 0);
		var reverse = ReadRaw(ref platform, state, obj, NumericReverse, 0);
		if (TryReadNumericStateRecord(ref platform, state, obj, out var record))
		{
			if (record.Minimum != minimum || record.Maximum != maximum ||
				record.Value != value || record.Default != numericDefault ||
				record.Reverse != reverse)
			{
				record.Minimum = minimum;
				record.Maximum = maximum;
				record.Value = value;
				record.Default = numericDefault;
				record.Reverse = reverse;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					NumericStateKey);
				if (!MuiNumericStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
		}
		result.Minimum = minimum;
		result.Maximum = maximum;
		result.Value = value;
		result.Default = numericDefault;
		result.Reverse = reverse;
		return true;
	}

	private static bool TryReadNumericStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiNumericStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			NumericStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			NumericStateKey) != unchecked((int)MuiNumericStateRecord.Size))
			return false;
		return MuiNumericStateRecordCodec.TryRead(ref platform, block, out value);
	}

	private static bool EnsureNumericStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadNumericStateRecord(ref platform, state, obj, out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiNumericStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiNumericStateRecord.Size);
		var value = default(MuiNumericStateRecord);
		value.Magic = MuiNumericStateRecord.Cookie;
		value.Minimum = Read(ref platform, state, obj, NumericMin, 0);
		value.Maximum = Read(ref platform, state, obj, NumericMax, 100);
		value.Value = Read(ref platform, state, obj, NumericValue, value.Minimum);
		value.Default = Read(ref platform, state, obj, NumericDefault, 0);
		value.Reverse = Read(ref platform, state, obj, NumericReverse, 0);
		var written = MuiNumericStateRecordCodec.Write(ref platform, scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			NumericStateKey, scratch, unchecked((int)MuiNumericStateRecord.Size));
		platform.Clear(scratch, MuiNumericStateRecord.Size);
		platform.Free(scratch, MuiNumericStateRecord.Size);
		return added;
	}

	private static bool PublishNumericState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, MuiNumericState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureNumericStateRecord(ref platform, state, obj)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			NumericStateKey);
		var stored = default(MuiNumericStateRecord);
		stored.Magic = MuiNumericStateRecord.Cookie;
		stored.Minimum = value.Minimum;
		stored.Maximum = value.Maximum;
		stored.Value = value.Value;
		stored.Default = value.Default;
		stored.Reverse = value.Reverse;
		if (!MuiNumericStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			NumericMin, stored.Minimum, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			NumericMax, stored.Maximum, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			NumericValue, stored.Value, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			NumericDefault, stored.Default, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			NumericReverse, stored.Reverse, false);
	}

	internal static bool TryGetNumericStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiNumericStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadNumericStateRecord(ref platform, state, obj, out value);

	// Generic Get/OM_GET projections for shared Numeric and Gauge values. The
	// state records are the consumer-facing shapes; raw storage is consulted
	// only to synchronize legacy SetAttribute writes without recursing through
	// the public getter path.
	internal static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		handled = false;
		if (MuiApplicationMessageCore.IsPublicGetterAttribute(attribute))
		{
			if (MuiApplicationMessageCore.TryGet(ref platform, state, obj,
				attribute, out value, out var applicationMessageHandled) &&
				applicationMessageHandled)
			{
				handled = true;
				return true;
			}
			if (applicationMessageHandled)
			{
				handled = true;
				return false;
			}
		}
		if (MuiApplicationWindowCore.IsPublicGetterAttribute(attribute))
		{
			if (MuiApplicationWindowCore.TryGet(ref platform, state, obj,
				attribute, out value, out var applicationWindowHandled) &&
				applicationWindowHandled)
			{
				handled = true;
				return true;
			}
			if (applicationWindowHandled)
			{
				handled = true;
				return false;
			}
		}
		if (MuiWindowPublicCore.IsPublicGetterAttribute(attribute))
		{
			if (MuiWindowPublicCore.TryGet(ref platform, state, obj,
				attribute, out value, out var windowPublicHandled) &&
				windowPublicHandled)
			{
				handled = true;
				return true;
			}
			if (windowPublicHandled)
			{
				handled = true;
				return false;
			}
		}
		if (MuiApplicationCommandsCore.IsPublicGetterAttribute(attribute))
		{
			if (MuiApplicationCommandsCore.TryGet(ref platform, state, obj,
				attribute, out value, out var commandsHandled) && commandsHandled)
			{
				handled = true;
				return true;
			}
			if (commandsHandled)
			{
				handled = true;
				return false;
			}
		}
		if (MuiApplicationWindowListCore.IsPublicGetterAttribute(attribute))
		{
			if (MuiApplicationWindowListCore.TryGet(ref platform, state, obj,
				attribute, out value, out var windowListHandled) && windowListHandled)
			{
				handled = true;
				return true;
			}
			if (windowListHandled)
			{
				handled = true;
				return false;
			}
		}
		if (MuiObjectMetadataCore.IsPublicGetterAttribute(attribute) &&
			MuiObjectMetadataCore.TryGet(ref platform, state, obj, attribute,
				out value))
		{
			handled = true;
			return true;
		}
		if (MuiGroupChildrenCore.IsPublicGetterAttribute(attribute))
		{
			if (MuiGroupChildrenCore.TryGet(ref platform, state, obj, attribute,
				out value, out var childrenHandled) && childrenHandled)
			{
				handled = true;
				return true;
			}
			if (childrenHandled)
			{
				handled = true;
				return false;
			}
		}
		if (MuiGroupChildrenCore.IsFamilyPublicGetterAttribute(attribute))
		{
			if (MuiGroupChildrenCore.TryGetFamily(ref platform, state, obj,
				attribute, out value, out var familyHandled) && familyHandled)
			{
				handled = true;
				return true;
			}
			if (familyHandled)
			{
				handled = true;
				return false;
			}
		}
		if (MuiGroupPageCore.IsPublicGetterAttribute(attribute) &&
			MuiGroupPageCore.TryGetAttribute(ref platform, state, obj, attribute,
				out value))
		{
			handled = true;
			return true;
		}
		if (MuiGroupGridCore.IsGridAttribute(attribute) &&
			MuiGroupGridCore.TryGetAttribute(ref platform, state, obj, attribute,
				out value))
		{
			handled = true;
			return true;
		}
		if (MuiGroupLayoutHookCore.IsPublicGetterAttribute(attribute) &&
			MuiGroupLayoutHookCore.TryGetAttribute(ref platform, state, obj,
				attribute, out value))
		{
			handled = true;
			return true;
		}
		if (MuiGroupLayoutCore.IsPublicGetterAttribute(attribute) &&
			MuiGroupLayoutCore.TryGetAttribute(ref platform, state, obj, attribute,
				out value))
		{
			handled = true;
			return true;
		}
		var cls = Classify(ref platform, state, obj);
		if ((attribute == Disabled || attribute == ShowMe ||
			attribute == Background || attribute == Frame ||
			attribute == CustomBackfill) &&
			cls != MuiControlClass.Unknown)
		{
			// Shared Area presentation values use one named guest record. Raw
			// storage is consulted only while bootstrapping/synchronizing it.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetAreaPresentationStateRecord(ref platform, state, obj,
					out _)) return false;
			handled = true;
			if (!TryReadAreaPresentationState(ref platform, state, obj,
				out var presentation)) return false;
			value = attribute == Disabled ? presentation.Disabled :
				attribute == ShowMe ? presentation.ShowMe :
				attribute == Background ? presentation.Background :
				attribute == Frame ? presentation.Frame :
				presentation.CustomBackfill;
			return true;
		}
		if ((attribute == LeftEdge || attribute == TopEdge ||
			attribute == Width || attribute == Height ||
			attribute == RightEdge || attribute == BottomEdge) &&
			cls != MuiControlClass.Unknown)
		{
			// Layout publishes the six signed Area coordinates as one named
			// guest record. Raw storage is only a bootstrap/synchronization seam.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!MuiAreaLayoutCore.TryGetGeometryStateRecord(ref platform, state,
					obj, out _)) return false;
			handled = true;
			if (!MuiAreaLayoutCore.TryReadGeometryState(ref platform, state, obj,
				out var geometry)) return false;
			var projected = attribute == LeftEdge ? geometry.Left :
				attribute == TopEdge ? geometry.Top :
				attribute == Width ? geometry.Width :
				attribute == Height ? geometry.Height :
				attribute == RightEdge ? geometry.Right : geometry.Bottom;
			value = unchecked((uint)projected);
			return true;
		}
		if (attribute == Weight && cls != MuiControlClass.Unknown)
		{
			// MUIA_Weight is the shared Area default input. Keep its public
			// projection in a dedicated named record instead of exposing the raw
			// scalar slot used by the legacy object path.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetAreaWeightStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadAreaWeightState(ref platform, state, obj,
				out var weight)) return false;
			value = weight.Weight;
			return true;
		}
		if ((attribute == HorizWeight ||
			attribute == VertWeight || attribute == FixWidth ||
			attribute == FixHeight || attribute == MaxWidth ||
			attribute == MaxHeight || attribute == InnerLeft ||
			attribute == InnerRight || attribute == InnerTop ||
			attribute == InnerBottom) && cls != MuiControlClass.Unknown)
		{
			// Min/max, fixed-size, inset, and weighting inputs share one named
			// layout-policy record. Raw storage is only the bootstrap seam; the
			// record is the generic Get/OM_GET projection.
			handled = true;
			if (!MuiAreaLayoutCore.TryReadLayoutPolicyState(ref platform, state, obj,
				out var policy)) return false;
			value = attribute == HorizWeight ? policy.HorizontalWeight :
				attribute == VertWeight ? policy.VerticalWeight :
				attribute == FixWidth ? policy.FixWidth :
				attribute == FixHeight ? policy.FixHeight :
				attribute == MaxWidth ? policy.MaxWidth :
				attribute == MaxHeight ? policy.MaxHeight :
				attribute == InnerLeft ? policy.InnerLeft :
				attribute == InnerRight ? policy.InnerRight :
				attribute == InnerTop ? policy.InnerTop : policy.InnerBottom;
			return true;
		}
		if (attribute == FillArea && cls != MuiControlClass.Unknown)
		{
			// FillArea is the remaining public Area render-policy input not covered
			// by the shared presentation/font records. The named render-policy
			// record is authoritative for Get and OM_GET; raw storage is used only
			// while synchronizing it.
			handled = true;
			if (!MuiAreaLayoutCore.TryReadRenderPolicyState(ref platform, state, obj,
				out var renderPolicy)) return false;
			value = renderPolicy.FillArea;
			return true;
		}
		if ((attribute == Draggable || attribute == Dropable) &&
			cls != MuiControlClass.Unknown)
		{
			handled = true;
			if (!MuiAreaDragCore.TryReadPolicyState(ref platform, state, obj,
				out var dragPolicy)) return false;
			value = attribute == Draggable ? dragPolicy.Draggable :
				dragPolicy.Dropable;
			return true;
		}
		if ((attribute == FrameVisible || attribute == FramePhantomHoriz) &&
			cls != MuiControlClass.Unknown)
		{
			handled = true;
			if (!MuiAreaLayoutCore.TryReadRenderPolicyState(ref platform, state, obj,
				out var framePolicy)) return false;
			value = attribute == FrameVisible ? framePolicy.FrameVisible :
				framePolicy.FramePhantomHoriz;
			return true;
		}
		if (attribute == DoubleBuffer && cls != MuiControlClass.Unknown)
		{
			// MUIA_DoubleBuffer is a BOOL Area policy. The named record is the
			// getter projection; raw storage is only synchronized through the
			// bounded record helper and never exposed as a private offset.
			handled = true;
			if (!MuiAreaDoubleBufferCore.TryReadState(ref platform, state, obj,
				out var doubleBuffer)) return false;
			value = doubleBuffer.Enabled;
			return true;
		}
		if (attribute == ShortHelp && cls != MuiControlClass.Unknown)
		{
			// ShortHelp is an opaque caller-owned OBString pointer. Its named
			// record is the public projection; no managed string is created.
			handled = true;
			if (!MuiAreaShortHelpCore.TryReadState(ref platform, state, obj,
				out var shortHelp)) return false;
			value = shortHelp.Text.Raw;
			return true;
		}
		if ((attribute == InputMode || attribute == Selected ||
			attribute == Pressed || attribute == ShowSelState) &&
			cls == MuiControlClass.Gadget)
		{
			// Gadget interaction values share one named record. Raw storage is only
			// the bootstrap/synchronization seam; the record is the public getter
			// projection for Get and OM_GET.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetGadgetInteractionStateRecord(ref platform, state, obj,
					out _)) return false;
			handled = true;
			if (!TryReadGadgetInteractionState(ref platform, state, obj,
				out var gadget)) return false;
			value = attribute == InputMode ? gadget.InputMode :
				attribute == Selected ? gadget.Selected :
				attribute == Pressed ? gadget.Pressed : gadget.ShowSelState;
			return true;
		}
		if (attribute == GadgetGadget && cls == MuiControlClass.Gadget)
		{
			// Gadget_Gadget is a getter-only caller-owned relationship. Raw storage
			// is only the compatibility bootstrap/synchronization seam; the named
			// record is the Get/OM_GET projection.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetGadgetGadgetStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadGadgetGadgetState(ref platform, state, obj,
				out var relationship)) return false;
			value = relationship.Gadget.Raw;
			return true;
		}
		if (((attribute == CycleEntries || attribute == CycleActive) &&
			cls == MuiControlClass.Cycle) ||
			((attribute == RadioEntries || attribute == RadioActive) &&
			cls == MuiControlClass.Radio))
		{
			// Choice vectors and active indices are separate named records. Raw
			// attributes are consulted only to bootstrap/synchronize those records;
			// the records are the Get/OM_GET projection.
			handled = true;
			if (attribute == CycleEntries || attribute == RadioEntries)
			{
				var entriesAttribute = cls == MuiControlClass.Radio ? RadioEntries :
					CycleEntries;
				if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
					entriesAttribute, out _) &&
					!TryGetChoiceEntriesStateRecord(ref platform, state, obj,
						out _)) return false;
				if (!TryReadChoiceEntriesState(ref platform, state, obj,
					entriesAttribute, out var entries)) return false;
				value = entries.Entries.Raw;
				return true;
			}
			var activeAttribute = cls == MuiControlClass.Radio ? RadioActive :
				CycleActive;
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				activeAttribute, out _) &&
				!TryGetChoiceActiveStateRecord(ref platform, state, obj,
					out _)) return false;
			if (!TryReadChoiceActiveState(ref platform, state, obj,
				activeAttribute, out var active)) return false;
			value = active.Active;
			return true;
		}
		if (cls == MuiControlClass.String &&
			MuiStringScrollAttributeCore.IsScrollAttribute(attribute))
		{
			// String.mui scroll metrics and pixel offsets share one named record.
			// Metric recomputation consults raw storage only, so generic Get and
			// OM_GET cannot recurse through the public getter seam.
			handled = true;
			if (!MuiStringScrollAttributeCore.TryReadMetricsState(ref platform,
				state, obj, out var metrics)) return false;
			value = attribute == MuiStringScrollAttributeCore.ScrollWidth ?
				metrics.Width : attribute == MuiStringScrollAttributeCore.ScrollHeight ?
				metrics.Height : attribute == MuiStringScrollAttributeCore.ScrollVisibleWidth ?
				metrics.VisibleWidth : attribute == MuiStringScrollAttributeCore.ScrollVisibleHeight ?
				metrics.VisibleHeight : attribute == MuiStringScrollAttributeCore.ScrollLeft ?
				metrics.Left : metrics.Top;
			return true;
		}
		if ((attribute == PropEntries || attribute == PropVisible ||
			attribute == PropFirst) && IsPropClass(cls))
		{
			// Prop and Scrollbar share the bounded range record. Raw storage is
			// only the bootstrap/synchronization seam for Get and OM_GET.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetPropRangeStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadPropRangeState(ref platform, state, obj,
				out var range)) return false;
			value = attribute == PropEntries ? range.Entries :
				attribute == PropVisible ? range.Visible : range.First;
			return true;
		}
		if ((attribute == GroupHoriz || attribute == ScrollbarType) &&
			cls == MuiControlClass.Scrollbar)
		{
			// Scrollbar orientation and frame type share a named layout record.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetScrollbarLayoutStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadScrollbarLayoutState(ref platform, state, obj,
				out var layout)) return false;
			value = attribute == GroupHoriz ? layout.Horizontal : layout.Type;
			return true;
		}
		if ((attribute == PropHoriz || attribute == PropDeltaFactor ||
			attribute == PropSlider || attribute == PropUseWinBorder) &&
			IsPropClass(cls))
		{
			// Prop/Scrollbar policy values share a named record; raw storage is
			// consulted only to bootstrap/reconcile persistence writes.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetPropPolicyStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadPropPolicyState(ref platform, state, obj,
				out var policy)) return false;
			value = attribute == PropHoriz ? policy.Horizontal :
				attribute == PropDeltaFactor ? policy.DeltaFactor :
				attribute == PropSlider ? policy.Slider : policy.UseWinBorder;
			return true;
		}
		if ((attribute == SliderHoriz || attribute == SliderQuiet) &&
			cls == MuiControlClass.Slider)
		{
			// Slider orientation and quiet-display policy share one named record.
			// Raw storage is only the bootstrap/synchronization seam.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetSliderPresentationStateRecord(ref platform, state, obj,
					out _)) return false;
			handled = true;
			if (!TryReadSliderPresentationState(ref platform, state, obj,
				out var slider)) return false;
			value = attribute == SliderHoriz ? slider.Horizontal : slider.Quiet;
			return true;
		}
		if (attribute == ScaleHoriz && cls == MuiControlClass.Scale)
		{
			// Scale orientation is a named presentation record rather than a
			// private scalar slot.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetScalePresentationStateRecord(ref platform, state, obj,
					out _)) return false;
			handled = true;
			if (!TryReadScalePresentationState(ref platform, state, obj,
				out var scale)) return false;
			value = scale.Horizontal;
			return true;
		}
		if (attribute == GaugeHoriz && cls == MuiControlClass.Levelmeter)
		{
			// Levelmeter uses the Gauge_Horiz public key but owns a distinct
			// presentation record from the Gauge progress state.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetLevelmeterPresentationStateRecord(ref platform, state, obj,
					out _)) return false;
			handled = true;
			if (!TryReadLevelmeterPresentationState(ref platform, state, obj,
				out var levelmeter)) return false;
			value = levelmeter.Horizontal;
			return true;
		}
		if ((attribute == NumericMin || attribute == NumericMax ||
			attribute == NumericValue || attribute == NumericDefault ||
			attribute == NumericReverse) && IsNumericClass(cls))
		{
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetNumericStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadNumericState(ref platform, state, obj, out var numeric))
				return false;
			value = attribute == NumericMin ? numeric.Minimum :
				attribute == NumericMax ? numeric.Maximum :
				attribute == NumericValue ? numeric.Value :
				attribute == NumericDefault ? numeric.Default : numeric.Reverse;
			return true;
		}
		if (attribute == NumericFormat && IsNumericClass(cls))
		{
			// NumericFormat is an owned guest C string.  Read the raw attribute
			// only for bootstrap/synchronization; the named record remains the
			// authoritative projection for generic Get and OM_GET callers.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetNumericFormatStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadNumericFormatState(ref platform, state, obj,
				out var format)) return false;
			value = format.Format.Raw;
			return true;
		}
		if (attribute == GaugeInfoText && cls == MuiControlClass.Gauge)
		{
			// InfoText is an owned guest C string. Consult raw storage only for
			// bootstrap/synchronization; the named record is the getter shape.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetGaugeInfoTextStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadGaugeInfoTextState(ref platform, state, obj,
				out var infoText)) return false;
			value = infoText.InfoText.Raw;
			return true;
		}
		if (attribute == LevelmeterLabel && cls == MuiControlClass.Levelmeter)
		{
			// Label is an owned guest C string. Consult raw storage only for
			// bootstrap/synchronization; the named record is the getter shape.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetLevelmeterLabelStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadLevelmeterLabelState(ref platform, state, obj,
				out var label)) return false;
			value = label.Label.Raw;
			return true;
		}
		if (attribute == TextContents && cls == MuiControlClass.Text)
		{
			// Contents may be caller-owned or an object-owned copy, as recorded
			// by MuiTextContentsStateRecord. Raw storage only bootstraps/syncs.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetTextContentsStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadTextContentsState(ref platform, state, obj,
				out var contents)) return false;
			value = contents.Contents.Raw;
			return true;
		}
		if (attribute == TextPreParse && cls == MuiControlClass.Text)
		{
			// PreParse is always an object-owned guest C string. Consult raw
			// storage only for bootstrap/synchronization; the named record is
			// the getter shape.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetTextPreParseStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadTextPreParseState(ref platform, state, obj,
				out var preParse)) return false;
			value = preParse.PreParse.Raw;
			return true;
		}
		if ((attribute == TextSetMin || attribute == TextSetMax ||
			attribute == TextSetVMax || attribute == TextControlChar ||
			attribute == TextMarking || attribute == TextShorten ||
			attribute == TextHiChar) && cls == MuiControlClass.Text)
		{
			// Text scalar presentation is one named state record. Raw storage is
			// consulted only to bootstrap/synchronize it; the record is the getter
			// projection for generic Get and OM_GET.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetTextPresentationStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadTextPresentationState(ref platform, state, obj,
				out var presentation)) return false;
			value = attribute == TextSetMin ? presentation.SetMin :
				attribute == TextSetMax ? presentation.SetMax :
				attribute == TextSetVMax ? presentation.SetVMax :
				attribute == TextControlChar ? presentation.ControlChar :
				attribute == TextMarking ? presentation.Marking :
				attribute == TextShorten ? presentation.Shorten :
				(presentation.HiCharPresent == 0 ? 0u : presentation.HiChar);
			return true;
		}
		if (attribute == TextShortened && cls == MuiControlClass.Text)
		{
			// Shortened is renderer-produced status. The named status record is the
			// getter projection, while raw storage remains only a compatibility seam.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetTextShortenedStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadTextShortenedState(ref platform, state, obj,
				out var shortened)) return false;
			value = shortened.Shortened;
			return true;
		}
		if ((attribute == ImageSpec || attribute == ImageBuiltinSpec) &&
			cls == MuiControlClass.Image)
		{
			// Image_Spec and Image_BuiltinSpec are one MorphOS union, but their
			// presence bits remain separate. Raw storage is consulted only to
			// bootstrap/synchronize the guest-resident named record.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetImageSpecStateRecord(ref platform, state, obj, out _))
				return false;
			if (!TryReadImageSpecState(ref platform, state, obj,
				out var imageSpec)) return false;
			var present = attribute == ImageSpec ? imageSpec.Present :
				imageSpec.BuiltinPresent;
			if (!present) return false;
			handled = true;
			value = attribute == ImageSpec ? imageSpec.Raw : imageSpec.Builtin;
			return true;
		}
		if ((attribute == ImageState || attribute == Selected ||
			attribute == ImageFreeHoriz || attribute == ImageFreeVert ||
			attribute == ShowSelState) &&
			cls == MuiControlClass.Image)
		{
			// Image selection and free-axis policy share one guest-resident record.
			// Raw storage is only the bootstrap/synchronization seam; the named
			// struct is the getter projection for both Get and OM_GET.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetImageRenderStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadImageRenderState(ref platform, state, obj,
				out var imageRender)) return false;
			value = attribute == ImageState ? imageRender.ImageState :
				attribute == Selected ? imageRender.Selected :
				attribute == ImageFreeHoriz ? imageRender.FreeHoriz :
				attribute == ImageFreeVert ? imageRender.FreeVert :
				imageRender.ShowSelState;
			return true;
		}
		if (attribute == ImageOldImage && cls == MuiControlClass.Image)
		{
			// OldImage is a caller-owned graphics.library Image pointer. Its named
			// record preserves the pointer without copying or managed wrappers.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetImageOldImageStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadImageOldImageState(ref platform, state, obj,
				out var oldImage)) return false;
			value = oldImage.Image.Raw;
			return true;
		}
		if (attribute == BitmapRemapped && IsBitmapFamily(cls))
		{
			// Remapped is renderer-produced state shared by Bitmap and Bodychunk.
			// Raw storage is only the compatibility bootstrap/synchronization seam;
			// the named guest record is the Get/OM_GET projection.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetBitmapRemappedStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadBitmapRemappedState(ref platform, state, obj,
				out var remapped)) return false;
			value = remapped.Remapped.Raw;
			return true;
		}
		if ((attribute == BitmapWidth || attribute == BitmapHeight) &&
			IsBitmapFamily(cls))
		{
			// Bitmap and Bodychunk share one named geometry record. The raw
			// attribute is consulted only when bootstrapping or synchronizing it.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetBitmapGeometryStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadBitmapGeometryState(ref platform, state, obj,
				out var geometry)) return false;
			value = attribute == BitmapWidth ? geometry.Width : geometry.Height;
			return true;
		}
		if ((attribute == BitmapBitmap && cls == MuiControlClass.Bitmap) ||
			(attribute == BodychunkBody && cls == MuiControlClass.Bodychunk))
		{
			// Source pointers remain caller-owned and are exposed through the
			// class-specific named source record without managed wrappers.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetBitmapSourceStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadBitmapSourceState(ref platform, state, obj, cls,
				out var source)) return false;
			value = source.Source.Raw;
			return true;
		}
		if ((attribute == BitmapAlpha || attribute == BitmapMappingTable ||
			attribute == BitmapPrecision || attribute == BitmapSourceColors ||
			attribute == BitmapTransparent || attribute == BitmapUseFriend) &&
			cls == MuiControlClass.Bitmap)
		{
			// Bitmap-only policy/source values share a named record. Raw storage is
			// the bootstrap/persistence seam; the record is the Get/OM_GET shape.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetBitmapPolicyStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadBitmapPolicyState(ref platform, state, obj,
				out var policy)) return false;
			value = attribute == BitmapAlpha ? policy.Alpha :
				attribute == BitmapMappingTable ? policy.MappingTable :
				attribute == BitmapPrecision ? policy.Precision :
				attribute == BitmapSourceColors ? policy.SourceColors :
				attribute == BitmapTransparent ? policy.Transparent :
				policy.UseFriend;
			return true;
		}
		if ((attribute == BodychunkCompression || attribute == BodychunkDepth ||
			attribute == BodychunkMasking) && cls == MuiControlClass.Bodychunk)
		{
			// Body decoding format is one guest-resident named state record;
			// malformed class-specific tags are not widened into Bitmap behavior.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetBodychunkFormatStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadBodychunkFormatState(ref platform, state, obj,
				out var format)) return false;
			value = attribute == BodychunkCompression ? format.Compression :
				attribute == BodychunkDepth ? format.Depth : format.Masking;
			return true;
		}
		if ((attribute == RectangleHBar || attribute == RectangleVBar) &&
			cls == MuiControlClass.Rectangle)
		{
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetRectanglePresentationStateRecord(ref platform, state, obj,
					out _)) return false;
			handled = true;
			if (!TryReadRectanglePresentationState(ref platform, state, obj,
				out var presentation)) return false;
			value = attribute == RectangleHBar ? presentation.HorizontalBar :
				presentation.VerticalBar;
			return true;
		}
		if (attribute == RectangleBarTitle && cls == MuiControlClass.Rectangle)
		{
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetRectangleBarTitleStateRecord(ref platform, state, obj,
					out _)) return false;
			handled = true;
			if (!TryReadRectangleBarTitleState(ref platform, state, obj,
				out var title)) return false;
			value = title.Present ? title.Title.Raw : 0;
			return true;
		}
		if (attribute == Font && cls != MuiControlClass.Unknown)
		{
			// Font is an optional caller-owned TextFont pointer shared by all
			// common controls. Presence and pointer value stay in the named record.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetControlFontStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadControlFontState(ref platform, state, obj,
				out var font)) return false;
			value = font.Present ? font.Font.Raw : 0;
			return true;
		}
		if ((attribute == ImageFontMatch || attribute == ImageFontMatchHeight ||
			attribute == ImageFontMatchWidth) && cls == MuiControlClass.Image)
		{
			// Image FontMatch scalar policy is initializer-only. Raw storage is
			// consulted only to bootstrap/reconcile the named record used by Get
			// and OM_GET; SetControlAttribute continues to reject these [I..]
			// attributes at runtime.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetImageFontMatchStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadImageFontMatchState(ref platform, state, obj,
				out var match)) return false;
			value = attribute == ImageFontMatch ? match.Match :
				attribute == ImageFontMatchHeight ? match.Height : match.Width;
			return true;
		}
		if (attribute == ImageFontMatchString && cls == MuiControlClass.Image)
		{
			// MatchString is a bounded caller-owned C string. The named record
			// validates and preserves its pointer for Get and OM_GET.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetImageFontMatchStringStateRecord(ref platform, state, obj,
					out _)) return false;
			handled = true;
			if (!TryReadImageFontMatchStringState(ref platform, state, obj,
				out var matchString)) return false;
			value = matchString.Present ? matchString.MatchString.Raw : 0;
			return true;
		}
		if (attribute == StringContents && cls == MuiControlClass.String)
		{
			// Contents are backed by the String-owned copy. Consult raw storage
			// only for bootstrap/synchronization; the named record is the getter
			// shape for generic Get and OM_GET.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetStringContentsStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadStringContentsState(ref platform, state, obj,
				out var contents)) return false;
			value = contents.Contents.Raw;
			return true;
		}
		if (attribute == StringPlaceholder && cls == MuiControlClass.String)
		{
			// Placeholder is an object-owned bounded guest C string. Consult raw
			// storage only for bootstrap/synchronization; the named record is the
			// getter shape for generic Get and OM_GET.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetStringPlaceholderStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadStringPlaceholderState(ref platform, state, obj,
				out var placeholder)) return false;
			value = placeholder.Contents.Raw;
			return true;
		}
		if (attribute == StringAcknowledge && cls == MuiControlClass.String)
		{
			// Acknowledge is getter-only and names the current contents buffer.
			// Raw storage is consulted only for bootstrap/synchronization; the
			// guest-resident record remains the public projection.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetStringAcknowledgeStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadStringAcknowledgeState(ref platform, state, obj,
				out var acknowledge)) return false;
			value = acknowledge.Contents.Raw;
			return true;
		}
		if (attribute == StringAttachedList && cls == MuiControlClass.String)
		{
			// AttachedList is a caller-owned live Listview relationship. Raw
			// storage only bootstraps/synchronizes; the named record performs the
			// relationship validation and supplies the getter value.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetStringAttachedListStateRecord(ref platform, state, obj, out _))
				return false;
			if (!TryReadStringAttachedListState(ref platform, state, obj,
				out var attached))
			{
				// Preserve malformed construction tags for the generic raw path so
				// EnsureDefault cannot replace an invalid caller pointer with zero.
				handled = false;
				return false;
			}
			handled = true;
			value = attached.Listview.Raw;
			return true;
		}
		if (attribute == StringInteger && cls == MuiControlClass.String)
		{
			// The signed semantic value is stored in the guest record. Raw storage
			// is consulted only to bootstrap an object that predates the record.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetStringIntegerStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadStringIntegerState(ref platform, state, obj,
				out var integer)) return false;
			value = unchecked((uint)integer.Value);
			return true;
		}
		if (attribute == StringInteger64 && cls == MuiControlClass.String)
		{
			// Integer64 is a caller-facing pointer to a guest QUAD. The object owns
			// a validated copy and exposes that pointer through the named semantic
			// state struct; no managed 64-bit value or private offset is involved.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryReadStringInteger64State(ref platform, state, obj,
					out _, out _))
				return false;
			handled = true;
			if (!TryReadStringInteger64State(ref platform, state, obj,
				out var integer64State, out _)) return false;
			value = integer64State.Value.Raw;
			return true;
		}
		if (attribute == StringSpellChecking && cls == MuiControlClass.String)
		{
			// Canonicalize the MorphOS BOOL through the guest record. Raw storage is
			// consulted only while bootstrapping the named state.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetStringSpellCheckingStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadStringSpellCheckingState(ref platform, state, obj,
				out var spellChecking)) return false;
			value = spellChecking.Enabled == 0 ? 0u : 1u;
			return true;
		}
		if ((attribute == StringEditHook || attribute == StringLonelyEditHook) &&
			cls == MuiControlClass.String)
		{
			// Keep the caller-owned Hook and its BOOL policy in one guest record;
			// raw storage is only the bootstrap source for that record.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetStringEditHookStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadStringEditHookState(ref platform, state, obj,
				out var editHook)) return false;
			value = attribute == StringEditHook ? editHook.EditHook.Raw :
				(editHook.LonelyEditHook == 0 ? 0u : 1u);
			return true;
		}
		if ((attribute == StringAccept || attribute == StringReject) &&
			cls == MuiControlClass.String)
		{
			// Accept/Reject remain caller-owned C strings; the named record supplies
			// the validated pointer pair for the public getter surface.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetStringFilterStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadStringFilterState(ref platform, state, obj,
				out var filters)) return false;
			value = attribute == StringAccept ? filters.Accept.Raw :
				filters.Reject.Raw;
			return true;
		}
		if ((attribute == StringBufferPos || attribute == StringDisplayPos) &&
			cls == MuiControlClass.String)
		{
			// BufferPos and DisplayPos are one logical editing state. Raw storage is
			// consulted only to bootstrap/synchronize the named record; the record is
			// the getter projection used by generic Get and OM_GET.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetStringCursorStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadStringCursorState(ref platform, state, obj,
				out var cursor)) return false;
			value = attribute == StringBufferPos ?
				unchecked((uint)cursor.BufferPos) :
				unchecked((uint)cursor.DisplayPos);
			return true;
		}
		if ((attribute == StringEditable || attribute == StringAdvanceOnCR ||
			attribute == StringMultiline) && cls == MuiControlClass.String)
		{
			// These BOOLs form one interaction state. Raw storage is consulted only
			// to bootstrap/synchronize the named record; the record is the getter
			// projection used by generic Get and OM_GET.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetStringInteractionStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadStringInteractionState(ref platform, state, obj,
				out var interaction)) return false;
			value = attribute == StringEditable ? interaction.Editable :
				attribute == StringAdvanceOnCR ? interaction.AdvanceOnCR :
				interaction.Multiline;
			return true;
		}
		if ((attribute == StringMaxLen || attribute == StringSecret ||
			attribute == StringFormat || attribute == Unicode) &&
			cls == MuiControlClass.String)
		{
			// MaxLen, Secret, Format, and Unicode form one initializer-only
			// presentation state. Raw storage is consulted only to bootstrap or
			// synchronize the named record used by generic Get and OM_GET.
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetStringPresentationStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadStringPresentationState(ref platform, state, obj,
				out var presentation)) return false;
			value = attribute == StringMaxLen ? presentation.MaxLen :
				attribute == StringSecret ? presentation.Secret :
				attribute == StringFormat ? presentation.Format :
				presentation.Unicode;
			return true;
		}
		if ((attribute == GaugeCurrent || attribute == GaugeMax ||
			attribute == GaugeDivide || attribute == GaugeHoriz) &&
			cls == MuiControlClass.Gauge)
		{
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryGetGaugeStateRecord(ref platform, state, obj, out _))
				return false;
			handled = true;
			if (!TryReadGaugeState(ref platform, state, obj, out var gauge))
				return false;
			value = attribute == GaugeCurrent ? gauge.Current :
				attribute == GaugeMax ? gauge.Maximum :
				attribute == GaugeDivide ? gauge.Divide : gauge.Horizontal;
			return true;
		}
		return false;
	}

	internal static bool TryReadAreaWeightState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiAreaWeightState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		// Weight is a shared legacy scalar, but the named guest record is the
		// getter/layout projection. Raw storage is only the synchronization seam.
		var rawWeight = ReadRaw(ref platform, state, obj, Weight, 100);
		if (TryReadAreaWeightStateRecord(ref platform, state, obj, out var record) &&
			record.Weight != rawWeight)
		{
			record.Weight = rawWeight;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				AreaWeightStateKey);
			if (!MuiAreaWeightStateRecordCodec.Write(ref platform, block, record))
				return false;
		}
		result.Weight = rawWeight;
		return true;
	}

	private static bool TryReadAreaWeightStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiAreaWeightStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			AreaWeightStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			AreaWeightStateKey) != unchecked((int)MuiAreaWeightStateRecord.Size))
			return false;
		return MuiAreaWeightStateRecordCodec.TryRead(ref platform, block, out value);
	}

	private static bool EnsureAreaWeightStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadAreaWeightStateRecord(ref platform, state, obj, out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaWeightStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaWeightStateRecord.Size);
		var value = default(MuiAreaWeightStateRecord);
		value.Magic = MuiAreaWeightStateRecord.Cookie;
		value.Weight = ReadRaw(ref platform, state, obj, Weight, 100);
		var written = MuiAreaWeightStateRecordCodec.Write(ref platform, scratch,
			value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			AreaWeightStateKey, scratch,
			unchecked((int)MuiAreaWeightStateRecord.Size));
		platform.Clear(scratch, MuiAreaWeightStateRecord.Size);
		platform.Free(scratch, MuiAreaWeightStateRecord.Size);
		return added;
	}

	private static bool PublishAreaWeightState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, MuiAreaWeightState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureAreaWeightStateRecord(ref platform, state, obj)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			AreaWeightStateKey);
		var stored = default(MuiAreaWeightStateRecord);
		stored.Magic = MuiAreaWeightStateRecord.Cookie;
		stored.Weight = value.Weight;
		if (!MuiAreaWeightStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			Weight, stored.Weight, false);
	}

	internal static bool TryGetAreaWeightStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiAreaWeightStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadAreaWeightStateRecord(ref platform, state, obj, out value);

	private static bool SyncAreaWeightState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint weight)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var value = default(MuiAreaWeightState);
		value.Weight = weight;
		return PublishAreaWeightState(ref platform, state, obj, value);
	}

	private static void ClampNumeric<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadNumericState(ref platform, state, obj, out var numeric)) return;
		var minimum = unchecked((int)numeric.Minimum);
		var maximum = unchecked((int)numeric.Maximum);
		if (maximum < minimum) return;
		var value = unchecked((int)numeric.Value);
		var clamped = value < minimum ? minimum : (value > maximum ? maximum : value);
		if (clamped != value)
		{
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, NumericValue,
				unchecked((uint)clamped), false);
			numeric.Value = unchecked((uint)clamped);
			PublishNumericState(ref platform, state, obj, numeric);
		}
	}

	internal static bool TryReadSliderPresentationState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiSliderPresentationState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		// Getter projection uses this routine, so legacy attributes must be read
		// raw-only to avoid recursing through CommonControlCore.TryGet.
		var horizontal = ReadRaw(ref platform, state, obj, SliderHoriz, 1);
		var quiet = ReadRaw(ref platform, state, obj, SliderQuiet, 0);
		if (TryReadSliderPresentationStateRecord(ref platform, state, obj,
			out var record))
		{
			if (record.Horizontal != horizontal || record.Quiet != quiet)
			{
				record.Horizontal = horizontal;
				record.Quiet = quiet;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					SliderPresentationStateKey);
				if (!MuiSliderPresentationStateRecordCodec.Write(ref platform,
					block, record)) return false;
			}
		}
		result.Horizontal = horizontal;
		result.Quiet = quiet;
		return true;
	}

	private static bool TryReadSliderPresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiSliderPresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			SliderPresentationStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			SliderPresentationStateKey) != unchecked((int)
			MuiSliderPresentationStateRecord.Size)) return false;
		return MuiSliderPresentationStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureSliderPresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadSliderPresentationStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiSliderPresentationStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiSliderPresentationStateRecord.Size);
		var value = default(MuiSliderPresentationStateRecord);
		value.Magic = MuiSliderPresentationStateRecord.Cookie;
		value.Horizontal = ReadRaw(ref platform, state, obj, SliderHoriz, 1);
		value.Quiet = ReadRaw(ref platform, state, obj, SliderQuiet, 0);
		var written = MuiSliderPresentationStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			SliderPresentationStateKey, scratch,
			unchecked((int)MuiSliderPresentationStateRecord.Size));
		platform.Clear(scratch, MuiSliderPresentationStateRecord.Size);
		platform.Free(scratch, MuiSliderPresentationStateRecord.Size);
		return added;
	}

	private static bool PublishSliderPresentationState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiSliderPresentationState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureSliderPresentationStateRecord(ref platform, state, obj))
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			SliderPresentationStateKey);
		var stored = default(MuiSliderPresentationStateRecord);
		stored.Magic = MuiSliderPresentationStateRecord.Cookie;
		stored.Horizontal = value.Horizontal;
		stored.Quiet = value.Quiet;
		if (!MuiSliderPresentationStateRecordCodec.Write(ref platform, block,
			stored)) return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			SliderHoriz, stored.Horizontal, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			SliderQuiet, stored.Quiet, false);
	}

	internal static bool TryGetSliderPresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiSliderPresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadSliderPresentationStateRecord(ref platform, state, obj,
			out value);

	internal static bool TryReadScalePresentationState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiScalePresentationState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		// Keep Scale getter synchronization on the raw legacy attribute path.
		var horizontal = ReadRaw(ref platform, state, obj, ScaleHoriz, 1);
		if (TryReadScalePresentationStateRecord(ref platform, state, obj,
			out var record) && record.Horizontal != horizontal)
		{
			record.Horizontal = horizontal;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				ScalePresentationStateKey);
			if (!MuiScalePresentationStateRecordCodec.Write(ref platform, block,
				record)) return false;
		}
		result.Horizontal = horizontal;
		return true;
	}

	private static bool TryReadScalePresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiScalePresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ScalePresentationStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ScalePresentationStateKey) != unchecked((int)
			MuiScalePresentationStateRecord.Size)) return false;
		return MuiScalePresentationStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureScalePresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadScalePresentationStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiScalePresentationStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiScalePresentationStateRecord.Size);
		var value = default(MuiScalePresentationStateRecord);
		value.Magic = MuiScalePresentationStateRecord.Cookie;
		value.Horizontal = ReadRaw(ref platform, state, obj, ScaleHoriz, 1);
		var written = MuiScalePresentationStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			ScalePresentationStateKey, scratch,
			unchecked((int)MuiScalePresentationStateRecord.Size));
		platform.Clear(scratch, MuiScalePresentationStateRecord.Size);
		platform.Free(scratch, MuiScalePresentationStateRecord.Size);
		return added;
	}

	private static bool PublishScalePresentationState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiScalePresentationState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureScalePresentationStateRecord(ref platform, state, obj))
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ScalePresentationStateKey);
		var stored = default(MuiScalePresentationStateRecord);
		stored.Magic = MuiScalePresentationStateRecord.Cookie;
		stored.Horizontal = value.Horizontal;
		if (!MuiScalePresentationStateRecordCodec.Write(ref platform, block,
			stored)) return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ScaleHoriz, stored.Horizontal, false);
	}

	internal static bool TryGetScalePresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiScalePresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadScalePresentationStateRecord(ref platform, state, obj,
			out value);

	internal static bool TryReadGadgetInteractionState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiGadgetInteractionState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		// Getter projection uses this routine, so legacy Gadget attributes must be
		// read raw-only to avoid recursing through CommonControlCore.TryGet.
		var mode = ReadRaw(ref platform, state, obj, InputMode, InputModeNone);
		var selected = ReadRaw(ref platform, state, obj, Selected, 0);
		var pressed = ReadRaw(ref platform, state, obj, Pressed, 0);
		var showSelState = ReadRaw(ref platform, state, obj, ShowSelState, 1);
		if (TryReadGadgetInteractionStateRecord(ref platform, state, obj,
			out var record))
		{
			if (record.InputMode != mode || record.Selected != selected ||
				record.Pressed != pressed || record.ShowSelState != showSelState)
			{
				record.InputMode = mode;
				record.Selected = selected;
				record.Pressed = pressed;
				record.ShowSelState = showSelState;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					GadgetInteractionStateKey);
				if (!MuiGadgetInteractionStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
		}
		result.InputMode = mode;
		result.Selected = selected;
		result.Pressed = pressed;
		result.ShowSelState = showSelState;
		return true;
	}

	private static bool TryReadGadgetInteractionStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiGadgetInteractionStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			GadgetInteractionStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			GadgetInteractionStateKey) != unchecked((int)
			MuiGadgetInteractionStateRecord.Size)) return false;
		return MuiGadgetInteractionStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureGadgetInteractionStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadGadgetInteractionStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiGadgetInteractionStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiGadgetInteractionStateRecord.Size);
		var value = default(MuiGadgetInteractionStateRecord);
		value.Magic = MuiGadgetInteractionStateRecord.Cookie;
		value.InputMode = ReadRaw(ref platform, state, obj, InputMode,
			InputModeNone);
		value.Selected = ReadRaw(ref platform, state, obj, Selected, 0);
		value.Pressed = ReadRaw(ref platform, state, obj, Pressed, 0);
		value.ShowSelState = ReadRaw(ref platform, state, obj, ShowSelState, 1);
		var written = MuiGadgetInteractionStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			GadgetInteractionStateKey, scratch,
			unchecked((int)MuiGadgetInteractionStateRecord.Size));
		platform.Clear(scratch, MuiGadgetInteractionStateRecord.Size);
		platform.Free(scratch, MuiGadgetInteractionStateRecord.Size);
		return added;
	}

	private static bool PublishGadgetInteractionState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiGadgetInteractionState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureGadgetInteractionStateRecord(ref platform, state, obj))
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			GadgetInteractionStateKey);
		var stored = default(MuiGadgetInteractionStateRecord);
		stored.Magic = MuiGadgetInteractionStateRecord.Cookie;
		stored.InputMode = value.InputMode;
		stored.Selected = value.Selected;
		stored.Pressed = value.Pressed;
		stored.ShowSelState = value.ShowSelState;
		if (!MuiGadgetInteractionStateRecordCodec.Write(ref platform, block,
			stored)) return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			InputMode, stored.InputMode, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			Selected, stored.Selected, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			Pressed, stored.Pressed, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ShowSelState, stored.ShowSelState, false);
	}

	internal static bool TryGetGadgetInteractionStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiGadgetInteractionStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadGadgetInteractionStateRecord(ref platform, state, obj,
			out value);

	internal static bool TryReadGadgetGadgetState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiGadgetGadgetState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (TryReadGadgetGadgetStateRecord(ref platform, state, obj,
			out var record))
		{
			var raw = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				GadgetGadget, record.Gadget.Raw));
			if (raw.Raw != record.Gadget.Raw)
			{
				record.Gadget = raw;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					GadgetGadgetStateKey);
				if (!MuiGadgetGadgetStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			result.Gadget = raw;
			return true;
		}
		result.Gadget = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			GadgetGadget, 0));
		return true;
	}

	private static bool TryReadGadgetGadgetStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiGadgetGadgetStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			GadgetGadgetStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			GadgetGadgetStateKey) != unchecked((int)
				MuiGadgetGadgetStateRecord.Size)) return false;
		return MuiGadgetGadgetStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureGadgetGadgetStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadGadgetGadgetStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiGadgetGadgetStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiGadgetGadgetStateRecord.Size);
		var value = default(MuiGadgetGadgetStateRecord);
		value.Magic = MuiGadgetGadgetStateRecord.Cookie;
		value.Gadget = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			GadgetGadget, 0));
		var written = MuiGadgetGadgetStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			GadgetGadgetStateKey, scratch,
			unchecked((int)MuiGadgetGadgetStateRecord.Size));
		platform.Clear(scratch, MuiGadgetGadgetStateRecord.Size);
		platform.Free(scratch, MuiGadgetGadgetStateRecord.Size);
		return added;
	}

	private static bool PublishGadgetGadgetState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiGadgetGadgetState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureGadgetGadgetStateRecord(ref platform, state, obj))
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			GadgetGadgetStateKey);
		var stored = default(MuiGadgetGadgetStateRecord);
		stored.Magic = MuiGadgetGadgetStateRecord.Cookie;
		stored.Gadget = value.Gadget;
		if (!MuiGadgetGadgetStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			GadgetGadget, stored.Gadget.Raw, false);
	}

	internal static bool TryGetGadgetGadgetStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiGadgetGadgetStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadGadgetGadgetStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadLevelmeterPresentationState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiLevelmeterPresentationState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		// Levelmeter shares MUIA_Gauge_Horiz; keep its record synchronization raw.
		var horizontal = ReadRaw(ref platform, state, obj, GaugeHoriz, 1);
		if (TryReadLevelmeterPresentationStateRecord(ref platform, state, obj,
			out var record) && record.Horizontal != horizontal)
		{
			record.Horizontal = horizontal;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				LevelmeterPresentationStateKey);
			if (!MuiLevelmeterPresentationStateRecordCodec.Write(ref platform,
				block, record)) return false;
		}
		result.Horizontal = horizontal;
		return true;
	}

	private static bool TryReadLevelmeterPresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiLevelmeterPresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			LevelmeterPresentationStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			LevelmeterPresentationStateKey) != unchecked((int)
			MuiLevelmeterPresentationStateRecord.Size)) return false;
		return MuiLevelmeterPresentationStateRecordCodec.TryRead(ref platform,
			block, out value);
	}

	private static bool EnsureLevelmeterPresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadLevelmeterPresentationStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiLevelmeterPresentationStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiLevelmeterPresentationStateRecord.Size);
		var value = default(MuiLevelmeterPresentationStateRecord);
		value.Magic = MuiLevelmeterPresentationStateRecord.Cookie;
		value.Horizontal = ReadRaw(ref platform, state, obj, GaugeHoriz, 1);
		var written = MuiLevelmeterPresentationStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			LevelmeterPresentationStateKey, scratch,
			unchecked((int)MuiLevelmeterPresentationStateRecord.Size));
		platform.Clear(scratch, MuiLevelmeterPresentationStateRecord.Size);
		platform.Free(scratch, MuiLevelmeterPresentationStateRecord.Size);
		return added;
	}

	private static bool PublishLevelmeterPresentationState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiLevelmeterPresentationState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureLevelmeterPresentationStateRecord(ref platform, state, obj))
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			LevelmeterPresentationStateKey);
		var stored = default(MuiLevelmeterPresentationStateRecord);
		stored.Magic = MuiLevelmeterPresentationStateRecord.Cookie;
		stored.Horizontal = value.Horizontal;
		if (!MuiLevelmeterPresentationStateRecordCodec.Write(ref platform,
			block, stored)) return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			GaugeHoriz, stored.Horizontal, false);
	}

	internal static bool TryGetLevelmeterPresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiLevelmeterPresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadLevelmeterPresentationStateRecord(ref platform, state, obj,
			out value);

	internal static bool TryReadTextPresentationState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiTextPresentationState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		var setMin = ReadRaw(ref platform, state, obj, TextSetMin, 1);
		var setMax = ReadRaw(ref platform, state, obj, TextSetMax, 0);
		var setVMax = ReadRaw(ref platform, state, obj, TextSetVMax, 1);
		var controlChar = ReadRaw(ref platform, state, obj, TextControlChar, 0);
		var marking = ReadRaw(ref platform, state, obj, TextMarking, 0);
		var shorten = ReadRaw(ref platform, state, obj, TextShorten,
			TextShortenNothing);
		var hiPresent = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			TextHiChar, out var rawHiChar) ? 1u : 0u;
		var hiChar = hiPresent == 0 ? 0u : rawHiChar;
		if (TryReadTextPresentationStateRecord(ref platform, state, obj,
			out var record))
		{
			if (record.SetMin != setMin || record.SetMax != setMax ||
				record.SetVMax != setVMax || record.ControlChar != controlChar ||
				record.Marking != marking || record.Shorten != shorten ||
				record.HiChar != hiChar || record.HiCharPresent != hiPresent)
			{
				record.SetMin = setMin;
				record.SetMax = setMax;
				record.SetVMax = setVMax;
				record.ControlChar = controlChar;
				record.Marking = marking;
				record.Shorten = shorten;
				record.HiChar = hiChar;
				record.HiCharPresent = hiPresent;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					TextPresentationStateKey);
				if (!MuiTextPresentationStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
		}
		result.SetMin = setMin;
		result.SetMax = setMax;
		result.SetVMax = setVMax;
		result.ControlChar = controlChar;
		result.Marking = marking;
		result.Shorten = shorten;
		result.HiChar = hiChar;
		result.HiCharPresent = hiPresent;
		return true;
	}

	private static bool TryReadTextPresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiTextPresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			TextPresentationStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			TextPresentationStateKey) != unchecked((int)
			MuiTextPresentationStateRecord.Size)) return false;
		return MuiTextPresentationStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureTextPresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadTextPresentationStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiTextPresentationStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiTextPresentationStateRecord.Size);
		var value = default(MuiTextPresentationStateRecord);
		value.Magic = MuiTextPresentationStateRecord.Cookie;
		value.SetMin = ReadRaw(ref platform, state, obj, TextSetMin, 1);
		value.SetMax = ReadRaw(ref platform, state, obj, TextSetMax, 0);
		value.SetVMax = ReadRaw(ref platform, state, obj, TextSetVMax, 1);
		value.ControlChar = ReadRaw(ref platform, state, obj, TextControlChar, 0);
		value.Marking = ReadRaw(ref platform, state, obj, TextMarking, 0);
		value.Shorten = ReadRaw(ref platform, state, obj, TextShorten,
			TextShortenNothing);
		value.HiCharPresent = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, TextHiChar, out var rawHiChar) ? 1u : 0u;
		value.HiChar = value.HiCharPresent == 0 ? 0u : rawHiChar;
		var written = MuiTextPresentationStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			TextPresentationStateKey, scratch,
			unchecked((int)MuiTextPresentationStateRecord.Size));
		platform.Clear(scratch, MuiTextPresentationStateRecord.Size);
		platform.Free(scratch, MuiTextPresentationStateRecord.Size);
		return added;
	}

	private static bool PublishTextPresentationState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiTextPresentationState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureTextPresentationStateRecord(ref platform, state, obj))
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			TextPresentationStateKey);
		var stored = default(MuiTextPresentationStateRecord);
		stored.Magic = MuiTextPresentationStateRecord.Cookie;
		stored.SetMin = value.SetMin;
		stored.SetMax = value.SetMax;
		stored.SetVMax = value.SetVMax;
		stored.ControlChar = value.ControlChar;
		stored.Marking = value.Marking;
		stored.Shorten = value.Shorten;
		stored.HiChar = value.HiChar;
		stored.HiCharPresent = value.HiCharPresent;
		if (!MuiTextPresentationStateRecordCodec.Write(ref platform, block,
			stored)) return false;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			TextSetMin, stored.SetMin, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			TextSetMax, stored.SetMax, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			TextSetVMax, stored.SetVMax, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			TextControlChar, stored.ControlChar, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			TextMarking, stored.Marking, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			TextShorten, stored.Shorten, false)) return false;
		if (stored.HiCharPresent != 0 &&
			!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				TextHiChar, stored.HiChar, false)) return false;
		return true;
	}

	internal static bool TryGetTextPresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiTextPresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadTextPresentationStateRecord(ref platform, state, obj,
			out value);

	internal static bool TryReadTextShortenedState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiTextShortenedState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (TryReadTextShortenedStateRecord(ref platform, state, obj,
			out var record))
		{
			var raw = ReadRaw(ref platform, state, obj, TextShortened,
				record.Shortened);
			var normalized = raw == 0 ? 0u : 1u;
			if (record.Shortened != normalized)
			{
				record.Shortened = normalized;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					TextShortenedStateKey);
				if (!MuiTextShortenedStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			if (raw != normalized && !MuiHeadlessObjectCore.SetAttribute(ref platform,
				state, obj, TextShortened, normalized, false)) return false;
			result.Shortened = normalized;
			return true;
		}
		result.Shortened = ReadRaw(ref platform, state, obj, TextShortened, 0) ==
			0 ? 0u : 1u;
		return true;
	}

	private static bool TryReadTextShortenedStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiTextShortenedStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			TextShortenedStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			TextShortenedStateKey) != unchecked((int)MuiTextShortenedStateRecord.Size))
			return false;
		return MuiTextShortenedStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureTextShortenedStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadTextShortenedStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiTextShortenedStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiTextShortenedStateRecord.Size);
		var value = default(MuiTextShortenedStateRecord);
		value.Magic = MuiTextShortenedStateRecord.Cookie;
		value.Shortened = ReadRaw(ref platform, state, obj, TextShortened, 0) ==
			0 ? 0u : 1u;
		var written = MuiTextShortenedStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			TextShortenedStateKey, scratch,
			unchecked((int)MuiTextShortenedStateRecord.Size));
		platform.Clear(scratch, MuiTextShortenedStateRecord.Size);
		platform.Free(scratch, MuiTextShortenedStateRecord.Size);
		return added;
	}

	private static bool PublishTextShortenedState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiTextShortenedState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureTextShortenedStateRecord(ref platform, state, obj))
			return false;
		var normalized = value.Shortened == 0 ? 0u : 1u;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			TextShortenedStateKey);
		var stored = default(MuiTextShortenedStateRecord);
		stored.Magic = MuiTextShortenedStateRecord.Cookie;
		stored.Shortened = normalized;
		if (!MuiTextShortenedStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			TextShortened, normalized, false);
	}

	internal static bool TryGetTextShortenedStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiTextShortenedStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadTextShortenedStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadRectanglePresentationState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiRectanglePresentationState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		var horizontal = ReadRaw(ref platform, state, obj, RectangleHBar, 0);
		var vertical = ReadRaw(ref platform, state, obj, RectangleVBar, 0);
		if (TryReadRectanglePresentationStateRecord(ref platform, state, obj,
			out var record))
		{
			if (record.HorizontalBar != horizontal || record.VerticalBar != vertical)
			{
				record.HorizontalBar = horizontal;
				record.VerticalBar = vertical;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					RectanglePresentationStateKey);
				if (!MuiRectanglePresentationStateRecordCodec.Write(ref platform,
					block, record)) return false;
			}
		}
		result.HorizontalBar = horizontal;
		result.VerticalBar = vertical;
		return true;
	}

	private static bool TryReadRectanglePresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiRectanglePresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			RectanglePresentationStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			RectanglePresentationStateKey) != unchecked((int)
			MuiRectanglePresentationStateRecord.Size)) return false;
		return MuiRectanglePresentationStateRecordCodec.TryRead(ref platform,
			block, out value);
	}

	private static bool EnsureRectanglePresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadRectanglePresentationStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiRectanglePresentationStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiRectanglePresentationStateRecord.Size);
		var value = default(MuiRectanglePresentationStateRecord);
		value.Magic = MuiRectanglePresentationStateRecord.Cookie;
		value.HorizontalBar = ReadRaw(ref platform, state, obj, RectangleHBar, 0);
		value.VerticalBar = ReadRaw(ref platform, state, obj, RectangleVBar, 0);
		var written = MuiRectanglePresentationStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			RectanglePresentationStateKey, scratch,
			unchecked((int)MuiRectanglePresentationStateRecord.Size));
		platform.Clear(scratch, MuiRectanglePresentationStateRecord.Size);
		platform.Free(scratch, MuiRectanglePresentationStateRecord.Size);
		return added;
	}

	private static bool PublishRectanglePresentationState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiRectanglePresentationState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureRectanglePresentationStateRecord(ref platform, state, obj))
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			RectanglePresentationStateKey);
		var stored = default(MuiRectanglePresentationStateRecord);
		stored.Magic = MuiRectanglePresentationStateRecord.Cookie;
		stored.HorizontalBar = value.HorizontalBar;
		stored.VerticalBar = value.VerticalBar;
		if (!MuiRectanglePresentationStateRecordCodec.Write(ref platform, block,
			stored)) return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			RectangleHBar, stored.HorizontalBar, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			RectangleVBar, stored.VerticalBar, false);
	}

	internal static bool TryGetRectanglePresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiRectanglePresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadRectanglePresentationStateRecord(ref platform, state, obj,
			out value);

	private static void ClampGauge<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadGaugeState(ref platform, state, obj, out var gauge)) return;
		if (gauge.Current > gauge.Maximum)
		{
			gauge.Current = gauge.Maximum;
			PublishGaugeState(ref platform, state, obj, gauge);
		}
	}

	internal static bool TryReadGaugeState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiGaugeState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		var maximum = ReadRaw(ref platform, state, obj, GaugeMax, 100);
		var current = ReadRaw(ref platform, state, obj, GaugeCurrent, 0);
		var divide = ReadRaw(ref platform, state, obj, GaugeDivide, 0);
		var horizontal = ReadRaw(ref platform, state, obj, GaugeHoriz, 1);
		if (TryReadGaugeStateRecord(ref platform, state, obj, out var record))
		{
			if (record.Maximum != maximum || record.Current != current ||
				record.Divide != divide || record.Horizontal != horizontal)
			{
				record.Maximum = maximum;
				record.Current = current;
				record.Divide = divide;
				record.Horizontal = horizontal;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					GaugeStateKey);
				if (!MuiGaugeStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
		}
		result.Maximum = maximum;
		result.Current = current;
		result.Divide = divide;
		result.Horizontal = horizontal;
		return true;
	}

	private static bool TryReadGaugeStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiGaugeStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			GaugeStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			GaugeStateKey) != unchecked((int)MuiGaugeStateRecord.Size))
			return false;
		return MuiGaugeStateRecordCodec.TryRead(ref platform, block, out value);
	}

	private static bool EnsureGaugeStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadGaugeStateRecord(ref platform, state, obj, out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiGaugeStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiGaugeStateRecord.Size);
		var value = default(MuiGaugeStateRecord);
		value.Magic = MuiGaugeStateRecord.Cookie;
		value.Maximum = ReadRaw(ref platform, state, obj, GaugeMax, 100);
		value.Current = ReadRaw(ref platform, state, obj, GaugeCurrent, 0);
		value.Divide = ReadRaw(ref platform, state, obj, GaugeDivide, 0);
		value.Horizontal = ReadRaw(ref platform, state, obj, GaugeHoriz, 1);
		var written = MuiGaugeStateRecordCodec.Write(ref platform, scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			GaugeStateKey, scratch, unchecked((int)MuiGaugeStateRecord.Size));
		platform.Clear(scratch, MuiGaugeStateRecord.Size);
		platform.Free(scratch, MuiGaugeStateRecord.Size);
		return added;
	}

	private static bool PublishGaugeState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, MuiGaugeState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureGaugeStateRecord(ref platform, state, obj)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			GaugeStateKey);
		var stored = default(MuiGaugeStateRecord);
		stored.Magic = MuiGaugeStateRecord.Cookie;
		stored.Maximum = value.Maximum;
		stored.Current = value.Current;
		stored.Divide = value.Divide;
		stored.Horizontal = value.Horizontal;
		if (!MuiGaugeStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			GaugeMax, stored.Maximum, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			GaugeCurrent, stored.Current, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			GaugeDivide, stored.Divide, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			GaugeHoriz, stored.Horizontal, false);
	}

	internal static bool TryGetGaugeStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiGaugeStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadGaugeStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadScrollbarLayoutState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiScrollbarLayoutState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		// Getter projection uses this routine, so legacy Scrollbar attributes are
		// read raw-only to avoid recursing through CommonControlCore.TryGet.
		var horizontal = ReadRaw(ref platform, state, obj, GroupHoriz, 0);
		var type = ReadRaw(ref platform, state, obj, ScrollbarType,
			ScrollbarTypeDefault);
		if (TryReadScrollbarLayoutStateRecord(ref platform, state, obj,
			out var record))
		{
			if (record.Horizontal != horizontal || record.Type != type)
			{
				record.Horizontal = horizontal;
				record.Type = type;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					ScrollbarLayoutStateKey);
				if (!MuiScrollbarLayoutStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
		}
		result.Horizontal = horizontal;
		result.Type = type;
		return true;
	}

	private static bool TryReadScrollbarLayoutStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiScrollbarLayoutStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ScrollbarLayoutStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ScrollbarLayoutStateKey) != unchecked((int)MuiScrollbarLayoutStateRecord.Size))
			return false;
		return MuiScrollbarLayoutStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureScrollbarLayoutStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadScrollbarLayoutStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiScrollbarLayoutStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiScrollbarLayoutStateRecord.Size);
		var value = default(MuiScrollbarLayoutStateRecord);
		value.Magic = MuiScrollbarLayoutStateRecord.Cookie;
		value.Horizontal = ReadRaw(ref platform, state, obj, GroupHoriz, 0);
		value.Type = ReadRaw(ref platform, state, obj, ScrollbarType,
			ScrollbarTypeDefault);
		var written = MuiScrollbarLayoutStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			ScrollbarLayoutStateKey, scratch,
			unchecked((int)MuiScrollbarLayoutStateRecord.Size));
		platform.Clear(scratch, MuiScrollbarLayoutStateRecord.Size);
		platform.Free(scratch, MuiScrollbarLayoutStateRecord.Size);
		return added;
	}

	private static bool PublishScrollbarLayoutState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiScrollbarLayoutState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureScrollbarLayoutStateRecord(ref platform, state, obj))
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ScrollbarLayoutStateKey);
		var stored = default(MuiScrollbarLayoutStateRecord);
		stored.Magic = MuiScrollbarLayoutStateRecord.Cookie;
		stored.Horizontal = value.Horizontal;
		stored.Type = value.Type;
		if (!MuiScrollbarLayoutStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			GroupHoriz, stored.Horizontal, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ScrollbarType, stored.Type, false);
	}

	internal static bool TryGetScrollbarLayoutStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiScrollbarLayoutStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadScrollbarLayoutStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadPropRangeState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiPropRangeState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		// Getter projection uses this routine, so legacy Prop attributes are read
		// raw-only to avoid recursing through CommonControlCore.TryGet.
		var entries = ReadRaw(ref platform, state, obj, PropEntries, 0);
		var visible = ReadRaw(ref platform, state, obj, PropVisible, 0);
		var first = ReadRaw(ref platform, state, obj, PropFirst, 0);
		if (TryReadPropRangeStateRecord(ref platform, state, obj, out var record))
		{
			if (record.Entries != entries || record.Visible != visible ||
				record.First != first)
			{
				record.Entries = entries;
				record.Visible = visible;
				record.First = first;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					PropRangeStateKey);
				if (!MuiPropRangeStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
		}
		result.Entries = entries;
		result.Visible = visible;
		result.First = first;
		return true;
	}

	private static bool TryReadPropRangeStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiPropRangeStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PropRangeStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			PropRangeStateKey) != unchecked((int)MuiPropRangeStateRecord.Size))
			return false;
		return MuiPropRangeStateRecordCodec.TryRead(ref platform, block, out value);
	}

	private static bool EnsurePropRangeStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadPropRangeStateRecord(ref platform, state, obj, out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiPropRangeStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiPropRangeStateRecord.Size);
		var value = default(MuiPropRangeStateRecord);
		value.Magic = MuiPropRangeStateRecord.Cookie;
		value.Entries = ReadRaw(ref platform, state, obj, PropEntries, 0);
		value.Visible = ReadRaw(ref platform, state, obj, PropVisible, 0);
		value.First = ReadRaw(ref platform, state, obj, PropFirst, 0);
		var written = MuiPropRangeStateRecordCodec.Write(ref platform, scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			PropRangeStateKey, scratch, unchecked((int)MuiPropRangeStateRecord.Size));
		platform.Clear(scratch, MuiPropRangeStateRecord.Size);
		platform.Free(scratch, MuiPropRangeStateRecord.Size);
		return added;
	}

	private static bool PublishPropRangeState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiPropRangeState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsurePropRangeStateRecord(ref platform, state, obj)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PropRangeStateKey);
		var stored = default(MuiPropRangeStateRecord);
		stored.Magic = MuiPropRangeStateRecord.Cookie;
		stored.Entries = value.Entries;
		stored.Visible = value.Visible;
		stored.First = value.First;
		if (!MuiPropRangeStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			PropEntries, stored.Entries, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			PropVisible, stored.Visible, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			PropFirst, stored.First, false);
	}

	internal static bool TryGetPropRangeStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiPropRangeStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadPropRangeStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadPropPolicyState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiPropPolicyState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		// Generic Get and OM_GET project these values through this record. Raw
		// storage is the compatibility seam for caller writes and bootstrap only.
		var horizontal = ReadRaw(ref platform, state, obj, PropHoriz, 1);
		var deltaFactor = ReadRaw(ref platform, state, obj, PropDeltaFactor, 1);
		var slider = ReadRaw(ref platform, state, obj, PropSlider, 0);
		var useWinBorder = ReadRaw(ref platform, state, obj, PropUseWinBorder, 0);
		if (TryReadPropPolicyStateRecord(ref platform, state, obj, out var record))
		{
			if (record.Horizontal != horizontal || record.DeltaFactor != deltaFactor ||
				record.Slider != slider || record.UseWinBorder != useWinBorder)
			{
				record.Horizontal = horizontal;
				record.DeltaFactor = deltaFactor;
				record.Slider = slider;
				record.UseWinBorder = useWinBorder;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					PropPolicyStateKey);
				if (!MuiPropPolicyStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
		}
		result.Horizontal = horizontal;
		result.DeltaFactor = deltaFactor;
		result.Slider = slider;
		result.UseWinBorder = useWinBorder;
		return true;
	}

	private static bool TryReadPropPolicyStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiPropPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PropPolicyStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			PropPolicyStateKey) != unchecked((int)MuiPropPolicyStateRecord.Size))
			return false;
		return MuiPropPolicyStateRecordCodec.TryRead(ref platform, block, out value);
	}

	private static bool EnsurePropPolicyStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadPropPolicyStateRecord(ref platform, state, obj, out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiPropPolicyStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiPropPolicyStateRecord.Size);
		var value = default(MuiPropPolicyStateRecord);
		value.Magic = MuiPropPolicyStateRecord.Cookie;
		value.Horizontal = ReadRaw(ref platform, state, obj, PropHoriz, 1);
		value.DeltaFactor = ReadRaw(ref platform, state, obj, PropDeltaFactor, 1);
		value.Slider = ReadRaw(ref platform, state, obj, PropSlider, 0);
		value.UseWinBorder = ReadRaw(ref platform, state, obj, PropUseWinBorder, 0);
		var written = MuiPropPolicyStateRecordCodec.Write(ref platform, scratch,
			value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			PropPolicyStateKey, scratch,
			unchecked((int)MuiPropPolicyStateRecord.Size));
		platform.Clear(scratch, MuiPropPolicyStateRecord.Size);
		platform.Free(scratch, MuiPropPolicyStateRecord.Size);
		return added;
	}

	private static bool PublishPropPolicyState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, MuiPropPolicyState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsurePropPolicyStateRecord(ref platform, state, obj)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PropPolicyStateKey);
		var stored = default(MuiPropPolicyStateRecord);
		stored.Magic = MuiPropPolicyStateRecord.Cookie;
		stored.Horizontal = value.Horizontal;
		stored.DeltaFactor = value.DeltaFactor;
		stored.Slider = value.Slider;
		stored.UseWinBorder = value.UseWinBorder;
		if (!MuiPropPolicyStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			PropHoriz, stored.Horizontal, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			PropDeltaFactor, stored.DeltaFactor, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			PropSlider, stored.Slider, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			PropUseWinBorder, stored.UseWinBorder, false);
	}

	internal static bool TryGetPropPolicyStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiPropPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadPropPolicyStateRecord(ref platform, state, obj, out value);

	private static bool SyncPropPolicyField<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadPropPolicyState(ref platform, state, obj,
			out var policy)) return false;
		if (attribute == PropHoriz) policy.Horizontal = value;
		else if (attribute == PropDeltaFactor) policy.DeltaFactor = value;
		else if (attribute == PropSlider) policy.Slider = value;
		else if (attribute == PropUseWinBorder) policy.UseWinBorder = value;
		else return false;
		return PublishPropPolicyState(ref platform, state, obj, policy);
	}

	private static void ClampProp<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadPropRangeState(ref platform, state, obj,
			out var range)) return;
		var entries = range.Entries;
		var visible = range.Visible;
		var first = unchecked((int)range.First);
		var last = entries > visible ? unchecked((int)(entries - visible)) : 0;
		var clamped = first < 0 ? 0 : (first > last ? last : first);
		if (clamped != first)
		{
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, PropFirst,
				unchecked((uint)clamped), false);
			range.First = unchecked((uint)clamped);
			PublishPropRangeState(ref platform, state, obj, range);
		}
	}

	// ---- Numeric value behavior ----------------------------------------------

	public static bool SetNumericValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int value, bool fromInput, bool notify = true)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (fromInput && (!TryReadAreaPresentationState(ref platform, state, obj,
			out var areaPresentation) || areaPresentation.Disabled != 0))
			return false;
		if (!TryReadNumericState(ref platform, state, obj,
			out var numeric)) return false;
		var minimum = unchecked((int)numeric.Minimum);
		var maximum = unchecked((int)numeric.Maximum);
		if (maximum < minimum) return false;
		var clamped = value < minimum ? minimum : (value > maximum ? maximum : value);
		var current = unchecked((int)numeric.Value);
		if (clamped == current) return true;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			NumericValue, unchecked((uint)clamped), notify)) return false;
		numeric.Value = unchecked((uint)clamped);
		if (!PublishNumericState(ref platform, state, obj, numeric)) return false;
		return platform.ScheduleRedraw(obj, 2);
	}

	public static bool ChangeNumeric<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int amount) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadNumericState(ref platform, state, obj,
			out var numeric)) return false;
		var current = unchecked((int)numeric.Value);
		int value;
		if (amount > 0 && current > int.MaxValue - amount) value = int.MaxValue;
		else if (amount < 0 && current < int.MinValue - amount) value = int.MinValue;
		else value = current + amount;
		return SetNumericValue(ref platform, state, obj, value, true);
	}

	public static bool SetNumericDefault<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiLayoutPlatform =>
		TryReadNumericState(ref platform, state, obj, out var numeric) &&
		SetNumericValue(ref platform, state, obj,
			unchecked((int)numeric.Default), false);

	public static int ScaleToValue<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int scaleMinimum, int scaleMaximum, int scale)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadNumericState(ref platform, state, obj,
			out var numeric)) return scaleMinimum;
		var minimum = unchecked((int)numeric.Minimum);
		var maximum = unchecked((int)numeric.Maximum);
		if (scaleMaximum <= scaleMinimum || maximum <= minimum) return minimum;
		var target = scale;
		if (target < scaleMinimum) target = scaleMinimum;
		if (target > scaleMaximum) target = scaleMaximum;
		if (numeric.Reverse != 0)
			target = scaleMaximum - (target - scaleMinimum);
		return minimum + (int)((uint)(target - scaleMinimum) *
			(uint)(maximum - minimum) / (uint)(scaleMaximum - scaleMinimum));
	}

	public static int ValueToScale<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int scaleMinimum, int scaleMaximum)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadNumericState(ref platform, state, obj,
			out var numeric)) return scaleMinimum;
		var minimum = unchecked((int)numeric.Minimum);
		var maximum = unchecked((int)numeric.Maximum);
		var value = unchecked((int)numeric.Value);
		if (scaleMaximum <= scaleMinimum || maximum <= minimum) return scaleMinimum;
		if (value < minimum) value = minimum;
		if (value > maximum) value = maximum;
		var result = scaleMinimum + (int)((uint)(value - minimum) *
			(uint)(scaleMaximum - scaleMinimum) / (uint)(maximum - minimum));
		if (numeric.Reverse != 0)
			result = scaleMaximum - (result - scaleMinimum);
		return result;
	}

	// Decimal stringification into a caller-owned buffer. Returns character count
	// (excluding the terminator) or -1 on a bounds failure.
	public static int StringifyValue<TPlatform>(ref TPlatform platform, APTR buffer,
		int bufferSize, int value) where TPlatform : struct, IMuiGuestMemory
	{
		if (buffer.IsNull || bufferSize < 2 ||
			!platform.IsMapped(buffer, (uint)bufferSize)) return -1;
		var position = 0;
		uint number;
		if (value < 0)
		{
			platform.WriteUInt8(buffer, position++, (byte)'-');
			number = ~(uint)value + 1u;
		}
		else number = (uint)value;
		uint divisor = 1;
		var digits = 1;
		while (number / divisor >= 10 && digits < 10)
		{
			divisor *= 10;
			digits++;
		}
		while (divisor >= 1 && position < bufferSize - 1)
		{
			var digit = (byte)((number / divisor) % 10);
			platform.WriteUInt8(buffer, position++, unchecked((byte)('0' + digit)));
			if (divisor == 1) break;
			divisor /= 10;
		}
		platform.WriteUInt8(buffer, position, 0);
		return position;
	}

	// Bounded printf-style formatting for MUIA_Numeric_Format. Numeric.mui's
	// documented contract is deliberately small here: one or more integer
	// conversions, optional width/zero padding, and literal text. The parser
	// reads guest memory only, never invokes a managed formatter, and caps both
	// the format scan and output buffer.
	private static int StringifyFormattedValue<TPlatform>(ref TPlatform platform,
		APTR buffer, int bufferSize, APTR format, int value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (format.IsNull) return StringifyValue(ref platform, buffer, bufferSize,
			value);
		if (buffer.IsNull || bufferSize < 2 ||
			!platform.IsMapped(buffer, (uint)bufferSize)) return -1;
		var position = 0;
		var formatPosition = 0;
		while (formatPosition < 256)
		{
			if (!platform.IsMapped(format, (uint)formatPosition + 1)) return -1;
			var ch = platform.ReadUInt8(format, formatPosition++);
			if (ch == 0) break;
			if (ch != (byte)'%')
			{
				if (!WriteFormattedByte(ref platform, buffer, bufferSize,
					ref position, ch)) return -1;
				continue;
			}
			if (!platform.IsMapped(format, (uint)formatPosition + 1)) return -1;
			ch = platform.ReadUInt8(format, formatPosition++);

			if (ch == (byte)'%')
			{
				if (!WriteFormattedByte(ref platform, buffer, bufferSize,
					ref position, (byte)'%')) return -1;
				continue;
			}
			var flags = 0;
			while (ch == (byte)'+' || ch == (byte)'0')
			{
				if (ch == (byte)'+') flags |= FormatPlus;
				if (ch == (byte)'0') flags |= FormatZero;
				if (!platform.IsMapped(format, (uint)formatPosition + 1))
					return -1;
				ch = platform.ReadUInt8(format, formatPosition++);
			}
			var width = 0;
			while (ch >= (byte)'0' && ch <= (byte)'9')
			{
				if (width < 64) width = width * 10 + ch - (byte)'0';
				if (!platform.IsMapped(format, (uint)formatPosition + 1))
					return -1;
				ch = platform.ReadUInt8(format, formatPosition++);
			}
			if (ch == (byte)'l')
			{
				if (!platform.IsMapped(format, (uint)formatPosition + 1)) return -1;
				ch = platform.ReadUInt8(format, formatPosition++);
			}
			if (ch == (byte)'u') flags |= FormatUnsigned;
			if (ch == (byte)'x' || ch == (byte)'X')
				flags |= FormatUnsigned | FormatHexadecimal;
			if (ch == (byte)'X') flags |= FormatUppercase;
			if (ch != (byte)'d' && ch != (byte)'i' && ch != (byte)'u' &&
				ch != (byte)'x' && ch != (byte)'X') return -1;
			if (!WriteFormattedInteger(ref platform, buffer, bufferSize,
				ref position, value, width, flags)) return -1;
		}
		if (formatPosition >= 256) return -1;
		if (position >= bufferSize) return -1;
		platform.WriteUInt8(buffer, position, 0);
		return position;
	}

	private static bool WriteFormattedByte<TPlatform>(ref TPlatform platform,
		APTR buffer, int bufferSize, ref int position, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (position >= bufferSize - 1) return false;
		platform.WriteUInt8(buffer, position++, value);
		return true;
	}

	private static bool WriteFormattedInteger<TPlatform>(ref TPlatform platform,
		APTR buffer, int bufferSize, ref int position, int value, int width,
		int flags) where TPlatform : struct, IMuiGuestMemory
	{
		var unsigned = (flags & FormatUnsigned) != 0;
		var negative = !unsigned && value < 0;
		var number = negative ? ~(uint)value + 1u : (uint)value;
		var radix = (flags & FormatHexadecimal) != 0 ? 16u : 10u;
		var digits = IntegerDigits(number, radix);
		var prefix = negative ? (byte)'-' :
			(!unsigned && (flags & FormatPlus) != 0 ? (byte)'+' : (byte)0);
		var padding = width - digits - (prefix == 0 ? 0 : 1);
		if (padding < 0) padding = 0;
		var zero = (flags & FormatZero) != 0;
		if (!zero && !WriteFormattedPadding(ref platform, buffer,
			bufferSize, ref position, padding, (byte)' ')) return false;
		if (prefix != 0 && !WriteFormattedByte(ref platform, buffer, bufferSize,
			ref position, prefix)) return false;
		if (zero && !WriteFormattedPadding(ref platform, buffer, bufferSize,
			ref position, padding, (byte)'0')) return false;
		if (!WriteIntegerDigits(ref platform, buffer, bufferSize, ref position,
			number, radix, digits, (flags & FormatUppercase) != 0)) return false;
		return true;
	}

	private static int IntegerDigits(uint number, uint radix)
	{
		var digits = 1;
		var divisor = 1u;
		while (number / divisor >= radix && digits < 8)
		{
			divisor *= radix;
			digits++;
		}
		return digits;
	}

	private static bool WriteFormattedPadding<TPlatform>(ref TPlatform platform,
		APTR buffer, int bufferSize, ref int position, int count, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		for (var index = 0; index < count; index++)
			if (!WriteFormattedByte(ref platform, buffer, bufferSize,
				ref position, value)) return false;
		return true;
	}

	private static bool WriteIntegerDigits<TPlatform>(ref TPlatform platform,
		APTR buffer, int bufferSize, ref int position, uint number, uint radix,
		int digits, bool uppercase) where TPlatform : struct, IMuiGuestMemory
	{
		var divisor = 1u;
		for (var index = 1; index < digits; index++) divisor *= radix;
		for (var index = 0; index < digits; index++)
		{
			var digit = (byte)((number / divisor) % radix);
			if (digit < 10) digit = unchecked((byte)('0' + digit));
			else digit = unchecked((byte)((uppercase ? 'A' : 'a') + digit - 10));
			if (!WriteFormattedByte(ref platform, buffer, bufferSize,
				ref position, digit)) return false;
			if (divisor == 1) break;
			divisor /= radix;
		}
		return true;
	}

	// Stringify the value into a guest buffer owned by the object store so it is
	// retired on disposal. Returns the buffer pointer.
	public static APTR StringifyOwned<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int value) where TPlatform : struct, IMuiHeadlessPlatform
	{
		const int StringifyCapacity = 40;
		var buffer = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StringifyKey);
		if (buffer.IsNull)
		{
			if (!MuiStoreCore.DataspaceAdd(ref platform, state, obj, StringifyKey,
				state, StringifyCapacity)) return APTR.Null;
			buffer = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringifyKey);
			if (buffer.IsNull) return APTR.Null;
		}
		else if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			StringifyKey) < StringifyCapacity &&
			!MuiStoreCore.DataspaceResize(ref platform, state, obj, StringifyKey,
				StringifyCapacity)) return APTR.Null;
		var format = APTR.Null;
		if (TryReadNumericFormatState(ref platform, state, obj,
			out var formatState)) format = formatState.Format;
		if (StringifyFormattedValue(ref platform, buffer, StringifyCapacity,
			format, value) < 0)
			StringifyValue(ref platform, buffer, StringifyCapacity, value);
		return buffer;
	}

	// Render the gauge InfoText format ("%ld"/"%%" aware) into an owned buffer,
	// substituting the current value. Returns APTR.Null when no InfoText is set
	// or the format could not be rendered within bounds.
	private static APTR GaugeInfoTextOwned<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int value) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var format = APTR.Null;
		if (TryReadGaugeInfoTextState(ref platform, state, obj,
			out var infoTextState)) format = infoTextState.InfoText;
		if (format.IsNull) return APTR.Null;
		const int Capacity = 64;
		var buffer = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			GaugeInfoRenderKey);
		if (buffer.IsNull)
		{
			if (!MuiStoreCore.DataspaceAdd(ref platform, state, obj,
				GaugeInfoRenderKey, state, Capacity)) return APTR.Null;
			buffer = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				GaugeInfoRenderKey);
			if (buffer.IsNull) return APTR.Null;
		}
		else if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			GaugeInfoRenderKey) < Capacity && !MuiStoreCore.DataspaceResize(
			ref platform, state, obj, GaugeInfoRenderKey, Capacity))
			return APTR.Null;
		return StringifyFormattedValue(ref platform, buffer, Capacity, format,
			value) < 0 ? APTR.Null : buffer;
	}

	// Build an owned all-dots buffer of the given length for MUIA_String_Secret
	// rendering, so the real contents never reach the raster.
	private static APTR SecretMaskOwned<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int length) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (length <= 0) return APTR.Null;
		var count = length > 255 ? 255 : length;
		var capacity = count + 1;
		var buffer = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StringMaskKey);
		if (buffer.IsNull)
		{
			if (!MuiStoreCore.DataspaceAdd(ref platform, state, obj, StringMaskKey,
				state, capacity)) return APTR.Null;
			buffer = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringMaskKey);
		}
		else if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			StringMaskKey) < capacity && !MuiStoreCore.DataspaceResize(ref platform,
			state, obj, StringMaskKey, capacity)) return APTR.Null;
		if (buffer.IsNull) buffer = MuiStoreCore.DataspaceFind(ref platform, state,
			obj, StringMaskKey);
		if (buffer.IsNull || !platform.IsMapped(buffer, (uint)capacity))
			return APTR.Null;
		for (var index = 0; index < count; index++)
			platform.WriteUInt8(buffer, index, (byte)'.');
		platform.WriteUInt8(buffer, count, 0);
		return buffer;
	}

	// ---- Prop movement --------------------------------------------------------

	public static bool ChangeProp<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int amount) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadAreaPresentationState(ref platform, state, obj,
			out var areaPresentation) || areaPresentation.Disabled != 0) return false;
		var scaledAmount = ScalePropAmount(amount, unchecked((int)Read(ref platform,
			state, obj, PropDeltaFactor, 1)));
		var scrollbar = Classify(ref platform, state, obj) ==
			MuiControlClass.Scrollbar;
		if (!TryReadPropRangeState(ref platform, state, obj,
			out var range)) return false;
		var entries = range.Entries;
		var visible = range.Visible;
		var first = unchecked((int)range.First);
		var last = entries > visible ? unchecked((int)(entries - visible)) : 0;
		var next = first + scaledAmount;
		if (next < 0) next = 0;
		if (next > last) next = last;
		if (next == first)
			return !scrollbar || SyncScrollbarProp(ref platform, state, obj);
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, PropFirst,
			unchecked((uint)next), true)) return false;
		range.First = unchecked((uint)next);
		if (!PublishPropRangeState(ref platform, state, obj, range)) return false;
		if (!platform.ScheduleRedraw(obj, 2)) return false;
		return !scrollbar || SyncScrollbarProp(ref platform, state, obj);
	}

	private static int ScalePropAmount(int amount, int factor)
	{
		if (amount == 0 || factor == 0) return 0;
		if (factor == -1)
			return amount == int.MinValue ? int.MaxValue : -amount;
		if (factor > 0)
		{
			if (amount > 0 && amount > int.MaxValue / factor)
				return int.MaxValue;
			if (amount < 0 && amount < int.MinValue / factor)
				return int.MinValue;
		}
		else
		{
			if (amount > 0 && amount > int.MinValue / factor)
				return int.MinValue;
			if (amount < 0 && amount < int.MaxValue / factor)
				return int.MaxValue;
		}
		return amount * factor;
	}

	// ---- Gauge clamp ----------------------------------------------------------

	public static bool SetGauge<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint current, bool notify = true, bool applyDivide = true)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadGaugeState(ref platform, state, obj,
			out var gauge)) return false;
		var effectiveCurrent = current;
		if (applyDivide && gauge.Divide != 0)
			effectiveCurrent /= gauge.Divide;
		var value = effectiveCurrent > gauge.Maximum ? gauge.Maximum :
			effectiveCurrent;
		var existing = gauge.Current;
		if (value == existing) return true;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			GaugeCurrent, value, notify)) return false;
		gauge.Current = value;
		if (!PublishGaugeState(ref platform, state, obj, gauge)) return false;
		return platform.ScheduleRedraw(obj, 2);
	}

	// ---- Bounded choices ------------------------------------------------------

	public static bool SetChoice<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint activeAttribute, APTR entries, int selection)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadAreaPresentationState(ref platform, state, obj,
			out var areaPresentation) || areaPresentation.Disabled != 0) return false;
		return SetChoiceValue(ref platform, state, obj, activeAttribute, entries,
			selection, true, true);
	}

	private static bool SetChoiceValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint activeAttribute, APTR entries, int selection,
		bool interpretSpecial, bool notify)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (entries.IsNull) return false;
		var count = CountEntries(ref platform, entries);
		if (count == 0) return false;
		if (!TryReadChoiceActiveState(ref platform, state, obj, activeAttribute,
			out var activeState)) return false;
		var currentRaw = unchecked((int)activeState.Active);
		var current = currentRaw;
		if (current < 0 || current >= count) current = 0;
		var target = selection;
		if (interpretSpecial && target == -1)
			target = current + 1 >= count ? 0 : current + 1;
		else if (interpretSpecial && target == -2)
			target = current <= 0 ? count - 1 : current - 1;
		else if (target < 0 || target >= count)
			return false;
		if (target == currentRaw) return true;
		activeState.Active = unchecked((uint)target);
		if (!PublishChoiceActiveState(ref platform, state, obj, activeAttribute,
			activeState, notify)) return false;
		return platform.ScheduleRedraw(obj, 2);
	}

	private static bool NormalizeChoiceActive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint activeAttribute, uint entriesAttribute,
		bool notify = false)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadChoiceEntriesState(ref platform, state, obj,
			entriesAttribute, out var entriesState)) return false;
		var entries = entriesState.Entries;
		var count = entries.IsNull ? 0 : CountEntries(ref platform, entries);
		if (!TryReadChoiceActiveState(ref platform, state, obj, activeAttribute,
			out var activeState)) return false;
		var current = unchecked((int)activeState.Active);
		var target = count == 0 || current < 0 || current >= count ? 0 : current;
		if (current == target) return true;
		activeState.Active = unchecked((uint)target);
		return PublishChoiceActiveState(ref platform, state, obj, activeAttribute,
			activeState, notify);
	}

	private static bool SetChoiceEntries<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var entries = APTR.FromPointer(value);
		if (!TryValidateChoiceEntries(ref platform, entries)) return false;
		var changed = !MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			attribute, out var current) || current != value;
		var stateValue = default(MuiChoiceEntriesState);
		stateValue.Entries = entries;
		if (!PublishChoiceEntriesState(ref platform, state, obj, attribute,
			stateValue, notify && changed)) return false;
		if (!NormalizeChoiceActive(ref platform, state, obj, CycleActive,
			CycleEntries, notify)) return false;
		if (!changed) return true;
		return platform.ScheduleRedraw(obj, 2);
	}

	private static bool TryValidateChoiceEntries<TPlatform>(ref TPlatform platform,
		APTR entries) where TPlatform : struct, IMuiGuestMemory
	{
		if (entries.IsNull) return true;
		var cursor = default(MuiChoiceEntryCursor);
		cursor.Base = entries;
		for (var index = 0u; index < MuiChoiceEntryCursor.MaximumEntries;
			index++)
		{
			cursor.Index = index;
			if (!MuiChoiceEntryCursorCodec.TryGetEntry(ref platform, cursor,
				out var slot) || !MuiChoiceEntryCodec.TryRead(ref platform, slot,
				out var entry)) return false;
			if (entry.Text.IsNull) return true;
		}
		return false;
	}

	internal static bool TryReadChoiceEntriesState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint entriesAttribute,
		out MuiChoiceEntriesState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (TryReadChoiceEntriesStateRecord(ref platform, state, obj,
			out var record))
		{
			var raw = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
				obj, entriesAttribute, out var rawEntries) ?
				APTR.FromPointer(rawEntries) : record.Entries;
			if (!TryValidateChoiceEntries(ref platform, raw)) return false;
			if (raw.Raw != record.Entries.Raw)
			{
				record.Entries = raw;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					ChoiceEntriesStateKey);
				if (!MuiChoiceEntriesStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			result.Entries = raw;
			return true;
		}
		result.Entries = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, entriesAttribute, out var fallback) ?
			APTR.FromPointer(fallback) : APTR.Null;
		return TryValidateChoiceEntries(ref platform, result.Entries);
	}

	private static bool TryReadChoiceEntriesStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiChoiceEntriesStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ChoiceEntriesStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ChoiceEntriesStateKey) != unchecked((int)MuiChoiceEntriesStateRecord.Size))
			return false;
		return MuiChoiceEntriesStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureChoiceEntriesStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint entriesAttribute)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadChoiceEntriesStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiChoiceEntriesStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiChoiceEntriesStateRecord.Size);
		var value = default(MuiChoiceEntriesStateRecord);
		value.Magic = MuiChoiceEntriesStateRecord.Cookie;
		value.Entries = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			entriesAttribute, 0));
		var written = MuiChoiceEntriesStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			obj, ChoiceEntriesStateKey, scratch,
			unchecked((int)MuiChoiceEntriesStateRecord.Size));
		platform.Clear(scratch, MuiChoiceEntriesStateRecord.Size);
		platform.Free(scratch, MuiChoiceEntriesStateRecord.Size);
		return added;
	}

	private static bool PublishChoiceEntriesState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint entriesAttribute,
		MuiChoiceEntriesState value, bool notify = false)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryValidateChoiceEntries(ref platform, value.Entries) ||
			!EnsureChoiceEntriesStateRecord(ref platform, state, obj,
				entriesAttribute)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ChoiceEntriesStateKey);
		var stored = default(MuiChoiceEntriesStateRecord);
		stored.Magic = MuiChoiceEntriesStateRecord.Cookie;
		stored.Entries = value.Entries;
		if (!MuiChoiceEntriesStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			entriesAttribute, stored.Entries.Raw, notify);
	}

	internal static bool TryReadChoiceActiveState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint activeAttribute,
		out MuiChoiceActiveState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (!EnsureChoiceActiveStateRecord(ref platform, state, obj,
			activeAttribute) || !TryReadChoiceActiveStateRecord(ref platform, state,
			obj, out var record)) return false;
		var raw = ReadRaw(ref platform, state, obj, activeAttribute,
			record.Active);
		if (raw != record.Active)
		{
			record.Active = raw;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				ChoiceActiveStateKey);
			if (!MuiChoiceActiveStateRecordCodec.Write(ref platform, block,
				record)) return false;
		}
		result.Active = raw;
		return true;
	}

	private static bool TryReadChoiceActiveStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiChoiceActiveStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ChoiceActiveStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ChoiceActiveStateKey) != unchecked((int)MuiChoiceActiveStateRecord.Size))
			return false;
		return MuiChoiceActiveStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureChoiceActiveStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint activeAttribute)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadChoiceActiveStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiChoiceActiveStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiChoiceActiveStateRecord.Size);
		var value = default(MuiChoiceActiveStateRecord);
		value.Magic = MuiChoiceActiveStateRecord.Cookie;
		value.Active = ReadRaw(ref platform, state, obj, activeAttribute, 0);
		var written = MuiChoiceActiveStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			obj, ChoiceActiveStateKey, scratch,
			unchecked((int)MuiChoiceActiveStateRecord.Size));
		platform.Clear(scratch, MuiChoiceActiveStateRecord.Size);
		platform.Free(scratch, MuiChoiceActiveStateRecord.Size);
		return added;
	}

	private static bool PublishChoiceActiveState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		uint activeAttribute, MuiChoiceActiveState value, bool notify = false)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureChoiceActiveStateRecord(ref platform, state, obj,
			activeAttribute)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ChoiceActiveStateKey);
		var stored = default(MuiChoiceActiveStateRecord);
		stored.Magic = MuiChoiceActiveStateRecord.Cookie;
		stored.Active = value.Active;
		if (!MuiChoiceActiveStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			activeAttribute, stored.Active, notify);
	}

	internal static bool TryGetChoiceActiveStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiChoiceActiveStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadChoiceActiveStateRecord(ref platform, state, obj, out value);

	internal static bool TryGetChoiceEntriesStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiChoiceEntriesStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadChoiceEntriesStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadTextContentsState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiTextContentsState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (TryReadTextContentsStateRecord(ref platform, state, obj,
			out var record))
		{
			var raw = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				TextContents, record.Contents.Raw));
			if (!raw.IsNull && !CStringCodec.TryReadLength(ref platform, raw,
				65536, out _)) return false;
			if (raw.Raw != record.Contents.Raw)
			{
				record.Contents = raw;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					TextContentsStateKey);
				if (!MuiTextContentsStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			result.Contents = raw;
			return true;
		}
		result.Contents = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			TextContents, 0));
		return result.Contents.IsNull || CStringCodec.TryReadLength(ref platform,
			result.Contents, 65536, out _);
	}

	private static bool TryReadTextContentsStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiTextContentsStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			TextContentsStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			TextContentsStateKey) != unchecked((int)MuiTextContentsStateRecord.Size))
			return false;
		return MuiTextContentsStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureTextContentsStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadTextContentsStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiTextContentsStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiTextContentsStateRecord.Size);
		var value = default(MuiTextContentsStateRecord);
		value.Magic = MuiTextContentsStateRecord.Cookie;
		value.Contents = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			TextContents, 0));
		var written = MuiTextContentsStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			obj, TextContentsStateKey, scratch,
			unchecked((int)MuiTextContentsStateRecord.Size));
		platform.Clear(scratch, MuiTextContentsStateRecord.Size);
		platform.Free(scratch, MuiTextContentsStateRecord.Size);
		return added;
	}

	private static bool PublishTextContentsState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiTextContentsState value, bool notify = false)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!value.Contents.IsNull && !CStringCodec.TryReadLength(ref platform,
			value.Contents, 65536, out _)) return false;
		if (!EnsureTextContentsStateRecord(ref platform, state, obj)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			TextContentsStateKey);
		var stored = default(MuiTextContentsStateRecord);
		stored.Magic = MuiTextContentsStateRecord.Cookie;
		stored.Contents = value.Contents;
		if (!MuiTextContentsStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			TextContents, stored.Contents.Raw, notify);
	}

	internal static bool TryGetTextContentsStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiTextContentsStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadTextContentsStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadTextPreParseState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiTextPreParseState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (TryReadTextPreParseStateRecord(ref platform, state, obj,
			out var record))
		{
			var raw = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				TextPreParse, record.PreParse.Raw));
			if (!raw.IsNull && !CStringCodec.TryReadLength(ref platform, raw,
				65536, out _)) return false;
			if (raw.Raw != record.PreParse.Raw)
			{
				record.PreParse = raw;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					TextPreParseStateKey);
				if (!MuiTextPreParseStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			result.PreParse = raw;
			return true;
		}
		result.PreParse = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			TextPreParse, 0));
		return result.PreParse.IsNull || CStringCodec.TryReadLength(ref platform,
			result.PreParse, 65536, out _);
	}

	private static bool TryReadTextPreParseStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiTextPreParseStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			TextPreParseStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			TextPreParseStateKey) != unchecked((int)MuiTextPreParseStateRecord.Size))
			return false;
		return MuiTextPreParseStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureTextPreParseStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadTextPreParseStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiTextPreParseStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiTextPreParseStateRecord.Size);
		var value = default(MuiTextPreParseStateRecord);
		value.Magic = MuiTextPreParseStateRecord.Cookie;
		value.PreParse = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			TextPreParse, 0));
		var written = MuiTextPreParseStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			obj, TextPreParseStateKey, scratch,
			unchecked((int)MuiTextPreParseStateRecord.Size));
		platform.Clear(scratch, MuiTextPreParseStateRecord.Size);
		platform.Free(scratch, MuiTextPreParseStateRecord.Size);
		return added;
	}

	private static bool PublishTextPreParseState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiTextPreParseState value, bool notify = false)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!value.PreParse.IsNull && !CStringCodec.TryReadLength(ref platform,
			value.PreParse, 65536, out _)) return false;
		if (!EnsureTextPreParseStateRecord(ref platform, state, obj)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			TextPreParseStateKey);
		var stored = default(MuiTextPreParseStateRecord);
		stored.Magic = MuiTextPreParseStateRecord.Cookie;
		stored.PreParse = value.PreParse;
		if (!MuiTextPreParseStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			TextPreParse, stored.PreParse.Raw, notify);
	}

	internal static bool TryGetTextPreParseStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiTextPreParseStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadTextPreParseStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadNumericFormatState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiNumericFormatState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (TryReadNumericFormatStateRecord(ref platform, state, obj,
			out var record))
		{
			var raw = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				NumericFormat, record.Format.Raw));
			if (!raw.IsNull && !CStringCodec.TryReadLength(ref platform, raw,
				256, out _)) return false;
			if (raw.Raw != record.Format.Raw)
			{
				record.Format = raw;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					NumericFormatStateKey);
				if (!MuiNumericFormatStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			result.Format = raw;
			return true;
		}
		result.Format = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			NumericFormat, 0));
		return result.Format.IsNull || CStringCodec.TryReadLength(ref platform,
			result.Format, 256, out _);
	}

	private static bool TryReadNumericFormatStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiNumericFormatStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			NumericFormatStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			NumericFormatStateKey) != unchecked((int)MuiNumericFormatStateRecord.Size))
			return false;
		return MuiNumericFormatStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureNumericFormatStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadNumericFormatStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiNumericFormatStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiNumericFormatStateRecord.Size);
		var value = default(MuiNumericFormatStateRecord);
		value.Magic = MuiNumericFormatStateRecord.Cookie;
		value.Format = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			NumericFormat, 0));
		var written = MuiNumericFormatStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			obj, NumericFormatStateKey, scratch,
			unchecked((int)MuiNumericFormatStateRecord.Size));
		platform.Clear(scratch, MuiNumericFormatStateRecord.Size);
		platform.Free(scratch, MuiNumericFormatStateRecord.Size);
		return added;
	}

	private static bool PublishNumericFormatState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiNumericFormatState value, bool notify = false)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!value.Format.IsNull && !CStringCodec.TryReadLength(ref platform,
			value.Format, 256, out _)) return false;
		if (!EnsureNumericFormatStateRecord(ref platform, state, obj)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			NumericFormatStateKey);
		var stored = default(MuiNumericFormatStateRecord);
		stored.Magic = MuiNumericFormatStateRecord.Cookie;
		stored.Format = value.Format;
		if (!MuiNumericFormatStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			NumericFormat, stored.Format.Raw, notify);
	}

	internal static bool TryGetNumericFormatStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiNumericFormatStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadNumericFormatStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadGaugeInfoTextState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiGaugeInfoTextState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (TryReadGaugeInfoTextStateRecord(ref platform, state, obj,
			out var record))
		{
			var raw = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				GaugeInfoText, record.InfoText.Raw));
			if (!raw.IsNull && !CStringCodec.TryReadLength(ref platform, raw,
				256, out _)) return false;
			if (raw.Raw != record.InfoText.Raw)
			{
				record.InfoText = raw;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					GaugeInfoTextStateKey);
				if (!MuiGaugeInfoTextStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			result.InfoText = raw;
			return true;
		}
		result.InfoText = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			GaugeInfoText, 0));
		return result.InfoText.IsNull || CStringCodec.TryReadLength(ref platform,
			result.InfoText, 256, out _);
	}

	private static bool TryReadGaugeInfoTextStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiGaugeInfoTextStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			GaugeInfoTextStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			GaugeInfoTextStateKey) != unchecked((int)MuiGaugeInfoTextStateRecord.Size))
			return false;
		return MuiGaugeInfoTextStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureGaugeInfoTextStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadGaugeInfoTextStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiGaugeInfoTextStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiGaugeInfoTextStateRecord.Size);
		var value = default(MuiGaugeInfoTextStateRecord);
		value.Magic = MuiGaugeInfoTextStateRecord.Cookie;
		value.InfoText = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			GaugeInfoText, 0));
		var written = MuiGaugeInfoTextStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			obj, GaugeInfoTextStateKey, scratch,
			unchecked((int)MuiGaugeInfoTextStateRecord.Size));
		platform.Clear(scratch, MuiGaugeInfoTextStateRecord.Size);
		platform.Free(scratch, MuiGaugeInfoTextStateRecord.Size);
		return added;
	}

	private static bool PublishGaugeInfoTextState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiGaugeInfoTextState value, bool notify = false)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!value.InfoText.IsNull && !CStringCodec.TryReadLength(ref platform,
			value.InfoText, 256, out _)) return false;
		if (!EnsureGaugeInfoTextStateRecord(ref platform, state, obj)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			GaugeInfoTextStateKey);
		var stored = default(MuiGaugeInfoTextStateRecord);
		stored.Magic = MuiGaugeInfoTextStateRecord.Cookie;
		stored.InfoText = value.InfoText;
		if (!MuiGaugeInfoTextStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			GaugeInfoText, stored.InfoText.Raw, notify);
	}

	internal static bool TryGetGaugeInfoTextStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiGaugeInfoTextStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadGaugeInfoTextStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadLevelmeterLabelState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiLevelmeterLabelState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (TryReadLevelmeterLabelStateRecord(ref platform, state, obj,
			out var record))
		{
			var raw = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				LevelmeterLabel, record.Label.Raw));
			if (!raw.IsNull && !CStringCodec.TryReadLength(ref platform, raw,
				64, out _)) return false;
			if (raw.Raw != record.Label.Raw)
			{
				record.Label = raw;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					LevelmeterLabelStateKey);
				if (!MuiLevelmeterLabelStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			result.Label = raw;
			return true;
		}
		result.Label = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			LevelmeterLabel, 0));
		return result.Label.IsNull || CStringCodec.TryReadLength(ref platform,
			result.Label, 64, out _);
	}

	private static bool TryReadLevelmeterLabelStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiLevelmeterLabelStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			LevelmeterLabelStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			LevelmeterLabelStateKey) != unchecked((int)MuiLevelmeterLabelStateRecord.Size))
			return false;
		return MuiLevelmeterLabelStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureLevelmeterLabelStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadLevelmeterLabelStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiLevelmeterLabelStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiLevelmeterLabelStateRecord.Size);
		var value = default(MuiLevelmeterLabelStateRecord);
		value.Magic = MuiLevelmeterLabelStateRecord.Cookie;
		value.Label = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			LevelmeterLabel, 0));
		var written = MuiLevelmeterLabelStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			obj, LevelmeterLabelStateKey, scratch,
			unchecked((int)MuiLevelmeterLabelStateRecord.Size));
		platform.Clear(scratch, MuiLevelmeterLabelStateRecord.Size);
		platform.Free(scratch, MuiLevelmeterLabelStateRecord.Size);
		return added;
	}

	private static bool PublishLevelmeterLabelState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiLevelmeterLabelState value, bool notify = false)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!value.Label.IsNull && !CStringCodec.TryReadLength(ref platform,
			value.Label, 64, out _)) return false;
		if (!EnsureLevelmeterLabelStateRecord(ref platform, state, obj)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			LevelmeterLabelStateKey);
		var stored = default(MuiLevelmeterLabelStateRecord);
		stored.Magic = MuiLevelmeterLabelStateRecord.Cookie;
		stored.Label = value.Label;
		if (!MuiLevelmeterLabelStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			LevelmeterLabel, stored.Label.Raw, notify);
	}

	internal static bool TryGetLevelmeterLabelStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiLevelmeterLabelStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadLevelmeterLabelStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadImageRenderState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiImageRenderState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		var imageState = ReadRaw(ref platform, state, obj, ImageState, 0);
		var selected = ReadRaw(ref platform, state, obj, Selected, 0);
		var freeHoriz = ReadRaw(ref platform, state, obj, ImageFreeHoriz, 0);
		var freeVert = ReadRaw(ref platform, state, obj, ImageFreeVert, 0);
		var showSelState = ReadRaw(ref platform, state, obj, ShowSelState, 1);
		if (TryReadImageRenderStateRecord(ref platform, state, obj,
			out var record))
		{
			if (record.ImageState != imageState || record.Selected != selected ||
				record.FreeHoriz != freeHoriz || record.FreeVert != freeVert ||
				record.ShowSelState != showSelState)
			{
				record.ImageState = imageState;
				record.Selected = selected;
				record.FreeHoriz = freeHoriz;
				record.FreeVert = freeVert;
				record.ShowSelState = showSelState;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					ImageRenderStateKey);
				if (!MuiImageRenderStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
		}
		result.ImageState = imageState;
		result.Selected = selected;
		result.FreeHoriz = freeHoriz;
		result.FreeVert = freeVert;
		result.ShowSelState = showSelState;
		return true;
	}

	private static bool TryReadImageRenderStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiImageRenderStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ImageRenderStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ImageRenderStateKey) != unchecked((int)MuiImageRenderStateRecord.Size))
			return false;
		return MuiImageRenderStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureImageRenderStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadImageRenderStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiImageRenderStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiImageRenderStateRecord.Size);
		var value = default(MuiImageRenderStateRecord);
		value.Magic = MuiImageRenderStateRecord.Cookie;
		value.ImageState = ReadRaw(ref platform, state, obj, ImageState, 0);
		value.Selected = ReadRaw(ref platform, state, obj, Selected, 0);
		value.FreeHoriz = ReadRaw(ref platform, state, obj, ImageFreeHoriz, 0);
		value.FreeVert = ReadRaw(ref platform, state, obj, ImageFreeVert, 0);
		value.ShowSelState = ReadRaw(ref platform, state, obj, ShowSelState, 1);
		var written = MuiImageRenderStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			ImageRenderStateKey, scratch,
			unchecked((int)MuiImageRenderStateRecord.Size));
		platform.Clear(scratch, MuiImageRenderStateRecord.Size);
		platform.Free(scratch, MuiImageRenderStateRecord.Size);
		return added;
	}

	private static bool PublishImageRenderState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiImageRenderState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureImageRenderStateRecord(ref platform, state, obj))
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ImageRenderStateKey);
		var stored = default(MuiImageRenderStateRecord);
		stored.Magic = MuiImageRenderStateRecord.Cookie;
		stored.ImageState = value.ImageState;
		stored.Selected = value.Selected;
		stored.FreeHoriz = value.FreeHoriz;
		stored.FreeVert = value.FreeVert;
		stored.ShowSelState = value.ShowSelState;
		if (!MuiImageRenderStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ImageState, stored.ImageState, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			Selected, stored.Selected, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ImageFreeHoriz, stored.FreeHoriz, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ImageFreeVert, stored.FreeVert, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ShowSelState, stored.ShowSelState, false);
	}

	internal static bool TryGetImageRenderStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiImageRenderStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadImageRenderStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadImageOldImageState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiImageOldImageState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (TryReadImageOldImageStateRecord(ref platform, state, obj,
			out var record))
		{
			var raw = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				ImageOldImage, record.Image.Raw));
			if (raw.Raw != record.Image.Raw)
			{
				record.Image = raw;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					ImageOldImageStateKey);
				if (!MuiImageOldImageStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			result.Image = raw;
			return true;
		}
		result.Image = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			ImageOldImage, 0));
		return true;
	}

	private static bool TryReadImageOldImageStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiImageOldImageStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ImageOldImageStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ImageOldImageStateKey) != unchecked((int)MuiImageOldImageStateRecord.Size))
			return false;
		return MuiImageOldImageStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureImageOldImageStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadImageOldImageStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiImageOldImageStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiImageOldImageStateRecord.Size);
		var value = default(MuiImageOldImageStateRecord);
		value.Magic = MuiImageOldImageStateRecord.Cookie;
		value.Image = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			ImageOldImage, 0));
		var written = MuiImageOldImageStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			obj, ImageOldImageStateKey, scratch,
			unchecked((int)MuiImageOldImageStateRecord.Size));
		platform.Clear(scratch, MuiImageOldImageStateRecord.Size);
		platform.Free(scratch, MuiImageOldImageStateRecord.Size);
		return added;
	}

	private static bool PublishImageOldImageState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiImageOldImageState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureImageOldImageStateRecord(ref platform, state, obj))
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ImageOldImageStateKey);
		var stored = default(MuiImageOldImageStateRecord);
		stored.Magic = MuiImageOldImageStateRecord.Cookie;
		stored.Image = value.Image;
		if (!MuiImageOldImageStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ImageOldImage, stored.Image.Raw, false);
	}

	internal static bool TryGetImageOldImageStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiImageOldImageStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadImageOldImageStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadImageSpecState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiImageSpecState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (TryReadImageSpecStateRecord(ref platform, state, obj,
			out var record))
		{
			var present = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
				obj, ImageSpec, out var raw);
			var presentValue = present ? 1u : 0u;
			var builtinPresent = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
				state, obj, ImageBuiltinSpec, out var builtin);
			var builtinPresentValue = builtinPresent ? 1u : 0u;
			if (presentValue != record.Present || (present && raw != record.Raw) ||
				builtinPresentValue != record.BuiltinPresent ||
				(builtinPresent && builtin != record.Builtin))
			{
				record.Present = presentValue;
				if (present) record.Raw = raw;
				record.BuiltinPresent = builtinPresentValue;
				if (builtinPresent) record.Builtin = builtin;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					ImageSpecStateKey);
				if (!MuiImageSpecStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			result.Present = present;
			result.Raw = present ? raw : record.Raw;
			result.BuiltinPresent = builtinPresent;
			result.Builtin = builtinPresent ? builtin : record.Builtin;
			return present || builtinPresent;
		}
		var hasSpec = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			ImageSpec, out var fallback);
		var hasBuiltin = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			obj, ImageBuiltinSpec, out var builtinFallback);
		if (!hasSpec && !hasBuiltin) return false;
		result.Present = hasSpec;
		result.Raw = hasSpec ? fallback : 0;
		result.BuiltinPresent = hasBuiltin;
		result.Builtin = hasBuiltin ? builtinFallback : 0;
		return true;
	}

	private static bool TryReadImageSpecStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiImageSpecStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ImageSpecStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ImageSpecStateKey) != unchecked((int)MuiImageSpecStateRecord.Size))
			return false;
		return MuiImageSpecStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureImageSpecStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadImageSpecStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiImageSpecStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiImageSpecStateRecord.Size);
		var value = default(MuiImageSpecStateRecord);
		value.Magic = MuiImageSpecStateRecord.Cookie;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			ImageSpec, out var raw))
		{
			value.Present = 1;
			value.Raw = raw;
		}
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			ImageBuiltinSpec, out var builtin))
		{
			value.BuiltinPresent = 1;
			value.Builtin = builtin;
		}
		var written = MuiImageSpecStateRecordCodec.Write(ref platform, scratch,
			value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			obj, ImageSpecStateKey, scratch,
			unchecked((int)MuiImageSpecStateRecord.Size));
		platform.Clear(scratch, MuiImageSpecStateRecord.Size);
		platform.Free(scratch, MuiImageSpecStateRecord.Size);
		return added;
	}

	private static bool PublishImageSpecState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, MuiImageSpecState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureImageSpecStateRecord(ref platform, state, obj)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ImageSpecStateKey);
		var stored = default(MuiImageSpecStateRecord);
		stored.Magic = MuiImageSpecStateRecord.Cookie;
		stored.Present = value.Present ? 1u : 0u;
		stored.Raw = value.Raw;
		stored.BuiltinPresent = value.BuiltinPresent ? 1u : 0u;
		stored.Builtin = value.Builtin;
		if (!MuiImageSpecStateRecordCodec.Write(ref platform, block, stored))
			return false;
		if (value.Present && !MuiHeadlessObjectCore.SetAttribute(ref platform,
			state, obj, ImageSpec, stored.Raw, false)) return false;
		if (value.BuiltinPresent && !MuiHeadlessObjectCore.SetAttribute(ref platform,
			state, obj, ImageBuiltinSpec, stored.Builtin, false)) return false;
		return true;
	}

	internal static bool TryGetImageSpecStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiImageSpecStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadImageSpecStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadBitmapGeometryState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiBitmapGeometryState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		var width = ReadRaw(ref platform, state, obj, BitmapWidth, 0);
		var height = ReadRaw(ref platform, state, obj, BitmapHeight, 0);
		if (TryReadBitmapGeometryStateRecord(ref platform, state, obj,
			out var record))
		{
			if (record.Width != width || record.Height != height)
			{
				record.Width = width;
				record.Height = height;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					BitmapGeometryStateKey);
				if (!MuiBitmapGeometryStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
		}
		result.Width = width;
		result.Height = height;
		return true;
	}

	private static bool TryReadBitmapGeometryStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiBitmapGeometryStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			BitmapGeometryStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			BitmapGeometryStateKey) !=
			unchecked((int)MuiBitmapGeometryStateRecord.Size)) return false;
		return MuiBitmapGeometryStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureBitmapGeometryStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadBitmapGeometryStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiBitmapGeometryStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiBitmapGeometryStateRecord.Size);
		var value = default(MuiBitmapGeometryStateRecord);
		value.Magic = MuiBitmapGeometryStateRecord.Cookie;
		value.Width = ReadRaw(ref platform, state, obj, BitmapWidth, 0);
		value.Height = ReadRaw(ref platform, state, obj, BitmapHeight, 0);
		var written = MuiBitmapGeometryStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			BitmapGeometryStateKey, scratch,
			unchecked((int)MuiBitmapGeometryStateRecord.Size));
		platform.Clear(scratch, MuiBitmapGeometryStateRecord.Size);
		platform.Free(scratch, MuiBitmapGeometryStateRecord.Size);
		return added;
	}

	private static bool PublishBitmapGeometryState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiBitmapGeometryState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureBitmapGeometryStateRecord(ref platform, state, obj))
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			BitmapGeometryStateKey);
		var stored = default(MuiBitmapGeometryStateRecord);
		stored.Magic = MuiBitmapGeometryStateRecord.Cookie;
		stored.Width = value.Width;
		stored.Height = value.Height;
		if (!MuiBitmapGeometryStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			BitmapWidth, stored.Width, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			BitmapHeight, stored.Height, false);
	}

	internal static bool TryGetBitmapGeometryStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiBitmapGeometryStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadBitmapGeometryStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadBodychunkFormatState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiBodychunkFormatState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		var compression = ReadRaw(ref platform, state, obj, BodychunkCompression, 0);
		var depth = ReadRaw(ref platform, state, obj, BodychunkDepth, 1);
		var masking = ReadRaw(ref platform, state, obj, BodychunkMasking, 0);
		if (TryReadBodychunkFormatStateRecord(ref platform, state, obj,
			out var record))
		{
			if (record.Compression != compression || record.Depth != depth ||
				record.Masking != masking)
			{
				record.Compression = compression;
				record.Depth = depth;
				record.Masking = masking;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					BodychunkFormatStateKey);
				if (!MuiBodychunkFormatStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
		}
		result.Compression = compression;
		result.Depth = depth;
		result.Masking = masking;
		return true;
	}

	private static bool TryReadBodychunkFormatStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiBodychunkFormatStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			BodychunkFormatStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			BodychunkFormatStateKey) !=
			unchecked((int)MuiBodychunkFormatStateRecord.Size)) return false;
		return MuiBodychunkFormatStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureBodychunkFormatStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadBodychunkFormatStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiBodychunkFormatStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiBodychunkFormatStateRecord.Size);
		var value = default(MuiBodychunkFormatStateRecord);
		value.Magic = MuiBodychunkFormatStateRecord.Cookie;
		value.Compression = ReadRaw(ref platform, state, obj,
			BodychunkCompression, 0);
		value.Depth = ReadRaw(ref platform, state, obj, BodychunkDepth, 1);
		value.Masking = ReadRaw(ref platform, state, obj, BodychunkMasking, 0);
		var written = MuiBodychunkFormatStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			BodychunkFormatStateKey, scratch,
			unchecked((int)MuiBodychunkFormatStateRecord.Size));
		platform.Clear(scratch, MuiBodychunkFormatStateRecord.Size);
		platform.Free(scratch, MuiBodychunkFormatStateRecord.Size);
		return added;
	}

	private static bool PublishBodychunkFormatState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiBodychunkFormatState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureBodychunkFormatStateRecord(ref platform, state, obj))
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			BodychunkFormatStateKey);
		var stored = default(MuiBodychunkFormatStateRecord);
		stored.Magic = MuiBodychunkFormatStateRecord.Cookie;
		stored.Compression = value.Compression;
		stored.Depth = value.Depth;
		stored.Masking = value.Masking;
		if (!MuiBodychunkFormatStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			BodychunkCompression, stored.Compression, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			BodychunkDepth, stored.Depth, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			BodychunkMasking, stored.Masking, false);
	}

	internal static bool TryGetBodychunkFormatStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiBodychunkFormatStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadBodychunkFormatStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadBitmapRemappedState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiBitmapRemappedState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (TryReadBitmapRemappedStateRecord(ref platform, state, obj,
			out var record))
		{
			var raw = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				BitmapRemapped, record.Remapped.Raw));
			if (raw.Raw != record.Remapped.Raw)
			{
				record.Remapped = raw;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					BitmapRemappedStateKey);
				if (!MuiBitmapRemappedStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			result.Remapped = raw;
			return true;
		}
		result.Remapped = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			BitmapRemapped, 0));
		return true;
	}

	private static bool TryReadBitmapRemappedStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiBitmapRemappedStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			BitmapRemappedStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			BitmapRemappedStateKey) != unchecked((int)
				MuiBitmapRemappedStateRecord.Size)) return false;
		return MuiBitmapRemappedStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureBitmapRemappedStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadBitmapRemappedStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiBitmapRemappedStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiBitmapRemappedStateRecord.Size);
		var value = default(MuiBitmapRemappedStateRecord);
		value.Magic = MuiBitmapRemappedStateRecord.Cookie;
		value.Remapped = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			BitmapRemapped, 0));
		var written = MuiBitmapRemappedStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			BitmapRemappedStateKey, scratch,
			unchecked((int)MuiBitmapRemappedStateRecord.Size));
		platform.Clear(scratch, MuiBitmapRemappedStateRecord.Size);
		platform.Free(scratch, MuiBitmapRemappedStateRecord.Size);
		return added;
	}

	private static bool PublishBitmapRemappedState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiBitmapRemappedState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureBitmapRemappedStateRecord(ref platform, state, obj))
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			BitmapRemappedStateKey);
		var stored = default(MuiBitmapRemappedStateRecord);
		stored.Magic = MuiBitmapRemappedStateRecord.Cookie;
		stored.Remapped = value.Remapped;
		if (!MuiBitmapRemappedStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			BitmapRemapped, stored.Remapped.Raw, false);
	}

	internal static bool TryGetBitmapRemappedStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiBitmapRemappedStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadBitmapRemappedStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadBitmapPolicyState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiBitmapPolicyState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (TryReadBitmapPolicyStateRecord(ref platform, state, obj,
			out var record))
		{
			var alpha = ReadRaw(ref platform, state, obj, BitmapAlpha,
				record.Alpha);
			var mappingTable = ReadRaw(ref platform, state, obj,
				BitmapMappingTable, record.MappingTable);
			var precision = ReadRaw(ref platform, state, obj, BitmapPrecision,
				record.Precision);
			var sourceColors = ReadRaw(ref platform, state, obj,
				BitmapSourceColors, record.SourceColors);
			var transparent = ReadRaw(ref platform, state, obj,
				BitmapTransparent, record.Transparent);
			var useFriend = ReadRaw(ref platform, state, obj, BitmapUseFriend,
				record.UseFriend);
			if (alpha != record.Alpha || mappingTable != record.MappingTable ||
				precision != record.Precision || sourceColors != record.SourceColors ||
				transparent != record.Transparent || useFriend != record.UseFriend)
			{
				record.Alpha = alpha;
				record.MappingTable = mappingTable;
				record.Precision = precision;
				record.SourceColors = sourceColors;
				record.Transparent = transparent;
				record.UseFriend = useFriend;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					BitmapPolicyStateKey);
				if (!MuiBitmapPolicyStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			result.Alpha = alpha;
			result.MappingTable = mappingTable;
			result.Precision = precision;
			result.SourceColors = sourceColors;
			result.Transparent = transparent;
			result.UseFriend = useFriend;
			return true;
		}
		result.Alpha = ReadRaw(ref platform, state, obj, BitmapAlpha, 0);
		result.MappingTable = ReadRaw(ref platform, state, obj,
			BitmapMappingTable, 0);
		result.Precision = ReadRaw(ref platform, state, obj, BitmapPrecision, 0);
		result.SourceColors = ReadRaw(ref platform, state, obj,
			BitmapSourceColors, 0);
		result.Transparent = ReadRaw(ref platform, state, obj,
			BitmapTransparent, 0);
		result.UseFriend = ReadRaw(ref platform, state, obj, BitmapUseFriend, 0);
		return true;
	}

	private static bool TryReadBitmapPolicyStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiBitmapPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			BitmapPolicyStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			BitmapPolicyStateKey) != unchecked((int)MuiBitmapPolicyStateRecord.Size))
			return false;
		return MuiBitmapPolicyStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureBitmapPolicyStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadBitmapPolicyStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiBitmapPolicyStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiBitmapPolicyStateRecord.Size);
		var value = default(MuiBitmapPolicyStateRecord);
		value.Magic = MuiBitmapPolicyStateRecord.Cookie;
		value.Alpha = ReadRaw(ref platform, state, obj, BitmapAlpha, 0);
		value.MappingTable = ReadRaw(ref platform, state, obj,
			BitmapMappingTable, 0);
		value.Precision = ReadRaw(ref platform, state, obj, BitmapPrecision, 0);
		value.SourceColors = ReadRaw(ref platform, state, obj,
			BitmapSourceColors, 0);
		value.Transparent = ReadRaw(ref platform, state, obj,
			BitmapTransparent, 0);
		value.UseFriend = ReadRaw(ref platform, state, obj, BitmapUseFriend, 0);
		var written = MuiBitmapPolicyStateRecordCodec.Write(ref platform, scratch,
			value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			BitmapPolicyStateKey, scratch,
			unchecked((int)MuiBitmapPolicyStateRecord.Size));
		platform.Clear(scratch, MuiBitmapPolicyStateRecord.Size);
		platform.Free(scratch, MuiBitmapPolicyStateRecord.Size);
		return added;
	}

	private static bool PublishBitmapPolicyState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiBitmapPolicyState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureBitmapPolicyStateRecord(ref platform, state, obj)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			BitmapPolicyStateKey);
		var stored = default(MuiBitmapPolicyStateRecord);
		stored.Magic = MuiBitmapPolicyStateRecord.Cookie;
		stored.Alpha = value.Alpha;
		stored.MappingTable = value.MappingTable;
		stored.Precision = value.Precision;
		stored.SourceColors = value.SourceColors;
		stored.Transparent = value.Transparent;
		stored.UseFriend = value.UseFriend;
		if (!MuiBitmapPolicyStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			BitmapAlpha, stored.Alpha, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			BitmapMappingTable, stored.MappingTable, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			BitmapPrecision, stored.Precision, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			BitmapSourceColors, stored.SourceColors, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			BitmapTransparent, stored.Transparent, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			BitmapUseFriend, stored.UseFriend, false);
	}

	internal static bool TryGetBitmapPolicyStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiBitmapPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadBitmapPolicyStateRecord(ref platform, state, obj, out value);

	private static uint BitmapSourceAttribute(MuiControlClass cls) =>
		cls == MuiControlClass.Bodychunk ? BodychunkBody : BitmapBitmap;

	private static APTR ReadBitmapSource<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiControlClass cls)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryReadBitmapSourceState(ref platform, state, obj, cls,
			out var source) ? source.Source : APTR.Null;
	}

	internal static bool TryReadBitmapSourceState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, MuiControlClass cls,
		out MuiBitmapSourceState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		var attribute = BitmapSourceAttribute(cls);
		if (TryReadBitmapSourceStateRecord(ref platform, state, obj,
			out var record))
		{
			var raw = APTR.FromPointer(ReadRaw(ref platform, state, obj, attribute,
				record.Source.Raw));
			if (raw.Raw != record.Source.Raw)
			{
				record.Source = raw;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					BitmapSourceStateKey);
				if (!MuiBitmapSourceStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			result.Source = raw;
			return true;
		}
		result.Source = APTR.FromPointer(ReadRaw(ref platform, state, obj, attribute,
			0));
		return true;
	}

	private static bool TryReadBitmapSourceStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiBitmapSourceStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			BitmapSourceStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			BitmapSourceStateKey) != unchecked((int)MuiBitmapSourceStateRecord.Size))
			return false;
		return MuiBitmapSourceStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureBitmapSourceStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint sourceAttribute)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadBitmapSourceStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiBitmapSourceStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiBitmapSourceStateRecord.Size);
		var value = default(MuiBitmapSourceStateRecord);
		value.Magic = MuiBitmapSourceStateRecord.Cookie;
		value.Source = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			sourceAttribute, 0));
		var written = MuiBitmapSourceStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			obj, BitmapSourceStateKey, scratch,
			unchecked((int)MuiBitmapSourceStateRecord.Size));
		platform.Clear(scratch, MuiBitmapSourceStateRecord.Size);
		platform.Free(scratch, MuiBitmapSourceStateRecord.Size);
		return added;
	}

	private static bool PublishBitmapSourceState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint sourceAttribute,
		MuiBitmapSourceState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureBitmapSourceStateRecord(ref platform, state, obj,
			sourceAttribute)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			BitmapSourceStateKey);
		var stored = default(MuiBitmapSourceStateRecord);
		stored.Magic = MuiBitmapSourceStateRecord.Cookie;
		stored.Source = value.Source;
		if (!MuiBitmapSourceStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			sourceAttribute, stored.Source.Raw, false);
	}

	internal static bool TryGetBitmapSourceStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiBitmapSourceStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadBitmapSourceStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadRectangleBarTitleState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiRectangleBarTitleState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (TryReadRectangleBarTitleStateRecord(ref platform, state, obj,
			out var record))
		{
			var present = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
				obj, RectangleBarTitle, out var raw);
			var presentValue = present ? 1u : 0u;
			if (presentValue != record.Present || (present && raw !=
				record.Title.Raw))
			{
				record.Present = presentValue;
				if (present) record.Title = APTR.FromPointer(raw);
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					RectangleBarTitleStateKey);
				if (!MuiRectangleBarTitleStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			result.Present = present;
			result.Title = present ? APTR.FromPointer(raw) : record.Title;
			return true;
		}
		result.Present = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			obj, RectangleBarTitle, out var fallback);
		result.Title = APTR.FromPointer(fallback);
		return true;
	}

	private static bool TryReadRectangleBarTitleStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiRectangleBarTitleStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			RectangleBarTitleStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			RectangleBarTitleStateKey) != unchecked((int)MuiRectangleBarTitleStateRecord.Size))
			return false;
		return MuiRectangleBarTitleStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureRectangleBarTitleStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadRectangleBarTitleStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiRectangleBarTitleStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiRectangleBarTitleStateRecord.Size);
		var value = default(MuiRectangleBarTitleStateRecord);
		value.Magic = MuiRectangleBarTitleStateRecord.Cookie;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			RectangleBarTitle, out var title))
		{
			value.Present = 1;
			value.Title = APTR.FromPointer(title);
		}
		var written = MuiRectangleBarTitleStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			obj, RectangleBarTitleStateKey, scratch,
			unchecked((int)MuiRectangleBarTitleStateRecord.Size));
		platform.Clear(scratch, MuiRectangleBarTitleStateRecord.Size);
		platform.Free(scratch, MuiRectangleBarTitleStateRecord.Size);
		return added;
	}

	private static bool PublishRectangleBarTitleState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiRectangleBarTitleState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureRectangleBarTitleStateRecord(ref platform, state, obj))
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			RectangleBarTitleStateKey);
		var stored = default(MuiRectangleBarTitleStateRecord);
		stored.Magic = MuiRectangleBarTitleStateRecord.Cookie;
		stored.Present = value.Present ? 1u : 0u;
		stored.Title = value.Title;
		if (!MuiRectangleBarTitleStateRecordCodec.Write(ref platform, block,
			stored)) return false;
		if (!value.Present) return true;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			RectangleBarTitle, stored.Title.Raw, false);
	}

	internal static bool TryGetRectangleBarTitleStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiRectangleBarTitleStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadRectangleBarTitleStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadControlFontState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiControlFontState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (TryReadControlFontStateRecord(ref platform, state, obj,
			out var record))
		{
			var present = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
				obj, Font, out var raw);
			var presentValue = present ? 1u : 0u;
			if (presentValue != record.Present || (present && raw !=
				record.Font.Raw))
			{
				record.Present = presentValue;
				if (present) record.Font = APTR.FromPointer(raw);
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					ControlFontStateKey);
				if (!MuiControlFontStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			result.Present = present;
			result.Font = present ? APTR.FromPointer(raw) : record.Font;
			return true;
		}
		result.Present = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			obj, Font, out var fallback);
		result.Font = APTR.FromPointer(fallback);
		return true;
	}

	private static bool TryReadControlFontStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiControlFontStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ControlFontStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ControlFontStateKey) != unchecked((int)MuiControlFontStateRecord.Size))
			return false;
		return MuiControlFontStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureControlFontStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadControlFontStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiControlFontStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiControlFontStateRecord.Size);
		var value = default(MuiControlFontStateRecord);
		value.Magic = MuiControlFontStateRecord.Cookie;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, Font,
			out var font))
		{
			value.Present = 1;
			value.Font = APTR.FromPointer(font);
		}
		var written = MuiControlFontStateRecordCodec.Write(ref platform, scratch,
			value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			obj, ControlFontStateKey, scratch,
			unchecked((int)MuiControlFontStateRecord.Size));
		platform.Clear(scratch, MuiControlFontStateRecord.Size);
		platform.Free(scratch, MuiControlFontStateRecord.Size);
		return added;
	}

	private static bool PublishControlFontState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiControlFontState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureControlFontStateRecord(ref platform, state, obj)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ControlFontStateKey);
		var stored = default(MuiControlFontStateRecord);
		stored.Magic = MuiControlFontStateRecord.Cookie;
		stored.Present = value.Present ? 1u : 0u;
		stored.Font = value.Font;
		if (!MuiControlFontStateRecordCodec.Write(ref platform, block, stored))
			return false;
		if (!value.Present) return true;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			Font, stored.Font.Raw, false);
	}

	internal static bool TryGetControlFontStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiControlFontStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadControlFontStateRecord(ref platform, state, obj, out value);

	internal static bool TryReadImageFontMatchState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiImageFontMatchState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (TryReadImageFontMatchStateRecord(ref platform, state, obj,
			out var record))
		{
			var match = ReadRaw(ref platform, state, obj, ImageFontMatch,
				record.Match);
			var height = ReadRaw(ref platform, state, obj, ImageFontMatchHeight,
				record.Height);
			var width = ReadRaw(ref platform, state, obj, ImageFontMatchWidth,
				record.Width);
			if (match != record.Match || height != record.Height ||
				width != record.Width)
			{
				record.Match = match;
				record.Height = height;
				record.Width = width;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					ImageFontMatchStateKey);
				if (!MuiImageFontMatchStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			result.Match = match;
			result.Height = height;
			result.Width = width;
			return true;
		}
		result.Match = ReadRaw(ref platform, state, obj, ImageFontMatch, 0);
		result.Height = ReadRaw(ref platform, state, obj, ImageFontMatchHeight,
			0);
		result.Width = ReadRaw(ref platform, state, obj, ImageFontMatchWidth, 0);
		return true;
	}

	private static bool TryReadImageFontMatchStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiImageFontMatchStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ImageFontMatchStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ImageFontMatchStateKey) != unchecked((int)
				MuiImageFontMatchStateRecord.Size)) return false;
		return MuiImageFontMatchStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureImageFontMatchStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadImageFontMatchStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiImageFontMatchStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiImageFontMatchStateRecord.Size);
		var value = default(MuiImageFontMatchStateRecord);
		value.Magic = MuiImageFontMatchStateRecord.Cookie;
		value.Match = ReadRaw(ref platform, state, obj, ImageFontMatch, 0);
		value.Height = ReadRaw(ref platform, state, obj, ImageFontMatchHeight, 0);
		value.Width = ReadRaw(ref platform, state, obj, ImageFontMatchWidth, 0);
		var written = MuiImageFontMatchStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			obj, ImageFontMatchStateKey, scratch,
			unchecked((int)MuiImageFontMatchStateRecord.Size));
		platform.Clear(scratch, MuiImageFontMatchStateRecord.Size);
		platform.Free(scratch, MuiImageFontMatchStateRecord.Size);
		return added;
	}

	private static bool PublishImageFontMatchState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiImageFontMatchState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureImageFontMatchStateRecord(ref platform, state, obj))
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ImageFontMatchStateKey);
		var stored = default(MuiImageFontMatchStateRecord);
		stored.Magic = MuiImageFontMatchStateRecord.Cookie;
		stored.Match = value.Match;
		stored.Height = value.Height;
		stored.Width = value.Width;
		if (!MuiImageFontMatchStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ImageFontMatch, stored.Match, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ImageFontMatchHeight, stored.Height, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ImageFontMatchWidth, stored.Width, false);
	}

	internal static bool TryGetImageFontMatchStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiImageFontMatchStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadImageFontMatchStateRecord(ref platform, state, obj, out value);

	private const int ImageFontMatchStringCapacity = 128;

	private static bool IsValidImageFontMatchString<TPlatform>(
		ref TPlatform platform, APTR value)
		where TPlatform : struct, IMuiGuestMemory => value.IsNull ||
		CStringCodec.TryReadLength(ref platform, value,
			ImageFontMatchStringCapacity, out _);

	internal static bool TryReadImageFontMatchStringState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiImageFontMatchStringState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (TryReadImageFontMatchStringStateRecord(ref platform, state, obj,
			out var record))
		{
			var present = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
				obj, ImageFontMatchString, out var raw);
			var pointer = APTR.FromPointer(raw);
			if (present && !IsValidImageFontMatchString(ref platform, pointer))
				return false;
			var presentValue = present ? 1u : 0u;
			if (presentValue != record.Present || (present && raw !=
				record.MatchString.Raw))
			{
				record.Present = presentValue;
				if (present) record.MatchString = pointer;
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					ImageFontMatchStringStateKey);
				if (!MuiImageFontMatchStringStateRecordCodec.Write(ref platform,
					block, record)) return false;
			}
			result.Present = present;
			result.MatchString = present ? pointer : record.MatchString;
			return true;
		}
		result.Present = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			obj, ImageFontMatchString, out var fallback);
		result.MatchString = APTR.FromPointer(fallback);
		return !result.Present || IsValidImageFontMatchString(ref platform,
			result.MatchString);
	}

	private static bool TryReadImageFontMatchStringStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiImageFontMatchStringStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ImageFontMatchStringStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ImageFontMatchStringStateKey) != unchecked((int)
				MuiImageFontMatchStringStateRecord.Size)) return false;
		return MuiImageFontMatchStringStateRecordCodec.TryRead(ref platform,
			block, out value);
	}

	private static bool EnsureImageFontMatchStringStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadImageFontMatchStringStateRecord(ref platform, state, obj,
			out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiImageFontMatchStringStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiImageFontMatchStringStateRecord.Size);
		var value = default(MuiImageFontMatchStringStateRecord);
		value.Magic = MuiImageFontMatchStringStateRecord.Cookie;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			ImageFontMatchString, out var raw))
		{
			value.Present = 1;
			value.MatchString = APTR.FromPointer(raw);
		}
		var written = MuiImageFontMatchStringStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			obj, ImageFontMatchStringStateKey, scratch,
			unchecked((int)MuiImageFontMatchStringStateRecord.Size));
		platform.Clear(scratch, MuiImageFontMatchStringStateRecord.Size);
		platform.Free(scratch, MuiImageFontMatchStringStateRecord.Size);
		return added;
	}

	private static bool PublishImageFontMatchStringState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiImageFontMatchStringState value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (value.Present && !IsValidImageFontMatchString(ref platform,
			value.MatchString)) return false;
		if (!EnsureImageFontMatchStringStateRecord(ref platform, state, obj))
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ImageFontMatchStringStateKey);
		var stored = default(MuiImageFontMatchStringStateRecord);
		stored.Magic = MuiImageFontMatchStringStateRecord.Cookie;
		stored.Present = value.Present ? 1u : 0u;
		stored.MatchString = value.MatchString;
		if (!MuiImageFontMatchStringStateRecordCodec.Write(ref platform, block,
			stored)) return false;
		if (!value.Present) return true;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ImageFontMatchString, stored.MatchString.Raw, notify);
	}

	private static bool SetImageFontMatchString<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint value, bool notify)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var matchString = APTR.FromPointer(value);
		if (!IsValidImageFontMatchString(ref platform, matchString)) return false;
		if (MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			ImageFontMatchString, out var current) && current == value) return true;
		var semantic = default(MuiImageFontMatchStringState);
		semantic.Present = true;
		semantic.MatchString = matchString;
		return PublishImageFontMatchStringState(ref platform, state, obj,
			semantic, notify);
	}

	internal static bool TryGetImageFontMatchStringStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiImageFontMatchStringStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadImageFontMatchStringStateRecord(ref platform, state, obj,
			out value);

	// ---- Generic disabled-aware keyboard interaction --------------------------

	public static uint HandleEvent<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR intuiMessage, int muiKey)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadAreaPresentationState(ref platform, state, obj,
			out var areaPresentation) || areaPresentation.Disabled != 0) return 0;
		var cls = Classify(ref platform, state, obj);
		if (cls == MuiControlClass.Image)
		{
			if ((muiKey != KeyPress && muiKey != KeyToggle) ||
				Read(ref platform, state, obj, InputMode, 0) == 0) return 0;
			var selected = ReadRaw(ref platform, state, obj, Selected, 0) == 0 ? 1u : 0u;
			if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, Selected,
				selected, true) || !MuiHeadlessObjectCore.SetAttribute(ref platform,
				state, obj, ImageState, selected, true)) return 0;
			if (!TryReadImageRenderState(ref platform, state, obj,
				out var imageRender)) return 0;
			imageRender.Selected = selected;
			imageRender.ImageState = selected;
			if (!PublishImageRenderState(ref platform, state, obj, imageRender))
				return 0;
			return platform.ScheduleRedraw(obj, 2) ? 1u : 0u;
		}
		if (cls == MuiControlClass.Text)
			return HandleTextKey(ref platform, state, obj, intuiMessage, muiKey);
		if (cls == MuiControlClass.String)
			return HandleStringKey(ref platform, state, obj, intuiMessage, muiKey);
		if (cls == MuiControlClass.Gadget)
			return HandleGadgetKey(ref platform, state, obj, muiKey);
		if (cls == MuiControlClass.Cycle)
			return HandleChoiceKey(ref platform, state, obj, CycleActive,
				CycleEntries, muiKey);
		if (cls == MuiControlClass.Radio)
			return HandleChoiceKey(ref platform, state, obj, RadioActive,
				RadioEntries, muiKey);
		if (cls == MuiControlClass.Prop || cls == MuiControlClass.Scrollbar)
		{
			var propStep = PropStep(muiKey);
			if (propStep == 0) return 0;
			return ChangeProp(ref platform, state, obj, propStep) ? 1u : 0u;
		}
		if (!IsNumericFamily(cls)) return 0;
		var step = NumericStep(ref platform, state, obj, muiKey, cls);
		if (step == 0) return 0;
		return ChangeNumeric(ref platform, state, obj, step) ? 1u : 0u;
	}

	private static uint HandleTextKey<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR intuiMessage, int muiKey)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadTextPresentationState(ref platform, state, obj,
			out var presentation)) return 0;
		var control = unchecked((byte)presentation.ControlChar);
		if (control == 0) return 0;
		var translated = muiKey == -1 ? platform.TranslateTextInput(intuiMessage) :
			(muiKey >= 32 && muiKey <= 255 ? muiKey : -1);
		if (translated < 0 || translated > 255 ||
			Lower(unchecked((byte)translated)) != Lower(control)) return 0;
		var mode = Read(ref platform, state, obj, InputMode, InputModeNone);
		if (mode == InputModeNone || mode > InputModeToggle) return 0;
		if (mode == InputModeRelVerify)
			return SetGadgetState(ref platform, state, obj,
				Read(ref platform, state, obj, Selected, 0), 1);
		if (mode == InputModeImmediate)
			return SetGadgetState(ref platform, state, obj, 1, 1);
		return SetGadgetState(ref platform, state, obj,
			Read(ref platform, state, obj, Selected, 0) == 0 ? 1u : 0u, 0);
	}

	private static uint HandleGadgetKey<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int muiKey)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadGadgetInteractionState(ref platform, state, obj,
			out var gadget)) return 0;
		var mode = gadget.InputMode;
		if (mode == InputModeNone || mode > InputModeToggle) return 0;
		var activate = muiKey == KeyPress || muiKey == KeyToggle;
		var release = muiKey == KeyUp;
		if (!activate && !release) return 0;
		var selected = gadget.Selected;
		if (mode == InputModeRelVerify)
			return SetGadgetState(ref platform, state, obj,
				release ? selected : 1, release ? 0u : 1u);
		if (mode == InputModeImmediate)
			return SetGadgetState(ref platform, state, obj,
				release ? 0u : 1u, release ? 0u : 1u);
		if (release) return 0;
		return SetGadgetState(ref platform, state, obj,
			selected == 0 ? 1u : 0u, 0);
	}

	private static uint SetGadgetState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint selected, uint pressed)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var changed = false;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, Selected,
			out var currentSelected) || currentSelected != selected)
		{
			if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, Selected,
				selected, true)) return 0;
			changed = true;
		}
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, Pressed,
			out var currentPressed) || currentPressed != pressed)
		{
			if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, Pressed,
				pressed, true)) return 0;
			changed = true;
		}
		if (Classify(ref platform, state, obj) == MuiControlClass.Gadget)
		{
			if (!TryReadGadgetInteractionState(ref platform, state, obj,
				out var gadget)) return 0;
			gadget.Selected = selected;
			gadget.Pressed = pressed;
			if (!PublishGadgetInteractionState(ref platform, state, obj,
				gadget)) return 0;
		}
		return changed && platform.ScheduleRedraw(obj, 2) ? 1u : 0u;
	}

	private static uint HandleChoiceKey<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint activeAttribute, uint entriesAttribute,
		int muiKey) where TPlatform : struct, IMuiLayoutPlatform
	{
		int selection;
		if (muiKey == KeyUp || muiKey == KeyLeft) selection = -2;
		else if (muiKey == KeyDown || muiKey == KeyRight || muiKey == KeyToggle)
			selection = -1;
		else return 0;
		var entries = APTR.FromPointer(Read(ref platform, state, obj,
			entriesAttribute, 0));
		return SetChoice(ref platform, state, obj, activeAttribute, entries,
			selection) ? 1u : 0u;
	}

	private static int PropStep(int muiKey)
	{
		if (muiKey == KeyUp || muiKey == KeyLeft) return -1;
		if (muiKey == KeyDown || muiKey == KeyRight) return 1;
		if (muiKey == KeyPageUp) return -8;
		if (muiKey == KeyPageDown) return 8;
		return 0;
	}

	private static int NumericStep<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int muiKey, MuiControlClass cls)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var vertical = muiKey == KeyUp || muiKey == KeyDown;
		var horizontal = muiKey == KeyLeft || muiKey == KeyRight;
		if (!vertical && !horizontal) return 0;
		var forward = muiKey == KeyUp || muiKey == KeyRight;
		if (!TryReadNumericState(ref platform, state, obj,
			out var numeric)) return 0;
		var reverse = numeric.Reverse != 0;
		if (vertical && Read(ref platform, state, obj, NumericRevUpDown, 0) != 0)
			reverse = !reverse;
		if (horizontal && Read(ref platform, state, obj, NumericRevLeftRight, 0) != 0)
			reverse = !reverse;
		if (reverse) forward = !forward;
		return forward ? 1 : -1;
	}

	private static uint HandleStringKey<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR intuiMessage, int muiKey)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadStringInteractionState(ref platform, state, obj,
			out var interaction) || interaction.Editable == 0) return 0;
		if (!TryReadStringContentsState(ref platform, state, obj,
			out var contentsState)) return 0;
		var contents = contentsState.Contents;
		if (contents.IsNull) return 0;
		var unicode = StringIsUnicode(ref platform, state, obj);
		var byteLength = StringLength(ref platform, contents);
		var length = StringCursorLength(ref platform, state, obj, contents);
		if (!TryReadStringCursorState(ref platform, state, obj,
			out var cursor)) return 0;
		var position = cursor.BufferPos;
		if (position < 0) position = 0;
		if (position > length)
			position = unchecked((int)ClampStringPosition(unchecked((uint)position),
				unchecked((uint)length)));
		var translated = muiKey == -1 ? platform.TranslateTextInput(intuiMessage) : -1;
		var hookResult = InvokeStringEditHook(ref platform, state, obj,
			intuiMessage, translated, position, length, byteLength,
			out var hookHandled);
		if (hookHandled) return hookResult;
		if (TryReadStringAttachedListState(ref platform, state, obj,
			out var attached) && attached.Listview.IsNotNull &&
			MuiListviewCore.TryMapInputKey(muiKey, out _) &&
			MuiListviewCore.HandleInput(ref platform, state, attached.Listview,
				intuiMessage, muiKey)) return 1;
		if (muiKey == KeyLeft) return MoveStringCursor(ref platform, state, obj,
			position > 0 ? position - 1 : position);
		if (muiKey == KeyRight) return MoveStringCursor(ref platform, state, obj,
			position < length ? position + 1 : position);
		if (muiKey == KeyHome) return MoveStringCursor(ref platform, state, obj, 0);
		if (muiKey == KeyEnd) return MoveStringCursor(ref platform, state, obj,
			length);
		if (muiKey == KeyBackspace)
		{
			if (position == 0) return 0;
			var start = StringByteOffset(ref platform, contents, position - 1,
				unicode);
			var end = StringByteOffset(ref platform, contents, position, unicode);
			var removed = end > start ? end - start : 1u;
			for (var index = unchecked((int)start);
				index <= byteLength - unchecked((int)removed); index++)
				platform.WriteUInt8(contents, index,
					platform.ReadUInt8(contents, index + unchecked((int)removed)));
			return CommitStringEdit(ref platform, state, obj, position - 1);
		}
		if (muiKey == KeyDelete)
		{
			if (position >= length) return 0;
			var start = StringByteOffset(ref platform, contents, position, unicode);
			var end = StringByteOffset(ref platform, contents, position + 1,
				unicode);
			var removed = end > start ? end - start : 1u;
			for (var index = unchecked((int)start);
				index <= byteLength - unchecked((int)removed); index++)
				platform.WriteUInt8(contents, index,
					platform.ReadUInt8(contents, index + unchecked((int)removed)));
			return CommitStringEdit(ref platform, state, obj, position);
		}
		if (muiKey == KeyPress)
		{
			if (interaction.AdvanceOnCR != 0)
				// Leave CR unclaimed so the containing cycle-chain/input platform
				// can advance focus; no host focus graph is created here.
				return 0;
			return PublishStringAcknowledge(ref platform, state, obj, contents) ?
				1u : 0u;
		}
		if (!TryEncodeUtf8Input(translated, unicode, out var encoded)) return 0;
		if (!StringAllowsCodePoint(ref platform, state, obj, encoded.CodePoint,
			unicode)) return 0;
		var maximum = Read(ref platform, state, obj, StringMaxLen, 0);
		var maxChars = maximum == 0 ? 4095 : unchecked((int)maximum) - 1;
		if (maxChars < 0 || length >= maxChars) return 0;
		var desired = byteLength + encoded.Length + 1;
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			StringCopyKey) < desired && !MuiStoreCore.DataspaceResize(ref platform,
			state, obj, StringCopyKey, desired)) return 0;
		contents = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StringCopyKey);
		if (contents.IsNull) return 0;
		var bytePosition = StringByteOffset(ref platform, contents, position,
			unicode);
		for (var index = byteLength; index >= unchecked((int)bytePosition); index--)
			platform.WriteUInt8(contents, index + encoded.Length,
				platform.ReadUInt8(contents, index));
		WriteUtf8Character(ref platform, contents, unchecked((int)bytePosition),
			encoded);
		var editedContents = default(MuiStringContentsState);
		editedContents.Contents = contents;
		if (!PublishStringContentsState(ref platform, state, obj, editedContents,
			false)) return 0;
		return CommitStringEdit(ref platform, state, obj, position + 1);
	}

	private static uint MoveStringCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int position)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadStringCursorState(ref platform, state, obj,
			out var cursor)) return 0;
		var current = cursor.BufferPos;
		if (current == position) return 0;
		cursor.BufferPos = position;
		if (!PublishStringCursorState(ref platform, state, obj, cursor,
			StringBufferPos, false)) return 0;
		EnsureStringCursorVisible(ref platform, state, obj, position);
		return platform.ScheduleRedraw(obj, 2) ? 1u : 0u;
	}

	private static bool PublishStringAcknowledge<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR contents)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var acknowledge = default(MuiStringAcknowledgeState);
		acknowledge.Contents = contents;
		if (!acknowledge.Contents.IsNull &&
			!CStringCodec.TryReadLength(ref platform, acknowledge.Contents, 4096,
				out _)) return false;
		if (!EnsureStringAcknowledgeStateRecord(ref platform, state, obj))
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StringAcknowledgeStateKey);
		var stored = default(MuiStringAcknowledgeStateRecord);
		stored.Magic = MuiStringAcknowledgeStateRecord.Cookie;
		stored.Contents = acknowledge.Contents;
		if (!MuiStringAcknowledgeStateRecordCodec.Write(ref platform, block,
			stored)) return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			StringAcknowledge, acknowledge.Contents.Raw, true);
	}

	private static uint CommitStringEdit<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int position)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadStringContentsState(ref platform, state, obj,
			out var contentsState)) return 0;
		var contents = contentsState.Contents;
		if (!PublishStringContentsState(ref platform, state, obj, contentsState,
			true)) return 0;
		SyncStringInteger(ref platform, state, obj);
		SyncStringInteger64(ref platform, state, obj);
		if (!TryReadStringCursorState(ref platform, state, obj,
			out var cursor)) return 0;
		cursor.BufferPos = position;
		if (!PublishStringCursorState(ref platform, state, obj, cursor,
			StringBufferPos, false)) return 0;
		EnsureStringCursorVisible(ref platform, state, obj, position);
		return platform.ScheduleRedraw(obj, 2) ? 1u : 0u;
	}

	// Invoke the caller-owned MUIA_String_EditHook through the native Hook ABI.
	// A2 receives the guest SGWork record and A1 receives the SGH_KEY command;
	// no managed callback wrapper or host object graph is created.  A hook that
	// returns zero falls back to MUI's private editor unless LonelyEditHook is
	// set.
	private static uint InvokeStringEditHook<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR intuiMessage, int translated, int position,
		int logicalLength, int byteLength, out bool handled)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		handled = false;
		if (!TryReadStringEditHookState(ref platform, state, obj,
			out var hookState)) return 0;
		var hook = hookState.EditHook;
		if (hook.IsNull) return 0;
		var lonely = hookState.LonelyEditHook != 0;
		if (!IsValidStringEditHook(ref platform, hook))
		{
			handled = lonely;
			return 0;
		}
		var workAddress = MuiHeadlessMemory.Allocate(ref platform,
			MuiStringEditWorkRecord.Size);
		if (workAddress.IsNull)
		{
			handled = lonely;
			return 0;
		}
		var commandAddress = MuiHeadlessMemory.Allocate(ref platform, 4);
		if (commandAddress.IsNull)
		{
			platform.Free(workAddress, MuiStringEditWorkRecord.Size);
			handled = lonely;
			return 0;
		}
		var work = default(MuiStringEditWorkRecord);
		work.Gadget = obj;
		if (!TryReadStringContentsState(ref platform, state, obj,
			out var hookContents))
		{
			platform.Free(commandAddress, 4);
			platform.Free(workAddress, MuiStringEditWorkRecord.Size);
			handled = lonely;
			return 0;
		}
		work.WorkBuffer = hookContents.Contents;
		work.PrevBuffer = work.WorkBuffer;
		work.InputEvent = intuiMessage;
		work.Code = translated < 0 || translated > ushort.MaxValue ?
			(ushort)0 : unchecked((ushort)translated);
		work.BufferPos = unchecked((short)ClampStringPosition(
			unchecked((uint)position), unchecked((uint)logicalLength)));
		work.NumChars = unchecked((short)(byteLength > short.MaxValue ?
			short.MaxValue : byteLength));
		work.Actions = MuiStringEditWorkCodec.ActionUse;
		work.LongInt = unchecked((int)ParseInteger(ref platform,
			work.WorkBuffer));
		if (!MuiStringEditWorkCodec.Write(ref platform, workAddress, work))
		{
			platform.Free(commandAddress, 4);
			platform.Free(workAddress, MuiStringEditWorkRecord.Size);
			handled = lonely;
			return 0;
		}
		platform.WriteUInt32(commandAddress, 0, MuiStringEditWorkCodec.CommandKey);
		var result = platform.InvokeHook(hook, workAddress, commandAddress);
		var read = MuiStringEditWorkCodec.TryRead(ref platform, workAddress,
			out work);
		platform.Clear(commandAddress, 4);
		platform.Free(commandAddress, 4);
		platform.Clear(workAddress, MuiStringEditWorkRecord.Size);
		platform.Free(workAddress, MuiStringEditWorkRecord.Size);
		if (result == 0)
		{
			handled = lonely;
			return 0;
		}
		handled = true;
		if (!read) return 0;
		if ((work.Actions & MuiStringEditWorkCodec.ActionUse) != 0)
		{
			if (work.WorkBuffer.IsNull || !CStringCodec.TryReadLength(ref platform,
				work.WorkBuffer, 4096, out _)) return 0;
			if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				StringContents, work.WorkBuffer.Raw, false) ||
				!CopyContents(ref platform, state, obj, StringContents, StringCopyKey,
					StringMaxChars(ref platform, state, obj), true)) return 0;
			SyncStringInteger(ref platform, state, obj);
			SyncStringInteger64(ref platform, state, obj);
			if (!TryReadStringContentsState(ref platform, state, obj,
				out var updatedHookContents)) return 0;
			work.WorkBuffer = updatedHookContents.Contents;
			logicalLength = StringCursorLength(ref platform, state, obj,
				work.WorkBuffer);
		}
		var nextPosition = work.BufferPos < 0 ? 0 : work.BufferPos;
		if (nextPosition > logicalLength) nextPosition = logicalLength;
		if (!TryReadStringCursorState(ref platform, state, obj,
			out var cursor)) return 0;
		cursor.BufferPos = nextPosition;
		if (!PublishStringCursorState(ref platform, state, obj, cursor,
			StringBufferPos, false)) return 0;
		EnsureStringCursorVisible(ref platform, state, obj, nextPosition);
		if ((work.Actions & (MuiStringEditWorkCodec.ActionUse |
			MuiStringEditWorkCodec.ActionRedisplay)) != 0)
			platform.ScheduleRedraw(obj, 2);
		return result == 0 ? 0u : 1u;
	}

	private static bool StringAllowsCodePoint<TPlatform>(ref TPlatform platform,
			APTR state, APTR obj, uint codePoint, bool unicode)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (!TryReadStringFilterState(ref platform, state, obj,
				out var filters)) return false;
			if (!unicode)
			{
				if (codePoint > 255) return false;
				if (filters.Accept.IsNotNull && !ContainsByte(ref platform, filters.Accept,
					unchecked((byte)codePoint))) return false;
				return filters.Reject.IsNull || !ContainsByte(ref platform, filters.Reject,
					unchecked((byte)codePoint));
			}
			if (filters.Accept.IsNotNull && !ContainsUtf8CodePoint(ref platform,
				filters.Accept,
				codePoint)) return false;
			return filters.Reject.IsNull || !ContainsUtf8CodePoint(ref platform,
				filters.Reject,
				codePoint);
		}

		private static bool IsValidStringFilterPointer<TPlatform>(
			ref TPlatform platform, APTR source)
			where TPlatform : struct, IMuiGuestMemory =>
			source.IsNull || CStringCodec.TryReadLength(ref platform, source,
				4096, out _);

	private static bool NormalizeStringInteractionState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			var interaction = default(MuiStringInteractionState);
			if (!TryReadStringInteractionStateRecord(ref platform, state, obj,
				out var stored)) return false;
			interaction.Editable = stored.Editable;
			interaction.AdvanceOnCR = stored.AdvanceOnCR;
			interaction.Multiline = stored.Multiline;
			return PublishStringInteractionState(ref platform, state, obj,
				interaction);
		}

		internal static bool TryReadStringInteractionState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringInteractionState result)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			result = default;
			if (TryReadStringInteractionStateRecord(ref platform, state, obj,
				out var record))
			{
				var editable = ReadRaw(ref platform, state, obj, StringEditable,
					record.Editable);
				var advance = ReadRaw(ref platform, state, obj, StringAdvanceOnCR,
					record.AdvanceOnCR);
				var multiline = ReadRaw(ref platform, state, obj, StringMultiline,
					record.Multiline);
				if (editable != record.Editable || advance != record.AdvanceOnCR ||
					multiline != record.Multiline)
				{
					record.Editable = editable;
					record.AdvanceOnCR = advance;
					record.Multiline = multiline;
					var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
						StringInteractionStateKey);
					MuiStringInteractionStateRecordCodec.Write(ref platform, block,
						record);
				}
				result.Editable = record.Editable == 0 ? 0u : 1u;
				result.AdvanceOnCR = record.AdvanceOnCR == 0 ? 0u : 1u;
				result.Multiline = record.Multiline == 0 ? 0u : 1u;
				return true;
			}
			result.Editable = ReadRaw(ref platform, state, obj, StringEditable,
				1) == 0 ? 0u : 1u;
			result.AdvanceOnCR = ReadRaw(ref platform, state, obj,
				StringAdvanceOnCR, 0) == 0 ? 0u : 1u;
			result.Multiline = ReadRaw(ref platform, state, obj, StringMultiline,
				0) == 0 ? 0u : 1u;
			return true;
		}

		private static bool TryReadStringInteractionStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringInteractionStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			value = default;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringInteractionStateKey);
			if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
				StringInteractionStateKey) !=
				unchecked((int)MuiStringInteractionStateRecord.Size)) return false;
			return MuiStringInteractionStateRecordCodec.TryRead(ref platform,
				block, out value);
		}

		private static bool EnsureStringInteractionStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (TryReadStringInteractionStateRecord(ref platform, state, obj,
				out _)) return true;
			var scratch = MuiHeadlessMemory.Allocate(ref platform,
				MuiStringInteractionStateRecord.Size);
			if (scratch.IsNull) return false;
			platform.Clear(scratch, MuiStringInteractionStateRecord.Size);
			var value = default(MuiStringInteractionStateRecord);
			value.Magic = MuiStringInteractionStateRecord.Cookie;
			value.Editable = ReadRaw(ref platform, state, obj, StringEditable, 1);
			value.AdvanceOnCR = ReadRaw(ref platform, state, obj,
				StringAdvanceOnCR, 0);
			value.Multiline = ReadRaw(ref platform, state, obj, StringMultiline, 0);
			var written = MuiStringInteractionStateRecordCodec.Write(ref platform,
				scratch, value);
			var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
				obj, StringInteractionStateKey, scratch,
				unchecked((int)MuiStringInteractionStateRecord.Size));
			platform.Clear(scratch, MuiStringInteractionStateRecord.Size);
			platform.Free(scratch, MuiStringInteractionStateRecord.Size);
			return added;
		}

		private static bool PublishStringInteractionState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			MuiStringInteractionState value)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (!EnsureStringInteractionStateRecord(ref platform, state, obj))
				return false;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringInteractionStateKey);
			var stored = default(MuiStringInteractionStateRecord);
			stored.Magic = MuiStringInteractionStateRecord.Cookie;
			stored.Editable = value.Editable == 0 ? 0u : 1u;
			stored.AdvanceOnCR = value.AdvanceOnCR == 0 ? 0u : 1u;
			stored.Multiline = value.Multiline == 0 ? 0u : 1u;
			if (!MuiStringInteractionStateRecordCodec.Write(ref platform, block,
				stored)) return false;
			return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				StringEditable, stored.Editable, false) &&
				MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					StringAdvanceOnCR, stored.AdvanceOnCR, false) &&
				MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					StringMultiline, stored.Multiline, false);
		}

		internal static bool TryGetStringInteractionStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringInteractionStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform =>
			TryReadStringInteractionStateRecord(ref platform, state, obj,
				out value);

	private static bool NormalizeStringPresentationState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			var presentation = default(MuiStringPresentationState);
			if (!TryReadStringPresentationStateRecord(ref platform, state, obj,
				out var stored)) return false;
			presentation.MaxLen = stored.MaxLen;
			presentation.Secret = stored.Secret;
			presentation.Format = stored.Format;
			presentation.Unicode = stored.Unicode;
			var format = presentation.Format;
			presentation.Format = format <= StringFormatRight ? format :
				StringFormatLeft;
			presentation.Secret = presentation.Secret == 0 ? 0u : 1u;
			presentation.Unicode = presentation.Unicode == 0 ? 0u : 1u;
			return PublishStringPresentationState(ref platform, state, obj,
				presentation);
		}

	private static bool NormalizeStringSpellCheckingState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			var spellChecking = default(MuiStringSpellCheckingState);
			if (!TryReadStringSpellCheckingStateRecord(ref platform, state, obj,
				out var stored)) return false;
			spellChecking.Enabled = stored.Enabled;
			return PublishStringSpellCheckingState(ref platform, state, obj,
				spellChecking);
		}

	private static bool IsValidStringEditHook<TPlatform>(
			ref TPlatform platform, APTR hook)
			where TPlatform : struct, IMuiGuestMemory =>
			hook.IsNull || platform.IsMapped(hook, 20);

		private static bool IsValidStringAttachedList<TPlatform>(
			ref TPlatform platform, APTR state, APTR listview)
			where TPlatform : struct, IMuiHeadlessPlatform =>
			listview.IsNull || MuiListCore.Classify(ref platform, state, listview) ==
				MuiCollectionClass.Listview;

	private static bool NormalizeStringAttachedListState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			var attached = default(MuiStringAttachedListState);
			if (!TryReadStringAttachedListState(ref platform, state, obj,
				out attached)) return false;
			return PublishStringAttachedListState(ref platform, state, obj,
				attached);
		}

		private static bool NormalizeStringEditHookState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			var hooks = default(MuiStringEditHookState);
			if (!TryReadStringEditHookState(ref platform, state, obj,
				out hooks)) return false;
			return PublishStringEditHookState(ref platform, state, obj, hooks);
		}

		internal static bool TryReadStringFilterState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringFilterState result)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			result = default;
			if (TryReadStringFilterStateRecord(ref platform, state, obj,
				out var record))
			{
				// Generic bootstrap/persistence may write either public pointer
				// directly. Validate both before updating the named pair.
				var rawAccept = APTR.FromPointer(ReadRaw(ref platform, state, obj,
					StringAccept, record.Accept.Raw));
				var rawReject = APTR.FromPointer(ReadRaw(ref platform, state, obj,
					StringReject, record.Reject.Raw));
				if (!IsValidStringFilterPointer(ref platform, rawAccept) ||
					!IsValidStringFilterPointer(ref platform, rawReject)) return false;
				if (rawAccept.Raw != record.Accept.Raw || rawReject.Raw !=
					record.Reject.Raw)
				{
					record.Accept = rawAccept;
					record.Reject = rawReject;
					var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
						StringFilterStateKey);
					if (!MuiStringFilterStateRecordCodec.Write(ref platform, block,
						record)) return false;
				}
				result.Accept = rawAccept;
				result.Reject = rawReject;
				return true;
			}
			result.Accept = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				StringAccept, 0));
			result.Reject = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				StringReject, 0));
			return IsValidStringFilterPointer(ref platform, result.Accept) &&
				IsValidStringFilterPointer(ref platform, result.Reject);
		}

		private static bool TryReadStringFilterStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringFilterStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			value = default;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringFilterStateKey);
			if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
				StringFilterStateKey) !=
				unchecked((int)MuiStringFilterStateRecord.Size)) return false;
			return MuiStringFilterStateRecordCodec.TryRead(ref platform, block,
				out value);
		}

		private static bool EnsureStringFilterStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (TryReadStringFilterStateRecord(ref platform, state, obj,
				out _)) return true;
			var scratch = MuiHeadlessMemory.Allocate(ref platform,
				MuiStringFilterStateRecord.Size);
			if (scratch.IsNull) return false;
			platform.Clear(scratch, MuiStringFilterStateRecord.Size);
			var value = default(MuiStringFilterStateRecord);
			value.Magic = MuiStringFilterStateRecord.Cookie;
			value.Accept = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				StringAccept, 0));
			value.Reject = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				StringReject, 0));
			var written = MuiStringFilterStateRecordCodec.Write(ref platform,
				scratch, value);
			var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
				obj, StringFilterStateKey, scratch,
				unchecked((int)MuiStringFilterStateRecord.Size));
			platform.Clear(scratch, MuiStringFilterStateRecord.Size);
			platform.Free(scratch, MuiStringFilterStateRecord.Size);
			return added;
		}

		private static bool PublishStringFilterState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			MuiStringFilterState value)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (!IsValidStringFilterPointer(ref platform, value.Accept) ||
				!IsValidStringFilterPointer(ref platform, value.Reject) ||
				!EnsureStringFilterStateRecord(ref platform, state, obj))
				return false;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringFilterStateKey);
			var stored = default(MuiStringFilterStateRecord);
			stored.Magic = MuiStringFilterStateRecord.Cookie;
			stored.Accept = value.Accept;
			stored.Reject = value.Reject;
			if (!MuiStringFilterStateRecordCodec.Write(ref platform, block,
				stored)) return false;
			return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				StringAccept, stored.Accept.Raw, false) &&
				MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					StringReject, stored.Reject.Raw, false);
		}

		internal static bool TryGetStringFilterStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringFilterStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform =>
			TryReadStringFilterStateRecord(ref platform, state, obj, out value);

		internal static bool TryReadStringPlaceholderState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringPlaceholderState result)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			result = default;
			if (TryReadStringPlaceholderStateRecord(ref platform, state, obj,
				out var record))
			{
				var raw = APTR.FromPointer(ReadRaw(ref platform, state, obj,
					StringPlaceholder, record.Contents.Raw));
				if (!raw.IsNull && !CStringCodec.TryReadLength(ref platform, raw,
					128, out _)) return false;
				if (raw.Raw != record.Contents.Raw)
				{
					record.Contents = raw;
					var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
						StringPlaceholderStateKey);
					if (!MuiStringPlaceholderStateRecordCodec.Write(ref platform,
						block, record)) return false;
				}
				result.Contents = raw;
				return true;
			}
			result.Contents = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				StringPlaceholder, 0));
			return result.Contents.IsNull || CStringCodec.TryReadLength(
				ref platform, result.Contents, 128, out _);
		}

		private static bool TryReadStringPlaceholderStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringPlaceholderStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			value = default;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringPlaceholderStateKey);
			if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
				StringPlaceholderStateKey) !=
				unchecked((int)MuiStringPlaceholderStateRecord.Size)) return false;
			return MuiStringPlaceholderStateRecordCodec.TryRead(ref platform,
				block, out value);
		}

		private static bool EnsureStringPlaceholderStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (TryReadStringPlaceholderStateRecord(ref platform, state, obj,
				out _)) return true;
			var scratch = MuiHeadlessMemory.Allocate(ref platform,
				MuiStringPlaceholderStateRecord.Size);
			if (scratch.IsNull) return false;
			platform.Clear(scratch, MuiStringPlaceholderStateRecord.Size);
			var value = default(MuiStringPlaceholderStateRecord);
			value.Magic = MuiStringPlaceholderStateRecord.Cookie;
			value.Contents = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				StringPlaceholder, 0));
			var written = MuiStringPlaceholderStateRecordCodec.Write(ref platform,
				scratch, value);
			var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
				obj, StringPlaceholderStateKey, scratch,
				unchecked((int)MuiStringPlaceholderStateRecord.Size));
			platform.Clear(scratch, MuiStringPlaceholderStateRecord.Size);
			platform.Free(scratch, MuiStringPlaceholderStateRecord.Size);
			return added;
		}

		private static bool PublishStringPlaceholderState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			MuiStringPlaceholderState value, bool notify)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (!value.Contents.IsNull && !CStringCodec.TryReadLength(ref platform,
				value.Contents, 128, out _)) return false;
			if (!EnsureStringPlaceholderStateRecord(ref platform, state, obj))
				return false;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringPlaceholderStateKey);
			var stored = default(MuiStringPlaceholderStateRecord);
			stored.Magic = MuiStringPlaceholderStateRecord.Cookie;
			stored.Contents = value.Contents;
			if (!MuiStringPlaceholderStateRecordCodec.Write(ref platform, block,
				stored)) return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			StringPlaceholder, stored.Contents.Raw, notify);
		}

		internal static bool TryGetStringPlaceholderStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringPlaceholderStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform =>
			TryReadStringPlaceholderStateRecord(ref platform, state, obj,
				out value);

		internal static bool TryReadStringContentsState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringContentsState result)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			result = default;
			if (TryReadStringContentsStateRecord(ref platform, state, obj,
				out var record))
			{
				var raw = APTR.FromPointer(ReadRaw(ref platform, state, obj,
					StringContents, record.Contents.Raw));
				if (!raw.IsNull && !CStringCodec.TryReadLength(ref platform, raw,
					65536, out _)) return false;
				if (raw.Raw != record.Contents.Raw)
				{
					record.Contents = raw;
					var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
						StringContentsStateKey);
					if (!MuiStringContentsStateRecordCodec.Write(ref platform, block,
						record)) return false;
				}
				result.Contents = raw;
				return true;
			}
			result.Contents = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				StringContents, 0));
			return result.Contents.IsNull || CStringCodec.TryReadLength(
				ref platform, result.Contents, 65536, out _);
		}

		private static bool TryReadStringContentsStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringContentsStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			value = default;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringContentsStateKey);
			if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
				StringContentsStateKey) !=
				unchecked((int)MuiStringContentsStateRecord.Size)) return false;
			return MuiStringContentsStateRecordCodec.TryRead(ref platform, block,
				out value);
		}

		private static bool EnsureStringContentsStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (TryReadStringContentsStateRecord(ref platform, state, obj,
				out _)) return true;
			var scratch = MuiHeadlessMemory.Allocate(ref platform,
				MuiStringContentsStateRecord.Size);
			if (scratch.IsNull) return false;
			platform.Clear(scratch, MuiStringContentsStateRecord.Size);
			var value = default(MuiStringContentsStateRecord);
			value.Magic = MuiStringContentsStateRecord.Cookie;
			value.Contents = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				StringContents, 0));
			var written = MuiStringContentsStateRecordCodec.Write(ref platform,
				scratch, value);
			var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
				obj, StringContentsStateKey, scratch,
				unchecked((int)MuiStringContentsStateRecord.Size));
			platform.Clear(scratch, MuiStringContentsStateRecord.Size);
			platform.Free(scratch, MuiStringContentsStateRecord.Size);
			return added;
		}

		private static bool PublishStringContentsState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			MuiStringContentsState value, bool notify)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (!value.Contents.IsNull && !CStringCodec.TryReadLength(ref platform,
				value.Contents, 65536, out _)) return false;
			if (!EnsureStringContentsStateRecord(ref platform, state, obj))
				return false;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringContentsStateKey);
			var stored = default(MuiStringContentsStateRecord);
			stored.Magic = MuiStringContentsStateRecord.Cookie;
			stored.Contents = value.Contents;
			if (!MuiStringContentsStateRecordCodec.Write(ref platform, block,
				stored)) return false;
			return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				StringContents, stored.Contents.Raw, notify);
		}

		internal static bool TryGetStringContentsStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringContentsStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform =>
			TryReadStringContentsStateRecord(ref platform, state, obj, out value);

		internal static bool TryReadStringPresentationState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringPresentationState result)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			result = default;
			if (TryReadStringPresentationStateRecord(ref platform, state, obj,
				out var record))
			{
				// Accept direct guest scalar writes from persistence/bootstrap code,
				// then fold them back into the named projection before consumers read
				// the policy.
				var maxLen = ReadRaw(ref platform, state, obj, StringMaxLen,
					record.MaxLen);
				var secret = ReadRaw(ref platform, state, obj, StringSecret,
					record.Secret);
				var format = ReadRaw(ref platform, state, obj, StringFormat,
					record.Format);
				var unicode = ReadRaw(ref platform, state, obj, Unicode,
					record.Unicode);
				if (maxLen != record.MaxLen || secret != record.Secret ||
					format != record.Format || unicode != record.Unicode)
				{
					record.MaxLen = maxLen;
					record.Secret = secret;
					record.Format = format;
					record.Unicode = unicode;
					var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
						StringPresentationStateKey);
					MuiStringPresentationStateRecordCodec.Write(ref platform, block,
						record);
				}
				result.MaxLen = record.MaxLen;
				result.Secret = record.Secret == 0 ? 0u : 1u;
				result.Format = record.Format;
				result.Unicode = record.Unicode == 0 ? 0u : 1u;
				return result.Format <= StringFormatRight;
			}
			result.MaxLen = ReadRaw(ref platform, state, obj, StringMaxLen, 80);
			result.Secret = ReadRaw(ref platform, state, obj, StringSecret, 0) == 0 ?
				0u : 1u;
			result.Format = ReadRaw(ref platform, state, obj, StringFormat,
				StringFormatLeft);
			result.Unicode = ReadRaw(ref platform, state, obj, Unicode, 0) == 0 ?
				0u : 1u;
			return result.Format <= StringFormatRight;
		}

		private static bool TryReadStringPresentationStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringPresentationStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			value = default;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringPresentationStateKey);
			if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
				StringPresentationStateKey) !=
				unchecked((int)MuiStringPresentationStateRecord.Size)) return false;
			return MuiStringPresentationStateRecordCodec.TryRead(ref platform,
				block, out value);
		}

		private static bool EnsureStringPresentationStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (TryReadStringPresentationStateRecord(ref platform, state, obj,
				out _)) return true;
			var scratch = MuiHeadlessMemory.Allocate(ref platform,
				MuiStringPresentationStateRecord.Size);
			if (scratch.IsNull) return false;
			platform.Clear(scratch, MuiStringPresentationStateRecord.Size);
			var value = default(MuiStringPresentationStateRecord);
			value.Magic = MuiStringPresentationStateRecord.Cookie;
			value.MaxLen = ReadRaw(ref platform, state, obj, StringMaxLen, 80);
			value.Secret = ReadRaw(ref platform, state, obj, StringSecret, 0);
			value.Format = ReadRaw(ref platform, state, obj, StringFormat,
				StringFormatLeft);
			value.Unicode = ReadRaw(ref platform, state, obj, Unicode, 0);
			var written = MuiStringPresentationStateRecordCodec.Write(ref platform,
				scratch, value);
			var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
				obj, StringPresentationStateKey, scratch,
				unchecked((int)MuiStringPresentationStateRecord.Size));
			platform.Clear(scratch, MuiStringPresentationStateRecord.Size);
			platform.Free(scratch, MuiStringPresentationStateRecord.Size);
			return added;
		}

		private static bool PublishStringPresentationState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			MuiStringPresentationState value)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (!EnsureStringPresentationStateRecord(ref platform, state, obj))
				return false;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringPresentationStateKey);
			var stored = default(MuiStringPresentationStateRecord);
			stored.Magic = MuiStringPresentationStateRecord.Cookie;
			stored.MaxLen = value.MaxLen;
			stored.Secret = value.Secret;
			stored.Format = value.Format;
			stored.Unicode = value.Unicode;
			if (!MuiStringPresentationStateRecordCodec.Write(ref platform, block,
				stored)) return false;
			return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				StringMaxLen, value.MaxLen, false) &&
				MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					StringSecret, value.Secret, false) &&
				MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					StringFormat, value.Format, false) &&
				MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					Unicode, value.Unicode, false);
		}

		internal static bool TryGetStringPresentationStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringPresentationStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform =>
			TryReadStringPresentationStateRecord(ref platform, state, obj,
				out value);

		internal static bool TryReadStringSpellCheckingState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringSpellCheckingState result)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			result = default;
			if (TryReadStringSpellCheckingStateRecord(ref platform, state, obj,
				out var record))
			{
				// Generic bootstrap and persistence paths may stage the public BOOL
				// directly. Absorb that write into the named record before returning
				// the canonical value.
				var raw = ReadRaw(ref platform, state, obj, StringSpellChecking,
					record.Enabled);
				var normalized = raw == 0 ? 0u : 1u;
				if (normalized != record.Enabled)
				{
					record.Enabled = normalized;
					var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
						StringSpellCheckingStateKey);
					if (!MuiStringSpellCheckingStateRecordCodec.Write(ref platform,
						block, record)) return false;
				}
				result.Enabled = normalized;
				if (raw != normalized && !MuiHeadlessObjectCore.SetAttribute(
					ref platform, state, obj, StringSpellChecking, normalized,
					false)) return false;
				return true;
			}
			result.Enabled = ReadRaw(ref platform, state, obj,
				StringSpellChecking, 0) == 0 ? 0u : 1u;
			return true;
		}

		private static bool TryReadStringSpellCheckingStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringSpellCheckingStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			value = default;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringSpellCheckingStateKey);
			if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
				StringSpellCheckingStateKey) !=
				unchecked((int)MuiStringSpellCheckingStateRecord.Size)) return false;
			return MuiStringSpellCheckingStateRecordCodec.TryRead(ref platform,
				block, out value);
		}

		private static bool EnsureStringSpellCheckingStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (TryReadStringSpellCheckingStateRecord(ref platform, state, obj,
				out _)) return true;
			var scratch = MuiHeadlessMemory.Allocate(ref platform,
				MuiStringSpellCheckingStateRecord.Size);
			if (scratch.IsNull) return false;
			platform.Clear(scratch, MuiStringSpellCheckingStateRecord.Size);
			var value = default(MuiStringSpellCheckingStateRecord);
			value.Magic = MuiStringSpellCheckingStateRecord.Cookie;
			value.Enabled = ReadRaw(ref platform, state, obj, StringSpellChecking, 0);
			var written = MuiStringSpellCheckingStateRecordCodec.Write(ref platform,
				scratch, value);
			var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
				obj, StringSpellCheckingStateKey, scratch,
				unchecked((int)MuiStringSpellCheckingStateRecord.Size));
			platform.Clear(scratch, MuiStringSpellCheckingStateRecord.Size);
			platform.Free(scratch, MuiStringSpellCheckingStateRecord.Size);
			return added;
		}

		private static bool PublishStringSpellCheckingState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			MuiStringSpellCheckingState value)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (!EnsureStringSpellCheckingStateRecord(ref platform, state, obj))
				return false;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringSpellCheckingStateKey);
			var stored = default(MuiStringSpellCheckingStateRecord);
			stored.Magic = MuiStringSpellCheckingStateRecord.Cookie;
			stored.Enabled = value.Enabled == 0 ? 0u : 1u;
			if (!MuiStringSpellCheckingStateRecordCodec.Write(ref platform, block,
				stored)) return false;
			return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				StringSpellChecking, stored.Enabled, false);
		}

		internal static bool TryGetStringSpellCheckingStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringSpellCheckingStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform =>
			TryReadStringSpellCheckingStateRecord(ref platform, state, obj,
				out value);

		internal static bool TryReadStringEditHookState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringEditHookState result)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			result = default;
			if (TryReadStringEditHookStateRecord(ref platform, state, obj,
				out var record))
			{
				// Generic bootstrap and persistence can stage either public field
				// directly. Validate the Hook before allowing it into the record.
				var rawHook = APTR.FromPointer(ReadRaw(ref platform, state, obj,
					StringEditHook, record.EditHook.Raw));
				if (!IsValidStringEditHook(ref platform, rawHook)) return false;
				var rawLonely = ReadRaw(ref platform, state, obj,
					StringLonelyEditHook, record.LonelyEditHook);
				var lonely = rawLonely == 0 ? 0u : 1u;
				if (rawHook.Raw != record.EditHook.Raw || lonely !=
					record.LonelyEditHook)
				{
					record.EditHook = rawHook;
					record.LonelyEditHook = lonely;
					var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
						StringEditHookStateKey);
					if (!MuiStringEditHookStateRecordCodec.Write(ref platform,
						block, record)) return false;
				}
				result.EditHook = rawHook;
				result.LonelyEditHook = lonely;
				if (rawLonely != lonely && !MuiHeadlessObjectCore.SetAttribute(
					ref platform, state, obj, StringLonelyEditHook, lonely, false))
					return false;
				return true;
			}
			result.EditHook = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				StringEditHook, 0));
			if (!IsValidStringEditHook(ref platform, result.EditHook)) return false;
			result.LonelyEditHook = ReadRaw(ref platform, state, obj,
				StringLonelyEditHook, 0) == 0 ? 0u : 1u;
			return true;
		}

		private static bool TryReadStringEditHookStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringEditHookStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			value = default;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringEditHookStateKey);
			if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
				StringEditHookStateKey) !=
				unchecked((int)MuiStringEditHookStateRecord.Size)) return false;
			return MuiStringEditHookStateRecordCodec.TryRead(ref platform, block,
				out value);
		}

		private static bool EnsureStringEditHookStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (TryReadStringEditHookStateRecord(ref platform, state, obj,
				out _)) return true;
			var scratch = MuiHeadlessMemory.Allocate(ref platform,
				MuiStringEditHookStateRecord.Size);
			if (scratch.IsNull) return false;
			platform.Clear(scratch, MuiStringEditHookStateRecord.Size);
			var value = default(MuiStringEditHookStateRecord);
			value.Magic = MuiStringEditHookStateRecord.Cookie;
			value.EditHook = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				StringEditHook, 0));
			value.LonelyEditHook = ReadRaw(ref platform, state, obj,
				StringLonelyEditHook, 0);
			var written = MuiStringEditHookStateRecordCodec.Write(ref platform,
				scratch, value);
			var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
				obj, StringEditHookStateKey, scratch,
				unchecked((int)MuiStringEditHookStateRecord.Size));
			platform.Clear(scratch, MuiStringEditHookStateRecord.Size);
			platform.Free(scratch, MuiStringEditHookStateRecord.Size);
			return added;
		}

		private static bool PublishStringEditHookState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			MuiStringEditHookState value)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (!IsValidStringEditHook(ref platform, value.EditHook) ||
				!EnsureStringEditHookStateRecord(ref platform, state, obj))
				return false;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringEditHookStateKey);
			var stored = default(MuiStringEditHookStateRecord);
			stored.Magic = MuiStringEditHookStateRecord.Cookie;
			stored.EditHook = value.EditHook;
			stored.LonelyEditHook = value.LonelyEditHook == 0 ? 0u : 1u;
			if (!MuiStringEditHookStateRecordCodec.Write(ref platform, block,
				stored)) return false;
			return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				StringEditHook, stored.EditHook.Raw, false) &&
				MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					StringLonelyEditHook, stored.LonelyEditHook, false);
		}

		internal static bool TryGetStringEditHookStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringEditHookStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform =>
			TryReadStringEditHookStateRecord(ref platform, state, obj,
				out value);

		internal static bool TryReadStringAttachedListState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringAttachedListState result)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			result = default;
			if (TryReadStringAttachedListStateRecord(ref platform, state, obj,
				out var record))
			{
				// Bootstrap/persistence may stage the public pointer directly. Keep
				// the typed relationship authoritative after validating the target.
			var raw = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				StringAttachedList, record.Listview.Raw));
				if (!IsValidStringAttachedList(ref platform, state, raw))
					return false;
				if (raw.Raw != record.Listview.Raw)
				{
					record.Listview = raw;
					var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
						StringAttachedListStateKey);
					if (!MuiStringAttachedListStateRecordCodec.Write(ref platform,
						block, record)) return false;
				}
				result.Listview = raw;
				return true;
			}
			result.Listview = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				StringAttachedList, 0));
			return IsValidStringAttachedList(ref platform, state, result.Listview);
		}

		private static bool TryReadStringAttachedListStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringAttachedListStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			value = default;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringAttachedListStateKey);
			if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
				StringAttachedListStateKey) !=
				unchecked((int)MuiStringAttachedListStateRecord.Size)) return false;
			return MuiStringAttachedListStateRecordCodec.TryRead(ref platform,
				block, out value);
		}

		private static bool EnsureStringAttachedListStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (TryReadStringAttachedListStateRecord(ref platform, state, obj,
				out _)) return true;
			var scratch = MuiHeadlessMemory.Allocate(ref platform,
				MuiStringAttachedListStateRecord.Size);
			if (scratch.IsNull) return false;
			platform.Clear(scratch, MuiStringAttachedListStateRecord.Size);
			var value = default(MuiStringAttachedListStateRecord);
			value.Magic = MuiStringAttachedListStateRecord.Cookie;
			value.Listview = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				StringAttachedList, 0));
			var written = MuiStringAttachedListStateRecordCodec.Write(ref platform,
				scratch, value);
			var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
				obj, StringAttachedListStateKey, scratch,
				unchecked((int)MuiStringAttachedListStateRecord.Size));
			platform.Clear(scratch, MuiStringAttachedListStateRecord.Size);
			platform.Free(scratch, MuiStringAttachedListStateRecord.Size);
			return added;
		}

		private static bool PublishStringAttachedListState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			MuiStringAttachedListState value)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (!IsValidStringAttachedList(ref platform, state, value.Listview) ||
				!EnsureStringAttachedListStateRecord(ref platform, state, obj))
				return false;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringAttachedListStateKey);
			var stored = default(MuiStringAttachedListStateRecord);
			stored.Magic = MuiStringAttachedListStateRecord.Cookie;
			stored.Listview = value.Listview;
			if (!MuiStringAttachedListStateRecordCodec.Write(ref platform, block,
				stored)) return false;
			return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				StringAttachedList, stored.Listview.Raw, false);
		}

		internal static bool TryGetStringAttachedListStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringAttachedListStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform =>
			TryReadStringAttachedListStateRecord(ref platform, state, obj,
				out value);

		internal static bool TryReadStringAcknowledgeState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringAcknowledgeState result)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			result = default;
			if (TryReadStringAcknowledgeStateRecord(ref platform, state, obj,
				out var record))
			{
				// Persistence/bootstrap code can still stage the public pointer
				// directly. Absorb it only after bounded guest-string validation so
				// the record never publishes an invalid caller address.
				var raw = APTR.FromPointer(ReadRaw(ref platform, state, obj,
					StringAcknowledge, record.Contents.Raw));
				if (!raw.IsNull && !CStringCodec.TryReadLength(ref platform, raw,
					4096, out _)) return false;
				if (raw.Raw != record.Contents.Raw)
				{
					record.Contents = raw;
					var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
						StringAcknowledgeStateKey);
					if (!MuiStringAcknowledgeStateRecordCodec.Write(ref platform,
						block, record)) return false;
				}
				result.Contents = raw;
				return true;
			}
			result.Contents = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				StringAcknowledge, 0));
			return result.Contents.IsNull || CStringCodec.TryReadLength(ref platform,
				result.Contents, 4096, out _);
		}

		private static bool TryReadStringAcknowledgeStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringAcknowledgeStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			value = default;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringAcknowledgeStateKey);
			if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
				StringAcknowledgeStateKey) !=
				unchecked((int)MuiStringAcknowledgeStateRecord.Size)) return false;
			return MuiStringAcknowledgeStateRecordCodec.TryRead(ref platform,
				block, out value);
		}

		private static bool EnsureStringAcknowledgeStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (TryReadStringAcknowledgeStateRecord(ref platform, state, obj,
				out _)) return true;
			var scratch = MuiHeadlessMemory.Allocate(ref platform,
				MuiStringAcknowledgeStateRecord.Size);
			if (scratch.IsNull) return false;
			platform.Clear(scratch, MuiStringAcknowledgeStateRecord.Size);
			var value = default(MuiStringAcknowledgeStateRecord);
			value.Magic = MuiStringAcknowledgeStateRecord.Cookie;
			value.Contents = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				StringAcknowledge, 0));
			var written = MuiStringAcknowledgeStateRecordCodec.Write(ref platform,
				scratch, value);
			var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
				obj, StringAcknowledgeStateKey, scratch,
				unchecked((int)MuiStringAcknowledgeStateRecord.Size));
			platform.Clear(scratch, MuiStringAcknowledgeStateRecord.Size);
			platform.Free(scratch, MuiStringAcknowledgeStateRecord.Size);
			return added;
		}

		internal static bool TryGetStringAcknowledgeStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringAcknowledgeStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform =>
			TryReadStringAcknowledgeStateRecord(ref platform, state, obj,
				out value);

		internal static bool TryReadStringCursorState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringCursorState result)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			result = default;
			if (TryReadStringCursorStateRecord(ref platform, state, obj,
				out var record))
			{
				// The generic guest attribute seam is also used by persistence and
				// bootstrap code.  If it staged a public scalar directly, absorb that
				// change into the named record before returning the canonical value.
				var rawBuffer = unchecked((int)ReadRaw(ref platform, state, obj,
					StringBufferPos, unchecked((uint)record.BufferPos)));
				var rawDisplay = unchecked((int)ReadRaw(ref platform, state, obj,
					StringDisplayPos, unchecked((uint)record.DisplayPos)));
				if (rawBuffer != record.BufferPos || rawDisplay != record.DisplayPos)
				{
					record.BufferPos = rawBuffer;
					record.DisplayPos = rawDisplay;
					var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
						StringCursorStateKey);
					MuiStringCursorStateRecordCodec.Write(ref platform, block,
						record);
				}
				result.BufferPos = record.BufferPos;
				result.DisplayPos = record.DisplayPos;
				return true;
			}
			result.BufferPos = unchecked((int)ReadRaw(ref platform, state, obj,
				StringBufferPos, 0));
			result.DisplayPos = unchecked((int)ReadRaw(ref platform, state, obj,
				StringDisplayPos, 0));
			return true;
		}

		private static bool TryReadStringCursorStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringCursorStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			value = default;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringCursorStateKey);
			if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
				StringCursorStateKey) != unchecked((int)MuiStringCursorStateRecord.Size))
				return false;
			return MuiStringCursorStateRecordCodec.TryRead(ref platform, block,
				out value);
		}

		private static bool EnsureStringCursorStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (TryReadStringCursorStateRecord(ref platform, state, obj,
				out _)) return true;
			var scratch = MuiHeadlessMemory.Allocate(ref platform,
				MuiStringCursorStateRecord.Size);
			if (scratch.IsNull) return false;
			platform.Clear(scratch, MuiStringCursorStateRecord.Size);
			var value = default(MuiStringCursorStateRecord);
			value.Magic = MuiStringCursorStateRecord.Cookie;
			value.BufferPos = unchecked((int)ReadRaw(ref platform, state, obj,
				StringBufferPos, 0));
			value.DisplayPos = unchecked((int)ReadRaw(ref platform, state, obj,
				StringDisplayPos, 0));
			var written = MuiStringCursorStateRecordCodec.Write(ref platform,
				scratch, value);
			var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
				obj, StringCursorStateKey, scratch,
				unchecked((int)MuiStringCursorStateRecord.Size));
			platform.Clear(scratch, MuiStringCursorStateRecord.Size);
			platform.Free(scratch, MuiStringCursorStateRecord.Size);
			return added;
		}

		private static bool PublishStringCursorState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			MuiStringCursorState value, uint changedAttribute, bool notify)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (!EnsureStringCursorStateRecord(ref platform, state, obj))
				return false;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringCursorStateKey);
			var stored = default(MuiStringCursorStateRecord);
			stored.Magic = MuiStringCursorStateRecord.Cookie;
			stored.BufferPos = value.BufferPos;
			stored.DisplayPos = value.DisplayPos;
			if (!MuiStringCursorStateRecordCodec.Write(ref platform, block,
				stored)) return false;
			return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				StringBufferPos, unchecked((uint)value.BufferPos),
				notify && changedAttribute == StringBufferPos) &&
				MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					StringDisplayPos, unchecked((uint)value.DisplayPos),
					notify && changedAttribute == StringDisplayPos);
		}

		internal static bool TryGetStringCursorStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringCursorStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform =>
			TryReadStringCursorStateRecord(ref platform, state, obj, out value);

		private static bool IsValidStringInteger64Pointer<TPlatform>(
			ref TPlatform platform, APTR value)
			where TPlatform : struct, IMuiGuestMemory => value.IsNull ||
			platform.IsMapped(value, MuiStringInteger64Value.Size);

		private static bool NormalizeStringInteger64State<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			var value = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				StringInteger64, 0));
			if (!IsValidStringInteger64Pointer(ref platform, value)) return false;
			if (value.IsNull) return true;
			if (!MuiStoreCore.DataspaceAdd(ref platform, state, obj,
				StringInteger64Key, value,
				unchecked((int)MuiStringInteger64Value.Size))) return false;
			var owned = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringInteger64Key);
			return owned.IsNotNull && MuiHeadlessObjectCore.SetAttribute(ref platform,
				state, obj, StringInteger64, owned.Raw, false);
		}

		internal static bool TryReadStringInteger64State<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringInteger64State result,
			out MuiStringInteger64Value value)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			result = default;
			value = default;
			result.Value = APTR.FromPointer(ReadRaw(ref platform, state, obj,
				StringInteger64, 0));
			return MuiStringInteger64Codec.TryRead(ref platform, result.Value,
				out value);
		}

		internal static bool ContainsUtf8CodePoint<TPlatform>(ref TPlatform platform,
			APTR source, uint codePoint) where TPlatform : struct, IMuiGuestMemory
		{
			if (!CStringCodec.TryReadLength(ref platform, source, 65536,
				out var length)) return false;
			var index = 0u;
			while (index < length)
			{
				if (!MuiStringscrollCore.TryReadUtf8(ref platform, source, index,
					length, out var current, out var bytes)) bytes = 1;
				if (current == codePoint) return true;
				index += bytes;
			}
			return false;
		}

		internal static bool TryEncodeUtf8Input(int translated, bool unicode,
			out MuiUtf8Character encoded)
		{
			encoded = default;
			if (translated < 32) return false;
			var codePoint = unchecked((uint)translated);
			if (!unicode)
			{
				if (codePoint > 255) return false;
				encoded.CodePoint = codePoint;
				encoded.Length = 1;
				encoded.First = unchecked((byte)codePoint);
				return true;
			}
			if (unicode && (codePoint > 0x10FFFFu ||
				(codePoint >= 0xD800u && codePoint <= 0xDFFFu))) return false;
			encoded.CodePoint = codePoint;
			if (codePoint <= 0x7Fu)
			{
				encoded.Length = 1;
				encoded.First = unchecked((byte)codePoint);
				return true;
			}
			if (codePoint <= 0x7FFu)
			{
				encoded.Length = 2;
				encoded.First = unchecked((byte)(0xC0u | (codePoint >> 6)));
				encoded.Second = unchecked((byte)(0x80u | (codePoint & 0x3Fu)));
				return true;
			}
			if (codePoint <= 0xFFFFu)
			{
				encoded.Length = 3;
				encoded.First = unchecked((byte)(0xE0u | (codePoint >> 12)));
				encoded.Second = unchecked((byte)(0x80u |
					((codePoint >> 6) & 0x3Fu)));
				encoded.Third = unchecked((byte)(0x80u | (codePoint & 0x3Fu)));
				return true;
			}
			encoded.Length = 4;
			encoded.First = unchecked((byte)(0xF0u | (codePoint >> 18)));
			encoded.Second = unchecked((byte)(0x80u |
				((codePoint >> 12) & 0x3Fu)));
			encoded.Third = unchecked((byte)(0x80u |
				((codePoint >> 6) & 0x3Fu)));
			encoded.Fourth = unchecked((byte)(0x80u | (codePoint & 0x3Fu)));
			return true;
		}

		private static void WriteUtf8Character<TPlatform>(ref TPlatform platform,
			APTR destination, int offset, MuiUtf8Character encoded)
			where TPlatform : struct, IMuiGuestMemory
		{
			platform.WriteUInt8(destination, offset, encoded.First);
			if (encoded.Length < 2) return;
			platform.WriteUInt8(destination, offset + 1, encoded.Second);
			if (encoded.Length < 3) return;
			platform.WriteUInt8(destination, offset + 2, encoded.Third);
			if (encoded.Length < 4) return;
			platform.WriteUInt8(destination, offset + 3, encoded.Fourth);
		}

		private static bool ContainsByte<TPlatform>(ref TPlatform platform,
		APTR source, byte value) where TPlatform : struct, IMuiGuestMemory
	{
		for (var index = 0; index < 256; index++)
		{
			if (!platform.IsMapped(source, (uint)index + 1)) return false;
			var current = platform.ReadUInt8(source, index);
			if (current == 0) return false;
			if (current == value) return true;
		}
		return false;
	}

	private static bool NormalizeStringCursorState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadStringContentsState(ref platform, state, obj,
			out var contentsState)) return false;
		var contents = contentsState.Contents;
		var length = StringCursorLength(ref platform, state, obj, contents);
		if (!TryReadStringCursorState(ref platform, state, obj, out var cursor))
			return false;
		cursor.BufferPos = ClampStringCursorPosition(cursor.BufferPos, length);
		cursor.DisplayPos = ClampStringCursorPosition(cursor.DisplayPos, length);
		if (!PublishStringCursorState(ref platform, state, obj, cursor, 0,
			false)) return false;
		EnsureStringCursorVisible(ref platform, state, obj, cursor.BufferPos);
		return true;
	}

	private static int ClampStringCursorPosition(int position, int length)
	{
		if (position < 0) return 0;
		if (position > length) return length;
		return position;
	}

	private static int ClampStringCursorPosition(uint position, int length)
	{
		if (position > unchecked((uint)length)) return length;
		return unchecked((int)position);
	}

	private static void ClampStringCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		NormalizeStringCursorState(ref platform, state, obj);
	}

	// String.mui 3.20 exposes the text extent and the current scroll origin so
	// applications can bind a Prop object without depending on private widget
	// state.  The implementation intentionally uses bounded byte metrics and a
	// stable 8x10 character cell; the graphics seam may use a different font,
	// but the values remain deterministic and safe before setup has occurred.
	// MUIA_String_Integer set: render the value with a signed decimal
	// conversion into the owned contents buffer (honouring MaxLen), then keep the
	// stored integer in sync with the resulting contents.
	private static bool SetStringInteger<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var maxChars = StringMaxChars(ref platform, state, obj);
		var capacity = maxChars < 0 ? 12 : maxChars + 1;
		if (capacity < 2) capacity = 2;
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj, StringCopyKey) <
			capacity && !MuiStoreCore.DataspaceResize(ref platform, state, obj,
			StringCopyKey, capacity)) return false;
		var buffer = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StringCopyKey);
		if (buffer.IsNull)
		{
			if (!MuiStoreCore.DataspaceAdd(ref platform, state, obj, StringCopyKey,
				state, capacity)) return false;
			buffer = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringCopyKey);
			if (buffer.IsNull) return false;
		}
		if (StringifyValue(ref platform, buffer, capacity,
			unchecked((int)value)) < 0) return false;
		var contents = default(MuiStringContentsState);
		contents.Contents = buffer;
		if (!PublishStringContentsState(ref platform, state, obj, contents,
			notify)) return false;
		if (!TryReadStringCursorState(ref platform, state, obj,
			out var cursor)) return false;
		cursor.BufferPos = StringCursorLength(ref platform, state, obj, buffer);
		if (!PublishStringCursorState(ref platform, state, obj, cursor,
			StringBufferPos, false)) return false;
		SyncStringInteger(ref platform, state, obj);
		SyncStringInteger64(ref platform, state, obj);
		return true;
	}

	// MUIA_String_Integer64 is a pointer to a signed QUAD.  Copy the caller's
	// record into object-owned dataspace, render its decimal value into the same
	// owned String.mui contents buffer, and keep the live pointer stable for GET.
	private static bool SetStringInteger64<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR source, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiStringInteger64Codec.TryRead(ref platform, source, out var value))
			return false;
		var maxChars = StringMaxChars(ref platform, state, obj);
		var capacity = maxChars < 0 ? 22 : maxChars + 1;
		if (capacity < 2) capacity = 2;
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj, StringCopyKey) <
			capacity && !MuiStoreCore.DataspaceResize(ref platform, state, obj,
				StringCopyKey, capacity)) return false;
		var buffer = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StringCopyKey);
		if (buffer.IsNull)
		{
			if (!MuiStoreCore.DataspaceAdd(ref platform, state, obj, StringCopyKey,
				state, capacity)) return false;
			buffer = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringCopyKey);
			if (buffer.IsNull) return false;
		}
		if (MuiStringInteger64Codec.Stringify(ref platform, buffer, capacity,
			value) < 0) return false;
		var owned = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StringInteger64Key);
		if (owned.IsNull)
		{
			if (!MuiStoreCore.DataspaceAdd(ref platform, state, obj,
				StringInteger64Key, source,
				unchecked((int)MuiStringInteger64Value.Size))) return false;
			owned = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringInteger64Key);
		}
		else if (owned.Raw != source.Raw &&
			!MuiStringInteger64Codec.Write(ref platform, owned, value)) return false;
		if (owned.IsNull || !MuiHeadlessObjectCore.SetAttribute(ref platform, state,
			obj, StringInteger64, owned.Raw, notify)) return false;
		var contents = default(MuiStringContentsState);
		contents.Contents = buffer;
		if (!PublishStringContentsState(ref platform, state, obj, contents,
			notify)) return false;
		if (!TryReadStringCursorState(ref platform, state, obj,
			out var cursor)) return false;
		cursor.BufferPos = StringCursorLength(ref platform, state, obj, buffer);
		if (!PublishStringCursorState(ref platform, state, obj, cursor,
			StringBufferPos, false)) return false;
		SyncStringInteger(ref platform, state, obj);
		SyncStringInteger64(ref platform, state, obj);
		return true;
	}

		internal static bool TryReadStringIntegerState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringIntegerState result)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			result = default;
			if (TryReadStringIntegerStateRecord(ref platform, state, obj,
				out var record))
			{
				var raw = unchecked((int)ReadRaw(ref platform, state, obj,
					StringInteger, unchecked((uint)record.Value)));
				if (raw != record.Value)
				{
					record.Value = raw;
					var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
						StringIntegerStateKey);
					if (!MuiStringIntegerStateRecordCodec.Write(ref platform, block,
						record)) return false;
				}
				result.Value = raw;
				return true;
			}
			result.Value = unchecked((int)ReadRaw(ref platform, state, obj,
				StringInteger, 0));
			return true;
		}

		private static bool TryReadStringIntegerStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringIntegerStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			value = default;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringIntegerStateKey);
			if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
				StringIntegerStateKey) !=
				unchecked((int)MuiStringIntegerStateRecord.Size)) return false;
			return MuiStringIntegerStateRecordCodec.TryRead(ref platform, block,
				out value);
		}

		private static bool EnsureStringIntegerStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (TryReadStringIntegerStateRecord(ref platform, state, obj,
				out _)) return true;
			var scratch = MuiHeadlessMemory.Allocate(ref platform,
				MuiStringIntegerStateRecord.Size);
			if (scratch.IsNull) return false;
			platform.Clear(scratch, MuiStringIntegerStateRecord.Size);
			var value = default(MuiStringIntegerStateRecord);
			value.Magic = MuiStringIntegerStateRecord.Cookie;
			value.Value = unchecked((int)ReadRaw(ref platform, state, obj,
				StringInteger, 0));
			var written = MuiStringIntegerStateRecordCodec.Write(ref platform,
				scratch, value);
			var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
				obj, StringIntegerStateKey, scratch,
				unchecked((int)MuiStringIntegerStateRecord.Size));
			platform.Clear(scratch, MuiStringIntegerStateRecord.Size);
			platform.Free(scratch, MuiStringIntegerStateRecord.Size);
			return added;
		}

		private static bool PublishStringIntegerState<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			MuiStringIntegerState value)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (!EnsureStringIntegerStateRecord(ref platform, state, obj))
				return false;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StringIntegerStateKey);
			var stored = default(MuiStringIntegerStateRecord);
			stored.Magic = MuiStringIntegerStateRecord.Cookie;
			stored.Value = value.Value;
			if (!MuiStringIntegerStateRecordCodec.Write(ref platform, block,
				stored)) return false;
			return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				StringInteger, unchecked((uint)stored.Value), false);
		}

		internal static bool TryGetStringIntegerStateRecord<TPlatform>(
			ref TPlatform platform, APTR state, APTR obj,
			out MuiStringIntegerStateRecord value)
			where TPlatform : struct, IMuiHeadlessPlatform =>
			TryReadStringIntegerStateRecord(ref platform, state, obj, out value);

	// Keep MUIA_String_Integer readable as "the contents as a number" by parsing
	// the live contents into the stored attribute after every content change.
	private static void SyncStringInteger<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadStringContentsState(ref platform, state, obj,
			out var contentsState)) return;
		var contents = contentsState.Contents;
		var integer = default(MuiStringIntegerState);
		integer.Value = unchecked((int)ParseInteger(ref platform, contents));
		PublishStringIntegerState(ref platform, state, obj, integer);
	}

	// Mirror the live String.mui contents into the caller-visible QUAD record.
	// Invalid or non-numeric text leaves the last valid record untouched, matching
	// the permissive scalar Integer behaviour while keeping the pointer safe.
	private static void SyncStringInteger64<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var target = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			StringInteger64, 0));
		if (target.IsNull || !MuiStringInteger64Codec.TryRead(ref platform, target,
			out _)) return;
		if (!TryReadStringContentsState(ref platform, state, obj,
			out var contentsState)) return;
		var contents = contentsState.Contents;
		if (!MuiStringInteger64Codec.TryParse(ref platform, contents,
			out var value)) return;
		MuiStringInteger64Codec.Write(ref platform, target, value);
	}

	// Bounded signed-decimal parse of a guest string into a longword.
	private static uint ParseInteger<TPlatform>(ref TPlatform platform, APTR source)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (source.IsNull) return 0;
		var index = 0;
		var negative = false;
		if (platform.IsMapped(source, 1))
		{
			var first = platform.ReadUInt8(source, 0);
			if (first == (byte)'-') { negative = true; index = 1; }
			else if (first == (byte)'+') index = 1;
		}
		uint result = 0;
		for (; index < 4096; index++)
		{
			if (!platform.IsMapped(source, (uint)index + 1)) break;
			var ch = platform.ReadUInt8(source, index);
			if (ch < (byte)'0' || ch > (byte)'9') break;
			result = unchecked(result * 10u + (uint)(ch - (byte)'0'));
		}
		return negative ? unchecked(~result + 1u) : result;
	}

	// ---- Practical settable / init-only / get-only enforcement ----------------

	public static bool SetControlAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiLayoutPlatform =>
		SetControlAttribute(ref platform, state, obj, attribute, value, true);

	public static bool SetControlAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (IsGetOnly(attribute) || IsInitOnly(attribute)) return false;
		var cls = Classify(ref platform, state, obj);
		if (attribute == Disabled || attribute == ShowMe ||
			attribute == Background || attribute == Frame ||
			attribute == CustomBackfill)
		{
			var storedValue = attribute == CustomBackfill && value != 0 ? 1u :
				attribute == CustomBackfill ? 0u : value;
			if (!ChangeDetectedSet(ref platform, state, obj, attribute, storedValue,
				notify)) return false;
			if (!TryReadAreaPresentationState(ref platform, state, obj,
				out var areaPresentation)) return false;
			if (attribute == Disabled) areaPresentation.Disabled = value;
			else if (attribute == ShowMe) areaPresentation.ShowMe = value;
			else if (attribute == Background) areaPresentation.Background = value;
			else if (attribute == Frame) areaPresentation.Frame = value;
			else areaPresentation.CustomBackfill = storedValue;
			return PublishAreaPresentationState(ref platform, state, obj,
				areaPresentation);
		}
		if (cls != MuiControlClass.Unknown && attribute == FillArea)
		{
			if (!ChangeDetectedSet(ref platform, state, obj, attribute, value,
				notify)) return false;
			return MuiAreaLayoutCore.TryReadRenderPolicyState(ref platform, state,
				obj, out _);
		}
		if (cls != MuiControlClass.Unknown &&
			(attribute == Draggable || attribute == Dropable))
		{
			var normalized = value == 0 ? 0u : 1u;
			if (!ChangeDetectedSet(ref platform, state, obj, attribute, normalized,
				notify)) return false;
			return MuiAreaDragCore.TryReadPolicyState(ref platform, state, obj,
				out _);
		}
		if (cls != MuiControlClass.Unknown && attribute == FrameVisible)
		{
			var normalized = value == 0 ? 0u : 1u;
			if (!ChangeDetectedSet(ref platform, state, obj, attribute, normalized,
				notify)) return false;
			return MuiAreaLayoutCore.TryReadRenderPolicyState(ref platform, state,
				obj, out _);
		}
		if (cls != MuiControlClass.Unknown && attribute == DoubleBuffer)
		{
			var normalized = value == 0 ? 0u : 1u;
			if (!ChangeDetectedSet(ref platform, state, obj, attribute, normalized,
				notify)) return false;
			// ChangeDetectedSet has synchronized the legacy raw attribute. Keep the
			// typed state boundary explicit even when the value did not change.
			return MuiAreaDoubleBufferCore.TryReadState(ref platform, state, obj,
				out _);
		}
		if (cls != MuiControlClass.Unknown && attribute == ShortHelp)
		{
			if (!ChangeDetectedSet(ref platform, state, obj, attribute, value,
				notify)) return false;
			return MuiAreaShortHelpCore.TryReadState(ref platform, state, obj,
				out _);
		}
		if (cls == MuiControlClass.Image && attribute == ImageFontMatchString)
			return SetImageFontMatchString(ref platform, state, obj, value, notify);
		if (cls == MuiControlClass.Scrollbar &&
			IsScrollbarPropAttribute(attribute))
			return SetScrollbarPropAttribute(ref platform, state, obj, attribute,
				value, notify);
		if (cls == MuiControlClass.Prop && attribute == PropSlider)
		{
			var normalized = value == 0 ? 0u : 1u;
			if (!ChangeDetectedSet(ref platform, state, obj, attribute, normalized,
				notify)) return false;
			return SyncPropPolicyField(ref platform, state, obj, attribute, normalized);
		}
		if (cls == MuiControlClass.Prop && attribute == PropDeltaFactor)
		{
			if (!ChangeDetectedSet(ref platform, state, obj, attribute, value,
				notify)) return false;
			return SyncPropPolicyField(ref platform, state, obj, attribute, value);
		}
		if (cls == MuiControlClass.Slider && attribute == SliderHoriz)
		{
			var normalized = value == 0 ? 0u : 1u;
			if (!ChangeDetectedSet(ref platform, state, obj, attribute,
				normalized, notify)) return false;
			if (!TryReadSliderPresentationState(ref platform, state, obj,
				out var presentation)) return false;
			presentation.Horizontal = normalized;
			return PublishSliderPresentationState(ref platform, state, obj,
				presentation);
		}
		if (cls == MuiControlClass.Scale && attribute == ScaleHoriz)
		{
			var normalized = value == 0 ? 0u : 1u;
			if (!ChangeDetectedSet(ref platform, state, obj, attribute,
				normalized, notify)) return false;
			if (!TryReadScalePresentationState(ref platform, state, obj,
				out var presentation)) return false;
			presentation.Horizontal = normalized;
			return PublishScalePresentationState(ref platform, state, obj,
				presentation);
		}
		if (IsNumericFamily(cls) && attribute == NumericCheckAllSizes)
			return ChangeDetectedSet(ref platform, state, obj, attribute,
				value == 0 ? 0u : 1u, notify);
		if (cls == MuiControlClass.Image && attribute == Selected)
		{
			var normalized = value == 0 ? 0u : 1u;
			if (!ChangeDetectedSet(ref platform, state, obj, Selected, normalized,
				notify)) return false;
			if (!ChangeDetectedSet(ref platform, state, obj, ImageState,
				normalized, notify)) return false;
			if (!TryReadImageRenderState(ref platform, state, obj,
				out var imageRender)) return false;
			imageRender.Selected = normalized;
			imageRender.ImageState = normalized;
			return PublishImageRenderState(ref platform, state, obj, imageRender);
		}
		if (cls == MuiControlClass.Image && attribute == ImageState)
		{
			if (!ChangeDetectedSet(ref platform, state, obj, ImageState, value,
				notify)) return false;
			if (!TryReadImageRenderState(ref platform, state, obj,
				out var imageRender)) return false;
			imageRender.ImageState = value;
			return PublishImageRenderState(ref platform, state, obj, imageRender);
		}
		if (cls == MuiControlClass.Image && attribute == ImageBuiltinSpec)
		{
			if (!ChangeDetectedSet(ref platform, state, obj, ImageBuiltinSpec,
				value, notify)) return false;
			if (!TryReadImageSpecState(ref platform, state, obj,
				out var imageSpec)) return false;
			imageSpec.BuiltinPresent = true;
			imageSpec.Builtin = value;
			return PublishImageSpecState(ref platform, state, obj, imageSpec);
		}
		if (cls == MuiControlClass.Image && attribute == ImageSpec)
		{
			if (!ChangeDetectedSet(ref platform, state, obj, ImageSpec,
				value, notify)) return false;
			if (!TryReadImageSpecState(ref platform, state, obj,
				out var imageSpec)) return false;
			imageSpec.Present = true;
			imageSpec.Raw = value;
			return PublishImageSpecState(ref platform, state, obj, imageSpec);
		}
		if (cls == MuiControlClass.Gadget && attribute == Selected)
		{
			var normalized = value == 0 ? 0u : 1u;
			if (!ChangeDetectedSet(ref platform, state, obj, Selected,
				normalized, notify)) return false;
			if (!TryReadGadgetInteractionState(ref platform, state, obj,
				out var gadget)) return false;
			gadget.Selected = normalized;
			return PublishGadgetInteractionState(ref platform, state, obj,
				gadget);
		}
		if (cls == MuiControlClass.String &&
			(attribute == MuiStringScrollAttributeCore.ScrollLeft ||
			attribute == MuiStringScrollAttributeCore.ScrollTop))
			return MuiStringScrollAttributeCore.Set(ref platform, state, obj,
				attribute, value, notify);
		if (cls == MuiControlClass.String &&
			(attribute == StringAccept || attribute == StringReject))
		{
			// MUI keeps these [ISG] pointers caller-owned.  Validate the bounded
			// guest C string before publishing it, then retain the original pointer
			// so the filter semantics remain byte/UTF-8 compatible with MorphOS.
			var filter = APTR.FromPointer(value);
			if (!IsValidStringFilterPointer(ref platform, filter)) return false;
			if (!ChangeDetectedSet(ref platform, state, obj, attribute, value,
				notify)) return false;
			if (!TryReadStringFilterState(ref platform, state, obj,
				out var filters)) return false;
			if (attribute == StringAccept)
				filters.Accept = filter;
			else
				filters.Reject = filter;
			return PublishStringFilterState(ref platform, state, obj, filters);
		}
		if (cls == MuiControlClass.String && attribute == StringAttachedList)
		{
			var listview = APTR.FromPointer(value);
			if (!IsValidStringAttachedList(ref platform, state, listview))
				return false;
			if (!ChangeDetectedSet(ref platform, state, obj, attribute, value,
				notify)) return false;
			if (!TryReadStringAttachedListState(ref platform, state, obj,
				out var attached)) return false;
			attached.Listview = listview;
			return PublishStringAttachedListState(ref platform, state, obj,
				attached);
		}
		if (cls == MuiControlClass.String && attribute == StringInteger64)
		{
			if (value == 0) return ChangeDetectedSet(ref platform, state, obj,
				attribute, 0, notify);
			if (!SetStringInteger64(ref platform, state, obj,
				APTR.FromPointer(value), notify)) return false;
			return platform.ScheduleRedraw(obj, 2);
		}
		if (cls == MuiControlClass.String && attribute == StringSpellChecking)
		{
			var normalized = value == 0 ? 0u : 1u;
			if (!ChangeDetectedSet(ref platform, state, obj, attribute,
				normalized, notify)) return false;
			if (!TryReadStringSpellCheckingState(ref platform, state, obj,
				out var spellChecking)) return false;
			spellChecking.Enabled = normalized;
			return PublishStringSpellCheckingState(ref platform, state, obj,
				spellChecking);
		}
		if (cls == MuiControlClass.String &&
			(attribute == StringEditable || attribute == StringAdvanceOnCR))
		{
			var normalized = value == 0 ? 0u : 1u;
			if (!ChangeDetectedSet(ref platform, state, obj, attribute,
				normalized, notify)) return false;
			if (!TryReadStringInteractionState(ref platform, state, obj,
				out var interaction)) return false;
			if (attribute == StringEditable)
				interaction.Editable = normalized;
			else
				interaction.AdvanceOnCR = normalized;
			return PublishStringInteractionState(ref platform, state, obj,
				interaction);
		}
		if (cls == MuiControlClass.String && attribute == StringEditHook)
		{
			var hook = APTR.FromPointer(value);
			if (!IsValidStringEditHook(ref platform, hook))
				return false;
			if (!ChangeDetectedSet(ref platform, state, obj, attribute, value,
				notify)) return false;
			if (!TryReadStringEditHookState(ref platform, state, obj,
				out var hooks)) return false;
			hooks.EditHook = hook;
			return PublishStringEditHookState(ref platform, state, obj, hooks);
		}
		if (cls == MuiControlClass.String && attribute == StringLonelyEditHook)
		{
			var normalized = value == 0 ? 0u : 1u;
			if (!ChangeDetectedSet(ref platform, state, obj, attribute,
				normalized, notify)) return false;
			if (!TryReadStringEditHookState(ref platform, state, obj,
				out var hooks)) return false;
			hooks.LonelyEditHook = normalized;
			return PublishStringEditHookState(ref platform, state, obj, hooks);
		}
		if (cls == MuiControlClass.Cycle && attribute == CycleActive)
		{
			if (!TryReadChoiceEntriesState(ref platform, state, obj, CycleEntries,
				out var entriesState)) return false;
			return SetChoiceValue(ref platform, state, obj, CycleActive,
				entriesState.Entries,
				unchecked((int)value), true, notify);
		}
		if (cls == MuiControlClass.Radio && attribute == RadioActive)
		{
			if (!TryReadChoiceEntriesState(ref platform, state, obj, RadioEntries,
				out var entriesState)) return false;
			return SetChoiceValue(ref platform, state, obj, RadioActive,
				entriesState.Entries,
				unchecked((int)value), false, notify);
		}
		if (cls == MuiControlClass.Cycle && attribute == CycleEntries)
			return SetChoiceEntries(ref platform, state, obj, attribute, value, notify);
		if (attribute == StringContents)
		{
			if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, value, false)) return false;
			if (!CopyContents(ref platform, state, obj, StringContents,
				StringCopyKey, StringMaxChars(ref platform, state, obj), notify))
				return false;
			ClampStringCursor(ref platform, state, obj);
			SyncStringInteger(ref platform, state, obj);
			SyncStringInteger64(ref platform, state, obj);
			return platform.ScheduleRedraw(obj, 2);
		}
		if (cls == MuiControlClass.String &&
			(attribute == StringBufferPos || attribute == StringDisplayPos))
		{
			if (!TryReadStringContentsState(ref platform, state, obj,
				out var contentsState)) return false;
			var contents = contentsState.Contents;
			var length = StringCursorLength(ref platform, state, obj, contents);
			if (!TryReadStringCursorState(ref platform, state, obj,
				out var cursor)) return false;
			var clamped = ClampStringCursorPosition(value, length);
			if (attribute == StringBufferPos)
				cursor.BufferPos = clamped;
			else
				cursor.DisplayPos = clamped;
			if (!PublishStringCursorState(ref platform, state, obj, cursor,
				attribute, notify)) return false;
			if (attribute == StringBufferPos)
				EnsureStringCursorVisible(ref platform, state, obj,
					cursor.BufferPos);
			return platform.ScheduleRedraw(obj, 2);
		}
		if (attribute == StringInteger)
		{
			if (!SetStringInteger(ref platform, state, obj, value, notify)) return false;
			return platform.ScheduleRedraw(obj, 2);
		}
		if (attribute == StringPlaceholder)
		{
			var source = APTR.FromPointer(value);
			if (!source.IsNull && !CStringCodec.TryReadLength(ref platform, source,
				128, out _)) return false;
			if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, value, false)) return false;
			if (!CopyContents(ref platform, state, obj, StringPlaceholder,
				StringPlaceholderKey, 128, notify)) return false;
			var placeholder = default(MuiStringPlaceholderState);
			placeholder.Contents = APTR.FromPointer(Read(ref platform, state, obj,
				StringPlaceholder, 0));
			if (!PublishStringPlaceholderState(ref platform, state, obj,
				placeholder, false)) return false;
			return platform.ScheduleRedraw(obj, 2);
		}
		if (attribute == TextContents)
		{
			if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, value, false)) return false;
			if (Read(ref platform, state, obj, TextCopy, 1) != 0 &&
				!CopyContents(ref platform, state, obj, TextContents, TextCopyKey,
					-1, notify)) return false;
			if (Read(ref platform, state, obj, TextCopy, 1) == 0)
			{
				var textContents = default(MuiTextContentsState);
				textContents.Contents = APTR.FromPointer(value);
				if (!PublishTextContentsState(ref platform, state, obj,
					textContents, notify)) return false;
			}
			return platform.ScheduleRedraw(obj, 2);
		}
		if (attribute == TextPreParse)
		{
			if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, value, false)) return false;
			if (!CopyContents(ref platform, state, obj, TextPreParse, TextPreParseKey,
				-1, notify)) return false;
			return platform.ScheduleRedraw(obj, 2);
		}
		if (attribute == TextShorten)
		{
			// MorphOS accepts only the documented Nothing/Cutoff/Hide selectors.
			// Keep malformed values out of the renderer so they cannot silently
			// acquire a different meaning as the implementation grows.
			if (value > TextShortenHide) return false;
			if (!ChangeDetectedSet(ref platform, state, obj, attribute, value,
				notify)) return false;
			if (!TryReadTextPresentationState(ref platform, state, obj,
				out var presentation)) return false;
			presentation.Shorten = value;
			return PublishTextPresentationState(ref platform, state, obj,
				presentation);
		}
		if (attribute == TextControlChar)
		{
			var normalized = value & 0xFFu;
			if (!ChangeDetectedSet(ref platform, state, obj, attribute,
				normalized, notify)) return false;
			if (!TryReadTextPresentationState(ref platform, state, obj,
				out var presentation)) return false;
			presentation.ControlChar = normalized;
			return PublishTextPresentationState(ref platform, state, obj,
				presentation);
		}
		if (attribute == NumericValue)
			return SetNumericValue(ref platform, state, obj,
				unchecked((int)value), false, notify);
		if (attribute == NumericFormat)
		{
			if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, value, false)) return false;
			if (!CopyContents(ref platform, state, obj, NumericFormat,
				NumericFormatKey, 32, notify)) return false;
			return platform.ScheduleRedraw(obj, 2);
		}
		if (attribute == LevelmeterLabel && cls == MuiControlClass.Levelmeter)
		{
			if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, value, false)) return false;
			if (!CopyContents(ref platform, state, obj, LevelmeterLabel,
				LevelmeterLabelKey, 6, notify)) return false;
			return platform.ScheduleRedraw(obj, 2);
		}
		if (attribute == NumericMin || attribute == NumericMax)
		{
			if (!TryReadNumericState(ref platform, state, obj,
				out var numeric)) return false;
			var minimum = attribute == NumericMin ? unchecked((int)value) :
				unchecked((int)numeric.Minimum);
			var maximum = attribute == NumericMax ? unchecked((int)value) :
				unchecked((int)numeric.Maximum);
			if (maximum < minimum) return false;
			if (!ChangeDetectedSet(ref platform, state, obj, attribute, value, notify))
				return false;
			if (attribute == NumericMin) numeric.Minimum = value;
			else numeric.Maximum = value;
			if (!PublishNumericState(ref platform, state, obj, numeric)) return false;
			return SetNumericValue(ref platform, state, obj, unchecked((int)Read(
				ref platform, state, obj, NumericValue, unchecked((uint)minimum))),
				false, notify);
		}
		if (attribute == GaugeCurrent)
			return SetGauge(ref platform, state, obj, value, notify);
		if (attribute == GaugeMax)
		{
			if (!ChangeDetectedSet(ref platform, state, obj, attribute, value, notify))
				return false;
			if (!TryReadGaugeState(ref platform, state, obj,
				out var gauge)) return false;
			gauge.Maximum = value;
			if (!PublishGaugeState(ref platform, state, obj, gauge)) return false;
			return SetGauge(ref platform, state, obj, Read(ref platform, state, obj,
				GaugeCurrent, 0), notify, false);
		}
		if (attribute == GaugeDivide)
		{
			if (!ChangeDetectedSet(ref platform, state, obj, attribute, value, notify))
				return false;
			if (!TryReadGaugeState(ref platform, state, obj,
				out var gauge)) return false;
			gauge.Divide = value;
			return PublishGaugeState(ref platform, state, obj, gauge);
		}
		if (attribute == GaugeInfoText)
		{
			if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, value, false)) return false;
			if (!CopyContents(ref platform, state, obj, GaugeInfoText,
				GaugeInfoTextKey, 128, notify)) return false;
			return platform.ScheduleRedraw(obj, 2);
		}
		if (cls != MuiControlClass.Unknown && attribute == Weight)
		{
			if (!ChangeDetectedSet(ref platform, state, obj, attribute, value,
				notify)) return false;
			return SyncAreaWeightState(ref platform, state, obj, value);
		}
		if (attribute == PropFirst)
		{
			var current = Read(ref platform, state, obj, PropFirst, 0);
			var delta = unchecked((int)value) - unchecked((int)current);
			return ChangePropUnconditional(ref platform, state, obj, delta, notify);
		}
		if (attribute == PropEntries || attribute == PropVisible)
		{
			if (!ChangeDetectedSet(ref platform, state, obj, attribute, value, notify))
				return false;
			return ChangePropUnconditional(ref platform, state, obj, 0, notify);
		}
		if (IsBitmapFamily(cls) && IsBitmapMutableAttribute(cls, attribute))
			return SetBitmapFamilyAttribute(ref platform, state, obj, attribute,
				value, notify);
		return ChangeDetectedSet(ref platform, state, obj, attribute, value, notify);
	}

	// Import helper for MUIM_Import. Unlike the interactive setter this method
	// intentionally has no layout/scheduling requirement: it copies the bounded
	// guest C string into the class-owned Dataspace buffer and publishes the
	// resulting pointer as the live contents attribute.
	internal static bool SetPersistenceContents<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, APTR source)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var cls = Classify(ref platform, state, obj);
		if (source.IsNull || !platform.IsMapped(source, 1)) return false;
		if (attribute == StringContents && cls == MuiControlClass.String)
		{
			if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, source.Raw, false)) return false;
			if (!CopyContents(ref platform, state, obj, StringContents,
				StringCopyKey, StringMaxChars(ref platform, state, obj), false))
				return false;
			ClampStringCursor(ref platform, state, obj);
			SyncStringInteger(ref platform, state, obj);
			SyncStringInteger64(ref platform, state, obj);
			return true;
		}
		if (attribute == TextContents && cls == MuiControlClass.Text)
		{
			if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, source.Raw, false)) return false;
			// Import must own the contents even when MUIA_Text_Copy is FALSE;
			// otherwise disposing the caller's Dataspace would leave a dangling
			// STRPTR in the Text object.
			return CopyContents(ref platform, state, obj, TextContents,
				TextCopyKey, -1, false);
		}
		return false;
	}

	// Scalar import helper for Area-derived image/gadget controls. The public
	// MUIA_Selected value is mirrored into ImageState for image-backed gadgets;
	// keeping that normalization here avoids requiring the interactive layout
	// platform or scheduling a redraw during a load operation.
	internal static bool SetPersistenceScalar<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var cls = Classify(ref platform, state, obj);
		if (attribute == Disabled || attribute == ShowMe ||
			attribute == Background || attribute == Frame)
		{
			if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, value, false)) return false;
			if (!TryReadAreaPresentationState(ref platform, state, obj,
				out var areaPresentation)) return false;
			if (attribute == Disabled) areaPresentation.Disabled = value;
			else if (attribute == ShowMe) areaPresentation.ShowMe = value;
			else if (attribute == Background) areaPresentation.Background = value;
			else areaPresentation.Frame = value;
			return PublishAreaPresentationState(ref platform, state, obj,
				areaPresentation);
		}
		if (attribute == Selected && cls == MuiControlClass.Image)
		{
			var normalized = value == 0 ? 0u : 1u;
			if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				Selected, normalized, false)) return false;
			if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				ImageState, normalized, false)) return false;
			if (!TryReadImageRenderState(ref platform, state, obj,
				out var imageRender)) return false;
			imageRender.Selected = normalized;
			imageRender.ImageState = normalized;
			return PublishImageRenderState(ref platform, state, obj, imageRender);
		}
		if (attribute == Selected && cls == MuiControlClass.Gadget)
		{
			if (!TryReadGadgetInteractionState(ref platform, state, obj,
				out var gadget)) return false;
			gadget.Selected = value == 0 ? 0u : 1u;
			return PublishGadgetInteractionState(ref platform, state, obj, gadget);
		}
		if (attribute == Weight && cls != MuiControlClass.Unknown)
		{
			if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, value, false)) return false;
			return SyncAreaWeightState(ref platform, state, obj, value);
		}
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			attribute, value, false);
	}

	private static bool IsScrollbarPropAttribute(uint attribute) =>
		attribute == PropEntries || attribute == PropVisible ||
		attribute == PropFirst || attribute == PropDeltaFactor ||
		attribute == PropSlider || attribute == PropUseWinBorder;

	private static bool SetScrollbarPropAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (attribute == PropFirst)
		{
			var current = Read(ref platform, state, obj, PropFirst, 0);
			var delta = unchecked((int)value) - unchecked((int)current);
			if (!ChangePropUnconditional(ref platform, state, obj, delta, notify))
				return false;
		}
		else
		{
			var normalized = attribute == PropSlider && value != 0 ? 1u : value;
			if (!ChangeDetectedSet(ref platform, state, obj, attribute, normalized,
				notify)) return false;
			if (!ChangePropUnconditional(ref platform, state, obj, 0, notify))
				return false;
		}
		if (!SyncScrollbarProp(ref platform, state, obj)) return false;
		if (attribute == PropDeltaFactor || attribute == PropSlider)
			return SyncPropPolicyField(ref platform, state, obj, attribute,
				attribute == PropSlider && value != 0 ? 1u : value);
		return true;
	}

	private static bool ChangeDetectedSet<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var found = attribute == Draggable || attribute == Dropable ?
			MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out var current) :
			MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
				out current);
		if (found && current == value) return true;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
			value, notify)) return false;
		return platform.ScheduleRedraw(obj, 2);
	}

	private static bool ChangePropUnconditional<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int amount, bool notify)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadPropRangeState(ref platform, state, obj,
			out var range)) return false;
		var entries = range.Entries;
		var visible = range.Visible;
		var first = unchecked((int)range.First);
		var last = entries > visible ? unchecked((int)(entries - visible)) : 0;
		var next = first + amount;
		if (next < 0) next = 0;
		if (next > last) next = last;
		if (next == first) return true;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, PropFirst,
			unchecked((uint)next), notify)) return false;
		range.First = unchecked((uint)next);
		if (!PublishPropRangeState(ref platform, state, obj, range)) return false;
		return platform.ScheduleRedraw(obj, 2);
	}

	private static bool IsGetOnly(uint attribute) =>
		attribute == TextShortened || attribute == StringAcknowledge ||
		attribute == StringScrollWidth ||
		attribute == StringScrollHeight ||
		attribute == StringScrollVisibleWidth ||
		attribute == StringScrollVisibleHeight ||
		attribute == BitmapRemapped ||
		attribute == Pressed ||
		attribute == GadgetGadget;

	private static bool IsInitOnly(uint attribute) =>
		attribute == StringMaxLen || attribute == StringSecret ||
		attribute == StringFormat || attribute == StringMultiline ||
		attribute == Unicode ||
		attribute == TextHiChar || attribute == TextSetMin ||
		attribute == TextSetMax || attribute == TextMarking ||
		attribute == SliderQuiet || attribute == PropUseWinBorder ||
		attribute == GroupHoriz || attribute == ScrollbarType ||
		attribute == MuiGroupLayoutHookCore.Attribute ||
		attribute == RectangleBarTitle ||
		attribute == RectangleHBar || attribute == RectangleVBar ||
		attribute == PropHoriz || attribute == GaugeHoriz ||
			attribute == RadioEntries ||
				attribute == InputMode || attribute == ShowSelState ||
			attribute == FramePhantomHoriz ||
		attribute == ImageFontMatch ||
		attribute == ImageFontMatchHeight ||
		attribute == ImageFontMatchWidth ||
		attribute == ImageOldImage || attribute == ImageFreeHoriz ||
		attribute == ImageFreeVert || attribute == BitmapUseFriend;

	// Bitmap/Bodychunk source and geometry attributes the autodocs mark [ISG]
	// (Bitmap Bitmap/Width/Height/Alpha/MappingTable/Precision/SourceColors/
	// Transparent; Bodychunk Body/Compression/Depth/Masking). These are settable
	// at runtime; a change must invalidate and rebuild the remapped/decoded
	// pixels rather than being silently dropped. Width/Height are shared by both
	// families; Bitmap-only and Bodychunk-only source attributes are gated by
	// class so a plain Bitmap does not accept Bodychunk source tags.
	private static bool IsBitmapMutableAttribute(MuiControlClass cls, uint attribute)
	{
		if (attribute == BitmapWidth || attribute == BitmapHeight ||
			attribute == BitmapAlpha || attribute == BitmapMappingTable ||
			attribute == BitmapPrecision || attribute == BitmapSourceColors ||
			attribute == BitmapTransparent)
			return true;
		if (cls == MuiControlClass.Bitmap && attribute == BitmapBitmap)
			return true;
		return cls == MuiControlClass.Bodychunk &&
			(attribute == BodychunkBody || attribute == BodychunkCompression ||
			attribute == BodychunkDepth || attribute == BodychunkMasking);
	}

	// ---- String / Text ownership ----------------------------------------------

		private static bool StringIsUnicode<TPlatform>(ref TPlatform platform,
			APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
		{
			return TryReadStringPresentationState(ref platform, state, obj,
				out var presentation) && presentation.Unicode != 0;
		}

		internal static uint ClampStringPosition(uint value, uint length) =>
			value > length ? length : value;

		internal static uint StringVisibleOrigin(uint cursor, uint display,
			uint visibleColumns)
		{
			if (visibleColumns == 0 || cursor == display) return display;
			if (cursor < display) return cursor;
			return cursor - display > visibleColumns ? cursor - visibleColumns : display;
		}

		private static void EnsureStringCursorVisible<TPlatform>(ref TPlatform platform,
			APTR state, APTR obj, int position)
			where TPlatform : struct, IMuiHeadlessPlatform
		{
			if (position < 0) position = 0;
			if (!MuiAreaLayoutCore.TryReadGeometryState(ref platform, state, obj,
				out var geometry)) return;
			var visibleColumns = geometry.Width <= 0 ? 0u :
				unchecked((uint)(geometry.Width / 8));
			if (visibleColumns == 0) return;
			if (!TryReadStringCursorState(ref platform, state, obj,
				out var cursor)) return;
			var display = cursor.DisplayPos < 0 ? 0u :
				unchecked((uint)cursor.DisplayPos);
			var origin = StringVisibleOrigin(unchecked((uint)position), display,
				visibleColumns);
			if (origin != display)
			{
				cursor.DisplayPos = unchecked((int)origin);
				PublishStringCursorState(ref platform, state, obj, cursor, 0,
					false);
			}
		}

	private static int StringCursorLength<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR source)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!StringIsUnicode(ref platform, state, obj))
			return StringLength(ref platform, source);
		if (MuiStringscrollCore.TryCountUtf8Columns(ref platform, source,
			out var columns)) return unchecked((int)columns);
		return StringLength(ref platform, source);
	}

	private static uint StringByteOffset<TPlatform>(ref TPlatform platform,
		APTR source, int columns, bool unicode)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (columns <= 0 || source.IsNull) return 0;
		if (!unicode) return unchecked((uint)columns);
		if (!CStringCodec.TryReadLength(ref platform, source, 65536,
			out var length)) return 0;
		return MuiStringscrollCore.ByteOffsetForColumns(ref platform, source, 0,
			length, unchecked((uint)columns));
	}

	private static int StringMaxChars<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var maxLen = TryReadStringPresentationState(ref platform, state, obj,
			out var presentation) ? presentation.MaxLen : Read(ref platform, state,
				obj, StringMaxLen, 80);
		return maxLen == 0 ? -1 : unchecked((int)maxLen) - 1;
	}

	private static bool CopyContents<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint storeKey, int maxChars, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out var raw) || raw == 0) return true;
			var source = APTR.FromPointer(raw);
			var owned = MuiStoreCore.DataspaceFind(ref platform, state, obj, storeKey);
			if (owned.IsNotNull && owned.Raw == raw)
			{
				if (attribute == StringContents &&
					Classify(ref platform, state, obj) == MuiControlClass.String)
				{
					var contents = default(MuiStringContentsState);
					contents.Contents = owned;
					return PublishStringContentsState(ref platform, state, obj,
						contents, notify);
				}
				if (attribute == TextContents &&
					Classify(ref platform, state, obj) == MuiControlClass.Text)
				{
					var contents = default(MuiTextContentsState);
					contents.Contents = owned;
					return PublishTextContentsState(ref platform, state, obj,
						contents, notify);
				}
				if (attribute == TextPreParse &&
					Classify(ref platform, state, obj) == MuiControlClass.Text)
				{
					var preParse = default(MuiTextPreParseState);
					preParse.PreParse = owned;
					return PublishTextPreParseState(ref platform, state, obj,
						preParse, notify);
				}
				if (attribute == NumericFormat && IsNumericFamily(
					Classify(ref platform, state, obj)))
				{
					var numericFormat = default(MuiNumericFormatState);
					numericFormat.Format = owned;
					return PublishNumericFormatState(ref platform, state, obj,
						numericFormat, notify);
				}
				if (attribute == GaugeInfoText &&
					Classify(ref platform, state, obj) == MuiControlClass.Gauge)
				{
					var infoText = default(MuiGaugeInfoTextState);
					infoText.InfoText = owned;
					return PublishGaugeInfoTextState(ref platform, state, obj,
						infoText, notify);
				}
				if (attribute == LevelmeterLabel &&
					Classify(ref platform, state, obj) == MuiControlClass.Levelmeter)
				{
					var label = default(MuiLevelmeterLabelState);
					label.Label = owned;
					return PublishLevelmeterLabelState(ref platform, state, obj,
						label, notify);
				}
				return true;
			}
			var length = StringLength(ref platform, source);
			if (maxChars >= 0 && length > maxChars)
			{
				var unicode = StringIsUnicode(ref platform, state, obj) &&
					(attribute == StringContents || attribute == StringPlaceholder);
				length = unicode ? unchecked((int)MuiStringscrollCore.ByteOffsetForColumns(
					ref platform, source, 0, unchecked((uint)length),
					unchecked((uint)maxChars))) : maxChars;
			}
		if (!MuiStoreCore.DataspaceAdd(ref platform, state, obj, storeKey, source,
			length + 1)) return false;
		var copy = MuiStoreCore.DataspaceFind(ref platform, state, obj, storeKey);
		if (copy.IsNull) return false;
		platform.WriteUInt8(copy, length, 0);
		if (attribute == StringContents &&
			Classify(ref platform, state, obj) == MuiControlClass.String)
		{
			var contents = default(MuiStringContentsState);
			contents.Contents = copy;
			return PublishStringContentsState(ref platform, state, obj, contents,
				notify);
		}
		if (attribute == TextContents &&
			Classify(ref platform, state, obj) == MuiControlClass.Text)
		{
			var contents = default(MuiTextContentsState);
			contents.Contents = copy;
			return PublishTextContentsState(ref platform, state, obj, contents,
				notify);
		}
		if (attribute == TextPreParse &&
			Classify(ref platform, state, obj) == MuiControlClass.Text)
		{
			var preParse = default(MuiTextPreParseState);
			preParse.PreParse = copy;
			return PublishTextPreParseState(ref platform, state, obj, preParse,
				notify);
		}
		if (attribute == NumericFormat && IsNumericFamily(
			Classify(ref platform, state, obj)))
		{
			var numericFormat = default(MuiNumericFormatState);
			numericFormat.Format = copy;
			return PublishNumericFormatState(ref platform, state, obj,
				numericFormat, notify);
		}
		if (attribute == GaugeInfoText &&
			Classify(ref platform, state, obj) == MuiControlClass.Gauge)
		{
			var infoText = default(MuiGaugeInfoTextState);
			infoText.InfoText = copy;
			return PublishGaugeInfoTextState(ref platform, state, obj,
				infoText, notify);
		}
		if (attribute == LevelmeterLabel &&
			Classify(ref platform, state, obj) == MuiControlClass.Levelmeter)
		{
			var label = default(MuiLevelmeterLabelState);
			label.Label = copy;
			return PublishLevelmeterLabelState(ref platform, state, obj, label,
				notify);
		}
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
			copy.Raw, notify);
	}

	private static int StringLength<TPlatform>(ref TPlatform platform, APTR source)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (source.IsNull) return 0;
		for (var index = 0; index < 4096; index++)
		{
			if (!platform.IsMapped(source, (uint)index + 1)) return index;
			if (platform.ReadUInt8(source, index) == 0) return index;
		}
		return 4096;
	}

	// ---- Text engine (PreParse + Contents formatting) -------------------------

	// The MUI text engine treats a string as PreParse followed by Contents and
	// interprets a small escape grammar (introduced by 0x1B, written "\33" in the
	// autodocs) plus '\n' line breaks. This is a safe, self-authored subset that
	// matches the documented semantics without reproducing SDK sources: escape
	// sequences carry no visible width, '\n' starts another line, "\33-" disables
	// further parsing, and bracketed image/colour specs ("\33I[..]", "\33p[..]",
	// "\33P[..]") are consumed whole.
	private struct MuiTextMetrics
	{
		public int MaxLineChars;
		public int LineCount;
	}

	private static MuiTextMetrics MeasureText<TPlatform>(ref TPlatform platform,
		APTR preParse, APTR contents) where TPlatform : struct, IMuiGuestMemory
	{
		var maxLine = 0;
		var lineChars = 0;
		var lineCount = 1;
		var engineOn = true;
		ScanVisible(ref platform, preParse, ref maxLine, ref lineChars,
			ref lineCount, ref engineOn);
		ScanVisible(ref platform, contents, ref maxLine, ref lineChars,
			ref lineCount, ref engineOn);
		if (lineChars > maxLine) maxLine = lineChars;
		MuiTextMetrics result = default;
		result.MaxLineChars = maxLine;
		result.LineCount = lineCount;
		return result;
	}

	private static void ScanVisible<TPlatform>(ref TPlatform platform, APTR text,
		ref int maxLine, ref int lineChars, ref int lineCount, ref bool engineOn)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (text.IsNull) return;
		for (var i = 0; i < 4096; i++)
		{
			if (!platform.IsMapped(text, (uint)i + 1)) break;
			var ch = platform.ReadUInt8(text, i);
			if (ch == 0) break;
			if (engineOn && ch == TextEscape)
			{
				i += ConsumeEscape(ref platform, text, i + 1, ref engineOn);
				continue;
			}
			if (ch == (byte)'\n')
			{
				if (lineChars > maxLine) maxLine = lineChars;
				lineChars = 0;
				lineCount++;
				continue;
			}
			lineChars++;
		}
	}

	// Returns the number of bytes following the ESC that form the escape spec, so
	// the caller can advance past them. Recognises the disable directive and the
	// bracketed image/pen specifications; every other spec is a single character.
	private static int ConsumeEscape<TPlatform>(ref TPlatform platform, APTR text,
		int pos, ref bool engineOn) where TPlatform : struct, IMuiGuestMemory
	{
		if (!platform.IsMapped(text, (uint)pos + 1)) return 0;
		var c = platform.ReadUInt8(text, pos);
		if (c == 0) return 0;
		if (c == (byte)'-') { engineOn = false; return 1; }
		if (c == (byte)'p' || c == (byte)'P' || c == (byte)'I')
			return ConsumeBracketedSpec(ref platform, text, pos);
		return 1;
	}

	private static int ConsumeBracketedSpec<TPlatform>(ref TPlatform platform,
		APTR text, int pos) where TPlatform : struct, IMuiGuestMemory
	{
		var consumed = 1;
		if (platform.IsMapped(text, (uint)pos + 2) &&
			platform.ReadUInt8(text, pos + 1) == (byte)'[')
		{
			consumed++;
			for (var j = pos + 2; j < pos + 2 + 64; j++)
			{
				if (!platform.IsMapped(text, (uint)j + 1)) break;
				var d = platform.ReadUInt8(text, j);
				consumed++;
				if (d == (byte)']' || d == 0) break;
			}
		}
		return consumed;
	}

	// Build a private render buffer holding the visible glyphs of PreParse +
	// Contents with the escape sequences removed but '\n' preserved, and report
	// the effective alignment and front pen derived from the escape stream.
	private static APTR BuildTextRenderOwned<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR preParse, APTR contents, out int align,
		out uint frontPen) where TPlatform : struct, IMuiHeadlessPlatform
	{
		const int Capacity = 1024;
		align = 0;
		frontPen = uint.MaxValue;
		var buffer = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			TextRenderKey);
		if (buffer.IsNull)
		{
			if (!MuiStoreCore.DataspaceAdd(ref platform, state, obj, TextRenderKey,
				state, Capacity)) return APTR.Null;
			buffer = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				TextRenderKey);
		}
		else if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			TextRenderKey) < Capacity && !MuiStoreCore.DataspaceResize(ref platform,
			state, obj, TextRenderKey, Capacity)) return APTR.Null;
		if (buffer.IsNull || !platform.IsMapped(buffer, Capacity)) return APTR.Null;
		var outIdx = 0;
		var engineOn = true;
		var atLineStart = true;
		AppendRender(ref platform, preParse, buffer, Capacity, ref outIdx,
			ref engineOn, ref atLineStart, ref align, ref frontPen);
		AppendRender(ref platform, contents, buffer, Capacity, ref outIdx,
			ref engineOn, ref atLineStart, ref align, ref frontPen);
		platform.WriteUInt8(buffer, outIdx, 0);
		return buffer;
	}

	private static void AppendRender<TPlatform>(ref TPlatform platform, APTR text,
		APTR buffer, int capacity, ref int outIdx, ref bool engineOn,
		ref bool atLineStart, ref int align, ref uint frontPen)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (text.IsNull) return;
		for (var i = 0; i < 4096; i++)
		{
			if (!platform.IsMapped(text, (uint)i + 1)) break;
			var ch = platform.ReadUInt8(text, i);
			if (ch == 0) break;
			if (engineOn && ch == TextEscape)
			{
				i += HandleEscapeForRender(ref platform, text, i + 1, ref engineOn,
					atLineStart, ref align, ref frontPen);
				continue;
			}
			if (ch == (byte)'\n')
			{
				if (outIdx < capacity - 1)
					platform.WriteUInt8(buffer, outIdx++, (byte)'\n');
				atLineStart = true;
				continue;
			}
			if (outIdx < capacity - 1)
			{
				platform.WriteUInt8(buffer, outIdx++, ch);
				atLineStart = false;
			}
		}
	}

	private static int HandleEscapeForRender<TPlatform>(ref TPlatform platform,
		APTR text, int pos, ref bool engineOn, bool atLineStart, ref int align,
		ref uint frontPen) where TPlatform : struct, IMuiGuestMemory
	{
		if (!platform.IsMapped(text, (uint)pos + 1)) return 0;
		var c = platform.ReadUInt8(text, pos);
		if (c == 0) return 0;
		switch (c)
		{
			case (byte)'-': engineOn = false; return 1;
			// Alignment is only meaningful at the start of a line.
			case (byte)'l': if (atLineStart) align = 0; return 1;
			case (byte)'c': if (atLineStart) align = 1; return 1;
			case (byte)'r': if (atLineStart) align = 2; return 1;
			case (byte)'M': if (frontPen == uint.MaxValue) frontPen = TextMarkingPen;
				return 1;
		}
		// "\33<n>" selects DrawInfo pen n (2..9) as the front pen.
		if (c >= (byte)'2' && c <= (byte)'9')
		{
			if (frontPen == uint.MaxValue) frontPen = (uint)(c - (byte)'0');
			return 1;
		}
		if (c == (byte)'p' || c == (byte)'P' || c == (byte)'I')
			return ConsumeBracketedSpec(ref platform, text, pos);
		return 1;
	}

	// Draw the render buffer line by line, honouring alignment and, when cutoff
	// shortening is active, replacing the tail of an over-wide line with "...".
	private static void DrawTextRender<TPlatform>(ref TPlatform platform,
		APTR rastPort, APTR font, APTR render, int left, int top, int width,
		int height, int align, bool cutoff, int fitChars, byte hiChar)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var lineHeight = platform.TextHeight(rastPort, font);
		if (lineHeight <= 0) lineHeight = 8;
		var total = StringLength(ref platform, render);
		var lineStart = 0;
		var baseline = top + lineHeight;
		for (var i = 0; i <= total; i++)
		{
			var ch = i < total ? platform.ReadUInt8(render, i) : (byte)0;
			if (i != total && ch != (byte)'\n') continue;
			var lineLen = i - lineStart;
			if (cutoff && lineLen > fitChars)
			{
				if (fitChars >= 3)
				{
					platform.WriteUInt8(render, lineStart + fitChars - 3, (byte)'.');
					platform.WriteUInt8(render, lineStart + fitChars - 2, (byte)'.');
					platform.WriteUInt8(render, lineStart + fitChars - 1, (byte)'.');
					lineLen = fitChars;
				}
				else
				{
					lineLen = fitChars < 0 ? 0 : fitChars;
				}
			}
			if (lineLen > 0)
			{
				var lineText = APTR.FromPointer(render.Raw + (uint)lineStart);
				var textWidth = platform.TextWidth(rastPort, font, lineText, lineLen);
				var textLeft = left;
				if (align == 1) textLeft = left + (width - textWidth) / 2;
				else if (align == 2) textLeft = left + width - textWidth;
				if (textLeft < left) textLeft = left;
				platform.DrawText(rastPort, font, textLeft, baseline, lineText,
					lineLen);
				if (hiChar != 0)
				{
					var match = -1;
					for (var index = 0; index < lineLen; index++)
					{
						var candidate = platform.ReadUInt8(lineText, index);
						if (Lower(candidate) == Lower(hiChar))
						{
							match = index;
							break;
						}
					}
					if (match >= 0)
					{
						var before = platform.TextWidth(rastPort, font, lineText, match);
						var glyph = APTR.FromPointer(lineText.Raw + (uint)match);
						var glyphWidth = platform.TextWidth(rastPort, font, glyph, 1);
						if (glyphWidth <= 0) glyphWidth = 1;
						platform.SetPen(rastPort, 3);
						platform.DrawLine(rastPort, textLeft + before, baseline + 1,
							textLeft + before + glyphWidth - 1, baseline + 1);
					}
				}
			}
			baseline += lineHeight;
			lineStart = i + 1;
		}
	}

	// ---- Class-specific neutral AskMinMax / Draw ------------------------------

	public static bool AskMinMax<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR storage) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!platform.IsMapped(storage, 12)) return false;
		var cls = Classify(ref platform, state, obj);
		if (cls == MuiControlClass.Radio || cls == MuiControlClass.Scrollbar)
			return MuiGroupLayoutCore.AskMinMax(ref platform, state, obj, storage);
		return MuiAreaLayoutCore.WriteMinMax(ref platform, storage,
			ComputeControlMinMax(ref platform, state, obj));
	}

	// Scrollbar is a Group subclass, but its three children have fixed semantic
	// positions: two 16-pixel arrow buttons and a proportional track.  The
	// generic Group weight allocator would distribute all three children evenly,
	// so keep the group topology while applying the MorphOS scrollbar geometry.
	public static bool LayoutScrollbar<TPlatform>(ref TPlatform platform, APTR state,
		APTR scrollbar, int left, int top, int width, int height)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (width < 0 || height < 0) return false;
		if (!TryReadScrollbarLayoutState(ref platform, state, scrollbar,
			out var layout)) return false;
		var horizontal = layout.Horizontal != 0;
		var type = layout.Type;
		var arrowsShown = type != ScrollbarTypeNone;
		var arrowExtent = arrowsShown ? 16 : 0;
		var total = horizontal ? width : height;
		var trackExtent = total - arrowExtent * 2;
		if (trackExtent < 0) trackExtent = 0;
		var prop = FindScrollbarPart(ref platform, state, scrollbar,
			ScrollbarPartProp);
		if (prop.IsNull) return false;
		var firstArrow = FindScrollbarArrow(ref platform, state, scrollbar, 0);
		var secondArrow = FindScrollbarArrow(ref platform, state, scrollbar, 1);
		if (firstArrow.IsNull || secondArrow.IsNull) return false;
		if (!arrowsShown)
		{
			if (!LayoutScrollbarPart(ref platform, state, prop, left, top, width,
				height)) return false;
			LayoutScrollbarPart(ref platform, state, firstArrow, left, top, 0, 0);
			LayoutScrollbarPart(ref platform, state, secondArrow, left, top, 0, 0);
		}
		else if (horizontal)
		{
			if (type == ScrollbarTypeBottom)
			{
				if (!LayoutScrollbarPart(ref platform, state, prop, left, top,
					trackExtent, height) ||
					!LayoutScrollbarPart(ref platform, state, firstArrow,
						left + trackExtent, top, arrowExtent, height) ||
					!LayoutScrollbarPart(ref platform, state, secondArrow,
						left + trackExtent + arrowExtent, top, arrowExtent, height))
					return false;
			}
			else if (type == ScrollbarTypeTop)
			{
				if (!LayoutScrollbarPart(ref platform, state, firstArrow, left, top,
					arrowExtent, height) ||
					!LayoutScrollbarPart(ref platform, state, secondArrow,
						left + arrowExtent, top, arrowExtent, height) ||
					!LayoutScrollbarPart(ref platform, state, prop,
						left + arrowExtent * 2, top, trackExtent, height)) return false;
			}
			else if (!LayoutScrollbarPart(ref platform, state, firstArrow, left,
				top, arrowExtent, height) ||
				!LayoutScrollbarPart(ref platform, state, prop, left + arrowExtent,
					top, trackExtent, height) ||
				!LayoutScrollbarPart(ref platform, state, secondArrow,
					left + arrowExtent + trackExtent, top, arrowExtent, height))
				return false;
		}
		else
		{
			if (type == ScrollbarTypeBottom)
			{
				if (!LayoutScrollbarPart(ref platform, state, prop, left, top,
					width, trackExtent) ||
					!LayoutScrollbarPart(ref platform, state, firstArrow, left,
						top + trackExtent, width, arrowExtent) ||
					!LayoutScrollbarPart(ref platform, state, secondArrow, left,
						top + trackExtent + arrowExtent, width, arrowExtent)) return false;
			}
			else if (type == ScrollbarTypeTop)
			{
				if (!LayoutScrollbarPart(ref platform, state, firstArrow, left, top,
					width, arrowExtent) ||
					!LayoutScrollbarPart(ref platform, state, secondArrow, left,
						top + arrowExtent, width, arrowExtent) ||
					!LayoutScrollbarPart(ref platform, state, prop, left,
						top + arrowExtent * 2, width, trackExtent)) return false;
			}
			else if (!LayoutScrollbarPart(ref platform, state, firstArrow, left,
				top, width, arrowExtent) ||
				!LayoutScrollbarPart(ref platform, state, prop, left,
					top + arrowExtent, width, trackExtent) ||
				!LayoutScrollbarPart(ref platform, state, secondArrow, left,
					top + arrowExtent + trackExtent, width, arrowExtent)) return false;
		}
		return MuiAreaLayoutCore.Layout(ref platform, state, scrollbar, left, top,
			width, height);
	}

	private static bool LayoutScrollbarPart<TPlatform>(ref TPlatform platform,
		APTR state, APTR child, int left, int top, int width, int height)
		where TPlatform : struct, IMuiLayoutPlatform =>
		MuiAreaLayoutCore.Layout(ref platform, state, child, left, top, width,
			height);

	internal static bool TryComputeMinMax<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiMinMaxValues values)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (Classify(ref platform, state, obj) == MuiControlClass.Unknown)
		{
			values = default;
			return false;
		}
		values = ComputeControlMinMax(ref platform, state, obj);
		return true;
	}

	private static MuiMinMaxValues ComputeControlMinMax<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		MuiMinMaxValues result = default;
		if (!TryReadAreaPresentationState(ref platform, state, obj,
			out var areaPresentation) || areaPresentation.ShowMe == 0) return result;
		var cls = Classify(ref platform, state, obj);
		switch (cls)
		{
			case MuiControlClass.Numeric:
			case MuiControlClass.Levelmeter:
			case MuiControlClass.Numericbutton:
				Fill(ref result, 48, 14, 48, 14, 10000, 14);
				break;
			case MuiControlClass.Slider:
				if (TryReadSliderPresentationState(ref platform, state, obj,
					out var sliderPresentation) && sliderPresentation.Horizontal != 0)
					Fill(ref result, 48, 14, 48, 14, 10000, 14);
				else
					Fill(ref result, 14, 48, 14, 48, 14, 10000);
				break;
			case MuiControlClass.Knob:
				Fill(ref result, 32, 32, 32, 32, 32, 32);
				break;
			case MuiControlClass.Gauge:
				Fill(ref result, 64, 16, 64, 16, 10000, 16);
				break;
			case MuiControlClass.Prop:
				if (Read(ref platform, state, obj, PropUseWinBorder, 0) != 0)
					break;
				Fill(ref result, 16, 16, 16, 16, 10000, 10000);
				break;
			case MuiControlClass.Scrollbar:
				return MuiGroupLayoutCore.ComputeMinMax(ref platform, state, obj);
			case MuiControlClass.Scale:
				Fill(ref result, 64, 8, 64, 8, 10000, 8);
				break;
			case MuiControlClass.String:
				Fill(ref result, 64, 14, 64, 14, 10000, 14);
				break;
			case MuiControlClass.Text:
			{
				var textPresentation = default(MuiTextPresentationState);
				TryReadTextPresentationState(ref platform, state, obj,
					out textPresentation);
				var preParseState = default(MuiTextPreParseState);
				TryReadTextPreParseState(ref platform, state, obj,
					out preParseState);
				var preParse = preParseState.PreParse;
				var textContents = default(MuiTextContentsState);
				TryReadTextContentsState(ref platform, state, obj,
					out textContents);
				var contents = textContents.Contents;
				// Measure the visible glyphs only: PreParse is prepended, ESC
				// formatting sequences carry no width, and '\n' begins another line.
				var metrics = MeasureText(ref platform, preParse, contents);
				var textWidth = (metrics.MaxLineChars <= 0 ? 1 : metrics.MaxLineChars)
					* 8;
				var textHeight = metrics.LineCount * 10;
				var setMin = textPresentation.SetMin != 0;
				var setMax = textPresentation.SetMax != 0;
				var setVerticalMax = textPresentation.SetVMax != 0;
				Fill(ref result, setMin ? textWidth : 0, setMin ? textHeight : 0,
					textWidth, textHeight, setMax ? textWidth : 10000,
					setVerticalMax ? textHeight : 10000);
				break;
			}
			case MuiControlClass.Image:
			{
				var minWidth = 16;
				var minHeight = 16;
				var oldImageState = default(MuiImageOldImageState);
				TryReadImageOldImageState(ref platform, state, obj,
					out oldImageState);
				var oldImage = oldImageState.Image;
				if (MuiImageGeometryCodec.TryRead(ref platform, oldImage,
					out var imageGeometry))
				{
					var imageWidth = imageGeometry.Width;
					var imageHeight = imageGeometry.Height;
					if (imageWidth > 0) minWidth = imageWidth;
					if (imageHeight > 0) minHeight = imageHeight;
				}
				var imageRender = default(MuiImageRenderState);
				TryReadImageRenderState(ref platform, state, obj, out imageRender);
				var freeHoriz = imageRender.FreeHoriz != 0;
				var freeVert = imageRender.FreeVert != 0;
				Fill(ref result, minWidth, minHeight, minWidth, minHeight,
					freeHoriz ? 10000 : minWidth, freeVert ? 10000 : minHeight);
				break;
			}
			case MuiControlClass.Bitmap:
			case MuiControlClass.Bodychunk:
			{
				var geometry = default(MuiBitmapGeometryState);
				TryReadBitmapGeometryState(ref platform, state, obj,
					out geometry);
				var width = unchecked((int)geometry.Width);
				var height = unchecked((int)geometry.Height);
				if (width <= 0) width = 16;
				if (height <= 0) height = 16;
				Fill(ref result, width, height, width, height, width, height);
				break;
			}
			case MuiControlClass.Gadget:
				Fill(ref result, 8, 8, 8, 8, 10000, 10000);
				break;
			case MuiControlClass.Cycle:
			{
				// A cycle gadget shows the active entry text next to a fixed
				// cycle image (the arrow button). The neutral geometry sizes the
				// minimum to the widest entry plus the image and inner spacing,
				// with a fixed row height and horizontal growth.
				var entries = default(MuiChoiceEntriesState);
				TryReadChoiceEntriesState(ref platform, state, obj,
					CycleEntries, out entries);
				var widest = 0;
				if (entries.Entries.IsNotNull)
				{
					var count = CountEntries(ref platform, entries.Entries);
					for (var index = 0; index < count; index++)
					{
						var chars = StringLength(ref platform, ChoiceEntry(
							ref platform, entries.Entries, unchecked((uint)index)));
						if (chars > widest) widest = chars;
					}
				}
				var textWidth = (widest <= 0 ? 1 : widest) * 8;
				var minWidth = CycleImageWidth + CycleSpacing + textWidth;
				Fill(ref result, minWidth, 14, minWidth, 14, 10000, 14);
				break;
			}
			case MuiControlClass.Rectangle:
				Fill(ref result, 1, 1, 1, 1, 10000, 10000);
				break;
			default:
				return MuiAreaLayoutCore.ComputeMinMax(ref platform, state, obj);
		}
		return result;
	}

	private static void Fill(ref MuiMinMaxValues values, int minWidth, int minHeight,
		int defWidth, int defHeight, int maxWidth, int maxHeight)
	{
		values.MinWidth = Dim(minWidth);
		values.MinHeight = Dim(minHeight);
		values.DefWidth = Dim(defWidth);
		values.DefHeight = Dim(defHeight);
		values.MaxWidth = Dim(maxWidth);
		values.MaxHeight = Dim(maxHeight);
	}

	private static short Dim(int value) =>
		unchecked((short)(value > 10000 ? 10000 : (value < 0 ? 0 : value)));

	public static bool DrawControl<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint flags) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!GetRenderPort(ref platform, state, obj, out var rastPort)) return false;
		if (!MuiAreaLayoutCore.TryReadGeometryState(ref platform, state, obj,
			out var geometry)) return false;
		var left = geometry.Left;
		var top = geometry.Top;
		var width = geometry.Width;
		var height = geometry.Height;
		if (!MuiAreaLayoutCore.TryReadRenderPolicyState(ref platform, state, obj,
			out var renderPolicy)) return false;
		if (width <= 0 || height <= 0 || !platform.LockLayer(rastPort)) return false;
		if (!platform.BeginUpdate(rastPort))
		{
			platform.UnlockLayer(rastPort);
			return false;
		}
		var clip = platform.PushClip(rastPort, left, top, width, height);
		if (renderPolicy.FillArea != 0)
		{
			platform.SetPen(rastPort, renderPolicy.Background);
			platform.FillRectangle(rastPort, left, top, left + width - 1,
				top + height - 1);
		}
		DrawContent(ref platform, state, obj, rastPort, left, top, width, height);
		if (renderPolicy.Frame != 0 && renderPolicy.FrameVisible != 0)
		{
			platform.SetPen(rastPort, 4);
			if (renderPolicy.FramePhantomHoriz == 0)
				platform.DrawLine(rastPort, left, top, left + width - 1, top);
			platform.DrawLine(rastPort, left, top, left, top + height - 1);
			if (renderPolicy.FramePhantomHoriz == 0)
				platform.DrawLine(rastPort, left, top + height - 1, left + width - 1,
					top + height - 1);
			platform.DrawLine(rastPort, left + width - 1, top, left + width - 1,
				top + height - 1);
		}
		platform.PopClip(rastPort, clip);
		platform.EndUpdate(rastPort, true);
		platform.UnlockLayer(rastPort);
		return true;
	}

	private static void DrawContent<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR rastPort, int left, int top, int width, int height)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var cls = Classify(ref platform, state, obj);
		var font = APTR.Null;
		if (TryReadControlFontState(ref platform, state, obj, out var fontState) &&
			fontState.Present) font = fontState.Font;
		if (cls == MuiControlClass.Gadget)
		{
			if (TryReadGadgetInteractionState(ref platform, state, obj,
				out var gadget) && gadget.ShowSelState != 0 &&
				gadget.Selected != 0 &&
				width > 2 && height > 2)
			{
				if (!TryReadAreaPresentationState(ref platform, state, obj,
					out var areaPresentation)) return;
				var pen = areaPresentation.Disabled != 0 ? 1u : 3u;
				platform.SetPen(rastPort, pen);
				platform.DrawLine(rastPort, left + 1, top + 1,
					left + width - 2, top + 1);
				platform.DrawLine(rastPort, left + 1, top + 1,
					left + 1, top + height - 2);
				platform.DrawLine(rastPort, left + 1, top + height - 2,
					left + width - 2, top + height - 2);
				platform.DrawLine(rastPort, left + width - 2, top + 1,
					left + width - 2, top + height - 2);
			}
			return;
		}
		if (cls == MuiControlClass.Rectangle)
		{
			if (!TryReadRectanglePresentationState(ref platform, state, obj,
				out var rectanglePresentation)) return;
			var right = left + width - 1;
			var bottom = top + height - 1;
			var middleX = left + width / 2;
			var middleY = top + height / 2;
			if (rectanglePresentation.HorizontalBar != 0)
			{
				platform.SetPen(rastPort, 2);
				platform.DrawLine(rastPort, left, middleY, right, middleY);
			}
			if (rectanglePresentation.VerticalBar != 0)
			{
				platform.SetPen(rastPort, 2);
				platform.DrawLine(rastPort, middleX, top, middleX, bottom);
			}
			var title = APTR.Null;
			if (TryReadRectangleBarTitleState(ref platform, state, obj,
				out var titleState) && titleState.Present)
				title = titleState.Title;
			var titleLength = StringLength(ref platform, title);
			if (titleLength > 0)
				platform.DrawText(rastPort, font, left, top + height, title,
					titleLength);
			return;
		}
		if (cls == MuiControlClass.Gauge)
		{
			if (!TryReadGaugeState(ref platform, state, obj,
				out var gauge)) return;
			var maximum = gauge.Maximum;
			var current = gauge.Current;
			var horizontal = gauge.Horizontal != 0;
			var span = horizontal ? width : height;
			var extent = maximum == 0 ? 0 :
				(int)(current * (uint)span / maximum);
			if (extent > 0)
			{
				platform.SetPen(rastPort, 2);
				if (horizontal)
					platform.FillRectangle(rastPort, left, top, left + extent - 1,
						top + height - 1);
				else
					platform.FillRectangle(rastPort, left, top + height - extent,
						left + width - 1, top + height - 1);
			}
			// InfoText is displayed inside the gauge, centered and highlighted
			// (preparse "\33c\0338"), with %ld substituted by the current value.
			var info = GaugeInfoTextOwned(ref platform, state, obj,
				unchecked((int)current));
			var infoLength = StringLength(ref platform, info);
			if (infoLength > 0)
			{
				platform.SetPen(rastPort, 8);
				var textWidth = platform.TextWidth(rastPort, font, info,
					infoLength);
				var textHeight = platform.TextHeight(rastPort, font);
				var textLeft = left + (width - textWidth) / 2;
				if (textLeft < left) textLeft = left;
				platform.DrawText(rastPort, font, textLeft,
					top + (height + textHeight) / 2, info, infoLength);
			}
			return;
		}
		if (cls == MuiControlClass.Levelmeter)
		{
			if (!TryReadNumericState(ref platform, state, obj,
				out var numeric)) return;
			if (!TryReadLevelmeterPresentationState(ref platform, state, obj,
				out var levelmeterPresentation)) return;
			var minimum = unchecked((int)numeric.Minimum);
			var maximum = unchecked((int)numeric.Maximum);
			var current = unchecked((int)numeric.Value);
			var range = maximum > minimum ? maximum - minimum : 0;
			var relative = current - minimum;
			if (relative < 0) relative = 0;
			if (relative > range) relative = range;
			var horizontal = levelmeterPresentation.Horizontal != 0;
			var span = horizontal ? width : height;
			var extent = range == 0 ? 0 : (int)((uint)relative *
				(uint)span / (uint)range);
			if (extent > 0)
			{
				platform.SetPen(rastPort, 2);
				if (horizontal)
					platform.FillRectangle(rastPort, left, top, left + extent - 1,
						top + height - 1);
				else
					platform.FillRectangle(rastPort, left, top + height - extent,
						left + width - 1, top + height - 1);
			}
			var label = APTR.Null;
			if (TryReadLevelmeterLabelState(ref platform, state, obj,
				out var labelState)) label = labelState.Label;
			var labelLength = StringLength(ref platform, label);
			if (labelLength > 0)
				platform.DrawText(rastPort, font, left, top + height, label,
					labelLength);
			return;
		}
		if (cls == MuiControlClass.Numeric || cls == MuiControlClass.Numericbutton)
		{
			if (!TryReadNumericState(ref platform, state, obj,
				out var numeric)) return;
			var value = unchecked((int)numeric.Value);
			var buffer = StringifyOwned(ref platform, state, obj, value);
			var length = StringLength(ref platform, buffer);
			if (length > 0)
				platform.DrawText(rastPort, font, left, top + height, buffer, length);
			return;
		}
		if (cls == MuiControlClass.Slider || cls == MuiControlClass.Knob)
		{
			var horizontal = cls == MuiControlClass.Knob;
			var quiet = 0u;
			if (cls == MuiControlClass.Slider &&
				TryReadSliderPresentationState(ref platform, state, obj,
					out var sliderPresentation))
			{
				horizontal = sliderPresentation.Horizontal != 0;
				quiet = sliderPresentation.Quiet;
			}
			var span = horizontal ? width : height;
			var position = ValueToScale(ref platform, state, obj, 0,
				span > 1 ? span - 1 : 0);
			platform.SetPen(rastPort, 2);
			if (horizontal)
			{
				var thumb = left + position;
				platform.FillRectangle(rastPort, thumb, top, thumb + 1,
					top + height - 1);
			}
			else
			{
				var thumb = top + height - 1 - position;
				platform.FillRectangle(rastPort, left, thumb, left + width - 1,
					thumb + 1);
			}
			if (cls == MuiControlClass.Slider && quiet == 0)
			{
				var valueText = StringifyOwned(ref platform, state, obj,
					unchecked((int)Read(ref platform, state, obj, NumericValue, 0)));
				var valueLength = StringLength(ref platform, valueText);
				if (valueLength > 0)
				{
					platform.SetPen(rastPort, 3);
					var textWidth = platform.TextWidth(rastPort, font, valueText,
						valueLength);
					var textHeight = platform.TextHeight(rastPort, font);
					var textLeft = left + (width - textWidth) / 2;
					if (textLeft < left) textLeft = left;
					platform.DrawText(rastPort, font, textLeft,
						top + (height + textHeight) / 2, valueText, valueLength);
				}
			}
			return;
		}
		if (cls == MuiControlClass.Prop)
		{
			if (Read(ref platform, state, obj, PropUseWinBorder, 0) != 0)
				return;
			if (!TryReadPropRangeState(ref platform, state, obj,
				out var range)) return;
			var entries = range.Entries;
			var visible = range.Visible;
			var first = range.First;
			var horizontal = Read(ref platform, state, obj, PropHoriz, 1) != 0;
			var track = horizontal ? width : height;
			var span = entries == 0 ? track :
				(int)(visible * (uint)track / entries);
			var offset = entries == 0 ? 0 : (int)(first * (uint)track / entries);
			if (span <= 0) span = 1;
			platform.SetPen(rastPort, 2);
			if (horizontal)
				platform.FillRectangle(rastPort, left + offset, top,
					left + offset + span - 1, top + height - 1);
			else
				platform.FillRectangle(rastPort, left, top + height - offset - span,
					left + width - 1, top + height - offset - 1);
			return;
		}
		if (cls == MuiControlClass.Scrollbar)
		{
			DrawScrollbarContent(ref platform, state, obj, rastPort, left, top,
				width, height);
			return;
		}
		if (cls == MuiControlClass.Text)
		{
			if (!TryReadTextPresentationState(ref platform, state, obj,
				out var textPresentation)) return;
			var preParseState = default(MuiTextPreParseState);
			if (!TryReadTextPreParseState(ref platform, state, obj,
				out preParseState)) return;
			var preParse = preParseState.PreParse;
			var textContents = default(MuiTextContentsState);
			if (!TryReadTextContentsState(ref platform, state, obj,
				out textContents)) return;
			var contents = textContents.Contents;
			var metrics = MeasureText(ref platform, preParse, contents);
			var totalWidth = metrics.MaxLineChars * 8;
			var didShorten = metrics.MaxLineChars > 0 && totalWidth > width;
			// MUIA_Text_Shortened reports whether the visible text exceeded the
			// allocated width, regardless of the chosen shorten mode.
			var shortenedState = default(MuiTextShortenedState);
			shortenedState.Shortened = didShorten ? 1u : 0u;
			PublishTextShortenedState(ref platform, state, obj, shortenedState);
			if (metrics.MaxLineChars <= 0) return;
			var shorten = textPresentation.Shorten;
			// MUIV_Text_Shorten_Hide suppresses the whole text when it will not fit.
			if (didShorten && shorten == TextShortenHide) return;
			var render = BuildTextRenderOwned(ref platform, state, obj, preParse,
				contents, out var align, out var frontPen);
			if (render.IsNull) return;
			// MUIA_Text_Marking (and an "\33M" sequence) select the marking pen.
			if (textPresentation.Marking != 0)
				frontPen = TextMarkingPen;
			if (frontPen != uint.MaxValue) platform.SetPen(rastPort, frontPen);
			var cutoff = didShorten && shorten == TextShortenCutoff;
			var hiChar = unchecked((byte)textPresentation.HiChar);
			DrawTextRender(ref platform, rastPort, font, render, left, top, width,
				height, align, cutoff, width / 8, hiChar);
			return;
		}
		if (cls == MuiControlClass.String)
		{
			DrawStringContent(ref platform, state, obj, rastPort, left, top, width,
				height, font);
			return;
		}
		if (cls == MuiControlClass.Cycle || cls == MuiControlClass.Radio)
		{
			var entries = default(MuiChoiceEntriesState);
			var entriesAttribute = cls == MuiControlClass.Cycle ? CycleEntries :
				RadioEntries;
			if (!TryReadChoiceEntriesState(ref platform, state, obj,
				entriesAttribute, out entries)) return;
			var active = Read(ref platform, state, obj,
				cls == MuiControlClass.Cycle ? CycleActive : RadioActive, 0);
			var choice = ChoiceEntry(ref platform, entries.Entries, active);
			var length = StringLength(ref platform, choice);
			if (length > 0)
				platform.DrawText(rastPort, font, left, top + height, choice, length);
			return;
		}
		if (cls == MuiControlClass.Image)
		{
			DrawImageContent(ref platform, state, obj, rastPort, left, top,
				width, height);
			return;
		}
		if (IsBitmapFamily(cls))
		{
			var image = APTR.FromPointer(Read(ref platform, state, obj,
				BitmapRemapped, 0));
			if (image.IsNull)
			{
				var source = default(MuiBitmapSourceState);
				if (TryReadBitmapSourceState(ref platform, state, obj, cls,
					out source)) image = source.Source;
			}
			if (image.IsNotNull)
				platform.DrawImage(rastPort, image, left, top, width, height);
			return;
		}
		if (cls == MuiControlClass.Scale)
		{
			if (!TryReadScalePresentationState(ref platform, state, obj,
				out var scalePresentation)) return;
			platform.SetPen(rastPort, 2);
			if (scalePresentation.Horizontal != 0)
			{
				// A graduated 0%..100% scale. The number of divisions adapts to the
				// available width so the scale is "more or less detailed" as the
				// autodoc describes; each division carries a tick and the ends /
				// midpoint are full-height majors, the rest half-height minors.
				var axisY = top + height / 2;
				platform.DrawLine(rastPort, left, axisY, left + width - 1, axisY);
				var divisions = width >= 110 ? 10 : width >= 50 ? 5 :
					width >= 20 ? 2 : 1;
				var minorTop = top + height / 4;
				var minorBottom = top + height - 1 - height / 4;
				if (minorBottom < minorTop) minorBottom = minorTop;
				for (var index = 0; index <= divisions; index++)
				{
					var x = left + index * (width - 1) / divisions;
					var major = index == 0 || index == divisions ||
						(divisions % 2 == 0 && index == divisions / 2);
					if (major)
						platform.DrawLine(rastPort, x, top, x, top + height - 1);
					else
						platform.DrawLine(rastPort, x, minorTop, x, minorBottom);
				}
			}
			else
			{
				// Vertical scales use the same graduated 0%..100% policy, with
				// the axis running through the centre and horizontal ticks at
				// adaptive height divisions. Keep the geometry integer-only and
				// bounded so the MC68000 path needs no floating point or runtime.
				var axisX = left + width / 2;
				platform.DrawLine(rastPort, axisX, top, axisX,
					top + height - 1);
				var divisions = height >= 110 ? 10 : height >= 50 ? 5 :
					height >= 20 ? 2 : 1;
				var minorLeft = left + width / 4;
				var minorRight = left + width - 1 - width / 4;
				var majorRight = left + width - 1;
				if (minorRight < minorLeft) minorRight = minorLeft;
				for (var index = 0; index <= divisions; index++)
				{
					var y = top + index * (height - 1) / divisions;
					var major = index == 0 || index == divisions ||
						(divisions % 2 == 0 && index == divisions / 2);
					if (major)
						platform.DrawLine(rastPort, left, y, majorRight, y);
					else
						platform.DrawLine(rastPort, minorLeft, y, minorRight, y);
				}
			}
		}
	}

	private static void DrawScrollbarContent<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR rastPort, int left, int top, int width,
		int height) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadScrollbarLayoutState(ref platform, state, obj,
			out var layout)) return;
		var horizontal = layout.Horizontal != 0;
		var type = layout.Type;
		var arrows = type != ScrollbarTypeNone;
		var arrowExtent = arrows ? 16 : 0;
		var propLeft = left;
		var propTop = top;
		var propWidth = width;
		var propHeight = height;
		var firstLeft = left;
		var firstTop = top;
		var secondLeft = left;
		var secondTop = top;
		var firstNegative = true;
		var secondNegative = false;
		if (arrows)
		{
			if (horizontal)
			{
				propWidth = width - (type == ScrollbarTypeSym ||
					type == ScrollbarTypeDefault ? arrowExtent * 2 : arrowExtent);
				if (propWidth < 0) propWidth = 0;
				if (type == ScrollbarTypeBottom)
				{
					firstLeft = left + propWidth;
					secondLeft = firstLeft + arrowExtent;
				}
				else if (type == ScrollbarTypeTop)
				{
					firstLeft = left;
					secondLeft = left + arrowExtent;
					propLeft = left + arrowExtent * 2;
				}
				else
				{
					propLeft = left + arrowExtent;
					secondLeft = propLeft + propWidth;
				}
			}
			else
			{
				propHeight = height - (type == ScrollbarTypeSym ||
					type == ScrollbarTypeDefault ? arrowExtent * 2 : arrowExtent);
				if (propHeight < 0) propHeight = 0;
				if (type == ScrollbarTypeBottom)
				{
					firstTop = top + propHeight;
					secondTop = firstTop + arrowExtent;
				}
				else if (type == ScrollbarTypeTop)
				{
					firstTop = top;
					secondTop = top + arrowExtent;
					propTop = top + arrowExtent * 2;
				}
				else
				{
					propTop = top + arrowExtent;
					secondTop = propTop + propHeight;
				}
			}
			if (type == ScrollbarTypeTop) secondNegative = true;
			if (type == ScrollbarTypeBottom) firstNegative = false;
			DrawScrollbarArrow(ref platform, rastPort, firstLeft, firstTop,
				horizontal, firstNegative, width, height);
			DrawScrollbarArrow(ref platform, rastPort, secondLeft, secondTop,
				horizontal, secondNegative, width, height);
		}

		if (!TryReadPropRangeState(ref platform, state, obj,
			out var range)) return;
		var entries = range.Entries;
		var visible = range.Visible;
		var first = range.First;
		var span = horizontal ? propWidth : propHeight;
		var knob = entries == 0 ? span : (int)(visible * (uint)span / entries);
		var offset = entries == 0 ? 0 : (int)(first * (uint)span / entries);
		if (knob <= 0 && span > 0) knob = 1;
		if (offset < 0) offset = 0;
		if (offset + knob > span) offset = span - knob;
		if (offset < 0) offset = 0;
		if (knob > 0)
		{
			platform.SetPen(rastPort, 2);
			if (horizontal)
				platform.FillRectangle(rastPort, propLeft + offset, propTop,
					propLeft + offset + knob - 1, propTop + propHeight - 1);
			else
				platform.FillRectangle(rastPort, propLeft,
					propTop + propHeight - offset - knob,
					propLeft + propWidth - 1,
					propTop + propHeight - offset - 1);
		}
	}

	private static void DrawScrollbarArrow<TPlatform>(ref TPlatform platform,
		APTR rastPort, int left, int top, bool horizontal, bool negative,
		int parentWidth, int parentHeight) where TPlatform : struct, IMuiLayoutPlatform
	{
		var width = horizontal ? 16 : parentWidth;
		var height = horizontal ? parentHeight : 16;
		if (width <= 0 || height <= 0) return;
		var right = left + width - 1;
		var bottom = top + height - 1;
		platform.SetPen(rastPort, 3);
		platform.DrawLine(rastPort, left, top, right, top);
		platform.DrawLine(rastPort, left, top, left, bottom);
		platform.DrawLine(rastPort, left, bottom, right, bottom);
		platform.DrawLine(rastPort, right, top, right, bottom);
		var midX = left + width / 2;
		var midY = top + height / 2;
		if (horizontal)
		{
			var tip = negative ? left + 4 : right - 4;
			var basePoint = negative ? right - 4 : left + 4;
			platform.DrawLine(rastPort, tip, midY, basePoint, top + 4);
			platform.DrawLine(rastPort, tip, midY, basePoint, bottom - 4);
		}
		else
		{
			var tip = negative ? top + 4 : bottom - 4;
			var basePoint = negative ? bottom - 4 : top + 4;
			platform.DrawLine(rastPort, midX, tip, left + 4, basePoint);
			platform.DrawLine(rastPort, midX, tip, right - 4, basePoint);
		}
	}

	private static void DrawStringContent<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR rastPort, int left, int top, int width,
		int height, APTR font) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadStringContentsState(ref platform, state, obj,
			out var contentsState)) return;
		var contents = contentsState.Contents;
		var hasPresentation = TryReadStringPresentationState(ref platform, state,
			obj, out var presentation);
		var unicode = hasPresentation && presentation.Unicode != 0;
		var contentLength = StringCursorLength(ref platform, state, obj, contents);
		APTR drawText;
		var secret = false;
		if (contentLength == 0)
		{
			// Empty field shows the placeholder (dimmed) instead of nothing.
			if (!TryReadStringPlaceholderState(ref platform, state, obj,
				out var placeholder)) return;
			drawText = placeholder.Contents;
			contentLength = StringCursorLength(ref platform, state, obj, drawText);
			platform.SetPen(rastPort, 1);
		}
		else if (hasPresentation && presentation.Secret != 0)
		{
			// Secret gadgets mask every logical character with a dot.
			drawText = SecretMaskOwned(ref platform, state, obj, contentLength);
			contentLength = StringLength(ref platform, drawText);
			secret = true;
		}
		else
		{
			drawText = contents;
		}
		if (drawText.IsNull || contentLength <= 0) return;
		if (!TryReadStringCursorState(ref platform, state, obj,
			out var cursor)) return;
		EnsureStringCursorVisible(ref platform, state, obj, cursor.BufferPos);
		// Visibility may have advanced DisplayPos, so reread the canonical record
		// before choosing the byte span to draw.
		if (!TryReadStringCursorState(ref platform, state, obj,
			out cursor)) return;
		var display = cursor.DisplayPos;
		if (display < 0) display = 0;
		if (display > contentLength) display = contentLength;
		var drawStart = 0u;
		var drawLength = StringLength(ref platform, drawText);
		var drawnColumns = contentLength - display;
		if (unicode && !secret)
		{
			drawStart = StringByteOffset(ref platform, drawText, display, true);
			if (!CStringCodec.TryReadLength(ref platform, drawText, 65536,
				out var byteLength)) return;
			var visibleColumns = width > 0 ? unchecked((uint)(width / 8)) : 0u;
			var end = MuiStringscrollCore.ByteOffsetForColumns(ref platform,
				drawText, drawStart, byteLength, visibleColumns);
			drawLength = end > drawStart ? unchecked((int)(end - drawStart)) : 0;
			if (drawnColumns > visibleColumns) drawnColumns = unchecked((int)visibleColumns);
		}
		if (drawLength <= 0 || drawnColumns <= 0) return;

		// MUIA_String_Format aligns the visible text within the gadget. For UTF-8
		// the width is based on logical columns, while DrawText receives the
		// original byte span so the guest renderer can decode it.
		var format = hasPresentation ? presentation.Format : StringFormatLeft;
		var textWidth = unicode && !secret ? drawnColumns * 8 :
			platform.TextWidth(rastPort, font, APTR.FromPointer(drawText.Raw + drawStart),
				drawLength);
		var textLeft = left;
		if (format == StringFormatCenter)
			textLeft = left + (width - textWidth) / 2;
		else if (format == StringFormatRight)
			textLeft = left + width - textWidth;
		if (textLeft < left) textLeft = left;
		platform.DrawText(rastPort, font, textLeft, top + height,
			APTR.FromPointer(drawText.Raw + drawStart), drawLength);
	}

	private static void DrawImageContent<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR rastPort, int left, int top, int width,
		int height) where TPlatform : struct, IMuiLayoutPlatform
	{
		var imageSpecState = default(MuiImageSpecState);
		if (TryReadImageSpecState(ref platform, state, obj, out imageSpecState))
		{
			if (imageSpecState.Present)
			{
				var rawSpec = imageSpecState.Raw;
				if (rawSpec <= MUIImageBuiltinMax)
				{
					DrawBuiltinImage(ref platform, state, obj, rastPort, rawSpec,
						left, top, width, height);
						return;
				}
				var spec = APTR.FromPointer(rawSpec);
				if (spec.IsNotNull && TryParseImageSpec(ref platform, spec,
					out var parsed))
				{
					StoreResolvedSpec(ref platform, state, obj, parsed);
					switch (parsed.Kind)
					{
						case MuiImageSpecKind.BackgroundPattern:
						case MuiImageSpecKind.BuiltinImage:
						case MuiImageSpecKind.Preconfigured:
							if (parsed.Value <= MUIImageBuiltinMax)
								DrawBuiltinImage(ref platform, state, obj, rastPort,
									parsed.Value, left, top, width, height);
							return;
						case MuiImageSpecKind.Color:
							platform.SetPen(rastPort, PackColor(parsed));
							platform.FillRectangle(rastPort, left, top,
								left + width - 1, top + height - 1);
							return;
						case MuiImageSpecKind.BoopsiImage:
						case MuiImageSpecKind.Brush:
						case MuiImageSpecKind.Picture:
							// Resolving external classes, brushes, and datatypes needs
							// services outside this freestanding common-control core.
							return;
					}
				}
				else if (spec.IsNotNull)
				{
					// A non-spec pointer is the conventional Intuition Image
					// structure accepted by MUIA_Image_Spec in legacy programs.
					platform.DrawImage(rastPort, spec, left, top, width, height);
					return;
				}
			}
			if (imageSpecState.BuiltinPresent &&
				imageSpecState.Builtin <= MUIImageBuiltinMax)
			{
				DrawBuiltinImage(ref platform, state, obj, rastPort,
					imageSpecState.Builtin, left, top, width, height);
				return;
			}
		}
		var oldImageState = default(MuiImageOldImageState);
		if (TryReadImageOldImageState(ref platform, state, obj,
			out oldImageState) && oldImageState.Image.IsNotNull)
			platform.DrawImage(rastPort, oldImageState.Image, left, top, width,
				height);
	}

	private static void DrawBuiltinImage<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR rastPort, uint imageId, int left, int top,
		int width, int height) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (width <= 0 || height <= 0 || imageId > MUIImageBuiltinMax) return;
		var imageRender = default(MuiImageRenderState);
		if (!TryReadImageRenderState(ref platform, state, obj,
			out imageRender)) return;
		var selected = imageRender.ShowSelState != 0 &&
			(imageRender.ImageState != 0 || imageRender.Selected != 0);
		if (!TryReadAreaPresentationState(ref platform, state, obj,
			out var areaPresentation)) return;
		var disabled = areaPresentation.Disabled != 0;
		var pen = disabled ? 1u : selected ? 3u : 2u;
		platform.SetPen(rastPort, pen);
		var right = left + width - 1;
		var bottom = top + height - 1;
		switch (imageId)
		{
			case 0x0Bu: // MUII_ArrowUp
				platform.DrawLine(rastPort, left + width / 2, top,
					left, bottom);
				platform.DrawLine(rastPort, left + width / 2, top,
					right, bottom);
				platform.DrawLine(rastPort, left, bottom, right, bottom);
				break;
			case 0x0Cu: // MUII_ArrowDown
				platform.DrawLine(rastPort, left, top, right, top);
				platform.DrawLine(rastPort, left, top, left + width / 2, bottom);
				platform.DrawLine(rastPort, right, top, left + width / 2, bottom);
				break;
			case 0x0Du: // MUII_ArrowLeft
				platform.DrawLine(rastPort, left, top + height / 2, right, top);
				platform.DrawLine(rastPort, left, top + height / 2, right, bottom);
				platform.DrawLine(rastPort, right, top, right, bottom);
				break;
			case 0x0Eu: // MUII_ArrowRight
				platform.DrawLine(rastPort, left, top, right, top + height / 2);
				platform.DrawLine(rastPort, left, bottom, right, top + height / 2);
				platform.DrawLine(rastPort, left, top, left, bottom);
				break;
			default:
				platform.FillRectangle(rastPort, left, top, right, bottom);
				break;
		}
	}

	private static bool GetRenderPort<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out APTR rastPort) where TPlatform : struct, IMuiLayoutPlatform
	{
		rastPort = APTR.Null;
		var raw = Read(ref platform, state, obj, RenderInfoAttr, 0);
		if (raw == 0) return false;
		var renderInfo = APTR.FromPointer(raw);
		if (!MuiDrawingRenderInfoCodec.TryRead(ref platform, renderInfo,
			out var renderInfoValue)) return false;
		rastPort = renderInfoValue.RastPort;
		return rastPort.IsNotNull;
	}

	// ---- Bitmap-family Setup / Cleanup remapped-state exposure ----------------

	public static bool SetupBitmap<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR renderInfo) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!MuiAreaLayoutCore.Setup(ref platform, state, obj, renderInfo))
			return false;
		if (!RebuildBitmapRemap(ref platform, state, obj))
		{
			MuiAreaLayoutCore.Cleanup(ref platform, state, obj);
			return false;
		}
		return true;
	}

	// Build (or rebuild) the remapped/decoded pixels for a bitmap-family object.
	// For Bodychunk this decodes the BODY chunk into owned setup storage; for
	// Bitmap it points the remapped state at the caller-owned BitMap. Any prior
	// decoded storage is retired first so a re-decode after a source change does
	// not leak. On a missing/invalid source the remapped pointer is cleared so
	// no stale pixels are drawn.
	private static bool RebuildBitmapRemap<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiLayoutPlatform
	{
		var cls = Classify(ref platform, state, obj);
		if (cls == MuiControlClass.Bodychunk && MuiStoreCore.DataspaceFind(
			ref platform, state, obj, BodychunkDecodedKey).IsNotNull)
			MuiStoreCore.DataspaceRemove(ref platform, state, obj,
				BodychunkDecodedKey);
		var source = cls == MuiControlClass.Bodychunk ?
			PrepareBodychunk(ref platform, state, obj) :
			ReadBitmapSource(ref platform, state, obj, cls);
		if (source.IsNull)
		{
			var cleared = default(MuiBitmapRemappedState);
			if (!PublishBitmapRemappedState(ref platform, state, obj, cleared))
				return false;
			return false;
		}
		var remapped = default(MuiBitmapRemappedState);
		remapped.Remapped = source;
		return PublishBitmapRemappedState(ref platform, state, obj, remapped);
	}

	// Runtime set of a settable Bitmap/Bodychunk source or geometry attribute.
	// When the object is currently set up (its remapped/decoded state is live),
	// the change invalidates and rebuilds that state so fresh pixels are
	// honoured; a change then notifies once and schedules a redraw.
	private static bool SetBitmapFamilyAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, attribute,
			out var current) && current == value) return true;
		var live = ReadRaw(ref platform, state, obj, BitmapRemapped, 0) != 0;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
			value, false)) return false;
		if (attribute == BitmapBitmap || attribute == BodychunkBody)
		{
			var source = default(MuiBitmapSourceState);
			source.Source = APTR.FromPointer(value);
			if (!PublishBitmapSourceState(ref platform, state, obj, attribute,
				source)) return false;
		}
		if (attribute == BitmapWidth || attribute == BitmapHeight)
		{
			var geometry = default(MuiBitmapGeometryState);
			geometry.Width = ReadRaw(ref platform, state, obj, BitmapWidth, 0);
			geometry.Height = ReadRaw(ref platform, state, obj, BitmapHeight, 0);
			if (!PublishBitmapGeometryState(ref platform, state, obj, geometry))
				return false;
		}
		if (Classify(ref platform, state, obj) == MuiControlClass.Bodychunk &&
			(attribute == BodychunkCompression || attribute == BodychunkDepth ||
			attribute == BodychunkMasking))
		{
			var format = default(MuiBodychunkFormatState);
			format.Compression = ReadRaw(ref platform, state, obj,
				BodychunkCompression, 0);
			format.Depth = ReadRaw(ref platform, state, obj, BodychunkDepth, 1);
			format.Masking = ReadRaw(ref platform, state, obj, BodychunkMasking, 0);
			if (!PublishBodychunkFormatState(ref platform, state, obj, format))
				return false;
		}
		if (Classify(ref platform, state, obj) == MuiControlClass.Bitmap &&
			(attribute == BitmapAlpha || attribute == BitmapMappingTable ||
			attribute == BitmapPrecision || attribute == BitmapSourceColors ||
			attribute == BitmapTransparent || attribute == BitmapUseFriend))
		{
			if (!TryReadBitmapPolicyState(ref platform, state, obj,
				out var policy)) return false;
			if (attribute == BitmapAlpha) policy.Alpha = value;
			else if (attribute == BitmapMappingTable) policy.MappingTable = value;
			else if (attribute == BitmapPrecision) policy.Precision = value;
			else if (attribute == BitmapSourceColors) policy.SourceColors = value;
			else if (attribute == BitmapTransparent) policy.Transparent = value;
			else policy.UseFriend = value;
			if (!PublishBitmapPolicyState(ref platform, state, obj, policy))
				return false;
		}
		if (live) RebuildBitmapRemap(ref platform, state, obj);
		if (notify && !MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			attribute, value, true)) return false;
		return platform.ScheduleRedraw(obj, 2);
	}

	public static bool CleanupBitmap<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiLayoutPlatform
	{
		var cleared = default(MuiBitmapRemappedState);
		if (!PublishBitmapRemappedState(ref platform, state, obj, cleared))
			return false;
		if (Classify(ref platform, state, obj) == MuiControlClass.Bodychunk &&
			MuiStoreCore.DataspaceFind(ref platform, state, obj,
				BodychunkDecodedKey).IsNotNull)
			MuiStoreCore.DataspaceRemove(ref platform, state, obj,
				BodychunkDecodedKey);
		return MuiAreaLayoutCore.Cleanup(ref platform, state, obj);
	}

	private static APTR PrepareBodychunk<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiLayoutPlatform
	{
		var source = ReadBitmapSource(ref platform, state, obj,
			MuiControlClass.Bodychunk);
		if (!TryReadBitmapGeometryState(ref platform, state, obj,
			out var geometry)) return APTR.Null;
		var width = geometry.Width;
		var height = geometry.Height;
		if (!TryReadBodychunkFormatState(ref platform, state, obj,
			out var format)) return APTR.Null;
		var depth = format.Depth;
		var masking = format.Masking;
		var compression = format.Compression;
		if (source.IsNull || width == 0 || height == 0 || depth == 0 || depth > 32 ||
			masking > 1 || compression > 1 || width > uint.MaxValue - 15)
			return APTR.Null;
		var rowBytes = ((width + 15) >> 4) * 2;
		var planes = depth + (masking == 1 ? 1u : 0u);
		const uint maximumDecoded = 16u * 1024u * 1024u;
		if (rowBytes == 0 || height > maximumDecoded / rowBytes)
			return APTR.Null;
		var planeSize = rowBytes * height;
		if (planes == 0 || planeSize > maximumDecoded / planes)
			return APTR.Null;
		var decodedSize = planeSize * planes;
		var scratch = MuiHeadlessMemory.Allocate(ref platform, decodedSize);
		if (scratch.IsNull) return APTR.Null;
		var decoded = compression == 0 ?
			CopyBody(ref platform, source, scratch, decodedSize) :
			DecodeByteRun1(ref platform, source, scratch, decodedSize);
		if (!decoded || !MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			BodychunkDecodedKey, scratch, unchecked((int)decodedSize)))
		{
			platform.Clear(scratch, decodedSize);
			platform.Free(scratch, decodedSize);
			return APTR.Null;
		}
		platform.Clear(scratch, decodedSize);
		platform.Free(scratch, decodedSize);
		return MuiStoreCore.DataspaceFind(ref platform, state, obj,
			BodychunkDecodedKey);
	}

	private static bool CopyBody<TPlatform>(ref TPlatform platform, APTR source,
		APTR destination, uint size) where TPlatform : struct, IMuiGuestMemory
	{
		if (!platform.IsMapped(source, size) || !platform.IsMapped(destination, size))
			return false;
		platform.Copy(source, destination, size);
		return true;
	}

	private static bool DecodeByteRun1<TPlatform>(ref TPlatform platform,
		APTR source, APTR destination, uint outputSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		uint input = 0;
		uint output = 0;
		while (output < outputSize)
		{
			if (source.Raw > uint.MaxValue - input ||
				!platform.IsMapped(APTR.FromPointer(source.Raw + input), 1)) return false;
			var control = platform.ReadUInt8(source, unchecked((int)input));
			input++;
			if (control == 128) continue;
			if (control < 128)
			{
				var count = (uint)control + 1;
				if (count > outputSize - output || source.Raw > uint.MaxValue - input ||
					destination.Raw > uint.MaxValue - output) return false;
				var from = APTR.FromPointer(source.Raw + input);
				var to = APTR.FromPointer(destination.Raw + output);
				if (!platform.IsMapped(from, count) || !platform.IsMapped(to, count))
					return false;
				platform.Copy(from, to, count);
				input += count;
				output += count;
				continue;
			}
			var repeat = 257u - control;
			if (repeat > outputSize - output || source.Raw > uint.MaxValue - input ||
				!platform.IsMapped(APTR.FromPointer(source.Raw + input), 1)) return false;
			var value = platform.ReadUInt8(source, unchecked((int)input));
			input++;
			for (uint index = 0; index < repeat; index++)
				platform.WriteUInt8(destination, unchecked((int)(output + index)), value);
			output += repeat;
		}
		return true;
	}

	// ---- Shared helpers -------------------------------------------------------

	private static int CountEntries<TPlatform>(ref TPlatform platform, APTR entries)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (entries.IsNull) return 0;
		var cursor = default(MuiChoiceEntryCursor);
		cursor.Base = entries;
		for (var count = 0; count < 4096; count++)
		{
			cursor.Index = unchecked((uint)count);
			if (!MuiChoiceEntryCursorCodec.TryGetEntry(ref platform, cursor,
				out var slot)) return 0;
			if (!MuiChoiceEntryCodec.TryRead(ref platform, slot,
				out var entry)) return 0;
			if (entry.Text.IsNull) return count;
		}
		return 0;
	}

	private static APTR ChoiceEntry<TPlatform>(ref TPlatform platform,
		APTR entries, uint active) where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiChoiceEntryCursor);
		cursor.Base = entries;
		cursor.Index = active;
		if (!MuiChoiceEntryCursorCodec.TryGetEntry(ref platform, cursor,
			out var slot)) return APTR.Null;
		return MuiChoiceEntryCodec.TryRead(ref platform, slot,
			out var entry) ? entry.Text : APTR.Null;
	}

	private static uint Read<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint defaultValue)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out var value) ? value : defaultValue;

	private static uint ReadRaw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint defaultValue)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, attribute,
			out var value) ? value : defaultValue;

	// ---- MUIA_Image_Spec parsing ----------------------------------------------

	// Parses a MUIA_Image_Spec string of the form "kind:value" from guest
	// memory. Returns false (Kind = Invalid) when the pointer does not address
	// a well-formed spec string, in which case callers treat the value as an
	// opaque drawable rather than a spec.
	public static bool TryParseImageSpec<TPlatform>(ref TPlatform platform,
		APTR spec, out MuiImageSpec result)
		where TPlatform : struct, IMuiGuestMemory
	{
		result = default;
		result.Kind = MuiImageSpecKind.Invalid;
		if (spec.IsNull || !platform.IsMapped(spec, 2)) return false;
		var lead = platform.ReadUInt8(spec, 0);
		if (lead < (byte)'0' || lead > (byte)'6') return false;
		if (platform.ReadUInt8(spec, 1) != (byte)':') return false;
		var kind = (MuiImageSpecKind)(lead - (byte)'0');
		if (kind == MuiImageSpecKind.Color)
			return TryParseSpecColor(ref platform, spec, ref result);
		if (kind == MuiImageSpecKind.BoopsiImage ||
			kind == MuiImageSpecKind.Brush || kind == MuiImageSpecKind.Picture)
			return TryParseNamedSpec(ref platform, spec, kind, ref result);
		if (!TryParseSpecDecimal(ref platform, spec, out var id)) return false;
		result.Kind = kind;
		result.Value = id;
		return true;
	}

	private static bool TryParseSpecColor<TPlatform>(ref TPlatform platform,
		APTR spec, ref MuiImageSpec result)
		where TPlatform : struct, IMuiGuestMemory
	{
		var length = 0;
		while (length <= 24)
		{
			var offset = 2 + length;
			if (!platform.IsMapped(spec, (uint)(offset + 1))) break;
			var ch = platform.ReadUInt8(spec, offset);
			if (ch == 0 || HexNibble(ch) < 0) break;
			length++;
		}
		if (!platform.IsMapped(spec, (uint)(2 + length + 1)) ||
			platform.ReadUInt8(spec, 2 + length) != 0) return false;
		uint red, green, blue;
		if (length == 6)
		{
			red = HexByte(ref platform, spec, 0);
			green = HexByte(ref platform, spec, 2);
			blue = HexByte(ref platform, spec, 4);
		}
		else if (length == 24)
		{
			// Eight hex digits per channel; use the high byte of each channel.
			red = HexByte(ref platform, spec, 0);
			green = HexByte(ref platform, spec, 8);
			blue = HexByte(ref platform, spec, 16);
		}
		else
		{
			return false;
		}
		result.Kind = MuiImageSpecKind.Color;
		result.Red = red;
		result.Green = green;
		result.Blue = blue;
		result.Value = PackColor(result);
		return true;
	}

	private static bool TryParseNamedSpec<TPlatform>(ref TPlatform platform,
		APTR spec, MuiImageSpecKind kind, ref MuiImageSpec result)
		where TPlatform : struct, IMuiGuestMemory
	{
		for (var length = 2; length < 258; length++)
		{
			if (!platform.IsMapped(spec, (uint)(length + 1))) return false;
			if (platform.ReadUInt8(spec, length) == 0)
			{
				if (length == 2) return false;
				result.Kind = kind;
				result.Value = 0;
				return true;
			}
		}
		return false;
	}

	private static bool TryParseSpecDecimal<TPlatform>(ref TPlatform platform,
		APTR spec, out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var digits = 0;
		while (digits < 9)
		{
			var offset = 2 + digits;
			if (!platform.IsMapped(spec, (uint)(offset + 1))) break;
			var ch = platform.ReadUInt8(spec, offset);
			if (ch < (byte)'0' || ch > (byte)'9') break;
			value = value * 10u + (uint)(ch - (byte)'0');
			digits++;
		}
		return digits > 0 && platform.IsMapped(spec, (uint)(2 + digits + 1)) &&
			platform.ReadUInt8(spec, 2 + digits) == 0;
	}

	private static uint HexByte<TPlatform>(ref TPlatform platform, APTR spec,
		int nibbleIndex) where TPlatform : struct, IMuiGuestMemory
	{
		var high = HexNibble(platform.ReadUInt8(spec, 2 + nibbleIndex));
		var low = HexNibble(platform.ReadUInt8(spec, 2 + nibbleIndex + 1));
		return (uint)((high << 4) | low);
	}

	private static int HexNibble(byte ch)
	{
		if (ch >= (byte)'0' && ch <= (byte)'9') return ch - (byte)'0';
		if (ch >= (byte)'a' && ch <= (byte)'f') return ch - (byte)'a' + 10;
		if (ch >= (byte)'A' && ch <= (byte)'F') return ch - (byte)'A' + 10;
		return -1;
	}

	private static uint PackColor(MuiImageSpec spec) =>
		((spec.Red & 0xFF) << 16) | ((spec.Green & 0xFF) << 8) | (spec.Blue & 0xFF);

	private static void StoreResolvedSpec<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiImageSpec spec)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ImageResolvedKindKey, unchecked((uint)(int)spec.Kind), false);
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ImageResolvedValueKey, spec.Value, false);
	}
}

public static class MuiCommonControlDispatcher
{
	private const uint OmGet = MuiCommonControlPacketCore.OmGet;
	private const uint Set = MuiCommonControlPacketCore.Set;
	private const uint NoNotifySet = MuiCommonControlPacketCore.NoNotifySet;
	private const uint NumericDecrease = MuiCommonControlPacketCore.NumericDecrease;
	private const uint NumericIncrease = MuiCommonControlPacketCore.NumericIncrease;
	private const uint NumericScaleToValue = MuiCommonControlPacketCore.NumericScaleToValue;
	private const uint NumericSetDefault = MuiCommonControlPacketCore.NumericSetDefault;
	private const uint NumericStringify = MuiCommonControlPacketCore.NumericStringify;
	private const uint NumericValueToScale = MuiCommonControlPacketCore.NumericValueToScale;
	private const uint PropDecrease = MuiCommonControlPacketCore.PropDecrease;
	private const uint PropIncrease = MuiCommonControlPacketCore.PropIncrease;
	private const uint HandleEvent = MuiCommonControlPacketCore.HandleEvent;
	private const uint AskMinMax = MuiCommonControlPacketCore.AskMinMax;
	private const uint Layout = MuiCommonControlPacketCore.Layout;
	private const uint Draw = MuiCommonControlPacketCore.Draw;
	private const uint Setup = MuiCommonControlPacketCore.Setup;
	private const uint Cleanup = MuiCommonControlPacketCore.Cleanup;
	private const uint CreateShortHelp = MuiAreaShortHelpMessageCodec.CreateShortHelp;
	private const uint DeleteShortHelp = MuiAreaShortHelpMessageCodec.DeleteShortHelp;

	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!MuiCommonControlPacketCore.TryReadMethodId(ref platform, message,
			out var methodPacket)) return 0;
		var method = methodPacket.MethodId;
			switch (method)
		{
			case CreateShortHelp:
				if (!MuiAreaShortHelpMessageCodec.TryReadCreate(ref platform, message,
					out var createShortHelp)) return 0;
				if (MuiCommonControlCore.Classify(ref platform, state, obj) ==
					MuiControlClass.Unknown) break;
				return MuiAreaShortHelpPacketCore.Create(ref platform, state, obj,
					createShortHelp.MouseX, createShortHelp.MouseY).Raw;
			case DeleteShortHelp:
				if (!MuiAreaShortHelpMessageCodec.TryReadDelete(ref platform, message,
					out var deleteShortHelp)) return 0;
				if (MuiCommonControlCore.Classify(ref platform, state, obj) ==
					MuiControlClass.Unknown) break;
				return MuiAreaShortHelpPacketCore.Delete(ref platform, state, obj,
					deleteShortHelp.Help) ? 1u : 0u;
			case NumericDecrease:
			case NumericIncrease:
				if (!MuiCommonControlCore.IsNumericClass(MuiCommonControlCore.Classify(
					ref platform, state, obj))) return 0;
				if (!MuiCommonControlPacketCore.TryReadSigned(ref platform, message,
					method, out var signedPacket)) return 0;
				var amount = signedPacket.Value;
				if (method == NumericDecrease) amount = -amount;
				return MuiCommonControlCore.ChangeNumeric(ref platform, state, obj,
					amount) ? 1u : 0u;
			case NumericScaleToValue:
				if (!MuiCommonControlCore.IsNumericClass(MuiCommonControlCore.Classify(
					ref platform, state, obj))) return 0;
				if (!MuiCommonControlPacketCore.TryReadScaleToValue(ref platform,
					message, out var scaleToValue)) return 0;
				return unchecked((uint)MuiCommonControlCore.ScaleToValue(ref platform,
					state, obj, scaleToValue.Min, scaleToValue.Max,
					scaleToValue.Value));
			case NumericSetDefault:
				if (!MuiCommonControlCore.IsNumericClass(MuiCommonControlCore.Classify(
					ref platform, state, obj))) return 0;
				return MuiCommonControlCore.SetNumericDefault(ref platform, state, obj) ?
					1u : 0u;
			case NumericValueToScale:
				if (!MuiCommonControlCore.IsNumericClass(MuiCommonControlCore.Classify(
					ref platform, state, obj))) return 0;
				if (!MuiCommonControlPacketCore.TryReadValueToScale(ref platform,
					message, out var valueToScale)) return 0;
				return unchecked((uint)MuiCommonControlCore.ValueToScale(ref platform,
					state, obj, valueToScale.Min, valueToScale.Max));
			case NumericStringify:
				if (!MuiCommonControlCore.IsNumericClass(MuiCommonControlCore.Classify(
					ref platform, state, obj))) return 0;
				if (!MuiCommonControlPacketCore.TryReadStringify(ref platform,
					message, out var stringify)) return 0;
				return MuiCommonControlCore.StringifyOwned(ref platform, state, obj,
					stringify.Value).Raw;
			case PropDecrease:
			case PropIncrease:
				if (!MuiCommonControlCore.IsPropClass(MuiCommonControlCore.Classify(
					ref platform, state, obj))) return 0;
				if (!MuiCommonControlPacketCore.TryReadSigned(ref platform, message,
					method, out var propPacket)) return 0;
				var delta = propPacket.Value;
				if (method == PropDecrease) delta = -delta;
				return MuiCommonControlCore.ChangeProp(ref platform, state, obj, delta) ?
					1u : 0u;
			case HandleEvent:
				if (!MuiCommonControlPacketCore.TryReadHandleEvent(ref platform,
					message, out var handleEvent)) return 0;
				return MuiCommonControlCore.HandleEvent(ref platform, state, obj,
					APTR.FromPointer(handleEvent.InputMessage),
					handleEvent.MuiKey);
			case OmGet:
				// struct opGet { ULONG MethodID; ULONG opg_AttrID;
				// ULONG *opg_Storage; }
				if (!MuiCommonControlPacketCore.TryReadGet(ref platform, message,
					out var getPacket)) return 0;
				if (MuiCommonControlCore.Classify(ref platform, state, obj) ==
					MuiControlClass.Unknown &&
					!MuiApplicationMessageCore.IsPublicGetterAttribute(getPacket.Attribute) &&
					!MuiApplicationWindowCore.IsPublicGetterAttribute(getPacket.Attribute) &&
					!MuiWindowPublicCore.IsPublicGetterAttribute(getPacket.Attribute) &&
					!MuiApplicationCommandsCore.IsPublicGetterAttribute(getPacket.Attribute) &&
					!MuiApplicationWindowListCore.IsPublicGetterAttribute(getPacket.Attribute) &&
					!MuiGroupPageCore.IsPublicGetterAttribute(getPacket.Attribute) &&
					!MuiGroupGridCore.IsGridAttribute(getPacket.Attribute) &&
					!MuiGroupChildrenCore.IsPublicGetterAttribute(getPacket.Attribute) &&
					!MuiGroupChildrenCore.IsFamilyPublicGetterAttribute(getPacket.Attribute) &&
					!MuiObjectMetadataCore.IsPublicGetterAttribute(getPacket.Attribute) &&
					!MuiGroupLayoutHookCore.IsPublicGetterAttribute(getPacket.Attribute) &&
					!MuiGroupLayoutCore.IsPublicGetterAttribute(getPacket.Attribute)) break;
				var storage = APTR.FromPointer(getPacket.Storage);
				if (storage.IsNull || !platform.IsMapped(storage,
					MuiGuestUlongStorage.Size)) return 0;
				if (MuiCommonControlCore.TryGet(ref platform, state, obj,
					getPacket.Attribute, out var controlValue, out var controlHandled) &&
					controlHandled)
				{
					MuiGuestUlongStorageCodec.WriteValue(ref platform, storage,
						controlValue);
					return 1u;
				}
				if (controlHandled) return 0;
				if (!MuiStringScrollAttributeCore.Get(ref platform, state, obj,
					getPacket.Attribute, out var value)) return 0;
				MuiGuestUlongStorageCodec.WriteValue(ref platform, storage, value);
				return 1u;
			case Set:
			case NoNotifySet:
				if (!MuiCommonControlPacketCore.TryReadAttribute(ref platform,
					message, method, out var setPacket)) break;
				if (setPacket.Attribute == MuiGroupLayoutHookCore.Attribute)
					return 0;
				var setClass = MuiCommonControlCore.Classify(ref platform, state, obj);
				if (setClass ==
					MuiControlClass.Unknown) break;
				var setAttribute = setPacket.Attribute;
				if (setClass == MuiControlClass.String &&
					(setAttribute == MuiStringScrollAttributeCore.ScrollLeft ||
						setAttribute == MuiStringScrollAttributeCore.ScrollTop))
					return MuiStringScrollAttributeCore.Set(ref platform, state, obj,
						setAttribute, setPacket.Value, method == Set) ? 1u : 0u;
				return MuiCommonControlCore.SetControlAttribute(ref platform, state,
					obj, setAttribute,
					setPacket.Value, method == Set) ? 1u : 0u;
			case AskMinMax:
				if (!MuiCommonControlPacketCore.TryReadAskMinMax(ref platform,
					message, out var askMinMax)) return 0;
				if (MuiCommonControlCore.Classify(ref platform, state, obj) ==
					MuiControlClass.Unknown) break;
				return MuiCommonControlCore.AskMinMax(ref platform, state, obj,
					APTR.FromPointer(askMinMax.Storage)) ? 1u : 0u;
			case Layout:
				if (!MuiCommonControlPacketCore.TryReadLayout(ref platform, message,
					out var layoutPacket)) return 0;
				var layoutClass = MuiCommonControlCore.Classify(ref platform, state,
					obj);
				if (layoutClass != MuiControlClass.Radio &&
					layoutClass != MuiControlClass.Scrollbar) break;
				if (layoutClass == MuiControlClass.Scrollbar)
					return MuiCommonControlCore.LayoutScrollbar(ref platform, state, obj,
						unchecked((int)layoutPacket.Left), unchecked((int)layoutPacket.Top),
						unchecked((int)layoutPacket.Width),
						unchecked((int)layoutPacket.Height)) ?
						1u : 0u;
				return MuiGroupLayoutCore.Layout(ref platform, state, obj,
					unchecked((int)layoutPacket.Left), unchecked((int)layoutPacket.Top),
					unchecked((int)layoutPacket.Width),
					unchecked((int)layoutPacket.Height)) ?
					1u : 0u;
			case Draw:
				if (!MuiCommonControlPacketCore.TryReadDraw(ref platform, message,
					out var drawPacket)) return 0;
				if (MuiCommonControlCore.Classify(ref platform, state, obj) ==
					MuiControlClass.Unknown) break;
				return MuiCommonControlCore.DrawControl(ref platform, state, obj,
					drawPacket.Flags) ? 1u : 0u;
			case Setup:
				if (!MuiCommonControlPacketCore.TryReadSetup(ref platform, message,
					out var setupPacket)) return 0;
				var setupClass = MuiCommonControlCore.Classify(ref platform, state,
					obj);
				if (setupClass != MuiControlClass.Bitmap &&
					setupClass != MuiControlClass.Bodychunk) break;
				return MuiCommonControlCore.SetupBitmap(ref platform, state, obj,
					APTR.FromPointer(setupPacket.RenderInfo)) ? 1u : 0u;
			case Cleanup:
				var cleanupClass = MuiCommonControlCore.Classify(ref platform, state,
					obj);
				if (cleanupClass != MuiControlClass.Bitmap &&
					cleanupClass != MuiControlClass.Bodychunk) break;
				return MuiCommonControlCore.CleanupBitmap(ref platform, state, obj) ?
					1u : 0u;
		}
		return MuiLayoutDispatcher.Dispatch(ref platform, state, obj, message);
	}

}
