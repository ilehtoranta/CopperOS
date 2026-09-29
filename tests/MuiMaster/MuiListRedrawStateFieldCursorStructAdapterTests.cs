using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListRedrawStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedRedrawStateRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListRedrawState
		{
			Magic = MuiListCore.MuiListRedrawState.Cookie,
			Dirty = 1,
			Requests = 9,
		};
		Assert.True(MuiListCore.MuiListRedrawStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListRedrawStateFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListRedrawStateField.Requests,
		};
		Assert.True(MuiListCore.MuiListRedrawStateFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3508u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListRedrawStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListRedrawStateField.Requests, 12));
		Assert.True(MuiListCore.MuiListRedrawStateFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListRedrawStateField.Dirty, out var dirty));
		Assert.Equal(1u, dirty);
		Assert.True(MuiListCore.MuiListRedrawStateCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Dirty, decoded.Dirty);
		Assert.Equal(12u, decoded.Requests);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListRedrawStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListRedrawStateFieldCursor
				{
					Address = address,
					Field = (MuiListCore.MuiListRedrawStateField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListRedrawStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListRedrawStateFieldCursor
				{
					Address = APTR.FromPointer(0x30FF9),
					Field = MuiListCore.MuiListRedrawStateField.Magic,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListRedrawStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListCore.MuiListRedrawStateField.Magic,
			out _, out _));
	}
}
