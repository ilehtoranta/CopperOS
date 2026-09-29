/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNotifyUserDataStructAdapterTests
{
	[Fact]
	public void NotifyUserDataFieldAdapterUsesNamedFindGetAndSetRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var packet = APTR.FromPointer(0x2400);
		Assert.True(MuiFindUDataMessageCodec.Write(ref platform, packet,
			new MuiFindUDataMessage
			{
				MethodId = MuiNotifyUserDataCore.FindUData,
				UserData = 0x11,
			}));
		Assert.True(MuiNotifyUserDataPacketMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyUserDataPacketKind.Find,
			MuiNotifyUserDataPacketField.UserData, 0x22));
		Assert.True(MuiNotifyUserDataPacketMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyUserDataPacketKind.Find,
			MuiNotifyUserDataPacketField.MethodId, out var method));
		Assert.Equal(MuiNotifyUserDataCore.FindUData, method);
		Assert.True(MuiNotifyUserDataPacketMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyUserDataPacketKind.Find,
			MuiNotifyUserDataPacketField.UserData, out var userData));
		Assert.Equal(0x22u, userData);

		Assert.True(MuiGetUDataMessageCodec.Write(ref platform, packet,
			new MuiGetUDataMessage
			{
				MethodId = MuiNotifyUserDataCore.GetUData,
				UserData = 0x33,
				Attribute = 0x8042AAAA,
				Storage = 0x3400,
			}));
		Assert.True(MuiNotifyUserDataPacketMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyUserDataPacketKind.Get,
			MuiNotifyUserDataPacketField.UserData, 0x44));
		Assert.True(MuiNotifyUserDataPacketMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyUserDataPacketKind.Get,
			MuiNotifyUserDataPacketField.Storage, 0x3500));
		Assert.True(MuiNotifyUserDataPacketMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyUserDataPacketKind.Get,
			MuiNotifyUserDataPacketField.Attribute, out var attribute));
		Assert.Equal(0x8042AAAAu, attribute);
		Assert.True(MuiNotifyUserDataPacketMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyUserDataPacketKind.Get,
			MuiNotifyUserDataPacketField.UserData, out userData));
		Assert.Equal(0x44u, userData);
		Assert.True(MuiNotifyUserDataPacketMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyUserDataPacketKind.Get,
			MuiNotifyUserDataPacketField.Storage, out var storage));
		Assert.Equal(0x3500u, storage);

		Assert.True(MuiSetUDataMessageCodec.Write(ref platform, packet,
			new MuiSetUDataMessage
			{
				MethodId = MuiNotifyUserDataCore.SetUData,
				UserData = 0x55,
				Attribute = 0x8042BBBB,
				Value = 7,
			}));
		Assert.True(MuiNotifyUserDataPacketMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyUserDataPacketKind.Set,
			MuiNotifyUserDataPacketField.Value, 8));
		Assert.True(MuiNotifyUserDataPacketMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyUserDataPacketKind.Set,
			MuiNotifyUserDataPacketField.Attribute, out attribute));
		Assert.Equal(0x8042BBBBu, attribute);
		Assert.True(MuiNotifyUserDataPacketMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyUserDataPacketKind.Set,
			MuiNotifyUserDataPacketField.Value, out var value));
		Assert.Equal(8u, value);
	}

	[Fact]
	public void NotifyUserDataFieldAdapterRejectsMismatchedFieldsAndBadRanges()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var packet = APTR.FromPointer(0x2400);
		Assert.True(MuiFindUDataMessageCodec.Write(ref platform, packet,
			new MuiFindUDataMessage
			{
				MethodId = MuiNotifyUserDataCore.FindUData,
				UserData = 1,
			}));
		Assert.False(MuiNotifyUserDataPacketMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyUserDataPacketKind.Find,
			MuiNotifyUserDataPacketField.Attribute, out _));
		Assert.False(MuiNotifyUserDataPacketMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyUserDataPacketKind.Find,
			MuiNotifyUserDataPacketField.Storage, 1));
		Assert.False(MuiNotifyUserDataPacketMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x20FFC), MuiNotifyUserDataPacketKind.Find,
			MuiNotifyUserDataPacketField.UserData, out _));
		Assert.False(MuiNotifyUserDataPacketMemoryCodec.TryWriteUInt32(ref platform,
			APTR.Null, MuiNotifyUserDataPacketKind.Set,
			MuiNotifyUserDataPacketField.Value, 1));
		Assert.False(MuiNotifyUserDataPacketMemoryCodec.TryReadUInt32(ref platform,
			packet, (MuiNotifyUserDataPacketKind)255,
			MuiNotifyUserDataPacketField.MethodId, out _));
		Assert.False(MuiNotifyUserDataPacketMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyUserDataPacketKind.Get,
			(MuiNotifyUserDataPacketField)255, out _));
	}

	[Fact]
	public void NotifyUserDataFieldCursorTraversesEachNamedPacket()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var packet = APTR.FromPointer(0x1200);
		var cursor = default(MuiNotifyUserDataPacketFieldCursor);
		cursor.Message = packet;
		cursor.Packet = MuiNotifyUserDataPacketKind.Find;
		cursor.Field = MuiNotifyUserDataPacketField.UserData;
		Assert.True(MuiNotifyUserDataPacketMemoryCodec.TryGetAddress(ref platform,
			cursor, out var address, out var fieldSize));
		Assert.Equal(packet.Raw + 4, address.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiNotifyUserDataPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address, out fieldSize));
		Assert.Equal(packet.Raw + 4, address.Raw);
		Assert.Equal(4u, fieldSize);

		cursor.Packet = MuiNotifyUserDataPacketKind.Get;
		cursor.Field = MuiNotifyUserDataPacketField.Storage;
		Assert.True(MuiNotifyUserDataPacketMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize));
		Assert.Equal(packet.Raw + 12, address.Raw);
		Assert.Equal(4u, fieldSize);
		cursor.Packet = MuiNotifyUserDataPacketKind.Set;
		cursor.Field = MuiNotifyUserDataPacketField.Value;
		Assert.True(MuiNotifyUserDataPacketMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize));
		Assert.Equal(packet.Raw + 12, address.Raw);
		Assert.Equal(4u, fieldSize);

		cursor.Packet = MuiNotifyUserDataPacketKind.Find;
		cursor.Field = MuiNotifyUserDataPacketField.Attribute;
		Assert.False(MuiNotifyUserDataPacketMemoryCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		cursor.Field = (MuiNotifyUserDataPacketField)255;
		Assert.False(MuiNotifyUserDataPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _, out _));
		cursor.Message = APTR.FromPointer(0x20FFC);
		cursor.Field = MuiNotifyUserDataPacketField.UserData;
		Assert.False(MuiNotifyUserDataPacketMemoryCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		cursor.Message = APTR.Null;
		Assert.False(MuiNotifyUserDataPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _, out _));
	}
}
