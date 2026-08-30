using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNotifyPacketTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint Attribute = 0x80420020;
	private const uint EveryTime = 1233727793;
	private const uint NoNotifyAttribute = 0x804237F9;
	private const uint NoNotifyMethodAttribute = 0x80420A74;

	[Fact]
	public void NotifyMethodHeaderUsesNamedField()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiNotifyMethodMessageCodec.Write(ref platform, packet,
			new MuiNotifyMethodMessage { MethodId = MuiNotifyCore.NotifyMethod }));
		Assert.True(MuiNotifyPacketCodec.TryReadMethodId(ref platform, packet,
			out var header));
		Assert.Equal(MuiNotifyCore.NotifyMethod, header.MethodId);
		Assert.True(MuiNotifyPacketCodec.TryReadMethodIdValue(ref platform,
			packet, out var methodId));
		Assert.Equal(MuiNotifyCore.NotifyMethod, methodId);
		Assert.False(MuiNotifyPacketCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
		Assert.False(MuiNotifyMethodMessageCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
	}

	[Fact]
	public void NotifyTypedReadersUseNamedMethodHeader()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiNotifyMessageCodec.Write(ref platform, packet,
			new MuiNotifyMessage
			{
				MethodId = MuiNotifyCore.NotifyMethod,
				TriggerAttribute = Attribute,
				TriggerValue = EveryTime,
				Destination = 0x1300,
				FollowCount = 1,
			}));
		var request = default(MuiNotifyPacketCodec.PacketAddress);
		request.Address = packet;
		request.Method = MuiNotifyCore.NotifyMethod;

		Assert.True(MuiNotifyPacketCodec.TryReadNotify(ref platform,
			ref request, out var message));
		Assert.Equal(MuiNotifyCore.NotifyMethod, message.MethodId);
		Assert.Equal(Attribute, message.TriggerAttribute);
		Assert.Equal(EveryTime, message.TriggerValue);
		Assert.Equal(0x1300u, message.Destination);
		Assert.Equal(1u, message.FollowCount);
		Assert.True(MuiNotifyMessageCodec.TryRead(ref platform, packet,
			out var direct));
		Assert.Equal(message.Destination, direct.Destination);
		Assert.False(MuiNotifyMessageCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
	}

	[Fact]
	public void SetPacketWriterUsesNamedRecord()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		var value = new MuiSetAttributeMessage
		{
			MethodId = MuiNotifyCore.SetMethod,
			Attribute = Attribute,
			Value = 77,
		};

		Assert.True(MuiNotifyPacketCodec.TryWriteSet(ref platform, packet, value));
		var request = default(MuiNotifyPacketCodec.PacketAddress);
		request.Address = packet;
		request.Method = MuiNotifyCore.SetMethod;
		Assert.True(MuiNotifyPacketCodec.TryReadSet(ref platform, ref request,
			out var roundTrip));
		Assert.Equal(value.MethodId, roundTrip.MethodId);
		Assert.Equal(value.Attribute, roundTrip.Attribute);
		Assert.Equal(value.Value, roundTrip.Value);
		Assert.True(MuiSetAttributeMessageCodec.TryRead(ref platform, packet,
			out var direct));
		Assert.Equal(value.Attribute, direct.Attribute);
		Assert.Equal(value.Value, direct.Value);

		value.MethodId = MuiNotifyCore.NotifyMethod;
		Assert.False(MuiNotifyPacketCodec.TryWriteSet(ref platform, packet, value));
		Assert.False(MuiNotifyPacketCodec.TryWriteSet(ref platform,
			APTR.FromPointer(0x20FF8), value));
		Assert.False(MuiSetAttributeMessageCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
	}

	[Fact]
	public void KillNotifyAndFindObjectCodecsUseCompleteNamedRecords()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);

		var kill = new MuiKillNotifyMessage
		{
			MethodId = MuiNotifyCore.KillNotifyMethod,
			TriggerAttribute = Attribute,
		};
		Assert.True(MuiKillNotifyMessageCodec.Write(ref platform, packet, kill));
		Assert.True(MuiKillNotifyMessageCodec.TryRead(ref platform, packet,
			out var killRoundTrip));
		Assert.Equal(kill.MethodId, killRoundTrip.MethodId);
		Assert.Equal(kill.TriggerAttribute, killRoundTrip.TriggerAttribute);
		var killRequest = default(MuiNotifyPacketCodec.PacketAddress);
		killRequest.Address = packet;
		killRequest.Method = MuiNotifyCore.KillNotifyMethod;
		Assert.True(MuiNotifyPacketCodec.TryReadKillNotify(ref platform,
			ref killRequest, out var admittedKill));
		Assert.Equal(kill.TriggerAttribute, admittedKill.TriggerAttribute);

		var killObject = new MuiKillNotifyObjectMessage
		{
			MethodId = MuiNotifyCore.KillNotifyObjectMethod,
			TriggerAttribute = Attribute,
			Destination = 0x1300,
		};
		Assert.True(MuiKillNotifyObjectMessageCodec.Write(ref platform, packet,
			killObject));
		Assert.True(MuiKillNotifyObjectMessageCodec.TryRead(ref platform, packet,
			out var killObjectRoundTrip));
		Assert.Equal(killObject.MethodId, killObjectRoundTrip.MethodId);
		Assert.Equal(killObject.TriggerAttribute, killObjectRoundTrip.TriggerAttribute);
		Assert.Equal(killObject.Destination, killObjectRoundTrip.Destination);
		var killObjectRequest = default(MuiNotifyPacketCodec.PacketAddress);
		killObjectRequest.Address = packet;
		killObjectRequest.Method = MuiNotifyCore.KillNotifyObjectMethod;
		Assert.True(MuiNotifyPacketCodec.TryReadKillNotifyObject(ref platform,
			ref killObjectRequest, out var admittedKillObject));
		Assert.Equal(killObject.Destination, admittedKillObject.Destination);

		var find = new MuiFindObjectMessage
		{
			MethodId = MuiNotifyCore.FindObjectMethod,
			FindObject = 0x1400,
		};
		Assert.True(MuiFindObjectMessageCodec.Write(ref platform, packet, find));
		Assert.True(MuiFindObjectMessageCodec.TryRead(ref platform, packet,
			out var findRoundTrip));
		Assert.Equal(find.MethodId, findRoundTrip.MethodId);
		Assert.Equal(find.FindObject, findRoundTrip.FindObject);
		var findRequest = default(MuiNotifyPacketCodec.PacketAddress);
		findRequest.Address = packet;
		findRequest.Method = MuiNotifyCore.FindObjectMethod;
		Assert.True(MuiNotifyPacketCodec.TryReadFindObject(ref platform,
			ref findRequest, out var admittedFind));
		Assert.Equal(find.FindObject, admittedFind.FindObject);

		Assert.False(MuiKillNotifyMessageCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
		Assert.False(MuiKillNotifyObjectMessageCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
		Assert.False(MuiFindObjectMessageCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
		killRequest.Method = MuiNotifyCore.FindObjectMethod;
		Assert.False(MuiNotifyPacketCodec.TryReadKillNotify(ref platform,
			ref killRequest, out _));
	}

	[Fact]
	public void NotifyFollowParameterCodecUsesNamedValue()
	{
		var platform = CreatePlatform(out _);
		var slotAddress = APTR.FromPointer(0x1300);
		Assert.True(MuiNotifyFollowParameterSlotCodec.Write(ref platform,
			slotAddress, new MuiNotifyFollowParameterSlot { Value = EveryTime }));
		Assert.True(MuiNotifyFollowParameterSlotCodec.TryRead(ref platform,
			slotAddress, out var slot));
		Assert.Equal(EveryTime, slot.Value);

		slot.Value = 0xABCD;
		Assert.True(MuiNotifyFollowParameterSlotCodec.Write(ref platform,
			slotAddress, slot));
		Assert.True(MuiNotifyFollowParameterSlotCodec.TryRead(ref platform,
			slotAddress, out var updated));
		Assert.Equal(0xABCDu, updated.Value);
	}

	[Fact]
	public void NotifyFollowParameterVectorUsesNamedCursorBoundary()
	{
		var platform = CreatePlatform(out _);
		var cursor = new MuiNotifyFollowParameterVectorCursor
		{
			Base = APTR.FromPointer(0x1800),
			Index = 2,
		};

		Assert.True(MuiNotifyFollowParameterVectorCodec.TryGetEntry(
			ref platform, cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x1808), address);
		cursor.Base = APTR.FromPointer(0x20FFE);
		cursor.Index = 0;
		Assert.False(MuiNotifyFollowParameterVectorCodec.TryGetEntry(
			ref platform, cursor, out _));
		cursor.Base = APTR.FromPointer(0x1800);
		cursor.Index = MuiNotifyFollowParameterVectorCursor.MaximumEntries;
		Assert.False(MuiNotifyFollowParameterVectorCodec.TryGetEntry(
			ref platform, cursor, out _));
	}

	[Fact]
	public void NotifyFollowParameterMemoryAdapterOwnsEntryBounds()
	{
		var platform = CreatePlatform(out _);
		var vector = APTR.FromPointer(0x1800);

		Assert.True(MuiNotifyFollowParameterVectorMemoryCodec.TryGetEntry(
			ref platform, vector, 2, out var address));
		Assert.Equal(APTR.FromPointer(0x1808), address);
		Assert.False(MuiNotifyFollowParameterVectorMemoryCodec.TryGetEntry(
			ref platform, vector,
			MuiNotifyFollowParameterVectorCursor.MaximumEntries, out _));
		Assert.False(MuiNotifyFollowParameterVectorMemoryCodec.TryGetEntry(
			ref platform, APTR.FromPointer(0x20FFE), 0, out _));
		Assert.False(MuiNotifyFollowParameterVectorMemoryCodec.TryGetEntry(
			ref platform, APTR.Null, 0, out _));
	}

	[Fact]
	public void NotifyFollowParameterVectorBridgeUsesNamedSlots()
	{
		var platform = CreatePlatform(out _);
		var vector = APTR.FromPointer(0x1800);
		var expected = new MuiNotifyFollowParameterSlot
		{
			Value = 0xFEDCBA98u,
		};

		Assert.True(MuiNotifyFollowParameterVectorCodec.TryWrite(ref platform,
			vector, 2, expected));
		Assert.True(MuiNotifyFollowParameterVectorCodec.TryRead(ref platform,
			vector, 2, out var decoded));
		Assert.Equal(expected.Value, decoded.Value);
		Assert.True(MuiNotifyFollowParameterVectorCodec.TryReadValue(ref platform,
			vector, 2, out var rawValue));
		Assert.Equal(expected.Value, rawValue);
		Assert.True(MuiNotifyFollowParameterVectorCodec.TryWriteValue(ref platform,
			vector, 3, 0x80000001u));
		Assert.True(MuiNotifyFollowParameterVectorCodec.TryReadValue(ref platform,
			vector, 3, out rawValue));
		Assert.Equal(0x80000001u, rawValue);
		Assert.False(MuiNotifyFollowParameterVectorCodec.TryReadValue(ref platform,
			vector, MuiNotifyFollowParameterVectorCursor.MaximumEntries, out _));
		Assert.False(MuiNotifyFollowParameterVectorCodec.TryWriteValue(ref platform,
			APTR.FromPointer(0xFFFFFFF0), 0, expected.Value));
	}

	[Fact]
	public void MultiSetTargetMemoryAdapterOwnsEntryBounds()
	{
		var platform = CreatePlatform(out _);
		var vector = APTR.FromPointer(0x1800);

		Assert.True(MuiMultiSetTargetVectorMemoryCodec.TryGetEntry(ref platform,
			vector, 2, out var address));
		Assert.Equal(APTR.FromPointer(0x1808), address);
		Assert.False(MuiMultiSetTargetVectorMemoryCodec.TryGetEntry(ref platform,
			vector, MuiMultiSetTargetVectorCursor.MaximumEntries, out _));
		Assert.False(MuiMultiSetTargetVectorMemoryCodec.TryGetEntry(ref platform,
			APTR.FromPointer(0x20FFE), 0, out _));
		Assert.False(MuiMultiSetTargetVectorMemoryCodec.TryGetEntry(ref platform,
			APTR.Null, 0, out _));
	}

	[Fact]
	public void NotifyInlineVectorCursorUsesNamedPacketBoundary()
	{
		var cursor = default(MuiNotifyInlineVectorCursor);
		cursor.Message = APTR.FromPointer(0x1800);
		cursor.Kind = MuiNotifyInlineVectorKind.FollowParameters;
		cursor.Index = 2;
		Assert.True(MuiNotifyInlineVectorCursorCodec.TryGetAddress(cursor,
			out var address));
		Assert.Equal(APTR.FromPointer(0x181C), address);
		cursor.Kind = MuiNotifyInlineVectorKind.MultiSetTargets;
		cursor.Index = 1;
		Assert.True(MuiNotifyInlineVectorCursorCodec.TryGetAddress(cursor,
			out address));
		Assert.Equal(APTR.FromPointer(0x1814), address);
		cursor.Message = APTR.FromPointer(0xFFFFFFF0);
		Assert.False(MuiNotifyInlineVectorCursorCodec.TryGetAddress(cursor,
			out _));
	}

	[Fact]
	public void NotifyInlineVectorMemoryAdapterOwnsEntryBounds()
	{
		var platform = CreatePlatform(out _);
		var message = APTR.FromPointer(0x1800);

		Assert.True(MuiNotifyInlineVectorMemoryCodec.TryGetAddress(ref platform,
			message, MuiNotifyInlineVectorKind.FollowParameters, 2,
			out var address));
		Assert.Equal(APTR.FromPointer(0x181C), address);
		Assert.True(MuiNotifyInlineVectorMemoryCodec.TryGetAddress(ref platform,
			message, MuiNotifyInlineVectorKind.MultiSetTargets, 1,
			out address));
		Assert.Equal(APTR.FromPointer(0x1814), address);
		Assert.False(MuiNotifyInlineVectorMemoryCodec.TryGetAddress(ref platform,
			message, MuiNotifyInlineVectorKind.FollowParameters,
			MuiNotifyInlineVectorCursor.MaximumEntries, out _));
		Assert.False(MuiNotifyInlineVectorMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x20FEC), MuiNotifyInlineVectorKind.FollowParameters,
			0, out _));
		Assert.False(MuiNotifyInlineVectorMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0xFFFFFFF0), MuiNotifyInlineVectorKind.MultiSetTargets,
			0, out _));
	}

	[Fact]
	public void NotifyPacketVectorHelpersUseNamedInlineRecords()
	{
		var packet = APTR.FromPointer(0x1800);
		Assert.Equal(APTR.FromPointer(0x1810),
			MuiNotifyPacketCodec.MultiSetVector(packet));
		Assert.Equal(APTR.FromPointer(0x1814),
			MuiNotifyPacketCodec.FollowParameters(packet));
		Assert.Equal(APTR.Null,
			MuiNotifyPacketCodec.MultiSetVector(APTR.FromPointer(0xFFFFFFF0u)));
	}

	[Fact]
	public void NotifyPacketFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1400);
		var cursor = default(MuiNotifyPacketFieldCursor);
		cursor.Message = packet;
		cursor.Packet = MuiNotifyPacketKind.Notify;
		cursor.Field = MuiNotifyPacketField.TriggerAttribute;
		Assert.True(MuiNotifyPacketFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var address));
		Assert.Equal(packet.Raw + 4, address.Raw);
		cursor.Field = MuiNotifyPacketField.FollowCount;
		Assert.True(MuiNotifyPacketFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 16, address.Raw);
		cursor.Packet = MuiNotifyPacketKind.KillNotifyObject;
		cursor.Field = MuiNotifyPacketField.Destination;
		Assert.True(MuiNotifyPacketFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 8, address.Raw);
		cursor.Packet = MuiNotifyPacketKind.MultiSet;
		cursor.Field = MuiNotifyPacketField.FirstObject;
		Assert.True(MuiNotifyPacketFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 12, address.Raw);
		cursor.Packet = MuiNotifyPacketKind.FindObject;
		cursor.Field = MuiNotifyPacketField.FindObject;
		Assert.True(MuiNotifyPacketFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 4, address.Raw);

		Assert.True(MuiNotifyPacketFieldCursorCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyPacketKind.Set, MuiNotifyPacketField.Attribute,
			Attribute));
		Assert.True(MuiNotifyPacketFieldCursorCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyPacketKind.Set, MuiNotifyPacketField.Value, 77));
		Assert.True(MuiNotifyPacketFieldCursorCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyPacketKind.Set, MuiNotifyPacketField.Value,
			out var value));
		Assert.Equal(77u, value);

		cursor.Packet = MuiNotifyPacketKind.KillNotify;
		cursor.Field = MuiNotifyPacketField.Destination;
		Assert.False(MuiNotifyPacketFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
		cursor.Message = APTR.FromPointer(0xfffffff0u);
		cursor.Packet = MuiNotifyPacketKind.Notify;
		cursor.Field = MuiNotifyPacketField.FollowCount;
		Assert.False(MuiNotifyPacketFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	[Fact]
	public void NotifyPacketFieldMemoryCodecResolvesNamedMixedPacketBoundaries()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1400);
		Assert.True(MuiNotifyPacketFieldMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyPacketKind.Notify,
			MuiNotifyPacketField.TriggerAttribute, Attribute));
		Assert.True(MuiNotifyPacketFieldMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyPacketKind.Notify,
			MuiNotifyPacketField.TriggerAttribute, out var attribute));
		Assert.Equal(Attribute, attribute);
		Assert.True(MuiNotifyPacketFieldMemoryCodec.TryGetAddress(ref platform,
			packet, MuiNotifyPacketKind.MultiSet,
			MuiNotifyPacketField.FirstObject, out var address));
		Assert.Equal(packet.Raw + 12u, address.Raw);
		Assert.False(MuiNotifyPacketFieldMemoryCodec.TryGetAddress(ref platform,
			packet, MuiNotifyPacketKind.KillNotify,
			MuiNotifyPacketField.Destination, out _));
		Assert.False(MuiNotifyPacketFieldMemoryCodec.TryGetAddress(ref platform,
			packet, (MuiNotifyPacketKind)0xFF,
			MuiNotifyPacketField.Value, out _));
		Assert.False(MuiNotifyPacketFieldMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0xFFFFFFF0u), MuiNotifyPacketKind.Notify,
			MuiNotifyPacketField.FollowCount, out _));
	}

	[Fact]
	public void NotificationPayloadAddressUsesNamedRecordBoundary()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1800);

		Assert.True(MuiHeadlessNotificationCodec.TryGetPayload(ref platform,
			address, 8, out var payload));
		Assert.Equal(APTR.FromPointer(0x1820), payload);
		Assert.False(MuiHeadlessNotificationCodec.TryGetPayload(ref platform,
			APTR.FromPointer(0x20FFC), 4, out _));
		Assert.False(MuiHeadlessNotificationCodec.TryGetPayload(ref platform,
			APTR.FromPointer(0xFFFFFFF0), 0, out _));
	}

	[Fact]
	public void NotificationPayloadCursorUsesNamedRecordBoundary()
	{
		var platform = CreatePlatform(out _);
		var cursor = default(MuiHeadlessNotificationPayloadCursor);
		cursor.Record = APTR.FromPointer(0x1800);
		cursor.PayloadBytes = 8;
		Assert.True(MuiHeadlessNotificationPayloadCursorCodec.TryGetAddress(
			ref platform, cursor, out var payload));
		Assert.Equal(APTR.FromPointer(0x1820), payload);
		cursor.Record = APTR.FromPointer(0xFFFFFFF0);
		Assert.False(MuiHeadlessNotificationPayloadCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void FocusedDispatcherRoutesNotifySetAndKillPackets()
	{
		var platform = CreatePlatform(out var cl);
		var source = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var destination = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			cl, APTR.Null);
		var packet = APTR.FromPointer(0x1200);
		var follow = APTR.FromPointer(0x1300);
		platform.WriteUInt32(follow, 0, 0x90000001);
		platform.WriteUInt32(follow, 4, EveryTime);

		platform.WriteUInt32(packet, 0, MuiNotifyCore.NotifyMethod);
		platform.WriteUInt32(packet, 4, Attribute);
		platform.WriteUInt32(packet, 8, EveryTime);
		platform.WriteUInt32(packet, 12, destination.Raw);
		platform.WriteUInt32(packet, 16, 2);
		platform.WriteUInt32(packet, 20, 0x90000001);
		platform.WriteUInt32(packet, 24, EveryTime);
		Assert.Equal(1u, MuiHeadlessDispatcher.DispatchNotify(ref platform, State,
			source, packet));

		platform.WriteUInt32(packet, 0, MuiNotifyCore.SetMethod);
		platform.WriteUInt32(packet, 4, Attribute);
		platform.WriteUInt32(packet, 8, 77);
		Assert.Equal(1u, MuiHeadlessDispatcher.DispatchNotify(ref platform, State,
			source, packet));
		Assert.Equal(1u, platform.DispatchCount);
		Assert.Equal(destination, platform.LastDispatchObject);
		Assert.Equal(77u, platform.LastDispatchArgument);

		platform.WriteUInt32(packet, 0, MuiNotifyCore.KillNotifyObjectMethod);
		platform.WriteUInt32(packet, 4, Attribute);
		platform.WriteUInt32(packet, 8, destination.Raw);
		Assert.Equal(1u, MuiHeadlessDispatcher.DispatchNotify(ref platform, State,
			source, packet));

		platform.WriteUInt32(packet, 0, MuiNotifyCore.NoNotifySetMethod);
		platform.WriteUInt32(packet, 4, Attribute);
		platform.WriteUInt32(packet, 8, 88);
		Assert.Equal(1u, MuiHeadlessDispatcher.DispatchNotify(ref platform, State,
			source, packet));
		Assert.Equal(1u, platform.DispatchCount);
	}

	[Fact]
	public void DisposingNotificationDestinationRemovesGuestRecipe()
	{
		var platform = CreatePlatform(out var cl);
		var source = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var destination = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			cl, APTR.Null);
		var follow = APTR.FromPointer(0x1380);
		platform.WriteUInt32(follow, 0, 0x90000001);

		Assert.True(MuiNotifyCore.Add(ref platform, State, source, Attribute,
			EveryTime, destination, 1, follow));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			source, Attribute, 7, true));
		Assert.Equal(1u, platform.DispatchCount);

		// Teardown resolves the named destination before its guest object is
		// released, so a later source mutation cannot dispatch into stale memory.
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			destination));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			source, Attribute, 8, true));
		Assert.Equal(1u, platform.DispatchCount);
	}

	[Fact]
	public void OmSetMUIANoNotifySuppressesOnlyThatTagOperation()
	{
		var platform = CreatePlatform(out var cl);
		var source = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var destination = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			cl, APTR.Null);
		var follow = APTR.FromPointer(0x1380);
		platform.WriteUInt32(follow, 0, 0x90000001);
		platform.WriteUInt32(follow, 4, EveryTime);
		Assert.True(MuiNotifyCore.Add(ref platform, State, source, Attribute,
			EveryTime, destination, 2, follow));

		var tags = APTR.FromPointer(0x1400);
		platform.WriteUInt32(tags, 0, NoNotifyAttribute);
		platform.WriteUInt32(tags, 4, 1);
		platform.WriteUInt32(tags, 8, Attribute);
		platform.WriteUInt32(tags, 12, 55);
		platform.WriteUInt32(tags, 16, 0);
		platform.WriteUInt32(tags, 20, 0);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0x00000103u); // OM_SET
		platform.WriteUInt32(packet, 4, tags.Raw);
		platform.WriteUInt32(packet, 8, 0);

		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			source, packet));
		Assert.Equal(0u, platform.DispatchCount);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, source,
			Attribute, out var stored));
		Assert.Equal(55u, stored);
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State, source,
			NoNotifyAttribute, out _));

		// The control is operation-local. Clearing it on the next tag list makes
		// the ordinary attribute write notify the original destination.
		platform.WriteUInt32(tags, 4, 0);
		platform.WriteUInt32(tags, 12, 66);
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			source, packet));
		Assert.Equal(1u, platform.DispatchCount);
		Assert.Equal(66u, platform.LastDispatchArgument);
	}

	[Fact]
	public void OmSetMUIANoNotifyMethodSuppressesOnlyMatchingFollowMethod()
	{
		var platform = CreatePlatform(out var cl);
		var source = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var destination = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			cl, APTR.Null);
		var setFollow = APTR.FromPointer(0x1380);
		var writeFollow = APTR.FromPointer(0x13A0);
		platform.WriteUInt32(setFollow, 0, MuiNotifyCore.SetMethod);
		platform.WriteUInt32(setFollow, 4, 0xAAAA);
		platform.WriteUInt32(writeFollow, 0, 0x80428D86u); // MUIM_WriteLong
		platform.WriteUInt32(writeFollow, 4, 0xBBBB);
		Assert.True(MuiNotifyCore.Add(ref platform, State, source, Attribute,
			EveryTime, destination, 2, setFollow));
		Assert.True(MuiNotifyCore.Add(ref platform, State, source, Attribute,
			EveryTime, destination, 2, writeFollow));

		var tags = APTR.FromPointer(0x1400);
		platform.WriteUInt32(tags, 0, NoNotifyMethodAttribute);
		platform.WriteUInt32(tags, 4, MuiNotifyCore.SetMethod);
		platform.WriteUInt32(tags, 8, Attribute);
		platform.WriteUInt32(tags, 12, 77);
		platform.WriteUInt32(tags, 16, 0);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0x00000103u); // OM_SET
		platform.WriteUInt32(packet, 4, tags.Raw);
		platform.WriteUInt32(packet, 8, 0);

		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			source, packet));
		Assert.Equal(1u, platform.DispatchCount);
		Assert.Equal(0xBBBBu, platform.LastDispatchArgument);
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			source, NoNotifyMethodAttribute, out _));

		// The method selector is one-shot. With it cleared, both follow methods
		// are eligible again for the next OM_SET operation.
		platform.WriteUInt32(tags, 4, 0);
		platform.WriteUInt32(tags, 12, 88);
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			source, packet));
		Assert.Equal(3u, platform.DispatchCount);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			destination));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			source));
	}

	[Fact]
	public void FocusedDispatcherRejectsUnknownAndTruncatedPackets()
	{
		var platform = CreatePlatform(out var cl);
		var source = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var packet = APTR.FromPointer(0x1FFF8);
		platform.WriteUInt32(packet, 0, MuiNotifyCore.NotifyMethod);
		Assert.Equal(0u, MuiHeadlessDispatcher.DispatchNotify(ref platform, State,
			source, packet));

		packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.Equal(0u, MuiHeadlessDispatcher.DispatchNotify(ref platform, State,
			source, packet));
		Assert.Equal(0u, platform.ReadUInt32(source, 0x34));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR cl)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
