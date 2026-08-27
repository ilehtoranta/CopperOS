using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiChoiceActiveStructAdapterTests
{
	[Fact]
	public void ChoiceActiveStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiChoiceActiveStateRecord
		{
			Magic = MuiChoiceActiveStateRecord.Cookie,
			Active = 7,
		};

		Assert.True(MuiChoiceActiveStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiChoiceActiveStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiChoiceActiveStateField.Active,
			out var activeAddress));
		Assert.Equal(0x3504u, activeAddress.Raw);
		Assert.True(MuiChoiceActiveStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiChoiceActiveStateField.Active, 8));
		Assert.True(MuiChoiceActiveStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(8u, decoded.Active);
		Assert.False(MuiChoiceActiveStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF9),
			MuiChoiceActiveStateField.Magic, out _));
		Assert.False(MuiChoiceActiveStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiChoiceActiveStateField.Magic, out _));
	}
}
