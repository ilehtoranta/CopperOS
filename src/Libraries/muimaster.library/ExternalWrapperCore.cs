/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

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

internal static class MuiExternalStateCursorCodec
{
	internal static bool TryGetAddress(MuiExternalStateCursor cursor,
		out APTR address)
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Region)
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
		if (cursor.Instance.IsNull || cursor.Instance.Raw >
			uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Instance.Raw + offset);
		return true;
	}
}

// The fixed Boopsi geometry/configuration block contains the caller-visible
// minimum/maximum values and the three tag ids used to patch the creation list.
// Keeping these seven words together makes the ABI boundary explicit and keeps
// the wrapper logic independent of private guest offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalBoopsiGeometryState
{
	internal const uint Size = 28;

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
		var cursor = default(MuiExternalStateCursor);
		cursor.Instance = instance;
		cursor.Region = MuiExternalStateRegion.BoopsiGeometry;
		if (!MuiExternalStateCursorCodec.TryGetAddress(cursor, out address))
			return false;
		return platform.IsMapped(address, MuiExternalBoopsiGeometryState.Size);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, out MuiExternalBoopsiGeometryState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryAddress(ref platform, instance, out _)) return false;
		return MuiExternalBoopsiGeometryFieldCursorCodec.TryRead(ref platform,
			instance, MuiExternalBoopsiGeometryField.MinWidth, out value.MinWidth) &&
			MuiExternalBoopsiGeometryFieldCursorCodec.TryRead(ref platform,
				instance, MuiExternalBoopsiGeometryField.MinHeight,
				out value.MinHeight) &&
			MuiExternalBoopsiGeometryFieldCursorCodec.TryRead(ref platform,
				instance, MuiExternalBoopsiGeometryField.MaxWidth,
				out value.MaxWidth) &&
			MuiExternalBoopsiGeometryFieldCursorCodec.TryRead(ref platform,
				instance, MuiExternalBoopsiGeometryField.MaxHeight,
				out value.MaxHeight) &&
			MuiExternalBoopsiGeometryFieldCursorCodec.TryRead(ref platform,
				instance, MuiExternalBoopsiGeometryField.TagWindow,
				out value.TagWindow) &&
			MuiExternalBoopsiGeometryFieldCursorCodec.TryRead(ref platform,
				instance, MuiExternalBoopsiGeometryField.TagScreen,
				out value.TagScreen) &&
			MuiExternalBoopsiGeometryFieldCursorCodec.TryRead(ref platform,
				instance, MuiExternalBoopsiGeometryField.TagDrawInfo,
				out value.TagDrawInfo);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalBoopsiGeometryState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryAddress(ref platform, instance, out _)) return false;
		return MuiExternalBoopsiGeometryFieldCursorCodec.TryWrite(ref platform,
			instance, MuiExternalBoopsiGeometryField.MinWidth, value.MinWidth) &&
			MuiExternalBoopsiGeometryFieldCursorCodec.TryWrite(ref platform,
				instance, MuiExternalBoopsiGeometryField.MinHeight,
				value.MinHeight) &&
			MuiExternalBoopsiGeometryFieldCursorCodec.TryWrite(ref platform,
				instance, MuiExternalBoopsiGeometryField.MaxWidth, value.MaxWidth) &&
			MuiExternalBoopsiGeometryFieldCursorCodec.TryWrite(ref platform,
				instance, MuiExternalBoopsiGeometryField.MaxHeight,
				value.MaxHeight) &&
			MuiExternalBoopsiGeometryFieldCursorCodec.TryWrite(ref platform,
				instance, MuiExternalBoopsiGeometryField.TagWindow,
				value.TagWindow) &&
			MuiExternalBoopsiGeometryFieldCursorCodec.TryWrite(ref platform,
				instance, MuiExternalBoopsiGeometryField.TagScreen,
				value.TagScreen) &&
			MuiExternalBoopsiGeometryFieldCursorCodec.TryWrite(ref platform,
				instance, MuiExternalBoopsiGeometryField.TagDrawInfo,
				value.TagDrawInfo);
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

internal static class MuiExternalBoopsiGeometryFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalBoopsiGeometryFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Field)
		{
			case MuiExternalBoopsiGeometryField.MinWidth:
				offset = 0;
				break;
			case MuiExternalBoopsiGeometryField.MinHeight:
				offset = 4;
				break;
			case MuiExternalBoopsiGeometryField.MaxWidth:
				offset = 8;
				break;
			case MuiExternalBoopsiGeometryField.MaxHeight:
				offset = 12;
				break;
			case MuiExternalBoopsiGeometryField.TagWindow:
				offset = 16;
				break;
			case MuiExternalBoopsiGeometryField.TagScreen:
				offset = 20;
				break;
			case MuiExternalBoopsiGeometryField.TagDrawInfo:
				offset = 24;
				break;
			default:
				return false;
		}
		var region = default(MuiExternalStateCursor);
		region.Instance = cursor.Instance;
		region.Region = MuiExternalStateRegion.BoopsiGeometry;
		if (!MuiExternalStateCursorCodec.TryGetAddress(region,
			out var baseAddress) || baseAddress.Raw >
			uint.MaxValue - offset) return false;
		address = APTR.FromPointer(baseAddress.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalBoopsiGeometryField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiExternalBoopsiGeometryFieldCursor);
		cursor.Instance = instance;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalBoopsiGeometryField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiExternalBoopsiGeometryFieldCursor);
		cursor.Instance = instance;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// The five contiguous pointer slots that describe a Boopsi wrapper’s external
