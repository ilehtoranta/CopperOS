using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiImageFontMatchStateAdmissionTests
{
	[Fact]
	public void ImageFontMatchStateRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1BC0);
		var value = new MuiImageFontMatchStateRecord
		{
			Magic = MuiImageFontMatchStateRecord.Cookie,
			Match = uint.MaxValue,
			Height = 0x80000000u,
			Width = uint.MaxValue,
		};

		Assert.True(MuiImageFontMatchStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiImageFontMatchStateRecordCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.Match, structural.Match);
		Assert.Equal(value.Height, structural.Height);
		Assert.Equal(value.Width, structural.Width);
		Assert.True(MuiImageFontMatchStateRecordCodec.TryRead(ref platform,
			address, out _));
		Assert.True(MuiImageFontMatchStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 12, out var widthField));
		Assert.Equal(address.Raw + 12, widthField.Raw);
		Assert.True(MuiImageFontMatchStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out var match));
		Assert.Equal(value.Match, match);
		Assert.True(MuiImageFontMatchStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiImageFontMatchStateField.Width,
			out var typedWidth));
		Assert.Equal(address.Raw + MuiImageFontMatchStateRecord.WidthOffset,
			typedWidth.Raw);
		Assert.True(MuiImageFontMatchStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiImageFontMatchStateField.Height, 720));
		Assert.True(MuiImageFontMatchStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiImageFontMatchStateField.Magic,
			out var typedMagic));
		Assert.Equal(value.Magic, typedMagic);
		Assert.True(MuiImageFontMatchStateRecordCodec.TryReadStructural(ref platform,
			address, out var typedStructural));
		Assert.Equal(720u, typedStructural.Height);
		Assert.Equal(value.Width, typedStructural.Width);
		Assert.False(MuiImageFontMatchStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiImageFontMatchStateField)0xFF, out _));
		Assert.False(MuiImageFontMatchStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiImageFontMatchStateRecord.Size, out _));
		Assert.False(MuiImageFontMatchStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, (uint)0, out _));
		Assert.False(MuiImageFontMatchStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void ImageFontMatchSequentialRecordPreservesScalarsAndRejectsTruncation()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3600);
		var value = new MuiImageFontMatchStateRecord
		{
			Magic = MuiImageFontMatchStateRecord.Cookie,
			Match = uint.MaxValue,
			Height = 0x80000000u,
			Width = 640,
		};
		Assert.True(MuiImageFontMatchStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiImageFontMatchStateRecordCodec.TryReadRecord(ref platform,
			address, out var actual));
		Assert.Equal(value.Magic, actual.Magic);
		Assert.Equal(value.Match, actual.Match);
		Assert.Equal(value.Height, actual.Height);
		Assert.Equal(value.Width, actual.Width);
		Assert.False(MuiImageFontMatchStateRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FFC), out _));
	}
}
