using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationSetConfigItemStateStructAdapterTests
{
	[Fact]
	public void ApplicationSetConfigItemStateUsesDedicatedNamedStructAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2D00);
		var value = new MuiApplicationSetConfigItemStateRecord
		{
			Magic = MuiApplicationSetConfigItemStateRecord.Cookie,
			Item = 0x12345678u,
			Data = APTR.Null,
			Requests = 4,
		};

		Assert.True(MuiApplicationSetConfigItemStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationSetConfigItemStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationSetConfigItemStateField.Data,
			out var dataField));
		Assert.Equal(APTR.FromPointer(0x2D08), dataField);
		Assert.True(MuiApplicationSetConfigItemStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationSetConfigItemStateField.Requests,
			out var requestsField));
		Assert.Equal(APTR.FromPointer(0x2D0C), requestsField);
		Assert.True(MuiApplicationSetConfigItemStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationSetConfigItemStateField.Item,
			0x87654321u));
		Assert.True(MuiApplicationSetConfigItemStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationSetConfigItemStateField.Data,
			0x2E00u));
		Assert.True(MuiApplicationSetConfigItemStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(0x87654321u, decoded.Item);
		Assert.Equal(APTR.FromPointer(0x2E00), decoded.Data);
		Assert.Equal(value.Requests, decoded.Requests);
		Assert.False(MuiApplicationSetConfigItemStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF8),
			MuiApplicationSetConfigItemStateField.Magic, out _));
		Assert.False(MuiApplicationSetConfigItemStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiApplicationSetConfigItemStateField.Item,
			out _));
	}

	[Fact]
	public void ApplicationSetConfigItemSequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x35C0);
		var value = new MuiApplicationSetConfigItemStateRecord
		{
			Magic = MuiApplicationSetConfigItemStateRecord.Cookie,
			Item = uint.MaxValue,
			Data = APTR.FromPointer(uint.MaxValue),
			Requests = 0xA5A5A5A5u,
		};

		Assert.True(MuiApplicationSetConfigItemStateRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiApplicationSetConfigItemStateRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Item, decoded.Item);
		Assert.Equal(value.Data, decoded.Data);
		Assert.Equal(value.Requests, decoded.Requests);

		var crossingEnd = APTR.FromPointer(0x30FF1);
		Assert.False(MuiApplicationSetConfigItemStateRecordCodec.WriteRecord(
			ref platform, crossingEnd, value));
		Assert.False(MuiApplicationSetConfigItemStateRecordCodec.TryReadRecord(
			ref platform, crossingEnd, out _));
	}
}
