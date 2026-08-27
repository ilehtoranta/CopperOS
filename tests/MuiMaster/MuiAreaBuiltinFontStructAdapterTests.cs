using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaBuiltinFontStructAdapterTests
{
	[Fact]
	public void AreaBuiltinFontStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiAreaBuiltinFontStateRecord
		{
			Magic = MuiAreaBuiltinFontStateRecord.Cookie,
			Selector = unchecked((uint)-1),
			Present = 1,
			Generation = 7,
		};

		Assert.True(MuiAreaBuiltinFontStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaBuiltinFontStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaBuiltinFontStateField.Selector,
			out var selectorAddress));
		Assert.Equal(0x3504u, selectorAddress.Raw);
		Assert.True(MuiAreaBuiltinFontStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaBuiltinFontStateField.Selector, 2));
		Assert.True(MuiAreaBuiltinFontStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(2u, decoded.Selector);
		Assert.False(MuiAreaBuiltinFontStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaBuiltinFontStateField)255, out _));
		Assert.False(MuiAreaBuiltinFontStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaBuiltinFontStateField.Magic, out _));
		Assert.False(MuiAreaBuiltinFontStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}
}
