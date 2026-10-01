using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListOwnedRecordHeaderFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedOwnedRecordHeader()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.True(MuiListCore.MuiListOwnedRecordHeaderCodec.WriteRecord(
			ref platform, address,
			new MuiListCore.MuiListOwnedRecordHeader { Length = 48 }));
		var cursor = new MuiListCore.MuiListOwnedRecordHeaderFieldCursor
		{
			Record = address,
			Field = MuiListCore.MuiListOwnedRecordHeaderField.Length,
		};
		Assert.True(MuiListCore.MuiListOwnedRecordHeaderFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(address.Raw, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListOwnedRecordHeaderFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListOwnedRecordHeaderField.Length, 64));
		Assert.True(MuiListCore.MuiListOwnedRecordHeaderFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListOwnedRecordHeaderField.Length, out var length));
		Assert.Equal(64u, length);
		Assert.True(MuiListCore.MuiListOwnedRecordHeaderCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(64u, decoded.Length);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListOwnedRecordHeaderFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListOwnedRecordHeaderFieldCursor
				{
					Record = address,
					Field = (MuiListCore.MuiListOwnedRecordHeaderField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListOwnedRecordHeaderFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListOwnedRecordHeaderFieldCursor
				{
					Record = APTR.FromPointer(0x30FFD),
					Field = MuiListCore.MuiListOwnedRecordHeaderField.Length,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListOwnedRecordHeaderMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
				MuiListCore.MuiListOwnedRecordHeaderField.Length, out _, out _));
	}
}
