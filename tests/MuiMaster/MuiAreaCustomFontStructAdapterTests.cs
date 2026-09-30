using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaCustomFontStructAdapterTests
{
	[Fact]
	public void AreaCustomFontStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3A00);
		var value = new MuiAreaCustomFontStateRecord
		{
			Magic = MuiAreaCustomFontStateRecord.Cookie,
			Spec = APTR.FromPointer(0x1A00),
			Present = 1,
			Generation = 7,
		};

		Assert.True(MuiAreaCustomFontStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaCustomFontStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaCustomFontStateField.Spec,
			out var specAddress));
		Assert.Equal(0x3A04u, specAddress.Raw);
		var cursor = new MuiAreaCustomFontStateFieldCursor
		{
			Record = address,
			Field = MuiAreaCustomFontStateField.Spec,
		};
		Assert.True(MuiAreaCustomFontStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var typedSpecAddress, out var typedSpecSize));
		Assert.Equal(specAddress, typedSpecAddress);
		Assert.Equal(MuiAreaCustomFontStateRecord.FieldSize, typedSpecSize);
		Assert.True(MuiAreaCustomFontStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out var memorySpecAddress, out var memorySpecSize));
		Assert.Equal(typedSpecAddress, memorySpecAddress);
		Assert.Equal(typedSpecSize, memorySpecSize);
		Assert.True(MuiAreaCustomFontStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(value.Spec, decoded.Spec);
		Assert.Equal(value.Present, decoded.Present);
		Assert.False(MuiAreaCustomFontStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaCustomFontStateField)255, out _));
		Assert.False(MuiAreaCustomFontStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaCustomFontStateField.Magic, out _));
		cursor.Field = (MuiAreaCustomFontStateField)255;
		Assert.False(MuiAreaCustomFontStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		cursor.Record = APTR.Null;
		cursor.Field = MuiAreaCustomFontStateField.Magic;
		Assert.False(MuiAreaCustomFontStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		Assert.False(MuiAreaCustomFontStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void AreaCustomFontSequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3AC0);
		var value = new MuiAreaCustomFontStateRecord
		{
			Magic = MuiAreaCustomFontStateRecord.Cookie,
			Spec = APTR.FromPointer(0xFEEDBEEF),
			Present = uint.MaxValue,
			Generation = uint.MaxValue,
		};

		Assert.True(MuiAreaCustomFontStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaCustomFontStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Spec, decoded.Spec);
		Assert.Equal(value.Present, decoded.Present);
		Assert.Equal(value.Generation, decoded.Generation);

		var crossingEnd = APTR.FromPointer(0x30FF1);
		Assert.False(MuiAreaCustomFontStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiAreaCustomFontStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}

	[Fact]
	public void AreaCustomFontFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3640);
		var initial = new MuiAreaCustomFontStateRecord
		{
			Magic = 0x10203040u,
			Spec = APTR.FromPointer(0x50607080u),
			Present = 1,
			Generation = 0x90A0B0C0u,
		};

		Assert.True(MuiAreaCustomFontStateRecordCodec.WriteRecord(ref platform,
			address, initial));
		Assert.True(MuiAreaCustomFontStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaCustomFontStateField.Spec,
			0xF1020304u));
		Assert.True(MuiAreaCustomFontStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiAreaCustomFontStateField.Generation,
			out var generation));
		Assert.Equal(initial.Generation, generation);
		Assert.True(MuiAreaCustomFontStateRecordCodec.TryReadStructural(ref platform,
			address, out var updated));
		Assert.Equal(initial.Magic, updated.Magic);
		Assert.Equal(APTR.FromPointer(0xF1020304u), updated.Spec);
		Assert.Equal(initial.Present, updated.Present);
		Assert.Equal(initial.Generation, updated.Generation);
		Assert.False(MuiAreaCustomFontStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address,
			unchecked((MuiAreaCustomFontStateField)255), 1));
	}
}
