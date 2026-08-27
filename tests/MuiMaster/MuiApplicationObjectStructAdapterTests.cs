using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationObjectStructAdapterTests
{
	[Fact]
	public void ApplicationObjectStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiApplicationObjectStateRecord
		{
			Magic = MuiApplicationObjectStateRecord.Cookie,
			DiskObject = APTR.Null,
			DropObject = APTR.Null,
			Menustrip = APTR.Null,
		};

		Assert.True(MuiApplicationObjectStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationObjectStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationObjectStateField.Menustrip,
			out var menustripAddress));
		Assert.Equal(0x350Cu, menustripAddress.Raw);
		Assert.True(MuiApplicationObjectStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationObjectStateField.DropObject,
			0x12345678));
		Assert.True(MuiApplicationObjectStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(0x12345678u, decoded.DropObject.Raw);
		Assert.False(MuiApplicationObjectStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF1),
			MuiApplicationObjectStateField.Magic, out _));
		Assert.False(MuiApplicationObjectStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiApplicationObjectStateField.Magic, out _));
	}
}
