using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringInteger64AdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint StringInteger64 = 0x80424820;
	private const uint StateKey = 0x7F07000F;

	[Fact]
	public void StringInteger64AdmissionRequiresMappedValueAndOwner()
	{
		var platform = CreatePlatform(out var stringClass, out var source);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, source));
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryReadStringInteger64State(ref platform,
			State, stringObj, out var stateValue, out var valid));
		Assert.Equal(5u, valid.Low);
		Assert.True(MuiStringInteger64StateAdmission.Validate(stateValue.Value));
		Assert.True(MuiStringInteger64StateAdmission.ValidateLive(ref platform,
			State, stringObj, stateValue.Value));
		var malformed = APTR.FromPointer(0x30000);
		Assert.True(MuiStringInteger64StateAdmission.Validate(malformed));
		Assert.False(MuiStringInteger64StateAdmission.ValidateLive(ref platform,
			State, stringObj, malformed));
		Assert.False(MuiStringInteger64StateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), stateValue.Value));
	}

	[Fact]
	public void MalformedStringInteger64FailsClosedBeforeClearOrReplacement()
	{
		var platform = CreatePlatform(out var stringClass, out var source);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, source));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringInteger64, out var rawBefore));
		Assert.NotEqual(0u, rawBefore);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			stringObj, StringInteger64, 0x30000, false));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, stringObj,
			StateKey);
		Assert.True(block.IsNotNull);
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryReadStringInteger64State(ref platform,
			State, stringObj, out _, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			StringInteger64, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringInteger64, 0));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringInteger64, source.Raw));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			stringObj, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringInteger64, out var rawAfter));
		Assert.Equal(0x30000u, rawAfter);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR stringClass,
		out APTR source)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		source = APTR.FromPointer(0x1800);
		platform.WriteCString(name, "String.mui");
		platform.WriteUInt32(source, 0, 0);
		platform.WriteUInt32(source, 4, 5);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		stringClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform,
		uint address, APTR source)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, StringInteger64);
		platform.WriteUInt32(tags, 4, source.Raw);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		return tags;
	}
}
