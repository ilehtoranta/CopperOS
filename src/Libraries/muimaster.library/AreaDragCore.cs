/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiDragImageCreateSample
{
	// The provider owns the temporary MorphOS MUI_DragImage allocation. The
	// core only carries the named request and returned opaque handle.
	public APTR Object;
	public int TouchX;
	public int TouchY;
	public uint Flags;
	public APTR Result;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiDragImageDeleteSample
{
	public APTR Object;
	public APTR DragImage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDragState
{
	internal const uint Size = 32;
	internal const uint FieldSize = 4;
	internal const uint ActiveFlag = 1;
	internal const uint DroppedFlag = 2;
	internal const uint ReportedFlag = 4;
	internal const uint CapturedFlag = 8;

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

// The fixed state record owns its packed positions in this bounded adapter.
// Live drag lifecycle code uses this codec directly; the typed cursor below
// remains available for compatibility callers and adapter-focused tests.
internal static class MuiAreaDragStateMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiAreaDragStateField field,
		out uint index)
	{
		if (field == MuiAreaDragStateField.Magic) index = 0;
		else if (field == MuiAreaDragStateField.Source) index = 1;
		else if (field == MuiAreaDragStateField.Target) index = 2;
		else if (field == MuiAreaDragStateField.LastX) index = 3;
		else if (field == MuiAreaDragStateField.LastY) index = 4;
		else if (field == MuiAreaDragStateField.Qualifier) index = 5;
		else if (field == MuiAreaDragStateField.EventFlags) index = 6;
		else if (field == MuiAreaDragStateField.Flags) index = 7;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDragStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaDragStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaDragStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiAreaDragState.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiAreaDragState.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiAreaDragState.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDragStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaDragStateCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiAreaDragStateField.Magic)
			value = state.Magic;
		else if (field == MuiAreaDragStateField.Source)
			value = state.Source;
		else if (field == MuiAreaDragStateField.Target)
			value = state.Target;
		else if (field == MuiAreaDragStateField.LastX)
			value = unchecked((uint)state.LastX);
		else if (field == MuiAreaDragStateField.LastY)
			value = unchecked((uint)state.LastY);
		else if (field == MuiAreaDragStateField.Qualifier)
			value = state.Qualifier;
		else if (field == MuiAreaDragStateField.EventFlags)
			value = state.EventFlags;
		else if (field == MuiAreaDragStateField.Flags)
			value = state.Flags;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDragStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaDragStateCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiAreaDragStateField.Magic)
			state.Magic = value;
		else if (field == MuiAreaDragStateField.Source)
			state.Source = value;
		else if (field == MuiAreaDragStateField.Target)
			state.Target = value;
		else if (field == MuiAreaDragStateField.LastX)
			state.LastX = unchecked((int)value);
		else if (field == MuiAreaDragStateField.LastY)
			state.LastY = unchecked((int)value);
		else if (field == MuiAreaDragStateField.Qualifier)
			state.Qualifier = value;
		else if (field == MuiAreaDragStateField.EventFlags)
			state.EventFlags = value;
		else if (field == MuiAreaDragStateField.Flags)
			state.Flags = value;
		else return false;
		return MuiAreaDragStateCodec.TryWriteStruct(ref platform, record, state);
	}
}

internal static class MuiAreaDragStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaDragStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaDragStateFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaDragStateMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDragStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaDragStateMemoryCodec.TryReadUInt32(ref platform, record, field,
			out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDragStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaDragStateMemoryCodec.TryWriteUInt32(ref platform, record, field,
			value);
}

internal static class MuiAreaDragStateCodec
{
	internal const uint Cookie = 0x41445247u; // 'ADRG'

	// The live state path is struct-first.  The legacy field adapter above is
	// retained for targeted address/corruption tests, but production state is
	// exchanged in declaration order through one bounded cursor.
	internal static bool TryWriteStruct<TPlatform>(ref TPlatform platform,
		APTR storage, MuiAreaDragState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, storage,
			MuiAreaDragState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Source) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Target) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.LastX)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.LastY)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Qualifier) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.EventFlags) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Flags)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR storage, out MuiAreaDragState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, storage,
			MuiAreaDragState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Source) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Target) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawLastX) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawLastY) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Qualifier) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.EventFlags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Flags) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.LastX = unchecked((int)rawLastX);
		value.LastY = unchecked((int)rawLastY);
		return true;
	}

	internal static bool TryReadStruct<TPlatform>(ref TPlatform platform,
		APTR storage, out MuiAreaDragState value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, storage, out value) &&
		value.Magic == Cookie;

	internal static void Write<TPlatform>(ref TPlatform platform, APTR storage,
		MuiAreaDragState value) where TPlatform : struct, IMuiGuestMemory
	{
		_ = TryWriteStruct(ref platform, storage, value);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR storage,
		out MuiAreaDragState value) where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadStruct(ref platform, storage, out value);
	}

	internal static void Clear<TPlatform>(ref TPlatform platform, APTR storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (storage.IsNull || !platform.IsMapped(storage, MuiAreaDragState.Size))
			return;
		platform.Clear(storage, MuiAreaDragState.Size);
	}
}

