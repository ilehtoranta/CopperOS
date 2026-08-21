/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The Objective-C MorphOS MUIArea API describes handledEvents as a
// window-owned event registration. Keep the implementation state in a
// guest-resident named record: the object owns the event mask and the
// generated MUI_EventHandlerNode, while the window owns the scheduler link.
// This deliberately does not invent a numeric MUIA_HandledEvents tag until
// the MorphOS C header ABI exposes that value.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaHandledEventsStateRecord
{
	internal const uint Size = 24;
	internal const uint Magic = 0x4D484556; // "MHEV"
	internal const ushort PolicyFlags =
		MuiEventHandlerNodeInput.MUI_EHF_ALWAYSKEYS |
		MuiEventHandlerNodeInput.MUI_EHF_GUIMODE |
		MuiEventHandlerNodeInput.MUI_EHF_PRIORITY;

	internal uint Signature;
	internal uint Events;
	internal APTR Window;
	internal APTR Handler;
	internal uint Generation;
	internal ushort HandlerFlags;
	internal sbyte Priority;
	internal byte Reserved;
}

internal enum MuiAreaHandledEventsStateField : byte
{
	Signature,
	Events,
	Window,
	Handler,
	Generation,
	HandlerFlags,
	Priority,
	Reserved,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaHandledEventsStateFieldCursor
{
	internal APTR Address;
	internal MuiAreaHandledEventsStateField Field;
}

internal static class MuiAreaHandledEventsStateCodec
{
	private static bool TryResolve(MuiAreaHandledEventsStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaHandledEventsStateField.Signature:
				offset = 0;
				return true;
			case MuiAreaHandledEventsStateField.Events:
				offset = 4;
				return true;
			case MuiAreaHandledEventsStateField.Window:
				offset = 8;
				return true;
			case MuiAreaHandledEventsStateField.Handler:
				offset = 12;
				return true;
			case MuiAreaHandledEventsStateField.Generation:
				offset = 16;
				return true;
			case MuiAreaHandledEventsStateField.HandlerFlags:
				offset = 20;
				return true;
			case MuiAreaHandledEventsStateField.Priority:
				offset = 22;
				return true;
			case MuiAreaHandledEventsStateField.Reserved:
				offset = 23;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	private static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaHandledEventsStateFieldCursor cursor, uint size, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address, MuiAreaHandledEventsStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, size);
	}