// resource ownership and caller-provided inputs.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalBoopsiResourceState
{
	internal const uint Size = 20;

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
		var cursor = default(MuiExternalStateCursor);
		cursor.Instance = instance;
		cursor.Region = MuiExternalStateRegion.BoopsiResources;
		if (!MuiExternalStateCursorCodec.TryGetAddress(cursor, out address))
			return false;
		return platform.IsMapped(address, MuiExternalBoopsiResourceState.Size);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, out MuiExternalBoopsiResourceState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryAddress(ref platform, instance, out _)) return false;
		return MuiExternalBoopsiResourceFieldCursorCodec.TryRead(ref platform,
			instance, MuiExternalBoopsiResourceField.PrivateClass,
			out value.PrivateClass) &&
			MuiExternalBoopsiResourceFieldCursorCodec.TryRead(ref platform,
				instance, MuiExternalBoopsiResourceField.ClassId,
				out value.ClassId) &&
			MuiExternalBoopsiResourceFieldCursorCodec.TryRead(ref platform,
				instance, MuiExternalBoopsiResourceField.OpenedClass,
				out value.OpenedClass) &&
			MuiExternalBoopsiResourceFieldCursorCodec.TryRead(ref platform,
				instance, MuiExternalBoopsiResourceField.BoopsiObject,
				out value.BoopsiObject) &&
			MuiExternalBoopsiResourceFieldCursorCodec.TryRead(ref platform,
				instance, MuiExternalBoopsiResourceField.CreationTags,
				out value.CreationTags);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalBoopsiResourceState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryAddress(ref platform, instance, out _)) return false;
		return MuiExternalBoopsiResourceFieldCursorCodec.TryWrite(ref platform,
			instance, MuiExternalBoopsiResourceField.PrivateClass,
			value.PrivateClass) &&
			MuiExternalBoopsiResourceFieldCursorCodec.TryWrite(ref platform,
				instance, MuiExternalBoopsiResourceField.ClassId, value.ClassId) &&
			MuiExternalBoopsiResourceFieldCursorCodec.TryWrite(ref platform,
				instance, MuiExternalBoopsiResourceField.OpenedClass,
				value.OpenedClass) &&
			MuiExternalBoopsiResourceFieldCursorCodec.TryWrite(ref platform,
				instance, MuiExternalBoopsiResourceField.BoopsiObject,
				value.BoopsiObject) &&
			MuiExternalBoopsiResourceFieldCursorCodec.TryWrite(ref platform,
				instance, MuiExternalBoopsiResourceField.CreationTags,
				value.CreationTags);
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

internal static class MuiExternalBoopsiResourceFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalBoopsiResourceFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Field)
		{
			case MuiExternalBoopsiResourceField.PrivateClass:
				offset = 0;
				break;
			case MuiExternalBoopsiResourceField.ClassId:
				offset = 4;
				break;
			case MuiExternalBoopsiResourceField.OpenedClass:
				offset = 8;
				break;
			case MuiExternalBoopsiResourceField.BoopsiObject:
				offset = 12;
				break;
			case MuiExternalBoopsiResourceField.CreationTags:
				offset = 16;
				break;
			default:
				return false;
		}
		var region = default(MuiExternalStateCursor);
		region.Instance = cursor.Instance;
		region.Region = MuiExternalStateRegion.BoopsiResources;
		if (!MuiExternalStateCursorCodec.TryGetAddress(region,
			out var baseAddress) || baseAddress.Raw >
			uint.MaxValue - offset) return false;
		address = APTR.FromPointer(baseAddress.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalBoopsiResourceField field, out APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = APTR.Null;
		var cursor = default(MuiExternalBoopsiResourceFieldCursor);
		cursor.Instance = instance;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = APTR.FromPointer(platform.ReadUInt32(address, 0));
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalBoopsiResourceField field, APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiExternalBoopsiResourceFieldCursor);
		cursor.Instance = instance;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value.Raw);
		return true;
	}
}

// Owned scratch storage used by both wrapper classes. The remember list keeps
// Boopsi tag values across regeneration; the work block is the common message
// packet scratch for Boopsi calls and Dtpic layout.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalScratchState
{
	internal const uint Size = 12;

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
		var cursor = default(MuiExternalStateCursor);
		cursor.Instance = instance;
		cursor.Region = MuiExternalStateRegion.Scratch;
		if (!MuiExternalStateCursorCodec.TryGetAddress(cursor, out address))
			return false;
		return platform.IsMapped(address, MuiExternalScratchState.Size);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, out MuiExternalScratchState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryAddress(ref platform, instance, out _)) return false;
		uint raw;
		if (!MuiExternalScratchFieldCursorCodec.TryRead(ref platform, instance,
			MuiExternalScratchField.RememberBuffer, out raw)) return false;
		value.RememberBuffer = APTR.FromPointer(raw);
		if (!MuiExternalScratchFieldCursorCodec.TryRead(ref platform, instance,
			MuiExternalScratchField.RememberCount, out value.RememberCount) ||
			!MuiExternalScratchFieldCursorCodec.TryRead(ref platform, instance,
				MuiExternalScratchField.WorkBuffer, out raw)) return false;
		value.WorkBuffer = APTR.FromPointer(raw);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalScratchState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryAddress(ref platform, instance, out _)) return false;
		return MuiExternalScratchFieldCursorCodec.TryWrite(ref platform, instance,
			MuiExternalScratchField.RememberBuffer, value.RememberBuffer.Raw) &&
			MuiExternalScratchFieldCursorCodec.TryWrite(ref platform, instance,
				MuiExternalScratchField.RememberCount, value.RememberCount) &&
			MuiExternalScratchFieldCursorCodec.TryWrite(ref platform, instance,
				MuiExternalScratchField.WorkBuffer, value.WorkBuffer.Raw);
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

internal static class MuiExternalScratchFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalScratchFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Field)
		{
			case MuiExternalScratchField.RememberBuffer:
				offset = 0;
				break;
			case MuiExternalScratchField.RememberCount:
				offset = 4;
				break;
			case MuiExternalScratchField.WorkBuffer:
				offset = 8;
				break;
			default:
				return false;
		}
		var region = default(MuiExternalStateCursor);
		region.Instance = cursor.Instance;
		region.Region = MuiExternalStateRegion.Scratch;
		if (!MuiExternalStateCursorCodec.TryGetAddress(region,
			out var baseAddress) || baseAddress.Raw >
			uint.MaxValue - offset) return false;
		address = APTR.FromPointer(baseAddress.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalScratchField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiExternalScratchFieldCursor);
		cursor.Instance = instance;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalScratchField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiExternalScratchFieldCursor);
		cursor.Instance = instance;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// All Dtpic-owned and caller-facing scalar state is one contiguous guest block:
