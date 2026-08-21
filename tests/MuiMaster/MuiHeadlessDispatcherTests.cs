using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiHeadlessDispatcherTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint MUIM_Notify = 0x8042C9CB;
	private const uint MUIM_Export = 0x80420F1C;
	private const uint MUIM_Import = 0x8042D012;
	private const uint MUIM_Set = 0x8042549A;
	private const uint MUIM_Family_AddTail = 0x8042D752;
	private const uint MUIM_Family_GetChild = 0x8042C556;
	private const uint MUIM_Group_MoveMember = 0x8042FF4E;
	private const uint MUIM_Group_Reorder = 0x80426C3F;
	private const uint MUIM_Group_Sort = 0x80427417;
	private const uint MUIM_Dataspace_Add = 0x80423366;
	private const uint MUIM_Dataspace_Get = 0x8042483F;
	private const uint MUIM_Datamap_Set = 0x8042B84F;
	private const uint MUIM_Datamap_Get = 0x8042C2BA;
	private const uint MUIM_Objectmap_Set = 0x80421EC5;
	private const uint MUIM_Objectmap_Find = 0x80426506;
	private const uint MUIM_Semaphore_Attempt = 0x80426CE2;
	private const uint MUIM_Semaphore_Release = 0x80421F2D;
	private const uint MUIA_Datamap_AutoLock = 0x8042FBE4;
	private const uint MUIA_Datamap_CopyKeys = 0x8042A179;
	private const uint MUIA_Objectmap_CopyKeys = 0x8042B964;
	private const uint MUIA_ObjectID = 0x8042D76E;
	private const uint MUIA_String_Contents = 0x80428FFD;
	private const uint MUIA_Text_Contents = 0x8042F8DC;
	private const uint MUIA_Selected = 0x8042654B;
	private const uint MUIA_Image_State = 0x8042A3AD;
	private const uint MUIA_Group_ActivePage = 0x80424199;

	[Fact]
	public void HeadlessMethodHeaderUsesNamedField()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, MUIM_Notify);
		Assert.True(MuiHeadlessMessageCodec.TryReadMethodId(ref platform, packet,
			out var header));
		Assert.Equal(MUIM_Notify, header.MethodId);
		Assert.False(MuiHeadlessMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void ExactPacketsRouteAcrossTheHeadlessCore()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		var family = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(family.IsNotNull && child.IsNotNull);
		var packet = APTR.FromPointer(0x1200);

		WritePacket(ref platform, packet, MUIM_Family_AddTail, child.Raw, 0, 0);
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));
		WritePacket(ref platform, packet, MUIM_Family_GetChild, 0, 0, 0);
		Assert.Equal(child.Raw, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));

		const uint attribute = 0x80420001;
		platform.WriteUInt32(packet, 0, MUIM_Notify);
		platform.WriteUInt32(packet, 4, attribute);
		platform.WriteUInt32(packet, 8, 42);
		platform.WriteUInt32(packet, 12, child.Raw);
		platform.WriteUInt32(packet, 16, 2);
		platform.WriteUInt32(packet, 20, 0x90000001);
		platform.WriteUInt32(packet, 24, 7);
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));
		WritePacket(ref platform, packet, MUIM_Set, attribute, 42, 0);
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));
		Assert.Equal(1u, platform.DispatchCount);

		var data = APTR.FromPointer(0x1300);
		var size = APTR.FromPointer(0x1320);
		platform.WriteUInt32(data, 0, 0x11223344);
		WritePacket(ref platform, packet, MUIM_Dataspace_Add, data.Raw, 4, 17);
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));
		WritePacket(ref platform, packet, MUIM_Dataspace_Get, 17, size.Raw, 0);
		var copied = MuiHeadlessDispatcher.Dispatch(ref platform, State, family,
			packet);
		Assert.NotEqual(data.Raw, copied);
		Assert.Equal(4u, platform.ReadUInt32(size, 0));
		Assert.Equal(0x11223344u,
			platform.ReadUInt32(APTR.FromPointer(copied), 0));

		var key = APTR.FromPointer(0x1340);
		var keyCopy = APTR.FromPointer(0x1360);
		platform.WriteCString(key, "key");
		platform.WriteCString(keyCopy, "key");
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, family,
			MUIA_Datamap_CopyKeys, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, family,
			MUIA_Datamap_AutoLock, 1, false));
		WritePacket(ref platform, packet, MUIM_Datamap_Set, data.Raw, 4, key.Raw);
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));
		platform.WriteCString(key, "changed");
		WritePacket(ref platform, packet, MUIM_Datamap_Get, keyCopy.Raw, size.Raw, 0);
		Assert.NotEqual(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));
		Assert.True(MuiSemaphoreCore.Attempt(ref platform, State, family));
		Assert.True(MuiSemaphoreCore.Release(ref platform, State, family));

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, family,
			MUIA_Objectmap_CopyKeys, 1, false));
		var references = platform.ReadUInt32(child, 4);
		WritePacket(ref platform, packet, MUIM_Objectmap_Set, child.Raw, child.Raw,
			0);
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));
		Assert.Equal(references + 1, platform.ReadUInt32(child, 4));
		WritePacket(ref platform, packet, MUIM_Objectmap_Find, child.Raw, 0, 0);
		Assert.Equal(child.Raw, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));
		Assert.True(MuiStoreCore.ObjectmapRemove(ref platform, State, family,
			child));
		Assert.Equal(references, platform.ReadUInt32(child, 4));

		WritePacket(ref platform, packet, MUIM_Semaphore_Attempt, 0, 0, 0);
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));
		WritePacket(ref platform, packet, MUIM_Semaphore_Release, 0, 0, 0);
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));
	}

	[Fact]
	public void DataspaceFocusedDispatcherUsesNamedPacketsAndRejectsTruncation()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Dataspace.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var source = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(obj.IsNotNull && source.IsNotNull);
		var packet = APTR.FromPointer(0x1200);
		var data = APTR.FromPointer(0x1300);
		var size = APTR.FromPointer(0x1320);
		platform.WriteUInt32(data, 0, 0xAABBCCDD);
		Assert.True(MuiDataspaceMessageCore.WriteAddRecord(ref platform, packet,
			data, 4, 19));
		Assert.Equal(1u, MuiHeadlessDispatcher.DispatchDataspace(ref platform,
			State, obj, packet));
		Assert.True(MuiDataspaceMessageCore.WriteFindRecord(ref platform, packet,
			19));
		Assert.NotEqual(0u, MuiHeadlessDispatcher.DispatchDataspace(ref platform,
			State, obj, packet));
		Assert.True(MuiDataspaceMessageCore.WriteGetRecord(ref platform, packet,
			19, size));
		var returned = MuiHeadlessDispatcher.DispatchDataspace(ref platform,
			State, obj, packet);
		Assert.NotEqual(0u, returned);
		Assert.Equal(4u, platform.ReadUInt32(size, 0));
		Assert.Equal(0xAABBCCDDu,
			platform.ReadUInt32(APTR.FromPointer(returned), 0));
		Assert.True(MuiDataspaceMessageCore.WriteAddRecord(ref platform, packet,
			data, 4, 23));
		Assert.Equal(1u, MuiHeadlessDispatcher.DispatchDataspace(ref platform,
			State, source, packet));
		Assert.True(MuiDataspaceMessageCore.WriteMergeRecord(ref platform, packet,
			source));
		Assert.Equal(1u, MuiHeadlessDispatcher.DispatchDataspace(ref platform,
			State, obj, packet));
		Assert.True(MuiDataspaceMessageCore.WriteRemoveRecord(ref platform, packet,
			19));
		Assert.Equal(1u, MuiHeadlessDispatcher.DispatchDataspace(ref platform,
			State, obj, packet));
		Assert.True(MuiDataspaceMessageCore.WriteClearRecord(ref platform, packet));
		Assert.Equal(1u, MuiHeadlessDispatcher.DispatchDataspace(ref platform,
			State, obj, packet));

		var truncated = APTR.FromPointer(0x20FFC);
		platform.WriteUInt32(truncated, 0, MUIM_Dataspace_Add);
		Assert.Equal(0u, MuiHeadlessDispatcher.DispatchDataspace(ref platform,
			State, obj, truncated));
	}

	[Fact]
	public void DataspaceMethodHeaderUsesNamedField()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiDataspaceMessageCore.WriteFindRecord(ref platform, packet,
			7));
		Assert.True(MuiDataspaceMessageCodec.TryReadMethodId(ref platform, packet,
			out var header));
		Assert.Equal(MuiDataspaceMessageCore.FindMethod, header.MethodId);
		Assert.False(MuiDataspaceMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void DataspaceFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		var cursor = default(MuiDataspaceFieldCursor);
		cursor.Message = packet;
		cursor.Packet = MuiDataspacePacketKind.Add;
		cursor.Field = MuiDataspaceField.MethodId;
		Assert.True(MuiDataspaceFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var address));
		Assert.Equal(packet.Raw, address.Raw);
		cursor.Field = MuiDataspaceField.Data;
		Assert.True(MuiDataspaceFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 4, address.Raw);
		cursor.Field = MuiDataspaceField.Length;
		Assert.True(MuiDataspaceFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 8, address.Raw);
		cursor.Field = MuiDataspaceField.Id;
		Assert.True(MuiDataspaceFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 12, address.Raw);

		Assert.True(MuiDataspaceFieldCursorCodec.TryWriteUInt32(ref platform,
			packet, MuiDataspacePacketKind.Add, MuiDataspaceField.Length,
			unchecked((uint)-12)));
		Assert.True(MuiDataspaceFieldCursorCodec.TryReadUInt32(ref platform,
			packet, MuiDataspacePacketKind.Add, MuiDataspaceField.Length,
			out var length));
		Assert.Equal(unchecked((uint)-12), length);

		cursor.Packet = MuiDataspacePacketKind.Clear;
		cursor.Field = MuiDataspaceField.Id;
		Assert.False(MuiDataspaceFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
		cursor.Message = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Packet = MuiDataspacePacketKind.Get;
		cursor.Field = MuiDataspaceField.SizeStorage;
		Assert.False(MuiDataspaceFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	[Fact]
	public void DataspaceTypedReadersUseNamedMethodHeader()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		var data = APTR.FromPointer(0x1300);
		Assert.True(MuiDataspaceMessageCore.WriteAddRecord(ref platform, packet,
			data, 4, 7));
		Assert.True(MuiDataspaceMessageCore.TryReadAdd(ref platform, packet,
			out var add));
		Assert.Equal(MuiDataspaceMessageCore.AddMethod, add.MethodId);
		Assert.Equal(7u, add.Id);

		Assert.True(MuiDataspaceMessageCore.WriteFindRecord(ref platform, packet,
			7));
		Assert.False(MuiDataspaceMessageCore.TryReadAdd(ref platform, packet,
			out _));
		Assert.True(MuiDataspaceMessageCore.TryReadFind(ref platform, packet,
			out var find));
		Assert.Equal(MuiDataspaceMessageCore.FindMethod, find.MethodId);
	}

	[Fact]
	public void StoreFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		var cursor = default(MuiStoreFieldCursor);
		cursor.Message = packet;
		cursor.Packet = MuiStorePacketKind.DatamapSet;
		cursor.Field = MuiStoreField.MethodId;
		Assert.True(MuiStoreFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var address));
		Assert.Equal(packet.Raw, address.Raw);
		cursor.Field = MuiStoreField.Data;
		Assert.True(MuiStoreFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 4, address.Raw);
		cursor.Field = MuiStoreField.Length;
		Assert.True(MuiStoreFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 8, address.Raw);
		cursor.Field = MuiStoreField.Key;
		Assert.True(MuiStoreFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 12, address.Raw);

		Assert.True(MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform,
			packet, MuiStorePacketKind.DatamapSet, MuiStoreField.Data, 0x1300));
		Assert.True(MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform,
			packet, MuiStorePacketKind.DatamapSet, MuiStoreField.Length,
			unchecked((uint)-24)));
		Assert.True(MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform,
			packet, MuiStorePacketKind.DatamapSet, MuiStoreField.Key, 0x1400));
		Assert.True(MuiStoreFieldCursorCodec.TryReadUInt32(ref platform, packet,
			MuiStorePacketKind.DatamapSet, MuiStoreField.Length, out var length));
		Assert.Equal(unchecked((uint)-24), length);

		cursor.Packet = MuiStorePacketKind.DatamapGet;
		cursor.Field = MuiStoreField.SizeStorage;
		Assert.True(MuiStoreFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 8, address.Raw);
		cursor.Packet = MuiStorePacketKind.ObjectmapSet;
		cursor.Field = MuiStoreField.Object;
		Assert.True(MuiStoreFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 4, address.Raw);
		cursor.Field = MuiStoreField.Key;
		Assert.True(MuiStoreFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 8, address.Raw);

		cursor.Packet = MuiStorePacketKind.Clear;
		cursor.Field = MuiStoreField.Key;
		Assert.False(MuiStoreFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
		cursor.Message = APTR.FromPointer(0xfffffff0u);
		cursor.Packet = MuiStorePacketKind.DatamapSet;
		cursor.Field = MuiStoreField.Key;
		Assert.False(MuiStoreFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	[Fact]
	public void FamilyPacketFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		var cursor = default(MuiFamilyPacketFieldCursor);
		cursor.Message = packet;
		cursor.Packet = MuiFamilyPacketKind.Child;
		cursor.Field = MuiFamilyPacketField.MethodId;
		Assert.True(MuiFamilyPacketFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var address));
		Assert.Equal(packet.Raw, address.Raw);
		cursor.Field = MuiFamilyPacketField.Object;
		Assert.True(MuiFamilyPacketFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 4, address.Raw);

		cursor.Packet = MuiFamilyPacketKind.Insert;
		cursor.Field = MuiFamilyPacketField.Predecessor;
		Assert.True(MuiFamilyPacketFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 8, address.Raw);
		cursor.Packet = MuiFamilyPacketKind.Transfer;
		cursor.Field = MuiFamilyPacketField.Family;
		Assert.True(MuiFamilyPacketFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 4, address.Raw);
		cursor.Packet = MuiFamilyPacketKind.Reorder;
		cursor.Field = MuiFamilyPacketField.After;
		Assert.True(MuiFamilyPacketFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 4, address.Raw);

		Assert.True(MuiFamilyPacketFieldCursorCodec.TryWriteUInt32(ref platform,
			packet, MuiFamilyPacketKind.Insert, MuiFamilyPacketField.Object,
			0x1300));
		Assert.True(MuiFamilyPacketFieldCursorCodec.TryWriteUInt32(ref platform,
			packet, MuiFamilyPacketKind.Insert,
			MuiFamilyPacketField.Predecessor, 0x1400));
		Assert.True(MuiFamilyPacketFieldCursorCodec.TryReadUInt32(ref platform,
			packet, MuiFamilyPacketKind.Insert, MuiFamilyPacketField.Predecessor,
			out var predecessor));
		Assert.Equal(0x1400u, predecessor);

		var list = APTR.FromPointer(0x1500);
		var listCursor = default(MuiFamilyMutationListFieldCursor);
		listCursor.List = list;
		listCursor.Field = MuiFamilyMutationListField.Head;
		Assert.True(MuiFamilyMutationListFieldCursorCodec.TryGetAddress(
			ref platform, listCursor, out address));
		Assert.Equal(list.Raw, address.Raw);
		listCursor.Field = MuiFamilyMutationListField.Tail;
		Assert.True(MuiFamilyMutationListFieldCursorCodec.TryGetAddress(
			ref platform, listCursor, out address));
		Assert.Equal(list.Raw + 4, address.Raw);
		Assert.True(MuiFamilyMutationListFieldCursorCodec.TryWrite(ref platform,
			list, MuiFamilyMutationListField.Tail, 0x1600));
		Assert.True(MuiFamilyMutationListFieldCursorCodec.TryRead(ref platform,
			list, MuiFamilyMutationListField.Tail, out var tail));
		Assert.Equal(0x1600u, tail);

		cursor.Packet = MuiFamilyPacketKind.Method;
		cursor.Field = MuiFamilyPacketField.Object;
		Assert.False(MuiFamilyPacketFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
		cursor.Message = APTR.FromPointer(0xfffffff0u);
		cursor.Packet = MuiFamilyPacketKind.Insert;
		cursor.Field = MuiFamilyPacketField.Predecessor;
		Assert.False(MuiFamilyPacketFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	[Fact]
	public void StoreRecordFieldCursorUsesNamedBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var record = APTR.FromPointer(0x1400);
		var cursor = default(MuiStoreRecordFieldCursor);
		cursor.Record = record;
		cursor.Field = MuiStoreRecordField.Next;
		Assert.True(MuiStoreRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var address));
		Assert.Equal(record.Raw, address.Raw);
		cursor.Field = MuiStoreRecordField.Key;
		Assert.True(MuiStoreRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(record.Raw + 4, address.Raw);
		cursor.Field = MuiStoreRecordField.Data;
		Assert.True(MuiStoreRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(record.Raw + 8, address.Raw);
		cursor.Field = MuiStoreRecordField.Length;
		Assert.True(MuiStoreRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(record.Raw + 12, address.Raw);
		cursor.Field = MuiStoreRecordField.Flags;
		Assert.True(MuiStoreRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(record.Raw + 16, address.Raw);
		cursor.Field = MuiStoreRecordField.Generation;
		Assert.True(MuiStoreRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(record.Raw + 20, address.Raw);
		Assert.True(MuiStoreRecordFieldCursorCodec.TryWrite(ref platform, record,
			MuiStoreRecordField.Next, 0x1500));
		Assert.True(MuiStoreRecordFieldCursorCodec.TryWrite(ref platform, record,
			MuiStoreRecordField.Key, 7));
		Assert.True(MuiStoreRecordFieldCursorCodec.TryWrite(ref platform, record,
			MuiStoreRecordField.Data, 0x1600));
		Assert.True(MuiStoreRecordFieldCursorCodec.TryWrite(ref platform, record,
			MuiStoreRecordField.Length, 4));
		Assert.True(MuiStoreRecordFieldCursorCodec.TryWrite(ref platform, record,
			MuiStoreRecordField.Flags, 0x100));
		Assert.True(MuiStoreRecordFieldCursorCodec.TryWrite(ref platform, record,
			MuiStoreRecordField.Generation, 11));
		Assert.True(MuiStoreRecordFieldCursorCodec.TryRead(ref platform, record,
			MuiStoreRecordField.Key, out var key));
		Assert.Equal(7u, key);
		Assert.True(MuiStoreRecordFieldCursorCodec.TryRead(ref platform, record,
			MuiStoreRecordField.Generation, out var generation));
		Assert.Equal(11u, generation);
		cursor.Record = APTR.FromPointer(0xfffffff0u);
		Assert.False(MuiStoreRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	[Fact]
	public void HeadlessStateFieldCursorUsesNamedBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var state = APTR.FromPointer(0x1800);
		var cursor = default(MuiHeadlessStateFieldCursor);
		cursor.State = state;
		var fields = new[]
		{
			MuiHeadlessStateField.Magic,
			MuiHeadlessStateField.Version,
			MuiHeadlessStateField.Classes,
			MuiHeadlessStateField.Objects,
			MuiHeadlessStateField.NextSequence,
			MuiHeadlessStateField.NotifyDepth,
			MuiHeadlessStateField.Mutation,
			MuiHeadlessStateField.Reserved,
		};
		for (var i = 0; i < fields.Length; i++)
		{
			cursor.Field = fields[i];
			Assert.True(MuiHeadlessStateFieldCursorCodec.TryGetAddress(
				ref platform, cursor, out var address));
			Assert.Equal(state.Raw + (uint)(i * 4), address.Raw);
			Assert.True(MuiHeadlessStateFieldCursorCodec.TryWrite(ref platform,
				state, fields[i], (uint)(0x100 + i)));
		}
		Assert.True(MuiHeadlessStateFieldCursorCodec.TryRead(ref platform, state,
			MuiHeadlessStateField.Magic, out var magic));
		Assert.Equal(0x100u, magic);
		Assert.True(MuiHeadlessStateFieldCursorCodec.TryRead(ref platform, state,
			MuiHeadlessStateField.Mutation, out var mutation));
		Assert.Equal(0x106u, mutation);
		cursor.State = APTR.FromPointer(0xfffffff0u);
		cursor.Field = MuiHeadlessStateField.Reserved;
		Assert.False(MuiHeadlessStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	[Fact]
	public void HeadlessClassFieldCursorUsesNamedMixedWidthBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var record = APTR.FromPointer(0x1c00);
		var cursor = default(MuiHeadlessClassFieldCursor);
		cursor.Record = record;
		var fields = new[]
		{
			MuiHeadlessClassField.Next,
			MuiHeadlessClassField.Name,
			MuiHeadlessClassField.Boopsi,
			MuiHeadlessClassField.Super,
			MuiHeadlessClassField.InstanceSize,
			MuiHeadlessClassField.Reserved,
			MuiHeadlessClassField.Flags,
			MuiHeadlessClassField.ObjectCount,
		};
		var offsets = new uint[] { 0, 4, 8, 12, 16, 18, 20, 24 };
		for (var i = 0; i < fields.Length; i++)
		{
			cursor.Field = fields[i];
			Assert.True(MuiHeadlessClassFieldCursorCodec.TryGetAddress(
				ref platform, cursor, out var address));
			Assert.Equal(record.Raw + offsets[i], address.Raw);
		}
		Assert.True(MuiHeadlessClassFieldCursorCodec.TryWriteUInt32(ref platform,
			record, MuiHeadlessClassField.Next, 0x2100));
		Assert.True(MuiHeadlessClassFieldCursorCodec.TryWriteUInt32(ref platform,
			record, MuiHeadlessClassField.Boopsi, 0x2200));
		Assert.True(MuiHeadlessClassFieldCursorCodec.TryWriteUInt16(ref platform,
			record, MuiHeadlessClassField.InstanceSize, 96));
		Assert.True(MuiHeadlessClassFieldCursorCodec.TryWriteUInt16(ref platform,
			record, MuiHeadlessClassField.Reserved, 0x55aa));
		Assert.True(MuiHeadlessClassFieldCursorCodec.TryWriteUInt32(ref platform,
			record, MuiHeadlessClassField.Flags, 3));
		Assert.True(MuiHeadlessClassFieldCursorCodec.TryWriteUInt32(ref platform,
			record, MuiHeadlessClassField.ObjectCount, 7));
		Assert.True(MuiHeadlessClassFieldCursorCodec.TryReadUInt16(ref platform,
			record, MuiHeadlessClassField.InstanceSize, out var instanceSize));
		Assert.Equal((ushort)96, instanceSize);
		Assert.True(MuiHeadlessClassFieldCursorCodec.TryReadUInt16(ref platform,
			record, MuiHeadlessClassField.Reserved, out var reserved));
		Assert.Equal((ushort)0x55aa, reserved);
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, record,
			out var decoded));
		Assert.Equal(0x2100u, decoded.Next.Raw);
		Assert.Equal((ushort)96, decoded.InstanceSize);
		Assert.Equal(7u, decoded.ObjectCount);
		cursor.Record = APTR.FromPointer(0xfffffff0u);
		cursor.Field = MuiHeadlessClassField.ObjectCount;
		Assert.False(MuiHeadlessClassFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	[Fact]
	public void HeadlessObjectFieldCursorUsesNamedBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var record = APTR.FromPointer(0x2000);
		var cursor = default(MuiHeadlessObjectFieldCursor);
		cursor.Record = record;
		var fields = new[]
		{
			MuiHeadlessObjectField.Next,
			MuiHeadlessObjectField.Boopsi,
			MuiHeadlessObjectField.Class,
			MuiHeadlessObjectField.Attributes,
			MuiHeadlessObjectField.Notifications,
			MuiHeadlessObjectField.ChildrenHead,
			MuiHeadlessObjectField.ChildrenTail,
			MuiHeadlessObjectField.Parent,
			MuiHeadlessObjectField.Stores,
			MuiHeadlessObjectField.SemaphoreOwner,
			MuiHeadlessObjectField.SemaphoreDepth,
			MuiHeadlessObjectField.SemaphoreShared,
			MuiHeadlessObjectField.Flags,
			MuiHeadlessObjectField.Generation,
			MuiHeadlessObjectField.ObjectId,
			MuiHeadlessObjectField.UserData,
		};
		for (var i = 0; i < fields.Length; i++)
		{
			cursor.Field = fields[i];
			Assert.True(MuiHeadlessObjectFieldCursorCodec.TryGetAddress(
				ref platform, cursor, out var address));
			Assert.Equal(record.Raw + (uint)(i * 4), address.Raw);
			Assert.True(MuiHeadlessObjectFieldCursorCodec.TryWrite(ref platform,
				record, fields[i], (uint)(0x3000 + i)));
		}
		Assert.True(MuiHeadlessObjectFieldCursorCodec.TryRead(ref platform, record,
			MuiHeadlessObjectField.SemaphoreDepth, out var depth));
		Assert.Equal(0x300au, depth);
		Assert.True(MuiHeadlessObjectFieldCursorCodec.TryRead(ref platform, record,
			MuiHeadlessObjectField.UserData, out var userData));
		Assert.Equal(0x300fu, userData);
		Assert.True(MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var decoded));
		Assert.Equal(0x3000u, decoded.Next.Raw);
		Assert.Equal(0x3002u, decoded.Class.Raw);
		Assert.Equal(0x300au, decoded.SemaphoreDepth);
		Assert.Equal(0x300eu, decoded.ObjectId);
		cursor.Record = APTR.FromPointer(0xfffffff0u);
		cursor.Field = MuiHeadlessObjectField.UserData;
		Assert.False(MuiHeadlessObjectFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	[Fact]
	public void HeadlessAttributeFieldCursorUsesNamedBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var record = APTR.FromPointer(0x2400);
		var cursor = default(MuiHeadlessAttributeFieldCursor);
		cursor.Record = record;
		var fields = new[]
		{
			MuiHeadlessAttributeField.Next,
			MuiHeadlessAttributeField.Id,
			MuiHeadlessAttributeField.Value,
			MuiHeadlessAttributeField.Generation,
		};
		for (var i = 0; i < fields.Length; i++)
		{
			cursor.Field = fields[i];
			Assert.True(MuiHeadlessAttributeFieldCursorCodec.TryGetAddress(
				ref platform, cursor, out var address));
			Assert.Equal(record.Raw + (uint)(i * 4), address.Raw);
			Assert.True(MuiHeadlessAttributeFieldCursorCodec.TryWrite(ref platform,
				record, fields[i], (uint)(0x4000 + i)));
		}
		Assert.True(MuiHeadlessAttributeFieldCursorCodec.TryRead(ref platform,
			record, MuiHeadlessAttributeField.Value, out var value));
		Assert.Equal(0x4002u, value);
		Assert.True(MuiHeadlessAttributeCodec.TryRead(ref platform, record,
			out var decoded));
		Assert.Equal(0x4000u, decoded.Next.Raw);
		Assert.Equal(0x4001u, decoded.Id);
		Assert.Equal(0x4003u, decoded.Generation);
		cursor.Record = APTR.FromPointer(0xfffffff0u);
		cursor.Field = MuiHeadlessAttributeField.Generation;
		Assert.False(MuiHeadlessAttributeFieldCursorCodec.TryGetAddress(
				ref platform, cursor, out _));
	}

	[Fact]
	public void HeadlessChildFieldCursorUsesNamedBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var record = APTR.FromPointer(0x2800);
		var cursor = default(MuiHeadlessChildFieldCursor);
		cursor.Record = record;
		var fields = new[]
		{
			MuiHeadlessChildField.Next,
			MuiHeadlessChildField.Previous,
			MuiHeadlessChildField.Object,
			MuiHeadlessChildField.Owner,
		};
		for (var i = 0; i < fields.Length; i++)
		{
			cursor.Field = fields[i];
			Assert.True(MuiHeadlessChildFieldCursorCodec.TryGetAddress(
				ref platform, cursor, out var address));
			Assert.Equal(record.Raw + (uint)(i * 4), address.Raw);
			Assert.True(MuiHeadlessChildFieldCursorCodec.TryWrite(ref platform,
				record, fields[i], (uint)(0x5000 + i)));
		}
		Assert.True(MuiHeadlessChildFieldCursorCodec.TryRead(ref platform, record,
			MuiHeadlessChildField.Previous, out var previous));
		Assert.Equal(0x5001u, previous);
		Assert.True(MuiHeadlessChildCodec.TryRead(ref platform, record,
			out var decoded));
		Assert.Equal(0x5000u, decoded.Next.Raw);
		Assert.Equal(0x5002u, decoded.Object.Raw);
		Assert.Equal(0x5003u, decoded.Owner.Raw);
		cursor.Record = APTR.FromPointer(0xfffffff0u);
		cursor.Field = MuiHeadlessChildField.Owner;
		Assert.False(MuiHeadlessChildFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	[Fact]
	public void HeadlessNotificationFieldCursorUsesNamedBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var record = APTR.FromPointer(0x2c00);
		var cursor = default(MuiHeadlessNotificationFieldCursor);
		cursor.Record = record;
		var fields = new[]
		{
			MuiHeadlessNotificationField.Next,
			MuiHeadlessNotificationField.Sequence,
			MuiHeadlessNotificationField.TriggerAttribute,
			MuiHeadlessNotificationField.TriggerValue,
			MuiHeadlessNotificationField.Destination,
			MuiHeadlessNotificationField.FollowCount,
			MuiHeadlessNotificationField.Flags,
			MuiHeadlessNotificationField.Reserved,
		};
		for (var i = 0; i < fields.Length; i++)
		{
			cursor.Field = fields[i];
			Assert.True(MuiHeadlessNotificationFieldCursorCodec.TryGetAddress(
				ref platform, cursor, out var address));
			Assert.Equal(record.Raw + (uint)(i * 4), address.Raw);
			Assert.True(MuiHeadlessNotificationFieldCursorCodec.TryWrite(
				ref platform, record, fields[i], (uint)(0x6000 + i)));
		}
		Assert.True(MuiHeadlessNotificationFieldCursorCodec.TryRead(ref platform,
			record, MuiHeadlessNotificationField.TriggerValue, out var trigger));
		Assert.Equal(0x6003u, trigger);
		Assert.True(MuiHeadlessNotificationCodec.TryRead(ref platform, record,
			out var decoded));
		Assert.Equal(0x6000u, decoded.Next.Raw);
		Assert.Equal(0x6004u, decoded.Destination.Raw);
		Assert.Equal(0x6006u, decoded.Flags);
		var payload = APTR.FromPointer(record.Raw + 32);
		platform.WriteUInt32(payload, 0, 0xabcdef01);
		Assert.True(MuiHeadlessNotificationCodec.TryGetPayload(ref platform,
			record, 4, out var payloadAddress));
		Assert.Equal(payload.Raw, payloadAddress.Raw);
		Assert.Equal(0xabcdef01u, platform.ReadUInt32(payloadAddress, 0));
		cursor.Record = APTR.FromPointer(0xfffffff0u);
		cursor.Field = MuiHeadlessNotificationField.Reserved;
		Assert.False(MuiHeadlessNotificationFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
		Assert.False(MuiHeadlessNotificationCodec.TryGetPayload(ref platform,
			APTR.FromPointer(0xfffffff0u), 32, out _));
	}

	[Fact]
	public void StorePacketsUseNamedRecordsAcrossTheLiveDispatcher()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "StorePackets.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		var store = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var objectKey = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			cl, APTR.Null);
		Assert.True(store.IsNotNull && objectKey.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, store,
			MUIA_Datamap_CopyKeys, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, store,
			MUIA_Objectmap_CopyKeys, 1, false));

		var packet = APTR.FromPointer(0x1200);
		var data = APTR.FromPointer(0x1300);
		var size = APTR.FromPointer(0x1320);
		var key = APTR.FromPointer(0x1340);
		platform.WriteUInt32(data, 0, 0x11223344);
		platform.WriteCString(key, "named-key");
		Assert.True(MuiStoreMessageCore.WriteDatamapSetRecord(ref platform,
			packet, data, 4, key));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			store, packet));
		Assert.True(MuiStoreMessageCore.WriteDatamapGetRecord(ref platform,
			packet, key, size));
		var copied = MuiHeadlessDispatcher.Dispatch(ref platform, State, store,
			packet);
		Assert.NotEqual(0u, copied);
		Assert.Equal(4u, platform.ReadUInt32(size, 0));
		Assert.Equal(0x11223344u, platform.ReadUInt32(APTR.FromPointer(copied), 0));
		Assert.True(MuiStoreMessageCore.WriteDatamapKeyRecord(ref platform,
			packet, MuiStoreMessageCore.DatamapFindMethod, key));
		Assert.NotEqual(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			store, packet));
		var counter = APTR.FromPointer(0x1360);
		Assert.True(MuiStoreMessageCore.WriteDatamapCounterRecord(ref platform,
			packet, MuiStoreMessageCore.DatamapIterateMethod, counter));
		Assert.NotEqual(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			store, packet));
		Assert.True(MuiStoreMessageCore.WriteDatamapKeyRecord(ref platform,
			packet, MuiStoreMessageCore.DatamapRemoveMethod, key));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			store, packet));
		Assert.True(MuiStoreMessageCore.WriteDatamapClearRecord(ref platform,
			packet));
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			store, packet));

		Assert.True(MuiStoreMessageCore.WriteObjectmapSetRecord(ref platform,
			packet, objectKey, objectKey));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			store, packet));
		Assert.True(MuiStoreMessageCore.WriteObjectmapKeyRecord(ref platform,
			packet, MuiStoreMessageCore.ObjectmapFindMethod, objectKey));
		Assert.Equal(objectKey.Raw, MuiHeadlessDispatcher.Dispatch(ref platform,
			State, store, packet));
		Assert.True(MuiStoreMessageCore.WriteObjectmapKeyRecord(ref platform,
			packet, MuiStoreMessageCore.ObjectmapRemoveMethod, objectKey));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			store, packet));
		Assert.True(MuiStoreMessageCore.WriteObjectmapClearRecord(ref platform,
			packet));
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			store, packet));

		var truncated = APTR.FromPointer(0x1FFF8);
		platform.WriteUInt32(truncated, 0, MuiStoreMessageCore.DatamapSetMethod);
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			store, truncated));
	}

	[Fact]
	public void StoreMethodHeaderUsesNamedField()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiStoreMessageCore.WriteDatamapGetRecord(ref platform, packet,
			APTR.FromPointer(0x1300), APTR.FromPointer(0x1340)));
		Assert.True(MuiStoreMessageCodec.TryReadMethodId(ref platform, packet,
			out var header));
		Assert.Equal(MuiStoreMessageCore.DatamapGetMethod, header.MethodId);
		Assert.False(MuiStoreMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void StoreTypedReadersUseNamedMethodHeader()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		var data = APTR.FromPointer(0x1300);
		var key = APTR.FromPointer(0x1340);
		var sizeStorage = APTR.FromPointer(0x1380);
		var counter = APTR.FromPointer(0x13A0);

		Assert.True(MuiStoreMessageCore.WriteDatamapSetRecord(ref platform,
			packet, data, 4, key));
		Assert.Equal(4u, MuiStoreMessageCore.DispatchRecord(ref platform,
			packet));
		Assert.True(MuiStoreMessageCore.WriteDatamapGetRecord(ref platform,
			packet, key, sizeStorage));
		Assert.Equal(sizeStorage.Raw, MuiStoreMessageCore.DispatchRecord(
			ref platform, packet));
		Assert.True(MuiStoreMessageCore.WriteDatamapKeyRecord(ref platform,
			packet, MuiStoreMessageCore.DatamapFindMethod, key));
		Assert.Equal(key.Raw, MuiStoreMessageCore.DispatchRecord(ref platform,
			packet));
		Assert.True(MuiStoreMessageCore.WriteDatamapCounterRecord(ref platform,
			packet, MuiStoreMessageCore.DatamapIterateMethod, counter));
		Assert.Equal(counter.Raw, MuiStoreMessageCore.DispatchRecord(
			ref platform, packet));
		Assert.True(MuiStoreMessageCore.WriteDatamapClearRecord(ref platform,
			packet));
		Assert.Equal(1u, MuiStoreMessageCore.DispatchRecord(ref platform,
			packet));

		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.Equal(0u, MuiStoreMessageCore.DispatchRecord(ref platform,
			packet));
	}

	[Fact]
	public void DataspaceIffRoundTripUsesTypedPacketsAndShortTransfers()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Dataspace.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(obj.IsNotNull);

		// The chunk contains two big-endian Dataspace entry records. The host
		// capability deliberately returns at most three bytes per read.
		var stream = platform.IffBuffer;
		platform.WriteUInt32(stream, 0, 0x00000111);
		platform.WriteUInt32(stream, 4, 3);
		platform.WriteUInt8(stream, 8, 0xA1);
		platform.WriteUInt8(stream, 9, 0xA2);
		platform.WriteUInt8(stream, 10, 0xA3);
		platform.WriteUInt32(stream, 11, 0x00000222);
		platform.WriteUInt32(stream, 15, 4);
		platform.WriteUInt32(stream, 19, 0x10203040);
		platform.IffLength = 23;
		platform.IffPosition = 0;
		platform.IffReadChunkLimit = 3;
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiDataspaceIffMessageCore.WriteReadIffRecord(ref platform,
			packet, APTR.FromPointer(2)));
		Assert.Equal(0u, MuiHeadlessDispatcher.DispatchDataspaceIff(ref platform,
			State, obj, packet));
		Assert.Equal(3, MuiStoreCore.DataspaceLength(ref platform, State, obj,
			0x111));
		Assert.Equal(4, MuiStoreCore.DataspaceLength(ref platform, State, obj,
			0x222));
		var first = MuiStoreCore.DataspaceFind(ref platform, State, obj, 0x111);
		Assert.Equal((byte)0xA1, platform.ReadUInt8(first, 0));
		Assert.Equal((byte)0xA3, platform.ReadUInt8(first, 2));

		platform.IffWriteChunkLimit = 5;
		Assert.True(MuiDataspaceIffMessageCore.WriteWriteIffRecord(ref platform,
			packet, APTR.FromPointer(2), 0x464F524D, 0x44415441));
		Assert.Equal(0u, MuiHeadlessDispatcher.DispatchDataspaceIff(ref platform,
			State, obj, packet));
		Assert.Equal(1u, platform.IffPushCount);
		Assert.Equal(1u, platform.IffPopCount);
		Assert.Equal(0x464F524Du, platform.IffLastType);
		Assert.Equal(0x44415441u, platform.IffLastId);
		Assert.Equal(23u, platform.IffLength);
		// StoreCore keeps the newest numeric entry at the head, so the writer
		// emits the second entry before the first one.
		Assert.Equal(0x00000222u, platform.ReadUInt32(stream, 0));
		Assert.Equal(4u, platform.ReadUInt32(stream, 4));
		Assert.Equal(0x10203040u, platform.ReadUInt32(stream, 8));
		Assert.Equal(0x00000111u, platform.ReadUInt32(stream, 12));
		Assert.Equal(3u, platform.ReadUInt32(stream, 16));
		Assert.Equal((byte)0xA1, platform.ReadUInt8(stream, 20));

		var truncated = APTR.FromPointer(0x20FFC);
		platform.WriteUInt32(truncated, 0, MuiDataspaceIffMessageCore.ReadIffMethod);
		Assert.Equal(0u, MuiHeadlessDispatcher.DispatchDataspaceIff(ref platform,
			State, obj, truncated));
	}

	[Fact]
	public void DataspaceIffMethodHeaderUsesNamedField()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiDataspaceIffMessageCore.WriteReadIffRecord(ref platform,
			packet, APTR.FromPointer(0x1300)));
		Assert.True(MuiDataspaceIffMessageCodec.TryReadMethodId(ref platform,
			packet, out var header));
		Assert.Equal(MuiDataspaceIffMessageCore.ReadIffMethod, header.MethodId);
		Assert.False(MuiDataspaceIffMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void DataspaceIffMethodFieldCursorUsesNamedBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var message = APTR.FromPointer(0x1200);
		var cursor = default(MuiDataspaceIffMethodFieldCursor);
		cursor.Message = message;
		cursor.Field = MuiDataspaceIffMethodField.MethodId;
		Assert.True(MuiDataspaceIffMethodFieldCursorCodec.TryGetAddress(ref
			platform, cursor, out var address));
		Assert.Equal(message.Raw, address.Raw);
		Assert.True(MuiDataspaceIffMethodFieldCursorCodec.TryWrite(ref platform,
			message, MuiDataspaceIffMethodField.MethodId,
			MuiDataspaceIffMessageCore.WriteIffMethod));
		Assert.True(MuiDataspaceIffMethodFieldCursorCodec.TryRead(ref platform,
			message, MuiDataspaceIffMethodField.MethodId, out var method));
		Assert.Equal(MuiDataspaceIffMessageCore.WriteIffMethod, method);
		cursor.Message = APTR.FromPointer(0x50000);
		Assert.False(MuiDataspaceIffMethodFieldCursorCodec.TryGetAddress(ref
			platform, cursor, out _));
	}

	[Fact]
	public void DataspaceIffEntryHeaderUsesNamedFields()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var address = APTR.FromPointer(0x1400);
		var expected = new MuiDataspaceIffEntryHeader
		{
			Id = 0x44415441,
			Length = 37,
		};

		Assert.True(MuiDataspaceIffEntryHeaderCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiDataspaceIffEntryHeaderCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Id, actual.Id);
		Assert.Equal(expected.Length, actual.Length);
		Assert.False(MuiDataspaceIffEntryHeaderCodec.TryRead(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void DataspaceIffEntryHeaderFieldCursorUsesNamedBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var header = APTR.FromPointer(0x1400);
		var cursor = default(MuiDataspaceIffEntryHeaderFieldCursor);
		cursor.Header = header;
		cursor.Field = MuiDataspaceIffEntryHeaderField.Id;
		Assert.True(MuiDataspaceIffEntryHeaderFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var address));
		Assert.Equal(header.Raw, address.Raw);
		cursor.Field = MuiDataspaceIffEntryHeaderField.Length;
		Assert.True(MuiDataspaceIffEntryHeaderFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(header.Raw + 4, address.Raw);
		Assert.True(MuiDataspaceIffEntryHeaderFieldCursorCodec.TryWrite(ref
			platform, header, MuiDataspaceIffEntryHeaderField.Id, 0x44415441));
		Assert.True(MuiDataspaceIffEntryHeaderFieldCursorCodec.TryWrite(ref
			platform, header, MuiDataspaceIffEntryHeaderField.Length, 37));
		Assert.True(MuiDataspaceIffEntryHeaderFieldCursorCodec.TryRead(ref
			platform, header, MuiDataspaceIffEntryHeaderField.Id, out var id));
		Assert.Equal(0x44415441u, id);
		Assert.True(MuiDataspaceIffEntryHeaderFieldCursorCodec.TryRead(ref
			platform, header, MuiDataspaceIffEntryHeaderField.Length,
			out var length));
		Assert.Equal(37u, length);
		cursor.Header = APTR.FromPointer(0xfffffffcu);
		Assert.False(MuiDataspaceIffEntryHeaderFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void DataspaceIffTransferCursorUsesNamedChunkBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var cursor = new MuiDataspaceIffTransferCursor
		{
			Base = APTR.FromPointer(0x1800),
			Offset = 4,
		};

		Assert.True(MuiDataspaceIffTransferCursorCodec.TryGetAddress(ref platform,
			cursor, 4, out var address));
		Assert.Equal(APTR.FromPointer(0x1804), address);
		cursor.Base = APTR.FromPointer(0x20FFC);
		cursor.Offset = 0;
		Assert.True(MuiDataspaceIffTransferCursorCodec.TryGetAddress(ref platform,
			cursor, 4, out address));
		Assert.Equal(APTR.FromPointer(0x20FFC), address);
		Assert.False(MuiDataspaceIffTransferCursorCodec.TryGetAddress(
			ref platform, cursor, 5, out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		Assert.False(MuiDataspaceIffTransferCursorCodec.TryGetAddress(
			ref platform, cursor, 4, out _));
		Assert.False(MuiDataspaceIffTransferCursorCodec.TryGetAddress(
			ref platform, default, 1, out _));
	}

	[Fact]
	public void DataspaceIffWriteMessageFieldCursorUsesNamedBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var message = APTR.FromPointer(0x1200);
		var cursor = default(MuiDataspaceWriteIffFieldCursor);
		cursor.Message = message;
		cursor.Field = MuiDataspaceWriteIffField.MethodId;
		Assert.True(MuiDataspaceWriteIffFieldCursorCodec.TryGetAddress(ref
			platform, cursor, out var address));
		Assert.Equal(message.Raw, address.Raw);
		cursor.Field = MuiDataspaceWriteIffField.Handle;
		Assert.True(MuiDataspaceWriteIffFieldCursorCodec.TryGetAddress(ref
			platform, cursor, out address));
		Assert.Equal(message.Raw + 4, address.Raw);
		cursor.Field = MuiDataspaceWriteIffField.Type;
		Assert.True(MuiDataspaceWriteIffFieldCursorCodec.TryGetAddress(ref
			platform, cursor, out address));
		Assert.Equal(message.Raw + 8, address.Raw);
		cursor.Field = MuiDataspaceWriteIffField.Id;
		Assert.True(MuiDataspaceWriteIffFieldCursorCodec.TryGetAddress(ref
			platform, cursor, out address));
		Assert.Equal(message.Raw + 12, address.Raw);
		Assert.True(MuiDataspaceWriteIffFieldCursorCodec.TryWrite(ref platform,
			message, MuiDataspaceWriteIffField.MethodId,
			MuiDataspaceIffMessageCore.WriteIffMethod));
		Assert.True(MuiDataspaceWriteIffFieldCursorCodec.TryWrite(ref platform,
			message, MuiDataspaceWriteIffField.Handle, 0x1300));
		Assert.True(MuiDataspaceWriteIffFieldCursorCodec.TryWrite(ref platform,
			message, MuiDataspaceWriteIffField.Type, 1));
		Assert.True(MuiDataspaceWriteIffFieldCursorCodec.TryWrite(ref platform,
			message, MuiDataspaceWriteIffField.Id, 2));
		Assert.True(MuiDataspaceWriteIffFieldCursorCodec.TryRead(ref platform,
			message, MuiDataspaceWriteIffField.Handle, out var handle));
		Assert.Equal(0x1300u, handle);
		Assert.True(MuiDataspaceWriteIffFieldCursorCodec.TryRead(ref platform,
			message, MuiDataspaceWriteIffField.Id, out var id));
		Assert.Equal(2u, id);
		cursor.Message = APTR.FromPointer(0xfffffffcu);
		Assert.False(MuiDataspaceWriteIffFieldCursorCodec.TryGetAddress(ref
			platform, cursor, out _));
	}

	[Fact]
	public void DataspaceIffReadMessageFieldCursorUsesNamedBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var message = APTR.FromPointer(0x1200);
		var cursor = default(MuiDataspaceReadIffFieldCursor);
		cursor.Message = message;
		cursor.Field = MuiDataspaceReadIffField.MethodId;
		Assert.True(MuiDataspaceReadIffFieldCursorCodec.TryGetAddress(ref
			platform, cursor, out var address));
		Assert.Equal(message.Raw, address.Raw);
		cursor.Field = MuiDataspaceReadIffField.Handle;
		Assert.True(MuiDataspaceReadIffFieldCursorCodec.TryGetAddress(ref
			platform, cursor, out address));
		Assert.Equal(message.Raw + 4, address.Raw);
		Assert.True(MuiDataspaceReadIffFieldCursorCodec.TryWrite(ref platform,
			message, MuiDataspaceReadIffField.MethodId,
			MuiDataspaceIffMessageCore.ReadIffMethod));
		Assert.True(MuiDataspaceReadIffFieldCursorCodec.TryWrite(ref platform,
			message, MuiDataspaceReadIffField.Handle, 0x1300));
		Assert.True(MuiDataspaceReadIffFieldCursorCodec.TryRead(ref platform,
			message, MuiDataspaceReadIffField.MethodId, out var method));
		Assert.Equal(MuiDataspaceIffMessageCore.ReadIffMethod, method);
		Assert.True(MuiDataspaceReadIffFieldCursorCodec.TryRead(ref platform,
			message, MuiDataspaceReadIffField.Handle, out var handle));
		Assert.Equal(0x1300u, handle);
		cursor.Message = APTR.FromPointer(0xfffffffcu);
		Assert.False(MuiDataspaceReadIffFieldCursorCodec.TryGetAddress(ref
			platform, cursor, out _));
	}

	[Fact]
	public void DataspaceIffTypedReadersUseNamedMethodHeader()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiDataspaceIffMessageCore.WriteReadIffRecord(ref platform,
			packet, APTR.FromPointer(0x1300)));
		Assert.True(MuiDataspaceIffMessageCore.TryReadReadIff(ref platform,
			packet, out var read));
		Assert.Equal(MuiDataspaceIffMessageCore.ReadIffMethod, read.MethodId);

		Assert.True(MuiDataspaceIffMessageCore.WriteWriteIffRecord(ref platform,
			packet, APTR.FromPointer(0x1300), 1, 2));
		Assert.False(MuiDataspaceIffMessageCore.TryReadReadIff(ref platform,
			packet, out _));
		Assert.True(MuiDataspaceIffMessageCore.TryReadWriteIff(ref platform,
			packet, out var write));
		Assert.Equal(MuiDataspaceIffMessageCore.WriteIffMethod, write.MethodId);
	}

	[Fact]
	public void NotifyWriteMethodsUseNamedPacketsAndBoundedGuestCopy()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(obj.IsNotNull);
		var packet = APTR.FromPointer(0x1200);
		var memory = APTR.FromPointer(0x1300);
		Assert.True(MuiNotifyWriteCore.WriteLongRecord(ref platform, packet,
			0xAABBCCDD, memory));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.Equal(0xAABBCCDDu, platform.ReadUInt32(memory, 0));

		var source = APTR.FromPointer(0x1400);
		var destination = APTR.FromPointer(0x1500);
		platform.WriteCString(source, "MorphOS MUI");
		Assert.True(MuiNotifyWriteCore.WriteStringRecord(ref platform, packet,
			source, destination));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State, obj,
			packet));
		uint sourceLength;
		uint destinationLength;
		Assert.True(CStringCodec.TryReadLength(ref platform, source, 4096,
			out sourceLength));
		Assert.True(CStringCodec.TryReadLength(ref platform, destination, 4096,
			out destinationLength));
		Assert.Equal(sourceLength, destinationLength);
		for (var index = 0u; index <= sourceLength; index++)
			Assert.Equal(platform.ReadUInt8(APTR.FromPointer(source.Raw + index), 0),
				platform.ReadUInt8(APTR.FromPointer(destination.Raw + index), 0));

		var truncated = APTR.FromPointer(0x20FFC);
		platform.WriteUInt32(truncated, 0, MuiNotifyWriteCore.WriteLongMethod);
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State, obj,
			truncated));
	}

	[Fact]
	public void NotifyWriteMethodHeaderUsesNamedField()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiNotifyWriteCore.WriteLongRecord(ref platform, packet,
			0xAABBCCDD, APTR.FromPointer(0x1300)));
		Assert.True(MuiNotifyWriteMessageCodec.TryReadMethodId(ref platform,
			packet, out var header));
		Assert.Equal(MuiNotifyWriteCore.WriteLongMethod, header.MethodId);
		Assert.False(MuiNotifyWriteMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void NotifyWriteTypedReadersUseNamedMethodHeader()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		var memory = APTR.FromPointer(0x1300);
		Assert.True(MuiNotifyWriteCore.WriteLongRecord(ref platform, packet,
			0xA5, memory));
		Assert.True(MuiNotifyWriteCore.TryReadWriteLong(ref platform, packet,
			out var writeLong));
		Assert.Equal(MuiNotifyWriteCore.WriteLongMethod, writeLong.MethodId);
		Assert.Equal(memory.Raw, writeLong.Memory.Raw);

		Assert.True(MuiNotifyWriteCore.WriteStringRecord(ref platform, packet,
			APTR.FromPointer(0x1400), memory));
		Assert.False(MuiNotifyWriteCore.TryReadWriteLong(ref platform, packet,
			out _));
		Assert.True(MuiNotifyWriteCore.TryReadWriteString(ref platform, packet,
			out var writeString));
		Assert.Equal(MuiNotifyWriteCore.WriteStringMethod, writeString.MethodId);
	}

	[Fact]
	public void NotifyWritePacketFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		var cursor = default(MuiNotifyWritePacketFieldCursor);
		cursor.Message = packet;
		cursor.Packet = MuiNotifyWritePacketKind.WriteLong;
		cursor.Field = MuiNotifyWritePacketField.MethodId;
		Assert.True(MuiNotifyWritePacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var address));
		Assert.Equal(packet.Raw, address.Raw);
		cursor.Field = MuiNotifyWritePacketField.Value;
		Assert.True(MuiNotifyWritePacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(packet.Raw + 4, address.Raw);
		cursor.Field = MuiNotifyWritePacketField.Memory;
		Assert.True(MuiNotifyWritePacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(packet.Raw + 8, address.Raw);
		cursor.Packet = MuiNotifyWritePacketKind.WriteString;
		cursor.Field = MuiNotifyWritePacketField.String;
		Assert.True(MuiNotifyWritePacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(packet.Raw + 4, address.Raw);

		Assert.True(MuiNotifyWritePacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiNotifyWritePacketKind.WriteLong,
			MuiNotifyWritePacketField.Value, 0xAABBCCDD));
		Assert.True(MuiNotifyWritePacketFieldCursorCodec.TryReadUInt32(
			ref platform, packet, MuiNotifyWritePacketKind.WriteLong,
			MuiNotifyWritePacketField.Value, out var value));
		Assert.Equal(0xAABBCCDDu, value);
		Assert.True(MuiNotifyWritePacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiNotifyWritePacketKind.WriteString,
			MuiNotifyWritePacketField.String, 0x1500));
		Assert.True(MuiNotifyWritePacketFieldCursorCodec.TryReadUInt32(
			ref platform, packet, MuiNotifyWritePacketKind.WriteString,
			MuiNotifyWritePacketField.String, out var source));
		Assert.Equal(0x1500u, source);

		cursor.Packet = MuiNotifyWritePacketKind.WriteLong;
		cursor.Field = MuiNotifyWritePacketField.String;
		Assert.False(MuiNotifyWritePacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
		cursor.Message = APTR.FromPointer(0xfffffff0u);
		cursor.Field = MuiNotifyWritePacketField.Memory;
		Assert.False(MuiNotifyWritePacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void GetConfigItemPacketFieldCursorUsesNamedMixedBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		var cursor = default(MuiGetConfigItemPacketFieldCursor);
		cursor.Message = packet;
		cursor.Field = MuiGetConfigItemPacketField.MethodId;
		Assert.True(MuiGetConfigItemPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var address));
		Assert.Equal(packet.Raw, address.Raw);
		cursor.Field = MuiGetConfigItemPacketField.ConfigId;
		Assert.True(MuiGetConfigItemPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(packet.Raw + 4, address.Raw);
		cursor.Field = MuiGetConfigItemPacketField.Storage;
		Assert.True(MuiGetConfigItemPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(packet.Raw + 8, address.Raw);

		Assert.True(MuiGetConfigItemPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiGetConfigItemPacketField.ConfigId,
			0x8042BEEFu));
		Assert.True(MuiGetConfigItemPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiGetConfigItemPacketField.Storage, 0x1500));
		Assert.True(MuiGetConfigItemPacketFieldCursorCodec.TryReadUInt32(
			ref platform, packet, MuiGetConfigItemPacketField.ConfigId,
			out var configId));
		Assert.Equal(0x8042BEEFu, configId);
		Assert.True(MuiGetConfigItemPacketFieldCursorCodec.TryReadUInt32(
			ref platform, packet, MuiGetConfigItemPacketField.Storage,
			out var storage));
		Assert.Equal(0x1500u, storage);

		cursor.Message = APTR.FromPointer(0xfffffff0u);
		cursor.Field = MuiGetConfigItemPacketField.Storage;
		Assert.False(MuiGetConfigItemPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void NotifyUserDataPacketAndTraversalCursorsUseNamedBoundaries()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		var cursor = default(MuiNotifyUserDataPacketFieldCursor);
		cursor.Message = packet;
		cursor.Packet = MuiNotifyUserDataPacketKind.Find;
		cursor.Field = MuiNotifyUserDataPacketField.UserData;
		Assert.True(MuiNotifyUserDataPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var address));
		Assert.Equal(packet.Raw + 4, address.Raw);
		cursor.Packet = MuiNotifyUserDataPacketKind.Get;
		cursor.Field = MuiNotifyUserDataPacketField.Storage;
		Assert.True(MuiNotifyUserDataPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(packet.Raw + 12, address.Raw);
		cursor.Packet = MuiNotifyUserDataPacketKind.Set;
		cursor.Field = MuiNotifyUserDataPacketField.Value;
		Assert.True(MuiNotifyUserDataPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(packet.Raw + 12, address.Raw);

		Assert.True(MuiNotifyUserDataPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiNotifyUserDataPacketKind.Get,
			MuiNotifyUserDataPacketField.UserData, 7));
		Assert.True(MuiNotifyUserDataPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiNotifyUserDataPacketKind.Get,
			MuiNotifyUserDataPacketField.Attribute, 0x8042AAAA));
		Assert.True(MuiNotifyUserDataPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiNotifyUserDataPacketKind.Get,
			MuiNotifyUserDataPacketField.Storage, 0x1500));
		Assert.True(MuiNotifyUserDataPacketFieldCursorCodec.TryReadUInt32(
			ref platform, packet, MuiNotifyUserDataPacketKind.Get,
			MuiNotifyUserDataPacketField.Attribute, out var attribute));
		Assert.Equal(0x8042AAAAu, attribute);

		var frame = APTR.FromPointer(0x1600);
		var frameCursor = default(MuiUDataTraversalFieldCursor);
		frameCursor.Frame = frame;
		frameCursor.Field = MuiUDataTraversalField.Object;
		Assert.True(MuiUDataTraversalFieldCursorCodec.TryGetAddress(ref platform,
			frameCursor, out address));
		Assert.Equal(frame.Raw, address.Raw);
		frameCursor.Field = MuiUDataTraversalField.NextChild;
		Assert.True(MuiUDataTraversalFieldCursorCodec.TryGetAddress(ref platform,
			frameCursor, out address));
		Assert.Equal(frame.Raw + 4, address.Raw);
		Assert.True(MuiUDataTraversalFieldCursorCodec.TryWriteUInt32(ref platform,
			frame, MuiUDataTraversalField.NextChild, 3));
		Assert.True(MuiUDataTraversalFieldCursorCodec.TryReadUInt32(ref platform,
			frame, MuiUDataTraversalField.NextChild, out var nextChild));
		Assert.Equal(3u, nextChild);

		cursor.Packet = MuiNotifyUserDataPacketKind.Find;
		cursor.Field = MuiNotifyUserDataPacketField.Attribute;
		Assert.False(MuiNotifyUserDataPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
		frameCursor.Frame = APTR.FromPointer(0xfffffff0u);
		Assert.False(MuiUDataTraversalFieldCursorCodec.TryGetAddress(ref platform,
			frameCursor, out _));
	}

	[Fact]
	public void NotifySetAsStringUsesTypedHeaderAndOwnedBoundedText()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(obj.IsNotNull);

		const uint attribute = 0x8042AAAA;
		var packet = APTR.FromPointer(0x1200);
		var format = APTR.FromPointer(0x1400);
		platform.WriteCString(format, "value=%ld");
		Assert.True(MuiNotifySetAsStringCore.WriteRecord(ref platform, packet,
			attribute, format, 42));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			attribute, out var value));
		var expected = APTR.FromPointer(0x1600);
		platform.WriteCString(expected, "value=42");
		Assert.True(CStringCodec.TryEquals(ref platform,
			APTR.FromPointer(value), expected, 1024, out var equal) && equal);

		var stringFormat = APTR.FromPointer(0x1800);
		var source = APTR.FromPointer(0x1900);
		platform.WriteCString(stringFormat, "name=%s value=%ld");
		platform.WriteCString(source, "guest");
		Assert.True(MuiNotifySetAsStringCore.WriteRecord(ref platform, packet,
			attribute, stringFormat, source.Raw));
		platform.WriteUInt32(packet, 16, 7);
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			attribute, out value));
		platform.WriteCString(expected, "name=guest value=7");
		Assert.True(CStringCodec.TryEquals(ref platform,
			APTR.FromPointer(value), expected, 1024, out equal) && equal);

		var tooMany = APTR.FromPointer(0x1700);
		platform.WriteCString(tooMany,
			"%ld%ld%ld%ld%ld%ld%ld%ld%ld");
		Assert.True(MuiNotifySetAsStringCore.WriteRecord(ref platform, packet,
			attribute, tooMany, 1));
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State, obj,
			packet));

		var truncated = APTR.FromPointer(0x20FFC);
		platform.WriteUInt32(truncated, 0, MuiNotifySetAsStringCore.Method);
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State, obj,
			truncated));
	}

	[Fact]
	public void NotifySetAsStringMethodHeaderUsesNamedField()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiNotifySetAsStringCore.WriteRecord(ref platform, packet,
			0x8042AAAA, APTR.FromPointer(0x1400), 42));
		Assert.True(MuiSetAsStringMessageCodec.TryReadMethodId(ref platform,
			packet, out var header));
		Assert.Equal(MuiNotifySetAsStringCore.Method, header.MethodId);
		Assert.False(MuiSetAsStringMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void NotifySetAsStringParameterAddressUsesNamedTailBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var message = APTR.FromPointer(0x1800);

		Assert.True(MuiSetAsStringMessageCodec.TryGetParameters(ref platform,
			message, out var parameters));
		Assert.Equal(APTR.FromPointer(0x180C), parameters);
		Assert.False(MuiSetAsStringMessageCodec.TryGetParameters(ref platform,
			APTR.FromPointer(0x20FFC), out _));
		Assert.False(MuiSetAsStringMessageCodec.TryGetParameters(ref platform,
			APTR.FromPointer(0xFFFFFFF0), out _));
	}

	[Fact]
	public void NotifySetAsStringValueCursorUsesNamedFieldBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var cursor = default(MuiSetAsStringValueCursor);
		cursor.Message = APTR.FromPointer(0x1800);
		Assert.True(MuiSetAsStringValueCursorCodec.TryGetAddress(ref platform,
			cursor, out var value));
		Assert.Equal(APTR.FromPointer(0x180C), value);
		cursor.Message = APTR.FromPointer(0xFFFFFFF0);
		Assert.False(MuiSetAsStringValueCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	[Fact]
	public void NotifySetAsStringPacketFieldCursorUsesNamedMixedBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		var cursor = default(MuiSetAsStringPacketFieldCursor);
		cursor.Message = packet;
		cursor.Field = MuiSetAsStringPacketField.MethodId;
		Assert.True(MuiSetAsStringPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var address));
		Assert.Equal(packet.Raw, address.Raw);
		cursor.Field = MuiSetAsStringPacketField.Attribute;
		Assert.True(MuiSetAsStringPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(packet.Raw + 4, address.Raw);
		cursor.Field = MuiSetAsStringPacketField.Format;
		Assert.True(MuiSetAsStringPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(packet.Raw + 8, address.Raw);
		cursor.Field = MuiSetAsStringPacketField.Value;
		Assert.True(MuiSetAsStringPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(packet.Raw + 12, address.Raw);

		Assert.True(MuiSetAsStringPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiSetAsStringPacketField.Attribute,
			0x8042AAAA));
		Assert.True(MuiSetAsStringPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiSetAsStringPacketField.Format, 0x1500));
		Assert.True(MuiSetAsStringPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiSetAsStringPacketField.Value, 42));
		Assert.True(MuiSetAsStringPacketFieldCursorCodec.TryReadUInt32(
			ref platform, packet, MuiSetAsStringPacketField.Attribute,
			out var attribute));
		Assert.Equal(0x8042AAAAu, attribute);
		Assert.True(MuiSetAsStringPacketFieldCursorCodec.TryReadUInt32(
			ref platform, packet, MuiSetAsStringPacketField.Value, out var value));
		Assert.Equal(42u, value);

		cursor.Message = APTR.FromPointer(0xfffffff0u);
		Assert.False(MuiSetAsStringPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void BoopsiQueryUsesTheCompleteNamedPacketRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		var screen = APTR.FromPointer(0x1300);
		var renderInfo = APTR.FromPointer(0x1400);
		const uint flags = 0x000000A5;
		Assert.Equal(40u, MuiBoopsiQueryCore.PacketSize);
		Assert.True(MuiBoopsiQueryCore.WriteRecord(ref platform, packet, screen,
			flags, -8, 9, 640, 480, 320, 200, renderInfo));
		Assert.Equal(flags, MuiBoopsiQueryCore.DispatchRecord(ref platform,
			packet));
		Assert.Equal(MuiBoopsiQueryCore.Method, platform.ReadUInt32(packet, 0));
		Assert.Equal(screen.Raw, platform.ReadUInt32(packet, 4));
		Assert.Equal(renderInfo.Raw, platform.ReadUInt32(packet, 36));
		Assert.Equal(unchecked((uint)-8), platform.ReadUInt32(packet, 12));
		Assert.Equal(640u, platform.ReadUInt32(packet, 20));

		var truncated = APTR.FromPointer(0x20FFC);
		platform.WriteUInt32(truncated, 0, MuiBoopsiQueryCore.Method);
		Assert.Equal(0u, MuiBoopsiQueryCore.DispatchRecord(ref platform,
			truncated));
	}

	[Fact]
	public void BoopsiQueryMethodHeaderUsesNamedField()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiBoopsiQueryCore.WriteRecord(ref platform, packet,
			APTR.FromPointer(0x1300), 0xA5, -8, 9, 640, 480, 320, 200,
			APTR.FromPointer(0x1400)));
		Assert.True(MuiBoopsiQueryMessageCodec.TryReadMethodId(ref platform,
			packet, out var header));
		Assert.Equal(MuiBoopsiQueryCore.Method, header.MethodId);
		Assert.False(MuiBoopsiQueryMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void BoopsiQueryPacketFieldCursorUsesNamedMixedSignedBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		var cursor = default(MuiBoopsiQueryPacketFieldCursor);
		cursor.Message = packet;
		cursor.Field = MuiBoopsiQueryPacketField.MethodId;
		Assert.True(MuiBoopsiQueryPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var address));
		Assert.Equal(packet.Raw, address.Raw);
		cursor.Field = MuiBoopsiQueryPacketField.Screen;
		Assert.True(MuiBoopsiQueryPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(packet.Raw + 4, address.Raw);
		cursor.Field = MuiBoopsiQueryPacketField.MinWidth;
		Assert.True(MuiBoopsiQueryPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(packet.Raw + 12, address.Raw);
		cursor.Field = MuiBoopsiQueryPacketField.MaxHeight;
		Assert.True(MuiBoopsiQueryPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(packet.Raw + 24, address.Raw);
		cursor.Field = MuiBoopsiQueryPacketField.RenderInfo;
		Assert.True(MuiBoopsiQueryPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(packet.Raw + 36, address.Raw);

		Assert.True(MuiBoopsiQueryPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiBoopsiQueryPacketField.MinWidth,
			unchecked((uint)-8)));
		Assert.True(MuiBoopsiQueryPacketFieldCursorCodec.TryReadUInt32(
			ref platform, packet, MuiBoopsiQueryPacketField.MinWidth,
			out var minWidth));
		Assert.Equal(unchecked((uint)-8), minWidth);
		Assert.True(MuiBoopsiQueryPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiBoopsiQueryPacketField.RenderInfo, 0x1500));
		Assert.True(MuiBoopsiQueryPacketFieldCursorCodec.TryReadUInt32(
			ref platform, packet, MuiBoopsiQueryPacketField.RenderInfo,
			out var renderInfo));
		Assert.Equal(0x1500u, renderInfo);

		cursor.Message = APTR.FromPointer(0xfffffff0u);
		cursor.Field = MuiBoopsiQueryPacketField.RenderInfo;
		Assert.False(MuiBoopsiQueryPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void CallHookUsesTheNamedPacketAndPassesA1ToTheFirstParameter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		var hook = APTR.FromPointer(0x1300);
		var data = APTR.FromPointer(0x1340);
		var packet = APTR.FromPointer(0x1400);
		platform.WriteCString(name, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1));
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(obj.IsNotNull);
		platform.WriteUInt32(hook, 8,
			MuiHeadlessTestPlatform.HookEntryConstruct);
		platform.WriteUInt32(hook, 16, data.Raw);
		Assert.True(MuiCallHookCore.WriteRecord(ref platform, packet, hook,
			0xCAFEBABEu));
		Assert.Equal(12u, MuiCallHookCore.PacketSize);
		Assert.Equal(data.Raw, MuiHeadlessDispatcher.DispatchNotify(
			ref platform, State, obj, packet));
		Assert.Equal(1u, platform.HookInvokeCount);
		Assert.Equal(hook.Raw, platform.LastHookBase.Raw);
		Assert.Equal(obj.Raw, platform.LastHookA2.Raw);
		Assert.Equal(packet.Raw + 8u, platform.LastHookA1.Raw);
		Assert.Equal(hook.Raw, platform.ReadUInt32(data, 0));
		Assert.Equal(obj.Raw, platform.ReadUInt32(data, 4));
		Assert.Equal(packet.Raw + 8u, platform.ReadUInt32(data, 8));

		var truncated = APTR.FromPointer(0x20FFC);
		Assert.Equal(0u, MuiHeadlessDispatcher.DispatchNotify(ref platform,
			State, obj, truncated));
	}

	[Fact]
	public void CallHookFirstParameterUsesNamedTailBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1400);

		Assert.True(MuiCallHookMessageCodec.TryGetFirstParameter(
			ref platform, packet, out var parameter));
		Assert.Equal(APTR.FromPointer(0x1408), parameter);
		Assert.False(MuiCallHookMessageCodec.TryGetFirstParameter(
			ref platform, APTR.Null, out _));
		Assert.False(MuiCallHookMessageCodec.TryGetFirstParameter(ref platform,
			APTR.FromPointer(0x20FFC), out _));
	}

	[Fact]
	public void CallHookParameterCursorUsesNamedEntryBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var cursor = default(MuiCallHookParameterCursor);
		cursor.Message = APTR.FromPointer(0x1400);
		cursor.Index = 1;
		Assert.True(MuiCallHookParameterCursorCodec.TryGetEntry(ref platform,
			cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x140C), address);
		cursor.Message = APTR.FromPointer(0xFFFFFFFC);
		cursor.Index = 0;
		Assert.False(MuiCallHookParameterCursorCodec.TryGetEntry(ref platform,
			cursor, out _));
	}

	[Fact]
	public void CallHookMethodHeaderUsesNamedField()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1400);
		Assert.True(MuiCallHookCore.WriteRecord(ref platform, packet,
			APTR.FromPointer(0x1300), 0xCAFEBABEu));
		Assert.True(MuiCallHookMessageCodec.TryReadMethodId(ref platform,
			packet, out var header));
		Assert.Equal(MuiCallHookCore.Method, header.MethodId);
		Assert.False(MuiCallHookMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void CallHookPacketFieldCursorUsesNamedMixedBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1400);
		var cursor = default(MuiCallHookPacketFieldCursor);
		cursor.Message = packet;
		cursor.Field = MuiCallHookPacketField.MethodId;
		Assert.True(MuiCallHookPacketFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var address));
		Assert.Equal(packet.Raw, address.Raw);
		cursor.Field = MuiCallHookPacketField.Hook;
		Assert.True(MuiCallHookPacketFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 4, address.Raw);
		cursor.Field = MuiCallHookPacketField.Param1;
		Assert.True(MuiCallHookPacketFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 8, address.Raw);

		Assert.True(MuiCallHookPacketFieldCursorCodec.TryWriteUInt32(ref platform,
			packet, MuiCallHookPacketField.Hook, 0x1500));
		Assert.True(MuiCallHookPacketFieldCursorCodec.TryWriteUInt32(ref platform,
			packet, MuiCallHookPacketField.Param1, 0xCAFEBABEu));
		Assert.True(MuiCallHookPacketFieldCursorCodec.TryReadUInt32(ref platform,
			packet, MuiCallHookPacketField.Hook, out var hook));
		Assert.Equal(0x1500u, hook);
		Assert.True(MuiCallHookPacketFieldCursorCodec.TryReadUInt32(ref platform,
			packet, MuiCallHookPacketField.Param1, out var param1));
		Assert.Equal(0xCAFEBABEu, param1);

		cursor.Message = APTR.FromPointer(0xfffffff0u);
		cursor.Field = MuiCallHookPacketField.Param1;
		Assert.False(MuiCallHookPacketFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	[Fact]
	public void UpdateConfigUsesNamedHeaderAndCompleteRedrawTables()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var message = APTR.FromPointer(0x1500);
		var first = APTR.FromPointer(0x1600);
		var last = APTR.FromPointer(0x1700);
		const uint cfgId = 0x8042BEEF;
		Assert.Equal(332u, MuiUpdateConfigCore.PacketSize);
		Assert.True(MuiUpdateConfigCore.WriteRecord(ref platform, message, cfgId,
			2));
		Assert.True(MuiUpdateConfigCore.WriteEntry(ref platform, message, 0,
			first, 0x11));
		Assert.True(MuiUpdateConfigCore.WriteEntry(ref platform, message, 63,
			last, 0xA5));
		Assert.Equal(cfgId, MuiUpdateConfigCore.DispatchRecord(ref platform,
			message));
		Assert.Equal(MuiUpdateConfigCore.Method, platform.ReadUInt32(message, 0));
		Assert.Equal(cfgId, platform.ReadUInt32(message, 4));
		Assert.Equal(2u, platform.ReadUInt32(message, 8));
		Assert.Equal(first.Raw, platform.ReadUInt32(message, 12));
		Assert.Equal(last.Raw, platform.ReadUInt32(message, 12 + (63 * 4)));
		Assert.Equal((byte)0x11, platform.ReadUInt8(message, 268));
		Assert.Equal((byte)0xA5, platform.ReadUInt8(message, 268 + 63));
		Assert.False(MuiUpdateConfigCore.WriteEntry(ref platform, message, 64,
			first, 1));
		Assert.False(MuiUpdateConfigCore.WriteRecord(ref platform, message,
			cfgId, 65));

		var truncated = APTR.FromPointer(0x20FFC);
		platform.WriteUInt32(truncated, 0, MuiUpdateConfigCore.Method);
		Assert.Equal(0u, MuiUpdateConfigCore.DispatchRecord(ref platform,
			truncated));
	}

	[Fact]
	public void UpdateConfigMethodHeaderUsesNamedField()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var message = APTR.FromPointer(0x1500);
		Assert.True(MuiUpdateConfigCore.WriteRecord(ref platform, message, 7, 0));
		Assert.True(MuiUpdateConfigCore.TryReadMethodId(ref platform, message,
			out var header));
		Assert.Equal(MuiUpdateConfigCore.Method, header.MethodId);
		Assert.False(MuiUpdateConfigCore.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void UpdateConfigPacketFieldCursorUsesNamedHeaderBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var message = APTR.FromPointer(0x1500);
		var cursor = default(MuiUpdateConfigPacketFieldCursor);
		cursor.Message = message;
		cursor.Field = MuiUpdateConfigPacketField.MethodId;
		Assert.True(MuiUpdateConfigPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var address));
		Assert.Equal(message.Raw, address.Raw);
		cursor.Field = MuiUpdateConfigPacketField.CfgId;
		Assert.True(MuiUpdateConfigPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(message.Raw + 4, address.Raw);
		cursor.Field = MuiUpdateConfigPacketField.RedrawCount;
		Assert.True(MuiUpdateConfigPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(message.Raw + 8, address.Raw);

		Assert.True(MuiUpdateConfigPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, message, MuiUpdateConfigPacketField.CfgId,
			0x8042BEEFu));
		Assert.True(MuiUpdateConfigPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, message, MuiUpdateConfigPacketField.RedrawCount,
			unchecked((uint)-1)));
		Assert.True(MuiUpdateConfigPacketFieldCursorCodec.TryReadUInt32(
			ref platform, message, MuiUpdateConfigPacketField.CfgId,
			out var cfgId));
		Assert.Equal(0x8042BEEFu, cfgId);
		Assert.True(MuiUpdateConfigPacketFieldCursorCodec.TryReadUInt32(
			ref platform, message, MuiUpdateConfigPacketField.RedrawCount,
			out var redrawCount));
		Assert.Equal(unchecked((uint)-1), redrawCount);

		cursor.Message = APTR.FromPointer(0xfffffff0u);
		Assert.False(MuiUpdateConfigPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void UpdateConfigObjectSlotCodecUsesNamedPointer()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var slotAddress = APTR.FromPointer(0x1500);
		var slot = default(MuiUpdateConfigObjectSlot);
		slot.Object = APTR.FromPointer(0x1A00);
		Assert.True(MuiUpdateConfigObjectSlotCodec.Write(ref platform,
			slotAddress, slot));
		Assert.True(MuiUpdateConfigObjectSlotCodec.TryRead(ref platform,
			slotAddress, out var decoded));
		Assert.Equal(slot.Object, decoded.Object);
	}

	[Fact]
	public void UpdateConfigFlagSlotCodecUsesNamedByte()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var slotAddress = APTR.FromPointer(0x1500);
		var slot = default(MuiUpdateConfigFlagSlot);
		slot.Value = 0xA5;
		Assert.True(MuiUpdateConfigFlagSlotCodec.Write(ref platform,
			slotAddress, slot));
		Assert.True(MuiUpdateConfigFlagSlotCodec.TryRead(ref platform,
			slotAddress, out var decoded));
		Assert.Equal(slot.Value, decoded.Value);
		Assert.False(MuiUpdateConfigFlagSlotCodec.TryRead(ref platform,
			APTR.FromPointer(0x21000), out _));
	}

	[Fact]
	public void UpdateConfigTableCursorsUseNamedEntryBoundaries()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var objectCursor = default(MuiUpdateConfigObjectCursor);
		objectCursor.Base = APTR.FromPointer(0x1500);
		objectCursor.Index = 63;
		Assert.True(MuiUpdateConfigObjectCursorCodec.TryGetEntry(
			ref platform, objectCursor, out var objectAddress));
		Assert.Equal(APTR.FromPointer(0x15FC), objectAddress);
		objectCursor.Index = 64;
		Assert.False(MuiUpdateConfigObjectCursorCodec.TryGetEntry(
			ref platform, objectCursor, out _));

		var flagCursor = default(MuiUpdateConfigFlagCursor);
		flagCursor.Base = APTR.FromPointer(0x1800);
		flagCursor.Index = 63;
		Assert.True(MuiUpdateConfigFlagCursorCodec.TryGetEntry(ref platform,
			flagCursor, out var flagAddress));
		Assert.Equal(APTR.FromPointer(0x183F), flagAddress);
		flagCursor.Index = 64;
		Assert.False(MuiUpdateConfigFlagCursorCodec.TryGetEntry(ref platform,
			flagCursor, out _));
		flagCursor.Base = APTR.FromPointer(0xFFFFFFFF);
		flagCursor.Index = 1;
		Assert.False(MuiUpdateConfigFlagCursorCodec.TryGetEntry(ref platform,
			flagCursor, out _));
	}

	[Fact]
	public void FamilyChildMutationsUseTheNamedPacketForHeadAndTail()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Family.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		var family = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(family.IsNotNull && first.IsNotNull && second.IsNotNull);
		var packet = APTR.FromPointer(0x1200);

		Assert.True(MuiFamilyMutationCore.WriteRecord(ref platform, packet,
			MuiFamilyMutationCore.AddTailMethod, first));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));
		Assert.True(MuiFamilyMutationCore.WriteRecord(ref platform, packet,
			MuiFamilyMutationCore.AddHeadMethod, second));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));
		Assert.Equal(second.Raw, MuiFamilyCore.GetChild(ref platform, State,
			family, 0, APTR.Null).Raw);
		Assert.Equal(first.Raw, MuiFamilyCore.GetChild(ref platform, State,
			family, 1, APTR.Null).Raw);
		Assert.True(MuiFamilyMutationCore.WriteRecord(ref platform, packet,
			MuiFamilyMutationCore.RemoveMethod, second));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));
		Assert.Equal(first.Raw, MuiFamilyCore.GetChild(ref platform, State,
			family, 0, APTR.Null).Raw);
		Assert.Equal(0u, MuiFamilyCore.GetChild(ref platform, State, family, 1,
			APTR.Null).Raw);
		Assert.True(MuiFamilyMutationCore.WriteRecord(ref platform, packet,
			MuiFamilyMutationCore.RemoveMethod, second));
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));
		Assert.True(MuiFamilyMutationCore.WriteInsertRecord(ref platform, packet,
			second, first));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));
		Assert.Equal(first.Raw, MuiFamilyCore.GetChild(ref platform, State,
			family, 0, APTR.Null).Raw);
		Assert.Equal(second.Raw, MuiFamilyCore.GetChild(ref platform, State,
			family, 1, APTR.Null).Raw);

		var truncated = APTR.FromPointer(0x20FFC);
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, truncated));
		Assert.False(MuiFamilyMutationCore.WriteRecord(ref platform, packet,
			0x80420000, first));
	}

	[Fact]
	public void FamilyMutationPacketCodecRejectsTruncatedChildRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x20FFC);
		platform.WriteUInt32(packet, 0, MuiFamilyMutationCore.AddHeadMethod);
		Assert.Equal(0u, MuiFamilyMutationCore.DispatchRecord(ref platform, State,
			APTR.Null, packet));
		Assert.False(MuiFamilyMutationCore.WriteRecord(ref platform, packet,
			MuiFamilyMutationCore.AddHeadMethod, APTR.FromPointer(0x1200)));
	}

	[Fact]
	public void FamilyMethodHeaderUsesNamedField()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiFamilyMutationCore.WriteRecord(ref platform, packet,
			MuiFamilyMutationCore.AddHeadMethod, APTR.FromPointer(0x1300)));
		Assert.True(MuiFamilyMutationMessageCodec.TryReadMethodId(ref platform,
			packet, out var header));
		Assert.Equal(MuiFamilyMutationCore.AddHeadMethod, header.MethodId);
		Assert.False(MuiFamilyMutationMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void FamilyMutationReadersUseNamedMethodHeader()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiFamilyMutationCore.WriteInsertRecord(ref platform, packet,
			APTR.FromPointer(0x1300), APTR.Null));
		Assert.True(MuiFamilyMutationMessageCodec.TryReadInsert(ref platform,
			packet, out var insert));
		Assert.Equal(MuiFamilyMutationCore.InsertMethod, insert.MethodId);
		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.False(MuiFamilyMutationMessageCodec.TryReadInsert(ref platform,
			packet, out _));
	}

	[Fact]
	public void CollectionAdvancedPacketCodecUsesNamedRecordsAndRejectsTruncation()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		const uint entry = 0x1300;
		const uint image = 0x1340;

		Assert.True(MuiCollectionAdvancedMessageCodec.WriteInsertSingle(ref platform,
			packet, entry, unchecked((uint)-1)));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadInsertSingle(ref platform,
			packet, out var single));
		Assert.Equal(entry, single.Entry);
		Assert.Equal(unchecked((uint)-1), single.Position);

		Assert.True(MuiCollectionAdvancedMessageCodec.WriteInsert(ref platform,
			packet, entry, unchecked((uint)-3), 2));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadInsert(ref platform,
			packet, out var insert));
		Assert.Equal(entry, insert.Entry);
		Assert.Equal(unchecked((uint)-3), insert.Position);
		Assert.Equal(2u, insert.Column);

		Assert.True(MuiCollectionAdvancedMessageCodec.WritePair(ref platform, packet,
			MuiCollectionAdvancedMessageCodec.Move, 1, 3));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadPair(ref platform,
			packet, MuiCollectionAdvancedMessageCodec.Move, out var move));
		Assert.Equal(1u, move.First);
		Assert.Equal(3u, move.Second);

		Assert.True(MuiCollectionAdvancedMessageCodec.WritePosition(ref platform,
			packet, MuiCollectionAdvancedMessageCodec.Jump, unchecked((uint)-2)));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadPosition(ref platform,
			packet, MuiCollectionAdvancedMessageCodec.Jump, out var jump));
		Assert.Equal(unchecked((uint)-2), jump.Position);

		Assert.True(MuiCollectionAdvancedMessageCodec.WritePointer(ref platform,
			packet, MuiCollectionAdvancedMessageCodec.DeleteImage, image));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadPointer(ref platform,
			packet, MuiCollectionAdvancedMessageCodec.DeleteImage,
			out var deleteImage));
		Assert.Equal(image, deleteImage.Pointer);

		Assert.True(MuiCollectionAdvancedMessageCodec.WriteCreateImage(ref platform,
			packet, image, 3));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadCreateImage(ref platform,
			packet, out var createImage));
		Assert.Equal(image, createImage.Image);
		Assert.Equal(3u, createImage.Flags);

		Assert.False(MuiCollectionAdvancedMessageCodec.TryReadInsert(ref platform,
			APTR.FromPointer(0x20FFF), out _));
		Assert.False(MuiCollectionAdvancedMessageCodec.WriteCreateImage(ref platform,
			APTR.FromPointer(0x20FFF), image, 3));
		Assert.False(MuiCollectionAdvancedMessageCodec.TryReadPosition(ref platform,
			packet, 0x80420000u, out _));
	}

	[Fact]
	public void CollectionAdvancedFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		var cursor = default(MuiCollectionAdvancedFieldCursor);
		cursor.Message = packet;
		cursor.Packet = MuiCollectionAdvancedPacketKind.Insert;
		cursor.Field = MuiCollectionAdvancedField.MethodId;
		Assert.True(MuiCollectionAdvancedFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var address));
		Assert.Equal(0x1200u, address.Raw);
		cursor.Field = MuiCollectionAdvancedField.Entry;
		Assert.True(MuiCollectionAdvancedFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(0x1204u, address.Raw);
		cursor.Field = MuiCollectionAdvancedField.Position;
		Assert.True(MuiCollectionAdvancedFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(0x1208u, address.Raw);
		cursor.Field = MuiCollectionAdvancedField.Column;
		Assert.True(MuiCollectionAdvancedFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(0x120Cu, address.Raw);

		Assert.True(MuiCollectionAdvancedFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiCollectionAdvancedPacketKind.Pair,
			MuiCollectionAdvancedField.First, unchecked((uint)-2)));
		Assert.True(MuiCollectionAdvancedFieldCursorCodec.TryReadUInt32(
			ref platform, packet, MuiCollectionAdvancedPacketKind.Pair,
			MuiCollectionAdvancedField.First, out var first));
		Assert.Equal(unchecked((uint)-2), first);
		cursor.Packet = MuiCollectionAdvancedPacketKind.Pointer;
		cursor.Field = MuiCollectionAdvancedField.Entry;
		Assert.False(MuiCollectionAdvancedFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
		cursor.Message = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Packet = MuiCollectionAdvancedPacketKind.CreateImage;
		cursor.Field = MuiCollectionAdvancedField.Flags;
		Assert.False(MuiCollectionAdvancedFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void CollectionBasicPacketCodecUsesNamedRecordsAndRejectsTruncation()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1400);
		const uint storage = 0x1500;

		Assert.True(MuiCollectionBasicMessageCodec.WriteGetEntry(ref platform,
			packet, unchecked((uint)-2), storage));
		Assert.True(MuiCollectionBasicMessageCodec.TryReadGetEntry(ref platform,
			packet, out var getEntry));
		Assert.Equal(unchecked((uint)-2), getEntry.Position);
		Assert.Equal(storage, getEntry.Storage);

		Assert.True(MuiCollectionBasicMessageCodec.WriteSelect(ref platform,
			packet, 4, 2, storage));
		Assert.True(MuiCollectionBasicMessageCodec.TryReadSelect(ref platform,
			packet, out var select));
		Assert.Equal(4u, select.Position);
		Assert.Equal(2u, select.Select);
		Assert.Equal(storage, select.Storage);

		Assert.True(MuiCollectionBasicMessageCodec.WriteMethod(ref platform,
			packet, MuiCollectionBasicMessageCodec.Clear));
		Assert.True(MuiCollectionBasicMessageCodec.TryReadMethod(ref platform,
			packet, MuiCollectionBasicMessageCodec.Clear, out var clear));
		Assert.Equal(MuiCollectionBasicMessageCodec.Clear, clear.MethodId);
		Assert.True(MuiCollectionBasicMessageCodec.WriteMethod(ref platform,
			packet, MuiCollectionBasicMessageCodec.Sort));
		Assert.True(MuiCollectionBasicMessageCodec.TryReadMethod(ref platform,
			packet, MuiCollectionBasicMessageCodec.Sort, out var sort));
		Assert.Equal(MuiCollectionBasicMessageCodec.Sort, sort.MethodId);

		Assert.False(MuiCollectionBasicMessageCodec.TryReadSelect(ref platform,
			APTR.FromPointer(0x20FFF), out _));
		Assert.False(MuiCollectionBasicMessageCodec.WriteMethod(ref platform,
			packet, 0x80420000u));
		Assert.False(MuiCollectionBasicMessageCodec.TryReadMethod(ref platform,
			packet, 0x80420000u, out _));
	}

	[Fact]
	public void CollectionBasicFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1400);
		var cursor = default(MuiCollectionBasicFieldCursor);
		cursor.Message = packet;
		cursor.Packet = MuiCollectionBasicPacketKind.Select;
		cursor.Field = MuiCollectionBasicField.MethodId;
		Assert.True(MuiCollectionBasicFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var address));
		Assert.Equal(0x1400u, address.Raw);
		cursor.Field = MuiCollectionBasicField.Position;
		Assert.True(MuiCollectionBasicFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(0x1404u, address.Raw);
		cursor.Field = MuiCollectionBasicField.Select;
		Assert.True(MuiCollectionBasicFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(0x1408u, address.Raw);
		cursor.Field = MuiCollectionBasicField.Storage;
		Assert.True(MuiCollectionBasicFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(0x140Cu, address.Raw);

		Assert.True(MuiCollectionBasicFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiCollectionBasicPacketKind.Select,
			MuiCollectionBasicField.Storage, 0x5500));
		Assert.True(MuiCollectionBasicFieldCursorCodec.TryReadUInt32(
			ref platform, packet, MuiCollectionBasicPacketKind.Select,
			MuiCollectionBasicField.Storage, out var storage));
		Assert.Equal(0x5500u, storage);
		cursor.Packet = MuiCollectionBasicPacketKind.GetEntry;
		cursor.Field = MuiCollectionBasicField.Select;
		Assert.False(MuiCollectionBasicFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
		cursor.Message = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Packet = MuiCollectionBasicPacketKind.Select;
		cursor.Field = MuiCollectionBasicField.Storage;
		Assert.False(MuiCollectionBasicFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void CollectionSurfacePacketCodecUsesNamedRecordsAndRejectsTruncation()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1600);
		const uint storage = 0x1700;

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteLayout(ref platform,
			packet, 1, 2, 80, 40));
		Assert.True(MuiCollectionSurfaceMessageCodec.TryReadLayout(ref platform,
			packet, out var layout));
		Assert.Equal(1u, layout.Left);
		Assert.Equal(2u, layout.Top);
		Assert.Equal(80u, layout.Width);
		Assert.Equal(40u, layout.Height);

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteAskMinMax(ref platform,
			packet, storage));
		Assert.True(MuiCollectionSurfaceMessageCodec.TryReadAskMinMax(ref platform,
			packet, out var askMinMax));
		Assert.Equal(storage, askMinMax.Storage);

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteDraw(ref platform,
			packet, 3));
		Assert.True(MuiCollectionSurfaceMessageCodec.TryReadDraw(ref platform,
			packet, out var draw));
		Assert.Equal(3u, draw.Flags);

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteAttribute(ref platform,
			packet, MuiCollectionSurfaceMessageCodec.Set, 0x120, 0x456));
		Assert.True(MuiCollectionSurfaceMessageCodec.TryReadAttribute(ref platform,
			packet, MuiCollectionSurfaceMessageCodec.Set, out var attribute));
		Assert.Equal(0x120u, attribute.Attribute);
		Assert.Equal(0x456u, attribute.Value);

		Assert.False(MuiCollectionSurfaceMessageCodec.TryReadLayout(ref platform,
			APTR.FromPointer(0x20FFF), out _));
		Assert.False(MuiCollectionSurfaceMessageCodec.WriteAttribute(ref platform,
			packet, 0x80420000u, 1, 2));
		Assert.False(MuiCollectionSurfaceMessageCodec.TryReadAttribute(ref platform,
			packet, 0x80420000u, out _));
	}

	[Fact]
	public void CollectionSurfaceFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1600);
		var cursor = default(MuiCollectionSurfaceFieldCursor);
		cursor.Message = packet;
		cursor.Packet = MuiCollectionSurfacePacketKind.Layout;
		cursor.Field = MuiCollectionSurfaceField.MethodId;
		Assert.True(MuiCollectionSurfaceFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var address));
		Assert.Equal(0x1600u, address.Raw);
		cursor.Field = MuiCollectionSurfaceField.Left;
		Assert.True(MuiCollectionSurfaceFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(0x1604u, address.Raw);
		cursor.Field = MuiCollectionSurfaceField.Top;
		Assert.True(MuiCollectionSurfaceFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(0x1608u, address.Raw);
		cursor.Field = MuiCollectionSurfaceField.Width;
		Assert.True(MuiCollectionSurfaceFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(0x160Cu, address.Raw);
		cursor.Field = MuiCollectionSurfaceField.Height;
		Assert.True(MuiCollectionSurfaceFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(0x1610u, address.Raw);

		Assert.True(MuiCollectionSurfaceFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiCollectionSurfacePacketKind.HandleInput,
			MuiCollectionSurfaceField.MuiKey, unchecked((uint)-7)));
		Assert.True(MuiCollectionSurfaceFieldCursorCodec.TryReadUInt32(
			ref platform, packet, MuiCollectionSurfacePacketKind.HandleInput,
			MuiCollectionSurfaceField.MuiKey, out var rawMuiKey));
		Assert.Equal(-7, unchecked((int)rawMuiKey));
		cursor.Packet = MuiCollectionSurfacePacketKind.Draw;
		cursor.Field = MuiCollectionSurfaceField.Storage;
		Assert.False(MuiCollectionSurfaceFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
		cursor.Message = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Packet = MuiCollectionSurfacePacketKind.Attribute;
		cursor.Field = MuiCollectionSurfaceField.Value;
		Assert.False(MuiCollectionSurfaceFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void CollectionSurfaceMethodHeaderUsesNamedField()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1600);
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteDraw(ref platform,
			packet, 7));
		Assert.True(MuiCollectionBasicMessageCodec.TryReadMethodId(ref platform,
			packet, out var header));
		Assert.Equal(MuiCollectionSurfaceMessageCodec.Draw, header.MethodId);
		Assert.True(MuiCollectionSurfaceMessageCodec.TryReadDraw(ref platform,
			packet, out var draw));
		Assert.Equal(7u, draw.Flags);
		Assert.True(MuiCollectionBasicFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiCollectionBasicPacketKind.Method,
			MuiCollectionBasicField.MethodId, 0xDEADBEEFu));
		Assert.False(MuiCollectionSurfaceMessageCodec.TryReadDraw(ref platform,
			packet, out _));
	}

	[Fact]
	public void FamilyTransferUsesTheNamedPacketAndMovesSourceChildren()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Family.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		var destination = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			cl, APTR.Null);
		var source = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var moved = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(destination.IsNotNull && source.IsNotNull &&
			first.IsNotNull && second.IsNotNull && moved.IsNotNull);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, destination, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, destination, second));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, source, moved));
		var packet = APTR.FromPointer(0x1250);
		Assert.True(MuiFamilyMutationCore.WriteTransferRecord(ref platform, packet,
			source));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			destination, packet));
		Assert.Equal(first.Raw, MuiFamilyCore.GetChild(ref platform, State,
			destination, 0, APTR.Null).Raw);
		Assert.Equal(second.Raw, MuiFamilyCore.GetChild(ref platform, State,
			destination, 1, APTR.Null).Raw);
		Assert.Equal(moved.Raw, MuiFamilyCore.GetChild(ref platform, State,
			destination, 2, APTR.Null).Raw);
		Assert.Equal(0u, MuiFamilyCore.GetChild(ref platform, State, source, 0,
			APTR.Null).Raw);
		var truncated = APTR.FromPointer(0x20FFC);
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			destination, truncated));
	}

	[Fact]
	public void FamilyOrderingUsesNamedHeadersAndVectors()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Family.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		var family = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var third = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(family.IsNotNull && first.IsNotNull && second.IsNotNull &&
			third.IsNotNull);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, family, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, family, second));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, family, third));
		var packet = APTR.FromPointer(0x1280);
		Assert.True(MuiFamilyMutationCore.WriteReorderRecord(ref platform, packet,
			APTR.Null));
		Assert.True(MuiFamilyMutationCore.WriteVectorEntry(ref platform, packet,
			MuiFamilyMutationCore.ReorderArrayOffset, 0, third));
		Assert.True(MuiFamilyMutationCore.WriteVectorEntry(ref platform, packet,
			MuiFamilyMutationCore.ReorderArrayOffset, 1, first));
		Assert.True(MuiFamilyMutationCore.WriteVectorEntry(ref platform, packet,
			MuiFamilyMutationCore.ReorderArrayOffset, 2, second));
		Assert.True(MuiFamilyMutationCore.WriteVectorEntry(ref platform, packet,
			MuiFamilyMutationCore.ReorderArrayOffset, 3, APTR.Null));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));
		Assert.Equal(third.Raw, MuiFamilyCore.GetChild(ref platform, State,
			family, 0, APTR.Null).Raw);
		Assert.Equal(first.Raw, MuiFamilyCore.GetChild(ref platform, State,
			family, 1, APTR.Null).Raw);

		Assert.True(MuiFamilyMutationCore.WriteSortRecord(ref platform, packet));
		Assert.True(MuiFamilyMutationCore.WriteVectorEntry(ref platform, packet,
			MuiFamilyMutationCore.SortArrayOffset, 0, second));
		Assert.True(MuiFamilyMutationCore.WriteVectorEntry(ref platform, packet,
			MuiFamilyMutationCore.SortArrayOffset, 1, third));
		Assert.True(MuiFamilyMutationCore.WriteVectorEntry(ref platform, packet,
			MuiFamilyMutationCore.SortArrayOffset, 2, first));
		Assert.True(MuiFamilyMutationCore.WriteVectorEntry(ref platform, packet,
			MuiFamilyMutationCore.SortArrayOffset, 3, APTR.Null));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));
		Assert.Equal(second.Raw, MuiFamilyCore.GetChild(ref platform, State,
			family, 0, APTR.Null).Raw);
		Assert.Equal(third.Raw, MuiFamilyCore.GetChild(ref platform, State,
			family, 1, APTR.Null).Raw);
		Assert.Equal(first.Raw, MuiFamilyCore.GetChild(ref platform, State,
			family, 2, APTR.Null).Raw);
	}

	[Fact]
	public void FamilyProjectionListAndVectorUseNamedRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var list = APTR.FromPointer(0x1500);
		var vector = APTR.FromPointer(0x1510);
		var listRecord = new MuiFamilyMutationListRecord
		{
			Head = APTR.FromPointer(0x1600),
			Tail = APTR.FromPointer(0x1700)
		};
		Assert.True(MuiFamilyMutationListCodec.Write(ref platform, list,
			listRecord));
		Assert.True(MuiFamilyMutationListCodec.TryRead(ref platform, list,
			out var decodedList));
		Assert.Equal(listRecord.Head, decodedList.Head);
		Assert.Equal(listRecord.Tail, decodedList.Tail);

		var vectorRecord = new MuiFamilyMutationVectorEntry
		{
			Object = APTR.FromPointer(0x1800)
		};
		Assert.True(MuiFamilyMutationVectorCodec.Write(ref platform, vector,
			vectorRecord));
		Assert.True(MuiFamilyMutationVectorCodec.TryRead(ref platform, vector,
			out var decodedVector));
		Assert.Equal(vectorRecord.Object, decodedVector.Object);
		Assert.False(MuiFamilyMutationListCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFC), out _));
		Assert.False(MuiFamilyMutationVectorCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFF), out _));
	}

	[Fact]
	public void FamilyGetChildSelectorsUseTheTypedPacketAndReference()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Family.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		var family = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var third = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(family.IsNotNull && first.IsNotNull && second.IsNotNull &&
			third.IsNotNull);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, family, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, family, second));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, family, third));
		var packet = APTR.FromPointer(0x1200);

		Assert.True(MuiFamilyGetChildCore.WriteRecord(ref platform, packet, 0,
			APTR.Null));
		Assert.Equal(first.Raw, MuiHeadlessDispatcher.Dispatch(ref platform,
			State, family, packet));
		Assert.True(MuiFamilyGetChildCore.WriteRecord(ref platform, packet, -1,
			APTR.Null));
		Assert.Equal(third.Raw, MuiHeadlessDispatcher.Dispatch(ref platform,
			State, family, packet));
		Assert.True(MuiFamilyGetChildCore.WriteRecord(ref platform, packet, -2,
			first));
		Assert.Equal(second.Raw, MuiHeadlessDispatcher.Dispatch(ref platform,
			State, family, packet));
		Assert.True(MuiFamilyGetChildCore.WriteRecord(ref platform, packet, -4,
			second));
		Assert.Equal(third.Raw, MuiHeadlessDispatcher.Dispatch(ref platform,
			State, family, packet));
		Assert.True(MuiFamilyGetChildCore.WriteRecord(ref platform, packet, -3,
			third));
		Assert.Equal(second.Raw, MuiHeadlessDispatcher.Dispatch(ref platform,
			State, family, packet));
		Assert.True(MuiFamilyGetChildCore.WriteRecord(ref platform, packet, -3,
			APTR.Null));
		Assert.Equal(third.Raw, MuiHeadlessDispatcher.Dispatch(ref platform,
			State, family, packet));

		Assert.True(MuiFamilyGetChildCore.WriteRecord(ref platform, packet, -5,
			APTR.Null));
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));
		Assert.True(MuiFamilyGetChildCore.WriteRecord(ref platform, packet, -2,
			APTR.FromPointer(0x7000)));
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));
	}

	[Fact]
	public void FamilyGetChildPacketCodecRejectsTruncatedGuestRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x20FF8);
		Assert.False(MuiFamilyGetChildCore.WriteRecord(ref platform, packet, 0,
			APTR.Null));
		platform.WriteUInt32(packet, 0, MUIM_Family_GetChild);
		Assert.Equal(0u, MuiFamilyGetChildCore.DispatchRecord(ref platform,
			APTR.Null, APTR.Null, packet));
	}

	[Fact]
	public void FamilyGetChildMethodHeaderUsesNamedField()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiFamilyGetChildCore.WriteRecord(ref platform, packet, 0,
			APTR.Null));
		Assert.True(MuiFamilyGetChildMessageCodec.TryReadMethodId(ref platform,
			packet, out var header));
		Assert.Equal(MUIM_Family_GetChild, header.MethodId);
		Assert.False(MuiFamilyGetChildMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void FamilyGetChildPacketFieldCursorUsesNamedSignedBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		var cursor = default(MuiFamilyGetChildPacketFieldCursor);
		cursor.Message = packet;
		cursor.Field = MuiFamilyGetChildPacketField.MethodId;
		Assert.True(MuiFamilyGetChildPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var address));
		Assert.Equal(packet.Raw, address.Raw);
		cursor.Field = MuiFamilyGetChildPacketField.Number;
		Assert.True(MuiFamilyGetChildPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(packet.Raw + 4, address.Raw);
		cursor.Field = MuiFamilyGetChildPacketField.Reference;
		Assert.True(MuiFamilyGetChildPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(packet.Raw + 8, address.Raw);

		Assert.True(MuiFamilyGetChildPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiFamilyGetChildPacketField.Number,
			unchecked((uint)-3)));
		Assert.True(MuiFamilyGetChildPacketFieldCursorCodec.TryReadUInt32(
			ref platform, packet, MuiFamilyGetChildPacketField.Number,
			out var number));
		Assert.Equal(unchecked((uint)-3), number);
		Assert.True(MuiFamilyGetChildPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiFamilyGetChildPacketField.Reference, 0x1500));
		Assert.True(MuiFamilyGetChildPacketFieldCursorCodec.TryReadUInt32(
			ref platform, packet, MuiFamilyGetChildPacketField.Reference,
			out var reference));
		Assert.Equal(0x1500u, reference);

		cursor.Message = APTR.FromPointer(0xfffffff0u);
		cursor.Field = MuiFamilyGetChildPacketField.Reference;
		Assert.False(MuiFamilyGetChildPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void FamilyDoChildMethodsForwardsTheNamedMessageToEveryChild()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Family.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1));
		var family = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var third = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(family.IsNotNull && first.IsNotNull && second.IsNotNull &&
			third.IsNotNull);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, family, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, family, second));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, family, third));
		var packet = APTR.FromPointer(0x1250);
		Assert.True(MuiFamilyDoChildMethodsCore.WriteRecord(ref platform, packet));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, packet));
		Assert.Equal(3u, platform.DispatchCount);
		Assert.Equal(third.Raw, platform.LastDispatchObject.Raw);
		Assert.Equal(MuiFamilyDoChildMethodsCore.Method,
			platform.LastDispatchMethod);

		var truncated = APTR.FromPointer(0x20FFC);
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			family, truncated));
	}

	[Fact]
	public void FamilyDoChildMethodsPacketCodecRejectsTruncatedGuestRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x20FFF);
		Assert.False(MuiFamilyDoChildMethodsCore.WriteRecord(ref platform,
			packet));
		Assert.Equal(0u, MuiFamilyDoChildMethodsCore.DispatchRecord(ref platform,
			APTR.Null, APTR.Null, packet));
	}

	[Fact]
	public void FamilyDoChildMethodsMethodHeaderUsesNamedField()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiFamilyDoChildMethodsCore.WriteRecord(ref platform, packet));
		Assert.True(MuiFamilyDoChildMethodsMessageCodec.TryReadMethodId(
			ref platform, packet, out var header));
		Assert.Equal(MuiFamilyDoChildMethodsCore.Method, header.MethodId);
		Assert.False(MuiFamilyDoChildMethodsMessageCodec.TryReadMethodId(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void FamilyDoChildMethodsPacketFieldCursorUsesNamedMethodBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		var cursor = default(MuiFamilyDoChildMethodsPacketFieldCursor);
		cursor.Message = packet;
		cursor.Field = MuiFamilyDoChildMethodsPacketField.MethodId;
		Assert.True(MuiFamilyDoChildMethodsPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var address));
		Assert.Equal(packet.Raw, address.Raw);
		Assert.True(MuiFamilyDoChildMethodsPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet,
			MuiFamilyDoChildMethodsPacketField.MethodId, 0x80429A3Cu));
		Assert.True(MuiFamilyDoChildMethodsPacketFieldCursorCodec.TryReadUInt32(
			ref platform, packet,
			MuiFamilyDoChildMethodsPacketField.MethodId, out var method));
		Assert.Equal(0x80429A3Cu, method);
		cursor.Message = APTR.FromPointer(0x1fff0u);
		Assert.True(MuiFamilyDoChildMethodsPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(0x1fff0u, address.Raw);
		cursor.Message = APTR.Null;
		Assert.False(MuiFamilyDoChildMethodsPacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void GroupChangePacketsTrackNestedBracketAndUnderflow()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var groupName = APTR.FromPointer(0x1100);
		var subclassName = APTR.FromPointer(0x1140);
		var ordinaryName = APTR.FromPointer(0x1180);
		platform.WriteCString(groupName, "Group.mui");
		platform.WriteCString(subclassName, "MyGroup.mui");
		platform.WriteCString(ordinaryName, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var groupClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, groupName, APTR.Null, 0, APTR.FromPointer(1));
		var subclass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, subclassName, MuiHeadlessObjectCore.ClassPointer(ref platform,
				groupClass), 0, APTR.FromPointer(1));
		var ordinaryClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, ordinaryName, APTR.Null, 0, APTR.FromPointer(1));
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var childGroup = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			subclass, APTR.Null);
		var ordinary = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			ordinaryClass, APTR.Null);
		Assert.True(group.IsNotNull && childGroup.IsNotNull && ordinary.IsNotNull);
		var packet = APTR.FromPointer(0x1200);

		Assert.True(MuiGroupChangeCore.WriteInitChangeRecord(ref platform,
			packet));
		Assert.Equal(group.Raw, MuiHeadlessDispatcher.Dispatch(ref platform,
			State, group, packet));
		Assert.Equal(1u, MuiGroupChangeCore.ChangeDepth(ref platform, State,
			group));
		Assert.Equal(childGroup.Raw, MuiHeadlessDispatcher.Dispatch(ref platform,
			State, childGroup, packet));
		Assert.Equal(1u, MuiGroupChangeCore.ChangeDepth(ref platform, State,
			childGroup));

		Assert.True(MuiGroupChangeCore.WriteInitChangeRecord(ref platform,
			packet));
		Assert.Equal(group.Raw, MuiHeadlessDispatcher.Dispatch(ref platform,
			State, group, packet));
		Assert.Equal(2u, MuiGroupChangeCore.ChangeDepth(ref platform, State,
			group));
		Assert.True(MuiGroupChangeCore.WriteExitChange2Record(ref platform,
			packet, 0xA5));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			group, packet));
		Assert.Equal(1u, MuiGroupChangeCore.ChangeDepth(ref platform, State,
			group));
		Assert.Equal(0xA5u, MuiGroupChangeCore.ChangeExitFlags(ref platform,
			State, group));
		Assert.True(MuiGroupChangeCore.WriteExitChangeRecord(ref platform,
			packet));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			group, packet));
		Assert.Equal(0u, MuiGroupChangeCore.ChangeDepth(ref platform, State,
			group));
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			group, packet));

		Assert.True(MuiGroupChangeCore.WriteInitChangeRecord(ref platform,
			packet));
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			ordinary, packet));
		Assert.Equal(childGroup.Raw, MuiHeadlessDispatcher.Dispatch(ref platform,
			State, childGroup, packet));
		var truncated = APTR.FromPointer(0x20FFC);
		Assert.False(MuiGroupChangeCore.WriteExitChange2Record(ref platform,
			truncated, 0xA5));
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			group, truncated));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			childGroup));
		Assert.Equal(0u, MuiGroupChangeCore.ChangeDepth(ref platform, State,
			childGroup));
	}

	[Fact]
	public void GroupChangeMethodHeaderUsesNamedField()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiGroupChangeCore.WriteInitChangeRecord(ref platform,
			packet));
		Assert.True(MuiGroupChangeMessageCodec.TryReadMethodId(ref platform,
			packet, out var header));
		Assert.Equal(MuiGroupChangeCore.InitChangeMethod, header.MethodId);
		Assert.True(MuiGroupChangeCore.WriteExitChange2Record(ref platform,
			packet, 0xA5));
		Assert.True(MuiGroupChangeMessageCodec.TryReadMethodId(ref platform,
			packet, out header));
		Assert.Equal(MuiGroupChangeCore.ExitChange2Method, header.MethodId);
		Assert.False(MuiGroupChangeMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void GroupChangeRecordFieldCursorUsesSemanticPacketAndStateKinds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var cursor = default(MuiGroupChangeRecordFieldCursor);
		cursor.Address = APTR.FromPointer(0x1200);
		cursor.Record = MuiGroupChangeRecordKind.ExitChange2;
		cursor.Field = MuiGroupChangeRecordField.Flags;
		Assert.True(MuiGroupChangeRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var fieldAddress));
		Assert.Equal(0x1204u, fieldAddress.Raw);
		cursor.Record = MuiGroupChangeRecordKind.State;
		cursor.Field = MuiGroupChangeRecordField.ExitRequests;
		Assert.True(MuiGroupChangeRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out fieldAddress));
		Assert.Equal(0x120Cu, fieldAddress.Raw);
		Assert.True(MuiGroupChangeRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			cursor.Address, MuiGroupChangeRecordKind.State,
			MuiGroupChangeRecordField.ExitFlags, 0xA5A5u));
		Assert.True(MuiGroupChangeRecordFieldCursorCodec.TryReadUInt32(ref platform,
			cursor.Address, MuiGroupChangeRecordKind.State,
			MuiGroupChangeRecordField.ExitFlags, out var flags));
		Assert.Equal(0xA5A5u, flags);
		cursor.Record = MuiGroupChangeRecordKind.Message;
		cursor.Field = MuiGroupChangeRecordField.Flags;
		Assert.False(MuiGroupChangeRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
		cursor.Address = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Field = MuiGroupChangeRecordField.MethodId;
		Assert.False(MuiGroupChangeRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	[Fact]
	public void GroupChangeTypedReadersUseNamedMethodHeader()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiGroupChangeCore.WriteInitChangeRecord(ref platform,
			packet));
		Assert.True(MuiGroupChangeCore.TryReadChange(ref platform, packet,
			MuiGroupChangeCore.InitChangeMethod, out var init));
		Assert.Equal(MuiGroupChangeCore.InitChangeMethod, init.MethodId);

		Assert.True(MuiGroupChangeCore.WriteExitChange2Record(ref platform,
			packet, 0xA5));
		Assert.True(MuiGroupChangeCore.TryReadExitChange2(ref platform, packet,
			out var exit2));
		Assert.Equal(0xA5u, exit2.Flags);
		platform.WriteUInt32(packet, 0, MuiGroupChangeCore.ExitChangeMethod);
		Assert.False(MuiGroupChangeCore.TryReadExitChange2(ref platform, packet,
			out _));
	}

	[Fact]
	public void GroupOrderingMethodHeaderUsesNamedField()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiGroupOperationsCore.WriteSortRecord(ref platform, packet,
			APTR.FromPointer(0x1300)));
		Assert.True(MuiGroupOrderingMessageCodec.TryReadMethodId(ref platform,
			packet, out var header));
		Assert.Equal(MuiGroupOperationsCore.SortMethod, header.MethodId);
		Assert.False(MuiGroupOrderingMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void GroupOrderingFieldsUseSemanticPacketBoundaries()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1500);
		var cursor = new MuiGroupOrderingPacketFieldCursor
		{
			Message = packet,
			Packet = MuiGroupOrderingPacketKind.MoveMember,
			Field = MuiGroupOrderingPacketField.Position,
		};
		Assert.True(MuiGroupOrderingPacketFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var address, out var fieldSize));
		Assert.Equal(APTR.FromPointer(0x1508), address);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiGroupOrderingPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiGroupOrderingPacketKind.MoveMember,
			MuiGroupOrderingPacketField.Position, unchecked((uint)-2)));
		Assert.True(MuiGroupOrderingPacketFieldCursorCodec.TryReadUInt32(
			ref platform, packet, MuiGroupOrderingPacketKind.MoveMember,
			MuiGroupOrderingPacketField.Position, out var position));
		Assert.Equal(-2, unchecked((int)position));
		Assert.True(MuiGroupOrderingPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiGroupOrderingPacketKind.Reorder,
			MuiGroupOrderingPacketField.Objects, 0x12345678u));
		Assert.True(MuiGroupOrderingPacketFieldCursorCodec.TryReadUInt32(
			ref platform, packet, MuiGroupOrderingPacketKind.Reorder,
			MuiGroupOrderingPacketField.Objects, out var objects));
		Assert.Equal(0x12345678u, objects);
		Assert.False(MuiGroupOrderingPacketFieldCursorCodec.TryReadUInt32(
			ref platform, packet, MuiGroupOrderingPacketKind.Sort,
			MuiGroupOrderingPacketField.After, out _));
		Assert.False(MuiGroupOrderingPacketFieldCursorCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0xFFFFFFF0u),
			MuiGroupOrderingPacketKind.MoveMember,
			MuiGroupOrderingPacketField.Object, out _));
	}

	[Fact]
	public void GroupOrderingPacketsMoveReorderAndSortChildren()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var groupName = APTR.FromPointer(0x1100);
		var ordinaryName = APTR.FromPointer(0x1140);
		platform.WriteCString(groupName, "Group.mui");
		platform.WriteCString(ordinaryName, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var groupClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, groupName, APTR.Null, 0, APTR.FromPointer(1));
		var ordinaryClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, ordinaryName, APTR.Null, 0, APTR.FromPointer(1));
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			ordinaryClass, APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			ordinaryClass, APTR.Null);
		var third = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			ordinaryClass, APTR.Null);
		var ordinary = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			ordinaryClass, APTR.Null);
		Assert.True(group.IsNotNull && first.IsNotNull && second.IsNotNull &&
			third.IsNotNull && ordinary.IsNotNull);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, third));
		var packet = APTR.FromPointer(0x1200);

		platform.WriteUInt32(packet, 0, MUIM_Group_MoveMember);
		platform.WriteUInt32(packet, 4, second.Raw);
		platform.WriteUInt32(packet, 8, unchecked((uint)-1));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			group, packet));
		Assert.Equal(first, MuiFamilyCore.GetChild(ref platform, State, group,
			0, APTR.Null));
		Assert.Equal(third, MuiFamilyCore.GetChild(ref platform, State, group,
			1, APTR.Null));
		Assert.Equal(second, MuiFamilyCore.GetChild(ref platform, State, group,
			2, APTR.Null));

		platform.WriteUInt32(packet, 8, 0);
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			group, packet));
		Assert.Equal(second, MuiFamilyCore.GetChild(ref platform, State, group,
			0, APTR.Null));
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			ordinary, packet));

		const uint order = 0x1400;
		platform.WriteUInt32(APTR.FromPointer(order), 0, third.Raw);
		platform.WriteUInt32(APTR.FromPointer(order), 4, first.Raw);
		platform.WriteUInt32(APTR.FromPointer(order), 8, second.Raw);
		platform.WriteUInt32(APTR.FromPointer(order), 12, 0);
		platform.WriteUInt32(packet, 0, MUIM_Group_Sort);
		platform.WriteUInt32(packet, 4, order);
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State,
			group, packet));
		Assert.Equal(third, MuiFamilyCore.GetChild(ref platform, State, group,
			0, APTR.Null));
		Assert.Equal(first, MuiFamilyCore.GetChild(ref platform, State, group,
			1, APTR.Null));
		Assert.Equal(second, MuiFamilyCore.GetChild(ref platform, State, group,
			2, APTR.Null));

		platform.WriteUInt32(packet, 0, MUIM_Group_Reorder);
		platform.WriteUInt32(packet, 4, first.Raw);
		platform.WriteUInt32(packet, 8, order);
		platform.WriteUInt32(APTR.FromPointer(order), 0, second.Raw);
		platform.WriteUInt32(APTR.FromPointer(order), 4, third.Raw);
		platform.WriteUInt32(APTR.FromPointer(order), 8, 0);
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State,
			group, packet));
		Assert.Equal(first, MuiFamilyCore.GetChild(ref platform, State, group,
			0, APTR.Null));
		Assert.Equal(second, MuiFamilyCore.GetChild(ref platform, State, group,
			1, APTR.Null));
		Assert.Equal(third, MuiFamilyCore.GetChild(ref platform, State, group,
			2, APTR.Null));

		platform.WriteUInt32(packet, 0, MUIM_Group_Reorder);
		platform.WriteUInt32(packet, 4, uint.MaxValue);
		platform.WriteUInt32(packet, 8, order);
		platform.WriteUInt32(APTR.FromPointer(order), 0, first.Raw);
		platform.WriteUInt32(APTR.FromPointer(order), 4, 0);
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			group, packet));
		Assert.Equal(second, MuiFamilyCore.GetChild(ref platform, State, group,
			0, APTR.Null));
		Assert.Equal(third, MuiFamilyCore.GetChild(ref platform, State, group,
			1, APTR.Null));
		Assert.Equal(first, MuiFamilyCore.GetChild(ref platform, State, group,
			2, APTR.Null));

		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(APTR.FromPointer(order), 0, second.Raw);
		platform.WriteUInt32(APTR.FromPointer(order), 4, second.Raw);
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			group, packet));
		Assert.Equal(second, MuiFamilyCore.GetChild(ref platform, State, group,
			0, APTR.Null));
		Assert.Equal(third, MuiFamilyCore.GetChild(ref platform, State, group,
			1, APTR.Null));
		Assert.Equal(first, MuiFamilyCore.GetChild(ref platform, State, group,
			2, APTR.Null));

		platform.WriteUInt32(packet, 0, MUIM_Group_Sort);
		platform.WriteUInt32(APTR.FromPointer(order), 0, second.Raw);
		platform.WriteUInt32(APTR.FromPointer(order), 4, 0);
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			group, packet));

		platform.WriteUInt32(packet, 0, MUIM_Group_Reorder);
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, 0x30FFE);
		Assert.Equal(0u, MuiLayoutDispatcher.Dispatch(ref platform, State,
			group, packet));
	}

	[Fact]
	public void ExportAndImportRequireObjectIdsAndLiveDataspaces()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var dataspace = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(obj.IsNotNull && dataspace.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MUIA_ObjectID, 0xCAFE, false));
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiObjectPersistenceMessageCore.WriteExportRecord(ref platform,
			packet, dataspace));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.Equal(1u, platform.ObjectExportRequestCount);
		Assert.Equal(obj, platform.LastPersistenceObject);
		Assert.Equal(dataspace, platform.LastPersistenceDataspace);
		Assert.Equal(0xCAFEu, platform.LastPersistenceObjectId);

		Assert.True(MuiObjectPersistenceMessageCore.WriteImportRecord(ref platform,
			packet, dataspace));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.Equal(1u, platform.ObjectImportRequestCount);
		platform.PersistenceOperationResult = false;
		Assert.True(MuiObjectPersistenceMessageCore.WriteExportRecord(ref platform,
			packet, dataspace));
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.Equal(1u, platform.ObjectExportRequestCount);
		platform.PersistenceOperationResult = true;

		// MUI explicitly suppresses persistence for ObjectID == 0.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MUIA_ObjectID, 0, false));
		Assert.True(MuiObjectPersistenceMessageCore.WriteExportRecord(ref platform,
			packet, dataspace));
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.Equal(1u, platform.ObjectExportRequestCount);

		// A malformed dataspace or target object is rejected before the capability.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MUIA_ObjectID, 1, false));
		Assert.True(MuiObjectPersistenceMessageCore.WriteExportRecord(ref platform,
			packet, APTR.FromPointer(0x21000)));
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiObjectPersistenceMessageCore.WriteExportRecord(ref platform,
			packet, dataspace));
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			APTR.FromPointer(0x21000), packet));
	}

	[Fact]
	public void ExportImportPacketCodecRejectsTruncatedGuestRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x20FFC);
		Assert.False(MuiObjectPersistenceMessageCore.WriteExportRecord(ref platform,
			packet, APTR.FromPointer(0x1200)));
		platform.WriteUInt32(packet, 0, MuiObjectPersistenceMessageCore.ExportMethod);
		Assert.Equal(0u, MuiObjectPersistenceMessageCore.DispatchRecord(ref platform,
			packet));
	}

	[Fact]
	public void ObjectPersistenceMethodHeaderUsesNamedField()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		Assert.True(MuiObjectPersistenceMessageCore.WriteExportRecord(ref platform,
			packet, APTR.FromPointer(0x1300)));
		Assert.True(MuiObjectPersistenceMessageCodec.TryReadMethodId(ref platform,
			packet, out var header));
		Assert.Equal(MuiObjectPersistenceMessageCore.ExportMethod, header.MethodId);
		Assert.False(MuiObjectPersistenceMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void ObjectPersistencePacketFieldCursorUsesNamedPointerBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var packet = APTR.FromPointer(0x1200);
		var cursor = default(MuiObjectPersistencePacketFieldCursor);
		cursor.Message = packet;
		cursor.Field = MuiObjectPersistencePacketField.MethodId;
		Assert.True(MuiObjectPersistencePacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var address));
		Assert.Equal(packet.Raw, address.Raw);
		cursor.Field = MuiObjectPersistencePacketField.Dataspace;
		Assert.True(MuiObjectPersistencePacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(packet.Raw + 4, address.Raw);
		Assert.True(MuiObjectPersistencePacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiObjectPersistencePacketField.MethodId,
			MuiObjectPersistenceMessageCore.ExportMethod));
		Assert.True(MuiObjectPersistencePacketFieldCursorCodec.TryWriteUInt32(
			ref platform, packet, MuiObjectPersistencePacketField.Dataspace,
			0x1500));
		Assert.True(MuiObjectPersistencePacketFieldCursorCodec.TryReadUInt32(
			ref platform, packet, MuiObjectPersistencePacketField.Dataspace,
			out var dataspace));
		Assert.Equal(0x1500u, dataspace);
		cursor.Message = APTR.FromPointer(0xfffffff0u);
		Assert.False(MuiObjectPersistencePacketFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void NumericExportAndImportRoundTripUsesGuestDataspace()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var numericName = APTR.FromPointer(0x1100);
		var dataspaceName = APTR.FromPointer(0x1180);
		platform.WriteCString(numericName, "Numeric.mui");
		platform.WriteCString(dataspaceName, "Dataspace.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var numericClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			numericName, APTR.Null, 0, APTR.FromPointer(1), false);
		var dataspaceClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			dataspaceName, APTR.Null, 0, APTR.FromPointer(1), false);
		var numeric = MuiCommonControlCore.CreateControl(ref platform, State,
			numericClass, APTR.Null);
		var dataspace = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			dataspaceClass, APTR.Null);
		Assert.True(numeric.IsNotNull && dataspace.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, numeric,
			0x8042D76E, 0x9001, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, numeric,
			0x8042AE3A, 73, false));

		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0x80420F1C);
		platform.WriteUInt32(packet, 4, dataspace.Raw);
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State, numeric,
			packet));
		Assert.Equal(0u, platform.ObjectExportRequestCount);
		Assert.Equal(4, MuiStoreCore.DataspaceLength(ref platform, State,
			dataspace, 0x9001));
		var stored = MuiStoreCore.DataspaceFind(ref platform, State, dataspace,
			0x9001);
		Assert.NotEqual(APTR.Null, stored);
		Assert.Equal(73u, platform.ReadUInt32(stored, 0));

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, numeric,
			0x8042AE3A, 12, false));
		platform.WriteUInt32(packet, 0, 0x8042D012);
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State, numeric,
			packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, numeric,
			0x8042AE3A, out var restored));
		Assert.Equal(73u, restored);
	}

	[Fact]
	public void StringTextAreaAndGroupPersistenceRoundTripUsesGuestDataspace()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var stringName = APTR.FromPointer(0x1100);
		var textName = APTR.FromPointer(0x1140);
		var imageName = APTR.FromPointer(0x1180);
		var groupName = APTR.FromPointer(0x11C0);
		var dataspaceName = APTR.FromPointer(0x1200);
		platform.WriteCString(stringName, "String.mui");
		platform.WriteCString(textName, "Text.mui");
		platform.WriteCString(imageName, "Image.mui");
		platform.WriteCString(groupName, "Group.mui");
		platform.WriteCString(dataspaceName, "Dataspace.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var stringClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, stringName, APTR.Null, 0, APTR.FromPointer(1));
		var textClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, textName, APTR.Null, 0, APTR.FromPointer(1));
		var imageClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, imageName, APTR.Null, 0, APTR.FromPointer(1));
		var groupClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, groupName, APTR.Null, 0, APTR.FromPointer(1));
		var dataspaceClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, dataspaceName, APTR.Null, 0, APTR.FromPointer(1));
		var stringObject = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, APTR.Null);
		var textObject = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, APTR.Null);
		var imageObject = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, APTR.Null);
		var groupObject = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var dataspace = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			dataspaceClass, APTR.Null);
		Assert.True(stringObject.IsNotNull && textObject.IsNotNull &&
			imageObject.IsNotNull && groupObject.IsNotNull && dataspace.IsNotNull);
		Assert.Equal(MuiControlClass.String, MuiCommonControlCore.Classify(ref platform,
			State, stringObject));

		var stringSource = APTR.FromPointer(0x1400);
		var textSource = APTR.FromPointer(0x1440);
		var stringReplacement = APTR.FromPointer(0x1480);
		var textReplacement = APTR.FromPointer(0x14C0);
		platform.WriteCString(stringSource, "hello");
		platform.WriteCString(textSource, "status");
		platform.WriteCString(stringReplacement, "changed");
		platform.WriteCString(textReplacement, "updated");
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			stringObject, MUIA_ObjectID, 0x9101, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			textObject, MUIA_ObjectID, 0x9102, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			imageObject, MUIA_ObjectID, 0x9103, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			groupObject, MUIA_ObjectID, 0x9104, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			stringObject, MUIA_String_Contents, stringSource.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			textObject, MUIA_Text_Contents, textSource.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			imageObject, MUIA_Selected, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			groupObject, MUIA_Group_ActivePage, 3, false));

		var packet = APTR.FromPointer(0x1500);
		platform.WriteUInt32(packet, 0, MUIM_Export);
		platform.WriteUInt32(packet, 4, dataspace.Raw);
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			stringObject, packet));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			textObject, packet));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			imageObject, packet));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			groupObject, packet));
		Assert.Equal(6, MuiStoreCore.DataspaceLength(ref platform, State,
			dataspace, 0x9101));
		Assert.Equal(7, MuiStoreCore.DataspaceLength(ref platform, State,
			dataspace, 0x9102));
		Assert.Equal(4, MuiStoreCore.DataspaceLength(ref platform, State,
			dataspace, 0x9103));
		Assert.Equal(4, MuiStoreCore.DataspaceLength(ref platform, State,
			dataspace, 0x9104));
		Assert.Equal((byte)'h', platform.ReadUInt8(MuiStoreCore.DataspaceFind(
			ref platform, State, dataspace, 0x9101), 0));
		Assert.Equal((byte)'s', platform.ReadUInt8(MuiStoreCore.DataspaceFind(
			ref platform, State, dataspace, 0x9102), 0));
		Assert.Equal(1u, platform.ReadUInt32(MuiStoreCore.DataspaceFind(
			ref platform, State, dataspace, 0x9103), 0));
		Assert.Equal(3u, platform.ReadUInt32(MuiStoreCore.DataspaceFind(
			ref platform, State, dataspace, 0x9104), 0));
		var malformed = APTR.FromPointer(0x1600);
		platform.WriteUInt8(malformed, 0, (byte)'x');
		platform.WriteUInt8(malformed, 1, (byte)'y');
		Assert.True(MuiStoreCore.DataspaceAdd(ref platform, State, dataspace,
			0x9101, malformed, 2));
		Assert.Equal(2, MuiStoreCore.DataspaceLength(ref platform, State,
			dataspace, 0x9101));
		Assert.Equal((byte)'y', platform.ReadUInt8(MuiStoreCore.DataspaceFind(
			ref platform, State, dataspace, 0x9101), 1));
		platform.WriteUInt32(packet, 0, MUIM_Import);
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			stringObject, packet));
		platform.WriteUInt32(packet, 0, MUIM_Export);
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			stringObject, packet));

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			stringObject, MUIA_String_Contents, stringReplacement.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			textObject, MUIA_Text_Contents, textReplacement.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			imageObject, MUIA_Selected, 0, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			groupObject, MUIA_Group_ActivePage, 0, false));
		platform.WriteUInt32(packet, 0, MUIM_Import);
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			stringObject, packet));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			textObject, packet));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			imageObject, packet));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			groupObject, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			stringObject, MUIA_String_Contents, out var restoredString));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			textObject, MUIA_Text_Contents, out var restoredText));
		Assert.Equal((byte)'h', platform.ReadUInt8(APTR.FromPointer(restoredString), 0));
		Assert.Equal((byte)'s', platform.ReadUInt8(APTR.FromPointer(restoredText), 0));
		Assert.Equal((byte)'t', platform.ReadUInt8(APTR.FromPointer(restoredText), 1));
		Assert.Equal(1u, Get(ref platform, imageObject, MUIA_Selected));
		Assert.Equal(1u, Get(ref platform, imageObject, MUIA_Image_State));
		Assert.Equal(3u, Get(ref platform, groupObject, MUIA_Group_ActivePage));
	}

	[Fact]
	public void ApplicationPersistenceWalksGuestTreeInPreorderAndSkipsZeroIds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var applicationName = APTR.FromPointer(0x1100);
		var numericName = APTR.FromPointer(0x1140);
		var stringName = APTR.FromPointer(0x1180);
		var groupName = APTR.FromPointer(0x11C0);
		var dataspaceName = APTR.FromPointer(0x1200);
		platform.WriteCString(applicationName, "Notify.mui");
		platform.WriteCString(numericName, "Numeric.mui");
		platform.WriteCString(stringName, "String.mui");
		platform.WriteCString(groupName, "Group.mui");
		platform.WriteCString(dataspaceName, "Dataspace.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var applicationClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, applicationName, APTR.Null, 0, APTR.FromPointer(1), false);
		var numericClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, numericName, APTR.Null, 0, APTR.FromPointer(1));
		var stringClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, stringName, APTR.Null, 0, APTR.FromPointer(1));
		var groupClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, groupName, APTR.Null, 0, APTR.FromPointer(1));
		var dataspaceClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, dataspaceName, APTR.Null, 0, APTR.FromPointer(1));
		var application = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			applicationClass, APTR.Null);
		var customFirst = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			applicationClass, APTR.Null);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var customSecond = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			applicationClass, APTR.Null);
		var stringObject = MuiCommonControlCore.CreateControl(ref platform, State,
			stringClass, APTR.Null);
		var numericObject = MuiCommonControlCore.CreateControl(ref platform, State,
			numericClass, APTR.Null);
		var dataspace = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			dataspaceClass, APTR.Null);
		Assert.True(application.IsNotNull && customFirst.IsNotNull &&
			group.IsNotNull && customSecond.IsNotNull && stringObject.IsNotNull &&
			numericObject.IsNotNull && dataspace.IsNotNull);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, application,
			customFirst));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, application, group));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group,
			customSecond));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group,
			stringObject));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, application,
			numericObject));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			customFirst, MUIA_ObjectID, 0xA001, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			customSecond, MUIA_ObjectID, 0xA002, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			stringObject, MUIA_ObjectID, 0xA003, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			numericObject, MUIA_ObjectID, 0xA004, false));
		var text = APTR.FromPointer(0x1400);
		platform.WriteCString(text, "nested");
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			stringObject, MUIA_String_Contents, text.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			numericObject, 0x8042AE3A, 91, false));

		Assert.True(MuiApplicationPersistenceCore.Export(ref platform, State,
			application, dataspace));
		Assert.Equal(2u, platform.ObjectExportRequestCount);
		Assert.Equal(customFirst, platform.PreviousPersistenceObject);
		Assert.Equal(customSecond, platform.LastPersistenceObject);
		Assert.Equal(7, MuiStoreCore.DataspaceLength(ref platform, State,
			dataspace, 0xA003));
		Assert.Equal(4, MuiStoreCore.DataspaceLength(ref platform, State,
			dataspace, 0xA004));
		Assert.Equal(91u, platform.ReadUInt32(MuiStoreCore.DataspaceFind(
			ref platform, State, dataspace, 0xA004), 0));

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			stringObject, MUIA_String_Contents, text.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			numericObject, 0x8042AE3A, 7, false));
		Assert.True(MuiApplicationPersistenceCore.Import(ref platform, State,
			application, dataspace));
		Assert.Equal(91u, Get(ref platform, numericObject, 0x8042AE3A));
		Assert.Equal((byte)'n', platform.ReadUInt8(APTR.FromPointer(Get(
			ref platform, stringObject, MUIA_String_Contents)), 0));

		// A malformed built-in payload rejects the walk before later objects are
		// touched; the zero-ID group itself remains deliberately non-persistent.
		var malformed = APTR.FromPointer(0x1440);
		platform.WriteUInt8(malformed, 0, (byte)'x');
		Assert.True(MuiStoreCore.DataspaceAdd(ref platform, State, dataspace,
			0xA003, malformed, 1));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			numericObject, 0x8042AE3A, 7, false));
		Assert.False(MuiApplicationPersistenceCore.Import(ref platform, State,
			application, dataspace));
		Assert.Equal(7u, Get(ref platform, numericObject, 0x8042AE3A));
	}

	[Fact]
	public void ApplicationPersistenceTransactionalImportRestoresSnapshotAfterFailure()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var applicationName = APTR.FromPointer(0x1100);
		var numericName = APTR.FromPointer(0x1140);
		var dataspaceName = APTR.FromPointer(0x1180);
		platform.WriteCString(applicationName, "Notify.mui");
		platform.WriteCString(numericName, "Numeric.mui");
		platform.WriteCString(dataspaceName, "Dataspace.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var applicationClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, applicationName, APTR.Null, 0, APTR.FromPointer(1), false);
		var numericClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, numericName, APTR.Null, 0, APTR.FromPointer(1));
		var dataspaceClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, dataspaceName, APTR.Null, 0, APTR.FromPointer(1));
		var application = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			applicationClass, APTR.Null);
		var numericObject = MuiCommonControlCore.CreateControl(ref platform, State,
			numericClass, APTR.Null);
		var customObject = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			applicationClass, APTR.Null);
		var incoming = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			dataspaceClass, APTR.Null);
		var snapshot = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			dataspaceClass, APTR.Null);
		Assert.True(application.IsNotNull && numericObject.IsNotNull &&
			customObject.IsNotNull && incoming.IsNotNull && snapshot.IsNotNull);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, application,
			numericObject));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, application,
			customObject));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			numericObject, MUIA_ObjectID, 0xB001, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			customObject, MUIA_ObjectID, 0xB002, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			numericObject, 0x8042AE3A, 99, false));
		Assert.True(MuiApplicationPersistenceCore.Export(ref platform, State,
			application, incoming));
		Assert.Equal(99u, platform.ReadUInt32(MuiStoreCore.DataspaceFind(
			ref platform, State, incoming, 0xB001), 0));

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			numericObject, 0x8042AE3A, 7, false));
		var stale = APTR.FromPointer(0x1500);
		platform.WriteUInt32(stale, 0, 1234);
		Assert.True(MuiStoreCore.DataspaceAdd(ref platform, State, snapshot,
			0xDEAD, stale, 4));
		platform.PersistenceImportFailureDataspace = incoming;
		platform.PersistenceImportFailureObject = customObject;
		Assert.False(MuiApplicationPersistenceCore.ImportTransactional(ref platform,
			State, application, incoming, snapshot));
		Assert.Equal(7u, Get(ref platform, numericObject, 0x8042AE3A));
		Assert.True(platform.ObjectImportRequestCount >= 1);
		Assert.Equal(0, MuiStoreCore.DataspaceLength(ref platform, State,
			snapshot, 0xDEAD));
		Assert.False(MuiApplicationPersistenceCore.ImportTransactional(ref platform,
			State, application, incoming, incoming));
	}

	[Fact]
	public void ApplicationPersistenceFrameCodecUsesNamedGuestFields()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var address = APTR.FromPointer(0x1800);
		var expected = default(MuiApplicationPersistenceFrameState);
		expected.Object = APTR.FromPointer(0xA100);
		expected.NextChild = 3;
		expected.VisitMarker = 17;

		Assert.True(MuiApplicationPersistenceFrameCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiApplicationPersistenceFrameCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.Object, actual.Object);
		Assert.Equal(expected.NextChild, actual.NextChild);
		Assert.Equal(expected.VisitMarker, actual.VisitMarker);
		Assert.False(MuiApplicationPersistenceFrameCodec.TryRead(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void ApplicationPersistenceFrameFieldCursorUsesNamedBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var frame = APTR.FromPointer(0x1800);
		var cursor = default(MuiApplicationPersistenceFrameFieldCursor);
		cursor.Frame = frame;
		cursor.Field = MuiApplicationPersistenceFrameField.Object;
		Assert.True(MuiApplicationPersistenceFrameFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var address));
		Assert.Equal(frame.Raw, address.Raw);
		cursor.Field = MuiApplicationPersistenceFrameField.NextChild;
		Assert.True(MuiApplicationPersistenceFrameFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(frame.Raw + 4, address.Raw);
		cursor.Field = MuiApplicationPersistenceFrameField.VisitMarker;
		Assert.True(MuiApplicationPersistenceFrameFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out address));
		Assert.Equal(frame.Raw + 8, address.Raw);
		Assert.True(MuiApplicationPersistenceFrameFieldCursorCodec.TryWrite(
			ref platform, frame, MuiApplicationPersistenceFrameField.Object,
			0xA100));
		Assert.True(MuiApplicationPersistenceFrameFieldCursorCodec.TryWrite(
			ref platform, frame, MuiApplicationPersistenceFrameField.NextChild, 3));
		Assert.True(MuiApplicationPersistenceFrameFieldCursorCodec.TryWrite(
			ref platform, frame, MuiApplicationPersistenceFrameField.VisitMarker,
			17));
		Assert.True(MuiApplicationPersistenceFrameFieldCursorCodec.TryRead(ref
			platform, frame, MuiApplicationPersistenceFrameField.Object,
			out var objectValue));
		Assert.Equal(0xA100u, objectValue);
		Assert.True(MuiApplicationPersistenceFrameFieldCursorCodec.TryRead(ref
			platform, frame, MuiApplicationPersistenceFrameField.VisitMarker,
			out var marker));
		Assert.Equal(17u, marker);
		cursor.Frame = APTR.FromPointer(0xfffffffcu);
		Assert.False(MuiApplicationPersistenceFrameFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void ApplicationPersistenceFrameAddressUsesNamedDepthBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var stack = APTR.FromPointer(0x1800);

		Assert.True(MuiApplicationPersistenceFrameCodec.TryGetFrame(ref platform,
			stack, 2, out var frame));
		Assert.Equal(APTR.FromPointer(0x180C), frame);
		Assert.False(MuiApplicationPersistenceFrameCodec.TryGetFrame(ref platform,
			stack, 0, out _));
		Assert.False(MuiApplicationPersistenceFrameCodec.TryGetFrame(ref platform,
			APTR.FromPointer(0x20FFC), 1, out _));
	}

	[Fact]
	public void ApplicationPersistenceFrameCursorUsesNamedEntryBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var cursor = default(MuiApplicationPersistenceFrameCursor);
		cursor.Base = APTR.FromPointer(0x1800);
		cursor.Index = MuiApplicationPersistenceFrameCursor.MaximumEntries - 1;

		Assert.True(MuiApplicationPersistenceFrameCursorCodec.TryGetEntry(
			ref platform, cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x23F4), address);
		cursor.Index = MuiApplicationPersistenceFrameCursor.MaximumEntries;
		Assert.False(MuiApplicationPersistenceFrameCursorCodec.TryGetEntry(
			ref platform, cursor, out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFF0);
		cursor.Index = 1;
		Assert.False(MuiApplicationPersistenceFrameCursorCodec.TryGetEntry(
			ref platform, cursor, out _));
	}

	private static void WritePacket(ref MuiHeadlessTestPlatform platform,
		APTR packet, uint method, uint first, uint second, uint third)
	{
		platform.WriteUInt32(packet, 0, method);
		platform.WriteUInt32(packet, 4, first);
		platform.WriteUInt32(packet, 8, second);
		platform.WriteUInt32(packet, 12, third);
	}

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			attribute, out var value));
		return value;
	}
}
