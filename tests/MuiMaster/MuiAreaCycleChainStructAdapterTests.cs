using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaCycleChainStructAdapterTests
{
	[Fact]
	public void AreaCycleChainStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3700);
		var value = new MuiAreaCycleChainStateRecord
		{
			Magic = MuiAreaCycleChainStateRecord.Cookie,
			Value = -123,
			Generation = 7,
		};

		Assert.True(MuiAreaCycleChainStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaCycleChainStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaCycleChainStateField.Value,
			out var valueAddress));
		Assert.Equal(0x3704u, valueAddress.Raw);
		Assert.True(MuiAreaCycleChainStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaCycleChainStateField.Value,
			unchecked((uint)456)));
		Assert.True(MuiAreaCycleChainStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(456, decoded.Value);
		Assert.False(MuiAreaCycleChainStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaCycleChainStateField)255, out _));
		Assert.False(MuiAreaCycleChainStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaCycleChainStateField.Magic, out _));
		Assert.False(MuiAreaCycleChainStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void AreaCycleChainSequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x37C0);
		var value = new MuiAreaCycleChainStateRecord
		{
			Magic = MuiAreaCycleChainStateRecord.Cookie,
			Value = int.MinValue,
			Generation = uint.MaxValue,
		};

		Assert.True(MuiAreaCycleChainStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaCycleChainStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Value, decoded.Value);
		Assert.Equal(value.Generation, decoded.Generation);

		var crossingEnd = APTR.FromPointer(0x30FF5);
		Assert.False(MuiAreaCycleChainStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiAreaCycleChainStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}

	[Fact]
	public void AreaCycleChainFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3640);
		var initial = new MuiAreaCycleChainStateRecord
		{
			Magic = 0x10203040u,
			Value = -123,
			Generation = 0x90A0B0C0u,
		};

		Assert.True(MuiAreaCycleChainStateRecordCodec.WriteRecord(ref platform,
			address, initial));
		Assert.True(MuiAreaCycleChainStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaCycleChainStateField.Value,
			unchecked((uint)456)));
		Assert.True(MuiAreaCycleChainStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiAreaCycleChainStateField.Generation,
			out var generation));
		Assert.Equal(initial.Generation, generation);
		Assert.True(MuiAreaCycleChainStateRecordCodec.TryReadStructural(ref platform,
			address, out var updated));
		Assert.Equal(initial.Magic, updated.Magic);
		Assert.Equal(456, updated.Value);
		Assert.Equal(initial.Generation, updated.Generation);
		Assert.False(MuiAreaCycleChainStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address,
			unchecked((MuiAreaCycleChainStateField)255), 1));
	}
}
