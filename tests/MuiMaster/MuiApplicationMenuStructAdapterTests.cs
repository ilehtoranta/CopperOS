using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationMenuStructAdapterTests
{
	[Fact]
	public void ApplicationMenuStateUsesDedicatedNamedStructAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiApplicationMenuStateRecord
		{
			Magic = MuiApplicationMenuStateRecord.Cookie,
			MenuAction = 0x80420001,
			MenuHelp = 0x12345678,
		};

		Assert.True(MuiApplicationMenuStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiApplicationMenuStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationMenuStateField.MenuAction,
			out var actionField));
		Assert.Equal(APTR.FromPointer(0x3504), actionField);
		Assert.True(MuiApplicationMenuStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationMenuStateField.MenuHelp,
			out var helpField));
		Assert.Equal(APTR.FromPointer(0x3508), helpField);
		Assert.True(MuiApplicationMenuStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationMenuStateField.MenuHelp,
			0xCAFEBABE));
		Assert.True(MuiApplicationMenuStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.MenuAction, decoded.MenuAction);
		Assert.Equal(0xCAFEBABEu, decoded.MenuHelp);
		Assert.False(MuiApplicationMenuStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF8),
			MuiApplicationMenuStateField.Magic, out _));
		Assert.False(MuiApplicationMenuStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
			MuiApplicationMenuStateField.MenuAction, out _));
	}
}
