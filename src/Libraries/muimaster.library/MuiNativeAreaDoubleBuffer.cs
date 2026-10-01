/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// One temporary coordinate/render-info view for a native MUIM_Draw call. The
// parent pointer makes nested redraws restore the previous off-screen context.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeAreaDoubleBufferFrame
{
	internal const uint Magic = 0x4D444246; // "MDBF"
	internal const uint Size = 44;
	internal uint Signature;
	internal APTR Object;
	internal APTR Sidecar;
	internal APTR OwnerRoot;
	internal APTR Previous;
	internal int SourceLeft;
	internal int SourceTop;
	internal int Width;
	internal int Height;
	internal int TargetLeft;
	internal int TargetTop;
}

internal static class MuiNativeAreaDoubleBufferFrameCodec
{
	internal static bool TryRead<TMemory>(ref TMemory memory, APTR address,
		out MuiNativeAreaDoubleBufferFrame value)
		where TMemory : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeAreaDoubleBufferFrame.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Signature) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var obj) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var sidecar) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var ownerRoot) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var previous) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var sourceLeft) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var sourceTop) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var width) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var height) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var targetLeft) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var targetTop) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.Object = APTR.FromPointer(obj);
		value.Sidecar = APTR.FromPointer(sidecar);
		value.OwnerRoot = APTR.FromPointer(ownerRoot);
		value.Previous = APTR.FromPointer(previous);
		value.SourceLeft = unchecked((int)sourceLeft);
		value.SourceTop = unchecked((int)sourceTop);
		value.Width = unchecked((int)width);
		value.Height = unchecked((int)height);
		value.TargetLeft = unchecked((int)targetLeft);
		value.TargetTop = unchecked((int)targetTop);
		return IsValid(value);
	}

	internal static bool Write<TMemory>(ref TMemory memory, APTR address,
		MuiNativeAreaDoubleBufferFrame value)
		where TMemory : struct, IMuiGuestMemory =>
		IsValid(value) && MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeAreaDoubleBufferFrame.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Signature) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Object.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Sidecar.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.OwnerRoot.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Previous.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			unchecked((uint)value.SourceLeft)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			unchecked((uint)value.SourceTop)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			unchecked((uint)value.Width)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			unchecked((uint)value.Height)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			unchecked((uint)value.TargetLeft)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			unchecked((uint)value.TargetTop)) &&
		MuiGuestStructCursor.IsComplete(cursor);

	internal static bool IsValid(MuiNativeAreaDoubleBufferFrame value) =>
		value.Signature == MuiNativeAreaDoubleBufferFrame.Magic &&
		value.Object.IsNotNull && value.Sidecar.IsNotNull &&
		value.OwnerRoot.IsNotNull && value.Width > 0 && value.Height > 0 &&
		value.SourceLeft <= int.MaxValue - value.Width &&
		value.SourceTop <= int.MaxValue - value.Height &&
		value.TargetLeft <= int.MaxValue - value.Width &&
		value.TargetTop <= int.MaxValue - value.Height;
}

internal struct MuiNativeAreaDoubleBufferLease
{
	internal MuiDoubleBufferRenderRequest Request;
	internal APTR Sidecar;
	internal APTR Frame;
	internal APTR PreviousFrame;
	internal APTR PreviousRenderInfo;
	internal uint Active;
}

internal static class MuiNativeAreaDoubleBufferCore
{
	internal const uint FrameAttribute = 0x7FFF0002u;

	internal static bool TryBeginForObject(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR obj, APTR message,
		out MuiNativeAreaDoubleBufferLease lease)
	{
		lease = default;
		if (!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
			ownerRoot, obj, out var binding) ||
			platform.IntuitionBase.IsNull ||
			!MuiNativeObjectStateCore.TryGetAttribute(ref platform,
				binding.Sidecar, MuiCommonControlCore.DoubleBuffer,
				out var enabled) || enabled == 0 ||
			!MuiNativeRedrawMessageCodec.TryRead(ref platform, message,
				out var drawMessage) || drawMessage.MethodId !=
				MuiNativeRedrawMessage.DrawMethodId) return false;

		var storage = platform.Allocate(MuiGuestUlongStorage.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (storage.IsNull) return false;
		var hasGeometry = MuiNativeGuiMode.TryReadAreaRectangle(ref platform,
			obj, storage, out var geometry);
		platform.Free(storage, MuiGuestUlongStorage.Size);
		if (!hasGeometry || geometry.Width <= 0 || geometry.Height <= 0 ||
			geometry.Left > int.MaxValue - geometry.Width ||
			geometry.Top > int.MaxValue - geometry.Height) return false;

		return TryBegin(ref platform, binding.Sidecar, ownerRoot, obj,
			geometry, drawMessage.Flags, out lease);
	}

