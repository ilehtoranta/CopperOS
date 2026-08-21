/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using Amiga.MUI;

namespace CopperOS.MuiMaster;

// MorphOS Group custom-layout hook bridge. The hook receives the real
// MUI_LayoutMsg record in guest memory, with lm_Children pointing at the
// read-only Group ChildList projection. The temporary message is allocated
// from the guest Exec seam and released explicitly; no managed packet or
// callback wrapper crosses the freestanding boundary.
internal static class MuiGroupLayoutHookCore
{
	// Keep the host-side result as a named record even when a local SDK
	// reference assembly exposes MUI_LayoutMsg without projecting its nested
	// dimensions type.  The guest ABI still travels through the SDK message;
	// this record is only the strongly typed result returned to AreaLayoutCore.
	internal struct LayoutDimensions
	{
		internal int Width;
		internal int Height;
		internal uint Private5;
		internal uint Private6;
	}

	internal const uint Attribute = 0x8042C3B2;
	internal const uint StateAttribute = 0x7FFE0046;
	internal const uint MinMax = 1;
	internal const uint Layout = 2;

	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == Attribute;

	internal static bool TryGetAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (!IsPublicGetterAttribute(attribute) ||
			!MuiGroupChangeCore.IsGroupObject(ref platform, state, group))
			return false;
		if (TryReadEffectiveState(ref platform, state, group,
			out var hookState))
		{
			value = hookState.Hook.Raw;
			return true;
		}
		return true;
	}

	internal static bool TrySet<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, uint attribute, uint value, bool notify, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		handled = false;
		if (attribute != Attribute ||
			!MuiHeadlessObjectCodec.TryRead(ref platform, record,
				out var objectValue) || objectValue.Boopsi.IsNull ||
			!MuiGroupChangeCore.IsGroupObject(ref platform, state,
				objectValue.Boopsi)) return false;
		handled = true;
		var block = EnsureState(ref platform, state, record);
		if (block.IsNull || !MuiGroupLayoutHookStateRecordCodec.TryRead(ref platform,
			block, out var hookState))
			return false;
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, Attribute, value, false)) return false;
		hookState.Hook = APTR.FromPointer(value);
		if (!MuiGroupLayoutHookStateRecordCodec.Write(ref platform, block,
			hookState)) return false;
		MuiHeadlessMemory.Mutated(ref platform, state);
		if (notify) MuiNotifyCore.DispatchAttributeChange(ref platform, state,
			record, Attribute, value);
		return true;
	}

	internal static bool IsInstalled<TPlatform>(ref TPlatform platform,
		APTR state, APTR group) where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadEffectiveState(ref platform, state, group, out var hookState) &&
		hookState.Hook.IsNotNull;

	internal static bool InvokeMinMax<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, out MuiMinMaxValues values)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		values = default;
		if (!TryGetHookAndChildren(ref platform, state, group, out var hook,
			out var children)) return false;
		return InvokeMinMaxRecord(ref platform, hook, group, children,
			out values);
	}

	// Focused native qualification seam. Production callers use the object
	// lookup above; the explicit record form proves the fixed guest packet and
	// callback ABI without pulling the complete headless lifecycle into a small
	// freestanding closure.
	internal static bool InvokeMinMaxRecord<TPlatform>(ref TPlatform platform,
		APTR hook, APTR group, APTR children, out MuiMinMaxValues values)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		values = default;
		if (hook.IsNull || children.IsNull) return false;
		var message = MuiHeadlessMemory.Allocate(ref platform,
			MUI_LayoutMsg.Size);
		if (message.IsNull) return false;
		var packet = default(MUI_LayoutMsg);
		packet.lm_Type = MinMax;
		packet.lm_Children = children;
		MUI_LayoutMsgCodec.Write(ref platform, message, packet);
		platform.InvokeHook(hook, group, message);
		var read = MUI_LayoutMsgCodec.TryRead(ref platform, message,
			out packet);
		if (read)
		{
			values.MinWidth = packet.lm_MinMax.MinWidth;
			values.MinHeight = packet.lm_MinMax.MinHeight;
			values.MaxWidth = packet.lm_MinMax.MaxWidth;
			values.MaxHeight = packet.lm_MinMax.MaxHeight;
			values.DefWidth = packet.lm_MinMax.DefWidth;
			values.DefHeight = packet.lm_MinMax.DefHeight;
		}
		ReleaseMessage(ref platform, message);
		return read;
	}

	internal static bool InvokeLayout<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int width, int height,
		out LayoutDimensions dimensions)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		dimensions = default;
		if (!TryGetHookAndChildren(ref platform, state, group, out var hook,
			out var children)) return false;
		return InvokeLayoutRecord(ref platform, hook, group, children, width,
			height, out dimensions);
 	}

	internal static bool InvokeLayoutRecord<TPlatform>(ref TPlatform platform,
		APTR hook, APTR group, APTR children, int width, int height,
		out LayoutDimensions dimensions)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		dimensions = default;
		if (hook.IsNull || children.IsNull) return false;
		var message = MuiHeadlessMemory.Allocate(ref platform,
			MUI_LayoutMsg.Size);
		if (message.IsNull) return false;
		var packet = default(MUI_LayoutMsg);
		packet.lm_Type = Layout;
		packet.lm_Children = children;
		packet.lm_Layout.Width = width;
		packet.lm_Layout.Height = height;
		MUI_LayoutMsgCodec.Write(ref platform, message, packet);
		var result = platform.InvokeHook(hook, group, message);
		var read = MUI_LayoutMsgCodec.TryRead(ref platform, message,
			out packet);
		if (read)
		{
			dimensions.Width = packet.lm_Layout.Width;
			dimensions.Height = packet.lm_Layout.Height;
			dimensions.Private5 = packet.lm_Layout.priv5;
			dimensions.Private6 = packet.lm_Layout.priv6;
		}
		ReleaseMessage(ref platform, message);
		return read && result != 0;
	}

	private static bool TryGetHookAndChildren<TPlatform>(
		ref TPlatform platform, APTR state, APTR group, out APTR hook,
		out APTR children) where TPlatform : struct, IMuiHeadlessPlatform
	{
		hook = APTR.Null;
		children = APTR.Null;
		if (!TryReadEffectiveState(ref platform, state, group,
			out var hookState) || hookState.Hook.IsNull) return false;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, group,
			MuiGroupChildrenCore.ChildList, out var childListRaw) ||
			childListRaw == 0) return false;
		hook = hookState.Hook;
		children = APTR.FromPointer(childListRaw);
		return true;
	}

	private static bool TryReadEffectiveState<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, out MuiGroupLayoutHookStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, group);
		if (record.IsNull) return false;
		if (TryReadState(ref platform, state, record, out value)) return true;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
			Attribute, out var hook))
		{
			value.Magic = MuiGroupLayoutHookStateRecord.Cookie;
			return true;
		}
		value.Magic = MuiGroupLayoutHookStateRecord.Cookie;
		value.Hook = APTR.FromPointer(hook);
		return true;
	}

	private static APTR EnsureState<TPlatform>(ref TPlatform platform, APTR state,
		APTR record) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return APTR.Null;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			objectValue.Boopsi, StateAttribute, out var existing) && existing != 0)
		{
			var existingBlock = APTR.FromPointer(existing);
			if (MuiGroupLayoutHookStateRecordCodec.TryRead(ref platform,
				existingBlock, out _)) return existingBlock;
		}
		var block = MuiHeadlessMemory.Allocate(ref platform,
			MuiGroupLayoutHookStateRecord.Size);
		if (block.IsNull) return APTR.Null;
		var value = default(MuiGroupLayoutHookStateRecord);
		value.Magic = MuiGroupLayoutHookStateRecord.Cookie;
		if (!MuiGroupLayoutHookStateRecordCodec.Write(ref platform, block,
			value) || !MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform,
			state, record, StateAttribute, block.Raw, false))
		{
			platform.Clear(block, MuiGroupLayoutHookStateRecord.Size);
			platform.Free(block, MuiGroupLayoutHookStateRecord.Size);
			return APTR.Null;
		}
		return block;
	}

	private static bool TryReadState<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, out MuiGroupLayoutHookStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (record.IsNull || !MuiHeadlessObjectCodec.TryRead(ref platform,
			record, out var objectValue)) return false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			objectValue.Boopsi, StateAttribute,
			out var blockRaw) || blockRaw == 0) return false;
		return MuiGroupLayoutHookStateRecordCodec.TryRead(ref platform,
			APTR.FromPointer(blockRaw), out value);
	}

	internal static void Cleanup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull || !MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, StateAttribute, out var blockRaw) || blockRaw == 0) return;
		var block = APTR.FromPointer(blockRaw);
		if (!MuiGroupLayoutHookStateRecordCodec.TryRead(ref platform, block,
			out _)) return;
		platform.Clear(block, MuiGroupLayoutHookStateRecord.Size);
		platform.Free(block, MuiGroupLayoutHookStateRecord.Size);
		MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
			StateAttribute, 0, false);
	}

	private static void ReleaseMessage<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiHeadlessPlatform
	{
		platform.Clear(message, MUI_LayoutMsg.Size);
		platform.Free(message, MUI_LayoutMsg.Size);
	}
}
