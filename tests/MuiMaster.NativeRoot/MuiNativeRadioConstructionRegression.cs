using Amiga;

namespace CopperOS.MuiMaster.NativeRoot;

// Runs the production Radio constructor with simulated native providers.
// This is not real Intuition rendering, callback, or allocator-reuse evidence.
public static class MuiNativeRadioConstructionRegression
{
	public static uint RadioConstructionRoot()
	{
		var platform = default(MuiNativeHeadlessPlatform);
		platform.Reset();
		var state = APTR.FromPointer(0x00036000);
		var root = APTR.FromPointer(0x00035F00);
		var radioName = APTR.FromPointer(0x00036100);
		var textName = APTR.FromPointer(0x00036120);
		var entries = APTR.FromPointer(0x00036200);
		if (!WriteClassName(ref platform, radioName, true) ||
			!WriteClassName(ref platform, textName, false)) return 1;
		if (!MuiMasterLifecycleCore.Create(ref platform, root, state)) return 2;
		var textClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			state, textName, APTR.Null, 8, APTR.FromPointer(1));
		var radioClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			state, radioName, APTR.Null, 8, APTR.FromPointer(1));
		if (textClass.IsNull || radioClass.IsNull) return 3;
		if (!MuiChoiceEntryVectorMemoryCodec.TryGetEntry(ref platform, entries, 0, out var first) ||
			!MuiChoiceEntryVectorMemoryCodec.TryGetEntry(ref platform, entries, 1, out var end) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, first, textName.Raw) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, end, 0)) return 4;
		var radio = MuiHeadlessObjectCore.CreateObjectA(ref platform, state, radioClass, APTR.Null);
		if (radio.IsNull || !MuiHeadlessObjectCore.SetAttribute(ref platform, state, radio,
			MuiCommonControlCore.RadioEntries, entries.Raw, false)) return 5;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, radio,
			MuiCommonControlCore.RadioActive, 5, false)) return 16;
		if (!MuiCommonControlCore.Construct(ref platform, state, radioClass, radio)) return 6;
		if (!MuiCommonControlCore.TryGetChoiceActiveStateRecord(ref platform, state,
			radio, out var active) || active.Active != 0 ||
			!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, radio,
				MuiCommonControlCore.RadioActive, out var rawActive) || rawActive != 0) return 17;
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, radio);
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner, out var complete) ||
			(complete.Flags & MuiHeadlessObjectCore.ObjectRadioConstructionComplete) == 0 ||
			(complete.Flags & (MuiHeadlessObjectCore.ObjectRadioConstructionPending |
				MuiHeadlessObjectCore.ObjectControlConstructionActive)) != 0 ||
			complete.ChildrenHead.IsNull || complete.ChildrenHead.Raw != complete.ChildrenTail.Raw)
			return 7;
		if (!MuiHeadlessChildCodec.TryRead(ref platform, complete.ChildrenHead, out var link) ||
			link.Owner.Raw != owner.Raw || link.Next.IsNotNull || link.Previous.IsNotNull ||
			!MuiHeadlessObjectCodec.TryRead(ref platform, link.Object, out var child) ||
			child.Parent.Raw != owner.Raw || child.Class.Raw != textClass.Raw) return 8;
		if (!MuiCommonControlCore.Construct(ref platform, state, radioClass, radio) ||
			!MuiHeadlessClassCodec.TryRead(ref platform, textClass, out var textState) ||
			textState.ObjectCount != 1) return 9;
		var unfinished = child;
		unfinished.Flags &= ~MuiHeadlessObjectCore.ObjectInitialized;
		if (!MuiHeadlessObjectCodec.Write(ref platform, link.Object, unfinished) ||
			MuiCommonControlCore.Construct(ref platform, state, radioClass, radio) ||
			!MuiHeadlessObjectCodec.Write(ref platform, link.Object, child)) return 10;
		var pendingChild = child;
		pendingChild.Flags |= MuiHeadlessObjectCore.ObjectRadioConstructionPending;
		if (!MuiHeadlessObjectCodec.Write(ref platform, link.Object, pendingChild) ||
			MuiCommonControlCore.Construct(ref platform, state, radioClass, radio) ||
			!MuiHeadlessObjectCodec.Write(ref platform, link.Object, child)) return 18;
		pendingChild = child;
		pendingChild.Flags |= MuiHeadlessObjectCore.ObjectScrollbarConstructionPending;
		if (!MuiHeadlessObjectCodec.Write(ref platform, link.Object, pendingChild) ||
			MuiCommonControlCore.Construct(ref platform, state, radioClass, radio) ||
			!MuiHeadlessObjectCodec.Write(ref platform, link.Object, child)) return 19;
		var missing = complete;
		missing.ChildrenHead = APTR.Null;
		missing.ChildrenTail = APTR.Null;
		if (!MuiHeadlessObjectCodec.Write(ref platform, owner, missing) ||
			MuiCommonControlCore.Construct(ref platform, state, radioClass, radio) ||
			!MuiHeadlessObjectCodec.Write(ref platform, owner, complete)) return 11;
		if (!MuiHeadlessClassCodec.TryRead(ref platform, textClass, out textState) ||
			textState.ObjectCount != 1) return 12;
		if (!MuiHeadlessObjectCore.DisposeObject(ref platform, state, radio) ||
			!MuiHeadlessStateCodec.TryRead(ref platform, state, out var registry) ||
			registry.Objects.IsNotNull) return 13;
		if (!MuiHeadlessClassCodec.TryRead(ref platform, textClass, out textState) ||
			textState.ObjectCount != 0 ||
			!MuiHeadlessClassCodec.TryRead(ref platform, radioClass, out var radioState) ||
			radioState.ObjectCount != 0) return 14;
		if (!MuiMasterLifecycleCore.Dispose(ref platform, root)) return 15;
		return 42;
	}

	private static bool WriteClassName(ref MuiNativeHeadlessPlatform platform,
		APTR address, bool radio)
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address, radio ? 10u : 9u,
			out var cursor)) return false;
		if (!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor, radio ? (byte)'R' : (byte)'T') ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor, radio ? (byte)'a' : (byte)'e') ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor, radio ? (byte)'d' : (byte)'x') ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor, radio ? (byte)'i' : (byte)'t'))
			return false;
		if (radio && !MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor, (byte)'o')) return false;
		return MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor, (byte)'.') &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor, (byte)'m') &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor, (byte)'u') &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor, (byte)'i') &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor, 0) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}
}
