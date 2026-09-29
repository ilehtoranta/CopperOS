/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Named bounded text-span cursor for ExternalWrapper's Dtpic name and class-ID
// paths. Source/destination exchange and fixed-ID matching share the same
// address, overflow, and mapped-byte admission rules.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalTextByteCursor
{
	internal const uint MaximumLength = 257;
	internal APTR Base;
	internal uint Index;
	internal uint Length;
}

internal static class MuiExternalTextByteCursorCodec
{
	internal static bool TryReadByte<TPlatform>(ref TPlatform platform,
		MuiExternalTextByteCursor cursor, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (cursor.Length == 0 || cursor.Length >
			MuiExternalTextByteCursor.MaximumLength)
		{
			value = 0;
			return false;
		}
		var shared = default(MuiCStringByteCursor);
		shared.Base = cursor.Base;
		shared.Index = cursor.Index;
		shared.Limit = cursor.Length;
		return MuiCStringByteCursorCodec.TryReadByte(ref platform, shared,
			out value);
	}

	internal static bool TryWriteByte<TPlatform>(ref TPlatform platform,
		MuiExternalTextByteCursor cursor, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (cursor.Length == 0 || cursor.Length >
			MuiExternalTextByteCursor.MaximumLength) return false;
		var shared = default(MuiCStringByteCursor);
		shared.Base = cursor.Base;
		shared.Index = cursor.Index;
		shared.Limit = cursor.Length;
		return MuiCStringByteCursorCodec.TryWriteByte(ref platform, shared,
			value);
	}
}

// Fixed guest-memory layout for the MG09 external-resource wrapper family:
// the official Boopsi.mui and Dtpic.mui classes, both of which inherit from
// Area and both of which own an external resource (an opened BOOPSI class plus
// its object, or a datatypes.library picture object). The two classes share a
// single initialized guest-resident instance block discriminated by an exact,
// case-sensitive official class id. The family never allocates on the managed
// heap, holds no managed data, and never chains into the frozen common-control,
// collection, generic-object or layout cores or their dispatchers. It is
// deliberately additive: it only requires the MG09 service surface
// (IMuiServicePlatform), which now carries the narrow external BOOPSI loader
// seam (IMuiExternalBoopsiCapability) and the datatypes picture seam
// (IMuiDatatypeCapability).
internal static class MuiExternalWrapperLayout
{
	public const uint Magic = 0x4D455857;   // "MEXW"

	public const uint InstanceSize = 136;
	public const int Class = 4;
	public const int Flags = 8;

	// ---- Boopsi.mui ----------------------------------------------------------
	public const int BoopsiResources = 12;  // five-pointer resource record
	public const int BoopsiGeometry = 32;   // min/max and tag-id record
	public const int DisplayEnvironment = 60; // Window/Screen/DrawInfo record
	public const int ScratchState = 72;    // remember/work ownership record

	// ---- Dtpic.mui -----------------------------------------------------------
	public const int DtpicState = 84;       // name/picture/size record

	// ---- Shared notification (IDCMP_UPDATE -> MUI notification) --------------
	public const int NotificationState = 120;
	public const int RastPort = 132;        // live RastPort (display record tail)

	// Flags.
	public const uint FlagDisabled = 1u << 0;      // MUIA_Disabled
	public const uint FlagSetup = 1u << 1;         // MUIM_Setup seen (window open)
	public const uint FlagShown = 1u << 2;         // MUIM_Show seen
	public const uint FlagObjectCreated = 1u << 3; // boopsi object currently alive
	public const uint FlagSmart = 1u << 4;         // MUIA_Boopsi_Smart
	public const uint FlagColorwheel = 1u << 5;    // classId == colorwheel.gadget
	public const uint FlagFreeHoriz = 1u << 6;     // MUIA_Dtpic_FreeHoriz
	public const uint FlagFreeVert = 1u << 7;      // MUIA_Dtpic_FreeVert
	public const uint FlagLighten = 1u << 8;       // MUIA_Dtpic_LightenOnMouse
	public const uint FlagDarken = 1u << 9;        // MUIA_Dtpic_DarkenSelState
	public const uint FlagPicture = 1u << 10;      // picture object currently acquired
	public const uint FlagRedraw = 1u << 11;       // a redraw is pending

	// Owned block sizes.
	public const uint RememberSize = 40;    // 5 * (tag,value)
	public const uint WorkSize = 64;
	public const int MaxRemember = 5;
	public const int MaxNameLength = 256;   // bound on a copied MUIA_Dtpic_Name
	public const int MaxTagWalk = 64;       // bound on a creation tag-list walk

	// Documented defaults from MUI_Boopsi: 1x1 minimum, "unlimited" maximum
	// (represented as the MUI_MAXMAX sentinel used across the library).
	public const uint MaxDefault = 10000;

	// The RenderInfo record contract this wrapper reads at MUIM_Setup. MUI hands
	// the object a struct MUI_RenderInfo; this family only needs four opaque
	// guest pointers from it, published at these fixed offsets.
	public const int RiScreen = 0;
	public const int RiWindow = 4;
	public const int RiDrawInfo = 8;
	public const int RiRastPort = 12;
}

// Semantic view of the fixed regions in an ExternalWrapper instance block.
// Individual state codecs still validate their record sizes; this cursor owns
// only the shared instance-to-region ABI mapping and overflow check.
internal enum MuiExternalStateRegion : byte
{
	BoopsiResources,
	BoopsiGeometry,
	DisplayEnvironment,
	Scratch,
	Dtpic,
	Notification,
	RastPort,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalStateCursor
{
	internal APTR Instance;
	internal MuiExternalStateRegion Region;
}

internal static class MuiExternalStateMemoryCodec
{
	internal static bool TryGetAddress(APTR instance,
		MuiExternalStateRegion region, out APTR address)
	{
		address = APTR.Null;
		uint offset;
		switch (region)
		{
			case MuiExternalStateRegion.BoopsiResources:
				offset = unchecked((uint)MuiExternalWrapperLayout.BoopsiResources);
				break;
			case MuiExternalStateRegion.BoopsiGeometry:
				offset = unchecked((uint)MuiExternalWrapperLayout.BoopsiGeometry);
				break;
			case MuiExternalStateRegion.DisplayEnvironment:
				offset = unchecked((uint)MuiExternalWrapperLayout.DisplayEnvironment);
				break;
			case MuiExternalStateRegion.Scratch:
				offset = unchecked((uint)MuiExternalWrapperLayout.ScratchState);
				break;
			case MuiExternalStateRegion.Dtpic:
				offset = unchecked((uint)MuiExternalWrapperLayout.DtpicState);
				break;
			case MuiExternalStateRegion.Notification:
				offset = unchecked((uint)MuiExternalWrapperLayout.NotificationState);
				break;
			case MuiExternalStateRegion.RastPort:
				offset = unchecked((uint)MuiExternalWrapperLayout.RastPort);
				break;
			default:
				return false;
		}
		if (instance.IsNull || instance.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(instance.Raw + offset);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// state cursor. New code passes the instance and named region directly to the
// struct-backed memory adapter above.
internal static class MuiExternalStateCursorCodec
{
	internal static bool TryGetAddress(MuiExternalStateCursor cursor,
		out APTR address)
		=> MuiExternalStateMemoryCodec.TryGetAddress(cursor.Instance,
			cursor.Region, out address);
}

// The fixed Boopsi geometry/configuration block contains the caller-visible
// minimum/maximum values and the three tag ids used to patch the creation list.
// Keeping these seven words together makes the ABI boundary explicit and keeps
// the wrapper logic independent of private guest offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalBoopsiGeometryState
{
	internal const uint Size = 28;
	internal const uint FieldSize = 4;

	internal uint MinWidth;
	internal uint MinHeight;
	internal uint MaxWidth;
	internal uint MaxHeight;
	internal uint TagWindow;
	internal uint TagScreen;
	internal uint TagDrawInfo;
}

internal static class MuiExternalBoopsiGeometryCodec
{
	private static bool TryAddress<TPlatform>(ref TPlatform platform,
		APTR instance, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiExternalStateMemoryCodec.TryGetAddress(instance,
			MuiExternalStateRegion.BoopsiGeometry, out address))
			return false;
		return platform.IsMapped(address, MuiExternalBoopsiGeometryState.Size);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, out MuiExternalBoopsiGeometryState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryAddress(ref platform, instance, out var address) ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiExternalBoopsiGeometryState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MinWidth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MinHeight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MaxWidth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MaxHeight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.TagWindow) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.TagScreen) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.TagDrawInfo)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalBoopsiGeometryState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryAddress(ref platform, instance, out var address) ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiExternalBoopsiGeometryState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MinWidth) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MinHeight) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MaxWidth) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MaxHeight) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.TagWindow) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.TagScreen) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.TagDrawInfo)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal enum MuiExternalBoopsiGeometryField : byte
{
	MinWidth,
	MinHeight,
	MaxWidth,
	MaxHeight,
	TagWindow,
	TagScreen,
	TagDrawInfo,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalBoopsiGeometryFieldCursor
{
	internal APTR Instance;
	internal MuiExternalBoopsiGeometryField Field;
}

internal static class MuiExternalBoopsiGeometryFieldMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiExternalBoopsiGeometryField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		switch (field)
		{
			case MuiExternalBoopsiGeometryField.MinWidth:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiGeometryState.FieldSize, out address);
			case MuiExternalBoopsiGeometryField.MinHeight:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiGeometryState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiGeometryState.FieldSize, out address);
			case MuiExternalBoopsiGeometryField.MaxWidth:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiGeometryState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiGeometryState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiGeometryState.FieldSize, out address);
			case MuiExternalBoopsiGeometryField.MaxHeight:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiGeometryState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiGeometryState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiGeometryState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiGeometryState.FieldSize, out address);
			case MuiExternalBoopsiGeometryField.TagWindow:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiGeometryState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiGeometryState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiGeometryState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiGeometryState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiGeometryState.FieldSize, out address);
			case MuiExternalBoopsiGeometryField.TagScreen:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiGeometryState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiGeometryState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiGeometryState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiGeometryState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiGeometryState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiGeometryState.FieldSize, out address);
			case MuiExternalBoopsiGeometryField.TagDrawInfo:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiGeometryState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiGeometryState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiGeometryState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiGeometryState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiGeometryState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiGeometryState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiGeometryState.FieldSize, out address);
			default:
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalBoopsiGeometryField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiExternalStateMemoryCodec.TryGetAddress(instance,
			MuiExternalStateRegion.BoopsiGeometry,
			out var baseAddress) || !platform.IsMapped(baseAddress,
			MuiExternalBoopsiGeometryState.Size) ||
			!MuiGuestStructCursor.TryCreate(ref platform, baseAddress,
				MuiExternalBoopsiGeometryState.Size, out var cursor) ||
			!TryTakeField(ref platform, ref cursor, field, out address))
			return false;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalBoopsiGeometryField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, instance, field, out var address))
			return false;
		return MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalBoopsiGeometryField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, instance, field, out var address))
			return false;
		MuiGuestUlongStorageCodec.WriteValue(ref platform, address, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// Boopsi geometry field cursor. New code passes the instance and named field
// directly to the struct-backed memory adapter above.
internal static class MuiExternalBoopsiGeometryFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalBoopsiGeometryFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalBoopsiGeometryFieldMemoryCodec.TryGetAddress(ref platform,
			cursor.Instance, cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalBoopsiGeometryField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalBoopsiGeometryFieldMemoryCodec.TryRead(ref platform,
			instance, field, out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalBoopsiGeometryField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalBoopsiGeometryFieldMemoryCodec.TryWrite(ref platform,
			instance, field, value);
}

// The five contiguous pointer slots that describe a Boopsi wrapper’s external
// resource ownership and caller-provided inputs.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalBoopsiResourceState
{
	internal const uint Size = 20;
	internal const uint FieldSize = 4;

	internal APTR PrivateClass;
	internal APTR ClassId;
	internal APTR OpenedClass;
	internal APTR BoopsiObject;
	internal APTR CreationTags;
}

internal static class MuiExternalBoopsiResourceCodec
{
	private static bool TryAddress<TPlatform>(ref TPlatform platform,
		APTR instance, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiExternalStateMemoryCodec.TryGetAddress(instance,
			MuiExternalStateRegion.BoopsiResources, out address))
			return false;
		return platform.IsMapped(address, MuiExternalBoopsiResourceState.Size);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, out MuiExternalBoopsiResourceState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryAddress(ref platform, instance, out var address) ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiExternalBoopsiResourceState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var privateClass) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var classId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var openedClass) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var boopsiObject) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var creationTags)) return false;
		value.PrivateClass = APTR.FromPointer(privateClass);
		value.ClassId = APTR.FromPointer(classId);
		value.OpenedClass = APTR.FromPointer(openedClass);
		value.BoopsiObject = APTR.FromPointer(boopsiObject);
		value.CreationTags = APTR.FromPointer(creationTags);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalBoopsiResourceState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryAddress(ref platform, instance, out var address) ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiExternalBoopsiResourceState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.PrivateClass.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.ClassId.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.OpenedClass.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.BoopsiObject.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.CreationTags.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal enum MuiExternalBoopsiResourceField : byte
{
	PrivateClass,
	ClassId,
	OpenedClass,
	BoopsiObject,
	CreationTags,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalBoopsiResourceFieldCursor
{
	internal APTR Instance;
	internal MuiExternalBoopsiResourceField Field;
}

internal static class MuiExternalBoopsiResourceFieldMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiExternalBoopsiResourceField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		switch (field)
		{
			case MuiExternalBoopsiResourceField.PrivateClass:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiResourceState.FieldSize, out address);
			case MuiExternalBoopsiResourceField.ClassId:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiResourceState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiResourceState.FieldSize, out address);
			case MuiExternalBoopsiResourceField.OpenedClass:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiResourceState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiResourceState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiResourceState.FieldSize, out address);
			case MuiExternalBoopsiResourceField.BoopsiObject:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiResourceState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiResourceState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiResourceState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiResourceState.FieldSize, out address);
			case MuiExternalBoopsiResourceField.CreationTags:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiResourceState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiResourceState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiResourceState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalBoopsiResourceState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalBoopsiResourceState.FieldSize, out address);
			default:
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalBoopsiResourceField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiExternalStateMemoryCodec.TryGetAddress(instance,
			MuiExternalStateRegion.BoopsiResources,
			out var baseAddress) || !platform.IsMapped(baseAddress,
			MuiExternalBoopsiResourceState.Size) ||
			!MuiGuestStructCursor.TryCreate(ref platform, baseAddress,
				MuiExternalBoopsiResourceState.Size, out var cursor) ||
			!TryTakeField(ref platform, ref cursor, field, out address))
			return false;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalBoopsiResourceField field, out APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = APTR.Null;
		if (!TryGetAddress(ref platform, instance, field, out var address))
			return false;
		if (!MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out var raw)) return false;
		value = APTR.FromPointer(raw);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalBoopsiResourceField field, APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, instance, field, out var address))
			return false;
		MuiGuestUlongStorageCodec.WriteValue(ref platform, address, value.Raw);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// Boopsi resource field cursor. New code passes the instance and named field
// directly to the struct-backed memory adapter above.
internal static class MuiExternalBoopsiResourceFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalBoopsiResourceFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalBoopsiResourceFieldMemoryCodec.TryGetAddress(ref platform,
			cursor.Instance, cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalBoopsiResourceField field, out APTR value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalBoopsiResourceFieldMemoryCodec.TryRead(ref platform,
			instance, field, out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalBoopsiResourceField field, APTR value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalBoopsiResourceFieldMemoryCodec.TryWrite(ref platform,
			instance, field, value);
}

// Owned scratch storage used by both wrapper classes. The remember list keeps
// Boopsi tag values across regeneration; the work block is the common message
// packet scratch for Boopsi calls and Dtpic layout.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalScratchState
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;

	internal APTR RememberBuffer;
	internal uint RememberCount;
	internal APTR WorkBuffer;
}

