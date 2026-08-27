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
}
