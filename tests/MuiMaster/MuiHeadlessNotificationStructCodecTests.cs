using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiHeadlessNotificationStructCodecTests
{
	[Fact]
	public void HeadlessNotificationStructCodecRoundTripsHeaderFields()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2800);
		var value = new MuiHeadlessNotificationRecord
		{
			Next = APTR.FromPointer(0x3000),
			Sequence = 1,
			TriggerAttribute = 2,
			TriggerValue = 3,
			Destination = APTR.FromPointer(0x3040),
			FollowCount = 4,
			Flags = 5,
			Reserved = 6,
		};
		Assert.True(MuiHeadlessNotificationCodec.WriteRecord(ref platform, address,
			value));
		Assert.True(MuiHeadlessNotificationCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Next.Raw, decoded.Next.Raw);
		Assert.Equal(value.Sequence, decoded.Sequence);
		Assert.Equal(value.TriggerAttribute, decoded.TriggerAttribute);
		Assert.Equal(value.TriggerValue, decoded.TriggerValue);
		Assert.Equal(value.Destination.Raw, decoded.Destination.Raw);
		Assert.Equal(value.FollowCount, decoded.FollowCount);
		Assert.Equal(value.Flags, decoded.Flags);
		Assert.Equal(value.Reserved, decoded.Reserved);
		Assert.False(MuiHeadlessNotificationCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FE1), out _));
	}
}
