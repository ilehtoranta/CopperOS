using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationConfigWindowStateStructAdapterTests
{
	[Fact]
	public void ApplicationConfigWindowStateUsesDedicatedNamedStructAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2C80);
		var value = new MuiApplicationConfigWindowStateRecord
		{
			Magic = MuiApplicationConfigWindowStateRecord.Cookie,
			Flags = 0x10203040u,
			ClassId = APTR.Null,
			Requests = 3,
		};

		Assert.True(MuiApplicationConfigWindowStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationConfigWindowStateRecordMemoryCodec.TryGetAddress(
			ref platform, address,
			MuiApplicationConfigWindowStateField.ClassId, out var classIdField));
		Assert.Equal(APTR.FromPointer(0x2C88), classIdField);
		Assert.True(MuiApplicationConfigWindowStateRecordMemoryCodec.TryGetAddress(
			ref platform, address,
			MuiApplicationConfigWindowStateField.Requests, out var requestsField));
		Assert.Equal(APTR.FromPointer(0x2C8C), requestsField);
		Assert.True(MuiApplicationConfigWindowStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationConfigWindowStateField.Flags,
			0x55667788u));
		Assert.True(MuiApplicationConfigWindowStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(0x55667788u, decoded.Flags);
		Assert.Equal(value.Requests, decoded.Requests);
		Assert.False(MuiApplicationConfigWindowStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF4),
			MuiApplicationConfigWindowStateField.Magic, out _));
		Assert.False(MuiApplicationConfigWindowStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiApplicationConfigWindowStateField.Flags,
			out _));
	}
}
