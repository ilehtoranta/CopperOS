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

	[Fact]
	public void ApplicationMessageRoutingSequentialRecordPreservesPointerAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x35C0);
		var value = new MuiApplicationMessageRoutingStateRecord
		{
			Magic = MuiApplicationMessageRoutingStateRecord.Cookie,
			AppMessage = APTR.FromPointer(uint.MaxValue),
			WindowAppWindow = 1,
		};

		Assert.True(MuiApplicationMessageRoutingStateRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiApplicationMessageRoutingStateRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.AppMessage, decoded.AppMessage);
		Assert.Equal(value.WindowAppWindow, decoded.WindowAppWindow);

		var crossingEnd = APTR.FromPointer(0x30FF5);
		Assert.False(MuiApplicationMessageRoutingStateRecordCodec.WriteRecord(
			ref platform, crossingEnd, value));
		Assert.False(MuiApplicationMessageRoutingStateRecordCodec.TryReadRecord(
			ref platform, crossingEnd, out _));
	}

	[Fact]
	public void ApplicationMessageRoutingFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3640);
		var initial = new MuiApplicationMessageRoutingStateRecord
		{
			Magic = 0x10203040u,
			AppMessage = APTR.FromPointer(0x50607080u),
			WindowAppWindow = 1,
		};

		Assert.True(MuiApplicationMessageRoutingStateRecordCodec.WriteRecord(
			ref platform, address, initial));
		Assert.True(MuiApplicationMessageRoutingStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationMessageRoutingStateField.AppMessage,
			0xF1020304u));
		Assert.True(MuiApplicationMessageRoutingStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address,
			MuiApplicationMessageRoutingStateField.WindowAppWindow, out var window));
		Assert.Equal(initial.WindowAppWindow, window);
		Assert.True(MuiApplicationMessageRoutingStateRecordCodec.TryReadStructural(
			ref platform, address, out var updated));
		Assert.Equal(initial.Magic, updated.Magic);
		Assert.Equal(APTR.FromPointer(0xF1020304u), updated.AppMessage);
		Assert.Equal(initial.WindowAppWindow, updated.WindowAppWindow);
		Assert.False(MuiApplicationMessageRoutingStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address,
			unchecked((MuiApplicationMessageRoutingStateField)255), 1));
	}
}