internal static class MuiExternalScratchStateCodec
{
	private static bool TryAddress<TPlatform>(ref TPlatform platform,
		APTR instance, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiExternalStateMemoryCodec.TryGetAddress(instance,
			MuiExternalStateRegion.Scratch, out address))
			return false;
		return platform.IsMapped(address, MuiExternalScratchState.Size);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, out MuiExternalScratchState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryAddress(ref platform, instance, out var address) ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiExternalScratchState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rememberBuffer) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.RememberCount) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var workBuffer)) return false;
		value.RememberBuffer = APTR.FromPointer(rememberBuffer);
		value.WorkBuffer = APTR.FromPointer(workBuffer);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalScratchState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryAddress(ref platform, instance, out var address) ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiExternalScratchState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.RememberBuffer.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.RememberCount) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.WorkBuffer.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal enum MuiExternalScratchField : byte
{
	RememberBuffer,
	RememberCount,
	WorkBuffer,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalScratchFieldCursor
{
	internal APTR Instance;
	internal MuiExternalScratchField Field;
}

internal static class MuiExternalScratchFieldMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiExternalScratchField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		switch (field)
		{
			case MuiExternalScratchField.RememberBuffer:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalScratchState.FieldSize, out address);
			case MuiExternalScratchField.RememberCount:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalScratchState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalScratchState.FieldSize, out address);
			case MuiExternalScratchField.WorkBuffer:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalScratchState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalScratchState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalScratchState.FieldSize, out address);
			default:
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalScratchField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiExternalStateMemoryCodec.TryGetAddress(instance,
			MuiExternalStateRegion.Scratch,
			out var baseAddress) || !platform.IsMapped(baseAddress,
			MuiExternalScratchState.Size) ||
			!MuiGuestStructCursor.TryCreate(ref platform, baseAddress,
				MuiExternalScratchState.Size, out var cursor) ||
			!TryTakeField(ref platform, ref cursor, field, out address))
			return false;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalScratchField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, instance, field, out var address))
			return false;
		return MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalScratchField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, instance, field, out var address))
			return false;
		MuiGuestUlongStorageCodec.WriteValue(ref platform, address, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// scratch field cursor. New code passes the instance and named field directly
// to the struct-backed memory adapter above.
internal static class MuiExternalScratchFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalScratchFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalScratchFieldMemoryCodec.TryGetAddress(ref platform,
			cursor.Instance, cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalScratchField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalScratchFieldMemoryCodec.TryRead(ref platform, instance,
			field, out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalScratchField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalScratchFieldMemoryCodec.TryWrite(ref platform, instance,
			field, value);
}

// All Dtpic-owned and caller-facing scalar state is one contiguous guest block:
// the caller name, owned copy, picture handle, alpha, explicit minimums, and
// laid-out natural dimensions.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalDtpicState
{
	internal const uint Size = 36;
	internal const uint FieldSize = 4;

	internal APTR CallerName;
	internal APTR OwnedName;
	internal uint OwnedNameSize;
	internal APTR PictureObject;
	internal uint Alpha;
	internal uint MinWidth;
	internal uint MinHeight;
	internal uint PicWidth;
	internal uint PicHeight;
}

internal static class MuiExternalDtpicStateCodec
{
	private static bool TryAddress<TPlatform>(ref TPlatform platform,
		APTR instance, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiExternalStateMemoryCodec.TryGetAddress(instance,
			MuiExternalStateRegion.Dtpic, out address))
			return false;
		return platform.IsMapped(address, MuiExternalDtpicState.Size);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, out MuiExternalDtpicState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryAddress(ref platform, instance, out var address) ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiExternalDtpicState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var callerName) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var ownedName) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.OwnedNameSize) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var pictureObject) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Alpha) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MinWidth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MinHeight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.PicWidth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.PicHeight)) return false;
		value.CallerName = APTR.FromPointer(callerName);
		value.OwnedName = APTR.FromPointer(ownedName);
		value.PictureObject = APTR.FromPointer(pictureObject);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalDtpicState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryAddress(ref platform, instance, out var address) ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiExternalDtpicState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.CallerName.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.OwnedName.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.OwnedNameSize) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.PictureObject.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Alpha) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MinWidth) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MinHeight) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.PicWidth) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.PicHeight)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal enum MuiExternalDtpicField : byte
{
	CallerName,
	OwnedName,
	OwnedNameSize,
	PictureObject,
	Alpha,
	MinWidth,
	MinHeight,
	PicWidth,
	PicHeight,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalDtpicFieldCursor
{
	internal APTR Instance;
	internal MuiExternalDtpicField Field;
}

internal static class MuiExternalDtpicFieldMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiExternalDtpicField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		switch (field)
		{
			case MuiExternalDtpicField.CallerName:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDtpicState.FieldSize, out address);
			case MuiExternalDtpicField.OwnedName:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDtpicState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDtpicState.FieldSize, out address);
			case MuiExternalDtpicField.OwnedNameSize:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDtpicState.FieldSize, out address);
			case MuiExternalDtpicField.PictureObject:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDtpicState.FieldSize, out address);
			case MuiExternalDtpicField.Alpha:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDtpicState.FieldSize, out address);
			case MuiExternalDtpicField.MinWidth:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDtpicState.FieldSize, out address);
			case MuiExternalDtpicField.MinHeight:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDtpicState.FieldSize, out address);
			case MuiExternalDtpicField.PicWidth:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDtpicState.FieldSize, out address);
			case MuiExternalDtpicField.PicHeight:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDtpicState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDtpicState.FieldSize, out address);
			default:
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalDtpicField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiExternalStateMemoryCodec.TryGetAddress(instance,
			MuiExternalStateRegion.Dtpic, out var baseAddress) ||
			!MuiGuestStructCursor.TryCreate(ref platform, baseAddress,
				MuiExternalDtpicState.Size, out var cursor)) return false;
		return TryTakeField(ref platform, ref cursor, field, out address);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalDtpicField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, instance, field, out var address))
			return false;
		return MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalDtpicField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, instance, field, out var address))
			return false;
		MuiGuestUlongStorageCodec.WriteValue(ref platform, address, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// Dtpic field cursor. New code passes the instance and named field directly to
// the struct-backed memory adapter above.
internal static class MuiExternalDtpicFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalDtpicFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalDtpicFieldMemoryCodec.TryGetAddress(ref platform,
			cursor.Instance, cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalDtpicField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalDtpicFieldMemoryCodec.TryRead(ref platform, instance,
			field, out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalDtpicField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalDtpicFieldMemoryCodec.TryWrite(ref platform, instance,
			field, value);
}

// datatypes.library writes the laid-out picture dimensions into this compact
// caller-provided work record. Keep the result as a named guest struct so the
// Dtpic lifecycle does not depend on ad-hoc width/height offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalDtpicLayoutResult
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;

	internal uint Width;
	internal uint Height;
}

internal enum MuiExternalDtpicLayoutField : byte
{
	Width,
	Height,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalDtpicLayoutFieldCursor
{
	internal APTR Result;
	internal MuiExternalDtpicLayoutField Field;
}

internal static class MuiExternalDtpicLayoutFieldMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR result, MuiExternalDtpicLayoutField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, result,
			MuiExternalDtpicLayoutResult.Size, out var cursor)) return false;
		switch (field)
		{
			case MuiExternalDtpicLayoutField.Width:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDtpicLayoutResult.FieldSize, out address);
			case MuiExternalDtpicLayoutField.Height:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDtpicLayoutResult.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDtpicLayoutResult.FieldSize, out address);
			default:
				return false;
		}
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR result, MuiExternalDtpicLayoutField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, result, field, out var address))
			return false;
		return MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out value);
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// Dtpic layout-result cursor. New code passes the result address and named
// field directly to the struct-backed memory adapter above.
internal static class MuiExternalDtpicLayoutFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalDtpicLayoutFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalDtpicLayoutFieldMemoryCodec.TryGetAddress(ref platform,
			cursor.Result, cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR result, MuiExternalDtpicLayoutField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalDtpicLayoutFieldMemoryCodec.TryRead(ref platform, result,
			field, out value);
}

internal static class MuiExternalDtpicLayoutResultCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiExternalDtpicLayoutResult value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalDtpicLayoutResult.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Width) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Height)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// The most recent notification and its monotonic count are one shared guest
// record. Queries and notification recording consume this named state rather
// than repeating the three private offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalNotificationState
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;

	internal uint Attribute;
	internal uint Value;
	internal uint Count;
}

internal static class MuiExternalNotificationStateCodec
{
	private static bool TryAddress<TPlatform>(ref TPlatform platform,
		APTR instance, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiExternalStateMemoryCodec.TryGetAddress(instance,
			MuiExternalStateRegion.Notification, out address))
			return false;
		return platform.IsMapped(address, MuiExternalNotificationState.Size);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, out MuiExternalNotificationState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryAddress(ref platform, instance, out var address) ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiExternalNotificationState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Attribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Value) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Count)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalNotificationState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryAddress(ref platform, instance, out var address) ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiExternalNotificationState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Value) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Count)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal enum MuiExternalNotificationField : byte
{
	Attribute,
	Value,
	Count,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalNotificationFieldCursor
{
	internal APTR Instance;
	internal MuiExternalNotificationField Field;
}

internal static class MuiExternalNotificationFieldMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiExternalNotificationField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		switch (field)
		{
			case MuiExternalNotificationField.Attribute:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalNotificationState.FieldSize, out address);
			case MuiExternalNotificationField.Value:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalNotificationState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalNotificationState.FieldSize, out address);
			case MuiExternalNotificationField.Count:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalNotificationState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalNotificationState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalNotificationState.FieldSize, out address);
			default:
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalNotificationField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiExternalStateMemoryCodec.TryGetAddress(instance,
			MuiExternalStateRegion.Notification, out var baseAddress) ||
			!MuiGuestStructCursor.TryCreate(ref platform, baseAddress,
				MuiExternalNotificationState.Size, out var cursor)) return false;
		return TryTakeField(ref platform, ref cursor, field, out address);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalNotificationField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, instance, field, out var address))
			return false;
		return MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalNotificationField field, uint value)
	where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, instance, field, out var address))
			return false;
		MuiGuestUlongStorageCodec.WriteValue(ref platform, address, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// notification field cursor. New code passes the instance and named field
// directly to the struct-backed memory adapter above.
internal static class MuiExternalNotificationFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalNotificationFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalNotificationFieldMemoryCodec.TryGetAddress(ref platform,
			cursor.Instance, cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalNotificationField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalNotificationFieldMemoryCodec.TryRead(ref platform, instance,
			field, out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalNotificationField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalNotificationFieldMemoryCodec.TryWrite(ref platform, instance,
			field, value);
}

// Setup publishes three render-environment pointers in one contiguous guest
// block and keeps the RastPort at the tail of the instance. The named record
// mirrors that logical display state while the codec owns the two guest spans.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalDisplayState
{
	internal const uint Size = 16;

	internal APTR Window;
	internal APTR Screen;
	internal APTR DrawInfo;
	internal APTR RastPort;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalDisplayEnvironmentRecord
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;

	internal APTR Window;
	internal APTR Screen;
	internal APTR DrawInfo;
}

internal enum MuiExternalDisplayEnvironmentField : byte
{
	Window,
	Screen,
	DrawInfo,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalDisplayEnvironmentFieldCursor
{
	internal APTR Environment;
	internal MuiExternalDisplayEnvironmentField Field;
}

internal static class MuiExternalDisplayEnvironmentFieldMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor,
		MuiExternalDisplayEnvironmentField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		switch (field)
		{
			case MuiExternalDisplayEnvironmentField.Window:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDisplayEnvironmentRecord.FieldSize, out address);
			case MuiExternalDisplayEnvironmentField.Screen:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDisplayEnvironmentRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDisplayEnvironmentRecord.FieldSize, out address);
			case MuiExternalDisplayEnvironmentField.DrawInfo:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDisplayEnvironmentRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalDisplayEnvironmentRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalDisplayEnvironmentRecord.FieldSize, out address);
			default:
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR environment, MuiExternalDisplayEnvironmentField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, environment,
			MuiExternalDisplayEnvironmentRecord.Size, out var cursor)) return false;
		return TryTakeField(ref platform, ref cursor, field, out address);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR environment, MuiExternalDisplayEnvironmentField field,
		out APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = APTR.Null;
		if (!TryGetAddress(ref platform, environment, field, out var address))
			return false;
		if (!MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out var raw)) return false;
		value = APTR.FromPointer(raw);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR environment, MuiExternalDisplayEnvironmentField field,
		APTR value)
	where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, environment, field, out var address))
			return false;
		MuiGuestUlongStorageCodec.WriteValue(ref platform, address, value.Raw);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// display-environment field cursor. New code passes the environment address
// and named field directly to the struct-backed memory adapter above.
internal static class MuiExternalDisplayEnvironmentFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalDisplayEnvironmentFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalDisplayEnvironmentFieldMemoryCodec.TryGetAddress(ref platform,
			cursor.Environment, cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR environment, MuiExternalDisplayEnvironmentField field,
		out APTR value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalDisplayEnvironmentFieldMemoryCodec.TryRead(ref platform,
			environment, field, out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR environment, MuiExternalDisplayEnvironmentField field, APTR value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalDisplayEnvironmentFieldMemoryCodec.TryWrite(ref platform,
			environment, field, value);
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalRastPortSlot
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;

	internal APTR RastPort;
}

internal enum MuiExternalRastPortSlotField : byte
{
	RastPort,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalRastPortSlotFieldCursor
{
	internal APTR Slot;
	internal MuiExternalRastPortSlotField Field;
}

internal static class MuiExternalRastPortSlotFieldMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR slot, MuiExternalRastPortSlotField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (field != MuiExternalRastPortSlotField.RastPort ||
			!MuiGuestStructCursor.TryCreate(ref platform, slot,
				MuiExternalRastPortSlot.Size, out var cursor)) return false;
		return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiExternalRastPortSlot.FieldSize, out address);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR slot, MuiExternalRastPortSlotField field, out APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = APTR.Null;
		if (!TryGetAddress(ref platform, slot, field, out var address))
			return false;
		if (!MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out var raw)) return false;
		value = APTR.FromPointer(raw);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR slot, MuiExternalRastPortSlotField field, APTR value)
	where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, slot, field, out var address))
			return false;
		MuiGuestUlongStorageCodec.WriteValue(ref platform, address, value.Raw);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// RastPort slot cursor. New code passes the slot address and named field
// directly to the struct-backed memory adapter above.
internal static class MuiExternalRastPortSlotFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalRastPortSlotFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalRastPortSlotFieldMemoryCodec.TryGetAddress(ref platform,
			cursor.Slot, cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR slot, MuiExternalRastPortSlotField field, out APTR value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalRastPortSlotFieldMemoryCodec.TryRead(ref platform, slot,
			field, out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR slot, MuiExternalRastPortSlotField field, APTR value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalRastPortSlotFieldMemoryCodec.TryWrite(ref platform, slot,
			field, value);
}

internal static class MuiExternalDisplayEnvironmentCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiExternalDisplayEnvironmentRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalDisplayEnvironmentRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var window) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var screen) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var drawInfo)) return false;
		value.Window = APTR.FromPointer(window);
		value.Screen = APTR.FromPointer(screen);
		value.DrawInfo = APTR.FromPointer(drawInfo);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalDisplayEnvironmentRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalDisplayEnvironmentRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Window.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Screen.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.DrawInfo.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiExternalRastPortSlotCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WriteValue<TPlatform>(ref TPlatform platform,
		APTR address, APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalRastPortSlot.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalRastPortSlot.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var raw)) return false;
		value = APTR.FromPointer(raw);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiExternalRastPortSlot value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadValue(ref platform, address, out var rastPort)) return false;
		value.RastPort = rastPort;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalRastPortSlot value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteValue(ref platform, address, value.RastPort);
}

internal static class MuiExternalDisplayStateCodec
{
	private static bool TryAddresses<TPlatform>(ref TPlatform platform,
		APTR instance, out APTR environment, out APTR rastPort)
		where TPlatform : struct, IMuiGuestMemory
	{
		environment = APTR.Null;
		rastPort = APTR.Null;
		if (!MuiExternalStateMemoryCodec.TryGetAddress(instance,
			MuiExternalStateRegion.DisplayEnvironment, out environment) ||
			!MuiExternalStateMemoryCodec.TryGetAddress(instance,
				MuiExternalStateRegion.RastPort, out rastPort)) return false;
		return platform.IsMapped(environment, 12) &&
			platform.IsMapped(rastPort, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, out MuiExternalDisplayState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryAddresses(ref platform, instance, out var environment,
			out var rastPort)) return false;
		if (!MuiExternalDisplayEnvironmentCodec.TryRead(ref platform,
			environment, out var environmentValue) ||
			!MuiExternalRastPortSlotCodec.TryRead(ref platform, rastPort,
				out var rastPortValue)) return false;
		value.Window = environmentValue.Window;
		value.Screen = environmentValue.Screen;
		value.DrawInfo = environmentValue.DrawInfo;
		value.RastPort = rastPortValue.RastPort;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalDisplayState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryAddresses(ref platform, instance, out var environment,
			out var rastPort)) return false;
		var environmentValue = default(MuiExternalDisplayEnvironmentRecord);
		environmentValue.Window = value.Window;
		environmentValue.Screen = value.Screen;
		environmentValue.DrawInfo = value.DrawInfo;
		var rastPortValue = default(MuiExternalRastPortSlot);
		rastPortValue.RastPort = value.RastPort;
		return MuiExternalDisplayEnvironmentCodec.Write(ref platform,
			environment, environmentValue) &&
			MuiExternalRastPortSlotCodec.Write(ref platform, rastPort,
				rastPortValue);
	}
}

