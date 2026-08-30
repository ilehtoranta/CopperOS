using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaLayoutPolicyStructAdapterTests
{
	[Fact]
	public void AreaLayoutPolicySequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3D00);
		var value = new MuiAreaLayoutPolicyStateRecord
		{
			Magic = MuiAreaLayoutPolicyStateRecord.Cookie,
			ShowMe = 1,
			FixWidth = uint.MaxValue,
			FixHeight = 10,
			MaxWidth = 100,
			MaxHeight = 80,
			InnerLeft = 2,
			InnerRight = 3,
			InnerTop = 1,
			InnerBottom = 1,
			HorizontalWeight = 7,
			VerticalWeight = uint.MaxValue - 1,
		};

		Assert.True(MuiAreaLayoutPolicyStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaLayoutPolicyStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.ShowMe, decoded.ShowMe);
		Assert.Equal(value.FixWidth, decoded.FixWidth);
		Assert.Equal(value.FixHeight, decoded.FixHeight);
		Assert.Equal(value.MaxWidth, decoded.MaxWidth);
		Assert.Equal(value.MaxHeight, decoded.MaxHeight);
		Assert.Equal(value.InnerLeft, decoded.InnerLeft);
		Assert.Equal(value.InnerRight, decoded.InnerRight);
		Assert.Equal(value.InnerTop, decoded.InnerTop);
		Assert.Equal(value.InnerBottom, decoded.InnerBottom);
		Assert.Equal(value.HorizontalWeight, decoded.HorizontalWeight);
		Assert.Equal(value.VerticalWeight, decoded.VerticalWeight);

		var crossingEnd = APTR.FromPointer(0x30FF1);
		Assert.False(MuiAreaLayoutPolicyStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiAreaLayoutPolicyStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}
}
