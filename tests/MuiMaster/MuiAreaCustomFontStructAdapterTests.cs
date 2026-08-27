using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaCustomFontStructAdapterTests
{
	[Fact]
	public void AreaCustomFontStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3A00);
		var value = new MuiAreaCustomFontStateRecord
		{
			Magic = MuiAreaCustomFontStateRecord.Cookie,
			Spec = APTR.FromPointer(0x1A00),
			Present = 1,
			Generation = 7,
		};

		Assert.True(MuiAreaCustomFontStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaCustomFontStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaCustomFontStateField.Spec,
			out var specAddress));
		Assert.Equal(0x3A04u, specAddress.Raw);
		Assert.True(MuiAreaCustomFontStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(value.Spec, decoded.Spec);
		Assert.Equal(value.Present, decoded.Present);
		Assert.False(MuiAreaCustomFontStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaCustomFontStateField)255, out _));
		Assert.False(MuiAreaCustomFontStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaCustomFontStateField.Magic, out _));
		Assert.False(MuiAreaCustomFontStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}
}
