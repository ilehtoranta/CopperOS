using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationDefaultConfigAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationDefaultConfigAdmissionRequiresCookieAndOwner()
	{
		var platform = CreateApplication(out var application);
		var value = default(MuiApplicationDefaultConfigStateRecord);
		value.Magic = MuiApplicationDefaultConfigStateRecord.Cookie;
		value.ConfigId = uint.MaxValue;
		value.Value = uint.MaxValue;
		value.Requests = uint.MaxValue;
		Assert.True(MuiApplicationDefaultConfigStateAdmission.Validate(value));
		Assert.True(MuiApplicationDefaultConfigStateAdmission.ValidateLive(
			ref platform, State, application, value));
		Assert.True(MuiApplicationDefaultConfigStateRecordCodec.Write(ref platform,
			APTR.FromPointer(0x1300), value));

		value.Magic = 0;
		Assert.False(MuiApplicationDefaultConfigStateAdmission.Validate(value));
		Assert.False(MuiApplicationDefaultConfigStateAdmission.ValidateLive(
			ref platform, State, APTR.FromPointer(0xDEAD), value));
	}

	[Fact]
	public void MalformedApplicationDefaultConfigMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreateApplication(out var application);
		platform.DefaultConfigItemValue = 0x12345678;
		Assert.Equal(0x12345678u, MuiApplicationWindowCore.DefaultConfigItem(
			ref platform, State, application, 0x44));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, application,
			MuiApplicationWindowCore.ApplicationDefaultConfigStateKey);
		Assert.True(MuiApplicationDefaultConfigStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiApplicationDefaultConfigStateField.Magic, 0));

		Assert.True(MuiApplicationDefaultConfigStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiApplicationDefaultConfigStateRecordCodec.TryRead(
			ref platform, block, out _));
		Assert.False(MuiApplicationWindowCore.TryGetApplicationDefaultConfigState(
			ref platform, State, application, out _));
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			application, MuiApplicationWindowCore.ApplicationDefaultConfigStateKey));
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
