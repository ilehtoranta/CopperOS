using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiTextPreParseAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint TextPreParse = 0x8042566D;
	private const uint StateKey = 0x7F07001D;

	[Fact]
	public void TextPreParseAdmissionRequiresCanonicalMagicMappedStringAndOwner()
	{
		var platform = CreatePlatform(out var textClass, out var source);
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, BuildTags(ref platform, 0x1900, source));
		Assert.True(MuiCommonControlCore.TryGetTextPreParseStateRecord(
			ref platform, State, text, out var valid));
		Assert.True(MuiTextPreParseStateAdmission.Validate(valid));
		Assert.True(MuiTextPreParseStateAdmission.ValidateLive(ref platform, State,
			text, valid));

		var malformed = valid;
		malformed.PreParse = APTR.FromPointer(0x30000);
		Assert.True(MuiTextPreParseStateAdmission.Validate(malformed));
		Assert.False(MuiTextPreParseStateAdmission.ValidateLive(ref platform,
			State, text, malformed));
		malformed = valid;
		malformed.Magic = 0;
		Assert.False(MuiTextPreParseStateAdmission.Validate(malformed));
		Assert.False(MuiTextPreParseStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedTextPreParseFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var textClass, out var source);
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, BuildTags(ref platform, 0x1900, source));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			text, TextPreParse, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, text, StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiTextPreParseStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiTextPreParseStateField.PreParse, 0x30000));
		Assert.True(MuiTextPreParseStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0x30000u, structural.PreParse.Raw);
		Assert.True(MuiTextPreParseStateAdmission.Validate(structural));
		Assert.False(MuiTextPreParseStateAdmission.ValidateLive(ref platform,
			State, text, structural));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetTextPreParseStateRecord(
			ref platform, State, text, out _));
		Assert.False(MuiCommonControlCore.TryReadTextPreParseState(ref platform,
			State, text, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, text,
			TextPreParse, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			text, TextPreParse, 0));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, text,
			StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, text,
			TextPreParse, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiTextPreParseStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiTextPreParseStateField.PreParse,
			out var preserved));
		Assert.Equal(0x30000u, preserved);
	}

	[Fact]
	public void TextPreParseRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var recordAddress = APTR.FromPointer(0x1D60);
		var value = new MuiTextPreParseStateRecord
		{
			Magic = MuiTextPreParseStateRecord.Cookie,
			PreParse = APTR.FromPointer(0x1DC0),
		};
		Assert.True(MuiTextPreParseStateRecordCodec.Write(ref platform,
			recordAddress, value));
		Assert.True(MuiTextPreParseStateRecordMemoryCodec.TryGetAddress(ref platform,
			recordAddress, 4, out var preParseAddress));
		Assert.Equal(0x1D64u, preParseAddress.Raw);
		Assert.True(MuiTextPreParseStateRecordMemoryCodec.TryReadUInt32(ref platform,
			recordAddress, 4, out var preParse));
		Assert.Equal(0x1DC0u, preParse);
		Assert.True(MuiTextPreParseStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			recordAddress, 4, 0));
		Assert.True(MuiTextPreParseStateRecordCodec.TryReadStructural(ref platform,
			recordAddress, out var decoded));
		Assert.True(decoded.PreParse.IsNull);
		Assert.False(MuiTextPreParseStateRecordMemoryCodec.TryGetAddress(ref platform,
			recordAddress, MuiTextPreParseStateRecord.Size, out _));
		Assert.False(MuiTextPreParseStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, 0, out _));
		Assert.False(MuiTextPreParseStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR textClass,
		out APTR source)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		source = APTR.FromPointer(0x1800);
		platform.WriteCString(name, "Text.mui");
		platform.WriteCString(source, "\\33p[2]");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		textClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform, uint address,
		APTR source)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, TextPreParse);
		platform.WriteUInt32(tags, 4, source.Raw);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		return tags;
	}
}
