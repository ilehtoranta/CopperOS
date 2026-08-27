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
}
