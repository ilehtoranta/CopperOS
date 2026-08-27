using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiChoiceEntriesStructAdapterTests
{
	[Fact]
	public void ChoiceEntriesStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiChoiceEntriesStateRecord
		{
			Magic = MuiChoiceEntriesStateRecord.Cookie,
			Entries = APTR.Null,
		};

		Assert.True(MuiChoiceEntriesStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiChoiceEntriesStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiChoiceEntriesStateField.Entries,
			out var entriesAddress));
		Assert.Equal(0x3504u, entriesAddress.Raw);
		Assert.True(MuiChoiceEntriesStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiChoiceEntriesStateField.Entries,
			0x12345678u));
		Assert.True(MuiChoiceEntriesStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(0x12345678u, decoded.Entries.Raw);
		Assert.False(MuiChoiceEntriesStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF9),
			MuiChoiceEntriesStateField.Magic, out _));
		Assert.False(MuiChoiceEntriesStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiChoiceEntriesStateField.Magic, out _));
	}
}
