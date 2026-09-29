/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS MUIArea double-buffer policy. The named BOOL state is paired with a
// typed native Begin/End render capability. Allocation and blitting stay on the
// provider side; the core never creates a managed bitmap or exception path.
internal static class MuiAreaDoubleBufferCore
{
	internal const uint StateKey = 0x7F070040u;
	internal const uint RenderInfoAttribute = 0x7FFF0001u;

	internal static bool Begin<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR renderInfo, APTR sourceRastPort, int left, int top,
		int width, int height, uint flags,
		out MuiAreaDoubleBufferLease lease)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		lease = default;
		if (!TryReadState(ref platform, state, obj, out var policy)) return false;
		if (policy.Enabled == 0) return true;
		var request = default(MuiDoubleBufferRenderRequest);
		request.Object = obj;
		request.RenderInfo = renderInfo;
		request.SourceRastPort = sourceRastPort;
		request.Left = left;
		request.Top = top;
		request.Width = width;
		request.Height = height;
		request.TargetLeft = left;
		request.TargetTop = top;
		request.TargetWidth = width;
		request.TargetHeight = height;
		request.Flags = flags;
		if (!platform.BeginMuiDoubleBuffer(ref request)) return true;
		if (request.TargetRastPort.IsNull || request.TargetWidth <= 0 ||
			request.TargetHeight <= 0 || request.TargetLeft >
			int.MaxValue - request.TargetWidth || request.TargetTop >
			int.MaxValue - request.TargetHeight)
		{
			platform.EndMuiDoubleBuffer(ref request, false);
			return false;
		}
		var published = false;
		if (request.TargetRenderInfo.IsNotNull)
		{
			if (!MuiDrawingRenderInfoCodec.TryRead(ref platform,
				request.TargetRenderInfo, out var targetRenderInfo) ||
				targetRenderInfo.RastPort != request.TargetRastPort ||
				!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					RenderInfoAttribute, request.TargetRenderInfo.Raw, false))
			{
				platform.EndMuiDoubleBuffer(ref request, false);
				return false;
			}
			published = true;
		}
		lease.Active = 1;
		lease.RenderInfoPublished = published ? 1u : 0u;
		lease.OriginalRenderInfo = renderInfo;
		lease.Request = request;
		return true;
	}

	internal static bool End<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, ref MuiAreaDoubleBufferLease lease, bool completed)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (lease.Active == 0) return true;
		var result = platform.EndMuiDoubleBuffer(ref lease.Request, completed);
		if (lease.RenderInfoPublished != 0)
			result = MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				RenderInfoAttribute, lease.OriginalRenderInfo.Raw, false) && result;
		lease.Active = 0;
		return result;
	}

	internal static bool TryReadState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaDoubleBufferStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var enabled = 0u;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			MuiCommonControlCore.DoubleBuffer, out var raw))
			enabled = raw == 0 ? 0u : 1u;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj, StateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			StateKey);
		MuiAreaDoubleBufferStateRecord record;
		if (block.IsNotNull || length != 0)
		{
			// A present block is authoritative typed state.  Do not repair a
			// malformed BOOL or generation from the legacy raw attribute.
			if (length != unchecked((int)MuiAreaDoubleBufferStateRecord.Size) ||
				!MuiAreaDoubleBufferStateRecordCodec.TryReadStructural(ref platform,
					block, out record) ||
				!MuiAreaDoubleBufferStateAdmission.ValidateLive(ref platform, state,
					obj, record)) return false;
			if (record.Enabled != enabled)
			{
				record.Enabled = enabled;
				record.Generation = record.Generation == uint.MaxValue ? 1u :
					record.Generation + 1u;
				if (!MuiAreaDoubleBufferStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			value.Enabled = record.Enabled;
			return true;
		}
		if (!WriteState(ref platform, state, obj, enabled, 1)) return false;
		value.Enabled = enabled;
		return true;
	}

	internal static bool WriteState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint enabled, uint generation)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaDoubleBufferStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaDoubleBufferStateRecord.Size);
		var record = default(MuiAreaDoubleBufferStateRecord);
		record.Magic = MuiAreaDoubleBufferStateRecord.Cookie;
		record.Enabled = enabled == 0 ? 0u : 1u;
		record.Generation = generation == 0 ? 1u : generation;
		var written = MuiAreaDoubleBufferStateAdmission.ValidateLive(ref platform,
			state, obj, record) &&
			MuiAreaDoubleBufferStateRecordCodec.Write(ref platform, scratch, record);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateKey, scratch, unchecked((int)MuiAreaDoubleBufferStateRecord.Size));
		platform.Clear(scratch, MuiAreaDoubleBufferStateRecord.Size);
		platform.Free(scratch, MuiAreaDoubleBufferStateRecord.Size);
		return stored;
	}
}

internal struct MuiAreaDoubleBufferLease
{
	internal MuiDoubleBufferRenderRequest Request;
	internal APTR OriginalRenderInfo;
	internal uint RenderInfoPublished;
	internal uint Active;
}

// Public typed seam for the Area double-buffer policy. The input/output is a
// value type; callers do not receive a pointer into the private Dataspace.
public static class MuiAreaDoubleBufferPacketCore
{
	public static bool Set<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint enabled)
		where TPlatform : struct, IMuiLayoutPlatform =>
		MuiCommonControlCore.SetControlAttribute(ref platform, state, obj,
			MuiCommonControlCore.DoubleBuffer, enabled, true);

	public static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiAreaDoubleBufferStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaDoubleBufferCore.TryReadState(ref platform, state, obj, out value);
}