// MUI supplies this fixed four-pointer RenderInfo record during MUIM_Setup.
// Keep the guest-facing input as a named record so setup code consumes a
// decoded ABI value instead of scattering RenderInfo field offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalRenderInfoRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;

	internal APTR Screen;
	internal APTR Window;
	internal APTR DrawInfo;
	internal APTR RastPort;
}

internal enum MuiExternalRenderInfoField : byte
{
	Screen,
	Window,
	DrawInfo,
	RastPort,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalRenderInfoFieldCursor
{
	internal APTR RenderInfo;
	internal MuiExternalRenderInfoField Field;
}

internal static class MuiExternalRenderInfoFieldMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiExternalRenderInfoField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		switch (field)
		{
			case MuiExternalRenderInfoField.Screen:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalRenderInfoRecord.FieldSize, out address);
			case MuiExternalRenderInfoField.Window:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalRenderInfoRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalRenderInfoRecord.FieldSize, out address);
			case MuiExternalRenderInfoField.DrawInfo:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalRenderInfoRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalRenderInfoRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalRenderInfoRecord.FieldSize, out address);
			case MuiExternalRenderInfoField.RastPort:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalRenderInfoRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalRenderInfoRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalRenderInfoRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalRenderInfoRecord.FieldSize, out address);
			default:
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR renderInfo, MuiExternalRenderInfoField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, renderInfo,
			MuiExternalRenderInfoRecord.Size, out var cursor)) return false;
		return TryTakeField(ref platform, ref cursor, field, out address);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR renderInfo, MuiExternalRenderInfoField field, out APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = APTR.Null;
		if (!TryGetAddress(ref platform, renderInfo, field, out var address))
			return false;
		if (!MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out var raw)) return false;
		value = APTR.FromPointer(raw);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// RenderInfo field cursor. New code passes the record address and named field
// directly to the struct-backed memory adapter above.
internal static class MuiExternalRenderInfoFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalRenderInfoFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalRenderInfoFieldMemoryCodec.TryGetAddress(ref platform,
			cursor.RenderInfo, cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR renderInfo, MuiExternalRenderInfoField field, out APTR value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalRenderInfoFieldMemoryCodec.TryRead(ref platform, renderInfo,
			field, out value);
}

internal static class MuiExternalRenderInfoCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiExternalRenderInfoRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalRenderInfoRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var screen) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var window) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var drawInfo) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rastPort)) return false;
		value.Screen = APTR.FromPointer(screen);
		value.Window = APTR.FromPointer(window);
		value.DrawInfo = APTR.FromPointer(drawInfo);
		value.RastPort = APTR.FromPointer(rastPort);
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// The first three words of every external-wrapper instance form one stable
// guest-owned header. Keep the class discriminator and flag word together so
// lifecycle code does not scatter private header offsets through the family.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalWrapperHeader
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint Cookie = MuiExternalWrapperLayout.Magic;

	internal uint Magic;
	internal MuiExternalWrapperClass Class;
	internal uint Flags;
}

internal enum MuiExternalWrapperHeaderField : byte
{
	Magic,
	Class,
	Flags,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalWrapperHeaderFieldCursor
{
	internal APTR Header;
	internal MuiExternalWrapperHeaderField Field;
}

internal static class MuiExternalWrapperHeaderFieldMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiExternalWrapperHeaderField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		switch (field)
		{
			case MuiExternalWrapperHeaderField.Magic:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalWrapperHeader.FieldSize, out address);
			case MuiExternalWrapperHeaderField.Class:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalWrapperHeader.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalWrapperHeader.FieldSize, out address);
			case MuiExternalWrapperHeaderField.Flags:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalWrapperHeader.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiExternalWrapperHeader.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiExternalWrapperHeader.FieldSize, out address);
			default:
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR header, MuiExternalWrapperHeaderField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, header,
			MuiExternalWrapperHeader.Size, out var cursor)) return false;
		return TryTakeField(ref platform, ref cursor, field, out address);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR header, MuiExternalWrapperHeaderField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, header, field, out var address))
			return false;
		return MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR header, MuiExternalWrapperHeaderField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, header, field, out var address))
			return false;
		MuiGuestUlongStorageCodec.WriteValue(ref platform, address, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// header cursor. New code passes the header address and named field directly
// to the struct-backed memory adapter above.
internal static class MuiExternalWrapperHeaderFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalWrapperHeaderFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalWrapperHeaderFieldMemoryCodec.TryGetAddress(ref platform,
			cursor.Header, cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		MuiExternalWrapperHeaderFieldCursor cursor, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalWrapperHeaderFieldMemoryCodec.TryRead(ref platform,
			cursor.Header, cursor.Field, out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		MuiExternalWrapperHeaderFieldCursor cursor, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalWrapperHeaderFieldMemoryCodec.TryWrite(ref platform,
			cursor.Header, cursor.Field, value);
}

internal static class MuiExternalWrapperHeaderCodec
{
	// Declaration-order wrapper header: Magic, class discriminator, and
	// lifecycle flags. Production exchange uses the bounded named cursor;
	// field addressing remains a compatibility seam for diagnostics.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiExternalWrapperHeader value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalWrapperHeader.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var magic) || magic != MuiExternalWrapperHeader.Cookie ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var cls) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Flags)) return false;
		value.Magic = magic;
		value.Class = (MuiExternalWrapperClass)cls;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalWrapperHeader value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalWrapperHeader.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			(uint)value.Class) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Flags) && MuiGuestStructCursor.IsComplete(cursor);

	// Structural ABI reader. This intentionally validates only the packed
	// header record so codec tests and low-level recovery paths can inspect a
	// header before the surrounding sidecar has been admitted.
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiExternalWrapperHeader value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	// Live consumers use the strict reader. It keeps the named header codec as
	// the only ABI boundary, then applies the class-specific state topology
	// rules before any external resource or method dispatch can observe it.
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiExternalWrapperHeader value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadStructural(ref platform, address, out value)) return false;
		return MuiExternalWrapperAdmission.Validate(ref platform, address, value);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalWrapperHeader value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiExternalWrapperHeader.Size) ||
			value.Magic != MuiExternalWrapperHeader.Cookie ||
			value.Class == MuiExternalWrapperClass.None)
			return false;
		return WriteRecord(ref platform, address, value);
	}
}

// The wrapper class discriminator. Both classes inherit from Area.
public enum MuiExternalWrapperClass : uint
{
	None = 0,
	Boopsi = 1,   // Boopsi.mui : Area
	Dtpic = 2,    // Dtpic.mui  : Area
}

// Strict admission for a live external-wrapper sidecar. The structural
// codecs above own every packed field; this validator only composes their
// named records into the invariants required by lifecycle and dispatch code.
// In particular, it rejects cross-class flags, impossible resource ownership,
// unmapped owned blocks, and picture/name states that would make teardown walk
// arbitrary guest memory. No managed state or exceptions are involved.
internal static class MuiExternalWrapperAdmission
{
	private const uint AllFlags =
		MuiExternalWrapperLayout.FlagDisabled |
		MuiExternalWrapperLayout.FlagSetup |
		MuiExternalWrapperLayout.FlagShown |
		MuiExternalWrapperLayout.FlagObjectCreated |
		MuiExternalWrapperLayout.FlagSmart |
		MuiExternalWrapperLayout.FlagColorwheel |
		MuiExternalWrapperLayout.FlagFreeHoriz |
		MuiExternalWrapperLayout.FlagFreeVert |
		MuiExternalWrapperLayout.FlagLighten |
		MuiExternalWrapperLayout.FlagDarken |
		MuiExternalWrapperLayout.FlagPicture |
		MuiExternalWrapperLayout.FlagRedraw;

	private const uint BoopsiOnlyFlags =
		MuiExternalWrapperLayout.FlagObjectCreated |
		MuiExternalWrapperLayout.FlagSmart |
		MuiExternalWrapperLayout.FlagColorwheel;

	private const uint DtpicOnlyFlags =
		MuiExternalWrapperLayout.FlagFreeHoriz |
		MuiExternalWrapperLayout.FlagFreeVert |
		MuiExternalWrapperLayout.FlagLighten |
		MuiExternalWrapperLayout.FlagDarken |
		MuiExternalWrapperLayout.FlagPicture;

	internal static bool ValidateHeader(MuiExternalWrapperHeader header)
	{
		if (header.Magic != MuiExternalWrapperHeader.Cookie ||
			(header.Class != MuiExternalWrapperClass.Boopsi &&
				header.Class != MuiExternalWrapperClass.Dtpic)) return false;
		if ((header.Flags & ~AllFlags) != 0 ||
			(header.Flags & MuiExternalWrapperLayout.FlagShown) != 0 &&
			(header.Flags & MuiExternalWrapperLayout.FlagSetup) == 0 ||
			(header.Flags & MuiExternalWrapperLayout.FlagObjectCreated) != 0 &&
			(header.Flags & MuiExternalWrapperLayout.FlagSetup) == 0 ||
			(header.Flags & MuiExternalWrapperLayout.FlagPicture) != 0 &&
			(header.Flags & MuiExternalWrapperLayout.FlagSetup) == 0) return false;
		if (header.Class == MuiExternalWrapperClass.Boopsi)
			return (header.Flags & DtpicOnlyFlags) == 0;
		return (header.Flags & BoopsiOnlyFlags) == 0;
	}

	internal static bool Validate<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalWrapperHeader header)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (instance.IsNull ||
			!platform.IsMapped(instance, MuiExternalWrapperLayout.InstanceSize) ||
			!ValidateHeader(header)) return false;
		if (!MuiExternalScratchStateCodec.TryRead(ref platform, instance,
			out var scratch) || scratch.RememberCount >
			MuiExternalWrapperLayout.MaxRemember || scratch.WorkBuffer.IsNull ||
			!platform.IsMapped(scratch.WorkBuffer,
				MuiExternalWrapperLayout.WorkSize)) return false;

		if (header.Class == MuiExternalWrapperClass.Boopsi)
			return ValidateBoopsi(ref platform, instance, scratch, header.Flags);
		return ValidateDtpic(ref platform, instance, scratch, header.Flags);
	}

	private static bool ValidateBoopsi<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalScratchState scratch, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (scratch.RememberBuffer.IsNull ||
			!platform.IsMapped(scratch.RememberBuffer,
				MuiExternalWrapperLayout.RememberSize) ||
			!MuiExternalDtpicStateCodec.TryRead(ref platform, instance,
				out var dtpic) || dtpic.CallerName.IsNotNull ||
			dtpic.OwnedName.IsNotNull || dtpic.OwnedNameSize != 0 ||
			dtpic.PictureObject.IsNotNull || dtpic.PicWidth != 0 ||
			dtpic.PicHeight != 0) return false;
		if (!MuiExternalBoopsiResourceCodec.TryRead(ref platform, instance,
			out var resources) ||
			(flags & MuiExternalWrapperLayout.FlagObjectCreated) == 0 &&
			resources.BoopsiObject.IsNotNull ||
			(flags & MuiExternalWrapperLayout.FlagObjectCreated) != 0 &&
			resources.BoopsiObject.IsNull) return false;
		return true;
	}

	private static bool ValidateDtpic<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalScratchState scratch, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (scratch.RememberBuffer.IsNotNull || scratch.RememberCount != 0 ||
			!MuiExternalBoopsiResourceCodec.TryRead(ref platform, instance,
				out var resources) || resources.PrivateClass.IsNotNull ||
			resources.ClassId.IsNotNull || resources.OpenedClass.IsNotNull ||
			resources.BoopsiObject.IsNotNull || resources.CreationTags.IsNotNull ||
			!MuiExternalDtpicStateCodec.TryRead(ref platform, instance,
				out var dtpic)) return false;
		if (dtpic.OwnedName.IsNull != (dtpic.OwnedNameSize == 0) ||
			(dtpic.OwnedName.IsNotNull &&
				(dtpic.OwnedNameSize > MuiExternalWrapperLayout.MaxNameLength + 1 ||
					dtpic.OwnedNameSize == 0 ||
					!platform.IsMapped(dtpic.OwnedName, dtpic.OwnedNameSize))) ||
			(dtpic.PictureObject.IsNull !=
				((flags & MuiExternalWrapperLayout.FlagPicture) == 0)) ||
			(dtpic.PictureObject.IsNull &&
				(dtpic.PicWidth != 0 || dtpic.PicHeight != 0))) return false;
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalBoopsiOpSetMessage
{
	internal const uint Size = 12;
	internal uint MethodId;
	internal APTR AttributeList;
	internal APTR GadgetInfo;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalBoopsiOpGetMessage
{
	internal const uint Size = 12;
	internal uint MethodId;
	internal uint Attribute;
	internal APTR Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalBoopsiRenderMessage
{
	internal const uint Size = 12;
	internal uint MethodId;
	internal APTR GadgetInfo;
	internal APTR RastPort;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalBoopsiTagItem
{
	internal const uint Size = 8;
	internal uint Tag;
	internal uint Data;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalBoopsiResultWord
{
	internal const uint Size = 4;
	internal uint Value;
}

// Complete declaration-order codecs for the fixed BOOPSI packet records. The
// legacy field adapter below remains available for compatibility diagnostics,
// but live packet exchange goes through these named records and one bounded
// cursor per record. This keeps packet access independent of repeated offsets
// while preserving the exact MorphOS 32-bit ABI words.
internal static class MuiExternalBoopsiOpSetMessageStructCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiExternalBoopsiOpSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalBoopsiOpSetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var attributeList) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var gadgetInfo)) return false;
		value.MethodId = methodId;
		value.AttributeList = APTR.FromPointer(attributeList);
		value.GadgetInfo = APTR.FromPointer(gadgetInfo);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalBoopsiOpSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalBoopsiOpSetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.AttributeList.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.GadgetInfo.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiExternalBoopsiOpGetMessageStructCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiExternalBoopsiOpGetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalBoopsiOpGetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var attribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var storage)) return false;
		value.MethodId = methodId;
		value.Attribute = attribute;
		value.Storage = APTR.FromPointer(storage);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalBoopsiOpGetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalBoopsiOpGetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Storage.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiExternalBoopsiRenderMessageStructCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiExternalBoopsiRenderMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalBoopsiRenderMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var gadgetInfo) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rastPort)) return false;
		value.MethodId = methodId;
		value.GadgetInfo = APTR.FromPointer(gadgetInfo);
		value.RastPort = APTR.FromPointer(rastPort);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalBoopsiRenderMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalBoopsiRenderMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.GadgetInfo.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.RastPort.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiExternalBoopsiTagItemStructCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiExternalBoopsiTagItem value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalBoopsiTagItem.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var tag) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var data)) return false;
		value.Tag = tag;
		value.Data = data;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalBoopsiTagItem value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalBoopsiTagItem.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Tag) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Data)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiExternalBoopsiResultWordStructCodec
{
	// Keep scalar-safe entry points beside the named record so the native
	// boundary never has to pass this one-field struct by value.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out value);

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WriteValue<TPlatform>(ref TPlatform platform,
		APTR address, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestUlongStorageCodec.WriteValue(ref platform, address, value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiExternalBoopsiResultWord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadValue(ref platform, address,
			out var result)) return false;
		value.Value = result;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalBoopsiResultWord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return WriteValue(ref platform, address, value.Value);
	}
}

internal enum MuiExternalBoopsiPacketKind : byte
{
	OpSet,
	OpGet,
	Render,
	Tag,
	Result,
}

