/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Platform-owned dynamic ShortHelp request. The provider may replace the
// caller-owned static text with a temporary guest string and later receive the
// same handle through MuiShortHelpDeleteSample. The fields remain value-type
// state so no managed text or object graph crosses the core boundary.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiShortHelpCreateSample
{
	public APTR Object;
	public APTR CurrentHelp;
	public int MouseX;
	public int MouseY;
	public APTR Result;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiShortHelpCheckSample
{
	public APTR Object;
	public APTR CurrentHelp;
	public int MouseX;
	public int MouseY;
	public APTR Result;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiShortHelpDeleteSample
{
	public APTR Object;
	public APTR Help;
}

// Bounded MorphOS Area short-help state. Static OBString ownership stays with
// the caller; this core only retains the guest pointer and exposes it through
// a fixed-width record. Bubble creation and lifetime callbacks are separate
// capability work and are intentionally not synthesized here.
internal static class MuiAreaShortHelpCore
{
	internal const uint StateKey = 0x7F070041u;

	internal static bool TryReadState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaShortHelpStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var hasRaw = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			MuiCommonControlCore.ShortHelp, out var raw);
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj, StateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj, StateKey) ==
			unchecked((int)MuiAreaShortHelpStateRecord.Size) &&
			MuiAreaShortHelpStateRecordCodec.TryRead(ref platform, block,
				out var record))
		{
			if (hasRaw && record.Text.Raw != raw)
			{
				record.Text = APTR.FromPointer(raw);
				record.Generation = record.Generation == uint.MaxValue ? 1u :
					record.Generation + 1u;
				if (!MuiAreaShortHelpStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			value.Text = hasRaw ? APTR.FromPointer(raw) : record.Text;
			return true;
		}
		var text = hasRaw ? APTR.FromPointer(raw) : APTR.Null;
		if (!WriteState(ref platform, state, obj, text, 1)) return false;
		value.Text = text;
		return true;
	}

	internal static bool WriteState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR text, uint generation)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaShortHelpStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaShortHelpStateRecord.Size);
		var record = default(MuiAreaShortHelpStateRecord);
		record.Magic = MuiAreaShortHelpStateRecord.Cookie;
		record.Text = text;
		record.Generation = generation == 0 ? 1u : generation;
		var written = MuiAreaShortHelpStateRecordCodec.Write(ref platform, scratch,
			record);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateKey, scratch, unchecked((int)MuiAreaShortHelpStateRecord.Size));
		platform.Clear(scratch, MuiAreaShortHelpStateRecord.Size);
		platform.Free(scratch, MuiAreaShortHelpStateRecord.Size);
		return stored;
	}
}

// Public typed seam for the caller-owned MorphOS ShortHelp pointer.
public static class MuiAreaShortHelpPacketCore
{
	public static APTR Check<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR help, int mouseX, int mouseY)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiAreaShortHelpCore.TryReadState(ref platform, state, obj,
			out var value)) return APTR.Null;
		var sample = default(MuiShortHelpCheckSample);
		sample.Object = obj;
		sample.CurrentHelp = help;
		sample.MouseX = mouseX;
		sample.MouseY = mouseY;
		sample.Result = value.Text;
		var handled = platform.CheckMuiShortHelp(ref sample);
		if (handled)
		{
			if (sample.Object != obj || sample.CurrentHelp != help ||
				sample.MouseX != mouseX || sample.MouseY != mouseY)
				return APTR.Null;
			return sample.Result;
		}
		// Area's default implementation returns the caller-owned static
		// MUIA_ShortHelp pointer. Coordinate-sensitive or state-dependent text
		// is supplied only by an explicitly implemented provider capability.
		return value.Text;
	}

	public static APTR Create<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int mouseX, int mouseY)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiAreaShortHelpCore.TryReadState(ref platform, state, obj,
			out var value)) return APTR.Null;
		var sample = default(MuiShortHelpCreateSample);
		sample.Object = obj;
		sample.CurrentHelp = value.Text;
		sample.MouseX = mouseX;
		sample.MouseY = mouseY;
		sample.Result = value.Text;
		var handled = platform.CreateMuiShortHelp(ref sample);
		if (handled)
		{
			if (sample.Object != obj || sample.CurrentHelp != value.Text ||
				sample.MouseX != mouseX || sample.MouseY != mouseY)
				return APTR.Null;
			return sample.Result;
		}
		return value.Text;
	}

	public static bool Delete<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR help)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var sample = default(MuiShortHelpDeleteSample);
		sample.Object = obj;
		sample.Help = help;
		var handled = platform.DeleteMuiShortHelp(ref sample);
		if (handled && (sample.Object != obj || sample.Help != help)) return false;
		// Static caller-owned text remains an accepted no-op when no provider
		// owns dynamic help for this class.
		return true;
	}

	public static bool Set<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR text)
		where TPlatform : struct, IMuiLayoutPlatform =>
		MuiCommonControlCore.SetControlAttribute(ref platform, state, obj,
			MuiCommonControlCore.ShortHelp, text.Raw, true);

	public static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiAreaShortHelpStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaShortHelpCore.TryReadState(ref platform, state, obj, out value);
}
