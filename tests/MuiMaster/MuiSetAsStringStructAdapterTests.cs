/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiSetAsStringStructAdapterTests
{
	[Fact]
	public void SetAsStringFieldAdapterUsesNamedRecordAndPreservesSiblings()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var packet = APTR.FromPointer(0x2400);
		Assert.True(MuiSetAsStringMessageCodec.WriteRecord(ref platform, packet,
			new MuiSetAsStringMessage
			{
				MethodId = MuiNotifySetAsStringCore.Method,
				Attribute = 0x8042AAAA,
				Format = APTR.FromPointer(0x3000),
				Value = 17,
			}));
		Assert.True(MuiSetAsStringMessageMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiSetAsStringPacketField.Attribute, MuiSetAsStringMessage.Size,
			0x8042BBBB));
		Assert.True(MuiSetAsStringMessageMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiSetAsStringPacketField.Format, MuiSetAsStringMessage.Size,
			0x3100));
		Assert.True(MuiSetAsStringMessageMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiSetAsStringPacketField.Value, MuiSetAsStringMessage.Size,
			42));
		Assert.True(MuiSetAsStringMessageMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiSetAsStringPacketField.MethodId, MuiSetAsStringMessage.Size,
			out var method));
		Assert.Equal(MuiNotifySetAsStringCore.Method, method);
		Assert.True(MuiSetAsStringMessageMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiSetAsStringPacketField.Attribute,
			MuiSetAsStringMessage.Size, out var attribute));
		Assert.Equal(0x8042BBBBu, attribute);
		Assert.True(MuiSetAsStringMessageMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiSetAsStringPacketField.Format, MuiSetAsStringMessage.Size,
			out var format));
		Assert.Equal(0x3100u, format);
		Assert.True(MuiSetAsStringMessageMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiSetAsStringPacketField.Value, MuiSetAsStringMessage.Size,
			out var value));
		Assert.Equal(42u, value);
		var cursor = new MuiSetAsStringPacketFieldCursor
		{
			Message = packet,
			Field = MuiSetAsStringPacketField.Format,
		};
		Assert.True(MuiSetAsStringPacketFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var formatAddress, out var formatSize));
		Assert.Equal(packet.Raw + 8u, formatAddress.Raw);
		Assert.Equal(4u, formatSize);
	}

	[Fact]
	public void SetAsStringFieldAdapterKeepsMethodHeaderBoundedAndRejectsBadRanges()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var header = APTR.FromPointer(0x2600);
		const uint replacement = 0xF1234567u;
		Assert.True(MuiSetAsStringMessageMemoryCodec.TryWriteUInt32(ref platform,
			header, MuiSetAsStringPacketField.MethodId,
			MuiSetAsStringMethodMessage.Size, replacement));
		Assert.True(MuiSetAsStringMessageMemoryCodec.TryReadUInt32(ref platform,
			header, MuiSetAsStringPacketField.MethodId,
			MuiSetAsStringMethodMessage.Size, out var method));
		Assert.Equal(replacement, method);
		Assert.False(MuiSetAsStringMessageMemoryCodec.TryReadUInt32(ref platform,
			header, MuiSetAsStringPacketField.Attribute,
			MuiSetAsStringMethodMessage.Size, out _));
		Assert.False(MuiSetAsStringMessageMemoryCodec.TryWriteUInt32(ref platform,
			header, MuiSetAsStringPacketField.Value,
			MuiSetAsStringMethodMessage.Size, 1));
		Assert.False(MuiSetAsStringMessageMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x20FFC), MuiSetAsStringPacketField.Value,
			MuiSetAsStringMessage.Size, out _));
		Assert.False(MuiSetAsStringMessageMemoryCodec.TryWriteUInt32(ref platform,
			APTR.Null, MuiSetAsStringPacketField.Attribute,
			MuiSetAsStringMessage.Size, 1));
		Assert.False(MuiSetAsStringMessageMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x2400), (MuiSetAsStringPacketField)255,
			MuiSetAsStringMessage.Size, out _));
		var cursor = new MuiSetAsStringPacketFieldCursor
		{
			Message = header,
			Field = MuiSetAsStringPacketField.Attribute,
		};
		Assert.False(MuiSetAsStringMessageMemoryCodec.TryGetAddress(ref platform,
			cursor, MuiSetAsStringMethodMessage.Size, out _, out _));
		cursor.Field = MuiSetAsStringPacketField.MethodId;
		Assert.True(MuiSetAsStringMessageMemoryCodec.TryGetAddress(ref platform,
			cursor, MuiSetAsStringMethodMessage.Size, out var methodAddress,
			out var methodSize));
		Assert.Equal(header.Raw, methodAddress.Raw);
		Assert.Equal(4u, methodSize);
	}
}
