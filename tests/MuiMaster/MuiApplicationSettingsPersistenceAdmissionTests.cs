using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationSettingsPersistenceAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationSettingsPersistenceAdmissionRequiresCanonicalOperationAndOwner()
	{
		var platform = CreateApplication(out var application);
		var value = default(MuiApplicationSettingsPersistenceStateRecord);
		value.Magic = MuiApplicationSettingsPersistenceStateRecord.Cookie;
		value.Operation = 1;
		value.Name = APTR.FromPointer(uint.MaxValue);
		value.Requests = uint.MaxValue;
		value.Saves = uint.MaxValue;
		value.Loads = uint.MaxValue;
		Assert.True(MuiApplicationSettingsPersistenceStateAdmission.Validate(
			ref platform, value));
		Assert.True(MuiApplicationSettingsPersistenceStateAdmission.ValidateLive(
			ref platform, State, application, value));
		Assert.True(MuiApplicationSettingsPersistenceStateRecordCodec.Write(
			ref platform, APTR.FromPointer(0x1300), value));

		value.Operation = 2;
		Assert.False(MuiApplicationSettingsPersistenceStateAdmission.Validate(
			ref platform, value));
		Assert.False(MuiApplicationSettingsPersistenceStateAdmission.ValidateLive(
			ref platform, State, APTR.FromPointer(0xDEAD), value));
	}

	[Fact]
	public void MalformedApplicationSettingsPersistenceMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreateApplication(out var application);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		var name = APTR.FromPointer(0x3B00);
		platform.WriteCString(name, "ENV:CopperOS.prefs");
		Assert.True(MuiApplicationWindowCore.SaveApplicationSettings(ref platform,
			State, application, name));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, application,
			MuiApplicationWindowCore.ApplicationSettingsPersistenceStateKey);
		Assert.True(MuiApplicationSettingsPersistenceStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiApplicationSettingsPersistenceStateField.Magic, 0));

		Assert.True(MuiApplicationSettingsPersistenceStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiApplicationSettingsPersistenceStateRecordCodec.TryRead(
			ref platform, block, out _));
		Assert.False(MuiApplicationWindowCore.TryGetApplicationSettingsPersistenceState(
			ref platform, State, application, out _));
		var calls = platform.SettingsSaveRequestCount;
		Assert.False(MuiApplicationWindowCore.SaveApplicationSettings(ref platform,
			State, application, name));
		Assert.Equal(calls, platform.SettingsSaveRequestCount);
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
