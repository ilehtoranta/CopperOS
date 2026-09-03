using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationCommandsStateStructAdapterTests
{
	[Fact]
	public void ApplicationCommandsStateUsesDedicatedNamedStructAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2B80);
		var value = new MuiApplicationCommandsStateRecord
		{
			Magic = MuiApplicationCommandsStateRecord.Cookie,
			Table = APTR.Null,
		};

		Assert.True(MuiApplicationCommandsStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiApplicationCommandsStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationCommandsStateField.Magic,
			out var magicField));
		Assert.Equal(APTR.FromPointer(0x2B80), magicField);
		Assert.True(MuiApplicationCommandsStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationCommandsStateField.Table,
			out var tableField));
		Assert.Equal(APTR.FromPointer(0x2B84), tableField);
		Assert.True(MuiApplicationCommandsStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationCommandsStateField.Magic,
			out var magic));
		Assert.Equal(MuiApplicationCommandsStateRecord.Cookie, magic);
		Assert.True(MuiApplicationCommandsStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationCommandsStateField.Table,
			0x2C00u));
		Assert.True(MuiApplicationCommandsStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(APTR.FromPointer(0x2C00), decoded.Table);
		Assert.False(MuiApplicationCommandsStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FFC),
			MuiApplicationCommandsStateField.Magic, out _));
		Assert.False(MuiApplicationCommandsStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiApplicationCommandsStateField.Table,
			out _));
	}

	[Fact]
	public void ApplicationCommandsStateSequentialRecordPreservesPointerAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2BC0);
		var value = new MuiApplicationCommandsStateRecord
		{
			Magic = MuiApplicationCommandsStateRecord.Cookie,
			Table = APTR.FromPointer(uint.MaxValue),
		};

		Assert.True(MuiApplicationCommandsStateRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiApplicationCommandsStateRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Table, decoded.Table);

		var crossingEnd = APTR.FromPointer(0x30FF9);
		Assert.False(MuiApplicationCommandsStateRecordCodec.WriteRecord(
			ref platform, crossingEnd, value));
		Assert.False(MuiApplicationCommandsStateRecordCodec.TryReadRecord(
			ref platform, crossingEnd, out _));
	}

	[Fact]
	public void ApplicationCommandsStateFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2C80);
		var initial = new MuiApplicationCommandsStateRecord
		{
			Magic = 0x10203040u,
			Table = APTR.FromPointer(0xF1020304u),
		};

		Assert.True(MuiApplicationCommandsStateRecordCodec.WriteRecord(ref platform,
			address, initial));
		Assert.True(MuiApplicationCommandsStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationCommandsStateField.Magic,
			MuiApplicationCommandsStateRecord.Cookie));
		Assert.True(MuiApplicationCommandsStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationCommandsStateField.Table,
			out var table));
		Assert.Equal(initial.Table.Raw, table);
		Assert.True(MuiApplicationCommandsStateRecordCodec.TryReadStructural(
			ref platform, address, out var updated));
		Assert.Equal(MuiApplicationCommandsStateRecord.Cookie, updated.Magic);
		Assert.Equal(initial.Table, updated.Table);
		Assert.False(MuiApplicationCommandsStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, unchecked((MuiApplicationCommandsStateField)255),
			1));
	}

	[Fact]
	public void ApplicationCommandFieldAccessUsesCompleteNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2D00);
		var initial = new MuiApplicationCommandRecord
		{
			Name = APTR.FromPointer(0x1800),
			Template = APTR.FromPointer(0x1820),
			Parameters = -7,
			Hook = APTR.FromPointer(0x1840),
			Reserved0 = 0x10203040,
			Reserved1 = -2,
			Reserved2 = 3,
			Reserved3 = -4,
			Reserved4 = 0x55667788,
		};

		Assert.True(MuiApplicationCommandRecordCodec.WriteStructural(ref platform,
			address, initial));
		Assert.True(MuiApplicationCommandRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationCommandField.Parameters,
			unchecked((uint)-19)));
		Assert.True(MuiApplicationCommandRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationCommandField.Hook, 0x1860u));
		Assert.True(MuiApplicationCommandRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationCommandField.Name, out var name));
		Assert.Equal(initial.Name.Raw, name);
		Assert.True(MuiApplicationCommandRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationCommandField.Parameters,
			out var parameters));
		Assert.Equal(unchecked((uint)-19), parameters);
		Assert.True(MuiApplicationCommandRecordCodec.TryReadStructural(ref platform,
			address, out var updated));
		Assert.Equal(APTR.FromPointer(0x1860), updated.Hook);
		Assert.Equal(initial.Reserved4, updated.Reserved4);
		Assert.False(MuiApplicationCommandRecordMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FFC),
			MuiApplicationCommandField.Name, out _));
		Assert.False(MuiApplicationCommandRecordMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null, MuiApplicationCommandField.Template, 1));
		Assert.False(MuiApplicationCommandRecordMemoryCodec.TryReadUInt32(
			ref platform, address, unchecked((MuiApplicationCommandField)255),
			out _));
	}
}
