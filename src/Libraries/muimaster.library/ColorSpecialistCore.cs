/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Fixed guest-memory layout for the MG09 pen/color specialist family
// (Pendisplay.mui, Colorfield.mui, Coloradjust.mui, Palette.mui and the private
// Penadjust.mui). Each specialist owns a dedicated, initialized guest-resident
// instance block plus, for the classes that need them, class-owned copied
// blocks (a 32-byte MUI_PenSpec black box and/or a 12-byte generated
// MUI_RGBColor). The family never allocates on the managed heap, holds no
// managed data, and never interprets the black-box MUI_PenSpec crossing the MG09
// pen capability. Pen tokens are always obtained and released through
// MuiDrawingServiceCore so the frozen drawing-service pen tracking stays the
// single owner of live pens; this family never bypasses it. The layout is
// deliberately independent of MuiHeadlessObjectCore: the specialists carry their
// own lifecycle and rollback and do not reach the generic object registry.
internal static class MuiColorSpecialistLayout
{
	public const uint Magic = 0x4D434F4C;   // "MCOL"

	// Instance block.
	public const uint InstanceSize = 64;
	// The instance wire shape is represented by MuiColorSpecialistState below;
	// field offsets are confined to its codec.

	// Flags.
	public const uint FlagSetupActive = 1u << 0;
	public const uint FlagPenHeld = 1u << 1;
	public const uint FlagGroupable = 1u << 2;   // Palette (default set)
	public const uint FlagShowAlpha = 1u << 3;   // Coloradjust
	public const uint FlagPSIMode = 1u << 4;     // Penadjust (private)
	public const uint FlagObsolete = 1u << 5;    // Palette classification marker

	// Owned block sizes.
	public const uint SpecSize = 32;   // struct MUI_PenSpec (explicit black box)
	public const uint RgbSize = 12;    // struct MUI_RGBColor: three ULONGs

	// Internal MUI_PenSpec encoding written by the Pendisplay Set* methods. The
	// drawing service treats the 32-byte spec as an opaque black box, but the
	// Pendisplay class is the component that authors the spec, so it uses the
	// bounded named record below. It is never parsed by the drawing service.
	public const uint SpecKindColormap = 1;
	public const uint SpecKindMuiPen = 2;
	public const uint SpecKindRgb = 3;

	// Fallback rendering pen used when no shared pen is currently held (a
	// disabled/not-set-up draw still paints, using the background pen 0).
	public const uint FallbackPen = 0;

	// render info is exactly 28 bytes (see MuiDrawingRenderInfoRecord.Size).
	public const uint RenderInfoSize = 28;
}

// MUI_PenSpec is opaque to the drawing service, but Pendisplay owns and
// authors this bounded 32-byte copy. Keep its internal representation named so
// specialist logic never depends on individual guest offsets. The final three
// words are reserved opaque payload preserved by the codec when a caller
// supplies an existing spec.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiColorPenSpecRecord
{
	internal const uint Size = MuiColorSpecialistLayout.SpecSize;
	internal const uint FieldSize = 4;
	internal const uint KindOffset = 0;
	internal const uint ScalarOffset = 4;
	internal const uint RedOffset = 8;
	internal const uint GreenOffset = 12;
	internal const uint BlueOffset = 16;
	internal const uint Reserved0Offset = 20;
	internal const uint Reserved1Offset = 24;
	internal const uint Reserved2Offset = 28;

	internal uint Kind;
	internal uint Scalar;
	internal uint Red;
	internal uint Green;
	internal uint Blue;
	internal uint Reserved0;
	internal uint Reserved1;
	internal uint Reserved2;
}

internal enum MuiColorRecordKind : byte
{
	PenSpec,
	State,
	Rgb,
}

internal enum MuiColorRecordField : byte
{
	Kind,
	Scalar,
	Red,
	Green,
	Blue,
	Reserved0,
	Reserved1,
	Reserved2,
	Magic,
	Class,
	Flags,
	RenderInfo,
	DrawState,
	Pen,
	SpecBlock,
	RgbBlock,
	Reference,
	ModeID,
	Alpha,
	Entries,
	Names,
	NotifyAttribute,
	NotifyValue,
	NotifyCount,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiColorRecordFieldCursor
{
	internal APTR Address;
	internal MuiColorRecordKind Record;
	internal MuiColorRecordField Field;
}

internal static class MuiColorRecordMemoryCodec
{
	private static bool TryResolve(MuiColorRecordKind record,
		MuiColorRecordField field, out uint offset, out uint size)
	{
		offset = 0;
		size = 0;
		if (record == MuiColorRecordKind.PenSpec)
		{
			size = MuiColorPenSpecRecord.Size;
			if (field == MuiColorRecordField.Kind)
				offset = MuiColorPenSpecRecord.KindOffset;
			else if (field == MuiColorRecordField.Scalar)
				offset = MuiColorPenSpecRecord.ScalarOffset;
			else if (field == MuiColorRecordField.Red)
				offset = MuiColorPenSpecRecord.RedOffset;
			else if (field == MuiColorRecordField.Green)
				offset = MuiColorPenSpecRecord.GreenOffset;
			else if (field == MuiColorRecordField.Blue)
				offset = MuiColorPenSpecRecord.BlueOffset;
			else if (field == MuiColorRecordField.Reserved0)
				offset = MuiColorPenSpecRecord.Reserved0Offset;
			else if (field == MuiColorRecordField.Reserved1)
				offset = MuiColorPenSpecRecord.Reserved1Offset;
			else if (field == MuiColorRecordField.Reserved2)
				offset = MuiColorPenSpecRecord.Reserved2Offset;
			else return false;
			return true;
		}
		if (record == MuiColorRecordKind.State)
		{
			size = MuiColorSpecialistState.Size;
			if (field == MuiColorRecordField.Magic)
				offset = MuiColorSpecialistState.MagicOffset;
			else if (field == MuiColorRecordField.Class)
				offset = MuiColorSpecialistState.ClassOffset;
			else if (field == MuiColorRecordField.Flags)
				offset = MuiColorSpecialistState.FlagsOffset;
			else if (field == MuiColorRecordField.RenderInfo)
				offset = MuiColorSpecialistState.RenderInfoOffset;
			else if (field == MuiColorRecordField.DrawState)
				offset = MuiColorSpecialistState.DrawStateOffset;
			else if (field == MuiColorRecordField.Pen)
				offset = MuiColorSpecialistState.PenOffset;
			else if (field == MuiColorRecordField.SpecBlock)
				offset = MuiColorSpecialistState.SpecBlockOffset;
			else if (field == MuiColorRecordField.RgbBlock)
				offset = MuiColorSpecialistState.RgbBlockOffset;
			else if (field == MuiColorRecordField.Reference)
				offset = MuiColorSpecialistState.ReferenceOffset;
			else if (field == MuiColorRecordField.ModeID)
				offset = MuiColorSpecialistState.ModeIDOffset;
			else if (field == MuiColorRecordField.Alpha)
				offset = MuiColorSpecialistState.AlphaOffset;
			else if (field == MuiColorRecordField.Entries)
				offset = MuiColorSpecialistState.EntriesOffset;
			else if (field == MuiColorRecordField.Names)
				offset = MuiColorSpecialistState.NamesOffset;
			else if (field == MuiColorRecordField.NotifyAttribute)
				offset = MuiColorSpecialistState.NotifyAttributeOffset;
			else if (field == MuiColorRecordField.NotifyValue)
				offset = MuiColorSpecialistState.NotifyValueOffset;
			else if (field == MuiColorRecordField.NotifyCount)
				offset = MuiColorSpecialistState.NotifyCountOffset;
			else return false;
			return true;
		}
		if (record == MuiColorRecordKind.Rgb)
		{
			size = MuiColorRgbRecord.Size;
			if (field == MuiColorRecordField.Red)
				offset = MuiColorRgbRecord.RedOffset;
			else if (field == MuiColorRecordField.Green)
				offset = MuiColorRgbRecord.GreenOffset;
			else if (field == MuiColorRecordField.Blue)
				offset = MuiColorRgbRecord.BlueOffset;
			else return false;
			return true;
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR recordAddress, MuiColorRecordKind record,
		MuiColorRecordField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(record, field, out var offset, out var size) ||
			recordAddress.IsNull || recordAddress.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(recordAddress, size)) return false;
		address = APTR.FromPointer(recordAddress.Raw + offset);
		return platform.IsMapped(address, MuiColorPenSpecRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiColorRecordKind record, MuiColorRecordField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, address, record, field,
			out var fieldAddress))
			return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiColorRecordKind record, MuiColorRecordField field,
		uint value) where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, address, record, field,
			out var fieldAddress))
			return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// color record cursor. Live specialist serialization uses the direct adapter.
