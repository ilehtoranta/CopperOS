using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaWeightStructAdapterTests
{
	[Fact]
	public void AreaWeightStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiAreaWeightStateRecord
		{
			Magic = MuiAreaWeightStateRecord.Cookie,
			Weight = uint.MaxValue,
		};

		Assert.True(MuiAreaWeightStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaWeightStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiAreaWeightStateField.Weight, out var weightAddress));
		Assert.Equal(0x3504u, weightAddress.Raw);
		Assert.True(MuiAreaWeightStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiAreaWeightStateField.Weight, out var weight));
		Assert.Equal(uint.MaxValue, weight);
		Assert.True(MuiAreaWeightStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiAreaWeightStateField.Weight, 0x12345678));
		Assert.True(MuiAreaWeightStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0x12345678u, decoded.Weight);
		Assert.False(MuiAreaWeightStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x30FF9), MuiAreaWeightStateField.Magic, out _));
		Assert.False(MuiAreaWeightStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiAreaWeightStateField.Magic, out _));
	}
}
