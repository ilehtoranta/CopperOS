/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNotifyWriteStructAdapterTests
{
	[Fact]
	public void NotifyWriteFieldAdapterUsesEachNamedPacketRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var packet = APTR.FromPointer(0x2400);
		Assert.True(MuiNotifyWriteCore.WriteLongRecord(ref platform, packet,
			0xAABBCCDD, APTR.FromPointer(0x3000)));
		Assert.True(MuiNotifyWritePacketMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyWritePacketKind.WriteLong,
			MuiNotifyWritePacketField.Value, MuiWriteLongMessage.Size, 17));
		Assert.True(MuiNotifyWritePacketMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyWritePacketKind.WriteLong,
			MuiNotifyWritePacketField.Memory, MuiWriteLongMessage.Size, 0x3100));
		Assert.True(MuiNotifyWritePacketMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyWritePacketKind.WriteLong,
			MuiNotifyWritePacketField.MethodId, MuiWriteLongMessage.Size,
			out var method));
		Assert.Equal(MuiNotifyWriteCore.WriteLongMethod, method);
		Assert.True(MuiNotifyWritePacketMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyWritePacketKind.WriteLong,
			MuiNotifyWritePacketField.Value, MuiWriteLongMessage.Size,
			out var value));
		Assert.Equal(17u, value);
		Assert.True(MuiNotifyWritePacketMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyWritePacketKind.WriteLong,
			MuiNotifyWritePacketField.Memory, MuiWriteLongMessage.Size,
			out var memory));
		Assert.Equal(0x3100u, memory);

		Assert.True(MuiNotifyWriteCore.WriteStringRecord(ref platform, packet,
			APTR.FromPointer(0x3200), APTR.FromPointer(0x3300)));
		Assert.True(MuiNotifyWritePacketMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyWritePacketKind.WriteString,
			MuiNotifyWritePacketField.String, MuiWriteStringMessage.Size, 0x3400));
		Assert.True(MuiNotifyWritePacketMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyWritePacketKind.WriteString,
			MuiNotifyWritePacketField.Memory, MuiWriteStringMessage.Size, 0x3500));
		Assert.True(MuiNotifyWritePacketMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyWritePacketKind.WriteString,
			MuiNotifyWritePacketField.String, MuiWriteStringMessage.Size,
			out var source));
		Assert.Equal(0x3400u, source);
		Assert.True(MuiNotifyWritePacketMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyWritePacketKind.WriteString,
			MuiNotifyWritePacketField.Memory, MuiWriteStringMessage.Size,
			out memory));
		Assert.Equal(0x3500u, memory);
	}

	[Fact]
	public void NotifyWriteFieldAdapterKeepsMethodHeaderBoundedAndRejectsBadRanges()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var header = APTR.FromPointer(0x2600);
		const uint replacement = 0xF1234567u;
		Assert.True(MuiNotifyWritePacketMemoryCodec.TryWriteUInt32(ref platform,
			header, MuiNotifyWritePacketKind.WriteLong,
			MuiNotifyWritePacketField.MethodId, MuiNotifyWriteMethodMessage.Size,
			replacement));
		Assert.True(MuiNotifyWritePacketMemoryCodec.TryReadUInt32(ref platform,
			header, MuiNotifyWritePacketKind.WriteString,
			MuiNotifyWritePacketField.MethodId, MuiNotifyWriteMethodMessage.Size,
			out var method));
		Assert.Equal(replacement, method);
		Assert.False(MuiNotifyWritePacketMemoryCodec.TryReadUInt32(ref platform,
			header, MuiNotifyWritePacketKind.WriteLong,
			MuiNotifyWritePacketField.Value, MuiNotifyWriteMethodMessage.Size,
			out _));
		Assert.False(MuiNotifyWritePacketMemoryCodec.TryWriteUInt32(ref platform,
			header, MuiNotifyWritePacketKind.WriteString,
			MuiNotifyWritePacketField.Memory, MuiNotifyWriteMethodMessage.Size, 1));
		Assert.False(MuiNotifyWritePacketMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x20FFC), MuiNotifyWritePacketKind.WriteLong,
			MuiNotifyWritePacketField.Memory, MuiWriteLongMessage.Size, out _));
		Assert.False(MuiNotifyWritePacketMemoryCodec.TryWriteUInt32(ref platform,
			APTR.Null, MuiNotifyWritePacketKind.WriteString,
			MuiNotifyWritePacketField.String, MuiWriteStringMessage.Size, 1));
		Assert.False(MuiNotifyWritePacketMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x2400), (MuiNotifyWritePacketKind)255,
			MuiNotifyWritePacketField.MethodId, MuiWriteLongMessage.Size, out _));
		Assert.False(MuiNotifyWritePacketMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x2400), MuiNotifyWritePacketKind.WriteLong,
			(MuiNotifyWritePacketField)255, MuiWriteLongMessage.Size, out _));
	}

	[Fact]
	public void NotifyWriteFieldCursorTraversesNamedRecordsAndPartialHeaders()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var packet = APTR.FromPointer(0x1200);
		var cursor = default(MuiNotifyWritePacketFieldCursor);
		cursor.Message = packet;
		cursor.Packet = MuiNotifyWritePacketKind.WriteLong;
		cursor.Field = MuiNotifyWritePacketField.Value;
		Assert.True(MuiNotifyWritePacketMemoryCodec.TryGetAddress(ref platform,
			cursor, MuiWriteLongMessage.Size, out var address, out var fieldSize));
		Assert.Equal(packet.Raw + 4, address.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiNotifyWritePacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address, out fieldSize));
		Assert.Equal(packet.Raw + 4, address.Raw);
		Assert.Equal(4u, fieldSize);

		cursor.Packet = MuiNotifyWritePacketKind.WriteString;
		cursor.Field = MuiNotifyWritePacketField.Memory;
		Assert.True(MuiNotifyWritePacketMemoryCodec.TryGetAddress(ref platform,
			cursor, MuiWriteStringMessage.Size, out address, out fieldSize));
		Assert.Equal(packet.Raw + 8, address.Raw);
		Assert.Equal(4u, fieldSize);

		var methodOnly = APTR.FromPointer(0x1300);
		cursor.Message = methodOnly;
		cursor.Field = MuiNotifyWritePacketField.MethodId;
		Assert.True(MuiNotifyWritePacketMemoryCodec.TryGetAddress(ref platform,
			cursor, MuiNotifyWriteMethodMessage.Size, out address, out fieldSize));
		Assert.Equal(methodOnly.Raw, address.Raw);
		Assert.Equal(4u, fieldSize);
		cursor.Field = MuiNotifyWritePacketField.Memory;
		Assert.False(MuiNotifyWritePacketMemoryCodec.TryGetAddress(ref platform,
			cursor, MuiNotifyWriteMethodMessage.Size, out _, out _));

		cursor.Message = APTR.FromPointer(0x20FFC);
		cursor.Packet = MuiNotifyWritePacketKind.WriteLong;
		cursor.Field = MuiNotifyWritePacketField.Memory;
		Assert.False(MuiNotifyWritePacketMemoryCodec.TryGetAddress(ref platform,
			cursor, MuiWriteLongMessage.Size, out _, out _));
		cursor.Message = APTR.Null;
		Assert.False(MuiNotifyWritePacketMemoryCodec.TryGetAddress(ref platform,
			cursor, MuiWriteLongMessage.Size, out _, out _));
	}
}
