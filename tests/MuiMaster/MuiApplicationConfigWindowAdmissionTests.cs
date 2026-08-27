using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationConfigWindowAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationConfigWindowAdmissionRequiresBoundedClassIdAndOwner()
	{
		var platform = CreateApplication(out var application);
		var classId = APTR.FromPointer(0x3B00);
		platform.WriteCString(classId, "MUI:Config");
		var value = default(MuiApplicationConfigWindowStateRecord);
		value.Magic = MuiApplicationConfigWindowStateRecord.Cookie;
		value.Flags = uint.MaxValue;
		value.ClassId = classId;
		value.Requests = uint.MaxValue;
		Assert.True(MuiApplicationConfigWindowStateAdmission.Validate(ref platform,
			value));
		Assert.True(MuiApplicationConfigWindowStateAdmission.ValidateLive(
			ref platform, State, application, value));
		Assert.True(MuiApplicationConfigWindowStateRecordCodec.Write(ref platform,
			APTR.FromPointer(0x1300), value));

		value.ClassId = APTR.FromPointer(0x2F000);
		Assert.False(MuiApplicationConfigWindowStateAdmission.Validate(ref platform,
			value));
		Assert.False(MuiApplicationConfigWindowStateAdmission.ValidateLive(
			ref platform, State, APTR.FromPointer(0xDEAD), value));
	}

	[Fact]
	public void MalformedApplicationConfigWindowMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreateApplication(out var application);
		var classId = APTR.FromPointer(0x3B00);
		platform.WriteCString(classId, "MUI:Config");
		Assert.True(MuiApplicationWindowCore.OpenConfigWindow(ref platform, State,
			application, 3, classId));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, application,
			MuiApplicationWindowCore.ApplicationConfigWindowStateKey);
		Assert.True(MuiApplicationConfigWindowStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiApplicationConfigWindowStateField.Magic, 0));

		Assert.True(MuiApplicationConfigWindowStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiApplicationConfigWindowStateRecordCodec.TryRead(
			ref platform, block, out _));
		Assert.False(MuiApplicationWindowCore.TryGetApplicationConfigWindowState(
			ref platform, State, application, out _));
		var requests = platform.OpenConfigWindowRequestCount;
		Assert.False(MuiApplicationWindowCore.OpenConfigWindow(ref platform, State,
			application, 4, classId));
		Assert.Equal(requests, platform.OpenConfigWindowRequestCount);
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
