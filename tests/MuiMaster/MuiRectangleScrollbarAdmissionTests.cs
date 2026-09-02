using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiRectangleScrollbarAdmissionTests
{
	[Fact]
	public void RectangleAndScrollbarRecordsRoundTripThroughNamedStructs()
	{
		var platform = CreatePlatform();
		var rectangleAddress = APTR.FromPointer(0x1500);
		var scrollbarAddress = APTR.FromPointer(0x1530);
		var rectangle = new MuiRectanglePresentationStateRecord
		{
			Magic = MuiRectanglePresentationStateRecord.Cookie,
			HorizontalBar = 1,
			VerticalBar = 0,
		};
		var scrollbar = new MuiScrollbarLayoutStateRecord
		{
			Magic = MuiScrollbarLayoutStateRecord.Cookie,
			Horizontal = 1,
			Type = 3,
		};
		Assert.True(MuiRectanglePresentationStateRecordCodec.WriteRecord(ref platform,
			rectangleAddress, rectangle));
		Assert.True(MuiScrollbarLayoutStateRecordCodec.Write(ref platform,
			scrollbarAddress, scrollbar));
		Assert.True(MuiRectanglePresentationStateRecordCodec.TryRead(ref platform,
			rectangleAddress, out var rectangleRead));
		Assert.True(MuiScrollbarLayoutStateRecordCodec.TryRead(ref platform,
			scrollbarAddress, out var scrollbarRead));
		Assert.Equal(rectangle.HorizontalBar, rectangleRead.HorizontalBar);
		Assert.Equal(rectangle.VerticalBar, rectangleRead.VerticalBar);
		Assert.Equal(scrollbar.Horizontal, scrollbarRead.Horizontal);
		Assert.Equal(scrollbar.Type, scrollbarRead.Type);
	}

	[Fact]
	public void MalformedRectangleAndScrollbarMagicRemainStructuralButFailClosed()
	{
		var platform = CreatePlatform();
		var rectangleAddress = APTR.FromPointer(0x1600);
		var scrollbarAddress = APTR.FromPointer(0x1630);
		Assert.True(MuiRectanglePresentationStateRecordCodec.WriteRecord(ref platform,
			rectangleAddress, new MuiRectanglePresentationStateRecord
			{
				Magic = MuiRectanglePresentationStateRecord.Cookie,
				HorizontalBar = 0,
				VerticalBar = 1,
			}));
		Assert.True(MuiScrollbarLayoutStateRecordCodec.Write(ref platform,
			scrollbarAddress, new MuiScrollbarLayoutStateRecord
			{
				Magic = MuiScrollbarLayoutStateRecord.Cookie,
				Horizontal = 0,
				Type = 0,
			}));
		Assert.True(MuiRectanglePresentationStateFieldCursorCodec.TryWriteUInt32(
			ref platform, rectangleAddress,
			MuiRectanglePresentationStateField.Magic, 0));
		Assert.True(MuiScrollbarLayoutStateFieldCursorCodec.TryWriteUInt32(
			ref platform, scrollbarAddress, MuiScrollbarLayoutStateField.Magic, 0));
		Assert.True(MuiRectanglePresentationStateRecordCodec.TryReadRecord(
			ref platform, rectangleAddress, out var rectangle));
		Assert.True(MuiScrollbarLayoutStateRecordCodec.TryReadStructural(ref platform,
			scrollbarAddress, out var scrollbar));
		Assert.Equal(0u, rectangle.Magic);
		Assert.Equal(0u, scrollbar.Magic);
		Assert.False(MuiRectanglePresentationStateRecordCodec.TryRead(ref platform,
			rectangleAddress, out _));
		Assert.False(MuiScrollbarLayoutStateRecordCodec.TryRead(ref platform,
			scrollbarAddress, out _));
		Assert.False(MuiRectanglePresentationStateAdmission.Validate(rectangle));
		Assert.False(MuiScrollbarLayoutStateAdmission.Validate(scrollbar));
	}

	[Fact]
	public void RectanglePresentationRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1A40);
		var record = new MuiRectanglePresentationStateRecord
		{
			Magic = MuiRectanglePresentationStateRecord.Cookie,
			HorizontalBar = 1,
			VerticalBar = 0,
		};
		Assert.True(MuiRectanglePresentationStateRecordCodec.WriteRecord(ref platform,
			address, record));
		Assert.True(MuiRectanglePresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiRectanglePresentationStateField.VerticalBar,
			out var typedVerticalAddress));
		Assert.Equal(0x1A48u, typedVerticalAddress.Raw);
		Assert.True(MuiRectanglePresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiRectanglePresentationStateField.HorizontalBar,
			out var typedHorizontal));
		Assert.Equal(1u, typedHorizontal);
		Assert.True(MuiRectanglePresentationStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiRectanglePresentationStateField.HorizontalBar, 0));
		Assert.True(MuiRectanglePresentationStateRecordCodec.TryReadStructural(
			ref platform, address, out var typedUpdated));
		Assert.Equal(0u, typedUpdated.HorizontalBar);
		Assert.Equal(record.VerticalBar, typedUpdated.VerticalBar);
		Assert.True(MuiRectanglePresentationStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiRectanglePresentationStateField.HorizontalBar, 1));
		Assert.False(MuiRectanglePresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiRectanglePresentationStateField)0xFF, out _));
		Assert.True(MuiRectanglePresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 8, out var verticalAddress));
		Assert.Equal(0x1A48u, verticalAddress.Raw);
		Assert.True(MuiRectanglePresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out var horizontal));
		Assert.Equal(1u, horizontal);
		Assert.True(MuiRectanglePresentationStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 4, 0));
		Assert.True(MuiRectanglePresentationStateRecordCodec.TryReadRecord(
			ref platform, address, out var updated));
		Assert.Equal(0u, updated.HorizontalBar);
		Assert.False(MuiRectanglePresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiRectanglePresentationStateRecord.Size, out _));
		Assert.False(MuiRectanglePresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, (uint)0, out _));
		Assert.False(MuiRectanglePresentationStateRecordCodec.TryReadRecord(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void ScrollbarLayoutRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1A60);
		var record = new MuiScrollbarLayoutStateRecord
		{
			Magic = MuiScrollbarLayoutStateRecord.Cookie,
			Horizontal = 1,
			Type = 3,
		};
		Assert.True(MuiScrollbarLayoutStateRecordCodec.Write(ref platform, address,
			record));
		Assert.True(MuiScrollbarLayoutStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 8, out var typeAddress));
		Assert.Equal(0x1A68u, typeAddress.Raw);
		Assert.True(MuiScrollbarLayoutStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out var horizontal));
		Assert.Equal(1u, horizontal);
		Assert.True(MuiScrollbarLayoutStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 8, 0));
		Assert.True(MuiScrollbarLayoutStateRecordCodec.TryReadStructural(ref platform,
			address, out var updated));
		Assert.Equal(0u, updated.Type);
		Assert.True(MuiScrollbarLayoutStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiScrollbarLayoutStateField.Horizontal,
			out var typedHorizontal));
		Assert.Equal(address.Raw + MuiScrollbarLayoutStateRecord.HorizontalOffset,
			typedHorizontal.Raw);
		Assert.True(MuiScrollbarLayoutStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiScrollbarLayoutStateField.Type, 2));
		Assert.True(MuiScrollbarLayoutStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiScrollbarLayoutStateField.Magic,
			out var typedMagic));
		Assert.Equal(record.Magic, typedMagic);
		Assert.True(MuiScrollbarLayoutStateRecordCodec.TryReadStructural(ref platform,
			address, out var typedUpdated));
		Assert.Equal(2u, typedUpdated.Type);
		Assert.Equal(updated.Horizontal, typedUpdated.Horizontal);
		Assert.False(MuiScrollbarLayoutStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiScrollbarLayoutStateField)0xFF, out _));
		Assert.False(MuiScrollbarLayoutStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiScrollbarLayoutStateRecord.Size, out _));
		Assert.False(MuiScrollbarLayoutStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, (uint)0, out _));
		Assert.False(MuiScrollbarLayoutStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform() =>
		new(0x1000, 0x20000, 0x4000, APTR.FromPointer(0x1000));
}
