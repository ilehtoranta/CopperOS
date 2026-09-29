using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaFontSelectionStructAdapterTests
{
	[Fact]
	public void AreaFontSelectionStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3C00);
		var value = new MuiAreaFontSelectionStateRecord
		{
			Magic = MuiAreaFontSelectionStateRecord.Cookie,
			Active = (uint)MuiAreaFontSelectionKind.CustomFont,
			Source = APTR.FromPointer(0x1A00),
			Generation = 7,
		};

		Assert.True(MuiAreaFontSelectionStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiAreaFontSelectionStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaFontSelectionStateField.Source,
			out var sourceAddress));
		Assert.Equal(0x3C08u, sourceAddress.Raw);
		var cursor = new MuiAreaFontSelectionStateFieldCursor
		{
			Record = address,
			Field = MuiAreaFontSelectionStateField.Source,
		};
		Assert.True(MuiAreaFontSelectionStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var typedSourceAddress, out var typedSourceSize));
		Assert.Equal(sourceAddress, typedSourceAddress);
		Assert.Equal(MuiAreaFontSelectionStateRecord.FieldSize, typedSourceSize);
		Assert.True(MuiAreaFontSelectionStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out var memorySourceAddress, out var memorySourceSize));
		Assert.Equal(typedSourceAddress, memorySourceAddress);
		Assert.Equal(typedSourceSize, memorySourceSize);
		Assert.True(MuiAreaFontSelectionStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Source, decoded.Source);
		Assert.Equal(value.Active, decoded.Active);
		Assert.False(MuiAreaFontSelectionStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaFontSelectionStateField)255, out _));
		Assert.False(MuiAreaFontSelectionStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaFontSelectionStateField.Magic, out _));
		cursor.Field = (MuiAreaFontSelectionStateField)255;
		Assert.False(MuiAreaFontSelectionStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		cursor.Record = APTR.Null;
		cursor.Field = MuiAreaFontSelectionStateField.Magic;
		Assert.False(MuiAreaFontSelectionStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		Assert.False(MuiAreaFontSelectionStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void AreaFontSelectionSequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3CC0);
		var value = new MuiAreaFontSelectionStateRecord
		{
			Magic = MuiAreaFontSelectionStateRecord.Cookie,
			Active = uint.MaxValue,
			Source = APTR.FromPointer(0xFEEDBEEF),
			Generation = uint.MaxValue,
		};

		Assert.True(MuiAreaFontSelectionStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaFontSelectionStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Active, decoded.Active);
		Assert.Equal(value.Source, decoded.Source);
		Assert.Equal(value.Generation, decoded.Generation);

		var crossingEnd = APTR.FromPointer(0x30FF1);
		Assert.False(MuiAreaFontSelectionStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiAreaFontSelectionStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}

	[Fact]
	public void AreaFontSelectionFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3E00);
		var value = new MuiAreaFontSelectionStateRecord
		{
			Magic = MuiAreaFontSelectionStateRecord.Cookie,
			Active = (uint)MuiAreaFontSelectionKind.Font,
			Source = APTR.FromPointer(0x4100),
			Generation = 19,
		};

		Assert.True(MuiAreaFontSelectionStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaFontSelectionStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaFontSelectionStateField.Source,
			0xFEEDBEEFu));
		Assert.True(MuiAreaFontSelectionStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiAreaFontSelectionStateField.Generation,
			out var generation));
		Assert.Equal(value.Generation, generation);
		Assert.True(MuiAreaFontSelectionStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Active, decoded.Active);
		Assert.Equal(0xFEEDBEEFu, decoded.Source.Raw);
		Assert.Equal(value.Generation, decoded.Generation);
	}
}
