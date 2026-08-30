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

	[Fact]
	public void ApplicationSchedulerSequentialRecordPreservesPointersAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x35C0);
		var value = new MuiApplicationSchedulerStateRecord
		{
			Magic = MuiApplicationSchedulerStateRecord.Cookie,
			ReturnHead = APTR.FromPointer(uint.MaxValue),
			ReturnTail = APTR.FromPointer(0x01020304u),
			InputHandlers = APTR.FromPointer(0x11223344u),
			SignalMask = 0x55667788u,
			PushHead = APTR.FromPointer(0x99AABBCCu),
			PushTail = APTR.FromPointer(0xDDEEFF00u),
		};

		Assert.True(MuiApplicationSchedulerStateRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiApplicationSchedulerStateRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.ReturnHead, decoded.ReturnHead);
		Assert.Equal(value.ReturnTail, decoded.ReturnTail);
		Assert.Equal(value.InputHandlers, decoded.InputHandlers);
		Assert.Equal(value.SignalMask, decoded.SignalMask);
		Assert.Equal(value.PushHead, decoded.PushHead);
		Assert.Equal(value.PushTail, decoded.PushTail);

		var crossingEnd = APTR.FromPointer(0x30FE5);
		Assert.False(MuiApplicationSchedulerStateRecordCodec.WriteRecord(
			ref platform, crossingEnd, value));
		Assert.False(MuiApplicationSchedulerStateRecordCodec.TryReadRecord(
			ref platform, crossingEnd, out _));
	}
}
