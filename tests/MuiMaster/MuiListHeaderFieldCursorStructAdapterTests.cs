using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListHeaderFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedHeaderRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListHeaderState
		{
			Magic = MuiListHeaderState.Cookie,
			Index = APTR.FromPointer(0x1234),
			Capacity = 8,
			Count = 3,
			Images = APTR.FromPointer(0x5678),
		};
		Assert.True(MuiListHeaderCodec.WriteRecord(ref platform, address, value));
		var cursor = new MuiListHeaderFieldCursor
		{
			Address = address,
			Field = MuiListHeaderField.Images,
		};
		Assert.True(MuiListHeaderFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3510u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListHeaderFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiListHeaderField.Count, 6));
		Assert.True(MuiListHeaderFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiListHeaderField.Index, out var index));
		Assert.Equal(value.Index.Raw, index);
		Assert.True(MuiListHeaderCodec.TryReadRecord(ref platform, address,
			out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Index.Raw, decoded.Index.Raw);
		Assert.Equal(value.Capacity, decoded.Capacity);
		Assert.Equal(6u, decoded.Count);
		Assert.Equal(value.Images.Raw, decoded.Images.Raw);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListHeaderFieldCursorCodec.TryGetAddress(ref platform,
			new MuiListHeaderFieldCursor
			{
				Address = address,
				Field = (MuiListHeaderField)255,
			}, out _, out _));
		Assert.False(MuiListHeaderFieldCursorCodec.TryGetAddress(ref platform,
			new MuiListHeaderFieldCursor
			{
				Address = APTR.FromPointer(0x30FF9),
				Field = MuiListHeaderField.Magic,
			}, out _, out _));
		Assert.False(MuiListHeaderMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiListHeaderField.Magic, out _, out _));
	}
}
