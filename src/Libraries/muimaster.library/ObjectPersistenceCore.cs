/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Bounded core for MorphOS MUIM_Export/MUIM_Import. MUI invokes these methods
// with one live dataspace Object* while walking the application tree. The
// The core enforces the MUIA_ObjectID contract, handles the bounded built-in
// scalar payloads, and delegates still-unimplemented class payloads to a
// narrow native capability seam.
public static class MuiObjectPersistenceCore
{
	private const uint ObjectIdAttribute = 0x8042D76E;
	private const uint NumericValue = 0x8042AE3A;
	private const uint RadioActive = 0x80429B41;
	private const uint CycleActive = 0x80421788;
	private const uint ListActive = 0x8042391C;
	private const uint MenuitemChecked = 0x8042562A;
	private const uint StringContents = 0x80428FFD;
	private const uint TextContents = 0x8042F8DC;
	private const uint Selected = 0x8042654B;
	private const uint GroupActivePage = 0x80424199;

	public static bool Export<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR dataspace) where TPlatform : struct, IMuiHeadlessPlatform =>
		Persist(ref platform, state, obj, dataspace, true);

	public static bool Import<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR dataspace) where TPlatform : struct, IMuiHeadlessPlatform =>
		Persist(ref platform, state, obj, dataspace, false);

	private static bool Persist<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR dataspace, bool export)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, dataspace).IsNull)
			return false;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			ObjectIdAttribute, out var objectId) || objectId == 0) return false;
		var payloadResult = TryClassPayload(ref platform, state, obj, dataspace,
			objectId, export, out var handled);
		if (handled) return payloadResult;
		return export ? platform.ExportMuiObject(obj, dataspace, objectId) :
			platform.ImportMuiObject(obj, dataspace, objectId);
	}

	// MorphOS's built-in persistence uses the object's ObjectID as a dataspace
	// key. Scalar classes store one ULONG; String/Text store a bounded NUL-
	// terminated guest blob. The object cores retain ownership of imported
	// strings, so the Dataspace remains a transport rather than object state.
	private static bool TryClassPayload<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR dataspace, uint objectId, bool export,
		out bool handled) where TPlatform : struct, IMuiHeadlessPlatform
	{
		handled = false;
		var attribute = 0u;
		var kind = 0u;
		var common = MuiCommonControlCore.Classify(ref platform, state, obj);
		if (MuiCommonControlCore.IsNumericClass(common))
		{
			attribute = NumericValue;
			kind = 1;
		}
		else if (common == MuiControlClass.Radio)
		{
			attribute = RadioActive;
			kind = 1;
		}
		else if (common == MuiControlClass.Cycle)
		{
			attribute = CycleActive;
			kind = 1;
		}
		else if (MuiListCore.Classify(ref platform, state, obj) ==
			MuiCollectionClass.List)
		{
			attribute = ListActive;
			kind = 2;
		}
		else if (MuiMenuSpecialistCore.Classify(ref platform, state, obj) ==
			MuiMenuSpecialistClass.Menuitem)
		{
			attribute = MenuitemChecked;
			kind = 3;
		}
		else if (common == MuiControlClass.String)
		{
			attribute = StringContents;
			kind = 4;
		}
		else if (common == MuiControlClass.Text)
		{
			attribute = TextContents;
			kind = 5;
		}
		else if (common == MuiControlClass.Image ||
			common == MuiControlClass.Gadget ||
			IsNamedClass(ref platform, state, obj, false))
		{
			attribute = Selected;
			kind = 1;
		}
		else if (IsNamedClass(ref platform, state, obj, true))
		{
			attribute = GroupActivePage;
			kind = 1;
		}
		else return false;

		handled = true;
		if (kind == 4 || kind == 5)
			return export ? ExportString(ref platform, state, obj, dataspace,
				objectId, attribute) : ImportString(ref platform, state, obj,
				dataspace, objectId, attribute);
		return export ? ExportScalar(ref platform, state, obj, dataspace,
			objectId, attribute) : ImportScalar(ref platform, state, obj, dataspace,
			objectId, attribute, kind);
	}

	// The headless registry keeps the public class name as a guest pointer. A
	// tiny case-insensitive name probe is enough to identify the base Area and
	// Group records without adding managed class metadata or a second registry.
	private static bool IsNamedClass<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, bool group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.ObjectClassRecord(ref platform, state, obj);
		if (record.IsNull) return false;
		if (!MuiHeadlessClassCodec.TryRead(ref platform, record,
			out var classValue)) return false;
		var name = classValue.Name;
		var length = group ? 9u : 8u;
		for (var index = 0u; index <= length; index++)
		{
			if (name.Raw > uint.MaxValue - index ||
				!platform.IsMapped(APTR.FromPointer(name.Raw + index), 1))
				return false;
			var actual = platform.ReadUInt8(name, (int)index);
			if (index == length) return actual == 0;
			var expected = group
				? index switch
				{
					0 => (byte)'g', 1 => (byte)'r', 2 => (byte)'o',
					3 => (byte)'u', 4 => (byte)'p', 5 => (byte)'.',
					6 => (byte)'m', 7 => (byte)'u', _ => (byte)'i'
				}
				: index switch
				{
					0 => (byte)'a', 1 => (byte)'r', 2 => (byte)'e',
					3 => (byte)'a', 4 => (byte)'.', 5 => (byte)'m',
					6 => (byte)'u', _ => (byte)'i'
				};
			if (actual >= (byte)'A' && actual <= (byte)'Z')
				actual = unchecked((byte)(actual + 32));
			if (actual != expected) return false;
		}
		return false;
	}

	private static bool ExportScalar<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR dataspace, uint objectId, uint attribute)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			attribute, out var value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform, 4);
		if (scratch.IsNull) return false;
		if (!MuiGuestUlongStorageCodec.WriteValue(ref platform, scratch, value))
		{
			platform.Clear(scratch, 4);
			platform.Free(scratch, 4);
			return false;
		}
		var result = MuiStoreCore.DataspaceAdd(ref platform, state, dataspace,
			objectId, scratch, 4);
		platform.Clear(scratch, 4);
		platform.Free(scratch, 4);
		return result;
	}

	private static bool ExportString<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR dataspace, uint objectId, uint attribute)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out var raw);
		if (raw == 0)
		{
			var empty = MuiHeadlessMemory.Allocate(ref platform, 1);
			if (empty.IsNull) return false;
			platform.WriteUInt8(empty, 0, 0);
			var result = MuiStoreCore.DataspaceAdd(ref platform, state, dataspace,
				objectId, empty, 1);
			platform.Clear(empty, 1);
			platform.Free(empty, 1);
			return result;
		}
		var source = APTR.FromPointer(raw);
		if (!CStringCodec.TryReadLength(ref platform, source, 4096,
			out var length)) return false;
		return MuiStoreCore.DataspaceAdd(ref platform, state, dataspace, objectId,
			source, unchecked((int)length + 1));
	}

	private static bool ImportString<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR dataspace, uint objectId, uint attribute)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var length = MuiStoreCore.DataspaceLength(ref platform, state, dataspace,
			objectId);
		if (length <= 0 || length > 4096) return false;
		var data = MuiStoreCore.DataspaceFind(ref platform, state, dataspace,
			objectId);
		if (data.IsNull || !platform.IsMapped(data, (uint)length) ||
			platform.ReadUInt8(data, length - 1) != 0) return false;
		return MuiCommonControlCore.SetPersistenceContents(ref platform, state, obj,
			attribute, data);
	}

	private static bool ImportScalar<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR dataspace, uint objectId, uint attribute,
		uint kind) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiStoreCore.DataspaceLength(ref platform, state, dataspace,
			objectId) != 4) return false;
		var data = MuiStoreCore.DataspaceFind(ref platform, state, dataspace,
			objectId);
		if (data.IsNull || !platform.IsMapped(data, 4)) return false;
		var value = platform.ReadUInt32(data, 0);
		if (kind == 2 && !MuiListCore.SetAttribute(ref platform, state, obj,
			attribute, value, false)) return false;
		if (kind == 3)
		{
			if (!MuiMenuSpecialistCore.SetAttribute(ref platform, state, obj,
				attribute, value, false, false, out _)) return false;
			return true;
		}
		return MuiCommonControlCore.SetPersistenceScalar(ref platform, state, obj,
			attribute, value);
	}
}
