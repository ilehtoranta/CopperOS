using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationWindowRelationshipAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationWindowRelationshipAdmissionRequiresMappedChildAndOwner()
	{
		var platform = CreateApplication(out var application, out var window);
		var value = default(MuiApplicationWindowRelationshipStateRecord);
		value.Magic = MuiApplicationWindowRelationshipStateRecord.Cookie;
		value.LastWindow = window;
		value.AddedCount = uint.MaxValue;
		Assert.True(MuiApplicationWindowRelationshipStateAdmission.Validate(
			ref platform, value));
		Assert.False(MuiApplicationWindowRelationshipStateAdmission.ValidateLive(
			ref platform, State, application, value));
		Assert.True(MuiApplicationWindowCore.SetApplicationWindowValue(
			ref platform, State, application, window.Raw));
		Assert.True(MuiApplicationWindowRelationshipStateAdmission.ValidateLive(
			ref platform, State, application, value));
		Assert.True(MuiApplicationWindowRelationshipStateRecordCodec.Write(
			ref platform, APTR.FromPointer(0x1300), value));

		value.LastWindow = APTR.FromPointer(0x2F000);
		Assert.False(MuiApplicationWindowRelationshipStateAdmission.Validate(
			ref platform, value));
		Assert.False(MuiApplicationWindowRelationshipStateAdmission.ValidateLive(
			ref platform, State, APTR.FromPointer(0xDEAD), value));
	}

	[Fact]
	public void MalformedApplicationWindowRelationshipMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreateApplication(out var application, out var window);
		Assert.True(MuiApplicationWindowCore.SetApplicationWindowValue(
			ref platform, State, application, window.Raw));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, application,
			MuiApplicationWindowCore.ApplicationWindowRelationshipStateKey);
		Assert.True(MuiApplicationWindowRelationshipStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiApplicationWindowRelationshipStateField.Magic, 0));

		Assert.True(MuiApplicationWindowRelationshipStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiApplicationWindowRelationshipStateRecordCodec.TryRead(
			ref platform, block, out _));
		Assert.False(MuiApplicationWindowCore.TryGetApplicationWindowRelationshipState(
			ref platform, State, application, out _));
		Assert.False(MuiApplicationWindowCore.SetApplicationWindowValue(
			ref platform, State, application, window.Raw));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			application, 0x8042BFE0u, out var preserved));
		Assert.Equal(window.Raw, preserved);
	}

	private static MuiHeadlessTestPlatform CreateApplication(out APTR application,
		out APTR window)
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
		window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			applicationClass, APTR.Null);
		return platform;
	}
}
