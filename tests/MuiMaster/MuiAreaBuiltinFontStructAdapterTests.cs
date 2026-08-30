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

	[Fact]
	public void AreaBuiltinFontSequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x35C0);
		var value = new MuiAreaBuiltinFontStateRecord
		{
			Magic = MuiAreaBuiltinFontStateRecord.Cookie,
			Selector = 0x80000000u,
			Present = uint.MaxValue,
			Generation = uint.MaxValue,
		};

		Assert.True(MuiAreaBuiltinFontStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaBuiltinFontStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Selector, decoded.Selector);
		Assert.Equal(value.Present, decoded.Present);
		Assert.Equal(value.Generation, decoded.Generation);

		var crossingEnd = APTR.FromPointer(0x30FF1);
		Assert.False(MuiAreaBuiltinFontStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiAreaBuiltinFontStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}
}
