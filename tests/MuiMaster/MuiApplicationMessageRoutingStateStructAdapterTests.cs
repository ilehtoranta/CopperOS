using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationMessageRoutingStateStructAdapterTests
{
	[Fact]
	public void ApplicationMessageRoutingStateUsesDedicatedNamedStructAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3200);
		var value = new MuiApplicationMessageRoutingStateRecord
		{
			Magic = MuiApplicationMessageRoutingStateRecord.Cookie,
			AppMessage = APTR.Null,
			WindowAppWindow = 1,
		};

		Assert.True(MuiApplicationMessageRoutingStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationMessageRoutingStateRecordMemoryCodec.TryGetAddress(
			ref platform, address,
			MuiApplicationMessageRoutingStateField.AppMessage, out var messageField));
		Assert.Equal(APTR.FromPointer(0x3204), messageField);
		Assert.True(MuiApplicationMessageRoutingStateRecordMemoryCodec.TryGetAddress(
			ref platform, address,
			MuiApplicationMessageRoutingStateField.WindowAppWindow,
			out var windowField));
		Assert.Equal(APTR.FromPointer(0x3208), windowField);
		Assert.True(MuiApplicationMessageRoutingStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationMessageRoutingStateField.AppMessage, 0x3300u));
		Assert.True(MuiApplicationMessageRoutingStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(APTR.FromPointer(0x3300), decoded.AppMessage);
		Assert.Equal(value.WindowAppWindow, decoded.WindowAppWindow);
		Assert.False(MuiApplicationMessageRoutingStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF8),
			MuiApplicationMessageRoutingStateField.Magic, out _));
		Assert.False(MuiApplicationMessageRoutingStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
			MuiApplicationMessageRoutingStateField.AppMessage, out _));
	}
}
