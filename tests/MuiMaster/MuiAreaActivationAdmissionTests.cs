using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaActivationAdmissionTests
{
	[Fact]
	public void AreaActivationAdmissionRoundTripsNamedStruct()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1500);
		var expected = new MuiAreaActivationStateRecord
		{
			Signature = MuiAreaActivationStateRecord.Cookie,
			Active = 1,
			Flags = 0xA5A5A5A5u,
			Generation = 7,
		};

		Assert.True(MuiAreaActivationStateCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiAreaActivationStateCodec.TryReadStructural(ref platform,
			address, out var structural));
		Assert.Equal(expected.Signature, structural.Signature);
		Assert.Equal(expected.Active, structural.Active);
		Assert.Equal(expected.Flags, structural.Flags);
		Assert.Equal(expected.Generation, structural.Generation);
		Assert.True(MuiAreaActivationStateCodec.TryRead(ref platform, address,
			out var admitted));
		Assert.Equal(expected.Active, admitted.Active);
	}

	[Fact]
	public void MalformedAreaActivationMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1500);
		var expected = new MuiAreaActivationStateRecord
		{
			Signature = MuiAreaActivationStateRecord.Cookie,
			Active = 1,
			Flags = 9,
			Generation = 1,
		};
		Assert.True(MuiAreaActivationStateCodec.Write(ref platform, address,
			expected));

		var cursor = new MuiAreaActivationStateFieldCursor
		{
			Address = address,
			Field = MuiAreaActivationStateField.Signature,
		};
		Assert.True(MuiAreaActivationStateFieldCursorCodec.TryWrite(ref platform,
			address, MuiAreaActivationStateField.Signature, 0));
		Assert.True(MuiAreaActivationStateCodec.TryReadStructural(ref platform,
			address, out var structural));
		Assert.Equal(0u, structural.Signature);
		Assert.False(MuiAreaActivationStateCodec.TryRead(ref platform, address,
			out _));
		Assert.True(MuiAreaActivationStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var signatureAddress));
		Assert.Equal(address.Raw, signatureAddress.Raw);
	}
}
