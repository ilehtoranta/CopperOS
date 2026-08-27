using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationMenuAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationMenuAdmissionRequiresCookieAndOwner()
	{
		var platform = CreateApplication(out var application);
		var value = default(MuiApplicationMenuStateRecord);
		value.Magic = MuiApplicationMenuStateRecord.Cookie;
		value.MenuAction = uint.MaxValue;
		value.MenuHelp = 0xCAFE;
		Assert.True(MuiApplicationMenuStateAdmission.Validate(value));
		Assert.True(MuiApplicationMenuStateAdmission.ValidateLive(ref platform,
			State, application, value));
		Assert.True(MuiApplicationMenuStateRecordCodec.Write(ref platform,
			APTR.FromPointer(0x1300), value));

		value.Magic = 0;
		Assert.False(MuiApplicationMenuStateAdmission.Validate(value));
		Assert.False(MuiApplicationMenuStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), value));
	}

	[Fact]
	public void MalformedApplicationMenuMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreateApplication(out var application);
		Assert.True(MuiApplicationWindowCore.SetApplicationMenuActionValue(
			ref platform, State, application, 0xCAFE));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, application,
			MuiApplicationWindowCore.ApplicationMenuStateKey);
		Assert.True(MuiApplicationMenuStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiApplicationMenuStateField.Magic, 0));

		Assert.True(MuiApplicationMenuStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiApplicationMenuStateRecordCodec.TryRead(ref platform,
			block, out _));
		Assert.False(MuiApplicationWindowCore.TryGetApplicationMenuState(
			ref platform, State, application, out _));
		Assert.False(MuiApplicationWindowCore.SetApplicationMenuActionValue(
			ref platform, State, application, 0xBEEF));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			application, 0x80428961u, out var preserved));
		Assert.Equal(0xCAFEu, preserved);
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
