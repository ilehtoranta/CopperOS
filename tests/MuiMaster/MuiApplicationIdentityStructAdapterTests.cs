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
}
