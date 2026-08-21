using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGroupLayoutPolicyTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint Horizontal = 0x8042536B;
	private const uint Spacing = 0x8042866D;
	private const uint SameWidth = 0x8042B3EC;
	private const uint PageMode = 0x80421A5F;

	[Fact]
	public void GroupLayoutPolicyUsesNamedFieldBoundaries()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			State);
		var address = APTR.FromPointer(0x3000);
		var cursor = new MuiGroupLayoutPolicyFieldCursor
		{
			Address = address,
			Field = MuiGroupLayoutPolicyField.PageMode,
		};
		Assert.True(MuiGroupLayoutPolicyFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var fieldAddress));
		Assert.Equal(APTR.FromPointer(0x3018), fieldAddress);
		Assert.True(MuiGroupLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGroupLayoutPolicyField.Horizontal, 1));
		Assert.True(MuiGroupLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiGroupLayoutPolicyField.Horizontal, out var horizontal));
		Assert.Equal(1u, horizontal);
		cursor.Field = unchecked((MuiGroupLayoutPolicyField)255);
		Assert.False(MuiGroupLayoutPolicyFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
		cursor.Address = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Field = MuiGroupLayoutPolicyField.PageMode;
		Assert.False(MuiGroupLayoutPolicyFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));

		var expected = new MuiGroupLayoutPolicyStateRecord
		{
			Magic = MuiGroupLayoutPolicyStateRecord.Cookie,
			Horizontal = 1,
			HorizontalSpacing = 4,
			VerticalSpacing = 6,
			SameWidth = 1,
			SameHeight = 0,
			PageMode = 1,
		};
		Assert.True(MuiGroupLayoutPolicyStateRecordCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiGroupLayoutPolicyStateRecordCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Horizontal, actual.Horizontal);
		Assert.Equal(expected.HorizontalSpacing, actual.HorizontalSpacing);
		Assert.Equal(expected.VerticalSpacing, actual.VerticalSpacing);
		Assert.Equal(expected.SameWidth, actual.SameWidth);
		Assert.Equal(expected.SameHeight, actual.SameHeight);
		Assert.Equal(expected.PageMode, actual.PageMode);
	}

	[Fact]
	public void GroupLayoutPublishesEffectivePolicyRecord()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Set(ref platform, group, Horizontal, 1);
		Set(ref platform, group, Spacing, 4);
		Set(ref platform, group, SameWidth, 1);

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 5, 7,
			104, 20));
		Assert.True(MuiGroupLayoutCore.TryGetLayoutState(ref platform, State, group,
			out var policy));
		Assert.Equal(MuiGroupLayoutPolicyStateRecord.Cookie, policy.Magic);
		Assert.Equal(1u, policy.Horizontal);
		Assert.Equal(4u, policy.HorizontalSpacing);
		Assert.Equal(4u, policy.VerticalSpacing);
		Assert.Equal(1u, policy.SameWidth);
		Assert.Equal(0u, policy.SameHeight);
		Assert.Equal(0u, policy.PageMode);
		Assert.Equal(50u, Get(ref platform, first, 0x8042B59C));
		Assert.Equal(50u, Get(ref platform, second, 0x8042B59C));
	}

	[Fact]
	public void GroupLayoutPolicyGettersPreferNamedRecordAndOmGetUsesProjection()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Set(ref platform, group, Horizontal, 1);
		Set(ref platform, group, Spacing, 4);
		Set(ref platform, group, PageMode, 1);
		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 5, 7,
			100, 60));
		Assert.True(MuiGroupLayoutCore.TryGetLayoutState(ref platform, State, group,
			out var policy));

		// Raw compatibility writes cannot replace the named layout policy.
		var record = MuiHeadlessObjectCore.FindObject(ref platform, State, group);
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			record, Horizontal, 0, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			record, PageMode, 0, false));
		Assert.Equal(policy.Horizontal, Get(ref platform, group, Horizontal));
		Assert.Equal(policy.PageMode, Get(ref platform, group, PageMode));

		var message = APTR.FromPointer(0x7800);
		var storage = APTR.FromPointer(0x7900);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Attribute, PageMode));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Storage, storage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			group, message));
		Assert.True(MuiGuestUlongStorageCodec.TryRead(ref platform, storage,
			out var stored));
		Assert.Equal(policy.PageMode, stored.Value);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR cl)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Group.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static void Set(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute, uint value) => Assert.True(
		MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj, attribute,
			value, false));

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			attribute, out var value));
		return value;
	}
}
