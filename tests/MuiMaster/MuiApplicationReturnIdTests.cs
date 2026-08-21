using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationReturnIdTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ReturnIdQueuesValuesAndSignalsTheInitializedApplication()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0x20));
		var packet = APTR.FromPointer(0x1200);
		var signals = APTR.FromPointer(0x1240);
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.ApplicationReturnIdMethod);
		platform.WriteUInt32(packet, 4, 41);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationReturnId(
			ref platform, State, application, packet));
		platform.WriteUInt32(packet, 4, 42);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationReturnId(
			ref platform, State, application, packet));
		Assert.Equal(0x20u, platform.SignaledMask);
		Assert.Equal(41u, MuiApplicationWindowCore.Input(ref platform, State,
			application, signals));
		Assert.Equal(42u, MuiApplicationWindowCore.Input(ref platform, State,
			application, signals));
	}

	[Fact]
	public void ReturnIdAcceptsAnUnsignalledApplicationAndRejectsInvalidPackets()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.ApplicationReturnIdMethod);
		platform.WriteUInt32(packet, 4, 7);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationReturnId(
			ref platform, State, application, packet));
		Assert.Equal(0u, platform.SignaledMask);

		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationReturnId(
			ref platform, State, application, packet));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			application));
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.ApplicationReturnIdMethod);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationReturnId(
			ref platform, State, application, packet));

		var truncated = APTR.FromPointer(0x20FFC);
		platform.WriteUInt32(truncated, 0,
			MuiApplicationDispatcher.ApplicationReturnIdMethod);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationReturnId(
			ref platform, State, application, truncated));
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
