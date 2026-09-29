using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListColumnOrderStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedColumnOrderRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListColumnOrderState
		{
			Magic = MuiListCore.MuiListColumnOrderState.Cookie,
			Count = 4,
			Values = APTR.FromPointer(0x1234),
			Reserved = 0xABCD,
		};
		Assert.True(MuiListCore.MuiListColumnOrderStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListColumnOrderStateFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListColumnOrderStateField.Reserved,
		};
		Assert.True(MuiListCore.MuiListColumnOrderStateFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x350Cu, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListColumnOrderStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListColumnOrderStateField.Count, 8));
		Assert.True(MuiListCore.MuiListColumnOrderStateFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListColumnOrderStateField.Values, out var values));
		Assert.Equal(value.Values.Raw, values);
		Assert.True(MuiListCore.MuiListColumnOrderStateCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(8u, decoded.Count);
		Assert.Equal(value.Values.Raw, decoded.Values.Raw);
		Assert.Equal(value.Reserved, decoded.Reserved);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListColumnOrderStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListColumnOrderStateFieldCursor
				{
					Address = address,
					Field = (MuiListCore.MuiListColumnOrderStateField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListColumnOrderStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListColumnOrderStateFieldCursor
				{
					Address = APTR.FromPointer(0x30FF9),
					Field = MuiListCore.MuiListColumnOrderStateField.Magic,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListColumnOrderStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListCore.MuiListColumnOrderStateField.Magic,
			out _, out _));
	}
}