	private static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaHandledEventsStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaHandledEventsStateFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, 4, out var fieldAddress)) return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	private static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaHandledEventsStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaHandledEventsStateFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, 4, out var fieldAddress)) return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}

	private static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaHandledEventsStateField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaHandledEventsStateFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (field != MuiAreaHandledEventsStateField.HandlerFlags ||
			!TryGetAddress(ref platform, cursor, 2, out var fieldAddress)) return false;
		value = platform.ReadUInt16(fieldAddress, 0);
		return true;
	}

	private static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaHandledEventsStateField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaHandledEventsStateFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if ((field != MuiAreaHandledEventsStateField.Priority &&
			field != MuiAreaHandledEventsStateField.Reserved) ||
			!TryGetAddress(ref platform, cursor, 1, out var fieldAddress)) return false;
		value = platform.ReadUInt8(fieldAddress, 0);
		return true;
	}

	private static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaHandledEventsStateField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaHandledEventsStateFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (field != MuiAreaHandledEventsStateField.HandlerFlags ||
			!TryGetAddress(ref platform, cursor, 2, out var fieldAddress)) return false;
		platform.WriteUInt16(fieldAddress, 0, value);
		return true;
	}

	private static bool TryWriteUInt8<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaHandledEventsStateField field, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaHandledEventsStateFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if ((field != MuiAreaHandledEventsStateField.Priority &&
			field != MuiAreaHandledEventsStateField.Reserved) ||
			!TryGetAddress(ref platform, cursor, 1, out var fieldAddress)) return false;
		platform.WriteUInt8(fieldAddress, 0, value);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaHandledEventsStateRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!TryReadUInt32(ref platform, address,
			MuiAreaHandledEventsStateField.Signature, out record.Signature) ||
			!TryReadUInt32(ref platform, address,
				MuiAreaHandledEventsStateField.Events, out record.Events) ||
			!TryReadUInt32(ref platform, address,
				MuiAreaHandledEventsStateField.Window, out var window) ||
			!TryReadUInt32(ref platform, address,
				MuiAreaHandledEventsStateField.Handler, out var handler) ||
			!TryReadUInt32(ref platform, address,
				MuiAreaHandledEventsStateField.Generation, out record.Generation) ||
			!TryReadUInt16(ref platform, address,
				MuiAreaHandledEventsStateField.HandlerFlags, out record.HandlerFlags) ||
			!TryReadUInt8(ref platform, address,
				MuiAreaHandledEventsStateField.Priority, out var priority) ||
			!TryReadUInt8(ref platform, address,
				MuiAreaHandledEventsStateField.Reserved, out record.Reserved))
			return false;
		record.Window = APTR.FromPointer(window);
		record.Handler = APTR.FromPointer(handler);
		record.Priority = unchecked((sbyte)priority);
		record.HandlerFlags = (ushort)(record.HandlerFlags &
			MuiAreaHandledEventsStateRecord.PolicyFlags);
		return record.Signature == MuiAreaHandledEventsStateRecord.Magic;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaHandledEventsStateRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaHandledEventsStateRecord.Size)) return false;
		return TryWriteUInt32(ref platform, address,
			MuiAreaHandledEventsStateField.Signature, record.Signature) &&
			TryWriteUInt32(ref platform, address,
				MuiAreaHandledEventsStateField.Events, record.Events) &&
			TryWriteUInt32(ref platform, address,
				MuiAreaHandledEventsStateField.Window, record.Window.Raw) &&
			TryWriteUInt32(ref platform, address,
				MuiAreaHandledEventsStateField.Handler, record.Handler.Raw) &&
			TryWriteUInt32(ref platform, address,
				MuiAreaHandledEventsStateField.Generation, record.Generation) &&
			TryWriteUInt16(ref platform, address,
				MuiAreaHandledEventsStateField.HandlerFlags, record.HandlerFlags) &&
			TryWriteUInt8(ref platform, address,
				MuiAreaHandledEventsStateField.Priority, unchecked((byte)record.Priority)) &&
			TryWriteUInt8(ref platform, address,
				MuiAreaHandledEventsStateField.Reserved, record.Reserved);
	}
}

internal enum MuiAreaEventHandlerPolicyField : byte
{
	AlwaysKeys,
	GuiMode,
	Priority,
}

// Struct-first core for the MorphOS MUIArea handled-events contract. The
// state is intentionally stored through Dataspace so object disposal and
// object moves never require a managed dictionary or a private object offset.
internal static class MuiAreaEventHandlerCore
{
	private const uint StateKey = 0x7F07003E;

