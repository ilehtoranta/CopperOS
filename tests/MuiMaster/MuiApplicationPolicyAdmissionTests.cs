using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationPolicyAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationPolicyAdmissionRequiresCanonicalBooleansAndOwner()
	{
		var platform = CreateApplication(out var application);
		var value = default(MuiApplicationPolicyStateRecord);
		value.Magic = MuiApplicationPolicyStateRecord.Cookie;
		value.UseRexx = 1;
		value.UseCommodities = 1;
		value.UseScreenNotify = 1;
		Assert.True(MuiApplicationPolicyStateAdmission.Validate(value));
		Assert.True(MuiApplicationPolicyStateAdmission.ValidateLive(ref platform,
			State, application, value));
		Assert.True(MuiApplicationPolicyStateRecordCodec.Write(ref platform,
			APTR.FromPointer(0x1300), value));

		value.UseRexx = 2;
		Assert.False(MuiApplicationPolicyStateAdmission.Validate(value));
		Assert.False(MuiApplicationPolicyStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), value));
	}

	[Fact]
	public void MalformedApplicationPolicyMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreateApplication(out var application);
		Assert.True(MuiApplicationWindowCore.SetApplicationUseRexxValue(
			ref platform, State, application, 0));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, application,
			MuiApplicationWindowCore.ApplicationPolicyStateKey);
		Assert.True(MuiApplicationPolicyStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiApplicationPolicyStateField.Magic, 0));

		Assert.True(MuiApplicationPolicyStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiApplicationPolicyStateRecordCodec.TryRead(ref platform,
			block, out _));
		Assert.False(MuiApplicationWindowCore.TryGetApplicationPolicyState(
			ref platform, State, application, out _));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x80425EE5u, out _));
		Assert.False(MuiApplicationWindowCore.SetApplicationUseRexxValue(
			ref platform, State, application, 1));
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			application, MuiApplicationWindowCore.ApplicationPolicyStateKey));
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
