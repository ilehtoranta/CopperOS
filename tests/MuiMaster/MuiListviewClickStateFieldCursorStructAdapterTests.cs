using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewClickStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedClickStateRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListviewCore.MuiListviewClickState
		{
			Magic = MuiListviewCore.MuiListviewClickState.Cookie,
			ClickColumn = 2,
			DoubleClick = 3,
			AgainClick = 4,
			Clicks = 5,
			DefClickColumn = 6,
		};
		Assert.True(MuiListviewCore.MuiListviewClickStateCodec.WriteRecord(
			ref platform, address, value));

		var cursor = new MuiListviewCore.MuiListviewClickStateFieldCursor
		{
			Record = address,
			Field = MuiListviewCore.MuiListviewClickStateField.DefClickColumn,
		};
		Assert.True(MuiListviewCore.MuiListviewClickStateFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress,
				out var fieldSize));
		Assert.Equal(0x3514u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListviewCore.MuiListviewClickStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListviewCore.MuiListviewClickStateField.Clicks, 9));
		Assert.True(MuiListviewCore.MuiListviewClickStateFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListviewCore.MuiListviewClickStateField.Clicks, out var clicks));
		Assert.Equal(9u, clicks);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewCore.MuiListviewClickStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListviewCore.MuiListviewClickStateFieldCursor
				{
					Record = address,
					Field = (MuiListviewCore.MuiListviewClickStateField)255,
				}, out _, out _));
		Assert.False(MuiListviewCore.MuiListviewClickStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListviewCore.MuiListviewClickStateFieldCursor
				{
					Record = APTR.FromPointer(0x30FF0),
					Field = MuiListviewCore.MuiListviewClickStateField.Magic,
				}, out _, out _));
		Assert.False(MuiListviewCore.MuiListviewClickStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
			MuiListviewCore.MuiListviewClickStateField.Magic, out _, out _));
	}
}