	internal static bool SetHandledEvents<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint events)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var hadPrevious = TryReadState(ref platform, state, obj, out var previous);
		if (hadPrevious)
		{
			var detached = previous;
			detached.Window = APTR.Null;
			detached.Handler = APTR.Null;
			if (!StoreState(ref platform, state, obj, detached)) return false;
			Unregister(ref platform, state, previous);
		}
		if (events == 0)
		{
			MuiStoreCore.DataspaceRemove(ref platform, state, obj, StateKey);
			return true;
		}
		var next = default(MuiAreaHandledEventsStateRecord);
		next.Signature = MuiAreaHandledEventsStateRecord.Magic;
		next.Events = events;
		next.Generation = MuiHeadlessMemory.NextSequence(ref platform, state);
		next.HandlerFlags = hadPrevious ? previous.HandlerFlags :
			MuiEventHandlerNodeInput.MUI_EHF_GUIMODE;
		next.Priority = hadPrevious ? previous.Priority : (sbyte)0;
		if (!StoreState(ref platform, state, obj, next)) return false;
		return Reconcile(ref platform, state, obj);
	}

	internal static bool SetEventHandlerAlwaysKeys<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, bool enabled)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		UpdatePolicy(ref platform, state, obj, enabled ?
			MuiEventHandlerNodeInput.MUI_EHF_ALWAYSKEYS : (ushort)0,
			MuiAreaEventHandlerPolicyField.AlwaysKeys, 0);

	internal static bool SetEventHandlerGuiMode<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, bool enabled)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		UpdatePolicy(ref platform, state, obj, enabled ?
			MuiEventHandlerNodeInput.MUI_EHF_GUIMODE : (ushort)0,
			MuiAreaEventHandlerPolicyField.GuiMode, 0);

	internal static bool SetEventHandlerPriority<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, sbyte priority)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		UpdatePolicy(ref platform, state, obj, 0,
			MuiAreaEventHandlerPolicyField.Priority, priority);

	internal static bool TryGetEventHandlerPolicy<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out ushort flags, out sbyte priority)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		flags = 0;
		priority = 0;
		if (!TryReadState(ref platform, state, obj, out var record)) return false;
		flags = record.HandlerFlags;
		priority = record.Priority;
		return true;
	}

	private static bool UpdatePolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, ushort flagValue,
		MuiAreaEventHandlerPolicyField field, sbyte priority)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadState(ref platform, state, obj, out var current)) return false;
		var next = current;
		switch (field)
		{
			case MuiAreaEventHandlerPolicyField.AlwaysKeys:
				next.HandlerFlags = flagValue != 0 ?
					(ushort)(next.HandlerFlags |
						MuiEventHandlerNodeInput.MUI_EHF_ALWAYSKEYS) :
					(ushort)(next.HandlerFlags &
						~MuiEventHandlerNodeInput.MUI_EHF_ALWAYSKEYS);
				break;
			case MuiAreaEventHandlerPolicyField.GuiMode:
				next.HandlerFlags = flagValue != 0 ?
					(ushort)(next.HandlerFlags |
						MuiEventHandlerNodeInput.MUI_EHF_GUIMODE) :
					(ushort)(next.HandlerFlags &
						~MuiEventHandlerNodeInput.MUI_EHF_GUIMODE);
				break;
			case MuiAreaEventHandlerPolicyField.Priority:
				next.Priority = priority;
				break;
			default:
				return false;
		}
		next.HandlerFlags = (ushort)(next.HandlerFlags &
			MuiAreaHandledEventsStateRecord.PolicyFlags);
		next.Window = APTR.Null;
		next.Handler = APTR.Null;
		if (!StoreState(ref platform, state, obj, next)) return false;
		Unregister(ref platform, state, current);
		return Reconcile(ref platform, state, obj);
	}

	internal static bool TryGetHandledEventsState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiAreaHandledEventsStateRecord record)
		where TPlatform : struct, IMuiApplicationPlatform =>
		TryReadState(ref platform, state, obj, out record);

	internal static bool Reconcile<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadState(ref platform, state, obj, out var record)) return true;
		var owner = FindWindow(ref platform, state, obj);
		if (record.Handler.IsNotNull && record.Window != owner)
		{
			// Persist the detached state before releasing the old node. If the
			// guest allocation for this update fails, the old registration and
			// its ownership record remain intact for a later retry.
			var detached = record;
			detached.Window = APTR.Null;
			detached.Handler = APTR.Null;
			if (!StoreState(ref platform, state, obj, detached)) return false;
			Unregister(ref platform, state, record);
			record = detached;
		}
		if (owner.IsNotNull && record.Handler.IsNull)
		{
			var handler = MuiHeadlessMemory.Allocate(ref platform,
				MuiEventHandlerNodeRecord.Size);
			if (handler.IsNull) return false;
			var node = default(MuiEventHandlerNodeRecord);
			node.Object = obj;
			node.Priority = record.Priority;
			node.Events = record.Events;
			node.Flags = (ushort)(record.HandlerFlags &
				MuiAreaHandledEventsStateRecord.PolicyFlags);
			if (!MuiEventHandlerNodeCodec.Write(ref platform, handler, node) ||
				!MuiApplicationWindowCore.AddEventHandlerRegistration(ref platform, state,
					owner, handler))
			{
				platform.Clear(handler, MuiEventHandlerNodeRecord.Size);
				platform.Free(handler, MuiEventHandlerNodeRecord.Size);
				return false;
			}
			record.Window = owner;
			record.Handler = handler;
			if (!StoreState(ref platform, state, obj, record))
			{
				Unregister(ref platform, state, record);
				return false;
			}
		}
		return true;
	}

	internal static bool Cleanup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadState(ref platform, state, obj, out var record)) return true;
		Unregister(ref platform, state, record);
		return MuiStoreCore.DataspaceRemove(ref platform, state, obj, StateKey);
	}

	private static APTR FindWindow<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var current = obj;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (MuiApplicationMessageCore.IsWindowObject(ref platform, state,
				current)) return current;
			current = MuiHeadlessObjectCore.ParentObject(ref platform, state,
				current);
		}
		return APTR.Null;
	}

	private static bool TryReadState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaHandledEventsStateRecord record)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		record = default;
		var data = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StateKey);
		return data.IsNotNull && MuiStoreCore.DataspaceLength(ref platform, state,
			obj, StateKey) == (int)MuiAreaHandledEventsStateRecord.Size &&
			MuiAreaHandledEventsStateCodec.TryRead(ref platform, data, out record);
	}

	private static bool StoreState<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, MuiAreaHandledEventsStateRecord record)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaHandledEventsStateRecord.Size);
		if (scratch.IsNull) return false;
		var written = MuiAreaHandledEventsStateCodec.Write(ref platform, scratch,
		record);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateKey, scratch, (int)MuiAreaHandledEventsStateRecord.Size);
		platform.Clear(scratch, MuiAreaHandledEventsStateRecord.Size);
		platform.Free(scratch, MuiAreaHandledEventsStateRecord.Size);
		return stored;
	}

	private static void Unregister<TPlatform>(ref TPlatform platform, APTR state,
		MuiAreaHandledEventsStateRecord record)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (record.Handler.IsNull) return;
		if (record.Window.IsNotNull &&
			!MuiHeadlessObjectCore.FindObject(ref platform, state,
				record.Window).IsNull)
			MuiApplicationWindowCore.RemoveEventHandler(ref platform, state,
				record.Window, record.Handler);
		if (platform.IsMapped(record.Handler, MuiEventHandlerNodeRecord.Size))
		{
			platform.Clear(record.Handler, MuiEventHandlerNodeRecord.Size);
			platform.Free(record.Handler, MuiEventHandlerNodeRecord.Size);
		}
	}
}

