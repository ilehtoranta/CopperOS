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
		var cursor = new MuiAreaLayoutPolicyFieldCursor
		{
			Address = address,
			Field = MuiAreaLayoutPolicyField.VerticalWeight,
		};
		Assert.True(MuiAreaLayoutPolicyFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var typedWeightAddress, out var typedWeightSize));
		Assert.Equal(APTR.FromPointer(0x3D2C), typedWeightAddress);
		Assert.Equal(MuiAreaLayoutPolicyStateRecord.FieldSize, typedWeightSize);
		Assert.True(MuiAreaLayoutPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out var memoryWeightAddress, out var memoryWeightSize));
		Assert.Equal(typedWeightAddress, memoryWeightAddress);
		Assert.Equal(typedWeightSize, memoryWeightSize);
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

	[Fact]
	public void AreaLayoutPolicyFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x4000);
		var value = new MuiAreaLayoutPolicyStateRecord
		{
			Magic = MuiAreaLayoutPolicyStateRecord.Cookie,
			ShowMe = 1,
			FixWidth = 10,
			FixHeight = 20,
			MaxWidth = 100,
			MaxHeight = 80,
			InnerLeft = 2,
			InnerRight = 3,
			InnerTop = 1,
			InnerBottom = 1,
			HorizontalWeight = 7,
			VerticalWeight = 9,
		};

		Assert.True(MuiAreaLayoutPolicyStateRecordCodec.WriteRecord(ref platform,
			address, value));
		var cursor = new MuiAreaLayoutPolicyFieldCursor
		{
			Address = address,
			Field = MuiAreaLayoutPolicyField.MaxWidth,
		};
		Assert.True(MuiAreaLayoutPolicyFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var typedMaxWidthAddress, out var typedMaxWidthSize));
		Assert.Equal(APTR.FromPointer(0x4000 + 4 * MuiAreaLayoutPolicyStateRecord.FieldSize), typedMaxWidthAddress);
		Assert.Equal(MuiAreaLayoutPolicyStateRecord.FieldSize, typedMaxWidthSize);
		Assert.True(MuiAreaLayoutPolicyStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaLayoutPolicyField.MaxWidth, 0xFEEDBEEFu));
		Assert.True(MuiAreaLayoutPolicyStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiAreaLayoutPolicyField.VerticalWeight,
			out var weight));
		Assert.Equal(value.VerticalWeight, weight);
		Assert.True(MuiAreaLayoutPolicyStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0xFEEDBEEFu, decoded.MaxWidth);
		Assert.Equal(value.ShowMe, decoded.ShowMe);
		Assert.Equal(value.VerticalWeight, decoded.VerticalWeight);
		Assert.Equal(value.Magic, decoded.Magic);
	}
}
