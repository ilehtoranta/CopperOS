using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiTextPresentationAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint TextShorten = 0x80428BBD;
	private const uint StateKey = 0x7F070032;

	[Fact]
	public void TextPresentationAdmissionRequiresCanonicalPolicyAndOwner()
	{
		var platform = CreatePlatform(out var textClass);
		var textObj = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, BuildTags(ref platform, 0x1900, 1));
		Assert.NotEqual(APTR.Null, textObj);
		Assert.True(MuiCommonControlCore.TryGetTextPresentationStateRecord(
			ref platform, State, textObj, out var valid));
		Assert.True(MuiTextPresentationStateAdmission.Validate(valid));
		Assert.True(MuiTextPresentationStateAdmission.ValidateLive(ref platform,
			State, textObj, valid));
		var malformed = valid;
		malformed.Shorten = 3;
		Assert.False(MuiTextPresentationStateAdmission.Validate(malformed));
		Assert.False(MuiTextPresentationStateAdmission.ValidateLive(ref platform,
			State, textObj, malformed));
		Assert.False(MuiTextPresentationStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedTextPresentationFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var textClass);
		var textObj = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, BuildTags(ref platform, 0x1900, 1));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			textObj, TextShorten, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, textObj,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiTextPresentationStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiTextPresentationStateField.Shorten, 3));
		Assert.True(MuiTextPresentationStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(3u, structural.Shorten);
		Assert.False(MuiTextPresentationStateAdmission.Validate(structural));
		Assert.False(MuiTextPresentationStateAdmission.ValidateLive(ref platform,
			State, textObj, structural));
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetTextPresentationStateRecord(
			ref platform, State, textObj, out _));
		Assert.False(MuiCommonControlCore.TryReadTextPresentationState(ref platform,
			State, textObj, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, textObj,
			TextShorten, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			textObj, TextShorten, 2));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			textObj, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			textObj, TextShorten, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiTextPresentationStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiTextPresentationStateField.Shorten,
			out var preserved));
		Assert.Equal(3u, preserved);
	}

	[Fact]
	public void TextPresentationRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x8000,
			State);
		var recordAddress = APTR.FromPointer(0x1D20);
		var value = new MuiTextPresentationStateRecord
		{
			Magic = MuiTextPresentationStateRecord.Cookie,
			SetMin = 1,
			SetMax = 0,
			SetVMax = 1,
			ControlChar = 13,
			Marking = 1,
			Shorten = 2,
			HiChar = 10,
			HiCharPresent = 1,
		};
		Assert.True(MuiTextPresentationStateRecordCodec.Write(ref platform,
			recordAddress, value));
		Assert.True(MuiTextPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, MuiTextPresentationStateField.Shorten,
			out var typedShortenAddress));
		Assert.Equal(0x1D38u, typedShortenAddress.Raw);
		Assert.True(MuiTextPresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, MuiTextPresentationStateField.ControlChar,
			out var typedControlChar));
		Assert.Equal(13u, typedControlChar);
		Assert.True(MuiTextPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, 24, out var shortenAddress));
		Assert.Equal(0x1D38u, shortenAddress.Raw);
		Assert.True(MuiTextPresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, 16, out var controlChar));
		Assert.Equal(13u, controlChar);
		Assert.True(MuiTextPresentationStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, recordAddress, MuiTextPresentationStateField.Shorten, 0));
		Assert.True(MuiTextPresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, MuiTextPresentationStateField.Magic,
			out var typedMagic));
		Assert.Equal(MuiTextPresentationStateRecord.Cookie, typedMagic);
		Assert.False(MuiTextPresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, (MuiTextPresentationStateField)255, out _));
		Assert.True(MuiTextPresentationStateRecordCodec.TryReadStructural(
			ref platform, recordAddress, out var typedUpdated));
		Assert.Equal(MuiTextPresentationStateRecord.Cookie, typedUpdated.Magic);
		Assert.Equal(value.SetMin, typedUpdated.SetMin);
		Assert.Equal(value.Marking, typedUpdated.Marking);
		Assert.Equal(0u, typedUpdated.Shorten);
		Assert.True(MuiTextPresentationStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, recordAddress, 24, 0));
		Assert.True(MuiTextPresentationStateRecordCodec.TryReadStructural(
			ref platform, recordAddress, out var decoded));
		Assert.Equal(0u, decoded.Shorten);
		Assert.False(MuiTextPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, MuiTextPresentationStateRecord.Size,
			out _));
		Assert.False(MuiTextPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, (uint)0, out _));
		Assert.False(MuiTextPresentationStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
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
		uint address, uint shorten)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, TextShorten);
		platform.WriteUInt32(tags, 4, shorten);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		return tags;
	}
}
