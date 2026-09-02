using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaTextColorStructAdapterTests
{
	[Fact]
	public void AreaTextColorStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiAreaTextColorStateRecord
		{
			Magic = MuiAreaTextColorStateRecord.Cookie,
			Color = 0x00C0FFEE,
			Active = 1,
			Generation = 7,
		};

		Assert.True(MuiAreaTextColorStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaTextColorStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaTextColorStateField.Color,
			out var colorAddress));
		Assert.Equal(0x3504u, colorAddress.Raw);
		Assert.True(MuiAreaTextColorStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaTextColorStateField.Color, 0x00112233));
		Assert.True(MuiAreaTextColorStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0x00112233u, decoded.Color);
		Assert.False(MuiAreaTextColorStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaTextColorStateField)255, out _));
		Assert.False(MuiAreaTextColorStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaTextColorStateField.Magic, out _));
		Assert.False(MuiAreaTextColorStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void AreaTextColorSequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x35C0);
		var value = new MuiAreaTextColorStateRecord
		{
			Magic = MuiAreaTextColorStateRecord.Cookie,
			Color = 0x00FFFFFFu,
			Active = uint.MaxValue,
			Generation = uint.MaxValue,
		};

		Assert.True(MuiAreaTextColorStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaTextColorStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Color, decoded.Color);
		Assert.Equal(value.Active, decoded.Active);
		Assert.Equal(value.Generation, decoded.Generation);

		var crossingEnd = APTR.FromPointer(0x30FF1);
		Assert.False(MuiAreaTextColorStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiAreaTextColorStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}

	[Fact]
	public void AreaTextColorFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3F00);
		var value = new MuiAreaTextColorStateRecord
		{
			Magic = MuiAreaTextColorStateRecord.Cookie,
			Color = 0x00C0FFEEu,
			Active = 1,
			Generation = 19,
		};

		Assert.True(MuiAreaTextColorStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaTextColorStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaTextColorStateField.Color, 0x00112233u));
		Assert.True(MuiAreaTextColorStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiAreaTextColorStateField.Generation,
			out var generation));
		Assert.Equal(value.Generation, generation);
		Assert.True(MuiAreaTextColorStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0x00112233u, decoded.Color);
		Assert.Equal(value.Active, decoded.Active);
		Assert.Equal(value.Magic, decoded.Magic);
	}
}
