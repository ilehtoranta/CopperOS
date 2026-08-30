using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListInputRecordsTests
{
	[Fact]
	public void InputStructAdaptersAndDragCursorUseNamedBoundaries()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3000);

		Assert.True(MuiListTestPosResultMemoryCodec.TryGetAddress(ref platform,
			address, MuiListTestPosResultField.XOffset, out var fieldAddress));
		Assert.Equal(APTR.FromPointer(0x3008), fieldAddress);
		Assert.True(MuiListTestPosResultMemoryCodec.TryWriteUInt16(ref platform,
			address, MuiListTestPosResultField.XOffset, unchecked((ushort)-9)));
		Assert.True(MuiListTestPosResultMemoryCodec.TryReadUInt16(ref platform,
			address, MuiListTestPosResultField.XOffset, out var xOffset));
		Assert.Equal(-9, unchecked((short)xOffset));

		Assert.True(MuiIntuiPointerMessageMemoryCodec.TryGetAddress(ref platform,
			address, MuiIntuiPointerMessageField.MouseY, out fieldAddress,
			out var fieldSize));
		Assert.Equal(APTR.FromPointer(0x3022), fieldAddress);
		Assert.Equal(2u, fieldSize);
		Assert.True(MuiIntuiPointerMessageMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiIntuiPointerMessageField.IAddress, 0x12345678u));
		Assert.True(MuiIntuiPointerMessageMemoryCodec.TryReadUInt32(ref platform,
			address, MuiIntuiPointerMessageField.IAddress, out var iAddress));
		Assert.Equal(0x12345678u, iAddress);

		Assert.True(MuiListviewDragStateMemoryCodec.TryGetAddress(ref platform,
			address, MuiListviewDragStateField.Flags, out fieldAddress));
		Assert.Equal(APTR.FromPointer(0x301C), fieldAddress);
		Assert.True(MuiListviewDragStateMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiListviewDragStateField.Flags,
			MuiListviewDragState.ActiveFlag));
		Assert.True(MuiListviewDragStateMemoryCodec.TryReadUInt32(ref platform,
			address, MuiListviewDragStateField.Flags, out var dragFlags));
		Assert.Equal(MuiListviewDragState.ActiveFlag, dragFlags);

		Assert.False(MuiListTestPosResultMemoryCodec.TryReadUInt16(ref platform,
			address, MuiListTestPosResultField.Entry, out _));
		Assert.False(MuiListScalarStorageRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, out _));
		Assert.False(MuiListviewDragStateMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0xFFFFFFF0u), MuiListviewDragStateField.Flags, out _));
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
		Assert.Equal(1u, message.TimestampValid);

		Assert.True(MuiIntuiMessageCodec.WritePointerWithTime(ref platform,
			messageAddress, 0x12345678u, 0x3456, 0x789A, 0x10203040u, -11, 22,
			77, 880000));
		Assert.True(MuiIntuiMessageCodec.TryReadPointer(ref platform,
			messageAddress, out message));
		Assert.Equal(1u, message.TimestampValid);
		Assert.Equal(77u, message.Seconds);
		Assert.Equal(880000u, message.Micros);
		Assert.True(MuiIntuiPointerMessageMemoryCodec.TryGetAddress(ref platform,
			messageAddress, MuiIntuiPointerMessageField.Seconds,
			out var timestampAddress, out var timestampSize));
		Assert.Equal(APTR.FromPointer(0x3224), timestampAddress);
		Assert.Equal(4u, timestampSize);

		Assert.True(MuiIntuiRawKeyMessageMemoryCodec.TryGetAddress(ref platform,
			messageAddress, MuiIntuiRawKeyMessageField.Code,
			out var rawKeyFieldAddress, out var rawKeyFieldSize));
		Assert.Equal(APTR.FromPointer(0x3218), rawKeyFieldAddress);
		Assert.Equal(2u, rawKeyFieldSize);

		Assert.False(MuiIntuiMessageCodec.TryReadRawKey(ref platform,
			messageAddress, out _));
		Assert.True(MuiIntuiMessageCodec.WriteRawKey(ref platform, messageAddress,
			MuiIntuiMessageCodec.RawKeyClass, (ushort)'K', (ushort)0xA55A));
		Assert.True(MuiIntuiMessageCodec.TryReadRawKey(ref platform,
			messageAddress, out var rawKey));
		Assert.Equal(MuiIntuiMessageCodec.RawKeyClass, rawKey.Class);
		Assert.Equal((ushort)'K', rawKey.Code);
		Assert.Equal((ushort)0xA55A, rawKey.Qualifier);
		var shortRawKey = APTR.FromPointer(0x3400);
		Assert.True(MuiIntuiMessageCodec.WriteRawKey(ref platform, shortRawKey,
			MuiIntuiMessageCodec.RawKeyClass, (ushort)'R'));
		Assert.True(MuiIntuiMessageCodec.TryReadRawKey(ref platform,
			shortRawKey, out rawKey));
		Assert.Equal((ushort)'R', rawKey.Code);
		Assert.Equal((ushort)0, rawKey.Qualifier);
		Assert.False(MuiIntuiMessageCodec.WriteRawKey(ref platform,
			APTR.FromPointer(0xFFFFFFF0u), MuiIntuiMessageCodec.RawKeyClass,
			(ushort)'X'));
		Assert.False(MuiIntuiMessageCodec.TryReadRawKey(ref platform,
			APTR.FromPointer(0xFFFFFFF0u), out _));

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
		Assert.True(MuiListviewDragStateCodec.WriteRecord(ref platform, dragAddress, drag));
		Assert.True(MuiListviewDragStateCodec.TryReadRecord(ref platform, dragAddress,
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

	[Fact]
	public void ListTestPosSequentialRecordPreservesMixedWidthsAndRejectsTruncation()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3600);
		var expected = new MuiListTestPosResult
		{
			Entry = int.MinValue,
			Column = short.MinValue,
			Flags = 0xA55A,
			XOffset = -9,
			YOffset = short.MaxValue,
		};
		Assert.True(MuiListTestPosResultCodec.WriteRecord(ref platform, address,
			expected));
		Assert.True(MuiListTestPosResultCodec.TryReadRecord(ref platform, address,
			out var actual));
		Assert.Equal(expected.Entry, actual.Entry);
		Assert.Equal(expected.Column, actual.Column);
		Assert.Equal(expected.Flags, actual.Flags);
		Assert.Equal(expected.XOffset, actual.XOffset);
		Assert.Equal(expected.YOffset, actual.YOffset);
		Assert.False(MuiListTestPosResultCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x40FF5), out _));
	}
}
