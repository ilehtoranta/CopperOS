using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringFilterAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint StringAccept = 0x8042E3E1;
	private const uint StringReject = 0x8042179C;
	private const uint StateKey = 0x7F070017;

	[Fact]
	public void StringFilterAdmissionRequiresCanonicalPointersAndOwner()
	{
		var platform = CreatePlatform(out var stringClass, out var accept,
			out var reject);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, accept, reject));
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryGetStringFilterStateRecord(
			ref platform, State, stringObj, out var valid));
		Assert.Equal(accept.Raw, valid.Accept.Raw);
		Assert.Equal(reject.Raw, valid.Reject.Raw);
		Assert.True(MuiStringFilterStateAdmission.Validate(valid));
		Assert.True(MuiStringFilterStateAdmission.ValidateLive(ref platform,
			State, stringObj, valid));

		var malformed = valid;
		malformed.Accept = APTR.FromPointer(0x30000);
		Assert.True(MuiStringFilterStateAdmission.Validate(malformed));
		Assert.False(MuiStringFilterStateAdmission.ValidateLive(ref platform,
			State, stringObj, malformed));
		malformed = valid;
		malformed.Magic = 0;
		Assert.False(MuiStringFilterStateAdmission.Validate(malformed));
		Assert.False(MuiStringFilterStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedStringFilterFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var stringClass, out var accept,
			out var reject);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, accept, reject));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringReject, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, stringObj,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiStringFilterStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiStringFilterStateField.Reject, 0x30000));
		Assert.True(MuiStringFilterStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0x30000u, structural.Reject.Raw);
		Assert.True(MuiStringFilterStateAdmission.Validate(structural));
		Assert.False(MuiStringFilterStateAdmission.ValidateLive(ref platform,
			State, stringObj, structural));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetStringFilterStateRecord(
			ref platform, State, stringObj, out _));
		Assert.False(MuiCommonControlCore.TryReadStringFilterState(ref platform,
			State, stringObj, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			StringReject, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringReject, 0));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			stringObj, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringReject, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiStringFilterStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiStringFilterStateField.Reject,
			out var preserved));
		Assert.Equal(0x30000u, preserved);
	}

	[Fact]
	public void StringFilterRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var recordAddress = APTR.FromPointer(0x1D60);
		var value = new MuiStringFilterStateRecord
		{
			Magic = MuiStringFilterStateRecord.Cookie,
			Accept = APTR.FromPointer(0x1DC0),
			Reject = APTR.FromPointer(0x1DE0),
		};
		Assert.True(MuiStringFilterStateRecordCodec.Write(ref platform,
			recordAddress, value));
		Assert.True(MuiStringFilterStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, MuiStringFilterStateField.Accept,
			out var typedAcceptAddress));
		Assert.Equal(0x1D64u, typedAcceptAddress.Raw);
		Assert.True(MuiStringFilterStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, MuiStringFilterStateField.Reject,
			out var typedRejectAddress));
		Assert.Equal(0x1D68u, typedRejectAddress.Raw);
		Assert.True(MuiStringFilterStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, MuiStringFilterStateField.Accept,
			out var typedAccept));
		Assert.Equal(0x1DC0u, typedAccept);
		Assert.True(MuiStringFilterStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, recordAddress, MuiStringFilterStateField.Reject, 0));
		Assert.True(MuiStringFilterStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, MuiStringFilterStateField.Reject,
			out var typedReject));
		Assert.Equal(0u, typedReject);
		Assert.False(MuiStringFilterStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, (MuiStringFilterStateField)255, out _));
		Assert.True(MuiStringFilterStateRecordCodec.TryReadStructural(ref platform,
			recordAddress, out var typedUpdated));
		Assert.Equal(MuiStringFilterStateRecord.Cookie, typedUpdated.Magic);
		Assert.Equal(0x1DC0u, typedUpdated.Accept.Raw);
		Assert.True(typedUpdated.Reject.IsNull);
		Assert.True(MuiStringFilterStateRecordMemoryCodec.TryGetAddress(ref platform,
			recordAddress, 8, out var rejectAddress));
		Assert.Equal(0x1D68u, rejectAddress.Raw);
		Assert.True(MuiStringFilterStateRecordMemoryCodec.TryReadUInt32(ref platform,
			recordAddress, 4, out var accept));
		Assert.Equal(0x1DC0u, accept);
		Assert.True(MuiStringFilterStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, recordAddress, 8, 0));
		Assert.True(MuiStringFilterStateRecordCodec.TryReadStructural(ref platform,
			recordAddress, out var decoded));
		Assert.Equal(0x1DC0u, decoded.Accept.Raw);
		Assert.True(decoded.Reject.IsNull);
		Assert.False(MuiStringFilterStateRecordMemoryCodec.TryGetAddress(ref platform,
			recordAddress, MuiStringFilterStateRecord.Size, out _));
		Assert.False(MuiStringFilterStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, (uint)0, out _));
		Assert.False(MuiStringFilterStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR stringClass,
		out APTR accept, out APTR reject)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		accept = APTR.FromPointer(0x1A00);
		reject = APTR.FromPointer(0x1A20);
		platform.WriteCString(name, "String.mui");
		platform.WriteCString(accept, "accepted");
		platform.WriteCString(reject, "rejected");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		stringClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform,
		uint address, APTR accept, APTR reject)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, StringAccept);
		platform.WriteUInt32(tags, 4, accept.Raw);
		platform.WriteUInt32(tags, 8, StringReject);
		platform.WriteUInt32(tags, 12, reject.Raw);
		platform.WriteUInt32(tags, 16, 0);
		platform.WriteUInt32(tags, 20, 0);
		return tags;
	}
}
