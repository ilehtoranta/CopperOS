/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Volumelist.mui (autodoc MUI_Volumelist.doc). Volumelist is a subclass of
// Dirlist that lists all available volumes instead of the entries of one
// directory. It reuses the Dirlist owned-record building, sorting, status and
// attribute machinery verbatim; only the population source differs: volumes are
// enumerated through the IMuiDirectoryCapability volume seam (or synthesised
// deterministically when MUIA_Volumelist_ExampleMode is set). Every volume is
// reported as a drawer. Construction is failure-atomic and allocation-free at
// the managed level, matching the rest of the collection suite.
public static class MuiVolumelistCore
{
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiVolumelistModeStateRecord
	{
		internal const uint Size = 8;
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint ExampleModeOffset = 4;
		internal const uint Cookie = 0x564C4D44u; // 'VLMD'

		internal uint Magic;
		internal uint ExampleMode;
	}

	// The structural codec owns the packed guest representation. This
	// admission boundary owns only the MorphOS [I..] BOOL invariant so a
	// malformed mode cannot reach Volumelist population or public getters.
	internal static class MuiVolumelistModeStateAdmission
	{
		internal static bool Validate(MuiVolumelistModeStateRecord value) =>
			value.Magic == MuiVolumelistModeStateRecord.Cookie &&
			value.ExampleMode <= 1;
	}

	internal enum MuiVolumelistModeField : byte
	{
		Magic,
		ExampleMode,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiVolumelistModeFieldCursor
	{
		internal APTR Address;
		internal MuiVolumelistModeField Field;
	}

internal static class MuiVolumelistModeFieldCursorCodec
{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiVolumelistModeFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
			=> MuiVolumelistModeStateRecordMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiVolumelistModeField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
			=> MuiVolumelistModeStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiVolumelistModeField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
			=> MuiVolumelistModeStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, field, value);
	}

