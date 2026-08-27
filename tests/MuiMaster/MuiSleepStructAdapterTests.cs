using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiSleepStructAdapterTests
{
	[Fact]
	public void SleepStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiSleepStateRecord
		{
			Magic = MuiSleepStateRecord.Cookie,
			Depth = 3,
			SavedDisabled = 1,
			Request = 3,
		};

		Assert.True(MuiSleepStateRecordCodec.Write(ref platform, address, value));
		Assert.True(MuiSleepStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiSleepStateField.SavedDisabled, out var disabledAddress));
		Assert.Equal(0x3508u, disabledAddress.Raw);
		Assert.True(MuiSleepStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiSleepStateField.Request, out var request));
		Assert.Equal(3u, request);
		Assert.True(MuiSleepStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiSleepStateField.Depth, 4));
		Assert.True(MuiSleepStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiSleepStateField.Request, 4));
		Assert.True(MuiSleepStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(4u, decoded.Depth);
		Assert.Equal(4u, decoded.Request);
		Assert.False(MuiSleepStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x30FF1), MuiSleepStateField.Magic, out _));
		Assert.False(MuiSleepStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiSleepStateField.Magic, out _));
	}
}
