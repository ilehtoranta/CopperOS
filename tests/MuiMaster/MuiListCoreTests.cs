using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListCoreTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	// MUIV_List_* selectors exercised by the tests.
	private const int InsertBottom = -3;
	private const int InsertTop = 0;
	private const int InsertSorted = -2;
	private const int RemoveActive = -1;
	private const int RemoveSelected = -3;
	private const int GetEntryActive = -1;
	private const int SelectAll = -2;
	private const uint SelectOn = 1;
	private const uint SelectToggle = 2;
	private const uint SelectOff = 0;
	private const uint SelectAsk = 3;
	private const int NextSelectedStart = -1;
	private const uint ActiveAttr = 0x8042391cu;
	private const uint EntriesAttr = 0x80421654u;
	private const uint ConstructHookAttr = 0x8042894fu;
	private const uint DestructHookAttr = 0x804297ceu;
	private const uint FirstAttr = 0x804238d4u;
	private const uint VisibleAttr = 0x8042191fu;
	private const uint SelectChangeAttr = 0x8042178fu;
	private const uint InputAttr = 0x8042682du;
	private const uint MultiSelectAttr = 0x80427e08u;
	private const uint ScrollerPosAttr = 0x8042b1b4u;
	private const uint AgainClickAttr = 0x804214c2u;
	private const uint ClickColumnAttr = 0x8042d1b3u;
	private const uint DefClickColumnAttr = 0x8042b296u;
	private const uint DoubleClickAttr = 0x80424635u;
	private const uint QuietAttr = 0x8042d8c7u;
	private const uint FontAttr = 0x8042be50u;
	private const uint FormatAttr = 0x80423c0au;
	private const uint MaxColumnsAttr = 0x8042a98bu;
	private const uint AdjustHeightAttr = 0x8042850du;
	private const uint AdjustWidthAttr = 0x8042354au;
	private const uint StripesAttr = 0x8042a308u;
	private const uint DropMarkAttr = 0x8042aba6u;
	private const uint ShowDropMarksAttr = 0x8042c6f3u;
	private const uint DragSortableAttr = 0x80426099u;
	private const uint DragTypeAttr = 0x80425cd3u;
	private const uint AutoVisibleAttr = 0x8042a445u;
	private const uint SortColumnAttr = 0x8042cafbu;
	private const uint TitleClickAttr = 0x80422fd9u;
	private const uint DisplayHookAttr = 0x8042b4d5u;
	private const uint MultiTestHookAttr = 0x8042c2c6u;
	private const uint MinLineHeightAttr = 0x8042d1c3u;
	private const uint AutoLineHeightAttr = 0x8042bc08u;
	private const uint LineHeightAttr = 0x80425880u;
	private const uint TopPixelAttr = 0x80429df3u;
	private const uint TotalPixelAttr = 0x8042a8f5u;
	private const uint VisiblePixelAttr = 0x804273e9u;
	private const uint TitleAttr = 0x80423e66u;
	private const uint TitleArrayAttr = 0x80427d95u;
	private const uint HScrollerVisibilityAttr = 0x804280a6u;
	private const uint SourceArrayAttr = 0x8042c0a0u;
	private const uint EditableAttr = 0x8042f9b9u;
	private const uint LeftEdgeAttr = 0x8042bec6u;
	private const uint TopEdgeAttr = 0x8042509bu;
	private const uint WidthAttr = 0x8042b59cu;
	private const uint HeightAttr = 0x80423237u;
	private const uint StringContentsAttr = 0x80428ffdu;
	private const uint PoolAttr = 0x80423431u;
	private const uint PoolPuddleSizeAttr = 0x8042a4ebu;
	private const uint PoolThreshSizeAttr = 0x8042c48cu;
	private const uint CompareHookAttr = 0x80425c14u;
	private const uint LayoutMethod = 0x8042845bu;
	private const uint DrawMethod = 0x80426f3fu;
	private const uint AskMinMaxMethod = 0x80423874u;
	private const uint TestPosMethod = 0x80425f48u;
	private const uint CreateImageMethod = 0x80429804u;
	private const uint DeleteImageMethod = 0x80420f58u;
	private const uint HookString = 0xFFFFFFFFu;
	private const uint HookStringArray = 0xFFFFFFFEu;
	private const uint EveryTime = 1233727793;

	[Fact]
	public void ListReadOnlyAndConstructionProjectionsRejectRuntimeWrites()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		var readOnly = new[]
		{
			EntriesAttr, VisibleAttr, SelectChangeAttr, DropMarkAttr,
			InputAttr, MultiSelectAttr, ScrollerPosAttr,
			LineHeightAttr, TotalPixelAttr, VisiblePixelAttr,
			MaxColumnsAttr, SourceArrayAttr,
		};
		foreach (var attribute in readOnly)
			Assert.False(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
				attribute, 1, false));
		Assert.Equal(0u, Get(ref platform, list, EntriesAttr));
		Assert.Equal(0u, Get(ref platform, list, SelectChangeAttr));
		Assert.Equal(0u, Get(ref platform, list, TotalPixelAttr));
		Assert.Equal(0u, Get(ref platform, list, VisiblePixelAttr));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
	}

	[Fact]
	public void ListInteractionConstructionTagsRemainAvailableButImmutable()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var tags = APTR.FromPointer(0x2380);
		platform.WriteUInt32(tags, 0, InputAttr);
		platform.WriteUInt32(tags, 4, 1);
		platform.WriteUInt32(tags, 8, MultiSelectAttr);
		platform.WriteUInt32(tags, 12, 3);
		platform.WriteUInt32(tags, 16, ScrollerPosAttr);
		platform.WriteUInt32(tags, 20, 1);
		platform.WriteUInt32(tags, 24, 0);

		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.Equal(1u, Get(ref platform, list, InputAttr));
		Assert.Equal(3u, Get(ref platform, list, MultiSelectAttr));
		Assert.Equal(1u, Get(ref platform, list, ScrollerPosAttr));
		Assert.False(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
			InputAttr, 0));
		Assert.False(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
			MultiSelectAttr, 0));
		Assert.False(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
			ScrollerPosAttr, 0));
		Assert.Equal(1u, Get(ref platform, list, InputAttr));
		Assert.Equal(3u, Get(ref platform, list, MultiSelectAttr));
		Assert.Equal(1u, Get(ref platform, list, ScrollerPosAttr));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
	}

	[Fact]
	public void ListInteractionPolicyUsesNamedRecordAndNormalizesConstructionValues()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var policyStorage = APTR.FromPointer(0x23A0);
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, policyStorage,
			MuiListCore.MuiListStateRecordKind.InteractionPolicy,
			MuiListCore.MuiListStateField.Magic,
			MuiListCore.MuiListInteractionPolicyState.Cookie));
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, policyStorage,
			MuiListCore.MuiListStateRecordKind.InteractionPolicy,
			MuiListCore.MuiListStateField.MultiSelect, 2));
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, policyStorage,
			MuiListCore.MuiListStateRecordKind.InteractionPolicy,
			MuiListCore.MuiListStateField.MultiSelect, out var shifted));
		Assert.Equal(2u, shifted);

		var tags = APTR.FromPointer(0x23C0);
		platform.WriteUInt32(tags, 0, InputAttr);
		platform.WriteUInt32(tags, 4, 9);
		platform.WriteUInt32(tags, 8, MultiSelectAttr);
		platform.WriteUInt32(tags, 12, 99);
		platform.WriteUInt32(tags, 16, ScrollerPosAttr);
		platform.WriteUInt32(tags, 20, 99);
		platform.WriteUInt32(tags, 24, 0);

		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.TryGetInteractionPolicy(ref platform, State,
			list, out var policy));
		Assert.Equal(MuiListCore.MuiListInteractionPolicyState.Cookie,
			policy.Magic);
		Assert.Equal(1u, policy.Input);
		Assert.Equal(1u, policy.MultiSelect);
		Assert.Equal(0u, policy.ScrollerPos);
		Assert.Equal(policy.Input, Get(ref platform, list, InputAttr));
		Assert.Equal(policy.MultiSelect, Get(ref platform, list,
			MultiSelectAttr));
		Assert.Equal(policy.ScrollerPos, Get(ref platform, list,
			ScrollerPosAttr));
		Assert.False(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
			MultiSelectAttr, 3));
		Assert.False(MuiListCore.SetAttribute(ref platform, State, list,
			ScrollerPosAttr, 2));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
	}

	[Fact]
	public void ListClickStateUsesNamedRecordAndKeepsClickAttributesCoherent()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var clickStorage = APTR.FromPointer(0x2460);
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, clickStorage,
			MuiListCore.MuiListStateRecordKind.ClickState,
			MuiListCore.MuiListStateField.Magic,
			MuiListCore.MuiListClickState.Cookie));
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, clickStorage,
			MuiListCore.MuiListStateRecordKind.ClickState,
			MuiListCore.MuiListStateField.ClickColumn, 4));
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, clickStorage,
			MuiListCore.MuiListStateRecordKind.ClickState,
			MuiListCore.MuiListStateField.ClickColumn, out var column));
		Assert.Equal(4u, column);

		var tags = APTR.FromPointer(0x2480);
		platform.WriteUInt32(tags, 0, ClickColumnAttr);
		platform.WriteUInt32(tags, 4, 3);
		platform.WriteUInt32(tags, 8, AgainClickAttr);
		platform.WriteUInt32(tags, 12, 7);
		platform.WriteUInt32(tags, 16, DoubleClickAttr);
		platform.WriteUInt32(tags, 20, 0);
		platform.WriteUInt32(tags, 24, DefClickColumnAttr);
		platform.WriteUInt32(tags, 28, 9);
		platform.WriteUInt32(tags, 32, 0);

		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.TryGetClickState(ref platform, State, list,
			out var clickState));
		Assert.Equal(MuiListCore.MuiListClickState.Cookie, clickState.Magic);
		Assert.Equal(3u, clickState.ClickColumn);
		Assert.Equal(1u, clickState.AgainClick);
		Assert.Equal(0u, clickState.DoubleClick);
		Assert.Equal(9u, clickState.DefClickColumn);
		Assert.Equal(clickState.ClickColumn, Get(ref platform, list,
			ClickColumnAttr));
		Assert.Equal(clickState.AgainClick, Get(ref platform, list,
			AgainClickAttr));
		Assert.Equal(clickState.DoubleClick, Get(ref platform, list,
			DoubleClickAttr));
		Assert.Equal(clickState.DefClickColumn, Get(ref platform, list,
			DefClickColumnAttr));

		Assert.True(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
			ClickColumnAttr, 6));
		Assert.True(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
			AgainClickAttr, 0));
		Assert.True(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
			DoubleClickAttr, 5));
		Assert.True(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
			DefClickColumnAttr, 11));
		Assert.True(MuiListCore.TryGetClickState(ref platform, State, list,
			out clickState));
		Assert.Equal(6u, clickState.ClickColumn);
		Assert.Equal(0u, clickState.AgainClick);
		Assert.Equal(1u, clickState.DoubleClick);
		Assert.Equal(11u, clickState.DefClickColumn);
		Assert.Equal(clickState.DoubleClick, Get(ref platform, list,
			DoubleClickAttr));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
	}

	[Fact]
	public void ListHookPolicyUsesNamedRecordAndKeepsHookAttributesCoherent()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var hookStorage = APTR.FromPointer(0x2520);
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, hookStorage,
			MuiListCore.MuiListStateRecordKind.HookPolicy,
			MuiListCore.MuiListStateField.Magic,
			MuiListCore.MuiListHookPolicyState.Cookie));
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, hookStorage,
			MuiListCore.MuiListStateRecordKind.HookPolicy,
			MuiListCore.MuiListStateField.MultiTestHook, 0x12345678u));
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, hookStorage,
			MuiListCore.MuiListStateRecordKind.HookPolicy,
			MuiListCore.MuiListStateField.MultiTestHook, out var testHook));
		Assert.Equal(0x12345678u, testHook);

		var tags = APTR.FromPointer(0x2540);
		platform.WriteUInt32(tags, 0, ConstructHookAttr);
		platform.WriteUInt32(tags, 4, HookString);
		platform.WriteUInt32(tags, 8, DestructHookAttr);
		platform.WriteUInt32(tags, 12, HookString);
		platform.WriteUInt32(tags, 16, DisplayHookAttr);
		platform.WriteUInt32(tags, 20, HookStringArray);
		platform.WriteUInt32(tags, 24, CompareHookAttr);
		platform.WriteUInt32(tags, 28, HookStringArray);
		platform.WriteUInt32(tags, 32, MultiTestHookAttr);
		platform.WriteUInt32(tags, 36, 0x12345678u);
		platform.WriteUInt32(tags, 40, 0);

		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.TryGetHookPolicy(ref platform, State, list,
			out var policy));
		Assert.Equal(MuiListCore.MuiListHookPolicyState.Cookie, policy.Magic);
		Assert.Equal(HookString, policy.ConstructHook);
		Assert.Equal(HookString, policy.DestructHook);
		Assert.Equal(HookStringArray, policy.DisplayHook);
		Assert.Equal(HookStringArray, policy.CompareHook);
		Assert.Equal(0x12345678u, policy.MultiTestHook);
		Assert.Equal(policy.DisplayHook, Get(ref platform, list,
			DisplayHookAttr));
		Assert.Equal(policy.MultiTestHook, Get(ref platform, list,
			MultiTestHookAttr));

		Assert.True(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
			DisplayHookAttr, HookString));
		Assert.True(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
			MultiTestHookAttr, 0x23456789u));
		Assert.True(MuiListCore.TryGetHookPolicy(ref platform, State, list,
			out policy));
		Assert.Equal(HookString, policy.DisplayHook);
		Assert.Equal(0x23456789u, policy.MultiTestHook);
		Assert.Equal(policy.MultiTestHook, Get(ref platform, list,
			MultiTestHookAttr));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
	}

	[Fact]
	public void ListSortStateUsesNamedRecordAndKeepsColumnProjectionsCoherent()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var sortStorage = APTR.FromPointer(0x25A0);
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, sortStorage,
			MuiListCore.MuiListStateRecordKind.SortState,
			MuiListCore.MuiListStateField.Magic,
			MuiListCore.MuiListSortState.Cookie));
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, sortStorage,
			MuiListCore.MuiListStateRecordKind.SortState,
			MuiListCore.MuiListStateField.TitleClick, 2));
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, sortStorage,
			MuiListCore.MuiListStateRecordKind.SortState,
			MuiListCore.MuiListStateField.TitleClick, out var titleClick));
		Assert.Equal(2u, titleClick);

		var tags = APTR.FromPointer(0x25C0);
		platform.WriteUInt32(tags, 0, SortColumnAttr);
		platform.WriteUInt32(tags, 4, 9);
		platform.WriteUInt32(tags, 8, TitleClickAttr);
		platform.WriteUInt32(tags, 12, 5);
		platform.WriteUInt32(tags, 16, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.TryGetSortState(ref platform, State, list,
			out var sortState));
		Assert.Equal(MuiListCore.MuiListSortState.Cookie, sortState.Magic);
		// The default single-column format clamps an out-of-range sort column.
		Assert.Equal(0u, sortState.SortColumn);
		Assert.Equal(5u, sortState.TitleClick);
		Assert.Equal(sortState.SortColumn, Get(ref platform, list,
			SortColumnAttr));
		Assert.Equal(sortState.TitleClick, Get(ref platform, list,
			TitleClickAttr));

		Assert.True(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
			SortColumnAttr, 12));
		Assert.True(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
			TitleClickAttr, 7));
		Assert.True(MuiListCore.TryGetSortState(ref platform, State, list,
			out sortState));
		Assert.Equal(0u, sortState.SortColumn);
		Assert.Equal(7u, sortState.TitleClick);
		Assert.Equal(sortState.TitleClick, Get(ref platform, list,
			TitleClickAttr));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
	}

	[Fact]
	public void ListPresentationPolicyUsesNamedRecordAndNormalizesProjections()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var policyStorage = APTR.FromPointer(0x2600);
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, policyStorage,
			MuiListCore.MuiListStateRecordKind.PresentationPolicy,
			MuiListCore.MuiListStateField.Magic,
			MuiListCore.MuiListPresentationPolicyState.Cookie));
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, policyStorage,
			MuiListCore.MuiListStateRecordKind.PresentationPolicy,
			MuiListCore.MuiListStateField.Stripes, 1));
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, policyStorage,
			MuiListCore.MuiListStateRecordKind.PresentationPolicy,
			MuiListCore.MuiListStateField.Stripes, out var stripes));
		Assert.Equal(1u, stripes);

		var tags = APTR.FromPointer(0x2640);
		var tag = 0u;
		void Add(uint attribute, uint value)
		{
			platform.WriteUInt32(tags, unchecked((int)(tag * 8)), attribute);
			platform.WriteUInt32(tags, unchecked((int)(tag * 8 + 4)), value);
			tag++;
		}
		Add(EditableAttr, 3);
		Add(QuietAttr, 2);
		Add(AdjustHeightAttr, 9);
		Add(AdjustWidthAttr, 7);
		Add(StripesAttr, 5);
		Add(ShowDropMarksAttr, 0);
		Add(DragSortableAttr, 4);
		Add(DragTypeAttr, 9);
		Add(AutoVisibleAttr, 6);
		Add(AutoLineHeightAttr, 8);
		Add(MinLineHeightAttr, 1);
		platform.WriteUInt32(tags, unchecked((int)(tag * 8)), 0);

		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.TryGetPresentationPolicy(ref platform, State,
			list, out var policy));
		Assert.Equal(MuiListCore.MuiListPresentationPolicyState.Cookie,
			policy.Magic);
		Assert.Equal(1u, policy.Editable);
		Assert.Equal(1u, policy.Quiet);
		Assert.Equal(1u, policy.AdjustHeight);
		Assert.Equal(1u, policy.AdjustWidth);
		Assert.Equal(1u, policy.Stripes);
		Assert.Equal(0u, policy.ShowDropMarks);
		Assert.Equal(1u, policy.DragSortable);
		Assert.Equal(0u, policy.DragType);
		Assert.Equal(1u, policy.AutoVisible);
		Assert.Equal(1u, policy.AutoLineHeight);
		Assert.Equal(8u, policy.MinLineHeight);
		Assert.Equal(policy.Stripes, Get(ref platform, list, StripesAttr));
		Assert.Equal(policy.DragType, Get(ref platform, list, DragTypeAttr));
		Assert.Equal(policy.MinLineHeight,
			Get(ref platform, list, MinLineHeightAttr));

		Assert.True(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
			EditableAttr, 0));
		Assert.True(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
			QuietAttr, 0));
		Assert.True(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
			StripesAttr, 0));
		Assert.True(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
			ShowDropMarksAttr, 4));
		Assert.True(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
			DragSortableAttr, 0));
		Assert.True(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
			DragTypeAttr, 1));
		Assert.True(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
			AutoVisibleAttr, 0));
		Assert.True(MuiListCore.SetRuntimeAttribute(ref platform, State, list,
			AutoLineHeightAttr, 0));
		Assert.True(MuiListCore.TryGetPresentationPolicy(ref platform, State,
			list, out policy));
		Assert.Equal(0u, policy.Editable);
		Assert.Equal(0u, policy.Quiet);
		Assert.Equal(0u, policy.Stripes);
		Assert.Equal(1u, policy.ShowDropMarks);
		Assert.Equal(0u, policy.DragSortable);
		Assert.Equal(1u, policy.DragType);
		Assert.Equal(0u, policy.AutoVisible);
		Assert.Equal(0u, policy.AutoLineHeight);
		Assert.Equal(policy.ShowDropMarks,
			Get(ref platform, list, ShowDropMarksAttr));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
	}

	[Fact]
	public void ListHeaderCodecUsesNamedGuestFields()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2400);
		var expected = default(MuiListHeaderState);
		expected.Magic = MuiListHeaderState.Cookie;
		expected.Index = APTR.FromPointer(0x2800);
		expected.Capacity = 8;
		expected.Count = 3;
		expected.Images = APTR.FromPointer(0x2C00);

		Assert.True(MuiListHeaderCodec.Write(ref platform, address, expected));
		Assert.True(MuiListHeaderCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Index, actual.Index);
		Assert.Equal(expected.Capacity, actual.Capacity);
		Assert.Equal(expected.Count, actual.Count);
		Assert.Equal(expected.Images, actual.Images);
		Assert.False(MuiListHeaderCodec.TryRead(ref platform, APTR.Null,
			out _));
	}

	[Fact]
	public void ListPoolPolicyCodecUsesNamedFields()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x23C0);
		var expected = default(MuiListCore.MuiListPoolPolicyState);
		expected.Magic = MuiListCore.MuiListPoolPolicyState.Cookie;
		expected.Pool = APTR.FromPointer(0x2800);
		expected.PuddleSize = 2008;
		expected.ThresholdSize = 1024;
		expected.UsesExternalPool = 1;

		Assert.True(MuiListCore.MuiListPoolPolicyStateCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiListCore.MuiListPoolPolicyStateCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Pool, actual.Pool);
		Assert.Equal(expected.PuddleSize, actual.PuddleSize);
		Assert.Equal(expected.ThresholdSize, actual.ThresholdSize);
		Assert.Equal(expected.UsesExternalPool, actual.UsesExternalPool);
		Assert.False(MuiListCore.MuiListPoolPolicyStateCodec.TryRead(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void ListPoolConstructionTagsPublishNamedPolicyAndStayImmutable()
	{
		var platform = CreatePlatform(out var listClass, out var otherClass,
			0x40000);
		var tags = APTR.FromPointer(0x2380);
		var pool = APTR.FromPointer(0x2F00);
		platform.WriteUInt32(tags, 0, PoolAttr);
		platform.WriteUInt32(tags, 4, pool.Raw);
		platform.WriteUInt32(tags, 8, PoolPuddleSizeAttr);
		platform.WriteUInt32(tags, 12, 4096);
		platform.WriteUInt32(tags, 16, PoolThreshSizeAttr);
		platform.WriteUInt32(tags, 20, 2048);
		platform.WriteUInt32(tags, 24, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.TryGetPoolPolicy(ref platform, State, list,
			out var policy));
		Assert.Equal(pool, policy.Pool);
		Assert.Equal(4096u, policy.PuddleSize);
		Assert.Equal(2048u, policy.ThresholdSize);
		Assert.Equal(1u, policy.UsesExternalPool);
		Assert.False(MuiListCore.SetAttribute(ref platform, State, list,
			PoolAttr, APTR.FromPointer(0x2F40).Raw));
		Assert.False(MuiListCore.SetAttribute(ref platform, State, list,
			PoolPuddleSizeAttr, 8192));
		Assert.True(MuiListCore.TryGetPoolPolicy(ref platform, State, list,
			out policy));
		Assert.Equal(4096u, policy.PuddleSize);
		Assert.Equal(2048u, policy.ThresholdSize);

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void ListHeaderFieldCursorUsesNamedRecordBoundary()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2480);
		Assert.True(MuiListHeaderFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiListHeaderField.Magic, MuiListHeaderState.Cookie));
		Assert.True(MuiListHeaderFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiListHeaderField.Index, 0x2800u));
		Assert.True(MuiListHeaderFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiListHeaderField.Count, 3u));
		Assert.True(MuiListHeaderFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiListHeaderField.Images, 0x2C00u));
		Assert.True(MuiListHeaderFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiListHeaderField.Count, out var count));
		Assert.Equal(3u, count);
		Assert.False(MuiListHeaderFieldCursorCodec.TryReadUInt32(ref platform,
			address, unchecked((MuiListHeaderField)255), out _));
		Assert.False(MuiListHeaderFieldCursorCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0xFFFFFFF0u), MuiListHeaderField.Count, out _));
	}

	[Fact]
	public void HScrollerStateUsesNamedFieldsAndMorphosPolicyResolution()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2580);
		var expected = default(MuiListHScrollerState);
		expected.Magic = MuiListHScrollerState.Cookie;
		expected.Policy = 1; // MUIV_List_HScrollerVisibility_Always
		expected.ContentWidth = 160;
		expected.ViewWidth = 100;
		expected.Visible = 1;

		Assert.True(MuiListHScrollerStateCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiListHScrollerStateCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Policy, actual.Policy);
		Assert.Equal(expected.ContentWidth, actual.ContentWidth);
		Assert.Equal(expected.ViewWidth, actual.ViewWidth);
		Assert.Equal(expected.Visible, actual.Visible);
		Assert.True(MuiListCore.ResolveHScrollerVisibility(1, 0, 0));
		Assert.False(MuiListCore.ResolveHScrollerVisibility(2, 500, 1));
		Assert.False(MuiListCore.ResolveHScrollerVisibility(0, 100, 100));
		Assert.True(MuiListCore.ResolveHScrollerVisibility(0, 101, 100));
	}

	[Fact]
	public void HScrollerVisibilityNormalizesConstructionPolicyAndTracksViewport()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x50000);
		var tags = APTR.FromPointer(0x2600);
		platform.WriteUInt32(tags, 0, HScrollerVisibilityAttr);
		platform.WriteUInt32(tags, 4, 99); // malformed values map to Auto
		platform.WriteUInt32(tags, 8, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.TryGetHScrollerState(ref platform, State, list,
			out var state));
		Assert.Equal(0u, state.Policy);
		Assert.True(MuiListCore.SetHScrollerViewport(ref platform, State, list,
			120, 100));
		Assert.True(MuiListCore.TryGetHScrollerState(ref platform, State, list,
			out state));
		Assert.Equal(120u, state.ContentWidth);
		Assert.Equal(100u, state.ViewWidth);
		Assert.Equal(1u, state.Visible);
		Assert.False(MuiListCore.SetAttribute(ref platform, State, list,
			HScrollerVisibilityAttr, 1)); // [I..] remains construction-only
	}

	[Fact]
	public void ListPointerSlotCodecUsesNamedGuestPointer()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2400);
		var expected = default(MuiListCore.MuiListPointerSlotRecord);
		expected.Value = APTR.FromPointer(0x2800);
		Assert.True(MuiListCore.MuiListPointerSlotCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiListCore.MuiListPointerSlotCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Value, actual.Value);
		Assert.False(MuiListCore.MuiListPointerSlotCodec.TryRead(ref platform,
			APTR.FromPointer(0x43FFE), out _));
	}

	[Fact]
	public void ListPointerSlotCursorUsesNamedEntryBoundary()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var cursor = default(MuiListCore.MuiListPointerSlotCursor);
		cursor.Base = APTR.FromPointer(0x2400);
		cursor.Index = 256;

		Assert.True(MuiListCore.MuiListPointerSlotCursorCodec.TryGetEntry(
			ref platform, cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x2800), address);
		cursor.Index = 257;
		Assert.False(MuiListCore.MuiListPointerSlotCursorCodec.TryGetEntry(
			ref platform, cursor, out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		cursor.Index = 0;
		Assert.False(MuiListCore.MuiListPointerSlotCursorCodec.TryGetEntry(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ListSlotCursorUsesNamedEntryBoundary()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var cursor = default(MuiListCore.MuiListSlotCursor);
		cursor.Base = APTR.FromPointer(0x2400);
		cursor.Index = 256;

		Assert.True(MuiListCore.MuiListSlotCursorCodec.TryGetEntry(
			ref platform, cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x2C00), address);
		cursor.Index = 0x00100000u;
		Assert.False(MuiListCore.MuiListSlotCursorCodec.TryGetEntry(
			ref platform, cursor, out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		cursor.Index = 0;
		Assert.False(MuiListCore.MuiListSlotCursorCodec.TryGetEntry(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ListPointerVectorCursorUsesLargeEntryBoundary()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var cursor = default(MuiListCore.MuiListPointerVectorCursor);
		cursor.Base = APTR.FromPointer(0x2400);
		cursor.Index = 1024;

		Assert.True(MuiListCore.MuiListPointerVectorCursorCodec.TryGetEntry(
			ref platform, cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x3400), address);
		cursor.Index = 0x00100000u;
		Assert.False(MuiListCore.MuiListPointerVectorCursorCodec.TryGetEntry(
			ref platform, cursor, out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		cursor.Index = 0;
		Assert.False(MuiListCore.MuiListPointerVectorCursorCodec.TryGetEntry(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ListColumnMetricCursorUsesGeometryBoundary()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var cursor = default(MuiListCore.MuiListColumnMetricCursor);
		cursor.Base = APTR.FromPointer(0x2400);
		cursor.Index = 63;

		Assert.True(MuiListCore.MuiListColumnMetricCursorCodec.TryGetEntry(
			ref platform, cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x24FC), address);
		cursor.Index = 255;
		Assert.True(MuiListCore.MuiListColumnMetricCursorCodec.TryGetEntry(
			ref platform, cursor, out address));
		Assert.Equal(APTR.FromPointer(0x27FC), address);
		cursor.Index = 256;
		Assert.False(MuiListCore.MuiListColumnMetricCursorCodec.TryGetEntry(
			ref platform, cursor, out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		cursor.Index = 0;
		Assert.False(MuiListCore.MuiListColumnMetricCursorCodec.TryGetEntry(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ListFormatDescriptorCursorUsesNamedEntryBoundary()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var cursor = default(MuiListCore.MuiListFormatDescriptorCursor);
		cursor.Base = APTR.FromPointer(0x2400);
		cursor.Index = 255;

		Assert.True(MuiListCore.MuiListFormatDescriptorCursorCodec.TryGetEntry(
			ref platform, cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x4BD8), address);
		cursor.Index = 256;
		Assert.False(MuiListCore.MuiListFormatDescriptorCursorCodec.TryGetEntry(
			ref platform, cursor, out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		cursor.Index = 0;
		Assert.False(MuiListCore.MuiListFormatDescriptorCursorCodec.TryGetEntry(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ListColumnGeometryCursorUsesNamedEntryBoundary()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var cursor = default(MuiListCore.MuiListColumnGeometryCursor);
		cursor.Base = APTR.FromPointer(0x2400);
		cursor.Index = 63;

		Assert.True(MuiListCore.MuiListColumnGeometryCursorCodec.TryGetEntry(
			ref platform, cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x25F8), address);
		cursor.Index = 255;
		Assert.True(MuiListCore.MuiListColumnGeometryCursorCodec.TryGetEntry(
			ref platform, cursor, out address));
		Assert.Equal(APTR.FromPointer(0x2BF8), address);
		cursor.Index = 256;
		Assert.False(MuiListCore.MuiListColumnGeometryCursorCodec.TryGetEntry(
			ref platform, cursor, out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		cursor.Index = 0;
		Assert.False(MuiListCore.MuiListColumnGeometryCursorCodec.TryGetEntry(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ListColumnOrderByteCursorUsesNamedEntryBoundary()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var cursor = default(MuiListCore.MuiListColumnOrderByteCursor);
		cursor.Base = APTR.FromPointer(0x2400);
		cursor.Index = 63;

		Assert.True(MuiListCore.MuiListColumnOrderByteCursorCodec.TryGetEntry(
			ref platform, cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x243F), address);
		cursor.Index = 255;
		Assert.True(MuiListCore.MuiListColumnOrderByteCursorCodec.TryGetEntry(
			ref platform, cursor, out address));
		Assert.Equal(APTR.FromPointer(0x24FF), address);
		cursor.Index = 256;
		Assert.False(MuiListCore.MuiListColumnOrderByteCursorCodec.TryGetEntry(
			ref platform, cursor, out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFFF);
		cursor.Index = 1;
		Assert.False(MuiListCore.MuiListColumnOrderByteCursorCodec.TryGetEntry(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ListScalarStorageCodecUsesNamedValue()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2A00);
		var expected = new MuiListScalarStorageRecord { Value = 0xFFFFFFFEu };

		Assert.True(MuiListScalarStorageCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiListScalarStorageCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Value, actual.Value);
		Assert.False(MuiListScalarStorageCodec.TryRead(ref platform, APTR.Null,
			out _));
	}

	[Fact]
	public void ListColumnMetricCodecUsesNamedValue()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2A40);
		var expected = default(MuiListCore.MuiListColumnMetricValue);
		expected.Value = 640;

		Assert.True(MuiListCore.MuiListColumnMetricCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiListCore.MuiListColumnMetricCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.Value, actual.Value);
		Assert.False(MuiListCore.MuiListColumnMetricCodec.TryRead(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void ListSingleFieldRecordsUseNamedFieldCursors()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var metric = APTR.FromPointer(0x2C00);
		var metricCursor = new MuiListCore.MuiListColumnMetricFieldCursor
		{
			Record = metric,
			Field = MuiListCore.MuiListColumnMetricField.Value,
		};
		Assert.True(MuiListCore.MuiListColumnMetricFieldCursorCodec.TryGetAddress(
			ref platform, metricCursor, out var metricAddress));
		Assert.Equal(metric, metricAddress);
		Assert.True(MuiListCore.MuiListColumnMetricFieldCursorCodec.TryWriteUInt32(
			ref platform, metric, MuiListCore.MuiListColumnMetricField.Value, 96u));
		Assert.True(MuiListCore.MuiListColumnMetricFieldCursorCodec.TryReadUInt32(
			ref platform, metric, MuiListCore.MuiListColumnMetricField.Value,
			out var width));
		Assert.Equal(96u, width);

		var pointer = APTR.FromPointer(0x2C20);
		Assert.True(MuiListCore.MuiListPointerSlotFieldCursorCodec.TryWriteUInt32(
			ref platform, pointer, MuiListCore.MuiListPointerSlotField.Value,
			0x3300u));
		Assert.True(MuiListCore.MuiListPointerSlotFieldCursorCodec.TryReadUInt32(
			ref platform, pointer, MuiListCore.MuiListPointerSlotField.Value,
			out var pointerValue));
		Assert.Equal(0x3300u, pointerValue);

		var header = APTR.FromPointer(0x2C40);
		Assert.True(MuiListCore.MuiListOwnedRecordHeaderFieldCursorCodec
			.TryWriteUInt32(ref platform, header,
				MuiListCore.MuiListOwnedRecordHeaderField.Length, 28u));
		Assert.True(MuiListCore.MuiListOwnedRecordHeaderFieldCursorCodec
			.TryReadUInt32(ref platform, header,
				MuiListCore.MuiListOwnedRecordHeaderField.Length, out var length));
		Assert.Equal(28u, length);
		Assert.False(MuiListCore.MuiListOwnedRecordHeaderFieldCursorCodec
			.TryReadUInt32(ref platform, header,
				unchecked((MuiListCore.MuiListOwnedRecordHeaderField)255), out _));
	}

	[Fact]
	public void ListColumnMetricsStateUsesNamedPointerField()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2A80);
		var expected = default(MuiListCore.MuiListColumnMetricsState);
		expected.Magic = 0x434D4554u;
		expected.Width = 96;
		expected.Columns = 2;
		expected.Values = APTR.FromPointer(0x2B00);

		Assert.True(MuiListCore.MuiListColumnMetricsStateCodec.Write(
			ref platform, address, expected));
		Assert.True(MuiListCore.MuiListColumnMetricsStateCodec.TryRead(
			ref platform, address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Width, actual.Width);
		Assert.Equal(expected.Columns, actual.Columns);
		Assert.Equal(expected.Values, actual.Values);
		Assert.False(MuiListCore.MuiListColumnMetricsStateCodec.TryRead(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void ListColumnMetricsFieldCursorUsesNamedRecordBoundary()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2A80);
		Assert.True(MuiListCore.MuiListColumnMetricsFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListColumnMetricsField.Magic,
				0x4C4D4554u));
		Assert.True(MuiListCore.MuiListColumnMetricsFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListColumnMetricsField.Width, 96u));
		Assert.True(MuiListCore.MuiListColumnMetricsFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListColumnMetricsField.Columns, 3u));
		Assert.True(MuiListCore.MuiListColumnMetricsFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListColumnMetricsField.Values, 0x2B00u));
		Assert.True(MuiListCore.MuiListColumnMetricsFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListColumnMetricsField.Values, out var values));
		Assert.Equal(0x2B00u, values);
		Assert.False(MuiListCore.MuiListColumnMetricsFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				unchecked((MuiListCore.MuiListColumnMetricsField)255), out _));
	}

	[Fact]
	public void ListPrivateStateFieldCursorUsesNamedRecordKinds()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var title = APTR.FromPointer(0x2C00);
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, title, MuiListCore.MuiListStateRecordKind.TitleArray,
			MuiListCore.MuiListStateField.Pointers, 0x3000u));
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, title, MuiListCore.MuiListStateRecordKind.TitleArray,
			MuiListCore.MuiListStateField.Pointers, out var pointers));
		Assert.Equal(0x3000u, pointers);

		var redraw = APTR.FromPointer(0x2C20);
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, redraw, MuiListCore.MuiListStateRecordKind.Redraw,
			MuiListCore.MuiListStateField.Dirty, 1u));
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, redraw, MuiListCore.MuiListStateRecordKind.Redraw,
			MuiListCore.MuiListStateField.Dirty, out var dirty));
		Assert.Equal(1u, dirty);

		var visibility = APTR.FromPointer(0x2C40);
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, visibility,
			MuiListCore.MuiListStateRecordKind.ColumnVisibility,
			MuiListCore.MuiListStateField.High, 0x20u));
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, visibility,
			MuiListCore.MuiListStateRecordKind.ColumnVisibility,
			MuiListCore.MuiListStateField.High, out var high));
		Assert.Equal(0x20u, high);

		var order = APTR.FromPointer(0x2C60);
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, order, MuiListCore.MuiListStateRecordKind.ColumnOrder,
			MuiListCore.MuiListStateField.Reserved, 12u));
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, order, MuiListCore.MuiListStateRecordKind.ColumnOrder,
			MuiListCore.MuiListStateField.Reserved, out var reserved));
		Assert.Equal(12u, reserved);

		var viewport = APTR.FromPointer(0x2C80);
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, viewport, MuiListCore.MuiListStateRecordKind.Viewport,
			MuiListCore.MuiListStateField.TotalPixel, 640u));
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, viewport, MuiListCore.MuiListStateRecordKind.Viewport,
			MuiListCore.MuiListStateField.TotalPixel, out var total));
		Assert.Equal(640u, total);
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, viewport, MuiListCore.MuiListStateRecordKind.Viewport,
			MuiListCore.MuiListStateField.First, 7u));
		Assert.True(MuiListCore.MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, viewport, MuiListCore.MuiListStateRecordKind.Viewport,
			MuiListCore.MuiListStateField.First, out var first));
		Assert.Equal(7u, first);
		Assert.False(MuiListCore.MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, order, MuiListCore.MuiListStateRecordKind.TitleArray,
			MuiListCore.MuiListStateField.Reserved, out _));
		Assert.False(MuiListCore.MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0xFFFFFFF0u),
			MuiListCore.MuiListStateRecordKind.Viewport,
			MuiListCore.MuiListStateField.TotalPixel, out _));
	}

	[Fact]
	public void ListEditStateUsesNamedPointerFields()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2AC0);
		var expected = default(MuiListCore.MuiListEditState);
		expected.Magic = 0x4C454449u;
		expected.Row = 3;
		expected.Column = 1;
		expected.Entry = APTR.FromPointer(0x2B40);
		expected.EditObject = APTR.FromPointer(0x2BC0);
		expected.Flags = 2;

		Assert.True(MuiListCore.MuiListEditStateCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiListCore.MuiListEditStateCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Row, actual.Row);
		Assert.Equal(expected.Column, actual.Column);
		Assert.Equal(expected.Entry, actual.Entry);
		Assert.Equal(expected.EditObject, actual.EditObject);
		Assert.Equal(expected.Flags, actual.Flags);
		Assert.False(MuiListCore.MuiListEditStateCodec.TryRead(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void ListEditFieldCursorUsesNamedRecordBoundary()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2B00);
		Assert.True(MuiListCore.MuiListEditFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListEditField.Magic,
			0x4C454449u));
		Assert.True(MuiListCore.MuiListEditFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListEditField.Row,
			unchecked((uint)-2)));
		Assert.True(MuiListCore.MuiListEditFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListEditField.EditObject,
			0x2BC0u));
		Assert.True(MuiListCore.MuiListEditFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiListCore.MuiListEditField.Row,
			out var row));
		Assert.Equal(unchecked((uint)-2), row);
		Assert.True(MuiListCore.MuiListEditFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiListCore.MuiListEditField.EditObject,
			out var editObject));
		Assert.Equal(0x2BC0u, editObject);
		Assert.False(MuiListCore.MuiListEditFieldCursorCodec.TryReadUInt32(
			ref platform, address, unchecked((MuiListCore.MuiListEditField)255),
			out _));
	}

	[Fact]
	public void ListFormatDescriptorUsesNamedPreparsePointers()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2C00);
		var expected = default(MuiListCore.MuiListFormatDescriptor);
		expected.Delta = 4;
		expected.Weight = 2;
		expected.MinWidth = 8;
		expected.MaxWidth = 120;
		expected.Column = 1;
		expected.Flags = 3;
		expected.Preparse = APTR.FromPointer(0x2C40);
		expected.PreparseLength = 6;
		expected.PreparseStorage = APTR.FromPointer(0x2C80);
		expected.PreparseStorageLength = 7;

		MuiListCore.WriteFormatDescriptor(ref platform, address, ref expected);
		MuiListCore.ReadFormatDescriptor(ref platform, address, out var actual);
		Assert.Equal(expected.Delta, actual.Delta);
		Assert.Equal(expected.Weight, actual.Weight);
		Assert.Equal(expected.MinWidth, actual.MinWidth);
		Assert.Equal(expected.MaxWidth, actual.MaxWidth);
		Assert.Equal(expected.Column, actual.Column);
		Assert.Equal(expected.Flags, actual.Flags);
		Assert.Equal(expected.Preparse, actual.Preparse);
		Assert.Equal(expected.PreparseLength, actual.PreparseLength);
		Assert.Equal(expected.PreparseStorage, actual.PreparseStorage);
		Assert.Equal(expected.PreparseStorageLength,
			actual.PreparseStorageLength);
	}

	[Fact]
	public void ListOwnedRecordHeaderUsesNamedLength()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2D00);
		var expected = new MuiListCore.MuiListOwnedRecordHeader { Length = 28 };

		Assert.True(MuiListCore.MuiListOwnedRecordHeaderCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiListCore.MuiListOwnedRecordHeaderCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Length, actual.Length);
		Assert.False(MuiListCore.MuiListOwnedRecordHeaderCodec.TryRead(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void ColumnOrderStatePublishesTypedPointerTable()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var storage = APTR.FromPointer(0x2480);
		var values = APTR.FromPointer(0x2500);
		var source = APTR.FromPointer(0x2580);
		platform.WriteUInt8(source, 0, 1);
		platform.WriteUInt8(source, 1, 0);

		Assert.True(MuiListCore.WriteColumnOrder(ref platform, storage, values,
			source, 2));
		Assert.Equal(1u, MuiListCore.GetColumnOrderDisplayColumn(ref platform,
			storage, 0, 99));
		Assert.Equal(0u, MuiListCore.GetColumnOrderDisplayColumn(ref platform,
			storage, 1, 99));
		Assert.Equal(99u, MuiListCore.GetColumnOrderDisplayColumn(ref platform,
			storage, 2, 99));
	}

	[Fact]
	public void CollectionMethodHeaderUsesNamedField()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2E00);
		Assert.True(MuiCollectionBasicMessageCodec.WriteMethod(ref platform,
			address, MuiCollectionBasicMessageCodec.Sort));
		Assert.True(MuiCollectionBasicMessageCodec.TryReadMethodId(ref platform,
			address, out var packet));
		Assert.Equal(MuiCollectionBasicMessageCodec.Sort, packet.MethodId);
		Assert.False(MuiCollectionBasicMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void CollectionRecordReadersUseNamedMethodHeader()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2F00);
		Assert.True(MuiCollectionRecordMessageCodec.WriteDisplay(ref platform,
			address, 1, 2, 3));
		Assert.True(MuiCollectionRecordMessageCodec.TryReadDisplay(ref platform,
			address, out var packet));
		Assert.Equal(MuiCollectionRecordMessageCodec.Display, packet.MethodId);
		Assert.Equal(1u, packet.Entry);
		Assert.True(MuiCollectionRecordFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiCollectionRecordPacketKind.Display,
			MuiCollectionRecordField.MethodId, 0xDEADBEEFu));
		Assert.False(MuiCollectionRecordMessageCodec.TryReadDisplay(ref platform,
			address, out _));
	}

	[Fact]
	public void CollectionRecordFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2F00);
		var cursor = default(MuiCollectionRecordFieldCursor);
		cursor.Message = address;
		cursor.Packet = MuiCollectionRecordPacketKind.Display;
		cursor.Field = MuiCollectionRecordField.MethodId;
		Assert.True(MuiCollectionRecordFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(0x2F00u, fieldAddress.Raw);
		cursor.Field = MuiCollectionRecordField.Entry;
		Assert.True(MuiCollectionRecordFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out fieldAddress));
		Assert.Equal(0x2F04u, fieldAddress.Raw);
		cursor.Field = MuiCollectionRecordField.Array;
		Assert.True(MuiCollectionRecordFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out fieldAddress));
		Assert.Equal(0x2F08u, fieldAddress.Raw);
		cursor.Field = MuiCollectionRecordField.Row;
		Assert.True(MuiCollectionRecordFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out fieldAddress));
		Assert.Equal(0x2F0Cu, fieldAddress.Raw);

		Assert.True(MuiCollectionRecordFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiCollectionRecordPacketKind.TestPos,
			MuiCollectionRecordField.Result, unchecked((uint)-3)));
		Assert.True(MuiCollectionRecordFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiCollectionRecordPacketKind.TestPos,
			MuiCollectionRecordField.Result, out var result));
		Assert.Equal(unchecked((uint)-3), result);
		cursor.Packet = MuiCollectionRecordPacketKind.Compare;
		cursor.Field = MuiCollectionRecordField.Pool;
		Assert.False(MuiCollectionRecordFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
		cursor.Message = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Packet = MuiCollectionRecordPacketKind.TestPos;
		cursor.Field = MuiCollectionRecordField.Result;
		Assert.False(MuiCollectionRecordFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void CollectionEditReadersUseNamedMethodHeader()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2F40);
		Assert.True(MuiCollectionEditMessageCodec.WriteEdit(ref platform,
			address, -2, 3));
		Assert.True(MuiCollectionEditMessageCodec.TryReadEdit(ref platform,
			address, out var packet));
		Assert.Equal(MuiCollectionEditMessageCodec.Edit, packet.MethodId);
		Assert.Equal(-2, packet.Row);
		Assert.True(MuiCollectionEditFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiCollectionEditPacketKind.Edit,
			MuiCollectionEditField.MethodId, 0xDEADBEEFu));
		Assert.False(MuiCollectionEditMessageCodec.TryReadEdit(ref platform,
			address, out _));
	}

	[Fact]
	public void CollectionEditFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2F40);
		var cursor = default(MuiCollectionEditFieldCursor);
		cursor.Message = address;
		cursor.Packet = MuiCollectionEditPacketKind.EditDone;
		cursor.Field = MuiCollectionEditField.MethodId;
		Assert.True(MuiCollectionEditFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(0x2F40u, fieldAddress.Raw);
		cursor.Field = MuiCollectionEditField.Row;
		Assert.True(MuiCollectionEditFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out fieldAddress));
		Assert.Equal(0x2F44u, fieldAddress.Raw);
		cursor.Field = MuiCollectionEditField.Column;
		Assert.True(MuiCollectionEditFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out fieldAddress));
		Assert.Equal(0x2F48u, fieldAddress.Raw);
		cursor.Field = MuiCollectionEditField.Entry;
		Assert.True(MuiCollectionEditFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out fieldAddress));
		Assert.Equal(0x2F4Cu, fieldAddress.Raw);
		cursor.Field = MuiCollectionEditField.EditObject;
		Assert.True(MuiCollectionEditFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out fieldAddress));
		Assert.Equal(0x2F50u, fieldAddress.Raw);

		Assert.True(MuiCollectionEditFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiCollectionEditPacketKind.Edit,
			MuiCollectionEditField.Row, unchecked((uint)-5)));
		Assert.True(MuiCollectionEditFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiCollectionEditPacketKind.Edit,
			MuiCollectionEditField.Row, out var rawRow));
		Assert.Equal(-5, unchecked((int)rawRow));
		cursor.Packet = MuiCollectionEditPacketKind.EndEdit;
		cursor.Field = MuiCollectionEditField.Entry;
		Assert.False(MuiCollectionEditFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
		cursor.Message = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Packet = MuiCollectionEditPacketKind.EndEdit;
		cursor.Field = MuiCollectionEditField.Mode;
		Assert.False(MuiCollectionEditFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void CollectionAdvancedReadersUseNamedMethodHeader()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2F80);
		Assert.True(MuiCollectionAdvancedMessageCodec.WriteInsertSingle(
			ref platform, address, 11, 2));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadInsertSingle(
			ref platform, address, out var packet));
		Assert.Equal(MuiCollectionAdvancedMessageCodec.InsertSingle,
			packet.MethodId);
		Assert.Equal(11u, packet.Entry);
		platform.WriteUInt32(address, 0, 0xDEADBEEFu);
		Assert.False(MuiCollectionAdvancedMessageCodec.TryReadInsertSingle(
			ref platform, address, out _));
	}

	[Fact]
	public void CollectionBasicTypedReadersUseNamedMethodHeader()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2FC0);
		Assert.True(MuiCollectionBasicMessageCodec.WriteGetEntry(ref platform,
			address, 4, 0x3100));
		Assert.True(MuiCollectionBasicMessageCodec.TryReadGetEntry(ref platform,
			address, out var packet));
		Assert.Equal(MuiCollectionBasicMessageCodec.GetEntry, packet.MethodId);
		Assert.Equal(4u, packet.Position);
		platform.WriteUInt32(address, 0, 0xDEADBEEFu);
		Assert.False(MuiCollectionBasicMessageCodec.TryReadGetEntry(ref platform,
			address, out _));
	}

	[Fact]
	public void CollectionBasicMethodReaderUsesNamedMethodHeader()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2FC0);
		Assert.True(MuiCollectionBasicMessageCodec.WriteMethod(ref platform,
			address, MuiCollectionBasicMessageCodec.Clear));
		Assert.True(MuiCollectionBasicMessageCodec.TryReadMethod(ref platform,
			address, MuiCollectionBasicMessageCodec.Clear, out var packet));
		Assert.Equal(MuiCollectionBasicMessageCodec.Clear, packet.MethodId);
		Assert.False(MuiCollectionBasicMessageCodec.TryReadMethod(ref platform,
			address, MuiCollectionBasicMessageCodec.Sort, out _));
	}

	[Fact]
	public void ListSlotAndImageCodecsUseNamedGuestFields()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var slotAddress = APTR.FromPointer(0x2800);
		var slot = default(MuiListSlotState);
		slot.Entry = APTR.FromPointer(0x9000);
		slot.Flags = 0x00000007;
		Assert.True(MuiListSlotCodec.Write(ref platform, slotAddress, slot));
		Assert.True(MuiListSlotCodec.TryRead(ref platform, slotAddress,
			out var readSlot));
		Assert.Equal(slot.Entry, readSlot.Entry);
		Assert.Equal(slot.Flags, readSlot.Flags);

		var imageAddress = APTR.FromPointer(0x2900);
		var image = default(MuiListImageState);
		image.Magic = MuiListImageState.Cookie;
		image.ImageObject = APTR.FromPointer(0xA000);
		image.Flags = 0x12;
		image.Next = APTR.FromPointer(0x2A00);
		Assert.True(MuiListImageCodec.Write(ref platform, imageAddress, image));
		Assert.True(MuiListImageCodec.TryRead(ref platform, imageAddress,
			out var readImage));
		Assert.Equal(image.Magic, readImage.Magic);
		Assert.Equal(image.ImageObject, readImage.ImageObject);
		Assert.Equal(image.Flags, readImage.Flags);
		Assert.Equal(image.Next, readImage.Next);
		Assert.False(MuiListImageCodec.TryRead(ref platform, APTR.Null,
			out _));
	}

	[Fact]
	public void ListSlotAndImageFieldCursorsUseNamedRecordBoundaries()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var slot = APTR.FromPointer(0x2480);
		Assert.True(MuiListSlotFieldCursorCodec.TryWriteUInt32(ref platform, slot,
			MuiListSlotField.Entry, 0x2800u));
		Assert.True(MuiListSlotFieldCursorCodec.TryWriteUInt32(ref platform, slot,
			MuiListSlotField.Flags, 7u));
		Assert.True(MuiListSlotFieldCursorCodec.TryReadUInt32(ref platform, slot,
			MuiListSlotField.Flags, out var flags));
		Assert.Equal(7u, flags);
		Assert.False(MuiListSlotFieldCursorCodec.TryReadUInt32(ref platform, slot,
			unchecked((MuiListSlotField)255), out _));

		var image = APTR.FromPointer(0x2500);
		Assert.True(MuiListImageFieldCursorCodec.TryWriteUInt32(ref platform, image,
			MuiListImageField.Magic, MuiListImageState.Cookie));
		Assert.True(MuiListImageFieldCursorCodec.TryWriteUInt32(ref platform, image,
			MuiListImageField.ImageObject, 0x2900u));
		Assert.True(MuiListImageFieldCursorCodec.TryWriteUInt32(ref platform, image,
			MuiListImageField.Next, 0x2A00u));
		Assert.True(MuiListImageFieldCursorCodec.TryReadUInt32(ref platform, image,
			MuiListImageField.Next, out var next));
		Assert.Equal(0x2A00u, next);
		Assert.False(MuiListImageFieldCursorCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0xFFFFFFF0u), MuiListImageField.Next, out _));
	}

	[Fact]
	public void ListColumnGeometryCodecUsesNamedGuestFields()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2A00);
		var expected = default(MuiListCore.MuiListColumnGeometry);
		expected.Offset = 24;
		expected.Width = 96;
		Assert.True(MuiListCore.MuiListColumnGeometryCodec.Write(ref platform,
			address,
			expected));
		Assert.True(MuiListCore.MuiListColumnGeometryCodec.TryRead(ref platform,
			address,
			out var actual));
		Assert.Equal(expected.Offset, actual.Offset);
		Assert.Equal(expected.Width, actual.Width);
		Assert.False(MuiListCore.MuiListColumnGeometryCodec.TryRead(ref platform,
			APTR.Null,
			out _));
	}

	[Fact]
	public void ListColumnGeometryFieldCursorUsesNamedRecordBoundary()
	{
		var platform = CreatePlatform(out _, out _, 0x40000);
		var address = APTR.FromPointer(0x2A40);
		Assert.True(MuiListCore.MuiListColumnGeometryFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListColumnGeometryField.Offset, 24u));
		Assert.True(MuiListCore.MuiListColumnGeometryFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListColumnGeometryField.Width, 96u));
		Assert.True(MuiListCore.MuiListColumnGeometryFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListColumnGeometryField.Width, out var width));
		Assert.Equal(96u, width);
		Assert.False(MuiListCore.MuiListColumnGeometryFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				unchecked((MuiListCore.MuiListColumnGeometryField)255), out _));
	}

	[Fact]
	public void IdentifiesExactlyTheListClass()
	{
		var platform = CreatePlatform(out var listClass, out var otherClass,
			0x40000);
		Assert.Equal(MuiCollectionClass.List, MuiListCore.ClassifyRecord(
			ref platform, listClass));
		Assert.Equal(MuiCollectionClass.Unknown, MuiListCore.ClassifyRecord(
			ref platform, otherClass));
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		Assert.Equal(MuiCollectionClass.List, MuiListCore.Classify(ref platform,
			State, list));
		var other = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			otherClass, APTR.Null);
		Assert.Equal(MuiCollectionClass.Unknown, MuiListCore.Classify(ref platform,
			State, other));
	}

	[Fact]
	public void InsertGetEntryAndCountTrackWithConstantTimeLookup()
	{
		var platform = CreatePlatform(out var listClass, out var otherClass, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		var a = APTR.FromPointer(0x9000001);
		var b = APTR.FromPointer(0x9000002);
		var c = APTR.FromPointer(0x9000003);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, a,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, c,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, b,
			1)); // insert between a and c
		Assert.Equal(3u, MuiListCore.EntryCount(ref platform, State, list));
		Assert.Equal(3u, Get(ref platform, list, EntriesAttr));
		var storage = APTR.FromPointer(0x2000);
		Assert.Equal(a, MuiListCore.GetEntry(ref platform, State, list, 0,
			storage));
		Assert.Equal(a.Raw, platform.ReadUInt32(storage, 0));
		Assert.Equal(b, MuiListCore.GetEntry(ref platform, State, list, 1,
			APTR.Null));
		Assert.Equal(c, MuiListCore.GetEntry(ref platform, State, list, 2,
			APTR.Null));
		Assert.Equal(APTR.Null, MuiListCore.GetEntry(ref platform, State, list, 3,
			storage));
		Assert.Equal(0u, platform.ReadUInt32(storage, 0));
	}

	[Fact]
	public void InsertArraySupportsCountedAndNullTerminatedForms()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		var array = APTR.FromPointer(0x2000);
		platform.WriteUInt32(array, 0, 0x8000010);
		platform.WriteUInt32(array, 4, 0x8000020);
		platform.WriteUInt32(array, 8, 0x8000030);
		Assert.True(MuiListCore.Insert(ref platform, State, list, array, 3,
			InsertTop));
		Assert.Equal(3u, MuiListCore.EntryCount(ref platform, State, list));
		var terminated = APTR.FromPointer(0x2100);
		platform.WriteUInt32(terminated, 0, 0x8000040);
		platform.WriteUInt32(terminated, 4, 0x8000050);
		platform.WriteUInt32(terminated, 8, 0);
		Assert.True(MuiListCore.Insert(ref platform, State, list, terminated, -1,
			InsertBottom));
		Assert.Equal(5u, MuiListCore.EntryCount(ref platform, State, list));
		Assert.Equal(APTR.FromPointer(0x8000010), MuiListCore.GetEntry(
			ref platform, State, list, 0, APTR.Null));
		Assert.Equal(APTR.FromPointer(0x8000050), MuiListCore.GetEntry(
			ref platform, State, list, 4, APTR.Null));
	}

	[Fact]
	public void RemoveAndClearMaintainActiveAndCount()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		for (var i = 0u; i < 4; i++)
			MuiListCore.InsertSingle(ref platform, State, list,
				APTR.FromPointer(0x7000000 + i), InsertBottom);
		// Make entry 2 active, then remove entry 0: active should shift to 1.
		MuiListCore.SetAttribute(ref platform, State, list, ActiveAttr,
			2, false);
		Assert.True(MuiListCore.Remove(ref platform, State, list, 0));
		Assert.Equal(1u, Get(ref platform, list, ActiveAttr));
		Assert.Equal(3u, MuiListCore.EntryCount(ref platform, State, list));
		// Remove the active entry.
		Assert.True(MuiListCore.Remove(ref platform, State, list, RemoveActive));
		Assert.Equal(2u, MuiListCore.EntryCount(ref platform, State, list));
		Assert.True(MuiListCore.Clear(ref platform, State, list));
		Assert.Equal(0u, MuiListCore.EntryCount(ref platform, State, list));
		Assert.Equal(0u, Get(ref platform, list, ActiveAttr));
	}

	[Fact]
	public void SelectionTogglesAndNextSelectedIterates()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		for (var i = 0u; i < 5; i++)
			MuiListCore.InsertSingle(ref platform, State, list,
				APTR.FromPointer(0x6000000 + i), InsertBottom);
		Assert.True(MuiListCore.Select(ref platform, State, list, 1, SelectOn,
			APTR.Null));
		Assert.True(MuiListCore.Select(ref platform, State, list, 3, SelectToggle,
			APTR.Null));
		var cursor = APTR.FromPointer(0x2000);
		platform.WriteUInt32(cursor, 0, unchecked((uint)NextSelectedStart));
		Assert.True(MuiListCore.NextSelected(ref platform, State, list, cursor));
		Assert.Equal(1u, platform.ReadUInt32(cursor, 0));
		Assert.True(MuiListCore.NextSelected(ref platform, State, list, cursor));
		Assert.Equal(3u, platform.ReadUInt32(cursor, 0));
		Assert.True(MuiListCore.NextSelected(ref platform, State, list, cursor));
		Assert.Equal(0xFFFFFFFFu, platform.ReadUInt32(cursor, 0)); // End
		// Select all, then unselect one, then remove all selected.
		Assert.True(MuiListCore.Select(ref platform, State, list, SelectAll,
			SelectOn, APTR.Null));
		Assert.True(MuiListCore.Select(ref platform, State, list, 2, SelectOff,
			APTR.Null));
		var selectedCount = APTR.FromPointer(0x2200);
		Assert.True(MuiListCore.Select(ref platform, State, list, SelectAll,
			SelectAsk, selectedCount));
		Assert.Equal(4u, platform.ReadUInt32(selectedCount, 0));
		Assert.True(MuiListCore.Remove(ref platform, State, list, RemoveSelected));
		Assert.Equal(1u, MuiListCore.EntryCount(ref platform, State, list));
		Assert.Equal(APTR.FromPointer(0x6000002), MuiListCore.GetEntry(
			ref platform, State, list, 0, APTR.Null));
	}

	[Fact]
	public void NextSelectedFallsBackToActiveWhenNoRowsAreSelected()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x43000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		for (var i = 0u; i < 3; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
				APTR.FromPointer(0x6100000 + i), InsertBottom));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			ActiveAttr, 1, false));
		var cursor = APTR.FromPointer(0x2400);
		var initial = default(MuiListScalarStorageRecord);
		initial.Value = unchecked((uint)NextSelectedStart);
		Assert.True(MuiListScalarStorageCodec.Write(ref platform, cursor, initial));
		Assert.True(MuiListCore.NextSelected(ref platform, State, list, cursor));
		Assert.True(MuiListScalarStorageCodec.TryRead(ref platform, cursor,
			out var position));
		Assert.Equal(1u, position.Value);
		Assert.True(MuiListCore.NextSelected(ref platform, State, list, cursor));
		Assert.True(MuiListScalarStorageCodec.TryRead(ref platform, cursor,
			out position));
		Assert.Equal(0xFFFFFFFFu, position.Value);
	}

	[Fact]
	public void SortOrdersEntriesWithDefaultStringCompare()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		var charlie = APTR.FromPointer(0x2000);
		var alpha = APTR.FromPointer(0x2040);
		var bravo = APTR.FromPointer(0x2080);
		platform.WriteCString(charlie, "charlie");
		platform.WriteCString(alpha, "alpha");
		platform.WriteCString(bravo, "bravo");
		MuiListCore.InsertSingle(ref platform, State, list, charlie, InsertBottom);
		MuiListCore.InsertSingle(ref platform, State, list, alpha, InsertBottom);
		MuiListCore.InsertSingle(ref platform, State, list, bravo, InsertBottom);
		Assert.True(MuiListCore.Sort(ref platform, State, list));
		Assert.Equal(alpha, MuiListCore.GetEntry(ref platform, State, list, 0,
			APTR.Null));
		Assert.Equal(bravo, MuiListCore.GetEntry(ref platform, State, list, 1,
			APTR.Null));
		Assert.Equal(charlie, MuiListCore.GetEntry(ref platform, State, list, 2,
			APTR.Null));
		// Sorted insertion keeps the invariant.
		var delta = APTR.FromPointer(0x20C0);
		platform.WriteCString(delta, "aaa");
		MuiListCore.InsertSingle(ref platform, State, list, delta, InsertSorted);
		Assert.Equal(delta, MuiListCore.GetEntry(ref platform, State, list, 0,
			APTR.Null));
	}

	[Fact]
	public void MoveExchangeAndJumpRepositionEntries()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		for (var i = 0u; i < 4; i++)
			MuiListCore.InsertSingle(ref platform, State, list,
				APTR.FromPointer(0x5000000 + i), InsertBottom);
		// Move entry 0 to position 2: order becomes 1,2,0,3.
		Assert.True(MuiListCore.Move(ref platform, State, list, 0, 2));
		Assert.Equal(APTR.FromPointer(0x5000001), MuiListCore.GetEntry(
			ref platform, State, list, 0, APTR.Null));
		Assert.Equal(APTR.FromPointer(0x5000000), MuiListCore.GetEntry(
			ref platform, State, list, 2, APTR.Null));
		// Exchange positions 0 and 3.
		Assert.True(MuiListCore.Exchange(ref platform, State, list, 0, 3));
		Assert.Equal(APTR.FromPointer(0x5000003), MuiListCore.GetEntry(
			ref platform, State, list, 0, APTR.Null));
		Assert.Equal(APTR.FromPointer(0x5000001), MuiListCore.GetEntry(
			ref platform, State, list, 3, APTR.Null));
		// Jump records the resolved first-visible line.
		Assert.True(MuiListCore.Jump(ref platform, State, list, 2));
		Assert.Equal(2u, Get(ref platform, list, FirstAttr));
	}

	[Fact]
	public void SourceArrayIsMaterializedAtConstruction()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var array = APTR.FromPointer(0x2000);
		platform.WriteUInt32(array, 0, 0x4000010);
		platform.WriteUInt32(array, 4, 0x4000020);
		platform.WriteUInt32(array, 8, 0x4000030);
		platform.WriteUInt32(array, 12, 0);
		var tags = APTR.FromPointer(0x2100);
		platform.WriteUInt32(tags, 0, 0x8042c0a0u); // MUIA_List_SourceArray
		platform.WriteUInt32(tags, 4, array.Raw);
		platform.WriteUInt32(tags, 8, 0); // TAG_DONE
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.Equal(3u, MuiListCore.EntryCount(ref platform, State, list));
		Assert.Equal(APTR.FromPointer(0x4000030), MuiListCore.GetEntry(
			ref platform, State, list, 2, APTR.Null));
	}

	[Fact]
	public void StringConstructOwnsAndDisposalFreesEntriesWithoutLeak()
	{
		var platform = CreatePlatform(out var listClass, out var otherClass,
			0x40000);
		var tags = APTR.FromPointer(0x2000);
		platform.WriteUInt32(tags, 0, ConstructHookAttr);
		platform.WriteUInt32(tags, 4, HookString);
		platform.WriteUInt32(tags, 8, DestructHookAttr);
		platform.WriteUInt32(tags, 12, HookString);
		platform.WriteUInt32(tags, 16, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		var text = APTR.FromPointer(0x2100);
		platform.WriteCString(text, "owned-entry");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, text,
			InsertBottom));
		// The stored entry is a private duplicate, not the caller buffer.
		var stored = MuiListCore.GetEntry(ref platform, State, list, 0, APTR.Null);
		Assert.NotEqual(text, stored);
		Assert.NotEqual(APTR.Null, stored);
		Assert.Equal((byte)'o', platform.ReadUInt8(stored, 0));
		// Full teardown must balance every allocation with a free.
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State, list));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void StringArrayConstructCopiesColumnsDisplaysAndComparesWithoutLeak()
	{
		var platform = CreatePlatform(out var listClass, out var otherClass,
			0x40000);
		var tags = APTR.FromPointer(0x2000);
		platform.WriteUInt32(tags, 0, ConstructHookAttr);
		platform.WriteUInt32(tags, 4, 0xFFFFFFFEu); // StringArray
		platform.WriteUInt32(tags, 8, DestructHookAttr);
		platform.WriteUInt32(tags, 12, 0xFFFFFFFEu);
		platform.WriteUInt32(tags, 16, 0x8042b4d5u); // MUIA_List_DisplayHook
		platform.WriteUInt32(tags, 20, 0xFFFFFFFEu);
		platform.WriteUInt32(tags, 24, 0x80425c14u); // MUIA_List_CompareHook
		platform.WriteUInt32(tags, 28, 0xFFFFFFFEu);
		platform.WriteUInt32(tags, 32, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		var source = APTR.FromPointer(0x2100);
		var first = APTR.FromPointer(0x2200);
		var second = APTR.FromPointer(0x2240);
		platform.WriteCString(first, "alpha");
		platform.WriteCString(second, "001");
		platform.WriteUInt32(source, 0, first.Raw);
		platform.WriteUInt32(source, 4, second.Raw);
		platform.WriteUInt32(source, 8, 0);

		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, source,
			InsertBottom));
		var stored = MuiListCore.GetEntry(ref platform, State, list, 0,
			APTR.Null);
		Assert.NotEqual(source, stored);
		var storedFirst = APTR.FromPointer(platform.ReadUInt32(stored, 0));
		var storedSecond = APTR.FromPointer(platform.ReadUInt32(stored, 4));
		Assert.NotEqual(first, storedFirst);
		Assert.NotEqual(second, storedSecond);
		Assert.Equal((byte)'a', platform.ReadUInt8(storedFirst, 0));
		Assert.Equal((byte)'0', platform.ReadUInt8(storedSecond, 0));

		var display = APTR.FromPointer(0x2300);
		Assert.True(MuiListCore.Display(ref platform, State, list, stored,
			display, 0));
		Assert.Equal(storedFirst.Raw, platform.ReadUInt32(display, 0));
		Assert.Equal(storedSecond.Raw, platform.ReadUInt32(display, 4));
		Assert.Equal(0u, platform.ReadUInt32(display, 8));
		Assert.True(MuiListCore.Compare(ref platform, State, list, stored, stored,
			1) == 0);

		var other = APTR.FromPointer(0x2400);
		var otherFirst = APTR.FromPointer(0x2480);
		var otherSecond = APTR.FromPointer(0x24C0);
		platform.WriteCString(otherFirst, "alpha");
		platform.WriteCString(otherSecond, "002");
		platform.WriteUInt32(other, 0, otherFirst.Raw);
		platform.WriteUInt32(other, 4, otherSecond.Raw);
		platform.WriteUInt32(other, 8, 0);
		Assert.True(MuiListCore.Compare(ref platform, State, list, stored, other,
			1) < 0);

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State, list));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void StringArrayDisplayRejectsUnmappedTerminatorThroughCursor()
	{
		var platform = CreatePlatform(out var listClass, out var otherClass,
			0x40000);
		var tags = APTR.FromPointer(0x2000);
		platform.WriteUInt32(tags, 0, DisplayHookAttr);
		platform.WriteUInt32(tags, 4, HookStringArray);
		platform.WriteUInt32(tags, 8, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		var text = APTR.FromPointer(0x2800);
		platform.WriteCString(text, "bounded");
		// The first slot ends exactly at the arena boundary; the required
		// terminator slot is therefore unmapped and must be rejected.
		var source = APTR.FromPointer(0x40FFC);
		platform.WriteUInt32(source, 0, text.Raw);
		var display = APTR.FromPointer(0x3000);

		Assert.False(MuiListCore.Display(ref platform, State, list, source,
			display, 0));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void SortColumnClampsToNamedFormatColumnsAndSortsStringArrayData()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var format = APTR.FromPointer(0x2500);
		platform.WriteCString(format, ",");
		var tags = APTR.FromPointer(0x2600);
		platform.WriteUInt32(tags, 0, ConstructHookAttr);
		platform.WriteUInt32(tags, 4, HookStringArray);
		platform.WriteUInt32(tags, 8, DestructHookAttr);
		platform.WriteUInt32(tags, 12, HookStringArray);
		platform.WriteUInt32(tags, 16, DisplayHookAttr);
		platform.WriteUInt32(tags, 20, HookStringArray);
		platform.WriteUInt32(tags, 24, CompareHookAttr);
		platform.WriteUInt32(tags, 28, HookStringArray);
		platform.WriteUInt32(tags, 32, FormatAttr);
		platform.WriteUInt32(tags, 36, format.Raw);
		platform.WriteUInt32(tags, 40, MaxColumnsAttr);
		platform.WriteUInt32(tags, 44, 2);
		platform.WriteUInt32(tags, 48, SortColumnAttr);
		platform.WriteUInt32(tags, 52, 1);
		platform.WriteUInt32(tags, 56, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.Equal(1u, Get(ref platform, list, SortColumnAttr));
		var alpha = APTR.FromPointer(0x2700);
		var alphaFirst = APTR.FromPointer(0x2740);
		var alphaSecond = APTR.FromPointer(0x2780);
		var zulu = APTR.FromPointer(0x27C0);
		var zuluFirst = APTR.FromPointer(0x2800);
		var zuluSecond = APTR.FromPointer(0x2840);
		platform.WriteCString(alphaFirst, "same");
		platform.WriteCString(alphaSecond, "alpha");
		platform.WriteCString(zuluFirst, "same");
		platform.WriteCString(zuluSecond, "zulu");
		platform.WriteUInt32(alpha, 0, alphaFirst.Raw);
		platform.WriteUInt32(alpha, 4, alphaSecond.Raw);
		platform.WriteUInt32(alpha, 8, 0);
		platform.WriteUInt32(zulu, 0, zuluFirst.Raw);
		platform.WriteUInt32(zulu, 4, zuluSecond.Raw);
		platform.WriteUInt32(zulu, 8, 0);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, zulu,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, alpha,
			InsertBottom));
		Assert.True(MuiListCore.Sort(ref platform, State, list));
		var sorted = MuiListCore.GetEntry(ref platform, State, list, 0,
			APTR.Null);
		Assert.NotEqual(APTR.Null, sorted);
		Assert.Equal((byte)'a', platform.ReadUInt8(
			APTR.FromPointer(platform.ReadUInt32(sorted, 4)), 0));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			SortColumnAttr, 99));
		Assert.Equal(1u, Get(ref platform, list, SortColumnAttr));
	}

	[Fact]
	public void ChangeOnlyNotificationsFireForRealMutationsOnly()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		var follow = APTR.FromPointer(0x2000);
		platform.WriteUInt32(follow, 0, 0x90000001);
		platform.WriteUInt32(follow, 4, EveryTime);
		Assert.True(MuiNotifyCore.Add(ref platform, State, list, EntriesAttr,
			EveryTime, list, 2, follow));
		var baseline = platform.DispatchCount;
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x3000001), InsertBottom));
		Assert.Equal(baseline + 1, platform.DispatchCount); // Entries changed
		var afterInsert = platform.DispatchCount;
		MuiListCore.GetEntry(ref platform, State, list, 0, APTR.Null);
		Assert.Equal(afterInsert, platform.DispatchCount); // read: no notify
	}

	[Fact]
	public void SelectionChangeNotifiesSelectedRemovalAndClear()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x41000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		for (var i = 0u; i < 3; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
				APTR.FromPointer(0x41000 + i), InsertBottom));
		var follow = APTR.FromPointer(0x41100);
		platform.WriteUInt32(follow, 0, 0x90000002);
		platform.WriteUInt32(follow, 4, EveryTime);
		Assert.True(MuiNotifyCore.Add(ref platform, State, list,
			SelectChangeAttr, EveryTime, list, 2, follow));

		Assert.True(MuiListCore.Select(ref platform, State, list, 1, SelectOn,
			APTR.Null));
		var afterSelect = platform.DispatchCount;
		Assert.True(MuiListCore.Remove(ref platform, State, list, 1));
		Assert.Equal(afterSelect + 1, platform.DispatchCount);

		Assert.True(MuiListCore.Select(ref platform, State, list, 0, SelectOn,
			APTR.Null));
		var afterReselect = platform.DispatchCount;
		Assert.True(MuiListCore.Clear(ref platform, State, list));
		Assert.Equal(afterReselect + 1, platform.DispatchCount);
	}

	[Fact]
	public void SelectionChangeUsesNamedSignalRecord()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x42000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.True(MuiListCore.TryGetSelectionSignal(ref platform, State, list,
			out var signal));
		Assert.Equal(0u, signal.Value);

		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x4C00), InsertBottom));
		Assert.True(MuiListCore.Select(ref platform, State, list, 0, SelectOn,
			APTR.Null));
		Assert.True(MuiListCore.TryGetSelectionSignal(ref platform, State, list,
			out signal));
		Assert.Equal(1u, signal.Value);
		Assert.Equal(1u, Get(ref platform, list, SelectChangeAttr));

		Assert.True(MuiListCore.Select(ref platform, State, list, 0, SelectOff,
			APTR.Null));
		Assert.True(MuiListCore.TryGetSelectionSignal(ref platform, State, list,
			out signal));
		Assert.Equal(0u, signal.Value);
		Assert.Equal(0u, Get(ref platform, list, SelectChangeAttr));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
	}

	[Fact]
	public void DispatcherRoutesListMethodsAndRedraws()
	{
		var platform = CreatePlatform(out var listClass, out var otherClass,
			0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		var packet = APTR.FromPointer(0x2000);
		// MUIM_List_InsertSingle(entry, MUIV_List_Insert_Bottom)
		platform.WriteUInt32(packet, 0, 0x804254d5u);
		platform.WriteUInt32(packet, 4, 0x3300001);
		platform.WriteUInt32(packet, 8, unchecked((uint)InsertBottom));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			list, packet));
		// MUIM_List_GetEntry(0, &storage)
		var storage = APTR.FromPointer(0x2100);
		platform.WriteUInt32(packet, 0, 0x804280ecu);
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, storage.Raw);
		Assert.Equal(0x3300001u, MuiCollectionDispatcher.Dispatch(ref platform,
			State, list, packet));
		Assert.Equal(0x3300001u, platform.ReadUInt32(storage, 0));
		// MUIM_List_Redraw(MUIV_List_Redraw_All) schedules a redraw.
		var beforeRedraw = platform.RedrawCount;
		platform.WriteUInt32(packet, 0, 0x80427993u);
		platform.WriteUInt32(packet, 4, unchecked((uint)(-2)));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			list, packet));
		Assert.Equal(beforeRedraw + 1, platform.RedrawCount);
		// A List method aimed at a non-List object falls through unchanged.
		var other = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			otherClass, APTR.Null);
		platform.WriteUInt32(packet, 0, 0x8042ad89u); // MUIM_List_Clear
		Assert.Equal(0u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			other, packet));
	}

	[Fact]
	public void RedrawSkipsRowsOutsideVisibleViewportAndInactiveSentinel()
	{
		var platform = CreatePlatform(out var listClass, out var otherClass,
			0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		for (var i = 0u; i < 3; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
				APTR.FromPointer(0x4300 + i * 0x40), InsertBottom));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 16));
		Assert.Equal(2u, Get(ref platform, list, VisibleAttr));

		var before = platform.RedrawCount;
		Assert.True(MuiListCore.Redraw(ref platform, State, list, 2));
		Assert.Equal(before, platform.RedrawCount);
		Assert.True(MuiListCore.Redraw(ref platform, State, list, 1));
		Assert.Equal(before + 1, platform.RedrawCount);

		// No active row exists yet, so MUIV_List_Redraw_Active is a no-op.
		Assert.True(MuiListCore.Redraw(ref platform, State, list, -1));
		Assert.Equal(before + 1, platform.RedrawCount);
		Assert.True(MuiListCore.Redraw(ref platform, State, list, -2));
		Assert.Equal(before + 2, platform.RedrawCount);

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void VisibleRowsTrackGeometryWhenListIsShorter()
	{
		var platform = CreatePlatform(out var listClass, out var otherClass,
			0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x4500), InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x4540), InsertBottom));

		var renderInfo = APTR.FromPointer(0x4580);
		platform.WriteUInt32(renderInfo, 20, 0x4600);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, list,
			renderInfo));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 40));
		Assert.Equal(5u, Get(ref platform, list, VisibleAttr));
		Assert.Equal(0u, Get(ref platform, list, FirstAttr));
		Assert.Equal(40u, Get(ref platform, list, VisiblePixelAttr));
		Assert.Equal(16u, Get(ref platform, list, TotalPixelAttr));

		// A short list has no scrollable first row even though its geometry can
		// display more rows than the list contains.
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			FirstAttr, 99));
		Assert.Equal(0u, Get(ref platform, list, FirstAttr));

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void InvisibleListPublishesMorphosOffViewportSentinels()
	{
		var platform = CreatePlatform(out var listClass, out var otherClass,
			0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x4700), InsertBottom));

		// MorphOS reports LONG -1 while a List has no visible rectangle. The
		// named viewport record still keeps pixel metrics bounded and useful for
		// the later visible layout pass.
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 0));
		Assert.Equal(uint.MaxValue, Get(ref platform, list, VisibleAttr));
		Assert.Equal(uint.MaxValue, Get(ref platform, list, FirstAttr));
		Assert.Equal(0u, Get(ref platform, list, TopPixelAttr));
		Assert.Equal(0u, Get(ref platform, list, VisiblePixelAttr));
		Assert.Equal(8u, Get(ref platform, list, TotalPixelAttr));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			FirstAttr, 7));
		Assert.Equal(uint.MaxValue, Get(ref platform, list, FirstAttr));

		// A later non-zero layout restores normal row capacity and the first-row
		// projection without rebuilding the List object.
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 8));
		Assert.Equal(1u, Get(ref platform, list, VisibleAttr));
		Assert.Equal(0u, Get(ref platform, list, FirstAttr));

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void CollectionDispatcherMalformedSurfaceStaysOnNamedPacketRoute()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		// Only the method word is mapped. The typed surface codec must claim the
		// recognized method and reject the truncated payload without allowing a
		// second offset-based fallback to inspect guest memory.
		var truncated = APTR.FromPointer(0x40FF8);
		platform.WriteUInt32(truncated, 0, LayoutMethod);
		Assert.True(MuiCollectionDispatcher.TryDispatch(ref platform, State, list,
			truncated, out var result));
		Assert.Equal(0u, result);
	}

	[Fact]
	public void ListLayoutAndDrawPublishRowsThroughCollectionDispatcher()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var first = APTR.FromPointer(0x3000);
		var second = APTR.FromPointer(0x3040);
		platform.WriteCString(first, "alpha");
		platform.WriteCString(second, "bravo");
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, first,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, second,
			InsertBottom));
		var renderInfo = APTR.FromPointer(0x3800);
		platform.WriteUInt32(renderInfo, 20, 0x3900);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, list,
			renderInfo));
		var packet = APTR.FromPointer(0x3A00);
		platform.WriteUInt32(packet, 0, AskMinMaxMethod);
		platform.WriteUInt32(packet, 4, 0x3B00);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		Assert.True(platform.ReadUInt16(APTR.FromPointer(0x3B00), 2) >= 8);

		platform.WriteUInt32(packet, 0, LayoutMethod);
		platform.WriteUInt32(packet, 4, 4);
		platform.WriteUInt32(packet, 8, 6);
		platform.WriteUInt32(packet, 12, 80);
		platform.WriteUInt32(packet, 16, 16);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		Assert.Equal(80u, Get(ref platform, list, 0x8042b59cu));
		Assert.Equal(16u, Get(ref platform, list, 0x80423237u));

		platform.TextCount = 0;
		platform.WriteUInt32(packet, 0, DrawMethod);
		platform.WriteUInt32(packet, 4, 0);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		Assert.Equal(2u, platform.TextCount);
		Assert.Equal(5, platform.LastTextLength);
		Assert.Equal((byte)'b', platform.ReadUInt8(platform.LastText, 0));
	}

	[Fact]
	public void TestPosPublishesEntryColumnAndCellOffsetsThroughDispatcher()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var format = APTR.FromPointer(0x3C00);
		platform.WriteCString(format,
			"MINWIDTH=16px WEIGHT=1,MINWIDTH=8px MAXWIDTH=24px WEIGHT=3");
		var tags = APTR.FromPointer(0x3D00);
		platform.WriteUInt32(tags, 0, FormatAttr);
		platform.WriteUInt32(tags, 4, format.Raw);
		platform.WriteUInt32(tags, 8, MaxColumnsAttr);
		platform.WriteUInt32(tags, 12, 2);
		platform.WriteUInt32(tags, 16, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		var first = APTR.FromPointer(0x3E00);
		var second = APTR.FromPointer(0x3E40);
		platform.WriteCString(first, "alpha");
		platform.WriteCString(second, "bravo");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, first,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, second,
			InsertBottom));
		var renderInfo = APTR.FromPointer(0x3E80);
		platform.WriteUInt32(renderInfo, 20, 0x3F00);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, list,
			renderInfo));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 10, 20, 64,
			16));

		var result = APTR.FromPointer(0x3F40);
		Assert.True(MuiListCore.TestPos(ref platform, State, list, 22, 12,
			result));
		Assert.Equal(1u, platform.ReadUInt32(result, 0));
		Assert.Equal(unchecked((ushort)1), platform.ReadUInt16(result, 4));
		Assert.Equal((ushort)0, platform.ReadUInt16(result, 6));
		Assert.Equal((ushort)2, platform.ReadUInt16(result, 8));
		Assert.Equal((ushort)0, platform.ReadUInt16(result, 10));

		// The dispatcher exposes the same ABI and preserves signed -1/-1 for a
		// point outside the viewport, with the public LEFT/ABOVE flags set.
		var packet = APTR.FromPointer(0x3F60);
		platform.WriteUInt32(packet, 0, TestPosMethod);
		platform.WriteUInt32(packet, 4, unchecked((uint)-1));
		platform.WriteUInt32(packet, 8, unchecked((uint)-1));
		platform.WriteUInt32(packet, 12, result.Raw);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			list, packet));
		Assert.Equal(unchecked((uint)-1), platform.ReadUInt32(result, 0));
		Assert.Equal(unchecked((ushort)-1), platform.ReadUInt16(result, 4));
		Assert.Equal((ushort)5, platform.ReadUInt16(result, 6));
	}

	[Fact]
	public void ListGeometryConsumersReconcileSharedAreaRecord()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.True(MuiListCore.Layout(ref platform, State, list, 10, 20, 64,
			16));
		Assert.True(MuiAreaLayoutCore.TryGetGeometryStateRecord(ref platform,
			State, list, out var initialGeometry));
		Assert.Equal(64, initialGeometry.Width);

		// Simulate a public projection write made outside the Area layout method.
		// TestPos must cross the shared geometry boundary so the named record is
		// reconciled before the hit-test consumes the dimensions.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, list,
			WidthAttr, 32, false));
		var result = APTR.FromPointer(0x4000);
		Assert.True(MuiListCore.TestPos(ref platform, State, list, 40, 4,
			result));
		Assert.True(MuiAreaLayoutCore.TryGetGeometryStateRecord(ref platform,
			State, list, out var reconciledGeometry));
		Assert.Equal(32, reconciledGeometry.Width);
		Assert.Equal(10, reconciledGeometry.Left);
		Assert.Equal(20, reconciledGeometry.Top);
	}

	[Fact]
	public void ImageHandlesAreGuestOwnedAndDispatcherDeletable()
	{
		var platform = CreatePlatform(out var listClass, out var otherClass,
			0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		var imageObject = APTR.FromPointer(0x4A00);
		var first = MuiListCore.CreateImage(ref platform, State, list,
			imageObject, 3);
		Assert.NotEqual(APTR.Null, first);
		Assert.Equal(1u, MuiListCore.ImageCount(ref platform, State, list));

		var packet = APTR.FromPointer(0x4B00);
		platform.WriteUInt32(packet, 0, CreateImageMethod);
		platform.WriteUInt32(packet, 4, imageObject.Raw);
		platform.WriteUInt32(packet, 8, 7);
		var second = APTR.FromPointer(MuiCollectionDispatcher.Dispatch(
			ref platform, State, list, packet));
		Assert.NotEqual(APTR.Null, second);
		Assert.NotEqual(first, second);
		Assert.Equal(2u, MuiListCore.ImageCount(ref platform, State, list));

		platform.WriteUInt32(packet, 0, DeleteImageMethod);
		platform.WriteUInt32(packet, 4, first.Raw);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			list, packet));
		Assert.Equal(1u, MuiListCore.ImageCount(ref platform, State, list));
		Assert.True(MuiListCore.DeleteImage(ref platform, State, list, second));
		Assert.False(MuiListCore.DeleteImage(ref platform, State, list, second));
		Assert.Equal(0u, MuiListCore.ImageCount(ref platform, State, list));

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State, list));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void SelectConsultsMultiTestHookBeforeAddingRows()
	{
		var platform = CreatePlatform(out var listClass, out var otherClass,
			0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		var first = APTR.FromPointer(0x4500);
		var second = APTR.FromPointer(0x4540);
		platform.WriteCString(first, "denied");
		platform.WriteCString(second, "accepted");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, first,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, second,
			InsertBottom));

		var hook = APTR.FromPointer(0x4600);
		var hookData = APTR.FromPointer(0x4640);
		platform.WriteUInt32(hook, 8, MuiHeadlessTestPlatform.HookEntryMultiTest);
		platform.WriteUInt32(hook, 16, hookData.Raw);
		platform.WriteUInt32(hookData, 0, first.Raw); // deny the first row
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			MultiTestHookAttr, hook.Raw));

		var result = APTR.FromPointer(0x4680);
		Assert.True(MuiListCore.Select(ref platform, State, list, 0, SelectOn,
			APTR.Null));
		Assert.True(MuiListCore.Select(ref platform, State, list, 0, SelectAsk,
			result));
		Assert.Equal(0u, platform.ReadUInt32(result, 0));

		Assert.True(MuiListCore.Select(ref platform, State, list, 1, SelectOn,
			APTR.Null));
		Assert.True(MuiListCore.Select(ref platform, State, list, 1, SelectAsk,
			result));
		Assert.Equal(1u, platform.ReadUInt32(result, 0));

		// Once selected, a row can always be removed from the selection; the
		// admission hook only controls joining a multi-selection.
		Assert.True(MuiListCore.Select(ref platform, State, list, 1,
			SelectToggle, APTR.Null));
		Assert.True(MuiListCore.Select(ref platform, State, list, 1, SelectAsk,
			result));
		Assert.Equal(0u, platform.ReadUInt32(result, 0));
		Assert.Equal(2u, platform.HookInvokeCount);

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void ActiveSelectorsClampAndKeepCursorVisibleThroughSetPath()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		for (var i = 0u; i < 8; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
				APTR.FromPointer(0x4400000 + i), InsertBottom));
		// Visible is a layout-published value; seed it in the raw attribute store
		// before exercising class-aware Active/First/Quiet setters.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, list,
			VisibleAttr, 3, false));
		var packet = APTR.FromPointer(0x2600);
		platform.WriteUInt32(packet, 0, 0x8042549Au); // MUIM_Set
		platform.WriteUInt32(packet, 4, ActiveAttr);
		platform.WriteUInt32(packet, 8, unchecked((uint)-2)); // Top
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		Assert.Equal(0u, Get(ref platform, list, ActiveAttr));
		Assert.Equal(0u, Get(ref platform, list, FirstAttr));

		platform.WriteUInt32(packet, 8, unchecked((uint)-7)); // PageDown
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		Assert.Equal(3u, Get(ref platform, list, ActiveAttr));
		Assert.Equal(1u, Get(ref platform, list, FirstAttr));

		platform.WriteUInt32(packet, 8, unchecked((uint)-3)); // Bottom
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		Assert.Equal(7u, Get(ref platform, list, ActiveAttr));
		Assert.Equal(5u, Get(ref platform, list, FirstAttr));

		platform.WriteUInt32(packet, 4, FirstAttr);
		platform.WriteUInt32(packet, 8, 0x7FFFFFFFu); // clamp to last page
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		Assert.Equal(5u, Get(ref platform, list, FirstAttr));
		platform.WriteUInt32(packet, 8, unchecked((uint)-1)); // not visible
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		Assert.Equal(0xFFFFFFFFu, Get(ref platform, list, FirstAttr));

		platform.WriteUInt32(packet, 4, ActiveAttr);
		platform.WriteUInt32(packet, 8, 0x7FFFFFFFu); // explicit index clamps
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		Assert.Equal(7u, Get(ref platform, list, ActiveAttr));
		Assert.Equal(5u, Get(ref platform, list, FirstAttr));
		platform.WriteUInt32(packet, 8, 9); // Quiet is a BOOL, not an integer
		platform.WriteUInt32(packet, 4, QuietAttr);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		Assert.Equal(1u, Get(ref platform, list, QuietAttr));
		platform.WriteUInt32(packet, 8, 0);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		Assert.Equal(0u, Get(ref platform, list, QuietAttr));
	}

	[Fact]
	public void ActiveStateUsesNamedCursorValueAndPresenceBit()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.TryGetActiveState(ref platform, State, list,
			out var active));
		Assert.Equal(0u, active.Active);
		Assert.Equal(0u, active.HasActive);

		for (var row = 0u; row < 3; row++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
				APTR.FromPointer(0x4A00 + row * 0x20), InsertBottom));
		Assert.True(MuiListCore.TryGetActiveState(ref platform, State, list,
			out active));
		Assert.Equal(0u, active.Active);
		Assert.Equal(0u, active.HasActive);
		Assert.Equal(-1, MuiListCore.ActiveRow(ref platform, State, list));

		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			ActiveAttr, 2));
		Assert.True(MuiListCore.TryGetActiveState(ref platform, State, list,
			out active));
		Assert.Equal(2u, active.Active);
		Assert.Equal(1u, active.HasActive);
		Assert.Equal(2, MuiListCore.ActiveRow(ref platform, State, list));

		Assert.True(MuiListCore.Clear(ref platform, State, list));
		Assert.True(MuiListCore.TryGetActiveState(ref platform, State, list,
			out active));
		Assert.Equal(0u, active.Active);
		Assert.Equal(0u, active.HasActive);
		Assert.Equal(-1, MuiListCore.ActiveRow(ref platform, State, list));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
	}

	[Fact]
	public void Morphos320EmptyListActivePublishesZeroWithoutCreatingCursor()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.Equal(0u, Get(ref platform, list, ActiveAttr));

		// MorphOS 3.20 exposes zero for all requests while there is no row to own
		// the cursor. The named record remains present, but its HasActive bit keeps
		// internal selectors from treating the public zero as row zero.
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list, ActiveAttr,
			unchecked((uint)-2), false));
		Assert.Equal(0u, Get(ref platform, list, ActiveAttr));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list, ActiveAttr,
			unchecked((uint)-3), false));
		Assert.Equal(0u, Get(ref platform, list, ActiveAttr));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list, ActiveAttr,
			123, false));
		Assert.Equal(0u, Get(ref platform, list, ActiveAttr));

		var entry = APTR.FromPointer(0x4480000);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, entry,
			InsertBottom));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list, ActiveAttr,
			unchecked((uint)-2), false));
		Assert.Equal(0u, Get(ref platform, list, ActiveAttr));
		Assert.True(MuiListCore.Clear(ref platform, State, list));
		Assert.Equal(0u, Get(ref platform, list, ActiveAttr));
	}

	[Fact]
	public void CollectionDispatcherRoutesListMethodsForAListObject()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		var packet = APTR.FromPointer(0x2700);
		platform.WriteUInt32(packet, 0, 0x804254d5u); // MUIM_List_InsertSingle
		platform.WriteUInt32(packet, 4, 0x5500001);
		platform.WriteUInt32(packet, 8, unchecked((uint)InsertBottom));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		Assert.Equal(APTR.FromPointer(0x5500001), MuiListCore.GetEntry(
			ref platform, State, list, 0, APTR.Null));
	}

	[Fact]
	public void MorphosListEditPacketsUseOneGuestResidentInlineStringSession()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var stringName = APTR.FromPointer(0x1180);
		platform.WriteCString(stringName, "String.mui");
		var stringClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			stringName, APTR.Null, 0, APTR.FromPointer(1), false);
		Assert.NotEqual(APTR.Null, stringClass);
		var entry = APTR.FromPointer(0x3000);
		platform.WriteCString(entry, "alpha");
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, entry,
			InsertBottom));
		Assert.False(MuiListCore.Edit(ref platform, State, list, 0, 0));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			EditableAttr, 1));

		var packet = APTR.FromPointer(0x3200);
		platform.WriteUInt32(packet, 0, 0x804219aeu); // MUIM_List_CreateEditObject
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, 0);
		platform.WriteUInt32(packet, 12, entry.Raw);
		var editor = APTR.FromPointer(MuiCollectionDispatcher.Dispatch(ref platform,
			State, list, packet));
		Assert.NotEqual(APTR.Null, editor);
		Assert.Equal(MuiControlClass.String, MuiCommonControlCore.Classify(
			ref platform, State, editor));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, editor,
			StringContentsAttr, out var editorContents));
		Assert.NotEqual(0u, editorContents);
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, editor));

		platform.WriteUInt32(packet, 0, 0x8042843du); // MUIM_List_Edit
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, 0);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		platform.WriteUInt32(packet, 0, 0x80423ab3u); // MUIM_List_EditDone
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, 0);
		platform.WriteUInt32(packet, 12, entry.Raw);
		platform.WriteUInt32(packet, 16, 0); // current editor is implicit
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		platform.WriteUInt32(packet, 0, 0x8042843du); // MUIM_List_Edit
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		platform.WriteUInt32(packet, 0, 0x804203eeu); // MUIM_List_EndEdit
		platform.WriteUInt32(packet, 4, 1); // MUIV_List_EndEdit_Abort
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		Assert.Equal(0u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
	}

	[Fact]
	public void MorphosListEditDoneCommitsOwnedDefaultStringEntry()
	{
		var platform = CreatePlatform(out var listClass, out var otherClass,
			0x40000);
		var stringName = APTR.FromPointer(0x1180);
		platform.WriteCString(stringName, "String.mui");
		var stringClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			stringName, APTR.Null, 0, APTR.FromPointer(1), false);
		Assert.NotEqual(APTR.Null, stringClass);

		var tags = APTR.FromPointer(0x2400);
		platform.WriteUInt32(tags, 0, ConstructHookAttr);
		platform.WriteUInt32(tags, 4, HookString);
		platform.WriteUInt32(tags, 8, DestructHookAttr);
		platform.WriteUInt32(tags, 12, HookString);
		platform.WriteUInt32(tags, 16, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		var entry = APTR.FromPointer(0x2500);
		var replacementText = APTR.FromPointer(0x2580);
		platform.WriteCString(entry, "alpha");
		platform.WriteCString(replacementText, "beta");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, entry,
			InsertBottom));
		var storedBeforeEdit = MuiListCore.GetEntry(ref platform, State, list, 0,
			APTR.Null);
		Assert.NotEqual(entry, storedBeforeEdit);
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			EditableAttr, 1));
		Assert.True(MuiListCore.Select(ref platform, State, list, 0, SelectOn,
			APTR.Null));
		var selection = APTR.FromPointer(0x2700);
		Assert.True(MuiListCore.Select(ref platform, State, list, 0, SelectAsk,
			selection));
		Assert.Equal(1u, platform.ReadUInt32(selection, 0));

		var packet = APTR.FromPointer(0x2600);
		platform.WriteUInt32(packet, 0, 0x8042843du); // MUIM_List_Edit
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, 0);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		platform.WriteUInt32(packet, 0, 0x804219aeu); // MUIM_List_CreateEditObject
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, 0);
		platform.WriteUInt32(packet, 12, storedBeforeEdit.Raw);
		var editor = APTR.FromPointer(MuiCollectionDispatcher.Dispatch(ref platform,
			State, list, packet));
		Assert.NotEqual(APTR.Null, editor);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, editor,
			StringContentsAttr, replacementText.Raw, false));

		platform.WriteUInt32(packet, 0, 0x80423ab3u); // MUIM_List_EditDone
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, 0);
		platform.WriteUInt32(packet, 12, storedBeforeEdit.Raw);
		platform.WriteUInt32(packet, 16, editor.Raw);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		var stored = MuiListCore.GetEntry(ref platform, State, list, 0,
			APTR.Null);
		Assert.NotEqual(entry, stored);
		Assert.Equal("beta", ReadCString(ref platform, stored));
		Assert.True(MuiListCore.Select(ref platform, State, list, 0, SelectAsk,
			selection));
		Assert.Equal(1u, platform.ReadUInt32(selection, 0));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State, list));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			stringClass));
	}

	[Fact]
	public void MorphosListEditDoneCommitsSelectedStringArrayColumn()
	{
		var platform = CreatePlatform(out var listClass, out var otherClass,
			0x50000);
		var stringName = APTR.FromPointer(0x1180);
		platform.WriteCString(stringName, "String.mui");
		var stringClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			stringName, APTR.Null, 0, APTR.FromPointer(1), false);
		Assert.NotEqual(APTR.Null, stringClass);

		var tags = APTR.FromPointer(0x2400);
		platform.WriteUInt32(tags, 0, ConstructHookAttr);
		platform.WriteUInt32(tags, 4, HookStringArray);
		platform.WriteUInt32(tags, 8, DestructHookAttr);
		platform.WriteUInt32(tags, 12, HookStringArray);
		platform.WriteUInt32(tags, 16, MaxColumnsAttr);
		platform.WriteUInt32(tags, 20, 2);
		var format = APTR.FromPointer(0x2300);
		platform.WriteCString(format, "COL=1,COL=0");
		platform.WriteUInt32(tags, 24, FormatAttr);
		platform.WriteUInt32(tags, 28, format.Raw);
		platform.WriteUInt32(tags, 32, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		var first = APTR.FromPointer(0x2500);
		var second = APTR.FromPointer(0x2540);
		var replacementText = APTR.FromPointer(0x2580);
		var source = APTR.FromPointer(0x2600);
		platform.WriteCString(first, "alpha");
		platform.WriteCString(second, "001");
		platform.WriteCString(replacementText, "002");
		platform.WriteUInt32(source, 0, first.Raw);
		platform.WriteUInt32(source, 4, second.Raw);
		platform.WriteUInt32(source, 8, 0);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, source,
			InsertBottom));
		var storedBeforeEdit = MuiListCore.GetEntry(ref platform, State, list, 0,
			APTR.Null);
		Assert.NotEqual(source, storedBeforeEdit);
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			EditableAttr, 1));
		Assert.True(MuiListCore.Select(ref platform, State, list, 0, SelectOn,
			APTR.Null));
		var selection = APTR.FromPointer(0x2780);
		Assert.True(MuiListCore.Select(ref platform, State, list, 0, SelectAsk,
			selection));
		Assert.Equal(1u, platform.ReadUInt32(selection, 0));
		Assert.Equal(2u, MuiListCore.FormatColumnCount(ref platform, State, list));
		var layoutPacket = APTR.FromPointer(0x2800);
		platform.WriteUInt32(layoutPacket, 0, LayoutMethod);
		platform.WriteUInt32(layoutPacket, 4, 10);
		platform.WriteUInt32(layoutPacket, 8, 20);
		platform.WriteUInt32(layoutPacket, 12, 100);
		platform.WriteUInt32(layoutPacket, 16, 24);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			list, layoutPacket));

		var packet = APTR.FromPointer(0x2700);
		platform.WriteUInt32(packet, 0, 0x8042843du); // MUIM_List_Edit
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, 1);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		platform.WriteUInt32(packet, 0, 0x804219aeu); // MUIM_List_CreateEditObject
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, 1);
		platform.WriteUInt32(packet, 12, storedBeforeEdit.Raw);
		var editor = APTR.FromPointer(MuiCollectionDispatcher.Dispatch(ref platform,
			State, list, packet));
		Assert.NotEqual(APTR.Null, editor);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, editor,
			LeftEdgeAttr, out var editorLeft));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, editor,
			TopEdgeAttr, out var editorTop));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, editor,
			WidthAttr, out var editorWidth));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, editor,
			HeightAttr, out var editorHeight));
		Assert.Equal(62u, editorLeft);
		Assert.Equal(20u, editorTop);
		Assert.Equal(48u, editorWidth);
		Assert.Equal(8u, editorHeight);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, editor,
			StringContentsAttr, out var editorContents));
		Assert.Equal("alpha", ReadCString(ref platform,
			APTR.FromPointer(editorContents)));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, editor,
			StringContentsAttr, replacementText.Raw, false));

		platform.WriteUInt32(packet, 0, 0x80423ab3u); // MUIM_List_EditDone
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, 1);
		platform.WriteUInt32(packet, 12, storedBeforeEdit.Raw);
		platform.WriteUInt32(packet, 16, editor.Raw);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		var stored = MuiListCore.GetEntry(ref platform, State, list, 0,
			APTR.Null);
		Assert.NotEqual(storedBeforeEdit, stored);
		Assert.Equal("002", ReadCString(ref platform,
			APTR.FromPointer(platform.ReadUInt32(stored, 0))));
		Assert.Equal("001", ReadCString(ref platform,
			APTR.FromPointer(platform.ReadUInt32(stored, 4))));
		Assert.True(MuiListCore.Select(ref platform, State, list, 0, SelectAsk,
			selection));
		Assert.Equal(1u, platform.ReadUInt32(selection, 0));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State, list));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			stringClass));
	}

	[Fact]
	public void FormatRetainsGuestPointerAndDerivesBoundedColumnCount()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var format = APTR.FromPointer(0x2800);
		platform.WriteCString(format,
			"DELTA=8 WEIGHT=200 MINWIDTH=25 MAXWIDTH=50 BAR SORTABLE ORDER=DESC,COL=1 PREPARSE=P");
		var tags = APTR.FromPointer(0x2900);
		platform.WriteUInt32(tags, 0, FormatAttr);
		platform.WriteUInt32(tags, 4, format.Raw);
		platform.WriteUInt32(tags, 8, MaxColumnsAttr);
		platform.WriteUInt32(tags, 12, 4);
		platform.WriteUInt32(tags, 16, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.Equal(format.Raw, Get(ref platform, list, FormatAttr));
		Assert.Equal(4u, Get(ref platform, list, MaxColumnsAttr));
		Assert.Equal(2u, MuiListCore.FormatColumnCount(ref platform, State, list));
		var descriptor = APTR.FromPointer(0x2A00);
		Assert.True(MuiListCore.GetFormatColumn(ref platform, State, list, 0,
			descriptor));
		Assert.Equal(8u, platform.ReadUInt32(descriptor, 0)); // DELTA
		Assert.Equal(200u, platform.ReadUInt32(descriptor, 4)); // WEIGHT
		Assert.Equal(25u, platform.ReadUInt32(descriptor, 8)); // MINWIDTH
		Assert.Equal(50u, platform.ReadUInt32(descriptor, 12)); // MAXWIDTH
		Assert.Equal(7u, platform.ReadUInt32(descriptor, 20)); // BAR/SORTABLE/DESC
		Assert.True(MuiListCore.GetFormatColumn(ref platform, State, list, 1,
			descriptor));
		Assert.Equal(1u, platform.ReadUInt32(descriptor, 16)); // COL
		Assert.Equal(1u, platform.ReadUInt32(descriptor, 28)); // PREPARSE length

		var replacement = APTR.FromPointer(0x2840);
		platform.WriteCString(replacement, ",,,");
		var packet = APTR.FromPointer(0x2940);
		platform.WriteUInt32(packet, 0, 0x8042549Au); // MUIM_Set
		platform.WriteUInt32(packet, 4, FormatAttr);
		platform.WriteUInt32(packet, 8, replacement.Raw);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		Assert.Equal(4u, MuiListCore.FormatColumnCount(ref platform, State, list));

		platform.WriteUInt32(packet, 4, MaxColumnsAttr);
		platform.WriteUInt32(packet, 8, 2);
		// MUIA_List_MaxColumns is [I..] in MorphOS: it may be supplied while
		// constructing the list, but a later OM_SET must not rewrite the live
		// column policy or its named FORMAT projection.
		Assert.Equal(0u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		Assert.Equal(4u, Get(ref platform, list, MaxColumnsAttr));
		Assert.Equal(4u, MuiListCore.FormatColumnCount(ref platform, State, list));
	}

	[Fact]
	public void FormatPolicyUsesNamedStateRecord()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var format = APTR.FromPointer(0x3600);
		platform.WriteCString(format, ",,");
		var tags = APTR.FromPointer(0x3700);
		platform.WriteUInt32(tags, 0, FormatAttr);
		platform.WriteUInt32(tags, 4, format.Raw);
		platform.WriteUInt32(tags, 8, MaxColumnsAttr);
		platform.WriteUInt32(tags, 12, 4);
		platform.WriteUInt32(tags, 16, 0);

		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.TryGetFormatPolicyState(ref platform, State,
			list, out var policy));
		Assert.Equal(format, policy.Format);
		Assert.Equal(Get(ref platform, list, MaxColumnsAttr), policy.MaxColumns);
		Assert.Equal(3u, policy.Columns);
		Assert.Equal(policy.Columns,
			MuiListCore.FormatColumnCount(ref platform, State, list));

		var replacement = APTR.FromPointer(0x3640);
		platform.WriteCString(replacement, ",,,");
		var packet = APTR.FromPointer(0x3740);
		platform.WriteUInt32(packet, 0, 0x8042549Au); // MUIM_Set
		platform.WriteUInt32(packet, 4, FormatAttr);
		platform.WriteUInt32(packet, 8, replacement.Raw);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State,
			list, packet));
		Assert.True(MuiListCore.TryGetFormatPolicyState(ref platform, State,
			list, out policy));
		Assert.Equal(replacement, policy.Format);
		Assert.Equal(4u, policy.Columns);
		Assert.Equal(policy.Columns,
			MuiListCore.FormatColumnCount(ref platform, State, list));

		Assert.True(MuiListCore.TryGetFormatPolicyState(ref platform, State,
			list, out policy));
		Assert.Equal(MuiListCore.MuiListFormatPolicyState.Cookie, policy.Magic);
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
	}

	[Fact]
	public void FontUsesNamedCallerPointerState()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var initial = APTR.FromPointer(0x3800);
		var tags = APTR.FromPointer(0x3900);
		platform.WriteUInt32(tags, 0, FontAttr);
		platform.WriteUInt32(tags, 4, initial.Raw);
		platform.WriteUInt32(tags, 8, 0);

		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.TryGetFontState(ref platform, State, list,
			out var fontState));
		Assert.Equal(MuiListCore.MuiListFontState.Cookie, fontState.Magic);
		Assert.Equal(initial, fontState.Font);
		Assert.Equal(initial.Raw, Get(ref platform, list, FontAttr));

		var replacement = APTR.FromPointer(0x3840);
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			FontAttr, replacement.Raw, false));
		Assert.True(MuiListCore.TryGetFontState(ref platform, State, list,
			out fontState));
		Assert.Equal(replacement, fontState.Font);
		Assert.Equal(replacement.Raw, Get(ref platform, list, FontAttr));

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
	}

	[Fact]
	public void FormatReadArgsAliasesAndDescendingOrderAreStructBacked()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var format = APTR.FromPointer(0x2E00);
		platform.WriteCString(format,
			"d=0 p=\\33c w=1 miw=10px maw=80px c=0 bar sortable o=descending");
		var tags = APTR.FromPointer(0x2F00);
		platform.WriteUInt32(tags, 0, FormatAttr);
		platform.WriteUInt32(tags, 4, format.Raw);
		platform.WriteUInt32(tags, 8, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);

		var descriptor = APTR.FromPointer(0x3000);
		Assert.True(MuiListCore.GetFormatColumn(ref platform, State, list, 0,
			descriptor));
		Assert.Equal(0u, platform.ReadUInt32(descriptor, 0)); // D=DELTA
		Assert.Equal(1u, platform.ReadUInt32(descriptor, 4)); // W=WEIGHT
		Assert.Equal(10u, platform.ReadUInt32(descriptor, 8)); // MIW=MINWIDTH
		Assert.Equal(80u, platform.ReadUInt32(descriptor, 12)); // MAW=MAXWIDTH
		Assert.Equal(31u, platform.ReadUInt32(descriptor, 20)); // BAR/SORTABLE/DESC/px
		Assert.Equal(4u, platform.ReadUInt32(descriptor, 28)); // P=PREPARSE

		var zulu = APTR.FromPointer(0x3100);
		var alpha = APTR.FromPointer(0x3140);
		platform.WriteCString(zulu, "zulu");
		platform.WriteCString(alpha, "alpha");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, zulu,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, alpha,
			InsertBottom));
		Assert.True(MuiListCore.Sort(ref platform, State, list));
		Assert.Equal(zulu, MuiListCore.GetEntry(ref platform, State, list, 0,
			APTR.Null));
		Assert.Equal(alpha, MuiListCore.GetEntry(ref platform, State, list, 1,
			APTR.Null));
	}

	[Fact]
	public void FormatQuotedReadArgsValuesKeepEntryBoundariesAndRejectUnmatchedQuotes()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var format = APTR.FromPointer(0x3180);
		platform.WriteCString(format,
			"P=\"*ec,keep\" MAXWIDTH=\"25px\",O=\"DE*SC\"");
		var tags = APTR.FromPointer(0x31C0);
		platform.WriteUInt32(tags, 0, FormatAttr);
		platform.WriteUInt32(tags, 4, format.Raw);
		platform.WriteUInt32(tags, 8, MaxColumnsAttr);
		platform.WriteUInt32(tags, 12, 2);
		platform.WriteUInt32(tags, 16, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.Equal(2u, MuiListCore.FormatColumnCount(ref platform, State, list));

		var descriptor = APTR.FromPointer(0x3200);
		Assert.True(MuiListCore.GetFormatColumn(ref platform, State, list, 0,
			descriptor));
		var preparse = APTR.FromPointer(platform.ReadUInt32(descriptor, 24));
		Assert.NotEqual(APTR.Null, preparse);
		Assert.NotEqual(format.Raw + 3u, preparse.Raw);
		Assert.Equal(7u, platform.ReadUInt32(descriptor, 28));
		Assert.Equal(0x1Bu, platform.ReadUInt8(preparse, 0));
		Assert.Equal((byte)'c', platform.ReadUInt8(preparse, 1));
		Assert.Equal((byte)',', platform.ReadUInt8(preparse, 2));
		Assert.Equal(25u, platform.ReadUInt32(descriptor, 12));
		Assert.Equal(16u, platform.ReadUInt32(descriptor, 20));
		Assert.True(MuiListCore.GetFormatColumn(ref platform, State, list, 1,
			descriptor));
		Assert.Equal(4u, platform.ReadUInt32(descriptor, 20));

		var malformed = APTR.FromPointer(0x3240);
		platform.WriteCString(malformed, "P=\"unterminated");
		Assert.False(MuiListCore.SetAttribute(ref platform, State, list,
			FormatAttr, malformed.Raw, false));
		Assert.Equal(format.Raw, Get(ref platform, list, FormatAttr));
	}

	[Fact]
	public void FormatQuotedReadArgsDecodesNewlineAndEscapedQuoteValues()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var format = APTR.FromPointer(0x3380);
		// The first value contains *" (an escaped quote). The final quote on
		// that entry still closes the ReadArgs item, so the comma remains a
		// column separator. The third value verifies *n decoding.
		platform.WriteCString(format, "P=\"*\"c\",O=\"DE*SC\",P=\"*n\"");
		var tags = APTR.FromPointer(0x33C0);
		platform.WriteUInt32(tags, 0, FormatAttr);
		platform.WriteUInt32(tags, 4, format.Raw);
		platform.WriteUInt32(tags, 8, MaxColumnsAttr);
		platform.WriteUInt32(tags, 12, 3);
		platform.WriteUInt32(tags, 16, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.Equal(3u, MuiListCore.FormatColumnCount(ref platform, State, list));

		var descriptor = APTR.FromPointer(0x3400);
		Assert.True(MuiListCore.GetFormatColumn(ref platform, State, list, 0,
			descriptor));
		var escapedQuote = APTR.FromPointer(platform.ReadUInt32(descriptor, 24));
		Assert.Equal(2u, platform.ReadUInt32(descriptor, 28));
		Assert.Equal((byte)'\"', platform.ReadUInt8(escapedQuote, 0));
		Assert.Equal((byte)'c', platform.ReadUInt8(escapedQuote, 1));

		Assert.True(MuiListCore.GetFormatColumn(ref platform, State, list, 1,
			descriptor));
		Assert.Equal(4u, platform.ReadUInt32(descriptor, 20));
		Assert.True(MuiListCore.GetFormatColumn(ref platform, State, list, 2,
			descriptor));
		var newline = APTR.FromPointer(platform.ReadUInt32(descriptor, 24));
		Assert.Equal(1u, platform.ReadUInt32(descriptor, 28));
		Assert.Equal((byte)'\n', platform.ReadUInt8(newline, 0));

		var malformed = APTR.FromPointer(0x3440);
		platform.WriteCString(malformed, "P=\"*\"");
		Assert.False(MuiListCore.SetAttribute(ref platform, State, list,
			FormatAttr, malformed.Raw, false));
		Assert.Equal(format.Raw, Get(ref platform, list, FormatAttr));
		var outstanding = platform.AllocationCount - platform.FreeCount;
		platform.WriteCString(malformed, "P=\"ok\" UNKNOWN=1");
		Assert.False(MuiListCore.SetAttribute(ref platform, State, list,
			FormatAttr, malformed.Raw, false));
		Assert.Equal(outstanding, platform.AllocationCount - platform.FreeCount);
	}

	[Fact]
	public void FormatReadArgsAcceptsSpaceSeparatedKeywordsAndQuotedItems()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var format = APTR.FromPointer(0x3480);
		platform.WriteCString(format,
			"D = 4 W = 50 P \"*nc,keep\", O DESCENDING, BAR SORTABLE");
		var tags = APTR.FromPointer(0x34C0);
		platform.WriteUInt32(tags, 0, FormatAttr);
		platform.WriteUInt32(tags, 4, format.Raw);
		platform.WriteUInt32(tags, 8, MaxColumnsAttr);
		platform.WriteUInt32(tags, 12, 3);
		platform.WriteUInt32(tags, 16, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.Equal(3u, MuiListCore.FormatColumnCount(ref platform, State, list));

		var descriptor = APTR.FromPointer(0x3500);
		Assert.True(MuiListCore.GetFormatColumn(ref platform, State, list, 0,
			descriptor));
		Assert.Equal(4u, platform.ReadUInt32(descriptor, 0));
		Assert.Equal(50u, platform.ReadUInt32(descriptor, 4));
		var preparse = APTR.FromPointer(platform.ReadUInt32(descriptor, 24));
		Assert.Equal(7u, platform.ReadUInt32(descriptor, 28));
		Assert.Equal((byte)'\n', platform.ReadUInt8(preparse, 0));
		Assert.Equal((byte)'c', platform.ReadUInt8(preparse, 1));
		Assert.Equal((byte)',', platform.ReadUInt8(preparse, 2));
		Assert.Equal((byte)'k', platform.ReadUInt8(preparse, 3));
		Assert.Equal((byte)'e', platform.ReadUInt8(preparse, 4));
		Assert.Equal((byte)'e', platform.ReadUInt8(preparse, 5));
		Assert.Equal((byte)'p', platform.ReadUInt8(preparse, 6));

		Assert.True(MuiListCore.GetFormatColumn(ref platform, State, list, 1,
			descriptor));
		Assert.Equal(4u, platform.ReadUInt32(descriptor, 20));
		Assert.True(MuiListCore.GetFormatColumn(ref platform, State, list, 2,
			descriptor));
		Assert.Equal(3u, platform.ReadUInt32(descriptor, 20));
	}

	[Fact]
	public void FormatReadArgsRejectsMalformedFieldsAndKeepsInstalledState()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var format = APTR.FromPointer(0x3280);
		platform.WriteCString(format, "D=+8,O=ASC");
		var tags = APTR.FromPointer(0x32C0);
		platform.WriteUInt32(tags, 0, FormatAttr);
		platform.WriteUInt32(tags, 4, format.Raw);
		platform.WriteUInt32(tags, 8, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.Equal(format.Raw, Get(ref platform, list, FormatAttr));

		var malformed = APTR.FromPointer(0x3300);
		var replacements = new[]
		{
			"D=8px",
			"WEIGHT=-2",
			"COL=0,COL=0",
			"MIW=10junk",
			"O=SIDEWAYS",
			"BAR=1",
			"UNKNOWN=1",
			"P="
		};
		for (var i = 0; i < replacements.Length; i++)
		{
			platform.WriteCString(malformed, replacements[i]);
			Assert.False(MuiListCore.SetAttribute(ref platform, State, list,
				FormatAttr, malformed.Raw, false));
			Assert.Equal(format.Raw, Get(ref platform, list, FormatAttr));
		}
	}

	[Fact]
	public void FormatColReordersDisplayedStringArrayColumnsWithoutChangingHookData()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var format = APTR.FromPointer(0x3200);
		platform.WriteCString(format, "COL=2,COL=1,COL=0");
		var tags = APTR.FromPointer(0x3300);
		platform.WriteUInt32(tags, 0, ConstructHookAttr);
		platform.WriteUInt32(tags, 4, HookStringArray);
		platform.WriteUInt32(tags, 8, DisplayHookAttr);
		platform.WriteUInt32(tags, 12, HookStringArray);
		platform.WriteUInt32(tags, 16, FormatAttr);
		platform.WriteUInt32(tags, 20, format.Raw);
		platform.WriteUInt32(tags, 24, MaxColumnsAttr);
		platform.WriteUInt32(tags, 28, 3);
		platform.WriteUInt32(tags, 32, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		var zero = APTR.FromPointer(0x3400);
		var one = APTR.FromPointer(0x3440);
		var two = APTR.FromPointer(0x3480);
		platform.WriteCString(zero, "zero");
		platform.WriteCString(one, "one");
		platform.WriteCString(two, "two");
		var source = APTR.FromPointer(0x34C0);
		platform.WriteUInt32(source, 0, zero.Raw);
		platform.WriteUInt32(source, 4, one.Raw);
		platform.WriteUInt32(source, 8, two.Raw);
		platform.WriteUInt32(source, 12, 0);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, source,
			InsertBottom));
		var stored = MuiListCore.GetEntry(ref platform, State, list, 0,
			APTR.Null);
		var storedZero = APTR.FromPointer(platform.ReadUInt32(stored, 0));
		var storedOne = APTR.FromPointer(platform.ReadUInt32(stored, 4));
		var storedTwo = APTR.FromPointer(platform.ReadUInt32(stored, 8));

		var display = APTR.FromPointer(0x3500);
		Assert.True(MuiListCore.Display(ref platform, State, list, source,
			display, 0));
		Assert.Equal(zero.Raw, platform.ReadUInt32(display, 0));
		Assert.Equal(one.Raw, platform.ReadUInt32(display, 4));
		Assert.Equal(two.Raw, platform.ReadUInt32(display, 8));

		var renderInfo = APTR.FromPointer(0x3800);
		platform.WriteUInt32(renderInfo, 20, 0x3900);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, list,
			renderInfo));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 96, 8));
		platform.TextCount = 0;
		Assert.True(MuiListCore.Draw(ref platform, State, list, 0));
		Assert.Equal(3u, platform.TextCount);
		Assert.Equal(storedTwo, platform.FirstText);
		Assert.Equal(storedOne, platform.SecondText);
		Assert.Equal(storedZero, platform.ThirdText);
	}

	[Fact]
	public void ColumnOrderReordersDescriptorsAndCopiesGuestBytePermutation()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var format = APTR.FromPointer(0x3580);
		platform.WriteCString(format, ",,");
		var tags = APTR.FromPointer(0x35C0);
		platform.WriteUInt32(tags, 0, ConstructHookAttr);
		platform.WriteUInt32(tags, 4, HookStringArray);
		platform.WriteUInt32(tags, 8, DisplayHookAttr);
		platform.WriteUInt32(tags, 12, HookStringArray);
		platform.WriteUInt32(tags, 16, FormatAttr);
		platform.WriteUInt32(tags, 20, format.Raw);
		platform.WriteUInt32(tags, 24, MaxColumnsAttr);
		platform.WriteUInt32(tags, 28, 3);
		platform.WriteUInt32(tags, 32, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);

		var zero = APTR.FromPointer(0x3600);
		var one = APTR.FromPointer(0x3640);
		var two = APTR.FromPointer(0x3680);
		platform.WriteCString(zero, "zero");
		platform.WriteCString(one, "one");
		platform.WriteCString(two, "two");
		var source = APTR.FromPointer(0x36C0);
		platform.WriteUInt32(source, 0, zero.Raw);
		platform.WriteUInt32(source, 4, one.Raw);
		platform.WriteUInt32(source, 8, two.Raw);
		platform.WriteUInt32(source, 12, 0);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, source,
			InsertBottom));

		var storedEntry = MuiListCore.GetEntry(ref platform, State, list, 0,
			APTR.Null);
		var storedZero = APTR.FromPointer(platform.ReadUInt32(storedEntry, 0));
		var storedOne = APTR.FromPointer(platform.ReadUInt32(storedEntry, 4));
		var storedTwo = APTR.FromPointer(platform.ReadUInt32(storedEntry, 8));

		var order = APTR.FromPointer(0x3700);
		platform.WriteUInt8(order, 0, 2);
		platform.WriteUInt8(order, 1, 0);
		platform.WriteUInt8(order, 2, 1);
		platform.WriteUInt8(order, 3, 0xFF);
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			0x9d5100f6u, order.Raw)); // MUIA_List_ColumnOrder
		var stored = APTR.FromPointer(Get(ref platform, list, 0x9d5100f6u));
		Assert.NotEqual(order, stored);
		Assert.Equal((byte)2, platform.ReadUInt8(stored, 0));
		Assert.Equal((byte)0, platform.ReadUInt8(stored, 1));
		Assert.Equal((byte)1, platform.ReadUInt8(stored, 2));
		Assert.Equal(2u, MuiListCore.GetFormatDisplaySourceColumn(ref platform,
			State, list, 0));
		Assert.Equal(0u, MuiListCore.GetFormatDisplaySourceColumn(ref platform,
			State, list, 1));
		Assert.Equal(1u, MuiListCore.GetFormatDisplaySourceColumn(ref platform,
			State, list, 2));

		var renderInfo = APTR.FromPointer(0x3800);
		platform.WriteUInt32(renderInfo, 20, 0x3900);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, list,
			renderInfo));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 96, 8));
		platform.TextCount = 0;
		Assert.True(MuiListCore.Draw(ref platform, State, list, 0));
		Assert.Equal(storedTwo, platform.FirstText);
		Assert.Equal(storedZero, platform.SecondText);
		Assert.Equal(storedOne, platform.ThirdText);

		var malformed = APTR.FromPointer(0x3740);
		platform.WriteUInt8(malformed, 0, 2);
		platform.WriteUInt8(malformed, 1, 2);
		platform.WriteUInt8(malformed, 2, 0);
		platform.WriteUInt8(malformed, 3, 0xFF);
		Assert.False(MuiListCore.SetAttribute(ref platform, State, list,
			0x9d5100f6u, malformed.Raw));
		Assert.Equal(stored.Raw, Get(ref platform, list, 0x9d5100f6u));
	}

