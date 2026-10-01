using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiCommonControlFieldStructAdapterTests
{
	private static MuiHeadlessTestPlatform NewPlatform()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000,
			0x8000, state);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, state));
		return platform;
	}

	[Fact]
	public void ChoiceEntryFieldAdapterUsesNamedRecordRoundTrip()
	{
		var platform = NewPlatform();
		var address = APTR.FromPointer(0x1180);
		Assert.True(MuiChoiceEntryCodec.Write(ref platform, address,
			new MuiChoiceEntry { Text = APTR.FromPointer(0x1300) }));

		Assert.True(MuiChoiceEntryMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiChoiceEntryField.Text, 0xF00DCAFE));
		Assert.True(MuiChoiceEntryMemoryCodec.TryReadUInt32(ref platform,
			address, MuiChoiceEntryField.Text, out var text));
		Assert.Equal(0xF00DCAFEu, text);
		Assert.True(MuiChoiceEntryCodec.TryRead(ref platform, address,
			out var entry));
		Assert.Equal(APTR.FromPointer(0xF00DCAFE), entry.Text);

		Assert.False(MuiChoiceEntryMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x40FFE), MuiChoiceEntryField.Text, out _));
		Assert.False(MuiChoiceEntryMemoryCodec.TryWriteUInt32(ref platform,
			APTR.Null, MuiChoiceEntryField.Text, 1));
	}

	[Fact]
	public void ImageGeometryFieldAdapterPreservesSiblingWords()
	{
		var platform = NewPlatform();
		var address = APTR.FromPointer(0x3500);
		var expected = new MuiImageGeometryState
		{
			LeftEdge = -3,
			TopEdge = 4,
			Width = 24,
			Height = 20,
		};
		Assert.True(MuiImageGeometryCodec.Write(ref platform, address, expected));

		Assert.True(MuiImageGeometryMemoryCodec.TryWriteUInt16(ref platform,
			address, MuiImageGeometryField.Width, 320));
		Assert.True(MuiImageGeometryMemoryCodec.TryReadUInt16(ref platform,
			address, MuiImageGeometryField.LeftEdge, out var left));
		Assert.Equal(unchecked((ushort)expected.LeftEdge), left);
		Assert.True(MuiImageGeometryCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.LeftEdge, actual.LeftEdge);
		Assert.Equal(expected.TopEdge, actual.TopEdge);
		Assert.Equal((ushort)320, actual.Width);
		Assert.Equal(expected.Height, actual.Height);

		Assert.False(MuiImageGeometryMemoryCodec.TryReadUInt16(ref platform,
			APTR.FromPointer(0x40FFC), MuiImageGeometryField.Height, out _));
		Assert.False(MuiImageGeometryMemoryCodec.TryWriteUInt16(ref platform,
			APTR.Null, MuiImageGeometryField.Height, 1));
	}
}
