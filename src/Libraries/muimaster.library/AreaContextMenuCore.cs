/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Native context-menu provider requests.  Menu strips and menu items remain
// opaque guest objects.  The core validates the owning Area and optional LONG
// output pointers, while native menu ownership stays with the provider.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiContextMenuAddSample
{
	public APTR Object;
	public APTR MenuStrip;
	public int MouseX;
	public int MouseY;
	public APTR MouseXPointer;
	public APTR MouseYPointer;
	public uint Result;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiContextMenuChoiceSample
{
	public APTR Object;
	public APTR Item;
}

internal static class MuiAreaContextMenuCore
{
	internal const uint StateKey = 0x7F070061u;

	internal static bool TryReadState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaContextMenuStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var hasRaw = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			MuiCommonControlCore.ContextMenu, out var raw);
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj, StateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			StateKey);
		MuiAreaContextMenuStateRecord record;
		if (block.IsNotNull || length != 0)
		{
			// A present block is authoritative typed state.  Reject malformed
			// records instead of rebuilding menu ownership from raw aliases.
			if (length != unchecked((int)MuiAreaContextMenuStateRecord.Size) ||
				!MuiAreaContextMenuStateRecordCodec.TryReadStructural(ref platform,
					block, out record) ||
				!MuiAreaContextMenuStateAdmission.ValidateLive(ref platform, state,
					obj, record)) return false;
			if (hasRaw && record.MenuStrip.Raw != raw)
			{
				record.MenuStrip = APTR.FromPointer(raw);
				record.Generation = record.Generation == uint.MaxValue ? 1u :
					record.Generation + 1u;
				if (!MuiAreaContextMenuStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			value.MenuStrip = hasRaw ? APTR.FromPointer(raw) : record.MenuStrip;
			value.Trigger = record.Trigger;
			return true;
		}
		var menuStrip = hasRaw ? APTR.FromPointer(raw) : APTR.Null;
		if (!WriteState(ref platform, state, obj, menuStrip, APTR.Null, 1))
			return false;
		value.MenuStrip = menuStrip;
		value.Trigger = APTR.Null;
		return true;
	}

	internal static bool WriteState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR menuStrip, APTR trigger, uint generation)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaContextMenuStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaContextMenuStateRecord.Size);
		var record = default(MuiAreaContextMenuStateRecord);
		record.Magic = MuiAreaContextMenuStateRecord.Cookie;
		record.MenuStrip = menuStrip;
		record.Trigger = trigger;
		record.Generation = generation == 0 ? 1u : generation;
		var written = MuiAreaContextMenuStateAdmission.ValidateLive(ref platform,
			state, obj, record) && MuiAreaContextMenuStateRecordCodec.Write(
			ref platform, scratch, record);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateKey, scratch, unchecked((int)MuiAreaContextMenuStateRecord.Size));
		platform.Clear(scratch, MuiAreaContextMenuStateRecord.Size);
		platform.Free(scratch, MuiAreaContextMenuStateRecord.Size);
		return stored;
	}

	internal static bool PublishTrigger<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR item, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadState(ref platform, state, obj, out var current)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj, StateKey);
		if (!MuiAreaContextMenuStateRecordCodec.TryReadStructural(ref platform,
			block, out var record) ||
			!MuiAreaContextMenuStateAdmission.ValidateLive(ref platform, state,
				obj, record)) return false;
		record.Trigger = item;
		record.Generation = record.Generation == uint.MaxValue ? 1u :
			record.Generation + 1u;
		if (!MuiAreaContextMenuStateRecordCodec.Write(ref platform, block, record))
			return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiCommonControlCore.ContextMenuTrigger, item.Raw, notify);
	}

	internal static uint Add<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR menuStrip, int mouseX, int mouseY, APTR mouseXPointer,
		APTR mouseYPointer) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull ||
			(!mouseXPointer.IsNull && !platform.IsMapped(mouseXPointer, 4)) ||
			(!mouseYPointer.IsNull && !platform.IsMapped(mouseYPointer, 4)))
			return 0;
		var sample = default(MuiContextMenuAddSample);
		sample.Object = obj;
		sample.MenuStrip = menuStrip;
		sample.MouseX = mouseX;
		sample.MouseY = mouseY;
		sample.MouseXPointer = mouseXPointer;
		sample.MouseYPointer = mouseYPointer;
		if (!platform.AddMuiContextMenu(ref sample)) return 0;
		if (sample.Object != obj || sample.MenuStrip != menuStrip ||
			sample.MouseX != mouseX || sample.MouseY != mouseY ||
			sample.MouseXPointer != mouseXPointer ||
			sample.MouseYPointer != mouseYPointer) return 0;
		return sample.Result;
	}

	internal static uint Choice<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR item) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return 0;
		var sample = default(MuiContextMenuChoiceSample);
		sample.Object = obj;
		sample.Item = item;
		if (platform.HandleMuiContextMenuChoice(ref sample))
		{
			return sample.Object == obj && sample.Item == item ? 1u : 0u;
		}
		return PublishTrigger(ref platform, state, obj, item, true) ? 1u : 0u;
	}
}

public static class MuiAreaContextMenuPacketCore
{
	// MUIM_ContextMenuBuild reaches Area as the static-menu fallback.  Keep
	// the result as the named guest pointer held by the typed Area state;
	// callers convert it to the ABI scalar only at the dispatcher boundary.
	public static APTR Build<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiAreaContextMenuCore.TryReadState(ref platform, state, obj,
			out var value)) return APTR.Null;
		return value.MenuStrip;
	}

	public static bool Set<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR menuStrip) where TPlatform : struct, IMuiLayoutPlatform =>
		MuiCommonControlCore.SetControlAttribute(ref platform, state, obj,
			MuiCommonControlCore.ContextMenu, menuStrip.Raw, true);

	public static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiAreaContextMenuStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaContextMenuCore.TryReadState(ref platform, state, obj, out value);

	public static uint Add<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR menuStrip, int mouseX, int mouseY, APTR mouseXPointer,
		APTR mouseYPointer) where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaContextMenuCore.Add(ref platform, state, obj, menuStrip, mouseX,
			mouseY, mouseXPointer, mouseYPointer);

	public static uint Choice<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR item) where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaContextMenuCore.Choice(ref platform, state, obj, item);
}
