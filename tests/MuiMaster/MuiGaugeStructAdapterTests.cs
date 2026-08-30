using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGaugeStructAdapterTests
{
	[Fact]
	public void GaugeStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiGaugeStateRecord
		{
			Magic = MuiGaugeStateRecord.Cookie,
			Maximum = uint.MaxValue,
			Current = 17,
			Divide = 3,
			Horizontal = 1,
		};

		Assert.True(MuiGaugeStateRecordCodec.Write(ref platform, address, value));
		Assert.True(MuiGaugeStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiGaugeStateField.Horizontal, out var horizontalAddress));
		Assert.Equal(0x3510u, horizontalAddress.Raw);
		Assert.True(MuiGaugeStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiGaugeStateField.Current, out var current));
		Assert.Equal(17u, current);
		Assert.True(MuiGaugeStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiGaugeStateField.Divide, 0));
		Assert.True(MuiGaugeStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0u, decoded.Divide);
		Assert.False(MuiGaugeStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x30FED), MuiGaugeStateField.Magic, out _));
		Assert.False(MuiGaugeStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiGaugeStateField.Magic, out _));
	}

	[Fact]
	public void GaugeSequentialRecordPreservesProgressAndRejectsTruncation()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3600);
		var expected = new MuiGaugeStateRecord
		{
			Magic = MuiGaugeStateRecord.Cookie,
			Maximum = 100,
			Current = 17,
			Divide = 3,
			Horizontal = 1,
		};
		Assert.True(MuiGaugeStateRecordCodec.WriteRecord(ref platform, address,
			expected));
		Assert.True(MuiGaugeStateRecordCodec.TryReadRecord(ref platform, address,
			out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Maximum, actual.Maximum);
		Assert.Equal(expected.Current, actual.Current);
		Assert.Equal(expected.Divide, actual.Divide);
		Assert.Equal(expected.Horizontal, actual.Horizontal);
		Assert.False(MuiGaugeStateRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FFC), out _));
	}
}
