using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationHelpAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationHelpAdmissionRequiresBoundedStringsAndLiveReferences()
	{
		var platform = CreateApplication(out var application, out var reference);
		var name = APTR.FromPointer(0x3B00);
		var node = APTR.FromPointer(0x3B40);
		platform.WriteCString(name, "SYS:Help.guide");
		platform.WriteCString(node, "main");
		var value = default(MuiApplicationHelpStateRecord);
		value.Magic = MuiApplicationHelpStateRecord.Cookie;
		value.AboutReferenceWindow = reference;
		value.AboutRequests = uint.MaxValue;
		value.HelpWindow = reference;
		value.HelpName = name;
		value.HelpNode = node;
		value.HelpLine = uint.MaxValue;
		value.HelpRequests = uint.MaxValue;
		Assert.True(MuiApplicationHelpStateAdmission.Validate(ref platform, value));
		Assert.True(MuiApplicationHelpStateAdmission.ValidateLive(ref platform,
			State, application, value));
		Assert.True(MuiApplicationHelpStateRecordCodec.Write(ref platform,
			APTR.FromPointer(0x1300), value));

		value.HelpNode = APTR.FromPointer(0x2F000);
		Assert.False(MuiApplicationHelpStateAdmission.Validate(ref platform, value));
		Assert.False(MuiApplicationHelpStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), value));
	}

	[Fact]
	public void MalformedApplicationHelpMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreateApplication(out var application, out var reference);
		var name = APTR.FromPointer(0x3B00);
		var node = APTR.FromPointer(0x3B40);
		platform.WriteCString(name, "SYS:Help.guide");
		platform.WriteCString(node, "main");
		Assert.True(MuiApplicationWindowCore.AboutMUI(ref platform, State,
			application, reference));
		Assert.True(MuiApplicationWindowCore.ShowHelp(ref platform, State,
			application, reference, name, node, 1));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, application,
			MuiApplicationWindowCore.ApplicationHelpStateKey);
		Assert.True(MuiApplicationHelpStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiApplicationHelpStateField.Magic, 0));

		Assert.True(MuiApplicationHelpStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiApplicationHelpStateRecordCodec.TryRead(ref platform,
			block, out _));
		Assert.False(MuiApplicationWindowCore.TryGetApplicationHelpState(
			ref platform, State, application, out _));
		var aboutRequests = platform.AboutMUIRequestCount;
		Assert.False(MuiApplicationWindowCore.AboutMUI(ref platform, State,
			application, reference));
		Assert.Equal(aboutRequests, platform.AboutMUIRequestCount);
	}

	private static MuiHeadlessTestPlatform CreateApplication(out APTR application,
		out APTR reference)
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
		reference = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			applicationClass, APTR.Null);
		return platform;
	}
}
