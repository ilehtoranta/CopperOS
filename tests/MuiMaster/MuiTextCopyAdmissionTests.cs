using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiTextCopyAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint TextCopy = 0x80427727;
	private const uint StateKey = 0x7F07003F;

	[Fact]
	public void TextCopyAdmissionRequiresCanonicalBooleanAndOwner()
	{
		var platform = CreatePlatform(out var textClass);
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, APTR.Null);
		var valid = new MuiTextCopyStateRecord
		{
			Magic = MuiTextCopyStateRecord.Cookie,
			Copy = 1,
		};
		Assert.True(MuiTextCopyStateAdmission.Validate(valid));
		Assert.True(MuiTextCopyStateAdmission.ValidateLive(ref platform, State,
			text, valid));
		var malformed = valid;
		malformed.Copy = 2;
		Assert.False(MuiTextCopyStateAdmission.Validate(malformed));
		Assert.False(MuiTextCopyStateAdmission.ValidateLive(ref platform, State,
			text, malformed));
		malformed = valid;
		malformed.Magic = 0;
		Assert.False(MuiTextCopyStateAdmission.Validate(malformed));
		Assert.False(MuiTextCopyStateAdmission.ValidateLive(ref platform, State,
			APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedTextCopyFailsClosedBeforeRawRepairOrSet()
	{
		var platform = CreatePlatform(out var textClass);
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, APTR.Null);
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			text, TextCopy, out var rawBefore));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, text, StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiTextCopyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			block, MuiTextCopyStateField.Copy, 2));
		Assert.True(MuiTextCopyStateRecordCodec.TryReadStructural(ref platform,
			block, out var structural));
		Assert.Equal(2u, structural.Copy);
		Assert.False(MuiTextCopyStateAdmission.Validate(structural));
		Assert.False(MuiTextCopyStateRecordCodec.TryRead(ref platform, block,
			out _));

		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiCommonControlCore.TryGetTextCopyStateRecord(ref platform,
			State, text, out _));
		Assert.False(MuiCommonControlCore.TryReadTextCopyState(ref platform, State,
			text, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, text,
			TextCopy, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			text, TextCopy, 0));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, text,
			StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			text, TextCopy, out var rawAfter));
		Assert.Equal(rawBefore, rawAfter);
		Assert.True(MuiTextCopyStateFieldCursorCodec.TryReadUInt32(ref platform,
			block, MuiTextCopyStateField.Copy, out var preserved));
		Assert.Equal(2u, preserved);
	}

	[Fact]
	public void TextCopyRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1D20);
		var record = new MuiTextCopyStateRecord
		{
			Magic = MuiTextCopyStateRecord.Cookie,
			Copy = 1,
		};
		Assert.True(MuiTextCopyStateRecordCodec.Write(ref platform, address, record));
		var fieldCursor = new MuiTextCopyStateFieldCursor
		{
			Record = address,
			Field = MuiTextCopyStateField.Copy,
		};
		Assert.True(MuiTextCopyStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out var cursorCopyAddress));
		Assert.Equal(0x1D24u, cursorCopyAddress.Raw);
		Assert.True(MuiTextCopyStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out var typedCursorCopyAddress, out var typedCursorFieldSize));
		Assert.Equal(cursorCopyAddress, typedCursorCopyAddress);
		Assert.Equal(MuiTextCopyStateRecord.FieldSize, typedCursorFieldSize);
		Assert.True(MuiTextCopyStateRecordMemoryCodec.TryGetAddress(ref platform,
			fieldCursor, out var memoryCursorCopyAddress, out var memoryCursorFieldSize));
		Assert.Equal(typedCursorCopyAddress, memoryCursorCopyAddress);
		Assert.Equal(typedCursorFieldSize, memoryCursorFieldSize);
		Assert.True(MuiTextCopyStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiTextCopyStateField.Copy, out var typedCopyAddress));
		Assert.Equal(0x1D24u, typedCopyAddress.Raw);
		Assert.True(MuiTextCopyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiTextCopyStateField.Copy, out var typedCopy));
		Assert.Equal(1u, typedCopy);
		Assert.True(MuiTextCopyStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, 4, out var copyAddress));
		Assert.Equal(0x1D24u, copyAddress.Raw);
		Assert.True(MuiTextCopyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out var copy));
		Assert.Equal(1u, copy);
		Assert.True(MuiTextCopyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiTextCopyStateField.Copy, 0));
		Assert.True(MuiTextCopyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiTextCopyStateField.Magic, out var typedMagic));
		Assert.Equal(MuiTextCopyStateRecord.Cookie, typedMagic);
		Assert.False(MuiTextCopyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, (MuiTextCopyStateField)255, out _));
		fieldCursor.Field = (MuiTextCopyStateField)255;
		Assert.False(MuiTextCopyStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out _, out _));
		fieldCursor.Record = APTR.Null;
		fieldCursor.Field = MuiTextCopyStateField.Copy;
		Assert.False(MuiTextCopyStateFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out _, out _));
		Assert.True(MuiTextCopyStateRecordCodec.TryReadStructural(ref platform,
			address, out var typedUpdated));
		Assert.Equal(MuiTextCopyStateRecord.Cookie, typedUpdated.Magic);
		Assert.Equal(0u, typedUpdated.Copy);
		Assert.True(MuiTextCopyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 4, 0));
		Assert.True(MuiTextCopyStateRecordCodec.TryReadStructural(ref platform,
			address, out var updated));
		Assert.Equal(0u, updated.Copy);
		Assert.False(MuiTextCopyStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiTextCopyStateRecord.Size, out _));
		Assert.False(MuiTextCopyStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, (uint)0, out _));
		Assert.False(MuiTextCopyStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR textClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Text.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		textClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