internal static class MuiColorRecordFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiColorRecordFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiColorRecordMemoryCodec.TryGetAddress(ref platform, cursor.Address,
			cursor.Record, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiColorRecordKind record, MuiColorRecordField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory =>
		MuiColorRecordMemoryCodec.TryReadUInt32(ref platform, address, record,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiColorRecordKind record, MuiColorRecordField field,
		uint value) where TPlatform : struct, IMuiGuestMemory =>
		MuiColorRecordMemoryCodec.TryWriteUInt32(ref platform, address, record,
			field, value);
}

internal static class MuiColorPenSpecCodec
{
	// Production access is sequential and struct-shaped. The field-address
	// adapter remains available only for compatibility diagnostics.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiColorPenSpecRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if ((address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiColorPenSpecRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Kind) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Scalar) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Red) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Green) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Blue) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Reserved0) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Reserved1) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Reserved2) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiColorPenSpecRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiColorPenSpecRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if ((address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiColorPenSpecRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Kind) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Scalar) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Red) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Green) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Blue) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Reserved0) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Reserved1) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Reserved2)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiColorPenSpecRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiColorSpecialistState
{
	internal const uint Size = MuiColorSpecialistLayout.InstanceSize;
	internal const uint Cookie = MuiColorSpecialistLayout.Magic;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ClassOffset = 4;
	internal const uint FlagsOffset = 8;
	internal const uint RenderInfoOffset = 12;
	internal const uint DrawStateOffset = 16;
	internal const uint PenOffset = 20;
	internal const uint SpecBlockOffset = 24;
	internal const uint RgbBlockOffset = 28;
	internal const uint ReferenceOffset = 32;
	internal const uint ModeIDOffset = 36;
	internal const uint AlphaOffset = 40;
	internal const uint EntriesOffset = 44;
	internal const uint NamesOffset = 48;
	internal const uint NotifyAttributeOffset = 52;
	internal const uint NotifyValueOffset = 56;
	internal const uint NotifyCountOffset = 60;

	internal uint Magic;
	internal uint Class;
	internal uint Flags;
	internal APTR RenderInfo;
	internal APTR DrawState;
	internal uint Pen;
	internal APTR SpecBlock;
	internal APTR RgbBlock;
	internal APTR Reference;
	internal uint ModeID;
	internal uint Alpha;
	internal APTR Entries;
	internal APTR Names;
	internal uint NotifyAttribute;
	internal uint NotifyValue;
	internal uint NotifyCount;
}

internal static class MuiColorSpecialistStateCodec
{
	// Production access is sequential and struct-shaped. The field-address
	// adapter remains available only for compatibility diagnostics.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiColorSpecialistState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if ((address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiColorSpecialistState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var @class) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var flags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var renderInfo) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var drawState) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var pen) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var specBlock) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rgbBlock) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var reference) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var modeId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var alpha) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var entries) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var names) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var notifyAttribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var notifyValue) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var notifyCount) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Magic = magic;
		value.Class = @class;
		value.Flags = flags;
		value.RenderInfo = APTR.FromPointer(renderInfo);
		value.DrawState = APTR.FromPointer(drawState);
		value.Pen = pen;
		value.SpecBlock = APTR.FromPointer(specBlock);
		value.RgbBlock = APTR.FromPointer(rgbBlock);
		value.Reference = APTR.FromPointer(reference);
		value.ModeID = modeId;
		value.Alpha = alpha;
		value.Entries = APTR.FromPointer(entries);
		value.Names = APTR.FromPointer(names);
		value.NotifyAttribute = notifyAttribute;
		value.NotifyValue = notifyValue;
		value.NotifyCount = notifyCount;
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiColorSpecialistState value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiColorSpecialistState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadStructural(ref platform, address, out value)) return false;
		return MuiColorSpecialistStateAdmission.Validate(ref platform, value);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiColorSpecialistState value)
		where TPlatform : struct, IMuiGuestMemory
		=> value.Magic == MuiColorSpecialistState.Cookie &&
			WriteRecord(ref platform, address, value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform, APTR address,
		MuiColorSpecialistState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if ((address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiColorSpecialistState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Class) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Flags) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.RenderInfo.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.DrawState.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Pen) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.SpecBlock.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.RgbBlock.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Reference.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.ModeID) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Alpha) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Entries.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Names.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.NotifyAttribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.NotifyValue) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.NotifyCount)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// The structural codec preserves the complete 64-byte specialist ABI for
// qualification. Live pen/color operations use this semantic boundary so a
// guest cannot turn arbitrary mapped data into a pen owner or draw binding.
internal static class MuiColorSpecialistStateAdmission
{
	private const uint AllowedFlags = MuiColorSpecialistLayout.FlagSetupActive |
		MuiColorSpecialistLayout.FlagPenHeld |
		MuiColorSpecialistLayout.FlagGroupable |
		MuiColorSpecialistLayout.FlagShowAlpha |
		MuiColorSpecialistLayout.FlagPSIMode |
		MuiColorSpecialistLayout.FlagObsolete;

	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiColorSpecialistState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cls = (MuiColorSpecialistClass)value.Class;
		if (value.Magic != MuiColorSpecialistState.Cookie ||
			cls < MuiColorSpecialistClass.Pendisplay ||
			cls > MuiColorSpecialistClass.Penadjust ||
			(value.Flags & ~AllowedFlags) != 0 ||
			(value.Flags & MuiColorSpecialistLayout.FlagPenHeld) != 0 &&
			(value.Flags & MuiColorSpecialistLayout.FlagSetupActive) == 0 ||
			(cls != MuiColorSpecialistClass.Palette &&
				(value.Flags & MuiColorSpecialistLayout.FlagObsolete) != 0) ||
			(cls == MuiColorSpecialistClass.Palette &&
				(value.Flags & MuiColorSpecialistLayout.FlagObsolete) == 0) ||
			(cls != MuiColorSpecialistClass.Coloradjust &&
				(value.Flags & MuiColorSpecialistLayout.FlagShowAlpha) != 0) ||
			(cls != MuiColorSpecialistClass.Penadjust &&
				(value.Flags & MuiColorSpecialistLayout.FlagPSIMode) != 0) ||
			(cls != MuiColorSpecialistClass.Palette &&
				(value.Flags & MuiColorSpecialistLayout.FlagGroupable) != 0) ||
			(cls != MuiColorSpecialistClass.Pendisplay &&
				cls != MuiColorSpecialistClass.Colorfield &&
				(value.Flags & MuiColorSpecialistLayout.FlagPenHeld) != 0))
			return false;

