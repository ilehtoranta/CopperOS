using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaGeometryStructAdapterTests
{
	[Fact]
	public void AreaGeometryFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3F00);
		var value = new MuiAreaGeometryStateRecord
		{
			Magic = MuiAreaGeometryStateRecord.Cookie,
			Left = 10,
			Top = 20,
			Width = 30,
			Height = 40,
			Right = 39,
			Bottom = 59,
		};

		Assert.True(MuiAreaGeometryStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaGeometryStateRecordMemoryCodec.TryWriteFieldInt32(
			ref platform, address, MuiAreaGeometryStateField.Left, -7));
		Assert.True(MuiAreaGeometryStateRecordMemoryCodec.TryReadFieldInt32(
			ref platform, address, MuiAreaGeometryStateField.Bottom, out var bottom));
		Assert.Equal(value.Bottom, bottom);
		Assert.True(MuiAreaGeometryStateRecordMemoryCodec.TryReadFieldUInt32(
			ref platform, address, MuiAreaGeometryStateField.Height, out var height));
		Assert.Equal((uint)value.Height, height);
		Assert.True(MuiAreaGeometryStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(-7, decoded.Left);
		Assert.Equal(value.Top, decoded.Top);
		Assert.Equal(value.Width, decoded.Width);
		Assert.Equal(value.Height, decoded.Height);
		Assert.Equal(value.Right, decoded.Right);
		Assert.Equal(value.Bottom, decoded.Bottom);
		Assert.False(MuiAreaGeometryStateRecordMemoryCodec.TryReadFieldUInt32(ref platform,
			address, (MuiAreaGeometryStateField)255, out _));
	}

	[Fact]
	public void AreaGeometrySequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3FC0);
		var value = new MuiAreaGeometryStateRecord
		{
			Magic = MuiAreaGeometryStateRecord.Cookie,
			Left = int.MinValue,
			Top = -2,
			Width = 0,
			Height = 0,
			Right = 0,
			Bottom = 0,
		};

		Assert.True(MuiAreaGeometryStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaGeometryStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Left, decoded.Left);
		Assert.Equal(value.Top, decoded.Top);
		Assert.Equal(value.Width, decoded.Width);
		Assert.Equal(value.Height, decoded.Height);
		Assert.Equal(value.Right, decoded.Right);
		Assert.Equal(value.Bottom, decoded.Bottom);
		var crossingEnd = APTR.FromPointer(0x30FE5);
		Assert.False(MuiAreaGeometryStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiAreaGeometryStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}
}
