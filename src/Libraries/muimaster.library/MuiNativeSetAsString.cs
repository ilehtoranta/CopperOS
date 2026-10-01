/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Native MUIM_SetAsString owns the formatted value for as long as the
// corresponding object attribute refers to it. All packet fields, attribute
// storage and allocation ownership remain guest-resident named records.
internal static class MuiNativeSetAsStringCore
{
	internal static bool Apply(ref MuiNativeClassPlatform platform,
		APTR sidecarAddress, MuiSetAsStringMessage packet, APTR parameters)
	{
		if (packet.Format.IsNull)
			return MuiNativeObjectStateCore.SetOwnedTextAttribute(ref platform,
				sidecarAddress, packet.Attribute, APTR.Null, 0, true);

		if (!MuiRequesterPayloadCore.TryGetFormatParameterCount(ref platform,
			packet.Format, out var argumentCount) ||
			argumentCount > MuiNotifySetAsStringCore.MaximumArguments ||
			!MuiRequesterFormatCore.TryMaterialize(ref platform, packet.Format,
				parameters, out var materialized, out var materializedSize))
			return false;

		if (!CStringCodec.TryReadLength(ref platform, materialized,
			MuiNotifySetAsStringCore.MaximumOutputLength + 1, out var length) ||
			length > MuiNotifySetAsStringCore.MaximumOutputLength)
		{
			if (materializedSize != 0)
				platform.Free(materialized, materializedSize);
			return false;
		}

		var ownedText = materialized;
		var ownedTextSize = materializedSize;
		if (ownedTextSize == 0)
		{
			ownedTextSize = length + 1;
			ownedText = platform.Allocate(ownedTextSize,
				MuiHeadlessLayout.AllocationFlags);
			if (ownedText.IsNull) return false;
			platform.Copy(materialized, ownedText, ownedTextSize);
		}

		if (MuiNativeObjectStateCore.SetOwnedTextAttribute(ref platform,
			sidecarAddress, packet.Attribute, ownedText, ownedTextSize, true))
			return true;

		platform.Free(ownedText, ownedTextSize);
		return false;
	}
}
