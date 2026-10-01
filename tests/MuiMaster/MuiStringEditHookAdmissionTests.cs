using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringEditHookAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint StringEditHook = 0x80424C33;
	private const uint StringLonelyEditHook = 0x80421569;
	private const uint StateKey = 0x7F070016;

	[Fact]
	public void StringEditHookAdmissionRequiresCanonicalBooleanMappedHookAndOwner()
	{
		var platform = CreatePlatform(out var stringClass);
		var hook = APTR.FromPointer(0x1800);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, hook.Raw, 1));
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryGetStringEditHookStateRecord(
			ref platform, State, stringObj, out var valid));
		Assert.Equal(hook.Raw, valid.EditHook.Raw);
		Assert.Equal(1u, valid.LonelyEditHook);
		Assert.True(MuiStringEditHookStateAdmission.Validate(valid));
		Assert.True(MuiStringEditHookStateAdmission.ValidateLive(ref platform,
			State, stringObj, valid));

		var malformed = valid;
		malformed.LonelyEditHook = 2;
		Assert.False(MuiStringEditHookStateAdmission.Validate(malformed));
		Assert.False(MuiStringEditHookStateAdmission.ValidateLive(ref platform,
			State, stringObj, malformed));
		malformed = valid;
		malformed.EditHook = APTR.FromPointer(0x7F0000);
		Assert.True(MuiStringEditHookStateAdmission.Validate(malformed));
		Assert.False(MuiStringEditHookStateAdmission.ValidateLive(ref platform,
			State, stringObj, malformed));
		malformed = valid;
		malformed.Magic = 0;
		Assert.False(MuiStringEditHookStateAdmission.Validate(malformed));
		Assert.False(MuiStringEditHookStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedStringEditHookFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var stringClass);
		var hook = APTR.FromPointer(0x1800);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, hook.Raw, 1));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringLonelyEditHook, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, stringObj,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiStringEditHookStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiStringEditHookStateField.LonelyEditHook, 2));
		Assert.True(MuiStringEditHookStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(2u, structural.LonelyEditHook);
		Assert.False(MuiStringEditHookStateAdmission.Validate(structural));
		Assert.False(MuiStringEditHookStateAdmission.ValidateLive(ref platform,
			State, stringObj, structural));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetStringEditHookStateRecord(
			ref platform, State, stringObj, out _));
		Assert.False(MuiCommonControlCore.TryReadStringEditHookState(ref platform,
			State, stringObj, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			StringLonelyEditHook, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringLonelyEditHook, 0));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			stringObj, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringLonelyEditHook, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiStringEditHookStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiStringEditHookStateField.LonelyEditHook,
			out var preserved));
		Assert.Equal(2u, preserved);
	}

	[Fact]
	public void StringEditHookRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var recordAddress = APTR.FromPointer(0x1D40);
		var value = new MuiStringEditHookStateRecord
		{
			Magic = MuiStringEditHookStateRecord.Cookie,
			EditHook = APTR.FromPointer(0x1DC0),
			LonelyEditHook = 1,
		};
		Assert.True(MuiStringEditHookStateRecordCodec.Write(ref platform,
			recordAddress, value));
		Assert.True(MuiStringEditHookStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, MuiStringEditHookStateField.EditHook,
			out var typedHookAddress));
		Assert.Equal(0x1D44u, typedHookAddress.Raw);
		var hookCursor = new MuiStringEditHookStateFieldCursor
		{
			Record = recordAddress,
			Field = MuiStringEditHookStateField.EditHook,
		};
		Assert.True(MuiStringEditHookStateFieldCursorCodec.TryGetAddress(ref platform,
			hookCursor, out var cursorHookAddress, out var cursorFieldSize));
		Assert.Equal(typedHookAddress, cursorHookAddress);
		Assert.Equal(MuiStringEditHookStateRecord.FieldSize, cursorFieldSize);
		Assert.True(MuiStringEditHookStateRecordMemoryCodec.TryGetAddress(ref platform,
			hookCursor, out var memoryHookAddress, out var memoryFieldSize));
		Assert.Equal(cursorHookAddress, memoryHookAddress);
		Assert.Equal(cursorFieldSize, memoryFieldSize);
		Assert.True(MuiStringEditHookStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress,
			MuiStringEditHookStateField.LonelyEditHook, out var typedLonelyAddress));
		Assert.Equal(0x1D48u, typedLonelyAddress.Raw);
		Assert.True(MuiStringEditHookStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, MuiStringEditHookStateField.EditHook,
			out var typedHook));
		Assert.Equal(0x1DC0u, typedHook);
		Assert.True(MuiStringEditHookStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, recordAddress, MuiStringEditHookStateField.LonelyEditHook, 0));
		Assert.True(MuiStringEditHookStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, MuiStringEditHookStateField.Magic,
			out var typedMagic));
		Assert.Equal(MuiStringEditHookStateRecord.Cookie, typedMagic);
		Assert.False(MuiStringEditHookStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, (MuiStringEditHookStateField)255, out _));
		Assert.True(MuiStringEditHookStateRecordCodec.TryReadStructural(ref platform,
			recordAddress, out var typedUpdated));
		Assert.Equal(MuiStringEditHookStateRecord.Cookie, typedUpdated.Magic);
		Assert.Equal(0x1DC0u, typedUpdated.EditHook.Raw);
		Assert.Equal(0u, typedUpdated.LonelyEditHook);
		Assert.True(MuiStringEditHookStateRecordMemoryCodec.TryGetAddress(ref platform,
			recordAddress, 4, out var hookAddress));
		Assert.Equal(0x1D44u, hookAddress.Raw);
		Assert.True(MuiStringEditHookStateRecordMemoryCodec.TryReadUInt32(ref platform,
			recordAddress, 4, out var hook));
		Assert.Equal(0x1DC0u, hook);
		Assert.True(MuiStringEditHookStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, recordAddress, 8, 0));
		Assert.True(MuiStringEditHookStateRecordCodec.TryReadStructural(ref platform,
			recordAddress, out var decoded));
		Assert.Equal(0u, decoded.LonelyEditHook);
		Assert.False(MuiStringEditHookStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, MuiStringEditHookStateRecord.Size, out _));
		Assert.False(MuiStringEditHookStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, (uint)0, out _));
		Assert.False(MuiStringEditHookStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
		hookCursor.Field = (MuiStringEditHookStateField)255;
		Assert.False(MuiStringEditHookStateFieldCursorCodec.TryGetAddress(ref platform,
			hookCursor, out _, out _));
		hookCursor.Record = APTR.Null;
		hookCursor.Field = MuiStringEditHookStateField.EditHook;
		Assert.False(MuiStringEditHookStateFieldCursorCodec.TryGetAddress(ref platform,
			hookCursor, out _, out _));
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
		uint address, uint hook, uint lonely)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, StringEditHook);
		platform.WriteUInt32(tags, 4, hook);
		platform.WriteUInt32(tags, 8, StringLonelyEditHook);
		platform.WriteUInt32(tags, 12, lonely);
		platform.WriteUInt32(tags, 16, 0);
		platform.WriteUInt32(tags, 20, 0);
		return tags;
	}
}
