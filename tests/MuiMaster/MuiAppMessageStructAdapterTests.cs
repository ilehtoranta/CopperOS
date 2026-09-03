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

	[Fact]
	public void AppMessageFieldAccessUsesCompleteNamedRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var nodeAddress = APTR.FromPointer(0x3400);
		var node = default(MuiAppMessageNodeState);
		node.Successor = APTR.FromPointer(0x3500);
		node.Predecessor = APTR.FromPointer(0x3600);
		node.Type = 7;
		node.Priority = -3;
		node.Name = APTR.FromPointer(0x3700);
		node.ReplyPort = APTR.FromPointer(0x3800);
		node.Length = 12;
		Assert.True(MuiAppMessageNodeCodec.WriteStructural(ref platform,
			nodeAddress, node));
		Assert.True(MuiAppMessageNodeMemoryCodec.TryWriteUInt8(ref platform,
			nodeAddress, MuiAppMessageNodeField.Type, 9));
		Assert.True(MuiAppMessageNodeMemoryCodec.TryWriteUInt32(ref platform,
			nodeAddress, MuiAppMessageNodeField.Successor, 0x3900));
		Assert.True(MuiAppMessageNodeMemoryCodec.TryReadUInt8(ref platform,
			nodeAddress, MuiAppMessageNodeField.Priority, out var priority));
		Assert.Equal(unchecked((byte)-3), priority);
		Assert.True(MuiAppMessageNodeCodec.TryReadStructural(ref platform,
			nodeAddress, out var nodeAfter));
		Assert.Equal(APTR.FromPointer(0x3900), nodeAfter.Successor);
		Assert.Equal(node.Predecessor, nodeAfter.Predecessor);
		Assert.Equal((byte)9, nodeAfter.Type);
		Assert.Equal(node.Priority, nodeAfter.Priority);
		Assert.False(MuiAppMessageNodeMemoryCodec.TryReadUInt32(ref platform,
			nodeAddress, MuiAppMessageNodeField.Type, out _));

		var recordAddress = APTR.FromPointer(0x3A00);
		var record = default(MuiAppMessageRecord);
		record.Message = node;
		record.Type = 0x1234;
		record.NumberOfArguments = -2;
		record.ArgumentList = APTR.FromPointer(0x3B00);
		record.MouseX = -4;
		record.MouseY = 12;
		record.Reserved7 = 0xDEADBEEFu;
		Assert.True(MuiAppMessageRecordCodec.WriteStructural(ref platform,
			recordAddress, record));
		Assert.True(MuiAppMessageRecordMemoryCodec.TryWriteUInt16(ref platform,
			recordAddress, MuiAppMessageField.MouseX, unchecked((ushort)-20)));
		Assert.True(MuiAppMessageRecordMemoryCodec.TryWriteUInt32(ref platform,
			recordAddress, MuiAppMessageField.Reserved7, 0x01020304u));
		Assert.True(MuiAppMessageRecordMemoryCodec.TryReadUInt32(ref platform,
			recordAddress, MuiAppMessageField.NumberOfArguments, out var argumentCount));
		Assert.Equal(unchecked((uint)-2), argumentCount);
		Assert.True(MuiAppMessageRecordCodec.TryReadStructural(ref platform,
			recordAddress, out var recordAfter));
		Assert.Equal((short)-20, recordAfter.MouseX);
		Assert.Equal(record.MouseY, recordAfter.MouseY);
		Assert.Equal(0x01020304u, recordAfter.Reserved7);
		Assert.Equal(node.Successor, recordAfter.Message.Successor);
		Assert.False(MuiAppMessageRecordMemoryCodec.TryReadUInt16(ref platform,
			recordAddress, MuiAppMessageField.Reserved0, out _));
		Assert.False(MuiAppMessageRecordCodec.TryReadStructural(ref platform,
			APTR.FromPointer(0x40000), out _));
	}

	[Fact]
	public void WorkbenchArgumentFieldAccessUsesCompleteNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3C00);
		var expected = new MuiWorkbenchArgumentRecord
		{
			Lock = BPTR.FromRaw(0x00000120),
			Name = STRPTR.FromPointer(0x00003D00),
		};
		Assert.True(MuiWorkbenchArgumentRecordCodec.WriteStructural(ref platform,
			address, expected));
		Assert.True(MuiWorkbenchArgumentRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiWorkbenchArgumentField.Name, 0x00003E00));
		Assert.True(MuiWorkbenchArgumentRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiWorkbenchArgumentField.Lock, out var lockValue));
		Assert.Equal(expected.Lock.Raw, lockValue);
		Assert.True(MuiWorkbenchArgumentRecordCodec.TryReadStructural(ref platform,
			address, out var actual));
		Assert.Equal(expected.Lock, actual.Lock);
		Assert.Equal(STRPTR.FromPointer(0x00003E00), actual.Name);
		Assert.False(MuiWorkbenchArgumentRecordMemoryCodec.TryReadUInt32(ref platform,
			address, (MuiWorkbenchArgumentField)255, out _));
		Assert.False(MuiWorkbenchArgumentRecordCodec.TryReadStructural(ref platform,
			APTR.FromPointer(0x40000), out _));
	}
}
