/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Commodities' IX is a fixed, public 12-byte MorphOS record. Keep it typed
// with the SDK shape; only this bounded codec crosses guest memory.
internal static class MuiInputExpressionMemoryCodec
{
	internal const byte Version = 2;
	internal const uint MaximumDescriptionLength = 4096;

	internal static bool TryRead<TMemory>(ref TMemory memory, APTR address,
		out InputXpression value)
		where TMemory : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			InputXpression.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt8(ref memory, ref cursor,
				out value.Version) ||
			!MuiGuestStructCursor.TryReadUInt8(ref memory, ref cursor,
				out value.Class) ||
			!MuiGuestStructCursor.TryReadUInt16(ref memory, ref cursor,
				out value.Code) ||
			!MuiGuestStructCursor.TryReadUInt16(ref memory, ref cursor,
				out value.CodeMask) ||
			!MuiGuestStructCursor.TryReadUInt16(ref memory, ref cursor,
				out value.Qualifier) ||
			!MuiGuestStructCursor.TryReadUInt16(ref memory, ref cursor,
				out value.QualifierMask) ||
			!MuiGuestStructCursor.TryReadUInt16(ref memory, ref cursor,
				out value.QualifierSame) ||
			!MuiGuestStructCursor.IsComplete(cursor) ||
			value.Version != Version) return false;
		return true;
	}

	internal static bool Write<TMemory>(ref TMemory memory, APTR address,
		InputXpression value)
		where TMemory : struct, IMuiGuestMemory =>
		value.Version == Version &&
		MuiGuestStructCursor.TryCreate(ref memory, address,
			InputXpression.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt8(ref memory, ref cursor,
			value.Version) &&
		MuiGuestStructCursor.TryWriteUInt8(ref memory, ref cursor,
			value.Class) &&
		MuiGuestStructCursor.TryWriteUInt16(ref memory, ref cursor,
			value.Code) &&
		MuiGuestStructCursor.TryWriteUInt16(ref memory, ref cursor,
			value.CodeMask) &&
		MuiGuestStructCursor.TryWriteUInt16(ref memory, ref cursor,
			value.Qualifier) &&
		MuiGuestStructCursor.TryWriteUInt16(ref memory, ref cursor,
			value.QualifierMask) &&
		MuiGuestStructCursor.TryWriteUInt16(ref memory, ref cursor,
			value.QualifierSame) && MuiGuestStructCursor.IsComplete(cursor);
}
