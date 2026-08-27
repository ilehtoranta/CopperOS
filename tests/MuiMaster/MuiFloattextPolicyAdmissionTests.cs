using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiFloattextPolicyAdmissionTests
{
	[Fact]
	public void FloattextPolicyRoundTripsThroughNamedStruct()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2680);
		var value = new MuiFloattextPolicyState
		{
			Magic = MuiFloattextPolicyState.Cookie,
			Text = APTR.FromPointer(0x2700),
			SkipChars = APTR.FromPointer(0x2740),
			TabSize = 4,
			Justify = 1,
			Width = 640,
		};
		Assert.True(MuiFloattextPolicyStateCodec.Write(ref platform, address, value));
		Assert.True(MuiFloattextPolicyStateCodec.TryRead(ref platform, address,
			out var read));
		Assert.Equal(value.Magic, read.Magic);
		Assert.Equal(value.Text, read.Text);
		Assert.Equal(value.SkipChars, read.SkipChars);
		Assert.Equal(value.TabSize, read.TabSize);
		Assert.Equal(value.Justify, read.Justify);
		Assert.Equal(value.Width, read.Width);
		Assert.True(MuiFloattextPolicyValidation.IsValid(read));
	}

	[Fact]
	public void MalformedFloattextPolicyMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x26C0);
		var value = new MuiFloattextPolicyState
		{
			Magic = MuiFloattextPolicyState.Cookie,
			Text = APTR.FromPointer(0x2780),
			SkipChars = APTR.FromPointer(0x27C0),
			TabSize = 8,
			Justify = 0,
			Width = 320,
		};
		Assert.True(MuiFloattextPolicyStateCodec.Write(ref platform, address, value));
		Assert.True(MuiFloattextPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiFloattextPolicyField.Magic, 0));
		Assert.True(MuiFloattextPolicyStateCodec.TryReadStructural(ref platform,
			address, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.Equal(value.Text, structural.Text);
		Assert.Equal(value.Width, structural.Width);
		Assert.False(MuiFloattextPolicyStateCodec.TryRead(ref platform, address,
			out _));
	}
}
