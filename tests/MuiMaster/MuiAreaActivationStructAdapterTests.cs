using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaActivationStructAdapterTests
{
	[Fact]
	public void AreaActivationStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiAreaActivationStateRecord
		{
			Signature = MuiAreaActivationStateRecord.Cookie,
			Active = 1,
			Flags = uint.MaxValue,
			Generation = 7,
		};

		Assert.True(MuiAreaActivationStateCodec.Write(ref platform, address, value));
		Assert.True(MuiAreaActivationStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaActivationStateField.Flags,
			out var flagsAddress));
		Assert.Equal(0x3508u, flagsAddress.Raw);
		Assert.True(MuiAreaActivationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiAreaActivationStateField.Generation,
			out var generation));
		Assert.Equal(7u, generation);
		Assert.True(MuiAreaActivationStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaActivationStateField.Active, 0));
		Assert.True(MuiAreaActivationStateCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0u, decoded.Active);
		Assert.False(MuiAreaActivationStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF1),
			MuiAreaActivationStateField.Signature, out _));
		Assert.False(MuiAreaActivationStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaActivationStateField.Signature, out _));
	}

	[Fact]
	public void AreaActivationSequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x35C0);
		var value = new MuiAreaActivationStateRecord
		{
			Signature = MuiAreaActivationStateRecord.Cookie,
			Active = uint.MaxValue,
			Flags = 0x01020304u,
			Generation = uint.MaxValue,
		};

		Assert.True(MuiAreaActivationStateCodec.WriteRecord(ref platform, address,
			value));
		Assert.True(MuiAreaActivationStateCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Signature, decoded.Signature);
		Assert.Equal(value.Active, decoded.Active);
		Assert.Equal(value.Flags, decoded.Flags);
		Assert.Equal(value.Generation, decoded.Generation);

		var crossingEnd = APTR.FromPointer(0x30FF1);
		Assert.False(MuiAreaActivationStateCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiAreaActivationStateCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}
}
