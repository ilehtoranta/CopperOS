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
		var entriesCursor = new MuiChoiceEntriesStateFieldCursor
		{
			Record = address,
			Field = MuiChoiceEntriesStateField.Entries,
		};
		Assert.True(MuiChoiceEntriesStateFieldCursorCodec.TryGetAddress(
			ref platform, entriesCursor, out var cursorEntriesAddress,
			out var fieldSize));
		Assert.Equal(entriesAddress, cursorEntriesAddress);
		Assert.Equal(MuiChoiceEntriesStateRecord.FieldSize, fieldSize);
		Assert.True(MuiChoiceEntriesStateRecordMemoryCodec.TryGetAddress(
			ref platform, entriesCursor, out var memoryCursorAddress,
			out var memoryFieldSize));
		Assert.Equal(cursorEntriesAddress, memoryCursorAddress);
		Assert.Equal(fieldSize, memoryFieldSize);
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
		entriesCursor.Record = APTR.Null;
		Assert.False(MuiChoiceEntriesStateFieldCursorCodec.TryGetAddress(
			ref platform, entriesCursor, out _, out _));
	}

	[Fact]
	public void ChoiceEntriesSequentialRecordPreservesPointerAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3580);
		var value = new MuiChoiceEntriesStateRecord
		{
			Magic = MuiChoiceEntriesStateRecord.Cookie,
			Entries = APTR.FromPointer(0xFEEDBEEF),
		};

		Assert.True(MuiChoiceEntriesStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiChoiceEntriesStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Entries, decoded.Entries);

		var crossingEnd = APTR.FromPointer(0x30FFD);
		Assert.False(MuiChoiceEntriesStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiChoiceEntriesStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}
}
