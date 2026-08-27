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
}
