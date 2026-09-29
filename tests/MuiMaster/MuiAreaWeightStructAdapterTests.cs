using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaWeightStructAdapterTests
{
	[Fact]
	public void AreaWeightStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiAreaWeightStateRecord
		{
			Magic = MuiAreaWeightStateRecord.Cookie,
			Weight = uint.MaxValue,
		};

		Assert.True(MuiAreaWeightStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaWeightStateRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiAreaWeightStateField.Weight, out var weightAddress));
		Assert.Equal(0x3504u, weightAddress.Raw);
		var cursor = new MuiAreaWeightStateFieldCursor
		{
			Record = address,
			Field = MuiAreaWeightStateField.Weight,
		};
		Assert.True(MuiAreaWeightStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var typedWeightAddress, out var typedWeightSize));
		Assert.Equal(weightAddress, typedWeightAddress);
		Assert.Equal(MuiAreaWeightStateRecord.FieldSize, typedWeightSize);
		Assert.True(MuiAreaWeightStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out var memoryWeightAddress, out var memoryWeightSize));
		Assert.Equal(typedWeightAddress, memoryWeightAddress);
		Assert.Equal(typedWeightSize, memoryWeightSize);
		Assert.True(MuiAreaWeightStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiAreaWeightStateField.Weight, out var weight));
		Assert.Equal(uint.MaxValue, weight);
		Assert.True(MuiAreaWeightStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiAreaWeightStateField.Weight, 0x12345678));
		Assert.True(MuiAreaWeightStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0x12345678u, decoded.Weight);
		Assert.False(MuiAreaWeightStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x30FF9), MuiAreaWeightStateField.Magic, out _));
		Assert.False(MuiAreaWeightStateRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiAreaWeightStateField.Magic, out _));
		cursor.Field = (MuiAreaWeightStateField)255;
		Assert.False(MuiAreaWeightStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		cursor.Record = APTR.Null;
		cursor.Field = MuiAreaWeightStateField.Weight;
		Assert.False(MuiAreaWeightStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
	}

	[Fact]
	public void AreaWeightSequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x35C0);
		var value = new MuiAreaWeightStateRecord
		{
			Magic = MuiAreaWeightStateRecord.Cookie,
			Weight = uint.MaxValue,
		};

		Assert.True(MuiAreaWeightStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaWeightStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Weight, decoded.Weight);

		var crossingEnd = APTR.FromPointer(0x30FF9);
		Assert.False(MuiAreaWeightStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiAreaWeightStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}

	[Fact]
	public void AreaWeightFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3F00);
		var value = new MuiAreaWeightStateRecord
		{
			Magic = MuiAreaWeightStateRecord.Cookie,
			Weight = 7,
		};

		Assert.True(MuiAreaWeightStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaWeightStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiAreaWeightStateField.Weight, 0xFEEDBEEFu));
		Assert.True(MuiAreaWeightStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiAreaWeightStateField.Magic, out var magic));
		Assert.Equal(value.Magic, magic);
		Assert.True(MuiAreaWeightStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(0xFEEDBEEFu, decoded.Weight);
	}
}
