using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringAttachedListAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint StringAttachedList = 0x80420FD2;
	private const uint StateKey = 0x7F070015;

	[Fact]
	public void StringAttachedListAdmissionRequiresCanonicalMagicLiveListviewAndOwner()
	{
		var platform = CreatePlatform(out var stringClass, out var listview);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, listview));
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryGetStringAttachedListStateRecord(
			ref platform, State, stringObj, out var valid));
		Assert.True(MuiStringAttachedListStateAdmission.Validate(valid));
		Assert.True(MuiStringAttachedListStateAdmission.ValidateLive(ref platform,
			State, stringObj, valid));

		var malformed = valid;
		malformed.Listview = APTR.FromPointer(0x30000);
		Assert.True(MuiStringAttachedListStateAdmission.Validate(malformed));
		Assert.False(MuiStringAttachedListStateAdmission.ValidateLive(ref platform,
			State, stringObj, malformed));
		malformed = valid;
		malformed.Magic = 0;
		Assert.False(MuiStringAttachedListStateAdmission.Validate(malformed));
		Assert.False(MuiStringAttachedListStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedStringAttachedListFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var stringClass, out var listview);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, listview));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringAttachedList, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, stringObj,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiStringAttachedListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiStringAttachedListStateField.Listview,
			0x30000));
		Assert.True(MuiStringAttachedListStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0x30000u, structural.Listview.Raw);
		Assert.True(MuiStringAttachedListStateAdmission.Validate(structural));
		Assert.False(MuiStringAttachedListStateAdmission.ValidateLive(ref platform,
			State, stringObj, structural));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetStringAttachedListStateRecord(
			ref platform, State, stringObj, out _));
		Assert.False(MuiCommonControlCore.TryReadStringAttachedListState(
			ref platform, State, stringObj, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			StringAttachedList, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringAttachedList, 0));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			stringObj, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringAttachedList, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiStringAttachedListStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiStringAttachedListStateField.Listview,
			out var preserved));
		Assert.Equal(0x30000u, preserved);
	}

	[Fact]
	public void StringAttachedListRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var recordAddress = APTR.FromPointer(0x1D20);
		var value = new MuiStringAttachedListStateRecord
		{
			Magic = MuiStringAttachedListStateRecord.Cookie,
			Listview = APTR.FromPointer(0x1D80),
		};
		Assert.True(MuiStringAttachedListStateRecordCodec.Write(ref platform,
			recordAddress, value));
		Assert.True(MuiStringAttachedListStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress,
			MuiStringAttachedListStateField.Listview, out var listviewAddress));
		Assert.Equal(0x1D24u, listviewAddress.Raw);
		Assert.True(MuiStringAttachedListStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, MuiStringAttachedListStateField.Listview,
			out var listview));
		Assert.Equal(0x1D80u, listview);
		Assert.True(MuiStringAttachedListStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, recordAddress, MuiStringAttachedListStateField.Listview, 0));
		Assert.True(MuiStringAttachedListStateRecordCodec.TryReadStructural(
			ref platform, recordAddress, out var decoded));
		Assert.True(decoded.Listview.IsNull);
		Assert.False(MuiStringAttachedListStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress,
			(MuiStringAttachedListStateField)255, out _));
		Assert.False(MuiStringAttachedListStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiStringAttachedListStateField.Magic, out _));
		Assert.False(MuiStringAttachedListStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR stringClass,
		out APTR listview)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var stringName = APTR.FromPointer(0x1100);
		var listName = APTR.FromPointer(0x1140);
		var listviewName = APTR.FromPointer(0x1180);
		platform.WriteCString(stringName, "String.mui");
		platform.WriteCString(listName, "List.mui");
		platform.WriteCString(listviewName, "Listview.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		stringClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			stringName, APTR.Null, 0, APTR.FromPointer(1), false);
		var listClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			listName, APTR.Null, 0, APTR.FromPointer(1), false);
		var listviewClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			listviewName, APTR.Null, 0, APTR.FromPointer(1), false);
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		listview = MuiListviewCore.CreateListview(ref platform, State,
			listviewClass, BuildTags(ref platform, 0x1800, list));
		Assert.NotEqual(APTR.Null, listview);
		return platform;
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform,
		uint address, APTR listview)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, StringAttachedList);
		platform.WriteUInt32(tags, 4, listview.Raw);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		return tags;
	}
}
