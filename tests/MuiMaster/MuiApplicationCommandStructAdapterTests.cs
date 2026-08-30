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
}
