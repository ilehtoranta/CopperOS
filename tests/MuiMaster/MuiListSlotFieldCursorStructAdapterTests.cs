using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListSlotFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedListSlotRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListSlotState
		{
			Entry = APTR.FromPointer(0x4567),
			Flags = 0x10203040u,
		};
		Assert.True(MuiListSlotCodec.WriteRecord(ref platform, address, value));
		var cursor = new MuiListSlotFieldCursor
		{
			Address = address,
			Field = MuiListSlotField.Flags,
		};
		Assert.True(MuiListSlotFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3504u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListSlotFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiListSlotField.Flags, 0xAABBCCDDu));
		Assert.True(MuiListSlotFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiListSlotField.Flags, out var flags));
		Assert.Equal(0xAABBCCDDu, flags);
		Assert.True(MuiListSlotCodec.TryReadStructural(ref platform, address,
			out var decoded));
		Assert.Equal(value.Entry, decoded.Entry);
		Assert.Equal(0xAABBCCDDu, decoded.Flags);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListSlotFieldCursorCodec.TryGetAddress(ref platform,
			new MuiListSlotFieldCursor
			{
				Address = address,
				Field = (MuiListSlotField)255,
			}, out _, out _));
		Assert.False(MuiListSlotFieldCursorCodec.TryGetAddress(ref platform,
			new MuiListSlotFieldCursor
			{
				Address = APTR.FromPointer(0x30FF9),
				Field = MuiListSlotField.Entry,
			}, out _, out _));
		Assert.False(MuiListSlotMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiListSlotField.Entry, out _, out _));
	}
}
