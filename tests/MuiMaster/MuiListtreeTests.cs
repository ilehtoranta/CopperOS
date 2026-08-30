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
	private const uint InsertFlagsNextNode = 1u << 12;
	private const uint MethodFlagsVisible = 1u << 14;
	private const uint MethodFlagsNr = 1u << 15;
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
	public void ListtreeHeaderMemoryAdapterUsesNamedStructFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x24C0);
		Assert.True(MuiListtreeCore.MuiListtreeHeaderMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeHeaderField.RootCount,
			3));
		Assert.True(MuiListtreeCore.MuiListtreeHeaderMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeHeaderField.RootCount,
			out var count));
		Assert.Equal(3u, count);
		Assert.True(MuiListtreeCore.MuiListtreeHeaderMemoryCodec.TryGetAddress(
			ref platform, address, MuiListtreeCore.MuiListtreeHeaderField.DropValue,
			out var dropValueAddress));
		Assert.Equal(0x24E0u, dropValueAddress.Raw);
		Assert.False(MuiListtreeCore.MuiListtreeHeaderMemoryCodec.TryGetAddress(
			ref platform, address,
			(MuiListtreeCore.MuiListtreeHeaderField)255, out _));
	}

	[Fact]
	public void ListtreePolicyMemoryAdapterUsesNamedStructFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2500);
		Assert.True(MuiListtreeCore.MuiListtreePolicyMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreePolicyField.Quiet, 1));
		Assert.True(MuiListtreeCore.MuiListtreePolicyMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreePolicyField.Quiet,
			out var quiet));
		Assert.Equal(1u, quiet);
		Assert.True(MuiListtreeCore.MuiListtreePolicyMemoryCodec.TryGetAddress(
			ref platform, address, MuiListtreeCore.MuiListtreePolicyField.SortHook,
			out var sortHookAddress));
		Assert.Equal(0x252Cu, sortHookAddress.Raw);
		Assert.False(MuiListtreeCore.MuiListtreePolicyMemoryCodec.TryGetAddress(
			ref platform, address,
			(MuiListtreeCore.MuiListtreePolicyField)255, out _));
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
		var record = MuiHeadlessObjectCore.FindObject(ref platform, State, tree);
		Assert.True(record.IsNotNull);

		// A raw compatibility write cannot replace the canonical policy record.
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			record, MuiListtreeCore.DuplicateNodeName, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			record, MuiListtreeCore.Quiet, 0, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			record, MuiListtreeCore.SortHook, 0x5678u, false));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, tree,
			MuiListtreeCore.DuplicateNodeName, out var duplicate));
		Assert.Equal(policy.DuplicateNodeName, duplicate);
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State, tree,
			MuiListtreeCore.Quiet, out _));
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
	public void ListtreeQuietIsRuntimeSetOnly()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		Assert.NotEqual(APTR.Null, tree);

		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Quiet, 1, false));
		Assert.True(MuiListtreeCore.TryGetPolicyStateRecord(ref platform, State,
			tree, out var policy));
		Assert.Equal(1u, policy.Quiet);
		Assert.False(MuiListtreeCore.GetAttribute(ref platform, State, tree,
			MuiListtreeCore.Quiet, out _));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State, tree,
			MuiListtreeCore.Quiet, out _));

		var message = APTR.FromPointer(0x7D00);
		var storage = APTR.FromPointer(0x7D40);
		Assert.True(MuiListtreeMessageCodec.WriteGet(ref platform, message,
			MuiListtreeCore.Quiet, storage.Raw));
		Assert.Equal(0u, MuiListtreeDispatcher.Dispatch(ref platform, State, tree,
			message));

		var tags = APTR.FromPointer(0x7D80);
		platform.WriteUInt32(tags, 0, MuiListtreeCore.Quiet);
		platform.WriteUInt32(tags, 4, 1);
		platform.WriteUInt32(tags, 8, 0);
		var constructionTree = MuiListtreeCore.CreateListtree(ref platform, State,
			listtreeClass, tags);
		Assert.Equal(APTR.Null, constructionTree);
	}

	[Fact]
	public void ListtreeGenericSetUsesNamedPolicyRecord()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);

		// Generic SetAttribute is the OM_SET-compatible path. It must reach the
		// same named policy record as the direct Listtree setter, rather than
		// leaving a stale raw compatibility scalar behind.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.DuplicateNodeName, 7, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.DragDropSort, 9, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.SortHook, 0x1234u, false));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, tree,
			MuiListtreeCore.DuplicateNodeName, out var duplicate));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, tree,
			MuiListtreeCore.DragDropSort, out var dragDrop));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, tree,
			MuiListtreeCore.SortHook, out var sortHook));
		Assert.Equal(1u, duplicate);
		Assert.Equal(1u, dragDrop);
		Assert.Equal(0x1234u, sortHook);
	}

	[Fact]
	public void ListtreeConstructionTagsPublishNamedPolicyAndPresentationRecords()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tags = APTR.FromPointer(0x1300);
		var title = APTR.FromPointer(0x1500);
		platform.WriteCString(title, "Tagged tree");
		Assert.True(MuiAslTagItemCodec.Write(ref platform, tags,
			new MuiAslTagItemRecord
			{
				Tag = MuiListtreeCore.DuplicateNodeName,
				Data = 0
			}));
		Assert.True(MuiAslTagItemCodec.Write(ref platform,
			APTR.FromPointer(tags.Raw + MuiAslTagItemRecord.Size),
			new MuiAslTagItemRecord
			{
				Tag = MuiListtreeCore.DragDropSort,
				Data = 0
			}));
		Assert.True(MuiAslTagItemCodec.Write(ref platform,
			APTR.FromPointer(tags.Raw + (MuiAslTagItemRecord.Size * 2u)),
			new MuiAslTagItemRecord
			{
				Tag = MuiListtreeCore.Title,
				Data = title.Raw
			}));
		Assert.True(MuiAslTagItemCodec.Write(ref platform,
			APTR.FromPointer(tags.Raw + (MuiAslTagItemRecord.Size * 3u)),
			new MuiAslTagItemRecord { Tag = MuiAslTagListCore.TagDone }));

		var tree = MuiListtreeCore.CreateListtree(ref platform, State,
			listtreeClass, tags);
		Assert.True(tree.IsNotNull);
		Assert.True(MuiListtreeCore.TryGetPolicyStateRecord(ref platform, State,
			tree, out var policy));
		Assert.True(MuiListtreeCore.TryGetPresentationStateRecord(ref platform,
			State, tree, out var presentation));
		Assert.Equal(0u, policy.DuplicateNodeName);
		Assert.Equal(0u, policy.DragDropSort);
		// MorphOS treats the historically string-typed Title tag as BOOL:
		// any nonzero caller value projects to TRUE.
		Assert.Equal(1u, presentation.Title);
	}

	[Fact]
	public void ListtreePresentationAttributesUseNamedRecordAndClassGatedSetGet()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		Assert.True(MuiListtreeCore.TryGetPresentationStateRecord(ref platform,
			State, tree, out var initial));
		Assert.Equal(MuiListtreePresentationStateRecord.Cookie, initial.Magic);
		Assert.Equal(0u, initial.EmptyNodes);
		Assert.Equal(0u, initial.Format.Raw);
		Assert.Equal(0u, initial.MultiSelect);
		Assert.Equal(0u, initial.NList);
		Assert.Equal(0u, initial.Title);
		Assert.Equal(0u, initial.TreeColumn);

		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.EmptyNodes, 7, false));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Format, 0x6100, false));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.MultiSelect, 2, false));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.NList, 3, false));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Title, 0x6200, false));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.TreeColumn, 4, false));

		Assert.True(MuiListtreeCore.TryGetPresentationStateRecord(ref platform,
			State, tree, out var actual));
		Assert.Equal(1u, actual.EmptyNodes);
		Assert.Equal(0x6100u, actual.Format.Raw);
		Assert.Equal(1u, actual.MultiSelect);
		Assert.Equal(1u, actual.NList);
		Assert.Equal(1u, actual.Title);
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Title, 0, false));
		Assert.True(MuiListtreeCore.GetAttribute(ref platform, State, tree,
			MuiListtreeCore.Title, out var titleOff));
		Assert.Equal(0u, titleOff);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Title, 0xFFFFFFFFu, false));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, tree,
			MuiListtreeCore.Title, out var titleOn));
		Assert.Equal(1u, titleOn);
		Assert.Equal(1u, actual.TreeColumn);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.NList, 0, false));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, tree,
			MuiListtreeCore.NList, out var nList));
		Assert.Equal(0u, nList);

		// A raw compatibility write does not replace the canonical typed getter.
		var record = MuiHeadlessObjectCore.FindObject(ref platform, State, tree);
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			record, MuiListtreeCore.Format, 0x6300, false));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, tree,
			MuiListtreeCore.Format, out var format));
		Assert.Equal(0x6100u, format);
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
	public void ListtreeTestPosMemoryAdapterUsesNamedMixedWidthFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2E00);
		Assert.True(MuiListtreeCore.MuiListtreeTestPosMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeTestPosField.ListEntry,
			unchecked((uint)-2)));
		Assert.True(MuiListtreeCore.MuiListtreeTestPosMemoryCodec.TryWriteUInt16(
			ref platform, address, MuiListtreeCore.MuiListtreeTestPosField.Flags,
			3));
		Assert.True(MuiListtreeCore.MuiListtreeTestPosMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeTestPosField.ListEntry,
			out var entry));
		Assert.Equal(unchecked((uint)-2), entry);
		Assert.True(MuiListtreeCore.MuiListtreeTestPosMemoryCodec.TryGetAddress(
			ref platform, address, MuiListtreeCore.MuiListtreeTestPosField.ListFlags,
			out var flagsAddress, out var fieldSize));
		Assert.Equal(0x2E0Au, flagsAddress.Raw);
		Assert.Equal(2u, fieldSize);
		Assert.False(MuiListtreeCore.MuiListtreeTestPosMemoryCodec.TryGetAddress(
			ref platform, address,
			(MuiListtreeCore.MuiListtreeTestPosField)255, out _, out _));
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
	public void VisibleOrdinalInsertionUsesOnlyVisibleChildren()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var parent = InsertName(ref platform, tree, "closed-parent", ListRoot,
			PrevTail);
		var first = InsertName(ref platform, tree, "first", (uint)parent.Raw,
			PrevTail);
		var second = InsertName(ref platform, tree, "second", (uint)parent.Raw,
			PrevTail);

		// The parent is closed, so its child list has no visible ordinal zero.
		// The visible insertion therefore appends instead of anchoring before the
		// hidden first child.
		var closedInsert = MuiListtreeCore.Insert(ref platform, State, tree,
			WriteUniqueString(ref platform, "closed-visible"), APTR.Null, parent,
			APTR.FromPointer(0), MethodFlagsNr | MethodFlagsVisible);
		Assert.NotEqual(APTR.Null, closedInsert);
		Assert.Equal(first, GetEntry(ref platform, tree, (uint)parent.Raw, 0, 0));
		Assert.Equal(second, GetEntry(ref platform, tree, (uint)parent.Raw, 1, 0));
		Assert.Equal(closedInsert, GetEntry(ref platform, tree,
			(uint)parent.Raw, 2, 0));

		Assert.True(MuiListtreeCore.Open(ref platform, State, tree,
			APTR.FromPointer(ListRoot), parent, 0));
		var openInsert = MuiListtreeCore.Insert(ref platform, State, tree,
			WriteUniqueString(ref platform, "open-visible"), APTR.Null, parent,
			APTR.FromPointer(0), MethodFlagsNr | MethodFlagsVisible |
				InsertFlagsNextNode);
		Assert.NotEqual(APTR.Null, openInsert);
		Assert.Equal(openInsert, GetEntry(ref platform, tree,
			(uint)parent.Raw, 0, 0));
		Assert.Equal(first, GetEntry(ref platform, tree, (uint)parent.Raw, 1, 0));

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
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
	public void MoveResolvesNumericDestinationOrdinalWithVisibleParentRules()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var moving = InsertName(ref platform, tree, "moving", ListRoot, PrevTail);
		var parent = InsertName(ref platform, tree, "parent", ListRoot, PrevTail);
		var first = InsertName(ref platform, tree, "first", (uint)parent.Raw,
			PrevTail);
		var second = InsertName(ref platform, tree, "second", (uint)parent.Raw,
			PrevTail);

		// The destination list is closed, so visible ordinal zero has no anchor;
		// moving therefore appends after the hidden children.
		Assert.True(MuiListtreeCore.Move(ref platform, State, tree,
			APTR.FromPointer(ListRoot), APTR.FromPointer(0), parent,
			APTR.FromPointer(0), MethodFlagsNr | MethodFlagsVisible));
		Assert.Equal(first, GetEntry(ref platform, tree, (uint)parent.Raw, 0, 0));
		Assert.Equal(second, GetEntry(ref platform, tree, (uint)parent.Raw, 1, 0));
		Assert.Equal(moving, GetEntry(ref platform, tree, (uint)parent.Raw, 2, 0));

		Assert.True(MuiListtreeCore.Open(ref platform, State, tree,
			APTR.FromPointer(ListRoot), parent, 0));
		// After unlinking, visible ordinal zero names 'first'; Move inserts after
		// that typed anchor, yielding first, moving, second.
		Assert.True(MuiListtreeCore.Move(ref platform, State, tree,
			parent, APTR.FromPointer(2), parent, APTR.FromPointer(0),
			MethodFlagsNr | MethodFlagsVisible));
		Assert.Equal(first, GetEntry(ref platform, tree, (uint)parent.Raw, 0, 0));
		Assert.Equal(moving, GetEntry(ref platform, tree, (uint)parent.Raw, 1, 0));
		Assert.Equal(second, GetEntry(ref platform, tree, (uint)parent.Raw, 2, 0));

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
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
		// MorphOS relative selectors exchange TreeNode1 with its immediate
		// sibling. ListNode2 is intentionally ignored for these selectors; the
		// typed topology record owning TreeNode1 is the authoritative list.
		Assert.True(MuiListtreeCore.Exchange(ref platform, State, tree,
			APTR.FromPointer(ListRoot), APTR.FromPointer(b.Raw),
			APTR.FromPointer(0xDEADBEEFu),
			APTR.FromPointer(unchecked((uint)MuiListtreeCore.ExchangeTreeNode2Down)),
			0));
		Assert.Equal("c", NodeName(ref platform, GetEntry(ref platform, tree,
			ListRoot, 0, 0)));
		Assert.Equal("a", NodeName(ref platform, GetEntry(ref platform, tree,
			ListRoot, 1, 0)));
		Assert.Equal("b", NodeName(ref platform, GetEntry(ref platform, tree,
			ListRoot, 2, 0)));
		Assert.True(MuiListtreeCore.Exchange(ref platform, State, tree,
			APTR.FromPointer(ListRoot), APTR.FromPointer(b.Raw),
			APTR.FromPointer(0xDEADBEEFu),
			APTR.FromPointer(unchecked((uint)MuiListtreeCore.ExchangeTreeNode2Up)),
			0));
		Assert.Equal("b", NodeName(ref platform, GetEntry(ref platform, tree,
			ListRoot, 1, 0)));
		Assert.False(MuiListtreeCore.Exchange(ref platform, State, tree,
			APTR.FromPointer(ListRoot), APTR.FromPointer(c.Raw),
			APTR.FromPointer(0xDEADBEEFu),
			APTR.FromPointer(unchecked((uint)MuiListtreeCore.ExchangeTreeNode2Up)),
			0));
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
		Assert.Equal(0x3010u, insert.User);
		Assert.Equal(0x3020u, insert.ListNode);
		Assert.Equal(0x3030u, insert.PrevNode);
		Assert.Equal(4u, insert.Flags);

		Assert.True(MuiListtreeMessageCodec.WriteRemove(ref p, packet,
			0x3020, 0x3040, 5));
		Assert.True(MuiListtreeMessageCodec.TryReadRemove(ref p, packet,
			out var remove));
		Assert.Equal(0x3020u, remove.ListNode);
		Assert.Equal(0x3040u, remove.TreeNode);

		Assert.True(MuiListtreeMessageCodec.WriteGetEntry(ref p, packet,
			0x3020, unchecked((uint)-2), 6));
		Assert.True(MuiListtreeMessageCodec.TryReadGetEntry(ref p, packet,
			out var entry));
		Assert.Equal(0x3020u, entry.Node);
		Assert.Equal(unchecked((uint)-2), entry.Position);
		Assert.Equal(6u, entry.Flags);

		Assert.True(MuiListtreeMessageCodec.WriteOpenClose(ref p, packet,
			MuiListtreeMessageCodec.Open, 0x3020, 0x3040, 1));
		Assert.True(MuiListtreeMessageCodec.TryReadOpenClose(ref p, packet,
			MuiListtreeMessageCodec.Open, out var open));
		Assert.Equal(0x3020u, open.ListNode);
		Assert.Equal(0x3040u, open.TreeNode);
		Assert.True(MuiListtreeMessageCodec.WriteGetNr(ref p, packet, 0x3020, 2));
		Assert.True(MuiListtreeMessageCodec.TryReadGetNr(ref p, packet,
			MuiListtreeMessageCodec.GetNr, out var getNr));
		Assert.Equal(0x3020u, getNr.TreeNode);
		Assert.Equal(2u, getNr.Flags);

		Assert.True(MuiListtreeMessageCodec.WriteMoveExchange(ref p, packet,
			MuiListtreeMessageCodec.Move, 0x3020, 0x3040, 0x3050, 0x3060, 3));
		Assert.True(MuiListtreeMessageCodec.TryReadMoveExchange(ref p, packet,
			MuiListtreeMessageCodec.Move, out var move));
		Assert.Equal(0x3020u, move.OldListNode);
		Assert.Equal(0x3040u, move.OldTreeNode);
		Assert.Equal(0x3050u, move.NewListNode);
		Assert.Equal(0x3060u, move.NewTreeNode);
		Assert.Equal(3u, move.Flags);

		Assert.True(MuiListtreeMessageCodec.WriteRename(ref p, packet,
			0x3040, 0x3070, 8));
		Assert.True(MuiListtreeMessageCodec.TryReadRename(ref p, packet,
			out var rename));
		Assert.Equal(0x3040u, rename.TreeNode);
		Assert.Equal(0x3070u, rename.NewName);
		Assert.True(MuiListtreeMessageCodec.WriteFindName(ref p, packet,
			0x3020, 0x3070, 9));
		Assert.True(MuiListtreeMessageCodec.TryReadFindName(ref p, packet,
			out var find));
		Assert.Equal(0x3020u, find.ListNode);

		Assert.True(MuiListtreeMessageCodec.WriteDropMark(ref p, packet, 10, 11));
		Assert.True(MuiListtreeMessageCodec.TryReadDropMark(ref p, packet,
			out var drop));
		Assert.Equal(10u, drop.Entry);
		Assert.Equal(11u, drop.Values);
		Assert.True(MuiListtreeMessageCodec.WriteTestPos(ref p, packet, 12, 13,
			0x3040));
		Assert.True(MuiListtreeMessageCodec.TryReadTestPos(ref p, packet,
			out var testPos));
		Assert.Equal(12u, testPos.X);
		Assert.Equal(13u, testPos.Y);
		Assert.Equal(0x3040u, testPos.Result);

		Assert.False(MuiListtreeMessageCodec.WriteSet(ref p, packet,
			0x80420000u, 1, 2));
		Assert.False(MuiListtreeMessageCodec.TryReadInsert(ref p,
			APTR.FromPointer(0x80FFF), out _));
		// The construct-hook packet is a full 24-byte guest record; a boundary
		// cursor that cannot cover the entire record must be rejected.
		Assert.False(MuiListtreeMessageCodec.TryReadInsert(ref p,
			APTR.FromPointer(0x7FFF8), out _));
		Assert.False(MuiListtreeMessageCodec.TryReadGet(ref p, packet, out _));
	}

	[Fact]
	public void ListtreeCorePacketsUseDedicatedStructCodecs()
	{
		var p = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x2E00);

		var set = default(MuiListtreeSetMessage);
		set.MethodId = MuiListtreeMessageCodec.Set;
		set.Attribute = 7;
		set.Value = 9;
		Assert.True(MuiListtreeSetMessageCodec.TryWrite(ref p, packet, set));
		Assert.True(MuiListtreeSetMessageCodec.TryRead(ref p, packet,
			out var setRead));
		Assert.Equal(set.Attribute, setRead.Attribute);
		Assert.Equal(set.Value, setRead.Value);

		var get = default(MuiListtreeGetMessage);
		get.MethodId = MuiListtreeMessageCodec.Get;
		get.Attribute = 11;
		get.Storage = 0x2E80;
		Assert.True(MuiListtreeGetMessageCodec.TryWrite(ref p, packet, get));
		Assert.True(MuiListtreeGetMessageCodec.TryRead(ref p, packet,
			out var getRead));
		Assert.Equal(get.Attribute, getRead.Attribute);
		Assert.Equal(get.Storage, getRead.Storage);

		var entry = default(MuiListtreeGetEntryMessage);
		entry.MethodId = MuiListtreeMessageCodec.GetEntry;
		entry.Node = 0x2F00;
		entry.Position = unchecked((uint)-2);
		entry.Flags = 3;
		Assert.True(MuiListtreeGetEntryMessageCodec.TryWrite(ref p, packet,
			entry));
		Assert.True(MuiListtreeGetEntryMessageCodec.TryRead(ref p, packet,
			out var entryRead));
		Assert.Equal(entry.Node, entryRead.Node);
		Assert.Equal(entry.Position, entryRead.Position);
		Assert.Equal(entry.Flags, entryRead.Flags);

		var method = default(MuiListtreeMethodMessage);
		method.MethodId = MuiListtreeMessageCodec.Insert;
		Assert.True(MuiListtreeMethodMessageCodec.TryWrite(ref p, packet,
			method.MethodId));
		Assert.True(MuiListtreeMethodMessageCodec.TryRead(ref p, packet,
			out var methodRead));
		Assert.Equal(method.MethodId, methodRead.MethodId);

		var insert = default(MuiListtreeInsertMessage);
		insert.MethodId = MuiListtreeMessageCodec.Insert;
		insert.Name = 0x2F20;
		insert.User = 0x2F24;
		insert.ListNode = 0x2F28;
		insert.PrevNode = 0x2F2C;
		insert.Flags = 5;
		Assert.True(MuiListtreeInsertMessageCodec.TryWrite(ref p, packet,
			insert));
		Assert.True(MuiListtreeInsertMessageCodec.TryRead(ref p, packet,
			out var insertRead));
		Assert.Equal(insert.Name, insertRead.Name);
		Assert.Equal(insert.PrevNode, insertRead.PrevNode);
		Assert.Equal(insert.Flags, insertRead.Flags);

		var remove = default(MuiListtreeRemoveMessage);
		remove.MethodId = MuiListtreeMessageCodec.Remove;
		remove.ListNode = 0x2F30;
		remove.TreeNode = 0x2F34;
		remove.Flags = 6;
		Assert.True(MuiListtreeRemoveMessageCodec.TryWrite(ref p, packet,
			remove));
		Assert.True(MuiListtreeRemoveMessageCodec.TryRead(ref p, packet,
			out var removeRead));
		Assert.Equal(remove.TreeNode, removeRead.TreeNode);
		Assert.Equal(remove.Flags, removeRead.Flags);

		var openClose = default(MuiListtreeOpenCloseMessage);
		openClose.MethodId = MuiListtreeMessageCodec.Open;
		openClose.ListNode = 0x2F40;
		openClose.TreeNode = 0x2F44;
		openClose.Flags = 7;
		Assert.True(MuiListtreeOpenCloseMessageCodec.TryWrite(ref p, packet,
			openClose));
		Assert.True(MuiListtreeOpenCloseMessageCodec.TryRead(ref p, packet,
			out var openCloseRead));
		Assert.Equal(openClose.MethodId, openCloseRead.MethodId);
		Assert.Equal(openClose.ListNode, openCloseRead.ListNode);
		Assert.Equal(openClose.Flags, openCloseRead.Flags);

		var sort = default(MuiListtreeSortMessage);
		sort.MethodId = MuiListtreeMessageCodec.Sort;
		sort.ListNode = 0x2F50;
		sort.Flags = 8;
		Assert.True(MuiListtreeSortMessageCodec.TryWrite(ref p, packet, sort));
		Assert.True(MuiListtreeSortMessageCodec.TryRead(ref p, packet,
			out var sortRead));
		Assert.Equal(sort.ListNode, sortRead.ListNode);
		Assert.Equal(sort.Flags, sortRead.Flags);

		var getNr = default(MuiListtreeGetNrMessage);
		getNr.MethodId = MuiListtreeMessageCodec.GetNr;
		getNr.TreeNode = 0x2F54;
		getNr.Flags = 9;
		Assert.True(MuiListtreeGetNrMessageCodec.TryWrite(ref p, packet, getNr));
		Assert.True(MuiListtreeGetNrMessageCodec.TryRead(ref p, packet,
			out var getNrRead));
		Assert.Equal(getNr.TreeNode, getNrRead.TreeNode);
		Assert.Equal(getNr.Flags, getNrRead.Flags);

		var move = default(MuiListtreeMoveExchangeMessage);
		move.MethodId = MuiListtreeMessageCodec.Move;
		move.OldListNode = 0x2F60;
		move.OldTreeNode = 0x2F64;
		move.NewListNode = 0x2F68;
		move.NewTreeNode = 0x2F6C;
		move.Flags = 10;
		Assert.True(MuiListtreeMoveExchangeMessageCodec.TryWrite(ref p,
			packet, move));
		Assert.True(MuiListtreeMoveExchangeMessageCodec.TryRead(ref p, packet,
			out var moveRead));
		Assert.Equal(move.OldTreeNode, moveRead.OldTreeNode);
		Assert.Equal(move.NewTreeNode, moveRead.NewTreeNode);
		Assert.Equal(move.Flags, moveRead.Flags);

		var rename = default(MuiListtreeRenameMessage);
		rename.MethodId = MuiListtreeMessageCodec.Rename;
		rename.TreeNode = 0x2F70;
		rename.NewName = 0x2F74;
		rename.Flags = 11;
		Assert.True(MuiListtreeRenameMessageCodec.TryWrite(ref p, packet,
			rename));
		Assert.True(MuiListtreeRenameMessageCodec.TryRead(ref p, packet,
			out var renameRead));
		Assert.Equal(rename.TreeNode, renameRead.TreeNode);
		Assert.Equal(rename.NewName, renameRead.NewName);
		Assert.Equal(rename.Flags, renameRead.Flags);

		var findName = default(MuiListtreeFindNameMessage);
		findName.MethodId = MuiListtreeMessageCodec.FindName;
		findName.ListNode = 0x2F80;
		findName.Name = 0x2F84;
		findName.Flags = 12;
		Assert.True(MuiListtreeFindNameMessageCodec.TryWrite(ref p, packet,
			findName));
		Assert.True(MuiListtreeFindNameMessageCodec.TryRead(ref p, packet,
			out var findNameRead));
		Assert.Equal(findName.ListNode, findNameRead.ListNode);
		Assert.Equal(findName.Name, findNameRead.Name);
		Assert.Equal(findName.Flags, findNameRead.Flags);

		var dropMark = default(MuiListtreeDropMarkMessage);
		dropMark.MethodId = MuiListtreeMessageCodec.SetDropMark;
		dropMark.Entry = 13;
		dropMark.Values = 0x2F90;
		Assert.True(MuiListtreeDropMarkMessageCodec.TryWrite(ref p, packet,
			dropMark));
		Assert.True(MuiListtreeDropMarkMessageCodec.TryRead(ref p, packet,
			out var dropMarkRead));
		Assert.Equal(dropMark.Entry, dropMarkRead.Entry);
		Assert.Equal(dropMark.Values, dropMarkRead.Values);

		var testPos = default(MuiListtreeTestPosMessage);
		testPos.MethodId = MuiListtreeMessageCodec.TestPos;
		testPos.X = 14;
		testPos.Y = 15;
		testPos.Result = 0x2FA0;
		Assert.True(MuiListtreeTestPosMessageCodec.TryWrite(ref p, packet,
			testPos));
		Assert.True(MuiListtreeTestPosMessageCodec.TryRead(ref p, packet,
			out var testPosRead));
		Assert.Equal(testPos.X, testPosRead.X);
		Assert.Equal(testPos.Y, testPosRead.Y);
		Assert.Equal(testPos.Result, testPosRead.Result);
		Assert.False(MuiListtreeGetEntryMessageCodec.TryRead(ref p,
			APTR.FromPointer(0x200FFF), out _));
	}

	[Fact]
	public void ListtreeHookPoolRecordUsesNamedFieldsAndRejectsMalformedBoundary()
	{
		var p = new MuiHeadlessTestPlatform(0x1000, 0x80, 0x1080, State);
		var recordAddress = APTR.FromPointer(0x1020);
		var value = default(MuiListtreeCore.MuiListtreeHookPoolStateRecord);
		value.Magic = MuiListtreeCore.MuiListtreeHookPoolStateRecord.Cookie;
		value.Pool = APTR.FromPointer(0x1040);
		value.Requirements = 0;
		value.PuddleSize = 2008;
		value.ThresholdSize = 1024;
		value.Owned = 1;
		Assert.True(MuiListtreeCore.MuiListtreeHookPoolStateRecordCodec.Write(
			ref p, recordAddress, value));
		Assert.True(MuiListtreeCore.MuiListtreeHookPoolStateRecordCodec.TryRead(
			ref p, recordAddress, out var read));
		Assert.Equal(value.Pool, read.Pool);
		Assert.Equal(value.PuddleSize, read.PuddleSize);
		Assert.Equal(value.ThresholdSize, read.ThresholdSize);
		Assert.False(MuiListtreeCore.MuiListtreeHookPoolStateRecordCodec.TryRead(
			ref p, APTR.FromPointer(0x1069), out _));
		p.WriteUInt32(recordAddress, 0, 0);
		Assert.False(MuiListtreeCore.MuiListtreeHookPoolStateRecordCodec.TryRead(
			ref p, recordAddress, out _));
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
		Assert.True(MuiListtreeMessageCodec.TryReadMethodIdValue(ref p, packet,
			out var methodId));
		Assert.Equal(MuiListtreeMessageCodec.Insert, methodId);
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
		cursor.Field = MuiListtreeField.User;
		Assert.True(MuiListtreeFieldCursorCodec.TryGetAddress(ref p, cursor,
			out address));
		Assert.Equal(0x2D08u, address.Raw);
		cursor.Field = MuiListtreeField.ListNode;
		Assert.True(MuiListtreeFieldCursorCodec.TryGetAddress(ref p, cursor,
			out address));
		Assert.Equal(0x2D0Cu, address.Raw);
		cursor.Field = MuiListtreeField.PrevNode;
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
		cursor.Field = MuiListtreeField.TreeNode;
		Assert.False(MuiListtreeFieldCursorCodec.TryGetAddress(ref p, cursor,
			out _));
		cursor.Message = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Packet = MuiListtreePacketKind.TestPos;
		cursor.Field = MuiListtreeField.Result;
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
	public void DispatcherRoutesTypedSetGetAndTreeLifecycle()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var rootName = WriteString(ref platform, 0x2C00, "root");
		var childName = WriteString(ref platform, 0x2C20, "child");
		var packet = APTR.FromPointer(0x2D00);
		var storage = APTR.FromPointer(0x2D40);

		Assert.True(MuiListtreeMessageCodec.WriteSet(ref platform, packet,
			MuiListtreeMessageCodec.Set, MuiListtreeCore.DuplicateNodeName, 1));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeMessageCodec.WriteSet(ref platform, packet,
			MuiListtreeMessageCodec.NoNotifySet, MuiListtreeCore.DuplicateNodeName,
			1));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeMessageCodec.WriteGet(ref platform, packet,
			MuiListtreeCore.DuplicateNodeName, storage.Raw));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.Equal(1u, platform.ReadUInt32(storage, 0));

		Assert.True(MuiListtreeMessageCodec.WriteInsert(ref platform, packet,
			rootName.Raw, 0, ListRoot, PrevTail, 0));
		var root = APTR.FromPointer(MuiListtreeDispatcher.Dispatch(ref platform,
			State, tree, packet));
		Assert.NotEqual(APTR.Null, root);
		Assert.True(MuiListtreeMessageCodec.WriteInsert(ref platform, packet,
			childName.Raw, 0, root.Raw, PrevTail, 0));
		var child = APTR.FromPointer(MuiListtreeDispatcher.Dispatch(ref platform,
			State, tree, packet));
		Assert.NotEqual(APTR.Null, child);

		Assert.True(MuiListtreeMessageCodec.WriteOpenClose(ref platform, packet,
			MuiListtreeMessageCodec.Open, ListRoot, root.Raw, 0));
		Assert.Equal(1u, MuiListtreeDispatcher.Dispatch(ref platform, State, tree,
			packet));
		Assert.True(MuiListtreeMessageCodec.WriteGetNr(ref platform, packet,
			root.Raw, GetNrCountAll));
		Assert.Equal(2u, MuiListtreeDispatcher.Dispatch(ref platform, State, tree,
			packet));
		Assert.True(MuiListtreeMessageCodec.WriteGetEntry(ref platform, packet,
			root.Raw, 0, 0));
		Assert.Equal(child.Raw, MuiListtreeDispatcher.Dispatch(ref platform, State,
			tree, packet));
		Assert.True(MuiListtreeMessageCodec.WriteSort(ref platform, packet,
			MuiListtreeMessageCodec.Sort, ListRoot, 0));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.NotEqual(0u, MuiListtreeCore.NodeFlags(ref platform, root) &
			MuiListtreeCore.TNF_OPEN);
		Assert.True(MuiListtreeMessageCodec.WriteOpenClose(ref platform, packet,
			MuiListtreeMessageCodec.Close, ListRoot, root.Raw, 0));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		var renamed = WriteString(ref platform, 0x2D80, "renamed");
		Assert.True(MuiListtreeMessageCodec.WriteRename(ref platform, packet,
			root.Raw, renamed.Raw, 1u << 9));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeMessageCodec.WriteFindName(ref platform, packet,
			ListRoot, renamed.Raw, 0));
		Assert.Equal(root.Raw, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		var p2Name = WriteString(ref platform, 0x2DA0, "p2");
		Assert.True(MuiListtreeMessageCodec.WriteInsert(ref platform, packet,
			p2Name.Raw, 0, ListRoot, PrevTail, 0));
		var p2 = APTR.FromPointer(MuiListtreeDispatcher.DispatchTreePacket(
			ref platform, State, tree, packet));
		Assert.NotEqual(APTR.Null, p2);
		Assert.True(MuiListtreeMessageCodec.WriteMoveExchange(ref platform, packet,
			MuiListtreeMessageCodec.Move, root.Raw, child.Raw, p2.Raw,
			PrevTail, 0));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.Equal(p2.Raw, MuiListtreeCore.GetEntry(ref platform, State, tree,
			child, -5, 0).Raw);
		Assert.True(MuiListtreeMessageCodec.WriteMoveExchange(ref platform, packet,
			MuiListtreeMessageCodec.Exchange, ListRoot, root.Raw, ListRoot,
			p2.Raw, 0));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.Equal(p2.Raw, MuiListtreeCore.GetEntry(ref platform, State, tree,
			APTR.FromPointer(ListRoot), 0, 0).Raw);
		Assert.True(MuiListtreeMessageCodec.WriteDropMark(ref platform, packet,
			0, MuiListtreeCore.DropMarkBelow));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		var testPosResult = APTR.FromPointer(0x2DC0);
		Assert.True(MuiListtreeMessageCodec.WriteTestPos(ref platform, packet,
			4, 0, testPosResult.Raw));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeCore.MuiListtreeTestPosResultCodec.TryRead(
			ref platform, testPosResult, out var testPos));
		Assert.Equal(p2.Raw, testPos.TreeNode.Raw);
		Assert.Equal(0, testPos.ListEntry);
		Assert.Equal((ushort)MuiListtreeCore.DropMarkOnto, testPos.Flags);
		Assert.True(MuiListtreeMessageCodec.WriteRemove(ref platform, packet,
			ListRoot, root.Raw, 0));
		Assert.Equal(1u, MuiListtreeDispatcher.Dispatch(ref platform, State, tree,
			packet));

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void DispatcherRoutesListtreeLifecycleHooksThroughTypedPolicy()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var rootName = WriteString(ref platform, 0x2E00, "hook-root");
		var childName = WriteString(ref platform, 0x2E20, "hook-child");
		var packet = APTR.FromPointer(0x2E80);
		var hook = APTR.FromPointer(0x2EC0);
		var hookData = APTR.FromPointer(0x2F00);
		platform.WriteUInt32(hook, 8, MuiHeadlessTestPlatform.HookEntryConstruct);
		platform.WriteUInt32(hook, 16, hookData.Raw);

		Assert.True(MuiListtreeMessageCodec.WriteSet(ref platform, packet,
			MuiListtreeMessageCodec.Set, MuiListtreeCore.OpenHook, hook.Raw));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeMessageCodec.WriteSet(ref platform, packet,
			MuiListtreeMessageCodec.Set, MuiListtreeCore.CloseHook, hook.Raw));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeMessageCodec.WriteInsert(ref platform, packet,
			rootName.Raw, 0, ListRoot, PrevTail, 0));
		var root = APTR.FromPointer(MuiListtreeDispatcher.DispatchTreePacket(
			ref platform, State, tree, packet));
		Assert.NotEqual(APTR.Null, root);
		Assert.True(MuiListtreeMessageCodec.WriteInsert(ref platform, packet,
			childName.Raw, 0, root.Raw, PrevTail, 0));
		Assert.NotEqual(0u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));

		Assert.True(MuiListtreeMessageCodec.WriteOpenClose(ref platform, packet,
			MuiListtreeMessageCodec.Open, ListRoot, root.Raw, 0));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.Equal(hook, platform.LastHookBase);
		Assert.Equal(tree, platform.LastHookA2);
		Assert.Equal(root, platform.LastHookA1);
		Assert.Equal(hookData, platform.LastHookData);

		Assert.True(MuiListtreeMessageCodec.WriteOpenClose(ref platform, packet,
			MuiListtreeMessageCodec.Close, ListRoot, root.Raw, 0));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(platform.HookInvokeCount >= 2);
		Assert.Equal(hook, platform.LastHookBase);
		Assert.Equal(tree, platform.LastHookA2);
		Assert.Equal(root, platform.LastHookA1);

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void DispatcherRoutesListtreeConstructAndDestructHooksThroughTypedPolicy()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var rootName = WriteString(ref platform, 0x2F20, "construct-root");
		var user = WriteString(ref platform, 0x2F40, "construct-user");
		var packet = APTR.FromPointer(0x2F80);
		var constructHook = APTR.FromPointer(0x2FC0);
		var constructData = APTR.FromPointer(0x3000);
		var destructHook = APTR.FromPointer(0x3040);
		var destructData = APTR.FromPointer(0x3080);
		platform.WriteUInt32(constructHook, 8,
			MuiHeadlessTestPlatform.HookEntryConstruct);
		platform.WriteUInt32(constructHook, 16, constructData.Raw);
		platform.WriteUInt32(destructHook, 8,
			MuiHeadlessTestPlatform.HookEntryDestruct);
		platform.WriteUInt32(destructHook, 16, destructData.Raw);

		Assert.True(MuiListtreeMessageCodec.WriteSet(ref platform, packet,
			MuiListtreeMessageCodec.Set, MuiListtreeCore.ConstructHook,
			constructHook.Raw));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeMessageCodec.WriteSet(ref platform, packet,
			MuiListtreeMessageCodec.Set, MuiListtreeCore.DestructHook,
			destructHook.Raw));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));

		Assert.True(MuiListtreeMessageCodec.WriteInsert(ref platform, packet,
			rootName.Raw, user.Raw, ListRoot, PrevTail, 0));
		var root = APTR.FromPointer(MuiListtreeDispatcher.DispatchTreePacket(
			ref platform, State, tree, packet));
		Assert.NotEqual(APTR.Null, root);
		Assert.Equal(1u, platform.HookInvokeCount);
		Assert.Equal(constructHook, platform.LastHookBase);
		Assert.NotEqual(APTR.Null, platform.LastHookA2);
		var hookPool = platform.LastHookA2;
		Assert.Equal(1u, platform.PoolCreateCount);
		Assert.Equal(0u, platform.LastPoolRequirements);
		Assert.Equal(2008u, platform.LastPoolPuddleSize);
		Assert.Equal(1024u, platform.LastPoolThreshold);
		Assert.True(MuiListtreeCore.TryGetHookPoolStateRecord(ref platform, State,
			tree, out var hookPoolState));
		Assert.Equal(hookPool, hookPoolState.Pool);
		Assert.Equal(1u, hookPoolState.Owned);
		var pooledScratch = platform.AllocPooled(hookPool, 12);
		Assert.NotEqual(APTR.Null, pooledScratch);
		Assert.Equal(1u, platform.PooledAllocationCount);
		platform.FreePooled(hookPool, pooledScratch, 12);
		Assert.Equal(1u, platform.PooledFreeCount);
		Assert.NotEqual(user, platform.LastHookA1);
		Assert.Equal(platform.LastHookA1, platform.LastConstructMessage);
		Assert.True(platform.LastConstructMessageValid);
		Assert.Equal(rootName, platform.LastConstructName);
		Assert.Equal(user, platform.LastConstructUser);
		Assert.Equal(APTR.FromPointer(ListRoot), platform.LastConstructListNode);
		Assert.Equal(APTR.FromPointer(PrevTail), platform.LastConstructPrevNode);
		Assert.Equal(0u, platform.LastConstructFlags);
		Assert.Equal(constructData, platform.LastHookData);
		Assert.Equal(constructData.Raw, platform.ReadUInt32(root,
			MuiListtreeCore.TreeNodeUserOffset));

		Assert.True(MuiListtreeMessageCodec.WriteRemove(ref platform, packet,
			ListRoot, root.Raw, 0));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.Equal(2u, platform.HookInvokeCount);
		Assert.Equal(1u, platform.HookDestructCount);
		Assert.Equal(destructHook, platform.LastHookBase);
		Assert.Equal(hookPool, platform.LastHookA2);
		Assert.Equal(constructData, platform.LastHookA1);
		Assert.Equal(destructData, platform.LastHookData);

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
		Assert.Equal(1u, platform.PoolDeleteCount);
	}

	[Fact]
	public void DispatcherRoutesListtreeSortHookThroughTypedPolicy()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var firstName = WriteString(ref platform, 0x30C0, "zeta");
		var secondName = WriteString(ref platform, 0x30E0, "alpha");
		var packet = APTR.FromPointer(0x3120);
		var hook = APTR.FromPointer(0x3160);
		var hookData = APTR.FromPointer(0x31A0);
		platform.WriteUInt32(hook, 8, MuiHeadlessTestPlatform.HookEntryCompare);
		platform.WriteUInt32(hook, 16, hookData.Raw);

		Assert.True(MuiListtreeMessageCodec.WriteSet(ref platform, packet,
			MuiListtreeMessageCodec.Set, MuiListtreeCore.SortHook, hook.Raw));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeMessageCodec.WriteInsert(ref platform, packet,
			firstName.Raw, 0, ListRoot, PrevTail, 0));
		Assert.NotEqual(APTR.Null, APTR.FromPointer(
			MuiListtreeDispatcher.DispatchTreePacket(ref platform, State, tree,
				packet)));
		Assert.True(MuiListtreeMessageCodec.WriteInsert(ref platform, packet,
			secondName.Raw, 0, ListRoot, PrevTail, 0));
		Assert.NotEqual(APTR.Null, APTR.FromPointer(
			MuiListtreeDispatcher.DispatchTreePacket(ref platform, State, tree,
				packet)));

		Assert.True(MuiListtreeMessageCodec.WriteSort(ref platform, packet,
			MuiListtreeMessageCodec.Sort, ListRoot, 0));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(platform.HookInvokeCount > 0);
		Assert.Equal(hook, platform.LastHookBase);
		Assert.NotEqual(APTR.Null, platform.LastHookA2);
		Assert.NotEqual(APTR.Null, platform.LastHookA1);
		Assert.Equal(hookData, platform.LastHookData);

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void DispatcherRoutesListtreeDisplayHookThroughTypedDraw()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var rootName = WriteString(ref platform, 0x31E0, "display-root");
		var childName = WriteString(ref platform, 0x3200, "display-child");
		var format = WriteString(ref platform, 0x3220, "A,B");
		var replacement = WriteString(ref platform, 0x3230, "hook-column");
		var packet = APTR.FromPointer(0x3240);
		var hook = APTR.FromPointer(0x3280);
		var hookData = APTR.FromPointer(0x32C0);
		platform.DisplayHookReplacement = replacement;
		platform.WriteUInt32(hook, 8, MuiHeadlessTestPlatform.HookEntryDisplay);
		platform.WriteUInt32(hook, 16, hookData.Raw);

		Assert.True(MuiListtreeMessageCodec.WriteSet(ref platform, packet,
			MuiListtreeMessageCodec.Set, MuiListtreeCore.Format, format.Raw));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeMessageCodec.WriteSet(ref platform, packet,
			MuiListtreeMessageCodec.Set, MuiListtreeCore.TreeColumn, 1));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));

		Assert.True(MuiListtreeMessageCodec.WriteSet(ref platform, packet,
			MuiListtreeMessageCodec.Set, MuiListtreeCore.DisplayHook, hook.Raw));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeMessageCodec.WriteInsert(ref platform, packet,
			rootName.Raw, 0, ListRoot, PrevTail, 0));
		var root = APTR.FromPointer(MuiListtreeDispatcher.DispatchTreePacket(
			ref platform, State, tree, packet));
		Assert.NotEqual(APTR.Null, root);
		Assert.True(MuiListtreeMessageCodec.WriteInsert(ref platform, packet,
			childName.Raw, 0, root.Raw, PrevTail, 0));
		var child = APTR.FromPointer(MuiListtreeDispatcher.DispatchTreePacket(
			ref platform, State, tree, packet));
		Assert.NotEqual(APTR.Null, child);
		Assert.True(MuiListtreeMessageCodec.WriteOpenClose(ref platform, packet,
			MuiListtreeMessageCodec.Open, ListRoot, root.Raw, 0));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteDraw(ref platform,
			packet, 0));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(platform.HookInvokeCount >= 2);
		Assert.Equal(hook, platform.LastHookBase);
		Assert.Equal(platform.LastDisplayArray, platform.LastHookA2);
		Assert.Equal(child, platform.LastHookA1);
		Assert.Equal(child, platform.LastDisplayNode);
		Assert.Equal(APTR.Null, platform.LastDisplayFirstText);
		Assert.Equal(APTR.Null, platform.LastDisplaySecondText);
		Assert.Equal(replacement, platform.LastDisplaySecondTextAfterHook);
		Assert.Equal(2u, platform.DisplayHookCount);
		Assert.Equal(hookData, platform.LastHookData);
		Assert.True(MuiListtreeCore.TryGetDisplaySnapshotStateRecord(ref platform,
			State, tree, out var snapshot));
		Assert.Equal(MuiListtreeCore.MuiListtreeDisplaySnapshotState.Cookie,
			snapshot.Magic);
		Assert.Equal(child, snapshot.Node);
		Assert.Equal(2u, snapshot.Columns);
		Assert.NotEqual(APTR.Null, snapshot.Values);
		var snapshotCursor = default(MuiListtreeCore.MuiListtreeDisplayColumnCursor);
		snapshotCursor.Base = snapshot.Values;
		snapshotCursor.Index = 1;
		Assert.True(MuiListtreeCore.MuiListtreeDisplayColumnCursorCodec.TryGetEntry(ref platform,
			snapshotCursor, out var snapshotSlot));
		Assert.True(MuiListtreeCore.MuiListtreeDisplayColumnCodec.TryRead(ref platform,
			snapshotSlot, out var snapshotColumn));
		Assert.Equal(replacement, snapshotColumn.Text);

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void ListtreeTitleUsesBooleanDisplayHookRowAndTypedSnapshot()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var packet = APTR.FromPointer(0x32E0);
		var format = WriteString(ref platform, 0x3320, "A,B");
		var hook = APTR.FromPointer(0x3360);
		platform.WriteUInt32(hook, 8, MuiHeadlessTestPlatform.HookEntryDisplay);

		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Format, format.Raw, false));
		// MorphOS accepts this historically pointer-shaped input but stores TRUE.
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Title, 0x7FFFu, false));
		Assert.True(MuiListtreeCore.GetAttribute(ref platform, State, tree,
			MuiListtreeCore.Title, out var title));
		Assert.Equal(1u, title);
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.DisplayHook, hook.Raw, false));
		var root = InsertName(ref platform, tree, "title-row", ListRoot,
			PrevTail);
		Assert.NotEqual(APTR.Null, root);
		Assert.True(MuiListtreeCore.Layout(ref platform, State, tree, 0, 0,
			80, 16));
		var minMaxStorage = APTR.FromPointer(0x33C0);
		Assert.True(MuiListtreeCore.AskMinMax(ref platform, State, tree,
			minMaxStorage));
		Assert.True(MuiMinMaxRecordCodec.TryRead(ref platform, minMaxStorage,
			out var minMax));
		Assert.Equal((short)32, minMax.DefHeight);
		Assert.True(MuiListtreeCore.Draw(ref platform, State, tree, 0));
		// One row is available, so the title consumes it and the data node is not
		// sent to the hook in this pass. A title hook receives A1 == NULL.
		Assert.Equal(1u, platform.DisplayHookCount);
		Assert.Equal(APTR.Null, platform.LastDisplayNode);
		Assert.Equal(APTR.Null, platform.LastHookA1);
		Assert.True(MuiListtreeCore.TryGetDisplaySnapshotStateRecord(ref platform,
			State, tree, out var snapshot));
		Assert.Equal(APTR.Null, snapshot.Node);
		Assert.Equal(MuiListtreeCore.MuiListtreeDisplaySnapshotState.DisplayTitle,
			snapshot.DisplayFlags);

		var result = APTR.FromPointer(0x33A0);
		Assert.True(MuiListtreeCore.TestPos(ref platform, State, tree, 4, 4,
			result));
		Assert.True(MuiListtreeCore.MuiListtreeTestPosResultCodec.TryRead(
			ref platform, result, out var titlePosition));
		Assert.Equal(APTR.Null, titlePosition.TreeNode);
		Assert.Equal(-1, titlePosition.ListEntry);

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void ListtreeEmptyNodesProjectsIndicatorThroughTypedDisplaySnapshot()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var hook = APTR.FromPointer(0x34B0);
		platform.WriteUInt32(hook, 8, MuiHeadlessTestPlatform.HookEntryDisplay);

		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.DisplayHook, hook.Raw, false));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.EmptyNodes, 1, false));
		var root = InsertName(ref platform, tree, "empty-node", ListRoot, PrevTail);
		var child = InsertName(ref platform, tree, "temporary-child",
			(uint)root.Raw, PrevTail);
		Assert.NotEqual(APTR.Null, child);
		Assert.True(MuiListtreeCore.Remove(ref platform, State, tree,
			root, child, 0));

		Assert.True(MuiListtreeCore.Draw(ref platform, State, tree, 0));
		Assert.True(MuiListtreeCore.TryGetDisplaySnapshotStateRecord(ref platform,
			State, tree, out var emptySnapshot));
		Assert.Equal(MuiListtreeCore.MuiListtreeDisplaySnapshotState.DisplayListNode,
			emptySnapshot.DisplayFlags &
			MuiListtreeCore.MuiListtreeDisplaySnapshotState.DisplayListNode);
		Assert.Equal(0u, emptySnapshot.DisplayFlags &
			MuiListtreeCore.MuiListtreeDisplaySnapshotState.DisplayIndicator);

		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.EmptyNodes, 0, false));
		Assert.True(MuiListtreeCore.Draw(ref platform, State, tree, 0));
		Assert.True(MuiListtreeCore.TryGetDisplaySnapshotStateRecord(ref platform,
			State, tree, out var nodeSnapshot));
		Assert.NotEqual(0u, nodeSnapshot.DisplayFlags &
			MuiListtreeCore.MuiListtreeDisplaySnapshotState.DisplayIndicator);

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void ListtreeDrawHonorsNamedViewportOrigin()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var packet = APTR.FromPointer(0x32F0);
		var rootName = WriteString(ref platform, 0x3320, "draw-viewport-root");
		var childName = WriteString(ref platform, 0x3350, "draw-viewport-child");
		var hook = APTR.FromPointer(0x3380);
		var hookData = APTR.FromPointer(0x33C0);
		platform.WriteUInt32(hook, 8, MuiHeadlessTestPlatform.HookEntryConstruct);
		platform.WriteUInt32(hook, 16, hookData.Raw);

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteLayout(ref platform,
			packet, 0, 0, 80, 16));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeMessageCodec.WriteSet(ref platform, packet,
			MuiListtreeMessageCodec.Set, MuiListtreeCore.DisplayHook, hook.Raw));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeMessageCodec.WriteInsert(ref platform, packet,
			rootName.Raw, 0, ListRoot, PrevTail, 0));
		var root = APTR.FromPointer(MuiListtreeDispatcher.DispatchTreePacket(
			ref platform, State, tree, packet));
		Assert.NotEqual(APTR.Null, root);
		Assert.True(MuiListtreeMessageCodec.WriteInsert(ref platform, packet,
			childName.Raw, 0, root.Raw, PrevTail, 0));
		var child = APTR.FromPointer(MuiListtreeDispatcher.DispatchTreePacket(
			ref platform, State, tree, packet));
		Assert.NotEqual(APTR.Null, child);
		Assert.True(MuiListtreeMessageCodec.WriteOpenClose(ref platform, packet,
			MuiListtreeMessageCodec.Open, ListRoot, root.Raw, 0));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteDraw(ref platform,
			packet, 0));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.Equal(1u, platform.HookInvokeCount);
		Assert.Equal(root, platform.LastHookA1);

		Assert.True(MuiListtreeMessageCodec.WriteSet(ref platform, packet,
			MuiListtreeMessageCodec.Set, MuiListtreeCore.Active, child.Raw));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeCore.TryGetSurfaceStateRecord(ref platform, State,
			tree, out var surface));
		Assert.Equal(1u, surface.FirstVisible);
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteDraw(ref platform,
			packet, 0));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.Equal(2u, platform.HookInvokeCount);
		Assert.Equal(child, platform.LastHookA1);

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void ListtreeViewportTracksActiveRowAcrossTopologyMutations()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var first = InsertName(ref platform, tree, "viewport-first", ListRoot,
			PrevTail);
		_ = first;
		var middle = InsertName(ref platform, tree, "viewport-middle", ListRoot,
			PrevTail);
		var active = InsertName(ref platform, tree, "viewport-active", ListRoot,
			PrevTail);
		Assert.NotEqual(APTR.Null, middle);
		Assert.NotEqual(APTR.Null, active);
		Assert.True(MuiListtreeCore.Layout(ref platform, State, tree, 0, 0,
			80, 32));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Active, active.Raw, false));
		Assert.True(MuiListtreeCore.TryGetSurfaceStateRecord(ref platform, State,
			tree, out var beforeInsert));
		Assert.Equal(1u, beforeInsert.FirstVisible);

		var inserted = MuiListtreeCore.Insert(ref platform, State, tree,
			WriteUniqueString(ref platform, "viewport-inserted"), APTR.Null,
			APTR.FromPointer(ListRoot), active, InsertFlagsNextNode);
		Assert.NotEqual(APTR.Null, inserted);
		Assert.True(MuiListtreeCore.TryGetSurfaceStateRecord(ref platform, State,
			tree, out var afterInsert));
		Assert.Equal(2u, afterInsert.FirstVisible);

		Assert.True(MuiListtreeCore.Remove(ref platform, State, tree,
			APTR.FromPointer(ListRoot), inserted, 0));
		Assert.True(MuiListtreeCore.TryGetSurfaceStateRecord(ref platform, State,
			tree, out var afterRemove));
		Assert.Equal(1u, afterRemove.FirstVisible);

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void ListtreeActiveRejectsHiddenNodeUntilParentIsOpen()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var parent = InsertName(ref platform, tree, "active-parent", ListRoot,
			PrevTail);
		var child = InsertName(ref platform, tree, "hidden-child", parent.Raw,
			PrevTail);
		Assert.NotEqual(APTR.Null, child);
		Assert.False(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Active, child.Raw, false));
		Assert.Equal(APTR.Null, MuiListtreeCore.ActiveNode(ref platform, State,
			tree));

		Assert.True(MuiListtreeCore.Open(ref platform, State, tree,
			APTR.FromPointer(ListRoot), parent, 0));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Active, child.Raw, false));
		Assert.Equal(child, MuiListtreeCore.ActiveNode(ref platform, State, tree));

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void DispatcherRoutesListtreeLayoutAndAskMinMaxThroughTypedSurfacePackets()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var rootName = WriteString(ref platform, 0x32E0, "surface-root");
		var childName = WriteString(ref platform, 0x3300, "surface-child");
		var packet = APTR.FromPointer(0x3340);
		var storage = APTR.FromPointer(0x3360);

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteLayout(ref platform,
			packet, 4, 6, 120, 48));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeCore.TryGetSurfaceStateRecord(ref platform, State,
			tree, out var surface));
		Assert.Equal(4, surface.Left);
		Assert.Equal(6, surface.Top);
		Assert.Equal(120, surface.Width);
		Assert.Equal(48, surface.Height);
		Assert.Equal(16u, surface.RowHeight);

		Assert.True(MuiListtreeMessageCodec.WriteInsert(ref platform, packet,
			rootName.Raw, 0, ListRoot, PrevTail, 0));
		Assert.NotEqual(APTR.Null, APTR.FromPointer(
			MuiListtreeDispatcher.DispatchTreePacket(ref platform, State, tree,
				packet)));
		Assert.True(MuiListtreeMessageCodec.WriteInsert(ref platform, packet,
			childName.Raw, 0, ListRoot, PrevTail, 0));
		Assert.NotEqual(APTR.Null, APTR.FromPointer(
			MuiListtreeDispatcher.DispatchTreePacket(ref platform, State, tree,
				packet)));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteAskMinMax(ref platform,
			packet, storage.Raw));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiMinMaxRecordCodec.TryRead(ref platform, storage,
			out var minMax));
		Assert.Equal((short)1, minMax.MinWidth);
		Assert.Equal((short)16, minMax.MinHeight);
		Assert.Equal((short)32, minMax.DefHeight);

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void DispatcherRoutesListtreeAreaLifecycleThroughTypedPackets()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var packet = APTR.FromPointer(0x3410);
		var renderInfo = APTR.FromPointer(0x3440);
		var renderRecord = default(MuiDrawingRenderInfoRecord);
		Assert.True(MuiDrawingRenderInfoCodec.Write(ref platform, renderInfo,
			renderRecord));

		Assert.True(MuiExternalWrapperMessageCodec.WriteRenderInfo(ref platform,
			packet, MuiListtreeCore.MethodSetup, renderInfo.Raw));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeCore.TryGetLifecycleStateRecord(ref platform, State,
			tree, out var lifecycle));
		Assert.Equal(renderInfo, lifecycle.RenderInfo);
		Assert.Equal(1u, lifecycle.Setup);
		Assert.Equal(1u, lifecycle.Shown);

		platform.WriteUInt32(packet, 0, MuiListtreeCore.MethodHide);
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeCore.TryGetLifecycleStateRecord(ref platform, State,
			tree, out lifecycle));
		Assert.Equal(0u, lifecycle.Shown);
		platform.WriteUInt32(packet, 0, MuiListtreeCore.MethodShow);
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeCore.TryGetLifecycleStateRecord(ref platform, State,
			tree, out lifecycle));
		Assert.Equal(1u, lifecycle.Shown);
		platform.WriteUInt32(packet, 0, MuiListtreeCore.MethodCleanup);
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeCore.TryGetLifecycleStateRecord(ref platform, State,
			tree, out lifecycle));
		Assert.Equal(APTR.Null, lifecycle.RenderInfo);
		Assert.Equal(0u, lifecycle.Setup);
		Assert.Equal(0u, lifecycle.Shown);

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void DispatcherRoutesListtreeHandleInputRightAndLeftThroughTypedPacket()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var packet = APTR.FromPointer(0x34A0);
		var root = MuiListtreeCore.Insert(ref platform, State, tree,
			WriteString(ref platform, 0x34D0, "input-root"), APTR.Null,
			APTR.FromPointer(ListRoot), APTR.FromPointer(PrevTail), 0);
		Assert.NotEqual(APTR.Null, root);
		var child = MuiListtreeCore.Insert(ref platform, State, tree,
			WriteString(ref platform, 0x3500, "input-child"), APTR.Null,
			root, APTR.FromPointer(PrevTail), 0);
		Assert.NotEqual(APTR.Null, child);
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Active, root.Raw, false));

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0, 9)); // MUIKEY_RIGHT
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.NotEqual(0u, MuiListtreeCore.NodeFlags(ref platform, root) & TNF_OPEN);

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0, 8)); // MUIKEY_LEFT
		Assert.Equal(1u, MuiListtreeDispatcher.Dispatch(ref platform, State, tree,
			packet));
		Assert.Equal(0u, MuiListtreeCore.NodeFlags(ref platform, root) & TNF_OPEN);

		// Leaves and unsupported keys are left unclaimed, preserving the parent
		// Listview/input dispatcher contract.
		Assert.True(MuiListtreeCore.Open(ref platform, State, tree,
			APTR.FromPointer(ListRoot), root, 0));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Active, child.Raw, false));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0, 9));
		Assert.Equal(0u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void DispatcherRoutesListtreeVisibleKeyboardNavigationThroughNamedTreeState()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var packet = APTR.FromPointer(0x3580);
		var root = MuiListtreeCore.Insert(ref platform, State, tree,
			WriteString(ref platform, 0x35B0, "nav-root"), APTR.Null,
			APTR.FromPointer(ListRoot), APTR.FromPointer(PrevTail), 0);
		var child = MuiListtreeCore.Insert(ref platform, State, tree,
			WriteString(ref platform, 0x35E0, "nav-child"), APTR.Null, root,
			APTR.FromPointer(PrevTail), 0);
		Assert.NotEqual(APTR.Null, root);
		Assert.NotEqual(APTR.Null, child);
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Active, root.Raw, false));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteLayout(ref platform,
			packet, 0, 0, 80, 32));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0, 9)); // MUIKEY_RIGHT
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0, 3)); // MUIKEY_DOWN
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.Equal(child, MuiListtreeCore.ActiveNode(ref platform, State, tree));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0, 2)); // MUIKEY_UP
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.Equal(root, MuiListtreeCore.ActiveNode(ref platform, State, tree));

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0, 7)); // MUIKEY_BOTTOM
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.Equal(child, MuiListtreeCore.ActiveNode(ref platform, State, tree));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0, 6)); // MUIKEY_TOP
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.Equal(root, MuiListtreeCore.ActiveNode(ref platform, State, tree));

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0, 5)); // MUIKEY_PAGEDOWN; two visible rows fit one page
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.Equal(child, MuiListtreeCore.ActiveNode(ref platform, State, tree));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0, 4)); // MUIKEY_PAGEUP
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.Equal(root, MuiListtreeCore.ActiveNode(ref platform, State, tree));

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void ListtreeActiveMaintainsNamedViewportOriginForPointerAndTestPos()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var packet = APTR.FromPointer(0x3610);
		var intui = APTR.FromPointer(0x3640);
		var result = APTR.FromPointer(0x3670);
		var root = MuiListtreeCore.Insert(ref platform, State, tree,
			WriteString(ref platform, 0x36A0, "viewport-root"), APTR.Null,
			APTR.FromPointer(ListRoot), APTR.FromPointer(PrevTail), 0);
		var child = MuiListtreeCore.Insert(ref platform, State, tree,
			WriteString(ref platform, 0x36D0, "viewport-child"), APTR.Null, root,
			APTR.FromPointer(PrevTail), 0);
		Assert.NotEqual(APTR.Null, root);
		Assert.NotEqual(APTR.Null, child);
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteLayout(ref platform,
			packet, 0, 0, 80, 16));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeMessageCodec.WriteOpenClose(ref platform, packet,
			MuiListtreeMessageCodec.Open, ListRoot, root.Raw, 0));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeMessageCodec.WriteSet(ref platform, packet,
			MuiListtreeMessageCodec.Set, MuiListtreeCore.Active, child.Raw));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeCore.TryGetSurfaceStateRecord(ref platform, State,
			tree, out var surface));
		Assert.Equal(1u, surface.FirstVisible);

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0069, 0, 0, 4, 4));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, -1));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.Equal(child, MuiListtreeCore.ActiveNode(ref platform, State, tree));

		Assert.True(MuiListtreeMessageCodec.WriteTestPos(ref platform, packet,
			4, 0, result.Raw));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeCore.MuiListtreeTestPosResultCodec.TryRead(
			ref platform, result, out var testPos));
		Assert.Equal(child, testPos.TreeNode);

		Assert.True(MuiListtreeMessageCodec.WriteSet(ref platform, packet,
			MuiListtreeMessageCodec.Set, MuiListtreeCore.Active, root.Raw));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeCore.TryGetSurfaceStateRecord(ref platform, State,
			tree, out surface));
		Assert.Equal(0u, surface.FirstVisible);

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void ListtreeTestPosUsesLaidOutPixelBoundsAndDropFlags()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var result = APTR.FromPointer(0x3710);
		var root = InsertName(ref platform, tree, "testpos-root", ListRoot,
			PrevTail);
		Assert.NotEqual(APTR.Null, root);
		Assert.True(MuiListtreeCore.Layout(ref platform, State, tree, 0, 0,
			80, 16));

		Assert.True(MuiListtreeCore.TestPos(ref platform, State, tree, 4, 1,
			result));
		Assert.True(MuiListtreeCore.MuiListtreeTestPosResultCodec.TryRead(
			ref platform, result, out var above));
		Assert.Equal(root, above.TreeNode);
		Assert.Equal((ushort)MuiListtreeCore.DropMarkAbove, above.Flags);
		Assert.Equal(0, above.ListEntry);

		Assert.True(MuiListtreeCore.TestPos(ref platform, State, tree, 4, 15,
			result));
		Assert.True(MuiListtreeCore.MuiListtreeTestPosResultCodec.TryRead(
			ref platform, result, out var below));
		Assert.Equal(root, below.TreeNode);
		Assert.Equal((ushort)MuiListtreeCore.DropMarkBelow, below.Flags);

		// A laid-out TestPos outside the object rectangle must not resolve a
		// visible row merely because its Y coordinate is valid.
		Assert.True(MuiListtreeCore.TestPos(ref platform, State, tree, 80, 4,
			result));
		Assert.True(MuiListtreeCore.MuiListtreeTestPosResultCodec.TryRead(
			ref platform, result, out var outside));
		Assert.Equal(APTR.Null, outside.TreeNode);
		Assert.Equal((ushort)MuiListtreeCore.DropMarkNone, outside.Flags);
		Assert.Equal(-1, outside.ListEntry);

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void DispatcherRoutesListtreeSelectUpThroughNamedIntuiPointerRecord()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var packet = APTR.FromPointer(0x3540);
		var intui = APTR.FromPointer(0x3570);
		var root = MuiListtreeCore.Insert(ref platform, State, tree,
			WriteString(ref platform, 0x35A0, "pointer-root"), APTR.Null,
			APTR.FromPointer(ListRoot), APTR.FromPointer(PrevTail), 0);
		Assert.NotEqual(APTR.Null, root);
		Assert.NotEqual(APTR.Null, MuiListtreeCore.Insert(ref platform, State,
			tree, WriteString(ref platform, 0x35D0, "pointer-child"), APTR.Null,
			root, APTR.FromPointer(PrevTail), 0));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteLayout(ref platform,
			packet, 0, 0, 80, 32));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0069, 0, 0, 4, 4)); // IDCMP_MOUSEBUTTONS / SELECTUP
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, -1)); // MUIKEY_NONE
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.Equal(root, MuiListtreeCore.ActiveNode(ref platform, State, tree));

		// A pointer outside the named viewport is not consumed and cannot change
		// the active node.
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0069, 0, 0, 4, 40));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, -1));
		Assert.Equal(0u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.Equal(root, MuiListtreeCore.ActiveNode(ref platform, State, tree));

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void ListtreeTimestampedSelectUpHonoursDoubleClickPolicyThroughNamedState()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var intui = APTR.FromPointer(0x38A0);
		var root = InsertName(ref platform, tree, "double-root", ListRoot, PrevTail);
		var child = InsertName(ref platform, tree, "double-child",
			unchecked((uint)root.Raw), PrevTail);
		Assert.NotEqual(APTR.Null, root);
		Assert.NotEqual(APTR.Null, child);
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.DoubleClick, 0, false));
		Assert.True(MuiListtreeCore.Layout(ref platform, State, tree, 0, 0,
			80, 32));

		Assert.True(MuiIntuiMessageCodec.WritePointerWithTime(ref platform, intui,
			1u << 3, 0x0068, 0, 0, 4, 4, 20, 100000));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.True(MuiIntuiMessageCodec.WritePointerWithTime(ref platform, intui,
			1u << 3, 0x0069, 0, 0, 4, 4, 20, 100000));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.Equal(0u, MuiListtreeCore.NodeFlags(ref platform, root) & TNF_OPEN);
		Assert.True(MuiListtreeCore.TryGetClickStateRecord(ref platform, State,
			tree, out var first));
		Assert.Equal(1u, first.Clicks);
		Assert.Equal(0u, first.DoubleClick);
		Assert.Equal(1u, first.TimestampValid);

		Assert.True(MuiIntuiMessageCodec.WritePointerWithTime(ref platform, intui,
			1u << 3, 0x0068, 0, 0, 4, 4, 20, 450000));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.True(MuiIntuiMessageCodec.WritePointerWithTime(ref platform, intui,
			1u << 3, 0x0069, 0, 0, 4, 4, 20, 450000));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.NotEqual(0u, MuiListtreeCore.NodeFlags(ref platform, root) & TNF_OPEN);
		Assert.True(MuiListtreeCore.TryGetClickStateRecord(ref platform, State,
			tree, out var second));
		Assert.Equal(root, second.LastNode);
		Assert.Equal(2u, second.Clicks);
		Assert.Equal(1u, second.DoubleClick);
		Assert.Equal(20u, second.LastSeconds);
		Assert.Equal(450000u, second.LastMicros);

		// A third timestamped release starts the next pair rather than repeating
		// the same double-click edge.
		Assert.True(MuiIntuiMessageCodec.WritePointerWithTime(ref platform, intui,
			1u << 3, 0x0069, 0, 0, 4, 4, 20, 600000));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.True(MuiListtreeCore.TryGetClickStateRecord(ref platform, State,
			tree, out var restarted));
		Assert.Equal(1u, restarted.Clicks);
		Assert.Equal(0u, restarted.DoubleClick);

		// Reusing the prefix-only writer clears the optional timestamp, so the
		// next event starts a new click sequence rather than a third pair.
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0069, 0, 0, 4, 4));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.True(MuiListtreeCore.TryGetClickStateRecord(ref platform, State,
			tree, out var third));
		Assert.Equal(1u, third.Clicks);
		Assert.Equal(0u, third.DoubleClick);
		Assert.Equal(1u, third.TimestampValid);

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void ListtreeDoubleClickHonoursMorphosColumnSelectorsAndNamedHitState()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var intui = APTR.FromPointer(0x3A80);
		var format = WriteString(ref platform, 0x3AC0, "A,B");
		var root = InsertName(ref platform, tree, "column-root", ListRoot,
			PrevTail);
		Assert.NotEqual(APTR.Null, root);
		Assert.NotEqual(APTR.Null, InsertName(ref platform, tree, "column-child",
			root.Raw, PrevTail));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Format, format.Raw, false));
		Assert.True(MuiListtreeCore.Layout(ref platform, State, tree, 0, 0,
			80, 32));

		void Click(int x, uint seconds, uint micros)
		{
			Assert.True(MuiIntuiMessageCodec.WritePointerWithTime(ref platform,
				intui, 1u << 3, 0x0068, 0, 0, (short)x, 4, seconds, micros));
			Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
				intui, -1));
			Assert.True(MuiIntuiMessageCodec.WritePointerWithTime(ref platform,
				intui, 1u << 3, 0x0069, 0, 0, (short)x, 4, seconds, micros));
			Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
				intui, -1));
		}

		// Off rejects the pair even though the pointer hit is still classified.
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.DoubleClick,
			unchecked((uint)MuiListtreeCore.DoubleClickOff), false));
		Click(10, 1, 100000);
		Click(10, 1, 300000);
		Assert.Equal(0u, MuiListtreeCore.NodeFlags(ref platform, root) & TNF_OPEN);
		Assert.True(MuiListtreeCore.TryGetClickStateRecord(ref platform, State,
			tree, out var offState));
		Assert.Equal(0u, offState.DoubleClick);

		// All accepts a pair in either FORMAT column.
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.DoubleClick,
			unchecked((uint)MuiListtreeCore.DoubleClickAll), false));
		Click(50, 3, 100000);
		Click(50, 3, 300000);
		Assert.NotEqual(0u, MuiListtreeCore.NodeFlags(ref platform, root) & TNF_OPEN);

		// Even with All enabled, a different FORMAT column starts a fresh pair.
		Click(10, 4, 100000);
		Click(50, 4, 300000);
		Assert.NotEqual(0u, MuiListtreeCore.NodeFlags(ref platform, root) & TNF_OPEN);
		Assert.True(MuiListtreeCore.TryGetClickStateRecord(ref platform, State,
			tree, out var crossColumnState));
		Assert.Equal(0u, crossColumnState.DoubleClick);

		// A numeric selector recognizes only its named column.
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.DoubleClick, 1, false));
		Click(10, 5, 100000);
		Click(10, 5, 300000);
		Assert.NotEqual(0u, MuiListtreeCore.NodeFlags(ref platform, root) & TNF_OPEN);
		Click(50, 6, 100000);
		Click(50, 6, 300000);
		Assert.Equal(0u, MuiListtreeCore.NodeFlags(ref platform, root) & TNF_OPEN);

		// Tree selects the normalized TreeColumn (column one here).
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.TreeColumn, 1, false));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.DoubleClick,
			unchecked((uint)MuiListtreeCore.DoubleClickTree), false));
		Click(50, 7, 100000);
		Click(50, 7, 300000);
		Assert.NotEqual(0u, MuiListtreeCore.NodeFlags(ref platform, root) & TNF_OPEN);
		Assert.True(MuiListtreeCore.TryGetClickStateRecord(ref platform, State,
			tree, out var finalState));
		Assert.Equal(1u, finalState.DoubleClick);

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void ListtreeDoubleClickNotifiesLeafWithNamedNodeTrigger()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var intui = APTR.FromPointer(0x3C80);
		var follow = APTR.FromPointer(0x3CC0);
		var leaf = InsertName(ref platform, tree, "notify-leaf", ListRoot,
			PrevTail);
		Assert.NotEqual(APTR.Null, leaf);
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.DoubleClick, 0, false));
		Assert.True(MuiListtreeCore.Layout(ref platform, State, tree, 0, 0,
			80, 32));

		// The follow vector is a named two-slot message: method followed by the
		// MUI trigger parameter. Dispatch replaces the trigger slot with the
		// guest Listtree node pointer.
		platform.WriteUInt32(follow, 0, 0x90000077);
		platform.WriteUInt32(follow, 4, 1233727793u); // MUIV_EveryTime
		Assert.True(MuiNotifyCore.Add(ref platform, State, tree,
			MuiListtreeCore.DoubleClick, (uint)Value.EveryTime, tree, 2, follow));

		void Click(uint seconds, uint micros)
		{
			Assert.True(MuiIntuiMessageCodec.WritePointerWithTime(ref platform,
				intui, 1u << 3, 0x0068, 0, 0, 4, 4, seconds, micros));
			Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
				intui, -1));
			Assert.True(MuiIntuiMessageCodec.WritePointerWithTime(ref platform,
				intui, 1u << 3, 0x0069, 0, 0, 4, 4, seconds, micros));
			Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
				intui, -1));
		}

		var before = platform.DispatchCount;
		Click(40, 100000);
		Assert.Equal(before, platform.DispatchCount);
		Click(40, 300000);
		Assert.Equal(before + 1, platform.DispatchCount);
		Assert.Equal(tree, platform.LastDispatchObject);
		Assert.Equal(0x90000077u, platform.LastDispatchMethod);
		Assert.Equal(leaf.Raw, platform.LastDispatchArgument);
		Assert.True(MuiListtreeCore.TryGetClickStateRecord(ref platform, State,
			tree, out var clickState));
		Assert.Equal(1u, clickState.DoubleClick);
		Assert.Equal(0u, MuiListtreeCore.NodeFlags(ref platform, leaf) & TNF_OPEN);

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void ListtreeDoubleClickNotifiesUnselectedNodeColumn()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var intui = APTR.FromPointer(0x3D80);
		var format = WriteString(ref platform, 0x3DC0, "A,B");
		var follow = APTR.FromPointer(0x3E00);
		var root = InsertName(ref platform, tree, "notify-root", ListRoot,
			PrevTail);
		Assert.NotEqual(APTR.Null, root);
		Assert.NotEqual(APTR.Null, InsertName(ref platform, tree,
			"notify-child", root.Raw, PrevTail));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Format, format.Raw, false));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.DoubleClick, 0, false));
		Assert.True(MuiListtreeCore.Layout(ref platform, State, tree, 0, 0,
			80, 32));
		platform.WriteUInt32(follow, 0, 0x90000078);
		platform.WriteUInt32(follow, 4, 1233727793u); // MUIV_EveryTime
		Assert.True(MuiNotifyCore.Add(ref platform, State, tree,
			MuiListtreeCore.DoubleClick, (uint)Value.EveryTime, tree, 2, follow));

		void Click(uint seconds, uint micros)
		{
			Assert.True(MuiIntuiMessageCodec.WritePointerWithTime(ref platform,
				intui, 1u << 3, 0x0068, 0, 0, 50, 4, seconds, micros));
			Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
				intui, -1));
			Assert.True(MuiIntuiMessageCodec.WritePointerWithTime(ref platform,
				intui, 1u << 3, 0x0069, 0, 0, 50, 4, seconds, micros));
			Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
				intui, -1));
		}

		var before = platform.DispatchCount;
		Click(60, 100000);
		Click(60, 300000);
		Assert.Equal(before + 1, platform.DispatchCount);
		Assert.Equal(tree, platform.LastDispatchObject);
		Assert.Equal(0x90000078u, platform.LastDispatchMethod);
		Assert.Equal(root.Raw, platform.LastDispatchArgument);
		Assert.True(MuiListtreeCore.TryGetClickStateRecord(ref platform, State,
			tree, out var clickState));
		Assert.Equal(0u, clickState.DoubleClick);
		Assert.Equal(0u, MuiListtreeCore.NodeFlags(ref platform, root) & TNF_OPEN);

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void ListtreeFormatGeometryUsesNamedDeltaAndWeightForHitColumns()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var intui = APTR.FromPointer(0x3C20);
		var root = InsertName(ref platform, tree, "geometry-root", ListRoot,
			PrevTail);
		Assert.NotEqual(APTR.Null, root);
		// The first FORMAT column is 20 pixels of inter-column gap and has
		// weight 1; the second has weight 3.  In a 100-pixel surface this
		// produces 20/20/60 (column/gap/column), unlike equal-width fallback.
		var format = WriteString(ref platform, 0x3C60,
			"A DELTA=20 WEIGHT=1,B WEIGHT=3");
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Format, format.Raw, false));
		Assert.True(MuiListtreeCore.Layout(ref platform, State, tree, 0, 0,
			100, 16));

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0069, 0, 0, 10, 4));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.True(MuiListtreeCore.TryGetClickColumnStateRecord(ref platform,
			State, tree, out var first));
		Assert.Equal(0u, first.LastColumn);

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0069, 0, 0, 70, 4));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.True(MuiListtreeCore.TryGetClickColumnStateRecord(ref platform,
			State, tree, out var second));
		Assert.Equal(1u, second.LastColumn);

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void ListtreeFormatGeometryHonoursPixelMinAndMaxWidths()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var intui = APTR.FromPointer(0x3D20);
		var root = InsertName(ref platform, tree, "width-root", ListRoot,
			PrevTail);
		Assert.NotEqual(APTR.Null, root);
		Assert.True(MuiListtreeCore.Layout(ref platform, State, tree, 0, 0,
			100, 16));

		// MAXWIDTH=20px forces the first column to 20 pixels (plus the
		// default four-pixel gap), so x=30 resolves to column one.
		var maximum = WriteString(ref platform, 0x3D60,
			"A MAXWIDTH=20px WEIGHT=1,B WEIGHT=1");
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Format, maximum.Raw, false));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0069, 0, 0, 30, 4));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.True(MuiListtreeCore.TryGetClickColumnStateRecord(ref platform,
			State, tree, out var maxHit));
		Assert.Equal(1u, maxHit.LastColumn);

		// Without the optional px suffix, MorphOS FORMAT widths are percentages.
		// MAXWIDTH=25 therefore has the same boundary on this 100-pixel surface.
		var percentage = WriteString(ref platform, 0x3D80,
			"A MAXWIDTH=25 WEIGHT=1,B WEIGHT=1");
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Format, percentage.Raw, false));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0069, 0, 0, 30, 4));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.True(MuiListtreeCore.TryGetClickColumnStateRecord(ref platform,
			State, tree, out var percentageHit));
		Assert.Equal(1u, percentageHit.LastColumn);

		// MINWIDTH=70px expands the first column to 70 pixels, so x=60
		// remains in column zero even though proportional allocation would
		// otherwise have placed the boundary near the middle.
		var minimum = WriteString(ref platform, 0x3DA0,
			"A MINWIDTH=70px WEIGHT=1,B WEIGHT=1");
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Format, minimum.Raw, false));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0069, 0, 0, 60, 4));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.True(MuiListtreeCore.TryGetClickColumnStateRecord(ref platform,
			State, tree, out var minHit));
		Assert.Equal(0u, minHit.LastColumn);

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void ListtreePointerDragTracksNamedCaptureAndDropMark()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var packet = APTR.FromPointer(0x35E0);
		var intui = APTR.FromPointer(0x3610);
		var root = InsertName(ref platform, tree, "drag-root", ListRoot, PrevTail);
		var child = InsertName(ref platform, tree, "drag-child",
			(uint)root.Raw, PrevTail);
		Assert.True(MuiListtreeCore.Open(ref platform, State, tree,
			APTR.FromPointer(ListRoot), root, 0));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteLayout(ref platform,
			packet, 0, 0, 80, 32));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0068, 0, 0, 4, 4)); // SELECTDOWN on root
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.Equal(root, MuiListtreeCore.ActiveNode(ref platform, State, tree));
		Assert.True(MuiListtreeCore.TryGetDragStateRecord(ref platform, State,
			tree, out var drag));
		Assert.Equal(0, drag.Source);
		Assert.Equal(0, drag.Target);
		Assert.NotEqual(0u, drag.Flags & MuiListviewDragState.ActiveFlag);

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 4, 0, 0, 0, 4, 30)); // MOUSEMOVE near child bottom edge
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.True(MuiListtreeCore.TryGetDragStateRecord(ref platform, State,
			tree, out drag));
		Assert.Equal(1, drag.Target);
		Assert.NotEqual(0u, drag.Flags & MuiListviewDragState.MovedFlag);
		Assert.True(MuiListtreeCore.TryGetHeaderStateRecord(ref platform, State,
			tree, out var header));
		Assert.Equal(1, header.DropEntry);
		Assert.Equal(MuiListtreeCore.DropMarkBelow, header.DropValue);

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0069, 0, 0, 4, 30)); // SELECTUP closes capture
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.True(MuiListtreeCore.TryGetDragStateRecord(ref platform, State,
			tree, out drag));
		Assert.Equal(0u, drag.Flags);
		Assert.True(MuiListtreeCore.TryGetHeaderStateRecord(ref platform, State,
			tree, out header));
		Assert.Equal(-1, header.DropEntry);
		Assert.Equal(MuiListtreeCore.DropMarkNone, header.DropValue);
		Assert.Equal(root, MuiListtreeCore.ActiveNode(ref platform, State, tree));

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void ListtreePointerDragCommitsBelowAboveAndOntoUsingTypedMove()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var packet = APTR.FromPointer(0x3660);
		var intui = APTR.FromPointer(0x3690);
		var a = InsertName(ref platform, tree, "a", ListRoot, PrevTail);
		var b = InsertName(ref platform, tree, "b", ListRoot, PrevTail);
		var c = InsertName(ref platform, tree, "c", ListRoot, PrevTail);
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteLayout(ref platform,
			packet, 0, 0, 80, 48));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));

		// Move a below c: [a,b,c] -> [b,c,a].
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0068, 0, 0, 4, 4));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 4, 0, 0, 0, 4, 47));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0069, 0, 0, 4, 47));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.Equal("b", NodeName(ref platform, GetEntry(ref platform, tree,
			ListRoot, 0, 0)));
		Assert.Equal("c", NodeName(ref platform, GetEntry(ref platform, tree,
			ListRoot, 1, 0)));
		Assert.Equal("a", NodeName(ref platform, GetEntry(ref platform, tree,
			ListRoot, 2, 0)));

		// Move a above b: [b,c,a] -> [a,b,c].
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0068, 0, 0, 4, 47));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 4, 0, 0, 0, 4, 0));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0069, 0, 0, 4, 0));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.Equal(a.Raw, MuiListtreeCore.GetEntry(ref platform, State, tree,
			APTR.FromPointer(ListRoot), 0, 0).Raw);
		Assert.Equal(b.Raw, MuiListtreeCore.GetEntry(ref platform, State, tree,
			APTR.FromPointer(ListRoot), 1, 0).Raw);

		// Move c onto b: [a,b,c] -> [a,b(c)].
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0068, 0, 0, 4, 40));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 4, 0, 0, 0, 4, 24));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0069, 0, 0, 4, 24));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.Equal(2u, MuiListtreeCore.RootCount(ref platform, State, tree));
		Assert.Equal(1u, MuiListtreeCore.ChildCount(ref platform, b));
		Assert.Equal(c.Raw, MuiListtreeCore.GetEntry(ref platform, State, tree,
			b, 0, 0).Raw);

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void ListtreePointerSelectionUsesTypedNodeStateAndQualifiers()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var packet = APTR.FromPointer(0x36C0);
		var intui = APTR.FromPointer(0x3700);
		var a = InsertName(ref platform, tree, "a", ListRoot, PrevTail);
		var b = InsertName(ref platform, tree, "b", ListRoot, PrevTail);
		var c = InsertName(ref platform, tree, "c", ListRoot, PrevTail);
		var d = InsertName(ref platform, tree, "d", ListRoot, PrevTail);
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.DragDropSort, 0, false));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.MultiSelect, 1, false));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteLayout(ref platform,
			packet, 0, 0, 80, 64));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));

		// Plain click is exclusive.
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0069, 0, 0, 4, 4));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.True(MuiListtreeCore.IsNodeSelected(ref platform, a));
		Assert.Equal(1u, MuiListtreeCore.SelectedCount(ref platform, State, tree));

		// Control extends the selection without losing the first entry.
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0069, 0x0008, 0, 4, 20));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.True(MuiListtreeCore.IsNodeSelected(ref platform, a));
		Assert.True(MuiListtreeCore.IsNodeSelected(ref platform, b));
		Assert.Equal(2u, MuiListtreeCore.SelectedCount(ref platform, State, tree));

		// Control again toggles the second entry off.
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0069, 0x0008, 0, 4, 20));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.False(MuiListtreeCore.IsNodeSelected(ref platform, b));
		Assert.Equal(1u, MuiListtreeCore.SelectedCount(ref platform, State, tree));

		// Shift extends from the typed anchor (the last control-click) to d.
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			1u << 3, 0x0069, 0x0003, 0, 4, 52));
		Assert.True(MuiListtreeCore.HandleInput(ref platform, State, tree,
			intui, -1));
		Assert.False(MuiListtreeCore.IsNodeSelected(ref platform, a));
		Assert.True(MuiListtreeCore.IsNodeSelected(ref platform, b));
		Assert.True(MuiListtreeCore.IsNodeSelected(ref platform, c));
		Assert.True(MuiListtreeCore.IsNodeSelected(ref platform, d));
		Assert.Equal(3u, MuiListtreeCore.SelectedCount(ref platform, State, tree));

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
	}

	[Fact]
	public void DispatcherRoutesListtreeKeyboardSelectionThroughTypedNodeState()
	{
		var platform = CreatePlatform(out var listtreeClass);
		var tree = Create(ref platform, listtreeClass);
		var packet = APTR.FromPointer(0x3730);
		var a = InsertName(ref platform, tree, "key-a", ListRoot, PrevTail);
		var b = InsertName(ref platform, tree, "key-b", ListRoot, PrevTail);
		var c = InsertName(ref platform, tree, "key-c", ListRoot, PrevTail);
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.MultiSelect, 1, false));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Active, a.Raw, false));

		// MUIKEY_PRESS selects the active node exclusively.
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0, 0));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeCore.IsNodeSelected(ref platform, a));
		Assert.Equal(1u, MuiListtreeCore.SelectedCount(ref platform, State, tree));

		// MUIKEY_TOGGLE adds and then removes the active node without disturbing
		// the typed anchor/selection state of the other node.
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Active, b.Raw, false));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0, 1));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeCore.IsNodeSelected(ref platform, a));
		Assert.True(MuiListtreeCore.IsNodeSelected(ref platform, b));
		Assert.Equal(2u, MuiListtreeCore.SelectedCount(ref platform, State, tree));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0, 1));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.False(MuiListtreeCore.IsNodeSelected(ref platform, b));
		Assert.Equal(1u, MuiListtreeCore.SelectedCount(ref platform, State, tree));

		// PRESS remains exclusive after the active node changes.
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Active, c.Raw, false));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0, 0));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.False(MuiListtreeCore.IsNodeSelected(ref platform, a));
		Assert.True(MuiListtreeCore.IsNodeSelected(ref platform, c));
		Assert.Equal(1u, MuiListtreeCore.SelectedCount(ref platform, State, tree));

		// Toggle falls back to exclusive selection when MultiSelect is disabled,
		// matching the inherited Listview policy.
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.MultiSelect, 0, false));
		Assert.True(MuiListtreeCore.SetAttribute(ref platform, State, tree,
			MuiListtreeCore.Active, a.Raw, false));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0, 1));
		Assert.Equal(1u, MuiListtreeDispatcher.DispatchTreePacket(ref platform,
			State, tree, packet));
		Assert.True(MuiListtreeCore.IsNodeSelected(ref platform, a));
		Assert.False(MuiListtreeCore.IsNodeSelected(ref platform, c));
		Assert.Equal(1u, MuiListtreeCore.SelectedCount(ref platform, State, tree));

		DisposeAndAssertBalanced(ref platform, tree, listtreeClass);
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
