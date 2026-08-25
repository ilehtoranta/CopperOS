/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace Amiga;

// The local CopperSharp SDK deliberately omits the broad Layers guest-codec
// surface from its host assembly. MUI's application-window projection only
// needs the public Exec List head, so keep this fallback narrow and named
// rather than importing the complete Layers implementation or exposing a raw
// private offset to MUI code. The package build receives the SDK codec instead.
public static class LayersExecListCodec
{
	private const uint ListSize = 16;
	private const int HeadOffset = 0;

	public static APTR ReadHead<TMemory>(ref TMemory memory, APTR address)
		where TMemory : struct, IAmigaGuestMemory
	{
		if (address.IsNull || !memory.IsMapped(address, ListSize))
			return APTR.Null;
		var head = APTR.FromPointer(memory.ReadUInt32(address, HeadOffset));
		return head;
	}
}
