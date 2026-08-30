using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreeVectorStructAdapterTests
{
	[Fact]
	public void DisplayColumnVectorMemoryAdapterOwnsNamedRecordBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x5000,
			APTR.FromPointer(0x1000));
		var vector = APTR.FromPointer(0x2400);

		Assert.True(MuiListtreeCore.MuiListtreeDisplayColumnVectorMemoryCodec
			.TryGetEntry(ref platform, vector, 255, out var address));
		Assert.Equal(APTR.FromPointer(0x27FC), address);
		var expected = new MuiListtreeCore.MuiListtreeDisplayColumnRecord
		{
			Text = APTR.FromPointer(0x2A00)
		};
		Assert.True(MuiListtreeCore.MuiListtreeDisplayColumnCodec.Write(
			ref platform, address, expected));
		Assert.True(MuiListtreeCore.MuiListtreeDisplayColumnCodec.TryRead(
			ref platform, address, out var actual));
		Assert.Equal(expected.Text, actual.Text);

		var cursor = default(MuiListtreeCore.MuiListtreeDisplayColumnCursor);
		cursor.Base = vector;
		cursor.Index = 1;
		Assert.True(MuiListtreeCore.MuiListtreeDisplayColumnCursorCodec
			.TryGetEntry(ref platform, cursor, out var cursorAddress));
		Assert.Equal(APTR.FromPointer(0x2404), cursorAddress);
		Assert.False(MuiListtreeCore.MuiListtreeDisplayColumnVectorMemoryCodec
			.TryGetEntry(ref platform, vector,
				MuiListtreeCore.MuiListtreeDisplayColumnCursor.MaximumEntries, out _));
		Assert.False(MuiListtreeCore.MuiListtreeDisplayColumnVectorMemoryCodec
			.TryGetEntry(ref platform, APTR.FromPointer(0x40FFE), 0, out _));
		Assert.False(MuiListtreeCore.MuiListtreeDisplayColumnVectorMemoryCodec
			.TryGetEntry(ref platform, APTR.FromPointer(0xFFFFFFFE), 1, out _));
	}

	[Fact]
	public void DisplayColumnVectorBridgeUsesNamedTextRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x5000,
			APTR.FromPointer(0x1000));
		var vector = APTR.FromPointer(0x2400);
		var expected = new MuiListtreeCore.MuiListtreeDisplayColumnRecord
		{
			Text = APTR.FromPointer(0xFEDCBA98u),
		};

		Assert.True(MuiListtreeCore.MuiListtreeDisplayColumnVectorCodec.TryWrite(
			ref platform, vector, 2, expected));
		Assert.True(MuiListtreeCore.MuiListtreeDisplayColumnVectorCodec.TryRead(
			ref platform, vector, 2, out var decoded));
		Assert.Equal(expected.Text.Raw, decoded.Text.Raw);
		Assert.True(MuiListtreeCore.MuiListtreeDisplayColumnVectorCodec
			.TryWriteTextValue(ref platform, vector, 3, 0x80000001u));
		Assert.True(MuiListtreeCore.MuiListtreeDisplayColumnVectorCodec
			.TryReadTextValue(ref platform, vector, 3, out var rawText));
		Assert.Equal(0x80000001u, rawText);
		Assert.False(MuiListtreeCore.MuiListtreeDisplayColumnVectorCodec
			.TryReadTextValue(ref platform, vector,
				MuiListtreeCore.MuiListtreeDisplayColumnCursor.MaximumEntries, out _));
		Assert.False(MuiListtreeCore.MuiListtreeDisplayColumnVectorCodec
			.TryWriteTextValue(ref platform, APTR.FromPointer(0xFFFFFFFE), 1,
				expected.Text.Raw));
	}

	[Fact]
	public void DisplayColumnMemoryAdapterUsesNamedTextField()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x5000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2800);
		Assert.True(MuiListtreeCore.MuiListtreeDisplayColumnMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeDisplayColumnField.Text, 0x2A00u));
		Assert.True(MuiListtreeCore.MuiListtreeDisplayColumnMemoryCodec
			.TryReadUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeDisplayColumnField.Text, out var text));
		Assert.Equal(0x2A00u, text);
		Assert.True(MuiListtreeCore.MuiListtreeDisplayColumnMemoryCodec
			.TryGetAddress(ref platform, address,
				MuiListtreeCore.MuiListtreeDisplayColumnField.Text,
				out var textAddress));
		Assert.Equal(0x2800u, textAddress.Raw);
		Assert.False(MuiListtreeCore.MuiListtreeDisplayColumnMemoryCodec
			.TryGetAddress(ref platform, address,
				(MuiListtreeCore.MuiListtreeDisplayColumnField)255, out _));
	}

	[Fact]
	public void ColumnGeometryVectorMemoryAdapterOwnsNamedRecordBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x5000,
			APTR.FromPointer(0x1000));
		var vector = APTR.FromPointer(0x2400);

		Assert.True(MuiListtreeCore.MuiListtreeColumnGeometryVectorMemoryCodec
			.TryGetEntry(ref platform, vector, 255, out var address));
		Assert.Equal(APTR.FromPointer(0x3BE8), address);
		var expected = new MuiListtreeCore.MuiListtreeColumnGeometryRecord
		{
			Width = 320,
			Delta = 4,
			Weight = 100,
			MinWidth = 16,
			MaxWidth = 640,
			Flags = MuiListtreeCore.MuiListtreeColumnGeometryRecord.MinPixel
		};
		Assert.True(MuiListtreeCore.MuiListtreeColumnGeometryCodec.Write(
			ref platform, address, expected));
		Assert.True(MuiListtreeCore.MuiListtreeColumnGeometryCodec.TryRead(
			ref platform, address, out var actual));
		Assert.Equal(expected.Width, actual.Width);
		Assert.Equal(expected.Delta, actual.Delta);
		Assert.Equal(expected.Weight, actual.Weight);
		Assert.Equal(expected.MinWidth, actual.MinWidth);
		Assert.Equal(expected.MaxWidth, actual.MaxWidth);
		Assert.Equal(expected.Flags, actual.Flags);
		Assert.True(MuiListtreeCore.MuiListtreeColumnGeometryFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeColumnGeometryField.Delta, 8));
		Assert.True(MuiListtreeCore.MuiListtreeColumnGeometryFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeColumnGeometryField.Delta, out var delta));
		Assert.Equal(8u, delta);
		Assert.False(MuiListtreeCore.MuiListtreeColumnGeometryFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				unchecked((MuiListtreeCore.MuiListtreeColumnGeometryField)255),
				out _));

		var cursor = default(MuiListtreeCore.MuiListtreeColumnGeometryCursor);
		cursor.Base = vector;
		cursor.Index = 1;
		Assert.True(MuiListtreeCore.MuiListtreeColumnGeometryCursorCodec
			.TryGetEntry(ref platform, cursor, out var cursorAddress));
		Assert.Equal(APTR.FromPointer(0x2418), cursorAddress);
		Assert.False(MuiListtreeCore.MuiListtreeColumnGeometryVectorMemoryCodec
			.TryGetEntry(ref platform, vector,
				MuiListtreeCore.MuiListtreeColumnGeometryCursor.MaximumEntries, out _));
		Assert.False(MuiListtreeCore.MuiListtreeColumnGeometryVectorMemoryCodec
			.TryGetEntry(ref platform, APTR.FromPointer(0x40FFE), 0, out _));
		Assert.False(MuiListtreeCore.MuiListtreeColumnGeometryVectorMemoryCodec
			.TryGetEntry(ref platform, APTR.FromPointer(0xFFFFFFF0), 1, out _));
	}

	[Fact]
	public void ColumnGeometryVectorBridgeUsesNamedRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x5000,
			APTR.FromPointer(0x1000));
		var vector = APTR.FromPointer(0x2400);
		var expected = new MuiListtreeCore.MuiListtreeColumnGeometryRecord
		{
			Width = 0xFEDCBA98u,
			Delta = 4,
			Weight = 100,
			MinWidth = 16,
			MaxWidth = 0x80000001u,
			Flags = MuiListtreeCore.MuiListtreeColumnGeometryRecord.MaxContent,
		};

		Assert.True(MuiListtreeCore.MuiListtreeColumnGeometryVectorCodec.TryWrite(
			ref platform, vector, 2, expected));
		Assert.True(MuiListtreeCore.MuiListtreeColumnGeometryVectorCodec.TryRead(
			ref platform, vector, 2, out var decoded));
		Assert.Equal(expected.Width, decoded.Width);
		Assert.Equal(expected.Delta, decoded.Delta);
		Assert.Equal(expected.Weight, decoded.Weight);
		Assert.Equal(expected.MinWidth, decoded.MinWidth);
		Assert.Equal(expected.MaxWidth, decoded.MaxWidth);
		Assert.Equal(expected.Flags, decoded.Flags);
		Assert.False(MuiListtreeCore.MuiListtreeColumnGeometryVectorCodec.TryRead(
			ref platform, vector,
			MuiListtreeCore.MuiListtreeColumnGeometryCursor.MaximumEntries, out _));
		Assert.False(MuiListtreeCore.MuiListtreeColumnGeometryVectorCodec.TryWrite(
			ref platform, APTR.FromPointer(0xFFFFFFF0), 1, expected));
	}

	[Fact]
	public void ColumnGeometryMemoryAdapterUsesNamedRecordFields()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x5000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3C00);
		Assert.True(MuiListtreeCore.MuiListtreeColumnGeometryMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeColumnGeometryField.Width, 640));
		Assert.True(MuiListtreeCore.MuiListtreeColumnGeometryMemoryCodec
			.TryReadUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeColumnGeometryField.Width, out var width));
		Assert.Equal(640u, width);
		Assert.True(MuiListtreeCore.MuiListtreeColumnGeometryMemoryCodec
			.TryGetAddress(ref platform, address,
				MuiListtreeCore.MuiListtreeColumnGeometryField.Flags,
				out var flagsAddress));
		Assert.Equal(0x3C14u, flagsAddress.Raw);
		Assert.False(MuiListtreeCore.MuiListtreeColumnGeometryMemoryCodec
			.TryGetAddress(ref platform, address,
				(MuiListtreeCore.MuiListtreeColumnGeometryField)255, out _));
	}
}
