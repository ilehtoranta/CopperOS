using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiWindowMenuStateTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void WindowMenuPacketsUseNamedGetAndSetRecords()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.WindowGetMenuCheckMethod);
		platform.WriteUInt32(packet, 4, 7);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowMenuState(
			ref platform, State, window, packet));
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.WindowGetMenuStateMethod);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowMenuState(
			ref platform, State, window, packet));

		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.WindowSetMenuCheckMethod);
		platform.WriteUInt32(packet, 8, 1);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowMenuState(
			ref platform, State, window, packet));
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.WindowSetMenuStateMethod);
		platform.WriteUInt32(packet, 8, 0);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowMenuState(
			ref platform, State, window, packet));
		Assert.Equal(2u, platform.MenuOperationCount);
	}

	[Fact]
	public void WindowMenuPacketsRejectUnknownTruncatedUnopenedAndDeadCalls()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.WindowGetMenuStateMethod);
		platform.WriteUInt32(packet, 4, 7);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowMenuState(
			ref platform, State, window, packet));
		Assert.Equal(0u, platform.MenuOperationCount);

		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowMenuState(
			ref platform, State, window, packet));
		var truncated = APTR.FromPointer(0x20FFC);
		platform.WriteUInt32(truncated, 0,
			MuiApplicationDispatcher.WindowSetMenuCheckMethod);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowMenuState(
			ref platform, State, window, truncated));
		var unmapped = APTR.FromPointer(0x21000);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowMenuState(
			ref platform, State, window, unmapped));

		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 0));
		Assert.True(MuiApplicationWindowCore.CloseWindow(ref platform, State,
			window));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			window));
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.WindowGetMenuStateMethod);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowMenuState(
			ref platform, State, window, packet));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR cl)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Window.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR Object(ref MuiHeadlessTestPlatform platform, APTR cl) =>
		MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl, APTR.Null);
}