		if (!ValidateBinding(ref platform, value)) return false;
		if (cls == MuiColorSpecialistClass.Pendisplay &&
			!ValidateBlock(ref platform, value.SpecBlock,
				MuiColorSpecialistLayout.SpecSize) ||
			cls == MuiColorSpecialistClass.Colorfield &&
			value.SpecBlock.IsNotNull && !ValidateBlock(ref platform,
				value.SpecBlock, MuiColorSpecialistLayout.SpecSize)) return false;
		if ((cls == MuiColorSpecialistClass.Pendisplay ||
			cls == MuiColorSpecialistClass.Colorfield ||
			cls == MuiColorSpecialistClass.Coloradjust) &&
			!ValidateBlock(ref platform, value.RgbBlock,
				MuiColorSpecialistLayout.RgbSize)) return false;
		if (cls == MuiColorSpecialistClass.Palette ||
			cls == MuiColorSpecialistClass.Penadjust)
		{
			if (value.SpecBlock.IsNotNull || value.RgbBlock.IsNotNull) return false;
		}
		if (cls != MuiColorSpecialistClass.Pendisplay &&
			value.Reference.IsNotNull) return false;
		return value.Reference.IsNull || ValidateBlock(ref platform,
			value.Reference, MuiColorSpecialistState.Size);
	}

	private static bool ValidateBinding<TPlatform>(ref TPlatform platform,
		MuiColorSpecialistState value) where TPlatform : struct, IMuiGuestMemory
	{
		var active = (value.Flags & MuiColorSpecialistLayout.FlagSetupActive) != 0;
		if (!active)
			return value.RenderInfo.IsNull && value.DrawState.IsNull &&
				(value.Flags & MuiColorSpecialistLayout.FlagPenHeld) == 0 &&
				value.Pen == 0;
		return value.RenderInfo.IsNotNull && value.DrawState.IsNotNull &&
			ValidateBlock(ref platform, value.RenderInfo,
				MuiColorSpecialistLayout.RenderInfoSize) &&
			ValidateBlock(ref platform, value.DrawState,
				MuiDrawingServiceStateRecord.Size);
	}

	private static bool ValidateBlock<TPlatform>(ref TPlatform platform,
		APTR address, uint size) where TPlatform : struct, IMuiGuestMemory =>
	address.IsNotNull && (address.Raw & 1u) == 0 && platform.IsMapped(address,
		size);
}

// Public MUI_RGBColor wire record: three ULONG intensities in guest memory.
// Keep all component serialization at this named boundary; specialist logic
// works with the fields rather than repeating RGB member offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiColorRgbRecord
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint RedOffset = 0;
	internal const uint GreenOffset = 4;
	internal const uint BlueOffset = 8;
	internal uint Red;
	internal uint Green;
	internal uint Blue;
}

internal static class MuiColorRgbCodec
{
	// Production access is sequential and struct-shaped. The field-address
	// adapter remains available only for compatibility diagnostics.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiColorRgbRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if ((address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiColorRgbRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Red) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Green) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Blue) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiColorRgbRecord record)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out record);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiColorRgbRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if ((address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiColorRgbRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Red) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Green) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Blue)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiColorRgbRecord record)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, record);
}

// The MG09 pen/color specialist family. Every entry point works over a validated
// guest-resident instance block; the classes are classified by their exact,
// case-sensitive official class id. The core owns the copied MUI_PenSpec/RGB
// blocks and the Setup/Cleanup pen lifecycle, always routing pens through
// MuiDrawingServiceCore. The frozen cores, dispatchers and platform aggregates
// are not modified; the family only requires the frozen MG09 service surface
// plus the graphics seam for its bounded fill, expressed as a combined generic
// constraint so no additional aggregate interface is required.
public enum MuiColorSpecialistClass : uint
{
	None = 0,
	Pendisplay = 1,
	Colorfield = 2,
	Coloradjust = 3,
	Palette = 4,
	Penadjust = 5,
}

public static class MuiColorSpecialistCore
{
	// ---- Classification ------------------------------------------------------

	// Classify a guest C-string class id against the exact official names. The
	// loader contract is case-sensitive, so the match is byte-exact. Each
	// official fixed-length identity is decoded through its named packed record;
	// no managed strings, arrays or spans are involved.
	public static MuiColorSpecialistClass ClassifyName<TPlatform>(
		ref TPlatform platform, APTR classId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (MuiColorSpecialistPendisplayClassNameRecordCodec.TryMatch(
			ref platform, classId)) return MuiColorSpecialistClass.Pendisplay;
		if (MuiColorSpecialistColorfieldClassNameRecordCodec.TryMatch(
			ref platform, classId)) return MuiColorSpecialistClass.Colorfield;
		if (MuiColorSpecialistColoradjustClassNameRecordCodec.TryMatch(
			ref platform, classId)) return MuiColorSpecialistClass.Coloradjust;
		if (MuiColorSpecialistPaletteClassNameRecordCodec.TryMatch(
			ref platform, classId)) return MuiColorSpecialistClass.Palette;
		if (MuiColorSpecialistPenadjustClassNameRecordCodec.TryMatch(
			ref platform, classId)) return MuiColorSpecialistClass.Penadjust;
		return MuiColorSpecialistClass.None;
	}

	// Palette is obsolete but explicitly supported: it is fully initialized and
	// operative, never a placeholder. This reports the documented disposition.
	public static bool IsObsolete(MuiColorSpecialistClass cls) =>
		cls == MuiColorSpecialistClass.Palette;

	// Penadjust is a private class.
	public static bool IsPrivate(MuiColorSpecialistClass cls) =>
		cls == MuiColorSpecialistClass.Penadjust;

	public static MuiColorSpecialistClass Classify<TPlatform>(
		ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return MuiColorSpecialistClass.None;
		return (MuiColorSpecialistClass)state.Class;
	}

	public static bool Valid<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiColorSpecialistStateCodec.TryRead(ref platform, instance, out _);

	// ---- Creation ------------------------------------------------------------

	// Initialize a specialist instance of a class named by a guest C-string.
	// Returns the classified class (None on an unknown id or an allocation
	// failure). Allocation failures are atomic: any partially allocated owned
	// block is released before returning None.
	public static MuiColorSpecialistClass CreateByName<TPlatform>(
		ref TPlatform platform, APTR instance, APTR classId)
		where TPlatform : struct, IMuiExecCapability, IMuiGuestMemory
	{
		var cls = ClassifyName(ref platform, classId);
		if (cls == MuiColorSpecialistClass.None) return MuiColorSpecialistClass.None;
		return Create(ref platform, instance, cls) ? cls :
			MuiColorSpecialistClass.None;
	}