internal enum MuiExternalBoopsiPacketField : byte
{
	MethodId,
	AttributeList,
	GadgetInfo,
	Attribute,
	Storage,
	RastPort,
	Tag,
	Data,
	Value,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalBoopsiPacketFieldCursor
{
	internal APTR Packet;
	internal MuiExternalBoopsiPacketKind Kind;
	internal MuiExternalBoopsiPacketField Field;
}

internal static class MuiExternalBoopsiPacketFieldMemoryCodec
{
	private static bool TryTakeOpSetField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiExternalBoopsiPacketField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		switch (field)
		{
			case MuiExternalBoopsiPacketField.MethodId:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
					out address);
			case MuiExternalBoopsiPacketField.AttributeList:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
					out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
					out address);
			case MuiExternalBoopsiPacketField.GadgetInfo:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
					out _) || !MuiGuestStructCursor.TryTake(ref platform,
					ref cursor, 4, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
					out address);
			default:
				return false;
		}
	}

	private static bool TryTakeOpGetField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiExternalBoopsiPacketField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		switch (field)
		{
			case MuiExternalBoopsiPacketField.MethodId:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
					out address);
			case MuiExternalBoopsiPacketField.Attribute:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
					out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
					out address);
			case MuiExternalBoopsiPacketField.Storage:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
					out _) || !MuiGuestStructCursor.TryTake(ref platform,
					ref cursor, 4, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
					out address);
			default:
				return false;
		}
	}

	private static bool TryTakeRenderField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiExternalBoopsiPacketField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		switch (field)
		{
			case MuiExternalBoopsiPacketField.MethodId:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
					out address);
			case MuiExternalBoopsiPacketField.GadgetInfo:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
					out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
					out address);
			case MuiExternalBoopsiPacketField.RastPort:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
					out _) || !MuiGuestStructCursor.TryTake(ref platform,
					ref cursor, 4, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
					out address);
			default:
				return false;
		}
	}

	private static bool TryTakeTagField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiExternalBoopsiPacketField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		switch (field)
		{
			case MuiExternalBoopsiPacketField.Tag:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
					out address);
			case MuiExternalBoopsiPacketField.Data:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
					out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
					out address);
			default:
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR packet, MuiExternalBoopsiPacketKind kind,
		MuiExternalBoopsiPacketField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		MuiGuestStructCursor cursor;
		switch (kind)
		{
			case MuiExternalBoopsiPacketKind.OpSet:
				if (!MuiGuestStructCursor.TryCreate(ref platform, packet,
					MuiExternalBoopsiOpSetMessage.Size, out cursor)) return false;
				return TryTakeOpSetField(ref platform, ref cursor, field,
					out address);
			case MuiExternalBoopsiPacketKind.OpGet:
				if (!MuiGuestStructCursor.TryCreate(ref platform, packet,
					MuiExternalBoopsiOpGetMessage.Size, out cursor)) return false;
				return TryTakeOpGetField(ref platform, ref cursor, field,
					out address);
			case MuiExternalBoopsiPacketKind.Render:
				if (!MuiGuestStructCursor.TryCreate(ref platform, packet,
					MuiExternalBoopsiRenderMessage.Size, out cursor)) return false;
				return TryTakeRenderField(ref platform, ref cursor, field,
					out address);
			case MuiExternalBoopsiPacketKind.Tag:
				if (!MuiGuestStructCursor.TryCreate(ref platform, packet,
					MuiExternalBoopsiTagItem.Size, out cursor)) return false;
				return TryTakeTagField(ref platform, ref cursor, field,
					out address);
			case MuiExternalBoopsiPacketKind.Result:
				if (field != MuiExternalBoopsiPacketField.Value ||
					!MuiGuestStructCursor.TryCreate(ref platform, packet,
						MuiExternalBoopsiResultWord.Size, out cursor)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
					out address);
			default:
				return false;
		}
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR packet, MuiExternalBoopsiPacketKind kind,
		MuiExternalBoopsiPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, packet, kind, field, out var address))
			return false;
		return MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR packet, MuiExternalBoopsiPacketKind kind,
		MuiExternalBoopsiPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, packet, kind, field, out var address))
			return false;
		MuiGuestUlongStorageCodec.WriteValue(ref platform, address, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// Boopsi packet cursor. New code passes the packet address, kind, and named
// field directly to the struct-backed memory adapter above.
internal static class MuiExternalBoopsiPacketFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalBoopsiPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalBoopsiPacketFieldMemoryCodec.TryGetAddress(ref platform,
			cursor.Packet, cursor.Kind, cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		MuiExternalBoopsiPacketFieldCursor cursor, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalBoopsiPacketFieldMemoryCodec.TryRead(ref platform,
			cursor.Packet, cursor.Kind, cursor.Field, out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		MuiExternalBoopsiPacketFieldCursor cursor, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalBoopsiPacketFieldMemoryCodec.TryWrite(ref platform,
			cursor.Packet, cursor.Kind, cursor.Field, value);
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalTagListCursor
{
	internal const uint EntrySize = MuiAslTagItemRecord.Size;
	internal const uint MaximumEntries = MuiExternalWrapperLayout.MaxTagWalk;
	internal APTR Base;
	internal uint Index;
}

// Struct-first guest-memory adapter for caller-owned TagItem vectors. The
// complete tag record and bounded walk are admitted at one boundary; the
// cursor below remains only as a compatibility wrapper for existing callers.
internal static class MuiExternalTagListVectorMemoryCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (vector.IsNull || index >= MuiExternalTagListCursor.MaximumEntries ||
			index > (uint.MaxValue - vector.Raw) /
			MuiAslTagItemRecord.Size) return false;
		var offset = index * MuiAslTagItemRecord.Size;
		if (vector.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(vector.Raw + offset);
		return platform.IsMapped(address, MuiAslTagItemRecord.Size);
	}
}

// Production bridge for the caller-owned creation/update TagItem vector. The
// vector-specific bound stays in the adapter while callers exchange the
// complete named TagItem record rather than a slot address.
internal static class MuiExternalTagListVectorCodec
{
	internal static bool TryAdvance(ref MuiExternalTagListCursor cursor,
		uint items)
	{
		if (items == 0 || cursor.Index > uint.MaxValue - items)
			return false;
		var next = cursor.Index + items;
		if (next > MuiExternalTagListCursor.MaximumEntries) return false;
		cursor.Index = next;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		MuiExternalTagListCursor cursor, out MuiAslTagItemRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiExternalTagListCursorCodec.TryGetEntry(ref platform, cursor,
			out var address) || !MuiAslTagItemCodec.TryRead(ref platform, address,
			out value))
		{
			value = default;
			return false;
		}
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		MuiExternalTagListCursor cursor, MuiAslTagItemRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiExternalTagListCursorCodec.TryGetEntry(ref platform, cursor,
			out var address)) return false;
		return MuiAslTagItemCodec.Write(ref platform, address, value);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR vector,
		uint index, out MuiAslTagItemRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiExternalTagListVectorMemoryCodec.TryGetEntry(ref platform, vector,
			index, out var address) || !MuiAslTagItemCodec.TryRead(ref platform,
			address, out value))
		{
			value = default;
			return false;
		}
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, MuiAslTagItemRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiExternalTagListVectorMemoryCodec.TryGetEntry(ref platform, vector,
			index, out var address)) return false;
		return MuiAslTagItemCodec.Write(ref platform, address, value);
	}
}

internal static class MuiExternalTagListCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiExternalTagListCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalTagListVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index, out address);
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalRememberCursor
{
	internal const uint EntrySize = MuiAslTagItemRecord.Size;
	internal const uint MaximumEntries = MuiExternalWrapperLayout.MaxRemember;
	internal APTR Base;
	internal uint Index;
}

// Struct-first adapter for the five-entry remember buffer. Keeping this
// separate from the general TagItem walk makes the MorphOS limit explicit.
internal static class MuiExternalRememberVectorMemoryCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (vector.IsNull || index >= MuiExternalRememberCursor.MaximumEntries ||
			index > (uint.MaxValue - vector.Raw) /
			MuiAslTagItemRecord.Size) return false;
		var offset = index * MuiAslTagItemRecord.Size;
		if (vector.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(vector.Raw + offset);
		return platform.IsMapped(address, MuiAslTagItemRecord.Size);
	}
}

// Production bridge for the fixed five-entry remember buffer. The smaller
// MorphOS bound remains owned by this adapter; callers receive named TagItems.
internal static class MuiExternalRememberVectorCodec
{
	internal static bool TryAdvance(ref MuiExternalRememberCursor cursor,
		uint items)
	{
		if (items == 0 || cursor.Index > uint.MaxValue - items)
			return false;
		var next = cursor.Index + items;
		if (next > MuiExternalRememberCursor.MaximumEntries) return false;
		cursor.Index = next;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		MuiExternalRememberCursor cursor, out MuiAslTagItemRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiExternalRememberCursorCodec.TryGetEntry(ref platform, cursor,
			out var address) || !MuiAslTagItemCodec.TryRead(ref platform, address,
			out value))
		{
			value = default;
			return false;
		}
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		MuiExternalRememberCursor cursor, MuiAslTagItemRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiExternalRememberCursorCodec.TryGetEntry(ref platform, cursor,
			out var address)) return false;
		return MuiAslTagItemCodec.Write(ref platform, address, value);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR vector,
		uint index, out MuiAslTagItemRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiExternalRememberVectorMemoryCodec.TryGetEntry(ref platform, vector,
			index, out var address) || !MuiAslTagItemCodec.TryRead(ref platform,
			address, out value))
		{
			value = default;
			return false;
		}
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, MuiAslTagItemRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiExternalRememberVectorMemoryCodec.TryGetEntry(ref platform, vector,
			index, out var address)) return false;
		return MuiAslTagItemCodec.Write(ref platform, address, value);
	}
}

internal static class MuiExternalRememberCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiExternalRememberCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalRememberVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index, out address);
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalBoopsiTagCursor
{
	internal const uint EntrySize = MuiExternalBoopsiTagItem.Size;
	internal const uint MaximumEntries = MuiExternalBoopsiPacketCodec.InlineTagBytes /
		MuiExternalBoopsiTagItem.Size;
	internal APTR Base;
	internal uint Index;
}

// Struct-first adapter for the inline BOOPSI TagItem vector. The packet's
// fixed inline capacity is the public bound; callers receive only complete
// named records that are mapped in guest memory.
internal static class MuiExternalBoopsiTagVectorMemoryCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (vector.IsNull || index >= MuiExternalBoopsiTagCursor.MaximumEntries ||
			index > (uint.MaxValue - vector.Raw) /
			MuiExternalBoopsiTagItem.Size) return false;
		var offset = index * MuiExternalBoopsiTagItem.Size;
		if (vector.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(vector.Raw + offset);
		return platform.IsMapped(address, MuiExternalBoopsiTagItem.Size);
	}
}

// Production bridge for the fixed inline BOOPSI TagItem area. The inline
// capacity remains owned by the vector adapter; callers exchange complete
// named BOOPSI TagItem records without exposing slot addresses.
internal static class MuiExternalBoopsiTagVectorCodec
{
	internal static bool TryAdvance(ref MuiExternalBoopsiTagCursor cursor,
		uint items)
	{
		if (items == 0 || cursor.Index > uint.MaxValue - items)
			return false;
		var next = cursor.Index + items;
		if (next > MuiExternalBoopsiTagCursor.MaximumEntries) return false;
		cursor.Index = next;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		MuiExternalBoopsiTagCursor cursor, out MuiExternalBoopsiTagItem value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiExternalBoopsiTagCursorCodec.TryGetEntry(ref platform, cursor,
			out var address) || !MuiExternalBoopsiTagItemStructCodec.TryRead(
				ref platform, address, out value))
		{
			value = default;
			return false;
		}
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		MuiExternalBoopsiTagCursor cursor, MuiExternalBoopsiTagItem value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiExternalBoopsiTagCursorCodec.TryGetEntry(ref platform, cursor,
			out var address)) return false;
		return MuiExternalBoopsiTagItemStructCodec.Write(ref platform, address,
			value);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR vector,
		uint index, out MuiExternalBoopsiTagItem value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiExternalBoopsiTagVectorMemoryCodec.TryGetEntry(ref platform,
			vector, index, out var address) ||
			!MuiExternalBoopsiTagItemStructCodec.TryRead(ref platform, address,
				out value))
		{
			value = default;
			return false;
		}
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, MuiExternalBoopsiTagItem value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiExternalBoopsiTagVectorMemoryCodec.TryGetEntry(ref platform,
			vector, index, out var address)) return false;
		return MuiExternalBoopsiTagItemStructCodec.Write(ref platform, address,
			value);
	}
}

internal static class MuiExternalBoopsiTagCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiExternalBoopsiTagCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalBoopsiTagVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index, out address);
}

// The Boopsi work block carries its operation packet at the base and a
// caller-facing inline TagItem/result area at one fixed semantic region. Keep
// that region boundary in a named cursor so packet code does not repeat raw
// work-buffer arithmetic.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalBoopsiWorkBuffer
{
	internal const uint Size = MuiExternalWrapperLayout.WorkSize;
	internal const uint HeaderSize = 16;
	// Compatibility aliases retained for callers that display the stable ABI
	// boundary; address resolution below advances a named cursor instead.
	internal const uint InlineTagListOffset = HeaderSize;
	internal const uint InlineResultOffset = InlineTagListOffset;
	internal const uint InlineRegionSize = 40;
}

internal enum MuiExternalWorkRegion : byte
{
	InlineTagList,
	InlineResult,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalWorkRegionCursor
{
	internal APTR Work;
	internal MuiExternalWorkRegion Region;
}

internal static class MuiExternalWorkRegionMemoryCodec
{
	internal const uint InlineOffset = MuiExternalBoopsiWorkBuffer.InlineTagListOffset;
	internal const uint InlineBytes = MuiExternalBoopsiWorkBuffer.InlineRegionSize;

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR work, MuiExternalWorkRegion region, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if ((region != MuiExternalWorkRegion.InlineTagList &&
			region != MuiExternalWorkRegion.InlineResult) ||
			!MuiGuestStructCursor.TryCreate(ref platform, work,
				MuiExternalBoopsiWorkBuffer.Size, out var cursor)) return false;
		// The first four ULONGs are the operation packet header/reserved area;
		// both semantic inline regions begin immediately after that named span.
		if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4, out _) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4, out _) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4, out _) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4, out _))
			return false;
		return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiExternalBoopsiWorkBuffer.InlineRegionSize, out address);
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// work-region cursor. New code passes the work-buffer address and named region
// directly to the struct-backed memory adapter above.
internal static class MuiExternalWorkRegionCursorCodec
{
	internal const uint InlineOffset = MuiExternalWorkRegionMemoryCodec.InlineOffset;
	internal const uint InlineBytes = MuiExternalWorkRegionMemoryCodec.InlineBytes;

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalWorkRegionCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalWorkRegionMemoryCodec.TryGetAddress(ref platform,
			cursor.Work, cursor.Region, out address);
}

// Fixed BOOPSI operation frames used by the external wrapper. WorkBuffer's
// inline tag area is an ABI boundary; all operation and TagItem field access
// stays here so the live wrapper logic consumes named records instead of
// repeating packed offsets.
internal static class MuiExternalBoopsiPacketCodec
{
	internal const uint OmSet = 0x00000103u;
	internal const uint OmGet = 0x00000104u;
	internal const uint GmRender = 0x00000001u;
	internal const uint InlineOffset = MuiExternalBoopsiWorkBuffer.InlineTagListOffset;
	internal const uint InlineTagBytes = MuiExternalBoopsiWorkBuffer.InlineRegionSize;

	internal static bool TryGetInlineTagList<TPlatform>(
		ref TPlatform platform, APTR work, out APTR list)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalWorkRegionMemoryCodec.TryGetAddress(ref platform, work,
			MuiExternalWorkRegion.InlineTagList, out list);

	internal static bool TryGetInlineResult<TPlatform>(
		ref TPlatform platform, APTR work, out APTR storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		storage = APTR.Null;
		if (!TryGetInlineTagList(ref platform, work, out var list)) return false;
		storage = list;
		return platform.IsMapped(storage, MuiExternalBoopsiResultWord.Size);
	}

	internal static bool WriteOpSet<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalBoopsiOpSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (packet.MethodId != OmSet) return false;
		return MuiExternalBoopsiOpSetMessageStructCodec.Write(ref platform,
			address, packet);
	}

	internal static bool WriteOpGet<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalBoopsiOpGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return WriteOpGetValues(ref platform, address, packet.MethodId,
			packet.Attribute, packet.Storage.Raw);
	}

	internal static bool WriteOpGetValues<TPlatform>(ref TPlatform platform,
		APTR address, uint methodId, uint attribute, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (methodId != OmGet) return false;
		var packet = default(MuiExternalBoopsiOpGetMessage);
		packet.MethodId = methodId;
		packet.Attribute = attribute;
		packet.Storage = APTR.FromPointer(storage);
		return MuiExternalBoopsiOpGetMessageStructCodec.Write(ref platform,
			address, packet);
	}

	internal static bool WriteRender<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalBoopsiRenderMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return WriteRenderValues(ref platform, address, packet.MethodId,
			packet.GadgetInfo.Raw, packet.RastPort.Raw);
	}

	internal static bool WriteRenderValues<TPlatform>(ref TPlatform platform,
		APTR address, uint methodId, uint gadgetInfo, uint rastPort)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (methodId != GmRender) return false;
		var packet = default(MuiExternalBoopsiRenderMessage);
		packet.MethodId = methodId;
		packet.GadgetInfo = APTR.FromPointer(gadgetInfo);
		packet.RastPort = APTR.FromPointer(rastPort);
		return MuiExternalBoopsiRenderMessageStructCodec.Write(ref platform,
			address, packet);
	}

	internal static bool WriteTag<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalBoopsiTagItem packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalBoopsiTagItemStructCodec.Write(ref platform, address,
			packet);

	internal static bool WriteResult<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalBoopsiResultWord packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalBoopsiResultWordStructCodec.Write(ref platform, address,
			packet);

	// Keep a scalar ABI seam for the freestanding compiler's one-word record
	// argument path. The public/host-facing surface remains the named result
	// record above; native roots can use this helper without passing a tiny
	// struct by value across the guest call boundary.
	internal static bool WriteResultValue<TPlatform>(ref TPlatform platform,
		APTR address, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteResultValueCore(ref platform, address, value);

	private static bool WriteResultValueCore<TPlatform>(ref TPlatform platform,
		APTR address, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiExternalBoopsiResultWord);
		packet.Value = value;
		return MuiExternalBoopsiResultWordStructCodec.Write(ref platform,
			address, packet);
	}

	internal static bool TryReadResult<TPlatform>(ref TPlatform platform,
		APTR address, out MuiExternalBoopsiResultWord packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalBoopsiResultWordStructCodec.TryRead(ref platform, address,
			out packet);

	internal static bool TryReadResultValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiExternalBoopsiResultWordStructCodec.TryRead(ref platform,
			address, out var packet)) return false;
		value = packet.Value;
		return true;
	}
}

