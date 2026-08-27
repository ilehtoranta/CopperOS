using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaDoubleBufferStructAdapterTests
{
	[Fact]
	public void AreaDoubleBufferStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3800);
		var value = new MuiAreaDoubleBufferStateRecord
		{
			Magic = MuiAreaDoubleBufferStateRecord.Cookie,
			Enabled = 1,
			Generation = 7,
		};

		Assert.True(MuiAreaDoubleBufferStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaDoubleBufferStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaDoubleBufferStateField.Enabled,
			out var enabledAddress));
		Assert.Equal(0x3804u, enabledAddress.Raw);
		Assert.True(MuiAreaDoubleBufferStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaDoubleBufferStateField.Enabled, 0));
		Assert.True(MuiAreaDoubleBufferStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0u, decoded.Enabled);
		Assert.False(MuiAreaDoubleBufferStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaDoubleBufferStateField)255, out _));
		Assert.False(MuiAreaDoubleBufferStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaDoubleBufferStateField.Magic, out _));
		Assert.False(MuiAreaDoubleBufferStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}
}
