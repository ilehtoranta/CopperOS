using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationIdentityAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationIdentityAdmissionRequiresBoundedGuestStringsAndOwner()
	{
		var platform = CreateApplication(out var application);
		var title = APTR.FromPointer(0x3B00);
		platform.WriteCString(title, "CopperOS");
		var value = default(MuiApplicationIdentityStateRecord);
		value.Magic = MuiApplicationIdentityStateRecord.Cookie;
		value.Title = title;
		Assert.True(MuiApplicationIdentityStateAdmission.Validate(ref platform,
			value));
		Assert.True(MuiApplicationIdentityStateAdmission.ValidateLive(ref platform,
			State, application, value));
		Assert.True(MuiApplicationIdentityStateRecordCodec.Write(ref platform,
			APTR.FromPointer(0x1400), value));

		value.Title = APTR.FromPointer(0x31000);
		Assert.False(MuiApplicationIdentityStateAdmission.Validate(ref platform,
			value));
		Assert.False(MuiApplicationIdentityStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), value));
	}

	[Fact]
	public void MalformedApplicationIdentityMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreateApplication(out var application);
		var title = APTR.FromPointer(0x3B00);
		platform.WriteCString(title, "CopperOS");
		Assert.True(MuiApplicationWindowCore.SetApplicationInitializerStringValue(
			ref platform, State, application, 0x804281B8u, title.Raw));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, application,
			MuiApplicationWindowCore.ApplicationIdentityStateKey);
		Assert.True(MuiApplicationIdentityStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiApplicationIdentityStateField.Magic, 0));

		Assert.True(MuiApplicationIdentityStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiApplicationIdentityStateRecordCodec.TryRead(ref platform,
			block, out _));
		Assert.False(MuiApplicationWindowCore.TryGetApplicationIdentityState(
			ref platform, State, application, out _));
		var replacement = APTR.FromPointer(0x3B40);
		platform.WriteCString(replacement, "CopperOS 2");
		Assert.False(MuiApplicationWindowCore.SetApplicationInitializerStringValue(
			ref platform, State, application, 0x804281B8u, replacement.Raw));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			application, 0x804281B8u, out var preserved));
		Assert.Equal(title.Raw, preserved);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			application, MuiApplicationWindowCore.ApplicationIdentityStateKey));
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
