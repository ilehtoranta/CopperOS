using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiServiceRecordCursorTests
{
	[Fact]
	public void ErrorServiceFieldsUseNamedRecordBoundary()
	{
		var platform = CreatePlatform();
		var record = APTR.FromPointer(0x3000);
		var cursor = new MuiErrorServiceStateFieldCursor
		{
			Record = record,
			Field = MuiErrorServiceStateField.Error,
		};
		Assert.True(MuiErrorServiceStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x3008), address);
		Assert.True(MuiErrorServiceStateFieldCursorCodec.TryWriteUInt32(
			ref platform, record, MuiErrorServiceStateField.Error, 9));
		Assert.True(MuiErrorServiceStateFieldCursorCodec.TryReadUInt32(
			ref platform, record, MuiErrorServiceStateField.Error, out var error));
		Assert.Equal(9u, error);
		Assert.False(MuiErrorServiceStateFieldCursorCodec.TryReadUInt32(
			ref platform, record, unchecked((MuiErrorServiceStateField)255), out _));
	}

	[Fact]
	public void ErrorServiceMemoryAdapterOwnsStructBounds()
	{
		var platform = CreatePlatform();
		var record = APTR.FromPointer(0x3000);
		Assert.True(MuiErrorServiceStateMemoryCodec.TryGetAddress(ref platform,
			record, MuiErrorServiceStateField.Sequence, out var address));
		Assert.Equal(record.Raw + MuiErrorServiceStateRecord.SequenceOffset,
			address.Raw);
		Assert.True(MuiErrorServiceStateMemoryCodec.TryWriteUInt32(ref platform,
			record, MuiErrorServiceStateField.Error, 0xFFFFFFFEu));
		Assert.True(MuiErrorServiceStateMemoryCodec.TryReadUInt32(ref platform,
			record, MuiErrorServiceStateField.Error, out var error));
		Assert.Equal(0xFFFFFFFEu, error);
		Assert.False(MuiErrorServiceStateMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x40FF9), MuiErrorServiceStateField.Sequence, out _));
		Assert.False(MuiErrorServiceStateMemoryCodec.TryGetAddress(ref platform,
			record, (MuiErrorServiceStateField)255, out _));
		Assert.False(MuiErrorServiceStateMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiErrorServiceStateField.Error, out _));
	}

	[Fact]
	public void ErrorServiceFieldAccessUsesCompleteNamedStateRecord()
	{
		var platform = CreatePlatform();
		var record = APTR.FromPointer(0x3400);
		var original = new MuiErrorServiceStateRecord
		{
			Magic = MuiErrorServiceLayout.Magic,
			Version = MuiErrorServiceLayout.Version,
			Error = 7,
			Sequence = 11,
		};
		Assert.True(MuiErrorServiceStateStructCodec.Write(ref platform, record,
			original));
		Assert.True(MuiErrorServiceStateMemoryCodec.TryWriteUInt32(ref platform,
			record, MuiErrorServiceStateField.Error, 0xFFFFFFFEu));
		Assert.True(MuiErrorServiceStateMemoryCodec.TryWriteUInt32(ref platform,
			record, MuiErrorServiceStateField.Sequence, 23));
		Assert.True(MuiErrorServiceStateStructCodec.TryRead(ref platform, record,
			out var afterFields));
		Assert.Equal(original.Magic, afterFields.Magic);
		Assert.Equal(original.Version, afterFields.Version);
		Assert.Equal(0xFFFFFFFEu, afterFields.Error);
		Assert.Equal(23u, afterFields.Sequence);

		Assert.False(MuiErrorServiceStateMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x40FF1), MuiErrorServiceStateField.Error, out _));
		Assert.False(MuiErrorServiceStateMemoryCodec.TryWriteUInt32(ref platform,
			APTR.FromPointer(0x40FF1), MuiErrorServiceStateField.Sequence, 1));
		Assert.False(MuiErrorServiceStateMemoryCodec.TryReadUInt32(ref platform,
			record, (MuiErrorServiceStateField)255, out _));
	}

	[Fact]
	public void GroupPageFieldsUseNamedRecordBoundary()
	{
		var platform = CreatePlatform();
		var record = APTR.FromPointer(0x3100);
		Assert.True(MuiGroupPageStateMemoryCodec.TryGetAddress(ref platform,
			record, MuiGroupPageStateField.LastSelector, out var address));
		Assert.Equal(APTR.FromPointer(0x310C), address);
		Assert.True(MuiGroupPageStateMemoryCodec.TryWriteUInt32(ref platform,
			record, MuiGroupPageStateField.Active, 2));
		Assert.True(MuiGroupPageStateMemoryCodec.TryReadUInt32(ref platform,
			record, MuiGroupPageStateField.Active, out var active));
		Assert.Equal(2u, active);
		Assert.False(MuiGroupPageStateMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0xFFFFFFF0u), MuiGroupPageStateField.Changes, out _));
	}

	[Fact]
	public void StringInteger64FieldsUseNamedQuadBoundary()
	{
		var platform = CreatePlatform();
		var record = APTR.FromPointer(0x3200);
		var cursor = new MuiStringInteger64FieldCursor
		{
			Record = record,
			Field = MuiStringInteger64Field.Low,
		};
		Assert.True(MuiStringInteger64FieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x3204), address);
		Assert.True(MuiStringInteger64FieldCursorCodec.TryWriteUInt32(ref platform,
			record, MuiStringInteger64Field.High, 0x7FFFFFFFu));
		Assert.True(MuiStringInteger64FieldCursorCodec.TryReadUInt32(ref platform,
			record, MuiStringInteger64Field.High, out var high));
		Assert.Equal(0x7FFFFFFFu, high);
		Assert.False(MuiStringInteger64FieldCursorCodec.TryReadUInt32(ref platform,
			record, unchecked((MuiStringInteger64Field)255), out _));
	}

	[Fact]
	public void StringInteger64MemoryAdapterOwnsQuadRecordBounds()
	{
		var platform = CreatePlatform();
		var record = APTR.FromPointer(0x3200);
		Assert.True(MuiStringInteger64ValueMemoryCodec.TryGetAddress(ref platform,
			record, MuiStringInteger64Field.Low, out var lowField));
		Assert.Equal(APTR.FromPointer(0x3204), lowField);
		Assert.True(MuiStringInteger64ValueMemoryCodec.TryWriteUInt32(ref platform,
			record, MuiStringInteger64Field.High, 0x80000000u));
		Assert.True(MuiStringInteger64ValueMemoryCodec.TryReadUInt32(ref platform,
			record, MuiStringInteger64Field.High, out var high));
		Assert.Equal(0x80000000u, high);
		Assert.False(MuiStringInteger64ValueMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x40FFC), MuiStringInteger64Field.Low, out _));
		Assert.False(MuiStringInteger64ValueMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiStringInteger64Field.High, out _));
		Assert.False(MuiStringInteger64ValueMemoryCodec.TryGetAddress(ref platform,
			record, unchecked((MuiStringInteger64Field)255), out _));
	}

	[Fact]
	public void StringInteger64FieldPathPreservesNamedQuadRecord()
	{
		var platform = CreatePlatform();
		var record = APTR.FromPointer(0x3280);
		var initial = new MuiStringInteger64Value
		{
			High = 0x10203040u,
			Low = 0x50607080u,
		};

		Assert.True(MuiStringInteger64ValueStructCodec.Write(ref platform, record,
			initial));
		Assert.True(MuiStringInteger64ValueMemoryCodec.TryWriteUInt32(ref platform,
			record, MuiStringInteger64Field.High, 0xF1020304u));
		Assert.True(MuiStringInteger64ValueStructCodec.TryRead(ref platform, record,
			out var updated));
		Assert.Equal(0xF1020304u, updated.High);
		Assert.Equal(initial.Low, updated.Low);
		Assert.True(MuiStringInteger64ValueMemoryCodec.TryReadUInt32(ref platform,
			record, MuiStringInteger64Field.Low, out var low));
		Assert.Equal(initial.Low, low);
		Assert.False(MuiStringInteger64ValueMemoryCodec.TryWriteUInt32(ref platform,
			record, unchecked((MuiStringInteger64Field)255), 1));
		Assert.False(MuiStringInteger64ValueMemoryCodec.TryWriteUInt32(ref platform,
			APTR.FromPointer(0x40FFC), MuiStringInteger64Field.High, 1));
	}

	[Fact]
	public void StringEditWorkMemoryAdapterOwnsMixedFieldRecordBounds()
	{
		var platform = CreatePlatform();
		var record = APTR.FromPointer(0x3400);
		Assert.True(MuiStringEditWorkRecordMemoryCodec.TryGetAddress(ref platform,
			record, MuiStringEditRecordField.Actions, out var actionsField,
			out var actionsSize));
		Assert.Equal(APTR.FromPointer(0x341E), actionsField);
		Assert.Equal(MuiStringEditWorkRecord.LongFieldSize, actionsSize);
		Assert.True(MuiStringEditWorkRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, MuiStringEditRecordField.Actions, 0xAABBCCDD));
		Assert.True(MuiStringEditWorkRecordMemoryCodec.TryReadUInt32(ref platform,
			record, MuiStringEditRecordField.Actions, out var actions));
		Assert.Equal(0xAABBCCDDu, actions);
		Assert.True(MuiStringEditWorkRecordMemoryCodec.TryWriteUInt16(ref platform,
			record, MuiStringEditRecordField.EditOp, 7));
		Assert.True(MuiStringEditWorkRecordMemoryCodec.TryReadUInt16(ref platform,
			record, MuiStringEditRecordField.EditOp, out var editOp));
		Assert.Equal((ushort)7, editOp);
		Assert.False(MuiStringEditWorkRecordMemoryCodec.TryReadUInt16(ref platform,
			record, MuiStringEditRecordField.Actions, out _));
		Assert.False(MuiStringEditWorkRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x40FD5), MuiStringEditRecordField.Gadget, out _,
			out _));
		Assert.False(MuiStringEditWorkRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiStringEditRecordField.EditOp, out _, out _));
		Assert.False(MuiStringEditWorkRecordMemoryCodec.TryGetAddress(ref platform,
			record, (MuiStringEditRecordField)255, out _, out _));
	}

	[Fact]
	public void StringEditWorkCodecUsesCompleteNamedRecord()
	{
		var platform = CreatePlatform();
		var record = APTR.FromPointer(0x3600);
		var expected = new MuiStringEditWorkRecord
		{
			Gadget = APTR.FromPointer(0x11111111),
			StringInfo = APTR.FromPointer(0x22222222),
			WorkBuffer = APTR.FromPointer(0x33333333),
			PrevBuffer = APTR.FromPointer(0x44444444),
			Modes = 0x55555555,
			InputEvent = APTR.FromPointer(0x66666666),
			Code = 0x1234,
			BufferPos = -12,
			NumChars = 34,
			Actions = 0x77777777,
			LongInt = -45,
			GadgetInfo = APTR.FromPointer(0x88888888),
			EditOp = 0x4321,
		};

		Assert.Equal(44, Unsafe.SizeOf<MuiStringEditWorkRecord>());
		Assert.True(MuiStringEditWorkCodec.Write(ref platform, record, expected));
		Assert.True(MuiStringEditWorkCodec.TryRead(ref platform, record,
			out var actual));
		Assert.Equal(expected.Gadget, actual.Gadget);
		Assert.Equal(expected.StringInfo, actual.StringInfo);
		Assert.Equal(expected.WorkBuffer, actual.WorkBuffer);
		Assert.Equal(expected.PrevBuffer, actual.PrevBuffer);
		Assert.Equal(expected.Modes, actual.Modes);
		Assert.Equal(expected.InputEvent, actual.InputEvent);
		Assert.Equal(expected.Code, actual.Code);
		Assert.Equal(expected.BufferPos, actual.BufferPos);
		Assert.Equal(expected.NumChars, actual.NumChars);
		Assert.Equal(expected.Actions, actual.Actions);
		Assert.Equal(expected.LongInt, actual.LongInt);
		Assert.Equal(expected.GadgetInfo, actual.GadgetInfo);
		Assert.Equal(expected.EditOp, actual.EditOp);
		Assert.False(MuiStringEditWorkCodec.TryRead(ref platform,
			APTR.FromPointer(0x40FD5), out _));
		Assert.False(MuiStringEditWorkCodec.Write(ref platform,
			APTR.FromPointer(0x40FD5), expected));
	}

	[Fact]
	public void RequesterServiceFieldsUseNamedRecordBoundary()
	{
		var platform = CreatePlatform();
		var record = APTR.FromPointer(0x3300);
		var cursor = new MuiRequesterServiceStateFieldCursor
		{
			Record = record,
			Field = MuiRequesterServiceStateField.Generation,
		};
		Assert.True(MuiRequesterServiceStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x3304), address);
		Assert.True(MuiRequesterServiceStateFieldCursorCodec.TryWriteUInt32(
			ref platform, record, MuiRequesterServiceStateField.Generation, 3));
		Assert.True(MuiRequesterServiceStateFieldCursorCodec.TryReadUInt32(
			ref platform, record, MuiRequesterServiceStateField.Generation,
			out var generation));
		Assert.Equal(3u, generation);
		Assert.False(MuiRequesterServiceStateFieldCursorCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0xFFFFFFF0u),
			MuiRequesterServiceStateField.Magic, out _));
	}

	[Fact]
	public void RequesterServiceFieldAccessUsesCompleteNamedStateRecord()
	{
		var platform = CreatePlatform();
		var record = APTR.FromPointer(0x3500);
		var original = new MuiRequesterServiceStateRecord
		{
			Magic = MuiRequesterServiceLayout.Magic,
			Generation = 7,
		};
		Assert.True(MuiRequesterServiceStateStructCodec.Write(ref platform,
			record, original));
		Assert.True(MuiRequesterServiceStateMemoryCodec.TryWriteUInt32(ref platform,
			record, MuiRequesterServiceStateField.Generation, 11));
		Assert.True(MuiRequesterServiceStateMemoryCodec.TryReadUInt32(ref platform,
			record, MuiRequesterServiceStateField.Magic, out var magic));
		Assert.Equal(original.Magic, magic);
		Assert.True(MuiRequesterServiceStateStructCodec.TryRead(ref platform,
			record, out var after));
		Assert.Equal(original.Magic, after.Magic);
		Assert.Equal(11u, after.Generation);

		Assert.False(MuiRequesterServiceStateMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x40FFC), MuiRequesterServiceStateField.Magic, out _));
		Assert.False(MuiRequesterServiceStateMemoryCodec.TryWriteUInt32(ref platform,
			APTR.FromPointer(0x40FFC), MuiRequesterServiceStateField.Generation, 1));
		Assert.False(MuiRequesterServiceStateMemoryCodec.TryReadUInt32(ref platform,
			record, (MuiRequesterServiceStateField)255, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform() =>
		new(0x1000, 0x40000, 0x4000, APTR.FromPointer(0x1000));
}
