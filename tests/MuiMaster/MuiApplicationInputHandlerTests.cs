using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationInputHandlerTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void AddAndRemoveInputHandlerRouteTheExactPacketAndDispatchSignals()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var target = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		var handler = APTR.FromPointer(0x1300);
		platform.WriteUInt32(handler, 8, target.Raw);
		platform.WriteUInt32(handler, 12, 0x20);
		platform.WriteUInt32(handler, 20, 0x90000001);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.AddInputHandlerMethod);
		platform.WriteUInt32(packet, 4, handler.Raw);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationInputHandler(
			ref platform, State, application, packet));
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchInputHandlers(ref platform,
			State, application, 0));
		Assert.Equal(1u, MuiApplicationWindowCore.DispatchInputHandlers(ref platform,
			State, application, 0x20));
		Assert.Equal(target, platform.LastDispatchObject);
		Assert.Equal(0x90000001u, platform.LastDispatchMethod);

		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.RemoveInputHandlerMethod);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationInputHandler(
			ref platform, State, application, packet));
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchInputHandlers(ref platform,
			State, application, 0x20));
	}

	[Fact]
	public void InputHandlerPacketsRejectUnknownUnmappedAndDeadApplications()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationInputHandler(
			ref platform, State, application, packet));
		var unmapped = APTR.FromPointer(0x21000);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationInputHandler(
			ref platform, State, application, unmapped));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			application));
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.AddInputHandlerMethod);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationInputHandler(
			ref platform, State, application, packet));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR cl)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Application.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR Object(ref MuiHeadlessTestPlatform platform, APTR cl) =>
		MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl, APTR.Null);
}
