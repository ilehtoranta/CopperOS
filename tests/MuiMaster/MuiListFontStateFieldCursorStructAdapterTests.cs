using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListFontStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedFontStateRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListFontState
		{
			Magic = MuiListCore.MuiListFontState.Cookie,
			Font = APTR.FromPointer(0x4567),
		};
		Assert.True(MuiListCore.MuiListFontStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListFontStateFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListFontStateField.Font,
		};
		Assert.True(MuiListCore.MuiListFontStateFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3504u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListFontStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListFontStateField.Font, 0xA640));
		Assert.True(MuiListCore.MuiListFontStateFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListFontStateField.Font, out var font));
		Assert.Equal(0xA640u, font);
		Assert.True(MuiListCore.MuiListFontStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(APTR.FromPointer(0xA640), decoded.Font);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListFontStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListFontStateFieldCursor
				{
					Address = address,
					Field = (MuiListCore.MuiListFontStateField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListFontStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListFontStateFieldCursor
				{
					Address = APTR.FromPointer(0x30FF9),
					Field = MuiListCore.MuiListFontStateField.Magic,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListFontStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListCore.MuiListFontStateField.Magic,
			out _, out _));
	}
}
