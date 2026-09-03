using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiTextShortenedAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint TextShortened = 0x80425A86;
	private const uint StateKey = 0x7F070072;

	[Fact]
	public void TextShortenedAdmissionRequiresCanonicalStatusAndOwner()
	{
		var platform = CreatePlatform(out var textClass);
		var textObj = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, APTR.Null);
		Assert.NotEqual(APTR.Null, textObj);
		Assert.True(MuiCommonControlCore.TryGetTextShortenedStateRecord(
			ref platform, State, textObj, out var valid));
		Assert.True(MuiTextShortenedStateAdmission.Validate(valid));
		Assert.True(MuiTextShortenedStateAdmission.ValidateLive(ref platform,
			State, textObj, valid));
		var malformed = valid;
		malformed.Shortened = 2;
		Assert.False(MuiTextShortenedStateAdmission.Validate(malformed));
		Assert.False(MuiTextShortenedStateAdmission.ValidateLive(ref platform,
			State, textObj, malformed));
		Assert.False(MuiTextShortenedStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedTextShortenedFailsClosedBeforeRawRepairOrPublish()
	{
		var platform = CreatePlatform(out var textClass);
		var textObj = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, APTR.Null);
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			textObj, TextShortened, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, textObj,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiTextShortenedStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiTextShortenedStateField.Shortened, 2));
		Assert.True(MuiTextShortenedStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(2u, structural.Shortened);
		Assert.False(MuiTextShortenedStateAdmission.Validate(structural));
		Assert.False(MuiTextShortenedStateAdmission.ValidateLive(ref platform,
			State, textObj, structural));
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetTextShortenedStateRecord(
			ref platform, State, textObj, out _));
		Assert.False(MuiCommonControlCore.TryReadTextShortenedState(
			ref platform, State, textObj, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, textObj,
			TextShortened, out _, out _));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			textObj, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			textObj, TextShortened, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiTextShortenedStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiTextShortenedStateField.Shortened,
			out var preserved));
		Assert.Equal(2u, preserved);
	}

	[Fact]
	public void TextShortenedRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1B20);
		var record = new MuiTextShortenedStateRecord
		{
			Magic = MuiTextShortenedStateRecord.Cookie,
			Shortened = 1,
		};
		Assert.True(MuiTextShortenedStateRecordCodec.Write(ref platform, address,
			record));
		Assert.True(MuiTextShortenedStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiTextShortenedStateField.Shortened,
			out var typedShortenedAddress));
		Assert.Equal(0x1B24u, typedShortenedAddress.Raw);
		Assert.True(MuiTextShortenedStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiTextShortenedStateField.Shortened,
			out var typedShortened));
		Assert.Equal(1u, typedShortened);
		Assert.True(MuiTextShortenedStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 4, out var shortenedAddress));
		Assert.Equal(0x1B24u, shortenedAddress.Raw);
		Assert.True(MuiTextShortenedStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out var shortened));
		Assert.Equal(1u, shortened);
		Assert.True(MuiTextShortenedStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiTextShortenedStateField.Shortened, 0));
		Assert.True(MuiTextShortenedStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 4, 0));
		Assert.True(MuiTextShortenedStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiTextShortenedStateField.Magic,
			out var typedMagic));
		Assert.Equal(MuiTextShortenedStateRecord.Cookie, typedMagic);
		Assert.False(MuiTextShortenedStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiTextShortenedStateField)255, out _));
		Assert.True(MuiTextShortenedStateRecordCodec.TryReadStructural(ref platform,
			address, out var updated));
		Assert.Equal(0u, updated.Shortened);
		Assert.False(MuiTextShortenedStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiTextShortenedStateRecord.Size, out _));
		Assert.False(MuiTextShortenedStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, (uint)0, out _));
		Assert.False(MuiTextShortenedStateRecordCodec.TryReadStructural(ref platform,
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
}
