using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationUsedClassesAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationUsedClassesAdmissionRequiresBoundedVectorAndOwner()
	{
		var platform = CreateApplication(out var application);
		var vector = APTR.FromPointer(0x3B00);
		var className = APTR.FromPointer(0x3B40);
		platform.WriteCString(className, "Listtree.mcc");
		platform.WriteUInt32(vector, 0, className.Raw);
		platform.WriteUInt32(vector, 4, 0);
		var value = default(MuiApplicationUsedClassesStateRecord);
		value.Magic = MuiApplicationUsedClassesStateRecord.Cookie;
		value.Vector = vector;
		Assert.True(MuiApplicationUsedClassesStateAdmission.Validate(ref platform,
			value));
		Assert.True(MuiApplicationUsedClassesStateAdmission.ValidateLive(ref platform,
			State, application, value));
		Assert.True(MuiApplicationUsedClassesStateRecordCodec.Write(ref platform,
			APTR.FromPointer(0x1300), value));

		value.Vector = APTR.FromPointer(0x2F000);
		Assert.False(MuiApplicationUsedClassesStateAdmission.Validate(ref platform,
			value));
		Assert.False(MuiApplicationUsedClassesStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), value));
	}

	[Fact]
	public void MalformedApplicationUsedClassesMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreateApplication(out var application);
		var vector = APTR.FromPointer(0x3B00);
		var className = APTR.FromPointer(0x3B40);
		platform.WriteCString(className, "Listtree.mcc");
		platform.WriteUInt32(vector, 0, className.Raw);
		platform.WriteUInt32(vector, 4, 0);
		Assert.True(MuiApplicationWindowCore.SetApplicationUsedClassesValue(
			ref platform, State, application, vector.Raw));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, application,
			MuiApplicationWindowCore.ApplicationUsedClassesStateKey);
		Assert.True(MuiApplicationUsedClassesStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiApplicationUsedClassesStateField.Magic, 0));

		Assert.True(MuiApplicationUsedClassesStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiApplicationUsedClassesStateRecordCodec.TryRead(ref platform,
			block, out _));
		Assert.False(MuiApplicationWindowCore.TryGetApplicationUsedClassesState(
			ref platform, State, application, out _));
		Assert.False(MuiApplicationWindowCore.SetApplicationUsedClassesValue(
			ref platform, State, application, vector.Raw));
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
