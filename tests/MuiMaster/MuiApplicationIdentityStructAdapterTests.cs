using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationIdentityStructAdapterTests
{
	[Fact]
	public void ApplicationIdentityStateUsesDedicatedNamedStructAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiApplicationIdentityStateRecord
		{
			Magic = MuiApplicationIdentityStateRecord.Cookie,
			Author = APTR.Null,
			Base = APTR.Null,
			Copyright = APTR.Null,
			Description = APTR.Null,
			Title = APTR.Null,
			Version = APTR.Null,
		};

		Assert.True(MuiApplicationIdentityStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationIdentityStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationIdentityStateField.Title,
			out var titleField));
		Assert.Equal(APTR.FromPointer(0x3514), titleField);
		Assert.True(MuiApplicationIdentityStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationIdentityStateField.Version,
			out var versionField));
		Assert.Equal(APTR.FromPointer(0x3518), versionField);
		Assert.True(MuiApplicationIdentityStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationIdentityStateField.Title, 0x3600));
		Assert.True(MuiApplicationIdentityStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationIdentityStateField.Version, 0x3610));
		Assert.True(MuiApplicationIdentityStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(APTR.FromPointer(0x3600), decoded.Title);
		Assert.Equal(APTR.FromPointer(0x3610), decoded.Version);
		Assert.False(MuiApplicationIdentityStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FE8),
			MuiApplicationIdentityStateField.Magic, out _));
		Assert.False(MuiApplicationIdentityStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
			MuiApplicationIdentityStateField.Author, out _));
	}

	[Fact]
	public void ApplicationIdentityStateSequentialRecordPreservesPointersAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x35C0);
		var value = new MuiApplicationIdentityStateRecord
		{
			Magic = MuiApplicationIdentityStateRecord.Cookie,
			Author = APTR.FromPointer(uint.MaxValue),
			Base = APTR.FromPointer(0x01020304u),
			Copyright = APTR.FromPointer(0x11223344u),
			Description = APTR.FromPointer(0x55667788u),
			Title = APTR.FromPointer(0x99AABBCCu),
			Version = APTR.FromPointer(0xDDEEFF00u),
		};

		Assert.True(MuiApplicationIdentityStateRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiApplicationIdentityStateRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Author, decoded.Author);
		Assert.Equal(value.Base, decoded.Base);
		Assert.Equal(value.Copyright, decoded.Copyright);
		Assert.Equal(value.Description, decoded.Description);
		Assert.Equal(value.Title, decoded.Title);
		Assert.Equal(value.Version, decoded.Version);

		var crossingEnd = APTR.FromPointer(0x30FE5);
		Assert.False(MuiApplicationIdentityStateRecordCodec.WriteRecord(
			ref platform, crossingEnd, value));
		Assert.False(MuiApplicationIdentityStateRecordCodec.TryReadRecord(
			ref platform, crossingEnd, out _));
	}
}
