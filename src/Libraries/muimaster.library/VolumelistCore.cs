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
		internal const uint Cookie = 0x564C4D44u; // 'VLMD'

		internal uint Magic;
		internal uint ExampleMode;
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
		private static bool TryResolve(MuiVolumelistModeField field,
			out uint offset)
		{
			switch (field)
			{
				case MuiVolumelistModeField.Magic:
					offset = 0;
					return true;
				case MuiVolumelistModeField.ExampleMode:
					offset = 4;
					return true;
			}
			offset = 0;
			return false;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiVolumelistModeFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
				cursor.Address.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Address,
					MuiVolumelistModeStateRecord.Size)) return false;
			address = APTR.FromPointer(cursor.Address.Raw + offset);
			return platform.IsMapped(address, 4);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiVolumelistModeField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiVolumelistModeFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			value = platform.ReadUInt32(fieldAddress, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiVolumelistModeField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiVolumelistModeFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			platform.WriteUInt32(fieldAddress, 0, value);
			return true;
		}
	}

	internal static class MuiVolumelistModeStateRecordCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiVolumelistModeStateRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiVolumelistModeStateRecord.Size) ||
				!MuiVolumelistModeFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiVolumelistModeField.Magic, out var magic) ||
				magic != MuiVolumelistModeStateRecord.Cookie ||
				!MuiVolumelistModeFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiVolumelistModeField.ExampleMode, out value.ExampleMode))
				return false;
			value.Magic = magic;
			return true;
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiVolumelistModeStateRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiVolumelistModeStateRecord.Size) ||
				value.Magic != MuiVolumelistModeStateRecord.Cookie) return false;
			return MuiVolumelistModeFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiVolumelistModeField.Magic, value.Magic) &&
				MuiVolumelistModeFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiVolumelistModeField.ExampleMode, value.ExampleMode);
		}
	}

	private const uint ExampleMode = 0x804246a5u; // [I..] BOOL
	private const uint Status = 0x804240deu;      // MUIA_Dirlist_Status
	private const uint ModeStateKey = 0x7F0B0001u;

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
	// path); ExampleMode falls through to the generic store there.
	public static bool GetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		attribute == ExampleMode && TryReadModeValue(ref platform, state, obj,
			out value) ? true : MuiDirlistCore.GetAttribute(ref platform, state, obj,
			attribute, out value);

	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == ExampleMode || MuiDirlistCore.IsPublicGetterAttribute(attribute);

	public static bool SetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		attribute == ExampleMode ? SetModeAttribute(ref platform, state, obj,
			value) : MuiDirlistCore.SetAttribute(ref platform, state, obj, attribute,
			value);

	// Re-enumerate the volume set (MUIM_Dirlist_ReRead on a Volumelist).
	public static bool Populate<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiListCore.HasBackbone(ref platform, state, obj)) return false;
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
		var ok = ReadMode(ref platform, state, obj) != 0
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

	private static bool EnsureModeStateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadModeRecord(ref platform, state, obj, out _)) return true;
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

	private static uint ReadMode<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadModeValue(ref platform, state, obj, out var value)
			? value : ReadRaw(ref platform, state, obj, ExampleMode, 0);

	private static bool SetModeAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
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
