using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaControlCharStructAdapterTests
{
	[Fact]
	public void AreaControlCharStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiAreaControlCharStateRecord
		{
			Magic = MuiAreaControlCharStateRecord.Cookie,
			Character = 0x41,
			Generation = 7,
		};

		Assert.True(MuiAreaControlCharStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaControlCharStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaControlCharStateField.Character,
			out var characterAddress));
		Assert.Equal(0x3504u, characterAddress.Raw);
		Assert.True(MuiAreaControlCharStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaControlCharStateField.Character, 0x42));
		Assert.True(MuiAreaControlCharStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0x42u, decoded.Character);
		Assert.False(MuiAreaControlCharStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaControlCharStateField)255, out _));
		Assert.False(MuiAreaControlCharStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaControlCharStateField.Magic, out _));
		Assert.False(MuiAreaControlCharStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}
}
