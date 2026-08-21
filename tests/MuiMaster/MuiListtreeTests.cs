using System.Text;
using Amiga;
using Amiga.MUI;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

// Focused MG08 coverage for the external Listtree.mcc component. Listtree is a
// standalone external core (never a built-in .mui class): these tests exercise
// external registration/identity, fixed guest tree-node records with the
// read-only MUIS_Listtree_TreeNode prefix, parent/child/sibling topology and
// bounded visible traversal, the construct/destruct/display/sort/open/close
// hooks, the documented methods and selectors, active/quiet/notification/redraw
// behaviour, duplicate-name policy, failure rollback, deep/wide trees and
// allocation failures, and failure-atomic recursive disposal.
public sealed class MuiListtreeTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	// Class flag bits (HeadlessObjectCore): External == 2, Builtin == 8.
	private const uint ClassExternalBit = 2;
	private const uint ClassBuiltinBit = 8;
	private const int ClassFlagsOffset = 20;

	private const uint TNF_OPEN = MuiListtreeCore.TNF_OPEN;
	private const uint TNF_LIST = MuiListtreeCore.TNF_LIST;

	// Selectors used across the tests.
	private const uint ListRoot = 0;              // MUIV_*_ListNode_Root
	private const uint PrevHead = 0;              // MUIV_Listtree_Insert_PrevNode_Head
	private const uint PrevTail = 0xFFFFFFFFu;    // _Tail (-1)
	private const uint PrevSorted = 0xFFFFFFFCu;  // _Sorted (-4)
	private const uint TreeHead = 0;
	private const uint TreeTail = 0xFFFFFFFFu;
	private const uint TreeActive = 0xFFFFFFFEu;  // -2
	private const uint TreeAll = 0xFFFFFFFDu;     // -3
	private const uint ConstructHookString = 0xFFFFFFFFu;

	private const uint InsertFlagsActive = 1u << 13;
	private const uint GetNrCountAll = 1u << 15;
	private const uint GetNrCountLevel = 1u << 14;
	private const uint GetNrCountList = 1u << 13;
	private const uint GetNrListEmpty = 1u << 12;
	private const uint GetEntrySameLevel = 1u << 15;
	private const uint RenameFlagsUser = 1u << 8;
	private const uint RenameFlagsNoRefresh = 1u << 9;

	private const int PositionParent = -5;
	private const int PositionNext = -3;
	private const int PositionPrevious = -4;

	[Fact]
	public void ListtreeHeaderCodecUsesNamedGuestFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2400);
		var expected = default(MuiListtreeCore.MuiListtreeHeaderState);
		expected.Magic = MuiListtreeCore.MuiListtreeHeaderState.Cookie;
		expected.RootFirst = APTR.FromPointer(0x2800);
		expected.RootLast = APTR.FromPointer(0x2840);
		expected.RootCount = 2;
		expected.Total = 5;
		expected.Redraw = 7;
		expected.Dirty = 1;
		expected.DropEntry = -3;
		expected.DropValue = 4;
		expected.Reserved0 = 0x10;
		expected.Reserved1 = 0x20;
		expected.Reserved2 = 0x30;

		Assert.True(MuiListtreeCore.MuiListtreeHeaderCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiListtreeCore.MuiListtreeHeaderCodec.TryRead(ref platform,
			address,
			out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.RootFirst, actual.RootFirst);
		Assert.Equal(expected.RootLast, actual.RootLast);
		Assert.Equal(expected.RootCount, actual.RootCount);
		Assert.Equal(expected.Total, actual.Total);
		Assert.Equal(expected.Redraw, actual.Redraw);
		Assert.Equal(expected.Dirty, actual.Dirty);
		Assert.Equal(expected.DropEntry, actual.DropEntry);
		Assert.Equal(expected.DropValue, actual.DropValue);
		Assert.Equal(expected.Reserved0, actual.Reserved0);
		Assert.Equal(expected.Reserved1, actual.Reserved1);
		Assert.Equal(expected.Reserved2, actual.Reserved2);
		Assert.False(MuiListtreeCore.MuiListtreeHeaderCodec.TryRead(ref platform,
			APTR.Null,
			out _));
	}

	[Fact]
	public void ListtreeHeaderFieldCursorUsesNamedRecordBoundary()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2480);
		Assert.True(MuiListtreeCore.MuiListtreeHeaderFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeHeaderField.Magic,
				MuiListtreeCore.MuiListtreeHeaderState.Cookie));
		Assert.True(MuiListtreeCore.MuiListtreeHeaderFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeHeaderField.RootFirst, 0x2800u));
		Assert.True(MuiListtreeCore.MuiListtreeHeaderFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeHeaderField.DropEntry,
			unchecked((uint)-3)));
		Assert.True(MuiListtreeCore.MuiListtreeHeaderFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeHeaderField.Reserved2, 0x30u));
		Assert.True(MuiListtreeCore.MuiListtreeHeaderFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeHeaderField.DropEntry,
				out var dropEntry));
		Assert.Equal(unchecked((uint)-3), dropEntry);
		Assert.True(MuiListtreeCore.MuiListtreeHeaderFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeHeaderField.Reserved2,
				out var reserved));
		Assert.Equal(0x30u, reserved);
		Assert.False(MuiListtreeCore.MuiListtreeHeaderFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				unchecked((MuiListtreeCore.MuiListtreeHeaderField)255),
				out _));
		Assert.False(MuiListtreeCore.MuiListtreeHeaderFieldCursorCodec
			.TryReadUInt32(ref platform, APTR.FromPointer(0xFFFFFFF0u),
				MuiListtreeCore.MuiListtreeHeaderField.RootCount, out _));
	}

	[Fact]
	public void ListtreePolicyUsesNamedGuestRecord()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		Assert.True(MuiListtreeCore.TryGetPolicyStateRecord(ref platform, State,
			tree, out var initial));
		Assert.Equal(MuiListtreeCore.MuiListtreePolicyStateRecord.Cookie,
			initial.Magic);
		Assert.Equal(0u, initial.Active.Raw);
		Assert.Equal(1u, initial.DuplicateNodeName);
		Assert.Equal(0u, initial.Quiet);
		Assert.Equal(1u, initial.DragDropSort);
		Assert.Equal(0xFFFFFFFFu, initial.DoubleClick);

		var node = InsertName(ref platform, tree, "active", ListRoot, PrevTail);
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.DuplicateNodeName, 0, false));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Quiet, 1, false));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.SortHook, 0x1234u, false));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Active, node.Raw, false));

		Assert.True(MuiListtreeCore.TryGetPolicyStateRecord(ref platform, State,
			tree, out var actual));
		Assert.Equal(node.Raw, actual.Active.Raw);
		Assert.Equal(0u, actual.DuplicateNodeName);
		Assert.Equal(1u, actual.Quiet);
		Assert.Equal(0x1234u, actual.SortHook.Raw);
		Assert.True(MuiListtreeCore.GetAttribute(ref platform, State, tree,
			MuiListtreeCore.SortHook, out var sortHook));
		Assert.Equal(0x1234u, sortHook);
	}

	[Fact]
	public void ListtreePolicyGettersPreferNamedRecordAndCustomGetUsesStorage()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.DuplicateNodeName, 0, false));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Quiet, 1, false));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.SortHook, 0x1234u, false));
		Assert.True(MuiListtreeCore.TryGetPolicyStateRecord(ref platform, State,
			tree, out var policy));

		// A raw compatibility write cannot replace the canonical policy record.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.DuplicateNodeName, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Quiet, 0, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.SortHook, 0x5678u, false));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, tree,
			MuiListtreeCore.DuplicateNodeName, out var duplicate));
		Assert.Equal(policy.DuplicateNodeName, duplicate);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, tree,
			MuiListtreeCore.Quiet, out var quiet));
		Assert.Equal(policy.Quiet, quiet);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, tree,
			MuiListtreeCore.SortHook, out var sortHook));
		Assert.Equal(policy.SortHook.Raw, sortHook);

		var message = APTR.FromPointer(0x7B00);
		var storage = APTR.FromPointer(0x7C00);
		Assert.True(MuiListtreeMessageCodec.WriteGet(ref platform, message,
			MuiListtreeCore.SortHook, storage.Raw));
		Assert.Equal(1u, MuiListtreeDispatcher.Dispatch(ref platform, State, tree,
			message));
		Assert.True(MuiGuestUlongStorageCodec.TryRead(ref platform, storage,
			out var result));
		Assert.Equal(policy.SortHook.Raw, result.Value);
	}

	[Fact]
	public void ListtreeTestPosResultUsesNamedMixedWidthFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2C00);
		var expected = new MuiListtreeCore.MuiListtreeTestPosResult
		{
			TreeNode = APTR.FromPointer(0x3020),
			Flags = 2,
			ListEntry = -1,
			ListFlags = 0,
		};

		Assert.True(MuiListtreeCore.MuiListtreeTestPosResultCodec.Write(
			ref platform, address, expected));
		Assert.True(MuiListtreeCore.MuiListtreeTestPosResultCodec.TryRead(
			ref platform, address, out var actual));
		Assert.Equal(expected.TreeNode, actual.TreeNode);
		Assert.Equal(expected.Flags, actual.Flags);
		Assert.Equal(expected.ListEntry, actual.ListEntry);
		Assert.Equal(expected.ListFlags, actual.ListFlags);
		Assert.False(MuiListtreeCore.MuiListtreeTestPosResultCodec.TryRead(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void ListtreeTestPosFieldCursorUsesNamedMixedRecordBoundary()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2D00);
		Assert.True(MuiListtreeCore.MuiListtreeTestPosFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeTestPosField.TreeNode, 0x3020u));
		Assert.True(MuiListtreeCore.MuiListtreeTestPosFieldCursorCodec
			.TryWriteUInt16(ref platform, address,
				MuiListtreeCore.MuiListtreeTestPosField.Flags, 2));
		Assert.True(MuiListtreeCore.MuiListtreeTestPosFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeTestPosField.ListEntry,
				unchecked((uint)-1)));
		Assert.True(MuiListtreeCore.MuiListtreeTestPosFieldCursorCodec
			.TryWriteUInt16(ref platform, address,
				MuiListtreeCore.MuiListtreeTestPosField.ListFlags, 4));
		Assert.True(MuiListtreeCore.MuiListtreeTestPosFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeTestPosField.ListEntry,
				out var entry));
		Assert.Equal(unchecked((uint)-1), entry);
		Assert.True(MuiListtreeCore.MuiListtreeTestPosFieldCursorCodec
			.TryReadUInt16(ref platform, address,
				MuiListtreeCore.MuiListtreeTestPosField.ListFlags,
				out var listFlags));
		Assert.Equal((ushort)4, listFlags);
		Assert.False(MuiListtreeCore.MuiListtreeTestPosFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeTestPosField.Flags, out _));
	}

	[Fact]
	public void ListtreeNodePublicCodecUsesTypedPrefixFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2500);
		var expected = default(MuiListtreeCore.MuiListtreeNodePublicState);
		expected.Private1 =
			MuiListtreeCore.MuiListtreeNodePublicState.Cookie;
		expected.Private2 = APTR.FromPointer(0x2800);
		expected.Name = APTR.FromPointer(0x2900);
		expected.Flags = (ushort)(MuiListtreeCore.TNF_OPEN |
			MuiListtreeCore.TNF_LIST);
		expected.User = APTR.FromPointer(0x2A00);

		Assert.True(MuiListtreeCore.MuiListtreeNodePublicCodec.Write(
			ref platform, address, expected));
		Assert.True(MuiListtreeCore.MuiListtreeNodePublicCodec.TryRead(
			ref platform, address, out var actual));
		Assert.Equal(expected.Private1, actual.Private1);
		Assert.Equal(expected.Private2, actual.Private2);
		Assert.Equal(expected.Name, actual.Name);
		Assert.Equal(expected.Flags, actual.Flags);
		Assert.Equal(expected.User, actual.User);
		Assert.False(MuiListtreeCore.MuiListtreeNodePublicCodec.TryRead(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void ListtreeNodeCodecUsesNamedTopologyFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2600);
		var expected = default(MuiListtreeCore.MuiListtreeNodeState);
		expected.Private1 = MuiListtreeCore.MuiListtreeNodeState.Cookie;
		expected.Private2 = APTR.FromPointer(0x2800);
		expected.Name = APTR.FromPointer(0x2900);
		expected.Flags = (ushort)MuiListtreeCore.TNF_OPEN;
		expected.User = APTR.FromPointer(0x2A00);
		expected.Parent = APTR.FromPointer(0x2B00);
		expected.FirstChild = APTR.FromPointer(0x2B40);
		expected.LastChild = APTR.FromPointer(0x2B80);
		expected.Next = APTR.FromPointer(0x2BC0);
		expected.Previous = APTR.FromPointer(0x2C00);
		expected.ChildCount = 3;
		expected.NameOwned = 1;
		expected.NameSize = 12;
		expected.UserOwned = 1;
		expected.Reserved0 = 0x1234;
		expected.Reserved1 = 0x5678;

		Assert.True(MuiListtreeCore.MuiListtreeNodeCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiListtreeCore.MuiListtreeNodeCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.Private2, actual.Private2);
		Assert.Equal(expected.Name, actual.Name);
		Assert.Equal(expected.Flags, actual.Flags);
		Assert.Equal(expected.User, actual.User);
		Assert.Equal(expected.Parent, actual.Parent);
		Assert.Equal(expected.FirstChild, actual.FirstChild);
		Assert.Equal(expected.LastChild, actual.LastChild);
		Assert.Equal(expected.Next, actual.Next);
		Assert.Equal(expected.Previous, actual.Previous);
		Assert.Equal(expected.ChildCount, actual.ChildCount);
		Assert.Equal(expected.NameOwned, actual.NameOwned);
		Assert.Equal(expected.NameSize, actual.NameSize);
		Assert.Equal(expected.UserOwned, actual.UserOwned);
		Assert.Equal(expected.Reserved0, actual.Reserved0);
		Assert.Equal(expected.Reserved1, actual.Reserved1);
	}

	[Fact]
	public void ListtreeNodeFieldCursorUsesNamedMixedRecordBoundary()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2700);
		Assert.True(MuiListtreeCore.MuiListtreeNodeFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeNodeField.Private1,
				MuiListtreeCore.MuiListtreeNodeState.Cookie));
		Assert.True(MuiListtreeCore.MuiListtreeNodeFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeNodeField.Name, 0x2900u));
		Assert.True(MuiListtreeCore.MuiListtreeNodeFieldCursorCodec
			.TryWriteUInt16(ref platform, address,
				MuiListtreeCore.MuiListtreeNodeField.Flags, 0x1234));
		Assert.True(MuiListtreeCore.MuiListtreeNodeFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeNodeField.UserOwned, 1u));
		Assert.True(MuiListtreeCore.MuiListtreeNodeFieldCursorCodec
			.TryReadUInt16(ref platform, address,
				MuiListtreeCore.MuiListtreeNodeField.Flags, out var flags));
		Assert.Equal((ushort)0x1234, flags);
		Assert.True(MuiListtreeCore.MuiListtreeNodeFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeNodeField.UserOwned, out var owned));
		Assert.Equal(1u, owned);
		Assert.False(MuiListtreeCore.MuiListtreeNodeFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeNodeField.Flags, out _));
		Assert.False(MuiListtreeCore.MuiListtreeNodeFieldCursorCodec
			.TryReadUInt16(ref platform, APTR.FromPointer(0xFFFFFFF0u),
				MuiListtreeCore.MuiListtreeNodeField.Flags, out _));
	}

	// =====================================================================
	// External identity / registration
	// =====================================================================

	[Fact]
	public void ListtreeRegistersAsExternalNeverBuiltin()
	{
		var platform = CreatePlatform(out var listtreeClass);
		// Flagged external, not builtin.
		var flags = platform.ReadUInt32(listtreeClass, ClassFlagsOffset);
		Assert.Equal(ClassExternalBit, flags & ClassExternalBit);
		Assert.Equal(0u, flags & ClassBuiltinBit);
		Assert.True(MuiListtreeCore.ClassRecordIsListtree(ref platform,
			listtreeClass));
		// The built-in .mui collection classifier does not recognise it.
		Assert.Equal(MuiCollectionClass.Unknown, MuiListCore.ClassifyRecord(
			ref platform, listtreeClass));
	}

	[Fact]
	public void NonListtreeNameIsRejected()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x80000, 0x8000, State);
		MuiHeadlessObjectCore.Initialize(ref platform, State);
		var wrong = APTR.FromPointer(0x1100);
		platform.WriteCString(wrong, "Listtree.mui"); // .mui, not .mcc
		var boopsi = APTR.FromPointer(0x1200);
		var record = MuiListtreeCore.RegisterListtreeExternalClass(ref platform,
			State, wrong, boopsi, APTR.Null);
		Assert.Equal(APTR.Null, record);
	}

	// =====================================================================
	// Insert / topology / node record prefix
	// =====================================================================

	[Fact]
	public void InsertBuildsParentChildSiblingTopology()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var root = InsertName(ref platform, tree, "root", ListRoot, PrevTail);
		var childA = InsertName(ref platform, tree, "a",
			(uint)root.Raw, PrevTail);
		var childB = InsertName(ref platform, tree, "b",
			(uint)root.Raw, PrevTail);

		Assert.Equal(1u, MuiListtreeCore.RootCount(ref platform, State, tree));
		Assert.Equal(3u, MuiListtreeCore.TotalNodes(ref platform, State, tree));
		Assert.Equal(2u, MuiListtreeCore.ChildCount(ref platform, root));
		// Parent gained TNF_LIST; children are leaves.
		Assert.Equal(TNF_LIST, MuiListtreeCore.NodeFlags(ref platform, root)
			& TNF_LIST);
		Assert.Equal(0u, MuiListtreeCore.NodeFlags(ref platform, childA)
			& TNF_LIST);
		// Sibling order: a before b.
		Assert.Equal("a", NodeName(ref platform, GetEntry(ref platform, tree,
			(uint)root.Raw, 0, 0)));
		Assert.Equal("b", NodeName(ref platform, GetEntry(ref platform, tree,
			(uint)root.Raw, 1, 0)));
		// Parent navigation.
		Assert.Equal(root.Raw, GetEntry(ref platform, tree, (uint)childB.Raw,
			PositionParent, 0).Raw);
	}

	[Fact]
	public void NodeRecordExposesReadOnlyTreeNodePrefix()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var user = APTR.FromPointer(0x2000);
		var node = MuiListtreeCore.Insert(ref platform, State, tree,
			WriteString(ref platform, 0x2100, "leaf"), user,
			APTR.FromPointer(ListRoot), APTR.FromPointer(PrevTail), 0);
		Assert.NotEqual(APTR.Null, node);
		// tn_Name (offset 8) is an owned copy holding "leaf".
		var name = APTR.FromPointer(platform.ReadUInt32(node,
			MuiListtreeCore.TreeNodeNameOffset));
		Assert.Equal("leaf", ReadCString(ref platform, name));
		// tn_User (offset 14) is the user pointer verbatim (no hook).
		Assert.Equal(user.Raw, platform.ReadUInt32(node,
			MuiListtreeCore.TreeNodeUserOffset));
		// tn_Flags (offset 12) is a UWORD.
		Assert.Equal(0u, MuiListtreeCore.NodeFlags(ref platform, node));
	}

	[Fact]
	public void DuplicateNodeNamePolicyControlsNameBuffering()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		// Default TRUE: the name is copied into an owned buffer.
		var src = WriteString(ref platform, 0x2200, "copied");
		var owned = MuiListtreeCore.Insert(ref platform, State, tree, src,
			APTR.Null, APTR.FromPointer(ListRoot), APTR.FromPointer(PrevTail), 0);
		Assert.NotEqual(src.Raw, platform.ReadUInt32(owned,
			MuiListtreeCore.TreeNodeNameOffset));

		// FALSE: only the pointer is used.
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.DuplicateNodeName, 0, false));
		var borrowed = MuiListtreeCore.Insert(ref platform, State, tree, src,
			APTR.Null, APTR.FromPointer(ListRoot), APTR.FromPointer(PrevTail), 0);
		Assert.Equal(src.Raw, platform.ReadUInt32(borrowed,
			MuiListtreeCore.TreeNodeNameOffset));
	}

	// =====================================================================
	// Construct / destruct hooks
	// =====================================================================

	[Fact]
	public void ConstructHookStringDuplicatesUserData()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.ConstructHook, ConstructHookString, false));
		var userText = WriteString(ref platform, 0x2300, "payload");
		var node = MuiListtreeCore.Insert(ref platform, State, tree,
			WriteString(ref platform, 0x2380, "n"), userText,
			APTR.FromPointer(ListRoot), APTR.FromPointer(PrevTail), 0);
		Assert.NotEqual(APTR.Null, node);
		var stored = APTR.FromPointer(platform.ReadUInt32(node,
			MuiListtreeCore.TreeNodeUserOffset));
		Assert.NotEqual(userText.Raw, stored.Raw);          // owned copy
		Assert.Equal("payload", ReadCString(ref platform, stored));
	}

	[Fact]
	public void ConstructHookNullUserAddsNothing()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		// ConstructHook_String with a NULL user returns NULL -> nothing added.
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.ConstructHook, ConstructHookString, false));
		var node = MuiListtreeCore.Insert(ref platform, State, tree,
			WriteString(ref platform, 0x2400, "n"), APTR.Null,
			APTR.FromPointer(ListRoot), APTR.FromPointer(PrevTail), 0);
		Assert.Equal(APTR.Null, node);
		Assert.Equal(0u, MuiListtreeCore.RootCount(ref platform, State, tree));
		Assert.Equal(0u, MuiListtreeCore.TotalNodes(ref platform, State, tree));
	}

	// =====================================================================
	// GetEntry / GetNr
	// =====================================================================

	[Fact]
	public void GetEntryResolvesHeadTailNextPreviousAndIndex()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var a = InsertName(ref platform, tree, "a", ListRoot, PrevTail);
		var b = InsertName(ref platform, tree, "b", ListRoot, PrevTail);
		var c = InsertName(ref platform, tree, "c", ListRoot, PrevTail);
		Assert.Equal(a.Raw, GetEntry(ref platform, tree, ListRoot, 0, 0).Raw);
		Assert.Equal(c.Raw, GetEntry(ref platform, tree, ListRoot, -1, 0).Raw);
		Assert.Equal(b.Raw, GetEntry(ref platform, tree, (uint)a.Raw,
			PositionNext, GetEntrySameLevel).Raw);
		Assert.Equal(b.Raw, GetEntry(ref platform, tree, (uint)c.Raw,
			PositionPrevious, GetEntrySameLevel).Raw);
		Assert.Equal(c.Raw, GetEntry(ref platform, tree, ListRoot, 2, 0).Raw);
		Assert.Equal(APTR.Null, GetEntry(ref platform, tree, ListRoot, 9, 0));
	}

	[Fact]
	public void GetNrReportsCounts()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var root = InsertName(ref platform, tree, "root", ListRoot, PrevTail);
		InsertName(ref platform, tree, "x", (uint)root.Raw, PrevTail);
		InsertName(ref platform, tree, "y", (uint)root.Raw, PrevTail);
		var leaf = InsertName(ref platform, tree, "leaf", ListRoot, PrevTail);
		// CountAll == every node.
		Assert.Equal(4u, MuiListtreeCore.GetNr(ref platform, State, tree,
			APTR.FromPointer(root.Raw), GetNrCountAll));
		// CountList == children of the node.
		Assert.Equal(2u, MuiListtreeCore.GetNr(ref platform, State, tree,
			APTR.FromPointer(root.Raw), GetNrCountList));
		// CountLevel == entries in the node's own list (root level == 2).
		Assert.Equal(2u, MuiListtreeCore.GetNr(ref platform, State, tree,
			APTR.FromPointer(root.Raw), GetNrCountLevel));
		// ListEmpty for a leaf.
		Assert.Equal(1u, MuiListtreeCore.GetNr(ref platform, State, tree,
			APTR.FromPointer(leaf.Raw), GetNrListEmpty));
		Assert.Equal(0u, MuiListtreeCore.GetNr(ref platform, State, tree,
			APTR.FromPointer(root.Raw), GetNrListEmpty));
	}

	// =====================================================================
	// Open / Close + visible traversal + active
	// =====================================================================

	[Fact]
	public void OpenCloseControlsVisibleTraversal()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var root = InsertName(ref platform, tree, "root", ListRoot, PrevTail);
		var childA = InsertName(ref platform, tree, "a", (uint)root.Raw, PrevTail);
		InsertName(ref platform, tree, "b", (uint)root.Raw, PrevTail);
		// Closed by default: only the root node is visible.
		Assert.Equal(1u, MuiListtreeCore.VisibleCount(ref platform, State, tree));
		// Open the node: its two children join the display list.
		Assert.True(MuiListtreeCore.Open(ref platform, State, tree,
			APTR.FromPointer(ListRoot), APTR.FromPointer(root.Raw), 0));
		Assert.Equal(TNF_OPEN, MuiListtreeCore.NodeFlags(ref platform, root)
			& TNF_OPEN);
		Assert.Equal(3u, MuiListtreeCore.VisibleCount(ref platform, State, tree));
		// Make a child active, then close the node: the closed node becomes active.
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Active, childA.Raw, false));
		Assert.True(MuiListtreeCore.Close(ref platform, State, tree,
			APTR.FromPointer(ListRoot), APTR.FromPointer(root.Raw), 0));
		Assert.Equal(root.Raw, MuiListtreeCore.ActiveNode(ref platform, State,
			tree).Raw);
		Assert.Equal(1u, MuiListtreeCore.VisibleCount(ref platform, State, tree));
	}

	// =====================================================================
	// Sort
	// =====================================================================

	[Fact]
	public void SortLeavesBottomOrdersNodesBeforeLeavesAlphabetically()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		// Default sort hook is LeavesBottom (nodes first, then leaves; each
		// group alphabetical). Build: leaf "z", leaf "a", node "m" (has a child).
		InsertName(ref platform, tree, "z", ListRoot, PrevTail);
		InsertName(ref platform, tree, "a", ListRoot, PrevTail);
		var m = InsertName(ref platform, tree, "m", ListRoot, PrevTail);
		InsertName(ref platform, tree, "child", (uint)m.Raw, PrevTail); // m -> node
		Assert.True(MuiListtreeCore.Sort(ref platform, State, tree,
			APTR.FromPointer(ListRoot), 0));
		Assert.Equal("m", NodeName(ref platform, GetEntry(ref platform, tree,
			ListRoot, 0, 0)));  // node first
		Assert.Equal("a", NodeName(ref platform, GetEntry(ref platform, tree,
			ListRoot, 1, 0)));  // leaves alphabetical
		Assert.Equal("z", NodeName(ref platform, GetEntry(ref platform, tree,
			ListRoot, 2, 0)));
	}

	[Fact]
	public void InsertSortedUsesSortHook()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		// All leaves -> LeavesBottom degenerates to alphabetical.
		InsertName(ref platform, tree, "d", ListRoot, PrevSorted);
		InsertName(ref platform, tree, "b", ListRoot, PrevSorted);
		InsertName(ref platform, tree, "c", ListRoot, PrevSorted);
		InsertName(ref platform, tree, "a", ListRoot, PrevSorted);
		Assert.Equal("a", NodeName(ref platform, GetEntry(ref platform, tree,
			ListRoot, 0, 0)));
		Assert.Equal("d", NodeName(ref platform, GetEntry(ref platform, tree,
			ListRoot, 3, 0)));
	}

	// =====================================================================
	// Move / Exchange
	// =====================================================================

	[Fact]
	public void MoveReparentsNodeAndRejectsCycles()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var p1 = InsertName(ref platform, tree, "p1", ListRoot, PrevTail);
		var p2 = InsertName(ref platform, tree, "p2", ListRoot, PrevTail);
		var leaf = InsertName(ref platform, tree, "leaf", (uint)p1.Raw, PrevTail);
		Assert.Equal(1u, MuiListtreeCore.ChildCount(ref platform, p1));
		// Move leaf from p1 to p2 (tail).
		Assert.True(MuiListtreeCore.Move(ref platform, State, tree,
			APTR.FromPointer(p1.Raw), APTR.FromPointer(leaf.Raw),
			APTR.FromPointer(p2.Raw), APTR.FromPointer(PrevTail), 0));
		Assert.Equal(0u, MuiListtreeCore.ChildCount(ref platform, p1));
		Assert.Equal(1u, MuiListtreeCore.ChildCount(ref platform, p2));
		Assert.Equal(p2.Raw, GetEntry(ref platform, tree, (uint)leaf.Raw,
			PositionParent, 0).Raw);
		// A node may not be moved into its own subtree.
		Assert.False(MuiListtreeCore.Move(ref platform, State, tree,
			APTR.FromPointer(ListRoot), APTR.FromPointer(p2.Raw),
			APTR.FromPointer(leaf.Raw), APTR.FromPointer(PrevTail), 0));
	}

	[Fact]
	public void ExchangeSwapsSiblingsAndRejectsAncestors()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var a = InsertName(ref platform, tree, "a", ListRoot, PrevTail);
		var b = InsertName(ref platform, tree, "b", ListRoot, PrevTail);
		var c = InsertName(ref platform, tree, "c", ListRoot, PrevTail);
		Assert.True(MuiListtreeCore.Exchange(ref platform, State, tree,
			APTR.FromPointer(ListRoot), APTR.FromPointer(a.Raw),
			APTR.FromPointer(ListRoot), APTR.FromPointer(c.Raw), 0));
		Assert.Equal("c", NodeName(ref platform, GetEntry(ref platform, tree,
			ListRoot, 0, 0)));
		Assert.Equal("b", NodeName(ref platform, GetEntry(ref platform, tree,
			ListRoot, 1, 0)));
		Assert.Equal("a", NodeName(ref platform, GetEntry(ref platform, tree,
			ListRoot, 2, 0)));
		// Ancestor/descendant pairs cannot be exchanged.
		var child = InsertName(ref platform, tree, "child", (uint)b.Raw, PrevTail);
		Assert.False(MuiListtreeCore.Exchange(ref platform, State, tree,
			APTR.FromPointer(ListRoot), APTR.FromPointer(b.Raw),
			APTR.FromPointer(ListRoot), APTR.FromPointer(child.Raw), 0));
	}

	// =====================================================================
	// Rename
	// =====================================================================

	[Fact]
	public void RenameChangesNameAndUser()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var node = InsertName(ref platform, tree, "old", ListRoot, PrevTail);
		Assert.True(MuiListtreeCore.Rename(ref platform, State, tree,
			APTR.FromPointer(node.Raw), WriteString(ref platform, 0x2500, "new"),
			RenameFlagsNoRefresh));
		Assert.Equal("new", NodeName(ref platform, node));
		// Rename the user field (no hook -> pointer copied).
		var user = APTR.FromPointer(0x2600);
		Assert.True(MuiListtreeCore.Rename(ref platform, State, tree,
			APTR.FromPointer(node.Raw), user, RenameFlagsUser));
		Assert.Equal(user.Raw, platform.ReadUInt32(node,
			MuiListtreeCore.TreeNodeUserOffset));
	}

	// =====================================================================
	// FindName
	// =====================================================================

	[Fact]
	public void FindNameSearchesSameLevelOrRecursively()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var root = InsertName(ref platform, tree, "root", ListRoot, PrevTail);
		var deep = InsertName(ref platform, tree, "target", (uint)root.Raw,
			PrevTail);
		var target = WriteString(ref platform, 0x2700, "target");
		// SameLevel search of the root list does not descend: not found.
		Assert.Equal(APTR.Null, MuiListtreeCore.FindName(ref platform, State, tree,
			APTR.FromPointer(ListRoot), target, GetEntrySameLevel));
		// Recursive search finds it.
		Assert.Equal(deep.Raw, MuiListtreeCore.FindName(ref platform, State, tree,
			APTR.FromPointer(ListRoot), target, 0).Raw);
	}

	// =====================================================================
	// Remove (recursive) + active follow
	// =====================================================================

	[Fact]
	public void RemoveIsRecursiveAndMovesActive()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var a = InsertName(ref platform, tree, "a", ListRoot, PrevTail);
		var b = InsertName(ref platform, tree, "b", ListRoot, PrevTail);
		InsertName(ref platform, tree, "b1", (uint)b.Raw, PrevTail);
		InsertName(ref platform, tree, "b2", (uint)b.Raw, PrevTail);
		var c = InsertName(ref platform, tree, "c", ListRoot, PrevTail);
		Assert.Equal(5u, MuiListtreeCore.TotalNodes(ref platform, State, tree));
		// Make b active, then remove b: its subtree goes, active follows to c.
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Active, b.Raw, false));
		Assert.True(MuiListtreeCore.Remove(ref platform, State, tree,
			APTR.FromPointer(ListRoot), APTR.FromPointer(b.Raw), 0));
		Assert.Equal(2u, MuiListtreeCore.TotalNodes(ref platform, State, tree));
		Assert.Equal(2u, MuiListtreeCore.RootCount(ref platform, State, tree));
		Assert.Equal(c.Raw, MuiListtreeCore.ActiveNode(ref platform, State,
			tree).Raw);
		Assert.Equal(a.Raw, GetEntry(ref platform, tree, ListRoot, 0, 0).Raw);
	}

	[Fact]
	public void RemoveAllClearsAList()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		InsertName(ref platform, tree, "a", ListRoot, PrevTail);
		InsertName(ref platform, tree, "b", ListRoot, PrevTail);
		InsertName(ref platform, tree, "c", ListRoot, PrevTail);
		Assert.True(MuiListtreeCore.Remove(ref platform, State, tree,
			APTR.FromPointer(ListRoot), APTR.FromPointer(TreeAll), 0));
		Assert.Equal(0u, MuiListtreeCore.RootCount(ref platform, State, tree));
		Assert.Equal(0u, MuiListtreeCore.TotalNodes(ref platform, State, tree));
	}

	// =====================================================================
	// Quiet / redraw / notification
	// =====================================================================

	[Fact]
	public void QuietCoalescesRedraw()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		InsertName(ref platform, tree, "a", ListRoot, PrevTail);
		var baseline = MuiListtreeCore.RedrawRequests(ref platform, State, tree);
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Quiet, 1, false));
		// Mutations while quiet do not bump the redraw counter.
		InsertName(ref platform, tree, "b", ListRoot, PrevTail);
		InsertName(ref platform, tree, "c", ListRoot, PrevTail);
		Assert.Equal(baseline, MuiListtreeCore.RedrawRequests(ref platform, State,
			tree));
		// Turning quiet off flushes exactly one coalesced refresh.
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Quiet, 0, false));
		Assert.Equal(baseline + 1, MuiListtreeCore.RedrawRequests(ref platform,
			State, tree));
	}

	[Fact]
	public void ActiveChangeNotifiesUnlessQuietlySet()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var a = InsertName(ref platform, tree, "a", ListRoot, PrevTail);
		var b = InsertName(ref platform, tree, "b", ListRoot, PrevTail);
		// A notification that fires on any Active change, delivered to the object.
		var follow = APTR.FromPointer(0x2800);
		platform.WriteUInt32(follow, 0, 0);
		Assert.True(MuiNotifyCore.Add(ref platform, State, tree,
			MuiListtreeCore.Active, (uint)Value.EveryTime, tree, 1, follow));
		var before = platform.DispatchCount;
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Active, a.Raw, true));
		Assert.True(platform.DispatchCount > before);
		// A no-notify set does not fire the notification.
		var mid = platform.DispatchCount;
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Active, b.Raw, false));
		Assert.Equal(mid, platform.DispatchCount);
	}

	// =====================================================================
	// Dispatcher routing
	// =====================================================================

	[Fact]
	public void ListtreePacketCodecUsesNamedRecordsAndRejectsMalformedPackets()
	{
		var p = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x2D00);
		Assert.True(MuiListtreeMessageCodec.WriteSet(ref p, packet,
			MuiListtreeMessageCodec.Set, 0x80420001u, 7));
		Assert.True(MuiListtreeMessageCodec.TryReadSet(ref p, packet,
			MuiListtreeMessageCodec.Set, out var set));
		Assert.Equal(0x80420001u, set.Attribute);
		Assert.Equal(7u, set.Value);

		Assert.True(MuiListtreeMessageCodec.WriteGet(ref p, packet, 9, 0x2D80));
		Assert.True(MuiListtreeMessageCodec.TryReadGet(ref p, packet,
			out var get));
		Assert.Equal(9u, get.Attribute);
		Assert.Equal(0x2D80u, get.Storage);

		Assert.True(MuiListtreeMessageCodec.WriteInsert(ref p, packet,
			0x3000, 0x3010, 0x3020, 0x3030, 4));
		Assert.True(MuiListtreeMessageCodec.TryReadInsert(ref p, packet,
			out var insert));
		Assert.Equal(0x3000u, insert.Name);
		Assert.Equal(0x3010u, insert.UserData);
		Assert.Equal(4u, insert.Flags);

		Assert.True(MuiListtreeMessageCodec.WriteRemove(ref p, packet,
			0x3020, 0x3040, 5));
		Assert.True(MuiListtreeMessageCodec.TryReadRemove(ref p, packet,
			out var remove));
		Assert.Equal(0x3020u, remove.Parent);
		Assert.Equal(0x3040u, remove.Node);

		Assert.True(MuiListtreeMessageCodec.WriteGetEntry(ref p, packet,
			0x3020, unchecked((uint)-2), 6));
		Assert.True(MuiListtreeMessageCodec.TryReadGetEntry(ref p, packet,
			out var entry));
		Assert.Equal(unchecked((uint)-2), entry.Position);
		Assert.Equal(6u, entry.Flags);

		Assert.True(MuiListtreeMessageCodec.WriteOpenClose(ref p, packet,
			MuiListtreeMessageCodec.Open, 0x3020, 0x3040, 1));
		Assert.True(MuiListtreeMessageCodec.TryReadOpenClose(ref p, packet,
			MuiListtreeMessageCodec.Open, out var open));
		Assert.Equal(0x3040u, open.Node);
		Assert.True(MuiListtreeMessageCodec.WriteSort(ref p, packet,
			MuiListtreeMessageCodec.GetNr, 0x3020, 2));
		Assert.True(MuiListtreeMessageCodec.TryReadSort(ref p, packet,
			MuiListtreeMessageCodec.GetNr, out var getNr));
		Assert.Equal(2u, getNr.Flags);

		Assert.True(MuiListtreeMessageCodec.WriteMoveExchange(ref p, packet,
			MuiListtreeMessageCodec.Move, 0x3020, 0x3040, 0x3050, 0x3060, 3));
		Assert.True(MuiListtreeMessageCodec.TryReadMoveExchange(ref p, packet,
			MuiListtreeMessageCodec.Move, out var move));
		Assert.Equal(0x3050u, move.NewParent);
		Assert.Equal(3u, move.Flags);

		Assert.True(MuiListtreeMessageCodec.WriteRename(ref p, packet,
			0x3040, 0x3070, 8));
		Assert.True(MuiListtreeMessageCodec.TryReadRename(ref p, packet,
			out var rename));
		Assert.Equal(0x3070u, rename.Name);
		Assert.True(MuiListtreeMessageCodec.WriteFindName(ref p, packet,
			0x3020, 0x3070, 9));
		Assert.True(MuiListtreeMessageCodec.TryReadFindName(ref p, packet,
			out var find));
		Assert.Equal(0x3020u, find.Parent);

		Assert.True(MuiListtreeMessageCodec.WriteDropMark(ref p, packet, 10, 11));
		Assert.True(MuiListtreeMessageCodec.TryReadDropMark(ref p, packet,
			out var drop));
		Assert.Equal(10u, drop.Position);
		Assert.True(MuiListtreeMessageCodec.WriteTestPos(ref p, packet, 12, 13,
			0x3040));
		Assert.True(MuiListtreeMessageCodec.TryReadTestPos(ref p, packet,
			out var testPos));
		Assert.Equal(12u, testPos.X);
		Assert.Equal(0x3040u, testPos.Entry);

		Assert.False(MuiListtreeMessageCodec.WriteSet(ref p, packet,
			0x80420000u, 1, 2));
		Assert.False(MuiListtreeMessageCodec.TryReadInsert(ref p,
			APTR.FromPointer(0x80FFF), out _));
		Assert.False(MuiListtreeMessageCodec.TryReadGet(ref p, packet, out _));
	}

	[Fact]
	public void ListtreeMethodHeaderUsesNamedField()
	{
		var p = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x2D00);
		Assert.True(MuiListtreeMessageCodec.WriteInsert(ref p, packet,
			0x3000, 0x3010, 0x3020, 0x3030, 4));
		Assert.True(MuiListtreeMessageCodec.TryReadMethodId(ref p, packet,
			out var header));
		Assert.Equal(MuiListtreeMessageCodec.Insert, header.MethodId);
		Assert.False(MuiListtreeMessageCodec.TryReadMethodId(ref p,
			APTR.Null, out _));
	}

	[Fact]
	public void ListtreeTypedReadersUseNamedMethodHeader()
	{
		var p = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x2D00);
		Assert.True(MuiListtreeMessageCodec.WriteSet(ref p, packet,
			MuiListtreeMessageCodec.Set, 9, 11));
		Assert.True(MuiListtreeMessageCodec.TryReadSet(ref p, packet,
			MuiListtreeMessageCodec.Set, out var set));
		Assert.Equal(MuiListtreeMessageCodec.Set, set.MethodId);
		Assert.False(MuiListtreeMessageCodec.TryReadSet(ref p, packet,
			MuiListtreeMessageCodec.NoNotifySet, out _));

		Assert.True(MuiListtreeMessageCodec.WriteInsert(ref p, packet,
			0x3000, 0x3010, 0x3020, 0x3030, 4));
		Assert.True(MuiListtreeMessageCodec.TryReadInsert(ref p, packet,
			out var insert));
		Assert.Equal(MuiListtreeMessageCodec.Insert, insert.MethodId);
		Assert.False(MuiListtreeMessageCodec.TryReadGet(ref p, packet, out _));
	}

	[Fact]
	public void ListtreeFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var p = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x2D00);
		var cursor = default(MuiListtreeFieldCursor);
		cursor.Message = packet;
		cursor.Packet = MuiListtreePacketKind.Insert;
		cursor.Field = MuiListtreeField.MethodId;
		Assert.True(MuiListtreeFieldCursorCodec.TryGetAddress(ref p, cursor,
			out var address));
		Assert.Equal(0x2D00u, address.Raw);
		cursor.Field = MuiListtreeField.Name;
		Assert.True(MuiListtreeFieldCursorCodec.TryGetAddress(ref p, cursor,
			out address));
		Assert.Equal(0x2D04u, address.Raw);
		cursor.Field = MuiListtreeField.UserData;
		Assert.True(MuiListtreeFieldCursorCodec.TryGetAddress(ref p, cursor,
			out address));
		Assert.Equal(0x2D08u, address.Raw);
		cursor.Field = MuiListtreeField.Parent;
		Assert.True(MuiListtreeFieldCursorCodec.TryGetAddress(ref p, cursor,
			out address));
		Assert.Equal(0x2D0Cu, address.Raw);
		cursor.Field = MuiListtreeField.Previous;
		Assert.True(MuiListtreeFieldCursorCodec.TryGetAddress(ref p, cursor,
			out address));
		Assert.Equal(0x2D10u, address.Raw);
		cursor.Field = MuiListtreeField.Flags;
		Assert.True(MuiListtreeFieldCursorCodec.TryGetAddress(ref p, cursor,
			out address));
		Assert.Equal(0x2D14u, address.Raw);

		Assert.True(MuiListtreeFieldCursorCodec.TryWriteUInt32(ref p, packet,
			MuiListtreePacketKind.GetEntry, MuiListtreeField.Position,
			unchecked((uint)-2)));
		Assert.True(MuiListtreeFieldCursorCodec.TryReadUInt32(ref p, packet,
			MuiListtreePacketKind.GetEntry, MuiListtreeField.Position,
			out var position));
		Assert.Equal(unchecked((uint)-2), position);
		cursor.Packet = MuiListtreePacketKind.Set;
		cursor.Field = MuiListtreeField.Node;
		Assert.False(MuiListtreeFieldCursorCodec.TryGetAddress(ref p, cursor,
			out _));
		cursor.Message = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Packet = MuiListtreePacketKind.TestPos;
		cursor.Field = MuiListtreeField.Entry;
		Assert.False(MuiListtreeFieldCursorCodec.TryGetAddress(ref p, cursor,
			out _));
	}

	[Fact]
	public void DispatcherRoutesInsertAndGetEntry()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var name = WriteString(ref platform, 0x2900, "routed");
		var packet = APTR.FromPointer(0x2A00);
		platform.WriteUInt32(packet, 0, MuiListtreeCore.MethodInsert);
		platform.WriteUInt32(packet, 4, name.Raw);   // Name
		platform.WriteUInt32(packet, 8, 0);           // User
		platform.WriteUInt32(packet, 12, ListRoot);   // ListNode
		platform.WriteUInt32(packet, 16, PrevTail);   // PrevNode
		platform.WriteUInt32(packet, 20, 0);          // Flags
		var node = MuiListtreeDispatcher.Dispatch(ref platform, State, tree,
			packet);
		Assert.NotEqual(0u, node);
		Assert.Equal(1u, MuiListtreeCore.RootCount(ref platform, State, tree));
		// MUIM_Listtree_GetEntry(Root, Head) returns the same node.
		platform.WriteUInt32(packet, 0, MuiListtreeCore.MethodGetEntry);
		platform.WriteUInt32(packet, 4, ListRoot);
		platform.WriteUInt32(packet, 8, TreeHead);
		platform.WriteUInt32(packet, 12, 0);
		Assert.Equal(node, MuiListtreeDispatcher.Dispatch(ref platform, State, tree,
			packet));
	}

	[Fact]
	public void DispatcherDeclinesForeignObjects()
	{
		var platform = CreatePlatform(out _);
		var otherName = APTR.FromPointer(0x1300);
		platform.WriteCString(otherName, "Group.mui");
		var otherClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			otherName, APTR.Null, 0, APTR.FromPointer(1), false);
		var other = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			otherClass, APTR.Null);
		var packet = APTR.FromPointer(0x2B00);
		platform.WriteUInt32(packet, 0, MuiListtreeCore.MethodGetNr);
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, 0);
		// Not a Listtree -> unclaimed.
		Assert.Equal(0u, MuiListtreeDispatcher.Dispatch(ref platform, State, other,
			packet));
	}

	// =====================================================================
	// Deep / wide trees + disposal balance
	// =====================================================================

	[Fact]
	public void DeepChainTraversesAndDisposesWithoutLeak()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		// Build a 200-deep chain first (a node only becomes openable once it owns
		// a child), then open every internal node so the whole chain is visible.
		const int depth = 200;
		var nodes = new APTR[depth];
		var parent = ListRoot;
		for (var i = 0; i < depth; i++)
		{
			nodes[i] = InsertName(ref platform, tree, "n", parent, PrevTail);
			parent = (uint)nodes[i].Raw;
		}
		for (var i = 0; i < depth - 1; i++)
			Assert.True(MuiListtreeCore.Open(ref platform, State, tree,
				APTR.FromPointer(ListRoot), APTR.FromPointer(nodes[i].Raw), 0));
		Assert.Equal((uint)depth, MuiListtreeCore.TotalNodes(ref platform, State,
			tree));
		Assert.Equal((uint)depth, MuiListtreeCore.VisibleCount(ref platform, State,
			tree));
		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void WideListTraversesAndDisposesWithoutLeak()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var root = InsertName(ref platform, tree, "root", ListRoot, PrevTail);
		const int width = 500;
		for (var i = 0; i < width; i++)
			InsertName(ref platform, tree, "leaf", (uint)root.Raw, PrevTail);
		Assert.Equal((uint)width, MuiListtreeCore.ChildCount(ref platform, root));
		Assert.Equal((uint)(width + 1), MuiListtreeCore.TotalNodes(ref platform,
			State, tree));
		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void AllocationFailureRollsBackAndBalances()
	{
		// A small arena forces an allocation failure mid-build. The failed insert
		// must return NULL and leave the tree consistent; disposal must free every
		// surviving node so allocations balance.
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x3000, 0x1600, State);
		MuiHeadlessObjectCore.Initialize(ref platform, State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Listtree.mcc");
		var boopsi = APTR.FromPointer(0x1200);
		var listtreeClass = MuiListtreeCore.RegisterListtreeExternalClass(
			ref platform, State, name, boopsi, APTR.Null);
		Assert.NotEqual(APTR.Null, listtreeClass);
		var tree = MuiListtreeCore.CreateListtree(ref platform, State,
			listtreeClass, APTR.Null);
		Assert.NotEqual(APTR.Null, tree);

		var leafName = APTR.FromPointer(0x1300);
		platform.WriteCString(leafName, "leaf");
		uint inserted = 0;
		var failed = false;
		for (var i = 0; i < 100000; i++)
		{
			var node = MuiListtreeCore.Insert(ref platform, State, tree, leafName,
				APTR.Null, APTR.FromPointer(ListRoot), APTR.FromPointer(PrevTail),
				0);
			if (node.IsNull) { failed = true; break; }
			inserted++;
		}
		Assert.True(failed);
		Assert.True(inserted > 0);
		// The failed insert added nothing.
		Assert.Equal(inserted, MuiListtreeCore.RootCount(ref platform, State,
			tree));
		Assert.Equal(inserted, MuiListtreeCore.TotalNodes(ref platform, State,
			tree));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State, tree));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listtreeClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void ConstructHookStringDisposalFreesOwnedUserData()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.ConstructHook, ConstructHookString, false));
		var payload = WriteString(ref platform, 0x2C00, "owned-user");
		var root = InsertName(ref platform, tree, "root", ListRoot, PrevTail);
		// Owned-string user data across a small subtree.
		for (var i = 0; i < 8; i++)
			MuiListtreeCore.Insert(ref platform, State, tree,
				WriteString(ref platform, (uint)(0x2C40 + i * 0x20), "n"), payload,
				APTR.FromPointer(root.Raw), APTR.FromPointer(PrevTail), 0);
		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	// =====================================================================
	// Helpers
	// =====================================================================

	private static void DisposeAndAssertBalanced(ref MuiHeadlessTestPlatform platform,
		APTR tree, APTR listtreeClass)
	{
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State, tree));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listtreeClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	private static APTR GetEntry(ref MuiHeadlessTestPlatform platform, APTR tree,
		uint node, int position, uint flags) =>
		MuiListtreeCore.GetEntry(ref platform, State, tree, APTR.FromPointer(node),
			position, flags);

	private static APTR InsertName(ref MuiHeadlessTestPlatform platform, APTR tree,
		string name, uint listNode, uint prevNode)
	{
		var n = WriteUniqueString(ref platform, name);
		return MuiListtreeCore.Insert(ref platform, State, tree, n, APTR.Null,
			APTR.FromPointer(listNode), APTR.FromPointer(prevNode), 0);
	}

	// A rolling scratch cursor in the low mapped region for name literals so each
	// inserted name has an independent backing buffer.
	private static uint _stringCursor = 0x3000;

	private static APTR WriteUniqueString(ref MuiHeadlessTestPlatform platform,
		string value)
	{
		var target = APTR.FromPointer(_stringCursor);
		platform.WriteCString(target, value);
		_stringCursor += (uint)((value.Length + 4) & ~3) + 4;
		if (_stringCursor > 0x7000) _stringCursor = 0x3000;
		return target;
	}

	private static APTR WriteString(ref MuiHeadlessTestPlatform platform,
		uint address, string value)
	{
		var target = APTR.FromPointer(address);
		platform.WriteCString(target, value);
		return target;
	}

	private static string NodeName(ref MuiHeadlessTestPlatform platform, APTR node)
	{
		if (node.IsNull) return string.Empty;
		return ReadCString(ref platform, APTR.FromPointer(platform.ReadUInt32(node,
			MuiListtreeCore.TreeNodeNameOffset)));
	}

	private static string ReadCString(ref MuiHeadlessTestPlatform platform,
		APTR address)
	{
		if (address.IsNull) return string.Empty;
		var builder = new StringBuilder();
		for (var i = 0; i < 4096; i++)
		{
			var ch = platform.ReadUInt8(address, i);
			if (ch == 0) break;
			builder.Append((char)ch);
		}
		return builder.ToString();
	}

	private static APTR Create(ref MuiHeadlessTestPlatform platform,
		APTR listtreeClass) =>
		MuiListtreeCore.CreateListtree(ref platform, State, listtreeClass,
			APTR.Null);

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR listtreeClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x200000, 0x8000, State);
		var listtreeName = APTR.FromPointer(0x1100);
		platform.WriteCString(listtreeName, "Listtree.mcc");
		MuiHeadlessObjectCore.Initialize(ref platform, State);
		// External component: caller-provided BOOPSI class pointer, registered as
		// external (never builtin).
		var boopsi = APTR.FromPointer(0x1200);
		listtreeClass = MuiListtreeCore.RegisterListtreeExternalClass(ref platform,
			State, listtreeName, boopsi, APTR.Null);
		return platform;
	}
}
