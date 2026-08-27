using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationMessageRoutingAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationMessageRoutingAdmissionRequiresValidMessageAndOwner()
	{
		var platform = CreateApplication(out var application);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		var message = APTR.FromPointer(0x1300);
		Assert.True(MuiAppMessageRecordCodec.Write(ref platform, message,
			default));
		var value = default(MuiApplicationMessageRoutingStateRecord);
		value.Magic = MuiApplicationMessageRoutingStateRecord.Cookie;
		value.AppMessage = message;
		Assert.True(MuiApplicationMessageRoutingStateAdmission.Validate(
			ref platform, value));
		Assert.True(MuiApplicationMessageRoutingStateAdmission.ValidateLive(
			ref platform, State, application, value));

		value.WindowAppWindow = 2;
		Assert.False(MuiApplicationMessageRoutingStateAdmission.Validate(
			ref platform, value));
		value.WindowAppWindow = 0;
		value.AppMessage = APTR.FromPointer(0x31000);
		Assert.False(MuiApplicationMessageRoutingStateAdmission.Validate(
			ref platform, value));
		Assert.False(MuiApplicationMessageRoutingStateAdmission.ValidateLive(
			ref platform, State, APTR.FromPointer(0xDEAD), value));
	}

	[Fact]
	public void MalformedApplicationMessageRoutingMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreateApplication(out var application);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, MuiApplicationMessageCore.AppMessage, out _));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, application,
			MuiApplicationMessageCore.RoutingStateKey);
		Assert.True(MuiApplicationMessageRoutingStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block,
			MuiApplicationMessageRoutingStateField.Magic, 0));

		Assert.True(MuiApplicationMessageRoutingStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiApplicationMessageRoutingStateRecordCodec.TryRead(
			ref platform, block, out _));
		Assert.False(MuiApplicationMessageCore.TryGetApplicationMessageRoutingState(
			ref platform, State, application, out _));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			application, MuiApplicationMessageCore.AppMessage, out var raw));
		Assert.Equal(0u, raw);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			application, MuiApplicationMessageCore.RoutingStateKey));
	}

	private static MuiHeadlessTestPlatform CreateApplication(out APTR application)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
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
