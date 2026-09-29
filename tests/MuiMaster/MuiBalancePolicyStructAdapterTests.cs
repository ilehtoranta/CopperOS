using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiBalancePolicyStructAdapterTests
{
	[Fact]
	public void BalancePolicyOffsetBridgeUsesNamedUlongCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x4200);
		var initial = new MuiBalancePolicyStateRecord
		{
			Magic = MuiBalancePolicyStateRecord.Cookie,
			Quiet = 3,
		};

		Assert.True(MuiBalancePolicyStateRecordCodec.WriteRecord(ref platform,
			address, initial));
		Assert.True(MuiBalancePolicyStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiBalancePolicyStateRecord.QuietOffset,
			0xF1020304u));
		Assert.True(MuiBalancePolicyStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiBalancePolicyStateRecord.QuietOffset,
			out var quiet));
		Assert.Equal(0xF1020304u, quiet);
		Assert.True(MuiBalancePolicyStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(initial.Magic, decoded.Magic);
		Assert.Equal(0xF1020304u, decoded.Quiet);
		Assert.False(MuiBalancePolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiBalancePolicyStateRecord.Size, out _));
		Assert.False(MuiBalancePolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			APTR.Null, MuiBalancePolicyStateRecord.QuietOffset, 1));
	}
}
