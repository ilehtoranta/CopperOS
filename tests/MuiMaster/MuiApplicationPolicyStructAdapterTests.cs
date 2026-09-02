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

	[Fact]
	public void ApplicationPolicySequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x35C0);
		var value = new MuiApplicationPolicyStateRecord
		{
			Magic = MuiApplicationPolicyStateRecord.Cookie,
			UseRexx = uint.MaxValue,
			UseCommodities = 0x01020304u,
			UseScreenNotify = 0xAABBCCDDu,
		};

		Assert.True(MuiApplicationPolicyStateRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiApplicationPolicyStateRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.UseRexx, decoded.UseRexx);
		Assert.Equal(value.UseCommodities, decoded.UseCommodities);
		Assert.Equal(value.UseScreenNotify, decoded.UseScreenNotify);

		var crossingEnd = APTR.FromPointer(0x30FF1);
		Assert.False(MuiApplicationPolicyStateRecordCodec.WriteRecord(
			ref platform, crossingEnd, value));
		Assert.False(MuiApplicationPolicyStateRecordCodec.TryReadRecord(
			ref platform, crossingEnd, out _));
	}

	[Fact]
	public void ApplicationPolicyFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3640);
		var initial = new MuiApplicationPolicyStateRecord
		{
			Magic = 0x10203040u,
			UseRexx = 1,
			UseCommodities = 0,
			UseScreenNotify = 1,
		};

		Assert.True(MuiApplicationPolicyStateRecordCodec.WriteRecord(
			ref platform, address, initial));
		Assert.True(MuiApplicationPolicyStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationPolicyStateField.UseCommodities,
			1));
		Assert.True(MuiApplicationPolicyStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationPolicyStateField.UseRexx,
			out var useRexx));
		Assert.Equal(initial.UseRexx, useRexx);
		Assert.True(MuiApplicationPolicyStateRecordCodec.TryReadStructural(
			ref platform, address, out var updated));
		Assert.Equal(initial.Magic, updated.Magic);
		Assert.Equal(initial.UseRexx, updated.UseRexx);
		Assert.Equal(1u, updated.UseCommodities);
		Assert.Equal(initial.UseScreenNotify, updated.UseScreenNotify);
		Assert.False(MuiApplicationPolicyStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address,
			unchecked((MuiApplicationPolicyStateField)255), 1));
	}
}
