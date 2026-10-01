using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListColumnGeometryFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedColumnGeometryRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListColumnGeometry
		{
			Offset = 12,
			Width = 96,
		};
		Assert.True(MuiListCore.MuiListColumnGeometryCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListColumnGeometryFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListColumnGeometryField.Width,
		};
		Assert.True(MuiListCore.MuiListColumnGeometryFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3504u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListColumnGeometryFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListColumnGeometryField.Width, 128));
		Assert.True(MuiListCore.MuiListColumnGeometryFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListColumnGeometryField.Offset, out var offset));
		Assert.Equal(12u, offset);
		Assert.True(MuiListCore.MuiListColumnGeometryCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(12u, decoded.Offset);
		Assert.Equal(128u, decoded.Width);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListColumnGeometryFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListColumnGeometryFieldCursor
				{
					Address = address,
					Field = (MuiListCore.MuiListColumnGeometryField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListColumnGeometryFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListColumnGeometryFieldCursor
				{
					Address = APTR.FromPointer(0x30FF9),
					Field = MuiListCore.MuiListColumnGeometryField.Offset,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListColumnGeometryMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListCore.MuiListColumnGeometryField.Offset,
			out _, out _));
	}
}
