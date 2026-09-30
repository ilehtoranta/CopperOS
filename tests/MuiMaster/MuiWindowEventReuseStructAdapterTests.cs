using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiWindowEventReuseStructAdapterTests
{
	[Fact]
	public void WindowEventReuseStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiWindowEventReuseStateRecord
		{
			Magic = MuiWindowEventReuseStateRecord.Cookie,
			ContextActive = 0,
			Pending = 0,
			EventMessage = APTR.Null,
			InputEvent = APTR.Null,
			EventClass = 0,
			MuiKey = -7,
		};

		Assert.True(MuiWindowEventReuseStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiWindowEventReuseStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiWindowEventReuseStateField.MuiKey,
			out var keyAddress));
		Assert.Equal(0x3518u, keyAddress.Raw);
		var fieldCursor = new MuiWindowEventReuseStateFieldCursor
		{
			Record = address,
			Field = MuiWindowEventReuseStateField.MuiKey,
		};
		Assert.True(MuiWindowEventReuseStateFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var cursorAddress, out var fieldSize));
		Assert.Equal(keyAddress, cursorAddress);
		Assert.Equal(MuiWindowEventReuseStateRecord.FieldSize, fieldSize);
		Assert.True(MuiWindowEventReuseStateRecordMemoryCodec.TryGetAddress(
			ref platform, fieldCursor, out var memoryCursorAddress,
			out var memoryFieldSize));
		Assert.Equal(keyAddress, memoryCursorAddress);
		Assert.Equal(MuiWindowEventReuseStateRecord.FieldSize, memoryFieldSize);
		Assert.True(MuiWindowEventReuseStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiWindowEventReuseStateField.MuiKey,
			unchecked((uint)-8)));
		Assert.True(MuiWindowEventReuseStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(-8, decoded.MuiKey);
		Assert.False(MuiWindowEventReuseStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FE5),
			MuiWindowEventReuseStateField.Magic, out _));
		Assert.False(MuiWindowEventReuseStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiWindowEventReuseStateField.Magic, out _));
		fieldCursor.Record = APTR.Null;
		fieldCursor.Field = MuiWindowEventReuseStateField.Magic;
		Assert.False(MuiWindowEventReuseStateFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out _, out _));
	}

	[Fact]
	public void WindowEventReuseSequentialRecordPreservesPointersAndSignedKey()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3580);
		var value = new MuiWindowEventReuseStateRecord
		{
			Magic = MuiWindowEventReuseStateRecord.Cookie,
			ContextActive = 1,
			Pending = 1,
			EventMessage = APTR.FromPointer(0xFEEDBEEF),
			InputEvent = APTR.FromPointer(0xCAFEBABE),
			EventClass = 7,
			MuiKey = -1,
		};

		Assert.True(MuiWindowEventReuseStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiWindowEventReuseStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.ContextActive, decoded.ContextActive);
		Assert.Equal(value.Pending, decoded.Pending);
		Assert.Equal(value.EventMessage, decoded.EventMessage);
		Assert.Equal(value.InputEvent, decoded.InputEvent);
		Assert.Equal(value.EventClass, decoded.EventClass);
		Assert.Equal(value.MuiKey, decoded.MuiKey);

		var crossingEnd = APTR.FromPointer(0x30FE5);
		Assert.False(MuiWindowEventReuseStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiWindowEventReuseStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}
}
