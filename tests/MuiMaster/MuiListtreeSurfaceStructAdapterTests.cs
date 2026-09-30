using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreeSurfaceStructAdapterTests
{
	[Fact]
	public void SurfaceFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListtreeCore.MuiListtreeSurfaceStateRecord
		{
			Magic = MuiListtreeCore.MuiListtreeSurfaceStateRecord.Cookie,
			Left = -10,
			Top = 2,
			Width = 320,
			Height = 200,
			RowHeight = 16,
			FirstVisible = 4,
		};

		Assert.True(MuiListtreeCore.MuiListtreeSurfaceStateRecordCodec.WriteRecord(
			ref platform, address, value));
		var fieldCursor = new MuiListtreeCore.MuiListtreeSurfaceFieldCursor
		{
			Address = address,
			Field = MuiListtreeCore.MuiListtreeSurfaceField.Width,
		};
		Assert.True(MuiListtreeCore.MuiListtreeSurfaceFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var widthAddress));
		Assert.Equal(0x350Cu, widthAddress.Raw);
		Assert.True(MuiListtreeCore.MuiListtreeSurfaceMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeSurfaceField.Left,
			unchecked((uint)-20)));
		Assert.True(MuiListtreeCore.MuiListtreeSurfaceMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeSurfaceField.FirstVisible,
			9));
		Assert.True(MuiListtreeCore.MuiListtreeSurfaceMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeSurfaceField.Left,
			out var left));
		Assert.Equal(unchecked((uint)-20), left);
		Assert.True(MuiListtreeCore.MuiListtreeSurfaceStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(-20, decoded.Left);
		Assert.Equal(value.Top, decoded.Top);
		Assert.Equal(value.Width, decoded.Width);
		Assert.Equal(value.Height, decoded.Height);
		Assert.Equal(value.RowHeight, decoded.RowHeight);
		Assert.Equal(9u, decoded.FirstVisible);
		Assert.True(MuiListtreeCore.MuiListtreeSurfaceMemoryCodec.TryGetAddress(
			ref platform, address, MuiListtreeCore.MuiListtreeSurfaceField.FirstVisible,
			out var firstVisibleAddress));
		Assert.Equal(0x3518u, firstVisibleAddress.Raw);
	}

	[Fact]
	public void SurfaceAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListtreeCore.MuiListtreeSurfaceMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeSurfaceField)0xFF,
			out _));
		Assert.False(MuiListtreeCore.MuiListtreeSurfaceMemoryCodec.TryWriteUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeSurfaceField)0xFF,
			1));
		Assert.False(MuiListtreeCore.MuiListtreeSurfaceMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FF0),
			MuiListtreeCore.MuiListtreeSurfaceField.Height, out _));
		Assert.False(MuiListtreeCore.MuiListtreeSurfaceMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null, MuiListtreeCore.MuiListtreeSurfaceField.Magic,
			1));
	}
}
