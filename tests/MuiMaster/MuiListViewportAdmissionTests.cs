using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListViewportAdmissionTests
{
	[Fact]
	public void ListViewportRecordRoundTripsThroughNamedStruct()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x32C0);
		var value = new MuiListCore.MuiListViewportState
		{
			Magic = MuiListCore.MuiListViewportState.Cookie,
			TopPixel = 64,
			VisiblePixel = 320,
			TotalPixel = 1024,
			First = 4,
			LineHeight = 16,
			Visible = 20,
			DropMark = 3,
		};
		Assert.True(MuiListCore.MuiListViewportStateCodec.Write(ref platform,
			address, value));
		Assert.True(MuiListCore.MuiListViewportStateCodec.TryRead(ref platform,
			address, out var read));
		Assert.Equal(value.Magic, read.Magic);
		Assert.Equal(value.TopPixel, read.TopPixel);
		Assert.Equal(value.VisiblePixel, read.VisiblePixel);
		Assert.Equal(value.TotalPixel, read.TotalPixel);
		Assert.Equal(value.First, read.First);
		Assert.Equal(value.LineHeight, read.LineHeight);
		Assert.Equal(value.Visible, read.Visible);
		Assert.Equal(value.DropMark, read.DropMark);
	}

	[Fact]
	public void MalformedListViewportMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x32E0);
		Assert.True(MuiListCore.MuiListViewportStateCodec.Write(ref platform,
			address, new MuiListCore.MuiListViewportState
			{
				Magic = MuiListCore.MuiListViewportState.Cookie,
				First = 2,
				LineHeight = 12,
				Visible = 8,
			}));
		Assert.True(MuiListCore.MuiListViewportStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListViewportStateField.Magic, 0));
		Assert.True(MuiListCore.MuiListViewportStateCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.Equal(2u, structural.First);
		Assert.Equal(12u, structural.LineHeight);
		Assert.False(MuiListCore.MuiListViewportStateCodec.TryRead(ref platform,
			address, out _));
		Assert.True(MuiListCore.MuiListViewportStateCodec.TryReadStorage(
			ref platform, address, out var storage));
		Assert.Equal(0u, storage.Magic);
	}
}
