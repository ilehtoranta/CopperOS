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

	[Fact]
	public void ApplicationDefaultConfigStateSequentialRecordPreservesFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3540);
		var value = new MuiApplicationDefaultConfigStateRecord
		{
			Magic = MuiApplicationDefaultConfigStateRecord.Cookie,
			ConfigId = 0xAABBCCDDu,
			Value = 0x01020304u,
			Requests = uint.MaxValue,
		};

		Assert.True(MuiApplicationDefaultConfigStateRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiApplicationDefaultConfigStateRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.ConfigId, decoded.ConfigId);
		Assert.Equal(value.Value, decoded.Value);
		Assert.Equal(value.Requests, decoded.Requests);

		var crossingEnd = APTR.FromPointer(0x30FF1);
		Assert.False(MuiApplicationDefaultConfigStateRecordCodec.WriteRecord(
			ref platform, crossingEnd, value));
		Assert.False(MuiApplicationDefaultConfigStateRecordCodec.TryReadRecord(
			ref platform, crossingEnd, out _));
	}
}
