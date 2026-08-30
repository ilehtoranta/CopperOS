using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaFixedTextStructAdapterTests
{
	[Fact]
	public void AreaFixedTextStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3D00);
		var value = new MuiAreaFixedTextStateRecord
		{
			Magic = MuiAreaFixedTextStateRecord.Cookie,
			WidthText = APTR.FromPointer(0x4100),
			HeightText = APTR.FromPointer(0x4200),
			Generation = 7,
		};

		Assert.True(MuiAreaFixedTextStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaFixedTextStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiAreaFixedTextStateField.WidthText, out var widthAddress));
		Assert.Equal(0x3D04u, widthAddress.Raw);
		Assert.True(MuiAreaFixedTextStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaFixedTextStateField.Generation, 9));
		Assert.True(MuiAreaFixedTextStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(9u, decoded.Generation);
		Assert.Equal(value.WidthText, decoded.WidthText);
		Assert.Equal(value.HeightText, decoded.HeightText);
		Assert.False(MuiAreaFixedTextStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, (MuiAreaFixedTextStateField)255, out _));
		Assert.False(MuiAreaFixedTextStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiAreaFixedTextStateField.Magic, out _));
		Assert.False(MuiAreaFixedTextStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void AreaFixedTextSequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3DC0);
		var value = new MuiAreaFixedTextStateRecord
		{
			Magic = MuiAreaFixedTextStateRecord.Cookie,
			WidthText = APTR.FromPointer(0xFEEDBEEF),
			HeightText = APTR.FromPointer(0xCAFEBABE),
			Generation = uint.MaxValue,
		};

		Assert.True(MuiAreaFixedTextStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaFixedTextStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.WidthText, decoded.WidthText);
		Assert.Equal(value.HeightText, decoded.HeightText);
		Assert.Equal(value.Generation, decoded.Generation);

		var crossingEnd = APTR.FromPointer(0x30FF1);
		Assert.False(MuiAreaFixedTextStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiAreaFixedTextStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}
}
