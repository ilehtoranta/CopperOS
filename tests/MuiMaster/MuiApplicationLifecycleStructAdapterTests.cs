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
}
