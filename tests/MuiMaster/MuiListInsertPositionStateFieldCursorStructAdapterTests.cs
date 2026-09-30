using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListInsertPositionStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedInsertPositionStateRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListInsertPositionState
		{
			Magic = MuiListCore.MuiListInsertPositionState.Cookie,
			Position = 7,
		};
		Assert.True(MuiListCore.MuiListInsertPositionStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListInsertPositionStateFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListInsertPositionStateField.Position,
		};
		Assert.True(MuiListCore.MuiListInsertPositionStateFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3504u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListInsertPositionStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListInsertPositionStateField.Position, 9));
		Assert.True(MuiListCore.MuiListInsertPositionStateFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListInsertPositionStateField.Magic, out var magic));
		Assert.Equal(value.Magic, magic);
		Assert.True(MuiListCore.MuiListInsertPositionStateCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(9u, decoded.Position);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListInsertPositionStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListInsertPositionStateFieldCursor
				{
					Address = address,
					Field = (MuiListCore.MuiListInsertPositionStateField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListInsertPositionStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListInsertPositionStateFieldCursor
				{
					Address = APTR.FromPointer(0x30FF9),
					Field = MuiListCore.MuiListInsertPositionStateField.Magic,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListInsertPositionStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
				MuiListCore.MuiListInsertPositionStateField.Magic, out _, out _));
	}
}
