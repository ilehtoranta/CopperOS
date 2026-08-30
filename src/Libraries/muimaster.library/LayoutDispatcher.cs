/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLayoutRenderInfoMessage
{
	public const uint Size = 8;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint RenderInfoOffset = 4;
	public uint MethodId;
	public uint RenderInfo;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLayoutFlagsMessage
{
	public const uint Size = 8;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint FlagsOffset = 4;
	public uint MethodId;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLayoutTextDimensionsMessage
{
	public const uint Size = 20;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint TextOffset = 4;
	public const uint LengthOffset = 8;
	public const uint PreParseOffset = 12;
	public const uint TextFlagsOffset = 16;
	public uint MethodId;
	public uint Text;
	public uint Length;
	public uint PreParse;
	public uint Flags;
}

public static class MuiLayoutDispatcher
{
	private const uint AskMinMax = MuiLayoutPacketCore.AskMinMax;
	private const uint Layout = 0x8042845B;
	private const uint Relayout = MuiLayoutPacketCore.Relayout;
	private const uint Setup = 0x80428354;
	private const uint Cleanup = 0x8042D985;
	private const uint InitResize = MuiAreaResizeCore.InitResize;
	private const uint ExitResize = MuiAreaResizeCore.ExitResize;
	private const uint Show = 0x8042CC84;
	private const uint Hide = 0x8042F20F;
	private const uint HandleInput = MuiCollectionSurfaceMessageCodec.HandleInput;
	private const uint Draw = 0x80426F3F;
	private const uint DrawBackground = MuiLayoutPacketCore.DrawBackground;
	private const uint Backfill = MuiLayoutPacketCore.Backfill;
	private const uint Text = MuiLayoutPacketCore.Text;
	private const uint TextDim = MuiLayoutPacketCodec.TextDim;
	private const uint GroupReorder = 0x80426C3F;
	private const uint GroupSort = 0x80427417;

	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message) where TPlatform : struct, IMuiLayoutPlatform
	{
		MuiLayoutMethodMessage methodHeader;
		if (!MuiLayoutPacketCodec.TryReadMethodId(ref platform, message,
			out methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (method == MuiAreaBubbleMessageCodec.CreateBubble)
		{
			if (!MuiAreaBubbleMessageCodec.TryReadCreate(ref platform, message,
				out var createBubble)) return 0;
			return MuiAreaBubblePacketCore.Create(ref platform, state, obj,
				createBubble.X, createBubble.Y, createBubble.Text,
				createBubble.Flags).Raw;
		}
		if (method == MuiAreaBubbleMessageCodec.DeleteBubble)
		{
			if (!MuiAreaBubbleMessageCodec.TryReadDelete(ref platform, message,
				out var deleteBubble)) return 0;
			return MuiAreaBubblePacketCore.Delete(ref platform, state, obj,
				deleteBubble.Bubble) ? 1u : 0u;
		}
		if (method == MuiAreaContextMenuMessageCodec.Add)
		{
			if (!MuiAreaContextMenuMessageCodec.TryReadAdd(ref platform, message,
				out var contextMenuAdd)) return 0;
			return MuiAreaContextMenuPacketCore.Add(ref platform, state, obj,
				contextMenuAdd.MenuStrip, contextMenuAdd.MouseX,
				contextMenuAdd.MouseY, contextMenuAdd.MouseXPointer,
				contextMenuAdd.MouseYPointer);
		}
		if (method == MuiAreaContextMenuMessageCodec.Build)
		{
			if (!MuiAreaContextMenuMessageCodec.TryReadBuild(ref platform, message,
				out _)) return 0;
			return MuiAreaContextMenuPacketCore.Build(ref platform, state, obj).Raw;
		}
		if (method == MuiAreaContextMenuMessageCodec.Choice)
		{
			if (!MuiAreaContextMenuMessageCodec.TryReadChoice(ref platform, message,
				out var contextMenuChoice)) return 0;
			return MuiAreaContextMenuPacketCore.Choice(ref platform, state, obj,
				contextMenuChoice.Item);
		}
		if (MuiAreaCustomFontMessageCodec.IsMethod(method))
			return MuiAreaCustomFontMessageCore.Dispatch(ref platform, state, obj,
				message);
		if (MuiAreaActivationCore.IsActivationMethod(method))
			return MuiAreaActivationCore.Dispatch(ref platform, state, obj, message);
		if (MuiAreaDragCore.IsDragMethod(method))
			return MuiAreaDragCore.Dispatch(ref platform, state, obj, message);
		switch (method)
		{
			case AskMinMax:
				if (!MuiLayoutPacketCore.TryReadAskMinMax(ref platform, message,
					out var askMinMaxPacket)) return 0;
				return MuiAreaLayoutCore.AskMinMax(ref platform, state, obj,
					APTR.FromPointer(askMinMaxPacket.Storage)) ? 1u : 0u;
			case Layout:
				if (!MuiLayoutPacketCodec.TryReadLayout(ref platform, message,
					out var layoutPacket))
					return 0;
				return MuiLayoutServiceCore.Layout(ref platform, state, obj,
					unchecked((int)layoutPacket.Left), unchecked((int)layoutPacket.Top),
					unchecked((int)layoutPacket.Width),
					unchecked((int)layoutPacket.Height), 0) ? 1u : 0u;
			case Relayout:
				if (!MuiLayoutPacketCore.TryReadRelayout(ref platform, message,
					out var relayoutPacket)) return 0;
				return MuiAreaLayoutCore.RequestRedraw(ref platform, obj,
					relayoutPacket.Flags) ? 1u : 0u;
			case Setup:
				if (!MuiLayoutPacketCodec.TryReadRenderInfo(ref platform, message, Setup,
					out var setupPacket)) return 0;
				return MuiAreaLayoutCore.Setup(ref platform, state, obj,
					APTR.FromPointer(setupPacket.RenderInfo)) ? 1u : 0u;
			case InitResize:
				if (!MuiAreaResizeMessageCodec.TryReadInit(ref platform, message,
					out var initResizePacket)) return 0;
				return MuiAreaResizeCore.Init(ref platform, state, obj,
					initResizePacket.Flags) ? 1u : 0u;
			case ExitResize:
				if (!MuiAreaResizeMessageCodec.TryReadExitMethod(ref platform,
					message, out _)) return 0;
				return MuiAreaResizeCore.Exit(ref platform, state, obj) ? 1u : 0u;
			case Cleanup:
				return MuiAreaLayoutCore.Cleanup(ref platform, state, obj) ? 1u : 0u;
			case Show:
				return MuiAreaLayoutCore.Show(ref platform, state, obj) ? 1u : 0u;
			case Hide:
				return MuiAreaLayoutCore.Hide(ref platform, state, obj) ? 1u : 0u;
			case HandleInput:
				if (!MuiVirtgroupCore.IsObject(ref platform, state, obj) ||
					!MuiCollectionSurfaceMessageCodec.TryReadHandleInput(ref platform,
						message, out var handleInput)) break;
				return MuiVirtgroupCore.HandleInput(ref platform, state, obj,
					APTR.FromPointer(handleInput.IntuiMessage), handleInput.MuiKey) ?
					1u : 0u;
			case Draw:
				if (!MuiLayoutPacketCodec.TryReadFlags(ref platform, message, Draw,
					out var drawPacket)) return 0;
				return MuiAreaLayoutCore.Draw(ref platform, state, obj,
					drawPacket.Flags) ? 1u : 0u;
			case DrawBackground:
				if (!MuiLayoutPacketCore.TryReadRectangle(ref platform, message,
					method, out var backgroundPacket)) return 0;
				return MuiAreaLayoutCore.DrawBackground(ref platform, state, obj,
					unchecked((int)backgroundPacket.Left),
					unchecked((int)backgroundPacket.Top),
					unchecked((int)backgroundPacket.RightOrWidth),
					unchecked((int)backgroundPacket.BottomOrHeight),
					backgroundPacket.XOffset, backgroundPacket.YOffset, 0,
					backgroundPacket.DrawBackgroundFlags,
					MuiBackfillRenderRequest.DrawBackground) ?
					1u : 0u;
			case Backfill:
				if (!MuiLayoutPacketCore.TryReadRectangle(ref platform, message,
					method, out var backfillPacket)) return 0;
				var left = unchecked((int)backfillPacket.Left);
				var top = unchecked((int)backfillPacket.Top);
				var right = unchecked((int)backfillPacket.RightOrWidth);
				var bottom = unchecked((int)backfillPacket.BottomOrHeight);
				if (right < left || bottom < top) return 0;
				var width = right - left;
				var height = bottom - top;
				if (width < 0 || height < 0 || width == int.MaxValue ||
					height == int.MaxValue) return 0;
				return MuiAreaLayoutCore.DrawBackground(ref platform, state, obj,
					left, top, width + 1, height + 1,
					backfillPacket.XOffset, backfillPacket.YOffset,
					backfillPacket.BackfillBrightness, 0,
					MuiBackfillRenderRequest.Backfill) ? 1u : 0u;
			case Text:
				if (!MuiLayoutPacketCore.TryReadText(ref platform, message,
					out var textRecord)) return 0;
				return MuiAreaLayoutCore.DrawText(ref platform, state, obj,
					unchecked((int)textRecord.Left), unchecked((int)textRecord.Top),
					unchecked((int)textRecord.Width), unchecked((int)textRecord.Height),
					APTR.FromPointer(textRecord.Text),
					unchecked((int)textRecord.Length),
					APTR.FromPointer(textRecord.PreParse), textRecord.Flags) ? 1u : 0u;
			case TextDim:
				if (!MuiLayoutPacketCodec.TryReadTextDimensions(ref platform, message,
					out var textPacket)) return 0;
				return MuiAreaLayoutCore.TextDimensions(ref platform, state, obj,
					APTR.FromPointer(textPacket.Text),
					unchecked((int)textPacket.Length),
					APTR.FromPointer(textPacket.PreParse), textPacket.Flags);
			case GroupReorder:
				if (!MuiGroupOperationsCore.TryReadReorder(ref platform, message,
					out var reorderPacket)) return 0;
				return MuiGroupOperationsCore.Reorder(ref platform, state, obj,
					APTR.FromPointer(reorderPacket.After),
					APTR.FromPointer(reorderPacket.Objects)) ? 1u : 0u;
			case GroupSort:
				if (!MuiGroupOperationsCore.TryReadSort(ref platform, message,
					out var sortPacket)) return 0;
				return MuiGroupOperationsCore.Sort(ref platform, state, obj,
					APTR.FromPointer(sortPacket.Objects)) ? 1u : 0u;
		}
		return MuiHeadlessDispatcher.Dispatch(ref platform, state, obj, message);
	}

}
