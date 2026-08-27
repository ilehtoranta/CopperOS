using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationCommandsAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationCommandsAdmissionRequiresBoundedTableAndOwner()
	{
		var platform = CreateApplication(out var application);
		var value = default(MuiApplicationCommandsStateRecord);
		value.Magic = MuiApplicationCommandsStateRecord.Cookie;
		Assert.True(MuiApplicationCommandsStateAdmission.Validate(ref platform,
			value));
		Assert.True(MuiApplicationCommandsStateAdmission.ValidateLive(ref platform,
			State, application, value));
		Assert.True(MuiApplicationCommandsStateRecordCodec.Write(ref platform,
			APTR.FromPointer(0x1300), value));

		value.Table = APTR.FromPointer(0x2F000);
		Assert.False(MuiApplicationCommandsStateAdmission.Validate(ref platform,
			value));
		Assert.False(MuiApplicationCommandsStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), value));
	}

	[Fact]
	public void MalformedApplicationCommandsMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreateApplication(out var application);
		Assert.True(MuiApplicationCommandsCore.SetApplicationCommandsValue(
			ref platform, State, application, 0));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, application,
			MuiApplicationCommandsCore.CommandsStateKey);
		Assert.True(MuiApplicationCommandsStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiApplicationCommandsStateField.Magic, 0));

		Assert.True(MuiApplicationCommandsStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiApplicationCommandsStateRecordCodec.TryRead(ref platform,
			block, out _));
		Assert.False(MuiApplicationCommandsCore.TryGetApplicationCommandsState(
			ref platform, State, application, out _));
		Assert.False(MuiApplicationCommandsCore.SetApplicationCommandsValue(
			ref platform, State, application, 0));
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			application, MuiApplicationCommandsCore.CommandsStateKey));
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
