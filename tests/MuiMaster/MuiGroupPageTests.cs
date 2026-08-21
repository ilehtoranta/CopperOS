using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGroupPageTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint PageMode = 0x80421A5F;
	private const uint ActivePage = 0x80424199;
	private const uint LeftEdge = 0x8042BEC6;
	private const uint Width = 0x8042B59C;
	private const uint FixWidth = 0x8042A3F1;
	private const uint FixHeight = 0x8042A92B;

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
			Assert.Equal(index == 0 ? 80u : 0u, Get(ref platform, child, Width));
			Assert.Equal(8u, Get(ref platform, child, LeftEdge));
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
}
