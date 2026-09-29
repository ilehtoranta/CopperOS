using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaFloatingStructAdapterTests
{
	[Fact]
	public void AreaFloatingStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3900);
		var value = new MuiAreaFloatingStateRecord
		{
			Magic = MuiAreaFloatingStateRecord.Cookie,
			Enabled = 1,
			Generation = 7,
		};

		Assert.True(MuiAreaFloatingStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaFloatingStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaFloatingStateField.Enabled,
			out var enabledAddress));
		Assert.Equal(0x3904u, enabledAddress.Raw);
		var cursor = new MuiAreaFloatingStateFieldCursor
		{
			Record = address,
			Field = MuiAreaFloatingStateField.Enabled,
		};
		Assert.True(MuiAreaFloatingStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var typedEnabledAddress, out var typedEnabledSize));
		Assert.Equal(enabledAddress, typedEnabledAddress);
		Assert.Equal(MuiAreaFloatingStateRecord.FieldSize, typedEnabledSize);
		Assert.True(MuiAreaFloatingStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out var memoryEnabledAddress, out var memoryEnabledSize));
		Assert.Equal(typedEnabledAddress, memoryEnabledAddress);
		Assert.Equal(typedEnabledSize, memoryEnabledSize);
		Assert.True(MuiAreaFloatingStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaFloatingStateField.Enabled, 0));
		Assert.True(MuiAreaFloatingStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0u, decoded.Enabled);
		Assert.False(MuiAreaFloatingStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaFloatingStateField)255, out _));
		Assert.False(MuiAreaFloatingStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaFloatingStateField.Magic, out _));
		cursor.Field = (MuiAreaFloatingStateField)255;
		Assert.False(MuiAreaFloatingStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		cursor.Record = APTR.Null;
		cursor.Field = MuiAreaFloatingStateField.Magic;
		Assert.False(MuiAreaFloatingStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		Assert.False(MuiAreaFloatingStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void AreaFloatingSequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x39C0);
		var value = new MuiAreaFloatingStateRecord
		{
			Magic = MuiAreaFloatingStateRecord.Cookie,
			Enabled = uint.MaxValue,
			Generation = uint.MaxValue,
		};

		Assert.True(MuiAreaFloatingStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaFloatingStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Enabled, decoded.Enabled);
		Assert.Equal(value.Generation, decoded.Generation);

		var crossingEnd = APTR.FromPointer(0x30FF5);
		Assert.False(MuiAreaFloatingStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiAreaFloatingStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}

	[Fact]
	public void AreaFloatingFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3B00);
		var value = new MuiAreaFloatingStateRecord
		{
			Magic = MuiAreaFloatingStateRecord.Cookie,
			Enabled = 1,
			Generation = 23,
		};

		Assert.True(MuiAreaFloatingStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaFloatingStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaFloatingStateField.Enabled, 0));
		Assert.True(MuiAreaFloatingStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiAreaFloatingStateField.Generation,
			out var generation));
		Assert.Equal(value.Generation, generation);
		Assert.True(MuiAreaFloatingStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0u, decoded.Enabled);
		Assert.Equal(value.Generation, decoded.Generation);
		Assert.Equal(value.Magic, decoded.Magic);
	}
}
