using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListHeaderEditAdmissionTests
{
	[Fact]
	public void ListHeaderAndEditRecordsRoundTripThroughNamedStructs()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var headerAddress = APTR.FromPointer(0x2A00);
		var editAddress = APTR.FromPointer(0x2A40);
		var header = new MuiListHeaderState
		{
			Magic = MuiListHeaderState.Cookie,
			Index = APTR.FromPointer(0x2B00),
			Capacity = 32,
			Count = 7,
			Images = APTR.FromPointer(0x2C00),
		};
		var edit = new MuiListCore.MuiListEditState
		{
			Magic = 0x4C454449u,
			Row = -2,
			Column = 3,
			Entry = APTR.FromPointer(0x2D00),
			EditObject = APTR.FromPointer(0x2E00),
			Flags = 1,
		};
		Assert.True(MuiListHeaderCodec.Write(ref platform, headerAddress, header));
		Assert.True(MuiListCore.MuiListEditStateCodec.Write(ref platform,
			editAddress, edit));
		Assert.True(MuiListHeaderCodec.TryRead(ref platform, headerAddress,
			out var readHeader));
		Assert.Equal(header.Index, readHeader.Index);
		Assert.Equal(header.Count, readHeader.Count);
		Assert.Equal(header.Images, readHeader.Images);
		Assert.True(MuiListCore.MuiListEditStateCodec.TryRead(ref platform,
			editAddress, out var readEdit));
		Assert.Equal(edit.Row, readEdit.Row);
		Assert.Equal(edit.Column, readEdit.Column);
		Assert.Equal(edit.EditObject, readEdit.EditObject);
		Assert.Equal(edit.Flags, readEdit.Flags);
	}

	[Fact]
	public void MalformedListHeaderAndEditMagicRemainStructuralButFailClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var headerAddress = APTR.FromPointer(0x2A80);
		var editAddress = APTR.FromPointer(0x2AC0);
		Assert.True(MuiListHeaderCodec.Write(ref platform, headerAddress,
			new MuiListHeaderState
			{
				Magic = MuiListHeaderState.Cookie,
				Index = APTR.FromPointer(0x2B80),
				Capacity = 16,
				Count = 2,
			}));
		Assert.True(MuiListCore.MuiListEditStateCodec.Write(ref platform,
			editAddress, new MuiListCore.MuiListEditState
			{
				Magic = 0x4C454449u,
				Row = 4,
				Column = 5,
			}));
		Assert.True(MuiListHeaderFieldCursorCodec.TryWriteUInt32(ref platform,
			headerAddress, MuiListHeaderField.Magic, 0));
		Assert.True(MuiListCore.MuiListEditFieldCursorCodec.TryWriteUInt32(
			ref platform, editAddress, MuiListCore.MuiListEditField.Magic, 0));
		Assert.True(MuiListHeaderCodec.TryReadStructural(ref platform,
			headerAddress, out var header));
		Assert.Equal(0u, header.Magic);
		Assert.Equal(2u, header.Count);
		Assert.False(MuiListHeaderCodec.TryRead(ref platform, headerAddress,
			out _));
		Assert.True(MuiListCore.MuiListEditStateCodec.TryReadStructural(
			ref platform, editAddress, out var edit));
		Assert.Equal(0u, edit.Magic);
		Assert.Equal(4, edit.Row);
		Assert.False(MuiListCore.MuiListEditStateCodec.TryRead(ref platform,
			editAddress, out _));
	}

	[Fact]
	public void EditRecordSequentialCodecPreservesSignedLongsPointersAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2B00);
		var value = new MuiListCore.MuiListEditState
		{
			Magic = 0x4C454449u,
			Row = -123456,
			Column = int.MinValue + 7,
			Entry = APTR.FromPointer(uint.MaxValue),
			EditObject = APTR.FromPointer(0x01020304u),
			Flags = 0xAABBCCDDu,
		};

		Assert.True(MuiListCore.MuiListEditStateCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiListCore.MuiListEditStateCodec.TryReadRecord(ref platform,
			address, out var read));
		Assert.Equal(value.Magic, read.Magic);
		Assert.Equal(value.Row, read.Row);
		Assert.Equal(value.Column, read.Column);
		Assert.Equal(value.Entry, read.Entry);
		Assert.Equal(value.EditObject, read.EditObject);
		Assert.Equal(value.Flags, read.Flags);

		var crossingEnd = APTR.FromPointer(0x30FE9);
		Assert.False(MuiListCore.MuiListEditStateCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiListCore.MuiListEditStateCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}

	[Fact]
	public void ListHeaderSequentialCodecPreservesPointersAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2B80);
		var value = new MuiListHeaderState
		{
			Magic = MuiListHeaderState.Cookie,
			Index = APTR.FromPointer(uint.MaxValue),
			Capacity = 0x10203040u,
			Count = 0xAABBCCDDu,
			Images = APTR.FromPointer(0x01020304u),
		};

		Assert.True(MuiListHeaderCodec.WriteRecord(ref platform, address, value));
		Assert.True(MuiListHeaderCodec.TryReadRecord(ref platform, address,
			out var read));
		Assert.Equal(value.Magic, read.Magic);
		Assert.Equal(value.Index, read.Index);
		Assert.Equal(value.Capacity, read.Capacity);
		Assert.Equal(value.Count, read.Count);
		Assert.Equal(value.Images, read.Images);

		var crossingEnd = APTR.FromPointer(0x30FED);
		Assert.False(MuiListHeaderCodec.WriteRecord(ref platform, crossingEnd,
			value));
		Assert.False(MuiListHeaderCodec.TryReadRecord(ref platform, crossingEnd,
			out _));
	}
}
