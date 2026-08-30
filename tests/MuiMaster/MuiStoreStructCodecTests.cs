using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStoreStructCodecTests
{
	[Fact]
	public void StoreStructCodecRoundTripsMixedDeclarationOrder()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2800);
		var value = new MuiStoreRecord
		{
			Next = APTR.FromPointer(0x3000),
			Key = 1,
			Data = APTR.FromPointer(0x3040),
			Length = 2,
			Flags = 3,
			Generation = 4,
		};
		Assert.True(MuiStoreRecordCodec.WriteRecord(ref platform, address, value));
		Assert.True(MuiStoreRecordCodec.TryReadRecord(ref platform, address,
			out var decoded));
		Assert.Equal(value.Next.Raw, decoded.Next.Raw);
		Assert.Equal(value.Key, decoded.Key);
		Assert.Equal(value.Data.Raw, decoded.Data.Raw);
		Assert.Equal(value.Length, decoded.Length);
		Assert.Equal(value.Flags, decoded.Flags);
		Assert.Equal(value.Generation, decoded.Generation);
		Assert.False(MuiStoreRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FE1), out _));
	}
}