public static class MuiExternalWrapperCore
{
	// ---- Classification ------------------------------------------------------

	// Classify a guest C-string class id against the exact official names. The
	// loader contract is case-sensitive, so the match is byte-exact against the
	// documented "Boopsi.mui" / "Dtpic.mui" ids. Fixed identities are admitted
	// through named packed records; no managed strings or byte walks are needed.
	public static MuiExternalWrapperClass ClassifyName<TPlatform>(
		ref TPlatform platform, APTR classId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (MuiExternalWrapperBoopsiClassNameRecordCodec.TryMatch(ref platform,
			classId))
			return MuiExternalWrapperClass.Boopsi;
		if (MuiExternalWrapperDtpicClassNameRecordCodec.TryMatch(ref platform,
			classId))
			return MuiExternalWrapperClass.Dtpic;
		return MuiExternalWrapperClass.None;
	}

	// Both classes descend directly from Area; neither is private.
	public static MuiExternalWrapperClass Superclass(MuiExternalWrapperClass cls) =>
		MuiExternalWrapperClass.None;   // : Area (Area itself is not in this family)

	public static MuiExternalWrapperClass Classify<TPlatform>(
		ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiExternalWrapperHeaderCodec.TryRead(ref platform, instance,
			out var header) ? header.Class : MuiExternalWrapperClass.None;

	public static bool Valid<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiGuestMemory =>
		instance.IsNotNull &&
		platform.IsMapped(instance, MuiExternalWrapperLayout.InstanceSize) &&
		MuiExternalWrapperHeaderCodec.TryRead(ref platform, instance,
			out _);

	// ---- Creation (failure-atomic) -------------------------------------------

	public static MuiExternalWrapperClass CreateByName<TPlatform>(
		ref TPlatform platform, APTR instance, APTR classId)
		where TPlatform : struct, IMuiServicePlatform
	{
		var cls = ClassifyName(ref platform, classId);
		if (cls == MuiExternalWrapperClass.None) return MuiExternalWrapperClass.None;
		return Create(ref platform, instance, cls) ? cls
			: MuiExternalWrapperClass.None;
	}

	// Create a wrapper instance of an explicit class. Every instance owns a
	// 64-byte message scratch used to marshal OM_SET/OM_GET/geometry packets to
	// the wrapped object; a Boopsi instance additionally owns a 40-byte remember
	// buffer. Both owned blocks are allocated atomically: if any allocation
	// fails, everything touched is freed and the call returns false with the
	// instance left clear. Defaults follow the MUI_Boopsi autodoc (1x1 minimum,
	// "unlimited" maximum) and a fully-opaque Dtpic alpha.
	public static bool Create<TPlatform>(ref TPlatform platform, APTR instance,
		MuiExternalWrapperClass cls) where TPlatform : struct, IMuiServicePlatform
	{
		if (instance.IsNull ||
			!platform.IsMapped(instance, MuiExternalWrapperLayout.InstanceSize) ||
			cls == MuiExternalWrapperClass.None) return false;

		var work = Alloc(ref platform, MuiExternalWrapperLayout.WorkSize);
		if (work.IsNull) return false;

		APTR remember = APTR.Null;
		if (cls == MuiExternalWrapperClass.Boopsi)
		{
			remember = Alloc(ref platform, MuiExternalWrapperLayout.RememberSize);
			if (remember.IsNull)
			{
				Free(ref platform, work, MuiExternalWrapperLayout.WorkSize);
				return false;
			}
		}

		platform.Clear(instance, MuiExternalWrapperLayout.InstanceSize);
		var header = default(MuiExternalWrapperHeader);
		header.Magic = MuiExternalWrapperHeader.Cookie;
		header.Class = cls;
		if (!MuiExternalWrapperHeaderCodec.Write(ref platform, instance, header))
		{
			Free(ref platform, work, MuiExternalWrapperLayout.WorkSize);
			if (remember.IsNotNull)
				Free(ref platform, remember, MuiExternalWrapperLayout.RememberSize);
			return false;
		}
		var scratch = default(MuiExternalScratchState);
		scratch.WorkBuffer = work;

		if (cls == MuiExternalWrapperClass.Boopsi)
		{
			scratch.RememberBuffer = remember;
			var geometry = default(MuiExternalBoopsiGeometryState);
			geometry.MinWidth = 1;
			geometry.MinHeight = 1;
			geometry.MaxWidth = MuiExternalWrapperLayout.MaxDefault;
			geometry.MaxHeight = MuiExternalWrapperLayout.MaxDefault;
			if (!MuiExternalBoopsiGeometryCodec.Write(ref platform, instance,
				geometry))
			{
				Free(ref platform, remember, MuiExternalWrapperLayout.RememberSize);
				Free(ref platform, work, MuiExternalWrapperLayout.WorkSize);
				return false;
			}
		}
		if (!MuiExternalScratchStateCodec.Write(ref platform, instance, scratch))
		{
			if (remember.IsNotNull)
				Free(ref platform, remember, MuiExternalWrapperLayout.RememberSize);
			Free(ref platform, work, MuiExternalWrapperLayout.WorkSize);
			return false;
		}
		if (cls != MuiExternalWrapperClass.Boopsi)
		{
			var dtpic = default(MuiExternalDtpicState);
			dtpic.Alpha = 255;
			if (!MuiExternalDtpicStateCodec.Write(ref platform, instance, dtpic))
			{
				Free(ref platform, work, MuiExternalWrapperLayout.WorkSize);
				return false;
			}
		}
		return true;
	}

	// Provide the caller-owned BOOPSI creation tag list (the mixed MUI/BOOPSI
	// tags supplied to BoopsiObject). It is never copied or freed; MUI fills the
	// TagWindow/TagScreen/TagDrawInfo entries in place at object-creation time.
	public static bool SetCreationTags<TPlatform>(ref TPlatform platform,
		APTR instance, APTR tags) where TPlatform : struct, IMuiGuestMemory
	{
		if (Classify(ref platform, instance) != MuiExternalWrapperClass.Boopsi)
			return false;
		if (!MuiExternalBoopsiResourceCodec.TryRead(ref platform, instance,
			out var resources)) return false;
		resources.CreationTags = tags;
		if (!MuiExternalBoopsiResourceCodec.Write(ref platform, instance,
			resources)) return false;
		return true;
	}

	// ---- Setup / Show / Hide / Cleanup ---------------------------------------

	// MUIM_Setup. The display environment becomes available here (the window is
	// open), so this is where a Boopsi object is finally created (MUI delays
	// BOOPSI creation until setup) and where a Dtpic picture is acquired. The
	// live Screen/Window/DrawInfo/RastPort are read from the RenderInfo record.
	// A Boopsi object-creation failure is reported failure-atomically: no
	// display state is retained and setup returns false. A Dtpic picture that
	// cannot be acquired leaves the object valid but empty (setup still
	// succeeds); the acquire itself is atomic.
	public static bool Setup<TPlatform>(ref TPlatform platform, APTR instance,
		APTR renderInfo) where TPlatform : struct, IMuiServicePlatform
	{
		if (!Valid(ref platform, instance)) return false;
		if (MuiExternalRenderInfoCodec.TryRead(ref platform, renderInfo,
			out var render))
		{
			var display = default(MuiExternalDisplayState);
			display.Screen = render.Screen;
			display.Window = render.Window;
			display.DrawInfo = render.DrawInfo;
			display.RastPort = render.RastPort;
			if (!MuiExternalDisplayStateCodec.Write(ref platform, instance,
				display)) return false;
		}

		if (!MuiExternalWrapperHeaderCodec.TryRead(ref platform, instance,
			out var header)) return false;
		var cls = header.Class;
		if (cls == MuiExternalWrapperClass.Boopsi)
		{
			if (!CreateBoopsiObject(ref platform, instance))
			{
				// Failure-atomic: forget the display environment we just recorded.
				ClearDisplay(ref platform, instance);
				return false;
			}
		}
		else
		{
			AcquirePicture(ref platform, instance);   // atomic; empty on failure
		}
		SetFlag(ref platform, instance, MuiExternalWrapperLayout.FlagSetup, true);
		return true;
	}

	// MUIM_Show marks the object visible. The wrapped boopsi object pointer
	// becomes meaningful to callers only between Setup and Cleanup, matching the
	// documented MUIA_Boopsi_Object validity window.
	public static bool Show<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!Valid(ref platform, instance)) return false;
		SetFlag(ref platform, instance, MuiExternalWrapperLayout.FlagShown, true);
		SetFlag(ref platform, instance, MuiExternalWrapperLayout.FlagRedraw, true);
		return true;
	}

	// MUIM_Hide marks the object hidden. The wrapped resource stays alive across
	// hide/show; it is torn down at cleanup (window close) or regenerated
	// explicitly on a resize (Regenerate).
	public static bool Hide<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!Valid(ref platform, instance)) return false;
		SetFlag(ref platform, instance, MuiExternalWrapperLayout.FlagShown, false);
		SetFlag(ref platform, instance, MuiExternalWrapperLayout.FlagRedraw, false);
		return true;
	}

	// MUIM_Cleanup tears down the external resource. A Boopsi object is disposed
	// and, if the wrapper opened the class itself, the class library is closed
	// exactly once; a Dtpic picture is released exactly once. The display
	// environment is forgotten. Idempotent.
	public static bool Cleanup<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Valid(ref platform, instance)) return false;
		if (!MuiExternalWrapperHeaderCodec.TryRead(ref platform, instance,
			out var header)) return false;
		var cls = header.Class;
		if (cls == MuiExternalWrapperClass.Boopsi)
			DisposeBoopsiObject(ref platform, instance, true);
		else
			ReleasePicture(ref platform, instance);
		ClearDisplay(ref platform, instance);
		SetFlag(ref platform, instance, MuiExternalWrapperLayout.FlagSetup, false);
		SetFlag(ref platform, instance, MuiExternalWrapperLayout.FlagShown, false);
		return true;
	}

	private static void ClearDisplay<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory
	{
		MuiExternalDisplayStateCodec.Write(ref platform, instance,
			default(MuiExternalDisplayState));
	}

	// ---- Boopsi object lifetime ----------------------------------------------

	// Create the wrapped boopsi object. The class is either a caller-owned
	// private class (MUIA_Boopsi_Class) or, when that is Null, resolved by
	// opening the public class named by MUIA_Boopsi_ClassID through the narrow
	// external loader seam. Before creation the live Window/Screen/DrawInfo
	// pointers are patched into the caller's creation tag list at the tag ids
	// named by TagWindow/TagScreen/TagDrawInfo. Failure is atomic: a class the
	// wrapper opened is closed again and no object is retained.
	private static bool CreateBoopsiObject<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiServicePlatform
	{
		if ((ReadFlags(ref platform, instance) &
			MuiExternalWrapperLayout.FlagObjectCreated) != 0) return true;
		if (!MuiExternalBoopsiResourceCodec.TryRead(ref platform, instance,
			out var resources)) return false;

		var classPtr = resources.PrivateClass;
		var openedHere = false;
		if (classPtr.IsNull)
		{
			// A class the wrapper opened earlier stays open across a regenerate;
			// reuse it rather than opening (and leaking) a second handle.
			var already = resources.OpenedClass;
			if (already.IsNotNull)
			{
				classPtr = already;
			}
			else
			{
				var classId = resources.ClassId;
				if (classId.IsNull) return false;
				classPtr = platform.OpenExternalClass(classId);
				if (classPtr.IsNull) return false;
				resources.OpenedClass = classPtr;
				if (!MuiExternalBoopsiResourceCodec.Write(ref platform, instance,
					resources))
				{
					platform.CloseExternalClass(classPtr);
					return false;
				}
				openedHere = true;
			}
		}

		FillCreationTags(ref platform, instance);

		var tags = resources.CreationTags;
		var obj = platform.NewObject(classPtr, tags);
		if (obj.IsNull)
		{
			if (openedHere)
			{
				platform.CloseExternalClass(classPtr);
				resources.OpenedClass = APTR.Null;
				MuiExternalBoopsiResourceCodec.Write(ref platform, instance,
					resources);
			}
			return false;
		}
		resources.BoopsiObject = obj;
		if (!MuiExternalBoopsiResourceCodec.Write(ref platform, instance,
			resources))
		{
			platform.DisposeObject(obj);
			if (openedHere)
			{
				platform.CloseExternalClass(classPtr);
				resources.OpenedClass = APTR.Null;
			}
			resources.BoopsiObject = APTR.Null;
			MuiExternalBoopsiResourceCodec.Write(ref platform, instance,
				resources);
			return false;
		}
		SetFlag(ref platform, instance,
			MuiExternalWrapperLayout.FlagObjectCreated, true);

		// Re-apply any values remembered across a previous dispose/regenerate.
		ReapplyRemembered(ref platform, instance);
		return true;
	}

	// Dispose the wrapped boopsi object and, when requested and the class was
	// opened by the wrapper, close that class exactly once. Guarded pointers make
	// a repeated call a safe no-op, guaranteeing exactly-once dispose/close.
	private static void DisposeBoopsiObject<TPlatform>(ref TPlatform platform,
		APTR instance, bool closeClass)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiExternalBoopsiResourceCodec.TryRead(ref platform, instance,
			out var resources)) return;
		var obj = resources.BoopsiObject;
		if (obj.IsNotNull)
		{
			platform.DisposeObject(obj);
			resources.BoopsiObject = APTR.Null;
		}
		MuiExternalBoopsiResourceCodec.Write(ref platform, instance, resources);
		SetFlag(ref platform, instance,
			MuiExternalWrapperLayout.FlagObjectCreated, false);

		if (!closeClass) return;
		var opened = resources.OpenedClass;
		if (opened.IsNotNull)
		{
			platform.CloseExternalClass(opened);
			resources.OpenedClass = APTR.Null;
			MuiExternalBoopsiResourceCodec.Write(ref platform, instance, resources);
		}
	}

	// Patch the live display pointers into the creation tag list. For each of
	// TagWindow/TagScreen/TagDrawInfo that names a non-zero tag id, the matching
	// ti_Tag entry in the caller's list gets its ti_Data set to the live
	// pointer. The list is caller-owned and only these entries are touched.
	private static void FillCreationTags<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiExternalBoopsiResourceCodec.TryRead(ref platform, instance,
			out var resources)) return;
		var tags = resources.CreationTags;
		if (tags.IsNull) return;
		if (!MuiExternalBoopsiGeometryCodec.TryRead(ref platform, instance,
			out var geometry)) return;
		if (!MuiExternalDisplayStateCodec.TryRead(ref platform, instance,
			out var display)) return;
		var tagWindow = geometry.TagWindow;
		var tagScreen = geometry.TagScreen;
		var tagDrawInfo = geometry.TagDrawInfo;
		var window = display.Window.Raw;
		var screen = display.Screen.Raw;
		var drawInfo = display.DrawInfo.Raw;
		var tagCursor = default(MuiExternalTagListCursor);
		tagCursor.Base = tags;
		tagCursor.Index = 0;
		for (var index = 0u; index < MuiExternalTagListCursor.MaximumEntries;
			index++)
		{
			if (!MuiExternalTagListVectorCodec.TryRead(ref platform, tagCursor,
				out var item)) break;
			if (item.Tag == MuiAslTagListCore.TagDone) break;
			var value = item.Data;
			if (tagWindow != 0 && item.Tag == tagWindow) value = window;
			else if (tagScreen != 0 && item.Tag == tagScreen) value = screen;
			else if (tagDrawInfo != 0 && item.Tag == tagDrawInfo) value = drawInfo;
			if (value != item.Data)
			{
				item.Data = value;
				if (!MuiExternalTagListVectorCodec.TryWrite(ref platform, tagCursor,
					item)) break;
			}
			if (index + 1 < MuiExternalTagListCursor.MaximumEntries &&
				!MuiExternalTagListVectorCodec.TryAdvance(ref tagCursor, 1)) break;
		}
	}

	// MUIA_Boopsi_Remember: append a tag id to remember (up to five). Silently
	// ignores a sixth request, matching the documented five-tag limit.
	private static bool AddRemember<TPlatform>(ref TPlatform platform,
		APTR instance, uint tag) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiExternalScratchStateCodec.TryRead(ref platform, instance,
			out var scratch)) return false;
		var count = scratch.RememberCount;
		if (count >= MuiExternalWrapperLayout.MaxRemember) return false;
		var buffer = scratch.RememberBuffer;
		if (buffer.IsNull) return false;
		var item = default(MuiAslTagItemRecord);
		item.Tag = tag;
		var cursor = default(MuiExternalRememberCursor);
		cursor.Base = buffer;
		cursor.Index = count;
		if (!MuiExternalRememberVectorCodec.TryWrite(ref platform, cursor,
			item)) return false;
		scratch.RememberCount = count + 1;
		if (!MuiExternalScratchStateCodec.Write(ref platform, instance, scratch))
			return false;
		return true;
	}

	// Read every remembered tag from the live boopsi object into the remember
	// buffer, immediately before disposing it during a regenerate.
	private static void SaveRemembered<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiExternalBoopsiResourceCodec.TryRead(ref platform, instance,
			out var resources)) return;
		var obj = resources.BoopsiObject;
		if (obj.IsNull) return;
		if (!MuiExternalScratchStateCodec.TryRead(ref platform, instance,
			out var scratch)) return;
		var buffer = scratch.RememberBuffer;
		if (buffer.IsNull) return;
		var count = scratch.RememberCount;
		if (count > MuiExternalWrapperLayout.MaxRemember)
			count = MuiExternalWrapperLayout.MaxRemember;
		var cursor = default(MuiExternalRememberCursor);
		cursor.Base = buffer;
		cursor.Index = 0;
		for (var index = 0u; index < count; index++)
		{
			if (!MuiExternalRememberVectorCodec.TryRead(ref platform, cursor,
				out var item)) return;
			item.Data = BoopsiGet(ref platform, instance, obj, item.Tag);
			if (!MuiExternalRememberVectorCodec.TryWrite(ref platform, cursor,
				item)) return;
			if (index + 1 < count &&
				!MuiExternalRememberVectorCodec.TryAdvance(ref cursor, 1)) return;
		}
	}

	// Set every remembered (tag,value) pair back onto a freshly created object.
	private static void ReapplyRemembered<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiExternalBoopsiResourceCodec.TryRead(ref platform, instance,
			out var resources)) return;
		var obj = resources.BoopsiObject;
		if (obj.IsNull) return;
		if (!MuiExternalScratchStateCodec.TryRead(ref platform, instance,
			out var scratch)) return;
		var buffer = scratch.RememberBuffer;
		if (buffer.IsNull) return;
		var count = scratch.RememberCount;
		if (count > MuiExternalWrapperLayout.MaxRemember)
			count = MuiExternalWrapperLayout.MaxRemember;
		var cursor = default(MuiExternalRememberCursor);
		cursor.Base = buffer;
		cursor.Index = 0;
		for (var index = 0u; index < count; index++)
		{
			if (!MuiExternalRememberVectorCodec.TryRead(ref platform, cursor,
				out var item)) return;
			BoopsiSet(ref platform, instance, obj, item.Tag, item.Data);
			if (index + 1 < count &&
				!MuiExternalRememberVectorCodec.TryAdvance(ref cursor, 1)) return;
		}
	}

	// Model a window resize / screen jump. Silly boopsi objects that cannot
	// resize are disposed and regenerated, remembering the tags the caller asked
	// for; the opened class library stays open across the regeneration and is
	// only closed at cleanup/dispose. A smart object (MUIA_Boopsi_Smart) is left
	// untouched. Only valid between Setup and Cleanup. Returns success.
	public static bool Regenerate<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiServicePlatform
	{
		if (Classify(ref platform, instance) != MuiExternalWrapperClass.Boopsi)
			return false;
		var flags = ReadFlags(ref platform, instance);
		if ((flags & MuiExternalWrapperLayout.FlagSetup) == 0) return false;
		if ((flags & MuiExternalWrapperLayout.FlagSmart) != 0) return true;

		SaveRemembered(ref platform, instance);
		DisposeBoopsiObject(ref platform, instance, false);   // keep class open
		return CreateBoopsiObject(ref platform, instance);
	}

	// ---- Dtpic picture lifetime ----------------------------------------------

	// Acquire (and lay out) the datatypes picture for the current owned name.
	// Atomic: a failed acquire leaves no picture and no laid-out dimensions.
	private static bool AcquirePicture<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiServicePlatform
	{
		if ((ReadFlags(ref platform, instance) &
			MuiExternalWrapperLayout.FlagPicture) != 0) return true;
		if (!MuiExternalDtpicStateCodec.TryRead(ref platform, instance,
			out var dtpic)) return false;
		var name = dtpic.OwnedName;
		if (name.IsNull) return false;   // no name -> valid but empty Dtpic
		if (!MuiExternalDisplayStateCodec.TryRead(ref platform, instance,
			out var display)) return false;
		if (!MuiExternalScratchStateCodec.TryRead(ref platform, instance,
			out var scratch)) return false;
		var screen = display.Screen;
		var picture = platform.AcquirePicture(name, screen);
		if (picture.IsNull) return false;
		dtpic.PictureObject = picture;

		var rastPort = display.RastPort;
		var work = scratch.WorkBuffer;
		if (work.IsNotNull &&
			platform.LayoutPicture(picture, rastPort, work))
		{
			if (MuiExternalDtpicLayoutResultCodec.TryRead(ref platform, work,
				out var dimensions))
			{
				dtpic.PicWidth = dimensions.Width;
				dtpic.PicHeight = dimensions.Height;
			}
		}
		if (!MuiExternalDtpicStateCodec.Write(ref platform, instance, dtpic))
		{
			platform.ReleasePicture(picture);
			return false;
		}
		SetFlag(ref platform, instance, MuiExternalWrapperLayout.FlagPicture, true);
		return true;
	}

	// Release the datatypes picture exactly once. Guarded so a repeat is a
	// no-op. Laid-out dimensions are forgotten.
	private static void ReleasePicture<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiExternalDtpicStateCodec.TryRead(ref platform, instance,
			out var dtpic)) return;
		var picture = dtpic.PictureObject;
		if (picture.IsNotNull)
		{
			platform.ReleasePicture(picture);
			dtpic.PictureObject = APTR.Null;
		}
		SetFlag(ref platform, instance, MuiExternalWrapperLayout.FlagPicture, false);
		dtpic.PicWidth = 0;
		dtpic.PicHeight = 0;
		MuiExternalDtpicStateCodec.Write(ref platform, instance, dtpic);
	}

	// MUIA_Dtpic_Name. Copy the caller-owned name into a class-owned block so
	// the caller may mutate or free its buffer afterwards. A previously owned
	// copy is freed first. When set at runtime while set up, the current picture
	// is released and the replacement one acquired (failure-atomic). A Null name clears
	// the owned copy and releases any picture. Returns whether the copy changed.
	public static bool SetName<TPlatform>(ref TPlatform platform, APTR instance,
		APTR name) where TPlatform : struct, IMuiServicePlatform
	{
		if (Classify(ref platform, instance) != MuiExternalWrapperClass.Dtpic)
			return false;
		FreeOwnedName(ref platform, instance);
		if (!MuiExternalDtpicStateCodec.TryRead(ref platform, instance,
			out var dtpic)) return false;
		dtpic.CallerName = name;
		if (!MuiExternalDtpicStateCodec.Write(ref platform, instance, dtpic))
			return false;

		if (name.IsNotNull)
		{
			var length = CStringLength(ref platform, name);
			var copySize = (uint)length + 1;
			var copy = Alloc(ref platform, copySize);
			if (copy.IsNull) return false;   // atomic: leaves no owned name
			var sourceCursor = default(MuiExternalTextByteCursor);
			sourceCursor.Base = name;
			sourceCursor.Length = copySize;
			var destinationCursor = default(MuiExternalTextByteCursor);
			destinationCursor.Base = copy;
			destinationCursor.Length = copySize;
			for (var index = 0u; index < copySize; index++)
			{
				sourceCursor.Index = index;
				destinationCursor.Index = index;
				if (!MuiExternalTextByteCursorCodec.TryReadByte(ref platform,
					sourceCursor, out var value) ||
					!MuiExternalTextByteCursorCodec.TryWriteByte(ref platform,
						destinationCursor, value))
				{
					Free(ref platform, copy, copySize);
					return false;
				}
			}
			dtpic.OwnedName = copy;
			dtpic.OwnedNameSize = copySize;
		}
		if (!MuiExternalDtpicStateCodec.Write(ref platform, instance, dtpic))
		{
			if (dtpic.OwnedName.IsNotNull)
				Free(ref platform, dtpic.OwnedName, dtpic.OwnedNameSize);
			return false;
		}

		// A runtime name change while set up reloads the picture atomically.
		if ((ReadFlags(ref platform, instance) &
			MuiExternalWrapperLayout.FlagSetup) != 0)
		{
			ReleasePicture(ref platform, instance);
			AcquirePicture(ref platform, instance);
			SetFlag(ref platform, instance, MuiExternalWrapperLayout.FlagRedraw,
				true);
		}
		return true;
	}

	private static void FreeOwnedName<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiExternalDtpicStateCodec.TryRead(ref platform, instance,
			out var dtpic)) return;
		var owned = dtpic.OwnedName;
		if (owned.IsNull) return;
		Free(ref platform, owned, dtpic.OwnedNameSize);
		dtpic.OwnedName = APTR.Null;
		dtpic.OwnedNameSize = 0;
		MuiExternalDtpicStateCodec.Write(ref platform, instance, dtpic);
	}

	private static int CStringLength<TPlatform>(ref TPlatform platform, APTR text)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiExternalTextByteCursor);
		cursor.Base = text;
		cursor.Length = MuiExternalTextByteCursor.MaximumLength;
		var length = 0;
		while (length < MuiExternalWrapperLayout.MaxNameLength)
		{
			cursor.Index = (uint)length;
			if (!MuiExternalTextByteCursorCodec.TryReadByte(ref platform, cursor,
				out var value) || value == 0) break;
			length++;
		}
		return length;
	}

	public static bool IsPictureAcquired<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		Valid(ref platform, instance) &&
		(ReadFlags(ref platform, instance) &
			MuiExternalWrapperLayout.FlagPicture) != 0;

	public static bool IsObjectCreated<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		Valid(ref platform, instance) &&
		(ReadFlags(ref platform, instance) &
			MuiExternalWrapperLayout.FlagObjectCreated) != 0;

	// ---- Layout / geometry / minmax / draw -----------------------------------

	// Bounded MUI_MinMax (six UWORDs). Boopsi publishes the caller-supplied
	// min/max clamped to a UWORD; Dtpic publishes the laid-out picture size (or
	// its explicit Dtpic min) and lets FreeHoriz/FreeVert relax the maximum.
	public static bool AskMinMax<TPlatform>(ref TPlatform platform, APTR instance,
		APTR storage) where TPlatform : struct, IMuiGuestMemory
	{
		if (!Valid(ref platform, instance) || storage.IsNull ||
			!platform.IsMapped(storage, 12)) return false;
		if (!MuiExternalWrapperHeaderCodec.TryRead(ref platform, instance,
			out var header)) return false;
		var cls = header.Class;
		uint minW, minH, maxW, maxH;
		if (cls == MuiExternalWrapperClass.Boopsi)
		{
			if (!MuiExternalBoopsiGeometryCodec.TryRead(ref platform, instance,
				out var geometry)) return false;
			minW = geometry.MinWidth;
			minH = geometry.MinHeight;
			maxW = geometry.MaxWidth;
			maxH = geometry.MaxHeight;
		}
		else
		{
			if (!MuiExternalDtpicStateCodec.TryRead(ref platform, instance,
				out var dtpic)) return false;
			var picW = dtpic.PicWidth;
			var picH = dtpic.PicHeight;
			var dtMinW = dtpic.MinWidth;
			var dtMinH = dtpic.MinHeight;
			minW = dtMinW != 0 ? dtMinW : picW;
			minH = dtMinH != 0 ? dtMinH : picH;
			var flags = ReadFlags(ref platform, instance);
			maxW = (flags & MuiExternalWrapperLayout.FlagFreeHoriz) != 0
				? MuiExternalWrapperLayout.MaxDefault : (minW != 0 ? minW : 1);
			maxH = (flags & MuiExternalWrapperLayout.FlagFreeVert) != 0
				? MuiExternalWrapperLayout.MaxDefault : (minH != 0 ? minH : 1);
		}
		if (minW == 0) minW = 1;
		if (minH == 0) minH = 1;
		if (maxW < minW) maxW = minW;
		if (maxH < minH) maxH = minH;
		var values = default(MuiMinMaxValues);
		values.MinWidth = unchecked((short)Clamp(minW));
		values.MinHeight = unchecked((short)Clamp(minH));
		values.MaxWidth = unchecked((short)Clamp(maxW));
		values.MaxHeight = unchecked((short)Clamp(maxH));
		values.DefWidth = values.MinWidth;
		values.DefHeight = values.MinHeight;
		return MuiAreaLayoutCore.WriteMinMax(ref platform, storage, values);
	}

	private static ushort Clamp(uint value) =>
		value > 0xFFFFu ? (ushort)0xFFFF : (ushort)value;

	// MUIM_Layout for a Boopsi object: push the assigned rectangle onto the
	// wrapped gadget through GA_Left/GA_Top/GA_Width/GA_Height. When the wrapped
	// class is the OS 3.0/3.1 colorwheel.gadget, one is subtracted from the width
	// and height first, the documented MUI workaround for the gadget rendering
	// itself one pixel too big. Only meaningful once the object exists.
	public static bool ApplyGeometry<TPlatform>(ref TPlatform platform,
		APTR instance, int left, int top, int width, int height)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (Classify(ref platform, instance) != MuiExternalWrapperClass.Boopsi)
			return false;
		if (!MuiExternalBoopsiResourceCodec.TryRead(ref platform, instance,
			out var resources)) return false;
		var obj = resources.BoopsiObject;
		if (obj.IsNull) return false;

		var appliedWidth = width;
		var appliedHeight = height;
		if ((ReadFlags(ref platform, instance) &
			MuiExternalWrapperLayout.FlagColorwheel) != 0)
		{
			appliedWidth = width - 1;
			appliedHeight = height - 1;
		}

		if (!MuiExternalScratchStateCodec.TryRead(ref platform, instance,
			out var scratch)) return false;
		var work = scratch.WorkBuffer;
		if (work.IsNull || !platform.IsMapped(work,
			MuiExternalWrapperLayout.WorkSize)) return false;

		if (!MuiExternalBoopsiPacketCodec.TryGetInlineTagList(ref platform,
			work, out var list)) return false;
		var opSet = default(MuiExternalBoopsiOpSetMessage);
		opSet.MethodId = MuiExternalBoopsiPacketCodec.OmSet;
		opSet.AttributeList = list;
		if (!MuiExternalBoopsiPacketCodec.WriteOpSet(ref platform, work, opSet))
			return false;
		var tagCursor = default(MuiExternalBoopsiTagCursor);
		tagCursor.Base = list;
		tagCursor.Index = 0;
		var tag = default(MuiExternalBoopsiTagItem);
		tag.Tag = GaLeft;
		tag.Data = unchecked((uint)left);
		if (!MuiExternalBoopsiTagVectorCodec.TryWrite(ref platform, tagCursor,
			tag))
			return false;
		if (!MuiExternalBoopsiTagVectorCodec.TryAdvance(ref tagCursor, 1))
			return false;
		tag.Tag = GaTop;
		tag.Data = unchecked((uint)top);
		if (!MuiExternalBoopsiTagVectorCodec.TryWrite(ref platform, tagCursor,
			tag)) return false;
		if (!MuiExternalBoopsiTagVectorCodec.TryAdvance(ref tagCursor, 1))
			return false;
		tag.Tag = GaWidth;
		tag.Data = unchecked((uint)appliedWidth);
		if (!MuiExternalBoopsiTagVectorCodec.TryWrite(ref platform, tagCursor,
			tag)) return false;
		if (!MuiExternalBoopsiTagVectorCodec.TryAdvance(ref tagCursor, 1))
			return false;
		tag.Tag = GaHeight;
		tag.Data = unchecked((uint)appliedHeight);
		if (!MuiExternalBoopsiTagVectorCodec.TryWrite(ref platform, tagCursor,
			tag)) return false;
		if (!MuiExternalBoopsiTagVectorCodec.TryAdvance(ref tagCursor, 1))
			return false;
		tag.Tag = 0; // TAG_DONE
		tag.Data = 0;
		if (!MuiExternalBoopsiTagVectorCodec.TryWrite(ref platform, tagCursor,
			tag)) return false;
		platform.DoMethod(obj, work);
		SetFlag(ref platform, instance, MuiExternalWrapperLayout.FlagRedraw, true);
		return true;
	}

	// MUIM_BoopsiQuery is the MorphOS bridge used by MUIized BOOPSI gadgets to
	// exchange their complete screen/geometry query record. Validate the named
	// packet first, then pass the caller-owned record to the wrapped object
	// through the existing BOOPSI method capability. Dtpic and uninitialized
	// wrappers do not own a BOOPSI object and therefore return no result.
	public static uint BoopsiQuery<TPlatform>(ref TPlatform platform,
		APTR instance, APTR message)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (Classify(ref platform, instance) != MuiExternalWrapperClass.Boopsi ||
			!MuiExternalBoopsiResourceCodec.TryRead(ref platform, instance,
				out var resources)) return 0;
		return MuiBoopsiQueryCore.DispatchToObject(ref platform,
			resources.BoopsiObject, message);
	}

	// MUIM_Draw. A disabled object draws nothing. A Boopsi object is rendered by
	// forwarding a gadget GM_RENDER method; a Dtpic picture is blitted through
	// the datatypes seam at its laid-out size, honouring the Lighten/Darken
	// state hints only where they are ABI-visible. Returns whether anything was
	// drawn.
	public static bool Draw<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Valid(ref platform, instance)) return false;
		var flags = ReadFlags(ref platform, instance);
		if ((flags & MuiExternalWrapperLayout.FlagShown) == 0) return false;
		if ((flags & MuiExternalWrapperLayout.FlagDisabled) != 0)
		{
			SetFlag(ref platform, instance, MuiExternalWrapperLayout.FlagRedraw,
				false);
			return false;
		}
		if (!MuiExternalDisplayStateCodec.TryRead(ref platform, instance,
			out var display)) return false;
		var rastPort = display.RastPort;
		if (!MuiExternalWrapperHeaderCodec.TryRead(ref platform, instance,
			out var header)) return false;
		var cls = header.Class;
		var drawn = false;
		if (cls == MuiExternalWrapperClass.Boopsi)
		{
			if (!MuiExternalBoopsiResourceCodec.TryRead(ref platform, instance,
				out var resources)) return false;
			if (!MuiExternalScratchStateCodec.TryRead(ref platform, instance,
				out var scratch)) return false;
			var obj = resources.BoopsiObject;
			var work = scratch.WorkBuffer;
			if (obj.IsNotNull && work.IsNotNull)
			{
				var render = default(MuiExternalBoopsiRenderMessage);
				render.MethodId = MuiExternalBoopsiPacketCodec.GmRender;
				render.RastPort = rastPort;
				if (MuiExternalBoopsiPacketCodec.WriteRender(ref platform, work,
					render))
				{
					platform.DoMethod(obj, work);
					drawn = true;
				}
			}
		}
		else if ((flags & MuiExternalWrapperLayout.FlagPicture) != 0)
		{
			if (!MuiExternalDtpicStateCodec.TryRead(ref platform, instance,
				out var dtpic)) return false;
			var picture = dtpic.PictureObject;
			var width = unchecked((int)dtpic.PicWidth);
			var height = unchecked((int)dtpic.PicHeight);
			drawn = platform.DrawPicture(picture, rastPort, 0, 0, width, height);
		}
		SetFlag(ref platform, instance, MuiExternalWrapperLayout.FlagRedraw, false);
		return drawn;
	}

	public static bool RedrawPending<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		Valid(ref platform, instance) &&
		(ReadFlags(ref platform, instance) &
			MuiExternalWrapperLayout.FlagRedraw) != 0;

	// ---- IDCMP_UPDATE -> MUI notification -------------------------------------

	// OM_UPDATE handler. A boopsi gadget that generates IDCMP_UPDATE
	// notifications targets its MUI wrapper with an OM_UPDATE carrying a tag
	// list of changed (attr,value) pairs. MUI turns each pair into a MUI
	// attribute notification. The tag list is walked with a bound; the last pair
	// is published as the most-recent notification and the notification count is
	// advanced per changed pair. Returns the number of pairs mapped.
	public static uint HandleUpdate<TPlatform>(ref TPlatform platform,
		APTR instance, APTR attrList) where TPlatform : struct, IMuiGuestMemory
	{
		if (!Valid(ref platform, instance) || attrList.IsNull) return 0;
		var mapped = 0u;
		var tagCursor = default(MuiExternalTagListCursor);
		tagCursor.Base = attrList;
		tagCursor.Index = 0;
		for (var index = 0u; index < MuiExternalTagListCursor.MaximumEntries;
			index++)
		{
			if (!MuiExternalTagListVectorCodec.TryRead(ref platform, tagCursor,
				out var item)) break;
			if (item.Tag == MuiAslTagListCore.TagDone) break;
			RecordNotify(ref platform, instance, item.Tag, item.Data);
			mapped++;
			if (index + 1 < MuiExternalTagListCursor.MaximumEntries &&
				!MuiExternalTagListVectorCodec.TryAdvance(ref tagCursor, 1)) break;
		}
		return mapped;
	}

	private static void RecordNotify<TPlatform>(ref TPlatform platform,
		APTR instance, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiExternalNotificationStateCodec.TryRead(ref platform, instance,
			out var state)) return;
		state.Attribute = attribute;
		state.Value = value;
		state.Count++;
		MuiExternalNotificationStateCodec.Write(ref platform, instance, state);
	}

	public static uint NotificationCount<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		Valid(ref platform, instance)
		&& MuiExternalNotificationStateCodec.TryRead(ref platform, instance,
			out var state) ? state.Count : 0;

	public static uint LastNotifiedAttribute<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		Valid(ref platform, instance)
		&& MuiExternalNotificationStateCodec.TryRead(ref platform, instance,
			out var state) ? state.Attribute : 0;

	public static uint LastNotifiedValue<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		Valid(ref platform, instance)
		&& MuiExternalNotificationStateCodec.TryRead(ref platform, instance,
			out var state) ? state.Value : 0;

	// ---- Attribute get -------------------------------------------------------

	public static bool GetAttribute<TPlatform>(ref TPlatform platform,
		APTR instance, uint attribute, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!Valid(ref platform, instance)) return false;
		if (!MuiExternalWrapperHeaderCodec.TryRead(ref platform, instance,
			out var header)) return false;
		var cls = header.Class;
		var flags = ReadFlags(ref platform, instance);

		switch (attribute)
		{
			case MuiExternalWrapperAttributes.Disabled:
				value = (flags & MuiExternalWrapperLayout.FlagDisabled) != 0 ? 1u : 0u;
				return true;
		}

		if (cls == MuiExternalWrapperClass.Boopsi)
		{
			if (!MuiExternalBoopsiGeometryCodec.TryRead(ref platform, instance,
				out var geometry)) return false;
			if (!MuiExternalBoopsiResourceCodec.TryRead(ref platform, instance,
				out var resources)) return false;
			switch (attribute)
			{
				case MuiExternalWrapperAttributes.Boopsi_Class:
					value = resources.PrivateClass.Raw;
					return true;
				case MuiExternalWrapperAttributes.Boopsi_ClassID:
					value = resources.ClassId.Raw;
					return true;
				case MuiExternalWrapperAttributes.Boopsi_MinWidth:
					value = geometry.MinWidth;
					return true;
				case MuiExternalWrapperAttributes.Boopsi_MinHeight:
					value = geometry.MinHeight;
					return true;
				case MuiExternalWrapperAttributes.Boopsi_MaxWidth:
					value = geometry.MaxWidth;
					return true;
				case MuiExternalWrapperAttributes.Boopsi_MaxHeight:
					value = geometry.MaxHeight;
					return true;
				case MuiExternalWrapperAttributes.Boopsi_TagWindow:
					value = geometry.TagWindow;
					return true;
				case MuiExternalWrapperAttributes.Boopsi_TagScreen:
					value = geometry.TagScreen;
					return true;
				case MuiExternalWrapperAttributes.Boopsi_TagDrawInfo:
					value = geometry.TagDrawInfo;
					return true;
				case MuiExternalWrapperAttributes.Boopsi_Object:
					// [..G]: only valid while the window is open (set up).
					value = (flags & MuiExternalWrapperLayout.FlagSetup) != 0
						? resources.BoopsiObject.Raw
						: 0u;
					return true;
			}
			return false;
		}

		if (!MuiExternalDtpicStateCodec.TryRead(ref platform, instance,
			out var dtpicState)) return false;
		switch (attribute)
		{
			case MuiExternalWrapperAttributes.Dtpic_Name:
				// Return the class-owned copy (a valid pointer callers may read).
				value = dtpicState.OwnedName.Raw;
				return true;
			case MuiExternalWrapperAttributes.Dtpic_Alpha:
				value = dtpicState.Alpha;
				return true;
			case MuiExternalWrapperAttributes.Dtpic_FreeHoriz:
				value = (flags & MuiExternalWrapperLayout.FlagFreeHoriz) != 0 ? 1u : 0u;
				return true;
			case MuiExternalWrapperAttributes.Dtpic_FreeVert:
				value = (flags & MuiExternalWrapperLayout.FlagFreeVert) != 0 ? 1u : 0u;
				return true;
			case MuiExternalWrapperAttributes.Dtpic_LightenOnMouse:
				value = (flags & MuiExternalWrapperLayout.FlagLighten) != 0 ? 1u : 0u;
				return true;
			case MuiExternalWrapperAttributes.Dtpic_DarkenSelState:
				value = (flags & MuiExternalWrapperLayout.FlagDarken) != 0 ? 1u : 0u;
				return true;
			case MuiExternalWrapperAttributes.Dtpic_MinWidth:
				value = dtpicState.MinWidth;
				return true;
			case MuiExternalWrapperAttributes.Dtpic_MinHeight:
				value = dtpicState.MinHeight;
				return true;
		}
		return false;
	}

	// ---- Attribute set -------------------------------------------------------

	// Set a recognized wrapper attribute. `handled` reports whether the
	// attribute belongs to the wrapper at all; when false the caller may pass an
	// unknown attribute through to the wrapped boopsi object (MUI transparency).
	public static bool SetAttribute<TPlatform>(ref TPlatform platform,
		APTR instance, uint attribute, uint value, bool isInit, bool notify,
		out bool changed, out bool handled)
		where TPlatform : struct, IMuiServicePlatform
	{
		changed = false;
		handled = false;
		if (!Valid(ref platform, instance)) return false;
		if (!MuiExternalWrapperHeaderCodec.TryRead(ref platform, instance,
			out var header)) return false;
		var cls = header.Class;

		switch (attribute)
		{
			case MuiExternalWrapperAttributes.Disabled:
				handled = true;
				changed = SetFlag(ref platform, instance,
					MuiExternalWrapperLayout.FlagDisabled, value != 0);
				if (changed)
					SetFlag(ref platform, instance,
						MuiExternalWrapperLayout.FlagRedraw, true);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
		}

		if (cls == MuiExternalWrapperClass.Boopsi)
			return SetBoopsi(ref platform, instance, attribute, value, isInit,
				notify, out changed, out handled);
		return SetDtpic(ref platform, instance, attribute, value, isInit, notify,
			out changed, out handled);
	}

	private static bool SetBoopsi<TPlatform>(ref TPlatform platform,
		APTR instance, uint attribute, uint value, bool isInit, bool notify,
		out bool changed, out bool handled)
		where TPlatform : struct, IMuiServicePlatform
	{
		changed = false;
		handled = true;
		switch (attribute)
		{
			case MuiExternalWrapperAttributes.Boopsi_Class:
				changed = WriteBoopsiResourceField(ref platform, instance,
					MuiExternalBoopsiResourceField.PrivateClass, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiExternalWrapperAttributes.Boopsi_ClassID:
				changed = WriteBoopsiResourceField(ref platform, instance,
					MuiExternalBoopsiResourceField.ClassId, value);
				// Detect the OS 3.0/3.1 colorwheel.gadget for the -1 workaround.
				SetFlag(ref platform, instance,
					MuiExternalWrapperLayout.FlagColorwheel,
					IsColorwheelId(ref platform, APTR.FromPointer(value)));
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiExternalWrapperAttributes.Boopsi_MinWidth:
				changed = WriteBoopsiGeometryField(ref platform, instance,
					MuiExternalBoopsiGeometryField.MinWidth, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiExternalWrapperAttributes.Boopsi_MinHeight:
				changed = WriteBoopsiGeometryField(ref platform, instance,
					MuiExternalBoopsiGeometryField.MinHeight, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiExternalWrapperAttributes.Boopsi_MaxWidth:
				changed = WriteBoopsiGeometryField(ref platform, instance,
					MuiExternalBoopsiGeometryField.MaxWidth, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiExternalWrapperAttributes.Boopsi_MaxHeight:
				changed = WriteBoopsiGeometryField(ref platform, instance,
					MuiExternalBoopsiGeometryField.MaxHeight, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiExternalWrapperAttributes.Boopsi_TagWindow:
				changed = WriteBoopsiGeometryField(ref platform, instance,
					MuiExternalBoopsiGeometryField.TagWindow, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiExternalWrapperAttributes.Boopsi_TagScreen:
				changed = WriteBoopsiGeometryField(ref platform, instance,
					MuiExternalBoopsiGeometryField.TagScreen, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiExternalWrapperAttributes.Boopsi_TagDrawInfo:
				changed = WriteBoopsiGeometryField(ref platform, instance,
					MuiExternalBoopsiGeometryField.TagDrawInfo, value);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiExternalWrapperAttributes.Boopsi_Remember:
				// [I..]: append a tag id to remember across dispose/regenerate.
				if (isInit) changed = AddRemember(ref platform, instance, value);
				return true;
			case MuiExternalWrapperAttributes.Boopsi_Smart:
				// [I..]: mark the gadget resizable so MUI never regenerates it.
				if (isInit)
					changed = SetFlag(ref platform, instance,
						MuiExternalWrapperLayout.FlagSmart, value != 0);
				return true;
			case MuiExternalWrapperAttributes.Boopsi_Object:
				return true;   // [..G]: silently ignore a set
		}
		handled = false;   // unknown attribute -> transparent pass-through
		return false;
	}

	private static bool SetDtpic<TPlatform>(ref TPlatform platform, APTR instance,
		uint attribute, uint value, bool isInit, bool notify, out bool changed,
		out bool handled) where TPlatform : struct, IMuiServicePlatform
	{
		changed = false;
		handled = true;
		switch (attribute)
		{
			case MuiExternalWrapperAttributes.Dtpic_Name:
				changed = SetName(ref platform, instance, APTR.FromPointer(value));
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiExternalWrapperAttributes.Dtpic_Alpha:
				changed = WriteDtpicField(ref platform, instance,
					MuiExternalDtpicField.Alpha, value);
				if (changed)
					SetFlag(ref platform, instance,
						MuiExternalWrapperLayout.FlagRedraw, true);
				Notify(ref platform, instance, attribute, value, isInit, notify,
					changed);
				return true;
			case MuiExternalWrapperAttributes.Dtpic_FreeHoriz:
				if (isInit)
					changed = SetFlag(ref platform, instance,
						MuiExternalWrapperLayout.FlagFreeHoriz, value != 0);
				return true;
			case MuiExternalWrapperAttributes.Dtpic_FreeVert:
				if (isInit)
					changed = SetFlag(ref platform, instance,
						MuiExternalWrapperLayout.FlagFreeVert, value != 0);
				return true;
			case MuiExternalWrapperAttributes.Dtpic_LightenOnMouse:
				if (isInit)
					changed = SetFlag(ref platform, instance,
						MuiExternalWrapperLayout.FlagLighten, value != 0);
				return true;
			case MuiExternalWrapperAttributes.Dtpic_DarkenSelState:
				if (isInit)
					changed = SetFlag(ref platform, instance,
						MuiExternalWrapperLayout.FlagDarken, value != 0);
				return true;
			case MuiExternalWrapperAttributes.Dtpic_MinWidth:
				if (isInit)
					changed = WriteDtpicField(ref platform, instance,
						MuiExternalDtpicField.MinWidth, value);
				return true;
			case MuiExternalWrapperAttributes.Dtpic_MinHeight:
				if (isInit)
					changed = WriteDtpicField(ref platform, instance,
						MuiExternalDtpicField.MinHeight, value);
				return true;
		}
		handled = false;
		return false;
	}

	// Transparent pass-through: forward an unknown attribute set/get to the
	// wrapped boopsi object exactly as MUI does. Only meaningful for a Boopsi
	// instance whose object currently exists.
	public static bool PassThroughSet<TPlatform>(ref TPlatform platform,
		APTR instance, uint attribute, uint value)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (Classify(ref platform, instance) != MuiExternalWrapperClass.Boopsi)
			return false;
		if (!MuiExternalBoopsiResourceCodec.TryRead(ref platform, instance,
			out var resources)) return false;
		var obj = resources.BoopsiObject;
		if (obj.IsNull) return false;
		BoopsiSet(ref platform, instance, obj, attribute, value);
		return true;
	}

	public static bool PassThroughGet<TPlatform>(ref TPlatform platform,
		APTR instance, uint attribute, out uint value)
		where TPlatform : struct, IMuiServicePlatform
	{
		value = 0;
		if (Classify(ref platform, instance) != MuiExternalWrapperClass.Boopsi)
			return false;
		if (!MuiExternalBoopsiResourceCodec.TryRead(ref platform, instance,
			out var resources)) return false;
		var obj = resources.BoopsiObject;
		if (obj.IsNull) return false;
		value = BoopsiGet(ref platform, instance, obj, attribute);
		return true;
	}

	// ---- Boopsi object OM_SET / OM_GET marshalling ---------------------------

	private static void BoopsiSet<TPlatform>(ref TPlatform platform,
		APTR instance, APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiExternalScratchStateCodec.TryRead(ref platform, instance,
			out var scratch)) return;
		var work = scratch.WorkBuffer;
		if (work.IsNull || !platform.IsMapped(work,
			MuiExternalWrapperLayout.WorkSize)) return;
		if (!MuiExternalBoopsiPacketCodec.TryGetInlineTagList(ref platform,
			work, out var list)) return;
		var opSet = default(MuiExternalBoopsiOpSetMessage);
		opSet.MethodId = MuiExternalBoopsiPacketCodec.OmSet;
		opSet.AttributeList = list;
		if (!MuiExternalBoopsiPacketCodec.WriteOpSet(ref platform, work, opSet))
			return;
		var tagCursor = default(MuiExternalBoopsiTagCursor);
		tagCursor.Base = list;
		tagCursor.Index = 0;
		var tag = default(MuiExternalBoopsiTagItem);
		tag.Tag = attribute;
		tag.Data = value;
		if (!MuiExternalBoopsiTagVectorCodec.TryWrite(ref platform, tagCursor,
			tag)) return;
		if (!MuiExternalBoopsiTagVectorCodec.TryAdvance(ref tagCursor, 1) ||
			!MuiExternalBoopsiTagVectorCodec.TryWrite(ref platform, tagCursor,
			default)) return;
		platform.DoMethod(obj, work);
	}

	private static uint BoopsiGet<TPlatform>(ref TPlatform platform,
		APTR instance, APTR obj, uint attribute)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiExternalScratchStateCodec.TryRead(ref platform, instance,
			out var scratch)) return 0;
		var work = scratch.WorkBuffer;
		if (work.IsNull || !platform.IsMapped(work,
			MuiExternalWrapperLayout.WorkSize)) return 0;
		if (!MuiExternalBoopsiPacketCodec.TryGetInlineResult(ref platform, work,
			out var storage)) return 0;
		if (!MuiExternalBoopsiPacketCodec.WriteResult(ref platform, storage,
			default)) return 0;
		var opGet = default(MuiExternalBoopsiOpGetMessage);
		opGet.MethodId = MuiExternalBoopsiPacketCodec.OmGet;
		opGet.Attribute = attribute;
		opGet.Storage = storage;
		if (!MuiExternalBoopsiPacketCodec.WriteOpGet(ref platform, work, opGet))
			return 0;
		platform.DoMethod(obj, work);
		return MuiExternalBoopsiPacketCodec.TryReadResult(ref platform, storage,
			out var result) ? result.Value : 0;
	}

	private static bool IsColorwheelId<TPlatform>(ref TPlatform platform,
		APTR classId) where TPlatform : struct, IMuiGuestMemory
	{
		// Byte-exact match against "colorwheel.gadget" with no managed data.
		if (classId.IsNull) return false;
		return M(ref platform, classId, 0, (byte)'c') &&
			M(ref platform, classId, 1, (byte)'o') &&
			M(ref platform, classId, 2, (byte)'l') &&
			M(ref platform, classId, 3, (byte)'o') &&
			M(ref platform, classId, 4, (byte)'r') &&
			M(ref platform, classId, 5, (byte)'w') &&
			M(ref platform, classId, 6, (byte)'h') &&
			M(ref platform, classId, 7, (byte)'e') &&
			M(ref platform, classId, 8, (byte)'e') &&
			M(ref platform, classId, 9, (byte)'l') &&
			M(ref platform, classId, 10, (byte)'.') &&
			M(ref platform, classId, 11, (byte)'g') &&
			M(ref platform, classId, 12, (byte)'a') &&
			M(ref platform, classId, 13, (byte)'d') &&
			M(ref platform, classId, 14, (byte)'g') &&
			M(ref platform, classId, 15, (byte)'e') &&
			M(ref platform, classId, 16, (byte)'t') &&
			M(ref platform, classId, 17, 0);
	}

	private static bool M<TPlatform>(ref TPlatform platform, APTR text, int index,
		byte expected) where TPlatform : struct, IMuiGuestMemory =>
		TryReadExternalTextByte(ref platform, text, index, out var value) &&
		value == expected;

	private static bool TryReadExternalTextByte<TPlatform>(ref TPlatform platform,
		APTR text, int index, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (index < 0) return false;
		var cursor = default(MuiExternalTextByteCursor);
		cursor.Base = text;
		cursor.Length = MuiExternalTextByteCursor.MaximumLength;
		cursor.Index = (uint)index;
		return MuiExternalTextByteCursorCodec.TryReadByte(ref platform, cursor,
			out value);
	}

	// ---- Recursive class-owned disposal --------------------------------------

	// Release everything the class owns: the wrapped boopsi object (and the class
	// library the wrapper opened, closed exactly once) or the datatypes picture
	// (released exactly once), the owned name copy, the remember buffer and the
	// message scratch. Guarded pointers make the whole teardown idempotent.
	internal static void DisposeOwned<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiExternalWrapperHeaderCodec.TryRead(ref platform, instance,
			out var header)) return;
		var cls = header.Class;
		if (cls == MuiExternalWrapperClass.Boopsi)
			DisposeBoopsiObject(ref platform, instance, true);
		else
			ReleasePicture(ref platform, instance);

		FreeOwnedName(ref platform, instance);

		if (!MuiExternalScratchStateCodec.TryRead(ref platform, instance,
			out var scratch)) return;
		var remember = scratch.RememberBuffer;
		if (remember.IsNotNull)
		{
			Free(ref platform, remember, MuiExternalWrapperLayout.RememberSize);
			scratch.RememberBuffer = APTR.Null;
			scratch.RememberCount = 0;
		}
		var work = scratch.WorkBuffer;
		if (work.IsNotNull)
		{
			Free(ref platform, work, MuiExternalWrapperLayout.WorkSize);
			scratch.WorkBuffer = APTR.Null;
		}
		MuiExternalScratchStateCodec.Write(ref platform, instance, scratch);
	}

	// ---- Internals -----------------------------------------------------------

	private static void Notify<TPlatform>(ref TPlatform platform, APTR instance,
		uint attribute, uint value, bool isInit, bool notify, bool changed)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (isInit || !notify || !changed) return;
		RecordNotify(ref platform, instance, attribute, value);
	}

	private static uint ReadFlags<TPlatform>(ref TPlatform platform,
		APTR instance) where TPlatform : struct, IMuiGuestMemory =>
		MuiExternalWrapperHeaderCodec.TryRead(ref platform, instance,
			out var header) ? header.Flags : 0;

	private static bool WriteBoopsiGeometryField<TPlatform>(
		ref TPlatform platform, APTR instance,
		MuiExternalBoopsiGeometryField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiExternalBoopsiGeometryCodec.TryRead(ref platform, instance,
			out var state)) return false;
		uint previous;
		switch (field)
		{
			case MuiExternalBoopsiGeometryField.MinWidth:
				previous = state.MinWidth;
				break;
			case MuiExternalBoopsiGeometryField.MinHeight:
				previous = state.MinHeight;
				break;
			case MuiExternalBoopsiGeometryField.MaxWidth:
				previous = state.MaxWidth;
				break;
			case MuiExternalBoopsiGeometryField.MaxHeight:
				previous = state.MaxHeight;
				break;
			case MuiExternalBoopsiGeometryField.TagWindow:
				previous = state.TagWindow;
				break;
			case MuiExternalBoopsiGeometryField.TagScreen:
				previous = state.TagScreen;
				break;
			case MuiExternalBoopsiGeometryField.TagDrawInfo:
				previous = state.TagDrawInfo;
				break;
			default:
				return false;
		}
		if (previous == value) return false;
		switch (field)
		{
			case MuiExternalBoopsiGeometryField.MinWidth:
				state.MinWidth = value;
				break;
			case MuiExternalBoopsiGeometryField.MinHeight:
				state.MinHeight = value;
				break;
			case MuiExternalBoopsiGeometryField.MaxWidth:
				state.MaxWidth = value;
				break;
			case MuiExternalBoopsiGeometryField.MaxHeight:
				state.MaxHeight = value;
				break;
			case MuiExternalBoopsiGeometryField.TagWindow:
				state.TagWindow = value;
				break;
			case MuiExternalBoopsiGeometryField.TagScreen:
				state.TagScreen = value;
				break;
			case MuiExternalBoopsiGeometryField.TagDrawInfo:
				state.TagDrawInfo = value;
				break;
			default:
				return false;
		}
		return MuiExternalBoopsiGeometryCodec.Write(ref platform, instance,
			state);
	}

	private static bool WriteDtpicField<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalDtpicField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiExternalDtpicStateCodec.TryRead(ref platform, instance,
			out var state)) return false;
		uint previous;
		switch (field)
		{
			case MuiExternalDtpicField.Alpha:
				previous = state.Alpha;
				break;
			case MuiExternalDtpicField.MinWidth:
				previous = state.MinWidth;
				break;
			case MuiExternalDtpicField.MinHeight:
				previous = state.MinHeight;
				break;
			default:
				return false;
		}
		if (previous == value) return false;
		switch (field)
		{
			case MuiExternalDtpicField.Alpha:
				state.Alpha = value;
				break;
			case MuiExternalDtpicField.MinWidth:
				state.MinWidth = value;
				break;
			case MuiExternalDtpicField.MinHeight:
				state.MinHeight = value;
				break;
			default:
				return false;
		}
		return MuiExternalDtpicStateCodec.Write(ref platform, instance, state);
	}

	private static bool WriteBoopsiResourceField<TPlatform>(
		ref TPlatform platform, APTR instance,
		MuiExternalBoopsiResourceField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiExternalBoopsiResourceCodec.TryRead(ref platform, instance,
			out var state)) return false;
		APTR previous;
		switch (field)
		{
			case MuiExternalBoopsiResourceField.PrivateClass:
				previous = state.PrivateClass;
				break;
			case MuiExternalBoopsiResourceField.ClassId:
				previous = state.ClassId;
				break;
			case MuiExternalBoopsiResourceField.OpenedClass:
				previous = state.OpenedClass;
				break;
			case MuiExternalBoopsiResourceField.BoopsiObject:
				previous = state.BoopsiObject;
				break;
			case MuiExternalBoopsiResourceField.CreationTags:
				previous = state.CreationTags;
				break;
			default:
				return false;
		}
		if (previous.Raw == value) return false;
		var updated = APTR.FromPointer(value);
		switch (field)
		{
			case MuiExternalBoopsiResourceField.PrivateClass:
				state.PrivateClass = updated;
				break;
			case MuiExternalBoopsiResourceField.ClassId:
				state.ClassId = updated;
				break;
			case MuiExternalBoopsiResourceField.OpenedClass:
				state.OpenedClass = updated;
				break;
			case MuiExternalBoopsiResourceField.BoopsiObject:
				state.BoopsiObject = updated;
				break;
			case MuiExternalBoopsiResourceField.CreationTags:
				state.CreationTags = updated;
				break;
			default:
				return false;
		}
		return MuiExternalBoopsiResourceCodec.Write(ref platform, instance,
			state);
	}

	private static bool SetFlag<TPlatform>(ref TPlatform platform, APTR instance,
		uint bit, bool set) where TPlatform : struct, IMuiGuestMemory
	{
		// Flag transitions may be paired with a resource-pointer write (picture
		// acquire/release and BOOPSI create/dispose). Read the packed header
		// structurally here so the two named records can be committed in either
		// order without exposing a transient state to live callers; all public
		// entry points still admit the completed sidecar strictly.
		if (!MuiExternalWrapperHeaderCodec.TryReadStructural(ref platform,
			instance, out var current)) return false;
		var flags = current.Flags;
		var updated = set ? flags | bit : flags & ~bit;
		if (updated == flags) return false;
		current.Flags = updated;
		if (!MuiExternalWrapperHeaderCodec.Write(ref platform, instance, current))
			return false;
		return true;
	}

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

	// ---- Standard BOOPSI / gadgetclass identifiers ---------------------------

	private const uint OmSet = 0x00000103u;   // OM_SET
	private const uint OmGet = 0x00000104u;   // OM_GET
	private const uint GmRender = 0x00000001u; // GM_RENDER (gadgetclass.h)
	// gadgetclass tags: GA_Dummy = TAG_USER + 0x30000. The GA_Rel* variants are
	// interleaved with the absolute variants in intuition/gadgetclass.h, so the
	// absolute geometry tags are not contiguous:
	//   GA_Left=+1, GA_RelRight=+2, GA_Top=+3, GA_RelBottom=+4,
	//   GA_Width=+5, GA_RelWidth=+6, GA_Height=+7, GA_RelHeight=+8.
	private const uint GaLeft = 0x80030001u;   // GA_Dummy+1
	private const uint GaTop = 0x80030003u;    // GA_Dummy+3
	private const uint GaWidth = 0x80030005u;  // GA_Dummy+5
	private const uint GaHeight = 0x80030007u; // GA_Dummy+7
}

