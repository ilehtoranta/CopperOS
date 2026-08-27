using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListColumnOrderAdmissionTests
{
	[Fact]
	public void ListColumnOrderRecordRoundTripsThroughNamedStruct()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3700);
		var values = APTR.FromPointer(0x3740);
		platform.WriteUInt8(values, 0, 1);
		platform.WriteUInt8(values, 1, 0);
		var value = new MuiListCore.MuiListColumnOrderState
		{
			Magic = MuiListCore.MuiListColumnOrderState.Cookie,
			Count = 2,
			Values = values,
			Reserved = 4,
		};
		Assert.True(MuiListCore.MuiListColumnOrderStateCodec.Write(ref platform,
			address, value));
		Assert.True(MuiListCore.MuiListColumnOrderStateCodec.TryRead(ref platform,
			address, out var read));
		Assert.Equal(value.Count, read.Count);
		Assert.Equal(value.Values, read.Values);
		Assert.Equal(value.Reserved, read.Reserved);
	}

	[Fact]
	public void MalformedListColumnOrderMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3780);
		var values = APTR.FromPointer(0x37C0);
		platform.WriteUInt8(values, 0, 1);
		platform.WriteUInt8(values, 1, 0);
		Assert.True(MuiListCore.MuiListColumnOrderStateCodec.Write(ref platform,
			address, new MuiListCore.MuiListColumnOrderState
			{
				Magic = MuiListCore.MuiListColumnOrderState.Cookie,
				Count = 2,
				Values = values,
				Reserved = 4,
			}));
		Assert.True(MuiListCore.MuiListColumnOrderStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListColumnOrderStateField.Magic, 0));
		Assert.True(MuiListCore.MuiListColumnOrderStateCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.Equal(2u, structural.Count);
		Assert.Equal(values, structural.Values);
		Assert.False(MuiListCore.MuiListColumnOrderStateCodec.TryRead(ref platform,
			address, out _));
		Assert.True(MuiListCore.MuiListColumnOrderStateCodec.TryReadStorage(
			ref platform, address, out var storage));
		Assert.Equal(0u, storage.Magic);
	}
}
