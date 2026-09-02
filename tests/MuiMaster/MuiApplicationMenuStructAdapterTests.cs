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

	[Fact]
	public void ApplicationMenuStateSequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x35C0);
		var value = new MuiApplicationMenuStateRecord
		{
			Magic = MuiApplicationMenuStateRecord.Cookie,
			MenuAction = uint.MaxValue,
			MenuHelp = 0xDEADBEEFu,
		};

		Assert.True(MuiApplicationMenuStateRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiApplicationMenuStateRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.MenuAction, decoded.MenuAction);
		Assert.Equal(value.MenuHelp, decoded.MenuHelp);

		var crossingEnd = APTR.FromPointer(0x30FF5);
		Assert.False(MuiApplicationMenuStateRecordCodec.WriteRecord(
			ref platform, crossingEnd, value));
		Assert.False(MuiApplicationMenuStateRecordCodec.TryReadRecord(
			ref platform, crossingEnd, out _));
	}

	[Fact]
	public void ApplicationMenuStateFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3640);
		var initial = new MuiApplicationMenuStateRecord
		{
			Magic = 0x10203040u,
			MenuAction = 0x50607080u,
			MenuHelp = 0x90A0B0C0u,
		};

		Assert.True(MuiApplicationMenuStateRecordCodec.WriteRecord(ref platform,
			address, initial));
		Assert.True(MuiApplicationMenuStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationMenuStateField.MenuAction,
			0xF1020304u));
		Assert.True(MuiApplicationMenuStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationMenuStateField.MenuHelp,
			out var help));
		Assert.Equal(initial.MenuHelp, help);
		Assert.True(MuiApplicationMenuStateRecordCodec.TryReadStructural(
			ref platform, address, out var updated));
		Assert.Equal(initial.Magic, updated.Magic);
		Assert.Equal(0xF1020304u, updated.MenuAction);
		Assert.Equal(initial.MenuHelp, updated.MenuHelp);
		Assert.False(MuiApplicationMenuStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address,
			unchecked((MuiApplicationMenuStateField)255), 1));
	}
}
