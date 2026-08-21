using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationWindowOmGetTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint ApplicationWindow = 0x8042BFE0;
	private const uint ApplicationInitialized = 0x7FFE0044;
	private const uint ApplicationActive = 0x804260AB;

	[Fact]
	public void ApplicationWindowLifecycleGettersUseNamedStateThroughCommonOmGet()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			State);
		var className = APTR.FromPointer(0x1100);
		platform.WriteCString(className, "Application.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var applicationClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref
			platform, State, className, APTR.Null, 0, APTR.FromPointer(1));
		var application = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			applicationClass, APTR.Null);
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			applicationClass, APTR.Null);
		Assert.NotEqual(APTR.Null, application);
		Assert.NotEqual(APTR.Null, window);
		Assert.True(MuiApplicationWindowCore.SetApplicationWindowValue(
			ref platform, State, application, window.Raw));
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.SetApplicationActiveValue(
			ref platform, State, application, 1));

		var message = APTR.FromPointer(0x1800);
		var storage = APTR.FromPointer(0x1900);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Storage, storage.Raw));

		foreach (var attribute in new[] { ApplicationWindow, ApplicationInitialized,
			ApplicationActive })
		{
			Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
				attribute));
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform,
				State, application, message));
			Assert.True(MuiGuestUlongStorageCodec.TryRead(ref platform, storage,
				out var stored));
			var expected = attribute == ApplicationWindow ? window.Raw : 1u;
			Assert.Equal(expected, stored.Value);
		}
	}
}
