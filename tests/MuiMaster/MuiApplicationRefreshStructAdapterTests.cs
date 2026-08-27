using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationRefreshStructAdapterTests
{
	[Fact]
	public void ApplicationRefreshStateUsesDedicatedNamedStructAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiApplicationRefreshStateRecord
		{
			Magic = MuiApplicationRefreshStateRecord.Cookie,
			Checks = 3,
			RefreshedWindows = 5,
		};

		Assert.True(MuiApplicationRefreshStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationRefreshStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationRefreshStateField.Checks,
			out var checksField));
		Assert.Equal(APTR.FromPointer(0x3504), checksField);
		Assert.True(MuiApplicationRefreshStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationRefreshStateField.RefreshedWindows,
			out var windowsField));
		Assert.Equal(APTR.FromPointer(0x3508), windowsField);
		Assert.True(MuiApplicationRefreshStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationRefreshStateField.Checks, 4));
		Assert.True(MuiApplicationRefreshStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(4u, decoded.Checks);
		Assert.Equal(value.RefreshedWindows, decoded.RefreshedWindows);
		Assert.False(MuiApplicationRefreshStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF8),
			MuiApplicationRefreshStateField.Magic, out _));
		Assert.False(MuiApplicationRefreshStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
			MuiApplicationRefreshStateField.Checks, out _));
	}
}