	internal static bool TryBegin<TPlatform>(ref TPlatform platform,
		APTR sidecar, APTR ownerRoot, APTR obj, MuiNativeAreaRectangle geometry,
		uint flags, out MuiNativeAreaDoubleBufferLease lease)
		where TPlatform : struct, IMuiAllocationPlatform,
			IMuiDoubleBufferCapability
	{
		lease = default;
		if (sidecar.IsNull || ownerRoot.IsNull || obj.IsNull ||
			geometry.Width <= 0 || geometry.Height <= 0 ||
			geometry.Left > int.MaxValue - geometry.Width ||
			geometry.Top > int.MaxValue - geometry.Height ||
			!MuiNativeMuiObjectCodec.TryRead(ref platform, sidecar,
				out var sidecarRecord) || sidecarRecord.Object != obj ||
			sidecarRecord.OwnerRoot != ownerRoot || sidecarRecord.LifecycleState !=
				MuiNativeMuiObjectRecord.StateLive ||
			(sidecarRecord.Flags &
				MuiNativeMuiObjectRecord.ObjectInitialized) == 0 ||
			(sidecarRecord.Flags &
				MuiNativeMuiObjectRecord.ObjectDisposing) != 0 ||
			!MuiNativeObjectStateCore.TryGetAttribute(ref platform, sidecar,
				MuiCommonControlCore.DoubleBuffer, out var enabled) || enabled == 0 ||
			!MuiNativeObjectStateCore.TryGetAttribute(ref platform, sidecar,
				MuiAreaWindowRelationshipCore.RenderInfoAttribute,
				out var renderInfoRaw) || renderInfoRaw == 0 ||
			!MuiDrawingRenderInfoCodec.TryRead(ref platform,
				APTR.FromPointer(renderInfoRaw), out var sourceRenderInfo) ||
			sourceRenderInfo.RastPort.IsNull) return false;

		var previousFrame = APTR.Null;
		if (MuiNativeObjectStateCore.TryGetAttribute(ref platform, sidecar,
			FrameAttribute, out var previousFrameRaw) && previousFrameRaw != 0)
		{
			previousFrame = APTR.FromPointer(previousFrameRaw);
			if (!MuiNativeAreaDoubleBufferFrameCodec.TryRead(ref platform,
				previousFrame, out var previous) || previous.Object != obj ||
				previous.Sidecar != sidecar || previous.OwnerRoot != ownerRoot)
				return false;
		}

		var request = default(MuiDoubleBufferRenderRequest);
		request.Object = obj;
		request.RenderInfo = APTR.FromPointer(renderInfoRaw);
		request.SourceRastPort = sourceRenderInfo.RastPort;
		request.Left = geometry.Left;
		request.Top = geometry.Top;
		request.Width = geometry.Width;
		request.Height = geometry.Height;
		request.TargetLeft = 0;
		request.TargetTop = 0;
		request.TargetWidth = geometry.Width;
		request.TargetHeight = geometry.Height;
		request.Flags = flags;
		if (!platform.BeginMuiDoubleBuffer(ref request)) return false;
		if (request.TargetRenderInfo.IsNull || request.TargetRastPort.IsNull ||
			request.TargetWidth != geometry.Width ||
			request.TargetHeight != geometry.Height ||
			!MuiDrawingRenderInfoCodec.TryRead(ref platform,
				request.TargetRenderInfo, out var targetRenderInfo) ||
			targetRenderInfo.RastPort != request.TargetRastPort)
		{
			platform.EndMuiDoubleBuffer(ref request, false);
			return false;
		}

		var frameAddress = platform.Allocate(
			MuiNativeAreaDoubleBufferFrame.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (frameAddress.IsNull)
		{
			platform.EndMuiDoubleBuffer(ref request, false);
			return false;
		}
		var frame = new MuiNativeAreaDoubleBufferFrame
		{
			Signature = MuiNativeAreaDoubleBufferFrame.Magic,
			Object = obj,
			Sidecar = sidecar,
			OwnerRoot = ownerRoot,
			Previous = previousFrame,
			SourceLeft = geometry.Left,
			SourceTop = geometry.Top,
			Width = geometry.Width,
			Height = geometry.Height,
			TargetLeft = request.TargetLeft,
			TargetTop = request.TargetTop,
		};
		if (!MuiNativeAreaDoubleBufferFrameCodec.Write(ref platform,
			frameAddress, frame))
		{
			platform.Free(frameAddress, MuiNativeAreaDoubleBufferFrame.Size);
			platform.EndMuiDoubleBuffer(ref request, false);
			return false;
		}

		if (!MuiNativeObjectStateCore.SetBorrowedAttributeNoNotify(ref platform,
			sidecar, MuiAreaWindowRelationshipCore.RenderInfoAttribute,
			request.TargetRenderInfo.Raw))
		{
			platform.Free(frameAddress, MuiNativeAreaDoubleBufferFrame.Size);
			platform.EndMuiDoubleBuffer(ref request, false);
			return false;
		}
		if (!MuiNativeObjectStateCore.SetBorrowedAttributeNoNotify(ref platform,
			sidecar, FrameAttribute, frameAddress.Raw))
		{
			MuiNativeObjectStateCore.SetBorrowedAttributeNoNotify(ref platform,
				sidecar, MuiAreaWindowRelationshipCore.RenderInfoAttribute,
				renderInfoRaw);
			platform.Free(frameAddress, MuiNativeAreaDoubleBufferFrame.Size);
			platform.EndMuiDoubleBuffer(ref request, false);
			return false;
		}

		lease.Request = request;
		lease.Sidecar = sidecar;
		lease.Frame = frameAddress;
		lease.PreviousFrame = previousFrame;
		lease.PreviousRenderInfo = APTR.FromPointer(renderInfoRaw);
		lease.Active = 1;
		return true;
	}

	internal static bool End<TPlatform>(ref TPlatform platform,
		ref MuiNativeAreaDoubleBufferLease lease, bool completed)
		where TPlatform : struct, IMuiAllocationPlatform,
			IMuiDoubleBufferCapability
	{
		if (lease.Active == 0) return true;
		var restoredFrame = MuiNativeObjectStateCore.SetBorrowedAttributeNoNotify(
			ref platform, lease.Sidecar, FrameAttribute,
			lease.PreviousFrame.Raw);
		var restoredRenderInfo = MuiNativeObjectStateCore.SetBorrowedAttributeNoNotify(
			ref platform, lease.Sidecar,
			MuiAreaWindowRelationshipCore.RenderInfoAttribute,
			lease.PreviousRenderInfo.Raw);
		var result = platform.EndMuiDoubleBuffer(ref lease.Request,
			completed && restoredFrame && restoredRenderInfo) &&
			restoredFrame && restoredRenderInfo;
		platform.Free(lease.Frame, MuiNativeAreaDoubleBufferFrame.Size);
		lease.Active = 0;
		return result;
	}

	internal static bool TryGetTemporaryGeometry<TMemory>(ref TMemory memory,
		APTR sidecar, APTR ownerRoot, APTR obj, uint attribute,
		out uint value)
		where TMemory : struct, IMuiGuestMemory
	{
		value = 0;
		if (attribute != MuiNativeGuiMode.AreaLeftEdge &&
			attribute != MuiNativeGuiMode.AreaTopEdge &&
			attribute != MuiNativeGuiMode.AreaWidth &&
			attribute != MuiNativeGuiMode.AreaHeight) return false;
		if (!MuiNativeObjectStateCore.TryGetAttribute(ref memory, sidecar,
			FrameAttribute, out var currentRaw) || currentRaw == 0) return false;
		var current = APTR.FromPointer(currentRaw);
		if (!MuiNativeAreaDoubleBufferFrameCodec.TryRead(ref memory, current,
			out var frame) || frame.Object != obj || frame.Sidecar != sidecar ||
			frame.OwnerRoot != ownerRoot)
		{
			value = 0;
			return true;
		}
		if (attribute == MuiNativeGuiMode.AreaLeftEdge)
			value = unchecked((uint)frame.TargetLeft);
		else if (attribute == MuiNativeGuiMode.AreaTopEdge)
			value = unchecked((uint)frame.TargetTop);
		else if (attribute == MuiNativeGuiMode.AreaWidth)
			value = unchecked((uint)frame.Width);
		else value = unchecked((uint)frame.Height);
		return true;
	}
}

// Native graphics operations stay behind the explicit graphics.library base.
// The request owns the bitmap, RastPort and RenderInfo until End releases them.
internal static class MuiNativeAreaDoubleBufferGraphicsCore
{
	private const uint BitmapClear = 1;
	private const uint SourceCopyMinterm = 0xC0;

