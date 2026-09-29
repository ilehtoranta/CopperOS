using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringPlaceholderAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint StringPlaceholder = 0x8042AE65;
	private const uint StateKey = 0x7F070019;

	[Fact]
	public void StringPlaceholderAdmissionRequiresCanonicalMagicMappedStringAndOwner()
	{
		var platform = CreatePlatform(out var stringClass, out var source);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, source));
		Assert.True(MuiCommonControlCore.TryGetStringPlaceholderStateRecord(
			ref platform, State, stringObj, out var valid));
		Assert.True(MuiStringPlaceholderStateAdmission.Validate(valid));
		Assert.True(MuiStringPlaceholderStateAdmission.ValidateLive(ref platform,
			State, stringObj, valid));

		var malformed = valid;
		malformed.Contents = APTR.FromPointer(0x30000);
		Assert.True(MuiStringPlaceholderStateAdmission.Validate(malformed));
		Assert.False(MuiStringPlaceholderStateAdmission.ValidateLive(ref platform,
			State, stringObj, malformed));
		malformed = valid;
		malformed.Magic = 0;
		Assert.False(MuiStringPlaceholderStateAdmission.Validate(malformed));
		Assert.False(MuiStringPlaceholderStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedStringPlaceholderFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var stringClass, out var source);
		var stringObj = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, BuildTags(ref platform, 0x1900, source));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringPlaceholder, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, stringObj,
			StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiStringPlaceholderStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiStringPlaceholderStateField.Contents,
			0x30000));
		Assert.True(MuiStringPlaceholderStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0x30000u, structural.Contents.Raw);
		Assert.True(MuiStringPlaceholderStateAdmission.Validate(structural));
		Assert.False(MuiStringPlaceholderStateAdmission.ValidateLive(ref platform,
			State, stringObj, structural));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetStringPlaceholderStateRecord(
			ref platform, State, stringObj, out _));
		Assert.False(MuiCommonControlCore.TryReadStringPlaceholderState(ref platform,
			State, stringObj, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, stringObj,
			StringPlaceholder, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			stringObj, StringPlaceholder, 0));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			stringObj, StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			stringObj, StringPlaceholder, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiStringPlaceholderStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiStringPlaceholderStateField.Contents,
			out var preserved));
		Assert.Equal(0x30000u, preserved);
	}

	[Fact]
	public void StringPlaceholderRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _, out var source);
		var address = APTR.FromPointer(0x1D80);
		var record = new MuiStringPlaceholderStateRecord
		{
			Magic = MuiStringPlaceholderStateRecord.Cookie,
			Contents = source,
		};
		Assert.True(MuiStringPlaceholderStateRecordCodec.Write(ref platform, address,
			record));
		Assert.True(MuiStringPlaceholderStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiStringPlaceholderStateField.Contents,
			out var typedContentsAddress));
		Assert.Equal(0x1D84u, typedContentsAddress.Raw);
		var contentsCursor = new MuiStringPlaceholderStateFieldCursor
		{
			Record = address,
			Field = MuiStringPlaceholderStateField.Contents,
		};
		Assert.True(MuiStringPlaceholderStateFieldCursorCodec.TryGetAddress(ref platform,
			contentsCursor, out var cursorContentsAddress, out var cursorFieldSize));
		Assert.Equal(typedContentsAddress, cursorContentsAddress);
		Assert.Equal(MuiStringPlaceholderStateRecord.FieldSize, cursorFieldSize);
		Assert.True(MuiStringPlaceholderStateRecordMemoryCodec.TryGetAddress(ref platform,
			contentsCursor, out var memoryContentsAddress, out var memoryFieldSize));
		Assert.Equal(cursorContentsAddress, memoryContentsAddress);
		Assert.Equal(cursorFieldSize, memoryFieldSize);
		Assert.True(MuiStringPlaceholderStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiStringPlaceholderStateField.Contents,
			out var typedContents));
		Assert.Equal(source.Raw, typedContents);
		Assert.True(MuiStringPlaceholderStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 4, out var contentsAddress));
		Assert.Equal(0x1D84u, contentsAddress.Raw);
		Assert.True(MuiStringPlaceholderStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out var contents));
		Assert.Equal(source.Raw, contents);
		Assert.True(MuiStringPlaceholderStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiStringPlaceholderStateField.Contents, 0));
		Assert.True(MuiStringPlaceholderStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiStringPlaceholderStateField.Magic,
			out var typedMagic));
		Assert.Equal(MuiStringPlaceholderStateRecord.Cookie, typedMagic);
		Assert.False(MuiStringPlaceholderStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiStringPlaceholderStateField)255, out _));
		Assert.True(MuiStringPlaceholderStateRecordCodec.TryReadStructural(ref platform,
			address, out var typedUpdated));
		Assert.Equal(MuiStringPlaceholderStateRecord.Cookie, typedUpdated.Magic);
		Assert.True(typedUpdated.Contents.IsNull);
		Assert.True(MuiStringPlaceholderStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 4, 0));
		Assert.True(MuiStringPlaceholderStateRecordCodec.TryReadStructural(ref platform,
			address, out var updated));
		Assert.Equal(APTR.Null, updated.Contents);
		Assert.False(MuiStringPlaceholderStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiStringPlaceholderStateRecord.Size, out _));
		Assert.False(MuiStringPlaceholderStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, (uint)0, out _));
		Assert.False(MuiStringPlaceholderStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
		contentsCursor.Field = (MuiStringPlaceholderStateField)255;
		Assert.False(MuiStringPlaceholderStateFieldCursorCodec.TryGetAddress(ref platform,
			contentsCursor, out _, out _));
		contentsCursor.Record = APTR.Null;
		contentsCursor.Field = MuiStringPlaceholderStateField.Contents;
		Assert.False(MuiStringPlaceholderStateFieldCursorCodec.TryGetAddress(ref platform,
			contentsCursor, out _, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR stringClass,
		out APTR source)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		source = APTR.FromPointer(0x1800);
		platform.WriteCString(name, "String.mui");
		platform.WriteCString(source, "hint");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		stringClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform,
		uint address, APTR source)
	{
		var tags = APTR.FromPointer(address);
		platform.WriteUInt32(tags, 0, StringPlaceholder);
		platform.WriteUInt32(tags, 4, source.Raw);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		return tags;
	}
}
