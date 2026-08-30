using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiHeadlessObjectStructCodecTests
{
	[Fact]
	public void HeadlessObjectStructCodecRoundTripsLinksAndScalars()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2800);
		var value = new MuiHeadlessObjectRecord
		{
			Next = APTR.FromPointer(0x3000),
			Boopsi = APTR.FromPointer(0x3040),
			Class = APTR.FromPointer(0x3080),
			Attributes = APTR.FromPointer(0x30C0),
			Notifications = APTR.FromPointer(0x3100),
			ChildrenHead = APTR.FromPointer(0x3140),
			ChildrenTail = APTR.FromPointer(0x3180),
			Parent = APTR.FromPointer(0x31C0),
			Stores = APTR.FromPointer(0x3200),
			SemaphoreOwner = APTR.FromPointer(0x3240),
			SemaphoreDepth = 1,
			SemaphoreShared = 2,
			Flags = 3,
			Generation = 4,
			ObjectId = 5,
			UserData = 6,
		};
		Assert.True(MuiHeadlessObjectCodec.WriteRecord(ref platform, address, value));
		Assert.True(MuiHeadlessObjectCodec.TryReadRecord(ref platform, address,
			out var decoded));
		Assert.Equal(value.Next.Raw, decoded.Next.Raw);
		Assert.Equal(value.Boopsi.Raw, decoded.Boopsi.Raw);
		Assert.Equal(value.Class.Raw, decoded.Class.Raw);
		Assert.Equal(value.Attributes.Raw, decoded.Attributes.Raw);
		Assert.Equal(value.Notifications.Raw, decoded.Notifications.Raw);
		Assert.Equal(value.ChildrenHead.Raw, decoded.ChildrenHead.Raw);
		Assert.Equal(value.ChildrenTail.Raw, decoded.ChildrenTail.Raw);
		Assert.Equal(value.Parent.Raw, decoded.Parent.Raw);
		Assert.Equal(value.Stores.Raw, decoded.Stores.Raw);
		Assert.Equal(value.SemaphoreOwner.Raw, decoded.SemaphoreOwner.Raw);
		Assert.Equal(value.SemaphoreDepth, decoded.SemaphoreDepth);
		Assert.Equal(value.SemaphoreShared, decoded.SemaphoreShared);
		Assert.Equal(value.Flags, decoded.Flags);
		Assert.Equal(value.Generation, decoded.Generation);
		Assert.Equal(value.ObjectId, decoded.ObjectId);
		Assert.Equal(value.UserData, decoded.UserData);
		Assert.False(MuiHeadlessObjectCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FC1), out _));
	}
}
