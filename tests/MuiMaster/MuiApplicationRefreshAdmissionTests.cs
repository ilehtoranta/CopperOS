using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationRefreshAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationRefreshAdmissionRequiresCookieAndOwner()
	{
		var platform = CreateApplication(out var application);
		var value = default(MuiApplicationRefreshStateRecord);
		value.Magic = MuiApplicationRefreshStateRecord.Cookie;
		value.Checks = uint.MaxValue;
		value.RefreshedWindows = uint.MaxValue;
		Assert.True(MuiApplicationRefreshStateAdmission.Validate(value));
		Assert.True(MuiApplicationRefreshStateAdmission.ValidateLive(ref platform,
			State, application, value));
		Assert.True(MuiApplicationRefreshStateRecordCodec.Write(ref platform,
			APTR.FromPointer(0x1300), value));

		value.Magic = 0;
		Assert.False(MuiApplicationRefreshStateAdmission.Validate(value));
		Assert.False(MuiApplicationRefreshStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), value));
	}

	[Fact]
	public void MalformedApplicationRefreshMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreateApplication(out var application);
		platform.RefreshMuiWindowCount = 0;
		Assert.True(MuiApplicationWindowCore.CheckRefresh(ref platform, State,
			application));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, application,
			MuiApplicationWindowCore.ApplicationRefreshStateKey);
		Assert.True(MuiApplicationRefreshStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiApplicationRefreshStateField.Magic, 0));

		Assert.True(MuiApplicationRefreshStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiApplicationRefreshStateRecordCodec.TryRead(ref platform,
			block, out _));
		Assert.False(MuiApplicationWindowCore.TryGetApplicationRefreshState(
			ref platform, State, application, out _));
		var calls = platform.RefreshMuiWindowCount;
		Assert.False(MuiApplicationWindowCore.CheckRefresh(ref platform, State,
			application));
		Assert.Equal(calls, platform.RefreshMuiWindowCount);
	}

	private static MuiHeadlessTestPlatform CreateApplication(out APTR application)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Application.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var applicationClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, name, APTR.Null, 0, APTR.FromPointer(1), false);
		application = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			applicationClass, APTR.Null);
		return platform;
	}
}
