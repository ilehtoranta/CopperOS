using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiSleepVolumelistAdmissionTests
{
	[Fact]
	public void SleepAndVolumelistRecordsRoundTripThroughNamedStructs()
	{
		var platform = CreatePlatform();
		var sleepAddress = APTR.FromPointer(0x1500);
		var volumeAddress = APTR.FromPointer(0x1520);
		var sleep = new MuiSleepStateRecord
		{
			Magic = MuiSleepStateRecord.Cookie,
			Depth = 3,
			SavedDisabled = 1,
			Request = 3,
		};
		var volume = new MuiVolumelistCore.MuiVolumelistModeStateRecord
		{
			Magic = MuiVolumelistCore.MuiVolumelistModeStateRecord.Cookie,
			ExampleMode = 1,
		};
		Assert.True(MuiSleepStateRecordCodec.Write(ref platform, sleepAddress,
			sleep));
		Assert.True(MuiVolumelistCore.MuiVolumelistModeStateRecordCodec.Write(
			ref platform, volumeAddress, volume));
		Assert.True(MuiSleepStateRecordCodec.TryRead(ref platform, sleepAddress,
			out var sleepRead));
		Assert.True(MuiVolumelistCore.MuiVolumelistModeStateRecordCodec.TryRead(
			ref platform, volumeAddress, out var volumeRead));
		Assert.Equal(sleep.Depth, sleepRead.Depth);
		Assert.Equal(sleep.SavedDisabled, sleepRead.SavedDisabled);
		Assert.Equal(sleep.Request, sleepRead.Request);
		Assert.Equal(volume.ExampleMode, volumeRead.ExampleMode);
		Assert.False(MuiVolumelistCore.MuiVolumelistModeStateRecordCodec
			.TryReadStructural(ref platform, APTR.FromPointer(0x20FFC), out _));
		Assert.False(MuiVolumelistCore.MuiVolumelistModeStateRecordCodec.Write(
			ref platform, APTR.FromPointer(0x20FFC), volume));
	}

	[Fact]
	public void MalformedSleepAndVolumelistMagicRemainStructuralButFailClosed()
	{
		var platform = CreatePlatform();
		var sleepAddress = APTR.FromPointer(0x1600);
		var volumeAddress = APTR.FromPointer(0x1620);
		Assert.True(MuiSleepStateRecordCodec.Write(ref platform, sleepAddress,
			new MuiSleepStateRecord
			{
				Magic = MuiSleepStateRecord.Cookie,
				Depth = 1,
				SavedDisabled = 0,
				Request = 1,
			}));
		Assert.True(MuiVolumelistCore.MuiVolumelistModeStateRecordCodec.Write(
			ref platform, volumeAddress,
			new MuiVolumelistCore.MuiVolumelistModeStateRecord
			{
				Magic = MuiVolumelistCore.MuiVolumelistModeStateRecord.Cookie,
				ExampleMode = 0,
			}));
		Assert.False(MuiSleepStateRecordCodec.Write(ref platform,
			APTR.FromPointer(0x1640), new MuiSleepStateRecord
			{
				Magic = MuiSleepStateRecord.Cookie,
				Depth = 2,
				SavedDisabled = 2,
				Request = 1,
			}));
		Assert.False(MuiVolumelistCore.MuiVolumelistModeStateRecordCodec.Write(
			ref platform, APTR.FromPointer(0x1660),
			new MuiVolumelistCore.MuiVolumelistModeStateRecord
			{
				Magic = MuiVolumelistCore.MuiVolumelistModeStateRecord.Cookie,
				ExampleMode = 2,
			}));
		Assert.True(MuiSleepStateFieldCursorCodec.TryWriteUInt32(ref platform,
			sleepAddress, MuiSleepStateField.Magic, 0));
		Assert.True(MuiVolumelistCore.MuiVolumelistModeStateRecordMemoryCodec
			.TryWriteUInt32(ref platform, volumeAddress,
				MuiVolumelistCore.MuiVolumelistModeField.Magic, 0));
		Assert.True(MuiSleepStateRecordCodec.TryReadStructural(ref platform,
			sleepAddress, out var sleep));
		Assert.True(MuiVolumelistCore.MuiVolumelistModeStateRecordCodec
			.TryReadStructural(ref platform, volumeAddress, out var volume));
		Assert.Equal(0u, sleep.Magic);
		Assert.Equal(0u, volume.Magic);
		Assert.False(MuiSleepStateRecordCodec.TryRead(ref platform, sleepAddress,
			out _));
		Assert.False(MuiVolumelistCore.MuiVolumelistModeStateRecordCodec.TryRead(
			ref platform, volumeAddress, out _));
		Assert.False(MuiSleepStateAdmission.Validate(sleep));
		Assert.False(MuiVolumelistCore.MuiVolumelistModeStateAdmission.Validate(
			volume));
	}

	private static MuiHeadlessTestPlatform CreatePlatform() =>
		new(0x1000, 0x20000, 0x4000, APTR.FromPointer(0x1000));
}
