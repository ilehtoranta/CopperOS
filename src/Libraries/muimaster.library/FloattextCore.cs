/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Named cursor for bounded Floattext guest-byte spans. A span carries its
// guest base, logical index, and validated byte length; all source/scratch
// reads and writes use this adapter so consumers never form indexed guest
// addresses themselves.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFloattextByteCursor
{
	internal const uint MaximumLength = 65536;
	internal APTR Base;
	internal uint Index;
	internal uint Length;
}

internal static class MuiFloattextByteCursorCodec
{
	internal static bool TryReadAt<TPlatform>(ref TPlatform platform,
		APTR baseAddress, uint length, int index, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (index < 0) return false;
		var cursor = default(MuiFloattextByteCursor);
		cursor.Base = baseAddress;
		cursor.Index = (uint)index;
		cursor.Length = length;
		return TryReadByte(ref platform, cursor, out value);
	}

	internal static bool TryGetByte<TPlatform>(ref TPlatform platform,
		MuiFloattextByteCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var shared = default(MuiCStringByteCursor);
		shared.Base = cursor.Base;
		shared.Index = cursor.Index;
		shared.Limit = cursor.Length;
		return MuiCStringByteCursorCodec.TryGetAddress(ref platform, shared,
			out address);
	}

	internal static bool TryReadByte<TPlatform>(ref TPlatform platform,
		MuiFloattextByteCursor cursor, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var shared = default(MuiCStringByteCursor);
		shared.Base = cursor.Base;
		shared.Index = cursor.Index;
		shared.Limit = cursor.Length;
		return MuiCStringByteCursorCodec.TryReadByte(ref platform, shared,
			out value);
	}

	internal static bool TryWriteByte<TPlatform>(ref TPlatform platform,
		MuiFloattextByteCursor cursor, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var shared = default(MuiCStringByteCursor);
		shared.Base = cursor.Base;
		shared.Index = cursor.Index;
		shared.Limit = cursor.Length;
		return MuiCStringByteCursorCodec.TryWriteByte(ref platform, shared,
			value);
	}
}

// Floattext keeps its caller-facing policy and owned text pointers together
// so parsing/rebuild paths consume one canonical record instead of rereading
// individual attributes. The pointers refer to guest-owned dataspace copies.
public struct MuiFloattextState
{
	public APTR Text;
	public APTR SkipChars;
	public uint TabSize;
	public uint Justify;
	public uint Width;
}

// Guest-resident Floattext policy. Text and SkipChars point at the private
// dataspace copies owned by Floattext; the scalar policy values are normalized
// at the same boundary. Keeping the complete policy together prevents parser
// and append paths from rebuilding state from unrelated raw attribute words.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFloattextPolicyState
{
	internal const uint Size = 24;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint TextOffset = 4;
	internal const uint SkipCharsOffset = 8;
	internal const uint TabSizeOffset = 12;
	internal const uint JustifyOffset = 16;
	internal const uint WidthOffset = 20;
	internal const uint Cookie = 0x4654504Cu; // 'FTPL'

	internal uint Magic;
	internal APTR Text;
	internal APTR SkipChars;
	internal uint TabSize;
	internal uint Justify;
	internal uint Width;
}

internal enum MuiFloattextPolicyField : byte
{
	Magic,
	Text,
	SkipChars,
	TabSize,
	Justify,
	Width,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFloattextPolicyFieldCursor
{
	internal APTR Record;
	internal MuiFloattextPolicyField Field;
}

// The fixed Floattext policy record owns its packed positions in this bounded
// adapter. Live policy paths use it directly; the typed field cursor below is
// retained only for compatibility callers and adapter-focused tests.
internal static class MuiFloattextPolicyStateMemoryCodec
{
	private static bool TryResolve(MuiFloattextPolicyField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiFloattextPolicyField.Magic:
				offset = MuiFloattextPolicyState.MagicOffset; return true;
			case MuiFloattextPolicyField.Text:
				offset = MuiFloattextPolicyState.TextOffset; return true;
			case MuiFloattextPolicyField.SkipChars:
				offset = MuiFloattextPolicyState.SkipCharsOffset; return true;
			case MuiFloattextPolicyField.TabSize:
				offset = MuiFloattextPolicyState.TabSizeOffset; return true;
			case MuiFloattextPolicyField.Justify:
				offset = MuiFloattextPolicyState.JustifyOffset; return true;
			case MuiFloattextPolicyField.Width:
				offset = MuiFloattextPolicyState.WidthOffset; return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiFloattextPolicyField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, MuiFloattextPolicyState.Size))
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiFloattextPolicyState.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiFloattextPolicyField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiFloattextPolicyField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiFloattextPolicyFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiFloattextPolicyFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiFloattextPolicyStateMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiFloattextPolicyField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiFloattextPolicyStateMemoryCodec.TryReadUInt32(ref platform, record,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiFloattextPolicyField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiFloattextPolicyStateMemoryCodec.TryWriteUInt32(ref platform, record,
			field, value);
}

internal static class MuiFloattextPolicyStateCodec
{
	// Production access is sequential and struct-shaped. The field-address
	// adapters above remain available for compatibility diagnostics only.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiFloattextPolicyState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if ((address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiFloattextPolicyState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var text) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var skipChars) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var tabSize) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var justify) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var width) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.Magic = magic;
		value.Text = APTR.FromPointer(text);
		value.SkipChars = APTR.FromPointer(skipChars);
		value.TabSize = tabSize;
		value.Justify = justify;
		value.Width = width;
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiFloattextPolicyState value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiFloattextPolicyState value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value) &&
		value.Magic == MuiFloattextPolicyState.Cookie;

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiFloattextPolicyState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if ((address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiFloattextPolicyState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Text.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.SkipChars.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.TabSize) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Justify) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Width)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiFloattextPolicyState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return value.Magic == MuiFloattextPolicyState.Cookie &&
			WriteRecord(ref platform, address, value);
	}
}

