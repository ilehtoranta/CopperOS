using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaTimerStructAdapterTests
{
	[Fact]
	public void AreaTimerStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3D00);
		var value = new MuiAreaTimerStateRecord
		{
			Magic = MuiAreaTimerStateRecord.Cookie,
			Value = -123,
			Generation = 7,
		};

		Assert.True(MuiAreaTimerStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaTimerStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiAreaTimerStateField.Value, out var valueAddress));
		Assert.Equal(0x3D04u, valueAddress.Raw);
		var cursor = new MuiAreaTimerStateFieldCursor
		{
			Record = address,
			Field = MuiAreaTimerStateField.Value,
		};
		Assert.True(MuiAreaTimerStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var typedValueAddress, out var typedValueSize));
		Assert.Equal(valueAddress, typedValueAddress);
		Assert.Equal(MuiAreaTimerStateRecord.FieldSize, typedValueSize);
		Assert.True(MuiAreaTimerStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out var memoryValueAddress, out var memoryValueSize));
		Assert.Equal(typedValueAddress, memoryValueAddress);
		Assert.Equal(typedValueSize, memoryValueSize);
		Assert.True(MuiAreaTimerStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiAreaTimerStateField.Value, unchecked((uint)456)));
		Assert.True(MuiAreaTimerStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(456, decoded.Value);
		Assert.False(MuiAreaTimerStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, (MuiAreaTimerStateField)255, out _));
		Assert.False(MuiAreaTimerStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiAreaTimerStateField.Magic, out _));
		cursor.Field = (MuiAreaTimerStateField)255;
		Assert.False(MuiAreaTimerStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		cursor.Record = APTR.Null;
		cursor.Field = MuiAreaTimerStateField.Magic;
		Assert.False(MuiAreaTimerStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		Assert.False(MuiAreaTimerStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void AreaTimerSequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3DC0);
		var value = new MuiAreaTimerStateRecord
		{
			Magic = MuiAreaTimerStateRecord.Cookie,
			Value = int.MinValue,
			Generation = uint.MaxValue,
		};

		Assert.True(MuiAreaTimerStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaTimerStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Value, decoded.Value);
		Assert.Equal(value.Generation, decoded.Generation);

		var crossingEnd = APTR.FromPointer(0x30FF5);
		Assert.False(MuiAreaTimerStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiAreaTimerStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}

	[Fact]
	public void AreaTimerFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3F00);
		var value = new MuiAreaTimerStateRecord
		{
			Magic = MuiAreaTimerStateRecord.Cookie,
			Value = -17,
			Generation = 23,
		};

		Assert.True(MuiAreaTimerStateRecordCodec.WriteRecord(ref platform, address,
			value));
		Assert.True(MuiAreaTimerStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiAreaTimerStateField.Value, unchecked((uint)29)));
		Assert.True(MuiAreaTimerStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiAreaTimerStateField.Generation, out var generation));
		Assert.Equal(value.Generation, generation);
		Assert.True(MuiAreaTimerStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(29, decoded.Value);
		Assert.Equal(value.Generation, decoded.Generation);
		Assert.Equal(value.Magic, decoded.Magic);
	}
}
