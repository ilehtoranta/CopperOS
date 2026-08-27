using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationPolicyStructAdapterTests
{
	[Fact]
	public void ApplicationPolicyStateUsesDedicatedNamedStructAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiApplicationPolicyStateRecord
		{
			Magic = MuiApplicationPolicyStateRecord.Cookie,
			UseRexx = 1,
			UseCommodities = 0,
			UseScreenNotify = 1,
		};

		Assert.True(MuiApplicationPolicyStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationPolicyStateField.UseCommodities,
			out var commoditiesField));
		Assert.Equal(APTR.FromPointer(0x3508), commoditiesField);
		Assert.True(MuiApplicationPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationPolicyStateField.UseScreenNotify,
			out var notifyField));
		Assert.Equal(APTR.FromPointer(0x350C), notifyField);
		Assert.True(MuiApplicationPolicyStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationPolicyStateField.UseCommodities, 1));
		Assert.True(MuiApplicationPolicyStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.UseRexx, decoded.UseRexx);
		Assert.Equal(1u, decoded.UseCommodities);
		Assert.Equal(value.UseScreenNotify, decoded.UseScreenNotify);
		Assert.False(MuiApplicationPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF8),
			MuiApplicationPolicyStateField.Magic, out _));
		Assert.False(MuiApplicationPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
			MuiApplicationPolicyStateField.UseRexx, out _));
	}
}
