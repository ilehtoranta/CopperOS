using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiChoiceAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ChoiceActiveAndEntriesRecordsRoundTripThroughNamedStructs()
	{
		var platform = CreatePlatform();
		var activeAddress = APTR.FromPointer(0x1500);
		var entriesAddress = APTR.FromPointer(0x1520);
		var entries = APTR.FromPointer(0x1800);
		var text = APTR.FromPointer(0x1840);
		platform.WriteCString(text, "First");
		platform.WriteUInt32(entries, 0, text.Raw);
		platform.WriteUInt32(entries, 4, 0);
		var active = new MuiChoiceActiveStateRecord
		{
			Magic = MuiChoiceActiveStateRecord.Cookie,
			Active = 1,
		};
		var choiceEntries = new MuiChoiceEntriesStateRecord
		{
			Magic = MuiChoiceEntriesStateRecord.Cookie,
			Entries = entries,
		};

		Assert.True(MuiChoiceActiveStateRecordCodec.Write(ref platform,
			activeAddress, active));
		Assert.True(MuiChoiceEntriesStateRecordCodec.Write(ref platform,
			entriesAddress, choiceEntries));
		Assert.True(MuiChoiceActiveStateRecordCodec.TryRead(ref platform,
			activeAddress, out var activeRead));
		Assert.True(MuiChoiceEntriesStateRecordCodec.TryRead(ref platform,
			entriesAddress, out var entriesRead));
		Assert.Equal(active.Active, activeRead.Active);
		Assert.Equal(choiceEntries.Entries, entriesRead.Entries);
	}

	[Fact]
	public void ChoiceActiveRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1BC0);
		var value = new MuiChoiceActiveStateRecord
		{
			Magic = MuiChoiceActiveStateRecord.Cookie,
			Active = 0x7FFFFFFFu,
		};

		Assert.True(MuiChoiceActiveStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiChoiceActiveStateRecordCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.Active, structural.Active);
		Assert.True(MuiChoiceActiveStateRecordCodec.TryRead(ref platform, address,
			out _));
		Assert.True(MuiChoiceActiveStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiChoiceActiveStateField.Active,
			out var activeField));
		Assert.Equal(address.Raw + 4, activeField.Raw);
		Assert.True(MuiChoiceActiveStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiChoiceActiveStateField.Magic,
			out var magic));
		Assert.Equal(value.Magic, magic);
		Assert.False(MuiChoiceActiveStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiChoiceActiveStateField)255, out _));
		Assert.False(MuiChoiceActiveStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiChoiceActiveStateField.Magic, out _));
		Assert.False(MuiChoiceActiveStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void ChoiceEntriesRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1BE0);
		var entries = APTR.FromPointer(0x1C40);
		var text = APTR.FromPointer(0x1D00);
		platform.WriteCString(text, "Entry");
		platform.WriteUInt32(entries, 0, text.Raw);
		platform.WriteUInt32(entries, 4, 0);
		var value = new MuiChoiceEntriesStateRecord
		{
			Magic = MuiChoiceEntriesStateRecord.Cookie,
			Entries = entries,
		};

		Assert.True(MuiChoiceEntriesStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiChoiceEntriesStateRecordCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.Entries, structural.Entries);
		Assert.True(MuiChoiceEntriesStateRecordCodec.TryRead(ref platform, address,
			out _));
		Assert.True(MuiChoiceEntriesStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiChoiceEntriesStateField.Entries,
			out var entriesField));
		Assert.Equal(address.Raw + 4, entriesField.Raw);
		Assert.True(MuiChoiceEntriesStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiChoiceEntriesStateField.Entries,
			out var entriesRaw));
		Assert.Equal(value.Entries.Raw, entriesRaw);
		Assert.False(MuiChoiceEntriesStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiChoiceEntriesStateField)255, out _));
		Assert.False(MuiChoiceEntriesStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiChoiceEntriesStateField.Magic, out _));
		Assert.False(MuiChoiceEntriesStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void ChoiceEntriesAdmissionConsumesCompleteNamedVectorEntries()
	{
		var platform = CreatePlatform();
		var vector = APTR.FromPointer(0x1E40);
		var state = new MuiChoiceEntriesState
		{
			Entries = vector,
		};

		Assert.True(MuiChoiceEntryVectorCodec.TryWrite(ref platform, vector, 0,
			new MuiChoiceEntry { Text = APTR.FromPointer(0xFEDCBA98u) }));
		Assert.True(MuiChoiceEntryVectorCodec.TryWrite(ref platform, vector, 1,
			new MuiChoiceEntry { Text = APTR.Null }));
		Assert.True(MuiChoiceEntriesStateAdmission.Validate(ref platform, state));

		state.Entries = APTR.FromPointer(0x20FFE);
		Assert.False(MuiChoiceEntriesStateAdmission.Validate(ref platform, state));
	}

	[Fact]
	public void MalformedChoiceMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreatePlatform();
		var activeAddress = APTR.FromPointer(0x1600);
		var entriesAddress = APTR.FromPointer(0x1620);
		Assert.True(MuiChoiceActiveStateRecordCodec.Write(ref platform,
			activeAddress, new MuiChoiceActiveStateRecord
			{
				Magic = MuiChoiceActiveStateRecord.Cookie,
				Active = 1,
			}));
		Assert.True(MuiChoiceEntriesStateRecordCodec.Write(ref platform,
			entriesAddress, new MuiChoiceEntriesStateRecord
			{
				Magic = MuiChoiceEntriesStateRecord.Cookie,
				Entries = APTR.Null,
			}));
		Assert.True(MuiChoiceActiveStateFieldCursorCodec.TryWriteUInt32(
			ref platform, activeAddress, MuiChoiceActiveStateField.Magic, 0));
		Assert.True(MuiChoiceEntriesStateFieldCursorCodec.TryWriteUInt32(
			ref platform, entriesAddress, MuiChoiceEntriesStateField.Magic, 0));

		Assert.True(MuiChoiceActiveStateRecordCodec.TryReadStructural(ref platform,
			activeAddress, out var active));
		Assert.True(MuiChoiceEntriesStateRecordCodec.TryReadStructural(ref platform,
			entriesAddress, out var entries));
		Assert.Equal(0u, active.Magic);
		Assert.Equal(0u, entries.Magic);
		Assert.False(MuiChoiceActiveStateRecordCodec.TryRead(ref platform,
			activeAddress, out _));
		Assert.False(MuiChoiceEntriesStateRecordCodec.TryRead(ref platform,
			entriesAddress, out _));
		Assert.False(MuiChoiceActiveStateAdmission.Validate(active));
		Assert.False(MuiChoiceEntriesStateAdmission.Validate(ref platform,
			entries));
	}

	private static MuiHeadlessTestPlatform CreatePlatform() =>
		new(0x1000, 0x20000, 0x4000, State);
}
