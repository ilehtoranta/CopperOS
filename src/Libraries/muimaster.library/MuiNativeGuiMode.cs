/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;
using MuiConstants = Amiga.MUI.MUIConstants;

namespace CopperOS.MuiMaster;

// Intuition's NextObject API receives a pointer to this one-field iterator
// record. Keeping the state named avoids treating the caller's storage as an
// anonymous ULONG offset.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeNextObjectStateRecord
{
	internal const uint Size = 4;
	internal APTR Current;
}

internal static class MuiNativeNextObjectStateCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiNativeNextObjectStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNativeNextObjectStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Current.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeAreaRectangle
{
	internal int Left;
	internal int Top;
	internal int Width;
	internal int Height;
}

// Public Area insets define the content rectangle inside an object's frame.
// Keep them together as signed LONG values instead of re-reading positional
// fields or treating these values as anonymous offsets.
internal struct MuiNativeAreaInsets
{
	internal int Left;
	internal int Top;
	internal int Right;
	internal int Bottom;
}

// Native counterpart of ApplicationWindowCore's GUI-mode page and
// virtual-group visibility checks. Values are obtained through public MUI
// attributes; child enumeration uses the public Group ChildList and
// Intuition.NextObject contract rather than inspecting BOOPSI object memory.
internal static class MuiNativeGuiMode
{
	internal const uint FamilyList = MuiConstants.MUIA_Family_List;
	internal const uint GroupPageMode = MuiConstants.MUIA_Group_PageMode;
	internal const uint GroupActivePage = MuiConstants.MUIA_Group_ActivePage;
	internal const uint GroupChildList = MuiConstants.MUIA_Group_ChildList;
	internal const uint VirtgroupWidth = MuiConstants.MUIA_Virtgroup_Width;
	internal const uint VirtgroupHeight = MuiConstants.MUIA_Virtgroup_Height;
	internal const uint VirtgroupLeft = MuiConstants.MUIA_Virtgroup_Left;
	internal const uint VirtgroupTop = MuiConstants.MUIA_Virtgroup_Top;
	internal const uint AreaLeftEdge = MuiConstants.MUIA_LeftEdge;
	internal const uint AreaTopEdge = MuiConstants.MUIA_TopEdge;
	internal const uint AreaWidth = MuiConstants.MUIA_Width;
	internal const uint AreaHeight = MuiConstants.MUIA_Height;
	internal const uint AreaInnerLeft = MuiConstants.MUIA_InnerLeft;
	internal const uint AreaInnerTop = MuiConstants.MUIA_InnerTop;
	internal const uint AreaInnerRight = MuiConstants.MUIA_InnerRight;
	internal const uint AreaInnerBottom = MuiConstants.MUIA_InnerBottom;
	private const uint IsShownAttribute = 0x7FFF0003;

