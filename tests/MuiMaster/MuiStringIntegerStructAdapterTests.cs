using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringIntegerStructAdapterTests
{
	[Fact]
	public void StringIntegerStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiStringIntegerStateRecord
		{
			Magic = MuiStringIntegerStateRecord.Cookie,
			Value = -123,
		};

		Assert.True(MuiStringIntegerStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiStringIntegerStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiStringIntegerStateField.Value,
			out var valueAddress));
		Assert.Equal(0x3504u, valueAddress.Raw);
		Assert.True(MuiStringIntegerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiStringIntegerStateField.Value,
			unchecked((uint)456)));
		Assert.True(MuiStringIntegerStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiStringIntegerStateField.Magic,
			out var magic) && magic == MuiStringIntegerStateRecord.Cookie);
		Assert.True(MuiStringIntegerStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(456, decoded.Value);
		Assert.Equal(MuiStringIntegerStateRecord.Cookie, decoded.Magic);
		Assert.False(MuiStringIntegerStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiStringIntegerStateField)255, out _));
		Assert.False(MuiStringIntegerStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF9),
			MuiStringIntegerStateField.Magic, out _));
		Assert.False(MuiStringIntegerStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiStringIntegerStateField.Magic, out _));
	}
}
