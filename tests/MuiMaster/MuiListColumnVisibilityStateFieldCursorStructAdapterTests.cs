using Amiga;
using CopperOS.MuiMaster;
using static CopperOS.MuiMaster.MuiListCore;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListColumnVisibilityStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedColumnVisibilityRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListColumnVisibilityState
		{
			Magic = MuiListColumnVisibilityState.Cookie,
			Low = 1,
			High = 2,
			Word2 = 3,
			Word3 = 4,
			Word4 = 5,
			Word5 = 6,
			Word6 = 7,
			Word7 = 8,
		};
		Assert.True(MuiListColumnVisibilityStateCodec.WriteRecord(ref platform,
			address, value));
		var cursor = new MuiListColumnVisibilityStateFieldCursor
		{
			Address = address,
			Field = MuiListColumnVisibilityStateField.Word7,
		};
		Assert.True(MuiListColumnVisibilityStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3520u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListColumnVisibilityStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiListColumnVisibilityStateField.Word4, 55));
		Assert.True(MuiListColumnVisibilityStateFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiListColumnVisibilityStateField.High,
			out var high));
		Assert.Equal(value.High, high);
		Assert.True(MuiListColumnVisibilityStateCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Low, decoded.Low);
		Assert.Equal(value.High, decoded.High);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Word3, decoded.Word3);
		Assert.Equal(55u, decoded.Word4);
		Assert.Equal(value.Word5, decoded.Word5);
		Assert.Equal(value.Word6, decoded.Word6);
		Assert.Equal(value.Word7, decoded.Word7);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListColumnVisibilityStateFieldCursorCodec.TryGetAddress(
			ref platform, new MuiListColumnVisibilityStateFieldCursor
			{
				Address = address,
				Field = (MuiListColumnVisibilityStateField)255,
			}, out _, out _));
		Assert.False(MuiListColumnVisibilityStateFieldCursorCodec.TryGetAddress(
			ref platform, new MuiListColumnVisibilityStateFieldCursor
			{
				Address = APTR.FromPointer(0x30FF9),
				Field = MuiListColumnVisibilityStateField.Magic,
			}, out _, out _));
		Assert.False(MuiListColumnVisibilityStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListColumnVisibilityStateField.Magic,
			out _, out _));
	}
}
