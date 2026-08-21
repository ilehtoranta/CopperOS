/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDragState
{
	internal const uint Size = 32;
	internal const uint ActiveFlag = 1;
	internal const uint DroppedFlag = 2;
	internal const uint ReportedFlag = 4;

	internal uint Magic;
	internal uint Source;
	internal uint Target;
	internal int LastX;
	internal int LastY;
	internal uint Qualifier;
	internal uint EventFlags;
	internal uint Flags;
}

internal enum MuiAreaDragStateField : byte
{
	Magic,
	Source,
	Target,
	LastX,
	LastY,
	Qualifier,
	EventFlags,
	Flags,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDragStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaDragStateField Field;
}

internal static class MuiAreaDragStateFieldCursorCodec
{
	private static bool TryResolve(MuiAreaDragStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiAreaDragStateField.Magic => 0,
			MuiAreaDragStateField.Source => 4,
			MuiAreaDragStateField.Target => 8,
			MuiAreaDragStateField.LastX => 12,
			MuiAreaDragStateField.LastY => 16,
			MuiAreaDragStateField.Qualifier => 20,
			MuiAreaDragStateField.EventFlags => 24,
			MuiAreaDragStateField.Flags => 28,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaDragStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDragStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaDragStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDragStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaDragStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaDragStateCodec
{
	internal const uint Cookie = 0x41445247u; // 'ADRG'

	internal static void Write<TPlatform>(ref TPlatform platform, APTR storage,
		MuiAreaDragState value) where TPlatform : struct, IMuiGuestMemory
	{
		_ = MuiAreaDragStateFieldCursorCodec.TryWriteUInt32(ref platform, storage,
			MuiAreaDragStateField.Magic, value.Magic);
		_ = MuiAreaDragStateFieldCursorCodec.TryWriteUInt32(ref platform, storage,
			MuiAreaDragStateField.Source, value.Source);
		_ = MuiAreaDragStateFieldCursorCodec.TryWriteUInt32(ref platform, storage,
			MuiAreaDragStateField.Target, value.Target);
		_ = MuiAreaDragStateFieldCursorCodec.TryWriteUInt32(ref platform, storage,
			MuiAreaDragStateField.LastX, unchecked((uint)value.LastX));
		_ = MuiAreaDragStateFieldCursorCodec.TryWriteUInt32(ref platform, storage,
			MuiAreaDragStateField.LastY, unchecked((uint)value.LastY));
		_ = MuiAreaDragStateFieldCursorCodec.TryWriteUInt32(ref platform, storage,
			MuiAreaDragStateField.Qualifier, value.Qualifier);
		_ = MuiAreaDragStateFieldCursorCodec.TryWriteUInt32(ref platform, storage,
			MuiAreaDragStateField.EventFlags, value.EventFlags);
		_ = MuiAreaDragStateFieldCursorCodec.TryWriteUInt32(ref platform, storage,
			MuiAreaDragStateField.Flags, value.Flags);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR storage,
		out MuiAreaDragState value) where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (storage.IsNull || !platform.IsMapped(storage, MuiAreaDragState.Size) ||
			!MuiAreaDragStateFieldCursorCodec.TryReadUInt32(ref platform, storage,
				MuiAreaDragStateField.Magic, out var magic) || magic != Cookie ||
			!MuiAreaDragStateFieldCursorCodec.TryReadUInt32(ref platform, storage,
				MuiAreaDragStateField.Source, out var source) ||
			!MuiAreaDragStateFieldCursorCodec.TryReadUInt32(ref platform, storage,
				MuiAreaDragStateField.Target, out var target) ||
			!MuiAreaDragStateFieldCursorCodec.TryReadUInt32(ref platform, storage,
				MuiAreaDragStateField.LastX, out var lastX) ||
			!MuiAreaDragStateFieldCursorCodec.TryReadUInt32(ref platform, storage,
				MuiAreaDragStateField.LastY, out var lastY) ||
			!MuiAreaDragStateFieldCursorCodec.TryReadUInt32(ref platform, storage,
				MuiAreaDragStateField.Qualifier, out var qualifier) ||
			!MuiAreaDragStateFieldCursorCodec.TryReadUInt32(ref platform, storage,
				MuiAreaDragStateField.EventFlags, out var eventFlags) ||
			!MuiAreaDragStateFieldCursorCodec.TryReadUInt32(ref platform, storage,
				MuiAreaDragStateField.Flags, out var flags)) return false;
		value.Magic = Cookie;
		value.Source = source;
		value.Target = target;
		value.LastX = unchecked((int)lastX);
		value.LastY = unchecked((int)lastY);
		value.Qualifier = qualifier;
		value.EventFlags = eventFlags;
		value.Flags = flags;
		return true;
	}

	internal static void Clear<TPlatform>(ref TPlatform platform, APTR storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (storage.IsNull || !platform.IsMapped(storage, MuiAreaDragState.Size))
			return;
		platform.Clear(storage, MuiAreaDragState.Size);
	}
}

// First MorphOS Area drag slice. This owns the fixed method-family defaults
// and a guest-resident source state record, but deliberately does not pretend
// to provide Intuition pointer capture, drag images, or application-level drop
// dispatch. Those capabilities remain separate progressive seams.
public static class MuiAreaDragCore
{
	internal const uint Draggable = 0x80420B6Eu;
	internal const uint Dropable = 0x8042FBCEu;
	internal const uint QueryRefuse = 0;
	internal const uint QueryAccept = 1;
	internal const uint ReportAbort = 0;
	internal const uint ReportContinue = 1;
	internal const uint ReportLock = 2;
	internal const uint ReportRefresh = 3;

