using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGroupLayoutHookStructAdapterTests
{
	[Fact]
	public void GroupLayoutHookSequentialRecordPreservesPointerAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3D00);
		var value = new MuiGroupLayoutHookStateRecord
		{
			Magic = MuiGroupLayoutHookStateRecord.Cookie,
			Hook = APTR.FromPointer(0xFEEDBEEF),
		};

		Assert.True(MuiGroupLayoutHookStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiGroupLayoutHookStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Hook, decoded.Hook);

		var crossingEnd = APTR.FromPointer(0x30FFD);
		Assert.False(MuiGroupLayoutHookStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiGroupLayoutHookStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}
}
