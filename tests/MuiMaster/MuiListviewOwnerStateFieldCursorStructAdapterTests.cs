using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewOwnerStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedOwnerRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListviewOwnerState
		{
			Magic = MuiListviewOwnerState.Cookie,
			Owner = APTR.FromPointer(0x4567),
		};
		Assert.True(MuiListviewOwnerStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListviewOwnerStateFieldCursor
		{
			Record = address,
			Field = MuiListviewOwnerStateField.Owner,
		};
		Assert.True(MuiListviewOwnerStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3504u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListviewOwnerStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiListviewOwnerStateField.Owner,
			0x0000789Au));
		Assert.True(MuiListviewOwnerStateFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiListviewOwnerStateField.Owner,
			out var owner));
		Assert.Equal(0x0000789Au, owner);
		Assert.True(MuiListviewOwnerStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(0x0000789Au, decoded.Owner.Raw);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewOwnerStateFieldCursorCodec.TryGetAddress(
			ref platform,
			new MuiListviewOwnerStateFieldCursor
			{
				Record = address,
				Field = (MuiListviewOwnerStateField)255,
			}, out _, out _));
		Assert.False(MuiListviewOwnerStateFieldCursorCodec.TryGetAddress(
			ref platform,
			new MuiListviewOwnerStateFieldCursor
			{
				Record = APTR.FromPointer(0x30FF9),
				Field = MuiListviewOwnerStateField.Magic,
			}, out _, out _));
		Assert.False(MuiListviewOwnerStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListviewOwnerStateField.Magic,
			out _, out _));
	}
}
