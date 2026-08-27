using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringIntegerAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint StringInteger = 0x80426E8A;
	private const uint StateKey = 0x7F070018;

	[Fact]
	public void StringIntegerAdmissionRequiresCanonicalRecordAndOwner()
	{
		var platform = CreatePlatform(out var stringClass);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, 37));
		Assert.NotEqual(APTR.Null, stringObj);
		Assert.True(MuiCommonControlCore.TryGetStringIntegerStateRecord(
			ref platform, State, stringObj, out var valid));
		Assert.Equal(37, valid.Value);
		Assert.True(MuiStringIntegerStateAdmission.Validate(valid));
		Assert.True(MuiStringIntegerStateAdmission.ValidateLive(ref platform,
			State, stringObj, valid));

		var malformed = valid;
		malformed.Magic = 0;
		Assert.False(MuiStringIntegerStateAdmission.Validate(malformed));
		Assert.False(MuiStringIntegerStateAdmission.ValidateLive(ref platform,
			State, stringObj, malformed));
		Assert.False(MuiStringIntegerStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedStringIntegerFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var stringClass);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, 37));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringInteger, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, stringObj,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiStringIntegerStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiStringIntegerStateField.Magic, 0));
		Assert.True(MuiStringIntegerStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiStringIntegerStateAdmission.Validate(structural));
		Assert.False(MuiStringIntegerStateAdmission.ValidateLive(ref platform,
			State, stringObj, structural));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetStringIntegerStateRecord(
			ref platform, State, stringObj, out _));
		Assert.False(MuiCommonControlCore.TryReadStringIntegerState(ref platform,
			State, stringObj, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			StringInteger, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringInteger, 99));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			stringObj, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringInteger, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiStringIntegerStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiStringIntegerStateField.Magic,
			out var preserved));
		Assert.Equal(0u, preserved);
	}

	[Fact]
	public void StringIntegerRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1A00);
		var record = new MuiStringIntegerStateRecord
		{
			Magic = MuiStringIntegerStateRecord.Cookie,
			Value = -123,
		};
		Assert.True(MuiStringIntegerStateRecordCodec.Write(ref platform, address,
			record));
		Assert.True(MuiStringIntegerStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiStringIntegerStateField.Value,
			out var valueAddress));
		Assert.Equal(0x1A04u, valueAddress.Raw);
		Assert.True(MuiStringIntegerStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiStringIntegerStateField.Value,
			out var raw));
		Assert.Equal(unchecked((uint)-123), raw);
		Assert.True(MuiStringIntegerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiStringIntegerStateField.Value,
			unchecked((uint)456)));
		Assert.True(MuiStringIntegerStateRecordCodec.TryReadStructural(ref platform,
			address, out var updated));
		Assert.Equal(456, updated.Value);
		Assert.False(MuiStringIntegerStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiStringIntegerStateField)255, out _));
		Assert.False(MuiStringIntegerStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiStringIntegerStateField.Magic, out _));
		Assert.False(MuiStringIntegerStateRecordCodec.TryReadStructural(ref platform,
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
		uint address, uint value)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, StringInteger);
		platform.WriteUInt32(tags, 4, value);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		return tags;
	}
}
