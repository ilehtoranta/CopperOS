using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiCollectionSurfaceMessageStructAdapterTests
{
	[Fact]
	public void CollectionSurfacePacketsUseNamedFieldsAndCompleteBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var layoutAddress = APTR.FromPointer(0x3000);
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteLayout(ref platform,
			layoutAddress, 1, 2, 3, 4));
		Assert.True(MuiCollectionSurfaceMessageCodec.TryReadLayout(ref platform,
			layoutAddress, out var layout));
		Assert.Equal(3u, layout.Width);
		Assert.True(MuiCollectionSurfaceMessageMemoryCodec.TryGetAddress(
			ref platform, layoutAddress, MuiCollectionSurfacePacketKind.Layout,
			MuiCollectionSurfaceField.Height, out var heightAddress));
		Assert.Equal(APTR.FromPointer(0x3010), heightAddress);
		Assert.True(MuiCollectionSurfaceMessageMemoryCodec.TryWriteUInt32(ref platform,
			layoutAddress, MuiCollectionSurfacePacketKind.Layout,
			MuiCollectionSurfaceField.Width, 30));
		Assert.True(MuiCollectionSurfaceMessageCodec.TryReadLayout(ref platform,
			layoutAddress, out layout));
		Assert.Equal(MuiCollectionSurfaceMessageCodec.Layout, layout.MethodId);
		Assert.Equal(1u, layout.Left);
		Assert.Equal(2u, layout.Top);
		Assert.Equal(30u, layout.Width);
		Assert.Equal(4u, layout.Height);

		var minMaxAddress = APTR.FromPointer(0x3040);
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteAskMinMax(ref platform,
			minMaxAddress, 0x3500));
		Assert.True(MuiCollectionSurfaceMessageCodec.TryReadAskMinMax(ref platform,
			minMaxAddress, out var minMax));
		Assert.Equal(0x3500u, minMax.Storage);
		Assert.True(MuiCollectionSurfaceMessageMemoryCodec.TryWriteUInt32(ref platform,
			minMaxAddress, MuiCollectionSurfacePacketKind.AskMinMax,
			MuiCollectionSurfaceField.Storage, 0x3510));
		Assert.True(MuiCollectionSurfaceMessageCodec.TryReadAskMinMax(ref platform,
			minMaxAddress, out minMax));
		Assert.Equal(MuiCollectionSurfaceMessageCodec.AskMinMax, minMax.MethodId);
		Assert.Equal(0x3510u, minMax.Storage);

		var drawAddress = APTR.FromPointer(0x3060);
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteDraw(ref platform,
			drawAddress, 7));
		Assert.True(MuiCollectionSurfaceMessageCodec.TryReadDraw(ref platform,
			drawAddress, out var draw));
		Assert.Equal(7u, draw.Flags);
		Assert.True(MuiCollectionSurfaceMessageMemoryCodec.TryWriteUInt32(ref platform,
			drawAddress, MuiCollectionSurfacePacketKind.Draw,
			MuiCollectionSurfaceField.Flags, 8));
		Assert.True(MuiCollectionSurfaceMessageCodec.TryReadDraw(ref platform,
			drawAddress, out draw));
		Assert.Equal(MuiCollectionSurfaceMessageCodec.Draw, draw.MethodId);
		Assert.Equal(8u, draw.Flags);

		var inputAddress = APTR.FromPointer(0x3080);
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			inputAddress, 0x3600, -9));
		Assert.True(MuiCollectionSurfaceMessageCodec.TryReadHandleInput(ref platform,
			inputAddress, out var input));
		Assert.Equal(0x3600u, input.IntuiMessage);
		Assert.Equal(-9, input.MuiKey);
		Assert.True(MuiCollectionSurfaceMessageMemoryCodec.TryWriteUInt32(ref platform,
			inputAddress, MuiCollectionSurfacePacketKind.HandleInput,
			MuiCollectionSurfaceField.MuiKey, unchecked((uint)-10)));
		Assert.True(MuiCollectionSurfaceMessageCodec.TryReadHandleInput(ref platform,
			inputAddress, out input));
		Assert.Equal(MuiCollectionSurfaceMessageCodec.HandleInput, input.MethodId);
		Assert.Equal(0x3600u, input.IntuiMessage);
		Assert.Equal(-10, input.MuiKey);

		var attributeAddress = APTR.FromPointer(0x30A0);
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteAttribute(ref platform,
			attributeAddress, MuiCollectionSurfaceMessageCodec.Set, 0x120, 0x456));
		Assert.True(MuiCollectionSurfaceMessageCodec.TryReadAttribute(ref platform,
			attributeAddress, MuiCollectionSurfaceMessageCodec.Set,
			out var attribute));
		Assert.Equal(0x120u, attribute.Attribute);
		Assert.Equal(0x456u, attribute.Value);
		Assert.True(MuiCollectionSurfaceMessageMemoryCodec.TryWriteUInt32(ref platform,
			attributeAddress, MuiCollectionSurfacePacketKind.Attribute,
			MuiCollectionSurfaceField.Value, 0x457));
		Assert.True(MuiCollectionSurfaceMessageCodec.TryReadAttribute(ref platform,
			attributeAddress, MuiCollectionSurfaceMessageCodec.Set,
			out attribute));
		Assert.Equal(MuiCollectionSurfaceMessageCodec.Set, attribute.MethodId);
		Assert.Equal(0x120u, attribute.Attribute);
		Assert.Equal(0x457u, attribute.Value);
		Assert.False(MuiCollectionSurfaceMessageMemoryCodec.TryReadUInt32(ref platform,
			attributeAddress, MuiCollectionSurfacePacketKind.Draw,
			MuiCollectionSurfaceField.Value, out _));
		Assert.False(MuiCollectionSurfaceMessageMemoryCodec.TryWriteUInt32(ref platform,
			attributeAddress, MuiCollectionSurfacePacketKind.Attribute,
			(MuiCollectionSurfaceField)255, 1));

		Assert.False(MuiCollectionSurfaceMessageMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x20FF0),
			MuiCollectionSurfacePacketKind.Layout,
			MuiCollectionSurfaceField.Height, out _));
		Assert.False(MuiCollectionSurfaceMessageMemoryCodec.TryGetAddress(
			ref platform, layoutAddress, MuiCollectionSurfacePacketKind.Layout,
			MuiCollectionSurfaceField.Storage, out _));
		Assert.False(MuiCollectionSurfaceMessageMemoryCodec.TryGetAddress(
			ref platform, layoutAddress, (MuiCollectionSurfacePacketKind)255,
			MuiCollectionSurfaceField.MethodId, out _));
		Assert.False(MuiCollectionSurfaceMessageMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiCollectionSurfacePacketKind.Draw,
			MuiCollectionSurfaceField.Flags, out _));
	}
}
