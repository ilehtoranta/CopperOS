using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListColumnVisibilityAdmissionTests
{
	[Fact]
	public void ListColumnVisibilityRecordRoundTripsThroughNamedStruct()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3600);
		var value = new MuiListCore.MuiListColumnVisibilityState
		{
			Magic = MuiListCore.MuiListColumnVisibilityState.Cookie,
			Low = 0x00000005,
			High = 0x80000000,
			Word2 = 0x12,
			Word7 = 0x80000001,
		};
		Assert.True(MuiListCore.MuiListColumnVisibilityStateCodec.Write(
			ref platform, address, value));
		Assert.True(MuiListCore.MuiListColumnVisibilityStateCodec.TryRead(
			ref platform, address, out var read));
		Assert.Equal(value.Low, read.Low);
		Assert.Equal(value.High, read.High);
		Assert.Equal(value.Word2, read.Word2);
		Assert.Equal(value.Word7, read.Word7);
	}

	[Fact]
	public void MalformedListColumnVisibilityMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3650);
		Assert.True(MuiListCore.MuiListColumnVisibilityStateCodec.Write(
			ref platform, address, new MuiListCore.MuiListColumnVisibilityState
			{
				Magic = MuiListCore.MuiListColumnVisibilityState.Cookie,
				Low = 0x55,
				Word7 = 0xAA,
			}));
		Assert.True(MuiListCore.MuiListColumnVisibilityStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListColumnVisibilityStateField.Magic, 0));
		Assert.True(MuiListCore.MuiListColumnVisibilityStateCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.Equal(0x55u, structural.Low);
		Assert.Equal(0xAAu, structural.Word7);
		Assert.False(MuiListCore.MuiListColumnVisibilityStateCodec.TryRead(
			ref platform, address, out _));
		Assert.True(MuiListCore.MuiListColumnVisibilityStateCodec.TryReadStorage(
			ref platform, address, out _));
	}
}
