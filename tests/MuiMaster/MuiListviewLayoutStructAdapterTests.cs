using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewLayoutStructAdapterTests
{
	[Fact]
	public void LayoutFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListviewCore.MuiListviewLayoutState
		{
			Magic = MuiListviewCore.MuiListviewLayoutState.Cookie,
			Left = -10,
			Top = 2,
			Width = 320,
			Height = 200,
			ChildLeft = 4,
			ChildTop = 5,
			ChildWidth = 300,
			ChildHeight = 180,
		};

		Assert.True(MuiListviewCore.MuiListviewLayoutStateCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiListviewCore.MuiListviewLayoutMemoryCodec.TryWriteInt32(
			ref platform, address, MuiListviewCore.MuiListviewLayoutField.Left, -20));
		Assert.True(MuiListviewCore.MuiListviewLayoutMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListviewCore.MuiListviewLayoutField.ChildHeight,
			160));
		Assert.True(MuiListviewCore.MuiListviewLayoutMemoryCodec.TryReadInt32(
			ref platform, address, MuiListviewCore.MuiListviewLayoutField.Left,
			out var left));
		Assert.Equal(-20, left);

		Assert.True(MuiListviewCore.MuiListviewLayoutStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(-20, decoded.Left);
		Assert.Equal(value.Top, decoded.Top);
		Assert.Equal(value.Width, decoded.Width);
		Assert.Equal(value.Height, decoded.Height);
		Assert.Equal(value.ChildLeft, decoded.ChildLeft);
		Assert.Equal(value.ChildTop, decoded.ChildTop);
		Assert.Equal(value.ChildWidth, decoded.ChildWidth);
		Assert.Equal(160, decoded.ChildHeight);

		Assert.True(MuiListviewCore.MuiListviewLayoutMemoryCodec.TryGetAddress(
			ref platform, address,
			MuiListviewCore.MuiListviewLayoutField.ChildHeight,
			out var childHeightAddress));
		Assert.Equal(0x3520u, childHeightAddress.Raw);
	}

	[Fact]
	public void LayoutAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewCore.MuiListviewLayoutMemoryCodec.TryReadUInt32(
			ref platform, address,
			(MuiListviewCore.MuiListviewLayoutField)0xFF, out _));
		Assert.False(MuiListviewCore.MuiListviewLayoutMemoryCodec.TryWriteUInt32(
			ref platform, address,
			(MuiListviewCore.MuiListviewLayoutField)0xFF, 1));
		Assert.False(MuiListviewCore.MuiListviewLayoutMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FF0),
			MuiListviewCore.MuiListviewLayoutField.Height, out _));
		Assert.False(MuiListviewCore.MuiListviewLayoutMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null,
			MuiListviewCore.MuiListviewLayoutField.Magic, 1));
	}
}
