/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

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

internal static class MuiFloattextPolicyFieldCursorCodec
{
	private static bool TryResolve(MuiFloattextPolicyField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiFloattextPolicyField.Magic: offset = 0; return true;
			case MuiFloattextPolicyField.Text: offset = 4; return true;
			case MuiFloattextPolicyField.SkipChars: offset = 8; return true;
			case MuiFloattextPolicyField.TabSize: offset = 12; return true;
			case MuiFloattextPolicyField.Justify: offset = 16; return true;
			case MuiFloattextPolicyField.Width: offset = 20; return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiFloattextPolicyFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiFloattextPolicyState.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiFloattextPolicyField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiFloattextPolicyFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiFloattextPolicyField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiFloattextPolicyFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiFloattextPolicyStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiFloattextPolicyState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiFloattextPolicyState.Size) ||
			!MuiFloattextPolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiFloattextPolicyField.Magic, out var magic) ||
			magic != MuiFloattextPolicyState.Cookie)
			return false;
		value.Magic = magic;
		if (!MuiFloattextPolicyFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiFloattextPolicyField.Text, out var text) ||
			!MuiFloattextPolicyFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiFloattextPolicyField.SkipChars, out var skipChars) ||
			!MuiFloattextPolicyFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiFloattextPolicyField.TabSize, out value.TabSize) ||
			!MuiFloattextPolicyFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiFloattextPolicyField.Justify, out value.Justify) ||
			!MuiFloattextPolicyFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiFloattextPolicyField.Width, out value.Width))
			return false;
		value.Text = APTR.FromPointer(text);
		value.SkipChars = APTR.FromPointer(skipChars);
		value.Justify = value.Justify == 0 ? 0u : 1u;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiFloattextPolicyState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiFloattextPolicyState.Size) || value.Magic !=
			MuiFloattextPolicyState.Cookie) return false;
		return MuiFloattextPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiFloattextPolicyField.Magic, value.Magic) &&
			MuiFloattextPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiFloattextPolicyField.Text, value.Text.Raw) &&
			MuiFloattextPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiFloattextPolicyField.SkipChars, value.SkipChars.Raw) &&
			MuiFloattextPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiFloattextPolicyField.TabSize, value.TabSize) &&
			MuiFloattextPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiFloattextPolicyField.Justify,
				value.Justify == 0 ? 0u : 1u) &&
			MuiFloattextPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiFloattextPolicyField.Width, value.Width);
	}
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
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PolicyKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj, PolicyKey) !=
			unchecked((int)MuiFloattextPolicyState.Size)) return false;
		return MuiFloattextPolicyStateCodec.TryRead(ref platform, block,
			out value);
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
		if (!TryReadPolicyState(ref platform, state, obj, out var value))
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
		if (TryReadPolicyState(ref platform, state, obj, out var policy))
		{
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
		for (var i = 0u; i < oldLength; i++)
			platform.WriteUInt8(scratch, (int)i, platform.ReadUInt8(existing, (int)i));
		for (var i = 0u; i < addLength; i++)
			platform.WriteUInt8(scratch, (int)(oldLength + i),
				platform.ReadUInt8(text, (int)i));
		platform.WriteUInt8(scratch, (int)total, 0);

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
		for (var i = 0u; i < textLength; i++)
		{
			var ch = platform.ReadUInt8(text, (int)i);
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
				for (var k = 0; k < carryLen; k++)
					platform.WriteUInt8(scratch, k,
						platform.ReadUInt8(scratch, carryStart + k));
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
			platform.WriteUInt8(scratch, lineLen, ch);
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
		if (gaps > 0)
		{
			var per = extra / gaps;
			var remainder = extra % gaps;
			var outIndex = 0;
			var gapIndex = 0;
			for (var i = 0; i < lineLength; i++)
			{
				var ch = platform.ReadUInt8(scratch, i);
				platform.WriteUInt8(buffer, outIndex++, ch);
				if (ch == (byte)' ' && IsWordGap(ref platform, scratch, lineLength, i))
				{
					var add = per + (gapIndex < remainder ? 1 : 0);
					gapIndex++;
					for (var s = 0; s < add; s++)
						platform.WriteUInt8(buffer, outIndex++, (byte)' ');
				}
			}
			platform.WriteUInt8(buffer, outIndex, 0);
		}
		else
		{
			for (var i = 0; i < lineLength; i++)
				platform.WriteUInt8(buffer, i, platform.ReadUInt8(scratch, i));
			platform.WriteUInt8(buffer, lineLength, 0);
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
		if (platform.ReadUInt8(scratch, i) != (byte)' ') return false;
		if (i == 0 || i == length - 1) return false;
		return platform.ReadUInt8(scratch, i - 1) != (byte)' ' &&
			platform.ReadUInt8(scratch, i + 1) != (byte)' ';
	}

	private static bool InSkip<TPlatform>(ref TPlatform platform, APTR skip,
		byte ch) where TPlatform : struct, IMuiGuestMemory
	{
		if (skip.IsNull) return false;
		for (var i = 0u; i < MaximumSkipLength; i++)
		{
			var value = platform.ReadUInt8(skip, (int)i);
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
