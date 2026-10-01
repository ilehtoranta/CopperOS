using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListImageFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedListImageRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListImageState
		{
			Magic = MuiListImageState.Cookie,
			ImageObject = APTR.FromPointer(0x4567),
			Flags = 3,
			Next = APTR.FromPointer(0x4890),
		};
		Assert.True(MuiListImageCodec.WriteRecord(ref platform, address, value));
		var cursor = new MuiListImageFieldCursor
		{
			Address = address,
			Field = MuiListImageField.Next,
		};
		Assert.True(MuiListImageFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x350Cu, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListImageFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiListImageField.Flags, 0xAABBCCDDu));
		Assert.True(MuiListImageFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiListImageField.Flags, out var flags));
		Assert.Equal(0xAABBCCDDu, flags);
		Assert.True(MuiListImageCodec.TryReadStructural(ref platform, address,
			out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.ImageObject, decoded.ImageObject);
		Assert.Equal(0xAABBCCDDu, decoded.Flags);
		Assert.Equal(value.Next, decoded.Next);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListImageFieldCursorCodec.TryGetAddress(ref platform,
			new MuiListImageFieldCursor
			{
				Address = address,
				Field = (MuiListImageField)255,
			}, out _, out _));
		Assert.False(MuiListImageFieldCursorCodec.TryGetAddress(ref platform,
			new MuiListImageFieldCursor
			{
				Address = APTR.FromPointer(0x30FF9),
				Field = MuiListImageField.Magic,
			}, out _, out _));
		Assert.False(MuiListImageMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiListImageField.Magic, out _, out _));
	}
}
