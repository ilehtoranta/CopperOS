using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListTitleArrayAdmissionTests
{
	[Fact]
	public void ListTitleArrayRecordRoundTripsThroughNamedStruct()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3900);
		var pointers = APTR.FromPointer(0x3940);
		Assert.True(MuiListCore.MuiListPointerSlotCodec.Write(ref platform,
			pointers, new MuiListCore.MuiListPointerSlotRecord
			{
				Value = APTR.FromPointer(0x3A00),
			}));
		var second = APTR.FromPointer(pointers.Raw + 4);
		Assert.True(MuiListCore.MuiListPointerSlotCodec.Write(ref platform,
			second, new MuiListCore.MuiListPointerSlotRecord
			{
				Value = APTR.FromPointer(0x3A20),
			}));
		var value = new MuiListCore.MuiListTitleArrayState
		{
			Magic = MuiListCore.MuiListTitleArrayState.Cookie,
			Pointers = pointers,
			Count = 2,
		};
		Assert.True(MuiListCore.MuiListTitleArrayStateCodec.Write(ref platform,
			address, value));
		Assert.True(MuiListCore.MuiListTitleArrayStateCodec.TryRead(ref platform,
			address, out var read));
		Assert.Equal(value.Pointers, read.Pointers);
		Assert.Equal(value.Count, read.Count);
	}

	[Fact]
	public void MalformedListTitleArrayMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3980);
		var pointers = APTR.FromPointer(0x39C0);
		Assert.True(MuiListCore.MuiListTitleArrayStateCodec.Write(ref platform,
			address, new MuiListCore.MuiListTitleArrayState
			{
				Magic = MuiListCore.MuiListTitleArrayState.Cookie,
				Pointers = pointers,
				Count = 1,
			}));
		Assert.True(MuiListCore.MuiListTitleArrayStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListTitleArrayStateField.Magic, 0));
		Assert.True(MuiListCore.MuiListTitleArrayStateCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.Equal(1u, structural.Count);
		Assert.Equal(pointers, structural.Pointers);
		Assert.False(MuiListCore.MuiListTitleArrayStateCodec.TryRead(ref platform,
			address, out _));
		Assert.True(MuiListCore.MuiListTitleArrayStateCodec.TryReadStorage(
			ref platform, address, out _));
	}
}
