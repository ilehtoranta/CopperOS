using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class LayersExecListCodecTests
{
	[Fact]
	public void ReadsTheNamedPackedExecListRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x200, 0x3000,
			APTR.FromPointer(0x1200));
		var address = APTR.FromPointer(0x1080);
		var expected = new List
		{
			Head = APTR.FromPointer(0x1300),
			Tail = APTR.FromPointer(0x1304),
			TailPred = APTR.FromPointer(0x1308),
			Type = NodeType.Library,
			Padding = 0x5A,
		};
		Assert.True(LayersExecListCodec.Write(ref platform, address, expected));

		Assert.True(LayersExecListCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Head, actual.Head);
		Assert.Equal(expected.Tail, actual.Tail);
		Assert.Equal(expected.TailPred, actual.TailPred);
		Assert.Equal(expected.Type, actual.Type);
		Assert.Equal(expected.Padding, actual.Padding);
		Assert.Equal(expected.Head, LayersExecListCodec.ReadHead(ref platform,
			address));
	}

	[Fact]
	public void RejectsNullAndShortMappedRangesBeforeDecoding()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 16, 0x2000,
			APTR.FromPointer(0x1000));
		Assert.False(LayersExecListCodec.TryRead(ref platform, APTR.Null,
			out _));
		Assert.False(LayersExecListCodec.TryRead(ref platform,
			APTR.FromPointer(0x1001), out _));
		Assert.Equal(APTR.Null, LayersExecListCodec.ReadHead(ref platform,
			APTR.FromPointer(0x1001)));
	}
}
