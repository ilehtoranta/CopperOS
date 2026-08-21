/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Generic MUI object metadata is held in the named headless object/class
// records: ObjectId/UserData are scalar fields, Parent is a guest object link,
// and Version/Revision come from the owning class metadata. Keep the public
// getter seam on those records so OM_GET never falls through to an unrelated
// raw attribute-list entry when explicit metadata exists.
internal static class MuiObjectMetadataCore
{
	internal const uint ObjectId = 0x8042D76E;
	internal const uint Parent = 0x8042E35F;
	internal const uint UserData = 0x80420313;
	internal const uint Revision = 0x80427EAA;
	internal const uint Version = 0x80422301;

	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == ObjectId || attribute == Parent || attribute == UserData ||
		attribute == Revision || attribute == Version;

	internal static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (!IsPublicGetterAttribute(attribute)) return false;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		if (attribute == Parent)
		{
			value = MuiHeadlessObjectCore.ParentObject(ref platform, state, obj).Raw;
			return true;
		}
		if (attribute == Revision || attribute == Version)
		{
			var classRecord = MuiHeadlessObjectCore.ObjectClassRecord(ref platform,
				state, obj);
			if (!MuiHeadlessObjectCore.TryGetClassVersionRevision(ref platform,
				classRecord, out var metadata)) return false;
			value = attribute == Version ? metadata.Version : metadata.Revision;
			return true;
		}
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return false;
		value = attribute == ObjectId ? objectValue.ObjectId :
			objectValue.UserData;
		return true;
	}
}
