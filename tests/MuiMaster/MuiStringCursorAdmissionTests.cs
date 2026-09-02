using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringCursorAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint StringBufferPos = 0x80428B6C;
	private const uint StringDisplayPos = 0x8042CCBF;
	private const uint StateKey = 0x7F070010;

	[Fact]
	public void StringCursorAdmissionRequiresCanonicalNonnegativePositionsAndOwner()
	{
		var platform = CreatePlatform(out var stringClass);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, 0, 0));
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryGetStringCursorStateRecord(
			ref platform, State, stringObj, out var valid));
		Assert.Equal(0, valid.BufferPos);
		Assert.Equal(0, valid.DisplayPos);
		Assert.True(MuiStringCursorStateAdmission.Validate(valid));
		Assert.True(MuiStringCursorStateAdmission.ValidateLive(ref platform,
			State, stringObj, valid));

		var malformed = valid;
		malformed.BufferPos = -1;
		Assert.False(MuiStringCursorStateAdmission.Validate(malformed));
		Assert.False(MuiStringCursorStateAdmission.ValidateLive(ref platform,
			State, stringObj, malformed));
		Assert.False(MuiStringCursorStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedStringCursorFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var stringClass);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, 0, 0));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringBufferPos, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, stringObj,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiStringCursorStateFieldCursorCodec.TryWriteInt32(
			ref platform, block, MuiStringCursorStateField.BufferPos, -1));
		Assert.True(MuiStringCursorStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(-1, structural.BufferPos);
		Assert.False(MuiStringCursorStateAdmission.Validate(structural));
		Assert.False(MuiStringCursorStateAdmission.ValidateLive(ref platform,
			State, stringObj, structural));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetStringCursorStateRecord(
			ref platform, State, stringObj, out _));
		Assert.False(MuiCommonControlCore.TryReadStringCursorState(ref platform,
			State, stringObj, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			StringBufferPos, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringBufferPos, 0));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			stringObj, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringBufferPos, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiStringCursorStateFieldCursorCodec.TryReadInt32(
			ref platform, block, MuiStringCursorStateField.BufferPos,
			out var preserved));
		Assert.Equal(-1, preserved);
	}

	[Fact]
	public void StringCursorRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var recordAddress = APTR.FromPointer(0x1D20);
		var value = new MuiStringCursorStateRecord
		{
			Magic = MuiStringCursorStateRecord.Cookie,
			BufferPos = 12,
			DisplayPos = 5,
		};
		Assert.True(MuiStringCursorStateRecordCodec.Write(ref platform,
			recordAddress, value));
		Assert.True(MuiStringCursorStateRecordMemoryCodec.TryGetAddress(ref platform,
			recordAddress, MuiStringCursorStateField.DisplayPos,
			out var typedDisplayAddress));
		Assert.Equal(0x1D28u, typedDisplayAddress.Raw);
		Assert.True(MuiStringCursorStateRecordMemoryCodec.TryReadInt32(ref platform,
			recordAddress, MuiStringCursorStateField.BufferPos, out var typedBuffer));
		Assert.Equal(12, typedBuffer);
		Assert.True(MuiStringCursorStateRecordMemoryCodec.TryWriteInt32(ref platform,
			recordAddress, MuiStringCursorStateField.DisplayPos, 0));
		Assert.True(MuiStringCursorStateRecordCodec.TryReadStructural(ref platform,
			recordAddress, out var typedUpdated));
		Assert.Equal(0, typedUpdated.DisplayPos);
		Assert.Equal(value.BufferPos, typedUpdated.BufferPos);
		Assert.False(MuiStringCursorStateRecordMemoryCodec.TryReadUInt32(ref platform,
			recordAddress, (MuiStringCursorStateField)255, out _));
		Assert.True(MuiStringCursorStateRecordMemoryCodec.TryGetAddress(ref platform,
			recordAddress, 4, out var bufferAddress));
		Assert.Equal(0x1D24u, bufferAddress.Raw);
		Assert.True(MuiStringCursorStateRecordMemoryCodec.TryReadInt32(ref platform,
			recordAddress, 4, out var buffer));
		Assert.Equal(12, buffer);
		Assert.True(MuiStringCursorStateRecordMemoryCodec.TryWriteInt32(ref platform,
			recordAddress, 8, 0));
		Assert.True(MuiStringCursorStateRecordCodec.TryReadStructural(ref platform,
			recordAddress, out var decoded));
		Assert.Equal(0, decoded.DisplayPos);
		Assert.False(MuiStringCursorStateRecordMemoryCodec.TryGetAddress(ref platform,
			recordAddress, MuiStringCursorStateRecord.Size, out _));
		Assert.False(MuiStringCursorStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, (uint)0, out _));
		Assert.False(MuiStringCursorStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
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
		uint address, uint bufferPos, uint displayPos)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, StringBufferPos);
		platform.WriteUInt32(tags, 4, bufferPos);
		platform.WriteUInt32(tags, 8, StringDisplayPos);
		platform.WriteUInt32(tags, 12, displayPos);
		platform.WriteUInt32(tags, 16, 0);
		platform.WriteUInt32(tags, 20, 0);
		return tags;
	}
}
