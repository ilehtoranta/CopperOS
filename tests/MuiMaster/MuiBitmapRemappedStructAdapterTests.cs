using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiBitmapRemappedStructAdapterTests
{
	[Fact]
	public void BitmapRemappedStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiBitmapRemappedStateRecord
		{
			Magic = MuiBitmapRemappedStateRecord.Cookie,
			Remapped = APTR.Null,
		};

		Assert.True(MuiBitmapRemappedStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiBitmapRemappedStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiBitmapRemappedStateField.Remapped,
			out var remappedAddress));
		Assert.Equal(0x3504u, remappedAddress.Raw);
		Assert.True(MuiBitmapRemappedStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiBitmapRemappedStateField.Remapped,
			0x12345678));
		Assert.True(MuiBitmapRemappedStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(0x12345678u, decoded.Remapped.Raw);
		Assert.False(MuiBitmapRemappedStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF9),
			MuiBitmapRemappedStateField.Magic, out _));
		Assert.False(MuiBitmapRemappedStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiBitmapRemappedStateField.Magic, out _));
	}
}
