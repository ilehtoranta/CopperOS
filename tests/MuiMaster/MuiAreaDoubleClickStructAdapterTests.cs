using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaDoubleClickStructAdapterTests
{
	[Fact]
	public void AreaDoubleClickStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiAreaDoubleClickStateRecord
		{
			Magic = MuiAreaDoubleClickStateRecord.Cookie,
			Value = -123,
			Generation = 7,
		};

		Assert.True(MuiAreaDoubleClickStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaDoubleClickStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaDoubleClickStateField.Value,
			out var valueAddress));
		Assert.Equal(0x3504u, valueAddress.Raw);
		Assert.True(MuiAreaDoubleClickStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaDoubleClickStateField.Value,
			unchecked((uint)456)));
		Assert.True(MuiAreaDoubleClickStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(456, decoded.Value);
		Assert.False(MuiAreaDoubleClickStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaDoubleClickStateField)255, out _));
		Assert.False(MuiAreaDoubleClickStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaDoubleClickStateField.Magic, out _));
		Assert.False(MuiAreaDoubleClickStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}
}
