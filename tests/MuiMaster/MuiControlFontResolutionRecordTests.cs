using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiControlFontResolutionRecordTests
{
	[Fact]
	public void FontResolutionRecordUsesDedicatedStructAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2800);
		var value = new MuiControlFontResolutionRecord
		{
			Magic = MuiControlFontResolutionRecord.Cookie,
			Present = 1,
			Inherited = 1,
			Depth = 2,
			Font = APTR.FromPointer(0x2A00),
		};

		Assert.True(MuiControlFontResolutionRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiControlFontResolutionRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiControlFontResolutionRecordField.Font,
			out var fontField));
		Assert.Equal(APTR.FromPointer(0x2810), fontField);
		Assert.True(MuiControlFontResolutionRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiControlFontResolutionRecordField.Inherited,
			out var inherited));
		Assert.Equal(1u, inherited);
		Assert.True(MuiControlFontResolutionRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiControlFontResolutionRecordField.Depth, 3));
		Assert.True(MuiControlFontResolutionRecordCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(3u, decoded.Depth);
		Assert.Equal(value.Font, decoded.Font);
		Assert.False(MuiControlFontResolutionRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF0),
			MuiControlFontResolutionRecordField.Font, out _));
		Assert.False(MuiControlFontResolutionRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiControlFontResolutionRecordField.Magic,
			out _));
	}
}
