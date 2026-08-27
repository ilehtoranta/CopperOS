using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAppMessageStructAdapterTests
{
	[Fact]
	public void AppMessageRecordUsesDedicatedMixedWidthStructAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3000);
		var value = new MuiAppMessageRecord
		{
			Message = new MuiAppMessageNodeState
			{
				Type = 8,
				Priority = -2,
				Name = APTR.FromPointer(0x3500),
				ReplyPort = APTR.FromPointer(0x3600),
				Length = 86,
			},
			Type = 0x1234,
			UserData = 0x55,
			Id = 0x66,
			NumberOfArguments = -2,
			ArgumentList = APTR.FromPointer(0x3700),
			Version = 1,
			Class = 2,
			MouseX = -4,
			MouseY = 12,
			Seconds = 3,
			Micros = 4,
			Reserved7 = 0xDEADBEEFu,
		};

		Assert.True(MuiAppMessageRecordCodec.Write(ref platform, address, value));
		Assert.True(MuiAppMessageRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiAppMessageField.MouseX, out var mouseXField,
			out var mouseXSize));
		Assert.Equal(APTR.FromPointer(0x302A), mouseXField);
		Assert.Equal(2u, mouseXSize);
		Assert.True(MuiAppMessageRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiAppMessageField.Seconds, out var secondsField,
			out var secondsSize));
		Assert.Equal(APTR.FromPointer(0x302E), secondsField);
		Assert.Equal(4u, secondsSize);
		Assert.True(MuiAppMessageRecordMemoryCodec.TryReadUInt16(ref platform,
			address, MuiAppMessageField.MouseX, out var mouseX));
		Assert.Equal(-4, unchecked((short)mouseX));
		Assert.True(MuiAppMessageRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiAppMessageField.Reserved7, 0x01020304u));
		Assert.True(MuiAppMessageRecordCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.Type, decoded.Type);
		Assert.Equal(value.NumberOfArguments, decoded.NumberOfArguments);
		Assert.Equal(value.MouseY, decoded.MouseY);
		Assert.Equal(0x01020304u, decoded.Reserved7);
		Assert.False(MuiAppMessageRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x30FF0), MuiAppMessageField.Type, out _, out _));
		Assert.False(MuiAppMessageRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiAppMessageField.UserData, out _, out _));
	}
}
