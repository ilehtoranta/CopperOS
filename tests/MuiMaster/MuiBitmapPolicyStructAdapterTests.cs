using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiBitmapPolicyStructAdapterTests
{
	[Fact]
	public void BitmapPolicyStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiBitmapPolicyStateRecord
		{
			Magic = MuiBitmapPolicyStateRecord.Cookie,
			Alpha = uint.MaxValue,
			MappingTable = 0,
			Precision = 7,
			SourceColors = 0,
			Transparent = 1,
			UseFriend = 1,
		};

		Assert.True(MuiBitmapPolicyStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiBitmapPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiBitmapPolicyStateField.UseFriend,
			out var friendAddress));
		Assert.Equal(0x3518u, friendAddress.Raw);
		Assert.True(MuiBitmapPolicyStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiBitmapPolicyStateField.Precision,
			out var precision));
		Assert.Equal(7u, precision);
		Assert.True(MuiBitmapPolicyStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiBitmapPolicyStateField.Transparent, 0));
		Assert.True(MuiBitmapPolicyStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(0u, decoded.Transparent);
		Assert.False(MuiBitmapPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FE5),
			MuiBitmapPolicyStateField.Magic, out _));
		Assert.False(MuiBitmapPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiBitmapPolicyStateField.Magic, out _));
	}

	[Fact]
	public void BitmapPolicySequentialRecordPreservesMixedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3580);
		var value = new MuiBitmapPolicyStateRecord
		{
			Magic = MuiBitmapPolicyStateRecord.Cookie,
			Alpha = uint.MaxValue,
			MappingTable = 0xFEEDBEEF,
			Precision = 0x12345678,
			SourceColors = 0xCAFEBABE,
			Transparent = 0xA5A5A5A5,
			UseFriend = 1,
		};

		Assert.True(MuiBitmapPolicyStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiBitmapPolicyStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Alpha, decoded.Alpha);
		Assert.Equal(value.MappingTable, decoded.MappingTable);
		Assert.Equal(value.Precision, decoded.Precision);
		Assert.Equal(value.SourceColors, decoded.SourceColors);
		Assert.Equal(value.Transparent, decoded.Transparent);
		Assert.Equal(value.UseFriend, decoded.UseFriend);

		var crossingEnd = APTR.FromPointer(0x30FFF);
		Assert.False(MuiBitmapPolicyStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiBitmapPolicyStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}
}
