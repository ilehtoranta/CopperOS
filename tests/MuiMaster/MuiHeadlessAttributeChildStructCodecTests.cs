using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiHeadlessAttributeChildStructCodecTests
{
	[Fact]
	public void HeadlessAttributeStructCodecRoundTripsDeclarationOrder()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2800);
		var value = new MuiHeadlessAttributeRecord
		{
			Next = APTR.FromPointer(0x3000), Id = 1, Value = 2, Generation = 3,
		};
		Assert.True(MuiHeadlessAttributeCodec.WriteRecord(ref platform, address,
			value));
		Assert.True(MuiHeadlessAttributeCodec.TryReadRecord(ref platform, address,
			out var decoded));
		Assert.Equal(value.Next.Raw, decoded.Next.Raw);
		Assert.Equal(value.Id, decoded.Id);
		Assert.Equal(value.Value, decoded.Value);
		Assert.Equal(value.Generation, decoded.Generation);
		Assert.False(MuiHeadlessAttributeCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FF1), out _));
	}

	[Fact]
	public void HeadlessChildStructCodecRoundTripsDeclarationOrder()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2900);
		var value = new MuiHeadlessChildRecord
		{
			Next = APTR.FromPointer(0x3000),
			Previous = APTR.FromPointer(0x3040),
			Object = APTR.FromPointer(0x3080),
			Owner = APTR.FromPointer(0x30C0),
		};
		Assert.True(MuiHeadlessChildCodec.WriteRecord(ref platform, address,
			value));
		Assert.True(MuiHeadlessChildCodec.TryReadRecord(ref platform, address,
			out var decoded));
		Assert.Equal(value.Next.Raw, decoded.Next.Raw);
		Assert.Equal(value.Previous.Raw, decoded.Previous.Raw);
		Assert.Equal(value.Object.Raw, decoded.Object.Raw);
		Assert.Equal(value.Owner.Raw, decoded.Owner.Raw);
		Assert.False(MuiHeadlessChildCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FF1), out _));
	}
}