internal static class MuiFloattextPolicyValidation
{
	// Justify is the only BOOL in this record. Keep the raw guest byte lossless
	// in the codec, then reject any non-canonical value at the semantic boundary.
	internal static bool IsValid(MuiFloattextPolicyState value) =>
		value.Justify <= 1;
}

// Floattext.mui (autodoc MUI_Floattext.doc). Floattext is a subclass of list
// class that takes one big string and splits it into display rows, honouring
// paragraph linefeeds, tab expansion (MUIA_Floattext_TabSize), skipped control
// characters (MUIA_Floattext_SkipChars) and optional word-wrap justification
// (MUIA_Floattext_Justify). MUI copies the supplied string into a private
// buffer, so the caller need not keep it; the copy and the parsed rows live in
// guest memory and are freed on disposal. Rebuilds (on Text/SkipChars/TabSize/
// Justify changes and MUIM_Floattext_Append) are atomic: the row set is cleared
// and repopulated, and a mid-rebuild allocation failure leaves a valid, empty
// list rather than a partially wrapped one. No managed allocations are used.
public static class MuiFloattextCore
{
	// ---- Public attribute / method identifiers (autodoc MUI_Floattext.doc) ---
	private const uint Justify = 0x8042dc03u;    // [ISG] BOOL
	private const uint SkipChars = 0x80425c7du;  // [IS.] STRPTR
	private const uint TabSize = 0x80427d17u;    // [IS.] LONG (defaults to 8)
	private const uint Text = 0x8042d16au;       // [ISG] STRPTR
	public const uint MethodAppend = 0x8042a221u;// MUIM_Floattext_Append

	// MUIA_Width (shared area attribute) drives word-wrap; interpreted through a
	// fixed character cell so wrapping is deterministic without a render context.
	private const uint Width = 0x8042B59Cu;
	private const uint CharCell = 8;

	// Guest-owned dataspace keys, retired automatically through the object store
	// on disposal (StoreCore owns the copied data).
	private const uint TextKey = 0x0F100001u;
	private const uint SkipKey = 0x0F100002u;
	private const uint PolicyKey = 0x0F100004u;
	// Transactional append staging.  The old TextKey remains intact until the
	// staged rows have been parsed successfully; the pending record is ordinary
	// guest dataspace and is retired on every exit path.
	private const uint PendingTextKey = 0x0F100003u;

	private const uint MaximumTextLength = 65536;
	private const uint MaximumLineLength = 2048;
	private const uint MaximumSkipLength = 256;
	private const uint DefaultTabSize = 8;

	// ---- Construction ---------------------------------------------------------

