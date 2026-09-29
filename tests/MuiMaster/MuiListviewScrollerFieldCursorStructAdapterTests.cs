using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewScrollerFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedScrollerRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListviewCore.MuiListviewScrollerState
		{
			Magic = MuiListviewCore.MuiListviewScrollerState.Cookie,
			Entries = 100,
			Visible = 20,
			First = 3,
			MaxFirst = 80,
		};
		Assert.True(MuiListviewCore.MuiListviewScrollerStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListviewCore.MuiListviewScrollerFieldCursor
		{
			Record = address,
			Field = MuiListviewCore.MuiListviewScrollerField.MaxFirst,
		};
		Assert.True(MuiListviewCore.MuiListviewScrollerFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3510u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListviewCore.MuiListviewScrollerFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiListviewCore.MuiListviewScrollerField.First, 9));
		Assert.True(MuiListviewCore.MuiListviewScrollerFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiListviewCore.MuiListviewScrollerField.First,
			out var first));
		Assert.Equal(9u, first);
		Assert.True(MuiListviewCore.MuiListviewScrollerStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Entries, decoded.Entries);
		Assert.Equal(9u, decoded.First);
		Assert.Equal(value.MaxFirst, decoded.MaxFirst);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewCore.MuiListviewScrollerFieldCursorCodec.TryGetAddress(
			ref platform,
			new MuiListviewCore.MuiListviewScrollerFieldCursor
			{
				Record = address,
				Field = (MuiListviewCore.MuiListviewScrollerField)255,
			}, out _, out _));
		Assert.False(MuiListviewCore.MuiListviewScrollerFieldCursorCodec.TryGetAddress(
			ref platform,
			new MuiListviewCore.MuiListviewScrollerFieldCursor
			{
				Record = APTR.FromPointer(0x30FF0),
				Field = MuiListviewCore.MuiListviewScrollerField.Magic,
			}, out _, out _));
		Assert.False(MuiListviewCore.MuiListviewScrollerMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListviewCore.MuiListviewScrollerField.Magic,
			out _, out _));
	}
}
