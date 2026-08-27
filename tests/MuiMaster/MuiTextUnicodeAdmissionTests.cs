using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiTextUnicodeAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint Unicode = 0x8042E7D0;
	private const uint StateKey = 0x7F070071;

	[Fact]
	public void TextUnicodeAdmissionRequiresCanonicalModeAndOwner()
	{
		var platform = CreatePlatform(out var textClass);
		var textObj = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, BuildTags(ref platform, 0x1900, 1));
		Assert.NotEqual(APTR.Null, textObj);
		Assert.True(MuiCommonControlCore.TryGetTextUnicodeStateRecord(
			ref platform, State, textObj, out var valid));
		Assert.True(MuiTextUnicodeStateAdmission.Validate(valid));
		Assert.True(MuiTextUnicodeStateAdmission.ValidateLive(ref platform,
			State, textObj, valid));
		var malformed = valid;
		malformed.Unicode = 2;
		Assert.False(MuiTextUnicodeStateAdmission.Validate(malformed));
		Assert.False(MuiTextUnicodeStateAdmission.ValidateLive(ref platform,
			State, textObj, malformed));
		Assert.False(MuiTextUnicodeStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedTextUnicodeFailsClosedBeforeRawRepairOrMetrics()
	{
		var platform = CreatePlatform(out var textClass);
		var textObj = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, BuildTags(ref platform, 0x1A00, 1));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			textObj, Unicode, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, textObj,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiTextUnicodeStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiTextUnicodeStateField.Unicode, 2));
		Assert.True(MuiTextUnicodeStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(2u, structural.Unicode);
		Assert.False(MuiTextUnicodeStateAdmission.Validate(structural));
		Assert.False(MuiTextUnicodeStateAdmission.ValidateLive(ref platform,
			State, textObj, structural));
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetTextUnicodeStateRecord(
			ref platform, State, textObj, out _));
		Assert.False(MuiCommonControlCore.TryReadTextUnicodeState(
			ref platform, State, textObj, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, textObj,
			Unicode, out _, out _));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			textObj, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			textObj, Unicode, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiTextUnicodeStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiTextUnicodeStateField.Unicode,
			out var preserved));
		Assert.Equal(2u, preserved);
	}

	[Fact]
	public void TextUnicodeRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1B00);
		var record = new MuiTextUnicodeStateRecord
		{
			Magic = MuiTextUnicodeStateRecord.Cookie,
			Unicode = 1,
		};
		Assert.True(MuiTextUnicodeStateRecordCodec.Write(ref platform, address,
			record));
		Assert.True(MuiTextUnicodeStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 4, out var unicodeAddress));
		Assert.Equal(0x1B04u, unicodeAddress.Raw);
		Assert.True(MuiTextUnicodeStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out var unicode));
		Assert.Equal(1u, unicode);
		Assert.True(MuiTextUnicodeStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 4, 0));
		Assert.True(MuiTextUnicodeStateRecordCodec.TryReadStructural(ref platform,
			address, out var updated));
		Assert.Equal(0u, updated.Unicode);
		Assert.False(MuiTextUnicodeStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiTextUnicodeStateRecord.Size, out _));
		Assert.False(MuiTextUnicodeStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, 0, out _));
		Assert.False(MuiTextUnicodeStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR textClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x8000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Text.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		textClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 1, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform,
		uint address, uint unicode)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, Unicode);
		platform.WriteUInt32(tags, 4, unicode);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		return tags;
	}
}
