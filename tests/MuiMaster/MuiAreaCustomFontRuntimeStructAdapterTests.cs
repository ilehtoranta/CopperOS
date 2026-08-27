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
}
