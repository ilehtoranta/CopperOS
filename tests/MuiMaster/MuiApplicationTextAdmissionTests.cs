using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationTextAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationTextAdmissionRequiresBoundedGuestStringsAndOwner()
	{
		var platform = CreateApplication(out var application);
		var helpFile = APTR.FromPointer(0x3A40);
		platform.WriteCString(helpFile, "SYS:Help.guide");
		var value = default(MuiApplicationTextStateRecord);
		value.Magic = MuiApplicationTextStateRecord.Cookie;
		value.HelpFile = helpFile;
		Assert.True(MuiApplicationTextStateAdmission.Validate(ref platform, value));
		Assert.True(MuiApplicationTextStateAdmission.ValidateLive(ref platform,
			State, application, value));
		Assert.True(MuiApplicationTextStateRecordCodec.Write(ref platform,
			APTR.FromPointer(0x3AC0), value));

		value.HelpFile = APTR.FromPointer(0x31000);
		Assert.False(MuiApplicationTextStateAdmission.Validate(ref platform, value));
		Assert.False(MuiApplicationTextStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), value));
	}

	[Fact]
	public void MalformedApplicationTextMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreateApplication(out var application);
		var original = APTR.FromPointer(0x3A40);
		platform.WriteCString(original, "SYS:Help.guide");
		Assert.True(MuiApplicationWindowCore.SetApplicationHelpFileValue(
			ref platform, State, application, original.Raw));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, application,
			MuiApplicationWindowCore.ApplicationTextStateKey);
		Assert.True(MuiApplicationTextStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiApplicationTextStateField.Magic, 0));

		Assert.True(MuiApplicationTextStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiApplicationTextStateRecordCodec.TryRead(ref platform, block,
			out _));
		Assert.False(MuiApplicationWindowCore.TryGetApplicationTextState(
			ref platform, State, application, out _));
		var replacement = APTR.FromPointer(0x3A80);
		platform.WriteCString(replacement, "SYS:Other.guide");
		Assert.False(MuiApplicationWindowCore.SetApplicationHelpFileValue(
			ref platform, State, application, replacement.Raw));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			application, 0x804293F4u, out var preserved));
		Assert.Equal(original.Raw, preserved);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			application, MuiApplicationWindowCore.ApplicationTextStateKey));
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
