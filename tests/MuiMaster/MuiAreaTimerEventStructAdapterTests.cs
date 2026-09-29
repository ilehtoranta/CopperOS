using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaTimerEventStructAdapterTests
{
	[Fact]
	public void AreaTimerEventStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3E00);
		var value = new MuiAreaTimerEventStateRecord
		{
			Magic = MuiAreaTimerEventStateRecord.Cookie,
			Armed = 1,
			MouseOver = 1,
			DelayElapsed = 0,
			LastTick = 0xFEDCBA98,
			Generation = 7,
		};

		Assert.True(MuiAreaTimerEventStateCodec.Write(ref platform, address, value));
		Assert.True(MuiAreaTimerEventStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaTimerEventStateField.LastTick,
			out var tickAddress));
		Assert.Equal(0x3E10u, tickAddress.Raw);
		var cursor = new MuiAreaTimerEventStateFieldCursor
		{
			Record = address,
			Field = MuiAreaTimerEventStateField.LastTick,
		};
		Assert.True(MuiAreaTimerEventStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var cursorTickAddress));
		Assert.Equal(tickAddress.Raw, cursorTickAddress.Raw);
		Assert.True(MuiAreaTimerEventStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var typedTickAddress, out var typedTickSize));
		Assert.Equal(cursorTickAddress, typedTickAddress);
		Assert.Equal(MuiAreaTimerEventStateRecord.FieldSize, typedTickSize);
		Assert.True(MuiAreaTimerEventStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out var memoryTickAddress, out var memoryTickSize));
		Assert.Equal(typedTickAddress, memoryTickAddress);
		Assert.Equal(typedTickSize, memoryTickSize);
		Assert.True(MuiAreaTimerEventStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaTimerEventStateField.LastTick,
			0x12345678));
		Assert.True(MuiAreaTimerEventStateCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0x12345678u, decoded.LastTick);
		Assert.Equal(1u, decoded.Armed);
		Assert.False(MuiAreaTimerEventStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaTimerEventStateField)255, out _));
		Assert.False(MuiAreaTimerEventStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaTimerEventStateField.Magic, out _));
		cursor.Field = (MuiAreaTimerEventStateField)255;
		Assert.False(MuiAreaTimerEventStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		cursor.Record = APTR.Null;
		cursor.Field = MuiAreaTimerEventStateField.Magic;
		Assert.False(MuiAreaTimerEventStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		Assert.False(MuiAreaTimerEventStateCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void AreaTimerEventFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3F40);
		var value = new MuiAreaTimerEventStateRecord
		{
			Magic = MuiAreaTimerEventStateRecord.Cookie,
			Armed = 1,
			MouseOver = 1,
			DelayElapsed = 0,
			LastTick = 0xFEDCBA98u,
			Generation = 23,
		};

		Assert.True(MuiAreaTimerEventStateCodec.WriteRecord(ref platform, address,
			value));
		Assert.True(MuiAreaTimerEventStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaTimerEventStateField.LastTick,
			0x12345678u));
		Assert.True(MuiAreaTimerEventStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiAreaTimerEventStateField.Generation,
			out var generation));
		Assert.Equal(value.Generation, generation);
		Assert.True(MuiAreaTimerEventStateCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0x12345678u, decoded.LastTick);
		Assert.Equal(value.Armed, decoded.Armed);
		Assert.Equal(value.MouseOver, decoded.MouseOver);
		Assert.Equal(value.Generation, decoded.Generation);
	}
}
