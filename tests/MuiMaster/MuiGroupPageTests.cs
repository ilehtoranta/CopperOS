using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGroupPageTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint PageMode = 0x80421A5F;
	private const uint ActivePage = 0x80424199;
	private const uint LeftEdge = 0x8042BEC6;
	private const uint TopEdge = 0x8042509B;
	private const uint Width = 0x8042B59C;
	private const uint Height = 0x80423237;
	private const uint FixWidth = 0x8042A3F1;
	private const uint FixHeight = 0x8042A92B;
	private const uint ShowMe = 0x80429BA8;
	private const uint PageStateAttribute = 0x7FFE0041;

	[Fact]
	public void ActivePageSelectorsNormalizeAndDrivePageLayout()
	{
		var platform = CreatePageGroup(out var group, out var children);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			PageMode, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			ActivePage, unchecked((uint)MuiGroupPageCore.ActiveNext), false));
		Assert.Equal(1u, Get(ref platform, group, ActivePage));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			ActivePage, unchecked((uint)MuiGroupPageCore.ActivePrev), false));
		Assert.Equal(0u, Get(ref platform, group, ActivePage));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			ActivePage, unchecked((uint)MuiGroupPageCore.ActiveLast), false));
		Assert.Equal(3u, Get(ref platform, group, ActivePage));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			ActivePage, unchecked((uint)MuiGroupPageCore.ActiveAdvance), false));
		Assert.Equal(0u, Get(ref platform, group, ActivePage));
		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			ActivePage, 4, false));
		Assert.Equal(0u, Get(ref platform, group, ActivePage));

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 8, 9,
			80, 30));
		for (var index = 0; index < children.Length; index++)
		{
			var child = children[index];
			Assert.Equal(index == 0 ? 10u : 0u, Get(ref platform, child, Width));
			Assert.Equal(index == 0 ? 43u : 8u, Get(ref platform, child, LeftEdge));
		}
	}

	[Fact]
	public void ActivePageStateIsReleasedWithTheGroup()
	{
		var platform = CreatePageGroup(out var group, out _);
		var before = platform.AllocationCount;
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			ActivePage, 0, false));
		Assert.True(platform.AllocationCount > before);
		var freesBefore = platform.FreeCount;
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, group));
		Assert.True(platform.FreeCount > freesBefore);
	}

	[Fact]
	public void ActivePageGetterPrefersNamedStateAndOmGetUsesProjection()
	{
		var platform = CreatePageGroup(out var group, out _);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			ActivePage, unchecked((uint)MuiGroupPageCore.ActiveLast), false));
		Assert.Equal(3u, Get(ref platform, group, ActivePage));

		// A raw compatibility write cannot replace the normalized named page
		// state used by public Get and OM_GET.
		var record = MuiHeadlessObjectCore.FindObject(ref platform, State, group);
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			record, ActivePage, 1, false));
		Assert.Equal(3u, Get(ref platform, group, ActivePage));

		var message = APTR.FromPointer(0x7800);
		var storage = APTR.FromPointer(0x7900);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Attribute, ActivePage));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Storage, storage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			group, message));
		Assert.True(MuiGuestUlongStorageCodec.TryRead(ref platform, storage,
			out var stored));
		Assert.Equal(3u, stored.Value);
	}

	[Fact]
	public void HiddenActivePageKeepsSelectionButReceivesZeroAreaGeometry()
	{
		var platform = CreatePageGroup(out var group, out var children);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			PageMode, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			ActivePage, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			children[1], ShowMe, 0, false));

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 8, 9,
			80, 30));
		Assert.Equal(1u, Get(ref platform, group, ActivePage));
		Assert.Equal(0u, Get(ref platform, children[0], Width));
		Assert.Equal(0u, Get(ref platform, children[1], Width));
		Assert.Equal(0u, Get(ref platform, children[1], Height));
	}

	[Fact]
	public void MalformedNamedPageStateFailsClosedBeforeGetterAndLayout()
	{
		var platform = CreatePageGroup(out var group, out var children);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			PageMode, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			ActivePage, 1, false));
		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 8, 9,
			80, 30));
		var beforeWidth = Get(ref platform, children[1], Width);
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, group,
			PageStateAttribute, out var blockRaw));
		var block = APTR.FromPointer(blockRaw);
		Assert.True(MuiGroupPageStateFieldCursorCodec.TryWriteUInt32(ref platform,
			block, MuiGroupPageStateField.Active, uint.MaxValue));

		Assert.False(MuiGroupPageCore.TryGetAttribute(ref platform, State, group,
			ActivePage, out _));
		Assert.False(MuiGroupPageCore.TryReadActivePage(ref platform, State, group,
			4, out _));
		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			ActivePage, 0, false));
		Assert.False(MuiGroupLayoutCore.Layout(ref platform, State, group, 8, 9,
			80, 30));
		Assert.Equal(beforeWidth, Get(ref platform, children[1], Width));
		Assert.Equal(1u, GetRaw(ref platform, group, ActivePage));
	}

	[Fact]
	public void PageStateSequentialRecordRoundTripsAndRejectsTruncation()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var record = APTR.FromPointer(0x1200);
		Assert.True(MuiGroupPageCore.WritePageRecord(ref platform, record, 2, 7,
			unchecked((uint)MuiGroupPageCore.ActiveNext)));
		Assert.True(MuiGroupPageStateCodec.TryReadRecord(ref platform, record,
			out var value));
		Assert.Equal(MuiGroupPageState.Magic, value.Cookie);
		Assert.Equal(2u, value.Active);
		Assert.Equal(7u, value.Changes);
		Assert.Equal(MuiGroupPageCore.ActiveNext, unchecked((int)value.LastSelector));

		var truncated = APTR.FromPointer(0x20FFC);
		Assert.False(MuiGroupPageStateCodec.TryReadRecord(ref platform,
			truncated, out _));
		platform.WriteUInt32(record, 0, 0);
		Assert.False(MuiGroupPageStateCodec.TryRead(ref platform, record,
			out _));
	}

	[Fact]
	public void PageMinimumUsesLargestChildMinimumAndSmallestChildMaximum()
	{
		var platform = CreatePageGroup(out var group, out var children);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			PageMode, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			children[1], FixWidth, 20, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			ActivePage, 0, false));

		var minMax = APTR.FromPointer(0x1800);
		Assert.True(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			minMax));
		Assert.Equal((ushort)20, platform.ReadUInt16(minMax, 0));
		Assert.Equal((ushort)10, platform.ReadUInt16(minMax, 4));

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 8, 9,
			80, 30));
		Assert.Equal(10u, Get(ref platform, children[0], Width));
		Assert.Equal(43u, Get(ref platform, children[0], LeftEdge));
		Assert.Equal(10u, Get(ref platform, children[0], Height));
		Assert.Equal(19u, Get(ref platform, children[0], TopEdge));
	}

	private static MuiHeadlessTestPlatform CreatePageGroup(out APTR group,
		out APTR[] children)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var groupName = APTR.FromPointer(0x1100);
		var childName = APTR.FromPointer(0x1140);
		platform.WriteCString(groupName, "Group.mui");
		platform.WriteCString(childName, "Area.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var groupClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, groupName, APTR.Null, 0, APTR.FromPointer(1));
		var childClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, childName, APTR.Null, 0, APTR.FromPointer(1));
		group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		children = new APTR[4];
		for (var index = 0; index < children.Length; index++)
		{
			children[index] = MuiHeadlessObjectCore.CreateObjectA(ref platform,
				State, childClass, APTR.Null);
			Assert.True(children[index].IsNotNull);
			Assert.True(MuiFamilyCore.AddTail(ref platform, State, group,
				children[index]));
			Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
				children[index], FixWidth, 10, false));
			Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
				children[index], FixHeight, 10, false));
		}
		Assert.True(group.IsNotNull);
		return platform;
	}

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			attribute, out var value));
		return value;
	}

	private static uint GetRaw(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, obj,
			attribute, out var value));
		return value;
	}
}
