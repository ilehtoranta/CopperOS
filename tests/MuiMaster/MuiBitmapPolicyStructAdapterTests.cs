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
}
