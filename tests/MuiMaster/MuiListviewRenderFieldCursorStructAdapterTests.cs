using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewRenderFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedRenderRecordAndPreservesPointers()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListviewCore.MuiListviewRenderState
		{
			Magic = MuiListviewCore.MuiListviewRenderState.Cookie,
			RenderInfo = APTR.FromPointer(0x123456),
			RastPort = APTR.FromPointer(0x234567),
		};
		Assert.True(MuiListviewCore.MuiListviewRenderStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListviewCore.MuiListviewRenderFieldCursor
		{
			Record = address,
			Field = MuiListviewCore.MuiListviewRenderField.RastPort,
		};
		Assert.True(MuiListviewCore.MuiListviewRenderFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3508u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListviewCore.MuiListviewRenderFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiListviewCore.MuiListviewRenderField.RenderInfo,
			0x345678));
		Assert.True(MuiListviewCore.MuiListviewRenderFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiListviewCore.MuiListviewRenderField.RenderInfo,
			out var renderInfo));
		Assert.Equal(0x345678u, renderInfo);
		Assert.True(MuiListviewCore.MuiListviewRenderStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(0x345678u, decoded.RenderInfo.Raw);
		Assert.Equal(value.RastPort.Raw, decoded.RastPort.Raw);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewCore.MuiListviewRenderFieldCursorCodec.TryGetAddress(
			ref platform,
			new MuiListviewCore.MuiListviewRenderFieldCursor
			{
				Record = address,
				Field = (MuiListviewCore.MuiListviewRenderField)255,
			}, out _, out _));
		Assert.False(MuiListviewCore.MuiListviewRenderFieldCursorCodec.TryGetAddress(
			ref platform,
			new MuiListviewCore.MuiListviewRenderFieldCursor
			{
				Record = APTR.FromPointer(0x30FFC),
				Field = MuiListviewCore.MuiListviewRenderField.Magic,
			}, out _, out _));
		Assert.False(MuiListviewCore.MuiListviewRenderMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListviewCore.MuiListviewRenderField.Magic,
			out _, out _));
	}
}