// the caller name, owned copy, picture handle, alpha, explicit minimums, and
// laid-out natural dimensions.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalDtpicState
{
	internal const uint Size = 36;

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
		var cursor = default(MuiExternalStateCursor);
		cursor.Instance = instance;
		cursor.Region = MuiExternalStateRegion.Dtpic;
		if (!MuiExternalStateCursorCodec.TryGetAddress(cursor, out address))
			return false;
		return platform.IsMapped(address, MuiExternalDtpicState.Size);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, out MuiExternalDtpicState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryAddress(ref platform, instance, out _)) return false;
		uint raw;
		if (!MuiExternalDtpicFieldCursorCodec.TryRead(ref platform, instance,
			MuiExternalDtpicField.CallerName, out raw)) return false;
		value.CallerName = APTR.FromPointer(raw);
		if (!MuiExternalDtpicFieldCursorCodec.TryRead(ref platform, instance,
			MuiExternalDtpicField.OwnedName, out raw)) return false;
		value.OwnedName = APTR.FromPointer(raw);
		if (!MuiExternalDtpicFieldCursorCodec.TryRead(ref platform, instance,
			MuiExternalDtpicField.OwnedNameSize, out value.OwnedNameSize) ||
			!MuiExternalDtpicFieldCursorCodec.TryRead(ref platform, instance,
				MuiExternalDtpicField.PictureObject, out raw)) return false;
		value.PictureObject = APTR.FromPointer(raw);
		return MuiExternalDtpicFieldCursorCodec.TryRead(ref platform, instance,
			MuiExternalDtpicField.Alpha, out value.Alpha) &&
			MuiExternalDtpicFieldCursorCodec.TryRead(ref platform, instance,
				MuiExternalDtpicField.MinWidth, out value.MinWidth) &&
			MuiExternalDtpicFieldCursorCodec.TryRead(ref platform, instance,
				MuiExternalDtpicField.MinHeight, out value.MinHeight) &&
			MuiExternalDtpicFieldCursorCodec.TryRead(ref platform, instance,
				MuiExternalDtpicField.PicWidth, out value.PicWidth) &&
			MuiExternalDtpicFieldCursorCodec.TryRead(ref platform, instance,
				MuiExternalDtpicField.PicHeight, out value.PicHeight);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalDtpicState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryAddress(ref platform, instance, out _)) return false;
		return MuiExternalDtpicFieldCursorCodec.TryWrite(ref platform, instance,
			MuiExternalDtpicField.CallerName, value.CallerName.Raw) &&
			MuiExternalDtpicFieldCursorCodec.TryWrite(ref platform, instance,
				MuiExternalDtpicField.OwnedName, value.OwnedName.Raw) &&
			MuiExternalDtpicFieldCursorCodec.TryWrite(ref platform, instance,
				MuiExternalDtpicField.OwnedNameSize, value.OwnedNameSize) &&
			MuiExternalDtpicFieldCursorCodec.TryWrite(ref platform, instance,
				MuiExternalDtpicField.PictureObject, value.PictureObject.Raw) &&
			MuiExternalDtpicFieldCursorCodec.TryWrite(ref platform, instance,
				MuiExternalDtpicField.Alpha, value.Alpha) &&
			MuiExternalDtpicFieldCursorCodec.TryWrite(ref platform, instance,
				MuiExternalDtpicField.MinWidth, value.MinWidth) &&
			MuiExternalDtpicFieldCursorCodec.TryWrite(ref platform, instance,
				MuiExternalDtpicField.MinHeight, value.MinHeight) &&
			MuiExternalDtpicFieldCursorCodec.TryWrite(ref platform, instance,
				MuiExternalDtpicField.PicWidth, value.PicWidth) &&
			MuiExternalDtpicFieldCursorCodec.TryWrite(ref platform, instance,
				MuiExternalDtpicField.PicHeight, value.PicHeight);
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

internal static class MuiExternalDtpicFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalDtpicFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Field)
		{
			case MuiExternalDtpicField.CallerName:
				offset = 0;
				break;
			case MuiExternalDtpicField.OwnedName:
				offset = 4;
				break;
			case MuiExternalDtpicField.OwnedNameSize:
				offset = 8;
				break;
			case MuiExternalDtpicField.PictureObject:
				offset = 12;
				break;
			case MuiExternalDtpicField.Alpha:
				offset = 16;
				break;
			case MuiExternalDtpicField.MinWidth:
				offset = 20;
				break;
			case MuiExternalDtpicField.MinHeight:
				offset = 24;
				break;
			case MuiExternalDtpicField.PicWidth:
				offset = 28;
				break;
			case MuiExternalDtpicField.PicHeight:
				offset = 32;
				break;
			default:
				return false;
		}
		var region = default(MuiExternalStateCursor);
		region.Instance = cursor.Instance;
		region.Region = MuiExternalStateRegion.Dtpic;
		if (!MuiExternalStateCursorCodec.TryGetAddress(region,
			out var baseAddress) || baseAddress.Raw >
			uint.MaxValue - offset) return false;
		address = APTR.FromPointer(baseAddress.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalDtpicField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiExternalDtpicFieldCursor);
		cursor.Instance = instance;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalDtpicField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiExternalDtpicFieldCursor);
		cursor.Instance = instance;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// datatypes.library writes the laid-out picture dimensions into this compact
// caller-provided work record. Keep the result as a named guest struct so the
// Dtpic lifecycle does not depend on ad-hoc width/height offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalDtpicLayoutResult
{
	internal const uint Size = 8;

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

internal static class MuiExternalDtpicLayoutFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalDtpicLayoutFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Field)
		{
			case MuiExternalDtpicLayoutField.Width:
				offset = 0;
				break;
			case MuiExternalDtpicLayoutField.Height:
				offset = 4;
				break;
			default:
				return false;
		}
		if (cursor.Result.IsNull || cursor.Result.Raw >
			uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Result.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR result, MuiExternalDtpicLayoutField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiExternalDtpicLayoutFieldCursor);
		cursor.Result = result;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}
}

internal static class MuiExternalDtpicLayoutResultCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiExternalDtpicLayoutResult value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiExternalDtpicLayoutResult.Size)) return false;
		return MuiExternalDtpicLayoutFieldCursorCodec.TryRead(ref platform,
			address, MuiExternalDtpicLayoutField.Width, out value.Width) &&
			MuiExternalDtpicLayoutFieldCursorCodec.TryRead(ref platform, address,
				MuiExternalDtpicLayoutField.Height, out value.Height);
	}
}

// The most recent notification and its monotonic count are one shared guest
// record. Queries and notification recording consume this named state rather
// than repeating the three private offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalNotificationState
{
	internal const uint Size = 12;

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
		var cursor = default(MuiExternalStateCursor);
		cursor.Instance = instance;
		cursor.Region = MuiExternalStateRegion.Notification;
		if (!MuiExternalStateCursorCodec.TryGetAddress(cursor, out address))
			return false;
		return platform.IsMapped(address, MuiExternalNotificationState.Size);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, out MuiExternalNotificationState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryAddress(ref platform, instance, out _)) return false;
		return MuiExternalNotificationFieldCursorCodec.TryRead(ref platform,
			instance, MuiExternalNotificationField.Attribute, out value.Attribute) &&
			MuiExternalNotificationFieldCursorCodec.TryRead(ref platform, instance,
				MuiExternalNotificationField.Value, out value.Value) &&
			MuiExternalNotificationFieldCursorCodec.TryRead(ref platform, instance,
				MuiExternalNotificationField.Count, out value.Count);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalNotificationState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryAddress(ref platform, instance, out _)) return false;
		return MuiExternalNotificationFieldCursorCodec.TryWrite(ref platform,
			instance, MuiExternalNotificationField.Attribute, value.Attribute) &&
			MuiExternalNotificationFieldCursorCodec.TryWrite(ref platform, instance,
				MuiExternalNotificationField.Value, value.Value) &&
			MuiExternalNotificationFieldCursorCodec.TryWrite(ref platform, instance,
				MuiExternalNotificationField.Count, value.Count);
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

internal static class MuiExternalNotificationFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalNotificationFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Field)
		{
			case MuiExternalNotificationField.Attribute:
				offset = 0;
				break;
			case MuiExternalNotificationField.Value:
				offset = 4;
				break;
			case MuiExternalNotificationField.Count:
				offset = 8;
				break;
			default:
				return false;
		}
		var region = default(MuiExternalStateCursor);
		region.Instance = cursor.Instance;
		region.Region = MuiExternalStateRegion.Notification;
		if (!MuiExternalStateCursorCodec.TryGetAddress(region,
			out var baseAddress) || baseAddress.Raw >
			uint.MaxValue - offset) return false;
		address = APTR.FromPointer(baseAddress.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalNotificationField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiExternalNotificationFieldCursor);
		cursor.Instance = instance;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR instance, MuiExternalNotificationField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiExternalNotificationFieldCursor);
		cursor.Instance = instance;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
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

internal static class MuiExternalDisplayEnvironmentFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalDisplayEnvironmentFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Field)
		{
			case MuiExternalDisplayEnvironmentField.Window:
				offset = 0;
				break;
			case MuiExternalDisplayEnvironmentField.Screen:
				offset = 4;
				break;
			case MuiExternalDisplayEnvironmentField.DrawInfo:
				offset = 8;
				break;
			default:
				return false;
		}
		if (cursor.Environment.IsNull || cursor.Environment.Raw >
			uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Environment.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR environment, MuiExternalDisplayEnvironmentField field,
		out APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = APTR.Null;
		var cursor = default(MuiExternalDisplayEnvironmentFieldCursor);
		cursor.Environment = environment;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = APTR.FromPointer(platform.ReadUInt32(address, 0));
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR environment, MuiExternalDisplayEnvironmentField field,
		APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiExternalDisplayEnvironmentFieldCursor);
		cursor.Environment = environment;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value.Raw);
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalRastPortSlot
{
	internal const uint Size = 4;

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

internal static class MuiExternalRastPortSlotFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalRastPortSlotFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Field != MuiExternalRastPortSlotField.RastPort ||
			cursor.Slot.IsNull) return false;
		address = cursor.Slot;
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR slot, MuiExternalRastPortSlotField field, out APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = APTR.Null;
		var cursor = default(MuiExternalRastPortSlotFieldCursor);
		cursor.Slot = slot;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = APTR.FromPointer(platform.ReadUInt32(address, 0));
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR slot, MuiExternalRastPortSlotField field, APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiExternalRastPortSlotFieldCursor);
		cursor.Slot = slot;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value.Raw);
		return true;
	}
}

internal static class MuiExternalDisplayEnvironmentCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiExternalDisplayEnvironmentRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiExternalDisplayEnvironmentRecord.Size)) return false;
		return MuiExternalDisplayEnvironmentFieldCursorCodec.TryRead(ref platform,
			address, MuiExternalDisplayEnvironmentField.Window, out value.Window) &&
			MuiExternalDisplayEnvironmentFieldCursorCodec.TryRead(ref platform,
				address, MuiExternalDisplayEnvironmentField.Screen, out value.Screen) &&
			MuiExternalDisplayEnvironmentFieldCursorCodec.TryRead(ref platform,
				address, MuiExternalDisplayEnvironmentField.DrawInfo,
				out value.DrawInfo);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalDisplayEnvironmentRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiExternalDisplayEnvironmentRecord.Size)) return false;
		return MuiExternalDisplayEnvironmentFieldCursorCodec.TryWrite(ref platform,
			address, MuiExternalDisplayEnvironmentField.Window, value.Window) &&
			MuiExternalDisplayEnvironmentFieldCursorCodec.TryWrite(ref platform,
				address, MuiExternalDisplayEnvironmentField.Screen, value.Screen) &&
			MuiExternalDisplayEnvironmentFieldCursorCodec.TryWrite(ref platform,
				address, MuiExternalDisplayEnvironmentField.DrawInfo,
				value.DrawInfo);
	}
}

internal static class MuiExternalRastPortSlotCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiExternalRastPortSlot value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiExternalRastPortSlot.Size)) return false;
		return MuiExternalRastPortSlotFieldCursorCodec.TryRead(ref platform,
			address, MuiExternalRastPortSlotField.RastPort, out value.RastPort);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalRastPortSlot value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiExternalRastPortSlot.Size)) return false;
		return MuiExternalRastPortSlotFieldCursorCodec.TryWrite(ref platform,
			address, MuiExternalRastPortSlotField.RastPort, value.RastPort);
	}
}

internal static class MuiExternalDisplayStateCodec
{
	private static bool TryAddresses<TPlatform>(ref TPlatform platform,
		APTR instance, out APTR environment, out APTR rastPort)
		where TPlatform : struct, IMuiGuestMemory
	{
		environment = APTR.Null;
		rastPort = APTR.Null;
		var environmentCursor = default(MuiExternalStateCursor);
		environmentCursor.Instance = instance;
		environmentCursor.Region = MuiExternalStateRegion.DisplayEnvironment;
		var rastPortCursor = default(MuiExternalStateCursor);
		rastPortCursor.Instance = instance;
		rastPortCursor.Region = MuiExternalStateRegion.RastPort;
		if (!MuiExternalStateCursorCodec.TryGetAddress(environmentCursor,
			out environment) || !MuiExternalStateCursorCodec.TryGetAddress(
			rastPortCursor, out rastPort)) return false;
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

internal static class MuiExternalRenderInfoFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalRenderInfoFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Field)
		{
			case MuiExternalRenderInfoField.Screen:
				offset = unchecked((uint)MuiExternalWrapperLayout.RiScreen);
				break;
			case MuiExternalRenderInfoField.Window:
				offset = unchecked((uint)MuiExternalWrapperLayout.RiWindow);
				break;
			case MuiExternalRenderInfoField.DrawInfo:
				offset = unchecked((uint)MuiExternalWrapperLayout.RiDrawInfo);
				break;
			case MuiExternalRenderInfoField.RastPort:
				offset = unchecked((uint)MuiExternalWrapperLayout.RiRastPort);
				break;
			default:
				return false;
		}
		if (cursor.RenderInfo.IsNull || cursor.RenderInfo.Raw >
			uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.RenderInfo.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR renderInfo, MuiExternalRenderInfoField field, out APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = APTR.Null;
		var cursor = default(MuiExternalRenderInfoFieldCursor);
		cursor.RenderInfo = renderInfo;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = APTR.FromPointer(platform.ReadUInt32(address, 0));
		return true;
	}
}

internal static class MuiExternalRenderInfoCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiExternalRenderInfoRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiExternalRenderInfoRecord.Size)) return false;
		return MuiExternalRenderInfoFieldCursorCodec.TryRead(ref platform,
			address, MuiExternalRenderInfoField.Screen, out value.Screen) &&
			MuiExternalRenderInfoFieldCursorCodec.TryRead(ref platform, address,
				MuiExternalRenderInfoField.Window, out value.Window) &&
			MuiExternalRenderInfoFieldCursorCodec.TryRead(ref platform, address,
				MuiExternalRenderInfoField.DrawInfo, out value.DrawInfo) &&
			MuiExternalRenderInfoFieldCursorCodec.TryRead(ref platform, address,
				MuiExternalRenderInfoField.RastPort, out value.RastPort);
	}
}

// The first three words of every external-wrapper instance form one stable
// guest-owned header. Keep the class discriminator and flag word together so
// lifecycle code does not scatter private header offsets through the family.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalWrapperHeader
{
	internal const uint Size = 12;
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

internal static class MuiExternalWrapperHeaderFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalWrapperHeaderFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Field)
		{
			case MuiExternalWrapperHeaderField.Magic:
				offset = 0;
				break;
			case MuiExternalWrapperHeaderField.Class:
				offset = 4;
				break;
			case MuiExternalWrapperHeaderField.Flags:
				offset = 8;
				break;
			default:
				return false;
		}
		if (cursor.Header.IsNull || cursor.Header.Raw >
			uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Header.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		MuiExternalWrapperHeaderFieldCursor cursor, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		MuiExternalWrapperHeaderFieldCursor cursor, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiExternalWrapperHeaderCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiExternalWrapperHeader value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiExternalWrapperHeader.Size)) return false;
		var magicCursor = default(MuiExternalWrapperHeaderFieldCursor);
		magicCursor.Header = address;
		magicCursor.Field = MuiExternalWrapperHeaderField.Magic;
		var classCursor = default(MuiExternalWrapperHeaderFieldCursor);
		classCursor.Header = address;
		classCursor.Field = MuiExternalWrapperHeaderField.Class;
		var flagsCursor = default(MuiExternalWrapperHeaderFieldCursor);
		flagsCursor.Header = address;
		flagsCursor.Field = MuiExternalWrapperHeaderField.Flags;
		if (!MuiExternalWrapperHeaderFieldCursorCodec.TryRead(ref platform,
			magicCursor, out var magic) || magic !=
			MuiExternalWrapperHeader.Cookie ||
			!MuiExternalWrapperHeaderFieldCursorCodec.TryRead(ref platform,
				classCursor, out var cls) ||
			!MuiExternalWrapperHeaderFieldCursorCodec.TryRead(ref platform,
				flagsCursor, out var flags)) return false;
		value.Magic = MuiExternalWrapperHeader.Cookie;
		value.Class = (MuiExternalWrapperClass)cls;
		value.Flags = flags;
		return true;
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
		var magicCursor = default(MuiExternalWrapperHeaderFieldCursor);
		magicCursor.Header = address;
		magicCursor.Field = MuiExternalWrapperHeaderField.Magic;
		var classCursor = default(MuiExternalWrapperHeaderFieldCursor);
		classCursor.Header = address;
		classCursor.Field = MuiExternalWrapperHeaderField.Class;
		var flagsCursor = default(MuiExternalWrapperHeaderFieldCursor);
		flagsCursor.Header = address;
		flagsCursor.Field = MuiExternalWrapperHeaderField.Flags;
		return MuiExternalWrapperHeaderFieldCursorCodec.TryWrite(ref platform,
			magicCursor, value.Magic) &&
			MuiExternalWrapperHeaderFieldCursorCodec.TryWrite(ref platform,
				classCursor, (uint)value.Class) &&
			MuiExternalWrapperHeaderFieldCursorCodec.TryWrite(ref platform,
				flagsCursor, value.Flags);
	}
}