[Fact]
	public void ListGeometrySupportsMoreThanSixtyFourFormatColumns()
	{
		var platform = CreatePlatform(out var listClass, out var otherClass, 0x40000);
		var format = APTR.FromPointer(0x3980);
		platform.WriteCString(format, new string(',', 64));
		var tags = APTR.FromPointer(0x3A00);
		platform.WriteUInt32(tags, 0, FormatAttr);
		platform.WriteUInt32(tags, 4, format.Raw);
		platform.WriteUInt32(tags, 8, MaxColumnsAttr);
		platform.WriteUInt32(tags, 12, 256);
		platform.WriteUInt32(tags, 16, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.Equal(65u, MuiListCore.FormatColumnCount(ref platform, State,
			list));

		var storage = APTR.FromPointer(0x3B00);
		Assert.True(MuiListCore.GetColumnGeometry(ref platform, State, list,
			520, storage));
		var last = APTR.FromPointer(0x3B00 + 64 * 8);
		Assert.True(platform.IsMapped(last, 8));

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.True(platform.AllocationCount == platform.FreeCount,
			$"allocations={platform.AllocationCount} frees={platform.FreeCount}");
	}

	[Fact]
	public void FormatBarDrawsVerticalSeparatorBetweenColumns()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var format = APTR.FromPointer(0x3600);
		platform.WriteCString(format, "BAR,");
		var tags = APTR.FromPointer(0x3700);
		platform.WriteUInt32(tags, 0, ConstructHookAttr);
		platform.WriteUInt32(tags, 4, HookStringArray);
		platform.WriteUInt32(tags, 8, DisplayHookAttr);
		platform.WriteUInt32(tags, 12, HookStringArray);
		platform.WriteUInt32(tags, 16, FormatAttr);
		platform.WriteUInt32(tags, 20, format.Raw);
		platform.WriteUInt32(tags, 24, MaxColumnsAttr);
		platform.WriteUInt32(tags, 28, 2);
		platform.WriteUInt32(tags, 32, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		var first = APTR.FromPointer(0x3800);
		var second = APTR.FromPointer(0x3840);
		var source = APTR.FromPointer(0x3880);
		platform.WriteCString(first, "left");
		platform.WriteCString(second, "right");
		platform.WriteUInt32(source, 0, first.Raw);
		platform.WriteUInt32(source, 4, second.Raw);
		platform.WriteUInt32(source, 8, 0);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, source,
			InsertBottom));
		var renderInfo = APTR.FromPointer(0x3900);
		platform.WriteUInt32(renderInfo, 20, 0x3940);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, list,
			renderInfo));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 8));
		platform.LineCount = 0;
		platform.TextCount = 0;
		Assert.True(MuiListCore.Draw(ref platform, State, list, 0));
		Assert.Equal(1u, platform.LineCount);
		Assert.Equal(38, platform.LastLineX1);
		Assert.Equal(38, platform.LastLineX2);
		Assert.Equal(0, platform.LastLineY1);
		Assert.Equal(7, platform.LastLineY2);
		Assert.Equal(2u, platform.TextCount);
	}

	[Fact]
	public void FormatPreparseAlignsDisplayedColumnText()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var format = APTR.FromPointer(0x3A00);
		platform.WriteCString(format, "P=\\33c,P=\\33r");
		var tags = APTR.FromPointer(0x3B00);
		platform.WriteUInt32(tags, 0, ConstructHookAttr);
		platform.WriteUInt32(tags, 4, HookStringArray);
		platform.WriteUInt32(tags, 8, DisplayHookAttr);
		platform.WriteUInt32(tags, 12, HookStringArray);
		platform.WriteUInt32(tags, 16, FormatAttr);
		platform.WriteUInt32(tags, 20, format.Raw);
		platform.WriteUInt32(tags, 24, MaxColumnsAttr);
		platform.WriteUInt32(tags, 28, 2);
		platform.WriteUInt32(tags, 32, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		var first = APTR.FromPointer(0x3C00);
		var second = APTR.FromPointer(0x3C40);
		var source = APTR.FromPointer(0x3C80);
		platform.WriteCString(first, "a");
		platform.WriteCString(second, "bb");
		platform.WriteUInt32(source, 0, first.Raw);
		platform.WriteUInt32(source, 4, second.Raw);
		platform.WriteUInt32(source, 8, 0);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, source,
			InsertBottom));
		var renderInfo = APTR.FromPointer(0x3D00);
		platform.WriteUInt32(renderInfo, 20, 0x3D40);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, list,
			renderInfo));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 8));
		platform.TextCount = 0;
		Assert.True(MuiListCore.Draw(ref platform, State, list, 0));
		Assert.Equal(2u, platform.TextCount);
		Assert.Equal(15, platform.FirstTextLeft);
		Assert.Equal(64, platform.SecondTextLeft);
	}

	[Fact]
	public void FormatMinusOneWidthsUseWidestDisplayedEntryPerColumn()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var format = APTR.FromPointer(0x3E00);
		platform.WriteCString(format, "MINWIDTH=-1,MAXWIDTH=-1");
		var tags = APTR.FromPointer(0x3F00);
		platform.WriteUInt32(tags, 0, ConstructHookAttr);
		platform.WriteUInt32(tags, 4, HookStringArray);
		platform.WriteUInt32(tags, 8, DisplayHookAttr);
		platform.WriteUInt32(tags, 12, HookStringArray);
		platform.WriteUInt32(tags, 16, FormatAttr);
		platform.WriteUInt32(tags, 20, format.Raw);
		platform.WriteUInt32(tags, 24, MaxColumnsAttr);
		platform.WriteUInt32(tags, 28, 2);
		platform.WriteUInt32(tags, 32, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);

		var shortFirst = APTR.FromPointer(0x4000);
		var wideFirst = APTR.FromPointer(0x4040);
		var shortSecond = APTR.FromPointer(0x4080);
		var narrowSecond = APTR.FromPointer(0x40C0);
		platform.WriteCString(shortFirst, "x");
		platform.WriteCString(wideFirst, "long");
		platform.WriteCString(shortSecond, "bb");
		platform.WriteCString(narrowSecond, "c");
		var firstRow = APTR.FromPointer(0x4100);
		var secondRow = APTR.FromPointer(0x4120);
		platform.WriteUInt32(firstRow, 0, shortFirst.Raw);
		platform.WriteUInt32(firstRow, 4, shortSecond.Raw);
		platform.WriteUInt32(firstRow, 8, 0);
		platform.WriteUInt32(secondRow, 0, wideFirst.Raw);
		platform.WriteUInt32(secondRow, 4, narrowSecond.Raw);
		platform.WriteUInt32(secondRow, 8, 0);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			firstRow, InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			secondRow, InsertBottom));

		var descriptor = APTR.FromPointer(0x4180);
		Assert.True(MuiListCore.GetFormatColumn(ref platform, State, list, 0,
			descriptor));
		Assert.Equal(0x20u, platform.ReadUInt32(descriptor, 20));
		Assert.True(MuiListCore.GetFormatColumn(ref platform, State, list, 1,
			descriptor));
		Assert.Equal(0x40u, platform.ReadUInt32(descriptor, 20));

		var renderInfo = APTR.FromPointer(0x4200);
		platform.WriteUInt32(renderInfo, 20, 0x4240);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, list,
			renderInfo));
		var geometry = APTR.FromPointer(0x4280);
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 8));
		Assert.True(MuiListCore.GetColumnGeometry(ref platform, State, list, 80,
			geometry));
		Assert.Equal(38u, platform.ReadUInt32(geometry, 4));
		Assert.Equal(16u, platform.ReadUInt32(geometry, 12));

		// At a narrower width the explicit minimum still pins the first column
		// to the widest displayed entry instead of behaving like an omitted min.
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 40, 8));
		Assert.True(MuiListCore.GetColumnGeometry(ref platform, State, list, 40,
			geometry));
		Assert.Equal(32u, platform.ReadUInt32(geometry, 4));
	}

	[Fact]
	public void FormatWeightMinusOneUsesWidestDisplayedEntryAsFixedColumn()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var format = APTR.FromPointer(0x4300);
		platform.WriteCString(format, "WEIGHT=-1,WEIGHT=1");
		var tags = APTR.FromPointer(0x4340);
		platform.WriteUInt32(tags, 0, ConstructHookAttr);
		platform.WriteUInt32(tags, 4, HookStringArray);
		platform.WriteUInt32(tags, 8, DisplayHookAttr);
		platform.WriteUInt32(tags, 12, HookStringArray);
		platform.WriteUInt32(tags, 16, FormatAttr);
		platform.WriteUInt32(tags, 20, format.Raw);
		platform.WriteUInt32(tags, 24, MaxColumnsAttr);
		platform.WriteUInt32(tags, 28, 2);
		platform.WriteUInt32(tags, 32, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);

		var first = APTR.FromPointer(0x4380);
		var second = APTR.FromPointer(0x43C0);
		platform.WriteCString(first, "a");
		platform.WriteCString(second, "long");
		var firstRow = APTR.FromPointer(0x4400);
		var secondRow = APTR.FromPointer(0x4420);
		platform.WriteUInt32(firstRow, 0, first.Raw);
		platform.WriteUInt32(firstRow, 4, second.Raw);
		platform.WriteUInt32(firstRow, 8, 0);
		platform.WriteUInt32(secondRow, 0, second.Raw);
		platform.WriteUInt32(secondRow, 4, first.Raw);
		platform.WriteUInt32(secondRow, 8, 0);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			firstRow, InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			secondRow, InsertBottom));

		var descriptor = APTR.FromPointer(0x4440);
		Assert.True(MuiListCore.GetFormatColumn(ref platform, State, list, 0,
			descriptor));
		Assert.Equal(uint.MaxValue, platform.ReadUInt32(descriptor, 4));
		Assert.Equal(128u, platform.ReadUInt32(descriptor, 20));

		var renderInfo = APTR.FromPointer(0x4480);
		platform.WriteUInt32(renderInfo, 20, 0x44C0);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, list,
			renderInfo));
		var geometry = APTR.FromPointer(0x4500);
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 8));
		Assert.True(MuiListCore.GetColumnGeometry(ref platform, State, list, 80,
			geometry));
		Assert.Equal(32u, platform.ReadUInt32(geometry, 4));
		Assert.Equal(44u, platform.ReadUInt32(geometry, 12));
	}

	[Fact]
	public void FormatDescriptorsDeriveBoundedWeightedColumnGeometry()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var format = APTR.FromPointer(0x2B00);
		platform.WriteCString(format,
			"MINWIDTH=16px WEIGHT=1,MINWIDTH=8px MAXWIDTH=24px WEIGHT=3");
		var tags = APTR.FromPointer(0x2C00);
		platform.WriteUInt32(tags, 0, FormatAttr);
		platform.WriteUInt32(tags, 4, format.Raw);
		platform.WriteUInt32(tags, 8, MaxColumnsAttr);
		platform.WriteUInt32(tags, 12, 2);
		platform.WriteUInt32(tags, 16, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		var geometry = APTR.FromPointer(0x2D00);
		Assert.True(MuiListCore.GetColumnGeometry(ref platform, State, list, 64,
			geometry));
		Assert.Equal(0u, platform.ReadUInt32(geometry, 0));
		Assert.Equal(16u, platform.ReadUInt32(geometry, 4));
		// The default four-pixel delta follows the first column.
		Assert.Equal(20u, platform.ReadUInt32(geometry, 8));
		Assert.Equal(24u, platform.ReadUInt32(geometry, 12));
	}

	[Fact]
	public void FormatMinimumWidthHidesMiddleColumnAndRedistributesSpace()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var format = APTR.FromPointer(0x2B80);
		platform.WriteCString(format,
			"MINWIDTH=60px,MINWIDTH=60px,MINWIDTH=10px");
		var tags = APTR.FromPointer(0x2C80);
		platform.WriteUInt32(tags, 0, FormatAttr);
		platform.WriteUInt32(tags, 4, format.Raw);
		platform.WriteUInt32(tags, 8, MaxColumnsAttr);
		platform.WriteUInt32(tags, 12, 3);
		platform.WriteUInt32(tags, 16, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);

		var geometry = APTR.FromPointer(0x2D80);
		Assert.True(MuiListCore.GetColumnGeometry(ref platform, State, list,
			100, geometry));
		Assert.Equal(0u, platform.ReadUInt32(geometry, 0));
		Assert.Equal(60u, platform.ReadUInt32(geometry, 4));
		Assert.Equal(64u, platform.ReadUInt32(geometry, 8));
		Assert.Equal(0u, platform.ReadUInt32(geometry, 12));
		Assert.Equal(64u, platform.ReadUInt32(geometry, 16));
		Assert.Equal(36u, platform.ReadUInt32(geometry, 20));
		var clipped = APTR.FromPointer(0x2E80);
		Assert.True(MuiListCore.GetColumnGeometry(ref platform, State, list, 50,
			clipped));
		Assert.Equal(0u, platform.ReadUInt32(clipped, 0));
		Assert.Equal(50u, platform.ReadUInt32(clipped, 4));
		Assert.Equal(50u, platform.ReadUInt32(clipped, 8));
		Assert.Equal(0u, platform.ReadUInt32(clipped, 12));
		Assert.Equal(50u, platform.ReadUInt32(clipped, 16));
		Assert.Equal(0u, platform.ReadUInt32(clipped, 20));
	}

	[Fact]
	public void HideAndShowColumnAttributesUseGuestVisibilityMask()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var format = APTR.FromPointer(0x3000);
		platform.WriteCString(format,
			"DELTA=0 WEIGHT=1,DELTA=0 WEIGHT=1,DELTA=0 WEIGHT=1");
		var tags = APTR.FromPointer(0x3100);
		platform.WriteUInt32(tags, 0, FormatAttr);
		platform.WriteUInt32(tags, 4, format.Raw);
		platform.WriteUInt32(tags, 8, MaxColumnsAttr);
		platform.WriteUInt32(tags, 12, 3);
		platform.WriteUInt32(tags, 16, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);

		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			0x80428052u, 1)); // MUIA_List_HideColumn
		var geometry = APTR.FromPointer(0x3200);
		Assert.True(MuiListCore.GetColumnGeometry(ref platform, State, list,
			100, geometry));
		Assert.Equal(0u, platform.ReadUInt32(geometry, 0));
		Assert.Equal(50u, platform.ReadUInt32(geometry, 4));
		Assert.Equal(50u, platform.ReadUInt32(geometry, 8));
		Assert.Equal(0u, platform.ReadUInt32(geometry, 12));
		Assert.Equal(50u, platform.ReadUInt32(geometry, 16));
		Assert.Equal(50u, platform.ReadUInt32(geometry, 20));

		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			0x8042c840u, 1)); // MUIA_List_ShowColumn
		Assert.True(MuiListCore.GetColumnGeometry(ref platform, State, list,
			100, geometry));
		Assert.Equal(0u, platform.ReadUInt32(geometry, 0));
		Assert.Equal(33u, platform.ReadUInt32(geometry, 4));
		Assert.Equal(33u, platform.ReadUInt32(geometry, 8));
		Assert.Equal(33u, platform.ReadUInt32(geometry, 12));
		Assert.Equal(66u, platform.ReadUInt32(geometry, 16));
		Assert.Equal(34u, platform.ReadUInt32(geometry, 20));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State, list));
	}

	[Fact]
	public void PercentageFormatWidthsUseListPixelsForBoundedGeometry()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var format = APTR.FromPointer(0x2D80);
		platform.WriteCString(format,
			"DELTA=0 MINWIDTH=25 MAXWIDTH=25 WEIGHT=1,DELTA=0 MINWIDTH=25 MAXWIDTH=75 WEIGHT=1");
		var tags = APTR.FromPointer(0x2E80);
		platform.WriteUInt32(tags, 0, FormatAttr);
		platform.WriteUInt32(tags, 4, format.Raw);
		platform.WriteUInt32(tags, 8, MaxColumnsAttr);
		platform.WriteUInt32(tags, 12, 2);
		platform.WriteUInt32(tags, 16, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		var geometry = APTR.FromPointer(0x2F80);
		Assert.True(MuiListCore.GetColumnGeometry(ref platform, State, list, 100,
			geometry));
		Assert.Equal(0u, platform.ReadUInt32(geometry, 0));
		Assert.Equal(25u, platform.ReadUInt32(geometry, 4));
		Assert.Equal(25u, platform.ReadUInt32(geometry, 8));
		Assert.Equal(75u, platform.ReadUInt32(geometry, 12));
	}

	[Fact]
	public void FormatGeometryPositionsStringArrayColumnsWithinCells()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var format = APTR.FromPointer(0x2E00);
		platform.WriteCString(format,
			"MINWIDTH=16px WEIGHT=1,MINWIDTH=8px MAXWIDTH=24px WEIGHT=3");
		var tags = APTR.FromPointer(0x2F00);
		platform.WriteUInt32(tags, 0, ConstructHookAttr);
		platform.WriteUInt32(tags, 4, 0xFFFFFFFEu); // StringArray
		platform.WriteUInt32(tags, 8, 0x8042b4d5u); // DisplayHook
		platform.WriteUInt32(tags, 12, 0xFFFFFFFEu);
		platform.WriteUInt32(tags, 16, FormatAttr);
		platform.WriteUInt32(tags, 20, format.Raw);
		platform.WriteUInt32(tags, 24, MaxColumnsAttr);
		platform.WriteUInt32(tags, 28, 2);
		platform.WriteUInt32(tags, 32, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		var first = APTR.FromPointer(0x3000);
		var second = APTR.FromPointer(0x3040);
		var source = APTR.FromPointer(0x3080);
		platform.WriteCString(first, "alpha");
		platform.WriteCString(second, "bravo");
		platform.WriteUInt32(source, 0, first.Raw);
		platform.WriteUInt32(source, 4, second.Raw);
		platform.WriteUInt32(source, 8, 0);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, source,
			InsertBottom));
		var renderInfo = APTR.FromPointer(0x3800);
		platform.WriteUInt32(renderInfo, 20, 0x3900);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, list,
			renderInfo));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 64, 16));
		platform.TextCount = 0;
		Assert.True(MuiListCore.Draw(ref platform, State, list, 0));
		Assert.Equal(2u, platform.TextCount);
		Assert.Equal(20, platform.LastTextLeft);
	}

	[Fact]
	public void ThousandsOfEntriesGrowBoundedWithConstantTimeLookup()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x400000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		const uint total = 5000;
		for (var i = 0u; i < total; i++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
				APTR.FromPointer(0x100000 + i), InsertBottom));
		Assert.Equal(total, MuiListCore.EntryCount(ref platform, State, list));
		Assert.Equal(total, Get(ref platform, list, EntriesAttr));
		// O(1) indexed retrieval across the whole span.
		Assert.Equal(APTR.FromPointer(0x100000), MuiListCore.GetEntry(
			ref platform, State, list, 0, APTR.Null));
		Assert.Equal(APTR.FromPointer(0x100000 + 2500), MuiListCore.GetEntry(
			ref platform, State, list, 2500, APTR.Null));
		Assert.Equal(APTR.FromPointer(0x100000 + total - 1), MuiListCore.GetEntry(
			ref platform, State, list, (int)total - 1, APTR.Null));
		Assert.True(MuiListCore.Clear(ref platform, State, list));
		Assert.Equal(0u, MuiListCore.EntryCount(ref platform, State, list));
	}

	[Fact]
	public void TitleStateStoresAndPublishesNeutralTitleRowThroughDisplayHook()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		var title = APTR.FromPointer(0x3080);
		platform.WriteCString(title, "Name");
		// MUIA_List_Title is [ISG]: the Set path stores it and Get round-trips.
		var packet = APTR.FromPointer(0x3100);
		platform.WriteUInt32(packet, 0, 0x8042549Au); // MUIM_Set
		platform.WriteUInt32(packet, 4, TitleAttr);
		platform.WriteUInt32(packet, 8, title.Raw);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, list,
			packet));
		Assert.Equal(title.Raw, Get(ref platform, list, TitleAttr));

		var renderInfo = APTR.FromPointer(0x3800);
		platform.WriteUInt32(renderInfo, 20, 0x3900);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, list, renderInfo));
		// 24px / 8px rows == 3 lines; the title consumes one so the empty list
		// still publishes exactly the neutral title row through the display hook.
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 24));
		platform.TextCount = 0;
		Assert.True(MuiListCore.Draw(ref platform, State, list, 0));
		Assert.Equal(1u, platform.TextCount);
		Assert.Equal(title.Raw, platform.LastText.Raw);
		Assert.Equal(4, platform.LastTextLength);         // "Name"
		Assert.Equal(8, platform.LastTextBaseline);       // top + one row

		// With data present the title reserves one visible line above the rows.
		var alpha = APTR.FromPointer(0x3200);
		var bravo = APTR.FromPointer(0x3240);
		platform.WriteCString(alpha, "alpha");
		platform.WriteCString(bravo, "bravo");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, alpha,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, bravo,
			InsertBottom));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 24));
		Assert.Equal(2u, Get(ref platform, list, VisibleAttr)); // 3 rows - title
		platform.TextCount = 0;
		Assert.True(MuiListCore.Draw(ref platform, State, list, 0));
		Assert.Equal(3u, platform.TextCount);             // title + 2 rows
		Assert.Equal(bravo.Raw, platform.LastText.Raw);   // last data row
	}

	[Fact]
	public void TitleValueUsesNamedRecordForPointerAndBooleanForms()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.True(MuiListCore.TryGetTitleState(ref platform, State, list,
			out var title));
		Assert.Equal(0u, title.Value);

		var pointer = APTR.FromPointer(0x34C0);
		platform.WriteCString(pointer, "Title");
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			TitleAttr, pointer.Raw));
		Assert.True(MuiListCore.TryGetTitleState(ref platform, State, list,
			out title));
		Assert.Equal(pointer.Raw, title.Value);
		Assert.Equal(pointer.Raw, Get(ref platform, list, TitleAttr));

		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			TitleAttr, 1));
		Assert.True(MuiListCore.TryGetTitleState(ref platform, State, list,
			out title));
		Assert.Equal(1u, title.Value);
		Assert.Equal(1u, Get(ref platform, list, TitleAttr));

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
	}

	[Fact]
	public void BooleanTitleCallsDisplayHookWithNullEntryOnEmptyList()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var hook = APTR.FromPointer(0x32C0);
		platform.WriteUInt32(hook, 8, MuiHeadlessTestPlatform.HookEntryDestruct);
		var tags = APTR.FromPointer(0x3300);
		platform.WriteUInt32(tags, 0, TitleAttr);
		platform.WriteUInt32(tags, 4, 1);
		platform.WriteUInt32(tags, 8, DisplayHookAttr);
		platform.WriteUInt32(tags, 12, hook.Raw);
		platform.WriteUInt32(tags, 16, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);

		var renderInfo = APTR.FromPointer(0x3340);
		platform.WriteUInt32(renderInfo, 20, 0x3380);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, list,
			renderInfo));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 16));
		platform.HookInvokeCount = 0;
		platform.HookDestructCount = 0;
		platform.LastHookA2 = APTR.FromPointer(0x1234);
		Assert.True(MuiListCore.Draw(ref platform, State, list, 0));
		Assert.Equal(1u, platform.HookInvokeCount);
		Assert.Equal(1u, platform.HookDestructCount);
		Assert.Equal(APTR.Null, platform.LastHookA2);
	}

	[Fact]
	public void DisplayHookReceivesNamedRowRecordBeforeColumnArray()
	{
		var platform = CreatePlatform(out var listClass, out var otherClass,
			0x40000);
		var hook = APTR.FromPointer(0x3A40);
		platform.WriteUInt32(hook, 8, MuiHeadlessTestPlatform.HookEntryDestruct);
		var tags = APTR.FromPointer(0x3A80);
		platform.WriteUInt32(tags, 0, DisplayHookAttr);
		platform.WriteUInt32(tags, 4, hook.Raw);
		platform.WriteUInt32(tags, 8, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);

		// The caller-visible array begins after the named row record, matching
		// the MorphOS display-hook ABI without exposing a magic offset to the
		// implementation under test.
		var rowStorage = APTR.FromPointer(0x3AC0);
		var displayArray = APTR.FromPointer(0x3AC4);
		platform.HookInvokeCount = 0;
		Assert.True(MuiListCore.Display(ref platform, State, list,
			APTR.FromPointer(0x3B40), displayArray, 7));
		Assert.Equal(1u, platform.HookInvokeCount);
		Assert.True(MuiListDisplayRowRecordCodec.TryRead(ref platform,
			rowStorage, out var row));
		Assert.Equal(7, row.Row);

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void TitleArrayCopiesPointerTableAndBypassesDisplayHook()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var firstTitle = APTR.FromPointer(0x3080);
		var secondTitle = APTR.FromPointer(0x30A0);
		var source = APTR.FromPointer(0x30C0);
		var format = APTR.FromPointer(0x30D0);
		platform.WriteCString(firstTitle, "Name");
		platform.WriteCString(secondTitle, "Population");
		platform.WriteCString(format, ",");
		platform.WriteUInt32(source, 0, firstTitle.Raw);
		platform.WriteUInt32(source, 4, secondTitle.Raw);
		platform.WriteUInt32(source, 8, 0);
		var displayHook = APTR.FromPointer(0x30E0);
		platform.WriteUInt32(displayHook, 8,
			MuiHeadlessTestPlatform.HookEntryDestruct);
		var tags = APTR.FromPointer(0x3100);
		platform.WriteUInt32(tags, 0, MaxColumnsAttr);
		platform.WriteUInt32(tags, 4, 2);
		platform.WriteUInt32(tags, 8, TitleArrayAttr);
		platform.WriteUInt32(tags, 12, source.Raw);
		platform.WriteUInt32(tags, 16, FormatAttr);
		platform.WriteUInt32(tags, 20, format.Raw);
		platform.WriteUInt32(tags, 24, 0x8042b4d5u); // MUIA_List_DisplayHook
		platform.WriteUInt32(tags, 28, displayHook.Raw);
		platform.WriteUInt32(tags, 32, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);

		var stored = APTR.FromPointer(Get(ref platform, list, TitleArrayAttr));
		Assert.NotEqual(source, stored);
		Assert.Equal(firstTitle.Raw, platform.ReadUInt32(stored, 0));
		Assert.Equal(secondTitle.Raw, platform.ReadUInt32(stored, 4));
		Assert.Equal(0u, platform.ReadUInt32(stored, 8));
		Assert.Equal(0u, Get(ref platform, list, TitleAttr));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			TitleArrayAttr, source.Raw));
		var refreshed = APTR.FromPointer(Get(ref platform, list, TitleArrayAttr));
		Assert.NotEqual(stored, refreshed);
		Assert.Equal(firstTitle.Raw, platform.ReadUInt32(refreshed, 0));
		Assert.Equal(secondTitle.Raw, platform.ReadUInt32(refreshed, 4));

		var renderInfo = APTR.FromPointer(0x3800);
		platform.WriteUInt32(renderInfo, 20, 0x3900);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, list, renderInfo));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 200, 24));
		Assert.Equal(2u, Get(ref platform, list, VisibleAttr));
		platform.TextCount = 0;
		platform.HookInvokeCount = 0;
		Assert.True(MuiListCore.Draw(ref platform, State, list, 0));
		Assert.Equal(2u, platform.TextCount);
		Assert.Equal(0u, platform.HookInvokeCount);
		Assert.Equal(secondTitle.Raw, platform.LastText.Raw);
		Assert.Equal(10, platform.LastTextLength);
		Assert.Equal(8, platform.LastTextBaseline);
	}

	[Fact]
	public void MinLineHeightConstructionTagControlsRowsHitTestingAndMinimums()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var tags = APTR.FromPointer(0x2E00);
		platform.WriteUInt32(tags, 0, MinLineHeightAttr);
		platform.WriteUInt32(tags, 4, 16);
		platform.WriteUInt32(tags, 8, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.Equal(16u, Get(ref platform, list, MinLineHeightAttr));
		Assert.False(MuiListCore.SetAttribute(ref platform, State, list,
			MinLineHeightAttr, 32));
		Assert.Equal(16u, Get(ref platform, list, MinLineHeightAttr));

		var first = APTR.FromPointer(0x3000);
		var second = APTR.FromPointer(0x3040);
		platform.WriteCString(first, "first");
		platform.WriteCString(second, "second");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, first,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, second,
			InsertBottom));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 32));
		Assert.Equal(2u, Get(ref platform, list, VisibleAttr));

		var hit = APTR.FromPointer(0x3100);
		Assert.True(MuiListCore.TestPos(ref platform, State, list, 4, 20, hit));
		Assert.Equal(1u, platform.ReadUInt32(hit, 0));

		var minMax = APTR.FromPointer(0x3140);
		Assert.True(MuiListCore.AskMinMax(ref platform, State, list, minMax));
		Assert.True(unchecked((short)platform.ReadUInt16(minMax, 2)) >= 16);
		Assert.True(unchecked((short)platform.ReadUInt16(minMax, 6)) >= 16);
	}

	[Fact]
	public void MinLineHeightBaselineUsesNamedPresentationPolicy()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var tags = APTR.FromPointer(0x3180);
		platform.WriteUInt32(tags, 0, MinLineHeightAttr);
		platform.WriteUInt32(tags, 4, 16);
		platform.WriteUInt32(tags, 8, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.TryGetPresentationPolicy(ref platform, State,
			list, out var policy));
		Assert.Equal(16u, policy.MinLineHeight);

		// Simulate a stale public projection. The named policy remains the
		// authoritative baseline consumed by AskMinMax and line-height refresh.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, list,
			MinLineHeightAttr, 2, false));
		var minMax = APTR.FromPointer(0x3200);
		Assert.True(MuiListCore.AskMinMax(ref platform, State, list, minMax));
		Assert.True(unchecked((short)platform.ReadUInt16(minMax, 2)) >= 16);
		Assert.True(unchecked((short)platform.ReadUInt16(minMax, 6)) >= 16);

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
	}

	[Fact]
	public void AutoLineHeightUsesMultilineEntriesAndCanBeDisabled()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var tags = APTR.FromPointer(0x2E80);
		platform.WriteUInt32(tags, 0, MinLineHeightAttr);
		platform.WriteUInt32(tags, 4, 8);
		platform.WriteUInt32(tags, 8, AutoLineHeightAttr);
		platform.WriteUInt32(tags, 12, 1);
		platform.WriteUInt32(tags, 16, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);

		var multiline = APTR.FromPointer(0x3300);
		var singleline = APTR.FromPointer(0x3340);
		platform.WriteCString(multiline, "one\ntwo");
		platform.WriteCString(singleline, "three");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, multiline,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, singleline,
			InsertBottom));
		Assert.Equal(1u, Get(ref platform, list, AutoLineHeightAttr));
		Assert.Equal(16u, Get(ref platform, list, LineHeightAttr));

		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 32));
		Assert.Equal(2u, Get(ref platform, list, VisibleAttr));
		var hit = APTR.FromPointer(0x3380);
		Assert.True(MuiListCore.TestPos(ref platform, State, list, 4, 20, hit));
		Assert.Equal(1u, platform.ReadUInt32(hit, 0));

		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			AutoLineHeightAttr, 0));
		Assert.Equal(8u, Get(ref platform, list, LineHeightAttr));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 16));
		Assert.Equal(2u, Get(ref platform, list, VisibleAttr));
	}

	[Fact]
	public void ViewportStatePublishesNamedLineHeightAcrossAutoRefresh()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var tags = APTR.FromPointer(0x2F80);
		platform.WriteUInt32(tags, 0, MinLineHeightAttr);
		platform.WriteUInt32(tags, 4, 8);
		platform.WriteUInt32(tags, 8, AutoLineHeightAttr);
		platform.WriteUInt32(tags, 12, 1);
		platform.WriteUInt32(tags, 16, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);

		var multiline = APTR.FromPointer(0x4680);
		platform.WriteCString(multiline, "one\ntwo");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			multiline, InsertBottom));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 32));
		Assert.True(MuiListCore.TryGetViewportState(ref platform, State, list,
			out var viewport));
		Assert.Equal(16u, viewport.LineHeight);
		Assert.Equal(16u, Get(ref platform, list, LineHeightAttr));

		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			AutoLineHeightAttr, 0));
		Assert.True(MuiListCore.TryGetViewportState(ref platform, State, list,
			out viewport));
		Assert.Equal(8u, viewport.LineHeight);
		Assert.Equal(8u, Get(ref platform, list, LineHeightAttr));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
	}

	[Fact]
	public void ViewportStatePublishesNamedVisibleCapacityAndHiddenSentinel()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x4780), InsertBottom));

		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 16));
		Assert.True(MuiListCore.TryGetViewportState(ref platform, State, list,
			out var viewport));
		Assert.Equal(2u, viewport.Visible);
		Assert.Equal(2u, Get(ref platform, list, VisibleAttr));

		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 0));
		Assert.True(MuiListCore.TryGetViewportState(ref platform, State, list,
			out viewport));
		Assert.Equal(uint.MaxValue, viewport.Visible);
		Assert.Equal(uint.MaxValue, Get(ref platform, list, VisibleAttr));

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
	}

	[Fact]
	public void ViewportPixelAttributesTrackFirstVisibleAndTotalRows()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		for (var row = 0u; row < 3; row++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
				APTR.FromPointer(0x3400 + row * 0x20), InsertBottom));

		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 16));
		Assert.Equal(0u, Get(ref platform, list, TopPixelAttr));
		Assert.Equal(16u, Get(ref platform, list, VisiblePixelAttr));
		Assert.Equal(24u, Get(ref platform, list, TotalPixelAttr));

		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			FirstAttr, 1));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 16));
		Assert.Equal(8u, Get(ref platform, list, TopPixelAttr));
		Assert.Equal(16u, Get(ref platform, list, VisiblePixelAttr));
		Assert.Equal(24u, Get(ref platform, list, TotalPixelAttr));

		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			TitleAttr, 1));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 16));
		Assert.Equal(8u, Get(ref platform, list, TopPixelAttr));
		Assert.Equal(16u, Get(ref platform, list, VisiblePixelAttr));
		Assert.Equal(32u, Get(ref platform, list, TotalPixelAttr));

		var viewportRecord = APTR.FromPointer(0x3A00);
		Assert.True(MuiListCore.WriteViewportMetrics(ref platform,
			viewportRecord, uint.MaxValue, uint.MaxValue, uint.MaxValue,
			uint.MaxValue, 1));
		Assert.Equal(uint.MaxValue, platform.ReadUInt32(viewportRecord, 4));
		Assert.Equal(uint.MaxValue, platform.ReadUInt32(viewportRecord, 8));
		Assert.Equal(uint.MaxValue, platform.ReadUInt32(viewportRecord, 12));
		Assert.Equal(uint.MaxValue, platform.ReadUInt32(viewportRecord, 16));
		Assert.Equal(uint.MaxValue, platform.ReadUInt32(viewportRecord, 20));
		Assert.Equal(uint.MaxValue, platform.ReadUInt32(viewportRecord, 24));
		Assert.Equal(uint.MaxValue, platform.ReadUInt32(viewportRecord, 28));
	}

	[Fact]
	public void ViewportStateUsesNamedFirstCursorAndTracksNavigation()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		for (var row = 0u; row < 3; row++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
				APTR.FromPointer(0x4200 + row * 0x20), InsertBottom));

		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 16));
		Assert.True(MuiListCore.TryGetViewportState(ref platform, State, list,
			out var viewport));
		Assert.Equal(0u, viewport.First);
		Assert.Equal(8u, viewport.LineHeight);
		Assert.Equal(2u, viewport.Visible);
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			FirstAttr, 1));
		Assert.True(MuiListCore.TryGetViewportState(ref platform, State, list,
			out viewport));
		Assert.Equal(1u, viewport.First);
		Assert.Equal(8u, viewport.LineHeight);
		Assert.Equal(2u, viewport.Visible);
		Assert.Equal(8u, viewport.TopPixel);

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
	}

	[Fact]
	public void JumpRefreshesViewportPixelAttributesWithoutLayout()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		for (var row = 0u; row < 4; row++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
				APTR.FromPointer(0x3C00 + row * 0x20), InsertBottom));

		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 16));
		Assert.Equal(0u, Get(ref platform, list, TopPixelAttr));
		Assert.Equal(16u, Get(ref platform, list, VisiblePixelAttr));
		Assert.Equal(32u, Get(ref platform, list, TotalPixelAttr));

		Assert.True(MuiListCore.Jump(ref platform, State, list, 2));
		Assert.Equal(2u, Get(ref platform, list, FirstAttr));
		Assert.Equal(16u, Get(ref platform, list, TopPixelAttr));
		Assert.Equal(16u, Get(ref platform, list, VisiblePixelAttr));
		Assert.Equal(32u, Get(ref platform, list, TotalPixelAttr));
	}

	[Fact]
	public void DirectActiveAndFirstWritesRefreshViewportPixelAttributes()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		for (var row = 0u; row < 5; row++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
				APTR.FromPointer(0x3E00 + row * 0x20), InsertBottom));

		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 16));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			FirstAttr, 1));
		Assert.Equal(1u, Get(ref platform, list, FirstAttr));
		Assert.Equal(8u, Get(ref platform, list, TopPixelAttr));
		Assert.Equal(40u, Get(ref platform, list, TotalPixelAttr));

		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			ActiveAttr, 3));
		Assert.Equal(3u, Get(ref platform, list, ActiveAttr));
		Assert.Equal(2u, Get(ref platform, list, FirstAttr));
		Assert.Equal(16u, Get(ref platform, list, TopPixelAttr));
		Assert.Equal(16u, Get(ref platform, list, VisiblePixelAttr));
		Assert.Equal(40u, Get(ref platform, list, TotalPixelAttr));
	}

	[Fact]
	public void AdjustHeightPinsListMinDefaultAndMaximumToAllEntries()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var tags = APTR.FromPointer(0x2F00);
		platform.WriteUInt32(tags, 0, AdjustHeightAttr);
		platform.WriteUInt32(tags, 4, 1);
		platform.WriteUInt32(tags, 8, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.Equal(1u, Get(ref platform, list, AdjustHeightAttr));
		Assert.False(MuiListCore.SetAttribute(ref platform, State, list,
			AdjustHeightAttr, 0));
		Assert.Equal(1u, Get(ref platform, list, AdjustHeightAttr));
		for (var row = 0u; row < 3; row++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
				APTR.FromPointer(0x3400 + row * 0x20), InsertBottom));

		var minMax = APTR.FromPointer(0x3500);
		Assert.True(MuiListCore.AskMinMax(ref platform, State, list, minMax));
		Assert.Equal(24, unchecked((short)platform.ReadUInt16(minMax, 2)));
		Assert.Equal(24, unchecked((short)platform.ReadUInt16(minMax, 6)));
		Assert.Equal(24, unchecked((short)platform.ReadUInt16(minMax, 10)));
	}

	[Fact]
	public void AdjustWidthPinsListMinDefaultAndMaximumToWidestDisplayedEntry()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var tags = APTR.FromPointer(0x2F00);
		platform.WriteUInt32(tags, 0, AdjustWidthAttr);
		platform.WriteUInt32(tags, 4, 1);
		platform.WriteUInt32(tags, 8, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.Equal(1u, Get(ref platform, list, AdjustWidthAttr));
		Assert.False(MuiListCore.SetAttribute(ref platform, State, list,
			AdjustWidthAttr, 0));
		Assert.Equal(1u, Get(ref platform, list, AdjustWidthAttr));
		var shortText = APTR.FromPointer(0x3400);
		var widestText = APTR.FromPointer(0x3440);
		platform.WriteCString(shortText, "short");
		platform.WriteCString(widestText, "widest");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			shortText, InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			widestText, InsertBottom));

		var minMax = APTR.FromPointer(0x3500);
		Assert.True(MuiListCore.AskMinMax(ref platform, State, list, minMax));
		Assert.Equal(48, unchecked((short)platform.ReadUInt16(minMax, 0)));
		Assert.Equal(48, unchecked((short)platform.ReadUInt16(minMax, 4)));
		Assert.Equal(48, unchecked((short)platform.ReadUInt16(minMax, 8)));
	}

	[Fact]
	public void StripesDrawEverySecondDataRowThroughGraphicsSeam()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var tags = APTR.FromPointer(0x3600);
		platform.WriteUInt32(tags, 0, StripesAttr);
		platform.WriteUInt32(tags, 4, 1);
		platform.WriteUInt32(tags, 8, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.Equal(1u, Get(ref platform, list, StripesAttr));
		var first = APTR.FromPointer(0x3640);
		var second = APTR.FromPointer(0x3680);
		platform.WriteCString(first, "alpha");
		platform.WriteCString(second, "bravo");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, first,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, second,
			InsertBottom));
		var renderInfo = APTR.FromPointer(0x36C0);
		platform.WriteUInt32(renderInfo, 20, 0x3700);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, list,
			renderInfo));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 16));
		platform.FillCount = 0;
		platform.TextCount = 0;
		Assert.True(MuiListCore.Draw(ref platform, State, list, 0));
		Assert.Equal(2u, platform.FillCount); // background plus the odd row
		Assert.Equal(2u, platform.TextCount);
		Assert.Equal(2u, platform.LastPen);

		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			StripesAttr, 0));
		Assert.Equal(0u, Get(ref platform, list, StripesAttr));
	}

	[Fact]
	public void DropMarkPublishesBoundedInsertionLineAndHonoursVisibility()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var tags = APTR.FromPointer(0x3800);
		platform.WriteUInt32(tags, 0, ShowDropMarksAttr);
		platform.WriteUInt32(tags, 4, 1);
		platform.WriteUInt32(tags, 8, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.Equal(unchecked((uint)-1), Get(ref platform, list, DropMarkAttr));
		var first = APTR.FromPointer(0x3840);
		var second = APTR.FromPointer(0x3880);
		var third = APTR.FromPointer(0x38C0);
		platform.WriteCString(first, "alpha");
		platform.WriteCString(second, "bravo");
		platform.WriteCString(third, "charlie");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, first,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, second,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, third,
			InsertBottom));
		Assert.False(MuiListCore.SetAttribute(ref platform, State, list,
			DropMarkAttr, 1));
		Assert.True(MuiListCore.SetDropMark(ref platform, State, list, 1));
		Assert.Equal(1u, Get(ref platform, list, DropMarkAttr));
		Assert.True(MuiListCore.TryGetViewportState(ref platform, State, list,
			out var viewport));
		Assert.Equal(1u, viewport.DropMark);
		var renderInfo = APTR.FromPointer(0x3900);
		platform.WriteUInt32(renderInfo, 20, 0x3940);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, list,
			renderInfo));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 16));
		platform.LineCount = 0;
		Assert.True(MuiListCore.Draw(ref platform, State, list, 0));
		Assert.Equal(1u, platform.LineCount);
		Assert.Equal(8, platform.LastLineY1);
		Assert.Equal(0, platform.LastLineX1);
		Assert.Equal(79, platform.LastLineX2);

		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			ShowDropMarksAttr, 0));
		platform.LineCount = 0;
		Assert.True(MuiListCore.Draw(ref platform, State, list, 0));
		Assert.Equal(0u, platform.LineCount);
	}

	[Fact]
	public void ViewportStatePublishesNamedDropMarkAndClearsSentinel()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x48C0), InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x4900), InsertBottom));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 16));

		Assert.True(MuiListCore.SetDropMark(ref platform, State, list, 1));
		Assert.True(MuiListCore.TryGetViewportState(ref platform, State, list,
			out var viewport));
		Assert.Equal(1u, viewport.DropMark);
		Assert.Equal(1u, Get(ref platform, list, DropMarkAttr));

		Assert.True(MuiListCore.SetDropMark(ref platform, State, list, -1));
		Assert.True(MuiListCore.TryGetViewportState(ref platform, State, list,
			out viewport));
		Assert.Equal(uint.MaxValue, viewport.DropMark);
		Assert.Equal(uint.MaxValue, Get(ref platform, list, DropMarkAttr));

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			list));
	}

	[Fact]
	public void DragMoveReordersOnlyWhenMorphosDragSortingIsEnabled()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var tags = APTR.FromPointer(0x3A00);
		platform.WriteUInt32(tags, 0, DragSortableAttr);
		platform.WriteUInt32(tags, 4, 1);
		platform.WriteUInt32(tags, 8, DragTypeAttr);
		platform.WriteUInt32(tags, 12, 1);
		platform.WriteUInt32(tags, 16, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		Assert.Equal(1u, Get(ref platform, list, DragSortableAttr));
		Assert.Equal(1u, Get(ref platform, list, DragTypeAttr));
		var alpha = APTR.FromPointer(0x3A40);
		var bravo = APTR.FromPointer(0x3A80);
		var charlie = APTR.FromPointer(0x3AC0);
		platform.WriteCString(alpha, "alpha");
		platform.WriteCString(bravo, "bravo");
		platform.WriteCString(charlie, "charlie");
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, alpha,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, bravo,
			InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, charlie,
			InsertBottom));
		Assert.True(MuiListCore.DragMove(ref platform, State, list, 0, 2));
		Assert.Equal(bravo, MuiListCore.GetEntry(ref platform, State, list, 0,
			APTR.Null));
		Assert.Equal(charlie, MuiListCore.GetEntry(ref platform, State, list, 1,
			APTR.Null));
		Assert.Equal(alpha, MuiListCore.GetEntry(ref platform, State, list, 2,
			APTR.Null));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			DragTypeAttr, 99));
		Assert.Equal(0u, Get(ref platform, list, DragTypeAttr));
		Assert.False(MuiListCore.DragMove(ref platform, State, list, 2, 0));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			DragTypeAttr, 1));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			DragSortableAttr, 0));
		Assert.False(MuiListCore.DragMove(ref platform, State, list, 2, 0));
	}

	[Fact]
	public void AutoVisibleControlsDisplayTimeJumpToActiveEntry()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		for (var row = 0u; row < 8; row++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
				APTR.FromPointer(0x3B00 + row * 0x20), InsertBottom));
		Assert.Equal(0u, Get(ref platform, list, AutoVisibleAttr));
		// Seed an off-screen active row and a caller-selected first row through
		// the raw guest record, as a display-time layout would observe them.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, list,
			ActiveAttr, 7, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, list,
			FirstAttr, 0, false));
		var renderInfo = APTR.FromPointer(0x3B80);
		platform.WriteUInt32(renderInfo, 20, 0x3BC0);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, list,
			renderInfo));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 24));
		Assert.Equal(0u, Get(ref platform, list, FirstAttr));

		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			AutoVisibleAttr, 1));
		Assert.Equal(1u, Get(ref platform, list, AutoVisibleAttr));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, list,
			FirstAttr, 0, false));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 24));
		Assert.Equal(5u, Get(ref platform, list, FirstAttr));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, list,
			FirstAttr, unchecked((uint)-1), false));
		Assert.True(MuiListCore.Layout(ref platform, State, list, 0, 0, 80, 24));
		Assert.Equal(5u, Get(ref platform, list, FirstAttr));
	}

	[Fact]
	public void QuietCoalescesMutationRedrawUntilCleared()
	{
		var platform = CreatePlatform(out var listClass, out _, 0x40000);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x3C00), InsertBottom));
		var baseline = MuiListCore.RedrawRequests(ref platform, State, list);
		Assert.Equal(1u, baseline);

		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			QuietAttr, 1));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x3C20), InsertBottom));
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x3C40), InsertBottom));
		Assert.Equal(baseline, MuiListCore.RedrawRequests(ref platform, State,
			list));

		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			QuietAttr, 0));
		Assert.Equal(baseline + 1, MuiListCore.RedrawRequests(ref platform,
			State, list));
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			QuietAttr, 0));
		Assert.Equal(baseline + 1, MuiListCore.RedrawRequests(ref platform,
			State, list));
	}

	[Fact]
	public void ArbitraryConstructAndCompareHooksReceiveAmigaAbiAndReachHData()
	{
		var platform = CreatePlatform(out var listClass, out var otherClass,
			0x40000);
		// struct Hook: h_Entry at +8 (sentinel), h_Data at +16 (scratch). A0 must
		// deliver the hook base so the callback can reach its own h_Data.
		var constructHook = APTR.FromPointer(0x3000);
		var constructData = APTR.FromPointer(0x3040);
		platform.WriteUInt32(constructHook, 8,
			MuiHeadlessTestPlatform.HookEntryConstruct);
		platform.WriteUInt32(constructHook, 16, constructData.Raw);
		var pool = APTR.FromPointer(0x30C0);
		var tags = APTR.FromPointer(0x3100);
		platform.WriteUInt32(tags, 0, ConstructHookAttr);
		platform.WriteUInt32(tags, 4, constructHook.Raw);
		platform.WriteUInt32(tags, 8, PoolAttr);
		platform.WriteUInt32(tags, 12, pool.Raw);
		platform.WriteUInt32(tags, 16, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.NotEqual(APTR.Null, list);
		var entry = APTR.FromPointer(0x3200);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list, entry,
			InsertBottom));
		// Register ABI: A0 = hook base, A2 = pool, A1 = entry; h_Data via A0.
		Assert.Equal(constructHook, platform.LastHookBase);
		Assert.Equal(pool, platform.LastHookA2);
		Assert.Equal(entry, platform.LastHookA1);
		Assert.Equal(constructData, platform.LastHookData);
		// The callback wrote the three delivered registers through h_Data, which
		// it could only reach because A0 carried the hook base.
		var stored = MuiListCore.GetEntry(ref platform, State, list, 0, APTR.Null);
		Assert.Equal(constructData, stored);
		Assert.Equal(constructHook.Raw, platform.ReadUInt32(constructData, 0));
		Assert.Equal(pool.Raw, platform.ReadUInt32(constructData, 4));
		Assert.Equal(entry.Raw, platform.ReadUInt32(constructData, 8));

		// A compare hook gated on the h_Data cookie: a missing A0 would return 0.
		var compareHook = APTR.FromPointer(0x3300);
		var compareData = APTR.FromPointer(0x3340);
		platform.WriteUInt32(compareHook, 8,
			MuiHeadlessTestPlatform.HookEntryCompare);
		platform.WriteUInt32(compareHook, 16, compareData.Raw);
		platform.WriteUInt32(compareData, 0,
			MuiHeadlessTestPlatform.HookDataCookie);
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list,
			CompareHookAttr, compareHook.Raw));
		var e1 = APTR.FromPointer(0x3400);
		var e2 = APTR.FromPointer(0x3440);
		platform.WriteUInt8(e1, 0, (byte)'A');
		platform.WriteUInt8(e2, 0, (byte)'B');
		Assert.True(MuiListCore.Compare(ref platform, State, list, e1, e2, 0) < 0);
		Assert.True(MuiListCore.Compare(ref platform, State, list, e2, e1, 0) > 0);
		Assert.Equal(compareHook, platform.LastHookBase);
		Assert.Equal(compareData, platform.LastHookData);

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State, list));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			otherClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void InsertGrowthFailureIsAtomicAndBalancesOnDisposal()
	{
		// A small arena eventually starves an index capacity-growth allocation.
		// The failing insert must add nothing (no partial list) and disposal must
		// free every surviving allocation.
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x2000, 0x1600, State);
		var listName = APTR.FromPointer(0x1100);
		platform.WriteCString(listName, "List.mui");
		MuiHeadlessObjectCore.Initialize(ref platform, State);
		var listClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			listName, APTR.Null, 0, APTR.FromPointer(1), false);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		var entryPtr = APTR.FromPointer(0x900001);
		uint inserted = 0;
		var failed = false;
		for (var i = 0; i < 100000; i++)
		{
			if (!MuiListCore.InsertSingle(ref platform, State, list, entryPtr,
				InsertBottom)) { failed = true; break; }
			inserted++;
		}
		Assert.True(failed);
		Assert.True(inserted > 0);
		// The failed insert left the count exactly at the last success.
		Assert.Equal(inserted, MuiListCore.EntryCount(ref platform, State, list));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State, list));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void SourceArrayMaterializationFailureConstructsAtomically()
	{
		// A large MUIA_List_SourceArray cannot be materialized in a constrained
		// arena: an index growth fails mid-materialization. Construction must then
		// fail atomically (NULL, no half-built list) with balanced allocations.
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x3100, 0x3100, State);
		var listName = APTR.FromPointer(0x1100);
		platform.WriteCString(listName, "List.mui");
		MuiHeadlessObjectCore.Initialize(ref platform, State);
		var listClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			listName, APTR.Null, 0, APTR.FromPointer(1), false);
		const int count = 2000;
		var array = APTR.FromPointer(0x1180);
		for (var i = 0; i < count; i++)
			platform.WriteUInt32(array, i * 4, unchecked((uint)(0x900000 + i)));
		platform.WriteUInt32(array, count * 4, 0); // NULL terminator
		var tags = APTR.FromPointer(0x1140);
		platform.WriteUInt32(tags, 0, 0x8042c0a0u); // MUIA_List_SourceArray
		platform.WriteUInt32(tags, 4, array.Raw);
		platform.WriteUInt32(tags, 8, 0);
		var list = MuiListCore.CreateList(ref platform, State, listClass, tags);
		Assert.Equal(APTR.Null, list);
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj, attribute,
			out var value);
		return value;
	}

	private static string ReadCString(ref MuiHeadlessTestPlatform platform,
		APTR address)
	{
		var chars = new System.Text.StringBuilder();
		for (var i = 0; i < 256; i++)
		{
			var value = platform.ReadUInt8(address, i);
			if (value == 0) break;
			chars.Append((char)value);
		}
		return chars.ToString();
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR listClass,
		out APTR otherClass, int size)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, size, 0x5000, State);
		var listName = APTR.FromPointer(0x1100);
		var otherName = APTR.FromPointer(0x1140);
		platform.WriteCString(listName, "List.mui");
		platform.WriteCString(otherName, "Group.mui");
		MuiHeadlessObjectCore.Initialize(ref platform, State);
		listClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			listName, APTR.Null, 0, APTR.FromPointer(1), false);
		otherClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			otherName, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
