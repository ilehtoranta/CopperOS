using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaDisappearPolicyStructAdapterTests
{
	[Fact]
	public void AreaDisappearPolicyStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiAreaDisappearPolicyStateRecord
		{
			Magic = MuiAreaDisappearPolicyStateRecord.Cookie,
			HorizDisappear = -3,
			VertDisappear = 5,
		};

		Assert.True(MuiAreaDisappearPolicyStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiAreaDisappearPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaDisappearPolicyStateField.HorizDisappear,
			out var horizontalAddress));
		Assert.Equal(0x3504u, horizontalAddress.Raw);
		Assert.True(MuiAreaDisappearPolicyStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaDisappearPolicyStateField.VertDisappear,
			unchecked((uint)-7)));
		Assert.True(MuiAreaDisappearPolicyStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(-7, decoded.VertDisappear);
		Assert.False(MuiAreaDisappearPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaDisappearPolicyStateField)255, out _));
		Assert.False(MuiAreaDisappearPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaDisappearPolicyStateField.Magic, out _));
		Assert.False(MuiAreaDisappearPolicyStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void AreaDisappearPolicySequentialRecordPreservesValuesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x35C0);
		var value = new MuiAreaDisappearPolicyStateRecord
		{
			Magic = MuiAreaDisappearPolicyStateRecord.Cookie,
			HorizDisappear = int.MinValue,
			VertDisappear = int.MaxValue,
		};

		Assert.True(MuiAreaDisappearPolicyStateRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiAreaDisappearPolicyStateRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.HorizDisappear, decoded.HorizDisappear);
		Assert.Equal(value.VertDisappear, decoded.VertDisappear);

		var crossingEnd = APTR.FromPointer(0x30FF5);
		Assert.False(MuiAreaDisappearPolicyStateRecordCodec.WriteRecord(
			ref platform, crossingEnd, value));
		Assert.False(MuiAreaDisappearPolicyStateRecordCodec.TryReadRecord(
			ref platform, crossingEnd, out _));
	}

	[Fact]
	public void AreaDisappearPolicyFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3900);
		var value = new MuiAreaDisappearPolicyStateRecord
		{
			Magic = MuiAreaDisappearPolicyStateRecord.Cookie,
			HorizDisappear = -11,
			VertDisappear = 13,
		};

		Assert.True(MuiAreaDisappearPolicyStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaDisappearPolicyStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaDisappearPolicyStateField.HorizDisappear,
			unchecked((uint)21)));
		Assert.True(MuiAreaDisappearPolicyStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiAreaDisappearPolicyStateField.VertDisappear,
			out var vertical));
		Assert.Equal(13u, vertical);
		Assert.True(MuiAreaDisappearPolicyStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(21, decoded.HorizDisappear);
		Assert.Equal(value.VertDisappear, decoded.VertDisappear);
		Assert.Equal(value.Magic, decoded.Magic);
	}
}
