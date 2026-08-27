using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaCycleChainStructAdapterTests
{
	[Fact]
	public void AreaCycleChainStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3700);
		var value = new MuiAreaCycleChainStateRecord
		{
			Magic = MuiAreaCycleChainStateRecord.Cookie,
			Value = -123,
			Generation = 7,
		};

		Assert.True(MuiAreaCycleChainStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaCycleChainStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaCycleChainStateField.Value,
			out var valueAddress));
		Assert.Equal(0x3704u, valueAddress.Raw);
		Assert.True(MuiAreaCycleChainStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaCycleChainStateField.Value,
			unchecked((uint)456)));
		Assert.True(MuiAreaCycleChainStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(456, decoded.Value);
		Assert.False(MuiAreaCycleChainStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaCycleChainStateField)255, out _));
		Assert.False(MuiAreaCycleChainStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaCycleChainStateField.Magic, out _));
		Assert.False(MuiAreaCycleChainStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}
}
