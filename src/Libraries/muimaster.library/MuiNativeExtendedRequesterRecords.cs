/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS intuition/extensions.h ExtEasyStruct. The ABI's structure-size
// discriminator is required before Intuition will inspect the named tag list.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeExtendedEasyStructRecord
{
	internal const uint Size = 24;
	internal uint StructureSize;
	internal uint Flags;
	internal APTR Title;
	internal APTR TextFormat;
	internal APTR GadgetFormat;
	internal APTR Tags;
}

internal static class MuiNativeExtendedEasyStructCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiNativeExtendedEasyStructRecord record)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNativeExtendedEasyStructRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.StructureSize) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Flags) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Title.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.TextFormat.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.GadgetFormat.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Tags.Raw) &&
		MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiNativeExtendedEasyStructRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		uint title;
		uint textFormat;
		uint gadgetFormat;
		uint tags;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNativeExtendedEasyStructRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.StructureSize) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Flags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out title) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out textFormat) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out gadgetFormat) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out tags) || !MuiGuestStructCursor.IsComplete(cursor)) return false;
		record.Title = APTR.FromPointer(title);
		record.TextFormat = APTR.FromPointer(textFormat);
		record.GadgetFormat = APTR.FromPointer(gadgetFormat);
		record.Tags = APTR.FromPointer(tags);
		return true;
	}
}

// Two-item tag list consumed synchronously by ExtEasyStruct: the active-button
// extension and TAG_DONE. The SDK TagItem members remain the public typed form.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRequesterActiveButtonTagListRecord
{
	internal const uint Size = 16;
	internal TagItem ActiveButton;
	internal TagItem Terminator;
}

internal static class MuiRequesterActiveButtonTagListCodec
{
	internal const uint ActiveButtonTag = ExecConstants.TagUser + 1;

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiRequesterActiveButtonTagListRecord record)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiRequesterActiveButtonTagListRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.ActiveButton.Tag) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.ActiveButton.Data) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Terminator.Tag) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Terminator.Data) &&
		MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiRequesterActiveButtonTagListRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiRequesterActiveButtonTagListRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.ActiveButton.Tag) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.ActiveButton.Data) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Terminator.Tag) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Terminator.Data)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}
