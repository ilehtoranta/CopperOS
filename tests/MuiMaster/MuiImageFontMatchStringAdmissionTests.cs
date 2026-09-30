using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiImageFontMatchStringAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint ImageFontMatchString = 0x804263C1;
	private const uint StateKey = 0x7F070026;

	[Fact]
	public void ImageFontMatchStringAdmissionRequiresCanonicalPresenceMappedStringAndOwner()
	{
		var platform = CreatePlatform(out var imageClass, out var matchString);
		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, BuildTags(ref platform, 0x1900, matchString));
		var valid = new MuiImageFontMatchStringStateRecord
		{
			Magic = MuiImageFontMatchStringStateRecord.Cookie,
			Present = 1,
			MatchString = matchString,
		};
		Assert.True(MuiImageFontMatchStringStateAdmission.Validate(valid));
		Assert.True(MuiImageFontMatchStringStateAdmission.ValidateLive(
			ref platform, State, image, valid));

		var malformed = valid;
		malformed.Present = 2;
		Assert.False(MuiImageFontMatchStringStateAdmission.Validate(malformed));
		Assert.False(MuiImageFontMatchStringStateAdmission.ValidateLive(
			ref platform, State, image, malformed));

		malformed = valid;
		malformed.Present = 0;
		malformed.MatchString = APTR.FromPointer(0x30000);
		Assert.True(MuiImageFontMatchStringStateAdmission.Validate(malformed));
		Assert.False(MuiImageFontMatchStringStateAdmission.ValidateLive(
			ref platform, State, image, malformed));
		Assert.False(MuiImageFontMatchStringStateAdmission.ValidateLive(
			ref platform, State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void ImageFontMatchStringRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _, out var matchString);
		var address = APTR.FromPointer(0x1BC0);
		var value = new MuiImageFontMatchStringStateRecord
		{
			Magic = MuiImageFontMatchStringStateRecord.Cookie,
			Present = 1,
			MatchString = matchString,
		};

		Assert.True(MuiImageFontMatchStringStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiImageFontMatchStringStateRecordCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.Present, structural.Present);
		Assert.Equal(value.MatchString, structural.MatchString);
		Assert.True(MuiImageFontMatchStringStateRecordCodec.TryRead(ref platform,
			address, out _));
		Assert.True(MuiImageFontMatchStringStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 8, out var matchField));
		Assert.Equal(address.Raw + 8, matchField.Raw);
		Assert.True(MuiImageFontMatchStringStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 8, out var matchRaw));
		Assert.Equal(value.MatchString.Raw, matchRaw);
		Assert.True(MuiImageFontMatchStringStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiImageFontMatchStringStateField.MatchString,
			out var typedMatch));
		Assert.Equal(address.Raw + MuiImageFontMatchStringStateRecord.MatchStringOffset,
			typedMatch.Raw);
		var matchCursor = new MuiImageFontMatchStringStateFieldCursor
		{
			Record = address,
			Field = MuiImageFontMatchStringStateField.MatchString,
		};
		Assert.True(MuiImageFontMatchStringStateFieldCursorCodec.TryGetAddress(
			ref platform, matchCursor, out var cursorMatch, out var cursorFieldSize));
		Assert.Equal(typedMatch, cursorMatch);
		Assert.Equal(MuiImageFontMatchStringStateRecord.FieldSize, cursorFieldSize);
		Assert.True(MuiImageFontMatchStringStateRecordMemoryCodec.TryGetAddress(
			ref platform, matchCursor, out var memoryMatch, out var memoryFieldSize));
		Assert.Equal(cursorMatch, memoryMatch);
		Assert.Equal(cursorFieldSize, memoryFieldSize);
		Assert.True(MuiImageFontMatchStringStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiImageFontMatchStringStateField.MatchString,
			0x2B00));
		Assert.True(MuiImageFontMatchStringStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiImageFontMatchStringStateField.Magic,
			out var typedMagic));
		Assert.Equal(value.Magic, typedMagic);
		Assert.True(MuiImageFontMatchStringStateRecordCodec.TryReadStructural(
			ref platform, address, out var typedStructural));
		Assert.Equal(0x2B00u, typedStructural.MatchString.Raw);
		Assert.False(MuiImageFontMatchStringStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiImageFontMatchStringStateField)0xFF, out _));
		Assert.False(MuiImageFontMatchStringStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiImageFontMatchStringStateRecord.Size, out _));
		Assert.False(MuiImageFontMatchStringStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, (uint)0, out _));
		matchCursor.Field = (MuiImageFontMatchStringStateField)255;
		Assert.False(MuiImageFontMatchStringStateFieldCursorCodec.TryGetAddress(
			ref platform, matchCursor, out _, out _));
		matchCursor.Record = APTR.Null;
		matchCursor.Field = MuiImageFontMatchStringStateField.MatchString;
		Assert.False(MuiImageFontMatchStringStateFieldCursorCodec.TryGetAddress(
			ref platform, matchCursor, out _, out _));
		Assert.False(MuiImageFontMatchStringStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void ImageFontMatchStringSequentialRecordPreservesPointerAndRejectsTruncation()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			State);
		var address = APTR.FromPointer(0x3600);
		var value = new MuiImageFontMatchStringStateRecord
		{
			Magic = MuiImageFontMatchStringStateRecord.Cookie,
			Present = 1,
			MatchString = APTR.FromPointer(0x2A00),
		};
		Assert.True(MuiImageFontMatchStringStateRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiImageFontMatchStringStateRecordCodec.TryReadRecord(
			ref platform, address, out var actual));
		Assert.Equal(value.Magic, actual.Magic);
		Assert.Equal(value.Present, actual.Present);
		Assert.Equal(value.MatchString, actual.MatchString);
		Assert.False(MuiImageFontMatchStringStateRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x30FFC), out _));
	}

	[Fact]
	public void MalformedImageFontMatchStringFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var imageClass, out var matchString);
		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, BuildTags(ref platform, 0x1900, matchString));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			image, ImageFontMatchString, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, image,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiImageFontMatchStringStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiImageFontMatchStringStateField.Present, 2));
		Assert.True(MuiImageFontMatchStringStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(2u, structural.Present);
		Assert.False(MuiImageFontMatchStringStateAdmission.Validate(structural));
		Assert.False(MuiImageFontMatchStringStateRecordCodec.TryRead(ref platform,
			block, out _));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetImageFontMatchStringStateRecord(
			ref platform, State, image, out _));
		Assert.False(MuiCommonControlCore.TryReadImageFontMatchStringState(
			ref platform, State, image, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, image,
			ImageFontMatchString, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			image, ImageFontMatchString, APTR.FromPointer(0x1C00).Raw));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, image,
			StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			image, ImageFontMatchString, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiImageFontMatchStringStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiImageFontMatchStringStateField.Present,
			out var preserved));
		Assert.Equal(2u, preserved);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR imageClass,
		out APTR matchString)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		matchString = APTR.FromPointer(0x1800);
		platform.WriteCString(name, "Image.mui");
		platform.WriteCString(matchString, "topaz/8");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		imageClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform, uint address,
		APTR matchString)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, ImageFontMatchString);
		platform.WriteUInt32(tags, 4, matchString.Raw);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		return tags;
	}
}
