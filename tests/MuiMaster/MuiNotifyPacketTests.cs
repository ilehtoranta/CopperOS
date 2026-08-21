using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNotifyPacketTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint Attribute = 0x80420020;
	private const uint EveryTime = 1233727793;

	[Fact]
	public void NotifyMethodHeaderUsesNamedField()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, MuiNotifyCore.NotifyMethod);
		Assert.True(MuiNotifyPacketCodec.TryReadMethodId(ref platform, packet,
			out var header));
		Assert.Equal(MuiNotifyCore.NotifyMethod, header.MethodId);
		Assert.False(MuiNotifyPacketCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void NotifyFollowParameterCodecUsesNamedValue()
	{
		var platform = CreatePlatform(out _);
		var slotAddress = APTR.FromPointer(0x1300);
		platform.WriteUInt32(slotAddress, 0, EveryTime);
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