// First MorphOS Area drag slice. This owns the fixed method-family defaults,
// guest-resident source state record, and typed provider-owned drag-image and
// external-routing capabilities. Intuition pointer capture remains a separate
// progressive seam so local controls can retain deterministic drag behavior.
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

	// Build the provider-owned request as one named value. Keeping source and
	// coordinates together avoids a platform adapter having to reconstruct the
	// capture ABI from positional arguments or guest offsets.
	public static bool BuildPointerCaptureSample(APTR source, int x, int y,
		out MuiPointerCaptureSample sample)
	{
		sample = default;
		if (source.IsNull) return false;
		sample.Object = source;
		sample.Kind = MuiPointerCaptureKind.AreaDrag;
		sample.StartX = x;
		sample.StartY = y;
		return true;
	}

	// Build the complete named route sample for MorphOS MUIM_DoDrag.  This is
	// also the small freestanding boundary used by native qualification; the
	// dispatcher never asks a provider to recover touch coordinates from an
	// untyped packet address.
	public static bool BuildDoDragRouteSample(APTR source, int touchX,
		int touchY, uint flags, out MuiDragRouteSample sample)
	{
		sample = default;
		if (source.IsNull) return false;
		sample.Phase = MuiDragRoutePhase.Begin;
		sample.Source = source;
		sample.X = touchX;
		sample.Y = touchY;
		sample.Flags = flags;
		sample.Result = 1;
		return true;
	}

	public static bool IsDragMethod(uint method) =>
		MuiAreaDragMessageCodec.IsMethod(method);

	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiAreaDragMessageCodec.TryReadMethodId(ref platform, message,
			out var methodHeader)) return 0;
		switch (methodHeader.MethodId)
		{
			case MuiAreaDragMessageCodec.DoDrag:
				if (!MuiAreaDragMessageCodec.TryReadDoDrag(ref platform, message,
					out var doDrag)) return 0;
				return DoDrag(ref platform, state, obj, doDrag);
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
			case MuiAreaDragMessageCodec.CreateDragImage:
				if (!MuiAreaDragMessageCodec.TryReadCreateDragImage(ref platform,
					message, out var createDragImage)) return 0;
				return CreateDragImage(ref platform, state, obj, createDragImage).Raw;
			case MuiAreaDragMessageCodec.DeleteDragImage:
				if (!MuiAreaDragMessageCodec.TryReadDeleteDragImage(ref platform,
					message, out var deleteDragImage)) return 0;
				return DeleteDragImage(ref platform, state, obj, deleteDragImage);
		}
		return 0;
	}

	internal static uint Begin<TPlatform>(ref TPlatform platform, APTR state,
		APTR source) where TPlatform : struct, IMuiHeadlessPlatform
		=> Begin(ref platform, state, source, 0, 0, 0);

	// MUIM_DoDrag is the handle-oriented entry point in MorphOS MUI.  Its
	// receiver is the drag source, while the packet carries the initial touch
	// coordinates and flags.  Keep those values in the named route sample so a
	// native provider can implement synchronous or asynchronous policy without
	// reconstructing an ABI from positional arguments.
	internal static uint DoDrag<TPlatform>(ref TPlatform platform, APTR state,
		APTR source, MuiAreaDoDragMessage packet)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (source.IsNull || MuiHeadlessObjectCore.FindObject(ref platform, state,
			source).IsNull)
			return 0;
		return Begin(ref platform, state, source, packet.TouchX, packet.TouchY,
			packet.Flags, true);
	}

	private static uint Begin<TPlatform>(ref TPlatform platform, APTR state,
		APTR source, int touchX, int touchY, uint flags, bool hasTouchInput = false)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (source.IsNull || !TryIsEnabled(ref platform, state, source,
			Draggable, out var sourceEnabled) || !sourceEnabled)
			return 0;
		var route = default(MuiDragRouteSample);
		if (hasTouchInput)
		{
			if (!BuildDoDragRouteSample(source, touchX, touchY, flags,
				out route)) return 0;
		}
		else
		{
			route.Phase = MuiDragRoutePhase.Begin;
			route.Source = source;
			route.Result = 1;
		}
		if (TryRoute(ref platform, ref route) && route.Result == 0)
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

	internal static APTR CreateDragImage<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaCreateDragImageMessage packet)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return APTR.Null;
		var sample = default(MuiDragImageCreateSample);
		sample.Object = obj;
		sample.TouchX = packet.TouchX;
		sample.TouchY = packet.TouchY;
		sample.Flags = packet.Flags;
		if (!platform.CreateMuiDragImage(ref sample)) return APTR.Null;
		if (sample.Object != obj || sample.TouchX != packet.TouchX ||
			sample.TouchY != packet.TouchY || sample.Flags != packet.Flags)
			return APTR.Null;
		return sample.Result;
	}

	internal static uint DeleteDragImage<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaDeleteDragImageMessage packet)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return 0;
		var sample = default(MuiDragImageDeleteSample);
		sample.Object = obj;
		sample.DragImage = APTR.FromPointer(packet.DragImage);
		if (!platform.DeleteMuiDragImage(ref sample)) return 0;
		if (sample.Object != obj || sample.DragImage !=
			APTR.FromPointer(packet.DragImage)) return 0;
		return 1;
	}

	internal static uint Query<TPlatform>(ref TPlatform platform, APTR state,
		APTR target, MuiAreaDragQueryMessage packet)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var source = APTR.FromPointer(packet.Object);
		if (source.IsNull || target.IsNull ||
			!TryIsEnabled(ref platform, state, source, Draggable,
				out var sourceEnabled) || !sourceEnabled ||
			!TryIsEnabled(ref platform, state, target, Dropable,
				out var targetEnabled) || !targetEnabled)
			return QueryRefuse;
		var route = default(MuiDragRouteSample);
		route.Phase = MuiDragRoutePhase.Query;
		route.Source = source;
		route.Target = target;
		route.Result = QueryAccept;
		if (TryRoute(ref platform, ref route))
			return route.Result == 0 ? QueryRefuse : QueryAccept;
		return QueryAccept;
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
		var route = default(MuiDragRouteSample);
		route.Phase = MuiDragRoutePhase.Drop;
		route.Source = source;
		route.Target = target;
		route.X = packet.X;
		route.Y = packet.Y;
		route.Qualifier = packet.Qualifier;
		route.Result = 1;
		if (TryRoute(ref platform, ref route) && route.Result == 0)
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
		var route = default(MuiDragRouteSample);
		route.Phase = MuiDragRoutePhase.Event;
		route.Window = APTR.FromPointer(packet.Window);
		route.Source = source;
		route.DragImage = APTR.FromPointer(packet.DragImage);
		route.IntuiMessage = APTR.FromPointer(packet.IntuiMessage);
		route.MuiKey = packet.MuiKey;
		route.MousePointerType = packet.MousePointerType;
		route.Flags = packet.Flags;
		route.Result = 1;
		if (TryRoute(ref platform, ref route) && route.Result == 0)
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
		var route = default(MuiDragRouteSample);
		route.Phase = MuiDragRoutePhase.Report;
		route.Source = source;
		route.X = packet.X;
		route.Y = packet.Y;
		route.Update = packet.Update;
		route.Qualifier = packet.Qualifier;
		route.Result = ReportContinue;
		var routeResult = ReportContinue;
		if (TryRoute(ref platform, ref route))
		{
			if (route.Result > ReportRefresh) routeResult = ReportAbort;
			else routeResult = route.Result;
		}
		if (routeResult != ReportAbort &&
			(value.Flags & MuiAreaDragState.CapturedFlag) == 0 &&
			CapturePointer(ref platform, source, packet.X, packet.Y))
			value.Flags |= MuiAreaDragState.CapturedFlag;
		value.LastX = packet.X;
		value.LastY = packet.Y;
		value.Qualifier = packet.Qualifier;
		value.EventFlags = unchecked((uint)packet.Update);
		value.Flags |= MuiAreaDragState.ReportedFlag;
		MuiAreaDragStateCodec.Write(ref platform, storage, value);
		return routeResult;
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
		var route = default(MuiDragRouteSample);
		route.Phase = MuiDragRoutePhase.Finish;
		route.Source = source;
		route.DropFollows = packet.DropFollows;
		route.Result = 1;
		var routed = TryRoute(ref platform, ref route);
		if ((value.Flags & MuiAreaDragState.CapturedFlag) != 0)
			ReleasePointer(ref platform, source, value.LastX, value.LastY);
		ReleaseState(ref platform, state, source, storage);
		return routed && route.Result == 0 ? 0u : 1u;
	}

	// Object disposal is a second terminal path for an active drag.  A guest
	// source can disappear without receiving MUIM_DragFinish, so release the
	// typed state block before the generic attribute list is reclaimed.
	internal static bool Cleanup<TPlatform>(ref TPlatform platform, APTR state,
		APTR source) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (source.IsNull || !MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, source, StateKey, out var raw)) return true;
		var storage = APTR.FromPointer(raw);
		if (storage.IsNull)
			return MuiHeadlessObjectCore.SetExistingAttribute(ref platform, state,
				source, StateKey, 0);
		if (MuiAreaDragStateCodec.TryRead(ref platform, storage, out var value) &&
			(value.Flags & MuiAreaDragState.CapturedFlag) != 0)
			ReleasePointer(ref platform, source, value.LastX, value.LastY);
		ReleaseState(ref platform, state, source, storage);
		return true;
	}

	private static bool CapturePointer<TPlatform>(ref TPlatform platform,
		APTR source, int x, int y)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!BuildPointerCaptureSample(source, x, y, out var sample))
			return false;
		return platform.CaptureMuiPointer(ref sample);
	}

	private static void ReleasePointer<TPlatform>(ref TPlatform platform,
		APTR source, int x, int y)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (BuildPointerCaptureSample(source, x, y, out var sample))
			_ = platform.ReleaseMuiPointer(ref sample);
	}

	// The provider receives a complete value-type sample.  Only Result is an
	// output; rejecting identity changes keeps an accidental native ABI mismatch
	// from redirecting a drag to a different guest object or message.
	private static bool TryRoute<TPlatform>(ref TPlatform platform,
		ref MuiDragRouteSample sample)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var expected = sample;
		if (!platform.RouteMuiDrag(ref sample)) return false;
		if (sample.Phase != expected.Phase || sample.Window != expected.Window ||
			sample.Source != expected.Source || sample.Target != expected.Target ||
			sample.DragImage != expected.DragImage ||
			sample.IntuiMessage != expected.IntuiMessage ||
			sample.X != expected.X || sample.Y != expected.Y ||
			sample.Update != expected.Update ||
			sample.DropFollows != expected.DropFollows ||
			sample.MuiKey != expected.MuiKey ||
			sample.MousePointerType != expected.MousePointerType ||
			sample.Qualifier != expected.Qualifier ||
			sample.Flags != expected.Flags)
		{
			sample.Result = 0;
		}
		return true;
	}

	internal static bool TryReadPolicyState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaDragPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (obj.IsNull || MuiHeadlessObjectCore.FindObject(ref platform, state,
			obj).IsNull) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PolicyStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			PolicyStateKey);
		if (block.IsNotNull || length != 0)
		{
			if (length != unchecked((int)MuiAreaDragPolicyStateRecord.Size) ||
				!MuiAreaDragPolicyStateRecordCodec.TryReadStructural(ref platform,
					block, out value) ||
				!MuiAreaDragPolicyStateAdmission.ValidateLive(ref platform, state,
					obj, value)) return false;
			var currentDraggable = ReadPolicyAttribute(ref platform, state, obj,
				Draggable, 0);
			var currentDropable = ReadPolicyAttribute(ref platform, state, obj,
				Dropable, 1);
			if (value.Draggable != currentDraggable || value.Dropable !=
				currentDropable)
			{
				value.Draggable = currentDraggable;
				value.Dropable = currentDropable;
				if (!MuiAreaDragPolicyStateRecordCodec.Write(ref platform, block,
					value)) return false;
			}
			return true;
		}
		var draggable = ReadPolicyAttribute(ref platform, state, obj, Draggable,
			0);
		var dropable = ReadPolicyAttribute(ref platform, state, obj, Dropable, 1);
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
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			PolicyStateKey);
		if ((block.IsNull && length == 0) || length !=
			unchecked((int)MuiAreaDragPolicyStateRecord.Size) ||
			!MuiAreaDragPolicyStateRecordCodec.TryReadStructural(ref platform, block,
				out value) || !MuiAreaDragPolicyStateAdmission.ValidateLive(ref platform,
				state, obj, value))
			return false;
		return true;
	}

	private static bool TryIsEnabled<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, out bool enabled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		enabled = false;
		if (obj.IsNull || MuiHeadlessObjectCore.FindObject(ref platform, state,
			obj).IsNull) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PolicyStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			PolicyStateKey);
		if (block.IsNotNull || length != 0)
		{
			if (length != unchecked((int)MuiAreaDragPolicyStateRecord.Size) ||
				!MuiAreaDragPolicyStateRecordCodec.TryReadStructural(ref platform,
					block, out var policy) ||
				!MuiAreaDragPolicyStateAdmission.ValidateLive(ref platform, state,
					obj, policy)) return false;
			enabled = (attribute == Draggable ? policy.Draggable : policy.Dropable) != 0;
			return true;
		}
		var defaultValue = attribute == Dropable ? 1u : 0u;
		enabled = ReadPolicyAttribute(ref platform, state, obj, attribute,
			defaultValue) != 0;
		return true;
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
