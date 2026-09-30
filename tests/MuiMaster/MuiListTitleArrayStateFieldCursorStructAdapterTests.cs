using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListTitleArrayStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedTitleArrayRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListTitleArrayState
		{
			Magic = MuiListCore.MuiListTitleArrayState.Cookie,
			Pointers = APTR.FromPointer(0x4567),
			Count = 7,
		};
		Assert.True(MuiListCore.MuiListTitleArrayStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListTitleArrayStateFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListTitleArrayStateField.Count,
		};
		Assert.True(MuiListCore.MuiListTitleArrayStateFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3508u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListTitleArrayStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListTitleArrayStateField.Pointers, 0x0000789Au));
		Assert.True(MuiListCore.MuiListTitleArrayStateFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListTitleArrayStateField.Pointers, out var pointers));
		Assert.Equal(0x0000789Au, pointers);
		Assert.True(MuiListCore.MuiListTitleArrayStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(0x0000789Au, decoded.Pointers.Raw);
		Assert.Equal(value.Count, decoded.Count);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListTitleArrayStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListTitleArrayStateFieldCursor
				{
					Address = address,
					Field = (MuiListCore.MuiListTitleArrayStateField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListTitleArrayStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListTitleArrayStateFieldCursor
				{
					Address = APTR.FromPointer(0x30FF9),
					Field = MuiListCore.MuiListTitleArrayStateField.Magic,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListTitleArrayStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
			MuiListCore.MuiListTitleArrayStateField.Magic, out _, out _));
	}
}