	internal static bool Begin(ref MuiNativeClassPlatform platform,
		ref MuiDoubleBufferRenderRequest request)
	{
		var graphics = platform.GraphicsBase;
		if (graphics.IsNull || request.Width <= 0 || request.Height <= 0 ||
			request.SourceRastPort.IsNull || request.RenderInfo.IsNull ||
			!platform.IsMapped(request.SourceRastPort, RastPort.Size) ||
			!MuiDrawingRenderInfoCodec.TryRead(ref platform, request.RenderInfo,
				out var renderInfo) || renderInfo.RastPort != request.SourceRastPort ||
			!MuiNativeDoubleBufferRasterPortCodec.TryRead(ref platform,
				request.SourceRastPort, out var sourceRasterPort) ||
			sourceRasterPort.BitMap.IsNull) return false;

		var depth = MuiNativeDrawingCalls.GetBitMapAttribute(graphics,
			sourceRasterPort.BitMap, BitMapAttribute.Depth);
		if (depth == 0) return false;
		request.TargetBitmap = MuiNativeDrawingCalls.AllocateBitmap(graphics,
			unchecked((uint)request.Width), unchecked((uint)request.Height), depth,
			BitmapClear, sourceRasterPort.BitMap);
		if (request.TargetBitmap.IsNull) return false;

		request.TargetRastPort = platform.Allocate(RastPort.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (request.TargetRastPort.IsNull)
		{
			End(ref platform, ref request, false);
			return false;
		}
		platform.Copy(request.SourceRastPort, request.TargetRastPort,
			RastPort.Size);
		var targetRasterPort = sourceRasterPort;
		targetRasterPort.Layer = APTR.Null;
		targetRasterPort.BitMap = request.TargetBitmap;
		if (!MuiNativeDoubleBufferRasterPortCodec.Write(ref platform,
			request.TargetRastPort, targetRasterPort))
		{
			End(ref platform, ref request, false);
			return false;
		}

		request.TargetRenderInfo = platform.Allocate(
			MuiDrawingRenderInfoRecord.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (request.TargetRenderInfo.IsNull)
		{
			End(ref platform, ref request, false);
			return false;
		}
		renderInfo.RastPort = request.TargetRastPort;
		if (!MuiDrawingRenderInfoCodec.Write(ref platform,
			request.TargetRenderInfo, renderInfo))
		{
			End(ref platform, ref request, false);
			return false;
		}
		request.TargetLeft = 0;
		request.TargetTop = 0;
		request.TargetWidth = request.Width;
		request.TargetHeight = request.Height;
		// Preserve untouched pixels for update-only Draw calls. ClipBlit keeps
		// the source layer's current damage/visibility clip while seeding the
		// temporary surface; the final ClipBlit applies the destination clip.
		MuiNativeDrawingCalls.ClipBlit(graphics, request.SourceRastPort,
			request.Left, request.Top, request.TargetRastPort,
			request.TargetLeft, request.TargetTop, request.Width,
			request.Height, SourceCopyMinterm);
		return true;
	}

	internal static bool End(ref MuiNativeClassPlatform platform,
		ref MuiDoubleBufferRenderRequest request, bool completed)
	{
		var graphics = platform.GraphicsBase;
		var blitted = !completed;
		if (completed && graphics.IsNotNull && request.TargetRastPort.IsNotNull &&
			request.SourceRastPort.IsNotNull && request.TargetBitmap.IsNotNull &&
			request.Width > 0 && request.Height > 0)
		{
			MuiNativeDrawingCalls.ClipBlit(graphics,
				request.TargetRastPort, 0, 0, request.SourceRastPort,
				request.Left, request.Top, request.Width, request.Height,
				SourceCopyMinterm);
			blitted = true;
		}
		if (request.TargetRenderInfo.IsNotNull)
			platform.Free(request.TargetRenderInfo,
				MuiDrawingRenderInfoRecord.Size);
		if (request.TargetRastPort.IsNotNull)
			platform.Free(request.TargetRastPort, RastPort.Size);
		if (request.TargetBitmap.IsNotNull && graphics.IsNotNull)
			MuiNativeDrawingCalls.FreeBitmap(graphics, request.TargetBitmap);
		request.TargetRenderInfo = APTR.Null;
		request.TargetRastPort = APTR.Null;
		request.TargetBitmap = APTR.Null;
		return blitted;
	}
}

// The codec names the RastPort fields used by double buffering while checking
// and advancing across the complete SDK RastPort ABI record.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeDoubleBufferRasterPort
{
	internal const uint Size = RastPort.Size;
	internal const uint HeaderSize = 8;
	internal APTR Layer;
	internal APTR BitMap;
}

internal static class MuiNativeDoubleBufferRasterPortCodec
{
	internal static bool TryRead<TMemory>(ref TMemory memory, APTR address,
		out MuiNativeDoubleBufferRasterPort value)
		where TMemory : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeDoubleBufferRasterPort.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var layer) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var bitmap) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativeDoubleBufferRasterPort.Size -
				MuiNativeDoubleBufferRasterPort.HeaderSize, out _) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Layer = APTR.FromPointer(layer);
		value.BitMap = APTR.FromPointer(bitmap);
		return true;
	}

	internal static bool Write<TMemory>(ref TMemory memory, APTR address,
		MuiNativeDoubleBufferRasterPort value)
		where TMemory : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeDoubleBufferRasterPort.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Layer.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.BitMap.Raw) &&
		MuiGuestStructCursor.TryTake(ref memory, ref cursor,
			MuiNativeDoubleBufferRasterPort.Size -
			MuiNativeDoubleBufferRasterPort.HeaderSize, out _) &&
		MuiGuestStructCursor.IsComplete(cursor);
}
