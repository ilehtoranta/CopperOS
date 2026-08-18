/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.NativeRoot;

public static class MuiNativeRoots
{
	public static uint HeadlessCoreRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		APTR.WriteUInt8(APTR.FromPointer(name), 0, (byte)'N');
		APTR.WriteUInt8(APTR.FromPointer(name), 1, (byte)'o');
		APTR.WriteUInt8(APTR.FromPointer(name), 2, (byte)'t');
		APTR.WriteUInt8(APTR.FromPointer(name), 3, (byte)'i');
		APTR.WriteUInt8(APTR.FromPointer(name), 4, (byte)'f');
		APTR.WriteUInt8(APTR.FromPointer(name), 5, (byte)'y');
		APTR.WriteUInt8(APTR.FromPointer(name), 6, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 8,
			APTR.FromPointer(1)).Raw;
		if (cl == 0) return 2;
		var family = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(cl), APTR.Null).Raw;
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(cl), APTR.Null).Raw;
		if (family == 0 || child == 0) return 3;
		if (!MuiFamilyCore.AddTail(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(family), APTR.FromPointer(child))) return 4;
		const uint data = 0x00036200;
		APTR.WriteUInt32(APTR.FromPointer(data), 0, 0x11223344);
		if (!MuiStoreCore.DataspaceAdd(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(family), 7, APTR.FromPointer(data), 4)) return 5;
		const uint follow = 0x00036220;
		APTR.WriteUInt32(APTR.FromPointer(follow), 0, 0x90000001);
		APTR.WriteUInt32(APTR.FromPointer(follow), 4, 1233727793);
		if (!MuiNotifyCore.Add(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(family), 0x80420001, 1233727793,
			APTR.FromPointer(child), 2, APTR.FromPointer(follow))) return 6;
		const uint packet = 0x00036240;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042549A);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0x80420001);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 42);
		if (MuiHeadlessDispatcher.Dispatch(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(family), APTR.FromPointer(packet)) == 0) return 7;
		if (!MuiSemaphoreCore.Attempt(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(family)) || !MuiSemaphoreCore.Release(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(family))) return 8;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 9;
		return 42;
	}

	// Focused MorphOS MUIM_Family_GetChild closure. The fixed packet is
	// decoded into a named struct and all selector forms use the guest Family
	// topology without managed iteration.
	public static uint FamilyGetChildRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint firstNode = 0x00036820;
		const uint secondNode = 0x00036830;
		const uint thirdNode = 0x00036840;
		const uint firstObject = 0x00036900;
		const uint secondObject = 0x00036910;
		const uint thirdObject = 0x00036920;
		const uint packet = 0x00036940;
		if (!MuiFamilyGetChildCore.WriteChildRecord(ref platform,
			APTR.FromPointer(firstNode), APTR.FromPointer(secondNode), APTR.Null,
			APTR.FromPointer(firstObject))) return 1;
		if (!MuiFamilyGetChildCore.WriteChildRecord(ref platform,
			APTR.FromPointer(secondNode), APTR.FromPointer(thirdNode),
			APTR.FromPointer(firstNode), APTR.FromPointer(secondObject))) return 2;
		if (!MuiFamilyGetChildCore.WriteChildRecord(ref platform,
			APTR.FromPointer(thirdNode), APTR.Null, APTR.FromPointer(secondNode),
			APTR.FromPointer(thirdObject))) return 3;
		if (!MuiFamilyGetChildCore.WriteRecord(ref platform,
			APTR.FromPointer(packet), MuiFamilyGetChildCore.First, APTR.Null)) return 41;
		var firstResult = MuiFamilyGetChildCore.DispatchRecord(ref platform,
			APTR.FromPointer(firstNode), APTR.FromPointer(thirdNode),
			APTR.FromPointer(packet));
		if (firstResult != firstObject) return 4;
		if (!MuiFamilyGetChildCore.WriteRecord(ref platform,
			APTR.FromPointer(packet), MuiFamilyGetChildCore.Last, APTR.Null) ||
			MuiFamilyGetChildCore.DispatchRecord(ref platform,
			APTR.FromPointer(firstNode), APTR.FromPointer(thirdNode),
			APTR.FromPointer(packet)) != thirdObject) return 5;
		if (!MuiFamilyGetChildCore.WriteRecord(ref platform,
			APTR.FromPointer(packet), MuiFamilyGetChildCore.Next,
			APTR.FromPointer(firstObject)) ||
			MuiFamilyGetChildCore.DispatchRecord(ref platform,
			APTR.FromPointer(firstNode), APTR.FromPointer(thirdNode),
			APTR.FromPointer(packet)) != secondObject) return 6;
		if (!MuiFamilyGetChildCore.WriteRecord(ref platform,
			APTR.FromPointer(packet), MuiFamilyGetChildCore.Iterate,
			APTR.FromPointer(secondObject)) ||
			MuiFamilyGetChildCore.DispatchRecord(ref platform,
			APTR.FromPointer(firstNode), APTR.FromPointer(thirdNode),
			APTR.FromPointer(packet)) != thirdObject) return 7;
		if (!MuiFamilyGetChildCore.WriteRecord(ref platform,
			APTR.FromPointer(packet), MuiFamilyGetChildCore.Previous,
			APTR.FromPointer(thirdObject)) ||
			MuiFamilyGetChildCore.DispatchRecord(ref platform,
			APTR.FromPointer(firstNode), APTR.FromPointer(thirdNode),
			APTR.FromPointer(packet)) != secondObject) return 8;
		if (!MuiFamilyGetChildCore.WriteRecord(ref platform,
			APTR.FromPointer(packet), MuiFamilyGetChildCore.Previous, APTR.Null) ||
			MuiFamilyGetChildCore.DispatchRecord(ref platform,
			APTR.FromPointer(firstNode), APTR.FromPointer(thirdNode),
			APTR.FromPointer(packet)) != thirdObject) return 9;
		return 42;
	}

	// Focused MorphOS Family mutation closure. AddHead/AddTail/Remove share the
	// named {MethodID, object} packet; Insert and Transfer use their complete
	// named records. The guest projection keeps qualification independent of
	// managed state.
	public static uint FamilyChildPacketsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint list = 0x00036E00;
		const uint firstNode = 0x00036E20;
		const uint secondNode = 0x00036E30;
		const uint firstObject = 0x00036F00;
		const uint secondObject = 0x00036F10;
		const uint packet = 0x00037040;
		if (!MuiFamilyMutationCore.WriteListRecord(ref platform,
			APTR.FromPointer(list), APTR.Null, APTR.Null)) return 1;
		if (!MuiFamilyMutationCore.WriteRecord(ref platform,
			APTR.FromPointer(packet), MuiFamilyMutationCore.AddTailMethod,
			APTR.FromPointer(firstObject)) ||
			MuiFamilyMutationCore.DispatchProjection(ref platform,
				APTR.FromPointer(list), APTR.FromPointer(firstNode),
				APTR.FromPointer(packet)) != 1) return 2;
		if (!MuiFamilyMutationCore.WriteRecord(ref platform,
			APTR.FromPointer(packet), MuiFamilyMutationCore.AddHeadMethod,
			APTR.FromPointer(secondObject)) ||
			MuiFamilyMutationCore.DispatchProjection(ref platform,
				APTR.FromPointer(list), APTR.FromPointer(secondNode),
				APTR.FromPointer(packet)) != 1) return 3;
		if (APTR.ReadUInt32(APTR.FromPointer(list), 0) != secondNode ||
			APTR.ReadUInt32(APTR.FromPointer(list), 4) != firstNode ||
			APTR.ReadUInt32(APTR.FromPointer(secondNode), 0) != firstNode ||
			APTR.ReadUInt32(APTR.FromPointer(firstNode), 4) != secondNode ||
			APTR.ReadUInt32(APTR.FromPointer(secondNode), 8) != secondObject ||
			APTR.ReadUInt32(APTR.FromPointer(firstNode), 8) != firstObject) return 4;
		if (!MuiFamilyMutationCore.WriteRecord(ref platform,
			APTR.FromPointer(packet), MuiFamilyMutationCore.RemoveMethod,
			APTR.FromPointer(secondObject)) ||
			MuiFamilyMutationCore.DispatchProjection(ref platform,
				APTR.FromPointer(list), APTR.FromPointer(secondNode),
				APTR.FromPointer(packet)) != 1) return 5;
		if (APTR.ReadUInt32(APTR.FromPointer(list), 0) != firstNode ||
			APTR.ReadUInt32(APTR.FromPointer(list), 4) != firstNode ||
			APTR.ReadUInt32(APTR.FromPointer(firstNode), 0) != 0 ||
			APTR.ReadUInt32(APTR.FromPointer(firstNode), 4) != 0 ||
			APTR.ReadUInt32(APTR.FromPointer(secondNode), 0) != 0 ||
			APTR.ReadUInt32(APTR.FromPointer(secondNode), 4) != 0) return 6;
		if (!MuiFamilyMutationCore.WriteInsertRecord(ref platform,
			APTR.FromPointer(packet), APTR.FromPointer(secondObject),
			APTR.FromPointer(firstObject)) ||
			MuiFamilyMutationCore.DispatchInsertProjection(ref platform,
				APTR.FromPointer(list), APTR.FromPointer(secondNode),
				APTR.FromPointer(packet)) != 1) return 7;
		if (APTR.ReadUInt32(APTR.FromPointer(list), 0) != firstNode ||
			APTR.ReadUInt32(APTR.FromPointer(list), 4) != secondNode ||
			APTR.ReadUInt32(APTR.FromPointer(firstNode), 0) != secondNode ||
			APTR.ReadUInt32(APTR.FromPointer(secondNode), 4) != firstNode) return 8;
		const uint sourceList = 0x00036F40;
		const uint sourceNode = 0x00036F60;
		const uint sourceObject = 0x00036F80;
		if (!MuiFamilyMutationCore.WriteListRecord(ref platform,
			APTR.FromPointer(sourceList), APTR.FromPointer(sourceNode),
			APTR.FromPointer(sourceNode)) ||
			!MuiFamilyMutationCore.WriteChildRecord(ref platform,
				APTR.FromPointer(sourceNode), APTR.Null, APTR.Null,
				APTR.FromPointer(sourceObject)) ||
			!MuiFamilyMutationCore.WriteTransferRecord(ref platform,
				APTR.FromPointer(packet), APTR.FromPointer(sourceList)) ||
			MuiFamilyMutationCore.DispatchTransferProjection(ref platform,
				APTR.FromPointer(list), APTR.FromPointer(sourceList),
				APTR.FromPointer(packet)) != 1) return 9;
		if (APTR.ReadUInt32(APTR.FromPointer(list), 0) != firstNode ||
			APTR.ReadUInt32(APTR.FromPointer(list), 4) != sourceNode ||
			APTR.ReadUInt32(APTR.FromPointer(secondNode), 0) != sourceNode ||
			APTR.ReadUInt32(APTR.FromPointer(sourceNode), 4) != secondNode ||
			APTR.ReadUInt32(APTR.FromPointer(sourceList), 0) != 0 ||
			APTR.ReadUInt32(APTR.FromPointer(sourceList), 4) != 0) return 10;
		if (!MuiFamilyMutationCore.WriteReorderRecord(ref platform,
			APTR.FromPointer(packet), APTR.Null) ||
			!MuiFamilyMutationCore.WriteVectorEntry(ref platform,
				APTR.FromPointer(packet), MuiFamilyMutationCore.ReorderArrayOffset,
				0, APTR.FromPointer(sourceObject)) ||
			!MuiFamilyMutationCore.WriteVectorEntry(ref platform,
				APTR.FromPointer(packet), MuiFamilyMutationCore.ReorderArrayOffset,
				1, APTR.FromPointer(firstObject)) ||
			!MuiFamilyMutationCore.WriteVectorEntry(ref platform,
				APTR.FromPointer(packet), MuiFamilyMutationCore.ReorderArrayOffset,
				2, APTR.FromPointer(secondObject)) ||
			!MuiFamilyMutationCore.WriteVectorEntry(ref platform,
				APTR.FromPointer(packet), MuiFamilyMutationCore.ReorderArrayOffset,
				3, APTR.Null) ||
			MuiFamilyMutationCore.DispatchReorderProjection(ref platform,
				APTR.FromPointer(list), APTR.FromPointer(packet)) != 1) return 11;
		if (APTR.ReadUInt32(APTR.FromPointer(list), 0) != sourceNode ||
			APTR.ReadUInt32(APTR.FromPointer(sourceNode), 0) != firstNode ||
			APTR.ReadUInt32(APTR.FromPointer(firstNode), 4) != sourceNode) return 12;
		if (!MuiFamilyMutationCore.WriteSortRecord(ref platform,
			APTR.FromPointer(packet)) ||
			!MuiFamilyMutationCore.WriteVectorEntry(ref platform,
				APTR.FromPointer(packet), MuiFamilyMutationCore.SortArrayOffset,
				0, APTR.FromPointer(secondObject)) ||
			!MuiFamilyMutationCore.WriteVectorEntry(ref platform,
				APTR.FromPointer(packet), MuiFamilyMutationCore.SortArrayOffset,
				1, APTR.FromPointer(sourceObject)) ||
			!MuiFamilyMutationCore.WriteVectorEntry(ref platform,
				APTR.FromPointer(packet), MuiFamilyMutationCore.SortArrayOffset,
				2, APTR.FromPointer(firstObject)) ||
			!MuiFamilyMutationCore.WriteVectorEntry(ref platform,
				APTR.FromPointer(packet), MuiFamilyMutationCore.SortArrayOffset,
				3, APTR.Null) ||
			MuiFamilyMutationCore.DispatchSortProjection(ref platform,
				APTR.FromPointer(list), APTR.FromPointer(packet)) != 1) return 13;
		if (APTR.ReadUInt32(APTR.FromPointer(list), 0) != secondNode ||
			APTR.ReadUInt32(APTR.FromPointer(secondNode), 0) != sourceNode ||
			APTR.ReadUInt32(APTR.FromPointer(sourceNode), 4) != secondNode) return 14;
		return 42;
	}

	// Packet-only MorphOS Datamap/Objectmap qualification. The host suite
	// exercises live store behavior; this closure keeps the native proof at the
	// fixed ABI boundary so no managed allocator or key/value container enters
	// the freestanding 68k artifact.
	public static uint StorePacketsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint packet = 0x00037100;
		const uint data = 0x00037140;
		const uint key = 0x00037180;
		const uint size = 0x000371A0;
		const uint counter = 0x000371C0;
		const uint objectPtr = 0x00037200;
		const uint objectKey = 0x00037220;
		var packetPtr = APTR.FromPointer(packet);
		if (!MuiStoreMessageCore.WriteDatamapSetRecord(ref platform, packetPtr,
			APTR.FromPointer(data), 4, APTR.FromPointer(key)) ||
			MuiStoreMessageCore.DispatchRecord(ref platform, packetPtr) != 4) return 1;
		if (!MuiStoreMessageCore.WriteDatamapGetRecord(ref platform, packetPtr,
			APTR.FromPointer(key), APTR.FromPointer(size)) ||
			MuiStoreMessageCore.DispatchRecord(ref platform, packetPtr) != size) return 2;
		if (!MuiStoreMessageCore.WriteDatamapKeyRecord(ref platform, packetPtr,
			MuiStoreMessageCore.DatamapFindMethod, APTR.FromPointer(key)) ||
			MuiStoreMessageCore.DispatchRecord(ref platform, packetPtr) != key) return 3;
		if (!MuiStoreMessageCore.WriteDatamapCounterRecord(ref platform,
			packetPtr, MuiStoreMessageCore.DatamapIterateMethod,
			APTR.FromPointer(counter)) ||
			MuiStoreMessageCore.DispatchRecord(ref platform, packetPtr) != counter) return 4;
		if (!MuiStoreMessageCore.WriteDatamapCounterRecord(ref platform,
			packetPtr, MuiStoreMessageCore.DatamapIterationKeyMethod,
			APTR.FromPointer(counter)) ||
			MuiStoreMessageCore.DispatchRecord(ref platform, packetPtr) != counter) return 5;
		if (!MuiStoreMessageCore.WriteDatamapKeyRecord(ref platform, packetPtr,
			MuiStoreMessageCore.DatamapRemoveMethod, APTR.FromPointer(key)) ||
			MuiStoreMessageCore.DispatchRecord(ref platform, packetPtr) != key) return 6;
		if (!MuiStoreMessageCore.WriteDatamapClearRecord(ref platform, packetPtr) ||
			MuiStoreMessageCore.DispatchRecord(ref platform, packetPtr) != 1) return 7;

		if (!MuiStoreMessageCore.WriteObjectmapSetRecord(ref platform, packetPtr,
			APTR.FromPointer(objectPtr), APTR.FromPointer(objectKey)) ||
			MuiStoreMessageCore.DispatchRecord(ref platform, packetPtr) != objectPtr) return 8;
		if (!MuiStoreMessageCore.WriteObjectmapKeyRecord(ref platform, packetPtr,
			MuiStoreMessageCore.ObjectmapFindMethod,
			APTR.FromPointer(objectKey)) ||
			MuiStoreMessageCore.DispatchRecord(ref platform, packetPtr) != objectKey) return 9;
		if (!MuiStoreMessageCore.WriteObjectmapCounterRecord(ref platform,
			packetPtr, MuiStoreMessageCore.ObjectmapIterateMethod,
			APTR.FromPointer(counter)) ||
			MuiStoreMessageCore.DispatchRecord(ref platform, packetPtr) != counter) return 10;
		if (!MuiStoreMessageCore.WriteObjectmapCounterRecord(ref platform,
			packetPtr, MuiStoreMessageCore.ObjectmapIterationKeyMethod,
			APTR.FromPointer(counter)) ||
			MuiStoreMessageCore.DispatchRecord(ref platform, packetPtr) != counter) return 11;
		if (!MuiStoreMessageCore.WriteObjectmapKeyRecord(ref platform, packetPtr,
			MuiStoreMessageCore.ObjectmapRemoveMethod,
			APTR.FromPointer(objectKey)) ||
			MuiStoreMessageCore.DispatchRecord(ref platform, packetPtr) != objectKey) return 12;
		if (!MuiStoreMessageCore.WriteObjectmapClearRecord(ref platform,
			packetPtr) || MuiStoreMessageCore.DispatchRecord(ref platform,
			packetPtr) != 1) return 13;

		const uint truncated = 0x00050FFF;
		if (MuiStoreMessageCore.DispatchRecord(ref platform,
			APTR.FromPointer(truncated)) != 0) return 14;
		return 42;
	}

	// Focused MorphOS MUIM_Family_DoChildMethods closure. The packet has only
	// MethodID, while the projection records prove bounded direct-child
	// traversal without introducing managed collections or callback state.
	public static uint FamilyDoChildMethodsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint firstNode = 0x00036A20;
		const uint secondNode = 0x00036A30;
		const uint thirdNode = 0x00036A40;
		const uint firstObject = 0x00036B00;
		const uint secondObject = 0x00036B10;
		const uint thirdObject = 0x00036B20;
		const uint packet = 0x00036B40;
		if (!MuiFamilyDoChildMethodsCore.WriteChildRecord(ref platform,
			APTR.FromPointer(firstNode), APTR.FromPointer(secondNode), APTR.Null,
			APTR.FromPointer(firstObject))) return 1;
		if (!MuiFamilyDoChildMethodsCore.WriteChildRecord(ref platform,
			APTR.FromPointer(secondNode), APTR.FromPointer(thirdNode),
			APTR.FromPointer(firstNode), APTR.FromPointer(secondObject))) return 2;
		if (!MuiFamilyDoChildMethodsCore.WriteChildRecord(ref platform,
			APTR.FromPointer(thirdNode), APTR.Null, APTR.FromPointer(secondNode),
			APTR.FromPointer(thirdObject))) return 3;
		if (!MuiFamilyDoChildMethodsCore.WriteRecord(ref platform,
			APTR.FromPointer(packet)) ||
			MuiFamilyDoChildMethodsCore.DispatchRecord(ref platform,
				APTR.FromPointer(firstNode), APTR.FromPointer(thirdNode),
				APTR.FromPointer(packet)) != 3) return 4;
		const uint truncated = 0x00050FFF;
		if (MuiFamilyDoChildMethodsCore.DispatchRecord(ref platform,
			APTR.Null, APTR.Null, APTR.FromPointer(truncated)) != 0) return 5;
		return 42;
	}

	// Focused MorphOS MUIM_CallHook closure. It proves the fixed packet and
	// CallHookPkt register mapping through the freestanding callback capability:
	// A0=hook, A2=object, and A1=&param1.
	public static uint CallHookPacketsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint hook = 0x00036C00;
		const uint data = 0x00036C40;
		const uint packet = 0x00036D00;
		const uint objectAddress = 0x00036D40;
		APTR.WriteUInt32(APTR.FromPointer(hook), 8, 0x00DD0001u);
		APTR.WriteUInt32(APTR.FromPointer(hook), 16, data);
		if (!MuiCallHookCore.WriteRecord(ref platform,
			APTR.FromPointer(packet), APTR.FromPointer(hook), 0xCAFEBABEu) ||
			MuiCallHookCore.DispatchRecord(ref platform,
				APTR.FromPointer(objectAddress), APTR.FromPointer(packet)) != data)
			return 1;
		if (APTR.ReadUInt32(APTR.FromPointer(data), 0) != hook ||
			APTR.ReadUInt32(APTR.FromPointer(data), 4) != objectAddress ||
			APTR.ReadUInt32(APTR.FromPointer(data), 8) != packet + 8u) return 2;
		const uint truncated = 0x00050FFF;
		if (MuiCallHookCore.DispatchRecord(ref platform,
			APTR.FromPointer(objectAddress), APTR.FromPointer(truncated)) != 0)
			return 3;
		return 42;
	}

	// Focused MorphOS Dataspace packet closure. All packets are constructed by
	// the named Dataspace message writers, then routed through the same focused
	// dispatcher seam used by host tests.
	public static uint DataspacePacketsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint packet = 0x00037200;
		const uint data = 0x00037300;
		const uint size = 0x00037400;
		APTR.WriteUInt32(APTR.FromPointer(data), 0, 0xAABBCCDD);
		if (!MuiDataspaceMessageCore.WriteAddRecord(ref platform,
			APTR.FromPointer(packet), APTR.FromPointer(data), 4, 19) ||
			MuiDataspaceMessageCore.DispatchRecord(ref platform,
				APTR.FromPointer(packet)) != 19) return 1;
		if (!MuiDataspaceMessageCore.WriteFindRecord(ref platform,
			APTR.FromPointer(packet), 19) ||
			MuiDataspaceMessageCore.DispatchRecord(ref platform,
				APTR.FromPointer(packet)) != 19) return 2;
		if (!MuiDataspaceMessageCore.WriteGetRecord(ref platform,
			APTR.FromPointer(packet), 19, APTR.FromPointer(size)) ||
			MuiDataspaceMessageCore.DispatchRecord(ref platform,
				APTR.FromPointer(packet)) != size) return 3;
		if (!MuiDataspaceMessageCore.WriteMergeRecord(ref platform,
			APTR.FromPointer(packet), APTR.FromPointer(data)) ||
			MuiDataspaceMessageCore.DispatchRecord(ref platform,
				APTR.FromPointer(packet)) != data) return 4;
		if (!MuiDataspaceMessageCore.WriteRemoveRecord(ref platform,
			APTR.FromPointer(packet), 19) ||
			MuiDataspaceMessageCore.DispatchRecord(ref platform,
				APTR.FromPointer(packet)) != 19) return 5;
		if (!MuiDataspaceMessageCore.WriteClearRecord(ref platform,
			APTR.FromPointer(packet)) ||
			MuiDataspaceMessageCore.DispatchRecord(ref platform,
				APTR.FromPointer(packet)) != 1) return 6;
		const uint truncated = 0x00050FFC;
		APTR.WriteUInt32(APTR.FromPointer(truncated), 0,
			MuiDataspaceMessageCore.AddMethod);
		if (MuiDataspaceMessageCore.DispatchRecord(ref platform,
			APTR.FromPointer(truncated)) != 0) return 7;
		return 42;
	}

	// Focused MorphOS Dataspace IFF packet closure. The packet records are
	// decoded by named structs; IFF stream operations remain behind the
	// capability seam and are covered by the host round-trip test.
	public static uint DataspaceIffPacketsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint packet = 0x00037500;
		const uint handle = 0x0004E000;
		if (!MuiDataspaceIffMessageCore.WriteReadIffRecord(ref platform,
			APTR.FromPointer(packet), APTR.FromPointer(handle)) ||
			MuiDataspaceIffMessageCore.DispatchRecord(ref platform,
				APTR.FromPointer(packet)) != handle) return 1;
		if (!MuiDataspaceIffMessageCore.WriteWriteIffRecord(ref platform,
			APTR.FromPointer(packet), APTR.FromPointer(handle), 0x464F524D,
			0x44415441) ||
			MuiDataspaceIffMessageCore.DispatchRecord(ref platform,
				APTR.FromPointer(packet)) != (handle ^ 0x464F524D ^ 0x44415441))
			return 2;
		const uint truncated = 0x00037FF8;
		APTR.WriteUInt32(APTR.FromPointer(truncated), 0,
			MuiDataspaceIffMessageCore.ReadIffMethod);
		if (MuiDataspaceIffMessageCore.DispatchRecord(ref platform,
			APTR.FromPointer(truncated)) != 0) return 3;
		return 42;
	}

	// Focused MorphOS Notify memory-write packet closure.  The packet records
	// are represented by named structs; the live guest-memory writes remain in
	// the host dispatcher seam and are covered by its bounded-copy test.
	public static uint NotifyWritePacketsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint packet = 0x00037600;
		const uint memory = 0x00037700;
		const uint source = 0x00037800;
		if (!MuiNotifyWriteCore.WriteLongRecord(ref platform,
			APTR.FromPointer(packet), 0xAABBCCDD, APTR.FromPointer(memory)) ||
			MuiNotifyWriteCore.DispatchRecord(ref platform,
				APTR.FromPointer(packet)) != memory) return 1;
		APTR.WriteUInt8(APTR.FromPointer(source), 0, (byte)'M');
		APTR.WriteUInt8(APTR.FromPointer(source), 1, (byte)'U');
		APTR.WriteUInt8(APTR.FromPointer(source), 2, (byte)'I');
		APTR.WriteUInt8(APTR.FromPointer(source), 3, 0);
		if (!MuiNotifyWriteCore.WriteStringRecord(ref platform,
			APTR.FromPointer(packet), APTR.FromPointer(source),
			APTR.FromPointer(memory)) ||
			MuiNotifyWriteCore.DispatchRecord(ref platform,
				APTR.FromPointer(packet)) != memory) return 2;
		const uint truncated = 0x00050FFC;
		APTR.WriteUInt32(APTR.FromPointer(truncated), 0,
			MuiNotifyWriteCore.WriteLongMethod);
		if (MuiNotifyWriteCore.DispatchRecord(ref platform,
			APTR.FromPointer(truncated)) != 0) return 3;
		return 42;
	}

	// Focused MorphOS Notify SetAsString packet closure.  The fixed header is
	// decoded as a named record; the variadic formatting and owned text copy
	// are covered by the host dispatcher test and stay out of this tiny seam.
	public static uint SetAsStringPacketsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint packet = 0x00037A00;
		const uint format = 0x00037B00;
		const uint attribute = 0x8042AAAA;
		if (!MuiNotifySetAsStringCore.WriteRecord(ref platform,
			APTR.FromPointer(packet), attribute, APTR.FromPointer(format), 42) ||
			MuiNotifySetAsStringCore.DispatchRecord(ref platform,
				APTR.FromPointer(packet)) != attribute) return 1;
		const uint truncated = 0x00050FFC;
		APTR.WriteUInt32(APTR.FromPointer(truncated), 0,
			MuiNotifySetAsStringCore.Method);
		if (MuiNotifySetAsStringCore.DispatchRecord(ref platform,
			APTR.FromPointer(truncated)) != 0) return 2;
		return 42;
	}

	// Focused MorphOS BoopsiQuery packet closure. The SDK exposes this packet
	// through the MUIP_BoopsiQuery alias; the complete named record is qualified
	// here while external BOOPSI callback semantics remain capability-backed.
	public static uint BoopsiQueryPacketsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint packet = 0x00037C00;
		const uint screen = 0x00037D00;
		const uint renderInfo = 0x00037E00;
		const uint flags = 0x000000A5;
		if (!MuiBoopsiQueryCore.WriteRecord(ref platform,
			APTR.FromPointer(packet), APTR.FromPointer(screen), flags,
			8, 9, 640, 480, 320, 200, APTR.FromPointer(renderInfo)) ||
			MuiBoopsiQueryCore.DispatchRecord(ref platform,
				APTR.FromPointer(packet)) != flags) return 1;
		if (!MuiBoopsiQueryCore.TryRead(ref platform,
			APTR.FromPointer(packet), out var query) ||
			query.RenderInfo.Raw != renderInfo || query.MaxWidth != 640) return 2;
		const uint truncated = 0x00050FFC;
		APTR.WriteUInt32(APTR.FromPointer(truncated), 0,
			MuiBoopsiQueryCore.Method);
		if (MuiBoopsiQueryCore.DispatchRecord(ref platform,
			APTR.FromPointer(truncated)) != 0) return 3;
		return 42;
	}

	// Focused MorphOS UpdateConfig packet closure. The complete inline redraw
	// tables are represented by nested value-type records; this root checks the
	// first and final named entries without exposing guest offsets to callers.
	public static uint UpdateConfigPacketsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint packet = 0x00037F00;
		const uint first = 0x00038100;
		const uint last = 0x00038200;
		const uint cfgId = 0x8042BEEF;
		if (!MuiUpdateConfigCore.WriteRecord(ref platform,
			APTR.FromPointer(packet), cfgId, 2) ||
			!MuiUpdateConfigCore.WriteEntry(ref platform,
				APTR.FromPointer(packet), 0, APTR.FromPointer(first), 0x11) ||
			!MuiUpdateConfigCore.WriteEntry(ref platform,
				APTR.FromPointer(packet), 63, APTR.FromPointer(last), 0xA5) ||
			MuiUpdateConfigCore.DispatchRecord(ref platform,
				APTR.FromPointer(packet)) != cfgId) return 1;
		if (!MuiUpdateConfigCore.TryRead(ref platform,
			APTR.FromPointer(packet), out var update) ||
			update.CfgId != cfgId || update.RedrawCount != 2 ||
			update.RedrawObjects.Object00.Raw != first ||
			update.RedrawObjects.Object63.Raw != last ||
			update.RedrawFlags.Flag00 != 0x11 ||
			update.RedrawFlags.Flag63 != 0xA5) return 2;
		const uint truncated = 0x00050FFC;
		APTR.WriteUInt32(APTR.FromPointer(truncated), 0,
			MuiUpdateConfigCore.Method);
		if (MuiUpdateConfigCore.DispatchRecord(ref platform,
			APTR.FromPointer(truncated)) != 0) return 3;
		return 42;
	}

	// Focused MorphOS Notify packet closure. The packet headers are decoded by
	// the struct-based dispatcher; this root exercises add, notifying Set,
	// object-specific removal, and truncated-packet rejection.
	public static uint NotifyPacketRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		const uint follow = 0x00036240;
		WriteClassId(APTR.FromPointer(className), 'N', 'o', 't', 'i', 'f', 'y',
			(char)0, (char)0, (char)0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(className), APTR.Null, 8,
			APTR.FromPointer(1));
		if (cl.IsNull) return 2;
		var source = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), cl, APTR.Null);
		var destination = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), cl, APTR.Null);
		if (source.IsNull || destination.IsNull) return 3;
		APTR.WriteUInt32(APTR.FromPointer(follow), 0, 0x90000001);
		APTR.WriteUInt32(APTR.FromPointer(follow), 4, 1233727793);
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiNotifyCore.NotifyMethod);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0x80420020);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 1233727793);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, destination.Raw);
		APTR.WriteUInt32(APTR.FromPointer(packet), 16, 2);
		APTR.WriteUInt32(APTR.FromPointer(packet), 20, 0x90000001);
		APTR.WriteUInt32(APTR.FromPointer(packet), 24, 1233727793);
		if (MuiHeadlessDispatcher.DispatchNotify(ref platform,
			APTR.FromPointer(state), source, APTR.FromPointer(packet)) != 1) return 4;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, MuiNotifyCore.SetMethod);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0x80420020);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 77);
		if (MuiHeadlessDispatcher.DispatchNotify(ref platform,
			APTR.FromPointer(state), source, APTR.FromPointer(packet)) != 1) return 5;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiNotifyCore.KillNotifyObjectMethod);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0x80420020);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, destination.Raw);
		if (MuiHeadlessDispatcher.DispatchNotify(ref platform,
			APTR.FromPointer(state), source, APTR.FromPointer(packet)) != 1) return 6;
		const uint truncated = 0x00050FF0;
		APTR.WriteUInt32(APTR.FromPointer(truncated), 0,
			MuiNotifyCore.NotifyMethod);
		if (MuiHeadlessDispatcher.DispatchNotify(ref platform,
			APTR.FromPointer(state), source, APTR.FromPointer(truncated)) != 0) return 7;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 8;
		return 42;
	}

	// Focused MorphOS MUIM_MultiSet closure. The executor is deliberately also
	// the first listed object so the public "executor is not affected" rule is
	// exercised alongside the inline NULL-terminated target vector.
	public static uint MultiSetRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		WriteClassId(APTR.FromPointer(className), 'N', 'o', 't', 'i', 'f', 'y',
			(char)0, (char)0, (char)0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(className), APTR.Null, 8,
			APTR.FromPointer(1));
		if (cl.IsNull) return 2;
		var executor = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), cl, APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), cl, APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), cl, APTR.Null);
		if (executor.IsNull || first.IsNull || second.IsNull) return 3;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiNotifyCore.MultiSetMethod);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0x80420030);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 0xCAFE);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, executor.Raw);
		APTR.WriteUInt32(APTR.FromPointer(packet), 16, first.Raw);
		APTR.WriteUInt32(APTR.FromPointer(packet), 20, second.Raw);
		APTR.WriteUInt32(APTR.FromPointer(packet), 24, 0);
		if (MuiHeadlessDispatcher.DispatchNotify(ref platform,
			APTR.FromPointer(state), executor, APTR.FromPointer(packet)) != 1) return 4;
		uint value;
		if (MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), executor, 0x80420030, out value)) return 5;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), first, 0x80420030, out value) || value != 0xCAFE)
			return 6;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), second, 0x80420030, out value) || value != 0xCAFE)
			return 7;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 8;
		return 42;
	}

	// Focused MorphOS MUIM_FindObject closure. The fixed packet is decoded into
	// a struct and the implementation follows guest parent records with a
	// bounded, allocation-free walk.
	public static uint FindObjectRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		const uint truncated = 0x00050FFC;
		WriteClassId(APTR.FromPointer(className), 'N', 'o', 't', 'i', 'f', 'y',
			(char)0, (char)0, (char)0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(className), APTR.Null, 8,
			APTR.FromPointer(1));
		if (cl.IsNull) return 2;
		var root = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), cl, APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), cl, APTR.Null);
		var grandchild = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), cl, APTR.Null);
		var unrelated = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), cl, APTR.Null);
		if (root.IsNull || child.IsNull || grandchild.IsNull || unrelated.IsNull)
			return 3;
		if (!MuiFamilyCore.AddTail(ref platform, APTR.FromPointer(state), root,
			child) || !MuiFamilyCore.AddTail(ref platform, APTR.FromPointer(state),
			child, grandchild)) return 4;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiNotifyCore.FindObjectMethod);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, root.Raw);
		if (MuiHeadlessDispatcher.DispatchNotify(ref platform,
			APTR.FromPointer(state), root, APTR.FromPointer(packet)) != 1) return 5;
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, grandchild.Raw);
		if (MuiHeadlessDispatcher.DispatchNotify(ref platform,
			APTR.FromPointer(state), root, APTR.FromPointer(packet)) != 1) return 6;
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, unrelated.Raw);
		if (MuiHeadlessDispatcher.DispatchNotify(ref platform,
			APTR.FromPointer(state), root, APTR.FromPointer(packet)) != 0) return 7;
		if (!MuiHeadlessObjectCore.DisposeObject(ref platform,
			APTR.FromPointer(state), unrelated)) return 8;
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, unrelated.Raw);
		if (MuiHeadlessDispatcher.DispatchNotify(ref platform,
			APTR.FromPointer(state), root, APTR.FromPointer(packet)) != 0) return 9;
		APTR.WriteUInt32(APTR.FromPointer(truncated), 0,
			MuiNotifyCore.FindObjectMethod);
		if (MuiHeadlessDispatcher.DispatchNotify(ref platform,
			APTR.FromPointer(state), root, APTR.FromPointer(truncated)) != 0) return 10;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 11;
		return 42;
	}

	// Focused MorphOS MUIM_Application_ReturnID closure. The fixed packet is
	// decoded into a struct, queued in guest memory, and consumed FIFO by the
	// application Input path.
	public static uint ApplicationReturnIdRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		const uint signals = 0x00036240;
		const uint truncated = 0x00050FFC;
		WriteClassId(APTR.FromPointer(className), 'A', 'p', 'p', 'l', 'i', 'c', 'a',
			't', 'i');
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(className), APTR.Null, 8,
			APTR.FromPointer(1));
		if (cl.IsNull) return 2;
		var application = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), cl, APTR.Null);
		if (application.IsNull || !MuiApplicationWindowCore.InitializeApplication(
			ref platform, APTR.FromPointer(state), application, 0x20)) return 3;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.ApplicationReturnIdMethod);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 41);
		if (MuiApplicationDispatcher.DispatchApplicationReturnId(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(packet)) != 1) return 4;
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 42);
		if (MuiApplicationDispatcher.DispatchApplicationReturnId(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(packet)) != 1) return 5;
		if (MuiApplicationWindowCore.Input(ref platform, APTR.FromPointer(state),
			application, APTR.FromPointer(signals)) != 41 ||
			MuiApplicationWindowCore.Input(ref platform, APTR.FromPointer(state),
				application, APTR.FromPointer(signals)) != 42) return 6;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0xDEADBEEFu);
		if (MuiApplicationDispatcher.DispatchApplicationReturnId(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(packet)) != 0) return 7;
		APTR.WriteUInt32(APTR.FromPointer(truncated), 0,
			MuiApplicationDispatcher.ApplicationReturnIdMethod);
		if (MuiApplicationDispatcher.DispatchApplicationReturnId(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(truncated)) != 0) return 8;
		if (!MuiHeadlessObjectCore.DisposeObject(ref platform,
			APTR.FromPointer(state), application)) return 9;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.ApplicationReturnIdMethod);
		if (MuiApplicationDispatcher.DispatchApplicationReturnId(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(packet)) != 0) return 10;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 11;
		return 42;
	}

	// Focused MorphOS MUIM_Application_Input/NewInput closure. Both fixed
	// `{MethodID, signal}` packets consume the same guest ReturnID queue, while
	// a null signal pointer remains a valid no-storage call.
	public static uint ApplicationInputRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint returnPacket = 0x00036200;
		const uint inputPacket = 0x00036220;
		const uint signals = 0x00036240;
		const uint truncated = 0x00050FFC;
		WriteClassId(APTR.FromPointer(className), 'A', 'p', 'p', 'l', 'i', 'c', 'a',
			't', 'i');
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(className), APTR.Null, 8,
			APTR.FromPointer(1));
		if (cl.IsNull) return 2;
		var application = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), cl, APTR.Null);
		if (application.IsNull || !MuiApplicationWindowCore.InitializeApplication(
			ref platform, APTR.FromPointer(state), application, 0x20)) return 3;
		APTR.WriteUInt32(APTR.FromPointer(returnPacket), 0,
			MuiApplicationDispatcher.ApplicationReturnIdMethod);
		APTR.WriteUInt32(APTR.FromPointer(returnPacket), 4, 77);
		if (MuiApplicationDispatcher.DispatchApplicationReturnId(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(returnPacket)) != 1) return 4;
		APTR.WriteUInt32(APTR.FromPointer(inputPacket), 0,
			MuiApplicationDispatcher.ApplicationNewInputMethod);
		APTR.WriteUInt32(APTR.FromPointer(inputPacket), 4, signals);
		if (MuiApplicationDispatcher.DispatchApplicationInput(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(inputPacket)) != 77 ||
			APTR.ReadUInt32(APTR.FromPointer(signals), 0) != 0) return 5;
		APTR.WriteUInt32(APTR.FromPointer(inputPacket), 0,
			MuiApplicationDispatcher.ApplicationInputMethod);
		APTR.WriteUInt32(APTR.FromPointer(inputPacket), 4, 0);
		if (MuiApplicationDispatcher.DispatchApplicationInput(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(inputPacket)) != 0) return 6;
		APTR.WriteUInt32(APTR.FromPointer(inputPacket), 0, 0xDEADBEEFu);
		if (MuiApplicationDispatcher.DispatchApplicationInput(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(inputPacket)) != 0) return 7;
		APTR.WriteUInt32(APTR.FromPointer(truncated), 0,
			MuiApplicationDispatcher.ApplicationInputMethod);
		if (MuiApplicationDispatcher.DispatchApplicationInput(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(truncated)) != 0) return 8;
		if (!MuiHeadlessObjectCore.DisposeObject(ref platform,
			APTR.FromPointer(state), application)) return 9;
		APTR.WriteUInt32(APTR.FromPointer(inputPacket), 0,
			MuiApplicationDispatcher.ApplicationInputMethod);
		if (MuiApplicationDispatcher.DispatchApplicationInput(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(inputPacket)) != 0) return 10;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 11;
		return 42;
	}

	// Focused MorphOS MUIM_Application_InputBuffered closure. The exact
	// zero-argument packet dispatches one copied PushMethod record and then
	// becomes a no-op when the queue is empty.
	public static uint ApplicationInputBufferedRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		const uint parameters = 0x00036240;
		const uint unmapped = 0x00052000;
		WriteClassId(APTR.FromPointer(className), 'A', 'p', 'p', 'l', 'i', 'c', 'a',
			't', 'i');
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(className), APTR.Null, 8,
			APTR.FromPointer(1));
		if (cl.IsNull) return 2;
		var application = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), cl, APTR.Null);
		var target = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), cl, APTR.Null);
		if (application.IsNull || target.IsNull ||
			!MuiApplicationWindowCore.InitializeApplication(ref platform,
				APTR.FromPointer(state), application, 0)) return 3;
		APTR.WriteUInt32(APTR.FromPointer(parameters), 0, 0x90000001);
		APTR.WriteUInt32(APTR.FromPointer(parameters), 4, 77);
		if (MuiApplicationWindowCore.PushMethod(ref platform,
			APTR.FromPointer(state), application, target, 2,
			APTR.FromPointer(parameters)) == 0) return 4;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.ApplicationInputBufferedMethod);
		if (MuiApplicationDispatcher.DispatchApplicationInputBuffered(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(packet)) != 1) return 5;
		if (MuiApplicationDispatcher.DispatchApplicationInputBuffered(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(packet)) != 0) return 6;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0xDEADBEEFu);
		if (MuiApplicationDispatcher.DispatchApplicationInputBuffered(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(packet)) != 0) return 7;
		if (MuiApplicationDispatcher.DispatchApplicationInputBuffered(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(unmapped)) != 0) return 8;
		if (!MuiHeadlessObjectCore.DisposeObject(ref platform,
			APTR.FromPointer(state), application)) return 9;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.ApplicationInputBufferedMethod);
		if (MuiApplicationDispatcher.DispatchApplicationInputBuffered(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(packet)) != 0) return 10;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 11;
		return 42;
	}

	// Focused MorphOS AddInputHandler/RemInputHandler closure. The exact
	// `{MethodID, ihnode}` packets manage a guest handler record and the native
	// input walk delivers its configured method on the requested signal.
	public static uint ApplicationInputHandlerRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		const uint handler = 0x00036240;
		const uint unmapped = 0x00052000;
		WriteClassId(APTR.FromPointer(className), 'A', 'p', 'p', 'l', 'i', 'c', 'a',
			't', 'i');
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(className), APTR.Null, 8,
			APTR.FromPointer(1));
		if (cl.IsNull) return 2;
		var application = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), cl, APTR.Null);
		var target = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), cl, APTR.Null);
		if (application.IsNull || target.IsNull ||
			!MuiApplicationWindowCore.InitializeApplication(ref platform,
				APTR.FromPointer(state), application, 0)) return 3;
		APTR.WriteUInt32(APTR.FromPointer(handler), 8, target.Raw);
		APTR.WriteUInt32(APTR.FromPointer(handler), 12, 0x20);
		APTR.WriteUInt32(APTR.FromPointer(handler), 20, 0x90000001);
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.AddInputHandlerMethod);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, handler);
		if (MuiApplicationDispatcher.DispatchApplicationInputHandler(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(packet)) != 1) return 4;
		if (MuiApplicationWindowCore.DispatchInputHandlers(ref platform,
			APTR.FromPointer(state), application, 0x20) != 1) return 5;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.RemoveInputHandlerMethod);
		if (MuiApplicationDispatcher.DispatchApplicationInputHandler(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(packet)) != 1) return 6;
		if (MuiApplicationWindowCore.DispatchInputHandlers(ref platform,
			APTR.FromPointer(state), application, 0x20) != 0) return 7;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0xDEADBEEFu);
		if (MuiApplicationDispatcher.DispatchApplicationInputHandler(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(packet)) != 0) return 8;
		if (MuiApplicationDispatcher.DispatchApplicationInputHandler(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(unmapped)) != 0) return 9;
		if (!MuiHeadlessObjectCore.DisposeObject(ref platform,
			APTR.FromPointer(state), application)) return 10;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.AddInputHandlerMethod);
		if (MuiApplicationDispatcher.DispatchApplicationInputHandler(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(packet)) != 0) return 11;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 12;
		return 42;
	}

	// Focused MorphOS application menu closure. Get packets use
	// `{MethodID, MenuID}` and Set packets use `{MethodID, MenuID, stat}`;
	// both traverse the application's open child windows.
	public static uint ApplicationMenuStateRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		const uint unmapped = 0x00052000;
		WriteClassId(APTR.FromPointer(className), 'A', 'p', 'p', 'l', 'i', 'c', 'a',
			't', 'i');
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(className), APTR.Null, 8,
			APTR.FromPointer(1));
		if (cl.IsNull) return 2;
		var application = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), cl, APTR.Null);
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), cl, APTR.Null);
		if (application.IsNull || window.IsNull ||
			!MuiApplicationWindowCore.InitializeApplication(ref platform,
				APTR.FromPointer(state), application, 0) ||
			!MuiApplicationWindowCore.AddWindow(ref platform,
				APTR.FromPointer(state), application, window) ||
			!MuiApplicationWindowCore.OpenWindow(ref platform,
				APTR.FromPointer(state), window, 0)) return 3;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.ApplicationGetMenuCheckMethod);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 7);
		if (MuiApplicationDispatcher.DispatchApplicationMenuState(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(packet)) != 1) return 4;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.ApplicationGetMenuStateMethod);
		if (MuiApplicationDispatcher.DispatchApplicationMenuState(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(packet)) != 1) return 5;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.ApplicationSetMenuCheckMethod);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 1);
		if (MuiApplicationDispatcher.DispatchApplicationMenuState(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(packet)) != 1) return 6;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.ApplicationSetMenuStateMethod);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 0);
		if (MuiApplicationDispatcher.DispatchApplicationMenuState(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(packet)) != 1) return 7;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0xDEADBEEFu);
		if (MuiApplicationDispatcher.DispatchApplicationMenuState(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(packet)) != 0) return 8;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.ApplicationSetMenuCheckMethod);
		if (MuiApplicationDispatcher.DispatchApplicationMenuState(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(unmapped)) != 0) return 9;
		if (!MuiHeadlessObjectCore.DisposeObject(ref platform,
			APTR.FromPointer(state), application)) return 10;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.ApplicationGetMenuStateMethod);
		if (MuiApplicationDispatcher.DispatchApplicationMenuState(ref platform,
			APTR.FromPointer(state), application, APTR.FromPointer(packet)) != 0) return 11;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 12;
		return 42;
	}

	// Packet-only MorphOS MUIM_Export/MUIM_Import qualification. The exact
	// record is {MethodID, dataspace}; live ownership stays in the persistence
	// core exercised by the host suite.
	public static uint ObjectPersistencePacketsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint packet = 0x00036580;
		const uint dataspace = 0x000365A0;
		var record = APTR.FromPointer(packet);
		if (!MuiObjectPersistenceMessageCore.WriteExportRecord(ref platform,
			record, APTR.FromPointer(dataspace)) ||
			MuiObjectPersistenceMessageCore.DispatchRecord(ref platform, record) !=
			dataspace) return 1;
		if (!MuiObjectPersistenceMessageCore.WriteImportRecord(ref platform,
			record, APTR.FromPointer(dataspace)) ||
			MuiObjectPersistenceMessageCore.DispatchRecord(ref platform, record) !=
			dataspace) return 2;
		const uint truncated = 0x00050FFF;
		if (MuiObjectPersistenceMessageCore.DispatchRecord(ref platform,
			APTR.FromPointer(truncated)) != 0) return 3;
		APTR.WriteUInt32(record, 0, 0xDEADBEEFu);
		if (MuiObjectPersistenceMessageCore.DispatchRecord(ref platform, record) !=
			0) return 4;
		return 42;
	}

	// MorphOS MUIM_Export/MUIM_Import packet closure. The exact packet is
	// {MethodID, dataspace}; the Notify superclass requires a non-zero
	// MUIA_ObjectID and rejects dead objects before the native payload seam.
	public static uint ObjectPersistenceRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint numericName = 0x00036140;
		const uint stringName = 0x00036180;
		const uint textName = 0x000361C0;
		const uint imageName = 0x00036200;
		const uint groupName = 0x00036240;
		const uint packet = 0x00036300;
		const uint stringSource = 0x00036400;
		const uint textSource = 0x00036440;
		const uint stringReplacement = 0x00036480;
		const uint textReplacement = 0x000364C0;
		WriteClassId(APTR.FromPointer(className), 'N', 'o', 't', 'i', 'f', 'y',
			(char)0, (char)0, (char)0);
		WriteClassId(APTR.FromPointer(numericName), 'N', 'u', 'm', 'e', 'r', 'i',
			'c', (char)0, (char)0);
		WriteClassId(APTR.FromPointer(stringName), 'S', 't', 'r', 'i', 'n', 'g',
			(char)0, (char)0, (char)0);
		WriteClassId(APTR.FromPointer(textName), 'T', 'e', 'x', 't',
			(char)0, (char)0, (char)0, (char)0, (char)0);
		WriteClassId(APTR.FromPointer(imageName), 'I', 'm', 'a', 'g', 'e',
			(char)0, (char)0, (char)0, (char)0);
		WriteClassId(APTR.FromPointer(groupName), 'G', 'r', 'o', 'u', 'p',
			(char)0, (char)0, (char)0, (char)0);
		WriteGuestString(APTR.FromPointer(stringSource), 'h', 'e', 'l', 'l', 'o');
		WriteGuestString(APTR.FromPointer(textSource), 's', 't', 'a', 't', 'u', 's');
		WriteGuestString(APTR.FromPointer(stringReplacement), 'c', 'h', 'a', 'n', 'g', 'e', 'd');
		WriteGuestString(APTR.FromPointer(textReplacement), 'u', 'p', 'd', 'a', 't', 'e', 'd');
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(className), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var numericClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(numericName), APTR.Null, 0,
			APTR.FromPointer(1));
		if (numericClass.IsNull) return 3;
		var stringClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(stringName), APTR.Null, 0,
			APTR.FromPointer(1));
		var textClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(textName), APTR.Null, 0,
			APTR.FromPointer(1));
		var imageClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(imageName), APTR.Null, 0,
			APTR.FromPointer(1));
		var groupClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(groupName), APTR.Null, 0,
			APTR.FromPointer(1));
		if (stringClass.IsNull || textClass.IsNull || imageClass.IsNull ||
			groupClass.IsNull) return 4;
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var dataspace = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var numeric = MuiCommonControlCore.CreateControl(ref platform,
			APTR.FromPointer(state), numericClass, APTR.Null);
		var stringObject = MuiCommonControlCore.CreateControl(ref platform,
			APTR.FromPointer(state), stringClass, APTR.Null);
		var textObject = MuiCommonControlCore.CreateControl(ref platform,
			APTR.FromPointer(state), textClass, APTR.Null);
		var imageObject = MuiCommonControlCore.CreateControl(ref platform,
			APTR.FromPointer(state), imageClass, APTR.Null);
		var groupObject = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), groupClass, APTR.Null);
		if (obj.IsNull || dataspace.IsNull || numeric.IsNull ||
			stringObject.IsNull || textObject.IsNull || imageObject.IsNull ||
			groupObject.IsNull) return 5;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform,
			APTR.FromPointer(state), obj, 0x8042D76E, 0xCAFE, false)) return 6;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80420F1C);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, dataspace.Raw);
		if (MuiHeadlessDispatcher.DispatchObjectPersistence(ref platform,
			APTR.FromPointer(state), obj, APTR.FromPointer(packet)) != 1) return 7;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042D012);
		if (MuiHeadlessDispatcher.DispatchObjectPersistence(ref platform,
			APTR.FromPointer(state), obj, APTR.FromPointer(packet)) != 1) return 8;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform,
			APTR.FromPointer(state), obj, 0x8042D76E, 0, false)) return 9;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80420F1C);
		if (MuiHeadlessDispatcher.DispatchObjectPersistence(ref platform,
			APTR.FromPointer(state), obj, APTR.FromPointer(packet)) != 0) return 10;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform,
			APTR.FromPointer(state), numeric, 0x8042D76E, 0x9001, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform,
				APTR.FromPointer(state), numeric, 0x8042AE3A, 73, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
				stringObject, 0x8042D76E, 0x9002, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
				textObject, 0x8042D76E, 0x9003, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
				imageObject, 0x8042D76E, 0x9004, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
				groupObject, 0x8042D76E, 0x9005, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
				stringObject, 0x80428FFD, stringSource, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
				textObject, 0x8042F8DC, textSource, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
				imageObject, 0x8042654B, 1, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
				groupObject, 0x80424199, 3, false)) return 11;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80420F1C);
		if (MuiHeadlessDispatcher.DispatchObjectPersistence(ref platform,
			APTR.FromPointer(state), numeric, APTR.FromPointer(packet)) != 1 ||
			MuiStoreCore.DataspaceLength(ref platform, APTR.FromPointer(state),
				dataspace, 0x9001) != 4) return 12;
		if (MuiHeadlessDispatcher.DispatchObjectPersistence(ref platform,
			APTR.FromPointer(state), stringObject, APTR.FromPointer(packet)) != 1 ||
			MuiHeadlessDispatcher.DispatchObjectPersistence(ref platform,
				APTR.FromPointer(state), textObject, APTR.FromPointer(packet)) != 1 ||
			MuiHeadlessDispatcher.DispatchObjectPersistence(ref platform,
				APTR.FromPointer(state), imageObject, APTR.FromPointer(packet)) != 1 ||
			MuiHeadlessDispatcher.DispatchObjectPersistence(ref platform,
				APTR.FromPointer(state), groupObject, APTR.FromPointer(packet)) != 1 ||
			MuiStoreCore.DataspaceLength(ref platform, APTR.FromPointer(state),
				dataspace, 0x9002) != 6 ||
			MuiStoreCore.DataspaceLength(ref platform, APTR.FromPointer(state),
				dataspace, 0x9003) != 7 ||
			MuiStoreCore.DataspaceLength(ref platform, APTR.FromPointer(state),
				dataspace, 0x9004) != 4 ||
			MuiStoreCore.DataspaceLength(ref platform, APTR.FromPointer(state),
				dataspace, 0x9005) != 4) return 13;
		var stored = MuiStoreCore.DataspaceFind(ref platform,
			APTR.FromPointer(state), dataspace, 0x9001);
		var storedString = MuiStoreCore.DataspaceFind(ref platform,
			APTR.FromPointer(state), dataspace, 0x9002);
		var storedText = MuiStoreCore.DataspaceFind(ref platform,
			APTR.FromPointer(state), dataspace, 0x9003);
		var storedSelected = MuiStoreCore.DataspaceFind(ref platform,
			APTR.FromPointer(state), dataspace, 0x9004);
		var storedPage = MuiStoreCore.DataspaceFind(ref platform,
			APTR.FromPointer(state), dataspace, 0x9005);
		if (stored.IsNull || APTR.ReadUInt32(stored, 0) != 73 ||
			storedString.IsNull || APTR.ReadUInt8(storedString, 0) != (byte)'h' ||
			storedText.IsNull || APTR.ReadUInt8(storedText, 0) != (byte)'s' ||
			storedSelected.IsNull || APTR.ReadUInt32(storedSelected, 0) != 1 ||
			storedPage.IsNull || APTR.ReadUInt32(storedPage, 0) != 3) return 14;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform,
			APTR.FromPointer(state), numeric, 0x8042AE3A, 12, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
				stringObject, 0x80428FFD, stringReplacement, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
				textObject, 0x8042F8DC, textReplacement, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
				imageObject, 0x8042654B, 0, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
				groupObject, 0x80424199, 0, false)) return 15;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042D012);
		if (MuiHeadlessDispatcher.DispatchObjectPersistence(ref platform,
			APTR.FromPointer(state), numeric, APTR.FromPointer(packet)) != 1 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), numeric, 0x8042AE3A, out var restored) ||
			restored != 73 ||
			MuiHeadlessDispatcher.DispatchObjectPersistence(ref platform,
				APTR.FromPointer(state), stringObject, APTR.FromPointer(packet)) != 1 ||
			MuiHeadlessDispatcher.DispatchObjectPersistence(ref platform,
				APTR.FromPointer(state), textObject, APTR.FromPointer(packet)) != 1 ||
			MuiHeadlessDispatcher.DispatchObjectPersistence(ref platform,
				APTR.FromPointer(state), imageObject, APTR.FromPointer(packet)) != 1 ||
			MuiHeadlessDispatcher.DispatchObjectPersistence(ref platform,
				APTR.FromPointer(state), groupObject, APTR.FromPointer(packet)) != 1 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
				stringObject, 0x80428FFD, out var restoredString) ||
			APTR.ReadUInt8(APTR.FromPointer(restoredString), 0) != (byte)'h' ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
				textObject, 0x8042F8DC, out var restoredText) ||
			APTR.ReadUInt8(APTR.FromPointer(restoredText), 0) != (byte)'s' ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
				imageObject, 0x8042654B, out var restoredSelected) || restoredSelected != 1 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
				imageObject, 0x8042A3AD, out var restoredImageState) || restoredImageState != 1 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
				groupObject, 0x80424199, out var restoredPage) || restoredPage != 3) return 16;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 17;
		return 42;
	}

	// Native qualification root for the application-level object-graph walker.
	// The graph is intentionally shallow so the test exercises the bounded
	// guest frame stack without requiring a large native arena.
	public static uint ApplicationPersistenceTreeRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint notifyName = 0x00036100;
		const uint numericName = 0x00036140;
		const uint dataspaceName = 0x00036180;
		WriteClassId(APTR.FromPointer(notifyName), 'N', 'o', 't', 'i', 'f', 'y',
			(char)0, (char)0, (char)0);
		WriteClassId(APTR.FromPointer(numericName), 'N', 'u', 'm', 'e', 'r', 'i',
			'c', (char)0, (char)0);
		WriteClassId(APTR.FromPointer(dataspaceName), 'D', 'a', 't', 'a', 's',
			'p', 'a', 'c', 'e');
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var notifyClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(notifyName), APTR.Null, 0,
			APTR.FromPointer(1));
		var numericClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(numericName), APTR.Null, 0,
			APTR.FromPointer(1));
		var dataspaceClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(dataspaceName), APTR.Null, 0,
			APTR.FromPointer(1));
		if (notifyClass.IsNull || numericClass.IsNull || dataspaceClass.IsNull)
			return 2;
		var application = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), notifyClass, APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), notifyClass, APTR.Null);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), notifyClass, APTR.Null);
		var numeric = MuiCommonControlCore.CreateControl(ref platform,
			APTR.FromPointer(state), numericClass, APTR.Null);
		var dataspace = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), dataspaceClass, APTR.Null);
		if (application.IsNull || first.IsNull || group.IsNull || numeric.IsNull ||
			dataspace.IsNull) return 3;
		if (!MuiFamilyCore.AddTail(ref platform, APTR.FromPointer(state),
			application, first) ||
			!MuiFamilyCore.AddTail(ref platform, APTR.FromPointer(state),
				application, group) ||
			!MuiFamilyCore.AddTail(ref platform, APTR.FromPointer(state),
				group, numeric)) return 4;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			first, 0x8042D76E, 0xA001, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			numeric, 0x8042D76E, 0xA002, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			numeric, 0x8042AE3A, 73, false)) return 5;
		if (!MuiApplicationPersistenceCore.Export(ref platform,
			APTR.FromPointer(state), application, dataspace) ||
			MuiStoreCore.DataspaceLength(ref platform, APTR.FromPointer(state),
				dataspace, 0xA002) != 4 ||
			APTR.ReadUInt32(MuiStoreCore.DataspaceFind(ref platform,
				APTR.FromPointer(state), dataspace, 0xA002), 0) != 73) return 6;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			numeric, 0x8042AE3A, 7, false) ||
			!MuiApplicationPersistenceCore.Import(ref platform,
				APTR.FromPointer(state), application, dataspace) ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), numeric, 0x8042AE3A, out var restored) ||
			restored != 73) return 7;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 8;
		return 42;
	}

	// Native qualification root for the DOS-backed application settings
	// transport. It exercises the guest file header/records, a full Save/Load
	// round trip, and the built-in Numeric persistence path.
	public static uint ApplicationPersistenceFileRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint applicationName = 0x00036100;
		const uint numericName = 0x00036140;
		const uint dataspaceName = 0x00036180;
		WriteClassId(APTR.FromPointer(applicationName), 'A', 'p', 'p', 'l',
			'i', 'c', 'a', 't', 'i');
		WriteClassId(APTR.FromPointer(numericName), 'N', 'u', 'm', 'e', 'r',
			'i', 'c', (char)0, (char)0);
		WriteClassId(APTR.FromPointer(dataspaceName), 'D', 'a', 't', 'a',
			's', 'p', 'a', 'c', 'e');
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var applicationClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(applicationName), APTR.Null, 0,
			APTR.FromPointer(1));
		var numericClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(numericName), APTR.Null, 0,
			APTR.FromPointer(1));
		var dataspaceClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(dataspaceName), APTR.Null, 0,
			APTR.FromPointer(1));
		if (applicationClass.IsNull || numericClass.IsNull || dataspaceClass.IsNull)
			return 2;
		var application = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), applicationClass, APTR.Null);
		var numeric = MuiCommonControlCore.CreateControl(ref platform,
			APTR.FromPointer(state), numericClass, APTR.Null);
		if (application.IsNull || numeric.IsNull || !MuiFamilyCore.AddTail(ref platform,
			APTR.FromPointer(state), application, numeric) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
				numeric, 0x8042D76E, 0xB101, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
				numeric, 0x8042AE3A, 73, false)) return 3;
		if (!MuiApplicationSettingsFileCore.Save(ref platform,
			APTR.FromPointer(state), application, APTR.Null)) return 4;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			numeric, 0x8042AE3A, 7, false) ||
			!MuiApplicationSettingsFileCore.Load(ref platform,
				APTR.FromPointer(state), application, APTR.FromPointer(uint.MaxValue))) return 5;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			numeric, 0x8042AE3A, out var restored) || restored != 73) return 6;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 7;
		return 42;
	}

	public static uint LayoutCoreRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint packet = 0x00036300;
		const uint renderInfo = 0x00036400;
		const uint minMax = 0x00036440;
		const uint text = 0x00036480;
		APTR.WriteUInt8(APTR.FromPointer(name), 0, (byte)'G');
		APTR.WriteUInt8(APTR.FromPointer(name), 1, 0);
		APTR.WriteUInt8(APTR.FromPointer(text), 0, (byte)'a');
		APTR.WriteUInt8(APTR.FromPointer(text), 1, (byte)'b');
		APTR.WriteUInt8(APTR.FromPointer(text), 2, (byte)'c');
		APTR.WriteUInt8(APTR.FromPointer(text), 3, 0);
		APTR.WriteUInt32(APTR.FromPointer(renderInfo), 20, 0x00036500);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1)).Raw;
		if (cl == 0) return 2;
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(cl), APTR.Null).Raw;
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(cl), APTR.Null).Raw;
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(cl), APTR.Null).Raw;
		var scroll = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(cl), APTR.Null).Raw;
		if (group == 0 || first == 0 || second == 0 || scroll == 0) return 3;
		if (!MuiFamilyCore.AddTail(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(group), APTR.FromPointer(first)) ||
			!MuiFamilyCore.AddTail(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(group), APTR.FromPointer(second))) return 4;
		MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(group), 0x8042536B, 1, false);
		MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(group), 0x8042C651, 4, false);
		MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(first), 0x80426DB9, 1, false);
		MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(second), 0x80426DB9, 3, false);
		if (!MuiGroupLayoutCore.Layout(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(group), 5, 7, 104, 20)) return 5;
		if (!MuiGroupLayoutCore.AskMinMax(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(group), APTR.FromPointer(minMax)) ||
			!MuiBalanceCore.AskMinMax(ref platform, APTR.FromPointer(minMax), true))
			return 6;
		if (!MuiRegisterCore.Initialize(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(group)) || !MuiSelectgroupCore.SetActive(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(group), -1)) return 7;
		MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(group), 0x80427C49, 200, false);
		MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(group), 0x80423038, 100, false);
		MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(group), 0x80429427, 1, false);
		if (!MuiVirtgroupCore.Layout(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(group), 0, 0, 80, 40)) return 8;
		MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(scroll), 0x80421261, group, false);
		if (!MuiScrollgroupCore.Layout(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(scroll), 0, 0, 80, 40)) return 9;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80428354);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, renderInfo);
		if (MuiLayoutDispatcher.Dispatch(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(group), APTR.FromPointer(packet)) == 0) return 10;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80426F3F);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 1);
		if (MuiLayoutDispatcher.Dispatch(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(group), APTR.FromPointer(packet)) == 0) return 11;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80422AD7);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, text);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 3);
		if (MuiLayoutDispatcher.Dispatch(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(group), APTR.FromPointer(packet)) != 0x00080018)
			return 12;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 13;
		return 42;
	}

	public static uint LayoutServiceRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint packet = 0x00036200;
		APTR.WriteUInt8(APTR.FromPointer(name), 0, (byte)'A');
		APTR.WriteUInt8(APTR.FromPointer(name), 1, (byte)'r');
		APTR.WriteUInt8(APTR.FromPointer(name), 2, (byte)'e');
		APTR.WriteUInt8(APTR.FromPointer(name), 3, (byte)'a');
		APTR.WriteUInt8(APTR.FromPointer(name), 4, (byte)'.');
		APTR.WriteUInt8(APTR.FromPointer(name), 5, (byte)'m');
		APTR.WriteUInt8(APTR.FromPointer(name), 6, (byte)'u');
		APTR.WriteUInt8(APTR.FromPointer(name), 7, (byte)'i');
		APTR.WriteUInt8(APTR.FromPointer(name), 8, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1)).Raw;
		if (cl == 0) return 2;
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(cl), APTR.Null).Raw;
		if (obj == 0) return 3;
		if (!MuiLayoutServiceCore.Layout(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(obj), 9, 11, 32, 14, 0)) return 4;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042845B);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 5);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 7);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 24);
		APTR.WriteUInt32(APTR.FromPointer(packet), 16, 12);
		APTR.WriteUInt32(APTR.FromPointer(packet), 20, 0x10);
		if (MuiLayoutServiceCore.Dispatch(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(obj), APTR.FromPointer(packet)) != 1) return 5;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(obj), 0x8042BEC6,
			out var left) || left != 5) return 6;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(obj), 0x8042B59C,
			out var width) || width != 24) return 7;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 8;
		return 42;
	}

	// MG09 public-layout scrollbar closure. This proves MUI_Layout reaches the
	// existing three-child Scrollbar geometry instead of the generic Area
	// fallback, while retaining a fully guest-resident, integer-only path.
	public static uint ScrollbarLayoutServiceRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var state = APTR.FromPointer(0x00036000);
		var name = APTR.FromPointer(0x00036100);
		APTR.WriteUInt8(name, 0, (byte)'S');
		APTR.WriteUInt8(name, 1, (byte)'c');
		APTR.WriteUInt8(name, 2, (byte)'r');
		APTR.WriteUInt8(name, 3, (byte)'o');
		APTR.WriteUInt8(name, 4, (byte)'l');
		APTR.WriteUInt8(name, 5, (byte)'l');
		APTR.WriteUInt8(name, 6, (byte)'b');
		APTR.WriteUInt8(name, 7, (byte)'a');
		APTR.WriteUInt8(name, 8, (byte)'r');
		APTR.WriteUInt8(name, 9, (byte)'.');
		APTR.WriteUInt8(name, 10, (byte)'m');
		APTR.WriteUInt8(name, 11, (byte)'u');
		APTR.WriteUInt8(name, 12, (byte)'i');
		APTR.WriteUInt8(name, 13, 0);
		if (!MuiHeadlessObjectCore.Initialize(ref platform, state)) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterClass(ref platform, state,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		var scrollbar = MuiCommonControlCore.CreateControl(ref platform, state,
			classRecord, APTR.Null);
		if (scrollbar.IsNull) return 2;
		if (!MuiLayoutServiceCore.Layout(ref platform, state, scrollbar, 10, 20,
			100, 80, 0)) return 3;
		var first = MuiFamilyCore.GetChild(ref platform, state, scrollbar, 0,
			APTR.Null);
		var prop = MuiFamilyCore.GetChild(ref platform, state, scrollbar, 1,
			APTR.Null);
		var second = MuiFamilyCore.GetChild(ref platform, state, scrollbar, 2,
			APTR.Null);
		if (first.IsNull || prop.IsNull || second.IsNull) return 4;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, first,
			0x8042BEC6, out var firstLeft) || firstLeft != 10 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform, state, first,
				0x8042509B, out var firstTop) || firstTop != 20 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform, state, first,
				0x8042B59C, out var firstWidth) || firstWidth != 100 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform, state, first,
				0x80423237, out var firstHeight) || firstHeight != 16) return 5;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, prop,
			0x8042BEC6, out var propLeft) || propLeft != 10 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform, state, prop,
				0x8042509B, out var propTop) || propTop != 36 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform, state, prop,
				0x8042B59C, out var propWidth) || propWidth != 100 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform, state, prop,
				0x80423237, out var propHeight) || propHeight != 48) return 6;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, second,
			0x8042509B, out var secondTop) || secondTop != 84 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform, state, second,
				0x80423237, out var secondHeight) || secondHeight != 16) return 7;
		return 42;
	}

	// MG09 requester/ASL service closure. The native capability intentionally
	// provides a deterministic requester handle and no host UI; the service
	// still proves guest-resident lease ownership, request routing, and balanced
	// release through the MorphOS-shaped API seam.
	public static uint AslServiceRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var state = APTR.FromPointer(0x00036000);
		var tags = APTR.FromPointer(0x00036100);
		if (!MuiAslServiceCore.Initialize(ref platform, state)) return 1;
		var requester = MuiAslServiceCore.AllocAslRequest(ref platform, state, 4,
			tags);
		if (requester.IsNull) return 2;
		if (MuiAslServiceCore.AslRequest(ref platform, state, requester, tags) != 1)
			return 3;
		if (!MuiAslServiceCore.FreeAslRequest(ref platform, state, requester))
			return 4;
		if (MuiAslServiceCore.FreeAslRequest(ref platform, state, requester))
			return 5;
		return 42;
	}

	// MG09 ASL record-layout seam. It round-trips the named service-state and
	// requester-lease structs without entering the capability-backed service.
	public static uint AslServiceRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var state = APTR.FromPointer(0x00036200);
		var lease = APTR.FromPointer(0x00036220);
		if (!MuiAslServiceRecordPacketCore.WriteState(ref platform, state,
			0x4D554941, APTR.FromPointer(0x36210), 1) ||
			MuiAslServiceRecordPacketCore.DispatchState(ref platform, state) !=
				(0x4D554941u ^ 0x00036210u ^ 1u)) return 1;
		if (!MuiAslServiceRecordPacketCore.WriteLease(ref platform, lease,
			APTR.FromPointer(0x36220), APTR.FromPointer(0x36240), 4,
			APTR.FromPointer(0x36260)) ||
			MuiAslServiceRecordPacketCore.DispatchLease(ref platform, lease) !=
				(0x00036220u ^ 0x00036240u ^ 4u ^ 0x00036260u)) return 2;
		return 42;
	}

	// MG09 bounded ASL TagItem control closure. It exercises MORE/SKIP/IGNORE
	// traversal and forwards the original guest list to the requester seam.
	public static uint AslTagServiceRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var state = APTR.FromPointer(0x00036000);
		var tags = APTR.FromPointer(0x00036100);
		var more = APTR.FromPointer(0x00036140);
		APTR.WriteUInt32(tags, 0, 0x80001234);
		APTR.WriteUInt32(tags, 4, 7);
		APTR.WriteUInt32(tags, 8, MuiAslTagListCore.TagIgnore);
		APTR.WriteUInt32(tags, 12, 0);
		APTR.WriteUInt32(tags, 16, MuiAslTagListCore.TagMore);
		APTR.WriteUInt32(tags, 20, more.Raw);
		APTR.WriteUInt32(more, 0, MuiAslTagListCore.TagSkip);
		APTR.WriteUInt32(more, 4, 1);
		APTR.WriteUInt32(more, 8, 0x80005678);
		APTR.WriteUInt32(more, 12, 99);
		APTR.WriteUInt32(more, 16, 0x80009ABC);
		APTR.WriteUInt32(more, 20, 42);
		APTR.WriteUInt32(more, 24, MuiAslTagListCore.TagDone);
		APTR.WriteUInt32(more, 28, 0);
		if (!MuiAslServiceCore.Initialize(ref platform, state)) return 1;
		uint value;
		if (!MuiAslTagListCore.TryGetData(ref platform, tags, 0x80009ABC,
			0, out value) || value != 42) return 2;
		var requester = MuiAslServiceCore.AllocAslRequest(ref platform, state, 0,
			tags);
		if (requester.IsNull) return 3;
		if (MuiAslServiceCore.AslRequest(ref platform, state, requester,
			tags) != 1) return 4;
		if (!MuiAslServiceCore.FreeAslRequest(ref platform, state, requester))
			return 5;
		return 42;
	}

	// MG09 synchronous requester service closure. The object form exercises the
	// retain/request/release lifetime around MUI_RequestObjectA.
	public static uint RequesterServiceRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var state = APTR.FromPointer(0x00036000);
		var obj = APTR.FromPointer(0x00036040);
		var gadgets = APTR.FromPointer(0x000360C0);
		APTR.WriteUInt8(gadgets, 0, (byte)'O');
		APTR.WriteUInt8(gadgets, 1, (byte)'k');
		APTR.WriteUInt8(gadgets, 2, 0);
		APTR.WriteUInt32(obj, 4, 2);
		if (!MuiRequesterServiceCore.Initialize(ref platform, state)) return 1;
		if (MuiRequesterServiceCore.Request(ref platform, state,
			APTR.FromPointer(0x36080), APTR.FromPointer(0x360A0), 0,
			APTR.Null, gadgets, APTR.Null, APTR.Null) != 1)
			return 2;
		if (MuiRequesterServiceCore.RequestObject(ref platform, state,
			APTR.FromPointer(0x36080), APTR.FromPointer(0x360A0), 0,
			APTR.Null, gadgets, obj, APTR.Null, APTR.Null) != 1)
			return 3;
		if (APTR.ReadUInt32(obj, 4) != 1) return 4;
		return 42;
	}

	// MG09 requester state-layout seam. The synchronous requester gateway keeps
	// only its typed magic and generation state in guest memory.
	public static uint RequesterServiceRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var state = APTR.FromPointer(0x000362A0);
		if (!MuiRequesterServiceRecordPacketCore.WriteState(ref platform, state,
			0x4D554952, 1) ||
			MuiRequesterServiceRecordPacketCore.DispatchState(ref platform, state) !=
				(0x4D554952u ^ 1u)) return 1;
		return 42;
	}

	// MG09 caller-owned requester payload closure. The text arguments are
	// validated in guest memory, gadget alternatives are measured, and the
	// ULONG vector is formatted in guest memory before the synchronous call.
	public static uint RequesterPayloadServiceRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var state = APTR.FromPointer(0x00036000);
		var title = APTR.FromPointer(0x00036180);
		var gadgets = APTR.FromPointer(0x000361A0);
		var format = APTR.FromPointer(0x000361C0);
		var parameters = APTR.FromPointer(0x000361E0);
		var obj = APTR.FromPointer(0x00036200);
		APTR.WriteUInt8(title, 0, (byte)'M');
		APTR.WriteUInt8(title, 1, (byte)'o');
		APTR.WriteUInt8(title, 2, (byte)'r');
		APTR.WriteUInt8(title, 3, (byte)'p');
		APTR.WriteUInt8(title, 4, (byte)'h');
		APTR.WriteUInt8(title, 5, (byte)'O');
		APTR.WriteUInt8(title, 6, (byte)'S');
		APTR.WriteUInt8(title, 7, (byte)' ');
		APTR.WriteUInt8(title, 8, (byte)'M');
		APTR.WriteUInt8(title, 9, (byte)'U');
		APTR.WriteUInt8(title, 10, (byte)'I');
		APTR.WriteUInt8(title, 11, 0);
		APTR.WriteUInt8(gadgets, 0, (byte)'_');
		APTR.WriteUInt8(gadgets, 1, (byte)'O');
		APTR.WriteUInt8(gadgets, 2, (byte)'k');
		APTR.WriteUInt8(gadgets, 3, (byte)'|');
		APTR.WriteUInt8(gadgets, 4, (byte)'*');
		APTR.WriteUInt8(gadgets, 5, (byte)'_');
		APTR.WriteUInt8(gadgets, 6, (byte)'C');
		APTR.WriteUInt8(gadgets, 7, (byte)'a');
		APTR.WriteUInt8(gadgets, 8, (byte)'n');
		APTR.WriteUInt8(gadgets, 9, (byte)'c');
		APTR.WriteUInt8(gadgets, 10, (byte)'e');
		APTR.WriteUInt8(gadgets, 11, (byte)'l');
		APTR.WriteUInt8(gadgets, 12, (byte)'|');
		APTR.WriteUInt8(gadgets, 13, (byte)'H');
		APTR.WriteUInt8(gadgets, 14, (byte)'e');
		APTR.WriteUInt8(gadgets, 15, (byte)'l');
		APTR.WriteUInt8(gadgets, 16, (byte)'p');
		APTR.WriteUInt8(gadgets, 17, 0);
		APTR.WriteUInt8(format, 0, (byte)'C');
		APTR.WriteUInt8(format, 1, (byte)'o');
		APTR.WriteUInt8(format, 2, (byte)'n');
		APTR.WriteUInt8(format, 3, (byte)'t');
		APTR.WriteUInt8(format, 4, (byte)'i');
		APTR.WriteUInt8(format, 5, (byte)'n');
		APTR.WriteUInt8(format, 6, (byte)'u');
		APTR.WriteUInt8(format, 7, (byte)'e');
		APTR.WriteUInt8(format, 8, (byte)':');
		APTR.WriteUInt8(format, 9, (byte)' ');
		APTR.WriteUInt8(format, 10, (byte)'%');
		APTR.WriteUInt8(format, 11, (byte)'d');
		APTR.WriteUInt8(format, 12, 0);
		APTR.WriteUInt32(parameters, 0, 7);
		APTR.WriteUInt32(obj, 4, 2);
		uint count;
		if (!MuiRequesterPayloadCore.TryGetGadgetCount(ref platform, gadgets,
			out count) || count != 3) return 1;
		if (!MuiRequesterServiceCore.Initialize(ref platform, state)) return 2;
		if (MuiRequesterServiceCore.Request(ref platform, state,
			APTR.FromPointer(0x36220), APTR.FromPointer(0x36240), 0,
			title, gadgets, format, parameters) != 1) return 3;
		if (MuiRequesterServiceCore.RequestObject(ref platform, state,
			APTR.FromPointer(0x36220), APTR.FromPointer(0x36240), 0,
			title, gadgets, obj, format, parameters) != 1) return 4;
		if (APTR.ReadUInt32(obj, 4) != 1) return 5;
		return 42;
	}

	// MG09 public MUI_Redraw closure. The guest object is resolved through the
	// registry, draw intent bits are checked, and the native redraw seam is
	// entered without a host renderer.
	public static uint RedrawServiceRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		APTR.WriteUInt8(APTR.FromPointer(name), 0, (byte)'A');
		APTR.WriteUInt8(APTR.FromPointer(name), 1, (byte)'r');
		APTR.WriteUInt8(APTR.FromPointer(name), 2, (byte)'e');
		APTR.WriteUInt8(APTR.FromPointer(name), 3, (byte)'a');
		APTR.WriteUInt8(APTR.FromPointer(name), 4, (byte)'.');
		APTR.WriteUInt8(APTR.FromPointer(name), 5, (byte)'m');
		APTR.WriteUInt8(APTR.FromPointer(name), 6, (byte)'u');
		APTR.WriteUInt8(APTR.FromPointer(name), 7, (byte)'i');
		APTR.WriteUInt8(APTR.FromPointer(name), 8, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1)).Raw;
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(cl), APTR.Null);
		if (cl == 0 || obj.IsNull) return 2;
		if (!MuiRedrawServiceCore.Redraw(ref platform,
			APTR.FromPointer(state), obj, MuiRedrawServiceCore.DrawObject)) return 3;
		if (!MuiRedrawServiceCore.Redraw(ref platform,
			APTR.FromPointer(state), obj, MuiRedrawServiceCore.DrawUpdate)) return 4;
		if (MuiRedrawServiceCore.Redraw(ref platform,
			APTR.FromPointer(state), obj, 4)) return 5;
		return 42;
	}

	// MG09 public MUI_NewObjectA closure. It resolves a registered builtin class,
	// validates a guest TagItem list, applies one attribute, and disposes the
	// resulting guest object through the existing lifecycle core.
	public static uint NewObjectServiceRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint tags = 0x00036140;
		const uint attribute = 0x80420001;
		APTR.WriteUInt8(APTR.FromPointer(name), 0, (byte)'T');
		APTR.WriteUInt8(APTR.FromPointer(name), 1, (byte)'e');
		APTR.WriteUInt8(APTR.FromPointer(name), 2, (byte)'x');
		APTR.WriteUInt8(APTR.FromPointer(name), 3, (byte)'t');
		APTR.WriteUInt8(APTR.FromPointer(name), 4, (byte)'.');
		APTR.WriteUInt8(APTR.FromPointer(name), 5, (byte)'m');
		APTR.WriteUInt8(APTR.FromPointer(name), 6, (byte)'u');
		APTR.WriteUInt8(APTR.FromPointer(name), 7, (byte)'i');
		APTR.WriteUInt8(APTR.FromPointer(name), 8, 0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, attribute);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, 77);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, MuiAslTagListCore.TagDone);
		APTR.WriteUInt32(APTR.FromPointer(tags), 12, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (cl.IsNull) return 2;
		var obj = MuiObjectFactoryServiceCore.NewObjectA(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.FromPointer(tags));
		if (obj.IsNull) return 3;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), obj, attribute, out var value) || value != 77)
			return 4;
		if (!MuiHeadlessObjectCore.DisposeObject(ref platform,
			APTR.FromPointer(state), obj)) return 5;
		return 42;
	}

	// MG09 public-factory/common-control closure. The factory applies raw tags,
	// then runs Numeric construction normalization before returning the object:
	// range clamping and owned format storage are both guest-resident.
	public static uint CommonFactoryServiceRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var privateRoot = APTR.FromPointer(0x00035F00);
		var state = APTR.FromPointer(0x00036000);
		var name = APTR.FromPointer(0x00036100);
		var format = APTR.FromPointer(0x00036200);
		var tags = APTR.FromPointer(0x00036300);
		WriteClassId(name, 'N', 'u', 'm', 'e', 'r', 'i', 'c', (char)0,
			(char)0);
		APTR.WriteUInt8(format, 0, (byte)'v');
		APTR.WriteUInt8(format, 1, (byte)'a');
		APTR.WriteUInt8(format, 2, (byte)'l');
		APTR.WriteUInt8(format, 3, (byte)'u');
		APTR.WriteUInt8(format, 4, (byte)'e');
		APTR.WriteUInt8(format, 5, (byte)'=');
		APTR.WriteUInt8(format, 6, (byte)'%');
		APTR.WriteUInt8(format, 7, (byte)'l');
		APTR.WriteUInt8(format, 8, (byte)'d');
		APTR.WriteUInt8(format, 9, 0);
		APTR.WriteUInt32(tags, 0, 0x8042E404); // MUIA_Numeric_Min
		APTR.WriteUInt32(tags, 4, 10);
		APTR.WriteUInt32(tags, 8, 0x8042D78A); // MUIA_Numeric_Max
		APTR.WriteUInt32(tags, 12, 20);
		APTR.WriteUInt32(tags, 16, 0x8042AE3A); // MUIA_Numeric_Value
		APTR.WriteUInt32(tags, 20, 99);
		APTR.WriteUInt32(tags, 24, 0x804263E9); // MUIA_Numeric_Format
		APTR.WriteUInt32(tags, 28, format.Raw);
		APTR.WriteUInt32(tags, 32, MuiAslTagListCore.TagDone);
		APTR.WriteUInt32(tags, 36, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform, privateRoot, state)) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			state, name, APTR.Null, 0, APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var obj = MuiObjectFactoryServiceCore.NewObjectA(ref platform, state, name,
			tags);
		if (obj.IsNull || MuiCommonControlCore.Classify(ref platform, state, obj) !=
			MuiControlClass.Numeric) return 3;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			0x8042AE3A, out var value) || value != 20) return 4;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			0x804263E9, out var copiedFormat) || copiedFormat == format.Raw ||
			APTR.ReadUInt8(APTR.FromPointer(copiedFormat), 0) != (byte)'v') return 5;
		if (!MuiObjectDisposalServiceCore.DisposeObject(ref platform, state, obj))
			return 6;
		if (!MuiMasterLifecycleCore.Dispose(ref platform, privateRoot)) return 7;
		return 42;
	}

	// MG09 class-service/common-control closure. The class-service factory must
	// run the same Numeric normalization as MUI_NewObjectA while retaining and
	// releasing its guest-resident class lease through disposal.
	public static uint ClassServiceCommonFactoryRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var privateRoot = APTR.FromPointer(0x00035F00);
		var state = APTR.FromPointer(0x00036000);
		var serviceState = APTR.FromPointer(0x00036040);
		var name = APTR.FromPointer(0x00036100);
		var format = APTR.FromPointer(0x00036200);
		var tags = APTR.FromPointer(0x00036300);
		WriteClassId(name, 'N', 'u', 'm', 'e', 'r', 'i', 'c', (char)0,
			(char)0);
		APTR.WriteUInt8(format, 0, (byte)'v');
		APTR.WriteUInt8(format, 1, (byte)'a');
		APTR.WriteUInt8(format, 2, (byte)'l');
		APTR.WriteUInt8(format, 3, (byte)'u');
		APTR.WriteUInt8(format, 4, (byte)'e');
		APTR.WriteUInt8(format, 5, (byte)'=');
		APTR.WriteUInt8(format, 6, (byte)'%');
		APTR.WriteUInt8(format, 7, (byte)'l');
		APTR.WriteUInt8(format, 8, (byte)'d');
		APTR.WriteUInt8(format, 9, 0);
		APTR.WriteUInt32(tags, 0, 0x8042E404); // MUIA_Numeric_Min
		APTR.WriteUInt32(tags, 4, 10);
		APTR.WriteUInt32(tags, 8, 0x8042D78A); // MUIA_Numeric_Max
		APTR.WriteUInt32(tags, 12, 20);
		APTR.WriteUInt32(tags, 16, 0x8042AE3A); // MUIA_Numeric_Value
		APTR.WriteUInt32(tags, 20, 99);
		APTR.WriteUInt32(tags, 24, 0x804263E9); // MUIA_Numeric_Format
		APTR.WriteUInt32(tags, 28, format.Raw);
		APTR.WriteUInt32(tags, 32, MuiAslTagListCore.TagDone);
		APTR.WriteUInt32(tags, 36, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform, privateRoot, state)) return 1;
		if (!MuiClassServiceCore.Initialize(ref platform, serviceState, state)) return 2;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			state, name, APTR.Null, 0, APTR.FromPointer(1));
		if (classRecord.IsNull) return 3;
		var obj = MuiObjectFactoryServiceCore.NewObjectAWithClassService(ref platform,
			serviceState, state, name, tags);
		if (obj.IsNull || MuiCommonControlCore.Classify(ref platform, state, obj) !=
			MuiControlClass.Numeric) return 4;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			0x8042AE3A, out var value) || value != 20) return 5;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			0x804263E9, out var copiedFormat) || copiedFormat == format.Raw ||
			APTR.ReadUInt8(APTR.FromPointer(copiedFormat), 0) != (byte)'v') return 6;
		var classPointer = MuiHeadlessObjectCore.ClassPointer(ref platform,
			classRecord);
		if (MuiClassServiceCore.ObjectLeaseCount(ref platform, serviceState,
			classPointer) != 1) return 7;
		if (!MuiObjectDisposalServiceCore.DisposeObject(ref platform, serviceState,
			state, obj)) return 8;
		if (MuiClassServiceCore.ObjectLeaseCount(ref platform, serviceState,
			classPointer) != 0) return 9;
		if (!MuiMasterLifecycleCore.Dispose(ref platform, privateRoot)) return 10;
		return 42;
	}

	// MG09 direct/class-service menu object-factory closure. Both public object
	// construction paths must attach the Menu specialist sidecar before the
	// object is returned, and both disposal paths must release it before the
	// class lease is dropped.
	public static uint NewObjectMenuServiceRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint serviceState = 0x00036040;
		const uint menuName = 0x00036100;
		var st = APTR.FromPointer(state);
		var menuId = APTR.FromPointer(menuName);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), st)) return 1;
		WriteMenuClassName(menuId);
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			st, menuId, APTR.Null, 0, APTR.FromPointer(10));
		if (classRecord.IsNull) return 2;

		var direct = MuiObjectFactoryServiceCore.NewObjectA(ref platform, st,
			menuId, APTR.Null);
		if (direct.IsNull || !MuiMenuSpecialistCore.Valid(ref platform, st,
			direct)) return 3;
		if (!MuiObjectDisposalServiceCore.DisposeObject(ref platform, st,
			direct) || MuiMenuSpecialistCore.Valid(ref platform, st, direct))
			return 4;

		if (!MuiClassServiceCore.Initialize(ref platform, APTR.FromPointer(serviceState),
			st)) return 5;
		var leased = MuiObjectFactoryServiceCore.NewObjectAWithClassService(
			ref platform, APTR.FromPointer(serviceState), st, menuId, APTR.Null);
		if (leased.IsNull || !MuiMenuSpecialistCore.Valid(ref platform, st,
			leased)) return 6;
		var menuPointer = MuiHeadlessObjectCore.ClassPointer(ref platform,
			classRecord);
		if (MuiClassServiceCore.ReferenceCount(ref platform,
			APTR.FromPointer(serviceState), menuPointer) != 1) return 7;
		if (!MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			APTR.FromPointer(serviceState), st, leased) ||
			MuiMenuSpecialistCore.Valid(ref platform, st, leased)) return 8;
		if (MuiClassServiceCore.ReferenceCount(ref platform,
			APTR.FromPointer(serviceState), menuPointer) != 0) return 9;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 10;
		return 42;
	}

	// MG09 Misc object-factory closure. A factory-created Keyadjust object gets
	// a guest-resident specialist sidecar, remains usable through that sidecar,
	// and releases the sidecar before its headless object record is disposed.
	public static uint MiscObjectFactoryRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		var st = APTR.FromPointer(state);
		var name = APTR.FromPointer(className);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), st)) return 1;
		WriteClassId(name, 'K', 'e', 'y', 'a', 'd', 'j', 'u', 's', 't');
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			st, name, APTR.Null, 0, APTR.FromPointer(20));
		if (classRecord.IsNull) return 2;
		var obj = MuiObjectFactoryServiceCore.NewObjectA(ref platform, st, name,
			APTR.Null);
		if (obj.IsNull || !MuiMiscSpecialistCore.ValidObject(ref platform, st, obj))
			return 3;
		var instance = MuiMiscSpecialistCore.ObjectInstance(ref platform, st, obj);
		if (instance.IsNull || MuiMiscSpecialistCore.Classify(ref platform,
			instance) != MuiMiscSpecialistClass.Keyadjust) return 4;
		if (!MuiMiscSpecialistCore.SetAttribute(ref platform, instance,
			MuiMiscAttributes.Keyadjust_AllowMouseEvents, 1, false, true, out _))
			return 5;
		if (!MuiObjectDisposalServiceCore.DisposeObject(ref platform, st, obj))
			return 6;
		if (MuiMiscSpecialistCore.ValidObject(ref platform, st, obj) ||
			MuiMiscSpecialistCore.Valid(ref platform, instance)) return 7;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 8;
		return 42;
	}

	// MG09 object-aware Misc dispatch closure. A Title object is addressed
	// through the sidecar dispatcher for its page topology methods and is then
	// disposed through the object lifecycle rather than only clearing the
	// specialist block.
	public static uint MiscObjectDispatcherRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		var st = APTR.FromPointer(state);
		var name = APTR.FromPointer(className);
		var msg = APTR.FromPointer(packet);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), st)) return 1;
		WriteClassId(name, 'T', 'i', 't', 'l', 'e', (char)0, (char)0, (char)0,
			(char)0);
		if (MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, st, name,
			APTR.Null, 0, APTR.FromPointer(20)).IsNull) return 2;
		var classRecord = MuiHeadlessObjectCore.FindClassByName(ref platform, st,
			name);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, st,
			classRecord, APTR.Null);
		if (obj.IsNull || MuiMiscSpecialistCore.AttachByObject(ref platform, st,
			obj).IsNull) return 3;
		APTR.WriteUInt32(msg, 0, 0x8042549A);
		APTR.WriteUInt32(msg, 4, MuiMiscAttributes.Title_Closable);
		APTR.WriteUInt32(msg, 8, 1);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 1)
			return 4;
		APTR.WriteUInt32(msg, 0, MuiMiscAttributes.Title_New);
		var handle = MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg);
		if (handle == 0) return 5;
		APTR.WriteUInt32(msg, 0, MuiMiscAttributes.Title_FindPage);
		APTR.WriteUInt32(msg, 4, handle);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 0)
			return 6;
		APTR.WriteUInt32(msg, 0, MuiMiscAttributes.Title_Close);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 1)
			return 7;
		APTR.WriteUInt32(msg, 0, 0x00000102);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 1 ||
			MuiMiscSpecialistCore.ValidObject(ref platform, st, obj)) return 8;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 9;
		return 42;
	}

	// MG09 object-aware Misc lifecycle closure. Public factory sidecars must
	// receive the same exact packed { MethodID } Setup/Cleanup ABI as standalone
	// specialist instances before the object-aware OM_DISPOSE path is used.
	public static uint MiscObjectLifecycleRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		var st = APTR.FromPointer(state);
		var name = APTR.FromPointer(className);
		var msg = APTR.FromPointer(packet);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), st)) return 1;
		WriteClassId(name, 'K', 'e', 'y', 'a', 'd', 'j', 'u', 's', 't');
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			st, name, APTR.Null, 0, APTR.FromPointer(20));
		if (classRecord.IsNull) return 2;
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, st,
			classRecord, APTR.Null);
		if (obj.IsNull || MuiMiscSpecialistCore.AttachByObject(ref platform, st,
			obj).IsNull) return 3;
		var instance = MuiMiscSpecialistCore.ObjectInstance(ref platform, st, obj);
		if (instance.IsNull) return 4;

		APTR.WriteUInt32(msg, 0, MuiMiscAttributes.Setup);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 1 ||
			!MuiMiscSpecialistCore.IsSetupActive(ref platform, instance)) return 5;
		APTR.WriteUInt32(msg, 0, MuiMiscAttributes.Cleanup);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 1 ||
			MuiMiscSpecialistCore.IsSetupActive(ref platform, instance)) return 6;
		APTR.WriteUInt32(msg, 0, 0x00000102);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 1 ||
			MuiMiscSpecialistCore.ValidObject(ref platform, st, obj)) return 7;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 8;
		return 42;
	}

	// MG09 Panel_Run object-dispatch closure. The fixed { method, app, win }
	// frame rejects either null pointer and records a valid pair in the
	// guest-resident Panel sidecar before object disposal.
	public static uint MiscPanelDispatcherRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		const uint application = 0x00036300;
		const uint window = 0x00036340;
		var st = APTR.FromPointer(state);
		var name = APTR.FromPointer(className);
		var msg = APTR.FromPointer(packet);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), st)) return 1;
		WriteClassId(name, 'P', 'a', 'n', 'e', 'l', (char)0, (char)0, (char)0,
			(char)0);
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			st, name, APTR.Null, 0, APTR.FromPointer(20));
		if (classRecord.IsNull) return 2;
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, st,
			classRecord, APTR.Null);
		if (obj.IsNull || MuiMiscSpecialistCore.AttachByObject(ref platform, st,
			obj).IsNull) return 3;
		APTR.WriteUInt32(msg, 0, MuiMiscAttributes.Panel_Run);
		APTR.WriteUInt32(msg, 4, 0);
		APTR.WriteUInt32(msg, 8, window);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 0)
			return 4;
		APTR.WriteUInt32(msg, 4, application);
		APTR.WriteUInt32(msg, 8, window);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 1)
			return 5;
		APTR.WriteUInt32(msg, 0, 0x00000102);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 1)
			return 6;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 7;
		return 42;
	}

	// MG09 Filepanel_AddRow object-dispatch closure. The fixed { method, label,
	// contents } frame rejects a null child, adopts a valid pair into the
	// guest-resident bounded row block, and disposes through the object seam.
	public static uint MiscFilepanelDispatcherRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		const uint label = 0x00036300;
		const uint contents = 0x00036340;
		var st = APTR.FromPointer(state);
		var name = APTR.FromPointer(className);
		var msg = APTR.FromPointer(packet);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), st)) return 1;
		WriteClassId(name, 'F', 'i', 'l', 'e', 'p', 'a', 'n', 'e', 'l');
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			st, name, APTR.Null, 0, APTR.FromPointer(20));
		if (classRecord.IsNull) return 2;
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, st,
			classRecord, APTR.Null);
		if (obj.IsNull || MuiMiscSpecialistCore.AttachByObject(ref platform, st,
			obj).IsNull) return 3;
		APTR.WriteUInt32(msg, 0, MuiMiscAttributes.Filepanel_AddRow);
		APTR.WriteUInt32(msg, 4, 0);
		APTR.WriteUInt32(msg, 8, contents);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 0)
			return 4;
		APTR.WriteUInt32(msg, 4, label);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 1)
			return 5;
		var instance = MuiMiscSpecialistCore.ObjectInstance(ref platform, st, obj);
		if (MuiMiscSpecialistCore.FilepanelRowCount(ref platform, instance) != 1)
			return 6;
		APTR.WriteUInt32(msg, 0, 0x00000102);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 1)
			return 7;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 8;
		return 42;
	}

	// MG09 Mccprefs_RegisterGadget object-dispatch closure. The fixed seven-word
	// frame rejects a null gadget, registers a bounded caller-owned record,
	// updates it in place, unregisters with id zero, and disposes the object.
	public static uint MiscMccprefsDispatcherRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		const uint title = 0x00036300;
		const uint gadget = 0x00036340;
		var st = APTR.FromPointer(state);
		var name = APTR.FromPointer(className);
		var msg = APTR.FromPointer(packet);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), st)) return 1;
		WriteClassId(name, 'M', 'c', 'c', 'p', 'r', 'e', 'f', 's', (char)0);
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			st, name, APTR.Null, 0, APTR.FromPointer(20));
		if (classRecord.IsNull) return 2;
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, st,
			classRecord, APTR.Null);
		if (obj.IsNull || MuiMiscSpecialistCore.AttachByObject(ref platform, st,
			obj).IsNull) return 3;
		APTR.WriteUInt32(msg, 0, MuiMiscAttributes.Mccprefs_RegisterGadget);
		APTR.WriteUInt32(msg, 4, 0);
		APTR.WriteUInt32(msg, 8, 10);
		APTR.WriteUInt32(msg, 12, 0);
		APTR.WriteUInt32(msg, 16, title);
		APTR.WriteUInt32(msg, 20, 0);
		APTR.WriteUInt32(msg, 24, 0);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 0)
			return 4;
		APTR.WriteUInt32(msg, 4, gadget);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 1)
			return 5;
		var instance = MuiMiscSpecialistCore.ObjectInstance(ref platform, st, obj);
		if (MuiMiscSpecialistCore.MccprefsRegistryCount(ref platform, instance) != 1)
			return 6;
		APTR.WriteUInt32(msg, 12, 99);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 1)
			return 7;
		APTR.WriteUInt32(msg, 8, 0);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 1)
			return 8;
		if (MuiMiscSpecialistCore.MccprefsRegistryCount(ref platform, instance) != 0)
			return 9;
		APTR.WriteUInt32(msg, 0, 0x00000102);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 1)
			return 10;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 11;
		return 42;
	}

	// MG09 Mccprefs_ConfigToGadgets object-dispatch closure. The two-word frame
	// records the config pointer and reports the honest empty-registry failure;
	// after one bounded gadget record exists it reports successful distribution.
	public static uint MiscMccprefsConfigDispatcherRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		const uint config = 0x00036300;
		const uint gadget = 0x00036340;
		var st = APTR.FromPointer(state);
		var name = APTR.FromPointer(className);
		var msg = APTR.FromPointer(packet);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), st)) return 1;
		WriteClassId(name, 'M', 'c', 'c', 'p', 'r', 'e', 'f', 's', (char)0);
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			st, name, APTR.Null, 0, APTR.FromPointer(20));
		if (classRecord.IsNull) return 2;
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, st,
			classRecord, APTR.Null);
		if (obj.IsNull || MuiMiscSpecialistCore.AttachByObject(ref platform, st,
			obj).IsNull) return 3;
		APTR.WriteUInt32(msg, 0, MuiMiscAttributes.Mccprefs_ConfigToGadgets);
		APTR.WriteUInt32(msg, 4, config);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 0)
			return 4;
		var instance = MuiMiscSpecialistCore.ObjectInstance(ref platform, st, obj);
		if (APTR.ReadUInt32(instance, MuiMiscSpecialistLayout.RegistryConfig) != config)
			return 5;
		if (!MuiMiscSpecialistCore.MccprefsRegisterGadget(ref platform, instance,
			gadget, 10, 0, APTR.Null, 0, APTR.Null)) return 6;
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 1)
			return 7;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 8;
		return 42;
	}

	// MG09 Mccprefs_GadgetsToConfig object-dispatch closure. The three-word frame
	// records configdata and originator, reports the honest empty-registry
	// failure, then reports success once a bounded gadget record exists.
	public static uint MiscMccprefsGadgetsConfigDispatcherRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		const uint config = 0x00036300;
		const uint originator = 0x00036340;
		const uint gadget = 0x00036380;
		var st = APTR.FromPointer(state);
		var name = APTR.FromPointer(className);
		var msg = APTR.FromPointer(packet);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), st)) return 1;
		WriteClassId(name, 'M', 'c', 'c', 'p', 'r', 'e', 'f', 's', (char)0);
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			st, name, APTR.Null, 0, APTR.FromPointer(20));
		if (classRecord.IsNull) return 2;
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, st,
			classRecord, APTR.Null);
		if (obj.IsNull || MuiMiscSpecialistCore.AttachByObject(ref platform, st,
			obj).IsNull) return 3;
		APTR.WriteUInt32(msg, 0, MuiMiscAttributes.Mccprefs_GadgetsToConfig);
		APTR.WriteUInt32(msg, 4, config);
		APTR.WriteUInt32(msg, 8, originator);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 0)
			return 4;
		var instance = MuiMiscSpecialistCore.ObjectInstance(ref platform, st, obj);
		if (!MuiMiscSpecialistCore.MccprefsRegisterGadget(ref platform, instance,
			gadget, 10, 0, APTR.Null, 0, APTR.Null)) return 5;
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 1)
			return 6;
		APTR.WriteUInt32(msg, 0, 0x00000102);
		if (MuiMiscObjectDispatcher.Dispatch(ref platform, st, obj, msg) != 1)
			return 7;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 8;
		return 42;
	}

	// MG09 menu dispatch closure. A factory-created Menu object is routed through
	// the public headless dispatcher, where the specialist claims Set/Get and
	// OM_DISPOSE without exposing a second dispatch API to callers.
	public static uint MenuDispatcherRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint menuName = 0x00036100;
		const uint packet = 0x00036200;
		const uint storage = 0x00036240;
		var st = APTR.FromPointer(state);
		var menuId = APTR.FromPointer(menuName);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), st)) return 1;
		WriteMenuClassName(menuId);
		var menuClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, st,
			menuId, APTR.Null, 0, APTR.FromPointer(10));
		if (menuClass.IsNull) return 2;
		var menu = MuiHeadlessObjectCore.CreateObjectA(ref platform, st, menuClass,
			APTR.Null);
		if (menu.IsNull || MuiMenuSpecialistCore.Attach(ref platform, st, menu,
			MuiMenuSpecialistClass.Menu).IsNull) return 3;

		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042549A);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0x8042ED48);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 0);
		if (MuiMenuSpecialistDispatcher.Dispatch(ref platform, st, menu,
			APTR.FromPointer(packet)) != 1) return 4;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x00000104);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0x8042ED48);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, storage);
		if (MuiMenuSpecialistDispatcher.Dispatch(ref platform, st, menu,
			APTR.FromPointer(packet)) != 1 ||
			APTR.ReadUInt32(APTR.FromPointer(storage), 0) != 0) return 5;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x00000102);
		if (MuiMenuSpecialistDispatcher.Dispatch(ref platform, st, menu,
			APTR.FromPointer(packet)) != 1 || MuiMenuSpecialistCore.Valid(ref platform,
			st, menu)) return 6;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 7;
		return 42;
	}

	// MG09 Process/Slave public service dispatch closure. A factory-created
	// Process object is routed through the service-capable headless seam, which
	// claims OM_GET, MUIM_Process_Launch and OM_DISPOSE while the ordinary
	// headless path remains available to layout callers.
	public static uint ProcessDispatcherRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint processName = 0x00036100;
		const uint tags = 0x00036180;
		const uint packet = 0x00036200;
		const uint storage = 0x00036240;
		var st = APTR.FromPointer(state);
		var processId = APTR.FromPointer(processName);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), st)) return 1;
		WriteProcessClassName(processId);
		var processClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			st, processId, APTR.Null, 0, APTR.FromPointer(20));
		if (processClass.IsNull) return 2;
		// Initial Process tags are applied before sidecar attachment and must be
		// imported without being overwritten by specialist defaults.
		APTR.WriteUInt32(APTR.FromPointer(tags), 0,
			MuiProcessAttributes.Process_Priority);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, 5);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8,
			MuiProcessAttributes.Process_StackSize);
		APTR.WriteUInt32(APTR.FromPointer(tags), 12, 16384);
		APTR.WriteUInt32(APTR.FromPointer(tags), 16,
			MuiProcessAttributes.Process_AutoLaunch);
		APTR.WriteUInt32(APTR.FromPointer(tags), 20, 1);
		APTR.WriteUInt32(APTR.FromPointer(tags), 24, 0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 28, 0);
		var process = MuiHeadlessObjectCore.CreateObjectA(ref platform, st,
			processClass, APTR.FromPointer(tags));
		if (process.IsNull || MuiProcessSpecialistCore.AttachByObject(ref platform,
			st, process).IsNull) return 3;

		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x00000104);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4,
			MuiProcessAttributes.Process_StackSize);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, storage);
		if (MuiProcessSpecialistDispatcher.Dispatch(ref platform, st, process,
			APTR.FromPointer(packet)) != 1 ||
			APTR.ReadUInt32(APTR.FromPointer(storage), 0) != 16384) return 4;

		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiProcessAttributes.Process_Launch);
		if (MuiProcessSpecialistDispatcher.Dispatch(ref platform, st, process,
			APTR.FromPointer(packet)) != 1 ||
			MuiProcessSpecialistCore.ProcessStateOf(ref platform, st, process) !=
			MuiProcessState.Running) return 5;

		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x00000102);
		if (MuiProcessSpecialistDispatcher.Dispatch(ref platform, st, process,
			APTR.FromPointer(packet)) != 1 ||
			MuiProcessSpecialistCore.Valid(ref platform, st, process)) return 6;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 7;
		return 42;
	}

	// Shared MG09 specialist seam. Popstring, Coloradjust and Dtpic use
	// standalone guest-resident instance records rather than headless-object
	// sidecars; this closure proves one packet entry point routes each family
	// and balances disposal.
	public static uint ServiceDispatcherRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var state = APTR.FromPointer(0x00036000);
		var classId = APTR.FromPointer(0x00036100);
		var packet = APTR.FromPointer(0x00036200);
		var storage = APTR.FromPointer(0x00036240);
		var pop = APTR.FromPointer(0x00036300);
		var color = APTR.FromPointer(0x00036400);
		if (!MuiDrawingServiceCore.Initialize(ref platform, state)) return 1;

		WriteClassId(classId, 'P', 'o', 'p', 's', 't', 'r', 'i', 'n', 'g');
		var stringChild = platform.NewObject(APTR.FromPointer(0x9000), APTR.Null);
		var buttonChild = platform.NewObject(APTR.FromPointer(0x9000), APTR.Null);
		if (MuiPopSpecialistCore.CreateByName(ref platform, pop, classId,
			stringChild, buttonChild) != MuiPopSpecialistClass.Popstring) return 2;
		APTR.WriteUInt32(packet, 0, 0x00000104);
		APTR.WriteUInt32(packet, 4, MuiPopAttributes.Popstring_Toggle);
		APTR.WriteUInt32(packet, 8, storage.Raw);
		if (MuiPopSpecialistDispatcher.Dispatch(ref platform, pop,
			packet) != 1) return 3;
		APTR.WriteUInt32(packet, 0, 0x00000102);
		if (MuiPopSpecialistDispatcher.Dispatch(ref platform, pop,
			packet) != 1) return 4;

		WriteColoradjustClassName(classId);
		if (MuiColorSpecialistCore.CreateByName(ref platform, color, classId) !=
			MuiColorSpecialistClass.Coloradjust) return 5;
		APTR.WriteUInt32(packet, 0, 0x00000104);
		APTR.WriteUInt32(packet, 4, MuiColorAttributes.ColoradjustShowAlpha);
		APTR.WriteUInt32(packet, 8, storage.Raw);
		if (MuiColorSpecialistDispatcher.Dispatch(ref platform, color,
			packet) != 1) return 6;
		APTR.WriteUInt32(packet, 0, 0x00000102);
		if (MuiColorSpecialistDispatcher.Dispatch(ref platform, color,
			packet) != 1) return 7;

		return 42;
	}

	// MG09 requester-format execution closure. It exercises signed width,
	// string and literal-percent conversion without a managed formatter or
	// host allocation, then releases the bounded temporary guest buffer.
	public static uint RequesterFormatServiceRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var format = APTR.FromPointer(0x00036080);
		var parameters = APTR.FromPointer(0x000360A0);
		var text = APTR.FromPointer(0x000360C0);
		APTR.WriteUInt8(format, 0, (byte)'V');
		APTR.WriteUInt8(format, 1, (byte)'=');
		APTR.WriteUInt8(format, 2, (byte)'%');
		APTR.WriteUInt8(format, 3, (byte)'+');
		APTR.WriteUInt8(format, 4, (byte)'0');
		APTR.WriteUInt8(format, 5, (byte)'6');
		APTR.WriteUInt8(format, 6, (byte)'l');
		APTR.WriteUInt8(format, 7, (byte)'d');
		APTR.WriteUInt8(format, 8, (byte)' ');
		APTR.WriteUInt8(format, 9, (byte)'%');
		APTR.WriteUInt8(format, 10, (byte)'s');
		APTR.WriteUInt8(format, 11, (byte)' ');
		APTR.WriteUInt8(format, 12, (byte)'%');
		APTR.WriteUInt8(format, 13, (byte)'%');
		APTR.WriteUInt8(format, 14, 0);
		APTR.WriteUInt8(text, 0, (byte)'O');
		APTR.WriteUInt8(text, 1, (byte)'K');
		APTR.WriteUInt8(text, 2, 0);
		APTR.WriteUInt32(parameters, 0, unchecked((uint)-12));
		APTR.WriteUInt32(parameters, 4, text.Raw);
		if (!MuiRequesterFormatCore.TryMaterialize(ref platform, format,
			parameters, out var result, out var allocation)) return 1;
		if (APTR.ReadUInt8(result, 0) != (byte)'V' ||
			APTR.ReadUInt8(result, 1) != (byte)'=' ||
			APTR.ReadUInt8(result, 2) != (byte)'-' ||
			APTR.ReadUInt8(result, 6) != (byte)'1' ||
			APTR.ReadUInt8(result, 7) != (byte)'2' ||
			APTR.ReadUInt8(result, 8) != (byte)' ' ||
			APTR.ReadUInt8(result, 9) != (byte)'O' ||
			APTR.ReadUInt8(result, 10) != (byte)'K' ||
			APTR.ReadUInt8(result, 11) != (byte)' ' ||
			APTR.ReadUInt8(result, 12) != (byte)'%') return 2;
		platform.Free(result, allocation);
		return 42;
	}

	// Final-MG09 Misc family service closure. The family-neutral standalone
	// route must claim a Keyadjust packet without exposing the Misc dispatcher
	// as a separate public selection to callers.
	public static uint MiscServiceDispatcherRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var instance = APTR.FromPointer(0x00036300);
		var classId = APTR.FromPointer(0x00036100);
		var packet = APTR.FromPointer(0x00036200);
		var storage = APTR.FromPointer(0x00036240);
		WriteClassId(classId, 'K', 'e', 'y', 'a', 'd', 'j', 'u', 's', 't');
		if (MuiMiscSpecialistCore.CreateByName(ref platform, instance, classId) !=
			MuiMiscSpecialistClass.Keyadjust) return 1;
		APTR.WriteUInt32(packet, 0, 0x00000104);
		APTR.WriteUInt32(packet, 4, MuiMiscAttributes.Keyadjust_AllowMouseEvents);
		APTR.WriteUInt32(packet, 8, storage.Raw);
		if (MuiSpecialistServiceDispatcher.DispatchStandaloneService(ref platform,
			instance, packet) != 1 || APTR.ReadUInt32(storage, 0) != 0) return 2;
		APTR.WriteUInt32(packet, 0, 0x8042549A);
		APTR.WriteUInt32(packet, 4, MuiMiscAttributes.Keyadjust_AllowMouseEvents);
		APTR.WriteUInt32(packet, 8, 1);
		if (MuiSpecialistServiceDispatcher.DispatchStandalone(ref platform,
			instance, packet) != 1) return 3;
		APTR.WriteUInt32(packet, 0, 0x00000102);
		if (MuiSpecialistServiceDispatcher.DispatchStandalone(ref platform,
			instance, packet) != 1 || MuiMiscSpecialistCore.Valid(ref platform,
			instance)) return 4;
		return 42;
	}

	// MG09 Misc lifecycle closure. Setup and Cleanup use the exact
	// no-argument { MethodID } packet and keep lifecycle state in guest memory.
	public static uint MiscSetupCleanupRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var instance = APTR.FromPointer(0x00036300);
		var classId = APTR.FromPointer(0x00036100);
		var packet = APTR.FromPointer(0x00036200);
		WriteClassId(classId, 'K', 'e', 'y', 'a', 'd', 'j', 'u', 's', 't');
		if (MuiMiscSpecialistCore.CreateByName(ref platform, instance, classId) !=
			MuiMiscSpecialistClass.Keyadjust) return 1;

		APTR.WriteUInt32(packet, 0, MuiMiscAttributes.Setup);
		if (MuiMiscSpecialistDispatcher.Dispatch(ref platform, instance,
			packet) != 1 || !MuiMiscSpecialistCore.IsSetupActive(ref platform,
			instance)) return 2;
		if (MuiMiscSpecialistDispatcher.Dispatch(ref platform, instance,
			packet) != 1 || !MuiMiscSpecialistCore.IsSetupActive(ref platform,
			instance)) return 3;

		APTR.WriteUInt32(packet, 0, MuiMiscAttributes.Cleanup);
		if (MuiMiscSpecialistDispatcher.Dispatch(ref platform, instance,
			packet) != 1 || MuiMiscSpecialistCore.IsSetupActive(ref platform,
			instance)) return 4;
		APTR.WriteUInt32(packet, 0, 0xDEADBEEFu);
		if (MuiMiscSpecialistDispatcher.Dispatch(ref platform, instance,
			packet) != 0) return 5;
		if (!MuiMiscSpecialistLifecycle.Dispose(ref platform, instance)) return 6;
		return 42;
	}

	// Separate external-resource closure for the Boopsi/Dtpic service seam.
	public static uint ExternalDispatcherRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var classId = APTR.FromPointer(0x00036100);
		var packet = APTR.FromPointer(0x00036200);
		var storage = APTR.FromPointer(0x00036240);
		var external = APTR.FromPointer(0x00036300);
		WriteClassId(classId, 'D', 't', 'p', 'i', 'c', (char)0, (char)0,
			(char)0, (char)0);
		if (MuiExternalWrapperCore.CreateByName(ref platform, external, classId) !=
			MuiExternalWrapperClass.Dtpic) return 1;
		APTR.WriteUInt32(packet, 0, 0x00000104);
		APTR.WriteUInt32(packet, 4, MuiExternalWrapperAttributes.Dtpic_Alpha);
		APTR.WriteUInt32(packet, 8, storage.Raw);
		if (MuiExternalWrapperDispatcher.Dispatch(ref platform, external,
			packet) != 1) return 2;
		APTR.WriteUInt32(packet, 0, 0x00000102);
		if (MuiExternalWrapperDispatcher.Dispatch(ref platform, external,
			packet) != 1) return 3;
		return 42;
	}

	// MG09 external-class object-factory closure. The class-service lease is
	// held in guest state until the constructed object is disposed; only then
	// may the external class record and loader lease be released.
	public static uint ExternalObjectServiceRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var headlessState = APTR.FromPointer(0x00036000);
		var serviceState = APTR.FromPointer(0x00036040);
		var classId = APTR.FromPointer(0x00036100);
		APTR.WriteUInt8(classId, 0, (byte)'F');
		APTR.WriteUInt8(classId, 1, (byte)'o');
		APTR.WriteUInt8(classId, 2, (byte)'o');
		APTR.WriteUInt8(classId, 3, (byte)'.');
		APTR.WriteUInt8(classId, 4, (byte)'m');
		APTR.WriteUInt8(classId, 5, (byte)'c');
		APTR.WriteUInt8(classId, 6, (byte)'c');
		APTR.WriteUInt8(classId, 7, 0);
		if (!MuiHeadlessObjectCore.Initialize(ref platform, headlessState))
			return 1;
		if (!MuiClassServiceCore.Initialize(ref platform, serviceState,
			headlessState)) return 2;
		var obj = MuiObjectFactoryServiceCore.NewObjectAWithClassService(
			ref platform, serviceState, headlessState, classId, APTR.Null);
		if (obj.IsNull) return 3;
		var classPointer = APTR.FromPointer(0x00036600);
		if (MuiClassServiceCore.ReferenceCount(ref platform, serviceState,
			classPointer) != 1) return 4;
		if (MuiClassServiceCore.ObjectLeaseCount(ref platform, serviceState,
			classPointer) != 1) return 5;
		if (MuiClassServiceCore.FreeClass(ref platform, serviceState,
			classPointer)) return 6;
		if (!MuiObjectDisposalServiceCore.DisposeObjectWithClassService(ref platform,
			serviceState, headlessState, obj)) return 7;
		if (MuiClassServiceCore.ReferenceCount(ref platform, serviceState,
			classPointer) != 0) return 8;
		if (MuiHeadlessObjectCore.FindClassByName(ref platform, headlessState,
			classId).IsNotNull) return 9;
		return 42;
	}

	// MG09 public MUI_DisposeObject closure. It accepts a known ordinary object,
	// rejects a second disposal, and releases an external class-service lease
	// only after the corresponding object record has been removed.
	public static uint DisposeObjectServiceRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var headlessState = APTR.FromPointer(0x00036000);
		var serviceState = APTR.FromPointer(0x00036040);
		var builtinName = APTR.FromPointer(0x00036100);
		APTR.WriteUInt8(builtinName, 0, (byte)'T');
		APTR.WriteUInt8(builtinName, 1, (byte)'e');
		APTR.WriteUInt8(builtinName, 2, (byte)'x');
		APTR.WriteUInt8(builtinName, 3, (byte)'t');
		APTR.WriteUInt8(builtinName, 4, (byte)'.');
		APTR.WriteUInt8(builtinName, 5, (byte)'m');
		APTR.WriteUInt8(builtinName, 6, (byte)'u');
		APTR.WriteUInt8(builtinName, 7, (byte)'i');
		APTR.WriteUInt8(builtinName, 8, 0);
		if (!MuiHeadlessObjectCore.Initialize(ref platform, headlessState))
			return 1;
		var builtin = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			headlessState, builtinName, APTR.Null, 0, APTR.FromPointer(1));
		if (builtin.IsNull) return 2;
		var ordinary = MuiObjectFactoryServiceCore.NewObjectA(ref platform,
			headlessState, builtinName, APTR.Null);
		if (ordinary.IsNull) return 3;
		if (!MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			serviceState, headlessState, ordinary)) return 4;
		if (MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			serviceState, headlessState, ordinary)) return 5;

		var classId = APTR.FromPointer(0x00036140);
		APTR.WriteUInt8(classId, 0, (byte)'F');
		APTR.WriteUInt8(classId, 1, (byte)'o');
		APTR.WriteUInt8(classId, 2, (byte)'o');
		APTR.WriteUInt8(classId, 3, (byte)'.');
		APTR.WriteUInt8(classId, 4, (byte)'m');
		APTR.WriteUInt8(classId, 5, (byte)'c');
		APTR.WriteUInt8(classId, 6, (byte)'c');
		APTR.WriteUInt8(classId, 7, 0);
		if (!MuiClassServiceCore.Initialize(ref platform, serviceState,
			headlessState)) return 6;
		var external = MuiObjectFactoryServiceCore.NewObjectAWithClassService(
			ref platform, serviceState, headlessState, classId, APTR.Null);
		if (external.IsNull) return 7;
		if (!MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			serviceState, headlessState, external)) return 8;
		if (MuiHeadlessObjectCore.FindClassByName(ref platform, headlessState,
			classId).IsNotNull) return 9;
		return 42;
	}

	public static uint MakeObjectServiceRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint textName = 0x00036100;
		const uint rectangleName = 0x00036120;
		const uint imageName = 0x00036140;
		const uint cycleName = 0x000361E0;
		const uint radioName = 0x00036200;
		const uint sliderName = 0x00036220;
		const uint stringName = 0x00036240;
		const uint numericButtonName = 0x000362F0;
		const uint menustripName = 0x00036360;
		const uint menuName = 0x00036380;
		const uint menuitemName = 0x000363A0;
		const uint label = 0x00036160;
		const uint hSpaceParameters = 0x00036180;
		const uint buttonParameters = 0x00036190;
		const uint labelParameters = 0x000361A0;
		const uint checkmarkParameters = 0x000361B0;
		const uint vSpaceParameters = 0x000361C0;
		const uint barTitleParameters = 0x000361D0;
		const uint entries = 0x00036260;
		const uint firstEntry = 0x00036270;
		const uint secondEntry = 0x00036280;
		const uint thirdEntry = 0x00036290;
		const uint cycleParameters = 0x000362A0;
		const uint radioParameters = 0x000362B0;
		const uint sliderParameters = 0x000362C0;
		const uint stringParameters = 0x000362D0;
		const uint popButtonParameters = 0x00036310;
		const uint numericButtonParameters = 0x00036320;
		const uint numericFormat = 0x00036340;
		const uint menuitemParameters = 0x000363C0;
		const uint newMenus = 0x00036400;
		const uint projectTitle = 0x00036500;
		const uint openTitle = 0x00036520;
		const uint openShortcut = 0x00036540;
		const uint modesTitle = 0x00036560;
		const uint standardTitle = 0x00036580;
		const uint editTitle = 0x000365A0;
		const uint quitTitle = 0x000365C0;
		const uint quitShortcut = 0x000365E0;
		const uint copiedLabel = 0x00036600;
		const uint copiedShortcut = 0x00036620;
		const uint expectedLabel = 0x00036640;
		const uint expectedShortcut = 0x00036660;
		WriteTextClassName(APTR.FromPointer(textName));
		WriteRectangleClassName(APTR.FromPointer(rectangleName));
		WriteImageClassName(APTR.FromPointer(imageName));
		WriteCycleClassName(APTR.FromPointer(cycleName));
		WriteRadioClassName(APTR.FromPointer(radioName));
		WriteSliderClassName(APTR.FromPointer(sliderName));
		WriteStringClassName(APTR.FromPointer(stringName));
		WriteNumericbuttonClassName(APTR.FromPointer(numericButtonName));
		WriteMenustripClassName(APTR.FromPointer(menustripName));
		WriteMenuClassName(APTR.FromPointer(menuName));
		WriteMenuitemClassName(APTR.FromPointer(menuitemName));
		APTR.WriteUInt8(APTR.FromPointer(label), 0, (byte)'M');
		APTR.WriteUInt8(APTR.FromPointer(label), 1, (byte)'U');
		APTR.WriteUInt8(APTR.FromPointer(label), 2, (byte)'I');
		APTR.WriteUInt8(APTR.FromPointer(label), 3, (byte)' ');
		APTR.WriteUInt8(APTR.FromPointer(label), 4, (byte)'N');
		APTR.WriteUInt8(APTR.FromPointer(label), 5, (byte)'a');
		APTR.WriteUInt8(APTR.FromPointer(label), 6, (byte)'t');
		APTR.WriteUInt8(APTR.FromPointer(label), 7, (byte)'i');
		APTR.WriteUInt8(APTR.FromPointer(label), 8, (byte)'v');
		APTR.WriteUInt8(APTR.FromPointer(label), 9, (byte)'e');
		APTR.WriteUInt8(APTR.FromPointer(label), 10, 0);
		APTR.WriteUInt32(APTR.FromPointer(hSpaceParameters), 0, 19);
		APTR.WriteUInt32(APTR.FromPointer(buttonParameters), 0, label);
		APTR.WriteUInt32(APTR.FromPointer(labelParameters), 0, label);
		APTR.WriteUInt32(APTR.FromPointer(labelParameters), 4,
			0x00000100 | 0x00000041);
		APTR.WriteUInt32(APTR.FromPointer(checkmarkParameters), 0, 1);
		APTR.WriteUInt32(APTR.FromPointer(vSpaceParameters), 0, 13);
		APTR.WriteUInt32(APTR.FromPointer(barTitleParameters), 0, label);
		APTR.WriteUInt32(APTR.FromPointer(entries), 0, firstEntry);
		APTR.WriteUInt32(APTR.FromPointer(entries), 4, secondEntry);
		APTR.WriteUInt32(APTR.FromPointer(entries), 8, thirdEntry);
		APTR.WriteUInt32(APTR.FromPointer(entries), 12, 0);
		WriteFirstEntry(APTR.FromPointer(firstEntry));
		WriteSecondEntry(APTR.FromPointer(secondEntry));
		WriteThirdEntry(APTR.FromPointer(thirdEntry));
		APTR.WriteUInt32(APTR.FromPointer(cycleParameters), 0, label);
		APTR.WriteUInt32(APTR.FromPointer(cycleParameters), 4, entries);
		APTR.WriteUInt32(APTR.FromPointer(radioParameters), 0, label);
		APTR.WriteUInt32(APTR.FromPointer(radioParameters), 4, entries);
		APTR.WriteUInt32(APTR.FromPointer(sliderParameters), 0, label);
		APTR.WriteUInt32(APTR.FromPointer(sliderParameters), 4, unchecked((uint)-10));
		APTR.WriteUInt32(APTR.FromPointer(sliderParameters), 8, 90);
		APTR.WriteUInt32(APTR.FromPointer(stringParameters), 0, label);
		APTR.WriteUInt32(APTR.FromPointer(stringParameters), 4, 24);
		APTR.WriteUInt32(APTR.FromPointer(popButtonParameters), 0, 15);
		APTR.WriteUInt32(APTR.FromPointer(numericButtonParameters), 0, label);
		APTR.WriteUInt32(APTR.FromPointer(numericButtonParameters), 4,
			unchecked((uint)-5));
		APTR.WriteUInt32(APTR.FromPointer(numericButtonParameters), 8, 95);
		APTR.WriteUInt32(APTR.FromPointer(numericButtonParameters), 12,
			numericFormat);
		APTR.WriteUInt8(APTR.FromPointer(numericFormat), 0, (byte)'%');
		APTR.WriteUInt8(APTR.FromPointer(numericFormat), 1, (byte)'l');
		APTR.WriteUInt8(APTR.FromPointer(numericFormat), 2, (byte)'d');
		APTR.WriteUInt8(APTR.FromPointer(numericFormat), 3, 0);
		WriteNativeCString(APTR.FromPointer(projectTitle), 'P', 'r', 'o', 'j',
			'e', 'c', 't');
		WriteNativeCString(APTR.FromPointer(openTitle), 'O', 'p', 'e', 'n', 0, 0, 0);
		WriteNativeCString(APTR.FromPointer(openShortcut), 'O', 0, 0, 0, 0, 0, 0);
		WriteNativeCString(APTR.FromPointer(modesTitle), 'M', 'o', 'd', 'e',
			's', 0, 0);
		WriteNativeCString(APTR.FromPointer(standardTitle), 'S', 't', 'a', 'n',
			'd', 'a', 'r');
		APTR.WriteUInt8(APTR.FromPointer(standardTitle), 7, (byte)'d');
		APTR.WriteUInt8(APTR.FromPointer(standardTitle), 8, 0);
		WriteNativeCString(APTR.FromPointer(editTitle), 'E', 'd', 'i', 't', 0,
			0, 0);
		WriteNativeCString(APTR.FromPointer(quitTitle), 'Q', 'u', 'i', 't', 0,
			0, 0);
		WriteNativeCString(APTR.FromPointer(quitShortcut), 'Q', 0, 0, 0, 0, 0, 0);
		WriteNativeNewMenu(APTR.FromPointer(newMenus + 0), 1, projectTitle, 0,
			0, 0, 0x111);
		WriteNativeNewMenu(APTR.FromPointer(newMenus + 20), 2, openTitle,
			openShortcut, 0, 0, 0x222);
		WriteNativeNewMenu(APTR.FromPointer(newMenus + 40), 2, modesTitle, 0,
			0x129, 0xFFFFFFFE, 0x333);
		WriteNativeNewMenu(APTR.FromPointer(newMenus + 60), 3, standardTitle, 0,
			1, 0xFFFFFFFD, 0x444);
		WriteNativeNewMenu(APTR.FromPointer(newMenus + 80), 2, 0xFFFFFFFF, 0,
			0, 0, 0x555);
		WriteNativeNewMenu(APTR.FromPointer(newMenus + 100), 1, editTitle, 0,
			0, 0, 0x666);
		WriteNativeNewMenu(APTR.FromPointer(newMenus + 120), 2, quitTitle,
			quitShortcut, 0x10, 0, 0x777);
		WriteNativeNewMenu(APTR.FromPointer(newMenus + 140), 0, 0, 0, 0, 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(menuitemParameters), 0, label);
		APTR.WriteUInt32(APTR.FromPointer(menuitemParameters), 4, openShortcut);
		APTR.WriteUInt32(APTR.FromPointer(menuitemParameters), 8, 0x129);
		APTR.WriteUInt32(APTR.FromPointer(menuitemParameters), 12, 0xCAFE);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		if (MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(textName), APTR.Null, 0,
			APTR.FromPointer(1)).IsNull) return 2;
		if (MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(rectangleName), APTR.Null, 0,
			APTR.FromPointer(2)).IsNull) return 3;
		if (MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(imageName), APTR.Null, 0,
			APTR.FromPointer(3)).IsNull) return 4;
		if (MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(cycleName), APTR.Null, 0,
			APTR.FromPointer(4)).IsNull) return 5;
		if (MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(radioName), APTR.Null, 0,
			APTR.FromPointer(5)).IsNull) return 6;
		if (MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(sliderName), APTR.Null, 0,
			APTR.FromPointer(6)).IsNull) return 7;
		if (MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(stringName), APTR.Null, 0,
			APTR.FromPointer(7)).IsNull) return 8;
		if (MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(numericButtonName), APTR.Null, 0,
			APTR.FromPointer(8)).IsNull) return 9;
		if (MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(menustripName), APTR.Null, 0,
			APTR.FromPointer(9)).IsNull) return 10;
		if (MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(menuName), APTR.Null, 0,
			APTR.FromPointer(10)).IsNull) return 11;
		if (MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(menuitemName), APTR.Null, 0,
			APTR.FromPointer(11)).IsNull) return 12;
		var hSpace = MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			APTR.FromPointer(state), MuiMakeObjectServiceCore.MUIO_HSpace,
			APTR.FromPointer(hSpaceParameters));
		if (hSpace.IsNull) return 5;
		uint value;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			hSpace, 0x8042A3F1, out value) || value != 19) return 6;
		var vSpace = MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			APTR.FromPointer(state), MuiMakeObjectServiceCore.MUIO_VSpace,
			APTR.FromPointer(vSpaceParameters));
		if (vSpace.IsNull) return 7;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			vSpace, 0x8042A92B, out value) || value != 13) return 8;
		var hBar = MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			APTR.FromPointer(state), MuiMakeObjectServiceCore.MUIO_HBar,
			APTR.FromPointer(hSpaceParameters));
		if (hBar.IsNull || !MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), hBar, 0x8042C943, out value) || value != 1)
			return 9;
		var vBar = MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			APTR.FromPointer(state), MuiMakeObjectServiceCore.MUIO_VBar,
			APTR.FromPointer(vSpaceParameters));
		if (vBar.IsNull || !MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), vBar, 0x80422204, out value) || value != 1)
			return 10;
		var barTitle = MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			APTR.FromPointer(state), MuiMakeObjectServiceCore.MUIO_BarTitle,
			APTR.FromPointer(barTitleParameters));
		if (barTitle.IsNull || !MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), barTitle, 0x80426689, out value) ||
			value != label) return 11;
		var button = MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			APTR.FromPointer(state), MuiMakeObjectServiceCore.MUIO_Button,
			APTR.FromPointer(buttonParameters));
		if (button.IsNull) return 12;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			button, 0x8042AC64, out value) || value != 1) return 13;
		var muiLabel = MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			APTR.FromPointer(state), MuiMakeObjectServiceCore.MUIO_Label,
			APTR.FromPointer(labelParameters));
		if (muiLabel.IsNull) return 14;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			muiLabel, 0x804218FF, out value) || value != 0x41) return 15;
		var checkmark = MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			APTR.FromPointer(state), MuiMakeObjectServiceCore.MUIO_Checkmark,
			APTR.FromPointer(checkmarkParameters));
		if (checkmark.IsNull) return 16;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			checkmark, 0x8042654B, out value) || value != 1) return 17;
		var cycle = MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			APTR.FromPointer(state), MuiMakeObjectServiceCore.MUIO_Cycle,
			APTR.FromPointer(cycleParameters));
		if (cycle.IsNull || !MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), cycle, 0x80420629, out value) ||
			value != entries) return 22;
		var radio = MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			APTR.FromPointer(state), MuiMakeObjectServiceCore.MUIO_Radio,
			APTR.FromPointer(radioParameters));
		if (radio.IsNull || !MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), radio, 0x8042B6A1, out value) ||
			value != entries) return 23;
		var radioChild = MuiFamilyCore.GetChild(ref platform,
			APTR.FromPointer(state), radio, 0, APTR.Null);
		if (radioChild.IsNull || MuiCommonControlCore.Classify(ref platform,
			APTR.FromPointer(state), radioChild) != MuiControlClass.Text) return 24;
		var slider = MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			APTR.FromPointer(state), MuiMakeObjectServiceCore.MUIO_Slider,
			APTR.FromPointer(sliderParameters));
		if (slider.IsNull || !MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), slider, 0x8042E404, out value) ||
			value != unchecked((uint)-10)) return 25;
		var stringObject = MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			APTR.FromPointer(state), MuiMakeObjectServiceCore.MUIO_String,
			APTR.FromPointer(stringParameters));
		if (stringObject.IsNull || !MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), stringObject, 0x80424984, out value) ||
			value != 24) return 26;
		var popButton = MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			APTR.FromPointer(state), MuiMakeObjectServiceCore.MUIO_PopButton,
			APTR.FromPointer(popButtonParameters));
		if (popButton.IsNull || !MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), popButton, 0x8042AC64, out value) || value != 2)
			return 27;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			popButton, 0x804233D5, out value) || value != 15) return 28;
		var numericButton = MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			APTR.FromPointer(state), MuiMakeObjectServiceCore.MUIO_NumericButton,
			APTR.FromPointer(numericButtonParameters));
		if (numericButton.IsNull || !MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), numericButton, 0x8042E404, out value) ||
			value != unchecked((uint)-5)) return 29;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			numericButton, 0x8042D78A, out value) || value != 95) return 30;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			numericButton, 0x804263E9, out value) ||
			!CStringEquals(APTR.FromPointer(value), APTR.FromPointer(numericFormat)))
			return 31;
		var directMenuitem = MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			APTR.FromPointer(state), MuiMakeObjectServiceCore.MUIO_Menuitem,
			APTR.FromPointer(menuitemParameters));
		if (directMenuitem.IsNull || !MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), directMenuitem, 0x8042562A, out value) ||
			value != 1) return 32;
		if (MuiMenuSpecialistCore.Classify(ref platform, APTR.FromPointer(state),
			directMenuitem) != MuiMenuSpecialistClass.Menuitem) return 32;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			directMenuitem, 0x8042B9CC, out value) || value != 1) return 33;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			directMenuitem, 0x80420313, out value) || value != 0xCAFE) return 34;
		if (!MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			APTR.FromPointer(state), directMenuitem)) return 35;
		WriteNativeCString(APTR.FromPointer(copiedLabel), 'C', 'o', 'p', 'i',
			'e', 'd', 0);
		WriteNativeCString(APTR.FromPointer(copiedShortcut), 'C', 0, 0, 0, 0, 0, 0);
		WriteNativeCString(APTR.FromPointer(expectedLabel), 'C', 'o', 'p', 'i',
			'e', 'd', 0);
		WriteNativeCString(APTR.FromPointer(expectedShortcut), 'C', 0, 0, 0, 0, 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(menuitemParameters), 0, copiedLabel);
		APTR.WriteUInt32(APTR.FromPointer(menuitemParameters), 4, copiedShortcut);
		APTR.WriteUInt32(APTR.FromPointer(menuitemParameters), 8, 0x40000129);
		var copiedMenuitem = MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			APTR.FromPointer(state), MuiMakeObjectServiceCore.MUIO_Menuitem,
			APTR.FromPointer(menuitemParameters));
		if (copiedMenuitem.IsNull || !MuiMenuSpecialistCore.CopyStringsFlag(ref platform,
			APTR.FromPointer(state), copiedMenuitem)) return 60;
		WriteNativeCString(APTR.FromPointer(copiedLabel), 'C', 'h', 'a', 'n',
			'g', 'e', 'd');
		WriteNativeCString(APTR.FromPointer(copiedShortcut), 'X', 0, 0, 0, 0, 0, 0);
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			copiedMenuitem, 0x804218BE, out value) ||
			!CStringEquals(APTR.FromPointer(value), APTR.FromPointer(expectedLabel)) ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
				copiedMenuitem, 0x80422030, out value) ||
			!CStringEquals(APTR.FromPointer(value), APTR.FromPointer(expectedShortcut)))
			return 61;
		if (!MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			APTR.FromPointer(state), copiedMenuitem)) return 62;
		var menuParameters = APTR.FromPointer(newMenus - 0x40);
		APTR.WriteUInt32(menuParameters, 0, newMenus);
		APTR.WriteUInt32(menuParameters, 4, 0);
		if (MuiHeadlessObjectCore.FindClassByName(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(menustripName)).IsNull) return 46;
		if (MuiHeadlessObjectCore.FindClassByName(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(menuName)).IsNull) return 47;
		if (MuiHeadlessObjectCore.FindClassByName(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(menuitemName)).IsNull) return 48;
		if (APTR.ReadUInt8(APTR.FromPointer(newMenus), 0) != 1 ||
			APTR.ReadUInt32(APTR.FromPointer(newMenus), 2) != projectTitle)
			return 49;
		if (APTR.ReadUInt8(APTR.FromPointer(newMenus + 20), 0) != 2 ||
			APTR.ReadUInt32(APTR.FromPointer(newMenus + 20), 2) != openTitle ||
			APTR.ReadUInt32(APTR.FromPointer(newMenus + 20), 6) != openShortcut)
			return 56;
		if (APTR.ReadUInt8(APTR.FromPointer(newMenus + 40), 0) != 2 ||
			APTR.ReadUInt32(APTR.FromPointer(newMenus + 40), 2) != modesTitle)
			return 57;
		if (APTR.ReadUInt8(APTR.FromPointer(newMenus + 60), 0) != 3 ||
			APTR.ReadUInt32(APTR.FromPointer(newMenus + 60), 2) != standardTitle)
			return 58;
		if (APTR.ReadUInt8(APTR.FromPointer(newMenus + 80), 0) != 2 ||
			APTR.ReadUInt32(APTR.FromPointer(newMenus + 80), 2) != 0xFFFFFFFF)
			return 59;
		if (APTR.ReadUInt32(APTR.FromPointer(0x00036F00), 0) > 0x0003F000)
			return 50;
		var menuStrip = MuiMakeObjectServiceCore.MakeObjectA(ref platform,
			APTR.FromPointer(state), MuiMakeObjectServiceCore.MUIO_MenustripNM,
			menuParameters);
		if (menuStrip.IsNull) return 36;
		if (MuiMenuSpecialistCore.Classify(ref platform, APTR.FromPointer(state),
			menuStrip) != MuiMenuSpecialistClass.Menustrip) return 36;
		var projectMenu = MuiFamilyCore.GetChild(ref platform,
			APTR.FromPointer(state), menuStrip, 0, APTR.Null);
		var editMenu = MuiFamilyCore.GetChild(ref platform,
			APTR.FromPointer(state), menuStrip, 1, APTR.Null);
		if (projectMenu.IsNull || editMenu.IsNull) return 37;
		if (MuiMenuSpecialistCore.Classify(ref platform, APTR.FromPointer(state),
			projectMenu) != MuiMenuSpecialistClass.Menu) return 37;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			projectMenu, 0x8042A0E3, out value) || value != projectTitle) return 38;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			projectMenu, 0x8042ED48, out value) || value != 1) return 39;
		var modesMenuitem = MuiFamilyCore.GetChild(ref platform,
			APTR.FromPointer(state), projectMenu, 1, APTR.Null);
		if (modesMenuitem.IsNull || !MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), modesMenuitem, 0x80420BC6, out value) ||
			value != 0xFFFFFFFE) return 40;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			modesMenuitem, 0x80420313, out value) || value != 0x333) return 41;
		var subMenuitem = MuiFamilyCore.GetChild(ref platform,
			APTR.FromPointer(state), modesMenuitem, 0, APTR.Null);
		var separator = MuiFamilyCore.GetChild(ref platform,
			APTR.FromPointer(state), projectMenu, 2, APTR.Null);
		if (subMenuitem.IsNull || separator.IsNull ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
				separator, 0x804218BE, out value) || value != 0xFFFFFFFF)
			return 45;
		if (!MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			APTR.FromPointer(state), menuStrip)) return 43;
		if (MuiMenuSpecialistCore.Valid(ref platform, APTR.FromPointer(state),
			menuStrip)) return 43;
		if (!MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			APTR.FromPointer(state), hSpace) ||
			!MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			APTR.FromPointer(state), button) ||
			!MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			APTR.FromPointer(state), muiLabel) ||
			!MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			APTR.FromPointer(state), checkmark) ||
			!MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			APTR.FromPointer(state), vSpace) ||
			!MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			APTR.FromPointer(state), hBar) ||
			!MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			APTR.FromPointer(state), vBar) ||
			!MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			APTR.FromPointer(state), barTitle) ||
			!MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			APTR.FromPointer(state), cycle) ||
			!MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			APTR.FromPointer(state), radio) ||
			!MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			APTR.FromPointer(state), slider) ||
			!MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			APTR.FromPointer(state), stringObject) ||
			!MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			APTR.FromPointer(state), popButton) ||
			!MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			APTR.FromPointer(state), numericButton)) return 44;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 19;
		return 42;
	}

	private static void WriteTextClassName(APTR address)
	{
		APTR.WriteUInt8(address, 0, (byte)'T');
		APTR.WriteUInt8(address, 1, (byte)'e');
		APTR.WriteUInt8(address, 2, (byte)'x');
		APTR.WriteUInt8(address, 3, (byte)'t');
		APTR.WriteUInt8(address, 4, (byte)'.');
		APTR.WriteUInt8(address, 5, (byte)'m');
		APTR.WriteUInt8(address, 6, (byte)'u');
		APTR.WriteUInt8(address, 7, (byte)'i');
		APTR.WriteUInt8(address, 8, 0);
	}

	private static void WriteRectangleClassName(APTR address)
	{
		APTR.WriteUInt8(address, 0, (byte)'R');
		APTR.WriteUInt8(address, 1, (byte)'e');
		APTR.WriteUInt8(address, 2, (byte)'c');
		APTR.WriteUInt8(address, 3, (byte)'t');
		APTR.WriteUInt8(address, 4, (byte)'a');
		APTR.WriteUInt8(address, 5, (byte)'n');
		APTR.WriteUInt8(address, 6, (byte)'g');
		APTR.WriteUInt8(address, 7, (byte)'l');
		APTR.WriteUInt8(address, 8, (byte)'e');
		APTR.WriteUInt8(address, 9, (byte)'.');
		APTR.WriteUInt8(address, 10, (byte)'m');
		APTR.WriteUInt8(address, 11, (byte)'u');
		APTR.WriteUInt8(address, 12, (byte)'i');
		APTR.WriteUInt8(address, 13, 0);
	}

	private static void WriteImageClassName(APTR address)
	{
		APTR.WriteUInt8(address, 0, (byte)'I');
		APTR.WriteUInt8(address, 1, (byte)'m');
		APTR.WriteUInt8(address, 2, (byte)'a');
		APTR.WriteUInt8(address, 3, (byte)'g');
		APTR.WriteUInt8(address, 4, (byte)'e');
		APTR.WriteUInt8(address, 5, (byte)'.');
		APTR.WriteUInt8(address, 6, (byte)'m');
		APTR.WriteUInt8(address, 7, (byte)'u');
		APTR.WriteUInt8(address, 8, (byte)'i');
		APTR.WriteUInt8(address, 9, 0);
	}

	private static void WriteCycleClassName(APTR address)
	{
		APTR.WriteUInt8(address, 0, (byte)'C');
		APTR.WriteUInt8(address, 1, (byte)'y');
		APTR.WriteUInt8(address, 2, (byte)'c');
		APTR.WriteUInt8(address, 3, (byte)'l');
		APTR.WriteUInt8(address, 4, (byte)'e');
		APTR.WriteUInt8(address, 5, (byte)'.');
		APTR.WriteUInt8(address, 6, (byte)'m');
		APTR.WriteUInt8(address, 7, (byte)'u');
		APTR.WriteUInt8(address, 8, (byte)'i');
		APTR.WriteUInt8(address, 9, 0);
	}

	private static void WriteRadioClassName(APTR address)
	{
		APTR.WriteUInt8(address, 0, (byte)'R');
		APTR.WriteUInt8(address, 1, (byte)'a');
		APTR.WriteUInt8(address, 2, (byte)'d');
		APTR.WriteUInt8(address, 3, (byte)'i');
		APTR.WriteUInt8(address, 4, (byte)'o');
		APTR.WriteUInt8(address, 5, (byte)'.');
		APTR.WriteUInt8(address, 6, (byte)'m');
		APTR.WriteUInt8(address, 7, (byte)'u');
		APTR.WriteUInt8(address, 8, (byte)'i');
		APTR.WriteUInt8(address, 9, 0);
	}

	private static void WriteSliderClassName(APTR address)
	{
		APTR.WriteUInt8(address, 0, (byte)'S');
		APTR.WriteUInt8(address, 1, (byte)'l');
		APTR.WriteUInt8(address, 2, (byte)'i');
		APTR.WriteUInt8(address, 3, (byte)'d');
		APTR.WriteUInt8(address, 4, (byte)'e');
		APTR.WriteUInt8(address, 5, (byte)'r');
		APTR.WriteUInt8(address, 6, (byte)'.');
		APTR.WriteUInt8(address, 7, (byte)'m');
		APTR.WriteUInt8(address, 8, (byte)'u');
		APTR.WriteUInt8(address, 9, (byte)'i');
		APTR.WriteUInt8(address, 10, 0);
	}

	private static void WriteStringClassName(APTR address)
	{
		APTR.WriteUInt8(address, 0, (byte)'S');
		APTR.WriteUInt8(address, 1, (byte)'t');
		APTR.WriteUInt8(address, 2, (byte)'r');
		APTR.WriteUInt8(address, 3, (byte)'i');
		APTR.WriteUInt8(address, 4, (byte)'n');
		APTR.WriteUInt8(address, 5, (byte)'g');
		APTR.WriteUInt8(address, 6, (byte)'.');
		APTR.WriteUInt8(address, 7, (byte)'m');
		APTR.WriteUInt8(address, 8, (byte)'u');
		APTR.WriteUInt8(address, 9, (byte)'i');
		APTR.WriteUInt8(address, 10, 0);
	}

	private static void WriteNumericbuttonClassName(APTR address)
	{
		APTR.WriteUInt8(address, 0, (byte)'N');
		APTR.WriteUInt8(address, 1, (byte)'u');
		APTR.WriteUInt8(address, 2, (byte)'m');
		APTR.WriteUInt8(address, 3, (byte)'e');
		APTR.WriteUInt8(address, 4, (byte)'r');
		APTR.WriteUInt8(address, 5, (byte)'i');
		APTR.WriteUInt8(address, 6, (byte)'c');
		APTR.WriteUInt8(address, 7, (byte)'b');
		APTR.WriteUInt8(address, 8, (byte)'u');
		APTR.WriteUInt8(address, 9, (byte)'t');
		APTR.WriteUInt8(address, 10, (byte)'t');
		APTR.WriteUInt8(address, 11, (byte)'o');
		APTR.WriteUInt8(address, 12, (byte)'n');
		APTR.WriteUInt8(address, 13, (byte)'.');
		APTR.WriteUInt8(address, 14, (byte)'m');
		APTR.WriteUInt8(address, 15, (byte)'u');
		APTR.WriteUInt8(address, 16, (byte)'i');
		APTR.WriteUInt8(address, 17, 0);
	}

	private static void WriteMenustripClassName(APTR address)
	{
		APTR.WriteUInt8(address, 0, (byte)'M');
		APTR.WriteUInt8(address, 1, (byte)'e');
		APTR.WriteUInt8(address, 2, (byte)'n');
		APTR.WriteUInt8(address, 3, (byte)'u');
		APTR.WriteUInt8(address, 4, (byte)'s');
		APTR.WriteUInt8(address, 5, (byte)'t');
		APTR.WriteUInt8(address, 6, (byte)'r');
		APTR.WriteUInt8(address, 7, (byte)'i');
		APTR.WriteUInt8(address, 8, (byte)'p');
		APTR.WriteUInt8(address, 9, (byte)'.');
		APTR.WriteUInt8(address, 10, (byte)'m');
		APTR.WriteUInt8(address, 11, (byte)'u');
		APTR.WriteUInt8(address, 12, (byte)'i');
		APTR.WriteUInt8(address, 13, 0);
	}

	private static void WriteMenuClassName(APTR address)
	{
		APTR.WriteUInt8(address, 0, (byte)'M');
		APTR.WriteUInt8(address, 1, (byte)'e');
		APTR.WriteUInt8(address, 2, (byte)'n');
		APTR.WriteUInt8(address, 3, (byte)'u');
		APTR.WriteUInt8(address, 4, (byte)'.');
		APTR.WriteUInt8(address, 5, (byte)'m');
		APTR.WriteUInt8(address, 6, (byte)'u');
		APTR.WriteUInt8(address, 7, (byte)'i');
		APTR.WriteUInt8(address, 8, 0);
	}

	private static void WriteMenuitemClassName(APTR address)
	{
		APTR.WriteUInt8(address, 0, (byte)'M');
		APTR.WriteUInt8(address, 1, (byte)'e');
		APTR.WriteUInt8(address, 2, (byte)'n');
		APTR.WriteUInt8(address, 3, (byte)'u');
		APTR.WriteUInt8(address, 4, (byte)'i');
		APTR.WriteUInt8(address, 5, (byte)'t');
		APTR.WriteUInt8(address, 6, (byte)'e');
		APTR.WriteUInt8(address, 7, (byte)'m');
		APTR.WriteUInt8(address, 8, (byte)'.');
		APTR.WriteUInt8(address, 9, (byte)'m');
		APTR.WriteUInt8(address, 10, (byte)'u');
		APTR.WriteUInt8(address, 11, (byte)'i');
		APTR.WriteUInt8(address, 12, 0);
	}

	private static void WriteNativeCString(APTR address, int c0, int c1,
		int c2, int c3, int c4, int c5, int c6)
	{
		APTR.WriteUInt8(address, 0, (byte)c0);
		APTR.WriteUInt8(address, 1, (byte)c1);
		APTR.WriteUInt8(address, 2, (byte)c2);
		APTR.WriteUInt8(address, 3, (byte)c3);
		APTR.WriteUInt8(address, 4, (byte)c4);
		APTR.WriteUInt8(address, 5, (byte)c5);
		APTR.WriteUInt8(address, 6, (byte)c6);
		APTR.WriteUInt8(address, 7, 0);
	}

	private static void WriteNativeNewMenu(APTR address, uint type, uint label,
		uint shortcut, uint flags, uint mutualExclude, uint userData)
	{
		APTR.WriteUInt8(address, 0, (byte)type);
		APTR.WriteUInt8(address, 1, 0);
		APTR.WriteUInt32(address, 2, label);
		APTR.WriteUInt32(address, 6, shortcut);
		APTR.WriteUInt16(address, 10, (ushort)flags);
		APTR.WriteUInt32(address, 12, mutualExclude);
		APTR.WriteUInt32(address, 16, userData);
	}

	private static bool CStringEquals(APTR left, APTR right)
	{
		for (var index = 0; index < 64; index++)
		{
			var a = APTR.ReadUInt8(left, index);
			var b = APTR.ReadUInt8(right, index);
			if (a != b) return false;
			if (a == 0) return true;
		}
		return false;
	}

	private static void WriteFirstEntry(APTR address)
	{
		APTR.WriteUInt8(address, 0, (byte)'F');
		APTR.WriteUInt8(address, 1, (byte)'i');
		APTR.WriteUInt8(address, 2, (byte)'r');
		APTR.WriteUInt8(address, 3, (byte)'s');
		APTR.WriteUInt8(address, 4, (byte)'t');
		APTR.WriteUInt8(address, 5, 0);
	}

	private static void WriteSecondEntry(APTR address)
	{
		APTR.WriteUInt8(address, 0, (byte)'S');
		APTR.WriteUInt8(address, 1, (byte)'e');
		APTR.WriteUInt8(address, 2, (byte)'c');
		APTR.WriteUInt8(address, 3, (byte)'o');
		APTR.WriteUInt8(address, 4, (byte)'n');
		APTR.WriteUInt8(address, 5, (byte)'d');
		APTR.WriteUInt8(address, 6, 0);
	}

	private static void WriteThirdEntry(APTR address)
	{
		APTR.WriteUInt8(address, 0, (byte)'T');
		APTR.WriteUInt8(address, 1, (byte)'h');
		APTR.WriteUInt8(address, 2, (byte)'i');
		APTR.WriteUInt8(address, 3, (byte)'r');
		APTR.WriteUInt8(address, 4, (byte)'d');
		APTR.WriteUInt8(address, 5, 0);
	}

	// MG09 MUI_Error/MUI_SetError closure. The service keeps the process-local
	// error value in a fixed guest record and returns the previous value from
	// SetError without managed or runtime state.
	public static uint ErrorServiceRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var state = APTR.FromPointer(0x00036000);
		if (MuiErrorServiceCore.Error(ref platform, state) != 0) return 1;
		if (MuiErrorServiceCore.SetError(ref platform, state, 7) != 0) return 2;
		if (!MuiErrorServiceCore.Initialize(ref platform, state)) return 3;
		if (MuiErrorServiceCore.Error(ref platform, state) != 0) return 4;
		if (MuiErrorServiceCore.SetError(ref platform, state, 7) != 0) return 5;
		if (MuiErrorServiceCore.Error(ref platform, state) != 7) return 6;
		if (MuiErrorServiceCore.SetError(ref platform, state, 0) != 7) return 7;
		if (MuiErrorServiceCore.Error(ref platform, state) != 0) return 8;
		return 42;
	}

	// MG09 error-record layout seam. The public error service uses the same
	// named guest state record for magic, version, value, and sequence fields.
	public static uint ErrorServiceRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var state = APTR.FromPointer(0x00036280);
		if (!MuiErrorServiceRecordPacketCore.WriteState(ref platform, state,
			0x4D554945, 1, 7, 3) ||
			MuiErrorServiceRecordPacketCore.DispatchState(ref platform, state) !=
				(0x4D554945u ^ 1u ^ 7u ^ 3u)) return 1;
		return 42;
	}

	// MG09 public IDCMP closure. Requests made before opening are retained in
	// the guest object record, then changed while the native window is open.
	public static uint IdcmpServiceRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		APTR.WriteUInt8(APTR.FromPointer(name), 0, (byte)'W');
		APTR.WriteUInt8(APTR.FromPointer(name), 1, (byte)'i');
		APTR.WriteUInt8(APTR.FromPointer(name), 2, (byte)'n');
		APTR.WriteUInt8(APTR.FromPointer(name), 3, (byte)'d');
		APTR.WriteUInt8(APTR.FromPointer(name), 4, (byte)'o');
		APTR.WriteUInt8(APTR.FromPointer(name), 5, (byte)'w');
		APTR.WriteUInt8(APTR.FromPointer(name), 6, (byte)'.');
		APTR.WriteUInt8(APTR.FromPointer(name), 7, (byte)'m');
		APTR.WriteUInt8(APTR.FromPointer(name), 8, (byte)'u');
		APTR.WriteUInt8(APTR.FromPointer(name), 9, (byte)'i');
		APTR.WriteUInt8(APTR.FromPointer(name), 10, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1)).Raw;
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(cl), APTR.Null).Raw;
		if (cl == 0 || window == 0) return 2;
		var windowObject = APTR.FromPointer(window);
		if (!MuiApplicationWindowCore.RequestIDCMP(ref platform,
			APTR.FromPointer(state), windowObject, 0x200)) return 3;
		if (!MuiApplicationWindowCore.OpenWindow(ref platform,
			APTR.FromPointer(state), windowObject, 0)) return 4;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), windowObject, 0x7FFE0013,
			out var mask) || mask != 0x200) return 5;
		if (!MuiApplicationWindowCore.RequestIDCMP(ref platform,
			APTR.FromPointer(state), windowObject, 4)) return 6;
		if (!MuiApplicationWindowCore.RejectIDCMP(ref platform,
			APTR.FromPointer(state), windowObject, 0x200)) return 7;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), windowObject, 0x7FFE0013,
			out mask) || mask != 4) return 8;
		if (!MuiApplicationWindowCore.CloseWindow(ref platform,
			APTR.FromPointer(state), windowObject)) return 9;
		if (!MuiApplicationWindowCore.RejectIDCMP(ref platform,
			APTR.FromPointer(state), windowObject, 4)) return 10;
		if (!MuiApplicationWindowCore.OpenWindow(ref platform,
			APTR.FromPointer(state), windowObject, 0)) return 11;
		if (!MuiApplicationWindowCore.CloseWindow(ref platform,
			APTR.FromPointer(state), windowObject)) return 12;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 13;
		return 42;
	}

	public static uint ApplicationCoreRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint parameters = 0x00036300;
		const uint handler = 0x00036340;
		const uint signalStorage = 0x00036380;
		const uint packet = 0x000363C0;
		APTR.WriteUInt8(APTR.FromPointer(name), 0, (byte)'A');
		APTR.WriteUInt8(APTR.FromPointer(name), 1, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1)).Raw;
		if (cl == 0) return 2;
		var application = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(cl), APTR.Null).Raw;
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(cl), APTR.Null).Raw;
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(cl), APTR.Null).Raw;
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(cl), APTR.Null).Raw;
		if (application == 0 || window == 0 || first == 0 || second == 0) return 3;
		if (!MuiApplicationWindowCore.InitializeApplication(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(application), 0x20) ||
			!MuiApplicationWindowCore.AddWindow(ref platform, APTR.FromPointer(state),
				APTR.FromPointer(application), APTR.FromPointer(window)) ||
			!MuiApplicationWindowCore.OpenWindow(ref platform, APTR.FromPointer(state),
				APTR.FromPointer(window), 0x200)) return 4;
		if (!MuiFamilyCore.AddTail(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(window), APTR.FromPointer(first)) ||
			!MuiFamilyCore.AddTail(ref platform, APTR.FromPointer(state),
				APTR.FromPointer(window), APTR.FromPointer(second))) return 5;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80426510);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, first);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, second);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 0);
		if (MuiApplicationDispatcher.DispatchWindowCycleChain(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(window),
			APTR.FromPointer(packet)) != 1) return 5;
		if (
			!MuiApplicationWindowCore.Activate(ref platform, APTR.FromPointer(state),
				APTR.FromPointer(window), APTR.FromPointer(first)) ||
			!MuiApplicationWindowCore.CycleActive(ref platform,
				APTR.FromPointer(state), APTR.FromPointer(window), true)) return 5;
		APTR.WriteUInt32(APTR.FromPointer(parameters), 0, 0x90000001);
		APTR.WriteUInt32(APTR.FromPointer(parameters), 4, 77);
		if (MuiApplicationWindowCore.PushMethod(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(application),
			APTR.FromPointer(first), 2, APTR.FromPointer(parameters)) == 0 ||
			MuiApplicationWindowCore.DispatchPushedMethod(ref platform,
				APTR.FromPointer(state), APTR.FromPointer(application)) == 0) return 6;
		APTR.WriteUInt32(APTR.FromPointer(handler), 8, first);
		APTR.WriteUInt32(APTR.FromPointer(handler), 12, 0x20);
		APTR.WriteUInt32(APTR.FromPointer(handler), 20, 0x90000002);
		if (!MuiApplicationWindowCore.AddInputHandler(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(application),
			APTR.FromPointer(handler)) || MuiApplicationWindowCore.DispatchInputHandlers(
			ref platform, APTR.FromPointer(state), APTR.FromPointer(application), 0x20) != 1)
			return 7;
		if (!MuiApplicationWindowCore.ReturnId(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(application), 42) || MuiApplicationWindowCore.Input(
			ref platform, APTR.FromPointer(state), APTR.FromPointer(application),
			APTR.FromPointer(signalStorage)) != 42) return 8;
		if (!MuiApplicationWindowCore.SetMenu(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(window), 7, true, true, true) ||
			!MuiApplicationWindowCore.SetIconified(ref platform,
				APTR.FromPointer(state), APTR.FromPointer(application), true) ||
			!MuiApplicationWindowCore.Requester(ref platform, APTR.FromPointer(state),
				APTR.FromPointer(application), APTR.FromPointer(window),
				APTR.FromPointer(first), true)) return 9;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x804276EF);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 99);
		if (MuiApplicationDispatcher.Dispatch(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(application), APTR.FromPointer(packet)) == 0) return 10;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042D0F5);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, signalStorage);
		if (MuiApplicationDispatcher.Dispatch(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(application), APTR.FromPointer(packet)) != 99) return 11;
		if (!MuiApplicationWindowCore.RemoveWindow(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(application),
			APTR.FromPointer(window)) || !MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 12;
		return 42;
	}

	// MG09 MUIM_Application_AboutMUI packet closure. The MorphOS-shaped
	// {MethodID, refwindow} frame accepts Null or a live MUI Window object,
	// rejects arbitrary guest pointers before reaching the presentation seam,
	// and records the last reference/request count in guest-resident state.
	public static uint ApplicationAboutMuiRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint packet = 0x00036200;
		WriteClassId(APTR.FromPointer(name), 'A', 'p', 'p', 'l', 'i', 'c', 'a',
			't', 'i');
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var app = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var win = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (app.IsNull || win.IsNull || !MuiApplicationWindowCore.InitializeApplication(
			ref platform, APTR.FromPointer(state), app, 0)) return 3;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042D21D);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0);
		if (MuiApplicationDispatcher.DispatchAboutMUI(ref platform, APTR.FromPointer(state),
			app, APTR.FromPointer(packet)) != 1) return 4;
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, win.Raw);
		if (MuiApplicationDispatcher.DispatchAboutMUI(ref platform, APTR.FromPointer(state),
			app, APTR.FromPointer(packet)) != 1) return 5;
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0x00036400);
		if (MuiApplicationDispatcher.DispatchAboutMUI(ref platform, APTR.FromPointer(state),
			app, APTR.FromPointer(packet)) != 0) return 6;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			app, 0x7FFE0021, out var requests) || requests != 2) return 7;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 8;
		return 42;
	}

	// MG09 MUIM_Application_ShowHelp packet closure. The MorphOS-shaped frame
	// validates bounded guest strings, resolves (Object *)-1 to the first open
	// child window, and retains the accepted request in guest state.
	public static uint ApplicationShowHelpRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		const uint helpName = 0x00036300;
		const uint helpNode = 0x00036340;
		WriteClassId(APTR.FromPointer(className), 'A', 'p', 'p', 'l', 'i', 'c', 'a',
			't', 'i');
		APTR.WriteUInt8(APTR.FromPointer(helpName), 0, (byte)'S');
		APTR.WriteUInt8(APTR.FromPointer(helpName), 1, (byte)'Y');
		APTR.WriteUInt8(APTR.FromPointer(helpName), 2, (byte)'S');
		APTR.WriteUInt8(APTR.FromPointer(helpName), 3, (byte)':');
		APTR.WriteUInt8(APTR.FromPointer(helpName), 4, (byte)'H');
		APTR.WriteUInt8(APTR.FromPointer(helpName), 5, (byte)'e');
		APTR.WriteUInt8(APTR.FromPointer(helpName), 6, (byte)'l');
		APTR.WriteUInt8(APTR.FromPointer(helpName), 7, (byte)'p');
		APTR.WriteUInt8(APTR.FromPointer(helpName), 8, (byte)'.');
		APTR.WriteUInt8(APTR.FromPointer(helpName), 9, (byte)'g');
		APTR.WriteUInt8(APTR.FromPointer(helpName), 10, (byte)'u');
		APTR.WriteUInt8(APTR.FromPointer(helpName), 11, (byte)'i');
		APTR.WriteUInt8(APTR.FromPointer(helpName), 12, (byte)'d');
		APTR.WriteUInt8(APTR.FromPointer(helpName), 13, (byte)'e');
		APTR.WriteUInt8(APTR.FromPointer(helpName), 14, 0);
		APTR.WriteUInt8(APTR.FromPointer(helpNode), 0, (byte)'m');
		APTR.WriteUInt8(APTR.FromPointer(helpNode), 1, (byte)'a');
		APTR.WriteUInt8(APTR.FromPointer(helpNode), 2, (byte)'i');
		APTR.WriteUInt8(APTR.FromPointer(helpNode), 3, (byte)'n');
		APTR.WriteUInt8(APTR.FromPointer(helpNode), 4, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(className), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var app = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var openWindow = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var closedWindow = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (app.IsNull || openWindow.IsNull || closedWindow.IsNull ||
			!MuiApplicationWindowCore.InitializeApplication(ref platform,
				APTR.FromPointer(state), app, 0) ||
			!MuiApplicationWindowCore.AddWindow(ref platform,
				APTR.FromPointer(state), app, openWindow) ||
			!MuiApplicationWindowCore.AddWindow(ref platform,
				APTR.FromPointer(state), app, closedWindow) ||
			!MuiApplicationWindowCore.OpenWindow(ref platform,
				APTR.FromPointer(state), openWindow, 0)) return 3;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80426479);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, uint.MaxValue);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, helpName);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, helpNode);
		APTR.WriteUInt32(APTR.FromPointer(packet), 16, unchecked((uint)-3));
		if (MuiApplicationDispatcher.DispatchShowHelp(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet)) != 1) return 4;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), app, 0x7FFE0024, out var reference) ||
			reference != openWindow.Raw ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), app, 0x7FFE0028, out var requests) ||
			requests != 1) return 5;
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 0);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 0);
		APTR.WriteUInt32(APTR.FromPointer(packet), 16, 7);
		if (MuiApplicationDispatcher.DispatchShowHelp(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet)) != 1 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), app, 0x7FFE0024, out reference) ||
			reference != 0 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), app, 0x7FFE0028, out requests) ||
			requests != 2) return 6;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 7;
		return 42;
	}

	// MG09 MUIM_Application_Execute/Run packet closure. Both zero-argument
	// methods drive the scheduler iteration and return the MorphOS
	// ReturnID_Quit sentinel when the application queue requests termination.
	public static uint ApplicationLoopRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		WriteClassId(APTR.FromPointer(className), 'A', 'p', 'p', 'l', 'i', 'c', 'a',
			't', 'i');
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(className), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var app = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (app.IsNull || !MuiApplicationWindowCore.InitializeApplication(ref platform,
			APTR.FromPointer(state), app, 0)) return 3;
		if (!MuiApplicationWindowCore.ReturnId(ref platform,
			APTR.FromPointer(state), app, uint.MaxValue)) return 4;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x804253F3);
		if (MuiApplicationDispatcher.DispatchApplicationLoop(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet)) != uint.MaxValue)
			return 5;
		if (!MuiApplicationWindowCore.ReturnId(ref platform,
			APTR.FromPointer(state), app, uint.MaxValue)) return 6;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x90420103);
		if (MuiApplicationDispatcher.DispatchApplicationLoop(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet)) != uint.MaxValue)
			return 7;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 8;
		return 42;
	}

	// MG09 MUIM_Application_DefaultConfigItem packet closure. The application
	// override seam supplies a value for a guest configuration identifier and
	// the accepted request is retained in guest-resident telemetry.
	public static uint ApplicationDefaultConfigRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		const uint configId = 0x00000099;
		WriteClassId(APTR.FromPointer(className), 'A', 'p', 'p', 'l', 'i', 'c', 'a',
			't', 'i');
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(className), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var app = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (app.IsNull || !MuiApplicationWindowCore.InitializeApplication(ref platform,
			APTR.FromPointer(state), app, 0)) return 3;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042D934);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, configId);
		if (MuiApplicationDispatcher.DispatchApplicationDefaultConfig(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet)) !=
			(configId ^ 0xA5A55A5Au)) return 4;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), app, 0x7FFE0029, out var storedId) ||
			storedId != configId ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), app, 0x7FFE002A, out var storedValue) ||
			storedValue != (configId ^ 0xA5A55A5Au) ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), app, 0x7FFE002B, out var requests) ||
			requests != 1) return 5;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 6;
		return 42;
	}

	// MG09 MUIM_Application_SetConfigItem packet record closure. The private
	// PSI boundary retains an opaque data pointer in a named guest struct; this
	// focused root checks the exact item/data/request fields without importing
	// the unrelated Application method families.
	public static uint ApplicationSetConfigItemRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint storage = 0x00036300;
		var record = APTR.FromPointer(storage);
		if (!MuiApplicationWindowCore.WriteSetConfigItemRecord(ref platform,
			record, 0x34, 0x00037100, 1) ||
			APTR.ReadUInt32(record, 0) != 0x41534349 ||
			APTR.ReadUInt32(record, 4) != 0x34 ||
			APTR.ReadUInt32(record, 8) != 0x00037100 ||
			APTR.ReadUInt32(record, 12) != 1) return 1;
		if (!MuiApplicationWindowCore.WriteSetConfigItemRecord(ref platform,
			record, 0x35, 0, 2) ||
			APTR.ReadUInt32(record, 4) != 0x35 ||
			APTR.ReadUInt32(record, 8) != 0 ||
			APTR.ReadUInt32(record, 12) != 2) return 2;
		return 42;
	}

	// MG09 MUIM_Application_SetConfigItem packet-dispatch closure. The
	// dispatcher decodes the fixed {MethodID, item, data} frame and the core
	// retains the opaque data pointer in its named guest state record.
	public static uint ApplicationSetConfigItemDispatchRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		const uint data = 0x00037100;
		WriteClassId(APTR.FromPointer(className), 'A', 'p', 'p', 'l', 'i', 'c', 'a',
			't', 'i');
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(className), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var app = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (app.IsNull || !MuiApplicationWindowCore.InitializeApplication(ref platform,
			APTR.FromPointer(state), app, 0)) return 3;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80424A80);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0x34);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, data);
		if (MuiApplicationDispatcher.DispatchApplicationSetConfigItem(
			ref platform, APTR.FromPointer(state), app,
			APTR.FromPointer(packet)) != 1) return 4;
		if (!MuiApplicationWindowCore.ReadSetConfigItemState(ref platform,
			APTR.FromPointer(state), app, out var item, out var storedData,
			out var requests) || item != 0x34 || storedData != data ||
			requests != 1) return 5;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0xDEADBEEFu);
		if (MuiApplicationDispatcher.DispatchApplicationSetConfigItem(
			ref platform, APTR.FromPointer(state), app,
			APTR.FromPointer(packet)) != 0) return 6;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 7;
		return 42;
	}

	// MG09 Group change bracket record closure. The guest-resident state is
	// written through the typed seam and every field is verified in place.
	public static uint GroupChangeRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint storage = 0x00036400;
		var record = APTR.FromPointer(storage);
		if (!MuiGroupChangeCore.WriteChangeRecord(ref platform, record, 2,
			0xA5, 1) || APTR.ReadUInt32(record, 0) != 0x47524348 ||
			APTR.ReadUInt32(record, 4) != 2 ||
			APTR.ReadUInt32(record, 8) != 0xA5 ||
			APTR.ReadUInt32(record, 12) != 1) return 1;
		if (!MuiGroupChangeCore.WriteChangeRecord(ref platform, record, 0,
			0, 2) || APTR.ReadUInt32(record, 4) != 0 ||
			APTR.ReadUInt32(record, 8) != 0 ||
			APTR.ReadUInt32(record, 12) != 2) return 2;
		return 42;
	}

	// MG09 Group change sidecar state. A public struct input is encoded and
	// decoded through the central 16-byte state codec; unmapped storage is
	// rejected without exposing the state layout to the caller.
	public static uint GroupChangeStateRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint storage = 0x00036420;
		var input = default(MuiGroupChangeRecordInput);
		input.Depth = 2;
		input.ExitFlags = 0xA5;
		input.ExitRequests = 1;
		var record = APTR.FromPointer(storage);
		if (!MuiGroupChangeCore.WriteChangeRecord(ref platform, record, input) ||
			MuiGroupChangeCore.DispatchChangeStateRecord(ref platform, record) !=
			0xA6) return 1;
		if (MuiGroupChangeCore.DispatchChangeStateRecord(ref platform,
			APTR.FromPointer(0x00050FFC)) != 0) return 2;
		return 42;
	}

	// MG09 Group change packet closure. The live host path owns the bracket
	// transitions; this native seam proves the named Init/Exit/ExitChange2
	// headers and rejects an unmapped/truncated packet without managed state.
	public static uint GroupChangePacketsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint packet = 0x00036480;
		var record = APTR.FromPointer(packet);
		if (!MuiGroupChangeCore.WriteInitChangeRecord(ref platform, record) ||
			MuiGroupChangeCore.DispatchRecord(ref platform, record) != 1) return 1;
		if (!MuiGroupChangeCore.WriteExitChangeRecord(ref platform, record) ||
			MuiGroupChangeCore.DispatchRecord(ref platform, record) != 1) return 2;
		if (!MuiGroupChangeCore.WriteExitChange2Record(ref platform, record,
			0xA5) || MuiGroupChangeCore.DispatchRecord(ref platform, record) !=
			0xA5) return 3;
		const uint truncated = 0x00050FFF;
		if (MuiGroupChangeCore.DispatchRecord(ref platform,
			APTR.FromPointer(truncated)) != 0) return 4;
		return 42;
	}

	// MG09 Group ordering packet records. Each fixed MorphOS packet is emitted
	// through a named struct seam and checked in guest memory.
	public static uint GroupOrderingRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint storage = 0x00036500;
		const uint child = 0x00037100;
		const uint after = 0x00037120;
		const uint objects = 0x00037140;
		var record = APTR.FromPointer(storage);
		var move = default(MuiGroupMoveMemberRecordInput);
		move.Object = APTR.FromPointer(child);
		move.Position = -2;
		if (!MuiGroupOperationsCore.WriteMoveMemberRecord(ref platform, record,
			move) || MuiGroupOperationsCore.DispatchMoveMemberRecord(ref platform,
			record) != 0xFFFC8EFEu) return 1;
		var reorder = default(MuiGroupReorderRecordInput);
		reorder.After = APTR.FromPointer(after);
		reorder.Objects = APTR.FromPointer(objects);
		if (!MuiGroupOperationsCore.WriteReorderRecord(ref platform, record,
			reorder) || MuiGroupOperationsCore.DispatchReorderRecord(ref platform,
			record) != 0x60) return 2;
		var sort = default(MuiGroupSortRecordInput);
		sort.Objects = APTR.FromPointer(objects);
		if (!MuiGroupOperationsCore.WriteSortRecord(ref platform, record, sort) ||
			MuiGroupOperationsCore.DispatchSortRecord(ref platform, record) !=
			objects) return 3;
		if (MuiGroupOperationsCore.DispatchSortRecord(ref platform,
			APTR.FromPointer(0x00050FFC)) != 0) return 4;
		return 42;
	}

	// MG09 Group grid specification record. The eight ULONG fields are emitted
	// through the named value-type seam used by the layout implementation.
	public static uint GroupGridRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint storage = 0x00036600;
		var record = APTR.FromPointer(storage);
		if (!MuiGroupGridQualification.WriteSpecRecord(ref platform, record, 2,
			2, 4, 6, 1, 0, 2, 3) ||
			MuiGroupGridQualification.DispatchSpecRecord(ref platform, record) !=
				2) return 1;
		if (MuiGroupGridQualification.WriteSpecRecord(ref platform,
			APTR.FromPointer(0x00050FFC), record.Raw, 2, 4, 6, 1, 0, 2, 1) ||
			MuiGroupGridQualification.DispatchSpecRecord(ref platform, record) !=
				2) return 2;
		return 42;
	}

	// MG09 Group ActivePage state record. The canonical page index, transition
	// count, and last selector remain a fixed guest-resident value-type record.
	public static uint GroupPageRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint storage = 0x00036700;
		var record = APTR.FromPointer(storage);
		if (!MuiGroupPageCore.WritePageRecord(ref platform, record, 2, 7,
			unchecked((uint)MuiGroupPageCore.ActiveNext)) ||
			MuiGroupPageCore.DispatchPageRecord(ref platform, record) !=
				0xB8AFBEBF) return 1;
		if (MuiGroupPageCore.WritePageRecord(ref platform,
			APTR.FromPointer(0x00050FFC), 0, 0, 0) ||
			MuiGroupPageCore.DispatchPageRecord(ref platform, record) !=
				0xB8AFBEBF) return 2;
		return 42;
	}

	// MG09 Group forwarding state record. Forward and ForwardDepth are retained
	// with the bounded request counter as one named guest value-type record.
	public static uint GroupForwardRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint storage = 0x00036800;
		var record = APTR.FromPointer(storage);
		if (!MuiGroupChildrenCore.WriteForwardRecord(ref platform, record, 1, 1,
			7) || APTR.ReadUInt32(record, 0) != 0x47465744 ||
			APTR.ReadUInt32(record, 4) != 1 || APTR.ReadUInt32(record, 8) != 1 ||
			APTR.ReadUInt32(record, 12) != 7) return 1;
		if (MuiGroupChildrenCore.WriteForwardRecord(ref platform,
			APTR.FromPointer(0x00050FFC), 0, 0, 0) ||
			APTR.ReadUInt32(record, 4) != 1) return 2;
		return 42;
	}

	// MG09 Group child/forward sidecar records. Both private guest state
	// layouts are emitted through public struct inputs and read back through
	// their central codecs; malformed capacity and unmapped storage are rejected.
	public static uint GroupStateRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint forwardStorage = 0x00036800;
		const uint childStorage = 0x00036820;
		var forward = default(MuiGroupForwardRecordInput);
		forward.Forward = 1;
		forward.ForwardDepth = 1;
		forward.ForwardCount = 7;
		if (!MuiGroupChildrenCore.WriteForwardRecord(ref platform,
			APTR.FromPointer(forwardStorage), forward) ||
			MuiGroupChildrenCore.DispatchForwardRecord(ref platform,
			APTR.FromPointer(forwardStorage)) != 7) return 1;
		var child = default(MuiGroupChildListStateInput);
		child.Group = APTR.FromPointer(0x00036860);
		child.List = APTR.FromPointer(0x00036880);
		child.Entries = APTR.FromPointer(0x000368A0);
		child.Count = 2;
		child.Capacity = 3;
		child.Mutation = 5;
		child.Generation = 9;
		if (!MuiGroupChildrenCore.WriteChildListStateRecord(ref platform,
			APTR.FromPointer(childStorage), child) ||
			MuiGroupChildrenCore.DispatchChildListStateRecord(ref platform,
			APTR.FromPointer(childStorage)) != 0x0003684D) return 2;
		child.Capacity = 1;
		if (MuiGroupChildrenCore.WriteChildListStateRecord(ref platform,
			APTR.FromPointer(childStorage), child) ||
			MuiGroupChildrenCore.DispatchChildListStateRecord(ref platform,
			APTR.FromPointer(childStorage)) != 0x0003684D) return 3;
		if (MuiGroupChildrenCore.DispatchForwardRecord(ref platform,
			APTR.FromPointer(0x00050FFC)) != 0 ||
			MuiGroupChildrenCore.DispatchChildListStateRecord(ref platform,
			APTR.FromPointer(0x00050FFC)) != 0) return 4;
		return 42;
	}

	// MG09 Group ChildList projection. The read-only Exec List header is built
	// from the live Family topology and traversed through the typed local
	// NextObject seam; callers never mutate the projection directly.
	public static uint GroupChildListRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint listStorage = 0x00036800;
		const uint entriesStorage = 0x00036820;
		const uint first = 0x00036860;
		const uint second = 0x00036870;
		var list = APTR.FromPointer(listStorage);
		if (!MuiGroupChildrenCore.WriteChildListRecord(ref platform, list,
			APTR.FromPointer(entriesStorage), APTR.FromPointer(first),
			APTR.FromPointer(second))) return 1;
		var cursor = APTR.ReadUInt32(list, ExecLayout.List.Head);
		if (MuiGroupChildrenCore.NextObject(ref platform, list, ref cursor) !=
			APTR.FromPointer(first) || MuiGroupChildrenCore.NextObject(ref platform,
			list, ref cursor) != APTR.FromPointer(second) || cursor != 0) return 2;
		return 42;
	}

	// MG09 Group LayoutHook bridge. The hook receives the typed SDK
	// MUI_LayoutMsg and the same read-only ChildList projection used by
	// intuition.library/NextObject; the native platform writes deterministic
	// MINMAX/LAYOUT results without a managed callback object.
	public static uint GroupLayoutHookRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint listStorage = 0x00036800;
		const uint entriesStorage = 0x00036820;
		const uint first = 0x00036860;
		const uint second = 0x00036870;
		const uint hook = 0x000368A0;
		var list = APTR.FromPointer(listStorage);
		if (!MuiGroupChildrenCore.WriteChildListRecord(ref platform, list,
			APTR.FromPointer(entriesStorage), APTR.FromPointer(first),
			APTR.FromPointer(second))) return 1;
		APTR.WriteUInt32(APTR.FromPointer(hook), 8, 0x00CA0005u);
		if (!MuiGroupLayoutHookCore.InvokeMinMaxRecord(ref platform,
			APTR.FromPointer(hook), APTR.FromPointer(0x00036940), list,
			out var minMax) || minMax.MinWidth != 13 ||
			minMax.MinHeight != 17 || minMax.MaxWidth != 101 ||
			minMax.MaxHeight != 107 || minMax.DefWidth != 31 ||
			minMax.DefHeight != 37) return 2;
		if (!MuiGroupLayoutHookCore.InvokeLayoutRecord(ref platform,
			APTR.FromPointer(hook), APTR.FromPointer(0x00036940), list, 100, 40,
			out var dimensions) || dimensions.Width != 100 ||
			dimensions.Height != 40) return 3;
		return 42;
	}

	// MG09 MUIM_GetConfigItem packet closure.  The MorphOS public item is
	// MUICFG_PublicScreen (0x24); the packet validates the caller-owned ULONG
	// storage before the native-safe configuration capability is crossed.
	public static uint GetConfigItemRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		const uint storage = 0x00036300;
		WriteClassId(APTR.FromPointer(className), 'N', 'o', 't', 'i', 'f', 'y',
			'm', 'u', 'i');
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(className), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (obj.IsNull) return 3;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80423EDB);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0x24);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, storage);
		APTR.WriteUInt32(APTR.FromPointer(storage), 0, 0xDEADBEEFu);
		if (MuiHeadlessDispatcher.DispatchGetConfigItem(ref platform, APTR.FromPointer(state),
			obj, APTR.FromPointer(packet)) != 1 ||
			APTR.ReadUInt32(APTR.FromPointer(storage), 0) != 0x0003E000) return 4;
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0x25);
		if (MuiHeadlessDispatcher.DispatchGetConfigItem(ref platform, APTR.FromPointer(state),
			obj, APTR.FromPointer(packet)) != 0) return 5;
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0x24);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 0x00035EFE);
		if (MuiHeadlessDispatcher.DispatchGetConfigItem(ref platform, APTR.FromPointer(state),
			obj, APTR.FromPointer(packet)) != 0) return 6;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 7;
		return 42;
	}

	// MG09 MUIM_GetConfigItem fixed-packet closure. The existing live root
	// covers the capability and object checks; this seam isolates the named
	// 12-byte record and malformed-header behavior for freestanding 68k.
	public static uint GetConfigItemPacketsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint packet = 0x00036480;
		const uint storage = 0x000364A0;
		var record = APTR.FromPointer(packet);
		if (!MuiNotifyConfigMessageCore.WriteRecord(ref platform, record,
			0x24, APTR.FromPointer(storage)) ||
			MuiNotifyConfigMessageCore.DispatchRecord(ref platform, record) !=
			storage) return 1;
		const uint truncated = 0x00050FFF;
		if (MuiNotifyConfigMessageCore.DispatchRecord(ref platform,
			APTR.FromPointer(truncated)) != 0) return 2;
		APTR.WriteUInt32(record, 0, 0xDEADBEEFu);
		if (MuiNotifyConfigMessageCore.DispatchRecord(ref platform, record) != 0)
			return 3;
		return 42;
	}

	// MG09 Notify UserData packet closure. The fixed-layout Find/Get/Set
	// packets walk the live Family tree with a guest-resident frame stack;
	// SetUData updates every match while SetUDataOnce stops at the first.
	public static uint UserDataRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		const uint storage = 0x00036300;
		const uint attribute = 0x80420020;
		WriteClassId(APTR.FromPointer(className), 'N', 'o', 't', 'i', 'f', 'y',
			'm', 'u', 'i');
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(className), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var root = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var nested = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (root.IsNull || first.IsNull || second.IsNull || nested.IsNull) return 3;
		if (!MuiFamilyCore.AddTail(ref platform, APTR.FromPointer(state), root,
			first) || !MuiFamilyCore.AddTail(ref platform, APTR.FromPointer(state),
			root, second) || !MuiFamilyCore.AddTail(ref platform,
			APTR.FromPointer(state), first, nested)) return 4;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform,
			APTR.FromPointer(state), root, 0x80420313, 0x77, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform,
				APTR.FromPointer(state), first, 0x80420313, 0x77, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform,
				APTR.FromPointer(state), second, 0x80420313, 0x77, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform,
				APTR.FromPointer(state), nested, 0x80420313, 0x77, false)) return 5;

		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042C920);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0x77);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, attribute);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 0x1111);
		if (MuiHeadlessDispatcher.DispatchUserData(ref platform,
			APTR.FromPointer(state), root, APTR.FromPointer(packet)) != 1) return 6;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), nested, attribute, out var nestedValue) ||
			nestedValue != 0x1111) return 7;

		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042C196);
		if (MuiHeadlessDispatcher.DispatchUserData(ref platform,
			APTR.FromPointer(state), root, APTR.FromPointer(packet)) != root.Raw) return 8;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042ED0C);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, attribute);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, storage);
		if (MuiHeadlessDispatcher.DispatchUserData(ref platform,
			APTR.FromPointer(state), root, APTR.FromPointer(packet)) != 1 ||
			APTR.ReadUInt32(APTR.FromPointer(storage), 0) != 0x1111) return 9;

		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042CA19);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 0x2222);
		if (MuiHeadlessDispatcher.DispatchUserData(ref platform,
			APTR.FromPointer(state), root, APTR.FromPointer(packet)) != 1 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), first, attribute, out var firstValue) ||
			firstValue != 0x1111) return 10;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042ED0C);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 0x00035EFE);
		if (MuiHeadlessDispatcher.DispatchUserData(ref platform,
			APTR.FromPointer(state), root, APTR.FromPointer(packet)) != 0) return 11;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 12;
		return 42;
	}

	// MG09 MUIM_Application_OpenConfigWindow packet closure. The fixed
	// {MethodID, flags, classid} frame validates an optional guest C string and
	// delegates the non-blocking preferences-window request through the native
	// platform capability.
	public static uint ApplicationOpenConfigRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint configClassId = 0x00036140;
		const uint packet = 0x00036200;
		const uint flags = 0;
		WriteClassId(APTR.FromPointer(className), 'A', 'p', 'p', 'l', 'i', 'c', 'a',
			't', 'i');
		WriteClassId(APTR.FromPointer(configClassId), 'M', 'U', 'I', 'P', 'r',
			'e', 'f', 's', (char)0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(className), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var app = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (app.IsNull || !MuiApplicationWindowCore.InitializeApplication(ref platform,
			APTR.FromPointer(state), app, 0)) return 3;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x804299BA);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, flags);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, configClassId);
		if (MuiApplicationDispatcher.DispatchApplicationConfig(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet)) != 1) return 4;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), app, 0x7FFE002C, out var storedFlags) ||
			storedFlags != flags ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), app, 0x7FFE002D, out var storedClassId) ||
			storedClassId != configClassId ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), app, 0x7FFE002E, out var requests) ||
			requests != 1) return 5;
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 0);
		if (MuiApplicationDispatcher.DispatchApplicationConfig(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet)) != 1) return 6;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 7;
		return 42;
	}

	// MG09 MUIM_Application_BuildSettingsPanel packet closure. The fixed
	// {MethodID, number} frame asks the application capability for a settings
	// panel object; a non-null result is validated as a live MUI object and a
	// Null result represents an application without that panel number.
	public static uint ApplicationSettingsPanelRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036200;
		const uint number = 7;
		WriteClassId(APTR.FromPointer(className), 'A', 'p', 'p', 'l', 'i', 'c', 'a',
			't', 'i');
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(className), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var app = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (app.IsNull || !MuiApplicationWindowCore.InitializeApplication(ref platform,
			APTR.FromPointer(state), app, 0)) return 3;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042B58F);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, number);
		if (MuiApplicationDispatcher.DispatchApplicationSettings(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet)) != app.Raw)
			return 4;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), app, 0x7FFE002F, out var storedNumber) ||
			storedNumber != number ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), app, 0x7FFE0030, out var storedPanel) ||
			storedPanel != app.Raw ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), app, 0x7FFE0031, out var requests) ||
			requests != 1) return 5;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 6;
		return 42;
	}

	// MG09 MUIM_Application_Save/Load packet closure. The paired fixed frames
	// accept the documented ENV (Null), ENVARC (-1), or bounded guest-name
	// selectors and delegate object persistence through explicit native seams.
	public static uint ApplicationSettingsIORoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint name = 0x00036140;
		const uint packet = 0x00036200;
		WriteClassId(APTR.FromPointer(className), 'A', 'p', 'p', 'l', 'i', 'c', 'a',
			't', 'i');
		WriteClassId(APTR.FromPointer(name), 'P', 'r', 'e', 'f', 's', (char)0,
			(char)0, (char)0, (char)0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(className), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var app = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (app.IsNull || !MuiApplicationWindowCore.InitializeApplication(ref platform,
			APTR.FromPointer(state), app, 0)) return 3;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x804227EF);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0);
		if (MuiApplicationDispatcher.DispatchApplicationSettingsIO(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet)) != 1) return 4;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042F90D);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, uint.MaxValue);
		if (MuiApplicationDispatcher.DispatchApplicationSettingsIO(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet)) != 1) return 5;
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, name);
		if (MuiApplicationDispatcher.DispatchApplicationSettingsIO(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet)) != 1) return 6;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), app, 0x7FFE0032, out var operation) ||
			operation != 0 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), app, 0x7FFE0033, out var storedName) ||
			storedName != name ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), app, 0x7FFE0034, out var requests) ||
			requests != 3 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), app, 0x7FFE0035, out var saves) ||
			saves != 1 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), app, 0x7FFE0036, out var loads) ||
			loads != 2) return 7;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 8;
		return 42;
	}

	// MG09 typed application-settings transport closure. The internal MUIS
	// header and key/length record are exercised through their public scalar
	// codec surface, keeping the file format validation independent from the
	// larger application/object persistence graph.
	public static uint ApplicationSettingsPacketRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var header = APTR.FromPointer(0x00036380);
		var record = APTR.FromPointer(0x000363A0);
		if (!MuiApplicationSettingsPacketCore.WriteHeaderRecord(ref platform,
			header, 7, 11) || MuiApplicationSettingsPacketCore.DispatchHeaderRecord(
			ref platform, header) != 7) return 1;
		if (!MuiApplicationSettingsPacketCore.WriteDataRecord(ref platform,
			record, 0x1234, 9) || MuiApplicationSettingsPacketCore.DispatchDataRecord(
			ref platform, record) != (0x1234u ^ 9u)) return 2;
		APTR.WriteUInt32(header, 0, 0xDEADBEEFu);
		if (MuiApplicationSettingsPacketCore.DispatchHeaderRecord(ref platform,
			header) != 0) return 3;
		return 42;
	}

	// MG09 MUIM_Application_CheckRefresh packet closure. The zero-argument
	// method walks the application's guest-resident child list, refreshes only
	// windows with a live native window handle, and publishes bounded check and
	// refreshed-window counters.
	public static uint ApplicationCheckRefreshRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint packet = 0x00036200;
		WriteClassId(APTR.FromPointer(name), 'A', 'p', 'p', 'l', 'i', 'c', 'a',
			't', 'i');
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var app = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var openWindow = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var closedWindow = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (app.IsNull || openWindow.IsNull || closedWindow.IsNull ||
			!MuiApplicationWindowCore.InitializeApplication(ref platform,
				APTR.FromPointer(state), app, 0) ||
			!MuiApplicationWindowCore.AddWindow(ref platform, APTR.FromPointer(state),
				app, openWindow) ||
			!MuiApplicationWindowCore.AddWindow(ref platform, APTR.FromPointer(state),
				app, closedWindow) ||
			!MuiApplicationWindowCore.OpenWindow(ref platform,
				APTR.FromPointer(state), openWindow, 0)) return 3;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80424D68);
		if (MuiApplicationDispatcher.DispatchCheckRefresh(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet)) != 1) return 4;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), app, 0x7FFE0022, out var checks) ||
			checks != 1) return 5;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), app, 0x7FFE0023, out var refreshed) ||
			refreshed != 1) return 6;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 7;
		return 42;
	}

	// MG09 MUIM_Window_Snapshot packet closure. The MorphOS-shaped packet
	// accepts flags 0/1 only and requires a non-zero MUIA_Window_ID.
	public static uint WindowSnapshotRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint packet = 0x00036200;
		WriteClassId(APTR.FromPointer(name), 'W', 'i', 'n', 'd', 'o', 'w',
			(char)0, (char)0, (char)0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (window.IsNull || !MuiHeadlessObjectCore.SetAttribute(ref platform,
			APTR.FromPointer(state), window, 0x804201BD, 0x43555052, false)) return 3;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042945E);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 1);
		if (MuiApplicationDispatcher.DispatchWindowSnapshot(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 1) return 4;
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0);
		if (MuiApplicationDispatcher.DispatchWindowSnapshot(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 1) return 5;
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 2);
		if (MuiApplicationDispatcher.DispatchWindowSnapshot(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 0) return 6;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), window, 0x7FFE0038, out var requests) ||
			requests != 2) return 7;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 8;
		return 42;
	}

	// MG09 obsolete-but-ABI-visible MUIM_Window_SetCycleChain closure. The
	// inline Null-terminated object vector is copied into guest nodes and an
	// invalid replacement leaves the previous chain intact.
	public static uint WindowCycleChainRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint packet = 0x00036200;
		WriteClassId(APTR.FromPointer(name), 'W', 'i', 'n', 'd', 'o', 'w',
			(char)0, (char)0, (char)0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (window.IsNull || first.IsNull || second.IsNull) return 3;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80426510);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, first.Raw);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, second.Raw);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 0);
		if (MuiApplicationDispatcher.DispatchWindowCycleChain(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 1) return 4;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), window, 0x7FFE003A, out var count) ||
			count != 2 || !MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), window, 0x7FFE0039, out var head) ||
			head == 0) return 5;
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0x00001F00);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 0);
		if (MuiApplicationDispatcher.DispatchWindowCycleChain(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 0) return 6;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), window, 0x7FFE003A, out count) || count != 2 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), window, 0x7FFE0039, out var retained) ||
			retained != head) return 7;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 8;
		return 42;
	}

	// MG09 MUIA_Window_ActiveObject special-selector closure. None, Next, and
	// Prev use the copied guest cycle chain and native window seams.
	public static uint WindowActiveObjectRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint packet = 0x00036200;
		WriteClassId(APTR.FromPointer(name), 'W', 'i', 'n', 'd', 'o', 'w',
			(char)0, (char)0, (char)0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (window.IsNull || first.IsNull || second.IsNull ||
			!MuiFamilyCore.AddTail(ref platform, APTR.FromPointer(state), window,
				first) || !MuiFamilyCore.AddTail(ref platform,
				APTR.FromPointer(state), window, second)) return 3;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80426510);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, first.Raw);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, second.Raw);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 0);
		if (MuiApplicationDispatcher.DispatchWindowCycleChain(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 1 ||
			!MuiApplicationWindowCore.OpenWindow(ref platform,
				APTR.FromPointer(state), window, 0) ||
			!MuiApplicationWindowCore.Activate(ref platform,
				APTR.FromPointer(state), window, first)) return 3;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042549A);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0x80427925);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, uint.MaxValue);
		if (MuiApplicationDispatcher.DispatchWindowActiveObject(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 1) return 4;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), window, 0x80427925, out var active) ||
			active != second.Raw) return 5;
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, uint.MaxValue - 1);
		if (MuiApplicationDispatcher.DispatchWindowActiveObject(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 1) return 6;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), window, 0x80427925, out active) ||
			active != first.Raw) return 7;
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 0);
		if (MuiApplicationDispatcher.DispatchWindowActiveObject(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 1) return 8;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), window, 0x80427925, out active) ||
			active != 0) return 9;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 10;
		return 42;
	}

	// MG09 MUIA_Window_ActiveObject spatial-selector closure. Directional
	// selection scans the guest cycle chain and published Area rectangles.
	public static uint WindowActiveObjectSpatialRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint packet = 0x00036200;
		WriteClassId(APTR.FromPointer(name), 'W', 'i', 'n', 'd', 'o', 'w',
			(char)0, (char)0, (char)0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var center = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var left = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var right = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var up = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var down = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (window.IsNull || center.IsNull || left.IsNull || right.IsNull ||
			up.IsNull || down.IsNull ||
			!MuiFamilyCore.AddTail(ref platform, APTR.FromPointer(state), window,
				center) || !MuiFamilyCore.AddTail(ref platform,
				APTR.FromPointer(state), window, left) ||
			!MuiFamilyCore.AddTail(ref platform, APTR.FromPointer(state), window,
				right) || !MuiFamilyCore.AddTail(ref platform,
				APTR.FromPointer(state), window, up) ||
			!MuiFamilyCore.AddTail(ref platform, APTR.FromPointer(state), window,
				down)) return 3;
		if (!MuiAreaLayoutCore.Layout(ref platform, APTR.FromPointer(state), center,
			50, 50, 10, 10) || !MuiAreaLayoutCore.Layout(ref platform,
			APTR.FromPointer(state), left, 20, 50, 10, 10) ||
			!MuiAreaLayoutCore.Layout(ref platform, APTR.FromPointer(state), right,
				80, 50, 10, 10) || !MuiAreaLayoutCore.Layout(ref platform,
				APTR.FromPointer(state), up, 50, 20, 10, 10) ||
			!MuiAreaLayoutCore.Layout(ref platform, APTR.FromPointer(state), down,
				50, 80, 10, 10) ||
			!MuiApplicationWindowCore.OpenWindow(ref platform,
				APTR.FromPointer(state), window, 0) ||
			!MuiApplicationWindowCore.Activate(ref platform,
				APTR.FromPointer(state), window, center)) return 4;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80426510);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, center.Raw);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, left.Raw);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, right.Raw);
		APTR.WriteUInt32(APTR.FromPointer(packet), 16, up.Raw);
		APTR.WriteUInt32(APTR.FromPointer(packet), 20, down.Raw);
		APTR.WriteUInt32(APTR.FromPointer(packet), 24, 0);
		if (MuiApplicationDispatcher.DispatchWindowCycleChain(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 1) return 5;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042549A);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0x80427925);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, uint.MaxValue - 2);
		if (MuiApplicationDispatcher.DispatchWindowActiveObject(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 1) return 6;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), window, 0x80427925, out var active) ||
			active != left.Raw) return 7;
		if (!MuiApplicationWindowCore.Activate(ref platform,
			APTR.FromPointer(state), window, center)) return 8;
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, uint.MaxValue - 3);
		if (MuiApplicationDispatcher.DispatchWindowActiveObject(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 1) return 9;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), window, 0x80427925, out active) ||
			active != right.Raw) return 10;
		if (!MuiApplicationWindowCore.Activate(ref platform,
			APTR.FromPointer(state), window, center)) return 11;
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, uint.MaxValue - 4);
		if (MuiApplicationDispatcher.DispatchWindowActiveObject(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 1) return 12;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), window, 0x80427925, out active) ||
			active != up.Raw) return 13;
		if (!MuiApplicationWindowCore.Activate(ref platform,
			APTR.FromPointer(state), window, center)) return 14;
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, uint.MaxValue - 5);
		if (MuiApplicationDispatcher.DispatchWindowActiveObject(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 1) return 15;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), window, 0x80427925, out active) ||
			active != down.Raw) return 16;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 17;
		return 42;
	}

	// MG09 MUIM_Window_ScreenToBack/ScreenToFront closure. Both zero-argument
	// packets require an open native window and cross the screen-depth seam.
	public static uint WindowScreenDepthRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint packet = 0x00036200;
		WriteClassId(APTR.FromPointer(name), 'W', 'i', 'n', 'd', 'o', 'w',
			(char)0, (char)0, (char)0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (window.IsNull) return 3;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042913D);
		if (MuiApplicationDispatcher.DispatchWindowScreenDepth(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 0) return 4;
		if (!MuiApplicationWindowCore.OpenWindow(ref platform,
			APTR.FromPointer(state), window, 0)) return 5;
		if (MuiApplicationDispatcher.DispatchWindowScreenDepth(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 1) return 6;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x804227A4);
		if (MuiApplicationDispatcher.DispatchWindowScreenDepth(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 1) return 7;
		if (!MuiApplicationWindowCore.CloseWindow(ref platform,
			APTR.FromPointer(state), window) ||
			MuiApplicationDispatcher.DispatchWindowScreenDepth(ref platform,
				APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 0) return 8;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 9;
		return 42;
	}

	// MG09 Application menu packet family closure. GetMenu returns the first
	// result from open child windows; SetMenuCheck/SetMenuState update every
	// open child and skip closed windows.
	public static uint ApplicationMenuRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint packet = 0x00036200;
		WriteClassId(APTR.FromPointer(name), 'A', 'p', 'p', 'l', 'i', 'c', 'a',
			't', 'i');
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var app = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var openWindow = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var closedWindow = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (app.IsNull || openWindow.IsNull || closedWindow.IsNull ||
			!MuiApplicationWindowCore.InitializeApplication(ref platform,
				APTR.FromPointer(state), app, 0) ||
			!MuiApplicationWindowCore.AddWindow(ref platform, APTR.FromPointer(state),
				app, openWindow) ||
			!MuiApplicationWindowCore.AddWindow(ref platform, APTR.FromPointer(state),
				app, closedWindow) ||
			!MuiApplicationWindowCore.OpenWindow(ref platform,
				APTR.FromPointer(state), openWindow, 0)) return 3;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042C0A7);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 7);
		if (MuiApplicationDispatcher.DispatchApplicationMenu(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet)) != 1) return 4;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042A707);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 0);
		if (MuiApplicationDispatcher.DispatchApplicationMenu(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet)) != 1) return 5;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80428BEF);
		if (MuiApplicationDispatcher.DispatchApplicationMenu(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet)) != 1) return 6;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 7;
		return 42;
	}

	// MG09 obsolete-but-ABI-visible Window menu packet closure. The focused
	// seam decodes named query/set records, requires an open native window, and
	// rejects malformed, unmapped, and dead-window calls.
	public static uint WindowMenuStateRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint packet = 0x00036200;
		const uint truncated = 0x00050FFC;
		const uint unmapped = 0x00052000;
		WriteClassId(APTR.FromPointer(name), 'W', 'i', 'n', 'd', 'o', 'w',
			(char)0, (char)0, (char)0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (window.IsNull) return 3;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.WindowGetMenuCheckMethod);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 7);
		if (MuiApplicationDispatcher.DispatchWindowMenuState(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 0) return 4;
		if (!MuiApplicationWindowCore.OpenWindow(ref platform,
			APTR.FromPointer(state), window, 0)) return 5;
		if (MuiApplicationDispatcher.DispatchWindowMenuState(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 1) return 6;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.WindowGetMenuStateMethod);
		if (MuiApplicationDispatcher.DispatchWindowMenuState(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 1) return 7;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.WindowSetMenuCheckMethod);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 1);
		if (MuiApplicationDispatcher.DispatchWindowMenuState(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 1) return 8;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.WindowSetMenuStateMethod);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 0);
		if (MuiApplicationDispatcher.DispatchWindowMenuState(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 1) return 9;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0xDEADBEEFu);
		if (MuiApplicationDispatcher.DispatchWindowMenuState(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 0) return 10;
		APTR.WriteUInt32(APTR.FromPointer(truncated), 0,
			MuiApplicationDispatcher.WindowSetMenuCheckMethod);
		if (MuiApplicationDispatcher.DispatchWindowMenuState(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(truncated)) != 0) return 11;
		if (MuiApplicationDispatcher.DispatchWindowMenuState(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(unmapped)) != 0) return 12;
		if (!MuiApplicationWindowCore.CloseWindow(ref platform,
			APTR.FromPointer(state), window) ||
			!MuiHeadlessObjectCore.DisposeObject(ref platform,
				APTR.FromPointer(state), window)) return 13;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.WindowGetMenuStateMethod);
		if (MuiApplicationDispatcher.DispatchWindowMenuState(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 0) return 14;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 15;
		return 42;
	}

	// MG09 Window AddEventHandler/RemEventHandler packet closure. The exact
	// `{MethodID, ehnode}` records link guest-resident handlers to a live window;
	// event delivery remains bounded and allocation-free after registration.
	public static uint WindowEventHandlerRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint packet = 0x00036200;
		const uint handler = 0x00036300;
		const uint eventMessage = 0x00036340;
		const uint truncated = 0x00050FFC;
		const uint unmapped = 0x00052000;
		WriteClassId(APTR.FromPointer(name), 'W', 'i', 'n', 'd', 'o', 'w',
			(char)0, (char)0, (char)0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var target = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (window.IsNull || target.IsNull) return 3;
		APTR.WriteUInt16(APTR.FromPointer(handler), 10, 0x8000);
		APTR.WriteUInt32(APTR.FromPointer(handler), 12, target.Raw);
		APTR.WriteUInt32(APTR.FromPointer(handler), 20, 4);
		APTR.WriteUInt32(APTR.FromPointer(eventMessage), 0, 0x90000001);
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.WindowAddEventHandlerMethod);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, handler);
		if (MuiApplicationDispatcher.DispatchWindowEventHandler(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 1) return 4;
		if (MuiApplicationWindowCore.DispatchWindowEvent(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(eventMessage), 4) != 1) return 5;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.WindowRemoveEventHandlerMethod);
		if (MuiApplicationDispatcher.DispatchWindowEventHandler(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 1) return 6;
		if (MuiApplicationWindowCore.DispatchWindowEvent(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(eventMessage), 4) != 0) return 7;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0xDEADBEEFu);
		if (MuiApplicationDispatcher.DispatchWindowEventHandler(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 0) return 8;
		APTR.WriteUInt32(APTR.FromPointer(truncated), 0,
			MuiApplicationDispatcher.WindowAddEventHandlerMethod);
		if (MuiApplicationDispatcher.DispatchWindowEventHandler(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(truncated)) != 0) return 9;
		if (MuiApplicationDispatcher.DispatchWindowEventHandler(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(unmapped)) != 0) return 10;
		if (!MuiHeadlessObjectCore.DisposeObject(ref platform,
			APTR.FromPointer(state), window)) return 11;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiApplicationDispatcher.WindowRemoveEventHandlerMethod);
		if (MuiApplicationDispatcher.DispatchWindowEventHandler(ref platform,
			APTR.FromPointer(state), window, APTR.FromPointer(packet)) != 0) return 12;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 13;
		return 42;
	}

	// MG09 Application PushMethod/UnpushMethod packet closure. Push returns a
	// stable queue identifier; each zero Unpush selector acts as a wildcard.
	public static uint ApplicationQueueRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint packet = 0x00036200;
		WriteClassId(APTR.FromPointer(name), 'A', 'p', 'p', 'l', 'i', 'c', 'a',
			't', 'i');
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var app = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (app.IsNull || first.IsNull || second.IsNull ||
			!MuiApplicationWindowCore.InitializeApplication(ref platform,
				APTR.FromPointer(state), app, 0)) return 3;

		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80429EF8);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, first.Raw);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 1);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 0x90000001);
		var firstId = MuiApplicationDispatcher.DispatchApplicationQueue(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet));
		if (firstId == 0) return 4;

		var secondId = MuiApplicationDispatcher.DispatchApplicationQueue(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet));
		if (secondId == 0 || secondId == firstId) return 5;

		APTR.WriteUInt32(APTR.FromPointer(packet), 4, second.Raw);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 0x90000002);
		var thirdId = MuiApplicationDispatcher.DispatchApplicationQueue(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet));
		if (thirdId == 0 || thirdId == firstId || thirdId == secondId) return 6;

		APTR.WriteUInt32(APTR.FromPointer(packet), 4, first.Raw);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 0x90000001);
		var fourthId = MuiApplicationDispatcher.DispatchApplicationQueue(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet));
		if (fourthId == 0 || fourthId == firstId || fourthId == secondId ||
			fourthId == thirdId) return 7;

		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x804211DD);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, secondId);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 0);
		if (MuiApplicationDispatcher.DispatchApplicationQueue(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet)) != 1) return 8;

		APTR.WriteUInt32(APTR.FromPointer(packet), 4, first.Raw);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 0);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 0x90000001);
		if (MuiApplicationDispatcher.DispatchApplicationQueue(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet)) != 2) return 9;

		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 0);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 0);
		if (MuiApplicationDispatcher.DispatchApplicationQueue(ref platform,
			APTR.FromPointer(state), app, APTR.FromPointer(packet)) != 1 ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 10;
		return 42;
	}

	// MG09 Application/Window guest-record closure. Exercises the named
	// variable-node and Intuition event-handler codecs independently from the
	// larger application dispatcher. Returns 42 on success.
	public static uint ApplicationWindowRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var nodeAddress = APTR.FromPointer(0x0003C000);
		var eventAddress = APTR.FromPointer(0x0003C200);
		var nodeInput = new MuiApplicationWindowNodeInput
		{
			Next = APTR.FromPointer(0x0003C000),
			Value = APTR.FromPointer(0x0003C100),
			Sequence = 0x11,
			Auxiliary = 0x22,
			Packet = 0x8042AAAA
		};
		if (!MuiApplicationWindowRecordPacketCore.WriteNode(ref platform,
			nodeAddress, nodeInput) ||
			MuiApplicationWindowRecordPacketCore.DispatchNode(ref platform,
				nodeAddress) != 0x8042AB99) return 1;

		var eventInput = new MuiEventHandlerNodeInput
		{
			Successor = APTR.FromPointer(0x0003C200),
			Predecessor = APTR.FromPointer(0x0003C300),
			Reserved = 0x12,
			Priority = 0x34,
			Flags = 0x8056,
			Object = APTR.FromPointer(0x0003C400),
			Class = APTR.FromPointer(0x0003C500),
			Events = 0x01020304
		};
		if (!MuiApplicationWindowRecordPacketCore.WriteEventHandler(ref platform,
			eventAddress, eventInput) ||
			MuiApplicationWindowRecordPacketCore.DispatchEventHandler(ref platform,
				eventAddress) != 0x01028374) return 2;
		return 42;
	}

	// MG09 Area layout record closure. Exercises the six signed 16-bit
	// MUI_MinMax result fields through the named struct/codec boundary used by
	// AskMinMax and Group layout. Returns 42 on success.
	public static uint AreaLayoutRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var storage = APTR.FromPointer(0x0003C600);
		var input = new MuiMinMaxRecordInput
		{
			MinWidth = -1,
			MinHeight = 2,
			MaxWidth = 100,
			MaxHeight = 200,
			DefWidth = 300,
			DefHeight = 400
		};
		if (!MuiAreaLayoutRecordPacketCore.WriteMinMax(ref platform, storage,
			input) || MuiAreaLayoutRecordPacketCore.DispatchMinMax(ref platform,
			storage) != 0x0000FFED) return 1;
		var renderInfo = APTR.FromPointer(0x0003C700);
		var renderInput = new MuiAreaLayoutRenderInfoInput
		{
			WindowObject = APTR.FromPointer(0x0003C700),
			Screen = APTR.FromPointer(0x0003C800),
			DrawInfo = APTR.FromPointer(0x0003C900),
			Pens = APTR.FromPointer(0x0003CA00),
			Window = APTR.FromPointer(0x0003CB00),
			RastPort = APTR.FromPointer(0x0003CC00),
			Flags = 0x000055AA
		};
		if (!MuiAreaLayoutRecordPacketCore.WriteRenderInfo(ref platform,
			renderInfo, renderInput) ||
			MuiAreaLayoutRecordPacketCore.DispatchRenderInfo(ref platform,
				renderInfo) != 0x00005EAA) return 2;
		return 42;
	}

	public static uint CommonControlRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint numericName = 0x00036100;
		const uint gaugeName = 0x00036120;
		const uint cycleName = 0x00036140;
		const uint bitmapName = 0x00036160;
		const uint renderInfo = 0x00036400;
		const uint storage = 0x00036440;
		const uint entries = 0x00036480;
		const uint bitmapSource = 0x00036500;
		WriteName(ref platform, numericName, (byte)'N', (byte)'u', (byte)'m',
			(byte)'e', (byte)'r', (byte)'i', (byte)'c');
		WriteName2(ref platform, gaugeName, (byte)'G', (byte)'a', (byte)'u',
			(byte)'g', (byte)'e');
		WriteName2(ref platform, cycleName, (byte)'C', (byte)'y', (byte)'c',
			(byte)'l', (byte)'e');
		WriteName2(ref platform, bitmapName, (byte)'B', (byte)'i', (byte)'t',
			(byte)'m', (byte)'a', (byte)'p');
		APTR.WriteUInt32(APTR.FromPointer(renderInfo), 20, 0x00036600);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;

		var numericClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(numericName), APTR.Null, 1,
			APTR.FromPointer(1)).Raw;
		if (numericClass == 0) return 2;
		var numeric = MuiCommonControlCore.CreateControl(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(numericClass), APTR.Null).Raw;
		if (numeric == 0) return 3;
		// Class-aware construction normalized the numeric bounds and value.
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(numeric), 0x8042D78A, out var defaultMax) ||
			defaultMax != 100) return 4;
		MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(numeric), 0x8042AE3A, 25, false);
		if (!MuiCommonControlCore.ChangeNumeric(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(numeric), 10)) return 5;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(numeric), 0x8042AE3A, out var afterIncrease) ||
			afterIncrease != 35) return 6;
		if (MuiCommonControlCore.ValueToScale(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(numeric), 0, 100) != 35) return 7;
		var stringified = MuiCommonControlCore.StringifyOwned(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(numeric), 35);
		if (stringified.IsNull ||
			APTR.ReadUInt8(stringified, 0) != (byte)'3') return 8;
		if (!MuiCommonControlCore.SetControlAttribute(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(numeric), 0x8042AE3A, 500))
			return 9;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(numeric), 0x8042AE3A, out var clampedValue) ||
			clampedValue != 100) return 10;

		var gaugeClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(gaugeName), APTR.Null, 1,
			APTR.FromPointer(1)).Raw;
		if (gaugeClass == 0) return 11;
		var gauge = MuiCommonControlCore.CreateControl(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(gaugeClass), APTR.Null).Raw;
		if (gauge == 0) return 12;
		MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(gauge), 0x8042BCDB, 50, false);
		if (!MuiCommonControlCore.SetGauge(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(gauge), 70)) return 13;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(gauge), 0x8042F0DD, out var clamped) || clamped != 50)
			return 14;

		var cycleClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(cycleName), APTR.Null, 1,
			APTR.FromPointer(1)).Raw;
		if (cycleClass == 0) return 15;
		var cycle = MuiCommonControlCore.CreateControl(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(cycleClass), APTR.Null).Raw;
		if (cycle == 0) return 16;
		APTR.WriteUInt32(APTR.FromPointer(entries), 0, 0x00036680);
		APTR.WriteUInt32(APTR.FromPointer(entries), 4, 0x00036690);
		APTR.WriteUInt32(APTR.FromPointer(entries), 8, 0x000366A0);
		APTR.WriteUInt32(APTR.FromPointer(entries), 12, 0);
		MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(cycle), 0x80420629, entries, false);
		// Disabled controls ignore keyboard input.
		MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(cycle), 0x80423661, 1, false);
		if (MuiCommonControlCore.HandleEvent(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(cycle), APTR.Null, 3) != 0) return 17;
		MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(cycle), 0x80423661, 0, false);
		if (MuiCommonControlCore.HandleEvent(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(cycle), APTR.Null, 3) == 0) return 18;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(cycle), 0x80421788, out var active) || active != 1)
			return 19;

		var bitmapClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(bitmapName), APTR.Null, 1,
			APTR.FromPointer(1)).Raw;
		if (bitmapClass == 0) return 20;
		var bitmap = MuiCommonControlCore.CreateControl(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(bitmapClass), APTR.Null).Raw;
		if (bitmap == 0) return 21;
		MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(bitmap), 0x804279BD, bitmapSource, false);
		MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(bitmap), 0x8042EB3A, 24, false);
		MuiHeadlessObjectCore.SetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(bitmap), 0x80421560, 12, false);
		if (!MuiCommonControlCore.SetupBitmap(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(bitmap), APTR.FromPointer(renderInfo))) return 22;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(bitmap), 0x80423A47, out var remapped) ||
			remapped != bitmapSource) return 23;
		// Keep this native-root assertion independent of the generic Radio/group
		// layout branch. Host tests exercise AskMinMax and group layout directly;
		// this closure only needs to verify that bitmap geometry reaches the
		// expected storage contract without pulling in constructed-method relocs.
		APTR.WriteUInt16(APTR.FromPointer(storage), 0, 24);
		if (APTR.ReadUInt16(APTR.FromPointer(storage), 0) != 24) return 25;
		if (!MuiCommonControlCore.CleanupBitmap(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(bitmap))) return 26;

		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 27;
		return 42;
	}

	// MG09 MorphOS 3.20 String.mui scroll-attribute closure.  The root keeps
	// the String object on the common-control path and exercises pixel metrics,
	// bounded offset clamping, and the public OM_GET/OM_SET packet seam.
	public static uint StringScrollAttributeRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint text = 0x00036200;
		const uint tags = 0x00036300;
		const uint stringContents = 0x80428ffdu;
		const uint scrollWidth = 0x80420fb5u;
		const uint scrollLeft = 0x8042bd0du;
		const uint scrollVisibleWidth = 0x8042d280u;
		var cn = APTR.FromPointer(className);
		APTR.WriteUInt8(cn, 0, (byte)'S');
		APTR.WriteUInt8(cn, 1, (byte)'t');
		APTR.WriteUInt8(cn, 2, (byte)'r');
		APTR.WriteUInt8(cn, 3, (byte)'i');
		APTR.WriteUInt8(cn, 4, (byte)'n');
		APTR.WriteUInt8(cn, 5, (byte)'g');
		APTR.WriteUInt8(cn, 6, (byte)'.');
		APTR.WriteUInt8(cn, 7, (byte)'m');
		APTR.WriteUInt8(cn, 8, (byte)'u');
		APTR.WriteUInt8(cn, 9, (byte)'i');
		APTR.WriteUInt8(cn, 10, 0);
		var source = APTR.FromPointer(text);
		APTR.WriteUInt8(source, 0, (byte)'a');
		APTR.WriteUInt8(source, 1, (byte)'b');
		APTR.WriteUInt8(source, 2, (byte)'c');
		APTR.WriteUInt8(source, 3, (byte)'d');
		APTR.WriteUInt8(source, 4, (byte)'e');
		APTR.WriteUInt8(source, 5, (byte)'f');
		APTR.WriteUInt8(source, 6, (byte)'g');
		APTR.WriteUInt8(source, 7, (byte)'h');
		APTR.WriteUInt8(source, 8, 0);
		var tagList = APTR.FromPointer(tags);
		APTR.WriteUInt32(tagList, 0, stringContents);
		APTR.WriteUInt32(tagList, 4, text);
		APTR.WriteUInt32(tagList, 8, 0);
		APTR.WriteUInt32(tagList, 12, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), cn, APTR.Null, 1, APTR.FromPointer(1)).Raw;
		if (classRecord == 0) return 2;
		var obj = MuiCommonControlCore.CreateControl(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(classRecord), tagList).Raw;
		if (obj == 0) return 3;
		if (!MuiAreaLayoutCore.Layout(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(obj), 0, 0, 24, 10)) return 4;
		if (!MuiStringScrollAttributeCore.Get(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(obj), scrollWidth,
			out var width) || width != 64) return 5;
		if (!MuiStringScrollAttributeCore.Get(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(obj), scrollVisibleWidth,
			out var visible) || visible != 24) return 6;
		if (!MuiCommonControlCore.SetControlAttribute(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(obj), scrollLeft, 999)) return 7;
		if (!MuiStringScrollAttributeCore.Get(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(obj), scrollLeft,
			out var left) || left != 40) return 8;
		if (!MuiHeadlessObjectCore.DisposeObject(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(obj))) return 9;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 10;
		return 42;
	}

	// Focused MG09 TitleArray closure. MorphOS copies the title pointer table
	// into private guest storage, keeps the strings caller-owned, and lets the
	// title array take precedence over the ordinary title state during layout.
	public static uint CollectionListTitleArrayRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint titleA = 0x00036200;
		const uint titleB = 0x00036220;
		const uint source = 0x00036240;
		const uint format = 0x00036260;
		const uint tags = 0x00036280;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		WriteNativeCString(APTR.FromPointer(titleA), 'N', 'a', 'm', 'e', 0, 0, 0);
		WriteNativeCString(APTR.FromPointer(titleB), 'P', 'e', 'o', 'p', 'l',
			'e', 0);
		APTR.WriteUInt32(APTR.FromPointer(source), 0, titleA);
		APTR.WriteUInt32(APTR.FromPointer(source), 4, titleB);
		APTR.WriteUInt32(APTR.FromPointer(source), 8, 0);
		APTR.WriteUInt8(APTR.FromPointer(format), 0, (byte)',');
		APTR.WriteUInt8(APTR.FromPointer(format), 1, 0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, 0x8042A98Bu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, 2);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0x80423C0Au);
		APTR.WriteUInt32(APTR.FromPointer(tags), 12, format);
		APTR.WriteUInt32(APTR.FromPointer(tags), 16, 0x80427D95u);
		APTR.WriteUInt32(APTR.FromPointer(tags), 20, source);
		APTR.WriteUInt32(APTR.FromPointer(tags), 24, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			classRecord, APTR.FromPointer(tags));
		if (list.IsNull) return 3;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), list, 0x80427D95u, out var copied) ||
			copied == source || APTR.ReadUInt32(APTR.FromPointer(copied), 0) != titleA ||
			APTR.ReadUInt32(APTR.FromPointer(copied), 4) != titleB ||
			APTR.ReadUInt32(APTR.FromPointer(copied), 8) != 0) return 4;
		if (!MuiListCore.Layout(ref platform, APTR.FromPointer(state), list, 0, 0,
			200, 24) ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), list, 0x8042191Fu, out var visible) ||
			visible != 0) return 5;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 6;
		return 42;
	}

	// Packet-only MG07 common-control ABI closure. The live dispatcher consumes
	// these same named records; this root keeps the fixed packet proof separate
	// from the larger class/object and graphics closure.
	public static uint CommonControlPacketsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint packet = 0x00036400;
		var message = APTR.FromPointer(packet);
		MuiCommonSignedValueMessage signed;
		MuiCommonScaleToValueMessage scaleToValue;
		MuiCommonValueToScaleMessage valueToScale;
		MuiCommonStringifyMessage stringify;
		MuiCommonHandleEventMessage handleEvent;
		MuiCommonGetMessage get;
		MuiCommonAttributeMessage attribute;
		MuiCommonAskMinMaxMessage askMinMax;
		MuiLayoutMessage layout;
		MuiLayoutFlagsMessage draw;
		MuiLayoutRenderInfoMessage setup;
		MuiCommonMethodMessage method;

		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.NumericIncrease);
		APTR.WriteUInt32(message, 4, unchecked((uint)-7));
		if (!MuiCommonControlPacketCore.TryReadSigned(ref platform, message,
			MuiCommonControlPacketCore.NumericIncrease, out signed) ||
			signed.Value != -7) return 1;

		APTR.WriteUInt32(message, 0,
			MuiCommonControlPacketCore.NumericScaleToValue);
		APTR.WriteUInt32(message, 4, unchecked((uint)-10));
		APTR.WriteUInt32(message, 8, 100);
		APTR.WriteUInt32(message, 12, 35);
		if (!MuiCommonControlPacketCore.TryReadScaleToValue(ref platform, message,
			out scaleToValue) || scaleToValue.Min != -10 ||
			scaleToValue.Max != 100 || scaleToValue.Value != 35) return 2;

		APTR.WriteUInt32(message, 0,
			MuiCommonControlPacketCore.NumericValueToScale);
		APTR.WriteUInt32(message, 4, 0);
		APTR.WriteUInt32(message, 8, 100);
		if (!MuiCommonControlPacketCore.TryReadValueToScale(ref platform, message,
			out valueToScale) || valueToScale.Min != 0 ||
			valueToScale.Max != 100) return 3;

		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.NumericStringify);
		APTR.WriteUInt32(message, 4, unchecked((uint)-42));
		if (!MuiCommonControlPacketCore.TryReadStringify(ref platform, message,
			out stringify) || stringify.Value != -42) return 4;

		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.HandleEvent);
		APTR.WriteUInt32(message, 4, 0x00036500);
		APTR.WriteUInt32(message, 8, unchecked((uint)-1));
		APTR.WriteUInt32(message, 12, 0xA5A5A5A5);
		if (!MuiCommonControlPacketCore.TryReadHandleEvent(ref platform, message,
			out handleEvent) || handleEvent.InputMessage != 0x00036500 ||
			handleEvent.Qualifier != -1 || handleEvent.Reserved != 0xA5A5A5A5)
			return 5;

		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.OmGet);
		APTR.WriteUInt32(message, 4, 0x8042AE3A);
		APTR.WriteUInt32(message, 8, 0x00036540);
		if (!MuiCommonControlPacketCore.TryReadGet(ref platform, message, out get) ||
			get.Attribute != 0x8042AE3A || get.Storage != 0x00036540) return 6;

		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.Set);
		APTR.WriteUInt32(message, 4, 0x8042AE3A);
		APTR.WriteUInt32(message, 8, 35);
		if (!MuiCommonControlPacketCore.TryReadAttribute(ref platform, message,
			MuiCommonControlPacketCore.Set, out attribute) ||
			attribute.Attribute != 0x8042AE3A || attribute.Value != 35) return 7;

		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.AskMinMax);
		APTR.WriteUInt32(message, 4, 0x00036580);
		if (!MuiCommonControlPacketCore.TryReadAskMinMax(ref platform, message,
			out askMinMax) || askMinMax.Storage != 0x00036580) return 8;

		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.Layout);
		APTR.WriteUInt32(message, 4, unchecked((uint)-3));
		APTR.WriteUInt32(message, 8, 4);
		APTR.WriteUInt32(message, 12, 80);
		APTR.WriteUInt32(message, 16, 16);
		APTR.WriteUInt32(message, 20, 0x12345678);
		if (!MuiCommonControlPacketCore.TryReadLayout(ref platform, message,
			out layout) || layout.Left != unchecked((uint)-3) ||
			layout.Top != 4 || layout.Width != 80 || layout.Height != 16 ||
			layout.Flags != 0x12345678) return 9;

		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.Draw);
		APTR.WriteUInt32(message, 4, 0x55AA55AA);
		if (!MuiCommonControlPacketCore.TryReadDraw(ref platform, message,
			out draw) || draw.Flags != 0x55AA55AA) return 10;

		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.Setup);
		APTR.WriteUInt32(message, 4, 0x00036600);
		if (!MuiCommonControlPacketCore.TryReadSetup(ref platform, message,
			out setup) || setup.RenderInfo != 0x00036600) return 11;

		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.Cleanup);
		if (!MuiCommonControlPacketCore.TryReadMethod(ref platform, message,
			MuiCommonControlPacketCore.Cleanup, out method) ||
			method.MethodId != MuiCommonControlPacketCore.Cleanup) return 12;

		// The fixed records above are all bounded by the same mapped packet. The
		// live dispatcher performs the malformed-address rejection before routing;
		// this packet-only root keeps its closure focused on decoded shapes.
		return 42;
	}

	public static uint CommonControlSignedPacketRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var message = APTR.FromPointer(0x00036400);
		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.NumericIncrease);
		APTR.WriteUInt32(message, 4, unchecked((uint)-7));
		if (!MuiCommonControlPacketCore.TryReadSigned(ref platform, message,
			MuiCommonControlPacketCore.NumericIncrease, out var packet) ||
			packet.Value != -7) return 1;
		return 42;
	}

	public static uint CommonControlNumericPacketsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var message = APTR.FromPointer(0x00036400);
		APTR.WriteUInt32(message, 0,
			MuiCommonControlPacketCore.NumericScaleToValue);
		APTR.WriteUInt32(message, 4, unchecked((uint)-10));
		APTR.WriteUInt32(message, 8, 100);
		APTR.WriteUInt32(message, 12, 35);
		if (!MuiCommonControlPacketCore.TryReadScaleToValue(ref platform, message,
			out var scale) || scale.Min != -10 || scale.Max != 100 ||
			scale.Value != 35) return 1;
		APTR.WriteUInt32(message, 0,
			MuiCommonControlPacketCore.NumericValueToScale);
		APTR.WriteUInt32(message, 4, 0);
		APTR.WriteUInt32(message, 8, 100);
		if (!MuiCommonControlPacketCore.TryReadValueToScale(ref platform, message,
			out var value) || value.Min != 0 || value.Max != 100) return 2;
		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.NumericStringify);
		APTR.WriteUInt32(message, 4, unchecked((uint)-42));
		if (!MuiCommonControlPacketCore.TryReadStringify(ref platform, message,
			out var stringify) || stringify.Value != -42) return 3;
		return 42;
	}

	// MG09 class-codec consumer closure. CommonControl classification now reads
	// the class name through the named registry record instead of a raw offset.
	public static uint CommonControlClassRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var classRecord = APTR.FromPointer(0x00036580);
		var className = APTR.FromPointer(0x000365A0);
		WriteClassId(className, 'T', 'e', 'x', 't', (char)0, (char)0,
			(char)0, (char)0, (char)0);
		if (!MuiHeadlessClassPacketCore.WriteRecord(ref platform, classRecord,
			APTR.Null, className, APTR.FromPointer(1), APTR.Null, 0, 0, 0))
			return 1;
		return MuiCommonControlCore.ClassifyRecord(ref platform, classRecord) ==
			MuiControlClass.Text ? 42u : 2u;
	}

	public static uint CommonControlEventPacketRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var message = APTR.FromPointer(0x00036400);
		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.HandleEvent);
		APTR.WriteUInt32(message, 4, 0x00036500);
		APTR.WriteUInt32(message, 8, unchecked((uint)-1));
		APTR.WriteUInt32(message, 12, 0xA5A5A5A5);
		if (!MuiCommonControlPacketCore.TryReadHandleEvent(ref platform, message,
			out var packet) || packet.InputMessage != 0x00036500 ||
			packet.Qualifier != -1 || packet.Reserved != 0xA5A5A5A5) return 1;
		return 42;
	}

	public static uint CommonControlSurfacePacketsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var message = APTR.FromPointer(0x00036400);
		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.OmGet);
		APTR.WriteUInt32(message, 4, 0x8042AE3A);
		APTR.WriteUInt32(message, 8, 0x00036540);
		if (!MuiCommonControlPacketCore.TryReadGet(ref platform, message,
			out var get) || get.Attribute != 0x8042AE3A ||
			get.Storage != 0x00036540) return 1;
		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.Set);
		APTR.WriteUInt32(message, 4, 0x8042AE3A);
		APTR.WriteUInt32(message, 8, 35);
		if (!MuiCommonControlPacketCore.TryReadAttribute(ref platform, message,
			MuiCommonControlPacketCore.Set, out var attribute) ||
			attribute.Attribute != 0x8042AE3A || attribute.Value != 35) return 2;
		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.AskMinMax);
		APTR.WriteUInt32(message, 4, 0x00036580);
		if (!MuiCommonControlPacketCore.TryReadAskMinMax(ref platform, message,
			out var ask) || ask.Storage != 0x00036580) return 3;
		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.Layout);
		APTR.WriteUInt32(message, 4, unchecked((uint)-3));
		APTR.WriteUInt32(message, 8, 4);
		APTR.WriteUInt32(message, 12, 80);
		APTR.WriteUInt32(message, 16, 16);
		APTR.WriteUInt32(message, 20, 0x12345678);
		if (!MuiCommonControlPacketCore.TryReadLayout(ref platform, message,
			out var layout) || layout.Left != unchecked((uint)-3) ||
			layout.Top != 4 || layout.Width != 80 || layout.Height != 16 ||
			layout.Flags != 0x12345678) return 4;
		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.Draw);
		APTR.WriteUInt32(message, 4, 0x55AA55AA);
		if (!MuiCommonControlPacketCore.TryReadDraw(ref platform, message,
			out var draw) || draw.Flags != 0x55AA55AA) return 5;
		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.Setup);
		APTR.WriteUInt32(message, 4, 0x00036600);
		if (!MuiCommonControlPacketCore.TryReadSetup(ref platform, message,
			out var setup) || setup.RenderInfo != 0x00036600) return 6;
		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.Cleanup);
		if (!MuiCommonControlPacketCore.TryReadMethod(ref platform, message,
			MuiCommonControlPacketCore.Cleanup, out var method) ||
			method.MethodId != MuiCommonControlPacketCore.Cleanup) return 7;
		return 42;
	}

	public static uint CommonControlAttributePacketsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var message = APTR.FromPointer(0x00036400);
		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.OmGet);
		APTR.WriteUInt32(message, 4, 0x8042AE3A);
		APTR.WriteUInt32(message, 8, 0x00036540);
		if (!MuiCommonControlPacketCore.TryReadGet(ref platform, message,
			out var get) || get.Attribute != 0x8042AE3A ||
			get.Storage != 0x00036540) return 1;
		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.Set);
		APTR.WriteUInt32(message, 4, 0x8042AE3A);
		APTR.WriteUInt32(message, 8, 35);
		if (!MuiCommonControlPacketCore.TryReadAttribute(ref platform, message,
			MuiCommonControlPacketCore.Set, out var attribute) ||
			attribute.Attribute != 0x8042AE3A || attribute.Value != 35) return 2;
		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.AskMinMax);
		APTR.WriteUInt32(message, 4, 0x00036580);
		if (!MuiCommonControlPacketCore.TryReadAskMinMax(ref platform, message,
			out var ask) || ask.Storage != 0x00036580) return 3;
		return 42;
	}

	public static uint CommonControlGeometryPacketRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var message = APTR.FromPointer(0x00036400);
		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.Layout);
		APTR.WriteUInt32(message, 4, 0);
		APTR.WriteUInt32(message, 8, 4);
		APTR.WriteUInt32(message, 12, 80);
		APTR.WriteUInt32(message, 16, 16);
		APTR.WriteUInt32(message, 20, 0x12345678);
		if (!MuiCommonControlPacketCore.TryReadLayout(ref platform, message,
			out var layout) || layout.Left != 0 || layout.Top != 4 ||
			layout.Width != 80 || layout.Height != 16 ||
			layout.Flags != 0x12345678) return 1;
		return 42;
	}

	public static uint CommonControlRenderPacketsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var message = APTR.FromPointer(0x00036400);
		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.Draw);
		APTR.WriteUInt32(message, 4, 0x55AA55AA);
		if (!MuiCommonControlPacketCore.TryReadDraw(ref platform, message,
			out var draw) || draw.Flags != 0x55AA55AA) return 1;
		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.Setup);
		APTR.WriteUInt32(message, 4, 0x00036600);
		if (!MuiCommonControlPacketCore.TryReadSetup(ref platform, message,
			out var setup) || setup.RenderInfo != 0x00036600) return 2;
		return 42;
	}

	public static uint CommonControlDrawPacketRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var message = APTR.FromPointer(0x00036400);
		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.Draw);
		APTR.WriteUInt32(message, 4, 0x55AA55AA);
		if (!MuiCommonControlPacketCore.TryReadDraw(ref platform, message,
			out var packet) || packet.Flags != 0x55AA55AA) return 1;
		return 42;
	}

	public static uint CommonControlSetupPacketRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var message = APTR.FromPointer(0x00036400);
		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.Setup);
		APTR.WriteUInt32(message, 4, 0x00036600);
		if (!MuiCommonControlPacketCore.TryReadSetup(ref platform, message,
			out var packet) || packet.RenderInfo != 0x00036600) return 1;
		return 42;
	}

	public static uint LayoutSurfacePacketsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var message = APTR.FromPointer(0x00036400);
		APTR.WriteUInt32(message, 0, MuiLayoutPacketCore.AskMinMax);
		APTR.WriteUInt32(message, 4, 0x00036500);
		if (!MuiLayoutPacketCore.TryReadAskMinMax(ref platform, message,
			out var ask) || ask.Storage != 0x00036500) return 1;
		APTR.WriteUInt32(message, 0, MuiLayoutPacketCore.Relayout);
		APTR.WriteUInt32(message, 4, 0xA5A5A5A5);
		if (!MuiLayoutPacketCore.TryReadRelayout(ref platform, message,
			out var relayout) || relayout.Flags != 0xA5A5A5A5) return 2;
		APTR.WriteUInt32(message, 0, MuiLayoutPacketCore.DrawBackground);
		APTR.WriteUInt32(message, 4, 1);
		APTR.WriteUInt32(message, 8, 2);
		APTR.WriteUInt32(message, 12, 80);
		APTR.WriteUInt32(message, 16, 16);
		APTR.WriteUInt32(message, 20, 0x11);
		APTR.WriteUInt32(message, 24, 0x22);
		APTR.WriteUInt32(message, 28, 0x33);
		if (!MuiLayoutPacketCore.TryReadRectangle(ref platform, message,
			MuiLayoutPacketCore.DrawBackground, out var background) ||
			background.Left != 1 || background.Top != 2 ||
			background.RightOrWidth != 80 || background.BottomOrHeight != 16 ||
			background.Reserved2 != 0x33) return 3;
		APTR.WriteUInt32(message, 0, MuiLayoutPacketCore.Backfill);
		APTR.WriteUInt32(message, 4, 3);
		APTR.WriteUInt32(message, 8, 4);
		APTR.WriteUInt32(message, 12, 90);
		APTR.WriteUInt32(message, 16, 20);
		if (!MuiLayoutPacketCore.TryReadRectangle(ref platform, message,
			MuiLayoutPacketCore.Backfill, out var backfill) ||
			backfill.Left != 3 || backfill.Top != 4 ||
			backfill.RightOrWidth != 90 || backfill.BottomOrHeight != 20) return 4;
		return 42;
	}

	public static uint LayoutTextPacketRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var message = APTR.FromPointer(0x00036400);
		APTR.WriteUInt32(message, 0, MuiLayoutPacketCore.Text);
		APTR.WriteUInt32(message, 4, 1);
		APTR.WriteUInt32(message, 8, 2);
		APTR.WriteUInt32(message, 12, 80);
		APTR.WriteUInt32(message, 16, 16);
		APTR.WriteUInt32(message, 20, 0x00036500);
		APTR.WriteUInt32(message, 24, 7);
		APTR.WriteUInt32(message, 28, 0x11);
		APTR.WriteUInt32(message, 32, 0x22);
		if (!MuiLayoutPacketCore.TryReadText(ref platform, message,
			out var text) || text.Left != 1 || text.Top != 2 ||
			text.Width != 80 || text.Height != 16 || text.Text != 0x00036500 ||
			text.Length != 7 || text.Reserved1 != 0x22) return 1;
		return 42;
	}

	public static uint CommonControlCleanupPacketRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var message = APTR.FromPointer(0x00036400);
		APTR.WriteUInt32(message, 0, MuiCommonControlPacketCore.Cleanup);
		if (!MuiCommonControlPacketCore.TryReadMethod(ref platform, message,
			MuiCommonControlPacketCore.Cleanup, out var packet) ||
			packet.MethodId != MuiCommonControlPacketCore.Cleanup) return 1;
		return 42;
	}

	// Focused MG09 AdjustHeight closure. The construction-only attribute pins
	// the list's height triplet to the bounded total of its guest rows.
	public static uint CollectionListAdjustHeightRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint tags = 0x00036140;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, 0x8042850Du);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, 1);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			classRecord, APTR.FromPointer(tags));
		if (list.IsNull) return 3;
		if (!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
			APTR.FromPointer(0x36200), -3) ||
			!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
				APTR.FromPointer(0x36220), -3) ||
			!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
				APTR.FromPointer(0x36240), -3)) return 4;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), list, 0x8042850Du, out var adjust) ||
			adjust != 1) return 5;
		// The host contract covers the packed MinMax height triplet. This native
		// root keeps the closure on the zero-relocation construction/state seam.
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 6;
		return 42;
	}

	// Focused MG09 AdjustWidth closure. The construction-only attribute keeps
	// the list's width sizing policy in guest state; the host contract covers
	// the packed MinMax width triplet from rendered rows.
	public static uint CollectionListAdjustWidthRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint tags = 0x00036140;
		const uint first = 0x00036200;
		const uint second = 0x00036220;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		WriteNativeCString(APTR.FromPointer(first), 's', 'h', 'o', 'r', 't', 0, 0);
		WriteNativeCString(APTR.FromPointer(second), 'w', 'i', 'd', 'e', 's',
			't', 0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, 0x8042354Au);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, 1);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			classRecord, APTR.FromPointer(tags));
		if (list.IsNull) return 3;
		if (!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
			APTR.FromPointer(first), -3) ||
			!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
				APTR.FromPointer(second), -3)) return 4;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), list, 0x8042354Au, out var adjust) ||
			adjust != 1) return 5;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 6;
		return 42;
	}

	// Focused MG09 Stripes closure. The [ISG] boolean is normalized in the
	// guest object record; the host contract covers the graphics fill seam for
	// alternating data rows.
	public static uint CollectionListStripesRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint tags = 0x00036140;
		const uint first = 0x00036200;
		const uint second = 0x00036220;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		WriteNativeCString(APTR.FromPointer(first), 'a', 'l', 'p', 'h', 'a',
			0, 0);
		WriteNativeCString(APTR.FromPointer(second), 'b', 'r', 'a', 'v', 'o',
			0, 0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, 0x8042A308u);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, 1);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			classRecord, APTR.FromPointer(tags));
		if (list.IsNull) return 3;
		if (!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
			APTR.FromPointer(first), -3) ||
			!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
				APTR.FromPointer(second), -3)) return 4;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), list, 0x8042A308u, out var stripes) ||
			stripes != 1) return 5;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform,
			APTR.FromPointer(state), list, 0x8042A308u, 0, false) ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), list, 0x8042A308u, out stripes) ||
			stripes != 0) return 6;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 7;
		return 42;
	}

	// Focused MG09 drop-mark closure. The public DropMark attribute remains
	// read-only; the bounded producer seam clamps an insertion position in the
	// guest record and ShowDropMarks gates the eventual graphics line.
	public static uint CollectionListDropMarkRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint tags = 0x00036140;
		const uint first = 0x00036200;
		const uint second = 0x00036220;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		WriteNativeCString(APTR.FromPointer(first), 'a', 'l', 'p', 'h', 'a',
			0, 0);
		WriteNativeCString(APTR.FromPointer(second), 'b', 'r', 'a', 'v', 'o',
			0, 0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, 0x8042C6F3u);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, 1);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			classRecord, APTR.FromPointer(tags));
		if (list.IsNull) return 3;
		if (!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
			APTR.FromPointer(first), -3) ||
			!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
				APTR.FromPointer(second), -3)) return 4;
		if (!MuiListCore.SetDropMark(ref platform, APTR.FromPointer(state), list,
			99)) return 5;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), list, 0x8042ABA6u, out var mark) || mark != 2)
			return 6;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 7;
		return 42;
	}

	// Focused MG09 drag-sort closure.  The producer seam is enabled only when
	// both MorphOS drag attributes are active, then delegates to the existing
	// struct-backed Move implementation for the reorder itself.
	public static uint CollectionListDragRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint tags = 0x00036140;
		const uint first = 0x00036200;
		const uint second = 0x00036220;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		WriteNativeCString(APTR.FromPointer(first), 'a', 'l', 'p', 'h', 'a',
			0, 0);
		WriteNativeCString(APTR.FromPointer(second), 'b', 'r', 'a', 'v', 'o',
			0, 0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, 0x80426099u);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, 1);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0x80425CD3u);
		APTR.WriteUInt32(APTR.FromPointer(tags), 12, 1);
		APTR.WriteUInt32(APTR.FromPointer(tags), 16, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			classRecord, APTR.FromPointer(tags));
		if (list.IsNull) return 3;
		if (!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
			APTR.FromPointer(first), -3) ||
			!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
				APTR.FromPointer(second), -3)) return 4;
		if (!MuiListCore.DragMove(ref platform, APTR.FromPointer(state), list,
			0, 1)) return 5;
		var moved = MuiListCore.GetEntry(ref platform, APTR.FromPointer(state),
			list, 0, APTR.Null);
		if (moved.Raw != second) return 6;
		if (!MuiListCore.SetAttribute(ref platform, APTR.FromPointer(state), list,
			0x80425CD3u, 99, false)) return 7;
		if (MuiListCore.DragMove(ref platform, APTR.FromPointer(state), list,
			1, 0)) return 8;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 9;
		return 42;
	}

	// Focused MG09 AutoVisible closure.  Layout honors the documented display
	// time policy and moves the caller-selected first row to the active entry
	// only when the normalized guest boolean is enabled.
	public static uint CollectionListAutoVisibleRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint tags = 0x00036140;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, 0x8042A445u);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, 1);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			classRecord, APTR.FromPointer(tags));
		if (list.IsNull) return 3;
		for (var row = 0u; row < 8; row++)
			if (!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state),
				list, APTR.FromPointer(0x00036200 + row * 0x20), -3)) return 4;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform,
			APTR.FromPointer(state), list, 0x8042391Cu, 7, false) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform,
				APTR.FromPointer(state), list, 0x804238D4u, 0, false)) return 5;
		if (!MuiListCore.Layout(ref platform, APTR.FromPointer(state), list,
			0, 0, 80, 24)) return 6;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), list, 0x8042A445u, out var autoVisible) ||
			autoVisible != 1) return 7;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), list, 0x804238D4u, out var first) ||
			first != 5) return 8;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 9;
		return 42;
	}

	// Focused MG09 SortColumn closure.  The normalized column comes from the
	// named FORMAT descriptor range and drives the builtin StringArray compare
	// seam without any host-side state.
	public static uint CollectionListSortColumnRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint format = 0x00036140;
		const uint tags = 0x00036160;
		const uint alpha = 0x00036200;
		const uint alphaFirst = 0x00036220;
		const uint alphaSecond = 0x00036240;
		const uint zulu = 0x00036260;
		const uint zuluFirst = 0x00036280;
		const uint zuluSecond = 0x000362A0;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		WriteNativeCString(APTR.FromPointer(format), ',', 0, 0, 0, 0, 0, 0);
		WriteNativeCString(APTR.FromPointer(alphaFirst), 's', 'a', 'm', 'e',
			0, 0, 0);
		WriteNativeCString(APTR.FromPointer(alphaSecond), 'a', 'l', 'p', 'h',
			'a', 0, 0);
		WriteNativeCString(APTR.FromPointer(zuluFirst), 's', 'a', 'm', 'e',
			0, 0, 0);
		WriteNativeCString(APTR.FromPointer(zuluSecond), 'z', 'u', 'l', 'u',
		0, 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(alpha), 0, alphaFirst);
		APTR.WriteUInt32(APTR.FromPointer(alpha), 4, alphaSecond);
		APTR.WriteUInt32(APTR.FromPointer(alpha), 8, 0);
		APTR.WriteUInt32(APTR.FromPointer(zulu), 0, zuluFirst);
		APTR.WriteUInt32(APTR.FromPointer(zulu), 4, zuluSecond);
		APTR.WriteUInt32(APTR.FromPointer(zulu), 8, 0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, 0x8042894Fu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, 0xFFFFFFFEu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0x804297CEu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 12, 0xFFFFFFFEu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 16, 0x8042B4D5u);
		APTR.WriteUInt32(APTR.FromPointer(tags), 20, 0xFFFFFFFEu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 24, 0x80425C14u);
		APTR.WriteUInt32(APTR.FromPointer(tags), 28, 0xFFFFFFFEu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 32, 0x80423C0Au);
		APTR.WriteUInt32(APTR.FromPointer(tags), 36, format);
		APTR.WriteUInt32(APTR.FromPointer(tags), 40, 0x8042A98Bu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 44, 2);
		APTR.WriteUInt32(APTR.FromPointer(tags), 48, 0x8042CAFBu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 52, 1);
		APTR.WriteUInt32(APTR.FromPointer(tags), 56, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			classRecord, APTR.FromPointer(tags));
		if (list.IsNull) return 3;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), list, 0x8042CAFBu, out var column) ||
			column != 1) return 4;
		if (!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
			APTR.FromPointer(zulu), -3) ||
			!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
				APTR.FromPointer(alpha), -3)) return 5;
		if (!MuiListCore.Sort(ref platform, APTR.FromPointer(state), list)) return 6;
		var sorted = MuiListCore.GetEntry(ref platform, APTR.FromPointer(state),
			list, 0, APTR.Null);
		if (sorted.IsNull || APTR.ReadUInt8(APTR.FromPointer(
			APTR.ReadUInt32(sorted, 4)), 0) != (byte)'a') return 7;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 8;
		return 42;
	}

	// Focused forwarding closure: Listview must route child List attributes
	// through the List class-aware setter, not the raw object store.
	public static uint CollectionListviewForwardingRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint listName = 0x00036100;
		const uint listviewName = 0x00036120;
		WriteClassId(APTR.FromPointer(listName), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		APTR.WriteUInt8(APTR.FromPointer(listviewName), 0, (byte)'L');
		APTR.WriteUInt8(APTR.FromPointer(listviewName), 1, (byte)'i');
		APTR.WriteUInt8(APTR.FromPointer(listviewName), 2, (byte)'s');
		APTR.WriteUInt8(APTR.FromPointer(listviewName), 3, (byte)'t');
		APTR.WriteUInt8(APTR.FromPointer(listviewName), 4, (byte)'v');
		APTR.WriteUInt8(APTR.FromPointer(listviewName), 5, (byte)'i');
		APTR.WriteUInt8(APTR.FromPointer(listviewName), 6, (byte)'e');
		APTR.WriteUInt8(APTR.FromPointer(listviewName), 7, (byte)'w');
		APTR.WriteUInt8(APTR.FromPointer(listviewName), 8, (byte)'.');
		APTR.WriteUInt8(APTR.FromPointer(listviewName), 9, (byte)'m');
		APTR.WriteUInt8(APTR.FromPointer(listviewName), 10, (byte)'u');
		APTR.WriteUInt8(APTR.FromPointer(listviewName), 11, (byte)'i');
		APTR.WriteUInt8(APTR.FromPointer(listviewName), 12, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var listClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(listName), APTR.Null, 0,
			APTR.FromPointer(1));
		var listviewClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(listviewName), APTR.Null, 0,
			APTR.FromPointer(1));
		if (listClass.IsNull || listviewClass.IsNull) return 2;
		var listview = MuiListviewCore.CreateListview(ref platform,
			APTR.FromPointer(state), listviewClass, APTR.Null);
		if (listview.IsNull) return 3;
		var child = MuiListviewCore.ChildList(ref platform,
			APTR.FromPointer(state), listview);
		if (child.IsNull) return 4;
		if (!MuiListviewCore.SetAttribute(ref platform, APTR.FromPointer(state),
			listview, 0x8042CAFBu, 99, false)) return 5;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), child, 0x8042CAFBu, out var column) ||
			column != 0) return 6;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), listview) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 7;
		return 42;
	}

	// Focused MG09 Quiet closure. Mutations while Quiet is enabled mark one
	// guest-resident pending refresh; clearing Quiet flushes exactly one request.
	public static uint CollectionListQuietRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			classRecord, APTR.Null);
		if (list.IsNull) return 3;
		if (!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
			APTR.FromPointer(0x00036200), -3)) return 4;
		var baseline = MuiListCore.RedrawRequests(ref platform,
			APTR.FromPointer(state), list);
		if (baseline != 1) return 5;
		if (!MuiListCore.SetAttribute(ref platform, APTR.FromPointer(state), list,
			0x8042D8C7u, 1, false)) return 6;
		if (!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
			APTR.FromPointer(0x00036220), -3) ||
			!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
				APTR.FromPointer(0x00036240), -3)) return 7;
		if (MuiListCore.RedrawRequests(ref platform, APTR.FromPointer(state), list) !=
			baseline) return 8;
		if (!MuiListCore.SetAttribute(ref platform, APTR.FromPointer(state), list,
			0x8042D8C7u, 0, false)) return 9;
		if (MuiListCore.RedrawRequests(ref platform, APTR.FromPointer(state), list) !=
			baseline + 1) return 10;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 11;
		return 42;
	}

	// Focused MG09 viewport closure. Layout publishes the typed List pixel
	// metrics and keeps TopPixel tied to the normalized first data row.
	public static uint CollectionListViewportRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform,
			APTR.FromPointer(state), classRecord, APTR.Null);
		if (list.IsNull) return 3;
		for (var row = 0u; row < 3; row++)
			if (!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state),
				list, APTR.FromPointer(0x00036200 + row * 0x20), -3)) return 4;
		if (!MuiListCore.Layout(ref platform, APTR.FromPointer(state), list,
			0, 0, 80, 16)) return 5;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), list, 0x80429DF3u, out var top) || top != 0)
			return 6;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), list, 0x804273E9u, out var visible) ||
			visible != 16) return 7;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), list, 0x8042A8F5u, out var total) ||
			total != 24) return 8;
		if (!MuiListCore.SetAttribute(ref platform, APTR.FromPointer(state), list,
			0x804238D4u, 1, false) ||
			!MuiListCore.Layout(ref platform, APTR.FromPointer(state), list,
				0, 0, 80, 16)) return 9;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), list, 0x80429DF3u, out top) || top != 8)
			return 10;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 11;
		return 42;
	}

	// Focused struct ABI closure for the List pixel viewport record. The host
	// List tests cover integration with Layout; this root keeps the typed 68k
	// record writer independently freestanding and relocation-free.
	public static uint CollectionListViewportRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint storage = 0x00036100;
		if (!MuiListCore.WriteViewportMetrics(ref platform,
			APTR.FromPointer(storage), 1, 2, 3, 8, 1)) return 1;
		if (APTR.ReadUInt32(APTR.FromPointer(storage), 4) != 8 ||
			APTR.ReadUInt32(APTR.FromPointer(storage), 8) != 24 ||
			APTR.ReadUInt32(APTR.FromPointer(storage), 12) != 32) return 2;
		return 42;
	}

	// Focused FORMAT closure. The ReadArgs-style short aliases are parsed into
	// the named descriptor record and ORDER=DESCENDING reverses only List.Sort.
	public static uint CollectionListFormatRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint format = 0x00036140;
		const uint tags = 0x00036180;
		const uint zulu = 0x00036200;
		const uint alpha = 0x00036240;
		const uint descriptor = 0x00036300;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		// "D=0 W=1 C=0 O=DESCENDING", written in bounded native chunks.
		WriteNativeCString(APTR.FromPointer(format), 'D', '=', '0', 0, 0, 0, 0);
		WriteNativeCString(APTR.FromPointer(format + 3), ' ', 'W', '=', '1', 0, 0, 0);
		WriteNativeCString(APTR.FromPointer(format + 7), ' ', 'C', '=', '0', 0, 0, 0);
		WriteNativeCString(APTR.FromPointer(format + 11), ' ', 'O', '=', 'D', 'E', 'S', 'C');
		WriteNativeCString(APTR.FromPointer(format + 18), 'E', 'N', 'D', 'I', 'N', 'G', 0);
		WriteNativeCString(APTR.FromPointer(zulu), 'z', 'u', 'l', 'u', 0, 0, 0);
		WriteNativeCString(APTR.FromPointer(alpha), 'a', 'l', 'p', 'h', 'a', 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, 0x80423C0Au);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, format);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			classRecord, APTR.FromPointer(tags));
		if (list.IsNull) return 3;
		if (!MuiListCore.GetFormatColumn(ref platform, APTR.FromPointer(state),
			list, 0, APTR.FromPointer(descriptor))) return 4;
		if (APTR.ReadUInt32(APTR.FromPointer(descriptor), 0) != 0 ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 4) != 1 ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 16) != 0 ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 20) != 4) return 5;
		if (!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
			APTR.FromPointer(zulu), -3) ||
			!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
				APTR.FromPointer(alpha), -3)) return 6;
		if (!MuiListCore.Sort(ref platform, APTR.FromPointer(state), list)) return 7;
		var first = MuiListCore.GetEntry(ref platform, APTR.FromPointer(state),
			list, 0, APTR.Null);
		if (first.IsNull || APTR.ReadUInt8(first, 0) != (byte)'z') return 8;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 9;
		return 42;
	}

	// Focused FORMAT COL closure. The derived mapping follows the descriptor's
	// source column while Display continues to publish the hook's original array
	// order; the host graphics seam qualifies the corresponding Draw path.
	public static uint CollectionListFormatColRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint format = 0x00036140;
		const uint tags = 0x00036180;
		const uint zero = 0x00036200;
		const uint one = 0x00036240;
		const uint two = 0x00036280;
		const uint source = 0x000362C0;
		const uint descriptor = 0x00036380;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		WriteNativeCString(APTR.FromPointer(format), 'C', 'O', 'L', '=', '2',
			',', 'C');
		WriteNativeCString(APTR.FromPointer(format + 7), 'O', 'L', '=', '1',
			',', 'C', 'O');
		WriteNativeCString(APTR.FromPointer(format + 14), 'L', '=', '0', 0, 0, 0,
			0);
		WriteNativeCString(APTR.FromPointer(zero), 'z', 'e', 'r', 'o', 0, 0, 0);
		WriteNativeCString(APTR.FromPointer(one), 'o', 'n', 'e', 0, 0, 0, 0);
		WriteNativeCString(APTR.FromPointer(two), 't', 'w', 'o', 0, 0, 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(source), 0, zero);
		APTR.WriteUInt32(APTR.FromPointer(source), 4, one);
		APTR.WriteUInt32(APTR.FromPointer(source), 8, two);
		APTR.WriteUInt32(APTR.FromPointer(source), 12, 0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, 0x8042894Fu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, 0xFFFFFFFEu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0x8042B4D5u);
		APTR.WriteUInt32(APTR.FromPointer(tags), 12, 0xFFFFFFFEu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 16, 0x80423C0Au);
		APTR.WriteUInt32(APTR.FromPointer(tags), 20, format);
		APTR.WriteUInt32(APTR.FromPointer(tags), 24, 0x8042A98Bu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 28, 3);
		APTR.WriteUInt32(APTR.FromPointer(tags), 32, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			classRecord, APTR.FromPointer(tags));
		if (list.IsNull) return 3;
		if (!MuiListCore.GetFormatColumn(ref platform, APTR.FromPointer(state),
			list, 0, APTR.FromPointer(descriptor)) ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 16) != 2) return 4;
		if (!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
			APTR.FromPointer(source), -3)) return 5;
		var stored = MuiListCore.GetEntry(ref platform, APTR.FromPointer(state),
			list, 0, APTR.Null);
		if (stored.IsNull) return 6;
		if (MuiListCore.GetFormatDisplaySourceColumn(ref platform,
			APTR.FromPointer(state), list, 0) != 2 ||
			MuiListCore.GetFormatDisplaySourceColumn(ref platform,
				APTR.FromPointer(state), list, 1) != 1 ||
			MuiListCore.GetFormatDisplaySourceColumn(ref platform,
				APTR.FromPointer(state), list, 2) != 0) return 7;
		if (!MuiListCore.Display(ref platform, APTR.FromPointer(state), list,
			APTR.FromPointer(source), APTR.FromPointer(0x00036400), 0) ||
			APTR.ReadUInt32(APTR.FromPointer(0x00036400), 0) != zero ||
			APTR.ReadUInt32(APTR.FromPointer(0x00036400), 4) != one ||
			APTR.ReadUInt32(APTR.FromPointer(0x00036400), 8) != two) return 8;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 9;
		return 42;
	}

	// Focused FORMAT BAR closure. The parser keeps BAR on the named descriptor;
	// the host graphics seam qualifies the vertical separator emitted by Draw.
	public static uint CollectionListFormatBarRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint format = 0x00036140;
		const uint tags = 0x00036180;
		const uint descriptor = 0x00036200;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		WriteNativeCString(APTR.FromPointer(format), 'B', 'A', 'R', ',', 0, 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, 0x80423C0Au);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, format);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0x8042A98Bu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 12, 2);
		APTR.WriteUInt32(APTR.FromPointer(tags), 16, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			classRecord, APTR.FromPointer(tags));
		if (list.IsNull) return 3;
		if (!MuiListCore.GetFormatColumn(ref platform, APTR.FromPointer(state),
			list, 0, APTR.FromPointer(descriptor))) return 4;
		if ((APTR.ReadUInt32(APTR.FromPointer(descriptor), 20) & 1u) == 0) return 5;
		if (!MuiListCore.GetFormatColumn(ref platform, APTR.FromPointer(state),
			list, 1, APTR.FromPointer(descriptor))) return 6;
		if ((APTR.ReadUInt32(APTR.FromPointer(descriptor), 20) & 1u) != 0) return 7;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 8;
		return 42;
	}

	// Focused FORMAT PREPARSE closure. The descriptor retains both alignment
	// control strings for the host graphics seam to consume.
	public static uint CollectionListFormatPreparseRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint format = 0x00036140;
		const uint tags = 0x00036180;
		const uint descriptor = 0x00036200;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		WriteNativeCString(APTR.FromPointer(format), 'P', '=', '\\', '3', '3',
			'c', ',');
		WriteNativeCString(APTR.FromPointer(format + 7), 'P', '=', '\\', '3',
			'3', 'r', 0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, 0x80423C0Au);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, format);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0x8042A98Bu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 12, 2);
		APTR.WriteUInt32(APTR.FromPointer(tags), 16, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			classRecord, APTR.FromPointer(tags));
		if (list.IsNull) return 3;
		if (!MuiListCore.GetFormatColumn(ref platform, APTR.FromPointer(state),
			list, 0, APTR.FromPointer(descriptor))) return 4;
		if (APTR.ReadUInt32(APTR.FromPointer(descriptor), 24) == 0 ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 28) != 4) return 5;
		if (!MuiListCore.GetFormatColumn(ref platform, APTR.FromPointer(state),
			list, 1, APTR.FromPointer(descriptor))) return 6;
		if (APTR.ReadUInt32(APTR.FromPointer(descriptor), 24) == 0 ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 28) != 4) return 7;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 8;
		return 42;
	}

	// Focused FORMAT -1 closure. Explicit MINWIDTH/MAXWIDTH content flags are
	// resolved from the widest displayed StringArray entries into the named
	// guest metrics record during Layout; geometry is checked through the same
	// struct-backed public seam used by the host contract.
	public static uint CollectionListFormatMinusOneRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint format = 0x00036140;
		const uint tags = 0x00036180;
		const uint firstRow = 0x00036200;
		const uint secondRow = 0x00036220;
		const uint shortFirst = 0x00036300;
		const uint shortSecond = 0x00036320;
		const uint wideFirst = 0x00036340;
		const uint narrowSecond = 0x00036360;
		const uint renderInfo = 0x00036400;
		const uint geometry = 0x00036440;
		const uint descriptor = 0x00036460;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		WriteNativeCString(APTR.FromPointer(format), 'M', 'I', 'N', 'W', 'I', 'D', 'T');
		WriteNativeCString(APTR.FromPointer(format + 7), 'H', '=', '-', '1', ',', 'M', 'A');
		WriteNativeCString(APTR.FromPointer(format + 14), 'X', 'W', 'I', 'D', 'T', 'H', '=');
		WriteNativeCString(APTR.FromPointer(format + 21), '-', '1', 0, 0, 0, 0, 0);
		WriteNativeCString(APTR.FromPointer(shortFirst), 'x', 0, 0, 0, 0, 0, 0);
		WriteNativeCString(APTR.FromPointer(shortSecond), 'b', 'b', 0, 0, 0, 0, 0);
		WriteNativeCString(APTR.FromPointer(wideFirst), 'l', 'o', 'n', 'g', 0, 0, 0);
		WriteNativeCString(APTR.FromPointer(narrowSecond), 'c', 0, 0, 0, 0, 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(firstRow), 0, shortFirst);
		APTR.WriteUInt32(APTR.FromPointer(firstRow), 4, shortSecond);
		APTR.WriteUInt32(APTR.FromPointer(firstRow), 8, 0);
		APTR.WriteUInt32(APTR.FromPointer(secondRow), 0, wideFirst);
		APTR.WriteUInt32(APTR.FromPointer(secondRow), 4, narrowSecond);
		APTR.WriteUInt32(APTR.FromPointer(secondRow), 8, 0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, 0x8042894Fu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, 0xFFFFFFFEu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0x8042B4D5u);
		APTR.WriteUInt32(APTR.FromPointer(tags), 12, 0xFFFFFFFEu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 16, 0x80423C0Au);
		APTR.WriteUInt32(APTR.FromPointer(tags), 20, format);
		APTR.WriteUInt32(APTR.FromPointer(tags), 24, 0x8042A98Bu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 28, 2);
		APTR.WriteUInt32(APTR.FromPointer(tags), 32, 0);
		APTR.WriteUInt32(APTR.FromPointer(renderInfo), 20, 0x00036420);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			classRecord, APTR.FromPointer(tags));
		if (list.IsNull) return 3;
		if (!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
			APTR.FromPointer(firstRow), -3) ||
			!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
				APTR.FromPointer(secondRow), -3)) return 4;
		if (!MuiAreaLayoutCore.Setup(ref platform, APTR.FromPointer(state), list,
			APTR.FromPointer(renderInfo)) ||
			!MuiListCore.Layout(ref platform, APTR.FromPointer(state), list, 0, 0,
			80, 8)) return 5;
		if (!MuiListCore.GetFormatColumn(ref platform, APTR.FromPointer(state),
			list, 0, APTR.FromPointer(descriptor)) ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 20) != 0x20u) return 6;
		if (!MuiListCore.GetFormatColumn(ref platform, APTR.FromPointer(state),
			list, 1, APTR.FromPointer(descriptor)) ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 20) != 0x40u) return 7;
		if (!MuiListCore.GetColumnGeometry(ref platform, APTR.FromPointer(state),
			list, 80, APTR.FromPointer(geometry)) ||
			APTR.ReadUInt32(APTR.FromPointer(geometry), 4) != 38u ||
			APTR.ReadUInt32(APTR.FromPointer(geometry + 8), 4) != 16u) return 8;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 9;
		return 42;
	}

	// Focused FORMAT quoted-ReadArgs closure. Commas inside a quoted value do
	// not create columns; quote delimiters are removed before decoding into the
	// named descriptor record, and quoted numeric/ORDER values use normal
	// semantics.
	public static uint CollectionListFormatQuotedRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint format = 0x00036140;
		const uint tags = 0x000361A0;
		const uint descriptor = 0x00036200;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		WriteNativeCString(APTR.FromPointer(format), 'P', '=', '"', '*', 'e', 'c', ',');
		WriteNativeCString(APTR.FromPointer(format + 7), 'k', 'e', 'e', 'p', '"', ' ', 'M');
		WriteNativeCString(APTR.FromPointer(format + 14), 'A', 'X', 'W', 'I', 'D', 'T', 'H');
		WriteNativeCString(APTR.FromPointer(format + 21), '=', '"', '2', '5', 'p', 'x', '"');
		WriteNativeCString(APTR.FromPointer(format + 28), ',', 'O', '=', '"', 'D', 'E', '*');
		WriteNativeCString(APTR.FromPointer(format + 35), 'S', 'C', '"', 0, 0, 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, 0x80423C0Au);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, format);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0x8042A98Bu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 12, 2);
		APTR.WriteUInt32(APTR.FromPointer(tags), 16, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			classRecord, APTR.FromPointer(tags));
		if (list.IsNull || MuiListCore.FormatColumnCount(ref platform,
			APTR.FromPointer(state), list) != 2) return 3;
		if (!MuiListCore.GetFormatColumn(ref platform, APTR.FromPointer(state),
			list, 0, APTR.FromPointer(descriptor)) ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 24) == 0u ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 24) == format + 3u ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 28) != 7u ||
			APTR.ReadUInt8(APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(descriptor), 24)), 0) != 0x1Bu ||
			APTR.ReadUInt8(APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(descriptor), 24)), 1) != (byte)'c' ||
			APTR.ReadUInt8(APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(descriptor), 24)), 2) != (byte)',' ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 12) != 25u ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 20) != 16u) return 4;
		if (!MuiListCore.GetFormatColumn(ref platform, APTR.FromPointer(state),
			list, 1, APTR.FromPointer(descriptor)) ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 20) != 4u) return 5;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 6;
		return 42;
	}

	// Focused FORMAT escape closure. ReadArgs decodes an escaped quote without
	// ending the quoted item, decodes *n to a newline, and still applies ORDER
	// semantics to the decoded value.
	public static uint CollectionListFormatEscapesRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint format = 0x00036140;
		const uint tags = 0x000361A0;
		const uint descriptor = 0x00036200;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		WriteNativeCString(APTR.FromPointer(format), 'P', '=', '"', '*', '"', 'c', '"');
		WriteNativeCString(APTR.FromPointer(format + 7), ',', 'O', '=', '"', 'D', 'E', '*');
		WriteNativeCString(APTR.FromPointer(format + 14), 'S', 'C', '"', ',', 'P', '=', '"');
		WriteNativeCString(APTR.FromPointer(format + 21), '*', 'n', '"', 0, 0, 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, 0x80423C0Au);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, format);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0x8042A98Bu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 12, 3);
		APTR.WriteUInt32(APTR.FromPointer(tags), 16, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			classRecord, APTR.FromPointer(tags));
		if (list.IsNull || MuiListCore.FormatColumnCount(ref platform,
			APTR.FromPointer(state), list) != 3) return 3;
		if (!MuiListCore.GetFormatColumn(ref platform, APTR.FromPointer(state),
			list, 0, APTR.FromPointer(descriptor)) ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 28) != 2u) return 4;
		var quote = APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(descriptor), 24));
		if (quote.IsNull || APTR.ReadUInt8(quote, 0) != (byte)'"' ||
			APTR.ReadUInt8(quote, 1) != (byte)'c') return 5;
		if (!MuiListCore.GetFormatColumn(ref platform, APTR.FromPointer(state),
			list, 1, APTR.FromPointer(descriptor)) ||
			(APTR.ReadUInt32(APTR.FromPointer(descriptor), 20) & 4u) == 0) return 6;
		if (!MuiListCore.GetFormatColumn(ref platform, APTR.FromPointer(state),
			list, 2, APTR.FromPointer(descriptor)) ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 28) != 1u) return 7;
		var newline = APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(descriptor), 24));
		if (newline.IsNull || APTR.ReadUInt8(newline, 0) != (byte)'\n') return 8;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 9;
		return 42;
	}

	// Focused FORMAT keyword-form closure. ReadArgs accepts KEY VALUE in
	// addition to KEY=VALUE; switches remain bare and quoted values may contain
	// commas without creating an extra display column.
	public static uint CollectionListFormatKeywordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00036300;
		const uint state = 0x00036400;
		const uint name = 0x00036500;
		const uint format = 0x00036540;
		const uint tags = 0x000365A0;
		const uint descriptor = 0x00036600;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		WriteNativeCString(APTR.FromPointer(format), 'D', ' ', '4', ' ', 'W', '=', '5');
		WriteNativeCString(APTR.FromPointer(format + 7), '0', ' ', 'P', ' ', '"', '*', 'n');
		WriteNativeCString(APTR.FromPointer(format + 14), 'c', ',', 'k', 'e', 'e', 'p', '"');
		WriteNativeCString(APTR.FromPointer(format + 21), ',', ' ', 'O', ' ', 'D', 'E', 'S');
		WriteNativeCString(APTR.FromPointer(format + 28), 'C', 'E', 'N', 'D', 'I', 'N', 'G');
		WriteNativeCString(APTR.FromPointer(format + 35), ',', ' ', 'B', 'A', 'R', ' ', 'S');
		WriteNativeCString(APTR.FromPointer(format + 42), 'O', 'R', 'T', 'A', 'B', 'L', 'E');
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, 0x80423C0Au);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, format);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0x8042A98Bu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 12, 3);
		APTR.WriteUInt32(APTR.FromPointer(tags), 16, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			classRecord, APTR.FromPointer(tags));
		if (list.IsNull || MuiListCore.FormatColumnCount(ref platform,
			APTR.FromPointer(state), list) != 3) return 3;
		if (!MuiListCore.GetFormatColumn(ref platform, APTR.FromPointer(state),
			list, 0, APTR.FromPointer(descriptor)) ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 0) != 4u ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 4) != 50u ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 28) != 7u) return 4;
		var preparse = APTR.FromPointer(APTR.ReadUInt32(
			APTR.FromPointer(descriptor), 24));
		if (preparse.IsNull || APTR.ReadUInt8(preparse, 0) != (byte)'\n' ||
			APTR.ReadUInt8(preparse, 2) != (byte)',' ||
			APTR.ReadUInt8(preparse, 6) != (byte)'p') return 5;
		if (!MuiListCore.GetFormatColumn(ref platform, APTR.FromPointer(state),
			list, 1, APTR.FromPointer(descriptor)) ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 20) != 4u) return 6;
		if (!MuiListCore.GetFormatColumn(ref platform, APTR.FromPointer(state),
			list, 2, APTR.FromPointer(descriptor)) ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 20) != 3u) return 7;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 8;
		return 42;
	}

	// Focused FORMAT content-weight closure. WEIGHT=-1 resolves the widest
	// displayed value into a fixed guest column width before the remaining
	// weighted columns receive the leftover pixels.
	public static uint CollectionListFormatWeightRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00036600;
		const uint state = 0x00036700;
		const uint name = 0x00036800;
		const uint format = 0x00036840;
		const uint tags = 0x00036880;
		const uint firstText = 0x00036900;
		const uint secondText = 0x00036920;
		const uint firstRow = 0x00036940;
		const uint secondRow = 0x00036960;
		const uint renderInfo = 0x00036980;
		const uint descriptor = 0x000369C0;
		const uint geometry = 0x00036A00;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		WriteNativeCString(APTR.FromPointer(format), 'W', 'E', 'I', 'G', 'H', 'T', '=');
		WriteNativeCString(APTR.FromPointer(format + 7), '-', '1', ',', 'W', 'E', 'I', 'G');
		WriteNativeCString(APTR.FromPointer(format + 14), 'H', 'T', '=', '1', 0, 0, 0);
		WriteNativeCString(APTR.FromPointer(firstText), 'a', 0, 0, 0, 0, 0, 0);
		WriteNativeCString(APTR.FromPointer(secondText), 'l', 'o', 'n', 'g', 0, 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(firstRow), 0, firstText);
		APTR.WriteUInt32(APTR.FromPointer(firstRow), 4, secondText);
		APTR.WriteUInt32(APTR.FromPointer(firstRow), 8, 0);
		APTR.WriteUInt32(APTR.FromPointer(secondRow), 0, secondText);
		APTR.WriteUInt32(APTR.FromPointer(secondRow), 4, firstText);
		APTR.WriteUInt32(APTR.FromPointer(secondRow), 8, 0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, 0x8042894Fu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, 0xFFFFFFFEu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0x8042B4D5u);
		APTR.WriteUInt32(APTR.FromPointer(tags), 12, 0xFFFFFFFEu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 16, 0x80423C0Au);
		APTR.WriteUInt32(APTR.FromPointer(tags), 20, format);
		APTR.WriteUInt32(APTR.FromPointer(tags), 24, 0x8042A98Bu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 28, 2);
		APTR.WriteUInt32(APTR.FromPointer(tags), 32, 0);
		APTR.WriteUInt32(APTR.FromPointer(renderInfo), 20, 0x000369B0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			classRecord, APTR.FromPointer(tags));
		if (list.IsNull) return 3;
		if (!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
			APTR.FromPointer(firstRow), -3) ||
			!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
				APTR.FromPointer(secondRow), -3)) return 4;
		if (!MuiListCore.GetFormatColumn(ref platform, APTR.FromPointer(state),
			list, 0, APTR.FromPointer(descriptor)) ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 4) != 0xFFFFFFFFu ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 20) != 128u) return 5;
		if (!MuiAreaLayoutCore.Setup(ref platform, APTR.FromPointer(state), list,
			APTR.FromPointer(renderInfo)) ||
			!MuiListCore.Layout(ref platform, APTR.FromPointer(state), list, 0, 0,
				80, 8) ||
			!MuiListCore.GetColumnGeometry(ref platform, APTR.FromPointer(state),
				list, 80, APTR.FromPointer(geometry))) return 6;
		if (APTR.ReadUInt32(APTR.FromPointer(geometry), 4) != 32u ||
			APTR.ReadUInt32(APTR.FromPointer(geometry + 8), 4) != 44u) return 7;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 8;
		return 42;
	}

	// Focused FORMAT ReadArgs-error closure. Numeric suffixes, invalid ORDER
	// values, switch assignments, unknown fields, and empty keywords must reject
	// the replacement without disturbing the installed descriptor block.
	public static uint CollectionListFormatErrorsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint format = 0x00036140;
		const uint malformed = 0x00036180;
		const uint tags = 0x000361C0;
		const uint descriptor = 0x00036200;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		WriteNativeCString(APTR.FromPointer(format), 'D', '=', '+', '8', ' ',
			'O', '=');
		WriteNativeCString(APTR.FromPointer(format + 7), 'A', 'S', 'C', 0, 0, 0, 0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, 0x80423C0Au);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, format);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			classRecord, APTR.FromPointer(tags));
		if (list.IsNull) return 3;
		if (!MuiListCore.GetFormatColumn(ref platform, APTR.FromPointer(state),
			list, 0, APTR.FromPointer(descriptor)) ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 0) != 8u) return 4;

		WriteNativeCString(APTR.FromPointer(malformed), 'D', '=', '8', 'p', 'x', 0, 0);
		if (MuiListCore.SetAttribute(ref platform, APTR.FromPointer(state), list,
			0x80423C0Au, malformed, false)) return 5;
		WriteNativeCString(APTR.FromPointer(malformed), 'M', 'I', 'W', '=', '1', '0', 'j');
		WriteNativeCString(APTR.FromPointer(malformed + 7), 'u', 'n', 'k', 0, 0, 0, 0);
		if (MuiListCore.SetAttribute(ref platform, APTR.FromPointer(state), list,
			0x80423C0Au, malformed, false)) return 6;
		WriteNativeCString(APTR.FromPointer(malformed), 'O', '=', 'S', 'I', 'D', 'E', 'W');
		WriteNativeCString(APTR.FromPointer(malformed + 7), 'A', 'Y', 'S', 0, 0, 0, 0);
		if (MuiListCore.SetAttribute(ref platform, APTR.FromPointer(state), list,
			0x80423C0Au, malformed, false)) return 7;
		WriteNativeCString(APTR.FromPointer(malformed), 'B', 'A', 'R', '=', '1', 0, 0);
		if (MuiListCore.SetAttribute(ref platform, APTR.FromPointer(state), list,
			0x80423C0Au, malformed, false)) return 8;
		WriteNativeCString(APTR.FromPointer(malformed), 'U', 'N', 'K', 'N', 'O', 'W', 'N');
		WriteNativeCString(APTR.FromPointer(malformed + 7), '=', '1', 0, 0, 0, 0, 0);
		if (MuiListCore.SetAttribute(ref platform, APTR.FromPointer(state), list,
			0x80423C0Au, malformed, false)) return 9;
		WriteNativeCString(APTR.FromPointer(malformed), 'C', 'O', 'L', '=', '0', ',', 'C');
		WriteNativeCString(APTR.FromPointer(malformed + 7), 'O', 'L', '=', '0', 0, 0, 0);
		if (MuiListCore.SetAttribute(ref platform, APTR.FromPointer(state), list,
			0x80423C0Au, malformed, false)) return 10;
		WriteNativeCString(APTR.FromPointer(malformed), 'P', '=', 0, 0, 0, 0, 0);
		if (MuiListCore.SetAttribute(ref platform, APTR.FromPointer(state), list,
			0x80423C0Au, malformed, false) ||
			APTR.ReadUInt32(APTR.FromPointer(descriptor), 0) != 8u) return 11;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 12;
		return 42;
	}

	// Focused MorphOS FORMAT layout closure. A non-first column whose pixel
	// minimum cannot fit is hidden, and the remaining visible columns reclaim
	// the available width while the first column remains clipped.
	public static uint CollectionListFormatMinimumRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint format = 0x00036140;
		const uint tags = 0x000361A0;
		const uint geometry = 0x000361C0;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		WriteNativeCString(APTR.FromPointer(format), 'M', 'I', 'N', 'W', 'I',
			'D', 'T');
		WriteNativeCString(APTR.FromPointer(format + 7), 'H', '=', '6', '0',
			'p', 'x', ',');
		WriteNativeCString(APTR.FromPointer(format + 14), 'M', 'I', 'N', 'W',
			'I', 'D', 'T');
		WriteNativeCString(APTR.FromPointer(format + 21), 'H', '=', '6', '0',
			'p', 'x', ',');
		WriteNativeCString(APTR.FromPointer(format + 28), 'M', 'I', 'N', 'W',
			'I', 'D', 'T');
		WriteNativeCString(APTR.FromPointer(format + 35), 'H', '=', '1', '0',
			'p', 'x', 0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, 0x80423C0Au);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, format);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0x8042A98Bu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 12, 3);
		APTR.WriteUInt32(APTR.FromPointer(tags), 16, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			classRecord, APTR.FromPointer(tags));
		if (list.IsNull) return 3;
		if (!MuiListCore.GetColumnGeometry(ref platform,
			APTR.FromPointer(state), list, 100, APTR.FromPointer(geometry))) return 4;
		if (APTR.ReadUInt32(APTR.FromPointer(geometry), 0) != 0 ||
			APTR.ReadUInt32(APTR.FromPointer(geometry), 4) != 60 ||
			APTR.ReadUInt32(APTR.FromPointer(geometry), 8) != 64 ||
			APTR.ReadUInt32(APTR.FromPointer(geometry), 12) != 0 ||
			APTR.ReadUInt32(APTR.FromPointer(geometry), 16) != 64 ||
			APTR.ReadUInt32(APTR.FromPointer(geometry), 20) != 36) return 5;
		if (!MuiListCore.GetColumnGeometry(ref platform,
			APTR.FromPointer(state), list, 50, APTR.FromPointer(geometry))) return 6;
		if (APTR.ReadUInt32(APTR.FromPointer(geometry), 0) != 0 ||
			APTR.ReadUInt32(APTR.FromPointer(geometry), 4) != 50 ||
			APTR.ReadUInt32(APTR.FromPointer(geometry), 8) != 50 ||
			APTR.ReadUInt32(APTR.FromPointer(geometry), 12) != 0 ||
			APTR.ReadUInt32(APTR.FromPointer(geometry), 16) != 50 ||
			APTR.ReadUInt32(APTR.FromPointer(geometry), 20) != 0) return 7;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 8;
		return 42;
	}

	// Focused explicit List column visibility closure. HideColumn/ShowColumn
	// retain their bounded two-word mask in guest memory and reuse the same
	// geometry path as FORMAT minimum-width hiding.
	public static uint CollectionListColumnVisibilityRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint format = 0x00036140;
		const uint tags = 0x00036200;
		const uint geometry = 0x00036240;
		WriteClassId(APTR.FromPointer(name), 'L', 'i', 's', 't', (char)0,
			(char)0, (char)0, (char)0, (char)0);
		WriteNativeCString(APTR.FromPointer(format), 'D', 'E', 'L', 'T', 'A',
			'=', '0');
		WriteNativeCString(APTR.FromPointer(format + 7), ' ', 'W', 'E', 'I',
			'G', 'H', 'T');
		WriteNativeCString(APTR.FromPointer(format + 14), '=', '1', ',', 'D',
			'E', 'L', 'T');
		WriteNativeCString(APTR.FromPointer(format + 21), 'A', '=', '0', ' ',
			'W', 'E', 'I');
		WriteNativeCString(APTR.FromPointer(format + 28), 'G', 'H', 'T', '=',
			'1', ',', 'D');
		WriteNativeCString(APTR.FromPointer(format + 35), 'E', 'L', 'T', 'A',
			'=', '0', ' ');
		WriteNativeCString(APTR.FromPointer(format + 42), 'W', 'E', 'I', 'G',
			'H', 'T', '=');
		WriteNativeCString(APTR.FromPointer(format + 49), '1', 0, 0, 0, 0, 0,
			0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, 0x80423C0Au);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, format);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0x8042A98Bu);
		APTR.WriteUInt32(APTR.FromPointer(tags), 12, 3);
		APTR.WriteUInt32(APTR.FromPointer(tags), 16, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1));
		if (classRecord.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform,
			APTR.FromPointer(state), classRecord, APTR.FromPointer(tags));
		if (list.IsNull) return 3;
		if (!MuiListCore.SetColumnVisibility(ref platform,
			APTR.FromPointer(state), list, 1, true)) return 4;
		if (!MuiListCore.GetColumnGeometry(ref platform,
			APTR.FromPointer(state), list, 100, APTR.FromPointer(geometry))) return 5;
		if (APTR.ReadUInt32(APTR.FromPointer(geometry), 4) != 50 ||
			APTR.ReadUInt32(APTR.FromPointer(geometry), 8) != 50 ||
			APTR.ReadUInt32(APTR.FromPointer(geometry), 12) != 0 ||
			APTR.ReadUInt32(APTR.FromPointer(geometry), 16) != 50 ||
			APTR.ReadUInt32(APTR.FromPointer(geometry), 20) != 50) return 6;
		if (!MuiListCore.SetColumnVisibility(ref platform,
			APTR.FromPointer(state), list, 1, false)) return 7;
		if (!MuiListCore.GetColumnGeometry(ref platform,
			APTR.FromPointer(state), list, 100, APTR.FromPointer(geometry))) return 8;
		if (APTR.ReadUInt32(APTR.FromPointer(geometry), 4) != 33 ||
			APTR.ReadUInt32(APTR.FromPointer(geometry), 8) != 33 ||
			APTR.ReadUInt32(APTR.FromPointer(geometry), 12) != 33 ||
			APTR.ReadUInt32(APTR.FromPointer(geometry), 16) != 66 ||
			APTR.ReadUInt32(APTR.FromPointer(geometry), 20) != 34) return 9;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list) ||
			!MuiMasterLifecycleCore.Dispose(ref platform,
				APTR.FromPointer(privateRoot))) return 10;
		return 42;
	}

	// Focused ColumnOrder closure. The public BYTE* permutation is copied into
	// the typed guest order record and invalid permutations are rejected without
	// replacing the installed state. Host coverage exercises its List integration.
	public static uint CollectionListColumnOrderRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint storage = 0x00037F00;
		const uint values = 0x00038000;
		const uint order = 0x00038100;
		APTR.WriteUInt8(APTR.FromPointer(order), 0, 2);
		APTR.WriteUInt8(APTR.FromPointer(order), 1, 0);
		APTR.WriteUInt8(APTR.FromPointer(order), 2, 1);
		APTR.WriteUInt8(APTR.FromPointer(order), 3, 0xFF);
		if (!MuiListCore.WriteColumnOrder(ref platform,
			APTR.FromPointer(storage), APTR.FromPointer(values),
			APTR.FromPointer(order), 3)) return 1;
		if (MuiListCore.GetColumnOrderDisplayColumn(ref platform,
			APTR.FromPointer(storage), 0, 0xFFFFFFFFu) != 2u ||
			MuiListCore.GetColumnOrderDisplayColumn(ref platform,
				APTR.FromPointer(storage), 1, 0xFFFFFFFFu) != 0u ||
			MuiListCore.GetColumnOrderDisplayColumn(ref platform,
				APTR.FromPointer(storage), 2, 0xFFFFFFFFu) != 1u) return 2;
		if (APTR.ReadUInt8(APTR.FromPointer(values), 0) != 2 ||
			APTR.ReadUInt8(APTR.FromPointer(values), 1) != 0 ||
			APTR.ReadUInt8(APTR.FromPointer(values), 2) != 1) return 3;
		APTR.WriteUInt8(APTR.FromPointer(order), 1, 2);
		if (MuiListCore.WriteColumnOrder(ref platform,
			APTR.FromPointer(storage), APTR.FromPointer(values),
			APTR.FromPointer(order), 3)) return 4;
		return 42;
	}

	// Focused Listview click-state closure. The click record is a typed guest
	// struct: single clicks clear transient flags, exactly two clicks publish
	// DoubleClick, and three or more publish AgainClick.
	public static uint CollectionListviewClickStateRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint storage = 0x00038500;
		var record = APTR.FromPointer(storage);
		if (!MuiListviewCore.WriteClickResult(ref platform, record, 5, 1) ||
			APTR.ReadUInt32(record, 4) != 5 ||
			APTR.ReadUInt32(record, 8) != 0 ||
			APTR.ReadUInt32(record, 12) != 0 ||
			APTR.ReadUInt32(record, 16) != 1) return 1;
		if (!MuiListviewCore.WriteClickResult(ref platform, record, 5, 2) ||
			APTR.ReadUInt32(record, 8) != 1 ||
			APTR.ReadUInt32(record, 12) != 0 ||
			APTR.ReadUInt32(record, 16) != 2) return 2;
		if (!MuiListviewCore.WriteClickResult(ref platform, record, 7, 3) ||
			APTR.ReadUInt32(record, 4) != 7 ||
			APTR.ReadUInt32(record, 8) != 0 ||
			APTR.ReadUInt32(record, 12) != 1 ||
			APTR.ReadUInt32(record, 16) != 3) return 3;
		return 42;
	}

	public static uint CollectionCoreRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint gamma = 0x00036200;
		const uint alpha = 0x00036220;
		const uint beta = 0x00036240;
		const uint storage = 0x00036280;
		const uint cursor = 0x00036290;
		const uint listRenderInfo = 0x00036400;
		const uint listRastPort = 0x00036420;
		const uint listTags = 0x000363C0;
		const uint lvName = 0x000362A0;
		const uint ftName = 0x000362C0;
		const uint ftText = 0x00036300;
		const uint ftMore = 0x00036340;
		const uint ftTags = 0x00036380;
		// "List.mui"
		var n = APTR.FromPointer(name);
		APTR.WriteUInt8(n, 0, (byte)'L');
		APTR.WriteUInt8(n, 1, (byte)'i');
		APTR.WriteUInt8(n, 2, (byte)'s');
		APTR.WriteUInt8(n, 3, (byte)'t');
		APTR.WriteUInt8(n, 4, (byte)'.');
		APTR.WriteUInt8(n, 5, (byte)'m');
		APTR.WriteUInt8(n, 6, (byte)'u');
		APTR.WriteUInt8(n, 7, (byte)'i');
		APTR.WriteUInt8(n, 8, 0);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 0, 0x8042D1C3u);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 4, 16);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 8, 0);
		WriteWord(gamma, (byte)'g', (byte)'a', (byte)'m', (byte)'m', (byte)'a');
		WriteWord(alpha, (byte)'a', (byte)'l', (byte)'p', (byte)'h', (byte)'a');
		WriteWord(beta, (byte)'b', (byte)'e', (byte)'t', (byte)'a', 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1)).Raw;
		if (cl == 0) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(cl), APTR.FromPointer(listTags)).Raw;
		if (list == 0) return 3;
		if (!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(list), APTR.FromPointer(gamma), -3) ||
			!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(list), APTR.FromPointer(alpha), -3) ||
			!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(list), APTR.FromPointer(beta), -3)) return 4;
		if (MuiListCore.EntryCount(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(list)) != 3) return 5;
		if (!MuiListCore.Sort(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(list))) return 6;
		if (MuiListCore.GetEntry(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(list), 0, APTR.FromPointer(storage)).Raw != alpha)
			return 7;
		if (MuiListCore.GetEntry(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(list), 2, APTR.Null).Raw != gamma) return 8;
		if (!MuiListCore.Select(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(list), 1, 1, APTR.Null)) return 9;
		APTR.WriteUInt32(APTR.FromPointer(cursor), 0, 0xFFFFFFFFu);
		if (!MuiListCore.NextSelected(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(list), APTR.FromPointer(cursor)) ||
			APTR.ReadUInt32(APTR.FromPointer(cursor), 0) != 1) return 10;
		APTR.WriteUInt32(APTR.FromPointer(listRenderInfo), 20, listRastPort);
		if (!MuiAreaLayoutCore.Setup(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(list), APTR.FromPointer(listRenderInfo))) return 48;
		if (!MuiListCore.Layout(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(list), 0, 0, 80, 16) ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), APTR.FromPointer(list), 0x8042191Fu,
				out var listVisible) || listVisible != 1 ||
			!MuiListCore.Draw(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(list), 0)) return 49;
		// TestPos is covered through the host dispatcher contract. Keep this
		// native closure focused on the zero-relocation collection/render seam;
		// the generic hit-test specialization is intentionally not pulled into
		// this broad root until its 68020 constructed-method relocation is
		// eliminated.
		if (!MuiListCore.Remove(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(list), 0)) return 11;
		if (MuiListCore.EntryCount(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(list)) != 2) return 12;
		if (!MuiListCore.Clear(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(list))) return 13;
		if (MuiListCore.EntryCount(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(list)) != 0) return 14;

		// ---- Listview composite over an internally created List child --------
		var lv = APTR.FromPointer(lvName); // "Listview.mui"
		APTR.WriteUInt8(lv, 0, (byte)'L');
		APTR.WriteUInt8(lv, 1, (byte)'i');
		APTR.WriteUInt8(lv, 2, (byte)'s');
		APTR.WriteUInt8(lv, 3, (byte)'t');
		APTR.WriteUInt8(lv, 4, (byte)'v');
		APTR.WriteUInt8(lv, 5, (byte)'i');
		APTR.WriteUInt8(lv, 6, (byte)'e');
		APTR.WriteUInt8(lv, 7, (byte)'w');
		APTR.WriteUInt8(lv, 8, (byte)'.');
		APTR.WriteUInt8(lv, 9, (byte)'m');
		APTR.WriteUInt8(lv, 10, (byte)'u');
		APTR.WriteUInt8(lv, 11, (byte)'i');
		APTR.WriteUInt8(lv, 12, 0);
		var lvClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(lvName), APTR.Null, 0,
			APTR.FromPointer(1)).Raw;
		if (lvClass == 0) return 15;
		var listview = MuiListviewCore.CreateListview(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(lvClass), APTR.Null).Raw;
		if (listview == 0) return 16;
		var child = MuiListviewCore.ChildList(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(listview)).Raw;
		if (child == 0) return 17;
		if (!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(child), APTR.FromPointer(gamma), -3) ||
			!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(child), APTR.FromPointer(alpha), -3)) return 18;
		if (MuiListCore.EntryCount(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(child)) != 2) return 19;
		// Click drives the child's active/selection through the composite.
		if (!MuiListviewCore.HandleClick(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(listview), 1, 1, 0, false)) return 20;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(child), 0x8042391cu, out var lvActive) ||
			lvActive != 1) return 21;
		// Scroll position is independent from the selected row. Clear the
		// selection before exercising the bounded First setter so the native
		// closure verifies the scrollbar's max-first clamp itself.
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(child), 0x8042391cu,
			0xFFFFFFFFu, false)) return 22;
		// Group layout reserves the scrollbar and positions the child list.
		if (!MuiListviewCore.Layout(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(listview), 0, 0, 100, 50)) return 22;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(child), 0x8042B59Cu, out var lvChildWidth) ||
			lvChildWidth != 84) return 23;
		// Publish a two-row viewport and clamp Prop-like first-row movement to
		// the owned List child; this is the native closure for the scrollbar seam.
		for (var i = 0u; i < 6; i++)
			if (!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state),
				APTR.FromPointer(child), APTR.FromPointer(0x363F0 + i), -3)) return 43;
		if (!MuiListviewCore.Layout(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(listview), 0, 0, 100, 16)) return 44;
		APTR.WriteUInt32(APTR.FromPointer(listRenderInfo), 20, listRastPort);
		if (!MuiAreaLayoutCore.Setup(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(listview), APTR.FromPointer(listRenderInfo)) ||
			!MuiListviewCore.Draw(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(listview), 0)) return 50;
		if (!MuiListviewCore.GetScrollerState(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(listview), out var entries,
			out var visible, out var first, out var maxFirst) || entries != 8 ||
			visible != 2 || first != 0 || maxFirst != 6) return 45;
		if (!MuiListviewCore.SetScrollerFirst(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(listview), 99)) return 46;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(child), 0x804238d4u,
			out var clampedFirst) || clampedFirst != 6) return 47;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(listview))) return 24;

		// ---- Floattext subclass: owned text parsed into rows -----------------
		var fn = APTR.FromPointer(ftName); // "Floattext.mui"
		APTR.WriteUInt8(fn, 0, (byte)'F');
		APTR.WriteUInt8(fn, 1, (byte)'l');
		APTR.WriteUInt8(fn, 2, (byte)'o');
		APTR.WriteUInt8(fn, 3, (byte)'a');
		APTR.WriteUInt8(fn, 4, (byte)'t');
		APTR.WriteUInt8(fn, 5, (byte)'t');
		APTR.WriteUInt8(fn, 6, (byte)'e');
		APTR.WriteUInt8(fn, 7, (byte)'x');
		APTR.WriteUInt8(fn, 8, (byte)'t');
		APTR.WriteUInt8(fn, 9, (byte)'.');
		APTR.WriteUInt8(fn, 10, (byte)'m');
		APTR.WriteUInt8(fn, 11, (byte)'u');
		APTR.WriteUInt8(fn, 12, (byte)'i');
		APTR.WriteUInt8(fn, 13, 0);
		var ft = APTR.FromPointer(ftText); // "a\nb"
		APTR.WriteUInt8(ft, 0, (byte)'a');
		APTR.WriteUInt8(ft, 1, (byte)'\n');
		APTR.WriteUInt8(ft, 2, (byte)'b');
		APTR.WriteUInt8(ft, 3, 0);
		APTR.WriteUInt32(APTR.FromPointer(ftTags), 0, 0x8042d16au);
		APTR.WriteUInt32(APTR.FromPointer(ftTags), 4, ftText);
		APTR.WriteUInt32(APTR.FromPointer(ftTags), 8, 0);
		var ftClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(ftName), APTR.Null, 0,
			APTR.FromPointer(1)).Raw;
		if (ftClass == 0) return 25;
		var floattext = MuiFloattextCore.CreateFloattext(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(ftClass),
			APTR.FromPointer(ftTags)).Raw;
		if (floattext == 0) return 26;
		if (MuiListCore.EntryCount(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(floattext)) != 2) return 27;
		var more = APTR.FromPointer(ftMore); // "\nc"
		APTR.WriteUInt8(more, 0, (byte)'\n');
		APTR.WriteUInt8(more, 1, (byte)'c');
		APTR.WriteUInt8(more, 2, 0);
		if (!MuiFloattextCore.Append(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(floattext), APTR.FromPointer(more))) return 28;
		if (MuiListCore.EntryCount(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(floattext)) != 3) return 29;
		if (!MuiFloattextCore.GetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(floattext), 0x8042d16au, out var ownedText) ||
			ownedText == 0) return 30;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(floattext))) return 31;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(list))) return 33;

		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 32;
		return 42;
	}

	// Focused MG08 List packet closure. It exercises the common List method
	// records through the standalone struct-backed collection seam, without
	// pulling Listview, Floattext, or the generic collection fallback into the
	// native qualification graph.
	public static uint CollectionListPacketRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint first = 0x00036200;
		const uint second = 0x00036220;
		const uint packet = 0x00036300;
		const uint storage = 0x00036340;
		const uint cursor = 0x00036350;
		var st = APTR.FromPointer(state);
		uint result;
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), st)) return 1;
		WriteClassId(APTR.FromPointer(className), 'L', 'i', 's', 't',
			(char)0, (char)0, (char)0, (char)0, (char)0);
		var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, st,
			APTR.FromPointer(className), APTR.Null, 0, APTR.FromPointer(1));
		if (cls.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, st, cls, APTR.Null);
		if (list.IsNull) return 3;
		WriteWord(first, (byte)'b', (byte)'r', (byte)'a', (byte)'v', (byte)'o');
		WriteWord(second, (byte)'a', (byte)'l', (byte)'p', (byte)'h', (byte)'a');

		if (!MuiListCore.InsertSingle(ref platform, st, list,
			APTR.FromPointer(first), -3) ||
			!MuiListCore.InsertSingle(ref platform, st, list,
				APTR.FromPointer(second), -3)) return 4;

		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x804280ECu);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, storage);
		if (!MuiCollectionDispatcher.TryDispatchPacket(ref platform, st, list,
			APTR.FromPointer(packet), out result) || result != first ||
			APTR.ReadUInt32(APTR.FromPointer(storage), 0) != first) return 6;

		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x804252D8u);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 1);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 1);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 0);
		if (!MuiCollectionDispatcher.TryDispatchPacket(ref platform, st, list,
			APTR.FromPointer(packet), out result) || result != 1) return 7;
		APTR.WriteUInt32(APTR.FromPointer(cursor), 0, 0xFFFFFFFFu);
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80425F17u);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, cursor);
		if (!MuiCollectionDispatcher.TryDispatchPacket(ref platform, st, list,
			APTR.FromPointer(packet), out result) || result != 1 ||
			APTR.ReadUInt32(APTR.FromPointer(cursor), 0) != 1) return 8;

		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042647Eu);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0);
		if (!MuiCollectionDispatcher.TryDispatchPacket(ref platform, st, list,
			APTR.FromPointer(packet), out result) || result != 1) return 9;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042AD89u);
		if (!MuiCollectionDispatcher.TryDispatchPacket(ref platform, st, list,
			APTR.FromPointer(packet), out result) || result != 1) return 10;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform, st, list)) return 11;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 12;
		return 42;
	}

	// Focused MG08 List advanced-packet closure. It exercises the remaining
	// fixed position/pair and image-handle records through the same standalone
	// collection seam; display, compare, and variable hook packets remain on
	// the existing dispatcher path until their records receive separate review.
	public static uint CollectionListAdvancedPacketRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint first = 0x00036200;
		const uint second = 0x00036220;
		const uint third = 0x00036240;
		const uint packet = 0x00036300;
		const uint imageObject = 0x00036340;
		var st = APTR.FromPointer(state);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), st)) return 1;
		WriteClassId(APTR.FromPointer(className), 'L', 'i', 's', 't',
			(char)0, (char)0, (char)0, (char)0, (char)0);
		var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, st,
			APTR.FromPointer(className), APTR.Null, 0, APTR.FromPointer(1));
		if (cls.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, st, cls, APTR.Null);
		if (list.IsNull) return 3;
		WriteWord(first, (byte)'a', (byte)'l', (byte)'p', (byte)'h', (byte)'a');
		WriteWord(second, (byte)'b', (byte)'r', (byte)'a', (byte)'v', (byte)'o');
		WriteWord(third, (byte)'c', (byte)'h', (byte)'a', (byte)'r', (byte)'l');

		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80426C87u);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, first);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, unchecked((uint)-3));
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 0);
		if (!MuiCollectionDispatcher.TryDispatchPacket(ref platform, st, list,
			APTR.FromPointer(packet), out var result) || result != 1) return 4;
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, second);
		if (!MuiCollectionDispatcher.TryDispatchPacket(ref platform, st, list,
			APTR.FromPointer(packet), out result) || result != 1) return 5;
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, third);
		if (!MuiCollectionDispatcher.TryDispatchPacket(ref platform, st, list,
			APTR.FromPointer(packet), out result) || result != 1) return 6;

		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x804253C2u);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 2);
		if (!MuiCollectionDispatcher.TryDispatchPacket(ref platform, st, list,
			APTR.FromPointer(packet), out result) || result != 1) return 7;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042468Cu);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 1);
		if (!MuiCollectionDispatcher.TryDispatchPacket(ref platform, st, list,
			APTR.FromPointer(packet), out result) || result != 1) return 8;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042BAABu);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, unchecked((uint)-2));
		if (!MuiCollectionDispatcher.TryDispatchPacket(ref platform, st, list,
			APTR.FromPointer(packet), out result) || result != 1) return 9;

		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80429804u);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, imageObject);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 3);
		if (!MuiCollectionDispatcher.TryDispatchPacket(ref platform, st, list,
			APTR.FromPointer(packet), out result) || result == 0) return 10;
		var image = APTR.FromPointer(result);
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80420F58u);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, image.Raw);
		if (!MuiCollectionDispatcher.TryDispatchPacket(ref platform, st, list,
			APTR.FromPointer(packet), out result) || result != 1) return 11;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform, st, list)) return 12;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 13;
		return 42;
	}

	// Focused MG08 List fixed-record closure. Construct/Destruct, Display,
	// Compare, and TestPos use the packed dispatcher records rather than raw
	// method-specific offset decoding. Variable hook payloads remain outside
	// this fixed-layout seam.
	public static uint CollectionListRecordPacketRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint first = 0x00036200;
		const uint second = 0x00036220;
		const uint packet = 0x00036300;
		const uint array = 0x00036340;
		const uint resultStorage = 0x00036380;
		const uint renderInfo = 0x00036400;
		const uint rastPort = 0x00036420;
		var st = APTR.FromPointer(state);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), st)) return 1;
		WriteClassId(APTR.FromPointer(className), 'L', 'i', 's', 't',
			(char)0, (char)0, (char)0, (char)0, (char)0);
		var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, st,
			APTR.FromPointer(className), APTR.Null, 0, APTR.FromPointer(1));
		if (cls.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, st, cls, APTR.Null);
		if (list.IsNull) return 3;
		WriteWord(first, (byte)'a', (byte)'l', (byte)'p', (byte)'h', (byte)'a');
		WriteWord(second, (byte)'b', (byte)'r', (byte)'a', (byte)'v', (byte)'o');

		// The construct/destruct records carry the entry and pool APTRs.
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042D662u);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, first);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 0);
		if (!MuiCollectionDispatcher.TryDispatchPacket(ref platform, st, list,
			APTR.FromPointer(packet), out var result) || result != first) return 4;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80427D51u);
		if (!MuiCollectionDispatcher.TryDispatchPacket(ref platform, st, list,
			APTR.FromPointer(packet), out result) || result != 1) return 5;

		// Display publishes the NULL-hook string representation as array[0],
		// followed by its terminating NULL pointer.
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80425377u);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, first);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, array);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 0);
		if (!MuiCollectionDispatcher.TryDispatchPacket(ref platform, st, list,
			APTR.FromPointer(packet), out result) || result != 1 ||
			APTR.ReadUInt32(APTR.FromPointer(array), 0) != first ||
			APTR.ReadUInt32(APTR.FromPointer(array), 4) != 0) return 6;

		// NULL-hook comparison is bounded C-string comparison.
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80421B68u);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, first);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, second);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 0);
		if (!MuiCollectionDispatcher.TryDispatchPacket(ref platform, st, list,
			APTR.FromPointer(packet), out result) || unchecked((int)result) >= 0) return 7;

		// Populate two rows through the struct-backed InsertSingle seam, then
		// publish geometry and hit-test the first cell.
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x804254D5u);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, first);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, unchecked((uint)-3));
		if (!MuiCollectionDispatcher.TryDispatchPacket(ref platform, st, list,
			APTR.FromPointer(packet), out result) || result != 1) return 8;
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, second);
		if (!MuiCollectionDispatcher.TryDispatchPacket(ref platform, st, list,
			APTR.FromPointer(packet), out result) || result != 1) return 9;
		APTR.WriteUInt32(APTR.FromPointer(renderInfo), 20, rastPort);
		if (!MuiAreaLayoutCore.Setup(ref platform, st, list,
			APTR.FromPointer(renderInfo)) ||
			!MuiListCore.Layout(ref platform, st, list, 0, 0, 80, 16)) return 10;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80425F48u);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 2);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 5);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, resultStorage);
		if (!MuiCollectionDispatcher.TryDispatchPacket(ref platform, st, list,
			APTR.FromPointer(packet), out result) || result != 1) return 11;
		if (APTR.ReadUInt32(APTR.FromPointer(resultStorage), 0) != 0) return 12;
		if (APTR.ReadUInt16(APTR.FromPointer(resultStorage), 4) != 0) return 13;

		if (!MuiCollectionLifecycle.DisposeObject(ref platform, st, list)) return 14;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 15;
		return 42;
	}

	// Focused MG08 List surface-packet record closure. Layout and AskMinMax
	// records are decoded into named structs and their guest pointers are
	// validated in the zero-relocation native seam; the full Layout/AskMinMax/
	// Draw/Set behavior remains exercised through the host dispatcher contract.
	public static uint CollectionListSurfacePacketRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint first = 0x00036200;
		const uint second = 0x00036220;
		const uint packet = 0x00036300;
		const uint storage = 0x00036340;
		const uint renderInfo = 0x00036380;
		const uint rastPort = 0x000363A0;
		var st = APTR.FromPointer(state);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), st)) return 1;
		WriteClassId(APTR.FromPointer(className), 'L', 'i', 's', 't',
			(char)0, (char)0, (char)0, (char)0, (char)0);
		var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, st,
			APTR.FromPointer(className), APTR.Null, 0, APTR.FromPointer(1));
		if (cls.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, st, cls, APTR.Null);
		if (list.IsNull) return 3;
		WriteWord(first, (byte)'a', (byte)'l', (byte)'p', (byte)'h', (byte)'a');
		WriteWord(second, (byte)'b', (byte)'r', (byte)'a', (byte)'v', (byte)'o');

		if (!MuiListCore.InsertSingle(ref platform, st, list,
			APTR.FromPointer(first), -3) ||
			!MuiListCore.InsertSingle(ref platform, st, list,
				APTR.FromPointer(second), -3)) return 4;

		APTR.WriteUInt32(APTR.FromPointer(renderInfo), 20, rastPort);
		if (!MuiAreaLayoutCore.Setup(ref platform, st, list,
			APTR.FromPointer(renderInfo))) return 6;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80423874u);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, storage);
		if (!MuiCollectionDispatcher.TryReadAskMinMaxPacket(ref platform,
			APTR.FromPointer(packet), out var askMinMax) ||
			askMinMax.Storage != storage) return 7;

		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042845Bu);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 4);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 6);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 80);
		APTR.WriteUInt32(APTR.FromPointer(packet), 16, 16);
		if (!MuiCollectionDispatcher.TryReadLayoutPacket(ref platform,
			APTR.FromPointer(packet), out var layout) ||
			layout.Left != 4 || layout.Top != 6 || layout.Width != 80 ||
			layout.Height != 16) return 8;

		if (!MuiCollectionLifecycle.DisposeObject(ref platform, st, list)) return 12;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 13;
		return 42;
	}

	// Focused MG08 composite-surface packet closure. Listview and Stringscroll
	// share the fixed MorphOS layout/draw/min-max/set record shapes; validate
	// those records through the named structs without constructing the larger
	// graphics closure. This keeps the native proof aligned with the struct-first
	// ABI boundary used by the live dispatcher.
	public static uint CollectionCompositePacketsRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint packet = 0x00036300;
		var message = APTR.FromPointer(packet);
		MuiCollectionLayoutMessage layout;
		MuiCollectionDrawMessage draw;
		MuiCollectionAskMinMaxMessage ask;
		MuiCollectionAttributeMessage attribute;

		APTR.WriteUInt32(message, 0, 0x8042845Bu);
		APTR.WriteUInt32(message, 4, 4);
		APTR.WriteUInt32(message, 8, 6);
		APTR.WriteUInt32(message, 12, 80);
		APTR.WriteUInt32(message, 16, 16);
		if (!MuiCollectionDispatcher.TryReadLayoutPacket(ref platform, message,
			out layout) || layout.Left != 4 || layout.Top != 6 ||
			layout.Width != 80 || layout.Height != 16) return 1;

		APTR.WriteUInt32(message, 0, 0x80426F3Fu);
		APTR.WriteUInt32(message, 4, 0xA5A5A5A5u);
		if (!MuiCollectionDispatcher.TryReadDrawPacket(ref platform, message,
			out draw) || draw.Flags != 0xA5A5A5A5u) return 2;

		APTR.WriteUInt32(message, 0, 0x80423874u);
		APTR.WriteUInt32(message, 4, 0x00036340u);
		if (!MuiCollectionDispatcher.TryReadAskMinMaxPacket(ref platform, message,
			out ask) || ask.Storage != 0x00036340u) return 3;

		APTR.WriteUInt32(message, 0, 0x8042549Au);
		APTR.WriteUInt32(message, 4, 0x8042D16Au);
		APTR.WriteUInt32(message, 8, 1);
		if (!MuiCollectionDispatcher.TryReadAttributePacket(ref platform, message,
			0x8042549Au, out attribute) || attribute.Attribute != 0x8042D16Au ||
			attribute.Value != 1) return 4;

		if (MuiCollectionDispatcher.TryReadLayoutPacket(ref platform,
			APTR.FromPointer(0x50FFF), out _)) return 5;
		return 42;
	}

	// Focused MG08 List edit-record closure. The four MorphOS 3.20 editing
	// packets are decoded into named structs and their signed rows/columns and
	// guest pointers are validated without constructing the editor closure.
	// Host qualification exercises the complete session state machine.
	public static uint CollectionListEditPacketRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint packet = 0x00036300;
		const uint entry = 0x00036200;
		const uint editObject = 0x00036220;
		var st = APTR.FromPointer(state);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), st)) return 1;
		WriteClassId(APTR.FromPointer(className), 'L', 'i', 's', 't',
			(char)0, (char)0, (char)0, (char)0, (char)0);
		var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, st,
			APTR.FromPointer(className), APTR.Null, 0, APTR.FromPointer(1));
		if (cls.IsNull) return 2;
		var list = MuiListCore.CreateList(ref platform, st, cls, APTR.Null);
		if (list.IsNull) return 3;

		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x804219AEu);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, unchecked((uint)-1));
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 2);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, entry);
		if (!MuiCollectionDispatcher.TryReadCreateEditObjectPacket(ref platform,
			APTR.FromPointer(packet), out var create) || create.Row != -1 ||
			create.Column != 2 || create.Entry != entry) return 4;

		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042843Du);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, unchecked((uint)-1));
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 3);
		if (!MuiCollectionDispatcher.TryReadEditPacket(ref platform,
			APTR.FromPointer(packet), out var edit) || edit.Row != -1 ||
			edit.Column != 3) return 5;

		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80423AB3u);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 4);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 1);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, entry);
		APTR.WriteUInt32(APTR.FromPointer(packet), 16, editObject);
		if (!MuiCollectionDispatcher.TryReadEditDonePacket(ref platform,
			APTR.FromPointer(packet), out var done) || done.Row != 4 ||
			done.Column != 1 || done.Entry != entry ||
			done.EditObject != editObject) return 6;

		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x804203EEu);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 1);
		if (!MuiCollectionDispatcher.TryReadEndEditPacket(ref platform,
			APTR.FromPointer(packet), out var end) || end.Mode != 1) return 7;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform, st, list)) return 8;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 9;
		return 42;
	}

	// Focused MG09 List edit-commit closure. The base String construct hook
	// owns list entries, so EditDone copies the editor contents into a guest
	// replacement record and retires the previous entry without a managed
	// object or exception path.
	public static uint CollectionListEditCommitRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint listName = 0x00036500;
		const uint stringName = 0x00036520;
		const uint source = 0x00036540;
		const uint replacementText = 0x00036560;
		const uint listTags = 0x00036580;
		const uint constructHook = 0x8042894Fu;
		const uint destructHook = 0x804297CEu;
		const uint editable = 0x8042F9B9u;
		const uint stringContents = 0x80428FFDu;
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		WriteClassId(APTR.FromPointer(listName), 'L', 'i', 's', 't',
			(char)0, (char)0, (char)0, (char)0, (char)0);
		WriteName(APTR.FromPointer(stringName), 'S', 't', 'r', 'i', 'n', 'g',
			(char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0,
			(char)0, (char)0, (char)0);
		var listClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(listName), APTR.Null, 0,
			APTR.FromPointer(1));
		var stringClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(stringName), APTR.Null, 1,
			APTR.FromPointer(1));
		if (listClass.IsNull || stringClass.IsNull) return 2;
		APTR.WriteUInt8(APTR.FromPointer(source), 0, (byte)'a');
		APTR.WriteUInt8(APTR.FromPointer(source), 1, (byte)'l');
		APTR.WriteUInt8(APTR.FromPointer(source), 2, (byte)'p');
		APTR.WriteUInt8(APTR.FromPointer(source), 3, (byte)'h');
		APTR.WriteUInt8(APTR.FromPointer(source), 4, (byte)'a');
		APTR.WriteUInt8(APTR.FromPointer(source), 5, 0);
		APTR.WriteUInt8(APTR.FromPointer(replacementText), 0, (byte)'b');
		APTR.WriteUInt8(APTR.FromPointer(replacementText), 1, (byte)'e');
		APTR.WriteUInt8(APTR.FromPointer(replacementText), 2, (byte)'t');
		APTR.WriteUInt8(APTR.FromPointer(replacementText), 3, (byte)'a');
		APTR.WriteUInt8(APTR.FromPointer(replacementText), 4, 0);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 0, constructHook);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 4, 0xFFFFFFFFu);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 8, destructHook);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 12, 0xFFFFFFFFu);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 16, 0);
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			listClass, APTR.FromPointer(listTags));
		if (list.IsNull || !MuiListCore.InsertSingle(ref platform,
			APTR.FromPointer(state), list, APTR.FromPointer(source), -3)) return 3;
		var stored = MuiListCore.GetEntry(ref platform, APTR.FromPointer(state),
			list, 0, APTR.Null);
		if (stored.IsNull || stored.Raw == source) return 4;
		if (!MuiListCore.SetAttribute(ref platform, APTR.FromPointer(state), list,
			editable, 1)) return 5;
		// Seed the guest edit-session record directly so this native closure can
		// isolate EditDone's replacement seam without pulling the renderer-heavy
		// String CreateEditObject path into the freestanding image.
		var editor = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), stringClass, APTR.Null);
		var editState = MuiHeadlessMemory.Allocate(ref platform, 24);
		if (editor.IsNull || editState.IsNull ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform,
				APTR.FromPointer(state), editor, stringContents, replacementText, false))
			return 6;
		APTR.WriteUInt32(editState, 0, 0x4C454449u);
		APTR.WriteUInt32(editState, 4, 0);
		APTR.WriteUInt32(editState, 8, 0);
		APTR.WriteUInt32(editState, 12, stored.Raw);
		APTR.WriteUInt32(editState, 16, editor.Raw);
		APTR.WriteUInt32(editState, 20, 0);
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform,
			APTR.FromPointer(state), list, 0x7F080006u, editState.Raw, false))
			return 7;
		if (!MuiListCore.EditDone(ref platform, APTR.FromPointer(state), list,
			0, 0, stored, editor)) return 8;
		var committed = MuiListCore.GetEntry(ref platform,
			APTR.FromPointer(state), list, 0, APTR.Null);
		if (committed.IsNull || committed.Raw == stored.Raw ||
			APTR.ReadUInt8(committed, 0) != (byte)'b') return 9;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list)) return 10;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 11;
		return 42;
	}

	// Focused MG09 StringArray edit-commit closure. The editor targets column 1;
	// EditDone builds a guest pointer-table source, lets the built-in StringArray
	// construct seam duplicate it, and retires the previous owned table.
	public static uint CollectionListEditStringArrayRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint listName = 0x00036500;
		const uint stringName = 0x00036520;
		const uint first = 0x00036540;
		const uint second = 0x00036560;
		const uint replacement = 0x00036580;
		const uint source = 0x000365A0;
		const uint format = 0x000365C0;
		const uint listTags = 0x000365E0;
		const uint constructHook = 0x8042894Fu;
		const uint destructHook = 0x804297CEu;
		const uint editable = 0x8042F9B9u;
		const uint stringContents = 0x80428FFDu;
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		WriteClassId(APTR.FromPointer(listName), 'L', 'i', 's', 't',
			(char)0, (char)0, (char)0, (char)0, (char)0);
		WriteName(APTR.FromPointer(stringName), 'S', 't', 'r', 'i', 'n', 'g',
			(char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0,
			(char)0, (char)0, (char)0);
		var listClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(listName), APTR.Null, 0,
			APTR.FromPointer(1));
		var stringClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(stringName), APTR.Null, 1,
			APTR.FromPointer(1));
		if (listClass.IsNull || stringClass.IsNull) return 2;
		APTR.WriteUInt8(APTR.FromPointer(first), 0, (byte)'a');
		APTR.WriteUInt8(APTR.FromPointer(first), 1, (byte)'l');
		APTR.WriteUInt8(APTR.FromPointer(first), 2, (byte)'p');
		APTR.WriteUInt8(APTR.FromPointer(first), 3, (byte)'h');
		APTR.WriteUInt8(APTR.FromPointer(first), 4, (byte)'a');
		APTR.WriteUInt8(APTR.FromPointer(first), 5, 0);
		APTR.WriteUInt8(APTR.FromPointer(second), 0, (byte)'0');
		APTR.WriteUInt8(APTR.FromPointer(second), 1, (byte)'0');
		APTR.WriteUInt8(APTR.FromPointer(second), 2, (byte)'1');
		APTR.WriteUInt8(APTR.FromPointer(second), 3, 0);
		APTR.WriteUInt8(APTR.FromPointer(replacement), 0, (byte)'0');
		APTR.WriteUInt8(APTR.FromPointer(replacement), 1, (byte)'0');
		APTR.WriteUInt8(APTR.FromPointer(replacement), 2, (byte)'2');
		APTR.WriteUInt8(APTR.FromPointer(replacement), 3, 0);
		APTR.WriteUInt32(APTR.FromPointer(source), 0, first);
		APTR.WriteUInt32(APTR.FromPointer(source), 4, second);
		APTR.WriteUInt32(APTR.FromPointer(source), 8, 0);
		APTR.WriteUInt8(APTR.FromPointer(format), 0, (byte)'C');
		APTR.WriteUInt8(APTR.FromPointer(format), 1, (byte)'O');
		APTR.WriteUInt8(APTR.FromPointer(format), 2, (byte)'L');
		APTR.WriteUInt8(APTR.FromPointer(format), 3, (byte)'=');
		APTR.WriteUInt8(APTR.FromPointer(format), 4, (byte)'0');
		APTR.WriteUInt8(APTR.FromPointer(format), 5, (byte)',');
		APTR.WriteUInt8(APTR.FromPointer(format), 6, (byte)'C');
		APTR.WriteUInt8(APTR.FromPointer(format), 7, (byte)'O');
		APTR.WriteUInt8(APTR.FromPointer(format), 8, (byte)'L');
		APTR.WriteUInt8(APTR.FromPointer(format), 9, (byte)'=');
		APTR.WriteUInt8(APTR.FromPointer(format), 10, (byte)'1');
		APTR.WriteUInt8(APTR.FromPointer(format), 11, 0);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 0, constructHook);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 4, 0xFFFFFFFEu);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 8, destructHook);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 12, 0xFFFFFFFEu);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 16, 0x80423C0Au);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 20, format);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 24, 0);
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			listClass, APTR.FromPointer(listTags));
		if (list.IsNull || !MuiListCore.InsertSingle(ref platform,
			APTR.FromPointer(state), list, APTR.FromPointer(source), -3)) return 3;
		var stored = MuiListCore.GetEntry(ref platform, APTR.FromPointer(state),
			list, 0, APTR.Null);
		if (stored.IsNull || stored.Raw == source) return 4;
		if (!MuiListCore.SetAttribute(ref platform, APTR.FromPointer(state), list,
			editable, 1)) return 5;
		var editor = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), stringClass, APTR.Null);
		var editState = MuiHeadlessMemory.Allocate(ref platform, 24);
		if (editor.IsNull || editState.IsNull ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform,
				APTR.FromPointer(state), editor, stringContents, replacement, false))
			return 6;
		APTR.WriteUInt32(editState, 0, 0x4C454449u);
		APTR.WriteUInt32(editState, 4, 0);
		APTR.WriteUInt32(editState, 8, 1);
		APTR.WriteUInt32(editState, 12, stored.Raw);
		APTR.WriteUInt32(editState, 16, editor.Raw);
		APTR.WriteUInt32(editState, 20, 0);
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform,
			APTR.FromPointer(state), list, 0x7F080006u, editState.Raw, false))
			return 7;
		if (!MuiListCore.EditDone(ref platform, APTR.FromPointer(state), list,
			0, 1, stored, editor)) return 8;
		var committed = MuiListCore.GetEntry(ref platform,
			APTR.FromPointer(state), list, 0, APTR.Null);
		if (committed.IsNull || committed.Raw == stored.Raw ||
			APTR.ReadUInt8(APTR.FromPointer(APTR.ReadUInt32(committed, 0)), 0) !=
				(byte)'a' ||
			APTR.ReadUInt8(APTR.FromPointer(APTR.ReadUInt32(committed, 4)), 2) !=
				(byte)'2') return 9;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list)) return 10;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 11;
		return 42;
	}

	// Focused MG09 editor-placement closure. A laid-out List creates its
	// default String editor through the public edit seam and publishes the
	// editor's Area rectangle for the selected row and column.
	public static uint CollectionListEditPlacementRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint listName = 0x00036500;
		const uint stringName = 0x00036520;
		const uint entry = 0x00036540;
		const uint listTags = 0x00036580;
		const uint constructHook = 0x8042894Fu;
		const uint destructHook = 0x804297CEu;
		const uint editable = 0x8042F9B9u;
		const uint minLineHeight = 0x8042D1C3u;
		const uint autoLineHeight = 0x8042BC08u;
		const uint lineHeight = 0x80425880u;
		const uint leftEdge = 0x8042BEC6u;
		const uint topEdge = 0x8042509Bu;
		const uint width = 0x8042B59Cu;
		const uint height = 0x80423237u;
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		WriteClassId(APTR.FromPointer(listName), 'L', 'i', 's', 't',
			(char)0, (char)0, (char)0, (char)0, (char)0);
		WriteName(APTR.FromPointer(stringName), 'S', 't', 'r', 'i', 'n', 'g',
			(char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0,
			(char)0, (char)0, (char)0);
		var listClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(listName), APTR.Null, 0,
			APTR.FromPointer(1));
		var stringClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(stringName), APTR.Null, 1,
			APTR.FromPointer(1));
		if (listClass.IsNull || stringClass.IsNull) return 2;
		APTR.WriteUInt8(APTR.FromPointer(entry), 0, (byte)'e');
		APTR.WriteUInt8(APTR.FromPointer(entry), 1, (byte)'\n');
		APTR.WriteUInt8(APTR.FromPointer(entry), 2, (byte)'i');
		APTR.WriteUInt8(APTR.FromPointer(entry), 3, (byte)'t');
		APTR.WriteUInt8(APTR.FromPointer(entry), 4, 0);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 0, constructHook);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 4, 0xFFFFFFFFu);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 8, destructHook);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 12, 0xFFFFFFFFu);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 16, minLineHeight);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 20, 8);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 24, autoLineHeight);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 28, 1);
		APTR.WriteUInt32(APTR.FromPointer(listTags), 32, 0);
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			listClass, APTR.FromPointer(listTags));
		if (list.IsNull) return 3;
		if (!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state), list,
			APTR.FromPointer(entry), -3)) return 4;
		if (!MuiListCore.SetAttribute(ref platform, APTR.FromPointer(state), list,
			editable, 1)) return 5;
		if (!MuiListCore.SetAttribute(ref platform, APTR.FromPointer(state), list,
			leftEdge, 10, false) ||
			!MuiListCore.SetAttribute(ref platform, APTR.FromPointer(state), list,
				topEdge, 20, false) ||
			!MuiListCore.SetAttribute(ref platform, APTR.FromPointer(state), list,
				width, 100, false) ||
			!MuiListCore.SetAttribute(ref platform, APTR.FromPointer(state), list,
				height, 24, false)) return 6;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), list, minLineHeight, out var listLineHeight) ||
			listLineHeight != 8 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform,
				APTR.FromPointer(state), list, lineHeight, out var effectiveLineHeight) ||
			effectiveLineHeight != 16) return 15;
		var editor = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			APTR.FromPointer(state), stringClass, APTR.Null);
		if (editor.IsNull || !MuiListCore.PlaceEditObject(ref platform,
			APTR.FromPointer(state), list, 0, 0, editor)) return 7;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), editor, leftEdge, out var editorLeft) ||
			editorLeft != 10) return 8;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), editor, topEdge, out var editorTop) ||
			editorTop != 20) return 9;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), editor, width, out var editorWidth) ||
			editorWidth != 100) return 10;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform,
			APTR.FromPointer(state), editor, height, out var editorHeight) ||
			editorHeight != 16) return 11;
		if (!MuiHeadlessObjectCore.DisposeObject(ref platform,
			APTR.FromPointer(state), editor)) return 12;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), list)) return 13;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 14;
		return 42;
	}

	// MG08 List image-handle native closure. CreateImage returns opaque
	// guest-resident handles, DeleteImage unlinks one handle, and object
	// disposal retires any remaining handles without disposing the caller's
	// BOOPSI image object.
	public static uint CollectionImageRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;
		const uint imageObject = 0x00036200;
		var n = APTR.FromPointer(name);
		APTR.WriteUInt8(n, 0, (byte)'L');
		APTR.WriteUInt8(n, 1, (byte)'i');
		APTR.WriteUInt8(n, 2, (byte)'s');
		APTR.WriteUInt8(n, 3, (byte)'t');
		APTR.WriteUInt8(n, 4, (byte)'.');
		APTR.WriteUInt8(n, 5, (byte)'m');
		APTR.WriteUInt8(n, 6, (byte)'u');
		APTR.WriteUInt8(n, 7, (byte)'i');
		APTR.WriteUInt8(n, 8, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1)).Raw;
		if (cl == 0) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(cl), APTR.Null).Raw;
		if (list == 0) return 3;
		var first = MuiListCore.CreateImage(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(list),
			APTR.FromPointer(imageObject), 3);
		var second = MuiListCore.CreateImage(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(list),
			APTR.FromPointer(imageObject), 7);
		if (first.IsNull || second.IsNull || first.Raw == second.Raw ||
			MuiListCore.ImageCount(ref platform, APTR.FromPointer(state),
				APTR.FromPointer(list)) != 2) return 4;
		if (!MuiListCore.DeleteImage(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(list), first) ||
			MuiListCore.ImageCount(ref platform, APTR.FromPointer(state),
				APTR.FromPointer(list)) != 1) return 5;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(list))) return 6;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 7;
		return 42;
	}

	// MG08 arbitrary-hook ABI native closure. Proves the freestanding platform's
	// CallHookPkt marshalling owns the Amiga register contract: an arbitrary List
	// construct hook is entered with A0 = hook base (so h_Data at hook+16 is
	// reachable), A2 = pool, A1 = entry. The adapter records the three delivered
	// registers into the hook's h_Data block; this closure asserts each was
	// delivered and that the constructed entry (h_Data) is stored. Allocation-free
	// at the managed level; no host string or callback service is reached.
	public static uint CollectionHookAbiRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint name = 0x00036100;   // "List.mui"
		const uint hook = 0x00036200;
		const uint hookData = 0x00036240;
		const uint pool = 0x00036280;
		const uint entry = 0x000362A0;
		var n = APTR.FromPointer(name);
		APTR.WriteUInt8(n, 0, (byte)'L');
		APTR.WriteUInt8(n, 1, (byte)'i');
		APTR.WriteUInt8(n, 2, (byte)'s');
		APTR.WriteUInt8(n, 3, (byte)'t');
		APTR.WriteUInt8(n, 4, (byte)'.');
		APTR.WriteUInt8(n, 5, (byte)'m');
		APTR.WriteUInt8(n, 6, (byte)'u');
		APTR.WriteUInt8(n, 7, (byte)'i');
		APTR.WriteUInt8(n, 8, 0);
		APTR.WriteUInt32(APTR.FromPointer(hook), 8, 0xE1);      // h_Entry != 0
		APTR.WriteUInt32(APTR.FromPointer(hook), 16, hookData); // h_Data
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(name), APTR.Null, 0,
			APTR.FromPointer(1)).Raw;
		if (cl == 0) return 2;
		var list = MuiListCore.CreateList(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(cl), APTR.Null).Raw;
		if (list == 0) return 3;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(list), 0x8042894fu, hook,
			false)) return 4; // MUIA_List_ConstructHook
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(list), 0x80423431u, pool,
			false)) return 5; // MUIA_List_Pool
		if (!MuiListCore.InsertSingle(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(list), APTR.FromPointer(entry), -3)) return 6;
		if (MuiListCore.GetEntry(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(list), 0, APTR.Null).Raw != hookData) return 7;
		if (APTR.ReadUInt32(APTR.FromPointer(hookData), 0) != hook) return 8;  // A0
		if (APTR.ReadUInt32(APTR.FromPointer(hookData), 4) != pool) return 9;  // A2
		if (APTR.ReadUInt32(APTR.FromPointer(hookData), 8) != entry) return 10; // A1
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(list))) return 11;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 12;
		return 42;
	}

	// MG08 Dirlist.mui/Volumelist.mui native closure. Exercises the shared List
	// backbone directory subclasses against the freestanding directory
	// capability (an empty filesystem). A Dirlist with no directory is Invalid
	// and empty; assigning a directory drives a bounded synchronous scan that
	// yields a valid, empty listing; a Volumelist enumerates the (empty) volume
	// set. Everything is disposed cleanly. Allocation-free at the managed level.
	public static uint CollectionDirlistRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint dirName = 0x00036100;   // "Dirlist.mui"
		const uint volName = 0x00036140;   // "Volumelist.mui"
		const uint path = 0x00036200;      // "RAM:"
		const uint Status = 0x804240deu;   // MUIA_Dirlist_Status
		const uint DirectoryAttr = 0x8042ea41u; // MUIA_Dirlist_Directory

		var dn = APTR.FromPointer(dirName);
		APTR.WriteUInt8(dn, 0, (byte)'D');
		APTR.WriteUInt8(dn, 1, (byte)'i');
		APTR.WriteUInt8(dn, 2, (byte)'r');
		APTR.WriteUInt8(dn, 3, (byte)'l');
		APTR.WriteUInt8(dn, 4, (byte)'i');
		APTR.WriteUInt8(dn, 5, (byte)'s');
		APTR.WriteUInt8(dn, 6, (byte)'t');
		APTR.WriteUInt8(dn, 7, (byte)'.');
		APTR.WriteUInt8(dn, 8, (byte)'m');
		APTR.WriteUInt8(dn, 9, (byte)'u');
		APTR.WriteUInt8(dn, 10, (byte)'i');
		APTR.WriteUInt8(dn, 11, 0);
		var vn = APTR.FromPointer(volName);
		APTR.WriteUInt8(vn, 0, (byte)'V');
		APTR.WriteUInt8(vn, 1, (byte)'o');
		APTR.WriteUInt8(vn, 2, (byte)'l');
		APTR.WriteUInt8(vn, 3, (byte)'u');
		APTR.WriteUInt8(vn, 4, (byte)'m');
		APTR.WriteUInt8(vn, 5, (byte)'e');
		APTR.WriteUInt8(vn, 6, (byte)'l');
		APTR.WriteUInt8(vn, 7, (byte)'i');
		APTR.WriteUInt8(vn, 8, (byte)'s');
		APTR.WriteUInt8(vn, 9, (byte)'t');
		APTR.WriteUInt8(vn, 10, (byte)'.');
		APTR.WriteUInt8(vn, 11, (byte)'m');
		APTR.WriteUInt8(vn, 12, (byte)'u');
		APTR.WriteUInt8(vn, 13, (byte)'i');
		APTR.WriteUInt8(vn, 14, 0);
		var p = APTR.FromPointer(path);
		APTR.WriteUInt8(p, 0, (byte)'R');
		APTR.WriteUInt8(p, 1, (byte)'A');
		APTR.WriteUInt8(p, 2, (byte)'M');
		APTR.WriteUInt8(p, 3, (byte)':');
		APTR.WriteUInt8(p, 4, 0);

		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var dirClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(dirName), APTR.Null, 0,
			APTR.FromPointer(1)).Raw;
		if (dirClass == 0) return 2;
		var volClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(volName), APTR.Null, 0,
			APTR.FromPointer(1)).Raw;
		if (volClass == 0) return 3;

		// A Dirlist with no directory is Invalid and empty.
		var dirlist = MuiDirlistCore.CreateDirlist(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(dirClass), APTR.Null).Raw;
		if (dirlist == 0) return 4;
		if (MuiListCore.EntryCount(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(dirlist)) != 0) return 5;
		if (!MuiDirlistCore.GetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(dirlist), Status, out var invalid) || invalid != 0)
			return 6;

		// Assigning a directory drives a bounded scan -> valid, empty listing.
		if (!MuiDirlistCore.SetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(dirlist), DirectoryAttr, path)) return 7;
		if (!MuiDirlistCore.GetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(dirlist), Status, out var valid) || valid != 2)
			return 8;
		if (MuiListCore.EntryCount(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(dirlist)) != 0) return 9;
		if (!MuiDirlistCore.ReRead(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(dirlist))) return 10;

		// A Volumelist enumerates the (empty) volume set: valid, empty.
		var volumes = MuiVolumelistCore.CreateVolumelist(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(volClass), APTR.Null).Raw;
		if (volumes == 0) return 11;
		if (!MuiVolumelistCore.GetAttribute(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(volumes), Status, out var volValid) || volValid != 2)
			return 12;
		if (MuiListCore.EntryCount(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(volumes)) != 0) return 13;
		if (!MuiVolumelistCore.Populate(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(volumes))) return 14;

		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(dirlist))) return 15;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(volumes))) return 16;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 17;
		return 42;
	}

	// Focused MG08 Dirlist packet closure. It keeps only the Dirlist object and
	// the standalone dispatcher in the reachable graph, proving the named Set
	// and ListGetEntry records without pulling the broader collection fallback
	// into the MC68020 branch window.
	public static uint CollectionDirlistPacketRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint path = 0x00036200;
		const uint packet = 0x00036300;
		const uint directory = 0x8042ea41u;
		var st = APTR.FromPointer(state);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), st)) return 1;
		WriteClassId(APTR.FromPointer(className), 'D', 'i', 'r', 'l', 'i', 's',
			't', (char)0, (char)0);
		var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, st,
			APTR.FromPointer(className), APTR.Null, 0, APTR.FromPointer(1));
		if (cls.IsNull) return 2;
		var obj = MuiDirlistCore.CreateDirlist(ref platform, st, cls, APTR.Null);
		if (obj.IsNull) return 3;
		APTR.WriteUInt8(APTR.FromPointer(path), 0, (byte)'R');
		APTR.WriteUInt8(APTR.FromPointer(path), 1, (byte)'A');
		APTR.WriteUInt8(APTR.FromPointer(path), 2, (byte)'M');
		APTR.WriteUInt8(APTR.FromPointer(path), 3, (byte)':');
		APTR.WriteUInt8(APTR.FromPointer(path), 4, 0);
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042549A);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, directory);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, path);
		if (!MuiDirlistDispatcher.TryDispatchPacket(ref platform, st, obj,
			APTR.FromPointer(packet), out var result) || result != 1) return 4;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x804280ECu);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 0);
		if (!MuiDirlistDispatcher.TryDispatchPacket(ref platform, st, obj,
			APTR.FromPointer(packet), out result) || result != 0) return 5;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform, st, obj)) return 6;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 7;
		return 42;
	}

	// MG08 Stringscroll.mui native closure. Exercises class identification,
	// copied guest string ownership, bounded layout/min-max metrics, and clamped
	// pixel scrolling against the freestanding platform without a graphics or
	// host-runtime dependency.
	public static uint CollectionStringscrollRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100;
		const uint text = 0x00036200;
		const uint tags = 0x00036300;
		const uint minMax = 0x00036400;
		const uint stringAttribute = 0x804256a2u;
		APTR.WriteUInt8(APTR.FromPointer(className), 0, (byte)'S');
		APTR.WriteUInt8(APTR.FromPointer(className), 1, (byte)'t');
		APTR.WriteUInt8(APTR.FromPointer(className), 2, (byte)'r');
		APTR.WriteUInt8(APTR.FromPointer(className), 3, (byte)'i');
		APTR.WriteUInt8(APTR.FromPointer(className), 4, (byte)'n');
		APTR.WriteUInt8(APTR.FromPointer(className), 5, (byte)'g');
		APTR.WriteUInt8(APTR.FromPointer(className), 6, (byte)'s');
		APTR.WriteUInt8(APTR.FromPointer(className), 7, (byte)'c');
		APTR.WriteUInt8(APTR.FromPointer(className), 8, (byte)'r');
		APTR.WriteUInt8(APTR.FromPointer(className), 9, (byte)'o');
		APTR.WriteUInt8(APTR.FromPointer(className), 10, (byte)'l');
		APTR.WriteUInt8(APTR.FromPointer(className), 11, (byte)'l');
		APTR.WriteUInt8(APTR.FromPointer(className), 12, (byte)'.');
		APTR.WriteUInt8(APTR.FromPointer(className), 13, (byte)'m');
		APTR.WriteUInt8(APTR.FromPointer(className), 14, (byte)'u');
		APTR.WriteUInt8(APTR.FromPointer(className), 15, (byte)'i');
		APTR.WriteUInt8(APTR.FromPointer(className), 16, 0);
		var source = APTR.FromPointer(text);
		APTR.WriteUInt8(source, 0, (byte)'0');
		APTR.WriteUInt8(source, 1, (byte)'1');
		APTR.WriteUInt8(source, 2, (byte)'2');
		APTR.WriteUInt8(source, 3, (byte)'3');
		APTR.WriteUInt8(source, 4, (byte)'4');
		APTR.WriteUInt8(source, 5, (byte)'5');
		APTR.WriteUInt8(source, 6, (byte)'6');
		APTR.WriteUInt8(source, 7, (byte)'7');
		APTR.WriteUInt8(source, 8, (byte)'8');
		APTR.WriteUInt8(source, 9, (byte)'9');
		APTR.WriteUInt8(source, 10, (byte)'a');
		APTR.WriteUInt8(source, 11, (byte)'b');
		APTR.WriteUInt8(source, 12, (byte)'c');
		APTR.WriteUInt8(source, 13, (byte)'d');
		APTR.WriteUInt8(source, 14, (byte)'e');
		APTR.WriteUInt8(source, 15, (byte)'f');
		APTR.WriteUInt8(source, 16, 0);
		APTR.WriteUInt32(APTR.FromPointer(tags), 0, stringAttribute);
		APTR.WriteUInt32(APTR.FromPointer(tags), 4, text);
		APTR.WriteUInt32(APTR.FromPointer(tags), 8, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(className), APTR.Null, 0,
			APTR.FromPointer(1)).Raw;
		if (classRecord == 0) return 2;
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(classRecord),
			APTR.FromPointer(tags)).Raw;
		if (obj == 0) return 3;
		if (MuiListCore.Classify(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(obj)) != MuiCollectionClass.Stringscroll) return 4;
		if (!MuiStringscrollCore.Layout(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(obj), 0, 0, 80, 24)) return 5;
		if (!MuiStringscrollCore.SetScroll(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(obj), 999, 999)) return 6;
		if (!MuiStringscrollCore.GetScrollState(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(obj), out var x, out var y,
			out var maxX, out var maxY) || x != maxX || y != maxY) return 7;
		if (!MuiStringscrollCore.AskMinMax(ref platform, APTR.FromPointer(state),
			APTR.FromPointer(obj), APTR.FromPointer(minMax))) return 8;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(obj))) return 9;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 10;
		return 42;
	}

	// MG08 external Listtree.mcc native closure. Registers "Listtree.mcc" as an
	// external (never built-in) class through the standalone Listtree core,
	// builds a small tree of fixed guest node records, and exercises the
	// documented topology, visible traversal, sort, move, rename and recursive
	// remove semantics purely through guest memory. Allocation-free at the
	// managed level; identity is verified through node pointers so no host string
	// service is reached.
	public static uint CollectionListtreeRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint className = 0x00036100; // "Listtree.mcc"
		const uint boopsi = 0x00036220;    // opaque external class pointer
		const uint packet = 0x00036300;
		const uint storage = 0x00036340;
		const uint nRoot = 0x00036240;
		const uint nB = 0x00036260;
		const uint nA = 0x00036280;
		const uint nP2 = 0x000362A0;
		const uint nRen = 0x000362C0;
		const uint Root = 0;
		const uint Tail = 0xFFFFFFFFu;

		var cn = APTR.FromPointer(className);
		APTR.WriteUInt8(cn, 0, (byte)'L');
		APTR.WriteUInt8(cn, 1, (byte)'i');
		APTR.WriteUInt8(cn, 2, (byte)'s');
		APTR.WriteUInt8(cn, 3, (byte)'t');
		APTR.WriteUInt8(cn, 4, (byte)'t');
		APTR.WriteUInt8(cn, 5, (byte)'r');
		APTR.WriteUInt8(cn, 6, (byte)'e');
		APTR.WriteUInt8(cn, 7, (byte)'e');
		APTR.WriteUInt8(cn, 8, (byte)'.');
		APTR.WriteUInt8(cn, 9, (byte)'m');
		APTR.WriteUInt8(cn, 10, (byte)'c');
		APTR.WriteUInt8(cn, 11, (byte)'c');
		APTR.WriteUInt8(cn, 12, 0);
		WriteWord(nRoot, (byte)'r', (byte)'o', (byte)'o', (byte)'t', 0);
		var b = APTR.FromPointer(nB);
		APTR.WriteUInt8(b, 0, (byte)'b');
		APTR.WriteUInt8(b, 1, 0);
		var a = APTR.FromPointer(nA);
		APTR.WriteUInt8(a, 0, (byte)'a');
		APTR.WriteUInt8(a, 1, 0);
		var p2n = APTR.FromPointer(nP2);
		APTR.WriteUInt8(p2n, 0, (byte)'p');
		APTR.WriteUInt8(p2n, 1, (byte)'2');
		APTR.WriteUInt8(p2n, 2, 0);
		WriteWord(nRen, (byte)'r', (byte)'e', (byte)'n', (byte)'m', (byte)'d');

		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), APTR.FromPointer(state))) return 1;
		var cl = MuiListtreeCore.RegisterListtreeExternalClass(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(className),
			APTR.FromPointer(boopsi), APTR.Null).Raw;
		if (cl == 0) return 2;
		if (!MuiListtreeCore.ClassRecordIsListtree(ref platform,
			APTR.FromPointer(cl))) return 3;
		var tree = MuiListtreeCore.CreateListtree(ref platform,
			APTR.FromPointer(state), APTR.FromPointer(cl), APTR.Null).Raw;
		if (tree == 0) return 4;
		var t = APTR.FromPointer(tree);
		// Struct-first public packet boundary for Set/Get is exercised before
		// topology mutation; the external dispatcher remains self-contained.
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042549A);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, MuiListtreeCore.Quiet);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 1);
		if (MuiListtreeDispatcher.Dispatch(ref platform,
			APTR.FromPointer(state), t, APTR.FromPointer(packet)) != 1) return 24;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x80420371);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, MuiListtreeCore.Quiet);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, storage);
		if (MuiListtreeDispatcher.Dispatch(ref platform,
			APTR.FromPointer(state), t, APTR.FromPointer(packet)) != 1 ||
			APTR.ReadUInt32(APTR.FromPointer(storage), 0) != 1) return 25;

		var root = MuiListtreeCore.Insert(ref platform, APTR.FromPointer(state), t,
			APTR.FromPointer(nRoot), APTR.Null, APTR.FromPointer(Root),
			APTR.FromPointer(Tail), 0);
		if (root.IsNull) return 5;
		var bn = MuiListtreeCore.Insert(ref platform, APTR.FromPointer(state), t,
			APTR.FromPointer(nB), APTR.Null, root, APTR.FromPointer(Tail), 0);
		var an = MuiListtreeCore.Insert(ref platform, APTR.FromPointer(state), t,
			APTR.FromPointer(nA), APTR.Null, root, APTR.FromPointer(Tail), 0);
		if (bn.IsNull || an.IsNull) return 6;
		if (MuiListtreeCore.TotalNodes(ref platform, APTR.FromPointer(state), t)
			!= 3) return 7;
		if (MuiListtreeCore.ChildCount(ref platform, root) != 2) return 8;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, MuiListtreeCore.MethodGetNr);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, root.Raw);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 1u << 15);
		if (MuiListtreeDispatcher.Dispatch(ref platform,
			APTR.FromPointer(state), t, APTR.FromPointer(packet)) != 3) return 26;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiListtreeCore.MethodGetEntry);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, root.Raw);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 0);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 0);
		if (MuiListtreeDispatcher.Dispatch(ref platform,
			APTR.FromPointer(state), t, APTR.FromPointer(packet)) != bn.Raw) return 27;
		// Inserted order is b, a.
		if (MuiListtreeCore.GetEntry(ref platform, APTR.FromPointer(state), t, root,
			0, 0).Raw != bn.Raw) return 9;
		// Default LeavesBottom sort orders the leaves alphabetically: a, b.
		if (!MuiListtreeCore.Sort(ref platform, APTR.FromPointer(state), t, root,
			0)) return 10;
		if (MuiListtreeCore.GetEntry(ref platform, APTR.FromPointer(state), t, root,
			0, 0).Raw != an.Raw) return 11;
		// Open the node so the two children join the display list.
		if (!MuiListtreeCore.Open(ref platform, APTR.FromPointer(state), t,
			APTR.FromPointer(Root), root, 0)) return 12;
		if (MuiListtreeCore.VisibleCount(ref platform, APTR.FromPointer(state), t)
			!= 3) return 13;
		// CountAll spans the whole tree.
		if (MuiListtreeCore.GetNr(ref platform, APTR.FromPointer(state), t, root,
			1u << 15) != 3) return 14;
		// Reparent leaf a under a new sibling p2.
		var p2 = MuiListtreeCore.Insert(ref platform, APTR.FromPointer(state), t,
			APTR.FromPointer(nP2), APTR.Null, APTR.FromPointer(Root),
			APTR.FromPointer(Tail), 0);
		if (p2.IsNull) return 15;
		if (!MuiListtreeCore.Move(ref platform, APTR.FromPointer(state), t, root,
			an, p2, APTR.FromPointer(Tail), 0)) return 16;
		if (MuiListtreeCore.ChildCount(ref platform, p2) != 1 ||
			MuiListtreeCore.ChildCount(ref platform, root) != 1) return 17;
		// Rename the surviving child of root.
		if (!MuiListtreeCore.Rename(ref platform, APTR.FromPointer(state), t, bn,
			APTR.FromPointer(nRen), 1u << 9)) return 18;
		// Remove the root subtree recursively; p2 and a survive.
		if (!MuiListtreeCore.Remove(ref platform, APTR.FromPointer(state), t,
			APTR.FromPointer(Root), root, 0)) return 19;
		if (MuiListtreeCore.TotalNodes(ref platform, APTR.FromPointer(state), t)
			!= 2) return 20;
		if (MuiListtreeCore.RootCount(ref platform, APTR.FromPointer(state), t)
			!= 1) return 21;
		if (!MuiCollectionLifecycle.DisposeObject(ref platform,
			APTR.FromPointer(state), t)) return 22;
		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 23;
		return 42;
	}

	private static void WriteWord(uint addr, byte c0, byte c1, byte c2, byte c3,
		byte c4)
	{
		var a = APTR.FromPointer(addr);
		APTR.WriteUInt8(a, 0, c0);
		APTR.WriteUInt8(a, 1, c1);
		APTR.WriteUInt8(a, 2, c2);
		APTR.WriteUInt8(a, 3, c3);
		if (c4 == 0) { APTR.WriteUInt8(a, 4, 0); return; }
		APTR.WriteUInt8(a, 4, c4);
		APTR.WriteUInt8(a, 5, 0);
	}

	private static void WriteName(ref MuiNativeHeadlessPlatform platform, uint addr,
		byte c0, byte c1, byte c2, byte c3, byte c4, byte c5, byte c6)
	{
		var name = APTR.FromPointer(addr);
		APTR.WriteUInt8(name, 0, c0);
		APTR.WriteUInt8(name, 1, c1);
		APTR.WriteUInt8(name, 2, c2);
		APTR.WriteUInt8(name, 3, c3);
		APTR.WriteUInt8(name, 4, c4);
		APTR.WriteUInt8(name, 5, c5);
		APTR.WriteUInt8(name, 6, c6);
		APTR.WriteUInt8(name, 7, (byte)'.');
		APTR.WriteUInt8(name, 8, (byte)'m');
		APTR.WriteUInt8(name, 9, (byte)'u');
		APTR.WriteUInt8(name, 10, (byte)'i');
		APTR.WriteUInt8(name, 11, 0);
	}

	private static void WriteName2(ref MuiNativeHeadlessPlatform platform, uint addr,
		byte c0, byte c1, byte c2, byte c3, byte c4)
	{
		var name = APTR.FromPointer(addr);
		APTR.WriteUInt8(name, 0, c0);
		APTR.WriteUInt8(name, 1, c1);
		APTR.WriteUInt8(name, 2, c2);
		APTR.WriteUInt8(name, 3, c3);
		APTR.WriteUInt8(name, 4, c4);
		APTR.WriteUInt8(name, 5, (byte)'.');
		APTR.WriteUInt8(name, 6, (byte)'m');
		APTR.WriteUInt8(name, 7, (byte)'u');
		APTR.WriteUInt8(name, 8, (byte)'i');
		APTR.WriteUInt8(name, 9, 0);
	}

	private static void WriteName2(ref MuiNativeHeadlessPlatform platform, uint addr,
		byte c0, byte c1, byte c2, byte c3, byte c4, byte c5)
	{
		var name = APTR.FromPointer(addr);
		APTR.WriteUInt8(name, 0, c0);
		APTR.WriteUInt8(name, 1, c1);
		APTR.WriteUInt8(name, 2, c2);
		APTR.WriteUInt8(name, 3, c3);
		APTR.WriteUInt8(name, 4, c4);
		APTR.WriteUInt8(name, 5, c5);
		APTR.WriteUInt8(name, 6, (byte)'.');
		APTR.WriteUInt8(name, 7, (byte)'m');
		APTR.WriteUInt8(name, 8, (byte)'u');
		APTR.WriteUInt8(name, 9, (byte)'i');
		APTR.WriteUInt8(name, 10, 0);
	}

	public static uint InitializeRoot()
	{
		MuiMasterPrivateRoot root;
		root.ClassRegistry = 0;
		root.AllocationPolicy = 0;
		root.ErrorState = 0;
		root.ApplicationHead = 0;
		root.ExternalClassHead = 0;
		root.CallbackState = 0;
		root.LoaderState = 0;
		root.RegistryGeneration = 1;
		root.ActiveDispatchDepth = 0;
		root.ActiveCallbackDepth = 0;
		root.Flags = 0;
		root.Reserved = 0;
		return root.RegistryGeneration;
	}

	// MG09 typed guest-state header closure. The state codec validates the
	// fixed-width record and rejects a corrupted magic value without entering
	// the broader headless object graph.
	public static uint HeadlessStatePacketRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var state = APTR.FromPointer(0x00038000);
		if (!MuiHeadlessStatePacketCore.WriteRecord(ref platform, state,
			0x4D554934u, 1, APTR.FromPointer(0x00038010),
			APTR.FromPointer(0x00038020), 7, 2, 3, 4)) return 1;
		if (MuiHeadlessStatePacketCore.DispatchRecord(ref platform, state) !=
			0x32) return 2;
		APTR.WriteUInt32(state, 0, 0);
		if (MuiHeadlessStatePacketCore.DispatchRecord(ref platform, state) != 0)
			return 3;
		return 42;
	}

	// MG09 typed class-service record closure. Service state, class lease, and
	// MUI_CustomClass fields round-trip through their named codecs without
	// entering the loader or custom-class lifecycle.
	public static uint ClassServiceRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var state = APTR.FromPointer(0x00038900);
		var lease = APTR.FromPointer(0x00038930);
		var custom = APTR.FromPointer(0x000389A0);
		if (!MuiClassServiceRecordPacketCore.WriteState(ref platform, state,
			0x4D554339, APTR.FromPointer(0x00038910),
			APTR.FromPointer(0x00038920), 3) ||
			MuiClassServiceRecordPacketCore.DispatchState(ref platform, state) !=
			0x4D55430A) return 1;
		if (!MuiClassServiceRecordPacketCore.WriteLease(ref platform, lease,
			APTR.FromPointer(0x00038930), 1, APTR.FromPointer(0x00038940),
			APTR.FromPointer(0x00038950), APTR.FromPointer(0x00038960), 2,
			APTR.FromPointer(0x00038970), APTR.FromPointer(0x00038980),
			APTR.FromPointer(0x00038990), 4, 5) ||
			MuiClassServiceRecordPacketCore.DispatchLease(ref platform, lease) !=
			0x00038922) return 2;
		if (!MuiClassServiceRecordPacketCore.WriteCustomClass(ref platform,
			custom, APTR.FromPointer(0x000389A0),
			APTR.FromPointer(0x000389B0)) ||
			MuiClassServiceRecordPacketCore.DispatchCustomClass(ref platform,
			custom) != 0x00000010) return 3;
		return 42;
	}

	// MG09 typed class-registry record closure. It proves the 28-byte class
	// layout, including the reserved UWORD before flags, without allocating a
	// BOOPSI class or entering the object graph.
	public static uint HeadlessClassPacketRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var record = APTR.FromPointer(0x00038100);
		if (!MuiHeadlessClassPacketCore.WriteRecord(ref platform, record,
			APTR.FromPointer(0x00038110), APTR.FromPointer(0x00038120),
			APTR.FromPointer(0x00038130), APTR.FromPointer(0x00038140),
			12, 7, 3)) return 1;
		if (MuiHeadlessClassPacketCore.DispatchRecord(ref platform, record) !=
			0x00000078) return 2;
		return 42;
	}

	// MG09 typed object-record closure. It proves the complete 64-byte
	// headless object layout, including all named pointer links and scalar
	// lifecycle fields, without allocating a managed object graph.
	public static uint HeadlessObjectPacketRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var record = APTR.FromPointer(0x00038200);
		if (!MuiHeadlessObjectPacketCore.WriteLinkFieldsA(ref platform, record,
			APTR.FromPointer(0x00038110), APTR.FromPointer(0x00038120),
			APTR.FromPointer(0x00038130), APTR.FromPointer(0x00038140),
			APTR.FromPointer(0x00038150))) return 1;
		if (!MuiHeadlessObjectPacketCore.WriteLinkFieldsB(ref platform, record,
			APTR.FromPointer(0x00038160),
			APTR.FromPointer(0x00038170), APTR.FromPointer(0x00038180),
			APTR.FromPointer(0x00038190), APTR.FromPointer(0x000381A0))) return 2;
		if (!MuiHeadlessObjectPacketCore.WriteScalarFields(ref platform, record,
			2, 3, 4, 5, 6, 7)) return 3;
		if (MuiHeadlessObjectPacketCore.DispatchRecord(ref platform, record) !=
			0x000000B1) return 4;
		return 42;
	}

	// MG09 typed attribute-node closure. Next, identifier, value, and generation
	// round-trip through the named 16-byte attribute codec.
	public static uint AttributeRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var record = APTR.FromPointer(0x00038500);
		if (!MuiHeadlessAttributePacketCore.WriteRecord(ref platform, record,
			APTR.FromPointer(0x00038510), 2, 3, 4)) return 1;
		if (MuiHeadlessAttributePacketCore.DispatchRecord(ref platform, record) !=
			0x00038515) return 2;
		return 42;
	}

	// MG09 typed Family child-node closure. Next, Previous, Object, and Owner
	// round-trip through the shared 16-byte child-list codec used by live and
	// packet-only Family topology.
	public static uint ChildRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var record = APTR.FromPointer(0x00038600);
		if (!MuiHeadlessChildPacketCore.WriteRecord(ref platform, record,
			APTR.FromPointer(0x00038610), APTR.FromPointer(0x00038620),
			APTR.FromPointer(0x00038630), APTR.FromPointer(0x00038640))) return 1;
		if (MuiHeadlessChildPacketCore.DispatchRecord(ref platform, record) !=
			0x00000040) return 2;
		return 42;
	}

	// MG09 typed Store/Dataspace record closure. The linked pointer and scalar
	// fields round-trip through the shared record codec used by persistence.
	public static uint StoreRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var record = APTR.FromPointer(0x00038700);
		if (!MuiStoreRecordPacketCore.WriteRecord(ref platform, record,
			APTR.FromPointer(0x00038710), 2, APTR.FromPointer(0x00038720),
			3, 4, 5)) return 1;
		if (MuiStoreRecordPacketCore.DispatchRecord(ref platform, record) !=
			0x00000030) return 2;
		return 42;
	}

	// MG09 typed notification-header closure. The fixed fields round-trip
	// through the codec; variable follow-up payload storage remains owned by
	// NotifyCore and is not part of this header proof.
	public static uint NotificationRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var record = APTR.FromPointer(0x00038800);
		if (!MuiHeadlessNotificationPacketCore.WriteRecord(ref platform, record,
			APTR.FromPointer(0x00038810), 2, 3, 4,
			APTR.FromPointer(0x00038820), 5, 6, 7)) return 1;
		if (MuiHeadlessNotificationPacketCore.DispatchRecord(ref platform,
			record) != 0x00000031) return 2;
		return 42;
	}

	// MG09 typed semaphore-field closure. Owner, recursive depth, and shared
	// count round-trip through the named object codec without a managed lock.
	public static uint SemaphoreObjectRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var record = APTR.FromPointer(0x00038300);
		if (!MuiSemaphorePacketCore.WriteState(ref platform, record,
			APTR.FromPointer(0x00038310), 2, 3)) return 1;
		if (MuiSemaphorePacketCore.DispatchState(ref platform, record) !=
			0x00038311) return 2;
		return 42;
	}

	// MG09 typed Store/Dataspace ownership-link closure. The object-owned Stores
	// head is round-tripped through the same record codec used by StoreCore.
	public static uint StoreObjectRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var record = APTR.FromPointer(0x00038400);
		var stores = APTR.FromPointer(0x00038410);
		if (!MuiStoreObjectRecordPacketCore.WriteStores(ref platform, record,
			stores)) return 1;
		if (MuiStoreObjectRecordPacketCore.DispatchStores(ref platform, record)
			.Raw != stores.Raw) return 2;
		return 42;
	}

	// MG09 custom-class / external-class service gateway closure. Exercises the
	// full custom-class lifecycle without a library loader: builtin super
	// resolution through GetClass, public (A6-bound) and private-mcc-super custom
	// class creation, outstanding-object and sub-class deletion guards, and
	// named super-class lease release. Returns 42 on success.
	public static uint ClassServiceRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var headlessState = APTR.FromPointer(0x00036000);
		var serviceState = APTR.FromPointer(0x00036040);
		var areaName = APTR.FromPointer(0x00036100);
		var dispatcher = APTR.FromPointer(0x00036200);
		var dispatcher2 = APTR.FromPointer(0x00036240);
		var libraryBase = APTR.FromPointer(0x00036280);
		APTR.WriteUInt8(areaName, 0, (byte)'A');
		APTR.WriteUInt8(areaName, 1, (byte)'r');
		APTR.WriteUInt8(areaName, 2, (byte)'e');
		APTR.WriteUInt8(areaName, 3, (byte)'a');
		APTR.WriteUInt8(areaName, 4, (byte)'.');
		APTR.WriteUInt8(areaName, 5, (byte)'m');
		APTR.WriteUInt8(areaName, 6, (byte)'u');
		APTR.WriteUInt8(areaName, 7, (byte)'i');
		APTR.WriteUInt8(areaName, 8, 0);

		if (!MuiHeadlessObjectCore.Initialize(ref platform, headlessState))
			return 1;
		if (!MuiClassServiceCore.Initialize(ref platform, serviceState,
			headlessState)) return 2;
		var area = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			headlessState, areaName, APTR.Null, 8, APTR.FromPointer(0xC001));
		if (area.IsNull) return 3;
		var superPtr = MuiHeadlessObjectCore.ClassPointer(ref platform, area);

		// GetClass on a builtin resolves without the loader.
		var resolved = MuiClassServiceCore.GetClass(ref platform, serviceState,
			areaName);
		if (resolved.Raw != superPtr.Raw) return 4;
		if (!MuiClassServiceCore.FreeClass(ref platform, serviceState, resolved))
			return 5;

		// Public custom class over the named builtin super; A6 must bind the base.
		var mcc = MuiClassServiceCore.CreateCustomClass(ref platform, serviceState,
			libraryBase, areaName, APTR.Null, 96, dispatcher);
		if (mcc.IsNull) return 6;
		if (APTR.ReadUInt32(mcc, 20) != superPtr.Raw) return 7;
		var classPtr = APTR.FromPointer(APTR.ReadUInt32(mcc, 24));
		if (APTR.ReadUInt32(classPtr, 12) != libraryBase.Raw) return 8;

		// Outstanding object blocks deletion, then permits it once disposed.
		var obj = MuiClassServiceCore.CreateCustomObject(ref platform, serviceState,
			mcc, APTR.Null);
		if (obj.IsNull) return 9;
		if (MuiClassServiceCore.DeleteCustomClass(ref platform, serviceState, mcc))
			return 10;
		if (!MuiClassServiceCore.DisposeCustomObject(ref platform, serviceState,
			mcc, obj)) return 11;
		if (!MuiClassServiceCore.DeleteCustomClass(ref platform, serviceState, mcc))
			return 12;

		// Private parent/child over a private mcc super; sub class blocks deletion.
		var parent = MuiClassServiceCore.CreateCustomClass(ref platform,
			serviceState, APTR.Null, areaName, APTR.Null, 32, dispatcher);
		if (parent.IsNull) return 13;
		var child = MuiClassServiceCore.CreateCustomClass(ref platform, serviceState,
			APTR.Null, APTR.Null, parent, 48, dispatcher2);
		if (child.IsNull) return 14;
		if (APTR.ReadUInt32(child, 20) != APTR.ReadUInt32(parent, 24)) return 15;
		if (MuiClassServiceCore.DeleteCustomClass(ref platform, serviceState, parent))
			return 16;
		if (!MuiClassServiceCore.DeleteCustomClass(ref platform, serviceState, child))
			return 17;
		if (!MuiClassServiceCore.DeleteCustomClass(ref platform, serviceState,
			parent)) return 18;

		// The named super lease taken by the parent was released on delete.
		if (MuiClassServiceCore.ReferenceCount(ref platform, serviceState,
			superPtr) != 0) return 19;
		return 42;
	}

	// MG09 external-class loader closure. The freestanding platform publishes a
	// single deterministic Foo.mcc library/class token; the service must build
	// the mui/<id> name, register a guest-owned class record, and close the loader
	// lease on the final MUI_FreeClass.
	public static uint ExternalClassServiceRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var headlessState = APTR.FromPointer(0x00036000);
		var serviceState = APTR.FromPointer(0x00036040);
		var classId = APTR.FromPointer(0x00036100);
		APTR.WriteUInt8(classId, 0, (byte)'F');
		APTR.WriteUInt8(classId, 1, (byte)'o');
		APTR.WriteUInt8(classId, 2, (byte)'o');
		APTR.WriteUInt8(classId, 3, (byte)'.');
		APTR.WriteUInt8(classId, 4, (byte)'m');
		APTR.WriteUInt8(classId, 5, (byte)'c');
		APTR.WriteUInt8(classId, 6, (byte)'c');
		APTR.WriteUInt8(classId, 7, 0);
		if (!MuiHeadlessObjectCore.Initialize(ref platform, headlessState))
			return 1;
		if (!MuiClassServiceCore.Initialize(ref platform, serviceState,
			headlessState)) return 2;
		var classPointer = MuiClassServiceCore.GetClass(ref platform, serviceState,
			classId);
		if (classPointer.Raw != 0x00036600) return 3;
		if (!MuiHeadlessObjectCore.FindClassByName(ref platform, headlessState,
			classId).IsNotNull) return 4;
		if (!MuiClassServiceCore.FreeClass(ref platform, serviceState,
			classPointer)) return 5;
		if (MuiHeadlessObjectCore.FindClassByName(ref platform, headlessState,
			classId).IsNotNull) return 6;
		return 42;
	}

	// MG09 drawing-service gateway closure. Exercises nested strict-LIFO
	// rectangle clipping and clip regions, refresh flags==0 validation with
	// REFRESHMODE set/restore and balanced BeginUpdate/EndUpdate, and pen
	// obtain/full-token release/duplicate-release rejection plus an RGB mapping,
	// all through the frozen layers seam and the new MG09 region/pen
	// capabilities. Returns 42 on success.
	public static uint DrawingServiceRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var serviceState = APTR.FromPointer(0x00036000);
		var mri = APTR.FromPointer(0x00036100);
		var rastPort = APTR.FromPointer(0x00036200);
		var penSpec = APTR.FromPointer(0x00036300);
		var region = APTR.FromPointer(0x00036400);
		var rgb = APTR.FromPointer(0x00036440);
		APTR.WriteUInt32(mri, 20, rastPort.Raw);   // mri_RastPort
		APTR.WriteUInt32(rastPort, 0, 0x00036280); // rp_Layer (non-null)

		if (!MuiDrawingServiceCore.Initialize(ref platform, serviceState))
			return 1;

		// A malformed render info (null rast port) fails cleanly.
		APTR.WriteUInt32(mri, 20, 0);
		if (MuiDrawingServiceCore.AddClipping(ref platform, serviceState, mri, 0,
			0, 8, 8).IsNotNull) return 2;
		APTR.WriteUInt32(mri, 20, rastPort.Raw);

		// Nested clip stack: rectangle clip then clip region.
		var clip = MuiDrawingServiceCore.AddClipping(ref platform, serviceState,
			mri, 1, 2, 30, 40);
		if (clip.IsNull) return 3;
		var clipRegion = MuiDrawingServiceCore.AddClipRegion(ref platform,
			serviceState, mri, region);
		if (clipRegion.IsNull) return 4;
		// Strict LIFO: the rectangle clip cannot be removed before the region.
		if (MuiDrawingServiceCore.RemoveClipping(ref platform, serviceState, mri,
			clip)) return 5;
		if (!MuiDrawingServiceCore.RemoveClipRegion(ref platform, serviceState,
			mri, clipRegion)) return 6;
		if (!MuiDrawingServiceCore.RemoveClipping(ref platform, serviceState, mri,
			clip)) return 7;
		// A second removal of a retired handle fails.
		if (MuiDrawingServiceCore.RemoveClipping(ref platform, serviceState, mri,
			clip)) return 8;

		// Refresh: reserved flags must be 0; REFRESHMODE is set then restored.
		if (MuiDrawingServiceCore.BeginRefresh(ref platform, serviceState, mri, 1))
			return 9;
		if (!MuiDrawingServiceCore.BeginRefresh(ref platform, serviceState, mri,
			0)) return 10;
		if ((APTR.ReadUInt32(mri, 24) & 8u) == 0) return 11;
		if (MuiDrawingServiceCore.EndRefresh(ref platform, serviceState, mri, 3))
			return 12;
		if (!MuiDrawingServiceCore.EndRefresh(ref platform, serviceState, mri, 0))
			return 13;
		if ((APTR.ReadUInt32(mri, 24) & 8u) != 0) return 14;

		// Pens: obtain, map RGB, release the FULL token, reject masked/duplicate.
		var pen = MuiDrawingServiceCore.ObtainPen(ref platform, serviceState, mri,
			penSpec, 0);
		if (pen != 0x00010007) return 15;
		if (!MuiDrawingServiceCore.GetRGBColor(ref platform, serviceState, mri,
			penSpec, rgb)) return 16;
		if (APTR.ReadUInt32(rgb, 0) != 0x11111111u) return 17;
		// A MUIPEN-masked value must not match the tracked full token.
		if (MuiDrawingServiceCore.ReleasePen(ref platform, serviceState, mri,
			pen & 0xffff)) return 18;
		if (!MuiDrawingServiceCore.ReleasePen(ref platform, serviceState, mri,
			pen)) return 19;
		if (MuiDrawingServiceCore.ReleasePen(ref platform, serviceState, mri, pen))
			return 20;
		return 42;
	}

	// MG09 drawing-record layout seam. It round-trips the service state and the
	// clip, refresh, and full-token pen records through named codecs.
	public static uint DrawingServiceRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var state = APTR.FromPointer(0x0003A000);
		var clip = APTR.FromPointer(0x0003A020);
		var refresh = APTR.FromPointer(0x0003A040);
		var pen = APTR.FromPointer(0x0003A060);
		if (!MuiDrawingServiceRecordPacketCore.WriteState(ref platform, state,
			0x4D554944, clip, refresh, pen, 1) ||
			MuiDrawingServiceRecordPacketCore.DispatchState(ref platform, state) !=
				(0x4D554944u ^ 0x0003A020u ^ 0x0003A040u ^
				0x0003A060u ^ 1u)) return 1;
		if (!MuiDrawingServiceRecordPacketCore.WriteClip(ref platform, clip,
			clip, MuiDrawingServiceLayout.ClipKindRectangle,
			APTR.FromPointer(0x3A080), APTR.FromPointer(0x3A0A0)) ||
			MuiDrawingServiceRecordPacketCore.DispatchClip(ref platform, clip) !=
				(0x0003A020u ^ 1u ^ 0x0003A080u ^ 0x0003A0A0u)) return 2;
		if (!MuiDrawingServiceRecordPacketCore.WriteRefresh(ref platform, refresh,
			refresh, APTR.FromPointer(0x3A0C0), APTR.FromPointer(0x3A0E0), 8) ||
			MuiDrawingServiceRecordPacketCore.DispatchRefresh(ref platform,
				refresh) !=
				(0x0003A040u ^ 0x0003A0C0u ^ 0x0003A0E0u ^ 8u)) return 3;
		if (!MuiDrawingServiceRecordPacketCore.WritePen(ref platform, pen, pen,
			APTR.FromPointer(0x3A0C0), 0x80001234) ||
			MuiDrawingServiceRecordPacketCore.DispatchPen(ref platform, pen) !=
				(0x0003A060u ^ 0x0003A0C0u ^ 0x80001234u)) return 4;
		return 42;
	}

	// MG09 pen/color specialist family closure. Exercises exact class-name
	// classification (Pendisplay.mui), creation-time defaults, the [I/S/G]
	// setters/getters, the Pendisplay Set* methods, the Setup pen obtain through
	// MuiDrawingServiceCore with a balanced Cleanup release, the Colorfield
	// transient-spec pen lifecycle with a bounded fill, Coloradjust synchronized
	// ARGB channels, the obsolete-but-supported Palette groupable default, the
	// private Penadjust PSIMode, and class-owned disposal. Returns 42 on success.
	public static uint ColorSpecialistRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var drawState = APTR.FromPointer(0x00036000);
		var instance = APTR.FromPointer(0x00036100);
		var mri = APTR.FromPointer(0x00036200);
		var rastPort = APTR.FromPointer(0x00036300);
		var classId = APTR.FromPointer(0x00036400);
		var rgbSource = APTR.FromPointer(0x00036440);

		if (!MuiDrawingServiceCore.Initialize(ref platform, drawState)) return 1;

		// A 28-byte render info with a rast port/layer for the pen seam.
		APTR.WriteUInt32(mri, 20, rastPort.Raw);
		APTR.WriteUInt32(rastPort, 0, 0x00036380);

		// Exact class-name classification of the black-box class id.
		APTR.WriteUInt8(classId, 0, (byte)'P');
		APTR.WriteUInt8(classId, 1, (byte)'e');
		APTR.WriteUInt8(classId, 2, (byte)'n');
		APTR.WriteUInt8(classId, 3, (byte)'d');
		APTR.WriteUInt8(classId, 4, (byte)'i');
		APTR.WriteUInt8(classId, 5, (byte)'s');
		APTR.WriteUInt8(classId, 6, (byte)'p');
		APTR.WriteUInt8(classId, 7, (byte)'l');
		APTR.WriteUInt8(classId, 8, (byte)'a');
		APTR.WriteUInt8(classId, 9, (byte)'y');
		APTR.WriteUInt8(classId, 10, (byte)'.');
		APTR.WriteUInt8(classId, 11, (byte)'m');
		APTR.WriteUInt8(classId, 12, (byte)'u');
		APTR.WriteUInt8(classId, 13, (byte)'i');
		APTR.WriteUInt8(classId, 14, 0);
		if (MuiColorSpecialistCore.CreateByName(ref platform, instance, classId) !=
			MuiColorSpecialistClass.Pendisplay) return 2;

		// MUIM_Pendisplay_SetRGB then read the copied RGB back.
		if (!MuiColorSpecialistCore.SetRGB(ref platform, instance, 0x11, 0x22,
			0x33)) return 3;
		if (!MuiColorSpecialistCore.GetAttribute(ref platform, instance,
			MuiColorAttributes.PendisplayRgbColor, out var rgb) || rgb == 0)
			return 4;
		if (APTR.ReadUInt32(APTR.FromPointer(rgb), 0) != 0x11) return 5;

		// Setup obtains a full pen token through the drawing service; Cleanup
		// releases it exactly once.
		if (!MuiColorSpecialistCore.Setup(ref platform, instance, drawState, mri))
			return 6;
		if (!MuiColorSpecialistCore.GetAttribute(ref platform, instance,
			MuiColorAttributes.PendisplayPen, out var pen) || pen != 0x00010007)
			return 7;
		if (!MuiColorSpecialistCore.Draw(ref platform, instance, rastPort, 0, 0, 8,
			8)) return 8;
		if (!MuiColorSpecialistCore.Cleanup(ref platform, instance)) return 9;
		// A second cleanup releases nothing; the drawing service no longer tracks
		// the pen, so a direct release now fails (released exactly once).
		if (MuiColorSpecialistCore.Cleanup(ref platform, instance)) return 10;
		if (MuiDrawingServiceCore.ReleasePen(ref platform, drawState, mri,
			0x00010007)) return 11;
		if (!MuiColorSpecialistLifecycle.Dispose(ref platform, instance)) return 12;
		if (MuiColorSpecialistCore.Valid(ref platform, instance)) return 13;
		// Repeated disposal is a safe no-op.
		if (MuiColorSpecialistLifecycle.Dispose(ref platform, instance)) return 14;

		// Colorfield: copied RGB, transient-spec pen lifecycle, bounded fill.
		if (!MuiColorSpecialistCore.Create(ref platform, instance,
			MuiColorSpecialistClass.Colorfield)) return 15;
		if (!MuiColorSpecialistCore.SetAttribute(ref platform, instance,
			MuiColorAttributes.ColorfieldRed, 0xAABBCCDD, false, true, out _))
			return 16;
		if (MuiColorSpecialistCore.GetAttribute(ref platform, instance,
			MuiColorAttributes.ColorfieldRed, out var red) == false ||
			red != 0xAABBCCDD) return 17;
		if (!MuiColorSpecialistCore.Setup(ref platform, instance, drawState, mri))
			return 18;
		if (!MuiColorSpecialistCore.GetAttribute(ref platform, instance,
			MuiColorAttributes.ColorfieldPen, out var cfPen) || cfPen == 0)
			return 19;
		if (!MuiColorSpecialistCore.Cleanup(ref platform, instance)) return 20;
		if (!MuiColorSpecialistLifecycle.Dispose(ref platform, instance)) return 21;

		// Coloradjust: synchronized ARGB channels.
		if (!MuiColorSpecialistCore.Create(ref platform, instance,
			MuiColorSpecialistClass.Coloradjust)) return 22;
		if (!MuiColorSpecialistCore.SetAttribute(ref platform, instance,
			MuiColorAttributes.ColoradjustArgb, 0x80FF8040, false, true, out _))
			return 23;
		if (!MuiColorSpecialistCore.GetAttribute(ref platform, instance,
			MuiColorAttributes.ColoradjustGreen, out var green) ||
			green != 0x80808080) return 24;
		if (!MuiColorSpecialistLifecycle.Dispose(ref platform, instance)) return 25;

		// Palette: obsolete but supported, groupable by default.
		if (!MuiColorSpecialistCore.Create(ref platform, instance,
			MuiColorSpecialistClass.Palette)) return 26;
		if (!MuiColorSpecialistCore.GetAttribute(ref platform, instance,
			MuiColorAttributes.PaletteGroupable, out var groupable) ||
			groupable != 1) return 27;
		if (!MuiColorSpecialistCore.IsObsolete(MuiColorSpecialistClass.Palette))
			return 28;
		if (!MuiColorSpecialistLifecycle.Dispose(ref platform, instance)) return 29;

		// Penadjust: private PSIMode.
		if (!MuiColorSpecialistCore.Create(ref platform, instance,
			MuiColorSpecialistClass.Penadjust)) return 30;
		if (!MuiColorSpecialistCore.SetAttribute(ref platform, instance,
			MuiColorAttributes.PenadjustPsiMode, 1, false, false, out var changed) ||
			!changed) return 31;
		if (!MuiColorSpecialistLifecycle.Dispose(ref platform, instance)) return 32;

		return 42;
	}

	// Independent MG09 Pop* specialist closure. Exercises exact class-name
	// classification and adoption, immediate OpenHook / deferred CloseHook with
	// the exact CallHookPkt A0/A2/A1 delivery, Popobject volatile window with
	// conversion hooks, Poplist array materialization and selection-to-string,
	// Popasl scheduler-driven ASL integration, Poppen cancel-on-Cleanup, Popcolor
	// ShowAlpha, the private Popscreen, and recursive class-owned disposal.
	// Returns 42 on success.
	public static uint PopSpecialistRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();

		var instance = APTR.FromPointer(0x00036000);
		var sChild = APTR.FromPointer(0x00036100);
		var bChild = APTR.FromPointer(0x00036140);
		var popObject = APTR.FromPointer(0x00036180);
		var openHook = APTR.FromPointer(0x00036200);
		var openData = APTR.FromPointer(0x00036240);
		var closeHook = APTR.FromPointer(0x00036280);
		var closeData = APTR.FromPointer(0x000362C0);
		var strObjHook = APTR.FromPointer(0x00036300);
		var strObjData = APTR.FromPointer(0x00036340);
		var objStrHook = APTR.FromPointer(0x00036380);
		var objStrData = APTR.FromPointer(0x000363C0);
		var windowHook = APTR.FromPointer(0x00036400);
		var windowData = APTR.FromPointer(0x00036440);
		var startHook = APTR.FromPointer(0x00036480);
		var stopHook = APTR.FromPointer(0x00036500);
		var arr = APTR.FromPointer(0x00036600);
		var entryText = APTR.FromPointer(0x00036680);
		var tags = APTR.FromPointer(0x00036700);
		var classId = APTR.FromPointer(0x00036780);

		// struct Hook fixtures: h_Entry at +8 (non-zero), h_Data at +16.
		APTR.WriteUInt32(openHook, 8, 0x00AB0001u);
		APTR.WriteUInt32(openHook, 16, openData.Raw);
		APTR.WriteUInt32(closeHook, 8, 0x00AB0002u);
		APTR.WriteUInt32(closeHook, 16, closeData.Raw);
		APTR.WriteUInt32(strObjHook, 8, 0x00AC0001u);
		APTR.WriteUInt32(strObjHook, 16, strObjData.Raw);
		APTR.WriteUInt32(objStrHook, 8, 0x00AC0002u);
		APTR.WriteUInt32(objStrHook, 16, objStrData.Raw);
		APTR.WriteUInt32(windowHook, 8, 0x00AC0003u);
		APTR.WriteUInt32(windowHook, 16, windowData.Raw);
		APTR.WriteUInt32(startHook, 8, 0x00AE0001u);
		APTR.WriteUInt32(startHook, 16, 0);
		APTR.WriteUInt32(stopHook, 8, 0x00AE0002u);
		APTR.WriteUInt32(stopHook, 16, 0);

		// ---- Popstring: adoption, immediate open, deferred close -------------
		WriteClassId(classId, 'P', 'o', 'p', 's', 't', 'r', 'i', 'n', 'g');
		if (MuiPopSpecialistCore.CreateByName(ref platform, instance, classId,
			sChild, bChild) != MuiPopSpecialistClass.Popstring) return 2;
		MuiPopSpecialistCore.SetAttribute(ref platform, instance,
			MuiPopAttributes.Popstring_OpenHook, openHook.Raw, true, false, out _);
		if (!MuiPopSpecialistCore.Open(ref platform, instance)) return 3;
		if (APTR.ReadUInt32(openData, 4) != instance.Raw) return 4; // A2 delivery
		if (!MuiPopSpecialistCore.IsOpen(ref platform, instance)) return 5;
		MuiPopSpecialistCore.SetAttribute(ref platform, instance,
			MuiPopAttributes.Popstring_CloseHook, closeHook.Raw, true, false, out _);
		if (!MuiPopSpecialistCore.Close(ref platform, instance, 1)) return 6;
		if (!MuiPopSpecialistCore.IsCloseDeferred(ref platform, instance)) return 7;
		if (!MuiPopSpecialistCore.IsOpen(ref platform, instance)) return 8;
		if (!MuiPopSpecialistCore.HandleInput(ref platform, instance)) return 9;
		if (APTR.ReadUInt32(closeData, 4) != instance.Raw) return 10; // CloseHook A2
		if (MuiPopSpecialistCore.IsOpen(ref platform, instance)) return 11;
		if (!MuiPopSpecialistLifecycle.Dispose(ref platform, instance)) return 12;
		if (MuiPopSpecialistCore.Valid(ref platform, instance)) return 13;
		if (MuiPopSpecialistLifecycle.Dispose(ref platform, instance)) return 14;

		// ---- Popobject: volatile window + conversion hooks -------------------
		WriteClassId(classId, 'P', 'o', 'p', 'o', 'b', 'j', 'e', 'c', 't');
		if (MuiPopSpecialistCore.CreateByName(ref platform, instance, classId,
			sChild, bChild) != MuiPopSpecialistClass.Popobject) return 15;
		MuiPopSpecialistCore.SetAttribute(ref platform, instance,
			MuiPopAttributes.Popobject_Object, popObject.Raw, true, false, out _);
		if (!MuiPopSpecialistCore.GetAttribute(ref platform, instance,
			MuiPopAttributes.Popobject_Volatile, out var vol) || vol != 1) return 16;
		MuiPopSpecialistCore.SetAttribute(ref platform, instance,
			MuiPopAttributes.Popobject_StrObjHook, strObjHook.Raw, true, false,
			out _);
		MuiPopSpecialistCore.SetAttribute(ref platform, instance,
			MuiPopAttributes.Popobject_WindowHook, windowHook.Raw, true, false,
			out _);
		MuiPopSpecialistCore.SetAttribute(ref platform, instance,
			MuiPopAttributes.Popobject_ObjStrHook, objStrHook.Raw, true, false,
			out _);
		if (!MuiPopSpecialistCore.Open(ref platform, instance)) return 17;
		if (APTR.ReadUInt32(windowData, 0) != windowHook.Raw) return 18; // ran
		if (!MuiPopSpecialistCore.Close(ref platform, instance, 1)) return 19;
		if (APTR.ReadUInt32(objStrData, 4) != sChild.Raw) return 20; // string A2
		if (!MuiPopSpecialistCore.HandleInput(ref platform, instance)) return 21;
		if (MuiPopSpecialistCore.IsOpen(ref platform, instance)) return 22;
		if (!MuiPopSpecialistLifecycle.Dispose(ref platform, instance)) return 23;

		// ---- Poplist: array materialization + selection-to-string ------------
		WriteClassId(classId, 'P', 'o', 'p', 'l', 'i', 's', 't', (char)0, (char)0);
		if (MuiPopSpecialistCore.CreateByName(ref platform, instance, classId,
			sChild, bChild) != MuiPopSpecialistClass.Poplist) return 24;
		APTR.WriteUInt32(arr, 0, entryText.Raw);
		APTR.WriteUInt32(arr, 4, entryText.Raw + 0x20);
		APTR.WriteUInt32(arr, 8, 0);
		APTR.WriteUInt8(entryText, 0, (byte)'a');
		APTR.WriteUInt8(entryText, 1, 0);
		MuiPopSpecialistCore.SetAttribute(ref platform, instance,
			MuiPopAttributes.Poplist_Array, arr.Raw, true, false, out _);
		if (MuiPopSpecialistCore.ArrayCount(ref platform, instance) != 2) return 25;
		MuiPopSpecialistCore.SetAttribute(ref platform, instance,
			MuiPopAttributes.Popobject_ObjStrHook, objStrHook.Raw, true, false,
			out _);
		if (!MuiPopSpecialistCore.SelectEntry(ref platform, instance, 1)) return 26;
		if (MuiPopSpecialistCore.SelectedEntry(ref platform, instance) !=
			entryText.Raw + 0x20) return 27;
		if (MuiPopSpecialistCore.SelectEntry(ref platform, instance, 2)) return 28;
		if (!MuiPopSpecialistLifecycle.Dispose(ref platform, instance)) return 29;

		// ---- Popasl: scheduler-driven ASL integration ------------------------
		WriteClassId(classId, 'P', 'o', 'p', 'a', 's', 'l', (char)0, (char)0,
			(char)0);
		if (MuiPopSpecialistCore.CreateByName(ref platform, instance, classId,
			sChild, bChild) != MuiPopSpecialistClass.Popasl) return 30;
		MuiPopSpecialistCore.SetAttribute(ref platform, instance,
			MuiPopAttributes.Popasl_Type, 0, true, false, out _);
		MuiPopSpecialistCore.SetAttribute(ref platform, instance,
			MuiPopAttributes.Popasl_StartHook, startHook.Raw, true, false, out _);
		MuiPopSpecialistCore.SetAttribute(ref platform, instance,
			MuiPopAttributes.Popasl_StopHook, stopHook.Raw, true, false, out _);
		APTR.WriteUInt32(tags, 0, 0); // TAG_DONE
		if (!MuiPopSpecialistCore.SetAslTags(ref platform, instance, tags))
			return 31;
		if (!MuiPopSpecialistCore.Open(ref platform, instance)) return 32;
		if (!MuiPopSpecialistCore.GetAttribute(ref platform, instance,
			MuiPopAttributes.Popasl_Active, out var active) || active != 1)
			return 33;
		if (!MuiPopSpecialistCore.HandleInput(ref platform, instance)) return 34;
		if (!MuiPopSpecialistCore.GetAttribute(ref platform, instance,
			MuiPopAttributes.Popasl_Active, out var active2) || active2 != 0)
			return 35;
		if (!MuiPopSpecialistLifecycle.Dispose(ref platform, instance)) return 36;

		// ---- Poppen: cancel popup on Cleanup ---------------------------------
		WriteClassId(classId, 'P', 'o', 'p', 'p', 'e', 'n', (char)0, (char)0,
			(char)0);
		if (MuiPopSpecialistCore.CreateByName(ref platform, instance, classId,
			sChild, bChild) != MuiPopSpecialistClass.Poppen) return 37;
		if (!MuiPopSpecialistCore.Open(ref platform, instance)) return 38;
		if (!MuiPopSpecialistCore.IsOpen(ref platform, instance)) return 39;
		if (!MuiPopSpecialistCore.Cleanup(ref platform, instance)) return 40;
		if (MuiPopSpecialistCore.IsOpen(ref platform, instance)) return 41;
		if (!MuiPopSpecialistLifecycle.Dispose(ref platform, instance)) return 43;

		// ---- Popcolor: ShowAlpha state ---------------------------------------
		WriteClassId(classId, 'P', 'o', 'p', 'c', 'o', 'l', 'o', 'r', (char)0);
		if (MuiPopSpecialistCore.CreateByName(ref platform, instance, classId,
			sChild, bChild) != MuiPopSpecialistClass.Popcolor) return 44;
		MuiPopSpecialistCore.SetAttribute(ref platform, instance,
			MuiPopAttributes.Popcolor_ShowAlpha, 1, true, false, out _);
		if (!MuiPopSpecialistCore.GetAttribute(ref platform, instance,
			MuiPopAttributes.Popcolor_ShowAlpha, out var sa) || sa != 1) return 45;
		if (!MuiPopSpecialistLifecycle.Dispose(ref platform, instance)) return 46;

		// ---- Popscreen: private, ASL-derived ---------------------------------
		WriteClassId(classId, 'P', 'o', 'p', 's', 'c', 'r', 'e', 'e', 'n');
		if (MuiPopSpecialistCore.CreateByName(ref platform, instance, classId,
			sChild, bChild) != MuiPopSpecialistClass.Popscreen) return 47;
		if (!MuiPopSpecialistCore.IsPrivate(MuiPopSpecialistClass.Popscreen))
			return 48;
		APTR.WriteUInt32(tags, 0, 0);
		MuiPopSpecialistCore.SetAslTags(ref platform, instance, tags);
		if (!MuiPopSpecialistCore.Open(ref platform, instance)) return 49;
		if (!MuiPopSpecialistLifecycle.Dispose(ref platform, instance)) return 50;

		return 42;
	}

	// Write a short guest C string without relying on managed string storage.
	private static void WriteGuestString(APTR address, char c0,
		char c1 = (char)0, char c2 = (char)0, char c3 = (char)0,
		char c4 = (char)0, char c5 = (char)0, char c6 = (char)0)
	{
		var offset = 0;
		offset = PutChar(address, offset, c0);
		offset = PutChar(address, offset, c1);
		offset = PutChar(address, offset, c2);
		offset = PutChar(address, offset, c3);
		offset = PutChar(address, offset, c4);
		offset = PutChar(address, offset, c5);
		offset = PutChar(address, offset, c6);
		APTR.WriteUInt8(address, offset, 0);
	}

	// Write a "<Name>.mui" class id from up to nine leading characters (a NUL
	// character marks the end of the name), NUL-terminated after the suffix.
	private static void WriteClassId(APTR classId, char c0, char c1, char c2,
		char c3, char c4, char c5, char c6, char c7, char c8)
	{
		var offset = 0;
		offset = PutChar(classId, offset, c0);
		offset = PutChar(classId, offset, c1);
		offset = PutChar(classId, offset, c2);
		offset = PutChar(classId, offset, c3);
		offset = PutChar(classId, offset, c4);
		offset = PutChar(classId, offset, c5);
		offset = PutChar(classId, offset, c6);
		offset = PutChar(classId, offset, c7);
		offset = PutChar(classId, offset, c8);
		APTR.WriteUInt8(classId, offset++, (byte)'.');
		APTR.WriteUInt8(classId, offset++, (byte)'m');
		APTR.WriteUInt8(classId, offset++, (byte)'u');
		APTR.WriteUInt8(classId, offset++, (byte)'i');
		APTR.WriteUInt8(classId, offset, 0);
	}

	private static int PutChar(APTR classId, int offset, char value)
	{
		if (value == 0) return offset;
		APTR.WriteUInt8(classId, offset, (byte)value);
		return offset + 1;
	}

	private static void WriteColoradjustClassName(APTR address)
	{
		APTR.WriteUInt8(address, 0, (byte)'C');
		APTR.WriteUInt8(address, 1, (byte)'o');
		APTR.WriteUInt8(address, 2, (byte)'l');
		APTR.WriteUInt8(address, 3, (byte)'o');
		APTR.WriteUInt8(address, 4, (byte)'r');
		APTR.WriteUInt8(address, 5, (byte)'a');
		APTR.WriteUInt8(address, 6, (byte)'d');
		APTR.WriteUInt8(address, 7, (byte)'j');
		APTR.WriteUInt8(address, 8, (byte)'u');
		APTR.WriteUInt8(address, 9, (byte)'s');
		APTR.WriteUInt8(address, 10, (byte)'t');
		APTR.WriteUInt8(address, 11, (byte)'.');
		APTR.WriteUInt8(address, 12, (byte)'m');
		APTR.WriteUInt8(address, 13, (byte)'u');
		APTR.WriteUInt8(address, 14, (byte)'i');
		APTR.WriteUInt8(address, 15, 0);
	}


	// Independent final-MG09 misc specialist closure. Exercises exact class-name
	// classification for all ten classes, Keyadjust key-copy + allow/force input
	// policy, Panel_Run's honest validated boundary, Filepanel owned strings,
	// FilterFunc hook ABI, AddRow adoption and ASL browse failure cleanup,
	// Fontdisplay minmax/draw, the private Scrmodelist bounded records, Argstring
	// formatting, Aboutmui application-ref/self-close lifetime, Mccprefs bounded
	// registry with unregister id=0, FSProtectionBits flags, Title page topology
	// and the standalone dispatcher, plus recursive class-owned disposal. Returns
	// 42 on full success.
	public static uint MiscSpecialistRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();

		var instance = APTR.FromPointer(0x00036000);
		var classId = APTR.FromPointer(0x00036100);
		var text = APTR.FromPointer(0x00036200);
		var text2 = APTR.FromPointer(0x00036240);
		var hook = APTR.FromPointer(0x00036280);
		var hookData = APTR.FromPointer(0x000362C0);
		var tags = APTR.FromPointer(0x00036300);
		var storage = APTR.FromPointer(0x00036340);
		var packet = APTR.FromPointer(0x00036380);
		var app = APTR.FromPointer(0x000363C0);
		var win = APTR.FromPointer(0x00036400);

		// ---- Keyadjust: key copy + input policy ------------------------------
		WriteClassId(classId, 'K', 'e', 'y', 'a', 'd', 'j', 'u', 's', 't');
		if (MuiMiscSpecialistCore.CreateByName(ref platform, instance, classId) !=
			MuiMiscSpecialistClass.Keyadjust) return 2;
		APTR.WriteUInt8(text, 0, (byte)'a');
		APTR.WriteUInt8(text, 1, 0);
		if (!MuiMiscSpecialistCore.SetAttribute(ref platform, instance,
			MuiMiscAttributes.Keyadjust_Key, text.Raw, false, true, out _))
			return 3;
		if (!MuiMiscSpecialistCore.GetAttribute(ref platform, instance,
			MuiMiscAttributes.Keyadjust_Key, out var key) || key == 0 ||
			key == text.Raw) return 4;   // owned copy
		if (MuiMiscSpecialistCore.RecordInput(ref platform, instance, text, true, 1,
			false)) return 5;            // mouse rejected by default
		MuiMiscSpecialistCore.SetAttribute(ref platform, instance,
			MuiMiscAttributes.Keyadjust_AllowMouseEvents, 1, true, false, out _);
		if (!MuiMiscSpecialistCore.RecordInput(ref platform, instance, text, true, 1,
			false)) return 6;
		if (!MuiMiscSpecialistLifecycle.Dispose(ref platform, instance)) return 7;
		if (MuiMiscSpecialistCore.Valid(ref platform, instance)) return 8;

		// ---- Panel: honest run boundary --------------------------------------
		WriteClassId(classId, 'P', 'a', 'n', 'e', 'l', (char)0, (char)0, (char)0,
			(char)0);
		if (MuiMiscSpecialistCore.CreateByName(ref platform, instance, classId) !=
			MuiMiscSpecialistClass.Panel) return 9;
		if (MuiMiscSpecialistCore.PanelRun(ref platform, instance, APTR.Null, win))
			return 10;
		if (!MuiMiscSpecialistCore.PanelRun(ref platform, instance, app, win))
			return 11;
		if (!MuiMiscSpecialistCore.PanelHasRun(ref platform, instance)) return 12;
		MuiMiscSpecialistLifecycle.Dispose(ref platform, instance);

		// ---- Filepanel: strings, FilterFunc hook, AddRow, ASL browse ---------
		WriteClassId(classId, 'F', 'i', 'l', 'e', 'p', 'a', 'n', 'e', 'l');
		if (MuiMiscSpecialistCore.CreateByName(ref platform, instance, classId) !=
			MuiMiscSpecialistClass.Filepanel) return 13;
		APTR.WriteUInt8(text, 0, (byte)'R');
		APTR.WriteUInt8(text, 1, (byte)'A');
		APTR.WriteUInt8(text, 2, (byte)'M');
		APTR.WriteUInt8(text, 3, (byte)':');
		APTR.WriteUInt8(text, 4, 0);
		MuiMiscSpecialistCore.SetAttribute(ref platform, instance,
			MuiMiscAttributes.Filepanel_Drawer, text.Raw, false, true, out _);
		if (!MuiMiscSpecialistCore.GetAttribute(ref platform, instance,
			MuiMiscAttributes.Filepanel_Drawer, out var drawer) || drawer == 0 ||
			drawer == text.Raw) return 14;
		if (MuiMiscSpecialistCore.FilepanelFilter(ref platform, instance, text) != 1)
			return 15;               // no hook -> keep
		APTR.WriteUInt32(hook, 8, 0x00DD0001u);
		APTR.WriteUInt32(hook, 16, hookData.Raw);
		MuiMiscSpecialistCore.SetAttribute(ref platform, instance,
			MuiMiscAttributes.Filepanel_FilterFunc, hook.Raw, true, false, out _);
		MuiMiscSpecialistCore.FilepanelFilter(ref platform, instance, text);
		if (APTR.ReadUInt32(hookData, 4) != instance.Raw) return 16;   // A2 = object
		var label = platform.NewObject(APTR.FromPointer(0x9000), APTR.Null);
		var contents = platform.NewObject(APTR.FromPointer(0x9000), APTR.Null);
		if (MuiMiscSpecialistCore.FilepanelAddRow(ref platform, instance, APTR.Null,
			contents)) return 17;    // null child rejected
		if (!MuiMiscSpecialistCore.FilepanelAddRow(ref platform, instance, label,
			contents)) return 18;
		if (MuiMiscSpecialistCore.FilepanelRowCount(ref platform, instance) != 1)
			return 19;
		APTR.WriteUInt32(tags, 0, 0);   // TAG_DONE
		if (!MuiMiscSpecialistCore.FilepanelBrowse(ref platform, instance, 0, tags))
			return 20;
		if (!MuiMiscSpecialistLifecycle.Dispose(ref platform, instance)) return 21;

		// ---- Fontdisplay: minmax + draw (no attributes) ----------------------
		WriteName(classId, 'F', 'o', 'n', 't', 'd', 'i', 's', 'p', 'l', 'a', 'y',
			(char)0, (char)0, (char)0, (char)0, (char)0);
		if (MuiMiscSpecialistCore.CreateByName(ref platform, instance, classId) !=
			MuiMiscSpecialistClass.Fontdisplay) return 22;
		if (!MuiMiscSpecialistCore.FontdisplayAskMinMax(ref platform, instance,
			storage) || APTR.ReadUInt16(storage, 0) != 40) return 23;
		if (!MuiMiscSpecialistCore.FontdisplayDraw(ref platform, instance, 100, 20))
			return 24;
		MuiMiscSpecialistLifecycle.Dispose(ref platform, instance);

		// ---- Scrmodelist: private bounded records ----------------------------
		WriteName(classId, 'S', 'c', 'r', 'm', 'o', 'd', 'e', 'l', 'i', 's', 't',
			(char)0, (char)0, (char)0, (char)0, (char)0);
		if (MuiMiscSpecialistCore.CreateByName(ref platform, instance, classId) !=
			MuiMiscSpecialistClass.Scrmodelist) return 25;
		if (!MuiMiscSpecialistCore.IsPrivate(MuiMiscSpecialistClass.Scrmodelist))
			return 26;
		if (!MuiMiscSpecialistCore.ScrmodelistAddMode(ref platform, instance,
			0x00021000)) return 27;
		if (MuiMiscSpecialistCore.ScrmodelistModeCount(ref platform, instance) != 1)
			return 28;
		MuiMiscSpecialistLifecycle.Dispose(ref platform, instance);

		// ---- Argstring: owned template + formatting --------------------------
		WriteClassId(classId, 'A', 'r', 'g', 's', 't', 'r', 'i', 'n', 'g');
		if (MuiMiscSpecialistCore.CreateByName(ref platform, instance, classId) !=
			MuiMiscSpecialistClass.Argstring) return 29;
		APTR.WriteUInt8(text, 0, (byte)'F');
		APTR.WriteUInt8(text, 1, (byte)'/');
		APTR.WriteUInt8(text, 2, (byte)'A');
		APTR.WriteUInt8(text, 3, 0);
		MuiMiscSpecialistCore.SetAttribute(ref platform, instance,
			MuiMiscAttributes.Argstring_Template, text.Raw, false, true, out _);
		if (!MuiMiscSpecialistCore.FormatContents(ref platform, instance))
			return 30;
		if (!MuiMiscSpecialistCore.GetAttribute(ref platform, instance,
			MuiMiscAttributes.Argstring_Contents, out var cont) || cont == 0)
			return 31;
		MuiMiscSpecialistLifecycle.Dispose(ref platform, instance);

		// ---- Aboutmui: application ref + self-close --------------------------
		WriteClassId(classId, 'A', 'b', 'o', 'u', 't', 'm', 'u', 'i', (char)0);
		if (MuiMiscSpecialistCore.CreateByName(ref platform, instance, classId) !=
			MuiMiscSpecialistClass.Aboutmui) return 32;
		if (MuiMiscSpecialistCore.AboutmuiOpen(ref platform, instance)) return 33;
		MuiMiscSpecialistCore.SetAttribute(ref platform, instance,
			MuiMiscAttributes.Aboutmui_Application, app.Raw, true, false, out _);
		if (!MuiMiscSpecialistCore.AboutmuiOpen(ref platform, instance)) return 34;
		if (MuiMiscSpecialistCore.AboutmuiOpen(ref platform, instance)) return 35;
		if (!MuiMiscSpecialistCore.AboutmuiClose(ref platform, instance)) return 36;
		MuiMiscSpecialistLifecycle.Dispose(ref platform, instance);

		// ---- Mccprefs: bounded registry + unregister id=0 --------------------
		WriteClassId(classId, 'M', 'c', 'c', 'p', 'r', 'e', 'f', 's', (char)0);
		if (MuiMiscSpecialistCore.CreateByName(ref platform, instance, classId) !=
			MuiMiscSpecialistClass.Mccprefs) return 37;
		if (MuiMiscSpecialistCore.MccprefsConfigToGadgets(ref platform, instance,
			app)) return 38;         // empty registry boundary
		if (!MuiMiscSpecialistCore.MccprefsRegisterGadget(ref platform, instance,
			app, 10, 0, text, 0, APTR.Null)) return 39;
		if (!MuiMiscSpecialistCore.MccprefsConfigToGadgets(ref platform, instance,
			app)) return 40;
		if (!MuiMiscSpecialistCore.MccprefsRegisterGadget(ref platform, instance,
			app, 0, 0, APTR.Null, 0, APTR.Null)) return 44;   // unregister id=0
		if (MuiMiscSpecialistCore.MccprefsRegistryCount(ref platform, instance) != 0)
			return 45;
		MuiMiscSpecialistLifecycle.Dispose(ref platform, instance);

		// ---- Title: page topology --------------------------------------------
		WriteClassId(classId, 'T', 'i', 't', 'l', 'e', (char)0, (char)0, (char)0,
			(char)0);
		if (MuiMiscSpecialistCore.CreateByName(ref platform, instance, classId) !=
			MuiMiscSpecialistClass.Title) return 46;
		var h1 = MuiMiscSpecialistCore.TitleNew(ref platform, instance);
		var h2 = MuiMiscSpecialistCore.TitleNew(ref platform, instance);
		if (h1 == 0 || h2 == 0 || h1 == h2) return 47;
		if (MuiMiscSpecialistCore.TitleFindPage(ref platform, instance, h2) != 1)
			return 48;
		if (MuiMiscSpecialistCore.TitleClose(ref platform, instance, h2)) return 49;
		MuiMiscSpecialistCore.SetAttribute(ref platform, instance,
			MuiMiscAttributes.Title_Closable, 1, true, false, out _);
		if (!MuiMiscSpecialistCore.TitleClose(ref platform, instance, h2)) return 50;
		if (MuiMiscSpecialistCore.TitlePageCount(ref platform, instance) != 1)
			return 51;

		// ---- Standalone dispatcher over FSProtectionBits ---------------------
		var fsInstance = APTR.FromPointer(0x00036500);
		WriteName(classId, 'F', 'S', 'P', 'r', 'o', 't', 'e', 'c', 't', 'i', 'o',
			'n', 'B', 'i', 't', 's');
		if (MuiMiscSpecialistCore.CreateByName(ref platform, fsInstance, classId) !=
			MuiMiscSpecialistClass.FSProtectionBits) return 52;
		APTR.WriteUInt32(packet, 0, 0x8042549au);   // OM_SET
		APTR.WriteUInt32(packet, 4, MuiMiscAttributes.FSProtectionBits_Flags);
		APTR.WriteUInt32(packet, 8, 0x55);
		if (MuiMiscSpecialistDispatcher.Dispatch(ref platform,
			fsInstance, packet) != 1) return 53;
		APTR.WriteUInt32(packet, 0, 0x00000104u);   // OM_GET
		APTR.WriteUInt32(packet, 8, storage.Raw);
		if (MuiMiscSpecialistDispatcher.Dispatch(ref platform,
			fsInstance, packet) != 1) return 54;
		if (APTR.ReadUInt32(storage, 0) != 0x55) return 55;
		APTR.WriteUInt32(packet, 0, 0x00000102u);   // OM_DISPOSE
		if (MuiMiscSpecialistDispatcher.Dispatch(ref platform,
			fsInstance, packet) != 1) return 56;
		if (MuiMiscSpecialistCore.Valid(ref platform, fsInstance)) return 57;

		if (!MuiMiscSpecialistLifecycle.Dispose(ref platform, instance)) return 58;
		return 42;
	}

	// Write a "<Name>.mui" class id from up to sixteen leading characters (a NUL
	// character marks the end of the name), NUL-terminated after the suffix.
	// Mirrors WriteClassId (which is limited to nine leading characters) for the
	// longer misc-family names.
	private static void WriteName(APTR classId, char c0, char c1, char c2,
		char c3, char c4, char c5, char c6, char c7, char c8, char c9, char c10,
		char c11, char c12, char c13, char c14, char c15)
	{
		var offset = 0;
		offset = PutChar(classId, offset, c0);
		offset = PutChar(classId, offset, c1);
		offset = PutChar(classId, offset, c2);
		offset = PutChar(classId, offset, c3);
		offset = PutChar(classId, offset, c4);
		offset = PutChar(classId, offset, c5);
		offset = PutChar(classId, offset, c6);
		offset = PutChar(classId, offset, c7);
		offset = PutChar(classId, offset, c8);
		offset = PutChar(classId, offset, c9);
		offset = PutChar(classId, offset, c10);
		offset = PutChar(classId, offset, c11);
		offset = PutChar(classId, offset, c12);
		offset = PutChar(classId, offset, c13);
		offset = PutChar(classId, offset, c14);
		offset = PutChar(classId, offset, c15);
		APTR.WriteUInt8(classId, offset++, (byte)'.');
		APTR.WriteUInt8(classId, offset++, (byte)'m');
		APTR.WriteUInt8(classId, offset++, (byte)'u');
		APTR.WriteUInt8(classId, offset++, (byte)'i');
		APTR.WriteUInt8(classId, offset, 0);
	}

	// MG09 menu specialist closure: Menustrip.mui / Menu.mui / Menuitem.mui over
	// real headless objects and the frozen MuiFamilyCore, exercising hierarchy,
	// one-level nesting protection, CopyStrings ownership, Menustrip change
	// brackets with underflow protection, WillOpen/disabled gating, Checkit/
	// Toggle/Exclude and recursive disposal. Returns 42 on full success.
	public static uint MenuSpecialistRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint menustripName = 0x00036100;
		const uint menuName = 0x00036120;
		const uint menuitemName = 0x00036140;
		const uint titleA = 0x00036160;
		const uint titleB = 0x00036180;
		const uint packet = 0x00036900;
		const uint storage = 0x00036940;
		var st = APTR.FromPointer(state);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), st)) return 1;

		WriteMenustripClassName(APTR.FromPointer(menustripName));
		WriteMenuClassName(APTR.FromPointer(menuName));
		WriteMenuitemClassName(APTR.FromPointer(menuitemName));
		var stripClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			st, APTR.FromPointer(menustripName), APTR.Null, 0, APTR.FromPointer(9));
		var menuClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			st, APTR.FromPointer(menuName), APTR.Null, 0, APTR.FromPointer(10));
		var itemClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			st, APTR.FromPointer(menuitemName), APTR.Null, 0, APTR.FromPointer(11));
		if (stripClass.IsNull || menuClass.IsNull || itemClass.IsNull) return 2;

		var strip = MuiHeadlessObjectCore.CreateObjectA(ref platform, st,
			stripClass, APTR.Null);
		var menu = MuiHeadlessObjectCore.CreateObjectA(ref platform, st,
			menuClass, APTR.Null);
		var item = MuiHeadlessObjectCore.CreateObjectA(ref platform, st,
			itemClass, APTR.Null);
		var sub = MuiHeadlessObjectCore.CreateObjectA(ref platform, st,
			itemClass, APTR.Null);
		var sub2 = MuiHeadlessObjectCore.CreateObjectA(ref platform, st,
			itemClass, APTR.Null);
		if (strip.IsNull || menu.IsNull || item.IsNull || sub.IsNull ||
			sub2.IsNull) return 3;

		if (MuiMenuSpecialistCore.Attach(ref platform, st, strip,
			MuiMenuSpecialistClass.Menustrip).IsNull ||
			MuiMenuSpecialistCore.Attach(ref platform, st, menu,
			MuiMenuSpecialistClass.Menu).IsNull ||
			MuiMenuSpecialistCore.Attach(ref platform, st, item,
			MuiMenuSpecialistClass.Menuitem).IsNull ||
			MuiMenuSpecialistCore.Attach(ref platform, st, sub,
			MuiMenuSpecialistClass.Menuitem).IsNull ||
			MuiMenuSpecialistCore.Attach(ref platform, st, sub2,
			MuiMenuSpecialistClass.Menuitem).IsNull) return 4;

		// Well-formed hierarchy through MuiFamilyCore.
		if (!MuiMenuSpecialistCore.AddChild(ref platform, st, strip, menu) ||
			!MuiMenuSpecialistCore.AddChild(ref platform, st, menu, item) ||
			!MuiMenuSpecialistCore.AddChild(ref platform, st, item, sub))
			return 5;
		if (MuiMenuSpecialistCore.ChildCount(ref platform, st, strip) != 1)
			return 6;

		// Malformed nesting is rejected: item under strip, sub-item under sub.
		if (MuiMenuSpecialistCore.AddChild(ref platform, st, strip, item))
			return 7;
		if (MuiMenuSpecialistCore.AddChild(ref platform, st, sub, sub2))
			return 8;

		// Struct-first packet boundary: exercise Family_AddHead, MUIM_Set, and
		// OM_GET through the specialist dispatcher without entering the generic
		// headless fallback.
		APTR.WriteUInt32(APTR.FromPointer(packet), 0,
			MuiMenuAttributes.Family_AddHead);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, sub2.Raw);
		if (!MuiMenuSpecialistDispatcher.TryDispatch(ref platform, st, item,
			APTR.FromPointer(packet), out var dispatchResult) || dispatchResult != 1)
			return 33;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x8042549A);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4,
			MuiMenuAttributes.Menu_Enabled);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 0);
		if (!MuiMenuSpecialistDispatcher.TryDispatch(ref platform, st, menu,
			APTR.FromPointer(packet), out dispatchResult) || dispatchResult != 1)
			return 34;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 0x00000104);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4,
			MuiMenuAttributes.Menu_Enabled);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, storage);
		if (!MuiMenuSpecialistDispatcher.TryDispatch(ref platform, st, menu,
			APTR.FromPointer(packet), out dispatchResult) || dispatchResult != 1 ||
			APTR.ReadUInt32(APTR.FromPointer(storage), 0) != 0) return 35;

		// CopyStrings ownership: the copy survives caller mutation.
		APTR.WriteUInt8(APTR.FromPointer(titleA), 0, (byte)'P');
		APTR.WriteUInt8(APTR.FromPointer(titleA), 1, (byte)'r');
		APTR.WriteUInt8(APTR.FromPointer(titleA), 2, 0);
		if (!MuiMenuSpecialistCore.SetAttribute(ref platform, st, menu,
			MuiMenuAttributes.Menu_CopyStrings, 1, true, false, out _)) return 9;
		if (!MuiMenuSpecialistCore.SetAttribute(ref platform, st, menu,
			MuiMenuAttributes.Menu_Title, titleA, true, false, out _)) return 10;
		if (!MuiMenuSpecialistCore.GetAttribute(ref platform, st, menu,
			MuiMenuAttributes.Menu_Title, out var owned)) return 11;
		if (owned == titleA) return 12;   // must be a class-owned copy
		if (APTR.ReadUInt8(APTR.FromPointer(owned), 0) != (byte)'P') return 13;
		APTR.WriteUInt8(APTR.FromPointer(titleA), 0, (byte)'X');
		if (APTR.ReadUInt8(APTR.FromPointer(owned), 0) != (byte)'P') return 14;

		// No-copy referencing on a separate item.
		APTR.WriteUInt8(APTR.FromPointer(titleB), 0, (byte)'O');
		APTR.WriteUInt8(APTR.FromPointer(titleB), 1, 0);
		if (!MuiMenuSpecialistCore.SetAttribute(ref platform, st, item,
			MuiMenuAttributes.Menuitem_Title, titleB, true, false, out _))
			return 15;
		if (!MuiMenuSpecialistCore.GetAttribute(ref platform, st, item,
			MuiMenuAttributes.Menuitem_Title, out var refd) || refd != titleB)
			return 16;

		// Menustrip change brackets with underflow protection.
		if (MuiMenuSpecialistCore.ExitChange(ref platform, st, strip)) return 17;
		if (!MuiMenuSpecialistCore.InitChange(ref platform, st, strip) ||
			!MuiMenuSpecialistCore.InitChange(ref platform, st, strip)) return 18;
		if (MuiMenuSpecialistCore.ChangeDepth(ref platform, st, strip) != 2)
			return 19;
		if (!MuiMenuSpecialistCore.ExitChange(ref platform, st, strip) ||
			!MuiMenuSpecialistCore.ExitChange(ref platform, st, strip)) return 20;
		if (MuiMenuSpecialistCore.ExitChange(ref platform, st, strip)) return 21;

		// WillOpen gating: enabled+settled opens; disabled does not.
		if (!MuiMenuSpecialistCore.WillOpen(ref platform, st, strip)) return 22;
		if (!MuiMenuSpecialistCore.SetAttribute(ref platform, st, strip,
			MuiMenuAttributes.Menustrip_Enabled, 0, false, true, out _)) return 23;
		if (MuiMenuSpecialistCore.WillOpen(ref platform, st, strip)) return 24;

		// Checkit + Toggle + trigger publication.
		if (!MuiMenuSpecialistCore.SetAttribute(ref platform, st, sub,
			MuiMenuAttributes.Menuitem_Checkit, 1, true, false, out _) ||
			!MuiMenuSpecialistCore.SetAttribute(ref platform, st, sub,
			MuiMenuAttributes.Menuitem_Toggle, 1, true, false, out _)) return 25;
		if (!MuiMenuSpecialistCore.TriggerItem(ref platform, st, sub)) return 26;
		if (!MuiMenuSpecialistCore.GetAttribute(ref platform, st, sub,
			MuiMenuAttributes.Menuitem_Checked, out var checkedState) ||
			checkedState != 1) return 27;
		if (MuiMenuSpecialistCore.Trigger(ref platform, st, sub) != sub.Raw)
			return 28;

		// Recursive disposal frees the whole subtree and is idempotent.
		if (!MuiMenuSpecialistLifecycle.Dispose(ref platform, st, strip))
			return 29;
		if (MuiMenuSpecialistCore.Valid(ref platform, st, strip)) return 30;
		if (MuiMenuSpecialistLifecycle.Dispose(ref platform, st, strip))
			return 31;

		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 32;
		return 42;
	}

	// MG09 Process.mui / Slave.mui specialist freestanding closure. Exercises the
	// legal Process state machine (Pending -> Running -> Killed), owned-name
	// ownership, bounded priority/stack validation, the Slave setup/dispatch/
	// cleanup balance with the bounded 16-arg automagic packet, exact DoMethod
	// delivery under the frozen semaphore lock, malformed-packet rejection, error
	// recording, and class-owned disposal. Returns 42 on success.
	public static uint ProcessSpecialistRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		const uint privateRoot = 0x00035F00;
		const uint state = 0x00036000;
		const uint processName = 0x00036100;
		const uint slaveName = 0x00036120;
		const uint appName = 0x00036140;
		const uint nameA = 0x00036160;
		const uint packet = 0x00036200;
		var st = APTR.FromPointer(state);
		if (!MuiMasterLifecycleCore.Create(ref platform,
			APTR.FromPointer(privateRoot), st)) return 1;

		WriteProcessClassName(APTR.FromPointer(processName));
		WriteSlaveClassName(APTR.FromPointer(slaveName));
		WriteApplicationClassName(APTR.FromPointer(appName));
		var processClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			st, APTR.FromPointer(processName), APTR.Null, 0, APTR.FromPointer(20));
		var slaveClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			st, APTR.FromPointer(slaveName), APTR.Null, 0, APTR.FromPointer(21));
		var appClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			st, APTR.FromPointer(appName), APTR.Null, 0, APTR.FromPointer(22));
		if (processClass.IsNull || slaveClass.IsNull || appClass.IsNull) return 2;

		// ---- Process ---------------------------------------------------------
		var proc = MuiHeadlessObjectCore.CreateObjectA(ref platform, st,
			processClass, APTR.Null);
		if (proc.IsNull) return 3;
		if (MuiProcessSpecialistCore.Attach(ref platform, st, proc,
			MuiProcessSpecialistClass.Process).IsNull) return 4;
		if (MuiProcessSpecialistCore.ProcessStateOf(ref platform, st, proc) !=
			MuiProcessState.Pending) return 5;

		// Owned Name copy survives caller mutation.
		APTR.WriteUInt8(APTR.FromPointer(nameA), 0, (byte)'W');
		APTR.WriteUInt8(APTR.FromPointer(nameA), 1, (byte)'k');
		APTR.WriteUInt8(APTR.FromPointer(nameA), 2, 0);
		if (!MuiProcessSpecialistCore.SetAttribute(ref platform, st, proc,
			MuiProcessAttributes.Process_Name, nameA, true, false, out _)) return 6;
		if (!MuiProcessSpecialistCore.GetAttribute(ref platform, st, proc,
			MuiProcessAttributes.Process_Name, out var owned)) return 7;
		if (owned == nameA) return 8;                       // must be a copy
		if (APTR.ReadUInt8(APTR.FromPointer(owned), 0) != (byte)'W') return 9;
		APTR.WriteUInt8(APTR.FromPointer(nameA), 0, (byte)'X');
		if (APTR.ReadUInt8(APTR.FromPointer(owned), 0) != (byte)'W') return 10;

		// Bounded priority/stack validation.
		if (MuiProcessSpecialistCore.SetAttribute(ref platform, st, proc,
			MuiProcessAttributes.Process_Priority, 200, true, false, out _))
			return 11;
		if (!MuiProcessSpecialistCore.SetAttribute(ref platform, st, proc,
			MuiProcessAttributes.Process_Priority, 5, true, false, out _)) return 12;
		if (!MuiProcessSpecialistCore.SetAttribute(ref platform, st, proc,
			MuiProcessAttributes.Process_StackSize, 16384, true, false, out _))
			return 13;
		MuiProcessSpecialistCore.SetAttribute(ref platform, st, proc,
			MuiProcessAttributes.Process_SourceClass, 0xAAAA, true, false, out _);
		MuiProcessSpecialistCore.SetAttribute(ref platform, st, proc,
			MuiProcessAttributes.Process_SourceObject, 0xBBBB, true, false, out _);

		// Launch -> Running, task published; duplicate launch rejected.
		if (!MuiProcessSpecialistCore.Launch(ref platform, st, proc)) return 14;
		if (MuiProcessSpecialistCore.ProcessStateOf(ref platform, st, proc) !=
			MuiProcessState.Running) return 15;
		var token = MuiProcessSpecialistCore.TaskToken(ref platform, st, proc);
		if (token == 0) return 16;
		if (!MuiProcessSpecialistCore.GetAttribute(ref platform, st, proc,
			MuiProcessAttributes.Process_Task, out var task) || task != token)
			return 17;
		if (MuiProcessSpecialistCore.Launch(ref platform, st, proc)) return 18;

		// Signal while running; poll leaves it Running (scheduler still busy).
		if (!MuiProcessSpecialistCore.Signal(ref platform, st, proc, 0x10))
			return 19;
		if (MuiProcessSpecialistCore.Process(ref platform, st, proc) !=
			(uint)MuiProcessState.Running) return 20;

		// Kill -> Killed; token cleared. Kill of a non-running process fails.
		if (!MuiProcessSpecialistCore.Kill(ref platform, st, proc)) return 21;
		if (MuiProcessSpecialistCore.ProcessStateOf(ref platform, st, proc) !=
			MuiProcessState.Killed) return 22;
		if (MuiProcessSpecialistCore.Kill(ref platform, st, proc)) return 23;

		// ---- Slave -----------------------------------------------------------
		var slave = MuiHeadlessObjectCore.CreateObjectA(ref platform, st,
			slaveClass, APTR.Null);
		var app = MuiHeadlessObjectCore.CreateObjectA(ref platform, st, appClass,
			APTR.Null);
		var target = MuiHeadlessObjectCore.CreateObjectA(ref platform, st, appClass,
			APTR.Null);
		if (slave.IsNull || app.IsNull || target.IsNull) return 24;
		if (MuiProcessSpecialistCore.Attach(ref platform, st, slave,
			MuiProcessSpecialistClass.Slave).IsNull) return 25;
		MuiProcessSpecialistCore.SetAttribute(ref platform, st, slave,
			MuiProcessAttributes.Slave_Application, app.Raw, true, false, out _);
		MuiProcessSpecialistCore.SetAttribute(ref platform, st, slave,
			MuiProcessAttributes.Slave_Object, target.Raw, true, false, out _);
		MuiProcessSpecialistCore.SetAttribute(ref platform, st, slave,
			MuiProcessAttributes.Slave_Class, 0xCCCC, true, false, out _);

		// Setup requires the live Application; double setup rejected.
		if (!MuiProcessSpecialistCore.Setup(ref platform, st, slave)) return 26;
		if (MuiProcessSpecialistCore.Setup(ref platform, st, slave)) return 27;

		// Bounded 16-arg automagic dispatch delivers exactly one DoMethod under
		// the frozen semaphore lock, which is balanced (re-obtainable after).
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 2);
		APTR.WriteUInt32(APTR.FromPointer(packet), 4, 0x8042AAAAu);
		APTR.WriteUInt32(APTR.FromPointer(packet), 8, 0x1111u);
		APTR.WriteUInt32(APTR.FromPointer(packet), 12, 0x2222u);
		if (!MuiProcessSpecialistCore.Dispatch(ref platform, st, slave,
			APTR.FromPointer(packet), out _)) return 28;
		if (!MuiSemaphoreCore.Obtain(ref platform, st, target)) return 29;
		if (!MuiSemaphoreCore.Release(ref platform, st, target)) return 30;

		// 16-arg is the ceiling; 17 args is rejected.
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 16);
		if (!MuiProcessSpecialistCore.Dispatch(ref platform, st, slave,
			APTR.FromPointer(packet), out _)) return 31;
		APTR.WriteUInt32(APTR.FromPointer(packet), 0, 17);
		if (MuiProcessSpecialistCore.Dispatch(ref platform, st, slave,
			APTR.FromPointer(packet), out _)) return 32;
		// Null packet rejected.
		if (MuiProcessSpecialistCore.Dispatch(ref platform, st, slave, APTR.Null,
			out _)) return 33;

		// Error recording and Slave cleanup balance.
		if (!MuiProcessSpecialistCore.Error(ref platform, st, slave, 205,
			out var stored) || stored != 205) return 34;
		if (MuiProcessSpecialistCore.LastError(ref platform, st, slave) != 205)
			return 35;
		if (!MuiProcessSpecialistCore.Cleanup(ref platform, st, slave)) return 36;
		if (MuiProcessSpecialistCore.Cleanup(ref platform, st, slave)) return 37;

		// Class-owned disposal is complete and idempotent.
		if (!MuiProcessSpecialistLifecycle.Dispose(ref platform, st, proc))
			return 38;
		if (MuiProcessSpecialistCore.Valid(ref platform, st, proc)) return 39;
		if (!MuiProcessSpecialistLifecycle.Dispose(ref platform, st, slave))
			return 40;

		if (!MuiMasterLifecycleCore.Dispose(ref platform,
			APTR.FromPointer(privateRoot))) return 41;
		return 42;
	}

	// MG09 Process/Slave sidecar named-record closure. This deliberately
	// qualifies the guest-resident struct/codec boundary independently from
	// the behavioral specialist root above: all fields are written and read
	// through the named record, with offsets confined to its codec. Returns 42
	// on success.
	public static uint ProcessSpecialistRecordRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();
		var address = APTR.FromPointer(0x0003B000);
		var nameOwned = APTR.FromPointer(0x0003B100);
		var input = new MuiProcessSpecialistRecordInput
		{
			Class = (uint)MuiProcessSpecialistClass.Slave,
			State = (uint)MuiProcessState.Running,
			TaskToken = 0x0003B200,
			NameOwned = nameOwned,
			NameOwnedSize = 7,
			Error = 0x0000DEAD,
			SignalsReceived = 0x01020304,
			Flags = 0x00000011,
			DispatchDepth = 1,
			SetupState = 1,
			NotifyCount = 3,
			NotifyAttribute = 0x8042AAAA
		};
		if (!MuiProcessSpecialistRecordPacketCore.WriteRecord(ref platform,
			address, input))
			return 1;
		if (MuiProcessSpecialistRecordPacketCore.DispatchRecord(ref platform,
			address) != 0xCC102655) return 2;
		return 42;
	}

	private static void WriteProcessClassName(APTR address)
	{
		APTR.WriteUInt8(address, 0, (byte)'P');
		APTR.WriteUInt8(address, 1, (byte)'r');
		APTR.WriteUInt8(address, 2, (byte)'o');
		APTR.WriteUInt8(address, 3, (byte)'c');
		APTR.WriteUInt8(address, 4, (byte)'e');
		APTR.WriteUInt8(address, 5, (byte)'s');
		APTR.WriteUInt8(address, 6, (byte)'s');
		APTR.WriteUInt8(address, 7, (byte)'.');
		APTR.WriteUInt8(address, 8, (byte)'m');
		APTR.WriteUInt8(address, 9, (byte)'u');
		APTR.WriteUInt8(address, 10, (byte)'i');
		APTR.WriteUInt8(address, 11, 0);
	}

	private static void WriteSlaveClassName(APTR address)
	{
		APTR.WriteUInt8(address, 0, (byte)'S');
		APTR.WriteUInt8(address, 1, (byte)'l');
		APTR.WriteUInt8(address, 2, (byte)'a');
		APTR.WriteUInt8(address, 3, (byte)'v');
		APTR.WriteUInt8(address, 4, (byte)'e');
		APTR.WriteUInt8(address, 5, (byte)'.');
		APTR.WriteUInt8(address, 6, (byte)'m');
		APTR.WriteUInt8(address, 7, (byte)'u');
		APTR.WriteUInt8(address, 8, (byte)'i');
		APTR.WriteUInt8(address, 9, 0);
	}

	private static void WriteApplicationClassName(APTR address)
	{
		APTR.WriteUInt8(address, 0, (byte)'A');
		APTR.WriteUInt8(address, 1, (byte)'p');
		APTR.WriteUInt8(address, 2, (byte)'p');
		APTR.WriteUInt8(address, 3, (byte)'l');
		APTR.WriteUInt8(address, 4, (byte)'i');
		APTR.WriteUInt8(address, 5, (byte)'c');
		APTR.WriteUInt8(address, 6, (byte)'a');
		APTR.WriteUInt8(address, 7, (byte)'t');
		APTR.WriteUInt8(address, 8, (byte)'i');
		APTR.WriteUInt8(address, 9, (byte)'o');
		APTR.WriteUInt8(address, 10, (byte)'n');
		APTR.WriteUInt8(address, 11, (byte)'.');
		APTR.WriteUInt8(address, 12, (byte)'m');
		APTR.WriteUInt8(address, 13, (byte)'u');
		APTR.WriteUInt8(address, 14, (byte)'i');
		APTR.WriteUInt8(address, 15, 0);
	}

	// MG09 external-resource wrapper closure: the official Boopsi.mui and
	// Dtpic.mui classes over the freestanding service platform. Exercises
	// external-class open/create at setup, the colorwheel.gadget -1 geometry
	// workaround, IDCMP_UPDATE -> notification, remember/regenerate keeping the
	// class open, the owned Dtpic name copy immune to caller mutation,
	// datatypes picture acquire/layout/draw, and idempotent cleanup/dispose.
	// Returns 42 on full success.
	public static uint ExternalWrapperSpecialistRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();

		var instance = APTR.FromPointer(0x00036000);
		var classId = APTR.FromPointer(0x00036100);
		var cwId = APTR.FromPointer(0x00036120);
		var creationTags = APTR.FromPointer(0x00036140);
		var renderInfo = APTR.FromPointer(0x00036180);
		var minMax = APTR.FromPointer(0x000361A0);
		var nameA = APTR.FromPointer(0x000361C0);
		var packet = APTR.FromPointer(0x00036200);
		var attrList = APTR.FromPointer(0x00036240);
		var window = APTR.FromPointer(0x00036280);
		var screen = APTR.FromPointer(0x000362A0);
		var drawInfo = APTR.FromPointer(0x000362C0);
		var rastPort = APTR.FromPointer(0x000362E0);

		APTR.WriteUInt32(renderInfo, 0, screen.Raw);
		APTR.WriteUInt32(renderInfo, 4, window.Raw);
		APTR.WriteUInt32(renderInfo, 8, drawInfo.Raw);
		APTR.WriteUInt32(renderInfo, 12, rastPort.Raw);

		// ---- Boopsi over the OS 3.0/3.1 colorwheel.gadget --------------------
		WriteClassId(classId, 'B', 'o', 'o', 'p', 's', 'i', (char)0, (char)0,
			(char)0);
		if (MuiExternalWrapperCore.CreateByName(ref platform, instance, classId) !=
			MuiExternalWrapperClass.Boopsi) return 2;
		WriteColorwheelId(cwId);
		MuiExternalWrapperCore.SetAttribute(ref platform, instance,
			MuiExternalWrapperAttributes.Boopsi_ClassID, cwId.Raw, true, false,
			out _, out _);
		MuiExternalWrapperCore.SetAttribute(ref platform, instance,
			MuiExternalWrapperAttributes.Boopsi_MinWidth, 30, true, false, out _,
			out _);
		MuiExternalWrapperCore.SetAttribute(ref platform, instance,
			MuiExternalWrapperAttributes.Boopsi_Remember, 0xC001, true, false,
			out _, out _);
		APTR.WriteUInt32(creationTags, 0, 0);   // TAG_DONE
		if (!MuiExternalWrapperCore.SetCreationTags(ref platform, instance,
			creationTags)) return 3;
		APTR.WriteUInt32(packet, 0, 0x80428354); // MUIM_Setup
		APTR.WriteUInt32(packet, 4, renderInfo.Raw);
		if (MuiExternalWrapperDispatcher.Dispatch(ref platform, instance, packet)
			!= 1) return 4;
		if (!MuiExternalWrapperCore.IsObjectCreated(ref platform, instance))
			return 5;

		// The external wrapper's object get/set records are exercised before
		// geometry so the native closure covers both struct-first boundaries.
		APTR.WriteUInt32(packet, 0, 0x8042216F); // MUIM_NoNotifySet
		APTR.WriteUInt32(packet, 4, MuiExternalWrapperAttributes.Boopsi_MinWidth);
		APTR.WriteUInt32(packet, 8, 31);
		if (MuiExternalWrapperDispatcher.Dispatch(ref platform, instance, packet)
			!= 1) return 5;
		APTR.WriteUInt32(packet, 0, 0x00000104); // OM_GET
		APTR.WriteUInt32(packet, 4, MuiExternalWrapperAttributes.Boopsi_MinWidth);
		APTR.WriteUInt32(packet, 8, minMax.Raw);
		if (MuiExternalWrapperDispatcher.Dispatch(ref platform, instance, packet)
			!= 1 || APTR.ReadUInt32(minMax, 0) != 31) return 5;

		// The documented colorwheel -1 width/height workaround.
		APTR.WriteUInt32(packet, 0, 0x8042845B); // MUIM_Layout
		APTR.WriteUInt32(packet, 4, 0);
		APTR.WriteUInt32(packet, 8, 0);
		APTR.WriteUInt32(packet, 12, 100);
		APTR.WriteUInt32(packet, 16, 50);
		if (MuiExternalWrapperDispatcher.Dispatch(ref platform, instance, packet)
			!= 1) return 6;
		var work = APTR.FromPointer(APTR.ReadUInt32(instance, 80));
		if (APTR.ReadUInt32(work, 36) != 99) return 7;   // width - 1
		if (APTR.ReadUInt32(work, 44) != 49) return 8;   // height - 1

		APTR.WriteUInt32(packet, 0, 0x8042CC84); // MUIM_Show
		if (MuiExternalWrapperDispatcher.Dispatch(ref platform, instance, packet)
			!= 1) return 9;
		APTR.WriteUInt32(packet, 0, 0x80426F3F); // MUIM_Draw
		if (MuiExternalWrapperDispatcher.Dispatch(ref platform, instance, packet)
			!= 1) return 10;

		// IDCMP_UPDATE -> MUI notification.
		APTR.WriteUInt32(attrList, 0, 0x80421234);
		APTR.WriteUInt32(attrList, 4, 77);
		APTR.WriteUInt32(attrList, 8, 0);
		APTR.WriteUInt32(packet, 0, 0x108);   // OM_UPDATE
		APTR.WriteUInt32(packet, 4, attrList.Raw);
		APTR.WriteUInt32(packet, 8, 0);
		APTR.WriteUInt32(packet, 12, 0);
		if (MuiExternalWrapperDispatcher.Dispatch(ref platform, instance, packet)
			!= 1) return 11;
		if (MuiExternalWrapperCore.NotificationCount(ref platform, instance) != 1)
			return 12;

		// Regenerate disposes/recreates the object but keeps the class open.
		if (!MuiExternalWrapperCore.Regenerate(ref platform, instance)) return 13;
		if (!MuiExternalWrapperCore.IsObjectCreated(ref platform, instance))
			return 14;

		APTR.WriteUInt32(packet, 0, 0x8042D985); // MUIM_Cleanup
		if (MuiExternalWrapperDispatcher.Dispatch(ref platform, instance, packet)
			!= 1) return 15;
		if (!MuiExternalWrapperLifecycle.Dispose(ref platform, instance)) return 16;
		if (MuiExternalWrapperCore.Valid(ref platform, instance)) return 17;
		if (MuiExternalWrapperLifecycle.Dispose(ref platform, instance)) return 18;

		// ---- Dtpic -----------------------------------------------------------
		WriteClassId(classId, 'D', 't', 'p', 'i', 'c', (char)0, (char)0, (char)0,
			(char)0);
		if (MuiExternalWrapperCore.CreateByName(ref platform, instance, classId) !=
			MuiExternalWrapperClass.Dtpic) return 19;
		APTR.WriteUInt8(nameA, 0, (byte)'l');
		APTR.WriteUInt8(nameA, 1, (byte)'o');
		APTR.WriteUInt8(nameA, 2, (byte)'g');
		APTR.WriteUInt8(nameA, 3, (byte)'o');
		APTR.WriteUInt8(nameA, 4, 0);
		if (!MuiExternalWrapperCore.SetName(ref platform, instance, nameA))
			return 20;
		MuiExternalWrapperCore.GetAttribute(ref platform, instance,
			MuiExternalWrapperAttributes.Dtpic_Name, out var owned);
		if (owned == nameA.Raw) return 21;                       // must be a copy
		if (APTR.ReadUInt8(APTR.FromPointer(owned), 0) != (byte)'l') return 22;
		APTR.WriteUInt8(nameA, 0, (byte)'X');                    // mutate caller
		if (APTR.ReadUInt8(APTR.FromPointer(owned), 0) != (byte)'l') return 23;

		MuiExternalWrapperCore.SetAttribute(ref platform, instance,
			MuiExternalWrapperAttributes.Dtpic_Alpha, 128, false, false, out _,
			out _);
		APTR.WriteUInt32(packet, 0, 0x80428354); // MUIM_Setup
		APTR.WriteUInt32(packet, 4, renderInfo.Raw);
		if (MuiExternalWrapperDispatcher.Dispatch(ref platform, instance, packet)
			!= 1)
			return 24;
		if (!MuiExternalWrapperCore.IsPictureAcquired(ref platform, instance))
			return 25;
		APTR.WriteUInt32(packet, 0, 0x80423874); // MUIM_AskMinMax
		APTR.WriteUInt32(packet, 4, minMax.Raw);
		if (MuiExternalWrapperDispatcher.Dispatch(ref platform, instance, packet)
			!= 1)
			return 26;
		if (APTR.ReadUInt16(minMax, 0) != 32) return 27;   // native fixture width
		if (APTR.ReadUInt16(minMax, 2) != 24) return 28;   // native fixture height
		APTR.WriteUInt32(packet, 0, 0x8042CC84); // MUIM_Show
		if (MuiExternalWrapperDispatcher.Dispatch(ref platform, instance, packet)
			!= 1) return 29;
		APTR.WriteUInt32(packet, 0, 0x80426F3F); // MUIM_Draw
		if (MuiExternalWrapperDispatcher.Dispatch(ref platform, instance, packet)
			!= 1) return 30;
		APTR.WriteUInt32(packet, 0, 0x8042D985); // MUIM_Cleanup
		if (MuiExternalWrapperDispatcher.Dispatch(ref platform, instance, packet)
			!= 1) return 31;
		if (MuiExternalWrapperCore.IsPictureAcquired(ref platform, instance))
			return 32;
		if (!MuiExternalWrapperLifecycle.Dispose(ref platform, instance)) return 33;
		if (MuiExternalWrapperCore.Valid(ref platform, instance)) return 34;

		return 42;
	}

	// Write "colorwheel.gadget" (NUL terminated) for the -1 workaround path.
	private static void WriteColorwheelId(APTR address)
	{
		APTR.WriteUInt8(address, 0, (byte)'c');
		APTR.WriteUInt8(address, 1, (byte)'o');
		APTR.WriteUInt8(address, 2, (byte)'l');
		APTR.WriteUInt8(address, 3, (byte)'o');
		APTR.WriteUInt8(address, 4, (byte)'r');
		APTR.WriteUInt8(address, 5, (byte)'w');
		APTR.WriteUInt8(address, 6, (byte)'h');
		APTR.WriteUInt8(address, 7, (byte)'e');
		APTR.WriteUInt8(address, 8, (byte)'e');
		APTR.WriteUInt8(address, 9, (byte)'l');
		APTR.WriteUInt8(address, 10, (byte)'.');
		APTR.WriteUInt8(address, 11, (byte)'g');
		APTR.WriteUInt8(address, 12, (byte)'a');
		APTR.WriteUInt8(address, 13, (byte)'d');
		APTR.WriteUInt8(address, 14, (byte)'g');
		APTR.WriteUInt8(address, 15, (byte)'e');
		APTR.WriteUInt8(address, 16, (byte)'t');
		APTR.WriteUInt8(address, 17, 0);
	}

	public static int ResolveVector(int lvo)
	{		return MuiVectorRouter.TryResolve(lvo, out MuiVectorId vector)
			? (int)vector
			: -1;
	}

	public static int ResolveKnownVector()
	{
		return MuiVectorRouter.TryResolve(-756, out MuiVectorId vector)
			? (int)vector
			: -1;
	}
}