// The wrapper class discriminator. Both classes inherit from Area.
public enum MuiExternalWrapperClass : uint
{
	None = 0,
	Boopsi = 1,   // Boopsi.mui : Area
	Dtpic = 2,    // Dtpic.mui  : Area
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

internal static class MuiExternalBoopsiPacketFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalBoopsiPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint size;
		uint offset;
		switch (cursor.Kind)
		{
			case MuiExternalBoopsiPacketKind.OpSet:
				size = MuiExternalBoopsiOpSetMessage.Size;
				switch (cursor.Field)
				{
					case MuiExternalBoopsiPacketField.MethodId:
						offset = 0;
						break;
					case MuiExternalBoopsiPacketField.AttributeList:
						offset = 4;
						break;
					case MuiExternalBoopsiPacketField.GadgetInfo:
						offset = 8;
						break;
					default:
						return false;
				}
				break;
			case MuiExternalBoopsiPacketKind.OpGet:
				size = MuiExternalBoopsiOpGetMessage.Size;
				switch (cursor.Field)
				{
					case MuiExternalBoopsiPacketField.MethodId:
						offset = 0;
						break;
					case MuiExternalBoopsiPacketField.Attribute:
						offset = 4;
						break;
					case MuiExternalBoopsiPacketField.Storage:
						offset = 8;
						break;
					default:
						return false;
				}
				break;
			case MuiExternalBoopsiPacketKind.Render:
				size = MuiExternalBoopsiRenderMessage.Size;
				switch (cursor.Field)
				{
					case MuiExternalBoopsiPacketField.MethodId:
						offset = 0;
						break;
					case MuiExternalBoopsiPacketField.GadgetInfo:
						offset = 4;
						break;
					case MuiExternalBoopsiPacketField.RastPort:
						offset = 8;
						break;
					default:
						return false;
				}
				break;
			case MuiExternalBoopsiPacketKind.Tag:
				size = MuiExternalBoopsiTagItem.Size;
				switch (cursor.Field)
				{
					case MuiExternalBoopsiPacketField.Tag:
						offset = 0;
						break;
					case MuiExternalBoopsiPacketField.Data:
						offset = 4;
						break;
					default:
						return false;
				}
				break;
			case MuiExternalBoopsiPacketKind.Result:
				size = MuiExternalBoopsiResultWord.Size;
				if (cursor.Field != MuiExternalBoopsiPacketField.Value)
					return false;
				offset = 0;
				break;
			default:
				return false;
		}
		if (cursor.Packet.IsNull || !platform.IsMapped(cursor.Packet, size) ||
			cursor.Packet.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Packet.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		MuiExternalBoopsiPacketFieldCursor cursor, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		MuiExternalBoopsiPacketFieldCursor cursor, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalTagListCursor
{
	internal const uint EntrySize = MuiAslTagItemRecord.Size;
	internal const uint MaximumEntries = MuiExternalWrapperLayout.MaxTagWalk;
	internal APTR Base;
	internal uint Index;
}

internal static class MuiExternalTagListCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiExternalTagListCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >=
			MuiExternalTagListCursor.MaximumEntries || cursor.Index >
			(uint.MaxValue - cursor.Base.Raw) /
			MuiExternalTagListCursor.EntrySize) return false;
		var offset = cursor.Index * MuiExternalTagListCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address, MuiExternalTagListCursor.EntrySize);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalRememberCursor
{
	internal const uint EntrySize = MuiAslTagItemRecord.Size;
	internal const uint MaximumEntries = MuiExternalWrapperLayout.MaxRemember;
	internal APTR Base;
	internal uint Index;
}

internal static class MuiExternalRememberCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiExternalRememberCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >=
			MuiExternalRememberCursor.MaximumEntries || cursor.Index >
			(uint.MaxValue - cursor.Base.Raw) /
			MuiExternalRememberCursor.EntrySize) return false;
		var offset = cursor.Index * MuiExternalRememberCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address, MuiExternalRememberCursor.EntrySize);
	}
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

internal static class MuiExternalBoopsiTagCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiExternalBoopsiTagCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >=
			MuiExternalBoopsiTagCursor.MaximumEntries || cursor.Index >
			(uint.MaxValue - cursor.Base.Raw) /
			MuiExternalBoopsiTagCursor.EntrySize) return false;
		var offset = cursor.Index * MuiExternalBoopsiTagCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address, MuiExternalBoopsiTagCursor.EntrySize);
	}
}

// The Boopsi work block carries its operation packet at the base and a
// caller-facing inline TagItem/result area at one fixed semantic region. Keep
// that region boundary in a named cursor so packet code does not repeat raw
// work-buffer arithmetic.
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

