using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationLifecycleAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationLifecycleAdmissionRequiresCanonicalFlags()
	{
		var platform = CreateApplication(out var application);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationLifecycleState(
			ref platform, State, application, out var valid));
		Assert.True(MuiApplicationLifecycleStateAdmission.Validate(valid));
		Assert.True(MuiApplicationLifecycleStateAdmission.ValidateLive(ref platform,
			State, application, valid));

		valid.Active = 2;
		Assert.False(MuiApplicationLifecycleStateAdmission.Validate(valid));
		Assert.False(MuiApplicationLifecycleStateAdmission.ValidateLive(ref platform,
			State, application, valid));
		Assert.False(MuiApplicationLifecycleStateRecordCodec.Write(ref platform,
			APTR.FromPointer(0x1F00), valid));
	}

	[Fact]
	public void MalformedApplicationLifecycleMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreateApplication(out var application);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, application,
			MuiApplicationWindowCore.ApplicationLifecycleStateKey);
		Assert.True(MuiApplicationLifecycleStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiApplicationLifecycleStateField.Magic, 0));

		Assert.True(MuiApplicationLifecycleStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiApplicationLifecycleStateRecordCodec.TryRead(ref platform,
			block, out _));
		Assert.False(MuiApplicationWindowCore.TryGetApplicationLifecycleState(
			ref platform, State, application, out _));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, 0x804260AB, out _));
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			application, MuiApplicationWindowCore.ApplicationLifecycleStateKey));
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
