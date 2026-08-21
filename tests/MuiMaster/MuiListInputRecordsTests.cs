using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListInputRecordsTests
{
	[Fact]
	public void MixedWidthCursorUsesNamedRecordBoundaries()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3000);

		var cursor = default(MuiListInputRecordFieldCursor);
		cursor.Address = address;
		cursor.Record = MuiListInputRecordKind.TestPos;
		cursor.Field = MuiListInputRecordField.XOffset;
		Assert.True(MuiListInputRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(APTR.FromPointer(0x3008), fieldAddress);
		Assert.Equal(2u, fieldSize);
		Assert.True(MuiListInputRecordFieldCursorCodec.TryWriteUInt16(ref platform,
			address, MuiListInputRecordKind.TestPos,
			MuiListInputRecordField.XOffset, unchecked((ushort)-9)));
		Assert.True(MuiListInputRecordFieldCursorCodec.TryReadUInt16(ref platform,
			address, MuiListInputRecordKind.TestPos,
			MuiListInputRecordField.XOffset, out var xOffset));
		Assert.Equal(-9, unchecked((short)xOffset));

		cursor.Record = MuiListInputRecordKind.IntuiMessage;
		cursor.Field = MuiListInputRecordField.MouseY;
		Assert.True(MuiListInputRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out fieldAddress, out fieldSize));
		Assert.Equal(APTR.FromPointer(0x3022), fieldAddress);
		Assert.Equal(2u, fieldSize);
		Assert.True(MuiListInputRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiListInputRecordKind.IntuiMessage,
			MuiListInputRecordField.IAddress, 0x12345678u));
		Assert.True(MuiListInputRecordFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiListInputRecordKind.IntuiMessage,
			MuiListInputRecordField.IAddress, out var iAddress));
		Assert.Equal(0x12345678u, iAddress);

		cursor.Record = MuiListInputRecordKind.DragState;
		cursor.Field = MuiListInputRecordField.Flags;
		Assert.True(MuiListInputRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out fieldAddress, out fieldSize));
		Assert.Equal(APTR.FromPointer(0x301C), fieldAddress);
		Assert.Equal(4u, fieldSize);

		Assert.False(MuiListInputRecordFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiListInputRecordKind.TestPos,
			MuiListInputRecordField.Column, out _));
		Assert.False(MuiListInputRecordFieldCursorCodec.TryReadUInt16(ref platform,
			address, MuiListInputRecordKind.Scalar,
			MuiListInputRecordField.XOffset, out _));
		Assert.False(MuiListInputRecordFieldCursorCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0xFFFFFFF0u), MuiListInputRecordKind.DragState,
			MuiListInputRecordField.Flags, out _));
	}

	[Fact]
	public void InputRecordCodecsRoundTripSignedAndEnvelopeFields()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			APTR.FromPointer(0x1000));
		var testPosAddress = APTR.FromPointer(0x3100);
		var testPos = new MuiListTestPosResult
		{
			Entry = -7,
			Column = -2,
			Flags = 0x55AA,
			XOffset = -9,
			YOffset = 12,
		};
		Assert.True(MuiListTestPosResultCodec.Write(ref platform, testPosAddress,
			testPos));
		Assert.True(MuiListTestPosResultCodec.TryRead(ref platform, testPosAddress,
			out var actualTestPos));
		Assert.Equal(testPos.Entry, actualTestPos.Entry);
		Assert.Equal(testPos.Column, actualTestPos.Column);
		Assert.Equal(testPos.Flags, actualTestPos.Flags);
		Assert.Equal(testPos.XOffset, actualTestPos.XOffset);
		Assert.Equal(testPos.YOffset, actualTestPos.YOffset);

		var messageAddress = APTR.FromPointer(0x3200);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, messageAddress,
			0x12345678u, 0x3456, 0x789A, 0x10203040u, -11, 22));
		Assert.True(MuiIntuiMessageCodec.TryReadPointer(ref platform,
			messageAddress, out var message));
		Assert.Equal(0x12345678u, message.Class);
		Assert.Equal((ushort)0x3456, message.Code);
		Assert.Equal((ushort)0x789A, message.Qualifier);
		Assert.Equal(0x10203040u, message.IAddress);
		Assert.Equal((short)-11, message.MouseX);
		Assert.Equal((short)22, message.MouseY);

		var dragAddress = APTR.FromPointer(0x3300);
		var drag = new MuiListviewDragState
		{
			Magic = MuiListviewDragStateCodec.Cookie,
			Source = -3,
			Target = 8,
			StartX = -12,
			StartY = 20,
			LastX = 40,
			LastY = -5,
			Flags = MuiListviewDragState.ActiveFlag |
				MuiListviewDragState.MovedFlag,
		};
		MuiListviewDragStateCodec.Write(ref platform, dragAddress, drag);
		Assert.True(MuiListviewDragStateCodec.TryRead(ref platform, dragAddress,
			out var actualDrag));
		Assert.Equal(drag.Magic, actualDrag.Magic);
		Assert.Equal(drag.Source, actualDrag.Source);
		Assert.Equal(drag.Target, actualDrag.Target);
		Assert.Equal(drag.StartX, actualDrag.StartX);
		Assert.Equal(drag.StartY, actualDrag.StartY);
		Assert.Equal(drag.LastX, actualDrag.LastX);
		Assert.Equal(drag.LastY, actualDrag.LastY);
		Assert.Equal(drag.Flags, actualDrag.Flags);
	}
}
