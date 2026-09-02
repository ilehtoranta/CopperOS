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
			Reserved = 0x55667788u,
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
		Assert.Equal(value.Reserved, decoded.Reserved);
		Assert.False(MuiApplicationConfigWindowStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF4),
			MuiApplicationConfigWindowStateField.Magic, out _));
		Assert.False(MuiApplicationConfigWindowStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiApplicationConfigWindowStateField.Flags,
			out _));
	}

	[Fact]
	public void ApplicationConfigWindowStateSequentialRecordPreservesFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2CE0);
		var value = new MuiApplicationConfigWindowStateRecord
		{
			Magic = MuiApplicationConfigWindowStateRecord.Cookie,
			Flags = 0x10203040u,
			ClassId = APTR.FromPointer(uint.MaxValue),
			Requests = 0xAABBCCDDu,
			Reserved = 0x55667788u,
		};

		Assert.True(MuiApplicationConfigWindowStateRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiApplicationConfigWindowStateRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Flags, decoded.Flags);
		Assert.Equal(value.ClassId, decoded.ClassId);
		Assert.Equal(value.Requests, decoded.Requests);
		Assert.Equal(value.Reserved, decoded.Reserved);

		var crossingEnd = APTR.FromPointer(0x30FED);
		Assert.False(MuiApplicationConfigWindowStateRecordCodec.WriteRecord(
			ref platform, crossingEnd, value));
		Assert.False(MuiApplicationConfigWindowStateRecordCodec.TryReadRecord(
			ref platform, crossingEnd, out _));
	}

	[Fact]
	public void ApplicationConfigWindowFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3640);
		var initial = new MuiApplicationConfigWindowStateRecord
		{
			Magic = 0x10203040u,
			Flags = 0x50607080u,
			ClassId = APTR.FromPointer(0x90A0B0C0u),
			Requests = 0x01020304u,
			Reserved = 0xDDEEFF00u,
		};

		Assert.True(MuiApplicationConfigWindowStateRecordCodec.WriteRecord(
			ref platform, address, initial));
		Assert.True(MuiApplicationConfigWindowStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationConfigWindowStateField.Flags,
			0xF1020304u));
		Assert.True(MuiApplicationConfigWindowStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationConfigWindowStateField.ClassId,
			out var classId));
		Assert.Equal(initial.ClassId.Raw, classId);
		Assert.True(MuiApplicationConfigWindowStateRecordCodec.TryReadStructural(
			ref platform, address, out var updated));
		Assert.Equal(initial.Magic, updated.Magic);
		Assert.Equal(0xF1020304u, updated.Flags);
		Assert.Equal(initial.ClassId, updated.ClassId);
		Assert.Equal(initial.Requests, updated.Requests);
		Assert.Equal(initial.Reserved, updated.Reserved);
		Assert.False(MuiApplicationConfigWindowStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address,
			unchecked((MuiApplicationConfigWindowStateField)255), 1));
	}
}
