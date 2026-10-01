/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNotifyPacketStructAdapterTests
{
	[Fact]
	public void NotifyPacketFieldAdapterUsesAllNamedPacketRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var packet = APTR.FromPointer(0x2400);
		Assert.True(MuiNotifyMessageCodec.Write(ref platform, packet,
			new MuiNotifyMessage
			{
				MethodId = MuiNotifyCore.NotifyMethod,
				TriggerAttribute = 0x80420020,
				TriggerValue = 11,
				Destination = 0x3000,
				FollowCount = 2,
			}));
		Assert.True(MuiNotifyPacketFieldMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyPacketKind.Notify,
			MuiNotifyPacketField.TriggerValue, 17));
		Assert.True(MuiNotifyPacketFieldMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyPacketKind.Notify,
			MuiNotifyPacketField.FollowCount, 3));
		Assert.True(MuiNotifyPacketFieldMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyPacketKind.Notify,
			MuiNotifyPacketField.Destination, out var destination));
		Assert.Equal(0x3000u, destination);
		Assert.True(MuiNotifyPacketFieldMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyPacketKind.Notify,
			MuiNotifyPacketField.TriggerValue, out var triggerValue));
		Assert.Equal(17u, triggerValue);

		Assert.True(MuiKillNotifyMessageCodec.Write(ref platform, packet,
			new MuiKillNotifyMessage
			{
				MethodId = MuiNotifyCore.KillNotifyMethod,
				TriggerAttribute = 0x8042AAAA,
			}));
		Assert.True(MuiNotifyPacketFieldMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyPacketKind.KillNotify,
			MuiNotifyPacketField.TriggerAttribute, 0x8042BBBB));
		Assert.True(MuiNotifyPacketFieldMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyPacketKind.KillNotify,
			MuiNotifyPacketField.TriggerAttribute, out var attribute));
		Assert.Equal(0x8042BBBBu, attribute);

		Assert.True(MuiKillNotifyObjectMessageCodec.Write(ref platform, packet,
			new MuiKillNotifyObjectMessage
			{
				MethodId = MuiNotifyCore.KillNotifyObjectMethod,
				TriggerAttribute = 0x8042AAAA,
				Destination = 0x3100,
			}));
		Assert.True(MuiNotifyPacketFieldMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyPacketKind.KillNotifyObject,
			MuiNotifyPacketField.Destination, 0x3200));
		Assert.True(MuiNotifyPacketFieldMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyPacketKind.KillNotifyObject,
			MuiNotifyPacketField.Destination, out destination));
		Assert.Equal(0x3200u, destination);

		Assert.True(MuiSetAttributeMessageCodec.Write(ref platform, packet,
			new MuiSetAttributeMessage
			{
				MethodId = MuiNotifyCore.SetMethod,
				Attribute = 0x8042AAAA,
				Value = 7,
			}));
		Assert.True(MuiNotifyPacketFieldMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyPacketKind.Set,
			MuiNotifyPacketField.Value, 8));
		Assert.True(MuiNotifyPacketFieldMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyPacketKind.Set,
			MuiNotifyPacketField.Value, out var value));
		Assert.Equal(8u, value);

		Assert.True(MuiMultiSetMessageCodec.Write(ref platform, packet,
			new MuiMultiSetMessage
			{
				MethodId = MuiNotifyCore.MultiSetMethod,
				Attribute = 0x8042AAAA,
				Value = 9,
				FirstObject = 0x3300,
			}));
		Assert.True(MuiNotifyPacketFieldMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyPacketKind.MultiSet,
			MuiNotifyPacketField.FirstObject, 0x3400));
		Assert.True(MuiNotifyPacketFieldMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyPacketKind.MultiSet,
			MuiNotifyPacketField.FirstObject, out var firstObject));
		Assert.Equal(0x3400u, firstObject);

		Assert.True(MuiFindObjectMessageCodec.Write(ref platform, packet,
			new MuiFindObjectMessage
			{
				MethodId = MuiNotifyCore.FindObjectMethod,
				FindObject = 0x3500,
			}));
		Assert.True(MuiNotifyPacketFieldMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyPacketKind.FindObject,
			MuiNotifyPacketField.FindObject, 0x3600));
		Assert.True(MuiNotifyPacketFieldMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyPacketKind.FindObject,
			MuiNotifyPacketField.FindObject, out var findObject));
		Assert.Equal(0x3600u, findObject);
	}

	[Fact]
	public void NotifyPacketFieldAdapterRejectsMismatchedFieldsAndBadRanges()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var packet = APTR.FromPointer(0x2400);
		Assert.True(MuiSetAttributeMessageCodec.Write(ref platform, packet,
			new MuiSetAttributeMessage
			{
				MethodId = MuiNotifyCore.SetMethod,
				Attribute = 0x8042AAAA,
				Value = 7,
			}));
		Assert.False(MuiNotifyPacketFieldMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyPacketKind.Set,
			MuiNotifyPacketField.Destination, out _));
		Assert.False(MuiNotifyPacketFieldMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyPacketKind.Set,
			MuiNotifyPacketField.FollowCount, 1));
		Assert.False(MuiNotifyPacketFieldMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x20FFC), MuiNotifyPacketKind.Notify,
			MuiNotifyPacketField.MethodId, out _));
		Assert.False(MuiNotifyPacketFieldMemoryCodec.TryWriteUInt32(ref platform,
			APTR.Null, MuiNotifyPacketKind.MultiSet,
			MuiNotifyPacketField.Value, 1));
		Assert.False(MuiNotifyPacketFieldMemoryCodec.TryReadUInt32(ref platform,
			packet, (MuiNotifyPacketKind)255,
			MuiNotifyPacketField.MethodId, out _));
		Assert.False(MuiNotifyPacketFieldMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyPacketKind.Notify,
			(MuiNotifyPacketField)255, out _));
	}
}
