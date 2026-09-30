using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListColumnLayoutFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedColumnLayoutRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListColumnLayoutState
		{
			Magic = MuiListCore.MuiListColumnLayoutState.Cookie,
			Width = 320,
			Columns = 4,
			Values = APTR.FromPointer(0x4567),
		};
		Assert.True(MuiListCore.MuiListColumnLayoutStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListColumnLayoutFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListColumnLayoutField.Columns,
		};
		Assert.True(MuiListCore.MuiListColumnLayoutFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3508u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListColumnLayoutFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListColumnLayoutField.Columns, 6));
		Assert.True(MuiListCore.MuiListColumnLayoutFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListColumnLayoutField.Width, out var width));
		Assert.Equal(320u, width);
		Assert.True(MuiListCore.MuiListColumnLayoutStateCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Width, decoded.Width);
		Assert.Equal(6u, decoded.Columns);
		Assert.Equal(value.Values, decoded.Values);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListColumnLayoutFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListColumnLayoutFieldCursor
				{
					Address = address,
					Field = (MuiListCore.MuiListColumnLayoutField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListColumnLayoutFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListColumnLayoutFieldCursor
				{
					Address = APTR.FromPointer(0x30FF9),
					Field = MuiListCore.MuiListColumnLayoutField.Magic,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListColumnLayoutMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListCore.MuiListColumnLayoutField.Magic,
			out _, out _));
	}
}
