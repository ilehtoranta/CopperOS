using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListActiveAdmissionTests
{
	[Fact]
	public void ListActiveRecordRoundTripsThroughNamedStruct()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3200);
		var value = new MuiListCore.MuiListActiveState
		{
			Magic = MuiListCore.MuiListActiveState.Cookie,
			HasActive = 1,
			Active = 7,
		};
		Assert.True(MuiListCore.MuiListActiveStateCodec.Write(ref platform,
			address, value));
		Assert.True(MuiListCore.MuiListActiveStateCodec.TryRead(ref platform,
			address, out var read));
		Assert.Equal(value.Magic, read.Magic);
		Assert.Equal(value.HasActive, read.HasActive);
		Assert.Equal(value.Active, read.Active);
	}

	[Fact]
	public void MalformedListActiveMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3240);
		Assert.True(MuiListCore.MuiListActiveStateCodec.Write(ref platform,
			address, new MuiListCore.MuiListActiveState
			{
				Magic = MuiListCore.MuiListActiveState.Cookie,
				HasActive = 1,
				Active = 2,
			}));
		Assert.True(MuiListCore.MuiListActiveStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListActiveStateField.Magic, 0));
		Assert.True(MuiListCore.MuiListActiveStateCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.Equal(1u, structural.HasActive);
		Assert.Equal(2u, structural.Active);
		Assert.False(MuiListCore.MuiListActiveStateCodec.TryRead(ref platform,
			address, out _));
		Assert.True(MuiListCore.MuiListActiveStateCodec.TryReadStorage(ref platform,
			address, out var storage));
		Assert.Equal(0u, storage.Magic);
	}

	[Fact]
	public void ListActiveSequentialRecordPreservesPresenceAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3640);
		var value = new MuiListCore.MuiListActiveState
		{
			Magic = MuiListCore.MuiListActiveState.Cookie,
			HasActive = 0x80000001u,
			Active = 0xFFFFFFFFu,
		};

		Assert.True(MuiListCore.MuiListActiveStateCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiListCore.MuiListActiveStateCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.HasActive, decoded.HasActive);
		Assert.Equal(value.Active, decoded.Active);

		Assert.False(MuiListCore.MuiListActiveStateCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FF5), out _));
	}
}
