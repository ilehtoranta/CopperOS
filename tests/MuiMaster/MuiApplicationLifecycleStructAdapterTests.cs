using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationLifecycleStructAdapterTests
{
	[Fact]
	public void ApplicationLifecycleStateUsesDedicatedNamedStructAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiApplicationLifecycleStateRecord
		{
			Magic = MuiApplicationLifecycleStateRecord.Cookie,
			Initialized = 1,
			Iconified = 0,
			Active = 1,
			SingleTask = 0,
			DoubleStart = 1,
			ForceQuit = 0,
		};

		Assert.True(MuiApplicationLifecycleStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationLifecycleStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationLifecycleStateField.Active,
			out var activeField));
		Assert.Equal(APTR.FromPointer(0x350C), activeField);
		Assert.True(MuiApplicationLifecycleStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationLifecycleStateField.ForceQuit,
			out var forceQuitField));
		Assert.Equal(APTR.FromPointer(0x3518), forceQuitField);
		Assert.True(MuiApplicationLifecycleStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationLifecycleStateField.ForceQuit, 1));
		Assert.True(MuiApplicationLifecycleStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Active, decoded.Active);
		Assert.Equal(1u, decoded.ForceQuit);
		Assert.False(MuiApplicationLifecycleStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FE8),
			MuiApplicationLifecycleStateField.Magic, out _));
		Assert.False(MuiApplicationLifecycleStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
			MuiApplicationLifecycleStateField.Initialized, out _));
	}

	[Fact]
	public void ApplicationLifecycleStateSequentialRecordPreservesRawFlagsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3600);
		var value = new MuiApplicationLifecycleStateRecord
		{
			Magic = MuiApplicationLifecycleStateRecord.Cookie,
			Initialized = 0x01020304u,
			Iconified = 0x11223344u,
			Active = 0x55667788u,
			SingleTask = 0x99AABBCCu,
			DoubleStart = 0xDDEEFF00u,
			ForceQuit = uint.MaxValue,
		};

		Assert.True(MuiApplicationLifecycleStateRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiApplicationLifecycleStateRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Initialized, decoded.Initialized);
		Assert.Equal(value.Iconified, decoded.Iconified);
		Assert.Equal(value.Active, decoded.Active);
		Assert.Equal(value.SingleTask, decoded.SingleTask);
		Assert.Equal(value.DoubleStart, decoded.DoubleStart);
		Assert.Equal(value.ForceQuit, decoded.ForceQuit);

		var crossingEnd = APTR.FromPointer(0x30FE5);
		Assert.False(MuiApplicationLifecycleStateRecordCodec.WriteRecord(
			ref platform, crossingEnd, value));
		Assert.False(MuiApplicationLifecycleStateRecordCodec.TryReadRecord(
			ref platform, crossingEnd, out _));
	}

	[Fact]
	public void ApplicationLifecycleFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3640);
		var initial = new MuiApplicationLifecycleStateRecord
		{
			Magic = 0x10203040u,
			Initialized = 1,
			Iconified = 0,
			Active = 1,
			SingleTask = 0,
			DoubleStart = 1,
			ForceQuit = 0,
		};

		Assert.True(MuiApplicationLifecycleStateRecordCodec.WriteRecord(
			ref platform, address, initial));
		Assert.True(MuiApplicationLifecycleStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationLifecycleStateField.ForceQuit, 1));
		Assert.True(MuiApplicationLifecycleStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationLifecycleStateField.Active,
			out var active));
		Assert.Equal(initial.Active, active);
		Assert.True(MuiApplicationLifecycleStateRecordCodec.TryReadStructural(
			ref platform, address, out var updated));
		Assert.Equal(initial.Magic, updated.Magic);
		Assert.Equal(initial.Initialized, updated.Initialized);
		Assert.Equal(initial.Iconified, updated.Iconified);
		Assert.Equal(initial.Active, updated.Active);
		Assert.Equal(initial.SingleTask, updated.SingleTask);
		Assert.Equal(initial.DoubleStart, updated.DoubleStart);
		Assert.Equal(1u, updated.ForceQuit);
		Assert.False(MuiApplicationLifecycleStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address,
			unchecked((MuiApplicationLifecycleStateField)255), 1));
	}
}
