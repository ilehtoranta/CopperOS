using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreeColumnGeometryStructAdapterTests
{
	[Fact]
	public void ColumnGeometryFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListtreeCore.MuiListtreeColumnGeometryRecord
		{
			Width = 320,
			Delta = 4,
			Weight = 2,
			MinWidth = 40,
			MaxWidth = 640,
			Flags = MuiListtreeCore.MuiListtreeColumnGeometryRecord.MinContent,
		};

		Assert.True(MuiListtreeCore.MuiListtreeColumnGeometryCodec.Write(
			ref platform, address, value));
		var fieldCursor = new MuiListtreeCore.MuiListtreeColumnGeometryFieldCursor
		{
			Address = address,
			Field = MuiListtreeCore.MuiListtreeColumnGeometryField.Delta,
		};
		Assert.True(MuiListtreeCore.MuiListtreeColumnGeometryFieldCursorCodec
			.TryGetAddress(ref platform, fieldCursor, out var deltaAddress));
		Assert.Equal(0x3504u, deltaAddress.Raw);
		Assert.True(MuiListtreeCore.MuiListtreeColumnGeometryMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeColumnGeometryField.Width,
			400));
		Assert.True(MuiListtreeCore.MuiListtreeColumnGeometryMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeColumnGeometryField.Flags,
			MuiListtreeCore.MuiListtreeColumnGeometryRecord.MaxContent));
		Assert.True(MuiListtreeCore.MuiListtreeColumnGeometryMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeColumnGeometryField.Flags,
			out var flags));
		Assert.Equal(MuiListtreeCore.MuiListtreeColumnGeometryRecord.MaxContent, flags);
		Assert.True(MuiListtreeCore.MuiListtreeColumnGeometryCodec.TryRead(
			ref platform, address, out var decoded));
		Assert.Equal(400u, decoded.Width);
		Assert.Equal(value.Delta, decoded.Delta);
		Assert.Equal(value.Weight, decoded.Weight);
		Assert.Equal(value.MinWidth, decoded.MinWidth);
		Assert.Equal(value.MaxWidth, decoded.MaxWidth);
		Assert.Equal(MuiListtreeCore.MuiListtreeColumnGeometryRecord.MaxContent,
			decoded.Flags);
		Assert.True(MuiListtreeCore.MuiListtreeColumnGeometryMemoryCodec.TryGetAddress(
			ref platform, address, MuiListtreeCore.MuiListtreeColumnGeometryField.MaxWidth,
			out var maxWidthAddress));
		Assert.Equal(0x3510u, maxWidthAddress.Raw);
		var vector = APTR.FromPointer(0x3700);
		Assert.True(MuiListtreeCore.MuiListtreeColumnGeometryVectorMemoryCodec
			.TryGetEntry(ref platform, vector, 2, out var thirdAddress));
		Assert.Equal(0x3730u, thirdAddress.Raw);
	}

	[Fact]
	public void ColumnGeometryAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListtreeCore.MuiListtreeColumnGeometryMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeColumnGeometryField)0xFF,
			out _));
		Assert.False(MuiListtreeCore.MuiListtreeColumnGeometryMemoryCodec.TryWriteUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeColumnGeometryField)0xFF,
			1));
		Assert.False(MuiListtreeCore.MuiListtreeColumnGeometryMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FF0),
			MuiListtreeCore.MuiListtreeColumnGeometryField.MaxWidth, out _));
		Assert.False(MuiListtreeCore.MuiListtreeColumnGeometryMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null, MuiListtreeCore.MuiListtreeColumnGeometryField.Width,
			1));
		Assert.False(MuiListtreeCore.MuiListtreeColumnGeometryVectorMemoryCodec
			.TryGetEntry(ref platform, address,
				MuiListtreeCore.MuiListtreeColumnGeometryCursor.MaximumEntries, out _));
	}
}