	// Struct-first guest-memory adapter for the fixed Volumelist mode sidecar.
	// The named record owns the wire positions; this bounded adapter is the only
	// place that projects them into guest addresses.
	internal static class MuiVolumelistModeStateRecordMemoryCodec
	{
		private static bool TryResolve(MuiVolumelistModeField field,
			out uint offset)
		{
			offset = field switch
			{
				MuiVolumelistModeField.Magic =>
					MuiVolumelistModeStateRecord.MagicOffset,
				MuiVolumelistModeField.ExampleMode =>
					MuiVolumelistModeStateRecord.ExampleModeOffset,
				_ => uint.MaxValue,
			};
			return offset != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR address, MuiVolumelistModeField field, out APTR fieldAddress)
			where TPlatform : struct, IMuiGuestMemory
		{
			fieldAddress = APTR.Null;
			if (!TryResolve(field, out var offset) || address.IsNull ||
				address.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(address, MuiVolumelistModeStateRecord.Size))
				return false;
			fieldAddress = APTR.FromPointer(address.Raw + offset);
			return platform.IsMapped(fieldAddress,
				MuiVolumelistModeStateRecord.FieldSize);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiVolumelistModeField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!TryGetAddress(ref platform, address, field, out var fieldAddress))
				return false;
			value = platform.ReadUInt32(fieldAddress, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiVolumelistModeField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!TryGetAddress(ref platform, address, field, out var fieldAddress))
				return false;
			platform.WriteUInt32(fieldAddress, 0, value);
			return true;
		}
	}

	internal static class MuiVolumelistModeStateRecordCodec
	{
		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR address, out MuiVolumelistModeStateRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiVolumelistModeStateRecord.Size) ||
				!MuiVolumelistModeStateRecordMemoryCodec.TryReadUInt32(ref platform,
					address, MuiVolumelistModeField.Magic, out var magic) ||
				!MuiVolumelistModeStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
					MuiVolumelistModeField.ExampleMode, out value.ExampleMode))
				return false;
			value.Magic = magic;
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiVolumelistModeStateRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			return TryReadStructural(ref platform, address, out value) &&
				MuiVolumelistModeStateAdmission.Validate(value);
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiVolumelistModeStateRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiVolumelistModeStateRecord.Size) ||
				!MuiVolumelistModeStateAdmission.Validate(value)) return false;
			return MuiVolumelistModeStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiVolumelistModeField.Magic, value.Magic) &&
				MuiVolumelistModeStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
					MuiVolumelistModeField.ExampleMode, value.ExampleMode);
		}
	}

	private const uint ExampleMode = 0x804246a5u; // [I..] BOOL
	private const uint Status = 0x804240deu;      // MUIA_Dirlist_Status
	internal const uint ModeStateKey = 0x7F0B0001u;

	private const int VolumeType = 2;             // ST_USERDIR: a volume is a root
	private const int MaxVolumes = 4096;
	private const int ErrorNoFreeStore = 103;

	// Create a Volumelist, failure-atomically, then populate it.
	public static APTR CreateVolumelist<TPlatform>(ref TPlatform platform,
		APTR state, APTR classRecord, APTR tags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiListCore.ClassifyRecord(ref platform, classRecord) !=
			MuiCollectionClass.Volumelist) return APTR.Null;
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, state,
			classRecord, tags);
		if (obj.IsNull) return APTR.Null;
		if (!MuiListCore.Construct(ref platform, state, classRecord, obj) ||
			!MuiListCore.HasBackbone(ref platform, state, obj) ||
			!EnsureModeStateRecord(ref platform, state, obj))
		{
			MuiCollectionLifecycle.DisposeObject(ref platform, state, obj);
			return APTR.Null;
		}
		var initial = default(MuiDirlistScanState);
		initial.Status = MuiDirlistCore.StatusInvalid;
		MuiDirlistCore.PublishScanState(ref platform, state, obj, initial, false);
		Populate(ref platform, state, obj);
		return obj;
	}

	// Attribute access is delegated to the Dirlist machinery (status, counters,
	// path). ExampleMode is authoritative in its named sidecar once that record
	// exists; a malformed record fails closed instead of falling back to raw data.
	public static bool GetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (attribute == ExampleMode)
		{
			if (TryReadModeValue(ref platform, state, obj, out value)) return true;
			if (HasModeStateStorage(ref platform, state, obj))
			{
				value = 0;
				return false;
			}
			return MuiDirlistCore.GetAttribute(ref platform, state, obj,
				attribute, out value);
		}
		return MuiDirlistCore.GetAttribute(ref platform, state, obj, attribute,
			out value);
	}

	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == ExampleMode || MuiDirlistCore.IsPublicGetterAttribute(attribute);

	public static bool SetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		attribute == ExampleMode ? SetModeAttribute(ref platform, state, obj,
			value) : MuiDirlistCore.SetAttribute(ref platform, state, obj, attribute,
			value);

	public static bool SetRuntimeAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (attribute == ExampleMode) return false;
		return MuiDirlistCore.SetRuntimeAttribute(ref platform, state, obj,
			attribute, value);
	}

	// Re-enumerate the volume set (MUIM_Dirlist_ReRead on a Volumelist).
	public static bool Populate<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiListCore.HasBackbone(ref platform, state, obj)) return false;
		// Read the named mode before changing the listing. A present malformed
		// sidecar fails closed and leaves the previous population untouched;
		// bootstrap from the raw initializer is allowed only when no sidecar
		// exists yet.
		if (!TryReadModeForPopulation(ref platform, state, obj,
			out var exampleMode)) return false;
		MuiDirlistCore.PublishScanStatus(ref platform, state, obj,
			MuiDirlistCore.StatusReading, false);
		MuiListCore.Clear(ref platform, state, obj);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiDirlistCore.ScanEntrySize);
		if (scratch.IsNull)
		{
			Invalidate(ref platform, state, obj, ErrorNoFreeStore);
			return false;
		}

		uint drawers = 0;
		var ok = exampleMode != 0
			? PopulateExample(ref platform, state, obj, scratch, ref drawers)
			: PopulateVolumes(ref platform, state, obj, scratch, ref drawers);
		FreeScratch(ref platform, scratch);
		if (!ok)
		{
			MuiListCore.Clear(ref platform, state, obj);
			Invalidate(ref platform, state, obj, platform.DirectoryError());
			return false;
		}
		MuiDirlistCore.SortEntries(ref platform, state, obj);
		var scan = default(MuiDirlistScanState);
		scan.Status = MuiDirlistCore.StatusValid;
		scan.NumDrawers = drawers;
		MuiDirlistCore.PublishScanState(ref platform, state, obj, scan, true);
		return true;
	}

	private static bool PopulateVolumes<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR scratch, ref uint drawers)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = platform.VolumeScan();
		if (count < 0) return false;
		var limit = count > MaxVolumes ? MaxVolumes : count;
		for (var i = 0; i < limit; i++)
		{
			platform.Clear(scratch, MuiDirlistCore.ScanEntrySize);
			if (!platform.VolumeEntry(i, scratch)) return false;
			if (!MuiDirlistCore.TryReadScanEntryState(ref platform, scratch,
				out var entry)) return false;
			entry.Type = VolumeType;
			if (!MuiDirlistCore.WriteScanEntryState(ref platform, scratch, entry))
				return false;
			if (!Emit(ref platform, state, obj, scratch)) return false;
			drawers++;
		}
		return true;
	}

	private static bool PopulateExample<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR scratch, ref uint drawers)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EmitExample(ref platform, state, obj, scratch, (byte)'0')) return false;
		drawers++;
		if (!EmitExample(ref platform, state, obj, scratch, (byte)'1')) return false;
		drawers++;
		return true;
	}

	// Write a deterministic "Example<digit>:" volume without any managed data,
	// keeping the freestanding native closure allocation-free.
	private static bool EmitExample<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR scratch, byte digit)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		platform.Clear(scratch, MuiDirlistCore.ScanEntrySize);
		if (!MuiDirlistCore.WriteExampleVolumeEntry(ref platform, scratch, digit))
			return false;
		return Emit(ref platform, state, obj, scratch);
	}

	private static bool Emit<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR scratch) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiDirlistCore.BuildRecord(ref platform, scratch);
		if (record.IsNull) return false;
		if (!MuiListCore.AppendOwnedRecord(ref platform, state, obj, record))
		{
			MuiDirlistCore.FreeRecord(ref platform, record);
			return false;
		}
		return true;
	}

	private static void Invalidate<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int ioErr) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var scan = default(MuiDirlistScanState);
		scan.Status = MuiDirlistCore.StatusInvalid;
		scan.IoErr = ioErr;
		MuiDirlistCore.PublishScanState(ref platform, state, obj, scan, true);
	}

	private static void FreeScratch<TPlatform>(ref TPlatform platform, APTR scratch)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		platform.Clear(scratch, MuiDirlistCore.ScanEntrySize);
		platform.Free(scratch, MuiDirlistCore.ScanEntrySize);
	}

	private static uint Read<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint fallback)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, attribute,
			out var value) ? value : fallback;

	private static bool TryReadModeRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiVolumelistModeStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ModeStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ModeStateKey) != unchecked((int)MuiVolumelistModeStateRecord.Size))
			return false;
		return MuiVolumelistModeStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool HasModeStateStorage<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiStoreCore.DataspaceLength(ref platform, state, obj, ModeStateKey) != 0;

	private static bool EnsureModeStateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadModeRecord(ref platform, state, obj, out _)) return true;
		// A present but malformed sidecar is not a bootstrap miss. Do not append a
		// append a replacement under the same logical key.
		if (HasModeStateStorage(ref platform, state, obj)) return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiVolumelistModeStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiVolumelistModeStateRecord.Size);
		var value = default(MuiVolumelistModeStateRecord);
		value.Magic = MuiVolumelistModeStateRecord.Cookie;
		value.ExampleMode = ReadRaw(ref platform, state, obj, ExampleMode, 0);
		var written = MuiVolumelistModeStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			ModeStateKey, scratch,
			unchecked((int)MuiVolumelistModeStateRecord.Size));
		platform.Clear(scratch, MuiVolumelistModeStateRecord.Size);
		platform.Free(scratch, MuiVolumelistModeStateRecord.Size);
		return added;
	}

	private static bool SyncModeStateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureModeStateRecord(ref platform, state, obj) ||
			!TryReadModeRecord(ref platform, state, obj, out var value)) return false;
		value.ExampleMode = ReadRaw(ref platform, state, obj, ExampleMode, 0);
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ModeStateKey);
		return MuiVolumelistModeStateRecordCodec.Write(ref platform, block, value);
	}

	private static bool TryReadModeValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (!TryReadModeRecord(ref platform, state, obj, out var record))
			return false;
		value = record.ExampleMode;
		return true;
	}

	private static bool TryReadModeForPopulation<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadModeValue(ref platform, state, obj, out value)) return true;
		if (HasModeStateStorage(ref platform, state, obj))
		{
			value = 0;
			return false;
		}
		value = ReadRaw(ref platform, state, obj, ExampleMode, 0);
		return true;
	}

	private static bool SetModeAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (HasModeStateStorage(ref platform, state, obj) &&
			!TryReadModeRecord(ref platform, state, obj, out _)) return false;
		value = value == 0 ? 0u : 1u;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ExampleMode, value, false)) return false;
		return SyncModeStateRecord(ref platform, state, obj);
	}

	internal static bool TryGetModeStateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiVolumelistModeStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadModeRecord(ref platform, state, obj, out value);

	private static uint ReadRaw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint fallback)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, attribute,
			out var value) ? value : fallback;

	private static void SetInternal<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
			value, false);

}