	// Create a Floattext, failure-atomically. The list backbone is constructed
	// (shared with List), defaults are applied, any creation-time Text/SkipChars
	// are copied into private buffers, and the text is parsed into rows.
	public static APTR CreateFloattext<TPlatform>(ref TPlatform platform,
		APTR state, APTR classRecord, APTR tags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiListCore.ClassifyRecord(ref platform, classRecord) !=
			MuiCollectionClass.Floattext) return APTR.Null;
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, state,
			classRecord, tags);
		if (obj.IsNull) return APTR.Null;
		if (!MuiListCore.Construct(ref platform, state, classRecord, obj) ||
			!Setup(ref platform, state, obj))
		{
			MuiCollectionLifecycle.DisposeObject(ref platform, state, obj);
			return APTR.Null;
		}
		return obj;
	}

	private static bool Setup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiListCore.HasBackbone(ref platform, state, obj)) return false;
		EnsureDefault(ref platform, state, obj, TabSize, DefaultTabSize);
		EnsureDefault(ref platform, state, obj, Justify, 0);
		if (!NormalizeState(ref platform, state, obj)) return false;

		// Creation tags stored the raw STRPTRs; own private copies of them.
		var rawSkip = APTR.FromPointer(Read(ref platform, state, obj, SkipChars, 0));
		if (rawSkip.IsNotNull && !OwnString(ref platform, state, obj, SkipKey,
			rawSkip, MaximumSkipLength)) return false;
		if (rawSkip.IsNotNull)
			SetInternal(ref platform, state, obj, SkipChars,
				MuiStoreCore.DataspaceFind(ref platform, state, obj, SkipKey).Raw);
		var rawText = APTR.FromPointer(Read(ref platform, state, obj, Text, 0));
		if (rawText.IsNotNull)
		{
			if (!OwnString(ref platform, state, obj, TextKey, rawText,
				MaximumTextLength)) return false;
			SetInternal(ref platform, state, obj, Text,
				MuiStoreCore.DataspaceFind(ref platform, state, obj, TextKey).Raw);
		}
		if (!EnsurePolicyState(ref platform, state, obj)) return false;
		return Rebuild(ref platform, state, obj);
	}

	private static bool NormalizeState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var justify = Read(ref platform, state, obj, Justify, 0) == 0 ?
			0u : 1u;
		return SetInternal(ref platform, state, obj, Justify, justify);
	}

	private static bool TryReadPolicyState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiFloattextPolicyState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (!TryReadPolicyAdmission(ref platform, state, obj, out value,
			out var present) || !present) return false;
		return ValidateOwnedPolicyPointers(ref platform, state, obj, value);
	}

	// A policy dataspace may be absent on legacy objects, but a present block is
	// authoritative typed state.  Do not reinterpret a wrong-sized or malformed
	// block as permission to fall back to raw attributes.
	private static bool TryReadPolicyAdmission<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiFloattextPolicyState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PolicyKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			PolicyKey);
		present = block.IsNotNull || length != 0;
		if (!present) return true;
		if (block.IsNull || length != unchecked((int)MuiFloattextPolicyState.Size))
			return false;
		return MuiFloattextPolicyStateCodec.TryRead(ref platform, block,
			out value) && MuiFloattextPolicyValidation.IsValid(value);
	}

	private static bool ValidateOwnedPolicyPointers<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiFloattextPolicyState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var text = MuiStoreCore.DataspaceFind(ref platform, state, obj, TextKey);
		var skip = MuiStoreCore.DataspaceFind(ref platform, state, obj, SkipKey);
		if (!ValidateOwnedPointer(ref platform, value.Text, text,
			MaximumTextLength) || !ValidateOwnedPointer(ref platform,
			value.SkipChars, skip, MaximumSkipLength)) return false;
		return true;
	}

	private static bool ValidateOwnedPointer<TPlatform>(ref TPlatform platform,
		APTR projected, APTR owned, uint maximum)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (projected.IsNull) return owned.IsNull;
		return owned.IsNotNull && projected.Raw == owned.Raw &&
			CStringCodec.TryReadLength(ref platform, owned, maximum, out _);
	}

	// Area layout owns the effective render width.  Keep the policy record's
	// explicit Width field for the public attribute boundary, but let parsing
	// consume the typed geometry projection whenever one is available.  This
	// also reconciles a raw public width write through AreaLayoutCore's existing
	// geometry record boundary instead of introducing another scalar offset.
	private static uint ReadEffectiveWidth<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiAreaLayoutCore.TryReadGeometryState(ref platform, state, obj,
			out var geometry))
			return geometry.Width <= 0 ? 0u : unchecked((uint)geometry.Width);
		return Read(ref platform, state, obj, Width, 0);
	}

	// Keep the policy record synchronized with the separately owned text/skip
	// dataspace entries and the public scalar attributes. The record itself is
	// stored in Dataspace so object disposal retires it with the other copies.
	private static bool SyncPolicyState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		// SetText/SetSkipChars deliberately update the owned dataspace before
		// moving the corresponding pointer in this record.  Read the already
		// admitted wire record without applying the pointer-coherence check again,
		// then publish the complete replacement atomically through the codec.
		if (!TryReadPolicyAdmission(ref platform, state, obj, out var value,
			out var present) || !present)
			return false;
		value.Text = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			TextKey);
		value.SkipChars = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			SkipKey);
		value.TabSize = Read(ref platform, state, obj, TabSize, DefaultTabSize);
		value.Justify = Read(ref platform, state, obj, Justify, 0) == 0 ? 0u : 1u;
		value.Width = ReadEffectiveWidth(ref platform, state, obj);
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PolicyKey);
		return MuiFloattextPolicyStateCodec.Write(ref platform, block, value);
	}

	private static bool EnsurePolicyState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadPolicyState(ref platform, state, obj, out _))
			return SyncPolicyState(ref platform, state, obj);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiFloattextPolicyState.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiFloattextPolicyState.Size);
		var value = default(MuiFloattextPolicyState);
		value.Magic = MuiFloattextPolicyState.Cookie;
		// Seed the complete typed projection before admission validation.  A
		// freshly-created record must agree with the already-owned Text/SkipChars
		// blocks; writing an all-zero placeholder would be rejected as malformed
		// pointer divergence by SyncPolicyState.
		value.Text = MuiStoreCore.DataspaceFind(ref platform, state, obj, TextKey);
		value.SkipChars = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			SkipKey);
		value.TabSize = Read(ref platform, state, obj, TabSize, DefaultTabSize);
		value.Justify = Read(ref platform, state, obj, Justify, 0) == 0 ? 0u : 1u;
		value.Width = ReadEffectiveWidth(ref platform, state, obj);
		var written = MuiFloattextPolicyStateCodec.Write(ref platform, scratch,
			value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			PolicyKey, scratch, unchecked((int)MuiFloattextPolicyState.Size));
		platform.Clear(scratch, MuiFloattextPolicyState.Size);
		platform.Free(scratch, MuiFloattextPolicyState.Size);
		return added && SyncPolicyState(ref platform, state, obj);
	}

	// Internal qualification seam for the complete guest-resident policy.
	internal static bool TryGetPolicyState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiFloattextPolicyState value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadPolicyState(ref platform, state, obj, out value);

	// ---- Attribute access -----------------------------------------------------

	internal static bool IsStateAttribute(uint attribute) =>
		attribute == Text || attribute == SkipChars || attribute == TabSize ||
		attribute == Justify || attribute == Width;

	// Public struct-first inspection seam for the owned text/skip pointers and
	// the rebuild policy consumed by Floattext parsing.
	public static bool TryReadState<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiFloattextState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (MuiListCore.Classify(ref platform, state, obj) !=
			MuiCollectionClass.Floattext) return false;
		if (!TryReadPolicyAdmission(ref platform, state, obj, out var policy,
			out var present)) return false;
		if (present)
		{
			if (!ValidateOwnedPolicyPointers(ref platform, state, obj, policy))
				return false;
			result.Text = policy.Text;
			result.SkipChars = policy.SkipChars;
			result.TabSize = policy.TabSize;
			result.Justify = policy.Justify;
			result.Width = ReadEffectiveWidth(ref platform, state, obj);
			return true;
		}
		result.Text = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			TextKey);
		result.SkipChars = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			SkipKey);
		result.TabSize = Read(ref platform, state, obj, TabSize, DefaultTabSize);
		result.Justify = Read(ref platform, state, obj, Justify, 0) == 0 ?
			0u : 1u;
		result.Width = ReadEffectiveWidth(ref platform, state, obj);
		return true;
	}

	// Hook used by the shared List dispatcher for Floattext-specific attributes.
	// The fixed policy record remains the only state consumed by rebuild paths;
	// generic List attributes continue through MuiListCore.
	internal static bool TrySetAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue) || MuiListCore.ClassifyRecord(ref platform,
			objectValue.Class) != MuiCollectionClass.Floattext ||
			!IsStateAttribute(attribute)) return false;
		return SetKnown(ref platform, state, objectValue.Boopsi, attribute,
			value, notify);
	}

	// MUIA_Floattext_Text returns the private buffer (or NULL when empty), per
	// the autodoc contract that callers must handle a NULL result.
	public static bool GetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (IsStateAttribute(attribute))
		{
			if (!TryReadPolicyState(ref platform, state, obj, out var policy))
			{
				value = 0;
				return false;
			}
			if (attribute == Width)
			{
				// Width is the shared Area geometry projection.  The policy
				// record retains the last synchronized value for parsing, but a
				// layout pass can publish a newer named geometry record without
				// touching Floattext's policy dataspace.  Get/OM_GET must expose
				// that current typed geometry rather than a stale scalar copy.
				value = ReadEffectiveWidth(ref platform, state, obj);
				return true;
			}
			value = attribute == Text ? policy.Text.Raw :
				attribute == SkipChars ? policy.SkipChars.Raw :
				attribute == TabSize ? policy.TabSize :
				attribute == Justify ? policy.Justify : policy.Width;
			return true;
		}
		return MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			attribute, out value);
	}

	private static bool SetKnown<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		// EnsurePolicyState may materialize a legacy-absent record, but it must
		// never repair a malformed present record by replacing the typed state.
		// Admit the existing record and its owned pointer projections before any
		// setter can mutate raw attributes or retire a copied string.
		if (!TryReadPolicyAdmission(ref platform, state, obj, out var existing,
			out var present)) return false;
		if (present && !ValidateOwnedPolicyPointers(ref platform, state, obj,
			existing)) return false;
		if (!EnsurePolicyState(ref platform, state, obj)) return false;
		if (attribute == Text)
			return SetText(ref platform, state, obj, APTR.FromPointer(value),
				notify);
		if (attribute == SkipChars)
		{
			var skip = APTR.FromPointer(value);
			if (skip.IsNull)
			{
				MuiStoreCore.DataspaceRemove(ref platform, state, obj, SkipKey);
				if (!SetInternal(ref platform, state, obj, SkipChars, 0, notify))
					return false;
			}
			else
			{
				if (!OwnString(ref platform, state, obj, SkipKey, skip,
					MaximumSkipLength)) return false;
				var owned = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					SkipKey);
				if (!SetInternal(ref platform, state, obj, SkipChars, owned.Raw,
					notify)) return false;
			}
			if (!SyncPolicyState(ref platform, state, obj)) return false;
			return Rebuild(ref platform, state, obj);
		}
		if (attribute == TabSize || attribute == Justify || attribute == Width)
		{
			var normalized = attribute == Justify && value != 0 ? 1u : value;
			if (!SetInternal(ref platform, state, obj, attribute, normalized,
				notify)) return false;
			if (!SyncPolicyState(ref platform, state, obj)) return false;
			return Rebuild(ref platform, state, obj);
		}
		return false;
	}

	// Set a Floattext attribute, rebuilding the row set atomically when the
	// change affects layout. MUIA_Floattext_Text == NULL clears the text.
	public static bool SetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value, bool notify = false)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiListCore.Classify(ref platform, state, obj) !=
			MuiCollectionClass.Floattext)
			return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, value, notify);
		if (IsStateAttribute(attribute))
			return SetKnown(ref platform, state, obj, attribute, value, notify);
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			attribute, value, notify);
	}

	private static bool SetText<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR text, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (text.IsNull)
		{
			MuiStoreCore.DataspaceRemove(ref platform, state, obj, TextKey);
			if (!SetInternal(ref platform, state, obj, Text, 0, notify))
				return false;
		}
		else
		{
			if (!OwnString(ref platform, state, obj, TextKey, text,
				MaximumTextLength)) return false;
			if (!SetInternal(ref platform, state, obj, Text,
				MuiStoreCore.DataspaceFind(ref platform, state, obj, TextKey).Raw,
				notify)) return false;
		}
		if (!SyncPolicyState(ref platform, state, obj)) return false;
		return Rebuild(ref platform, state, obj);
	}

	// ---- MUIM_Floattext_Append ------------------------------------------------

	// Append text to the current contents and rebuild. The combined string is
	// materialized in a scratch buffer, copied into the private text buffer, and
	// re-wrapped. Failure leaves the existing text unchanged.
	public static bool Append<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR text) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiListCore.Classify(ref platform, state, obj) !=
			MuiCollectionClass.Floattext) return false;
		if (text.IsNull) return true;
		if (!CStringCodec.TryReadLength(ref platform, text, MaximumTextLength,
			out var addLength)) return false;
		if (addLength == 0) return true;

		if (!TryReadState(ref platform, state, obj, out var current)) return false;
		var existing = current.Text;
		uint oldLength = 0;
		if (existing.IsNotNull && !CStringCodec.TryReadLength(ref platform, existing,
			MaximumTextLength, out oldLength)) return false;
		var total = oldLength + addLength;
		if (total >= MaximumTextLength) return false;

		var scratch = MuiHeadlessMemory.Allocate(ref platform, total + 1);
		if (scratch.IsNull) return false;
		var scratchCursor = default(MuiFloattextByteCursor);
		scratchCursor.Base = scratch;
		scratchCursor.Length = total + 1;
		var existingCursor = default(MuiFloattextByteCursor);
		existingCursor.Base = existing;
		existingCursor.Length = oldLength;
		for (var i = 0u; i < oldLength; i++)
		{
			existingCursor.Index = i;
			scratchCursor.Index = i;
			if (!MuiFloattextByteCursorCodec.TryReadByte(ref platform,
				existingCursor, out var oldValue) ||
				!MuiFloattextByteCursorCodec.TryWriteByte(ref platform,
					scratchCursor, oldValue))
			{
				platform.Clear(scratch, total + 1);
				platform.Free(scratch, total + 1);
				return false;
			}
		}
		var appendCursor = default(MuiFloattextByteCursor);
		appendCursor.Base = text;
		appendCursor.Length = addLength;
		for (var i = 0u; i < addLength; i++)
		{
			appendCursor.Index = i;
			scratchCursor.Index = oldLength + i;
			if (!MuiFloattextByteCursorCodec.TryReadByte(ref platform,
				appendCursor, out var appendedValue) ||
				!MuiFloattextByteCursorCodec.TryWriteByte(ref platform,
					scratchCursor, appendedValue))
			{
				platform.Clear(scratch, total + 1);
				platform.Free(scratch, total + 1);
				return false;
			}
		}
		scratchCursor.Index = total;
		if (!MuiFloattextByteCursorCodec.TryWriteByte(ref platform,
			scratchCursor, 0))
		{
			platform.Clear(scratch, total + 1);
			platform.Free(scratch, total + 1);
			return false;
		}

		var ok = MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			PendingTextKey, scratch, (int)(total + 1));
		platform.Clear(scratch, total + 1);
		platform.Free(scratch, total + 1);
		if (!ok) return false;

		var pending = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PendingTextKey);
		if (pending.IsNull)
		{
			MuiStoreCore.DataspaceRemove(ref platform, state, obj, PendingTextKey);
			return false;
		}
		// Parse against the staged guest buffer while the public Text pointer and
		// TextKey still identify the previous committed value.  If parsing or the
		// final copy fails, rebuild from that old pointer and leave the public
		// contents unchanged.
		if (!RebuildFromSource(ref platform, state, obj, pending))
		{
			MuiStoreCore.DataspaceRemove(ref platform, state, obj, PendingTextKey);
			RebuildFromSource(ref platform, state, obj, existing);
			return false;
		}
		if (!MuiStoreCore.DataspaceAdd(ref platform, state, obj, TextKey,
			pending, (int)(total + 1)))
		{
			MuiStoreCore.DataspaceRemove(ref platform, state, obj, PendingTextKey);
			RebuildFromSource(ref platform, state, obj, existing);
			return false;
		}
		var committed = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			TextKey);
		if (committed.IsNull || !SetInternal(ref platform, state, obj, Text,
			committed.Raw, false))
		{
			MuiStoreCore.DataspaceRemove(ref platform, state, obj, PendingTextKey);
			return false;
		}
		MuiStoreCore.DataspaceRemove(ref platform, state, obj, PendingTextKey);
		return SyncPolicyState(ref platform, state, obj);
	}

	// ---- Row (re)builder ------------------------------------------------------

	// Clear the current rows and repopulate from the owned text buffer. Atomic:
	// on any allocation failure the list is cleared back to empty and false is
	// returned.
	public static bool Rebuild<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadState(ref platform, state, obj, out var current)) return false;
		return RebuildFromSource(ref platform, state, obj, current.Text);
	}

	private static bool RebuildFromSource<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR text)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiListCore.HasBackbone(ref platform, state, obj)) return false;
		MuiListCore.Clear(ref platform, state, obj);
		if (text.IsNull) return true; // no text -> empty list
		if (!CStringCodec.TryReadLength(ref platform, text, MaximumTextLength,
			out var textLength) || textLength == 0) return true;
		if (!TryReadState(ref platform, state, obj, out var current)) return false;

		var tabSize = current.TabSize;
		var justify = current.Justify != 0;
		var width = current.Width;
		var wrapCols = width >= CharCell ? width / CharCell : 0;
		var skip = current.SkipChars;

		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MaximumLineLength + 1);
		if (scratch.IsNull) return false;

		var ok = Parse(ref platform, state, obj, text, textLength, skip, tabSize,
			justify, wrapCols, scratch);
		platform.Clear(scratch, MaximumLineLength + 1);
		platform.Free(scratch, MaximumLineLength + 1);
		if (!ok)
		{
			MuiListCore.Clear(ref platform, state, obj);
			return false;
		}
		return true;
	}

	// Deterministic single-pass parser. Characters in the skip set are dropped,
	// tabs are expanded to the next tab stop, linefeeds end a paragraph line
	// (never justified) and, when a wrap column is set, over-long lines break at
	// the last word boundary (justified when requested) or hard-break.
	private static bool Parse<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR text, uint textLength, APTR skip, uint tabSize, bool justify,
		uint wrapCols, APTR scratch) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var lineLen = 0;      // bytes currently in scratch (== visual column)
		var lastSpace = -1;   // index in scratch of the most recent space
		var textCursor = default(MuiFloattextByteCursor);
		textCursor.Base = text;
		textCursor.Length = textLength;
		for (var i = 0u; i < textLength; i++)
		{
			textCursor.Index = i;
			if (!MuiFloattextByteCursorCodec.TryReadByte(ref platform,
				textCursor, out var ch)) return false;
			if (ch == (byte)'\r') continue;
			if (InSkip(ref platform, skip, ch)) continue;
			if (ch == (byte)'\n')
			{
				if (!EmitLine(ref platform, state, obj, scratch, lineLen, false,
					wrapCols)) return false;
				lineLen = 0;
				lastSpace = -1;
				continue;
			}
			if (ch == (byte)'\t')
			{
				var spaces = tabSize == 0 ? 1 : (int)(tabSize - ((uint)lineLen %
					tabSize));
				for (var s = 0; s < spaces; s++)
					if (!AddChar(ref platform, state, obj, scratch, (byte)' ', justify,
						wrapCols, ref lineLen, ref lastSpace)) return false;
				continue;
			}
			if (!AddChar(ref platform, state, obj, scratch, ch, justify, wrapCols,
				ref lineLen, ref lastSpace)) return false;
		}
		if (lineLen > 0)
			return EmitLine(ref platform, state, obj, scratch, lineLen, false,
				wrapCols);
		return true;
	}

	private static bool AddChar<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR scratch, byte ch, bool justify, uint wrapCols,
		ref int lineLen, ref int lastSpace)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (wrapCols > 0 && (uint)lineLen >= wrapCols)
		{
			if (lastSpace >= 0 && lastSpace < lineLen)
			{
				// Break at the last space; carry the trailing word to the next line.
				if (!EmitLine(ref platform, state, obj, scratch, lastSpace, justify,
					wrapCols)) return false;
				var carryStart = lastSpace + 1;
				var carryLen = lineLen - carryStart;
				var carrySource = default(MuiFloattextByteCursor);
				carrySource.Base = scratch;
				carrySource.Length = MaximumLineLength + 1;
				var carryDestination = default(MuiFloattextByteCursor);
				carryDestination.Base = scratch;
				carryDestination.Length = MaximumLineLength + 1;
				for (var k = 0; k < carryLen; k++)
				{
					carrySource.Index = (uint)(carryStart + k);
					carryDestination.Index = (uint)k;
					if (!MuiFloattextByteCursorCodec.TryReadByte(ref platform,
						carrySource, out var carryValue) ||
						!MuiFloattextByteCursorCodec.TryWriteByte(ref platform,
							carryDestination, carryValue)) return false;
				}
				lineLen = carryLen;
				lastSpace = -1;
			}
			else
			{
				// No word boundary: hard break (cannot justify a gapless line).
				if (!EmitLine(ref platform, state, obj, scratch, lineLen, false,
					wrapCols)) return false;
				lineLen = 0;
				lastSpace = -1;
			}
		}
		if ((uint)lineLen < MaximumLineLength)
		{
			var cursor = default(MuiFloattextByteCursor);
			cursor.Base = scratch;
			cursor.Index = (uint)lineLen;
			cursor.Length = MaximumLineLength + 1;
			if (!MuiFloattextByteCursorCodec.TryWriteByte(ref platform, cursor, ch))
				return false;
			if (ch == (byte)' ') lastSpace = lineLen;
			lineLen++;
		}
		return true;
	}

	// Copy scratch[0..length) into a private owned buffer, optionally inserting
	// spaces between words to justify to wrapCols, and append it as a list row.
	private static bool EmitLine<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR scratch, int length, bool justify, uint wrapCols)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var lineLength = length < 0 ? 0 : length;
		var gaps = 0;
		if (justify && wrapCols > (uint)lineLength && lineLength > 0)
			gaps = CountGaps(ref platform, scratch, lineLength);
		var extra = gaps > 0 ? (int)wrapCols - lineLength : 0;
		var outLength = lineLength + extra;
		if ((uint)outLength > MaximumLineLength)
		{
			outLength = lineLength;
			extra = 0;
			gaps = 0;
		}

		var buffer = MuiHeadlessMemory.Allocate(ref platform, (uint)outLength + 1);
		if (buffer.IsNull) return false;
		var scratchCursor = default(MuiFloattextByteCursor);
		scratchCursor.Base = scratch;
		scratchCursor.Length = MaximumLineLength + 1;
		var bufferCursor = default(MuiFloattextByteCursor);
		bufferCursor.Base = buffer;
		bufferCursor.Length = (uint)outLength + 1;
		if (gaps > 0)
		{
			var per = extra / gaps;
			var remainder = extra % gaps;
			var outIndex = 0;
			var gapIndex = 0;
			for (var i = 0; i < lineLength; i++)
			{
				scratchCursor.Index = (uint)i;
				if (!MuiFloattextByteCursorCodec.TryReadByte(ref platform,
					scratchCursor, out var ch))
				{
					platform.Clear(buffer, (uint)outLength + 1);
					platform.Free(buffer, (uint)outLength + 1);
					return false;
				}
				bufferCursor.Index = (uint)outIndex++;
				if (!MuiFloattextByteCursorCodec.TryWriteByte(ref platform,
					bufferCursor, ch))
				{
					platform.Clear(buffer, (uint)outLength + 1);
					platform.Free(buffer, (uint)outLength + 1);
					return false;
				}
				if (ch == (byte)' ' && IsWordGap(ref platform, scratch, lineLength, i))
				{
					var add = per + (gapIndex < remainder ? 1 : 0);
					gapIndex++;
					for (var s = 0; s < add; s++)
					{
						bufferCursor.Index = (uint)outIndex++;
						if (!MuiFloattextByteCursorCodec.TryWriteByte(ref platform,
							bufferCursor, (byte)' '))
						{
							platform.Clear(buffer, (uint)outLength + 1);
							platform.Free(buffer, (uint)outLength + 1);
							return false;
						}
					}
				}
			}
			bufferCursor.Index = (uint)outIndex;
			if (!MuiFloattextByteCursorCodec.TryWriteByte(ref platform,
				bufferCursor, 0))
			{
				platform.Clear(buffer, (uint)outLength + 1);
				platform.Free(buffer, (uint)outLength + 1);
				return false;
			}
		}
		else
		{
			for (var i = 0; i < lineLength; i++)
			{
				scratchCursor.Index = (uint)i;
				if (!MuiFloattextByteCursorCodec.TryReadByte(ref platform,
					scratchCursor, out var ch) )
				{
					platform.Clear(buffer, (uint)outLength + 1);
					platform.Free(buffer, (uint)outLength + 1);
					return false;
				}
				bufferCursor.Index = (uint)i;
				if (!MuiFloattextByteCursorCodec.TryWriteByte(ref platform,
					bufferCursor, ch))
				{
					platform.Clear(buffer, (uint)outLength + 1);
					platform.Free(buffer, (uint)outLength + 1);
					return false;
				}
			}
			bufferCursor.Index = (uint)lineLength;
			if (!MuiFloattextByteCursorCodec.TryWriteByte(ref platform,
				bufferCursor, 0))
			{
				platform.Clear(buffer, (uint)outLength + 1);
				platform.Free(buffer, (uint)outLength + 1);
				return false;
			}
		}
		// On placement failure the buffer is destructed by the list backbone.
		return MuiListCore.AppendOwnedString(ref platform, state, obj, buffer);
	}

	// Count word-separating single spaces (a space flanked by non-space chars).
	private static int CountGaps<TPlatform>(ref TPlatform platform, APTR scratch,
		int length) where TPlatform : struct, IMuiGuestMemory
	{
		var gaps = 0;
		for (var i = 0; i < length; i++)
			if (IsWordGap(ref platform, scratch, length, i)) gaps++;
		return gaps;
	}

	private static bool IsWordGap<TPlatform>(ref TPlatform platform, APTR scratch,
		int length, int i) where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiFloattextByteCursor);
		cursor.Base = scratch;
		cursor.Length = MaximumLineLength + 1;
		cursor.Index = (uint)i;
		if (!MuiFloattextByteCursorCodec.TryReadByte(ref platform, cursor,
			out var current) || current != (byte)' ') return false;
		if (i == 0 || i == length - 1) return false;
		cursor.Index = (uint)(i - 1);
		if (!MuiFloattextByteCursorCodec.TryReadByte(ref platform, cursor,
			out var previous)) return false;
		cursor.Index = (uint)(i + 1);
		if (!MuiFloattextByteCursorCodec.TryReadByte(ref platform, cursor,
			out var next)) return false;
		return previous != (byte)' ' && next != (byte)' ';
	}

	private static bool InSkip<TPlatform>(ref TPlatform platform, APTR skip,
		byte ch) where TPlatform : struct, IMuiGuestMemory
	{
		if (skip.IsNull) return false;
		var cursor = default(MuiFloattextByteCursor);
		cursor.Base = skip;
		cursor.Length = MaximumSkipLength;
		for (var i = 0u; i < MaximumSkipLength; i++)
		{
			cursor.Index = i;
			if (!MuiFloattextByteCursorCodec.TryReadByte(ref platform, cursor,
				out var value)) return false;
			if (value == 0) return false;
			if (value == ch) return true;
		}
		return false;
	}

	// ---- Owned-buffer helper --------------------------------------------------

	// Copy a bounded C string (including its terminator) into an owned dataspace
	// blob under the given key, replacing any previous copy.
	private static bool OwnString<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint key, APTR source, uint maximum)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!CStringCodec.TryReadLength(ref platform, source, maximum,
			out var length)) return false;
		return MuiStoreCore.DataspaceAdd(ref platform, state, obj, key, source,
			(int)(length + 1));
	}

	private static uint Read<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint fallback)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, attribute,
			out var value) ? value : fallback;

	private static bool SetInternal<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value, bool notify = false)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
			value, notify);

	private static void EnsureDefault<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, attribute,
			out _))
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
				value, false);
	}
}