	internal const uint StateKey = 0x7F090003u;
	internal const uint PolicyStateKey = 0x7F07003Au;

	public static bool IsDragMethod(uint method) =>
		MuiAreaDragMessageCodec.IsMethod(method);

	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiAreaDragMessageCodec.TryReadMethodId(ref platform, message,
			out var methodHeader)) return 0;
		switch (methodHeader.MethodId)
		{
			case MuiAreaDragMessageCodec.DragBegin:
				if (!MuiAreaDragMessageCodec.TryReadBegin(ref platform, message,
					out var begin)) return 0;
				return Begin(ref platform, state, APTR.FromPointer(begin.Object));
			case MuiAreaDragMessageCodec.DragDrop:
				if (!MuiAreaDragMessageCodec.TryReadDrop(ref platform, message,
					out var drop)) return 0;
				return Drop(ref platform, state, obj, drop);
			case MuiAreaDragMessageCodec.DragEvent:
				if (!MuiAreaDragMessageCodec.TryReadEvent(ref platform, message,
					out var dragEvent)) return 0;
				return Event(ref platform, state, dragEvent);
			case MuiAreaDragMessageCodec.DragFinish:
				if (!MuiAreaDragMessageCodec.TryReadFinish(ref platform, message,
					out var finish)) return 0;
				return Finish(ref platform, state, finish);
			case MuiAreaDragMessageCodec.DragQuery:
				if (!MuiAreaDragMessageCodec.TryReadQuery(ref platform, message,
					out var query)) return 0;
				return Query(ref platform, state, obj, query);
			case MuiAreaDragMessageCodec.DragReport:
				if (!MuiAreaDragMessageCodec.TryReadReport(ref platform, message,
					out var report)) return 0;
				return Report(ref platform, state, report);
		}
		return 0;
	}

	internal static uint Begin<TPlatform>(ref TPlatform platform, APTR state,
		APTR source) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (source.IsNull || !IsEnabled(ref platform, state, source, Draggable))
			return 0;
		var storage = EnsureState(ref platform, state, source);
		if (storage.IsNull) return 0;
		var value = default(MuiAreaDragState);
		value.Magic = MuiAreaDragStateCodec.Cookie;
		value.Source = source.Raw;
		value.Flags = MuiAreaDragState.ActiveFlag;
		MuiAreaDragStateCodec.Write(ref platform, storage, value);
		return 1;
	}

	internal static uint Query<TPlatform>(ref TPlatform platform, APTR state,
		APTR target, MuiAreaDragQueryMessage packet)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var source = APTR.FromPointer(packet.Object);
		return source.IsNotNull && target.IsNotNull &&
			IsEnabled(ref platform, state, source, Draggable) &&
			IsEnabled(ref platform, state, target, Dropable) ? QueryAccept : QueryRefuse;
	}

	internal static uint Drop<TPlatform>(ref TPlatform platform, APTR state,
		APTR target, MuiAreaDragDropMessage packet)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var source = APTR.FromPointer(packet.Object);
		var query = default(MuiAreaDragQueryMessage);
		query.Object = packet.Object;
		if (Query(ref platform, state, target, query) != QueryAccept)
			return 0;
		var storage = StateStorage(ref platform, state, source,
			out var value);
		if (storage.IsNull || (value.Flags & MuiAreaDragState.ActiveFlag) == 0)
			return 0;
		value.Target = target.Raw;
		value.LastX = packet.X;
		value.LastY = packet.Y;
		value.Qualifier = packet.Qualifier;
		value.Flags |= MuiAreaDragState.DroppedFlag;
		MuiAreaDragStateCodec.Write(ref platform, storage, value);
		return 1;
	}

	internal static uint Event<TPlatform>(ref TPlatform platform, APTR state,
		MuiAreaDragEventMessage packet)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var source = APTR.FromPointer(packet.Object);
		var storage = StateStorage(ref platform, state, source,
			out var value);
		if (storage.IsNull || (value.Flags & MuiAreaDragState.ActiveFlag) == 0)
			return 0;
		value.EventFlags = packet.Flags;
		MuiAreaDragStateCodec.Write(ref platform, storage, value);
		return 1;
	}

	internal static uint Report<TPlatform>(ref TPlatform platform, APTR state,
		MuiAreaDragReportMessage packet)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var source = APTR.FromPointer(packet.Object);
		var storage = StateStorage(ref platform, state, source,
			out var value);
		if (storage.IsNull || (value.Flags & MuiAreaDragState.ActiveFlag) == 0)
			return ReportAbort;
		value.LastX = packet.X;
		value.LastY = packet.Y;
		value.Qualifier = packet.Qualifier;
		value.EventFlags = unchecked((uint)packet.Update);
		value.Flags |= MuiAreaDragState.ReportedFlag;
		MuiAreaDragStateCodec.Write(ref platform, storage, value);
		return ReportContinue;
	}

	internal static uint Finish<TPlatform>(ref TPlatform platform, APTR state,
		MuiAreaDragFinishMessage packet)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var source = APTR.FromPointer(packet.Object);
		var storage = StateStorage(ref platform, state, source,
			out var value);
		if (storage.IsNull || (value.Flags & MuiAreaDragState.ActiveFlag) == 0)
			return 0;
		ReleaseState(ref platform, state, source, storage);
		return 1;
	}

	internal static bool TryReadPolicyState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaDragPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var draggable = ReadPolicyAttribute(ref platform, state, obj, Draggable,
			0);
		var dropable = ReadPolicyAttribute(ref platform, state, obj, Dropable, 1);
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PolicyStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			PolicyStateKey) == unchecked((int)MuiAreaDragPolicyStateRecord.Size) &&
			MuiAreaDragPolicyStateRecordCodec.TryRead(ref platform, block,
				out value))
		{
			if (value.Draggable != draggable || value.Dropable != dropable)
			{
				value.Draggable = draggable;
				value.Dropable = dropable;
				if (!MuiAreaDragPolicyStateRecordCodec.Write(ref platform, block,
					value)) return false;
			}
			return true;
		}

		value = default;
		value.Magic = MuiAreaDragPolicyStateRecord.Cookie;
		value.Draggable = draggable;
		value.Dropable = dropable;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaDragPolicyStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaDragPolicyStateRecord.Size);
		var written = MuiAreaDragPolicyStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			PolicyStateKey, scratch,
			unchecked((int)MuiAreaDragPolicyStateRecord.Size));
		platform.Clear(scratch, MuiAreaDragPolicyStateRecord.Size);
		platform.Free(scratch, MuiAreaDragPolicyStateRecord.Size);
		return added;
	}

	internal static bool TryGetPolicyStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiAreaDragPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadPolicyState(ref platform, state, obj, out value);

	internal static bool TryGetExistingPolicyStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiAreaDragPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PolicyStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			PolicyStateKey) != unchecked((int)MuiAreaDragPolicyStateRecord.Size))
			return false;
		return MuiAreaDragPolicyStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool IsEnabled<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryGetExistingPolicyStateRecord(ref platform, state, obj,
			out var policy))
			return (attribute == Draggable ? policy.Draggable : policy.Dropable) != 0;
		var defaultValue = attribute == Dropable ? 1u : 0u;
		return ReadPolicyAttribute(ref platform, state, obj, attribute,
			defaultValue) != 0;
	}

	private static uint ReadPolicyAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, uint defaultValue)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, attribute,
			out var value) ? (value == 0 ? 0u : 1u) : defaultValue;

	private static APTR EnsureState<TPlatform>(ref TPlatform platform, APTR state,
		APTR source) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var existing = StateStorage(ref platform, state, source, out _);
		if (existing.IsNotNull) return existing;
		if (MuiHeadlessObjectCore.GetAttribute(ref platform, state, source,
			StateKey, out var raw))
		{
			var malformed = APTR.FromPointer(raw);
			if (malformed.IsNotNull && platform.IsMapped(malformed,
				MuiAreaDragState.Size))
				platform.Free(malformed, MuiAreaDragState.Size);
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, source,
				StateKey, 0, false);
		}
		var storage = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaDragState.Size);
		if (storage.IsNull || !MuiHeadlessObjectCore.SetAttribute(ref platform,
			state, source, StateKey, storage.Raw, false))
		{
			if (storage.IsNotNull) platform.Free(storage, MuiAreaDragState.Size);
			return APTR.Null;
		}
		return storage;
	}

	private static APTR StateStorage<TPlatform>(ref TPlatform platform, APTR state,
		APTR source, out MuiAreaDragState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (source.IsNull || !MuiHeadlessObjectCore.GetAttribute(ref platform,
			state, source, StateKey, out var raw)) return APTR.Null;
		var storage = APTR.FromPointer(raw);
		return MuiAreaDragStateCodec.TryRead(ref platform, storage, out value) ?
			storage : APTR.Null;
	}

	private static void ReleaseState<TPlatform>(ref TPlatform platform, APTR state,
		APTR source, APTR storage)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		MuiAreaDragStateCodec.Clear(ref platform, storage);
		if (storage.IsNotNull && platform.IsMapped(storage, MuiAreaDragState.Size))
			platform.Free(storage, MuiAreaDragState.Size);
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, source, StateKey,
			0, false);
	}
}
