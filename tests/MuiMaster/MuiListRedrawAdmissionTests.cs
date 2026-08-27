using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListRedrawAdmissionTests
{
	[Fact]
	public void ListRedrawRecordRoundTripsThroughNamedStruct()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3300);
		var value = new MuiListCore.MuiListRedrawState
		{
			Magic = MuiListCore.MuiListRedrawState.Cookie,
			Dirty = 1,
			Requests = 9,
		};
		Assert.True(MuiListCore.MuiListRedrawStateCodec.Write(ref platform,
			address, value));
		Assert.True(MuiListCore.MuiListRedrawStateCodec.TryRead(ref platform,
			address, out var read));
		Assert.Equal(value.Magic, read.Magic);
		Assert.Equal(value.Dirty, read.Dirty);
		Assert.Equal(value.Requests, read.Requests);
	}

	[Fact]
	public void MalformedListRedrawMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3320);
		Assert.True(MuiListCore.MuiListRedrawStateCodec.Write(ref platform,
			address, new MuiListCore.MuiListRedrawState
			{
				Magic = MuiListCore.MuiListRedrawState.Cookie,
				Dirty = 0,
				Requests = 4,
			}));
		Assert.True(MuiListCore.MuiListRedrawStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListRedrawStateField.Magic, 0));
		Assert.True(MuiListCore.MuiListRedrawStateCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.Equal(4u, structural.Requests);
		Assert.False(MuiListCore.MuiListRedrawStateCodec.TryRead(ref platform,
			address, out _));
		Assert.True(MuiListCore.MuiListRedrawStateCodec.TryReadStorage(ref platform,
			address, out var storage));
		Assert.Equal(0u, storage.Magic);
	}
}
