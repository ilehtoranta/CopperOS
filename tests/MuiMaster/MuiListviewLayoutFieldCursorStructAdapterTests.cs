using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewLayoutFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedLayoutRecordAndPreservesSignedValues()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListviewCore.MuiListviewLayoutState
		{
			Magic = MuiListviewCore.MuiListviewLayoutState.Cookie,
			Left = -10,
			Top = 20,
			Width = 640,
			Height = 480,
			ChildLeft = -3,
			ChildTop = 4,
			ChildWidth = 600,
			ChildHeight = 400,
		};
		Assert.True(MuiListviewCore.MuiListviewLayoutStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListviewCore.MuiListviewLayoutFieldCursor
		{
			Record = address,
			Field = MuiListviewCore.MuiListviewLayoutField.ChildHeight,
		};
		Assert.True(MuiListviewCore.MuiListviewLayoutFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3520u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListviewCore.MuiListviewLayoutFieldCursorCodec.TryWriteInt32(
			ref platform, address, MuiListviewCore.MuiListviewLayoutField.Left, -99));
		Assert.True(MuiListviewCore.MuiListviewLayoutFieldCursorCodec.TryReadInt32(
			ref platform, address, MuiListviewCore.MuiListviewLayoutField.Left,
			out var left));
		Assert.Equal(-99, left);
		Assert.True(MuiListviewCore.MuiListviewLayoutStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(-99, decoded.Left);
		Assert.Equal(value.ChildHeight, decoded.ChildHeight);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewCore.MuiListviewLayoutFieldCursorCodec.TryGetAddress(
			ref platform,
			new MuiListviewCore.MuiListviewLayoutFieldCursor
			{
				Record = address,
				Field = (MuiListviewCore.MuiListviewLayoutField)255,
			}, out _, out _));
		Assert.False(MuiListviewCore.MuiListviewLayoutFieldCursorCodec.TryGetAddress(
			ref platform,
			new MuiListviewCore.MuiListviewLayoutFieldCursor
			{
				Record = APTR.FromPointer(0x30FE0),
				Field = MuiListviewCore.MuiListviewLayoutField.Magic,
			}, out _, out _));
		Assert.False(MuiListviewCore.MuiListviewLayoutMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListviewCore.MuiListviewLayoutField.Magic,
			out _, out _));
	}
}
