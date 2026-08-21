using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationMessageOmGetTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationMessageGettersUseNamedRoutingThroughCommonOmGet()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			State);
		var applicationName = APTR.FromPointer(0x1100);
		platform.WriteCString(applicationName, "Application.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var applicationClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref
			platform, State, applicationName, APTR.Null, 0, APTR.FromPointer(1));
		var application = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			applicationClass, APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			applicationClass, APTR.Null);
		Assert.NotEqual(APTR.Null, application);
		Assert.NotEqual(APTR.Null, child);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, application, child));

		var message = APTR.FromPointer(0x1800);
		var storage = APTR.FromPointer(0x1900);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Storage, storage.Raw));

		foreach (var attribute in new[] { MuiApplicationMessageCore.ApplicationObject,
			MuiApplicationMessageCore.AppMessage,
			MuiApplicationMessageCore.WindowAppWindow })
		{
			Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
				attribute));
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform,
				State, child, message));
			Assert.True(MuiGuestUlongStorageCodec.TryRead(ref platform, storage,
				out var stored));
			var expected = attribute == MuiApplicationMessageCore.ApplicationObject
				? application.Raw : 0u;
			Assert.Equal(expected, stored.Value);
		}

		Assert.True(MuiApplicationMessageCore.TryGetApplicationMessageRoutingState(
			ref platform, State, child, out var routing));
		Assert.Equal(MuiApplicationMessageRoutingStateRecord.Cookie, routing.Magic);
		Assert.True(routing.AppMessage.IsNull);
		Assert.Equal(0u, routing.WindowAppWindow);
	}
}