internal static class MuiExternalWorkRegionCursorCodec
{
	internal const uint InlineOffset = 16;
	internal const uint InlineBytes = 40;

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalWorkRegionCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Work.IsNull || !platform.IsMapped(cursor.Work,
			MuiExternalWrapperLayout.WorkSize)) return false;
		uint offset;
		switch (cursor.Region)
		{
			case MuiExternalWorkRegion.InlineTagList:
			case MuiExternalWorkRegion.InlineResult:
				offset = InlineOffset;
				break;
			default:
				return false;
		}
		if (cursor.Work.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Work.Raw + offset);
		return platform.IsMapped(address, InlineBytes);
	}
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
	internal const uint InlineOffset = MuiExternalWorkRegionCursorCodec.InlineOffset;
	internal const uint InlineTagBytes = MuiExternalWorkRegionCursorCodec.InlineBytes;

	internal static bool TryGetInlineTagList<TPlatform>(
		ref TPlatform platform, APTR work, out APTR list)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiExternalWorkRegionCursor);
		cursor.Work = work;
		cursor.Region = MuiExternalWorkRegion.InlineTagList;
		return MuiExternalWorkRegionCursorCodec.TryGetAddress(ref platform,
			cursor, out list);
	}

	internal static bool TryGetInlineResult<TPlatform>(
		ref TPlatform platform, APTR work, out APTR storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		storage = APTR.Null;
		if (!TryGetInlineTagList(ref platform, work, out var list)) return false;
		storage = list;
		return platform.IsMapped(storage, MuiExternalBoopsiResultWord.Size);
	}

	private static bool WriteField<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalBoopsiPacketKind kind,
		MuiExternalBoopsiPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiExternalBoopsiPacketFieldCursor);
		cursor.Packet = address;
		cursor.Kind = kind;
		cursor.Field = field;
		return MuiExternalBoopsiPacketFieldCursorCodec.TryWrite(ref platform,
			cursor, value);
	}

	private static bool ReadField<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalBoopsiPacketKind kind,
		MuiExternalBoopsiPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiExternalBoopsiPacketFieldCursor);
		cursor.Packet = address;
		cursor.Kind = kind;
		cursor.Field = field;
		return MuiExternalBoopsiPacketFieldCursorCodec.TryRead(ref platform,
			cursor, out value);
	}

	internal static bool WriteOpSet<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalBoopsiOpSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (packet.MethodId != OmSet) return false;
		return WriteField(ref platform, address,
			MuiExternalBoopsiPacketKind.OpSet,
			MuiExternalBoopsiPacketField.MethodId, packet.MethodId) &&
			WriteField(ref platform, address, MuiExternalBoopsiPacketKind.OpSet,
				MuiExternalBoopsiPacketField.AttributeList,
				packet.AttributeList.Raw) &&
			WriteField(ref platform, address, MuiExternalBoopsiPacketKind.OpSet,
				MuiExternalBoopsiPacketField.GadgetInfo, packet.GadgetInfo.Raw);
	}

	internal static bool WriteOpGet<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalBoopsiOpGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (packet.MethodId != OmGet) return false;
		return WriteField(ref platform, address,
			MuiExternalBoopsiPacketKind.OpGet,
			MuiExternalBoopsiPacketField.MethodId, packet.MethodId) &&
			WriteField(ref platform, address, MuiExternalBoopsiPacketKind.OpGet,
				MuiExternalBoopsiPacketField.Attribute, packet.Attribute) &&
			WriteField(ref platform, address, MuiExternalBoopsiPacketKind.OpGet,
				MuiExternalBoopsiPacketField.Storage, packet.Storage.Raw);
	}

	internal static bool WriteRender<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalBoopsiRenderMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (packet.MethodId != GmRender) return false;
		return WriteField(ref platform, address,
			MuiExternalBoopsiPacketKind.Render,
			MuiExternalBoopsiPacketField.MethodId, packet.MethodId) &&
			WriteField(ref platform, address, MuiExternalBoopsiPacketKind.Render,
				MuiExternalBoopsiPacketField.GadgetInfo, packet.GadgetInfo.Raw) &&
			WriteField(ref platform, address, MuiExternalBoopsiPacketKind.Render,
				MuiExternalBoopsiPacketField.RastPort, packet.RastPort.Raw);
	}

	internal static bool WriteTag<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalBoopsiTagItem packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return WriteField(ref platform, address, MuiExternalBoopsiPacketKind.Tag,
			MuiExternalBoopsiPacketField.Tag, packet.Tag) &&
			WriteField(ref platform, address, MuiExternalBoopsiPacketKind.Tag,
				MuiExternalBoopsiPacketField.Data, packet.Data);
	}

	internal static bool WriteResult<TPlatform>(ref TPlatform platform,
		APTR address, MuiExternalBoopsiResultWord packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return WriteField(ref platform, address,
			MuiExternalBoopsiPacketKind.Result,
			MuiExternalBoopsiPacketField.Value, packet.Value);
	}

	internal static bool TryReadResult<TPlatform>(ref TPlatform platform,
		APTR address, out MuiExternalBoopsiResultWord packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!ReadField(ref platform, address,
			MuiExternalBoopsiPacketKind.Result,
			MuiExternalBoopsiPacketField.Value, out packet.Value)) return false;
		return true;
	}
}

public static class MuiExternalWrapperCore
{
	// ---- Classification ------------------------------------------------------