	// Initialize a specialist instance of an explicit class. Sets creation-time
	// defaults and allocates the class-owned copied blocks. Atomic on failure.
	public static bool Create<TPlatform>(ref TPlatform platform, APTR instance,
		MuiColorSpecialistClass cls)
		where TPlatform : struct, IMuiExecCapability, IMuiGuestMemory
	{
		if (instance.IsNull ||
			!platform.IsMapped(instance, MuiColorSpecialistLayout.InstanceSize) ||
			cls == MuiColorSpecialistClass.None) return false;
		platform.Clear(instance, MuiColorSpecialistLayout.InstanceSize);

		APTR spec = APTR.Null;
		APTR rgb = APTR.Null;
		// Pendisplay owns a persistent 32-byte pen spec and a 12-byte RGB copy.
		// Colorfield and Coloradjust own a 12-byte RGB copy (Colorfield's pen
		// spec is allocated transiently at Setup). Palette and Penadjust own no
		// copied blocks (Palette entries/names are caller-owned references).
		if (cls == MuiColorSpecialistClass.Pendisplay)
		{
			spec = MuiColorAllocate(ref platform, MuiColorSpecialistLayout.SpecSize);
			if (spec.IsNull) return false;
			rgb = MuiColorAllocate(ref platform, MuiColorSpecialistLayout.RgbSize);
			if (rgb.IsNull)
			{
				MuiColorFree(ref platform, spec, MuiColorSpecialistLayout.SpecSize);
				return false;
			}
			var specRecord = default(MuiColorPenSpecRecord);
			specRecord.Kind = MuiColorSpecialistLayout.SpecKindRgb;
			if (!MuiColorPenSpecCodec.Write(ref platform, spec, specRecord))
			{
				MuiColorFree(ref platform, rgb, MuiColorSpecialistLayout.RgbSize);
				MuiColorFree(ref platform, spec, MuiColorSpecialistLayout.SpecSize);
				return false;
			}
		}
		else if (cls == MuiColorSpecialistClass.Colorfield ||
			cls == MuiColorSpecialistClass.Coloradjust)
		{
			rgb = MuiColorAllocate(ref platform, MuiColorSpecialistLayout.RgbSize);
			if (rgb.IsNull) return false;
		}

		uint flags = 0;
		// Palette is groupable by default and carries the obsolete marker.
		if (cls == MuiColorSpecialistClass.Palette)
			flags |= MuiColorSpecialistLayout.FlagGroupable |
				MuiColorSpecialistLayout.FlagObsolete;
		var state = default(MuiColorSpecialistState);
		state.Magic = MuiColorSpecialistState.Cookie;
		state.Class = (uint)cls;
		state.SpecBlock = spec;
		state.RgbBlock = rgb;
		state.Flags = flags;
		return MuiColorSpecialistStateCodec.Write(ref platform, instance, state);
	}

	// ---- Setup / Cleanup (pen lifecycle) -------------------------------------

