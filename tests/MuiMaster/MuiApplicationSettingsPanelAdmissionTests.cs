using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationSettingsPanelAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationSettingsPanelAdmissionRequiresMappedPanelAndOwner()
	{
		var platform = CreateApplication(out var application, out var panel);
		var value = default(MuiApplicationSettingsPanelStateRecord);
		value.Magic = MuiApplicationSettingsPanelStateRecord.Cookie;
		value.Number = uint.MaxValue;
		value.Panel = panel;
		value.Requests = uint.MaxValue;
		Assert.True(MuiApplicationSettingsPanelStateAdmission.Validate(
			ref platform, value));
		Assert.True(MuiApplicationSettingsPanelStateAdmission.ValidateLive(
			ref platform, State, application, value));
		Assert.True(MuiApplicationSettingsPanelStateRecordCodec.Write(
			ref platform, APTR.FromPointer(0x1300), value));

		value.Panel = APTR.FromPointer(0x2F000);
		Assert.False(MuiApplicationSettingsPanelStateAdmission.Validate(
			ref platform, value));
		Assert.False(MuiApplicationSettingsPanelStateAdmission.ValidateLive(
			ref platform, State, APTR.FromPointer(0xDEAD), value));
	}

	[Fact]
	public void MalformedApplicationSettingsPanelMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreateApplication(out var application, out var panel);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		platform.SettingsPanelResult = panel;
		Assert.Equal(panel, MuiApplicationWindowCore.BuildSettingsPanel(ref platform,
			State, application, 3));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, application,
			MuiApplicationWindowCore.ApplicationSettingsPanelStateKey);
		Assert.True(MuiApplicationSettingsPanelStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiApplicationSettingsPanelStateField.Magic, 0));

		Assert.True(MuiApplicationSettingsPanelStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiApplicationSettingsPanelStateRecordCodec.TryRead(
			ref platform, block, out _));
		Assert.False(MuiApplicationWindowCore.TryGetApplicationSettingsPanelState(
			ref platform, State, application, out _));
		var calls = platform.BuildSettingsPanelRequestCount;
		Assert.True(MuiApplicationWindowCore.BuildSettingsPanel(ref platform, State,
			application, 4).IsNull);
		Assert.Equal(calls, platform.BuildSettingsPanelRequestCount);
	}

	private static MuiHeadlessTestPlatform CreateApplication(out APTR application,
		out APTR panel)
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
		panel = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			applicationClass, APTR.Null);
		return platform;
	}
}
