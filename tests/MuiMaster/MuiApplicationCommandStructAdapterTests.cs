using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationCommandStructAdapterTests
{
	[Fact]
	public void ApplicationCommandRecordUsesDedicatedNamedStructAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2B00);
		var value = new MuiApplicationCommandRecord
		{
			Name = APTR.FromPointer(0x7200),
			Template = APTR.FromPointer(0x7220),
			Parameters = -7,
			Hook = APTR.FromPointer(0x7240),
			Reserved0 = 1,
			Reserved1 = -2,
			Reserved2 = 3,
			Reserved3 = -4,
			Reserved4 = 5,
		};

		Assert.True(MuiApplicationCommandRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiApplicationCommandRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationCommandField.Hook,
			out var hookField));
		Assert.Equal(APTR.FromPointer(0x2B0C), hookField);
		Assert.True(MuiApplicationCommandRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationCommandField.Parameters,
			out var parameters));
		Assert.Equal(unchecked((uint)-7), parameters);
		Assert.True(MuiApplicationCommandRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationCommandField.Reserved4, 0xCAFEu));
		Assert.True(MuiApplicationCommandRecordCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.Name, decoded.Name);
		Assert.Equal(value.Hook, decoded.Hook);
		Assert.Equal(-7, decoded.Parameters);
		Assert.Equal(0xCAFE, decoded.Reserved4);
		Assert.False(MuiApplicationCommandRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x30FF0), out _));
		Assert.False(MuiApplicationCommandRecordCodec.Write(ref platform,
			APTR.FromPointer(0x30FF0), value));
		Assert.False(MuiApplicationCommandRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x30FF0), MuiApplicationCommandField.Name, out _));
		Assert.False(MuiApplicationCommandRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiApplicationCommandField.Template, out _));
	}

	[Fact]
	public void ApplicationCommandTableBridgeUsesCompleteNamedRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var table = APTR.FromPointer(0x2C00);
		var expected = new MuiApplicationCommandRecord
		{
			Name = APTR.FromPointer(0xF1234567u),
			Template = APTR.FromPointer(0xE2345678u),
			Parameters = -99,
			Hook = APTR.FromPointer(0xD3456789u),
			Reserved0 = 1,
			Reserved1 = -2,
			Reserved2 = 3,
			Reserved3 = -4,
			Reserved4 = 0x55667788,
		};

		Assert.True(MuiApplicationCommandTableCodec.TryWrite(ref platform, table,
			2, expected));
		Assert.True(MuiApplicationCommandTableCodec.TryRead(ref platform, table, 2,
			out var actual));
		Assert.Equal(expected.Name, actual.Name);
		Assert.Equal(expected.Template, actual.Template);
		Assert.Equal(expected.Parameters, actual.Parameters);
		Assert.Equal(expected.Hook, actual.Hook);
		Assert.Equal(expected.Reserved4, actual.Reserved4);

		Assert.False(MuiApplicationCommandTableCodec.TryRead(ref platform, table,
			0x10000000u, out _));
		Assert.False(MuiApplicationCommandTableCodec.TryRead(ref platform,
			APTR.FromPointer(0x30FF0u), 0, out _));
		Assert.False(MuiApplicationCommandTableCodec.TryWrite(ref platform,
			APTR.Null, 0, expected));
	}
}
