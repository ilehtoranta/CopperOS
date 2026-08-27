using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGroupLayoutPolicyTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint LayoutPolicyStateKey = 0x0D100013u;
	private const uint Horizontal = 0x8042536B;
	private const uint Spacing = 0x8042866D;
	private const uint SameWidth = 0x8042B3EC;
	private const uint SameHeight = 0x8042037E;
	private const uint SameSize = 0x80420860;
	private const uint PageMode = 0x80421A5F;
	private const uint LeftEdge = 0x8042BEC6;
	private const uint TopEdge = 0x8042509B;
	private const uint Width = 0x8042B59C;
	private const uint Height = 0x80423237;
	private const uint FixWidth = 0x8042A3F1;
	private const uint FixHeight = 0x8042A92B;
	private const uint HorizontalCenter = 0x8042CC64;
	private const uint VerticalCenter = 0x8042C008;
	private const uint HorizontalWeight = 0x80426DB9;
	private const uint VerticalWeight = 0x804298D0;
	private const uint InnerLeft = 0x804228F8;
	private const uint InnerTop = 0x80421EB6;
	private const uint MaxWidth = 0x8042F112;
	private const uint MaxHeight = 0x804293E4;
	private const uint VerticalSpacing = 0x8042E1BF;

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
	public void GroupLayoutPolicyRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			State);
		var address = APTR.FromPointer(0x1D20);
		var value = new MuiGroupLayoutPolicyStateRecord
		{
			Magic = MuiGroupLayoutPolicyStateRecord.Cookie,
			Horizontal = 1,
			HorizontalSpacing = unchecked((uint)-25),
			VerticalSpacing = 6,
			SameWidth = 1,
			SameHeight = 0,
			PageMode = 1,
		};
		Assert.True(MuiGroupLayoutPolicyStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiGroupLayoutPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiGroupLayoutPolicyField.PageMode,
			out var pageModeAddress));
		Assert.Equal(0x1D38u, pageModeAddress.Raw);
		Assert.True(MuiGroupLayoutPolicyStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiGroupLayoutPolicyField.HorizontalSpacing,
			out var horizontalSpacing));
		Assert.Equal(unchecked((uint)-25), horizontalSpacing);
		Assert.True(MuiGroupLayoutPolicyStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiGroupLayoutPolicyField.PageMode, 0));
		Assert.True(MuiGroupLayoutPolicyStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0u, decoded.PageMode);
		Assert.False(MuiGroupLayoutPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiGroupLayoutPolicyField)255, out _));
		Assert.False(MuiGroupLayoutPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiGroupLayoutPolicyField.Magic, out _));
		Assert.False(MuiGroupLayoutPolicyStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void GroupLayoutPolicyAdmissionRequiresShapeAndLiveOwner()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var valid = new MuiGroupLayoutPolicyStateRecord
		{
			Magic = MuiGroupLayoutPolicyStateRecord.Cookie,
			Horizontal = 1,
			HorizontalSpacing = 4,
			VerticalSpacing = unchecked((uint)-25),
			SameWidth = 1,
			SameHeight = 0,
			PageMode = 1,
		};
		Assert.True(MuiGroupLayoutPolicyStateAdmission.Validate(valid));
		Assert.True(MuiGroupLayoutPolicyStateAdmission.ValidateLive(ref platform,
			State, group, valid));
		var malformed = valid;
		malformed.PageMode = 2;
		Assert.False(MuiGroupLayoutPolicyStateAdmission.Validate(malformed));
		Assert.False(MuiGroupLayoutPolicyStateAdmission.ValidateLive(ref platform,
			State, group, malformed));
		Assert.False(MuiGroupLayoutPolicyStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void EqualExtentSelectionKeepsDefaultInsideFiniteBounds()
	{
		var selection = new MuiGroupEqualExtentSelection
		{
			MinimumExtent = 10,
			MaximumExtent = 20,
			DefaultExtent = 32,
			HasFiniteMaximum = 1,
		};

		MuiGroupEqualExtentCore.NormalizeDefault(ref selection);

		Assert.Equal(20, selection.DefaultExtent);
		Assert.InRange(selection.DefaultExtent, selection.MinimumExtent,
			selection.MaximumExtent);

		selection.DefaultExtent = 4;
		MuiGroupEqualExtentCore.NormalizeDefault(ref selection);
		Assert.Equal(10, selection.DefaultExtent);
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

	[Fact]
	public void MalformedNamedPolicyFailsClosedBeforeGetterAndLayout()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, child));
		Set(ref platform, group, Horizontal, 1);
		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 3, 4,
			80, 30));
		var beforeLeft = Get(ref platform, child, LeftEdge);
		var block = MuiStoreCore.DataspaceFind(ref platform, State, group,
			LayoutPolicyStateKey);
		Assert.True(MuiGroupLayoutPolicyFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiGroupLayoutPolicyField.PageMode, 2));
		Assert.True(MuiGroupLayoutPolicyStateRecordCodec.TryReadStructural(
			ref platform, block, out var malformed));
		Assert.Equal(2u, malformed.PageMode);
		Assert.False(MuiGroupLayoutPolicyStateAdmission.Validate(malformed));
		Assert.False(MuiGroupLayoutPolicyStateRecordCodec.TryRead(ref platform,
			block, out _));

		Assert.False(MuiGroupLayoutCore.TryGetLayoutState(ref platform, State, group,
			out _));
		Assert.False(MuiGroupLayoutCore.TryGetAttribute(ref platform, State, group,
			Horizontal, out _));
		var storage = APTR.FromPointer(0x1800);
		Assert.False(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			storage));
		Assert.False(MuiGroupLayoutCore.Layout(ref platform, State, group, 3, 4,
			80, 30));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			group, Horizontal, out var rawHorizontal));
		Assert.Equal(1u, rawHorizontal);
		Assert.Equal(beforeLeft, GetRaw(ref platform, child, LeftEdge));
	}

	[Fact]
	public void GroupSpacingSpecialInputsUseNamedResolution()
	{
		var defaultValue = MuiGroupSpacingCore.Decode(unchecked((uint)-100));
		Assert.Equal(MuiGroupSpacingKind.Default, defaultValue.Kind);
		Assert.Equal(0, defaultValue.Pixels);

		var percentValue = MuiGroupSpacingCore.Decode(unchecked((uint)-25));
		Assert.Equal(MuiGroupSpacingKind.Percent, percentValue.Kind);
		Assert.Equal(25, percentValue.Payload);
		Assert.Equal(25, MuiGroupSpacingCore.ResolveForLayout(
			unchecked((uint)-25), 100).Pixels);
	}

	[Fact]
	public void GroupSpacingPercentUsesLayoutExtentAndDefaultDoesNotSaturate()
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
		Set(ref platform, group, Spacing, unchecked((uint)-25));

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 0, 0,
			100, 20));
		Assert.Equal(62u, Get(ref platform, second, LeftEdge));
		Assert.True(MuiGroupLayoutCore.TryGetLayoutState(ref platform, State, group,
			out var policy));
		Assert.Equal(unchecked((uint)-25), policy.HorizontalSpacing);

		Set(ref platform, group, Spacing, unchecked((uint)-100));
		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 0, 0,
			100, 20));
		Assert.Equal(50u, Get(ref platform, second, LeftEdge));
	}

	[Fact]
	public void GroupSpacingPercentDoesNotBecomeMinMaxPixelGap()
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
		Set(ref platform, group, Spacing, unchecked((uint)-25));
		Set(ref platform, first, FixWidth, 10);
		Set(ref platform, second, FixWidth, 10);

		var storage = APTR.FromPointer(0x1800);
		Assert.True(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			storage));
		Assert.Equal((ushort)20, platform.ReadUInt16(storage, 0));
	}

	[Fact]
	public void SameWidthUsesCommonMaximumAndMinimumForLayout()
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
		Set(ref platform, first, InnerLeft, 10);
		Set(ref platform, first, MaxWidth, 20);
		Set(ref platform, second, InnerLeft, 20);
		Set(ref platform, second, MaxWidth, 40);

		var storage = APTR.FromPointer(0x1800);
		Assert.True(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			storage));
		Assert.Equal((ushort)44, platform.ReadUInt16(storage, 0));
		Assert.Equal((ushort)44, platform.ReadUInt16(storage, 4));

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 0, 0,
			104, 20));
		Assert.Equal(20u, Get(ref platform, first, 0x8042B59C));
		Assert.Equal(20u, Get(ref platform, second, 0x8042B59C));
		Assert.Equal(24u, Get(ref platform, second, LeftEdge));
	}

	[Fact]
	public void SameHeightUsesCommonMaximumAndMinimumForLayout()
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
		Set(ref platform, group, VerticalSpacing, 3);
		Set(ref platform, group, 0x8042037E, 1);
		Set(ref platform, first, InnerTop, 5);
		Set(ref platform, first, MaxHeight, 30);
		Set(ref platform, second, InnerTop, 10);
		Set(ref platform, second, MaxHeight, 20);

		var storage = APTR.FromPointer(0x1800);
		Assert.True(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			storage));
		Assert.Equal((ushort)23, platform.ReadUInt16(storage, 2));
		Assert.Equal((ushort)43, platform.ReadUInt16(storage, 6));

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 0, 0,
			40, 100));
		Assert.Equal(20u, Get(ref platform, first, Height));
		Assert.Equal(20u, Get(ref platform, second, Height));
		Assert.Equal(23u, Get(ref platform, second, TopEdge));
	}

	[Fact]
	public void HorizontalGroupSameHeightUsesOneCommonBoundedCrossAxisExtent()
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
		Set(ref platform, group, SameHeight, 1);
		Set(ref platform, first, InnerTop, 5);
		Set(ref platform, first, MaxHeight, 30);
		Set(ref platform, second, InnerTop, 10);
		Set(ref platform, second, MaxHeight, 20);

		var storage = APTR.FromPointer(0x1800);
		Assert.True(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			storage));
		Assert.Equal((ushort)10, platform.ReadUInt16(storage, 2));
		Assert.Equal((ushort)20, platform.ReadUInt16(storage, 6));

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 0, 0,
			100, 40));
		Assert.Equal(20u, Get(ref platform, first, Height));
		Assert.Equal(20u, Get(ref platform, second, Height));
		Assert.Equal(10u, Get(ref platform, first, TopEdge));
		Assert.Equal(10u, Get(ref platform, second, TopEdge));
	}

	[Fact]
	public void VerticalGroupSameWidthUsesOneCommonBoundedCrossAxisExtent()
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
		Set(ref platform, group, SameWidth, 1);
		Set(ref platform, first, InnerLeft, 5);
		Set(ref platform, first, MaxWidth, 30);
		Set(ref platform, second, InnerLeft, 10);
		Set(ref platform, second, MaxWidth, 20);

		var storage = APTR.FromPointer(0x1800);
		Assert.True(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			storage));
		Assert.Equal((ushort)10, platform.ReadUInt16(storage, 0));
		Assert.Equal((ushort)20, platform.ReadUInt16(storage, 4));

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 0, 0,
			40, 100));
		Assert.Equal(20u, Get(ref platform, first, Width));
		Assert.Equal(20u, Get(ref platform, second, Width));
		Assert.Equal(10u, Get(ref platform, first, LeftEdge));
		Assert.Equal(10u, Get(ref platform, second, LeftEdge));
	}

	[Fact]
	public void SameSizeShorthandAppliesToOrdinaryGroups()
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
		Set(ref platform, group, Spacing, 2);
		Set(ref platform, group, SameSize, 1);
		Set(ref platform, first, InnerLeft, 5);
		Set(ref platform, first, MaxWidth, 30);
		Set(ref platform, second, InnerLeft, 9);
		Set(ref platform, second, MaxWidth, 20);

		Assert.Equal(1u, Get(ref platform, group, SameWidth));
		Assert.Equal(1u, Get(ref platform, group, SameHeight));
		Assert.Equal(1u, Get(ref platform, group, SameSize));

		var storage = APTR.FromPointer(0x1800);
		Assert.True(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			storage));
		Assert.Equal((ushort)20, platform.ReadUInt16(storage, 0));
		Assert.Equal((ushort)42, platform.ReadUInt16(storage, 4));

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 0, 0,
			100, 20));
		Assert.Equal(20u, Get(ref platform, first, 0x8042B59C));
		Assert.Equal(20u, Get(ref platform, second, 0x8042B59C));
	}

	[Fact]
	public void OrdinaryGroupCenterModesAlignFiniteChildBounds()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, child));
		Set(ref platform, group, Horizontal, 1);
		Set(ref platform, group, HorizontalCenter, 1);
		Set(ref platform, group, VerticalCenter, 2);
		Set(ref platform, child, FixWidth, 20);
		Set(ref platform, child, FixHeight, 10);

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 10, 5,
			100, 40));
		Assert.Equal(50u, Get(ref platform, child, LeftEdge));
		Assert.Equal(35u, Get(ref platform, child, TopEdge));
		Assert.Equal(20u, Get(ref platform, child, Width));
		Assert.Equal(10u, Get(ref platform, child, Height));

		Set(ref platform, group, HorizontalCenter, 0);
		Set(ref platform, group, VerticalCenter, 0);
		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 10, 5,
			100, 40));
		Assert.Equal(10u, Get(ref platform, child, LeftEdge));
		Assert.Equal(5u, Get(ref platform, child, TopEdge));
	}

	[Fact]
	public void HorizontalGroupRedistributesSpaceAfterFiniteMaximum()
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
		Set(ref platform, first, HorizontalWeight, 1);
		Set(ref platform, second, HorizontalWeight, 1);
		Set(ref platform, first, MaxWidth, 10);

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 0, 0,
			100, 20));
		Assert.Equal(10u, Get(ref platform, first, Width));
		Assert.Equal(90u, Get(ref platform, second, Width));
		Assert.Equal(10u, Get(ref platform, second, LeftEdge));
	}

	[Fact]
	public void VerticalGroupRedistributesSpaceAfterFiniteMaximum()
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
		Set(ref platform, first, VerticalWeight, 1);
		Set(ref platform, second, VerticalWeight, 1);
		Set(ref platform, first, MaxHeight, 10);

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 0, 0,
			20, 100));
		Assert.Equal(10u, Get(ref platform, first, Height));
		Assert.Equal(90u, Get(ref platform, second, Height));
		Assert.Equal(10u, Get(ref platform, second, TopEdge));
	}

	[Fact]
	public void HorizontalWeightedGroupReservesChildMinimumBeforeSharing()
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
		Set(ref platform, first, HorizontalWeight, 1);
		Set(ref platform, second, HorizontalWeight, 1);
		Set(ref platform, first, InnerLeft, 80);

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 0, 0,
			100, 20));
		Assert.Equal(80u, Get(ref platform, first, Width));
		Assert.Equal(20u, Get(ref platform, second, Width));
		Assert.Equal(80u, Get(ref platform, second, LeftEdge));
	}

	[Fact]
	public void VerticalWeightedGroupReservesChildMinimumBeforeSharing()
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
		Set(ref platform, first, VerticalWeight, 1);
		Set(ref platform, second, VerticalWeight, 1);
		Set(ref platform, first, InnerTop, 70);

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 0, 0,
			20, 100));
		Assert.Equal(70u, Get(ref platform, first, Height));
		Assert.Equal(30u, Get(ref platform, second, Height));
		Assert.Equal(70u, Get(ref platform, second, TopEdge));
	}

	[Fact]
	public void HorizontalGroupPreservesUnboundedChildMaximums()
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
		Set(ref platform, first, MaxWidth, 10);
		Set(ref platform, first, MaxHeight, 20);
		Set(ref platform, second, MaxWidth, 0);
		Set(ref platform, second, MaxHeight, 0);

		var storage = APTR.FromPointer(0x1800);
		Assert.True(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			storage));
		Assert.Equal((ushort)0, platform.ReadUInt16(storage, 4));
		Assert.Equal((ushort)0, platform.ReadUInt16(storage, 6));
	}

	[Fact]
	public void VerticalGroupPreservesUnboundedChildMaximums()
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
		Set(ref platform, first, MaxWidth, 10);
		Set(ref platform, first, MaxHeight, 20);
		Set(ref platform, second, MaxWidth, 0);
		Set(ref platform, second, MaxHeight, 0);

		var storage = APTR.FromPointer(0x1800);
		Assert.True(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			storage));
		Assert.Equal((ushort)0, platform.ReadUInt16(storage, 4));
		Assert.Equal((ushort)0, platform.ReadUInt16(storage, 6));
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

	private static uint GetRaw(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, obj,
			attribute, out var value));
		return value;
	}
}