	// Pointer hit testing uses the same page-mode and virtual-group visibility
	// rules as GUI-mode event delivery, but does not exclude disabled objects:
	// MouseObject identifies what is beneath the pointer, not what can accept
	// input. NeedsMouseObject is obsolete on MorphOS MUI 4 and later.
	internal static bool IsVisibleForPointer(
		ref MuiNativeClassPlatform platform, APTR publicObjects,
		APTR ownerRoot, APTR windowObject, APTR obj, APTR storage)
	{
		var memory = default(MuiNativeClassMemory);
		var current = obj;
		uint visited = 0;
		while (current.IsNotNull && current != windowObject && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
				ownerRoot, current, out var binding)) return false;
			var explicitlyHidden = MuiNativeObjectStateCore.TryGetAttribute(
				ref memory, binding.Sidecar, MuiCommonControlCore.ShowMe,
				out var showMe) && showMe == 0;
			var notShown = MuiNativeObjectStateCore.TryGetAttribute(ref memory,
				binding.Sidecar, IsShownAttribute, out var isShown) && isShown == 0;
			if (explicitlyHidden || notShown) return false;
			var parent = binding.Parent;
			if (parent.IsNull || !PageAllows(ref platform, parent, current,
				storage) || !VirtualGroupAllows(ref platform, parent, current,
					storage)) return false;
			current = parent;
		}
		return current == windowObject;
	}

	internal static bool Allows(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR obj, uint eventClass,
		APTR storage)
	{
		var allowedStateEvent = (eventClass & (
			MuiNativeWindowEventHandlerQueue.EventClassActiveWindow |
			MuiNativeWindowEventHandlerQueue.EventClassInactiveWindow |
			MuiNativeWindowEventHandlerQueue.EventClassChangeWindow)) != 0;
		if (allowedStateEvent) return true;

		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeWindowEventHandlerQueue.GuiModeAllows(ref memory,
			publicObjects, ownerRoot, obj, eventClass) ||
			platform.IntuitionBase.IsNull || storage.IsNull) return false;

		var current = obj;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
				ownerRoot, current, out var childBinding)) return false;
			var parent = childBinding.Parent;
			if (parent.IsNull) return true;
			if (!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
				ownerRoot, parent, out _)) return false;
			if (!PageAllows(ref platform, parent, current, storage) ||
				!VirtualGroupAllows(ref platform, parent, current, storage))
				return false;
			current = parent;
		}
		return current.IsNull;
	}

	private static bool PageAllows(ref MuiNativeClassPlatform platform,
		APTR parent, APTR child, APTR storage)
	{
		if (!TryGetAttribute(ref platform, parent, GroupPageMode, storage,
			out var pageMode) || pageMode == 0) return true;
		if (!TryGetAttribute(ref platform, parent, GroupChildList, storage,
			out var childListRaw)) return false;
		var childList = APTR.FromPointer(childListRaw);
		var memory = default(MuiNativeClassMemory);
		if (!MuiGroupExecListCodec.TryRead(ref memory, childList,
			out var list)) return false;

		var iterator = default(MuiNativeNextObjectStateRecord);
		iterator.Current = list.Head;
		if (!MuiNativeNextObjectStateCodec.Write(ref platform, storage,
			iterator)) return false;
		uint childCount = 0;
		var childIndex = uint.MaxValue;
		while (childCount < MuiHeadlessLayout.MaximumTraversal)
		{
			var currentChild = MuiNativeIntuitionCalls.NextObject(
				platform.IntuitionBase, storage);
			if (currentChild.IsNull) break;
			if (currentChild == child) childIndex = childCount;
			childCount++;
		}
		if (childCount == MuiHeadlessLayout.MaximumTraversal ||
			childCount == 0 || childIndex == uint.MaxValue) return false;

		var activePage = 0u;
		if (TryGetAttribute(ref platform, parent, GroupActivePage, storage,
			out var activePageValue)) activePage = activePageValue;
		if (activePage >= childCount) activePage = 0;
		return childIndex == activePage;
	}

	// The public Group child list is the live ordering source when two equal-
	// depth Areas overlap. MorphOS requires callers to traverse this list with
	// intuition.library/NextObject; the child-list order also reflects public
	// Group reordering, unlike the library-owned object registration order.
	internal static bool IsLaterGroupChildInList(
		ref MuiNativeClassPlatform platform, APTR group, APTR candidate,
		APTR incumbent, APTR storage)
	{
		if (group.IsNull || candidate.IsNull || incumbent.IsNull ||
			candidate == incumbent || platform.IntuitionBase.IsNull ||
			!TryGetAttribute(ref platform, group, GroupChildList, storage,
				out var childListRaw)) return false;
		var childList = APTR.FromPointer(childListRaw);
		var memory = default(MuiNativeClassMemory);
		if (!MuiGroupExecListCodec.TryRead(ref memory, childList,
			out var list)) return false;

		var iterator = default(MuiNativeNextObjectStateRecord);
		iterator.Current = list.Head;
		if (!MuiNativeNextObjectStateCodec.Write(ref platform, storage,
			iterator)) return false;
		var candidateIndex = uint.MaxValue;
		var incumbentIndex = uint.MaxValue;
		var childCount = 0u;
		while (childCount < MuiHeadlessLayout.MaximumTraversal)
		{
			var child = MuiNativeIntuitionCalls.NextObject(platform.IntuitionBase,
				storage);
			if (child.IsNull) break;
			if (child == candidate) candidateIndex = childCount;
			if (child == incumbent) incumbentIndex = childCount;
			childCount++;
		}
		return childCount < MuiHeadlessLayout.MaximumTraversal &&
			candidateIndex != uint.MaxValue && incumbentIndex != uint.MaxValue &&
			IsLaterGroupChildPosition(candidateIndex, incumbentIndex);
	}

	internal static bool IsLaterGroupChildPosition(uint candidatePosition,
		uint incumbentPosition) => candidatePosition > incumbentPosition;

	private static bool VirtualGroupAllows(
		ref MuiNativeClassPlatform platform, APTR parent, APTR child,
		APTR storage)
	{
		if (!TryGetAttribute(ref platform, parent, VirtgroupWidth, storage,
			out _) || !TryGetAttribute(ref platform, parent, VirtgroupHeight,
			storage, out _)) return true;
		if (!TryReadAreaRectangle(ref platform, child, storage,
			out var childRectangle) ||
			!TryReadInnerAreaRectangle(ref platform, parent, storage,
				out var parentRectangle)) return true;
		return RectanglesIntersect(childRectangle, parentRectangle);
	}

	internal static bool TryReadAreaRectangle(
		ref MuiNativeClassPlatform platform, APTR obj, APTR storage,
		out MuiNativeAreaRectangle rectangle)
	{
		rectangle = default;
		if (!TryGetAttribute(ref platform, obj, AreaLeftEdge, storage,
			out var left) || !TryGetAttribute(ref platform, obj, AreaTopEdge,
				storage, out var top) ||
			!TryGetAttribute(ref platform, obj, AreaWidth, storage,
				out var width) || !TryGetAttribute(ref platform, obj, AreaHeight,
				storage, out var height)) return false;
		rectangle.Left = unchecked((int)left);
		rectangle.Top = unchecked((int)top);
		rectangle.Width = unchecked((int)width);
		rectangle.Height = unchecked((int)height);
		return true;
	}

	internal static bool TryReadInnerAreaRectangle(
		ref MuiNativeClassPlatform platform, APTR obj, APTR storage,
		out MuiNativeAreaRectangle rectangle)
	{
		rectangle = default;
		if (!TryReadAreaRectangle(ref platform, obj, storage, out var outer) ||
			!TryGetAttribute(ref platform, obj, AreaInnerLeft, storage,
				out var left) ||
			!TryGetAttribute(ref platform, obj, AreaInnerTop, storage,
				out var top) ||
			!TryGetAttribute(ref platform, obj, AreaInnerRight, storage,
				out var right) ||
			!TryGetAttribute(ref platform, obj, AreaInnerBottom, storage,
				out var bottom)) return false;

		var insets = new MuiNativeAreaInsets
		{
			Left = unchecked((int)left),
			Top = unchecked((int)top),
			Right = unchecked((int)right),
			Bottom = unchecked((int)bottom),
		};
		return TryInsetRectangle(outer, insets, out rectangle);
	}

	internal static bool TryInsetRectangle(MuiNativeAreaRectangle outer,
		MuiNativeAreaInsets insets, out MuiNativeAreaRectangle inner)
	{
		inner = default;
		if (outer.Width < 0 || outer.Height < 0 ||
			!TryAddSigned(outer.Left, insets.Left, out var left) ||
			!TryAddSigned(outer.Top, insets.Top, out var top) ||
			!TrySubtractSigned(outer.Width, insets.Left, out var remainingWidth) ||
			!TrySubtractSigned(remainingWidth, insets.Right, out var width) ||
			!TrySubtractSigned(outer.Height, insets.Top, out var remainingHeight) ||
			!TrySubtractSigned(remainingHeight, insets.Bottom, out var height))
			return false;

		inner.Left = left;
		inner.Top = top;
		inner.Width = width > 0 ? width : 0;
		inner.Height = height > 0 ? height : 0;
		return true;
	}

	private static bool TryAddSigned(int value, int amount, out int result)
	{
		result = 0;
		if ((amount > 0 && value > int.MaxValue - amount) ||
			(amount < 0 && value < int.MinValue - amount)) return false;
		result = unchecked(value + amount);
		return true;
	}

	private static bool TrySubtractSigned(int value, int amount, out int result)
	{
		result = 0;
		if ((amount > 0 && value < int.MinValue + amount) ||
			(amount < 0 && value > int.MaxValue + amount)) return false;
		result = unchecked(value - amount);
		return true;
	}

	internal static bool ContainsPoint(MuiNativeAreaRectangle rectangle,
		int x, int y)
	{
		if (rectangle.Width <= 0 || rectangle.Height <= 0) return false;
		var xDelta = unchecked((uint)x - (uint)rectangle.Left);
		var yDelta = unchecked((uint)y - (uint)rectangle.Top);
		return x >= rectangle.Left && xDelta < (uint)rectangle.Width &&
			y >= rectangle.Top && yDelta < (uint)rectangle.Height;
	}

	internal static bool RectanglesIntersect(MuiNativeAreaRectangle first,
		MuiNativeAreaRectangle second)
	{
		if (first.Width <= 0 || first.Height <= 0 || second.Width <= 0 ||
			second.Height <= 0) return false;
		return AxisIntersects(first.Left, first.Width, second.Left,
			second.Width) && AxisIntersects(first.Top, first.Height, second.Top,
			second.Height);
	}

	internal static bool TryIntersectRectangles(MuiNativeAreaRectangle first,
		MuiNativeAreaRectangle second, out MuiNativeAreaRectangle intersection)
	{
		intersection = default;
		if (!RectanglesIntersect(first, second)) return false;
		var left = first.Left >= second.Left ? first.Left : second.Left;
		var top = first.Top >= second.Top ? first.Top : second.Top;
		var firstX = unchecked((uint)left - (uint)first.Left);
		var secondX = unchecked((uint)left - (uint)second.Left);
		var firstY = unchecked((uint)top - (uint)first.Top);
		var secondY = unchecked((uint)top - (uint)second.Top);
		var firstWidth = unchecked((uint)first.Width - firstX);
		var secondWidth = unchecked((uint)second.Width - secondX);
		var firstHeight = unchecked((uint)first.Height - firstY);
		var secondHeight = unchecked((uint)second.Height - secondY);
		var width = firstWidth < secondWidth ? firstWidth : secondWidth;
		var height = firstHeight < secondHeight ? firstHeight : secondHeight;
		if (width == 0 || height == 0 || width > int.MaxValue ||
			height > int.MaxValue) return false;
		intersection.Left = left;
		intersection.Top = top;
		intersection.Width = (int)width;
		intersection.Height = (int)height;
		return true;
	}

	private static bool AxisIntersects(int start, int length, int clipStart,
		int clipLength)
	{
		var delta = start >= clipStart
			? unchecked((uint)start - (uint)clipStart)
			: unchecked((uint)clipStart - (uint)start);
		return start >= clipStart ? delta < (uint)clipLength :
			delta < (uint)length;
	}

	internal static bool TryGetAttribute(ref MuiNativeClassPlatform platform,
		APTR obj, uint attribute, APTR storage, out uint value)
	{
		value = 0;
		return MuiGuestUlongStorageCodec.WriteValue(ref platform, storage, 0) &&
			MuiNativeIntuitionCalls.GetAttr(platform.IntuitionBase, attribute,
				obj, storage) != 0 &&
			MuiGuestUlongStorageCodec.TryReadValue(ref platform, storage,
				out value);
	}
}
