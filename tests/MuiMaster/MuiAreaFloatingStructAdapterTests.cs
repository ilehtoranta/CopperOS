using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaFloatingStructAdapterTests
{
	[Fact]
	public void AreaFloatingStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3900);
		var value = new MuiAreaFloatingStateRecord
		{
			Magic = MuiAreaFloatingStateRecord.Cookie,
			Enabled = 1,
			Generation = 7,
		};

		Assert.True(MuiAreaFloatingStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaFloatingStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaFloatingStateField.Enabled,
			out var enabledAddress));
		Assert.Equal(0x3904u, enabledAddress.Raw);
		Assert.True(MuiAreaFloatingStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaFloatingStateField.Enabled, 0));
		Assert.True(MuiAreaFloatingStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0u, decoded.Enabled);
		Assert.False(MuiAreaFloatingStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaFloatingStateField)255, out _));
		Assert.False(MuiAreaFloatingStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaFloatingStateField.Magic, out _));
		Assert.False(MuiAreaFloatingStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}
}
