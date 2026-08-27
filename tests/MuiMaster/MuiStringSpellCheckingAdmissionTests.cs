using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringSpellCheckingAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint StringSpellChecking = 0x804266C6;
	private const uint StateKey = 0x7F070013;

	[Fact]
	public void StringSpellCheckingAdmissionRequiresCanonicalBooleanAndOwner()
	{
		var platform = CreatePlatform(out var stringClass);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, 0xFFFFFFFFu));
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryGetStringSpellCheckingStateRecord(
			ref platform, State, stringObj, out var valid));
		Assert.Equal(1u, valid.Enabled);
		Assert.True(MuiStringSpellCheckingStateAdmission.Validate(valid));
		Assert.True(MuiStringSpellCheckingStateAdmission.ValidateLive(ref platform,
			State, stringObj, valid));

		var malformed = valid;
		malformed.Enabled = 2;
		Assert.False(MuiStringSpellCheckingStateAdmission.Validate(malformed));
		Assert.False(MuiStringSpellCheckingStateAdmission.ValidateLive(ref platform,
			State, stringObj, malformed));
		malformed = valid;
		malformed.Magic = 0;
		Assert.False(MuiStringSpellCheckingStateAdmission.Validate(malformed));
		Assert.False(MuiStringSpellCheckingStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedStringSpellCheckingFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var stringClass);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, 1));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringSpellChecking, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, stringObj,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiStringSpellCheckingStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiStringSpellCheckingStateField.Enabled, 2));
		Assert.True(MuiStringSpellCheckingStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(2u, structural.Enabled);
		Assert.False(MuiStringSpellCheckingStateAdmission.Validate(structural));
		Assert.False(MuiStringSpellCheckingStateAdmission.ValidateLive(ref platform,
			State, stringObj, structural));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetStringSpellCheckingStateRecord(
			ref platform, State, stringObj, out _));
		Assert.False(MuiCommonControlCore.TryReadStringSpellCheckingState(
			ref platform, State, stringObj, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			StringSpellChecking, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringSpellChecking, 0));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			stringObj, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringSpellChecking, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiStringSpellCheckingStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiStringSpellCheckingStateField.Enabled,
			out var preserved));
		Assert.Equal(2u, preserved);
	}

	[Fact]
	public void StringSpellCheckingRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var recordAddress = APTR.FromPointer(0x1D40);
		var value = new MuiStringSpellCheckingStateRecord
		{
			Magic = MuiStringSpellCheckingStateRecord.Cookie,
			Enabled = 1,
		};
		Assert.True(MuiStringSpellCheckingStateRecordCodec.Write(ref platform,
			recordAddress, value));
		Assert.True(MuiStringSpellCheckingStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, 4, out var enabledAddress));
		Assert.Equal(0x1D44u, enabledAddress.Raw);
		Assert.True(MuiStringSpellCheckingStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, 4, out var enabled));
		Assert.Equal(1u, enabled);
		Assert.True(MuiStringSpellCheckingStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, recordAddress, 4, 0));
		Assert.True(MuiStringSpellCheckingStateRecordCodec.TryReadStructural(
			ref platform, recordAddress, out var decoded));
		Assert.Equal(0u, decoded.Enabled);
		Assert.False(MuiStringSpellCheckingStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, MuiStringSpellCheckingStateRecord.Size,
			out _));
		Assert.False(MuiStringSpellCheckingStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, 0, out _));
		Assert.False(MuiStringSpellCheckingStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR stringClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "String.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		stringClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform,
		uint address, uint enabled)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, StringSpellChecking);
		platform.WriteUInt32(tags, 4, enabled);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		return tags;
	}
}
