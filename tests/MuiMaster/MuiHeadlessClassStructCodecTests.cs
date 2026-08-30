using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiHeadlessClassStructCodecTests
{
	[Fact]
	public void HeadlessClassStructCodecRoundTripsMixedDeclarationOrder()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2800);
		var value = new MuiHeadlessClassRecord
		{
			Next = APTR.FromPointer(0x3000),
			Name = APTR.FromPointer(0x3040),
			Boopsi = APTR.FromPointer(0x3080),
			Super = APTR.FromPointer(0x30C0),
			InstanceSize = 96,
			Reserved = 0x55AA,
			Flags = 3,
			ObjectCount = 7,
		};
		Assert.True(MuiHeadlessClassCodec.WriteRecord(ref platform, address, value));
		Assert.True(MuiHeadlessClassCodec.TryReadRecord(ref platform, address,
			out var decoded));
		Assert.Equal(value.Next.Raw, decoded.Next.Raw);
		Assert.Equal(value.Name.Raw, decoded.Name.Raw);
		Assert.Equal(value.Boopsi.Raw, decoded.Boopsi.Raw);
		Assert.Equal(value.Super.Raw, decoded.Super.Raw);
		Assert.Equal(value.InstanceSize, decoded.InstanceSize);
		Assert.Equal(value.Reserved, decoded.Reserved);
		Assert.Equal(value.Flags, decoded.Flags);
		Assert.Equal(value.ObjectCount, decoded.ObjectCount);
		Assert.False(MuiHeadlessClassCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FF0), out _));
	}
}
