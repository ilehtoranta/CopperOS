using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaContextMenuStructAdapterTests
{
	[Fact]
	public void AreaContextMenuStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiAreaContextMenuStateRecord
		{
			Magic = MuiAreaContextMenuStateRecord.Cookie,
			MenuStrip = APTR.FromPointer(0x1234),
			Trigger = APTR.FromPointer(0x5678),
			Generation = 7,
		};

		Assert.True(MuiAreaContextMenuStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaContextMenuStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaContextMenuStateField.Trigger,
			out var triggerAddress));
		Assert.Equal(0x3508u, triggerAddress.Raw);
		Assert.True(MuiAreaContextMenuStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaContextMenuStateField.Trigger, 0xABCDEF01));
		Assert.True(MuiAreaContextMenuStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0xABCDEF01u, decoded.Trigger.Raw);
		Assert.False(MuiAreaContextMenuStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaContextMenuStateField)255, out _));
		Assert.False(MuiAreaContextMenuStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaContextMenuStateField.Magic, out _));
		Assert.False(MuiAreaContextMenuStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}
}
