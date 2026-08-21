using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiHandleEventPacketTests
{
	[Fact]
	public void HandleEventPacketUsesMorphosMuiKeyAndEventHandlerNodeFields()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiCommonControlPacketCore.WriteHandleEvent(ref platform,
			packet, 0x3500, -2, 0x3600));

		Assert.True(MuiCommonControlPacketCore.TryReadHandleEvent(ref platform,
			packet, out var value));
		Assert.Equal(0x3500u, value.InputMessage);
		Assert.Equal(-2, value.MuiKey);
		Assert.Equal(0x3600u, value.EventHandlerNode);
		Assert.False(MuiCommonControlPacketCore.TryReadHandleEvent(ref platform,
			APTR.FromPointer(0x20FFCu), out _));
	}
}
