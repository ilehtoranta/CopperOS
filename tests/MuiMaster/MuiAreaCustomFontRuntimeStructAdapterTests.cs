using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaCustomFontRuntimeStructAdapterTests
{
	[Fact]
	public void CustomFontRuntimeStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3B00);
		var value = new MuiAreaCustomFontRuntimeRecord
		{
			Magic = MuiAreaCustomFontRuntimeRecord.Cookie,
			Font = APTR.FromPointer(0x2A00),
			Spec = APTR.FromPointer(0x1A00),
			Generation = 7,
			Active = 1,
		};

		Assert.True(MuiAreaCustomFontRuntimeRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiAreaCustomFontRuntimeRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaCustomFontRuntimeField.Spec,
			out var specAddress));
		Assert.Equal(0x3B08u, specAddress.Raw);
		Assert.True(MuiAreaCustomFontRuntimeRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaCustomFontRuntimeField.Active, 0));
		Assert.True(MuiAreaCustomFontRuntimeRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Font, decoded.Font);
		Assert.Equal(value.Spec, decoded.Spec);
		Assert.Equal(0u, decoded.Active);
		Assert.False(MuiAreaCustomFontRuntimeRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaCustomFontRuntimeField)255, out _));
		Assert.False(MuiAreaCustomFontRuntimeRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaCustomFontRuntimeField.Magic, out _));
		Assert.False(MuiAreaCustomFontRuntimeRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void CustomFontRuntimeSequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3BC0);
		var value = new MuiAreaCustomFontRuntimeRecord
		{
			Magic = MuiAreaCustomFontRuntimeRecord.Cookie,
			Font = APTR.FromPointer(0xFEEDBEEF),
			Spec = APTR.FromPointer(0xCAFEBABE),
			Generation = uint.MaxValue,
			Active = uint.MaxValue,
		};

		Assert.True(MuiAreaCustomFontRuntimeRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaCustomFontRuntimeRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Font, decoded.Font);
		Assert.Equal(value.Spec, decoded.Spec);
		Assert.Equal(value.Generation, decoded.Generation);
		Assert.Equal(value.Active, decoded.Active);

		var crossingEnd = APTR.FromPointer(0x30FED);
		Assert.False(MuiAreaCustomFontRuntimeRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiAreaCustomFontRuntimeRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}
}
