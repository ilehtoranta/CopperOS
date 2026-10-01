using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiTextContentsAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint TextContents = 0x8042F8DC;
	private const uint StateKey = 0x7F07001C;

	[Fact]
	public void TextContentsAdmissionRequiresCanonicalMagicMappedContentsAndOwner()
	{
		var platform = CreatePlatform(out var textClass, out var source);
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, BuildTags(ref platform, 0x1900, source));
		Assert.True(MuiCommonControlCore.TryGetTextContentsStateRecord(
			ref platform, State, text, out var valid));
		Assert.True(MuiTextContentsStateAdmission.Validate(valid));
		Assert.True(MuiTextContentsStateAdmission.ValidateLive(ref platform, State,
			text, valid));

		var malformed = valid;
		malformed.Contents = APTR.FromPointer(0x30000);
		Assert.True(MuiTextContentsStateAdmission.Validate(malformed));
		Assert.False(MuiTextContentsStateAdmission.ValidateLive(ref platform,
			State, text, malformed));
		malformed = valid;
		malformed.Magic = 0;
		Assert.False(MuiTextContentsStateAdmission.Validate(malformed));
		Assert.False(MuiTextContentsStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedTextContentsFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var textClass, out var source);
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, BuildTags(ref platform, 0x1900, source));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			text, TextContents, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, text, StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiTextContentsStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiTextContentsStateField.Contents, 0x30000));
		Assert.True(MuiTextContentsStateRecordCodec.TryReadStructural(ref platform,
			block, out var structural));
		Assert.Equal(0x30000u, structural.Contents.Raw);
		Assert.True(MuiTextContentsStateAdmission.Validate(structural));
		Assert.False(MuiTextContentsStateAdmission.ValidateLive(ref platform, State,
			text, structural));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetTextContentsStateRecord(ref platform,
			State, text, out _));
		Assert.False(MuiCommonControlCore.TryReadTextContentsState(ref platform,
			State, text, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, text,
			TextContents, out _, out _));
		var replacement = APTR.FromPointer(0x1C00);
		platform.WriteCString(replacement, "replacement");
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			text, TextContents, replacement.Raw));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, text,
			StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, text,
			TextContents, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiTextContentsStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiTextContentsStateField.Contents,
			out var preserved));
		Assert.Equal(0x30000u, preserved);
	}

	[Fact]
	public void TextContentsRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _, out var source);
		var address = APTR.FromPointer(0x1D00);
		var record = new MuiTextContentsStateRecord
		{
			Magic = MuiTextContentsStateRecord.Cookie,
			Contents = source,
		};
		Assert.True(MuiTextContentsStateRecordCodec.Write(ref platform, address,
			record));
		var fieldCursor = new MuiTextContentsStateFieldCursor
		{
			Record = address,
			Field = MuiTextContentsStateField.Contents,
		};
		Assert.True(MuiTextContentsStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out var cursorContentsAddress));
		Assert.Equal(0x1D04u, cursorContentsAddress.Raw);
		Assert.True(MuiTextContentsStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out var typedCursorContentsAddress, out var typedCursorFieldSize));
		Assert.Equal(cursorContentsAddress, typedCursorContentsAddress);
		Assert.Equal(MuiTextContentsStateRecord.FieldSize, typedCursorFieldSize);
		Assert.True(MuiTextContentsStateRecordMemoryCodec.TryGetAddress(ref platform,
			fieldCursor, out var memoryCursorContentsAddress, out var memoryCursorFieldSize));
		Assert.Equal(typedCursorContentsAddress, memoryCursorContentsAddress);
		Assert.Equal(typedCursorFieldSize, memoryCursorFieldSize);
		Assert.True(MuiTextContentsStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiTextContentsStateField.Contents,
			out var typedContentsAddress));
		Assert.Equal(0x1D04u, typedContentsAddress.Raw);
		Assert.True(MuiTextContentsStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiTextContentsStateField.Contents,
			out var typedContents));
		Assert.Equal(source.Raw, typedContents);
		Assert.True(MuiTextContentsStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 4, out var contentsAddress));
		Assert.Equal(0x1D04u, contentsAddress.Raw);
		Assert.True(MuiTextContentsStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out var contents));
		Assert.Equal(source.Raw, contents);
		Assert.True(MuiTextContentsStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiTextContentsStateField.Contents, 0));
		Assert.True(MuiTextContentsStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiTextContentsStateField.Magic,
			out var typedMagic));
		Assert.Equal(MuiTextContentsStateRecord.Cookie, typedMagic);
		Assert.False(MuiTextContentsStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiTextContentsStateField)255, out _));
		fieldCursor.Field = (MuiTextContentsStateField)255;
		Assert.False(MuiTextContentsStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out _, out _));
		fieldCursor.Record = APTR.Null;
		fieldCursor.Field = MuiTextContentsStateField.Contents;
		Assert.False(MuiTextContentsStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out _, out _));
		Assert.True(MuiTextContentsStateRecordCodec.TryReadStructural(ref platform,
			address, out var typedUpdated));
		Assert.Equal(MuiTextContentsStateRecord.Cookie, typedUpdated.Magic);
		Assert.True(typedUpdated.Contents.IsNull);
		Assert.True(MuiTextContentsStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 4, 0));
		Assert.True(MuiTextContentsStateRecordCodec.TryReadStructural(ref platform,
			address, out var updated));
		Assert.Equal(APTR.Null, updated.Contents);
		Assert.False(MuiTextContentsStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiTextContentsStateRecord.Size, out _));
		Assert.False(MuiTextContentsStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, (uint)0, out _));
		Assert.False(MuiTextContentsStateRecordCodec.TryReadStructural(ref platform,
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
		platform.WriteCString(source, "initial text");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		textClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform, uint address,
		APTR source)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, TextContents);
		platform.WriteUInt32(tags, 4, source.Raw);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		return tags;
	}
}
