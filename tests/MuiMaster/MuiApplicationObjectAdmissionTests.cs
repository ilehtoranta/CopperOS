using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationObjectAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationObjectAdmissionRequiresMappedPointersAndOwner()
	{
		var platform = CreateApplication(out var application, out var dropObject);
		var diskObject = APTR.FromPointer(0x3B00);
		platform.WriteUInt8(diskObject, (int)Amiga.DiskObject.Size - 1, 0xA5);
		var value = default(MuiApplicationObjectStateRecord);
		value.Magic = MuiApplicationObjectStateRecord.Cookie;
		value.DiskObject = diskObject;
		value.DropObject = dropObject;
		Assert.True(MuiApplicationObjectStateAdmission.Validate(ref platform,
			value));
		Assert.True(MuiApplicationObjectStateAdmission.ValidateLive(ref platform,
			State, application, value));
		Assert.True(MuiApplicationObjectStateRecordCodec.Write(ref platform,
			APTR.FromPointer(0x1300), value));

		value.DiskObject = APTR.FromPointer(0x2F000);
		Assert.False(MuiApplicationObjectStateAdmission.Validate(ref platform,
			value));
		Assert.False(MuiApplicationObjectStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), value));
	}

	[Fact]
	public void MalformedApplicationObjectMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreateApplication(out var application, out _);
		var diskObject = APTR.FromPointer(0x3B00);
		platform.WriteUInt8(diskObject, (int)Amiga.DiskObject.Size - 1, 0xA5);
		Assert.True(MuiApplicationWindowCore.SetApplicationDiskObjectValue(
			ref platform, State, application, diskObject.Raw));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, application,
			MuiApplicationWindowCore.ApplicationObjectStateKey);
		Assert.True(MuiApplicationObjectStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiApplicationObjectStateField.Magic, 0));

		Assert.True(MuiApplicationObjectStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiApplicationObjectStateRecordCodec.TryRead(ref platform,
			block, out _));
		Assert.False(MuiApplicationWindowCore.TryGetApplicationObjectState(
			ref platform, State, application, out _));
		Assert.False(MuiApplicationWindowCore.SetApplicationDiskObjectValue(
			ref platform, State, application, 0));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			application, 0x804235CBu, out var preserved));
		Assert.Equal(diskObject.Raw, preserved);
	}

	private static MuiHeadlessTestPlatform CreateApplication(out APTR application,
		out APTR dropObject)
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
		dropObject = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			applicationClass, APTR.Null);
		return platform;
	}
}
