using System.Text;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

// Focused MG08 coverage for Listview.mui and Floattext.mui built on the
// verified MuiListCore backbone. Exercises class-aware construction, failure-
// atomic composite ownership, attribute/method forwarding, group layout/draw/
// input, and deterministic Floattext parsing/append with balanced allocations.
public sealed class MuiListviewFloattextTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	// Attribute / selector identifiers used by the tests.
	private const uint ListActive = 0x8042391cu;
	private const uint ListEntries = 0x80421654u;
	private const uint ListFirst = 0x804238d4u;
	private const uint ListVisible = 0x8042191fu;
	private const uint ListTopPixel = 0x80429df3u;
	private const uint ListTotalPixel = 0x8042a8f5u;
	private const uint ListSelectChange = 0x8042178fu;
	private const uint EveryTime = 1233727793;
	private const uint ListSortColumn = 0x8042cafbu;
	private const uint ListFormat = 0x80423c0au;
	private const uint ListTitle = 0x80423e66u;
	private const uint ListTitleArray = 0x80427d95u;
	private const uint ListTitleClick = 0x80422fd9u;
	private const uint ListDragSortable = 0x80426099u;
	private const uint ListDragType = 0x80425cd3u;
	private const uint ListDropMark = 0x8042aba6u;
	private const uint LvList = 0x8042bcceu;      // MUIA_Listview_List
	private const uint LvInput = 0x8042682du;     // MUIA_Listview_Input
	private const uint LvMultiSelect = 0x80427e08u;
	private const uint LvScrollerPos = 0x8042b1b4u;
	private const uint LvDragType = 0x80425cd3u;
	private const uint LvDoubleClick = 0x80424635u;
	private const uint LvAgainClick = 0x804214c2u;
	private const uint LvClickColumn = 0x8042d1b3u;
	private const uint LvDefClickColumn = 0x8042b296u;
	private const uint ListEditable = 0x8042f9b9u;
	private const uint FtText = 0x8042d16au;
	private const uint FtJustify = 0x8042dc03u;
	private const uint FtSkipChars = 0x80425c7du;
	private const uint FtTabSize = 0x80427d17u;
	private const uint FtAppend = 0x8042a221u;
	private const uint ListCreateImage = 0x80429804u;
	private const uint ListDeleteImage = 0x80420f58u;
	private const uint Width = 0x8042B59Cu;
	private const uint Height = 0x80423237u;
	private const uint LeftEdge = 0x8042BEC6u;
	private const int InsertBottom = -3;
	private const uint ScrollerPosNone = 3;
	private const uint MultiSelectShifted = 2;
	private const uint MultiSelectAlways = 3;
	private const uint ListMultiTestHook = 0x8042c2c6u;
	private const int SelectAll = -2;
	private const uint SelectOff = 0;
	private const uint SelectOn = 1;
	private const uint SelectAsk = 3;
	private const int KeyUp = 2;
	private const int KeyDown = 3;
	private const int KeyPageDown = 5;
	private const int KeyTop = 6;
	private const int KeyBottom = 7;
	private const int KeyLeft = 8;
	private const int KeyRight = 9;
	private const int KeyPress = 0;
	private const int KeyToggle = 1;
	private const int KeyNone = -1;
	private const int KeyRelease = -2;
	private const uint IdcmpMouseButtons = 1u << 3;
	private const ushort SelectDown = 0x0068;
	private const ushort SelectUp = 0x0069;
	private const ushort WheelUp = 0x007A;
	private const ushort WheelDown = 0x007B;
	private const ushort WheelLeft = 0x007C;
	private const ushort WheelRight = 0x007D;
	private const ushort ShiftQualifier = 0x0001;
	private const uint IdcmpMouseMove = 1u << 2;

	// ------------------------------------------------------------------ common

	[Fact]
	public void ClassifierIdentifiesListListviewAndFloattext()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out var floattextClass, out _, 0x80000);
		Assert.Equal(MuiCollectionClass.List, MuiListCore.ClassifyRecord(
			ref platform, listClass));
		Assert.Equal(MuiCollectionClass.Listview, MuiListCore.ClassifyRecord(
			ref platform, listviewClass));
		Assert.Equal(MuiCollectionClass.Floattext, MuiListCore.ClassifyRecord(
			ref platform, floattextClass));
	}

	// ----------------------------------------------------------------- Listview

	[Fact]
	public void ListviewClickStateCodecUsesNamedFields()
	{
		var platform = CreatePlatform(out _, out _, out _, out _, 0x80000);
		var address = APTR.FromPointer(0x7800);
		var expected = default(MuiListviewCore.MuiListviewClickState);
		expected.Magic = MuiListviewCore.MuiListviewClickState.Cookie;
		expected.ClickColumn = 3;
		expected.DoubleClick = 1;
		expected.AgainClick = 0;
		expected.Clicks = 2;
		expected.DefClickColumn = 6;
		Assert.True(MuiListviewCore.MuiListviewClickStateCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiListviewCore.MuiListviewClickStateCodec.TryRead(
			ref platform, address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.ClickColumn, actual.ClickColumn);
		Assert.Equal(1u, actual.DoubleClick);
		Assert.Equal(0u, actual.AgainClick);
		Assert.Equal(expected.Clicks, actual.Clicks);
		Assert.Equal(expected.DefClickColumn, actual.DefClickColumn);
		Assert.False(MuiListviewCore.MuiListviewClickStateCodec.TryRead(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void ListviewDefaultClickColumnUsesNamedClickState()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out var floattextClass, out var otherClass, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		Assert.True(MuiListviewCore.TryGetClickState(ref platform, State,
			listview, out var clickState));
		Assert.Equal(0u, clickState.DefClickColumn);
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvDefClickColumn, 5, false));
		Assert.True(MuiListviewCore.TryGetClickState(ref platform, State,
			listview, out clickState));
		Assert.Equal(5u, clickState.DefClickColumn);
		Assert.True(MuiListviewCore.GetAttribute(ref platform, State, listview,
			LvDefClickColumn, out var exposed));
		Assert.Equal(5u, exposed);
		DisposeListview(ref platform, listview, listClass, listviewClass,
			floattextClass, otherClass);
	}

	[Fact]
	public void ListviewClickStateCursorUsesNamedRecordBoundary()
	{
		var platform = CreatePlatform(out _, out _, out _, out _, 0x80000);
		var record = APTR.FromPointer(0x7900);
		var cursor = new MuiListviewCore.MuiListviewClickStateFieldCursor
		{
			Record = record,
			Field = MuiListviewCore.MuiListviewClickStateField.Clicks,
		};
		Assert.True(MuiListviewCore.MuiListviewClickStateFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x7910), address);
		Assert.True(MuiListviewCore.MuiListviewClickStateFieldCursorCodec
			.TryWriteUInt32(ref platform, record,
				MuiListviewCore.MuiListviewClickStateField.Clicks, 4));
		Assert.True(MuiListviewCore.MuiListviewClickStateFieldCursorCodec
			.TryReadUInt32(ref platform, record,
				MuiListviewCore.MuiListviewClickStateField.Clicks, out var clicks));
		Assert.Equal(4u, clicks);
		Assert.False(MuiListviewCore.MuiListviewClickStateFieldCursorCodec
			.TryReadUInt32(ref platform, record,
				unchecked((MuiListviewCore.MuiListviewClickStateField)255), out _));
		Assert.False(MuiListviewCore.MuiListviewClickStateFieldCursorCodec
			.TryReadUInt32(ref platform, APTR.FromPointer(0xFFFFFFF0u),
			MuiListviewCore.MuiListviewClickStateField.Clicks, out _));
	}

	[Fact]
	public void ListviewInteractionPolicyCodecUsesNamedFields()
	{
		var platform = CreatePlatform(out _, out _, out _, out _, 0x80000);
		var address = APTR.FromPointer(0x7A00);
		var expected = default(MuiListviewCore.MuiListviewInteractionPolicyState);
		expected.Magic =
			MuiListviewCore.MuiListviewInteractionPolicyState.Cookie;
		expected.Input = 1;
		expected.MultiSelect = MultiSelectAlways;
		expected.ScrollerPos = ScrollerPosNone;
		expected.DragType = 1;
		Assert.True(MuiListviewCore.MuiListviewInteractionPolicyStateCodec.Write(
			ref platform, address, expected));
		Assert.True(MuiListviewCore.MuiListviewInteractionPolicyStateCodec.TryRead(
			ref platform, address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Input, actual.Input);
		Assert.Equal(expected.MultiSelect, actual.MultiSelect);
		Assert.Equal(expected.ScrollerPos, actual.ScrollerPos);
		Assert.Equal(expected.DragType, actual.DragType);

		var cursor = default(
			MuiListviewCore.MuiListviewInteractionPolicyFieldCursor);
		cursor.Record = address;
		cursor.Field =
			MuiListviewCore.MuiListviewInteractionPolicyField.DragType;
		Assert.True(MuiListviewCore.MuiListviewInteractionPolicyFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress));
		Assert.Equal(APTR.FromPointer(0x7A10), fieldAddress);
		Assert.False(MuiListviewCore.MuiListviewInteractionPolicyFieldCursorCodec
			.TryReadUInt32(ref platform, address, unchecked((
				MuiListviewCore.MuiListviewInteractionPolicyField)255), out _));
	}

	[Fact]
	public void ListviewSelectionSignalCodecUsesNamedFields()
	{
		var platform = CreatePlatform(out _, out _, out _, out _, 0x80000);
		var address = APTR.FromPointer(0x7B00);
		var expected = default(MuiListviewCore.MuiListviewSelectionSignalState);
		expected.Magic =
			MuiListviewCore.MuiListviewSelectionSignalState.Cookie;
		expected.Value = 1;
		Assert.True(MuiListviewCore.MuiListviewSelectionSignalStateCodec.Write(
			ref platform, address, expected));
		Assert.True(MuiListviewCore.MuiListviewSelectionSignalStateCodec.TryRead(
			ref platform, address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(1u, actual.Value);

		var cursor = default(
			MuiListviewCore.MuiListviewSelectionSignalFieldCursor);
		cursor.Record = address;
		cursor.Field =
			MuiListviewCore.MuiListviewSelectionSignalField.Value;
		Assert.True(MuiListviewCore.MuiListviewSelectionSignalFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress));
		Assert.Equal(APTR.FromPointer(0x7B04), fieldAddress);
		Assert.False(MuiListviewCore.MuiListviewSelectionSignalFieldCursorCodec
			.TryReadUInt32(ref platform, address, unchecked((
				MuiListviewCore.MuiListviewSelectionSignalField)255), out _));
	}

	[Fact]
	public void ListviewChildStateCodecUsesNamedFields()
	{
		var platform = CreatePlatform(out _, out _, out _, out _, 0x80000);
		var address = APTR.FromPointer(0x7C00);
		var child = APTR.FromPointer(0x4400);
		var expected = default(MuiListviewCore.MuiListviewChildState);
		expected.Magic = MuiListviewCore.MuiListviewChildState.Cookie;
		expected.Child = child;
		Assert.True(MuiListviewCore.MuiListviewChildStateCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiListviewCore.MuiListviewChildStateCodec.TryRead(
			ref platform, address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(child, actual.Child);

		var cursor = default(
			MuiListviewCore.MuiListviewChildStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiListviewCore.MuiListviewChildStateField.Child;
		Assert.True(MuiListviewCore.MuiListviewChildStateFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress));
		Assert.Equal(APTR.FromPointer(0x7C04), fieldAddress);
		Assert.False(MuiListviewCore.MuiListviewChildStateFieldCursorCodec
			.TryReadUInt32(ref platform, address, unchecked((
				MuiListviewCore.MuiListviewChildStateField)255), out _));
	}

	[Fact]
	public void ListviewLayoutUsesNamedGuestRecord()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out var floattextClass, out var otherClass, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 4, 6,
			96, 64));
		Assert.True(MuiListviewCore.TryGetLayoutState(ref platform, State, listview,
			out var layout));
		Assert.Equal(MuiListviewCore.MuiListviewLayoutState.Cookie,
			layout.Magic);
		Assert.Equal(4, layout.Left);
		Assert.Equal(6, layout.Top);
		Assert.Equal(96, layout.Width);
		Assert.Equal(64, layout.Height);
		Assert.True(layout.ChildWidth > 0);
		Assert.True(layout.ChildHeight > 0);

		// A generic raw geometry write does not replace the canonical composite
		// record; a subsequent Listview layout republishes the new rectangle.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, listview,
			Width, 12, false));
		Assert.True(MuiListviewCore.TryGetLayoutState(ref platform, State, listview,
			out layout));
		Assert.Equal(96, layout.Width);
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 4, 6,
			80, 64));
		Assert.True(MuiListviewCore.TryGetLayoutState(ref platform, State, listview,
			out layout));
		Assert.Equal(80, layout.Width);
		DisposeListview(ref platform, listview, listClass, listviewClass,
			floattextClass, otherClass);
	}

	[Fact]
	public void ListviewScrollerGeometryReconcilesChildAreaRecord()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out var floattextClass, out var otherClass, 0x88000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		// Publish only the child's shared Area geometry.  The List viewport
		// record is intentionally absent so GetScrollerState exercises its
		// geometry fallback rather than the normal composite layout record.
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, child, 0, 0,
			64, 16));
		Assert.True(MuiAreaLayoutCore.TryGetGeometryStateRecord(ref platform,
			State, child, out var initialGeometry));
		Assert.Equal(16, initialGeometry.Height);

		// Force the Listview's zero-visible fallback path.  The public child
		// height is changed directly, so GetScrollerState must cross the shared
		// Area geometry boundary and reconcile the named record before deriving
		// its visible row count.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, child,
			Height, 8, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, child,
			ListVisible, 0, false));
		Assert.True(MuiListviewCore.GetScrollerState(ref platform, State, listview,
			out _, out var visible, out _, out _));
		Assert.Equal(1u, visible);
		Assert.True(MuiAreaLayoutCore.TryGetGeometryStateRecord(ref platform,
			State, child, out var reconciledGeometry));
		Assert.Equal(8, reconciledGeometry.Height);

		DisposeListview(ref platform, listview, listClass, listviewClass,
			floattextClass, otherClass);
	}

	[Fact]
	public void ListviewScrollerUsesNamedGuestRecord()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out var floattextClass, out var otherClass, 0x86000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		for (var i = 0u; i < 4; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
				APTR.FromPointer(0x86000 + i * 0x40), InsertBottom));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			64, 16));
		Assert.True(MuiListviewCore.TryGetScrollerState(ref platform, State,
			listview, out var record));
		Assert.Equal(MuiListviewCore.MuiListviewScrollerState.Cookie,
			record.Magic);
		Assert.Equal(4u, record.Entries);
		Assert.True(record.Visible > 0);
		Assert.Equal(0u, record.First);
		Assert.Equal(record.Entries > record.Visible ?
			record.Entries - record.Visible : 0u, record.MaxFirst);

		Assert.True(MuiListviewCore.SetScrollerFirst(ref platform, State,
			listview, 99));
		Assert.True(MuiListviewCore.TryGetScrollerState(ref platform, State,
			listview, out record));
		Assert.Equal(record.MaxFirst, record.First);
		DisposeListview(ref platform, listview, listClass, listviewClass,
			floattextClass, otherClass);
	}

	[Fact]
	public void ListviewAdoptsSuppliedListExposesItAndAppliesDefaults()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var list = MuiListCore.CreateList(ref platform, State, listClass, APTR.Null);
		var tags = APTR.FromPointer(0x3000);
		platform.WriteUInt32(tags, 0, LvList);
		platform.WriteUInt32(tags, 4, list.Raw);
		platform.WriteUInt32(tags, 8, 0);
		var listview = MuiListviewCore.CreateListview(ref platform, State,
			listviewClass, tags);
		Assert.NotEqual(APTR.Null, listview);
		Assert.Equal(list, MuiListviewCore.ChildList(ref platform, State, listview));
		Assert.True(MuiListviewCore.TryGetChildState(ref platform, State,
			listview, out var childState));
		Assert.Equal(MuiListviewCore.MuiListviewChildState.Cookie,
			childState.Magic);
		Assert.Equal(list, childState.Child);
		Assert.True(MuiListviewCore.GetAttribute(ref platform, State, listview,
			LvList, out var exposed));
		Assert.Equal(list.Raw, exposed);
		// Documented defaults: read/write input, prefs multi-select, default
		// scroller position, no drag.
		Assert.Equal(1u, Get(ref platform, listview, LvInput));
		Assert.Equal(1u, Get(ref platform, listview, LvMultiSelect));
		Assert.Equal(0u, Get(ref platform, listview, LvScrollerPos));
		Assert.Equal(0u, Get(ref platform, listview, LvDragType));
		Assert.True(MuiListviewCore.TryGetInteractionPolicy(ref platform, State,
			listview, out var policy));
		Assert.Equal(1u, policy.Input);
		Assert.Equal(1u, policy.MultiSelect);
		Assert.Equal(0u, policy.ScrollerPos);
		Assert.Equal(0u, policy.DragType);
		Assert.True(MuiListviewCore.TryGetSelectionSignal(ref platform, State,
			listview, out var signal));
		Assert.Equal(0u, signal.Value);
		Assert.Equal(0u, Get(ref platform, listview, ListSelectChange));
		Assert.False(MuiListviewCore.SetAttribute(ref platform, State, listview,
			ListSelectChange, 1, false));
		// Composite policy values are normalized at the Listview boundary, and
		// DragType remains coherent with the owned List projection.
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvInput, 9, false));
		Assert.Equal(1u, Get(ref platform, listview, LvInput));
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvMultiSelect, 9, false));
		Assert.Equal(1u, Get(ref platform, listview, LvMultiSelect));
		Assert.True(MuiListviewCore.TryGetInteractionPolicy(ref platform, State,
			listview, out policy));
		Assert.Equal(1u, policy.MultiSelect);
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvScrollerPos, 9, false));
		Assert.Equal(0u, Get(ref platform, listview, LvScrollerPos));
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvDragType, 9, false));
		Assert.Equal(0u, Get(ref platform, listview, LvDragType));
		Assert.Equal(0u, Get(ref platform, list, ListDragType));
		Assert.True(MuiListviewCore.TryGetInteractionPolicy(ref platform, State,
			listview, out policy));
		Assert.Equal(1u, policy.Input);
		Assert.Equal(1u, policy.MultiSelect);
		Assert.Equal(0u, policy.ScrollerPos);
		Assert.Equal(0u, policy.DragType);
	}

	[Fact]
	public void ListviewPolicyGettersPreferNamedInteractionRecord()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out var floattextClass, out var otherClass, 0x80000);
		var listview = MuiListviewCore.CreateListview(ref platform, State,
			listviewClass, APTR.Null);
		Assert.NotEqual(APTR.Null, listview);

		// Deliberately diverge the legacy scalar projection after each typed
		// policy update. The public getter must remain backed by the named,
		// guest-resident interaction record rather than whichever raw attribute
		// node was last written.
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvInput, 0, false));
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvMultiSelect, MultiSelectAlways, false));
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvScrollerPos, ScrollerPosNone, false));
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvDragType, 1, false));
		// Diverge the legacy scalar projection only after all typed setters have
		// completed; subsequent policy updates must not rebuild the record from
		// these deliberately stale values.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, listview,
			LvInput, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, listview,
			LvMultiSelect, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, listview,
			LvScrollerPos, 0, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, listview,
			LvDragType, 0, false));

		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, listview,
			LvInput, out var input));
		Assert.Equal(0u, input);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, listview,
			LvMultiSelect, out var multiSelect));
		Assert.Equal(MultiSelectAlways, multiSelect);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, listview,
			LvScrollerPos, out var scrollerPos));
		Assert.Equal(ScrollerPosNone, scrollerPos);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, listview,
			LvDragType, out var dragType));
		Assert.Equal(1u, dragType);

		var getMessage = APTR.FromPointer(0x3400);
		var getStorage = APTR.FromPointer(0x3440);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			LvInput));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.True(MuiCommonControlPacketCore.TryReadGet(ref platform,
			getMessage, out var getPacket));
		Assert.Equal(LvInput, getPacket.Attribute);
		Assert.True(MuiListviewCore.GetAttribute(ref platform, State, listview,
			LvInput, out var directPolicyInput));
		Assert.Equal(0u, directPolicyInput);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, getMessage));
		Assert.Equal(0u, platform.ReadUInt32(getStorage, 0));

		Assert.True(MuiListviewCore.TryGetInteractionPolicy(ref platform, State,
			listview, out var policy));
		Assert.Equal(0u, policy.Input);
		Assert.Equal(MultiSelectAlways, policy.MultiSelect);
		Assert.Equal(ScrollerPosNone, policy.ScrollerPos);
		Assert.Equal(1u, policy.DragType);
		DisposeListview(ref platform, listview, listClass, listviewClass,
			floattextClass, otherClass);
	}

	[Fact]
	public void ListviewCreatesInternalListWhenNoneSupplied()
	{
		var platform = CreatePlatform(out _, out var listviewClass, out _, out _,
			0x80000);
		var listview = MuiListviewCore.CreateListview(ref platform, State,
			listviewClass, APTR.Null);
		Assert.NotEqual(APTR.Null, listview);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		Assert.NotEqual(APTR.Null, child);
		Assert.Equal(MuiCollectionClass.List, MuiListCore.Classify(ref platform,
			State, child));
	}

	[Fact]
	public void ListviewFailsAtomicallyWhenSuppliedChildIsNotAList()
	{
		var platform = CreatePlatform(out _, out var listviewClass, out _,
			out var otherClass, 0x80000);
		var notAList = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			otherClass, APTR.Null);
		var tags = APTR.FromPointer(0x3000);
		platform.WriteUInt32(tags, 0, LvList);
		platform.WriteUInt32(tags, 4, notAList.Raw);
		platform.WriteUInt32(tags, 8, 0);
		Assert.Equal(APTR.Null, MuiListviewCore.CreateListview(ref platform, State,
			listviewClass, tags));
		// The rejected object is left intact and disposable without imbalance.
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			notAList));
	}

	[Fact]
	public void ListviewForwardsListAttributesButKeepsListviewAttributesLocal()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		// A list attribute set on the listview reaches the child.
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			ListActive, 4, false));
		// MorphOS 3.20 projects an empty List's active getter as zero, even when
		// forwarding an explicit index through its Listview composite.
		Assert.Equal(0u, Get(ref platform, child, ListActive));
		Assert.True(MuiListviewCore.GetAttribute(ref platform, State, listview,
			ListActive, out var active));
		Assert.Equal(0u, active);
		// The forwarded path remains class-aware: a one-column child clamps an
		// out-of-range SortColumn instead of leaving a raw invalid value behind.
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			ListSortColumn, 99, false));
		Assert.Equal(0u, Get(ref platform, child, ListSortColumn));
		// A listview attribute stays on the listview and does not leak to child.
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvInput, 0, false));
		Assert.Equal(0u, Get(ref platform, listview, LvInput));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State, child,
			LvInput, out _));
	}

	[Fact]
	public void ListviewRejectsGetterOnlyProjectionWrites()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);

		Assert.False(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvList, 0, false));
		Assert.Equal(child.Raw, Get(ref platform, listview, LvList));
		Assert.False(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvClickColumn, 7, false));
		Assert.False(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvAgainClick, 1, false));
		Assert.False(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvDoubleClick, 1, false));
		Assert.False(MuiListviewCore.SetAttribute(ref platform, State, listview,
			ListSelectChange, 1, false));
		Assert.Equal(0u, Get(ref platform, listview, LvClickColumn));
		Assert.Equal(0u, Get(ref platform, listview, LvAgainClick));
		Assert.Equal(0u, Get(ref platform, listview, LvDoubleClick));
		Assert.Equal(0u, Get(ref platform, listview, ListSelectChange));
	}

	[Fact]
	public void ListviewHandleClickDrivesActiveSelectionAndDoubleClick()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		for (var i = 0u; i < 4; i++)
			MuiListCore.InsertSingle(ref platform, State, child,
				APTR.FromPointer(0x5000000 + i), InsertBottom);
		// Single click selects and activates the entry.
		Assert.True(MuiListviewCore.HandleClick(ref platform, State, listview, 2, 1,
			0, false));
		Assert.Equal(2u, Get(ref platform, child, ListActive));
		Assert.Equal(0u, Get(ref platform, listview, LvDoubleClick));
		Assert.Equal(0u, Get(ref platform, child, LvDoubleClick));
		Assert.Equal(0u, Get(ref platform, child, LvAgainClick));
		Assert.Equal(0u, Get(ref platform, child, LvClickColumn));
		// Double click raises MUIA_Listview_DoubleClick.
		Assert.True(MuiListviewCore.HandleClick(ref platform, State, listview, 1, 2,
			0, false));
		Assert.Equal(1u, Get(ref platform, listview, LvDoubleClick));
		Assert.Equal(0u, Get(ref platform, listview, LvAgainClick));
		Assert.Equal(1u, Get(ref platform, child, LvDoubleClick));
		Assert.Equal(0u, Get(ref platform, child, LvAgainClick));
		Assert.Equal(0u, Get(ref platform, child, LvClickColumn));
		Assert.Equal(1u, Get(ref platform, child, ListActive));
		// A third click publishes AgainClick and clears the edge-triggered
		// DoubleClick flag. ClickColumn records the actual clicked column.
		Assert.True(MuiListviewCore.HandleClick(ref platform, State, listview, 1, 3,
			5, false));
		Assert.Equal(0u, Get(ref platform, listview, LvDoubleClick));
		Assert.Equal(1u, Get(ref platform, listview, LvAgainClick));
		Assert.Equal(5u, Get(ref platform, listview, LvClickColumn));
		Assert.Equal(0u, Get(ref platform, child, LvDoubleClick));
		Assert.Equal(1u, Get(ref platform, child, LvAgainClick));
		Assert.Equal(5u, Get(ref platform, child, LvClickColumn));
		// An unspecified input column resolves through DefClickColumn and clears
		// the transient AgainClick publication.
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvDefClickColumn, 7, false));
		Assert.True(MuiListviewCore.HandleClick(ref platform, State, listview, 0, 1,
			0xFFFFFFFFu, false));
		Assert.Equal(7u, Get(ref platform, listview, LvClickColumn));
		Assert.Equal(0u, Get(ref platform, listview, LvAgainClick));
		Assert.Equal(0u, Get(ref platform, child, LvDoubleClick));
		Assert.Equal(0u, Get(ref platform, child, LvAgainClick));
		Assert.Equal(7u, Get(ref platform, child, LvClickColumn));
		// A read-only listview ignores clicks.
		MuiListviewCore.SetAttribute(ref platform, State, listview, LvInput, 0,
			false);
		Assert.False(MuiListviewCore.HandleClick(ref platform, State, listview, 0, 1,
			0, false));
	}

	[Fact]
	public void ListviewPointerActivationRefreshesNamedViewportPixels()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x82000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		for (var i = 0u; i < 10; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
				APTR.FromPointer(0x82000 + i), InsertBottom));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 24));

		Assert.True(MuiListviewCore.HandleClick(ref platform, State, listview,
			9, 1, 0, false));
		Assert.Equal(7u, Get(ref platform, child, ListFirst));
		Assert.Equal(56u, Get(ref platform, child, ListTopPixel));
	}

	[Fact]
	public void ListviewListMutationsRefreshNamedViewportPixels()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x83000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		for (var i = 0u; i < 5; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
				APTR.FromPointer(0x83000 + i), InsertBottom));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 24));
		Assert.Equal(40u, Get(ref platform, child, ListTotalPixel));

		Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
			APTR.FromPointer(0x83100), InsertBottom));
		Assert.Equal(48u, Get(ref platform, child, ListTotalPixel));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListFirst, 3, false));
		Assert.Equal(3u, Get(ref platform, child, ListFirst));

		Assert.True(MuiListCore.Remove(ref platform, State, child, 3));
		Assert.True(MuiListCore.Remove(ref platform, State, child, 3));
		Assert.True(MuiListCore.Remove(ref platform, State, child, 3));
		Assert.Equal(24u, Get(ref platform, child, ListTotalPixel));
		Assert.Equal(0u, Get(ref platform, child, ListFirst));
		Assert.Equal(0u, Get(ref platform, child, ListTopPixel));
		Assert.True(MuiListCore.Remove(ref platform, State, child, 1));
		Assert.Equal(16u, Get(ref platform, child, ListTotalPixel));
		Assert.Equal(0u, Get(ref platform, child, ListFirst));

		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListFirst, 3, false));
		Assert.True(MuiListCore.Clear(ref platform, State, child));
		Assert.Equal(0u, Get(ref platform, child, ListFirst));
		Assert.Equal(0u, Get(ref platform, child, ListTopPixel));
		Assert.Equal(0u, Get(ref platform, child, ListTotalPixel));
	}

	[Fact]
	public void ListviewTitleArrayClickPublishesColumnAndSortsSortableFormat()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out var floattextClass, out var otherClass, 0xB0000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		var title = APTR.FromPointer(0xB400);
		var titleArray = APTR.FromPointer(0xB440);
		var format = APTR.FromPointer(0xB480);
		var alpha = APTR.FromPointer(0xB500);
		var bravo = APTR.FromPointer(0xB540);
		var charlie = APTR.FromPointer(0xB580);
		platform.WriteCString(title, "Name");
		platform.WriteCString(format, "SORTABLE");
		platform.WriteCString(alpha, "alpha");
		platform.WriteCString(bravo, "bravo");
		platform.WriteCString(charlie, "charlie");
		platform.WriteUInt32(titleArray, 0, title.Raw);
		platform.WriteUInt32(titleArray, 4, 0);
		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListTitleArray, titleArray.Raw, false));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListFormat, format.Raw, false));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, charlie,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, alpha,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, bravo,
			InsertBottom));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 24));

		var intui = APTR.FromPointer(0xB600);
		var packet = APTR.FromPointer(0xB700);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0, 0, 4, 4));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(0u, Get(ref platform, child, ListTitleClick));
		Assert.Equal(alpha, MuiListCore.GetEntry(ref platform, State, child, 0,
			APTR.Null));
		Assert.Equal(bravo, MuiListCore.GetEntry(ref platform, State, child, 1,
			APTR.Null));
		Assert.Equal(charlie, MuiListCore.GetEntry(ref platform, State, child, 2,
			APTR.Null));
		DisposeListview(ref platform, listview, listClass, listviewClass,
			floattextClass, otherClass);
	}

	[Fact]
	public void ListviewHandleInputUsesTypedPacketAndListActiveNavigation()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		for (var i = 0u; i < 10; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
				APTR.FromPointer(0x5200000 + i), InsertBottom));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 24));
		var packet = APTR.FromPointer(0x7200);
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0x7300, KeyDown));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(0u, Get(ref platform, child, ListActive));

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0x7300, KeyDown));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(1u, Get(ref platform, child, ListActive));

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0x7300, KeyPageDown));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(4u, Get(ref platform, child, ListActive));
		Assert.Equal(2u, Get(ref platform, child, ListFirst));
		Assert.Equal(16u, Get(ref platform, child, ListTopPixel));

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0x7300, KeyBottom));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(9u, Get(ref platform, child, ListActive));
		Assert.Equal(7u, Get(ref platform, child, ListFirst));
		Assert.Equal(56u, Get(ref platform, child, ListTopPixel));

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0x7300, KeyTop));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(0u, Get(ref platform, child, ListActive));
		Assert.Equal(0u, Get(ref platform, child, ListFirst));

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0x7300, KeyUp));
		Assert.Equal(0u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(0u, Get(ref platform, child, ListActive));

		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvInput, 0, false));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0x7300, KeyDown));
		Assert.Equal(0u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(0u, Get(ref platform, child, ListActive));
	}

	[Fact]
	public void ListviewHandleInputPressAndToggleUseListSelectionState()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		for (var i = 0u; i < 3; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
				APTR.FromPointer(0x5300000 + i), InsertBottom));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListActive, 1));
		var packet = APTR.FromPointer(0x7400);
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0x7500, KeyPress));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.True(IsSelected(ref platform, child, 1));
		Assert.False(IsSelected(ref platform, child, 0));

		// Default multi-select makes TOGGLE invert only the active row.
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0x7500, KeyToggle));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.False(IsSelected(ref platform, child, 1));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0x7500, KeyToggle));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.True(IsSelected(ref platform, child, 1));

		// None mode converts TOGGLE into an exclusive active selection.
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvMultiSelect, 0, false));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListActive, 2));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0x7500, KeyToggle));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.True(IsSelected(ref platform, child, 2));
		Assert.False(IsSelected(ref platform, child, 1));

		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvInput, 0, false));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0x7500, KeyPress));
		Assert.Equal(0u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
	}

	[Fact]
	public void ListviewExclusiveSelectionPublishesOneChangeNotification()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		for (var i = 0u; i < 2; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
				APTR.FromPointer(0x5390000 + i), InsertBottom));
		var follow = APTR.FromPointer(0x7700);
		platform.WriteUInt32(follow, 0, 0x90000004);
		platform.WriteUInt32(follow, 4, EveryTime);
		Assert.True(MuiNotifyCore.Add(ref platform, State, listview,
			ListSelectChange, EveryTime, listview, 2, follow));

		var baseline = platform.DispatchCount;
		Assert.True(MuiListviewCore.HandleClick(ref platform, State, listview,
			0, 1, 0, false));
		Assert.Equal(baseline + 1, platform.DispatchCount);
		var afterFirst = platform.DispatchCount;
		Assert.True(MuiListviewCore.HandleClick(ref platform, State, listview,
			1, 1, 0, false));
		Assert.Equal(afterFirst + 1, platform.DispatchCount);
		Assert.True(IsSelected(ref platform, child, 1));
		Assert.False(IsSelected(ref platform, child, 0));
	}

	[Fact]
	public void ListviewPassiveMouseMoveDoesNotActivateOrSelectRows()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		for (var i = 0u; i < 3; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
				APTR.FromPointer(0x5380000 + i), InsertBottom));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListActive, 0, false));

		var pointer = default(MuiIntuiPointerMessage);
		pointer.Class = IdcmpMouseMove;
		pointer.MouseX = 4;
		pointer.MouseY = 16; // row 2 in the default 8-pixel geometry
		Assert.False(MuiListviewCore.HandlePointer(ref platform, State, listview,
			child, pointer));
		Assert.Equal(0u, Get(ref platform, child, ListActive));
		Assert.False(IsSelected(ref platform, child, 2));
	}

	[Fact]
	public void ListviewProjectsOwnedListSelectionChangeNotifications()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		for (var i = 0u; i < 2; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
				APTR.FromPointer(0x5360000 + i), InsertBottom));

		var follow = APTR.FromPointer(0x7600);
		platform.WriteUInt32(follow, 0, 0x90000003);
		platform.WriteUInt32(follow, 4, EveryTime);
		Assert.True(MuiNotifyCore.Add(ref platform, State, listview,
			ListSelectChange, EveryTime, listview, 2, follow));

		var baseline = platform.DispatchCount;
		Assert.True(MuiListCore.Remove(ref platform, State, child, 1));
		Assert.Equal(baseline, platform.DispatchCount);

		// The child is the storage owner, but MorphOS Listview publishes the
		// same change signal at its composite boundary.
		Assert.True(MuiListCore.Select(ref platform, State, child, 0, SelectOn,
			APTR.Null));
		Assert.Equal(baseline + 1, platform.DispatchCount);
		Assert.True(MuiListviewCore.TryGetSelectionSignal(ref platform, State,
			listview, out var signal));
		Assert.Equal(1u, signal.Value);
		Assert.True(MuiListviewCore.GetAttribute(ref platform, State, listview,
			ListSelectChange, out var publicValue));
		Assert.Equal(1u, publicValue);

		var afterSelect = platform.DispatchCount;
		Assert.True(MuiListCore.Remove(ref platform, State, child, 0));
		Assert.Equal(afterSelect + 1, platform.DispatchCount);
		Assert.True(MuiListviewCore.TryGetSelectionSignal(ref platform, State,
			listview, out signal));
		Assert.Equal(0u, signal.Value);
		Assert.True(MuiListviewCore.GetAttribute(ref platform, State, listview,
			ListSelectChange, out publicValue));
		Assert.Equal(0u, publicValue);
	}

	[Fact]
	public void ListviewKeyboardPressPublishesDefaultClickColumn()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		for (var i = 0u; i < 2; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
				APTR.FromPointer(0x5350000 + i), InsertBottom));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListActive, 1));
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvDefClickColumn, 4, false));

		var packet = APTR.FromPointer(0x7450);
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0x7550, KeyPress));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.True(IsSelected(ref platform, child, 1));
		Assert.Equal(4u, Get(ref platform, listview, LvClickColumn));
		Assert.Equal(0u, Get(ref platform, listview, LvDoubleClick));
		Assert.Equal(0u, Get(ref platform, listview, LvAgainClick));
	}

	[Fact]
	public void ListviewHorizontalKeysAndNewMouseWheelUseNamedScrollState()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0xA8000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
			APTR.FromPointer(0xAA000), InsertBottom));
		Assert.True(MuiListCore.SetHScrollerViewport(ref platform, State, child,
			200, 1));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 40));

		var packet = APTR.FromPointer(0xA6000);
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0xA6800, KeyRight));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.True(MuiListCore.TryGetHScrollerState(ref platform, State, child,
			out var stateAfterKey));
		Assert.Equal(8u, stateAfterKey.ScrollX);

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0xA6800, KeyLeft));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.True(MuiListCore.TryGetHScrollerState(ref platform, State, child,
			out stateAfterKey));
		Assert.Equal(0u, stateAfterKey.ScrollX);

		var wheel = default(MuiIntuiPointerMessage);
		wheel.Class = IdcmpMouseButtons;
		wheel.Code = WheelRight;
		Assert.True(MuiListviewCore.HandlePointer(ref platform, State, listview,
			child, wheel));
		Assert.True(MuiListCore.TryGetHScrollerState(ref platform, State, child,
			out var stateAfterWheel));
		Assert.Equal(8u, stateAfterWheel.ScrollX);
		wheel.Code = WheelLeft;
		Assert.True(MuiListviewCore.HandlePointer(ref platform, State, listview,
			child, wheel));
		Assert.True(MuiListCore.TryGetHScrollerState(ref platform, State, child,
			out stateAfterWheel));
		Assert.Equal(0u, stateAfterWheel.ScrollX);
	}

	[Fact]
	public void ListviewVerticalNewMouseWheelUsesBoundedFirstState()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0xB0000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		for (var i = 0u; i < 6; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
				APTR.FromPointer(0xB8000 + i), InsertBottom));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 24));
		Assert.True(MuiListviewCore.GetScrollerState(ref platform, State, listview,
			out var entries, out _, out var first, out var maxFirst));
		Assert.Equal(6u, entries);
		Assert.Equal(0u, first);
		Assert.True(maxFirst > 1);

		var wheel = default(MuiIntuiPointerMessage);
		wheel.Class = IdcmpMouseButtons;
		wheel.Code = WheelDown;
		Assert.True(MuiListviewCore.HandlePointer(ref platform, State, listview,
			child, wheel));
		Assert.True(MuiListviewCore.GetScrollerState(ref platform, State, listview,
			out _, out _, out first, out _));
		Assert.Equal(1u, first);
		Assert.Equal(8u, Get(ref platform, child, ListTopPixel));

		wheel.Code = WheelUp;
		Assert.True(MuiListviewCore.HandlePointer(ref platform, State, listview,
			child, wheel));
		Assert.True(MuiListviewCore.GetScrollerState(ref platform, State, listview,
			out _, out _, out first, out _));
		Assert.Equal(0u, first);
		Assert.Equal(0u, Get(ref platform, child, ListTopPixel));

		var intui = APTR.FromPointer(0xA6000);
		var packet = APTR.FromPointer(0xA6800);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, WheelDown, 0, 0, 0, 0));
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvInput, 0, false));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(0u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.True(MuiListviewCore.GetScrollerState(ref platform, State, listview,
			out _, out _, out first, out _));
		Assert.Equal(0u, first);
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvInput, 1, false));

		Assert.True(MuiListviewCore.SetScrollerFirst(ref platform, State, listview,
			unchecked((int)maxFirst)));
		wheel.Code = WheelDown;
		Assert.True(MuiListviewCore.HandlePointer(ref platform, State, listview,
			child, wheel));
		Assert.True(MuiListviewCore.GetScrollerState(ref platform, State, listview,
			out _, out _, out first, out _));
		Assert.Equal(maxFirst, first);
		Assert.Equal(maxFirst * 8u, Get(ref platform, child, ListTopPixel));

		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvDragType, 1, false));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListDragSortable, 1, false));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListDragType, 1, false));
		var dragStart = default(MuiIntuiPointerMessage);
		dragStart.Class = IdcmpMouseButtons;
		dragStart.Code = SelectDown;
		dragStart.MouseX = 1;
		dragStart.MouseY = 1;
		Assert.True(MuiListviewCore.HandlePointer(ref platform, State, listview,
			child, dragStart));
		wheel.Code = WheelUp;
		Assert.False(MuiListviewCore.HandlePointer(ref platform, State, listview,
			child, wheel));
		Assert.True(MuiListviewCore.HandleInput(ref platform, State, listview,
			APTR.Null, KeyRelease));
	}

	[Fact]
	public void ListviewScrollerFirstRefreshesNamedViewportPixels()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x90000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		for (var i = 0u; i < 6; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
				APTR.FromPointer(0xC0000 + i), InsertBottom));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 24));
		Assert.Equal(0u, Get(ref platform, child, ListTopPixel));

		Assert.True(MuiListviewCore.SetScrollerFirst(ref platform, State, listview,
			2));
		Assert.Equal(16u, Get(ref platform, child, ListTopPixel));
		Assert.True(MuiListviewCore.SetScrollerFirst(ref platform, State, listview,
			0));
		Assert.Equal(0u, Get(ref platform, child, ListTopPixel));
	}

	[Fact]
	public void ListviewHandleInputSelectUpUsesTypedIntuiMessageHitTest()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		for (var i = 0u; i < 4; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
				APTR.FromPointer(0x5400000 + i), InsertBottom));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 24));

		var intui = APTR.FromPointer(0x7600);
		var packet = APTR.FromPointer(0x7700);
		// SELECTDOWN is only the beginning edge and must not select yet.
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectDown, 0, 0, 4, 12));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(0u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.False(IsSelected(ref platform, child, 1));

		// SELECTUP at y=12 hits row 1 and commits an exclusive click.
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0, 0, 4, 12));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(1u, Get(ref platform, child, ListActive));
		Assert.True(IsSelected(ref platform, child, 1));
		Assert.False(IsSelected(ref platform, child, 0));

		// Shifted SELECTUP accumulates the hit row through the existing Listview
		// MultiSelect policy; no second selection model is introduced.
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, ShiftQualifier, 0, 4, 20));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.True(IsSelected(ref platform, child, 1));
		Assert.True(IsSelected(ref platform, child, 2));

		// A click outside the child viewport is claimed by the input path but does
		// not mutate active or selection state.
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0, 0, 4, 100));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(0u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(2u, Get(ref platform, child, ListActive));
	}

	[Fact]
	public void ListviewPointerControlAndAltQualifiersAccumulateMultiSelect()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out var floattextClass, out var otherClass, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		for (var i = 0u; i < 4; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
				APTR.FromPointer(0x5500000 + i), InsertBottom));
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvMultiSelect, MultiSelectShifted, false));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 32));

		var intui = APTR.FromPointer(0x7E00);
		var packet = APTR.FromPointer(0x7F00);
		// The first click establishes the selection; Control and left Alt then
		// use the same typed qualifier path as Shift to accumulate rows.
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0, 0, 4, 4));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0x0008, 0, 4, 12));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0x0010, 0, 4, 20));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(3u, SelectedCount(ref platform, child));
		Assert.True(IsSelected(ref platform, child, 0));
		Assert.True(IsSelected(ref platform, child, 1));
		Assert.True(IsSelected(ref platform, child, 2));

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			listview));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listviewClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			floattextClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void ListviewPointerDragPreviewsDropMarkAndReordersSortableChild()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListDragSortable, 1));
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvDragType, 1, false));
		Assert.Equal(1u, Get(ref platform, child, ListDragType));
		var alpha = APTR.FromPointer(0x5A00);
		var bravo = APTR.FromPointer(0x5A40);
		var charlie = APTR.FromPointer(0x5A80);
		platform.WriteCString(alpha, "alpha");
		platform.WriteCString(bravo, "bravo");
		platform.WriteCString(charlie, "charlie");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, alpha,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, bravo,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, charlie,
			InsertBottom));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 24));

		var intui = APTR.FromPointer(0x7800);
		var packet = APTR.FromPointer(0x7900);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectDown, 0, 0, 4, 4));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseMove, 0, 0, 0, 4, 20));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(2u, Get(ref platform, child, ListDropMark));

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0, 0, 4, 20));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(unchecked((uint)-1), Get(ref platform, child, ListDropMark));
		Assert.Equal(bravo, MuiListCore.GetEntry(ref platform, State, child, 0,
			APTR.Null));
		Assert.Equal(charlie, MuiListCore.GetEntry(ref platform, State, child, 1,
			APTR.Null));
		Assert.Equal(alpha, MuiListCore.GetEntry(ref platform, State, child, 2,
			APTR.Null));
	}

	[Fact]
	public void ListviewPointerDragMovesSelectedRowsAsOneGuestSlotGroup()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out var floattextClass, out var otherClass, 0x90000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListDragSortable, 1));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListDragType, 1));
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvDragType, 1, false));
		var alpha = APTR.FromPointer(0x5C00);
		var bravo = APTR.FromPointer(0x5C40);
		var charlie = APTR.FromPointer(0x5C80);
		var delta = APTR.FromPointer(0x5CC0);
		platform.WriteCString(alpha, "alpha");
		platform.WriteCString(bravo, "bravo");
		platform.WriteCString(charlie, "charlie");
		platform.WriteCString(delta, "delta");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, alpha,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, bravo,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, charlie,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, delta,
			InsertBottom));
		Assert.True(MuiListCore.Select(ref platform, State, child, 0, SelectOn,
			APTR.Null));
		Assert.True(MuiListCore.Select(ref platform, State, child, 2, SelectOn,
			APTR.Null));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 48));

		var intui = APTR.FromPointer(0x7C00);
		var packet = APTR.FromPointer(0x7D00);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectDown, 0, 0, 4, 4));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseMove, 0, 0, 0, 4, 36));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(4u, Get(ref platform, child, ListDropMark));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0, 0, 4, 36));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));

		Assert.Equal(bravo, MuiListCore.GetEntry(ref platform, State, child, 0,
			APTR.Null));
		Assert.Equal(delta, MuiListCore.GetEntry(ref platform, State, child, 1,
			APTR.Null));
		Assert.Equal(alpha, MuiListCore.GetEntry(ref platform, State, child, 2,
			APTR.Null));
		Assert.Equal(charlie, MuiListCore.GetEntry(ref platform, State, child, 3,
			APTR.Null));
		Assert.True(IsSelected(ref platform, child, 2));
		Assert.True(IsSelected(ref platform, child, 3));
		DisposeListview(ref platform, listview, listClass, listviewClass,
			floattextClass, otherClass);
	}

	[Fact]
	public void ListviewPointerDragLeavingViewportClearsTargetAndDoesNotReorder()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out var floattextClass, out var otherClass, 0x90000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListDragSortable, 1));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListDragType, 1));
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvDragType, 1, false));
		var alpha = APTR.FromPointer(0x5E00);
		var bravo = APTR.FromPointer(0x5E40);
		var charlie = APTR.FromPointer(0x5E80);
		platform.WriteCString(alpha, "alpha");
		platform.WriteCString(bravo, "bravo");
		platform.WriteCString(charlie, "charlie");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, alpha,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, bravo,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, charlie,
			InsertBottom));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 24));

		var intui = APTR.FromPointer(0x7E00);
		var packet = APTR.FromPointer(0x7F00);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectDown, 0, 0, 4, 4));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));

		// First arm a real target, then leave the List viewport. The old target
		// must not survive as a commit destination.
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseMove, 0, 0, 0, 4, 20));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(2u, Get(ref platform, child, ListDropMark));

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseMove, 0, 0, 0, 4, 40));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(unchecked((uint)-1), Get(ref platform, child, ListDropMark));

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0, 0, 4, 40));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(alpha, MuiListCore.GetEntry(ref platform, State, child, 0,
			APTR.Null));
		Assert.Equal(bravo, MuiListCore.GetEntry(ref platform, State, child, 1,
			APTR.Null));
		Assert.Equal(charlie, MuiListCore.GetEntry(ref platform, State, child, 2,
			APTR.Null));
		DisposeListview(ref platform, listview, listClass, listviewClass,
			floattextClass, otherClass);
	}

	[Fact]
	public void ListviewPointerDragBelowVisibleRowsAppendsSortableChild()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out var floattextClass, out var otherClass, 0xA0000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListDragSortable, 1));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListDragType, 1));
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvDragType, 1, false));
		var alpha = APTR.FromPointer(0xA400);
		var bravo = APTR.FromPointer(0xA440);
		var charlie = APTR.FromPointer(0xA480);
		platform.WriteCString(alpha, "alpha");
		platform.WriteCString(bravo, "bravo");
		platform.WriteCString(charlie, "charlie");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, alpha,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, bravo,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, charlie,
			InsertBottom));
		// The fourth row is inside the viewport but has no entry, so TestPos
		// reports the typed Below boundary and Listview exposes append as target 3.
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 40));

		var intui = APTR.FromPointer(0xA800);
		var packet = APTR.FromPointer(0xA900);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectDown, 0, 0, 4, 4));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseMove, 0, 0, 0, 4, 28));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(3u, Get(ref platform, child, ListDropMark));

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0, 0, 4, 28));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(unchecked((uint)-1), Get(ref platform, child, ListDropMark));
		Assert.Equal(bravo, MuiListCore.GetEntry(ref platform, State, child, 0,
			APTR.Null));
		Assert.Equal(charlie, MuiListCore.GetEntry(ref platform, State, child, 1,
			APTR.Null));
		Assert.Equal(alpha, MuiListCore.GetEntry(ref platform, State, child, 2,
			APTR.Null));
		DisposeListview(ref platform, listview, listClass, listviewClass,
			floattextClass, otherClass);
	}

	[Fact]
	public void ListviewInputDisableCancelsActiveDragAndClearsDropMark()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListDragSortable, 1));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListDragType, 1));
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvDragType, 1, false));
		var alpha = APTR.FromPointer(0x5B00);
		var bravo = APTR.FromPointer(0x5B40);
		var charlie = APTR.FromPointer(0x5B80);
		platform.WriteCString(alpha, "alpha");
		platform.WriteCString(bravo, "bravo");
		platform.WriteCString(charlie, "charlie");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, alpha,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, bravo,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, charlie,
			InsertBottom));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 24));

		var intui = APTR.FromPointer(0x7A00);
		var packet = APTR.FromPointer(0x7B00);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectDown, 0, 0, 4, 4));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseMove, 0, 0, 0, 4, 20));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(2u, Get(ref platform, child, ListDropMark));

		// Disabling input is an immediate cancellation edge. The named drag
		// record is released by the Input attribute setter, and the public child
		// marker is cleared before any later pointer packet can arrive.
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvInput, 0, false));
		Assert.Equal(unchecked((uint)-1), Get(ref platform, child, ListDropMark));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0, KeyRelease));
		Assert.Equal(0u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(unchecked((uint)-1), Get(ref platform, child, ListDropMark));
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvInput, 1, false));

		// After cancellation SELECTUP is no longer consumed as a drag finish. It
		// therefore behaves like the ordinary row click and leaves list order
		// untouched.
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0, 0, 4, 20));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(alpha, MuiListCore.GetEntry(ref platform, State, child, 0,
			APTR.Null));
		Assert.Equal(bravo, MuiListCore.GetEntry(ref platform, State, child, 1,
			APTR.Null));
		Assert.Equal(charlie, MuiListCore.GetEntry(ref platform, State, child, 2,
			APTR.Null));

		// Disabling the Listview drag policy is the same immediate boundary even
		// when input remains enabled. The child policy is normalized and the
		// active named drag is released before SELECTUP can reorder the rows.
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvDragType, 1, false));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectDown, 0, 0, 4, 4));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseMove, 0, 0, 0, 4, 20));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(2u, Get(ref platform, child, ListDropMark));
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvDragType, 0, false));
		Assert.Equal(0u, Get(ref platform, child, ListDragType));
		Assert.Equal(unchecked((uint)-1), Get(ref platform, child, ListDropMark));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0, 0, 4, 20));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(alpha, MuiListCore.GetEntry(ref platform, State, child, 0,
			APTR.Null));
		Assert.Equal(bravo, MuiListCore.GetEntry(ref platform, State, child, 1,
			APTR.Null));
		Assert.Equal(charlie, MuiListCore.GetEntry(ref platform, State, child, 2,
			APTR.Null));
	}

	[Fact]
	public void ListviewLayoutReservesScrollerAndPositionsChild()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0, 100,
			50));
		// 16px scrollbar reserved by default; child gets the remaining width.
		Assert.Equal(84u, Get(ref platform, child, Width));
		Assert.Equal(0u, Get(ref platform, child, LeftEdge));
		Assert.Equal(100u, Get(ref platform, listview, Width));
		// With the scroller suppressed the child spans the full width.
		MuiListviewCore.SetAttribute(ref platform, State, listview, LvScrollerPos,
			ScrollerPosNone, false);
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0, 100,
			50));
		Assert.Equal(100u, Get(ref platform, child, Width));
	}

	[Fact]
	public void ListviewPublishesOffViewportSentinelsWhenChildIsInvisible()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x82000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
			APTR.FromPointer(0x82000), InsertBottom));

		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 0));
		Assert.Equal(uint.MaxValue, Get(ref platform, child, ListVisible));
		Assert.Equal(uint.MaxValue, Get(ref platform, child, ListFirst));
		Assert.Equal(0u, Get(ref platform, child, ListTopPixel));
		Assert.True(MuiListviewCore.GetScrollerState(ref platform, State, listview,
			out var entries, out var visible, out var first, out var maxFirst));
		Assert.Equal(1u, entries);
		Assert.Equal(uint.MaxValue, visible);
		Assert.Equal(uint.MaxValue, first);
		Assert.Equal(0u, maxFirst);

		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 8));
		Assert.Equal(1u, Get(ref platform, child, ListVisible));
		Assert.Equal(0u, Get(ref platform, child, ListFirst));
	}

	[Fact]
	public void ListviewHorizontalScrollerPolicyReservesBottomTrackAndDrawsNamedGeometry()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x90000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		// Publish a measured content width through the List-owned named state. The
		// listview will recompute the viewport width from its actual layout.
		Assert.True(MuiListCore.SetHScrollerViewport(ref platform, State, child,
			200, 1));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 40));
		Assert.Equal(24u, Get(ref platform, child, 0x80423237u)); // 40 - 16
		Assert.True(MuiListCore.TryGetHScrollerState(ref platform, State, child,
			out var hState));
		Assert.Equal(200u, hState.ContentWidth);
		Assert.Equal(84u, hState.ViewWidth); // vertical reserve leaves 84px
		Assert.Equal(1u, hState.Visible);

		var renderInfo = APTR.FromPointer(0x6800);
		platform.WriteUInt32(renderInfo, 20, 0x6840);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, listview,
			renderInfo));
		var before = platform.FillCount;
		Assert.True(MuiListviewCore.Draw(ref platform, State, listview, 0));
		// Vertical and horizontal tracks each emit a neutral track and thumb.
		Assert.True(platform.FillCount >= before + 4);
	}

	[Fact]
	public void ListviewHorizontalScrollerUsesNamedGuestRecord()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out var floattextClass, out var otherClass, 0x92000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		Assert.True(MuiListCore.SetHScrollerViewport(ref platform, State, child,
			200, 1));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 40));
		Assert.True(MuiListviewCore.TryGetHorizontalScrollerState(ref platform,
			State, listview, out var record));
		Assert.Equal(MuiListviewHorizontalScrollerState.Cookie, record.Magic);
		Assert.Equal(200u, record.ContentWidth);
		Assert.Equal(84u, record.ViewWidth);
		Assert.Equal(0u, record.ScrollX);
		Assert.Equal(116u, record.MaxScrollX);
		Assert.True(record.TrackRight >= record.TrackLeft);
		Assert.True(record.ThumbRight >= record.ThumbLeft);

		Assert.True(MuiListCore.SetHScrollerScroll(ref platform, State, child,
			99));
		Assert.True(MuiListviewCore.TryGetHorizontalScrollerState(ref platform,
			State, listview, out record));
		// The public record is refreshed by the next composite geometry pass;
		// drawing is that same pass and consumes only the typed projection.
		var renderInfo = APTR.FromPointer(0x92800);
		platform.WriteUInt32(renderInfo, 20, 0x92840);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, listview,
			renderInfo));
		Assert.True(MuiListviewCore.Draw(ref platform, State, listview, 0));
		Assert.True(MuiListviewCore.TryGetHorizontalScrollerState(ref platform,
			State, listview, out record));
		Assert.Equal(99u, record.ScrollX);
		DisposeListview(ref platform, listview, listClass, listviewClass,
			floattextClass, otherClass);
	}

	[Fact]
	public void ListviewHorizontalScrollerThumbDragPublishesBoundedNamedOffset()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0xA0000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
			APTR.FromPointer(0xA4000), InsertBottom));
		Assert.True(MuiListCore.SetHScrollerViewport(ref platform, State, child,
			200, 1));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 40));
		Assert.True(MuiListCore.TryGetHScrollerState(ref platform, State, child,
			out var before));
		Assert.True(before.Visible != 0 && before.MaxScrollX > 0);

		var down = default(MuiIntuiPointerMessage);
		down.Class = IdcmpMouseButtons;
		down.Code = SelectDown;
		down.MouseX = 10;
		down.MouseY = 30; // horizontal track, inside the initial thumb
		Assert.True(MuiListviewCore.HandlePointer(ref platform, State, listview,
			child, down));

		var move = down;
		move.Class = IdcmpMouseMove;
		move.Code = 0;
		move.MouseX = 50;
		Assert.True(MuiListviewCore.HandlePointer(ref platform, State, listview,
			child, move));

		var up = move;
		up.Class = IdcmpMouseButtons;
		up.Code = SelectUp;
		Assert.True(MuiListviewCore.HandlePointer(ref platform, State, listview,
			child, up));
		Assert.True(MuiListCore.TryGetHScrollerState(ref platform, State, child,
			out var after));
		Assert.True(after.ScrollX > 0);
		Assert.True(after.ScrollX <= after.MaxScrollX);
	}

	[Fact]
	public void ListviewPublishesVisibleRowsAndClampsScrollerFirst()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		for (var i = 0u; i < 10; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
				APTR.FromPointer(0x5000000 + i), InsertBottom));

		// A 24px viewport exposes three 8px rows and therefore seven legal
		// first-row positions for ten entries.
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 24));
		Assert.Equal(3u, Get(ref platform, child, ListVisible));
		Assert.True(MuiListviewCore.GetScrollerState(ref platform, State, listview,
			out var entries, out var visible, out var first, out var maxFirst));
		Assert.Equal(10u, entries);
		Assert.Equal(3u, visible);
		Assert.Equal(0u, first);
		Assert.Equal(7u, maxFirst);

		Assert.True(MuiListviewCore.SetScrollerFirst(ref platform, State, listview,
			999));
		Assert.Equal(7u, Get(ref platform, child, ListFirst));
		Assert.Equal(56u, Get(ref platform, child, ListTopPixel));
		Assert.True(MuiListviewCore.GetScrollerState(ref platform, State, listview,
			out _, out _, out first, out _));
		Assert.Equal(7u, first);
		// Growing the viewport past the end clamps the existing child position,
		// even without an intervening scrollbar event.
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 80));
		Assert.Equal(0u, Get(ref platform, child, ListFirst));
		Assert.Equal(0u, Get(ref platform, child, ListTopPixel));
	}

	[Fact]
	public void ListviewTitleConsumesVisibleDataRowAndScrollerRange()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x8A000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		var title = APTR.FromPointer(0x8A400);
		platform.WriteCString(title, "Name");
		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListTitle, title.Raw, false));
		for (var i = 0u; i < 4; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
				APTR.FromPointer(0x8A500 + i * 0x40), InsertBottom));

		// One of the three 8px rows is the title, leaving two data rows.
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			100, 24));
		Assert.Equal(2u, Get(ref platform, child, ListVisible));
		Assert.True(MuiListviewCore.GetScrollerState(ref platform, State, listview,
			out var entries, out var visible, out var first, out var maxFirst));
		Assert.Equal(4u, entries);
		Assert.Equal(2u, visible);
		Assert.Equal(0u, first);
		Assert.Equal(2u, maxFirst);

		Assert.True(MuiListviewCore.SetScrollerFirst(ref platform, State, listview,
			99));
		Assert.Equal(2u, Get(ref platform, child, ListFirst));
	}

	[Fact]
	public void ListviewDrawRendersSurroundAndSchedulesChildRedraw()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var renderInfo = APTR.FromPointer(0x6000);
		platform.WriteUInt32(renderInfo, 20, 0x6100); // rastPort
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, listview,
			renderInfo));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0, 40,
			20));
		var before = platform.RedrawCount;
		Assert.True(MuiListviewCore.Draw(ref platform, State, listview, 0));
		Assert.Equal(before + 1, platform.RedrawCount); // child redraw scheduled
	}

	[Fact]
	public void ListviewRenderUsesNamedGuestRecord()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out var floattextClass, out var otherClass, 0x84000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var renderInfo = APTR.FromPointer(0x6400);
		var rastPort = APTR.FromPointer(0x6440);
		platform.WriteUInt32(renderInfo, 20, rastPort.Raw);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, listview,
			renderInfo));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 2, 3,
			48, 24));
		Assert.True(MuiListviewCore.TryGetRenderState(ref platform, State, listview,
			out var record));
		Assert.Equal(MuiListviewCore.MuiListviewRenderState.Cookie,
			record.Magic);
		Assert.Equal(renderInfo.Raw, record.RenderInfo.Raw);
		Assert.Equal(rastPort.Raw, record.RastPort.Raw);

		// A generic raw write does not replace the canonical render record. A
		// subsequent layout through the Area seam republishes a new RenderInfo.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, listview,
			0x7fff0001u, 0, false));
		Assert.True(MuiListviewCore.TryGetRenderState(ref platform, State, listview,
			out record));
		Assert.Equal(renderInfo.Raw, record.RenderInfo.Raw);
		var replacement = APTR.FromPointer(0x6480);
		var replacementRastPort = APTR.FromPointer(0x64C0);
		platform.WriteUInt32(replacement, 20, replacementRastPort.Raw);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, listview,
			replacement));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 2, 3,
			48, 24));
		Assert.True(MuiListviewCore.TryGetRenderState(ref platform, State, listview,
			out record));
		Assert.Equal(replacement.Raw, record.RenderInfo.Raw);
		Assert.Equal(replacementRastPort.Raw, record.RastPort.Raw);
		DisposeListview(ref platform, listview, listClass, listviewClass,
			floattextClass, otherClass);
	}

	[Fact]
	public void ListviewDrawRendersOwnedChildRowsThroughSharedRenderInfo()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		var first = APTR.FromPointer(0x6200);
		var second = APTR.FromPointer(0x6240);
		platform.WriteCString(first, "alpha");
		platform.WriteCString(second, "bravo");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, first,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, second,
			InsertBottom));
		var renderInfo = APTR.FromPointer(0x6300);
		platform.WriteUInt32(renderInfo, 20, 0x6340); // shared rastPort
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, listview,
			renderInfo));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			40, 20));
		platform.TextCount = 0;
		Assert.True(MuiListviewCore.Draw(ref platform, State, listview, 0));
		Assert.Equal(2u, platform.TextCount);
		Assert.Equal(3, platform.LastTextLength);
		Assert.Equal((byte)'b', platform.ReadUInt8(platform.LastText, 0));
	}

	[Fact]
	public void ListviewDrawAddsNeutralScrollbarTrackAndThumbForOverflow()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		for (var i = 0u; i < 8; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
				APTR.FromPointer(0x5100000 + i), InsertBottom));
		var renderInfo = APTR.FromPointer(0x6000);
		platform.WriteUInt32(renderInfo, 20, 0x6100); // rastPort
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, listview,
			renderInfo));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			40, 20));
		var before = platform.FillCount;
		Assert.True(MuiListviewCore.Draw(ref platform, State, listview, 0));
		// The listview surround plus the scrollbar track and thumb all draw via
		// the platform seam; the child redraw remains independently scheduled.
		Assert.True(platform.FillCount >= before + 2);
		Assert.Equal(1u, platform.RedrawCount);
	}

	[Fact]
	public void ListviewScrollerPointerUsesNamedDragStateAndBoundedFirstRows()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out var floattextClass, out var otherClass, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		for (var i = 0u; i < 12; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
				APTR.FromPointer(0x5200000 + i), InsertBottom));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			40, 40));
		Assert.True(MuiListviewCore.GetScrollerState(ref platform, State, listview,
			out var entries, out var visible, out var first, out var maxFirst));
		Assert.Equal(12u, entries);
		Assert.Equal(5u, visible);
		Assert.Equal(0u, first);
		Assert.Equal(7u, maxFirst);

		var intui = APTR.FromPointer(0x7C00);
		var packet = APTR.FromPointer(0x7D00);
		// The default right-hand track is x=24..39 and the initial thumb is
		// y=0..15.  Preserve an eight-pixel grab offset through the drag.
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectDown, 0, 0, 32, 8));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseMove, 0, 0, 0, 32, 24));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.True(MuiListviewCore.GetScrollerState(ref platform, State, listview,
			out _, out _, out var movedFirst, out _));
		Assert.True(movedFirst > 0 && movedFirst < maxFirst);

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0, 0, 32, 32));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.True(MuiListviewCore.GetScrollerState(ref platform, State, listview,
			out _, out _, out var releasedFirst, out _));
		Assert.Equal(maxFirst, releasedFirst);

		// A SELECTUP on the track without an armed drag maps the click through
		// the same named geometry, leaving the List child as the sole scroll model.
		Assert.True(MuiListviewCore.SetScrollerFirst(ref platform, State, listview,
			0));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0, 0, 32, 30));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.True(MuiListviewCore.GetScrollerState(ref platform, State, listview,
			out _, out _, out var clickedFirst, out _));
		Assert.True(clickedFirst > 0 && clickedFirst <= maxFirst);

		// MUIKEY_RELEASE cancels an armed scroller gesture just like the existing
		// list drag cancellation path, so disabling input cannot strand state.
		Assert.True(MuiListviewCore.SetScrollerFirst(ref platform, State, listview,
			0));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectDown, 0, 0, 32, 8));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0, KeyRelease));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			listview));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listviewClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			floattextClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void ListviewScrollerPolicyChangeCancelsActiveScrollerGrab()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		for (var i = 0u; i < 12; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, child,
				APTR.FromPointer(0x5210000 + i), InsertBottom));
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview, 0, 0,
			40, 40));

		var intui = APTR.FromPointer(0x7E00);
		var packet = APTR.FromPointer(0x7F00);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectDown, 0, 0, 32, 8));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseMove, 0, 0, 0, 32, 24));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.True(MuiListviewCore.GetScrollerState(ref platform, State, listview,
			out _, out _, out var movedFirst, out _));
		Assert.True(movedFirst > 0);

		// Removing the scroller changes pointer geometry and releases the named
		// grab before a later movement can write First again.
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvScrollerPos, ScrollerPosNone, false));
		Assert.True(MuiListviewCore.GetScrollerState(ref platform, State, listview,
			out _, out _, out var firstAfterPolicy, out _));
		Assert.Equal(movedFirst, firstAfterPolicy);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseMove, 0, 0, 0, 32, 32));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(0u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.True(MuiListviewCore.GetScrollerState(ref platform, State, listview,
			out _, out _, out var firstAfterMove, out _));
		Assert.Equal(firstAfterPolicy, firstAfterMove);
	}

	[Fact]
	public void ListviewDispatcherForwardsListMethodsToChild()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		var packet = APTR.FromPointer(0x7000);
		// MUIM_List_InsertSingle(entry, Bottom) sent to the *listview*.
		platform.WriteUInt32(packet, 0, 0x804254d5u);
		platform.WriteUInt32(packet, 4, 0x4400001);
		platform.WriteUInt32(packet, 8, unchecked((uint)InsertBottom));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(1u, MuiListCore.EntryCount(ref platform, State, child));
		Assert.Equal(APTR.FromPointer(0x4400001), MuiListCore.GetEntry(ref platform,
			State, child, 0, APTR.Null));
		// Image lifecycle methods also forward to the owned child list.
		platform.WriteUInt32(packet, 0, ListCreateImage);
		platform.WriteUInt32(packet, 4, 0x4400020);
		platform.WriteUInt32(packet, 8, 5);
		var image = APTR.FromPointer(MuiCollectionDispatcher.Dispatch(ref platform,
			State, listview, packet));
		Assert.NotEqual(APTR.Null, image);
		Assert.Equal(1u, MuiListCore.ImageCount(ref platform, State, child));
		platform.WriteUInt32(packet, 0, ListDeleteImage);
		platform.WriteUInt32(packet, 4, image.Raw);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			listview, packet));
		Assert.Equal(0u, MuiListCore.ImageCount(ref platform, State, child));
	}

	[Fact]
	public void ListviewDisposalReleasesChildAndBalancesAllocations()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out var floattextClass, out var otherClass, 0x80000);
		var list = MuiListCore.CreateList(ref platform, State, listClass, APTR.Null);
		for (var i = 0u; i < 3; i++)
			MuiListCore.InsertSingle(ref platform, State, list,
				APTR.FromPointer(0x5500000 + i), InsertBottom);
		var tags = APTR.FromPointer(0x3000);
		platform.WriteUInt32(tags, 0, LvList);
		platform.WriteUInt32(tags, 4, list.Raw);
		platform.WriteUInt32(tags, 8, 0);
		var listview = MuiListviewCore.CreateListview(ref platform, State,
			listviewClass, tags);
		Assert.NotEqual(APTR.Null, listview);
		// Disposing the parent disposes the adopted child list too.
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			listview));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listviewClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			floattextClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	// ---------------------------------------------------------------- Floattext

	[Fact]
	public void FloattextSplitsParagraphsIntoRows()
	{
		var platform = CreatePlatform(out _, out _, out var floattextClass, out _,
			0x80000);
		var text = APTR.FromPointer(0x4000);
		platform.WriteCString(text, "alpha\nbravo\ncharlie");
		var floattext = CreateFloattext(ref platform, floattextClass, text, 0, 0, 0);
		Assert.Equal(3u, MuiListCore.EntryCount(ref platform, State, floattext));
		Assert.Equal("alpha", Row(ref platform, floattext, 0));
		Assert.Equal("bravo", Row(ref platform, floattext, 1));
		Assert.Equal("charlie", Row(ref platform, floattext, 2));
	}

	[Fact]
	public void FloattextDropsSkipCharsAndExpandsTabs()
	{
		var platform = CreatePlatform(out _, out _, out var floattextClass, out _,
			0x80000);
		var text = APTR.FromPointer(0x4000);
		// "a" TAB "b", plus a control char (0x01) that must be skipped.
		platform.WriteUInt8(text, 0, (byte)'a');
		platform.WriteUInt8(text, 1, 0x01);
		platform.WriteUInt8(text, 2, (byte)'\t');
		platform.WriteUInt8(text, 3, (byte)'b');
		platform.WriteUInt8(text, 4, 0);
		var skip = APTR.FromPointer(0x4100);
		platform.WriteUInt8(skip, 0, 0x01);
		platform.WriteUInt8(skip, 1, 0);
		// TabSize 4: after "a" the tab expands to the next stop (3 spaces).
		var floattext = CreateFloattext(ref platform, floattextClass, text, skip, 4,
			0);
		Assert.Equal(1u, MuiListCore.EntryCount(ref platform, State, floattext));
		Assert.Equal("a   b", Row(ref platform, floattext, 0));
	}

	[Fact]
	public void FloattextWrapsByWidthAtWordBoundaries()
	{
		var platform = CreatePlatform(out _, out _, out var floattextClass, out _,
			0x80000);
		var text = APTR.FromPointer(0x4000);
		platform.WriteCString(text, "one two three");
		// Width 56 / 8px cell = 7 columns.
		var floattext = CreateFloattextWithWidth(ref platform, floattextClass, text,
			56, 0);
		Assert.Equal(3u, MuiListCore.EntryCount(ref platform, State, floattext));
		Assert.Equal("one", Row(ref platform, floattext, 0));
		Assert.Equal("two", Row(ref platform, floattext, 1));
		Assert.Equal("three", Row(ref platform, floattext, 2));
	}

	[Fact]
	public void FloattextJustifyPadsWrappedLinesToWidth()
	{
		var platform = CreatePlatform(out _, out _, out var floattextClass, out _,
			0x80000);
		var text = APTR.FromPointer(0x4000);
		platform.WriteCString(text, "aa bb cc dd");
		// Width 48 / 8 = 6 columns, justify on.
		var floattext = CreateFloattextWithWidth(ref platform, floattextClass, text,
			48, 1);
		Assert.Equal(2u, MuiListCore.EntryCount(ref platform, State, floattext));
		// First (wrapped) line is padded to the full 6 columns.
		Assert.Equal("aa  bb", Row(ref platform, floattext, 0));
		// The paragraph-final line is left unjustified.
		Assert.Equal("cc dd", Row(ref platform, floattext, 1));
	}

	[Fact]
	public void FloattextGetTextReturnsOwnedCopyAndNullClears()
	{
		var platform = CreatePlatform(out _, out _, out var floattextClass, out _,
			0x80000);
		var text = APTR.FromPointer(0x4000);
		platform.WriteCString(text, "owned");
		var floattext = CreateFloattext(ref platform, floattextClass, text, 0, 0, 0);
		Assert.True(MuiFloattextCore.GetAttribute(ref platform, State, floattext,
			FtText, out var stored));
		Assert.NotEqual(0u, stored);
		Assert.NotEqual(text.Raw, stored); // private copy, not caller buffer
		Assert.Equal("owned", ReadCString(ref platform, APTR.FromPointer(stored)));
		// Setting the text to NULL clears the contents and the row set.
		Assert.True(MuiFloattextCore.SetAttribute(ref platform, State, floattext,
			FtText, 0));
		Assert.Equal(0u, MuiListCore.EntryCount(ref platform, State, floattext));
		Assert.True(MuiFloattextCore.GetAttribute(ref platform, State, floattext,
			FtText, out var cleared));
		Assert.Equal(0u, cleared);
	}

	[Fact]
	public void NamedFloattextStateTracksOwnedPointersAndPolicyWrites()
	{
		var platform = CreatePlatform(out _, out _, out var floattextClass, out _,
			0x80000);
		var text = APTR.FromPointer(0x4300);
		platform.WriteCString(text, "one two three");
		var skip = APTR.FromPointer(0x4400);
		platform.WriteUInt8(skip, 0, 0x01);
		platform.WriteUInt8(skip, 1, 0);
		var floattext = CreateFloattext(ref platform, floattextClass, text,
			skip, 4, 1);
		Assert.True(MuiFloattextCore.TryReadState(ref platform, State, floattext,
			out var current));
		Assert.NotEqual(APTR.Null, current.Text);
		Assert.NotEqual(text.Raw, current.Text.Raw);
		Assert.NotEqual(APTR.Null, current.SkipChars);
		Assert.NotEqual(skip.Raw, current.SkipChars.Raw);
		Assert.Equal(4u, current.TabSize);
		Assert.Equal(1u, current.Justify);
		Assert.Equal(0u, current.Width);

		Assert.True(MuiFloattextCore.SetAttribute(ref platform, State, floattext,
			FtJustify, 7));
		Assert.True(MuiFloattextCore.SetAttribute(ref platform, State, floattext,
			FtTabSize, 2));
		Assert.True(MuiFloattextCore.TryReadState(ref platform, State, floattext,
			out current));
		Assert.Equal(1u, current.Justify);
		Assert.Equal(2u, current.TabSize);

		var packet = APTR.FromPointer(0x4500);
		platform.WriteUInt32(packet, 0, 0x8042549Au); // MUIM_Set
		platform.WriteUInt32(packet, 4, FtJustify);
		platform.WriteUInt32(packet, 8, 0);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			floattext, packet));
		Assert.True(MuiFloattextCore.TryReadState(ref platform, State, floattext,
			out current));
		Assert.Equal(0u, current.Justify);
	}

	[Fact]
	public void FloattextPolicyUsesNamedGuestRecord()
	{
		var platform = CreatePlatform(out _, out _, out var floattextClass, out _,
			0x80000);
		var text = APTR.FromPointer(0x4600);
		platform.WriteCString(text, "policy text");
		var floattext = CreateFloattext(ref platform, floattextClass, text, 0, 4, 1);
		Assert.True(MuiFloattextCore.TryGetPolicyState(ref platform, State,
			floattext, out var policy));
		Assert.Equal(MuiFloattextPolicyState.Cookie, policy.Magic);
		Assert.NotEqual(APTR.Null, policy.Text);
		Assert.Equal(4u, policy.TabSize);
		Assert.Equal(1u, policy.Justify);

		// A raw backing-word write does not replace the guest policy record; the
		// parser-facing state remains the typed projection until SetAttribute
		// performs the normal synchronization boundary.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			floattext, FtTabSize, 99, false));
		Assert.True(MuiFloattextCore.TryReadState(ref platform, State, floattext,
			out var state));
		Assert.Equal(4u, state.TabSize);
		Assert.True(MuiFloattextCore.SetAttribute(ref platform, State, floattext,
			FtTabSize, 2, false));
		Assert.True(MuiFloattextCore.TryGetPolicyState(ref platform, State,
			floattext, out policy));
		Assert.Equal(2u, policy.TabSize);
		Assert.True(MuiFloattextCore.TryReadState(ref platform, State, floattext,
			out state));
		Assert.Equal(2u, state.TabSize);
	}

	[Fact]
	public void FloattextPolicyGettersPreferNamedRecord()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out var floattextClass, out var otherClass, 0x80000);
		var text = APTR.FromPointer(0x4800);
		platform.WriteCString(text, "getter text");
		var skip = APTR.FromPointer(0x4900);
		platform.WriteUInt8(skip, 0, 1);
		platform.WriteUInt8(skip, 1, 0);
		var floattext = CreateFloattext(ref platform, floattextClass, text,
			skip, 4, 1);
		Assert.NotEqual(APTR.Null, floattext);

		Assert.True(MuiFloattextCore.TryGetPolicyState(ref platform, State,
			floattext, out var policy));
		var ownedText = policy.Text.Raw;
		var ownedSkip = policy.SkipChars.Raw;
		Assert.Equal(4u, policy.TabSize);
		Assert.Equal(1u, policy.Justify);
		Assert.NotEqual(0u, ownedText);
		Assert.NotEqual(0u, ownedSkip);

		// Deliberately stale scalar nodes must not replace the guest-resident
		// policy record used by public getters.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			floattext, FtText, 0, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			floattext, FtSkipChars, 0, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			floattext, FtTabSize, 99, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			floattext, FtJustify, 0, false));
		Assert.True(MuiFloattextCore.TryGetPolicyState(ref platform, State,
			floattext, out policy));
		Assert.Equal(4u, policy.TabSize);

		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			floattext, FtText, out var gotText));
		Assert.Equal(ownedText, gotText);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			floattext, FtSkipChars, out var gotSkip));
		Assert.Equal(ownedSkip, gotSkip);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			floattext, FtTabSize, out var gotTabSize));
		Assert.Equal(4u, gotTabSize);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			floattext, FtJustify, out var gotJustify));
		Assert.Equal(1u, gotJustify);

		var getMessage = APTR.FromPointer(0x4A00);
		var getStorage = APTR.FromPointer(0x4A40);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			FtTabSize));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			floattext, getMessage));
		Assert.Equal(4u, platform.ReadUInt32(getStorage, 0));

		Assert.True(MuiFloattextCore.TryGetPolicyState(ref platform, State,
			floattext, out policy));
		Assert.Equal(4u, policy.TabSize);
		Assert.Equal(1u, policy.Justify);
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			floattext));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listviewClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			floattextClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void FloattextWrapWidthUsesAreaGeometryRecord()
	{
		var platform = CreatePlatform(out _, out _, out var floattextClass, out _,
			0x80000);
		var text = APTR.FromPointer(0x4700);
		platform.WriteCString(text, "one two three");
		var floattext = CreateFloattextWithWidth(ref platform, floattextClass,
			text, 0, 0);
		Assert.Equal(1u, MuiListCore.EntryCount(ref platform, State, floattext));

		// Layout publishes the typed Area geometry record.  Floattext's parser
		// must consume that record even though its own explicit Width policy was
		// created with zero, because wrapping follows the laid-out render width.
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, floattext, 0, 0,
			40, 20));
		Assert.True(MuiAreaLayoutCore.TryGetGeometryStateRecord(ref platform,
			State, floattext, out var geometry));
		Assert.Equal(40, geometry.Width);
		Assert.True(MuiFloattextCore.TryReadState(ref platform, State, floattext,
			out var state));
		Assert.Equal(40u, state.Width);

		Assert.True(MuiFloattextCore.Rebuild(ref platform, State, floattext));
		Assert.Equal(3u, MuiListCore.EntryCount(ref platform, State, floattext));
		Assert.Equal("one", Row(ref platform, floattext, 0));
		Assert.Equal("two", Row(ref platform, floattext, 1));
		Assert.Equal("three", Row(ref platform, floattext, 2));
	}

	[Fact]
	public void FloattextAppendGrowsContentsAndRebuildsRows()
	{
		var platform = CreatePlatform(out _, out _, out var floattextClass, out _,
			0x80000);
		var text = APTR.FromPointer(0x4000);
		platform.WriteCString(text, "first\n");
		var floattext = CreateFloattext(ref platform, floattextClass, text, 0, 0, 0);
		Assert.Equal(1u, MuiListCore.EntryCount(ref platform, State, floattext));
		var more = APTR.FromPointer(0x4100);
		platform.WriteCString(more, "second\nthird");
		// Drive the append through the dispatcher to exercise the method path.
		var packet = APTR.FromPointer(0x4200);
		platform.WriteUInt32(packet, 0, FtAppend);
		platform.WriteUInt32(packet, 4, more.Raw);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			floattext, packet));
		Assert.Equal(3u, MuiListCore.EntryCount(ref platform, State, floattext));
		Assert.Equal("first", Row(ref platform, floattext, 0));
		Assert.Equal("second", Row(ref platform, floattext, 1));
		Assert.Equal("third", Row(ref platform, floattext, 2));
	}

	[Fact]
	public void FloattextResetTextRebuildsAtomically()
	{
		var platform = CreatePlatform(out _, out _, out var floattextClass, out _,
			0x80000);
		var first = APTR.FromPointer(0x4000);
		platform.WriteCString(first, "x\ny\nz");
		var floattext = CreateFloattext(ref platform, floattextClass, first, 0, 0,
			0);
		Assert.Equal(3u, MuiListCore.EntryCount(ref platform, State, floattext));
		var second = APTR.FromPointer(0x4100);
		platform.WriteCString(second, "only");
		Assert.True(MuiFloattextCore.SetAttribute(ref platform, State, floattext,
			FtText, second.Raw));
		Assert.Equal(1u, MuiListCore.EntryCount(ref platform, State, floattext));
		Assert.Equal("only", Row(ref platform, floattext, 0));
	}

	[Fact]
	public void FloattextDisposalFreesBuffersAndRowsWithoutLeak()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out var floattextClass, out var otherClass, 0x80000);
		var text = APTR.FromPointer(0x4000);
		platform.WriteCString(text, "line one\nline two\nline three");
		var skip = APTR.FromPointer(0x4200);
		platform.WriteUInt8(skip, 0, 0x01);
		platform.WriteUInt8(skip, 1, 0);
		var floattext = CreateFloattext(ref platform, floattextClass, text, skip, 8,
			0);
		Assert.True(MuiListCore.EntryCount(ref platform, State, floattext) >= 3);
		// Append once more so the append scratch path is exercised before dispose.
		var more = APTR.FromPointer(0x4300);
		platform.WriteCString(more, "\nline four");
		Assert.True(MuiFloattextCore.Append(ref platform, State, floattext, more));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			floattext));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listviewClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			floattextClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	// -------------------------------------------------------------- test helpers

	[Fact]
	public void MultiSelectPolicyControlsAccumulationVersusSingleSelect()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		for (var i = 0u; i < 4; i++)
			MuiListCore.InsertSingle(ref platform, State, child,
				APTR.FromPointer(0x5200000 + i), InsertBottom);

		// Default policy (no shift): each click clears others and selects one.
		Assert.True(MuiListviewCore.HandleClick(ref platform, State, listview, 0, 1,
			0, false));
		Assert.True(MuiListviewCore.HandleClick(ref platform, State, listview, 2, 1,
			0, false));
		Assert.Equal(1u, SelectedCount(ref platform, child));
		Assert.True(IsSelected(ref platform, child, 2));
		Assert.False(IsSelected(ref platform, child, 0));

		// Shifted policy + shift held: the selection accumulates.
		MuiListviewCore.SetAttribute(ref platform, State, listview, LvMultiSelect,
			MultiSelectShifted, false);
		MuiListCore.Select(ref platform, State, child, SelectAll, SelectOff,
			APTR.Null);
		Assert.True(MuiListviewCore.HandleClick(ref platform, State, listview, 0, 1,
			0, true));
		Assert.True(MuiListviewCore.HandleClick(ref platform, State, listview, 1, 1,
			0, true));
		Assert.Equal(2u, SelectedCount(ref platform, child));

		// Always policy: accumulates even without shift.
		MuiListviewCore.SetAttribute(ref platform, State, listview, LvMultiSelect,
			MultiSelectAlways, false);
		MuiListCore.Select(ref platform, State, child, SelectAll, SelectOff,
			APTR.Null);
		Assert.True(MuiListviewCore.HandleClick(ref platform, State, listview, 0, 1,
			0, false));
		Assert.True(MuiListviewCore.HandleClick(ref platform, State, listview, 3, 1,
			0, false));
		Assert.Equal(2u, SelectedCount(ref platform, child));
	}

	[Fact]
	public void ListviewMultiSelectionRemainsAvailableDuringEditMode()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out var floattextClass, out var otherClass, 0x80000);
		var stringName = APTR.FromPointer(0x1200);
		platform.WriteCString(stringName, "String.mui");
		var stringClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			stringName, APTR.Null, 0, APTR.FromPointer(1), false);
		Assert.NotEqual(APTR.Null, stringClass);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		var first = APTR.FromPointer(0x5500);
		var second = APTR.FromPointer(0x5540);
		platform.WriteCString(first, "first");
		platform.WriteCString(second, "second");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, first,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, child, second,
			InsertBottom));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, child,
			ListEditable, 1));
		Assert.True(MuiListCore.Select(ref platform, State, child, 0, SelectOn,
			APTR.Null));
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			LvMultiSelect, MultiSelectAlways, false));
		Assert.True(MuiListCore.Edit(ref platform, State, child, 0, 0));

		// MorphOS 3.20 permits another row to join the selection while the
		// inline editor remains active.  HandleClick must not retire the named
		// edit session merely because the active row moves.
		Assert.True(MuiListviewCore.HandleClick(ref platform, State, listview, 1,
			1, 0, false));
		Assert.Equal(2u, SelectedCount(ref platform, child));
		Assert.True(MuiListCore.EditDone(ref platform, State, child, 0, 0,
			first, APTR.Null));

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			listview));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listviewClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			floattextClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			stringClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void MultiTestHookDeniesPerEntryMultiselection()
	{
		var platform = CreatePlatform(out var listClass, out var listviewClass,
			out _, out _, 0x80000);
		var listview = CreateListviewWith(ref platform, listClass, listviewClass);
		var child = MuiListviewCore.ChildList(ref platform, State, listview);
		var e0 = APTR.FromPointer(0x5300000);
		var e1 = APTR.FromPointer(0x5300040);
		MuiListCore.InsertSingle(ref platform, State, child, e0, InsertBottom);
		MuiListCore.InsertSingle(ref platform, State, child, e1, InsertBottom);
		// Accumulate without needing shift so the hook is the only gate.
		MuiListviewCore.SetAttribute(ref platform, State, listview, LvMultiSelect,
			MultiSelectAlways, false);
		// struct Hook: h_Entry sentinel at +8, h_Data at +16 records the entry
		// that must be denied. A0 must deliver the hook so h_Data is reachable.
		var hook = APTR.FromPointer(0x5400);
		var hookData = APTR.FromPointer(0x5440);
		platform.WriteUInt32(hook, 8, MuiHeadlessTestPlatform.HookEntryMultiTest);
		platform.WriteUInt32(hook, 16, hookData.Raw);
		platform.WriteUInt32(hookData, 0, e1.Raw); // deny e1
		// MUIA_List_MultiTestHook is a List attribute: setting it on the listview
		// forwards to the child list, where HandleClick consults it.
		Assert.True(MuiListviewCore.SetAttribute(ref platform, State, listview,
			ListMultiTestHook, hook.Raw, false));
		Assert.True(MuiListviewCore.HandleClick(ref platform, State, listview, 0, 1,
			0, false));
		Assert.True(MuiListviewCore.HandleClick(ref platform, State, listview, 1, 1,
			0, false));
		// The hook was consulted with A0 = hook base and reached its h_Data.
		Assert.Equal(hook, platform.LastHookBase);
		Assert.Equal(hookData, platform.LastHookData);
		// e0 was permitted (selected); e1 was denied (left unselected).
		Assert.True(IsSelected(ref platform, child, 0));
		Assert.False(IsSelected(ref platform, child, 1));
	}

	private static uint SelectedCount(ref MuiHeadlessTestPlatform platform,
		APTR child)
	{
		var storage = APTR.FromPointer(0x7F00);
		MuiListCore.Select(ref platform, State, child, SelectAll, SelectAsk,
			storage);
		return platform.ReadUInt32(storage, 0);
	}

	private static bool IsSelected(ref MuiHeadlessTestPlatform platform, APTR child,
		int index)
	{
		var storage = APTR.FromPointer(0x7F10);
		MuiListCore.Select(ref platform, State, child, index, SelectAsk, storage);
		return platform.ReadUInt32(storage, 0) != 0;
	}

	private static APTR CreateListviewWith(ref MuiHeadlessTestPlatform platform,
		APTR listClass, APTR listviewClass)
	{
		var list = MuiListCore.CreateList(ref platform, State, listClass, APTR.Null);
		var tags = APTR.FromPointer(0x3000);
		platform.WriteUInt32(tags, 0, LvList);
		platform.WriteUInt32(tags, 4, list.Raw);
		platform.WriteUInt32(tags, 8, 0);
		return MuiListviewCore.CreateListview(ref platform, State, listviewClass,
			tags);
	}

	private static APTR CreateFloattext(ref MuiHeadlessTestPlatform platform,
		APTR floattextClass, APTR text, uint skip, uint tabSize, uint justify)
	{
		var tags = APTR.FromPointer(0x3800);
		var offset = 0;
		if (text.IsNotNull)
		{
			platform.WriteUInt32(tags, offset, FtText);
			platform.WriteUInt32(tags, offset + 4, text.Raw);
			offset += 8;
		}
		if (skip != 0)
		{
			platform.WriteUInt32(tags, offset, FtSkipChars);
			platform.WriteUInt32(tags, offset + 4, skip);
			offset += 8;
		}
		if (tabSize != 0)
		{
			platform.WriteUInt32(tags, offset, FtTabSize);
			platform.WriteUInt32(tags, offset + 4, tabSize);
			offset += 8;
		}
		if (justify != 0)
		{
			platform.WriteUInt32(tags, offset, FtJustify);
			platform.WriteUInt32(tags, offset + 4, justify);
			offset += 8;
		}
		platform.WriteUInt32(tags, offset, 0);
		return MuiFloattextCore.CreateFloattext(ref platform, State, floattextClass,
			tags);
	}

	private static void DisposeListview(ref MuiHeadlessTestPlatform platform,
		APTR listview, APTR listClass, APTR listviewClass, APTR floattextClass,
		APTR otherClass)
	{
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			listview));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listviewClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			floattextClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	private static APTR CreateFloattextWithWidth(ref MuiHeadlessTestPlatform platform,
		APTR floattextClass, APTR text, uint width, uint justify)
	{
		var tags = APTR.FromPointer(0x3800);
		platform.WriteUInt32(tags, 0, FtText);
		platform.WriteUInt32(tags, 4, text.Raw);
		platform.WriteUInt32(tags, 8, Width);
		platform.WriteUInt32(tags, 12, width);
		platform.WriteUInt32(tags, 16, FtJustify);
		platform.WriteUInt32(tags, 20, justify);
		platform.WriteUInt32(tags, 24, 0);
		return MuiFloattextCore.CreateFloattext(ref platform, State, floattextClass,
			tags);
	}

	private static string Row(ref MuiHeadlessTestPlatform platform, APTR obj,
		int index)
	{
		var entry = MuiListCore.GetEntry(ref platform, State, obj, index, APTR.Null);
		return ReadCString(ref platform, entry);
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

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj, attribute,
			out var value);
		return value;
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR listClass,
		out APTR listviewClass, out APTR floattextClass, out APTR otherClass,
		int size)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, size, 0x8000, State);
		var listName = APTR.FromPointer(0x1100);
		var listviewName = APTR.FromPointer(0x1140);
		var floattextName = APTR.FromPointer(0x1180);
		var otherName = APTR.FromPointer(0x11C0);
		platform.WriteCString(listName, "List.mui");
		platform.WriteCString(listviewName, "Listview.mui");
		platform.WriteCString(floattextName, "Floattext.mui");
		platform.WriteCString(otherName, "Group.mui");
		MuiHeadlessObjectCore.Initialize(ref platform, State);
		listClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			listName, APTR.Null, 0, APTR.FromPointer(1), false);
		listviewClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			listviewName, APTR.Null, 0, APTR.FromPointer(1), false);
		floattextClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			floattextName, APTR.Null, 0, APTR.FromPointer(1), false);
		otherClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			otherName, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
