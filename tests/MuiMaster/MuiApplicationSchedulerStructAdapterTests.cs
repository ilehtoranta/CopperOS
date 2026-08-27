using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationSchedulerStructAdapterTests
{
	[Fact]
	public void ApplicationSchedulerStateUsesDedicatedNamedStructAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3400);
		var value = new MuiApplicationSchedulerStateRecord
		{
			Magic = MuiApplicationSchedulerStateRecord.Cookie,
			ReturnHead = APTR.Null,
			ReturnTail = APTR.Null,
			InputHandlers = APTR.Null,
			SignalMask = 0x20,
			PushHead = APTR.Null,
			PushTail = APTR.Null,
		};

		Assert.True(MuiApplicationSchedulerStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationSchedulerStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationSchedulerStateField.ReturnTail,
			out var returnTailField));
		Assert.Equal(APTR.FromPointer(0x3408), returnTailField);
		Assert.True(MuiApplicationSchedulerStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationSchedulerStateField.SignalMask,
			out var signalField));
		Assert.Equal(APTR.FromPointer(0x3410), signalField);
		Assert.True(MuiApplicationSchedulerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationSchedulerStateField.SignalMask,
			0x40));
		Assert.True(MuiApplicationSchedulerStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(0x40u, decoded.SignalMask);
		Assert.Equal(value.PushTail, decoded.PushTail);
		Assert.False(MuiApplicationSchedulerStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FE8),
			MuiApplicationSchedulerStateField.Magic, out _));
		Assert.False(MuiApplicationSchedulerStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
			MuiApplicationSchedulerStateField.PushHead, out _));
	}
}
