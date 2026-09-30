using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListActiveStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedActiveStateRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListActiveState
		{
			Magic = MuiListCore.MuiListActiveState.Cookie,
			HasActive = 1,
			Active = 7,
		};
		Assert.True(MuiListCore.MuiListActiveStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListActiveStateFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListActiveStateField.Active,
		};
		Assert.True(MuiListCore.MuiListActiveStateFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3508u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListActiveStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListActiveStateField.Active, 9));
		Assert.True(MuiListCore.MuiListActiveStateFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListActiveStateField.HasActive, out var present));
		Assert.Equal(1u, present);
		Assert.True(MuiListCore.MuiListActiveStateCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.HasActive, decoded.HasActive);
		Assert.Equal(9u, decoded.Active);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListActiveStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListActiveStateFieldCursor
				{
					Address = address,
					Field = (MuiListCore.MuiListActiveStateField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListActiveStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListActiveStateFieldCursor
				{
					Address = APTR.FromPointer(0x30FF9),
					Field = MuiListCore.MuiListActiveStateField.Magic,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListActiveStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListCore.MuiListActiveStateField.Magic,
			out _, out _));
	}
}
