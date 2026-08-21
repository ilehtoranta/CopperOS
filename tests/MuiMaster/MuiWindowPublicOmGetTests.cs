using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiWindowPublicOmGetTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void WindowLifecycleGettersUseNamedStateThroughCommonOmGet()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			State);
		var className = APTR.FromPointer(0x1100);
		platform.WriteCString(className, "Window.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var windowClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, className, APTR.Null, 0, APTR.FromPointer(1));
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			windowClass, APTR.Null);
		Assert.NotEqual(APTR.Null, window);
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));

		var message = APTR.FromPointer(0x1800);
		var storage = APTR.FromPointer(0x1900);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Storage, storage.Raw));

		foreach (var attribute in new[] { MuiWindowPublicCore.Open,
			MuiWindowPublicCore.Window })
		{
			Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
				attribute));
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform,
				State, window, message));
			Assert.True(MuiGuestUlongStorageCodec.TryRead(ref platform, storage,
				out var stored));
			if (attribute == MuiWindowPublicCore.Open)
				Assert.Equal(1u, stored.Value);
			else
				Assert.NotEqual(0u, stored.Value);
		}

		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
	}
}
