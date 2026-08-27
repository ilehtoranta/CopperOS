using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGroupLayoutHookStructAdapterTests
{
	[Fact]
	public void GroupLayoutHookStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiGroupLayoutHookStateRecord
		{
			Magic = MuiGroupLayoutHookStateRecord.Cookie,
			Hook = APTR.Null,
		};

		Assert.True(MuiGroupLayoutHookStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiGroupLayoutHookStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiGroupLayoutHookStateField.Hook,
			out var hookAddress));
		Assert.Equal(0x3504u, hookAddress.Raw);
		Assert.True(MuiGroupLayoutHookStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiGroupLayoutHookStateField.Hook, 0x12345678));
		Assert.True(MuiGroupLayoutHookStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(0x12345678u, decoded.Hook.Raw);
		Assert.False(MuiGroupLayoutHookStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF9),
			MuiGroupLayoutHookStateField.Magic, out _));
		Assert.False(MuiGroupLayoutHookStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiGroupLayoutHookStateField.Magic, out _));
	}
}
