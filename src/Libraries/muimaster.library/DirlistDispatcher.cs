/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDirlistMethodMessage
{
	public const uint Size = 4;
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDirlistSetMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Attribute;
	public uint Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDirlistRenameMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Entry;
	public uint Name;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDirlistProtectionMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Entry;
	public uint Protection;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDirlistGetEntryMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Position;
	public uint Storage;
}

// Method dispatch for Dirlist.mui / Volumelist.mui. This is deliberately a
// separate entry point rather than a branch inside MuiCollectionDispatcher:
// pulling the (large) directory subclasses into the shared collection graph
// grows the reachable closure enough to push method calls past the MC68020
// PC-relative branch window, regressing the zero-relocation native gates.
// (The earlier "reachable from MuiLayoutDispatcher" rationale no longer
// applies: the integration fix removed all collection routing from
// MuiLayoutDispatcher, so there is no Collection -> Common -> Layout recursion
// to avoid.) Keeping Dirlist dispatch standalone bounds each closure's branch
// range while still providing MUIM_Dirlist_* routing plus the shared List
// backbone methods for a Dirlist/Volumelist object. Unclaimed methods fall
// back to the generic collection/common/layout dispatcher.
public static class MuiDirlistDispatcher
{
	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (TryDispatch(ref platform, state, obj, message, out var result))
			return result;
		return MuiCollectionDispatcher.Dispatch(ref platform, state, obj, message);
	}

	// Focused packet seam used by the native zero-relocation evidence root. It
	// deliberately claims only the small struct-backed Set and ListGetEntry
	// family; the complete TryDispatch method below retains the full Dirlist and
	// Volumelist method surface for normal callers.
	public static bool TryDispatchPacket<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message, out uint result)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		result = 0;
		if (!MuiDirlistMessageCodec.TryReadMethodId(ref platform, message,
			out var packetMethod)) return false;
		var cls = MuiListCore.Classify(ref platform, state, obj);
		if (cls != MuiCollectionClass.Dirlist &&
			cls != MuiCollectionClass.Volumelist) return false;
		var method = packetMethod.MethodId;
		switch (method)
		{
			case MuiCommonControlPacketCore.OmGet:
				if (!MuiCommonControlPacketCore.TryReadGet(ref platform, message,
					out var getPacket)) return true;
				var storage = APTR.FromPointer(getPacket.Storage);
				if (storage.IsNull || !platform.IsMapped(storage,
					MuiGuestUlongStorage.Size)) return true;
				uint value;
				var got = cls == MuiCollectionClass.Volumelist
					? MuiVolumelistCore.GetAttribute(ref platform, state, obj,
						getPacket.Attribute, out value)
					: MuiDirlistCore.GetAttribute(ref platform, state, obj,
						getPacket.Attribute, out value);
				if (!got) return true;
				MuiGuestUlongStorageCodec.WriteValue(ref platform, storage, value);
				result = 1u;
				return true;
			case MuiDirlistMessageCodec.Set:
			case MuiDirlistMessageCodec.NoNotifySet:
				if (!MuiDirlistMessageCodec.TryReadSet(ref platform, message, method,
					out var setPacket)) return true;
				result = MuiDirlistCore.SetAttribute(ref platform, state, obj,
					setPacket.Attribute, setPacket.Value) ? 1u : 0u;
				return true;
			case MuiDirlistMessageCodec.ListGetEntry:
				if (!MuiDirlistMessageCodec.TryReadGetEntry(ref platform, message,
					out var getEntryPacket)) return true;
				result = MuiListCore.GetEntry(ref platform, state, obj,
					unchecked((int)getEntryPacket.Position),
					APTR.FromPointer(getEntryPacket.Storage)).Raw;
				return true;
		}
		return false;
	}

	public static bool TryDispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message, out uint result)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		result = 0;
		if (TryDispatchPacket(ref platform, state, obj, message, out result))
			return true;
		if (!MuiDirlistMessageCodec.TryReadMethodId(ref platform, message,
			out var methodHeader)) return false;
		var cls = MuiListCore.Classify(ref platform, state, obj);
		if (cls != MuiCollectionClass.Dirlist &&
			cls != MuiCollectionClass.Volumelist) return false;
		var method = methodHeader.MethodId;
		switch (method)
		{
			case MuiDirlistMessageCodec.ReRead:
				if (!MuiDirlistMessageCodec.IsValidMethod(ref platform, message,
					MuiDirlistMessageCodec.ReRead)) return true;
				result = (cls == MuiCollectionClass.Volumelist
					? MuiVolumelistCore.Populate(ref platform, state, obj)
					: MuiDirlistCore.ReRead(ref platform, state, obj)) ? 1u : 0u;
				return true;
			case MuiDirlistMessageCodec.Rename:
				if (!MuiDirlistMessageCodec.TryReadRename(ref platform, message,
					out var renamePacket))
					return true;
				result = unchecked((uint)MuiDirlistCore.Rename(ref platform, state, obj,
					unchecked((int)renamePacket.Entry),
					APTR.FromPointer(renamePacket.Name)));
				return true;
			case MuiDirlistMessageCodec.SetComment:
				if (!MuiDirlistMessageCodec.TryReadRename(ref platform, message,
					MuiDirlistMessageCodec.SetComment,
					out var commentPacket)) return true;
				result = unchecked((uint)MuiDirlistCore.SetComment(ref platform, state,
					obj, unchecked((int)commentPacket.Entry),
					APTR.FromPointer(commentPacket.Name)));
				return true;
			case MuiDirlistMessageCodec.SetProtection:
				if (!MuiDirlistMessageCodec.TryReadProtection(ref platform, message,
					out var protectionPacket)) return true;
				result = unchecked((uint)MuiDirlistCore.SetProtection(ref platform,
					state, obj, unchecked((int)protectionPacket.Entry),
					protectionPacket.Protection));
				return true;
			case MuiDirlistMessageCodec.ListClear:
				if (!MuiDirlistMessageCodec.IsValidMethod(ref platform, message,
					MuiDirlistMessageCodec.ListClear)) return true;
				result = MuiListCore.Clear(ref platform, state, obj) ? 1u : 0u;
				return true;
		}
		return false;
	}

}
