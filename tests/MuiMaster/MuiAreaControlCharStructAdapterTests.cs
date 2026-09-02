using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaControlCharStructAdapterTests
{
	[Fact]
	public void AreaControlCharStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiAreaControlCharStateRecord
		{
			Magic = MuiAreaControlCharStateRecord.Cookie,
			Character = 0x41,
			Generation = 7,
		};

		Assert.True(MuiAreaControlCharStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaControlCharStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaControlCharStateField.Character,
			out var characterAddress));
		Assert.Equal(0x3504u, characterAddress.Raw);
		Assert.True(MuiAreaControlCharStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaControlCharStateField.Character, 0x42));
		Assert.True(MuiAreaControlCharStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0x42u, decoded.Character);
		Assert.False(MuiAreaControlCharStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaControlCharStateField)255, out _));
		Assert.False(MuiAreaControlCharStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaControlCharStateField.Magic, out _));
		Assert.False(MuiAreaControlCharStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void AreaControlCharSequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x35C0);
		var value = new MuiAreaControlCharStateRecord
		{
			Magic = MuiAreaControlCharStateRecord.Cookie,
			Character = uint.MaxValue,
			Generation = uint.MaxValue,
		};

		Assert.True(MuiAreaControlCharStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaControlCharStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Character, decoded.Character);
		Assert.Equal(value.Generation, decoded.Generation);

		var crossingEnd = APTR.FromPointer(0x30FF5);
		Assert.False(MuiAreaControlCharStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiAreaControlCharStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}

	[Fact]
	public void AreaControlCharFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3640);
		var initial = new MuiAreaControlCharStateRecord
		{
			Magic = 0x10203040u,
			Character = 0x50607080u,
			Generation = 0x90A0B0C0u,
		};

		Assert.True(MuiAreaControlCharStateRecordCodec.WriteRecord(ref platform,
			address, initial));
		Assert.True(MuiAreaControlCharStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaControlCharStateField.Character,
			0xF1020304u));
		Assert.True(MuiAreaControlCharStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiAreaControlCharStateField.Generation,
			out var generation));
		Assert.Equal(initial.Generation, generation);
		Assert.True(MuiAreaControlCharStateRecordCodec.TryReadStructural(ref platform,
			address, out var updated));
		Assert.Equal(initial.Magic, updated.Magic);
		Assert.Equal(0xF1020304u, updated.Character);
		Assert.Equal(initial.Generation, updated.Generation);
		Assert.False(MuiAreaControlCharStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address,
			unchecked((MuiAreaControlCharStateField)255), 1));
	}
}
