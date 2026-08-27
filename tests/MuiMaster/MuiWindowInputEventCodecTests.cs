using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiWindowInputEventCodecTests
{
	[Fact]
	public void WindowInputEventUsesNamedMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x17A0);
		var value = new InputEvent
		{
			NextEvent = APTR.FromPointer(0x1800),
			Class = InputEventClass.RawKey,
			SubClass = InputEventSubClass.Compatible,
			Code = 0x44,
			Qualifier = InputEventQualifier.LeftShift,
			Position = -12,
			TimeStamp = new TimeVal { Seconds = 2, Microseconds = 3 },
		};
		Assert.True(MuiWindowInputEventCodec.Write(ref platform, address, value));
		Assert.True(MuiWindowInputEventMemoryCodec.TryGetAddress(ref platform,
			address, MuiWindowInputEventField.Code, out var code) && code.Raw ==
			0x17A6u);
		Assert.True(MuiWindowInputEventMemoryCodec.TryReadUInt32(ref platform,
			address, MuiWindowInputEventField.Position, out var position) &&
			position == unchecked((uint)-12));
		Assert.True(MuiWindowInputEventMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiWindowInputEventField.Position, 24));
		Assert.True(MuiWindowInputEventCodec.TryRead(ref platform, address,
			out var decoded) && decoded.Position == 24 && decoded.Code == 0x44);
		Assert.False(MuiWindowInputEventMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(uint.MaxValue - 8),
			MuiWindowInputEventField.Microseconds, out _));
		Assert.False(MuiWindowInputEventMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiWindowInputEventField.Class, out _));
	}
}
