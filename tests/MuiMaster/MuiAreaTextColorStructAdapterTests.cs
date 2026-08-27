using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaTextColorStructAdapterTests
{
	[Fact]
	public void AreaTextColorStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiAreaTextColorStateRecord
		{
			Magic = MuiAreaTextColorStateRecord.Cookie,
			Color = 0x00C0FFEE,
			Active = 1,
			Generation = 7,
		};

		Assert.True(MuiAreaTextColorStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaTextColorStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaTextColorStateField.Color,
			out var colorAddress));
		Assert.Equal(0x3504u, colorAddress.Raw);
		Assert.True(MuiAreaTextColorStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaTextColorStateField.Color, 0x00112233));
		Assert.True(MuiAreaTextColorStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0x00112233u, decoded.Color);
		Assert.False(MuiAreaTextColorStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaTextColorStateField)255, out _));
		Assert.False(MuiAreaTextColorStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaTextColorStateField.Magic, out _));
		Assert.False(MuiAreaTextColorStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}
}
