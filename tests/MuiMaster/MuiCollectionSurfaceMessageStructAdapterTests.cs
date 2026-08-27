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

		var minMaxAddress = APTR.FromPointer(0x3040);
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteAskMinMax(ref platform,
			minMaxAddress, 0x3500));
		Assert.True(MuiCollectionSurfaceMessageCodec.TryReadAskMinMax(ref platform,
			minMaxAddress, out var minMax));
		Assert.Equal(0x3500u, minMax.Storage);

		var drawAddress = APTR.FromPointer(0x3060);
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteDraw(ref platform,
			drawAddress, 7));
		Assert.True(MuiCollectionSurfaceMessageCodec.TryReadDraw(ref platform,
			drawAddress, out var draw));
		Assert.Equal(7u, draw.Flags);

		var inputAddress = APTR.FromPointer(0x3080);
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			inputAddress, 0x3600, -9));
		Assert.True(MuiCollectionSurfaceMessageCodec.TryReadHandleInput(ref platform,
			inputAddress, out var input));
		Assert.Equal(0x3600u, input.IntuiMessage);
		Assert.Equal(-9, input.MuiKey);

		var attributeAddress = APTR.FromPointer(0x30A0);
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteAttribute(ref platform,
			attributeAddress, MuiCollectionSurfaceMessageCodec.Set, 0x120, 0x456));
		Assert.True(MuiCollectionSurfaceMessageCodec.TryReadAttribute(ref platform,
			attributeAddress, MuiCollectionSurfaceMessageCodec.Set,
			out var attribute));
		Assert.Equal(0x120u, attribute.Attribute);
		Assert.Equal(0x456u, attribute.Value);

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
