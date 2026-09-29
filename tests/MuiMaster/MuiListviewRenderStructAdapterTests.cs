using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewRenderStructAdapterTests
{
	[Fact]
	public void RenderFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListviewCore.MuiListviewRenderState
		{
			Magic = MuiListviewCore.MuiListviewRenderState.Cookie,
			RenderInfo = APTR.FromPointer(0x36200),
			RastPort = APTR.FromPointer(0x36400),
		};

		Assert.True(MuiListviewCore.MuiListviewRenderStateCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiListviewCore.MuiListviewRenderMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListviewCore.MuiListviewRenderField.RenderInfo,
			0x36400));
		Assert.True(MuiListviewCore.MuiListviewRenderMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListviewCore.MuiListviewRenderField.RastPort,
			0x36600));
		Assert.True(MuiListviewCore.MuiListviewRenderMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListviewCore.MuiListviewRenderField.RastPort,
			out var rastPort));
		Assert.Equal(0x36600u, rastPort);

		Assert.True(MuiListviewCore.MuiListviewRenderStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(0x36400u, decoded.RenderInfo.Raw);
		Assert.Equal(0x36600u, decoded.RastPort.Raw);

		Assert.True(MuiListviewCore.MuiListviewRenderMemoryCodec.TryGetAddress(
			ref platform, address, MuiListviewCore.MuiListviewRenderField.RastPort,
			out var rastPortAddress));
		Assert.Equal(0x3508u, rastPortAddress.Raw);
	}

	[Fact]
	public void RenderAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewCore.MuiListviewRenderMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiListviewCore.MuiListviewRenderField)0xFF,
			out _));
		Assert.False(MuiListviewCore.MuiListviewRenderMemoryCodec.TryWriteUInt32(
			ref platform, address, (MuiListviewCore.MuiListviewRenderField)0xFF, 1));
		Assert.False(MuiListviewCore.MuiListviewRenderMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FFC),
			MuiListviewCore.MuiListviewRenderField.RastPort, out _));
		Assert.False(MuiListviewCore.MuiListviewRenderMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null, MuiListviewCore.MuiListviewRenderField.Magic, 1));
	}
}
