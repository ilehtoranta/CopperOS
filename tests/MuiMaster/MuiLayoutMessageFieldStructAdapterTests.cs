using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiLayoutMessageFieldStructAdapterTests
{
	[Fact]
	public void FieldWritesPreserveNamedLayoutPacketSiblings()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));

		var rectangleAddress = APTR.FromPointer(0x2400);
		var rectangle = new MuiLayoutRectangleMessage
		{
			MethodId = MuiLayoutPacketCore.DrawBackground,
			Left = 1,
			Top = 2,
			RightOrWidth = 3,
			BottomOrHeight = 4,
			Reserved0 = 5,
			Reserved1 = 6,
			Reserved2 = 7,
		};
		Assert.True(MuiLayoutMessageStructCodec.WriteRectangle(ref platform,
			rectangleAddress, rectangle));
		Assert.True(MuiLayoutMessageMemoryCodec.TryWriteUInt32(ref platform,
			rectangleAddress, MuiLayoutPacketKind.Rectangle,
			MuiLayoutField.Reserved1, 0x600));
		Assert.True(MuiLayoutMessageStructCodec.TryReadRectangle(ref platform,
			rectangleAddress, out var rectangleDecoded));
		Assert.Equal(rectangle.MethodId, rectangleDecoded.MethodId);
		Assert.Equal(rectangle.Left, rectangleDecoded.Left);
		Assert.Equal(0x600u, rectangleDecoded.Reserved1);
		Assert.Equal(rectangle.Reserved2, rectangleDecoded.Reserved2);

		var textAddress = APTR.FromPointer(0x2440);
		var text = new MuiLayoutTextMessage
		{
			MethodId = MuiLayoutPacketCore.Text,
			Left = 10,
			Top = 11,
			Width = 12,
			Height = 13,
			Text = 0x3500,
			Length = 14,
			PreParse = 15,
			Flags = 16,
		};
		Assert.True(MuiLayoutMessageStructCodec.WriteText(ref platform,
			textAddress, text));
		Assert.True(MuiLayoutMessageMemoryCodec.TryWriteUInt32(ref platform,
			textAddress, MuiLayoutPacketKind.Text, MuiLayoutField.PreParse,
			0xF0));
		Assert.True(MuiLayoutMessageStructCodec.TryReadText(ref platform,
			textAddress, out var textDecoded));
		Assert.Equal(text.MethodId, textDecoded.MethodId);
		Assert.Equal(text.Text, textDecoded.Text);
		Assert.Equal(0xF0u, textDecoded.PreParse);
		Assert.Equal(text.Flags, textDecoded.Flags);

		var layoutAddress = APTR.FromPointer(0x2480);
		var layout = new MuiLayoutMessage
		{
			MethodId = MuiLayoutPacketCore.Layout,
			Left = 21,
			Top = 22,
			Width = 23,
			Height = 24,
			Flags = 25,
		};
		Assert.True(MuiLayoutMessageStructCodec.WriteLayout(ref platform,
			layoutAddress, layout));
		Assert.True(MuiLayoutMessageMemoryCodec.TryWriteUInt32(ref platform,
			layoutAddress, MuiLayoutPacketKind.Layout, MuiLayoutField.Width,
			0x230));
		Assert.True(MuiLayoutMessageStructCodec.TryReadLayout(ref platform,
			layoutAddress, out var layoutDecoded));
		Assert.Equal(layout.MethodId, layoutDecoded.MethodId);
		Assert.Equal(layout.Left, layoutDecoded.Left);
		Assert.Equal(0x230u, layoutDecoded.Width);
		Assert.Equal(layout.Height, layoutDecoded.Height);
		Assert.Equal(layout.Flags, layoutDecoded.Flags);

		Assert.False(MuiLayoutMessageMemoryCodec.TryWriteUInt32(ref platform,
			layoutAddress, MuiLayoutPacketKind.Layout, MuiLayoutField.Text, 1));
		Assert.False(MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x20FF0), MuiLayoutPacketKind.Text,
			MuiLayoutField.Text, out _));
	}
}
