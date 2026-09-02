using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiRegisterPolicyAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint PolicyStateKey = 0x0D100015u;

	[Fact]
	public void RegisterPolicyAdmissionRequiresCanonicalTitlesAndOwner()
	{
		var platform = CreateRegister(out var register, out _, out var titles);
		Assert.True(MuiRegisterCore.Initialize(ref platform, State, register));
		Assert.True(MuiRegisterCore.TryGetPolicyState(ref platform, State, register,
			out var valid));
		Assert.True(MuiRegisterPolicyStateAdmission.Validate(ref platform, valid));
		Assert.True(MuiRegisterPolicyStateAdmission.ValidateLive(ref platform,
			State, register, valid));
		valid.Titles = APTR.FromPointer(0x3000);
		Assert.True(MuiRegisterPolicyStateAdmission.Validate(ref platform, valid));
		valid.Titles = APTR.FromPointer(0xF0000);
		Assert.False(MuiRegisterPolicyStateAdmission.Validate(ref platform, valid));
		Assert.False(MuiRegisterPolicyStateAdmission.ValidateLive(ref platform,
			State, register, valid));
		valid.Titles = titles;
		Assert.False(MuiRegisterPolicyStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedRegisterTitlesFailsClosedBeforeRepairOrGet()
	{
		var platform = CreateRegister(out var register, out _, out _);
		Assert.True(MuiRegisterCore.Initialize(ref platform, State, register));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			register, MuiRegisterCore.Titles, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, register,
			PolicyStateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiRegisterPolicyStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiRegisterPolicyStateField.Titles, 0xF0000));
		Assert.True(MuiRegisterPolicyStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0xF0000u, structural.Titles.Raw);
		Assert.False(MuiRegisterPolicyStateAdmission.Validate(ref platform,
			structural));
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiRegisterCore.TryGetAttribute(ref platform, State, register,
			MuiRegisterCore.Titles, out _));
		Assert.False(MuiRegisterCore.TryGetPolicyState(ref platform, State, register,
			out _));
		Assert.False(MuiRegisterCore.Initialize(ref platform, State, register));
		Assert.False(MuiRegisterCore.SetActive(ref platform, State, register, 0));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, register,
			PolicyStateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			register, MuiRegisterCore.Titles, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
	}

	[Fact]
	public void RegisterPolicyRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreateRegister(out _, out _, out var titles);
		var address = APTR.FromPointer(0x1A60);
		var record = new MuiRegisterPolicyStateRecord
		{
			Magic = MuiRegisterPolicyStateRecord.Cookie,
			Frame = 1,
			Titles = titles,
		};
		Assert.True(MuiRegisterPolicyStateRecordCodec.Write(ref platform, address,
			record));
		Assert.True(MuiRegisterPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiRegisterPolicyStateField.Titles, out var typedTitlesAddress));
		Assert.Equal(0x1A68u, typedTitlesAddress.Raw);
		Assert.True(MuiRegisterPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiRegisterPolicyStateField.Frame, out var typedFrame));
		Assert.Equal(1u, typedFrame);
		Assert.True(MuiRegisterPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiRegisterPolicyStateField.Frame, 0));
		Assert.True(MuiRegisterPolicyStateRecordCodec.TryReadStructural(ref platform,
			address, out var typedUpdated));
		Assert.Equal(0u, typedUpdated.Frame);
		Assert.Equal(record.Titles, typedUpdated.Titles);
		Assert.True(MuiRegisterPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiRegisterPolicyStateField.Frame, 1));
		Assert.False(MuiRegisterPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, (MuiRegisterPolicyStateField)0xFF, out _));
		Assert.True(MuiRegisterPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, 8, out var titlesAddress));
		Assert.Equal(0x1A68u, titlesAddress.Raw);
		Assert.True(MuiRegisterPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out var frame));
		Assert.Equal(1u, frame);
		Assert.True(MuiRegisterPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 4, 0));
		Assert.True(MuiRegisterPolicyStateRecordCodec.TryReadStructural(ref platform,
			address, out var updated));
		Assert.Equal(0u, updated.Frame);
		Assert.False(MuiRegisterPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiRegisterPolicyStateRecord.Size, out _));
		Assert.False(MuiRegisterPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, (uint)0, out _));
		Assert.False(MuiRegisterPolicyStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreateRegister(out APTR register,
		out APTR groupClass, out APTR titles)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var groupName = APTR.FromPointer(0x1100);
		var registerName = APTR.FromPointer(0x1140);
		platform.WriteCString(groupName, "Group.mui");
		platform.WriteCString(registerName, "Register.mui");
		groupClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			groupName, APTR.Null, 0, APTR.FromPointer(1), false);
		var registerClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			registerName, groupClass, 0, APTR.FromPointer(1), false);
		titles = APTR.FromPointer(0x1180);
		platform.WriteCString(titles, "First");
		var tags = APTR.FromPointer(0x1200);
		platform.WriteUInt32(tags, 0, MuiRegisterCore.Frame);
		platform.WriteUInt32(tags, 4, 1);
		platform.WriteUInt32(tags, 8, MuiRegisterCore.Titles);
		platform.WriteUInt32(tags, 12, titles.Raw);
		platform.WriteUInt32(tags, 16, 0);
		platform.WriteUInt32(tags, 20, 0);
		register = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			registerClass, tags);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, register, child));
		return platform;
	}
}
