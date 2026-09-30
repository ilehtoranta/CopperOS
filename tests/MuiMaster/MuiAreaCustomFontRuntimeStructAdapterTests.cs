using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaCustomFontRuntimeStructAdapterTests
{
	[Fact]
	public void CustomFontRuntimeStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3B00);
		var value = new MuiAreaCustomFontRuntimeRecord
		{
			Magic = MuiAreaCustomFontRuntimeRecord.Cookie,
			Font = APTR.FromPointer(0x2A00),
			Spec = APTR.FromPointer(0x1A00),
			Generation = 7,
			Active = 1,
		};

		Assert.True(MuiAreaCustomFontRuntimeRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiAreaCustomFontRuntimeRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaCustomFontRuntimeField.Spec,
			out var specAddress));
		Assert.Equal(0x3B08u, specAddress.Raw);
		var cursor = new MuiAreaCustomFontRuntimeFieldCursor
		{
			Record = address,
			Field = MuiAreaCustomFontRuntimeField.Spec,
		};
		Assert.True(MuiAreaCustomFontRuntimeFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var typedSpecAddress, out var typedSpecSize));
		Assert.Equal(specAddress, typedSpecAddress);
		Assert.Equal(MuiAreaCustomFontRuntimeRecord.FieldSize, typedSpecSize);
		Assert.True(MuiAreaCustomFontRuntimeRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out var memorySpecAddress, out var memorySpecSize));
		Assert.Equal(typedSpecAddress, memorySpecAddress);
		Assert.Equal(typedSpecSize, memorySpecSize);
		Assert.True(MuiAreaCustomFontRuntimeRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaCustomFontRuntimeField.Active, 0));
		Assert.True(MuiAreaCustomFontRuntimeRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Font, decoded.Font);
		Assert.Equal(value.Spec, decoded.Spec);
		Assert.Equal(0u, decoded.Active);
		Assert.False(MuiAreaCustomFontRuntimeRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaCustomFontRuntimeField)255, out _));
		Assert.False(MuiAreaCustomFontRuntimeRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaCustomFontRuntimeField.Magic, out _));
		cursor.Field = (MuiAreaCustomFontRuntimeField)255;
		Assert.False(MuiAreaCustomFontRuntimeFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		cursor.Record = APTR.Null;
		cursor.Field = MuiAreaCustomFontRuntimeField.Magic;
		Assert.False(MuiAreaCustomFontRuntimeFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		Assert.False(MuiAreaCustomFontRuntimeRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void CustomFontRuntimeSequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3BC0);
		var value = new MuiAreaCustomFontRuntimeRecord
		{
			Magic = MuiAreaCustomFontRuntimeRecord.Cookie,
			Font = APTR.FromPointer(0xFEEDBEEF),
			Spec = APTR.FromPointer(0xCAFEBABE),
			Generation = uint.MaxValue,
			Active = uint.MaxValue,
		};

		Assert.True(MuiAreaCustomFontRuntimeRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaCustomFontRuntimeRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Font, decoded.Font);
		Assert.Equal(value.Spec, decoded.Spec);
		Assert.Equal(value.Generation, decoded.Generation);
		Assert.Equal(value.Active, decoded.Active);

		var crossingEnd = APTR.FromPointer(0x30FED);
		Assert.False(MuiAreaCustomFontRuntimeRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiAreaCustomFontRuntimeRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}

	[Fact]
	public void CustomFontRuntimeFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3640);
		var initial = new MuiAreaCustomFontRuntimeRecord
		{
			Magic = 0x10203040u,
			Font = APTR.FromPointer(0x50607080u),
			Spec = APTR.FromPointer(0x90A0B0C0u),
			Generation = 0xDDEEFF00u,
			Active = 1,
		};

		Assert.True(MuiAreaCustomFontRuntimeRecordCodec.WriteRecord(ref platform,
			address, initial));
		Assert.True(MuiAreaCustomFontRuntimeRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaCustomFontRuntimeField.Font,
			0xF1020304u));
		Assert.True(MuiAreaCustomFontRuntimeRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiAreaCustomFontRuntimeField.Spec,
			out var spec));
		Assert.Equal(initial.Spec.Raw, spec);
		Assert.True(MuiAreaCustomFontRuntimeRecordCodec.TryReadStructural(
			ref platform, address, out var updated));
		Assert.Equal(initial.Magic, updated.Magic);
		Assert.Equal(APTR.FromPointer(0xF1020304u), updated.Font);
		Assert.Equal(initial.Spec, updated.Spec);
		Assert.Equal(initial.Generation, updated.Generation);
		Assert.Equal(initial.Active, updated.Active);
		Assert.False(MuiAreaCustomFontRuntimeRecordMemoryCodec.TryWriteUInt32(
			ref platform, address,
			unchecked((MuiAreaCustomFontRuntimeField)255), 1));
	}

	[Fact]
	public void CustomFontRuntimeOffsetBridgeUsesNamedUlongCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3D00);
		var initial = new MuiAreaCustomFontRuntimeRecord
		{
			Magic = 0x10203040u,
			Font = APTR.FromPointer(0x1500),
			Spec = APTR.FromPointer(0x1600),
			Generation = 3,
			Active = 1,
		};
		Assert.True(MuiAreaCustomFontRuntimeRecordCodec.WriteRecord(ref platform,
			address, initial));
		Assert.True(MuiAreaCustomFontRuntimeRecordMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiAreaCustomFontRuntimeRecord.GenerationOffset, 9));
		Assert.True(MuiAreaCustomFontRuntimeRecordMemoryCodec.TryReadUInt32(
			ref platform, address,
			MuiAreaCustomFontRuntimeRecord.GenerationOffset, out var generation));
		Assert.Equal(9u, generation);
		Assert.True(MuiAreaCustomFontRuntimeRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(initial.Magic, decoded.Magic);
		Assert.Equal(initial.Font, decoded.Font);
		Assert.Equal(initial.Spec, decoded.Spec);
		Assert.Equal(9u, decoded.Generation);
		Assert.Equal(initial.Active, decoded.Active);
		Assert.False(MuiAreaCustomFontRuntimeRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiAreaCustomFontRuntimeRecord.Size, out _));
		Assert.False(MuiAreaCustomFontRuntimeRecordMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null,
			MuiAreaCustomFontRuntimeRecord.GenerationOffset, 1));
	}
}
