using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaShortHelpStructAdapterTests
{
	[Fact]
	public void AreaShortHelpStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3600);
		var value = new MuiAreaShortHelpStateRecord
		{
			Magic = MuiAreaShortHelpStateRecord.Cookie,
			Text = APTR.FromPointer(0x1A00),
			Generation = 7,
		};

		Assert.True(MuiAreaShortHelpStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaShortHelpStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaShortHelpStateField.Text,
			out var textAddress));
		Assert.Equal(0x3604u, textAddress.Raw);
		Assert.True(MuiAreaShortHelpStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(value.Text, decoded.Text);
		Assert.False(MuiAreaShortHelpStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaShortHelpStateField)255, out _));
		Assert.False(MuiAreaShortHelpStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaShortHelpStateField.Magic, out _));
		Assert.False(MuiAreaShortHelpStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void AreaShortHelpSequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x36C0);
		var value = new MuiAreaShortHelpStateRecord
		{
			Magic = MuiAreaShortHelpStateRecord.Cookie,
			Text = APTR.FromPointer(0xFEEDBEEF),
			Generation = uint.MaxValue,
		};

		Assert.True(MuiAreaShortHelpStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaShortHelpStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Text, decoded.Text);
		Assert.Equal(value.Generation, decoded.Generation);

		var crossingEnd = APTR.FromPointer(0x30FF5);
		Assert.False(MuiAreaShortHelpStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiAreaShortHelpStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}

	[Fact]
	public void AreaShortHelpFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3F40);
		var value = new MuiAreaShortHelpStateRecord
		{
			Magic = MuiAreaShortHelpStateRecord.Cookie,
			Text = APTR.FromPointer(0x4100),
			Generation = 19,
		};

		Assert.True(MuiAreaShortHelpStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaShortHelpStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaShortHelpStateField.Text, 0xFEEDBEEFu));
		Assert.True(MuiAreaShortHelpStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiAreaShortHelpStateField.Generation,
			out var generation));
		Assert.Equal(value.Generation, generation);
		Assert.True(MuiAreaShortHelpStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(0xFEEDBEEFu, decoded.Text.Raw);
		Assert.Equal(value.Generation, decoded.Generation);
	}
}
