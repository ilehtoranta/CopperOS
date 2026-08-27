using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNumericStructAdapterTests
{
	[Fact]
	public void NumericStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiNumericStateRecord
		{
			Magic = MuiNumericStateRecord.Cookie,
			Minimum = unchecked((uint)-10),
			Maximum = 100,
			Value = 35,
			Default = 50,
			Reverse = 1,
		};

		Assert.True(MuiNumericStateRecordCodec.Write(ref platform, address, value));
		Assert.True(MuiNumericStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiNumericStateField.Value, out var valueAddress));
		Assert.Equal(0x350Cu, valueAddress.Raw);
		Assert.True(MuiNumericStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiNumericStateField.Minimum, out var minimum));
		Assert.Equal(unchecked((uint)-10), minimum);
		Assert.True(MuiNumericStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiNumericStateField.Value, 42));
		Assert.True(MuiNumericStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(42u, decoded.Value);
		Assert.False(MuiNumericStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, (MuiNumericStateField)255, out _));
		Assert.False(MuiNumericStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiNumericStateField.Magic, out _));
		Assert.False(MuiNumericStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}
}
