/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Owned scratch records for one synchronous Intuition EasyRequestArgs call.
// Addresses are carried by name so preparation, dispatch and cleanup share a
// single typed lifetime record.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeRequesterPreparationRecord
{
	internal APTR EasyStruct;
	internal APTR ActiveButtonTags;
	internal MuiRequesterSystemGadgetPlan Gadgets;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeEasyRequestCallRecord
{
	internal APTR Window;
	internal APTR EasyStruct;
	internal APTR Idcmp;
	internal APTR Arguments;
}

internal static class MuiNativeRequesterPreparationCore
{
	internal static bool TryPrepare<TPlatform>(ref TPlatform platform,
		APTR title, APTR gadgets, APTR textFormat,
		out MuiNativeRequesterPreparationRecord preparation)
		where TPlatform : struct, IMuiAllocationPlatform
	{
		preparation = default;
		if (!MuiRequesterSystemGadgetCore.TryPrepare(ref platform, gadgets,
			out preparation.Gadgets)) return false;

		if (preparation.Gadgets.HasActiveButton != 0)
		{
			preparation.ActiveButtonTags = platform.Allocate(
				MuiRequesterActiveButtonTagListRecord.Size,
				MuiHeadlessLayout.AllocationFlags);
			if (preparation.ActiveButtonTags.IsNull || !platform.IsMapped(
				preparation.ActiveButtonTags,
				MuiRequesterActiveButtonTagListRecord.Size))
			{
				Release(ref platform, ref preparation);
				return false;
			}
			var tags = new MuiRequesterActiveButtonTagListRecord
			{
				ActiveButton = TagItem.Create(
					MuiRequesterActiveButtonTagListCodec.ActiveButtonTag,
					preparation.Gadgets.ActiveButton),
				Terminator = TagItem.Done,
			};
			if (!MuiRequesterActiveButtonTagListCodec.Write(ref platform,
				preparation.ActiveButtonTags, tags))
			{
				Release(ref platform, ref preparation);
				return false;
			}
		}

		preparation.EasyStruct = platform.Allocate(
			MuiNativeExtendedEasyStructRecord.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (preparation.EasyStruct.IsNull || !platform.IsMapped(
			preparation.EasyStruct, MuiNativeExtendedEasyStructRecord.Size))
		{
			Release(ref platform, ref preparation);
			return false;
		}
		var easy = new MuiNativeExtendedEasyStructRecord
		{
			StructureSize = MuiNativeExtendedEasyStructRecord.Size,
			Title = title,
			TextFormat = textFormat,
			GadgetFormat = preparation.Gadgets.GadgetFormat,
			Tags = preparation.ActiveButtonTags,
		};
		if (!MuiNativeExtendedEasyStructCodec.Write(ref platform,
			preparation.EasyStruct, easy))
		{
			Release(ref platform, ref preparation);
			return false;
		}
		return true;
	}

	internal static MuiNativeEasyRequestCallRecord CreateCall(
		MuiNativeRequesterPreparationRecord preparation, APTR window)
		=> new()
		{
			Window = window,
			EasyStruct = preparation.EasyStruct,
			Idcmp = APTR.Null,
			Arguments = APTR.Null,
		};

	internal static void Release<TPlatform>(ref TPlatform platform,
		ref MuiNativeRequesterPreparationRecord preparation)
		where TPlatform : struct, IMuiAllocationPlatform
	{
		if (preparation.EasyStruct.IsNotNull)
		{
			platform.Free(preparation.EasyStruct,
				MuiNativeExtendedEasyStructRecord.Size);
			preparation.EasyStruct = APTR.Null;
		}
		if (preparation.ActiveButtonTags.IsNotNull)
		{
			platform.Free(preparation.ActiveButtonTags,
				MuiRequesterActiveButtonTagListRecord.Size);
			preparation.ActiveButtonTags = APTR.Null;
		}
		MuiRequesterSystemGadgetCore.Release(ref platform,
			preparation.Gadgets);
		preparation.Gadgets = default;
	}
}
