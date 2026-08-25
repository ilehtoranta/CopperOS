/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Common MorphOS help attributes.  The state is attached to the object through
// the normal guest Dataspace ownership path, which makes disposal automatic and
// keeps the public state in the named MuiHelpStateRecord.
internal static class MuiHelpStateCore
{
	internal const uint HelpLine = 0x8042A825;
	internal const uint HelpNode = 0x80420B85;
	internal const uint StateKey = 0x7F070043u;

	internal static bool IsAttribute(uint attribute) =>
		attribute == HelpNode || attribute == HelpLine;

	// MorphOS online help starts at the object under the pointer and searches
	// its named Parent chain independently for HelpNode and HelpLine.  Raw
	// attribute presence is used only to distinguish an explicitly supplied
	// zero/NULL value from an absent attribute; values themselves come from the
	// same struct-backed state used by public Get.
	internal static bool TryResolve<TPlatform>(ref TPlatform platform,
		APTR state, APTR current, out MuiHelpResolutionInput value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (current.IsNull) return true;
		var cursor = current;
		var nodeFound = false;
		var lineFound = false;
		uint visited = 0;
		while (cursor.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (MuiHeadlessObjectCore.FindObject(ref platform, state, cursor).IsNull)
				return false;
			var hasNode = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
				cursor, HelpNode, out _);
			var hasLine = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
				cursor, HelpLine, out _);
			var hasHelpState = hasNode || hasLine;
			var help = default(MuiHelpStateInput);
			if (hasHelpState && !TryReadState(ref platform, state, cursor,
				out help)) return false;
			if (!nodeFound && hasNode && help.Node.IsNotNull)
			{
				nodeFound = true;
				value.Node = help.Node;
				value.NodeObject = cursor;
			}
			if (!lineFound && hasLine)
			{
				lineFound = true;
				value.Line = help.Line;
				value.LineObject = cursor;
			}
			if (nodeFound && lineFound) return true;
			cursor = MuiHeadlessObjectCore.ParentObject(ref platform, state, cursor);
		}
		return cursor.IsNull;
	}

	internal static bool TryReadState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiHelpStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var hasRawNode = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			obj, HelpNode, out var rawNode);
		var hasRawLine = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			obj, HelpLine, out var rawLine);
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj, StateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj, StateKey) ==
			unchecked((int)MuiHelpStateRecord.Size) &&
			MuiHelpStateRecordCodec.TryRead(ref platform, block, out var record))
		{
			var node = hasRawNode ? APTR.FromPointer(rawNode) : record.Node;
			var line = hasRawLine ? rawLine : record.Line;
			if (node.Raw != record.Node.Raw || line != record.Line)
			{
				record.Node = node;
				record.Line = line;
				record.Generation = NextGeneration(record.Generation);
				if (!MuiHelpStateRecordCodec.Write(ref platform, block, record))
					return false;
			}
			value.Node = node;
			value.Line = unchecked((int)line);
			value.Generation = record.Generation;
			return true;
		}

		value.Node = hasRawNode ? APTR.FromPointer(rawNode) : APTR.Null;
		value.Line = unchecked((int)(hasRawLine ? rawLine : 0));
		value.Generation = 1;
		return WriteState(ref platform, state, obj, value.Node,
			unchecked((uint)value.Line), 1);
	}

	internal static bool Set<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!IsAttribute(attribute) ||
			!MuiHeadlessObjectCodec.TryRead(ref platform, record,
				out var objectValue)) return false;
		if (!TryReadState(ref platform, state, objectValue.Boopsi, out var current))
			return false;
		var next = current;
		if (attribute == HelpNode) next.Node = APTR.FromPointer(value);
		else next.Line = unchecked((int)value);
		if (current.Node.Raw == next.Node.Raw && current.Line == next.Line)
			return true;
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
			attribute, value, notify)) return false;
		return WriteState(ref platform, state, objectValue.Boopsi, next.Node,
			unchecked((uint)next.Line), NextGeneration(current.Generation));
	}

	internal static bool WriteState<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR node, uint line, uint generation)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiHelpStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiHelpStateRecord.Size);
		var record = default(MuiHelpStateRecord);
		record.Magic = MuiHelpStateRecord.Cookie;
		record.Node = node;
		record.Line = line;
		record.Generation = generation == 0 ? 1u : generation;
		var written = MuiHelpStateRecordCodec.Write(ref platform, scratch, record);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateKey, scratch, unchecked((int)MuiHelpStateRecord.Size));
		platform.Clear(scratch, MuiHelpStateRecord.Size);
		platform.Free(scratch, MuiHelpStateRecord.Size);
		return stored;
	}

	private static uint NextGeneration(uint generation) =>
		generation == uint.MaxValue ? 1u : generation + 1u;
}
