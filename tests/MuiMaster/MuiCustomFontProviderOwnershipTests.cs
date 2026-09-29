using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiCustomFontProviderOwnershipTests
{
	[Fact]
	public void ProviderRejectsDuplicateAndUnknownClosesWithoutConsumingAnotherHandle()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, APTR.FromPointer(0x1000));
		var request = default(MuiCustomFontOpenRequest);
		var first = platform.OpenMuiCustomFont(ref request);
		var second = platform.OpenMuiCustomFont(ref request);
		Assert.NotEqual(first, second);
		Assert.True(platform.CloseMuiCustomFont(first));
		Assert.False(platform.CloseMuiCustomFont(first));
		Assert.False(platform.CloseMuiCustomFont(APTR.FromPointer(0xD000)));
		Assert.True(platform.CloseMuiCustomFont(second));
		Assert.Equal(2u, platform.InvalidCustomFontCloses);
		Assert.Equal(4u, platform.CustomFontCloseCount);
	}
}
