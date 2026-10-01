using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListTitleStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedTitleRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListTitleState
		{
			Magic = MuiListCore.MuiListTitleState.Cookie,
			Value = 0x00004567u,
		};
		Assert.True(MuiListCore.MuiListTitleStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListTitleStateFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListTitleStateField.Value,
		};
		Assert.True(MuiListCore.MuiListTitleStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3504u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListTitleStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListTitleStateField.Value,
			0x0000789Au));
		Assert.True(MuiListCore.MuiListTitleStateFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiListCore.MuiListTitleStateField.Value,
			out var titleValue));
		Assert.Equal(0x0000789Au, titleValue);
		Assert.True(MuiListCore.MuiListTitleStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(0x0000789Au, decoded.Value);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListTitleStateFieldCursorCodec.TryGetAddress(
			ref platform,
			new MuiListCore.MuiListTitleStateFieldCursor
			{
				Address = address,
				Field = (MuiListCore.MuiListTitleStateField)255,
			}, out _, out _));
		Assert.False(MuiListCore.MuiListTitleStateFieldCursorCodec.TryGetAddress(
			ref platform,
			new MuiListCore.MuiListTitleStateFieldCursor
			{
				Address = APTR.FromPointer(0x30FF9),
				Field = MuiListCore.MuiListTitleStateField.Magic,
			}, out _, out _));
		Assert.False(MuiListCore.MuiListTitleStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListCore.MuiListTitleStateField.Magic,
			out _, out _));
	}
}