// Official MG09 Boopsi.mui / Dtpic.mui attribute and method identifiers,
// resolved from the authority (libraries/mui.h in the frozen MorphOS 3.20 SDK,
// mirrored in the abi-inventory) and the MUI_Boopsi / MUI_Dtpic autodocs. Kept
// beside the core so classification and dispatch stay byte-exact.
//
// Disposition notes required by the goal:
//  * The MUIA_Dtpic_* attributes are documented "yet undocumented" in the
//    autodoc; only their ABI-visible state (owned name copy, alpha, the
//    lighten/darken/free-horiz/free-vert flags and the explicit min sizes) is
//    modelled. No undocumented Dtpic internals are invented.
//  * MUIA_Boopsi_Object is [..G] and is only meaningful while the window is
//    open, matching the autodoc.
//  * The OS 3.0/3.1 colorwheel.gadget -1 width/height workaround is applied
//    exactly as the MUI_Boopsi autodoc requires.
public static class MuiExternalWrapperAttributes
{
	// Shared Area attribute.
	public const uint Disabled = 0x80423661u;

	// Boopsi.mui
	public const uint Boopsi_Class = 0x80426999u;
	public const uint Boopsi_ClassID = 0x8042bfa3u;
	public const uint Boopsi_MaxHeight = 0x8042757fu;
	public const uint Boopsi_MaxWidth = 0x8042bcb1u;
	public const uint Boopsi_MinHeight = 0x80422c93u;
	public const uint Boopsi_MinWidth = 0x80428fb2u;
	public const uint Boopsi_Object = 0x80420178u;
	public const uint Boopsi_Remember = 0x8042f4bdu;
	public const uint Boopsi_Smart = 0x8042b8d7u;
	public const uint Boopsi_TagDrawInfo = 0x8042bae7u;
	public const uint Boopsi_TagScreen = 0x8042bc71u;
	public const uint Boopsi_TagWindow = 0x8042e11du;

	// Dtpic.mui
	public const uint Dtpic_Alpha = 0x8042b4dbu;
	public const uint Dtpic_DarkenSelState = 0x80423247u;
	public const uint Dtpic_FreeHoriz = 0x8042d360u;
	public const uint Dtpic_FreeVert = 0x80424c12u;
	public const uint Dtpic_LightenOnMouse = 0x8042966au;
	public const uint Dtpic_MinHeight = 0x80423eccu;
	public const uint Dtpic_MinWidth = 0x8042c417u;
	public const uint Dtpic_Name = 0x80423d72u;
}