	// Setup obtains the specialist's shared pen through MuiDrawingServiceCore so
	// the drawing-service pen tracking remains the sole owner of live pens. The
	// render info and drawing-service state are captured for the balancing
	// Cleanup. Group specialists (Coloradjust/Palette/Penadjust) hold no pen and
	// their Setup simply records the render binding. A Pendisplay with a
	// reference borrows its pen from the referenced object and obtains none.
	// Returns success. Every failure path is atomic.
	public static bool Setup<TPlatform>(ref TPlatform platform, APTR instance,
		APTR drawState, APTR renderInfo)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state) || renderInfo.IsNull ||
			!platform.IsMapped(renderInfo, MuiColorSpecialistLayout.RenderInfoSize))
			return false;
		if ((state.Flags & MuiColorSpecialistLayout.FlagSetupActive) != 0)
			return false;

		var cls = (MuiColorSpecialistClass)state.Class;

		if (cls == MuiColorSpecialistClass.Pendisplay)
		{
			var reference = state.Reference;
			if (reference.IsNotNull)
			{
				// Borrowed pen: no pen obtained, so none is released at Cleanup.
				RecordSetup(ref platform, instance, drawState, renderInfo, false, 0);
				return true;
			}
			var spec = state.SpecBlock;
			var token = MuiDrawingServiceCore.ObtainPen(ref platform, drawState,
				renderInfo, spec, 0);
			if (token < 0) return false;
			RecordSetup(ref platform, instance, drawState, renderInfo, true,
				unchecked((uint)token));
			return true;
		}

		if (cls == MuiColorSpecialistClass.Colorfield)
		{
			// Colorfield's pen is described by its copied RGB. It allocates a
			// transient 32-byte pen spec (the copied RGB is mirrored into it) and
			// obtains the pen through the drawing service. Atomic on failure.
			var spec = MuiColorAllocate(ref platform,
				MuiColorSpecialistLayout.SpecSize);
			if (spec.IsNull) return false;
			var rgb = state.RgbBlock;
			if (!MuiColorRgbCodec.TryRead(ref platform, rgb, out var rgbRecord))
			{
				MuiColorFree(ref platform, spec, MuiColorSpecialistLayout.SpecSize);
				return false;
			}
			if (!MuiColorPenSpecCodec.TryRead(ref platform, spec,
				out var specRecord))
			{
				MuiColorFree(ref platform, spec, MuiColorSpecialistLayout.SpecSize);
				return false;
			}
			specRecord.Kind = MuiColorSpecialistLayout.SpecKindRgb;
			specRecord.Red = rgbRecord.Red;
			specRecord.Green = rgbRecord.Green;
			specRecord.Blue = rgbRecord.Blue;
			if (!MuiColorPenSpecCodec.Write(ref platform, spec, specRecord))
			{
				MuiColorFree(ref platform, spec, MuiColorSpecialistLayout.SpecSize);
				return false;
			}
			var token = MuiDrawingServiceCore.ObtainPen(ref platform, drawState,
				renderInfo, spec, 0);
			if (token < 0)
			{
				MuiColorFree(ref platform, spec, MuiColorSpecialistLayout.SpecSize);
				return false;
			}
			state.SpecBlock = spec;
			if (!MuiColorSpecialistStateCodec.Write(ref platform, instance, state))
				return false;
			RecordSetup(ref platform, instance, drawState, renderInfo, true,
				unchecked((uint)token));
			return true;
		}

		// Group specialists: record the render binding, hold no pen.
		RecordSetup(ref platform, instance, drawState, renderInfo, false, 0);
		return true;
	}

	private static void RecordSetup<TPlatform>(ref TPlatform platform,
		APTR instance, APTR drawState, APTR renderInfo, bool penHeld, uint token)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return;
		state.RenderInfo = renderInfo;
		state.DrawState = drawState;
		var flags = state.Flags | MuiColorSpecialistLayout.FlagSetupActive;
		if (penHeld)
		{
			state.Pen = token;
			flags |= MuiColorSpecialistLayout.FlagPenHeld;
		}
		state.Flags = flags;
		MuiColorSpecialistStateCodec.Write(ref platform, instance, state);
	}

	// Cleanup releases the shared pen exactly once through MuiDrawingServiceCore
	// and tears down the transient Colorfield pen spec. Idempotent: a second
	// Cleanup (or a Cleanup on a never-set-up instance) releases nothing.
	public static bool Cleanup<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return false;
		var flags = state.Flags;
		if ((flags & MuiColorSpecialistLayout.FlagSetupActive) == 0) return false;

		if ((flags & MuiColorSpecialistLayout.FlagPenHeld) != 0)
		{
			var drawState = state.DrawState;
			var renderInfo = state.RenderInfo;
			var pen = unchecked((int)state.Pen);
			MuiDrawingServiceCore.ReleasePen(ref platform, drawState, renderInfo,
				pen);
			flags &= ~MuiColorSpecialistLayout.FlagPenHeld;
			state.Pen = 0;
		}

		// Colorfield's pen spec is transient and released with the pen.
		var cls = (MuiColorSpecialistClass)state.Class;
		if (cls == MuiColorSpecialistClass.Colorfield)
		{
			var spec = state.SpecBlock;
			if (spec.IsNotNull)
			{
				MuiColorFree(ref platform, spec, MuiColorSpecialistLayout.SpecSize);
				state.SpecBlock = APTR.Null;
			}
		}

		flags &= ~MuiColorSpecialistLayout.FlagSetupActive;
		state.Flags = flags;
		state.RenderInfo = APTR.Null;
		state.DrawState = APTR.Null;
		return MuiColorSpecialistStateCodec.Write(ref platform, instance, state);
	}

	public static bool IsSetup<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state) && (state.Flags &
			MuiColorSpecialistLayout.FlagSetupActive) != 0;

	// ---- Attribute get -------------------------------------------------------

	// Resolve a documented [..G]/[.SG]/[ISG] getter. Returns false when the
	// attribute is not readable on this class (write-only or unknown).
	public static bool GetAttribute<TPlatform>(ref TPlatform platform,
		APTR instance, uint attribute, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return false;
		var cls = (MuiColorSpecialistClass)state.Class;
		var rgb = state.RgbBlock;

		switch (attribute)
		{
			// -- Pendisplay --
			case MuiColorAttributes.PendisplayPen:
				if (cls != MuiColorSpecialistClass.Pendisplay) return false;
				value = unchecked((uint)EffectivePen(ref platform, instance));
				return true;
			case MuiColorAttributes.PendisplaySpec:
				if (cls != MuiColorSpecialistClass.Pendisplay) return false;
				value = state.SpecBlock.Raw;
				return true;
			case MuiColorAttributes.PendisplayRgbColor:
				if (cls != MuiColorSpecialistClass.Pendisplay) return false;
				value = rgb.Raw;
				return true;
			case MuiColorAttributes.PendisplayReference:
				if (cls != MuiColorSpecialistClass.Pendisplay) return false;
				value = state.Reference.Raw;
				return true;
			case MuiColorAttributes.PendisplayArgb:
				if (cls != MuiColorSpecialistClass.Pendisplay) return false;
				value = PackArgb(ref platform, rgb, state.Alpha);
				return true;
			case MuiColorAttributes.PendisplayXrgb:
				if (cls != MuiColorSpecialistClass.Pendisplay) return false;
				value = PackXrgb(ref platform, rgb);
				return true;

			// -- Colorfield --
			case MuiColorAttributes.ColorfieldRed:
				if (cls != MuiColorSpecialistClass.Colorfield) return false;
				value = ReadRgbComponent(ref platform, rgb,
					MuiColorRgbComponent.Red);
				return true;
			case MuiColorAttributes.ColorfieldGreen:
				if (cls != MuiColorSpecialistClass.Colorfield) return false;
				value = ReadRgbComponent(ref platform, rgb,
					MuiColorRgbComponent.Green);
				return true;
			case MuiColorAttributes.ColorfieldBlue:
				if (cls != MuiColorSpecialistClass.Colorfield) return false;
				value = ReadRgbComponent(ref platform, rgb,
					MuiColorRgbComponent.Blue);
				return true;
			case MuiColorAttributes.ColorfieldRgb:
				if (cls != MuiColorSpecialistClass.Colorfield) return false;
				value = rgb.Raw;
				return true;
			case MuiColorAttributes.ColorfieldPen:
				if (cls != MuiColorSpecialistClass.Colorfield) return false;
				value = unchecked((uint)EffectivePen(ref platform, instance));
				return true;

			// -- Coloradjust --
			case MuiColorAttributes.ColoradjustRed:
				if (cls != MuiColorSpecialistClass.Coloradjust) return false;
				value = ReadRgbComponent(ref platform, rgb,
					MuiColorRgbComponent.Red);
				return true;
			case MuiColorAttributes.ColoradjustGreen:
				if (cls != MuiColorSpecialistClass.Coloradjust) return false;
				value = ReadRgbComponent(ref platform, rgb,
					MuiColorRgbComponent.Green);
				return true;
			case MuiColorAttributes.ColoradjustBlue:
				if (cls != MuiColorSpecialistClass.Coloradjust) return false;
				value = ReadRgbComponent(ref platform, rgb,
					MuiColorRgbComponent.Blue);
				return true;
			case MuiColorAttributes.ColoradjustRgb:
				if (cls != MuiColorSpecialistClass.Coloradjust) return false;
				value = rgb.Raw;
				return true;
			case MuiColorAttributes.ColoradjustModeId:
				if (cls != MuiColorSpecialistClass.Coloradjust) return false;
				value = state.ModeID;
				return true;
			case MuiColorAttributes.ColoradjustAlpha:
				if (cls != MuiColorSpecialistClass.Coloradjust) return false;
				value = state.Alpha;
				return true;
			case MuiColorAttributes.ColoradjustArgb:
				if (cls != MuiColorSpecialistClass.Coloradjust) return false;
				value = PackArgb(ref platform, rgb, state.Alpha);
				return true;
			case MuiColorAttributes.ColoradjustXrgb:
				if (cls != MuiColorSpecialistClass.Coloradjust) return false;
				value = PackXrgb(ref platform, rgb);
				return true;
			case MuiColorAttributes.ColoradjustShowAlpha:
				if (cls != MuiColorSpecialistClass.Coloradjust) return false;
				value = (state.Flags &
					MuiColorSpecialistLayout.FlagShowAlpha) != 0 ? 1u : 0u;
				return true;

			// -- Palette (obsolete but supported) --
			case MuiColorAttributes.PaletteEntries:
				if (cls != MuiColorSpecialistClass.Palette) return false;
				value = state.Entries.Raw;
				return true;
			case MuiColorAttributes.PaletteNames:
				if (cls != MuiColorSpecialistClass.Palette) return false;
				value = state.Names.Raw;
				return true;
			case MuiColorAttributes.PaletteGroupable:
				if (cls != MuiColorSpecialistClass.Palette) return false;
				value = (state.Flags &
					MuiColorSpecialistLayout.FlagGroupable) != 0 ? 1u : 0u;
				return true;
		}
		return false;
	}

	// ---- Attribute set -------------------------------------------------------

	// Apply a documented setter. `isInit` selects the OM_NEW init path, on which
	// init-only [I..] attributes are honoured and no notification is produced.
	// `notify` distinguishes the notifying set from the no-notify set. `changed`
	// reports whether the stored value actually changed. Returns whether the
	// attribute is a recognized setter on this class.
	public static bool SetAttribute<TPlatform>(ref TPlatform platform,
		APTR instance, uint attribute, uint value, bool isInit, bool notify,
		out bool changed) where TPlatform : struct, IMuiGuestMemory
	{
		changed = false;
		if (!MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return false;
		var cls = (MuiColorSpecialistClass)state.Class;
		var rgb = state.RgbBlock;

		switch (attribute)
		{
			// -- Pendisplay -- [ISG] Spec / RGBcolor / Reference; [.SG] ARGB/XRGB
			case MuiColorAttributes.PendisplaySpec:
				if (cls != MuiColorSpecialistClass.Pendisplay) return false;
				changed = CopyPenSpec(ref platform, APTR.FromPointer(value),
					state.SpecBlock);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiColorAttributes.PendisplayRgbColor:
				if (cls != MuiColorSpecialistClass.Pendisplay) return false;
				changed = CopyRgb(ref platform, APTR.FromPointer(value), rgb);
				MirrorRgbIntoSpec(ref platform, instance);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiColorAttributes.PendisplayReference:
				if (cls != MuiColorSpecialistClass.Pendisplay) return false;
				changed = state.Reference.Raw != value;
				state.Reference = APTR.FromPointer(value);
				MuiColorSpecialistStateCodec.Write(ref platform, instance, state);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiColorAttributes.PendisplayArgb:
				if (cls != MuiColorSpecialistClass.Pendisplay) return false;
				changed = UnpackArgb(ref platform, instance, rgb, value);
				MirrorRgbIntoSpec(ref platform, instance);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiColorAttributes.PendisplayXrgb:
				if (cls != MuiColorSpecialistClass.Pendisplay) return false;
				changed = UnpackXrgb(ref platform, instance, rgb, value);
				MirrorRgbIntoSpec(ref platform, instance);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;

			// -- Colorfield -- [ISG] Red / Green / Blue / RGB
			case MuiColorAttributes.ColorfieldRed:
				if (cls != MuiColorSpecialistClass.Colorfield) return false;
				changed = WriteComponent(ref platform, rgb,
					MuiColorRgbComponent.Red, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiColorAttributes.ColorfieldGreen:
				if (cls != MuiColorSpecialistClass.Colorfield) return false;
				changed = WriteComponent(ref platform, rgb,
					MuiColorRgbComponent.Green, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiColorAttributes.ColorfieldBlue:
				if (cls != MuiColorSpecialistClass.Colorfield) return false;
				changed = WriteComponent(ref platform, rgb,
					MuiColorRgbComponent.Blue, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiColorAttributes.ColorfieldRgb:
				if (cls != MuiColorSpecialistClass.Colorfield) return false;
				changed = CopyRgb(ref platform, APTR.FromPointer(value), rgb);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;

			// -- Coloradjust -- synchronized [ISG] components / RGB / ARGB / XRGB
			//    / Alpha / ModeID / ShowAlpha.
			case MuiColorAttributes.ColoradjustRed:
				if (cls != MuiColorSpecialistClass.Coloradjust) return false;
				changed = WriteComponent(ref platform, rgb,
					MuiColorRgbComponent.Red, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiColorAttributes.ColoradjustGreen:
				if (cls != MuiColorSpecialistClass.Coloradjust) return false;
				changed = WriteComponent(ref platform, rgb,
					MuiColorRgbComponent.Green, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiColorAttributes.ColoradjustBlue:
				if (cls != MuiColorSpecialistClass.Coloradjust) return false;
				changed = WriteComponent(ref platform, rgb,
					MuiColorRgbComponent.Blue, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiColorAttributes.ColoradjustRgb:
				if (cls != MuiColorSpecialistClass.Coloradjust) return false;
				changed = CopyRgb(ref platform, APTR.FromPointer(value), rgb);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiColorAttributes.ColoradjustModeId:
				if (cls != MuiColorSpecialistClass.Coloradjust) return false;
				changed = state.ModeID != value;
				state.ModeID = value;
				MuiColorSpecialistStateCodec.Write(ref platform, instance, state);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiColorAttributes.ColoradjustAlpha:
				if (cls != MuiColorSpecialistClass.Coloradjust) return false;
				changed = state.Alpha != value;
				state.Alpha = value;
				MuiColorSpecialistStateCodec.Write(ref platform, instance, state);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiColorAttributes.ColoradjustArgb:
				if (cls != MuiColorSpecialistClass.Coloradjust) return false;
				changed = UnpackArgb(ref platform, instance, rgb, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiColorAttributes.ColoradjustXrgb:
				if (cls != MuiColorSpecialistClass.Coloradjust) return false;
				changed = UnpackXrgb(ref platform, instance, rgb, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiColorAttributes.ColoradjustShowAlpha:
				if (cls != MuiColorSpecialistClass.Coloradjust) return false;
				changed = SetFlag(ref platform, instance,
					MuiColorSpecialistLayout.FlagShowAlpha, value != 0);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;

			// -- Palette (obsolete) -- [I.G] Entries; [ISG] Names / Groupable.
			case MuiColorAttributes.PaletteEntries:
				if (cls != MuiColorSpecialistClass.Palette || !isInit) return false;
				changed = state.Entries.Raw != value;
				if (changed)
				{
					state.Entries = APTR.FromPointer(value);
					MuiColorSpecialistStateCodec.Write(ref platform, instance, state);
				}
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiColorAttributes.PaletteNames:
				if (cls != MuiColorSpecialistClass.Palette) return false;
				changed = state.Names.Raw != value;
				if (changed)
				{
					state.Names = APTR.FromPointer(value);
					MuiColorSpecialistStateCodec.Write(ref platform, instance, state);
				}
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiColorAttributes.PaletteGroupable:
				if (cls != MuiColorSpecialistClass.Palette) return false;
				changed = SetFlag(ref platform, instance,
					MuiColorSpecialistLayout.FlagGroupable, value != 0);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;

			// -- Penadjust (private) -- [I..] PSIMode.
			case MuiColorAttributes.PenadjustPsiMode:
				if (cls != MuiColorSpecialistClass.Penadjust || !isInit)
					return false;
				changed = SetFlag(ref platform, instance,
					MuiColorSpecialistLayout.FlagPSIMode, value != 0);
				return true;
		}
		return false;
	}

	// ---- Pendisplay methods --------------------------------------------------

	// MUIM_Pendisplay_SetColormap(colormap). Authors an opaque colormap pen spec
	// and detaches any reference. Returns success.
	public static bool SetColormap<TPlatform>(ref TPlatform platform,
		APTR instance, uint colormap) where TPlatform : struct, IMuiGuestMemory =>
		SetSpecKind(ref platform, instance,
			MuiColorSpecialistLayout.SpecKindColormap, colormap);

	// MUIM_Pendisplay_SetMUIPen(muipen). Authors an opaque MUI-pen spec.
	public static bool SetMUIPen<TPlatform>(ref TPlatform platform, APTR instance,
		uint muipen) where TPlatform : struct, IMuiGuestMemory =>
		SetSpecKind(ref platform, instance,
			MuiColorSpecialistLayout.SpecKindMuiPen, muipen);

	// MUIM_Pendisplay_SetRGB(r, g, b). Authors an RGB spec and synchronizes the
	// copied MUI_RGBColor.
	public static bool SetRGB<TPlatform>(ref TPlatform platform, APTR instance,
		uint red, uint green, uint blue)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state) || (MuiColorSpecialistClass)state.Class !=
			MuiColorSpecialistClass.Pendisplay)
			return false;
		state.Reference = APTR.Null;
		if (!MuiColorSpecialistStateCodec.Write(ref platform, instance, state))
			return false;
		var rgb = state.RgbBlock;
		var rgbRecord = default(MuiColorRgbRecord);
		rgbRecord.Red = red;
		rgbRecord.Green = green;
		rgbRecord.Blue = blue;
		if (!MuiColorRgbCodec.Write(ref platform, rgb, rgbRecord)) return false;
		var spec = state.SpecBlock;
		if (!MuiColorPenSpecCodec.TryRead(ref platform, spec,
			out var specRecord)) return false;
		specRecord.Kind = MuiColorSpecialistLayout.SpecKindRgb;
		specRecord.Red = red;
		specRecord.Green = green;
		specRecord.Blue = blue;
		if (!MuiColorPenSpecCodec.Write(ref platform, spec, specRecord))
			return false;
		MirrorRgbIntoSpec(ref platform, instance);
		Notify(ref platform, instance, MuiColorAttributes.PendisplayRgbColor,
			rgb.Raw, false, true, true);
		return true;
	}

	private static bool SetSpecKind<TPlatform>(ref TPlatform platform,
		APTR instance, uint kind, uint scalar)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state) || (MuiColorSpecialistClass)state.Class !=
			MuiColorSpecialistClass.Pendisplay)
			return false;
		state.Reference = APTR.Null;
		if (!MuiColorSpecialistStateCodec.Write(ref platform, instance, state))
			return false;
		var spec = state.SpecBlock;
		if (!MuiColorPenSpecCodec.TryRead(ref platform, spec,
			out var specRecord)) return false;
		specRecord.Kind = kind;
		specRecord.Scalar = scalar;
		if (!MuiColorPenSpecCodec.Write(ref platform, spec, specRecord))
			return false;
		Notify(ref platform, instance, MuiColorAttributes.PendisplaySpec, spec.Raw,
			false, true, true);
		return true;
	}

	// ---- Bounded rendering ---------------------------------------------------

	// Bounded Area draw shared by Pendisplay and Colorfield. Paints a single
	// filled rectangle in the held shared pen, or in the fallback background pen
	// when no pen is currently held (a disabled/not-set-up field still paints).
	// Non-positive geometry is a no-op. Returns whether the object is drawable.
	public static bool Draw<TPlatform>(ref TPlatform platform, APTR instance,
		APTR rastPort, int left, int top, int width, int height)
		where TPlatform : struct, IMuiServicePlatform, IMuiGraphicsCapability
	{
		var cls = Classify(ref platform, instance);
		if (cls != MuiColorSpecialistClass.Pendisplay &&
			cls != MuiColorSpecialistClass.Colorfield) return false;
		if (rastPort.IsNull || width <= 0 || height <= 0) return true;
		if (!MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return false;
		var flags = state.Flags;
		uint pen = (flags & MuiColorSpecialistLayout.FlagPenHeld) != 0
			? unchecked((uint)EffectivePen(ref platform, instance))
			: MuiColorSpecialistLayout.FallbackPen;
		platform.SetPen(rastPort, pen);
		platform.FillRectangle(rastPort, left, top, left + width - 1,
			top + height - 1);
		return true;
	}

	// Bounded Area min/max for Pendisplay and Colorfield: a small fixed swatch
	// that can grow to fill. Writes the 12-byte MUI_MinMax (six UWORDs).
	public static bool AskMinMax<TPlatform>(ref TPlatform platform, APTR instance,
		APTR storage) where TPlatform : struct, IMuiGuestMemory
	{
		var cls = Classify(ref platform, instance);
		if (cls != MuiColorSpecialistClass.Pendisplay &&
			cls != MuiColorSpecialistClass.Colorfield) return false;
		if (storage.IsNull || !platform.IsMapped(storage, 12)) return false;
		var values = default(MuiMinMaxValues);
		values.MinWidth = 8;
		values.MinHeight = 6;
		values.MaxWidth = 10000;
		values.MaxHeight = 10000;
		values.DefWidth = 24;
		values.DefHeight = 12;
		return MuiAreaLayoutCore.WriteMinMax(ref platform, storage, values);
	}

	// ---- Pen / notification accessors (for callers and tests) ----------------

	// The effective pen, honouring a Pendisplay reference: a referenced, valid
	// specialist that currently holds a pen lends it; otherwise the object's own
	// held pen (0 when none).
	public static int EffectivePen<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return 0;
		var reference = state.Reference;
		if (reference.IsNotNull && Valid(ref platform, reference))
			return OwnPen(ref platform, reference);
		return OwnPen(ref platform, instance);
	}

	private static int OwnPen<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state) && (state.Flags & MuiColorSpecialistLayout.FlagPenHeld)
			!= 0 ? unchecked((int)state.Pen) : 0;

	public static uint NotificationCount<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state) ? state.NotifyCount : 0;

	public static uint LastNotifiedAttribute<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state) ? state.NotifyAttribute : 0;

	public static uint LastNotifiedValue<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state) ? state.NotifyValue : 0;

	// ---- Internals -----------------------------------------------------------

	private static void Notify<TPlatform>(ref TPlatform platform, APTR instance,
		uint attribute, uint value, bool isInit, bool notify, bool changed)
		where TPlatform : struct, IMuiGuestMemory
	{
		// MUI triggers notification on a runtime, notifying set that actually
		// changes the value. Init (OM_NEW) and the no-notify set never notify.
		if (isInit || !notify || !changed) return;
		if (!MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return;
		state.NotifyAttribute = attribute;
		state.NotifyValue = value;
		state.NotifyCount = state.NotifyCount == uint.MaxValue
			? uint.MaxValue : state.NotifyCount + 1;
		MuiColorSpecialistStateCodec.Write(ref platform, instance, state);
	}

	private enum MuiColorRgbComponent : byte
	{
		Red,
		Green,
		Blue,
	}

	private static uint ReadRgbComponent<TPlatform>(ref TPlatform platform,
		APTR rgb, MuiColorRgbComponent component)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiColorRgbCodec.TryRead(ref platform, rgb, out var record)) return 0;
		return component == MuiColorRgbComponent.Red ? record.Red :
			component == MuiColorRgbComponent.Green ? record.Green : record.Blue;
	}

	private static bool WriteComponent<TPlatform>(ref TPlatform platform,
		APTR rgb, MuiColorRgbComponent component, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiColorRgbCodec.TryRead(ref platform, rgb, out var record)) return false;
		var current = component == MuiColorRgbComponent.Red ? record.Red :
			component == MuiColorRgbComponent.Green ? record.Green : record.Blue;
		if (current == value) return false;
		if (component == MuiColorRgbComponent.Red) record.Red = value;
		else if (component == MuiColorRgbComponent.Green) record.Green = value;
		else record.Blue = value;
		return MuiColorRgbCodec.Write(ref platform, rgb, record);
	}

	private static bool CopyRgb<TPlatform>(ref TPlatform platform, APTR source,
		APTR destination) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiColorRgbCodec.TryRead(ref platform, source, out var value) ||
			!MuiColorRgbCodec.TryRead(ref platform, destination,
			out var current)) return false;
		if (value.Red == current.Red && value.Green == current.Green &&
			value.Blue == current.Blue) return false;
		return MuiColorRgbCodec.Write(ref platform, destination, value);
	}

	private static bool CopyPenSpec<TPlatform>(ref TPlatform platform, APTR source,
		APTR destination) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiColorPenSpecCodec.TryRead(ref platform, source,
			out var value) || !MuiColorPenSpecCodec.TryRead(ref platform,
			destination, out var current)) return false;
		if (value.Kind == current.Kind && value.Scalar == current.Scalar &&
			value.Red == current.Red && value.Green == current.Green &&
			value.Blue == current.Blue && value.Reserved0 == current.Reserved0 &&
			value.Reserved1 == current.Reserved1 &&
			value.Reserved2 == current.Reserved2) return false;
		return MuiColorPenSpecCodec.Write(ref platform, destination, value);
	}

	private static void MirrorRgbIntoSpec<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return;
		var spec = state.SpecBlock;
		var rgb = state.RgbBlock;
		if (spec.IsNull || rgb.IsNull ||
			!MuiColorRgbCodec.TryRead(ref platform, rgb, out var record) ||
			!MuiColorPenSpecCodec.TryRead(ref platform, spec,
				out var specRecord)) return;
		specRecord.Red = record.Red;
		specRecord.Green = record.Green;
		specRecord.Blue = record.Blue;
		MuiColorPenSpecCodec.Write(ref platform, spec, specRecord);
	}

	private static bool SetFlag<TPlatform>(ref TPlatform platform, APTR instance,
		uint bit, bool set) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return false;
		var flags = state.Flags;
		var updated = set ? flags | bit : flags & ~bit;
		if (updated == flags) return false;
		state.Flags = updated;
		return MuiColorSpecialistStateCodec.Write(ref platform, instance, state);
	}

	// ARGB packs the high byte of each 32-bit MUI intensity plus the alpha byte.
	private static uint PackArgb<TPlatform>(ref TPlatform platform, APTR rgb,
		uint alpha) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiColorRgbCodec.TryRead(ref platform, rgb, out var record)) return 0;
		return ((alpha >> 24) << 24) |
			((record.Red >> 24) << 16) |
			((record.Green >> 24) << 8) |
			record.Blue >> 24;
	}

	// XRGB is ARGB with the alpha/"don't care" byte reported as zero.
	private static uint PackXrgb<TPlatform>(ref TPlatform platform, APTR rgb)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiColorRgbCodec.TryRead(ref platform, rgb, out var record)) return 0;
		return ((record.Red >> 24) << 16) |
			((record.Green >> 24) << 8) |
			record.Blue >> 24;
	}

	// Expand each 8-bit ARGB field to a 32-bit MUI intensity by replication and
	// store alpha; returns whether anything changed.
	private static bool UnpackArgb<TPlatform>(ref TPlatform platform,
		APTR instance, APTR rgb, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var alpha = Expand(value >> 24);
		var changed = WriteComponent(ref platform, rgb,
			MuiColorRgbComponent.Red, Expand((value >> 16) & 0xff));
		changed |= WriteComponent(ref platform, rgb,
			MuiColorRgbComponent.Green, Expand((value >> 8) & 0xff));
		changed |= WriteComponent(ref platform, rgb,
			MuiColorRgbComponent.Blue, Expand(value & 0xff));
		if (!MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return changed;
		if (state.Alpha != alpha)
		{
			state.Alpha = alpha;
			MuiColorSpecialistStateCodec.Write(ref platform, instance, state);
			changed = true;
		}
		return changed;
	}

	// XRGB set: expand R/G/B and force the alpha channel fully opaque.
	private static bool UnpackXrgb<TPlatform>(ref TPlatform platform,
		APTR instance, APTR rgb, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var changed = WriteComponent(ref platform, rgb,
			MuiColorRgbComponent.Red, Expand((value >> 16) & 0xff));
		changed |= WriteComponent(ref platform, rgb,
			MuiColorRgbComponent.Green, Expand((value >> 8) & 0xff));
		changed |= WriteComponent(ref platform, rgb,
			MuiColorRgbComponent.Blue, Expand(value & 0xff));
		if (!MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return changed;
		if (state.Alpha != 0xffffffffu)
		{
			state.Alpha = 0xffffffffu;
			MuiColorSpecialistStateCodec.Write(ref platform, instance, state);
			changed = true;
		}
		return changed;
	}

	private static uint Expand(uint b) => (b & 0xff) * 0x01010101u;

	// ---- Ownership (allocation with bounds check) ----------------------------

	internal static APTR MuiColorAllocate<TPlatform>(ref TPlatform platform,
		uint size) where TPlatform : struct, IMuiExecCapability, IMuiGuestMemory
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

	internal static void MuiColorFree<TPlatform>(ref TPlatform platform,
		APTR block, uint size)
		where TPlatform : struct, IMuiExecCapability, IMuiGuestMemory
	{
		if (block.IsNull) return;
		platform.Clear(block, size);
		platform.Free(block, size);
	}
}

// Official MG09 pen/color attribute and method identifiers, resolved from the
// authority (libraries/mui.h in the frozen MorphOS 3.20 SDK, mirrored in the
// abi-inventory). Kept beside the core so the classification and dispatch stay
// exact.
public static class MuiColorAttributes
{
	// Colorfield.mui
	public const uint ColorfieldBlue = 0x8042d3b0u;
	public const uint ColorfieldGreen = 0x80424466u;
	public const uint ColorfieldPen = 0x8042713au;
	public const uint ColorfieldRed = 0x804279f6u;
	public const uint ColorfieldRgb = 0x8042677au;

	// Pendisplay.mui
	public const uint PendisplaySetColormap = 0x80426c80u;
	public const uint PendisplaySetMUIPen = 0x8042039du;
	public const uint PendisplaySetRGB = 0x8042c131u;
	public const uint PendisplayArgb = 0x804278d0u;
	public const uint PendisplayPen = 0x8042a748u;
	public const uint PendisplayReference = 0x8042dc24u;
	public const uint PendisplayRgbColor = 0x8042a1a9u;
	public const uint PendisplaySpec = 0x8042a204u;
	public const uint PendisplayXrgb = 0x8042de8au;

	// Penadjust.mui (private)
	public const uint PenadjustPsiMode = 0x80421cbbu;

	// Palette.mui (obsolete but supported)
	public const uint PaletteEntries = 0x8042a3d8u;
	public const uint PaletteGroupable = 0x80423e67u;
	public const uint PaletteNames = 0x8042c3a2u;

	// Coloradjust.mui
	public const uint ColoradjustAlpha = 0x8042a1f1u;
	public const uint ColoradjustArgb = 0x804250cau;
	public const uint ColoradjustBlue = 0x8042b8a3u;
	public const uint ColoradjustGreen = 0x804285abu;
	public const uint ColoradjustModeId = 0x8042ec59u;
	public const uint ColoradjustRed = 0x80420eaau;
	public const uint ColoradjustRgb = 0x8042f899u;
	public const uint ColoradjustShowAlpha = 0x8042e102u;
	public const uint ColoradjustXrgb = 0x8042cc13u;
}
