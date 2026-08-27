using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaBufferWeightAdmissionTests
{
	[Fact]
	public void AreaBufferAndWeightRecordsRoundTripThroughNamedStructs()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var doubleBufferAddress = APTR.FromPointer(0x1500);
		var floatingAddress = APTR.FromPointer(0x1520);
		var weightAddress = APTR.FromPointer(0x1540);
		var doubleBuffer = new MuiAreaDoubleBufferStateRecord
		{
			Magic = MuiAreaDoubleBufferStateRecord.Cookie,
			Enabled = 1,
			Generation = 3,
		};
		var floating = new MuiAreaFloatingStateRecord
		{
			Magic = MuiAreaFloatingStateRecord.Cookie,
			Enabled = 0,
			Generation = 5,
		};
		var weight = new MuiAreaWeightStateRecord
		{
			Magic = MuiAreaWeightStateRecord.Cookie,
			Weight = uint.MaxValue,
		};
		Assert.True(MuiAreaDoubleBufferStateRecordCodec.Write(ref platform,
			doubleBufferAddress, doubleBuffer));
		Assert.True(MuiAreaFloatingStateRecordCodec.Write(ref platform,
			floatingAddress, floating));
		Assert.True(MuiAreaWeightStateRecordCodec.Write(ref platform, weightAddress,
			weight));
		Assert.True(MuiAreaDoubleBufferStateRecordCodec.TryRead(ref platform,
			doubleBufferAddress, out var doubleBufferRead));
		Assert.True(MuiAreaFloatingStateRecordCodec.TryRead(ref platform,
			floatingAddress, out var floatingRead));
		Assert.True(MuiAreaWeightStateRecordCodec.TryRead(ref platform, weightAddress,
			out var weightRead));
		Assert.Equal(doubleBuffer.Enabled, doubleBufferRead.Enabled);
		Assert.Equal(floating.Enabled, floatingRead.Enabled);
		Assert.Equal(weight.Weight, weightRead.Weight);
	}

	[Fact]
	public void AreaDoubleBufferStateUsesDedicatedStructCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1580);
		var value = default(MuiAreaDoubleBufferStateRecord);
		value.Magic = MuiAreaDoubleBufferStateRecord.Cookie;
		value.Enabled = 1;
		value.Generation = 3;

		Assert.True(MuiAreaDoubleBufferStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiAreaDoubleBufferStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Enabled, decoded.Enabled);
		Assert.Equal(value.Generation, decoded.Generation);
		Assert.True(MuiAreaDoubleBufferStateRecordCodec.TryRead(ref platform,
			address, out decoded));
		Assert.False(MuiAreaDoubleBufferStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void AreaFloatingStateUsesDedicatedStructCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x15A0);
		var value = default(MuiAreaFloatingStateRecord);
		value.Magic = MuiAreaFloatingStateRecord.Cookie;
		value.Enabled = 0;
		value.Generation = 5;

		Assert.True(MuiAreaFloatingStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaFloatingStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Enabled, decoded.Enabled);
		Assert.Equal(value.Generation, decoded.Generation);
		Assert.True(MuiAreaFloatingStateRecordCodec.TryRead(ref platform, address,
			out decoded));
		Assert.False(MuiAreaFloatingStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void AreaWeightStateUsesDedicatedStructCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1560);
		var value = default(MuiAreaWeightStateRecord);
		value.Magic = MuiAreaWeightStateRecord.Cookie;
		value.Weight = uint.MaxValue;

		Assert.True(MuiAreaWeightStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaWeightStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Weight, decoded.Weight);
		Assert.True(MuiAreaWeightStateRecordCodec.TryRead(ref platform, address,
			out decoded));
		Assert.False(MuiAreaWeightStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void MalformedAreaBufferWeightMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var doubleBufferAddress = APTR.FromPointer(0x1500);
		var floatingAddress = APTR.FromPointer(0x1520);
		var weightAddress = APTR.FromPointer(0x1540);
		Assert.True(MuiAreaDoubleBufferStateRecordCodec.Write(ref platform,
			doubleBufferAddress, new MuiAreaDoubleBufferStateRecord
			{
				Magic = MuiAreaDoubleBufferStateRecord.Cookie,
				Enabled = 1,
				Generation = 1,
			}));
		Assert.True(MuiAreaFloatingStateRecordCodec.Write(ref platform,
			floatingAddress, new MuiAreaFloatingStateRecord
			{
				Magic = MuiAreaFloatingStateRecord.Cookie,
				Enabled = 1,
				Generation = 1,
			}));
		Assert.True(MuiAreaWeightStateRecordCodec.Write(ref platform, weightAddress,
			new MuiAreaWeightStateRecord
			{
				Magic = MuiAreaWeightStateRecord.Cookie,
				Weight = 7,
			}));
		Assert.True(MuiAreaDoubleBufferStateFieldCursorCodec.TryWriteUInt32(
			ref platform, doubleBufferAddress, MuiAreaDoubleBufferStateField.Magic,
			0));
		Assert.True(MuiAreaFloatingStateFieldCursorCodec.TryWriteUInt32(ref platform,
			floatingAddress, MuiAreaFloatingStateField.Magic, 0));
		Assert.True(MuiAreaWeightStateFieldCursorCodec.TryWriteUInt32(ref platform,
			weightAddress, MuiAreaWeightStateField.Magic, 0));
		Assert.True(MuiAreaDoubleBufferStateRecordCodec.TryReadStructural(
			ref platform, doubleBufferAddress, out var doubleBuffer));
		Assert.True(MuiAreaFloatingStateRecordCodec.TryReadStructural(ref platform,
			floatingAddress, out var floating));
		Assert.True(MuiAreaWeightStateRecordCodec.TryReadStructural(ref platform,
			weightAddress, out var weight));
		Assert.Equal(0u, doubleBuffer.Magic);
		Assert.Equal(0u, floating.Magic);
		Assert.Equal(0u, weight.Magic);
		Assert.False(MuiAreaDoubleBufferStateRecordCodec.TryRead(ref platform,
			doubleBufferAddress, out _));
		Assert.False(MuiAreaFloatingStateRecordCodec.TryRead(ref platform,
			floatingAddress, out _));
		Assert.False(MuiAreaWeightStateRecordCodec.TryRead(ref platform, weightAddress,
			out _));
	}
}
