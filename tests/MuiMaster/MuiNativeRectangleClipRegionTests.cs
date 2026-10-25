using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeRectangleClipRegionTests
{
	private static readonly APTR RegionAddress = APTR.FromPointer(0x1000);
	private static readonly APTR RectangleAddress = APTR.FromPointer(0x1100);

	[Theory]
	[InlineData(10, 20, 30, 40)]
	[InlineData(-100, -200, 70, 80)]
	[InlineData(0, 0, 1, 1)]
	[InlineData(short.MinValue, short.MinValue, short.MaxValue, short.MaxValue)]
	[InlineData(short.MaxValue, short.MaxValue, 1, 1)]
	public void RegionDecodesToRequestedAbsoluteClip(int left, int top,
		int width, int height)
	{
		var memory = Memory();
		Assert.True(MuiNativeRectangleClipRegion.TryInitialize(ref memory,
			RegionAddress, RectangleAddress, left, top, width, height));
		var region = LayersRegionCodec.ReadBounds(ref memory, RegionAddress);
		var relative = LayersRegionRectangleCodec.ReadBounds(ref memory,
			RectangleAddress);
		Assert.Equal(left, region.MinX + relative.MinX);
		Assert.Equal(top, region.MinY + relative.MinY);
		Assert.Equal(left + width - 1, region.MinX + relative.MaxX);
		Assert.Equal(top + height - 1, region.MinY + relative.MaxY);
		Assert.Equal(RectangleAddress, LayersRegionCodec.ReadFirst(ref memory,
			RegionAddress));
		Assert.Equal(APTR.Null, LayersRegionRectangleCodec.ReadNext(ref memory,
			RectangleAddress));
		Assert.Equal(LayersRegionCodec.HeadAnchor(RegionAddress),
			LayersRegionRectangleCodec.ReadPrevious(ref memory, RectangleAddress));
	}

	[Theory]
	[InlineData(short.MaxValue, 0, 2, 1)]
	[InlineData(0, short.MaxValue, 1, 2)]
	[InlineData(int.MaxValue, 0, 1, 1)]
	[InlineData(0, 0, int.MaxValue, 1)]
	[InlineData(0, 0, 0, 1)]
	[InlineData(0, 0, 1, -1)]
	public void InvalidGeometryLeavesRegionUntouched(int left, int top,
		int width, int height)
	{
		var memory = Memory();
		memory.WriteUInt32(RegionAddress, 0, 0x12345678);
		memory.WriteUInt32(RectangleAddress, 0, 0xAABBCCDD);
		Assert.False(MuiNativeRectangleClipRegion.TryInitialize(ref memory,
			RegionAddress, RectangleAddress, left, top, width, height));
		Assert.Equal(0x12345678u, memory.ReadUInt32(RegionAddress, 0));
		Assert.Equal(0xAABBCCDDu, memory.ReadUInt32(RectangleAddress, 0));
	}

	[Fact]
	public void TruncatedRectangleDoesNotPartiallyPublishRegion()
	{
		var memory = Memory();
		memory.WriteUInt32(RegionAddress, 0, 0x12345678);
		Assert.False(MuiNativeRectangleClipRegion.TryInitialize(ref memory,
			RegionAddress, APTR.FromPointer(0x11F8), 10, 20, 30, 40));
		Assert.Equal(0x12345678u, memory.ReadUInt32(RegionAddress, 0));
	}

	private static MuiHeadlessTestPlatform Memory() =>
		new(0x1000, 0x200, 0, RegionAddress);
}