	// Classify a guest C-string class id against the exact official names. The
	// loader contract is case-sensitive, so the match is byte-exact against the
	// documented "Boopsi.mui" / "Dtpic.mui" ids. Freestanding: the expected
	// names are compared as ASCII byte literals with no managed strings.
	public static MuiExternalWrapperClass ClassifyName<TPlatform>(
		ref TPlatform platform, APTR classId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (classId.IsNull) return MuiExternalWrapperClass.None;
		var c0 = B(ref platform, classId, 0);
		// Boopsi.mui
		if (c0 == 'B' && B(ref platform, classId, 1) == 'o' &&
			B(ref platform, classId, 2) == 'o' &&
			B(ref platform, classId, 3) == 'p' &&
			B(ref platform, classId, 4) == 's' &&
			B(ref platform, classId, 5) == 'i' && Suffix(ref platform, classId, 6))
			return MuiExternalWrapperClass.Boopsi;
		// Dtpic.mui
		if (c0 == 'D' && B(ref platform, classId, 1) == 't' &&
			B(ref platform, classId, 2) == 'p' &&
			B(ref platform, classId, 3) == 'i' &&
			B(ref platform, classId, 4) == 'c' && Suffix(ref platform, classId, 5))
			return MuiExternalWrapperClass.Dtpic;
		return MuiExternalWrapperClass.None;
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
		var cursor = default(MuiExternalTagListCursor);
		cursor.Base = tags;

		for (var index = 0; index < MuiExternalWrapperLayout.MaxTagWalk; index++)
		{
			cursor.Index = unchecked((uint)index);
			if (!MuiExternalTagListCursorCodec.TryGetEntry(ref platform, cursor,
				out var address)) break;
			if (!MuiAslTagItemCodec.TryRead(ref platform, address,
				out var item)) break;
			if (item.Tag == MuiAslTagListCore.TagDone) break;
			var value = item.Data;
			if (tagWindow != 0 && item.Tag == tagWindow) value = window;
			else if (tagScreen != 0 && item.Tag == tagScreen) value = screen;
			else if (tagDrawInfo != 0 && item.Tag == tagDrawInfo) value = drawInfo;
			if (value != item.Data)
			{
				item.Data = value;
				if (!MuiAslTagItemCodec.Write(ref platform, address, item)) break;
			}
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
		var cursor = default(MuiExternalRememberCursor);
		cursor.Base = buffer;
		cursor.Index = count;
		if (!MuiExternalRememberCursorCodec.TryGetEntry(ref platform, cursor,
			out var address)) return false;
		var item = default(MuiAslTagItemRecord);
		item.Tag = tag;
		if (!MuiAslTagItemCodec.Write(ref platform, address, item)) return false;
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
		for (var index = 0u; index < count; index++)
		{
			cursor.Index = index;
			if (!MuiExternalRememberCursorCodec.TryGetEntry(ref platform, cursor,
				out var address)) return;
			if (!MuiAslTagItemCodec.TryRead(ref platform, address,
				out var item)) return;
			item.Data = BoopsiGet(ref platform, instance, obj, item.Tag);
			if (!MuiAslTagItemCodec.Write(ref platform, address, item)) return;
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
		for (var index = 0u; index < count; index++)
		{
			cursor.Index = index;
			if (!MuiExternalRememberCursorCodec.TryGetEntry(ref platform, cursor,
				out var address)) return;
			if (!MuiAslTagItemCodec.TryRead(ref platform, address,
				out var item)) return;
			BoopsiSet(ref platform, instance, obj, item.Tag, item.Data);
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
			var copy = Alloc(ref platform, (uint)length + 1);
			if (copy.IsNull) return false;   // atomic: leaves no owned name
			for (var index = 0; index < length; index++)
				platform.WriteUInt8(copy, index, platform.ReadUInt8(name, index));
			platform.WriteUInt8(copy, length, 0);
			dtpic.OwnedName = copy;
			dtpic.OwnedNameSize = (uint)length + 1;
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
		var length = 0;
		while (length < MuiExternalWrapperLayout.MaxNameLength)
		{
			if (!platform.IsMapped(text, (uint)length + 1)) break;
			if (platform.ReadUInt8(text, length) == 0) break;
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
		var tag = default(MuiExternalBoopsiTagItem);
		var cursor = default(MuiExternalBoopsiTagCursor);
		cursor.Base = list;
		tag.Tag = GaLeft;
		tag.Data = unchecked((uint)left);
		cursor.Index = 0;
		if (!MuiExternalBoopsiTagCursorCodec.TryGetEntry(ref platform, cursor,
			out var address) || !MuiExternalBoopsiPacketCodec.WriteTag(ref platform,
			address, tag))
			return false;
		tag.Tag = GaTop;
		tag.Data = unchecked((uint)top);
		cursor.Index = 1;
		if (!MuiExternalBoopsiTagCursorCodec.TryGetEntry(ref platform, cursor,
			out address) || !MuiExternalBoopsiPacketCodec.WriteTag(ref platform,
			address, tag)) return false;
		tag.Tag = GaWidth;
		tag.Data = unchecked((uint)appliedWidth);
		cursor.Index = 2;
		if (!MuiExternalBoopsiTagCursorCodec.TryGetEntry(ref platform, cursor,
			out address) || !MuiExternalBoopsiPacketCodec.WriteTag(ref platform,
			address, tag)) return false;
		tag.Tag = GaHeight;
		tag.Data = unchecked((uint)appliedHeight);
		cursor.Index = 3;
		if (!MuiExternalBoopsiTagCursorCodec.TryGetEntry(ref platform, cursor,
			out address) || !MuiExternalBoopsiPacketCodec.WriteTag(ref platform,
			address, tag)) return false;
		tag.Tag = 0; // TAG_DONE
		tag.Data = 0;
		cursor.Index = 4;
		if (!MuiExternalBoopsiTagCursorCodec.TryGetEntry(ref platform, cursor,
			out address) || !MuiExternalBoopsiPacketCodec.WriteTag(ref platform,
			address, tag)) return false;
		platform.DoMethod(obj, work);
		SetFlag(ref platform, instance, MuiExternalWrapperLayout.FlagRedraw, true);
		return true;
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
		var cursor = default(MuiExternalTagListCursor);
		cursor.Base = attrList;
		for (var index = 0; index < MuiExternalWrapperLayout.MaxTagWalk; index++)
		{
			cursor.Index = unchecked((uint)index);
			if (!MuiExternalTagListCursorCodec.TryGetEntry(ref platform, cursor,
				out var address)) break;
			if (!MuiAslTagItemCodec.TryRead(ref platform, address,
				out var item)) break;
			if (item.Tag == MuiAslTagListCore.TagDone) break;
			RecordNotify(ref platform, instance, item.Tag, item.Data);
			mapped++;
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
		var tag = default(MuiExternalBoopsiTagItem);
		tag.Tag = attribute;
		tag.Data = value;
		var cursor = default(MuiExternalBoopsiTagCursor);
		cursor.Base = list;
		cursor.Index = 0;
		if (!MuiExternalBoopsiTagCursorCodec.TryGetEntry(ref platform, cursor,
			out var address) || !MuiExternalBoopsiPacketCodec.WriteTag(ref platform,
			address, tag)) return;
		cursor.Index = 1;
		if (!MuiExternalBoopsiTagCursorCodec.TryGetEntry(ref platform, cursor,
			out address) || !MuiExternalBoopsiPacketCodec.WriteTag(ref platform,
			address, default)) return;
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
		platform.IsMapped(text, (uint)index + 1) &&
		platform.ReadUInt8(text, index) == expected;

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
		var flags = ReadFlags(ref platform, instance);
		var updated = set ? flags | bit : flags & ~bit;
		if (updated == flags) return false;
		if (!MuiExternalWrapperHeaderCodec.TryRead(ref platform, instance,
			out var header)) return false;
		header.Flags = updated;
		if (!MuiExternalWrapperHeaderCodec.Write(ref platform, instance, header))
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
