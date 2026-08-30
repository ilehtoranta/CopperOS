using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGuestStructCursorTests
{
	[Fact]
	public void AppMessageCodecUsesNamedStructOrderAndFailsClosedAtBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2E00);
		var expected = new MuiAppMessageRecord
		{
			Message = new MuiAppMessageNodeState
			{
				Successor = APTR.FromPointer(0x2F00),
				Predecessor = APTR.FromPointer(0x2F20),
				Type = 3,
				Priority = -7,
				Name = APTR.FromPointer(0x2F40),
				ReplyPort = APTR.FromPointer(0x2F60),
				Length = 0x1234,
			},
			Type = 9,
			UserData = 0x11223344,
			Id = 0x55667788,
			NumberOfArguments = 2,
			ArgumentList = APTR.FromPointer(0x2800),
			Version = 1,
			Class = 0x2345,
			MouseX = -12,
			MouseY = 34,
			Seconds = 0x01020304,
			Micros = 0x05060708,
			Reserved0 = 0x10,
			Reserved1 = 0x11,
			Reserved2 = 0x12,
			Reserved3 = 0x13,
			Reserved4 = 0x14,
			Reserved5 = 0x15,
			Reserved6 = 0x16,
			Reserved7 = 0x17,
		};

		Assert.True(MuiAppMessageRecordCodec.Write(ref platform, address, expected));
		Assert.True(MuiAppMessageRecordCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Message.Successor, actual.Message.Successor);
		Assert.Equal(expected.Message.Priority, actual.Message.Priority);
		Assert.Equal(expected.Message.Length, actual.Message.Length);
		Assert.Equal(expected.Type, actual.Type);
		Assert.Equal(expected.UserData, actual.UserData);
		Assert.Equal(expected.NumberOfArguments, actual.NumberOfArguments);
		Assert.Equal(expected.ArgumentList, actual.ArgumentList);
		Assert.Equal(expected.MouseX, actual.MouseX);
		Assert.Equal(expected.MouseY, actual.MouseY);
		Assert.Equal(expected.Reserved7, actual.Reserved7);

		Assert.False(MuiAppMessageRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x30FFC), out _));
		Assert.False(MuiGuestStructCursor.TryCreate(ref platform,
			APTR.FromPointer(0x30FFC), MuiAppMessageRecord.Size, out _));
	}
}
