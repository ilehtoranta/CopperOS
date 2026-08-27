using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationSetConfigItemAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationSetConfigItemAdmissionRequiresMappedDataAndOwner()
	{
		var platform = CreateApplication(out var application);
		var value = default(MuiApplicationSetConfigItemStateRecord);
		value.Magic = MuiApplicationSetConfigItemStateRecord.Cookie;
		value.Item = uint.MaxValue;
		value.Data = APTR.FromPointer(0x3B00);
		value.Requests = uint.MaxValue;
		Assert.True(MuiApplicationSetConfigItemStateAdmission.Validate(
			ref platform, value));
		Assert.True(MuiApplicationSetConfigItemStateAdmission.ValidateLive(
			ref platform, State, application, value));
		Assert.True(MuiApplicationSetConfigItemStateRecordCodec.Write(
			ref platform, APTR.FromPointer(0x1300), value));

		value.Data = APTR.FromPointer(0x2F000);
		Assert.False(MuiApplicationSetConfigItemStateAdmission.Validate(
			ref platform, value));
		Assert.False(MuiApplicationSetConfigItemStateAdmission.ValidateLive(
			ref platform, State, APTR.FromPointer(0xDEAD), value));
	}

	[Fact]
	public void MalformedApplicationSetConfigItemMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreateApplication(out var application);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		var data = APTR.FromPointer(0x3B00);
		platform.WriteUInt8(data, 0, 0xA5);
		Assert.True(MuiApplicationWindowCore.SetConfigItem(ref platform, State,
			application, 0x34, data));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			application, MuiApplicationWindowCore.ApplicationSetConfigItemState,
			out var raw));
		var block = APTR.FromPointer(raw);
		Assert.True(MuiApplicationSetConfigItemStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiApplicationSetConfigItemStateField.Magic, 0));

		Assert.True(MuiApplicationSetConfigItemStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiApplicationSetConfigItemStateRecordCodec.TryRead(
			ref platform, block, out _));
		Assert.False(MuiApplicationWindowCore.ReadSetConfigItemState(ref platform,
			State, application, out _, out _, out _));
		Assert.False(MuiApplicationWindowCore.SetConfigItem(ref platform, State,
			application, 0x35, APTR.Null));
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
