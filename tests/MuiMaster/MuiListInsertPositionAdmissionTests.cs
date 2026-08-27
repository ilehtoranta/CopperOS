using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListInsertPositionAdmissionTests
{
	[Fact]
	public void ListInsertPositionRoundTripsThroughNamedStruct()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3280);
		var value = new MuiListCore.MuiListInsertPositionState
		{
			Magic = MuiListCore.MuiListInsertPositionState.Cookie,
			Position = 23,
		};
		Assert.True(MuiListCore.MuiListInsertPositionStateCodec.Write(ref platform, address,
			value));
		Assert.True(MuiListCore.MuiListInsertPositionStateCodec.TryRead(ref platform,
			address, out var read));
		Assert.Equal(value.Magic, read.Magic);
		Assert.Equal(value.Position, read.Position);
	}

	[Fact]
	public void MalformedListInsertPositionMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x32A0);
		Assert.True(MuiListCore.MuiListInsertPositionStateCodec.Write(ref platform, address,
			new MuiListCore.MuiListInsertPositionState
			{
				Magic = MuiListCore.MuiListInsertPositionState.Cookie,
				Position = 4,
			}));
		Assert.True(MuiListCore.MuiListInsertPositionStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListInsertPositionStateField.Magic, 0));
		Assert.True(MuiListCore.MuiListInsertPositionStateCodec.TryReadStructural(ref platform,
			address, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.Equal(4u, structural.Position);
		Assert.False(MuiListCore.MuiListInsertPositionStateCodec.TryRead(ref platform,
			address, out _));
		Assert.True(MuiListCore.MuiListInsertPositionStateCodec.TryReadStorage(ref platform,
			address, out var storage));
		Assert.Equal(0u, storage.Magic);
	}
}
