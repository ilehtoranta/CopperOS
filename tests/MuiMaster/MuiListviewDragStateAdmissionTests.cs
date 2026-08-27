using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewDragStateAdmissionTests
{
	[Fact]
	public void ListviewDragStateRoundTripsThroughNamedStruct()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2600);
		var value = new MuiListviewDragState
		{
			Magic = MuiListviewDragStateCodec.Cookie,
			Source = 7,
			Target = -1,
			StartX = -4,
			StartY = 12,
			LastX = 18,
			LastY = 24,
			Flags = MuiListviewDragState.ActiveFlag |
				MuiListviewDragState.MovedFlag,
		};
		Assert.True(MuiListviewDragStateCodec.TryWrite(ref platform, address, value));
		Assert.True(MuiListviewDragStateCodec.TryRead(ref platform, address,
			out var read));
		Assert.Equal(value.Magic, read.Magic);
		Assert.Equal(value.Source, read.Source);
		Assert.Equal(value.Target, read.Target);
		Assert.Equal(value.StartX, read.StartX);
		Assert.Equal(value.LastY, read.LastY);
		Assert.Equal(value.Flags, read.Flags);
	}

	[Fact]
	public void MalformedListviewDragMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2640);
		var value = new MuiListviewDragState
		{
			Magic = MuiListviewDragStateCodec.Cookie,
			Source = 3,
			Target = 4,
			StartX = 10,
			StartY = 11,
			LastX = 12,
			LastY = 13,
			Flags = MuiListviewDragState.ActiveFlag,
		};
		Assert.True(MuiListviewDragStateCodec.TryWrite(ref platform, address, value));
		Assert.True(MuiListviewDragStateMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiListviewDragStateField.Magic, 0));
		Assert.True(MuiListviewDragStateCodec.TryReadStructural(ref platform,
			address, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.Equal(value.Source, structural.Source);
		Assert.Equal(value.Target, structural.Target);
		Assert.False(MuiListviewDragStateCodec.TryRead(ref platform, address,
			out _));
	}
}
