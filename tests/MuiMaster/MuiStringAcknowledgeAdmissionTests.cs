using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringAcknowledgeAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint StringContents = 0x80428FFD;
	private const uint StringAcknowledge = 0x8042026C;
	private const uint StateKey = 0x7F070014;

	[Fact]
	public void StringAcknowledgeAdmissionRequiresCanonicalMagicMappedStringAndOwner()
	{
		var platform = CreatePlatform(out var stringClass, out var source);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, source));
		var packet = APTR.FromPointer(0x1A00);
		platform.WriteUInt32(packet, 0, 0x80426D66);
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, 0);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		Assert.True(MuiCommonControlCore.TryGetStringAcknowledgeStateRecord(
			ref platform, State, stringObj, out var valid));
		Assert.True(MuiStringAcknowledgeStateAdmission.Validate(valid));
		Assert.True(MuiStringAcknowledgeStateAdmission.ValidateLive(ref platform,
			State, stringObj, valid));

		var malformed = valid;
		malformed.Contents = APTR.FromPointer(0x30000);
		Assert.True(MuiStringAcknowledgeStateAdmission.Validate(malformed));
		Assert.False(MuiStringAcknowledgeStateAdmission.ValidateLive(ref platform,
			State, stringObj, malformed));
		malformed = valid;
		malformed.Magic = 0;
		Assert.False(MuiStringAcknowledgeStateAdmission.Validate(malformed));
		Assert.False(MuiStringAcknowledgeStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedStringAcknowledgeFailsClosedBeforeRawRepairOrPublish()
	{
		var platform = CreatePlatform(out var stringClass, out var source);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, source));
		var packet = APTR.FromPointer(0x1A00);
		platform.WriteUInt32(packet, 0, 0x80426D66);
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, 0);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringAcknowledge, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, stringObj,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiStringAcknowledgeStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiStringAcknowledgeStateField.Contents, 0x30000));
		Assert.True(MuiStringAcknowledgeStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0x30000u, structural.Contents.Raw);
		Assert.True(MuiStringAcknowledgeStateAdmission.Validate(structural));
		Assert.False(MuiStringAcknowledgeStateAdmission.ValidateLive(ref platform,
			State, stringObj, structural));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetStringAcknowledgeStateRecord(
			ref platform, State, stringObj, out _));
		Assert.False(MuiCommonControlCore.TryReadStringAcknowledgeState(ref platform,
			State, stringObj, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			StringAcknowledge, out _, out _));
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			stringObj, packet));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			stringObj, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringAcknowledge, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiStringAcknowledgeStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiStringAcknowledgeStateField.Contents,
			out var preserved));
		Assert.Equal(0x30000u, preserved);
	}

	[Fact]
	public void StringAcknowledgeRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _, out var source);
		var address = APTR.FromPointer(0x1D40);
		var record = new MuiStringAcknowledgeStateRecord
		{
			Magic = MuiStringAcknowledgeStateRecord.Cookie,
			Contents = source,
		};
		Assert.True(MuiStringAcknowledgeStateRecordCodec.Write(ref platform,
			address, record));
		Assert.True(MuiStringAcknowledgeStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 4, out var contentsAddress));
		Assert.Equal(0x1D44u, contentsAddress.Raw);
		Assert.True(MuiStringAcknowledgeStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out var contents));
		Assert.Equal(source.Raw, contents);
		Assert.True(MuiStringAcknowledgeStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 4, 0));
		Assert.True(MuiStringAcknowledgeStateRecordCodec.TryReadStructural(
			ref platform, address, out var updated));
		Assert.Equal(APTR.Null, updated.Contents);
		Assert.False(MuiStringAcknowledgeStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiStringAcknowledgeStateRecord.Size, out _));
		Assert.False(MuiStringAcknowledgeStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, 0, out _));
		Assert.False(MuiStringAcknowledgeStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR stringClass,
		out APTR source)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		source = APTR.FromPointer(0x1800);
		platform.WriteCString(name, "String.mui");
		platform.WriteCString(source, "ack");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		stringClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform,
		uint address, APTR source)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, StringContents);
		platform.WriteUInt32(tags, 4, source.Raw);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		return tags;
	}
}
