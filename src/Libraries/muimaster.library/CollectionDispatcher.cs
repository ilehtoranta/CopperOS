/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionMethodMessage
{
	public const uint Size = 4;
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionInsertSingleMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Entry;
	public uint Position;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionInsertMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint Entry;
	public uint Position;
	public uint Column;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionGetEntryMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Position;
	public uint Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionSelectMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint Position;
	public uint Select;
	public uint Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionPositionMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Position;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionPointerMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Pointer;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionPairMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint First;
	public uint Second;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionCreateImageMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Image;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionEntryPoolMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Entry;
	public uint Pool;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionDisplayMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint Entry;
	public uint Array;
	public uint Row;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionCompareMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint Entry1;
	public uint Entry2;
	public uint Column;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionTestPosMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint X;
	public uint Y;
	public uint Result;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionLayoutMessage
{
	public const uint Size = 20;
	public uint MethodId;
	public uint Left;
	public uint Top;
	public uint Width;
	public uint Height;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionAskMinMaxMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionDrawMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionHandleInputMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint IntuiMessage;
	public int MuiKey;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionAttributeMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Attribute;
	public uint Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionCreateEditObjectMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public int Row;
	public int Column;
	public uint Entry;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionEditMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public int Row;
	public int Column;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionEditDoneMessage
{
	public const uint Size = 20;
	public uint MethodId;
	public int Row;
	public int Column;
	public uint Entry;
	public uint EditObject;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionEndEditMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Mode;
}

// Routes the MG08 collection methods to the guest-resident List core. Methods
// are only claimed when the target object is exactly a List; everything else
// (including the shared Area/generic behaviour) falls through to the common
// control dispatcher so the collection layer composes with MG07 unchanged.
public static class MuiCollectionDispatcher
{
	// Public method identifiers (MUIConstants.Generated.cs / MUI_List.doc).
	private const uint ListClear = 0x8042ad89u;
	private const uint ListCompare = 0x80421b68u;
	private const uint ListConstruct = 0x8042d662u;
	private const uint ListCreateImage = 0x80429804u;
	internal const uint ListCreateEditObject = 0x804219aeu;
	private const uint ListDeleteImage = 0x80420f58u;
	private const uint ListDestruct = 0x80427d51u;
	private const uint ListDisplay = 0x80425377u;
	internal const uint ListEdit = 0x8042843du;
	internal const uint ListEditDone = 0x80423ab3u;
	internal const uint ListEndEdit = 0x804203eeu;
	private const uint ListExchange = 0x8042468cu;
	private const uint ListGetEntry = 0x804280ecu;
	private const uint ListInsert = 0x80426c87u;
	private const uint ListInsertSingle = 0x804254d5u;
	private const uint ListJump = 0x8042baabu;
	private const uint ListMove = 0x804253c2u;
	private const uint ListNextSelected = 0x80425f17u;
	private const uint ListRedraw = 0x80427993u;
	private const uint ListRemove = 0x8042647eu;
	private const uint ListSelect = 0x804252d8u;
	private const uint ListSort = 0x80422275u;
	private const uint ListSortEntries = 0x80429e32u;
	private const uint ListTestPos = 0x80425f48u;

	// MUIM_Floattext_Append plus the generic layout/set methods routed for the
	// Listview composite.
	private const uint Layout = 0x8042845Bu;
	private const uint Draw = 0x80426F3Fu;
	private const uint AskMinMax = 0x80423874u;
	private const uint HandleInput = 0x80422a1au;

	private const uint FloattextAppend = 0x8042a221u;
	private const uint OmGet = 0x00000104u;
	private const uint Set = 0x8042549Au;
	private const uint NoNotifySet = 0x8042216Fu;

	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message) where TPlatform : struct, IMuiLayoutPlatform
	{
		uint result;
		if (TryDispatch(ref platform, state, obj, message, out result))
			return result;
		return MuiCommonControlDispatcher.Dispatch(ref platform, state, obj,
		message);
	}

	// Focused struct-backed entry point for the List surface method family. The
	// complete dispatcher below remains the compatibility surface; this seam
	// claims only records whose fixed layouts are explicitly represented above.
	public static bool TryDispatchSurfacePacket<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message, out uint result)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		result = 0;
		if (!MuiCollectionBasicMessageCodec.TryReadMethodId(ref platform,
			message, out var surfaceMethod)) return false;
		var cls = MuiListCore.Classify(ref platform, state, obj);
		if (!MuiListCore.IsListBacked(cls)) return false;
		var method = surfaceMethod.MethodId;
		if (method == Layout)
		{
			if (!TryReadLayout(ref platform, message, out var layout)) return true;
			result = MuiListCore.Layout(ref platform, state, obj,
				unchecked((int)layout.Left), unchecked((int)layout.Top),
				unchecked((int)layout.Width), unchecked((int)layout.Height)) ? 1u : 0u;
			return true;
		}
		if (method == Draw)
		{
			if (!TryReadDraw(ref platform, message, out var draw)) return true;
			result = MuiListCore.Draw(ref platform, state, obj, draw.Flags)
				? 1u : 0u;
			return true;
		}
		if (method == AskMinMax)
		{
			if (!TryReadAskMinMax(ref platform, message, out var askMinMax))
				return true;
			result = MuiListCore.AskMinMax(ref platform, state, obj,
				APTR.FromPointer(askMinMax.Storage)) ? 1u : 0u;
			return true;
		}
		if (method == Set || method == NoNotifySet)
		{
			if (!TryReadAttribute(ref platform, message, method,
				out var attribute)) return true;
			result = MuiListCore.SetRuntimeAttribute(ref platform, state, obj,
				attribute.Attribute, attribute.Value, method == Set) ? 1u : 0u;
			return true;
		}
		return false;
	}

	public static bool TryDispatchPacket<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message, out uint result)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		result = 0;
		if (TryDispatchSurfacePacket(ref platform, state, obj, message,
			out result)) return true;
		if (!MuiCollectionBasicMessageCodec.TryReadMethodId(ref platform,
			message, out var packetMethod)) return false;
		var cls = MuiListCore.Classify(ref platform, state, obj);
		var method = packetMethod.MethodId;

		// Listview is a composite rather than a List-backed object. Keep its
		// forwarding and group methods on the same named-record packet path as
		// the direct List surface. This removes the old offset-based fallback for
		// the public layout/draw/min-max/set ABI while preserving the child-list
		// ownership rules.
		if (cls == MuiCollectionClass.Listview)
		{
			if (IsListMethod(method) || method == FloattextAppend)
			{
				var child = MuiListviewCore.ChildList(ref platform, state, obj);
				if (child.IsNull) return true;
				return TryDispatchPacket(ref platform, state, child, message,
					out result);
			}
			return TryDispatchCompositePacket(ref platform, state, obj,
				MuiCollectionClass.Listview, message, method, out result);
		}

		if (cls == MuiCollectionClass.Stringscroll)
			return TryDispatchCompositePacket(ref platform, state, obj, cls,
				message, method, out result);

		if (cls == MuiCollectionClass.Floattext &&
			(method == FloattextAppend || method == OmGet))
			return TryDispatchCompositePacket(ref platform, state, obj, cls,
				message, method, out result);

		if (cls != MuiCollectionClass.List &&
			cls != MuiCollectionClass.Floattext) return false;
		switch (method)
		{
			case ListInsertSingle:
				if (!TryReadInsertSingle(ref platform, message,
					out var insertSingle)) return true;
				result = MuiListCore.InsertSingle(ref platform, state, obj,
					APTR.FromPointer(insertSingle.Entry),
					unchecked((int)insertSingle.Position)) ? 1u : 0u;
				return true;
			case ListInsert:
				if (!TryReadInsert(ref platform, message, out var insert))
					return true;
				result = MuiListCore.Insert(ref platform, state, obj,
					APTR.FromPointer(insert.Entry),
					unchecked((int)insert.Position),
					unchecked((int)insert.Column)) ? 1u : 0u;
				return true;
			case ListGetEntry:
				if (!TryReadGetEntry(ref platform, message,
					out var getEntry)) return true;
				result = MuiListCore.GetEntry(ref platform, state, obj,
					unchecked((int)getEntry.Position),
					APTR.FromPointer(getEntry.Storage)).Raw;
				return true;
			case ListSelect:
				if (!TryReadSelect(ref platform, message, out var select))
					return true;
				result = MuiListCore.Select(ref platform, state, obj,
					unchecked((int)select.Position), select.Select,
					APTR.FromPointer(select.Storage)) ? 1u : 0u;
				return true;
			case ListNextSelected:
				if (!TryReadPointer(ref platform, message, ListNextSelected,
					out var nextSelected)) return true;
				result = MuiListCore.NextSelected(ref platform, state, obj,
					APTR.FromPointer(nextSelected.Pointer)) ? 1u : 0u;
				return true;
			case ListRemove:
				if (!TryReadPosition(ref platform, message, ListRemove,
					out var remove)) return true;
				result = MuiListCore.Remove(ref platform, state, obj,
					unchecked((int)remove.Position)) ? 1u : 0u;
				return true;
			case ListClear:
				if (!IsValidMethod(ref platform, message, ListClear)) return true;
				result = MuiListCore.Clear(ref platform, state, obj) ? 1u : 0u;
				return true;
			case ListSort:
				if (!IsValidMethod(ref platform, message, ListSort)) return true;
				result = MuiListCore.Sort(ref platform, state, obj) ? 1u : 0u;
				return true;
			case ListSortEntries:
				if (!TryReadPointer(ref platform, message, ListSortEntries,
					out var sortEntries)) return true;
				result = MuiListCore.SortEntries(ref platform, state, obj,
					APTR.FromPointer(sortEntries.Pointer)) ? 1u : 0u;
				return true;
			case ListMove:
			case ListExchange:
				if (!TryReadPair(ref platform, message, method,
					out var pair)) return true;
				result = (method == ListMove
					? MuiListCore.Move(ref platform, state, obj,
						unchecked((int)pair.First), unchecked((int)pair.Second))
					: MuiListCore.Exchange(ref platform, state, obj,
						unchecked((int)pair.First), unchecked((int)pair.Second)))
					? 1u : 0u;
				return true;
			case ListJump:
			case ListRedraw:
				if (!TryReadPosition(ref platform, message, method,
					out var position)) return true;
				result = (method == ListJump
					? MuiListCore.Jump(ref platform, state, obj,
						unchecked((int)position.Position))
					: MuiListCore.Redraw(ref platform, state, obj,
						unchecked((int)position.Position))) ? 1u : 0u;
				return true;
			case ListCreateImage:
				if (!TryReadCreateImage(ref platform, message,
					out var createImage)) return true;
				result = MuiListCore.CreateImage(ref platform, state, obj,
					APTR.FromPointer(createImage.Image), createImage.Flags).Raw;
				return true;
			case ListCreateEditObject:
				if (!TryReadCreateEditObject(ref platform, message,
					out var createEditObject)) return true;
				result = MuiListCore.CreateEditObject(ref platform, state, obj,
					createEditObject.Row, createEditObject.Column,
					APTR.FromPointer(createEditObject.Entry)).Raw;
				return true;
			case ListEdit:
				if (!TryReadEdit(ref platform, message, out var edit)) return true;
				result = MuiListCore.Edit(ref platform, state, obj,
					edit.Row, edit.Column) ? 1u : 0u;
				return true;
			case ListEditDone:
				if (!TryReadEditDone(ref platform, message,
					out var editDone)) return true;
				result = MuiListCore.EditDone(ref platform, state, obj,
					editDone.Row, editDone.Column,
					APTR.FromPointer(editDone.Entry),
					APTR.FromPointer(editDone.EditObject)) ? 1u : 0u;
				return true;
			case ListEndEdit:
				if (!TryReadEndEdit(ref platform, message,
					out var endEdit)) return true;
				result = MuiListCore.EndEdit(ref platform, state, obj,
					endEdit.Mode) ? 1u : 0u;
				return true;
			case ListDeleteImage:
				if (!TryReadPointer(ref platform, message, ListDeleteImage,
					out var deleteImage)) return true;
				result = MuiListCore.DeleteImage(ref platform, state, obj,
					APTR.FromPointer(deleteImage.Pointer)) ? 1u : 0u;
				return true;
			case ListConstruct:
				if (!TryReadEntryPool(ref platform, message, ListConstruct,
					out var construct)) return true;
				result = MuiListCore.Construct(ref platform, state, obj,
					APTR.FromPointer(construct.Entry),
					APTR.FromPointer(construct.Pool), out _).Raw;
				return true;
			case ListDestruct:
				if (!TryReadEntryPool(ref platform, message, ListDestruct,
					out var destruct)) return true;
				MuiListCore.Destruct(ref platform, state, obj,
					APTR.FromPointer(destruct.Entry), 0,
					APTR.FromPointer(destruct.Pool));
				result = 1u;
				return true;
			case ListDisplay:
				if (!TryReadDisplay(ref platform, message, out var display))
					return true;
				result = MuiListCore.Display(ref platform, state, obj,
					APTR.FromPointer(display.Entry),
					APTR.FromPointer(display.Array),
					unchecked((int)display.Row)) ? 1u : 0u;
				return true;
			case ListCompare:
				if (!TryReadCompare(ref platform, message, out var compare))
					return true;
				result = unchecked((uint)MuiListCore.Compare(ref platform, state,
					obj, APTR.FromPointer(compare.Entry1),
					APTR.FromPointer(compare.Entry2), compare.Column));
				return true;
			case ListTestPos:
				if (!TryReadTestPos(ref platform, message, out var testPos))
					return true;
				result = MuiListCore.TestPos(ref platform, state, obj,
					unchecked((int)testPos.X), unchecked((int)testPos.Y),
					APTR.FromPointer(testPos.Result)) ? 1u : 0u;
				return true;
		}
		return false;
	}

	// Named-record route for the composite collection classes. A recognized
	// method remains claimed even when its packet is truncated, matching the
	// legacy dispatcher contract while ensuring malformed guest packets cannot
	// reach an unchecked offset reader.
	private static bool TryDispatchCompositePacket<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, MuiCollectionClass cls,
		APTR message, uint method, out uint result)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		result = 0;
		switch (method)
		{
			case Layout:
				if (!TryReadLayout(ref platform, message, out var layout)) return true;
				result = (cls == MuiCollectionClass.Listview
					? MuiListviewCore.Layout(ref platform, state, obj,
						unchecked((int)layout.Left), unchecked((int)layout.Top),
						unchecked((int)layout.Width), unchecked((int)layout.Height))
					: MuiStringscrollCore.Layout(ref platform, state, obj,
						unchecked((int)layout.Left), unchecked((int)layout.Top),
						unchecked((int)layout.Width), unchecked((int)layout.Height)))
					? 1u : 0u;
				return true;
			case Draw:
				if (!TryReadDraw(ref platform, message, out var draw)) return true;
				result = (cls == MuiCollectionClass.Listview
					? MuiListviewCore.Draw(ref platform, state, obj, draw.Flags)
					: MuiStringscrollCore.Draw(ref platform, state, obj, draw.Flags))
					? 1u : 0u;
				return true;
			case HandleInput:
				if (cls != MuiCollectionClass.Listview &&
					cls != MuiCollectionClass.Stringscroll) return false;
				if (!TryReadHandleInput(ref platform, message, out var input))
					return true;
				result = (cls == MuiCollectionClass.Listview
					? MuiListviewCore.HandleInput(ref platform, state, obj,
						APTR.FromPointer(input.IntuiMessage), input.MuiKey)
					: MuiStringscrollCore.HandleInput(ref platform, state, obj,
						APTR.FromPointer(input.IntuiMessage), input.MuiKey)) ? 1u : 0u;
				return true;
			case AskMinMax:
				if (!TryReadAskMinMax(ref platform, message, out var askMinMax))
					return true;
				result = (cls == MuiCollectionClass.Listview
					? MuiListviewCore.AskMinMax(ref platform, state, obj,
						APTR.FromPointer(askMinMax.Storage))
					: MuiStringscrollCore.AskMinMax(ref platform, state, obj,
						APTR.FromPointer(askMinMax.Storage))) ? 1u : 0u;
				return true;
			case OmGet:
				if (cls != MuiCollectionClass.Listview &&
					cls != MuiCollectionClass.Floattext &&
					cls != MuiCollectionClass.Stringscroll) return false;
				if (!MuiCommonControlPacketCore.TryReadGet(ref platform, message,
					out var getPacket)) return true;
				var storage = APTR.FromPointer(getPacket.Storage);
				if (storage.IsNull || !platform.IsMapped(storage,
					MuiGuestUlongStorage.Size)) return true;
				uint value;
				var got = cls == MuiCollectionClass.Listview
					? MuiListviewCore.GetAttribute(ref platform, state, obj,
						getPacket.Attribute, out value)
					: cls == MuiCollectionClass.Floattext
						? MuiFloattextCore.GetAttribute(ref platform, state, obj,
							getPacket.Attribute, out value)
						: MuiStringscrollCore.GetAttribute(ref platform, state, obj,
							getPacket.Attribute, out value);
				if (!got) return true;
				MuiGuestUlongStorageCodec.WriteValue(ref platform, storage, value);
				result = 1u;
				return true;
			case Set:
			case NoNotifySet:
				if (!TryReadAttribute(ref platform, message, method,
					out var attribute)) return true;
				result = (cls == MuiCollectionClass.Listview
					? MuiListviewCore.SetAttribute(ref platform, state, obj,
						attribute.Attribute, attribute.Value, method == Set)
					: MuiStringscrollCore.SetAttribute(ref platform, state, obj,
						attribute.Attribute, attribute.Value, method == Set))
					? 1u : 0u;
				return true;
			case FloattextAppend:
				if (cls != MuiCollectionClass.Floattext) return false;
				if (!TryReadPointer(ref platform, message, FloattextAppend,
					out var append)) return true;
				result = MuiFloattextCore.Append(ref platform, state, obj,
					APTR.FromPointer(append.Pointer)) ? 1u : 0u;
				return true;
		}
		return false;
	}

	// Dispatch only the collection layer through named packet codecs. Returning false means the method is
	// not claimed by the collection classes for this object and lets an outer
	// dispatcher continue without creating a Collection -> Common -> Layout
	// recursion. Listview forwards list methods to its child and owns the group
	// layout/draw/min-max/set methods; Floattext adds MUIM_Floattext_Append on
	// top of the shared List backbone.
	public static bool TryDispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message, out uint result)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		// All fixed collection packets are decoded by TryDispatchPacket. Keep
		// this outer entry point as the collection-layer claim boundary and let
		// unclaimed objects continue to the common-control dispatcher without
		// re-reading guest words through legacy offsets.
		return TryDispatchPacket(ref platform, state, obj, message, out result);
	}


	private static bool IsListMethod(uint method) =>
		method == ListClear || method == ListCompare || method == ListConstruct ||
		method == ListDestruct || method == ListDisplay ||
		method == ListCreateImage || method == ListCreateEditObject ||
		method == ListDeleteImage || method == ListEdit ||
		method == ListEditDone || method == ListEndEdit || method == ListExchange ||
		method == ListGetEntry || method == ListInsert ||
		method == ListInsertSingle || method == ListJump || method == ListMove ||
		method == ListNextSelected || method == ListRedraw || method == ListRemove ||
		method == ListSelect || method == ListSort || method == ListSortEntries ||
		method == ListTestPos;

	private static bool IsValidMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionBasicMessageCodec.IsValidMethod(ref platform, message,
			method);

	private static bool TryReadInsertSingle<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionInsertSingleMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionAdvancedMessageCodec.TryReadInsertSingle(ref platform,
			message, out packet);

	private static bool TryReadInsert<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionInsertMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionAdvancedMessageCodec.TryReadInsert(ref platform, message,
			out packet);

	private static bool TryReadGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionGetEntryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionBasicMessageCodec.TryReadGetEntry(ref platform, message,
			out packet);

	private static bool TryReadSelect<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionSelectMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionBasicMessageCodec.TryReadSelect(ref platform, message,
			out packet);

	private static bool TryReadPosition<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCollectionPositionMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionAdvancedMessageCodec.TryReadPosition(ref platform,
			message, method, out packet);

	private static bool TryReadPointer<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCollectionPointerMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionAdvancedMessageCodec.TryReadPointer(ref platform, message,
			method, out packet);

	private static bool TryReadPair<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCollectionPairMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionAdvancedMessageCodec.TryReadPair(ref platform, message,
			method, out packet);

	private static bool TryReadCreateImage<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionCreateImageMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionAdvancedMessageCodec.TryReadCreateImage(ref platform,
			message, out packet);

	private static bool TryReadCreateEditObject<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionCreateEditObjectMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionEditMessageCodec.TryReadCreateEditObject(ref platform,
			message, out packet);

	private static bool TryReadEdit<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionEditMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionEditMessageCodec.TryReadEdit(ref platform, message,
			out packet);

	private static bool TryReadEditDone<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionEditDoneMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionEditMessageCodec.TryReadEditDone(ref platform, message,
			out packet);

	private static bool TryReadEndEdit<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionEndEditMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionEditMessageCodec.TryReadEndEdit(ref platform, message,
			out packet);

	// Native qualification helper for the fixed MorphOS 3.20 edit records.
	// Production callers use TryDispatchPacket, which applies the List state
	// machine after decoding these same structs.
	internal static bool TryReadCreateEditObjectPacket<TPlatform>(
		ref TPlatform platform, APTR message,
		out MuiCollectionCreateEditObjectMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadCreateEditObject(ref platform, message, out packet);

	internal static bool TryReadEditPacket<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionEditMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadEdit(ref platform, message, out packet);

	internal static bool TryReadEditDonePacket<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionEditDoneMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadEditDone(ref platform, message, out packet);

	internal static bool TryReadEndEditPacket<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionEndEditMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadEndEdit(ref platform, message, out packet);

	private static bool TryReadEntryPool<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCollectionEntryPoolMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionRecordMessageCodec.TryReadEntryPool(ref platform,
			message, method, out packet);

	private static bool TryReadDisplay<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionDisplayMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionRecordMessageCodec.TryReadDisplay(ref platform, message,
			out packet);

	private static bool TryReadCompare<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionCompareMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionRecordMessageCodec.TryReadCompare(ref platform, message,
			out packet);

	private static bool TryReadTestPos<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionTestPosMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionRecordMessageCodec.TryReadTestPos(ref platform, message,
			out packet);

	private static bool TryReadLayout<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionLayoutMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionSurfaceMessageCodec.TryReadLayout(ref platform, message,
			out packet);

	// Native qualification helper: the fixed record decoder is exposed without
	// pulling the generic graphics/layout dispatcher closure into a constructed
	// 68020 relocation path. Production callers use TryDispatchSurfacePacket.
	internal static bool TryReadLayoutPacket<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionLayoutMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadLayout(ref platform, message, out packet);

	private static bool TryReadAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionAskMinMaxMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionSurfaceMessageCodec.TryReadAskMinMax(ref platform,
			message, out packet);

	internal static bool TryReadAskMinMaxPacket<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionAskMinMaxMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadAskMinMax(ref platform, message, out packet);

	private static bool TryReadDraw<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionDrawMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionSurfaceMessageCodec.TryReadDraw(ref platform, message,
			out packet);

	private static bool TryReadHandleInput<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionHandleInputMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionSurfaceMessageCodec.TryReadHandleInput(ref platform,
			message, out packet);

	private static bool TryReadAttribute<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCollectionAttributeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCollectionSurfaceMessageCodec.TryReadAttribute(ref platform,
			message, method, out packet);

	// Native qualification helpers for the remaining composite surface records.
	// The live dispatcher above consumes these same named structs; these narrow
	// entry points let freestanding roots validate packet shape without pulling
	// the full graphics closure into a packet-only artifact.
	internal static bool TryReadDrawPacket<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionDrawMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadDraw(ref platform, message, out packet);

	internal static bool TryReadHandleInputPacket<TPlatform>(
		ref TPlatform platform, APTR message,
		out MuiCollectionHandleInputMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadHandleInput(ref platform, message, out packet);

	internal static bool TryReadAttributePacket<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCollectionAttributeMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadAttribute(ref platform, message, method, out packet);
}
