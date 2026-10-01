/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// MUI's bounded application-window projection uses the shared packed Exec
// List. Its local admission policy belongs to MUI, not the SDK namespace.
public static class LayersExecListCodec
{
	// Keep the historical admission guard: the Layers-facing list projection
	// reserves a 16-byte guest range even though the public List payload is the
	// 14-byte packed Exec structure. The extra two bytes are never interpreted.
	private const uint MappedSize = 16;

	public static bool TryRead<TMemory>(ref TMemory memory, APTR address,
		out List value)
		where TMemory : struct, IAmigaGuestMemory
	{
		value = default;
		if (address.IsNull || !memory.IsMapped(address, MappedSize))
			return false;

		// Decode the published packed List fields in declaration order. The
		// cursor is the sole guest-address adapter; the result remains the named
		// Amiga.List value type even though the pinned package omits ExecListCodec.
		if (!MuiGuestStructCursor.TryCreate(ref memory, address, List.Size,
			out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var head) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var tail) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var tailPred) ||
			!MuiGuestStructCursor.TryReadUInt8(ref memory, ref cursor,
				out var type) ||
			!MuiGuestStructCursor.TryReadUInt8(ref memory, ref cursor,
				out value.Padding) ||
			!MuiGuestStructCursor.IsComplete(cursor))
		{
			value = default;
			return false;
		}

		value.Head = APTR.FromPointer(head);
		value.Tail = APTR.FromPointer(tail);
		value.TailPred = APTR.FromPointer(tailPred);
		value.Type = (NodeType)type;
		return true;
	}

	public static APTR ReadHead<TMemory>(ref TMemory memory, APTR address)
		where TMemory : struct, IAmigaGuestMemory
	{
		if (address.IsNull || !memory.IsMapped(address, MappedSize) ||
			!MuiGuestStructCursor.TryCreate(ref memory, address, List.Size,
				out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var head)) return APTR.Null;
		return APTR.FromPointer(head);
	}

	public static bool Write<TMemory>(ref TMemory memory, APTR address,
		List value)
		where TMemory : struct, IAmigaGuestMemory
	{
		if (address.IsNull || !memory.IsMapped(address, MappedSize) ||
			!MuiGuestStructCursor.TryCreate(ref memory, address, List.Size,
				out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
				value.Head.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
				value.Tail.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
				value.TailPred.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref memory, ref cursor,
				(byte)value.Type) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref memory, ref cursor,
				value.Padding)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}
