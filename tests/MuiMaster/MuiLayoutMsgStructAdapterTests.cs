using Amiga;
using Amiga.MUI;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiLayoutMsgStructAdapterTests
{
	[Fact]
	public void LayoutMessageUsesDedicatedMixedWidthStructAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2900);
		var value = new MUI_LayoutMsg
		{
			lm_Type = 0x4C41594Fu,
			lm_Children = APTR.FromPointer(0x2A00),
			lm_MinMax = new MUI_MinMax
			{
				MinWidth = -8,
				MinHeight = 12,
				MaxWidth = 640,
				MaxHeight = 480,
				DefWidth = 320,
				DefHeight = 200,
			},
			lm_Layout = new MUI_LayoutDimensions
			{
				Width = 640,
				Height = 480,
				priv5 = 0x11223344u,
				priv6 = 0x55667788u,
			},
		};

		Assert.True(MUI_LayoutMsgCodec.WriteRecord(ref platform, address, value));
		Assert.True(MUI_LayoutMsgMemoryCodec.TryGetAddress(ref platform, address,
			MUI_LayoutMsgField.Height, out var heightField, out var heightSize));
		Assert.Equal(APTR.FromPointer(0x2918), heightField);
		Assert.Equal(4u, heightSize);
		Assert.True(MUI_LayoutMsgMemoryCodec.TryGetAddress(ref platform, address,
			MUI_LayoutMsgField.MinWidth, out var minWidthField, out var minWidthSize));
		Assert.Equal(APTR.FromPointer(0x2908), minWidthField);
		Assert.Equal(2u, minWidthSize);
		Assert.True(MUI_LayoutMsgMemoryCodec.TryReadUInt16(ref platform, address,
			MUI_LayoutMsgField.MinWidth, out var minWidth));
		Assert.Equal(-8, unchecked((short)minWidth));
		Assert.True(MUI_LayoutMsgMemoryCodec.TryWriteUInt32(ref platform, address,
			MUI_LayoutMsgField.Width, 800));
		Assert.True(MUI_LayoutMsgCodec.TryReadRecord(ref platform, address,
			out var decoded));
		Assert.Equal(800, decoded.lm_Layout.Width);
		Assert.Equal(value.lm_Children, decoded.lm_Children);
		Assert.Equal(value.lm_MinMax.DefHeight, decoded.lm_MinMax.DefHeight);
		Assert.True(MUI_LayoutMsgCodec.Write(ref platform, address, value));
		Assert.True(MUI_LayoutMsgCodec.TryRead(ref platform, address,
			out _));
		Assert.False(MUI_LayoutMsgCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FEA), out _));
		Assert.False(MUI_LayoutMsgMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x30FF0), MUI_LayoutMsgField.Height, out _, out _));
		Assert.False(MUI_LayoutMsgMemoryCodec.TryGetAddress(ref platform, APTR.Null,
			MUI_LayoutMsgField.Type, out _, out _));
	}
}
