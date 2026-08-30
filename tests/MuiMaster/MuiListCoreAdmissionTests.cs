using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListCoreAdmissionTests
{
	[Fact]
	public void ListCoreOwnerScrollerAndImageRecordsRoundTripThroughStructs()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var ownerAddress = APTR.FromPointer(0x2800);
		var scrollerAddress = APTR.FromPointer(0x2820);
		var imageAddress = APTR.FromPointer(0x2850);
		var owner = new MuiListviewOwnerState
		{
			Magic = MuiListviewOwnerState.Cookie,
			Owner = APTR.FromPointer(0x2900),
		};
		var scroller = new MuiListHScrollerState
		{
			Magic = MuiListHScrollerState.Cookie,
			Policy = 2,
			ContentWidth = 1200,
			ViewWidth = 320,
			Visible = 1,
			ScrollX = 40,
			MaxScrollX = 880,
		};
		var image = new MuiListImageState
		{
			Magic = MuiListImageState.Cookie,
			ImageObject = APTR.FromPointer(0x2940),
			Flags = 3,
			Next = APTR.FromPointer(0x2980),
		};
		Assert.True(MuiListviewOwnerStateCodec.Write(ref platform, ownerAddress,
			owner));
		Assert.True(MuiListHScrollerStateCodec.Write(ref platform, scrollerAddress,
			scroller));
		Assert.True(MuiListImageCodec.Write(ref platform, imageAddress, image));
		Assert.True(MuiListviewOwnerStateCodec.TryRead(ref platform, ownerAddress,
			out var readOwner));
		Assert.Equal(owner.Owner, readOwner.Owner);
		Assert.True(MuiListHScrollerStateCodec.TryRead(ref platform,
			scrollerAddress, out var readScroller));
		Assert.Equal(scroller.ContentWidth, readScroller.ContentWidth);
		Assert.Equal(scroller.MaxScrollX, readScroller.MaxScrollX);
		Assert.True(MuiListImageCodec.TryRead(ref platform, imageAddress,
			out var readImage));
		Assert.Equal(image.ImageObject, readImage.ImageObject);
		Assert.Equal(image.Next, readImage.Next);
		Assert.Equal(image.Flags, readImage.Flags);
	}

	[Fact]
	public void MalformedListCoreMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var ownerAddress = APTR.FromPointer(0x28A0);
		var scrollerAddress = APTR.FromPointer(0x28C0);
		var imageAddress = APTR.FromPointer(0x28F0);
		Assert.True(MuiListviewOwnerStateCodec.Write(ref platform, ownerAddress,
			new MuiListviewOwnerState
			{
				Magic = MuiListviewOwnerState.Cookie,
				Owner = APTR.FromPointer(0x2910),
			}));
		Assert.True(MuiListHScrollerStateCodec.Write(ref platform, scrollerAddress,
			new MuiListHScrollerState
			{
				Magic = MuiListHScrollerState.Cookie,
				ContentWidth = 900,
				ViewWidth = 300,
				MaxScrollX = 600,
			}));
		Assert.True(MuiListImageCodec.Write(ref platform, imageAddress,
			new MuiListImageState
			{
				Magic = MuiListImageState.Cookie,
				ImageObject = APTR.FromPointer(0x2920),
				Next = APTR.FromPointer(0x2930),
			}));
		Assert.True(MuiListviewOwnerStateFieldCursorCodec.TryWriteUInt32(
			ref platform, ownerAddress, MuiListviewOwnerStateField.Magic, 0));
		Assert.True(MuiListHScrollerStateFieldCursorCodec.TryWriteUInt32(
			ref platform, scrollerAddress, MuiListHScrollerStateField.Magic, 0));
		Assert.True(MuiListImageFieldCursorCodec.TryWriteUInt32(ref platform,
			imageAddress, MuiListImageField.Magic, 0));
		Assert.True(MuiListviewOwnerStateCodec.TryReadStructural(ref platform,
			ownerAddress, out var owner));
		Assert.Equal(0u, owner.Magic);
		Assert.False(MuiListviewOwnerStateCodec.TryRead(ref platform, ownerAddress,
			out _));
		Assert.True(MuiListHScrollerStateCodec.TryReadStructural(ref platform,
			scrollerAddress, out var scroller));
		Assert.Equal(0u, scroller.Magic);
		Assert.Equal(600u, scroller.MaxScrollX);
		Assert.False(MuiListHScrollerStateCodec.TryRead(ref platform,
			scrollerAddress, out _));
		Assert.True(MuiListImageCodec.TryReadStructural(ref platform, imageAddress,
			out var image));
		Assert.Equal(0u, image.Magic);
		Assert.Equal(APTR.FromPointer(0x2920), image.ImageObject);
		Assert.False(MuiListImageCodec.TryRead(ref platform, imageAddress, out _));
	}

	[Fact]
	public void ListviewOwnerSequentialRecordPreservesPointerAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2A00);
		var value = new MuiListviewOwnerState
		{
			Magic = MuiListviewOwnerState.Cookie,
			Owner = APTR.FromPointer(0xFFFFFFFFu),
		};

		Assert.True(MuiListviewOwnerStateCodec.WriteRecord(ref platform, address,
			value));
		Assert.True(MuiListviewOwnerStateCodec.TryReadRecord(ref platform, address,
			out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Owner, decoded.Owner);

		Assert.False(MuiListviewOwnerStateCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FF9), out _));
	}

	[Fact]
	public void ListviewHorizontalScrollerSequentialRecordPreservesRangeAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2A20);
		var value = new MuiListHScrollerState
		{
			Magic = MuiListHScrollerState.Cookie,
			Policy = 0x01020304u,
			ContentWidth = 0x11223344u,
			ViewWidth = 0x55667788u,
			Visible = 0x99AABBCCu,
			ScrollX = 0xDDEEFF00u,
			MaxScrollX = 0xFFFFFFFFu,
		};

		Assert.True(MuiListHScrollerStateCodec.WriteRecord(ref platform, address,
			value));
		Assert.True(MuiListHScrollerStateCodec.TryReadRecord(ref platform, address,
			out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Policy, decoded.Policy);
		Assert.Equal(value.ContentWidth, decoded.ContentWidth);
		Assert.Equal(value.ViewWidth, decoded.ViewWidth);
		Assert.Equal(value.Visible, decoded.Visible);
		Assert.Equal(value.ScrollX, decoded.ScrollX);
		Assert.Equal(value.MaxScrollX, decoded.MaxScrollX);

		Assert.False(MuiListHScrollerStateCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FE5), out _));
	}

	[Fact]
	public void ListImageSequentialRecordPreservesPointersFlagsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2A40);
		var value = new MuiListImageState
		{
			Magic = MuiListImageState.Cookie,
			ImageObject = APTR.FromPointer(uint.MaxValue),
			Flags = 0xAABBCCDDu,
			Next = APTR.FromPointer(0x01020304u),
		};

		Assert.True(MuiListImageCodec.WriteRecord(ref platform, address, value));
		Assert.True(MuiListImageCodec.TryReadRecord(ref platform, address,
			out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.ImageObject, decoded.ImageObject);
		Assert.Equal(value.Flags, decoded.Flags);
		Assert.Equal(value.Next, decoded.Next);

		var crossingEnd = APTR.FromPointer(0x30FF1);
		Assert.False(MuiListImageCodec.WriteRecord(ref platform, crossingEnd,
			value));
		Assert.False(MuiListImageCodec.TryReadRecord(ref platform, crossingEnd,
			out _));
	}
}
