using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeNewAmigaGuideStructAdapterTests
{
	[Fact]
	public void NewAmigaGuideCodecRoundTripsNamedAbiFields()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3800);
		var value = new NewAmigaGuide
		{
			Lock = BPTR.FromRaw(0x10203040u),
			Name = STRPTR.FromPointer(0x11223344u),
			Screen = APTR.FromPointer(0x22334455u),
			PublicScreen = STRPTR.FromPointer(0x33445566u),
			HostPort = STRPTR.FromPointer(0x44556677u),
			ClientPort = STRPTR.FromPointer(0x55667788u),
			BaseName = STRPTR.FromPointer(0x66778899u),
			Flags = 0x778899AAu,
			Context = APTR.FromPointer(0x8899AABBu),
			Node = STRPTR.FromPointer(0x99AABBCCu),
			Line = -1234567,
			Extensions = APTR.FromPointer(0xAABBCCDDu),
			Client = APTR.FromPointer(0xBBCCDDEEu),
		};

		Assert.True(MuiNativeNewAmigaGuideCodec.TryWrite(ref platform,
			address, value));
		Assert.True(MuiNativeNewAmigaGuideCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(value.Lock, decoded.Lock);
		Assert.Equal(value.Name, decoded.Name);
		Assert.Equal(value.Screen, decoded.Screen);
		Assert.Equal(value.PublicScreen, decoded.PublicScreen);
		Assert.Equal(value.HostPort, decoded.HostPort);
		Assert.Equal(value.ClientPort, decoded.ClientPort);
		Assert.Equal(value.BaseName, decoded.BaseName);
		Assert.Equal(value.Flags, decoded.Flags);
		Assert.Equal(value.Context, decoded.Context);
		Assert.Equal(value.Node, decoded.Node);
		Assert.Equal(value.Line, decoded.Line);
		Assert.Equal(value.Extensions, decoded.Extensions);
		Assert.Equal(value.Client, decoded.Client);
	}

	[Fact]
	public void NewAmigaGuideCodecRejectsTruncatedGuestRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x30FF0);

		Assert.False(MuiNativeNewAmigaGuideCodec.TryRead(ref platform,
			address, out _));
		Assert.False(MuiNativeNewAmigaGuideCodec.TryWrite(ref platform,
			address, default));
	}
}
