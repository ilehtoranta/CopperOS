using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringInteractionAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint StringEditable = 0x8042C94B;
	private const uint StringAdvanceOnCR = 0x804226DE;
	private const uint StringMultiline = 0x8042D18B;
	private const uint StateKey = 0x7F070012;

	[Fact]
	public void StringInteractionAdmissionRequiresCanonicalBooleansAndOwner()
	{
		var platform = CreatePlatform(out var stringClass);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, 1, 0, 1));
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryGetStringInteractionStateRecord(
			ref platform, State, stringObj, out var valid));
		Assert.True(MuiStringInteractionStateAdmission.Validate(valid));
		Assert.True(MuiStringInteractionStateAdmission.ValidateLive(ref platform,
			State, stringObj, valid));

		var malformed = valid;
		malformed.Editable = 2;
		Assert.False(MuiStringInteractionStateAdmission.Validate(malformed));
		Assert.False(MuiStringInteractionStateAdmission.ValidateLive(ref platform,
			State, stringObj, malformed));
		malformed = valid;
		malformed.Magic = 0;
		Assert.False(MuiStringInteractionStateAdmission.Validate(malformed));
		Assert.False(MuiStringInteractionStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedStringInteractionFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var stringClass);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, 1, 0, 1));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringEditable, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, stringObj,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiStringInteractionStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiStringInteractionStateField.Editable, 2));
		Assert.True(MuiStringInteractionStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(2u, structural.Editable);
		Assert.False(MuiStringInteractionStateAdmission.Validate(structural));
		Assert.False(MuiStringInteractionStateAdmission.ValidateLive(ref platform,
			State, stringObj, structural));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetStringInteractionStateRecord(
			ref platform, State, stringObj, out _));
		Assert.False(MuiCommonControlCore.TryReadStringInteractionState(ref platform,
			State, stringObj, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			StringEditable, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringEditable, 0));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			stringObj, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringEditable, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiStringInteractionStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiStringInteractionStateField.Editable,
			out var preserved));
		Assert.Equal(2u, preserved);
	}

	[Fact]
	public void StringInteractionRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var recordAddress = APTR.FromPointer(0x1D20);
		var value = new MuiStringInteractionStateRecord
		{
			Magic = MuiStringInteractionStateRecord.Cookie,
			Editable = 1,
			AdvanceOnCR = 0,
			Multiline = 1,
		};
		Assert.True(MuiStringInteractionStateRecordCodec.Write(ref platform,
			recordAddress, value));
		Assert.True(MuiStringInteractionStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, MuiStringInteractionStateField.Multiline,
			out var typedMultilineAddress));
		Assert.Equal(0x1D2Cu, typedMultilineAddress.Raw);
		var multilineCursor = new MuiStringInteractionStateFieldCursor
		{
			Record = recordAddress,
			Field = MuiStringInteractionStateField.Multiline,
		};
		Assert.True(MuiStringInteractionStateFieldCursorCodec.TryGetAddress(ref platform,
			multilineCursor, out var cursorMultilineAddress, out var cursorFieldSize));
		Assert.Equal(typedMultilineAddress, cursorMultilineAddress);
		Assert.Equal(MuiStringInteractionStateRecord.FieldSize, cursorFieldSize);
		Assert.True(MuiStringInteractionStateRecordMemoryCodec.TryGetAddress(ref platform,
			multilineCursor, out var memoryMultilineAddress, out var memoryFieldSize));
		Assert.Equal(cursorMultilineAddress, memoryMultilineAddress);
		Assert.Equal(cursorFieldSize, memoryFieldSize);
		Assert.True(MuiStringInteractionStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, MuiStringInteractionStateField.Editable,
			out var typedEditable));
		Assert.Equal(1u, typedEditable);
		Assert.True(MuiStringInteractionStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, recordAddress, MuiStringInteractionStateField.Multiline, 0));
		Assert.True(MuiStringInteractionStateRecordCodec.TryReadStructural(
			ref platform, recordAddress, out var typedUpdated));
		Assert.Equal(0u, typedUpdated.Multiline);
		Assert.Equal(value.Editable, typedUpdated.Editable);
		Assert.Equal(value.AdvanceOnCR, typedUpdated.AdvanceOnCR);
		Assert.False(MuiStringInteractionStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, (MuiStringInteractionStateField)255,
			out _));
		Assert.True(MuiStringInteractionStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, 12, out var multilineAddress));
		Assert.Equal(0x1D2Cu, multilineAddress.Raw);
		Assert.True(MuiStringInteractionStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, 4, out var editable));
		Assert.Equal(1u, editable);
		Assert.True(MuiStringInteractionStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, recordAddress, 12, 0));
		Assert.True(MuiStringInteractionStateRecordCodec.TryReadStructural(
			ref platform, recordAddress, out var decoded));
		Assert.Equal(0u, decoded.Multiline);
		Assert.False(MuiStringInteractionStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, MuiStringInteractionStateRecord.Size,
			out _));
		Assert.False(MuiStringInteractionStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, (uint)0, out _));
		Assert.False(MuiStringInteractionStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
		multilineCursor.Field = (MuiStringInteractionStateField)255;
		Assert.False(MuiStringInteractionStateFieldCursorCodec.TryGetAddress(ref platform,
			multilineCursor, out _, out _));
		multilineCursor.Record = APTR.Null;
		multilineCursor.Field = MuiStringInteractionStateField.Multiline;
		Assert.False(MuiStringInteractionStateFieldCursorCodec.TryGetAddress(ref platform,
			multilineCursor, out _, out _));
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
		uint address, uint editable, uint advanceOnCR, uint multiline)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, StringEditable);
		platform.WriteUInt32(tags, 4, editable);
		platform.WriteUInt32(tags, 8, StringAdvanceOnCR);
		platform.WriteUInt32(tags, 12, advanceOnCR);
		platform.WriteUInt32(tags, 16, StringMultiline);
		platform.WriteUInt32(tags, 20, multiline);
		platform.WriteUInt32(tags, 24, 0);
		platform.WriteUInt32(tags, 28, 0);
		return tags;
	}
}
