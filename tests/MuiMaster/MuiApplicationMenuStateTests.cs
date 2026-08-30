using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationMenuStateTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationMenuReadersUseNamedMethodHeader()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiApplicationMenuQueryMessageCodec.Write(ref platform, packet,
			new MuiApplicationMenuQueryMessage
			{
				MethodId = MuiApplicationDispatcher.ApplicationGetMenuCheckMethod,
				MenuId = 7,
			}));
		var applicationQuery = new MuiApplicationMenuPacketCodec.MenuPacketAddress
		{
			Address = packet,
			Method = MuiApplicationDispatcher.ApplicationGetMenuCheckMethod,
		};
		Assert.True(MuiApplicationMenuPacketCodec.TryReadApplicationQuery(
			ref platform, ref applicationQuery, out var query));
		Assert.Equal(applicationQuery.Method, query.MethodId);
		Assert.Equal(7u, query.MenuId);
		Assert.True(MuiApplicationMenuPacketCodec.TryReadMethodIdValue(
			ref platform, packet, MuiApplicationMenuPacketKind.ApplicationQuery,
			out var methodId));
		Assert.Equal(applicationQuery.Method, methodId);
		Assert.True(MuiApplicationMenuSetMessageCodec.Write(ref platform, packet,
			new MuiApplicationMenuSetMessage
			{
				MethodId = MuiApplicationDispatcher.ApplicationSetMenuStateMethod,
				MenuId = 8,
				State = 1,
			}));
		var applicationSet = new MuiApplicationMenuPacketCodec.MenuPacketAddress
		{
			Address = packet,
			Method = MuiApplicationDispatcher.ApplicationSetMenuStateMethod,
		};
		Assert.True(MuiApplicationMenuPacketCodec.TryReadApplicationSet(
			ref platform, ref applicationSet, out var appSet));
		Assert.Equal(8u, appSet.MenuId);
		Assert.Equal(1u, appSet.State);

		Assert.True(MuiWindowMenuQueryMessageCodec.Write(ref platform, packet,
			new MuiWindowMenuQueryMessage
			{
				MethodId = MuiApplicationDispatcher.WindowGetMenuStateMethod,
				MenuId = 9,
			}));
		var windowQuery = new MuiApplicationMenuPacketCodec.MenuPacketAddress
		{
			Address = packet,
			Method = MuiApplicationDispatcher.WindowGetMenuStateMethod,
		};
		Assert.True(MuiApplicationMenuPacketCodec.TryReadWindowQuery(ref platform,
			ref windowQuery, out var winQuery));
		Assert.Equal(9u, winQuery.MenuId);
		Assert.True(MuiApplicationMenuPacketCodec.TryReadMethodIdValue(
			ref platform, packet, MuiApplicationMenuPacketKind.WindowQuery,
			out var windowQueryMethodId));
		Assert.Equal(windowQuery.Method, windowQueryMethodId);
		Assert.True(MuiWindowMenuSetMessageCodec.Write(ref platform, packet,
			new MuiWindowMenuSetMessage
			{
				MethodId = MuiApplicationDispatcher.WindowSetMenuStateMethod,
				MenuId = 7,
				State = 1,
			}));
		var windowSet = new MuiApplicationMenuPacketCodec.MenuPacketAddress
		{
			Address = packet,
			Method = MuiApplicationDispatcher.WindowSetMenuStateMethod,
		};
		Assert.True(MuiApplicationMenuPacketCodec.TryReadWindowSet(ref platform,
			ref windowSet, out var set));
		Assert.Equal(windowSet.Method, set.MethodId);
		Assert.Equal(7u, set.MenuId);
		Assert.Equal(1u, set.State);
		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.False(MuiApplicationMenuPacketCodec.TryReadWindowSet(ref platform,
			ref windowSet, out _));

		Assert.False(MuiApplicationMenuQueryMessageCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
		Assert.False(MuiApplicationMenuSetMessageCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
		Assert.False(MuiWindowMenuQueryMessageCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
		Assert.False(MuiWindowMenuSetMessageCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
	}

	[Fact]
	public void ApplicationMenuPacketsUseNamedGetAndSetRecords()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		var openWindow = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, openWindow));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			openWindow, 0));
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.ApplicationGetMenuCheckMethod);
		platform.WriteUInt32(packet, 4, 7);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationMenuState(
			ref platform, State, application, packet));
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.ApplicationGetMenuStateMethod);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationMenuState(
			ref platform, State, application, packet));

		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.ApplicationSetMenuCheckMethod);
		platform.WriteUInt32(packet, 8, 1);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationMenuState(
			ref platform, State, application, packet));
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.ApplicationSetMenuStateMethod);
		platform.WriteUInt32(packet, 8, 0);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchApplicationMenuState(
			ref platform, State, application, packet));
		Assert.Equal(2u, platform.MenuOperationCount);
	}

	[Fact]
	public void ApplicationMenuPacketsRejectUnknownTruncatedAndDeadCalls()
	{
		var platform = CreatePlatform(out var cl);
		var application = Object(ref platform, cl);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationMenuState(
			ref platform, State, application, packet));
		var truncated = APTR.FromPointer(0x20FFC);
		platform.WriteUInt32(truncated, 0,
			MuiApplicationDispatcher.ApplicationSetMenuCheckMethod);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationMenuState(
			ref platform, State, application, truncated));
		var unmapped = APTR.FromPointer(0x21000);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationMenuState(
			ref platform, State, application, unmapped));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			application));
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.ApplicationGetMenuStateMethod);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchApplicationMenuState(
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
