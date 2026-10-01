using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListPointerSlotFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedPointerSlotRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListPointerSlotRecord
		{
			Value = APTR.FromPointer(0x4567),
		};
		Assert.True(MuiListCore.MuiListPointerSlotCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListPointerSlotFieldCursor
		{
			Record = address,
			Field = MuiListCore.MuiListPointerSlotField.Value,
		};
		Assert.True(MuiListCore.MuiListPointerSlotFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress,
				out var fieldSize));
		Assert.Equal(address.Raw, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListPointerSlotFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListPointerSlotField.Value, 0xA640));
		Assert.True(MuiListCore.MuiListPointerSlotFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListPointerSlotField.Value, out var pointer));
		Assert.Equal(0xA640u, pointer);
		Assert.True(MuiListCore.MuiListPointerSlotCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(APTR.FromPointer(0xA640), decoded.Value);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListPointerSlotFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListPointerSlotFieldCursor
				{
					Record = address,
					Field = (MuiListCore.MuiListPointerSlotField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListPointerSlotFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListPointerSlotFieldCursor
				{
					Record = APTR.FromPointer(0x30FFD),
					Field = MuiListCore.MuiListPointerSlotField.Value,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListPointerSlotMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListCore.MuiListPointerSlotField.Value,
			out _, out _));
	}
}