// Public, value-type projection used by the future Objective-C bridge. It
// exposes the guest-resident registration state without exposing Dataspace
// keys, managed dictionaries, or private object layout.
public struct MuiAreaEventHandlerStateInput
{
	public uint Events;
	public APTR Window;
	public APTR Handler;
	public ushort HandlerFlags;
	public sbyte Priority;
}

public static class MuiAreaEventHandlerPacketCore
{
	public static bool SetHandledEvents<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint events)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaEventHandlerCore.SetHandledEvents(ref platform, state, obj, events);

	public static bool SetEventHandlerAlwaysKeys<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, bool enabled)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaEventHandlerCore.SetEventHandlerAlwaysKeys(ref platform, state,
			obj, enabled);

	public static bool SetEventHandlerGuiMode<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, bool enabled)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaEventHandlerCore.SetEventHandlerGuiMode(ref platform, state,
			obj, enabled);

	public static bool SetEventHandlerPriority<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, sbyte priority)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaEventHandlerCore.SetEventHandlerPriority(ref platform, state,
			obj, priority);

	public static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiAreaEventHandlerStateInput value)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		value = default;
		if (!MuiAreaEventHandlerCore.TryGetHandledEventsState(ref platform, state,
			obj, out var record)) return false;
		value.Events = record.Events;
		value.Window = record.Window;
		value.Handler = record.Handler;
		value.HandlerFlags = record.HandlerFlags;
		value.Priority = record.Priority;
		return true;
	}
}
