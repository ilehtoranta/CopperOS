using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListClickStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedClickStateRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListClickState
		{
			Magic = MuiListCore.MuiListClickState.Cookie,
			ClickColumn = 2,
			DoubleClick = 1,
			AgainClick = 3,
			Clicks = 4,
			DefClickColumn = 5,
		};
		Assert.True(MuiListCore.MuiListClickStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListClickStateFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListClickStateField.DefClickColumn,
		};
		Assert.True(MuiListCore.MuiListClickStateFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3514u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListClickStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListClickStateField.Clicks, 8));
		Assert.True(MuiListCore.MuiListClickStateFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListClickStateField.DoubleClick, out var doubleClick));
		Assert.Equal(1u, doubleClick);
		Assert.True(MuiListCore.MuiListClickStateCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.ClickColumn, decoded.ClickColumn);
		Assert.Equal(value.AgainClick, decoded.AgainClick);
		Assert.Equal(8u, decoded.Clicks);
		Assert.Equal(value.DefClickColumn, decoded.DefClickColumn);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListClickStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListClickStateFieldCursor
				{
					Address = address,
					Field = (MuiListCore.MuiListClickStateField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListClickStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListClickStateFieldCursor
				{
					Address = APTR.FromPointer(0x30FF9),
					Field = MuiListCore.MuiListClickStateField.Magic,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListClickStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListCore.MuiListClickStateField.Magic,
			out _, out _));
	}
}
