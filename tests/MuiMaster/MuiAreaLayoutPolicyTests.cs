using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaLayoutPolicyTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint Weight = 0x80421D1F;
	private const uint HorizWeight = 0x80426DB9;
	private const uint VertWeight = 0x804298D0;
	private const uint FixWidth = 0x8042A3F1;
	private const uint FixHeight = 0x8042A92B;
	private const uint InnerLeft = 0x804228F8;
	private const uint InnerRight = 0x804297FF;
	private const uint InnerTop = 0x80421EB6;
	private const uint InnerBottom = 0x8042F2C0;
	private const uint MaxWidth = 0x8042F112;
	private const uint MaxHeight = 0x804293E4;

	[Fact]
	public void AreaLayoutPolicyUsesNamedFieldBoundaries()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			State);
		var address = APTR.FromPointer(0x3000);
		var cursor = new MuiAreaLayoutPolicyFieldCursor
		{
			Address = address,
			Field = MuiAreaLayoutPolicyField.VerticalWeight,
		};
		Assert.True(MuiAreaLayoutPolicyFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var fieldAddress));
		Assert.Equal(APTR.FromPointer(0x302C), fieldAddress);
		Assert.True(MuiAreaLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaLayoutPolicyField.HorizontalWeight, 7));
		Assert.True(MuiAreaLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiAreaLayoutPolicyField.HorizontalWeight, out var horizontal));
		Assert.Equal(7u, horizontal);
		cursor.Field = unchecked((MuiAreaLayoutPolicyField)255);
		Assert.False(MuiAreaLayoutPolicyFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
		cursor.Address = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Field = MuiAreaLayoutPolicyField.VerticalWeight;
		Assert.False(MuiAreaLayoutPolicyFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));

		var expected = new MuiAreaLayoutPolicyStateRecord
		{
			Magic = MuiAreaLayoutPolicyStateRecord.Cookie,
			ShowMe = 1,
			FixWidth = 20,
			FixHeight = 10,
			MaxWidth = 100,
			MaxHeight = 80,
			InnerLeft = 2,
			InnerRight = 3,
			InnerTop = 1,
			InnerBottom = 1,
			HorizontalWeight = 7,
			VerticalWeight = 9,
		};
		Assert.True(MuiAreaLayoutPolicyStateRecordCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiAreaLayoutPolicyStateRecordCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.ShowMe, actual.ShowMe);
		Assert.Equal(expected.FixWidth, actual.FixWidth);
		Assert.Equal(expected.FixHeight, actual.FixHeight);
		Assert.Equal(expected.MaxWidth, actual.MaxWidth);
		Assert.Equal(expected.MaxHeight, actual.MaxHeight);
		Assert.Equal(expected.InnerLeft, actual.InnerLeft);
		Assert.Equal(expected.InnerRight, actual.InnerRight);
		Assert.Equal(expected.InnerTop, actual.InnerTop);
		Assert.Equal(expected.InnerBottom, actual.InnerBottom);
		Assert.Equal(expected.HorizontalWeight, actual.HorizontalWeight);
		Assert.Equal(expected.VerticalWeight, actual.VerticalWeight);
	}

	[Fact]
	public void AreaLayoutPublishesCanonicalMinMaxAndWeightPolicy()
	{
		var platform = CreatePlatform(out var cl);
		var area = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Set(ref platform, area, FixWidth, 20);
		Set(ref platform, area, FixHeight, 10);
		Set(ref platform, area, InnerLeft, 2);
		Set(ref platform, area, InnerRight, 3);
		Set(ref platform, area, InnerTop, 1);
		Set(ref platform, area, InnerBottom, 1);
		Set(ref platform, area, MaxWidth, 100);
		Set(ref platform, area, MaxHeight, 80);
		Set(ref platform, area, Weight, 7);

		var storage = APTR.FromPointer(0x1200);
		Assert.True(MuiAreaLayoutCore.AskMinMax(ref platform, State, area,
			storage));
		Assert.Equal((ushort)25, platform.ReadUInt16(storage, 0));
		Assert.Equal((ushort)12, platform.ReadUInt16(storage, 2));
		Assert.Equal((ushort)25, platform.ReadUInt16(storage, 4));
		Assert.Equal((ushort)12, platform.ReadUInt16(storage, 6));
		Assert.True(MuiAreaLayoutCore.TryGetLayoutPolicyState(ref platform, State,
			area, out var policy));
		Assert.Equal(MuiAreaLayoutPolicyStateRecord.Cookie, policy.Magic);
		Assert.Equal(20u, policy.FixWidth);
		Assert.Equal(10u, policy.FixHeight);
		Assert.Equal(2u, policy.InnerLeft);
		Assert.Equal(3u, policy.InnerRight);
		Assert.Equal(1u, policy.InnerTop);
		Assert.Equal(1u, policy.InnerBottom);
		Assert.Equal(7u, policy.HorizontalWeight);
		Assert.Equal(7u, policy.VerticalWeight);
	}

	[Fact]
	public void SpecificWeightsOverrideSharedWeightInPublishedPolicy()
	{
		var platform = CreatePlatform(out var cl);
		var area = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Set(ref platform, area, Weight, 7);
		Set(ref platform, area, HorizWeight, 3);
		Set(ref platform, area, VertWeight, 5);
		Assert.True(MuiAreaLayoutCore.AskMinMax(ref platform, State, area,
			APTR.FromPointer(0x1200)));
		Assert.True(MuiAreaLayoutCore.TryGetLayoutPolicyState(ref platform, State,
			area, out var policy));
		Assert.Equal(3u, policy.HorizontalWeight);
		Assert.Equal(5u, policy.VerticalWeight);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR cl)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Area.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static void Set(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute, uint value) => Assert.True(
		MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj, attribute,
			value, false));
}
