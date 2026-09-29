using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiInputExpressionCodecTests
{
	[Fact]
	public void InputExpressionRoundTripsUsingNamedFields()
	{
		var address = APTR.FromPointer(0x1A00);
		var memory = new MuiHeadlessTestPlatform(address.Raw, 32, 0, address);
		var expected = new InputXpression
		{
			Version = MuiInputExpressionMemoryCodec.Version,
			Class = (byte)InputEventClass.RawKey,
			Code = 0x0033,
			CodeMask = 0x007F,
			Qualifier = (ushort)InputEventQualifier.RightCommand,
			QualifierMask = (ushort)InputEventQualifier.RightCommand,
			QualifierSame = 0,
		};

		Assert.True(MuiInputExpressionMemoryCodec.Write(ref memory, address,
			expected));
		Assert.True(MuiInputExpressionMemoryCodec.TryRead(ref memory, address,
			out var actual));
		Assert.Equal(expected, actual);
	}

	[Fact]
	public void InputExpressionRejectsUnknownVersionAndTruncation()
	{
		var address = APTR.FromPointer(0x1A00);
		var memory = new MuiHeadlessTestPlatform(address.Raw, 32, 0, address);
		var invalid = new InputXpression
		{
			Version = MuiInputExpressionMemoryCodec.Version + 1,
			Class = (byte)InputEventClass.RawKey,
		};
		Assert.False(MuiInputExpressionMemoryCodec.Write(ref memory, address,
			invalid));
		memory.WriteUInt8(address, 0, MuiInputExpressionMemoryCodec.Version);
		Assert.False(MuiInputExpressionMemoryCodec.TryRead(ref memory,
			APTR.FromPointer(address.Raw + InputXpression.Size - 1), out _));
	}
}
