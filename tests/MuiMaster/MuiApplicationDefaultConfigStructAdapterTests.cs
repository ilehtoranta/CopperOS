using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationDefaultConfigStructAdapterTests
{
	[Fact]
	public void ApplicationDefaultConfigStateUsesDedicatedNamedStructAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiApplicationDefaultConfigStateRecord
		{
			Magic = MuiApplicationDefaultConfigStateRecord.Cookie,
			ConfigId = 0x44,
			Value = 0x12345678,
			Requests = 7,
		};

		Assert.True(MuiApplicationDefaultConfigStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationDefaultConfigStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationDefaultConfigStateField.Value,
			out var valueField));
		Assert.Equal(APTR.FromPointer(0x3508), valueField);
		Assert.True(MuiApplicationDefaultConfigStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationDefaultConfigStateField.Requests,
			out var requestsField));
		Assert.Equal(APTR.FromPointer(0x350C), requestsField);
		Assert.True(MuiApplicationDefaultConfigStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationDefaultConfigStateField.Requests, 8));
		Assert.True(MuiApplicationDefaultConfigStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.ConfigId, decoded.ConfigId);
		Assert.Equal(value.Value, decoded.Value);
		Assert.Equal(8u, decoded.Requests);
		Assert.False(MuiApplicationDefaultConfigStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF8),
			MuiApplicationDefaultConfigStateField.Magic, out _));
		Assert.False(MuiApplicationDefaultConfigStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
			MuiApplicationDefaultConfigStateField.ConfigId, out _));
	}
}
